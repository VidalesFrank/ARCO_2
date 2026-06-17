Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class Form_SeccionMuroViewer
    Inherits Form

    Public Property Seccion As SeccionMuro
    Public Property NombreMuro As String = ""
    Public Property NombrePiso As String = ""
    Public Property MostrarTop As Boolean = True

    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorHormigon As Color = Color.FromArgb(200, 200, 195)
    Private ReadOnly ColorEB As Color = Color.FromArgb(150, 190, 230)
    Private ReadOnly ColorBarra As Color = Color.FromArgb(60, 60, 60)
    Private ReadOnly ColorMalla As Color = Color.FromArgb(100, 160, 100)
    Private ReadOnly ColorTexto As Color = Color.FromArgb(40, 40, 40)
    Private ReadOnly ColorCota As Color = Color.FromArgb(80, 80, 80)

    Private _canvas As PictureBox
    Private _radioTop As RadioButton
    Private _radioBot As RadioButton
    Private _lblInfo As Label

    Public Sub New()
        Me.Text = "Sección Transversal — Muro"
        Me.Size = New Size(900, 680)
        Me.MinimumSize = New Size(600, 450)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' ── Panel superior ────────────────────────────────────────────────────
        Dim panelTop As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 50,
            .BackColor = ColorEncabezado,
            .Padding = New Padding(10, 8, 10, 8)
        }

        _lblInfo = New Label() With {
            .AutoSize = True,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(10, 12)
        }
        panelTop.Controls.Add(_lblInfo)

        _radioTop = New RadioButton() With {
            .Text = "Top",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Checked = True,
            .Location = New Point(500, 14),
            .AutoSize = True
        }
        _radioBot = New RadioButton() With {
            .Text = "Bot",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(560, 14),
            .AutoSize = True
        }
        AddHandler _radioTop.CheckedChanged, AddressOf Radio_Changed
        AddHandler _radioBot.CheckedChanged, AddressOf Radio_Changed
        panelTop.Controls.Add(_radioTop)
        panelTop.Controls.Add(_radioBot)
        Me.Controls.Add(panelTop)

        ' ── Leyenda inferior ─────────────────────────────────────────────────
        Dim panelBot As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 44,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 5, 10, 5)
        }
        Dim leyenda = CrearPanelLeyenda()
        leyenda.Dock = DockStyle.Fill
        panelBot.Controls.Add(leyenda)
        Me.Controls.Add(panelBot)

        ' ── PictureBox de dibujo ─────────────────────────────────────────────
        _canvas = New PictureBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .SizeMode = PictureBoxSizeMode.Normal
        }
        AddHandler _canvas.Paint, AddressOf Canvas_Paint
        AddHandler _canvas.Resize, Sub(s, ev) _canvas.Invalidate()
        Me.Controls.Add(_canvas)

        AddHandler Me.Load, AddressOf Form_Load

    End Sub

    Private Function CrearPanelLeyenda() As FlowLayoutPanel
        Dim panel As New FlowLayoutPanel() With {
            .FlowDirection = FlowDirection.LeftToRight,
            .AutoSize = True,
            .WrapContents = False
        }
        panel.Controls.Add(ElementoLeyenda(ColorEB, "Elemento de borde"))
        panel.Controls.Add(ElementoLeyenda(ColorBarra, "Barras longitudinales"))
        panel.Controls.Add(ElementoLeyenda(ColorMalla, "Malla/refuerzo horizontal"))
        Return panel
    End Function

    Private Function ElementoLeyenda(color As Color, texto As String) As Panel
        Dim p As New Panel() With {.Width = 170, .Height = 26}
        Dim box As New Panel() With {
            .BackColor = color,
            .Size = New Size(18, 18),
            .Location = New Point(2, 4),
            .BorderStyle = BorderStyle.FixedSingle
        }
        Dim lbl As New Label() With {
            .Text = texto,
            .AutoSize = True,
            .Location = New Point(24, 5),
            .Font = New Font("Segoe UI", 9)
        }
        p.Controls.Add(box)
        p.Controls.Add(lbl)
        Return p
    End Function

    Private Sub Form_Load(sender As Object, e As EventArgs)
        _lblInfo.Text = $"Muro: {NombreMuro}   |   Piso: {NombrePiso}"
        _canvas.Invalidate()
    End Sub

    Private Sub Radio_Changed(sender As Object, e As EventArgs)
        MostrarTop = _radioTop.Checked
        _canvas.Invalidate()
    End Sub

    Private Sub Canvas_Paint(sender As Object, e As PaintEventArgs)

        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Color.White)

        If Seccion Is Nothing Then Return

        Dim W = _canvas.Width
        Dim H = _canvas.Height

        Dim tw = Seccion.tw_Planos   ' m
        Dim Lw = Seccion.Lw_Planos   ' m

        If tw <= 0 OrElse Lw <= 0 Then Return

        ' ── Escala y margen ───────────────────────────────────────────────────
        Dim margen As Integer = 70
        Dim disponibleW = W - 2 * margen
        Dim disponibleH = H - 2 * margen

        ' Escala para que quepa en el canvas (orientado Lw horizontal, tw vertical)
        Dim escala = Math.Min(disponibleW / Lw, disponibleH / tw)

        Dim drawW = CSng(Lw * escala)
        Dim drawH = CSng(tw * escala)

        Dim x0 = CSng((W - drawW) / 2)
        Dim y0 = CSng((H - drawH) / 2)

        ' ── Achurado de hormigón ──────────────────────────────────────────────
        Dim hatchBrush As New HatchBrush(HatchStyle.LightUpwardDiagonal,
                                         Color.FromArgb(160, 160, 155),
                                         ColorHormigon)
        g.FillRectangle(hatchBrush, x0, y0, drawW, drawH)
        g.DrawRectangle(New Pen(Color.FromArgb(80, 80, 80), 2), x0, y0, drawW, drawH)
        hatchBrush.Dispose()

        ' ── Elementos de borde ────────────────────────────────────────────────
        Dim leb_i = If(Seccion.EB_I_Top IsNot Nothing, Seccion.EB_I_Top.L_EB, 0.0F)
        Dim leb_d = If(Seccion.EB_D_Top IsNot Nothing, Seccion.EB_D_Top.L_EB, 0.0F)

        If leb_i > 0 Then
            Dim ebW_i = CSng(leb_i * escala)
            g.FillRectangle(New SolidBrush(Color.FromArgb(160, ColorEB)), x0, y0, ebW_i, drawH)
            g.DrawRectangle(New Pen(Color.FromArgb(60, 110, 170), 1.5F), x0, y0, ebW_i, drawH)
        End If
        If leb_d > 0 Then
            Dim ebW_d = CSng(leb_d * escala)
            g.FillRectangle(New SolidBrush(Color.FromArgb(160, ColorEB)), x0 + drawW - ebW_d, y0, ebW_d, drawH)
            g.DrawRectangle(New Pen(Color.FromArgb(60, 110, 170), 1.5F), x0 + drawW - ebW_d, y0, ebW_d, drawH)
        End If

        ' ── Barras longitudinales ─────────────────────────────────────────────
        Dim listaBarras = If(MostrarTop,
                             Seccion.ListaRefuerzoCompleto_Top,
                             Seccion.ListaRefuerzoCompleto_Bot)

        If listaBarras IsNot Nothing Then
            For Each barra In listaBarras
                Dim px = x0 + drawW / 2 + CSng(barra.Coordenada_Y * escala)  ' Coordenada_Y es a lo largo de Lw
                Dim py = y0 + drawH / 2 - CSng(barra.Coordenada_X * escala)  ' Coordenada_X es a lo largo de tw
                Dim radio = Math.Max(CSng(barra.Db / 2000 * escala), 3)

                g.FillEllipse(New SolidBrush(ColorBarra), px - radio, py - radio, radio * 2, radio * 2)
                g.DrawEllipse(New Pen(Color.White, 0.5F), px - radio, py - radio, radio * 2, radio * 2)
            Next
        End If

        ' ── Cotas ─────────────────────────────────────────────────────────────
        DibujarCota(g, x0, y0 - 28, x0 + drawW, y0 - 28,
                    $"Lw = {Math.Round(Lw, 2)} m", escala)
        DibujarCotaVertical(g, x0 - 40, y0, x0 - 40, y0 + drawH,
                            $"tw = {Math.Round(tw, 3)} m")

        ' ── Etiqueta de cuantía ───────────────────────────────────────────────
        Dim cuantia = If(MostrarTop, Seccion.Cuantia_Top_Col, Seccion.Cuantia_Bot_Col)
        Dim cuantiaReq = If(MostrarTop, Seccion.Cuantia_Top_Req, Seccion.Cuantia_Bot_Req)
        Dim asCol = If(MostrarTop, Seccion.AsT_Top_Col, Seccion.AsT_Bot_Col)
        Dim asReq = If(MostrarTop, Seccion.As_Top_Req, Seccion.As_Bot_Req)

        Dim fontInfo = New Font("Segoe UI", 9)
        Dim yText = y0 + drawH + 10
        g.DrawString($"As colocado: {Math.Round(asCol, 0)} mm²  |  ρ = {Math.Round(cuantia, 3)}%",
                     fontInfo, New SolidBrush(ColorTexto), x0, yText)
        g.DrawString($"As requerido: {Math.Round(asReq, 0)} mm²  |  ρ_req = {Math.Round(cuantiaReq, 3)}%",
                     fontInfo, New SolidBrush(ColorTexto), x0, yText + 16)

        ' ── Eje neutro C ─────────────────────────────────────────────────────
        Dim c_val = If(MostrarTop,
                       Math.Max(Seccion.C_I_Top, Seccion.C_D_Top),
                       Math.Max(Seccion.C_I_Bot, Seccion.C_D_Bot))
        If c_val > 0 AndAlso c_val < Lw Then
            Dim xc = x0 + CSng(c_val * escala)
            Dim penC = New Pen(Color.FromArgb(200, 50, 50), 2) With {.DashStyle = DashStyle.Dash}
            g.DrawLine(penC, xc, y0, xc, y0 + drawH)
            g.DrawString($"C = {Math.Round(c_val, 3)} m", New Font("Segoe UI", 8),
                         New SolidBrush(Color.FromArgb(180, 40, 40)), xc + 3, y0 + 3)
        End If

    End Sub

    Private Sub DibujarCota(g As Graphics, x1 As Single, y As Single, x2 As Single, y2 As Single,
                             texto As String, escala As Single)
        Dim pen = New Pen(ColorCota, 1)
        g.DrawLine(pen, x1, y, x2, y2)
        g.DrawLine(pen, x1, y - 5, x1, y + 5)
        g.DrawLine(pen, x2, y - 5, x2, y + 5)
        Dim font = New Font("Segoe UI", 9)
        Dim sz = g.MeasureString(texto, font)
        g.DrawString(texto, font, New SolidBrush(ColorCota),
                     x1 + (x2 - x1) / 2 - sz.Width / 2, y - sz.Height - 2)
    End Sub

    Private Sub DibujarCotaVertical(g As Graphics, x As Single, y1 As Single, x2 As Single, y2 As Single,
                                     texto As String)
        Dim pen = New Pen(ColorCota, 1)
        g.DrawLine(pen, x, y1, x2, y2)
        g.DrawLine(pen, x - 5, y1, x + 5, y1)
        g.DrawLine(pen, x - 5, y2, x + 5, y2)
        Dim font = New Font("Segoe UI", 9)
        Dim sz = g.MeasureString(texto, font)
        Dim state = g.Save()
        g.TranslateTransform(x - sz.Height - 4, y1 + (y2 - y1) / 2 + sz.Width / 2)
        g.RotateTransform(-90)
        g.DrawString(texto, font, New SolidBrush(ColorCota), 0, 0)
        g.Restore(state)
    End Sub

End Class
