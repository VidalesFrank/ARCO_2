Imports System.IO
Imports System.Drawing
Imports DocumentFormat.OpenXml.Packaging
Imports DocumentFormat.OpenXml.Wordprocessing

''' <summary>
''' Opciones del Informe Técnico ejecutivo — selección de módulos y datos de portada.
''' Para edificios aporticados, los 4 módulos son Pilas / Columnas / Vigas / Nervios.
''' </summary>
Public Class OpcionesInformeTecnico
    Public Property Fecha As Date = Date.Today
    Public Property CodigoProyecto As String = ""
    Public Property Cliente As String = ""
    Public Property Responsable As String = ""
    Public Property AsuntoProyecto As String = ""

    Public Property IncluirPilas As Boolean = True
    Public Property IncluirColumnas As Boolean = True
    Public Property IncluirMuros As Boolean = True
    Public Property IncluirVigas As Boolean = True
    Public Property IncluirNervios As Boolean = True
    Public Property IncluirZapatas As Boolean = True
End Class

''' <summary>
''' Genera el "Informe Técnico ejecutivo" (.docx) dirigido al cliente: portada,
''' resumen ejecutivo (1 fila por módulo), capítulos por módulo con planta coloreada
''' por C/D + torta cumple/no cumple + barras por piso + top-10 críticos.
'''
''' Reusa la misma plantilla corporativa que <see cref="ReporteRevisionService"/>
''' (My.Resources.PlantillaReporteRevision) para heredar los estilos EstrucMed.
''' </summary>
Public Class InformeTecnicoService

    Private Const UMBRAL As Double = Funciones_00_Varias.UMBRAL_CD

    ' Numeración de figuras y tablas: campos SEQ de Word (ver ReporteRevisionWordHelpers).
    ' Word los enumera y lista automáticamente en Referencias → Insertar índice.

    ' =========================================================================
    '  Punto de entrada
    ' =========================================================================
    Public Shared Sub Generar(proyecto As Proyecto, opciones As OpcionesInformeTecnico, rutaSalida As String)
        Dim servicio As New InformeTecnicoService()
        servicio.GenerarInterno(proyecto, opciones, rutaSalida)
    End Sub

    Private Sub GenerarInterno(proyecto As Proyecto, opciones As OpcionesInformeTecnico, rutaSalida As String)
        ' Mismo patrón que ReporteRevisionService: todo en memoria y volcado único al final para
        ' no chocar con Dropbox/OneDrive al re-abrir el docx.
        Dim plantilla = My.Resources.PlantillaReporteRevision
        Using ms As New MemoryStream()
            ms.Write(plantilla, 0, plantilla.Length)
            ms.Position = 0

            Using wordDoc As WordprocessingDocument = WordprocessingDocument.Open(ms, True)
                Dim mainPart = wordDoc.MainDocumentPart
                Dim body As Body = mainPart.Document.Body
                Dim sectPr As SectionProperties = body.Elements(Of SectionProperties)().FirstOrDefault()
                If sectPr IsNot Nothing Then sectPr.Remove()

                EscribirPortada(body, proyecto, opciones)
                Dim resumenes = ConstruirResumenes(proyecto, opciones)
                EscribirResumenEjecutivo(mainPart, body, resumenes)

                For Each resumen In resumenes
                    body.Append(ReporteRevisionWordHelpers.Heading1(TituloModulo(resumen.Modulo).ToUpper()))
                    EscribirCapitulo(mainPart, body, proyecto, resumen)
                Next

                If sectPr IsNot Nothing Then body.Append(sectPr)

                ReporteRevisionWordHelpers.ActualizarCodigoEnEncabezado(wordDoc, opciones.CodigoProyecto)
                ' Marca "actualizar campos al abrir" para que los SEQ de figuras y tablas
                ' se refresquen automáticamente la primera vez que Word abre el .docx.
                ReporteRevisionWordHelpers.ConfigurarActualizacionCampos(wordDoc)

                wordDoc.Save()
            End Using

            File.WriteAllBytes(rutaSalida, ms.ToArray())
        End Using
    End Sub

    ' =========================================================================
    '  Portada
    ' =========================================================================
    Private Sub EscribirPortada(body As Body, proyecto As Proyecto, opciones As OpcionesInformeTecnico)
        Dim info = proyecto.Info
        Dim asunto As String = If(String.IsNullOrWhiteSpace(opciones.AsuntoProyecto),
                                  If(info?.Nombre, ""), opciones.AsuntoProyecto)

        body.Append(ReporteRevisionWordHelpers.Heading1("INFORME TÉCNICO DE REVISIÓN"))

        Dim headers = {"Campo", "Valor"}
        Dim filas As New List(Of String())
        filas.Add({"Proyecto", asunto})
        If info IsNot Nothing Then
            Dim ubicacion = String.Join(", ", {info.Direccion, info.Ciudad, info.Departamento}.Where(
                Function(s) Not String.IsNullOrWhiteSpace(s)))
            If Not String.IsNullOrWhiteSpace(ubicacion) Then filas.Add({"Ubicación", ubicacion})
            If Not String.IsNullOrWhiteSpace(info.Designer) Then filas.Add({"Diseñador estructural", info.Designer})
            If info.NPisos > 0 Then filas.Add({"Número de pisos", info.NPisos.ToString()})
        End If
        If Not String.IsNullOrWhiteSpace(opciones.Cliente) Then filas.Add({"Cliente", opciones.Cliente})
        If Not String.IsNullOrWhiteSpace(opciones.Responsable) Then filas.Add({"Responsable", opciones.Responsable})
        filas.Add({"Fecha", opciones.Fecha.ToString("dd 'de' MMMM 'de' yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-CO"))})
        If Not String.IsNullOrWhiteSpace(opciones.CodigoProyecto) Then filas.Add({"Código", opciones.CodigoProyecto})

        body.Append(ReporteRevisionWordHelpers.TituloTablaEjecutivo("Datos generales del proyecto"))
        ' Columna 0 (campo) en negrita porque es la etiqueta tipo "Proyecto", "Ubicación".
        ' Las demás celdas en normal.
        body.Append(ReporteRevisionWordHelpers.TablaDatos(headers, filas, colsNegrita:={0}))
        body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))

        body.Append(ReporteRevisionWordHelpers.ParrafoNormal(
            "El presente documento es un informe ejecutivo de la revisión estructural del proyecto. " &
            "Para cada módulo revisado se presenta la planta estructural con elementos coloreados " &
            "según su cumplimiento, el resumen estadístico de cumplimiento, la distribución por piso y " &
            "los elementos más críticos. El criterio de cumplimiento es Capacidad/Demanda ≥ " &
            UMBRAL.ToString("0.00") & "; valores inferiores requieren revisión del diseñador."))
        body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
    End Sub

    ' =========================================================================
    '  Resumen ejecutivo (una fila por módulo)
    ' =========================================================================
    Friend Function ConstruirResumenes(proyecto As Proyecto, opciones As OpcionesInformeTecnico) As List(Of ModuloResumen)
        Dim lista As New List(Of ModuloResumen)
        If opciones.IncluirPilas Then AgregarSiTiene(lista, EstadisticasInformeService.Resumen(proyecto, "Pilas"))
        If opciones.IncluirColumnas Then AgregarSiTiene(lista, EstadisticasInformeService.Resumen(proyecto, "Columnas"))
        If opciones.IncluirMuros Then AgregarSiTiene(lista, EstadisticasInformeService.Resumen(proyecto, "Muros"))
        If opciones.IncluirVigas Then AgregarSiTiene(lista, EstadisticasInformeService.Resumen(proyecto, "Vigas"))
        If opciones.IncluirNervios Then AgregarSiTiene(lista, EstadisticasInformeService.Resumen(proyecto, "Nervios"))
        If opciones.IncluirZapatas Then AgregarSiTiene(lista, EstadisticasInformeService.Resumen(proyecto, "Zapatas"))
        Return lista
    End Function

    Private Shared Sub AgregarSiTiene(lista As List(Of ModuloResumen), r As ModuloResumen)
        If r IsNot Nothing AndAlso r.NTotal > 0 Then lista.Add(r)
    End Sub

    Private Sub EscribirResumenEjecutivo(mainPart As MainDocumentPart, body As Body, resumenes As List(Of ModuloResumen))
        body.Append(ReporteRevisionWordHelpers.Heading1("RESUMEN EJECUTIVO"))

        If resumenes.Count = 0 Then
            body.Append(ReporteRevisionWordHelpers.ParrafoNormal(
                "No hay elementos revisados en los módulos seleccionados."))
            Return
        End If

        ' Dashboard de KPIs al principio — una card por módulo con % y status.
        Dim pngDashboard = InformeChartsService.DashboardKPIs(resumenes, 980, 220)
        If pngDashboard IsNot Nothing Then
            body.Append(ReporteRevisionWordHelpers.ImagenCentrada(mainPart, pngDashboard, 17.0, 4.0))
            body.Append(ReporteRevisionWordHelpers.TituloFiguraEjecutivo(
                "Resumen de cumplimiento por módulo revisado."))
            body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
        End If

        Dim headers = {"Módulo", "Revisados", "Cumplen", "No cumplen", "% Cumple", "Estado"}
        Dim filas As New List(Of String())
        For Each r In resumenes
            filas.Add({
                TituloModulo(r.Modulo),
                r.NRevisados.ToString(),
                r.NCumple.ToString(),
                r.NNoCumple.ToString(),
                If(r.NRevisados > 0, r.PctCumple.ToString("0.0") & " %", "—"),
                EstadoTexto(r)
            })
        Next

        body.Append(ReporteRevisionWordHelpers.TituloTablaEjecutivo("Resumen general de cumplimiento por módulo"))
        ' Sin negrita en valores — solo el encabezado (que ya se maneja aparte en TablaDatos).
        body.Append(ReporteRevisionWordHelpers.TablaDatos(headers, filas, colsNegrita:=New Integer() {}))
        body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
    End Sub

    Friend Shared Function EstadoTexto(r As ModuloResumen) As String
        If r.NRevisados = 0 Then Return "Sin cálculo"
        If r.NNoCumple = 0 Then Return "Conforme"
        If r.PctCumple >= 95 Then Return "Aceptable"
        Return "Requiere revisión"
    End Function

    ' =========================================================================
    '  Capítulo por módulo
    ' =========================================================================
    Private Sub EscribirCapitulo(mainPart As MainDocumentPart, body As Body, proyecto As Proyecto, r As ModuloResumen)
        ' --- Intro ----------------------------------------------------------
        body.Append(ReporteRevisionWordHelpers.ParrafoNormal(
            IntroCapitulo(r)))

        ' --- Planta(s) coloreada(s) por C/D --------------------------------
        '    Pilas    → 1 planta de cimentación
        '    Columnas → 1 planta del primer piso (otros pisos van en nota textual)
        '    Vigas / Nervios → 1 planta "todos los pisos"
        For Each planta In RenderPlantasModulo(proyecto, r)
            Dim pngPlanta = InformeChartsService.BitmapAPNG(planta.Bmp)
            planta.Bmp.Dispose()
            If pngPlanta IsNot Nothing AndAlso pngPlanta.Length > 0 Then
                body.Append(ReporteRevisionWordHelpers.ImagenCentrada(mainPart, pngPlanta, 16.0, 12.0))
                body.Append(ReporteRevisionWordHelpers.TituloFiguraEjecutivo(planta.Caption))
                body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
            End If
        Next

        ' --- Nota de otros pisos con incumplimientos (solo Columnas) ---------
        '     La planta de Columnas sólo muestra el primer piso para no inflar el
        '     documento; esta nota complementa con el conteo por piso restante.
        If r.Modulo = "Columnas" Then
            Dim nota = NotaOtrosPisos(r)
            If Not String.IsNullOrEmpty(nota) Then
                body.Append(ReporteRevisionWordHelpers.ParrafoNormal("Nota: ", nota))
                body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
            End If
        End If

        ' --- Donut premium (ScottDashboardHelpers, mismo look que el dashboard) -
        Dim pngDonut = ScottDashboardHelpers.PngDonut(r.NCumple, r.NNoCumple, r.NSinDato,
                                                       DashboardTheme.TemaLight, 900, 600)
        If pngDonut IsNot Nothing Then
            body.Append(ReporteRevisionWordHelpers.ImagenCentrada(mainPart, pngDonut, 13.0, 8.6))
            body.Append(ReporteRevisionWordHelpers.TituloFiguraEjecutivo(
                "Distribución de elementos por veredicto de cumplimiento — " & TituloModulo(r.Modulo) & "."))
            body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
        End If

        ' --- Top-N peores: barras horizontales premium -----------------------
        If r.Top10Criticos IsNot Nothing AndAlso r.Top10Criticos.Count > 0 Then
            Dim itemsPr = r.Top10Criticos.Where(Function(e) e.CD > 0).
                                          Select(Function(e) New ItemCD(e.Nombre, CDbl(e.CD))).ToList()
            Dim pngTop = ScottDashboardHelpers.PngTopPeores(itemsPr, 10, DashboardTheme.TemaLight, width:=1100, height:=560)
            If pngTop IsNot Nothing Then
                body.Append(ReporteRevisionWordHelpers.ImagenCentrada(mainPart, pngTop, 16.0, 8.0))
                body.Append(ReporteRevisionWordHelpers.TituloFiguraEjecutivo(
                    "Elementos con menor C/D — vista premium del dashboard."))
                body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
            End If
        End If

        ' --- Barras por piso premium (solo si más de un piso tiene datos) ---
        If r.PorPiso IsNot Nothing AndAlso r.PorPiso.Count >= 2 Then
            Dim filas = r.PorPiso.Select(Function(p) New ScottDashboardHelpers.PisoFila With {
                                            .Piso = p.Piso, .Cumple = p.NCumple,
                                            .NoCumple = p.NNoCumple, .SinCalc = p.NSinDato}).ToList()
            Dim pngBarras = ScottDashboardHelpers.PngBarrasPorPiso(
                filas, DashboardTheme.TemaLight,
                "Distribución por piso — " & TituloModulo(r.Modulo), 1300, 580)
            If pngBarras IsNot Nothing Then
                body.Append(ReporteRevisionWordHelpers.ImagenCentrada(mainPart, pngBarras, 16.0, 7.5))
                body.Append(ReporteRevisionWordHelpers.TituloFiguraEjecutivo(
                    "Cantidad de elementos que cumplen y que no cumplen, discriminado por piso."))
                body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
            End If
        End If

        ' --- Top-10 críticos ------------------------------------------------
        If r.Top10Criticos IsNot Nothing AndAlso r.Top10Criticos.Count > 0 AndAlso r.NNoCumple > 0 Then
            Dim headers = {"#", "Elemento", "Piso", "C/D", "Criterio"}
            Dim filas As New List(Of String())
            Dim i As Integer = 1
            ' Mostrar solo elementos que NO cumplen (los que están debajo del umbral).
            ' Si cumple pero sale en el top-10 el cliente puede confundirse.
            Dim top = r.Top10Criticos.Where(Function(e) e.CD < UMBRAL).Take(10).ToList()
            If top.Count = 0 Then top = r.Top10Criticos.Take(10).ToList()
            For Each e In top
                filas.Add({i.ToString(), e.Nombre, e.Piso, e.CD.ToString("0.00"), e.Criterio})
                i += 1
            Next
            body.Append(ReporteRevisionWordHelpers.TituloTablaEjecutivo(
                $"Elementos más críticos — {TituloModulo(r.Modulo).ToLower()} ({top.Count} de menor C/D)."))
            body.Append(ReporteRevisionWordHelpers.TablaDatos(headers, filas, colsNegrita:=New Integer() {}))
            body.Append(ReporteRevisionWordHelpers.ParrafoNormal(""))
        End If
    End Sub

    ''' <summary>
    ''' Para Columnas: construye la nota con los pisos (distintos al primero
    ''' representado en la planta) que presentan incumplimientos. Devuelve "" si
    ''' no hay otros pisos problemáticos — en ese caso no se agrega la nota.
    ''' </summary>
    Friend Shared Function NotaOtrosPisos(r As ModuloResumen) As String
        If r.PorPiso Is Nothing OrElse r.PorPiso.Count <= 1 Then Return ""
        Dim pisoPrincipal = r.PorPiso.First().Piso
        Dim otros = r.PorPiso.Skip(1).Where(Function(p) p.NNoCumple > 0).ToList()
        If otros.Count = 0 Then
            Return $"La planta corresponde al piso {pisoPrincipal}. En los demás pisos revisados no se detectaron columnas por fuera del umbral."
        End If
        Dim detalle = String.Join(", ",
            otros.Select(Function(p)
                             Dim txt = $"{p.Piso} ({p.NNoCumple} columna" & If(p.NNoCumple = 1, "", "s") & ")"
                             Return txt
                         End Function))
        Return $"La planta corresponde al piso {pisoPrincipal}. En otros pisos también se detectaron columnas con C/D inferior al umbral: {detalle}. Ver el detalle gráfico en la distribución por piso y la tabla de elementos críticos."
    End Function

    Friend Shared Function IntroCapitulo(r As ModuloResumen) As String
        Dim mod_ = TituloModulo(r.Modulo).ToLower()
        If r.NRevisados = 0 Then
            Return $"No se encontraron {mod_} con resultados de C/D al momento de generar el informe."
        End If
        Dim partes As New List(Of String)
        partes.Add($"Se revisaron {r.NRevisados} {mod_}")
        partes.Add($"de los cuales {r.NCumple} cumplen con C/D ≥ {UMBRAL:0.00}")
        If r.NNoCumple > 0 Then partes.Add($"{r.NNoCumple} presentan C/D inferior al umbral y requieren revisión")
        If r.NSinDato > 0 Then partes.Add($"{r.NSinDato} quedaron sin dato")
        Dim texto As String = String.Join(", ", partes) & $" ({r.PctCumple:0.0} % de cumplimiento)."
        Return texto
    End Function

    Friend Shared Function TituloModulo(modulo As String) As String
        Select Case modulo
            Case "Pilas" : Return "Pilas"
            Case "Columnas" : Return "Columnas"
            Case "Muros" : Return "Muros estructurales"
            Case "Vigas" : Return "Vigas"
            Case "Nervios" : Return "Nervios"
            Case "Zapatas" : Return "Zapatas"
        End Select
        Return modulo
    End Function

    ' =========================================================================
    '  Render de plantas (delegado al Form_Planta_X correspondiente, headless)
    ' =========================================================================
    ''' <summary>
    ''' Devuelve el nombre del piso con MÁS columnas distintas (que tienen un tramo
    ''' en ese piso). En empate: el piso más bajo (sótano) que ya viene ordenado
    ''' por OrdenarPisos. Si no hay info de columnas, cae al primer piso del resumen.
    ''' </summary>
    Private Function PisoConMasColumnas(proyecto As Proyecto, r As ModuloResumen) As String
        Try
            Dim cols = proyecto?.Elementos?.Columnas?.Lista_Columnas
            If cols Is Nothing OrElse cols.Count = 0 Then
                Return If(r.PorPiso?.FirstOrDefault()?.Piso, "")
            End If
            Dim conteo As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For Each c In cols
                If c Is Nothing OrElse c.Lista_Tramos_Columnas Is Nothing Then Continue For
                Dim pisosElemento As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each tr In c.Lista_Tramos_Columnas
                    If tr IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(tr.Piso) Then
                        pisosElemento.Add(tr.Piso)
                    End If
                Next
                For Each p In pisosElemento
                    If conteo.ContainsKey(p) Then conteo(p) += 1 Else conteo(p) = 1
                Next
            Next
            If conteo.Count = 0 Then Return If(r.PorPiso?.FirstOrDefault()?.Piso, "")
            Dim maxN = conteo.Values.Max()
            ' Empate → el que esté primero en r.PorPiso (ya ordenado sótano→cubierta).
            If r.PorPiso IsNot Nothing Then
                For Each p In r.PorPiso
                    If conteo.ContainsKey(p.Piso) AndAlso conteo(p.Piso) = maxN Then Return p.Piso
                Next
            End If
            Return conteo.First(Function(kv) kv.Value = maxN).Key
        Catch ex As Exception
            Logger.Error(ex, "InformeTecnicoService.PisoConMasColumnas")
            Return If(r.PorPiso?.FirstOrDefault()?.Piso, "")
        End Try
    End Function

    Friend Class PlantaRender
        Public Property Bmp As Bitmap
        Public Property Caption As String
    End Class

    Friend Function RenderPlantasModulo(proyecto As Proyecto, r As ModuloResumen) As List(Of PlantaRender)
        Dim res As New List(Of PlantaRender)
        Try
            Dim gridlines = proyecto?.Elementos?.Grids?.GridLines
            Select Case r.Modulo
                Case "Pilas"
                    Using frm As New Form_Planta_Pilas() With {
                        .Proyecto = proyecto,
                        .GridLines = gridlines
                    }
                        Dim bmp = frm.RenderBitmapPlanta(1400, 1000, 0)
                        If bmp IsNot Nothing Then res.Add(New PlantaRender With {
                            .Bmp = bmp,
                            .Caption = "Planta de cimentación — pilas coloreadas por relación Capacidad/Demanda (peor global)."})
                    End Using

                Case "Columnas"
                    ' Elegimos el piso con MÁS columnas distintas: típicamente el primer
                    ' nivel bajo torre es el que concentra todas las columnas del edificio,
                    ' mientras que los pisos superiores pueden tener menos (las de borde
                    ' se cortan). Es la representación "más completa" para el reporte.
                    ' Si varios pisos empatan en conteo, prioriza el más bajo (sótano→cubierta
                    ' por EstadisticasInformeService.OrdenarPisos).
                    Dim pisoPrincipal = PisoConMasColumnas(proyecto, r)
                    If Not String.IsNullOrWhiteSpace(pisoPrincipal) Then
                        Dim bmp As Bitmap = Nothing
                        Using frm As New Form_Planta_Columnas() With {
                            .Proyecto = proyecto,
                            .GridLines = gridlines
                        }
                            bmp = frm.RenderBitmapPlanta(1400, 1000, pisoPrincipal, 0)
                        End Using
                        If bmp IsNot Nothing Then res.Add(New PlantaRender With {
                            .Bmp = bmp,
                            .Caption = $"Planta de columnas — piso {pisoPrincipal} (coloreado por C/D peor global)."})
                    End If

                Case "Vigas"
                    Dim diag = DiagramaServiceParaVigas()
                    Dim joints = JointsParaVigas(proyecto)
                    Dim todasVigas = proyecto.Elementos.Vigas?.Vigas?.
                                        Where(Function(v) v.IncluirEnRevision AndAlso VigaService.PeorCDViga(v) > 0).ToList()
                    If todasVigas IsNot Nothing AndAlso todasVigas.Count > 0 AndAlso joints IsNot Nothing Then
                        Dim grupos = PisosUnicosService.AgruparPisosVigas(proyecto)
                        If grupos Is Nothing OrElse grupos.Count = 0 Then
                            ' Fallback: no se pudo agrupar; una sola planta con todas superpuestas.
                            Dim bmp = diag.DibujarPlantaBitmap(1400, 1000, todasVigas, joints, gridlines, Nothing,
                                                                DiagramaService.ModoChequeoPlanta.PeorGlobal)
                            If bmp IsNot Nothing Then res.Add(New PlantaRender With {
                                .Bmp = bmp,
                                .Caption = $"Planta de vigas revisadas — {todasVigas.Count} elementos (coloreado por C/D peor global)."})
                        Else
                            For Each gr In grupos
                                Dim pisoRep = gr.PisoRepresentante
                                Dim vigasPiso = todasVigas.Where(Function(v) v.Piso = pisoRep).ToList()
                                If vigasPiso.Count = 0 Then Continue For
                                Dim bmp = diag.DibujarPlantaBitmap(1400, 1000, vigasPiso, joints, gridlines, pisoRep,
                                                                    DiagramaService.ModoChequeoPlanta.PeorGlobal)
                                If bmp IsNot Nothing Then res.Add(New PlantaRender With {
                                    .Bmp = bmp,
                                    .Caption = $"Planta de vigas — {PisosUnicosService.DescribirGrupo(gr)} ({vigasPiso.Count} vigas, C/D peor global)."})
                            Next
                        End If
                    End If

                Case "Nervios"
                    ' Pasamos TODOS los nervios del paño (revisados + no revisados).
                    ' Los no revisados se pintan en gris alpha 90 (ColorSinCalcular),
                    ' quedando como "sombra" para dar contexto estructural del paño
                    ' sin saturar visualmente. Los revisados quedan extruidos con su
                    ' Bw real y coloreados por C/D.
                    Dim todosNervios = proyecto.Elementos.Nervios?.Elementos?.ToList()
                    Dim joints = JointsParaNervios(proyecto)
                    If todosNervios IsNot Nothing AndAlso todosNervios.Count > 0 AndAlso joints IsNot Nothing Then
                        Dim grupos = PisosUnicosService.AgruparPisosNervios(proyecto)
                        If grupos Is Nothing OrElse grupos.Count = 0 Then
                            Dim bmp As Bitmap = Nothing
                            Using frm As New Form_PlantaInteractivaNervios() With {
                                .Nervios = todosNervios, .Joints = joints, .GridLines = gridlines}
                                bmp = frm.RenderBitmapPlanta(1400, 1000, Nothing, 0)
                            End Using
                            If bmp IsNot Nothing Then res.Add(New PlantaRender With {
                                .Bmp = bmp,
                                .Caption = $"Planta de nervios revisados — {todosNervios.Count} elementos (coloreado por C/D peor global)."})
                        Else
                            For Each gr In grupos
                                ' Elegir como representante el primer piso del grupo que tenga
                                ' nervios con refuerzo ingresado. Si el grupo entero está sin
                                ' calcular, saltarlo — evita plantas vacías en el informe.
                                Dim pisosConCalculo = gr.Pisos.
                                    Where(Function(p) todosNervios.Any(Function(n) n.Piso = p AndAlso
                                                                                    NervioService.PeorCDNervio(n) > 0)).ToList()
                                If pisosConCalculo.Count = 0 Then Continue For
                                Dim pisoRep = pisosConCalculo.First()
                                Dim nervPiso = todosNervios.Where(Function(n) n.Piso = pisoRep).ToList()
                                If nervPiso.Count = 0 Then Continue For
                                Dim bmp As Bitmap = Nothing
                                Using frm As New Form_PlantaInteractivaNervios() With {
                                    .Nervios = nervPiso, .Joints = joints, .GridLines = gridlines}
                                    bmp = frm.RenderBitmapPlanta(1400, 1000, pisoRep, 0)
                                End Using
                                If bmp IsNot Nothing Then res.Add(New PlantaRender With {
                                    .Bmp = bmp,
                                    .Caption = $"Planta de nervios — {PisosUnicosService.DescribirGrupo(gr)} ({nervPiso.Count} nervios, C/D peor global)."})
                            Next
                        End If
                    End If
            End Select
        Catch ex As Exception
            Logger.Error(ex, "InformeTecnicoService.RenderPlantasModulo", r.Modulo)
        End Try
        Return res
    End Function

    Private Shared Function DiagramaServiceParaVigas() As DiagramaService
        ' DiagramaService necesita un GeometryService en el constructor pero sólo lo
        ' consulta en operaciones de geometría que no aplican a DibujarPlanta.
        Return New DiagramaService(New GeometryService())
    End Function

    Private Shared Function JointsParaVigas(proyecto As Proyecto) As Dictionary(Of String, cJoint)
        Dim lista = proyecto?.Elementos?.Joints
        If lista Is Nothing Then Return Nothing
        Dim r As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
        For Each j In lista
            If j Is Nothing Then Continue For
            Dim key = If(Not String.IsNullOrWhiteSpace(j.ElementLabel), j.ElementLabel, j.ObjectLabel)
            If String.IsNullOrWhiteSpace(key) Then Continue For
            If Not r.ContainsKey(key) Then r(key) = j
        Next
        Return r
    End Function

    Private Shared Function JointsParaNervios(proyecto As Proyecto) As Dictionary(Of String, cJoint)
        ' Nervios tiene sus propios Joints (snapshot del import del módulo).
        Dim lista = proyecto?.Elementos?.Nervios?.Joints
        If lista Is Nothing OrElse lista.Count = 0 Then Return JointsParaVigas(proyecto)
        Dim r As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
        For Each j In lista
            If j Is Nothing Then Continue For
            Dim key = If(Not String.IsNullOrWhiteSpace(j.ElementLabel), j.ElementLabel, j.ObjectLabel)
            If String.IsNullOrWhiteSpace(key) Then Continue For
            If Not r.ContainsKey(key) Then r(key) = j
        Next
        Return r
    End Function

End Class
