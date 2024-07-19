Public Class Form_Combinaciones
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Not Proyecto.Columnas.Lista_Combinaciones_ALR.Exists(Function(p) p = Combo_Combinaciones.Text.ToString) Then
            Proyecto.Columnas.Lista_Combinaciones_ALR.Add(Combo_Combinaciones.Text)
            Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR.Add(Combo_Combinaciones.Text)
            Tabla_combinaciones.Rows.Add(Combo_Combinaciones.Text)
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub
End Class