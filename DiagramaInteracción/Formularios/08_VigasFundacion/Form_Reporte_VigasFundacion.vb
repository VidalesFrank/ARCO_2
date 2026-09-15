Imports System.Drawing
Imports ClosedXML.Excel

' ═══════════════════════════════════════════════════════════════════════════════
'  Reporte de Revisión — Módulo 08 Vigas de Fundación (Puntales)
'  Tab 1: Diagrama de Interacción  (C/D compresión + C/D tracción)
'  Tab 2: Requisitos Normativos    (dimensiones, cuantía, estribos)
'  Solo muestra vigas donde Calculado = True
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Reporte_VigasFundacion
    Inherits Form

    Public Property VigasFundacion As cVigasFundacion

    ' ── Paleta WinForms ───────────────────────────────────────────────────────
    Private ReadOnly ColorEncabezado    As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorEncabezadoTxt As Color = Color.White
    Private ReadOnly ColorOK            As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTxt         As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal           As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTxt        As Color = ColorTranslator.FromHtml("#9C0006")

    ' ── Paleta ClosedXML ──────────────────────────────────────────────────────
    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo    As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto    As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo   As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto   As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlFilaPar    As XLColor = XLColor.FromHtml("#F8F8F8")

    ' ── Controles ─────────────────────────────────────────────────────────────
    Private WithEvents DgvDI      As New DataGridView()
    Private WithEvents DgvNorm    As New DataGridView()
    Private WithEvents _chkSoloObs As New CheckBox()
    Private _tabs As TabControl

    ' =========================================================================
    Public Sub New()
        Me.Text          = "Reporte de Revisión — Vigas de Fundación"
        Me.Size          = New Size(1200, 720)
        Me.MinimumSize   = New Size(900, 560)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor     = Color.White
        Me.Font          = New Font("Segoe UI", 10)

        _tabs = New TabControl()
        _tabs.Dock    = DockStyle.Fill
        _tabs.Font    = New Font("Segoe UI", 10, FontStyle.Bold)
        _tabs.Padding = New Point(16, 6)

        ' Tab 1 — Diagrama de Interacción
        Dim tabDI As New TabPage("  Diagrama de Interacción  ") With {
            .BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvDI.Dock = DockStyle.Fill
        EstilarGrid(DgvDI)
        tabDI.Controls.Add(DgvDI)
        _tabs.TabPages.Add(tabDI)

        ' Tab 2 — Requisitos Normativos
        Dim tabNorm As New TabPage("  Requisitos Normativos  ") With {
            .BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvNorm.Dock = DockStyle.Fill
        EstilarGrid(DgvNorm)
        tabNorm.Controls.Add(DgvNorm)
        _tabs.TabPages.Add(tabNorm)

        Me.Controls.Add(_tabs)

        ' ── Barra inferior ────────────────────────────────────────────────────
        Dim barra As New Panel() With {
            .Dock      = DockStyle.Bottom,
            .Height    = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding   = New Padding(10, 9, 10, 9)
        }

        _chkSoloObs.Text     = "Solo elementos con observaciones (C/D < 0.90)"
        _chkSoloObs.AutoSize = True
        _chkSoloObs.Location = New Point(10, 16)
        _chkSoloObs.Font     = New Font("Segoe UI", 10)
        barra.Controls.Add(_chkSoloObs)

        Dim btnActualizar As New Button() With {
            .Text      = "Actualizar",
            .Size      = New Size(120, 36),
            .Location  = New Point(380, 9),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = ColorEncabezado,
            .ForeColor = Color.White,
            .Font      = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor    = Cursors.Hand
        }
        btnActualizar.FlatAppearance.BorderSize = 0
        AddHandler btnActualizar.Click, Sub(s, ev) CargarTodo()
        barra.Controls.Add(btnActualizar)

        Dim btnExportar As New Button() With {
            .Text      = "Exportar a Excel",
            .Size      = New Size(160, 36),
            .Location  = New Point(510, 9),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(21, 130, 70),
            .ForeColor = Color.White,
            .Font      = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor    = Cursors.Hand
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
        If VigasFundacion Is Nothing Then Return
        CargarDI()
        CargarNormativos()
    End Sub

    ' ── Tab 1: Diagrama de Interacción ────────────────────────────────────────
    '  VIGA | SECCIÓN | φPn_Max (kN) | φPn_Min (kN) | Pu (kN) | C/D Comp | C/D Trac | Observaciones

    Private Sub CargarDI()
        Dim dgv = DgvDI
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Viga",     "VIGA",                130)
        AgregarColumna(dgv, "Seccion",  "SECCIÓN (BxH/L)",     150)
        AgregarColumna(dgv, "PhiPnMax", "φPn_max (kN)",        110)
        AgregarColumna(dgv, "PhiPnMin", "φPn_min (kN)",        110)
        AgregarColumna(dgv, "Pu",       "Pu (kN)",              90)
        AgregarColumna(dgv, "CdComp",   "C/D Comp.",            90)
        AgregarColumna(dgv, "CdTrac",   "C/D Trac.",            90)
        AgregarColumna(dgv, "Obs",      "OBSERVACIONES",       200)

        Dim soloObs As Boolean = _chkSoloObs.Checked
        Dim idx As Integer = 0

        For Each vf As cVigaFundacion In VigasFundacion.Elementos
            If Not vf.Calculado Then Continue For

            Dim tieneObs As Boolean = (vf.CD_Comp < 0.9) OrElse (vf.CD_Trac < 0.9)
            If soloObs AndAlso Not tieneObs Then Continue For

            Dim obs As New List(Of String)()
            If vf.CD_Comp < 0.9 Then obs.Add("C/D compresión insuf.")
            If vf.CD_Trac < 0.9 Then obs.Add("C/D tracción insuf.")

            Dim r   As Integer         = dgv.Rows.Add()
            Dim row As DataGridViewRow = dgv.Rows(r)
            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)

            row.Cells("Viga").Value     = NombreViga(vf)
            row.Cells("Seccion").Value  = SeccionLabel(vf)
            row.Cells("PhiPnMax").Value = Math.Round(vf.PhiPn_Max, 2).ToString("F2")
            row.Cells("PhiPnMin").Value = Math.Round(vf.PhiPn_Min, 2).ToString("F2")
            row.Cells("Pu").Value       = Math.Round(vf.Pu, 2).ToString("F2")
            AsignarCD(row.Cells("CdComp"), vf.CD_Comp)
            AsignarCD(row.Cells("CdTrac"), vf.CD_Trac)
            row.Cells("Obs").Value = String.Join(" / ", obs)
            row.Cells("Obs").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            idx += 1
        Next
    End Sub

    ' ── Tab 2: Requisitos Normativos ──────────────────────────────────────────
    '  VIGA | SECCIÓN | Dimensiones | Cuantía | Estribos | Estado general

    Private Sub CargarNormativos()
        Dim dgv = DgvNorm
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Viga",     "VIGA",                130)
        AgregarColumna(dgv, "Seccion",  "SECCIÓN (BxH/L)",     150)
        AgregarColumna(dgv, "Dim",      "Dimensiones",          115)
        AgregarColumna(dgv, "DetDim",   "Detalle dim.",         220)
        AgregarColumna(dgv, "Cuantia",  "Cuantía",              90)
        AgregarColumna(dgv, "DetCuant", "Detalle cuantía",      200)
        AgregarColumna(dgv, "Est",      "Estribos",             90)
        AgregarColumna(dgv, "DetEst",   "Detalle estribos",     200)
        AgregarColumna(dgv, "EstGen",   "Estado",               90)

        Dim soloObs As Boolean = _chkSoloObs.Checked
        Dim idx As Integer = 0

        For Each vf As cVigaFundacion In VigasFundacion.Elementos
            If Not vf.Calculado Then Continue For

            Dim tieneObs As Boolean = Not vf.CumpleNormativo
            If soloObs AndAlso Not tieneObs Then Continue For

            Dim r   As Integer         = dgv.Rows.Add()
            Dim row As DataGridViewRow = dgv.Rows(r)
            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)

            row.Cells("Viga").Value    = NombreViga(vf)
            row.Cells("Seccion").Value = SeccionLabel(vf)
            AsignarEstado(row.Cells("Dim"),     vf.CumpleDim)
            row.Cells("DetDim").Value           = vf.DetalleDim
            row.Cells("DetDim").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            AsignarEstado(row.Cells("Cuantia"),  vf.CumpleCuantia)
            row.Cells("DetCuant").Value           = vf.DetalleCuantia
            row.Cells("DetCuant").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            AsignarEstado(row.Cells("Est"),      vf.CumpleEstribo)
            row.Cells("DetEst").Value           = vf.DetalleEstribo
            row.Cells("DetEst").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
            AsignarEstado(row.Cells("EstGen"),   vf.CumpleNormativo)
            idx += 1
        Next
    End Sub

    ' =========================================================================
    ' EXPORTAR EXCEL — ClosedXML
    ' =========================================================================

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        If VigasFundacion Is Nothing OrElse VigasFundacion.Elementos.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim dlg As New SaveFileDialog() With {
            .Filter   = "Excel (*.xlsx)|*.xlsx",
            .FileName = $"ARCO_VigasFundacion_{DateTime.Now:yyyyMMdd}",
            .Title    = "Exportar Reporte de Vigas de Fundación"
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Me.Cursor = Cursors.WaitCursor
            Using wb As New XLWorkbook()
                ExportarHojaDI(wb)
                ExportarHojaNormativos(wb)
                wb.SaveAs(dlg.FileName)
            End Using
            Me.Cursor = Cursors.Default
            MessageBox.Show("Reporte exportado correctamente." & vbCrLf & dlg.FileName,
                            "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            Logger.Error(ex, "Form_Reporte_VigasFundacion.BtnExportar_Click")
            MessageBox.Show("Error al exportar: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ── Hoja DI ───────────────────────────────────────────────────────────────

    Private Sub ExportarHojaDI(wb As XLWorkbook)
        Dim ws  = wb.Worksheets.Add("Diagrama de Interacción")
        Dim enc = {"VIGA", "SECCIÓN (BxH/L)",
                   "φPn_max (kN)", "φPn_min (kN)", "Pu (kN)",
                   "C/D Comp.", "C/D Trac.", "Observaciones"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila As Integer = 2

        For Each vf As cVigaFundacion In VigasFundacion.Elementos
            If Not vf.Calculado Then Continue For

            Dim obs As New List(Of String)()
            If vf.CD_Comp < 0.9 Then obs.Add("C/D compresión insuf.")
            If vf.CD_Trac < 0.9 Then obs.Add("C/D tracción insuf.")

            ws.Cell(fila, 1).Value = NombreViga(vf)
            ws.Cell(fila, 2).Value = SeccionLabel(vf)
            ws.Cell(fila, 3).Value = Math.Round(vf.PhiPn_Max, 2)
            ws.Cell(fila, 4).Value = Math.Round(vf.PhiPn_Min, 2)
            ws.Cell(fila, 5).Value = Math.Round(vf.Pu, 2)
            EscribirFactor(ws.Cell(fila, 6), vf.CD_Comp)
            EscribirFactor(ws.Cell(fila, 7), vf.CD_Trac)
            ws.Cell(fila, 8).Value = If(obs.Count > 0, String.Join(" / ", obs), "")
            ws.Cell(fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left

            EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
            fila += 1
        Next

        AjustarColumnas(ws, enc.Length)
        AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
    End Sub

    ' ── Hoja Normativos ───────────────────────────────────────────────────────

    Private Sub ExportarHojaNormativos(wb As XLWorkbook)
        Dim ws  = wb.Worksheets.Add("Requisitos Normativos")
        Dim enc = {"VIGA", "SECCIÓN (BxH/L)",
                   "Dimensiones", "Detalle dim.",
                   "Cuantía", "Detalle cuantía",
                   "Estribos", "Detalle estribos",
                   "Estado general"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila As Integer = 2

        For Each vf As cVigaFundacion In VigasFundacion.Elementos
            If Not vf.Calculado Then Continue For

            ws.Cell(fila, 1).Value = NombreViga(vf)
            ws.Cell(fila, 2).Value = SeccionLabel(vf)
            EscribirEstado(ws.Cell(fila, 3), If(vf.CumpleDim, "OK", "Revisar"),
                           If(vf.CumpleDim, XlOKFondo, XlMalFondo),
                           If(vf.CumpleDim, XlOKTexto, XlMalTexto))
            ws.Cell(fila, 4).Value = vf.DetalleDim
            ws.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            EscribirEstado(ws.Cell(fila, 5), If(vf.CumpleCuantia, "OK", "Revisar"),
                           If(vf.CumpleCuantia, XlOKFondo, XlMalFondo),
                           If(vf.CumpleCuantia, XlOKTexto, XlMalTexto))
            ws.Cell(fila, 6).Value = vf.DetalleCuantia
            ws.Cell(fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            EscribirEstado(ws.Cell(fila, 7), If(vf.CumpleEstribo, "OK", "Revisar"),
                           If(vf.CumpleEstribo, XlOKFondo, XlMalFondo),
                           If(vf.CumpleEstribo, XlOKTexto, XlMalTexto))
            ws.Cell(fila, 8).Value = vf.DetalleEstribo
            ws.Cell(fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            EscribirEstado(ws.Cell(fila, 9), If(vf.CumpleNormativo, "OK", "Revisar"),
                           If(vf.CumpleNormativo, XlOKFondo, XlMalFondo),
                           If(vf.CumpleNormativo, XlOKTexto, XlMalTexto))

            EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
            fila += 1
        Next

        AjustarColumnas(ws, enc.Length)
        AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
    End Sub

    ' =========================================================================
    ' HELPERS — datos
    ' =========================================================================

    Private Function NombreViga(vf As cVigaFundacion) As String
        Return If(Not String.IsNullOrWhiteSpace(vf.NombrePlano), vf.NombrePlano, vf.Nombre)
    End Function

    Private Function SeccionLabel(vf As cVigaFundacion) As String
        Return $"{Math.Round(vf.B * 100, 0):F0}x{Math.Round(vf.H * 100, 0):F0}/{vf.L:F2}m"
    End Function

    ' =========================================================================
    ' HELPERS — UI
    ' =========================================================================

    Private Sub EstilarGrid(dgv As DataGridView)
        dgv.AllowUserToAddRows = False
        dgv.ReadOnly           = True
        dgv.SelectionMode      = DataGridViewSelectionMode.FullRowSelect
        dgv.MultiSelect        = False
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.RowHeadersVisible  = False
        dgv.BackgroundColor    = Color.White
        dgv.BorderStyle        = BorderStyle.None
        dgv.CellBorderStyle    = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.GridColor          = Color.FromArgb(210, 210, 210)
        dgv.ColumnHeadersHeight = 42
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.RowTemplate.Height = 28
        dgv.EnableHeadersVisualStyles = False
        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor  = ColorEncabezado
            .ForeColor  = ColorEncabezadoTxt
            .Font       = New Font("Segoe UI", 10, FontStyle.Bold)
            .Alignment  = DataGridViewContentAlignment.MiddleCenter
        End With
        With dgv.DefaultCellStyle
            .BackColor          = Color.White
            .ForeColor          = Color.Black
            .SelectionBackColor = Color.FromArgb(200, 225, 255)
            .SelectionForeColor = Color.Black
            .Font               = New Font("Segoe UI", 10)
            .Alignment          = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

    Private Sub AgregarColumna(dgv As DataGridView, nombre As String, header As String, ancho As Integer)
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = nombre, .HeaderText = header, .Width = ancho})
    End Sub

    Private Sub AsignarCD(cell As DataGridViewCell, cd As Double)
        Dim v As Double = Math.Round(Math.Min(cd, 9.99), 2)
        cell.Value = v.ToString("F2")
        If cd >= 0.9 Then
            cell.Style.BackColor = ColorOK  : cell.Style.ForeColor = ColorOKTxt
        Else
            cell.Style.BackColor = ColorMal : cell.Style.ForeColor = ColorMalTxt
        End If
        cell.Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
    End Sub

    Private Sub AsignarEstado(cell As DataGridViewCell, cumple As Boolean)
        cell.Value = If(cumple, "OK", "Revisar")
        cell.Style.BackColor = If(cumple, ColorOK, ColorMal)
        cell.Style.ForeColor = If(cumple, ColorOKTxt, ColorMalTxt)
        cell.Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
    End Sub

    ' =========================================================================
    ' HELPERS — ClosedXML
    ' =========================================================================

    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, enc As String())
        For i As Integer = 0 To enc.Length - 1
            Dim cell = ws.Cell(fila, i + 1)
            cell.Value = enc(i)
            With cell.Style
                .Fill.BackgroundColor      = XlEncabezado
                .Font.FontColor            = XLColor.White
                .Font.Bold                 = True
                .Font.FontSize             = 11
                .Alignment.Horizontal      = XLAlignmentHorizontalValues.Center
                .Alignment.Vertical        = XLAlignmentVerticalValues.Center
                .Border.OutsideBorder      = XLBorderStyleValues.Thin
                .Border.OutsideBorderColor = XLColor.White
            End With
            ws.Row(fila).Height = 22
        Next
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        Dim v As Double = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format  = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        If v >= 0.9 Then
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor       = XlOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XlMalFondo
            cell.Style.Font.FontColor       = XlMalTexto
        End If
        cell.Style.Font.Bold = True
    End Sub

    Private Sub EscribirEstado(cell As IXLCell, texto As String,
                                fondo As XLColor, textoColor As XLColor)
        cell.Value = texto
        cell.Style.Fill.BackgroundColor = fondo
        cell.Style.Font.FontColor       = textoColor
        cell.Style.Font.Bold            = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
    End Sub

    Private Sub EstilarFilaDatos(ws As IXLWorksheet, fila As Integer,
                                  numCols As Integer, esPar As Boolean)
        Dim row = ws.Row(fila)
        row.Height = 18
        For col As Integer = 1 To numCols
            Dim cell = ws.Cell(fila, col)
            If esPar AndAlso cell.Style.Fill.BackgroundColor = XLColor.NoColor Then
                cell.Style.Fill.BackgroundColor = XlFilaPar
            End If
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
            If col <= 2 Then cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
        Next
    End Sub

    Private Sub AgregarBordesTabla(ws As IXLWorksheet,
                                    filaIni As Integer, filaFin As Integer,
                                    numCols As Integer)
        If filaFin < filaIni Then Return
        Dim rango = ws.Range(filaIni, 1, filaFin, numCols)
        rango.Style.Border.InsideBorder       = XLBorderStyleValues.Hair
        rango.Style.Border.InsideBorderColor  = XLColor.FromHtml("#CCCCCC")
        rango.Style.Border.OutsideBorder      = XLBorderStyleValues.Medium
        rango.Style.Border.OutsideBorderColor = XlEncabezado
    End Sub

    Private Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer)
        ws.Columns(1, numCols).AdjustToContents()
        For c As Integer = 1 To numCols
            If ws.Column(c).Width > 45 Then ws.Column(c).Width = 45
        Next
        ws.SheetView.FreezeRows(1)
    End Sub

End Class
