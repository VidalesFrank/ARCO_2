Imports System.Data.OleDb
Imports System.IO
Imports ExcelDataReader

' Partial de Funciones_00_Varias: I/O directo con archivos Excel exportados desde ETABS.
' Cubre lectura por OleDB (tablas convencionales) y por ExcelDataReader (streaming,
' obligatorio para BeamForces cuando la hoja se acerca al límite de 1'048.576 filas).
Partial Public Class Funciones_00_Varias

    Public Shared Function LeerHojaExcel(rutaArchivo As String, nombreHoja As String) As DataTable
        Dim dt As New DataTable()

        Try
            Dim conexionStr As String =
            "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & rutaArchivo &
            ";Extended Properties='Excel 12.0 Xml;HDR=NO;IMEX=1;'"

            Using conexion As New OleDbConnection(conexionStr)
                conexion.Open()
                ' 🔹 Leemos todo sin encabezado (HDR=NO)
                Dim cmd As New OleDbCommand("SELECT * FROM [" & nombreHoja & "$]", conexion)
                Dim da As New OleDbDataAdapter(cmd)
                da.Fill(dt)
            End Using

            ' ==============================================================
            ' 🔹 SALTAMOS las primeras filas
            ' Fila 1 → "TABLE: ..."
            ' Fila 2 → Encabezados reales
            ' Fila 3 → unidades (opcional de eliminar)
            ' ==============================================================
            If dt.Rows.Count >= 3 Then
                ' Tomamos la segunda fila como encabezado real
                Dim headerRow As DataRow = dt.Rows(1)

                ' Creamos una nueva tabla con esos encabezados
                Dim newTable As New DataTable()

                For i As Integer = 0 To dt.Columns.Count - 1
                    Dim colName As String = headerRow(i).ToString().Trim()
                    If colName = "" Then colName = $"Col_{i + 1}"
                    newTable.Columns.Add(colName)
                Next

                ' Copiamos las filas desde la cuarta en adelante (índice 3)
                For i As Integer = 3 To dt.Rows.Count - 1
                    Dim row As DataRow = dt.Rows(i)
                    Dim newRow As DataRow = newTable.NewRow()
                    For j As Integer = 0 To dt.Columns.Count - 1
                        newRow(j) = row(j)
                    Next
                    newTable.Rows.Add(newRow)
                Next

                dt = newTable
            End If

            ' 🔹 Limpieza de nombres de columnas
            For Each col As DataColumn In dt.Columns
                col.ColumnName = col.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "")
            Next

        Catch ex As Exception
            MessageBox.Show("Error leyendo la hoja '" & nombreHoja & "': " & ex.Message)
        End Try

        Return dt

    End Function

    ''' <summary>
    ''' Devuelve todas las hojas disponibles en un archivo Excel.
    ''' </summary>
    Public Shared Function ObtenerHojasExcel(rutaArchivo As String) As List(Of String)
        Dim hojas As New List(Of String)
        Try
            Dim cnStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & rutaArchivo &
                        ";Extended Properties='Excel 12.0 Xml;HDR=NO;IMEX=1;'"
            Using cn As New OleDbConnection(cnStr)
                cn.Open()
                Dim schema = cn.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Tables, Nothing)
                For Each row As DataRow In schema.Rows
                    Dim nombre = row("TABLE_NAME").ToString().Replace("$", "").Replace("'", "").Trim()
                    hojas.Add(nombre)
                Next
            End Using
        Catch ex As Exception
            Logger.Warning("ObtenerHojasExcel", $"No se pudo enumerar hojas de '{rutaArchivo}': {ex.Message}")
        End Try
        Return hojas
    End Function

    ''' <summary>
    ''' Resuelve el nombre real de una hoja probando candidatos en orden (E23 primero, E17 segundo).
    ''' Si no encuentra ninguno, devuelve el primer candidato como fallback.
    ''' </summary>
    Public Shared Function ResolverNombreHoja(hojasDisponibles As List(Of String), ParamArray candidatos As String()) As String
        For Each candidato In candidatos
            Dim coincidencia = hojasDisponibles.FirstOrDefault(
                Function(h) h.TrimEnd("$"c).Equals(candidato, StringComparison.OrdinalIgnoreCase))
            If coincidencia IsNot Nothing Then Return coincidencia.TrimEnd("$"c)
        Next
        Return candidatos(0)
    End Function

    ' =========================================================================
    ' Carga BeamForces directamente con ExcelDataReader (streaming, sin OleDB).
    ' Evita OutOfMemoryException en archivos grandes (>1M filas) y filtra casos
    ' modales (LinModEigen / StepType=Mode) que ARCO no necesita.
    ' posibleTruncamiento = True cuando la hoja alcanzó el límite de Excel
    ' (1,048,576 filas) y los últimos datos de diseño pueden estar cortados.
    ' =========================================================================
    Public Shared Function CargarBeamForcesDesdeExcel(
            rutaArchivo As String,
            nombreHoja As String,
            ByRef posibleTruncamiento As Boolean) As List(Of cCombinacionBeamForce)

        Const LIMITE_EXCEL As Integer = 1_048_576

        posibleTruncamiento = False
        Dim lista As New List(Of cCombinacionBeamForce)

        Using fs As FileStream = File.Open(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using reader As IExcelDataReader = ExcelReaderFactory.CreateReader(fs)

                ' Navegar a la hoja correcta
                Dim encontrada As Boolean = False
                Do
                    If String.Compare(reader.Name, nombreHoja, StringComparison.OrdinalIgnoreCase) = 0 Then
                        encontrada = True
                        Exit Do
                    End If
                Loop While reader.NextResult()

                If Not encontrada Then Return lista

                ' Fila 1: "TABLE: ..." — ignorar
                If Not reader.Read() Then Return lista

                ' Fila 2: encabezados — mapear por nombre exacto (Select Case)
                If Not reader.Read() Then Return lista

                ' Índices de columnas (−1 = no encontrado)
                Dim iStory As Integer = -1, iBeam As Integer = -1
                Dim iUniqueName As Integer = -1, iOutputCase As Integer = -1
                Dim iCaseType As Integer = -1, iStepType As Integer = -1
                Dim iStation As Integer = -1, iP As Integer = -1
                Dim iV2 As Integer = -1, iV3 As Integer = -1
                Dim iT As Integer = -1, iM2 As Integer = -1, iM3 As Integer = -1
                Dim iElement As Integer = -1, iElemStation As Integer = -1

                For col As Integer = 0 To reader.FieldCount - 1
                    Dim h As String = If(reader.IsDBNull(col), "", reader.GetString(col).Trim().ToLower())
                    Select Case h
                        Case "story" : iStory = col
                        Case "beam" : iBeam = col
                        Case "unique name" : iUniqueName = col
                        Case "output case" : iOutputCase = col
                        Case "case type" : iCaseType = col
                        Case "step type" : iStepType = col
                        Case "station" : iStation = col
                        Case "p" : iP = col
                        Case "v2" : iV2 = col
                        Case "v3" : iV3 = col
                        Case "t" : iT = col
                        Case "m2" : iM2 = col
                        Case "m3" : iM3 = col
                        Case "element" : iElement = col
                        Case "elem station", "element station" : iElemStation = col
                    End Select
                Next

                ' Fila 3: unidades — ignorar
                If Not reader.Read() Then Return lista

                ' Fila 4+: datos
                Dim totalFilas As Long = 0
                Do While reader.Read()
                    totalFilas += 1

                    ' Filtrar casos modales que no son de diseño
                    Dim caseType As String = BfStr(reader, iCaseType)
                    Dim stepType As String = BfStr(reader, iStepType)
                    If caseType.IndexOf("Eigen", StringComparison.OrdinalIgnoreCase) >= 0 Then Continue Do
                    If String.Compare(stepType, "Mode", StringComparison.OrdinalIgnoreCase) = 0 Then Continue Do

                    Dim bf As New cCombinacionBeamForce()
                    bf.Story = BfStr(reader, iStory)
                    bf.Beam = BfStr(reader, iBeam)
                    bf.UniqueName = BfStr(reader, iUniqueName)
                    bf.LoadCaseCombo = BfStr(reader, iOutputCase)
                    bf.stepType = stepType
                    bf.Station = BfDbl(reader, iStation)
                    bf.P = BfDbl(reader, iP)
                    bf.V2 = BfDbl(reader, iV2)
                    bf.V3 = BfDbl(reader, iV3)
                    bf.T = BfDbl(reader, iT)
                    bf.M2 = BfDbl(reader, iM2)
                    bf.M3 = BfDbl(reader, iM3)
                    bf.Element = BfStr(reader, iElement)
                    bf.ElementStation = BfDbl(reader, iElemStation)

                    lista.Add(bf)
                Loop

                ' Detectar truncamiento: si la hoja tiene ≈ el límite de Excel,
                ' los últimos registros de diseño probablemente quedaron cortados.
                If (totalFilas + 3) >= (LIMITE_EXCEL - 200) Then
                    posibleTruncamiento = True
                End If

            End Using
        End Using

        Return lista
    End Function

    Private Shared Function BfStr(reader As IExcelDataReader, idx As Integer) As String
        If idx < 0 OrElse reader.IsDBNull(idx) Then Return ""
        Return reader.GetValue(idx).ToString().Trim()
    End Function

    Private Shared Function BfDbl(reader As IExcelDataReader, idx As Integer) As Double
        If idx < 0 OrElse reader.IsDBNull(idx) Then Return 0.0
        Dim v = reader.GetValue(idx)
        If TypeOf v Is Double Then Return CDbl(v)
        If TypeOf v Is Single Then Return CDbl(v)
        Dim d As Double
        If Double.TryParse(v.ToString(), Globalization.NumberStyles.Any,
                           Globalization.CultureInfo.InvariantCulture, d) Then Return d
        Return 0.0
    End Function

End Class
