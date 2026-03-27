Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_04_Escalera
Public Class Form_04_Escaleras
    Public Shared Proyecto As New Proyecto_Escaleras

    Private Sub T_LDescanso_TextChanged(sender As Object, e As EventArgs) Handles T_LDescanso.TextChanged
        Try
            If T_LPeldaños.Text <> String.Empty And T_LDescanso.Text <> String.Empty Then
                T_L.Text = Convert.ToSingle(T_LPeldaños.Text) + Convert.ToSingle(T_LDescanso.Text)

                T_Hminima.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
                T_H.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub T_LPeldaños_TextChanged(sender As Object, e As EventArgs) Handles T_LPeldaños.TextChanged
        Try

            If T_LPeldaños.Text <> String.Empty And T_LDescanso.Text <> String.Empty Then
                T_L.Text = Convert.ToSingle(T_LPeldaños.Text) + Convert.ToSingle(T_LDescanso.Text)

                T_Hminima.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
                T_H.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim fc As Single = Convert.ToSingle(T_fc.Text)
        Dim fy As Single = Convert.ToSingle(T_fy.Text)
        Dim C_Impuesta As Single = Convert.ToSingle(T_C_Impuesta.Text)
        Dim C_Viva As Single = Convert.ToSingle(T_C_Viva.Text)
        Dim Peso_ConcretoReforzado As Single = Convert.ToSingle(T_PesoConcretoReforzado.Text)
        Dim Peso_Concreto As Single = Convert.ToSingle(T_PesoConcreto.Text)

        '---------------- Datos Escalera --------------
        Dim Huella As Single = Convert.ToSingle(T_Huella.Text)
        Dim Contrahuella As Single = Convert.ToSingle(T_Contrahuella.Text)
        Dim N_Peldanos As Integer = Convert.ToInt32(T_NPeldaños.Text)
        Dim L_Peldaños As Single = Convert.ToSingle(T_LPeldaños.Text)
        Dim L_Descanso As Single = Convert.ToSingle(T_LDescanso.Text)
        Dim L_Total As Single = Convert.ToSingle(T_L.Text)
        Dim A_Escalera As Single = Convert.ToSingle(T_AEscalera.Text)
        Dim A_Estudio As Single = Convert.ToSingle(T_AEstudio.Text)
        Dim Recubrimiento As Single = Convert.ToSingle(T_Recubrimiento.Text)
        Dim h As Single = Convert.ToSingle(T_H.Text)

        '------------- Peso de la Escalera --------------
        Dim L_Inclinada As Single = Math.Sqrt(L_Peldaños ^ 2 + (N_Peldanos * Contrahuella) ^ 2)
        Dim Peso_Losa As Single = h * Peso_ConcretoReforzado * A_Estudio
        Dim Peso_Peldanos As Single = (Huella * Contrahuella / 2) * N_Peldanos * Peso_Concreto * A_Estudio
        Dim Peso_Impuesta As Single = C_Impuesta * A_Estudio
        Dim Peso_Viva As Single = C_Viva * A_Estudio
        Dim Peso_ImpuestaPeldanos As Single = C_Impuesta * A_Estudio * (Huella + Contrahuella) / Huella

        Dim Wpp_Inclinada As Single = (Peso_Losa / (Math.Cos(Math.Atan(Contrahuella / Huella)))) + Peso_ImpuestaPeldanos

        Dim Wu_Inclinada As Single = 1.2 * Wpp_Inclinada + 1.6 * Peso_Viva
        Dim Wu_Descanso As Single = 1.2 * Peso_Losa + 1.2 * Peso_Impuesta + 1.6 * Peso_Viva

        Proyecto.fc = fc
        Proyecto.fy = fy
        Proyecto.C_Imp = C_Impuesta
        Proyecto.C_Viv = C_Viva
        Proyecto.P_ConR = Peso_ConcretoReforzado
        Proyecto.P_Con = Peso_Concreto

        Proyecto.Huella = Huella
        Proyecto.Contrahuella = Contrahuella
        Proyecto.N_Peldanos = N_Peldanos
        Proyecto.L_Peldanos = L_Peldaños
        Proyecto.L_Descanso = L_Descanso
        Proyecto.L_Total = L_Total
        Proyecto.A_Escalera = A_Escalera
        Proyecto.A_Estudio = A_Estudio
        Proyecto.Recubrimiento = Recubrimiento
        Proyecto.h = h

        Proyecto.Wu_Inclinada = Wu_Inclinada
        Proyecto.Wu_Descanso = Wu_Descanso

        Proyecto.Abscisas.Clear()
        Proyecto.Momentos.Clear()
        Proyecto.Cortantes.Clear()

        Dim N_Puntos As Integer = Convert.ToInt32(T_NPuntos.Text) + 1

        For i = 0 To Proyecto.L_Total Step Proyecto.L_Total / N_Puntos
            Proyecto.Abscisas.Add(i)
            Proyecto.Momentos.Add(Momento_Escalera(i, Proyecto.L_Peldanos, Proyecto.L_Descanso, Proyecto.Wu_Inclinada, Proyecto.Wu_Descanso))
            Proyecto.Cortantes.Add(Cortante_Escalera(i, Proyecto.L_Peldanos, Proyecto.L_Descanso, Proyecto.Wu_Inclinada, Proyecto.Wu_Descanso))
        Next

        T_Vmax.Text = Math.Round(Proyecto.Cortantes.Max, 2)
        T_Mmax.Text = Math.Round(Proyecto.Momentos.Max, 2)

        '--------------- Retraccion y temperatura -----------
        Proyecto.Cuantia_Temperatura = Convert.ToSingle(T_CuantiaTemperatura.Text)
        Proyecto.Acero_Temperatura = h * A_Estudio * Proyecto.Cuantia_Temperatura * 1000 ^ 2
        C_BarraTemperatura.SelectedIndex = 1

        '------------------ Diseño Flexion --------------------
        Proyecto.Mu = Math.Round(Proyecto.Momentos.Max, 2)
        Proyecto.Acero_Flexion = DiseñoFlexion(Proyecto.fy, Proyecto.fc, Proyecto.A_Estudio * 1000, Proyecto.h * 1000, Proyecto.Recubrimiento * 1000, Proyecto.Mu, 1.4 / Proyecto.fy, 0.0033)
        Proyecto.Cuantia_Flexion = Math.Round(Proyecto.Acero_Flexion / (Proyecto.A_Estudio * (Proyecto.h - Proyecto.Recubrimiento) * 1000000), 5)
        C_BarraFlexion.SelectedIndex = 2

        '-------------- Diseño a cortante--------------------
        Proyecto.Vu = Math.Max(Proyecto.Cortantes.Max, Math.Abs(Proyecto.Cortantes.Min))
        Proyecto.Vc = 0.75 * 0.17 * Math.Sqrt(fc) * 1000 * (h - Recubrimiento) * A_Estudio

        Rellenar()

    End Sub
    Private Sub C_BarraTemperatura_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_BarraTemperatura.SelectedIndexChanged
        Try
            If C_BarraTemperatura.Text <> String.Empty Then
                Dim Acero_BarraTemperatura As Single = AreaRefuerzo(C_BarraTemperatura.Text)
                Proyecto.Barra_Temperatura = C_BarraTemperatura.Items.IndexOf(C_BarraTemperatura.Text)
                Proyecto.S_Temperatura = Math.Round(Math.Min(0.45, Acero_BarraTemperatura / Proyecto.Acero_Temperatura), 2)
                T_SRequeridaTemperatura.Text = Proyecto.S_Temperatura
            End If
        Catch ex As Exception

        End Try
    End Sub
    Private Sub C_BarraFlexion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_BarraFlexion.SelectedIndexChanged
        Try
            If C_BarraFlexion.Text <> String.Empty Then
                Dim Acero_BarraFlexion As Single = AreaRefuerzo(C_BarraFlexion.Text)
                Proyecto.Barra_Flexion = C_BarraFlexion.Items.IndexOf(C_BarraFlexion.Text)
                Proyecto.S_Flexion = Math.Round(Math.Min(0.45, Acero_BarraFlexion / Proyecto.Acero_Flexion), 2)
                T_SRequeridaFlexion.Text = Proyecto.S_Flexion
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub T_SColocadaTemperatura_TextChanged(sender As Object, e As EventArgs) Handles T_SColocadaTemperatura.TextChanged
        Try
            Dim S_Colocada As Single = Convert.ToSingle(T_SColocadaTemperatura.Text)


            If S_Colocada > 0 Then
                Proyecto.Cuantia_Temperaruta_Colocada = (Proyecto.A_Estudio / S_Colocada) * AreaRefuerzo(C_BarraTemperatura.Text) / (Proyecto.A_Estudio * Proyecto.h * 1000000)

                Proyecto.S_Temperatura_Colocada = S_Colocada

                If Proyecto.Cuantia_Temperaruta_Colocada >= 0.9 * Proyecto.Cuantia_Temperatura Then
                    Casilla_Cumple(T_VerificacionTemperatura)
                    T_VerificacionTemperatura.Text = "Cumple"
                Else
                    Casilla_Nocumple(T_VerificacionTemperatura)
                    T_VerificacionTemperatura.Text = "No cumple"
                End If



            End If
        Catch ex As Exception

        End Try

    End Sub

    Private Sub T_SColocadaFlexion_TextChanged(sender As Object, e As EventArgs) Handles T_SColocadaFlexion.TextChanged
        Try
            Dim S_Colocada As Single = Convert.ToSingle(T_SColocadaFlexion.Text)

            If S_Colocada > 0 Then
                Proyecto.Acero_Flexion_Colocado = (Proyecto.A_Estudio / S_Colocada) * AreaRefuerzo(C_BarraFlexion.Text)
                Proyecto.S_Flexion_Colocada = S_Colocada


                If Proyecto.Acero_Flexion_Colocado >= 0.9 * Proyecto.Acero_Flexion Then
                    Casilla_Cumple(T_VerificacionFlexion)
                    T_VerificacionFlexion.Text = "Cumple"
                Else
                    Casilla_Nocumple(T_VerificacionFlexion)
                    T_VerificacionFlexion.Text = "No cumple"
                End If

            End If
        Catch ex As Exception

        End Try
    End Sub

    Public Function Funcion_Multiplo(ByVal Valor As Single, ByVal Multiplo As Single) As Single
        Dim Tol As Single = 1
        Dim H As Single = 0
        For i = Multiplo To 10 * Multiplo Step Multiplo
            If Math.Abs(i - Valor) < Tol Then
                Tol = Math.Abs(i - Valor)
                H = i
            End If
        Next
        Funcion_Multiplo = Math.Round(H, 2)
    End Function

    Public Sub P_Momento_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        e.Graphics.Clear(P_Momento.BackColor)

        Dim PictureBox As PictureBox = P_Momento

        Dim H_Cuadro As Single = PictureBox.Height
        Dim B_Cuadro As Single = PictureBox.Width

        Dim Escala_X As Single = B_Cuadro / (Proyecto.L_Total * 1.01)
        Dim Escala_Y As Single = H_Cuadro / (Proyecto.Momentos.Max * 2.05)

        Dim Cy As Single = H_Cuadro / 2

        Dim Lapiz_Negro As New Pen(Color.Black) : Lapiz_Negro.Width = 2
        Dim Lapiz_Azul As New Pen(Color.Blue) : Lapiz_Azul.Width = 1

        Dim P1 As New PointF : P1.X = Proyecto.Abscisas(0) * Escala_X : P1.Y = H_Cuadro / 2
        Dim P2 As New PointF : P2.X = Proyecto.Abscisas(Proyecto.Abscisas.Count - 1) * Escala_X : P2.Y = H_Cuadro / 2
        g.DrawLine(Lapiz_Negro, P1, P2)

        Dim P_1 As New PointF
        Dim M_Point As New List(Of PointF)
        M_Point.Clear()

        For i = 0 To Proyecto.Abscisas.Count - 1
            P_1.X = Proyecto.Abscisas(i) * Escala_X
            P_1.Y = Cy + Proyecto.Momentos(i) * Escala_Y
            Dim P_2 As New PointF : P_2.X = P_1.X : P_2.Y = Cy

            M_Point.Add(P_1)

            If i > 0 Then
                g.DrawLine(Lapiz_Azul, M_Point(i - 1), M_Point(i))
            End If
            g.DrawLine(Lapiz_Azul, P_1, P_2)
        Next

    End Sub

    Public Sub P_Cortante_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        e.Graphics.Clear(P_Cortante.BackColor)

        Dim PictureBox As PictureBox = P_Cortante

        Dim H_Cuadro As Single = PictureBox.Height
        Dim B_Cuadro As Single = PictureBox.Width

        Dim Escala_X As Single = B_Cuadro / (Proyecto.L_Total * 1.01)
        Dim Escala_Y As Single = H_Cuadro / (Proyecto.Cortantes.Max * 2.05)

        Dim Cy As Single = H_Cuadro / 2
        Dim Cx As Single = 5

        Dim Lapiz_Negro As New Pen(Color.Black) : Lapiz_Negro.Width = 2
        Dim Lapiz_Azul As New Pen(Color.Blue) : Lapiz_Azul.Width = 1

        Dim P1 As New PointF : P1.X = Proyecto.Abscisas(0) * Escala_X + Cx : P1.Y = H_Cuadro / 2
        Dim P2 As New PointF : P2.X = Proyecto.Abscisas(Proyecto.Abscisas.Count - 1) * Escala_X + Cx : P2.Y = H_Cuadro / 2
        g.DrawLine(Lapiz_Negro, P1, P2)

        Dim P_1 As New PointF
        Dim M_Point As New List(Of PointF)
        M_Point.Clear()

        For i = 0 To Proyecto.Abscisas.Count - 1
            P_1.X = Proyecto.Abscisas(i) * Escala_X + Cx
            P_1.Y = Cy + Proyecto.Cortantes(i) * Escala_Y
            Dim P_2 As New PointF : P_2.X = P_1.X : P_2.Y = Cy

            M_Point.Add(P_1)

            If i > 0 Then
                g.DrawLine(Lapiz_Azul, M_Point(i - 1), M_Point(i))
            End If
            g.DrawLine(Lapiz_Azul, P_1, P_2)
        Next

    End Sub

    Private Sub ToolStripMenuItem5_Click(sender As Object, e As EventArgs) Handles SaveAs_Escaleras.Click
        GuardarProyecto(Proyecto, "RevisiónEscaleras")
    End Sub

    Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs) Handles Save_Escaleras.Click
        Try
            If Proyecto.Ruta = String.Empty Then
                GuardarProyecto(Proyecto, "RevisiónEscaleras")
            Else
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles Open_Escaleras.Click
        Open()
    End Sub

    Public Sub Open()
        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto_Escaleras)(Open.FileName)
            Llenar_Celdas()
            Rellenar()
        End If
    End Sub

    Public Sub Llenar_Celdas()

        T_fc.Text = Proyecto.fc
        T_fy.Text = Proyecto.fy
        T_C_Impuesta.Text = Proyecto.C_Imp
        T_C_Viva.Text = Proyecto.C_Viv
        T_PesoConcretoReforzado.Text = Proyecto.P_ConR
        T_PesoConcreto.Text = Proyecto.P_Con

        T_Huella.Text = Proyecto.Huella
        T_Contrahuella.Text = Proyecto.Contrahuella
        T_NPeldaños.Text = Proyecto.N_Peldanos
        T_LPeldaños.Text = Proyecto.L_Peldanos
        T_LDescanso.Text = Proyecto.L_Descanso
        T_L.Text = Proyecto.L_Total
        T_AEscalera.Text = Proyecto.A_Escalera
        T_AEstudio.Text = Proyecto.A_Estudio
        T_Recubrimiento.Text = Proyecto.Recubrimiento
        T_H.Text = Proyecto.h

        C_BarraFlexion.SelectedIndex = Proyecto.Barra_Flexion
        C_BarraTemperatura.SelectedIndex = Proyecto.Barra_Temperatura

    End Sub
    Public Sub Rellenar()

        Label43.Visible = True
        T_NPuntos.Visible = True

        Label47.Visible = True
        T_Vmax.Visible = True
        Label48.Visible = True
        T_Mmax.Visible = True

        AddHandler P_Momento.Paint, AddressOf Me.P_Momento_Paint
        P_Momento.Refresh()

        AddHandler P_Cortante.Paint, AddressOf Me.P_Cortante_Paint
        P_Cortante.Refresh()

        '--------------- Retraccion y temperatura -----------
        T_CuantiaTemperatura.Text = Format(Proyecto.Cuantia_Temperatura, "##,##0.0000")
        T_AsTemperatura.Text = Math.Round(Proyecto.Acero_Temperatura, 0)
        T_SRequeridaTemperatura.Text = Format(Proyecto.S_Temperatura, "##,##0.00")
        T_SColocadaTemperatura.Text = Format(Proyecto.S_Temperatura_Colocada, "##,##0.00")
        If Proyecto.Cuantia_Temperaruta_Colocada >= 0.9 * Proyecto.Cuantia_Temperatura Then
            Casilla_Cumple(T_VerificacionTemperatura)
            T_VerificacionTemperatura.Text = "Cumple"
        Else
            Casilla_Nocumple(T_VerificacionTemperatura)
            T_VerificacionTemperatura.Text = "No cumple"
        End If

        '------------------ Diseño Flexion --------------------
        T_MuFlexion.Text = Math.Round(Proyecto.Momentos.Max, 2)
        T_CuantiaFlexion.Text = Format(Proyecto.Cuantia_Flexion, "##,##0.0000")
        T_AsFlexion.Text = Math.Round(Proyecto.Acero_Flexion, 0)
        T_SRequeridaFlexion.Text = Format(Proyecto.S_Flexion, "##,##0.00")
        T_SColocadaFlexion.Text = Format(Proyecto.S_Flexion_Colocada, "##,##0.00")

        If Proyecto.Acero_Flexion_Colocado >= 0.9 * Proyecto.Acero_Flexion Then
            Casilla_Cumple(T_VerificacionFlexion)
            T_VerificacionFlexion.Text = "Cumple"
        Else
            Casilla_Nocumple(T_VerificacionFlexion)
            T_VerificacionFlexion.Text = "No cumple"
        End If

        '-------------- Diseño a cortante--------------------
        T_Vu.Text = Format(Proyecto.Vu, "##,##0.00")
        T_Vc.Text = Format(Proyecto.Vc, "##,##0.00")

        If Proyecto.Vc >= Proyecto.Vu Then
            Proyecto.Verificacion_Cortante = True
            Casilla_Cumple(T_VerificacionCortante)
            T_VerificacionCortante.Text = "Cumple"
        Else
            Proyecto.Verificacion_Cortante = False
            Casilla_Nocumple(T_VerificacionCortante)
            T_VerificacionCortante.Text = "No cumple"
        End If

    End Sub

    Private Sub T_Huella_TextChanged(sender As Object, e As EventArgs) Handles T_Huella.TextChanged
        Try
            If T_NPeldaños.Text <> String.Empty And T_Huella.Text <> String.Empty Then
                T_LPeldaños.Text = Math.Round(Convert.ToSingle(T_NPeldaños.Text) * Convert.ToSingle(T_Huella.Text), 2)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub T_NPeldaños_TextChanged(sender As Object, e As EventArgs) Handles T_NPeldaños.TextChanged
        Try
            If T_NPeldaños.Text <> String.Empty And T_Huella.Text <> String.Empty Then
                T_LPeldaños.Text = Math.Round(Convert.ToSingle(T_NPeldaños.Text) * Convert.ToSingle(T_Huella.Text), 2)
            End If
        Catch ex As Exception

        End Try
    End Sub
End Class