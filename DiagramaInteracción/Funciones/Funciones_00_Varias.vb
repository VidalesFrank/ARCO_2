Imports System.Data.OleDb
Imports System.IO

Public Class Funciones_00_Varias


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

    ' 🔸 Función auxiliar para buscar nombres de columna de forma flexible
    Private Shared Function GetColumnName(cols As Dictionary(Of String, String), key As String) As String
        key = key.Trim().ToLower()
        For Each c In cols.Keys
            If c.Contains(key.Replace(" ", "")) OrElse c = key Then
                Return cols(c)
            End If
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

    Public Shared Function DataTableToFrames(dt As DataTable) As List(Of cFrame)

        Dim lista As New List(Of cFrame)
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
        Dim colJointI = GetColumnName(columnas, "Elm JtI")
        Dim colJointJ = GetColumnName(columnas, "Elm JtJ")

        ' 🔹 Recorremos cada fila
        For Each r As DataRow In dt.Rows

            Dim tipo As String = SafeString(r, colObjectType)
            If Not String.Equals(tipo, "Frame", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            Dim f As New cFrame()

            f.Story = SafeString(r, colStory)
            f.ElementLabel = SafeString(r, colElementLabel)
            f.ObjectType = SafeString(r, colObjectType)
            f.ObjectLabel = SafeString(r, colObjectLabel)
            f.JointI = SafeString(r, colJointI)
            f.JointJ = SafeString(r, colJointJ)

            lista.Add(f)
        Next

        Return lista
    End Function

    Public Shared Function DataTableToReactions(dt As DataTable) As List(Of cCombinacionPila)

        Dim lista As New List(Of cCombinacionPila)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return lista

        ' 🔹 Normalizar nombres de columnas
        Dim columnas = dt.Columns.Cast(Of DataColumn).ToDictionary(
        Function(c) c.ColumnName.Trim().Replace(vbCrLf, "").Replace(vbLf, "").Replace(vbTab, "").ToLower(),
        Function(c) c.ColumnName
    )

        ' 🔹 Buscar las columnas esperadas
        Dim colStory = GetColumnName(columnas, "Story")
        Dim colJointLabel = GetColumnName(columnas, "Joint Label")
        Dim colUniqueName = GetColumnName(columnas, "Unique Name")
        Dim colLoadCase = GetColumnName(columnas, "Load Case/Combo")
        Dim colFx = GetColumnName(columnas, "FX")
        Dim colFy = GetColumnName(columnas, "FY")
        Dim colFz = GetColumnName(columnas, "FZ")
        Dim colMx = GetColumnName(columnas, "MX")
        Dim colMy = GetColumnName(columnas, "MY")
        Dim colMz = GetColumnName(columnas, "MZ")

        ' 🔹 Recorremos cada fila
        For Each r As DataRow In dt.Rows

            Dim j As New cCombinacionPila()

            j.Story = SafeString(r, colStory)
            j.JointLabel = SafeString(r, colJointLabel)
            j.UniqueName = SafeString(r, colUniqueName)
            j.LoadCase = SafeString(r, colLoadCase)
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

    Public Shared Sub Funcion_Color_Cumple(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer, ByVal Opcion As String)

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
        Try
            Dim SaveAs As New SaveFileDialog
            SaveAs.Filter = "Archivo|*.esm"
            SaveAs.Title = "Guardar Archivo"
            SaveAs.FileName = Nombre_Archivo
            SaveAs.ShowDialog()
            If SaveAs.FileName <> String.Empty Then
                Objeto.Ruta = Path.GetFullPath(SaveAs.FileName)
                Funciones_Programa.Serializar(SaveAs.FileName, Objeto)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Public Shared Sub Abrir_Importar_Excel()
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
    Public Shared Sub Casilla_Cumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(198, 239, 206)
        Casilla.ForeColor = Color.FromArgb(0, 97, 0)
    End Sub
    Public Shared Sub Casilla_Nocumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(255, 199, 206)
        Casilla.ForeColor = Color.FromArgb(156, 0, 6)
    End Sub

    Public Shared Sub Estilo_Tabla(ByVal Tabla As DataGridView)

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


        ' 🔹 Paso 1: Filtramos TODAS las reacciones del elemento
        Dim delElemento = reacciones.
            Where(Function(r) Not String.IsNullOrWhiteSpace(r.UniqueName) AndAlso
                              String.Equals(r.UniqueName.Trim(), labelNorm, StringComparison.OrdinalIgnoreCase)).
            ToList()

        ' Si no encontró por UniqueName, intentamos con JointLabel
        If delElemento.Count = 0 Then
            delElemento = reacciones.
                Where(Function(r) Not String.IsNullOrWhiteSpace(r.JointLabel) AndAlso
                                  String.Equals(r.JointLabel.Trim(), labelNorm, StringComparison.OrdinalIgnoreCase)).
                ToList()
        End If

        If delElemento.Count = 0 Then
            If showDebug Then Debug.Print($"❌ No se encontraron reacciones para el elemento '{labelElemento}'")
            Return 0
        End If

        ' 🔹 Paso 2: Filtramos las combinaciones válidas
        Dim combosSet As New HashSet(Of String)(combinacionesValidas.Select(Function(c) c.Trim()), StringComparer.OrdinalIgnoreCase)

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


End Class
