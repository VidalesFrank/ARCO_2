Imports System.IO
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Media.Media3D
Imports Microsoft.Win32
Imports Shapes = System.Windows.Shapes

''' <summary>
''' Window WPF para visualizar modelos ETABS (.e2k). Convive con la
''' versión WinForms (<see cref="Form_VisualizadorE2K"/>) que queda intacta.
''' Usa Viewport3D nativo + cámara orbital. 2D (Planta/Alzado) se dibuja
''' sobre Canvas por simplicidad.
''' </summary>
Partial Public Class Form_VisualizadorE2K_WPF
    Inherits Window

    Private _modelo As E2kModel
    Private _escena As E2kScene
    Private _camaraCtl As OrbitalCameraController
    Private _mapaVisuales As Dictionary(Of String, GeometryModel3D)
    Private _modoActual As String = "3D"   ' "3D" | "Planta" | "Alzado"
    Private _modoLineas As Boolean = False  ' False = Extruido (dim reales), True = Líneas
    Private _lienzoArrastrando As Boolean = False
    Private _lienzoUltimo As Point

    ' Índice mesh→lista de pickables (para hit test 3D sobre ModelHit)
    Private _pickPorMesh As Dictionary(Of GeometryModel3D, List(Of E2kPickable))

    ' Estado de hover y selección (compartido entre 3D y 2D)
    Private _hoverActual As E2kPickable
    Private _seleccionado As E2kPickable

    ' Estado de resaltado sobre shapes del canvas 2D
    Private _formaHover2D As Shapes.Shape
    Private _formaHoverStrokeOrig As Brush
    Private _formaHoverThickOrig As Double
    Private _formasSel2D As New List(Of Tuple(Of Shapes.Shape, Brush, Double))

    ' Detección click vs arrastre
    Private _puntoMouseDown3D As Point
    Private _esClickPotencial3D As Boolean = False
    Private _puntoMouseDown2D As Point
    Private _esClickPotencial2D As Boolean = False
    Private Const UmbralClickPx As Double = 4.0

    Public Sub New(modelo As E2kModel)
        InitializeComponent()
        _modelo = modelo
        AddHandler Me.Loaded, AddressOf OnLoaded
    End Sub

    Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
        Try
            LblNombreModelo.Text = If(_modelo IsNot Nothing AndAlso Not String.IsNullOrEmpty(_modelo.FilePath),
                                       IO.Path.GetFileName(_modelo.FilePath), "(sin archivo)")
            PoblarEstadisticas()
            PoblarPisos()

            _escena = E2kSceneBuilder.Construir(_modelo, _modoLineas)
            ConstruirIndicePicking()
            ConstruirEscena3D()

            ' OnLoaded puede re-ejecutarse al abrir otro .e2k. Limpio handlers
            ' viejos para no duplicarlos.
            If _camaraCtl IsNot Nothing Then
                RemoveHandler _camaraCtl.CameraChanged, AddressOf CamaraCambio
                _camaraCtl.Detach()
            End If
            RemoveHandler Panel3D.MouseUp, AddressOf Panel3D_MouseUp

            _camaraCtl = New OrbitalCameraController()
            ' Ojo: la cámara se adjunta a Panel3D, no a Viewport3D. Si se
            ' adjunta al Viewport3D, al ocultar categorías con los filtros los
            ' huecos sin geometría dejan de disparar MouseMove y la cámara se
            ' congela. Panel3D con Background="Transparent" siempre es hittable.
            _camaraCtl.Attach(Camara, Panel3D)
            _camaraCtl.VistaIsometrica(_escena.Bounds)

            AddHandler Panel3D.MouseUp, AddressOf Panel3D_MouseUp
            AddHandler _camaraCtl.CameraChanged, AddressOf CamaraCambio
            ActualizarLabelsEjes3D()

            LblEstado.Text = $"Modo 3D · {_modelo.FrameCount} frames · {_modelo.AreaCount} áreas"
        Catch ex As Exception
            Logger.Error(ex, "Form_VisualizadorE2K_WPF.OnLoaded")
            MessageBox.Show("Error al cargar el modelo: " & ex.Message, "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub PoblarEstadisticas()
        If _modelo Is Nothing Then Return
        LblPisos.Text = _modelo.StoryCount.ToString()
        LblNodos.Text = _modelo.Points.Count.ToString()
        LblFrames.Text = _modelo.FrameCount.ToString()
        LblAreas.Text = _modelo.AreaCount.ToString()
        LblEjes.Text = _modelo.Grids.Count.ToString()
    End Sub

    Private Sub PoblarPisos()
        CbPiso.Items.Clear()
        If _modelo Is Nothing Then Return
        For Each s In _modelo.Stories
            CbPiso.Items.Add(s.Name)
        Next
        If CbPiso.Items.Count > 0 Then CbPiso.SelectedIndex = 0
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Construcción del Model3DGroup respetando filtros
    ' ═════════════════════════════════════════════════════════════
    Private Sub ConstruirEscena3D()
        If _escena Is Nothing Then Return
        Dim raiz As New Model3DGroup()
        _mapaVisuales = New Dictionary(Of String, GeometryModel3D)()

        AgregarCategoria(raiz, "Columnas", _escena.Columnas, ChkColumnas)
        AgregarCategoria(raiz, "Vigas", _escena.Vigas, ChkVigas)
        AgregarCategoria(raiz, "Nervios", _escena.Nervios, ChkNervios)
        AgregarCategoria(raiz, "Muros", _escena.Muros, ChkMuros)
        AgregarCategoria(raiz, "Losas", _escena.Losas, ChkLosas)
        AgregarCategoria(raiz, "Grids", _escena.Grids, ChkGrids)
        AgregarCategoria(raiz, "PisoBase", _escena.PisoBase, ChkPisoBase)

        EscenaVisual.Content = raiz
    End Sub

    Private Sub AgregarCategoria(raiz As Model3DGroup, clave As String,
                                  modelo As GeometryModel3D, chk As CheckBox)
        If modelo Is Nothing Then Return
        _mapaVisuales(clave) = modelo
        If chk.IsChecked.GetValueOrDefault(True) Then
            raiz.Children.Add(modelo)
        End If
    End Sub

    Private Sub Filtro_Changed(sender As Object, e As RoutedEventArgs)
        If _escena Is Nothing Then Return
        ' Si la categoría del elemento resaltado quedó oculta, limpiarlo
        If _hoverActual IsNot Nothing AndAlso Not CategoriaVisible(_hoverActual.Tipo) Then
            FijarHover(Nothing)
            OcultarInfoCard3D()
        End If
        If _seleccionado IsNot Nothing AndAlso Not CategoriaVisible(_seleccionado.Tipo) Then
            SeleccionarElemento(Nothing)
        End If
        ConstruirEscena3D()
        ActualizarLabelsEjes3D()
        If _modoActual <> "3D" Then Redibujar2D()
    End Sub

    Private Function CategoriaVisible(tipo As String) As Boolean
        Select Case tipo
            Case "Columna" : Return ChkColumnas.IsChecked.GetValueOrDefault()
            Case "Viga" : Return ChkVigas.IsChecked.GetValueOrDefault()
            Case "Nervio" : Return ChkNervios.IsChecked.GetValueOrDefault()
            Case "Muro" : Return ChkMuros.IsChecked.GetValueOrDefault()
        End Select
        Return True
    End Function

    ' ═════════════════════════════════════════════════════════════
    ' Mode switcher
    ' ═════════════════════════════════════════════════════════════
    Private Sub BtnModo3D_Click(sender As Object, e As RoutedEventArgs)
        CambiarModo("3D")
    End Sub
    Private Sub BtnModoPlanta_Click(sender As Object, e As RoutedEventArgs)
        CambiarModo("Planta")
    End Sub
    Private Sub BtnModoAlzado_Click(sender As Object, e As RoutedEventArgs)
        CambiarModo("Alzado")
    End Sub

    Private Sub CambiarModo(modo As String)
        _modoActual = modo
        BtnModo3D.Tag = Nothing
        BtnModoPlanta.Tag = Nothing
        BtnModoAlzado.Tag = Nothing
        Select Case modo
            Case "3D"
                BtnModo3D.Tag = "activo"
                Panel3D.Visibility = Visibility.Visible
                Panel2D.Visibility = Visibility.Collapsed
                PanelPiso.Visibility = Visibility.Collapsed
                PanelDireccion.Visibility = Visibility.Collapsed
                LblEstado.Text = "Modo 3D · arrastrar=orbitar · Shift+arrastrar=pan · rueda=zoom"
                ActualizarLabelsEjes3D()
            Case "Planta"
                BtnModoPlanta.Tag = "activo"
                Panel3D.Visibility = Visibility.Collapsed
                Panel2D.Visibility = Visibility.Visible
                PanelPiso.Visibility = Visibility.Visible
                PanelDireccion.Visibility = Visibility.Collapsed
                Redibujar2D()
                LblEstado.Text = "Modo Planta · piso seleccionado en el panel lateral"
            Case "Alzado"
                BtnModoAlzado.Tag = "activo"
                Panel3D.Visibility = Visibility.Collapsed
                Panel2D.Visibility = Visibility.Visible
                PanelPiso.Visibility = Visibility.Collapsed
                PanelDireccion.Visibility = Visibility.Visible
                Redibujar2D()
                LblEstado.Text = "Modo Alzado · dirección seleccionada en el panel lateral"
        End Select
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Presets de cámara
    ' ═════════════════════════════════════════════════════════════
    Private Sub BtnVistaIso_Click(sender As Object, e As RoutedEventArgs)
        _camaraCtl?.VistaIsometrica(_escena.Bounds)
    End Sub
    Private Sub BtnVistaTop_Click(sender As Object, e As RoutedEventArgs)
        _camaraCtl?.VistaPlanta(_escena.Bounds)
    End Sub
    Private Sub BtnVistaX_Click(sender As Object, e As RoutedEventArgs)
        _camaraCtl?.VistaFrontal(_escena.Bounds)
    End Sub
    Private Sub BtnVistaY_Click(sender As Object, e As RoutedEventArgs)
        _camaraCtl?.VistaLateral(_escena.Bounds)
    End Sub
    Private Sub BtnReset_Click(sender As Object, e As RoutedEventArgs)
        If _escena IsNot Nothing Then _camaraCtl?.VistaIsometrica(_escena.Bounds)
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Abrir otro .e2k
    ' ═════════════════════════════════════════════════════════════
    Private Sub BtnAbrir_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim ofd As New OpenFileDialog() With {
                .Filter = "Modelo ETABS (*.e2k)|*.e2k|Todos los archivos (*.*)|*.*",
                .Title = "Abrir modelo ETABS (.e2k)"
            }
            If ofd.ShowDialog() <> True Then Return
            Dim nuevo = E2kParser.Parse(ofd.FileName)
            If nuevo.Points.Count = 0 AndAlso nuevo.Lines.Count = 0 Then
                MessageBox.Show("No se encontraron elementos en el archivo.",
                                "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If
            _modelo = nuevo
            ' Limpiar estado de selección/hover del modelo anterior
            _seleccionado = Nothing
            _hoverActual = Nothing
            SeleccionVisual.Content = Nothing
            HoverVisual.Content = Nothing
            PanelSeleccion.Visibility = Visibility.Collapsed
            OnLoaded(Me, New RoutedEventArgs())
        Catch ex As Exception
            Logger.Error(ex, "Form_VisualizadorE2K_WPF.BtnAbrir_Click")
            MessageBox.Show("Error: " & ex.Message, "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Exportar imagen PNG
    ' ═════════════════════════════════════════════════════════════
    Private Sub BtnExportar_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim sfd As New SaveFileDialog() With {
                .Filter = "Imagen PNG (*.png)|*.png",
                .Title = "Exportar vista actual",
                .FileName = $"ARCO_ETABS_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            }
            If sfd.ShowDialog() <> True Then Return

            Dim objetivo As FrameworkElement
            If _modoActual = "3D" Then
                objetivo = Panel3D
            Else
                objetivo = Panel2D
            End If
            Dim w = CInt(Math.Max(objetivo.ActualWidth, 10))
            Dim h = CInt(Math.Max(objetivo.ActualHeight, 10))
            Dim rtb As New RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32)
            rtb.Render(objetivo)
            Dim enc As New PngBitmapEncoder()
            enc.Frames.Add(BitmapFrame.Create(rtb))
            Using fs As New FileStream(sfd.FileName, FileMode.Create)
                enc.Save(fs)
            End Using
            LblEstado.Text = "Imagen guardada: " & IO.Path.GetFileName(sfd.FileName)
        Catch ex As Exception
            Logger.Error(ex, "Form_VisualizadorE2K_WPF.BtnExportar_Click")
            MessageBox.Show("Error al exportar: " & ex.Message, "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Controles de ventana
    ' ═════════════════════════════════════════════════════════════
    Private Sub BtnMinimize_Click(sender As Object, e As RoutedEventArgs)
        Me.WindowState = WindowState.Minimized
    End Sub
    Private Sub BtnMaximize_Click(sender As Object, e As RoutedEventArgs)
        Me.WindowState = If(Me.WindowState = WindowState.Maximized,
                            WindowState.Normal, WindowState.Maximized)
    End Sub
    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Cambios en controles de 2D
    ' ═════════════════════════════════════════════════════════════
    Private Sub CbPiso_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If _modoActual = "Planta" Then Redibujar2D()
    End Sub
    Private Sub CbDireccion_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If _modoActual = "Alzado" Then Redibujar2D()
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Renderizado 2D (Planta / Alzado)
    ' ═════════════════════════════════════════════════════════════
    Private Sub Redibujar2D()
        LienzoZoom.ScaleX = 1 : LienzoZoom.ScaleY = 1
        LienzoPan.X = 0 : LienzoPan.Y = 0
        ' Al limpiar las Children se van las shapes referenciadas por los
        ' highlights 2D: olvido esas referencias antes de borrar.
        _formasSel2D.Clear()
        _formaHover2D = Nothing
        Lienzo2D.Children.Clear()
        If _modelo Is Nothing Then Return
        If _modoActual = "Planta" Then RedibujarPlanta() Else RedibujarAlzado()
        ' Re-pintar selección si corresponde al elemento recién dibujado
        If _seleccionado IsNot Nothing Then AplicarSeleccionForma2D()
    End Sub

    Private Sub RedibujarPlanta()
        Dim storyName = TryCast(CbPiso.SelectedItem, String)
        If String.IsNullOrEmpty(storyName) Then Return
        Dim story = _modelo.GetStory(storyName)
        If story Is Nothing Then Return

        Dim w = If(Lienzo2D.ActualWidth > 0, Lienzo2D.ActualWidth, 1000)
        Dim h = If(Lienzo2D.ActualHeight > 0, Lienzo2D.ActualHeight, 700)
        Lienzo2D.Width = w
        Lienzo2D.Height = h
        Dim r = _modelo.GetBoundsXY()
        Dim pad = 40.0
        Dim sx = (w - 2 * pad) / r.Width
        Dim sy = (h - 2 * pad) / r.Height
        Dim s = Math.Min(sx, sy)
        Dim toX = Function(x As Double) (x - r.X) * s + pad
        Dim toY = Function(y As Double) h - ((y - r.Y) * s + pad)

        ' Grids + etiquetas de ejes
        If ChkGrids.IsChecked.GetValueOrDefault() Then
            Dim mostrarEtqEjes = ChkEtqEjes.IsChecked.GetValueOrDefault()
            For Each g In _modelo.Grids
                AgregarLinea(toX(g.X1), toY(g.Y1), toX(g.X2), toY(g.Y2),
                              Color.FromArgb(180, 125, 145, 170), 1.0)
                If mostrarEtqEjes AndAlso Not String.IsNullOrEmpty(g.Label) Then
                    ' Etiqueta en ambos extremos, empujada hacia afuera
                    Dim mx = (g.X1 + g.X2) / 2.0
                    Dim my = (g.Y1 + g.Y2) / 2.0
                    AgregarEtiquetaEjePlanta(g.X1, g.Y1, mx, my, g.Label, toX, toY)
                    AgregarEtiquetaEjePlanta(g.X2, g.Y2, mx, my, g.Label, toX, toY)
                End If
            Next
        End If

        Dim mostrarEtqFrame = ChkEtqFrames.IsChecked.GetValueOrDefault()
        Dim mostrarEtqPier = ChkEtqPiers.IsChecked.GetValueOrDefault()
        Dim mostrarEtqJoint = ChkEtqJoints.IsChecked.GetValueOrDefault()

        ' Columnas del piso
        If ChkColumnas.IsChecked.GetValueOrDefault() Then
            For Each la In _modelo.LineAssigns
                If Not StringEquals(la.Story, storyName) Then Continue For
                Dim ln As E2kLine = Nothing
                If Not _modelo.Lines.TryGetValue(la.LineID, ln) OrElse ln.LineType <> E2kLineType.Column Then Continue For
                Dim pt As E2kPoint = Nothing
                If Not _modelo.Points.TryGetValue(ln.Point1, pt) Then Continue For
                Dim sec As E2kFrameSection = Nothing
                _modelo.FrameSections.TryGetValue(la.Section, sec)
                Dim B = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B, 0.3)
                Dim D = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D, 0.3)
                Dim pick As New E2kPickable With {
                    .Tipo = "Columna", .Label = ln.ID, .Story = la.Story,
                    .Section = la.Section, .SectionB = B, .SectionD = D
                }
                If _modoLineas Then
                    ' Modo Líneas: círculo pequeño sobre el eje central
                    Dim radio = 4.0
                    Dim el As New Shapes.Ellipse() With {
                        .Width = radio * 2, .Height = radio * 2,
                        .Fill = New SolidColorBrush(E2k3DPalette.Columna),
                        .Stroke = New SolidColorBrush(Color.FromArgb(230, 20, 40, 60)),
                        .StrokeThickness = 0.8,
                        .Tag = pick
                    }
                    Canvas.SetLeft(el, toX(pt.X) - radio)
                    Canvas.SetTop(el, toY(pt.Y) - radio)
                    Lienzo2D.Children.Add(el)
                Else
                    ' Extruido: rectángulo rotado con dims reales (D sobre X, B sobre Y a angle=0)
                    Dim sh = AgregarRectRotado(toX(pt.X), toY(pt.Y), D * s, B * s, la.Angle,
                                               E2k3DPalette.Columna)
                    sh.Tag = pick
                End If
                If mostrarEtqFrame Then
                    AgregarTexto(ln.ID, toX(pt.X) + 4, toY(pt.Y) - 6, 10,
                                 E2k3DPalette.Columna, bold:=True)
                End If
            Next
        End If

        ' Muros (polígono con espesor real en Extruido, línea gruesa en Líneas)
        If ChkMuros.IsChecked.GetValueOrDefault() Then
            For Each aa In _modelo.AreaAssigns
                If Not StringEquals(aa.Story, storyName) Then Continue For
                Dim ar As E2kArea = Nothing
                If Not _modelo.Areas.TryGetValue(aa.AreaID, ar) OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For
                Dim puntos = PuntosUnicos(ar)
                If puntos.Count < 2 Then Continue For
                Dim etiq = If(Not String.IsNullOrEmpty(aa.PierName), aa.PierName,
                               If(Not String.IsNullOrEmpty(aa.SpandrelName), aa.SpandrelName, ar.ID))
                ' Espesor real desde la sección SHELLPROP
                Dim thick As Double = 0.0
                Dim sh_ As E2kShellSection = Nothing
                If _modelo.ShellSections.TryGetValue(aa.Section, sh_) AndAlso sh_.Thickness > 0 Then
                    thick = sh_.Thickness
                End If
                Dim pick As New E2kPickable With {
                    .Tipo = "Muro", .Label = etiq, .Story = aa.Story,
                    .Section = aa.Section, .SectionB = thick
                }
                Dim sumX As Double = 0, sumY As Double = 0
                For i = 0 To puntos.Count - 2
                    Dim p1 = puntos(i), p2 = puntos(i + 1)
                    Dim sh As Shapes.Shape
                    If _modoLineas OrElse thick <= 0 Then
                        sh = AgregarLinea(toX(p1.X), toY(p1.Y), toX(p2.X), toY(p2.Y),
                                          E2k3DPalette.Muro, 3.5)
                    Else
                        sh = AgregarPoligonoSegmento(p1.X, p1.Y, p2.X, p2.Y, thick,
                                                     E2k3DPalette.Muro, 0.8, toX, toY)
                    End If
                    If sh IsNot Nothing Then sh.Tag = pick
                    sumX += (p1.X + p2.X) / 2 : sumY += (p1.Y + p2.Y) / 2
                Next
                If mostrarEtqPier AndAlso Not String.IsNullOrEmpty(etiq) Then
                    Dim cx = sumX / Math.Max(1, puntos.Count - 1)
                    Dim cy = sumY / Math.Max(1, puntos.Count - 1)
                    AgregarTexto(etiq, toX(cx) + 4, toY(cy) - 7, 10,
                                 Color.FromRgb(140, 100, 30), bold:=True)
                End If
            Next
        End If

        ' Vigas / Nervios (como líneas delgadas)
        If ChkVigas.IsChecked.GetValueOrDefault() OrElse ChkNervios.IsChecked.GetValueOrDefault() Then
            For Each la In _modelo.LineAssigns
                If Not StringEquals(la.Story, storyName) Then Continue For
                Dim ln As E2kLine = Nothing
                If Not _modelo.Lines.TryGetValue(la.LineID, ln) OrElse ln.LineType <> E2kLineType.Beam Then Continue For
                Dim p1 As E2kPoint = Nothing, p2 As E2kPoint = Nothing
                If Not _modelo.Points.TryGetValue(ln.Point1, p1) Then Continue For
                If Not _modelo.Points.TryGetValue(ln.Point2, p2) Then Continue For
                Dim sec As E2kFrameSection = Nothing
                _modelo.FrameSections.TryGetValue(la.Section, sec)
                Dim esNerv = (sec IsNot Nothing AndAlso
                              (sec.Name.ToUpperInvariant().Contains("_N_") OrElse
                               sec.Name.ToUpperInvariant().Contains("NERV")))
                If esNerv AndAlso Not ChkNervios.IsChecked.GetValueOrDefault() Then Continue For
                If Not esNerv AndAlso Not ChkVigas.IsChecked.GetValueOrDefault() Then Continue For
                Dim color = If(esNerv, E2k3DPalette.Nervio, E2k3DPalette.Viga)
                Dim Bv = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B, If(esNerv, 0.1, 0.3))
                Dim Dv = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D, If(esNerv, 0.4, 0.5))
                Dim pickVL As New E2kPickable With {
                    .Tipo = If(esNerv, "Nervio", "Viga"),
                    .Label = ln.ID, .Story = la.Story,
                    .Section = la.Section, .SectionB = Bv, .SectionD = Dv
                }
                Dim sh As Shapes.Shape
                If _modoLineas Then
                    sh = AgregarLinea(toX(p1.X), toY(p1.Y), toX(p2.X), toY(p2.Y), color, 1.4)
                Else
                    ' Extruido: polígono con ancho B real perpendicular al segmento
                    sh = AgregarPoligonoSegmento(p1.X, p1.Y, p2.X, p2.Y, Bv, color, 0.6, toX, toY)
                End If
                If sh IsNot Nothing Then sh.Tag = pickVL
                If mostrarEtqFrame Then
                    AgregarTexto(ln.ID,
                                 (toX(p1.X) + toX(p2.X)) / 2 + 2,
                                 (toY(p1.Y) + toY(p2.Y)) / 2 - 12,
                                 9.5, color, bold:=False)
                End If
            Next
        End If

        ' Nodos (joints) — pequeños círculos + etiqueta opcional
        If mostrarEtqJoint Then
            For Each pt In _modelo.Points.Values
                Dim e As New Shapes.Ellipse() With {
                    .Width = 4, .Height = 4,
                    .Fill = New SolidColorBrush(E2k3DPalette.Punto)
                }
                Canvas.SetLeft(e, toX(pt.X) - 2)
                Canvas.SetTop(e, toY(pt.Y) - 2)
                Lienzo2D.Children.Add(e)
                AgregarTexto(pt.ID, toX(pt.X) + 3, toY(pt.Y) + 2, 9,
                             E2k3DPalette.GridEtiqueta)
            Next
        End If

        ' Etiqueta de piso
        AgregarTexto($"{story.Name}  ({story.ElevTop:F2} m)", 12, 12, 14,
                      E2k3DPalette.GridEtiqueta, bold:=True)
    End Sub

    Private Sub RedibujarAlzado()
        Dim dirX As Boolean = (CbDireccion.SelectedIndex <> 1)  ' 0 = vista X, 1 = vista Y
        If _modelo.Stories.Count = 0 Then Return

        Dim w = If(Lienzo2D.ActualWidth > 0, Lienzo2D.ActualWidth, 1000)
        Dim h = If(Lienzo2D.ActualHeight > 0, Lienzo2D.ActualHeight, 700)
        Lienzo2D.Width = w
        Lienzo2D.Height = h
        Dim pad = 60.0

        Dim r = _modelo.GetBoundsXY()
        Dim rangoHoriz = If(dirX, r.Width, r.Height)
        Dim origenHoriz = If(dirX, r.X, r.Y)
        Dim zMin = _modelo.Stories.Min(Function(st0) st0.ElevBottom)
        Dim zMax = _modelo.Stories.Max(Function(st0) st0.ElevTop)

        Dim sx = (w - 2 * pad) / rangoHoriz
        Dim sz = (h - 2 * pad) / Math.Max(zMax - zMin, 0.5)
        Dim s = Math.Min(sx, sz)
        Dim toX = Function(xy As Double) (xy - origenHoriz) * s + pad
        Dim toY = Function(z As Double) h - ((z - zMin) * s + pad)

        ' Líneas de pisos
        For Each st In _modelo.Stories
            Dim y = toY(st.ElevTop)
            AgregarLinea(pad, y, w - pad, y, Color.FromArgb(120, 180, 190, 200), 0.8)
            AgregarTexto($"{st.Name}  {st.ElevTop:F2}m", 6, CSng(y - 14), 11,
                         E2k3DPalette.GridEtiqueta)
        Next

        ' Ejes perpendiculares al alzado (los que se ven como línea vertical)
        If ChkGrids.IsChecked.GetValueOrDefault() Then
            Dim mostrarEtqEjes = ChkEtqEjes.IsChecked.GetValueOrDefault()
            For Each g In _modelo.Grids
                ' El grid "se ve" en este alzado si sus dos extremos comparten
                ' coordenada en el eje horizontal de la vista (es un plano vertical).
                Dim pA = If(dirX, g.X1, g.Y1)
                Dim pB = If(dirX, g.X2, g.Y2)
                If Math.Abs(pA - pB) > 0.05 Then Continue For
                Dim xS = toX(pA)
                AgregarLinea(xS, pad, xS, h - pad, Color.FromArgb(120, 125, 145, 170), 0.8)
                If mostrarEtqEjes AndAlso Not String.IsNullOrEmpty(g.Label) Then
                    Dim el As New Shapes.Ellipse() With {
                        .Width = 22, .Height = 22,
                        .Fill = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                        .Stroke = New SolidColorBrush(E2k3DPalette.GridEtiqueta),
                        .StrokeThickness = 1.1
                    }
                    Canvas.SetLeft(el, xS - 11) : Canvas.SetTop(el, pad - 26)
                    Lienzo2D.Children.Add(el)
                    Dim tb As New TextBlock() With {
                        .Text = g.Label,
                        .FontFamily = TryCast(TryFindResource("FuenteDisplay"), FontFamily),
                        .FontSize = 10,
                        .FontWeight = FontWeights.SemiBold,
                        .Foreground = New SolidColorBrush(E2k3DPalette.GridEtiqueta)
                    }
                    tb.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
                    Canvas.SetLeft(tb, xS - tb.DesiredSize.Width / 2)
                    Canvas.SetTop(tb, pad - 15 - tb.DesiredSize.Height / 2)
                    Lienzo2D.Children.Add(tb)
                End If
            Next
        End If

        Dim mostrarEtqFrame = ChkEtqFrames.IsChecked.GetValueOrDefault()
        Dim mostrarEtqPier = ChkEtqPiers.IsChecked.GetValueOrDefault()

        ' Muros (como polígonos rellenos)
        If ChkMuros.IsChecked.GetValueOrDefault() Then
            For Each aa In _modelo.AreaAssigns
                Dim ar As E2kArea = Nothing
                If Not _modelo.Areas.TryGetValue(aa.AreaID, ar) OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For
                Dim st = _modelo.GetStory(aa.Story)
                If st Is Nothing Then Continue For
                Dim puntos = PuntosUnicos(ar)
                If puntos.Count < 2 Then Continue For
                Dim etiq = If(Not String.IsNullOrEmpty(aa.PierName), aa.PierName,
                               If(Not String.IsNullOrEmpty(aa.SpandrelName), aa.SpandrelName, ar.ID))
                Dim pick As New E2kPickable With {
                    .Tipo = "Muro", .Label = etiq, .Story = aa.Story, .Section = aa.Section
                }
                Dim xCentro As Double = 0, nSeg As Integer = 0
                For i = 0 To puntos.Count - 2
                    Dim p1 = puntos(i), p2 = puntos(i + 1)
                    Dim xa = If(dirX, p1.X, p1.Y)
                    Dim xb = If(dirX, p2.X, p2.Y)
                    Dim x1 = toX(Math.Min(xa, xb))
                    Dim x2 = toX(Math.Max(xa, xb))
                    Dim y1 = toY(st.ElevTop)
                    Dim y2 = toY(st.ElevBottom)
                    Dim sh = AgregarRectAlineado(x1, y1, x2 - x1, y2 - y1, E2k3DPalette.Muro)
                    sh.Tag = pick
                    xCentro += (xa + xb) / 2 : nSeg += 1
                Next
                If mostrarEtqPier AndAlso Not String.IsNullOrEmpty(etiq) AndAlso nSeg > 0 Then
                    AgregarTexto(etiq, toX(xCentro / nSeg), toY((st.ElevTop + st.ElevBottom) / 2) - 8,
                                 10, Color.FromRgb(140, 100, 30), bold:=True)
                End If
            Next
        End If

        ' Columnas (como rectángulos verticales)
        If ChkColumnas.IsChecked.GetValueOrDefault() Then
            For Each la In _modelo.LineAssigns
                Dim ln As E2kLine = Nothing
                If Not _modelo.Lines.TryGetValue(la.LineID, ln) OrElse ln.LineType <> E2kLineType.Column Then Continue For
                Dim pt As E2kPoint = Nothing
                If Not _modelo.Points.TryGetValue(ln.Point1, pt) Then Continue For
                Dim st = _modelo.GetStory(la.Story)
                If st Is Nothing Then Continue For
                Dim sec As E2kFrameSection = Nothing
                _modelo.FrameSections.TryGetValue(la.Section, sec)
                Dim B = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B, 0.3)
                Dim D = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D, 0.3)
                Dim xy = If(dirX, pt.X, pt.Y)
                Dim cx = toX(xy)
                Dim y1 = toY(st.ElevTop)
                Dim y2 = toY(st.ElevBottom)
                Dim pick As New E2kPickable With {
                    .Tipo = "Columna", .Label = ln.ID, .Story = la.Story,
                    .Section = la.Section, .SectionB = B, .SectionD = D
                }
                If _modoLineas Then
                    ' Modo Líneas: línea vertical delgada en el eje central
                    Dim sh = AgregarLinea(cx, y1, cx, y2, E2k3DPalette.Columna, 2.0)
                    sh.Tag = pick
                Else
                    ' Extruido: proyección de la sección rotada al eje del alzado.
                    ' Vista X: eje = X; Vista Y: eje = Y.
                    ' axis-2=D a (cos,sin), axis-3=B a (−sin,cos). Extent X = D·|cos|+B·|sin|.
                    Dim ang = la.Angle * Math.PI / 180.0
                    Dim visibleW = If(dirX,
                                       D * Math.Abs(Math.Cos(ang)) + B * Math.Abs(Math.Sin(ang)),
                                       D * Math.Abs(Math.Sin(ang)) + B * Math.Abs(Math.Cos(ang)))
                    Dim sh = AgregarRectAlineado(cx - (visibleW * s) / 2, y1, visibleW * s, y2 - y1, E2k3DPalette.Columna)
                    sh.Tag = pick
                End If
                If mostrarEtqFrame Then
                    AgregarTexto(ln.ID, cx + 3, (y1 + y2) / 2 - 7, 9,
                                 E2k3DPalette.Columna, bold:=True)
                End If
            Next
        End If
    End Sub

    ' ─────── Helpers 2D ───────────────────────────────────────
    Private Function AgregarLinea(x1 As Double, y1 As Double, x2 As Double, y2 As Double,
                                   color As Color, grosor As Double) As Shapes.Line
        Dim ln As New Shapes.Line() With {
            .X1 = x1, .Y1 = y1, .X2 = x2, .Y2 = y2,
            .Stroke = New SolidColorBrush(color), .StrokeThickness = grosor,
            .StrokeStartLineCap = PenLineCap.Round, .StrokeEndLineCap = PenLineCap.Round
        }
        Lienzo2D.Children.Add(ln)
        Return ln
    End Function

    Private Function AgregarRectAlineado(x As Double, y As Double, w As Double, h As Double,
                                          color As Color) As Shapes.Rectangle
        If w < 0 Then x += w : w = -w
        If h < 0 Then y += h : h = -h
        Dim r As New Shapes.Rectangle() With {
            .Width = Math.Max(w, 0.5),
            .Height = Math.Max(h, 0.5),
            .Fill = New SolidColorBrush(color),
            .Stroke = New SolidColorBrush(Color.FromArgb(230, color.R, color.G, color.B)),
            .StrokeThickness = 0.6
        }
        Canvas.SetLeft(r, x)
        Canvas.SetTop(r, y)
        Lienzo2D.Children.Add(r)
        Return r
    End Function

    ''' <summary>
    ''' Polígono de 4 esquinas que representa un elemento lineal (viga, muro)
    ''' con ancho real en coords mundo. Las dos esquinas de cada extremo
    ''' están offset perpendicular por <paramref name="anchoMetros"/>/2.
    ''' </summary>
    Private Function AgregarPoligonoSegmento(wx1 As Double, wy1 As Double,
                                              wx2 As Double, wy2 As Double,
                                              anchoMetros As Double,
                                              fill As Color, strokeThickness As Double,
                                              toX As Func(Of Double, Double),
                                              toY As Func(Of Double, Double)) As Shapes.Polygon
        Dim dx = wx2 - wx1, dy = wy2 - wy1
        Dim L = Math.Sqrt(dx * dx + dy * dy)
        If L < 1.0E-6 Then Return Nothing
        Dim nx = -dy / L, ny = dx / L
        Dim ht = anchoMetros / 2.0
        Dim p As New Shapes.Polygon()
        p.Fill = New SolidColorBrush(fill)
        p.Stroke = New SolidColorBrush(Color.FromArgb(230, fill.R, fill.G, fill.B))
        p.StrokeThickness = strokeThickness
        p.Points.Add(New Point(toX(wx1 + nx * ht), toY(wy1 + ny * ht)))
        p.Points.Add(New Point(toX(wx1 - nx * ht), toY(wy1 - ny * ht)))
        p.Points.Add(New Point(toX(wx2 - nx * ht), toY(wy2 - ny * ht)))
        p.Points.Add(New Point(toX(wx2 + nx * ht), toY(wy2 + ny * ht)))
        Lienzo2D.Children.Add(p)
        Return p
    End Function

    Private Function AgregarRectRotado(cx As Double, cy As Double, B As Double, D As Double,
                                        angleDeg As Double, color As Color) As Shapes.Rectangle
        Dim r As New Shapes.Rectangle() With {
            .Width = Math.Max(B, 1.0),
            .Height = Math.Max(D, 1.0),
            .Fill = New SolidColorBrush(color),
            .Stroke = New SolidColorBrush(Color.FromArgb(240, color.R, color.G, color.B)),
            .StrokeThickness = 0.8,
            .RenderTransformOrigin = New Point(0.5, 0.5)
        }
        r.RenderTransform = New RotateTransform(-angleDeg)
        Canvas.SetLeft(r, cx - B / 2)
        Canvas.SetTop(r, cy - D / 2)
        Lienzo2D.Children.Add(r)
        Return r
    End Function

    Private Sub AgregarTexto(texto As String, x As Double, y As Double, size As Double,
                              color As Color, Optional bold As Boolean = False)
        Dim tb As New TextBlock() With {
            .Text = texto,
            .FontFamily = TryCast(TryFindResource("FuenteBase"), FontFamily),
            .FontSize = size,
            .Foreground = New SolidColorBrush(color),
            .FontWeight = If(bold, FontWeights.SemiBold, FontWeights.Normal)
        }
        Canvas.SetLeft(tb, x)
        Canvas.SetTop(tb, y)
        Lienzo2D.Children.Add(tb)
    End Sub

    ''' <summary>
    ''' Burbuja circular con el label del eje, posicionada en (<paramref name="px"/>,
    ''' <paramref name="py"/>) empujada hacia afuera respecto al centro
    ''' (<paramref name="mx"/>, <paramref name="my"/>). Formato tipo plano.
    ''' </summary>
    Private Sub AgregarEtiquetaEjePlanta(px As Double, py As Double,
                                          mx As Double, my As Double,
                                          texto As String,
                                          toX As Func(Of Double, Double),
                                          toY As Func(Of Double, Double))
        Dim dx = px - mx, dy = py - my
        Dim L = Math.Sqrt(dx * dx + dy * dy)
        If L < 1.0E-6 Then Return
        Dim pxN = dx / L, pyN = dy / L
        ' Empujar 18 px de pantalla hacia afuera
        Dim sx = toX(px) + pxN * 18
        Dim sy = toY(py) - pyN * 18   ' invertido porque Y pantalla crece hacia abajo
        Dim radio = 11.0
        Dim el As New Shapes.Ellipse() With {
            .Width = radio * 2, .Height = radio * 2,
            .Fill = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            .Stroke = New SolidColorBrush(E2k3DPalette.GridEtiqueta),
            .StrokeThickness = 1.1
        }
        Canvas.SetLeft(el, sx - radio) : Canvas.SetTop(el, sy - radio)
        Lienzo2D.Children.Add(el)
        Dim tb As New TextBlock() With {
            .Text = texto,
            .FontFamily = TryCast(TryFindResource("FuenteDisplay"), FontFamily),
            .FontSize = 10,
            .FontWeight = FontWeights.SemiBold,
            .Foreground = New SolidColorBrush(E2k3DPalette.GridEtiqueta)
        }
        tb.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
        Canvas.SetLeft(tb, sx - tb.DesiredSize.Width / 2)
        Canvas.SetTop(tb, sy - tb.DesiredSize.Height / 2)
        Lienzo2D.Children.Add(tb)
    End Sub

    Private Function PuntosUnicos(ar As E2kArea) As List(Of E2kPoint)
        Dim res As New List(Of E2kPoint)
        Dim vistos As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each pid In ar.Points
            If vistos.Contains(pid) Then Continue For
            Dim p As E2kPoint = Nothing
            If _modelo.Points.TryGetValue(pid, p) Then
                res.Add(p)
                vistos.Add(pid)
            End If
        Next
        Return res
    End Function

    Private Function StringEquals(a As String, b As String) As Boolean
        Return String.Equals(a, b, StringComparison.OrdinalIgnoreCase)
    End Function

    ' ═════════════════════════════════════════════════════════════
    ' Pan & zoom del lienzo 2D
    ' ═════════════════════════════════════════════════════════════
    Private Sub Lienzo_MouseDown(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left Then
            _lienzoArrastrando = True
            _lienzoUltimo = e.GetPosition(Me)
            _puntoMouseDown2D = e.GetPosition(Lienzo2D)
            _esClickPotencial2D = True
            Lienzo2D.CaptureMouse()
        End If
    End Sub
    Private Sub Lienzo_MouseMove(sender As Object, e As MouseEventArgs)
        If _lienzoArrastrando Then
            Dim actual = e.GetPosition(Me)
            LienzoPan.X += (actual.X - _lienzoUltimo.X)
            LienzoPan.Y += (actual.Y - _lienzoUltimo.Y)
            _lienzoUltimo = actual
            ' Cancelar click si el usuario está panning
            If _esClickPotencial2D Then
                Dim pc = e.GetPosition(Lienzo2D)
                If Math.Abs(pc.X - _puntoMouseDown2D.X) > UmbralClickPx OrElse
                   Math.Abs(pc.Y - _puntoMouseDown2D.Y) > UmbralClickPx Then
                    _esClickPotencial2D = False
                End If
            End If
        Else
            ActualizarHover2D(e)
        End If
    End Sub
    Private Sub Lienzo_MouseUp(sender As Object, e As MouseButtonEventArgs)
        If _lienzoArrastrando Then
            _lienzoArrastrando = False
            Lienzo2D.ReleaseMouseCapture()
        End If
        If e.ChangedButton = MouseButton.Left AndAlso _esClickPotencial2D Then
            _esClickPotencial2D = False
            Dim hit = HitTest2DEn(e.GetPosition(Lienzo2D))
            SeleccionarElemento(hit)
        End If
    End Sub
    Private Sub Lienzo_MouseWheel(sender As Object, e As MouseWheelEventArgs)
        Dim factor = If(e.Delta > 0, 1.1, 1 / 1.1)
        LienzoZoom.ScaleX *= factor
        LienzoZoom.ScaleY *= factor
        e.Handled = True
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Hover + etiquetas
    ' ═════════════════════════════════════════════════════════════

    Private Sub ConstruirIndicePicking()
        _pickPorMesh = New Dictionary(Of GeometryModel3D, List(Of E2kPickable))()
        If _escena Is Nothing Then Return
        If _escena.Columnas IsNot Nothing AndAlso _escena.ColumnasPick IsNot Nothing Then
            _pickPorMesh(_escena.Columnas) = _escena.ColumnasPick
        End If
        If _escena.Vigas IsNot Nothing AndAlso _escena.VigasPick IsNot Nothing Then
            _pickPorMesh(_escena.Vigas) = _escena.VigasPick
        End If
        If _escena.Nervios IsNot Nothing AndAlso _escena.NerviosPick IsNot Nothing Then
            _pickPorMesh(_escena.Nervios) = _escena.NerviosPick
        End If
        If _escena.Muros IsNot Nothing AndAlso _escena.MurosPick IsNot Nothing Then
            _pickPorMesh(_escena.Muros) = _escena.MurosPick
        End If
    End Sub

    ' ─── Handlers 3D ────────────────────────────────────────────
    Private Sub Panel3D_MouseDown(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left Then
            _puntoMouseDown3D = e.GetPosition(Panel3D)
            _esClickPotencial3D = True
        End If
    End Sub

    Private Sub Panel3D_MouseUp(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton <> MouseButton.Left Then Return
        If _esClickPotencial3D Then
            _esClickPotencial3D = False
            Dim ptVp = e.GetPosition(Viewport3D)
            Dim hit = HitTest3DEn(ptVp)
            SeleccionarElemento(hit)
        End If
    End Sub

    Private Sub Panel3D_MouseMove(sender As Object, e As MouseEventArgs)
        ' Cancelar click potencial si se mueve más del umbral
        If _esClickPotencial3D Then
            Dim pt = e.GetPosition(Panel3D)
            If Math.Abs(pt.X - _puntoMouseDown3D.X) > UmbralClickPx OrElse
               Math.Abs(pt.Y - _puntoMouseDown3D.Y) > UmbralClickPx Then
                _esClickPotencial3D = False
            End If
        End If

        If _escena Is Nothing OrElse _pickPorMesh Is Nothing Then Return
        ' Durante drag (orbita o pan) ocultar hover para no saturar el UI thread
        If e.LeftButton = MouseButtonState.Pressed OrElse
           e.MiddleButton = MouseButtonState.Pressed Then
            FijarHover(Nothing)
            OcultarInfoCard3D()
            Return
        End If

        Dim ptVp = e.GetPosition(Viewport3D)
        Dim hit = HitTest3DEn(ptVp)
        FijarHover(hit)

        If hit Is Nothing Then
            OcultarInfoCard3D()
        Else
            MostrarInfoCard3D(hit, e.GetPosition(Panel3D))
        End If
    End Sub

    Private Sub Panel3D_MouseLeave(sender As Object, e As MouseEventArgs)
        FijarHover(Nothing)
        OcultarInfoCard3D()
        _esClickPotencial3D = False
    End Sub

    ''' <summary>Ray-cast en el punto del viewport; devuelve el pickable o Nothing.</summary>
    Private Function HitTest3DEn(ptViewport As Point) As E2kPickable
        Dim hit As E2kPickable = Nothing
        Try
            Dim params = New PointHitTestParameters(ptViewport)
            VisualTreeHelper.HitTest(Viewport3D,
                Nothing,
                Function(res As HitTestResult)
                    Dim r = TryCast(res, RayMeshGeometry3DHitTestResult)
                    If r Is Nothing Then Return HitTestResultBehavior.Continue
                    Dim lista As List(Of E2kPickable) = Nothing
                    If Not _pickPorMesh.TryGetValue(r.ModelHit, lista) Then Return HitTestResultBehavior.Continue
                    Dim vi = r.VertexIndex1
                    For Each p In lista
                        If vi >= p.StartVertex AndAlso vi < p.EndVertex Then
                            hit = p
                            Return HitTestResultBehavior.Stop
                        End If
                    Next
                    Return HitTestResultBehavior.Continue
                End Function,
                params)
        Catch
            ' HitTest puede fallar durante layout; silencioso.
        End Try
        Return hit
    End Function

    Private Sub MostrarInfoCard3D(p As E2kPickable, mousePt As Point)
        PoblarInfoCard(p, InfoCardChip3D, InfoCardTipo3D, InfoCardLabel3D,
                       InfoCardSeccion3D, InfoCardExtra3D)
        InfoCard3D.Visibility = Visibility.Visible
        Dim x = Math.Max(0, Math.Min(Panel3D.ActualWidth - 280, mousePt.X + 18))
        Dim y = Math.Max(0, Math.Min(Panel3D.ActualHeight - 120, mousePt.Y + 18))
        InfoCard3D.Margin = New Thickness(x, y, 0, 0)
    End Sub

    Private Sub OcultarInfoCard3D()
        If InfoCard3D IsNot Nothing Then InfoCard3D.Visibility = Visibility.Collapsed
    End Sub

    ' ─── Highlight mesh overlay en 3D ──────────────────────────
    Private Sub FijarHover(p As E2kPickable)
        ' No mostrar hover sobre el elemento ya seleccionado (sería redundante)
        If p IsNot Nothing AndAlso _seleccionado IsNot Nothing AndAlso
           SonElMismoElemento(p, _seleccionado) Then p = Nothing
        If (p Is Nothing AndAlso _hoverActual Is Nothing) Then Return
        If (p IsNot Nothing AndAlso _hoverActual IsNot Nothing AndAlso
            SonElMismoElemento(p, _hoverActual)) Then Return
        _hoverActual = p
        HoverVisual.Content = If(p Is Nothing, Nothing, CrearOverlayMesh(p, E2k3DPalette.Hover, 1.02))
    End Sub

    Private Sub FijarSeleccion(p As E2kPickable)
        _seleccionado = p
        SeleccionVisual.Content = If(p Is Nothing, Nothing, CrearOverlayMesh(p, E2k3DPalette.Seleccion, 1.03))
        ' Si el hover es sobre el mismo elemento, limpiarlo (ya está seleccionado)
        If p IsNot Nothing AndAlso _hoverActual IsNot Nothing AndAlso
           SonElMismoElemento(p, _hoverActual) Then
            _hoverActual = Nothing
            HoverVisual.Content = Nothing
        End If
    End Sub

    Private Function CrearOverlayMesh(p As E2kPickable, color As Color, escala As Double) As Model3D
        If p Is Nothing Then Return Nothing
        Dim meshBase = BuscarMeshBaseDe(p)
        If meshBase Is Nothing Then Return Nothing
        Dim sub_ = E2kSceneBuilder.ExtraerMeshDe(meshBase, p, escala)
        If sub_ Is Nothing Then Return Nothing
        Dim brush As New SolidColorBrush(color)
        brush.Freeze()
        Dim mat As New EmissiveMaterial(brush)
        mat.Freeze()
        Dim mdl As New GeometryModel3D(sub_, mat)
        mdl.BackMaterial = mat
        mdl.Freeze()
        Return mdl
    End Function

    Private Function BuscarMeshBaseDe(p As E2kPickable) As MeshGeometry3D
        Select Case p.Tipo
            Case "Columna" : Return TryCast(_escena.Columnas?.Geometry, MeshGeometry3D)
            Case "Viga" : Return TryCast(_escena.Vigas?.Geometry, MeshGeometry3D)
            Case "Nervio" : Return TryCast(_escena.Nervios?.Geometry, MeshGeometry3D)
            Case "Muro" : Return TryCast(_escena.Muros?.Geometry, MeshGeometry3D)
        End Select
        Return Nothing
    End Function

    Private Function SonElMismoElemento(a As E2kPickable, b As E2kPickable) As Boolean
        If a Is b Then Return True
        If a Is Nothing OrElse b Is Nothing Then Return False
        Return a.Tipo = b.Tipo AndAlso a.StartVertex = b.StartVertex
    End Function

    ' ─── Selección (compartida entre 3D y 2D) ──────────────────
    Private Sub SeleccionarElemento(p As E2kPickable)
        FijarSeleccion(p)         ' overlay 3D
        AplicarSeleccionForma2D() ' resaltado 2D (si hay shapes que matchean)
        If p Is Nothing Then
            PanelSeleccion.Visibility = Visibility.Collapsed
        Else
            PoblarInfoCard(p, SelChip, SelTipo, SelLabel, SelSeccion, SelExtra)
            PanelSeleccion.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub BtnDeseleccionar_Click(sender As Object, e As RoutedEventArgs)
        SeleccionarElemento(Nothing)
    End Sub

    ' ─── Hit test 2D ────────────────────────────────────────────
    Private Sub Lienzo_MouseLeave(sender As Object, e As MouseEventArgs)
        LimpiarHoverForma2D()
        If InfoCard2D IsNot Nothing Then InfoCard2D.Visibility = Visibility.Collapsed
    End Sub

    Private Sub ActualizarHover2D(e As MouseEventArgs)
        Dim ptCanvas = e.GetPosition(Lienzo2D)
        Dim shape As Shapes.Shape = Nothing
        Dim hit As E2kPickable = Nothing
        Try
            Dim res = VisualTreeHelper.HitTest(Lienzo2D, ptCanvas)
            If res IsNot Nothing AndAlso res.VisualHit IsNot Nothing Then
                shape = TryCast(res.VisualHit, Shapes.Shape)
                If shape IsNot Nothing Then hit = TryCast(shape.Tag, E2kPickable)
            End If
        Catch
        End Try

        AplicarHoverForma2D(shape, hit)

        If hit Is Nothing Then
            InfoCard2D.Visibility = Visibility.Collapsed
        Else
            PoblarInfoCard(hit, InfoCardChip2D, InfoCardTipo2D, InfoCardLabel2D,
                           InfoCardSeccion2D, InfoCardExtra2D)
            InfoCard2D.Visibility = Visibility.Visible
            Dim ptHost = e.GetPosition(Panel2D)
            Dim x = Math.Max(0, Math.Min(Panel2D.ActualWidth - 280, ptHost.X + 18))
            Dim y = Math.Max(0, Math.Min(Panel2D.ActualHeight - 120, ptHost.Y + 18))
            InfoCard2D.Margin = New Thickness(x, y, 0, 0)
        End If
    End Sub

    ''' <summary>Resalta <paramref name="sh"/> en el canvas 2D y limpia el anterior.</summary>
    Private Sub AplicarHoverForma2D(sh As Shapes.Shape, pick As E2kPickable)
        ' No cambiar nada si se hovered la misma shape
        If sh Is _formaHover2D Then Return
        LimpiarHoverForma2D()
        If sh Is Nothing OrElse pick Is Nothing Then Return
        ' Si la shape ya forma parte de la selección activa, no sobrepintar
        For Each t In _formasSel2D
            If t.Item1 Is sh Then Return
        Next
        _formaHover2D = sh
        _formaHoverStrokeOrig = sh.Stroke
        _formaHoverThickOrig = sh.StrokeThickness
        sh.Stroke = New SolidColorBrush(E2k3DPalette.Hover)
        sh.StrokeThickness = Math.Max(1.6, sh.StrokeThickness + 1.2)
    End Sub

    Private Sub LimpiarHoverForma2D()
        If _formaHover2D Is Nothing Then Return
        _formaHover2D.Stroke = _formaHoverStrokeOrig
        _formaHover2D.StrokeThickness = _formaHoverThickOrig
        _formaHover2D = Nothing
    End Sub

    ''' <summary>Resalta todas las shapes del canvas cuyo Tag coincida con el pickable seleccionado.</summary>
    Private Sub AplicarSeleccionForma2D()
        LimpiarSeleccionForma2D()
        If _seleccionado Is Nothing OrElse Lienzo2D Is Nothing Then Return
        For Each ch As Object In Lienzo2D.Children
            Dim sh = TryCast(ch, Shapes.Shape)
            If sh Is Nothing Then Continue For
            Dim pk = TryCast(sh.Tag, E2kPickable)
            If pk Is Nothing Then Continue For
            If Not SonElMismoElemento(pk, _seleccionado) Then Continue For
            ' Limpiar hover si pisa la selección
            If sh Is _formaHover2D Then LimpiarHoverForma2D()
            _formasSel2D.Add(Tuple.Create(sh, sh.Stroke, sh.StrokeThickness))
            sh.Stroke = New SolidColorBrush(E2k3DPalette.Seleccion)
            sh.StrokeThickness = Math.Max(2.2, sh.StrokeThickness + 2.0)
        Next
    End Sub

    Private Sub LimpiarSeleccionForma2D()
        For Each t In _formasSel2D
            t.Item1.Stroke = t.Item2
            t.Item1.StrokeThickness = t.Item3
        Next
        _formasSel2D.Clear()
    End Sub

    Private Function HitTest2DEn(ptCanvas As Point) As E2kPickable
        Try
            Dim res = VisualTreeHelper.HitTest(Lienzo2D, ptCanvas)
            If res IsNot Nothing AndAlso res.VisualHit IsNot Nothing Then
                Dim sh = TryCast(res.VisualHit, Shapes.Shape)
                If sh IsNot Nothing Then Return TryCast(sh.Tag, E2kPickable)
            End If
        Catch
        End Try
        Return Nothing
    End Function

    ' ─── Info card: formato ─────────────────────────────────────
    Private Sub PoblarInfoCard(p As E2kPickable,
                                chip As Shapes.Rectangle,
                                lblTipo As TextBlock,
                                lblLabel As TextBlock,
                                lblSeccion As TextBlock,
                                lblExtra As TextBlock)
        chip.Fill = New SolidColorBrush(ColorPorTipo(p.Tipo))
        lblTipo.Text = p.Tipo.ToUpperInvariant()
        lblLabel.Text = If(String.IsNullOrEmpty(p.Label), "(sin etiqueta)", p.Label)
        If Not String.IsNullOrEmpty(p.Section) Then
            lblSeccion.Text = p.Section
            lblSeccion.Visibility = Visibility.Visible
        Else
            lblSeccion.Visibility = Visibility.Collapsed
        End If
        Dim extras As New List(Of String)
        If p.SectionB > 0 AndAlso p.SectionD > 0 Then
            extras.Add($"B×D = {p.SectionB * 100:F0}×{p.SectionD * 100:F0} cm")
        End If
        If Not String.IsNullOrEmpty(p.Story) Then extras.Add("Piso " & p.Story)
        If extras.Count > 0 Then
            lblExtra.Text = String.Join("  ·  ", extras)
            lblExtra.Visibility = Visibility.Visible
        Else
            lblExtra.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Function ColorPorTipo(tipo As String) As Color
        Select Case tipo
            Case "Columna" : Return E2k3DPalette.Columna
            Case "Viga" : Return E2k3DPalette.Viga
            Case "Nervio" : Return E2k3DPalette.Nervio
            Case "Muro" : Return Color.FromRgb(214, 158, 46)
            Case Else : Return E2k3DPalette.GridEtiqueta
        End Select
    End Function

    ' ─── Etiquetas sobre canvas 2D ──────────────────────────────
    Private Sub EtqFrames_Changed(sender As Object, e As RoutedEventArgs)
        If _modoActual = "Planta" OrElse _modoActual = "Alzado" Then Redibujar2D()
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' Etiquetas de ejes (grids)
    ' ═════════════════════════════════════════════════════════════

    Private Sub EtqEjes_Changed(sender As Object, e As RoutedEventArgs)
        ActualizarLabelsEjes3D()
        If _modoActual = "Planta" OrElse _modoActual = "Alzado" Then Redibujar2D()
    End Sub

    ''' <summary>
    ''' Cambia entre modo Extruido (B×D reales) y Líneas (ejes centrales). En
    ''' Líneas, el sceneBuilder arma cajas de 2 cm × 2 cm para columnas/vigas/
    ''' nervios: reusa el mismo pipeline (rango de vértices y triángulos
    ''' preservado, por eso hover/selección siguen funcionando). Muros y losas
    ''' se mantienen planos en ambos modos.
    ''' </summary>
    Private Sub DibujoModo_Changed(sender As Object, e As RoutedEventArgs)
        If Not IsLoaded Then Return
        If _modelo Is Nothing Then Return
        Dim nuevo = (RbLineas IsNot Nothing AndAlso RbLineas.IsChecked.GetValueOrDefault())
        If nuevo = _modoLineas Then Return
        _modoLineas = nuevo
        ' Rebuild la escena 3D con el nuevo modo
        _escena = E2kSceneBuilder.Construir(_modelo, _modoLineas)
        ConstruirIndicePicking()
        ConstruirEscena3D()
        ' Overlay 3D labels se recalcula (los bounds no cambiaron, pero por si acaso)
        ActualizarLabelsEjes3D()
        ' Selección/hover: la referencia vieja apunta a pickables de otra escena
        _hoverActual = Nothing
        _seleccionado = Nothing
        HoverVisual.Content = Nothing
        SeleccionVisual.Content = Nothing
        PanelSeleccion.Visibility = Visibility.Collapsed
        ' 2D también cambia la representación de columnas
        If _modoActual <> "3D" Then Redibujar2D()
    End Sub

    Private Sub CamaraCambio(sender As Object, e As EventArgs)
        ActualizarLabelsEjes3D()
    End Sub

    Private Sub OverlayEjes3D_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        ActualizarLabelsEjes3D()
    End Sub

    ''' <summary>
    ''' Limpia el overlay y reproyecta cada endpoint de grid (+ un pequeño
    ''' offset hacia afuera para que la etiqueta no pise la línea).
    ''' Se llama en cada cambio de cámara / resize / toggle.
    '''
    ''' Nota: puede dispararse ANTES de que OnLoaded termine (WPF mide el
    ''' layout y lanza SizeChanged en el Canvas), por lo que validamos
    ''' explícitamente _escena y Camara antes de tocarlos.
    ''' </summary>
    Private Sub ActualizarLabelsEjes3D()
        Try
            If OverlayEjes3D Is Nothing Then Return
            OverlayEjes3D.Children.Clear()
            If _modelo Is Nothing OrElse _modelo.Grids Is Nothing OrElse _modelo.Grids.Count = 0 Then Return
            If _escena Is Nothing Then Return
            If Camara Is Nothing Then Return
            If ChkGrids Is Nothing OrElse Not ChkGrids.IsChecked.GetValueOrDefault() Then Return
            If ChkEtqEjes Is Nothing OrElse Not ChkEtqEjes.IsChecked.GetValueOrDefault() Then Return
            If _modoActual <> "3D" Then Return

            Dim zbase As Double = 0.0
            If _modelo.Stories IsNot Nothing AndAlso _modelo.Stories.Count > 0 Then
                zbase = _modelo.Stories.Min(Function(s) s.ElevBottom)
            End If

            ' Offset hacia afuera (en metros) para que la burbuja no pise la línea
            Dim escala = Math.Max(_escena.Bounds.SizeX, _escena.Bounds.SizeZ)
            If Double.IsInfinity(escala) OrElse Double.IsNaN(escala) OrElse escala <= 0 Then escala = 10.0
            Dim offsetMetros = Math.Max(0.5, escala * 0.015)

            For Each g In _modelo.Grids
                If g Is Nothing OrElse String.IsNullOrEmpty(g.Label) Then Continue For
                Dim mx = (g.X1 + g.X2) / 2.0
                Dim my = (g.Y1 + g.Y2) / 2.0
                ' Dos extremos (sin ValueTuple, para evitar dependencia de System.ValueTuple)
                For i = 0 To 1
                    Dim ex0 = If(i = 0, g.X1, g.X2)
                    Dim ey0 = If(i = 0, g.Y1, g.Y2)
                    Dim dx = ex0 - mx, dy = ey0 - my
                    Dim L = Math.Sqrt(dx * dx + dy * dy)
                    If L < 1.0E-6 Then Continue For
                    Dim ex = ex0 + dx / L * offsetMetros
                    Dim ey = ey0 + dy / L * offsetMetros
                    AgregarLabelEje3D(E2kSceneBuilder.Mapear(ex, ey, zbase), g.Label)
                Next
            Next
        Catch ex As Exception
            Logger.Error(ex, "Form_VisualizadorE2K_WPF.ActualizarLabelsEjes3D")
        End Try
    End Sub

    Private Sub AgregarLabelEje3D(worldPt As Point3D, texto As String)
        Dim pScreen = ProyectarPunto3D(worldPt)
        If Not pScreen.HasValue Then Return
        Dim fuente = TryCast(TryFindResource("FuenteDisplay"), FontFamily)
        Dim tb As New TextBlock() With {
            .Text = texto,
            .FontSize = 11,
            .FontWeight = FontWeights.SemiBold,
            .Foreground = New SolidColorBrush(E2k3DPalette.GridEtiqueta),
            .Background = New SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
            .Padding = New Thickness(5, 1, 5, 1)
        }
        If fuente IsNot Nothing Then tb.FontFamily = fuente
        tb.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
        Canvas.SetLeft(tb, pScreen.Value.X - tb.DesiredSize.Width / 2)
        Canvas.SetTop(tb, pScreen.Value.Y - tb.DesiredSize.Height / 2)
        OverlayEjes3D.Children.Add(tb)
    End Sub

    ''' <summary>
    ''' Proyecta un punto en coords WPF (X, Y-up, Z) a coords de pantalla del
    ''' Viewport3D. Devuelve Nothing si el punto está detrás de la cámara.
    ''' Asume FOV horizontal (convención de WPF PerspectiveCamera).
    ''' </summary>
    Private Function ProyectarPunto3D(wpt As Point3D) As Point?
        If Camara Is Nothing Then Return Nothing
        Dim w = Viewport3D.ActualWidth, h = Viewport3D.ActualHeight
        If w < 2 OrElse h < 2 Then Return Nothing

        Dim look = Camara.LookDirection
        If look.Length < 1.0E-6 Then Return Nothing
        look.Normalize()
        Dim up0 = Camara.UpDirection
        up0.Normalize()
        Dim right = Vector3D.CrossProduct(look, up0)
        If right.Length < 1.0E-6 Then Return Nothing
        right.Normalize()
        Dim up = Vector3D.CrossProduct(right, look)
        up.Normalize()

        Dim pos = Camara.Position
        Dim rel = New Vector3D(wpt.X - pos.X, wpt.Y - pos.Y, wpt.Z - pos.Z)
        Dim x = Vector3D.DotProduct(rel, right)
        Dim y = Vector3D.DotProduct(rel, up)
        Dim z = Vector3D.DotProduct(rel, look)
        If z <= 0.1 Then Return Nothing   ' detrás de la cámara o demasiado cerca

        Dim fovH = Camara.FieldOfView * Math.PI / 180.0
        Dim tanHalf = Math.Tan(fovH / 2.0)
        Dim aspect = w / h
        Dim ndcX = x / (z * tanHalf)
        Dim ndcY = y / (z * tanHalf / aspect)

        Dim sx = (ndcX + 1.0) * 0.5 * w
        Dim sy = (1.0 - ndcY) * 0.5 * h
        Return New Point(sx, sy)
    End Function

End Class
