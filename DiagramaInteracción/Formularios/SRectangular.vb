Imports System.Windows.Forms.DataVisualization.Charting

Partial Public Class SRectangular
    Inherits Form

    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Public B As Single
    Public H As Single
    Public Cuantia As Single
    Public ListaRefuerzo As List(Of RefuerzoSimple)

    Public Sub New()
        InitializeComponent() ' Inicializa los componentes visuales del formulario
    End Sub

    Public Sub New(b As Single, h As Single, cuantia As Single, listaRefuerzo As List(Of RefuerzoSimple))
        Me.New()
        Me.B = b
        Me.H = h
        Me.Cuantia = cuantia
        Me.ListaRefuerzo = listaRefuerzo
    End Sub


    Public Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'AddHandler PictureBox1.Paint, AddressOf Me.PictureBox5_Paint(sender, e, )
        'PictureBox1.Refresh()

    End Sub

    Private Sub Combo_Seccion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Seccion.SelectedIndexChanged


    End Sub


    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim Muro As Muro = Proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Seccion.Text)
        Dim Elemento As String = Muro.Name
        Dim Seccion As SeccionMuro = Muro.Lista_Secciones.Find(Function(p) p.Piso = Combo_Tramos.Text)
        Dim B As Single = Seccion.tw_Planos
        Dim H As Single = Seccion.Lw_Planos
        Dim List_Ref As List(Of RefuerzoSimple) = Seccion.ListaRefuerzoCompleto_Top
        Dim fc As Single = Seccion.fc

        If Combo_Estacion.Text = "Bottom" Then
            List_Ref = Seccion.ListaRefuerzoCompleto_Bot
        End If

        PictureBox1.Refresh()

        AddHandler PictureBox1.Paint, Sub(s, pe) PictureBox5_Paint(s, pe, B, H, List_Ref)

        PictureBox1.Refresh()

        ' PINTAR EL DIAGRAMA DE INTERACCION
        Dim result = Funcion_Diagrama_Interaccion(B, H, List_Ref, fc, 420, 200000, 0)
        Dim Lista_Phi_Mn As List(Of Single) = result.Item1
        Dim Lista_Phi_Pn As List(Of Single) = result.Item2
        Dim Lista_Mn As List(Of Single) = result.Item3
        Dim Lista_Pn As List(Of Single) = result.Item4

        GraficarInteraccion(Lista_Phi_Mn, Lista_Phi_Pn)

        Dim Lista_Combinaciones_Design As List(Of SeccionMuro.Fuerzas_Elementos) = Seccion.Lista_Combinaciones.Where(Function(p) Proyecto.Elementos.Muros.ListA_Combinaciones_Design.Contains(p.Name)).ToList()

        LlenarDatosSolicitaciones(Lista_Combinaciones_Design, Tabla_Solicitaciones)

        Dim Combinacion As SeccionMuro.Fuerzas_Elementos = Seccion.Lista_Combinaciones.Find(Function(p) p.Name = Seccion.Combinacion_Demanda_Flexo_Bot)

        Dim recta_Capacidad = Funciones_Muros.RectaCapacidadDemanda(Combinacion, Lista_Phi_Mn, Lista_Phi_Pn)
        Dim List_X As List(Of Single) = recta_Capacidad.Item1
        Dim List_Y As List(Of Single) = recta_Capacidad.Item2

        Dim nuevaSerie As New DataVisualization.Charting.Series()
        nuevaSerie.Name = "Recta"
        nuevaSerie.ChartType = DataVisualization.Charting.SeriesChartType.Spline
        nuevaSerie.BorderWidth = 2
        nuevaSerie.Color = Color.Red
        nuevaSerie.BorderDashStyle = ChartDashStyle.Dash
        nuevaSerie.IsVisibleInLegend = False

        For i As Integer = 0 To List_X.Count - 1
            nuevaSerie.Points.AddXY(List_X(i), List_Y(i))
        Next

        Grafico_DI.Series.Add(nuevaSerie)

    End Sub


    Public Sub PictureBox5_Paint(ByVal sender As Object, ByVal e As PaintEventArgs, B As Single, H As Single, Detalles_Refuerzo As List(Of RefuerzoSimple))

        Dim g As Graphics = e.Graphics

        g.Clear(PictureBox1.BackColor)

        Dim PictureBox5 = PictureBox1

        Dim R As Double = 0.02
        Dim Cuantia As Double = Math.Round(Cuantia * 100, 2)

        'LbCuantia.Text = Convert.ToString(Cuantia & " %")

        Dim cenY As Integer = Convert.ToInt16(Math.Round(PictureBox5.Height() / 2, 0))
        Dim cenX As Integer = Convert.ToInt16(Math.Round(PictureBox5.Width() / 2, 0))
        Dim Esc As Double

        If B > H Then
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
                    Dim D As Double = Detalles_Refuerzo(i).Db / 1000
                    Dim Dbb As Integer = D * Esc
                    Dim Cxx As Integer = PictureBox5.Width() / 2 + Detalles_Refuerzo(i).Coordenada_X * Esc - Detalles_Refuerzo(i).Db / 2000 * Esc
                    Dim Cyy As Integer = PictureBox5.Height() / 2 - Detalles_Refuerzo(i).Coordenada_Y * Esc - Detalles_Refuerzo(i).Db / 2000 * Esc
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

    Public Sub LlenarDatosSolicitaciones(solicitaciones As List(Of SeccionMuro.Fuerzas_Elementos), dgv As DataGridView)
        dgv.Rows.Clear()
        dgv.Columns.Clear()

        dgv.Columns.Add("Name", "Nombre")
        dgv.Columns.Add("Pu", "Pu (kN)")
        dgv.Columns.Add("Mu", "Mu (kN.m)")

        Dim nuevaSerie As New DataVisualization.Charting.Series()
        nuevaSerie.Name = "Demandas"
        nuevaSerie.ChartType = DataVisualization.Charting.SeriesChartType.Point
        nuevaSerie.MarkerStyle = DataVisualization.Charting.MarkerStyle.Circle
        nuevaSerie.MarkerSize = 7
        nuevaSerie.Color = Color.Black

        For Each solicitacion As SeccionMuro.Fuerzas_Elementos In solicitaciones
            dgv.Rows.Add(solicitacion.Name, -solicitacion.P, Math.Abs(solicitacion.M3))

            nuevaSerie.Points.AddXY(solicitacion.M3, solicitacion.P)
        Next

        Grafico_DI.Series.Add(nuevaSerie)

        ConfigurarLeyenda()

    End Sub


    'Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

    '    Reportes.Tabla_DI.Rows.Clear()
    '    Lista_Elementos.Clear()

    '    Dim Columna = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Seccion.Text)
    '    Dim Tramo = Columna.Lista_Tramos_Columnas.Find(Function(p) p.Piso = Combo_Tramos.Text)
    '    Dim Estacion = Combo_Estacion.Text
    '    Dim B As Double = Tramo.B_Plano
    '    Dim H As Double = Tramo.H_Plano
    '    Dim Detalles_Refuerzo = Tramo.Lista_Detalles_Refuerzo_Top
    '    If Estacion = "Bottom" Then
    '        Detalles_Refuerzo = Tramo.Lista_Detalles_Refuerzo_Bottom
    '    End If

    '    'Diagrama_Interaccion_Rectangular.Diagrama_Interaccion(B, H, Detalles_Refuerzo, Tramo.fc, 420, 200000, 60)

    '    For i = 0 To Lista_Elementos.Count - 1
    '        Reportes.Tabla_DI.Rows.Add(Lista_Elementos(i).Pn, Lista_Elementos(i).Mn_X, Lista_Elementos(i).Mn_Y, Lista_Elementos(i).Phi_Pn, Lista_Elementos(i).Phi_Mn_X, Lista_Elementos(i).Phi_Mn_Y)
    '    Next


    '    Reportes.Show()
    'End Sub



    'Private Sub Combo_Tramos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Tramos.SelectedIndexChanged
    '    Dibujar()
    'End Sub

    'Private Sub Combo_Seccion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Seccion.SelectedIndexChanged
    '    Dibujar()
    'End Sub

    'Private Sub Combo_Estacion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Estacion.SelectedIndexChanged
    '    Dibujar()
    'End Sub
    'Public Sub Dibujar()
    '    If Combo_Seccion.Items.Count > 0 And Combo_Tramos.Items.Count > 0 And Combo_Estacion.Items.Count > 0 Then
    '        Dim Columna = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Seccion.Text)
    '        Dim Tramo = Columna.Lista_Tramos_Columnas.Find(Function(p) p.Piso = Combo_Tramos.Text)
    '        Dim Estacion = Combo_Estacion.Text
    '        If Estacion = "Top" Then
    '            T_Cantidad_Corto.Text = Tramo.Cantidad_Lado_Corto_Top
    '            T_Cantidad_Largo.Text = Tramo.Cantidad_Lado_Largo_Top
    '            T_Acero.Text = Math.Round(Tramo.As_Col_Top / Tramo.Cantidad_Barras_Top, 0)
    '        Else
    '            T_Cantidad_Corto.Text = Tramo.Cantidad_Lado_Corto_Bottom
    '            T_Cantidad_Largo.Text = Tramo.Cantidad_Lado_Largo_Bottom
    '            T_Acero.Text = Math.Round(Tramo.As_Col_Bottom / Tramo.Cantidad_Barras_Bottom, 0)
    '        End If
    '    End If

    '    PictureBox1.Refresh()
    '    Dim PictureBox5 = PictureBox1
    '    AddHandler PictureBox5.Paint, AddressOf Me.PictureBox5_Paint
    'End Sub
    Public Lista_Elementos As New List(Of DI)

    Public Class DI

        Public Pn As Single
        Public Mn_X As Single
        Public Mn_Y As Single
        Public Phi_Pn As Single
        Public Phi_Mn_X As Single
        Public Phi_Mn_Y As Single


    End Class

    Private Sub T_Acero_TextChanged(sender As Object, e As EventArgs) Handles T_Acero.TextChanged

    End Sub

    Private Sub RefuerzoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RefuerzoToolStripMenuItem.Click

    End Sub

    'Private Sub SRectangular_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    '    'AddHandler PictureBox1.Paint, AddressOf Me.PictureBox5_Paint
    '    PictureBox1.Refresh()
    'End Sub

    'Private Sub SRectangular_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
    '    PictureBox1.Location = New Point(35, 66)
    '    PictureBox1.Size = New Size(Panel1.Width - 70, Panel1.Height - 100)
    '    'Label6.Left = Panel1.Width / 2 - Label6.Width / 2
    '    'AddHandler PictureBox1.Paint, AddressOf Me.PictureBox5_Paint
    '    'PictureBox1.Refresh()
    'End Sub


End Class