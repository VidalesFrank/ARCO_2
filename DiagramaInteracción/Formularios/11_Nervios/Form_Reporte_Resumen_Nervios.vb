Imports System.Drawing
Imports ClosedXML.Excel

' ═══════════════════════════════════════════════════════════════════════════════
'  Reporte de Revisión — Módulo 11 Nervios / Losas Nervadas
'  Flexión (C/D por apoyo) + Cortante (φVn vs Vu por extremo)
'  Sólo muestra frames que fueron calculados (PhiMn > 0 ó PhiVn > 0)
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Reporte_Resumen_Nervios
    Inherits Form

    Public Property Nervios As cNervios

    ' ── Paleta ────────────────────────────────────────────────────────────────
    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorEncabezadoTexto As Color = Color.White
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ColorAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ColorAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    ' Colores ClosedXML
    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F8F8F8")

    ' ── Grids ─────────────────────────────────────────────────────────────────
    Private WithEvents DgvFlexion As New DataGridView()
    Private WithEvents DgvCortante As New DataGridView()
    Private WithEvents _chkSoloObs As New CheckBox()
    Private _tabs As TabControl

    ' =========================================================================
    Public Sub New()

        Me.Text = "Reporte de Revisión — Nervios / Losas Nervadas"
        Me.Size = New Size(1450, 820)
        Me.MinimumSize = New Size(1000, 620)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' ── TabControl ────────────────────────────────────────────────────────
        _tabs = New TabControl()
        _tabs.Dock = DockStyle.Fill
        _tabs.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _tabs.Padding = New Point(16, 6)

        ' Tab 1: Revisión Flexión
        Dim tabFlex As New TabPage("  Revisión Flexión  ") With {
            .BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvFlexion.Dock = DockStyle.Fill
        EstilarGrid(DgvFlexion)
        tabFlex.Controls.Add(DgvFlexion)
        _tabs.TabPages.Add(tabFlex)

        ' Tab 2: Revisión Cortante
        Dim tabCor As New TabPage("  Revisión Cortante  ") With {
            .BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvCortante.Dock = DockStyle.Fill
        EstilarGrid(DgvCortante)
        tabCor.Controls.Add(DgvCortante)
        _tabs.TabPages.Add(tabCor)

        Me.Controls.Add(_tabs)

        ' ── Barra inferior ────────────────────────────────────────────────────
        Dim barra As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 9, 10, 9)
        }

        _chkSoloObs.Text = "Solo elementos con observaciones (C/D < 0.90)"
        _chkSoloObs.AutoSize = True
        _chkSoloObs.Location = New Point(10, 16)
        _chkSoloObs.Font = New Font("Segoe UI", 10)
        barra.Controls.Add(_chkSoloObs)

        Dim btnActualizar As New Button() With {
            .Text = "Actualizar",
            .Size = New Size(120, 36),
            .Location = New Point(350, 9),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = ColorEncabezado,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnActualizar.FlatAppearance.BorderSize = 0
        AddHandler btnActualizar.Click, Sub(s, ev) CargarTodo()
        barra.Controls.Add(btnActualizar)

        Dim btnExportar As New Button() With {
            .Text = "Exportar a Excel",
            .Size = New Size(160, 36),
            .Location = New Point(480, 9),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(21, 130, 70),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnExportar.FlatAppearance.BorderSize = 0
        AddHandler btnExportar.Click, AddressOf BtnExportar_Click
        barra.Controls.Add(btnExportar)

        Me.Controls.Add(barra)

        AddHandler Me.Load, AddressOf Form_Load
        AddHandler _chkSoloObs.CheckedChanged, Sub(s, ev) CargarTodo()

    End Sub

    ' =========================================================================
    ' CARGA
    ' =========================================================================

    Private Sub Form_Load(sender As Object, e As EventArgs)
        CargarTodo()
    End Sub

    Private Sub CargarTodo()
        If Nervios Is Nothing Then Return
        CargarFlexion()
        CargarCortante()
    End Sub

    ' ── REVISIÓN FLEXIÓN ──────────────────────────────────────────────────────
    '  Columnas: PISO | NERVIO | TRAMO |
    '            As Col Sup I | As Req Sup I | C/D Sup I |
    '            As Col Inf C | As Req Inf C | C/D Inf C |
    '            As Col Sup D | As Req Sup D | C/D Sup D | OBSERVACIONES

    Private Sub CargarFlexion()
        Dim dgv = DgvFlexion
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Piso",      "Piso",                70)
        AgregarColumna(dgv, "Nervio",    "Nervio",             140)
        AgregarColumna(dgv, "Tramo",     "Tramo",               88)
        AgregarColumna(dgv, "AcSupI",    "As Col Sup I (cm²)", 130)
        AgregarColumna(dgv, "ArSupI",    "As Req Sup I (cm²)", 130)
        AgregarColumna(dgv, "CdSupI",    "C/D Sup I",           80)
        AgregarColumna(dgv, "AcInfC",    "As Col Inf C (cm²)", 130)
        AgregarColumna(dgv, "ArInfC",    "As Req Inf C (cm²)", 130)
        AgregarColumna(dgv, "CdInfC",    "C/D Inf C",           80)
        AgregarColumna(dgv, "AcSupD",    "As Col Sup D (cm²)", 130)
        AgregarColumna(dgv, "ArSupD",    "As Req Sup D (cm²)", 130)
        AgregarColumna(dgv, "CdSupD",    "C/D Sup D",           80)
        AgregarColumna(dgv, "Obs",       "Observaciones",       240)

        Dim idx As Integer = 0
        Dim soloObs = _chkSoloObs.Checked

        For Each nerv In Nervios.Elementos
            For Each fn In nerv.Frames
                If Not CalculadoFlex(fn) Then Continue For

                Dim cdI = fn.CD_M_Sup_I
                Dim cdC = fn.CD_M_Inf_C
                Dim cdD = fn.CD_M_Sup_D

                Dim tieneObs = (cdI > 0 AndAlso cdI < 0.9) OrElse
                               (cdC > 0 AndAlso cdC < 0.9) OrElse
                               (cdD > 0 AndAlso cdD < 0.9)
                If soloObs AndAlso Not tieneObs Then Continue For

                Dim obs As New List(Of String)
                If cdI > 0 AndAlso cdI < 0.9 Then obs.Add("apoyo I por M(-)")
                If cdC > 0 AndAlso cdC < 0.9 Then obs.Add("centro por M(+)")
                If cdD > 0 AndAlso cdD < 0.9 Then obs.Add("apoyo D por M(-)")
                Dim obsStr = If(obs.Count > 0, "En " & String.Join(" y ", obs), "")

                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)

                row.Cells("Piso").Value   = nerv.Piso
                row.Cells("Nervio").Value = NombreNervio(nerv)
                row.Cells("Tramo").Value  = TramoLabel(fn)

                row.Cells("AcSupI").Value = If(fn.As_Prov_Sup_I > 0, CObj(Math.Round(fn.As_Prov_Sup_I, 2)), "—")
                row.Cells("ArSupI").Value = If(fn.As_Req_Sup_I  > 0, CObj(Math.Round(fn.As_Req_Sup_I,  2)), "—")
                AsignarCD(row.Cells("CdSupI"), cdI)

                row.Cells("AcInfC").Value = If(fn.As_Prov_Inf_C > 0, CObj(Math.Round(fn.As_Prov_Inf_C, 2)), "—")
                row.Cells("ArInfC").Value = If(fn.As_Req_Inf_C  > 0, CObj(Math.Round(fn.As_Req_Inf_C,  2)), "—")
                AsignarCD(row.Cells("CdInfC"), cdC)

                row.Cells("AcSupD").Value = If(fn.As_Prov_Sup_D > 0, CObj(Math.Round(fn.As_Prov_Sup_D, 2)), "—")
                row.Cells("ArSupD").Value = If(fn.As_Req_Sup_D  > 0, CObj(Math.Round(fn.As_Req_Sup_D,  2)), "—")
                AsignarCD(row.Cells("CdSupD"), cdD)

                row.Cells("Obs").Value = obsStr
                row.Cells("Obs").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
                idx += 1
            Next
        Next
    End Sub

    ' ── REVISIÓN CORTANTE ─────────────────────────────────────────────────────
    '  Columnas: PISO | NERVIO | TRAMO |
    '            Vu I (kN) | φVn I (kN) | C/D I |
    '            Vu D (kN) | φVn D (kN) | C/D D | Estado

    Private Sub CargarCortante()
        Dim dgv = DgvCortante
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Piso",   "Piso",         70)
        AgregarColumna(dgv, "Nervio", "Nervio",       140)
        AgregarColumna(dgv, "Tramo",  "Tramo",         88)
        AgregarColumna(dgv, "VuI",    "Vu I (kN)",     90)
        AgregarColumna(dgv, "VnI",    "φVn I (kN)",    90)
        AgregarColumna(dgv, "CdI",    "C/D I",         78)
        AgregarColumna(dgv, "VuD",    "Vu D (kN)",     90)
        AgregarColumna(dgv, "VnD",    "φVn D (kN)",    90)
        AgregarColumna(dgv, "CdD",    "C/D D",         78)
        AgregarColumna(dgv, "Estado", "Estado",        85)

        Dim idx As Integer = 0
        Dim soloObs = _chkSoloObs.Checked

        For Each nerv In Nervios.Elementos
            For Each fn In nerv.Frames
                If Not CalculadoFlex(fn) OrElse Not CalculadoCortante(fn) Then Continue For

                Dim cdI = fn.CD_Cortante_I
                Dim cdD = fn.CD_Cortante_D
                Dim cumpleI = (fn.PhiVn_I = 0 OrElse cdI >= 0.9)
                Dim cumpleD = (fn.PhiVn_D = 0 OrElse cdD >= 0.9)
                Dim cumple  = cumpleI AndAlso cumpleD

                If soloObs AndAlso cumple Then Continue For

                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)

                row.Cells("Piso").Value   = nerv.Piso
                row.Cells("Nervio").Value = NombreNervio(nerv)
                row.Cells("Tramo").Value  = TramoLabel(fn)

                row.Cells("VuI").Value = Math.Round(fn.Vu_I, 2).ToString("F2")
                row.Cells("VnI").Value = If(fn.PhiVn_I > 0, Math.Round(fn.PhiVn_I, 2).ToString("F2"), "—")
                AsignarCD(row.Cells("CdI"), If(fn.PhiVn_I > 0, cdI, Double.MaxValue))

                row.Cells("VuD").Value = Math.Round(fn.Vu_D, 2).ToString("F2")
                row.Cells("VnD").Value = If(fn.PhiVn_D > 0, Math.Round(fn.PhiVn_D, 2).ToString("F2"), "—")
                AsignarCD(row.Cells("CdD"), If(fn.PhiVn_D > 0, cdD, Double.MaxValue))

                row.Cells("Estado").Value = If(cumple, "OK", "Revisar")
                row.Cells("Estado").Style.BackColor = If(cumple, ColorOK, ColorMal)
                row.Cells("Estado").Style.ForeColor = If(cumple, ColorOKTexto, ColorMalTexto)
                row.Cells("Estado").Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                idx += 1
            Next
        Next
    End Sub

    ' =========================================================================
    ' FILTROS — un frame se incluye si fue calculado
    ' =========================================================================

    Private Function CalculadoFlex(fn As cFrameNervio) As Boolean
        Return fn.PhiMn_Sup_I > 0 OrElse fn.PhiMn_Inf_C > 0 OrElse fn.PhiMn_Sup_D > 0
    End Function

    Private Function CalculadoCortante(fn As cFrameNervio) As Boolean
        Return fn.PhiVn_I > 0 OrElse fn.PhiVn_D > 0
    End Function

    ' =========================================================================
    ' EXPORTAR EXCEL — ClosedXML
    ' =========================================================================

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        If Nervios Is Nothing OrElse Nervios.Elementos.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim dlg As New SaveFileDialog() With {
            .Filter   = "Excel (*.xlsx)|*.xlsx",
            .FileName = $"ARCO_Nervios_Reporte_{DateTime.Now:yyyyMMdd}",
            .Title    = "Exportar Reporte de Nervios"
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Me.Cursor = Cursors.WaitCursor
            Using wb As New XLWorkbook()
                ExportarHojaFlexion(wb)
                ExportarHojaCortante(wb)
                wb.SaveAs(dlg.FileName)
            End Using
            Me.Cursor = Cursors.Default
            MessageBox.Show("Reporte exportado correctamente." & vbCrLf & dlg.FileName,
                            "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            MessageBox.Show("Error al exportar: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ── Hoja Flexión ──────────────────────────────────────────────────────────

    Private Sub ExportarHojaFlexion(wb As XLWorkbook)
        Dim ws  = wb.Worksheets.Add("Revisión Flexión")
        Dim enc = {"Piso", "Nervio", "Tramo",
                   "As Col Sup I (cm2)", "As Req Sup I (cm2)", "C/D Sup I",
                   "As Col Inf C (cm2)", "As Req Inf C (cm2)", "C/D Inf C",
                   "As Col Sup D (cm2)", "As Req Sup D (cm2)", "C/D Sup D",
                   "Observaciones"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila As Integer = 2

        For Each nerv In Nervios.Elementos
            For Each fn In nerv.Frames
                If Not CalculadoFlex(fn) Then Continue For

                Dim cdI = fn.CD_M_Sup_I
                Dim cdC = fn.CD_M_Inf_C
                Dim cdD = fn.CD_M_Sup_D
                Dim obs As New List(Of String)
                If cdI > 0 AndAlso cdI < 0.9 Then obs.Add("apoyo I por M(-)")
                If cdC > 0 AndAlso cdC < 0.9 Then obs.Add("centro por M(+)")
                If cdD > 0 AndAlso cdD < 0.9 Then obs.Add("apoyo D por M(-)")

                ws.Cell(fila, 1).Value  = nerv.Piso
                ws.Cell(fila, 2).Value  = NombreNervio(nerv)
                ws.Cell(fila, 3).Value  = TramoLabel(fn)
                ws.Cell(fila, 4).Value  = If(fn.As_Prov_Sup_I > 0, CObj(Math.Round(fn.As_Prov_Sup_I, 2)), "-")
                ws.Cell(fila, 5).Value  = If(fn.As_Req_Sup_I  > 0, CObj(Math.Round(fn.As_Req_Sup_I,  2)), "-")
                If cdI > 0 Then EscribirFactor(ws.Cell(fila, 6), cdI)  Else ws.Cell(fila, 6).Value  = "-"
                ws.Cell(fila, 7).Value  = If(fn.As_Prov_Inf_C > 0, CObj(Math.Round(fn.As_Prov_Inf_C, 2)), "-")
                ws.Cell(fila, 8).Value  = If(fn.As_Req_Inf_C  > 0, CObj(Math.Round(fn.As_Req_Inf_C,  2)), "-")
                If cdC > 0 Then EscribirFactor(ws.Cell(fila, 9), cdC)  Else ws.Cell(fila, 9).Value  = "-"
                ws.Cell(fila, 10).Value = If(fn.As_Prov_Sup_D > 0, CObj(Math.Round(fn.As_Prov_Sup_D, 2)), "-")
                ws.Cell(fila, 11).Value = If(fn.As_Req_Sup_D  > 0, CObj(Math.Round(fn.As_Req_Sup_D,  2)), "-")
                If cdD > 0 Then EscribirFactor(ws.Cell(fila, 12), cdD) Else ws.Cell(fila, 12).Value = "-"
                ws.Cell(fila, 13).Value = If(obs.Count > 0, "En " & String.Join(" y ", obs), "")
                ws.Cell(fila, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left

                EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1
            Next
        Next

        AjustarColumnas(ws, enc.Length)
        AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
    End Sub

    ' ── Hoja Cortante ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCortante(wb As XLWorkbook)
        Dim ws  = wb.Worksheets.Add("Revisión Cortante")
        Dim enc = {"Piso", "Nervio", "Tramo",
                   "Vu I (kN)", "φVn I (kN)", "C/D I",
                   "Vu D (kN)", "φVn D (kN)", "C/D D",
                   "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila As Integer = 2

        For Each nerv In Nervios.Elementos
            For Each fn In nerv.Frames
                If Not CalculadoFlex(fn) OrElse Not CalculadoCortante(fn) Then Continue For

                Dim cdI    = fn.CD_Cortante_I
                Dim cdD    = fn.CD_Cortante_D
                Dim cumple = (fn.PhiVn_I = 0 OrElse cdI >= 0.9) AndAlso
                             (fn.PhiVn_D = 0 OrElse cdD >= 0.9)

                ws.Cell(fila, 1).Value  = nerv.Piso
                ws.Cell(fila, 2).Value  = NombreNervio(nerv)
                ws.Cell(fila, 3).Value  = TramoLabel(fn)
                ws.Cell(fila, 4).Value  = Math.Round(fn.Vu_I, 2)
                ws.Cell(fila, 5).Value  = If(fn.PhiVn_I > 0, CObj(Math.Round(fn.PhiVn_I, 2)), "-")
                If fn.PhiVn_I > 0 Then EscribirFactor(ws.Cell(fila, 6), cdI) Else ws.Cell(fila, 6).Value = "-"
                ws.Cell(fila, 7).Value  = Math.Round(fn.Vu_D, 2)
                ws.Cell(fila, 8).Value  = If(fn.PhiVn_D > 0, CObj(Math.Round(fn.PhiVn_D, 2)), "-")
                If fn.PhiVn_D > 0 Then EscribirFactor(ws.Cell(fila, 9), cdD) Else ws.Cell(fila, 9).Value = "-"
                EscribirEstado(ws.Cell(fila, 10), If(cumple, "OK", "Revisar"),
                               If(cumple, XlOKFondo, XlMalFondo),
                               If(cumple, XlOKTexto, XlMalTexto))

                EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1
            Next
        Next

        AjustarColumnas(ws, enc.Length)
        AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
    End Sub

    ' =========================================================================
    ' HELPERS
    ' =========================================================================

    Private Function TramoLabel(fn As cFrameNervio) As String
        If Not String.IsNullOrWhiteSpace(fn.EjeApoyo_I) OrElse
           Not String.IsNullOrWhiteSpace(fn.EjeApoyo_D) Then
            Return $"{fn.EjeApoyo_I}-{fn.EjeApoyo_D}"
        End If
        Return fn.ObjectLabel
    End Function

    Private Function NombreNervio(nerv As cNervio) As String
        Return If(String.IsNullOrWhiteSpace(nerv.NombrePlano), nerv.Nombre, nerv.NombrePlano)
    End Function

    ' ── UI helpers ────────────────────────────────────────────────────────────

    Private Sub EstilarGrid(dgv As DataGridView)
        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.MultiSelect = False
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.RowHeadersVisible = False
        dgv.BackgroundColor = Color.White
        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.GridColor = Color.FromArgb(210, 210, 210)
        dgv.ColumnHeadersHeight = 42
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.RowTemplate.Height = 28
        dgv.EnableHeadersVisualStyles = False
        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor = ColorEncabezado
            .ForeColor = ColorEncabezadoTexto
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
        With dgv.DefaultCellStyle
            .BackColor = Color.White
            .ForeColor = Color.Black
            .SelectionBackColor = Color.FromArgb(200, 225, 255)
            .SelectionForeColor = Color.Black
            .Font = New Font("Segoe UI", 10)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

    Private Sub AgregarColumna(dgv As DataGridView, nombre As String, header As String, ancho As Integer)
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = nombre, .HeaderText = header, .Width = ancho})
    End Sub

    Private Sub AsignarCD(cell As DataGridViewCell, cd As Double)
        If cd <= 0 OrElse cd = Double.MaxValue Then
            cell.Value = "—"
            Return
        End If
        Dim v = Math.Round(Math.Min(cd, 9.99), 2)
        cell.Value = v.ToString("F2")
        If cd >= 0.9 Then
            cell.Style.BackColor = ColorOK  : cell.Style.ForeColor = ColorOKTexto
        Else
            cell.Style.BackColor = ColorMal : cell.Style.ForeColor = ColorMalTexto
        End If
    End Sub

    ' ── ClosedXML helpers ─────────────────────────────────────────────────────

    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, enc As String())
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirEncabezados(ws, fila, enc)
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirFactor(cell, valor, ReporteHelpers.SinDato.MaxValue)
    End Sub

    Private Sub EscribirEstado(cell As IXLCell, texto As String, fondo As XLColor, textoColor As XLColor)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirEstado(cell, texto, fondo, textoColor)
    End Sub

    Private Sub EstilarFilaDatos(ws As IXLWorksheet, fila As Integer, numCols As Integer, esPar As Boolean)
        Dim row = ws.Row(fila)
        row.Height = 18
        For col = 1 To numCols
            Dim cell = ws.Cell(fila, col)
            If esPar AndAlso cell.Style.Fill.BackgroundColor = XLColor.NoColor Then
                cell.Style.Fill.BackgroundColor = XlFilaPar
            End If
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
            If col <= 3 Then cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
        Next
    End Sub

    Private Sub AgregarBordesTabla(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.AgregarBordesTabla(ws, filaIni, filaFin, numCols)
    End Sub

    Private Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.AjustarColumnas(ws, numCols, columnaAncha:=3, anchoMinimo:=20)
    End Sub

End Class
