Imports System.Linq
Imports ARCO.Funciones_00_Varias

' =========================================================================
' ActualizadorColumnas — re-leer un Excel ETABS actualizado sin perder
' refuerzo, combinaciones seleccionadas ni distribución personalizada de
' barras. Análogo al "Actualizar demandas" de vigas, extendido a Frame + Pier
' y a cambios de sección.
'
' Flujo:
'   1. Leer(path) → DatosExcelColumnas con los DataTables ya parseados a
'      diccionarios por label.
'   2. Reconciliar(datos, proyecto) → ReporteReconciliacion con las
'      diferencias contra el proyecto actual (cambios de sección, labels
'      nuevos, labels sin Excel, combinaciones nuevas).
'   3. Aplicar(datos, proyecto, labelsNuevosAceptados) → ejecuta la
'      actualización: mantiene refuerzo y lo manual, reemplaza propiedades
'      geométricas y fuerzas, agrega columnas nuevas si el usuario las
'      aceptó, añade las combinaciones nuevas al proyecto.
'
' No recalcula el chequeo de flexo-compresión (DI), cortante, confinamiento
' ni ALR por sí mismo: eso lo dispara el usuario al abrir Form_02_00 (que
' recalcula al entrar). Lo que SÍ se actualiza por envolvente es
' tramo.V2/V3/Pu_V2/Pu_V3 — el cortante toma de ahí la demanda.
' =========================================================================
Public Class ActualizadorColumnas

    ' Fuerza individual por estación (tal como viene de ETABS) agrupada por
    ' combinación. La envolvente por combo se construye en Aplicar.
    Public Class FuerzaCruda
        Public Property Combo As String
        Public Property P As Single
        Public Property V2 As Single
        Public Property V3 As Single
        Public Property T As Single
        Public Property M2 As Single
        Public Property M3 As Single
    End Class

    ' Datos geométricos de un tramo leído del Excel (una columna puede tener
    ' varios tramos — típicamente dos — porque ETABS corta por piso).
    Public Class TramoDatos
        Public Property Piso As String
        Public Property Seccion As String
        Public Property B_Modelo As Single
        Public Property H_Modelo As Single
        Public Property fc As Single
        ' Frame: As_Req directo en mm² (columna 10 de Conc Col Sum).
        ' Pier:  guardamos la cuantía en % (columna 9 de Pier Dgn Sum) y en
        '        Aplicar recalculamos As_Req a partir de b·h·cuantia.
        Public Property As_Req_Top As Single
        Public Property As_Req_Bottom As Single
        Public Property CuantiaBottomPct As Single
        Public Property CuantiaTopPct As Single
        ' Envolvente por combo (máximo |valor| por cada componente), luego
        ' Aplicar la vuelca en tramo.Lista_Combinaciones.
        Public Property FuerzasEnvolventePorCombo As New Dictionary(Of String, FuerzaCruda)(StringComparer.OrdinalIgnoreCase)
    End Class

    ' Snapshot del Excel reorganizado por label. Cada entrada representa una
    ' columna; sus tramos están en orden "Bottom → Top" (igual que en el proyecto).
    Public Class DatosExcelColumnas
        ' Clave: Name_Elemento (ObjectLabel del frame o nombre del pier)
        Public Property ColumnasFrame As New Dictionary(Of String, List(Of TramoDatos))(StringComparer.OrdinalIgnoreCase)
        Public Property ColumnasPier As New Dictionary(Of String, List(Of TramoDatos))(StringComparer.OrdinalIgnoreCase)
        Public Property CombinacionesLeidas As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        ' Para validar que el archivo trae lo que decimos que trae
        Public Property TieneDisenoFrame As Boolean
        Public Property TieneSeccionesFrame As Boolean
        Public Property TieneFuerzasFrame As Boolean
        Public Property TieneDisenoPier As Boolean
        Public Property TieneSeccionesPier As Boolean
        Public Property TieneFuerzasPier As Boolean
    End Class

    ' Resultado de reconciliar el Excel contra el proyecto.
    Public Class CambioSeccion
        Public Property Label As String
        Public Property Piso As String
        Public Property Antes As String  ' "0.40×0.60, fc=28"
        Public Property Despues As String
    End Class

    Public Class ReporteReconciliacion
        Public Property CambiosSeccionFrame As New List(Of CambioSeccion)
        Public Property CambiosSeccionPier As New List(Of CambioSeccion)
        Public Property LabelsNuevosFrame As New List(Of String)
        Public Property LabelsNuevosPier As New List(Of String)
        Public Property LabelsSinExcelFrame As New List(Of String)
        Public Property LabelsSinExcelPier As New List(Of String)
        Public Property CombinacionesNuevas As New List(Of String)
        Public Property ColumnasConFuerzasActualizadas As Integer
        Public Property TieneCambios As Boolean

        Public ReadOnly Property Resumen As String
            Get
                Dim sb As New System.Text.StringBuilder()
                sb.AppendLine("Comparación contra el proyecto actual:")
                sb.AppendLine()
                If CambiosSeccionFrame.Count > 0 OrElse CambiosSeccionPier.Count > 0 Then
                    sb.AppendLine($"• Secciones cambiadas: {CambiosSeccionFrame.Count + CambiosSeccionPier.Count}")
                End If
                If ColumnasConFuerzasActualizadas > 0 Then
                    sb.AppendLine($"• Columnas con fuerzas nuevas: {ColumnasConFuerzasActualizadas}")
                End If
                If LabelsNuevosFrame.Count > 0 Then
                    sb.AppendLine($"• Frames nuevos (sin refuerzo): {LabelsNuevosFrame.Count}  →  " &
                                  String.Join(", ", LabelsNuevosFrame.Take(8)) &
                                  If(LabelsNuevosFrame.Count > 8, "...", ""))
                End If
                If LabelsNuevosPier.Count > 0 Then
                    sb.AppendLine($"• Piers nuevos: {LabelsNuevosPier.Count}  →  " &
                                  String.Join(", ", LabelsNuevosPier.Take(8)) &
                                  If(LabelsNuevosPier.Count > 8, "...", ""))
                End If
                If LabelsSinExcelFrame.Count > 0 Then
                    sb.AppendLine($"• Frames del proyecto sin datos nuevos: {LabelsSinExcelFrame.Count}  →  " &
                                  String.Join(", ", LabelsSinExcelFrame.Take(8)) &
                                  If(LabelsSinExcelFrame.Count > 8, "...", "") &
                                  "  (se dejan intactos)")
                End If
                If LabelsSinExcelPier.Count > 0 Then
                    sb.AppendLine($"• Piers del proyecto sin datos nuevos: {LabelsSinExcelPier.Count}  →  " &
                                  String.Join(", ", LabelsSinExcelPier.Take(8)) &
                                  If(LabelsSinExcelPier.Count > 8, "...", "") &
                                  "  (se dejan intactos)")
                End If
                If CombinacionesNuevas.Count > 0 Then
                    sb.AppendLine($"• Combinaciones nuevas: {CombinacionesNuevas.Count}")
                End If
                If sb.Length = "Comparación contra el proyecto actual:".Length + Environment.NewLine.Length * 2 Then
                    sb.AppendLine("Sin cambios detectados.")
                End If
                Return sb.ToString()
            End Get
        End Property
    End Class

    ' =====================================================================
    '  LECTURA — Excel → DatosExcelColumnas
    ' =====================================================================

    ''' <summary>
    ''' Lee las 6 hojas relevantes (3 Frame + 3 Pier) del Excel. Las hojas
    ''' ausentes se saltan con flag en TieneXxx para que Aplicar sepa qué
    ''' puede/no puede tocar. No lanza por hojas faltantes: un Excel que
    ''' solo trae Pier (o solo Frame) es perfectamente válido.
    ''' </summary>
    Public Shared Function Leer(rutaArchivo As String) As DatosExcelColumnas

        Dim datos As New DatosExcelColumnas
        Dim hojas = ObtenerHojasExcel(rutaArchivo)
        If hojas Is Nothing OrElse hojas.Count = 0 Then Return datos

        ' --- FRAME ------------------------------------------------------
        Dim hDisF = BuscarHoja(hojas, "Conc Col Sum", "Concrete Column Summary")
        If hDisF IsNot Nothing Then
            Try
                LeerDisenoFrame(LeerHojaExcel(rutaArchivo, hDisF), datos.ColumnasFrame)
                datos.TieneDisenoFrame = True
            Catch ex As Exception
                Logger.Warning("ActualizadorColumnas.Leer", $"Diseño Frame '{hDisF}': {ex.Message}")
            End Try
        End If

        Dim hSecF = BuscarHoja(hojas, "Frame Sec Def - Conc Rect", "Frame Sections")
        If hSecF IsNot Nothing AndAlso datos.ColumnasFrame.Count > 0 Then
            Try
                LeerSeccionesFrame(LeerHojaExcel(rutaArchivo, hSecF), datos.ColumnasFrame)
                datos.TieneSeccionesFrame = True
            Catch ex As Exception
                Logger.Warning("ActualizadorColumnas.Leer", $"Secciones Frame '{hSecF}': {ex.Message}")
            End Try
        End If

        Dim hFzF = BuscarHoja(hojas, "Element Forces - Columns", "Column Forces")
        If hFzF IsNot Nothing AndAlso datos.ColumnasFrame.Count > 0 Then
            Try
                LeerFuerzasFrame(LeerHojaExcel(rutaArchivo, hFzF), datos.ColumnasFrame, datos.CombinacionesLeidas)
                datos.TieneFuerzasFrame = True
            Catch ex As Exception
                Logger.Warning("ActualizadorColumnas.Leer", $"Fuerzas Frame '{hFzF}': {ex.Message}")
            End Try
        End If

        ' --- PIER -------------------------------------------------------
        Dim hDisP = BuscarHoja(hojas, "Pier Dgn Sum", "Shear Wall Pier Summary")
        If hDisP IsNot Nothing Then
            Try
                LeerDisenoPier(LeerHojaExcel(rutaArchivo, hDisP), datos.ColumnasPier)
                datos.TieneDisenoPier = True
            Catch ex As Exception
                Logger.Warning("ActualizadorColumnas.Leer", $"Diseño Pier '{hDisP}': {ex.Message}")
            End Try
        End If

        Dim hSecP = BuscarHoja(hojas, "Pier Section Properties")
        If hSecP IsNot Nothing AndAlso datos.ColumnasPier.Count > 0 Then
            Try
                LeerSeccionesPier(LeerHojaExcel(rutaArchivo, hSecP), datos.ColumnasPier)
                datos.TieneSeccionesPier = True
            Catch ex As Exception
                Logger.Warning("ActualizadorColumnas.Leer", $"Secciones Pier '{hSecP}': {ex.Message}")
            End Try
        End If

        Dim hFzP = BuscarHoja(hojas, "Pier Forces")
        If hFzP IsNot Nothing AndAlso datos.ColumnasPier.Count > 0 Then
            Try
                LeerFuerzasPier(LeerHojaExcel(rutaArchivo, hFzP), datos.ColumnasPier, datos.CombinacionesLeidas)
                datos.TieneFuerzasPier = True
            Catch ex As Exception
                Logger.Warning("ActualizadorColumnas.Leer", $"Fuerzas Pier '{hFzP}': {ex.Message}")
            End Try
        End If

        Return datos

    End Function

    Private Shared Function BuscarHoja(hojas As List(Of String), ParamArray candidatos As String()) As String
        For Each c In candidatos
            Dim match = hojas.FirstOrDefault(Function(h) h.Contains(c))
            If match IsNot Nothing Then Return match.TrimEnd("$"c)
        Next
        Return Nothing
    End Function

    ' ─────── Diseño Frame ────────────────────────────────────────────────
    ' Conc Col Sum - ACI 318-14 (E23) / Concrete Column Summary (E17)
    ' Columnas canónicas: Story[0] Label[1] (UniqueName) Section[3] Station[4] ... As_Req[10]
    ' LeerHojaExcel ya saltó encabezados/unidades, así que filas = datos.
    Private Shared Sub LeerDisenoFrame(dt As DataTable, destino As Dictionary(Of String, List(Of TramoDatos)))
        If dt Is Nothing OrElse dt.Rows.Count = 0 OrElse dt.Columns.Count < 11 Then Return

        ' Tramo_Columna por (Piso, Label). Dos filas (Top/Bottom) por tramo.
        Dim porLabelPiso As New Dictionary(Of String, TramoDatos)(StringComparer.OrdinalIgnoreCase)

        For Each row As DataRow In dt.Rows
            Dim piso = TextoCelda(row, 0)
            Dim label = TextoCelda(row, 1)
            If String.IsNullOrWhiteSpace(label) OrElse String.IsNullOrWhiteSpace(piso) Then Continue For

            Dim seccion = TextoCelda(row, 3)
            Dim station = TextoCelda(row, 4)
            Dim asReq = NumCelda(row, 10)

            Dim clave = piso & "|" & label
            Dim t As TramoDatos = Nothing
            If Not porLabelPiso.TryGetValue(clave, t) Then
                t = New TramoDatos With {.Piso = piso, .Seccion = seccion}
                porLabelPiso(clave) = t
                If Not destino.ContainsKey(label) Then destino(label) = New List(Of TramoDatos)
                destino(label).Add(t)
            End If

            ' Station = "Bottom" → fila inferior; "Top" → superior. ETABS a veces
            ' pone números (distancia m); en ese caso el primero que vemos lo
            ' tratamos como Bottom.
            If station.Equals("Top", StringComparison.OrdinalIgnoreCase) Then
                t.As_Req_Top = asReq
            Else
                t.As_Req_Bottom = asReq
            End If
        Next
    End Sub

    ' ─────── Secciones Frame ─────────────────────────────────────────────
    ' Frame Sec Def - Conc Rect (E23): Name[0] Material[1] Depth[3] Width[4]
    ' Depth = H, Width = B. Material viene con el fc al inicio ("28" → fc=28).
    Private Shared Sub LeerSeccionesFrame(dt As DataTable, columnas As Dictionary(Of String, List(Of TramoDatos)))
        If dt Is Nothing OrElse dt.Rows.Count = 0 OrElse dt.Columns.Count < 5 Then Return

        Dim catalogo As New Dictionary(Of String, (b As Single, h As Single, fc As Single))(StringComparer.OrdinalIgnoreCase)

        For Each row As DataRow In dt.Rows
            Dim name = TextoCelda(row, 0)
            If String.IsNullOrWhiteSpace(name) Then Continue For
            Dim material = TextoCelda(row, 1)
            Dim depth = NumCelda(row, 3)
            Dim width = NumCelda(row, 4)
            Dim fc = ParsearFcDesdeMaterial(material)
            Dim bVal = Math.Min(width, depth)
            Dim hVal = Math.Max(width, depth)
            catalogo(name) = (bVal, hVal, fc)
        Next

        For Each kv In columnas
            For Each tramo In kv.Value
                Dim datos As (b As Single, h As Single, fc As Single) = Nothing
                If Not String.IsNullOrWhiteSpace(tramo.Seccion) AndAlso
                   catalogo.TryGetValue(tramo.Seccion, datos) Then
                    tramo.B_Modelo = datos.b
                    tramo.H_Modelo = datos.h
                    tramo.fc = datos.fc
                End If
            Next
        Next
    End Sub

    ' Mat label típico: "4000Psi", "28 MPa", "f'c=28". Agarra el primer bloque
    ' numérico y lo asume MPa. Si el nombre trae "Psi" convierte a MPa.
    Private Shared Function ParsearFcDesdeMaterial(material As String) As Single
        If String.IsNullOrWhiteSpace(material) Then Return 0
        Dim m = System.Text.RegularExpressions.Regex.Match(material, "\d+(\.\d+)?")
        If Not m.Success Then Return 0
        Dim val As Single
        If Not Single.TryParse(m.Value, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, val) Then Return 0
        If material.ToUpperInvariant().Contains("PSI") Then val = val / 145.0F
        Return val
    End Function

    ' ─────── Fuerzas Frame ───────────────────────────────────────────────
    ' Element Forces - Columns (E23): Story[0] Column[1] UniqueName[2] OutputCase[3]
    '   CaseType[4] StepType[5] StepNum[6] Station[7] P[8] V2[9] V3[10] T[11] M2[12] M3[13]
    Private Shared Sub LeerFuerzasFrame(dt As DataTable,
                                        columnas As Dictionary(Of String, List(Of TramoDatos)),
                                        combosLeidos As HashSet(Of String))
        If dt Is Nothing OrElse dt.Rows.Count = 0 OrElse dt.Columns.Count < 14 Then Return

        For Each row As DataRow In dt.Rows
            Dim piso = TextoCelda(row, 0)
            Dim label = TextoCelda(row, 1)
            If String.IsNullOrWhiteSpace(piso) OrElse String.IsNullOrWhiteSpace(label) Then Continue For

            Dim outputCase = TextoCelda(row, 3)
            Dim stepType = TextoCelda(row, 5)
            Dim combo = ColumnaService.ConstruirClaveCombo(outputCase, stepType)
            If String.IsNullOrWhiteSpace(combo) Then Continue For
            combosLeidos.Add(combo)

            Dim tramos As List(Of TramoDatos) = Nothing
            If Not columnas.TryGetValue(label, tramos) Then Continue For
            Dim tramo = tramos.FirstOrDefault(Function(t) t.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase))
            If tramo Is Nothing Then Continue For

            EnvolverEnTramo(tramo, combo,
                            NumCelda(row, 8), NumCelda(row, 9), NumCelda(row, 10),
                            NumCelda(row, 11), NumCelda(row, 12), NumCelda(row, 13))
        Next
    End Sub

    ' ─────── Diseño Pier ─────────────────────────────────────────────────
    ' Pier Dgn Sum (E23): Story[0] Pier[1] ... As_Req[9]. Dos filas por pier
    ' (Top/Bottom) alternadas; Salto=2.
    Private Shared Sub LeerDisenoPier(dt As DataTable, destino As Dictionary(Of String, List(Of TramoDatos)))
        If dt Is Nothing OrElse dt.Rows.Count = 0 OrElse dt.Columns.Count < 10 Then Return

        ' Detectar columna "Required"/As_Req dinámicamente — a veces está en 9, a veces en otra.
        Dim colAsReq As Integer = 9
        For ci = 0 To Math.Min(12, dt.Columns.Count - 1)
            Dim h = dt.Columns(ci).ColumnName
            If h IsNot Nothing AndAlso h.IndexOf("Required", StringComparison.OrdinalIgnoreCase) >= 0 Then
                colAsReq = ci
                Exit For
            End If
        Next

        Dim porLabelPiso As New Dictionary(Of String, TramoDatos)(StringComparer.OrdinalIgnoreCase)

        For i = 0 To dt.Rows.Count - 1 Step 2
            Dim rowBot = dt.Rows(i)
            Dim piso = TextoCelda(rowBot, 0)
            Dim label = TextoCelda(rowBot, 1)
            If String.IsNullOrWhiteSpace(label) OrElse String.IsNullOrWhiteSpace(piso) Then Continue For

            Dim clave = piso & "|" & label
            Dim t As TramoDatos = Nothing
            If Not porLabelPiso.TryGetValue(clave, t) Then
                t = New TramoDatos With {.Piso = piso, .Seccion = TextoCelda(rowBot, 1)}
                porLabelPiso(clave) = t
                If Not destino.ContainsKey(label) Then destino(label) = New List(Of TramoDatos)
                destino(label).Add(t)
            End If

            t.CuantiaBottomPct = NumCelda(rowBot, colAsReq)
            If i + 1 < dt.Rows.Count Then
                Dim rowTop = dt.Rows(i + 1)
                t.CuantiaTopPct = NumCelda(rowTop, colAsReq)
            End If
        Next
    End Sub

    ' ─────── Secciones Pier ──────────────────────────────────────────────
    ' Pier Section Properties: Section Name[1] (o 0), B[3], H[4], Material[2]
    Private Shared Sub LeerSeccionesPier(dt As DataTable, columnas As Dictionary(Of String, List(Of TramoDatos)))
        If dt Is Nothing OrElse dt.Rows.Count = 0 OrElse dt.Columns.Count < 5 Then Return

        Dim catalogo As New Dictionary(Of String, (b As Single, h As Single, fc As Single))(StringComparer.OrdinalIgnoreCase)
        For Each row As DataRow In dt.Rows
            Dim name = TextoCelda(row, 1)
            If String.IsNullOrWhiteSpace(name) Then name = TextoCelda(row, 0)
            If String.IsNullOrWhiteSpace(name) Then Continue For
            Dim material = TextoCelda(row, 2)
            Dim valB = NumCelda(row, 3)
            Dim valH = NumCelda(row, 4)
            catalogo(name) = (Math.Min(valB, valH), Math.Max(valB, valH), ParsearFcDesdeMaterial(material))
        Next

        For Each kv In columnas
            For Each tramo In kv.Value
                Dim datos As (b As Single, h As Single, fc As Single) = Nothing
                If Not String.IsNullOrWhiteSpace(tramo.Seccion) AndAlso
                   catalogo.TryGetValue(tramo.Seccion, datos) Then
                    tramo.B_Modelo = datos.b
                    tramo.H_Modelo = datos.h
                    tramo.fc = datos.fc
                End If
            Next
        Next
    End Sub

    ' ─────── Fuerzas Pier ────────────────────────────────────────────────
    ' Pier Forces (E23): Story[0] Pier[1] OutputCase[2] CaseType[3] StepType[4]
    '   StepNum[5] Location[6] P[7] V2[8] V3[9] T[10] M2[11] M3[12]
    Private Shared Sub LeerFuerzasPier(dt As DataTable,
                                       columnas As Dictionary(Of String, List(Of TramoDatos)),
                                       combosLeidos As HashSet(Of String))
        If dt Is Nothing OrElse dt.Rows.Count = 0 OrElse dt.Columns.Count < 13 Then Return

        For Each row As DataRow In dt.Rows
            Dim piso = TextoCelda(row, 0)
            Dim label = TextoCelda(row, 1)
            If String.IsNullOrWhiteSpace(piso) OrElse String.IsNullOrWhiteSpace(label) Then Continue For

            Dim combo = ColumnaService.ConstruirClaveCombo(TextoCelda(row, 2), TextoCelda(row, 4))
            If String.IsNullOrWhiteSpace(combo) Then Continue For
            combosLeidos.Add(combo)

            Dim tramos As List(Of TramoDatos) = Nothing
            If Not columnas.TryGetValue(label, tramos) Then Continue For
            Dim tramo = tramos.FirstOrDefault(Function(t) t.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase))
            If tramo Is Nothing Then Continue For

            EnvolverEnTramo(tramo, combo,
                            NumCelda(row, 7), NumCelda(row, 8), NumCelda(row, 9),
                            NumCelda(row, 10), NumCelda(row, 11), NumCelda(row, 12))
        Next
    End Sub

    ' Envolvente por valor absoluto componente a componente (misma regla que
    ' aplica Button2 al poblar Lista_Combinaciones).
    Private Shared Sub EnvolverEnTramo(tramo As TramoDatos, combo As String,
                                       p As Single, v2 As Single, v3 As Single,
                                       t As Single, m2 As Single, m3 As Single)
        Dim f As FuerzaCruda = Nothing
        If Not tramo.FuerzasEnvolventePorCombo.TryGetValue(combo, f) Then
            f = New FuerzaCruda With {.Combo = combo}
            tramo.FuerzasEnvolventePorCombo(combo) = f
        End If
        If Math.Abs(p) > Math.Abs(f.P) Then f.P = p
        If Math.Abs(v2) > Math.Abs(f.V2) Then f.V2 = v2
        If Math.Abs(v3) > Math.Abs(f.V3) Then f.V3 = v3
        If Math.Abs(t) > Math.Abs(f.T) Then f.T = t
        If Math.Abs(m2) > Math.Abs(f.M2) Then f.M2 = m2
        If Math.Abs(m3) > Math.Abs(f.M3) Then f.M3 = m3
    End Sub

    Private Shared Function TextoCelda(row As DataRow, idx As Integer) As String
        If idx < 0 OrElse idx >= row.Table.Columns.Count Then Return ""
        Dim v = row(idx)
        If v Is Nothing OrElse v Is DBNull.Value Then Return ""
        Return v.ToString().Trim()
    End Function

    Private Shared Function NumCelda(row As DataRow, idx As Integer) As Single
        If idx < 0 OrElse idx >= row.Table.Columns.Count Then Return 0
        Dim v = row(idx)
        If v Is Nothing OrElse v Is DBNull.Value Then Return 0
        Dim r As Single
        If Single.TryParse(v.ToString(), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, r) Then
            Return r
        End If
        Return 0
    End Function

    ' =====================================================================
    '  RECONCILIACIÓN — comparar Datos vs Proyecto
    ' =====================================================================

    Public Shared Function Reconciliar(datos As DatosExcelColumnas, proyecto As Proyecto) As ReporteReconciliacion

        Dim rep As New ReporteReconciliacion
        If datos Is Nothing OrElse proyecto Is Nothing Then Return rep

        Dim lista = proyecto.Elementos.Columnas.Lista_Columnas
        If lista Is Nothing Then lista = New List(Of Columna)

        ' Separar columnas del proyecto en Frame vs Pier por membresía en los
        ' diccionarios del Excel. La única forma heurística, en ausencia de un
        ' flag por columna, es ver en cuál lo encontramos; si no está en
        ' ninguno queda como "huérfano" de ambos tipos (solo reportamos en
        ' Frame por defecto — Pier se reporta aparte abajo).
        Dim labelsProyecto = New HashSet(Of String)(lista.Select(Function(c) c.Name_Elemento),
                                                    StringComparer.OrdinalIgnoreCase)

        ' Cambios de sección y recuento de fuerzas actualizadas
        Dim columnasAfectadas As Integer = 0
        For Each col In lista
            Dim excelTramos As List(Of TramoDatos) = Nothing
            Dim esPier As Boolean = False
            If datos.ColumnasFrame.TryGetValue(col.Name_Elemento, excelTramos) Then
                esPier = False
            ElseIf datos.ColumnasPier.TryGetValue(col.Name_Elemento, excelTramos) Then
                esPier = True
            Else
                Continue For
            End If

            Dim hayFuerzasNuevas As Boolean = False
            For Each tramoProy In col.Lista_Tramos_Columnas
                Dim tramoXls = excelTramos.FirstOrDefault(Function(t) t.Piso.Equals(tramoProy.Piso, StringComparison.OrdinalIgnoreCase))
                If tramoXls Is Nothing Then Continue For

                ' ¿Cambió la sección?
                Dim cambio = DetectarCambioSeccion(tramoProy, tramoXls)
                If cambio IsNot Nothing Then
                    cambio.Label = col.Name_Elemento
                    If esPier Then rep.CambiosSeccionPier.Add(cambio) Else rep.CambiosSeccionFrame.Add(cambio)
                End If

                If tramoXls.FuerzasEnvolventePorCombo.Count > 0 Then hayFuerzasNuevas = True
            Next
            If hayFuerzasNuevas Then columnasAfectadas += 1
        Next
        rep.ColumnasConFuerzasActualizadas = columnasAfectadas

        ' Labels nuevos (en Excel pero no en proyecto)
        For Each k In datos.ColumnasFrame.Keys
            If Not labelsProyecto.Contains(k) Then rep.LabelsNuevosFrame.Add(k)
        Next
        For Each k In datos.ColumnasPier.Keys
            If Not labelsProyecto.Contains(k) Then rep.LabelsNuevosPier.Add(k)
        Next

        ' Labels del proyecto que no están en el Excel (se dejan intactos)
        Dim keysExcel = New HashSet(Of String)(
            datos.ColumnasFrame.Keys.Concat(datos.ColumnasPier.Keys),
            StringComparer.OrdinalIgnoreCase)
        For Each col In lista
            If Not keysExcel.Contains(col.Name_Elemento) Then
                ' No podemos distinguir frame/pier si no está en ninguno; va a
                ' Frame por convención (es lo más común). El usuario lo ve y decide.
                rep.LabelsSinExcelFrame.Add(col.Name_Elemento)
            End If
        Next

        ' Combinaciones nuevas
        Dim combosExistentes As New HashSet(Of String)(
            proyecto.Elementos.Columnas.Lista_Combinaciones, StringComparer.OrdinalIgnoreCase)
        For Each c In datos.CombinacionesLeidas
            If Not combosExistentes.Contains(c) Then rep.CombinacionesNuevas.Add(c)
        Next

        rep.TieneCambios = rep.CambiosSeccionFrame.Count > 0 OrElse rep.CambiosSeccionPier.Count > 0 OrElse
                           rep.ColumnasConFuerzasActualizadas > 0 OrElse rep.LabelsNuevosFrame.Count > 0 OrElse
                           rep.LabelsNuevosPier.Count > 0 OrElse rep.CombinacionesNuevas.Count > 0

        Return rep

    End Function

    Private Shared Function DetectarCambioSeccion(proy As Tramo_Columna, xls As TramoDatos) As CambioSeccion
        Const TOL As Single = 0.005F ' 5 mm, 0.5 MPa
        Dim difB = Math.Abs(proy.B_Plano - xls.B_Modelo) > TOL
        Dim difH = Math.Abs(proy.H_Plano - xls.H_Modelo) > TOL
        Dim difFc = Math.Abs(proy.fc - xls.fc) > 0.5F
        If Not (difB OrElse difH OrElse difFc) Then Return Nothing

        Return New CambioSeccion With {
            .Piso = proy.Piso,
            .Antes = $"{proy.B_Plano:F2}×{proy.H_Plano:F2}  fc={proy.fc:F0}",
            .Despues = $"{xls.B_Modelo:F2}×{xls.H_Modelo:F2}  fc={xls.fc:F0}"
        }
    End Function

    ' =====================================================================
    '  APLICACIÓN — vuelca los datos del Excel al proyecto preservando refuerzo
    ' =====================================================================

    ''' <summary>
    ''' Aplica los cambios del Excel sobre el proyecto. Preserva Refuerzo_Col_*,
    ''' estribos, Distribucion_Personalizada y Lista_Barras_Seccion.
    ''' <paramref name="labelsNuevosAceptados"/> contiene los Name_Elemento que
    ''' el usuario autorizó agregar como columnas nuevas (de LabelsNuevosFrame
    ''' y LabelsNuevosPier). Cualquier otra columna del Excel sin match en el
    ''' proyecto se ignora.
    ''' </summary>
    Public Shared Sub Aplicar(datos As DatosExcelColumnas,
                              proyecto As Proyecto,
                              labelsNuevosAceptados As HashSet(Of String))
        If datos Is Nothing OrElse proyecto Is Nothing Then Return

        Dim col_s = proyecto.Elementos.Columnas
        If col_s.Lista_Columnas Is Nothing Then col_s.Lista_Columnas = New List(Of Columna)
        If labelsNuevosAceptados Is Nothing Then labelsNuevosAceptados = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        ' 1) Actualizar columnas existentes
        For Each col In col_s.Lista_Columnas
            Dim excelTramos As List(Of TramoDatos) = Nothing
            If Not datos.ColumnasFrame.TryGetValue(col.Name_Elemento, excelTramos) Then
                If Not datos.ColumnasPier.TryGetValue(col.Name_Elemento, excelTramos) Then Continue For
            End If
            AplicarAColumna(col, excelTramos)
        Next

        ' 2) Agregar columnas nuevas aceptadas
        For Each lbl In labelsNuevosAceptados
            Dim excelTramos As List(Of TramoDatos) = Nothing
            If Not datos.ColumnasFrame.TryGetValue(lbl, excelTramos) Then
                If Not datos.ColumnasPier.TryGetValue(lbl, excelTramos) Then Continue For
            End If
            Dim nueva As New Columna With {.Name_Elemento = lbl, .Name_Label = lbl}
            For Each td In excelTramos
                Dim tc As New Tramo_Columna With {.Name_Elemento = lbl, .Piso = td.Piso}
                nueva.Lista_Tramos_Columnas.Add(tc)
            Next
            AplicarAColumna(nueva, excelTramos)
            col_s.Lista_Columnas.Add(nueva)
        Next

        ' 3) Combinaciones nuevas → Proyecto.Elementos.Columnas.Lista_Combinaciones
        '    Se agregan sin pre-seleccionar en Design/Cortante/ALR; el llamador
        '    debe abrir Form_Opciones_Combinaciones si lo considera necesario.
        If col_s.Lista_Combinaciones Is Nothing Then col_s.Lista_Combinaciones = New List(Of String)
        Dim set_existentes As New HashSet(Of String)(col_s.Lista_Combinaciones, StringComparer.OrdinalIgnoreCase)
        For Each c In datos.CombinacionesLeidas
            If Not set_existentes.Contains(c) Then
                col_s.Lista_Combinaciones.Add(c)
                set_existentes.Add(c)
            End If
        Next

        ' 4) Re-extraer envolvente de cortante (tramo.V2/V3/Pu_V2/Pu_V3) usando
        '    las combinaciones de cortante ya seleccionadas en el proyecto.
        RepropagarEnvolventeCortante(col_s)
    End Sub

    Private Shared Sub AplicarAColumna(col As Columna, excelTramos As List(Of TramoDatos))
        For Each tramoProy In col.Lista_Tramos_Columnas
            Dim tramoXls = excelTramos.FirstOrDefault(Function(t) t.Piso.Equals(tramoProy.Piso, StringComparison.OrdinalIgnoreCase))
            If tramoXls Is Nothing Then Continue For

            ' Secciones y fc: solo se tocan si el Excel trajo valores (>0).
            If tramoXls.B_Modelo > 0 Then
                tramoProy.B_Modelo = tramoXls.B_Modelo
                tramoProy.B_Plano = tramoXls.B_Modelo
            End If
            If tramoXls.H_Modelo > 0 Then
                tramoProy.H_Modelo = tramoXls.H_Modelo
                tramoProy.H_Plano = tramoXls.H_Modelo
            End If
            If tramoXls.fc > 0 Then tramoProy.fc = tramoXls.fc

            ' As_Req ya viene en mm² (Frame) o como cuantía% (Pier, convertida en
            ' AplicarCuantiaPier). Para Pier se recalcula aquí a mm² con la sección.
            If tramoXls.As_Req_Top > 0 Then tramoProy.As_Req_Top = tramoXls.As_Req_Top
            If tramoXls.As_Req_Bottom > 0 Then tramoProy.As_Req_Bottom = tramoXls.As_Req_Bottom
            If tramoXls.CuantiaBottomPct > 0 Then
                tramoProy.Cuantia_Req_Bottom = tramoXls.CuantiaBottomPct
                tramoProy.As_Req_Bottom = tramoProy.B_Modelo * tramoProy.H_Modelo * tramoProy.Cuantia_Req_Bottom * 10000
            End If
            If tramoXls.CuantiaTopPct > 0 Then
                tramoProy.Cuantia_Req_Top = tramoXls.CuantiaTopPct
                tramoProy.As_Req_Top = tramoProy.B_Modelo * tramoProy.H_Modelo * tramoProy.Cuantia_Req_Top * 10000
            End If

            ' Fuerzas: si el Excel trajo algo, reemplazar Lista_Combinaciones de
            ' este tramo. Si no trajo nada, dejar las anteriores. Preserva
            ' Refuerzo_Col_* y demás (no se tocan).
            If tramoXls.FuerzasEnvolventePorCombo.Count > 0 Then
                tramoProy.Lista_Combinaciones = New List(Of Tramo_Columna.Fuerzas_Elementos)
                For Each f In tramoXls.FuerzasEnvolventePorCombo.Values
                    tramoProy.Lista_Combinaciones.Add(New Tramo_Columna.Fuerzas_Elementos With {
                        .Name = f.Combo, .P = f.P, .V2 = f.V2, .V3 = f.V3,
                        .T = f.T, .M2 = f.M2, .M3 = f.M3})
                Next
            End If
        Next
    End Sub

    ' Después de actualizar Lista_Combinaciones por tramo, los campos de
    ' envolvente de cortante quedan stale. Se re-extraen usando las
    ' combinaciones de cortante que ya tenía seleccionadas el proyecto;
    ' idéntica regla que Form_02_PagColumnas usa tras sincronizar combos.
    Private Shared Sub RepropagarEnvolventeCortante(col_s As cColumnas)
        Dim cortanteSel As New HashSet(Of String)(col_s.Lista_Combinaciones_Cortante, StringComparer.OrdinalIgnoreCase)
        If cortanteSel.Count = 0 Then Return

        For Each col In col_s.Lista_Columnas
            For Each tramo In col.Lista_Tramos_Columnas
                tramo.V2 = 0 : tramo.V3 = 0 : tramo.Pu_V2 = 0 : tramo.Pu_V3 = 0
                For Each f In tramo.Lista_Combinaciones
                    If Not cortanteSel.Contains(f.Name) Then Continue For
                    If Math.Abs(f.V2) > Math.Abs(tramo.V2) Then
                        tramo.V2 = f.V2
                        If f.P < 0 Then tramo.Pu_V2 = f.P
                    End If
                    If Math.Abs(f.V3) > Math.Abs(tramo.V3) Then
                        tramo.V3 = f.V3
                        If f.P < 0 Then tramo.Pu_V3 = f.P
                    End If
                Next
            Next
        Next
    End Sub

End Class
