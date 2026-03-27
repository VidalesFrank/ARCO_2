Public Class Form_ImagenProyecto

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Form_06_PagMuros.proyecto.Info.Imagen = P_ImagenProyecto.Image
        Me.Close()
    End Sub
End Class