Imports System.IO
Imports System.Data.OleDb
Imports ARCO.Funciones_02_Columnas

Public Class Form_02_PagColumnas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public Shared Columna As New Columna
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Cursor = Cursors.WaitCursor

        'Try

        If Op_Flexo.Checked = True Then
            Proyecto.Columnas.Verificacion_Flexo_Compresion = True
        End If
        If Op_Cortante.Checked = True Then
            Proyecto.Columnas.Verificacion_Cortante = True
        End If
        If Op_Confinamiento.Checked = True Then
            Proyecto.Columnas.Verificacion_Confinamiento = True
        End If
        If Op_ALR.Checked = True Then
            Proyecto.Columnas.Verificacion_ALR = True
        End If

        If Proyecto.Columnas.Elementos_Frame = True Then
            Dim Tabla As DataGridView

            Dim Col_Diseno = Columnas_Diseno("Frame")
            Dim Col_Secciones = Columnas_Secciones("Frame")
            Dim Col_Fuerzas = Columnas_Fuerzas("Frame")

            If Proyecto.Columnas.Info_Diseño = True Then
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
                        If Tabla.Rows(I0 + j).Cells(1).Value <> Section Then
                            Salto = j - 0
                            Exit For
                        End If
                    Next

                    If Tabla.Rows(i).Cells(0).Value <> String.Empty And Tabla.Rows(i).Cells(4).Value = 0 Then
                        Dim Seccion As New Tramo_Columna
                        Seccion.Name_Elemento = Tabla.Rows(i).Cells(Col_Label).Value
                        Seccion.Piso = Tabla.Rows(i).Cells(Col_Piso).Value
                        Seccion.Seccion = Tabla.Rows(i).Cells(Col_Seccion).Value

                        Seccion.As_Req_Bottom = Convert.ToSingle(Tabla.Rows(i).Cells(Col_As_Req).Value)
                        Seccion.As_Req_Top = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_As_Req).Value)

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

                    If Proyecto.Columnas.Lista_Columnas.Exists(Function(p) p.Name_Elemento = Columna_.Name_Elemento) Then
                    Else
                        Proyecto.Columnas.Lista_Columnas.Add(Columna_)
                        Combo_Elementos.Items.Add(Columna_.Name_Elemento)
                    End If
                Next
            End If

            If Proyecto.Columnas.Info_Secciones = True Then
                Tabla = Tabla_secciones

                Dim Col_Piso As Integer = Col_Secciones(0)
                Dim Col_Name As Integer = Col_Secciones(1)
                Dim Col_Material As Integer = Col_Secciones(2)
                Dim Col_B As Integer = Col_Secciones(3)
                Dim Col_H As Integer = Col_Secciones(4)

                For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1

                        For j = 2 To Tabla.Rows.Count - 1
                            If Tabla.Rows(j).Cells(Col_Name).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Name).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Seccion Then
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo = Math.Min(Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value) / 1000, Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value) / 1000)
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo = Math.Max(Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value) / 1000, Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value) / 1000)
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Plano = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Plano = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).fc = Convert.ToSingle(Mid(Tabla.Rows(j).Cells(Col_Material).Value, 1, 2))
                                Dim fc As Single = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).fc
                                'If Proyecto.Columnas.Lista_fc.Exists(Function(p) p = fc) Then
                                'Else
                                '    Proyecto.Columnas.Lista_fc.Add(fc)
                                'End If
                            End If
                        Next
                    Next
                Next
            End If

            If Proyecto.Columnas.Info_Fuerzas = True Then
                Tabla = Tabla_Fuerzas

                Dim Col_Piso As Integer = Col_Fuerzas(0)
                Dim Col_Label As Integer = Col_Fuerzas(1)
                Dim Col_Combinacion As Integer = Col_Fuerzas(2)
                Dim Salto As Integer = Col_Fuerzas(3)
                Dim Col_P As Integer = Col_Fuerzas(4)
                Dim Col_V2 As Integer = Col_Fuerzas(5)
                Dim Col_V3 As Integer = Col_Fuerzas(6)
                Dim Col_T As Integer = Col_Fuerzas(7)
                Dim Col_M2 As Integer = Col_Fuerzas(8)
                Dim Col_M3 As Integer = Col_Fuerzas(9)

                'Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(2).Cells(Col_Combinacion).Value.ToString)
                For j = 2 To Tabla.Rows.Count - 1
                    If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty Then
                        Dim F As Integer = j
                        If Not Proyecto.Columnas.Lista_Combinaciones.Exists(Function(p) p = Tabla.Rows(F).Cells(Col_Combinacion).Value) Then
                            Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString)
                        End If
                    End If
                Next

                For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1

                        For NC = 0 To Proyecto.Columnas.Lista_Combinaciones.Count - 1

                            For j = 2 To Tabla_Fuerzas.Rows.Count - 1 Step Salto
                                If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Piso).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso And Tabla.Rows(j).Cells(Col_Label).Value = Elemento And Tabla.Rows(j).Cells(Col_Combinacion).Value = Proyecto.Columnas.Lista_Combinaciones(NC).ToString Then
                                    Dim Fuerza As New Tramo_Columna.Fuerzas_Elementos

                                    Fuerza.Name = Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString
                                    Fuerza.P = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                                    Fuerza.V2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                                    Fuerza.V3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                                    Fuerza.T = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                                    Fuerza.M2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                                    Fuerza.M3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)

                                    Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(Fuerza)

                                    If Fuerza.Name.Contains("Cortante") Then
                                        If Math.Abs(Fuerza.V2) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2) Then
                                            Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2 = Fuerza.V2
                                            If Fuerza.P < 0 Then
                                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V2 = Fuerza.P
                                            End If
                                        End If
                                        If Math.Abs(Fuerza.V3) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3) Then
                                            Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3 = Fuerza.V3
                                            If Fuerza.P < 0 Then
                                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V3 = Fuerza.P
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                        Next
                    Next
                Next
            End If
        End If

        If Proyecto.Columnas.Elementos_Pier = True Then
            Dim Tabla As DataGridView

            Dim Col_Diseno = Columnas_Diseno("Pier")
            Dim Col_Secciones = Columnas_Secciones("Pier")
            Dim Col_Fuerzas = Columnas_Fuerzas("Pier")

            If Proyecto.Columnas.Info_Diseño = True Then
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

                    If Proyecto.Columnas.Lista_Columnas.Exists(Function(p) p.Name_Elemento = Columna_.Name_Elemento) Then
                    Else
                        Proyecto.Columnas.Lista_Columnas.Add(Columna_)
                        Combo_Elementos.Items.Add(Columna_.Name_Elemento)
                    End If
                Next
            End If

            If Proyecto.Columnas.Info_Secciones = True Then
                Tabla = Tabla_Secciones_Pier

                Dim Col_Piso As Integer = Col_Secciones(0)
                Dim Col_Name As Integer = Col_Secciones(1)
                Dim Col_Material As Integer = Col_Secciones(2)
                Dim Col_B As Integer = Col_Secciones(3)
                Dim Col_H As Integer = Col_Secciones(4)

                For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                        For j = 2 To Tabla.Rows.Count - 1
                            If Tabla.Rows(j).Cells(Col_Name).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Name).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Seccion And Tabla.Rows(j).Cells(Col_Piso).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso Then
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo = Math.Min(Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value), Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value)) / 1000
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo = Math.Max(Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value), Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value)) / 1000
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Plano = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Plano = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).fc = Convert.ToSingle(Mid(Tabla.Rows(j).Cells(Col_Material).Value, 1, 2))

                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).As_Req_Bottom = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo * Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo * Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Cuantia_Req_Bottom * 10000
                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).As_Req_Top = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).B_Modelo * Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).H_Modelo * Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Cuantia_Req_Top * 10000

                                Exit For
                            End If
                        Next
                    Next
                Next
            End If

            If Proyecto.Columnas.Info_Fuerzas = True Then
                Tabla = Tabla_Fuerzas_Pier

                Dim Col_Piso As Integer = Col_Fuerzas(0)
                Dim Col_Label As Integer = Col_Fuerzas(1)
                Dim Col_Combinacion As Integer = Col_Fuerzas(2)
                Dim Salto As Integer = Col_Fuerzas(3)
                Dim Col_P As Integer = Col_Fuerzas(4)
                Dim Col_V2 As Integer = Col_Fuerzas(5)
                Dim Col_V3 As Integer = Col_Fuerzas(6)
                Dim Col_T As Integer = Col_Fuerzas(7)
                Dim Col_M2 As Integer = Col_Fuerzas(8)
                Dim Col_M3 As Integer = Col_Fuerzas(9)

                'Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(2).Cells(Col_Combinacion).Value.ToString)
                For j = 2 To Tabla.Rows.Count - 1
                    If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty Then
                        Dim F As Integer = j
                        If Not Proyecto.Columnas.Lista_Combinaciones.Exists(Function(p) p = Tabla.Rows(F).Cells(Col_Combinacion).Value) Then
                            Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString)
                        End If
                    End If
                Next

                For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
                    Dim Elemento As String = Proyecto.Columnas.Lista_Columnas(i).Name_Elemento

                    For Np = 0 To Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1

                        For NC = 0 To Proyecto.Columnas.Lista_Combinaciones.Count - 1

                            For j = 2 To Tabla.Rows.Count - 1 Step Salto
                                If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Piso).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso And Tabla.Rows(j).Cells(Col_Label).Value = Elemento And Tabla.Rows(j).Cells(Col_Combinacion).Value = Proyecto.Columnas.Lista_Combinaciones(NC).ToString Then
                                    Dim Fuerza As New Tramo_Columna.Fuerzas_Elementos

                                    Fuerza.Name = Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString
                                    Fuerza.P = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                                    Fuerza.V2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                                    Fuerza.V3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                                    Fuerza.T = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                                    Fuerza.M2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                                    Fuerza.M3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)

                                    Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(Fuerza)

                                    If Fuerza.Name.Contains("Cortante") Then
                                        If Math.Abs(Fuerza.V2) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2) Then
                                            Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2 = Fuerza.V2
                                            If Fuerza.P < 0 Then
                                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V2 = Fuerza.P
                                            End If
                                        End If
                                        If Math.Abs(Fuerza.V3) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3) Then
                                            Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3 = Fuerza.V3
                                            If Fuerza.P < 0 Then
                                                Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V3 = Fuerza.P
                                            End If
                                        End If
                                    End If
                                End If
                            Next
                        Next
                    Next
                Next
            End If
        End If

        If Proyecto.Columnas.Verificacion_ALR = True Then
            For NC = 0 To Proyecto.Columnas.Lista_Combinaciones.Count - 1
                Form_Combinaciones.Combo_Combinaciones.Items.Add(Proyecto.Columnas.Lista_Combinaciones(NC).ToString)
            Next
            Form_Combinaciones.Combo_Combinaciones.Text = Proyecto.Columnas.Lista_Combinaciones(0).ToString
            If Proyecto.Columnas.Lista_Combinaciones_ALR.Count > 0 Then
                For i = 0 To Proyecto.Columnas.Lista_Combinaciones_ALR.Count - 1
                    Form_Combinaciones.Tabla_combinaciones.Rows.Add(Proyecto.Columnas.Lista_Combinaciones_ALR(i))
                Next
            End If
            Form_Combinaciones.Show()
        End If

        If Proyecto.Columnas.Info_Diseño = True Then
            Combo_Elementos.Text = Proyecto.Columnas.Lista_Columnas(0).Name_Elemento
        End If
        'Catch ex As exception
        '    messagebox.show("error al leer la información.", "error", messageboxbuttons.ok, messageboxicon.error)
        'Finally
        Cursor = Cursors.Arrow
        'End Try


    End Sub

    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Try
            Tabla_Resumen.Rows.Clear()
            Tabla_Resumen.Columns.Clear()
            If Proyecto.Columnas.Info_Diseño = True Then
                Tabla_Resumen.Columns.Add("Column1", "Elemento")
                Tabla_Resumen.Columns.Add("Column2", "Piso")
                Tabla_Resumen.Columns.Add("Column3", "Sección")
                Tabla_Resumen.Columns.Add("Column4", "Estación")
                Tabla_Resumen.Columns.Add("Column5", "As Requerido (mm2)")
            End If
            Dim Elemento As String = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Name_Elemento
            Dim Seccion = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Lista_Tramos_Columnas
            For i = 0 To (Seccion.Count - 1) * 2
                Tabla_Resumen.Rows.Add()
            Next


            If Proyecto.Columnas.Info_Fuerzas = True Then
                Tabla_Resumen.Columns.Add("Column6", "V2 (kN)")
                Tabla_Resumen.Columns.Add("Column7", "V3 (kN)")
            End If
            If Proyecto.Columnas.Info_Secciones = True Then
                Tabla_Resumen.Columns.Add("Column8", "Base (m)")
                Tabla_Resumen.Columns.Add("Column9", "Alto (m)")
                Tabla_Resumen.Columns.Add("Column10", "f'c (MPa)")
            End If

            If Proyecto.Columnas.Info_Diseño = True Then
                Tabla_Resumen.Rows(0).Cells(0).Value = Elemento
                For i = 0 To (Seccion.Count - 1) * 2 Step 2
                    Tabla_Resumen.Rows(i).Cells(1).Value = Seccion(i / 2).Piso
                    Tabla_Resumen.Rows(i).Cells(2).Value = Seccion(i / 2).Seccion
                    Tabla_Resumen.Rows(i + 1).Cells(2).Value = Seccion(i / 2).Seccion
                    Tabla_Resumen.Rows(i).Cells(3).Value = "Top"
                    Tabla_Resumen.Rows(i + 1).Cells(3).Value = "Bottom"
                    Tabla_Resumen.Rows(i).Cells(4).Value = Seccion(i / 2).As_Req_Top
                    Tabla_Resumen.Rows(i + 1).Cells(4).Value = Seccion(i / 2).As_Req_Bottom

                    If Proyecto.Columnas.Info_Fuerzas = True Then
                        Tabla_Resumen.Rows(i).Cells(5).Value = Math.Round(Seccion(i / 2).V2, 2)
                        Tabla_Resumen.Rows(i).Cells(6).Value = Math.Round(Seccion(i / 2).V3, 2)
                    End If
                    If Proyecto.Columnas.Info_Secciones = True Then
                        Tabla_Resumen.Rows(i).Cells(7).Value = Seccion(i / 2).B_Modelo
                        Tabla_Resumen.Rows(i).Cells(8).Value = Seccion(i / 2).H_Modelo
                        Tabla_Resumen.Rows(i).Cells(9).Value = Seccion(i / 2).fc
                    End If

                Next
            End If

        Catch ex As Exception

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

        End Try

    End Sub

    Private Sub SeccionesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Insertar_Secciones_Col.Click
        Form_02_00_PagInfoColumnas.Show()
    End Sub

    '-------------------- Importar Tablas desde Excel -----------------------
    Private Sub DiseñoAFlexoCompresiónToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles Diseno_Col_Frame.Click
        Proyecto.Columnas.Elementos_Frame = True
        Proyecto.Columnas.Info_Diseño = True
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
        Proyecto.Columnas.Elementos_Frame = True
        Proyecto.Columnas.Info_Secciones = True
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
        Proyecto.Columnas.Elementos_Frame = True
        Proyecto.Columnas.Info_Fuerzas = True
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
        Proyecto.Columnas.Elementos_Pier = True
        Proyecto.Columnas.Info_Diseño = True
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
        Proyecto.Columnas.Elementos_Pier = True
        Proyecto.Columnas.Info_Secciones = True
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
        Proyecto.Columnas.Elementos_Pier = True
        Proyecto.Columnas.Info_Fuerzas = True
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

    Public Function Importar_Datos_de_Excel(ByRef path As String, ByVal Datagrid As DataGridView, ByVal Op As String, ByVal Elemento As String)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            cnConex.Open()

            If Op = "Diseño" Then
                If Elemento = "Frame" Then
                    Dim Cmd As New OleDbCommand("Select * From [Concrete Column Summary - ACI 3$]")
                    Cmd.Connection = cnConex
                    Da.SelectCommand = Cmd
                Else
                    Dim Cmd As New OleDbCommand("Select * From [Shear Wall Pier Summary - ACI 3$]")
                    Cmd.Connection = cnConex
                    Da.SelectCommand = Cmd
                End If
            ElseIf Op = "Secciones" Then
                If Elemento = "Frame" Then
                    Dim Cmd As New OleDbCommand("Select * From [Frame Sections$]")
                    Cmd.Connection = cnConex
                    Da.SelectCommand = Cmd
                Else
                    Dim Cmd As New OleDbCommand("Select * From [Pier Section Properties$]")
                    Cmd.Connection = cnConex
                    Da.SelectCommand = Cmd
                End If
            ElseIf Op = "Fuerzas" Then
                If Elemento = "Frame" Then
                    Dim Cmd As New OleDbCommand("Select * From [Column Forces$]")
                    Cmd.Connection = cnConex
                    Da.SelectCommand = Cmd
                Else
                    Dim Cmd As New OleDbCommand("Select * From [Pier Forces$]")
                    Cmd.Connection = cnConex
                    Da.SelectCommand = Cmd
                End If
            End If
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        Form_Graficos.Show()
    End Sub

    Public Sub Rellenar_Columnas()
        If Proyecto.Columnas.Info_Diseño = True Then

            For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
                Combo_Elementos.Items.Add(Proyecto.Columnas.Lista_Columnas(i).Name_Elemento)
                Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Add(Proyecto.Columnas.Lista_Columnas(i).Name_Label)
            Next

            Combo_Elementos.Text = Proyecto.Columnas.Lista_Columnas(0).Name_Elemento
        End If

    End Sub

    Public Sub Open()
        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(Open.FileName)

            Rellenar_Columnas()

            VerToolStripMenuItem.Enabled = True
        End If

    End Sub

    Public Sub Borrar()
        Proyecto.Columnas.Lista_Columnas.Clear()
        Tabla_Resumen.Rows.Clear()
    End Sub

    Private Sub SaveAs(ByVal Objeto As Object)
        Try
            Dim SaveAs As New SaveFileDialog
            SaveAs.Filter = "Archivo|*.esm"
            SaveAs.Title = "Guardar Archivo"
            SaveAs.FileName = Convert.ToString("RevisiónColumnas_Proyecto - " & PagInfoGeneral.NameProject.Text)
            SaveAs.ShowDialog()
            If SaveAs.FileName <> String.Empty Then
                Proyecto.Ruta = Path.GetFullPath(SaveAs.FileName)
                Funciones_Programa.Serializar(SaveAs.FileName, Objeto)
            End If
        Catch ex As Exception

        End Try

    End Sub
    Private Sub GuardarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Save_Columnas.Click
        Try
            If Proyecto.Ruta = String.Empty Then
                SaveAs(Proyecto)
            Else
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            End If
        Catch ex As Exception

        End Try
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
        Form_02_01_ResultadosColumnas.Combo_Elementos.Text = Proyecto.Columnas.Lista_Columnas(0).Name_Label
        Form_02_01_ResultadosColumnas.Show()
    End Sub

    Private Sub GráficasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Graficos_Col.Click
        Form_Graficos.Show()
    End Sub

    Private Sub FrameToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FrameToolStripMenuItem.Click
        Proyecto.Columnas.Elementos_Frame = True
        Proyecto.Columnas.Info_Fuerzas = True
        Tabla_Fuerzas.Rows.Clear()

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

        Dim Tabla As DataGridView

        Tabla = Tabla_Fuerzas
        Dim Col_Fuerzas = Columnas_Fuerzas("Frame")

        Dim Col_Piso As Integer = Col_Fuerzas(0)
        Dim Col_Label As Integer = Col_Fuerzas(1)
        Dim Col_Combinacion As Integer = Col_Fuerzas(2)
        Dim Salto As Integer = Col_Fuerzas(3)
        Dim Col_P As Integer = Col_Fuerzas(4)
        Dim Col_V2 As Integer = Col_Fuerzas(5)
        Dim Col_V3 As Integer = Col_Fuerzas(6)
        Dim Col_T As Integer = Col_Fuerzas(7)
        Dim Col_M2 As Integer = Col_Fuerzas(8)
        Dim Col_M3 As Integer = Col_Fuerzas(9)

        'Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(2).Cells(Col_Combinacion).Value.ToString)
        For j = 2 To Tabla.Rows.Count - 1
            If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty Then
                Dim F As Integer = j
                If Not Proyecto.Columnas.Lista_Combinaciones.Exists(Function(p) p = Tabla.Rows(F).Cells(Col_Combinacion).Value) Then
                    Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString)
                End If
            End If
        Next

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
            Dim Elemento As String = Proyecto.Columnas.Lista_Columnas(i).Name_Elemento

            For Np = 0 To Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1

                For NC = 0 To Proyecto.Columnas.Lista_Combinaciones.Count - 1

                    For j = 2 To Tabla_Fuerzas.Rows.Count - 1 Step Salto
                        If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Piso).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso And Tabla.Rows(j).Cells(Col_Label).Value = Elemento And Tabla.Rows(j).Cells(Col_Combinacion).Value = Proyecto.Columnas.Lista_Combinaciones(NC).ToString Then
                            Dim Fuerza As New Tramo_Columna.Fuerzas_Elementos

                            Fuerza.Name = Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString
                            Fuerza.P = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                            Fuerza.V2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                            Fuerza.V3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                            Fuerza.T = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                            Fuerza.M2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                            Fuerza.M3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)

                            Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(Fuerza)

                            If Fuerza.Name.Contains("Cortante") Then
                                If Math.Abs(Fuerza.V2) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2) Then
                                    Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2 = Fuerza.V2
                                    If Fuerza.P < 0 Then
                                        Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V2 = Fuerza.P
                                    End If
                                End If
                                If Math.Abs(Fuerza.V3) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3) Then
                                    Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3 = Fuerza.V3
                                    If Fuerza.P < 0 Then
                                        Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V3 = Fuerza.P
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            Next
        Next




    End Sub

    Private Sub PierToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PierToolStripMenuItem.Click
        Proyecto.Columnas.Elementos_Pier = True
        Proyecto.Columnas.Info_Fuerzas = True
        Tabla_Fuerzas_Pier.Rows.Clear()
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

        Dim Tabla As DataGridView

        Tabla = Tabla_Fuerzas_Pier
        Dim Col_Fuerzas = Columnas_Fuerzas("Pier")

        Dim Col_Piso As Integer = Col_Fuerzas(0)
        Dim Col_Label As Integer = Col_Fuerzas(1)
        Dim Col_Combinacion As Integer = Col_Fuerzas(2)
        Dim Salto As Integer = Col_Fuerzas(3)
        Dim Col_P As Integer = Col_Fuerzas(4)
        Dim Col_V2 As Integer = Col_Fuerzas(5)
        Dim Col_V3 As Integer = Col_Fuerzas(6)
        Dim Col_T As Integer = Col_Fuerzas(7)
        Dim Col_M2 As Integer = Col_Fuerzas(8)
        Dim Col_M3 As Integer = Col_Fuerzas(9)

        For j = 2 To Tabla.Rows.Count - 1
            If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty Then
                Dim F As Integer = j
                If Not Proyecto.Columnas.Lista_Combinaciones.Exists(Function(p) p = Tabla.Rows(F).Cells(Col_Combinacion).Value) Then
                    Proyecto.Columnas.Lista_Combinaciones.Add(Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString)
                End If
            End If
        Next

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
            Dim Elemento As String = Proyecto.Columnas.Lista_Columnas(i).Name_Elemento

            For Np = 0 To Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1

                For NC = 0 To Proyecto.Columnas.Lista_Combinaciones.Count - 1

                    For j = 2 To Tabla.Rows.Count - 1 Step Salto
                        If Tabla.Rows(j).Cells(Col_Piso).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Piso).Value = Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Piso And Tabla.Rows(j).Cells(Col_Label).Value = Elemento And Tabla.Rows(j).Cells(Col_Combinacion).Value = Proyecto.Columnas.Lista_Combinaciones(NC).ToString Then
                            Dim Fuerza As New Tramo_Columna.Fuerzas_Elementos

                            Fuerza.Name = Tabla.Rows(j).Cells(Col_Combinacion).Value.ToString
                            Fuerza.P = Convert.ToSingle(Tabla.Rows(j).Cells(Col_P).Value)
                            Fuerza.V2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V2).Value)
                            Fuerza.V3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_V3).Value)
                            Fuerza.T = Convert.ToSingle(Tabla.Rows(j).Cells(Col_T).Value)
                            Fuerza.M2 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M2).Value)
                            Fuerza.M3 = Convert.ToSingle(Tabla.Rows(j).Cells(Col_M3).Value)

                            Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Lista_Combinaciones.Add(Fuerza)

                            If Fuerza.Name.Contains("Cortante") Then
                                If Math.Abs(Fuerza.V2) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2) Then
                                    Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V2 = Fuerza.V2
                                    If Fuerza.P < 0 Then
                                        Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V2 = Fuerza.P
                                    End If
                                End If
                                If Math.Abs(Fuerza.V3) > Math.Abs(Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3) Then
                                    Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).V3 = Fuerza.V3
                                    If Fuerza.P < 0 Then
                                        Proyecto.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(Np).Pu_V3 = Fuerza.P
                                    End If
                                End If
                            End If
                        End If
                    Next
                Next
            Next
        Next


    End Sub
End Class