Public Class PagMateriales
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        PagMateriales.ActiveForm.Hide()
    End Sub

    Private Sub Fc_TextChanged(sender As Object, e As EventArgs) Handles Fc.TextChanged

        Try
            Ec.Text = Math.Round(4700 * Math.Sqrt(Convert.ToDouble(Fc.Text)), 3)
        Catch ex As Exception

        End Try

    End Sub
End Class