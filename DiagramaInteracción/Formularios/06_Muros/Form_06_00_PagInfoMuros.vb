Imports ARCO.Funciones_Muros
Imports ARCO.Funciones_00_Varias

Public Class Form_06_00_PagInfoMuros
    Public Shared proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Try
            Tabla_Info_Seccion.Rows.Clear()
            Tabla_Info_EBorde.Rows.Clear()

            Dim Elemento As String = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Name
            Dim Seccion = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Lista_Secciones
            Dim SeccionesPatron As List(Of SeccionMuro)

            If IsNothing(Seccion.FindAll(Function(p) p.S_Patron = True)) Then
            Else
                SeccionesPatron = Seccion.FindAll(Function(p) p.S_Patron = True)

                For j = 0 To SeccionesPatron.Count - 1
                    C_Similar_1.Items.Add(SeccionesPatron(j).Piso)
                    C_Similar.Items.Add(SeccionesPatron(j).Piso)
                Next
            End If

            For i = 0 To (Seccion.Count * 2 - 1)
                Tabla_Info_Seccion.Rows.Add()
                Tabla_Info_EBorde.Rows.Add()
            Next

            Tabla_Info_Seccion.Refresh()
            Tabla_Info_EBorde.Refresh()

            For i = 0 To ((Seccion.Count * 2) - 1) Step 2
                Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

                If Seccion(i / 2).S_Patron = False Then
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "None"
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "Si"
                End If

                Tabla_Info_EBorde.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_EBorde.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_EBorde.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_EBorde.Rows(i + 1).Cells(4).Value = "Bottom"

                If proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Ref_Modificado_Muros = False Then
                    For j = 7 To 14

                        Tabla_Info_Seccion.Rows(i).Cells(j).Value = 0
                        Tabla_Info_Seccion.Rows(i + 1).Cells(j).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 1).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 1).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 14).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 14).Value = 0

                    Next

                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = ""
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = 0

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = "0"
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = "0"

                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(6).Value = ""
                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(18).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(20).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = 0

                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(19).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(32).Value = "None"

                Else

                    Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_2
                    Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_3
                    Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_4
                    Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_5
                    Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_6
                    Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_7
                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_8
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_10

                    Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_2
                    Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_3
                    Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_4
                    Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_5
                    Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_6
                    Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_7
                    Tabla_Info_Seccion.Rows(i + 1).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_8
                    Tabla_Info_Seccion.Rows(i + 1).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_10

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Malla.Malla
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).Malla.Capas)
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = Seccion(i / 2).RefH_W_Col.Acero
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = Seccion(i / 2).RefH_W_Col.Separacion
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = Convert.ToString(Seccion(i / 2).RefH_W_Col.Capas)

                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = Seccion(i / 2).EB_I_Top.L_EB
                    Tabla_Info_EBorde.Rows(i).Cells(8).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(9).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(10).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(11).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(12).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(13).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(14).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(15).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(8).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(9).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(10).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(11).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(12).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(13).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(14).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(15).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).EB_I_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = Seccion(i / 2).EB_I_Top.RefH.Acero
                    Tabla_Info_EBorde.Rows(i).Cells(18).Value = Seccion(i / 2).EB_I_Top.RefH.Separacion

                    Tabla_Info_EBorde.Rows(i).Cells(20).Value = Seccion(i / 2).EB_D_Top.L_EB

                    Tabla_Info_EBorde.Rows(i).Cells(21).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(22).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(23).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(24).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(25).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(26).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(27).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(28).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(21).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(22).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(23).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(24).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(25).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(26).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(27).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(28).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = Convert.ToString(Seccion(i / 2).EB_D_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = Seccion(i / 2).EB_D_Top.RefH.Acero
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = Seccion(i / 2).EB_D_Top.RefH.Separacion

                End If
            Next

        Catch ex As Exception
        Finally
            T_Seccion.Text = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Label
        End Try

    End Sub

    Private Sub Tabla_Info_Seccion_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_Seccion.CellValueChanged
        For i = 0 To Tabla_Info_Seccion.Rows.Count - 1
            For j = 7 To 14
                If Tabla_Info_Seccion.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_Seccion, i, j)
                End If
            Next
        Next
    End Sub
    Private Sub Tabla_Info_Eb_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_EBorde.CellValueChanged
        For i = 0 To Tabla_Info_EBorde.Rows.Count - 1
            For j = 8 To 15
                If Tabla_Info_EBorde.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_EBorde, i, j)
                End If
            Next
            For j = 21 To 28
                If Tabla_Info_EBorde.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_EBorde, i, j)
                End If
            Next
        Next
    End Sub

    Sub Color_Celda(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer)
        Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 224, 180)
        Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.Red
    End Sub


    Private Sub Form_06_00_PagInfoMuros_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Dim nuevaFuente As Font = New Font("Microsoft Sans Serif", 10, FontStyle.Bold)

        Dim Tabla = Tabla_Info_Seccion
        For i = 0 To Tabla.ColumnCount - 1
            If i < 5 Then
                If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                    Tabla.Columns(i).ReadOnly = True
                    Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                    Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
                End If
            End If
        Next


        Tabla = Tabla_Info_EBorde
        For i = 0 To Tabla.ColumnCount - 1
            If i < 5 Then
                If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                    Tabla.Columns(i).ReadOnly = True
                    Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                    Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
                End If
            End If
        Next

        If proyecto.Muros.Lista_Muros.Count > 0 Then
            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                Combo_Elementos.Items.Add(proyecto.Muros.Lista_Muros(i).Name)
            Next
            Combo_Elementos.Text = proyecto.Muros.Lista_Muros(0).Name
        End If



    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        'Try

        Dim Seccion = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Lista_Secciones
        Dim Elemento As String = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Name

        proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Label = T_Seccion.Text

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Seccion(i / 2).fc = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(1).Value)
            Seccion(i / 2).tw_Planos = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(2).Value)
            Seccion(i / 2).Lw_Planos = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(3).Value)
            Seccion(i / 2).AgM = Seccion(i / 2).Lw_Planos * Seccion(i / 2).tw_Planos

            Seccion(i / 2).Refuerzo_Muro_Top.Barras_2 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(7).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_3 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(8).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_4 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(9).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_5 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(10).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_6 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(11).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_7 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(12).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_8 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(13).Value)
            Seccion(i / 2).Refuerzo_Muro_Top.Barras_10 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(14).Value)

            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_2 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_3 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_4 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_5 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_6 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_7 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_8 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(13).Value)
            Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_10 = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(14).Value)

            Dim AreasAcero() As Single = {32, 71, 129, 199, 284, 387, 510, 819}
            Dim AsT_Top As Single = 0
            Dim AsT_Bot As Single = 0
            For j = 0 To 7
                Dim ac As Single = AreasAcero(j)
                Dim numBar_Top As Single = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(7 + j).Value)
                Dim numBar_Bot As Single = Convert.ToSingle(Tabla_Info_Seccion.Rows(i + 1).Cells(7 + j).Value)
                AsT_Top += ac * numBar_Top
                AsT_Bot += ac * numBar_Bot
            Next

            Seccion(i / 2).AsT_Top_Col = AsT_Top + (Acero_Mallas(Seccion(i / 2).Malla.Malla) * Seccion(i / 2).Malla.Capas * Seccion(i / 2).Lw_Planos)
            Seccion(i / 2).AsT_Bot_Col = AsT_Bot + (Acero_Mallas(Seccion(i / 2).Malla.Malla) * Seccion(i / 2).Malla.Capas * Seccion(i / 2).Lw_Planos)
            Seccion(i / 2).Cuantia_Top_Col = (Seccion(i / 2).AsT_Top_Col / (Seccion(i / 2).AgM * 1000000)) * 100
            Seccion(i / 2).Cuantia_Bot_Col = (Seccion(i / 2).AsT_Bot_Col / (Seccion(i / 2).AgM * 1000000)) * 100

            'Seccion(i / 2).Cuantia_Top_Col = (AsT_Top / (Seccion(i / 2).AgM * 1000000) + Calculo_Cuantia(Seccion(i / 2).tw_Planos, Seccion(i / 2).Malla.Malla, Seccion(i / 2).Malla.Capas, 1, Seccion(i / 2).RefH_W_Col.Acero, 1, 0)) * 100
            'Seccion(i / 2).Cuantia_Bot_Col = (AsT_Bot / (Seccion(i / 2).AgM * 1000000) + Calculo_Cuantia(Seccion(i / 2).tw_Planos, Seccion(i / 2).Malla.Malla, Seccion(i / 2).Malla.Capas, 1, Seccion(i / 2).RefH_W_Col.Acero, 1, 0)) * 100

            Seccion(i / 2).Malla.Malla = Tabla_Info_Seccion.Rows(i).Cells(15).Value
            Seccion(i / 2).Malla.Capas = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(16).Value)
            Seccion(i / 2).Malla.Acero_T = Acero_Mallas(Seccion(i / 2).Malla.Malla) * Seccion(i / 2).Malla.Capas

            Seccion(i / 2).RefH_W_Col.Acero = Tabla_Info_Seccion.Rows(i).Cells(17).Value
            Seccion(i / 2).RefH_W_Col.Separacion = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(18).Value)
            Seccion(i / 2).RefH_W_Col.Capas = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(19).Value)
            If Seccion(i / 2).RefH_W_Col.Separacion > 0 Then
                Seccion(i / 2).RefH_W_Col.Acero_T = (1 / Seccion(i / 2).RefH_W_Col.Separacion + 1) * AreaRefuerzo(Seccion(i / 2).RefH_W_Col.Acero) * Seccion(i / 2).RefH_W_Col.Capas
            End If
            Seccion(i / 2).Rho_H_col = Calculo_Cuantia(Seccion(i / 2).tw_Planos, Seccion(i / 2).Malla.Malla, Seccion(i / 2).Malla.Capas, 1, Seccion(i / 2).RefH_W_Col.Acero, Seccion(i / 2).RefH_W_Col.Capas, Seccion(i / 2).RefH_W_Col.Separacion)
            'Seccion(i / 2).AsH_Col = Seccion(i / 2).Malla.Acero_T + Seccion(i / 2).RefH_W_Col.Acero_T
            Seccion(i / 2).AsH_Col = Seccion(i / 2).Rho_H_col * 1000 * Seccion(i / 2).tw_Planos * 1000

            ' ------------- Tabla Elementos de Borde ----------------------
            '--------- INFORMACION DE EB IZQUIERDO ---------------
            Seccion(i / 2).EB_I_Top.L_EB = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(7).Value)
            Seccion(i / 2).EB_I_Bot.L_EB = Seccion(i / 2).EB_I_Top.L_EB

            Seccion(i / 2).EB_I_Top.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i).Cells(8).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i).Cells(9).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i).Cells(10).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i).Cells(11).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i).Cells(12).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i).Cells(13).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i).Cells(14).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i).Cells(15).Value

            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i + 1).Cells(8).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i + 1).Cells(9).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i + 1).Cells(10).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i + 1).Cells(11).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i + 1).Cells(12).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i + 1).Cells(13).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i + 1).Cells(14).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i + 1).Cells(15).Value

            AsT_Top = 0
            AsT_Bot = 0
            For j = 0 To 7
                Dim ac As Single = AreasAcero(j)
                Dim numBar_Top As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(8 + j).Value)
                Dim numBar_Bot As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i + 1).Cells(8 + j).Value)
                AsT_Top += ac * numBar_Top
                AsT_Bot += ac * numBar_Bot
            Next

            Seccion(i / 2).EB_I_Top.RefH.Capas = Convert.ToInt16(Tabla_Info_EBorde.Rows(i).Cells(16).Value)
            Seccion(i / 2).EB_I_Top.RefH.Acero = Tabla_Info_EBorde.Rows(i).Cells(17).Value
            Seccion(i / 2).EB_I_Top.RefH.Separacion = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(18).Value)
            Seccion(i / 2).EB_I_Top.RefH.Acero_T = AreaRefuerzo(Seccion(i / 2).EB_I_Top.RefH.Acero) * Seccion(i / 2).EB_I_Top.RefH.Capas
            Seccion(i / 2).EB_I_Top.Cuantia_L = (AsT_Top / (Seccion(i / 2).EB_I_Top.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100
            If Seccion(i / 2).EB_I_Top.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_I_Top.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_I_Top_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_I_Top_NoEsp = False
            End If

            Seccion(i / 2).EB_I_Bot.RefH.Capas = Seccion(i / 2).EB_I_Top.RefH.Capas
            Seccion(i / 2).EB_I_Bot.RefH.Acero = Seccion(i / 2).EB_I_Top.RefH.Acero
            Seccion(i / 2).EB_I_Bot.RefH.Separacion = Seccion(i / 2).EB_I_Top.RefH.Separacion
            Seccion(i / 2).EB_I_Bot.RefH.Acero_T = Seccion(i / 2).EB_I_Top.RefH.Acero_T
            Seccion(i / 2).EB_I_Bot.Cuantia_L = (AsT_Bot / (Seccion(i / 2).EB_I_Bot.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100
            If Seccion(i / 2).EB_I_Bot.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_I_Bot.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_I_Bot_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_I_Bot_NoEsp = False
            End If

            '--------- INFORMACION DE EB DERECHO ---------------
            Seccion(i / 2).EB_D_Top.L_EB = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(20).Value)
            Seccion(i / 2).EB_D_Bot.L_EB = Seccion(i / 2).EB_D_Top.L_EB

            Seccion(i / 2).EB_D_Top.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i).Cells(21).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i).Cells(22).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i).Cells(23).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i).Cells(24).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i).Cells(25).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i).Cells(26).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i).Cells(27).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i).Cells(28).Value

            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i + 1).Cells(21).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i + +1).Cells(22).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i + +1).Cells(23).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i + +1).Cells(24).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i + +1).Cells(25).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i + +1).Cells(26).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i + +1).Cells(27).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i + 1).Cells(28).Value

            AsT_Top = 0
            AsT_Bot = 0
            For j = 0 To 7
                Dim ac As Single = AreasAcero(j)
                Dim numBar_Top As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(21 + j).Value)
                Dim numBar_Bot As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i + 1).Cells(21 + j).Value)
                AsT_Top += ac * numBar_Top
                AsT_Bot += ac * numBar_Bot
            Next

            Seccion(i / 2).EB_D_Top.RefH.Capas = Convert.ToInt16(Tabla_Info_EBorde.Rows(i).Cells(29).Value)
            Seccion(i / 2).EB_D_Top.RefH.Acero = Tabla_Info_EBorde.Rows(i).Cells(30).Value
            Seccion(i / 2).EB_D_Top.RefH.Separacion = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(31).Value)
            Seccion(i / 2).EB_D_Top.RefH.Acero_T = AreaRefuerzo(Seccion(i / 2).EB_D_Top.RefH.Acero) * Seccion(i / 2).EB_D_Top.RefH.Capas
            Seccion(i / 2).EB_D_Top.Cuantia_L = (AsT_Top / (Seccion(i / 2).EB_D_Top.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100
            If Seccion(i / 2).EB_D_Top.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_D_Top.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_D_Top_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_D_Top_NoEsp = False
            End If

            Seccion(i / 2).EB_D_Bot.RefH.Capas = Seccion(i / 2).EB_D_Top.RefH.Capas
            Seccion(i / 2).EB_D_Bot.RefH.Acero = Seccion(i / 2).EB_D_Top.RefH.Acero
            Seccion(i / 2).EB_D_Bot.RefH.Separacion = Seccion(i / 2).EB_D_Top.RefH.Separacion
            Seccion(i / 2).EB_D_Bot.RefH.Acero_T = Seccion(i / 2).EB_D_Top.RefH.Acero_T
            Seccion(i / 2).EB_D_Bot.Cuantia_L = (AsT_Bot / (Seccion(i / 2).EB_D_Bot.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100
            If Seccion(i / 2).EB_D_Bot.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_D_Bot.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_D_Bot_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_D_Bot_NoEsp = False
            End If
        Next

        proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Ref_Modificado_Muros = True

        If proyecto.Muros.Lista_Muros.FindIndex(Function(p) p.Name = Elemento) < Combo_Elementos.Items.Count - 1 Then
            Combo_Elementos.Text = proyecto.Muros.Lista_Muros(proyecto.Muros.Lista_Muros.FindIndex(Function(p) p.Name = Elemento) + 1).Name
        Else
            MessageBox.Show("Hecho.", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If

        'Catch ex As Exception

        'End Try

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Try
            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1

                Dim Elemento = proyecto.Muros.Lista_Muros(i)

                For j = 0 To proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1
                    Dim seccion = proyecto.Muros.Lista_Muros(i).Lista_Secciones(j)

                    '----- Verificación Cuantias minimas ---------
                    seccion.fy = 420
                    Dim Ver_Cuantia = Funcion_CorteLimite(seccion.tw_Planos, seccion.Lw_Planos, seccion.fc, seccion.Vu)
                    seccion.Rho_Min_L = Ver_Cuantia(0)
                    seccion.Rho_Min_T = Ver_Cuantia(1)
                    seccion.V_Limite_Cuantia = Ver_Cuantia(2)

                    '------ Verificación a Flexo-Compresión ---------
                    seccion.F_Flexo_Top = Math.Round(seccion.AsT_Top_Col / seccion.As_Top_Req, 2)
                    seccion.F_Flexo_Bot = Math.Round(seccion.AsT_Bot_Col / seccion.As_Bot_Req, 2)

                    '----- Verificación a Cortante ----------
                    Dim Rev_Cortante = Funcion_Cortante(seccion.tw_Planos, seccion.Lw_Planos, seccion.fc, 420, seccion.Rho_H_col, seccion.Vu)
                    seccion.Vc = Rev_Cortante(1)
                    seccion.Vs = Rev_Cortante(2)
                    seccion.Vn = Rev_Cortante(3)
                    seccion.F_Cortante = Rev_Cortante(4)

                    seccion.AsH_Req_Top = seccion.Rho_Min_T * seccion.tw_Planos * 10000

                    '----- Verificacion de los elementos de borde --------------
                    seccion.EB_I_Top.Tipo_EB_Col = ""
                    seccion.EB_I_Bot.Tipo_EB_Col = ""
                    seccion.EB_D_Top.Tipo_EB_Col = ""
                    seccion.EB_D_Bot.Tipo_EB_Col = ""

                    Dim D_Techo As Single = proyecto.Muros.D_Techo_X
                    If seccion.Direccion_Muro = "Y" Then
                        D_Techo = proyecto.Muros.D_Techo_Y
                    End If
                    seccion.C_Limite = EB_C(proyecto.Disipacion, D_Techo, Elemento.Hw, seccion.Lw_Planos)
                    seccion.Chequeo_EB_I_Top_Def = "No requiere"
                    seccion.Chequeo_EB_I_Bot_Def = "No requiere"
                    seccion.Chequeo_EB_D_Top_Def = "No requiere"
                    seccion.Chequeo_EB_D_Bot_Def = "No requiere"

                    seccion.EB_I_Top.Tipo_EB_Req = "No requiere"
                    seccion.EB_I_Bot.Tipo_EB_Req = "No requiere"
                    seccion.EB_D_Top.Tipo_EB_Req = "No requiere"
                    seccion.EB_D_Bot.Tipo_EB_Req = "No requiere"

                    seccion.EB_I_Top.L_EB_Req = 0
                    seccion.EB_I_Bot.L_EB_Req = 0
                    seccion.EB_D_Top.L_EB_Req = 0
                    seccion.EB_D_Bot.L_EB_Req = 0

                    seccion.Req_EB_I_Top_Esp = False
                    seccion.Req_EB_I_Bot_Esp = False
                    seccion.Req_EB_D_Top_Esp = False
                    seccion.Req_EB_D_Bot_Esp = False

                    seccion.Req_EB_I_Top_NoEsp = False
                    seccion.Req_EB_I_Bot_NoEsp = False
                    seccion.Req_EB_D_Top_NoEsp = False
                    seccion.Req_EB_D_Bot_NoEsp = False

                    If seccion.C_Limite / 0.9 < seccion.C_I_Top Then
                        seccion.EB_I_Top.L_EB_Req = Math.Max(seccion.C_I_Top - 0.1 * seccion.Lw_Planos, seccion.C_I_Top / 2)
                        seccion.Chequeo_EB_I_Top_Def = "Requiere"
                        seccion.EB_I_Top.Tipo_EB_Req = "Especial"
                        seccion.Req_EB_I_Top_Esp = True

                        If Elemento.LV_EB_I_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Def = seccion.Altura
                        End If
                    End If

                    If seccion.C_Limite / 0.9 < seccion.C_I_Bot Then
                        seccion.EB_I_Bot.L_EB_Req = Math.Max(seccion.C_I_Bot - 0.1 * seccion.Lw_Planos, seccion.C_I_Bot / 2)
                        seccion.Chequeo_EB_I_Bot_Def = "Requiere"
                        seccion.EB_I_Bot.Tipo_EB_Req = "Especial"
                        seccion.Req_EB_I_Bot_Esp = True

                        If Elemento.LV_EB_I_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Def = seccion.Altura
                        End If
                    End If

                    If seccion.C_Limite / 0.9 < seccion.C_D_Top Then
                        seccion.EB_D_Top.L_EB_Req = Math.Max(seccion.C_D_Top - 0.1 * seccion.Lw_Planos, seccion.C_D_Top / 2)
                        seccion.Chequeo_EB_D_Top_Def = "Requiere"
                        seccion.EB_D_Top.Tipo_EB_Req = "Especial"

                        seccion.Req_EB_D_Top_Esp = True
                        If Elemento.LV_EB_D_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Def = seccion.Altura
                        End If
                    End If

                    If seccion.C_Limite / 0.9 < seccion.C_D_Bot Then
                        seccion.EB_D_Bot.L_EB_Req = Math.Max(seccion.C_D_Bot - 0.1 * seccion.Lw_Planos, seccion.C_D_Bot / 2)
                        seccion.Chequeo_EB_D_Bot_Def = "Requiere"
                        seccion.EB_D_Bot.Tipo_EB_Req = "Especial"

                        seccion.Req_EB_D_Bot_Esp = True
                        If Elemento.LV_EB_D_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Def = seccion.Altura
                        End If
                    End If

                    Dim Rev_EB_Esf_I_Top = EB_Esf(proyecto.Disipacion, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_I_Top)
                    Dim Rev_EB_Esf_I_Bot = EB_Esf(proyecto.Disipacion, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_I_Bot)

                    Dim Rev_EB_Esf_D_Top = EB_Esf(proyecto.Disipacion, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_D_Top)
                    Dim Rev_EB_Esf_D_Bot = EB_Esf(proyecto.Disipacion, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_D_Bot)

                    seccion.Esf_max = Rev_EB_Esf_I_Top(1)
                    seccion.Esf_Lim = Rev_EB_Esf_I_Top(2)

                    seccion.Chequeo_EB_I_Top_Esf = Rev_EB_Esf_I_Top(0)
                    If seccion.Chequeo_EB_I_Top_Esf = "Requiere" Then
                        seccion.EB_I_Top.Tipo_EB_Req = "Especial"
                        seccion.EB_I_Top.L_EB_Req = Math.Max(seccion.C_I_Top - 0.1 * seccion.Lw_Planos, seccion.C_I_Top / 2)
                        seccion.Req_EB_I_Top_Esp = True
                        If Elemento.LV_EB_I_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Esf = seccion.Altura
                        End If
                    End If

                    seccion.Chequeo_EB_I_Bot_Esf = Rev_EB_Esf_I_Bot(0)
                    If seccion.Chequeo_EB_I_Bot_Esf = "Requiere" Then
                        seccion.EB_I_Bot.Tipo_EB_Req = "Especial"
                        seccion.EB_I_Bot.L_EB_Req = Math.Max(seccion.C_I_Bot - 0.1 * seccion.Lw_Planos, seccion.C_I_Bot / 2)
                        seccion.Req_EB_I_Bot_Esp = True
                        If Elemento.LV_EB_I_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Esf = seccion.Altura
                        End If
                    End If

                    seccion.Chequeo_EB_D_Top_Esf = Rev_EB_Esf_D_Top(0)
                    If seccion.Chequeo_EB_D_Top_Esf = "Requiere" Then
                        seccion.EB_D_Top.Tipo_EB_Req = "Especial"
                        seccion.EB_D_Top.L_EB_Req = Math.Max(seccion.C_D_Top - 0.1 * seccion.Lw_Planos, seccion.C_D_Top / 2)
                        seccion.Req_EB_D_Top_Esp = True
                        If Elemento.LV_EB_D_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Esf = seccion.Altura
                        End If
                    End If

                    seccion.Chequeo_EB_D_Bot_Esf = Rev_EB_Esf_D_Bot(0)
                    If seccion.Chequeo_EB_D_Bot_Esf = "Requiere" Then
                        seccion.EB_D_Bot.Tipo_EB_Req = "Especial"
                        seccion.EB_D_Bot.L_EB_Req = Math.Max(seccion.C_D_Bot - 0.1 * seccion.Lw_Planos, seccion.C_D_Bot / 2)
                        seccion.Req_EB_D_Bot_Esp = True
                        If Elemento.LV_EB_D_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Esf = seccion.Altura
                        End If
                    End If

                    Dim Ash_H_EBI_Top = AceroH_EB(proyecto.Disipacion, seccion.EB_I_Top, seccion.tw_Planos, seccion.fc, seccion.fy)
                    Dim Ash_H_EBI_Bot = AceroH_EB(proyecto.Disipacion, seccion.EB_I_Bot, seccion.tw_Planos, seccion.fc, seccion.fy)
                    seccion.EB_I_Top.RefH_Req = Ash_H_EBI_Top(1)
                    seccion.EB_I_Bot.RefH_Req = Ash_H_EBI_Bot(1)

                    Dim Ash_H_EBD_Top = AceroH_EB(proyecto.Disipacion, seccion.EB_D_Top, seccion.tw_Planos, seccion.fc, seccion.fy)
                    Dim Ash_H_EBD_Bot = AceroH_EB(proyecto.Disipacion, seccion.EB_D_Bot, seccion.tw_Planos, seccion.fc, seccion.fy)
                    seccion.EB_D_Top.RefH_Req = Ash_H_EBD_Top(1)
                    seccion.EB_D_Bot.RefH_Req = Ash_H_EBD_Bot(1)

                    If 0 < seccion.EB_I_Top.RefH.Separacion And seccion.EB_I_Top.RefH.Separacion <= Ash_H_EBI_Top(0) / 1000 Then
                        seccion.EB_I_Top.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_I_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_I_Top.RefH.Separacion And seccion.EB_I_Top.RefH.Separacion > 0 Then
                        seccion.EB_I_Top.Tipo_EB_Col = "No especial"
                        If seccion.EB_I_Top.Tipo_EB_Req = "No especial" Then
                            seccion.EB_I_Top.RefH_Req = (Math.Round(seccion.EB_I_Top.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_I_Top.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_I_Top.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_I_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If 0 < seccion.EB_I_Bot.RefH.Separacion And seccion.EB_I_Bot.RefH.Separacion <= Ash_H_EBI_Bot(0) / 1000 Then
                        seccion.EB_I_Bot.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_I_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_I_Bot.RefH.Separacion And seccion.EB_I_Bot.RefH.Separacion > 0 Then
                        seccion.EB_I_Bot.Tipo_EB_Col = "No especial"
                        If seccion.EB_I_Bot.Tipo_EB_Req = "No especial" Then
                            seccion.EB_I_Bot.RefH_Req = (Math.Round(seccion.EB_I_Bot.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_I_Bot.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_I_Bot.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_I_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If 0 < seccion.EB_D_Top.RefH.Separacion And seccion.EB_D_Top.RefH.Separacion <= Ash_H_EBD_Top(0) / 1000 Then
                        seccion.EB_D_Top.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_D_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_D_Top.RefH.Separacion And seccion.EB_D_Top.RefH.Separacion > 0 Then
                        seccion.EB_D_Top.Tipo_EB_Col = "No especial"
                        If seccion.EB_D_Top.Tipo_EB_Req = "No especial" Then
                            seccion.EB_D_Top.RefH_Req = (Math.Round(seccion.EB_D_Top.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_D_Top.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_D_Top.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_D_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If 0 < seccion.EB_D_Bot.RefH.Separacion And seccion.EB_D_Bot.RefH.Separacion <= Ash_H_EBD_Bot(0) / 1000 Then
                        seccion.EB_D_Bot.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_D_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_D_Bot.RefH.Separacion And seccion.EB_D_Bot.RefH.Separacion > 0 Then
                        seccion.EB_D_Bot.Tipo_EB_Col = "No especial"
                        If seccion.EB_D_Bot.Tipo_EB_Req = "No especial" Then
                            seccion.EB_D_Bot.RefH_Req = (Math.Round(seccion.EB_D_Bot.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_D_Bot.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_D_Bot.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_D_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If seccion.Req_EB_I_Top_Esp = False And seccion.EB_I_Top.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_I_Top_NoEsp = True
                        seccion.EB_I_Top.Tipo_EB_Req = "No especial"
                        seccion.EB_I_Top.RefH_Req = (Math.Round(seccion.EB_I_Top.L_EB / 0.35, 0) + 1) * 32
                    End If
                    If seccion.Req_EB_I_Bot_Esp = False And seccion.EB_I_Bot.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_I_Bot_NoEsp = True
                        seccion.EB_I_Bot.Tipo_EB_Req = "No especial"
                        seccion.EB_I_Bot.RefH_Req = (Math.Round(seccion.EB_I_Bot.L_EB / 0.35, 0) + 1) * 32
                    End If

                    If seccion.Req_EB_D_Top_Esp = False And seccion.EB_D_Top.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_D_Top_NoEsp = True
                        seccion.EB_D_Top.Tipo_EB_Req = "No especial"
                        seccion.EB_D_Top.RefH_Req = (Math.Round(seccion.EB_D_Top.L_EB / 0.35, 0) + 1) * 32
                    End If
                    If seccion.Req_EB_D_Bot_Esp = False And seccion.EB_D_Bot.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_D_Bot_NoEsp = True
                        seccion.EB_D_Bot.Tipo_EB_Req = "No especial"
                        seccion.EB_D_Bot.RefH_Req = (Math.Round(seccion.EB_D_Bot.L_EB / 0.35, 0) + 1) * 32
                    End If
                Next

            Next

        Catch ex As Exception

        Finally
            If proyecto.Muros.Lista_Muros.Count > 0 Then
                For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                    Form_06_01_ResultadosMuros.Combo_Elementos.Items.Add(proyecto.Muros.Lista_Muros(i).Label)
                Next
                Form_06_01_ResultadosMuros.Combo_Elementos.Text = proyecto.Muros.Lista_Muros(0).Label
            End If

            Form_06_01_ResultadosMuros.Show()
            Form_06_01_ResultadosMuros.WindowState = FormWindowState.Maximized

        End Try

    End Sub

    Private Sub Op_SeccionSimilar_CheckedChanged(sender As Object, e As EventArgs) Handles Op_SeccionSimilar.CheckedChanged
        'Dim Secciones_Principales = proyecto.Muros.Lista_Muros.FindAll(Function(p) p.Secciones_Principal = True)
        C_Lista_Secciones_Principales.Items.Clear()

        If Op_SeccionPrincipal.Checked = False Then
            'If Secciones_Principales.Count < 1 Then
            '    '    MessageBox.Show("No se tiene registro de ninguna sección", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Error)
            '    '    Op_SeccionPrincipal.Checked = True
            '    'Else
            '    '    For i = 0 To Secciones_Principales.Count - 1
            '    '        C_Lista_Secciones_Principales.Items.Add(Secciones_Principales(i).Name_Label)
            '    '    Next
            '    '    Form_02_PagColumnas.Proyecto.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Similar = True
            '    '    Form_02_PagColumnas.Proyecto.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Principal = False
            '    '    C_Lista_Secciones_Principales.Enabled = True
            '    'End If
            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                C_Lista_Secciones_Principales.Items.Add(proyecto.Muros.Lista_Muros(i).Label)
            Next

            C_Lista_Secciones_Principales.Enabled = True
        End If
    End Sub

    Private Sub C_Lista_Secciones_Principales_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_Lista_Secciones_Principales.SelectedIndexChanged

        If Op_SeccionSimilar.Checked = True Then

            Tabla_Info_Seccion.Rows.Clear()
            Tabla_Info_EBorde.Rows.Clear()

            Dim Elemento = proyecto.Muros.Lista_Muros.Find(Function(p) p.Label = C_Lista_Secciones_Principales.Text)
            Dim Seccion = proyecto.Muros.Lista_Muros.Find(Function(p) p.Label = C_Lista_Secciones_Principales.Text).Lista_Secciones

            For i = 0 To (Seccion.Count - 1) * 2 + 1
                Tabla_Info_Seccion.Rows.Add()
                Tabla_Info_EBorde.Rows.Add()
            Next
            Tabla_Info_Seccion.Refresh()
            Tabla_Info_EBorde.Refresh()


            For i = 0 To ((Seccion.Count - 1) * 2) Step 2
                Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

                If Seccion(i / 2).S_Patron = False Then
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "None"
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "Si"
                End If

                Tabla_Info_EBorde.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_EBorde.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_EBorde.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_EBorde.Rows(i + 1).Cells(4).Value = "Bottom"

                If proyecto.Muros.Lista_Muros.Find(Function(p) p.Label = C_Lista_Secciones_Principales.Text).Ref_Modificado_Muros = False Then

                    For j = 7 To 14

                        Tabla_Info_Seccion.Rows(i).Cells(j).Value = 0
                        Tabla_Info_Seccion.Rows(i + 1).Cells(j).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 1).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 1).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 14).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 14).Value = 0

                    Next

                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = ""
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = 0

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = "0"
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = "0"

                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(6).Value = ""
                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(18).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(20).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = 0

                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(19).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(32).Value = "None"

                Else

                    Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_2
                    Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_3
                    Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_4
                    Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_5
                    Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_6
                    Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_7
                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_8
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_10

                    Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_2
                    Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_3
                    Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_4
                    Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_5
                    Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_6
                    Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_7
                    Tabla_Info_Seccion.Rows(i + 1).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_8
                    Tabla_Info_Seccion.Rows(i + 1).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_10

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Malla.Malla
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).Malla.Capas)
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = Seccion(i / 2).RefH_W_Col.Acero
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = Seccion(i / 2).RefH_W_Col.Separacion
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = Convert.ToString(Seccion(i / 2).RefH_W_Col.Capas)

                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = Seccion(i / 2).EB_I_Top.L_EB
                    Tabla_Info_EBorde.Rows(i).Cells(8).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(9).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(10).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(11).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(12).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(13).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(14).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(15).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(8).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(9).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(10).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(11).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(12).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(13).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(14).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(15).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).EB_I_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = Seccion(i / 2).EB_I_Top.RefH.Acero
                    Tabla_Info_EBorde.Rows(i).Cells(18).Value = Seccion(i / 2).EB_I_Top.RefH.Separacion

                    Tabla_Info_EBorde.Rows(i).Cells(20).Value = Seccion(i / 2).EB_D_Top.L_EB

                    Tabla_Info_EBorde.Rows(i).Cells(21).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(22).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(23).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(24).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(25).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(26).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(27).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(28).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(21).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(22).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(23).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(24).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(25).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(26).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(27).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(28).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = Convert.ToString(Seccion(i / 2).EB_D_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = Seccion(i / 2).EB_D_Top.RefH.Acero
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = Seccion(i / 2).EB_D_Top.RefH.Separacion

                End If
            Next


        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As System.Windows.Forms.Message, keyData As System.Windows.Forms.Keys) As Boolean

        ' Verificamos si el control que tiene el foco es un DataGridView.
        Dim focusedControl = Me.ActiveControl

        If TypeOf focusedControl Is DataGridView Then
            Dim dataGridView As DataGridView = DirectCast(focusedControl, DataGridView)

            ' Comprobamos si se ha pulsado la combinación de teclas Ctrl + V.
            If (keyData = (Keys.Control Or Keys.V)) Then
                ' Comprobamos si el contenido del portapapeles es texto.
                If Clipboard.ContainsText() Then
                    ' Recorremos las celdas seleccionadas y pegamos el texto del portapapeles en cada una de ellas.
                    For Each celda As DataGridViewCell In dataGridView.SelectedCells
                        celda.Value = Clipboard.GetText()
                    Next

                    ' Indicamos que hemos manejado la combinación de teclas.
                    Return True
                End If
            End If
        End If

        ' Si no se cumple ninguna de las condiciones anteriores, pasamos al procesamiento predeterminado de comandos.
        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function


End Class