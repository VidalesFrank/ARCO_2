Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias

Public Class Form_08_VigasFundacion

    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        ResumenDI.Rows.Clear()

        Dim B As Double = Convert.ToDouble(b_Elemento.Text)
        Dim H As Double = Convert.ToDouble(h_Elemento.Text)
        Dim rec As Double = Convert.ToDouble(Recubrimiento.Text)
        Dim L As Double = Convert.ToDouble(Longitudid.Text)

        Dim Barra_Sup As String = Ref_L1.Text
        Dim As_Sup As Double = Convert.ToDouble(As_L1.Text)
        Dim Cant_Ref_Sup As Double = Convert.ToDouble(NumRef_L1.Text)
        Dim As_T_Sup As Double = As_Sup * Cant_Ref_Sup

        Dim Barra_Inf As String = Ref_L2.Text
        Dim As_Inf As Double = Convert.ToDouble(As_L2.Text)
        Dim Cant_Ref_Inf As Double = Convert.ToDouble(NumRef_L2.Text)
        Dim As_T_Inf As Double = As_Inf * Cant_Ref_Inf

        Dim Barra_Est As String = Ref_Estribos.Text
        Dim As_Est As Double = Convert.ToDouble(As_Estribos.Text)
        Dim Sep_Est As Double = Convert.ToDouble(Sep_Estribos.Text)

        'Materiales
        Dim fc As Double = Convert.ToDouble(T_fc.Text)
        Dim fy As Double = Convert.ToDouble(T_fy.Text)

        Dim lista_Refuerzo As New List(Of RefuerzoSimple)

        Dim Cant_Barras_Total As Double = Cant_Ref_Sup + Cant_Ref_Inf

        Dim contador As Integer = 1

        For i = 0 To Cant_Ref_Sup - 1

            Dim ID As Integer = contador
            contador += 1
            Dim Db As Single = DiametroRefuerzo(Barra_Sup)
            Dim As_b As Single = As_Sup

            Dim B_Efect As Single = B - 2 * rec
            Dim Sep As Single = B_Efect / (Cant_Ref_Sup - 1)

            Dim Coor_X_1 As Single = -B / 2 + rec + Db / 2 + i * Sep
            Dim Coor_Y_1 As Single = H / 2 - rec

            Dim Refuerzo_1 As New RefuerzoSimple(ID, Barra_Sup, Db, As_b, Coor_X_1, Coor_Y_1)

            lista_Refuerzo.Add(Refuerzo_1)

        Next

        For i = 0 To Cant_Ref_Inf - 1

            Dim ID As Integer = contador
            contador += 1
            Dim Db As Single = DiametroRefuerzo(Barra_Inf)
            Dim As_b As Single = As_Inf

            Dim B_Efect As Single = B - 2 * rec
            Dim Sep As Single = B_Efect / (Cant_Ref_Inf - 1)

            Dim Coor_X_1 As Single = -B / 2 + rec + Db / 2 + i * Sep
            Dim Coor_Y_1 As Single = -H / 2 + rec

            Dim Refuerzo_1 As New RefuerzoSimple(ID, Barra_Sup, Db, As_b, Coor_X_1, Coor_Y_1)

            lista_Refuerzo.Add(Refuerzo_1)

        Next

        ' PINTAR EL DIAGRAMA DE INTERACCION
        Dim result = Funcion_Diagrama_Interaccion(B, H, lista_Refuerzo, fc, fy, 200000, 0)
        Dim Lista_Phi_Mn As List(Of Single) = result.Item1
        Dim Lista_Phi_Pn As List(Of Single) = result.Item2
        Dim Lista_Mn As List(Of Single) = result.Item3
        Dim Lista_Pn As List(Of Single) = result.Item4


        PictureBox1.Refresh()

        AddHandler PictureBox1.Paint, Sub(s, pe) PictureBox5_Paint(s, pe, B, H, rec, lista_Refuerzo)

        PictureBox1.Refresh()

        GraficarInteraccion(Lista_Phi_Mn, Lista_Phi_Pn)

        Dim Pmax As Double = Convert.ToDouble(Pu_Analisis.Text)
        Dim Aa As Double = Convert.ToDouble(T_Aa.Text)
        Dim Pu As Double = 0.25 * Aa * Pmax

        ResumenDI.Rows.Add(Math.Round(Lista_Phi_Pn.Max, 2), Math.Round(Pu, 2), Math.Round(Lista_Phi_Pn.Max / Pu, 2))
        ResumenDI.Rows.Add(Math.Round(Lista_Phi_Pn.Min, 2), Math.Round(Pu, 2), Math.Abs(Math.Round(Lista_Phi_Pn.Min / Pu, 2)))

        Dim NDE As String = Proyecto.ParametrosSismicos.NDE
        Dim Den_ As Double = 20

        'Verificaciones normativas
        If NDE = "DES" Then
            Den_ = 20
        End If

        Dim H_Min As Double = L / Den_
        Dim Chequeo_Dim As String = "Cumple"
        Dim Detalle_Dim As String = $"Max(B,H) = {Math.Max(B, H):0.00} >= H_Min = {H_Min:0.00}"

        If Math.Max(B, H) < H_Min Then
            Chequeo_Dim = "No Cumple"
            Detalle_Dim = $"Max(B,H) = {Math.Max(B, H):0.00} < H_Min = {H_Min:0.00}"
        End If

        Dim Cuantia_Sup As Double = As_T_Sup / (B * (H - rec) * 1000000.0)
        Dim Cuantia_Inf As Double = As_T_Inf / (B * (H - rec) * 1000000.0)

        Dim Chequeo_Cuantia As String = "Cumple"
        Dim Detalle_Cuantia As String = $"Rho = {Math.Min(Cuantia_Sup, Cuantia_Inf):0.0000} >= Rho_Min = {0.0033}"

        If Math.Min(Cuantia_Sup, Cuantia_Inf) < 0.0033 Then
            Chequeo_Cuantia = "No Cumple"
            Detalle_Cuantia = $"Rho = {Math.Min(Cuantia_Sup, Cuantia_Inf):0.0000} < Rho_Min = {0.0033}"
        End If

        Dim Sep_Max_Est As Double = Math.Min(Math.Min(B, H) / 2, 0.3)

        Dim Chequeo_Estribos As String = "Cumple"
        Dim Detalle_Estribos As String = $"Sep = {Sep_Est:0.00} ´<= Sep. Max = {Sep_Max_Est:0.00}"

        If Sep_Max_Est < Sep_Est Then
            Chequeo_Estribos = "No Cumple"
            Detalle_Estribos = $"Sep = {Sep_Est:0.00} ´> Sep. Max = {Sep_Max_Est:0.00}"
        End If


        'Resumen Requisitos
        Resultados_Requisitos.Rows.Clear()

        Resultados_Requisitos.Rows.Add("Dimensiones", Detalle_Dim, Chequeo_Dim)
        Resultados_Requisitos.Rows.Add("Cuantia Long.", Detalle_Cuantia, Chequeo_Cuantia)
        Resultados_Requisitos.Rows.Add("Sep. Est.", Detalle_Estribos, Chequeo_Estribos)


        For i = 0 To 2
            Dim fila_res As DataGridViewRow = Resultados_Requisitos.Rows(i)

            Dim columnasCumple_report() As Integer = {2}

            For Each col As Integer In columnasCumple_report

                Dim valor As String = fila_res.Cells(col).Value.ToString()

                If valor = "Cumple" Then
                    fila_res.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                    fila_res.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                Else
                    fila_res.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                    fila_res.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                End If

                ' Opcional: mejorar presentación
                fila_res.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                fila_res.Cells(col).Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
            Next
        Next



    End Sub

    Private Sub GraficarInteraccion(ByVal List_Mn As List(Of Single), ByVal List_Pn As List(Of Single))

        Grafico_DI.Series.Clear()

        Dim serie As New Series("Diagrama Interacción")
        serie.ChartType = SeriesChartType.Spline
        serie.BorderWidth = 2
        serie.Color = Color.Blue

        For i As Integer = 0 To List_Mn.Count - 1
            serie.Points.AddXY(List_Mn(i), List_Pn(i))
        Next

        Grafico_DI.Series.Add(serie)

        Grafico_DI.ChartAreas(0).AxisX.Title = "Momento (kN.m)"
        Grafico_DI.ChartAreas(0).AxisY.Title = "Carga Axial (kN)"

        Grafico_DI.ChartAreas(0).AxisX.Minimum = List_Mn.Min() * 1.1
        Grafico_DI.ChartAreas(0).AxisX.Maximum = List_Mn.Max() * 1.1
        Grafico_DI.ChartAreas(0).AxisX.Interval = 200
        Grafico_DI.ChartAreas(0).AxisY.Minimum = List_Pn.Min() * 1.1
        Grafico_DI.ChartAreas(0).AxisY.Maximum = List_Pn.Max() * 1.1
        Grafico_DI.ChartAreas(0).AxisY.Interval = 400

        With Grafico_DI
            .BorderlineColor = Color.Black
            .BorderlineWidth = 2 ' Ajusta el grosor de la línea según lo necesites
            .BorderlineDashStyle = DataVisualization.Charting.ChartDashStyle.Solid ' Estilo sólido
        End With

        With Grafico_DI.ChartAreas(0)

            .AxisY.MajorGrid.Enabled = False
            .AxisY.MinorGrid.Enabled = False

            .AxisX.MajorGrid.Enabled = False
            .AxisX.MinorGrid.Enabled = False

            .AxisX.TitleFont = New Font("Arial", 14, FontStyle.Bold) ' Cambiar el tamaño de la fuente del título del eje X
            .AxisY.TitleFont = New Font("Arial", 14, FontStyle.Bold) ' Cambiar el tamaño de la fuente del título del eje Y

            With .AxisY
                Dim stepSize As Integer = 100 ' Ajusta según sea necesario
                Dim maxValue = Math.Ceiling(List_Pn.Max() / stepSize) * stepSize
                Dim minValue = Math.Floor(List_Pn.Min() / stepSize) * stepSize
                Dim interval = (maxValue - minValue) / 10

                .Maximum = maxValue
                .Minimum = minValue
                .Interval = interval
            End With

        End With

        ConfigurarLeyenda()

    End Sub
    Public Sub ConfigurarLeyenda()
        With Grafico_DI.Legends(0)
            .Docking = DataVisualization.Charting.Docking.Top ' Posición superior
            .Alignment = StringAlignment.Far ' Alineación derecha
            .IsDockedInsideChartArea = True ' Dentro del área del gráfico
            .BackColor = Color.Transparent ' Fondo transparente
            .BorderColor = Color.Black ' Color de borde (opcional)
            .Font = New Font("Arial", 12, FontStyle.Regular) ' Cambiar el tamaño de la fuente de la leyenda
        End With
    End Sub

    Public Sub PictureBox5_Paint(ByVal sender As Object, ByVal e As PaintEventArgs, B As Single, H As Single, rec As Single, Detalles_Refuerzo As List(Of RefuerzoSimple))

        Dim g As Graphics = e.Graphics

        g.Clear(PictureBox1.BackColor)

        Dim PictureBox5 = PictureBox1

        Dim R As Double = rec
        Dim Cuantia As Double = Math.Round(Cuantia * 100, 2)

        'LbCuantia.Text = Convert.ToString(Cuantia & " %")

        Dim cenY As Integer = Convert.ToInt16(Math.Round(PictureBox5.Height() / 2, 0))
        Dim cenX As Integer = Convert.ToInt16(Math.Round(PictureBox5.Width() / 2, 0))
        Dim Esc As Double

        If B = H Then
            Esc = (Math.Min(PictureBox5.Width, PictureBox5.Height) - 20) / B
        ElseIf B > H Then
            Esc = (PictureBox5.Width - 20) / B
        Else
            Esc = (PictureBox5.Height - 20) / H
        End If

        Dim Esc1 As Double
        Dim Besc1 As Integer = B * Esc1
        Dim Hesc1 As Integer = H * Esc1
        Dim Resc1 As Integer = R * Esc1
        Dim Besc As Integer = B * Esc
        Dim Hesc As Integer = H * Esc
        Dim Resc As Integer = R * Esc
        Dim pen As New Pen(Color.FromArgb(74, 74, 74)) : pen.Width = 2
        Dim penG As New Pen(Color.Green) : penG.Width = 1
        Dim penBl As New Pen(Color.Blue) : penBl.Width = 1
        Dim penT As New Pen(Color.FromArgb(121, 121, 121)) : penT.Width = 3
        Dim coorxs As Integer = cenX - Besc / 2
        Dim coorys As Integer = cenY - Hesc / 2
        g.DrawRectangle(pen, coorxs, coorys, Besc, Hesc)
        Dim fillR As New SolidBrush(Color.FromArgb(210, 210, 210))
        g.FillRectangle(fillR, coorxs, coorys, Besc, Hesc)

        Try
            If Detalles_Refuerzo.Count > 1 Then
                'Label12.Visible = True
                For i = 0 To Detalles_Refuerzo.Count - 1
                    Dim D As Double = Detalles_Refuerzo(i).Db
                    Dim Dbb As Integer = D * Esc
                    Dim Cxx As Integer = PictureBox5.Width() / 2 + Detalles_Refuerzo(i).Coordenada_X * Esc - Detalles_Refuerzo(i).Db / 2 * Esc
                    Dim Cyy As Integer = PictureBox5.Height() / 2 - Detalles_Refuerzo(i).Coordenada_Y * Esc - Detalles_Refuerzo(i).Db / 2 * Esc
                    Dim solidBruh As New SolidBrush(Color.FromArgb(121, 121, 121))
                    Dim penB As New Pen(Color.FromArgb(21, 21, 21))
                    penB.Width = 1
                    Dim letra As New Font("Arial", 9, FontStyle.Regular, GraphicsUnit.Pixel)
                    Dim cor As New SolidBrush(Color.FromArgb(0, 0, 0))
                    Dim corBl As New SolidBrush(Color.Blue)
                    Dim corG As New SolidBrush(Color.Green)
                    Dim CorN As New SolidBrush(Color.Black)
                    g.DrawLine(penBl, cenX, cenY, cenX + 25, cenY)
                    g.DrawLine(penBl, cenX + 25, cenY, cenX + 20, cenY - 5)
                    g.DrawLine(penBl, cenX + 25, cenY, cenX + 20, cenY + 5)
                    g.DrawLine(penG, cenX, cenY, cenX, cenY - 25)
                    g.DrawLine(penG, cenX, cenY - 25, cenX - 5, cenY - 20)
                    g.DrawLine(penG, cenX, cenY - 25, cenX + 5, cenY - 20)
                    g.DrawString("X (3)", letra, corBl, New PointF(cenX + 26, cenY - 5.5))
                    g.DrawString("Y (2)", letra, corG, New PointF(cenX - 5, cenY - 36))

                    g.FillEllipse(solidBruh, Cxx, Cyy, Dbb, Dbb)
                    g.DrawEllipse(penB, Cxx, Cyy, Dbb, Dbb)
                    g.DrawString(Convert.ToString(Detalles_Refuerzo(i).Id_Patron), letra, CorN, New PointF(Cxx + Dbb, Cyy - Dbb))

                Next
            End If
        Catch ex As Exception

        End Try
        PictureBox5.Update()
    End Sub

    Private Sub Ref_L1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_L1.SelectedIndexChanged
        Try
            If Ref_L1.Text <> "Usuario" Then
                As_L1.Text = AreaRefuerzo(Ref_L1.Text)
            Else
                As_L1.Text = 199
            End If
        Catch ex As Exception

        End Try

    End Sub

    Private Sub Ref_L2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_L2.SelectedIndexChanged
        Try
            If Ref_L2.Text <> "Usuario" Then
                As_L2.Text = AreaRefuerzo(Ref_L2.Text)
            Else
                As_L2.Text = 199
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Ref_Estribos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_Estribos.SelectedIndexChanged
        Try
            If Ref_Estribos.Text <> "Usuario" Then
                As_Estribos.Text = AreaRefuerzo(Ref_Estribos.Text)
            Else
                As_Estribos.Text = 199
        End If
        Catch ex As Exception

        End Try
    End Sub
End Class