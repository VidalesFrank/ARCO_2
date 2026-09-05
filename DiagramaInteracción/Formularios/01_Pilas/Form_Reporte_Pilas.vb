Imports System.Drawing
Imports ClosedXML.Excel

Public Class Form_Reporte_Pilas
    Inherits Form

    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")

    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")

    Private WithEvents DgvResumen As New DataGridView()
    Private WithEvents DgvCapacidad As New DataGridView()
    Private WithEvents DgvInteraccion As New DataGridView()

    Private _tabs As TabControl

    Public Sub New()
        Me.Text = "Reporte — Diseño de Pilas"
        Me.Size = New Size(1300, 700)
        Me.MinimumSize = New Size(900, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        _tabs = New TabControl() With {.Dock = DockStyle.Fill}
        _tabs.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _tabs.Padding = New Point(16, 6)

        Dim tabRes As New TabPage("  Resumen  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvResumen.Dock = DockStyle.Fill
        EstilarGrid(DgvResumen)
        tabRes.Controls.Add(DgvResumen)
        _tabs.TabPages.Add(tabRes)

        Dim tabCap As New TabPage("  Capacidad de Carga  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvCapacidad.Dock = DockStyle.Fill
        EstilarGrid(DgvCapacidad)
        tabCap.Controls.Add(DgvCapacidad)
        _tabs.TabPages.Add(tabCap)

        Dim tabInt As New TabPage("  Cortante e Interacción  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvInteraccion.Dock = DockStyle.Fill
        EstilarGrid(DgvInteraccion)
        tabInt.Controls.Add(DgvInteraccion)
        _tabs.TabPages.Add(tabInt)

        Me.Controls.Add(_tabs)

        Dim barra As New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 9, 10, 9)
        }
        Dim btnActualizar As New Button() With {
            .Text = "Actualizar", .Size = New Size(120, 36), .Location = New Point(10, 9),
            .FlatStyle = FlatStyle.Flat, .BackColor = ColorEncabezado,
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand
        }
        btnActualizar.FlatAppearance.BorderSize = 0
        AddHandler btnActualizar.Click, Sub(s, ev) CargarTodo()
        barra.Controls.Add(btnActualizar)

        Dim btnExportar As New Button() With {
            .Text = "Exportar a Excel", .Size = New Size(160, 36), .Location = New Point(140, 9),
            .FlatStyle = FlatStyle.Flat, .BackColor = Color.FromArgb(21, 130, 70),
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand
        }
        btnExportar.FlatAppearance.BorderSize = 0
        AddHandler btnExportar.Click, AddressOf BtnExportar_Click
        barra.Controls.Add(btnExportar)
        Me.Controls.Add(barra)

        AddHandler Me.Load, AddressOf Form_Load
    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)
        CargarTodo()
    End Sub

    Private Sub CargarTodo()
        Dim pilas = Form_01_PagPilas.Proyecto.Elementos.Pilas.ListaElementos
        If pilas Is Nothing OrElse pilas.Count = 0 Then Return
        CargarResumen(pilas)
        CargarCapacidad(pilas)
        CargarInteraccion(pilas)
    End Sub

    ' ── TAB 1: RESUMEN ────────────────────────────────────────────────────────

    Private Sub CargarResumen(pilas As List(Of Elemento_Pila))
        Dim dgv = DgvResumen
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Elem", "Elemento", 120)
        Col(dgv, "Df", "Df [m]", 70)
        Col(dgv, "Dc", "Dc [m]", 70)
        Col(dgv, "L", "L [m]", 70)
        Col(dgv, "Ref", "Refuerzo", 90)
        Col(dgv, "fc", "fc [MPa]", 70)
        Col(dgv, "Cargas", "Cargas", 80)
        Col(dgv, "Suelo", "Suelo", 80)
        Col(dgv, "Cortante", "Cortante", 80)
        Col(dgv, "Interaccion", "Interacción", 90)

        For i = 0 To pilas.Count - 1
            Dim p = pilas(i)
            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)
            row.Cells("Elem").Value = p.Name_Elemento
            row.Cells("Df").Value = Math.Round(p.Df, 2)
            row.Cells("Dc").Value = Math.Round(CDbl(p.Dc), 2)
            row.Cells("L").Value = Math.Round(CDbl(p.L_Pila), 2)
            row.Cells("Ref").Value = p.N_Barra_Long & " × " & p.Cant_Barras_Long
            row.Cells("fc").Value = p.fc
            If i Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)

            Dim okCargas = p.Check1_PsE >= 0.9 AndAlso p.Check2_PsD >= 0.9 AndAlso p.Check3_PuE >= 0.9 AndAlso p.Check4_PuD >= 0.9
            Dim okSuelo = p.Relacion_EsfE >= 0.9 AndAlso p.Relacion_EsfD >= 0.9
            Dim okCortante = p.FactorShear >= 0.9
            Dim fManR1 As Single = p.Factor_Manual_DI
            Dim fDiagR1 As Single = If(fManR1 > 0, fManR1, p.Factor_Diagonal)
            Dim fCortR1 As Single = If(fManR1 > 0, fManR1, p.Factor_CortesH)
            Dim okInteraccion = fDiagR1 >= 0.9 AndAlso fCortR1 >= 0.9

            AsignarOk(row.Cells("Cargas"), okCargas)
            AsignarOk(row.Cells("Suelo"), okSuelo)
            AsignarOk(row.Cells("Cortante"), okCortante)
            AsignarOk(row.Cells("Interaccion"), okInteraccion)
        Next
    End Sub

    ' ── TAB 2: CAPACIDAD DE CARGA ─────────────────────────────────────────────

    Private Sub CargarCapacidad(pilas As List(Of Elemento_Pila))
        Dim dgv = DgvCapacidad
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Elem", "Elemento", 120)
        Col(dgv, "PsE", "Ps Est. [kN]", 100)
        Col(dgv, "PsD", "Ps Din. [kN]", 100)
        Col(dgv, "PuE", "Pu Est. [kN]", 100)
        Col(dgv, "PuD", "Pu Din. [kN]", 100)
        Col(dgv, "PuT", "Pu Trac. [kN]", 105)
        Col(dgv, "C1", "Ch1 (PsE)", 85)
        Col(dgv, "C2", "Ch2 (PsD)", 85)
        Col(dgv, "C3", "Ch3 (PuE)", 85)
        Col(dgv, "C4", "Ch4 (PuD)", 85)
        Col(dgv, "C5", "Ch5 (PuT)", 85)
        Col(dgv, "EsfE", "σ Est. [kPa]", 100)
        Col(dgv, "EsfD", "σ Din. [kPa]", 100)
        Col(dgv, "RelE", "σAdm/σ Est.", 95)
        Col(dgv, "RelD", "σAdm/σ Din.", 95)

        For i = 0 To pilas.Count - 1
            Dim p = pilas(i)
            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)
            row.Cells("Elem").Value = p.Name_Elemento
            row.Cells("PsE").Value = Math.Round(CDbl(p.Ps_Estatica), 2)
            row.Cells("PsD").Value = Math.Round(CDbl(p.Ps_Dinamica), 2)
            row.Cells("PuE").Value = Math.Round(CDbl(p.Pu_Estatica), 2)
            row.Cells("PuD").Value = Math.Round(CDbl(p.Pu_Dinamica), 2)
            row.Cells("PuT").Value = Math.Round(CDbl(p.P_Traccion), 2)
            If i Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)

            AsignarFactor(row.Cells("C1"), p.Check1_PsE)
            AsignarFactor(row.Cells("C2"), p.Check2_PsD)
            AsignarFactor(row.Cells("C3"), p.Check3_PuE)
            AsignarFactor(row.Cells("C4"), p.Check4_PuD)
            AsignarFactor(row.Cells("C5"), p.Check5_PuT)
            row.Cells("EsfE").Value = Math.Round(CDbl(p.EsfE_Trans), 2)
            row.Cells("EsfD").Value = Math.Round(CDbl(p.EsfD_Trans), 2)
            AsignarFactor(row.Cells("RelE"), p.Relacion_EsfE)
            AsignarFactor(row.Cells("RelD"), p.Relacion_EsfD)
        Next
    End Sub

    ' ── TAB 3: CORTANTE E INTERACCIÓN ────────────────────────────────────────

    Private Sub CargarInteraccion(pilas As List(Of Elemento_Pila))
        Dim dgv = DgvInteraccion
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Elem", "Elemento", 120)
        Col(dgv, "Df", "Df [m]", 70)
        Col(dgv, "NBar", "Barra", 70)
        Col(dgv, "Cant", "# Barras", 75)
        Col(dgv, "Rho", "ρ [%]", 70)
        Col(dgv, "Vn", "φVn [kN]", 90)
        Col(dgv, "Vu", "Vu [kN]", 90)
        Col(dgv, "FV", "φVn/Vu", 80)
        Col(dgv, "CV2", "Chequeo V2", 95)
        Col(dgv, "CV3", "Chequeo V3", 95)
        Col(dgv, "FCortes", "F Cortes H", 90)
        Col(dgv, "FDiag", "F Recta", 80)

        For i = 0 To pilas.Count - 1
            Dim p = pilas(i)
            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)
            row.Cells("Elem").Value = p.Name_Elemento
            row.Cells("Df").Value = Math.Round(p.Df, 2)
            row.Cells("NBar").Value = p.N_Barra_Long
            row.Cells("Cant").Value = p.Cant_Barras_Long
            row.Cells("Rho").Value = Math.Round(CDbl(p.Cuantia) * 100, 3)
            row.Cells("Vn").Value = Math.Round(CDbl(p.Vn), 2)
            row.Cells("Vu").Value = Math.Round(CDbl(p.Vu), 2)
            If i Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)

            AsignarFactor(row.Cells("FV"), p.FactorShear)
            row.Cells("CV2").Value = p.Check_V2
            row.Cells("CV3").Value = p.Check_V3
            Dim fManR3 As Single = p.Factor_Manual_DI
            Dim fCortR3 As Single = If(fManR3 > 0, fManR3, p.Factor_CortesH)
            Dim fDiagR3 As Single = If(fManR3 > 0, fManR3, p.Factor_Diagonal)
            AsignarFactor(row.Cells("FCortes"), fCortR3)
            AsignarFactor(row.Cells("FDiag"), fDiagR3)
        Next
    End Sub

    ' ── HELPERS ───────────────────────────────────────────────────────────────

    Private Sub EstilarGrid(dgv As DataGridView)
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.RowHeadersVisible = False
        dgv.BorderStyle = BorderStyle.None
        dgv.GridColor = Color.FromArgb(220, 220, 220)
        dgv.BackgroundColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorEncabezado
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5, FontStyle.Bold)
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.ColumnHeadersHeight = 34
        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 9.5)
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.EnableHeadersVisualStyles = False
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
    End Sub

    Private Sub Col(dgv As DataGridView, name As String, header As String, width As Integer)
        Dim c As New DataGridViewTextBoxColumn() With {
            .Name = name, .HeaderText = header, .Width = width,
            .SortMode = DataGridViewColumnSortMode.NotSortable
        }
        dgv.Columns.Add(c)
    End Sub

    Private Sub AsignarFactor(cell As DataGridViewCell, valor As Double)
        Dim v = Math.Round(valor, 2)
        cell.Value = v
        If v >= 0.9 Then
            cell.Style.BackColor = ColorOK
            cell.Style.ForeColor = ColorOKTexto
        Else
            cell.Style.BackColor = ColorMal
            cell.Style.ForeColor = ColorMalTexto
        End If
    End Sub

    Private Sub AsignarFactor(cell As DataGridViewCell, valor As Single)
        AsignarFactor(cell, CDbl(valor))
    End Sub

    Private Sub AsignarOk(cell As DataGridViewCell, cumple As Boolean)
        cell.Value = If(cumple, "Ok", "Revisar")
        cell.Style.BackColor = If(cumple, ColorOK, ColorMal)
        cell.Style.ForeColor = If(cumple, ColorOKTexto, ColorMalTexto)
    End Sub

    ' ── EXPORTAR EXCEL ───────────────────────────────────────────────────────

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        Dim pilas = Form_01_PagPilas.Proyecto.Elementos.Pilas.ListaElementos
        If pilas Is Nothing OrElse pilas.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim dlg As New SaveFileDialog() With {
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = "Reporte_Pilas.xlsx",
            .Title = "Exportar Reporte de Pilas"
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Using wb As New XLWorkbook()
                ExportarHojaResumen(wb, pilas)
                ExportarHojaCapacidad(wb, pilas)
                ExportarHojaInteraccion(wb, pilas)
                wb.SaveAs(dlg.FileName)
            End Using
            MessageBox.Show("Exportado correctamente.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al exportar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportarHojaResumen(wb As XLWorkbook, pilas As List(Of Elemento_Pila))
        Dim ws = wb.Worksheets.Add("Resumen")
        Dim hdrs = {"Elemento", "Df [m]", "Dc [m]", "L [m]", "Refuerzo", "fc [MPa]", "Cargas", "Suelo", "Cortante", "Interacción"}
        For j = 0 To hdrs.Length - 1
            Dim c = ws.Cell(1, j + 1)
            c.Value = hdrs(j)
            c.Style.Fill.BackgroundColor = XlEncabezado
            c.Style.Font.FontColor = XLColor.White
            c.Style.Font.Bold = True
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        Next
        For i = 0 To pilas.Count - 1
            Dim p = pilas(i)
            Dim row = i + 2
            ws.Cell(row, 1).Value = p.Name_Elemento
            ws.Cell(row, 2).Value = Math.Round(p.Df, 2)
            ws.Cell(row, 3).Value = Math.Round(CDbl(p.Dc), 2)
            ws.Cell(row, 4).Value = Math.Round(CDbl(p.L_Pila), 2)
            ws.Cell(row, 5).Value = p.N_Barra_Long & " × " & p.Cant_Barras_Long
            ws.Cell(row, 6).Value = p.fc

            Dim okC = p.Check1_PsE >= 0.9 AndAlso p.Check2_PsD >= 0.9 AndAlso p.Check3_PuE >= 0.9 AndAlso p.Check4_PuD >= 0.9
            Dim okS = p.Relacion_EsfE >= 0.9 AndAlso p.Relacion_EsfD >= 0.9
            Dim okV = p.FactorShear >= 0.9
            Dim fManR2 As Single = p.Factor_Manual_DI
            Dim fDiagR2 As Single = If(fManR2 > 0, fManR2, p.Factor_Diagonal)
            Dim fCortR2 As Single = If(fManR2 > 0, fManR2, p.Factor_CortesH)
            Dim okI = fDiagR2 >= 0.9 AndAlso fCortR2 >= 0.9

            ColorXL(ws.Cell(row, 7), If(okC, "Ok", "Revisar"), okC, Me)
            ColorXL(ws.Cell(row, 8), If(okS, "Ok", "Revisar"), okS, Me)
            ColorXL(ws.Cell(row, 9), If(okV, "Ok", "Revisar"), okV, Me)
            ColorXL(ws.Cell(row, 10), If(okI, "Ok", "Revisar"), okI, Me)
        Next
        ws.Columns().AdjustToContents()
    End Sub

    Private Sub ExportarHojaCapacidad(wb As XLWorkbook, pilas As List(Of Elemento_Pila))
        Dim ws = wb.Worksheets.Add("Capacidad de Carga")
        Dim hdrs = {"Elemento", "Ps Est. [kN]", "Ps Din. [kN]", "Pu Est. [kN]", "Pu Din. [kN]", "Pu Trac. [kN]",
                    "Ch1", "Ch2", "Ch3", "Ch4", "Ch5", "σ Est. [kPa]", "σ Din. [kPa]", "σAdm/σ Est.", "σAdm/σ Din."}
        For j = 0 To hdrs.Length - 1
            Dim c = ws.Cell(1, j + 1)
            c.Value = hdrs(j)
            c.Style.Fill.BackgroundColor = XlEncabezado
            c.Style.Font.FontColor = XLColor.White
            c.Style.Font.Bold = True
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        Next
        For i = 0 To pilas.Count - 1
            Dim p = pilas(i)
            Dim row = i + 2
            ws.Cell(row, 1).Value = p.Name_Elemento
            ws.Cell(row, 2).Value = Math.Round(CDbl(p.Ps_Estatica), 2)
            ws.Cell(row, 3).Value = Math.Round(CDbl(p.Ps_Dinamica), 2)
            ws.Cell(row, 4).Value = Math.Round(CDbl(p.Pu_Estatica), 2)
            ws.Cell(row, 5).Value = Math.Round(CDbl(p.Pu_Dinamica), 2)
            ws.Cell(row, 6).Value = Math.Round(CDbl(p.P_Traccion), 2)
            FactorXL(ws.Cell(row, 7), p.Check1_PsE, Me)
            FactorXL(ws.Cell(row, 8), p.Check2_PsD, Me)
            FactorXL(ws.Cell(row, 9), p.Check3_PuE, Me)
            FactorXL(ws.Cell(row, 10), p.Check4_PuD, Me)
            FactorXL(ws.Cell(row, 11), p.Check5_PuT, Me)
            ws.Cell(row, 12).Value = Math.Round(CDbl(p.EsfE_Trans), 2)
            ws.Cell(row, 13).Value = Math.Round(CDbl(p.EsfD_Trans), 2)
            FactorXL(ws.Cell(row, 14), p.Relacion_EsfE, Me)
            FactorXL(ws.Cell(row, 15), p.Relacion_EsfD, Me)
        Next
        ws.Columns().AdjustToContents()
    End Sub

    Private Sub ExportarHojaInteraccion(wb As XLWorkbook, pilas As List(Of Elemento_Pila))
        Dim ws = wb.Worksheets.Add("Cortante e Interacción")
        Dim hdrs = {"Elemento", "Df [m]", "Barra", "# Barras", "ρ [%]", "φVn [kN]", "Vu [kN]", "φVn/Vu", "Ch V2", "Ch V3", "F Cortes H", "F Recta"}
        For j = 0 To hdrs.Length - 1
            Dim c = ws.Cell(1, j + 1)
            c.Value = hdrs(j)
            c.Style.Fill.BackgroundColor = XlEncabezado
            c.Style.Font.FontColor = XLColor.White
            c.Style.Font.Bold = True
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        Next
        For i = 0 To pilas.Count - 1
            Dim p = pilas(i)
            Dim row = i + 2
            ws.Cell(row, 1).Value = p.Name_Elemento
            ws.Cell(row, 2).Value = Math.Round(p.Df, 2)
            ws.Cell(row, 3).Value = p.N_Barra_Long
            ws.Cell(row, 4).Value = p.Cant_Barras_Long
            ws.Cell(row, 5).Value = Math.Round(CDbl(p.Cuantia) * 100, 3)
            ws.Cell(row, 6).Value = Math.Round(CDbl(p.Vn), 2)
            ws.Cell(row, 7).Value = Math.Round(CDbl(p.Vu), 2)
            FactorXL(ws.Cell(row, 8), p.FactorShear, Me)
            ws.Cell(row, 9).Value = p.Check_V2
            ws.Cell(row, 10).Value = p.Check_V3
            Dim fManR4 As Single = p.Factor_Manual_DI
            Dim fCortR4 As Single = If(fManR4 > 0, fManR4, p.Factor_CortesH)
            Dim fDiagR4 As Single = If(fManR4 > 0, fManR4, p.Factor_Diagonal)
            FactorXL(ws.Cell(row, 11), fCortR4, Me)
            FactorXL(ws.Cell(row, 12), fDiagR4, Me)
        Next
        ws.Columns().AdjustToContents()
    End Sub

    Private Shared Sub ColorXL(cell As IXLCell, valor As String, cumple As Boolean, frm As Form_Reporte_Pilas)
        cell.Value = valor
        cell.Style.Fill.BackgroundColor = If(cumple, frm.XlOKFondo, frm.XlMalFondo)
        cell.Style.Font.FontColor = If(cumple, frm.XlOKTexto, frm.XlMalTexto)
        cell.Style.Font.Bold = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
    End Sub

    Private Shared Sub FactorXL(cell As IXLCell, valor As Single, frm As Form_Reporte_Pilas)
        Dim v = Math.Round(CDbl(valor), 2)
        cell.Value = v
        cell.Style.Fill.BackgroundColor = If(v >= 0.9, frm.XlOKFondo, frm.XlMalFondo)
        cell.Style.Font.FontColor = If(v >= 0.9, frm.XlOKTexto, frm.XlMalTexto)
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
    End Sub

End Class
