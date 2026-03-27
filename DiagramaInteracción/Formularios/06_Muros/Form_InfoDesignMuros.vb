Public Class Form_InfoDesignMuros
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Proyecto.Elementos.Muros.D_Techo_X = Convert.ToSingle(T_Dtecho_X.Text)
        Proyecto.Elementos.Muros.D_Techo_Y = Convert.ToSingle(T_Dtecho_Y.Text)

        Me.Close()

    End Sub

    Private Sub Form_InfoDesignMuros_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed

        Proyecto.Elementos.Muros.D_Techo_X = Convert.ToSingle(T_Dtecho_X.Text)
        Proyecto.Elementos.Muros.D_Techo_Y = Convert.ToSingle(T_Dtecho_Y.Text)

    End Sub
End Class