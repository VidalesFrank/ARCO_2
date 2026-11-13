Imports System.Windows
Imports System.Windows.Input
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Media3D

''' <summary>
''' Cámara orbital para el visualizador 3D. Rota alrededor de un pivote
''' (<see cref="Target"/>) por coordenadas esféricas (azimuth, elevation,
''' distance). Acopla eventos de ratón a una UIElement anfitriona:
''' arrastrar = orbitar, Shift+arrastrar o rueda central = pan, rueda = zoom.
''' Soporta presets animados (iso, planta, elevaciones).
''' </summary>
Public Class OrbitalCameraController

    Public Property Target As Point3D = New Point3D(0, 0, 0)
    Public Property Azimuth As Double = 35.0
    Public Property Elevation As Double = 25.0
    Public Property Distance As Double = 100.0
    Public Property Camera As PerspectiveCamera

    Private Const Azimuth_Sensibilidad    As Double = 0.4
    Private Const Elevation_Sensibilidad  As Double = 0.4
    Private Const Zoom_FactorPaso         As Double = 1.12
    Private Const Elevation_MaxAbs        As Double = 89.5
    Private Const Pan_Sensibilidad        As Double = 0.0016  ' escala relativa a distance

    Private _ui As UIElement
    Private _ultimoMouse As Point
    Private _orbitando As Boolean = False
    Private _paneando As Boolean = False

    Public Event CameraChanged As EventHandler

    Public Sub Attach(cam As PerspectiveCamera, ui As UIElement)
        Me.Camera = cam
        Me._ui = ui
        AddHandler ui.MouseDown, AddressOf OnMouseDown
        AddHandler ui.MouseMove, AddressOf OnMouseMove
        AddHandler ui.MouseUp, AddressOf OnMouseUp
        AddHandler ui.MouseWheel, AddressOf OnMouseWheel
        UpdateCamera()
    End Sub

    ''' <summary>Suelta los handlers de mouse registrados en Attach.
    ''' Usar al reemplazar el controlador para que el antiguo no siga
    ''' procesando eventos sobre el mismo UI.</summary>
    Public Sub Detach()
        If _ui Is Nothing Then Return
        RemoveHandler _ui.MouseDown, AddressOf OnMouseDown
        RemoveHandler _ui.MouseMove, AddressOf OnMouseMove
        RemoveHandler _ui.MouseUp, AddressOf OnMouseUp
        RemoveHandler _ui.MouseWheel, AddressOf OnMouseWheel
        _ui = Nothing
    End Sub

    ''' <summary>
    ''' Recalcula Position/LookDirection/UpDirection a partir de las
    ''' coordenadas esféricas actuales. Llamar tras modificar Azimuth,
    ''' Elevation, Distance o Target.
    ''' </summary>
    Public Sub UpdateCamera()
        If Camera Is Nothing Then Return
        Dim az = Azimuth * Math.PI / 180.0
        Dim el = Elevation * Math.PI / 180.0
        Dim r = Math.Max(Distance, 0.5)

        Dim cosEl = Math.Cos(el)
        Dim pos As New Point3D(
            Target.X + r * cosEl * Math.Sin(az),
            Target.Y + r * Math.Sin(el),
            Target.Z + r * cosEl * Math.Cos(az))

        Camera.Position = pos
        Camera.LookDirection = New Vector3D(
            Target.X - pos.X, Target.Y - pos.Y, Target.Z - pos.Z)
        Camera.UpDirection = New Vector3D(0, 1, 0)
        RaiseEvent CameraChanged(Me, EventArgs.Empty)
    End Sub

    ' ── Presets ───────────────────────────────────────────────────

    Public Sub Encuadrar(bounds As Rect3D, Optional margen As Double = 1.25)
        If bounds.IsEmpty Then Return
        Target = New Point3D(bounds.X + bounds.SizeX / 2,
                              bounds.Y + bounds.SizeY / 2,
                              bounds.Z + bounds.SizeZ / 2)
        Dim diag = Math.Sqrt(bounds.SizeX ^ 2 + bounds.SizeY ^ 2 + bounds.SizeZ ^ 2)
        Distance = Math.Max(diag * margen, 1.0)
        UpdateCamera()
    End Sub

    Public Sub VistaIsometrica(bounds As Rect3D)
        Azimuth = 35.0 : Elevation = 25.0
        Encuadrar(bounds)
    End Sub

    Public Sub VistaPlanta(bounds As Rect3D)
        Azimuth = 0.0 : Elevation = 89.0
        Encuadrar(bounds, 1.1)
    End Sub

    Public Sub VistaFrontal(bounds As Rect3D)
        Azimuth = 0.0 : Elevation = 0.0
        Encuadrar(bounds, 1.1)
    End Sub

    Public Sub VistaLateral(bounds As Rect3D)
        Azimuth = 90.0 : Elevation = 0.0
        Encuadrar(bounds, 1.1)
    End Sub

    ' ── Handlers de ratón ────────────────────────────────────────

    Private Sub OnMouseDown(sender As Object, e As MouseButtonEventArgs)
        _ultimoMouse = e.GetPosition(_ui)
        If e.ChangedButton = MouseButton.Left Then
            If Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) Then
                _paneando = True
            Else
                _orbitando = True
            End If
            _ui.CaptureMouse()
        ElseIf e.ChangedButton = MouseButton.Middle Then
            _paneando = True
            _ui.CaptureMouse()
        End If
    End Sub

    Private Sub OnMouseMove(sender As Object, e As MouseEventArgs)
        If Not (_orbitando Or _paneando) Then Return
        Dim actual = e.GetPosition(_ui)
        Dim dx = actual.X - _ultimoMouse.X
        Dim dy = actual.Y - _ultimoMouse.Y
        _ultimoMouse = actual

        If _orbitando Then
            Azimuth = (Azimuth - dx * Azimuth_Sensibilidad) Mod 360.0
            Elevation = Math.Max(-Elevation_MaxAbs,
                                 Math.Min(Elevation_MaxAbs, Elevation + dy * Elevation_Sensibilidad))
            UpdateCamera()
        ElseIf _paneando Then
            ' Pan: trasladar el target en el plano de la cámara.
            Dim fwd = Camera.LookDirection
            fwd.Normalize()
            Dim right = Vector3D.CrossProduct(fwd, Camera.UpDirection)
            right.Normalize()
            Dim up = Vector3D.CrossProduct(right, fwd)
            up.Normalize()
            Dim factor = Distance * Pan_Sensibilidad
            Dim delta As Vector3D = (right * (-dx)) + (up * dy)
            Target = New Point3D(Target.X + delta.X * factor,
                                  Target.Y + delta.Y * factor,
                                  Target.Z + delta.Z * factor)
            UpdateCamera()
        End If
    End Sub

    Private Sub OnMouseUp(sender As Object, e As MouseButtonEventArgs)
        If _orbitando OrElse _paneando Then
            _orbitando = False
            _paneando = False
            _ui.ReleaseMouseCapture()
        End If
    End Sub

    Private Sub OnMouseWheel(sender As Object, e As MouseWheelEventArgs)
        Dim pasos = e.Delta / 120.0
        If pasos > 0 Then
            Distance /= Math.Pow(Zoom_FactorPaso, pasos)
        Else
            Distance *= Math.Pow(Zoom_FactorPaso, -pasos)
        End If
        Distance = Math.Max(0.5, Distance)
        UpdateCamera()
        e.Handled = True
    End Sub

End Class
