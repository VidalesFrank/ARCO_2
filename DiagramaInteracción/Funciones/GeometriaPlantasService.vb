' ═══════════════════════════════════════════════════════════════════════════════
'  GeometriaPlantasService — resuelve coordenadas en planta de los elementos
'  estructurales a partir de los Joints importados desde Excel, y reporta qué
'  módulos quedan sin ubicar para que la UI de cada planta pueda mostrar
'  warning e invitar a importar el E2K.
'
'  Convención del proyecto: una etiqueta de nodo se repite en cada piso, así
'  que se usa el nodo de MENOR Z (el de cimentación) — mismo criterio que
'  ZapataService.IndexarJointsDeCimentacion y Funciones_00_Varias para Pilas.
'
'  El enlace elemento→nodo por módulo:
'
'    Pilas    Elemento_Pila.Name_Label ≡ cJoint.ObjectLabel
'    Columnas (futuro — Fase 4) Columna.Coordenadas se setea desde Frame.JointI
'    Muros    (futuro — Fase 3) Muro.Coor_X/Y ya se setea al importar
'
'  La resolución es idempotente: si TieneCoordenadas ya es True, no se repisa.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class GeometriaPlantasService

    ''' <summary>
    ''' Resumen de qué pudo y qué no pudo ubicarse por módulo. HayFaltantes es
    ''' lo que decide si cada planta muestra el warning de "importá e2k".
    ''' </summary>
    Public Class ReporteFaltantes
        Public Property JointsImportados As Integer

        Public Property PilasConCoord As Integer
        Public Property PilasSinCoord As Integer
        Public Property PilasSinJointAsociado As New List(Of String)

        Public Property ColumnasConCoord As Integer
        Public Property ColumnasSinCoord As Integer

        Public Property MurosConCoord As Integer
        Public Property MurosSinCoord As Integer

        Public ReadOnly Property HayFaltantes As Boolean
            Get
                Return PilasSinCoord > 0 OrElse ColumnasSinCoord > 0 OrElse MurosSinCoord > 0
            End Get
        End Property

        Public ReadOnly Property PlantaPilasListaParaDibujar As Boolean
            Get
                Return PilasConCoord > 0
            End Get
        End Property

        Public ReadOnly Property PlantaColumnasListaParaDibujar As Boolean
            Get
                Return ColumnasConCoord > 0
            End Get
        End Property

        Public ReadOnly Property PlantaMurosListaParaDibujar As Boolean
            Get
                Return MurosConCoord > 0
            End Get
        End Property

        ''' <summary>Mensaje legible para el panel "faltan datos" de la planta.</summary>
        Public Function MensajePara(modulo As String) As String
            Select Case modulo.ToLowerInvariant()
                Case "pilas"
                    If JointsImportados = 0 Then
                        Return "No hay nodos importados. Volvé a importar el Excel de ETABS incluyendo la hoja de nodos " &
                               """Objects and Elements - Joints"" (E23) o ""Joint Coordinates"" (E17); o importá el .e2k."
                    End If
                    If PilasSinCoord > 0 Then
                        Return $"{PilasSinCoord} pila(s) no cruzan con un nodo de cimentación por su etiqueta. " &
                               "Revisá que los nombres de los apoyos coincidan con los Object Labels del Excel."
                    End If
                    Return ""

                Case "columnas"
                    If ColumnasSinCoord > 0 Then
                        Return $"{ColumnasSinCoord} columna(s) no tienen coordenadas en planta. " &
                               "Volvé a importar el Excel de ETABS o importá el .e2k del proyecto."
                    End If
                    Return ""

                Case "muros"
                    If MurosSinCoord > 0 Then
                        Return $"{MurosSinCoord} muro(s) no tienen coordenadas en planta. " &
                               "Volvé a importar el Excel de ETABS o importá el .e2k del proyecto."
                    End If
                    Return ""

                Case Else
                    Return ""
            End Select
        End Function
    End Class

    ' =====================================================================
    ' PILAS — enlace por Name_Label ≡ cJoint.ObjectLabel
    ' =====================================================================

    ''' <summary>
    ''' Para cada Elemento_Pila sin coords, intenta resolverlas en dos pasos:
    '''
    '''   1. Cruce por Joints: busca cJoint cuyo ObjectLabel = Name_Label
    '''      (fallback por ElementLabel, igual que Zapatas) y copia GlobalX/Y
    '''      del nodo de MENOR Z. Resuelve pilas Frame donde Name_Label es un
    '''      ObjectLabel real.
    '''
    '''   2. Fallback por cPilas.CoordsPorLabel: diccionario poblado al
    '''      confirmar la importación con cCandidatoPila.SourceLabel → (X,Y).
    '''      Resuelve pilas Pier, donde Name_Label = pierName y NO cruza con
    '''      ningún joint. También cubre Frames en proyectos sin la hoja de
    '''      nodos importada.
    '''
    ''' Idempotente: una pila que ya tiene TieneCoordenadas = True no se toca,
    ''' así se preserva un seteo manual futuro.
    ''' </summary>
    Public Shared Function ResolverPilas(pilas As cPilas, joints As List(Of cJoint)) As Integer
        If pilas Is Nothing OrElse pilas.ListaElementos Is Nothing Then Return 0

        Dim hayJoints As Boolean = joints IsNot Nothing AndAlso joints.Count > 0
        Dim hayCoordsMap As Boolean = pilas.CoordsPorLabel IsNot Nothing AndAlso pilas.CoordsPorLabel.Count > 0
        If Not hayJoints AndAlso Not hayCoordsMap Then Return 0

        Dim porLabel As Dictionary(Of String, cJoint) = Nothing
        Dim porElem As Dictionary(Of String, cJoint) = Nothing
        If hayJoints Then
            porLabel = ZapataService.IndexarJointsDeCimentacion(joints, porElementName:=False)
            porElem = ZapataService.IndexarJointsDeCimentacion(joints, porElementName:=True)
        End If

        Dim resueltas As Integer = 0
        For Each p In pilas.ListaElementos
            If p Is Nothing Then Continue For
            If p.TieneCoordenadas Then Continue For

            Dim lbl = If(p.Name_Label, "").Trim()
            If lbl.Length = 0 Then Continue For

            ' Paso 1: cruce por joints (resuelve Frames)
            If hayJoints Then
                Dim j As cJoint = Nothing
                If porLabel.TryGetValue(lbl, j) OrElse porElem.TryGetValue(lbl, j) Then
                    If j IsNot Nothing Then
                        p.CoordX = CSng(j.GlobalX)
                        p.CoordY = CSng(j.GlobalY)
                        p.TieneCoordenadas = True
                        resueltas += 1
                        Continue For
                    End If
                End If
            End If

            ' Paso 2: fallback por el diccionario del import (resuelve Piers)
            If hayCoordsMap Then
                Dim pt As System.Drawing.PointF
                If pilas.CoordsPorLabel.TryGetValue(lbl, pt) Then
                    p.CoordX = pt.X
                    p.CoordY = pt.Y
                    p.TieneCoordenadas = True
                    resueltas += 1
                End If
            End If
        Next

        Return resueltas
    End Function

    ' =====================================================================
    ' COLUMNAS — enlace Tramo_Columna.Name_Elemento ≡ cFrame.ObjectLabel
    ' =====================================================================

    ''' <summary>
    ''' Para cada Columna sin coords, busca un frame cuyo ObjectLabel coincida
    ''' con el Name_Elemento de alguno de sus tramos, y copia GlobalX/Y del
    ''' joint I (base) de ese frame. En planta JointI y JointJ comparten X/Y
    ''' por verticalidad, así que cualquiera sirve — usamos el de MENOR Z.
    '''
    ''' Idempotente: columnas ya ubicadas (Coordenadas != (0,0)) no se tocan.
    ''' </summary>
    Public Shared Function ResolverColumnas(columnas As cColumnas,
                                            joints As List(Of cJoint),
                                            frames As List(Of cFrame)) As Integer
        If columnas Is Nothing OrElse columnas.Lista_Columnas Is Nothing Then Return 0

        ' Index frames por ObjectLabel y joints por ElementLabel. En proyectos
        ' solo-Pier estas listas pueden venir vacías; igual intentamos resolver
        ' por CoordsPorLabel (fallback).
        Dim framePorLabel As New Dictionary(Of String, cFrame)(StringComparer.OrdinalIgnoreCase)
        If frames IsNot Nothing Then
            For Each f In frames
                If f Is Nothing OrElse String.IsNullOrWhiteSpace(f.ObjectLabel) Then Continue For
                Dim exF As cFrame = Nothing
                If Not framePorLabel.TryGetValue(f.ObjectLabel, exF) Then
                    framePorLabel(f.ObjectLabel) = f
                End If
            Next
        End If

        Dim jointPorElem As Dictionary(Of String, cJoint) = Nothing
        If joints IsNot Nothing AndAlso joints.Count > 0 Then
            jointPorElem = ZapataService.IndexarJointsDeCimentacion(joints, porElementName:=True)
        End If

        Dim hayCoordsMap As Boolean =
            columnas.CoordsPorLabel IsNot Nothing AndAlso columnas.CoordsPorLabel.Count > 0

        Dim resueltas As Integer = 0
        For Each col In columnas.Lista_Columnas
            If col Is Nothing Then Continue For
            If col.Coordenadas IsNot Nothing AndAlso
               Not (col.Coordenadas.X = 0 AndAlso col.Coordenadas.Y = 0) Then Continue For

            If col.Lista_Tramos_Columnas Is Nothing OrElse col.Lista_Tramos_Columnas.Count = 0 Then Continue For

            Dim encontrado As Boolean = False

            ' 1) Cruce normal por frame + joint (columnas Frame).
            If framePorLabel.Count > 0 AndAlso jointPorElem IsNot Nothing Then
                For Each tr In col.Lista_Tramos_Columnas
                    If tr Is Nothing OrElse String.IsNullOrWhiteSpace(tr.Name_Elemento) Then Continue For
                    Dim fr As cFrame = Nothing
                    If Not framePorLabel.TryGetValue(tr.Name_Elemento.Trim(), fr) Then Continue For
                    If fr Is Nothing Then Continue For

                    Dim jt As cJoint = Nothing
                    If Not String.IsNullOrWhiteSpace(fr.JointI) AndAlso
                       jointPorElem.TryGetValue(fr.JointI, jt) Then
                    ElseIf Not String.IsNullOrWhiteSpace(fr.JointJ) AndAlso
                           jointPorElem.TryGetValue(fr.JointJ, jt) Then
                    End If
                    If jt Is Nothing Then Continue For

                    col.Coordenadas = New Columna.cCoordenadasElemento With {
                        .X = CSng(jt.GlobalX),
                        .Y = CSng(jt.GlobalY)
                    }
                    resueltas += 1
                    encontrado = True
                    Exit For
                Next
            End If

            ' 2) Fallback por CoordsPorLabel (columnas Pier — Name_Elemento = pier name).
            If Not encontrado AndAlso hayCoordsMap Then
                Dim pt As System.Drawing.PointF
                If columnas.CoordsPorLabel.TryGetValue(col.Name_Elemento, pt) Then
                    col.Coordenadas = New Columna.cCoordenadasElemento With {
                        .X = pt.X,
                        .Y = pt.Y
                    }
                    resueltas += 1
                End If
            End If
        Next

        Return resueltas
    End Function

    ' =====================================================================
    ' ENTRADA ÚNICA
    ' =====================================================================

    ''' <summary>
    ''' Resuelve lo que pueda y arma el reporte. La planta de cada módulo llama
    ''' esto antes de dibujar y decide si muestra el canvas o el warning.
    ''' </summary>
    Public Shared Function ResolverYValidar(proyecto As Proyecto) As ReporteFaltantes
        Dim rep As New ReporteFaltantes()
        If proyecto Is Nothing OrElse proyecto.Elementos Is Nothing Then Return rep

        Dim joints = proyecto.Elementos.Joints
        rep.JointsImportados = If(joints Is Nothing, 0, joints.Count)

        ' ── Pilas ──────────────────────────────────────────────────────
        If proyecto.Elementos.Pilas IsNot Nothing AndAlso
           proyecto.Elementos.Pilas.ListaElementos IsNot Nothing Then

            ResolverPilas(proyecto.Elementos.Pilas, joints)

            For Each p In proyecto.Elementos.Pilas.ListaElementos
                If p Is Nothing Then Continue For
                If p.TieneCoordenadas Then
                    rep.PilasConCoord += 1
                Else
                    rep.PilasSinCoord += 1
                    If Not String.IsNullOrWhiteSpace(p.Name_Label) Then
                        rep.PilasSinJointAsociado.Add(p.Name_Label)
                    End If
                End If
            Next
        End If

        ' ── Columnas ───────────────────────────────────────────────────
        ' Columna.Coordenadas se resuelve on-demand aquí porque el flujo de
        ' importación histórico no la seteaba. ResolverColumnas usa los Joints
        ' y Frames del proyecto (que están desde el import original).
        If proyecto.Elementos.Columnas IsNot Nothing AndAlso
           proyecto.Elementos.Columnas.Lista_Columnas IsNot Nothing Then

            Dim framesCol = proyecto.Elementos.Columnas.Frames
            If framesCol Is Nothing OrElse framesCol.Count = 0 Then framesCol = proyecto.Elementos.Frames
            ResolverColumnas(proyecto.Elementos.Columnas, joints, framesCol)

            For Each c In proyecto.Elementos.Columnas.Lista_Columnas
                If c Is Nothing Then Continue For
                If c.Coordenadas Is Nothing Then
                    rep.ColumnasSinCoord += 1
                ElseIf c.Coordenadas.X = 0 AndAlso c.Coordenadas.Y = 0 Then
                    rep.ColumnasSinCoord += 1
                Else
                    rep.ColumnasConCoord += 1
                End If
            Next
        End If

        ' ── Muros ──────────────────────────────────────────────────────
        If proyecto.Elementos.Muros IsNot Nothing AndAlso
           proyecto.Elementos.Muros.Lista_Muros IsNot Nothing Then

            For Each m In proyecto.Elementos.Muros.Lista_Muros
                If m Is Nothing Then Continue For
                Dim sinUbicacion As Boolean = (m.Coor_X = 0 AndAlso m.Coor_Y = 0)
                Dim sinLargo As Boolean = (m.Lw <= 0)
                If sinUbicacion OrElse sinLargo Then
                    rep.MurosSinCoord += 1
                Else
                    rep.MurosConCoord += 1
                End If
            Next
        End If

        Return rep
    End Function

End Class
