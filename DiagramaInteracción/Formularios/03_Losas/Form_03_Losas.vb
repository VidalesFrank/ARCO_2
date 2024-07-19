Imports System.IO
Imports ARCO.Funciones_03_Losa
Imports ARCO.Funciones_00_Varias

Public Class Form_03_Losas
    Public Shared Proyecto As New Proyecto_Losas
    Private Sub C_D_L_CheckedChanged(sender As Object, e As EventArgs) Handles C_D_L.CheckedChanged
        If C_C_L.Checked = True Then
            C_C_L.Checked = False
        End If
    End Sub

    Private Sub C_C_L_CheckedChanged(sender As Object, e As EventArgs) Handles C_C_L.CheckedChanged
        If C_D_L.Checked = True Then
            C_D_L.Checked = False
        End If
    End Sub

    Private Sub C_D_T_CheckedChanged(sender As Object, e As EventArgs) Handles C_D_T.CheckedChanged
        If C_C_T.Checked = True Then
            C_C_T.Checked = False
        End If
    End Sub

    Private Sub C_C_T_CheckedChanged(sender As Object, e As EventArgs) Handles C_C_T.CheckedChanged
        If C_D_T.Checked = True Then
            C_D_T.Checked = False
        End If
    End Sub

    Private Sub C_D_R_CheckedChanged(sender As Object, e As EventArgs) Handles C_D_R.CheckedChanged
        If C_C_R.Checked = True Then
            C_C_R.Checked = False
        End If
    End Sub

    Private Sub C_C_R_CheckedChanged(sender As Object, e As EventArgs) Handles C_C_R.CheckedChanged
        If C_D_R.Checked = True Then
            C_D_R.Checked = False
        End If
    End Sub

    Private Sub C_D_B_CheckedChanged(sender As Object, e As EventArgs) Handles C_D_B.CheckedChanged
        If C_C_B.Checked = True Then
            C_C_B.Checked = False
        End If
    End Sub

    Private Sub C_C_B_CheckedChanged(sender As Object, e As EventArgs) Handles C_C_B.CheckedChanged
        If C_D_B.Checked = True Then
            C_D_B.Checked = False
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        'If T_e_Loseta.Text = String.Empty Or T_H_Nervio.Text = String.Empty Or T_tw_Nervio.Text = String.Empty Or
        '    T_S_1.Text = String.Empty Or T_S_2.Text = String.Empty Or T_L1.Text = String.Empty Or T_L2.Text = String.Empty Or
        '    T_C_Impuesta.Text = String.Empty Or T_C_Viva.Text = String.Empty Or T_fy.Text = String.Empty Or T_fc.Text = String.Empty Then

        '    MessageBox.Show("Se tiene celdas vacías.", "Punto de control", MessageBoxButtons.OKCancel, MessageBoxIcon.Error)
        'End If

        Try
            Tabla_Coeficientes.Rows.Clear()
            Tabla_Resultados.Rows.Clear()

            Proyecto.e_losa = Convert.ToSingle(T_e_Loseta.Text)
            Proyecto.H_Nervio = Convert.ToSingle(T_H_Nervio.Text)
            Proyecto.tw_Nervio = Convert.ToSingle(T_tw_Nervio.Text)
            Proyecto.Separacion_X = Convert.ToSingle(T_S_1.Text)
            Proyecto.Separacion_Y = Convert.ToSingle(T_S_2.Text)
            Proyecto.Longitud_X = Convert.ToSingle(T_L1.Text)
            Proyecto.Longitud_Y = Convert.ToSingle(T_L2.Text)
            Proyecto.C_Impuesta = Convert.ToSingle(T_C_Impuesta.Text)
            Proyecto.C_Viva = Convert.ToSingle(T_C_Viva.Text)
            Proyecto.fc = Convert.ToSingle(T_fc.Text)
            Proyecto.fy = Convert.ToSingle(T_fy.Text)

            Proyecto.Op_Continua_L = C_C_L.Checked
            Proyecto.Op_Continua_T = C_C_T.Checked
            Proyecto.Op_Continua_R = C_C_R.Checked
            Proyecto.Op_Continua_B = C_C_B.Checked

            Me.Cursor = Cursors.WaitCursor

            Dim Caso As Integer
            Dim Lna As Single = Math.Min(Convert.ToSingle(T_L1.Text), Convert.ToSingle(T_L2.Text))
            Dim Lnb As Single = Math.Max(Convert.ToSingle(T_L1.Text), Convert.ToSingle(T_L2.Text))
            Dim Opcion As Integer = 1

            L_Horizontal.Text = "Dirección 'B'"
            L_Vertical.Text = "Dirección 'A'"

            If Lna = Convert.ToSingle(T_L1.Text) Then
                Opcion = 2
                L_Horizontal.Text = "Dirección 'A'"
                L_Vertical.Text = "Dirección 'B'"
            End If

            If C_D_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 1
            ElseIf C_C_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 2
            ElseIf Opcion = 1 And C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 3
            ElseIf Opcion = 2 And C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 3
            ElseIf C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 4
            ElseIf C_C_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 4
            ElseIf C_D_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 4
            ElseIf C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 4
            ElseIf Opcion = 1 And C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 5
            ElseIf Opcion = 2 And C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 5
            ElseIf Opcion = 1 And C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 6
            ElseIf Opcion = 1 And C_D_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 6
            ElseIf Opcion = 2 And C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 6
            ElseIf Opcion = 2 And C_D_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 6
            ElseIf Opcion = 1 And C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 7
            ElseIf Opcion = 1 And C_D_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 7
            ElseIf Opcion = 2 And C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 7
            ElseIf Opcion = 2 And C_D_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 7
            ElseIf Opcion = 1 And C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 8
            ElseIf Opcion = 1 And C_C_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 8
            ElseIf Opcion = 2 And C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 8
            ElseIf Opcion = 2 And C_C_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 8
            ElseIf Opcion = 1 And C_D_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 9
            ElseIf Opcion = 1 And C_C_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_D_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 9
            ElseIf Opcion = 2 And C_C_L.CheckState = CheckState.Checked And C_D_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_C_B.CheckState = CheckState.Checked Then
                Caso = 9
            ElseIf Opcion = 2 And C_C_L.CheckState = CheckState.Checked And C_C_T.CheckState = CheckState.Checked And C_C_R.CheckState = CheckState.Checked And C_D_B.CheckState = CheckState.Checked Then
                Caso = 9
            End If

            Dim Coeficientes_ = Coeficientes(Lna, Lnb, Caso)
            Dim Lista_Titulos = New String(7) {"Ca-", "Cb-", "Ca D", "Cb D", "Ca L", "Cb L", "qa", "qb"}

            Proyecto.Coeficientes.Add(Caso)
            Proyecto.Coeficientes.Add(Lna)
            Proyecto.Coeficientes.Add(Lnb)
            Proyecto.Coeficientes.Add(Math.Round(Lna / Lnb, 3))

            Proyecto.Titulos_Coeficientes.Add("Caso")
            Proyecto.Titulos_Coeficientes.Add("Lna")
            Proyecto.Titulos_Coeficientes.Add("Lnb")
            Proyecto.Titulos_Coeficientes.Add("m")

            For i = 0 To 7
                Proyecto.Titulos_Coeficientes.Add(Lista_Titulos(i))
                Proyecto.Coeficientes.Add(Coeficientes_(i))
            Next

            Dim e_loseta As Single = Convert.ToSingle(T_e_Loseta.Text)
            Dim H_Nervio As Single = Convert.ToSingle(T_H_Nervio.Text)
            Dim tw_Nervio As Single = Convert.ToSingle(T_tw_Nervio.Text)
            Dim S_L As Single = Convert.ToSingle(T_S_1.Text)
            Dim S_T As Single = Convert.ToSingle(T_S_2.Text)

            Dim Carga_Impuesta As Single = Convert.ToSingle(T_C_Impuesta.Text)
            Dim CV As Single = Convert.ToSingle(T_C_Viva.Text)

            Dim Vol_Loseta As Single = e_loseta * (S_L + tw_Nervio) * (S_T + tw_Nervio)
            Dim Vol_Nervio As Single = tw_Nervio * (S_L + S_T + tw_Nervio) * (H_Nervio - e_loseta)

            Dim Peso_Losa As Single = 24 * (Vol_Loseta + Vol_Nervio) / ((S_L + tw_Nervio) * (S_T + tw_Nervio))

            Dim CM As Single = Carga_Impuesta + Peso_Losa
            Dim Cu As Single = 1.2 * CM + 1.6 * CV

            Proyecto.CM = CM
            Proyecto.CV = CV
            Proyecto.CU = Cu

            Dim Sa As Single = Convert.ToSingle(T_S_2.Text)
            Dim Sb As Single = Convert.ToSingle(T_S_1.Text)
            If Lna = Convert.ToSingle(T_L1.Text) Then
                Sa = Convert.ToSingle(T_S_1.Text)
                Sb = Convert.ToSingle(T_S_2.Text)
            End If

            '--------------- Momentos en Franja Central -------------
            Dim Ma_N As Single = Coeficientes_(0) * Cu * Lna ^ 2 * (Sa + tw_Nervio)
            Dim Mb_N As Single = Coeficientes_(1) * Cu * Lnb ^ 2 * (Sb + tw_Nervio)
            Dim Ma_P As Single = (Coeficientes_(2) * 1.2 * CM + Coeficientes_(4) * 1.6 * CV) * Lna ^ 2 * (Sa + tw_Nervio)
            Dim Mb_P As Single = (Coeficientes_(3) * 1.2 * CM + Coeficientes_(5) * 1.6 * CV) * Lnb ^ 2 * (Sb + tw_Nervio)
            Dim Va As Single = Coeficientes_(6) * Cu * Lna / 2 * (Sa + tw_Nervio)
            Dim Vb As Single = Coeficientes_(7) * Cu * Lnb / 2 * (Sb + tw_Nervio)

            Dim Franja_Central = New Single(5) {Ma_N, Mb_N, Ma_P, Mb_P, Va, Vb}
            Dim Franja_Borde = New Single(5) {Ma_N / 3, Mb_N / 3, Ma_P / 3, Mb_P / 3, Va / 3, Vb / 3}
            Dim Titulos = New String(5) {"Ma- (kN.m)", "Mb- (kN.m)", "Ma+ (kN.m)", "Mb+ (kN.m)", "Va (kN)", "Vb (kN)"}

            For i = 0 To 5
                Proyecto.Titulos_Demandas.Add(Titulos(i))
                Proyecto.Momentos_Franja_Central.Add(Franja_Central(i))
                Proyecto.Momentos_Franja_Borde.Add(Franja_Borde(i))
            Next

            Rellenar()
            TabControl1.SelectedIndex = 1
            MessageBox.Show("Análisis Finalizado con Éxito.", "Ejecución de Análisis", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show("Se tienen celdas vacías.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Me.Cursor = Cursors.Arrow
    End Sub

    Public Sub Llenar_Celdas()
        T_e_Loseta.Text = Proyecto.e_losa
        T_H_Nervio.Text = Proyecto.H_Nervio
        T_tw_Nervio.Text = Proyecto.tw_Nervio
        T_S_1.Text = Proyecto.Separacion_X
        T_S_2.Text = Proyecto.Separacion_Y
        T_L1.Text = Proyecto.Longitud_X
        T_L2.Text = Proyecto.Longitud_Y
        T_C_Impuesta.Text = Proyecto.C_Impuesta
        T_C_Viva.Text = Proyecto.C_Viva
        T_fc.Text = Proyecto.fc
        T_fy.Text = Proyecto.fy

        Dim Lna As Single = Math.Min(Convert.ToSingle(T_L1.Text), Convert.ToSingle(T_L2.Text))
        Dim Lnb As Single = Math.Max(Convert.ToSingle(T_L1.Text), Convert.ToSingle(T_L2.Text))

        L_Horizontal.Text = "Dirección 'B'"
        L_Vertical.Text = "Dirección 'A'"

        If Lna = Convert.ToSingle(T_L1.Text) Then
            L_Horizontal.Text = "Dirección 'A'"
            L_Vertical.Text = "Dirección 'B'"
        End If

        ChequearContinuidad(Proyecto.Op_Continua_L, C_C_L, C_D_L)
        ChequearContinuidad(Proyecto.Op_Continua_T, C_C_T, C_D_T)
        ChequearContinuidad(Proyecto.Op_Continua_R, C_C_R, C_D_R)
        ChequearContinuidad(Proyecto.Op_Continua_B, C_C_B, C_D_B)

    End Sub

    Public Sub ChequearContinuidad(ByVal Opcion As Boolean, ByVal Cheq1 As CheckBox, ByVal Cheq2 As CheckBox)
        If Opcion = True Then
            Cheq1.Checked = True
            Cheq2.Checked = False
        Else
            Cheq1.Checked = False
            Cheq2.Checked = True
        End If
    End Sub

    Public Sub Rellenar()

        For i = 0 To Proyecto.Coeficientes.Count - 2
            Tabla_Coeficientes.Rows.Add(Proyecto.Titulos_Coeficientes(i), Math.Round(Proyecto.Coeficientes(i), 3))
        Next
        Tabla_Coeficientes.Rows(Proyecto.Coeficientes.Count - 1).Cells(0).Value = Proyecto.Titulos_Coeficientes(Proyecto.Coeficientes.Count - 1)
        Tabla_Coeficientes.Rows(Proyecto.Coeficientes.Count - 1).Cells(1).Value = Math.Round(Proyecto.Coeficientes(Proyecto.Coeficientes.Count - 1), 3)

        For i = 0 To 4
            Tabla_Resultados.Rows.Add(Proyecto.Titulos_Demandas(i), Math.Round(Proyecto.Momentos_Franja_Central(i), 3), Math.Round(Proyecto.Momentos_Franja_Borde(i), 3))
        Next
        Tabla_Resultados.Rows(5).Cells(0).Value = Proyecto.Titulos_Demandas(5)
        Tabla_Resultados.Rows(5).Cells(1).Value = Math.Round(Proyecto.Momentos_Franja_Central(5), 3)
        Tabla_Resultados.Rows(5).Cells(2).Value = Math.Round(Proyecto.Momentos_Franja_Borde(5), 3)

        '--------------- Diseño Dirección "A" -------------------

        Dim As_C_LA As Single = DiseñoFlexion(Proyecto.fy, Proyecto.fc, Proyecto.tw_Nervio * 1000, (Proyecto.H_Nervio + Proyecto.e_losa) * 1000, 20, Proyecto.Momentos_Franja_Central(0), 0.002, 0.002)
        Dim As_B_LA As Single = DiseñoFlexion(Proyecto.fy, Proyecto.fc, Proyecto.tw_Nervio * 1000, (Proyecto.H_Nervio + Proyecto.e_losa) * 1000, 20, Proyecto.Momentos_Franja_Central(2), 0.002, 0.002)
        Dim As_C_LB As Single = DiseñoFlexion(Proyecto.fy, Proyecto.fc, Proyecto.tw_Nervio * 1000, (Proyecto.H_Nervio + Proyecto.e_losa) * 1000, 20, Proyecto.Momentos_Franja_Central(1), 0.002, 0.002)
        Dim As_B_LB As Single = DiseñoFlexion(Proyecto.fy, Proyecto.fc, Proyecto.tw_Nervio * 1000, (Proyecto.H_Nervio + Proyecto.e_losa) * 1000, 20, Proyecto.Momentos_Franja_Central(3), 0.002, 0.002)

        T_As_C_LA.Text = Math.Round(As_C_LA, 0)
        T_As_B_LA.Text = Math.Round(As_B_LA, 0)
        T_As_C_LB.Text = Math.Round(As_C_LB, 0)
        T_As_B_LB.Text = Math.Round(As_B_LB, 0)

        T_Cuantia_C_LA.Text = Math.Round(As_C_LA / (Proyecto.tw_Nervio * 1000 * (Proyecto.H_Nervio + Proyecto.e_losa - 0.02) * 1000), 5)
        T_Cuantia_B_LA.Text = Math.Round(As_B_LA / (Proyecto.tw_Nervio * 1000 * (Proyecto.H_Nervio + Proyecto.e_losa - 0.02) * 1000), 5)
        T_Cuantia_C_LB.Text = Math.Round(As_C_LB / (Proyecto.tw_Nervio * 1000 * (Proyecto.H_Nervio + Proyecto.e_losa - 0.02) * 1000), 5)
        T_Cuantia_B_LB.Text = Math.Round(As_B_LB / (Proyecto.tw_Nervio * 1000 * (Proyecto.H_Nervio + Proyecto.e_losa - 0.02) * 1000), 5)

        Op_Barra_C_LA.SelectedIndex = 2
        Op_Barra_B_LA.SelectedIndex = 2
        Op_Barra_C_LB.SelectedIndex = 2
        Op_Barra_B_LB.SelectedIndex = 2

    End Sub

    Public Sub AyudaGlobo(ByVal Globo As ToolTip, ByVal Boton As TextBox, ByVal Mensaje As String)
        Globo.RemoveAll()
        Globo.SetToolTip(Boton, Mensaje)
        Globo.InitialDelay = 100
        Globo.IsBalloon = False
    End Sub

    Private Sub TextBox3_MouseEnter(sender As Object, e As EventArgs) Handles T_H_Nervio.MouseEnter
        AyudaGlobo(ToolTip1, T_H_Nervio, "Corresponde a la altura del nervio sin tener el cuenta el espesor de la loseta.")
    End Sub

    Private Sub Op_Barra_C_LA_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Op_Barra_C_LA.SelectedIndexChanged
        Try
            T_Propuesta_C_LA.Text = Math.Round(Convert.ToSingle(T_As_C_LA.Text) / AreaRefuerzo(Op_Barra_C_LA.Text), 0)
            If (Math.Round(Convert.ToSingle(T_As_C_LA.Text) / AreaRefuerzo(Op_Barra_C_LA.Text), 0) * AreaRefuerzo(Op_Barra_C_LA.Text)) < 0.9 * Convert.ToSingle(T_As_C_LA.Text) Then
                T_Propuesta_C_LA.Text = Convert.ToSingle(T_Propuesta_C_LA.Text) + 1
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Op_Barra_B_LA_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Op_Barra_B_LA.SelectedIndexChanged
        Try
            T_Propuesta_B_LA.Text = Math.Round(Convert.ToSingle(T_As_B_LA.Text) / AreaRefuerzo(Op_Barra_B_LA.Text), 0)
            If (Math.Round(Convert.ToSingle(T_As_B_LA.Text) / AreaRefuerzo(Op_Barra_B_LA.Text), 0) * AreaRefuerzo(Op_Barra_B_LA.Text)) < 0.9 * Convert.ToSingle(T_As_B_LA.Text) Then
                T_Propuesta_B_LA.Text = Convert.ToSingle(T_Propuesta_B_LA.Text) + 1
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Op_Barra_C_LB_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Op_Barra_C_LB.SelectedIndexChanged
        Try
            T_Propuesta_C_LB.Text = Math.Round(Convert.ToSingle(T_As_C_LB.Text) / AreaRefuerzo(Op_Barra_C_LB.Text), 0)
            If (Math.Round(Convert.ToSingle(T_As_C_LB.Text) / AreaRefuerzo(Op_Barra_C_LB.Text), 0) * AreaRefuerzo(Op_Barra_C_LB.Text)) < 0.9 * Convert.ToSingle(T_As_C_LB.Text) Then
                T_Propuesta_C_LB.Text = Convert.ToSingle(T_Propuesta_C_LB.Text) + 1
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Op_Barra_B_LB_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Op_Barra_B_LB.SelectedIndexChanged
        Try
            T_Propuesta_B_LB.Text = Math.Round(Convert.ToSingle(T_As_B_LB.Text) / AreaRefuerzo(Op_Barra_B_LB.Text), 0)
            If (Math.Round(Convert.ToSingle(T_As_B_LB.Text) / AreaRefuerzo(Op_Barra_B_LB.Text), 0) * AreaRefuerzo(Op_Barra_B_LB.Text)) < 0.9 * Convert.ToSingle(T_As_B_LB.Text) Then
                T_Propuesta_B_LB.Text = Convert.ToSingle(T_Propuesta_B_LB.Text) + 1
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub GuardarComoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAs_Losas.Click
        GuardarProyecto(Proyecto, "RevisiónLosaMétodoCoeficientes")
    End Sub

    'Private Sub SaveAs(ByVal Objeto As Object)
    '    Try
    '        Dim SaveAs As New SaveFileDialog
    '        SaveAs.Filter = "Archivo|*.esm"
    '        SaveAs.Title = "Guardar Archivo"
    '        SaveAs.FileName = Convert.ToString("RevisiónLosaMétodoCoeficientes")
    '        SaveAs.ShowDialog()
    '        If SaveAs.FileName <> String.Empty Then
    '            Proyecto.Ruta = Path.GetFullPath(SaveAs.FileName)
    '            Funciones_Programa.Serializar(SaveAs.FileName, Objeto)
    '        End If
    '    Catch ex As Exception

    '    End Try

    'End Sub

    Private Sub GuardarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Save_Losas.Click
        Try
            If Proyecto.Ruta = String.Empty Then
                GuardarProyecto(Proyecto, "RevisiónLosaMétodoCoeficientes")
            Else
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub AbrirToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Open_Losas.Click
        Open()
    End Sub
    Public Sub Open()
        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto_Losas)(Open.FileName)
            Llenar_Celdas()
            Rellenar()
        End If

    End Sub

End Class