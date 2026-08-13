Imports System.Data.OleDb
Imports System.IO
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

                Dim Col_Piso As Integer = Col_Diseno(0)
                Dim Col_Label As Integer = Col_Diseno(1)
                Dim Col_Seccion As Integer = Col_Diseno(2)
                Dim Salto As Integer = Col_Diseno(3)
                Dim Col_As_Req As Integer = Col_Diseno(4)

                For i = 0 To 12
                    Dim C As String = Tabla.Rows(0).Cells(i).Value.ToString
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
                        Seccion.Cuantia_Req_Top = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_As_Req).Value)

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

    Public Function Importar_Datos_de_Excel(ByRef path As String,
                                            ByVal Datagrid As DataGridView,
                                            ByVal Op As String,
                                            ByVal Elemento As String) As Boolean
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
                MsgBox($"No se encontró la hoja de {Op} ({Elemento}) en el archivo.{vbCrLf}Verifique que el archivo corresponde al tipo correcto.", MsgBoxStyle.Exclamation, "Hoja no encontrada")
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
        Proyecto.Elementos.Columnas.Elementos_Frame = True
        Proyecto.Elementos.Columnas.Info_Fuerzas = True

        Dim openFD As New OpenFileDialog With {
            .Title = "Seleccionar archivo Excel (Frame)",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If openFD.ShowDialog = Windows.Forms.DialogResult.OK Then
            Importar_Datos_de_Excel(openFD.FileName, Tabla_Fuerzas, "Fuerzas", "Frame")
            ProcesarFuerzasEnvolvente(Tabla_Fuerzas, "Frame")
        End If
    End Sub

    Private Sub PierToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PierToolStripMenuItem.Click
        Proyecto.Elementos.Columnas.Elementos_Pier = True
        Proyecto.Elementos.Columnas.Info_Fuerzas = True

        Dim openFD As New OpenFileDialog With {
            .Title = "Seleccionar archivo Excel (Pier)",
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*.xlsx",
            .Multiselect = False
        }
        If openFD.ShowDialog = Windows.Forms.DialogResult.OK Then
            Importar_Datos_de_Excel(openFD.FileName, Tabla_Fuerzas_Pier, "Fuerzas", "Pier")
            ProcesarFuerzasEnvolvente(Tabla_Fuerzas_Pier, "Pier")
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

        AddHandler _timerAutoSaveColumnas.Tick, AddressOf AutoSaveColumnas_Tick
        _timerAutoSaveColumnas.Start()
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

End Class