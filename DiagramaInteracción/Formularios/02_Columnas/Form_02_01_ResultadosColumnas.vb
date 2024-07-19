Imports Excel = Microsoft.Office.Interop.Excel
Imports System.Data.OleDb
Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.Funciones_00_Varias

Public Class Form_02_01_ResultadosColumnas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Tabla_Resultados.Columns(8).HeaderText = "φVn (kN)"
        Tabla_Resultados.Columns(9).HeaderText = "Vu (kN)"
        Tabla_Resultados.Columns(10).HeaderText = "φVn/Vu"

        Tabla_Resultados.Rows.Clear()

        Dim Seccion = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text).Lista_Tramos_Columnas
        For i = 0 To (Seccion.Count - 1) * 2
            Tabla_Resultados.Rows.Add()
        Next

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Tabla_Resultados.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Resultados.Rows(i).Cells(1).Value = Seccion(i / 2).Seccion
            Tabla_Resultados.Rows(i).Cells(2).Value = Math.Round(Seccion(i / 2).Cuantia_Col_Top * 100, 2)
            Tabla_Resultados.Rows(i + 1).Cells(2).Value = Math.Round(Seccion(i / 2).Cuantia_Col_Bottom * 100, 2)
            Tabla_Resultados.Rows(i).Cells(3).Value = Seccion(i / 2).As_Col_Top
            Tabla_Resultados.Rows(i + 1).Cells(3).Value = Seccion(i / 2).As_Col_Bottom
            Tabla_Resultados.Rows(i).Cells(4).Value = Seccion(i / 2).As_Req_Top
            Tabla_Resultados.Rows(i + 1).Cells(4).Value = Seccion(i / 2).As_Req_Bottom
            Tabla_Resultados.Rows(i).Cells(5).Value = Seccion(i / 2).F_Flexo_Top
            Tabla_Resultados.Rows(i + 1).Cells(5).Value = Seccion(i / 2).F_Flexo_Bottom
            Tabla_Resultados.Rows(i).Cells(6).Value = Seccion(i / 2).F_Flexo_Modelo_Top
            Tabla_Resultados.Rows(i + 1).Cells(6).Value = Seccion(i / 2).F_Flexo_Modelo_Bottom
            Tabla_Resultados.Rows(i).Cells(7).Value = "Largo"
            Tabla_Resultados.Rows(i + 1).Cells(7).Value = "Corto"
            Tabla_Resultados.Rows(i).Cells(8).Value = Seccion(i / 2).Vn_2
            Tabla_Resultados.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Vn_3
            Tabla_Resultados.Rows(i).Cells(9).Value = Seccion(i / 2).Vu_2
            Tabla_Resultados.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Vu_3
            Tabla_Resultados.Rows(i).Cells(10).Value = Seccion(i / 2).F_Cortante_2
            Tabla_Resultados.Rows(i + 1).Cells(10).Value = Seccion(i / 2).F_Cortante_3
            Tabla_Resultados.Rows(i).Cells(11).Value = Seccion(i / 2).Ash_Col_Largo
            Tabla_Resultados.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Ash_Col_Corto
            Tabla_Resultados.Rows(i).Cells(12).Value = Math.Round(Seccion(i / 2).Ash_L, 0)
            Tabla_Resultados.Rows(i + 1).Cells(12).Value = Math.Round(Seccion(i / 2).Ash_C, 0)
            Tabla_Resultados.Rows(i).Cells(13).Value = Seccion(i / 2).F_Ash_Largo
            Tabla_Resultados.Rows(i + 1).Cells(13).Value = Seccion(i / 2).F_Ash_Corto
        Next

        For i = 0 To Seccion.Count * 2 - 1
            If Tabla_Resultados.Rows(i).Cells(5).Value < 0.9 Then
                Funcion_Color_Cumple(Tabla_Resultados, i, 5, "No cumple")
            Else
                Funcion_Color_Cumple(Tabla_Resultados, i, 5, "Cumple")
            End If
            If Tabla_Resultados.Rows(i).Cells(6).Value < 0.9 And Tabla_Resultados.Rows(i).Cells(6).Value > 0 Then
                Funcion_Color_Cumple(Tabla_Resultados, i, 6, "No cumple")
            Else
                Funcion_Color_Cumple(Tabla_Resultados, i, 6, "Cumple")
            End If
            If Tabla_Resultados.Rows(i).Cells(10).Value < 0.9 Then
                Funcion_Color_Cumple(Tabla_Resultados, i, 10, "No cumple")
            Else
                Funcion_Color_Cumple(Tabla_Resultados, i, 10, "Cumple")
            End If
            If Tabla_Resultados.Rows(i).Cells(13).Value < 0.9 Then
                Funcion_Color_Cumple(Tabla_Resultados, i, 13, "No cumple")
            Else
                Funcion_Color_Cumple(Tabla_Resultados, i, 13, "Cumple")
            End If
        Next


    End Sub

    Private Sub Form_03_01_ResultadosColumnas_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize

        Label1.Left = (Panel1.Width - Label1.Width) / 2

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Form_ALR.Tabla_ALR.Rows.Clear()
        Form_ALR.Tabla_ALR.Columns.Clear()

        Form_ALR.Tabla_ALR.Columns.Add("Column1", "Piso")
        For i = 0 To Proyecto.Columnas.Lista_Combinaciones_ALR.Count - 1
            Form_ALR.Tabla_ALR.Columns.Add("Column" & i + 1, Proyecto.Columnas.Lista_Combinaciones_ALR(i).ToString)
        Next

        Dim Seccion = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Elementos.Text).Lista_Tramos_Columnas
        For i = 0 To Seccion.Count - 1
            Form_ALR.Tabla_ALR.Rows.Add()
        Next

        For i = 0 To Seccion.Count - 1
            Form_ALR.Tabla_ALR.Rows(i).Cells(0).Value = Seccion(i).Piso

            For j = 0 To Proyecto.Columnas.Lista_Combinaciones_ALR.Count - 1
                Dim ce As Integer = j
                Form_ALR.Tabla_ALR.Rows(i).Cells(j + 1).Value = Math.Round(Math.Abs(Seccion(i).Lista_Combinaciones.Find(Function(p) p.Name = Form_ALR.Tabla_ALR.Columns(ce + 1).HeaderText).P) / (Seccion(i).fc * Seccion(i).B_Plano * Seccion(i).H_Plano * 1000), 2)
            Next
        Next

        For i = 0 To Seccion.Count - 1
            For j = 0 To Proyecto.Columnas.Lista_Combinaciones_ALR.Count - 1
                If Form_ALR.Tabla_ALR.Rows(i).Cells(j + 1).Value > 0.4 Then
                    Funcion_Color_Cumple(Form_ALR.Tabla_ALR, i, j + 1, "No cumple")
                End If
            Next
        Next

        Form_ALR.T_ELemento.Text = Combo_Elementos.Text
        Form_ALR.Show()

    End Sub

    Private Sub Form_03_01_ResultadosColumnas_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Proyecto.Columnas.Verificacion_ALR = True Then
            Button1.Visible = True
        End If
    End Sub

    Public Sub AExcelToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Reporte_Col_Excel.Click

        Me.Cursor = Cursors.WaitCursor
        Dim P As Single = 2

        Dim Archivo As String = "RevisiónColumnas"
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDbConnection(connection)

        'Try
        Dim Color_Interior As Color = Color.FromArgb(200, 200, 200)
        Dim appXL As New Excel.Application
        Dim wbXL As Excel.Workbook
        Dim Hoja_Resultados As Excel.Worksheet
        wbXL = appXL.Workbooks.Add()

        Hoja_Resultados = wbXL.Sheets("Hoja1")
        Hoja_Resultados.PageSetup.Application.ActiveWindow.DisplayGridlines = False

        Hoja_Resultados.Name = "Revisión de Capacidad"

        Hoja_Resultados.Range("E1:F1").Merge(True)
        Hoja_Resultados.Range("H1:P1").Merge(True)
        Hoja_Resultados.Range("A1:A2").MergeCells = True
        Hoja_Resultados.Range("B1:B2").MergeCells = True
        Hoja_Resultados.Range("C1:C2").MergeCells = True
        Hoja_Resultados.Range("A1:F2").Interior.Color = Color_Interior
        Hoja_Resultados.Range("H1:P2").Interior.Color = Color_Interior
        Hoja_Resultados.Range("A1:P10000").Font.Name = "Arial"
        Hoja_Resultados.Range("A1:P2").Font.Bold = True
        Hoja_Resultados.Range("A1:P2").Font.Size = 11
        Hoja_Resultados.Range("A3:P10000").Font.Size = 10
        Hoja_Resultados.Range("A1:P100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A1:P10000").VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A:F").ColumnWidth = 25
        Hoja_Resultados.Range("H:P").ColumnWidth = 25

        Hoja_Resultados.Cells(1, 1) = "Columna"
        Hoja_Resultados.Cells(1, 2) = "Sección"
        Hoja_Resultados.Cells(1, 3) = "Cuantía"
        Hoja_Resultados.Cells(1, 4) = "Flexo-Compresión"
        Hoja_Resultados.Cells(2, 4) = "Capacidad/Demanda"
        Hoja_Resultados.Cells(1, 5) = "Cortante"
        Hoja_Resultados.Cells(2, 5) = "φVn/Vu (Lado corto)"
        Hoja_Resultados.Cells(2, 6) = "φVn/Vu (Lado largo)"
        Hoja_Resultados.Cells(1, 8) = "Confinamiento"
        Hoja_Resultados.Cells(2, 8) = "Sección"
        Hoja_Resultados.Cells(2, 9) = "S0 Colocado (m)"
        Hoja_Resultados.Cells(2, 10) = "S0 Requerido (m)"
        Hoja_Resultados.Cells(2, 11) = "l0 Colocado (m)"
        Hoja_Resultados.Cells(2, 12) = "l0 Requerido (m)"
        Hoja_Resultados.Cells(2, 13) = "Dirección"
        Hoja_Resultados.Cells(2, 14) = "Ash Colocado (mm2)"
        Hoja_Resultados.Cells(2, 15) = "Ash Requerido (mm2)"
        Hoja_Resultados.Cells(2, 16) = "AshCol/AshReq"

        Dim Secciones_Confinamiento As New List(Of String)
        Dim Seccion_Confinamiento As String = ""
        Dim g As Integer = 3
        Dim R_Ash_Min As Single = 1
        Dim Fc_Max As Single = 35

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
            Dim Columna = Proyecto.Columnas.Lista_Columnas(i)
            Hoja_Resultados.Cells(3 + i, 1) = Columna.Name_Label
            Hoja_Resultados.Cells(3 + i, 40) = Columna.Name_Label
            Hoja_Resultados.Cells(3 + i, 41) = Columna.Name_Elemento

            Dim F As Single = 100
            Dim Seccion As String = "E-1"
            Dim Cuantia As Single
            Dim F_V2 As Single = 100
            Dim F_V3 As Single = 100
            For j = 0 To Columna.Lista_Tramos_Columnas.Count - 1
                Dim Tramo = Columna.Lista_Tramos_Columnas(j)
                If Math.Max(Tramo.F_Flexo_Bottom, Tramo.F_Flexo_Modelo_Bottom) < F Then
                    F = Math.Max(Tramo.F_Flexo_Bottom, Tramo.F_Flexo_Modelo_Bottom)
                    Seccion = Convert.ToString(Format(Tramo.B_Plano, "##,##0.00") & "x" & Format(Tramo.H_Plano, "##,##0.00"))
                    Cuantia = Tramo.Cuantia_Col_Bottom
                    If Math.Max(Tramo.F_Flexo_Top, Tramo.F_Flexo_Modelo_Top) < F Then
                        F = Math.Max(Tramo.F_Flexo_Top, Tramo.F_Flexo_Modelo_Top)
                        Seccion = Convert.ToString(Format(Tramo.B_Plano, "##,##0.00") & "x" & Format(Tramo.H_Plano, "##,##0.00"))
                        Cuantia = Tramo.Cuantia_Col_Top
                    End If
                End If
                If Tramo.F_Cortante_2 < F_V2 Then
                    F_V2 = Tramo.F_Cortante_2
                End If
                If Tramo.F_Cortante_3 < F_V3 Then
                    F_V3 = Tramo.F_Cortante_3
                End If

                If Seccion_Confinamiento <> Tramo.Seccion And Not Secciones_Confinamiento.Exists(Function(u) u = Tramo.Seccion) Then
                    Seccion_Confinamiento = Tramo.Seccion
                    Secciones_Confinamiento.Add(Seccion_Confinamiento)
                    Hoja_Resultados.Range(Hoja_Resultados.Cells(g, 8), Hoja_Resultados.Cells(g + 1, 8)).MergeCells = True
                    Hoja_Resultados.Range(Hoja_Resultados.Cells(g, 9), Hoja_Resultados.Cells(g + 1, 9)).MergeCells = True
                    Hoja_Resultados.Range(Hoja_Resultados.Cells(g, 10), Hoja_Resultados.Cells(g + 1, 10)).MergeCells = True
                    Hoja_Resultados.Range(Hoja_Resultados.Cells(g, 11), Hoja_Resultados.Cells(g + 1, 11)).MergeCells = True
                    Hoja_Resultados.Range(Hoja_Resultados.Cells(g, 12), Hoja_Resultados.Cells(g + 1, 12)).MergeCells = True
                    Hoja_Resultados.Cells(g, 8) = Seccion_Confinamiento
                    Hoja_Resultados.Cells(g, 9) = Math.Round(Tramo.Separacion_Estribos, 2)
                    Hoja_Resultados.Cells(g, 10) = Math.Round(Math.Min(Tramo.S0_C, Tramo.S0_L), 2)
                    Hoja_Resultados.Cells(g, 12) = Math.Round(Math.Max(Tramo.L0_C, Tramo.L0_L), 2)
                    Hoja_Resultados.Cells(g, 13) = "Largo"
                    Hoja_Resultados.Cells(g + 1, 13) = "Corto"
                    Hoja_Resultados.Cells(g, 14) = Tramo.Ash_Col_Largo
                    Hoja_Resultados.Cells(g + 1, 14) = Tramo.Ash_Col_Corto
                    Hoja_Resultados.Cells(g, 15) = Math.Round(Tramo.Ash_L, 0)
                    Hoja_Resultados.Cells(g + 1, 15) = Math.Round(Tramo.Ash_C, 0)
                    Hoja_Resultados.Cells(g, 16) = Math.Round(Tramo.F_Ash_Largo, 2)
                    Hoja_Resultados.Cells(g + 1, 16) = Math.Round(Tramo.F_Ash_Corto, 2)

                    If R_Ash_Min > Math.Min(Tramo.F_Ash_Largo, Tramo.F_Ash_Corto) Then
                        R_Ash_Min = Math.Min(Tramo.F_Ash_Largo, Tramo.F_Ash_Corto)
                    End If

                    If Tramo.fc >= Fc_Max Then
                        Fc_Max = Tramo.fc
                    End If

                    If Tramo.F_Ash_Largo < 0.9 Then
                        Hoja_Resultados.Range("P" & g).Font.Color = Color.FromArgb(156, 0, 6)
                        Hoja_Resultados.Range("P" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                    Else
                        Hoja_Resultados.Range("P" & g).Font.Color = Color.FromArgb(0, 97, 0)
                        Hoja_Resultados.Range("P" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                    End If
                    If Tramo.F_Ash_Corto < 0.9 Then
                        Hoja_Resultados.Range("P" & g + 1).Font.Color = Color.FromArgb(156, 0, 6)
                        Hoja_Resultados.Range("P" & g + 1).Interior.Color = Color.FromArgb(255, 199, 206)
                    Else
                        Hoja_Resultados.Range("P" & g + 1).Font.Color = Color.FromArgb(0, 97, 0)
                        Hoja_Resultados.Range("P" & g + 1).Interior.Color = Color.FromArgb(198, 239, 206)
                    End If

                    g += 2
                End If
            Next

            Hoja_Resultados.Cells(3 + i, 2) = Seccion
            Hoja_Resultados.Cells(3 + i, 3) = Convert.ToString(Math.Round(Cuantia * 100, 2) & " %")
            Hoja_Resultados.Cells(3 + i, 4) = Math.Round(F, 2)
            Hoja_Resultados.Cells(3 + i, 5) = Math.Round(F_V3, 2)
            Hoja_Resultados.Cells(3 + i, 6) = Math.Round(F_V2, 2)

            If F < 0.9 Then
                Hoja_Resultados.Range("D" & 3 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("D" & 3 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            Else
                Hoja_Resultados.Range("D" & 3 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("D" & 3 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            End If
            If F_V3 < 0.9 Then
                Hoja_Resultados.Range("E" & 3 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("E" & 3 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            Else
                Hoja_Resultados.Range("E" & 3 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("E" & 3 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            End If
            If F_V2 < 0.9 Then
                Hoja_Resultados.Range("F" & 3 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("F" & 3 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            Else
                Hoja_Resultados.Range("F" & 3 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("F" & 3 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            End If
        Next

        'If Form_03_PagColumnas.Proyecto.Lista_fc.Max <= 35 And R_Ash_Min < 1 Then
        If R_Ash_Min < 1 And Fc_Max <= 35 Then
            Hoja_Resultados.Range("M" & g + 2 & ":P" & g + 3).MergeCells = True
            Hoja_Resultados.Cells(g + 2, 13) = "Nota:  Debido a que la resistencia a la compresión máxima del proyecto no supera los 35 MPa, es posible que su diseño satisfaga la alternativa planteada en el numeral C.21.3.5.9 de la NSR-10"
            Hoja_Resultados.Range("M" & g + 2 & ":P" & g + 3).WrapText = True
        End If

        Dim saveFileDialog1 As New SaveFileDialog()
        saveFileDialog1.Title = "Guardar documento Excel"
        saveFileDialog1.Filter = "Excel File|*.xlsx"
        saveFileDialog1.FileName = Convert.ToString(Archivo & "_Proyecto - " & PagInfoGeneral.NameProject.Text)
        saveFileDialog1.ShowDialog()
        wbXL.SaveAs(saveFileDialog1.FileName)
        appXL.Workbooks.Close()
        appXL.Quit()
        System.Diagnostics.Process.Start(saveFileDialog1.FileName)

        'Catch ex As Exception
        'MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Finally
        conexion.Close()
        Cursor = Cursors.Arrow
        'End Try

    End Sub

    Private Sub IngresarResultadosDeModeloToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Resultados_Modelo_Col.Click
        Form_02_01_02_ResultadosModelo.Combo_Elementos.Items.Clear()

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
            Form_02_01_02_ResultadosModelo.Combo_Elementos.Items.Add(Proyecto.Columnas.Lista_Columnas(i).Name_Label)

        Next
        Form_02_01_02_ResultadosModelo.Combo_Elementos.Text = Proyecto.Columnas.Lista_Columnas(0).Name_Label
        Form_02_01_02_ResultadosModelo.Show()
    End Sub

    Private Sub GráficosToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles Graficos_Resultados.Click
        Form_Graficos.Show()
    End Sub

    Private Sub RevisiónDeCortanteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Cortante_Resultados.Click
        Form_02_01_00_RevisionCortante.Combo_Elementos.Items.Clear()

        For i = 0 To Proyecto.Columnas.Lista_Columnas.Count - 1
            Form_02_01_00_RevisionCortante.Combo_Elementos.Items.Add(Proyecto.Columnas.Lista_Columnas(i).Name_Label)
        Next

        Form_02_01_00_RevisionCortante.Combo_Elementos.Text = Combo_Elementos.Text
        Form_02_01_00_RevisionCortante.Tabla_Resultados.Columns(3).HeaderText = "φVc (kN)"
        Form_02_01_00_RevisionCortante.Tabla_Resultados.Columns(4).HeaderText = "φVs (kN)"
        Form_02_01_00_RevisionCortante.Tabla_Resultados.Columns(5).HeaderText = "φVn (kN)"
        Form_02_01_00_RevisionCortante.Tabla_Resultados.Columns(6).HeaderText = "Vu (kN)"
        Form_02_01_00_RevisionCortante.Tabla_Resultados.Columns(7).HeaderText = "φVn/Vu"
        Form_02_01_00_RevisionCortante.Show()
    End Sub

End Class