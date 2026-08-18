Imports System.Drawing.Drawing2D

' ═══════════════════════════════════════════════════════════════════════════════
'  NervioDiagramaService — diagramas gráficos de Momento (M3) y Cortante (V2)
'  para un nervio continuo completo (todos sus tramos concatenados), Módulo 11.
'  Mismo lenguaje visual que DiagramaService (09_Vigas): escala global, una
'  polilínea por combinación, envolvente verde, apoyos grises con etiqueta de eje.
'  A diferencia de Vigas, Nervios no guarda un EnvolventeMomento precalculado por
'  frame — la envolvente se arma aquí al vuelo a partir de cFrameNervio.Combinaciones.
'
'  Además, en cada apoyo se marca explícitamente la CARA del apoyo (offset por
'  B_Apoyo_I/D desde el eje del joint) con el valor de demanda ya calculado en
'  NervioService (Mu_Neg_I/D, Vu_I/D — los mismos que se diseñan y se muestran en
'  las tablas), rotulado con el eje/viga de apoyo — así se lee directamente dónde
'  y cuánto es el momento/cortante de diseño, sin tener que inferirlo del gráfico.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class NervioDiagramaService

    ' Paleta categórica validada (colorblind-safe) — mismo criterio que la planta
    Private Shared ReadOnly Colores As Color() = {
        Color.FromArgb(42, 120, 214),   ' azul
        Color.FromArgb(235, 104, 52),   ' naranja
        Color.FromArgb(27, 175, 122),   ' aqua
        Color.FromArgb(237, 161, 0),    ' amarillo
        Color.FromArgb(232, 123, 164),  ' magenta
        Color.FromArgb(74, 58, 167)     ' violeta
    }

    Private Const COLOR_CARA_R As Integer = 150
    Private Const COLOR_CARA_G As Integer = 90
    Private Const COLOR_CARA_B As Integer = 20

    Public Sub DibujarDiagramaMomento(nervio As cNervio, pictureBox As PictureBox, combosDiseno As HashSet(Of String))
        ' Mu_Neg_I/D son magnitudes de momento NEGATIVO (hogging) por definición
        ' (NervioService.CalcularEnvolventesNervios) — el signo siempre es negativo.
        Dibujar(nervio, pictureBox, combosDiseno, Function(c) c.Momentos,
                Function(fn) fn.Mu_Neg_I, Function(fn) fn.Mu_Neg_D, "kN·m", signoFijo:=-1)
    End Sub

    Public Sub DibujarDiagramaCortante(nervio As cNervio, pictureBox As PictureBox, combosDiseno As HashSet(Of String))
        ' Vu_I/D son magnitudes (Math.Abs) — el signo se infiere de la envolvente
        ' en la estación más cercana a la cara, para que el marcador quede del
        ' lado correcto de la curva.
        Dibujar(nervio, pictureBox, combosDiseno, Function(c) c.Cortantes,
                Function(fn) fn.Vu_I, Function(fn) fn.Vu_D, "kN", signoFijo:=0)
    End Sub

    Private Sub Dibujar(nervio As cNervio, pictureBox As PictureBox, combosDiseno As HashSet(Of String),
                         valores As Func(Of cComboNervio, List(Of Double)),
                         valorCaraI As Func(Of cFrameNervio, Double),
                         valorCaraD As Func(Of cFrameNervio, Double),
                         unidad As String,
                         signoFijo As Integer)

        If pictureBox.Width <= 0 OrElse pictureBox.Height <= 0 Then Return

        Dim bmp As New Bitmap(Math.Max(pictureBox.Width, 50), Math.Max(pictureBox.Height, 50))
        Dim viejo = pictureBox.Image
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.White)

            If nervio Is Nothing OrElse nervio.Frames Is Nothing OrElse nervio.Frames.Count = 0 Then
                g.DrawString("Seleccione un nervio.", New Font("Segoe UI", 9), Brushes.Gray, 10, 10)
                pictureBox.Image = bmp
                If viejo IsNot Nothing Then viejo.Dispose()
                Return
            End If

            Dim margin As Single = 40

            ' ── Preparar datos por tramo: offset acumulado + combos de diseño + envolvente ──
            Dim datos As New List(Of (fn As cFrameNervio, offset As Double, L As Double,
                                       combos As List(Of cComboNervio),
                                       estaciones As List(Of Double),
                                       envMax As List(Of Double), envMin As List(Of Double)))

            Dim xOffset As Double = 0
            Dim todasY As New List(Of Double)
            Dim hayDatos As Boolean = False

            For Each fn In nervio.Frames
                Dim combos = If(fn.Combinaciones, New List(Of cComboNervio)) _
                    .Where(Function(c) (combosDiseno Is Nothing OrElse combosDiseno.Count = 0 OrElse combosDiseno.Contains(c.Nombre)) AndAlso
                                        c.Estaciones IsNot Nothing AndAlso c.Estaciones.Count >= 2) _
                    .ToList()

                Dim L = If(fn.Longitud > 0, fn.Longitud, If(combos.Count > 0, combos(0).Estaciones.Last(), 0))

                Dim estacionesRef As New List(Of Double)
                Dim envMax As New List(Of Double)
                Dim envMin As New List(Of Double)

                If combos.Count > 0 Then
                    estacionesRef = combos(0).Estaciones
                    For i = 0 To estacionesRef.Count - 1
                        Dim vMax As Double = Double.MinValue
                        Dim vMin As Double = Double.MaxValue
                        For Each c In combos
                            Dim lista = valores(c)
                            If lista Is Nothing OrElse i >= lista.Count Then Continue For
                            vMax = Math.Max(vMax, lista(i))
                            vMin = Math.Min(vMin, lista(i))
                        Next
                        If vMax = Double.MinValue Then vMax = 0
                        If vMin = Double.MaxValue Then vMin = 0
                        envMax.Add(vMax)
                        envMin.Add(vMin)
                        todasY.Add(vMax) : todasY.Add(vMin)
                    Next
                    hayDatos = True
                End If

                ' Los valores de diseño en cara de apoyo (ya calculados en NervioService)
                ' también entran en la escala aunque no coincidan exactamente con la
                ' estación de envolvente más cercana.
                todasY.Add(valorCaraI(fn))
                todasY.Add(valorCaraD(fn))

                datos.Add((fn, xOffset, L, combos, estacionesRef, envMax, envMin))
                xOffset += L
            Next

            If Not hayDatos Then
                g.DrawString("Sin combinaciones de diseño para este nervio. Calcule primero.",
                             New Font("Segoe UI", 9), Brushes.Gray, 10, 10)
                pictureBox.Image = bmp
                If viejo IsNot Nothing Then viejo.Dispose()
                Return
            End If

            Dim totalLen = Math.Max(xOffset, 0.01)
            Dim maxAbsY = todasY.Select(Function(v) Math.Abs(v)).DefaultIfEmpty(1).Max()
            If maxAbsY < 0.001 Then maxAbsY = 1

            Dim scaleX As Single = CSng((pictureBox.Width - 2 * margin) / totalLen)
            Dim scaleY As Single = CSng((pictureBox.Height / 2.0 - margin - 24) / maxAbsY)   ' -24: deja aire para etiquetas de cara
            Dim yZero As Single = pictureBox.Height / 2.0F

            Dim TransformX = Function(x As Double) CSng(margin + x * scaleX)
            Dim TransformY = Function(y As Double) CSng(yZero - y * scaleY)

            ' ── Eje cero ──
            Using penZero As New Pen(Color.FromArgb(120, 120, 120), 1) With {.DashStyle = DashStyle.Dash}
                g.DrawLine(penZero, margin, yZero, pictureBox.Width - margin, yZero)
            End Using

            ' ── Combinaciones individuales (una polilínea por combo) ──
            For Each d In datos
                For iCombo = 0 To d.combos.Count - 1
                    Dim c = d.combos(iCombo)
                    Dim lista = valores(c)
                    If lista Is Nothing Then Continue For
                    Dim color = Colores(iCombo Mod Colores.Length)
                    Using pen As New Pen(color, 1.3F)
                        For i = 0 To c.Estaciones.Count - 2
                            If i + 1 >= lista.Count Then Exit For
                            g.DrawLine(pen,
                                TransformX(d.offset + c.Estaciones(i)), TransformY(lista(i)),
                                TransformX(d.offset + c.Estaciones(i + 1)), TransformY(lista(i + 1)))
                        Next
                    End Using
                Next
            Next

            ' ── Envolvente (verde, relleno + contorno) ──
            Using penEnv As New Pen(Color.FromArgb(0, 131, 0), 2.2F), brushEnv As New SolidBrush(Color.FromArgb(45, 0, 131, 0))
                For Each d In datos
                    If d.estaciones.Count < 2 Then Continue For
                    Dim puntosSup As New List(Of PointF)
                    Dim puntosInf As New List(Of PointF)
                    For i = 0 To d.estaciones.Count - 1
                        puntosSup.Add(New PointF(TransformX(d.offset + d.estaciones(i)), TransformY(d.envMax(i))))
                    Next
                    For i = d.estaciones.Count - 1 To 0 Step -1
                        puntosInf.Add(New PointF(TransformX(d.offset + d.estaciones(i)), TransformY(d.envMin(i))))
                    Next
                    Dim poligono = puntosSup.Concat(puntosInf).ToArray()
                    If poligono.Length >= 3 Then g.FillPolygon(brushEnv, poligono)
                    If puntosSup.Count >= 2 Then g.DrawLines(penEnv, puntosSup.ToArray())
                    If puntosInf.Count >= 2 Then g.DrawLines(penEnv, puntosInf.ToArray())
                Next
            End Using

            ' ── Apoyos (rectángulo gris) y separadores de tramo ──
            Using brushApoyo As New SolidBrush(Color.FromArgb(60, 120, 120, 120))
                For idx = 0 To datos.Count - 1
                    Dim d = datos(idx)
                    Dim bApoyoI = Math.Max(d.fn.B_Apoyo_I, 0.02)
                    Dim bApoyoD = Math.Max(d.fn.B_Apoyo_D, 0.02)

                    DibujarRectApoyo(g, brushApoyo, TransformX(d.offset), TransformX(d.offset + bApoyoI), margin, pictureBox.Height)
                    DibujarRectApoyo(g, brushApoyo, TransformX(d.offset + d.L - bApoyoD), TransformX(d.offset + d.L), margin, pictureBox.Height)

                    If idx < datos.Count - 1 Then
                        Using penDiv As New Pen(Color.FromArgb(190, 190, 190), 1) With {.DashStyle = DashStyle.Dot}
                            g.DrawLine(penDiv, TransformX(d.offset + d.L), margin,
                                                TransformX(d.offset + d.L), pictureBox.Height - margin)
                        End Using
                    End If
                Next
            End Using

            ' ── Marcar la CARA del apoyo con el valor de diseño real (I y D) ──
            Using fontCara As New Font("Segoe UI", 8, FontStyle.Bold)
                For Each d In datos
                    Dim bApoyoI = Math.Max(d.fn.B_Apoyo_I, 0.02)
                    Dim bApoyoD = Math.Max(d.fn.B_Apoyo_D, 0.02)

                    Dim xLocalI = bApoyoI
                    Dim xLocalD = d.L - bApoyoD
                    Dim signoI = If(signoFijo <> 0, signoFijo, SignoCercano(d.estaciones, d.envMax, d.envMin, xLocalI))
                    Dim signoD = If(signoFijo <> 0, signoFijo, SignoCercano(d.estaciones, d.envMax, d.envMin, xLocalD))

                    MarcarCaraApoyo(g, fontCara, TransformX, TransformY, yZero,
                                     d.offset + xLocalI, signoI * Math.Abs(valorCaraI(d.fn)), d.fn.EjeApoyo_I, d.fn.Frame_Apoyo_I, unidad)
                    MarcarCaraApoyo(g, fontCara, TransformX, TransformY, yZero,
                                     d.offset + xLocalD, signoD * Math.Abs(valorCaraD(d.fn)), d.fn.EjeApoyo_D, d.fn.Frame_Apoyo_D, unidad)
                Next
            End Using

            ' ── Pico interior (p.ej. momento positivo central) ──
            Using fontLbl As New Font("Segoe UI", 8, FontStyle.Bold)
                For Each d In datos
                    If d.estaciones.Count < 3 Then Continue For
                    Dim iExtra As Integer = -1
                    Dim mejorExtra As Double = 0
                    For i = 1 To d.estaciones.Count - 2
                        Dim v = If(Math.Abs(d.envMax(i)) >= Math.Abs(d.envMin(i)), d.envMax(i), d.envMin(i))
                        If Math.Abs(v) > Math.Abs(mejorExtra) Then
                            mejorExtra = v : iExtra = i
                        End If
                    Next
                    If iExtra >= 0 Then
                        DibujarEtiqueta(g, fontLbl, Math.Round(mejorExtra, 1).ToString(Globalization.CultureInfo.InvariantCulture),
                                         TransformX(d.offset + d.estaciones(iExtra)), TransformY(mejorExtra), Color.Black)
                    End If
                Next
            End Using

            pictureBox.Image = bmp
        End Using

        If viejo IsNot Nothing Then viejo.Dispose()
    End Sub

    ''' <summary>Dibuja el marcador vertical en la cara del apoyo con el valor de diseño
    ''' (ya calculado en NervioService — el mismo que se usa para el cálculo y las tablas)
    ''' y el eje/viga de apoyo correspondiente, para poder leer directamente dónde y cuánto
    ''' es la demanda de diseño.</summary>
    Private Shared Sub MarcarCaraApoyo(g As Graphics, font As Font,
                                        TransformX As Func(Of Double, Single), TransformY As Func(Of Double, Single),
                                        yZero As Single,
                                        xFace As Double, valor As Double,
                                        eje As String, frameApoyo As String, unidad As String)
        If Math.Abs(valor) < 0.01 Then Return

        Dim xPix = TransformX(xFace)
        Dim yPix = TransformY(valor)
        Dim colorCara = Color.FromArgb(COLOR_CARA_R, COLOR_CARA_G, COLOR_CARA_B)

        Using penFace As New Pen(colorCara, 1.4F) With {.DashStyle = DashStyle.DashDot}
            g.DrawLine(penFace, xPix, yZero, xPix, yPix)
        End Using
        Using brPunto As New SolidBrush(colorCara)
            g.FillEllipse(brPunto, xPix - 3.5F, yPix - 3.5F, 7, 7)
        End Using

        Dim idEje = If(Not String.IsNullOrWhiteSpace(eje), $"Eje {eje}",
                       If(Not String.IsNullOrWhiteSpace(frameApoyo), frameApoyo, "Apoyo"))
        Dim texto = $"{idEje}: {Math.Round(valor, 1).ToString(Globalization.CultureInfo.InvariantCulture)} {unidad}"

        Dim sz = g.MeasureString(texto, font)
        Dim xLbl = xPix - sz.Width / 2.0F
        Dim yLbl = If(valor >= 0, yPix - sz.Height - 5.0F, yPix + 5.0F)

        Using bBg As New SolidBrush(Color.FromArgb(230, Color.White))
            g.FillRectangle(bBg, xLbl - 2, yLbl - 1, sz.Width + 4, sz.Height + 2)
        End Using
        Using penBorde As New Pen(colorCara, 1)
            g.DrawRectangle(penBorde, xLbl - 2, yLbl - 1, sz.Width + 4, sz.Height + 2)
        End Using
        Using brTexto As New SolidBrush(colorCara)
            g.DrawString(texto, font, brTexto, xLbl, yLbl)
        End Using
    End Sub

    ''' <summary>Signo (+1/-1) de la envolvente en la estación local más cercana a xLocal —
    ''' usado para saber de qué lado del eje cero cae el marcador de cara cuando el signo
    ''' no está definido de antemano (cortante).</summary>
    Private Shared Function SignoCercano(estaciones As List(Of Double), envMax As List(Of Double), envMin As List(Of Double), xLocal As Double) As Integer
        If estaciones Is Nothing OrElse estaciones.Count = 0 Then Return 1
        Dim mejorI As Integer = 0
        Dim mejorDist As Double = Double.MaxValue
        For i = 0 To estaciones.Count - 1
            Dim dist = Math.Abs(estaciones(i) - xLocal)
            If dist < mejorDist Then mejorDist = dist : mejorI = i
        Next
        Dim v = If(Math.Abs(envMax(mejorI)) >= Math.Abs(envMin(mejorI)), envMax(mejorI), envMin(mejorI))
        Return If(v < 0, -1, 1)
    End Function

    Private Shared Sub DibujarRectApoyo(g As Graphics, brush As SolidBrush,
                                         x1 As Single, x2 As Single, margin As Single, alturaTotal As Integer)
        Dim rect As New RectangleF(Math.Min(x1, x2), margin, Math.Abs(x2 - x1), alturaTotal - 2 * margin)
        g.FillRectangle(brush, rect)
    End Sub

    Private Shared Sub DibujarEtiqueta(g As Graphics, font As Font, texto As String, x As Single, y As Single, color As Color)
        Dim sz = g.MeasureString(texto, font)
        Using br As New SolidBrush(color)
            g.DrawString(texto, font, br, x - sz.Width / 2.0F, y - sz.Height - 3)
        End Using
    End Sub

End Class
