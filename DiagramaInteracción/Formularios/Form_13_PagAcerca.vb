Public Class Licencia
    Private Sub Link_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles Link.LinkClicked
        System.Diagnostics.Process.Start("http://www.estrucmed.com/")
    End Sub

    Private Sub Licencia_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub
End Class