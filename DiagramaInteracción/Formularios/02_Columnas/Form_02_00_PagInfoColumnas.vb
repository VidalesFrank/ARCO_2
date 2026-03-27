Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_02_Columnas
Public Class Form_02_00_PagInfoColumnas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged

        Try
            Tabla_Info_Seccion.Rows.Clear()

            Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Name_Elemento
            Dim Seccion = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Lista_Tramos_Columnas

            For i = 0 To (Seccion.Count - 1) * 2
                Tabla_Info_Seccion.Rows.Add()
            Next

            For i = 0 To (Seccion.Count - 1) * 2 Step 2
                Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).B_Plano
                Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).H_Plano
                Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

                If Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Ref_Modificado = False Then
                    For j = 5 To 12
                        Tabla_Info_Seccion.Rows(i).Cells(j).Value = 0
                        Tabla_Info_Seccion.Rows(i + 1).Cells(j).Value = 0
                    Next
                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = 0
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = 0
                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = "#3"
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = 0
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_2
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_3
                    Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_4
                    Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_5
                    Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_6
                    Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_7
                    Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_8
                    Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_10

                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2
                    Tabla_Info_Seccion.Rows(i + 1).Cells(6).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3
                    Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4
                    Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5
                    Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6
                    Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7
                    Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8
                    Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10

                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Num_Ramas_Largo
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Num_Ramas_Corto
                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Numero_Barras_Estribo
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = Seccion(i / 2).Separacion_Estribos
                End If
            Next

        Catch ex As Exception
        Finally
            T_Seccion.Text = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Name_Label
        End Try

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Try
        Dim Seccion = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Lista_Tramos_Columnas
        Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Name_Elemento
        Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Name_Label = T_Seccion.Text

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Seccion(i / 2).fc = Tabla_Info_Seccion.Rows(i).Cells(1).Value
            Seccion(i / 2).B_Plano = Tabla_Info_Seccion.Rows(i).Cells(2).Value
            Seccion(i / 2).H_Plano = Tabla_Info_Seccion.Rows(i).Cells(3).Value

            Seccion(i / 2).Refuerzo_Col_Top.Barras_2 = Tabla_Info_Seccion.Rows(i).Cells(5).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_3 = Tabla_Info_Seccion.Rows(i).Cells(6).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_4 = Tabla_Info_Seccion.Rows(i).Cells(7).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_5 = Tabla_Info_Seccion.Rows(i).Cells(8).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_6 = Tabla_Info_Seccion.Rows(i).Cells(9).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_7 = Tabla_Info_Seccion.Rows(i).Cells(10).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_8 = Tabla_Info_Seccion.Rows(i).Cells(11).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_10 = Tabla_Info_Seccion.Rows(i).Cells(12).Value

            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2 = Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3 = Tabla_Info_Seccion.Rows(i + 1).Cells(6).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4 = Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5 = Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6 = Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7 = Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8 = Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10 = Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value

            Seccion(i / 2).As_Col_Top = 32 * Seccion(i / 2).Refuerzo_Col_Top.Barras_2 + 71 * Seccion(i / 2).Refuerzo_Col_Top.Barras_3 + 129 * Seccion(i / 2).Refuerzo_Col_Top.Barras_4 + 199 * Seccion(i / 2).Refuerzo_Col_Top.Barras_5 + 284 * Seccion(i / 2).Refuerzo_Col_Top.Barras_6 + 387 * Seccion(i / 2).Refuerzo_Col_Top.Barras_7 + 510 * Seccion(i / 2).Refuerzo_Col_Top.Barras_8 + 819 * Seccion(i / 2).Refuerzo_Col_Top.Barras_10
            Seccion(i / 2).Cuantia_Col_Top = Seccion(i / 2).As_Col_Top / (Seccion(i / 2).B_Plano * Seccion(i / 2).H_Plano * 1000000)
            Seccion(i / 2).Cantidad_Barras_Top = Seccion(i / 2).Refuerzo_Col_Top.Barras_2 + Seccion(i / 2).Refuerzo_Col_Top.Barras_3 + Seccion(i / 2).Refuerzo_Col_Top.Barras_4 + Seccion(i / 2).Refuerzo_Col_Top.Barras_5 + Seccion(i / 2).Refuerzo_Col_Top.Barras_6 + Seccion(i / 2).Refuerzo_Col_Top.Barras_7 + Seccion(i / 2).Refuerzo_Col_Top.Barras_8 + Seccion(i / 2).Refuerzo_Col_Top.Barras_10

            Seccion(i / 2).As_Col_Bottom = 32 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2 + 71 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3 + 129 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4 + 199 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5 + 284 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6 + 387 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7 + 510 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8 + 819 * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10
            Seccion(i / 2).Cuantia_Col_Bottom = Seccion(i / 2).As_Col_Bottom / (Seccion(i / 2).B_Plano * Seccion(i / 2).H_Plano * 1000000)
            Seccion(i / 2).Cantidad_Barras_Bottom = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10

            Dim As_Equivalente_Top As Single = Seccion(i / 2).As_Col_Top / Seccion(i / 2).Cantidad_Barras_Top
            Dim As_Equivalente_Bottom As Single = Seccion(i / 2).As_Col_Bottom / Seccion(i / 2).Cantidad_Barras_Bottom

            If Seccion(i / 2).Lista_Detalles_Refuerzo_Top.Count > 2 Then
                Seccion(i / 2).Lista_Detalles_Refuerzo_Top.Clear()
            End If
            If Seccion(i / 2).Lista_Detalles_Refuerzo_Bottom.Count > 2 Then
                Seccion(i / 2).Lista_Detalles_Refuerzo_Bottom.Clear()
            End If

            Dim Bc As Single = Seccion(i / 2).B_Plano - 0.1
            Dim Hc As Single = Seccion(i / 2).H_Plano - 0.1
            Dim Cant_Lado_Corto As Single
            Dim Cant_Lado_Largo As Single
            If 0.1 < Bc And Bc < 0.35 Then
                Cant_Lado_Corto = 1
            Else
                Cant_Lado_Corto = Math.Round(Bc / 0.15, 0)
            End If
            Seccion(i / 2).Cantidad_Lado_Corto_Top = Cant_Lado_Corto
            Seccion(i / 2).Cantidad_Lado_Corto_Bottom = Cant_Lado_Corto

            For j = 0 To Seccion(i / 2).Cantidad_Barras_Top - 1
                Dim Cantidad As Integer = Seccion(i / 2).Cantidad_Barras_Top
                If Cantidad <= 4 Then
                    Cant_Lado_Corto = 0
                    Cant_Lado_Largo = 0
                Else
                    Cant_Lado_Largo = (Cantidad - 4 - 2 * Cant_Lado_Corto) / 2
                End If

                Seccion(i / 2).Cantidad_Lado_Largo_Top = Cant_Lado_Largo
                Dim Barra_ As New Tramo_Columna.Detalles_Refuerzo_Longitudinal
                Barra_.Name_Barra = j + 1
                Barra_.Asb = As_Equivalente_Top
                Barra_.Db = Math.Sqrt(4 * Barra_.Asb / Math.PI)
                'Dim Lista_Coordenadas_Barras = Coordenadas_Barras(Seccion(i / 2).B_Plano, Seccion(i / 2).H_Plano, Seccion(i / 2).Cantidad_Barras_Top, Cant_Lado_Corto, Cant_Lado_Largo)
                'Barra_.Coordenada_X = Lista_Coordenadas_Barras(j, 1)
                'Barra_.Coordenada_Y = Lista_Coordenadas_Barras(j, 2)
                Seccion(i / 2).Lista_Detalles_Refuerzo_Top.Add(Barra_)
            Next
            For j = 0 To Seccion(i / 2).Cantidad_Barras_Bottom - 1
                Dim Cantidad As Integer = Seccion(i / 2).Cantidad_Barras_Bottom
                If Cantidad <= 4 Then
                    Cant_Lado_Corto = 0
                    Cant_Lado_Largo = 0
                Else
                    Cant_Lado_Largo = (Cantidad - 4 - 2 * Cant_Lado_Corto) / 2
                End If

                Seccion(i / 2).Cantidad_Lado_Largo_Bottom = Cant_Lado_Largo
                Dim Barra_ As New Tramo_Columna.Detalles_Refuerzo_Longitudinal
                Barra_.Name_Barra = j + 1
                Barra_.Asb = As_Equivalente_Bottom
                Barra_.Db = Math.Sqrt(4 * Barra_.Asb / Math.PI)
                'Dim Lista_Coordenadas_Barras = Coordenadas_Barras(Seccion(i / 2).B_Plano, Seccion(i / 2).H_Plano, Seccion(i / 2).Cantidad_Barras_Bottom, Cant_Lado_Corto, Cant_Lado_Largo)
                'Barra_.Coordenada_X = Lista_Coordenadas_Barras(j, 1)
                'Barra_.Coordenada_Y = Lista_Coordenadas_Barras(j, 2)
                Seccion(i / 2).Lista_Detalles_Refuerzo_Bottom.Add(Barra_)
            Next

            For j = 5 To 12
                If Tabla_Info_Seccion.Rows(i).Cells(j).Value > 0 And j <= 11 Then
                    Seccion(i / 2).Barra_Long_Min = Convert.ToString("#" & j - 3)
                    Exit For
                Else
                    Seccion(i / 2).Barra_Long_Min = Convert.ToString("#10")
                End If
            Next

            Seccion(i / 2).Num_Ramas_Largo = Tabla_Info_Seccion.Rows(i).Cells(13).Value
            Seccion(i / 2).Num_Ramas_Corto = Tabla_Info_Seccion.Rows(i).Cells(14).Value
            Seccion(i / 2).Numero_Barras_Estribo = Tabla_Info_Seccion.Rows(i).Cells(15).Value

            Dim area_ref_var As Single = 0
            Dim area_var_Largo As Single = 0
            Dim area_var_Corto As Single = 0
            If Seccion(i / 2).Numero_Barras_Estribo = "User" Then
                area_ref_var = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(16).Value)
                area_var_Largo = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(16).Value)
                area_var_Corto = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(17).Value)
            Else
                area_ref_var = AreaRefuerzo(Seccion(i / 2).Numero_Barras_Estribo)
                area_var_Largo = area_ref_var
                area_var_Corto = area_ref_var
            End If

            Seccion(i / 2).Separacion_Estribos = Tabla_Info_Seccion.Rows(i).Cells(18).Value
            Seccion(i / 2).Separacion_Estribos_ZNC = Tabla_Info_Seccion.Rows(i).Cells(19).Value

            Seccion(i / 2).Ash_Col_Corto = Seccion(i / 2).Num_Ramas_Corto * area_var_Corto
            Seccion(i / 2).Ash_Col_Largo = Seccion(i / 2).Num_Ramas_Largo * area_var_Largo
        Next

        If Op_SeccionPrincipal.Checked = True Then
            Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Principal = True
        End If

        Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Ref_Modificado = True

        If Proyecto.Elementos.Columnas.Lista_Columnas.FindIndex(Function(p) p.Name_Elemento = Elemento) < Combo_Elementos.Items.Count - 1 Then
            Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(Proyecto.Elementos.Columnas.Lista_Columnas.FindIndex(Function(p) p.Name_Elemento = Elemento) + 1).Name_Elemento
        Else
            MessageBox.Show("Hecho.", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        'Catch ex As Exception
        'Finally
        Op_SeccionPrincipal.Checked = True
        'End Try

    End Sub

    Private Sub Tabla_Info_Seccion_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_Seccion.CellValueChanged
        For i = 0 To Tabla_Info_Seccion.Rows.Count - 1
            For j = 5 To 12
                If Tabla_Info_Seccion.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_Seccion, i, j)
                End If
            Next
        Next
    End Sub

    Sub Color_Celda(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer)
        Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 224, 180)
        Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.Red
    End Sub

    Private Sub DataGridView1_CellPainting(sender As System.Object, e As System.Windows.Forms.DataGridViewCellPaintingEventArgs) Handles Tabla_Info_Seccion.CellPainting
        If Tabla_Info_Seccion.Rows.Count > 1 Then
            If e.RowIndex >= 0 Then
                If Tabla_Info_Seccion.Rows(e.RowIndex).Cells(e.ColumnIndex).Value <> Nothing Then
                    If e.ColumnIndex <= 3 Or e.ColumnIndex > 12 Then
                        e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub Form_03_PagInfoColumnas_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        If Proyecto.Elementos.Columnas.Lista_Columnas.Count > 0 Then

            For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                Combo_Elementos.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Elemento)
            Next
            Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(0).Name_Elemento
        End If

    End Sub

    Private Sub Form_03_PagInfoColumnas_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize

        Button1.Left = (Panel1.Width - Button1.Width) / 2 - 0.6 * Button1.Width
        Button2.Left = (Panel1.Width - Button2.Width) / 2 + 0.6 * Button2.Width

        Label1.Left = (Panel1.Width - Label1.Width) / 2

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim cont As Integer = 0
        For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
            For j = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                If Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(j).As_Col_Top = 0 Then
                    Form_Reportes.Lista_Reporte.Items.Add("- No se ha ingresado refuerzo Top en la sección " & Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label & " en el piso " & Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(j).Piso)
                    cont += 1
                End If
                If Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(j).As_Col_Bottom = 0 Then
                    Form_Reportes.Lista_Reporte.Items.Add("- No se ha ingresado refuerzo Bottom en la sección " & Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label & " en el piso " & Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(j).Piso)
                    cont += 1
                End If
            Next
        Next

        If cont = 0 Then
            For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
                Dim Lista(3, 2) : Lista(1, 1) = 100 : Lista(2, 1) = 100 : Lista(3, 1) = 100
                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_F.Clear()
                Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_F_Piso.Clear()

                If Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Count < Proyecto.Elementos.Columnas.Lista_Columnas.Count Then
                    Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label)
                    Form_02_01_02_ResultadosModelo.Combo_Elementos.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label)
                    Form_02_01_00_RevisionCortante.Combo_Elementos.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label)
                End If

                For j = 0 To Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas.Count - 1
                    Dim Seccion = Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_Tramos_Columnas(j)

                    '--- Verificación a Flexo-Compresión -------
                    Seccion.F_Flexo_Top = Math.Round(Seccion.As_Col_Top / Seccion.As_Req_Top, 2)
                    Seccion.F_Flexo_Bottom = Math.Round(Seccion.As_Col_Bottom / Seccion.As_Req_Bottom, 2)

                    '---- Verificación a Cortante -------
                    Dim Rev_Cortante_L = Funcion_Cortante(Seccion.B_Plano, Seccion.H_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Numero_Barras_Estribo, Seccion.Num_Ramas_Largo, Math.Abs(Seccion.V2), Math.Abs(Seccion.Pu_V2))
                    Dim Rev_Cortante_C = Funcion_Cortante(Seccion.H_Plano, Seccion.B_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Numero_Barras_Estribo, Seccion.Num_Ramas_Corto, Math.Abs(Seccion.V3), Math.Abs(Seccion.Pu_V3))
                    Seccion.Vc_2 = Rev_Cortante_L(1)
                    Seccion.Vs_2 = Rev_Cortante_L(2)
                    Seccion.Vn_2 = Rev_Cortante_L(3)
                    Seccion.Vu_2 = Rev_Cortante_L(4)
                    Seccion.F_Cortante_2 = Rev_Cortante_L(5)
                    Seccion.Vc_3 = Rev_Cortante_C(1)
                    Seccion.Vs_3 = Rev_Cortante_C(2)
                    Seccion.Vn_3 = Rev_Cortante_C(3)
                    Seccion.Vu_3 = Rev_Cortante_C(4)
                    Seccion.F_Cortante_3 = Rev_Cortante_C(5)

                    '------ Verificaciòn al Confinamiento ------
                    Dim Rev_Confinamiento_L = Funcion_Confinamiento(Seccion.B_Plano, Seccion.H_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Barra_Long_Min, Seccion.Numero_Barras_Estribo, "DMO")
                    Dim Rev_Confinamiento_C = Funcion_Confinamiento(Seccion.H_Plano, Seccion.B_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Barra_Long_Min, Seccion.Numero_Barras_Estribo, "DMO")

                    Seccion.Ash_L = Rev_Confinamiento_L(1)
                    Seccion.Ramas_Req_L = Rev_Confinamiento_L(2)
                    Seccion.S0_L = Rev_Confinamiento_L(3)
                    Seccion.L0_L = Rev_Confinamiento_L(4)

                    Seccion.Ash_C = Rev_Confinamiento_C(1)
                    Seccion.Ramas_Req_C = Rev_Confinamiento_C(2)
                    Seccion.S0_C = Rev_Confinamiento_C(3)
                    Seccion.L0_C = Rev_Confinamiento_C(4)

                    Seccion.F_Ash_Largo = Math.Round(Seccion.Ash_Col_Largo / Seccion.Ash_L, 2)
                    Seccion.F_Ash_Corto = Math.Round(Seccion.Ash_Col_Corto / Seccion.Ash_C, 2)


                    If Lista(1, 1) > Seccion.F_Flexo_Top Then
                        Lista(1, 1) = Seccion.F_Flexo_Top
                        Lista(1, 2) = Seccion.Piso
                    End If
                    If Lista(1, 1) > Seccion.F_Flexo_Bottom Then
                        Lista(1, 1) = Seccion.F_Flexo_Bottom
                        Lista(1, 2) = Seccion.Piso
                    End If

                    If Lista(2, 1) > Seccion.F_Cortante_2 Then
                        Lista(2, 1) = Seccion.F_Cortante_2
                        Lista(2, 2) = Seccion.Piso
                    End If

                    If Lista(3, 1) > Seccion.F_Cortante_3 Then
                        Lista(3, 1) = Seccion.F_Cortante_3
                        Lista(3, 2) = Seccion.Piso
                    End If
                Next

                For k = 1 To 3
                    Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_F.Add(Lista(k, 1))
                    Proyecto.Elementos.Columnas.Lista_Columnas(i).Lista_F_Piso.Add(Lista(k, 2))
                Next
            Next

            '--------------- Debo verificar este proceso ya que arroja error --------------
            Dim Columnas = Proyecto.Elementos.Columnas.Lista_Columnas
            For i = 0 To Columnas.Count() - 1
                For j = 0 To Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR.Count - 1
                    Dim Ce As Integer = j

                    Dim Valor_ALR As New Columna.ALR
                    Valor_ALR.Combinacion = Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR(j)
                    Valor_ALR.ALR = Math.Round(Math.Abs(Columnas(i).Lista_Tramos_Columnas(Columnas(i).Lista_Tramos_Columnas.Count - 1).Lista_Combinaciones.Find(Function(p) p.Name = Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR(Ce)).P) / (Columnas(i).Lista_Tramos_Columnas(Columnas(i).Lista_Tramos_Columnas.Count - 1).fc * Columnas(i).Lista_Tramos_Columnas(Columnas(i).Lista_Tramos_Columnas.Count - 1).B_Plano * Columnas(i).Lista_Tramos_Columnas(Columnas(i).Lista_Tramos_Columnas.Count - 1).H_Plano * 1000), 2)

                    Columnas(i).Lista_ALR.Add(Valor_ALR)
                Next
            Next

            Form_02_01_ResultadosColumnas.Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(0).Name_Label
            Form_02_01_ResultadosColumnas.Show()
            MessageBox.Show("Análisis Finalizado con Éxito.", "Ejecución de Análisis", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            Form_Reportes.Show()
        End If

    End Sub

    'Private Sub SecciónToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SecciónToolStripMenuItem.Click
    '    If Proyecto.Elementos.Columnas.Lista_Columnas.Count > 0 Then
    '        Dim Columna = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text)
    '        For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
    '            If SRectangular.Combo_Seccion.Items.Count <= Proyecto.Elementos.Columnas.Lista_Columnas.Count Then
    '                SRectangular.Combo_Seccion.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label)
    '            End If
    '        Next
    '        SRectangular.Combo_Seccion.Text = SRectangular.Combo_Seccion.Items(0)
    '        For i = 0 To Columna.Lista_Tramos_Columnas.Count() - 1
    '            If SRectangular.Combo_Tramos.Items.Count <= Columna.Lista_Tramos_Columnas.Count Then
    '                SRectangular.Combo_Tramos.Items.Add(Columna.Lista_Tramos_Columnas(i).Piso)
    '            End If
    '        Next
    '        SRectangular.Combo_Tramos.Text = SRectangular.Combo_Tramos.Items(0)
    '        If SRectangular.Combo_Estacion.Items.Count < 2 Then
    '            SRectangular.Combo_Estacion.Items.Add("Top")
    '            SRectangular.Combo_Estacion.Items.Add("Bottom")
    '        End If
    '        SRectangular.Combo_Estacion.Text = SRectangular.Combo_Estacion.Items(0)

    '        Dim PictureBox5 = SRectangular.PictureBox1

    '        PictureBox5.Location = New Point(25, 70)
    '        PictureBox5.Size = New Size(SRectangular.Panel1.Width - 50, SRectangular.Panel1.Height - 100)
    '        PictureBox5.BackColor = Color.White
    '        PictureBox5.Anchor = AnchorStyles.Left And AnchorStyles.Top And AnchorStyles.Right And AnchorStyles.Bottom
    '        SRectangular.LbCuantia.BackColor = Color.White
    '        SRectangular.Panel1.Controls.Add(PictureBox5)
    '        AddHandler PictureBox5.Paint, AddressOf SRectangular.PictureBox5_Paint
    '        PictureBox5.Refresh()
    '    End If
    '    SRectangular.Show()
    'End Sub

    Private Sub Op_SeccionSimilar_CheckedChanged(sender As Object, e As EventArgs) Handles Op_SeccionSimilar.CheckedChanged

        Dim Secciones_Principales = Proyecto.Elementos.Columnas.Lista_Columnas.FindAll(Function(p) p.Secciones_Principal = True)
        C_Lista_Secciones_Principales.Items.Clear()

        If Op_SeccionPrincipal.Checked = False Then
            If Secciones_Principales.Count < 1 Then
                MessageBox.Show("No se tiene registro de ninguna sección", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Op_SeccionPrincipal.Checked = True
            Else
                For i = 0 To Secciones_Principales.Count - 1
                    C_Lista_Secciones_Principales.Items.Add(Secciones_Principales(i).Name_Label)
                Next
                Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Similar = True
                Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Principal = False
                C_Lista_Secciones_Principales.Enabled = True
            End If
        End If
    End Sub

    Private Sub C_Lista_Secciones_Principales_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_Lista_Secciones_Principales.SelectedIndexChanged
        Tabla_Info_Seccion.Rows.Clear()

        Dim Elemento As String = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = C_Lista_Secciones_Principales.Text).Name_Elemento
        Dim Seccion = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = C_Lista_Secciones_Principales.Text).Lista_Tramos_Columnas

        For i = 0 To (Seccion.Count - 1) * 2
            Tabla_Info_Seccion.Rows.Add()
        Next

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
            Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).B_Plano
            Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).H_Plano
            Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
            Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

            Tabla_Info_Seccion.Rows(i).Cells(5).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_2
            Tabla_Info_Seccion.Rows(i).Cells(6).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_3
            Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_4
            Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_5
            Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_6
            Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_7
            Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_8
            Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_10

            Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2
            Tabla_Info_Seccion.Rows(i + 1).Cells(6).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3
            Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4
            Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5
            Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6
            Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7
            Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8
            Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10

            Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Num_Ramas_Largo
            Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Num_Ramas_Corto
            Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Numero_Barras_Estribo
            Tabla_Info_Seccion.Rows(i).Cells(16).Value = Seccion(i / 2).Separacion_Estribos
        Next

    End Sub
    Protected Overrides Function ProcessCmdKey(
       ByRef msg As System.Windows.Forms.Message,
       keyData As System.Windows.Forms.Keys) As Boolean

        ' Si el control DataGridView no tiene el foco,
        ' abandonamos el procedimiento.
        '
        If (Not (Tabla_Info_Seccion.Focused)) Then _
                Return MyBase.ProcessCmdKey(msg, keyData)

        ' Comprobamos si se ha pulsado la combinación
        ' de teclas Ctrl + V.
        '
        If (Not (keyData = (Keys.V Or Keys.Control))) Then _
                    Return MyBase.ProcessCmdKey(msg, keyData)

        ' Comprobamos si el contenido del portapapeles es texto.
        '
        Dim isTexto As Boolean = Clipboard.GetDataObject.GetDataPresent(DataFormats.Text)

        If (isTexto) Then
            ' Celda actual del control DataGridView
            '
            Dim Celdas_Seleccionadas As DataGridViewSelectedCellCollection = Tabla_Info_Seccion.SelectedCells
            For Each Celda As DataGridViewCell In Celdas_Seleccionadas
                Celda.Value = My.Computer.Clipboard.GetText()
            Next

            'Dim currentCell As DataGridViewCell = Tabla_Info_Seccion.CurrentCell
            'currentCell.Value = My.Computer.Clipboard.GetText()

        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

    Private Sub Tabla_Info_Seccion_CellContentClick_1(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_Seccion.CellContentClick

    End Sub
End Class