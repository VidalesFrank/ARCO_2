Imports DocumentFormat.OpenXml
Imports DocumentFormat.OpenXml.Wordprocessing
Imports System.IO
Imports DocumentFormat.OpenXml.Packaging
Imports A = DocumentFormat.OpenXml.Drawing
Imports PIC = DocumentFormat.OpenXml.Drawing.Pictures
Imports DW = DocumentFormat.OpenXml.Drawing.Wordprocessing

''' <summary>
''' Helpers de bajo nivel para construir el Reporte de Revisión (.docx) con DocumentFormat.OpenXml,
''' replicando exactamente la receta de estilos extraída de la plantilla EstrucMed de referencia
''' (FormatosRevisión\P53844_C008_ReporteRevisión_E1A.docx): Arial 12pt justificado, encabezados
''' "Ttulo1"/"Ttulo2", tabla "Tabladelista3" con encabezado gris #595959 y texto blanco 9pt.
''' </summary>
Public Module ReporteRevisionWordHelpers

    Private Const FUENTE As String = "Arial"
    Private Const TAM_NORMAL As String = "24"      ' 12pt (en medios puntos)
    Private Const TAM_TABLA As String = "18"        ' 9pt
    Private Const TAM_TITULO As String = "20"       ' 10pt (estilos Tabla/Descripcin)
    Private Const COLOR_ENCABEZADO_TABLA As String = "595959"
    Private Const COLOR_BLANCO As String = "FFFFFF"

    ' ── Runs ──────────────────────────────────────────────────────────────────
    Public Function CrearRun(texto As String,
                              Optional negrita As Boolean = False,
                              Optional cursiva As Boolean = False,
                              Optional subrayado As Boolean = False,
                              Optional tamanoMedioPunto As String = TAM_NORMAL,
                              Optional colorHex As String = Nothing) As Run

        Dim rPr As New RunProperties()
        rPr.Append(New RunFonts() With {.Ascii = FUENTE, .HighAnsi = FUENTE, .ComplexScript = FUENTE})
        If negrita Then rPr.Append(New Bold())
        If cursiva Then rPr.Append(New Italic())
        If subrayado Then rPr.Append(New Underline() With {.Val = UnderlineValues.Single})
        If colorHex IsNot Nothing Then rPr.Append(New Color() With {.Val = colorHex})
        rPr.Append(New FontSize() With {.Val = tamanoMedioPunto})
        rPr.Append(New FontSizeComplexScript() With {.Val = tamanoMedioPunto})

        Dim r As New Run(rPr)
        r.Append(New Text(texto) With {.Space = SpaceProcessingModeValues.Preserve})
        Return r
    End Function

    ' ── Párrafos ──────────────────────────────────────────────────────────────
    Private Function NuevoParrafo(styleId As String, ParamArray runs() As Run) As Paragraph
        Dim p As New Paragraph()
        If styleId IsNot Nothing Then
            p.Append(New ParagraphProperties(New ParagraphStyleId() With {.Val = styleId}))
        End If
        For Each r In runs
            p.Append(r)
        Next
        Return p
    End Function

    Public Function Heading1(texto As String) As Paragraph
        Return NuevoParrafo("Ttulo1", CrearRun(texto, negrita:=True, subrayado:=True))
    End Function

    Public Function Heading2(texto As String) As Paragraph
        Return NuevoParrafo("Ttulo2", CrearRun(texto))
    End Function

    Public Function ParrafoNormal(texto As String) As Paragraph
        Return NuevoParrafo(Nothing, CrearRun(texto))
    End Function

    Public Function ParrafoNormal(negrita1 As String, resto As String) As Paragraph
        Return NuevoParrafo(Nothing, CrearRun(negrita1, negrita:=True), CrearRun(resto))
    End Function

    ''' <summary>Párrafo estilo "Sinespaciado" (No Spacing) con interlineado 1.5, usado para el
    ''' texto fijo "No se tienen anotaciones…" cuando ningún elemento incumple la verificación.</summary>
    Public Function ParrafoSinAnotaciones(texto As String) As Paragraph
        Dim p As New Paragraph()
        Dim pPr As New ParagraphProperties()
        pPr.Append(New ParagraphStyleId() With {.Val = "Sinespaciado"})
        pPr.Append(New SpacingBetweenLines() With {.Line = "360", .LineRule = LineSpacingRuleValues.Auto})
        p.Append(pPr)
        p.Append(CrearRun(texto))
        Return p
    End Function

    ''' <summary>Nota en cursiva para las piezas de cálculo que aún no existen en ARCO.</summary>
    Public Function PendienteImplementar(texto As String) As Paragraph
        Return NuevoParrafo(Nothing, CrearRun("Pendiente de implementar en ARCO: " & texto, cursiva:=True))
    End Function

    ' ── Títulos de tabla ("Tabla N.<tab>Descripción"), estilo "Tabla" ───────────
    Public Function TituloTabla(numero As Integer, descripcion As String) As Paragraph
        Return NuevoParrafo("Tabla",
            CrearRun("Tabla " & numero & "." & vbTab, negrita:=True, tamanoMedioPunto:=TAM_TITULO),
            CrearRun(descripcion, negrita:=True, tamanoMedioPunto:=TAM_TITULO))
    End Function

    ' ── Tabla de datos (estilo "Tabladelista3", encabezado gris #595959) ───────
    ''' <summary>
    ''' Construye una tabla con la receta visual exacta de la plantilla de referencia:
    ''' encabezado con fondo #595959 y texto blanco 9pt centrado sin negrita; filas de datos
    ''' 9pt centradas, con las columnas indicadas en negrita (por defecto, la última = C/D).
    ''' </summary>
    Public Function TablaDatos(headers() As String, filas As List(Of String()), Optional colsNegrita As Integer() = Nothing) As Table
        If colsNegrita Is Nothing Then colsNegrita = {headers.Length - 1}

        Dim nCols As Integer = headers.Length
        Dim pctBase As Integer = 5000 \ nCols

        Dim tbl As New Table()
        tbl.Append(New TableProperties(
            New TableStyle() With {.Val = "Tabladelista3"},
            New TableLayout() With {.Type = TableLayoutValues.Fixed},
            New TableWidth() With {.Type = TableWidthUnitValues.Pct, .Width = "5000"},
            New TableLook() With {.Val = "04A0", .FirstRow = True, .LastRow = False, .FirstColumn = True, .LastColumn = False, .NoHorizontalBand = False, .NoVerticalBand = True}))

        Dim grid As New TableGrid()
        For i = 0 To nCols - 1
            grid.Append(New GridColumn())
        Next
        tbl.Append(grid)

        Dim AnchoCol = Function(i As Integer) As String
                           Return (If(i = nCols - 1, 5000 - pctBase * (nCols - 1), pctBase)).ToString()
                       End Function

        ' Fila de encabezado
        Dim filaEnc As New TableRow()
        filaEnc.Append(New TableRowProperties(
            New TableHeader(),
            New TableRowHeight() With {.Val = 360, .HeightType = HeightRuleValues.AtLeast},
            New ConditionalFormatStyle() With {.Val = "100000000000", .FirstRow = True}))
        For i = 0 To nCols - 1
            Dim tc As New TableCell()
            tc.Append(New TableCellProperties(
                New TableCellWidth() With {.Type = TableWidthUnitValues.Pct, .Width = AnchoCol(i)},
                New Shading() With {.Val = ShadingPatternValues.Clear, .Color = "auto", .Fill = COLOR_ENCABEZADO_TABLA},
                New TableCellVerticalAlignment() With {.Val = TableVerticalAlignmentValues.Center}))
            Dim p As New Paragraph()
            p.Append(New ParagraphProperties(
                New SpacingBetweenLines() With {.After = "0", .Line = "240", .LineRule = LineSpacingRuleValues.Auto},
                New Justification() With {.Val = JustificationValues.Center}))
            p.Append(CrearRun(headers(i), negrita:=False, tamanoMedioPunto:=TAM_TABLA, colorHex:=COLOR_BLANCO))
            tc.Append(p)
            filaEnc.Append(tc)
        Next
        tbl.Append(filaEnc)

        ' Filas de datos
        For rowIdx = 0 To filas.Count - 1
            Dim fila = filas(rowIdx)
            Dim isOdd As Boolean = (rowIdx Mod 2 = 0)
            Dim tr As New TableRow()
            tr.Append(New TableRowProperties(
                New TableRowHeight() With {.Val = 317, .HeightType = HeightRuleValues.AtLeast},
                New ConditionalFormatStyle() With {.Val = If(isOdd, "000000100000", "000000010000")}))
            For i = 0 To nCols - 1
                Dim tc As New TableCell()
                tc.Append(New TableCellProperties(
                    New TableCellWidth() With {.Type = TableWidthUnitValues.Pct, .Width = AnchoCol(i)},
                    New TableCellVerticalAlignment() With {.Val = TableVerticalAlignmentValues.Center}))
                Dim p As New Paragraph()
                p.Append(New ParagraphProperties(
                    New SpacingBetweenLines() With {.After = "0"},
                    New Justification() With {.Val = JustificationValues.Center}))
                Dim valor As String = If(i < fila.Length, fila(i), "")
                Dim esNegrita As Boolean = Array.IndexOf(colsNegrita, i) >= 0
                p.Append(CrearRun(valor, negrita:=esNegrita, tamanoMedioPunto:="16"))
                tc.Append(p)
                tr.Append(tc)
            Next
            tbl.Append(tr)
        Next

        Return tbl
    End Function

    ' Contador para dar un Id único a cada imagen insertada en el documento.
    Private _contadorImagen As Integer = 0

    ' ── Imágenes (gráficos exportados como PNG, insertados directo desde memoria) ──────────────
    ''' <summary>
    ''' Inserta una imagen PNG centrada en el documento, con el tamaño (en cm) indicado.
    ''' No requiere Word instalado: la imagen se embebe como <see cref="ImagePart"/> del propio
    ''' paquete OpenXml. Cada llamada agrega una parte de imagen nueva al documento.
    ''' </summary>
    Public Function ImagenCentrada(mainPart As MainDocumentPart, pngBytes As Byte(), anchoCm As Double, altoCm As Double) As Paragraph
        If pngBytes Is Nothing OrElse pngBytes.Length = 0 Then Return ParrafoNormal("")

        Dim imagePart As ImagePart = mainPart.AddImagePart(ImagePartType.Png)
        Using ms As New MemoryStream(pngBytes)
            imagePart.FeedData(ms)
        End Using
        Dim relId As String = mainPart.GetIdOfPart(imagePart)

        _contadorImagen += 1
        Dim idImagen As UInteger = CUInt(_contadorImagen)
        Dim nombreImagen As String = "Imagen" & idImagen

        Const EMU_POR_CM As Double = 360000.0
        Dim cx As Long = CLng(anchoCm * EMU_POR_CM)
        Dim cy As Long = CLng(altoCm * EMU_POR_CM)

        ' Uri va en GraphicData, no en Picture ni en Graphic: en DocumentFormat.OpenXml 3.x
        ' solo GraphicData expone esa propiedad. La versión original de este código la
        ' ponía en los otros dos y no compilaba contra el paquete actual.
        Dim grafico As New A.Graphic(
            New A.GraphicData(
                New PIC.Picture(
                    New PIC.NonVisualPictureProperties(
                        New PIC.NonVisualDrawingProperties() With {.Id = 0UI, .Name = nombreImagen},
                        New PIC.NonVisualPictureDrawingProperties()),
                    New PIC.BlipFill(
                        New A.Blip() With {.Embed = relId},
                        New A.Stretch(New A.FillRectangle())),
                    New PIC.ShapeProperties(
                        New A.Transform2D(
                            New A.Offset() With {.X = 0L, .Y = 0L},
                            New A.Extents() With {.Cx = cx, .Cy = cy}),
                        New A.PresetGeometry(New A.AdjustValueList()) With {.Preset = A.ShapeTypeValues.Rectangle})
                )
            ) With {.Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"}
        )

        Dim inline1 As New DW.Inline(
            New DW.Extent() With {.Cx = cx, .Cy = cy},
            New DW.EffectExtent() With {.LeftEdge = 0L, .TopEdge = 0L, .RightEdge = 0L, .BottomEdge = 0L},
            New DW.DocProperties() With {.Id = idImagen, .Name = nombreImagen},
            New DW.NonVisualGraphicFrameDrawingProperties(New A.GraphicFrameLocks() With {.NoChangeAspect = True}),
            grafico
        ) With {.DistanceFromTop = 0UI, .DistanceFromBottom = 0UI, .DistanceFromLeft = 0UI, .DistanceFromRight = 0UI}

        Dim p As New Paragraph()
        p.Append(New ParagraphProperties(New Justification() With {.Val = JustificationValues.Center}))
        p.Append(New Run(New Drawing(inline1)))
        Return p
    End Function

    ''' <summary>Título de figura ("Figura N. Descripción"), en cursiva y centrado, debajo de la imagen.</summary>
    Public Function TituloFigura(numero As Integer, descripcion As String) As Paragraph
        Dim p As New Paragraph()
        p.Append(New ParagraphProperties(New Justification() With {.Val = JustificationValues.Center}))
        p.Append(CrearRun("Figura " & numero & ". " & descripcion, cursiva:=True, tamanoMedioPunto:=TAM_TITULO))
        Return p
    End Function

    ' ── Encabezado — sustituye el código de proyecto resaltado en amarillo ("xx") ──────────────
    ''' <summary>
    ''' Reemplaza el texto "xx" resaltado en amarillo del encabezado (membrete corporativo) por el
    ''' código de proyecto. Común a todos los reportes que reutilizan la plantilla corporativa
    ''' (<see cref="ReporteRevisionService"/>, <see cref="InformeEstadoMurosService"/>).
    ''' </summary>
    Public Sub ActualizarCodigoEnEncabezado(wordDoc As WordprocessingDocument, codigo As String)
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

End Module
