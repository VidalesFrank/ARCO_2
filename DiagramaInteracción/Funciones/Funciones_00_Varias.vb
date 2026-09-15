Imports System.IO
Imports System.Drawing

' Clase repartida en varios archivos parciales (mismo namespace ARCO, mismos miembros Shared):
'   Funciones\Funciones_00_Varias.vb                → utilidades generales, cálculo, persistencia
'   Servicios\Funciones_ExcelETABSImport.vb         → I/O Excel (OleDB + ExcelDataReader streaming)
'   Servicios\Funciones_DataTransformationETABS.vb  → DataTable ETABS → clases de dominio
'   Servicios\Funciones_Refuerzo.vb                 → AreaRefuerzo / DiametroRefuerzo
'   Servicios\Funciones_UIHelpers.vb                → FuncionColorCumple / CasillaCumple / EstiloTabla
Partial Public Class Funciones_00_Varias

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

    ' ═══════════════════════════════════════════════════════════════════════
    ' Detección de pilas desde Element Forces - Columns (fuerzas de elemento)
    ' Lee la sección gobernante por máximo √(M2²+M3²) por cada combo.
    ' Incluye geometría estructural (joints + frames) para vista en planta.
    ' ═══════════════════════════════════════════════════════════════════════
    Public Shared Function DetectarPilasDesdeElementForces(
            rutaArchivo As String,
            ByRef backdrop As GeometriaEstructural) As List(Of cCandidatoPila)

        Dim resultado As New List(Of cCandidatoPila)
        backdrop = New GeometriaEstructural()

        Try
            ' ── 0a. Secciones circulares: detectar cuáles son pilas ────────────
            '  Lee Frame Sec Def - Conc Circle → diccionario {sectionName → diameter}
            Dim pilaSecDiam As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
            Dim dtCircle = LeerHojaExcel(rutaArchivo, "Frame Sec Def - Conc Circle")
            If dtCircle Is Nothing Then
                dtCircle = LeerHojaExcel(rutaArchivo, "Frame Section Property Data - Concrete Circle")
            End If
            If dtCircle IsNot Nothing Then
                Dim colsC  = ETABSColDict(dtCircle)
                Dim colCNm = GetColumnName(colsC, "Name")
                Dim colCDi = GetColumnName(colsC, "Diameter")
                For Each row As DataRow In dtCircle.Rows
                    Dim sNm = SafeString(row, colCNm)
                    If Not String.IsNullOrEmpty(sNm) Then
                        pilaSecDiam(sNm) = SafeDouble(row, colCDi)
                    End If
                Next
            End If

            ' ── 0b. Asignación de sección por Label ────────────────────────────
            '  Lee Frame Assigns - Sect Prop → {label → sectionName} solo para secciones circulares
            Dim labelSection As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim dtAssign = LeerHojaExcel(rutaArchivo, "Frame Assigns - Sect Prop")
            If dtAssign Is Nothing Then
                dtAssign = LeerHojaExcel(rutaArchivo, "Frame Section Assignments")
            End If
            If dtAssign IsNot Nothing Then
                Dim colsA  = ETABSColDict(dtAssign)
                Dim colALb = GetColumnName(colsA, "Label")
                If String.IsNullOrEmpty(colALb) Then colALb = GetColumnName(colsA, "Object Label")
                Dim colASc = GetColumnName(colsA, "Section Property")
                For Each row As DataRow In dtAssign.Rows
                    Dim lb  = SafeString(row, colALb)
                    Dim sec = SafeString(row, colASc)
                    If String.IsNullOrEmpty(lb) OrElse String.IsNullOrEmpty(sec) Then Continue For
                    If pilaSecDiam.ContainsKey(sec) AndAlso Not labelSection.ContainsKey(lb) Then
                        labelSection(lb) = sec
                    End If
                Next
            End If
            ' Si no se detectaron secciones circulares, incluir todos los elementos (fallback)
            Dim filtrarPorSeccion As Boolean = (labelSection.Count > 0)

            ' ── 1. Geometría de joints y frames para backdrop ──────────────────
            Dim dtJoints = LeerHojaExcel(rutaArchivo, "Objects and Elements - Joints")
            Dim dtObjFrm = LeerHojaExcel(rutaArchivo, "Objects and Elements - Frames")

            Dim joints = DataTableToJoints(dtJoints)

            Dim byElem As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
            For Each j As cJoint In joints
                Dim ex As cJoint = Nothing
                If Not byElem.TryGetValue(j.ElementLabel, ex) OrElse j.GlobalZ > ex.GlobalZ Then
                    byElem(j.ElementLabel) = j
                End If
                backdrop.JointsXY.Add(New PointF(CSng(j.GlobalX), CSng(j.GlobalY)))
            Next

            ' Líneas de Frame para backdrop + mapa elementLabel → coords planta + Z range
            Dim elemToXY  As New Dictionary(Of String, PointF)(StringComparer.OrdinalIgnoreCase)
            Dim labelZMin As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
            Dim labelZMax As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
            Dim labelSeg  As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            If dtObjFrm IsNot Nothing Then
                Dim colsF  = ETABSColDict(dtObjFrm)
                Dim colFTp = GetColumnName(colsF, "Object Type")
                Dim colFLb = GetColumnName(colsF, "Object Label")
                Dim colJtI = GetColumnName(colsF, "Elm JtI")
                Dim colJtJ = GetColumnName(colsF, "Elm JtJ")

                For Each row As DataRow In dtObjFrm.Rows
                    If Not String.Equals(SafeString(row, colFTp), "Frame", StringComparison.OrdinalIgnoreCase) Then Continue For
                    Dim fLbl = SafeString(row, colFLb)
                    Dim jtI  = SafeString(row, colJtI)
                    Dim jtJ  = SafeString(row, colJtJ)
                    If jtI.StartsWith("~") OrElse jtJ.StartsWith("~") Then Continue For

                    Dim ptI As cJoint = Nothing, ptJ As cJoint = Nothing
                    If Not byElem.TryGetValue(jtI, ptI) OrElse Not byElem.TryGetValue(jtJ, ptJ) Then Continue For

                    backdrop.FramesXY.Add(Tuple.Create(
                        New PointF(CSng(ptI.GlobalX), CSng(ptI.GlobalY)),
                        New PointF(CSng(ptJ.GlobalX), CSng(ptJ.GlobalY))))

                    If Not String.IsNullOrEmpty(fLbl) AndAlso Not elemToXY.ContainsKey(fLbl) Then
                        Dim topPt = If(ptI.GlobalZ >= ptJ.GlobalZ,
                                       New PointF(CSng(ptI.GlobalX), CSng(ptI.GlobalY)),
                                       New PointF(CSng(ptJ.GlobalX), CSng(ptJ.GlobalY)))
                        elemToXY(fLbl) = topPt
                    End If

                    ' Rango Z y número de segmentos por label (para calcular profundidad total)
                    If Not String.IsNullOrEmpty(fLbl) Then
                        Dim zI = ptI.GlobalZ, zJ = ptJ.GlobalZ
                        Dim zLow = Math.Min(zI, zJ), zHigh = Math.Max(zI, zJ)
                        labelZMin(fLbl) = Math.Min(zLow,  If(labelZMin.ContainsKey(fLbl), labelZMin(fLbl), Double.MaxValue))
                        labelZMax(fLbl) = Math.Max(zHigh, If(labelZMax.ContainsKey(fLbl), labelZMax(fLbl), Double.MinValue))
                        Dim nS As Integer = 0
                        labelSeg(fLbl) = If(labelSeg.TryGetValue(fLbl, nS), nS + 1, 1)
                    End If
                Next
            End If

            ' ── 2. Leer hoja de fuerzas ────────────────────────────────────────
            Dim dtForces = LeerHojaExcel(rutaArchivo, "Element Forces - Columns")
            If dtForces Is Nothing OrElse dtForces.Rows.Count = 0 Then
                dtForces = LeerHojaExcel(rutaArchivo, "Column Forces")
            End If
            If dtForces Is Nothing OrElse dtForces.Rows.Count = 0 Then
                Logger.Warning("DetectarPilasDesdeElementForces",
                               "No se encontró 'Element Forces - Columns' ni 'Column Forces'")
                Return resultado
            End If

            ' ── 3. Nombres de columnas ─────────────────────────────────────────
            Dim cols     = ETABSColDict(dtForces)
            Dim colStory = GetColumnName(cols, "Story")
            Dim colElem  = GetColumnName(cols, "Column")
            If String.IsNullOrEmpty(colElem) Then colElem = GetColumnName(cols, "Frame")
            Dim colUniq  = GetColumnName(cols, "Unique Name")
            Dim colCase  = GetColumnName(cols, "Output Case")
            If String.IsNullOrEmpty(colCase) Then colCase = GetColumnName(cols, "Load Case/Combo")
            Dim colStep  = GetColumnName(cols, "Step Type")
            Dim colP     = GetColumnName(cols, "P")
            Dim colV2    = GetColumnName(cols, "V2")
            Dim colV3    = GetColumnName(cols, "V3")
            Dim colM2    = GetColumnName(cols, "M2")
            Dim colM3    = GetColumnName(cols, "M3")

            If String.IsNullOrEmpty(colElem) OrElse String.IsNullOrEmpty(colP) Then
                Logger.Warning("DetectarPilasDesdeElementForces",
                               "Columnas 'Column'/'P' no encontradas en la hoja de fuerzas")
                Return resultado
            End If

            ' ── 4. Agrupar filas por Label de pila ─────────────────────────────
            '   La columna "Column" (colElem) contiene el Label de la pila (p. ej. "C1")
            '   que es CONSTANTE en todos los tramos/stories de la misma pila.
            '   Si se detectaron secciones circulares, se filtran solo esos labels.
            Dim byElement As New Dictionary(Of String, List(Of DataRow))(StringComparer.OrdinalIgnoreCase)
            For Each row As DataRow In dtForces.Rows
                Dim en = SafeString(row, colElem)
                If String.IsNullOrEmpty(en) Then Continue For
                If filtrarPorSeccion AndAlso Not labelSection.ContainsKey(en) Then Continue For
                If Not byElement.ContainsKey(en) Then byElement(en) = New List(Of DataRow)
                byElement(en).Add(row)
            Next

            ' ── 5. Construir cCandidatoPila por elemento ───────────────────────
            For Each kvp In byElement
                Dim elemName As String = kvp.Key
                Dim rows     As List(Of DataRow) = kvp.Value

                ' Por cada combinación: guardar la estación gobernante (max |M| resultante)
                Dim byCaseName As New Dictionary(Of String, DataRow)(StringComparer.OrdinalIgnoreCase)
                Dim storyName  As String = ""

                For Each row In rows
                    Dim caseName = SafeString(row, colCase)
                    Dim stepType = SafeString(row, colStep)
                    Dim fullCase = If(Not String.IsNullOrEmpty(stepType),
                                     caseName & " (" & stepType & ")", caseName)
                    If String.IsNullOrEmpty(storyName) Then storyName = SafeString(row, colStory)

                    Dim m2Val = SafeDouble(row, colM2)
                    Dim m3Val = SafeDouble(row, colM3)
                    Dim mAbs  = Math.Sqrt(m2Val * m2Val + m3Val * m3Val)

                    Dim existing As DataRow = Nothing
                    If Not byCaseName.TryGetValue(fullCase, existing) Then
                        byCaseName(fullCase) = row
                    Else
                        Dim m2E   = SafeDouble(existing, colM2)
                        Dim m3E   = SafeDouble(existing, colM3)
                        If mAbs > Math.Sqrt(m2E * m2E + m3E * m3E) Then
                            byCaseName(fullCase) = row
                        End If
                    End If
                Next

                ' Construir lista de reacciones desde estaciones gobernantes
                Dim reactions As New List(Of cCombinacionPila)
                For Each kCase In byCaseName
                    Dim row = kCase.Value
                    Dim rxn As New cCombinacionPila()
                    rxn.Story      = SafeString(row, colStory)
                    rxn.JointLabel = elemName
                    rxn.UniqueName = SafeString(row, colUniq)
                    rxn.LoadCase   = kCase.Key
                    rxn.SourceType = "Frame"
                    rxn.SourceName = elemName
                    ' ETABS P negativo=compresión → FZ positivo=compresión (mismo convenio que Joint Reactions)
                    rxn.FZ = CSng(-SafeDouble(row, colP))
                    rxn.FX = CSng(SafeDouble(row, colV2))
                    rxn.FY = CSng(SafeDouble(row, colV3))
                    rxn.MX = CSng(SafeDouble(row, colM2))
                    rxn.MY = CSng(SafeDouble(row, colM3))
                    rxn.MZ = 0
                    reactions.Add(rxn)
                Next

                ' Coordenadas en planta y metadata de la pila
                Dim coord As PointF = PointF.Empty
                elemToXY.TryGetValue(elemName, coord)

                Dim zTop As Double = 0, zBot As Double = 0
                labelZMax.TryGetValue(elemName, zTop)
                labelZMin.TryGetValue(elemName, zBot)
                Dim nSeg As Integer = 1
                labelSeg.TryGetValue(elemName, nSeg)
                Dim secNom As String = ""
                labelSection.TryGetValue(elemName, secNom)
                Dim diam As Double = 0
                If Not String.IsNullOrEmpty(secNom) Then pilaSecDiam.TryGetValue(secNom, diam)

                Dim cand As New cCandidatoPila()
                cand.Nombre        = elemName
                cand.Tipo          = "Frame"
                cand.SourceLabel   = elemName
                cand.Story         = If(nSeg > 1, $"Z {zBot:F1}→{zTop:F1} m", storyName)
                cand.X             = coord.X
                cand.Y             = coord.Y
                cand.Z             = zTop
                cand.ZTop          = zTop
                cand.ZBottom       = zBot
                cand.NumSegmentos  = nSeg
                cand.SeccionNombre = secNom
                cand.Diametro      = diam
                cand.Reactions     = reactions
                cand.Seleccionado  = True
                If reactions.Count = 0 Then cand.Estado = "Sin fuerzas"

                resultado.Add(cand)
            Next

        Catch ex As Exception
            Logger.Error(ex, "DetectarPilasDesdeElementForces", "Error leyendo fuerzas de elementos Frame")
        End Try

        Return resultado
    End Function

End Class

''' <summary>
''' Geometría de la estructura exportada desde ETABS (joints y frames),
''' usada como backdrop visual en el formulario de importación de pilas.
''' </summary>
Public Class GeometriaEstructural
    Public Property JointsXY As New List(Of PointF)
    Public Property FramesXY As New List(Of Tuple(Of PointF, PointF))
End Class
