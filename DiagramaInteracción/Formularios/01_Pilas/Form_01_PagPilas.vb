Imports System.Data.OleDb
Imports System.IO
Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_01_Pilas
Imports Excel = Microsoft.Office.Interop.Excel
Public Class Form_01_PagPilas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public pictureBox4 As New PictureBox()

    '-------------------------------- INICIAR ANÁLISIS --------------------------------
    Public Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Dim Lista_Elementos As New List(Of String)

        Lista_Elementos = Proyecto.Elementos.Pilas.Reactions.Select(Function(r) r.JointLabel) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

        Form_01_00_PagInfoPilas.Tabla_Elementos.Rows.Clear()

        ComboElementos.Items.Clear()

        Panel2.Controls.Add(pictureBox4)
        Me.Cursor = Cursors.WaitCursor
        ResumenDI.Rows.Clear()
        ResumenDI.Rows.Add()
        ResumenDI.Rows.Add()
        TablaRevi.Rows.Clear()
        Tabla_ResumenVisual.Rows.Clear()

        ResumenDI.Visible = True

        Proyecto.Elementos.Pilas.Esf_Adm_Est = Convert.ToSingle(EadmEst.Text)
        Proyecto.Elementos.Pilas.Esf_Adm_Din = Convert.ToSingle(EadmDin.Text)
        Proyecto.Elementos.Pilas.Esf_Frccion = Convert.ToSingle(Esf_Friccion.Text)

        Proyecto.Elementos.Pilas.Def_Uni_ConcAs = Convert.ToSingle(PagMateriales.ecu.Text)
        Proyecto.Elementos.Pilas.Fy = Convert.ToSingle(PagMateriales.Fy.Text)
        Proyecto.Elementos.Pilas.ModuloE_Acero = Convert.ToSingle(PagMateriales.Es.Text)

        'Try
        Proyecto.Elementos.Pilas.ListaElementos.Clear()
        For Each Elemento_ In Lista_Elementos

            Dim Seccion As New Elemento_Pila

            Seccion.Name_Elemento = Elemento_
            Seccion.Name_Label = Elemento_

            Seccion.Df = Convert.ToDouble(Diametro.Text)
            Seccion.Dc = Convert.ToSingle(Dc.Text)
            Seccion.L_Pila = Convert.ToSingle(Long_Pila.Text)
            Seccion.fc = Convert.ToSingle(T_fc.Text)

            If Op_Seccion.Text = "Hueca" Then
                Seccion.Opcion_Hueca = "Si"
                Seccion.Esp_Anillo = Convert.ToSingle(T_Espesor.Text)
            Else
                Seccion.Opcion_Hueca = "No"
                Seccion.Esp_Anillo = 0
            End If

            Seccion.N_Barra_Long = RefuerzoLong.Text
            Seccion.Cant_Barras_Long = Convert.ToInt32(NumRLong.Text)
            Seccion.N_Barra_Trans = RefuerzoTransv.Text
            Seccion.Separacion_Trans = Convert.ToSingle(Separacion.Text)
            Seccion.Acero_Long = CDbl(AreaRefuerzo(Seccion.N_Barra_Long))

            Proyecto.Elementos.Pilas.ListaElementos.Add(Seccion)
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows.Add()
            Dim nRows As Integer = Form_01_00_PagInfoPilas.Tabla_Elementos.Rows.Count
            Dim te As DataGridViewRow = Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(nRows - 1)
            te.Cells(0).Value = Seccion.Name_Label
            te.Cells(1).Value = Seccion.Name_Elemento
            te.Cells(2).Value = Seccion.Df
            te.Cells(3).Value = Seccion.Dc
            te.Cells(4).Value = Seccion.L_Pila
            te.Cells(5).Value = Seccion.Opcion_Hueca
            te.Cells(6).Value = Seccion.Esp_Anillo
            te.Cells(7).Value = Seccion.N_Barra_Long
            te.Cells(8).Value = Seccion.Acero_Long
            te.Cells(9).Value = Seccion.Cant_Barras_Long
            te.Cells(10).Value = Seccion.fc

        Next


        'Dim ColLab As Integer = 1
        'Dim ColComb As Integer = 3
        'Dim ColP As Integer = 6
        'Dim ColM2 As Integer = 7
        'Dim ColM3 As Integer = 8
        'Dim FT As Integer = 1
        'Dim V2 As Integer = 4
        'Dim V3 As Integer = 5

        'If Proyecto.Elementos.Pilas.Opcion_Elemento = "Frame" Then
        '    ColLab = 1
        '    ColComb = 3
        '    ColP = 5
        '    ColM2 = 9
        '    ColM3 = 10
        '    V2 = 6
        '    V3 = 7
        '    FT = -1
        'End If
        'If Proyecto.Elementos.Pilas.Opcion_Elemento = "Pier" Then
        '    ColComb = 2
        '    ColP = 4
        '    ColM2 = 8
        '    ColM3 = 9
        '    V2 = 5
        '    V3 = 6
        '    FT = -1
        'End If

        'Dim NumServ As Double = TablaCServicio.Rows(0).Cells(15).Value + 1
        'Dim NumUlti As Double = TablaCUltimas.Rows(0).Cells(15).Value + 1
        'Dim Name As String = ""

        'For i = 2 To NumUlti
        '    If TablaCUltimas.Rows(i).Cells(ColLab).Value <> Name Then

        '        Form_01_00_PagInfoPilas.Tabla_Elementos.Rows.Add()
        '        Dim Seccion As New Elemento_Pila
        '        Name = Convert.ToString(TablaCUltimas.Rows(i).Cells(1).Value)
        '        Seccion.Name_Elemento = Convert.ToString(TablaCUltimas.Rows(i).Cells(1).Value)
        '        Seccion.Name_Label = Convert.ToString(TablaCUltimas.Rows(i).Cells(1).Value)
        '        Seccion.Matriz_PS = New List(Of Single)
        '        Seccion.Matriz_MS = New List(Of Single)
        '        Seccion.Matriz_PU = New List(Of Single)
        '        Seccion.Matriz_MU = New List(Of Single)
        '        Seccion.Matriz_V2 = New List(Of Single)
        '        Seccion.Matriz_V3 = New List(Of Single)

        '        Seccion.Matriz_Combinaciones = New List(Of String)
        '        Seccion.Df = Convert.ToDouble(Diametro.Text)
        '        Seccion.Dc = Convert.ToSingle(Dc.Text)
        '        Seccion.fc = Convert.ToSingle(PagMateriales.Fc.Text)

        '        If Op_Seccion.Text = "Hueca" Then
        '            Seccion.Opcion_Hueca = "Si"
        '            Seccion.Esp_Anillo = Convert.ToSingle(T_Espesor.Text)
        '        Else
        '            Seccion.Opcion_Hueca = "No"
        '            Seccion.Esp_Anillo = 0
        '        End If

        '        Seccion.N_Barra_Long = RefuerzoLong.Text
        '        Seccion.Cant_Barras_Long = Convert.ToInt32(NumRLong.Text)
        '        Seccion.N_Barra_Trans = RefuerzoTransv.Text
        '        Seccion.Separacion_Trans = Convert.ToSingle(Separacion.Text)

        '        Seccion.Ps_Estatica = 0
        '        Seccion.Ps_Dinamica = 0
        '        Seccion.Pu_Estatica = 0
        '        Seccion.Pu_Dinamica = 0

        '        For j = 2 To NumServ
        '            If TablaCServicio.Rows(j).Cells(ColLab).Value = Name Then
        '                Seccion.Matriz_PS.Add(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColP).Value))
        '                Seccion.Matriz_MS.Add(Math.Max(Math.Abs(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColM2).Value)), Math.Abs(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColM3).Value))))
        '                If Len(TablaCServicio.Rows(j).Cells(ColComb).Value.ToString) < 20 Then
        '                    If Math.Abs(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColP).Value)) > Math.Abs(Seccion.Ps_Estatica) Then
        '                        Seccion.Ps_Estatica = Math.Abs(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColP).Value))
        '                    End If
        '                Else
        '                    If Math.Abs(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColP).Value)) > Math.Abs(Seccion.Ps_Dinamica) Then
        '                        Seccion.Ps_Dinamica = Math.Abs(Convert.ToSingle(TablaCServicio.Rows(j).Cells(ColP).Value))
        '                    End If
        '                End If
        '            End If
        '        Next
        '        For j = 2 To NumUlti
        '            If TablaCUltimas.Rows(j).Cells(ColLab).Value = Name Then
        '                Seccion.Matriz_PU.Add(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColP).Value))
        '                Seccion.Matriz_MU.Add(Math.Max(Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColM2).Value)), Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColM3).Value))))
        '                Seccion.Matriz_V2.Add(Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(V2).Value)))
        '                Seccion.Matriz_V3.Add(Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(V3).Value)))

        '                Seccion.Matriz_Combinaciones.Add(TablaCUltimas.Rows(j).Cells(ColComb).Value)
        '                If Len(TablaCUltimas.Rows(j).Cells(ColComb).Value.ToString) < 20 Then
        '                    If Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColP).Value)) > Math.Abs(Seccion.Pu_Estatica) Then
        '                        Seccion.Pu_Estatica = Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColP).Value))
        '                    End If
        '                Else
        '                    If Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColP).Value)) > Math.Abs(Seccion.Pu_Dinamica) Then
        '                        Seccion.Pu_Dinamica = Math.Abs(Convert.ToSingle(TablaCUltimas.Rows(j).Cells(ColP).Value))
        '                    End If
        '                End If
        '            End If
        '        Next
        '        Proyecto.Elementos.Pilas.ListaElementos.Add(Seccion)
        '    End If
        'Next

        For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count - 1
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(0).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Name_Label
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(1).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Name_Elemento
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(2).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Df
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(3).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Dc
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(4).Value = Proyecto.Elementos.Pilas.ListaElementos(i).L_Pila
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(5).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Opcion_Hueca
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(6).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Esp_Anillo
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(7).Value = Proyecto.Elementos.Pilas.ListaElementos(i).N_Barra_Long
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(9).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Cant_Barras_Long
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i).Cells(10).Value = Proyecto.Elementos.Pilas.ListaElementos(i).fc

            TablaRevi.Rows.Add()
        Next

        If OpcionPila.Checked = True Then
            TablaRevi.Columns(1).HeaderText = "Ps Estática [kN]"
            TablaRevi.Columns(2).HeaderText = "Ps Dinámica [kN]"
            TablaRevi.Columns(3).HeaderText = "Pu Estática [kN]"
            TablaRevi.Columns(4).HeaderText = "Pu Dinámica [kN]"
            TablaRevi.Columns(5).HeaderText = "Pu Tracción [kN]"
            TablaRevi.Columns(6).HeaderText = "Chequeo 1 (Ps E)"
            TablaRevi.Columns(7).HeaderText = "Chequeo 2 (Ps D)"
            TablaRevi.Columns(8).HeaderText = "Chequeo 3 (Pu E)"
            TablaRevi.Columns(9).HeaderText = "Chequeo 4 (Pu D)"
            TablaRevi.Columns(10).HeaderText = "Chequeo 5 (Pu T)"
            TablaRevi.Columns(11).HeaderText = "σ Transmitido Estático [kN/m2]"
            TablaRevi.Columns(12).HeaderText = "σ Transmitido Dinámico [kN/m2]"
            TablaRevi.Columns(13).HeaderText = "σAdm/σTrans Estático"
            TablaRevi.Columns(14).HeaderText = "σAdm/σTrans Dinámico"
            TablaRevi.Columns(15).HeaderText = "φVn [kN]"
            TablaRevi.Columns(16).HeaderText = "Vu [kN]"
            TablaRevi.Columns(17).HeaderText = "φVn/Vu"
            TablaRevi.Columns(18).HeaderText = "Chequeo V2"
            TablaRevi.Columns(19).HeaderText = "Chequeo V3"
            TablaRevi.Columns(20).HeaderText = "ρ Col [%]"
            TablaRevi.Columns(21).HeaderText = "Capacidad/Demanda (Cortes)"
            TablaRevi.Columns(22).HeaderText = "Capacidad/Demanda (Recta)"
        End If

        ComboElementos.Visible = True

        'Catch ex As Exception
        'Finally

        Form_01_00_PagInfoPilas.Show()

        'End Try
        Me.Cursor = Cursors.Arrow


    End Sub
    Public Sub PictureBox4_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)

        Dim D As Single = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Dc
        Dim R As Single = 0.075
        Dim Num_Ref_Long As String = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).N_Barra_Long
        Dim Cant_Ref_Long As Single = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Cant_Barras_Long
        Dim Op_Hueca As String = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Opcion_Hueca
        Dim Esp_Anillo As Single = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Esp_Anillo
        Dim cuantia As Single = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Cuantia

        Dim g As Graphics = e.Graphics
        pictureBox4.BackColor = Color.White
        Dim Rb As Double = (D / 2) - R
        Dim Ac = AreaRefuerzo(Num_Ref_Long)
        Dim Db = DiametroRefuerzo(Num_Ref_Long)
        Dim Ag As Double = Math.PI * (D * 1000 / 2) ^ 2
        If Op_Hueca = "Si" Then
            Dim D2 As Single = D - 2 * Esp_Anillo
            Ag = Math.PI * (D ^ 2 - D2 ^ 2) / 4
        End If

        Dim letra As New Font("Arial", 11, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim corB As New SolidBrush(Color.Black)
        g.DrawString(Convert.ToString("ρ=" & cuantia), letra, corB, New PointF(245, 185))
        Dim Esc As Double = 190 / D
        Dim DEsc As Integer = D * Esc
        Dim DbEsc As Integer = Db * Esc
        Dim ancho As Double = Panel2.Width
        Dim alto As Double = Panel2.Height
        Dim solidBruh As New SolidBrush(Color.FromArgb(121, 121, 121))
        Dim cor As New SolidBrush(Color.FromArgb(210, 210, 210))
        Dim pen As New Pen(Color.FromArgb(21, 21, 21), 2)
        Dim pen1 As New Pen(Color.FromArgb(21, 21, 21))
        g.DrawEllipse(pen, Convert.ToInt32(148.5 - DEsc / 2), Convert.ToInt32(102 - DEsc / 2), DEsc, DEsc)
        g.FillEllipse(cor, Convert.ToInt32(148.5 - DEsc / 2), Convert.ToInt32(102 - DEsc / 2), DEsc, DEsc)
        If Op_Hueca = "Si" Then
            Dim D2 As Single = (D - 2 * Convert.ToSingle(T_Espesor.Text)) * Esc
            Dim pen2 As New SolidBrush(Color.White)
            g.DrawEllipse(pen, Convert.ToInt32(148.5 - D2 / 2), Convert.ToInt32(102 - D2 / 2), D2, D2)
            g.FillEllipse(pen2, Convert.ToInt32(148.5 - D2 / 2), Convert.ToInt32(102 - D2 / 2), D2, D2)
        End If
        Dim Beta As Double
        Dim X As Double
        Dim Y As Double
        For i = 1 To Cant_Ref_Long
            Beta = (360 / Cant_Ref_Long) * i
            Y = 102 - (Math.Sin(Beta * Math.PI / 180) * Rb * Esc) - DbEsc / 2
            X = 148.5 + (Math.Cos(Beta * Math.PI / 180) * Rb * Esc) - DbEsc / 2
            g.FillEllipse(solidBruh, Convert.ToInt32(X), Convert.ToInt32(Y), DbEsc, DbEsc)
            g.DrawEllipse(pen1, Convert.ToInt32(X), Convert.ToInt32(Y), DbEsc, DbEsc)
        Next
    End Sub
    '-------------------------- EXPORTAR RESUMEN A EXCEL --------------------------
    Public Sub DatosAExcelToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Exportar_Excel.Click
        Me.Cursor = Cursors.WaitCursor
        Dim Archivo As String = "Revisión"
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDb.OleDbConnection(connection)

        Try
            Dim c As Color = Color.FromArgb(200, 200, 200)
            Dim c1 As Color = Color.FromArgb(220, 220, 220)
            Dim appXL As New Microsoft.Office.Interop.Excel.Application
            Dim wbXL As Excel.Workbook
            'Dim shXL As Excel.Worksheet
            Dim shxl4 As Excel.Worksheet
            Dim shXL3 As Excel.Worksheet
            wbXL = appXL.Workbooks.Add()
            Dim indice As Integer = 2
            'Dim objExcelChart As Excel.Chart
            'Dim objRange As Excel.Range
            '---------------------------- Exportar Datos en la hoja 1 - (DIAGRAMA DE INTERACCIÓN) -----------------------------
            'appXL = CreateObject("Excel.Application")
            'appXL.Visible = False

            'shXL = wbXL.Sheets("Hoja1")
            'shXL.Name = "Diagrama de Interacción"
            'shXL.Range("A1:K1").Font.Bold = True
            'shXL.Range("A1:K1").Font.Size = 11
            'shXL.Range("A2:D1000").Font.Size = 10
            'shXL.Range("A1:D1000").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
            'shXL.Range("A1:D1").Interior.Color = c
            'shXL.Cells(1, 1) = "Mn (kN.m)"
            'shXL.Cells(1, 2) = "Pn (kN)"
            'shXL.Cells(1, 3) = "φMn (kN.m)"
            'shXL.Cells(1, 4) = "φPn (kN)"
            'For i = 1 To TablaDI.Rows.Count() - 1
            '    shXL.Cells(i + 1, 1) = TablaDI.Rows(i - 1).Cells(1).Value
            '    shXL.Cells(i + 1, 2) = TablaDI.Rows(i - 1).Cells(0).Value
            '    shXL.Cells(i + 1, 3) = TablaDI.Rows(i - 1).Cells(3).Value
            '    shXL.Cells(i + 1, 4) = TablaDI.Rows(i - 1).Cells(2).Value
            'Next

            'shXL.Range("F1:F4").Font.Bold = True
            'shXL.Range("A1:H1000").Font.Name = "Arial"
            'shXL.Range("F1:F4").Font.Size = 11
            'shXL.Range("G1:H1").Font.Bold = True
            'shXL.Range("G1:H1").Font.Size = 11
            'shXL.Range("G2:H4").Font.Size = 11
            'shXL.Range("F1:H4").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
            'shXL.Columns("F").ColumnWidth = 21
            'shXL.Range("A:D").ColumnWidth = 15
            'shXL.Range("G:H").ColumnWidth = 48
            'shXL.PageSetup.Application.ActiveWindow.DisplayGridlines = False

            ''--------------------------- Graficar Diagrama de Interacción ----------------------------
            'Dim xlCharts As Excel.ChartObjects
            'Dim MyChart As Excel.ChartObject
            'xlCharts = shXL.ChartObjects
            'MyChart = xlCharts.Add(390, 75, 595.14, 340.08)
            'objExcelChart = MyChart.Chart
            'objExcelChart.ChartType = Excel.XlChartType.xlXYScatterSmoothNoMarkers
            'objRange = shXL.Range("C1:D600")
            'objExcelChart.SetSourceData(objRange)

            '---------------------------- Exportar Datos en la hoja 2 - (RESUMEN Proyecto.Elementos) -----------------------------
            If OpcionPila.Checked = True Then
                Archivo = "RevisiónPilas"
                shxl4 = wbXL.Sheets.Add()
                shxl4 = wbXL.Sheets("Hoja2")
                Dim numfilas = TablaRevi.Rows.Count() - 1
                shxl4.Name = "Revisión Capacidad"
                shxl4.Range("A1:K1").Merge(True)
                shxl4.Cells(1, 1) = "REVISIÓN CAPACIDAD DEL ELEMENTO"
                shxl4.Range("A1:K2").Font.Bold = True
                shxl4.Range("A1:K2").Interior.Color = c
                shxl4.Range("A1:K100").Font.Name = "Arial"
                shxl4.Range("A1:K1").Font.Size = 11
                shxl4.Range("A2:K100").Font.Size = 10
                shxl4.Range("A1:K100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
                shxl4.Range("A:K").ColumnWidth = 25
                For j = 0 To numfilas - 1
                    For i = 13 To 22
                        shxl4.Cells(2, i - 11) = TablaRevi.Columns(i).HeaderText
                        shxl4.Cells(j + 3, i - 11) = TablaRevi.Rows(j).Cells(i).Value
                        shxl4.Cells(2, 1) = TablaRevi.Columns(0).HeaderText
                        shxl4.Cells(j + 3, 1) = TablaRevi.Rows(j).Cells(0).Value
                    Next
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(17).Value) >= 0.9 Then
                        shxl4.Range("F" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shxl4.Range("F" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shxl4.Range("F" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shxl4.Range("F" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(21).Value) >= 0.9 Then
                        shxl4.Range("J" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shxl4.Range("J" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shxl4.Range("J" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shxl4.Range("J" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(22).Value) >= 0.9 Then
                        shxl4.Range("K" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shxl4.Range("K" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shxl4.Range("K" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shxl4.Range("K" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                Next

                shxl4.PageSetup.Application.ActiveWindow.DisplayGridlines = False

                Dim shXL2 As Excel.Worksheet
                shXL2 = wbXL.Sheets.Add()
                shXL2 = wbXL.Sheets("Hoja3")
                shXL2.Name = "Revisión Suelo-Estructura"
                shXL2.Range("A1:G1").Merge(True)
                shXL2.Cells(1, 1) = "REVISIÓN INTERACCIÓN SUELO - ESTRUCTURA"
                shXL2.Range("A1:G100").Font.Name = "Arial"
                shXL2.Range("A1:G2").Font.Bold = True
                shXL2.Range("A1:G1").Font.Size = 11
                shXL2.Range("A2:G100").Font.Size = 10
                shXL2.Range("A1:G2").Interior.Color = c
                shXL2.Range("A1:G100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
                shXL2.Range("A:G").ColumnWidth = 30
                shXL2.PageSetup.Application.ActiveWindow.DisplayGridlines = False
                For j = 0 To numfilas - 1
                    For i = 9 To 14
                        shXL2.Cells(2, i - 7) = TablaRevi.Columns(i).HeaderText
                        shXL2.Cells(j + 3, i - 7) = TablaRevi.Rows(j).Cells(i).Value
                        shXL2.Cells(2, 1) = TablaRevi.Columns(0).HeaderText
                        shXL2.Cells(j + 3, 1) = TablaRevi.Rows(j).Cells(0).Value
                    Next
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(13).Value) >= 0.9 Then
                        shXL2.Range("F" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL2.Range("F" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL2.Range("F" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL2.Range("F" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(14).Value) >= 0.9 Then
                        shXL2.Range("G" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL2.Range("G" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL2.Range("G" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL2.Range("G" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                Next

                Dim shXL5 As Excel.Worksheet
                shXL5 = wbXL.Sheets.Add()
                shXL5 = wbXL.Sheets("Hoja4")
                shXL5.Name = "Revisión Carga Axial"
                shXL5.Range("A1:K1").Merge(True)
                shXL5.Cells(1, 1) = "REVISIÓN CARGA AXIAL EN EL ELEMENTO"
                shXL5.Range("A1:K100").Font.Name = "Arial"
                shXL5.Range("A1:K2").Font.Bold = True
                shXL5.Range("A1:K1").Font.Size = 11
                shXL5.Range("A2:K100").Font.Size = 10
                shXL5.Range("A1:K2").Interior.Color = c
                shXL5.Range("A1:K100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
                shXL5.Range("A:K").ColumnWidth = 15
                shXL5.PageSetup.Application.ActiveWindow.DisplayGridlines = False
                For j = 0 To numfilas - 1
                    For i = 0 To 10
                        shXL5.Cells(2, i + 1) = TablaRevi.Columns(i).HeaderText
                        shXL5.Cells(j + 3, i + 1) = TablaRevi.Rows(j).Cells(i).Value
                    Next
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(6).Value) >= 0.9 Then
                        shXL5.Range("G" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL5.Range("G" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL5.Range("G" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL5.Range("G" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(7).Value) >= 0.9 Then
                        shXL5.Range("H" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL5.Range("H" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL5.Range("H" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL5.Range("H" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(8).Value) >= 0.9 Then
                        shXL5.Range("I" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL5.Range("I" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL5.Range("I" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL5.Range("I" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(9).Value) >= 0.9 Then
                        shXL5.Range("J" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL5.Range("J" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL5.Range("J" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL5.Range("J" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                    If Convert.ToSingle(TablaRevi.Rows(j).Cells(10).Value) >= 0.9 Then
                        shXL5.Range("K" & 3 + j).Font.Color = Color.FromArgb(0, 97, 0)
                        shXL5.Range("K" & 3 + j).Interior.Color = Color.FromArgb(198, 239, 206)
                    Else
                        shXL5.Range("K" & 3 + j).Font.Color = Color.FromArgb(156, 0, 6)
                        shXL5.Range("K" & 3 + j).Interior.Color = Color.FromArgb(255, 199, 206)
                    End If
                Next
            End If
            '--------------------------- Imprimir resumen de Datos ----------------------------
            shXL3 = wbXL.Sheets.Add()
            If OpcionPila.Checked = True Then
                shXL3 = wbXL.Sheets("Hoja5")
            Else
                shXL3 = wbXL.Sheets("Hoja3")
            End If
            shXL3.Name = "Resumen de Datos"
            shXL3.Range("A1:F1").Merge(True)
            shXL3.Range("A1:F2").Font.Bold = True

            shXL3.Range("A1:F1").Interior.Color = c
            shXL3.Range("A2:F2").Interior.Color = c1

            shXL3.Range("A1:K50").Font.Name = "Arial"
            shXL3.Range("A1:K50").Font.Size = 11
            shXL3.Range("A1:K100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter

            shXL3.Cells(1, 1) = "Análisis de Sección Circular"

            shXL3.Cells(2, 1) = "Elementos"
            shXL3.Cells(2, 2) = "Df (m)"
            shXL3.Cells(2, 3) = "Dc (m)"
            shXL3.Cells(2, 4) = "Diametro de Barra"
            shXL3.Cells(2, 5) = "Cantidad de Barras"
            shXL3.Cells(2, 6) = "f'c (MPa)"

            For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count - 1
                shXL3.Cells(3 + i, 1) = Proyecto.Elementos.Pilas.ListaElementos(i).Name_Elemento
                shXL3.Cells(3 + i, 2) = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Df, 2)
                shXL3.Cells(3 + i, 3) = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Dc, 2)
                shXL3.Cells(3 + i, 4) = Proyecto.Elementos.Pilas.ListaElementos(i).N_Barra_Long
                shXL3.Cells(3 + i, 5) = Proyecto.Elementos.Pilas.ListaElementos(i).Cant_Barras_Long
                shXL3.Cells(3 + i, 6) = Proyecto.Elementos.Pilas.ListaElementos(i).fc

                shXL3.Range("A" & i + 3).BorderAround2(1, 2, 2, Color.FromArgb(210, 210, 210))
                shXL3.Range("B" & i + 3).BorderAround2(1, 2, 2, Color.FromArgb(210, 210, 210))
                shXL3.Range("C" & i + 3).BorderAround2(1, 2, 2, Color.FromArgb(210, 210, 210))
                shXL3.Range("D" & i + 3).BorderAround2(1, 2, 2, Color.FromArgb(210, 210, 210))
                shXL3.Range("E" & i + 3).BorderAround2(1, 2, 2, Color.FromArgb(210, 210, 210))
                shXL3.Range("F" & i + 3).BorderAround2(1, 2, 2, Color.FromArgb(210, 210, 210))
            Next

            shXL3.Range("A:F").ColumnWidth = 15

            shXL3.PageSetup.Application.ActiveWindow.DisplayGridlines = False

            Dim saveFileDialog1 As New SaveFileDialog()
            saveFileDialog1.Title = "Guardar documento Excel"
            saveFileDialog1.Filter = "Excel File|*.xlsx"
            saveFileDialog1.FileName = Convert.ToString(Archivo & "_Proyecto.Elementos -" & PagInfoGeneral.NameProject.Text)
            saveFileDialog1.ShowDialog()
            wbXL.SaveAs(saveFileDialog1.FileName)
            appXL.Workbooks.Close()
            appXL.Quit()
            System.Diagnostics.Process.Start(saveFileDialog1.FileName)
        Catch ex As Exception
            MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            conexion.Close()
            Cursor = Cursors.Arrow
        End Try
    End Sub

    '---------------------- LLAMAR Y LLENAR CADA DATAGRIDVIEW CON LAS TABLAS DE EXCEL ------------------------------- 
    Private Sub TipoFrameToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Importar_Frame.Click
        Proyecto.Elementos.Pilas.Opcion_Elemento = "Frame"
        Abrir_Importar_Excel()
    End Sub
    Private Sub TipoPuntoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Importar_Puntos.Click
        Proyecto.Elementos.Pilas.Opcion_Elemento = "Punto"
        Abrir_Importar_Excel()
    End Sub
    Private Sub TipoPierToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Importar_Pier.Click
        Proyecto.Elementos.Pilas.Opcion_Elemento = "Pier"
        Abrir_Importar_Excel()
    End Sub

    Private Sub SeccionC_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        Try
            pictureBox4.Location = New Point(Panel2.Width / 2 - pictureBox4.Width / 2, 11)
        Catch ex As Exception

        End Try
    End Sub
    Private Sub ComboElementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboElementos.SelectedIndexChanged
        Dim FT As Double = 1
        If Proyecto.Elementos.Pilas.Opcion_Elemento = "Pier" Then
            FT = -1
        End If
        If Proyecto.Elementos.Pilas.Opcion_Elemento = "Punto" Then
            FT = 1
        End If

        Try
            TablaDI.Rows.Clear()

            ResumenDI.Rows.Clear()
            ResumenDI.Rows.Add()
            ResumenDI.Rows.Add()

            Dim series As New Series
            Dim Serie2 As New Series
            Dim Demandas As New Series
            Grafica1.Series.Clear()
            Grafica1.Series.Add(series)
            Grafica1.Series.Add(Serie2)
            Grafica1.Series.Add(Demandas)
            Grafica1.ChartAreas(0).BackColor = Color.White
            Grafica1.Location = New Point(10, 250)
            Grafica1.Size = New Size(Panel2.Width - 25, Panel2.Height - 250)
            Grafica1.Visible = True
            series.ChartType = SeriesChartType.Spline
            Serie2.ChartType = SeriesChartType.Spline
            Demandas.ChartType = SeriesChartType.Point
            Demandas.MarkerStyle = MarkerStyle.Circle
            series.BorderWidth = 2
            Serie2.BorderWidth = 2
            Serie2.Color = Color.Gray
            series.Color = Color.Orange
            Demandas.Color = Color.Black
            Demandas.BorderWidth = 2

            Dim Mm As Integer = 0

            For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiMn.Count - 1
                series.Points.AddXY(Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiMn(i), Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiPn(i))
                Serie2.Points.AddXY(Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_Mn(i), Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_Pn(i))
                TablaDI.Rows.Add(Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_Pn(i), Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_Mn(i), Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiPn(i), Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiMn(i))
                If Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiMn.Max = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiMn(i) Then
                    Mm = i
                End If
            Next

            For fi = 0 To Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_MU.Count - 1
                Demandas.Points.AddXY(Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_MU(fi), FT * Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_PU(fi))
            Next

            ResumenDI.Rows(0).Cells(0).Value = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiPn.Max
            ResumenDI.Rows(0).Cells(1).Value = 0

            ResumenDI.Rows(1).Cells(0).Value = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiPn(Mm)
            ResumenDI.Rows(1).Cells(1).Value = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiMn.Max

            ResumenDI.Rows(2).Cells(0).Value = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text).Matriz_DI_PhiPn.Min
            ResumenDI.Rows(2).Cells(1).Value = 0

            Panel2.Controls.Add(pictureBox4)
            pictureBox4.Refresh()
            pictureBox4.Size = New Size(297, 204)
            pictureBox4.Location = New Point(Panel2.Width / 2 - pictureBox4.Width / 2, 11)
            AddHandler pictureBox4.Paint, AddressOf PictureBox4_Paint

            Dim Pila As Elemento_Pila = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Elemento = ComboElementos.Text)
            Diametro.Text = Pila.Df
            Dc.Text = Pila.Dc
            NumRLong.Text = Pila.Cant_Barras_Long
            RefuerzoLong.Text = Pila.N_Barra_Long

        Catch ex As Exception

        End Try
    End Sub
    Private Sub RefuerzoLong_SelectedIndexChanged(sender As Object, e As EventArgs) Handles RefuerzoLong.SelectedIndexChanged
        Try
            If RefuerzoLong.Text <> "Usuario" Then
                AreaLongitudinal.Text = AreaRefuerzo(RefuerzoLong.Text)
            Else
                AreaLongitudinal.Text = 199
            End If
        Catch ex As Exception

        End Try
    End Sub
    Private Sub Op_Seccion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Op_Seccion.SelectedIndexChanged
        Try
            If Op_Seccion.Text = "Maciza" Then
                T_Espesor.ReadOnly = True
            Else
                T_Espesor.ReadOnly = False
            End If


            'If Op_Seccion.Text = "Maciza" Then
            '    GroupBox1.Size = New Size(285, 110)
            '    Label3.Top = 53
            '    Recubrimiento.Top = 50
            '    Label5.Top = 53
            '    Label11.Top = 81
            '    Dc.Top = 78
            '    Label12.Top = 81
            '    Label16.Top = 109
            '    T_Espesor.Top = 106
            '    Label15.Top = 109

            '    Label16.Visible = False
            '    T_Espesor.Visible = False
            '    Label15.Visible = False
            '    GroupBox1.Location = New Point(10, GroupBox4.Top + GroupBox4.Height + 10)
            '    GroupBox2.Location = New Point(10, GroupBox1.Top + GroupBox1.Height + 10)
            '    GroupBox6.Location = New Point(10, GroupBox2.Top + GroupBox2.Height + 10)
            'Else

            '    Label3.Top = 81
            '    Recubrimiento.Top = 78
            '    Label5.Top = 81

            '    Label11.Top = 109
            '    Dc.Top = 106
            '    Label12.Top = 109

            '    Label16.Top = 53
            '    T_Espesor.Top = 50
            '    Label15.Top = 53

            '    Label16.Visible = True
            '    T_Espesor.Visible = True
            '    Label15.Visible = True

            '    GroupBox1.Size = New Size(285, 140)
            '    GroupBox1.Location = New Point(10, GroupBox4.Top + GroupBox4.Height + 10)
            '    GroupBox2.Location = New Point(10, GroupBox1.Top + GroupBox1.Height + 10)
            '    GroupBox6.Location = New Point(10, GroupBox2.Top + GroupBox2.Height + 10)

            'End If
        Catch ex As Exception

        End Try
    End Sub

    Public Sub SaveAs(ByVal Objeto As Object)
        Try
            Dim dlg As New SaveFileDialog
            dlg.Filter = "Archivo|*.esm"
            dlg.Title = "Guardar Archivo"
            dlg.FileName = "Proyecto - " & If(Proyecto.Info?.Nombre, "ARCO")
            If dlg.ShowDialog() <> DialogResult.OK Then Exit Sub
            Proyecto.Ruta = Path.GetFullPath(dlg.FileName)
            Form_00_PaginaPrincipal.proyecto = Proyecto
            Funciones_Programa.Serializar(dlg.FileName, Objeto)
            _hayCambiosPilas = False
            _ultimoGuardadoPilas = DateTime.Now
        Catch ex As Exception
            MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Sub Open()
        Dim dlg As New OpenFileDialog
        dlg.Filter = "Archivo|*.esm"
        dlg.Title = "Abrir Archivo"
        If dlg.ShowDialog() <> DialogResult.OK Then Exit Sub

        Try
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(dlg.FileName)
        Catch
            Try
                Dim elementos = Funciones_Programa.DeSerializar(Of cElementos)(dlg.FileName)
                Proyecto = New Proyecto()
                Proyecto.Elementos = elementos
            Catch ex As Exception
                MessageBox.Show("No se pudo abrir el archivo." & vbCrLf & ex.Message,
                                "Error al abrir", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try
        End Try

        Proyecto.Ruta = dlg.FileName
        Form_00_PaginaPrincipal.proyecto = Proyecto
        _hayCambiosPilas = False

        BorrarDatos()
        Rellenar()
        Form_01_00_PagInfoPilas.Show()
        Tabla_ResumenVisual.Visible = True
        Label20.Visible = True
        ResumenDI.Visible = True
        VerToolStripMenuItem.Enabled = True
        ExportarToolStripMenuItem.Enabled = True
    End Sub

    Private Sub GuardarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Save_Pilas.Click
        If String.IsNullOrEmpty(Proyecto.Ruta) Then
            SaveAs(Proyecto)
        Else
            Try
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
                _hayCambiosPilas = False
                _ultimoGuardadoPilas = DateTime.Now
            Catch ex As Exception
                MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub
    Private Sub GuardarComoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAs_Pilas.Click
        SaveAs(Proyecto)
    End Sub
    Private Sub AbrirToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Open_Pilas.Click
        Open()
    End Sub

    Public Sub Rellenar()
        TablaRevi.Columns(1).HeaderText = "Ps Estática [kN]"
        TablaRevi.Columns(2).HeaderText = "Ps Dinámica [kN]"
        TablaRevi.Columns(3).HeaderText = "Pu Estática [kN]"
        TablaRevi.Columns(4).HeaderText = "Pu Dinámica [kN]"
        TablaRevi.Columns(5).HeaderText = "Pu Tracción [kN]"
        TablaRevi.Columns(6).HeaderText = "Chequeo 1 (Ps E)"
        TablaRevi.Columns(7).HeaderText = "Chequeo 2 (Ps D)"
        TablaRevi.Columns(8).HeaderText = "Chequeo 3 (Pu E)"
        TablaRevi.Columns(9).HeaderText = "Chequeo 4 (Pu D)"
        TablaRevi.Columns(10).HeaderText = "Chequeo 5 (Pu T)"
        TablaRevi.Columns(11).HeaderText = "σ Transmitido Estático [kN/m2]"
        TablaRevi.Columns(12).HeaderText = "σ Transmitido Dinámico [kN/m2]"
        TablaRevi.Columns(13).HeaderText = "σAdm/σTrans Estático"
        TablaRevi.Columns(14).HeaderText = "σAdm/σTrans Dinámico"
        TablaRevi.Columns(15).HeaderText = "φVn [kN]"
        TablaRevi.Columns(16).HeaderText = "Vu [kN]"
        TablaRevi.Columns(17).HeaderText = "φVn/Vu"
        TablaRevi.Columns(18).HeaderText = "Chequeo V2"
        TablaRevi.Columns(19).HeaderText = "Chequeo V3"
        TablaRevi.Columns(20).HeaderText = "ρ Col [%]"
        TablaRevi.Columns(21).HeaderText = "Capacidad/Demanda (Cortes)"
        TablaRevi.Columns(22).HeaderText = "Capacidad/Demanda (Recta)"

        For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count() - 1
            TablaRevi.Rows.Add()
            Tabla_ResumenVisual.Rows.Add()
            ComboElementos.Items.Add(Proyecto.Elementos.Pilas.ListaElementos(i).Name_Elemento)
            Form_01_00_PagInfoPilas.Tabla_Elementos.Rows.Add()
        Next

        For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count() - 1
            Dim p = Proyecto.Elementos.Pilas.ListaElementos(i)

            ' ── TablaRevi ───────────────────────────────────────────────────
            TablaRevi.Rows(i).Cells(0).Value = p.Name_Elemento
            TablaRevi.Rows(i).Cells(1).Value = p.Ps_Estatica
            TablaRevi.Rows(i).Cells(2).Value = p.Ps_Dinamica
            TablaRevi.Rows(i).Cells(3).Value = p.Pu_Estatica
            TablaRevi.Rows(i).Cells(4).Value = p.Pu_Dinamica
            TablaRevi.Rows(i).Cells(5).Value = p.P_Traccion
            TablaRevi.Rows(i).Cells(6).Value = p.Check1_PsE
            TablaRevi.Rows(i).Cells(7).Value = p.Check2_PsD
            TablaRevi.Rows(i).Cells(8).Value = p.Check3_PuE
            TablaRevi.Rows(i).Cells(9).Value = p.Check4_PuD
            TablaRevi.Rows(i).Cells(10).Value = p.Check5_PuT
            TablaRevi.Rows(i).Cells(11).Value = p.EsfE_Trans
            TablaRevi.Rows(i).Cells(12).Value = p.EsfD_Trans
            TablaRevi.Rows(i).Cells(13).Value = p.Relacion_EsfE
            TablaRevi.Rows(i).Cells(14).Value = p.Relacion_EsfD
            TablaRevi.Rows(i).Cells(15).Value = p.Vn
            TablaRevi.Rows(i).Cells(16).Value = p.Vu
            TablaRevi.Rows(i).Cells(17).Value = p.FactorShear
            TablaRevi.Rows(i).Cells(18).Value = p.Check_V2
            TablaRevi.Rows(i).Cells(19).Value = p.Check_V3
            TablaRevi.Rows(i).Cells(20).Value = p.Cuantia
            TablaRevi.Rows(i).Cells(21).Value = p.Factor_CortesH
            TablaRevi.Rows(i).Cells(22).Value = p.Factor_Diagonal

            ' ── Tabla_Elementos (Form_01_00_PagInfoPilas) ───────────────────
            ' Col 0: Name_Label | 1: Nombre | 2: Df | 3: Dc | 4: L_Pila
            ' Col 5: Opcion_Hueca (ComboBox Si/No) | 6: Esp_Anillo
            ' Col 7: N_Barra_Long (ComboBox #2..#10) | 8: Área | 9: Cant | 10: fc
            Dim te = Form_01_00_PagInfoPilas.Tabla_Elementos.Rows(i)
            te.Cells(0).Value = p.Name_Label
            te.Cells(1).Value = p.Name_Elemento
            te.Cells(2).Value = p.Df
            te.Cells(3).Value = p.Dc
            te.Cells(4).Value = p.L_Pila
            te.Cells(5).Value = p.Opcion_Hueca
            te.Cells(6).Value = p.Esp_Anillo
            te.Cells(7).Value = p.N_Barra_Long
            te.Cells(8).Value = p.Acero_Long
            te.Cells(9).Value = p.Cant_Barras_Long
            te.Cells(10).Value = p.fc
        Next
        ' ── Tabla_ResumenVisual con colores (igual que Button1_Click) ──────────────
        For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count() - 1
            Dim p = Proyecto.Elementos.Pilas.ListaElementos(i)

            ' Color coding TablaRevi
            For j = 6 To 22
                If j <> 11 And j <> 12 And j <> 15 And j <> 16 And j <> 18 And j <> 19 And j <> 20 Then
                    Dim v As Object = TablaRevi.Rows(i).Cells(j).Value
                    Dim vd As Double = If(v Is Nothing, 0, Convert.ToDouble(v))
                    If vd >= 0.9 Then
                        TablaRevi.Rows(i).Cells(j).Style.BackColor = Color.FromArgb(198, 239, 206)
                        TablaRevi.Rows(i).Cells(j).Style.ForeColor = Color.FromArgb(0, 97, 0)
                    Else
                        TablaRevi.Rows(i).Cells(j).Style.BackColor = Color.FromArgb(255, 199, 206)
                        TablaRevi.Rows(i).Cells(j).Style.ForeColor = Color.FromArgb(156, 0, 6)
                    End If
                End If
            Next

            Tabla_ResumenVisual.Rows(i).Cells(0).Value = p.Name_Elemento

            Dim colorOk As Color = Color.FromArgb(198, 239, 206)
            Dim colorOkFG As Color = Color.FromArgb(0, 97, 0)
            Dim colorFail As Color = Color.FromArgb(255, 199, 206)
            Dim colorFailFG As Color = Color.FromArgb(156, 0, 6)

            ' Col 1: Cargas
            If p.Check1_PsE >= 0.9 And p.Check2_PsD >= 0.9 And p.Check3_PuE >= 0.9 And p.Check4_PuD >= 0.9 Then
                Tabla_ResumenVisual.Rows(i).Cells(1).Value = "Ok"
                Tabla_ResumenVisual.Rows(i).Cells(1).Style.BackColor = colorOk
                Tabla_ResumenVisual.Rows(i).Cells(1).Style.ForeColor = colorOkFG
            Else
                Tabla_ResumenVisual.Rows(i).Cells(1).Value = "Revisar"
                Tabla_ResumenVisual.Rows(i).Cells(1).Style.BackColor = colorFail
                Tabla_ResumenVisual.Rows(i).Cells(1).Style.ForeColor = colorFailFG
            End If
            ' Col 2: Suelo
            If p.Relacion_EsfD >= 0.9 And p.Relacion_EsfE >= 0.9 Then
                Tabla_ResumenVisual.Rows(i).Cells(2).Value = "Ok"
                Tabla_ResumenVisual.Rows(i).Cells(2).Style.BackColor = colorOk
                Tabla_ResumenVisual.Rows(i).Cells(2).Style.ForeColor = colorOkFG
            Else
                Tabla_ResumenVisual.Rows(i).Cells(2).Value = "Revisar"
                Tabla_ResumenVisual.Rows(i).Cells(2).Style.BackColor = colorFail
                Tabla_ResumenVisual.Rows(i).Cells(2).Style.ForeColor = colorFailFG
            End If
            ' Col 3: Cortante
            If p.FactorShear >= 0.9 Then
                Tabla_ResumenVisual.Rows(i).Cells(3).Value = "Ok"
                Tabla_ResumenVisual.Rows(i).Cells(3).Style.BackColor = colorOk
                Tabla_ResumenVisual.Rows(i).Cells(3).Style.ForeColor = colorOkFG
            Else
                Tabla_ResumenVisual.Rows(i).Cells(3).Value = "Revisar"
                Tabla_ResumenVisual.Rows(i).Cells(3).Style.BackColor = colorFail
                Tabla_ResumenVisual.Rows(i).Cells(3).Style.ForeColor = colorFailFG
            End If
            ' Col 4: Interacción
            If p.Factor_CortesH >= 0.9 And p.Factor_Diagonal >= 0.9 Then
                Tabla_ResumenVisual.Rows(i).Cells(4).Value = "Ok"
                Tabla_ResumenVisual.Rows(i).Cells(4).Style.BackColor = colorOk
                Tabla_ResumenVisual.Rows(i).Cells(4).Style.ForeColor = colorOkFG
            Else
                Tabla_ResumenVisual.Rows(i).Cells(4).Value = "Revisar"
                Tabla_ResumenVisual.Rows(i).Cells(4).Style.BackColor = colorFail
                Tabla_ResumenVisual.Rows(i).Cells(4).Style.ForeColor = colorFailFG
            End If
        Next

        ' ── Recalcular matrices DI para proyectos guardados sin ellas ───────────
        For Each p In Proyecto.Elementos.Pilas.ListaElementos
            If (p.Matriz_DI_PhiPn Is Nothing OrElse p.Matriz_DI_PhiPn.Count = 0) AndAlso
               Not String.IsNullOrEmpty(p.N_Barra_Long) Then
                Dim fy As Single = If(Proyecto.Elementos.Pilas.Fy > 0, Proyecto.Elementos.Pilas.Fy, 420)
                Dim es As Single = If(Proyecto.Elementos.Pilas.ModuloE_Acero > 0, Proyecto.Elementos.Pilas.ModuloE_Acero, 200000)
                Dim ecu As Single = If(Proyecto.Elementos.Pilas.Def_Uni_ConcAs > 0, Proyecto.Elementos.Pilas.Def_Uni_ConcAs, 0.003)
                Dim di = DiagramaInteraccionCircular(p.Df, 0.075, p.N_Barra_Long, p.Acero_Long, p.Cant_Barras_Long, p.fc, es, ecu, fy)
                p.Matriz_DI_Mn = New List(Of Single)
                p.Matriz_DI_Pn = New List(Of Single)
                p.Matriz_DI_PhiMn = New List(Of Single)
                p.Matriz_DI_PhiPn = New List(Of Single)
                For j = 1 To CInt(di(1, 5))
                    p.Matriz_DI_Mn.Add(CSng(di(j, 2)))
                    p.Matriz_DI_Pn.Add(CSng(di(j, 1)))
                    p.Matriz_DI_PhiMn.Add(CSng(di(j, 4)))
                    p.Matriz_DI_PhiPn.Add(CSng(di(j, 3)))
                Next
            End If
        Next

        ComboElementos.Text = Proyecto.Elementos.Pilas.ListaElementos(0).Name_Elemento
        ComboElementos.Visible = True

        EadmEst.Text = Proyecto.Elementos.Pilas.Esf_Adm_Est
        EadmDin.Text = Proyecto.Elementos.Pilas.Esf_Adm_Din

    End Sub
    Public Sub BorrarDatos()
        TablaRevi.Rows.Clear()
        Tabla_ResumenVisual.Rows.Clear()
        Form_01_00_PagInfoPilas.Tabla_Elementos.Rows.Clear()

    End Sub

    '------------------------ FUNCIONES PARA CONECTAR DATAGRIVIEW A TABLAS DE EXCEL ---------------------------- 
    '-------------------------- IMPORTAR CARGAS DE SERVICIO DE EXCEL --------------------------
    Public Function ImportExcellToDataGridView_CServicio(ByRef path As String, ByVal Datagrid As DataGridView)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            Dim Cmd As New OleDbCommand("Select * From [CargasServicio$]")
            cnConex.Open()
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function
    '-------------------------- IMPORTAR CARGAS ÚLTIMAS DE EXCEL --------------------------
    Public Function ImportExcellToDataGridView_CUltimas(ByRef path As String, ByVal Datagrid As DataGridView)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            Dim Cmd As New OleDbCommand("Select * From [CargasUltimas$]")
            cnConex.Open()
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function

    Private Sub ActualizarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ActualizarDemandasToolStripMenuItem.Click
        If Proyecto.Elementos.Pilas.Lista_Combinaciones.Count = 0 Then
            MsgBox("No hay combinaciones cargadas. Importe las demandas primero.", MsgBoxStyle.Exclamation)
            Return
        End If
        MostrarSelectorCombinacionesPilas()
    End Sub

    Private Sub MostrarSelectorCombinacionesPilas()
        Dim combos = Proyecto.Elementos.Pilas.Lista_Combinaciones

        Dim fGS As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Servicio.Contains(c) Then fGS.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Servicio
            fGS.Lista_Cargas_Design.Items.Add(c)
        Next
        fGS.OpcionLlamado = "Pilas" : fGS.Evaluacion = "Grav_Servicio"
        fGS.GroupBox2.Text = "Combinaciones Gravitacionales de Servicio"
        fGS.ShowDialog()

        Dim fGD As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Design.Contains(c) Then fGD.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Design
            fGD.Lista_Cargas_Design.Items.Add(c)
        Next
        fGD.OpcionLlamado = "Pilas" : fGD.Evaluacion = "Grav_Design"
        fGD.GroupBox2.Text = "Combinaciones Gravitacionales de Diseño"
        fGD.ShowDialog()

        Dim fSS As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Servicio.Contains(c) Then fSS.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Servicio
            fSS.Lista_Cargas_Design.Items.Add(c)
        Next
        fSS.OpcionLlamado = "Pilas" : fSS.Evaluacion = "Sismicas_Servicio"
        fSS.GroupBox2.Text = "Combinaciones Sísmicas de Servicio"
        fSS.ShowDialog()

        Dim fSD As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Design.Contains(c) Then fSD.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Design
            fSD.Lista_Cargas_Design.Items.Add(c)
        Next
        fSD.OpcionLlamado = "Pilas" : fSD.Evaluacion = "Sismicas_Design"
        fSD.GroupBox2.Text = "Combinaciones Sísmicas de Diseño"
        fSD.ShowDialog()

        Dim fTR As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Pilas.Lista_Combinaciones_Traccion.Contains(c) Then fTR.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Pilas.Lista_Combinaciones_Traccion
            fTR.Lista_Cargas_Design.Items.Add(c)
        Next
        fTR.OpcionLlamado = "Pilas" : fTR.Evaluacion = "Comb_Traccion"
        fTR.GroupBox2.Text = "Combinaciones de Tracción"
        fTR.ShowDialog()
    End Sub

    Private Sub ImportarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ImportarDemandasToolStripMenuItem.Click

        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivo de resultados ETABS"
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*"
            .Multiselect = False

            If .ShowDialog() = DialogResult.OK Then
                Dim path As String = .FileName
                Me.Cursor = Cursors.WaitCursor

                Try
                    ' Leer cada hoja
                    Proyecto.Elementos.Pilas.Tabla_JointReactions = LeerHojaExcel(path, "Joint Reactions")

                    MsgBox("Importación completada correctamente.", MsgBoxStyle.Information)

                    Proyecto.Elementos.Pilas.Reactions = DataTableToReactions(Proyecto.Elementos.Pilas.Tabla_JointReactions)
                    _hayCambiosPilas = True

                    ' 🔹 Extraer combinaciones únicas
                    Proyecto.Elementos.Pilas.Lista_Combinaciones = Proyecto.Elementos.Pilas.Reactions.Select(Function(r) r.LoadCase) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()


                    MostrarSelectorCombinacionesPilas()

                    MsgBox("Importación completada.", MsgBoxStyle.Information)

                Catch ex As Exception
                    MsgBox("Error al importar: " & ex.Message, MsgBoxStyle.Critical)
                Finally
                    Me.Cursor = Cursors.Arrow
                End Try
            End If
        End With





    End Sub

    Private Sub ReporteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ReporteToolStripMenuItem.Click
        Dim frm As New Form_Reporte_Pilas()
        frm.Show()
    End Sub

    Private Sub DiagramaDiametroToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DiagramaDiametroToolStripMenuItem.Click
        Dim frm As New Form_DiagramaInteraccionPilas_Resumen()
        frm.Show()
    End Sub

    Private _hayCambiosPilas As Boolean = False
    Private _ultimoGuardadoPilas As DateTime = DateTime.Now
    Private _timerAutoSavePilas As New Timer With {.Interval = 60000}

    Private Sub Form_01_PagPilas_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim itemAyuda As New ToolStripMenuItem("? Tablas ETABS")
        itemAyuda.ForeColor = Color.White
        itemAyuda.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler itemAyuda.Click, Sub(s, ev) Form_AyudaImportacion.MostrarModulo("Pilas")
        MenuStrip1.Items.Add(itemAyuda)

        AddHandler _timerAutoSavePilas.Tick, AddressOf AutoSavePilas_Tick
        _timerAutoSavePilas.Start()
    End Sub

    Private Sub AutoSavePilas_Tick(sender As Object, e As EventArgs)
        If Not _hayCambiosPilas Then Exit Sub
        If String.IsNullOrEmpty(Proyecto.Ruta) Then Exit Sub
        If (DateTime.Now - _ultimoGuardadoPilas).TotalMinutes < 10 Then Exit Sub
        Try
            Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            _ultimoGuardadoPilas = DateTime.Now
            _hayCambiosPilas = False
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Llamado por la página principal al abrir un proyecto.
    ''' Re-sincroniza la referencia y recalcula si hay datos de pilas.
    ''' </summary>
    Public Sub RefrescarDesdeProyecto()
        Proyecto = Form_00_PaginaPrincipal.proyecto
        If Proyecto.Elementos.Pilas.ListaElementos.Count = 0 Then Return
        If Proyecto.Elementos.Pilas.Reactions.Count = 0 Then Return
        Button1_Click(Nothing, EventArgs.Empty)
    End Sub

    Private Sub Form_01_PagPilas_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not _hayCambiosPilas Then Exit Sub
Dim r = MessageBox.Show("Hay cambios sin guardar. ¿Guardar antes de cerrar?",
                                "Cerrar", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)
        If r = DialogResult.Yes Then GuardarToolStripMenuItem_Click(sender, e)
        If r = DialogResult.Cancel Then e.Cancel = True
    End Sub

End Class