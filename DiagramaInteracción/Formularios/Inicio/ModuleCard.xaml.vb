Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media

''' <summary>
''' Tarjeta visual para un módulo del catálogo. Recibe un
''' <see cref="ModuleInfo"/> vía DataContext; refleja hover, badge "En
''' desarrollo" y dispara la acción al hacer click.
''' </summary>
Partial Public Class ModuleCard
    Inherits UserControl

    Public Sub New()
        InitializeComponent()
        AddHandler Me.DataContextChanged, AddressOf OnDataContextChanged
        AddHandler Me.MouseLeftButtonUp, AddressOf OnMouseLeftButtonUp
    End Sub

    Private Sub OnDataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim info = TryCast(DataContext, ModuleInfo)
        If info Is Nothing Then Return

        TitleText.Text = info.Nombre
        DescText.Text = info.Descripcion

        ' Geometry desde la clave (recurso de ModuleIcons.xaml)
        Dim geo = TryCast(Me.TryFindResource(info.IconKey), Geometry)
        IconPath.Data = geo

        ' Estado de disponibilidad
        If Not info.Disponible Then
            BadgeDisponibilidad.Visibility = Visibility.Visible
            IconPath.Opacity = 0.35
            TitleText.Opacity = 0.55
            DescText.Opacity = 0.55
            BorderRaiz.Cursor = Cursors.Arrow
        Else
            BadgeDisponibilidad.Visibility = Visibility.Collapsed
            IconPath.Opacity = 1.0
            TitleText.Opacity = 1.0
            DescText.Opacity = 1.0
            BorderRaiz.Cursor = Cursors.Hand
        End If
    End Sub

    Private Sub OnMouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
        Dim info = TryCast(DataContext, ModuleInfo)
        If info Is Nothing Then Return
        If Not info.Disponible OrElse info.Accion Is Nothing Then Return
        Try
            info.Accion.Invoke()
        Catch ex As Exception
            Logger.Error(ex, "ModuleCard.OnMouseLeftButtonUp", info.Id)
        End Try
    End Sub

End Class
