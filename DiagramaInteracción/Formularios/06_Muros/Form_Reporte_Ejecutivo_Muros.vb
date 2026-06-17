Imports ClosedXML.Excel

Public Class Form_Reporte_Ejecutivo_Muros
    Inherits Form

    ' ── Propiedad pública ──────────────────────────────────────────────────
    Public Property Muros As List(Of Muro)

    ' ── Paleta de colores ──────────────────────────────────────────────────
    Private ReadOnly ClEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ClOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ClOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ClMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ClMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ClAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ClAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    Private ReadOnly XlClEnc As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlClOK As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlClOKTxt As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlClMal As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlClMalTxt As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlClAlerta As XLColor = XLColor.FromHtml("#FFEB9C")
    Private ReadOnly XlClAlertaTxt As XLColor = XLColor.FromHtml("#9C5700")

    ' ── Controles ─────────────────────────────────────────────────────────
    Private WithEvents Dgv As New DataGridView()
    Private WithEvents ChkSoloBad As New CheckBox()
    Private WithEvents BtnExport As New Button()
    Private _lblConteo As New Label()

    ' Datos precalculados por muro
    Private _filas As New List(Of FilaMuro)

    ' ── Constructor ───────────────────────────────────────────────────────
    Public Sub New()
        Me.Text = "Resumen Ejecutivo — Muros Estructurales"
        Me.Size = New Size(1380, 680)
        Me.MinimumSize = New Size(1000, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9)
        BuildUI()
        AddHandler Me.Load, AddressOf OnLoad
    End Sub

    ' ── Construcción de interfaz ──────────────────────────────────────────
    Private Sub BuildUI()

        ' Header
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top, .Height = 46,
            .BackColor = ClEncabezado, .Padding = New Padding(12, 0, 12, 0)
        }
        Dim lblTitle As New Label With {
            .Text = "RESUMEN EJECUTIVO — MUROS ESTRUCTURALES",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .AutoSize = True, .Location = New Point(12, 12)
        }
        pnlHeader.Controls.Add(lblTitle)
        Me.Controls.Add(pnlHeader)

        ' Toolbar
        Dim pnlTools As New Panel With {
            .Dock = DockStyle.Top, .Height = 40,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(8, 0, 8, 0)
        }

        ChkSoloBad.Text = "Mostrar solo muros con observaciones"
        ChkSoloBad.AutoSize = True
        ChkSoloBad.Location = New Point(10, 11)

        BtnExport.Text = "Exportar a Excel"
        BtnExport.Size = New Size(130, 26)
        BtnExport.Location = New Point(290, 7)
        BtnExport.BackColor = Color.FromArgb(33, 115, 70)
        BtnExport.ForeColor = Color.White
        BtnExport.FlatStyle = FlatStyle.Flat
        BtnExport.Font = New Font("Segoe UI", 9, FontStyle.Bold)

        _lblConteo.AutoSize = True
        _lblConteo.Location = New Point(432, 13)
        _lblConteo.ForeColor = Color.FromArgb(90, 90, 90)

        pnlTools.Controls.AddRange({ChkSoloBad, BtnExport, _lblConteo})
        Me.Controls.Add(pnlTools)

        ' DataGridView
        Dgv.Dock = DockStyle.Fill
        Dgv.ReadOnly = True
        Dgv.AllowUserToAddRows = False
        Dgv.AllowUserToResizeRows = False
        Dgv.RowHeadersVisible = False
        Dgv.BorderStyle = BorderStyle.None
        Dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        Dgv.BackgroundColor = Color.White
        Dgv.GridColor = Color.FromArgb(220, 220, 220)
        Dgv.EnableHeadersVisualStyles = False
        Dgv.ColumnHeadersHeight = 32
        Dgv.RowTemplate.Height = 24

        With Dgv.ColumnHeadersDefaultCellStyle
            .BackColor = ClEncabezado
            .ForeColor = Color.White
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
        With Dgv.DefaultCellStyle
            .Font = New Font("Segoe UI", 9)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With

        AgregarColumnas()
        Me.Controls.Add(Dgv)
    End Sub

    Private Sub AgregarColumnas()
        Dim defs As (nombre As String, ancho As Integer, alin As DataGridViewContentAlignment)() = {
            ("Muro", 90, DataGridViewContentAlignment.MiddleLeft),
            ("Dir.", 50, DataGridViewContentAlignment.MiddleCenter),
            ("Lw (m)", 72, DataGridViewContentAlignment.MiddleCenter),
            ("tw (m)", 65, DataGridViewContentAlignment.MiddleCenter),
            ("Hw (m)", 68, DataGridViewContentAlignment.MiddleCenter),
            ("ALR grav.", 80, DataGridViewContentAlignment.MiddleCenter),
            ("ALR diseño", 85, DataGridViewContentAlignment.MiddleCenter),
            ("F Flex mín", 90, DataGridViewContentAlignment.MiddleCenter),
            ("Piso flex.", 95, DataGridViewContentAlignment.MiddleCenter),
            ("F Cort. mín", 92, DataGridViewContentAlignment.MiddleCenter),
            ("Piso cort.", 95, DataGridViewContentAlignment.MiddleCenter),
            ("EB Izquierdo", 108, DataGridViewContentAlignment.MiddleCenter),
            ("EB Derecho", 108, DataGridViewContentAlignment.MiddleCenter),
            ("Vs/Vn (%)", 82, DataGridViewContentAlignment.MiddleCenter),
            ("Estado", 95, DataGridViewContentAlignment.MiddleCenter)
        }
        For Each d In defs
            Dgv.Columns.Add(New DataGridViewTextBoxColumn With {
                .HeaderText = d.nombre,
                .Width = d.ancho,
                .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = d.alin},
                .SortMode = DataGridViewColumnSortMode.NotSortable
            })
        Next
    End Sub

    ' ── Carga ─────────────────────────────────────────────────────────────
    Private Sub OnLoad(sender As Object, e As EventArgs)
        If Muros Is Nothing OrElse Muros.Count = 0 Then Return
        _filas = Muros.Select(AddressOf CalcularFila).ToList()
        Mostrar(_filas)
    End Sub

    ' ── Cálculo por muro ──────────────────────────────────────────────────
    Private Function CalcularFila(m As Muro) As FilaMuro
        Dim f As New FilaMuro With {
            .Label = m.Label,
            .Direccion = m.Direccion.ToString(),
            .Lw = m.Lw,
            .tw = m.tw,
            .Hw = m.Hw,
            .ALR_G = m.ALR_G,
            .ALR_D = m.ALR_D,
            .PorcVs = m.Porc_Vs
        }

        ' Flexión: peor factor entre todas las secciones
        Dim fFlexMin As Single = Single.MaxValue
        Dim pisoCriticoFlex As String = "—"
        For Each sec In m.Lista_Secciones
            Dim ff As Single = Math.Min(sec.Factor_Demanda_Flexo_Top, sec.Factor_Demanda_Flexo_Bot)
            If ff < 99 AndAlso ff < fFlexMin Then
                fFlexMin = ff
                pisoCriticoFlex = sec.Piso
            End If
        Next
        f.FFlexMin = If(fFlexMin = Single.MaxValue, -1, fFlexMin)
        f.PisoCriticoFlex = pisoCriticoFlex

        ' Cortante: peor factor entre todas las secciones
        Dim fCortMin As Single = Single.MaxValue
        Dim pisoCriticoCort As String = "—"
        For Each sec In m.Lista_Secciones
            If sec.F_Cortante > 0 AndAlso sec.F_Cortante < fCortMin Then
                fCortMin = sec.F_Cortante
                pisoCriticoCort = sec.Piso
            End If
        Next
        f.FCortMin = If(fCortMin = Single.MaxValue, -1, fCortMin)
        f.PisoCriticoCort = pisoCriticoCort

        ' Elementos de Borde: peor condición en toda la altura
        f.EBIzq = PeorEB(m, esIzquierdo:=True)
        f.EBDer = PeorEB(m, esIzquierdo:=False)

        ' Estado global
        Dim cumpleFlex = f.FFlexMin < 0 OrElse f.FFlexMin >= 0.9F
        Dim cumpleCort = f.FCortMin < 0 OrElse f.FCortMin >= 0.9F
        Dim ebOK = (f.EBIzq = "No Requiere") AndAlso (f.EBDer = "No Requiere")
        f.Cumple = cumpleFlex AndAlso cumpleCort AndAlso ebOK
        Return f
    End Function

    Private Function PeorEB(m As Muro, esIzquierdo As Boolean) As String
        Dim reqEsp As Boolean = False
        Dim reqNoEsp As Boolean = False
        For Each sec In m.Lista_Secciones
            If esIzquierdo Then
                reqEsp = reqEsp OrElse sec.Req_EB_I_Top_Esp OrElse sec.Req_EB_I_Bot_Esp
                reqNoEsp = reqNoEsp OrElse sec.Req_EB_I_Top_NoEsp OrElse sec.Req_EB_I_Bot_NoEsp
            Else
                reqEsp = reqEsp OrElse sec.Req_EB_D_Top_Esp OrElse sec.Req_EB_D_Bot_Esp
                reqNoEsp = reqNoEsp OrElse sec.Req_EB_D_Top_NoEsp OrElse sec.Req_EB_D_Bot_NoEsp
            End If
        Next
        Return If(reqEsp, "Especializado", If(reqNoEsp, "No Especial", "No Requiere"))
    End Function

    ' ── Visualización ─────────────────────────────────────────────────────
    Private Sub Mostrar(filas As List(Of FilaMuro))
        Dgv.Rows.Clear()
        Dim total As Integer = filas.Count
        Dim conObs As Integer = filas.Where(Function(x) Not x.Cumple).Count()

        For i As Integer = 0 To filas.Count - 1
            Dim f = filas(i)
            If ChkSoloBad.Checked AndAlso f.Cumple Then Continue For

            Dim r = Dgv.Rows(Dgv.Rows.Add())
            r.DefaultCellStyle.BackColor = If(i Mod 2 = 1, Color.FromArgb(248, 248, 248), Color.White)

            r.Cells(0).Value = f.Label
            r.Cells(1).Value = f.Direccion
            r.Cells(2).Value = Math.Round(f.Lw, 2)
            r.Cells(3).Value = Math.Round(f.tw, 3)
            r.Cells(4).Value = Math.Round(f.Hw, 2)
            r.Cells(5).Value = If(f.ALR_G > 0, Math.Round(f.ALR_G, 3).ToString("F3"), "—")
            r.Cells(6).Value = If(f.ALR_D > 0, Math.Round(f.ALR_D, 3).ToString("F3"), "—")
            r.Cells(7).Value = If(f.FFlexMin < 0, "Sin cálculo", Math.Round(f.FFlexMin, 2).ToString("F2"))
            r.Cells(8).Value = f.PisoCriticoFlex
            r.Cells(9).Value = If(f.FCortMin < 0, "Sin cálculo", Math.Round(f.FCortMin, 2).ToString("F2"))
            r.Cells(10).Value = f.PisoCriticoCort
            r.Cells(11).Value = f.EBIzq
            r.Cells(12).Value = f.EBDer
            r.Cells(13).Value = If(f.PorcVs > 0, Math.Round(f.PorcVs, 1).ToString("F1") & "%", "—")
            r.Cells(14).Value = If(f.Cumple, "✓  OK", "✗  Revisar")

            AplicarColorFactor(r.Cells(7), f.FFlexMin)
            AplicarColorFactor(r.Cells(9), f.FCortMin)
            AplicarColorALR(r.Cells(6), f.ALR_D)
            AplicarColorEB(r.Cells(11), f.EBIzq)
            AplicarColorEB(r.Cells(12), f.EBDer)
            AplicarColorEstado(r.Cells(14), f.Cumple)
        Next

        _lblConteo.Text = $"{total} muros totales  |  {conObs} con observaciones  |  {total - conObs} OK"
    End Sub

    Private Sub AplicarColorFactor(cell As DataGridViewCell, factor As Single)
        If factor < 0 Then
            cell.Style.BackColor = Color.FromArgb(238, 238, 238)
            cell.Style.ForeColor = Color.Gray
        ElseIf factor >= 0.9F Then
            cell.Style.BackColor = ClOK : cell.Style.ForeColor = ClOKTexto
        Else
            cell.Style.BackColor = ClMal : cell.Style.ForeColor = ClMalTexto
            cell.Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        End If
    End Sub

    Private Sub AplicarColorALR(cell As DataGridViewCell, alr As Single)
        If alr <= 0 Then Return
        If alr <= 0.35F Then
            cell.Style.BackColor = ClOK : cell.Style.ForeColor = ClOKTexto
        ElseIf alr <= 0.40F Then
            cell.Style.BackColor = ClAlerta : cell.Style.ForeColor = ClAlertaTexto
        Else
            cell.Style.BackColor = ClMal : cell.Style.ForeColor = ClMalTexto
        End If
    End Sub

    Private Sub AplicarColorEB(cell As DataGridViewCell, status As String)
        Select Case status
            Case "No Requiere"
                cell.Style.BackColor = ClOK : cell.Style.ForeColor = ClOKTexto
            Case "No Especial"
                cell.Style.BackColor = ClAlerta : cell.Style.ForeColor = ClAlertaTexto
            Case "Especializado"
                cell.Style.BackColor = ClMal : cell.Style.ForeColor = ClMalTexto
                cell.Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        End Select
    End Sub

    Private Sub AplicarColorEstado(cell As DataGridViewCell, cumple As Boolean)
        cell.Style.BackColor = If(cumple, ClOK, ClMal)
        cell.Style.ForeColor = If(cumple, ClOKTexto, ClMalTexto)
        cell.Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
    End Sub

    ' ── Eventos ───────────────────────────────────────────────────────────
    Private Sub ChkSoloBad_CheckedChanged(sender As Object, e As EventArgs) Handles ChkSoloBad.CheckedChanged
        If _filas IsNot Nothing Then Mostrar(_filas)
    End Sub

    Private Sub BtnExport_Click(sender As Object, e As EventArgs) Handles BtnExport.Click
        Dim dlg As New SaveFileDialog With {
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = "Resumen_Ejecutivo_Muros.xlsx"
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return
        Try
            Using wb As New XLWorkbook()
                ExportarHoja(wb.Worksheets.Add("Resumen Ejecutivo"))
                wb.SaveAs(dlg.FileName)
            End Using
            MessageBox.Show("Archivo exportado correctamente.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ── Exportación Excel ─────────────────────────────────────────────────
    Private Sub ExportarHoja(ws As IXLWorksheet)
        Dim enc = {"Muro", "Dir.", "Lw (m)", "tw (m)", "Hw (m)", "ALR grav.", "ALR diseño",
                   "F Flex mín", "Piso flex.", "F Cort. mín", "Piso cort.",
                   "EB Izquierdo", "EB Derecho", "Vs/Vn (%)", "Estado"}

        For j = 0 To enc.Length - 1
            Dim c = ws.Cell(1, j + 1)
            c.Value = enc(j)
            c.Style.Fill.BackgroundColor = XlClEnc
            c.Style.Font.FontColor = XLColor.White
            c.Style.Font.Bold = True
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin
            c.Style.Border.OutsideBorderColor = XLColor.White
        Next

        Dim fila = 2
        For Each f In _filas
            ws.Cell(fila, 1).Value = f.Label
            ws.Cell(fila, 2).Value = f.Direccion
            ws.Cell(fila, 3).Value = Math.Round(f.Lw, 2)
            ws.Cell(fila, 4).Value = Math.Round(f.tw, 3)
            ws.Cell(fila, 5).Value = Math.Round(f.Hw, 2)
            ws.Cell(fila, 6).Value = If(f.ALR_G > 0, CDbl(Math.Round(f.ALR_G, 3)), "-")
            ws.Cell(fila, 7).Value = If(f.ALR_D > 0, CDbl(Math.Round(f.ALR_D, 3)), "-")
            ws.Cell(fila, 8).Value = If(f.FFlexMin < 0, "Sin cálculo", CDbl(Math.Round(f.FFlexMin, 2)))
            ws.Cell(fila, 9).Value = f.PisoCriticoFlex
            ws.Cell(fila, 10).Value = If(f.FCortMin < 0, "Sin cálculo", CDbl(Math.Round(f.FCortMin, 2)))
            ws.Cell(fila, 11).Value = f.PisoCriticoCort
            ws.Cell(fila, 12).Value = f.EBIzq
            ws.Cell(fila, 13).Value = f.EBDer
            ws.Cell(fila, 14).Value = If(f.PorcVs > 0, CDbl(Math.Round(f.PorcVs, 1)), "-")
            ws.Cell(fila, 15).Value = If(f.Cumple, "OK", "Revisar")

            EscribirFactorXL(ws.Cell(fila, 8), f.FFlexMin)
            EscribirFactorXL(ws.Cell(fila, 10), f.FCortMin)
            EscribirALRXL(ws.Cell(fila, 7), f.ALR_D)
            EscribirEBXL(ws.Cell(fila, 12), f.EBIzq)
            EscribirEBXL(ws.Cell(fila, 13), f.EBDer)
            EscribirEstadoXL(ws.Cell(fila, 15), f.Cumple)

            If fila Mod 2 = 0 Then
                ws.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F8F8")
            End If
            fila += 1
        Next

        ws.Columns().AdjustToContents()
        ws.Column(1).Width = 14
        ws.Column(9).Width = 16
        ws.Column(11).Width = 16
        ws.Column(12).Width = 16
        ws.Column(13).Width = 16
        ws.SheetView.FreezeRows(1)
    End Sub

    Private Sub EscribirFactorXL(cell As IXLCell, factor As Single)
        If factor < 0 Then
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE")
        ElseIf factor >= 0.9F Then
            cell.Style.Fill.BackgroundColor = XlClOK
            cell.Style.Font.FontColor = XlClOKTxt
        Else
            cell.Style.Fill.BackgroundColor = XlClMal
            cell.Style.Font.FontColor = XlClMalTxt
            cell.Style.Font.Bold = True
        End If
    End Sub

    Private Sub EscribirALRXL(cell As IXLCell, alr As Single)
        If alr <= 0 Then Return
        If alr <= 0.35F Then
            cell.Style.Fill.BackgroundColor = XlClOK
            cell.Style.Font.FontColor = XlClOKTxt
        ElseIf alr <= 0.40F Then
            cell.Style.Fill.BackgroundColor = XlClAlerta
            cell.Style.Font.FontColor = XlClAlertaTxt
        Else
            cell.Style.Fill.BackgroundColor = XlClMal
            cell.Style.Font.FontColor = XlClMalTxt
        End If
    End Sub

    Private Sub EscribirEBXL(cell As IXLCell, status As String)
        Select Case status
            Case "No Requiere"
                cell.Style.Fill.BackgroundColor = XlClOK
                cell.Style.Font.FontColor = XlClOKTxt
            Case "No Especial"
                cell.Style.Fill.BackgroundColor = XlClAlerta
                cell.Style.Font.FontColor = XlClAlertaTxt
            Case "Especializado"
                cell.Style.Fill.BackgroundColor = XlClMal
                cell.Style.Font.FontColor = XlClMalTxt
                cell.Style.Font.Bold = True
        End Select
    End Sub

    Private Sub EscribirEstadoXL(cell As IXLCell, cumple As Boolean)
        cell.Style.Fill.BackgroundColor = If(cumple, XlClOK, XlClMal)
        cell.Style.Font.FontColor = If(cumple, XlClOKTxt, XlClMalTxt)
        cell.Style.Font.Bold = True
    End Sub

    ' ── Clase de datos por muro ───────────────────────────────────────────
    Private Class FilaMuro
        Public Property Label As String
        Public Property Direccion As String
        Public Property Lw As Single
        Public Property tw As Single
        Public Property Hw As Single
        Public Property ALR_G As Single
        Public Property ALR_D As Single
        Public Property FFlexMin As Single       ' -1 = sin cálculo
        Public Property PisoCriticoFlex As String
        Public Property FCortMin As Single        ' -1 = sin cálculo
        Public Property PisoCriticoCort As String
        Public Property EBIzq As String
        Public Property EBDer As String
        Public Property PorcVs As Single
        Public Property Cumple As Boolean
    End Class

End Class
