Imports System.IO
Imports MigraDoc.DocumentObjectModel
Imports MigraDoc.DocumentObjectModel.Tables
Imports MigraDoc.Rendering
Imports GDI = System.Drawing

''' <summary>
''' Gemelo PDF del <see cref="InformeTecnicoService"/>. Produce exactamente el
''' mismo contenido (portada, resumen ejecutivo, capítulos por módulo con
''' planta/donut/barras/top-10) usando MigraDoc en vez de OpenXml, para que el
''' usuario pueda entregar el informe en .pdf cuando quiere algo "sellado" sin
''' que el cliente lo edite por accidente.
'''
''' Reutiliza <see cref="EstadisticasInformeService"/>, <see cref="InformeChartsService"/>
''' y <see cref="InformeTecnicoService.RenderPlantasModulo"/> — no duplica cálculo
''' ni la generación de PNGs; sólo la presentación.
''' </summary>
Public Class InformeTecnicoPdfService

    Private Const UMBRAL As Double = Funciones_00_Varias.UMBRAL_CD

    ' Paleta coherente con los dashboards premium light-mode.
    Private Shared ReadOnly ColAcento As GDI.Color = GDI.Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly ColAcentoBarra As GDI.Color = GDI.Color.FromArgb(3, 105, 161)
    Private Shared ReadOnly ColTexto As GDI.Color = GDI.Color.FromArgb(31, 41, 55)
    Private Shared ReadOnly ColTextoTenue As GDI.Color = GDI.Color.FromArgb(107, 114, 128)
    Private Shared ReadOnly ColSeparador As GDI.Color = GDI.Color.FromArgb(209, 213, 219)
    Private Shared ReadOnly ColVerde As GDI.Color = GDI.Color.FromArgb(22, 163, 74)
    Private Shared ReadOnly ColAmbar As GDI.Color = GDI.Color.FromArgb(234, 88, 12)
    Private Shared ReadOnly ColRojo As GDI.Color = GDI.Color.FromArgb(220, 38, 38)

    Private _tempFiles As New List(Of String)

    ' =========================================================================
    '  Punto de entrada
    ' =========================================================================
    Public Shared Sub Generar(proyecto As Proyecto, opciones As OpcionesInformeTecnico, rutaSalida As String)
        Dim servicio As New InformeTecnicoPdfService()
        Try
            servicio.GenerarInterno(proyecto, opciones, rutaSalida)
        Finally
            servicio.LimpiarTemporales()
        End Try
    End Sub

    Private Sub GenerarInterno(proyecto As Proyecto, opciones As OpcionesInformeTecnico, rutaSalida As String)
        Dim doc As New Document()
        ConfigurarEstilos(doc)
        Dim section = doc.AddSection()
        ConfigurarMargenes(section)

        EscribirPortada(section, proyecto, opciones)

        Dim serviceDocx As New InformeTecnicoService()
        Dim resumenes = serviceDocx.ConstruirResumenes(proyecto, opciones)
        EscribirResumenEjecutivo(section, resumenes)

        For Each r In resumenes
            section.AddPageBreak()
            Heading(section, InformeTecnicoService.TituloModulo(r.Modulo).ToUpper(), 1)
            EscribirCapitulo(section, proyecto, r, serviceDocx)
        Next

        Dim renderer As New PdfDocumentRenderer(unicode:=True)
        renderer.Document = doc
        renderer.RenderDocument()
        renderer.PdfDocument.Save(rutaSalida)
    End Sub

    ' =========================================================================
    '  Estilos globales
    ' =========================================================================
    Private Sub ConfigurarEstilos(doc As Document)
        doc.Info.Title = "Informe Técnico Ejecutivo"
        doc.Info.Author = "ARCO · EstrucMed Ingeniería Especializada"
        doc.Info.Subject = "Revisión estructural"

        Dim normal = doc.Styles("Normal")
        normal.Font.Name = "Segoe UI"
        normal.Font.Size = 10
        normal.Font.Color = AMc(ColTexto)
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(4)

        Dim h1 = doc.Styles("Heading1")
        h1.Font.Name = "Segoe UI Semibold"
        h1.Font.Size = 16
        h1.Font.Color = AMc(ColAcento)
        h1.ParagraphFormat.SpaceBefore = Unit.FromPoint(12)
        h1.ParagraphFormat.SpaceAfter = Unit.FromPoint(8)
        h1.ParagraphFormat.KeepWithNext = True

        Dim h2 = doc.Styles("Heading2")
        h2.Font.Name = "Segoe UI Semibold"
        h2.Font.Size = 12
        h2.Font.Color = AMc(ColAcento)
        h2.ParagraphFormat.SpaceBefore = Unit.FromPoint(8)
        h2.ParagraphFormat.SpaceAfter = Unit.FromPoint(4)

        Dim captionStyle = doc.AddStyle("Figura", "Normal")
        captionStyle.Font.Size = 9
        captionStyle.Font.Italic = True
        captionStyle.Font.Color = AMc(ColTextoTenue)
        captionStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center
        captionStyle.ParagraphFormat.SpaceAfter = Unit.FromPoint(8)
    End Sub

    Private Sub ConfigurarMargenes(section As Section)
        section.PageSetup.PageFormat = PageFormat.A4
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2.2)
        section.PageSetup.RightMargin = Unit.FromCentimeter(2.2)
        section.PageSetup.TopMargin = Unit.FromCentimeter(2.0)
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2.0)
    End Sub

    ' =========================================================================
    '  Portada
    ' =========================================================================
    Private Sub EscribirPortada(section As Section, proyecto As Proyecto, opciones As OpcionesInformeTecnico)
        Dim info = proyecto.Info
        Dim asunto As String = If(String.IsNullOrWhiteSpace(opciones.AsuntoProyecto),
                                  If(info?.Nombre, ""), opciones.AsuntoProyecto)

        Heading(section, "INFORME TÉCNICO DE REVISIÓN", 1)

        Dim datos As New List(Of Tuple(Of String, String))
        datos.Add(Tuple.Create("Proyecto", asunto))
        If info IsNot Nothing Then
            Dim ubicacion = String.Join(", ", {info.Direccion, info.Ciudad, info.Departamento}.
                                                Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
            If Not String.IsNullOrWhiteSpace(ubicacion) Then datos.Add(Tuple.Create("Ubicación", ubicacion))
            If Not String.IsNullOrWhiteSpace(info.Designer) Then datos.Add(Tuple.Create("Diseñador estructural", info.Designer))
            If info.NPisos > 0 Then datos.Add(Tuple.Create("Número de pisos", info.NPisos.ToString()))
        End If
        If Not String.IsNullOrWhiteSpace(opciones.Cliente) Then datos.Add(Tuple.Create("Cliente", opciones.Cliente))
        If Not String.IsNullOrWhiteSpace(opciones.Responsable) Then datos.Add(Tuple.Create("Responsable", opciones.Responsable))
        datos.Add(Tuple.Create("Fecha", opciones.Fecha.ToString("dd 'de' MMMM 'de' yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-CO"))))
        If Not String.IsNullOrWhiteSpace(opciones.CodigoProyecto) Then datos.Add(Tuple.Create("Código", opciones.CodigoProyecto))

        Heading(section, "Datos generales del proyecto", 2)
        AgregarTablaDosColumnas(section, datos)

        Parrafo(section,
            "El presente documento es un informe ejecutivo de la revisión estructural del proyecto. " &
            "Para cada módulo revisado se presenta la planta estructural con elementos coloreados " &
            "según su cumplimiento, el resumen estadístico de cumplimiento, la distribución por piso y " &
            "los elementos más críticos. El criterio de cumplimiento es Capacidad/Demanda ≥ " &
            UMBRAL.ToString("0.00") & "; valores inferiores requieren revisión del diseñador.")
    End Sub

    ' =========================================================================
    '  Resumen ejecutivo
    ' =========================================================================
    Private Sub EscribirResumenEjecutivo(section As Section, resumenes As List(Of ModuloResumen))
        section.AddPageBreak()
        Heading(section, "RESUMEN EJECUTIVO", 1)

        If resumenes.Count = 0 Then
            Parrafo(section, "No hay elementos revisados en los módulos seleccionados.")
            Return
        End If

        Dim pngDashboard = InformeChartsService.DashboardKPIs(resumenes, 980, 220)
        If pngDashboard IsNot Nothing Then
            AgregarImagen(section, pngDashboard, 16.5)
            PieFigura(section, "Resumen de cumplimiento por módulo revisado.")
        End If

        Heading(section, "Resumen general de cumplimiento por módulo", 2)
        Dim headers = {"Módulo", "Revisados", "Cumplen", "No cumplen", "% Cumple", "Estado"}
        Dim filas As New List(Of String())
        For Each r In resumenes
            filas.Add({
                InformeTecnicoService.TituloModulo(r.Modulo),
                r.NRevisados.ToString(),
                r.NCumple.ToString(),
                r.NNoCumple.ToString(),
                If(r.NRevisados > 0, r.PctCumple.ToString("0.0") & " %", "—"),
                InformeTecnicoService.EstadoTexto(r)})
        Next
        AgregarTablaDatos(section, headers, filas)
    End Sub

    ' =========================================================================
    '  Capítulo por módulo
    ' =========================================================================
    Private Sub EscribirCapitulo(section As Section, proyecto As Proyecto, r As ModuloResumen, serviceDocx As InformeTecnicoService)
        Parrafo(section, InformeTecnicoService.IntroCapitulo(r))

        ' --- Planta(s) ------------------------------------------------------
        For Each planta In serviceDocx.RenderPlantasModulo(proyecto, r)
            Dim png = InformeChartsService.BitmapAPNG(planta.Bmp)
            planta.Bmp.Dispose()
            If png IsNot Nothing AndAlso png.Length > 0 Then
                AgregarImagen(section, png, 15.5)
                PieFigura(section, planta.Caption)
            End If
        Next

        ' --- Nota de otros pisos (solo Columnas) ----------------------------
        If r.Modulo = "Columnas" Then
            Dim nota = InformeTecnicoService.NotaOtrosPisos(r)
            If Not String.IsNullOrEmpty(nota) Then
                Dim p = section.AddParagraph()
                p.AddFormattedText("Nota: ", TextFormat.Bold)
                p.AddText(nota)
            End If
        End If

        ' --- Donut premium (ScottDashboardHelpers, mismo look que el dashboard en pantalla)
        Dim pngDonut = ScottDashboardHelpers.PngDonut(r.NCumple, r.NNoCumple, r.NSinDato,
                                                       DashboardTheme.TemaLight, 900, 600)
        If pngDonut IsNot Nothing Then
            AgregarImagen(section, pngDonut, 12.0)
            PieFigura(section, "Distribución de elementos por veredicto de cumplimiento — " & InformeTecnicoService.TituloModulo(r.Modulo) & ".")
        End If

        ' --- Top-N peores: barras horizontales premium -----------------------
        If r.Top10Criticos IsNot Nothing AndAlso r.Top10Criticos.Count > 0 Then
            Dim items = r.Top10Criticos.Where(Function(e) e.CD > 0).
                                        Select(Function(e) New ItemCD(e.Nombre, CDbl(e.CD))).ToList()
            Dim pngTop = ScottDashboardHelpers.PngTopPeores(items, 10, DashboardTheme.TemaLight, width:=1100, height:=560)
            If pngTop IsNot Nothing Then
                AgregarImagen(section, pngTop, 15.5)
                PieFigura(section, "Elementos con menor C/D — vista premium del dashboard.")
            End If
        End If

        ' --- Barras por piso premium ---------------------------------------
        If r.PorPiso IsNot Nothing AndAlso r.PorPiso.Count >= 2 Then
            Dim filas = r.PorPiso.Select(Function(p) New ScottDashboardHelpers.PisoFila With {
                                            .Piso = p.Piso, .Cumple = p.NCumple,
                                            .NoCumple = p.NNoCumple, .SinCalc = p.NSinDato}).ToList()
            Dim pngBarras = ScottDashboardHelpers.PngBarrasPorPiso(
                filas, DashboardTheme.TemaLight,
                "Distribución por piso — " & InformeTecnicoService.TituloModulo(r.Modulo), 1300, 580)
            If pngBarras IsNot Nothing Then
                AgregarImagen(section, pngBarras, 15.5)
                PieFigura(section, "Cantidad de elementos que cumplen y que no cumplen, discriminado por piso.")
            End If
        End If

        ' --- Top-10 críticos ------------------------------------------------
        If r.Top10Criticos IsNot Nothing AndAlso r.Top10Criticos.Count > 0 AndAlso r.NNoCumple > 0 Then
            Dim top = r.Top10Criticos.Where(Function(e) e.CD < UMBRAL).Take(10).ToList()
            If top.Count = 0 Then top = r.Top10Criticos.Take(10).ToList()
            Heading(section, $"Elementos más críticos — {InformeTecnicoService.TituloModulo(r.Modulo).ToLower()} ({top.Count} de menor C/D)", 2)
            Dim headers = {"#", "Elemento", "Piso", "C/D", "Criterio"}
            Dim filas As New List(Of String())
            Dim i As Integer = 1
            For Each e In top
                filas.Add({i.ToString(), e.Nombre, e.Piso, e.CD.ToString("0.00"), e.Criterio})
                i += 1
            Next
            AgregarTablaDatos(section, headers, filas)
        End If
    End Sub

    ' =========================================================================
    '  Primitivas MigraDoc
    ' =========================================================================
    Private Sub Heading(section As Section, texto As String, nivel As Integer)
        Dim p As Paragraph = section.AddParagraph(texto)
        p.Style = If(nivel = 1, "Heading1", "Heading2")
    End Sub

    Private Sub Parrafo(section As Section, texto As String)
        Dim p As Paragraph = section.AddParagraph(texto)
        p.Format.Alignment = ParagraphAlignment.Justify
    End Sub

    Private Sub PieFigura(section As Section, texto As String)
        Dim p As Paragraph = section.AddParagraph(texto)
        p.Style = "Figura"
    End Sub

    Private Sub AgregarTablaDosColumnas(section As Section, filas As List(Of Tuple(Of String, String)))
        Dim t = section.AddTable()
        t.Borders.Width = 0.3
        t.Borders.Color = AMc(ColSeparador)
        t.AddColumn(Unit.FromCentimeter(5.0))
        t.AddColumn(Unit.FromCentimeter(11.5))

        For Each f In filas
            Dim row = t.AddRow()
            row.Shading.Color = AMc(GDI.Color.FromArgb(248, 250, 252))
            Dim cEt = row.Cells(0).AddParagraph(f.Item1)
            cEt.Format.Font.Bold = True
            cEt.Format.Font.Color = AMc(ColAcento)
            row.Cells(1).AddParagraph(f.Item2)
        Next

        Dim pSep = section.AddParagraph()
        pSep.Format.SpaceAfter = Unit.FromPoint(6)
    End Sub

    Private Sub AgregarTablaDatos(section As Section, headers As String(), filas As List(Of String()))
        Dim t = section.AddTable()
        t.Borders.Width = 0.3
        t.Borders.Color = AMc(ColSeparador)
        t.LeftPadding = Unit.FromPoint(3)
        t.RightPadding = Unit.FromPoint(3)
        t.TopPadding = Unit.FromPoint(2)
        t.BottomPadding = Unit.FromPoint(2)

        Dim totalW As Double = 16.5
        Dim colW As Double = totalW / headers.Length
        For i = 0 To headers.Length - 1
            t.AddColumn(Unit.FromCentimeter(colW))
        Next

        Dim rowH = t.AddRow()
        rowH.HeadingFormat = True
        rowH.Shading.Color = AMc(ColAcento)
        For i = 0 To headers.Length - 1
            Dim p = rowH.Cells(i).AddParagraph(headers(i))
            p.Format.Font.Bold = True
            p.Format.Font.Color = AMc(GDI.Color.White)
            p.Format.Alignment = ParagraphAlignment.Center
        Next

        Dim alt = False
        For Each f In filas
            Dim row = t.AddRow()
            If alt Then row.Shading.Color = AMc(GDI.Color.FromArgb(248, 250, 252))
            alt = Not alt
            For i = 0 To Math.Min(f.Length, headers.Length) - 1
                row.Cells(i).AddParagraph(If(f(i), ""))
            Next
        Next

        Dim pSep = section.AddParagraph()
        pSep.Format.SpaceAfter = Unit.FromPoint(6)
    End Sub

    ''' <summary>
    ''' MigraDoc 1.50 requiere ruta en disco para AddImage. Escribimos un .png
    ''' temporal con nombre único; se borra al terminar.
    ''' </summary>
    Private Sub AgregarImagen(section As Section, pngBytes As Byte(), anchoCm As Double)
        If pngBytes Is Nothing OrElse pngBytes.Length = 0 Then Return
        Dim ruta = Path.Combine(Path.GetTempPath(), "arco_pdf_" & Guid.NewGuid().ToString("N") & ".png")
        File.WriteAllBytes(ruta, pngBytes)
        _tempFiles.Add(ruta)

        Dim p = section.AddParagraph()
        p.Format.Alignment = ParagraphAlignment.Center
        Dim img = p.AddImage(ruta)
        img.LockAspectRatio = True
        img.Width = Unit.FromCentimeter(anchoCm)
    End Sub

    Private Sub LimpiarTemporales()
        For Each r In _tempFiles
            Try
                If File.Exists(r) Then File.Delete(r)
            Catch
                ' ignorar: nombres únicos, se limpiarán por el SO si quedan huérfanos
            End Try
        Next
        _tempFiles.Clear()
    End Sub

    ''' <summary>GDI Color → MigraDoc Color.</summary>
    Private Shared Function AMc(c As GDI.Color) As MigraDoc.DocumentObjectModel.Color
        Return New MigraDoc.DocumentObjectModel.Color(c.R, c.G, c.B)
    End Function

End Class
