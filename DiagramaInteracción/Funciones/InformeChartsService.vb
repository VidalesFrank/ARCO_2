Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Windows.Forms.DataVisualization.Charting

''' <summary>
''' Genera gráficos (donut de cumplimiento, barras apiladas por piso, dashboard de KPIs)
''' como PNG en bytes, listos para embeber en el .docx del Informe Técnico Ejecutivo con
''' <see cref="ReporteRevisionWordHelpers.ImagenCentrada"/>.
''' </summary>
Public Class InformeChartsService

    ' Paleta corporativa de veredictos — alineada con las plantas (verde / rojo / gris).
    Public Shared ReadOnly ColorCumple As Color = Color.FromArgb(76, 175, 80)
    Public Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(220, 53, 69)
    Public Shared ReadOnly ColorSinDato As Color = Color.FromArgb(170, 170, 170)

    Private Shared ReadOnly ColorTexto As Color = Color.FromArgb(60, 60, 70)
    Private Shared ReadOnly ColorTextoSuave As Color = Color.FromArgb(110, 110, 125)

    ''' <summary>
    ''' Donut premium: segmentos "Cumple / No cumple / Sin dato" + número grande en el
    ''' centro con el % de cumplimiento. Dibujado con GDI+ puro (no Chart) para tener
    ''' control fino sobre tipografía y evitar el aspecto "Office 2003" del Chart por default.
    ''' Devuelve Nothing si no hay ninguna categoría con valor.
    ''' </summary>
    Public Shared Function DonutCumplimiento(nCumple As Integer, nNoCumple As Integer, nSinDato As Integer,
                                             Optional titulo As String = "",
                                             Optional ancho As Integer = 820,
                                             Optional alto As Integer = 440) As Byte()
        Dim total = nCumple + nNoCumple + nSinDato
        If total = 0 Then Return Nothing

        Dim segmentos As New List(Of (Etiqueta As String, Valor As Integer, Color As Color)) From {
            ("Cumplen", nCumple, ColorCumple),
            ("No cumplen", nNoCumple, ColorNoCumple),
            ("Sin dato", nSinDato, ColorSinDato)
        }

        Dim bmp As New Bitmap(ancho, alto)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(Color.White)

            ' --- Título ---
            Dim yTop As Integer = 16
            If Not String.IsNullOrWhiteSpace(titulo) Then
                Using fTit As New Font("Segoe UI Semibold", 13, FontStyle.Bold)
                    Using bTit As New SolidBrush(ColorTexto)
                        Dim sfTit As New StringFormat With {.Alignment = StringAlignment.Center}
                        g.DrawString(titulo, fTit, bTit, New RectangleF(0, yTop, ancho, 24), sfTit)
                    End Using
                End Using
                yTop += 38
            End If

            ' --- Donut al centro-izquierda ---
            Dim diametro As Integer = Math.Min(alto - yTop - 30, 320)
            Dim donutX As Integer = 40
            Dim donutY As Integer = yTop + ((alto - yTop) - diametro) \ 2
            Dim rectExt As New Rectangle(donutX, donutY, diametro, diametro)

            Dim grosor As Single = diametro * 0.26F
            Dim rectInt As New RectangleF(donutX + grosor, donutY + grosor,
                                           diametro - 2 * grosor, diametro - 2 * grosor)

            ' Sombra sutil del anillo
            Using penSombra As New Pen(Color.FromArgb(30, 0, 0, 0), grosor)
                penSombra.StartCap = LineCap.Flat
                penSombra.EndCap = LineCap.Flat
                g.DrawArc(penSombra, rectExt.X + 3, rectExt.Y + 4, rectExt.Width, rectExt.Height, 0, 360)
            End Using

            Dim angulo As Single = -90
            For Each seg In segmentos
                If seg.Valor <= 0 Then Continue For
                Dim barrido As Single = CSng(360.0 * seg.Valor / total)
                Using penSeg As New Pen(seg.Color, grosor)
                    penSeg.StartCap = LineCap.Flat
                    penSeg.EndCap = LineCap.Flat
                    g.DrawArc(penSeg, rectExt.X + grosor / 2, rectExt.Y + grosor / 2,
                              rectExt.Width - grosor, rectExt.Height - grosor,
                              angulo, barrido)
                End Using
                angulo += barrido
            Next

            ' Hueco interior blanco (asegura que el centro quede limpio aun con antialiasing)
            Using bInt As New SolidBrush(Color.White)
                g.FillEllipse(bInt, rectInt)
            End Using

            ' --- Número grande al centro: % de cumplimiento sobre el total ---
            Dim pctCumple As Double = If(total > 0, CDbl(nCumple) / total * 100.0, 0)
            Dim textoCentro As String = pctCumple.ToString("0") & "%"
            Using fPct As New Font("Segoe UI", 44, FontStyle.Bold)
                Using bPct As New SolidBrush(ColorTexto)
                    Dim sf As New StringFormat With {
                        .Alignment = StringAlignment.Center,
                        .LineAlignment = StringAlignment.Center
                    }
                    g.DrawString(textoCentro, fPct, bPct, rectInt, sf)
                End Using
            End Using
            Using fSub As New Font("Segoe UI", 9)
                Using bSub As New SolidBrush(ColorTextoSuave)
                    Dim sfSub As New StringFormat With {.Alignment = StringAlignment.Center}
                    g.DrawString("de cumplimiento", fSub, bSub,
                                 New RectangleF(rectInt.X, rectInt.Y + rectInt.Height * 0.78F, rectInt.Width, 20),
                                 sfSub)
                End Using
            End Using

            ' --- Leyenda a la derecha, cards horizontales con swatch grande ---
            Dim legendX As Integer = donutX + diametro + 50
            Dim legendY As Integer = donutY + 20
            Dim rowH As Integer = 72
            For Each seg In segmentos
                ' Barra de color grande
                Using bSw As New SolidBrush(seg.Color)
                    g.FillRectangle(bSw, legendX, legendY, 6, 50)
                End Using
                ' Valor numérico
                Using fNum As New Font("Segoe UI", 22, FontStyle.Bold)
                    Using bNum As New SolidBrush(ColorTexto)
                        g.DrawString(seg.Valor.ToString(), fNum, bNum, legendX + 16, legendY - 4)
                    End Using
                End Using
                ' Etiqueta + porcentaje
                Dim pctSeg As Double = If(total > 0, CDbl(seg.Valor) / total * 100.0, 0)
                Using fLbl As New Font("Segoe UI", 10)
                    Using bLbl As New SolidBrush(ColorTexto)
                        g.DrawString(seg.Etiqueta, fLbl, bLbl, legendX + 90, legendY + 4)
                    End Using
                End Using
                Using fPct2 As New Font("Segoe UI", 9)
                    Using bPct2 As New SolidBrush(ColorTextoSuave)
                        g.DrawString(pctSeg.ToString("0.0") & " %", fPct2, bPct2, legendX + 90, legendY + 24)
                    End Using
                End Using
                legendY += rowH
            Next
        End Using

        Using ms As New MemoryStream()
            bmp.Save(ms, Imaging.ImageFormat.Png)
            bmp.Dispose()
            Return ms.ToArray()
        End Using
    End Function

    ''' <summary>
    ''' Barras horizontales apiladas: una fila por piso, series Cumplen / No cumplen / Sin dato.
    ''' Útil para ubicar en qué pisos están concentrados los problemas. Devuelve Nothing si
    ''' <paramref name="porPiso"/> es vacío.
    ''' </summary>
    Public Shared Function BarrasPorPiso(porPiso As List(Of PisoResumen),
                                         Optional titulo As String = "",
                                         Optional ancho As Integer = 700,
                                         Optional alto As Integer = 450) As Byte()
        If porPiso Is Nothing OrElse porPiso.Count = 0 Then Return Nothing

        Using chart As New Chart()
            chart.Width = ancho
            chart.Height = alto
            chart.BackColor = Color.White
            chart.AntiAliasing = AntiAliasingStyles.All

            If Not String.IsNullOrWhiteSpace(titulo) Then
                chart.Titles.Add(New Title(titulo) With {
                    .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                    .ForeColor = Color.FromArgb(60, 60, 60)
                })
            End If

            Dim area As New ChartArea("area") With {.BackColor = Color.White}
            area.AxisX.MajorGrid.Enabled = False
            area.AxisX.LabelStyle.Font = New Font("Segoe UI", 9)
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(230, 230, 230)
            area.AxisY.LabelStyle.Font = New Font("Segoe UI", 9)
            area.AxisY.Title = "Cantidad de elementos"
            area.AxisY.TitleFont = New Font("Segoe UI", 9, FontStyle.Bold)
            chart.ChartAreas.Add(area)

            Dim nuevaSerie = Function(nombre As String, color As Color) As Series
                                 Return New Series(nombre) With {
                                     .ChartType = SeriesChartType.StackedBar,
                                     .Color = color,
                                     .BorderWidth = 0,
                                     .Font = New Font("Segoe UI", 8, FontStyle.Bold),
                                     .IsValueShownAsLabel = True,
                                     .LabelForeColor = Color.White,
                                     .LabelFormat = "0",
                                     .CustomProperties = "PointWidth=0.65"
                                 }
                             End Function

            Dim sC = nuevaSerie("Cumplen", ColorCumple)
            Dim sN = nuevaSerie("No cumplen", ColorNoCumple)
            Dim sS = nuevaSerie("Sin dato", ColorSinDato)

            ' Chart apila de abajo hacia arriba en el orden que se agregan los puntos.
            ' Los pisos ya vienen ordenados (sótano → cubierta); la lista Y del StackedBar los
            ' muestra invertidos verticalmente (último punto arriba), por eso iteramos al revés.
            For i = porPiso.Count - 1 To 0 Step -1
                Dim pr = porPiso(i)
                sC.Points.AddXY(pr.Piso, pr.NCumple)
                sN.Points.AddXY(pr.Piso, pr.NNoCumple)
                sS.Points.AddXY(pr.Piso, pr.NSinDato)
            Next

            ' Las etiquetas 0 ensucian el gráfico — ocultarlas.
            Dim ocultarCero = Sub(s As Series)
                                  For Each pt In s.Points
                                      If pt.YValues(0) <= 0 Then pt.Label = " "
                                  Next
                              End Sub
            ocultarCero.Invoke(sC)
            ocultarCero.Invoke(sN)
            ocultarCero.Invoke(sS)

            chart.Series.Add(sC)
            chart.Series.Add(sN)
            chart.Series.Add(sS)

            chart.Legends.Add(New Legend("l") With {
                .Docking = Docking.Bottom,
                .Alignment = StringAlignment.Center,
                .Font = New Font("Segoe UI", 9),
                .BorderWidth = 0
            })

            Using ms As New MemoryStream()
                chart.SaveImage(ms, ChartImageFormat.Png)
                Return ms.ToArray()
            End Using
        End Using
    End Function

    ''' <summary>Convierte un Bitmap a byte() PNG — para la planta exportada por PlantaCanvas.RenderBitmap.</summary>
    Public Shared Function BitmapAPNG(bmp As Bitmap) As Byte()
        If bmp Is Nothing Then Return Nothing
        Using ms As New MemoryStream()
            bmp.Save(ms, Imaging.ImageFormat.Png)
            Return ms.ToArray()
        End Using
    End Function

    ''' <summary>
    ''' Dashboard horizontal con una card por módulo. Cada card: nombre arriba, % de
    ''' cumplimiento grande al centro, status abajo + barra lateral de color según
    ''' veredicto. Dibujado con GDI+ puro para que se vea como un panel ejecutivo
    ''' moderno, no como un chart genérico. Diseñado para ir arriba del Resumen Ejecutivo.
    ''' </summary>
    Public Shared Function DashboardKPIs(resumenes As List(Of ModuloResumen),
                                         Optional ancho As Integer = 980,
                                         Optional alto As Integer = 220) As Byte()
        If resumenes Is Nothing OrElse resumenes.Count = 0 Then Return Nothing

        Dim bmp As New Bitmap(ancho, alto)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(Color.White)

            Dim margen As Integer = 20
            Dim gap As Integer = 16
            Dim nCards As Integer = resumenes.Count
            Dim cardW As Integer = (ancho - 2 * margen - (nCards - 1) * gap) \ Math.Max(nCards, 1)
            Dim cardH As Integer = alto - 2 * margen
            Dim x As Integer = margen
            Dim y As Integer = margen

            For Each r In resumenes
                DibujarCardKPI(g, New Rectangle(x, y, cardW, cardH), r)
                x += cardW + gap
            Next
        End Using

        Using ms As New MemoryStream()
            bmp.Save(ms, Imaging.ImageFormat.Png)
            bmp.Dispose()
            Return ms.ToArray()
        End Using
    End Function

    Private Shared Sub DibujarCardKPI(g As Graphics, rect As Rectangle, r As ModuloResumen)
        Dim colorFondoCard As Color = Color.FromArgb(249, 250, 252)
        Dim colorBorde As Color = Color.FromArgb(225, 228, 234)
        Dim colorBarra As Color = ColorStatus(r)
        Dim colorTexto As Color = Color.FromArgb(50, 55, 65)
        Dim colorSub As Color = Color.FromArgb(120, 125, 135)

        ' Fondo redondeado + sombra
        Using pathCard = CrearPathRedondeado(rect, 10)
            Using bSombra As New SolidBrush(Color.FromArgb(25, 0, 0, 0))
                Dim rSombra = rect
                rSombra.Offset(0, 3)
                Using pSombra = CrearPathRedondeado(rSombra, 10)
                    g.FillPath(bSombra, pSombra)
                End Using
            End Using
            Using bFondo As New SolidBrush(colorFondoCard)
                g.FillPath(bFondo, pathCard)
            End Using
            Using penBorde As New Pen(colorBorde, 1)
                g.DrawPath(penBorde, pathCard)
            End Using
        End Using

        ' Barra lateral de color (status)
        Dim barraW As Integer = 6
        Using bBarra As New SolidBrush(colorBarra)
            Using pathBarra = CrearPathBarraIzq(rect, 10, barraW)
                g.FillPath(bBarra, pathBarra)
            End Using
        End Using

        Dim xTxt As Integer = rect.X + barraW + 14
        Dim wTxt As Integer = rect.Width - barraW - 24

        ' Título del módulo (arriba)
        Using fTit As New Font("Segoe UI Semibold", 11, FontStyle.Bold)
            Using bTit As New SolidBrush(colorTexto)
                g.DrawString(TituloModulo(r.Modulo).ToUpper(), fTit, bTit,
                             New RectangleF(xTxt, rect.Y + 14, wTxt, 22))
            End Using
        End Using

        ' % cumplimiento al centro — número grande
        Dim pct As String = If(r.NRevisados > 0, r.PctCumple.ToString("0") & "%", "—")
        Using fPct As New Font("Segoe UI", 32, FontStyle.Bold)
            Using bPct As New SolidBrush(colorTexto)
                g.DrawString(pct, fPct, bPct, xTxt - 2, rect.Y + 38)
            End Using
        End Using

        ' Status abajo
        Dim status As String = StatusTexto(r)
        Using fSt As New Font("Segoe UI Semibold", 9, FontStyle.Bold)
            Using bSt As New SolidBrush(colorBarra)
                g.DrawString(status.ToUpper(), fSt, bSt, xTxt, rect.Y + rect.Height - 50)
            End Using
        End Using

        ' Detalle "X de Y cumplen"
        If r.NRevisados > 0 Then
            Using fDet As New Font("Segoe UI", 8.5F)
                Using bDet As New SolidBrush(colorSub)
                    g.DrawString($"{r.NCumple} de {r.NRevisados} cumplen", fDet, bDet,
                                 xTxt, rect.Y + rect.Height - 30)
                End Using
            End Using
        Else
            Using fDet As New Font("Segoe UI", 8.5F)
                Using bDet As New SolidBrush(colorSub)
                    g.DrawString("Sin elementos revisados", fDet, bDet,
                                 xTxt, rect.Y + rect.Height - 30)
                End Using
            End Using
        End If
    End Sub

    Private Shared Function ColorStatus(r As ModuloResumen) As Color
        If r.NRevisados = 0 Then Return ColorSinDato
        If r.NNoCumple = 0 Then Return ColorCumple
        If r.PctCumple >= 95 Then Return Color.FromArgb(237, 161, 0)   ' ámbar — aceptable
        Return ColorNoCumple
    End Function

    Private Shared Function StatusTexto(r As ModuloResumen) As String
        If r.NRevisados = 0 Then Return "Sin cálculo"
        If r.NNoCumple = 0 Then Return "✓ Conforme"
        If r.PctCumple >= 95 Then Return "⚠ Aceptable"
        Return "✗ Requiere revisión"
    End Function

    Private Shared Function TituloModulo(modulo As String) As String
        Select Case modulo
            Case "Pilas" : Return "Pilas"
            Case "Columnas" : Return "Columnas"
            Case "Vigas" : Return "Vigas"
            Case "Nervios" : Return "Nervios"
        End Select
        Return modulo
    End Function

    ' --- Helpers de geometría ---
    Private Shared Function CrearPathRedondeado(rect As Rectangle, radio As Integer) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d = radio * 2
        p.AddArc(rect.X, rect.Y, d, d, 180, 90)
        p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

    ''' <summary>Barra lateral izquierda con esquinas redondeadas arriba y abajo (lado derecho recto).</summary>
    Private Shared Function CrearPathBarraIzq(rect As Rectangle, radio As Integer, ancho As Integer) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d = radio * 2
        Dim r = New Rectangle(rect.X, rect.Y, ancho, rect.Height)
        p.AddArc(r.X, r.Y, d, d, 180, 90)
        p.AddLine(r.X + radio, r.Y, r.Right, r.Y)
        p.AddLine(r.Right, r.Y, r.Right, r.Bottom)
        p.AddLine(r.Right, r.Bottom, r.X + radio, r.Bottom)
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

End Class
