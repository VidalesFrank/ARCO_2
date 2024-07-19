Imports System.Windows.Forms.DataVisualization.Charting
Public Class Form_Graficos
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Boton_ALR_Click(sender As Object, e As EventArgs) Handles Boton_ALR.Click

        Grafico.Series.Clear()
        Dim Fmax As Single

        For i = 0 To Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR.Count - 1

            Dim Serie As New Series
            Serie.ChartType = SeriesChartType.Column
            If i = 0 Then
                Serie.Color = Color.FromArgb(46, 117, 182)
            ElseIf i = 1 Then
                Serie.Color = Color.FromArgb(84, 130, 53)
            ElseIf i = 2 Then
                Serie.Color = Color.FromArgb(124, 124, 124)
            ElseIf i = 3 Then
                Serie.Color = Color.FromArgb(197, 90, 17)
            Else
                Serie.Color = Color.FromArgb(47, 85, 151)
            End If

            Dim g = i

            For j = 0 To Proyecto.Columnas.Lista_Columnas.Count() - 1
                Dim Columna = Proyecto.Columnas.Lista_Columnas(j)
                Dim ALR As Single = Proyecto.Columnas.Lista_Columnas(j).Lista_ALR.Find(Function(p) p.Combinacion = Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR(g)).ALR

                Dim Punto As New DataPoint
                Punto.AxisLabel = Columna.Name_Label
                Punto.XValue = j + 1
                Punto.YValues(0) = ALR

                Serie.Points.Add(Punto)

                If ALR > 0.3 Then
                    Serie.Color = Color.Red
                Else
                    Serie.Color = Color.FromArgb(84, 130, 53)
                End If
                If Fmax < ALR Then
                    Fmax = ALR
                End If
            Next

            Serie.LegendText = Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR(i)

            Grafico.Series.Add(Serie)
        Next

        Grafico.ChartAreas("ChartArea1").AxisY.Title = "Relación de Carga Axial"

        Grafico.ChartAreas("ChartArea1").AxisX.Minimum = 0
        Grafico.ChartAreas("ChartArea1").AxisY.Minimum = 0
        Grafico.ChartAreas("ChartArea1").AxisX.Maximum = Proyecto.Columnas.Lista_Columnas.Count() + 1
        Grafico.ChartAreas("ChartArea1").AxisY.Maximum = (Math.Round(Fmax * 10, 0) + 1) / 10
        'Grafico.ChartAreas("ChartArea1").AxisY.Interval = Math.Round((Math.Round(Fmax * 10, 0) + 1) / 10 / 10, 2)
        Grafico.ChartAreas("ChartArea1").AxisY.Interval = Math.Round(0.1, 2)

    End Sub

    Private Sub Boton_Flexo_Click(sender As Object, e As EventArgs) Handles Boton_Flexo.Click

        Grafico.Series.Clear()

        Dim Serie As New Series
        Dim Serie1 As New Series
        Serie.ChartType = SeriesChartType.StackedColumn
        Serie1.ChartType = SeriesChartType.StackedColumn
        Serie.Color = Color.FromArgb(57, 199, 84)
        Serie1.Color = Color.FromArgb(255, 17, 45)

        Dim Serie2 As New Series
        Serie2.ChartType = SeriesChartType.Line
        Serie2.Color = Color.Black
        Serie2.BorderWidth = 2
        Serie2.BorderDashStyle = ChartDashStyle.DashDot

        Dim Fmax As Single

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count() - 1
            Dim Columna = Proyecto.Columnas.Lista_Columnas(i)
            Dim Punto As New DataPoint
            Punto.AxisLabel = Columna.Name_Label
            Punto.XValue = i + 1
            Punto.YValues(0) = Columna.Lista_F(0)

            If Columna.Lista_F(0) >= 0.9 Then
                Serie.Points.Add(Punto)
                Serie1.Points.AddXY(i + 1, 0)
            Else
                Serie.Points.AddXY(i + 1, 0)
                Serie1.Points.Add(Punto)
            End If

            If Fmax < Columna.Lista_F(0) Then
                Fmax = Columna.Lista_F(0)
            End If
        Next
        Serie2.Points.AddXY(0, 0.9)
        Serie2.Points.AddXY(Proyecto.Columnas.Lista_Columnas.Count() + 1, 0.9)

        Grafico.ChartAreas("ChartArea1").AxisX.Minimum = 0
        Grafico.ChartAreas("ChartArea1").AxisX.Maximum = Proyecto.Columnas.Lista_Columnas.Count() + 1
        Grafico.ChartAreas("ChartArea1").AxisY.Title = "Capacidad/Demanda"

        Grafico.ChartAreas("ChartArea1").AxisY.Maximum = (Math.Round(Fmax * 10, 0) + 1) / 10
        Grafico.ChartAreas("ChartArea1").AxisY.Interval = Math.Round((Math.Round(Fmax * 10, 0) + 1) / 10 / 10, 2)

        Serie.LegendText = "Capacidad Adecuada"
        Serie1.LegendText = "Requiere Aumentar Capacidad"
        Serie2.LegendText = "Capacidad Admisible"

        Grafico.Series.Add(Serie)
        Grafico.Series.Add(Serie1)
        Grafico.Series.Add(Serie2)

    End Sub

    Private Sub Boton_Cortante_Click(sender As Object, e As EventArgs) Handles Boton_Cortante.Click

        Grafico.Series.Clear()

        Dim Serie As New Series
        Dim Serie1 As New Series
        Serie.ChartType = SeriesChartType.Column
        Serie1.ChartType = SeriesChartType.Column
        Serie.Color = Color.FromArgb(46, 117, 182)
        Serie1.Color = Color.FromArgb(84, 130, 53)

        Dim Serie2 As New Series
        Serie2.ChartType = SeriesChartType.Line
        Serie2.Color = Color.Black
        Serie2.BorderWidth = 2
        Serie2.BorderDashStyle = ChartDashStyle.DashDot
        Dim Fmax As Single

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count() - 1
            Dim Columna = Proyecto.Columnas.Lista_Columnas(i)
            Dim Punto As New DataPoint
            Punto.AxisLabel = Columna.Name_Label
            Punto.XValue = i + 1
            Punto.YValues(0) = Columna.Lista_F(1)

            Dim Punto1 As New DataPoint
            Punto1.AxisLabel = Columna.Name_Label
            Punto1.XValue = i + 1
            Punto1.YValues(0) = Columna.Lista_F(2)

            If Fmax < Math.Max(Columna.Lista_F(1), Columna.Lista_F(2)) Then
                Fmax = Math.Max(Columna.Lista_F(1), Columna.Lista_F(2))
            End If

            Serie.Points.Add(Punto)
            Serie1.Points.Add(Punto1)

        Next

        Serie2.Points.AddXY(0, 0.9)
        Serie2.Points.AddXY(Proyecto.Columnas.Lista_Columnas.Count() + 1, 0.9)

        Grafico.ChartAreas("ChartArea1").AxisX.Minimum = 0
        Grafico.ChartAreas("ChartArea1").AxisX.Maximum = Proyecto.Columnas.Lista_Columnas.Count() + 1
        Grafico.ChartAreas("ChartArea1").AxisY.Maximum = (Math.Round(Fmax * 10, 0) + 1) / 10
        Grafico.ChartAreas("ChartArea1").AxisY.Interval = Math.Round((Math.Round(Fmax * 10, 0) + 1) / 10 / 10, 2)

        Grafico.ChartAreas("ChartArea1").AxisY.Title = "φVn/Vu"

        Serie.LegendText = "Sentido Largo"
        Serie1.LegendText = "Sentido Corto"
        Serie2.LegendText = "Capacidad Admisible"

        Grafico.Series.Add(Serie)
        Grafico.Series.Add(Serie1)
        Grafico.Series.Add(Serie2)


    End Sub

    Private Sub CombinacionesDeAnálisisToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CombinacionesDeAnálisisToolStripMenuItem.Click
        For i = 0 To Proyecto.Columnas.Lista_Combinaciones_ALR.Count - 1
            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(Proyecto.Columnas.Lista_Combinaciones_ALR(i))
        Next
        For i = 0 To Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR.Count - 1
            Form_Opciones_Combinaciones.Lista_Cargas_Diseño.Items.Add(Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR(i))
        Next

        Form_Opciones_Combinaciones.Show()
    End Sub

End Class