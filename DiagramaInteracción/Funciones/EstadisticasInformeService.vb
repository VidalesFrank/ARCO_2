' ═══════════════════════════════════════════════════════════════════════════════
'  EstadisticasInformeService
'
'  Agrega los resultados de C/D por módulo para alimentar el Informe Técnico
'  ejecutivo (ver InformeTecnicoService). Granularidad del "elemento":
'
'    - Pilas    : una entrada por Elemento_Pila  (todas en piso "Cimentación")
'    - Columnas : una entrada por Tramo_Columna  (más informativo por piso)
'    - Vigas    : una entrada por cViga
'    - Nervios  : una entrada por cNervio
'
'  Veredicto único: C/D ≥ UMBRAL_CD (0.90). C/D = 0 → "sin dato" (gris en
'  planta, no cuenta como "no cumple"). Esta convención es la misma que usan
'  PilaService.MinChequeoEsfuerzos, ColumnaService.CDFlexoEnvolvente,
'  VigaService.PeorCDViga y NervioService.PeorCDNervio.
' ═══════════════════════════════════════════════════════════════════════════════

Public Class ElementoCritico
    Public Property Nombre As String
    Public Property Piso As String
    Public Property CD As Double
    Public Property Criterio As String       ' "Flexo-compresión", "Cortante", "Esfuerzos", etc.
End Class

Public Class PisoResumen
    Public Property Piso As String
    Public Property NCumple As Integer
    Public Property NNoCumple As Integer
    Public Property NSinDato As Integer

    Public ReadOnly Property NRevisados As Integer
        Get
            Return NCumple + NNoCumple
        End Get
    End Property

    Public ReadOnly Property PctCumple As Double
        Get
            If NRevisados = 0 Then Return 0
            Return CDbl(NCumple) / NRevisados * 100.0
        End Get
    End Property
End Class

Public Class ModuloResumen
    Public Property Modulo As String         ' "Pilas", "Columnas", "Vigas", "Nervios"
    Public Property NTotal As Integer        ' elementos considerados (= revisados + sin dato)
    Public Property NCumple As Integer
    Public Property NNoCumple As Integer
    Public Property NSinDato As Integer
    Public Property PorPiso As New List(Of PisoResumen)()
    Public Property Top10Criticos As New List(Of ElementoCritico)()

    Public ReadOnly Property NRevisados As Integer
        Get
            Return NCumple + NNoCumple
        End Get
    End Property

    ''' <summary>% cumplimiento sobre los que tienen cálculo. Si no hay revisados, 0.</summary>
    Public ReadOnly Property PctCumple As Double
        Get
            If NRevisados = 0 Then Return 0
            Return CDbl(NCumple) / NRevisados * 100.0
        End Get
    End Property

    ''' <summary>Semáforo global: True si el % de cumplimiento es ≥ 95% y hay al menos un revisado.</summary>
    Public ReadOnly Property CumpleGlobal As Boolean
        Get
            Return NRevisados > 0 AndAlso PctCumple >= 95.0
        End Get
    End Property
End Class

Public Class EstadisticasInformeService

    Private Const UMBRAL As Double = Funciones_00_Varias.UMBRAL_CD

    ''' <summary>Entrada estándar: genera el resumen del módulo pedido. Nombres válidos:
    ''' "Pilas", "Columnas", "Muros", "Vigas", "Nervios", "Zapatas". Si el módulo está vacío,
    ''' devuelve un ModuloResumen con NTotal = 0.</summary>
    Public Shared Function Resumen(proyecto As Proyecto, modulo As String) As ModuloResumen
        Dim r As New ModuloResumen With {.Modulo = modulo}
        If proyecto Is Nothing OrElse proyecto.Elementos Is Nothing Then Return r

        Select Case modulo
            Case "Pilas"     : LlenarPilas(proyecto, r)
            Case "Columnas"  : LlenarColumnas(proyecto, r)
            Case "Muros"     : LlenarMuros(proyecto, r)
            Case "Vigas"     : LlenarVigas(proyecto, r)
            Case "Nervios"   : LlenarNervios(proyecto, r)
            Case "Zapatas"   : LlenarZapatas(proyecto, r)
        End Select

        OrdenarPisos(r, proyecto)
        TomarTop10(r)
        Return r
    End Function

    ' =====================================================================
    ' Pilas — una entrada por Elemento_Pila; todas en piso "Cimentación".
    ' =====================================================================
    Private Shared Sub LlenarPilas(proyecto As Proyecto, r As ModuloResumen)
        Dim lista = proyecto.Elementos.Pilas?.ListaElementos
        If lista Is Nothing OrElse lista.Count = 0 Then Return

        Dim pisoCim As String = "Cimentación"
        Dim pr As New PisoResumen With {.Piso = pisoCim}

        For Each p In lista
            If p Is Nothing Then Continue For
            r.NTotal += 1
            Dim cd = PilaService.PeorCDGlobal(p)
            If cd <= 0 Then
                r.NSinDato += 1
                pr.NSinDato += 1
                Continue For
            End If
            If cd >= UMBRAL Then
                r.NCumple += 1
                pr.NCumple += 1
            Else
                r.NNoCumple += 1
                pr.NNoCumple += 1
            End If
            r.Top10Criticos.Add(New ElementoCritico With {
                .Nombre = If(p.Name_Elemento, p.Name_Label),
                .Piso = pisoCim,
                .CD = cd,
                .Criterio = CriterioPila(p)
            })
        Next

        r.PorPiso.Add(pr)
    End Sub

    Private Shared Function CriterioPila(p As Elemento_Pila) As String
        ' El criterio gobernante es el que empató al PeorCDGlobal. Como MinChequeoEsfuerzos,
        ' PeorCDSuelo, PeorCDCortante y PeorCDFlexo pueden empatarse, devolvemos el primero
        ' que iguale el peor valor (prioridad: esfuerzos > suelo > cortante > flexo).
        Dim peor = PilaService.PeorCDGlobal(p)
        If peor <= 0 Then Return ""
        If Math.Abs(PilaService.MinChequeoEsfuerzos(p) - peor) < 0.01 Then Return "Esf. en concreto"
        If Math.Abs(PilaService.PeorCDSuelo(p) - peor) < 0.01 Then Return "Esf. al suelo"
        If Math.Abs(PilaService.PeorCDCortante(p) - peor) < 0.01 Then Return "Cortante"
        If Math.Abs(PilaService.PeorCDFlexo(p) - peor) < 0.01 Then Return "Flexo-compresión"
        Return ""
    End Function

    ' =====================================================================
    ' Columnas — una entrada por Tramo_Columna.
    ' =====================================================================
    Private Shared Sub LlenarColumnas(proyecto As Proyecto, r As ModuloResumen)
        Dim cols = proyecto.Elementos.Columnas?.Lista_Columnas
        If cols Is Nothing OrElse cols.Count = 0 Then Return

        Dim porPiso As New Dictionary(Of String, PisoResumen)(StringComparer.OrdinalIgnoreCase)

        ' Granularidad: UNA entrada por COLUMNA (no por tramo). El dashboard
        ' premium y la vista del módulo cuentan columnas; antes el informe
        ' contaba tramos e inflaba el total ×N, dando totales que no cuadraban
        ' con lo que el usuario ve en el módulo.
        '
        ' Por piso: la columna cuenta una vez en cada piso donde tiene al
        ' menos un tramo. El C/D de la columna en ese piso = peor entre los
        ' tramos de esa columna en ese piso. El criterio del Top-10 se toma
        ' del tramo gobernante (el que da el peor C/D global de la columna).
        For Each c In cols
            If c Is Nothing OrElse c.Lista_Tramos_Columnas Is Nothing Then Continue For
            r.NTotal += 1

            ' CD global de la columna (peor entre todos sus tramos) para el veredicto global.
            Dim peorCDGlobal As Double = -1
            Dim tramoPeor As Tramo_Columna = Nothing
            For Each tr In c.Lista_Tramos_Columnas
                If tr Is Nothing Then Continue For
                Dim cdTr = ColumnaService.PeorCDGlobalTramo(tr)
                If cdTr > 0 AndAlso (peorCDGlobal < 0 OrElse cdTr < peorCDGlobal) Then
                    peorCDGlobal = cdTr
                    tramoPeor = tr
                End If
            Next

            If peorCDGlobal <= 0 Then
                r.NSinDato += 1
            ElseIf peorCDGlobal >= UMBRAL Then
                r.NCumple += 1
            Else
                r.NNoCumple += 1
            End If

            If peorCDGlobal > 0 AndAlso tramoPeor IsNot Nothing Then
                r.Top10Criticos.Add(New ElementoCritico With {
                    .Nombre = c.Name_Elemento,
                    .Piso = If(String.IsNullOrWhiteSpace(tramoPeor.Piso), "(Sin piso)", tramoPeor.Piso),
                    .CD = peorCDGlobal,
                    .Criterio = CriterioColumna(tramoPeor)
                })
            End If

            ' Agrupar tramos por piso; una entrada por columna-por-piso con el
            ' peor CD de esta columna EN ese piso.
            Dim gruposPorPiso = c.Lista_Tramos_Columnas.
                Where(Function(tr) tr IsNot Nothing).
                GroupBy(Function(tr) If(String.IsNullOrWhiteSpace(tr.Piso), "(Sin piso)", tr.Piso))
            For Each gp In gruposPorPiso
                Dim pr As PisoResumen = Nothing
                If Not porPiso.TryGetValue(gp.Key, pr) Then
                    pr = New PisoResumen With {.Piso = gp.Key}
                    porPiso(gp.Key) = pr
                End If
                Dim peorEnPiso As Double = -1
                For Each tr In gp
                    Dim cdTr = ColumnaService.PeorCDGlobalTramo(tr)
                    If cdTr > 0 AndAlso (peorEnPiso < 0 OrElse cdTr < peorEnPiso) Then peorEnPiso = cdTr
                Next
                If peorEnPiso <= 0 Then
                    pr.NSinDato += 1
                ElseIf peorEnPiso >= UMBRAL Then
                    pr.NCumple += 1
                Else
                    pr.NNoCumple += 1
                End If
            Next
        Next

        r.PorPiso.AddRange(porPiso.Values)
    End Sub

    Private Shared Function CriterioColumna(tr As Tramo_Columna) As String
        Dim flx = ColumnaService.CDFlexoEnvolvente(tr)
        Dim cor = ColumnaService.CDCortanteTramo(tr)
        If flx <= 0 AndAlso cor <= 0 Then Return ""
        If flx > 0 AndAlso cor > 0 Then
            Return If(flx <= cor, "Flexo-compresión", "Cortante")
        End If
        Return If(flx > 0, "Flexo-compresión", "Cortante")
    End Function

    ' =====================================================================
    ' Vigas — una entrada por cViga. Se EXCLUYEN las que no tienen cálculo
    ' porque en ARCO solo algunas vigas se revisan a flexión; contarlas como
    ' "sin dato" infla el total y confunde al cliente (los dejaríamos como
    ' pendientes cuando en realidad no están en el alcance de la revisión).
    ' =====================================================================
    Private Shared Sub LlenarVigas(proyecto As Proyecto, r As ModuloResumen)
        Dim vigas = proyecto.Elementos.Vigas?.Vigas
        If vigas Is Nothing OrElse vigas.Count = 0 Then Return

        Dim porPiso As New Dictionary(Of String, PisoResumen)(StringComparer.OrdinalIgnoreCase)

        For Each v In vigas
            If v Is Nothing Then Continue For
            If Not v.IncluirEnRevision Then Continue For
            Dim cd = VigaService.PeorCDViga(v)
            If cd <= 0 Then Continue For

            r.NTotal += 1
            Dim piso As String = If(String.IsNullOrWhiteSpace(v.Piso), "(Sin piso)", v.Piso)
            Dim pr As PisoResumen = Nothing
            If Not porPiso.TryGetValue(piso, pr) Then
                pr = New PisoResumen With {.Piso = piso}
                porPiso(piso) = pr
            End If

            If cd >= UMBRAL Then
                r.NCumple += 1
                pr.NCumple += 1
            Else
                r.NNoCumple += 1
                pr.NNoCumple += 1
            End If
            r.Top10Criticos.Add(New ElementoCritico With {
                .Nombre = NombreViga(v),
                .Piso = piso,
                .CD = cd,
                .Criterio = CriterioViga(v)
            })
        Next

        r.PorPiso.AddRange(porPiso.Values)
    End Sub

    Private Shared Function NombreViga(v As cViga) As String
        If Not String.IsNullOrWhiteSpace(v.NombrePlano) Then Return v.NombrePlano
        If Not String.IsNullOrWhiteSpace(v.Nombre) Then Return v.Nombre
        Return v.Name_Beam
    End Function

    Private Shared Function CriterioViga(v As cViga) As String
        Dim fx = VigaService.CDFlexionViga(v)
        Dim cr = VigaService.CDCortanteViga(v)
        If fx <= 0 AndAlso cr <= 0 Then Return ""
        If fx > 0 AndAlso cr > 0 Then Return If(fx <= cr, "Flexión", "Cortante")
        Return If(fx > 0, "Flexión", "Cortante")
    End Function

    ' =====================================================================
    ' Nervios — una entrada por cNervio. Como en Vigas: SOLO los nervios con
    ' cálculo (en nervios no se revisan todos los pisos ni todos los nervios).
    ' =====================================================================
    Private Shared Sub LlenarNervios(proyecto As Proyecto, r As ModuloResumen)
        Dim lista = proyecto.Elementos.Nervios?.Elementos
        If lista Is Nothing OrElse lista.Count = 0 Then Return

        Dim porPiso As New Dictionary(Of String, PisoResumen)(StringComparer.OrdinalIgnoreCase)

        For Each n In lista
            If n Is Nothing Then Continue For
            Dim cd = NervioService.PeorCDNervio(n)
            If cd <= 0 Then Continue For

            r.NTotal += 1
            Dim piso As String = If(String.IsNullOrWhiteSpace(n.Piso), "(Sin piso)", n.Piso)
            Dim pr As PisoResumen = Nothing
            If Not porPiso.TryGetValue(piso, pr) Then
                pr = New PisoResumen With {.Piso = piso}
                porPiso(piso) = pr
            End If

            If cd >= UMBRAL Then
                r.NCumple += 1
                pr.NCumple += 1
            Else
                r.NNoCumple += 1
                pr.NNoCumple += 1
            End If
            r.Top10Criticos.Add(New ElementoCritico With {
                .Nombre = If(Not String.IsNullOrWhiteSpace(n.NombrePlano), n.NombrePlano, n.Nombre),
                .Piso = piso,
                .CD = cd,
                .Criterio = CriterioNervio(n)
            })
        Next

        r.PorPiso.AddRange(porPiso.Values)
    End Sub

    Private Shared Function CriterioNervio(n As cNervio) As String
        Dim fx = NervioService.CDFlexionNervio(n)
        Dim cr = NervioService.CDCortanteNervio(n)
        If fx <= 0 AndAlso cr <= 0 Then Return ""
        If fx > 0 AndAlso cr > 0 Then Return If(fx <= cr, "Flexión", "Cortante")
        Return If(fx > 0, "Flexión", "Cortante")
    End Function

    ' =====================================================================
    ' Post-proceso
    ' =====================================================================
    Private Shared Sub OrdenarPisos(r As ModuloResumen, proyecto As Proyecto)
        ' Preferimos ordenar por Z real de proyecto.Pisos (CoorZ) — así "S1"
        ' queda abajo aunque alfabéticamente iría al final. Si un piso del
        ' módulo no está en proyecto.Pisos (modelos legacy o Excel sin lista),
        ' cae al OrdenPiso por regex. Siempre al final: pisos sin match (MaxValue).
        Dim zPorPiso As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        If proyecto IsNot Nothing AndAlso proyecto.Pisos IsNot Nothing Then
            For Each cp In proyecto.Pisos
                If cp Is Nothing OrElse String.IsNullOrWhiteSpace(cp.Nombre) Then Continue For
                zPorPiso(cp.Nombre.Trim()) = CDbl(cp.CoorZ)
            Next
        End If

        r.PorPiso = r.PorPiso.
            OrderBy(Function(pr) If(zPorPiso.ContainsKey(pr.Piso), 0, 1)).
            ThenBy(Function(pr)
                       Dim z As Double
                       Return If(zPorPiso.TryGetValue(pr.Piso, z), z, CDbl(OrdenPiso(pr.Piso)))
                   End Function).ToList()
    End Sub

    Private Shared Sub TomarTop10(r As ModuloResumen)
        r.Top10Criticos = r.Top10Criticos.OrderBy(Function(e) e.CD).Take(10).ToList()
    End Sub

    ''' <summary>Duplicado mínimo de ReporteRevisionService.OrdenPiso (que es Private).
    ''' Si esto se necesita en un tercer lugar, exponer allí y centralizar.</summary>
    Private Shared Function OrdenPiso(nombre As String) As Integer
        If String.IsNullOrWhiteSpace(nombre) Then Return Integer.MaxValue
        Dim n = nombre.Trim().ToUpperInvariant()
        If n.Contains("CIMENT") OrElse n.Contains("FUND") OrElse n = "BASE" Then Return -1000
        If n.Contains("CUBIERTA") OrElse n.Contains("TECHO") OrElse n.Contains("TERRAZA") OrElse n.Contains("AZOTEA") Then Return 900

        Dim mSot = System.Text.RegularExpressions.Regex.Match(n, "S[OÓ]?TANO?\s*(\d+)")
        If mSot.Success Then Return -CInt(mSot.Groups(1).Value)

        ' "S1", "S2", "SS1" (sótano abreviado) → sótano negativo. Debe ir ANTES del
        ' regex de "P + número" para que "S1" no caiga en el default (MaxValue).
        Dim mSotCorto = System.Text.RegularExpressions.Regex.Match(n, "^\s*S{1,2}\s*(\d+)\s*$")
        If mSotCorto.Success Then Return -CInt(mSotCorto.Groups(1).Value)

        Dim mPiso = System.Text.RegularExpressions.Regex.Match(n, "^\s*(?:PISO|PLANTA|STORY|NIVEL|P)\s*(\d+)")
        If mPiso.Success Then Return CInt(mPiso.Groups(1).Value)

        Dim mNum = System.Text.RegularExpressions.Regex.Match(n, "^\s*(\d+)\s*$")
        If mNum.Success Then Return CInt(mNum.Groups(1).Value)

        Return Integer.MaxValue
    End Function

    ' =====================================================================
    ' Muros — una entrada por Muro (peor sección de toda la altura).
    ' MurosResumenService.CalcularFila devuelve FilaMuro con FFlexMin, FCortMin,
    ' Cumple y los pisos críticos; esos son los mismos valores que usan los
    ' dashboards y el reporte ejecutivo de muros.
    ' =====================================================================
    Private Shared Sub LlenarMuros(proyecto As Proyecto, r As ModuloResumen)
        Dim lista = proyecto.Elementos.Muros?.Lista_Muros
        If lista Is Nothing OrElse lista.Count = 0 Then Return

        Dim pisos As New Dictionary(Of String, PisoResumen)(StringComparer.OrdinalIgnoreCase)

        For Each m In lista
            If m Is Nothing Then Continue For
            r.NTotal += 1

            Dim f = MurosResumenService.CalcularFila(m)
            Dim arr = {CDbl(f.FFlexMin), CDbl(f.FCortMin)}.Where(Function(v) v > 0).ToArray()
            Dim cd = If(arr.Length = 0, 0.0, arr.Min())
            Dim pisoKey = If(Not String.IsNullOrWhiteSpace(f.PisoCriticoFlex), f.PisoCriticoFlex,
                             If(Not String.IsNullOrWhiteSpace(f.PisoCriticoCort), f.PisoCriticoCort, "—"))

            If Not pisos.ContainsKey(pisoKey) Then pisos(pisoKey) = New PisoResumen With {.Piso = pisoKey}
            Dim pr = pisos(pisoKey)

            If cd <= 0 Then
                r.NSinDato += 1
                pr.NSinDato += 1
                Continue For
            End If
            If cd >= UMBRAL Then
                r.NCumple += 1
                pr.NCumple += 1
            Else
                r.NNoCumple += 1
                pr.NNoCumple += 1
            End If

            Dim criterio = If(f.FFlexMin > 0 AndAlso (f.FCortMin <= 0 OrElse f.FFlexMin <= f.FCortMin),
                              "Flexo-compresión", "Cortante")
            r.Top10Criticos.Add(New ElementoCritico With {
                .Nombre = If(Not String.IsNullOrWhiteSpace(f.Label), f.Label, m.Label),
                .Piso = pisoKey,
                .CD = cd,
                .Criterio = criterio & " · " & If(f.Direccion, "")
            })
        Next

        For Each p In pisos.Values
            r.PorPiso.Add(p)
        Next
    End Sub

    ' =====================================================================
    ' Zapatas — una entrada por cZapata (tipo); todas en piso "Cimentación".
    ' ZapataService.Resumir ya consolida el peor factor y el .Cumple.
    ' =====================================================================
    Private Shared Sub LlenarZapatas(proyecto As Proyecto, r As ModuloResumen)
        Dim lista = proyecto.Elementos.Zapatas?.Tipos
        If lista Is Nothing OrElse lista.Count = 0 Then Return

        Dim pisoCim = "Cimentación"
        Dim pr As New PisoResumen With {.Piso = pisoCim}

        For Each z In lista
            If z Is Nothing Then Continue For
            r.NTotal += 1

            Dim res = ZapataService.Resumir(z)
            If Not res.TieneResultados OrElse res.PeorFactor <= 0 Then
                r.NSinDato += 1
                pr.NSinDato += 1
                Continue For
            End If
            If res.Cumple Then
                r.NCumple += 1
                pr.NCumple += 1
            Else
                r.NNoCumple += 1
                pr.NNoCumple += 1
            End If
            r.Top10Criticos.Add(New ElementoCritico With {
                .Nombre = If(Not String.IsNullOrWhiteSpace(z.Nombre), z.Nombre, z.Label_joint),
                .Piso = pisoCim,
                .CD = res.PeorFactor,
                .Criterio = If(Not String.IsNullOrWhiteSpace(res.Revision), res.Revision, "Peor global")
            })
        Next

        r.PorPiso.Add(pr)
    End Sub

End Class
