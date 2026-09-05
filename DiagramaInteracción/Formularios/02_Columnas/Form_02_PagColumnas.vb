Imports System.Data.OleDb
Imports System.IO
Imports System.IO.Compression
Imports System.Threading.Tasks
Imports System.Xml
Imports ARCO.Funciones_02_Columnas

Public Class Form_02_PagColumnas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public Shared Columna As New Columna
    ' Secciones circulares detectadas en "Frame Sec Def - Conc Circle": nombre → (diámetro m, material)
    Private Shared _SeccionesCirculares As New Dictionary(Of String, Tuple(Of Single, String))(StringComparer.OrdinalIgnoreCase)
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Cursor = Cursors.WaitCursor
        Columna = New Columna()

        If Op_Flexo.Checked = True Then
            Proyecto.Elementos.Columnas.Verificacion_Flexo_Compresion = True
        End If
        If Op_Cortante.Checked = True Then
            Proyecto.Elementos.Columnas.Verificacion_Cortante = True
        End If
        If Op_Confinamiento.Checked = True Then
            Proyecto.Elementos.Columnas.Verificacion_Confinamiento = True
        End If
        If Op_ALR.Checked = True Then
            Proyecto.Elementos.Columnas.Verificacion_ALR = True
        End If

        If Proyecto.Elementos.Columnas.Elementos_Frame = True Then
            Dim Tabla As DataGridView

            Dim Col_Diseno = ColumnasDiseno("Frame")
            Dim Col_Fuerzas = ColumnasFuerzas("Frame")

            If Proyecto.Elementos.Columnas.Info_Diseño = True Then
                Tabla = Tabla_Diseño_Flexo

                If Tabla.Rows.Count <= 2 Then GoTo SkipFrameDiseno

                Dim Col_Piso As Integer = Col_Diseno(0)
                Dim Col_Label As Integer = Col_Diseno(1)
                Dim Col_Seccion As Integer = Col_Diseno(2)
                Dim Salto As Integer = Col_Diseno(3)
                Dim Col_As_Req As Integer = Col_Diseno(4)

                Dim Section As String = Tabla.Rows(2).Cells(1).Value
                Dim I0 As Integer = 2
                Dim Contar As Integer = 0

                For i = 2 To Tabla.Rows.Count() - 1
                    For j = 1 To 7
                        If I0 + j >= Tabla.Rows.Count Then Exit For
                        If Tabla.Rows(I0 + j).Cells(1).Value <> Section Then
                            Salto = j
                            Exit For
                        End If
                    Next

                    If Tabla.Rows(i).Cells(0).Value <> String.Empty And Tabla.Rows(i).Cells(4).Value = 0 Then
                        Dim Seccion As New Tramo_Columna
                        Seccion.Name_Elemento = Tabla.Rows(i).Cells(Col_Label).Value
                        Seccion.Piso = Tabla.Rows(i).Cells(Col_Piso).Value
                        Seccion.Seccion = Tabla.Rows(i).Cells(Col_Seccion).Value

                        ' E23: As viene en m², convertir a mm²
                        Seccion.As_Req_Bottom = Convert.ToSingle(Tabla.Rows(i).Cells(Col_As_Req).Value) * 1000000
                        Seccion.As_Req_Top = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_As_Req).Value) * 1000000

                        Columna.Lista_Tramos_Columnas.Add(Seccion)

                        Section = Seccion.Name_Elemento
                        I0 = i
                    End If
                Next

                For i = 0 To Columna.Lista_Tramos_Columnas.Count - 1
                    Dim Columna_ As New Columna
                    Columna_.Name_Elemento = Columna.Lista_Tramos_Columnas(i).Name_Elemento
                    Columna_.Name_Label = Columna.Lista_Tramos_Columnas(i).Name_Elemento
                    Columna_.Lista_Tramos_Columnas = Columna.Lista_Tramos_Columnas.FindAll(Function(p) p.Name_Elemento = Columna_.Name_Elemento)

                    If Proyecto.Elementos.Columnas.Lista_Columnas.Exists(Function(p) p.Name_Elemento = Columna_.Name_Elemento) Then
                    Else
                        Proyecto.Elementos.Columnas.Lista_Columnas.Add(Columna_)
                        Combo_Elementos.Items.Add(Columna_.Name_Elemento)
                    End If
                Next
SkipFrameDiseno:
            End If

            If Proyecto.Elementos.Columnas.Info_Secciones = True Then
                Tabla = Tabla_secciones

                Dim cols_S = IndicesColumnasSecciones(Tabla_secciones)
                Dim Col_Name     As Integer = cols_S("Name")
                Dim Col_Material As Integer = cols_S("Material")
                Dim Col_B        As Integer = cols_S("Depth")
                Dim Col_H        As Integer = cols_S("Width")

                For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1

                        For j = 2 To Tabla.Rows.Count - 1
                            Dim celdaNombre = Tabla.Rows(j).Cells(Col_Name).Value
                            If celdaNombre IsNot Nothing AndAlso celdaNombre IsNot DBNull.Value AndAlso
                               celdaNombre.ToString() <> String.Empty AndAlso
                               celdaNombre.ToString() = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Seccion Then

                                Dim rawB = Tabla.Rows(j).Cells(Col_B).Value
                                Dim rawH = Tabla.Rows(j).Cells(Col_H).Value
                                If rawB Is Nothing OrElse rawB Is DBNull.Value OrElse
                                   rawH Is Nothing OrElse rawH Is DBNull.Value Then Continue For

                                ' E23: dimensiones en m (Depth=H, Width=B)
                                Dim valB As Single = Convert.ToSingle(rawB)
                                Dim valH As Single = Convert.ToSingle(rawH)
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo = Math.Min(valB, valH)
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo = Math.Max(valB, valH)
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Plano = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Plano = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).fc = Convert.ToSingle(Mid(Tabla.Rows(j).Cells(Col_Material).Value.ToString(), 1, 2))
                            End If
                        Next

                        ' Fallback: sección circular (no encontrada en Conc Rect)
                        Dim tr = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np)
                        If tr.B_Modelo = 0 Then
                            Dim secNombre As String = tr.Seccion
                            If _SeccionesCirculares.ContainsKey(secNombre) Then
                                Dim info = _SeccionesCirculares(secNombre)
                                Dim D As Single = info.Item1
                                tr.EsCircular = True
                                tr.Diametro = D
                                tr.B_Modelo = D : tr.H_Modelo = D
                                tr.B_Plano = D : tr.H_Plano = D
                                tr.TipoTransversal = "Espiral"
                                Dim matStr As String = info.Item2
                                If matStr.Length >= 2 Then
                                    Dim fcStr As String = matStr.Trim().Substring(0, 2).Trim()
                                    Dim fcVal As Single
                                    If Single.TryParse(fcStr, fcVal) Then tr.fc = fcVal
                                End If
                            End If
                        End If
                    Next
                Next
            End If

            If Proyecto.Elementos.Columnas.Info_Fuerzas = True Then
                Tabla = Tabla_Fuerzas

                Dim cols_F = IndicesColumnasFuerzas(Tabla)
                Dim Col_Piso        As Integer = If(cols_F.ContainsKey("Story"),      cols_F("Story"),       0)
                Dim Col_Label       As Integer = If(cols_F.ContainsKey("Label"),      cols_F("Label"),       1)
                Dim Col_Combinacion As Integer = If(cols_F.ContainsKey("OutputCase"), cols_F("OutputCase"),  3)
                Dim Col_P           As Integer = If(cols_F.ContainsKey("P"),          cols_F("P"),           8)
                Dim Col_V2          As Integer = If(cols_F.ContainsKey("V2"),         cols_F("V2"),          9)
                Dim Col_V3          As Integer = If(cols_F.ContainsKey("V3"),         cols_F("V3"),         10)
                Dim Col_T           As Integer = If(cols_F.ContainsKey("T"),          cols_F("T"),          11)
                Dim Col_M2          As Integer = If(cols_F.ContainsKey("M2"),         cols_F("M2"),         12)
                Dim Col_M3          As Integer = If(cols_F.ContainsKey("M3"),         cols_F("M3"),         13)
                Dim Col_StepType    As Integer = cols_F("StepType")

                ' 1. Registrar combinaciones únicas (con clave compuesta si Step Type no está vacío)
                For j = 2 To Tabla.Rows.Count - 1
                    If Tabla.Rows(j).Cells(Col_Piso).Value IsNot Nothing AndAlso
                       Tabla.Rows(j).Cells(Col_Piso).Value.ToString <> String.Empty Then
                        Dim combo As String = If(Tabla.Rows(j).Cells(Col_Combinacion).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString(), "")
                        Dim stepVal As String = If(Col_StepType >= 0 AndAlso Tabla.Rows(j).Cells(Col_StepType).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_StepType).Value.ToString(), "")
                        Dim clave As String = Funciones_02_Columnas.ConstruirClaveCombo(combo, stepVal)
                        If clave <> "" AndAlso Not Proyecto.Elementos.Columnas.Lista_Combinaciones.Exists(Function(p) p = clave) Then
                            Proyecto.Elementos.Columnas.Lista_Combinaciones.Add(clave)
                        End If
                    End If
                Next

                ' 2. Asignar fuerzas con envolvente por estación (todas las estaciones por elemento-combo)
                For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                        Dim pisoTramo As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso

                        For j = 2 To Tabla.Rows.Count - 1
                            If Tabla.Rows(j).Cells(Col_Piso).Value Is Nothing OrElse
                               Tabla.Rows(j).Cells(Col_Piso).Value.ToString = String.Empty Then Continue For
                            If Tabla.Rows(j).Cells(Col_Piso).Value.ToString <> pisoTramo Then Continue For
                            If If(Tabla.Rows(j).Cells(Col_Label).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Label).Value.ToString(), "") <> Elemento Then Continue For

                            Dim combo As String = If(Tabla.Rows(j).Cells(Col_Combinacion).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString(), "")
                            Dim stepVal As String = If(Col_StepType >= 0 AndAlso Tabla.Rows(j).Cells(Col_StepType).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_StepType).Value.ToString(), "")
                            Dim clave As String = Funciones_02_Columnas.ConstruirClaveCombo(combo, stepVal)

                            Dim nuevoP As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                            Dim nuevoV2 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                            Dim nuevoV3 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                            Dim nuevoT As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                            Dim nuevoM2 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                            Dim nuevoM3 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)

                            Dim existente = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Find(Function(f) f.Name = clave)

                            If existente IsNot Nothing Then
                                ' Envolvente: máximo valor absoluto de cada componente
                                If Math.Abs(nuevoP) > Math.Abs(existente.P) Then existente.P = nuevoP
                                If Math.Abs(nuevoV2) > Math.Abs(existente.V2) Then existente.V2 = nuevoV2
                                If Math.Abs(nuevoV3) > Math.Abs(existente.V3) Then existente.V3 = nuevoV3
                                If Math.Abs(nuevoT) > Math.Abs(existente.T) Then existente.T = nuevoT
                                If Math.Abs(nuevoM2) > Math.Abs(existente.M2) Then existente.M2 = nuevoM2
                                If Math.Abs(nuevoM3) > Math.Abs(existente.M3) Then existente.M3 = nuevoM3
                            Else
                                Dim Fuerza As New Tramo_Columna.Fuerzas_Elementos
                                Fuerza.Name = clave
                                Fuerza.P = nuevoP
                                Fuerza.V2 = nuevoV2
                                Fuerza.V3 = nuevoV3
                                Fuerza.T = nuevoT
                                Fuerza.M2 = nuevoM2
                                Fuerza.M3 = nuevoM3
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(Fuerza)
                            End If
                        Next

                    Next
                Next
            End If
        End If

        If Proyecto.Elementos.Columnas.Elementos_Pier = True Then
            Dim Tabla As DataGridView

            Dim Col_Diseno = ColumnasDiseno("Pier")
            Dim Col_Secciones = ColumnasSecciones("Pier")
            Dim Col_Fuerzas = ColumnasFuerzas("Pier")

            If Proyecto.Elementos.Columnas.Info_Diseño = True Then
                Tabla = Tabla_Diseño_Pier

                If Tabla.Rows.Count <= 2 Then GoTo SkipPierDiseno

                Dim Col_Piso As Integer = Col_Diseno(0)
                Dim Col_Label As Integer = Col_Diseno(1)
                Dim Col_Seccion As Integer = Col_Diseno(2)
                Dim Salto As Integer = Col_Diseno(3)
                Dim Col_As_Req As Integer = Col_Diseno(4)

                For i = 0 To Math.Min(12, Tabla.Columns.Count - 1)
                    Dim C As String = If(Tabla.Rows(0).Cells(i).Value IsNot Nothing, Tabla.Rows(0).Cells(i).Value.ToString(), "")
                    If C.Contains("Required") Then
                        Col_As_Req = i
                    End If
                Next

                For i = 2 To Tabla.Rows.Count() - 1 Step Salto
                    If Tabla.Rows(i).Cells(0).Value <> String.Empty Then
                        Dim Seccion As New Tramo_Columna
                        Seccion.Name_Elemento = Tabla.Rows(i).Cells(Col_Label).Value
                        Seccion.Piso = Tabla.Rows(i).Cells(Col_Piso).Value
                        Seccion.Seccion = Tabla.Rows(i).Cells(Col_Seccion).Value

                        Seccion.Cuantia_Req_Bottom = Convert.ToSingle(Tabla.Rows(i).Cells(Col_As_Req).Value)
                        If i + Salto - 1 < Tabla.Rows.Count Then
                            Seccion.Cuantia_Req_Top = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_As_Req).Value)
                        End If

                        Columna.Lista_Tramos_Columnas.Add(Seccion)
                    End If
                Next

                For i = 0 To Columna.Lista_Tramos_Columnas.Count - 1
                    Dim Columna_ As New Columna
                    Columna_.Name_Elemento = Columna.Lista_Tramos_Columnas(i).Name_Elemento
                    Columna_.Name_Label = Columna.Lista_Tramos_Columnas(i).Name_Elemento
                    Columna_.Lista_Tramos_Columnas = Columna.Lista_Tramos_Columnas.FindAll(Function(p) p.Name_Elemento = Columna_.Name_Elemento)

                    If Proyecto.Elementos.Columnas.Lista_Columnas.Exists(Function(p) p.Name_Elemento = Columna_.Name_Elemento) Then
                    Else
                        Proyecto.Elementos.Columnas.Lista_Columnas.Add(Columna_)
                        Combo_Elementos.Items.Add(Columna_.Name_Elemento)
                    End If
                Next
SkipPierDiseno:
            End If

            If Proyecto.Elementos.Columnas.Info_Secciones = True Then
                Tabla = Tabla_Secciones_Pier

                Dim Col_Piso As Integer = Col_Secciones(0)
                Dim Col_Name As Integer = Col_Secciones(1)
                Dim Col_Material As Integer = Col_Secciones(2)
                Dim Col_B As Integer = Col_Secciones(3)
                Dim Col_H As Integer = Col_Secciones(4)

                For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                        For j = 2 To Tabla.Rows.Count - 1
                            If Tabla.Rows(j).Cells(Col_Name).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Name).Value = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Seccion And Tabla.Rows(j).Cells(Col_Piso).Value = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso Then
                                ' E23: dimensiones en m (Width=B, Thickness=H)
                                Dim valB As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value)
                                Dim valH As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value)
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo = Math.Min(valB, valH)
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo = Math.Max(valB, valH)
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Plano = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Plano = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).fc = Convert.ToSingle(Mid(Tabla.Rows(j).Cells(Col_Material).Value, 1, 2))
                                ' As_Req = B(m) × H(m) × Cuantia(%) × 10000 → mm²
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).As_Req_Bottom = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo * Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo * Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Cuantia_Req_Bottom * 10000
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).As_Req_Top = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo * Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo * Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Cuantia_Req_Top * 10000
                                Exit For
                            End If
                        Next
                    Next
                Next
            End If

            If Proyecto.Elementos.Columnas.Info_Fuerzas = True Then
                Tabla = Tabla_Fuerzas_Pier

                Dim cols_P = IndicesColumnasFuerzas(Tabla)
                Dim Col_Piso        As Integer = If(cols_P.ContainsKey("Story"),      cols_P("Story"),       0)
                Dim Col_Label       As Integer = If(cols_P.ContainsKey("Label"),      cols_P("Label"),       1)
                Dim Col_Combinacion As Integer = If(cols_P.ContainsKey("OutputCase"), cols_P("OutputCase"),  2)
                Dim Col_P           As Integer = If(cols_P.ContainsKey("P"),          cols_P("P"),           7)
                Dim Col_V2          As Integer = If(cols_P.ContainsKey("V2"),         cols_P("V2"),          8)
                Dim Col_V3          As Integer = If(cols_P.ContainsKey("V3"),         cols_P("V3"),          9)
                Dim Col_T           As Integer = If(cols_P.ContainsKey("T"),          cols_P("T"),          10)
                Dim Col_M2          As Integer = If(cols_P.ContainsKey("M2"),         cols_P("M2"),         11)
                Dim Col_M3          As Integer = If(cols_P.ContainsKey("M3"),         cols_P("M3"),         12)
                Dim Col_StepType    As Integer = cols_P("StepType")

                ' 1. Registrar combinaciones únicas con clave compuesta si Step Type no está vacío
                For j = 2 To Tabla.Rows.Count - 1
                    If Tabla.Rows(j).Cells(Col_Piso).Value IsNot Nothing AndAlso
                       Tabla.Rows(j).Cells(Col_Piso).Value.ToString <> String.Empty Then
                        Dim combo As String = If(Tabla.Rows(j).Cells(Col_Combinacion).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString(), "")
                        Dim stepVal As String = If(Col_StepType >= 0 AndAlso Tabla.Rows(j).Cells(Col_StepType).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_StepType).Value.ToString(), "")
                        Dim clave As String = Funciones_02_Columnas.ConstruirClaveCombo(combo, stepVal)
                        If clave <> "" AndAlso Not Proyecto.Elementos.Columnas.Lista_Combinaciones.Exists(Function(p) p = clave) Then
                            Proyecto.Elementos.Columnas.Lista_Combinaciones.Add(clave)
                        End If
                    End If
                Next

                ' 2. Asignar fuerzas con envolvente Top/Bottom por elemento-piso-combo
                For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                        Dim pisoTramo As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso

                        For j = 2 To Tabla.Rows.Count - 1
                            If Tabla.Rows(j).Cells(Col_Piso).Value Is Nothing OrElse
                               Tabla.Rows(j).Cells(Col_Piso).Value.ToString = String.Empty Then Continue For
                            If Tabla.Rows(j).Cells(Col_Piso).Value.ToString <> pisoTramo Then Continue For
                            If If(Tabla.Rows(j).Cells(Col_Label).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Label).Value.ToString(), "") <> Elemento Then Continue For

                            Dim combo As String = If(Tabla.Rows(j).Cells(Col_Combinacion).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString(), "")
                            Dim stepVal As String = If(Col_StepType >= 0 AndAlso Tabla.Rows(j).Cells(Col_StepType).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_StepType).Value.ToString(), "")
                            Dim clave As String = Funciones_02_Columnas.ConstruirClaveCombo(combo, stepVal)

                            Dim nuevoP As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                            Dim nuevoV2 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                            Dim nuevoV3 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                            Dim nuevoT As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                            Dim nuevoM2 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                            Dim nuevoM3 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)

                            Dim existente = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Find(Function(f) f.Name = clave)

                            If existente IsNot Nothing Then
                                If Math.Abs(nuevoP) > Math.Abs(existente.P) Then existente.P = nuevoP
                                If Math.Abs(nuevoV2) > Math.Abs(existente.V2) Then existente.V2 = nuevoV2
                                If Math.Abs(nuevoV3) > Math.Abs(existente.V3) Then existente.V3 = nuevoV3
                                If Math.Abs(nuevoT) > Math.Abs(existente.T) Then existente.T = nuevoT
                                If Math.Abs(nuevoM2) > Math.Abs(existente.M2) Then existente.M2 = nuevoM2
                                If Math.Abs(nuevoM3) > Math.Abs(existente.M3) Then existente.M3 = nuevoM3
                            Else
                                Dim Fuerza As New Tramo_Columna.Fuerzas_Elementos
                                Fuerza.Name = clave
                                Fuerza.P = nuevoP
                                Fuerza.V2 = nuevoV2
                                Fuerza.V3 = nuevoV3
                                Fuerza.T = nuevoT
                                Fuerza.M2 = nuevoM2
                                Fuerza.M3 = nuevoM3
                                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(Fuerza)
                            End If
                        Next

                    Next
                Next
            End If
        End If

        'If Proyecto.Elementos.Columnas.Verificacion_ALR = True Then
        '    For NC = 0 To Proyecto.Elementos.Columnas.Lista_Combinaciones.Count - 1
        '        formop
        '        Form_Combinaciones.Combo_Combinaciones.Items.Add(Proyecto.Elementos.Columnas.Lista_Combinaciones(NC).ToString)
        '    Next
        '    Form_Combinaciones.Combo_Combinaciones.Text = Proyecto.Elementos.Columnas.Lista_Combinaciones(0).ToString
        '    If Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR.Count > 0 Then
        '        For i = 0 To Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR.Count - 1
        '            Form_Combinaciones.Tabla_combinaciones.Rows.Add(Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR(i))
        '        Next
        '    End If
        '    Form_Combinaciones.Show()
        'End If

        If Proyecto.Elementos.Columnas.Info_Diseño = True AndAlso
           Proyecto.Elementos.Columnas.Lista_Columnas.Count > 0 Then
            Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(0).Name_Elemento
        End If

        Cursor = Cursors.Arrow

        ' Selección secuencial de combinaciones después de importar
        If Proyecto.Elementos.Columnas.Lista_Combinaciones.Count > 0 Then
            ' 1 — Combinaciones de Diseño
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
            Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()
            For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(combo)
            Next
            For Each combo As String In Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
                If Not Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Contains(combo) Then
                    Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(combo)
                    Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Remove(combo)
                End If
            Next
            Form_Opciones_Combinaciones.OpcionLlamado = "ColumnasDiseño"
            Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones de Diseño"
            Form_Opciones_Combinaciones.ShowDialog()

            ' 2 — Combinaciones de Cortante
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
            Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()
            For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(combo)
            Next
            For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones_Cortante
                If Not Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Contains(combo) Then
                    Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(combo)
                    Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Remove(combo)
                End If
            Next
            Form_Opciones_Combinaciones.OpcionLlamado = "ColumnasCortante"
            Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones Cortante"
            Form_Opciones_Combinaciones.ShowDialog()

            ' 3 — Combinaciones de ALR
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
            Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()
            For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(combo)
            Next
            For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR
                If Not Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Contains(combo) Then
                    Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(combo)
                    Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Remove(combo)
                End If
            Next
            Form_Opciones_Combinaciones.OpcionLlamado = "ColumnasALR"
            Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones ALR"
            Form_Opciones_Combinaciones.ShowDialog()
        End If

        ' Validación automática: muestra reporte en ventana no-modal para no bloquear el flujo
        Dim reporte As String = ValidarImportacionColumnas()
        Dim frm As New Form With {
            .Text = "Validación — Datos de Columnas",
            .Size = New Size(560, 460),
            .StartPosition = FormStartPosition.CenterParent,
            .MinimizeBox = False,
            .MaximizeBox = False
        }
        Dim txt As New RichTextBox With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(30, 30, 30),
            .ForeColor = Color.FromArgb(220, 220, 220),
            .Font = New Font("Consolas", 9.5),
            .Text = reporte,
            .BorderStyle = BorderStyle.None
        }
        Dim btnCerrar As New Button With {
            .Text = "Cerrar",
            .Dock = DockStyle.Bottom,
            .Height = 32
        }
        AddHandler btnCerrar.Click, Sub(s, ev) frm.Close()
        frm.Controls.Add(txt)
        frm.Controls.Add(btnCerrar)
        frm.Show(Me)

    End Sub

    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Try
            Tabla_Resumen.Rows.Clear()
            Tabla_Resumen.Columns.Clear()
            If Proyecto.Elementos.Columnas.Info_Diseño = True Then
                Tabla_Resumen.Columns.Add("Column1", "Elemento")
                Tabla_Resumen.Columns.Add("Column2", "Piso")
                Tabla_Resumen.Columns.Add("Column3", "Sección")
                Tabla_Resumen.Columns.Add("Column4", "Estación")
                Tabla_Resumen.Columns.Add("Column5", "As Requerido (mm2)")
            End If
            Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Name_Elemento
            Dim Seccion = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Lista_Tramos_Columnas
            For i = 0 To (Seccion.Count - 1) * 2
                Tabla_Resumen.Rows.Add()
            Next


            If Proyecto.Elementos.Columnas.Info_Fuerzas = True Then
                Tabla_Resumen.Columns.Add("Column6", "V2 (kN)")
                Tabla_Resumen.Columns.Add("Column7", "V3 (kN)")
            End If
            If Proyecto.Elementos.Columnas.Info_Secciones = True Then
                Tabla_Resumen.Columns.Add("Column8", "Base (m)")
                Tabla_Resumen.Columns.Add("Column9", "Alto (m)")
                Tabla_Resumen.Columns.Add("Column10", "f'c (MPa)")
            End If

            If Proyecto.Elementos.Columnas.Info_Diseño = True Then
                Tabla_Resumen.Rows(0).Cells(0).Value = Elemento
                For i = 0 To (Seccion.Count - 1) * 2 Step 2
                    Tabla_Resumen.Rows(i).Cells(1).Value = Seccion(i / 2).Piso
                    Tabla_Resumen.Rows(i).Cells(2).Value = Seccion(i / 2).Seccion
                    Tabla_Resumen.Rows(i + 1).Cells(2).Value = Seccion(i / 2).Seccion
                    Tabla_Resumen.Rows(i).Cells(3).Value = "Top"
                    Tabla_Resumen.Rows(i + 1).Cells(3).Value = "Bottom"
                    Tabla_Resumen.Rows(i).Cells(4).Value = Seccion(i / 2).As_Req_Top
                    Tabla_Resumen.Rows(i + 1).Cells(4).Value = Seccion(i / 2).As_Req_Bottom

                    If Proyecto.Elementos.Columnas.Info_Fuerzas = True Then
                        Tabla_Resumen.Rows(i).Cells(5).Value = Math.Round(Seccion(i / 2).V2, 2)
                        Tabla_Resumen.Rows(i).Cells(6).Value = Math.Round(Seccion(i / 2).V3, 2)
                    End If
                    If Proyecto.Elementos.Columnas.Info_Secciones = True Then
                        Tabla_Resumen.Rows(i).Cells(7).Value = Seccion(i / 2).B_Modelo
                        Tabla_Resumen.Rows(i).Cells(8).Value = Seccion(i / 2).H_Modelo
                        Tabla_Resumen.Rows(i).Cells(9).Value = Seccion(i / 2).fc
                    End If

                Next
            End If

        Catch ex As Exception
            Logger.Error(ex, "Form_02_PagColumnas.Button2_Click",
                         "Error durante el cálculo principal de columnas (Frame/Pier). " &
                         "Algunos elementos pueden no haberse procesado.")
        End Try
    End Sub
    Private Sub DataGridView1_CellPainting(sender As System.Object, e As System.Windows.Forms.DataGridViewCellPaintingEventArgs) Handles Tabla_Resumen.CellPainting
        Try
            If Tabla_Resumen.Rows.Count > 1 Then
                If e.RowIndex >= 0 Then
                    If Tabla_Resumen.Rows(e.RowIndex).Cells(e.ColumnIndex).Value <> Nothing Then
                        If e.ColumnIndex <= 1 Or e.ColumnIndex >= 5 Then
                            e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None
                        End If
                    End If
                    If e.ColumnIndex = 0 And e.RowIndex < Tabla_Resumen.Rows.Count - 1 Then
                        If Tabla_Resumen.Rows(e.RowIndex).Cells(0).Value = "" Then
                            e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            Logger.Warning("Form_02_PagColumnas.DataGridView1_CellPainting",
                           "Error al pintar celda de la tabla resumen (no afecta el cálculo): " & ex.Message)
        End Try

    End Sub

    Private Sub SeccionesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Insertar_Secciones_Col.Click
        Form_02_00_PagInfoColumnas.RefrescarCombo()
        Form_02_00_PagInfoColumnas.Show()
    End Sub

    '-------------------- Importar Tablas desde Excel -----------------------
    Private Sub DiseñoAFlexoCompresiónToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles Diseno_Col_Frame.Click
        Proyecto.Elementos.Columnas.Elementos_Frame = True
        Proyecto.Elementos.Columnas.Info_Diseño = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Importar_Datos_de_Excel(.FileName, Tabla_Diseño_Flexo, "Diseño", "Frame")
            End If
        End With
    End Sub

    Private Sub SeccionesToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles Secciones_Col_Frame.Click
        Proyecto.Elementos.Columnas.Elementos_Frame = True
        Proyecto.Elementos.Columnas.Info_Secciones = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Importar_Datos_de_Excel(.FileName, Tabla_secciones, "Secciones", "Frame")
            End If
        End With
    End Sub
    Private Sub FuerzasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Fuerzas_Col_Frame.Click
        Proyecto.Elementos.Columnas.Elementos_Frame = True
        Proyecto.Elementos.Columnas.Info_Fuerzas = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Importar_Datos_de_Excel(.FileName, Tabla_Fuerzas, "Fuerzas", "Frame")
            End If
        End With
    End Sub

    Private Sub DiseñoAFlexoCompresiónToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles Diseno_Col_Pier.Click
        Proyecto.Elementos.Columnas.Elementos_Pier = True
        Proyecto.Elementos.Columnas.Info_Diseño = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Importar_Datos_de_Excel(.FileName, Tabla_Diseño_Pier, "Diseño", "Pier")
            End If
        End With
    End Sub

    Private Sub SeccionesToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles Secciones_Col_Pier.Click
        Proyecto.Elementos.Columnas.Elementos_Pier = True
        Proyecto.Elementos.Columnas.Info_Secciones = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Importar_Datos_de_Excel(.FileName, Tabla_Secciones_Pier, "Secciones", "Pier")
            End If
        End With
    End Sub

    Private Sub FuerzasToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles Fuerzas_Col_Pier.Click
        Proyecto.Elementos.Columnas.Elementos_Pier = True
        Proyecto.Elementos.Columnas.Info_Fuerzas = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Importar_Datos_de_Excel(.FileName, Tabla_Fuerzas_Pier, "Fuerzas", "Pier")
            End If
        End With
    End Sub

    Private Sub ImportarTodoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Importar_Todo_Col.Click
        ' -- 1. Selector de archivo
        Dim openFD As New OpenFileDialog() With {
            .Title = "Seleccionar archivo ETABS (Frame + Pier)",
            .Filter = "Archivos Excel(*.xlsx)|*.xlsx|Todos los archivos(*.*)|*.*",
            .Multiselect = False
        }
        If openFD.ShowDialog() <> Windows.Forms.DialogResult.OK Then Return
        Dim ruta As String = openFD.FileName

        ' -- 2. Fase 1: leer hojas de diseno + geometria en una sola pasada (sincrono)
        Dim dtFrameD As DataTable = Nothing
        Dim dtPierD  As DataTable = Nothing
        Dim dtJoints As DataTable = Nothing
        Dim dtObjFrm As DataTable = Nothing
        Dim dtFrameS_p1 As DataTable = Nothing
        Dim dtPierS_p1  As DataTable = Nothing
        Me.Cursor = Cursors.WaitCursor
        Try
            Using za As New ZipArchive(File.OpenRead(ruta), ZipArchiveMode.Read)
                Dim wbMap = ZipLeerWorkbookMap(za)
                Dim sst   = ZipLeerSharedStrings(za)
                dtFrameD    = ZipLeerHoja(za, wbMap, sst, Nothing, 1, "Conc Col Sum", "Concrete Column Summary")
                dtPierD     = ZipLeerHoja(za, wbMap, sst, Nothing, 1, "Pier Dgn Sum", "Shear Wall Pier Summary")
                dtJoints    = ZipLeerHoja(za, wbMap, sst, Nothing, 0, "Objects and Elements - Joints", "Joint Coordinates")
                dtObjFrm    = ZipLeerHoja(za, wbMap, sst, Nothing, 0, "Objects and Elements - Frames", "Connectivity - Frame")
                dtFrameS_p1 = ZipLeerHoja(za, wbMap, sst, Nothing, 1, "Frame Sec Def - Conc Rect", "Frame Sections")
                dtPierS_p1  = ZipLeerHoja(za, wbMap, sst, Nothing, 1, "Pier Section Properties")
            End Using
        Catch ex As Exception
            Me.Cursor = Cursors.Arrow
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error al leer archivo")
            Return
        End Try
        Me.Cursor = Cursors.Arrow

        ' -- 3. Detectar automaticamente que tipos de elemento existen
        Dim etiqFrame As New List(Of String)()
        Dim etiqPier  As New List(Of String)()
        If dtFrameD IsNot Nothing Then
            For r = 2 To dtFrameD.Rows.Count - 1
                Dim lbl As String = If(dtFrameD.Rows(r)(1) IsNot DBNull.Value, dtFrameD.Rows(r)(1).ToString().Trim(), "")
                If lbl <> "" AndAlso Not etiqFrame.Contains(lbl) Then etiqFrame.Add(lbl)
            Next
        End If
        If dtPierD IsNot Nothing Then
            For r = 2 To dtPierD.Rows.Count - 1
                Dim lbl As String = If(dtPierD.Rows(r)(1) IsNot DBNull.Value, dtPierD.Rows(r)(1).ToString().Trim(), "")
                If lbl <> "" AndAlso Not etiqPier.Contains(lbl) Then etiqPier.Add(lbl)
            Next
        End If
        If etiqFrame.Count = 0 AndAlso etiqPier.Count = 0 Then
            MsgBox("No se encontraron elementos en las hojas de diseno del archivo.", MsgBoxStyle.Exclamation, "Sin elementos")
            Return
        End If

        ' -- 4. Construir candidatos con coordenadas + backdrop
        Dim backdrop As GeometriaEstructural = Nothing
        Dim candidatos = ConstruirCandidatosColumnas(
            etiqFrame, dtFrameD, dtFrameS_p1, dtJoints, dtObjFrm,
            etiqPier,  dtPierD,  dtPierS_p1,
            backdrop)

        ' -- 5. Formulario de seleccion con vista en planta
        Dim selFrame As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim selPier  As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Using frmSel As New Form_02_SeleccionColumnas(
            candidatos,
            If(backdrop IsNot Nothing, backdrop.JointsXY, Nothing),
            If(backdrop IsNot Nothing, backdrop.FramesXY, Nothing))
            If frmSel.ShowDialog(Me) <> DialogResult.OK Then Return
            selFrame = frmSel.SelFrame
            selPier  = frmSel.SelPier
        End Using
        If selFrame.Count = 0 AndAlso selPier.Count = 0 Then Return

        ' ── 6. Fase 2: cargar secciones + fuerzas FILTRADAS ─────────────────────────────
        Dim dtFrameD2 As DataTable = Nothing   ' diseño Frame filtrado por seleccion
        Dim dtFrameS  As DataTable = Nothing
        Dim dtFrameF  As DataTable = Nothing
        Dim dtPierD2  As DataTable = Nothing   ' diseño Pier filtrado por seleccion
        Dim dtPierS   As DataTable = Nothing
        Dim dtPierF   As DataTable = Nothing
        Dim dtCircle  As DataTable = Nothing
        Me.Cursor = Cursors.WaitCursor
        Try
            Using za As New ZipArchive(File.OpenRead(ruta), ZipArchiveMode.Read)
                Dim wbMap = ZipLeerWorkbookMap(za)
                Dim sst   = ZipLeerSharedStrings(za)
                ' Diseño filtrado: solo los elementos seleccionados llegan a Calcular
                If selFrame.Count > 0 Then
                    dtFrameD2 = ZipLeerHoja(za, wbMap, sst, selFrame, 1, "Conc Col Sum", "Concrete Column Summary")
                    dtFrameS  = ZipLeerHoja(za, wbMap, sst, Nothing,  1, "Frame Sec Def - Conc Rect", "Frame Sections")
                    dtFrameF  = ZipLeerHoja(za, wbMap, sst, selFrame, 1, "Element Forces - Columns",  "Column Forces")
                    dtCircle  = ZipLeerHoja(za, wbMap, sst, Nothing,  1, "Frame Sec Def - Conc Circle", "Conc Circle")
                End If
                If selPier.Count > 0 Then
                    dtPierD2 = ZipLeerHoja(za, wbMap, sst, selPier, 1, "Pier Dgn Sum", "Shear Wall Pier Summary")
                    dtPierS  = ZipLeerHoja(za, wbMap, sst, Nothing, 1, "Pier Section Properties")
                    dtPierF  = ZipLeerHoja(za, wbMap, sst, selPier, 1, "Pier Forces")
                End If
            End Using
        Catch ex As Exception
            Me.Cursor = Cursors.Arrow
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error al importar")
            Return
        End Try
        Me.Cursor = Cursors.Arrow

        ' ── 7. Vincular DataTables a DataGridViews ───────────────────────────────────────
        ' Para diseño se usa la versión filtrada de Fase 2; si falla el filtro, cae al resultado de Fase 1
        Proyecto.Elementos.Columnas.Info_Diseño   = True
        Proyecto.Elementos.Columnas.Info_Secciones = True
        Proyecto.Elementos.Columnas.Info_Fuerzas  = True
        Dim okFrameDiseno    As Boolean = VincularDGV(If(dtFrameD2 IsNot Nothing, dtFrameD2, dtFrameD), Tabla_Diseño_Flexo)
        Dim okFrameSecciones As Boolean = VincularDGV(dtFrameS,  Tabla_secciones)
        Dim okFrameFuerzas   As Boolean = VincularDGV(dtFrameF,  Tabla_Fuerzas)
        Dim okPierDiseno     As Boolean = VincularDGV(If(dtPierD2 IsNot Nothing, dtPierD2, dtPierD),   Tabla_Diseño_Pier)
        Dim okPierSecciones  As Boolean = VincularDGV(dtPierS,   Tabla_Secciones_Pier)
        Dim okPierFuerzas    As Boolean = VincularDGV(dtPierF,   Tabla_Fuerzas_Pier)

        ' ── Secciones circulares ──────────────────────────────────────────────────────
        If dtCircle IsNot Nothing AndAlso dtCircle.Rows.Count >= 3 Then
            _SeccionesCirculares.Clear()
            Dim colName As Integer = -1, colDiam As Integer = -1, colMat As Integer = -1
            For ci = 0 To dtCircle.Columns.Count - 1
                Dim hdr As String = If(dtCircle.Rows(0)(ci) IsNot DBNull.Value, dtCircle.Rows(0)(ci).ToString().Trim().ToUpperInvariant(), "")
                If hdr = "NAME" Then colName = ci
                If hdr = "DIAMETER" Then colDiam = ci
                If hdr = "MATERIAL" Then colMat = ci
            Next
            If colName >= 0 AndAlso colDiam >= 0 Then
                For r = 2 To dtCircle.Rows.Count - 1
                    Dim nm   As String = If(dtCircle.Rows(r)(colName) IsNot DBNull.Value, dtCircle.Rows(r)(colName).ToString().Trim(), "")
                    Dim matS As String = If(colMat >= 0 AndAlso dtCircle.Rows(r)(colMat) IsNot DBNull.Value, dtCircle.Rows(r)(colMat).ToString().Trim(), "")
                    Dim diamObj As Object = If(dtCircle.Rows(r)(colDiam) IsNot DBNull.Value, dtCircle.Rows(r)(colDiam), Nothing)
                    Dim diam As Single = 0
                    If diamObj IsNot Nothing Then
                        If TypeOf diamObj Is Double Then
                            diam = CSng(CDbl(diamObj))
                        Else
                            Single.TryParse(diamObj.ToString(), Globalization.NumberStyles.Float,
                                            Globalization.CultureInfo.InvariantCulture, diam)
                        End If
                    End If
                    If nm <> "" AndAlso diam > 0 Then
                        _SeccionesCirculares(nm) = Tuple.Create(diam, matS)
                    End If
                Next
            End If
        End If

        ' ── 9. Flags y resumen ────────────────────────────────────────────────────────────
        _hayCambiosColumnas = True
        Proyecto.Elementos.Columnas.Elementos_Frame = selFrame.Count > 0
        Proyecto.Elementos.Columnas.Elementos_Pier  = selPier.Count > 0

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine($"Archivo: {Path.GetFileName(ruta)}")
        sb.AppendLine(New String("─"c, 50))
        If selFrame.Count > 0 Then
            sb.AppendLine($"  FRAME: {selFrame.Count} elementos importados")
            sb.AppendLine(If(okFrameDiseno,    "    ✔  Diseño (Conc Col Sum)",          "    —  Diseño — no encontrado"))
            sb.AppendLine(If(okFrameSecciones, "    ✔  Secciones (Conc Rect)",          "    —  Secciones — no encontrado"))
            sb.AppendLine(If(okFrameFuerzas,   "    ✔  Fuerzas (Element Forces - Col)", "    —  Fuerzas — no encontrado"))
        End If
        If selPier.Count > 0 Then
            sb.AppendLine($"  PIER: {selPier.Count} elementos importados")
            sb.AppendLine(If(okPierDiseno,    "    ✔  Diseño (Pier Dgn Sum)",     "    —  Diseño — no encontrado"))
            sb.AppendLine(If(okPierSecciones, "    ✔  Secciones (Pier Sec Prop)", "    —  Secciones — no encontrado"))
            sb.AppendLine(If(okPierFuerzas,   "    ✔  Fuerzas (Pier Forces)",     "    —  Fuerzas — no encontrado"))
        End If
        Dim nCirc As Integer = _SeccionesCirculares.Count
        If nCirc > 0 Then sb.AppendLine($"  ✔  Secciones circulares: {nCirc} tipos")
        sb.AppendLine(New String("─"c, 50))
        sb.AppendLine("Ejecute 'Calcular' para procesar los elementos.")
        MsgBox(sb.ToString(), MsgBoxStyle.Information, "Importación exitosa")
    End Sub

    Public Function Importar_Datos_de_Excel(ByRef path As String,
                                            ByVal Datagrid As DataGridView,
                                            ByVal Op As String,
                                            ByVal Elemento As String,
                                            Optional suppressMsg As Boolean = False) As Boolean
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"
            Dim cnConex As New OleDbConnection(stConexion)
            cnConex.Open()

            ' Obtener todas las hojas para auto-detectar versión ETABS
            Dim schemaTables = cnConex.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Tables, Nothing)
            Dim sheetNames = schemaTables.Rows.Cast(Of DataRow)().Select(Function(r) r("TABLE_NAME").ToString()).ToList()

            Dim nombreHoja As String = ""

            If Op = "Diseño" Then
                If Elemento = "Frame" Then
                    ' E23: "Conc Col Sum - ACI 318-14"  |  E17: "Concrete Column Summary - ACI 3"
                    If sheetNames.Any(Function(s) s.Contains("Conc Col Sum")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Conc Col Sum"))
                    ElseIf sheetNames.Any(Function(s) s.Contains("Concrete Column Summary")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Concrete Column Summary"))
                    End If
                Else
                    ' E23: "Pier Dgn Sum"  |  E17: "Shear Wall Pier Summary"
                    If sheetNames.Any(Function(s) s.Contains("Pier Dgn Sum")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Pier Dgn Sum"))
                    ElseIf sheetNames.Any(Function(s) s.Contains("Shear Wall Pier Summary")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Shear Wall Pier Summary"))
                    End If
                End If

            ElseIf Op = "Secciones" Then
                If Elemento = "Frame" Then
                    ' E23: "Frame Sec Def - Conc Rect"  |  E17: "Frame Sections"
                    If sheetNames.Any(Function(s) s.Contains("Frame Sec Def - Conc Rect")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Frame Sec Def - Conc Rect"))
                    ElseIf sheetNames.Any(Function(s) s.Contains("Frame Sections")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Frame Sections"))
                    End If
                Else
                    If sheetNames.Any(Function(s) s.Contains("Pier Section Properties")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Pier Section Properties"))
                    End If
                End If

            ElseIf Op = "Fuerzas" Then
                If Elemento = "Frame" Then
                    ' E23: "Element Forces - Columns"  |  E17: "Column Forces"
                    If sheetNames.Any(Function(s) s.Contains("Element Forces - Columns")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Element Forces - Columns"))
                    ElseIf sheetNames.Any(Function(s) s.Contains("Column Forces")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Column Forces"))
                    End If
                Else
                    If sheetNames.Any(Function(s) s.Contains("Pier Forces")) Then
                        nombreHoja = sheetNames.First(Function(s) s.Contains("Pier Forces"))
                    End If
                End If
            End If

            If String.IsNullOrEmpty(nombreHoja) Then
                cnConex.Close()
                If Not suppressMsg Then
                    MsgBox($"No se encontró la hoja de {Op} ({Elemento}) en el archivo.{vbCrLf}Verifique que el archivo corresponde al tipo correcto.", MsgBoxStyle.Exclamation, "Hoja no encontrada")
                End If
                Return False
            End If

            Dim Cmd As New OleDbCommand($"Select * From [{nombreHoja}]")
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)

            ' Si cargamos secciones de Frame, también buscar Conc Circle para columnas circulares
            If Op = "Secciones" AndAlso Elemento = "Frame" Then
                Dim hCirc As String = sheetNames.FirstOrDefault(Function(s) s.ToUpperInvariant().Contains("CONC CIRCLE"))
                If hCirc IsNot Nothing Then
                    Try
                        Dim DsCirc As New DataSet
                        Dim DaCirc As New OleDbDataAdapter(New OleDbCommand($"Select * From [{hCirc}]", cnConex))
                        DaCirc.Fill(DsCirc)
                        Dim DtCirc As DataTable = DsCirc.Tables(0)
                        _SeccionesCirculares.Clear()
                        If DtCirc.Rows.Count >= 3 Then
                            Dim colName As Integer = -1, colDiam As Integer = -1, colMat As Integer = -1
                            For ci = 0 To DtCirc.Columns.Count - 1
                                Dim h As String = If(DtCirc.Rows(0)(ci) IsNot DBNull.Value, DtCirc.Rows(0)(ci).ToString().Trim().ToUpperInvariant(), "")
                                If h = "NAME" Then colName = ci
                                If h = "DIAMETER" Then colDiam = ci
                                If h = "MATERIAL" Then colMat = ci
                            Next
                            If colName >= 0 AndAlso colDiam >= 0 Then
                                For r = 2 To DtCirc.Rows.Count - 1
                                    Dim nm As String = If(DtCirc.Rows(r)(colName) IsNot DBNull.Value, DtCirc.Rows(r)(colName).ToString().Trim(), "")
                                    Dim dmStr As String = If(DtCirc.Rows(r)(colDiam) IsNot DBNull.Value, DtCirc.Rows(r)(colDiam).ToString(), "")
                                    Dim matStr As String = If(colMat >= 0 AndAlso DtCirc.Rows(r)(colMat) IsNot DBNull.Value, DtCirc.Rows(r)(colMat).ToString().Trim(), "")
                                    Dim diam As Single
                                    If nm <> "" AndAlso Single.TryParse(dmStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, diam) AndAlso diam > 0 Then
                                        _SeccionesCirculares(nm) = Tuple.Create(diam, matStr)
                                    End If
                                Next
                            End If
                        End If
                    Catch ex As Exception
                        Logger.Warning("Importar_Datos_de_Excel", "No se pudo leer Conc Circle: " & ex.Message)
                    End Try
                End If
            End If

            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
            _hayCambiosColumnas = True
            Return True
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error al importar")
            Return False
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        Form_Graficos.Show()
    End Sub

    Public Sub Rellenar_Columnas()
        If Proyecto.Elementos.Columnas.Info_Diseño = True Then

            For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                Combo_Elementos.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento)
                Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label)
            Next

            Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(0).Name_Elemento
        End If

    End Sub

    Public Sub Open()
        Dim dlg As New OpenFileDialog
        dlg.Filter = "Archivo|*.esm"
        dlg.Title = "Abrir Archivo"
        If dlg.ShowDialog() <> DialogResult.OK Then Exit Sub

        Try
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(dlg.FileName)
        Catch
            Try
                Dim elementos = Funciones_Programa.DeSerializar(Of cElementos)(dlg.FileName)
                Proyecto = New Proyecto()
                Proyecto.Elementos = elementos
            Catch ex As Exception
                MessageBox.Show("No se pudo abrir el archivo." & vbCrLf & ex.Message,
                                "Error al abrir", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try
        End Try

        Proyecto.Ruta = dlg.FileName
        Form_00_PaginaPrincipal.proyecto = Proyecto
        Form_00_PaginaPrincipal.SincronizarModulos()
        Form_02_00_PagInfoColumnas.Proyecto = Proyecto
        Form_02_01_ResultadosColumnas.Proyecto = Proyecto
        Form_02_01_00_RevisionCortante.Proyecto = Proyecto
        Form_02_01_02_ResultadosModelo.Proyecto = Proyecto
        _hayCambiosColumnas = False

        Rellenar_Columnas()
        VerToolStripMenuItem.Enabled = True
    End Sub

    Public Sub RefrescarDesdeProyecto()
        Proyecto = Form_00_PaginaPrincipal.proyecto
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then Return
        Combo_Elementos.Items.Clear()
        Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Clear()
        Form_02_00_PagInfoColumnas.Proyecto = Proyecto
        Form_02_01_ResultadosColumnas.Proyecto = Proyecto
        Form_02_01_00_RevisionCortante.Proyecto = Proyecto
        Form_02_01_02_ResultadosModelo.Proyecto = Proyecto
        Rellenar_Columnas()
        Form_02_00_PagInfoColumnas.RefrescarCombo()
        VerToolStripMenuItem.Enabled = True
    End Sub

    Public Sub Borrar()
        Proyecto.Elementos.Columnas.Lista_Columnas.Clear()
        Tabla_Resumen.Rows.Clear()
    End Sub

    Private Sub SaveAs(ByVal Objeto As Object)
        Try
            Dim dlg As New SaveFileDialog
            dlg.Filter = "Archivo|*.esm"
            dlg.Title = "Guardar Archivo"
            dlg.FileName = "Proyecto - " & If(Proyecto.Info?.Nombre, "ARCO")
            If dlg.ShowDialog() <> DialogResult.OK Then Exit Sub
            Proyecto.Ruta = Path.GetFullPath(dlg.FileName)
            Form_00_PaginaPrincipal.proyecto = Proyecto
            Funciones_Programa.Serializar(dlg.FileName, Objeto)
            _hayCambiosColumnas = False
            _ultimoGuardadoColumnas = DateTime.Now
            MessageBox.Show("El archivo se guardó correctamente.", "Guardar Como",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub GuardarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Save_Columnas.Click
        If String.IsNullOrEmpty(Proyecto.Ruta) Then
            SaveAs(Proyecto)
        Else
            Try
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
                _hayCambiosColumnas = False
                _ultimoGuardadoColumnas = DateTime.Now
                MessageBox.Show("El archivo se guardó correctamente.", "Guardar",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub
    Private Sub GuardarComoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAs_Columnas.Click
        SaveAs(Proyecto)
    End Sub

    Private Sub AbrirToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Open_Columnas.Click
        Open()
    End Sub

    Private Sub NuevoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles New_Columnas.Click
        Borrar()
    End Sub

    Private Sub ResultadosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Resultados_Col.Click
        Form_02_01_ResultadosColumnas.Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(0).Name_Label
        Form_02_01_ResultadosColumnas.Show()
    End Sub

    Private Sub Reporte_Col_Click(sender As Object, e As EventArgs) Handles Reporte_Col.Click
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            MessageBox.Show("No hay columnas procesadas. Ejecute el análisis primero.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim rpt As New Form_02_Reporte_Columnas
        rpt.Columnas = Proyecto.Elementos.Columnas.Lista_Columnas
        rpt.Show()
    End Sub

    Private Sub GráficasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Graficos_Col.Click
        Form_Graficos.Show()
    End Sub

    Private Sub FrameToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FrameToolStripMenuItem.Click
        Dim openFD As New OpenFileDialog With {
            .Title = "Seleccionar archivo Excel (Frame)",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If openFD.ShowDialog = Windows.Forms.DialogResult.OK Then
            ActualizarFuerzasFrame(openFD.FileName)
        End If
    End Sub

    Private Sub PierToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PierToolStripMenuItem.Click
        Dim openFD As New OpenFileDialog With {
            .Title = "Seleccionar archivo Excel (Pier)",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If openFD.ShowDialog = Windows.Forms.DialogResult.OK Then
            ActualizarFuerzasPier(openFD.FileName)
        End If
    End Sub

    ''' <summary>Lógica de envolvente compartida para Frame y Pier. Registra combinaciones y asigna fuerzas.</summary>
    Private Sub ProcesarFuerzasEnvolvente(ByVal Tabla As DataGridView, ByVal tipoElemento As String)
        Dim cf = ColumnasFuerzas(tipoElemento)
        Dim Col_Piso As Integer = cf(0)
        Dim Col_Label As Integer = cf(1)
        Dim Col_Combinacion As Integer = cf(2)
        Dim Col_P As Integer = cf(4)
        Dim Col_V2 As Integer = cf(5)
        Dim Col_V3 As Integer = cf(6)
        Dim Col_T As Integer = cf(7)
        Dim Col_M2 As Integer = cf(8)
        Dim Col_M3 As Integer = cf(9)
        Dim Col_StepType As Integer = cf(10)

        ' Registrar combinaciones únicas
        For j = 2 To Tabla.Rows.Count - 1
            If Tabla.Rows(j).Cells(Col_Piso).Value IsNot Nothing AndAlso
               Tabla.Rows(j).Cells(Col_Piso).Value.ToString <> String.Empty Then
                Dim combo As String = If(Tabla.Rows(j).Cells(Col_Combinacion).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString(), "")
                Dim stepVal As String = If(Tabla.Rows(j).Cells(Col_StepType).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_StepType).Value.ToString(), "")
                Dim clave As String = Funciones_02_Columnas.ConstruirClaveCombo(combo, stepVal)
                If clave <> "" AndAlso Not Proyecto.Elementos.Columnas.Lista_Combinaciones.Exists(Function(p) p = clave) Then
                    Proyecto.Elementos.Columnas.Lista_Combinaciones.Add(clave)
                End If
            End If
        Next

        ' Asignar fuerzas con envolvente
        For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
            Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento
            For Np = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                Dim pisoTramo As String = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso
                For j = 2 To Tabla.Rows.Count - 1
                    If Tabla.Rows(j).Cells(Col_Piso).Value Is Nothing OrElse Tabla.Rows(j).Cells(Col_Piso).Value.ToString = String.Empty Then Continue For
                    If Tabla.Rows(j).Cells(Col_Piso).Value.ToString <> pisoTramo Then Continue For
                    If If(Tabla.Rows(j).Cells(Col_Label).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Label).Value.ToString(), "") <> Elemento Then Continue For

                    Dim combo As String = If(Tabla.Rows(j).Cells(Col_Combinacion).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString(), "")
                    Dim stepVal As String = If(Tabla.Rows(j).Cells(Col_StepType).Value IsNot Nothing, Tabla.Rows(j).Cells(Col_StepType).Value.ToString(), "")
                    Dim clave As String = Funciones_02_Columnas.ConstruirClaveCombo(combo, stepVal)
                    Dim valP As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                    Dim valV2 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                    Dim valV3 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                    Dim valT As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                    Dim valM2 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                    Dim valM3 As Single = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)
                    Dim existente = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Find(Function(f) f.Name = clave)
                    If existente IsNot Nothing Then
                        If Math.Abs(valP) > Math.Abs(existente.P) Then existente.P = valP
                        If Math.Abs(valV2) > Math.Abs(existente.V2) Then existente.V2 = valV2
                        If Math.Abs(valV3) > Math.Abs(existente.V3) Then existente.V3 = valV3
                        If Math.Abs(valT) > Math.Abs(existente.T) Then existente.T = valT
                        If Math.Abs(valM2) > Math.Abs(existente.M2) Then existente.M2 = valM2
                        If Math.Abs(valM3) > Math.Abs(existente.M3) Then existente.M3 = valM3
                    Else
                        Dim fz As New Tramo_Columna.Fuerzas_Elementos With {.Name = clave, .P = valP, .V2 = valV2, .V3 = valV3, .T = valT, .M2 = valM2, .M3 = valM3}
                        Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(fz)
                    End If
                Next
            Next
        Next
    End Sub

    Private Sub Importar_Col_Frame_Click(sender As Object, e As EventArgs) Handles Importar_Col_Frame.Click
        Dim ofd As New OpenFileDialog With {
            .Title = "Importar columnas como Frame (ETABS E23)",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If ofd.ShowDialog() <> DialogResult.OK Then Return

        Dim path As String = ofd.FileName
        Me.Cursor = Cursors.WaitCursor

        Dim okDiseno = Importar_Datos_de_Excel(path, Tabla_Diseño_Flexo, "Diseño", "Frame")
        Dim okSecciones = Importar_Datos_de_Excel(path, Tabla_secciones, "Secciones", "Frame")
        Dim okFuerzas = Importar_Datos_de_Excel(path, Tabla_Fuerzas, "Fuerzas", "Frame")

        Proyecto.Elementos.Columnas.Elementos_Frame = True
        If okDiseno Then Proyecto.Elementos.Columnas.Info_Diseño = True
        If okSecciones Then Proyecto.Elementos.Columnas.Info_Secciones = True
        If okFuerzas Then Proyecto.Elementos.Columnas.Info_Fuerzas = True

        Me.Cursor = Cursors.Arrow

        Dim msg As String = "─── Resultado de la importación ───" & vbCrLf & vbCrLf
        msg &= $"  Diseño (Conc Col Sum)....  {If(okDiseno, "✔ OK  — " & (Tabla_Diseño_Flexo.Rows.Count - 2) & " filas", "✘ No encontrado")}" & vbCrLf
        msg &= $"  Secciones (Frame Sec)....  {If(okSecciones, "✔ OK  — " & (Tabla_secciones.Rows.Count - 2) & " secciones", "✘ No encontrado")}" & vbCrLf
        msg &= $"  Fuerzas (Elem Forces)....  {If(okFuerzas, "✔ OK  — " & (Tabla_Fuerzas.Rows.Count - 2) & " registros", "✘ No encontrado")}" & vbCrLf

        If okDiseno AndAlso okSecciones AndAlso okFuerzas Then
            msg &= vbCrLf & "Listo. Ejecute 'Calcular' para procesar los datos."
            MsgBox(msg, MsgBoxStyle.Information, "Importación exitosa — Frame")
        Else
            msg &= vbCrLf & "Verifique que el archivo sea un export de ETABS E23 de columnas Frame."
            MsgBox(msg, MsgBoxStyle.Exclamation, "Importación incompleta — Frame")
        End If
    End Sub

    Private Sub Importar_Col_Pier_Click(sender As Object, e As EventArgs) Handles Importar_Col_Pier.Click
        Dim ofd As New OpenFileDialog With {
            .Title = "Importar columnas como Pier (ETABS E23)",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If ofd.ShowDialog() <> DialogResult.OK Then Return

        Dim path As String = ofd.FileName
        Me.Cursor = Cursors.WaitCursor

        Dim okDiseno = Importar_Datos_de_Excel(path, Tabla_Diseño_Pier, "Diseño", "Pier")
        Dim okSecciones = Importar_Datos_de_Excel(path, Tabla_Secciones_Pier, "Secciones", "Pier")
        Dim okFuerzas = Importar_Datos_de_Excel(path, Tabla_Fuerzas_Pier, "Fuerzas", "Pier")

        Proyecto.Elementos.Columnas.Elementos_Pier = True
        If okDiseno Then Proyecto.Elementos.Columnas.Info_Diseño = True
        If okSecciones Then Proyecto.Elementos.Columnas.Info_Secciones = True
        If okFuerzas Then Proyecto.Elementos.Columnas.Info_Fuerzas = True

        Me.Cursor = Cursors.Arrow

        Dim msg As String = "─── Resultado de la importación ───" & vbCrLf & vbCrLf
        msg &= $"  Diseño (Pier Dgn Sum)....  {If(okDiseno, "✔ OK  — " & (Tabla_Diseño_Pier.Rows.Count - 2) & " filas", "✘ No encontrado")}" & vbCrLf
        msg &= $"  Secciones (Pier Sec)....   {If(okSecciones, "✔ OK  — " & (Tabla_Secciones_Pier.Rows.Count - 2) & " secciones", "✘ No encontrado")}" & vbCrLf
        msg &= $"  Fuerzas (Pier Forces)....  {If(okFuerzas, "✔ OK  — " & (Tabla_Fuerzas_Pier.Rows.Count - 2) & " registros", "✘ No encontrado")}" & vbCrLf

        If okDiseno AndAlso okSecciones AndAlso okFuerzas Then
            msg &= vbCrLf & "Listo. Ejecute 'Calcular' para procesar los datos."
            MsgBox(msg, MsgBoxStyle.Information, "Importación exitosa — Pier")
        Else
            msg &= vbCrLf & "Verifique que el archivo sea un export de ETABS E23 de columnas Pier."
            MsgBox(msg, MsgBoxStyle.Exclamation, "Importación incompleta — Pier")
        End If
    End Sub

    ''' <summary>
    ''' Valida la consistencia de los datos procesados. Retorna un reporte de texto.
    ''' Llamar después de Button2_Click para verificar que todo se cargó correctamente.
    ''' </summary>
    Public Function ValidarImportacionColumnas() As String
        Dim sb As New System.Text.StringBuilder
        Dim cols = Proyecto.Elementos.Columnas.Lista_Columnas
        Dim combos = Proyecto.Elementos.Columnas.Lista_Combinaciones

        sb.AppendLine("════════ VALIDACIÓN DE IMPORTACIÓN ════════")
        sb.AppendLine($"Tipo: {If(Proyecto.Elementos.Columnas.Elementos_Frame, "Frame", "")} {If(Proyecto.Elementos.Columnas.Elementos_Pier, "Pier", "")}")
        sb.AppendLine($"Elementos (columnas): {cols.Count}")
        sb.AppendLine($"Combinaciones registradas: {combos.Count}")
        sb.AppendLine()

        If cols.Count = 0 Then
            sb.AppendLine("⚠ Sin elementos. Verifique que se ejecutó 'Calcular' después de importar.")
            Return sb.ToString()
        End If

        ' — Tramos por elemento
        Dim minTramos = cols.Min(Function(c) c.Lista_Tramos_Columnas.Count)
        Dim maxTramos = cols.Max(Function(c) c.Lista_Tramos_Columnas.Count)
        sb.AppendLine($"Tramos por elemento: min={minTramos}, max={maxTramos}")

        ' — Validar As_Req (detectar error de unidades: valor < 0.1 mm² indica que quedó en m²)
        Dim asMin As Single = Single.MaxValue
        Dim asMax As Single = 0
        For Each col In cols
            For Each tramo In col.Lista_Tramos_Columnas
                asMin = Math.Min(asMin, Math.Max(tramo.As_Req_Bottom, tramo.As_Req_Top))
                asMax = Math.Max(asMax, Math.Max(tramo.As_Req_Bottom, tramo.As_Req_Top))
            Next
        Next
        sb.AppendLine($"As_Req: min={Math.Round(asMin, 1)} mm²  max={Math.Round(asMax, 1)} mm²")
        If asMax < 1 Then sb.AppendLine("  ⚠ As muy pequeño — posible error de unidades (esperado > 100 mm²)")
        If asMax > 500000 Then sb.AppendLine("  ⚠ As muy grande — posible error de unidades")

        ' — Validar dimensiones (B, H deben ser > 0 y razonables en metros)
        Dim bMin As Single = Single.MaxValue
        Dim hMax As Single = 0
        Dim sinSeccion As Integer = 0
        For Each col In cols
            For Each tramo In col.Lista_Tramos_Columnas
                If tramo.B_Modelo = 0 Then sinSeccion += 1
                If tramo.B_Modelo > 0 Then bMin = Math.Min(bMin, tramo.B_Modelo)
                hMax = Math.Max(hMax, tramo.H_Modelo)
            Next
        Next
        If sinSeccion > 0 Then sb.AppendLine($"  ⚠ {sinSeccion} tramo(s) sin dimensiones asignadas (revisar nombres de sección)")
        If bMin < Single.MaxValue Then sb.AppendLine($"Dimensiones: B_min={Math.Round(bMin, 3)} m  H_max={Math.Round(hMax, 3)} m")
        If hMax > 5 Then sb.AppendLine("  ⚠ H muy grande — ¿unidades en mm en lugar de m?")

        ' — Validar fuerzas
        Dim sinFuerzas As Integer = 0
        Dim maxP As Single = 0
        Dim maxV As Single = 0
        Dim conStepType As Integer = 0
        For Each col In cols
            For Each tramo In col.Lista_Tramos_Columnas
                If tramo.Lista_Combinaciones.Count = 0 Then sinFuerzas += 1
                For Each f In tramo.Lista_Combinaciones
                    maxP = Math.Max(maxP, Math.Abs(f.P))
                    maxV = Math.Max(maxV, Math.Max(Math.Abs(f.V2), Math.Abs(f.V3)))
                    If f.Name.Contains("(Max)") OrElse f.Name.Contains("(Min)") Then conStepType += 1
                Next
            Next
        Next
        If sinFuerzas > 0 Then sb.AppendLine($"  ⚠ {sinFuerzas} tramo(s) sin fuerzas asignadas")
        sb.AppendLine($"Fuerzas máx.: |P|={Math.Round(maxP, 1)} kN  |V|={Math.Round(maxV, 1)} kN")
        If conStepType > 0 Then sb.AppendLine($"Combinaciones con Step Type (Max/Min): {conStepType}")

        ' — Muestra de primeras 3 combinaciones
        sb.AppendLine()
        sb.AppendLine("Primeras combinaciones:")
        For k = 0 To Math.Min(4, combos.Count - 1)
            sb.AppendLine($"  {k + 1}. {combos(k)}")
        Next
        If combos.Count > 5 Then sb.AppendLine($"  ... ({combos.Count - 5} más)")

        sb.AppendLine()
        sb.AppendLine("════════════════════════════════════════")
        Return sb.ToString()
    End Function

    Public Sub ActualizarCortanteDesdeSeleccion()
        For Each col In Proyecto.Elementos.Columnas.Lista_Columnas
            For Each tramo In col.Lista_Tramos_Columnas
                tramo.V2 = 0
                tramo.V3 = 0
                tramo.Pu_V2 = 0
                tramo.Pu_V3 = 0
                For Each f In tramo.Lista_Combinaciones
                    If Proyecto.Elementos.Columnas.Lista_Combinaciones_Cortante.Contains(f.Name) Then
                        If Math.Abs(f.V2) > Math.Abs(tramo.V2) Then
                            tramo.V2 = f.V2
                            If f.P < 0 Then tramo.Pu_V2 = f.P
                        End If
                        If Math.Abs(f.V3) > Math.Abs(tramo.V3) Then
                            tramo.V3 = f.V3
                            If f.P < 0 Then tramo.Pu_V3 = f.P
                        End If
                    End If
                Next
            Next
        Next
    End Sub

    Private Sub Comb_Diseno_Columnas_Click(sender As Object, e As EventArgs) Handles Comb_Diseno_Columnas.Click
        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
        Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()

        For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(combo)
        Next
        For Each combo As String In Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
            If Not Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Contains(combo) Then
                Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(combo)
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Remove(combo)
            End If
        Next

        Form_Opciones_Combinaciones.OpcionLlamado = "ColumnasDiseño"
        Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones de Diseño"
        Form_Opciones_Combinaciones.ShowDialog()
    End Sub

    Private Sub Comb_ALR_Columnas_Click(sender As Object, e As EventArgs) Handles Comb_ALR_Columnas.Click
        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
        Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()

        For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(combo)
        Next
        For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR
            If Not Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Contains(combo) Then
                Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(combo)
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Remove(combo)
            End If
        Next

        Form_Opciones_Combinaciones.OpcionLlamado = "ColumnasALR"
        Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones ALR"
        Form_Opciones_Combinaciones.ShowDialog()
    End Sub

    Private Sub Comb_Cortante_Columnas_Click(sender As Object, e As EventArgs) Handles Comb_Cortante_Columnas.Click
        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
        Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()

        For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(combo)
        Next
        For Each combo As String In Proyecto.Elementos.Columnas.Lista_Combinaciones_Cortante
            If Not Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Contains(combo) Then
                Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(combo)
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Remove(combo)
            End If
        Next

        Form_Opciones_Combinaciones.OpcionLlamado = "ColumnasCortante"
        Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones Cortante"
        Form_Opciones_Combinaciones.ShowDialog()
    End Sub

    Private _hayCambiosColumnas As Boolean = False
    Private _ultimoGuardadoColumnas As DateTime = DateTime.Now
    Private _timerAutoSaveColumnas As New Timer With {.Interval = 60000}

    Private Sub Form_02_PagColumnas_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim itemAyuda As New ToolStripMenuItem("? Tablas ETABS")
        itemAyuda.ForeColor = Color.White
        itemAyuda.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler itemAyuda.Click, Sub(s, ev) Form_AyudaImportacion.MostrarModulo("Columnas")
        Menu_Columnas.Items.Add(itemAyuda)

        ' Actualizar Diseño As Req (Frame / Pier)
        Dim itemActDis As New ToolStripMenuItem("Actualizar Diseño As Req")
        itemActDis.ForeColor = Color.White
        itemActDis.BackColor = Color.FromArgb(87, 87, 87)
        Dim subActFrame As New ToolStripMenuItem("Frame")
        Dim subActPier As New ToolStripMenuItem("Pier")
        AddHandler subActFrame.Click, AddressOf ActualizarDisenoFrame_Click
        AddHandler subActPier.Click, AddressOf ActualizarDisenoPier_Click
        itemActDis.DropDownItems.Add(subActFrame)
        itemActDis.DropDownItems.Add(subActPier)
        OpcionesToolStripMenuItem.DropDownItems.Insert(1, itemActDis)

        ' Actualizar Todo (demandas + diseño + análisis en un solo paso)
        Dim itemActTodo As New ToolStripMenuItem("Actualizar Todo")
        itemActTodo.ForeColor = Color.White
        itemActTodo.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler itemActTodo.Click, AddressOf ActualizarTodo_Click
        OpcionesToolStripMenuItem.DropDownItems.Insert(2, itemActTodo)

        ' Tipo transversal columnas circulares (persiste en proyecto)
        Dim itemTransCirc As New ToolStripMenuItem("Transversal Circular")
        itemTransCirc.ForeColor = Color.White
        itemTransCirc.BackColor = Color.FromArgb(87, 87, 87)
        Dim subEspiral As New ToolStripMenuItem("Espiral") With {.CheckOnClick = False}
        Dim subEstribo As New ToolStripMenuItem("Estribo cerrado") With {.CheckOnClick = False}
        AddHandler itemTransCirc.DropDownOpening, Sub(s2, ev2)
            Dim tc As String = If(String.IsNullOrEmpty(Proyecto.Elementos.Columnas.Trans_Circular), "Espiral", Proyecto.Elementos.Columnas.Trans_Circular)
            subEspiral.Checked = (tc = "Espiral")
            subEstribo.Checked = (tc = "Estribo cerrado")
        End Sub
        AddHandler subEspiral.Click, Sub(s2, ev2)
            Proyecto.Elementos.Columnas.Trans_Circular = "Espiral"
            subEspiral.Checked = True : subEstribo.Checked = False
        End Sub
        AddHandler subEstribo.Click, Sub(s2, ev2)
            Proyecto.Elementos.Columnas.Trans_Circular = "Estribo cerrado"
            subEspiral.Checked = False : subEstribo.Checked = True
        End Sub
        itemTransCirc.DropDownItems.Add(subEspiral)
        itemTransCirc.DropDownItems.Add(subEstribo)
        OpcionesToolStripMenuItem.DropDownItems.Insert(3, itemTransCirc)

        AddHandler _timerAutoSaveColumnas.Tick, AddressOf AutoSaveColumnas_Tick
        _timerAutoSaveColumnas.Start()
        RefrescarDesdeProyecto()
    End Sub

    Private Sub AutoSaveColumnas_Tick(sender As Object, e As EventArgs)
        If Not _hayCambiosColumnas Then Exit Sub
        If String.IsNullOrEmpty(Proyecto.Ruta) Then Exit Sub
        If (DateTime.Now - _ultimoGuardadoColumnas).TotalMinutes < 10 Then Exit Sub
        Try
            Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            _ultimoGuardadoColumnas = DateTime.Now
            _hayCambiosColumnas = False
        Catch ex As Exception
            Logger.Warning("Form_02_PagColumnas.AutoGuardar",
                           "El autoguardado periódico falló. Los cambios no se han guardado en disco.")
        End Try
    End Sub

    Private Sub Form_02_PagColumnas_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not _hayCambiosColumnas Then Exit Sub
        Dim r = MessageBox.Show("Hay cambios sin guardar. ¿Guardar antes de cerrar?",
                                "Cerrar", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)
        If r = DialogResult.Yes Then GuardarToolStripMenuItem_Click(sender, e)
        If r = DialogResult.Cancel Then e.Cancel = True
    End Sub

    '--------------- Actualizar Diseño As Req (Frame / Pier) ---------------

    ''' <summary>
    ''' Abre la hoja de diseño desde un Excel ETABS y retorna su DataTable.
    ''' Detecta automáticamente "Conc Col Sum - ACI 318-14" (Frame) o "Pier Dgn Sum" (Pier).
    ''' Retorna Nothing si la hoja no existe.
    ''' </summary>
    Private Function LeerTablaDiseno(path As String, tipoElemento As String) As DataTable
        Dim stConexion As String = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"
        Using cnConex As New OleDbConnection(stConexion)
            cnConex.Open()
            Dim schema = cnConex.GetOleDbSchemaTable(OleDb.OleDbSchemaGuid.Tables, Nothing)
            Dim sheetNames = schema.Rows.Cast(Of DataRow)().Select(Function(r) r("TABLE_NAME").ToString()).ToList()

            Dim nombreHoja As String = ""
            If tipoElemento = "Frame" Then
                If sheetNames.Any(Function(s) s.Contains("Conc Col Sum")) Then
                    nombreHoja = sheetNames.First(Function(s) s.Contains("Conc Col Sum"))
                ElseIf sheetNames.Any(Function(s) s.Contains("Concrete Column Summary")) Then
                    nombreHoja = sheetNames.First(Function(s) s.Contains("Concrete Column Summary"))
                End If
            Else
                If sheetNames.Any(Function(s) s.Contains("Pier Dgn Sum")) Then
                    nombreHoja = sheetNames.First(Function(s) s.Contains("Pier Dgn Sum"))
                ElseIf sheetNames.Any(Function(s) s.Contains("Shear Wall Pier Summary")) Then
                    nombreHoja = sheetNames.First(Function(s) s.Contains("Shear Wall Pier Summary"))
                End If
            End If

            If String.IsNullOrEmpty(nombreHoja) Then Return Nothing

            Dim da As New OleDbDataAdapter(New OleDbCommand($"Select * From [{nombreHoja}]", cnConex))
            Dim ds As New DataSet()
            da.Fill(ds)
            Return ds.Tables(0)
        End Using
    End Function

    Private Sub ActualizarDisenoFrame_Click(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            MessageBox.Show("No hay elementos procesados. Importe y calcule primero.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim ofd As New OpenFileDialog With {
            .Title = "Actualizar Diseño Frame — Conc Col Sum - ACI 318-14",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If ofd.ShowDialog() <> DialogResult.OK Then Return

        Me.Cursor = Cursors.WaitCursor
        Try
            Dim actualizados As Integer = ActualizarDisenoFrameCore(ofd.FileName)
            If actualizados < 0 Then
                MsgBox("No se encontró la hoja 'Conc Col Sum - ACI 318-14' en el archivo.",
                       MsgBoxStyle.Exclamation, "Hoja no encontrada")
                Return
            End If
            MsgBox($"Diseño Frame actualizado: {actualizados} tramo(s) con nuevo As Req.",
                   MsgBoxStyle.Information, "Actualizar Diseño As Req — Frame")
        Catch ex As Exception
            Logger.Error(ex, "Form_02_PagColumnas.ActualizarDisenoFrame", "Error al actualizar As_Req desde Excel.")
            MsgBox("Error al procesar el archivo: " & ex.Message, MsgBoxStyle.Critical, "Actualizar Diseño")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
    End Sub

    Private Sub ActualizarDisenoPier_Click(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            MessageBox.Show("No hay elementos procesados. Importe y calcule primero.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim ofd As New OpenFileDialog With {
            .Title = "Actualizar Diseño Pier — Pier Dgn Sum",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If ofd.ShowDialog() <> DialogResult.OK Then Return

        Me.Cursor = Cursors.WaitCursor
        Try
            Dim sinDim As Integer = 0
            Dim actualizados As Integer = ActualizarDisenoPierCore(ofd.FileName, sinDim)
            If actualizados < 0 Then
                MsgBox("No se encontró la hoja 'Pier Dgn Sum' en el archivo.",
                       MsgBoxStyle.Exclamation, "Hoja no encontrada")
                Return
            End If
            Dim msg As String = $"Diseño Pier actualizado: {actualizados} tramo(s) con nuevo As Req."
            If sinDim > 0 Then
                msg &= $"{vbCrLf}⚠ {sinDim} tramo(s) sin dimensiones: solo se actualizó la cuantía. Importe secciones Pier para calcular As."
            End If
            MsgBox(msg, MsgBoxStyle.Information, "Actualizar Diseño As Req — Pier")
        Catch ex As Exception
            Logger.Error(ex, "Form_02_PagColumnas.ActualizarDisenoPier", "Error al actualizar As_Req Pier desde Excel.")
            MsgBox("Error al procesar el archivo: " & ex.Message, MsgBoxStyle.Critical, "Actualizar Diseño")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
    End Sub

    '--------------- Helpers para Actualizar Todo ---------------

    Private Sub ActualizarFuerzasFrame(rutaArchivo As String)
        Proyecto.Elementos.Columnas.Elementos_Frame = True
        Proyecto.Elementos.Columnas.Info_Fuerzas = True
        Importar_Datos_de_Excel(rutaArchivo, Tabla_Fuerzas, "Fuerzas", "Frame")
        ProcesarFuerzasEnvolvente(Tabla_Fuerzas, "Frame")
    End Sub

    Private Sub ActualizarFuerzasPier(rutaArchivo As String)
        Proyecto.Elementos.Columnas.Elementos_Pier = True
        Proyecto.Elementos.Columnas.Info_Fuerzas = True
        Importar_Datos_de_Excel(rutaArchivo, Tabla_Fuerzas_Pier, "Fuerzas", "Pier")
        ProcesarFuerzasEnvolvente(Tabla_Fuerzas_Pier, "Pier")
    End Sub

    ''' <summary>Actualiza As_Req desde hoja Frame. Devuelve tramos actualizados, o -1 si la hoja no existe.</summary>
    Private Function ActualizarDisenoFrameCore(rutaArchivo As String) As Integer
        Dim dt As DataTable = LeerTablaDiseno(rutaArchivo, "Frame")
        If dt Is Nothing OrElse dt.Rows.Count < 3 Then Return -1

        Dim Col_Piso As Integer = 0
        Dim Col_Label As Integer = 1
        Dim Salto As Integer = 3
        Dim Col_As_Req As Integer = 10

        For ci = 0 To Math.Min(dt.Columns.Count - 1, 14)
            Dim h As String = If(dt.Rows(0)(ci) IsNot DBNull.Value, dt.Rows(0)(ci).ToString().Trim(), "")
            If h.ToUpperInvariant().Contains("REQUIRED") Then
                Col_As_Req = ci
                Exit For
            End If
        Next

        If dt.Rows.Count >= 4 Then
            Dim secRef As String = If(dt.Rows(2)(Col_Label) IsNot DBNull.Value, dt.Rows(2)(Col_Label).ToString(), "")
            For j = 1 To 7
                If 2 + j < dt.Rows.Count Then
                    Dim secJ As String = If(dt.Rows(2 + j)(Col_Label) IsNot DBNull.Value, dt.Rows(2 + j)(Col_Label).ToString(), "")
                    If secJ <> secRef Then Salto = j : Exit For
                End If
            Next
        End If

        Dim actualizados As Integer = 0
        Dim I0 As Integer = 2
        Dim Section As String = If(dt.Rows.Count > 2 AndAlso dt.Rows(2)(Col_Label) IsNot DBNull.Value,
                                   dt.Rows(2)(Col_Label).ToString(), "")

        For i = 2 To dt.Rows.Count - 1
            For j = 1 To 7
                If I0 + j >= dt.Rows.Count Then Exit For
                Dim sj As String = If(dt.Rows(I0 + j)(Col_Label) IsNot DBNull.Value, dt.Rows(I0 + j)(Col_Label).ToString(), "")
                If sj <> Section Then Salto = j : Exit For
            Next

            Dim cellStory As Object = dt.Rows(i)(Col_Piso)
            If cellStory Is DBNull.Value OrElse cellStory.ToString() = "" Then Continue For

            Dim st4Val As Single = 0
            Dim st4Ok As Boolean = False
            If dt.Columns.Count > 4 AndAlso dt.Rows(i)(4) IsNot DBNull.Value Then
                st4Ok = Single.TryParse(dt.Rows(i)(4).ToString(),
                                        System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture, st4Val)
            End If
            If Not st4Ok OrElse st4Val <> 0 Then Continue For

            Dim nombre As String = If(dt.Rows(i)(Col_Label) IsNot DBNull.Value, dt.Rows(i)(Col_Label).ToString(), "")
            If nombre = "" Then Continue For

            Dim asBottom As Single = 0
            Dim asTop As Single = 0
            Single.TryParse(If(dt.Rows(i)(Col_As_Req) IsNot DBNull.Value, dt.Rows(i)(Col_As_Req).ToString(), ""),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, asBottom)
            Dim rowTop As Integer = i + Salto - 1
            If rowTop < dt.Rows.Count Then
                Single.TryParse(If(dt.Rows(rowTop)(Col_As_Req) IsNot DBNull.Value, dt.Rows(rowTop)(Col_As_Req).ToString(), ""),
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, asTop)
            End If

            Dim piso As String = cellStory.ToString()
            For Each col In Proyecto.Elementos.Columnas.Lista_Columnas
                If col.Name_Elemento = nombre Then
                    Dim tramo = col.Lista_Tramos_Columnas.Find(Function(t) t.Piso = piso)
                    If tramo IsNot Nothing Then
                        tramo.As_Req_Bottom = asBottom * 1000000
                        tramo.As_Req_Top = asTop * 1000000
                        actualizados += 1
                    End If
                End If
            Next

            Section = nombre
            I0 = i
        Next

        _hayCambiosColumnas = True
        If Combo_Elementos.SelectedIndex >= 0 Then Combo_Elementos_SelectedIndexChanged(Nothing, EventArgs.Empty)
        Return actualizados
    End Function

    ''' <summary>Actualiza As_Req desde hoja Pier. Devuelve tramos actualizados, o -1 si la hoja no existe.</summary>
    Private Function ActualizarDisenoPierCore(rutaArchivo As String, ByRef sinDimensiones As Integer) As Integer
        sinDimensiones = 0
        Dim dt As DataTable = LeerTablaDiseno(rutaArchivo, "Pier")
        If dt Is Nothing OrElse dt.Rows.Count < 3 Then Return -1

        Dim Col_Piso As Integer = 0
        Dim Col_Label As Integer = 1
        Dim Salto As Integer = 2
        Dim Col_As_Req As Integer = 9

        For ci = 0 To Math.Min(dt.Columns.Count - 1, 12)
            Dim h As String = If(dt.Rows(0)(ci) IsNot DBNull.Value, dt.Rows(0)(ci).ToString().Trim(), "")
            If h.ToUpperInvariant().Contains("REQUIRED") Then
                Col_As_Req = ci
                Exit For
            End If
        Next

        Dim actualizados As Integer = 0

        For i = 2 To dt.Rows.Count - 1 Step Salto
            Dim cellStory As Object = dt.Rows(i)(Col_Piso)
            If cellStory Is DBNull.Value OrElse cellStory.ToString() = "" Then Continue For

            Dim nombre As String = If(dt.Rows(i)(Col_Label) IsNot DBNull.Value, dt.Rows(i)(Col_Label).ToString(), "")
            If nombre = "" Then Continue For

            Dim cuantiaBot As Single = 0
            Dim cuantiaTop As Single = 0
            Single.TryParse(If(dt.Rows(i)(Col_As_Req) IsNot DBNull.Value, dt.Rows(i)(Col_As_Req).ToString(), ""),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, cuantiaBot)
            Dim rowTop As Integer = i + Salto - 1
            If rowTop < dt.Rows.Count Then
                Single.TryParse(If(dt.Rows(rowTop)(Col_As_Req) IsNot DBNull.Value, dt.Rows(rowTop)(Col_As_Req).ToString(), ""),
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, cuantiaTop)
            End If

            Dim piso As String = cellStory.ToString()
            For Each col In Proyecto.Elementos.Columnas.Lista_Columnas
                If col.Name_Elemento = nombre Then
                    Dim tramo = col.Lista_Tramos_Columnas.Find(Function(t) t.Piso = piso)
                    If tramo IsNot Nothing Then
                        tramo.Cuantia_Req_Bottom = cuantiaBot
                        tramo.Cuantia_Req_Top = cuantiaTop
                        If tramo.B_Modelo > 0 AndAlso tramo.H_Modelo > 0 Then
                            tramo.As_Req_Bottom = tramo.B_Modelo * tramo.H_Modelo * cuantiaBot * 10000
                            tramo.As_Req_Top = tramo.B_Modelo * tramo.H_Modelo * cuantiaTop * 10000
                        Else
                            sinDimensiones += 1
                        End If
                        actualizados += 1
                    End If
                End If
            Next
        Next

        _hayCambiosColumnas = True
        If Combo_Elementos.SelectedIndex >= 0 Then Combo_Elementos_SelectedIndexChanged(Nothing, EventArgs.Empty)
        Return actualizados
    End Function

    Private Sub ActualizarTodo_Click(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            MessageBox.Show("No hay elementos procesados. Importe y calcule primero.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If Not Proyecto.Elementos.Columnas.Elementos_Frame AndAlso
           Not Proyecto.Elementos.Columnas.Elementos_Pier Then
            MessageBox.Show("No se han definido elementos Frame ni Pier. Use 'Actualizar Demandas' primero para indicar el tipo de elementos.",
                            "Sin elementos definidos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim ofd As New OpenFileDialog With {
            .Title = "Actualizar Todo — Seleccione exportación ETABS",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If ofd.ShowDialog() <> DialogResult.OK Then Return

        Me.Cursor = Cursors.WaitCursor
        Dim resumen As New System.Text.StringBuilder()
        Try
            Dim path As String = ofd.FileName

            If Proyecto.Elementos.Columnas.Elementos_Frame Then
                ActualizarFuerzasFrame(path)
                resumen.AppendLine("• Fuerzas Frame: importadas.")
                Dim actFrame As Integer = ActualizarDisenoFrameCore(path)
                If actFrame >= 0 Then
                    resumen.AppendLine($"• Diseño Frame: {actFrame} tramo(s) actualizado(s).")
                Else
                    resumen.AppendLine("• Diseño Frame: hoja 'Conc Col Sum - ACI 318-14' no encontrada.")
                End If
            End If

            If Proyecto.Elementos.Columnas.Elementos_Pier Then
                ActualizarFuerzasPier(path)
                resumen.AppendLine("• Fuerzas Pier: importadas.")
                Dim sinDim As Integer = 0
                Dim actPier As Integer = ActualizarDisenoPierCore(path, sinDim)
                If actPier >= 0 Then
                    resumen.AppendLine($"• Diseño Pier: {actPier} tramo(s) actualizado(s).")
                    If sinDim > 0 Then
                        resumen.AppendLine($"  ⚠ {sinDim} tramo(s) sin dimensiones: solo cuantía actualizada.")
                    End If
                Else
                    resumen.AppendLine("• Diseño Pier: hoja 'Pier Dgn Sum' no encontrada.")
                End If
            End If

            Button2_Click(Nothing, EventArgs.Empty)
            resumen.AppendLine("• Análisis completo ejecutado.")

            MsgBox(resumen.ToString(), MsgBoxStyle.Information, "Actualizar Todo — Columnas")

        Catch ex As Exception
            Logger.Error(ex, "Form_02_PagColumnas.ActualizarTodo", "Error en Actualizar Todo.")
            MsgBox("Error al procesar: " & ex.Message, MsgBoxStyle.Critical, "Actualizar Todo")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
    End Sub

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

    ' ─── Construcción de candidatos de columna con coordenadas y backdrop ────────
    Private Function ConstruirCandidatosColumnas(
            etiqFrame As List(Of String),
            dtFrameD As DataTable, dtFrameS As DataTable,
            dtJoints As DataTable, dtObjFrm As DataTable,
            etiqPier As List(Of String),
            dtPierD As DataTable, dtPierS As DataTable,
            ByRef backdrop As GeometriaEstructural) As List(Of cCandidatoColumna)

        Dim candidatos As New List(Of cCandidatoColumna)()
        backdrop = New GeometriaEstructural()

        ' 1. Joints: elementLabel -> PointF(X, Y)
        Dim byElem As New Dictionary(Of String, PointF)(StringComparer.OrdinalIgnoreCase)
        If dtJoints IsNot Nothing AndAlso dtJoints.Rows.Count > 2 Then
            Dim colEN = ZipColIdx(dtJoints, "element name")
            Dim colOT = ZipColIdx(dtJoints, "object type")
            Dim colGX = ZipColIdx(dtJoints, "global x")
            Dim colGY = ZipColIdx(dtJoints, "global y")
            For r = 2 To dtJoints.Rows.Count - 1
                Dim row = dtJoints.Rows(r)
                If colOT >= 0 Then
                    Dim tp = ZipStr(row, colOT)
                    If tp.Length > 0 AndAlso Not tp.ToUpperInvariant().Contains("JOINT") Then Continue For
                End If
                Dim en = ZipStr(row, colEN)
                If en = "" Then Continue For
                Dim gx = CSng(ZipDbl(row, colGX))
                Dim gy = CSng(ZipDbl(row, colGY))
                backdrop.JointsXY.Add(New PointF(gx, gy))
                If Not byElem.ContainsKey(en) Then byElem(en) = New PointF(gx, gy)
            Next
        End If

        ' 2. Frames: objectLabel -> PointF centroide
        Dim frameToXY As New Dictionary(Of String, PointF)(StringComparer.OrdinalIgnoreCase)
        If dtObjFrm IsNot Nothing AndAlso dtObjFrm.Rows.Count > 2 Then
            Dim colOT = ZipColIdx(dtObjFrm, "object type")
            Dim colLb = ZipColIdx(dtObjFrm, "object label")
            Dim colJI = ZipColIdx(dtObjFrm, "elm jti")
            Dim colJJ = ZipColIdx(dtObjFrm, "elm jtj")
            For r = 2 To dtObjFrm.Rows.Count - 1
                Dim row = dtObjFrm.Rows(r)
                If colOT >= 0 AndAlso Not ZipStr(row, colOT).ToUpperInvariant().Contains("FRAME") Then Continue For
                Dim lbl = ZipStr(row, colLb)
                If lbl = "" Then Continue For
                Dim jtI = ZipStr(row, colJI)
                Dim jtJ = ZipStr(row, colJJ)
                Dim ptI As PointF, ptJ As PointF
                Dim haI = byElem.TryGetValue(jtI, ptI)
                Dim haJ = byElem.TryGetValue(jtJ, ptJ)
                If haI Then backdrop.FramesXY.Add(Tuple.Create(ptI, If(haJ, ptJ, ptI)))
                If Not frameToXY.ContainsKey(lbl) Then
                    If haI AndAlso haJ Then
                        frameToXY(lbl) = New PointF(CSng((ptI.X + ptJ.X) / 2), CSng((ptI.Y + ptJ.Y) / 2))
                    ElseIf haI Then
                        frameToXY(lbl) = ptI
                    ElseIf haJ Then
                        frameToXY(lbl) = ptJ
                    End If
                End If
            Next
        End If

        ' 3. Secciones Frame: nombre -> (B, H)
        Dim secDims As New Dictionary(Of String, PointF)(StringComparer.OrdinalIgnoreCase)
        If dtFrameS IsNot Nothing AndAlso dtFrameS.Rows.Count > 2 Then
            Dim cN = ZipColIdx(dtFrameS, "name")
            Dim cD = ZipColIdx(dtFrameS, "depth", "t3")
            Dim cW = ZipColIdx(dtFrameS, "width", "t2")
            For r = 2 To dtFrameS.Rows.Count - 1
                Dim row = dtFrameS.Rows(r)
                Dim nm  = ZipStr(row, cN)
                If nm = "" Then Continue For
                Dim dep = CSng(ZipDbl(row, cD))
                Dim wid = CSng(ZipDbl(row, cW))
                secDims(nm) = New PointF(Math.Min(dep, wid), Math.Max(dep, wid))
            Next
        End If

        ' 4. Seccion y piso de cada Frame label (col 3 = Design Sect, col 0 = Story en Conc Col Sum)
        Dim frameToSec   As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim frameToStory As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        If dtFrameD IsNot Nothing AndAlso dtFrameD.Rows.Count > 2 Then
            For r = 2 To dtFrameD.Rows.Count - 1
                Dim row = dtFrameD.Rows(r)
                Dim lbl = ZipStr(row, 1)
                If lbl = "" Then Continue For
                If Not frameToSec.ContainsKey(lbl) Then
                    Dim sec = ZipStr(row, 3)   ' col 3 = Design Section (col 2 = UniqueName, no es seccion)
                    If sec <> "" Then frameToSec(lbl) = sec
                    Dim sto = ZipStr(row, 0)   ' col 0 = Story — primera aparicion = piso mas bajo
                    If sto <> "" Then frameToStory(lbl) = sto
                End If
            Next
        End If

        ' 5. Candidatos Frame
        For Each lbl In etiqFrame
            Dim cand As New cCandidatoColumna()
            cand.Label = lbl : cand.Tipo = "Frame"
            Dim coord As PointF = PointF.Empty
            frameToXY.TryGetValue(lbl, coord)
            cand.X = coord.X : cand.Y = coord.Y
            Dim secNom As String = ""
            frameToSec.TryGetValue(lbl, secNom)
            cand.Seccion = secNom
            Dim dims As PointF = PointF.Empty
            If secNom <> "" Then secDims.TryGetValue(secNom, dims)
            cand.B = dims.X : cand.H = dims.Y
            Dim storyNom As String = ""
            frameToStory.TryGetValue(lbl, storyNom)
            cand.Story = storyNom
            cand.Seleccionado = True
            candidatos.Add(cand)
        Next

        ' 6. Piers: coordenadas de Pier Section Properties
        Dim pierBase As New Dictionary(Of String, Object())(StringComparer.OrdinalIgnoreCase)
        If dtPierS IsNot Nothing AndAlso dtPierS.Rows.Count > 2 Then
            Dim cPN = ZipColIdx(dtPierS, "pier")
            Dim cPS = ZipColIdx(dtPierS, "story")
            Dim cPX = ZipColIdx(dtPierS, "cg bottom x", "xcg", "xbottom", "x cg")
            Dim cPY = ZipColIdx(dtPierS, "cg bottom y", "ycg", "ybottom", "y cg")
            Dim cPB = ZipColIdx(dtPierS, "thickbot", "thick")
            Dim cPH = ZipColIdx(dtPierS, "lengbot", "leng")
            For r = 2 To dtPierS.Rows.Count - 1
                Dim row = dtPierS.Rows(r)
                Dim pn = ZipStr(row, cPN)
                If pn = "" OrElse pierBase.ContainsKey(pn) Then Continue For
                Dim pb = ZipDbl(row, cPB) : Dim ph = ZipDbl(row, cPH)
                pierBase(pn) = New Object() {
                    ZipStr(row, cPS), ZipDbl(row, cPX), ZipDbl(row, cPY),
                    Math.Min(pb, ph), Math.Max(pb, ph)
                }
            Next
        End If

        For Each lbl In etiqPier
            Dim cand As New cCandidatoColumna()
            cand.Label = lbl : cand.Tipo = "Pier"
            Dim info As Object() = Nothing
            If pierBase.TryGetValue(lbl, info) Then
                cand.Story = CStr(info(0))
                cand.X = CDbl(info(1)) : cand.Y = CDbl(info(2))
                cand.B = CDbl(info(3)) : cand.H = CDbl(info(4))
            End If
            If dtPierD IsNot Nothing AndAlso dtPierD.Rows.Count > 2 Then
                For r2 = 2 To dtPierD.Rows.Count - 1
                    Dim row = dtPierD.Rows(r2)
                    If ZipStr(row, 1) = lbl Then
                        cand.Seccion = ZipStr(row, 1)
                        If cand.Story = "" Then cand.Story = ZipStr(row, 0)
                        Exit For
                    End If
                Next
            End If
            cand.Seleccionado = True
            candidatos.Add(cand)
        Next

        Return candidatos
    End Function

End Class