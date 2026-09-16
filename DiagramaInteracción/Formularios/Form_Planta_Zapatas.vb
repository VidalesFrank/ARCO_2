Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports ARCO.eNumeradores

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_Planta_Zapatas — planta de cimentación con pan / zoom / hover.
'
'  Mismo patrón de interacción que las plantas de Vigas y Nervios: rueda para
'  zoom, arrastre para desplazar, cursor encima para ver la ficha del elemento,
'  clic para seleccionarlo. Lo que cambia es qué se dibuja y de qué se colorea.
'
'  Tres modos de color, porque son tres preguntas distintas que el ingeniero se
'  hace mirando la misma planta:
'
'    Tipo de apoyo   dónde está cada zapata — es la revisión visual de la
'                    clasificación automática de medianeras y esquineras, que
'                    es justo lo que hay que verificar antes de confiar en el
'                    punzonamiento
'    Estado          cuáles cumplen y cuáles no
'    C/D             qué tan justas van las que cumplen
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Planta_Zapatas
    Inherits Form

    ' ── Datos del modelo ──────────────────────────────────────
    Public Property Zapatas As List(Of cZapata)
    Public Property GridLines As List(Of cGridLine)
    Public Property Seleccionada As cZapata

    Public Event ZapataSeleccionada(z As cZapata)

    ' ── Estado de la vista ────────────────────────────────────
    Private _zoom As Double = 50.0
    Private _panX As Double = 0.0
    Private _panY As Double = 0.0

    ' ── Interacción ───────────────────────────────────────────
    Private _arrastrando As Boolean = False
    Private _seArrastro As Boolean = False
    Private _ptAbajo As Point
    Private _ultimoMouse As Point
    Private _posMouse As Point
    Private _zHover As cZapata = Nothing

    ' ── Controles ─────────────────────────────────────────────
    Private _panel As PanelDobleBuffer
    Private _lblInfo As Label
    Private _lblZoom As Label
    Private _cmbModo As ComboBox
    Private _btnAjustar As Button

    ' ── Resúmenes, calculados una vez por apertura ────────────
    ' Recorrer todas las combinaciones de todas las zapatas en cada repintado
    ' haría inusable el pan y el zoom.
    Private _resumenes As Dictionary(Of cZapata, ZapataService.ResumenZapata)

    Private Enum ModoColor
        TipoApoyo = 0
        Estado = 1
        FactorCD = 2
    End Enum

    ' ── Paleta ────────────────────────────────────────────────
    Private Shared ReadOnly ColorCentral As Color = Color.FromArgb(120, 42, 120, 214)
    Private Shared ReadOnly ColorMedianera As Color = Color.FromArgb(140, 237, 161, 0)
    Private Shared ReadOnly ColorEsquinera As Color = Color.FromArgb(150, 227, 73, 72)
    Private Shared ReadOnly ColorCumple As Color = Color.FromArgb(130, 27, 175, 122)
    Private Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(150, 227, 73, 72)
    Private Shared ReadOnly ColorSinCalcular As Color = Color.FromArgb(70, 150, 150, 150)
    Private Shared ReadOnly ColorFondo As Color = Color.FromArgb(245, 248, 252)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)

    ' =====================================================================
    Public Sub New()

        Me.Text = "Planta de cimentación — Zapatas"
        Me.Size = New Size(1150, 780)
        Me.MinimumSize = New Size(640, 440)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = GrisARCO
        Me.Font = New Font("Segoe UI", 9)

        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ConstruirUI()

    End Sub

    ' =====================================================================
    ' Interfaz
    ' =====================================================================
    Private Sub ConstruirUI()

        Dim pnlAbajo As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 40,
            .BackColor = GrisARCO,
            .Padding = New Padding(4, 0, 4, 0)
        }

        _lblInfo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(230, 230, 230),
            .Font = New Font("Segoe UI", 9),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 0, 0),
            .Text = "Pasa el cursor sobre una zapata para ver su información; haz clic para seleccionarla."
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
            .BackColor = Color.FromArgb(224, 224, 224),
            .ForeColor = GrisARCO,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        _btnAjustar.FlatAppearance.BorderSize = 0

        _cmbModo = New ComboBox With {
            .Dock = DockStyle.Right,
            .Width = 150,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(224, 224, 224),
            .ForeColor = GrisARCO,
            .Margin = New Padding(0, 6, 0, 6)
        }
        _cmbModo.Items.AddRange(New Object() {"Color: tipo de apoyo", "Color: estado", "Color: factor C/D"})
        _cmbModo.SelectedIndex = 0

        Dim lblModo As New Label With {
            .Dock = DockStyle.Right,
            .Width = 8,
            .BackColor = GrisARCO
        }

        pnlAbajo.Controls.Add(_lblInfo)
        pnlAbajo.Controls.Add(_lblZoom)
        pnlAbajo.Controls.Add(lblModo)
        pnlAbajo.Controls.Add(_cmbModo)
        pnlAbajo.Controls.Add(_btnAjustar)

        _panel = New PanelDobleBuffer With {
            .Dock = DockStyle.Fill,
            .BackColor = ColorFondo,
            .Cursor = Cursors.Hand
        }

        Me.Controls.Add(_panel)
        Me.Controls.Add(pnlAbajo)

        AddHandler _panel.Paint, AddressOf Panel_Paint
        AddHandler _panel.MouseWheel, AddressOf Panel_MouseWheel
        AddHandler _panel.MouseDown, AddressOf Panel_MouseDown
        AddHandler _panel.MouseMove, AddressOf Panel_MouseMove
        AddHandler _panel.MouseUp, AddressOf Panel_MouseUp
        AddHandler _panel.MouseDoubleClick, AddressOf Panel_MouseDoubleClick
        AddHandler _panel.MouseEnter, Sub() _panel.Focus()
        AddHandler _panel.Resize, Sub() _panel.Invalidate()
        AddHandler _btnAjustar.Click, Sub() AjustarVista()
        AddHandler _cmbModo.SelectedIndexChanged, Sub() _panel.Invalidate()
        AddHandler Me.Shown, Sub()
                                 CalcularResumenes()
                                 AjustarVista()
                             End Sub

    End Sub

    Private Sub CalcularResumenes()

        _resumenes = New Dictionary(Of cZapata, ZapataService.ResumenZapata)()
        If Zapatas Is Nothing Then Exit Sub

        For Each z In Zapatas
            _resumenes(z) = ZapataService.Resumir(z)
        Next

    End Sub

    Private ReadOnly Property Modo As ModoColor
        Get
            Return CType(Math.Max(0, _cmbModo.SelectedIndex), ModoColor)
        End Get
    End Property

    ''' <summary>Solo se dibujan las zapatas que traen coordenadas del modelo.</summary>
    Private Function Dibujables() As List(Of cZapata)
        If Zapatas Is Nothing Then Return New List(Of cZapata)()
        Return Zapatas.Where(Function(z) z.TieneCoordenadas).ToList()
    End Function

    ' =====================================================================
    ' Transformadas mundo <-> pantalla
    ' =====================================================================
    Private Function W2S(wx As Double, wy As Double) As PointF
        Return New PointF(CSng(_panX + wx * _zoom), CSng(_panY - wy * _zoom))
    End Function

    Private Sub S2W(sx As Double, sy As Double, ByRef wx As Double, ByRef wy As Double)
        wx = (sx - _panX) / _zoom
        wy = (_panY - sy) / _zoom
    End Sub

    ''' <summary>
    ''' Encuadra el conjunto. El margen se toma sobre la envolvente de las
    ''' zapatas, no solo de sus centros, para que las del borde no queden
    ''' cortadas a la mitad.
    ''' </summary>
    Public Sub AjustarVista()

        Dim lista = Dibujables()
        If lista.Count = 0 Then Return

        Dim minX = lista.Min(Function(z) z.CoordX - z.L_b / 2)
        Dim maxX = lista.Max(Function(z) z.CoordX + z.L_b / 2)
        Dim minY = lista.Min(Function(z) z.CoordY - z.L_h / 2)
        Dim maxY = lista.Max(Function(z) z.CoordY + z.L_h / 2)

        Dim anchoMundo = Math.Max(maxX - minX, 0.1)
        Dim altoMundo = Math.Max(maxY - minY, 0.1)

        Dim marg As Double = 70
        _zoom = Math.Max(1, Math.Min((_panel.Width - 2 * marg) / anchoMundo,
                                     (_panel.Height - 2 * marg) / altoMundo))

        _panX = _panel.Width / 2.0 - (minX + maxX) / 2.0 * _zoom
        _panY = _panel.Height / 2.0 + (minY + maxY) / 2.0 * _zoom

        _lblZoom.Text = $"Zoom: {Math.Round(_zoom, 1)} px/m"
        _panel.Invalidate()

    End Sub

    ' =====================================================================
    ' Ratón
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
            If Math.Abs(e.X - _ptAbajo.X) > 5 OrElse Math.Abs(e.Y - _ptAbajo.Y) > 5 Then _seArrastro = True
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

        ' Un clic es un arrastre que no llegó a moverse: así no se pierde la
        ' selección cada vez que se desplaza la vista.
        If estaba AndAlso Not _seArrastro AndAlso e.Button = MouseButtons.Left Then
            If _zHover IsNot Nothing Then
                Seleccionada = _zHover
                _panel.Invalidate()
                RaiseEvent ZapataSeleccionada(_zHover)
            End If
        End If

    End Sub

    ''' <summary>
    ''' Doble clic sobre una zapata: abre la vista de análisis fino
    ''' (Form_07_Zapata_Detalle). Se toma la zapata bajo el cursor sin depender
    ''' del hover previo, para funcionar aunque el ratón se haya movido de golpe.
    ''' </summary>
    Private Sub Panel_MouseDoubleClick(sender As Object, e As MouseEventArgs)

        If e.Button <> MouseButtons.Left Then Return

        Dim wx As Double = 0, wy As Double = 0
        S2W(e.X, e.Y, wx, wy)

        Dim z As cZapata = Nothing
        For Each cand In Dibujables()
            If Math.Abs(wx - cand.CoordX) <= cand.L_b / 2 AndAlso
               Math.Abs(wy - cand.CoordY) <= cand.L_h / 2 Then
                z = cand
                Exit For
            End If
        Next

        If z Is Nothing Then Return

        Try
            Seleccionada = z
            _panel.Invalidate()
            RaiseEvent ZapataSeleccionada(z)
            Form_07_Zapata_Detalle.Mostrar(z, Me)
        Catch ex As Exception
            Logger.Error(ex, "Form_Planta_Zapatas.Panel_MouseDoubleClick", z?.Label_joint)
        End Try

    End Sub

    Private Sub DetectarHover(sx As Integer, sy As Integer)

        Dim wx As Double = 0, wy As Double = 0
        S2W(sx, sy, wx, wy)

        Dim anterior = _zHover
        _zHover = Nothing

        For Each z In Dibujables()
            If Math.Abs(wx - z.CoordX) <= z.L_b / 2 AndAlso
               Math.Abs(wy - z.CoordY) <= z.L_h / 2 Then
                _zHover = z
                Exit For
            End If
        Next

        If Not ReferenceEquals(anterior, _zHover) Then
            MostrarInfo(_zHover)
            _panel.Invalidate()
        End If

    End Sub

    Private Sub MostrarInfo(z As cZapata)

        If z Is Nothing Then
            _lblInfo.Text = "Pasa el cursor sobre una zapata para ver su información; haz clic para seleccionarla."
            Exit Sub
        End If

        Dim r = Resumen(z)
        Dim estado As String = If(r.TieneResultados,
                                  $"peor C/D {r.PeorFactor:F2} ({r.Revision}, {r.Combinacion})",
                                  "sin calcular")

        _lblInfo.Text = $"{z.Label_joint} — {z.Nombre}   |   " &
                        $"{z.L_b:F2} x {z.L_h:F2} x {z.e:F2} m   |   " &
                        $"{ZapataService.NombreTipo(z.TipoApoyo)}{If(z.TipoApoyoManual, " (fijado a mano)", "")}   |   {estado}"

    End Sub

    Private Function Resumen(z As cZapata) As ZapataService.ResumenZapata
        If _resumenes Is Nothing Then Return New ZapataService.ResumenZapata()
        Dim r As ZapataService.ResumenZapata = Nothing
        If _resumenes.TryGetValue(z, r) Then Return r
        Return New ZapataService.ResumenZapata()
    End Function

    ''' <summary>La llama el formulario del módulo cuando cambia la fila activa.</summary>
    Public Sub ActualizarSeleccion(z As cZapata)
        Seleccionada = z
        _panel.Invalidate()
    End Sub

    ''' <summary>Vuelve a leer los resultados. Se llama después de recalcular.</summary>
    Public Sub Refrescar()
        CalcularResumenes()
        _panel.Invalidate()
    End Sub

    ' =====================================================================
    ' Dibujo
    ' =====================================================================
    Private Sub Panel_Paint(sender As Object, e As PaintEventArgs)

        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(ColorFondo)

        Dim lista = Dibujables()

        If lista.Count = 0 Then
            DibujarMensajeVacio(g)
            Return
        End If

        DibujarGridLines(g)
        DibujarZapatas(g, lista)

        If _zHover IsNot Nothing Then DibujarFicha(g, _zHover)

        DibujarLeyenda(g)
        DibujarEjesCoord(g)
        DibujarEscala(g)

    End Sub

    ''' <summary>
    ''' Sin coordenadas no hay planta. Se explica qué falta y cómo conseguirlo,
    ''' porque es el caso más probable al abrir un proyecto viejo: se importó
    ''' antes de que el módulo leyera la hoja de nodos.
    ''' </summary>
    Private Sub DibujarMensajeVacio(g As Graphics)

        Dim msg As String
        If Zapatas Is Nothing OrElse Zapatas.Count = 0 Then
            msg = "No hay zapatas. Importa las reacciones de ETABS y calcula primero."
        Else
            msg = "Las zapatas no tienen coordenadas." & vbCrLf & vbCrLf &
                  "Vuelve a importar el Excel de ETABS incluyendo la hoja de nodos" & vbCrLf &
                  """Objects and Elements - Joints"" (E23) o ""Joint Coordinates"" (E17)." & vbCrLf & vbCrLf &
                  "Está en el menú ""? Tablas ETABS"" del módulo."
        End If

        Using f As New Font("Segoe UI", 11),
              b As New SolidBrush(Color.FromArgb(120, 120, 120)),
              sf As New StringFormat() With {.Alignment = StringAlignment.Center,
                                             .LineAlignment = StringAlignment.Center}
            g.DrawString(msg, f, b, New RectangleF(20, 20, _panel.Width - 40, _panel.Height - 40), sf)
        End Using

    End Sub

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

    Private Sub DibujarZapatas(g As Graphics, lista As List(Of cZapata))

        Using fEtiqueta As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              penBorde As New Pen(Color.FromArgb(90, 90, 90), 1.2F),
              penPedestal As New Pen(Color.FromArgb(70, 70, 70), 1.0F),
              penSelec As New Pen(Color.FromArgb(20, 90, 200), 2.5F),
              penHover As New Pen(Color.FromArgb(255, 140, 0), 2.0F)

            For Each z In lista

                Dim rect = RectPantalla(z.CoordX, z.CoordY, z.L_b, z.L_h)
                If rect.Width < 0.5F OrElse rect.Height < 0.5F Then Continue For

                Using b As New SolidBrush(ColorDe(z))
                    g.FillRectangle(b, rect)
                End Using
                g.DrawRectangle(penBorde, rect.X, rect.Y, rect.Width, rect.Height)

                ' Pedestal: sin él no se ve dónde está la columna, que es
                ' justamente lo que define el perímetro de punzonamiento.
                If z.b > 0 AndAlso z.h > 0 AndAlso _zoom > 12 Then
                    Dim rp = RectPantalla(z.CoordX, z.CoordY, z.b, z.h)
                    Using bp As New SolidBrush(Color.FromArgb(90, 60, 60, 60))
                        g.FillRectangle(bp, rp)
                    End Using
                    g.DrawRectangle(penPedestal, rp.X, rp.Y, rp.Width, rp.Height)
                End If

                ' Una zapata marcada a mano lleva una esquina achaflanada: se
                ' distingue de un vistazo de las que propuso el programa.
                If z.TipoApoyoManual Then DibujarMarcaManual(g, rect)

                If ReferenceEquals(z, Seleccionada) Then
                    g.DrawRectangle(penSelec, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4)
                ElseIf ReferenceEquals(z, _zHover) Then
                    g.DrawRectangle(penHover, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                End If

                If _zoom > 18 AndAlso Not String.IsNullOrEmpty(z.Label_joint) Then
                    Dim sz = g.MeasureString(z.Label_joint, fEtiqueta)
                    If sz.Width < rect.Width + 14 Then
                        g.DrawString(z.Label_joint, fEtiqueta, Brushes.Black,
                                     rect.X + rect.Width / 2 - sz.Width / 2,
                                     rect.Y + rect.Height + 1)
                    End If
                End If

            Next
        End Using

    End Sub

    Private Function RectPantalla(cx As Double, cy As Double, ancho As Double, alto As Double) As RectangleF

        Dim p1 = W2S(cx - ancho / 2, cy + alto / 2)   ' esquina superior izquierda en pantalla
        Return New RectangleF(p1.X, p1.Y, CSng(ancho * _zoom), CSng(alto * _zoom))

    End Function

    Private Sub DibujarMarcaManual(g As Graphics, rect As RectangleF)

        Dim lado As Single = Math.Min(10.0F, Math.Min(rect.Width, rect.Height) / 3.0F)
        If lado < 3 Then Exit Sub

        Using b As New SolidBrush(Color.FromArgb(200, 50, 50, 50))
            g.FillPolygon(b, New PointF() {
                New PointF(rect.Right - lado, rect.Y),
                New PointF(rect.Right, rect.Y),
                New PointF(rect.Right, rect.Y + lado)})
        End Using

    End Sub

    ''' <summary>Color de relleno según el modo elegido en la barra inferior.</summary>
    Private Function ColorDe(z As cZapata) As Color

        Select Case Modo

            Case ModoColor.TipoApoyo
                Select Case z.TipoApoyo
                    Case eTipoApoyoZapata.Esquinera : Return ColorEsquinera
                    Case eTipoApoyoZapata.Medianera : Return ColorMedianera
                    Case Else : Return ColorCentral
                End Select

            Case ModoColor.Estado
                Dim r = Resumen(z)
                If Not r.TieneResultados Then Return ColorSinCalcular
                Return If(r.Cumple, ColorCumple, ColorNoCumple)

            Case Else
                Return ColorPorFactor(Resumen(z))

        End Select

    End Function

    ''' <summary>
    ''' Rampa continua: rojo por debajo del umbral, ámbar justo encima, verde
    ''' cuando sobra capacidad. Las tres bandas son las mismas del semáforo de
    ''' los reportes, para que la planta y el Excel cuenten lo mismo.
    ''' </summary>
    Private Shared Function ColorPorFactor(r As ZapataService.ResumenZapata) As Color

        If Not r.TieneResultados Then Return ColorSinCalcular

        Dim f = r.PeorFactor
        If f < Funciones_00_Varias.UMBRAL_CD Then Return ColorNoCumple
        If f < 1.15 Then Return Color.FromArgb(150, 237, 161, 0)
        Return ColorCumple

    End Function

    ' ─────────────────────────────────────────────────────────
    ' Ficha del elemento bajo el cursor
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarFicha(g As Graphics, z As cZapata)

        Dim r = Resumen(z)

        Dim lineas As New List(Of String) From {
            $"{z.Label_joint}   {z.Nombre}",
            $"Zapata   {z.L_b:F2} x {z.L_h:F2} x {z.e:F2} m",
            $"Pedestal  {z.b:F2} x {z.h:F2} m",
            $"Apoyo    {ZapataService.NombreTipo(z.TipoApoyo)}{If(z.TipoApoyoManual, "  (fijado a mano)", "")}",
            $"alfa_s = {ZapataService.AlfaS(z.TipoApoyo):F0}   b0 = {ZapataService.PerimetroCritico(z.b, z.h, z.d, z.TipoApoyo):F2} m"
        }

        If r.TieneResultados Then
            lineas.Add($"Peor C/D  {r.PeorFactor:F2}   ({r.Revision})")
            lineas.Add($"Combinación  {r.Combinacion}")
        Else
            lineas.Add("Sin calcular")
        End If

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

            ' Se mantiene dentro del panel: pegado al borde derecho o inferior
            ' la ficha se salía y quedaba ilegible.
            Dim x As Single = Math.Min(_posMouse.X + 16, _panel.Width - ancho2 - 6)
            Dim y As Single = Math.Min(_posMouse.Y + 16, _panel.Height - alto2 - 6)
            x = Math.Max(x, 4) : y = Math.Max(y, 4)

            Using bFondo As New SolidBrush(Color.FromArgb(242, 255, 255, 255)),
                  penBorde As New Pen(Color.FromArgb(180, 87, 87, 87), 1)
                g.FillRectangle(bFondo, x, y, ancho2, alto2)
                g.DrawRectangle(penBorde, x, y, ancho2, alto2)
            End Using

            Dim cy As Single = y + 6
            For i = 0 To lineas.Count - 1
                Dim fu = If(i = 0, fTit, f)
                Dim br As Brush = Brushes.Black
                If i = 5 AndAlso r.TieneResultados Then
                    br = If(r.Cumple, New SolidBrush(Color.FromArgb(0, 97, 0)),
                                      New SolidBrush(Color.FromArgb(156, 0, 6)))
                End If
                g.DrawString(lineas(i), fu, br, x + 8, cy)
                If Not ReferenceEquals(br, Brushes.Black) Then br.Dispose()
                cy += altoLinea
            Next

        End Using

    End Sub

    ' ─────────────────────────────────────────────────────────
    ' Leyenda (esquina sup-der)
    ' ─────────────────────────────────────────────────────────
    Private Sub DibujarLeyenda(g As Graphics)

        Dim entradas As New List(Of Tuple(Of Color, String))

        Select Case Modo
            Case ModoColor.TipoApoyo
                Dim lista = Dibujables()
                Dim nCen = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central).Count
                Dim nMed = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera).Count
                Dim nEsq = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera).Count
                entradas.Add(Tuple.Create(ColorCentral, $"Central   ({nCen})"))
                entradas.Add(Tuple.Create(ColorMedianera, $"Medianera  ({nMed})"))
                entradas.Add(Tuple.Create(ColorEsquinera, $"Esquinera  ({nEsq})"))

            Case ModoColor.Estado
                entradas.Add(Tuple.Create(ColorCumple, "Cumple"))
                entradas.Add(Tuple.Create(ColorNoCumple, "No cumple"))
                entradas.Add(Tuple.Create(ColorSinCalcular, "Sin calcular"))

            Case Else
                entradas.Add(Tuple.Create(ColorCumple, "C/D >= 1.15"))
                entradas.Add(Tuple.Create(Color.FromArgb(150, 237, 161, 0), $"C/D {Funciones_00_Varias.UMBRAL_CD:F2} a 1.15"))
                entradas.Add(Tuple.Create(ColorNoCumple, $"C/D < {Funciones_00_Varias.UMBRAL_CD:F2}"))
                entradas.Add(Tuple.Create(ColorSinCalcular, "Sin calcular"))
        End Select

        Using f As New Font("Segoe UI", 8)

            Dim ancho As Single = 0
            For Each en In entradas
                ancho = Math.Max(ancho, g.MeasureString(en.Item2, f).Width)
            Next

            Dim altoLinea As Single = f.Height + 4
            Dim w As Single = ancho + 34
            Dim h As Single = entradas.Count * altoLinea + 10
            Dim x As Single = _panel.Width - w - 12
            Dim y As Single = 12

            Using bFondo As New SolidBrush(Color.FromArgb(225, 255, 255, 255)),
                  pen As New Pen(Color.FromArgb(150, 87, 87, 87), 1)
                g.FillRectangle(bFondo, x, y, w, h)
                g.DrawRectangle(pen, x, y, w, h)
            End Using

            Dim cy As Single = y + 5
            For Each en In entradas
                Using b As New SolidBrush(en.Item1)
                    g.FillRectangle(b, x + 7, cy + 3, 14, 10)
                End Using
                g.DrawRectangle(Pens.DimGray, x + 7, cy + 3, 14, 10)
                g.DrawString(en.Item2, f, Brushes.Black, x + 26, cy)
                cy += altoLinea
            Next

        End Using

    End Sub

    Private Sub DibujarEjesCoord(g As Graphics)

        Dim ox As Single = 45, oy As Single = _panel.Height - 45
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

    Private Sub DibujarEscala(g As Graphics)

        Dim largo As Double = 1.0
        For Each candidato In {0.5, 1, 2, 5, 10, 20, 50}
            If candidato * _zoom >= 60 Then
                largo = candidato
                Exit For
            End If
        Next

        Dim pix As Single = CSng(largo * _zoom)
        Dim x0 As Single = _panel.Width - 20.0F - pix
        Dim y0 As Single = _panel.Height - 30

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
