Public Class Form_02_01_02_ResultadosModelo
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
            Tabla_Resultados.Rows(i).Cells(2).Value = "Top"
            Tabla_Resultados.Rows(i + 1).Cells(2).Value = "Bottom"
            Tabla_Resultados.Rows(i).Cells(3).Value = Seccion(i / 2).F_Flexo_Modelo_Top
            Tabla_Resultados.Rows(i + 1).Cells(3).Value = Seccion(i / 2).F_Flexo_Modelo_Bottom
        Next
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim Seccion = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text).Lista_Tramos_Columnas

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Seccion(i / 2).F_Flexo_Modelo_Top = Convert.ToSingle(Tabla_Resultados.Rows(i).Cells(3).Value)
            Seccion(i / 2).F_Flexo_Modelo_Bottom = Convert.ToSingle(Tabla_Resultados.Rows(i + 1).Cells(3).Value)
        Next

    End Sub
End Class