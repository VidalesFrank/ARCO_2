Imports System.Data.OleDb
Imports System.IO
Imports ExcelDataReader

Public Class Funciones_00_Varias

    ''' <summary>Normaliza claves E17 ("Envolvente Min/Max") al formato canónico "Envolvente (Min/Max)".</summary>
    Public Shared Function NormalizarClaveCombo(nombre As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return nombre
        Dim t = nombre.Trim()
        If t.EndsWith(" Max", StringComparison.OrdinalIgnoreCase) Then
            Return t.Substring(0, t.Length - 4).TrimEnd() & " (Max)"
        ElseIf t.EndsWith(" Min", StringComparison.OrdinalIgnoreCase) Then
            Return t.Substring(0, t.Length - 4).TrimEnd() & " (Min)"
        End If
        Return t
    End Function

    Public Shared Sub ImprimirEstructuraDataTable(dt As DataTable, Optional nombreTabla As String = "")
        If dt Is Nothing Then
            Console.WriteLine($"[❌] La tabla {nombreTabla} está vacía o es Nothing.")
            Return
        End If

        Console.WriteLine($"[📋] Tabla: {nombreTabla}")
        Console.WriteLine($"Filas: {dt.Rows.Count} | Columnas: {dt.Columns.Count}")

        ' 🔹 Imprimir nombres de columnas
        Dim nombresColumnas As String = String.Join(" | ", dt.Columns.Cast(Of DataColumn).Select(Function(c) c.ColumnName))
        Console.WriteLine("Columnas: " & nombresColumnas)
        Console.WriteLine(New String("-"c, 80))

        ' 🔹 Imprimir las primeras 5 filas
        Dim filasMostrar As Integer = Math.Min(5, dt.Rows.Count)
        For i As Integer = 0 To filasMostrar - 1
            Dim fila As DataRow = dt.Rows(i)
            Dim valores As String = String.Join(" | ", fila.ItemArray.Select(Function(x) x?.ToString()))
            Console.WriteLine(valores)
        Next

        Console.WriteLine(New String("="c, 80))
    End Sub


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
        Catch
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

    ' 🔸 Función auxiliar para buscar nombres de columna de forma flexible.
    ' Prioridad: 1) coincidencia exacta (sin espacios) 2) subcadena.
    ' La prioridad exacta evita que columnas cortas como "P", "T", "V2" colisionen
    ' con nombres más largos como "Step Type" o "Story".
    Private Shared Function GetColumnName(cols As Dictionary(Of String, String), key As String) As String
        key = key.Trim().ToLower()
        Dim keyNorm = key.Replace(" ", "")
        For Each c In cols.Keys
            If c.Replace(" ", "") = keyNorm Then Return cols(c)
        Next
        For Each c In cols.Keys
            If c.Contains(keyNorm) Then Return cols(c)
        Next
        Return Nothing
    End Function

    ' 🔸 Funciones auxiliares seguras
    Private Shared Function SafeString(r As DataRow, colName As String) As String
        If String.IsNullOrEmpty(colName) Then Return ""
        Return If(r.IsNull(colName), "", r(colName).ToString().Trim())
    End Function

    Private Shared Function SafeDouble(r As DataRow, colName As String) As Double
        If String.IsNullOrEmpty(colName) Then Return 0
        Dim val As String = If(r.IsNull(colName), "", r(colName).ToString().Trim())
        Dim d As Double
        If Double.TryParse(val, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then
            Return d
        ElseIf Double.TryParse(val, d) Then
            Return d
        Else
            Return 0
        End If
    End Function

    Public Shared Function DataTableToJoints(dt As DataTable) As List(Of cJoint)

        Dim lista As New List(Of cJoint)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

        ' 🔹 Normalizar nombres de columnas
        Dim columnas = dt.Columns.Cast(Of DataColumn).ToDictionary(
        Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
        Function(c) c.ColumnName
    )

        ' 🔹 Buscar las columnas esperadas
        Dim colStory = GetColumnName(columnas, "Story")
        Dim colElementLabel = GetColumnName(columnas, "Element Name")
        Dim colObjectType = GetColumnName(columnas, "Object Type")
        Dim colObjectLabel = GetColumnName(columnas, "Object Label")
        Dim colGlobalX = GetColumnName(columnas, "Global X")
        Dim colGlobalY = GetColumnName(columnas, "Global Y")
        Dim colGlobalZ = GetColumnName(columnas, "Global Z")

        ' 🔹 Recorremos cada fila
        For Each r As DataRow In dt.Rows

            Dim tipo As String = SafeString(r, colObjectType)
            If Not String.Equals(tipo, "Joint", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            Dim j As New cJoint()

            j.Story = SafeString(r, colStory)
            j.ElementLabel = SafeString(r, colElementLabel)
            j.ObjectType = SafeString(r, colObjectType)
            j.ObjectLabel = SafeString(r, colObjectLabel)
            j.GlobalX = SafeDouble(r, colGlobalX)
            j.GlobalY = SafeDouble(r, colGlobalY)
            j.GlobalZ = SafeDouble(r, colGlobalZ)

            lista.Add(j)
        Next

        Return lista
    End Function

    'Public Shared Function DataTableToFrames(dt As DataTable) As List(Of cFrame)

    '    Dim lista As New List(Of cFrame)
    '    If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

    '    ' 🔹 Normalizar nombres de columnas
    '    Dim columnas = dt.Columns.Cast(Of DataColumn).ToDictionary(
    '    Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
    '    Function(c) c.ColumnName
    ')

    '    ' 🔹 Buscar las columnas esperadas
    '    Dim colStory = GetColumnName(columnas, "Story")
    '    Dim colElementLabel = GetColumnName(columnas, "Element Name")
    '    Dim colObjectType = GetColumnName(columnas, "Object Type")
    '    Dim colObjectLabel = GetColumnName(columnas, "Object Label")
    '    Dim colJointI = GetColumnName(columnas, "Elm JtI")
    '    Dim colJointJ = GetColumnName(columnas, "Elm JtJ")

    '    ' 🔹 Recorremos cada fila
    '    For Each r As DataRow In dt.Rows

    '        Dim tipo As String = SafeString(r, colObjectType)
    '        If Not String.Equals(tipo, "Frame", StringComparison.OrdinalIgnoreCase) Then
    '            Continue For
    '        End If

    '        Dim f As New cFrame()

    '        f.Story = SafeString(r, colStory)
    '        f.ElementLabel = SafeString(r, colElementLabel)
    '        f.ObjectType = SafeString(r, colObjectType)
    '        f.ObjectLabel = SafeString(r, colObjectLabel)
    '        f.JointI = SafeString(r, colJointI)
    '        f.JointJ = SafeString(r, colJointJ)

    '        lista.Add(f)
    '    Next

    '    Return lista
    'End Function

    Public Shared Function DataTableToFrames(dt As DataTable) As List(Of cFrame)

        Dim lista As New List(Of cFrame)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

        ' 🔹 Normalizar nombres de columnas
        Dim columnas = dt.Columns.Cast(Of DataColumn).ToDictionary(
        Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
        Function(c) c.ColumnName
    )

        ' 🔹 Buscar columnas
        Dim colStory = GetColumnName(columnas, "Story")
        Dim colElementLabel = GetColumnName(columnas, "Element Name")
        Dim colObjectType = GetColumnName(columnas, "Object Type")
        Dim colObjectLabel = GetColumnName(columnas, "Object Label")
        Dim colJointI = GetColumnName(columnas, "Elm JtI")
        Dim colJointJ = GetColumnName(columnas, "Elm JtJ")

        ' 🔹 Filtrar solo frames
        Dim filasFrames = dt.AsEnumerable().
        Where(Function(r) String.Equals(SafeString(r, colObjectType), "Frame", StringComparison.OrdinalIgnoreCase))

        ' 🔹 Agrupar por Story + Object Label
        Dim grupos = filasFrames.GroupBy(Function(r) New With {
        Key .Story = SafeString(r, colStory),
        Key .Label = SafeString(r, colObjectLabel)
    })

        ' 🔹 Procesar cada grupo (cada viga real)
        For Each g In grupos

            Dim nodos As New List(Of String)

            For Each r In g
                nodos.Add(SafeString(r, colJointI))
                nodos.Add(SafeString(r, colJointJ))
            Next

            ' 🔹 Quitar duplicados
            'nodos = nodos.Distinct().ToList()

            ' 🔹 Contar ocurrencias
            Dim conteo = nodos.
            GroupBy(Function(n) n).
            ToDictionary(Function(x) x.Key, Function(x) x.Count())

            ' 🔹 Nodos extremos = aparecen una sola vez y no tienen "~"
            'Dim extremos = conteo.
            'Where(Function(kv) kv.Value = 1 AndAlso Not kv.Key.Contains("~")).
            'Select(Function(kv) kv.Key).
            'ToList()
            Dim extremos = conteo.
                Where(Function(kv) kv.Value = 1 AndAlso Not kv.Key.Contains("~")).
                Select(Function(kv) kv.Key).
                ToList()

            ' 🔁 Si no encontró extremos (caso raro), usar solo por ocurrencia
            If extremos.Count < 2 Then
                extremos = conteo.
                    Where(Function(kv) kv.Value = 1).
                    Select(Function(kv) kv.Key).
                    ToList()
            End If

            If extremos.Count < 2 Then Continue For

            ' 🔹 Crear el frame real
            Dim f As New cFrame()

            f.Story = g.Key.Story
            f.ObjectLabel = g.Key.Label
            f.ObjectType = "Frame"

            ' Puedes tomar cualquier element label del grupo
            f.ElementLabel = SafeString(g.First(), colElementLabel)

            f.JointI = extremos(0)
            f.JointJ = extremos(1)

            lista.Add(f)

        Next

        Return lista

    End Function

    Public Shared Function DataTableToReactions(dt As DataTable) As List(Of cCombinacionPila)

        Dim lista As New List(Of cCombinacionPila)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

        ' Normalizar nombres de columnas
        Dim columnas = dt.Columns.Cast(Of DataColumn).ToDictionary(
            Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
            Function(c) c.ColumnName
        )

        ' Detectar formato E23: tiene columna "Step Type"
        Dim colStepType = GetColumnName(columnas, "Step Type")
        Dim esE23 As Boolean = Not String.IsNullOrEmpty(colStepType)

        ' E17: "Joint Label" / "Load Case/Combo"
        ' E23: "Label"       / "Output Case"  +  "Step Type" (Max, Min o vacío)
        Dim colJointLabel As String = If(esE23,
                                         GetColumnName(columnas, "Label"),
                                         GetColumnName(columnas, "Joint Label"))
        Dim colLoadCaseBase As String = If(esE23,
                                           GetColumnName(columnas, "Output Case"),
                                           GetColumnName(columnas, "Load Case/Combo"))

        Dim colStory = GetColumnName(columnas, "Story")
        Dim colUniqueName = GetColumnName(columnas, "Unique Name")
        Dim colFx = GetColumnName(columnas, "FX")
        Dim colFy = GetColumnName(columnas, "FY")
        Dim colFz = GetColumnName(columnas, "FZ")
        Dim colMx = GetColumnName(columnas, "MX")
        Dim colMy = GetColumnName(columnas, "MY")
        Dim colMz = GetColumnName(columnas, "MZ")

        For Each r As DataRow In dt.Rows

            Dim j As New cCombinacionPila()

            j.Story = SafeString(r, colStory)
            j.JointLabel = SafeString(r, colJointLabel)
            j.UniqueName = SafeString(r, colUniqueName)

            Dim baseName As String = SafeString(r, colLoadCaseBase)
            Dim stepVal As String = If(esE23, SafeString(r, colStepType), "")
            If Not String.IsNullOrEmpty(stepVal) Then
                j.LoadCase = baseName & " (" & stepVal & ")"
            ElseIf baseName.EndsWith(" Max", StringComparison.OrdinalIgnoreCase) Then
                j.LoadCase = baseName.Substring(0, baseName.Length - 4).TrimEnd() & " (Max)"
            ElseIf baseName.EndsWith(" Min", StringComparison.OrdinalIgnoreCase) Then
                j.LoadCase = baseName.Substring(0, baseName.Length - 4).TrimEnd() & " (Min)"
            Else
                j.LoadCase = baseName
            End If

            j.FX = SafeDouble(r, colFx)
            j.FY = SafeDouble(r, colFy)
            j.FZ = SafeDouble(r, colFz)
            j.MX = SafeDouble(r, colMx)
            j.MY = SafeDouble(r, colMy)
            j.MZ = SafeDouble(r, colMz)

            lista.Add(j)
        Next

        Return lista

    End Function

    Public Shared Function DataTableToBeamForces(dt As DataTable) As List(Of cCombinacionBeamForce)

        Dim lista As New List(Of cCombinacionBeamForce)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

        ' 🔹 Normalizar nombres de columnas
        Dim columnas = dt.Columns.Cast(Of DataColumn).ToDictionary(
        Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
        Function(c) c.ColumnName
    )

        ' 🔹 Buscar las columnas esperadas
        Dim colStory = GetColumnName(columnas, "Story")
        Dim colBeam = GetColumnName(columnas, "Beam")
        Dim colUniqueName = GetColumnName(columnas, "Unique Name")
        'Dim colLoadCaseCombo = GetColumnName(columnas, "Load Case/Combo")
        Dim colLoadCaseCombo = GetColumnName(columnas, "Output Case")
        Dim colStepType = GetColumnName(columnas, "Step Type")
        Dim colStation = GetColumnName(columnas, "Station")
        Dim colP = GetColumnName(columnas, "P")
        Dim colV2 = GetColumnName(columnas, "V2")
        Dim colV3 = GetColumnName(columnas, "V3")
        Dim colT = GetColumnName(columnas, "T")
        Dim colM2 = GetColumnName(columnas, "M2")
        Dim colM3 = GetColumnName(columnas, "M3")
        Dim colElement = GetColumnName(columnas, "Element")
        Dim colElementStation = GetColumnName(columnas, "Element Station")

        ' 🔹 Recorremos cada fila
        For Each r As DataRow In dt.Rows
            Dim bf As New cCombinacionBeamForce()

            bf.Story = SafeString(r, colStory)
            bf.Beam = SafeString(r, colBeam)
            bf.UniqueName = SafeString(r, colUniqueName)
            bf.LoadCaseCombo = SafeString(r, colLoadCaseCombo)
            bf.stepType = SafeString(r, colStepType)
            bf.Station = SafeDouble(r, colStation)
            bf.P = SafeDouble(r, colP)
            bf.V2 = SafeDouble(r, colV2)
            bf.V3 = SafeDouble(r, colV3)
            bf.T = SafeDouble(r, colT)
            bf.M2 = SafeDouble(r, colM2)
            bf.M3 = SafeDouble(r, colM3)
            bf.Element = SafeString(r, colElement)
            bf.ElementStation = SafeDouble(r, colElementStation)

            lista.Add(bf)
        Next

        Return lista

    End Function

    Public Shared Function DataTableToGridLines(dt As DataTable) As List(Of cGridLine)

        Dim lista As New List(Of cGridLine)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

        For Each r As DataRow In dt.Rows

            ' Las grillas diagonales de ETABS no tienen Ordinate — se omiten
            If IsDBNull(r("Ordinate")) OrElse String.IsNullOrWhiteSpace(r("Ordinate").ToString()) Then Continue For

            Dim g As New cGridLine With {
            .GridSystem = r("Name").ToString(),
            .Direction = r("Grid Line Type").ToString().Trim()(0).ToString(),
            .GridID = r("ID").ToString().Trim(),
            .Visible = r("Visible").ToString().Trim().ToLower() = "yes",
            .BubbleLocation = r("Bubble Location").ToString().Trim(),
            .Ordinate = CDbl(r("Ordinate"))
        }

            lista.Add(g)
        Next

        Return lista
    End Function

    Public Shared Sub DataTableToAsignFrame(frames As List(Of cFrame), dt As DataTable, dt_Frame_Sec As DataTable, dt_Mat_Concrete As DataTable)

        Dim dictSecciones As New Dictionary(Of String, String)

        For Each row As DataRow In dt.Rows

            Dim story As String = row("Story").ToString().Trim()
            Dim label As String = row("Label").ToString().Trim()
            Dim seccion As String = row("Section Property").ToString().Trim()

            Dim key As String = story & "|" & label

            If Not dictSecciones.ContainsKey(key) Then
                dictSecciones.Add(key, seccion)
            End If

        Next


        ' 🔹 Diccionario: Name sección → propiedades geométricas
        Dim dictFrameSec As New Dictionary(Of String, (t3 As Double, t2 As Double, material As String))

        For Each row As DataRow In dt_Frame_Sec.Rows
            Dim name As String = row("Name").ToString().Trim()

            Dim t3 As Double = CDbl(row("Depth"))
            Dim t2 As Double = CDbl(row("Width"))
            Dim material As String = row("Material").ToString().Trim()

            If Not dictFrameSec.ContainsKey(name) Then
                dictFrameSec.Add(name, (t3, t2, material))
            End If
        Next


        ' 🔹 Diccionario: Material → propiedades mecánicas
        Dim dictMaterial As New Dictionary(Of String, (E As Double, G As Double, Fc As Double))

        For Each row As DataRow In dt_Mat_Concrete.Rows
            Dim name As String = row("Material").ToString().Trim()

            Dim Fc As Double = CDbl(row("Fc")) / 1000

            Dim E As Double = 4700 * Math.Sqrt(Fc) ' Relación empírica común para concreto: E = 4700 * sqrt(Fc)
            Dim G As Double = E / (2 * (1 + 0.2))

            If Not dictMaterial.ContainsKey(name) Then
                dictMaterial.Add(name, (E, G, Fc))
            End If
        Next

        For Each frame As cFrame In frames

            Dim key As String = frame.Story.Trim() & "|" & frame.ObjectLabel.Trim()

            ' 🔹 Buscar sección asignada
            If dictSecciones.ContainsKey(key) Then

                Dim secName As String = dictSecciones(key)

                ' 🔹 Buscar propiedades de sección
                If dictFrameSec.ContainsKey(secName) Then

                    Dim secData = dictFrameSec(secName)


                    If frame.Section Is Nothing Then
                        frame.Section = New cSeccion()
                    End If

                    frame.Section.Nombre = secName

                    frame.Section.h = secData.t3
                    frame.Section.b = secData.t2
                    frame.Section.recubrimiento = 0.05
                    frame.Section.nameMaterial = secData.material
                    frame.Section.LabelSec = $"{secData.t2:0.00}x{secData.t3:0.00}"

                    ' 🔹 Buscar propiedades del material
                    If dictMaterial.ContainsKey(secData.material) Then

                        Dim matData = dictMaterial(secData.material)

                        frame.section.E = matData.E
                        frame.section.G = matData.G
                        frame.Section.fc = matData.Fc
                        frame.Section.fy = 420

                    End If

                End If

            End If

        Next


    End Sub


    Public Shared Function AreaRefuerzo(ByVal Barra As String)
        Dim Ac As Single
        If Barra = "#2" Then
            Ac = 32
        ElseIf Barra = "#3" Then
            Ac = 71
        ElseIf Barra = "#4" Then
            Ac = 129
        ElseIf Barra = "#5" Then
            Ac = 199
        ElseIf Barra = "#6" Then
            Ac = 284
        ElseIf Barra = "#7" Then
            Ac = 387
        ElseIf Barra = "#8" Then
            Ac = 510
        ElseIf Barra = "#10" Then
            Ac = 819
        ElseIf Barra = "None" Then
            Ac = 0
        Else
            Ac = 0
        End If
        AreaRefuerzo = Ac
    End Function
    Public Shared Function DiametroRefuerzo(ByVal Barra As String)
        Dim Db As Single
        If Barra = "#2" Then
            Db = 2 * 25.4 / 8000
        ElseIf Barra = "#3" Then
            Db = 3 * 25.4 / 8000
        ElseIf Barra = "#4" Then
            Db = 4 * 25.4 / 8000
        ElseIf Barra = "#5" Then
            Db = 5 * 25.4 / 8000
        ElseIf Barra = "#6" Then
            Db = 6 * 25.4 / 8000
        ElseIf Barra = "#7" Then
            Db = 7 * 25.4 / 8000
        ElseIf Barra = "#8" Then
            Db = 8 * 25.4 / 8000
        ElseIf Barra = "#10" Then
            Db = 10 * 25.4 / 8000
        Else
            'Db = Math.Sqrt(4 * Convert.ToSingle(AreaLongitudinal.Text) / Math.PI) / 1000
        End If
        DiametroRefuerzo = Db
    End Function

    Public Shared Sub FuncionColorCumple(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer, ByVal Opcion As String)

        If Opcion = "Cumple" Then
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 239, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(0, 97, 0)
        Else
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(255, 199, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(156, 0, 6)
        End If

    End Sub

    Public Shared Function DiseñoFlexion(ByVal Fy As Single, ByVal Fc As Single, ByVal Base As Single, ByVal H As Single, ByVal r As Single, ByVal Momento As Single, ByVal CuantiaMinima1 As Single, ByVal CuantiaMinima2 As Single) As Single
        Dim Ace As Single
        '------------------ VALORES DE ALFA Y BETA DE ACUERDO A LA RESISTENCIA DEL CONCRETO --------------------
        Dim alfa As Single
        Dim beta As Single
        Dim pmax As Single
        Dim pmin As Single
        If Fc < 28 Then
            alfa = 0.7225
            beta = 0.425
        Else
            alfa = 0.7225 - (0.04 * ((Fc - 28) / 7))
            beta = 0.425 - (0.025 * ((Fc - 28) / 7))
        End If
        '------------------VALORES MAXIMOS --------------------
        pmax = 0.75 * (alfa * (Fc / Fy)) * (600 / (600 + Fy))
        '------------------VALORES DE CUANTIA MINIMA --------------------
        Dim pmin_1 As Single = CuantiaMinima1
        Dim pmin_2 As Single = CuantiaMinima2
        '------------------REVISA LA CUANTIA MINIMA --------------------
        If pmin_1 >= pmin_2 Then
            pmin = pmin_1
        Else
            pmin = pmin_2
        End If
        '------------------CÁLCULO DE ACERO DE REFUERZO --------------------
        Dim Mom As Single = Momento * 1000000
        Dim B As Single = Base
        Dim d As Single = H - r
        Dim m As Single = Fy / (0.85 * Fc)
        Dim p As Single = (1 - (1 - (2 * m * (Mom / (0.9 * B * d * d)) / Fy)) ^ 0.5) / m
        '------------------CHEQUEO DE CUANTIA MINIMA --------------------
        Dim pmin_3 As Double = 1.33 * p
        If p <= pmin And pmin_3 < pmin Then
            p = pmin_3
        End If
        If p <= pmin And pmin_3 > pmin Then
            p = pmin
        End If
        Ace = p * B * d
        DiseñoFlexion = Ace
    End Function

    Public Shared Sub GuardarProyecto(ByVal Objeto As Object, ByVal Nombre_Archivo As String)
        Dim dlg As New SaveFileDialog
        dlg.Filter = "Archivo|*.esm"
        dlg.Title = "Guardar Archivo"
        dlg.FileName = Nombre_Archivo
        If dlg.ShowDialog() <> DialogResult.OK Then Return
        Try
            Objeto.Ruta = Path.GetFullPath(dlg.FileName)
            Funciones_Programa.Serializar(dlg.FileName, Objeto)
            MessageBox.Show("El archivo se guardó correctamente.", "Guardar",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al guardar el archivo: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Shared Sub AbrirImportarExcel()
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            .InitialDirectory = My.Computer.FileSystem.SpecialDirectories.Desktop
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Form_01_PagPilas.ImportExcellToDataGridView_CServicio(.FileName, Form_01_PagPilas.TablaCServicio)
                Form_01_PagPilas.ImportExcellToDataGridView_CUltimas(.FileName, Form_01_PagPilas.TablaCUltimas)
            End If
        End With
    End Sub
    Public Shared Sub CasillaCumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(198, 239, 206)
        Casilla.ForeColor = Color.FromArgb(0, 97, 0)
    End Sub
    Public Shared Sub CasillaNoCumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(255, 199, 206)
        Casilla.ForeColor = Color.FromArgb(156, 0, 6)
    End Sub

    Public Shared Sub EstiloTabla(ByVal Tabla As DataGridView)

        With Tabla.DefaultCellStyle
            .Font = New Font("Arial", 10)

            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With

        With Tabla.ColumnHeadersDefaultCellStyle
            .Font = New Font("Arial", 10, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub


    Public Shared Function ObtenerFZMaximoPorElemento(reacciones As List(Of cCombinacionPila),
                                                      combinacionesValidas As List(Of String),
                                                      labelElemento As String,
                                                      Optional buscarMin As Boolean = False,
                                                      Optional showDebug As Boolean = True) As Single



        If reacciones Is Nothing OrElse reacciones.Count = 0 Then Return 0
        If String.IsNullOrWhiteSpace(labelElemento) Then Return 0

        Dim labelNorm = labelElemento.Trim()


        ' 🔹 Paso 1: Filtramos TODAS las reacciones del elemento por JointLabel
        Dim delElemento = reacciones.
            Where(Function(r) Not String.IsNullOrWhiteSpace(r.JointLabel) AndAlso
                              String.Equals(r.JointLabel.Trim(), labelNorm, StringComparison.OrdinalIgnoreCase)).
            ToList()

        If delElemento.Count = 0 Then
            If showDebug Then Debug.Print($"❌ No se encontraron reacciones para el elemento '{labelElemento}'")
            Return 0
        End If

        ' 🔹 Paso 2: Filtramos las combinaciones válidas
        Dim combosSet As New HashSet(Of String)(combinacionesValidas.Select(Function(c) NormalizarClaveCombo(c)), StringComparer.OrdinalIgnoreCase)

        Dim filtradas = delElemento.
            Where(Function(r) Not String.IsNullOrWhiteSpace(r.LoadCase) AndAlso
                              combosSet.Contains(r.LoadCase.Trim())).
            ToList()

        If filtradas.Count = 0 Then
            If showDebug Then
                Debug.Print($"⚠️ No se encontraron combinaciones válidas para '{labelElemento}'.")
                Debug.Print("Combinaciones disponibles en el elemento: " &
                            String.Join(", ", delElemento.Select(Function(r) r.LoadCase).Distinct()))
                Debug.Print("Combinaciones válidas esperadas: " & String.Join(", ", combosSet))
            End If
            Return 0
        End If

        ' 🔹 Paso 3: Tomamos el máximo o mínimo de FZ
        Dim resultado As Single
        If buscarMin Then
            resultado = filtradas.Min(Function(r) r.FZ)
        Else
            resultado = filtradas.Max(Function(r) r.FZ)
        End If

        If showDebug Then
            Debug.Print($"✅ Elemento: {labelElemento}, Reacciones encontradas: {filtradas.Count}, Resultado FZ: {resultado}")
        End If

        Return resultado

        'Dim filtradas = reacciones.
        'Where(Function(r) String.Equals(r.UniqueName.Trim(), labelElemento.Trim(), StringComparison.OrdinalIgnoreCase) AndAlso
        '                combinacionesValidas.Contains(r.LoadCase)).ToList()

        '' 🔹 Si no hay datos, devolvemos 0
        'If filtradas.Count = 0 Then Return 0

        '' 🔹 Retornar el valor máximo o mínimo de FZ
        'If buscarMin Then
        '    Return filtradas.Min(Function(r) r.FZ)
        'Else
        '    Return filtradas.Max(Function(r) r.FZ)
        'End If

    End Function

    <Serializable>
    Public Structure Vector3
        Public X As Double
        Public Y As Double
        Public Z As Double

        Public Sub New(x As Double, y As Double, z As Double)
            Me.X = x
            Me.Y = y
            Me.Z = z
        End Sub

        Public Shared Operator -(a As Vector3, b As Vector3) As Vector3
            Return New Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z)
        End Operator

        Public Sub Normalize()
            Dim len = Math.Sqrt(X * X + Y * Y + Z * Z)
            If len = 0 Then Exit Sub
            X /= len : Y /= len : Z /= len
        End Sub

        Public Shared Function Dot(a As Vector3, b As Vector3) As Double
            Return a.X * b.X + a.Y * b.Y + a.Z * b.Z
        End Function

        Public Shared Function Cross(a As Vector3, b As Vector3) As Vector3
            Return New Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X)
        End Function

        Public ReadOnly Property Length As Double
            Get
                Return Math.Sqrt(X * X + Y * Y + Z * Z)
            End Get
        End Property
    End Structure



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

    ' =========================================================================
    ' Detección automática de candidatos a pila desde un Excel ETABS (E23).
    ' Identifica apoyos tipo Frame (via Joint Reactions) y tipo Pier (Pier Forces).
    ' La base de cada elemento se determina geométricamente, sin asumir un story fijo.
    ' toleranciaXY: distancia máxima en planta [m] para asociar Frame↔Apoyo.
    ' =========================================================================
    Public Shared Function DetectarCandidatosETABS(
            rutaArchivo As String,
            Optional toleranciaXY As Double = 0.5) As List(Of cCandidatoPila)

        Dim resultado As New List(Of cCandidatoPila)
        Try
            ' ── 1. Leer hojas ─────────────────────────────────────────────────
            Dim dtJoints = LeerHojaExcel(rutaArchivo, "Objects and Elements - Joints")
            Dim dtFrames = LeerHojaExcel(rutaArchivo, "Objects and Elements - Frames")
            Dim dtReact  = LeerHojaExcel(rutaArchivo, "Joint Reactions")
            Dim dtPierF  = LeerHojaExcel(rutaArchivo, "Pier Forces")
            Dim dtPierP  = LeerHojaExcel(rutaArchivo, "Pier Section Properties")

            ' ── 2. Construir diccionarios de joints ───────────────────────────
            ' cJoint.ElementLabel = "Element Name" (referenciado por frames)
            ' cJoint.ObjectLabel  = "Object Label" (referenciado por Joint Reactions)
            Dim joints As List(Of cJoint) = DataTableToJoints(dtJoints)

            Dim byElem As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
            Dim byLabel As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)

            For Each j As cJoint In joints
                Dim ex As cJoint = Nothing
                If Not byElem.TryGetValue(j.ElementLabel, ex) OrElse j.GlobalZ < ex.GlobalZ Then
                    byElem(j.ElementLabel) = j
                End If
                Dim exL As cJoint = Nothing
                If Not byLabel.TryGetValue(j.ObjectLabel, exL) OrElse j.GlobalZ < exL.GlobalZ Then
                    byLabel(j.ObjectLabel) = j
                End If
            Next

            ' ── 3. Identificar joints de apoyo desde Joint Reactions ──────────
            Dim colsR  = ETABSColDict(dtReact)
            Dim colLbl = GetColumnName(colsR, "Label")
            Dim colCas = GetColumnName(colsR, "Output Case")
            Dim colStp = GetColumnName(colsR, "Step Type")
            Dim colRFX = GetColumnName(colsR, "FX")
            Dim colRFY = GetColumnName(colsR, "FY")
            Dim colRFZ = GetColumnName(colsR, "FZ")
            Dim colRMX = GetColumnName(colsR, "MX")
            Dim colRMY = GetColumnName(colsR, "MY")
            Dim colRMZ = GetColumnName(colsR, "MZ")
            Dim colUNm = GetColumnName(colsR, "Unique Name")
            Dim colRSt = GetColumnName(colsR, "Story")

            Dim supportJoints As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
            If dtReact IsNot Nothing Then
                For Each row As DataRow In dtReact.Rows
                    Dim lbl = SafeString(row, colLbl)
                    If String.IsNullOrEmpty(lbl) OrElse supportJoints.ContainsKey(lbl) Then Continue For
                    Dim jt As cJoint = Nothing
                    If byLabel.TryGetValue(lbl, jt) Then supportJoints(lbl) = jt
                Next
            End If

            ' ── 4. Detectar candidatos Frame ──────────────────────────────────
            If dtFrames IsNot Nothing AndAlso supportJoints.Count > 0 Then
                Dim colsF  = ETABSColDict(dtFrames)
                Dim colFTp = GetColumnName(colsF, "Object Type")
                Dim colFLb = GetColumnName(colsF, "Object Label")
                Dim colJtI = GetColumnName(colsF, "Elm JtI")
                Dim colJtJ = GetColumnName(colsF, "Elm JtJ")

                ' Agrupar por support: guardar el frame con la base más baja (más cercana a la cimentación)
                Dim frameGroups As New Dictionary(Of String, Tuple(Of String, Double))(StringComparer.OrdinalIgnoreCase)

                For Each row As DataRow In dtFrames.Rows
                    If Not String.Equals(SafeString(row, colFTp), "Frame", StringComparison.OrdinalIgnoreCase) Then Continue For
                    Dim jtI = SafeString(row, colJtI)
                    Dim jtJ = SafeString(row, colJtJ)
                    If jtI.StartsWith("~") OrElse jtJ.StartsWith("~") Then Continue For
                    If String.IsNullOrEmpty(jtI) OrElse String.IsNullOrEmpty(jtJ) Then Continue For

                    Dim ptI As cJoint = Nothing : Dim ptJ As cJoint = Nothing
                    If Not byElem.TryGetValue(jtI, ptI) Then Continue For
                    If Not byElem.TryGetValue(jtJ, ptJ) Then Continue For

                    ' Filtrar vigas/elementos no verticales (ΔXY > 1m en planta)
                    Dim dxy = Math.Sqrt((ptI.GlobalX - ptJ.GlobalX) ^ 2 + (ptI.GlobalY - ptJ.GlobalY) ^ 2)
                    If dxy > 1.0 Then Continue For

                    ' Joint base = mínimo Z
                    Dim baseX, baseY, baseZ As Double
                    If ptI.GlobalZ <= ptJ.GlobalZ Then
                        baseX = ptI.GlobalX : baseY = ptI.GlobalY : baseZ = ptI.GlobalZ
                    Else
                        baseX = ptJ.GlobalX : baseY = ptJ.GlobalY : baseZ = ptJ.GlobalZ
                    End If

                    ' Joint de apoyo más cercano en XY
                    Dim bestLbl As String = Nothing
                    Dim bestDist As Double = Double.MaxValue
                    For Each kvp In supportJoints
                        Dim d = Math.Sqrt((kvp.Value.GlobalX - baseX) ^ 2 + (kvp.Value.GlobalY - baseY) ^ 2)
                        If d < bestDist Then bestDist = d : bestLbl = kvp.Key
                    Next
                    If bestLbl Is Nothing OrElse bestDist > toleranciaXY Then Continue For

                    Dim fLbl = SafeString(row, colFLb)
                    If String.IsNullOrEmpty(fLbl) Then fLbl = bestLbl
                    Dim ex As Tuple(Of String, Double) = Nothing
                    If Not frameGroups.TryGetValue(bestLbl, ex) OrElse baseZ < ex.Item2 Then
                        frameGroups(bestLbl) = Tuple.Create(fLbl, baseZ)
                    End If
                Next

                For Each kvp In frameGroups
                    Dim spt As cJoint = Nothing : supportJoints.TryGetValue(kvp.Key, spt)
                    Dim rxns = ETABSReaccionesJoint(dtReact, kvp.Key, colLbl, colCas, colStp,
                                                    colRFX, colRFY, colRFZ, colRMX, colRMY, colRMZ, colUNm, colRSt)
                    Dim c As New cCandidatoPila()
                    c.Nombre      = kvp.Value.Item1
                    c.Tipo        = "Frame"
                    c.SourceLabel = kvp.Key
                    c.Story       = If(spt IsNot Nothing, spt.Story, "")
                    c.X           = If(spt IsNot Nothing, spt.GlobalX, 0)
                    c.Y           = If(spt IsNot Nothing, spt.GlobalY, 0)
                    c.Z           = If(spt IsNot Nothing, spt.GlobalZ, 0)
                    c.Reactions   = rxns
                    If rxns.Count = 0 Then c.Estado = "Sin reacciones"
                    resultado.Add(c)
                Next
            End If

            ' ── 5. Detectar candidatos Pier ───────────────────────────────────
            If dtPierP IsNot Nothing Then
                Dim colsP  = ETABSColDict(dtPierP)
                Dim colPN  = GetColumnName(colsP, "Pier")
                Dim colPS  = GetColumnName(colsP, "Story")
                Dim colBX  = GetColumnName(colsP, "CG Bottom X")
                Dim colBY  = GetColumnName(colsP, "CG Bottom Y")
                Dim colBZ  = GetColumnName(colsP, "CG Bottom Z")

                ' Un registro por pier con mínima CG Bottom Z (su base estructural)
                Dim pierBase As New Dictionary(Of String, Tuple(Of String, Double, Double, Double))(StringComparer.OrdinalIgnoreCase)

                For Each row As DataRow In dtPierP.Rows
                    Dim pn = SafeString(row, colPN)
                    If String.IsNullOrEmpty(pn) Then Continue For
                    Dim ps = SafeString(row, colPS)
                    Dim px = SafeDouble(row, colBX)
                    Dim py = SafeDouble(row, colBY)
                    Dim pz = SafeDouble(row, colBZ)
                    Dim ex As Tuple(Of String, Double, Double, Double) = Nothing
                    If Not pierBase.TryGetValue(pn, ex) OrElse pz < ex.Item4 Then
                        pierBase(pn) = Tuple.Create(ps, px, py, pz)
                    End If
                Next

                For Each kvp In pierBase
                    Dim rxns = ETABSReaccionesPier(dtPierF, kvp.Key, kvp.Value.Item1)
                    Dim c As New cCandidatoPila()
                    c.Nombre      = kvp.Key
                    c.Tipo        = "Pier"
                    c.SourceLabel = kvp.Key
                    c.Story       = kvp.Value.Item1
                    c.X           = kvp.Value.Item2
                    c.Y           = kvp.Value.Item3
                    c.Z           = kvp.Value.Item4
                    c.Reactions   = rxns
                    If rxns.Count = 0 Then c.Estado = "Sin fuerzas"
                    resultado.Add(c)
                Next
            End If

        Catch ex As Exception
            Logger.Error(ex, "DetectarCandidatosETABS", "Error detectando apoyos ETABS")
        End Try
        Return resultado
    End Function

    Private Shared Function ETABSColDict(dt As DataTable) As Dictionary(Of String, String)
        If dt Is Nothing Then Return New Dictionary(Of String, String)()
        Return dt.Columns.Cast(Of DataColumn).ToDictionary(
            Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
            Function(c) c.ColumnName)
    End Function

    Private Shared Function ETABSReaccionesJoint(
            dt As DataTable, label As String,
            colLbl As String, colCas As String, colStp As String,
            colFX As String, colFY As String, colFZ As String,
            colMX As String, colMY As String, colMZ As String,
            colUniq As String, colSt As String) As List(Of cCombinacionPila)

        Dim lista As New List(Of cCombinacionPila)
        If dt Is Nothing Then Return lista
        For Each row As DataRow In dt.Rows
            If Not String.Equals(SafeString(row, colLbl), label, StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim r As New cCombinacionPila()
            r.JointLabel  = label
            r.Story       = SafeString(row, colSt)
            r.UniqueName  = SafeString(row, colUniq)
            Dim baseName  = SafeString(row, colCas)
            Dim stepVal   = SafeString(row, colStp)
            r.LoadCase    = If(Not String.IsNullOrEmpty(stepVal), baseName & " (" & stepVal & ")", baseName)
            r.FX          = CSng(SafeDouble(row, colFX))
            r.FY          = CSng(SafeDouble(row, colFY))
            r.FZ          = CSng(SafeDouble(row, colFZ))
            r.MX          = CSng(SafeDouble(row, colMX))
            r.MY          = CSng(SafeDouble(row, colMY))
            r.MZ          = CSng(SafeDouble(row, colMZ))
            r.SourceType  = "Frame"
            r.SourceName  = label
            lista.Add(r)
        Next
        Return lista
    End Function

    Private Shared Function ETABSReaccionesPier(dt As DataTable, pierName As String, storyBase As String) As List(Of cCombinacionPila)
        Dim lista As New List(Of cCombinacionPila)
        If dt Is Nothing Then Return lista
        Dim cols   = ETABSColDict(dt)
        Dim colPN  = GetColumnName(cols, "Pier")
        Dim colSt  = GetColumnName(cols, "Story")
        Dim colLoc = GetColumnName(cols, "Location")
        Dim colCas = GetColumnName(cols, "Output Case")
        Dim colStp = GetColumnName(cols, "Step Type")
        Dim colP   = GetColumnName(cols, "P")
        Dim colV2  = GetColumnName(cols, "V2")
        Dim colV3  = GetColumnName(cols, "V3")
        Dim colT   = GetColumnName(cols, "T")
        Dim colM2  = GetColumnName(cols, "M2")
        Dim colM3  = GetColumnName(cols, "M3")
        For Each row As DataRow In dt.Rows
            If Not String.Equals(SafeString(row, colPN), pierName, StringComparison.OrdinalIgnoreCase) Then Continue For
            If Not String.Equals(SafeString(row, colSt), storyBase, StringComparison.OrdinalIgnoreCase) Then Continue For
            If Not String.Equals(SafeString(row, colLoc), "Bottom", StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim r As New cCombinacionPila()
            r.JointLabel = pierName
            r.Story      = storyBase
            Dim baseName = SafeString(row, colCas)
            Dim stepVal  = SafeString(row, colStp)
            r.LoadCase   = If(Not String.IsNullOrEmpty(stepVal), baseName & " (" & stepVal & ")", baseName)
            ' Pier Forces: P negativo = compresión → se invierte para coincidir con Joint Reactions (FZ positivo = compresión)
            r.FZ         = CSng(-SafeDouble(row, colP))
            r.FX         = CSng(SafeDouble(row, colV2))
            r.FY         = CSng(SafeDouble(row, colV3))
            r.MX         = CSng(SafeDouble(row, colM3))
            r.MY         = CSng(SafeDouble(row, colM2))
            r.MZ         = CSng(SafeDouble(row, colT))
            r.SourceType = "Pier"
            r.SourceName = pierName
            lista.Add(r)
        Next
        Return lista
    End Function

End Class
