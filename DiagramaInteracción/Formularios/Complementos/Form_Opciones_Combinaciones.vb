Public Class Form_Opciones_Combinaciones
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Boton_ALR_Click(sender As Object, e As EventArgs) Handles Boton_ALR.Click

        For i = 0 To Lista_Combinaciones.SelectedItems.Count - 1
            Lista_Cargas_Diseño.Items.Add(Lista_Combinaciones.SelectedItems.Item(i))
        Next
        Lista_Combinaciones.ClearSelected()

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        For i = 0 To Lista_Cargas_Diseño.SelectedItems.Count() - 1
            If Lista_Cargas_Diseño.SelectedItems.Count > 0 Then
                Dim F As Integer = Lista_Cargas_Diseño.SelectedItems.Count - 1
                Lista_Cargas_Diseño.Items.Remove(Lista_Cargas_Diseño.SelectedItems(F))
            End If
        Next

        Lista_Cargas_Diseño.ClearSelected()

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR.Clear()
        For i = 0 To Lista_Cargas_Diseño.Items.Count - 1
            Proyecto.Columnas.Lista_Combinaciones_Grafico_ALR.Add(Lista_Cargas_Diseño.Items(i))
        Next

        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub
End Class