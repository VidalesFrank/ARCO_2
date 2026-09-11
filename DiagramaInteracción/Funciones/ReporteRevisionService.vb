Imports System.IO
Imports DocumentFormat.OpenXml.Packaging
Imports DocumentFormat.OpenXml.Wordprocessing
Imports ARCO.eNumeradores

''' <summary>Opciones capturadas en <see cref="Form_Reporte_Revision"/> para generar el reporte.</summary>
Public Class OpcionesReporteRevision
    Public Property Fecha As Date = Date.Today
    Public Property CodigoProyecto As String = ""
    Public Property DestinatarioNombre As String = ""
    Public Property DestinatarioCargo As String = ""
    Public Property Cliente As String = ""
    Public Property AsuntoProyecto As String = ""

    Public Property IncluirCimentaciones As Boolean = True
    Public Property IncluirColumnas As Boolean = True
    Public Property IncluirMuros As Boolean = True
    Public Property IncluirVigas As Boolean = True
    Public Property IncluirLosas As Boolean = True
    Public Property IncluirDetallesPlanos As Boolean = True

    Public Property DetallesPlanos As New List(Of String)
End Class

''' <summary>
''' Genera el "Reporte de Revisión" (.docx) con el estilo de EstrucMed a partir de los resultados
''' ya calculados en un <see cref="Proyecto"/> de ARCO. Solo lista, por verificación, los elementos
''' con capacidad/demanda (C/D) inferior a <see cref="UMBRAL_CD"/> — igual que el reporte manual de
''' referencia (FormatosRevisión\P53844_C008_ReporteRevisión_E1A.docx).
''' </summary>
Public Class ReporteRevisionService

    Private Const UMBRAL_CD As Double = 0.9

    Private _numTabla As Integer = 0

    Private Shared Function EsConforme(ratio As Double) As Boolean
        Return ratio >= UMBRAL_CD
    End Function

    ''' Devuelve True si la zona indicada supera el chequeo de cortante plástico (C.21.5.4).
    ''' Solo aplica para zonas Izquierda/Derecha; la zona Centro no tiene chequeo plástico.
    Private Shared Function CumpleCortantePlastico(pos As PosicionTramoViga,
                                                    cp As cResultadoCortantePlasticoFrame) As Boolean
        If cp Is Nothing Then Return False
        Select Case pos
            Case PosicionTramoViga.Izquierda : Return cp.ZonaIzq IsNot Nothing AndAlso cp.ZonaIzq.Cumple
            Case PosicionTramoViga.Derecha : Return cp.ZonaDer IsNot Nothing AndAlso cp.ZonaDer.Cumple
            Case Else : Return False
        End Select
    End Function

    Private Shared Function FCD(valor As Double) As String
        Return valor.ToString("0.00")
    End Function

    Private Shared Function FNum(valor As Double, Optional decimales As Integer = 2) As String
        Return valor.ToString("0." & New String("0"c, decimales))
    End Function

    ' =========================================================================
    '  Punto de entrada
    ' =========================================================================
    Public Shared Sub GenerarReporte(proyecto As Proyecto, opciones As OpcionesReporteRevision, rutaSalida As String)
        Dim servicio As New ReporteRevisionService()
        servicio.Generar(proyecto, opciones, rutaSalida)
    End Sub

    Private Sub Generar(proyecto As Proyecto, opciones As OpcionesReporteRevision, rutaSalida As String)
        File.WriteAllBytes(rutaSalida, My.Resources.PlantillaReporteRevision)

        Using wordDoc As WordprocessingDocument = WordprocessingDocument.Open(rutaSalida, True)
            Dim body As Body = wordDoc.MainDocumentPart.Document.Body
            Dim sectPr As SectionProperties = body.Elements(Of SectionProperties)().FirstOrDefault()
            If sectPr IsNot Nothing Then sectPr.Remove()

            EscribirCarta(body, proyecto, opciones)

            If opciones.IncluirCimentaciones Then
                body.Append(Heading1("CIMENTACIONES"))
                EscribirSeccionPilas(body, proyecto)
                body.Append(Heading2("VIGAS DE CIMENTACIÓN"))
                EscribirVigasCimentacion(body)
            End If

            If opciones.IncluirColumnas Then
                body.Append(Heading1("COLUMNAS"))
                EscribirSeccionColumnas(body, proyecto)
            End If

            If opciones.IncluirMuros Then
                body.Append(Heading1("MUROS"))
                EscribirSeccionMuros(body, proyecto)
            End If

            If opciones.IncluirVigas Then
                body.Append(Heading1("VIGAS"))
                EscribirSeccionVigas(body, proyecto)
            End If

            If opciones.IncluirLosas Then
                body.Append(Heading1("LOSAS"))
                EscribirSeccionLosas(body, proyecto)
            End If

            If opciones.IncluirDetallesPlanos Then
                body.Append(Heading1("DETALLES EN PLANOS"))
                EscribirDetallesPlanos(body, opciones.DetallesPlanos)
            End If

            EscribirCierre(body)

            If sectPr IsNot Nothing Then body.Append(sectPr)

            ActualizarCodigoEnEncabezado(wordDoc, opciones.CodigoProyecto)

            wordDoc.MainDocumentPart.Document.Save()
        End Using
    End Sub

    ' =========================================================================
    '  Carta / portada
    ' =========================================================================
    Private Sub EscribirCarta(body As Body, proyecto As Proyecto, opciones As OpcionesReporteRevision)
        Dim info = proyecto.Info

        If Not String.IsNullOrWhiteSpace(opciones.DestinatarioNombre) Then
            body.Append(ParrafoNormal(opciones.DestinatarioNombre & ","))
        End If
        If Not String.IsNullOrWhiteSpace(opciones.DestinatarioCargo) Then
            body.Append(ParrafoNormal(opciones.DestinatarioCargo))
        End If
        If Not String.IsNullOrWhiteSpace(opciones.Cliente) Then
            body.Append(NuevoParrafoNegrita(opciones.Cliente))
        End If
        body.Append(ParrafoNormal(""))

        Dim asunto As String = If(String.IsNullOrWhiteSpace(opciones.AsuntoProyecto), info.Nombre, opciones.AsuntoProyecto)
        body.Append(ParrafoNormal("Asunto: ", "Reporte de la revisión del diseño estructural del proyecto " & asunto & "."))
        body.Append(ParrafoNormal(""))

        body.Append(ParrafoNormal(TextoIntroProyecto(info)))

        body.Append(ParrafoNormal(
            "Se realizó la revisión del diseño estructural de acuerdo con la información recibida. En esta se evalúan los " &
            "elementos estructurales y se presenta la relación más desfavorable entre la capacidad y la demanda; si esta " &
            "relación es mayor a la unidad indica que la capacidad del elemento es mayor que la demanda; sin embargo, el " &
            "grupo técnico de EstrucMed considera que los elementos que presentan una relación cercana a 0,90 se encuentran " &
            "dentro del rango admisible asociado probablemente a diferencias menores de modelación y distribución de cargas; " &
            "mientras que los elementos que presentan una relación inferior podrían requerir una mayor capacidad para cumplir " &
            "con el requisito evaluado."))

        body.Append(ParrafoNormal(
            "En este documento se presentan los resultados de los elementos en los cuales la relación capacidad/demanda " &
            "obtenida es inferior a 0,90 y por lo tanto deben ser revisados por el diseñador estructural. Los elementos que " &
            "no se presentan en este documento, cumplen con la capacidad requerida."))
        body.Append(ParrafoNormal(""))
    End Sub

    Private Shared Function TextoIntroProyecto(info As cInfoProyecto) As String
        Dim nombre As String = If(String.IsNullOrWhiteSpace(info.Nombre), "xx", info.Nombre)
        Dim ubicacion As String = String.Join(", ", {info.Direccion, info.Ciudad, info.Departamento}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
        If String.IsNullOrWhiteSpace(ubicacion) Then ubicacion = "xx"
        Dim disenador As String = If(String.IsNullOrWhiteSpace(info.Designer), "xx", info.Designer)
        Dim sistema As String = TextoSistemaEstructural(info.SistemaEstructural)
        Dim pisos As String = If(info.NPisos > 0, info.NPisos.ToString(), "xx")

        Dim comillaI As String = ChrW(8220)
        Dim comillaD As String = ChrW(8221)
        Return "El presente reporte incluye la revisión del sistema estructural, superestructura y subestructura del " &
               "proyecto " & comillaI & nombre & comillaD & ", el cual se encuentra localizado en " & ubicacion & ". El diseño estructural " &
               "fue desarrollado por " & disenador & ". El sistema estructural principal consiste en " & sistema &
               ". Este proyecto cuenta con " & pisos & " piso(s)."
    End Function

    Private Shared Function TextoSistemaEstructural(sistema As eSistemaEstructural) As String
        Select Case sistema
            Case eSistemaEstructural.Porticos : Return "pórticos de concreto reforzado"
            Case eSistemaEstructural.Combinado : Return "un sistema combinado de pórticos y muros de concreto reforzado"
            Case eSistemaEstructural.MCR : Return "muros de concreto reforzado"
            Case Else : Return "xx"
        End Select
    End Function

    Private Sub EscribirCierre(body As Body)
        body.Append(ParrafoNormal("Quedamos atentos a cualquier aclaración o información adicional que usted requiera."))
        body.Append(ParrafoNormal(""))
        body.Append(ParrafoNormal("Atentamente,"))
        body.Append(ParrafoNormal(""))
        body.Append(ParrafoNormal(""))
        body.Append(ParrafoNormal("Ricardo León Bonett Díaz"))
        body.Append(ParrafoNormal("Gerente Técnico"))
        body.Append(NuevoParrafoNegrita("EstrucMed Ingeniería Especializada"))
    End Sub

    Private Shared Function NuevoParrafoNegrita(texto As String) As Paragraph
        Dim p As New Paragraph()
        p.Append(CrearRun(texto, negrita:=True))
        Return p
    End Function

    ' =========================================================================
    '  PILAS
    ' =========================================================================
    Private Sub EscribirSeccionPilas(body As Body, proyecto As Proyecto)
        body.Append(Heading2("PILAS"))

        Dim pilas As List(Of Elemento_Pila) = proyecto.Elementos.Pilas.ListaElementos
        If pilas Is Nothing Then pilas = New List(Of Elemento_Pila)

        ' ── Flexo compresión ────────────────────────────────────────────────
        body.Append(Heading2("Flexo compresión"))
        Dim noFlexo = pilas.Where(Function(p) p.Factor_Diagonal > 0 AndAlso Not EsConforme(p.Factor_Diagonal)).ToList()
        If noFlexo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de flexo compresión es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexo-compresión de las pilas."))
            Dim filas As New List(Of String())
            For Each p In noFlexo
                filas.Add({p.Name_Elemento, FNum(p.Df), FNum(p.Dc), FormatoRefuerzo(p), FCD(p.Factor_Diagonal)})
            Next
            body.Append(TablaDatos({"PILA", "Df (m)", "Dc (m)", "REFUERZO", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("Cortante"))
        Dim noCortante = pilas.Where(Function(p) p.FactorShear > 0 AndAlso Not EsConforme(p.FactorShear)).ToList()
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de cortante de las pilas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de las pilas."))
            Dim filas As New List(Of String())
            For Each p In noCortante
                filas.Add({p.Name_Elemento, FCD(p.FactorShear)})
            Next
            body.Append(TablaDatos({"PILA", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Esfuerzos admisibles del concreto ───────────────────────────────
        body.Append(Heading2("Esfuerzos admisibles del concreto"))
        Dim conEsfConcreto = pilas.Select(Function(p) (Pila:=p, CD:=MinEsfConcreto(p))).Where(Function(x) x.CD > 0 AndAlso Not EsConforme(x.CD)).ToList()
        If conEsfConcreto.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad admisible del concreto es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de los esfuerzos del concreto."))
            Dim filas As New List(Of String())
            For Each x In conEsfConcreto
                filas.Add({x.Pila.Name_Elemento, FCD(x.CD)})
            Next
            body.Append(TablaDatos({"PILA", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Esfuerzos transmitidos al suelo ─────────────────────────────────
        body.Append(Heading2("Esfuerzos transmitidos al suelo"))
        Dim conEsfSuelo = pilas.Select(Function(p) (Pila:=p, CD:=MinEsfSuelo(p))).Where(Function(x) x.CD > 0 AndAlso Not EsConforme(x.CD)).ToList()
        If conEsfSuelo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de esfuerzos transmitidos al terreno es adecuada para las cargas del proyecto."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Capacidad de esfuerzos sobre el terreno."))
            Dim filas As New List(Of String())
            For Each x In conEsfSuelo
                filas.Add({x.Pila.Name_Elemento, FNum(x.Pila.Dc), FCD(x.CD)})
            Next
            body.Append(TablaDatos({"PILA", "CAMPANA COLOCADA (m)", "CAPACIDAD/DEMANDA"}, filas))
        End If
    End Sub

    Private Shared Function FormatoRefuerzo(p As Elemento_Pila) As String
        Return p.Cant_Barras_Long.ToString() & " x " & p.N_Barra_Long
    End Function

    Private Shared Function MinEsfConcreto(p As Elemento_Pila) As Double
        Dim valores = {p.Check1_PsE, p.Check2_PsD, p.Check3_PuE, p.Check4_PuD, p.Check5_PuT}.Where(Function(v) v > 0).ToList()
        If valores.Count = 0 Then Return 0
        Return valores.Min()
    End Function

    Private Shared Function MinEsfSuelo(p As Elemento_Pila) As Double
        Dim valores = {p.Relacion_EsfE, p.Relacion_EsfD}.Where(Function(v) v > 0).ToList()
        If valores.Count = 0 Then Return 0
        Return valores.Min()
    End Function

    Private Sub EscribirVigasCimentacion(body As Body)
        body.Append(PendienteImplementar(
            "el módulo de vigas de cimentación (Form_08_VigasFundacion) calcula Pmáx a compresión/tracción y φPn, " &
            "pero el resultado no se persiste por elemento dentro del proyecto — corre manualmente, uno a la vez — " &
            "por lo que hoy no es posible listarlos automáticamente en este reporte."))
    End Sub

    ' =========================================================================
    '  COLUMNAS
    ' =========================================================================
    Private Sub EscribirSeccionColumnas(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de las columnas a partir de la información recibida."))

        Dim columnas As List(Of Columna) = proyecto.Elementos.Columnas.Lista_Columnas
        If columnas Is Nothing Then columnas = New List(Of Columna)
        Dim columnasCalculadas = columnas.Where(
            Function(c) c.Ref_Modificado AndAlso
                        c.Lista_Tramos_Columnas IsNot Nothing AndAlso
                        c.Lista_Tramos_Columnas.Count > 0).ToList()

        ' ── Flexo-compresión ─────────────────────────────────────────────────
        body.Append(Heading2("FLEXO-COMPRESIÓN"))
        Dim noFlexo As New List(Of String())
        For Each col In columnasCalculadas
            For Each tr In col.Lista_Tramos_Columnas
                Dim cdTop = CDbl(tr.F_Flexo_Top)
                Dim cdBot = CDbl(tr.F_Flexo_Bottom)
                Dim failTop = cdTop > 0 AndAlso Not EsConforme(cdTop)
                Dim failBot = cdBot > 0 AndAlso Not EsConforme(cdBot)
                If Not failTop AndAlso Not failBot Then Continue For

                Dim asCol = Math.Max(CDbl(tr.As_Col_Top), CDbl(tr.As_Col_Bottom))
                Dim asReq = Math.Max(CDbl(tr.As_Req_Top), CDbl(tr.As_Req_Bottom))
                Dim cdVals = {cdTop, cdBot}.Where(Function(v) v > 0)
                Dim cdMin = If(cdVals.Any(), cdVals.Min(), 0.0)
                Dim obs As String
                If failTop AndAlso failBot Then
                    obs = "En zona superior e inferior"
                ElseIf failTop Then
                    obs = "En zona superior"
                Else
                    obs = "En zona inferior"
                End If
                noFlexo.Add({col.Name_Elemento, tr.Piso, FNum(asCol), FNum(asReq), FCD(cdMin), obs})
            Next
        Next
        If noFlexo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de flexo compresión es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexo-compresión de las columnas."))
            body.Append(TablaDatos({"COLUMNA", "TRAMO", "As colocada (cm" & ChrW(178) & ")", "As requerida (cm" & ChrW(178) & ")", "C/D", "OBSERVACIONES"}, noFlexo, {4}))
        End If

        ' ── Cortante (Ash colocado vs requerido, LC y LL) ───────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of String())
        For Each col In columnasCalculadas
            For Each tr In col.Lista_Tramos_Columnas
                Dim failLC = tr.F_Ash_Corto > 0 AndAlso Not EsConforme(CDbl(tr.F_Ash_Corto))
                Dim failLL = tr.F_Ash_Largo > 0 AndAlso Not EsConforme(CDbl(tr.F_Ash_Largo))
                If Not failLC AndAlso Not failLL Then Continue For
                Dim seccion = FNum(CDbl(tr.B_Plano), 2) & "x" & FNum(CDbl(tr.H_Plano), 2)
                Dim verif = If(failLC AndAlso failLL, "No cumple (LC y LL)",
                            If(failLC, "No cumple (LC)", "No cumple (LL)"))
                noCortante.Add({col.Name_Elemento, seccion,
                                FNum(CDbl(tr.Ash_Col_Corto)), FNum(CDbl(tr.Ash_Col_Largo)),
                                FNum(CDbl(tr.Ash_C)), FNum(CDbl(tr.Ash_L)),
                                verif})
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a cortante de las columnas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de las columnas."))
            body.Append(TablaDatos(
                {"COLUMNA", "SECCIÓN", "Ash col. LC (cm" & ChrW(178) & ")", "Ash col. LL (cm" & ChrW(178) & ")",
                 "Ash req. LC (cm" & ChrW(178) & ")", "Ash req. LL (cm" & ChrW(178) & ")", "VERIFICACIÓN"},
                noCortante, {6}))
        End If
    End Sub

    ' =========================================================================
    '  MUROS
    ' =========================================================================
    Private Sub EscribirSeccionMuros(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de los muros a partir de la información recibida."))

        Dim muros As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros
        If muros Is Nothing Then muros = New List(Of Muro)
        Dim murosCalculados = muros.Where(Function(m) m.Ref_Modificado_Muros).ToList()

        ' ── Flexo-compresión ─────────────────────────────────────────────────
        body.Append(Heading2("FLEXO-COMPRESIÓN"))
        Dim noFlexo As New List(Of (Muro As Muro, Sec As SeccionMuro, CD As Double))
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                Dim valores = {s.F_Flexo_Top, s.F_Flexo_Bot}.Where(Function(v) v > 0).ToList()
                If valores.Count = 0 Then Continue For
                Dim cd = valores.Min()
                If Not EsConforme(cd) Then noFlexo.Add((m, s, cd))
            Next
        Next
        If noFlexo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de flexo compresión es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexo-compresión de los muros."))
            Dim filas As New List(Of String())
            For Each x In noFlexo
                Dim asReq = Math.Max(x.Sec.As_Top_Req, x.Sec.As_Bot_Req)
                Dim asCol = Math.Max(x.Sec.AsT_Top_Col, x.Sec.AsT_Bot_Col)
                filas.Add({x.Muro.Name, x.Sec.Piso, FNum(asReq), FNum(asCol), FCD(x.CD)})
            Next
            body.Append(TablaDatos({"MURO", "PISO", "As Requerido (cm2)", "As Suministrado (cm2)", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of (Muro As Muro, Sec As SeccionMuro))
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                If s.F_Cortante > 0 AndAlso Not EsConforme(s.F_Cortante) Then noCortante.Add((m, s))
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de cortante de los muros es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de los muros."))
            Dim filas As New List(Of String())
            For Each x In noCortante
                Dim asReq = Math.Max(x.Sec.AsH_Req_Top, x.Sec.AsH_Req_Bot)
                filas.Add({x.Muro.Name, x.Sec.Piso, FNum(asReq), FNum(x.Sec.AsH_Col), FCD(x.Sec.F_Cortante)})
            Next
            body.Append(TablaDatos({"MURO", "PISO", "As Requerido (cm2)", "As Suministrado (cm2)", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Longitud vertical elemento de borde ─────────────────────────────
        body.Append(Heading2("LONGITUD VERTICAL ELEMENTO DE BORDE"))
        Dim noLV As New List(Of String())
        For Each m In murosCalculados
            Dim reqI = Math.Max(m.LV_EB_I_Req_Def, m.LV_EB_I_Req_Esf)
            If reqI > 0 AndAlso m.LV_EB_I_Col_Esp < reqI Then
                noLV.Add({m.Name, "Izquierdo", FNum(reqI), FNum(m.LV_EB_I_Col_Esp), "No Cumple"})
            End If
            Dim reqD = Math.Max(m.LV_EB_D_Req_Def, m.LV_EB_D_Req_Esf)
            If reqD > 0 AndAlso m.LV_EB_D_Col_Esp < reqD Then
                noLV.Add({m.Name, "Derecho", FNum(reqD), FNum(m.LV_EB_D_Col_Esp), "No Cumple"})
            End If
        Next
        If noLV.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la longitud vertical del elemento de borde es acorde a la solicitación en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la longitud vertical de los elementos de borde."))
            body.Append(TablaDatos({"MURO", "LADO", "LV Requerida (m)", "LV Colocada (m)", "VERIFICACIÓN"}, noLV))
        End If

        ' ── Longitud horizontal elemento de borde ───────────────────────────
        body.Append(Heading2("LONGITUD HORIZONTAL ELEMENTO DE BORDE"))
        Dim noLH As New List(Of String())
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                For Each par In New List(Of (Lado As String, EB As SeccionMuro.ElementoBorde)) From {
                    ("Izquierdo - Top", s.EB_I_Top), ("Izquierdo - Bot", s.EB_I_Bot),
                    ("Derecho - Top", s.EB_D_Top), ("Derecho - Bot", s.EB_D_Bot)}
                    If par.EB Is Nothing OrElse par.EB.L_EB_Req <= 0 Then Continue For
                    Dim cumple = par.EB.L_EB_Req <= par.EB.L_EB / 0.9
                    If Not cumple Then
                        noLH.Add({m.Name, s.Piso, par.Lado, FNum(par.EB.L_EB_Req), FNum(par.EB.L_EB), "No Cumple"})
                    End If
                Next
            Next
        Next
        If noLH.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la longitud horizontal del elemento de borde cumple en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la longitud horizontal de los elementos de borde."))
            body.Append(TablaDatos({"MURO", "PISO", "LADO", "LH Requerida (m)", "LH Colocada (m)", "VERIFICACIÓN"}, noLH))
        End If

        ' ── Confinamiento ────────────────────────────────────────────────────
        body.Append(Heading2("CONFINAMIENTO"))
        Dim noConf As New List(Of String())
        Dim nde = proyecto.ParametrosSismicos.NDE
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                For Each par In New List(Of (Lado As String, EB As SeccionMuro.ElementoBorde)) From {
                    ("Izquierdo - Top", s.EB_I_Top), ("Izquierdo - Bot", s.EB_I_Bot),
                    ("Derecho - Top", s.EB_D_Top), ("Derecho - Bot", s.EB_D_Bot)}
                    If par.EB Is Nothing OrElse par.EB.RefH Is Nothing OrElse par.EB.RefH.Separacion <= 0 Then Continue For
                    Try
                        Dim resultado = Funciones_Muros.AceroH_EB(nde, par.EB, s.tw_Planos, s.fc, s.fy)
                        Dim sMaxReq As Double = CDbl(resultado(0)) / 1000.0 ' mm -> m
                        Dim sCol As Double = par.EB.RefH.Separacion
                        If Math.Round(sCol, 3) > Math.Round(sMaxReq, 3) Then
                            noConf.Add({m.Name, s.Piso, par.Lado, FNum(sMaxReq, 3), FNum(sCol, 3), "No Cumple"})
                        End If
                    Catch
                        ' Datos insuficientes para calcular la separación requerida en este elemento de borde — se omite.
                    End Try
                Next
            Next
        Next
        If noConf.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la separación del refuerzo transversal de los elementos de borde cumple en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la separación máxima del refuerzo transversal de los elementos de borde."))
            body.Append(TablaDatos({"MURO", "PISO", "LADO", "S Requerida (m)", "S Colocada (m)", "VERIFICACIÓN"}, noConf))
            body.Append(ParrafoNormal(
                "Nota: esta verificación evalúa únicamente la separación del refuerzo transversal; la distribución de " &
                "ganchos suplementarios en el elemento de borde debe revisarse manualmente en los planos."))
        End If

        ' ── Requisitos normativos ────────────────────────────────────────────
        body.Append(Heading2("REQUISITOS NORMATIVOS"))
        body.Append(Heading2("Cuantías máximas"))
        body.Append(PendienteImplementar("no existe cálculo de cuantía máxima (Rho_Max) en el módulo de Muros."))

        body.Append(Heading2("Cuantías mínimas"))
        Dim noCuantiaMin As New List(Of String())
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                If s.Rho_Min_L <= 0 Then Continue For
                If s.Cuantia_Top_Col > 0 AndAlso s.Cuantia_Top_Col < s.Rho_Min_L Then
                    noCuantiaMin.Add({s.Piso, m.Name, "Top", FNum(s.Cuantia_Top_Col * 100, 2) & "%", "El refuerzo vertical (Top) no alcanza la cuantía mínima requerida."})
                End If
                If s.Cuantia_Bot_Col > 0 AndAlso s.Cuantia_Bot_Col < s.Rho_Min_L Then
                    noCuantiaMin.Add({s.Piso, m.Name, "Bot", FNum(s.Cuantia_Bot_Col * 100, 2) & "%", "El refuerzo vertical (Bot) no alcanza la cuantía mínima requerida."})
                End If
            Next
        Next
        If noCuantiaMin.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la cuantía vertical colocada cumple la cuantía mínima en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Muros donde la cuantía colocada es menor a la mínima requerida."))
            body.Append(TablaDatos({"PISO", "MURO", "TRAMO", "ρCol", "OBSERVACIÓN"}, noCuantiaMin, {}))
        End If
    End Sub

    ' =========================================================================
    '  VIGAS
    ' =========================================================================
    Private Sub EscribirSeccionVigas(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de las vigas a partir de la información recibida."))

        Dim vigas As List(Of cViga) = proyecto.Elementos.Vigas.Vigas
        If vigas Is Nothing Then vigas = New List(Of cViga)

        ' ── Flexión ──────────────────────────────────────────────────────────
        body.Append(Heading2("FLEXIÓN"))
        Dim noFlexion As New List(Of String())
        For Each v In vigas
            Dim nombreViga = ObtenerNombreViga(v)
            For Each f In v.Frames
                If f.RevisionFlexion Is Nothing OrElse f.RevisionFlexion.Count = 0 Then Continue For
                If Not (f.RefuerzoSuperior IsNot Nothing AndAlso f.RefuerzoSuperior.Any()) AndAlso
                   Not (f.RefuerzoInferior IsNot Nothing AndAlso f.RefuerzoInferior.Any()) Then Continue For

                ' Buscar peor resultado por zona
                Dim zonaIzq = f.RevisionFlexion.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Izquierda)
                Dim zonaCen = f.RevisionFlexion.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Centro)
                Dim zonaDer = f.RevisionFlexion.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Derecha)

                Dim failSupIzq = zonaIzq IsNot Nothing AndAlso zonaIzq.ResultadoActual IsNot Nothing AndAlso
                                 zonaIzq.ResultadoActual.AsReqSup > 0 AndAlso Not EsConforme(zonaIzq.ResultadoActual.RatioSup)
                Dim failSupDer = zonaDer IsNot Nothing AndAlso zonaDer.ResultadoActual IsNot Nothing AndAlso
                                 zonaDer.ResultadoActual.AsReqSup > 0 AndAlso Not EsConforme(zonaDer.ResultadoActual.RatioSup)
                Dim failInfCen = zonaCen IsNot Nothing AndAlso zonaCen.ResultadoActual IsNot Nothing AndAlso
                                 zonaCen.ResultadoActual.AsReqInf > 0 AndAlso Not EsConforme(zonaCen.ResultadoActual.RatioInf)

                If Not failSupIzq AndAlso Not failSupDer AndAlso Not failInfCen Then Continue For

                ' Peor caso superior
                Dim rIzq = If(zonaIzq?.ResultadoActual, New cResultadoFlexion())
                Dim rDer = If(zonaDer?.ResultadoActual, New cResultadoFlexion())
                Dim rCen = If(zonaCen?.ResultadoActual, New cResultadoFlexion())

                Dim worstSup As cResultadoFlexion
                If failSupIzq AndAlso failSupDer Then
                    worstSup = If(rIzq.RatioSup <= rDer.RatioSup, rIzq, rDer)
                ElseIf failSupIzq Then
                    worstSup = rIzq
                Else
                    worstSup = rDer
                End If

                ' Observación
                Dim obsParts As New List(Of String)
                If failSupIzq AndAlso failSupDer Then
                    obsParts.Add("En ambos extremos por M(-)")
                ElseIf failSupIzq Then
                    obsParts.Add("Extremo izquierdo por M(-)")
                ElseIf failSupDer Then
                    obsParts.Add("Extremo derecho por M(-)")
                End If
                If failInfCen Then obsParts.Add("zona central por M(+)")

                Dim eje = EjeFrame(f)
                noFlexion.Add({f.Story, nombreViga, eje,
                               FNum(worstSup.AsProvSup / 100.0), FNum(worstSup.AsReqSup / 100.0), FCD(worstSup.RatioSup),
                               FNum(rCen.AsProvInf / 100.0), FNum(rCen.AsReqInf / 100.0), FCD(rCen.RatioInf),
                               String.Join(" y ", obsParts)})
            Next
        Next
        If noFlexion.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a flexión de las vigas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexión de las vigas."))
            body.Append(TablaDatos(
                {"PISO", "VIGA", "EJE",
                 "As col. sup. (cm" & ChrW(178) & ")", "As req. sup. (cm" & ChrW(178) & ")", "C/D sup.",
                 "As col. inf. (cm" & ChrW(178) & ")", "As req. inf. (cm" & ChrW(178) & ")", "C/D inf.",
                 "OBSERVACIONES"},
                noFlexion, {5, 8}))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of String())
        For Each v In vigas
            Dim nombreViga = ObtenerNombreViga(v)
            For Each f In v.Frames
                If f.RevisionCortante Is Nothing OrElse f.RevisionCortante.Count = 0 Then Continue For
                If Not (f.RefuerzoSuperior IsNot Nothing AndAlso f.RefuerzoSuperior.Any()) AndAlso
                   Not (f.RefuerzoInferior IsNot Nothing AndAlso f.RefuerzoInferior.Any()) Then Continue For

                ' Excluir zonas que, aunque fallen el chequeo estándar, pasan el cortante plástico
                Dim zonasQ = f.RevisionCortante.Where(Function(z) z.Vu > 0 AndAlso z.Factor > 0 AndAlso
                                                        Not EsConforme(z.Factor) AndAlso
                                                        Not CumpleCortantePlastico(z.Posicion, f.CortantePlastico)).ToList()
                If zonasQ.Count = 0 Then Continue For

                ' Peor zona
                Dim peor = zonasQ.OrderBy(Function(z) z.Factor).First()

                ' Observación posicional
                Dim failIzq = zonasQ.Any(Function(z) z.Posicion = PosicionTramoViga.Izquierda)
                Dim failCen = zonasQ.Any(Function(z) z.Posicion = PosicionTramoViga.Centro)
                Dim failDer = zonasQ.Any(Function(z) z.Posicion = PosicionTramoViga.Derecha)

                Dim obs As String
                If failIzq AndAlso failDer Then
                    obs = If(failCen, "En ambos extremos y zona central", "En ambos extremos")
                ElseIf failIzq Then
                    obs = If(failCen, "Extremo izquierdo y zona central", "Extremo izquierdo")
                ElseIf failDer Then
                    obs = If(failCen, "Extremo derecho y zona central", "Extremo derecho")
                Else
                    obs = "Zona central"
                End If

                noCortante.Add({f.Story, nombreViga, EjeFrame(f),
                                FNum(peor.phiVn), FNum(peor.Vu), FCD(peor.Factor), obs})
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a cortante de las vigas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de las vigas."))
            body.Append(TablaDatos(
                {"PISO", "VIGA", "EJE", ChrW(966) & "Vn (kN)", "Vu (kN)", "C/D", "OBSERVACIONES"},
                noCortante, {5}))
        End If

        body.Append(Heading2("DEFLEXIONES"))
        body.Append(PendienteImplementar("el módulo de Vigas no calcula deflexiones inmediatas/diferidas ni el chequeo de elementos susceptibles de dañarse."))

        body.Append(Heading2("REQUISITOS NORMATIVOS"))
        body.Append(Heading2("Cuantías máximas"))
        body.Append(PendienteImplementar("no existe cálculo de cuantía máxima en el módulo de Vigas."))

        body.Append(Heading2("Separación horizontal entre barras longitudinales"))
        body.Append(PendienteImplementar("no existe verificación de separación libre entre barras longitudinales (NSR-10 C.7.6.1) en el módulo de Vigas."))
    End Sub

    Private Shared Function ObtenerNombreViga(v As cViga) As String
        Return If(String.IsNullOrWhiteSpace(v.NombrePlano), v.Nombre, v.NombrePlano)
    End Function

    ' =========================================================================
    '  LOSAS (viguetas de losa nervada — paneles macizos: pendiente)
    ' =========================================================================
    Private Sub EscribirSeccionLosas(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de las viguetas de losas nervadas a partir de la información recibida."))

        Dim nervios As List(Of cNervio) = proyecto.Elementos.Nervios.Elementos
        If nervios Is Nothing Then nervios = New List(Of cNervio)
        Dim nerviosCalculados = nervios.Where(Function(n) n.Frames IsNot Nothing AndAlso
            n.Frames.Any(Function(f) f.Ref_Modificado)).ToList()

        ' ── Flexión ──────────────────────────────────────────────────────────
        body.Append(Heading2("FLEXIÓN"))
        Dim noFlexion As New List(Of String())
        For Each n In nerviosCalculados
            Dim nombre = If(String.IsNullOrWhiteSpace(n.NombrePlano), n.Nombre, n.NombrePlano)
            For Each f In n.Frames
                If Not f.Ref_Modificado Then Continue For

                Dim failSupI = f.CD_M_Sup_I > 0 AndAlso Not EsConforme(f.CD_M_Sup_I)
                Dim failSupD = f.CD_M_Sup_D > 0 AndAlso Not EsConforme(f.CD_M_Sup_D)
                Dim failInfC = f.CD_M_Inf_C > 0 AndAlso Not EsConforme(f.CD_M_Inf_C)
                If Not failSupI AndAlso Not failSupD AndAlso Not failInfC Then Continue For

                ' Peor superior
                Dim cdSupI = If(f.CD_M_Sup_I > 0, f.CD_M_Sup_I, 99.0)
                Dim cdSupD = If(f.CD_M_Sup_D > 0, f.CD_M_Sup_D, 99.0)
                Dim asColSup As Double
                Dim asReqSup As Double
                Dim cdSup As Double
                If cdSupI <= cdSupD Then
                    asColSup = f.As_Prov_Sup_I
                    asReqSup = f.As_Req_Sup_I
                    cdSup = cdSupI
                Else
                    asColSup = f.As_Prov_Sup_D
                    asReqSup = f.As_Req_Sup_D
                    cdSup = cdSupD
                End If

                ' Observación
                Dim obsParts As New List(Of String)
                If failSupI AndAlso failSupD Then
                    obsParts.Add("En ambos extremos por M(-)")
                ElseIf failSupI Then
                    obsParts.Add("Extremo izquierdo por M(-)")
                ElseIf failSupD Then
                    obsParts.Add("Extremo derecho por M(-)")
                End If
                If failInfC Then obsParts.Add("zona central por M(+)")

                Dim eje = EjeNervio(f)
                noFlexion.Add({f.Story, nombre, eje,
                               FNum(asColSup), FNum(asReqSup), FCD(cdSup),
                               FNum(f.As_Prov_Inf_C), FNum(f.As_Req_Inf_C), FCD(f.CD_M_Inf_C),
                               String.Join(" y ", obsParts)})
            Next
        Next
        If noFlexion.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a flexión de las viguetas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexión de viguetas de losa nervada."))
            body.Append(TablaDatos(
                {"PISO", "NERVIO", "EJE",
                 "As col. sup. (cm" & ChrW(178) & ")", "As req. sup. (cm" & ChrW(178) & ")", "C/D sup.",
                 "As col. inf. (cm" & ChrW(178) & ")", "As req. inf. (cm" & ChrW(178) & ")", "C/D inf.",
                 "OBSERVACIONES"},
                noFlexion, {5, 8}))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of String())
        For Each n In nerviosCalculados
            Dim nombre = If(String.IsNullOrWhiteSpace(n.NombrePlano), n.Nombre, n.NombrePlano)
            For Each f In n.Frames
                If Not f.Ref_Modificado Then Continue For

                Dim failI = f.CD_Cortante_I > 0 AndAlso Not EsConforme(f.CD_Cortante_I)
                Dim failD = f.CD_Cortante_D > 0 AndAlso Not EsConforme(f.CD_Cortante_D)
                If Not failI AndAlso Not failD Then Continue For

                ' Calcular Ash colocado y requerido
                Dim d_m = f.H - f.Recubrimiento  ' m
                Dim fy_mpa = If(f.fy > 0, f.fy, 420.0)

                Dim AshColI As Double = 0
                Dim AshColD As Double = 0
                For Each zt In f.RefuerzoTransversal
                    Dim av = Funciones_00_Varias.AreaRefuerzo("#" & zt.NumeroBarra.ToString()) * zt.CantEstribos
                    If zt.Separacion > 0 Then
                        Dim ash = av / zt.Separacion
                        If zt.Posicion = PosicionTramoViga.Izquierda Then AshColI = ash
                        If zt.Posicion = PosicionTramoViga.Derecha Then AshColD = ash
                    End If
                Next

                ' Ash requerido: Vs_req / (fy * d) × 10 → cm²/m
                Dim AshReqI As Double = 0
                Dim AshReqD As Double = 0
                If f.Vu_I > 0 AndAlso d_m > 0 Then
                    Dim vsReqI = Math.Max(0, (f.Vu_I - f.PhiVc_I) / 0.75)
                    AshReqI = 10 * vsReqI / (fy_mpa * d_m)
                End If
                If f.Vu_D > 0 AndAlso d_m > 0 Then
                    Dim vsReqD = Math.Max(0, (f.Vu_D - f.PhiVc_D) / 0.75)
                    AshReqD = 10 * vsReqD / (fy_mpa * d_m)
                End If

                ' Un registro por lado que falla
                If failI Then
                    Dim cdI = If(f.CD_Cortante_I > 0, f.CD_Cortante_I, 0.0)
                    noCortante.Add({f.Story, nombre, EjeNervio(f) & " (I)",
                                    FNum(AshColI, 3), FNum(AshReqI, 3), FCD(cdI), "Extremo izquierdo"})
                End If
                If failD Then
                    Dim cdD = If(f.CD_Cortante_D > 0, f.CD_Cortante_D, 0.0)
                    noCortante.Add({f.Story, nombre, EjeNervio(f) & " (D)",
                                    FNum(AshColD, 3), FNum(AshReqD, 3), FCD(cdD), "Extremo derecho"})
                End If
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a cortante de las viguetas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de viguetas de losa nervada."))
            body.Append(TablaDatos(
                {"PISO", "NERVIO", "EJE", "Ash col. (cm" & ChrW(178) & "/m)", "Ash req. (cm" & ChrW(178) & "/m)", "C/D", "OBSERVACIONES"},
                noCortante, {5}))
        End If

        body.Append(Heading2("DEFLEXIONES"))
        body.Append(PendienteImplementar("el módulo de Nervios no calcula deflexiones inmediatas/diferidas."))

        body.Append(Heading2("PANELES DE LOSA MACIZA"))
        body.Append(PendienteImplementar(
            "el módulo de Losas macizas (Form_03_Losas / Proyecto_Losas) es hoy una calculadora aislada de un solo " &
            "panel, sin lista persistida dentro del proyecto ni cálculo de As requerido/colocado o C/D por panel — " &
            "no es posible listar paneles automáticamente en este reporte."))
    End Sub

    ' =========================================================================
    '  DETALLES EN PLANOS
    ' =========================================================================
    Private Sub EscribirDetallesPlanos(body As Body, detalles As List(Of String))
        If detalles Is Nothing OrElse detalles.Count = 0 Then
            body.Append(ParrafoNormal("No se registraron detalles pendientes en planos para este proyecto."))
            Return
        End If
        For Each linea In detalles
            If String.IsNullOrWhiteSpace(linea) Then Continue For
            body.Append(NuevoParrafoViñeta(linea.Trim()))
        Next
    End Sub

    Private Shared Function NuevoParrafoViñeta(texto As String) As Paragraph
        Return ParrafoNormal("•  " & texto)
    End Function

    Private Shared Function EjeFrame(f As cFrame) As String
        If Not String.IsNullOrWhiteSpace(f.EjeApoyo_I) AndAlso Not String.IsNullOrWhiteSpace(f.EjeApoyo_J) Then
            Return f.EjeApoyo_I & "-" & f.EjeApoyo_J
        End If
        If Not String.IsNullOrWhiteSpace(f.ObjectLabel) Then Return f.ObjectLabel
        Return f.ElementLabel
    End Function

    Private Shared Function EjeNervio(f As cFrameNervio) As String
        If Not String.IsNullOrWhiteSpace(f.EjeApoyo_I) AndAlso Not String.IsNullOrWhiteSpace(f.EjeApoyo_D) Then
            Return f.EjeApoyo_I & "-" & f.EjeApoyo_D
        End If
        Return If(f.ObjectLabel, f.ElementLabel)
    End Function

    ' =========================================================================
    '  Encabezado — sustituye el código de proyecto resaltado en amarillo ("xx")
    ' =========================================================================
    Private Shared Sub ActualizarCodigoEnEncabezado(wordDoc As WordprocessingDocument, codigo As String)
        If String.IsNullOrWhiteSpace(codigo) Then Return
        For Each headerPart In wordDoc.MainDocumentPart.HeaderParts
            For Each r In headerPart.Header.Descendants(Of Run)().ToList()
                Dim rPr = r.RunProperties
                If rPr Is Nothing OrElse rPr.Highlight Is Nothing OrElse rPr.Highlight.Val Is Nothing Then Continue For
                If rPr.Highlight.Val.Value <> HighlightColorValues.Yellow Then Continue For
                Dim t = r.GetFirstChild(Of Text)()
                If t IsNot Nothing AndAlso t.Text.Trim() = "xx" Then
                    t.Text = codigo
                    rPr.Highlight.Remove()
                End If
            Next
        Next
    End Sub

End Class
