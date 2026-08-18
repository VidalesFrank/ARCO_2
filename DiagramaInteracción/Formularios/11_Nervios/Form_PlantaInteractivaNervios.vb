Imports System.Drawing
Imports System.Drawing.Drawing2D

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_PlantaInteractivaNervios — visualizador ampliado con pan/zoom/hover del
'  plano de nervios. Mismo patrón que Form_PlantaInteractiva (Vigas): rueda para
'  zoom, arrastre para desplazar, hover con tooltip, ajustar vista. No incluye
'  modo de agrupación manual (Nervios usa Form_11_SepararNervio para eso).
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_PlantaInteractivaNervios
    Inherits Form

    ' ── Datos del modelo ──────────────────────────────────────
    Public Property Nervios As List(Of cNervio)
    Public Property Joints As Dictionary(Of String, cJoint)
    Public Property GridLines As List(Of cGridLine)
    Public Property NervioSeleccionado As cNervio
    Public Property PisoActual As String

    ' ── Estado de la vista ────────────────────────────────────
    Private _zoom As Double = 50.0
    Private _panX As Double = 0.0
    Private _panY As Double = 0.0

    ' ── Interacción ───────────────────────────────────────────
    Private _isDragging As Boolean = False
    Private _lastMouse As Point
    Private _mousePos As Point
    Private _hoveredNervio As cNervio = Nothing
    Private _hoveredFrame As cFrameNervio = Nothing

    ' ── Controles ─────────────────────────────────────────────
    Private WithEvents _panel As BufferedPanel
    Private _lblInfo As Label
    Private _btnFit As Button
    Private _chkAllFloors As CheckBox
    Private _lblZoom As Label

    ' ── Paleta categórica validada (misma que la planta inline) ──
    Private Shared ReadOnly ColoresNervio As Color() = {
        Color.FromArgb(42, 120, 214),   ' azul
        Color.FromArgb(235, 104, 52),   ' naranja
        Color.FromArgb(27, 175, 122),   ' aqua
        Color.FromArgb(237, 161, 0),    ' amarillo
        Color.FromArgb(232, 123, 164),  ' magenta
        Color.FromArgb(0, 131, 0),      ' verde
        Color.FromArgb(74, 58, 167),    ' violeta
        Color.FromArgb(227, 73, 72)     ' rojo
    }

    ' =====================================================================
    Public Sub New()
        Me.Text = "Vista Interactiva — Planta de Nervios"
        Me.Size = New Size(1100, 750)
        Me.MinimumSize = New Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(87, 87, 87)
        Me.Font = New Font("Segoe UI", 9)

        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        BuildUI()
    End Sub

    ' =====================================================================
    ' CONSTRUCCIÓN DE LA INTERFAZ
    ' =====================================================================
    Private Sub BuildUI()

        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 40,
            .BackColor = Color.FromArgb(87, 87, 87),
            .Padding = New Padding(4, 0, 4, 0)
        }

        _lblInfo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(230, 230, 230),
            .Font = New Font("Segoe UI", 9),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 0, 0),
            .Text = "Pasa el cursor sobre un tramo para ver su información."
        }

        _lblZoom = New Label With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .ForeColor = Color.FromArgb(190, 220, 190),
            .Font = New Font("Segoe UI", 8),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Text = "Zoom: --"
        }

        _chkAllFloors = New CheckBox With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .Text = "Todos los pisos",
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Checked = False
        }

        _btnFit = New Button With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .Text = "Ajustar vista",
            .BackColor = Color.FromArgb(224, 224, 224),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        _btnFit.FlatAppearance.BorderSize = 0

        pnlBottom.Controls.Add(_lblInfo)
        pnlBottom.Controls.Add(_lblZoom)
        pnlBottom.Controls.Add(_chkAllFloors)
        pnlBottom.Controls.Add(_btnFit)

        _panel = New BufferedPanel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(245, 248, 252),
            .Cursor = Cursors.Hand
        }

        Me.Controls.Add(_panel)
        Me.Controls.Add(pnlBottom)

        AddHandler _panel.Paint, AddressOf Panel_Paint
        AddHandler _panel.MouseWheel, AddressOf Panel_MouseWheel
        AddHandler _panel.MouseDown, AddressOf Panel_MouseDown
        AddHandler _panel.MouseMove, AddressOf Panel_MouseMove
        AddHandler _panel.MouseUp, AddressOf Panel_MouseUp
        AddHandler _panel.MouseEnter, Sub() _panel.Focus()
        AddHandler _panel.Resize, Sub() _panel.Invalidate()
        AddHandler _btnFit.Click, Sub() FitView()
        AddHandler _chkAllFloors.CheckedChanged, Sub() _panel.Invalidate()
        AddHandler Me.Shown, Sub() FitView()
    End Sub

    ' =====================================================================
    ' TRANSFORMADAS MUNDO ↔ PANTALLA
    ' =====================================================================
    Private Function W2S(wx As Double, wy As Double) As PointF
        Return New PointF(CSng(_panX + wx * _zoom), CSng(_panY - wy * _zoom))
    End Function

    Private Sub S2W(sx As Double, sy As Double, ByRef wx As Double, ByRef wy As Double)
        wx = (sx - _panX) / _zoom
        wy = (_panY - sy) / _zoom
    End Sub

    ' =====================================================================
    ' AJUSTAR VISTA AL MODELO
    ' =====================================================================
    Public Sub FitView()
        If Joints Is Nothing OrElse Joints.Count = 0 Then Return

        Dim xs = Joints.Values.Select(Function(j) j.GlobalX).ToList()
        Dim ys = Joints.Values.Select(Function(j) j.GlobalY).ToList()

        Dim minX = xs.Min() : Dim maxX = xs.Max()
        Dim minY = ys.Min() : Dim maxY = ys.Max()

        Dim worldW = Math.Max(maxX - minX, 0.1)
        Dim worldH = Math.Max(maxY - minY, 0.1)

        Dim marg As Double = 70
        _zoom = Math.Min((_panel.Width - 2 * marg) / worldW,
                          (_panel.Height - 2 * marg) / worldH)

        _panX = _panel.Width / 2.0 - (minX + maxX) / 2.0 * _zoom
        _panY = _panel.Height / 2.0 + (minY + maxY) / 2.0 * _zoom

        _lblZoom.Text = $"Zoom: {Math.Round(_zoom, 1)} px/m"
        _panel.Invalidate()
    End Sub

    ' =====================================================================
    ' EVENTOS DE RATÓN
    ' =====================================================================
    Private Sub Panel_MouseWheel(sender As Object, e As MouseEventArgs)
        Dim factor As Double = If(e.Delta > 0, 1.25, 0.8)

        Dim wx As Double = 0, wy As Double = 0
        S2W(e.X, e.Y, wx, wy)

        _zoom = Math.Max(1, Math.Min(20000, _zoom * factor))

        _panX = e.X - wx * _zoom
        _panY = e.Y + wy * _zoom

        _lblZoom.Text = $"Zoom: {Math.Round(_zoom, 1)} px/m"
        _panel.Invalidate()
    End Sub

    Private Sub Panel_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left OrElse e.Button = MouseButtons.Middle Then
            _isDragging = True
            _lastMouse = e.Location
            _panel.Cursor = Cursors.SizeAll
        End If
    End Sub

    Private Sub Panel_MouseMove(sender As Object, e As MouseEventArgs)
        _mousePos = e.Location
        If _isDragging Then
            _panX += e.X - _lastMouse.X
            _panY += e.Y - _lastMouse.Y
            _lastMouse = e.Location
            _panel.Invalidate()
        Else
            DetectarHover(e.X, e.Y)
        End If
    End Sub

    Private Sub Panel_MouseUp(sender As Object, e As MouseEventArgs)
        _isDragging = False
        _panel.Cursor = Cursors.Hand
    End Sub

    ' =====================================================================
    ' DETECCIÓN DE ELEMENTO BAJO EL CURSOR
    ' =====================================================================
    Private Sub DetectarHover(sx As Integer, sy As Integer)
        If Nervios Is Nothing OrElse Joints Is Nothing Then Return

        Dim wx As Double = 0, wy As Double = 0
        S2W(sx, sy, wx, wy)

        Dim umbral As Double = 8.0 / _zoom

        Dim mejorNervio As cNervio = Nothing
        Dim mejorFrame As cFrameNervio = Nothing
        Dim mejorDist As Double = Double.MaxValue

        For Each n In Nervios
            If Not MostrarNervio(n) Then Continue For
            For Each fn In n.Frames
                If Not Joints.ContainsKey(fn.JointI) OrElse Not Joints.ContainsKey(fn.JointJ) Then Continue For
                Dim ji = Joints(fn.JointI) : Dim jj = Joints(fn.JointJ)
                Dim d = DistASegmento(wx, wy, ji.GlobalX, ji.GlobalY, jj.GlobalX, jj.GlobalY)
                If d < mejorDist Then
                    mejorDist = d
                    mejorNervio = n
                    mejorFrame = fn
                End If
            Next
        Next

        If mejorDist < umbral Then
            _hoveredNervio = mejorNervio
            _hoveredFrame = mejorFrame
            MostrarInfoFrame(mejorNervio, mejorFrame)
        Else
            If _hoveredNervio IsNot Nothing Then
                _hoveredNervio = Nothing
                _hoveredFrame = Nothing
                _lblInfo.Text = "Pasa el cursor sobre un tramo para ver su información.   |   " &
                                $"Zoom: {Math.Round(_zoom, 1)} px/m"
                _panel.Invalidate()
            End If
        End If
    End Sub

    Private Function MostrarNervio(n As cNervio) As Boolean
        If _chkAllFloors.Checked Then Return True
        If String.IsNullOrEmpty(PisoActual) Then Return True
        Return n.Piso.Equals(PisoActual, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub MostrarInfoFrame(n As cNervio, fn As cFrameNervio)
        Dim partes As New List(Of String)
        partes.Add($"Nervio: {n.ToString()}  |  Piso: {n.Piso}  |  Tramo: {fn.ObjectLabel}")
        partes.Add($"Sección: {fn.NombreSeccion}   bw={Math.Round(fn.Bw * 100, 0)} cm  h={Math.Round(fn.H * 100, 0)} cm   L={Math.Round(fn.Longitud, 3)} m")
        _lblInfo.Text = String.Join("   ", partes)
        _panel.Invalidate()
    End Sub

    Private Function DistASegmento(px As Double, py As Double,
                                    x1 As Double, y1 As Double,
                                    x2 As Double, y2 As Double) As Double
        Dim dx = x2 - x1, dy = y2 - y1
        Dim lenSq = dx * dx + dy * dy
        If lenSq < 0.00001 Then Return Math.Sqrt((px - x1) ^ 2 + (py - y1) ^ 2)
        Dim t = Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lenSq))
        Return Math.Sqrt((px - x1 - t * dx) ^ 2 + (py - y1 - t * dy) ^ 2)
    End Function

    ' =====================================================================
    ' DIBUJO PRINCIPAL
    ' =====================================================================
    Private Sub Panel_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(Color.FromArgb(245, 248, 252))

        If Joints Is Nothing OrElse Joints.Count = 0 Then
            Using fMsg As New Font("Segoe UI", 12)
                g.DrawString("Sin datos. Importa y calcula primero.", fMsg, Brushes.Gray,
                             _panel.Width / 2 - 160, _panel.Height / 2 - 12)
            End Using
            Return
        End If

        DibujarGridLines(g)
        DibujarNervios(g)

        If _hoveredNervio IsNot Nothing AndAlso _hoveredFrame IsNot Nothing Then
            DibujarTooltipElemento(g, _hoveredNervio, _hoveredFrame)
        End If

        DibujarEjesCoord(g)
        DibujarEscala(g)
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Grid lines (mismo estilo que Form_PlantaInteractiva de Vigas)
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarGridLines(g As Graphics)
        If GridLines Is Nothing OrElse GridLines.Count = 0 Then Return

        Using penGrid As New Pen(Color.FromArgb(160, 190, 190, 200), 1)
            penGrid.DashStyle = DashStyle.Dash

            Dim wxMin As Double = 0, wyMin As Double = 0, wxMax As Double = 0, wyMax As Double = 0
            S2W(0, _panel.Height, wxMin, wyMin)
            S2W(_panel.Width, 0, wxMax, wyMax)
            Dim ext As Double = 3.0

            For Each gl In GridLines
                If Not gl.Visible Then Continue For

                Dim p1 As PointF, p2 As PointF
                If gl.Direction = "X" Then
                    p1 = W2S(gl.Ordinate, wyMin - ext)
                    p2 = W2S(gl.Ordinate, wyMax + ext)
                Else
                    p1 = W2S(wxMin - ext, gl.Ordinate)
                    p2 = W2S(wxMax + ext, gl.Ordinate)
                End If

                Try
                    g.DrawLine(penGrid, p1, p2)
                Catch ex As OverflowException
                    Continue For
                End Try

                If Not String.IsNullOrEmpty(gl.GridID) AndAlso _zoom > 5 Then
                    Dim labelPt As PointF
                    If gl.Direction = "X" Then
                        labelPt = New PointF(W2S(gl.Ordinate, 0).X - 8, 6)
                    Else
                        labelPt = New PointF(6, W2S(0, gl.Ordinate).Y - 10)
                    End If

                    Try
                        Using fGrid As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                            Dim sz = g.MeasureString(gl.GridID, fGrid)
                            Using bBg As New SolidBrush(Color.FromArgb(200, Color.White))
                                g.FillEllipse(bBg, labelPt.X - 2, labelPt.Y, sz.Width + 4, sz.Height)
                            End Using
                            g.DrawString(gl.GridID, fGrid, Brushes.SlateGray, labelPt.X, labelPt.Y)
                        End Using
                    Catch ex As OverflowException
                    End Try
                End If
            Next
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Nervios
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarNervios(g As Graphics)
        If Nervios Is Nothing OrElse Joints Is Nothing Then Return

        Using fontLabel As New Font("Segoe UI", 7.5F),
              fontEje As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              fontNombre As New Font("Segoe UI", 8, FontStyle.Bold)

            For idx As Integer = 0 To Nervios.Count - 1
                Dim n = Nervios(idx)
                If Not MostrarNervio(n) Then Continue For

                Dim esSelec = NervioSeleccionado IsNot Nothing AndAlso ReferenceEquals(n, NervioSeleccionado)
                Dim esHover = (n Is _hoveredNervio)

                Dim colorLinea As Color = ColoresNervio(idx Mod ColoresNervio.Length)
                Dim grosor As Single = If(esSelec, 4.5F, If(esHover, 3.5F, 2.2F))

                For Each fn In n.Frames
                    If Not Joints.ContainsKey(fn.JointI) OrElse Not Joints.ContainsKey(fn.JointJ) Then Continue For

                    Dim ji = Joints(fn.JointI), jj = Joints(fn.JointJ)
                    Dim p1 = W2S(ji.GlobalX, ji.GlobalY)
                    Dim p2 = W2S(jj.GlobalX, jj.GlobalY)

                    Try
                        If esSelec Then
                            Using penHalo As New Pen(Color.FromArgb(70, 227, 73, 72), grosor + 5)
                                g.DrawLine(penHalo, p1, p2)
                            End Using
                        ElseIf esHover Then
                            Using penHalo As New Pen(Color.FromArgb(60, 42, 120, 214), grosor + 4)
                                g.DrawLine(penHalo, p1, p2)
                            End Using
                        End If
                        Using pen As New Pen(colorLinea, grosor)
                            g.DrawLine(pen, p1, p2)
                        End Using
                    Catch ex As OverflowException
                        Continue For
                    End Try

                    If fn.Ref_Modificado Then
                        Dim cdMin = Math.Min(fn.CD_Flex_Inf_C, Math.Min(fn.CD_Cortante_I, fn.CD_Cortante_D))
                        Dim clrCD = If(cdMin >= 1.0, Color.FromArgb(0, 131, 0),
                                     If(cdMin >= 0.9, Color.FromArgb(237, 161, 0), Color.FromArgb(227, 73, 72)))
                        Dim xm = (p1.X + p2.X) / 2 : Dim ym = (p1.Y + p2.Y) / 2
                        Using penCD As New Pen(Color.White, 1.5F), brushCD As New SolidBrush(clrCD)
                            g.FillEllipse(brushCD, xm - 5, ym - 5, 10, 10)
                            g.DrawEllipse(penCD, xm - 5, ym - 5, 10, 10)
                        End Using
                    End If

                    If (esHover OrElse esSelec) AndAlso _zoom > 12 Then
                        Dim xm = (p1.X + p2.X) / 2 : Dim ym = (p1.Y + p2.Y) / 2
                        DibujarEtiquetaConFondo(g, fn.ObjectLabel, fontLabel, colorLinea, xm + 4, ym - 18)
                    End If

                    If _zoom > 8 Then
                        Using brApoyo As New SolidBrush(Color.FromArgb(235, 104, 52)), penApoyo As New Pen(Color.White, 1.2F)
                            If fn.B_Apoyo_I > 0 Then
                                g.FillEllipse(brApoyo, p1.X - 4, p1.Y - 4, 8, 8)
                                g.DrawEllipse(penApoyo, p1.X - 4, p1.Y - 4, 8, 8)
                            End If
                            If fn.B_Apoyo_D > 0 Then
                                g.FillEllipse(brApoyo, p2.X - 4, p2.Y - 4, 8, 8)
                                g.DrawEllipse(penApoyo, p2.X - 4, p2.Y - 4, 8, 8)
                            End If
                        End Using
                        If Not String.IsNullOrWhiteSpace(fn.EjeApoyo_I) Then
                            DibujarEtiquetaConFondo(g, fn.EjeApoyo_I, fontEje, Color.FromArgb(150, 90, 20), p1.X + 6, p1.Y + 4)
                        End If
                        If Not String.IsNullOrWhiteSpace(fn.EjeApoyo_D) Then
                            DibujarEtiquetaConFondo(g, fn.EjeApoyo_D, fontEje, Color.FromArgb(150, 90, 20), p2.X + 6, p2.Y + 4)
                        End If
                    End If
                Next

                If n.Frames.Count > 0 AndAlso _zoom > 6 Then
                    Dim fn0 = n.Frames(0)
                    Dim ji As cJoint = Nothing
                    If Joints.TryGetValue(fn0.JointI, ji) Then
                        DibujarEtiquetaConFondo(g, n.ToString(), fontNombre, colorLinea, W2S(ji.GlobalX, ji.GlobalY).X, W2S(ji.GlobalX, ji.GlobalY).Y - 16)
                    End If
                End If
            Next
        End Using
    End Sub

    Private Shared Sub DibujarEtiquetaConFondo(g As Graphics, texto As String, font As Font,
                                                colorTexto As Color, x As Single, y As Single)
        If String.IsNullOrEmpty(texto) Then Return
        Try
            Dim sz = g.MeasureString(texto, font)
            Using bBg As New SolidBrush(Color.FromArgb(215, Color.White))
                g.FillRectangle(bBg, x - 2, y - 1, sz.Width + 4, sz.Height + 2)
            End Using
            Using brTexto As New SolidBrush(colorTexto)
                g.DrawString(texto, font, brTexto, x, y)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Tooltip dibujado en canvas — aparece junto al cursor
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarTooltipElemento(g As Graphics, n As cNervio, fn As cFrameNervio)

        Dim lineas As New List(Of (texto As String, negrita As Boolean, color As Color))

        lineas.Add(($"Nervio: {n.ToString()}   —   Tramo: {fn.ObjectLabel}", True, Color.FromArgb(87, 87, 87)))
        lineas.Add(("", False, Color.Transparent))

        lineas.Add(($"Sección: {fn.NombreSeccion}", False, Color.Black))
        lineas.Add(($"bw = {Math.Round(fn.Bw * 100, 0)} cm     h = {Math.Round(fn.H * 100, 0)} cm     L = {Math.Round(fn.Longitud, 3)} m", False, Color.Black))
        lineas.Add(($"Eje Izq: {If(String.IsNullOrWhiteSpace(fn.EjeApoyo_I), "—", fn.EjeApoyo_I)}     Eje Der: {If(String.IsNullOrWhiteSpace(fn.EjeApoyo_D), "—", fn.EjeApoyo_D)}", False, Color.FromArgb(150, 90, 20)))
        lineas.Add(($"f'c = {fn.fc} MPa     fy = {fn.fy} MPa", False, Color.FromArgb(90, 90, 90)))

        If fn.Ref_Modificado Then
            lineas.Add(("", False, Color.Transparent))
            lineas.Add(("Flexión", True, Color.FromArgb(87, 87, 87)))

            Dim AgregarFlex = Sub(etiqueta As String, cd As Double)
                                   Dim ok = cd >= 0.9
                                   Dim clr = If(ok, Color.FromArgb(0, 110, 0), Color.FromArgb(180, 0, 0))
                                   Dim cdTxt = If(cd >= 99.0, "n/a", Math.Round(cd, 2).ToString())
                                   lineas.Add(($"  {etiqueta}   C/D = {cdTxt}{If(ok, " ✓", " ✗")}", False, clr))
                               End Sub
            AgregarFlex("Sup. Izq", fn.CD_Flex_Sup_I)
            AgregarFlex("Inf. Centro", fn.CD_Flex_Inf_C)
            AgregarFlex("Sup. Der", fn.CD_Flex_Sup_D)

            lineas.Add(("", False, Color.Transparent))
            lineas.Add(("Cortante", True, Color.FromArgb(87, 87, 87)))
            Dim AgregarCor = Sub(etiqueta As String, cd As Double)
                                  Dim ok = cd >= 0.9
                                  Dim clr = If(ok, Color.FromArgb(0, 110, 0), Color.FromArgb(180, 0, 0))
                                  Dim cdTxt = If(cd >= 99.0, "n/a", Math.Round(cd, 2).ToString())
                                  lineas.Add(($"  {etiqueta}   C/D = {cdTxt}{If(ok, " ✓", " ✗")}", False, clr))
                              End Sub
            AgregarCor("Izq", fn.CD_Cortante_I)
            AgregarCor("Der", fn.CD_Cortante_D)
        Else
            lineas.Add(("", False, Color.Transparent))
            lineas.Add(("Sin refuerzo asignado todavía.", False, Color.DimGray))
        End If

        Dim fntNormal As New Font("Segoe UI", 8.5F)
        Dim fntBold As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Dim lineH As Single = 17
        Dim pad As Single = 10

        Dim maxW As Single = 0
        For Each ln In lineas
            If String.IsNullOrEmpty(ln.texto) Then Continue For
            Dim fnt = If(ln.negrita, fntBold, fntNormal)
            Dim w = g.MeasureString(ln.texto, fnt).Width
            If w > maxW Then maxW = w
        Next

        Dim boxW As Single = maxW + pad * 2
        Dim boxH As Single = lineas.Count * lineH + pad * 2

        Dim ox As Single = _mousePos.X + 18
        Dim oy As Single = _mousePos.Y + 5
        If ox + boxW > _panel.Width - 5 Then ox = _mousePos.X - boxW - 10
        If oy + boxH > _panel.Height - 5 Then oy = _mousePos.Y - boxH - 5

        Using brushSombra As New SolidBrush(Color.FromArgb(40, 0, 0, 0))
            g.FillRectangle(brushSombra, ox + 4, oy + 4, boxW, boxH)
        End Using
        Using brushFondo As New SolidBrush(Color.FromArgb(252, 252, 252))
            g.FillRectangle(brushFondo, ox, oy, boxW, boxH)
        End Using
        Using penBorde As New Pen(Color.FromArgb(190, 190, 190), 1.2F)
            g.DrawRectangle(penBorde, ox, oy, boxW, boxH)
        End Using
        Using brushTop As New SolidBrush(Color.FromArgb(87, 87, 87))
            g.FillRectangle(brushTop, ox, oy, boxW, 3)
        End Using

        Dim y As Single = oy + pad
        For Each ln In lineas
            If String.IsNullOrEmpty(ln.texto) Then
                Using penSep As New Pen(Color.FromArgb(220, 220, 220), 1)
                    g.DrawLine(penSep, ox + 6, y + lineH / 2, ox + boxW - 6, y + lineH / 2)
                End Using
            Else
                Dim fnt = If(ln.negrita, fntBold, fntNormal)
                Using br As New SolidBrush(ln.color)
                    g.DrawString(ln.texto, fnt, br, ox + pad, y)
                End Using
            End If
            y += lineH
        Next

        fntNormal.Dispose()
        fntBold.Dispose()
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Ejes de coordenadas (esquina inf-izq)
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarEjesCoord(g As Graphics)
        Dim ox As Single = 45, oy As Single = _panel.Height - 45
        Dim len As Single = 30

        Using penX As New Pen(Color.FromArgb(227, 73, 72), 2)
            g.DrawLine(penX, ox, oy, ox + len, oy)
            g.DrawString("X", New Font("Segoe UI", 7.5F, FontStyle.Bold), New SolidBrush(Color.FromArgb(227, 73, 72)), ox + len + 2, oy - 7)
        End Using

        Using penY As New Pen(Color.FromArgb(0, 131, 0), 2)
            g.DrawLine(penY, ox, oy, ox, oy - len)
            g.DrawString("Y", New Font("Segoe UI", 7.5F, FontStyle.Bold), New SolidBrush(Color.FromArgb(0, 131, 0)), ox + 3, oy - len - 12)
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Barra de escala (esquina inf-der)
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarEscala(g As Graphics)
        Dim niceLen As Double = 1.0
        For Each candidato In {0.5, 1, 2, 5, 10, 20, 50}
            If candidato * _zoom >= 60 Then
                niceLen = candidato
                Exit For
            End If
        Next

        Dim pixLen As Single = CSng(niceLen * _zoom)
        Dim x0 As Single = _panel.Width - 20.0F - pixLen
        Dim y0 As Single = _panel.Height - 30

        Try
            Using penScale As New Pen(Color.FromArgb(90, 90, 90), 2)
                g.DrawLine(penScale, x0, y0, x0 + pixLen, y0)
                g.DrawLine(penScale, x0, y0 - 4, x0, y0 + 4)
                g.DrawLine(penScale, x0 + pixLen, y0 - 4, x0 + pixLen, y0 + 4)
            End Using
            Using fnt As New Font("Segoe UI", 7.5F)
                Dim lbl = $"{niceLen} m"
                Dim sz = g.MeasureString(lbl, fnt)
                g.DrawString(lbl, fnt, Brushes.DimGray, x0 + pixLen / 2 - sz.Width / 2, y0 - 17)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    ' =====================================================================
    ' PANEL CON DOBLE BUFFER (clase interna)
    ' =====================================================================
    Private Class BufferedPanel
        Inherits Panel
        Sub New()
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                        ControlStyles.UserPaint Or
                        ControlStyles.OptimizedDoubleBuffer, True)
        End Sub
    End Class

End Class
