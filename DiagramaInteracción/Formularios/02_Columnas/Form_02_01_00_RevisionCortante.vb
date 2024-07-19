Public Class Form_02_01_00_RevisionCortante
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Tabla_Resultados.Rows.Clear()

        Dim Seccion = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text).Lista_Tramos_Columnas
        For i = 0 To (Seccion.Count - 1) * 2
            Tabla_Resultados.Rows.Add()
        Next
        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Tabla_Resultados.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Resultados.Rows(i).Cells(1).Value = Seccion(i / 2).Seccion
            Tabla_Resultados.Rows(i).Cells(2).Value = "Largo"
            Tabla_Resultados.Rows(i + 1).Cells(2).Value = "Corto"
            Tabla_Resultados.Rows(i).Cells(3).Value = Seccion(i / 2).Vc_2
            Tabla_Resultados.Rows(i + 1).Cells(3).Value = Seccion(i / 2).Vc_3
            Tabla_Resultados.Rows(i).Cells(4).Value = Seccion(i / 2).Vs_2
            Tabla_Resultados.Rows(i + 1).Cells(4).Value = Seccion(i / 2).Vs_3
            Tabla_Resultados.Rows(i).Cells(5).Value = Seccion(i / 2).Vn_2
            Tabla_Resultados.Rows(i + 1).Cells(5).Value = Seccion(i / 2).Vn_3
            Tabla_Resultados.Rows(i).Cells(6).Value = Seccion(i / 2).Vu_2
            Tabla_Resultados.Rows(i + 1).Cells(6).Value = Seccion(i / 2).Vu_3
            Tabla_Resultados.Rows(i).Cells(7).Value = Seccion(i / 2).F_Cortante_2
            Tabla_Resultados.Rows(i + 1).Cells(7).Value = Seccion(i / 2).F_Cortante_3
        Next

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub
End Class