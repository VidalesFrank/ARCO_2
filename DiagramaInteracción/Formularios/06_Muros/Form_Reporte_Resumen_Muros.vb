Imports System.Drawing
Imports ClosedXML.Excel

Public Class Form_Reporte_Resumen_Muros
    Inherits Form

    Public Property Muros As List(Of Muro)

    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ColorAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ColorAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlAlertaFondo As XLColor = XLColor.FromHtml("#FFEB9C")
    Private ReadOnly XlAlertaTexto As XLColor = XLColor.FromHtml("#9C5700")
    Private ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F8F8F8")

    Private WithEvents DgvFlex As New DataGridView()
    Private WithEvents DgvCortante As New DataGridView()
    Private WithEvents DgvEB As New DataGridView()
    Private WithEvents DgvCompleto As New DataGridView()
    Private _tabs As TabControl

    Public Sub New()
        Me.Text = "Reporte Resumen — Muros Estructurales"
        Me.Size = New Size(1350, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        _tabs = New TabControl() With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Padding = New Point(16, 6)}

        Dim agregar = Sub(titulo As String, dgv As DataGridView)
                          Dim tab As New TabPage($"  {titulo}  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
                          dgv.Dock = DockStyle.Fill
                          EstilarGrid(dgv)
                          tab.Controls.Add(dgv)
                          _tabs.TabPages.Add(tab)
                      End Sub

        agregar("Flexo-Compresión", DgvFlex)
        agregar("Cortante", DgvCortante)
        agregar("Elementos de Borde", DgvEB)
        agregar("Resumen Completo", DgvCompleto)

        Me.Controls.Add(_tabs)

        Dim barra As New Panel() With {.Dock = DockStyle.Bottom, .Height = 54, .BackColor = Color.FromArgb(245, 245, 245)}

        Dim btnAct As New Button() With {.Text = "Actualizar", .Size = New Size(120, 36), .Location = New Point(10, 9),
                                          .FlatStyle = FlatStyle.Flat, .BackColor = ColorEncabezado, .ForeColor = Color.White,
                                          .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand}
        btnAct.FlatAppearance.BorderSize = 0
        AddHandler btnAct.Click, Sub(s, ev) CargarTodo()
        barra.Controls.Add(btnAct)

        Dim btnExp As New Button() With {.Text = "Exportar a Excel", .Size = New Size(160, 36), .Location = New Point(140, 9),
                                          .FlatStyle = FlatStyle.Flat, .BackColor = Color.FromArgb(21, 130, 70), .ForeColor = Color.White,
                                          .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand}
        btnExp.FlatAppearance.BorderSize = 0
        AddHandler btnExp.Click, AddressOf BtnExportar_Click
        barra.Controls.Add(btnExp)

        Me.Controls.Add(barra)
        AddHandler Me.Load, AddressOf Form_Load
    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)
        CargarTodo()
    End Sub

    Private Sub CargarTodo()
        If Muros Is Nothing Then Return
        CargarFlexion()
        CargarCortante()
        CargarEB()
        CargarCompleto()
    End Sub

    ' ── FLEXO-COMPRESIÓN ──────────────────────────────────────────────────────

    Private Sub CargarFlexion()
        Dim dgv = DgvFlex
        dgv.Columns.Clear() : dgv.Rows.Clear()
        ACol(dgv, "Muro", "Muro", 120) : ACol(dgv, "Piso", "Piso", 80)
        ACol(dgv, "tw", "tw (m)", 75) : ACol(dgv, "Lw", "Lw (m)", 75)
        ACol(dgv, "CTop", "ρ_req Top", 85) : ACol(dgv, "CBot", "ρ_req Bot", 85)
        ACol(dgv, "FTop", "F Top", 75) : ACol(dgv, "FBot", "F Bot", 75)
        ACol(dgv, "FDI", "F Diagrama I.", 100) : ACol(dgv, "CombCrit", "Comb. Crítica", 150)
        ACol(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                Dim fTop = If(sec.Cuantia_Top_Req > 0, Math.Round(sec.Cuantia_Top_Col / sec.Cuantia_Top_Req, 2), 0.0)
                Dim fBot = If(sec.Cuantia_Bot_Req > 0, Math.Round(sec.Cuantia_Bot_Col / sec.Cuantia_Bot_Req, 2), 0.0)
                Dim fDI = Math.Min(sec.Factor_Demanda_Flexo_Top, sec.Factor_Demanda_Flexo_Bot)
                Dim combCrit = If(sec.Factor_Demanda_Flexo_Top <= sec.Factor_Demanda_Flexo_Bot,
                                  sec.Combinacion_Demanda_Flexo_Top, sec.Combinacion_Demanda_Flexo_Bot)

                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                row.Cells("Muro").Value = muro.Label
                row.Cells("Piso").Value = sec.Piso
                row.Cells("tw").Value = Math.Round(sec.tw_Planos, 3)
                row.Cells("Lw").Value = Math.Round(sec.Lw_Planos, 2)
                row.Cells("CTop").Value = Math.Round(sec.Cuantia_Top_Req, 3)
                row.Cells("CBot").Value = Math.Round(sec.Cuantia_Bot_Req, 3)
                AsignarFactor(row.Cells("FTop"), fTop)
                AsignarFactor(row.Cells("FBot"), fBot)
                If fDI > 0 Then AsignarFactor(row.Cells("FDI"), fDI) Else row.Cells("FDI").Value = "-"
                row.Cells("CombCrit").Value = If(String.IsNullOrEmpty(combCrit), "-", combCrit)

                Dim ok = fTop >= 0.9 AndAlso fBot >= 0.9
                AplicarEstado(row.Cells("Estado"), If(ok, "OK", "Revisar"), If(ok, ColorOK, ColorMal), If(ok, ColorOKTexto, ColorMalTexto))

                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
                idx += 1
            Next
        Next
    End Sub

    ' ── CORTANTE ──────────────────────────────────────────────────────────────

    Private Sub CargarCortante()
        Dim dgv = DgvCortante
        dgv.Columns.Clear() : dgv.Rows.Clear()
        ACol(dgv, "Muro", "Muro", 120) : ACol(dgv, "Piso", "Piso", 80)
        ACol(dgv, "tw", "tw (m)", 75) : ACol(dgv, "Lw", "Lw (m)", 75)
        ACol(dgv, "Vu", "Vu (kN)", 85) : ACol(dgv, "Vc", "Vc (kN)", 85)
        ACol(dgv, "Vs", "Vs (kN)", 85) : ACol(dgv, "Vn", "Vn (kN)", 85)
        ACol(dgv, "FCor", "F Cortante", 90) : ACol(dgv, "Estado", "Estado", 80)

        Dim idx As Integer = 0
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                row.Cells("Muro").Value = muro.Label
                row.Cells("Piso").Value = sec.Piso
                row.Cells("tw").Value = Math.Round(sec.tw_Planos, 3)
                row.Cells("Lw").Value = Math.Round(sec.Lw_Planos, 2)
                row.Cells("Vu").Value = Math.Round(sec.Vu, 1)
                row.Cells("Vc").Value = Math.Round(sec.Vc, 1)
                row.Cells("Vs").Value = Math.Round(sec.Vs, 1)
                row.Cells("Vn").Value = Math.Round(sec.Vn, 1)
                AsignarFactor(row.Cells("FCor"), Math.Round(sec.F_Cortante, 2))
                Dim ok = sec.F_Cortante >= 0.9
                AplicarEstado(row.Cells("Estado"), If(ok, "OK", "Revisar"), If(ok, ColorOK, ColorMal), If(ok, ColorOKTexto, ColorMalTexto))
                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
                idx += 1
            Next
        Next
    End Sub

    ' ── ELEMENTOS DE BORDE ────────────────────────────────────────────────────

    Private Sub CargarEB()
        Dim dgv = DgvEB
        dgv.Columns.Clear() : dgv.Rows.Clear()
        ACol(dgv, "Muro", "Muro", 120) : ACol(dgv, "Piso", "Piso", 80)
        ACol(dgv, "CLim", "C límite (m)", 90)
        ACol(dgv, "CI_Top", "C_I Top (m)", 90) : ACol(dgv, "CD_Top", "C_D Top (m)", 90)
        ACol(dgv, "EB_I_Esf", "EB Izq Esf.", 100) : ACol(dgv, "EB_D_Esf", "EB Der Esf.", 100)
        ACol(dgv, "EB_I_Def", "EB Izq Def.", 100) : ACol(dgv, "EB_D_Def", "EB Der Def.", 100)
        ACol(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                row.Cells("Muro").Value = muro.Label
                row.Cells("Piso").Value = sec.Piso
                row.Cells("CLim").Value = Math.Round(sec.C_Limite, 3)
                row.Cells("CI_Top").Value = Math.Round(sec.C_I_Top, 3)
                row.Cells("CD_Top").Value = Math.Round(sec.C_D_Top, 3)

                PintarChequeoEB(row.Cells("EB_I_Esf"), sec.Chequeo_EB_I_Top_Esf)
                PintarChequeoEB(row.Cells("EB_D_Esf"), sec.Chequeo_EB_D_Top_Esf)
                PintarChequeoEB(row.Cells("EB_I_Def"), sec.Chequeo_EB_I_Top_Def)
                PintarChequeoEB(row.Cells("EB_D_Def"), sec.Chequeo_EB_D_Top_Def)

                Dim requiereEB = sec.Chequeo_EB_I_Top_Esf = "Requiere" OrElse sec.Chequeo_EB_D_Top_Esf = "Requiere" OrElse
                                 sec.Chequeo_EB_I_Top_Def = "Requiere" OrElse sec.Chequeo_EB_D_Top_Def = "Requiere"
                AplicarEstado(row.Cells("Estado"), If(Not requiereEB, "OK", "Rev. EB"),
                              If(Not requiereEB, ColorOK, ColorAlerta),
                              If(Not requiereEB, ColorOKTexto, ColorAlertaTexto))

                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
                idx += 1
            Next
        Next
    End Sub

    Private Sub PintarChequeoEB(cell As DataGridViewCell, chequeo As String)
        Dim requiere = chequeo = "Requiere"
        cell.Value = If(String.IsNullOrEmpty(chequeo), "-", chequeo)
        cell.Style.BackColor = If(requiere, ColorAlerta, ColorOK)
        cell.Style.ForeColor = If(requiere, ColorAlertaTexto, ColorOKTexto)
    End Sub

    ' ── RESUMEN COMPLETO ──────────────────────────────────────────────────────

    Private Sub CargarCompleto()
        Dim dgv = DgvCompleto
        dgv.Columns.Clear() : dgv.Rows.Clear()
        ACol(dgv, "Muro", "Muro", 120) : ACol(dgv, "Piso", "Piso", 80)
        ACol(dgv, "tw", "tw (m)", 75) : ACol(dgv, "Lw", "Lw (m)", 75)
        ACol(dgv, "FFlex", "F Flex mín", 90) : ACol(dgv, "FCor", "F Cortante", 90)
        ACol(dgv, "EstFlex", "Flexión", 80) : ACol(dgv, "EstCor", "Cortante", 80)
        ACol(dgv, "EstEB", "EB", 80) : ACol(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                Dim fTop = If(sec.Cuantia_Top_Req > 0, sec.Cuantia_Top_Col / sec.Cuantia_Top_Req, 9.99)
                Dim fBot = If(sec.Cuantia_Bot_Req > 0, sec.Cuantia_Bot_Col / sec.Cuantia_Bot_Req, 9.99)
                Dim fFlexMin = Math.Min(fTop, fBot)
                Dim okFlex = fFlexMin >= 0.9
                Dim okCor = sec.F_Cortante >= 0.9
                Dim requiereEB = sec.Chequeo_EB_I_Top_Esf = "Requiere" OrElse sec.Chequeo_EB_D_Top_Esf = "Requiere" OrElse
                                 sec.Chequeo_EB_I_Top_Def = "Requiere" OrElse sec.Chequeo_EB_D_Top_Def = "Requiere"

                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                row.Cells("Muro").Value = muro.Label
                row.Cells("Piso").Value = sec.Piso
                row.Cells("tw").Value = Math.Round(sec.tw_Planos, 3)
                row.Cells("Lw").Value = Math.Round(sec.Lw_Planos, 2)
                AsignarFactor(row.Cells("FFlex"), Math.Round(fFlexMin, 2))
                AsignarFactor(row.Cells("FCor"), Math.Round(sec.F_Cortante, 2))
                AplicarEstado(row.Cells("EstFlex"), If(okFlex, "OK", "Rev."), If(okFlex, ColorOK, ColorMal), If(okFlex, ColorOKTexto, ColorMalTexto))
                AplicarEstado(row.Cells("EstCor"), If(okCor, "OK", "Rev."), If(okCor, ColorOK, ColorMal), If(okCor, ColorOKTexto, ColorMalTexto))
                AplicarEstado(row.Cells("EstEB"), If(Not requiereEB, "OK", "Rev. EB"), If(Not requiereEB, ColorOK, ColorAlerta), If(Not requiereEB, ColorOKTexto, ColorAlertaTexto))

                Dim estadoOK = okFlex AndAlso okCor AndAlso Not requiereEB
                AplicarEstado(row.Cells("Estado"), If(estadoOK, "OK", "Revisar"),
                              If(estadoOK, ColorOK, ColorMal), If(estadoOK, ColorOKTexto, ColorMalTexto))

                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
                idx += 1
            Next
        Next
    End Sub

    ' ── EXPORTAR EXCEL ────────────────────────────────────────────────────────

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        If Muros Is Nothing OrElse Muros.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim dlg As New SaveFileDialog() With {
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = $"ARCO_Muros_Reporte_{DateTime.Now:yyyyMMdd}",
            .Title = "Exportar Reporte de Muros"
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return
        Try
            Me.Cursor = Cursors.WaitCursor
            Using wb As New XLWorkbook()
                ExportarHojaFlexion(wb)
                ExportarHojaCortante(wb)
                ExportarHojaEB(wb)
                ExportarHojaCompleto(wb)
                wb.SaveAs(dlg.FileName)
            End Using
            Me.Cursor = Cursors.Default
            MessageBox.Show("Reporte exportado correctamente." & vbCrLf & dlg.FileName, "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            MessageBox.Show("Error al exportar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportarHojaFlexion(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Flexo-Compresión")
        Dim enc = {"Muro", "Piso", "tw (m)", "Lw (m)", "ρ_req Top", "ρ_req Bot", "F Top", "F Bot", "F Diagrama I.", "Comb. Crítica", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                Dim fTop = If(sec.Cuantia_Top_Req > 0, sec.Cuantia_Top_Col / sec.Cuantia_Top_Req, 0.0)
                Dim fBot = If(sec.Cuantia_Bot_Req > 0, sec.Cuantia_Bot_Col / sec.Cuantia_Bot_Req, 0.0)
                Dim fDI = Math.Min(sec.Factor_Demanda_Flexo_Top, sec.Factor_Demanda_Flexo_Bot)
                ws.Cell(fila, 1).Value = muro.Label : ws.Cell(fila, 2).Value = sec.Piso
                ws.Cell(fila, 3).Value = sec.tw_Planos : ws.Cell(fila, 4).Value = sec.Lw_Planos
                ws.Cell(fila, 5).Value = sec.Cuantia_Top_Req : ws.Cell(fila, 6).Value = sec.Cuantia_Bot_Req
                EscribirFactor(ws.Cell(fila, 7), fTop) : EscribirFactor(ws.Cell(fila, 8), fBot)
                If fDI > 0 Then EscribirFactor(ws.Cell(fila, 9), fDI) Else ws.Cell(fila, 9).Value = "-"
                ws.Cell(fila, 10).Value = If(String.IsNullOrEmpty(sec.Combinacion_Demanda_Flexo_Top), "-", sec.Combinacion_Demanda_Flexo_Top)
                EscribirEstadoXL(ws.Cell(fila, 11), If(fTop >= 0.9 AndAlso fBot >= 0.9, "OK", "Revisar"))
                EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1
            Next
        Next
        AjustarColumnas(ws, enc.Length)
        AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    Private Sub ExportarHojaCortante(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Cortante")
        Dim enc = {"Muro", "Piso", "tw (m)", "Lw (m)", "Vu (kN)", "Vc (kN)", "Vs (kN)", "Vn (kN)", "F Cortante", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                ws.Cell(fila, 1).Value = muro.Label : ws.Cell(fila, 2).Value = sec.Piso
                ws.Cell(fila, 3).Value = sec.tw_Planos : ws.Cell(fila, 4).Value = sec.Lw_Planos
                ws.Cell(fila, 5).Value = Math.Round(sec.Vu, 1) : ws.Cell(fila, 6).Value = Math.Round(sec.Vc, 1)
                ws.Cell(fila, 7).Value = Math.Round(sec.Vs, 1) : ws.Cell(fila, 8).Value = Math.Round(sec.Vn, 1)
                EscribirFactor(ws.Cell(fila, 9), sec.F_Cortante)
                EscribirEstadoXL(ws.Cell(fila, 10), If(sec.F_Cortante >= 0.9, "OK", "Revisar"))
                EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1
            Next
        Next
        AjustarColumnas(ws, enc.Length) : AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    Private Sub ExportarHojaEB(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Elementos de Borde")
        Dim enc = {"Muro", "Piso", "C límite (m)", "C_I Top (m)", "C_D Top (m)", "EB Izq Esf.", "EB Der Esf.", "EB Izq Def.", "EB Der Def.", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                ws.Cell(fila, 1).Value = muro.Label : ws.Cell(fila, 2).Value = sec.Piso
                ws.Cell(fila, 3).Value = Math.Round(sec.C_Limite, 3) : ws.Cell(fila, 4).Value = Math.Round(sec.C_I_Top, 3)
                ws.Cell(fila, 5).Value = Math.Round(sec.C_D_Top, 3)
                EscribirChequeoEB_XL(ws.Cell(fila, 6), sec.Chequeo_EB_I_Top_Esf)
                EscribirChequeoEB_XL(ws.Cell(fila, 7), sec.Chequeo_EB_D_Top_Esf)
                EscribirChequeoEB_XL(ws.Cell(fila, 8), sec.Chequeo_EB_I_Top_Def)
                EscribirChequeoEB_XL(ws.Cell(fila, 9), sec.Chequeo_EB_D_Top_Def)
                Dim req = sec.Chequeo_EB_I_Top_Esf = "Requiere" OrElse sec.Chequeo_EB_D_Top_Esf = "Requiere" OrElse
                          sec.Chequeo_EB_I_Top_Def = "Requiere" OrElse sec.Chequeo_EB_D_Top_Def = "Requiere"
                EscribirEstadoXL(ws.Cell(fila, 10), If(Not req, "OK", "Rev. EB"))
                EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1) : fila += 1
            Next
        Next
        AjustarColumnas(ws, enc.Length) : AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    Private Sub ExportarHojaCompleto(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Resumen Completo")
        Dim enc = {"Muro", "Piso", "tw (m)", "Lw (m)", "F Flex mín", "F Cortante", "Flexión", "Cortante", "EB", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each muro In Muros
            For Each sec In muro.Lista_Secciones
                Dim fTop = If(sec.Cuantia_Top_Req > 0, sec.Cuantia_Top_Col / sec.Cuantia_Top_Req, 9.99)
                Dim fBot = If(sec.Cuantia_Bot_Req > 0, sec.Cuantia_Bot_Col / sec.Cuantia_Bot_Req, 9.99)
                Dim fFlex = Math.Min(fTop, fBot)
                Dim okF = fFlex >= 0.9 : Dim okC = sec.F_Cortante >= 0.9
                Dim req = sec.Chequeo_EB_I_Top_Esf = "Requiere" OrElse sec.Chequeo_EB_D_Top_Esf = "Requiere" OrElse
                          sec.Chequeo_EB_I_Top_Def = "Requiere" OrElse sec.Chequeo_EB_D_Top_Def = "Requiere"
                ws.Cell(fila, 1).Value = muro.Label : ws.Cell(fila, 2).Value = sec.Piso
                ws.Cell(fila, 3).Value = sec.tw_Planos : ws.Cell(fila, 4).Value = sec.Lw_Planos
                EscribirFactor(ws.Cell(fila, 5), fFlex) : EscribirFactor(ws.Cell(fila, 6), sec.F_Cortante)
                EscribirEstadoXL(ws.Cell(fila, 7), If(okF, "OK", "Rev."))
                EscribirEstadoXL(ws.Cell(fila, 8), If(okC, "OK", "Rev."))
                EscribirEstadoXL(ws.Cell(fila, 9), If(Not req, "OK", "Rev. EB"))
                EscribirEstadoXL(ws.Cell(fila, 10), If(okF AndAlso okC AndAlso Not req, "OK", "Revisar"))
                EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1) : fila += 1
            Next
        Next
        AjustarColumnas(ws, enc.Length) : AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    ' ── Helpers ClosedXML ─────────────────────────────────────────────────────

    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, enc As String())
        For i = 0 To enc.Length - 1
            With ws.Cell(fila, i + 1)
                .Value = enc(i)
                .Style.Fill.BackgroundColor = XlEncabezado
                .Style.Font.FontColor = XLColor.White
                .Style.Font.Bold = True
                .Style.Font.FontSize = 11
                .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                .Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
            End With
        Next
        ws.Row(fila).Height = 22
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        Dim v = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        cell.Style.Font.Bold = True
        cell.Style.Fill.BackgroundColor = If(v >= 0.9, XlOKFondo, XlMalFondo)
        cell.Style.Font.FontColor = If(v >= 0.9, XlOKTexto, XlMalTexto)
    End Sub

    Private Sub EscribirEstadoXL(cell As IXLCell, estado As String)
        cell.Value = estado
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        cell.Style.Font.Bold = True
        Select Case estado
            Case "OK"
                cell.Style.Fill.BackgroundColor = XlOKFondo
                cell.Style.Font.FontColor = XlOKTexto
            Case "Rev. EB"
                cell.Style.Fill.BackgroundColor = XlAlertaFondo
                cell.Style.Font.FontColor = XlAlertaTexto
            Case Else
                cell.Style.Fill.BackgroundColor = XlMalFondo
                cell.Style.Font.FontColor = XlMalTexto
        End Select
    End Sub

    Private Sub EscribirChequeoEB_XL(cell As IXLCell, chequeo As String)
        cell.Value = If(String.IsNullOrEmpty(chequeo), "-", chequeo)
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        If chequeo = "Requiere" Then
            cell.Style.Fill.BackgroundColor = XlAlertaFondo
            cell.Style.Font.FontColor = XlAlertaTexto
        Else
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor = XlOKTexto
        End If
    End Sub

    Private Sub EstilarFila(ws As IXLWorksheet, fila As Integer, numCols As Integer, esPar As Boolean)
        ws.Row(fila).Height = 18
        For c = 1 To numCols
            With ws.Cell(fila, c).Style
                .Alignment.Vertical = XLAlignmentVerticalValues.Center
                If c <= 2 Then .Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            End With
            If esPar AndAlso ws.Cell(fila, c).Style.Fill.BackgroundColor = XLColor.NoColor Then
                ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
            End If
        Next
    End Sub

    Private Sub AgregarBordes(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        If filaFin < filaIni Then Return
        With ws.Range(filaIni, 1, filaFin, numCols).Style.Border
            .InsideBorder = XLBorderStyleValues.Hair
            .InsideBorderColor = XLColor.FromHtml("#CCCCCC")
            .OutsideBorder = XLBorderStyleValues.Medium
            .OutsideBorderColor = XlEncabezado
        End With
    End Sub

    Private Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer)
        ws.Columns(1, numCols).AdjustToContents()
        For c = 1 To numCols
            If ws.Column(c).Width > 40 Then ws.Column(c).Width = 40
        Next
        ws.SheetView.FreezeRows(1)
    End Sub

    ' ── Helpers UI ───────────────────────────────────────────────────────────

    Private Sub EstilarGrid(dgv As DataGridView)
        dgv.AllowUserToAddRows = False : dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect : dgv.MultiSelect = False
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.RowHeadersVisible = False : dgv.BackgroundColor = Color.White
        dgv.BorderStyle = BorderStyle.None : dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.GridColor = Color.FromArgb(210, 210, 210)
        dgv.ColumnHeadersHeight = 42 : dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.RowTemplate.Height = 28 : dgv.EnableHeadersVisualStyles = False
        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor = ColorEncabezado : .ForeColor = Color.White
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
        With dgv.DefaultCellStyle
            .BackColor = Color.White : .ForeColor = Color.Black
            .SelectionBackColor = Color.FromArgb(200, 225, 255) : .SelectionForeColor = Color.Black
            .Font = New Font("Segoe UI", 10) : .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

    Private Sub ACol(dgv As DataGridView, nombre As String, header As String, ancho As Integer)
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = nombre, .HeaderText = header, .Width = ancho})
    End Sub

    Private Sub AsignarFactor(cell As DataGridViewCell, valor As Double)
        Dim v = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v.ToString("F2")
        cell.Style.BackColor = If(v >= 0.9, ColorOK, ColorMal)
        cell.Style.ForeColor = If(v >= 0.9, ColorOKTexto, ColorMalTexto)
    End Sub

    Private Sub AplicarEstado(cell As DataGridViewCell, texto As String, fondo As Color, textoColor As Color)
        cell.Value = texto : cell.Style.BackColor = fondo : cell.Style.ForeColor = textoColor
    End Sub

End Class
