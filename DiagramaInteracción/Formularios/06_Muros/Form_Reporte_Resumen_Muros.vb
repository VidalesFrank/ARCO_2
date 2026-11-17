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
        _filas = ConstruirFilas()
        CargarFlexion()
        CargarCortante()
        CargarEB()
        CargarCompleto()
    End Sub

    ''' <summary>
    ''' Una fila por sección de los muros ya calculados. Grilla y Excel leen de
    ''' aquí: así no pueden volver a mostrar valores o estados distintos.
    ''' </summary>
    Private Function ConstruirFilas() As List(Of FilaSeccionMuro)
        Dim filas As New List(Of FilaSeccionMuro)
        For Each muro In MurosResumenService.MurosCalculados(Muros)
            For Each sec In muro.Lista_Secciones
                filas.Add(MurosResumenService.CalcularFilaSeccion(muro, sec))
            Next
        Next
        Return filas
    End Function

    Private _filas As New List(Of FilaSeccionMuro)

    Private Shared Function TextoEstado(e As EstadoChequeoMuro, textoRevisar As String) As String
        Select Case e
            Case EstadoChequeoMuro.OK : Return "OK"
            Case EstadoChequeoMuro.SinDato : Return "Sin dato"
            Case Else : Return textoRevisar
        End Select
    End Function

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
        For Each f In _filas
            Dim row = dgv.Rows(dgv.Rows.Add())
            row.Cells("Muro").Value = f.Muro
            row.Cells("Piso").Value = f.Piso
            row.Cells("tw").Value = Math.Round(f.tw, 3)
            row.Cells("Lw").Value = Math.Round(f.Lw, 2)
            row.Cells("CTop").Value = Math.Round(f.CuantiaTopReq, 3)
            row.Cells("CBot").Value = Math.Round(f.CuantiaBotReq, 3)
            AsignarFactor(row.Cells("FTop"), f.FTop)
            AsignarFactor(row.Cells("FBot"), f.FBot)
            If f.FDI > 0 Then AsignarFactor(row.Cells("FDI"), f.FDI) Else row.Cells("FDI").Value = "-"
            row.Cells("CombCrit").Value = f.CombCritica
            PintarEstado(row.Cells("Estado"), TextoEstado(f.EstadoFlexion, "Revisar"))

            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1
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
        For Each f In _filas
            Dim row = dgv.Rows(dgv.Rows.Add())
            row.Cells("Muro").Value = f.Muro
            row.Cells("Piso").Value = f.Piso
            row.Cells("tw").Value = Math.Round(f.tw, 3)
            row.Cells("Lw").Value = Math.Round(f.Lw, 2)
            row.Cells("Vu").Value = Math.Round(f.Vu, 1)
            row.Cells("Vc").Value = Math.Round(f.Vc, 1)
            row.Cells("Vs").Value = Math.Round(f.Vs, 1)
            row.Cells("Vn").Value = Math.Round(f.Vn, 1)
            AsignarFactor(row.Cells("FCor"), f.FCortante)
            PintarEstado(row.Cells("Estado"), If(f.CumpleCortante, "OK", "Revisar"))
            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1
        Next
    End Sub

    ' ── ELEMENTOS DE BORDE ────────────────────────────────────────────────────
    ' Cada chequeo es el peor de los extremos superior e inferior de la sección.

    Private Sub CargarEB()
        Dim dgv = DgvEB
        dgv.Columns.Clear() : dgv.Rows.Clear()
        ACol(dgv, "Muro", "Muro", 120) : ACol(dgv, "Piso", "Piso", 80)
        ACol(dgv, "CLim", "C límite (m)", 90)
        ACol(dgv, "CI", "C_I máx (m)", 90) : ACol(dgv, "CD", "C_D máx (m)", 90)
        ACol(dgv, "EB_I_Esf", "EB Izq Esf.", 100) : ACol(dgv, "EB_D_Esf", "EB Der Esf.", 100)
        ACol(dgv, "EB_I_Def", "EB Izq Def.", 100) : ACol(dgv, "EB_D_Def", "EB Der Def.", 100)
        ACol(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0
        For Each f In _filas
            Dim row = dgv.Rows(dgv.Rows.Add())
            row.Cells("Muro").Value = f.Muro
            row.Cells("Piso").Value = f.Piso
            row.Cells("CLim").Value = Math.Round(f.CLimite, 3)
            row.Cells("CI").Value = Math.Round(f.C_I, 3)
            row.Cells("CD").Value = Math.Round(f.C_D, 3)

            PintarChequeoEB(row.Cells("EB_I_Esf"), f.EB_I_Esf)
            PintarChequeoEB(row.Cells("EB_D_Esf"), f.EB_D_Esf)
            PintarChequeoEB(row.Cells("EB_I_Def"), f.EB_I_Def)
            PintarChequeoEB(row.Cells("EB_D_Def"), f.EB_D_Def)
            PintarEstado(row.Cells("Estado"), If(f.RequiereEB, "Rev. EB", "OK"))

            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1
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
        For Each f In _filas
            Dim row = dgv.Rows(dgv.Rows.Add())
            row.Cells("Muro").Value = f.Muro
            row.Cells("Piso").Value = f.Piso
            row.Cells("tw").Value = Math.Round(f.tw, 3)
            row.Cells("Lw").Value = Math.Round(f.Lw, 2)
            AsignarFactor(row.Cells("FFlex"), f.FFlexMin)
            AsignarFactor(row.Cells("FCor"), f.FCortante)
            PintarEstado(row.Cells("EstFlex"), TextoEstado(f.EstadoFlexion, "Rev."))
            PintarEstado(row.Cells("EstCor"), If(f.CumpleCortante, "OK", "Rev."))
            PintarEstado(row.Cells("EstEB"), If(f.RequiereEB, "Rev. EB", "OK"))
            PintarEstado(row.Cells("Estado"), f.EstadoGlobal)

            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1
        Next
    End Sub

    ' ── EXPORTAR EXCEL ────────────────────────────────────────────────────────

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        If _filas Is Nothing OrElse _filas.Count = 0 Then
            MessageBox.Show("No hay muros calculados para exportar.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
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
            Logger.Error(ex, "Form_Reporte_Resumen_Muros.BtnExportar_Click", dlg.FileName)
            MessageBox.Show("Error al exportar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportarHojaFlexion(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Flexo-Compresión")
        Dim enc = {"Muro", "Piso", "tw (m)", "Lw (m)", "ρ_req Top", "ρ_req Bot", "F Top", "F Bot", "F Diagrama I.", "Comb. Crítica", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each f In _filas
            ws.Cell(fila, 1).Value = f.Muro : ws.Cell(fila, 2).Value = f.Piso
            ws.Cell(fila, 3).Value = f.tw : ws.Cell(fila, 4).Value = f.Lw
            ws.Cell(fila, 5).Value = f.CuantiaTopReq : ws.Cell(fila, 6).Value = f.CuantiaBotReq
            EscribirFactor(ws.Cell(fila, 7), f.FTop) : EscribirFactor(ws.Cell(fila, 8), f.FBot)
            If f.FDI > 0 Then EscribirFactor(ws.Cell(fila, 9), f.FDI) Else ws.Cell(fila, 9).Value = "-"
            ws.Cell(fila, 10).Value = f.CombCritica
            EscribirEstadoXL(ws.Cell(fila, 11), TextoEstado(f.EstadoFlexion, "Revisar"))
            EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1)
            fila += 1
        Next
        AjustarColumnas(ws, enc.Length)
        AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    Private Sub ExportarHojaCortante(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Cortante")
        Dim enc = {"Muro", "Piso", "tw (m)", "Lw (m)", "Vu (kN)", "Vc (kN)", "Vs (kN)", "Vn (kN)", "F Cortante", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each f In _filas
            ws.Cell(fila, 1).Value = f.Muro : ws.Cell(fila, 2).Value = f.Piso
            ws.Cell(fila, 3).Value = f.tw : ws.Cell(fila, 4).Value = f.Lw
            ws.Cell(fila, 5).Value = Math.Round(f.Vu, 1) : ws.Cell(fila, 6).Value = Math.Round(f.Vc, 1)
            ws.Cell(fila, 7).Value = Math.Round(f.Vs, 1) : ws.Cell(fila, 8).Value = Math.Round(f.Vn, 1)
            EscribirFactor(ws.Cell(fila, 9), f.FCortante)
            EscribirEstadoXL(ws.Cell(fila, 10), If(f.CumpleCortante, "OK", "Revisar"))
            EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1)
            fila += 1
        Next
        AjustarColumnas(ws, enc.Length) : AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    Private Sub ExportarHojaEB(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Elementos de Borde")
        Dim enc = {"Muro", "Piso", "C límite (m)", "C_I máx (m)", "C_D máx (m)", "EB Izq Esf.", "EB Der Esf.", "EB Izq Def.", "EB Der Def.", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each f In _filas
            ws.Cell(fila, 1).Value = f.Muro : ws.Cell(fila, 2).Value = f.Piso
            ws.Cell(fila, 3).Value = Math.Round(f.CLimite, 3) : ws.Cell(fila, 4).Value = Math.Round(f.C_I, 3)
            ws.Cell(fila, 5).Value = Math.Round(f.C_D, 3)
            EscribirChequeoEB_XL(ws.Cell(fila, 6), f.EB_I_Esf)
            EscribirChequeoEB_XL(ws.Cell(fila, 7), f.EB_D_Esf)
            EscribirChequeoEB_XL(ws.Cell(fila, 8), f.EB_I_Def)
            EscribirChequeoEB_XL(ws.Cell(fila, 9), f.EB_D_Def)
            EscribirEstadoXL(ws.Cell(fila, 10), If(f.RequiereEB, "Rev. EB", "OK"))
            EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1) : fila += 1
        Next
        AjustarColumnas(ws, enc.Length) : AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    Private Sub ExportarHojaCompleto(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Resumen Completo")
        Dim enc = {"Muro", "Piso", "tw (m)", "Lw (m)", "F Flex mín", "F Cortante", "Flexión", "Cortante", "EB", "Estado"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila = 2
        For Each f In _filas
            ws.Cell(fila, 1).Value = f.Muro : ws.Cell(fila, 2).Value = f.Piso
            ws.Cell(fila, 3).Value = f.tw : ws.Cell(fila, 4).Value = f.Lw
            EscribirFactor(ws.Cell(fila, 5), f.FFlexMin) : EscribirFactor(ws.Cell(fila, 6), f.FCortante)
            EscribirEstadoXL(ws.Cell(fila, 7), TextoEstado(f.EstadoFlexion, "Rev."))
            EscribirEstadoXL(ws.Cell(fila, 8), If(f.CumpleCortante, "OK", "Rev."))
            EscribirEstadoXL(ws.Cell(fila, 9), If(f.RequiereEB, "Rev. EB", "OK"))
            EscribirEstadoXL(ws.Cell(fila, 10), f.EstadoGlobal)
            EstilarFila(ws, fila, enc.Length, fila Mod 2 = 1) : fila += 1
        Next
        AjustarColumnas(ws, enc.Length) : AgregarBordes(ws, 1, fila - 1, enc.Length)
    End Sub

    ' ── Helpers ClosedXML ─────────────────────────────────────────────────────

    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, enc As String())
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirEncabezados(ws, fila, enc, conBorde:=False)
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirFactor(cell, valor)
    End Sub

    Private Sub EscribirEstadoXL(cell As IXLCell, estado As String)
        cell.Value = estado
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        cell.Style.Font.Bold = True
        Select Case estado
            Case "OK"
                cell.Style.Fill.BackgroundColor = XlOKFondo
                cell.Style.Font.FontColor = XlOKTexto
            Case "Rev. EB", "Sin dato"
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
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.AjustarColumnas(ws, numCols, anchoMaximo:=40)
    End Sub

    ' ── Helpers UI ───────────────────────────────────────────────────────────

    Private Sub EstilarGrid(dgv As DataGridView)
        ' Delega en los helpers compartidos de reporte.
        ReporteGridHelpers.EstilarGrid(dgv)
    End Sub

    Private Sub ACol(dgv As DataGridView, nombre As String, header As String, ancho As Integer)
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = nombre, .HeaderText = header, .Width = ancho})
    End Sub

    Private Sub AsignarFactor(cell As DataGridViewCell, valor As Double)
        ' Semáforo con el umbral único (UMBRAL_CD); NaN = sin dato -> "—".
        ReporteGridHelpers.AsignarCD(cell, valor)
    End Sub

    ''' <summary>Pinta un estado con el mismo código de color que el Excel.</summary>
    Private Sub PintarEstado(cell As DataGridViewCell, texto As String)
        Select Case texto
            Case "OK"
                AplicarEstado(cell, texto, ColorOK, ColorOKTexto)
            Case "Rev. EB", "Sin dato"
                AplicarEstado(cell, texto, ColorAlerta, ColorAlertaTexto)
            Case Else
                AplicarEstado(cell, texto, ColorMal, ColorMalTexto)
        End Select
    End Sub

    Private Sub AplicarEstado(cell As DataGridViewCell, texto As String, fondo As Color, textoColor As Color)
        ' Delega en los helpers compartidos de reporte.
        ReporteGridHelpers.AsignarEstado(cell, texto, fondo, textoColor)
    End Sub

End Class
