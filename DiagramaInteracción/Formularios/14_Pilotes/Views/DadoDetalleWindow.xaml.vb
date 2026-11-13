Imports System.Data
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Shapes

''' <summary>
''' Ventana modal de detalle de un apoyo. Muestra mini-planta del dado con
''' pilotes numerados y coloreados por su propio C/D, matriz de fuerzas
''' pilote×combinación, y resumen por pilote individual.
''' </summary>
Partial Public Class DadoDetalleWindow
    Inherits Window

    Private ReadOnly _vm As DadoDetalleVM

    Public Sub New(dado As Dado, contenedor As cPilotes)
        InitializeComponent()
        _vm = New DadoDetalleVM(dado, contenedor)
        DataContext = _vm

        ' La matriz es un DataTable con columnas dinámicas. Binding manual.
        GridMatriz.ItemsSource = _vm.MatrizCombinaciones.DefaultView

        AddHandler Loaded, Sub(s, e) DibujarDado()
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    Private Sub CanvasDado_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        DibujarDado()
    End Sub

    ''' <summary>
    ''' Dibuja el polígono del dado con los pilotes numerados dentro. Cada
    ''' pilote se colorea por su propio C/D (peor entre comp y tracc) para
    ''' que el usuario identifique visualmente los pilotes gobernantes.
    ''' Se centra y escala automáticamente al tamaño del canvas.
    ''' </summary>
    Private Sub DibujarDado()
        If CanvasDado Is Nothing OrElse _vm Is Nothing Then Return
        CanvasDado.Children.Clear()

        Dim td = _vm.TipoDado
        If td Is Nothing Then
            Dim lbl As New TextBlock With {
                .Text = "El apoyo no tiene dado asignado.",
                .Foreground = BuscarBrush("BrushTextoSecundario"),
                .FontSize = 13
            }
            lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            Canvas.SetLeft(lbl, (CanvasDado.ActualWidth - lbl.DesiredSize.Width) * 0.5)
            Canvas.SetTop(lbl, (CanvasDado.ActualHeight - lbl.DesiredSize.Height) * 0.5)
            CanvasDado.Children.Add(lbl)
            Return
        End If

        Dim w = CanvasDado.ActualWidth, h = CanvasDado.ActualHeight
        If w < 50 OrElse h < 50 Then Return

        ' Extents en coords locales del dado
        Dim poli = td.Poligono
        If poli Is Nothing OrElse poli.Count < 3 Then Return
        Dim xs = poli.Select(Function(p) p.X)
        Dim ys = poli.Select(Function(p) p.Y)
        Dim minX = xs.Min(), maxX = xs.Max()
        Dim minY = ys.Min(), maxY = ys.Max()
        Dim spanX = Math.Max(0.1, maxX - minX)
        Dim spanY = Math.Max(0.1, maxY - minY)

        Dim margen As Double = 0.15
        Dim escX = w / (spanX * (1 + 2 * margen))
        Dim escY = h / (spanY * (1 + 2 * margen))
        Dim escala = Math.Min(escX, escY)
        Dim midX = (minX + maxX) * 0.5
        Dim midY = (minY + maxY) * 0.5

        Dim mX = Function(xm As Double) (xm - midX) * escala + w * 0.5
        Dim mY = Function(ym As Double) -(ym - midY) * escala + h * 0.5

        ' Grid cada 0.5 m
        Dim brushGrid = BuscarBrush("BrushCanvasGrid")
        Dim xs0 = Math.Floor(minX * 2) / 2
        Dim xs1 = Math.Ceiling(maxX * 2) / 2
        Dim ys0 = Math.Floor(minY * 2) / 2
        Dim ys1 = Math.Ceiling(maxY * 2) / 2
        Dim g = xs0
        Do While g <= xs1
            Dim px = mX(g)
            CanvasDado.Children.Add(New Line With {
                .X1 = px, .X2 = px, .Y1 = 0, .Y2 = h,
                .Stroke = brushGrid, .StrokeThickness = 0.5,
                .StrokeDashArray = New DoubleCollection({2, 3})
            })
            g += 0.5
        Loop
        g = ys0
        Do While g <= ys1
            Dim py = mY(g)
            CanvasDado.Children.Add(New Line With {
                .X1 = 0, .X2 = w, .Y1 = py, .Y2 = py,
                .Stroke = brushGrid, .StrokeThickness = 0.5,
                .StrokeDashArray = New DoubleCollection({2, 3})
            })
            g += 0.5
        Loop

        ' Polígono del dado
        Dim pts As New PointCollection()
        For Each v In poli
            pts.Add(New Point(mX(v.X), mY(v.Y)))
        Next
        CanvasDado.Children.Add(New Polygon With {
            .Points = pts,
            .Fill = BuscarBrush("BrushDadoRelleno"),
            .Stroke = BuscarBrush("BrushDadoBorde"),
            .StrokeThickness = 2,
            .StrokeLineJoin = PenLineJoin.Round,
            .Opacity = 0.9
        })

        ' Pilotes numerados, coloreados por peor C/D individual
        For i = 0 To td.Pilotes.Count - 1
            Dim pp = td.Pilotes(i)
            Dim tp = _vm.Contenedor?.BuscarTipoPilote(pp.NombreTipoPilote)
            Dim radioM = If(tp IsNot Nothing, tp.Diametro * 0.5, 0.15)
            Dim radioPx = Math.Max(10, radioM * escala)

            Dim colorPilote = BuscarBrush("BrushPiloteRelleno")
            If _vm.Resultado.PorPilote.Count > i Then
                colorPilote = ColorPorCD(_vm.Resultado.PorPilote(i))
            End If

            Dim px = mX(pp.X_Local), py = mY(pp.Y_Local)
            Dim el As New Ellipse With {
                .Width = radioPx * 2, .Height = radioPx * 2,
                .Fill = colorPilote,
                .Stroke = Brushes.White, .StrokeThickness = 2.5
            }
            Canvas.SetLeft(el, px - radioPx)
            Canvas.SetTop(el, py - radioPx)
            CanvasDado.Children.Add(el)

            Dim lbl As New TextBlock With {
                .Text = $"P{i + 1}",
                .FontSize = Math.Max(11, Math.Min(16, radioPx * 0.9)),
                .FontFamily = New FontFamily("Segoe UI Semibold"),
                .Foreground = Brushes.White,
                .IsHitTestVisible = False
            }
            lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            Canvas.SetLeft(lbl, px - lbl.DesiredSize.Width * 0.5)
            Canvas.SetTop(lbl, py - lbl.DesiredSize.Height * 0.5)
            CanvasDado.Children.Add(lbl)
        Next

        ' Centroide del grupo
        If td.Pilotes.Count > 0 Then
            Dim cx = td.Pilotes.Average(Function(p) p.X_Local)
            Dim cy = td.Pilotes.Average(Function(p) p.Y_Local)
            Dim px = mX(cx), py = mY(cy)
            Dim brush = BuscarBrush("BrushCentroideGrupo")
            CanvasDado.Children.Add(New Line With {
                .X1 = px - 10, .X2 = px + 10, .Y1 = py, .Y2 = py,
                .Stroke = brush, .StrokeThickness = 2.5
            })
            CanvasDado.Children.Add(New Line With {
                .X1 = px, .X2 = px, .Y1 = py - 10, .Y2 = py + 10,
                .Stroke = brush, .StrokeThickness = 2.5
            })
        End If
    End Sub

    Private Function ColorPorCD(rp As PiloteService.ResumenPilote) As Brush
        Dim umbral = 0.9
        If String.IsNullOrEmpty(rp.Combo_Max) AndAlso rp.CD_Compresion <= 0 AndAlso rp.CD_Traccion <= 0 Then
            Return BuscarBrush("BrushNoAplica")
        End If
        If (rp.CD_Compresion > 0 AndAlso rp.CD_Compresion < umbral) OrElse
           (rp.CD_Traccion > 0 AndAlso rp.CD_Traccion < umbral) Then
            Return BuscarBrush("BrushNoCumple")
        End If
        If (rp.CD_Compresion > 0 AndAlso rp.CD_Compresion < 1.0) OrElse
           (rp.CD_Traccion > 0 AndAlso rp.CD_Traccion < 1.0) Then
            Return BuscarBrush("BrushAdvertencia")
        End If
        Return BuscarBrush("BrushCumple")
    End Function

    Private Function BuscarBrush(key As String) As Brush
        If Application.Current IsNot Nothing AndAlso Application.Current.Resources.Contains(key) Then
            Return CType(Application.Current.Resources(key), Brush)
        End If
        If Resources.Contains(key) Then Return CType(Resources(key), Brush)
        Return Brushes.Gray
    End Function

End Class
