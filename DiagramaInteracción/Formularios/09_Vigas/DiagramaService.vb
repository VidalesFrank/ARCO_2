Imports ARCO.Funciones_00_Varias

Public Class DiagramaService

    Private ReadOnly _geo As GeometryService
    Public Sub New(geo As GeometryService)
        _geo = geo
    End Sub

    Public Sub DibujarPlanta(pictureBox As PictureBox,
                            vigas As List(Of cViga),
                            joints As Dictionary(Of String, cJoint),
                            grids As List(Of cGridLine),
                            pisoSeleccionado As String,
                            vigaSeleccionada As cViga
                        )

        If vigas Is Nothing OrElse vigas.Count = 0 Then Exit Sub
        If String.IsNullOrEmpty(pisoSeleccionado) Then Exit Sub

        ' Filtrar vigas del piso seleccionado
        Dim vigasPiso = vigas.Where(Function(v) v.Frames.Any(Function(f) f.Story = pisoSeleccionado)).ToList()
        If vigasPiso.Count = 0 Then Exit Sub

        ' Crear imagen
        pictureBox.Image = New Bitmap(pictureBox.Width, pictureBox.Height)

        Using g As Graphics = Graphics.FromImage(pictureBox.Image)

            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.White)

            ' Calcular Bounding Box de todos los joints del piso
            Dim bbox = CalcularBoundingBox(vigasPiso, joints)

            ' 🔹 1. Dibujar ejes
            DibujarGridLines(g, pictureBox, grids, bbox, joints)

            ' Dibujar cada viga
            For Each v In vigasPiso
                Dim esSeleccionada As Boolean = False

                If vigaSeleccionada IsNot Nothing Then
                    esSeleccionada = (v.Name_Beam = vigaSeleccionada.Name_Beam)
                End If

                DibujarViga(g, pictureBox, v, bbox, joints, esSeleccionada)
            Next

        End Using

        pictureBox.Refresh()
    End Sub

    Private Sub DibujarGridLines(g As Graphics,
                                 pictureBox As PictureBox,
                                 grids As List(Of cGridLine),
                                 bbox As RectangleF,
                                 joints As Dictionary(Of String, cJoint))

        If grids Is Nothing OrElse grids.Count = 0 Then Exit Sub

        Dim penGrid As New Pen(Color.Gray, 1)
        penGrid.DashStyle = Drawing2D.DashStyle.Dash

        Dim fontGrid As New Font("Segoe UI", 8.5F, FontStyle.Bold)
        Dim brushText As Brush = Brushes.Black
        Dim brushBubble As Brush = Brushes.White
        Dim penBubble As New Pen(Color.Gray, 1)

        ' 🔹 Rango real del modelo
        Dim minX As Double = joints.Values.Min(Function(j) j.GlobalX)
        Dim maxX As Double = joints.Values.Max(Function(j) j.GlobalX)
        Dim minY As Double = joints.Values.Min(Function(j) j.GlobalY)
        Dim maxY As Double = joints.Values.Max(Function(j) j.GlobalY)

        ' 🔹 Extensión real del grid (metros)
        Dim margen As Double = 1.5
        Dim radioBurbuja As Single = 7.5

        For Each gl In grids

            If Not gl.Visible Then Continue For
            If String.IsNullOrWhiteSpace(gl.GridID) Then Continue For

            If gl.Direction = "X" Then
                ' ============================================
                ' 🔹 GRID X → LÍNEA VERTICAL
                ' ============================================
                Dim p1 = Transformar(New Vector3(gl.Ordinate, minY - margen, 0), bbox, pictureBox.Width, pictureBox.Height)
                Dim p2 = Transformar(New Vector3(gl.Ordinate, maxY + margen, 0), bbox, pictureBox.Width, pictureBox.Height)

                g.DrawLine(penGrid, p1, p2)

                ' 🔹 Posición del ID (arriba o abajo)
                Dim yTexto As Single =
                If(gl.BubbleLocation.ToLower() = "start", p2.Y + 15, p1.Y - 15)

                Dim pTexto As New PointF(p1.X, yTexto)

                DibujarBurbujaGrid(g, pTexto, gl.GridID, fontGrid, penBubble, brushBubble, brushText, gl.Direction)

            ElseIf gl.Direction = "Y" Then
                ' ============================================
                ' 🔹 GRID Y → LÍNEA HORIZONTAL
                ' ============================================
                Dim p1 = Transformar(New Vector3(minX - margen, gl.Ordinate, 0), bbox, pictureBox.Width, pictureBox.Height)
                Dim p2 = Transformar(New Vector3(maxX + margen, gl.Ordinate, 0), bbox, pictureBox.Width, pictureBox.Height)

                g.DrawLine(penGrid, p1, p2)

                ' 🔹 Posición del ID (izq o der)
                Dim xTexto As Single =
                If(gl.BubbleLocation.ToLower() = "start", p1.X - 15, p2.X + 15)

                Dim pTexto As New PointF(xTexto, p1.Y)

                DibujarBurbujaGrid(g, pTexto, gl.GridID, fontGrid, penBubble, brushBubble, brushText, gl.Direction)
            End If
        Next
    End Sub

    Private Sub DibujarBurbujaGrid(g As Graphics,
                                   centro As PointF,
                                   texto As String,
                                   font As Font,
                                   pen As Pen,
                                   fondo As Brush,
                                   textoBrush As Brush,
                                   Gl_Dir As String)

        Dim radio As Single = 10

        Dim Sig_Dir = 1

        If Gl_Dir = "Y" Then
            Sig_Dir = -1
        End If

        Dim rect As New RectangleF(
        centro.X - radio,
        centro.Y + Sig_Dir * radio,
        radio * 2,
        radio * 2)

        ' Burbuja
        g.FillEllipse(fondo, rect)
        g.DrawEllipse(pen, rect)

        ' Texto centrado
        Dim sf As New StringFormat With {
        .Alignment = StringAlignment.Center,
        .LineAlignment = StringAlignment.Center
    }

        g.DrawString(texto, font, textoBrush, rect, sf)
    End Sub


    Private Function CalcularBoundingBox(vigasPiso As List(Of cViga),
                                         joints As Dictionary(Of String, cJoint)) As RectangleF

        Dim xs As New List(Of Double)
        Dim ys As New List(Of Double)

        For Each v In vigasPiso
            For Each f In v.Frames
                Dim ji = joints(f.JointI)
                Dim jj = joints(f.JointJ)
                xs.Add(ji.GlobalX) : xs.Add(jj.GlobalX)
                ys.Add(ji.GlobalY) : ys.Add(jj.GlobalY)
            Next
        Next

        Return New RectangleF(CSng(xs.Min()), CSng(ys.Min()),
                          CSng(xs.Max() - xs.Min()), CSng(ys.Max() - ys.Min()))
    End Function


    Private Sub DibujarViga(g As Graphics,
                            pictureBox As PictureBox,
                            viga As cViga,
                            bbox As RectangleF,
                            joints As Dictionary(Of String, cJoint),
                            Optional seleccionada As Boolean = False)

        Dim font As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Dim brushTexto As Brush = Brushes.Black

        Dim colorViga As Color = Color.DarkBlue
        Dim espesor As Single = 2

        If seleccionada Then
            colorViga = Color.Red
            espesor = 3
        End If

        ' =====================================================
        ' 🔹 Dibujar frames de la viga
        ' =====================================================
        For Each f In viga.Frames

            Dim ji = joints(f.JointI)
            Dim jj = joints(f.JointJ)

            Dim p1 As PointF = Transformar(New Vector3(ji.GlobalX, ji.GlobalY, 0), bbox, pictureBox.Width, pictureBox.Height)
            Dim p2 As PointF = Transformar(New Vector3(jj.GlobalX, jj.GlobalY, 0), bbox, pictureBox.Width, pictureBox.Height)

            ' Halo de selección (más profesional)
            If seleccionada Then
                Using penHalo As New Pen(Color.Yellow, espesor + 4)
                    g.DrawLine(penHalo, p1, p2)
                End Using
            End If

            Using pen As New Pen(colorViga, espesor)
                g.DrawLine(pen, p1, p2)
            End Using

        Next

        ' =====================================================
        ' 🔹 Calcular punto medio de la viga
        ' =====================================================
        Dim minX As Single = Single.MaxValue
        Dim maxX As Single = Single.MinValue
        Dim minY As Single = Single.MaxValue
        Dim maxY As Single = Single.MinValue

        For Each f In viga.Frames

            Dim ji = joints(f.JointI)
            Dim jj = joints(f.JointJ)

            minX = Math.Min(minX, Math.Min(ji.GlobalX, jj.GlobalX))
            maxX = Math.Max(maxX, Math.Max(ji.GlobalX, jj.GlobalX))
            minY = Math.Min(minY, Math.Min(ji.GlobalY, jj.GlobalY))
            maxY = Math.Max(maxY, Math.Max(ji.GlobalY, jj.GlobalY))

        Next

        Dim midPoint As New Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0)
        Dim pMid As PointF = Transformar(midPoint, bbox, pictureBox.Width, pictureBox.Height)

        ' =====================================================
        ' 🔹 Calcular orientación de la viga
        ' =====================================================
        Dim fRef = viga.Frames(0)

        Dim jiRef = joints(fRef.JointI)
        Dim jjRef = joints(fRef.JointJ)

        Dim dx As Single = jjRef.GlobalX - jiRef.GlobalX
        Dim dy As Single = jjRef.GlobalY - jiRef.GlobalY

        Dim angle As Single = CSng(Math.Atan2(dy, dx) * 180 / Math.PI)

        ' =====================================================
        ' 🔹 Dibujar nombre de la viga
        ' =====================================================
        g.TranslateTransform(pMid.X, pMid.Y)
        g.RotateTransform(angle)

        Dim size = g.MeasureString(viga.Nombre, font)

        Dim rectTexto As New RectangleF(
        -size.Width / 2,
        -size.Height / 2,
        size.Width,
        size.Height)

        ' Fondo blanco para legibilidad
        Using brushFondo As New SolidBrush(Color.FromArgb(200, Color.White))
            g.FillRectangle(brushFondo, rectTexto)
        End Using

        g.DrawString(viga.Nombre, font, brushTexto, rectTexto.Location)

        g.ResetTransform()

    End Sub

    Private Function Transformar(p As Vector3,
                                 bbox As RectangleF,
                                 width As Integer,
                                 height As Integer) As PointF

        Dim canvasWidth As Single = width
        Dim canvasHeight As Single = height
        Dim margin As Single = 40

        ' Escala proporcional
        Dim scaleX = (canvasWidth - 2 * margin) / bbox.Width
        Dim scaleY = (canvasHeight - 2 * margin) / bbox.Height
        Dim scale = Math.Min(scaleX, scaleY)

        ' Ajustar para centrar
        Dim x As Single = (p.X - bbox.Left) * scale + margin + (canvasWidth - 2 * margin - bbox.Width * scale) / 2
        Dim y As Single = canvasHeight - ((p.Y - bbox.Top) * scale + margin + (canvasHeight - 2 * margin - bbox.Height * scale) / 2)

        Return New PointF(x, y)
    End Function

    Public Sub DibujarDiagramaMomentoFrames(viga As cViga,
                                            pictureBox As PictureBox,
                                            beamForces As List(Of cCombinacionBeamForce),
                                            combinaciones As HashSet(Of String))

        If viga.Frames Is Nothing OrElse viga.Frames.Count = 0 Then Exit Sub

        Dim bmp As New Bitmap(pictureBox.Width, pictureBox.Height)
        Dim g As Graphics = Graphics.FromImage(bmp)

        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        g.Clear(Color.White)

        Dim margin As Single = 50

        Dim colores As Color() =
    {
        Color.Red, Color.Blue, Color.DarkOrange,
        Color.Purple, Color.Brown, Color.DarkCyan
    }

        Dim datosFrame As New List(Of Object)
        Dim todasX As New List(Of Single)
        Dim todasY As New List(Of Double)

        Dim xOffset As Single = 0

        ' =========================================================
        ' 🔹 Preparar datos
        ' =========================================================
        For Each frame In viga.Frames

            If frame.EnvolventeMomento Is Nothing Then Continue For

            Dim estaciones = frame.EnvolventeMomento.Estaciones
            Dim envMax = frame.EnvolventeMomento.MmaxDesign
            Dim envMin = frame.EnvolventeMomento.MminDesign

            Dim combos = ObtenerCombinacionesFrame(frame, beamForces, combinaciones)

            Dim L As Double = estaciones.Last()

            For Each s In estaciones
                todasX.Add(xOffset + s)
            Next

            For Each m In envMax
                todasY.Add(m)
            Next

            For Each m In envMin
                todasY.Add(m)
            Next

            datosFrame.Add(New With {
            .Frame = frame,
            .Estaciones = estaciones,
            .EnvMax = envMax,
            .EnvMin = envMin,
            .Combos = combos,
            .L = L,
            .Offset = xOffset
        })

            xOffset += L

        Next

        If todasX.Count = 0 Then Exit Sub

        ' =========================================================
        ' 🔹 Escala
        ' =========================================================
        Dim minX = todasX.Min()
        Dim maxX = todasX.Max()

        Dim maxAbsY = Math.Max(Math.Abs(todasY.Min()), Math.Abs(todasY.Max()))

        Dim scaleX As Single = CSng((pictureBox.Width - 2 * margin) / (maxX - minX))
        Dim scaleY As Single = CSng((pictureBox.Height / 2 - margin) / maxAbsY)

        Dim yZero As Single = pictureBox.Height / 2

        Dim TransformX = Function(x As Single) margin + (x - minX) * scaleX
        Dim TransformY = Function(y As Single) yZero + y * scaleY

        ' =========================================================
        ' 🔹 Eje cero
        ' =========================================================
        Using penZero As New Pen(Color.Black, 1)
            penZero.DashStyle = Drawing2D.DashStyle.Dash
            g.DrawLine(penZero, margin, yZero, pictureBox.Width - margin, yZero)
        End Using

        ' =========================================================
        ' 🔹 Dibujar combinaciones
        ' =========================================================
        For iComb = 0 To combinaciones.Count - 1

            Dim combo = combinaciones(iComb)
            Dim colorCombo = colores(iComb Mod colores.Length)

            For Each df In datosFrame

                If Not df.Combos.ContainsKey(combo) Then Continue For

                'Dim lista = df.Combos(combo)
                Dim listaTotal = df.Combos(combo)

                ' 🔹 Separar por StepType
                Dim listaMax As New List(Of cCombinacionBeamForce)
                Dim listaMin As New List(Of cCombinacionBeamForce)
                Dim listaNormal As New List(Of cCombinacionBeamForce)

                For Each item In listaTotal
                    Select Case item.stepType
                        Case "Max"
                            listaMax.Add(item)
                        Case "Min"
                            listaMin.Add(item)
                        Case Else
                            listaNormal.Add(item)
                    End Select
                Next

                ' 🔸 Dibujar función reutilizable
                Dim Dibujar = Sub(lista As List(Of cCombinacionBeamForce), pen As Pen)

                                  If lista Is Nothing OrElse lista.Count < 2 Then Exit Sub

                                  ' 🔹 Ordenar por estación (CLAVE)
                                  lista = lista.OrderBy(Function(x) x.ElementStation).ToList()

                                  For i = 0 To lista.Count - 2

                                      Dim x1 = df.Offset + lista(i).ElementStation
                                      Dim y1 = lista(i).M3
                                      Dim x2 = df.Offset + lista(i + 1).ElementStation
                                      Dim y2 = lista(i + 1).M3

                                      ' 🔹 Validar valores (evita overflow)
                                      ' 🔹 Validación compatible con .NET Framework
                                      If (Double.IsNaN(x1) OrElse Double.IsInfinity(x1) OrElse
                                            Double.IsNaN(y1) OrElse Double.IsInfinity(y1)) Then Continue For

                                      If (Double.IsNaN(x2) OrElse Double.IsInfinity(x2) OrElse
                                            Double.IsNaN(y2) OrElse Double.IsInfinity(y2)) Then Continue For

                                      ' 🔹 Protección adicional (valores absurdos)
                                      If Math.Abs(y1) > 1000000000.0 OrElse Math.Abs(y2) > 1000000000.0 Then Continue For

                                      Try
                                          g.DrawLine(pen, TransformX(x1), TransformY(y1), TransformX(x2), TransformY(y2))
                                      Catch ex As OverflowException
                                          ' Puedes loguear si quieres depurar
                                          Debug.Print("Overflow en dibujo: " & ex.Message)
                                      End Try

                                  Next
                              End Sub

                ' 🔹 Dibujar según exista
                Using pen As New Pen(colorCombo, 2)

                    If listaMax.Count > 0 Then Dibujar(listaMax, pen)
                    If listaMin.Count > 0 Then Dibujar(listaMin, pen)

                    ' Si no hay max/min, dibuja la normal
                    If listaMax.Count = 0 AndAlso listaMin.Count = 0 Then
                        Dibujar(listaNormal, pen)
                    End If

                End Using

            Next

        Next

        ' =========================================================
        ' 🔹 Dibujar envolvente NSR
        ' =========================================================
        Using penEnv As New Pen(Color.Green, 3)

            For Each df In datosFrame

                Dim estaciones = df.Estaciones
                Dim envMax = df.EnvMax
                Dim envMin = df.EnvMin

                Dim puntosSup As New List(Of PointF)
                Dim puntosInf As New List(Of PointF)

                For i = 0 To estaciones.Count - 1

                    puntosSup.Add(New PointF(
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMax(i))))

                Next

                For i = estaciones.Count - 1 To 0 Step -1

                    puntosInf.Add(New PointF(
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMin(i))))

                Next

                Dim puntos = puntosSup.Concat(puntosInf).ToArray()

                Using brushEnv As New SolidBrush(Color.FromArgb(60, Color.Green))
                    g.FillPolygon(brushEnv, puntos)
                End Using

                For i = 0 To estaciones.Count - 2

                    g.DrawLine(penEnv,
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMax(i)),
                    TransformX(df.Offset + estaciones(i + 1)),
                    TransformY(envMax(i + 1)))

                    g.DrawLine(penEnv,
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMin(i)),
                    TransformX(df.Offset + estaciones(i + 1)),
                    TransformY(envMin(i + 1)))

                Next

                ' =========================================================
                ' 🔹 Etiquetas
                ' =========================================================
                Dim sInicio = estaciones(0)
                Dim sFinal = estaciones(estaciones.Count - 1)

                Dim MnegIzq = envMin(0)
                Dim MnegDer = envMin(envMin.Count - 1)

                DibujarEtiquetaMomento(g,
                Math.Round(MnegIzq, 1).ToString(),
                TransformX(df.Offset + sInicio / 2),
                TransformY(MnegIzq))

                DibujarEtiquetaMomento(g,
                Math.Round(MnegDer, 1).ToString(),
                TransformX(df.Offset + df.L - (df.L - sFinal) / 2),
                TransformY(MnegDer))

                Dim Mpos As Double = Double.MinValue
                Dim iMax As Integer = 0

                For i = 0 To envMax.Count - 1

                    If envMax(i) > Mpos Then
                        Mpos = envMax(i)
                        iMax = i
                    End If

                Next

                Dim sPos = estaciones(iMax)

                DibujarEtiquetaMomento(g,
                Math.Round(Mpos, 1).ToString(),
                TransformX(df.Offset + sPos),
                TransformY(Mpos))

            Next

        End Using

        ' =========================================================
        ' 🔹 Dibujar apoyos
        ' =========================================================
        For i = 0 To datosFrame.Count - 1

            Dim df = datosFrame(i)

            Dim estaciones = df.Estaciones
            Dim L = df.L
            Dim offset = df.Offset

            Dim sInicio = estaciones(0)
            Dim sFinal = estaciones(estaciones.Count - 1)

            If i = 0 Then

                DibujarApoyo(g,
                TransformX(offset),
                TransformX(offset + sInicio),
                pictureBox.Height,
                margin)

            End If

            If i < datosFrame.Count - 1 Then

                Dim dfNext = datosFrame(i + 1)
                Dim estacionesNext = dfNext.Estaciones
                Dim sInicioNext = estacionesNext(0)

                Dim x1 = offset + sFinal
                Dim x2 = offset + L + sInicioNext

                DibujarApoyo(g,
                TransformX(x1),
                TransformX(x2),
                pictureBox.Height,
                margin)

            Else

                DibujarApoyo(g,
                TransformX(offset + sFinal),
                TransformX(offset + L),
                pictureBox.Height,
                margin)

            End If

            Using penTramo As New Pen(Color.Gray, 1)

                penTramo.DashStyle = Drawing2D.DashStyle.Dot

                g.DrawLine(penTramo,
                TransformX(df.Offset + df.L),
                margin,
                TransformX(df.Offset + df.L),
                pictureBox.Height - margin)

            End Using

        Next

        pictureBox.Image = bmp

    End Sub


    Private Sub DibujarApoyo(g As Graphics,
                         x1 As Single,
                         x2 As Single,
                         pictureHeight As Integer,
                         margin As Single)

        Dim rect As New RectangleF(
        x1,
        margin,
        x2 - x1,
        pictureHeight - 2 * margin)

        Using brush As New SolidBrush(Color.FromArgb(90, Color.Gray))
            g.FillRectangle(brush, rect)
        End Using

    End Sub

    Private Sub DibujarEtiquetaMomento(
    g As Graphics,
    texto As String,
    x As Single,
    y As Single)

        Using f As New Font("Segoe UI", 8, FontStyle.Bold)

            Dim size = g.MeasureString(texto, f)

            g.DrawString(texto, f, Brushes.Black,
                     x - size.Width / 2,
                     y - size.Height - 4)

        End Using

    End Sub

    Public Sub DibujarDiagramaCortanteFrames(viga As cViga,
                                             pictureBox As PictureBox,
                                             beamForces As List(Of cCombinacionBeamForce),
                                             combinaciones As HashSet(Of String))

        If viga.Frames Is Nothing OrElse viga.Frames.Count = 0 Then Return
        If pictureBox.Width < 10 OrElse pictureBox.Height < 10 Then Return

        Dim margin As Single = 50

        ' Lookup by (combo, beam, story) for per-combination drawing
        Dim lookupCombo = beamForces _
            .GroupBy(Function(bf) (bf.LoadCaseKey, bf.Beam, bf.Story)) _
            .ToDictionary(Function(p) p.Key, Function(p) p.ToList())

        ' Lookup by (beam, story) for envelope across all combinations
        Dim lookupEnv = beamForces _
            .GroupBy(Function(bf) (bf.Beam, bf.Story)) _
            .ToDictionary(Function(p) p.Key, Function(p) p.ToList())

        ' =====================================================
        ' 🔹 1. ESCALA GLOBAL
        '        X = [0, totalLen] con frame.Longitud como espaciado
        '        Y = max|V2| de todos los datos
        ' =====================================================
        Dim totalLen As Double = viga.Frames.Sum(Function(f) f.Longitud)
        Dim maxAbsV As Double = 0

        For Each frame In viga.Frames
            Dim keyE = (frame.ObjectLabel, frame.Story)
            Dim bfE As List(Of cCombinacionBeamForce) = Nothing
            If lookupEnv.TryGetValue(keyE, bfE) AndAlso bfE.Count > 0 Then
                maxAbsV = Math.Max(maxAbsV, bfE.Max(Function(bf) Math.Abs(bf.V2)))
            End If
        Next

        Dim bmp As New Bitmap(pictureBox.Width, pictureBox.Height)
        Dim g As Graphics = Graphics.FromImage(bmp)
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        g.Clear(Color.White)

        If totalLen < 0.0001 OrElse maxAbsV < 0.0001 Then
            Using fMsg As New Font("Segoe UI", 9)
                g.DrawString("Sin datos de cortante válidos", fMsg, Brushes.Gray,
                             CSng(pictureBox.Width / 2 - 80), CSng(pictureBox.Height / 2 - 10))
            End Using
            pictureBox.Image = bmp
            Return
        End If

        Dim scaleX As Double = (pictureBox.Width - 2 * margin) / totalLen
        Dim scaleY As Double = (pictureBox.Height / 2 - margin) / maxAbsV
        Dim yZero As Single = CSng(pictureBox.Height / 2)

        ' Transform functions — x is absolute position [0, totalLen]
        Dim TrX = Function(x As Double) As Single
                      Return CSng(margin + x * scaleX)
                  End Function
        Dim MapY = Function(v As Double) As Single
                       Return CSng(yZero + v * scaleY)
                   End Function

        ' Zero line
        Using penZero As New Pen(Color.Black, 1)
            penZero.DashStyle = Drawing2D.DashStyle.Dash
            g.DrawLine(penZero, margin, yZero, pictureBox.Width - margin, yZero)
        End Using

        ' =====================================================
        ' 🔹 Iterate frames once — draw all layers per frame
        ' =====================================================
        Dim xOff As Double = 0

        For Each frame In viga.Frames

            Dim Lf As Double = frame.Longitud
            If Lf <= 0 Then
                xOff += Lf
                Continue For
            End If

            Dim keyEnv = (frame.ObjectLabel, frame.Story)
            Dim bfAll As List(Of cCombinacionBeamForce) = Nothing
            lookupEnv.TryGetValue(keyEnv, bfAll)

            ' Station range within this frame
            Dim sF As Double = If(bfAll IsNot Nothing AndAlso bfAll.Count > 0,
                                   bfAll.Min(Function(bf) bf.Station), 0)
            Dim sL As Double = If(bfAll IsNot Nothing AndAlso bfAll.Count > 0,
                                   bfAll.Max(Function(bf) bf.Station), Lf)

            ' --------------------------------------------------
            ' LAYER 1: Gray support rectangles [0,sF] and [sL,Lf]
            ' --------------------------------------------------
            Try
                Using brushGray As New SolidBrush(Color.FromArgb(90, Color.Gray))
                    If sF > 0.001 Then
                        Dim xA = TrX(xOff) : Dim xB = TrX(xOff + sF)
                        If xB > xA Then g.FillRectangle(brushGray, xA, margin, xB - xA, pictureBox.Height - 2 * margin)
                    End If
                    If Lf - sL > 0.001 Then
                        Dim xC = TrX(xOff + sL) : Dim xD = TrX(xOff + Lf)
                        If xD > xC Then g.FillRectangle(brushGray, xC, margin, xD - xC, pictureBox.Height - 2 * margin)
                    End If
                End Using
            Catch ex As OverflowException
            End Try

            ' Frame divider line (between frames)
            If xOff > 0.001 Then
                Try
                    Using penDiv As New Pen(Color.Gray, 1)
                        penDiv.DashStyle = Drawing2D.DashStyle.Dot
                        Dim xDiv = TrX(xOff)
                        g.DrawLine(penDiv, xDiv, margin, xDiv, pictureBox.Height - margin)
                    End Using
                Catch ex As OverflowException
                End Try
            End If

            ' --------------------------------------------------
            ' LAYER 2: Per-combination lines  Max=Rojo  Min=Azul
            ' --------------------------------------------------
            If bfAll IsNot Nothing AndAlso bfAll.Count > 0 Then

                For Each combo In combinaciones

                    Dim keyC = (combo, frame.ObjectLabel, frame.Story)
                    Dim bfCombo As List(Of cCombinacionBeamForce) = Nothing
                    If Not lookupCombo.TryGetValue(keyC, bfCombo) Then Continue For
                    If bfCombo.Count < 2 Then Continue For

                    Dim DrawSegments = Sub(lista As List(Of cCombinacionBeamForce), pen As Pen)
                                           Dim ord = lista.OrderBy(Function(b) b.Station).ToList()
                                           For i As Integer = 0 To ord.Count - 2
                                               Dim v1 = ord(i).V2 : Dim v2 = ord(i + 1).V2
                                               If Math.Abs(v1) > 1.0E+15 OrElse Math.Abs(v2) > 1.0E+15 Then Continue For
                                               Try
                                                   g.DrawLine(pen,
                                                       TrX(xOff + ord(i).Station), MapY(v1),
                                                       TrX(xOff + ord(i + 1).Station), MapY(v2))
                                               Catch ex As OverflowException
                                               End Try
                                           Next
                                       End Sub

                    Dim maxRecs = bfCombo.Where(Function(b) b.stepType.Trim().ToUpper() = "MAX").ToList()
                    Dim minRecs = bfCombo.Where(Function(b) b.stepType.Trim().ToUpper() = "MIN").ToList()
                    Dim other = bfCombo.Where(Function(b) b.stepType.Trim().ToUpper() <> "MAX" AndAlso
                                               b.stepType.Trim().ToUpper() <> "MIN").ToList()

                    Using penMax As New Pen(Color.FromArgb(200, Color.Red), 1.5F) : DrawSegments(maxRecs, penMax) : End Using
                    Using penMin As New Pen(Color.FromArgb(200, Color.Blue), 1.5F) : DrawSegments(minRecs, penMin) : End Using
                    Using penOth As New Pen(Color.FromArgb(150, Color.Gray), 1.0F) : DrawSegments(other, penOth) : End Using

                Next

            End If

            ' --------------------------------------------------
            ' LAYER 3: Shear envelope  max|V2| per station — Verde
            ' --------------------------------------------------
            If bfAll IsNot Nothing AndAlso bfAll.Count >= 2 Then

                Dim perStation = bfAll _
                    .GroupBy(Function(bf) Math.Round(bf.Station, 6)) _
                    .ToDictionary(Function(gr) gr.Key,
                                  Function(gr) gr.Max(Function(bf) Math.Abs(bf.V2)))

                Dim stEnv = perStation.Keys.OrderBy(Function(s) s).ToList()

                Using penEnv As New Pen(Color.FromArgb(0, 140, 0), 2.5F)
                    For i As Integer = 0 To stEnv.Count - 2
                        Dim s1 = stEnv(i) : Dim s2 = stEnv(i + 1)
                        Dim e1 = perStation(s1) : Dim e2 = perStation(s2)
                        If Math.Abs(e1) > 1.0E+15 OrElse Math.Abs(e2) > 1.0E+15 Then Continue For
                        Try
                            g.DrawLine(penEnv, TrX(xOff + s1), MapY(e1), TrX(xOff + s2), MapY(e2))
                            g.DrawLine(penEnv, TrX(xOff + s1), MapY(-e1), TrX(xOff + s2), MapY(-e2))
                        Catch ex As OverflowException
                        End Try
                    Next
                End Using

                ' Labels (max and min of envelope)
                Dim sEnvMax = stEnv.OrderByDescending(Function(s) perStation(s)).First()
                Dim vEnvMax = perStation(sEnvMax)
                Try
                    Dim xLbl = TrX(xOff + sEnvMax) : Dim yLbl = MapY(vEnvMax)
                    Using fEnv As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                        g.DrawString(Math.Round(vEnvMax, 1).ToString("0.0"), fEnv, Brushes.DarkGreen, xLbl + 3, yLbl - 10)
                        g.DrawString((-Math.Round(vEnvMax, 1)).ToString("0.0"), fEnv, Brushes.DarkGreen, xLbl + 3, MapY(-vEnvMax) + 1)
                    End Using
                Catch ex As OverflowException
                End Try

            End If

            ' --------------------------------------------------
            ' LAYER 4: Orange zone shading (from support face)
            ' --------------------------------------------------
            Dim refIzqZ = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = ARCO.eNumeradores.PosicionTramoViga.Izquierda)
            Dim refDerZ = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = ARCO.eNumeradores.PosicionTramoViga.Derecha)

            Dim zoneIzqLen As Double = If(refIzqZ IsNot Nothing AndAlso refIzqZ.NumEstribos > 0,
                                          CDbl(refIzqZ.NumEstribos) * refIzqZ.Separacion,
                                          2.0 * frame.Section.h)
            Dim zoneDerLen As Double = If(refDerZ IsNot Nothing AndAlso refDerZ.NumEstribos > 0,
                                          CDbl(refDerZ.NumEstribos) * refDerZ.Separacion,
                                          2.0 * frame.Section.h)

            ' Zone limits measured from support faces (sF and sL)
            Dim xZoneIzqEnd = xOff + sF + zoneIzqLen
            Dim xZoneDerStart = xOff + sL - zoneDerLen
            Dim xFaceL = xOff + sF
            Dim xFaceR = xOff + sL

            ' Prevent overlap: clamp to midpoint
            Dim xMid = (xFaceL + xFaceR) / 2.0
            xZoneIzqEnd = Math.Min(xZoneIzqEnd, xMid)
            xZoneDerStart = Math.Max(xZoneDerStart, xMid)

            Try
                Using brushZone As New SolidBrush(Color.FromArgb(22, Color.OrangeRed))
                    Dim xZI0 = TrX(xFaceL) : Dim xZI1 = TrX(xZoneIzqEnd)
                    Dim xZD0 = TrX(xZoneDerStart) : Dim xZD1 = TrX(xFaceR)
                    If xZI1 > xZI0 Then g.FillRectangle(brushZone, xZI0, margin, xZI1 - xZI0, pictureBox.Height - 2 * margin)
                    If xZD1 > xZD0 Then g.FillRectangle(brushZone, xZD0, margin, xZD1 - xZD0, pictureBox.Height - 2 * margin)
                End Using

                Using penZone As New Pen(Color.OrangeRed, 1.5F)
                    penZone.DashStyle = Drawing2D.DashStyle.DashDot
                    Dim xZI = TrX(xZoneIzqEnd)
                    Dim xZD = TrX(xZoneDerStart)
                    g.DrawLine(penZone, xZI, margin, xZI, pictureBox.Height - margin)
                    If xZD > xZI Then g.DrawLine(penZone, xZD, margin, xZD, pictureBox.Height - margin)
                End Using
            Catch ex As OverflowException
            End Try

            ' --------------------------------------------------
            ' LAYER 5: Vu design markers at sF+d and sL-d
            ' --------------------------------------------------
            For Each zonaCor In frame.RevisionCortante

                If zonaCor.Vu <= 0 OrElse Math.Abs(zonaCor.Vu) > 1.0E+15 Then Continue For

                Dim xSta As Double
                Select Case zonaCor.Posicion
                    Case ARCO.eNumeradores.PosicionTramoViga.Izquierda
                        xSta = xOff + sF + frame.Section.d
                    Case ARCO.eNumeradores.PosicionTramoViga.Derecha
                        xSta = xOff + sL - frame.Section.d
                    Case Else
                        Continue For
                End Select

                Try
                    Dim xPx = TrX(xSta)
                    Dim yPos = MapY(zonaCor.Vu)
                    Dim yNeg = MapY(-zonaCor.Vu)
                    Dim lbl = $"{Math.Round(zonaCor.Vu, 1)} kN"

                    Using penMark As New Pen(Color.DarkRed, 2)
                        g.DrawEllipse(penMark, xPx - 5, yPos - 5, 10, 10)
                        g.DrawEllipse(penMark, xPx - 5, yNeg - 5, 10, 10)
                    End Using

                    Using fnt As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                        g.DrawString(lbl, fnt, Brushes.DarkRed, xPx + 6, yPos - 9)
                        g.DrawString(lbl, fnt, Brushes.DarkRed, xPx + 6, yNeg + 1)
                    End Using
                Catch ex As OverflowException
                End Try

            Next

            xOff += Lf

        Next

        pictureBox.Image = bmp
    End Sub

    Private Sub DibujarEtiquetaCortante(g As Graphics,
                                        TransformX As Func(Of Double, Single),
                                        TransformY As Func(Of Double, Single),
                                        xOffset As Double,
                                        bf As cCombinacionBeamForce)

        If Math.Abs(bf.V2) > 1.0E+15 Then Return

        Dim x As Single = TransformX(xOffset + bf.Station)
        Dim y As Single = TransformY(bf.V2)

        Try
            g.FillEllipse(Brushes.Black, x - 3, y - 3, 6, 6)
            g.DrawString(Math.Round(bf.V2, 1).ToString(),
                     New System.Drawing.Font("Segoe UI", 8.0F, FontStyle.Bold),
                     Brushes.Black,
                     x + 5,
                     If(bf.V2 < 0, y - 15, y + 2))
        Catch ex As OverflowException
        End Try

    End Sub


    Public Function ObtenerCombinacionesFrame(frame As cFrame,
                                              beamForces As List(Of cCombinacionBeamForce),
                                              combinaciones As HashSet(Of String)
                                              ) As Dictionary(Of String, List(Of cCombinacionBeamForce))

        Dim bfFrame = beamForces _
        .Where(Function(r) r.Beam = frame.ObjectLabel _
        AndAlso r.Story = frame.Story _
        AndAlso combinaciones.Contains(r.LoadCaseKey)) _
        .ToList()

        Return bfFrame _
        .GroupBy(Function(bf) bf.LoadCaseKey) _
        .ToDictionary(Function(gp) gp.Key,
                      Function(gp) gp.OrderBy(Function(bf) bf.ElementStation).ToList())

    End Function


End Class
