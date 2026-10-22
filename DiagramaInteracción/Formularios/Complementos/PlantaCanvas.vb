Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

' ═══════════════════════════════════════════════════════════════════════════════
'  PlantaCanvas — UserControl reusable para todas las plantas estructurales.
'
'  Encapsula el patrón común (pan, zoom, grid de ejes ETABS, ejes X/Y, barra de
'  escala, leyenda, ficha hover, selección, combo de modos, botón "Ajustar vista"
'  y mensaje de "faltan datos") que hoy repiten a mano Form_Planta_Zapatas,
'  Form_PlantaInteractivaNervios y la planta de vigas de DiagramaService.
'
'  Lo que varía por módulo entra por callbacks:
'
'    OnResolverBounds   RectangleF del contenido en coordenadas mundo (m).
'    OnDrawElementos    Pintá tus elementos con el transform recibido.
'    OnHitTest          Dado un punto mundo, devolvé el elemento (o Nothing).
'    OnBuildFicha       Lista de líneas para la ficha hover (1a línea = título).
'    OnBuildLeyenda     Items de la leyenda esquina sup-der.
'
'  El consumer no toca pan/zoom ni Paint. Lee HoverElement/SelectedElement si
'  los necesita para pintar highlights.
' ═══════════════════════════════════════════════════════════════════════════════
<DesignerCategory("Code")>
Public Class PlantaCanvas
    Inherits UserControl

    ' ─── Datos externos ────────────────────────────────────────
    Public Property GridLines As List(Of cGridLine)
    Public Property MensajeVacio As String = "Sin datos para dibujar."
    Public Property TituloVacio As String = ""

    Private _selected As Object
    Public Property SelectedElement As Object
        Get
            Return _selected
        End Get
        Set(value As Object)
            If Not ReferenceEquals(_selected, value) Then
                _selected = value
                Invalidate()
            End If
        End Set
    End Property

    Public ReadOnly Property HoverElement As Object
        Get
            Return _hoverEl
        End Get
    End Property

    ' ─── Callbacks (lo variable por módulo) ────────────────────
    Public Property OnResolverBounds As Func(Of RectangleF)
    Public Property OnDrawElementos As Action(Of Graphics, PlantaTransform)
    Public Property OnHitTest As Func(Of Double, Double, Object)
    Public Property OnBuildFicha As Func(Of Object, IList(Of String))
    Public Property OnBuildLeyenda As Func(Of IList(Of PlantaLeyendaItem))

    ' ─── Toolbar: modos opcionales ─────────────────────────────
    Private _modos As IList(Of String)
    Public Property Modos As IList(Of String)
        Get
            Return _modos
        End Get
        Set(value As IList(Of String))
            _modos = value
            RebuildComboModos()
        End Set
    End Property

    Public ReadOnly Property ModoActualIndex As Integer
        Get
            If _cmbModo Is Nothing OrElse _cmbModo.SelectedIndex < 0 Then Return 0
            Return _cmbModo.SelectedIndex
        End Get
    End Property

    Public Property TextoAyuda As String = ""

    ' Panel inferior expuesto para que el consumer agregue controles propios si quiere.
    Public ReadOnly Property ToolBar As Panel
        Get
            Return _toolBar
        End Get
    End Property

    ' ─── Eventos ───────────────────────────────────────────────
    Public Event ElementoSeleccionado(elemento As Object)
    Public Event ElementoDoubleClick(elemento As Object)
    Public Event ModoCambio(nuevoIndex As Integer)
    Public Event VistaCambio()

    ' ─── Estado interno (pan/zoom/mouse) ───────────────────────
    Private _zoom As Double = 50.0
    Private _panX As Double = 0.0
    Private _panY As Double = 0.0
    Private _arrastrando As Boolean = False
    Private _seArrastro As Boolean = False
    Private _ptAbajo As Point
    Private _ultimoMouse As Point
    Private _posMouse As Point
    Private _hoverEl As Object

    ' ─── Controles ─────────────────────────────────────────────
    Private _panel As PanelDobleBuffer
    Private _toolBar As Panel
    Private _lblInfo As Label
    Private _lblZoom As Label
    Private _btnAjustar As Button
    Private _cmbModo As ComboBox
    Private _lblModoSep As Label
    Private _bannerAviso As Panel
    Private _lblAviso As Label
    Private _btnAviso As Button
    Private _onAvisoClick As Action

    ' ─── Paleta ────────────────────────────────────────────────
    Private Shared ReadOnly ColorFondo As Color = Color.FromArgb(245, 248, 252)
    Private Shared ReadOnly ColorToolBar As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly ColorBoton As Color = Color.FromArgb(224, 224, 224)

    ' =====================================================================
    Public Sub New()
        MyBase.New()
        Me.BackColor = ColorFondo
        Me.Font = New Font("Segoe UI", 9)
        Me.DoubleBuffered = True
        ConstruirUI()
    End Sub

    Private Sub ConstruirUI()

        _toolBar = New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 40,
            .BackColor = ColorToolBar,
            .Padding = New Padding(4, 0, 4, 0)
        }

        _lblInfo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(230, 230, 230),
            .Font = New Font("Segoe UI", 9),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 0, 0),
            .Text = ""
        }

        _lblZoom = New Label With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .ForeColor = Color.FromArgb(190, 220, 190),
            .Font = New Font("Segoe UI", 8),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Text = "Zoom: --"
        }

        _btnAjustar = New Button With {
            .Dock = DockStyle.Right,
            .Width = 110,
            .Text = "Ajustar vista",
            .BackColor = ColorBoton,
            .ForeColor = GrisARCO,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        _btnAjustar.FlatAppearance.BorderSize = 0

        _cmbModo = New ComboBox With {
            .Dock = DockStyle.Right,
            .Width = 160,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = ColorBoton,
            .ForeColor = GrisARCO,
            .Margin = New Padding(0, 6, 0, 6),
            .Visible = False
        }

        _lblModoSep = New Label With {
            .Dock = DockStyle.Right,
            .Width = 8,
            .BackColor = ColorToolBar,
            .Visible = False
        }

        _toolBar.Controls.Add(_lblInfo)
        _toolBar.Controls.Add(_lblZoom)
        _toolBar.Controls.Add(_lblModoSep)
        _toolBar.Controls.Add(_cmbModo)
        _toolBar.Controls.Add(_btnAjustar)

        _panel = New PanelDobleBuffer With {
            .Dock = DockStyle.Fill,
            .BackColor = ColorFondo,
            .Cursor = Cursors.Hand
        }

        ' Banner superior de aviso: oculto por defecto. Se muestra con MostrarAviso().
        _bannerAviso = New Panel With {
            .Dock = DockStyle.Top,
            .Height = 44,
            .BackColor = Color.FromArgb(255, 243, 205),
            .Padding = New Padding(10, 0, 10, 0),
            .Visible = False
        }
        _lblAviso = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(102, 77, 3),
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Text = ""
        }
        _btnAviso = New Button With {
            .Dock = DockStyle.Right,
            .Width = 180,
            .Text = "",
            .BackColor = Color.FromArgb(255, 193, 7),
            .ForeColor = Color.FromArgb(60, 50, 10),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Cursor = Cursors.Hand,
            .Margin = New Padding(0, 6, 0, 6)
        }
        _btnAviso.FlatAppearance.BorderSize = 0
        AddHandler _btnAviso.Click, Sub()
                                        If _onAvisoClick IsNot Nothing Then _onAvisoClick.Invoke()
                                    End Sub
        _bannerAviso.Controls.Add(_lblAviso)
        _bannerAviso.Controls.Add(_btnAviso)

        Me.Controls.Add(_panel)
        Me.Controls.Add(_toolBar)
        Me.Controls.Add(_bannerAviso)

        AddHandler _panel.Paint, AddressOf Panel_Paint
        AddHandler _panel.MouseWheel, AddressOf Panel_MouseWheel
        AddHandler _panel.MouseDown, AddressOf Panel_MouseDown
        AddHandler _panel.MouseMove, AddressOf Panel_MouseMove
        AddHandler _panel.MouseUp, AddressOf Panel_MouseUp
        AddHandler _panel.MouseDoubleClick, AddressOf Panel_MouseDoubleClick
        AddHandler _panel.MouseEnter, Sub() _panel.Focus()
        AddHandler _panel.Resize, Sub() _panel.Invalidate()
        AddHandler _btnAjustar.Click, Sub() AjustarVista()
        AddHandler _cmbModo.SelectedIndexChanged,
            Sub()
                _panel.Invalidate()
                RaiseEvent ModoCambio(_cmbModo.SelectedIndex)
            End Sub
    End Sub

    Private Sub RebuildComboModos()
        If _cmbModo Is Nothing Then Return
        _cmbModo.Items.Clear()
        If _modos Is Nothing OrElse _modos.Count = 0 Then
            _cmbModo.Visible = False
            _lblModoSep.Visible = False
            Return
        End If
        For Each m In _modos
            _cmbModo.Items.Add(m)
        Next
        _cmbModo.SelectedIndex = 0
        _cmbModo.Visible = True
        _lblModoSep.Visible = True
    End Sub

    ' =====================================================================
    ' API pública
    ' =====================================================================
    Public Sub Refrescar()
        If _panel IsNot Nothing Then _panel.Invalidate()
    End Sub

    ''' <summary>
    ''' Muestra una franja de aviso arriba del canvas con un botón de acción.
    ''' Si textoBoton es Nothing/vacío, solo se muestra el mensaje sin botón.
    ''' Llamar con texto vacío (o OcultarAviso) para esconderlo.
    ''' </summary>
    Public Sub MostrarAviso(texto As String,
                            Optional textoBoton As String = "",
                            Optional accion As Action = Nothing)
        If _bannerAviso Is Nothing Then Return
        If String.IsNullOrWhiteSpace(texto) Then
            OcultarAviso()
            Return
        End If
        _lblAviso.Text = texto
        If String.IsNullOrWhiteSpace(textoBoton) Then
            _btnAviso.Visible = False
            _onAvisoClick = Nothing
        Else
            _btnAviso.Text = textoBoton
            _btnAviso.Visible = True
            _onAvisoClick = accion
        End If
        _bannerAviso.Visible = True
    End Sub

    Public Sub OcultarAviso()
        If _bannerAviso Is Nothing Then Return
        _bannerAviso.Visible = False
        _onAvisoClick = Nothing
    End Sub

    ''' <summary>
    ''' Encuadra el bounding box devuelto por OnResolverBounds en el panel visible.
    ''' El margen se agrega al bbox del contenido, no solo a sus centros, para
    ''' que los elementos del borde no queden cortados.
    ''' </summary>
    Public Sub AjustarVista()
        If _panel Is Nothing OrElse _panel.Width <= 2 OrElse _panel.Height <= 2 Then Return

        Dim bounds = ObtenerBoundsSeguro()
        If bounds.IsEmpty OrElse bounds.Width <= 0 OrElse bounds.Height <= 0 Then
            _panel.Invalidate()
            Return
        End If

        Dim anchoMundo = Math.Max(CDbl(bounds.Width), 0.1)
        Dim altoMundo = Math.Max(CDbl(bounds.Height), 0.1)
        Dim marg As Double = 70

        _zoom = Math.Max(1, Math.Min((_panel.Width - 2 * marg) / anchoMundo,
                                     (_panel.Height - 2 * marg) / altoMundo))

        Dim cx = bounds.Left + bounds.Width / 2.0
        Dim cy = bounds.Top + bounds.Height / 2.0
        _panX = _panel.Width / 2.0 - cx * _zoom
        _panY = _panel.Height / 2.0 + cy * _zoom

        _lblZoom.Text = $"Zoom: {Math.Round(_zoom, 1)} px/m"
        _panel.Invalidate()
        RaiseEvent VistaCambio()
    End Sub

    ''' <summary>Render off-screen al tamaño actual del panel.</summary>
    Public Function ExportarPNG() As Bitmap
        If _panel Is Nothing OrElse _panel.Width <= 0 OrElse _panel.Height <= 0 Then
            Return New Bitmap(1, 1)
        End If
        Dim bmp As New Bitmap(_panel.Width, _panel.Height, PixelFormat.Format32bppArgb)
        Using g = Graphics.FromImage(bmp)
            DibujarEscena(g, _panel.Width, _panel.Height)
        End Using
        Return bmp
    End Function

    ''' <summary>
    ''' Render off-screen a un tamaño arbitrario (para informes .docx). Hace su propio fit al
    ''' bounding box devuelto por OnResolverBounds — no depende del tamaño actual del panel ni
    ''' de que el control esté visible. Si <paramref name="modoIndex"/> ≥ 0 y el combo de modos
    ''' tiene esa cantidad de items, cambia temporalmente el modo activo para el render.
    ''' Deja el estado interno (zoom, pan, hover, modo) exactamente como estaba.
    ''' </summary>
    Public Function RenderBitmap(ancho As Integer, alto As Integer,
                                 Optional modoIndex As Integer = -1) As Bitmap
        If ancho <= 0 OrElse alto <= 0 Then Return New Bitmap(1, 1)

        Dim zOld = _zoom, pxOld = _panX, pyOld = _panY
        Dim hoverOld = _hoverEl
        Dim modoOld As Integer = -1
        If modoIndex >= 0 AndAlso _cmbModo IsNot Nothing AndAlso modoIndex < _cmbModo.Items.Count Then
            modoOld = _cmbModo.SelectedIndex
            _cmbModo.SelectedIndex = modoIndex
        End If
        _hoverEl = Nothing

        Try
            Dim bounds = ObtenerBoundsSeguro()
            If Not bounds.IsEmpty AndAlso bounds.Width > 0 AndAlso bounds.Height > 0 Then
                Dim anchoMundo = Math.Max(CDbl(bounds.Width), 0.1)
                Dim altoMundo = Math.Max(CDbl(bounds.Height), 0.1)
                Dim marg As Double = 70
                _zoom = Math.Max(1, Math.Min((ancho - 2 * marg) / anchoMundo,
                                             (alto - 2 * marg) / altoMundo))
                Dim cx = bounds.Left + bounds.Width / 2.0
                Dim cy = bounds.Top + bounds.Height / 2.0
                _panX = ancho / 2.0 - cx * _zoom
                _panY = alto / 2.0 + cy * _zoom
            End If

            Dim bmp As New Bitmap(ancho, alto, PixelFormat.Format32bppArgb)
            Using g = Graphics.FromImage(bmp)
                DibujarEscena(g, ancho, alto)
            End Using
            Return bmp
        Finally
            _zoom = zOld
            _panX = pxOld
            _panY = pyOld
            _hoverEl = hoverOld
            If modoOld >= 0 Then _cmbModo.SelectedIndex = modoOld
        End Try
    End Function

    ' =====================================================================
    ' Transform mundo <-> pantalla
    ' =====================================================================
    Private Function W2S(wx As Double, wy As Double) As PointF
        Return New PointF(CSng(_panX + wx * _zoom), CSng(_panY - wy * _zoom))
    End Function

    Private Sub S2W(sx As Double, sy As Double, ByRef wx As Double, ByRef wy As Double)
        wx = (sx - _panX) / _zoom
        wy = (_panY - sy) / _zoom
    End Sub

    Private Function BuildTransform() As PlantaTransform
        Return New PlantaTransform(_zoom, _panX, _panY)
    End Function

    Private Function ObtenerBoundsSeguro() As RectangleF
        If OnResolverBounds Is Nothing Then Return RectangleF.Empty
        Try
            Return OnResolverBounds.Invoke()
        Catch
            Return RectangleF.Empty
        End Try
    End Function

    ' =====================================================================
    ' Interacción
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
        RaiseEvent VistaCambio()
    End Sub

    Private Sub Panel_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left OrElse e.Button = MouseButtons.Middle Then
            _arrastrando = True
            _seArrastro = False
            _ptAbajo = e.Location
            _ultimoMouse = e.Location
            _panel.Cursor = Cursors.SizeAll
        End If
    End Sub

    Private Sub Panel_MouseMove(sender As Object, e As MouseEventArgs)
        _posMouse = e.Location

        If _arrastrando Then
            If Math.Abs(e.X - _ptAbajo.X) > 5 OrElse Math.Abs(e.Y - _ptAbajo.Y) > 5 Then
                _seArrastro = True
            End If
            _panX += e.X - _ultimoMouse.X
            _panY += e.Y - _ultimoMouse.Y
            _ultimoMouse = e.Location
            _panel.Invalidate()
        Else
            DetectarHover(e.X, e.Y)
        End If
    End Sub

    Private Sub Panel_MouseUp(sender As Object, e As MouseEventArgs)
        Dim estaba = _arrastrando
        _arrastrando = False
        _panel.Cursor = Cursors.Hand

        ' Un clic es un drag que no se movió más de 5 px — así no se pierde la
        ' selección cada vez que uno desplaza la vista.
        If estaba AndAlso Not _seArrastro AndAlso e.Button = MouseButtons.Left Then
            If _hoverEl IsNot Nothing Then
                SelectedElement = _hoverEl
                RaiseEvent ElementoSeleccionado(_hoverEl)
            End If
        End If
    End Sub

    Private Sub Panel_MouseDoubleClick(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Return
        If OnHitTest Is Nothing Then Return

        Dim wx As Double = 0, wy As Double = 0
        S2W(e.X, e.Y, wx, wy)
        Dim el As Object = Nothing
        Try
            el = OnHitTest.Invoke(wx, wy)
        Catch
            Return
        End Try

        If el IsNot Nothing Then
            SelectedElement = el
            RaiseEvent ElementoDoubleClick(el)
        End If
    End Sub

    Private Sub DetectarHover(sx As Integer, sy As Integer)
        If OnHitTest Is Nothing Then Return

        Dim wx As Double = 0, wy As Double = 0
        S2W(sx, sy, wx, wy)

        Dim anterior = _hoverEl
        Try
            _hoverEl = OnHitTest.Invoke(wx, wy)
        Catch
            _hoverEl = Nothing
        End Try

        If Not ReferenceEquals(anterior, _hoverEl) Then
            ActualizarInfoHover()
            _panel.Invalidate()
        End If
    End Sub

    Private Sub ActualizarInfoHover()
        If _hoverEl Is Nothing Then
            _lblInfo.Text = TextoAyuda
            Return
        End If

        If OnBuildFicha Is Nothing Then
            _lblInfo.Text = _hoverEl.ToString()
            Return
        End If

        Try
            Dim lineas = OnBuildFicha.Invoke(_hoverEl)
            If lineas Is Nothing OrElse lineas.Count = 0 Then
                _lblInfo.Text = TextoAyuda
            Else
                _lblInfo.Text = String.Join("   |   ", lineas)
            End If
        Catch
            _lblInfo.Text = ""
        End Try
    End Sub

    ' =====================================================================
    ' Pintado — orquesta toda la escena
    ' =====================================================================
    Private Sub Panel_Paint(sender As Object, e As PaintEventArgs)
        DibujarEscena(e.Graphics, _panel.Width, _panel.Height)
    End Sub

    Private Sub DibujarEscena(g As Graphics, w As Integer, h As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(ColorFondo)

        Dim bounds = ObtenerBoundsSeguro()
        If bounds.IsEmpty OrElse bounds.Width <= 0 OrElse bounds.Height <= 0 Then
            DibujarMensajeVacio(g, w, h)
            Return
        End If

        DibujarGridLines(g, w, h)

        If OnDrawElementos IsNot Nothing Then
            Try
                OnDrawElementos.Invoke(g, BuildTransform())
            Catch
                ' Si el consumer tira al dibujar, al menos no quemamos la UI.
            End Try
        End If

        If _hoverEl IsNot Nothing Then DibujarFicha(g, w, h, _hoverEl)

        DibujarLeyenda(g, w, h)
        DibujarEjesCoord(g, h)
        DibujarEscala(g, w, h)
    End Sub

    Private Sub DibujarMensajeVacio(g As Graphics, w As Integer, h As Integer)
        Using fTit As New Font("Segoe UI", 13, FontStyle.Bold),
              f As New Font("Segoe UI", 10),
              bTit As New SolidBrush(Color.FromArgb(70, 70, 70)),
              b As New SolidBrush(Color.FromArgb(110, 110, 110)),
              sf As New StringFormat() With {.Alignment = StringAlignment.Center,
                                             .LineAlignment = StringAlignment.Center}

            If Not String.IsNullOrEmpty(TituloVacio) Then
                g.DrawString(TituloVacio, fTit, bTit,
                             New RectangleF(20, h / 3 - 20, w - 40, 30), sf)
                g.DrawString(MensajeVacio, f, b,
                             New RectangleF(20, h / 3 + 15, w - 40, h / 2), sf)
            Else
                g.DrawString(MensajeVacio, f, b,
                             New RectangleF(20, 20, w - 40, h - 40), sf)
            End If
        End Using
    End Sub

    Private Sub DibujarGridLines(g As Graphics, w As Integer, h As Integer)
        If GridLines Is Nothing OrElse GridLines.Count = 0 Then Return

        Using penGrid As New Pen(Color.FromArgb(160, 190, 190, 200), 1)
            penGrid.DashStyle = DashStyle.Dash

            Dim wxMin As Double = 0, wyMin As Double = 0, wxMax As Double = 0, wyMax As Double = 0
            S2W(0, h, wxMin, wyMin)
            S2W(w, 0, wxMax, wyMax)
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

                If String.IsNullOrEmpty(gl.GridID) OrElse _zoom <= 5 Then Continue For

                Dim pt As PointF
                If gl.EsTipoGeneral Then
                    pt = If(String.Equals(gl.BubbleLocation, "start", StringComparison.OrdinalIgnoreCase), p1, p2)
                ElseIf gl.Direction = "X" Then
                    pt = New PointF(W2S(gl.Ordinate, 0).X - 8, 6)
                Else
                    pt = New PointF(6, W2S(0, gl.Ordinate).Y - 10)
                End If

                Try
                    Using fGrid As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                        Dim sz = g.MeasureString(gl.GridID, fGrid)
                        Using bBg As New SolidBrush(Color.FromArgb(200, Color.White))
                            g.FillEllipse(bBg, pt.X - 2, pt.Y, sz.Width + 4, sz.Height)
                        End Using
                        g.DrawString(gl.GridID, fGrid, Brushes.SlateGray, pt.X, pt.Y)
                    End Using
                Catch ex As OverflowException
                End Try
            Next
        End Using
    End Sub

    Private Sub DibujarFicha(g As Graphics, w As Integer, h As Integer, el As Object)
        If OnBuildFicha Is Nothing Then Return

        Dim lineas As IList(Of String) = Nothing
        Try
            lineas = OnBuildFicha.Invoke(el)
        Catch
            Return
        End Try
        If lineas Is Nothing OrElse lineas.Count = 0 Then Return

        Using f As New Font("Segoe UI", 8.5F),
              fTit As New Font("Segoe UI", 9, FontStyle.Bold)

            Dim ancho As Single = 0
            For i = 0 To lineas.Count - 1
                Dim fu = If(i = 0, fTit, f)
                ancho = Math.Max(ancho, g.MeasureString(lineas(i), fu).Width)
            Next

            Dim altoLinea As Single = f.Height + 2
            Dim ancho2 As Single = ancho + 20
            Dim alto2 As Single = lineas.Count * altoLinea + 12

            Dim x As Single = Math.Min(_posMouse.X + 16, w - ancho2 - 6)
            Dim y As Single = Math.Min(_posMouse.Y + 16, h - alto2 - 6)
            x = Math.Max(x, 4) : y = Math.Max(y, 4)

            ' Sombra sutil
            Using bSombra As New SolidBrush(Color.FromArgb(40, 0, 0, 0))
                g.FillRectangle(bSombra, x + 2, y + 2, ancho2, alto2)
            End Using

            Using bFondo As New SolidBrush(Color.FromArgb(248, 255, 255, 255)),
                  penBorde As New Pen(Color.FromArgb(180, 87, 87, 87), 1)
                g.FillRectangle(bFondo, x, y, ancho2, alto2)
                g.DrawRectangle(penBorde, x, y, ancho2, alto2)
            End Using

            Dim cy As Single = y + 6
            For i = 0 To lineas.Count - 1
                Dim fu = If(i = 0, fTit, f)
                g.DrawString(lineas(i), fu, Brushes.Black, x + 8, cy)
                cy += altoLinea
            Next
        End Using
    End Sub

    Private Sub DibujarLeyenda(g As Graphics, w As Integer, h As Integer)
        If OnBuildLeyenda Is Nothing Then Return

        Dim items As IList(Of PlantaLeyendaItem) = Nothing
        Try
            items = OnBuildLeyenda.Invoke()
        Catch
            Return
        End Try
        If items Is Nothing OrElse items.Count = 0 Then Return

        Using f As New Font("Segoe UI", 8)

            Dim ancho As Single = 0
            For Each it In items
                Dim txt = ArmarTextoLeyenda(it)
                ancho = Math.Max(ancho, g.MeasureString(txt, f).Width)
            Next

            Dim altoLinea As Single = f.Height + 4
            Dim wBox As Single = ancho + 34
            Dim hBox As Single = items.Count * altoLinea + 10
            Dim x As Single = w - wBox - 12
            Dim y As Single = 12

            ' Sombra sutil
            Using bSombra As New SolidBrush(Color.FromArgb(30, 0, 0, 0))
                g.FillRectangle(bSombra, x + 2, y + 2, wBox, hBox)
            End Using

            Using bFondo As New SolidBrush(Color.FromArgb(230, 255, 255, 255)),
                  pen As New Pen(Color.FromArgb(150, 87, 87, 87), 1)
                g.FillRectangle(bFondo, x, y, wBox, hBox)
                g.DrawRectangle(pen, x, y, wBox, hBox)
            End Using

            Dim cy As Single = y + 5
            For Each it In items
                Using b As New SolidBrush(it.Color)
                    g.FillRectangle(b, x + 7, cy + 3, 14, 10)
                End Using
                g.DrawRectangle(Pens.DimGray, x + 7, cy + 3, 14, 10)
                g.DrawString(ArmarTextoLeyenda(it), f, Brushes.Black, x + 26, cy)
                cy += altoLinea
            Next
        End Using
    End Sub

    Private Shared Function ArmarTextoLeyenda(it As PlantaLeyendaItem) As String
        If it.Conteo.HasValue Then
            Return $"{it.Texto}  ({it.Conteo.Value})"
        End If
        Return it.Texto
    End Function

    Private Sub DibujarEjesCoord(g As Graphics, h As Integer)
        Dim ox As Single = 45, oy As Single = h - 45
        Dim len As Single = 30

        Using penX As New Pen(Color.FromArgb(227, 73, 72), 2),
              fEje As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              bX As New SolidBrush(Color.FromArgb(227, 73, 72))
            g.DrawLine(penX, ox, oy, ox + len, oy)
            g.DrawString("X", fEje, bX, ox + len + 2, oy - 7)
        End Using

        Using penY As New Pen(Color.FromArgb(0, 131, 0), 2),
              fEje As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              bY As New SolidBrush(Color.FromArgb(0, 131, 0))
            g.DrawLine(penY, ox, oy, ox, oy - len)
            g.DrawString("Y", fEje, bY, ox + 3, oy - len - 12)
        End Using
    End Sub

    Private Sub DibujarEscala(g As Graphics, w As Integer, h As Integer)
        Dim largo As Double = 1.0
        For Each candidato In {0.5, 1, 2, 5, 10, 20, 50}
            If candidato * _zoom >= 60 Then
                largo = candidato
                Exit For
            End If
        Next

        Dim pix As Single = CSng(largo * _zoom)
        Dim x0 As Single = w - 20.0F - pix
        Dim y0 As Single = h - 30

        Try
            Using pen As New Pen(Color.FromArgb(90, 90, 90), 2)
                g.DrawLine(pen, x0, y0, x0 + pix, y0)
                g.DrawLine(pen, x0, y0 - 4, x0, y0 + 4)
                g.DrawLine(pen, x0 + pix, y0 - 4, x0 + pix, y0 + 4)
            End Using
            Using f As New Font("Segoe UI", 7.5F)
                Dim lbl = $"{largo} m"
                Dim sz = g.MeasureString(lbl, f)
                g.DrawString(lbl, f, Brushes.DimGray, x0 + pix / 2 - sz.Width / 2, y0 - 17)
            End Using
        Catch ex As OverflowException
        End Try
    End Sub

    ' =====================================================================
    Private Class PanelDobleBuffer
        Inherits Panel
        Sub New()
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                        ControlStyles.UserPaint Or
                        ControlStyles.OptimizedDoubleBuffer, True)
        End Sub
    End Class

End Class

' ═══════════════════════════════════════════════════════════════════════════════
'  PlantaTransform — adaptador inmutable que ve el consumer. Encapsula el estado
'  pan/zoom del canvas para que el callback OnDrawElementos no toque internos.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class PlantaTransform

    Public ReadOnly Zoom As Double
    Private ReadOnly _panX As Double
    Private ReadOnly _panY As Double

    Public Sub New(zoomPxPorMetro As Double, panX As Double, panY As Double)
        Zoom = zoomPxPorMetro
        _panX = panX
        _panY = panY
    End Sub

    Public Function W2S(wx As Double, wy As Double) As PointF
        Return New PointF(CSng(_panX + wx * Zoom), CSng(_panY - wy * Zoom))
    End Function

    Public Function W2SRect(cx As Double, cy As Double, ancho As Double, alto As Double) As RectangleF
        Dim p1 = W2S(cx - ancho / 2, cy + alto / 2)
        Return New RectangleF(p1.X, p1.Y, CSng(ancho * Zoom), CSng(alto * Zoom))
    End Function

    Public Sub S2W(sx As Double, sy As Double, ByRef wx As Double, ByRef wy As Double)
        wx = (sx - _panX) / Zoom
        wy = (_panY - sy) / Zoom
    End Sub

End Class

' ═══════════════════════════════════════════════════════════════════════════════
'  PlantaLeyendaItem — una fila de la leyenda. Conteo opcional (p.ej. "(12)").
' ═══════════════════════════════════════════════════════════════════════════════
Public Class PlantaLeyendaItem
    Public Property Color As Color
    Public Property Texto As String
    Public Property Conteo As Integer?

    Public Sub New()
    End Sub

    Public Sub New(c As Color, t As String)
        Color = c
        Texto = t
    End Sub

    Public Sub New(c As Color, t As String, conteo As Integer)
        Color = c
        Texto = t
        Me.Conteo = conteo
    End Sub
End Class
