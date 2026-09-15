Imports DocumentFormat.OpenXml
Imports DocumentFormat.OpenXml.Wordprocessing

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

End Module
