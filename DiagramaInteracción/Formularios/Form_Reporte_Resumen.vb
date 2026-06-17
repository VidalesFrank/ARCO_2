Imports System.Drawing
Imports ARCO.eNumeradores
Imports ClosedXML.Excel

Public Class Form_Reporte_Resumen
    Inherits Form

    ' ── Datos del modelo ──────────────────────────────────────────────────────
    Public Property Vigas As List(Of cViga)

    ' ── Paleta (igual que la app) ─────────────────────────────────────────────
    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorEncabezadoTexto As Color = Color.White
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ColorAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ColorAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    ' Colores ClosedXML equivalentes
    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlAlertaFondo As XLColor = XLColor.FromHtml("#FFEB9C")
    Private ReadOnly XlAlertaTexto As XLColor = XLColor.FromHtml("#9C5700")
    Private ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F8F8F8")

    ' ── Grids ─────────────────────────────────────────────────────────────────
    Private WithEvents DgvFlexion As New DataGridView()
    Private WithEvents DgvCortanteTodas As New DataGridView()
    Private WithEvents DgvCortanteNoCumple As New DataGridView()
    Private WithEvents DgvCompleto As New DataGridView()

    ' ── Tab activo para saber qué exportar ────────────────────────────────────
    Private _tabs As TabControl

    ' =========================================================================
    Public Sub New()

        Me.Text = "Reporte Resumen — Diseño de Vigas"
        Me.Size = New Size(1320, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' ── TabControl ────────────────────────────────────────────────────────
        _tabs = New TabControl()
        _tabs.Dock = DockStyle.Fill
        _tabs.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _tabs.Padding = New Point(16, 6)

        ' Tab 1 — Flexión
        Dim tabFlex As New TabPage("  Resumen Flexión  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvFlexion.Dock = DockStyle.Fill
        EstilarGrid(DgvFlexion)
        tabFlex.Controls.Add(DgvFlexion)
        _tabs.TabPages.Add(tabFlex)

        ' Tab 2 — Cortante
        Dim tabCor As New TabPage("  Resumen Cortante  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        Dim split As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Horizontal,
            .SplitterDistance = 340,
            .BackColor = Color.FromArgb(200, 200, 200)
        }
        split.Panel1.Controls.Add(DgvCortanteTodas)
        split.Panel1.Controls.Add(CrearPanelHeader("Todas las Vigas"))
        DgvCortanteTodas.Dock = DockStyle.Fill
        EstilarGrid(DgvCortanteTodas)
        split.Panel2.Controls.Add(DgvCortanteNoCumple)
        split.Panel2.Controls.Add(CrearPanelHeader("Vigas que No Cumplen a Cortante"))
        DgvCortanteNoCumple.Dock = DockStyle.Fill
        EstilarGrid(DgvCortanteNoCumple)
        tabCor.Controls.Add(split)
        _tabs.TabPages.Add(tabCor)

        ' Tab 3 — Completo
        Dim tabComp As New TabPage("  Resumen Completo  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvCompleto.Dock = DockStyle.Fill
        EstilarGrid(DgvCompleto)
        tabComp.Controls.Add(DgvCompleto)
        _tabs.TabPages.Add(tabComp)

        Me.Controls.Add(_tabs)

        ' ── Barra inferior ────────────────────────────────────────────────────
        Dim barra As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 9, 10, 9)
        }

        Dim btnActualizar As New Button() With {
            .Text = "Actualizar",
            .Size = New Size(120, 36),
            .Location = New Point(10, 9),
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
            .Location = New Point(140, 9),
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

    End Sub

    Private Function CrearPanelHeader(titulo As String) As Panel
        Dim p As New Panel() With {.Dock = DockStyle.Top, .Height = 36, .BackColor = Color.FromArgb(60, 60, 60)}
        Dim lbl As New Label() With {
            .Text = "  " & titulo,
            .Dock = DockStyle.Fill,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        p.Controls.Add(lbl)
        Return p
    End Function

    ' =========================================================================
    ' CARGA
    ' =========================================================================

    Private Sub Form_Load(sender As Object, e As EventArgs)
        CargarTodo()
    End Sub

    Private Sub CargarTodo()
        If Vigas Is Nothing Then Return
        CargarResumenFlexion()
        CargarResumenCortante()
        CargarResumenCompleto()
    End Sub

    ' ── RESUMEN FLEXIÓN ───────────────────────────────────────────────────────

    Private Sub CargarResumenFlexion()

        Dim dgv = DgvFlexion
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Piso", "Piso", 100)
        AgregarColumna(dgv, "Viga", "Nombre (plano)", 150)
        AgregarColumna(dgv, "Frames", "Frames ETABS", 200)
        AgregarColumna(dgv, "FNeg", "F M- mín", 110)
        AgregarColumna(dgv, "FPos", "F M+ mín", 110)

        Dim idx As Integer = 0

        For Each viga In Vigas

            If Not viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any()) Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue

            For Each frame In viga.Frames
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act.AsReqSup > 0 AndAlso act.RatioSup > 0 Then fNegMin = Math.Min(fNegMin, act.RatioSup)
                    If act.AsReqInf > 0 AndAlso act.RatioInf > 0 Then fPosMin = Math.Min(fPosMin, act.RatioInf)
                Next
            Next

            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)

            row.Cells("Piso").Value = viga.Piso
            row.Cells("Viga").Value = NombreReporte(viga)
            row.Cells("Frames").Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            AsignarFactorCelda(row.Cells("FNeg"), fNegMin)
            AsignarFactorCelda(row.Cells("FPos"), fPosMin)

            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1

        Next

    End Sub

    ' ── RESUMEN CORTANTE ──────────────────────────────────────────────────────

    Private Sub CargarResumenCortante()

        For Each dgv In {DgvCortanteTodas, DgvCortanteNoCumple}
            dgv.Columns.Clear()
            dgv.Rows.Clear()
            AgregarColumna(dgv, "Piso", "Piso", 100)
            AgregarColumna(dgv, "Viga", "Nombre (plano)", 150)
            AgregarColumna(dgv, "Frames", "Frames ETABS", 200)
            AgregarColumna(dgv, "FMin", "F Cortante mín", 130)
            AgregarColumna(dgv, "Zona", "Zona crítica", 120)
            AgregarColumna(dgv, "Cumple", "Cumple", 90)
        Next

        Dim idxT As Integer = 0
        Dim idxN As Integer = 0

        For Each viga In Vigas

            If Not viga.Frames.Any(Function(f) f.RevisionCortante.Any(Function(z) z.phiVn > 0)) Then Continue For

            Dim fMin As Double = Double.MaxValue
            Dim zonaCritica As String = "-"
            Dim cumpleViga As Boolean = True

            For Each frame In viga.Frames
                For Each zona In frame.RevisionCortante
                    If zona.phiVn = 0 Then Continue For
                    If zona.Factor < fMin Then
                        fMin = zona.Factor
                        zonaCritica = frame.ObjectLabel & " " & PosTexto(zona.Posicion)
                    End If
                    If Not zona.Cumple Then cumpleViga = False
                Next
            Next

            Dim frameLabels = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))
            Dim fVal = Math.Round(Math.Min(If(fMin = Double.MaxValue, 0.0, fMin), 9.99), 2)

            AgregarFilaCortante(DgvCortanteTodas, idxT, viga, frameLabels, fVal, zonaCritica, cumpleViga)
            idxT += 1

            If Not cumpleViga Then
                AgregarFilaCortante(DgvCortanteNoCumple, idxN, viga, frameLabels, fVal, zonaCritica, cumpleViga)
                idxN += 1
            End If

        Next

        If idxN = 0 Then
            Dim r = DgvCortanteNoCumple.Rows.Add()
            DgvCortanteNoCumple.Rows(r).Cells("Piso").Value = "Todas las vigas cumplen a cortante"
            DgvCortanteNoCumple.Rows(r).DefaultCellStyle.BackColor = ColorOK
            DgvCortanteNoCumple.Rows(r).DefaultCellStyle.ForeColor = ColorOKTexto
        End If

    End Sub

    Private Sub AgregarFilaCortante(dgv As DataGridView, idx As Integer, viga As cViga,
                                    frames As String, fVal As Double, zona As String, cumple As Boolean)
        Dim r = dgv.Rows.Add()
        Dim row = dgv.Rows(r)

        row.Cells("Piso").Value = viga.Piso
        row.Cells("Viga").Value = NombreReporte(viga)
        row.Cells("Frames").Value = frames
        row.Cells("FMin").Value = fVal
        row.Cells("Zona").Value = zona
        row.Cells("Cumple").Value = If(cumple, "SI", "NO")

        Dim fondo = If(cumple, ColorOK, ColorMal)
        Dim texto = If(cumple, ColorOKTexto, ColorMalTexto)

        row.Cells("FMin").Style.BackColor = fondo
        row.Cells("FMin").Style.ForeColor = texto
        row.Cells("Cumple").Style.BackColor = fondo
        row.Cells("Cumple").Style.ForeColor = texto

        If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)

    End Sub

    ' ── RESUMEN COMPLETO ──────────────────────────────────────────────────────

    Private Sub CargarResumenCompleto()

        Dim dgv = DgvCompleto
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Piso", "Piso", 100)
        AgregarColumna(dgv, "Viga", "Nombre (plano)", 150)
        AgregarColumna(dgv, "Frames", "Frames ETABS", 200)
        AgregarColumna(dgv, "FNeg", "F M- mín", 110)
        AgregarColumna(dgv, "FPos", "F M+ mín", 110)
        AgregarColumna(dgv, "FCor", "F Cor mín", 110)
        AgregarColumna(dgv, "EstFlex", "Flexión", 90)
        AgregarColumna(dgv, "EstCor", "Cortante", 90)
        AgregarColumna(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0

        For Each viga In Vigas

            Dim tieneFlex = viga.Frames.Any(Function(f) f.RevisionFlexion.Count > 0)
            Dim tieneCor = viga.Frames.Any(Function(f) f.RevisionCortante.Count > 0)
            If Not tieneFlex AndAlso Not tieneCor Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue
            Dim fCorMin As Double = Double.MaxValue
            Dim cumpleFlex As Boolean = True
            Dim cumpleCor As Boolean = True
            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())

            For Each frame In viga.Frames
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act.AsReqSup > 0 AndAlso act.RatioSup > 0 Then
                        fNegMin = Math.Min(fNegMin, act.RatioSup)
                        If Not act.CumpleSuperior Then cumpleFlex = False
                    End If
                    If act.AsReqInf > 0 AndAlso act.RatioInf > 0 Then
                        fPosMin = Math.Min(fPosMin, act.RatioInf)
                        If Not act.CumpleInferior Then cumpleFlex = False
                    End If
                Next
                For Each zona In frame.RevisionCortante
                    If zona.phiVn > 0 Then
                        fCorMin = Math.Min(fCorMin, zona.Factor)
                        If Not zona.Cumple Then cumpleCor = False
                    End If
                Next
            Next

            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)

            row.Cells("Piso").Value = viga.Piso
            row.Cells("Viga").Value = NombreReporte(viga)
            row.Cells("Frames").Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            AsignarFactorCelda(row.Cells("FNeg"), fNegMin)
            AsignarFactorCelda(row.Cells("FPos"), fPosMin)
            AsignarFactorCelda(row.Cells("FCor"), fCorMin)

            ' Flexión
            If Not tieneRef Then
                AplicarEstado(row.Cells("EstFlex"), "Sin ref.", ColorAlerta, ColorAlertaTexto)
            ElseIf cumpleFlex Then
                AplicarEstado(row.Cells("EstFlex"), "OK", ColorOK, ColorOKTexto)
            Else
                AplicarEstado(row.Cells("EstFlex"), "Revisar", ColorMal, ColorMalTexto)
            End If

            ' Cortante
            If fCorMin = Double.MaxValue Then
                AplicarEstado(row.Cells("EstCor"), "Sin datos", ColorAlerta, ColorAlertaTexto)
            ElseIf cumpleCor Then
                AplicarEstado(row.Cells("EstCor"), "OK", ColorOK, ColorOKTexto)
            Else
                AplicarEstado(row.Cells("EstCor"), "Revisar", ColorMal, ColorMalTexto)
            End If

            ' Estado general
            Dim ok = tieneRef AndAlso cumpleFlex AndAlso fCorMin <> Double.MaxValue AndAlso cumpleCor
            Dim pendiente = Not tieneRef OrElse fCorMin = Double.MaxValue

            If ok Then
                AplicarEstado(row.Cells("Estado"), "OK", ColorOK, ColorOKTexto)
            ElseIf pendiente Then
                AplicarEstado(row.Cells("Estado"), "Pendiente", ColorAlerta, ColorAlertaTexto)
            Else
                AplicarEstado(row.Cells("Estado"), "Revisar", ColorMal, ColorMalTexto)
            End If

            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1

        Next

    End Sub

    ' =========================================================================
    ' EXPORTAR EXCEL — ClosedXML
    ' =========================================================================

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)

        If Vigas Is Nothing OrElse Vigas.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim dlg As New SaveFileDialog() With {
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = $"ARCO_Vigas_Reporte_{DateTime.Now:yyyyMMdd}",
            .Title = "Exportar Reporte de Vigas"
        }

        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Me.Cursor = Cursors.WaitCursor

            Using wb As New XLWorkbook()

                ExportarHojaFlexion(wb)
                ExportarHojaCortante(wb)
                ExportarHojaCompleto(wb)

                wb.SaveAs(dlg.FileName)

            End Using

            Me.Cursor = Cursors.Default
            MessageBox.Show("Reporte exportado correctamente." & vbCrLf & dlg.FileName,
                            "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            MessageBox.Show("Error al exportar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    ' ── Hoja Flexión ──────────────────────────────────────────────────────────

    Private Sub ExportarHojaFlexion(wb As XLWorkbook)

        Dim ws = wb.Worksheets.Add("Resumen Flexión")

        Dim encabezados = {"Piso", "Nombre (plano)", "Frames ETABS", "F M- mín", "F M+ mín"}
        EscribirEncabezados(ws, 1, encabezados)

        Dim fila As Integer = 2

        For Each viga In Vigas

            If Not viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any()) Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue

            For Each frame In viga.Frames
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act.AsReqSup > 0 AndAlso act.RatioSup > 0 Then fNegMin = Math.Min(fNegMin, act.RatioSup)
                    If act.AsReqInf > 0 AndAlso act.RatioInf > 0 Then fPosMin = Math.Min(fPosMin, act.RatioInf)
                Next
            Next

            ws.Cell(fila, 1).Value = viga.Piso
            ws.Cell(fila, 2).Value = NombreReporte(viga)
            ws.Cell(fila, 3).Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            EscribirFactor(ws.Cell(fila, 4), fNegMin)
            EscribirFactor(ws.Cell(fila, 5), fPosMin)

            EstilarFilaDatos(ws, fila, encabezados.Length, fila Mod 2 = 1)
            fila += 1

        Next

        AjustarColumnas(ws, encabezados.Length)
        AgregarBordesTabla(ws, 1, fila - 1, encabezados.Length)

    End Sub

    ' ── Hoja Cortante ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCortante(wb As XLWorkbook)

        Dim ws = wb.Worksheets.Add("Resumen Cortante")

        Dim encabezados = {"Piso", "Nombre (plano)", "Frames ETABS", "F Cortante mín", "Zona crítica", "Cumple"}

        ' Bloque 1: Todas las vigas
        ws.Cell(1, 1).Value = "TODAS LAS VIGAS"
        With ws.Cell(1, 1).Style
            .Font.Bold = True
            .Font.FontSize = 11
            .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XLColor.FromHtml("#3C3C3C")
        End With
        ws.Range(1, 1, 1, encabezados.Length).Merge()

        EscribirEncabezados(ws, 2, encabezados)

        Dim fila As Integer = 3
        Dim filaInicioTodas As Integer = 3
        Dim vigasNoCumplen As New List(Of (Piso As String, Nombre As String, Frames As String, FVal As Double, Zona As String, Cumple As Boolean))

        For Each viga In Vigas

            If Not viga.Frames.Any(Function(f) f.RevisionCortante.Any(Function(z) z.phiVn > 0)) Then Continue For

            Dim fMin As Double = Double.MaxValue
            Dim zonaCritica As String = "-"
            Dim cumpleViga As Boolean = True

            For Each frame In viga.Frames
                For Each zona In frame.RevisionCortante
                    If zona.phiVn = 0 Then Continue For
                    If zona.Factor < fMin Then
                        fMin = zona.Factor
                        zonaCritica = frame.ObjectLabel & " " & PosTexto(zona.Posicion)
                    End If
                    If Not zona.Cumple Then cumpleViga = False
                Next
            Next

            Dim fVal = Math.Round(Math.Min(If(fMin = Double.MaxValue, 0.0, fMin), 9.99), 2)
            Dim frameLabels = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            ws.Cell(fila, 1).Value = viga.Piso
            ws.Cell(fila, 2).Value = NombreReporte(viga)
            ws.Cell(fila, 3).Value = frameLabels
            EscribirFactor(ws.Cell(fila, 4), If(fMin = Double.MaxValue, 0.0, fMin))
            ws.Cell(fila, 5).Value = zonaCritica
            EscribirCeldaCumple(ws.Cell(fila, 6), cumpleViga)

            EstilarFilaDatos(ws, fila, encabezados.Length, fila Mod 2 = 1)
            fila += 1

            If Not cumpleViga Then
                vigasNoCumplen.Add((viga.Piso, NombreReporte(viga), frameLabels, fVal, zonaCritica, cumpleViga))
            End If

        Next

        AgregarBordesTabla(ws, 2, fila - 1, encabezados.Length)

        ' Separador
        fila += 1

        ' Bloque 2: Sólo las que no cumplen
        ws.Cell(fila, 1).Value = "VIGAS QUE NO CUMPLEN A CORTANTE"
        With ws.Cell(fila, 1).Style
            .Font.Bold = True
            .Font.FontSize = 11
            .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XLColor.FromHtml("#9C0006")
        End With
        ws.Range(fila, 1, fila, encabezados.Length).Merge()
        fila += 1

        EscribirEncabezados(ws, fila, encabezados)
        fila += 1

        If vigasNoCumplen.Count = 0 Then
            ws.Cell(fila, 1).Value = "Todas las vigas cumplen a cortante"
            ws.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C6EFCE")
            ws.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#006100")
            ws.Range(fila, 1, fila, encabezados.Length).Merge()
        Else
            For Each item In vigasNoCumplen
                ws.Cell(fila, 1).Value = item.Piso
                ws.Cell(fila, 2).Value = item.Nombre
                ws.Cell(fila, 3).Value = item.Frames
                EscribirFactor(ws.Cell(fila, 4), item.FVal)
                ws.Cell(fila, 5).Value = item.Zona
                EscribirCeldaCumple(ws.Cell(fila, 6), False)
                EstilarFilaDatos(ws, fila, encabezados.Length, fila Mod 2 = 1)
                fila += 1
            Next
            AgregarBordesTabla(ws, fila - vigasNoCumplen.Count - 1, fila - 1, encabezados.Length)
        End If

        AjustarColumnas(ws, encabezados.Length)

    End Sub

    ' ── Hoja Completo ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCompleto(wb As XLWorkbook)

        Dim ws = wb.Worksheets.Add("Resumen Completo")

        Dim encabezados = {"Piso", "Nombre (plano)", "Frames ETABS", "F M- mín", "F M+ mín", "F Cor mín", "Flexión", "Cortante", "Estado"}
        EscribirEncabezados(ws, 1, encabezados)

        Dim fila As Integer = 2

        For Each viga In Vigas

            Dim tieneFlex = viga.Frames.Any(Function(f) f.RevisionFlexion.Count > 0)
            Dim tieneCor = viga.Frames.Any(Function(f) f.RevisionCortante.Count > 0)
            If Not tieneFlex AndAlso Not tieneCor Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue
            Dim fCorMin As Double = Double.MaxValue
            Dim cumpleFlex As Boolean = True
            Dim cumpleCor As Boolean = True
            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())

            For Each frame In viga.Frames
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act.AsReqSup > 0 AndAlso act.RatioSup > 0 Then
                        fNegMin = Math.Min(fNegMin, act.RatioSup)
                        If Not act.CumpleSuperior Then cumpleFlex = False
                    End If
                    If act.AsReqInf > 0 AndAlso act.RatioInf > 0 Then
                        fPosMin = Math.Min(fPosMin, act.RatioInf)
                        If Not act.CumpleInferior Then cumpleFlex = False
                    End If
                Next
                For Each zona In frame.RevisionCortante
                    If zona.phiVn > 0 Then
                        fCorMin = Math.Min(fCorMin, zona.Factor)
                        If Not zona.Cumple Then cumpleCor = False
                    End If
                Next
            Next

            ws.Cell(fila, 1).Value = viga.Piso
            ws.Cell(fila, 2).Value = NombreReporte(viga)
            ws.Cell(fila, 3).Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            EscribirFactor(ws.Cell(fila, 4), fNegMin)
            EscribirFactor(ws.Cell(fila, 5), fPosMin)
            EscribirFactor(ws.Cell(fila, 6), fCorMin)

            ' Col 7 — Flexión
            If Not tieneRef Then
                EscribirEstado(ws.Cell(fila, 7), "Sin ref.", XLAlertaFondo, XLAlertaTexto)
            ElseIf cumpleFlex Then
                EscribirEstado(ws.Cell(fila, 7), "OK", XLOKFondo, XLOKTexto)
            Else
                EscribirEstado(ws.Cell(fila, 7), "Revisar", XLMalFondo, XLMalTexto)
            End If

            ' Col 8 — Cortante
            If fCorMin = Double.MaxValue Then
                EscribirEstado(ws.Cell(fila, 8), "Sin datos", XLAlertaFondo, XLAlertaTexto)
            ElseIf cumpleCor Then
                EscribirEstado(ws.Cell(fila, 8), "OK", XLOKFondo, XLOKTexto)
            Else
                EscribirEstado(ws.Cell(fila, 8), "Revisar", XLMalFondo, XLMalTexto)
            End If

            ' Col 9 — Estado general
            Dim ok = tieneRef AndAlso cumpleFlex AndAlso fCorMin <> Double.MaxValue AndAlso cumpleCor
            If ok Then
                EscribirEstado(ws.Cell(fila, 9), "OK", XLOKFondo, XLOKTexto)
            ElseIf Not tieneRef OrElse fCorMin = Double.MaxValue Then
                EscribirEstado(ws.Cell(fila, 9), "Pendiente", XLAlertaFondo, XLAlertaTexto)
            Else
                EscribirEstado(ws.Cell(fila, 9), "Revisar", XLMalFondo, XLMalTexto)
            End If

            EstilarFilaDatos(ws, fila, encabezados.Length, fila Mod 2 = 1)
            fila += 1

        Next

        AjustarColumnas(ws, encabezados.Length)
        AgregarBordesTabla(ws, 1, fila - 1, encabezados.Length)

    End Sub

    ' =========================================================================
    ' HELPERS CLOSEDXML
    ' =========================================================================

    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, encabezados As String())
        For i As Integer = 0 To encabezados.Length - 1
            Dim cell = ws.Cell(fila, i + 1)
            cell.Value = encabezados(i)
            With cell.Style
                .Fill.BackgroundColor = XlEncabezado
                .Font.FontColor = XLColor.White
                .Font.Bold = True
                .Font.FontSize = 11
                .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                .Alignment.Vertical = XLAlignmentVerticalValues.Center
                .Border.OutsideBorder = XLBorderStyleValues.Thin
                .Border.OutsideBorderColor = XLColor.White
            End With
            ws.Row(fila).Height = 22
        Next
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        If valor = Double.MaxValue Then
            cell.Value = "-"
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            Return
        End If

        Dim v = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center

        If v >= 0.9 Then
            cell.Style.Fill.BackgroundColor = XLOKFondo
            cell.Style.Font.FontColor = XLOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XLMalFondo
            cell.Style.Font.FontColor = XLMalTexto
        End If
        cell.Style.Font.Bold = True
    End Sub

    Private Sub EscribirCeldaCumple(cell As IXLCell, cumple As Boolean)
        cell.Value = If(cumple, "SI", "NO")
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        cell.Style.Font.Bold = True
        If cumple Then
            cell.Style.Fill.BackgroundColor = XLOKFondo
            cell.Style.Font.FontColor = XLOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XLMalFondo
            cell.Style.Font.FontColor = XLMalTexto
        End If
    End Sub

    Private Sub EscribirEstado(cell As IXLCell, texto As String, fondo As XLColor, textoColor As XLColor)
        cell.Value = texto
        cell.Style.Fill.BackgroundColor = fondo
        cell.Style.Font.FontColor = textoColor
        cell.Style.Font.Bold = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
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
            If col <= 3 Then
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            End If
        Next
    End Sub

    Private Sub AgregarBordesTabla(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        If filaFin < filaIni Then Return
        Dim rango = ws.Range(filaIni, 1, filaFin, numCols)
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair
        rango.Style.Border.InsideBorderColor = XLColor.FromHtml("#CCCCCC")
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Medium
        rango.Style.Border.OutsideBorderColor = XlEncabezado
    End Sub

    Private Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer)
        ws.Columns(1, numCols).AdjustToContents()
        ' Mínimo para columnas de Frames ETABS (col 3)
        If ws.Column(3).Width < 25 Then ws.Column(3).Width = 25
        ' Máximo para evitar columnas muy anchas
        For c = 1 To numCols
            If ws.Column(c).Width > 45 Then ws.Column(c).Width = 45
        Next
        ' Fijar hoja: congelar primera fila de encabezados
        ws.SheetView.FreezeRows(1)
    End Sub

    ' =========================================================================
    ' HELPERS UI (DataGridView)
    ' =========================================================================

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
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = nombre, .HeaderText = header, .Width = ancho})
    End Sub

    Private Sub AsignarFactorCelda(cell As DataGridViewCell, factor As Double)
        If factor = Double.MaxValue Then
            cell.Value = "-"
            Return
        End If
        Dim v = Math.Round(Math.Min(factor, 9.99), 2)
        cell.Value = v.ToString("F2")
        PintarCeldaFactor(cell, factor)
    End Sub

    Private Sub PintarCeldaFactor(cell As DataGridViewCell, factor As Double)
        If factor >= 0.9 Then
            cell.Style.BackColor = ColorOK : cell.Style.ForeColor = ColorOKTexto
        Else
            cell.Style.BackColor = ColorMal : cell.Style.ForeColor = ColorMalTexto
        End If
    End Sub

    Private Sub AplicarEstado(cell As DataGridViewCell, texto As String, fondo As Color, textoColor As Color)
        cell.Value = texto
        cell.Style.BackColor = fondo
        cell.Style.ForeColor = textoColor
    End Sub

    Private Function NombreReporte(viga As cViga) As String
        Return If(String.IsNullOrWhiteSpace(viga.NombrePlano), viga.Nombre, viga.NombrePlano)
    End Function

    Private Function PosTexto(pos As PosicionTramoViga) As String
        Select Case pos
            Case PosicionTramoViga.Izquierda : Return "Izq"
            Case PosicionTramoViga.Centro : Return "Cen"
            Case PosicionTramoViga.Derecha : Return "Der"
            Case Else : Return "?"
        End Select
    End Function

End Class
