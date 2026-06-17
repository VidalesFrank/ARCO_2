Imports System.Drawing
Imports ClosedXML.Excel
Imports ARCO.cZapata

''' <summary>
''' Reporte de revisión de zapatas: Resumen + Detalle por combinación + Excel.
''' </summary>
Public Class Form_Reporte_Zapatas
    Inherits Form

    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")

    Private ReadOnly _fontBold As New Font("Segoe UI", 9.5, FontStyle.Bold)

    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F0F4FA")

    Private WithEvents DgvResumen As New DataGridView()
    Private WithEvents DgvDetalle As New DataGridView()
    Private _tabs As TabControl

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_Reporte_Zapatas(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Private ReadOnly _proyecto As Proyecto

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
        CargarTodo()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Reporte — Revisión de Zapatas"
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

        Dim tabDet As New TabPage("  Detalle por Combinación  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvDetalle.Dock = DockStyle.Fill
        EstilarGrid(DgvDetalle)
        tabDet.Controls.Add(DgvDetalle)
        _tabs.TabPages.Add(tabDet)

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
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _fontBold.Dispose()
        MyBase.Dispose(disposing)
    End Sub

    ' =========================================================================
    Private Sub CargarTodo()
        If _proyecto Is Nothing Then Return
        Dim tipos = _proyecto.Elementos.Zapatas.Tipos
        If tipos Is Nothing OrElse tipos.Count = 0 Then Return
        CargarResumen(tipos)
        CargarDetalle(tipos)
    End Sub

    ' ── TAB 1: RESUMEN (peor caso entre todas las combinaciones) ─────────────
    Private Sub CargarResumen(tipos As List(Of cZapata))
        Dim dgv = DgvResumen
        dgv.SuspendLayout()
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Nombre",    "Zapata",       110)
        Col(dgv, "Lb",        "L_b [m]",       70)
        Col(dgv, "Lh",        "L_h [m]",       70)
        Col(dgv, "e",         "e [m]",          60)
        Col(dgv, "b",         "b [m]",          60)
        Col(dgv, "h",         "h [m]",          60)
        Col(dgv, "fc",        "fc [MPa]",       70)
        Col(dgv, "qAdmE",     "qAdm Est.",      80)
        Col(dgv, "qAdmD",     "qAdm Din.",      80)
        Col(dgv, "Capacidad", "Capacidad",      90)
        Col(dgv, "Punz",      "Punzonamiento",  110)
        Col(dgv, "Cortante",  "Cortante",        90)
        Col(dgv, "Flexion",   "Flexion",         80)
        Col(dgv, "General",   "General",         80)

        For i = 0 To tipos.Count - 1
            Dim z = tipos(i)
            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)

            row.Cells("Nombre").Value = z.Nombre
            row.Cells("Lb").Value     = Math.Round(z.L_b, 2)
            row.Cells("Lh").Value     = Math.Round(z.L_h, 2)
            row.Cells("e").Value      = Math.Round(z.e, 2)
            row.Cells("b").Value      = Math.Round(z.b, 2)
            row.Cells("h").Value      = Math.Round(z.h, 2)
            row.Cells("fc").Value     = z.fc
            row.Cells("qAdmE").Value  = z.qAdm_Est
            row.Cells("qAdmD").Value  = z.qAdm_Din
            If i Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)

            If z.Resultados Is Nothing OrElse z.Resultados.Count = 0 Then
                For Each k In {"Capacidad", "Punz", "Cortante", "Flexion", "General"}
                    row.Cells(k).Value = "Sin calcular"
                Next
                Continue For
            End If

            Dim vals = z.Resultados.Values.ToList()
            Dim okCap  = vals.All(Function(res) res.CumpleCapacidad)
            Dim okPunz = vals.All(Function(res) res.CumplePunzonamiento)
            Dim okCort = vals.All(Function(res) res.CumpleCortante_1 AndAlso res.CumpleCortante_2 AndAlso
                                                res.CumpleCortante_3 AndAlso res.CumpleCortante_4)
            Dim okFlex = vals.All(Function(res) res.Cumple_L1 AndAlso res.Cumple_L2)
            Dim okGen  = vals.All(Function(res) res.CumpleGeneral)

            AsignarOk(row.Cells("Capacidad"), okCap)
            AsignarOk(row.Cells("Punz"),      okPunz)
            AsignarOk(row.Cells("Cortante"),  okCort)
            AsignarOk(row.Cells("Flexion"),   okFlex)
            AsignarOk(row.Cells("General"),   okGen)
        Next
        dgv.ResumeLayout(True)
    End Sub

    ' ── TAB 2: DETALLE POR COMBINACIÓN ───────────────────────────────────────
    Private Sub CargarDetalle(tipos As List(Of cZapata))
        Dim dgv = DgvDetalle
        dgv.SuspendLayout()
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Nombre", "Zapata",      110)
        Col(dgv, "Combo",  "Combinacion",  150)
        Col(dgv, "qMax",   "qMax [kN/m2]", 100)
        Col(dgv, "qMin",   "qMin [kN/m2]", 100)
        Col(dgv, "g1",     "g1",            60)
        Col(dgv, "g2",     "g2",            60)
        Col(dgv, "Cap",    "Capacidad",     90)
        Col(dgv, "g5",     "g5 Punz.",      75)
        Col(dgv, "g6",     "g6 Punz.",      75)
        Col(dgv, "Punz",   "Punzonamiento",110)
        Col(dgv, "gfC",    "gf Cort.",      75)
        Col(dgv, "gaC",    "ga Cort.",      75)
        Col(dgv, "Cort",   "Cortante",      90)
        Col(dgv, "gfF",    "gf Flex.",      75)
        Col(dgv, "gaF",    "ga Flex.",      75)
        Col(dgv, "Flex",   "Flexion",       80)
        Col(dgv, "Gen",    "General",       80)

        Dim fila As Integer = 0
        For Each z In tipos
            If z.Resultados Is Nothing Then Continue For
            For Each kvp In z.Resultados
                Dim combo = kvp.Key
                Dim res = kvp.Value
                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)

                row.Cells("Nombre").Value = z.Nombre
                row.Cells("Combo").Value  = combo
                row.Cells("qMax").Value   = Math.Round(res.qMax, 2)
                row.Cells("qMin").Value   = Math.Round(res.qMin, 2)
                AsignarFactor(row.Cells("g1"), res.g1)
                AsignarFactor(row.Cells("g2"), res.g2)
                AsignarOk(row.Cells("Cap"),  res.CumpleCapacidad)
                AsignarFactor(row.Cells("g5"), res.g5)
                AsignarFactor(row.Cells("g6"), res.g6)
                AsignarOk(row.Cells("Punz"), res.CumplePunzonamiento)
                AsignarFactor(row.Cells("gfC"), res.gf_C)
                AsignarFactor(row.Cells("gaC"), res.ga_C)
                AsignarOk(row.Cells("Cort"), res.CumpleCortante_1 AndAlso res.CumpleCortante_2 AndAlso
                                              res.CumpleCortante_3 AndAlso res.CumpleCortante_4)
                AsignarFactor(row.Cells("gfF"), res.gf_F)
                AsignarFactor(row.Cells("gaF"), res.ga_F)
                AsignarOk(row.Cells("Flex"), res.Cumple_L1 AndAlso res.Cumple_L2)
                AsignarOk(row.Cells("Gen"),  res.CumpleGeneral)

                If fila Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
                fila += 1
            Next
        Next
        dgv.ResumeLayout(True)
    End Sub

    ' ── EXPORTAR EXCEL ────────────────────────────────────────────────────────
    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        If _proyecto Is Nothing OrElse _proyecto.Elementos.Zapatas.Tipos.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim dlg As New SaveFileDialog With {
            .Title = "Guardar Reporte de Zapatas",
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = "Reporte_Zapatas_" & DateTime.Now.ToString("yyyyMMdd")
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Dim tipos = _proyecto.Elementos.Zapatas.Tipos
            Using wb As New XLWorkbook()
                ExportarHojaResumen(wb, tipos)
                ExportarHojaDetalle(wb, tipos)
                wb.SaveAs(dlg.FileName)
            End Using
            Dim abrir = MessageBox.Show("Reporte exportado. ¿Abrir ahora?", "Listo",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportarHojaResumen(wb As XLWorkbook, tipos As List(Of cZapata))
        Dim ws = wb.Worksheets.Add("Resumen")
        ws.ShowGridLines = False
        ws.SheetView.FreezeRows(1)

        Dim hdrs = {"Zapata", "L_b (m)", "L_h (m)", "e (m)", "b (m)", "h (m)",
                    "fc (MPa)", "qAdm Est.", "qAdm Din.",
                    "Capacidad", "Punzonamiento", "Cortante", "Flexion", "General"}
        EscribirEncabezados(ws, 1, hdrs)

        Dim fila As Integer = 2
        For Each z In tipos
            ws.Cell(fila, 1).Value  = z.Nombre
            ws.Cell(fila, 2).Value  = Math.Round(z.L_b, 2)
            ws.Cell(fila, 3).Value  = Math.Round(z.L_h, 2)
            ws.Cell(fila, 4).Value  = Math.Round(z.e, 2)
            ws.Cell(fila, 5).Value  = Math.Round(z.b, 2)
            ws.Cell(fila, 6).Value  = Math.Round(z.h, 2)
            ws.Cell(fila, 7).Value  = z.fc
            ws.Cell(fila, 8).Value  = z.qAdm_Est
            ws.Cell(fila, 9).Value  = z.qAdm_Din

            If z.Resultados IsNot Nothing AndAlso z.Resultados.Count > 0 Then
                Dim vals = z.Resultados.Values.ToList()
                EscribirCumple(ws.Cell(fila, 10),  vals.All(Function(r) r.CumpleCapacidad))
                EscribirCumple(ws.Cell(fila, 11),  vals.All(Function(r) r.CumplePunzonamiento))
                EscribirCumple(ws.Cell(fila, 12),  vals.All(Function(r) r.CumpleCortante_1 AndAlso r.CumpleCortante_2 AndAlso
                                                                          r.CumpleCortante_3 AndAlso r.CumpleCortante_4))
                EscribirCumple(ws.Cell(fila, 13),  vals.All(Function(r) r.Cumple_L1 AndAlso r.Cumple_L2))
                EscribirCumple(ws.Cell(fila, 14),  vals.All(Function(r) r.CumpleGeneral))
            Else
                For c = 10 To 14
                    ws.Cell(fila, c).Value = "Sin calcular"
                Next
            End If

            If fila Mod 2 = 0 Then
                For c = 1 To 9
                    ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                Next
            End If
            fila += 1
        Next
        AgregarBordes(ws, 1, fila - 1, 14)
        ws.Columns().AdjustToContents()
    End Sub

    Private Sub ExportarHojaDetalle(wb As XLWorkbook, tipos As List(Of cZapata))
        Dim ws = wb.Worksheets.Add("Detalle Combinaciones")
        ws.SheetView.FreezeRows(1)

        Dim hdrs = {"Zapata", "Combinacion", "qMax (kN/m2)", "qMin (kN/m2)",
                    "g1", "g2", "Capacidad",
                    "g5 Punz.", "g6 Punz.", "Punzonamiento",
                    "gf Cort.", "ga Cort.", "Cortante",
                    "gf Flex.", "ga Flex.", "Flexion", "General"}
        EscribirEncabezados(ws, 1, hdrs)

        Dim fila As Integer = 2
        For Each z In tipos
            If z.Resultados Is Nothing Then Continue For
            For Each kvp In z.Resultados
                Dim res = kvp.Value
                ws.Cell(fila, 1).Value  = z.Nombre
                ws.Cell(fila, 2).Value  = kvp.Key
                ws.Cell(fila, 3).Value  = Math.Round(res.qMax, 2)
                ws.Cell(fila, 4).Value  = Math.Round(res.qMin, 2)
                EscribirFactor(ws.Cell(fila, 5),  res.g1)
                EscribirFactor(ws.Cell(fila, 6),  res.g2)
                EscribirCumple(ws.Cell(fila, 7),  res.CumpleCapacidad)
                EscribirFactor(ws.Cell(fila, 8),  res.g5)
                EscribirFactor(ws.Cell(fila, 9),  res.g6)
                EscribirCumple(ws.Cell(fila, 10), res.CumplePunzonamiento)
                EscribirFactor(ws.Cell(fila, 11), res.gf_C)
                EscribirFactor(ws.Cell(fila, 12), res.ga_C)
                EscribirCumple(ws.Cell(fila, 13), res.CumpleCortante_1 AndAlso res.CumpleCortante_2 AndAlso
                                                   res.CumpleCortante_3 AndAlso res.CumpleCortante_4)
                EscribirFactor(ws.Cell(fila, 14), res.gf_F)
                EscribirFactor(ws.Cell(fila, 15), res.ga_F)
                EscribirCumple(ws.Cell(fila, 16), res.Cumple_L1 AndAlso res.Cumple_L2)
                EscribirCumple(ws.Cell(fila, 17), res.CumpleGeneral)
                If fila Mod 2 = 0 Then
                    For c = 1 To 4
                        ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                    Next
                End If
                fila += 1
            Next
        Next
        AgregarBordes(ws, 1, fila - 1, 17)
        ws.Columns().AdjustToContents()
    End Sub

    ' ── HELPERS GRID ─────────────────────────────────────────────────────────
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
        dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
        dgv.RowTemplate.Height = 26
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
            cell.Style.BackColor = ColorOK : cell.Style.ForeColor = ColorOKTexto
        Else
            cell.Style.BackColor = ColorMal : cell.Style.ForeColor = ColorMalTexto
        End If
    End Sub

    Private Sub AsignarOk(cell As DataGridViewCell, cumple As Boolean)
        cell.Value = If(cumple, "Cumple", "No cumple")
        cell.Style.BackColor = If(cumple, ColorOK, ColorMal)
        cell.Style.ForeColor = If(cumple, ColorOKTexto, ColorMalTexto)
        cell.Style.Font = _fontBold
    End Sub

    ' ── HELPERS EXCEL ────────────────────────────────────────────────────────
    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, hdrs As String())
        For i = 0 To hdrs.Length - 1
            Dim cell = ws.Cell(fila, i + 1)
            cell.Value = hdrs(i)
            With cell.Style
                .Fill.BackgroundColor = XlEncabezado
                .Font.FontColor = XLColor.White
                .Font.Bold = True
                .Font.FontSize = 10
                .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                .Alignment.Vertical = XLAlignmentVerticalValues.Center
                .Border.OutsideBorder = XLBorderStyleValues.Thin
                .Border.OutsideBorderColor = XLColor.White
            End With
            ws.Row(fila).Height = 20
        Next
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        If valor <= 0 Then cell.Value = "-" : Return
        Dim v = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        If v >= 1.0 Then
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor = XlOKTexto
        ElseIf v >= 0.9 Then
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEB9C")
            cell.Style.Font.FontColor = XLColor.FromHtml("#9C5700")
        Else
            cell.Style.Fill.BackgroundColor = XlMalFondo
            cell.Style.Font.FontColor = XlMalTexto
        End If
        cell.Style.Font.Bold = True
    End Sub

    Private Sub EscribirCumple(cell As IXLCell, cumple As Boolean)
        cell.Value = If(cumple, "CUMPLE", "NO CUMPLE")
        cell.Style.Font.Bold = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        If cumple Then
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor = XlOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XlMalFondo
            cell.Style.Font.FontColor = XlMalTexto
        End If
    End Sub

    Private Sub AgregarBordes(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        If filaFin < filaIni Then Return
        Dim rango = ws.Range(filaIni, 1, filaFin, numCols)
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair
        rango.Style.Border.InsideBorderColor = XLColor.FromHtml("#CCCCCC")
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Medium
        rango.Style.Border.OutsideBorderColor = XlEncabezado
    End Sub

End Class
