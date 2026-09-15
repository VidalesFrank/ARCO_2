Imports System.Data.OleDb
Imports System.IO.Compression
Imports System.Xml

' Partial de Form_02_PagColumnas: lectura directa de archivos XLSX de ETABS vía
' System.IO.Compression.ZipArchive + System.Xml.XmlReader (sin OleDB) más wrappers
' OleDB legado. Extraído del formulario para separar el I/O crudo de la UI de importación.
' Los métodos siguen siendo Private/Private Shared — solo consumidos desde este formulario.
Partial Public Class Form_02_PagColumnas

    ''' <summary>Lee xl/workbook.xml + xl/_rels/workbook.xml.rels; devuelve sheetName→entryPath.</summary>
    Private Shared Function ZipLeerWorkbookMap(za As ZipArchive) As Dictionary(Of String, String)
        Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim sheetRId As New Dictionary(Of String, String)()
        Dim wbEntry = za.Entries.FirstOrDefault(Function(e) e.FullName.Equals("xl/workbook.xml", StringComparison.OrdinalIgnoreCase))
        If wbEntry Is Nothing Then Return result
        Using stream = wbEntry.Open()
            Using xr = XmlReader.Create(stream)
                While xr.Read()
                    If xr.NodeType = XmlNodeType.Element AndAlso xr.LocalName = "sheet" Then
                        Dim nm  = xr.GetAttribute("name")
                        Dim rid = xr.GetAttribute("r:id")
                        If rid Is Nothing Then rid = xr.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
                        If nm IsNot Nothing AndAlso rid IsNot Nothing Then sheetRId(nm) = rid
                    End If
                End While
            End Using
        End Using
        Dim ridToPath As New Dictionary(Of String, String)()
        Dim relsEntry = za.Entries.FirstOrDefault(Function(e) e.FullName.Equals("xl/_rels/workbook.xml.rels", StringComparison.OrdinalIgnoreCase))
        If relsEntry IsNot Nothing Then
            Using stream = relsEntry.Open()
                Using xr = XmlReader.Create(stream)
                    While xr.Read()
                        If xr.NodeType = XmlNodeType.Element AndAlso xr.LocalName = "Relationship" Then
                            Dim rid    = xr.GetAttribute("Id")
                            Dim target = xr.GetAttribute("Target")
                            If rid IsNot Nothing AndAlso target IsNot Nothing Then
                                If Not target.StartsWith("/") Then target = "xl/" & target Else target = target.TrimStart("/"c)
                                ridToPath(rid) = target
                            End If
                        End If
                    End While
                End Using
            End Using
        End If
        For Each kvp In sheetRId
            If ridToPath.ContainsKey(kvp.Value) Then result(kvp.Key) = ridToPath(kvp.Value)
        Next
        Return result
    End Function

    ''' <summary>Lee xl/sharedStrings.xml; devuelve array de texto indexado.</summary>
    Private Shared Function ZipLeerSharedStrings(za As ZipArchive) As String()
        Dim entry = za.Entries.FirstOrDefault(Function(e) e.FullName.Equals("xl/sharedStrings.xml", StringComparison.OrdinalIgnoreCase))
        If entry Is Nothing Then Return New String() {}
        Dim ssts As New List(Of String)()
        Using stream = entry.Open()
            Using xr = XmlReader.Create(stream)
                Dim inSi As Boolean = False, inT As Boolean = False
                Dim buf As New System.Text.StringBuilder()
                While xr.Read()
                    Select Case xr.NodeType
                        Case XmlNodeType.Element
                            If xr.LocalName = "si" Then
                                inSi = True : buf.Clear()
                            ElseIf xr.LocalName = "t" AndAlso inSi Then
                                inT = True
                            End If
                        Case XmlNodeType.Text, XmlNodeType.Whitespace, XmlNodeType.SignificantWhitespace
                            If inT Then buf.Append(xr.Value)
                        Case XmlNodeType.EndElement
                            If xr.LocalName = "t" Then
                                inT = False
                            ElseIf xr.LocalName = "si" Then
                                ssts.Add(buf.ToString())
                                inSi = False
                            End If
                    End Select
                End While
            End Using
        End Using
        Return ssts.ToArray()
    End Function

    ''' <summary>Lee una hoja xlsx como DataTable ETABS: Rows(0)=headers, Rows(1)=units, Rows(2+)=datos.
    ''' Si selectedLabels no es Nothing, filtra filas de datos por la columna labelColIdx.</summary>
    Private Shared Function ZipLeerHoja(za As ZipArchive, wbMap As Dictionary(Of String, String),
                                        sstLookup() As String, selectedLabels As HashSet(Of String),
                                        labelColIdx As Integer, ParamArray keywords As String()) As DataTable
        Dim entryPath As String = Nothing
        For Each kw In keywords
            For Each shName In wbMap.Keys
                If shName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    entryPath = wbMap(shName) : Exit For
                End If
            Next
            If entryPath IsNot Nothing Then Exit For
        Next
        If entryPath Is Nothing Then Return Nothing
        Dim entry = za.Entries.FirstOrDefault(Function(e) e.FullName.Equals(entryPath, StringComparison.OrdinalIgnoreCase))
        If entry Is Nothing Then Return Nothing

        Dim allRows As New List(Of Object())()
        Dim maxCols As Integer = 0
        Dim excelRowIdx As Integer = 0
        Dim filtrar As Boolean = (selectedLabels IsNot Nothing AndAlso selectedLabels.Count > 0)

        Using stream = entry.Open()
            Using xr = XmlReader.Create(stream)
                Dim inRow As Boolean = False, inCell As Boolean = False, inV As Boolean = False
                Dim cellType As String = "", cellRef As String = ""
                Dim cellVal As New System.Text.StringBuilder()
                Dim rowData As List(Of Object) = Nothing

                While xr.Read()
                    Select Case xr.NodeType
                        Case XmlNodeType.Element
                            Select Case xr.LocalName
                                Case "row"
                                    excelRowIdx += 1 : inRow = True
                                    rowData = New List(Of Object)()
                                Case "c"
                                    If inRow Then
                                        inCell = True
                                        cellRef  = If(xr.GetAttribute("r"), "")
                                        cellType = If(xr.GetAttribute("t"), "")
                                        cellVal.Clear()
                                    End If
                                Case "v", "t"
                                    If inCell AndAlso Not xr.IsEmptyElement Then inV = True
                            End Select
                        Case XmlNodeType.Text, XmlNodeType.Whitespace
                            If inV Then cellVal.Append(xr.Value)
                        Case XmlNodeType.EndElement
                            Select Case xr.LocalName
                                Case "v", "t"
                                    inV = False
                                Case "c"
                                    If inCell AndAlso rowData IsNot Nothing Then
                                        Dim colLetters As String = ""
                                        For Each ch In cellRef
                                            If Char.IsLetter(ch) Then colLetters &= ch Else Exit For
                                        Next
                                        Dim colIdx As Integer = XmlColToIndex(colLetters)
                                        While rowData.Count < colIdx : rowData.Add(DBNull.Value) : End While
                                        rowData.Add(ZipCellValue(cellType, cellVal.ToString(), sstLookup))
                                    End If
                                    inCell = False : cellRef = "" : cellType = "" : cellVal.Clear()
                                Case "row"
                                    If rowData IsNot Nothing Then
                                        Dim agregar As Boolean = True
                                        If filtrar AndAlso excelRowIdx > 3 Then
                                            Dim lbl As String = ""
                                            If rowData.Count > labelColIdx AndAlso rowData(labelColIdx) IsNot DBNull.Value Then
                                                lbl = rowData(labelColIdx).ToString().Trim()
                                            End If
                                            agregar = selectedLabels.Contains(lbl)
                                        End If
                                        If agregar Then
                                            If rowData.Count > maxCols Then maxCols = rowData.Count
                                            allRows.Add(rowData.ToArray())
                                        End If
                                    End If
                                    inRow = False : rowData = Nothing
                                Case "sheetData"
                                    Exit While
                            End Select
                    End Select
                End While
            End Using
        End Using

        If allRows.Count < 2 Then Return Nothing
        Dim dt As New DataTable()
        For i = 0 To maxCols - 1 : dt.Columns.Add("C" & i, GetType(Object)) : Next
        For r = 1 To allRows.Count - 1
            Dim dr = dt.NewRow()
            Dim rv = allRows(r)
            For c = 0 To Math.Min(rv.Length, maxCols) - 1
                dr(c) = If(rv(c) Is Nothing, DBNull.Value, rv(c))
            Next
            dt.Rows.Add(dr)
        Next
        Return dt
    End Function

    ''' <summary>Convierte referencia de columna Excel ("A"→0, "Z"→25, "AA"→26) a índice base 0.</summary>
    Private Shared Function XmlColToIndex(colLetters As String) As Integer
        Dim idx As Integer = 0
        For Each ch In colLetters.ToUpperInvariant()
            idx = idx * 26 + (AscW(ch) - AscW("A"c) + 1)
        Next
        Return idx - 1
    End Function

    ''' <summary>Convierte tipo+valor bruto de celda xlsx a Object (s=shared string, b=bool, numérico=Double).</summary>
    Private Shared Function ZipCellValue(cellType As String, rawVal As String, sstLookup() As String) As Object
        If String.IsNullOrEmpty(rawVal) Then Return DBNull.Value
        Select Case cellType
            Case "s"
                Dim idx As Integer
                If Integer.TryParse(rawVal, idx) AndAlso idx >= 0 AndAlso idx < sstLookup.Length Then Return sstLookup(idx)
                Return ""
            Case "b"
                Return If(rawVal = "1", "True", "False")
            Case "str", "inlineStr"
                Return rawVal
            Case "e"
                Return DBNull.Value
            Case Else
                Dim dbl As Double
                If Double.TryParse(rawVal, Globalization.NumberStyles.Float,
                                   Globalization.CultureInfo.InvariantCulture, dbl) Then Return dbl
                Return rawVal
        End Select
    End Function

    ''' <summary>
    ''' Busca una hoja en la lista por cualquiera de los keywords y devuelve su DataTable.
    ''' Sigue disponible para compatibilidad con importaciones individuales vía OleDb.
    ''' </summary>
    Private Function LeerDataTableOleDb(cn As OleDbConnection,
                                        hojas As List(Of String),
                                        ParamArray keywords As String()) As DataTable
        Dim nombre As String = Nothing
        For Each kw As String In keywords
            nombre = hojas.FirstOrDefault(Function(h) h.ToUpperInvariant().Contains(kw.ToUpperInvariant()))
            If nombre IsNot Nothing Then Exit For
        Next
        If nombre Is Nothing Then Return Nothing

        Try
            Dim ds As New DataSet
            Dim da As New OleDbDataAdapter(New OleDbCommand($"SELECT * FROM [{nombre}]", cn))
            da.Fill(ds)
            Return ds.Tables(0)
        Catch ex As Exception
            Logger.Warning("LeerDataTableOleDb", $"Error al leer hoja '{nombre}': {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Vincula un DataTable a un DataGridView. Se llama en el hilo UI tras completar el hilo STA.
    ''' </summary>
    Private Function VincularDGV(dt As DataTable, dgv As DataGridView) As Boolean
        If dt Is Nothing Then Return False
        dgv.Columns.Clear()
        dgv.DataSource = dt
        Return True
    End Function

    ' ─── Helpers para leer DataTables ZIP (Row 0 = headers) ────────────────────
    Private Shared Function ZipColIdx(dt As DataTable, ParamArray kws As String()) As Integer
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return -1
        Dim hr = dt.Rows(0)
        For Each kw In kws
            Dim kLow = kw.ToLowerInvariant()
            For ci = 0 To dt.Columns.Count - 1
                Dim h = If(hr(ci) IsNot DBNull.Value, hr(ci).ToString().Trim().ToLowerInvariant(), "")
                If h.Contains(kLow) Then Return ci
            Next
        Next
        Return -1
    End Function

    Private Shared Function ZipStr(row As DataRow, col As Integer) As String
        If col < 0 OrElse col >= row.Table.Columns.Count Then Return ""
        Return If(row(col) IsNot DBNull.Value, row(col).ToString().Trim(), "")
    End Function

    Private Shared Function ZipDbl(row As DataRow, col As Integer) As Double
        Dim s = ZipStr(row, col)
        Dim d As Double = 0
        Double.TryParse(s, Globalization.NumberStyles.Float,
                        Globalization.CultureInfo.InvariantCulture, d)
        Return d
    End Function

End Class
