Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Shapes

''' <summary>
''' Ventana WPF principal del módulo de Pilotes. Hereda de System.Windows.Window
''' y es invocada desde Form_00_PaginaPrincipal (WinForms) como ShowDialog.
'''
''' El code-behind se limita a wiring del canvas interactivo del editor de dados
''' (zoom, pan, colocación/borrado de pilotes, redibujo). Toda la lógica de
''' negocio vive en el ViewModel y en PiloteService.
''' </summary>
Partial Public Class PiloteOverviewWindow
    Inherits Window

    Private ReadOnly _vm As PiloteOverviewVM

    ' Apoyo resaltado por selección en la tabla Top-10 (halo naranja).
    ' Se repinta en la planta por encima de los dados.
    Private _dadoResaltado As Dado

    ' ── Zoom + pan interactivos sobre la planta ──────────────────
    ' El base scale (auto-fit a los extents de dados) se multiplica por
    ' _zoomPlanta. _panPlantaX/Y son offsets en px aplicados sobre el centro
    ' calculado. Botón "Ajustar vista" resetea a (1, 0, 0).
    Private _zoomPlanta As Double = 1.0
    Private _panPlantaX As Double = 0
    Private _panPlantaY As Double = 0
    Private _enPan As Boolean = False
    Private _panMouseInicioX As Double
    Private _panMouseInicioY As Double
    Private _panOffsetInicioX As Double
    Private _panOffsetInicioY As Double

    ' ── Transformación coordenadas del mundo (metros) → canvas (px) ──
    Private _escala As Double = 100.0              ' px por metro
    Private _offsetX As Double = 0                 ' offset en px, X positivo derecha
    Private _offsetY As Double = 0                 ' offset en px, Y positivo abajo
    Private _centroCanvasX As Double = 0
    Private _centroCanvasY As Double = 0

    ' ── Drag de vértices / pilotes ────────────────────────────────
    Private _arrastrando As Boolean = False
    Private _tipoArrastre As String = ""        ' "vertice" o "pilote"
    Private _indiceArrastre As Integer = -1
    Private Const SNAP_METROS As Double = 0.05  ' snap de drag y clic

    Public Sub New(proyecto As Proyecto)
        InitializeComponent()
        _vm = New PiloteOverviewVM(proyecto)
        DataContext = _vm

        AddHandler _vm.PropertyChanged, AddressOf OnVMPropertyChanged
        AddHandler Loaded, AddressOf OnLoaded
    End Sub

    Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
        ' Escucha también cambios internos del TipoDado seleccionado para repintar
        HookDadoSeleccionado()
        RedibujarCanvas()
    End Sub

    Private Sub OnVMPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
        Select Case e.PropertyName
            Case NameOf(PiloteOverviewVM.TipoDadoSeleccionado)
                HookDadoSeleccionado()
                RedibujarCanvas()
            Case NameOf(PiloteOverviewVM.VistaActiva)
                If _vm.EsDados Then RedibujarCanvas()
                If _vm.EsResultados Then DibujarPlanta()
            Case NameOf(PiloteOverviewVM.NumApoyos),
                 NameOf(PiloteOverviewVM.Top10Criticos),
                 NameOf(PiloteOverviewVM.NumApoyosCumplen),
                 NameOf(PiloteOverviewVM.ModoColorPlanta),
                 NameOf(PiloteOverviewVM.MostrarMallaMetrica),
                 NameOf(PiloteOverviewVM.MostrarEjesETABS)
                ' Después de importar, recalcular o cambiar el modo/toggles,
                ' repintar la planta.
                DibujarPlanta()
        End Select
    End Sub

    Private _hookedDado As TipoDadoVM = Nothing
    Private Sub HookDadoSeleccionado()
        If _hookedDado IsNot Nothing Then
            RemoveHandler _hookedDado.Pilotes.CollectionChanged, AddressOf OnPilotesCollectionChanged
            RemoveHandler _hookedDado.Vertices.CollectionChanged, AddressOf OnVerticesCollectionChanged
            RemoveHandler _hookedDado.GeometriaCambiada, AddressOf OnGeometriaCambiada
        End If
        _hookedDado = _vm.TipoDadoSeleccionado
        If _hookedDado IsNot Nothing Then
            AddHandler _hookedDado.Pilotes.CollectionChanged, AddressOf OnPilotesCollectionChanged
            AddHandler _hookedDado.Vertices.CollectionChanged, AddressOf OnVerticesCollectionChanged
            AddHandler _hookedDado.GeometriaCambiada, AddressOf OnGeometriaCambiada
        End If
    End Sub

    Private Sub OnGeometriaCambiada(sender As Object, e As EventArgs)
        ' Dispara cuando el usuario edita X/Y de un vértice o pilote en la
        ' tabla, o cuando se agrega/borra desde los botones laterales.
        RedibujarCanvas()
        _vm.OnPropertyChangedProxy(NameOf(PiloteOverviewVM.ErroresValidacion))
    End Sub

    Private Sub OnPilotesCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
        RedibujarCanvas()
    End Sub

    Private Sub OnVerticesCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
        RedibujarCanvas()
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' Dibujo del canvas
    ' ─────────────────────────────────────────────────────────────

    Private Sub RedibujarCanvas()
        If CanvasEditor Is Nothing Then Return
        CanvasEditor.Children.Clear()

        Dim dado = _vm.TipoDadoSeleccionado
        If dado Is Nothing Then Return
        If CanvasEditor.ActualWidth <= 0 OrElse CanvasEditor.ActualHeight <= 0 Then Return

        _centroCanvasX = CanvasEditor.ActualWidth * 0.5
        _centroCanvasY = CanvasEditor.ActualHeight * 0.5

        ' Auto-encuadre si es la primera vez
        If _offsetX = 0 AndAlso _offsetY = 0 Then
            AutoEncuadre(dado)
        End If

        DibujarGrilla()
        DibujarPoligono(dado)
        DibujarPilotes(dado)
        DibujarCentroide(dado)
        DibujarEjes()
    End Sub

    Private Sub AutoEncuadre(dado As TipoDadoVM)
        If dado.Modelo.Poligono.Count = 0 Then Return
        Dim xs = dado.Modelo.Poligono.Select(Function(p) p.X)
        Dim ys = dado.Modelo.Poligono.Select(Function(p) p.Y)
        Dim minX = xs.Min(), maxX = xs.Max()
        Dim minY = ys.Min(), maxY = ys.Max()
        Dim anchoM = Math.Max(0.5, maxX - minX) * 1.4
        Dim altoM = Math.Max(0.5, maxY - minY) * 1.4
        Dim escX = CanvasEditor.ActualWidth / anchoM
        Dim escY = CanvasEditor.ActualHeight / altoM
        _escala = Math.Min(escX, escY)
    End Sub

    Private Function MundoACanvasX(xm As Double) As Double
        Return _centroCanvasX + xm * _escala + _offsetX
    End Function

    Private Function MundoACanvasY(ym As Double) As Double
        ' Y del mundo crece hacia arriba; el canvas, hacia abajo.
        Return _centroCanvasY - ym * _escala + _offsetY
    End Function

    Private Function CanvasAMundoX(xp As Double) As Double
        Return (xp - _centroCanvasX - _offsetX) / _escala
    End Function

    Private Function CanvasAMundoY(yp As Double) As Double
        Return -(yp - _centroCanvasY - _offsetY) / _escala
    End Function

    Private Function BuscarBrush(key As String) As Brush
        If Application.Current IsNot Nothing AndAlso Application.Current.Resources.Contains(key) Then
            Return CType(Application.Current.Resources(key), Brush)
        End If
        If Resources.Contains(key) Then Return CType(Resources(key), Brush)
        Return Brushes.Gray
    End Function

    Private Sub DibujarGrilla()
        Dim brushMenor = BuscarBrush("BrushCanvasGrid")
        Dim brushMayor = BuscarBrush("BrushCanvasGridMayor")
        Dim w = CanvasEditor.ActualWidth, h = CanvasEditor.ActualHeight
        Dim paso = 0.5      ' 0.5 m
        Dim pasoMayor = 1.0 ' 1 m

        Dim xMin = CanvasAMundoX(0), xMax = CanvasAMundoX(w)
        Dim yMin = CanvasAMundoY(h), yMax = CanvasAMundoY(0)

        Dim xStart = Math.Floor(xMin / paso) * paso
        Dim xEnd = Math.Ceiling(xMax / paso) * paso
        Dim yStart = Math.Floor(yMin / paso) * paso
        Dim yEnd = Math.Ceiling(yMax / paso) * paso

        Dim x = xStart
        Do While x <= xEnd
            Dim px = MundoACanvasX(x)
            Dim esMayor = Math.Abs(x / pasoMayor - Math.Round(x / pasoMayor)) < 1.0E-6
            Dim l As New Line With {
                .X1 = px, .X2 = px, .Y1 = 0, .Y2 = h,
                .Stroke = If(esMayor, brushMayor, brushMenor),
                .StrokeThickness = If(esMayor, 1.0, 0.5)
            }
            CanvasEditor.Children.Add(l)
            x += paso
        Loop

        Dim y = yStart
        Do While y <= yEnd
            Dim py = MundoACanvasY(y)
            Dim esMayor = Math.Abs(y / pasoMayor - Math.Round(y / pasoMayor)) < 1.0E-6
            Dim l As New Line With {
                .X1 = 0, .X2 = w, .Y1 = py, .Y2 = py,
                .Stroke = If(esMayor, brushMayor, brushMenor),
                .StrokeThickness = If(esMayor, 1.0, 0.5)
            }
            CanvasEditor.Children.Add(l)
            y += paso
        Loop
    End Sub

    Private Sub DibujarEjes()
        Dim brush = BuscarBrush("BrushTextoDeshabilitado")
        Dim y0 = MundoACanvasY(0)
        Dim x0 = MundoACanvasX(0)
        Dim w = CanvasEditor.ActualWidth, h = CanvasEditor.ActualHeight
        If y0 >= 0 AndAlso y0 <= h Then
            CanvasEditor.Children.Add(New Line With {
                .X1 = 0, .X2 = w, .Y1 = y0, .Y2 = y0,
                .Stroke = brush, .StrokeThickness = 1.0,
                .StrokeDashArray = New DoubleCollection({3, 3})
            })
        End If
        If x0 >= 0 AndAlso x0 <= w Then
            CanvasEditor.Children.Add(New Line With {
                .X1 = x0, .X2 = x0, .Y1 = 0, .Y2 = h,
                .Stroke = brush, .StrokeThickness = 1.0,
                .StrokeDashArray = New DoubleCollection({3, 3})
            })
        End If
    End Sub

    Private Sub DibujarPoligono(dado As TipoDadoVM)
        If dado.Modelo.Poligono.Count < 3 Then Return

        Dim pts As New PointCollection()
        For Each v In dado.Modelo.Poligono
            pts.Add(New Point(MundoACanvasX(v.X), MundoACanvasY(v.Y)))
        Next
        Dim poly As New Polygon With {
            .Fill = BuscarBrush("BrushDadoRelleno"),
            .Stroke = BuscarBrush("BrushDadoBorde"),
            .StrokeThickness = 2,
            .StrokeLineJoin = PenLineJoin.Round,
            .Points = pts,
            .Opacity = 0.9
        }
        ' Doble clic en el polígono inserta un vértice sobre la arista más
        ' cercana al punto del click. Útil para pasar de un cuadrado a una L.
        AddHandler poly.MouseLeftButtonDown, AddressOf Poligono_MouseLeftButtonDown
        CanvasEditor.Children.Add(poly)

        ' Marcadores de vértices arrastrables (Tag = índice en la lista).
        For i = 0 To dado.Modelo.Poligono.Count - 1
            Dim v = dado.Modelo.Poligono(i)
            Dim cx = MundoACanvasX(v.X), cy = MundoACanvasY(v.Y)
            Dim el As New Ellipse With {
                .Width = 10, .Height = 10,
                .Fill = BuscarBrush("BrushDadoBorde"),
                .Stroke = Brushes.White, .StrokeThickness = 2,
                .Tag = i,
                .Cursor = Cursors.SizeAll,
                .ToolTip = $"Vértice {i + 1} ({v.X:0.00}, {v.Y:0.00}) m — arrastra para mover, clic derecho para eliminar"
            }
            AddHandler el.MouseLeftButtonDown, AddressOf Vertice_MouseLeftButtonDown
            AddHandler el.MouseRightButtonDown, AddressOf Vertice_MouseRightButtonDown
            Canvas.SetLeft(el, cx - 5)
            Canvas.SetTop(el, cy - 5)
            CanvasEditor.Children.Add(el)
        Next
    End Sub

    Private Sub DibujarPilotes(dado As TipoDadoVM)
        For i = 0 To dado.Modelo.Pilotes.Count - 1
            Dim p = dado.Modelo.Pilotes(i)
            Dim tp = _vm.Modelo.BuscarTipoPilote(p.NombreTipoPilote)
            Dim radioM = If(tp IsNot Nothing, tp.Diametro * 0.5, 0.15)

            Dim cx = MundoACanvasX(p.X_Local), cy = MundoACanvasY(p.Y_Local)
            Dim radioPx = radioM * _escala

            Dim el As New Ellipse With {
                .Width = radioPx * 2, .Height = radioPx * 2,
                .Fill = BuscarBrush("BrushPiloteRelleno"),
                .Stroke = BuscarBrush("BrushPiloteBorde"),
                .StrokeThickness = 2,
                .Tag = i,
                .Cursor = Cursors.SizeAll,
                .ToolTip = $"Pilote {i + 1} ({p.X_Local:0.00}, {p.Y_Local:0.00}) m — arrastra para mover, clic derecho para eliminar"
            }
            AddHandler el.MouseLeftButtonDown, AddressOf Pilote_MouseLeftButtonDown
            AddHandler el.MouseRightButtonDown, AddressOf PiloteBorrar_MouseRightButtonDown
            Canvas.SetLeft(el, cx - radioPx)
            Canvas.SetTop(el, cy - radioPx)
            CanvasEditor.Children.Add(el)

            ' Etiqueta numerada
            Dim txt As New TextBlock With {
                .Text = (i + 1).ToString(),
                .Foreground = Brushes.White,
                .FontFamily = New FontFamily("Segoe UI Semibold"),
                .FontSize = Math.Max(10, Math.Min(14, radioPx * 0.9)),
                .IsHitTestVisible = False
            }
            txt.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            Canvas.SetLeft(txt, cx - txt.DesiredSize.Width * 0.5)
            Canvas.SetTop(txt, cy - txt.DesiredSize.Height * 0.5)
            CanvasEditor.Children.Add(txt)
        Next
    End Sub

    Private Sub DibujarCentroide(dado As TipoDadoVM)
        If dado.Modelo.Pilotes.Count = 0 Then Return
        Dim c = DadoGeometria.CentroideGrupoPilotes(dado.Modelo.Pilotes)
        Dim cx = MundoACanvasX(c.X), cy = MundoACanvasY(c.Y)
        Dim brush = BuscarBrush("BrushCentroideGrupo")

        Dim l1 As New Line With {
            .X1 = cx - 10, .X2 = cx + 10, .Y1 = cy, .Y2 = cy,
            .Stroke = brush, .StrokeThickness = 2.5,
            .StrokeStartLineCap = PenLineCap.Round, .StrokeEndLineCap = PenLineCap.Round
        }
        Dim l2 As New Line With {
            .X1 = cx, .X2 = cx, .Y1 = cy - 10, .Y2 = cy + 10,
            .Stroke = brush, .StrokeThickness = 2.5,
            .StrokeStartLineCap = PenLineCap.Round, .StrokeEndLineCap = PenLineCap.Round
        }
        CanvasEditor.Children.Add(l1)
        CanvasEditor.Children.Add(l2)
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' Interacciones del canvas
    ' ─────────────────────────────────────────────────────────────

    Private Sub CanvasEditor_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        _offsetX = 0 : _offsetY = 0
        RedibujarCanvas()
    End Sub

    Private Sub CanvasEditor_MouseMove(sender As Object, e As MouseEventArgs)
        If CanvasEditor Is Nothing OrElse LblCoords Is Nothing Then Return
        Dim p = e.GetPosition(CanvasEditor)
        Dim mx = CanvasAMundoX(p.X)
        Dim my = CanvasAMundoY(p.Y)
        LblCoords.Text = $"x: {mx:+0.000;-0.000} m    y: {my:+0.000;-0.000} m"

        If _arrastrando AndAlso _vm.TipoDadoSeleccionado IsNot Nothing AndAlso _indiceArrastre >= 0 Then
            Dim mxS = Math.Round(mx / SNAP_METROS) * SNAP_METROS
            Dim myS = Math.Round(my / SNAP_METROS) * SNAP_METROS
            If _tipoArrastre = "vertice" Then
                Dim poli = _vm.TipoDadoSeleccionado.Modelo.Poligono
                If _indiceArrastre < poli.Count Then
                    poli(_indiceArrastre).X = mxS
                    poli(_indiceArrastre).Y = myS
                    RedibujarCanvas()
                End If
            ElseIf _tipoArrastre = "pilote" Then
                Dim pilotes = _vm.TipoDadoSeleccionado.Modelo.Pilotes
                If _indiceArrastre < pilotes.Count Then
                    pilotes(_indiceArrastre).X_Local = mxS
                    pilotes(_indiceArrastre).Y_Local = myS
                    RedibujarCanvas()
                End If
            End If
        End If
    End Sub

    ''' <summary>
    ''' Click izquierdo sobre un vértice. Si es doble clic, se trata como
    ''' intención de borrar vía doble clic (redundante con clic derecho,
    ''' pero WPF no distingue bien doble clic en Ellipse). El click simple
    ''' inicia el arrastre.
    ''' </summary>
    Private Sub Vertice_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        Dim el = TryCast(sender, Ellipse)
        If el Is Nothing OrElse el.Tag Is Nothing Then Return
        Dim idx As Integer
        If Not Integer.TryParse(el.Tag.ToString(), idx) Then Return
        _arrastrando = True
        _tipoArrastre = "vertice"
        _indiceArrastre = idx
        CanvasEditor.CaptureMouse()
        e.Handled = True
    End Sub

    Private Sub Vertice_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        Dim el = TryCast(sender, Ellipse)
        If el Is Nothing OrElse el.Tag Is Nothing Then Return
        Dim idx As Integer
        If Not Integer.TryParse(el.Tag.ToString(), idx) Then Return
        Dim vertices = _vm.TipoDadoSeleccionado.Vertices
        If idx < 0 OrElse idx >= vertices.Count Then Return
        If Not _vm.TipoDadoSeleccionado.BorrarVertice(vertices(idx)) Then
            MessageBox.Show("El polígono necesita al menos 3 vértices.", "No se puede eliminar",
                            MessageBoxButton.OK, MessageBoxImage.Information)
        End If
        RedibujarCanvas()
        e.Handled = True
    End Sub

    Private Sub Pilote_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        Dim el = TryCast(sender, Ellipse)
        If el Is Nothing OrElse el.Tag Is Nothing Then Return
        Dim idx As Integer
        If Not Integer.TryParse(el.Tag.ToString(), idx) Then Return
        _arrastrando = True
        _tipoArrastre = "pilote"
        _indiceArrastre = idx
        CanvasEditor.CaptureMouse()
        e.Handled = True
    End Sub

    ''' <summary>
    ''' Click izquierdo sobre el polígono (no sobre un vértice). Un clic
    ''' simple no hace nada; el doble clic inserta un vértice nuevo sobre
    ''' la arista más cercana al punto, lo que permite pasar un cuadrado a
    ''' una L sin regenerar toda la geometría.
    ''' </summary>
    Private Sub Poligono_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        If e.ClickCount < 2 Then Return

        Dim p = e.GetPosition(CanvasEditor)
        Dim mx = CanvasAMundoX(p.X), my = CanvasAMundoY(p.Y)
        mx = Math.Round(mx / SNAP_METROS) * SNAP_METROS
        my = Math.Round(my / SNAP_METROS) * SNAP_METROS

        Dim poli = _vm.TipoDadoSeleccionado.Modelo.Poligono
        If poli.Count < 3 Then Return

        ' Busca la arista más cercana al punto del click (segmento).
        Dim mejor As Integer = -1
        Dim mejorD As Double = Double.MaxValue
        Dim pxA As Double, pyA As Double
        For i = 0 To poli.Count - 1
            Dim a = poli(i)
            Dim b = poli((i + 1) Mod poli.Count)
            Dim pR = PuntoMasCercanoEnSegmento(mx, my, a.X, a.Y, b.X, b.Y)
            Dim dd = (pR.X - mx) * (pR.X - mx) + (pR.Y - my) * (pR.Y - my)
            If dd < mejorD Then
                mejorD = dd : mejor = i : pxA = pR.X : pyA = pR.Y
            End If
        Next
        If mejor < 0 Then Return
        _vm.TipoDadoSeleccionado.InsertarVertice(mejor, pxA, pyA)
        RedibujarCanvas()
        e.Handled = True
    End Sub

    Private Function PuntoMasCercanoEnSegmento(px As Double, py As Double,
                                               ax As Double, ay As Double,
                                               bx As Double, by As Double) As Point
        Dim dx = bx - ax, dy = by - ay
        Dim L2 = dx * dx + dy * dy
        If L2 < 1.0E-9 Then Return New Point(ax, ay)
        Dim t = ((px - ax) * dx + (py - ay) * dy) / L2
        t = Math.Max(0, Math.Min(1, t))
        Return New Point(ax + t * dx, ay + t * dy)
    End Function

    Private Sub CanvasEditor_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        If Not _arrastrando Then Return
        _arrastrando = False
        _tipoArrastre = ""
        _indiceArrastre = -1
        CanvasEditor.ReleaseMouseCapture()
        _vm.OnPropertyChangedProxy(NameOf(PiloteOverviewVM.ErroresValidacion))
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' Tablas de vértices y pilotes (panel de propiedades)
    ' ─────────────────────────────────────────────────────────────

    Private Sub BtnAgregarVertice_Click(sender As Object, e As RoutedEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        _vm.TipoDadoSeleccionado.AgregarVerticeAlFinal()
    End Sub

    Private Sub BtnBorrarVertice_Click(sender As Object, e As RoutedEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        Dim btn = TryCast(sender, Button)
        Dim vm = TryCast(btn?.DataContext, VerticeVM)
        If vm Is Nothing Then Return
        If Not _vm.TipoDadoSeleccionado.BorrarVertice(vm) Then
            MessageBox.Show("El polígono necesita al menos 3 vértices.", "No se puede eliminar",
                            MessageBoxButton.OK, MessageBoxImage.Information)
        End If
    End Sub

    Private Sub BtnAgregarPilote_Click(sender As Object, e As RoutedEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        Dim nombreTp = _vm.TiposPilote.FirstOrDefault()?.Nombre
        If nombreTp Is Nothing Then
            MessageBox.Show("Primero define al menos un tipo de pilote en la vista 'Pilotes'.",
                            "Sin tipo de pilote", MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If
        _vm.TipoDadoSeleccionado.AgregarPilote(0, 0, nombreTp)
    End Sub

    Private Sub BtnBorrarPiloteFila_Click(sender As Object, e As RoutedEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        Dim btn = TryCast(sender, Button)
        Dim vm = TryCast(btn?.DataContext, PilotePosicionVM)
        If vm Is Nothing Then Return
        _vm.TipoDadoSeleccionado.BorrarPilote(vm)
    End Sub

    ''' <summary>
    ''' Abre el diálogo de generación paramétrica y aplica el resultado al
    ''' TipoDado seleccionado. Alternativa a los 4 generadores hardcoded que
    ''' viven como "Atajos" al lado de este botón.
    ''' </summary>
    Private Sub BtnGenerarParametrico_Click(sender As Object, e As RoutedEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then
            MessageBox.Show("Primero selecciona o crea un tipo de dado.",
                            "Sin dado", MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If
        Dim nombreTp = _vm.TiposPilote.FirstOrDefault()?.Nombre
        If nombreTp Is Nothing Then
            MessageBox.Show("Primero define al menos un tipo de pilote en la vista 'Pilotes'.",
                            "Sin tipo de pilote", MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If
        Dim dlg As New GenerarDadoDialog(nombreTp) With {.Owner = Me}
        If dlg.ShowDialog() = True Then
            _vm.TipoDadoSeleccionado.ReemplazarGeometria(
                dlg.PoligonoResultante, dlg.PilotesResultantes)
            _vm.OnPropertyChangedProxy(NameOf(PiloteOverviewVM.ErroresValidacion))
        End If
    End Sub

    Private Sub CanvasEditor_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return

        Dim p = e.GetPosition(CanvasEditor)
        Dim mx = CanvasAMundoX(p.X)
        Dim my = CanvasAMundoY(p.Y)

        ' Snap a 0.05 m (5 cm)
        Dim snap = 0.05
        mx = Math.Round(mx / snap) * snap
        my = Math.Round(my / snap) * snap

        Dim nombreTp = _vm.TiposPilote.FirstOrDefault()?.Nombre
        If nombreTp Is Nothing Then
            MessageBox.Show("Primero define al menos un tipo de pilote en la vista 'Pilotes'.",
                            "Sin tipo de pilote", MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        _vm.TipoDadoSeleccionado.AgregarPilote(mx, my, nombreTp)
        _vm.OnPropertyChangedProxy(NameOf(PiloteOverviewVM.ErroresValidacion))
    End Sub

    Private Sub PiloteBorrar_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)
        If _vm.TipoDadoSeleccionado Is Nothing Then Return
        Dim el = TryCast(sender, Ellipse)
        If el Is Nothing OrElse el.Tag Is Nothing Then Return
        Dim idx As Integer
        If Not Integer.TryParse(el.Tag.ToString(), idx) Then Return
        If idx < 0 OrElse idx >= _vm.TipoDadoSeleccionado.Pilotes.Count Then Return

        Dim vm = _vm.TipoDadoSeleccionado.Pilotes(idx)
        _vm.TipoDadoSeleccionado.BorrarPilote(vm)
        _vm.OnPropertyChangedProxy(NameOf(PiloteOverviewVM.ErroresValidacion))
        e.Handled = True
    End Sub

    Private Sub CanvasEditor_MouseWheel(sender As Object, e As MouseWheelEventArgs)
        Dim factor = If(e.Delta > 0, 1.15, 1.0 / 1.15)
        Dim p = e.GetPosition(CanvasEditor)
        Dim mx = CanvasAMundoX(p.X), my = CanvasAMundoY(p.Y)
        _escala *= factor
        _escala = Math.Max(10, Math.Min(_escala, 2000))
        ' Mantener el punto bajo el cursor fijo durante el zoom
        Dim nx = MundoACanvasX(mx), ny = MundoACanvasY(my)
        _offsetX += p.X - nx
        _offsetY += p.Y - ny
        RedibujarCanvas()
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' Controles de la top bar
    ' ─────────────────────────────────────────────────────────────

    Private Sub BtnMinimizar_Click(sender As Object, e As RoutedEventArgs)
        WindowState = WindowState.Minimized
    End Sub

    Private Sub BtnMaximizar_Click(sender As Object, e As RoutedEventArgs)
        WindowState = If(WindowState = WindowState.Maximized, WindowState.Normal, WindowState.Maximized)
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' Planta de cimentación (vista Resultados)
    ' ─────────────────────────────────────────────────────────────

    Private Sub CanvasPlanta_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        DibujarPlanta()
    End Sub

    ''' <summary>
    ''' Zoom con rueda del mouse centrado en la posición del cursor: al ampliar
    ''' el zoom aplica un ajuste al pan para que el punto bajo el cursor se
    ''' mantenga fijo. El factor es 1.15 por paso — lo suficiente para que
    ''' algunas vueltas de rueda cubran el rango útil sin sentirse brusco.
    ''' </summary>
    Private Sub CanvasPlanta_MouseWheel(sender As Object, e As MouseWheelEventArgs)
        If CanvasPlanta Is Nothing Then Return
        Dim factor As Double = If(e.Delta > 0, 1.15, 1.0 / 1.15)
        Dim zoomNuevo = _zoomPlanta * factor
        ' Rango útil: 0.2x (vista muy alejada) a 20x (acercado a un pilote).
        zoomNuevo = Math.Max(0.2, Math.Min(20.0, zoomNuevo))
        If Math.Abs(zoomNuevo - _zoomPlanta) < 1.0E-6 Then Return

        ' Mantener fijo el punto bajo el cursor ajustando el pan. Como el
        ' resto del offset se recalcula dentro de DibujarPlanta, basta con
        ' actualizar _panPlantaX/Y según el delta de escala.
        Dim p = e.GetPosition(CanvasPlanta)
        Dim delta = zoomNuevo / _zoomPlanta - 1.0
        Dim cx = CanvasPlanta.ActualWidth * 0.5
        Dim cy = CanvasPlanta.ActualHeight * 0.5
        _panPlantaX -= (p.X - cx - _panPlantaX) * delta
        _panPlantaY -= (p.Y - cy - _panPlantaY) * delta

        _zoomPlanta = zoomNuevo
        DibujarPlanta()
        e.Handled = True
    End Sub

    Private Sub CanvasPlanta_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)
        If CanvasPlanta Is Nothing Then Return
        _enPan = True
        Dim p = e.GetPosition(CanvasPlanta)
        _panMouseInicioX = p.X
        _panMouseInicioY = p.Y
        _panOffsetInicioX = _panPlantaX
        _panOffsetInicioY = _panPlantaY
        CanvasPlanta.CaptureMouse()
        CanvasPlanta.Cursor = Cursors.SizeAll
        e.Handled = True
    End Sub

    Private Sub CanvasPlanta_MouseMove(sender As Object, e As MouseEventArgs)
        If Not _enPan OrElse CanvasPlanta Is Nothing Then Return
        Dim p = e.GetPosition(CanvasPlanta)
        _panPlantaX = _panOffsetInicioX + (p.X - _panMouseInicioX)
        _panPlantaY = _panOffsetInicioY + (p.Y - _panMouseInicioY)
        DibujarPlanta()
    End Sub

    Private Sub CanvasPlanta_MouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
        If Not _enPan Then Return
        _enPan = False
        If CanvasPlanta IsNot Nothing Then
            CanvasPlanta.ReleaseMouseCapture()
            CanvasPlanta.Cursor = Cursors.Arrow
        End If
        e.Handled = True
    End Sub

    ''' <summary>
    ''' Reset de zoom y pan — vuelve al auto-fit inicial. Se dispara desde
    ''' el botón "Ajustar vista" del toolbar.
    ''' </summary>
    Private Sub BtnAjustarVista_Click(sender As Object, e As RoutedEventArgs)
        _zoomPlanta = 1.0
        _panPlantaX = 0
        _panPlantaY = 0
        DibujarPlanta()
    End Sub

    ''' <summary>
    ''' Dibuja todos los apoyos del proyecto en planta, coloreados por C/D y
    ''' diferenciados por forma (círculo = Frame, cuadrado = Pier). Auto-encuadra
    ''' para que todos quepan con margen.
    '''
    ''' Capas de abajo hacia arriba (orden de inserción en Canvas): malla métrica,
    ''' GridLines de ETABS, edificio (columnas/muros), dados con pilotes, labels.
    ''' </summary>
    Private Sub DibujarPlanta()
        If CanvasPlanta Is Nothing Then Return
        CanvasPlanta.Children.Clear()
        If _vm Is Nothing Then Return
        Dim dados = _vm.Modelo.Dados
        If dados Is Nothing OrElse dados.Count = 0 Then Return

        Dim w As Double = CanvasPlanta.ActualWidth
        Dim h As Double = CanvasPlanta.ActualHeight
        If w < 50 OrElse h < 50 Then Return

        ' Extents iniciales desde los dados. El edificio (columnas/muros) suele
        ' caer dentro de la nube de dados — si algún elemento queda fuera el
        ' Border exterior tiene ClipToBounds y se recorta sin romper la vista.
        Dim minX = dados.Min(Function(d) d.X_Global)
        Dim maxX = dados.Max(Function(d) d.X_Global)
        Dim minY = dados.Min(Function(d) d.Y_Global)
        Dim maxY = dados.Max(Function(d) d.Y_Global)

        Dim spanX = Math.Max(1.0, maxX - minX)
        Dim spanY = Math.Max(1.0, maxY - minY)

        ' Margen de 10% a cada lado (base scale)
        Dim margen As Double = 0.1
        Dim escalaX = w / (spanX * (1 + 2 * margen))
        Dim escalaY = h / (spanY * (1 + 2 * margen))
        Dim escalaBase = Math.Min(escalaX, escalaY)

        ' Zoom del usuario (rueda del mouse) sobre el base scale
        Dim escala = escalaBase * _zoomPlanta

        ' Offsets para centrar + pan del usuario
        Dim offsetX = (w - spanX * escala) * 0.5 - minX * escala + _panPlantaX
        Dim offsetY = (h - spanY * escala) * 0.5 + maxY * escala + _panPlantaY  ' Y invertida: mundo+ = arriba

        ' Transformaciones
        Dim mundoACanvasX = Function(xm As Double) xm * escala + offsetX
        Dim mundoACanvasY = Function(ym As Double) -ym * escala + offsetY

        ' ── 1. Malla métrica dashed cada 1 m ──────────────────────────
        If _vm.MostrarMallaMetrica Then
            DibujarMallaMetrica(w, h, minX, maxX, minY, maxY, mundoACanvasX, mundoACanvasY)
        End If

        ' ── 2. GridLines de ETABS (A, B, 1, 2, …) ─────────────────────
        If _vm.MostrarEjesETABS Then
            DibujarGridLinesETABS(w, h, minX, maxX, minY, maxY, mundoACanvasX, mundoACanvasY)
        End If

        ' ── 3. Edificio: columnas y muros bajo los dados ──────────────
        DibujarContextoEdificio(escala, mundoACanvasX, mundoACanvasY)

        ' ── 4. Dados + pilotes por apoyo ─────────────────────────────────
        ' Si el apoyo tiene TipoDado asignado: se dibuja el polígono del dado
        ' trasladado a (X_Global, Y_Global) y rotado AnguloPlanta grados, con
        ' los pilotes como pequeños círculos dentro. Color del dado por C/D.
        ' Si no hay TipoDado: fallback a un marcador compacto como antes.
        Dim radioPxFallback As Double = Math.Max(8, Math.Min(18, escala * 0.3))

        For Each d As Dado In dados
            Dim cx = mundoACanvasX(d.X_Global)
            Dim cy = mundoACanvasY(d.Y_Global)
            Dim brush = ColorDePorEstado(d)
            Dim td = _vm.Modelo.BuscarTipoDado(d.NombreTipoDado)

            Dim esPier = (d.SourceType IsNot Nothing AndAlso
                          d.SourceType.Equals("Pier", StringComparison.OrdinalIgnoreCase))

            If td IsNot Nothing AndAlso td.Poligono IsNot Nothing AndAlso td.Poligono.Count >= 3 Then
                ' Polígono real del dado trasladado al apoyo y rotado
                Dim rad = d.AnguloPlanta * Math.PI / 180.0
                Dim cs = Math.Cos(rad), sn = Math.Sin(rad)

                Dim pts As New PointCollection()
                For Each v In td.Poligono
                    Dim xPoly = v.X * cs - v.Y * sn + d.X_Global
                    Dim yPoly = v.X * sn + v.Y * cs + d.Y_Global
                    pts.Add(New Point(mundoACanvasX(xPoly), mundoACanvasY(yPoly)))
                Next

                Dim poly As New Polygon With {
                    .Points = pts,
                    .Fill = brush,
                    .Stroke = BuscarBrush("BrushDadoBorde"),
                    .StrokeThickness = 1.3,
                    .StrokeLineJoin = PenLineJoin.Round,
                    .Opacity = 0.85,
                    .Tag = d,
                    .Cursor = Cursors.Hand
                }
                AddHandler poly.MouseEnter, AddressOf ApoyoShape_MouseEnter
                AddHandler poly.MouseLeave, AddressOf ApoyoShape_MouseLeave
                AddHandler poly.MouseLeftButtonDown, AddressOf ApoyoShape_MouseLeftButtonDown
                CanvasPlanta.Children.Add(poly)

                ' Pilotes dentro: solo si caben visualmente (≥ 2 px de radio)
                Dim radioPilotePx As Double = 0
                For Each pp In td.Pilotes
                    Dim tp = _vm.Modelo.BuscarTipoPilote(pp.NombreTipoPilote)
                    Dim radioM = If(tp IsNot Nothing, tp.Diametro * 0.5, 0.15)
                    radioPilotePx = radioM * escala
                    If radioPilotePx < 1.5 Then radioPilotePx = 1.5

                    Dim xPil = pp.X_Local * cs - pp.Y_Local * sn + d.X_Global
                    Dim yPil = pp.X_Local * sn + pp.Y_Local * cs + d.Y_Global
                    Dim px = mundoACanvasX(xPil)
                    Dim py = mundoACanvasY(yPil)

                    Dim el As New Ellipse With {
                        .Width = radioPilotePx * 2,
                        .Height = radioPilotePx * 2,
                        .Fill = BuscarBrush("BrushPiloteRelleno"),
                        .Stroke = Brushes.White,
                        .StrokeThickness = 0.8,
                        .IsHitTestVisible = False
                    }
                    Canvas.SetLeft(el, px - radioPilotePx)
                    Canvas.SetTop(el, py - radioPilotePx)
                    CanvasPlanta.Children.Add(el)
                Next
            Else
                ' Fallback: sin tipo asignado, marcador compacto (gris)
                Dim shape As Shape
                If esPier Then
                    shape = New Rectangle With {
                        .Width = radioPxFallback * 1.7, .Height = radioPxFallback * 1.7,
                        .Fill = brush, .Stroke = Brushes.White, .StrokeThickness = 2
                    }
                    Canvas.SetLeft(shape, cx - radioPxFallback * 0.85)
                    Canvas.SetTop(shape, cy - radioPxFallback * 0.85)
                Else
                    shape = New Ellipse With {
                        .Width = radioPxFallback * 1.8, .Height = radioPxFallback * 1.8,
                        .Fill = brush, .Stroke = Brushes.White, .StrokeThickness = 2
                    }
                    Canvas.SetLeft(shape, cx - radioPxFallback * 0.9)
                    Canvas.SetTop(shape, cy - radioPxFallback * 0.9)
                End If
                shape.Tag = d
                shape.Cursor = Cursors.Hand
                AddHandler shape.MouseEnter, AddressOf ApoyoShape_MouseEnter
                AddHandler shape.MouseLeave, AddressOf ApoyoShape_MouseLeave
                AddHandler shape.MouseLeftButtonDown, AddressOf ApoyoShape_MouseLeftButtonDown
                CanvasPlanta.Children.Add(shape)
            End If

            ' Label del joint cerca del centro del apoyo
            Dim lbl As New TextBlock With {
                .Text = d.JointLabel,
                .FontSize = 10,
                .FontFamily = New FontFamily("Segoe UI Semibold"),
                .Foreground = BuscarBrush("BrushTextoPrincipal"),
                .IsHitTestVisible = False
            }
            lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            ' Posicionar la etiqueta ligeramente arriba del apoyo para que no tape el dado
            Canvas.SetLeft(lbl, cx - lbl.DesiredSize.Width * 0.5)
            Canvas.SetTop(lbl, cy - lbl.DesiredSize.Height - 3)
            CanvasPlanta.Children.Add(lbl)
        Next

        ' Halo del apoyo resaltado (viene de selección en Top-10). Se dibuja
        ' por encima de todos los dados para que quede visible aunque el apoyo
        ' esté dentro de un cluster denso.
        If _dadoResaltado IsNot Nothing Then
            Dim hcx = mundoACanvasX(_dadoResaltado.X_Global)
            Dim hcy = mundoACanvasY(_dadoResaltado.Y_Global)
            Dim halo As New Ellipse With {
                .Width = 44, .Height = 44,
                .Stroke = New SolidColorBrush(Color.FromRgb(242, 169, 59)),
                .StrokeThickness = 3,
                .Fill = Brushes.Transparent,
                .IsHitTestVisible = False
            }
            Canvas.SetLeft(halo, hcx - 22)
            Canvas.SetTop(halo, hcy - 22)
            CanvasPlanta.Children.Add(halo)
        End If

        ' ── 5. Barra de escala ───────────────────────────────────────────
        Dim escalaMetros As Double = 1.0
        If escala * escalaMetros < 40 Then escalaMetros = 5.0
        If escala * escalaMetros < 40 Then escalaMetros = 10.0
        Dim xBarra = 20.0
        Dim yBarra = h - 24.0
        Dim lenBarra = escala * escalaMetros
        CanvasPlanta.Children.Add(New Line With {
            .X1 = xBarra, .X2 = xBarra + lenBarra,
            .Y1 = yBarra, .Y2 = yBarra,
            .Stroke = BuscarBrush("BrushTextoPrincipal"),
            .StrokeThickness = 2
        })
        CanvasPlanta.Children.Add(New TextBlock With {
            .Text = $"{escalaMetros:0} m",
            .FontSize = 11, .Foreground = BuscarBrush("BrushTextoSecundario")
        })
        Dim txtEsc = TryCast(CanvasPlanta.Children(CanvasPlanta.Children.Count - 1), TextBlock)
        If txtEsc IsNot Nothing Then
            Canvas.SetLeft(txtEsc, xBarra + lenBarra / 2 - 10)
            Canvas.SetTop(txtEsc, yBarra + 4)
        End If
    End Sub

    ''' <summary>
    ''' Capa 1: malla cartesiana cada 1 m, dashed, muy tenue. Sirve de referencia
    ''' visual mientras no haya grid de ETABS importado.
    ''' </summary>
    Private Sub DibujarMallaMetrica(w As Double, h As Double,
                                    minX As Double, maxX As Double,
                                    minY As Double, maxY As Double,
                                    mundoACanvasX As Func(Of Double, Double),
                                    mundoACanvasY As Func(Of Double, Double))
        Dim brushGrid = BuscarBrush("BrushCanvasGrid")
        Dim xg = Math.Floor(minX)
        Do While xg <= Math.Ceiling(maxX)
            Dim px = mundoACanvasX(xg)
            CanvasPlanta.Children.Add(New Line With {
                .X1 = px, .X2 = px, .Y1 = 0, .Y2 = h,
                .Stroke = brushGrid, .StrokeThickness = 0.5,
                .StrokeDashArray = New DoubleCollection({2, 3})
            })
            xg += 1
        Loop
        Dim yg = Math.Floor(minY)
        Do While yg <= Math.Ceiling(maxY)
            Dim py = mundoACanvasY(yg)
            CanvasPlanta.Children.Add(New Line With {
                .X1 = 0, .X2 = w, .Y1 = py, .Y2 = py,
                .Stroke = brushGrid, .StrokeThickness = 0.5,
                .StrokeDashArray = New DoubleCollection({2, 3})
            })
            yg += 1
        Loop
    End Sub

    ''' <summary>
    ''' Capa 2: líneas de eje de ETABS (A, B, 1, 2, …) con sus bubbles en
    ''' AMBOS extremos. Tres direcciones:
    '''   "X" = X Cartesian  → línea vertical,  usa Ordinate (X constante).
    '''   "Y" = Y Cartesian  → línea horizontal, usa Ordinate (Y constante).
    '''   "G" = General      → segmento libre, usa (X1,Y1)–(X2,Y2).
    '''
    ''' Convención de un plano estructural: las ejes XY cartesianas se
    ''' etiquetan en los dos extremos para que sean legibles desde cualquier
    ''' borde de la planta (lo que `BubbleLocation` dice solo gobierna cuál es
    ''' el "preferido" en ETABS, no limita la visualización). Para los ejes
    ''' General se respeta la posición original.
    ''' </summary>
    Private Sub DibujarGridLinesETABS(w As Double, h As Double,
                                      minX As Double, maxX As Double,
                                      minY As Double, maxY As Double,
                                      mundoACanvasX As Func(Of Double, Double),
                                      mundoACanvasY As Func(Of Double, Double))
        If _vm Is Nothing OrElse _vm.Proyecto Is Nothing Then Return
        Dim grids = _vm.Proyecto.Elementos?.Grids?.GridLines
        If grids Is Nothing OrElse grids.Count = 0 Then Return

        ' Dos tonos para distinguir ejes que corren en X (verticales, típicos
        ' "1, 2, 3") de los que corren en Y (horizontales, típicos "A, B, C").
        ' El contraste visual acelera la lectura del plano.
        Dim brushLineaX As Brush = New SolidColorBrush(Color.FromArgb(220, 110, 130, 170))
        Dim brushLineaY As Brush = New SolidColorBrush(Color.FromArgb(220, 155, 115, 155))
        Dim brushBubbleFondo As Brush = New SolidColorBrush(Color.FromArgb(240, 255, 255, 255))
        Dim brushBubbleTexto As Brush = New SolidColorBrush(Color.FromRgb(45, 60, 95))

        ' Rango extendido para que la línea cruce todo el canvas aunque el
        ' Ordinate del grid caiga fuera del extents de dados.
        Dim ext As Double = 3.0
        Dim xMin = minX - ext, xMax = maxX + ext
        Dim yMin = minY - ext, yMax = maxY + ext

        For Each gl In grids
            If Not gl.Visible Then Continue For

            Dim x1Px As Double, x2Px As Double, y1Px As Double, y2Px As Double
            Dim esX As Boolean = (gl.Direction = "X")
            Dim brushLinea As Brush = If(esX, brushLineaX, brushLineaY)

            If gl.EsTipoGeneral Then
                x1Px = mundoACanvasX(gl.X1) : y1Px = mundoACanvasY(gl.Y1)
                x2Px = mundoACanvasX(gl.X2) : y2Px = mundoACanvasY(gl.Y2)
                brushLinea = brushLineaY   ' General toma la paleta Y (neutral)
            ElseIf esX Then
                ' Línea vertical en X=Ordinate
                x1Px = mundoACanvasX(gl.Ordinate) : y1Px = mundoACanvasY(yMax)
                x2Px = mundoACanvasX(gl.Ordinate) : y2Px = mundoACanvasY(yMin)
            Else
                ' Dirección Y: línea horizontal en Y=Ordinate
                x1Px = mundoACanvasX(xMin) : y1Px = mundoACanvasY(gl.Ordinate)
                x2Px = mundoACanvasX(xMax) : y2Px = mundoACanvasY(gl.Ordinate)
            End If

            Dim linea As New Line With {
                .X1 = x1Px, .Y1 = y1Px, .X2 = x2Px, .Y2 = y2Px,
                .Stroke = brushLinea, .StrokeThickness = 1.1,
                .StrokeDashArray = New DoubleCollection({5, 4})
            }
            CanvasPlanta.Children.Add(linea)

            If String.IsNullOrEmpty(gl.GridID) Then Continue For

            ' Bubbles en AMBOS extremos para ejes X/Y; solo en el extremo
            ' preferido para ejes General (donde el ángulo importa).
            DibujarBubbleEje(gl.GridID, x1Px, y1Px, brushLinea, brushBubbleFondo, brushBubbleTexto, w, h)
            If Not gl.EsTipoGeneral Then
                DibujarBubbleEje(gl.GridID, x2Px, y2Px, brushLinea, brushBubbleFondo, brushBubbleTexto, w, h)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Un bubble: círculo blanco translúcido con el GridID centrado. No
    ''' dibuja si cae afuera del canvas (margen ±20 px) para no acumular
    ''' elementos invisibles que ralentizan el repintado.
    ''' </summary>
    Private Sub DibujarBubbleEje(gridID As String, cx As Double, cy As Double,
                                 brushLinea As Brush, brushFondo As Brush, brushTexto As Brush,
                                 w As Double, h As Double)
        If cx < -20 OrElse cx > w + 20 OrElse cy < -20 OrElse cy > h + 20 Then Return

        Dim radio As Double = 12
        Dim bubble As New Ellipse With {
            .Width = radio * 2, .Height = radio * 2,
            .Fill = brushFondo,
            .Stroke = brushLinea, .StrokeThickness = 1.3
        }
        Canvas.SetLeft(bubble, cx - radio)
        Canvas.SetTop(bubble, cy - radio)
        CanvasPlanta.Children.Add(bubble)

        Dim lbl As New TextBlock With {
            .Text = gridID,
            .FontSize = 11,
            .FontFamily = New FontFamily("Segoe UI Semibold"),
            .Foreground = brushTexto,
            .IsHitTestVisible = False
        }
        lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
        Canvas.SetLeft(lbl, cx - lbl.DesiredSize.Width * 0.5)
        Canvas.SetTop(lbl, cy - lbl.DesiredSize.Height * 0.5)
        CanvasPlanta.Children.Add(lbl)
    End Sub

    ''' <summary>
    ''' Capa 3: huella de columnas y muros del edificio, pintados en gris claro
    ''' semi-transparente ANTES de los dados. Da contexto estructural — el
    ''' ingeniero ve qué elemento vertical descansa sobre cada dado.
    '''
    ''' Columnas: rectángulo B_Plano × H_Plano rotado por AnguloOrientacion
    ''' centrado en (Coordenadas.X, Coordenadas.Y) del tramo base.
    ''' Muros: rectángulo alineado al segmento Xi,Yi → Xf,Yf con grueso tw_Planos
    ''' de la SeccionMuro con Z más bajo (piso base).
    ''' </summary>
    Private Sub DibujarContextoEdificio(escala As Double,
                                        mundoACanvasX As Func(Of Double, Double),
                                        mundoACanvasY As Func(Of Double, Double))
        If _vm Is Nothing OrElse _vm.Proyecto Is Nothing Then Return
        Dim elementos = _vm.Proyecto.Elementos
        If elementos Is Nothing Then Return

        Dim brushRelleno As Brush = New SolidColorBrush(Color.FromArgb(90, 110, 125, 145))
        Dim brushBorde As Brush = New SolidColorBrush(Color.FromArgb(180, 90, 100, 115))

        ' ── Columnas (tramo base por piso con coordenadas) ────────────
        If elementos.Columnas IsNot Nothing AndAlso elementos.Columnas.Lista_Columnas IsNot Nothing Then
            For Each col In elementos.Columnas.Lista_Columnas
                Dim coords = col.Coordenadas
                If coords Is Nothing Then Continue For
                If col.Lista_Tramos_Columnas Is Nothing OrElse col.Lista_Tramos_Columnas.Count = 0 Then Continue For

                ' Tramo base: el primero. B_Plano/H_Plano en metros; si vienen 0,
                ' se cae a un marcador mínimo de 30 cm para que la columna no
                ' desaparezca.
                Dim tramo = col.Lista_Tramos_Columnas(0)
                Dim b As Double = If(tramo.B_Plano > 0, CDbl(tramo.B_Plano), 0.3)
                Dim hCol As Double = If(tramo.H_Plano > 0, CDbl(tramo.H_Plano), 0.3)
                Dim ang As Double = CDbl(tramo.AnguloOrientacion)

                DibujarRectanguloRotado(coords.X, coords.Y, b, hCol, ang,
                                        brushRelleno, brushBorde,
                                        mundoACanvasX, mundoACanvasY)
            Next
        End If

        ' ── Muros (SeccionMuro con Z mínimo = piso base) ──────────────
        If elementos.Muros IsNot Nothing AndAlso elementos.Muros.Lista_Muros IsNot Nothing Then
            For Each muro In elementos.Muros.Lista_Muros
                If muro.Lista_Secciones Is Nothing OrElse muro.Lista_Secciones.Count = 0 Then Continue For

                ' Sección base: la de menor Zi (por si hay varios pisos). Un muro
                ' sin coordenadas no dibuja.
                Dim secBase = muro.Lista_Secciones.OrderBy(Function(s) s.Coordenadas?.Zi).FirstOrDefault()
                If secBase Is Nothing OrElse secBase.Coordenadas Is Nothing Then Continue For

                Dim tw As Double = If(secBase.tw_Planos > 0, CDbl(secBase.tw_Planos), 0.2)
                DibujarSegmentoMuro(secBase.Coordenadas.Xi, secBase.Coordenadas.Yi,
                                    secBase.Coordenadas.Xf, secBase.Coordenadas.Yf,
                                    tw, brushRelleno, brushBorde,
                                    mundoACanvasX, mundoACanvasY)
            Next
        End If
    End Sub

    ''' <summary>
    ''' Pinta un rectángulo (b×h) en coordenadas mundo (metros) centrado en
    ''' (xc, yc), rotado `anguloGrados` grados antihorarios. Se usa para la
    ''' huella de columnas rectangulares.
    ''' </summary>
    Private Sub DibujarRectanguloRotado(xc As Double, yc As Double,
                                        b As Double, h As Double,
                                        anguloGrados As Double,
                                        relleno As Brush, borde As Brush,
                                        mundoACanvasX As Func(Of Double, Double),
                                        mundoACanvasY As Func(Of Double, Double))
        Dim rad = anguloGrados * Math.PI / 180.0
        Dim cs = Math.Cos(rad), sn = Math.Sin(rad)
        Dim hb = b * 0.5, hh = h * 0.5
        Dim xs() As Double = {-hb, hb, hb, -hb}
        Dim ys() As Double = {-hh, -hh, hh, hh}

        Dim pts As New PointCollection()
        For i = 0 To 3
            Dim xm = xc + xs(i) * cs - ys(i) * sn
            Dim ym = yc + xs(i) * sn + ys(i) * cs
            pts.Add(New Point(mundoACanvasX(xm), mundoACanvasY(ym)))
        Next

        Dim poly As New Polygon With {
            .Points = pts,
            .Fill = relleno,
            .Stroke = borde,
            .StrokeThickness = 0.8,
            .IsHitTestVisible = False
        }
        CanvasPlanta.Children.Add(poly)
    End Sub

    ''' <summary>
    ''' Pinta un segmento de muro como rectángulo alineado al vector Xi,Yi→Xf,Yf
    ''' con grueso `tw` (metros) perpendicular. Si el segmento es degenerado
    ''' (longitud despreciable), no dibuja.
    ''' </summary>
    Private Sub DibujarSegmentoMuro(xi As Double, yi As Double,
                                    xf As Double, yf As Double,
                                    tw As Double,
                                    relleno As Brush, borde As Brush,
                                    mundoACanvasX As Func(Of Double, Double),
                                    mundoACanvasY As Func(Of Double, Double))
        Dim dx = xf - xi, dy = yf - yi
        Dim lw = Math.Sqrt(dx * dx + dy * dy)
        If lw < 0.05 Then Return

        ' Normal unitaria al eje del muro
        Dim nx = -dy / lw, ny = dx / lw
        Dim t2 = tw * 0.5

        Dim pts As New PointCollection()
        pts.Add(New Point(mundoACanvasX(xi + nx * t2), mundoACanvasY(yi + ny * t2)))
        pts.Add(New Point(mundoACanvasX(xf + nx * t2), mundoACanvasY(yf + ny * t2)))
        pts.Add(New Point(mundoACanvasX(xf - nx * t2), mundoACanvasY(yf - ny * t2)))
        pts.Add(New Point(mundoACanvasX(xi - nx * t2), mundoACanvasY(yi - ny * t2)))

        Dim poly As New Polygon With {
            .Points = pts,
            .Fill = relleno,
            .Stroke = borde,
            .StrokeThickness = 0.8,
            .IsHitTestVisible = False
        }
        CanvasPlanta.Children.Add(poly)
    End Sub

    ''' <summary>
    ''' Pinta cada apoyo según el modo de color activo del VM. Modos:
    '''   CDMin        (default) – peor C/D entre compresión y tracción.
    '''   CDCompresion – solo la relación en compresión.
    '''   CDTraccion   – solo la relación en tracción (muchos apoyos en N/A).
    '''   CDSTM        – peor C/D del modelo puntal-tensor (puntal/tensores/nodo).
    '''   PuMax        – gradiente azul por magnitud de Pu máximo.
    '''   TipoDado     – color categórico por NombreTipoDado (paleta cíclica).
    ''' Sin tipo asignado ⇒ gris N/A siempre, salvo en modo TipoDado donde
    ''' el gris significa "sin tipo" también.
    ''' </summary>
    Private Function ColorDePorEstado(d As Dado) As Brush
        If String.IsNullOrEmpty(d.NombreTipoDado) Then Return BuscarBrush("BrushNoAplica")

        Dim modo As String = If(_vm IsNot Nothing, _vm.ModoColorPlanta, "CDMin")

        Select Case modo
            Case "CDCompresion"
                ' Peor entre geotécnico y estructural para el lado de compresión.
                Return ColorPorCD(MenorPositivo(d.CD_Compresion, d.CD_Compresion_Estructural))
            Case "CDTraccion"
                Return ColorPorCD(MenorPositivo(d.CD_Traccion, d.CD_Traccion_Estructural))
            Case "CDSTM"
                Return ColorPorCD(PeorCDSTM(d))
            Case "PuMax"
                Return ColorPorPuMax(d.Pu_Pilote_Max)
            Case "TipoDado"
                Return ColorPorTipoDado(d.NombreTipoDado)
            Case Else
                ' Modo CDMin: peor C/D entre los 4 chequeos (ignora los = 0).
                Return ColorPorCD(PeorCDActiva(d))
        End Select
    End Function

    ''' <summary>
    ''' Peor C/D entre los cuatro chequeos STM (puntal, nodo, tensor X, tensor Y),
    ''' ignorando los que vienen con sentinela 0 = "no aplica / no evaluado".
    ''' Devuelve 0 si ninguno aplica — se interpreta como "N/A" (gris en planta).
    ''' </summary>
    Private Shared Function PeorCDSTM(d As Dado) As Double
        Dim valores As New List(Of Double)
        If d.CD_STM_Puntal > 0 Then valores.Add(d.CD_STM_Puntal)
        If d.CD_STM_Nodo > 0 Then valores.Add(d.CD_STM_Nodo)
        If d.CD_STM_Tensor_X > 0 Then valores.Add(d.CD_STM_Tensor_X)
        If d.CD_STM_Tensor_Y > 0 Then valores.Add(d.CD_STM_Tensor_Y)
        If valores.Count = 0 Then Return 0
        Return valores.Min()
    End Function

    ''' <summary>
    ''' Peor C/D entre los cuatro chequeos (geot comp, geot tracc, estr comp,
    ''' estr tracc), ignorando los que vienen con sentinela 0 = "no aplica".
    ''' Devuelve 0 si ninguno aplica.
    ''' </summary>
    Private Shared Function PeorCDActiva(d As Dado) As Double
        Dim valores As New List(Of Double)
        If d.CD_Compresion > 0 Then valores.Add(d.CD_Compresion)
        If d.CD_Traccion > 0 Then valores.Add(d.CD_Traccion)
        If d.CD_Compresion_Estructural > 0 Then valores.Add(d.CD_Compresion_Estructural)
        If d.CD_Traccion_Estructural > 0 Then valores.Add(d.CD_Traccion_Estructural)
        If valores.Count = 0 Then Return 0
        Return valores.Min()
    End Function

    ''' <summary>
    ''' Menor valor positivo entre dos C/D (ignora los 0 = "no aplica"). Útil
    ''' para colapsar geot y estr en un solo valor "efectivo" por dirección.
    ''' </summary>
    Private Shared Function MenorPositivo(a As Double, b As Double) As Double
        If a > 0 AndAlso b > 0 Then Return Math.Min(a, b)
        If a > 0 Then Return a
        If b > 0 Then Return b
        Return 0
    End Function

    ''' <summary>Semáforo C/D con sentinela 0 = "no aplica" (gris).</summary>
    Private Function ColorPorCD(cd As Double) As Brush
        If cd <= 0 Then Return BuscarBrush("BrushNoAplica")
        If cd < 0.9 Then Return BuscarBrush("BrushNoCumple")
        If cd < 1.0 Then Return BuscarBrush("BrushAdvertencia")
        Return BuscarBrush("BrushCumple")
    End Function

    ''' <summary>
    ''' Gradiente azul por Pu máximo. Rango [0, max] se normaliza al dominio
    ''' observado en el conjunto para que escale por proyecto en lugar de usar
    ''' un umbral absoluto que no significa nada entre edificios distintos.
    ''' </summary>
    Private Function ColorPorPuMax(pu As Double) As Brush
        If _vm Is Nothing OrElse _vm.Modelo Is Nothing Then Return BuscarBrush("BrushNoAplica")
        If pu <= 0 Then Return BuscarBrush("BrushNoAplica")

        Dim maxPu As Double = 1
        For Each d In _vm.Modelo.Dados
            If d.Pu_Pilote_Max > maxPu Then maxPu = d.Pu_Pilote_Max
        Next
        Dim t = Math.Min(1.0, Math.Max(0.0, pu / maxPu))
        ' Interpolación linear #BFD5E8 → #1F5B8C (azul claro a oscuro).
        ' VB.NET usa &H para hex, no 0x — lo mantenemos como decimales explícitos.
        Dim r = CByte(191 + (31 - 191) * t)
        Dim g = CByte(213 + (91 - 213) * t)
        Dim b = CByte(232 + (140 - 232) * t)
        Return New SolidColorBrush(Color.FromRgb(r, g, b))
    End Function

    ' Paleta cíclica para categorías (Tipo de dado). 10 colores distinguibles,
    ' evitando el rojo/verde/amarillo que están reservados para el semáforo C/D.
    Private Shared ReadOnly _paletaCategorias As Color() = {
        Color.FromRgb(50, 115, 168),
        Color.FromRgb(132, 94, 194),
        Color.FromRgb(230, 111, 47),
        Color.FromRgb(26, 141, 139),
        Color.FromRgb(192, 90, 141),
        Color.FromRgb(80, 130, 90),
        Color.FromRgb(201, 141, 56),
        Color.FromRgb(126, 123, 180),
        Color.FromRgb(70, 160, 185),
        Color.FromRgb(170, 85, 85)
    }

    Private Function ColorPorTipoDado(nombre As String) As Brush
        If String.IsNullOrEmpty(nombre) Then Return BuscarBrush("BrushNoAplica")
        If _vm Is Nothing OrElse _vm.Modelo Is Nothing Then Return BuscarBrush("BrushNoAplica")
        ' Índice estable: posición del TipoDado en el catálogo del contenedor.
        Dim idx = _vm.Modelo.TiposDado.FindIndex(
            Function(t) t.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))
        If idx < 0 Then Return BuscarBrush("BrushNoAplica")
        Return New SolidColorBrush(_paletaCategorias(idx Mod _paletaCategorias.Length))
    End Function

    Private Sub ApoyoShape_MouseEnter(sender As Object, e As MouseEventArgs)
        Dim shape = TryCast(sender, Shape)
        If shape Is Nothing OrElse LblInfoPlanta Is Nothing Then Return
        Dim d = TryCast(shape.Tag, Dado)
        If d Is Nothing Then Return

        Dim tipo = If(String.IsNullOrEmpty(d.SourceType), "—", d.SourceType)
        Dim dado = If(String.IsNullOrEmpty(d.NombreTipoDado), "sin tipo", d.NombreTipoDado)
        Dim fmt = Function(v As Double) If(v > 0, v.ToString("0.00"), "—")

        Dim linea1 = $"{d.JointLabel} ({tipo})   dado: {dado}"
        Dim linea2 = $"Geot: comp {fmt(d.CD_Compresion)} · tracc {fmt(d.CD_Traccion)}      " &
                     $"Estr: comp {fmt(d.CD_Compresion_Estructural)} · tracc {fmt(d.CD_Traccion_Estructural)}"

        ' Tercera línea opcional: distribución P_i para la combinación que
        ' gobierna compresión (muestra el análisis biaxial con tensor 2x2).
        Dim linea3 = ""
        Dim td = _vm.Modelo.BuscarTipoDado(d.NombreTipoDado)
        If td IsNot Nothing AndAlso td.Pilotes.Count > 0 AndAlso Not String.IsNullOrEmpty(d.Combo_Gob_Compresion) Then
            Dim combo = d.Combinaciones.FirstOrDefault(
                Function(c) c.LoadCase.Equals(d.Combo_Gob_Compresion, StringComparison.OrdinalIgnoreCase))
            If combo IsNot Nothing Then
                Dim mLocal = PiloteService.RotarMomentoAGlobalALocal(combo.MX, combo.MY, d.AnguloPlanta)
                Dim fuerzas = PiloteService.CalcularFuerzaPilotes(td.Pilotes, combo.FZ,
                                                                   mLocal.Item1, mLocal.Item2)
                If fuerzas.Length > 0 Then
                    Dim detalle = String.Join(" | ",
                        fuerzas.Select(Function(f, i) $"P{i + 1}={f:0} kN"))
                    linea3 = Environment.NewLine &
                             $"Combo {d.Combo_Gob_Compresion}: {detalle}"
                End If
            End If
        End If

        LblInfoPlanta.Text = linea1 & Environment.NewLine & linea2 & linea3
    End Sub

    Private Sub ApoyoShape_MouseLeave(sender As Object, e As MouseEventArgs)
        If LblInfoPlanta IsNot Nothing Then
            LblInfoPlanta.Text = "Rueda: zoom · Clic derecho + arrastrar: mover · Clic apoyo: detalle"
        End If
    End Sub

    Private Sub ApoyoShape_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        Dim shape = TryCast(sender, Shape)
        If shape Is Nothing Then Return
        Dim d = TryCast(shape.Tag, Dado)
        If d Is Nothing Then Return
        AbrirDetalleApoyo(d)
        e.Handled = True
    End Sub

    ''' <summary>
    ''' Selección en la tabla Top-10: pinta un halo naranja alrededor del
    ''' apoyo correspondiente en la planta. Doble clic sobre la fila abre el
    ''' detalle, igual que clicar el apoyo directamente.
    ''' </summary>
    Private Sub GridTop10_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim grid = TryCast(sender, DataGrid)
        If grid Is Nothing Then Return
        Dim dvm = TryCast(grid.SelectedItem, DadoVM)
        If dvm Is Nothing Then
            _dadoResaltado = Nothing
        Else
            _dadoResaltado = dvm.Modelo
        End If
        DibujarPlanta()
    End Sub

    Private Sub GridTop10_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        Dim grid = TryCast(sender, DataGrid)
        If grid Is Nothing Then Return
        Dim dvm = TryCast(grid.SelectedItem, DadoVM)
        If dvm Is Nothing OrElse dvm.Modelo Is Nothing Then Return
        AbrirDetalleApoyo(dvm.Modelo)
        e.Handled = True
    End Sub

    Private Sub AbrirDetalleApoyo(d As Dado)
        Try
            Dim detalle As New DadoDetalleWindow(d, _vm.Modelo)
            detalle.Owner = Me
            detalle.ShowDialog()
        Catch ex As Exception
            System.Windows.MessageBox.Show(
                "No se pudo abrir el detalle: " & ex.Message,
                "Detalle de apoyo", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

End Class
