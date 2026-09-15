' Partial de Funciones_00_Varias: transformación DataTable (Excel de ETABS) → clases de dominio.
' Cada método toma una tabla cruda y devuelve una lista tipada de la entidad correspondiente.
' Los helpers GetColumnName/SafeString/SafeDouble son privados y compartidos por todos los DataTableTo*.
Partial Public Class Funciones_00_Varias

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

            ' 🔁 Último recurso: tomar cualquier 2 joints distintos no vacíos.
            ' Cubre frames con filas duplicadas o con todos los joints compartidos
            ' (p.ej. pisos de transferencia o exportaciones E23 con mesh doble).
            If extremos.Count < 2 Then
                extremos = nodos.Where(Function(j) Not String.IsNullOrEmpty(j)) _
                                .Distinct() _
                                .Take(2) _
                                .ToList()
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

        ' Detectar si la tabla tiene columnas de tipo General (X1,Y1,X2,Y2)
        Dim tieneCoordGenerales As Boolean = dt.Columns.Contains("X1") AndAlso dt.Columns.Contains("Y1") AndAlso
                                             dt.Columns.Contains("X2") AndAlso dt.Columns.Contains("Y2")

        For Each r As DataRow In dt.Rows

            Dim tipoRaw As String = r("Grid Line Type").ToString().Trim()
            Dim esGeneral As Boolean = tipoRaw.ToLower().StartsWith("general")

            If esGeneral Then
                ' ── General (Cartesian): coordenadas X1,Y1 → X2,Y2 ──────────────────
                If Not tieneCoordGenerales Then Continue For
                Dim x1v, y1v, x2v, y2v As Double
                Double.TryParse(r("X1").ToString(), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, x1v)
                Double.TryParse(r("Y1").ToString(), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, y1v)
                Double.TryParse(r("X2").ToString(), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, x2v)
                Double.TryParse(r("Y2").ToString(), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, y2v)
                If x1v = x2v AndAlso y1v = y2v Then Continue For ' segmento degenerado
                lista.Add(New cGridLine With {
                    .GridSystem = r("Name").ToString(),
                    .Direction = "G",
                    .GridID = r("ID").ToString().Trim(),
                    .Visible = r("Visible").ToString().Trim().ToLower() = "yes",
                    .BubbleLocation = r("Bubble Location").ToString().Trim(),
                    .X1 = x1v, .Y1 = y1v, .X2 = x2v, .Y2 = y2v
                })
            Else
                ' ── X (Cartesian) / Y (Cartesian): ordenada única ───────────────────
                If IsDBNull(r("Ordinate")) OrElse String.IsNullOrWhiteSpace(r("Ordinate").ToString()) Then Continue For
                lista.Add(New cGridLine With {
                    .GridSystem = r("Name").ToString(),
                    .Direction = tipoRaw(0).ToString().ToUpper(),
                    .GridID = r("ID").ToString().Trim(),
                    .Visible = r("Visible").ToString().Trim().ToLower() = "yes",
                    .BubbleLocation = r("Bubble Location").ToString().Trim(),
                    .Ordinate = CDbl(r("Ordinate"))
                })
            End If
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

End Class
