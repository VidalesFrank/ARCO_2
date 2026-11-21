Imports System.Data
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Media.Media3D
Imports System.Windows.Shapes
Imports HelixToolkit.Wpf

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

        AddHandler Loaded, Sub(s, e)
                               DibujarDado()
                               DibujarSTM()
                               DibujarEscena3D()
                           End Sub
    End Sub

    Private Sub Chk3D_Toggle(sender As Object, e As RoutedEventArgs)
        DibujarEscena3D()
    End Sub

    Private Sub Btn3D_AjustarVista_Click(sender As Object, e As RoutedEventArgs)
        If Viewport3D IsNot Nothing Then Viewport3D.ZoomExtents(500)
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As RoutedEventArgs)
        Close()
    End Sub

    Private Sub CanvasDado_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        DibujarDado()
    End Sub

    Private Sub CanvasSTM_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        DibujarSTM()
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
        CanvasDado.Children.Add(New System.Windows.Shapes.Polygon With {
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

    ''' <summary>
    ''' Dibuja la elevación del modelo STM: silueta del dado + columna arriba,
    ''' puntales desde el nodo superior hasta cada pilote (líneas rojas con
    ''' grosor proporcional a la fuerza y coloreadas por C/D cuando no cumple),
    ''' tensores horizontales inferiores (líneas azules), nodos como esferas
    ''' y etiquetas con la fuerza de cada puntal y el ángulo.
    '''
    ''' Proyección: pilotes al eje X local (los que están "detrás" se solapan).
    ''' Para dados con pilotes en 2D se ve igual que si estuvieran en línea —
    ''' la vista 3D real llega en la Fase 7.
    ''' </summary>
    Private Sub DibujarSTM()
        If CanvasSTM Is Nothing OrElse _vm Is Nothing Then Return
        CanvasSTM.Children.Clear()

        Dim td = _vm.TipoDado
        Dim stm = _vm.STM
        If td Is Nothing OrElse stm Is Nothing OrElse stm.Modelo Is Nothing OrElse
           stm.Modelo.NodoSuperior Is Nothing Then
            Dim lbl As New TextBlock With {
                .Text = "Sin modelo STM disponible.",
                .Foreground = BuscarBrush("BrushTextoSecundario"),
                .FontSize = 13
            }
            lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            Canvas.SetLeft(lbl, (CanvasSTM.ActualWidth - lbl.DesiredSize.Width) * 0.5)
            Canvas.SetTop(lbl, (CanvasSTM.ActualHeight - lbl.DesiredSize.Height) * 0.5)
            CanvasSTM.Children.Add(lbl)
            Return
        End If

        Dim w = CanvasSTM.ActualWidth, h = CanvasSTM.ActualHeight
        If w < 50 OrElse h < 50 Then Return

        ' Extents en el plano XZ.
        Dim xs = td.Poligono.Select(Function(p) p.X)
        Dim minX = xs.Min(), maxX = xs.Max()
        Dim minZ = 0.0
        Dim maxZ = td.Espesor + Math.Max(0.6, td.Espesor * 0.6)   ' columna arriba
        Dim spanX = Math.Max(0.1, maxX - minX)
        Dim spanZ = Math.Max(0.1, maxZ - minZ)
        Dim margen As Double = 0.18
        Dim escX = w / (spanX * (1 + 2 * margen))
        Dim escZ = h / (spanZ * (1 + 2 * margen))
        Dim escala = Math.Min(escX, escZ)
        Dim midX = (minX + maxX) * 0.5
        Dim midZ = (minZ + maxZ) * 0.5
        Dim mX = Function(xm As Double) (xm - midX) * escala + w * 0.5
        Dim mZ = Function(zm As Double) -(zm - midZ) * escala + h * 0.5

        ' Silueta del dado (rectángulo en elevación: minX..maxX, 0..Espesor)
        Dim dado As New Rectangle With {
            .Width = (maxX - minX) * escala,
            .Height = td.Espesor * escala,
            .Fill = BuscarBrush("BrushDadoRelleno"),
            .Stroke = BuscarBrush("BrushDadoBorde"),
            .StrokeThickness = 2,
            .Opacity = 0.45
        }
        Canvas.SetLeft(dado, mX(minX))
        Canvas.SetTop(dado, mZ(td.Espesor))
        CanvasSTM.Children.Add(dado)

        ' Columna arriba (centro, LadoColumna_X ancho)
        Dim colAlto As Double = Math.Min(spanZ - td.Espesor, 0.6)
        Dim col As New Rectangle With {
            .Width = td.LadoColumna_X * escala,
            .Height = colAlto * escala,
            .Fill = BuscarBrush("BrushDadoBorde"),
            .Stroke = BuscarBrush("BrushDadoBorde"),
            .Opacity = 0.65
        }
        Canvas.SetLeft(col, mX(-td.LadoColumna_X * 0.5))
        Canvas.SetTop(col, mZ(td.Espesor + colAlto))
        CanvasSTM.Children.Add(col)

        ' Línea del nivel del acero inferior (tensores)
        CanvasSTM.Children.Add(New Line With {
            .X1 = mX(minX), .X2 = mX(maxX),
            .Y1 = mZ(td.Recubrimiento), .Y2 = mZ(td.Recubrimiento),
            .Stroke = BuscarBrush("BrushCanvasGrid"),
            .StrokeThickness = 0.8,
            .StrokeDashArray = New DoubleCollection({3, 3})
        })

        ' Puntales (de nodo superior a cada nodo inferior).
        Dim maxF = If(stm.Modelo.Puntales.Count > 0, stm.Modelo.Puntales.Max(Function(p) p.Fuerza), 1)
        Dim brushPuntalOK = BuscarBrush("BrushAzulPrincipal")
        For Each p In stm.Modelo.Puntales
            Dim grosor = 2 + 4 * (p.Fuerza / Math.Max(1.0, maxF))
            Dim stroke As Brush
            Select Case True
                Case p.CD >= 1.0   : stroke = BuscarBrush("BrushCumple")
                Case p.CD >= 0.9   : stroke = BuscarBrush("BrushAdvertencia")
                Case p.CD > 0      : stroke = BuscarBrush("BrushNoCumple")
                Case Else          : stroke = brushPuntalOK
            End Select
            CanvasSTM.Children.Add(New Line With {
                .X1 = mX(p.NodoSup.X), .Y1 = mZ(p.NodoSup.Z),
                .X2 = mX(p.NodoInf.X), .Y2 = mZ(p.NodoInf.Z),
                .Stroke = stroke, .StrokeThickness = grosor,
                .StrokeStartLineCap = PenLineCap.Round,
                .StrokeEndLineCap = PenLineCap.Round
            })
        Next

        ' Tensores X (en elevación se ven como líneas horizontales en Z=rec).
        Dim brushTensor = BuscarBrush("BrushAdvertencia")
        For Each t In stm.Modelo.TensoresX
            CanvasSTM.Children.Add(New Line With {
                .X1 = mX(t.NodoInfA.X), .Y1 = mZ(t.NodoInfA.Z),
                .X2 = mX(t.NodoInfB.X), .Y2 = mZ(t.NodoInfB.Z),
                .Stroke = brushTensor, .StrokeThickness = 3.5,
                .StrokeDashArray = New DoubleCollection({6, 3}),
                .StrokeStartLineCap = PenLineCap.Round,
                .StrokeEndLineCap = PenLineCap.Round
            })
        Next

        ' Nodo superior
        DibujarNodo(mX(stm.Modelo.NodoSuperior.X), mZ(stm.Modelo.NodoSuperior.Z),
                    "CCC", BuscarBrush("BrushTextoPrincipal"))

        ' Nodos inferiores
        For Each n In stm.Modelo.NodosInferiores
            DibujarNodo(mX(n.X), mZ(n.Z), "CCT", BuscarBrush("BrushAzulPrincipal"))
        Next

        ' Etiquetas de fuerza en el centro de cada puntal.
        For Each p In stm.Modelo.Puntales
            Dim cxLbl = (mX(p.NodoSup.X) + mX(p.NodoInf.X)) * 0.5
            Dim cyLbl = (mZ(p.NodoSup.Z) + mZ(p.NodoInf.Z)) * 0.5
            Dim lbl As New TextBlock With {
                .Text = $"{p.Fuerza:0} kN · θ{p.Angulo:0}°",
                .FontSize = 10,
                .FontFamily = New FontFamily("Segoe UI"),
                .Foreground = BuscarBrush("BrushTextoPrincipal"),
                .Background = New SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                .Padding = New Thickness(3, 1, 3, 1)
            }
            lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
            Canvas.SetLeft(lbl, cxLbl - lbl.DesiredSize.Width * 0.5)
            Canvas.SetTop(lbl, cyLbl - lbl.DesiredSize.Height * 0.5)
            CanvasSTM.Children.Add(lbl)
        Next
    End Sub

    ''' <summary>
    ''' Construye la escena 3D con HelixToolkit: polígono extruido del dado +
    ''' pilotes como cilindros + columna arriba + modelo STM superpuesto
    ''' (puntales como líneas gruesas, tensores inferiores, nodos como esferas).
    '''
    ''' Convención HelixToolkit coincide con nuestra convención local: X e Y
    ''' horizontales, Z vertical hacia arriba. El polígono del dado queda en
    ''' Z ∈ [0, Espesor], los pilotes bajan desde Z=0 hacia Z negativo.
    ''' </summary>
    Private Sub DibujarEscena3D()
        If Viewport3D Is Nothing OrElse _vm Is Nothing Then Return

        ' Limpiar solo los visuals de contenido, no DefaultLights ni ViewCube.
        Dim aEliminar As New List(Of Visual3D)
        For Each v In Viewport3D.Children
            If TypeOf v Is ModelVisual3D AndAlso Not (TypeOf v Is DefaultLights) Then
                aEliminar.Add(v)
            End If
            If TypeOf v Is LinesVisual3D OrElse TypeOf v Is SphereVisual3D OrElse
               TypeOf v Is BoxVisual3D OrElse TypeOf v Is TruncatedConeVisual3D OrElse
               TypeOf v Is MeshVisual3D Then
                aEliminar.Add(v)
            End If
        Next
        For Each v In aEliminar : Viewport3D.Children.Remove(v) : Next

        Dim td = _vm.TipoDado
        If td Is Nothing OrElse td.Poligono.Count < 3 Then Return

        ' ── Dado: polígono extruido en Z [0, Espesor] ─────────────
        If Chk3D_Dado IsNot Nothing AndAlso Chk3D_Dado.IsChecked = True Then
            Dim mb As New MeshBuilder(False, False)
            Dim pts As New List(Of Point)
            For Each v In td.Poligono
                pts.Add(New Point(v.X, v.Y))
            Next
            mb.AddExtrudedGeometry(pts,
                                   New Vector3D(0, 1, 0),
                                   New Point3D(0, 0, 0),
                                   New Point3D(0, 0, td.Espesor))
            Dim mesh As New MeshVisual3D With {
                .Content = New GeometryModel3D(mb.ToMesh(), MaterialFromGris(&H9C, 0.85)),
                .EdgeDiameter = 0.015,
                .VertexRadius = 0.0,
                .EdgeMaterial = MaterialFromGris(&H3C)
            }
            Viewport3D.Children.Add(mesh)
        End If

        ' ── Pilotes como cilindros hacia abajo ────────────────────
        If Chk3D_Pilotes IsNot Nothing AndAlso Chk3D_Pilotes.IsChecked = True Then
            For Each pp In td.Pilotes
                Dim tp = _vm.Contenedor?.BuscarTipoPilote(pp.NombreTipoPilote)
                Dim diam = If(tp IsNot Nothing, tp.Diametro, 0.6)
                Dim largo = If(tp IsNot Nothing, Math.Min(tp.Longitud, 4.0), 2.0)
                Dim cil As New TruncatedConeVisual3D With {
                    .Origin = New Point3D(pp.X_Local, pp.Y_Local, 0),
                    .Normal = New Vector3D(0, 0, -1),
                    .Height = largo,
                    .BaseRadius = diam * 0.5,
                    .TopRadius = diam * 0.5,
                    .Fill = New SolidColorBrush(Color.FromRgb(&H57, &H57, &H57))
                }
                Viewport3D.Children.Add(cil)
            Next
        End If

        ' ── Columna arriba del dado ───────────────────────────────
        If Chk3D_Columna IsNot Nothing AndAlso Chk3D_Columna.IsChecked = True Then
            Dim colAlto As Double = 0.6
            Dim col As New BoxVisual3D With {
                .Center = New Point3D(0, 0, td.Espesor + colAlto * 0.5),
                .Length = td.LadoColumna_X,
                .Width = td.LadoColumna_Y,
                .Height = colAlto,
                .Fill = New SolidColorBrush(Color.FromRgb(&H3C, &H3C, &H3C))
            }
            Viewport3D.Children.Add(col)
        End If

        ' ── Modelo STM encima ─────────────────────────────────────
        Dim stm = _vm.STM
        If Chk3D_STM IsNot Nothing AndAlso Chk3D_STM.IsChecked = True AndAlso
           stm IsNot Nothing AndAlso stm.Modelo IsNot Nothing AndAlso
           stm.Modelo.NodoSuperior IsNot Nothing Then

            Dim rec = td.Recubrimiento
            Dim zSup = td.Espesor
            Dim zInf = rec

            Dim maxF = If(stm.Modelo.Puntales.Count > 0,
                          stm.Modelo.Puntales.Max(Function(p) p.Fuerza), 1.0)

            ' Puntales: líneas de compresión, color por C/D, grosor por fuerza.
            For Each p In stm.Modelo.Puntales
                Dim stroke As Color
                Select Case True
                    Case p.CD >= 1.0 : stroke = Color.FromRgb(&H2A, &H9D, &H5F)
                    Case p.CD >= 0.9 : stroke = Color.FromRgb(&HD7, &HAF, &H5F)
                    Case p.CD > 0    : stroke = Color.FromRgb(&HC5, &H3E, &H3E)
                    Case Else        : stroke = Color.FromRgb(&HCF, &HCF, &HCF)
                End Select
                Dim gr = 2.0 + 4.0 * (p.Fuerza / Math.Max(1.0, maxF))
                Viewport3D.Children.Add(New LinesVisual3D With {
                    .Color = stroke, .Thickness = gr,
                    .Points = New Point3DCollection From {
                        New Point3D(p.NodoSup.X, p.NodoSup.Y, zSup),
                        New Point3D(p.NodoInf.X, p.NodoInf.Y, zInf)
                    }
                })
            Next

            ' Tensores inferiores en ámbar (acero traccionado).
            Dim tensoresTodos As New List(Of TensorSTM)
            tensoresTodos.AddRange(stm.Modelo.TensoresX)
            tensoresTodos.AddRange(stm.Modelo.TensoresY)
            For Each t In tensoresTodos
                Viewport3D.Children.Add(New LinesVisual3D With {
                    .Color = Color.FromRgb(&HE2, &H6F, &H2F), .Thickness = 4,
                    .Points = New Point3DCollection From {
                        New Point3D(t.NodoInfA.X, t.NodoInfA.Y, zInf),
                        New Point3D(t.NodoInfB.X, t.NodoInfB.Y, zInf)
                    }
                })
            Next

            ' Nodos como esferas
            AgregarNodo3D(stm.Modelo.NodoSuperior.X, stm.Modelo.NodoSuperior.Y, zSup,
                          Color.FromRgb(&H1F, &H1F, &H1F), 0.08)
            For Each n In stm.Modelo.NodosInferiores
                AgregarNodo3D(n.X, n.Y, zInf, Color.FromRgb(&H3C, &H3C, &H3C), 0.07)
            Next
        End If

        Viewport3D.ZoomExtents(400)
    End Sub

    Private Function MaterialFromGris(valor As Byte, Optional opacidad As Double = 1.0) As Material
        Dim b As New SolidColorBrush(Color.FromArgb(CByte(opacidad * 255), valor, valor, valor))
        Return MaterialHelper.CreateMaterial(b)
    End Function

    Private Sub AgregarNodo3D(x As Double, y As Double, z As Double, c As Color, r As Double)
        Viewport3D.Children.Add(New SphereVisual3D With {
            .Center = New Point3D(x, y, z),
            .Radius = r,
            .Fill = New SolidColorBrush(c)
        })
    End Sub

    Private Sub DibujarNodo(cx As Double, cy As Double, texto As String, color As Brush)
        Const r As Double = 9
        Dim el As New Ellipse With {
            .Width = r * 2, .Height = r * 2,
            .Fill = color,
            .Stroke = Brushes.White, .StrokeThickness = 2
        }
        Canvas.SetLeft(el, cx - r)
        Canvas.SetTop(el, cy - r)
        CanvasSTM.Children.Add(el)

        Dim lbl As New TextBlock With {
            .Text = texto, .FontSize = 9,
            .FontFamily = New FontFamily("Segoe UI Semibold"),
            .Foreground = Brushes.White,
            .IsHitTestVisible = False
        }
        lbl.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
        Canvas.SetLeft(lbl, cx - lbl.DesiredSize.Width * 0.5)
        Canvas.SetTop(lbl, cy - lbl.DesiredSize.Height * 0.5)
        CanvasSTM.Children.Add(lbl)
    End Sub

End Class
