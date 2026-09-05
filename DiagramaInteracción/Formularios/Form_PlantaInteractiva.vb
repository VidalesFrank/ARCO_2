Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports ARCO.eNumeradores

Public Class Form_PlantaInteractiva
    Inherits Form

    ' ── Datos del modelo ──────────────────────────────────────
    Public Property Vigas As List(Of cViga)
    Public Property Joints As Dictionary(Of String, cJoint)
    Public Property GridLines As List(Of cGridLine)
    Public Property VigaSeleccionada As cViga
    Public Property PisoActual As String

    ' ── Estado de la vista ────────────────────────────────────
    Private _zoom As Double = 50.0      ' píxeles por metro
    Private _panX As Double = 0.0       ' desplazamiento X en píxeles
    Private _panY As Double = 0.0       ' desplazamiento Y en píxeles

    ' ── Interacción ───────────────────────────────────────────
    Private _isDragging As Boolean = False
    Private _lastMouse As Point
    Private _mousePos As Point
    Private _hoveredViga As cViga = Nothing
    Private _hoveredFrame As cFrame = Nothing

    ' ── Modo agrupación manual ────────────────────────────────
    Private _modoAgrupacion As Boolean = False
    Private ReadOnly _framesSeleccionados As New HashSet(Of String)
    Private _clickPos As Point
    Private _pnlAgrupacion As Panel
    Private _lblSelInfo As Label

    ' ── Controles ─────────────────────────────────────────────
    Private WithEvents _panel As BufferedPanel
    Private _lblInfo As Label
    Private _btnFit As Button
    Private _chkAllFloors As CheckBox
    Private _lblZoom As Label

    ' ── Propiedades para modo agrupación ──────────────────────
    Public Property VigaEditada As cViga = Nothing
    Public Property FramesResultantes As New List(Of String)

    ' Notifica al padre cuando el usuario hace doble clic sobre una viga
    Public Event VigaSeleccionadaPorDobleClick As Action(Of cViga)

    Public Property ModoAgrupacion As Boolean
        Get
            Return _modoAgrupacion
        End Get
        Set(value As Boolean)
            _modoAgrupacion = value
            If _pnlAgrupacion IsNot Nothing Then _pnlAgrupacion.Visible = value
            If _chkAllFloors IsNot Nothing Then _chkAllFloors.Enabled = Not value
            If value Then
                _panel.Cursor = Cursors.Default
                If VigaEditada IsNot Nothing Then
                    _framesSeleccionados.Clear()
                    For Each f In VigaEditada.Frames
                        _framesSeleccionados.Add(f.ObjectLabel)
                    Next
                    ActualizarContadorSeleccion()
                End If
            Else
                _panel.Cursor = Cursors.Hand
            End If
        End Set
    End Property

    ' =====================================================================
    Public Sub New()
        Me.Text = "Vista Interactiva — Planta"
        Me.Size = New Size(1100, 750)
        Me.MinimumSize = New Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(50, 50, 50)
        Me.Font = New Font("Segoe UI", 9)

        ' Ícono del programa
        Try
            Me.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        BuildUI()
    End Sub

    ' =====================================================================
    ' CONSTRUCCIÓN DE LA INTERFAZ
    ' =====================================================================
    Private Sub BuildUI()

        ' ── Barra inferior de información ──────────────────────
        Dim pnlBottom As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 58,
            .BackColor = Color.FromArgb(40, 40, 40),
            .Padding = New Padding(4, 0, 4, 0)
        }

        _lblInfo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(220, 220, 220),
            .Font = New Font("Segoe UI", 9),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 0, 0),
            .Text = "Pasa el cursor sobre un elemento para ver su información."
        }

        _lblZoom = New Label With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .ForeColor = Color.FromArgb(160, 200, 160),
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
            .BackColor = Color.FromArgb(87, 87, 87),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        _btnFit.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120)

        pnlBottom.Controls.Add(_lblInfo)
        pnlBottom.Controls.Add(_lblZoom)
        pnlBottom.Controls.Add(_chkAllFloors)
        pnlBottom.Controls.Add(_btnFit)

        ' ── Panel de dibujo ───────────────────────────────────
        _panel = New BufferedPanel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(245, 248, 252),
            .Cursor = Cursors.Hand
        }

        Me.Controls.Add(_panel)
        Me.Controls.Add(pnlBottom)

        ' Eventos
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

        ' ── Barra de modo agrupación (oculta por defecto) ─────
        _pnlAgrupacion = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 48,
            .BackColor = Color.FromArgb(15, 70, 20),
            .Visible = False,
            .Padding = New Padding(8, 6, 8, 6)
        }

        _lblSelInfo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(200, 240, 200),
            .Font = New Font("Segoe UI", 9.5F),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Text = "Clic en un frame para seleccionarlo o deseleccionarlo.  Rueda o arrastre (botón central) para navegar."
        }

        Dim btnConfirmar As New Button With {
            .Dock = DockStyle.Right,
            .Width = 138,
            .Text = "✓  Confirmar",
            .BackColor = Color.FromArgb(0, 140, 60),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        }
        btnConfirmar.FlatAppearance.BorderSize = 0
        AddHandler btnConfirmar.Click, Sub()
            FramesResultantes = _framesSeleccionados.ToList()
            Me.DialogResult = DialogResult.OK
        End Sub

        Dim btnLimpiar As New Button With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .Text = "Limpiar",
            .BackColor = Color.FromArgb(130, 100, 20),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F)
        }
        btnLimpiar.FlatAppearance.BorderSize = 0
        AddHandler btnLimpiar.Click, Sub()
            _framesSeleccionados.Clear()
            ActualizarContadorSeleccion()
            _panel.Invalidate()
        End Sub

        Dim btnCancelarAgrup As New Button With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .Text = "Cancelar",
            .BackColor = Color.FromArgb(120, 35, 35),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F)
        }
        btnCancelarAgrup.FlatAppearance.BorderSize = 0
        AddHandler btnCancelarAgrup.Click, Sub()
            Me.DialogResult = DialogResult.Cancel
        End Sub

        _pnlAgrupacion.Controls.Add(_lblSelInfo)
        _pnlAgrupacion.Controls.Add(btnConfirmar)
        _pnlAgrupacion.Controls.Add(btnLimpiar)
        _pnlAgrupacion.Controls.Add(btnCancelarAgrup)

        ' Agregar al final → se procesa primero en el docking (Dock=Top)
        Me.Controls.Add(_pnlAgrupacion)

    End Sub

    ' =====================================================================
    ' TRANSFORMADAS MUNDO ↔ PANTALLA
    '   screenX = panX + worldX * zoom
    '   screenY = panY - worldY * zoom   (Y pantalla crece hacia abajo)
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
        ' Doble clic en modo normal → marcar seleccionada y notificar al padre
        If e.Button = MouseButtons.Left AndAlso e.Clicks = 2 AndAlso
           Not _modoAgrupacion AndAlso _hoveredViga IsNot Nothing Then
            VigaSeleccionada = _hoveredViga
            _panel.Invalidate()
            RaiseEvent VigaSeleccionadaPorDobleClick(_hoveredViga)
            Return
        End If

        If _modoAgrupacion Then
            If e.Button = MouseButtons.Left Then
                _clickPos = e.Location   ' registrar punto de inicio para detectar clic vs arrastre
            ElseIf e.Button = MouseButtons.Middle OrElse e.Button = MouseButtons.Right Then
                _isDragging = True
                _lastMouse = e.Location
                _panel.Cursor = Cursors.SizeAll
            End If
            Return
        End If
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
        If _modoAgrupacion Then
            If e.Button = MouseButtons.Left Then
                ' Solo seleccionar si fue un clic (desplazamiento < 6px)
                Dim dx = Math.Abs(e.X - _clickPos.X)
                Dim dy = Math.Abs(e.Y - _clickPos.Y)
                If dx < 6 AndAlso dy < 6 AndAlso
                   _hoveredFrame IsNot Nothing AndAlso _hoveredViga IsNot Nothing Then
                    Dim piso = _hoveredViga.Piso
                    If String.IsNullOrEmpty(PisoActual) OrElse
                       piso.Equals(PisoActual, StringComparison.OrdinalIgnoreCase) Then
                        Dim lbl = _hoveredFrame.ObjectLabel
                        If _framesSeleccionados.Contains(lbl) Then
                            _framesSeleccionados.Remove(lbl)
                        Else
                            _framesSeleccionados.Add(lbl)
                        End If
                        ActualizarContadorSeleccion()
                        _panel.Invalidate()
                    End If
                End If
            End If
            _isDragging = False
            _panel.Cursor = Cursors.Default
            Return
        End If
        _isDragging = False
        _panel.Cursor = Cursors.Hand
    End Sub

    ' =====================================================================
    ' DETECCIÓN DE ELEMENTO BAJO EL CURSOR
    ' =====================================================================
    Private Sub DetectarHover(sx As Integer, sy As Integer)
        If Vigas Is Nothing OrElse Joints Is Nothing Then Return

        Dim wx As Double = 0, wy As Double = 0
        S2W(sx, sy, wx, wy)

        Dim umbral As Double = 8.0 / _zoom   ' ~8 píxeles en coords mundo

        Dim mejorViga As cViga = Nothing
        Dim mejorFrame As cFrame = Nothing
        Dim mejorDist As Double = Double.MaxValue

        For Each v In Vigas
            If Not MostrarViga(v) Then Continue For
            For Each f In v.Frames
                If Not Joints.ContainsKey(f.JointI) OrElse Not Joints.ContainsKey(f.JointJ) Then Continue For
                Dim ji = Joints(f.JointI) : Dim jj = Joints(f.JointJ)
                Dim d = DistASegmento(wx, wy, ji.GlobalX, ji.GlobalY, jj.GlobalX, jj.GlobalY)
                If d < mejorDist Then
                    mejorDist = d
                    mejorViga = v
                    mejorFrame = f
                End If
            Next
        Next

        Dim cambio = (_hoveredViga IsNot mejorViga)

        If mejorDist < umbral Then
            _hoveredViga = mejorViga
            _hoveredFrame = mejorFrame
            MostrarInfoFrame(mejorViga, mejorFrame)
        Else
            If _hoveredViga IsNot Nothing Then
                _hoveredViga = Nothing
                _hoveredFrame = Nothing
                _lblInfo.Text = "Pasa el cursor sobre un elemento para ver información.   |   " &
                                $"Zoom: {Math.Round(_zoom, 1)} px/m"
                _panel.Invalidate()
            End If
        End If
    End Sub

    Private Function MostrarViga(v As cViga) As Boolean
        If _modoAgrupacion Then
            ' En modo agrupación mostrar solo el piso que se está editando
            Return String.IsNullOrEmpty(PisoActual) OrElse
                   v.Piso.Equals(PisoActual, StringComparison.OrdinalIgnoreCase)
        End If
        If _chkAllFloors.Checked Then Return True
        If String.IsNullOrEmpty(PisoActual) Then Return True
        Return v.Piso.Equals(PisoActual, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub MostrarInfoFrame(v As cViga, f As cFrame)
        Dim sec = f.Section
        Dim partes As New List(Of String)
        partes.Add($"Viga: {v.Name_Beam}  |  Piso: {v.Piso}  |  Frame: {f.ObjectLabel}")
        If sec IsNot Nothing Then
            partes.Add($"Sección: {sec.LabelSec}   b={Math.Round(sec.b * 100, 0)} cm  h={Math.Round(sec.h * 100, 0)} cm  d={Math.Round(sec.d * 100, 1)} cm   L={Math.Round(f.Longitud, 3)} m")
        End If
        _lblInfo.Text = String.Join("   ", partes)
        _panel.Invalidate()   ' redibujar para mostrar tooltip en canvas
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
                g.DrawString("Sin datos. Carga y calcula primero.", fMsg, Brushes.Gray,
                             _panel.Width / 2 - 160, _panel.Height / 2 - 12)
            End Using
            Return
        End If

        DibujarGridLines(g)

        If _modoAgrupacion Then
            DibujarVigasAgrupacion(g)
        Else
            DibujarVigas(g)
            ' Tooltip solo en modo normal
            If _hoveredViga IsNot Nothing AndAlso _hoveredFrame IsNot Nothing Then
                DibujarTooltipElemento(g, _hoveredViga, _hoveredFrame)
            End If
        End If

        DibujarEjes(g)
        DibujarEscala(g)
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Tooltip dibujado en canvas — aparece junto al cursor
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarTooltipElemento(g As Graphics, v As cViga, f As cFrame)

        Dim sec = f.Section
        If sec Is Nothing Then Return

        ' ── Armar líneas de texto ──────────────────────────────
        Dim lineas As New List(Of (texto As String, negrita As Boolean, color As Color))

        ' Encabezado
        lineas.Add(($"Frame: {f.ObjectLabel}   —   {v.Name_Beam}", True, Color.FromArgb(30, 60, 120)))

        ' Separador
        lineas.Add(("", False, Color.Transparent))

        ' Geometría y materiales
        lineas.Add(($"Sección : {sec.LabelSec}", False, Color.Black))
        lineas.Add(($"b = {Math.Round(sec.b * 100, 0)} cm     h = {Math.Round(sec.h * 100, 0)} cm     d = {Math.Round(sec.d * 100, 1)} cm", False, Color.Black))
        lineas.Add(($"Longitud: {Math.Round(f.Longitud, 3)} m", False, Color.Black))
        lineas.Add(($"f'c = {sec.fc} MPa     fy = {sec.fy} MPa", False, Color.FromArgb(80, 80, 80)))

        ' Flexión (si hay revisión)
        If f.RevisionFlexion IsNot Nothing AndAlso f.RevisionFlexion.Count > 0 Then
            lineas.Add(("", False, Color.Transparent))
            lineas.Add(("Flexión", True, Color.FromArgb(40, 90, 160)))

            For Each zona In f.RevisionFlexion
                Dim res = zona.ResultadoActual
                If res.AsProvSup = 0 AndAlso res.AsProvInf = 0 Then Continue For

                Dim posNombre As String
                Select Case zona.Posicion
                    Case PosicionTramoViga.Izquierda : posNombre = "Izq"
                    Case PosicionTramoViga.Centro : posNombre = "Cen"
                    Case Else : posNombre = "Der"
                End Select

                If res.AsProvSup > 0 Then
                    Dim ok = res.RatioSup >= 0.9
                    Dim clr = If(ok, Color.FromArgb(0, 120, 0), Color.FromArgb(180, 0, 0))
                    lineas.Add(($"  {posNombre} As⁻ = {Math.Round(res.AsProvSup, 0)} mm²   F = {Math.Round(res.RatioSup, 2)}{If(ok, " ✓", " ✗")}", False, clr))
                End If
                If res.AsProvInf > 0 Then
                    Dim ok = res.RatioInf >= 0.9
                    Dim clr = If(ok, Color.FromArgb(0, 120, 0), Color.FromArgb(180, 0, 0))
                    lineas.Add(($"  {posNombre} As⁺ = {Math.Round(res.AsProvInf, 0)} mm²   F = {Math.Round(res.RatioInf, 2)}{If(ok, " ✓", " ✗")}", False, clr))
                End If
            Next
        End If

        ' Cortante (si hay revisión con φVn calculado)
        Dim corZonas = f.RevisionCortante.Where(Function(z) z.phiVn > 0).ToList()
        If corZonas.Count > 0 Then
            lineas.Add(("", False, Color.Transparent))
            lineas.Add(("Cortante", True, Color.FromArgb(160, 80, 0)))

            For Each z In corZonas
                Dim posNombre As String
                Select Case z.Posicion
                    Case PosicionTramoViga.Izquierda : posNombre = "Izq"
                    Case PosicionTramoViga.Centro : posNombre = "Cen"
                    Case Else : posNombre = "Der"
                End Select
                Dim ok = z.Cumple
                Dim clr = If(ok, Color.FromArgb(0, 120, 0), Color.FromArgb(180, 0, 0))
                lineas.Add(($"  {posNombre}  Vu={Math.Round(z.Vu, 1)} kN   φVn={Math.Round(z.phiVn, 1)} kN   F={Math.Round(z.Factor, 2)}{If(ok, " ✓", " ✗")}", False, clr))
            Next
        End If

        ' ── Medir el tamaño del box ────────────────────────────
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

        ' ── Posicionar junto al cursor ─────────────────────────
        Dim ox As Single = _mousePos.X + 18
        Dim oy As Single = _mousePos.Y + 5

        ' Ajustar si se sale del panel
        If ox + boxW > _panel.Width - 5 Then ox = _mousePos.X - boxW - 10
        If oy + boxH > _panel.Height - 5 Then oy = _mousePos.Y - boxH - 5

        ' ── Dibujar sombra ────────────────────────────────────
        Using brushSombra As New SolidBrush(Color.FromArgb(40, 0, 0, 0))
            g.FillRectangle(brushSombra, ox + 4, oy + 4, boxW, boxH)
        End Using

        ' ── Dibujar fondo ─────────────────────────────────────
        Using brushFondo As New SolidBrush(Color.FromArgb(252, 252, 255))
            g.FillRectangle(brushFondo, ox, oy, boxW, boxH)
        End Using

        ' Borde
        Using penBorde As New Pen(Color.FromArgb(180, 190, 210), 1.2F)
            g.DrawRectangle(penBorde, ox, oy, boxW, boxH)
        End Using

        ' Barra de color en el top
        Using brushTop As New SolidBrush(Color.FromArgb(40, 70, 130))
            g.FillRectangle(brushTop, ox, oy, boxW, 3)
        End Using

        ' ── Dibujar líneas de texto ───────────────────────────
        Dim y As Single = oy + pad
        For Each ln In lineas
            If String.IsNullOrEmpty(ln.texto) Then
                ' Separador
                Using penSep As New Pen(Color.FromArgb(210, 215, 230), 1)
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
    ' Grid lines
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarGridLines(g As Graphics)
        If GridLines Is Nothing OrElse GridLines.Count = 0 Then Return

        Using penGrid As New Pen(Color.FromArgb(160, 190, 210, 230), 1)
            penGrid.DashStyle = DashStyle.Dash

            Dim wxMin As Double = 0, wyMin As Double = 0, wxMax As Double = 0, wyMax As Double = 0
            S2W(0, _panel.Height, wxMin, wyMin)
            S2W(_panel.Width, 0, wxMax, wyMax)
            Dim ext As Double = 3.0

            For Each gl In GridLines
                If Not gl.Visible Then Continue For

                Dim p1 As PointF, p2 As PointF

                If gl.EsTipoGeneral Then
                    p1 = W2S(gl.X1, gl.Y1)
                    p2 = W2S(gl.X2, gl.Y2)
                ElseIf gl.Direction = "X" Then
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

                ' Burbuja con el ID
                If Not String.IsNullOrEmpty(gl.GridID) AndAlso _zoom > 5 Then
                    Dim labelPt As PointF
                    If gl.EsTipoGeneral Then
                        labelPt = If(gl.BubbleLocation.ToLower() = "start", p1, p2)
                    ElseIf gl.Direction = "X" Then
                        labelPt = New PointF(W2S(gl.Ordinate, 0).X - 8, 6)
                    Else
                        labelPt = New PointF(6, W2S(0, gl.Ordinate).Y - 10)
                    End If

                    Try
                        Using fGrid As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                            Dim sz = g.MeasureString(gl.GridID, fGrid)
                            Using bBg As New SolidBrush(Color.FromArgb(200, Color.White))
                                g.FillEllipse(bBg, labelPt.X - 2, labelPt.Y,
                                              sz.Width + 4, sz.Height)
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
    ' Vigas
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarVigas(g As Graphics)
        If Vigas Is Nothing OrElse Joints Is Nothing Then Return

        For Each v In Vigas
            If Not MostrarViga(v) Then Continue For

            Dim esSelec = VigaSeleccionada IsNot Nothing AndAlso v.Name_Beam = VigaSeleccionada.Name_Beam
            Dim esHover = (v Is _hoveredViga)

            Dim colorLinea As Color
            Dim grosor As Single

            Select Case True
                Case esSelec
                    colorLinea = Color.FromArgb(210, 50, 50)
                    grosor = 4.5F
                Case esHover
                    colorLinea = Color.FromArgb(30, 130, 220)
                    grosor = 3.5F
                Case Else
                    colorLinea = Color.FromArgb(40, 70, 130)
                    grosor = 2.0F
            End Select

            For Each f In v.Frames
                If Not Joints.ContainsKey(f.JointI) OrElse Not Joints.ContainsKey(f.JointJ) Then Continue For

                Dim ji = Joints(f.JointI), jj = Joints(f.JointJ)
                Dim p1 = W2S(ji.GlobalX, ji.GlobalY)
                Dim p2 = W2S(jj.GlobalX, jj.GlobalY)

                Try
                    If esSelec Then
                        Using penHalo As New Pen(Color.FromArgb(70, 255, 210, 80), grosor + 5)
                            g.DrawLine(penHalo, p1, p2)
                        End Using
                    End If
                    If esHover AndAlso Not esSelec Then
                        Using penHalo As New Pen(Color.FromArgb(50, 100, 180, 255), grosor + 4)
                            g.DrawLine(penHalo, p1, p2)
                        End Using
                    End If
                    Using pen As New Pen(colorLinea, grosor)
                        g.DrawLine(pen, p1, p2)
                    End Using
                Catch ex As OverflowException
                End Try

                ' Nombre del frame (visible con zoom alto)
                If (esHover OrElse esSelec) AndAlso _zoom > 20 Then
                    DibujarEtiquetaFrame(g, ji, jj, f, colorLinea)
                End If
            Next

            ' Nombre de la viga (zoom medio)
            If _zoom > 15 Then
                DibujarNombreViga(g, v, esSelec, esHover)
            End If
        Next
    End Sub

    Private Sub DibujarEtiquetaFrame(g As Graphics,
                                      ji As cJoint, jj As cJoint,
                                      f As cFrame,
                                      c As Color)
        Try
            Dim mx = (ji.GlobalX + jj.GlobalX) / 2
            Dim my = (ji.GlobalY + jj.GlobalY) / 2
            Dim pt = W2S(mx, my)

            ' Solo si está dentro del panel
            If pt.X < 0 OrElse pt.X > _panel.Width OrElse
               pt.Y < 0 OrElse pt.Y > _panel.Height Then Return

            Dim lbl = f.ObjectLabel
            Using fnt As New Font("Segoe UI", 7.5F)
                Dim sz = g.MeasureString(lbl, fnt)
                Dim rx = pt.X - sz.Width / 2 - 3
                Dim ry = pt.Y - sz.Height - 6
                Using bBg As New SolidBrush(Color.FromArgb(200, Color.White))
                    g.FillRectangle(bBg, rx, ry, sz.Width + 6, sz.Height + 2)
                End Using
                g.DrawString(lbl, fnt, New SolidBrush(c), rx + 3, ry + 1)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    Private Sub DibujarNombreViga(g As Graphics, v As cViga,
                                   esSelec As Boolean, esHover As Boolean)
        If v.Frames.Count = 0 Then Return

        Try
            ' Punto medio de la viga completa
            Dim midIdx = v.Frames.Count \ 2
            Dim fMid = v.Frames(midIdx)

            If Not Joints.ContainsKey(fMid.JointI) OrElse Not Joints.ContainsKey(fMid.JointJ) Then Return

            Dim jiM = Joints(fMid.JointI) : Dim jjM = Joints(fMid.JointJ)
            Dim pt = W2S((jiM.GlobalX + jjM.GlobalX) / 2,
                          (jiM.GlobalY + jjM.GlobalY) / 2)

            If pt.X < 0 OrElse pt.X > _panel.Width OrElse
               pt.Y < 0 OrElse pt.Y > _panel.Height Then Return

            Dim fSize As Single = If(esSelec OrElse esHover, 9.0F, 7.5F)
            Dim brushNombre As Brush = If(esSelec, Brushes.DarkRed,
                                          If(esHover, Brushes.DarkBlue, Brushes.DimGray))

            Using fnt As New Font("Segoe UI", fSize, FontStyle.Bold)
                Dim sz = g.MeasureString(v.Nombre, fnt)
                Dim rx = pt.X - sz.Width / 2 - 2
                Dim ry = pt.Y + 5
                Using bBg As New SolidBrush(Color.FromArgb(180, Color.White))
                    g.FillRectangle(bBg, rx, ry, sz.Width + 4, sz.Height + 2)
                End Using
                g.DrawString(v.Nombre, fnt, brushNombre, rx + 2, ry + 1)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Ejes de coordenadas (esquina inf-izq)
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarEjes(g As Graphics)
        Dim ox As Single = 45, oy As Single = _panel.Height - 45
        Dim len As Single = 30

        Using penX As New Pen(Color.Red, 2)
            g.DrawLine(penX, ox, oy, ox + len, oy)
            g.DrawString("X", New Font("Segoe UI", 7.5F, FontStyle.Bold), Brushes.Red, ox + len + 2, oy - 7)
        End Using

        Using penY As New Pen(Color.FromArgb(0, 160, 0), 2)
            g.DrawLine(penY, ox, oy, ox, oy - len)
            g.DrawString("Y", New Font("Segoe UI", 7.5F, FontStyle.Bold), Brushes.Green, ox + 3, oy - len - 12)
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Barra de escala (esquina inf-der)
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarEscala(g As Graphics)
        ' Elegir longitud "bonita" para la barra
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
            Using penScale As New Pen(Color.FromArgb(80, 80, 80), 2)
                g.DrawLine(penScale, x0, y0, x0 + pixLen, y0)
                g.DrawLine(penScale, x0, y0 - 4, x0, y0 + 4)
                g.DrawLine(penScale, x0 + pixLen, y0 - 4, x0 + pixLen, y0 + 4)
            End Using

            Using fnt As New Font("Segoe UI", 7.5F)
                Dim lbl = $"{niceLen} m"
                Dim sz = g.MeasureString(lbl, fnt)
                g.DrawString(lbl, fnt, Brushes.DimGray,
                             x0 + pixLen / 2 - sz.Width / 2, y0 - 17)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    ' =====================================================================
    ' DIBUJO — MODO AGRUPACIÓN
    ' =====================================================================

    Private Sub DibujarVigasAgrupacion(g As Graphics)
        If Vigas Is Nothing OrElse Joints Is Nothing Then Return

        For Each v In Vigas
            If Not MostrarViga(v) Then Continue For

            For Each f In v.Frames
                If Not Joints.ContainsKey(f.JointI) OrElse Not Joints.ContainsKey(f.JointJ) Then Continue For

                Dim ji = Joints(f.JointI), jj = Joints(f.JointJ)
                Dim p1 = W2S(ji.GlobalX, ji.GlobalY)
                Dim p2 = W2S(jj.GlobalX, jj.GlobalY)

                Dim esSelec = _framesSeleccionados.Contains(f.ObjectLabel)
                Dim esHover = (f Is _hoveredFrame)

                Dim colorFrame As Color
                Dim grosor As Single

                If esSelec Then
                    colorFrame = Color.FromArgb(30, 200, 80)   ' verde brillante = en la viga
                    grosor = 4.5F
                ElseIf esHover Then
                    colorFrame = Color.FromArgb(255, 215, 0)   ' amarillo = hover
                    grosor = 3.5F
                Else
                    colorFrame = Color.FromArgb(100, 115, 145)  ' gris azulado = disponible
                    grosor = 1.8F
                End If

                Try
                    If esSelec Then
                        Using penHalo As New Pen(Color.FromArgb(80, 40, 220, 90), grosor + 6)
                            g.DrawLine(penHalo, p1, p2)
                        End Using
                    ElseIf esHover Then
                        Using penHalo As New Pen(Color.FromArgb(70, 255, 230, 0), grosor + 5)
                            g.DrawLine(penHalo, p1, p2)
                        End Using
                    End If

                    Using pen As New Pen(colorFrame, grosor)
                        g.DrawLine(pen, p1, p2)
                    End Using

                    If (esSelec OrElse esHover) AndAlso _zoom > 12 Then
                        DibujarEtiquetaFrame(g, ji, jj, f, colorFrame)
                    End If
                Catch ex As OverflowException
                End Try
            Next

            If _zoom > 10 Then
                DibujarNombreVigaAgrupacion(g, v)
            End If
        Next

        DibujarLeyendaAgrupacion(g)
    End Sub

    Private Sub DibujarNombreVigaAgrupacion(g As Graphics, v As cViga)
        If v.Frames.Count = 0 Then Return
        Try
            Dim midIdx = v.Frames.Count \ 2
            Dim fMid = v.Frames(midIdx)
            If Not Joints.ContainsKey(fMid.JointI) OrElse Not Joints.ContainsKey(fMid.JointJ) Then Return

            Dim jiM = Joints(fMid.JointI), jjM = Joints(fMid.JointJ)
            Dim pt = W2S((jiM.GlobalX + jjM.GlobalX) / 2, (jiM.GlobalY + jjM.GlobalY) / 2)

            If pt.X < 0 OrElse pt.X > _panel.Width OrElse
               pt.Y < 0 OrElse pt.Y > _panel.Height Then Return

            Dim tieneSelec = v.Frames.Any(Function(f) _framesSeleccionados.Contains(f.ObjectLabel))
            Dim clrNombre As Color = If(tieneSelec, Color.FromArgb(10, 130, 40), Color.FromArgb(90, 100, 120))

            Using fnt As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                Dim sz = g.MeasureString(v.Nombre, fnt)
                Dim rx = pt.X - sz.Width / 2 - 2
                Dim ry = pt.Y + 5
                Using bBg As New SolidBrush(Color.FromArgb(190, Color.White))
                    g.FillRectangle(bBg, rx, ry, sz.Width + 4, sz.Height + 2)
                End Using
                g.DrawString(v.Nombre, fnt, New SolidBrush(clrNombre), rx + 2, ry + 1)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    Private Sub DibujarLeyendaAgrupacion(g As Graphics)
        Dim items As New List(Of (clr As Color, texto As String)) From {
            (Color.FromArgb(30, 200, 80), $"En esta viga ({_framesSeleccionados.Count} frames)"),
            (Color.FromArgb(100, 115, 145), "Disponibles (clic para agregar)"),
            (Color.FromArgb(255, 215, 0), "Frame bajo el cursor")
        }

        Dim padX As Single = 12, padY As Single = 12
        Dim lineH As Single = 20
        Dim boxH As Single = items.Count * lineH + padY * 2
        Dim boxW As Single = 250
        Dim ox As Single = _panel.Width - boxW - 10
        Dim oy As Single = _panel.Height - boxH - 50

        Using bFondo As New SolidBrush(Color.FromArgb(210, 255, 255, 255))
            g.FillRectangle(bFondo, ox, oy, boxW, boxH)
        End Using
        Using penB As New Pen(Color.FromArgb(160, 160, 170), 1)
            g.DrawRectangle(penB, ox, oy, boxW, boxH)
        End Using

        Dim y As Single = oy + padY
        Using fnt As New Font("Segoe UI", 8)
            For Each item In items
                Using penLine As New Pen(item.clr, 3)
                    g.DrawLine(penLine, ox + padX, y + 8, ox + padX + 20, y + 8)
                End Using
                g.DrawString(item.texto, fnt, New SolidBrush(Color.FromArgb(40, 40, 40)),
                             ox + padX + 26, y)
                y += lineH
            Next
        End Using
    End Sub

    Private Sub ActualizarContadorSeleccion()
        If _lblSelInfo Is Nothing Then Return
        _lblSelInfo.Text = $"Clic = seleccionar / deseleccionar frame.   " &
                           $"Frames seleccionados: {_framesSeleccionados.Count}   " &
                           $"|   Piso: {PisoActual}   " &
                           $"|   Rueda o botón central = navegar"
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
