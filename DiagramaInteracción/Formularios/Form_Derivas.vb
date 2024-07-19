Public Class Form_Derivas
    Private Sub Form_Derivas_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        With Tabla_Derivas.DefaultCellStyle
            .Font = New Font("Arial", 10)

            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With

        With Tabla_Derivas.ColumnHeadersDefaultCellStyle
            .Font = New Font("Arial", 10, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With


        With Grafico_Derivas.ChartAreas(0)
            .AxisX.Title = "Deriva (%)"
            .AxisY.Title = "Altura del Edificio (m)"

            .AxisX.TitleFont = New Font("Arial", 11, FontStyle.Bold)
            .AxisY.TitleFont = New Font("Arial", 11, FontStyle.Bold)

            .AxisX.LabelStyle.Font = New Font("Arial", 10)
            .AxisY.LabelStyle.Font = New Font("Arial", 10)

            .AxisX.MajorGrid.LineColor = Color.LightGray
            .AxisY.MajorGrid.LineColor = Color.LightGray
            .AxisX.LabelStyle.Format = "0.00"
            .AxisX.Interval = 0.2
            .AxisX.Minimum = 0
            .AxisX.Maximum = 1.1
        End With


    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    End Sub
End Class