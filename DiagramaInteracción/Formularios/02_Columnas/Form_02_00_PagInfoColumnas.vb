Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_02_Columnas
Public Class Form_02_00_PagInfoColumnas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged

        Try
            Tabla_Info_Seccion.Rows.Clear()

            Dim col As Columna = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text)
            If col Is Nothing Then Return
            Dim Elemento As String = col.Name_Elemento
            Dim Seccion = col.Lista_Tramos_Columnas

            For i = 0 To (Seccion.Count - 1) * 2
                Tabla_Info_Seccion.Rows.Add()
            Next

            For i = 0 To (Seccion.Count - 1) * 2 Step 2
                Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).B_Plano
                Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

                If Seccion(i / 2).EsCircular Then
                    ' Columna circular: la columna 3 (Alto) no aplica — mostrar Ø en columna 2
                    Tabla_Info_Seccion.Rows(i).Cells(3).Value = Nothing
                    Tabla_Info_Seccion.Rows(i).Cells(3).ReadOnly = True
                    Tabla_Info_Seccion.Rows(i).Cells(3).Style.BackColor = Color.FromArgb(215, 215, 215)
                    Tabla_Info_Seccion.Rows(i).Cells(3).Style.ForeColor = Color.FromArgb(165, 165, 165)
                    Tabla_Info_Seccion.Rows(i + 1).Cells(3).ReadOnly = True
                    Tabla_Info_Seccion.Rows(i + 1).Cells(3).Style.BackColor = Color.FromArgb(215, 215, 215)
                    ' Marcar columna 2 como "Ø" con color distintivo
                    Tabla_Info_Seccion.Rows(i).Cells(2).Style.BackColor = Color.FromArgb(210, 230, 255)
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).H_Plano
                End If

                If Not col.Ref_Modificado Then
                    For j = 5 To 12
                        Tabla_Info_Seccion.Rows(i).Cells(j).Value = 0
                        Tabla_Info_Seccion.Rows(i + 1).Cells(j).Value = 0
                    Next
                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = If(Seccion(i / 2).EsCircular, 1, 0)
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = 0
                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = "#3"
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = AreaRefuerzo("#3")
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = AreaRefuerzo("#3")
                    Dim dimRef As Single = If(Seccion(i / 2).EsCircular, Seccion(i / 2).Diametro, Math.Min(Seccion(i / 2).B_Plano, Seccion(i / 2).H_Plano))
                    Dim sZC_new As Single = Math.Round(dimRef / 4, 3)
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = sZC_new
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = Math.Round(2 * sZC_new, 3)
                    Tabla_Info_Seccion.Rows(i).Cells(20).Value = 0
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_2
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_3
                    Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_4
                    Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_5
                    Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_6
                    Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_7
                    Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_8
                    Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Col_Top.Barras_10

                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2
                    Tabla_Info_Seccion.Rows(i + 1).Cells(6).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3
                    Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4
                    Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5
                    Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6
                    Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7
                    Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8
                    Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10

                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Num_Ramas_Largo
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Num_Ramas_Corto
                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Numero_Barras_Estribo
                    Dim asSentL As Single = Seccion(i / 2).As_Sent_Largo
                    If asSentL = 0 AndAlso Not String.IsNullOrEmpty(Seccion(i / 2).Numero_Barras_Estribo) AndAlso Seccion(i / 2).Numero_Barras_Estribo <> "User" Then
                        asSentL = AreaRefuerzo(Seccion(i / 2).Numero_Barras_Estribo)
                    End If
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = asSentL
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = Seccion(i / 2).As_Sent_Corto
                    Dim sZC_sug As Single = Math.Round(Math.Min(Seccion(i / 2).B_Plano, Seccion(i / 2).H_Plano) / 3, 3)
                    Dim sZC_val As Single = If(Seccion(i / 2).Separacion_Estribos > 0, Seccion(i / 2).Separacion_Estribos, sZC_sug)
                    Dim sZNC_val As Single = If(Seccion(i / 2).Separacion_Estribos_ZNC > 0, Seccion(i / 2).Separacion_Estribos_ZNC, Math.Round(2 * sZC_sug, 3))
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = sZC_val
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = sZNC_val
                    Tabla_Info_Seccion.Rows(i).Cells(20).Value = Seccion(i / 2).Num_Estribos_ZC
                End If
            Next

        Catch ex As Exception
        Finally
            T_Seccion.Text = Combo_Elementos.Text
        End Try

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Try
        Dim colActual As Columna = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text)
        If colActual Is Nothing Then Return
        Dim Seccion = colActual.Lista_Tramos_Columnas
        Dim Elemento As String = colActual.Name_Elemento
        colActual.Name_Label = T_Seccion.Text

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Seccion(i / 2).fc = Tabla_Info_Seccion.Rows(i).Cells(1).Value
            Seccion(i / 2).B_Plano = Tabla_Info_Seccion.Rows(i).Cells(2).Value
            If Seccion(i / 2).EsCircular Then
                ' Para circular: alto = diámetro (mismo valor)
                Seccion(i / 2).H_Plano = Seccion(i / 2).B_Plano
                Seccion(i / 2).Diametro = Seccion(i / 2).B_Plano
            Else
                Seccion(i / 2).H_Plano = Tabla_Info_Seccion.Rows(i).Cells(3).Value
            End If

            Seccion(i / 2).Refuerzo_Col_Top.Barras_2 = Tabla_Info_Seccion.Rows(i).Cells(5).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_3 = Tabla_Info_Seccion.Rows(i).Cells(6).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_4 = Tabla_Info_Seccion.Rows(i).Cells(7).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_5 = Tabla_Info_Seccion.Rows(i).Cells(8).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_6 = Tabla_Info_Seccion.Rows(i).Cells(9).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_7 = Tabla_Info_Seccion.Rows(i).Cells(10).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_8 = Tabla_Info_Seccion.Rows(i).Cells(11).Value
            Seccion(i / 2).Refuerzo_Col_Top.Barras_10 = Tabla_Info_Seccion.Rows(i).Cells(12).Value

            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2 = Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3 = Tabla_Info_Seccion.Rows(i + 1).Cells(6).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4 = Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5 = Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6 = Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7 = Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8 = Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value
            Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10 = Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value

            Seccion(i / 2).As_Col_Top =
                AreaRefuerzo("#2") * Seccion(i / 2).Refuerzo_Col_Top.Barras_2 +
                AreaRefuerzo("#3") * Seccion(i / 2).Refuerzo_Col_Top.Barras_3 +
                AreaRefuerzo("#4") * Seccion(i / 2).Refuerzo_Col_Top.Barras_4 +
                AreaRefuerzo("#5") * Seccion(i / 2).Refuerzo_Col_Top.Barras_5 +
                AreaRefuerzo("#6") * Seccion(i / 2).Refuerzo_Col_Top.Barras_6 +
                AreaRefuerzo("#7") * Seccion(i / 2).Refuerzo_Col_Top.Barras_7 +
                AreaRefuerzo("#8") * Seccion(i / 2).Refuerzo_Col_Top.Barras_8 +
                AreaRefuerzo("#10") * Seccion(i / 2).Refuerzo_Col_Top.Barras_10

            Seccion(i / 2).As_Col_Bottom =
                AreaRefuerzo("#2") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2 +
                AreaRefuerzo("#3") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3 +
                AreaRefuerzo("#4") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4 +
                AreaRefuerzo("#5") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5 +
                AreaRefuerzo("#6") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6 +
                AreaRefuerzo("#7") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7 +
                AreaRefuerzo("#8") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8 +
                AreaRefuerzo("#10") * Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10

            Seccion(i / 2).Cantidad_Barras_Top = Seccion(i / 2).Refuerzo_Col_Top.Barras_2 + Seccion(i / 2).Refuerzo_Col_Top.Barras_3 + Seccion(i / 2).Refuerzo_Col_Top.Barras_4 + Seccion(i / 2).Refuerzo_Col_Top.Barras_5 + Seccion(i / 2).Refuerzo_Col_Top.Barras_6 + Seccion(i / 2).Refuerzo_Col_Top.Barras_7 + Seccion(i / 2).Refuerzo_Col_Top.Barras_8 + Seccion(i / 2).Refuerzo_Col_Top.Barras_10
            Seccion(i / 2).Cantidad_Barras_Bottom = Seccion(i / 2).Refuerzo_Col_Bottom.Barras_2 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_3 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_4 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_5 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_6 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_7 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_8 + Seccion(i / 2).Refuerzo_Col_Bottom.Barras_10

            ' Cuantía — área bruta diferente para circular
            Dim Ag_sec As Single = If(Seccion(i / 2).EsCircular,
                                      CSng(Math.PI * Seccion(i / 2).Diametro ^ 2 / 4),
                                      Seccion(i / 2).B_Plano * Seccion(i / 2).H_Plano)
            Seccion(i / 2).Cuantia_Col_Top    = If(Ag_sec > 0, Seccion(i / 2).As_Col_Top    / (Ag_sec * 1000000), 0)
            Seccion(i / 2).Cuantia_Col_Bottom = If(Ag_sec > 0, Seccion(i / 2).As_Col_Bottom / (Ag_sec * 1000000), 0)

            Dim As_Equivalente_Top As Single = If(Seccion(i / 2).Cantidad_Barras_Top > 0, Seccion(i / 2).As_Col_Top / Seccion(i / 2).Cantidad_Barras_Top, 0)
            Dim As_Equivalente_Bottom As Single = If(Seccion(i / 2).Cantidad_Barras_Bottom > 0, Seccion(i / 2).As_Col_Bottom / Seccion(i / 2).Cantidad_Barras_Bottom, 0)

            Seccion(i / 2).Lista_Detalles_Refuerzo_Top.Clear()
            Seccion(i / 2).Lista_Detalles_Refuerzo_Bottom.Clear()

            If Seccion(i / 2).EsCircular Then
                ' Sección circular: distribuir barras en corona
                Seccion(i / 2).TipoTransversal = "Espiral"
                Dim N_Top As Integer = Seccion(i / 2).Cantidad_Barras_Top
                Dim Coords_Top = DistribuirBarrasEnCirculo(Seccion(i / 2).Diametro, 0.05F, N_Top)
                For j = 0 To N_Top - 1
                    Seccion(i / 2).Lista_Detalles_Refuerzo_Top.Add(
                        New Tramo_Columna.Detalles_Refuerzo_Longitudinal() With {
                            .Name_Barra = j + 1, .Asb = As_Equivalente_Top,
                            .Db = Math.Sqrt(4 * As_Equivalente_Top / Math.PI),
                            .Coordenada_X = Coords_Top(j, 1), .Coordenada_Y = Coords_Top(j, 2)})
                Next
                Dim N_Bot As Integer = Seccion(i / 2).Cantidad_Barras_Bottom
                Dim Coords_Bot = DistribuirBarrasEnCirculo(Seccion(i / 2).Diametro, 0.05F, N_Bot)
                For j = 0 To N_Bot - 1
                    Seccion(i / 2).Lista_Detalles_Refuerzo_Bottom.Add(
                        New Tramo_Columna.Detalles_Refuerzo_Longitudinal() With {
                            .Name_Barra = j + 1, .Asb = As_Equivalente_Bottom,
                            .Db = Math.Sqrt(4 * As_Equivalente_Bottom / Math.PI),
                            .Coordenada_X = Coords_Bot(j, 1), .Coordenada_Y = Coords_Bot(j, 2)})
                Next
            Else
                ' ----- Barras Top (rectangular) -----
                Dim N_Top As Integer = Seccion(i / 2).Cantidad_Barras_Top
                Dim Coords_Top = DistribuirBarrasConEsquinas(Seccion(i / 2).B_Plano, Seccion(i / 2).H_Plano, N_Top)
                For j = 0 To N_Top - 1
                    Seccion(i / 2).Lista_Detalles_Refuerzo_Top.Add(
                        New Tramo_Columna.Detalles_Refuerzo_Longitudinal() With {
                            .Name_Barra = j + 1, .Asb = As_Equivalente_Top,
                            .Db = Math.Sqrt(4 * As_Equivalente_Top / Math.PI),
                            .Coordenada_X = Coords_Top(j, 1), .Coordenada_Y = Coords_Top(j, 2)})
                Next
                ' ----- Barras Bottom (rectangular) -----
                Dim N_Bot As Integer = Seccion(i / 2).Cantidad_Barras_Bottom
                Dim Coords_Bot = DistribuirBarrasConEsquinas(Seccion(i / 2).B_Plano, Seccion(i / 2).H_Plano, N_Bot)
                For j = 0 To N_Bot - 1
                    Seccion(i / 2).Lista_Detalles_Refuerzo_Bottom.Add(
                        New Tramo_Columna.Detalles_Refuerzo_Longitudinal() With {
                            .Name_Barra = j + 1, .Asb = As_Equivalente_Bottom,
                            .Db = Math.Sqrt(4 * As_Equivalente_Bottom / Math.PI),
                            .Coordenada_X = Coords_Bot(j, 1), .Coordenada_Y = Coords_Bot(j, 2)})
                Next
            End If

            ' Limpiar distribución personalizada si el usuario vuelve a guardar (se regenerará)
            Seccion(i / 2).Distribucion_Personalizada = False
            Seccion(i / 2).Lista_Barras_Seccion.Clear()

            For j = 5 To 12
                If Tabla_Info_Seccion.Rows(i).Cells(j).Value > 0 And j <= 11 Then
                    Seccion(i / 2).Barra_Long_Min = Convert.ToString("#" & j - 3)
                    Exit For
                Else
                    Seccion(i / 2).Barra_Long_Min = Convert.ToString("#10")
                End If
            Next

            Seccion(i / 2).Num_Ramas_Largo = Tabla_Info_Seccion.Rows(i).Cells(13).Value
            Seccion(i / 2).Num_Ramas_Corto = Tabla_Info_Seccion.Rows(i).Cells(14).Value
            Seccion(i / 2).Numero_Barras_Estribo = Tabla_Info_Seccion.Rows(i).Cells(15).Value

            Dim area_ref_var As Single = 0
            Dim area_var_Largo As Single = 0
            Dim area_var_Corto As Single = 0
            If Seccion(i / 2).Numero_Barras_Estribo = "User" Then
                area_ref_var = Convert.ToSingle(If(Tabla_Info_Seccion.Rows(i).Cells(16).Value, 0))
                area_var_Largo = Convert.ToSingle(If(Tabla_Info_Seccion.Rows(i).Cells(16).Value, 0))
                area_var_Corto = Convert.ToSingle(If(Tabla_Info_Seccion.Rows(i).Cells(17).Value, 0))
            Else
                area_ref_var = AreaRefuerzo(Seccion(i / 2).Numero_Barras_Estribo)
                area_var_Largo = area_ref_var
                area_var_Corto = area_ref_var
            End If

            Seccion(i / 2).Separacion_Estribos = Tabla_Info_Seccion.Rows(i).Cells(18).Value
            Seccion(i / 2).Separacion_Estribos_ZNC = Tabla_Info_Seccion.Rows(i).Cells(19).Value

            Seccion(i / 2).As_Sent_Largo = area_var_Largo
            Seccion(i / 2).As_Sent_Corto = area_var_Corto
            Seccion(i / 2).Ash_Col_Corto = Seccion(i / 2).Num_Ramas_Corto * area_var_Corto
            Seccion(i / 2).Ash_Col_Largo = Seccion(i / 2).Num_Ramas_Largo * area_var_Largo
            Seccion(i / 2).Num_Estribos_ZC = Convert.ToInt32(If(Tabla_Info_Seccion.Rows(i).Cells(20).Value IsNot Nothing, Tabla_Info_Seccion.Rows(i).Cells(20).Value, 0))
        Next

        If Op_SeccionPrincipal.Checked = True Then
            colActual.Secciones_Principal = True
        End If

        colActual.Ref_Modificado = True

        ' Refrescar combo para reflejar el nuevo Name_Label y avanzar al siguiente elemento
        Dim currentIdx As Integer = Proyecto.Elementos.Columnas.Lista_Columnas.FindIndex(Function(p) p.Name_Elemento = Elemento)
        RefrescarCombo()
        If currentIdx >= 0 AndAlso currentIdx < Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1 Then
            Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(currentIdx + 1).Name_Label
        Else
            MessageBox.Show("Hecho.", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        'Catch ex As Exception
        'Finally
        Op_SeccionPrincipal.Checked = True
        'End Try

    End Sub

    Private Sub Tabla_Info_Seccion_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_Seccion.CellValueChanged
        For i = 0 To Tabla_Info_Seccion.Rows.Count - 1
            For j = 5 To 12
                If Tabla_Info_Seccion.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_Seccion, i, j)
                End If
            Next
        Next

        Dim fila As Integer = e.RowIndex
        If fila < 0 Then Return
        ' Datos de estribo y separaciones sólo aplican en filas pares (Top)
        If fila Mod 2 <> 0 Then Return

        Select Case e.ColumnIndex
            Case 15  ' # Barra cambia → auto-fill As Sent. Largo y Corto
                Dim barraObj As Object = Tabla_Info_Seccion.Rows(fila).Cells(15).Value
                If barraObj IsNot Nothing Then
                    Dim barra As String = barraObj.ToString()
                    If barra <> "User" AndAlso barra <> "" Then
                        Dim asSent As Single = AreaRefuerzo(barra)
                        Tabla_Info_Seccion.Rows(fila).Cells(16).Value = Math.Round(asSent, 2)
                        Tabla_Info_Seccion.Rows(fila).Cells(17).Value = Math.Round(asSent, 2)
                    End If
                End If

            Case 2, 3  ' Base/Ø o Alto cambia → auto-calc Sep. ZC y ZNC
                Dim bObj As Object = Tabla_Info_Seccion.Rows(fila).Cells(2).Value
                Dim hObj As Object = Tabla_Info_Seccion.Rows(fila).Cells(3).Value
                If bObj IsNot Nothing AndAlso hObj IsNot Nothing Then
                    Dim b As Single, h As Single
                    If Single.TryParse(bObj.ToString(), b) AndAlso Single.TryParse(hObj.ToString(), h) AndAlso b > 0 Then
                        ' Para sección circular: b = Ø, usar D/4; rectangular: min(b,h)/3
                        Dim esCircFila As Boolean = (h = 0 OrElse Math.Abs(b - h) < 0.001)
                        Dim dimMin As Single = If(esCircFila, b, Math.Min(b, h))
                        Dim sZC As Single = Math.Round(dimMin / If(esCircFila, 4, 3), 3)
                        Tabla_Info_Seccion.Rows(fila).Cells(18).Value = sZC
                        Tabla_Info_Seccion.Rows(fila).Cells(19).Value = Math.Round(2 * sZC, 3)
                    End If
                End If
        End Select
    End Sub

    Sub Color_Celda(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer)
        Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 224, 180)
        Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.Red
    End Sub

    Private Sub DataGridView1_CellPainting(sender As System.Object, e As System.Windows.Forms.DataGridViewCellPaintingEventArgs) Handles Tabla_Info_Seccion.CellPainting
        If Tabla_Info_Seccion.Rows.Count > 1 Then
            If e.RowIndex >= 0 Then
                If Tabla_Info_Seccion.Rows(e.RowIndex).Cells(e.ColumnIndex).Value <> Nothing Then
                    If e.ColumnIndex <= 3 Or e.ColumnIndex > 12 Then
                        e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub Form_03_PagInfoColumnas_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        For Each col As DataGridViewColumn In Tabla_Info_Seccion.Columns
            col.SortMode = DataGridViewColumnSortMode.NotSortable
            If col.HeaderText.Contains("Sep.") Then
                col.DefaultCellStyle.Format = "F3"
            End If
        Next

        RefrescarCombo()

        ' Botón independiente para el diagrama biaxial (no afecta flujo existente)
        Dim btnDI As New Button()
        btnDI.Name = "Btn_DiagramaBiaxial"
        btnDI.Text = "Diagrama Biaxial"
        btnDI.Size = New Size(130, Button2.Height)
        btnDI.BackColor = Color.FromArgb(30, 80, 160)
        btnDI.ForeColor = Color.White
        btnDI.FlatStyle = FlatStyle.Flat
        btnDI.FlatAppearance.BorderSize = 0
        btnDI.Top = Button2.Top
        btnDI.Left = Button2.Right + 10
        btnDI.Anchor = AnchorStyles.Bottom
        AddHandler btnDI.Click, AddressOf Btn_DiagramaBiaxial_Click
        Panel1.Controls.Add(btnDI)

        ' Botón para el diagrama de columnas circulares
        Dim btnCirc As New Button()
        btnCirc.Name = "Btn_DiagramaCircular"
        btnCirc.Text = "Diagrama Circular"
        btnCirc.Size = New Size(140, Button2.Height)
        btnCirc.BackColor = Color.FromArgb(100, 60, 160)
        btnCirc.ForeColor = Color.White
        btnCirc.FlatStyle = FlatStyle.Flat
        btnCirc.FlatAppearance.BorderSize = 0
        btnCirc.Top = Button2.Top
        btnCirc.Left = btnDI.Right + 8
        btnCirc.Anchor = AnchorStyles.Bottom
        AddHandler btnCirc.Click, AddressOf Btn_DiagramaCircular_Click
        Panel1.Controls.Add(btnCirc)

    End Sub

    Private Sub Btn_DiagramaBiaxial_Click(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            MessageBox.Show("No hay columnas importadas.", "Diagrama Biaxial",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim frm As New Form_02_02_DiagramaColumna()
        frm.Show()
    End Sub

    Private Sub Btn_DiagramaCircular_Click(sender As Object, e As EventArgs)
        Dim hayCirculares = Proyecto.Elementos.Columnas.Lista_Columnas.Any(
            Function(c) c.Lista_Tramos_Columnas.Any(Function(t) t.EsCircular))
        If Not hayCirculares Then
            MessageBox.Show("No se detectaron columnas circulares en el proyecto." & vbCrLf &
                            "Verifique que haya importado las secciones desde ETABS.",
                            "Sin columnas circulares", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim frm As New Form_02_02_DiagramaCircular()
        frm.Show()
    End Sub

    Public Sub RefrescarCombo()
        Dim textoActual As String = Combo_Elementos.Text
        Combo_Elementos.Items.Clear()
        If Proyecto.Elementos.Columnas.Lista_Columnas.Count > 0 Then
            For Each col In Proyecto.Elementos.Columnas.Lista_Columnas
                Combo_Elementos.Items.Add(col.Name_Label)
            Next
            ' Restaurar la selección actual si todavía existe; si no, ir al primero
            If Combo_Elementos.Items.Contains(textoActual) Then
                Combo_Elementos.Text = textoActual
            Else
                Combo_Elementos.Text = Proyecto.Elementos.Columnas.Lista_Columnas(0).Name_Label
            End If
        End If
    End Sub

    Private Sub Form_03_PagInfoColumnas_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize

        Button1.Left = (Panel1.Width - Button1.Width) / 2 - 0.6 * Button1.Width
        Button2.Left = (Panel1.Width - Button2.Width) / 2 + 0.6 * Button2.Width

        Label1.Left = (Panel1.Width - Label1.Width) / 2

        Dim ctls = Panel1.Controls.Find("Btn_DiagramaBiaxial", False)
        If ctls.Length > 0 Then ctls(0).Left = Button2.Right + 10
        Dim ctlsCirc = Panel1.Controls.Find("Btn_DiagramaCircular", False)
        If ctlsCirc.Length > 0 AndAlso ctls.Length > 0 Then ctlsCirc(0).Left = ctls(0).Right + 8

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim colsConRefuerzo = Proyecto.Elementos.Columnas.Lista_Columnas.Where(Function(c) c.Ref_Modificado).ToList()
        Dim cantOmitidas As Integer = Proyecto.Elementos.Columnas.Lista_Columnas.Count - colsConRefuerzo.Count

        If colsConRefuerzo.Count = 0 Then
            MessageBox.Show("Ninguna columna tiene refuerzo definido. Ingrese el refuerzo de al menos una columna antes de calcular.",
                            "Sin Refuerzo Definido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Repoblar combos únicamente con las columnas que se van a calcular
        Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Clear()
        Form_02_01_02_ResultadosModelo.Combo_Elementos.Items.Clear()
        Form_02_01_00_RevisionCortante.Combo_Elementos.Items.Clear()

        For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
            Dim col = Proyecto.Elementos.Columnas.Lista_Columnas(i)

            ' Omitir columnas sin refuerzo definido
            If Not col.Ref_Modificado Then Continue For

            Dim Lista(3, 2) : Lista(1, 1) = 100 : Lista(2, 1) = 100 : Lista(3, 1) = 100
            col.Lista_F.Clear()
            col.Lista_F_Piso.Clear()

            Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Add(col.Name_Label)
            Form_02_01_02_ResultadosModelo.Combo_Elementos.Items.Add(col.Name_Label)
            Form_02_01_00_RevisionCortante.Combo_Elementos.Items.Add(col.Name_Label)

            For j = 0 To col.Lista_Tramos_Columnas.Count - 1
                Dim Seccion = col.Lista_Tramos_Columnas(j)

                '--- Verificación a Flexo-Compresión -------
                Seccion.F_Flexo_Top = Math.Round(Seccion.As_Col_Top / Seccion.As_Req_Top, 2)
                Seccion.F_Flexo_Bottom = Math.Round(Seccion.As_Col_Bottom / Seccion.As_Req_Bottom, 2)

                '---- Verificación a Cortante -------
                If Seccion.EsCircular Then
                    Dim Rev_Cortante = FuncionCortanteCircular(Seccion.Diametro, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Numero_Barras_Estribo, Math.Abs(Seccion.V2), Math.Abs(Seccion.V3), Math.Abs(Seccion.Pu_V2), Seccion.As_Sent_Largo)
                    Seccion.Vc_2 = Rev_Cortante(1) : Seccion.Vs_2 = Rev_Cortante(2)
                    Seccion.Vn_2 = Rev_Cortante(3) : Seccion.Vu_2 = Rev_Cortante(4) : Seccion.F_Cortante_2 = Rev_Cortante(5)
                    Seccion.Vc_3 = Rev_Cortante(1) : Seccion.Vs_3 = Rev_Cortante(2)
                    Seccion.Vn_3 = Rev_Cortante(3) : Seccion.Vu_3 = Rev_Cortante(4) : Seccion.F_Cortante_3 = Rev_Cortante(5)
                Else
                    Dim Rev_Cortante_L = FuncionCortante(Seccion.B_Plano, Seccion.H_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Numero_Barras_Estribo, Seccion.Num_Ramas_Largo, Math.Abs(Seccion.V2), Math.Abs(Seccion.Pu_V2), Seccion.As_Sent_Largo)
                    Dim Rev_Cortante_C = FuncionCortante(Seccion.H_Plano, Seccion.B_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Numero_Barras_Estribo, Seccion.Num_Ramas_Corto, Math.Abs(Seccion.V3), Math.Abs(Seccion.Pu_V3), Seccion.As_Sent_Corto)
                    Seccion.Vc_2 = Rev_Cortante_L(1) : Seccion.Vs_2 = Rev_Cortante_L(2)
                    Seccion.Vn_2 = Rev_Cortante_L(3) : Seccion.Vu_2 = Rev_Cortante_L(4) : Seccion.F_Cortante_2 = Rev_Cortante_L(5)
                    Seccion.Vc_3 = Rev_Cortante_C(1) : Seccion.Vs_3 = Rev_Cortante_C(2)
                    Seccion.Vn_3 = Rev_Cortante_C(3) : Seccion.Vu_3 = Rev_Cortante_C(4) : Seccion.F_Cortante_3 = Rev_Cortante_C(5)
                End If

                '------ Verificación al Confinamiento ------
                If Seccion.EsCircular Then
                    Dim Rev_Conf = FuncionConfinamientoCircular(Seccion.Diametro, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Barra_Long_Min, Seccion.Numero_Barras_Estribo, "DMO", Proyecto.Elementos.Columnas.Trans_Circular)
                    Seccion.Ash_L = Rev_Conf(1) : Seccion.Ramas_Req_L = Rev_Conf(2) : Seccion.S0_L = Rev_Conf(3) : Seccion.L0_L = Rev_Conf(4)
                    Seccion.Ash_C = Rev_Conf(1) : Seccion.Ramas_Req_C = Rev_Conf(2) : Seccion.S0_C = Rev_Conf(3) : Seccion.L0_C = Rev_Conf(4)
                    ' Ash total = estribo circular principal + ganchos adicionales (Ramas Sentido Corto)
                    Dim aspReal As Single = Seccion.Ash_Col_Largo + Seccion.Ash_Col_Corto
                    Seccion.F_Ash_Largo = Math.Round(If(Seccion.Ash_L > 0, aspReal / Seccion.Ash_L, 100), 2)
                    Seccion.F_Ash_Corto = Seccion.F_Ash_Largo
                Else
                    Dim Rev_Confinamiento_L = FuncionConfinamiento(Seccion.B_Plano, Seccion.H_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Barra_Long_Min, Seccion.Numero_Barras_Estribo, "DMO")
                    Dim Rev_Confinamiento_C = FuncionConfinamiento(Seccion.H_Plano, Seccion.B_Plano, Seccion.fc, 420, Seccion.Separacion_Estribos, Seccion.Barra_Long_Min, Seccion.Numero_Barras_Estribo, "DMO")
                    Seccion.Ash_L = Rev_Confinamiento_L(1) : Seccion.Ramas_Req_L = Rev_Confinamiento_L(2) : Seccion.S0_L = Rev_Confinamiento_L(3) : Seccion.L0_L = Rev_Confinamiento_L(4)
                    Seccion.Ash_C = Rev_Confinamiento_C(1) : Seccion.Ramas_Req_C = Rev_Confinamiento_C(2) : Seccion.S0_C = Rev_Confinamiento_C(3) : Seccion.L0_C = Rev_Confinamiento_C(4)
                    Seccion.F_Ash_Largo = Math.Round(Seccion.Ash_Col_Largo / Seccion.Ash_L, 2)
                    Seccion.F_Ash_Corto = Math.Round(Seccion.Ash_Col_Corto / Seccion.Ash_C, 2)
                End If

                ' D/C biaxial (diagrama de interacción)
                Dim combosD = Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
                If Seccion.EsCircular Then
                    Dim okTop = FuncionDiagramaColumnaCircular(Seccion, 420.0F, 200000.0F, "Top", combosD)
                    Dim dcTop As Single = If(okTop, Seccion.F_Interaccion, 0)
                    FuncionDiagramaColumnaCircular(Seccion, 420.0F, 200000.0F, "Bottom", combosD)
                    If dcTop > Seccion.F_Interaccion Then Seccion.F_Interaccion = dcTop
                ElseIf Seccion.Distribucion_Personalizada Then
                    FuncionDiagramaColumna(Seccion, 420.0F, 200000.0F, "Top", combosD)
                Else
                    Dim okTop = FuncionDiagramaColumna(Seccion, 420.0F, 200000.0F, "Top", combosD)
                    Dim dcTop As Single = If(okTop, Seccion.F_Interaccion, 0)
                    FuncionDiagramaColumna(Seccion, 420.0F, 200000.0F, "Bottom", combosD)
                    If dcTop > Seccion.F_Interaccion Then Seccion.F_Interaccion = dcTop
                End If

                Seccion.L0_Prov = Math.Round(0.05 + Seccion.Num_Estribos_ZC * Seccion.Separacion_Estribos + 0.05, 3)
                Dim l0Req As Single = Math.Max(Seccion.L0_L, Seccion.L0_C)
                If l0Req > 0 Then Seccion.F_L0 = Math.Round(Seccion.L0_Prov / l0Req, 2)

                If Lista(1, 1) > Seccion.F_Flexo_Top Then
                    Lista(1, 1) = Seccion.F_Flexo_Top
                    Lista(1, 2) = Seccion.Piso
                End If
                If Lista(1, 1) > Seccion.F_Flexo_Bottom Then
                    Lista(1, 1) = Seccion.F_Flexo_Bottom
                    Lista(1, 2) = Seccion.Piso
                End If

                If Lista(2, 1) > Seccion.F_Cortante_2 Then
                    Lista(2, 1) = Seccion.F_Cortante_2
                    Lista(2, 2) = Seccion.Piso
                End If

                If Lista(3, 1) > Seccion.F_Cortante_3 Then
                    Lista(3, 1) = Seccion.F_Cortante_3
                    Lista(3, 2) = Seccion.Piso
                End If
            Next

            For k = 1 To 3
                col.Lista_F.Add(Lista(k, 1))
                col.Lista_F_Piso.Add(Lista(k, 2))
            Next
        Next

        ' ALR — solo columnas calculadas
        For Each col In colsConRefuerzo
            col.Lista_ALR.Clear()
            For j = 0 To Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR.Count - 1
                Dim Ce As Integer = j
                Dim ultimoTramo = col.Lista_Tramos_Columnas(col.Lista_Tramos_Columnas.Count - 1)
                Dim combMatch = ultimoTramo.Lista_Combinaciones.Find(Function(p) p.Name = Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR(Ce))
                If combMatch Is Nothing Then Continue For
                Dim Valor_ALR As New Columna.ALR
                Valor_ALR.Combinacion = Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR(j)
                Dim Ag_alr As Single = If(ultimoTramo.EsCircular,
                                          CSng(Math.PI * ultimoTramo.Diametro ^ 2 / 4),
                                          ultimoTramo.B_Plano * ultimoTramo.H_Plano)
                Valor_ALR.ALR = Math.Round(Math.Abs(combMatch.P) / (ultimoTramo.fc * Ag_alr * 1000), 2)
                col.Lista_ALR.Add(Valor_ALR)
            Next
        Next

        Form_02_01_ResultadosColumnas.Combo_Elementos.Text = colsConRefuerzo(0).Name_Label
        Form_02_01_ResultadosColumnas.Show()

        Dim msg As String = $"Análisis finalizado: {colsConRefuerzo.Count} columna(s) calculada(s)."
        If cantOmitidas > 0 Then
            msg &= $"{Environment.NewLine}{cantOmitidas} columna(s) omitida(s) por no tener refuerzo definido."
        End If
        MessageBox.Show(msg, "Ejecución de Análisis", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    'Private Sub SecciónToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SecciónToolStripMenuItem.Click
    '    If Proyecto.Elementos.Columnas.Lista_Columnas.Count > 0 Then
    '        Dim Columna = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text)
    '        For i = 0 To Proyecto.Elementos.Columnas.Lista_Columnas.Count - 1
    '            If SRectangular.Combo_Seccion.Items.Count <= Proyecto.Elementos.Columnas.Lista_Columnas.Count Then
    '                SRectangular.Combo_Seccion.Items.Add(Proyecto.Elementos.Columnas.Lista_Columnas(i).Name_Label)
    '            End If
    '        Next
    '        SRectangular.Combo_Seccion.Text = SRectangular.Combo_Seccion.Items(0)
    '        For i = 0 To Columna.Lista_Tramos_Columnas.Count() - 1
    '            If SRectangular.Combo_Tramos.Items.Count <= Columna.Lista_Tramos_Columnas.Count Then
    '                SRectangular.Combo_Tramos.Items.Add(Columna.Lista_Tramos_Columnas(i).Piso)
    '            End If
    '        Next
    '        SRectangular.Combo_Tramos.Text = SRectangular.Combo_Tramos.Items(0)
    '        If SRectangular.Combo_Estacion.Items.Count < 2 Then
    '            SRectangular.Combo_Estacion.Items.Add("Top")
    '            SRectangular.Combo_Estacion.Items.Add("Bottom")
    '        End If
    '        SRectangular.Combo_Estacion.Text = SRectangular.Combo_Estacion.Items(0)

    '        Dim PictureBox5 = SRectangular.PictureBox1

    '        PictureBox5.Location = New Point(25, 70)
    '        PictureBox5.Size = New Size(SRectangular.Panel1.Width - 50, SRectangular.Panel1.Height - 100)
    '        PictureBox5.BackColor = Color.White
    '        PictureBox5.Anchor = AnchorStyles.Left And AnchorStyles.Top And AnchorStyles.Right And AnchorStyles.Bottom
    '        SRectangular.LbCuantia.BackColor = Color.White
    '        SRectangular.Panel1.Controls.Add(PictureBox5)
    '        AddHandler PictureBox5.Paint, AddressOf SRectangular.PictureBox5_Paint
    '        PictureBox5.Refresh()
    '    End If
    '    SRectangular.Show()
    'End Sub

    Private Sub Op_SeccionSimilar_CheckedChanged(sender As Object, e As EventArgs) Handles Op_SeccionSimilar.CheckedChanged

        Dim Secciones_Principales = Proyecto.Elementos.Columnas.Lista_Columnas.FindAll(Function(p) p.Secciones_Principal = True)
        C_Lista_Secciones_Principales.Items.Clear()

        If Op_SeccionPrincipal.Checked = False Then
            If Secciones_Principales.Count < 1 Then
                MessageBox.Show("No se tiene registro de ninguna sección", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Op_SeccionPrincipal.Checked = True
            Else
                For i = 0 To Secciones_Principales.Count - 1
                    C_Lista_Secciones_Principales.Items.Add(Secciones_Principales(i).Name_Label)
                Next
                Dim colSim = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text)
                If colSim IsNot Nothing Then
                    colSim.Secciones_Similar = True
                    colSim.Secciones_Principal = False
                End If
                C_Lista_Secciones_Principales.Enabled = True
            End If
        End If
    End Sub

    Private Sub C_Lista_Secciones_Principales_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_Lista_Secciones_Principales.SelectedIndexChanged
        Tabla_Info_Seccion.Rows.Clear()

        ' Refuerzo: copiado de la sección principal seleccionada
        Dim SeccionPrincipal = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = C_Lista_Secciones_Principales.Text).Lista_Tramos_Columnas
        ' Geometría: mantenida de la sección actual (no se copia del principal)
        Dim SeccionActual = Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text)?.Lista_Tramos_Columnas

        For i = 0 To (SeccionPrincipal.Count - 1) * 2
            Tabla_Info_Seccion.Rows.Add()
        Next

        For i = 0 To (SeccionPrincipal.Count - 1) * 2 Step 2
            Dim tramoPrincipal = SeccionPrincipal(i \ 2)

            ' Geometría: de la sección ACTUAL (no del principal)
            If i \ 2 < SeccionActual.Count Then
                Dim tramoActual = SeccionActual(i \ 2)
                Tabla_Info_Seccion.Rows(i).Cells(0).Value = tramoActual.Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = tramoActual.fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = tramoActual.B_Plano
                Tabla_Info_Seccion.Rows(i).Cells(3).Value = tramoActual.H_Plano
            End If
            Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
            Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

            ' Refuerzo longitudinal: del principal
            Tabla_Info_Seccion.Rows(i).Cells(5).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_2
            Tabla_Info_Seccion.Rows(i).Cells(6).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_3
            Tabla_Info_Seccion.Rows(i).Cells(7).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_4
            Tabla_Info_Seccion.Rows(i).Cells(8).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_5
            Tabla_Info_Seccion.Rows(i).Cells(9).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_6
            Tabla_Info_Seccion.Rows(i).Cells(10).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_7
            Tabla_Info_Seccion.Rows(i).Cells(11).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_8
            Tabla_Info_Seccion.Rows(i).Cells(12).Value = tramoPrincipal.Refuerzo_Col_Top.Barras_10

            Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_2
            Tabla_Info_Seccion.Rows(i + 1).Cells(6).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_3
            Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_4
            Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_5
            Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_6
            Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_7
            Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_8
            Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = tramoPrincipal.Refuerzo_Col_Bottom.Barras_10

            ' Refuerzo transversal: del principal
            Tabla_Info_Seccion.Rows(i).Cells(13).Value = tramoPrincipal.Num_Ramas_Largo
            Tabla_Info_Seccion.Rows(i).Cells(14).Value = tramoPrincipal.Num_Ramas_Corto
            Tabla_Info_Seccion.Rows(i).Cells(15).Value = tramoPrincipal.Numero_Barras_Estribo
            Dim asSentLS As Single = tramoPrincipal.As_Sent_Largo
            If asSentLS = 0 AndAlso Not String.IsNullOrEmpty(tramoPrincipal.Numero_Barras_Estribo) AndAlso tramoPrincipal.Numero_Barras_Estribo <> "User" Then
                asSentLS = AreaRefuerzo(tramoPrincipal.Numero_Barras_Estribo)
            End If
            Tabla_Info_Seccion.Rows(i).Cells(16).Value = asSentLS
            Tabla_Info_Seccion.Rows(i).Cells(17).Value = tramoPrincipal.As_Sent_Corto

            ' Para la sugerencia de ZC, usar dimensiones de la sección ACTUAL si disponible
            Dim bRef As Single = If(i \ 2 < SeccionActual.Count, SeccionActual(i \ 2).B_Plano, tramoPrincipal.B_Plano)
            Dim hRef As Single = If(i \ 2 < SeccionActual.Count, SeccionActual(i \ 2).H_Plano, tramoPrincipal.H_Plano)
            Dim sZC_sugS As Single = Math.Round(Math.Min(bRef, hRef) / 3, 3)
            Tabla_Info_Seccion.Rows(i).Cells(18).Value = If(tramoPrincipal.Separacion_Estribos > 0, tramoPrincipal.Separacion_Estribos, sZC_sugS)
            Tabla_Info_Seccion.Rows(i).Cells(19).Value = If(tramoPrincipal.Separacion_Estribos_ZNC > 0, tramoPrincipal.Separacion_Estribos_ZNC, Math.Round(2 * sZC_sugS, 3))
            Tabla_Info_Seccion.Rows(i).Cells(20).Value = tramoPrincipal.Num_Estribos_ZC
        Next

    End Sub
    Protected Overrides Function ProcessCmdKey(
       ByRef msg As System.Windows.Forms.Message,
       keyData As System.Windows.Forms.Keys) As Boolean

        ' Si el control DataGridView no tiene el foco,
        ' abandonamos el procedimiento.
        '
        If (Not (Tabla_Info_Seccion.Focused)) Then _
                Return MyBase.ProcessCmdKey(msg, keyData)

        ' Comprobamos si se ha pulsado la combinación
        ' de teclas Ctrl + V.
        '
        If (Not (keyData = (Keys.V Or Keys.Control))) Then _
                    Return MyBase.ProcessCmdKey(msg, keyData)

        ' Comprobamos si el contenido del portapapeles es texto.
        '
        Dim isTexto As Boolean = Clipboard.GetDataObject.GetDataPresent(DataFormats.Text)

        If (isTexto) Then
            ' Celda actual del control DataGridView
            '
            Dim Celdas_Seleccionadas As DataGridViewSelectedCellCollection = Tabla_Info_Seccion.SelectedCells
            For Each Celda As DataGridViewCell In Celdas_Seleccionadas
                Celda.Value = My.Computer.Clipboard.GetText()
            Next

            'Dim currentCell As DataGridViewCell = Tabla_Info_Seccion.CurrentCell
            'currentCell.Value = My.Computer.Clipboard.GetText()

        End If

        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

    Private Sub Tabla_Info_Seccion_CellContentClick_1(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_Seccion.CellContentClick

    End Sub
End Class