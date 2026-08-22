Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq

' ════════════════════════════════════════════════════════════════════════════
'  E2kRenderer — Motor de renderizado GDI+ para el visualizador de modelo ETABS
'  Tres vistas: Planta (XY), Alzado (XZ o YZ), 3D (proyección ortográfica)
' ════════════════════════════════════════════════════════════════════════════

Public Module E2kRenderer

    ' ─── Opciones de renderizado ─────────────────────────────────────────────

    Public Class RenderOptions
        Public FilterStory As String = ""    ' "" = todos los pisos
        Public ShowColumnas As Boolean = True
        Public ShowVigas As Boolean = True
        Public ShowMuros As Boolean = True
        Public ShowLosas As Boolean = False
        Public ShowGrillas As Boolean = True
        Public ShowNervios As Boolean = True
        Public ShowLblFrame As Boolean = False
        Public ShowLblPier As Boolean = False
        Public ShowLblJoint As Boolean = False
        Public ModoExtruido As Boolean = False
        ' Transformación planta / alzado
        Public PanX As Single = 0
        Public PanY As Single = 0
        Public UserZoom As Single = 1.0F
        ' Cámara 3D
        Public Azimuth As Double = 215.0    ' grados, rotación alrededor del eje Z
        Public Elevation As Double = 28.0   ' grados, inclinación desde horizontal
        ' Vista alzado
        Public AlzDir As String = "XZ"      ' "XZ" o "YZ"
        ' Zoom independiente para 3D
        Public Zoom3D As Single = 1.0F
        Public Pan3DX As Single = 0
        Public Pan3DY As Single = 0
    End Class

    ' ─── Colores del tema ────────────────────────────────────────────────────

    Private ReadOnly CLR_BG As Color = Color.FromArgb(248, 250, 253)
    Private ReadOnly CLR_GRID As Color = Color.FromArgb(200, 200, 210)
    Private ReadOnly CLR_GRID_LBL As Color = Color.FromArgb(120, 120, 130)
    Private ReadOnly CLR_STORY_LINE As Color = Color.FromArgb(190, 190, 200)
    Private ReadOnly CLR_COL_FILL As Color = Color.FromArgb(40, 80, 180)
    Private ReadOnly CLR_COL_EDGE As Color = Color.FromArgb(20, 50, 130)
    Private ReadOnly CLR_BEAM_FILL As Color = Color.FromArgb(20, 140, 60)
    Private ReadOnly CLR_BEAM_EDGE As Color = Color.FromArgb(10, 90, 40)
    Private ReadOnly CLR_WALL_FILL As Color = Color.FromArgb(130, 130, 145)
    Private ReadOnly CLR_WALL_EDGE As Color = Color.FromArgb(80, 80, 95)
    Private ReadOnly CLR_SLAB_FILL As Color = Color.FromArgb(60, 200, 180, 140)
    Private ReadOnly CLR_SLAB_EDGE As Color = Color.FromArgb(160, 150, 110)
    Private ReadOnly CLR_NERVIO_FILL As Color = Color.FromArgb(210, 120, 30)
    Private ReadOnly CLR_NERVIO_EDGE As Color = Color.FromArgb(150, 80, 15)
    Private ReadOnly CLR_LBL_FRAME As Color = Color.FromArgb(20, 60, 160)
    Private ReadOnly CLR_LBL_PIER As Color = Color.FromArgb(80, 20, 160)
    Private ReadOnly CLR_LBL_JOINT As Color = Color.FromArgb(160, 60, 20)
    Private ReadOnly CLR_LBL_NERVIO As Color = Color.FromArgb(180, 80, 20)

    Private Const MARGIN As Integer = 40

    ' ════════════════════════════════════════════════════════════════════════
    '  VISTA EN PLANTA (XY)
    ' ════════════════════════════════════════════════════════════════════════

    Public Function RenderPlanta(model As E2kModel, opt As RenderOptions, W As Integer, H As Integer) As Bitmap
        Dim bmp As New Bitmap(Math.Max(W, 10), Math.Max(H, 10))
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.HighSpeed
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(CLR_BG)

            Dim bounds = model.GetBoundsXY()
            Dim scale = FitScale(bounds.Width, bounds.Height, W, H) * opt.UserZoom
            Dim cx = bounds.X + bounds.Width / 2.0F
            Dim cy = bounds.Y + bounds.Height / 2.0F

            Dim Tx As Func(Of Double, Single) =
                Function(wx) CSng(W / 2.0 + (wx - cx) * scale + opt.PanX)
            Dim Ty As Func(Of Double, Single) =
                Function(wy) CSng(H / 2.0 - (wy - cy) * scale + opt.PanY)

            ' Ejes de grilla
            If opt.ShowGrillas Then DrawGridsXY(g, model, Tx, Ty, W, H)

            ' Losas (fondo)
            If opt.ShowLosas Then DrawSlabsXY(g, model, opt, Tx, Ty)

            ' Muros
            If opt.ShowMuros Then DrawWallsXY(g, model, opt, Tx, Ty, scale)

            ' Vigas / Nervios
            If opt.ShowVigas OrElse opt.ShowNervios Then DrawBeamsXY(g, model, opt, Tx, Ty, scale)

            ' Columnas
            If opt.ShowColumnas Then DrawColumnsXY(g, model, opt, Tx, Ty, scale)

            DrawScaleBar(g, scale, W, H)
        End Using
        Return bmp
    End Function

    Private Sub DrawGridsXY(g As Graphics, model As E2kModel,
                             Tx As Func(Of Double, Single), Ty As Func(Of Double, Single),
                             W As Integer, H As Integer)
        Using pen As New Pen(CLR_GRID, 1) With {.DashStyle = DashStyle.Dash}
        Using lblFnt As New Font("Segoe UI", 11.5F)
        Using lblBrush As New SolidBrush(CLR_GRID_LBL)
            For Each g2 In model.Grids
                Dim sx1 = Tx(g2.X1) : Dim sy1 = Ty(g2.Y1)
                Dim sx2 = Tx(g2.X2) : Dim sy2 = Ty(g2.Y2)
                ' Extender línea hasta los bordes del canvas
                If g2.IsVertical Then
                    GfxLine(g, pen, sx1, 0, sx1, H)
                    g.DrawString(g2.Label, lblFnt, lblBrush, sx1 + 2, 2)
                Else
                    GfxLine(g, pen, 0, sy1, W, sy1)
                    g.DrawString(g2.Label, lblFnt, lblBrush, 2, sy1 + 2)
                End If
            Next
        End Using : End Using : End Using
    End Sub

    Private Sub DrawSlabsXY(g As Graphics, model As E2kModel, opt As RenderOptions,
                              Tx As Func(Of Double, Single), Ty As Func(Of Double, Single))
        Using fillBrush As New SolidBrush(CLR_SLAB_FILL)
        Using edgePen As New Pen(CLR_SLAB_EDGE, 1)
            For Each aa In model.AreaAssigns
                If Not StoryMatch(aa.Story, opt) Then Continue For
                Dim ar = GetArea(model, aa.AreaID)
                If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Floor Then Continue For
                Dim pts = AreaScreenPts(model, ar, Tx, Ty)
                If pts.Length >= 3 Then
                    g.FillPolygon(fillBrush, pts)
                    g.DrawPolygon(edgePen, pts)
                End If
            Next
        End Using : End Using
    End Sub

    Private Sub DrawWallsXY(g As Graphics, model As E2kModel, opt As RenderOptions,
                              Tx As Func(Of Double, Single), Ty As Func(Of Double, Single), scale As Single)
        Using fillBrush As New SolidBrush(CLR_WALL_FILL)
        Using edgePen As New Pen(CLR_WALL_EDGE, 1)
        Using lblFnt As New Font("Segoe UI", 10.5F)
        Using lblBrush As New SolidBrush(CLR_LBL_PIER)
            For Each aa In model.AreaAssigns
                If Not StoryMatch(aa.Story, opt) Then Continue For
                Dim ar = GetArea(model, aa.AreaID)
                If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For

                Dim uniq = UniqueAreaPoints(model, ar)
                If uniq.Count < 2 Then Continue For

                Dim thick = ParseThickness(aa.Section) * scale
                thick = Math.Max(thick, 2.0F)

                If uniq.Count = 2 Then
                    ' Muro lineal: dibujar como línea gruesa
                    Dim p1 = uniq(0), p2 = uniq(1)
                    Dim sx1 = Tx(p1.X) : Dim sy1 = Ty(p1.Y)
                    Dim sx2 = Tx(p2.X) : Dim sy2 = Ty(p2.Y)
                    Using wallPen As New Pen(CLR_WALL_FILL, Math.Max(thick, 2))
                        g.DrawLine(wallPen, sx1, sy1, sx2, sy2)
                    End Using
                    Using outlinePen As New Pen(CLR_WALL_EDGE, 0.5F)
                        g.DrawLine(outlinePen, sx1, sy1, sx2, sy2)
                    End Using
                    ' Label pier
                    If opt.ShowLblPier AndAlso aa.PierName <> "" Then
                        Dim mx = (sx1 + sx2) / 2 : Dim my = (sy1 + sy2) / 2
                        g.DrawString(aa.PierName, lblFnt, lblBrush, mx, my)
                    End If
                Else
                    ' Muro con polígono propio
                    Dim pts = uniq.Select(Function(p) New PointF(Tx(p.X), Ty(p.Y))).ToArray()
                    g.FillPolygon(fillBrush, pts)
                    g.DrawPolygon(edgePen, pts)
                End If
            Next
        End Using : End Using : End Using : End Using
    End Sub

    Private Sub DrawBeamsXY(g As Graphics, model As E2kModel, opt As RenderOptions,
                              Tx As Func(Of Double, Single), Ty As Func(Of Double, Single), scale As Single)
        Using lblFnt As New Font("Segoe UI", 10.5F)
            For Each la In model.LineAssigns
                If Not StoryMatch(la.Story, opt) Then Continue For
                Dim ln = GetLine(model, la.LineID)
                If ln Is Nothing OrElse ln.LineType <> E2kLineType.Beam Then Continue For
                If ln.Point1 = ln.Point2 Then Continue For

                Dim esNervio = IsNervio(la.Section)
                If esNervio AndAlso Not opt.ShowNervios Then Continue For
                If Not esNervio AndAlso Not opt.ShowVigas Then Continue For

                Dim p1 = GetPoint(model, ln.Point1)
                Dim p2 = GetPoint(model, ln.Point2)
                If p1 Is Nothing OrElse p2 Is Nothing Then Continue For

                Dim sx1 = Tx(p1.X) : Dim sy1 = Ty(p1.Y)
                Dim sx2 = Tx(p2.X) : Dim sy2 = Ty(p2.Y)

                Dim sec = GetSection(model, la.Section)
                Dim bPx = If(sec IsNot Nothing, CSng(sec.B * scale), 2.0F)
                bPx = Math.Max(bPx, 1.5F)

                Using pen As New Pen(If(esNervio, CLR_NERVIO_FILL, CLR_BEAM_FILL), bPx)
                    g.DrawLine(pen, sx1, sy1, sx2, sy2)
                End Using
                If opt.ShowLblFrame Then
                    Dim mx = (sx1 + sx2) / 2 : Dim my = (sy1 + sy2) / 2
                    Using lblBrush As New SolidBrush(If(esNervio, CLR_LBL_NERVIO, CLR_LBL_FRAME))
                        g.DrawString(la.LineID, lblFnt, lblBrush, mx, my)
                    End Using
                End If
            Next
        End Using
    End Sub

    Private Sub DrawColumnsXY(g As Graphics, model As E2kModel, opt As RenderOptions,
                                Tx As Func(Of Double, Single), Ty As Func(Of Double, Single), scale As Single)
        Using fillBrush As New SolidBrush(CLR_COL_FILL)
        Using edgePen As New Pen(CLR_COL_EDGE, 1)
        Using lblFnt As New Font("Segoe UI", 10.5F)
        Using lblBrush As New SolidBrush(CLR_LBL_FRAME)
            For Each la In model.LineAssigns
                If Not StoryMatch(la.Story, opt) Then Continue For
                Dim ln = GetLine(model, la.LineID)
                If ln Is Nothing OrElse ln.LineType <> E2kLineType.Column Then Continue For

                Dim pt = GetPoint(model, ln.Point1)
                If pt Is Nothing Then Continue For

                Dim sec = GetSection(model, la.Section)
                Dim B = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B, 0.3)
                Dim D = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D, 0.3)
                Dim ang = la.Angle
                Dim sx = Tx(pt.X) : Dim sy = Ty(pt.Y)
                Dim bPx = CSng(B * scale) : Dim dPx = CSng(D * scale)
                bPx = Math.Max(bPx, 4) : dPx = Math.Max(dPx, 4)

                If sec IsNot Nothing AndAlso sec.Shape = E2kSectionShape.Circle Then
                    ' Círculo
                    Dim r = bPx / 2
                    g.FillEllipse(fillBrush, sx - r, sy - r, bPx, bPx)
                    g.DrawEllipse(edgePen, sx - r, sy - r, bPx, bPx)
                Else
                    ' Rectángulo con rotación
                    DrawRotatedRect(g, fillBrush, edgePen, sx, sy, bPx, dPx, CSng(ang))
                End If

                If opt.ShowLblFrame Then
                    g.DrawString(la.LineID, lblFnt, lblBrush, sx + bPx / 2 + 1, sy)
                End If
                If opt.ShowLblJoint Then
                    Using jFnt As New Font("Segoe UI", 9.5F)
                    Using jBrush As New SolidBrush(CLR_LBL_JOINT)
                        g.DrawString(ln.Point1, jFnt, jBrush, sx - jFnt.Size, sy - jFnt.Size * 2)
                    End Using : End Using
                End If
            Next
        End Using : End Using : End Using : End Using
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  VISTA EN ALZADO (XZ o YZ)
    ' ════════════════════════════════════════════════════════════════════════

    Public Function RenderAlzado(model As E2kModel, opt As RenderOptions, W As Integer, H As Integer) As Bitmap
        Dim bmp As New Bitmap(Math.Max(W, 10), Math.Max(H, 10))
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.HighSpeed
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(CLR_BG)

            If model.Points.Count = 0 OrElse model.Stories.Count = 0 Then Return bmp

            Dim isXZ = (opt.AlzDir = "XZ")
            Dim hVals = model.Points.Values.Select(Function(p) If(isXZ, p.X, p.Y))
            Dim hMin = hVals.Min() : Dim hMax = hVals.Max()
            Dim zMax = model.MaxElevation()
            Dim hRange = Math.Max(hMax - hMin, 1.0)
            Dim zRange = Math.Max(zMax, 1.0)

            Dim hPad = hRange * 0.1 : Dim zPad = zRange * 0.08
            hMin -= hPad : hMax += hPad
            Dim hRangeP = hMax - hMin
            Dim zRangeP = zRange + zPad

            Dim scale = FitScale(CSng(hRangeP), CSng(zRangeP), W, H) * opt.UserZoom
            Dim hCenter = (hMin + hMax) / 2.0

            Dim Th As Func(Of Double, Single) =
                Function(hv) CSng(W / 2.0 + (hv - hCenter) * scale + opt.PanX)
            Dim Tz As Func(Of Double, Single) =
                Function(z) CSng(H - MARGIN - z * scale + opt.PanY)

            ' Líneas de piso
            DrawStoryLines(g, model, Th, Tz, W)

            ' Muros
            If opt.ShowMuros Then DrawWallsXZ(g, model, opt, Th, Tz, isXZ, scale)

            ' Columnas
            If opt.ShowColumnas Then DrawColumnsXZ(g, model, opt, Th, Tz, isXZ, scale)

            ' Vigas / Nervios
            If opt.ShowVigas OrElse opt.ShowNervios Then DrawBeamsXZ(g, model, opt, Th, Tz, isXZ, scale)

            DrawScaleBar(g, scale, W, H)
        End Using
        Return bmp
    End Function

    Private Sub DrawStoryLines(g As Graphics, model As E2kModel,
                                Th As Func(Of Double, Single), Tz As Func(Of Double, Single), W As Integer)
        Using linePen As New Pen(CLR_STORY_LINE, 1) With {.DashStyle = DashStyle.Dot}
        Using lblFnt As New Font("Segoe UI", 11.5F)
        Using lblBrush As New SolidBrush(CLR_GRID_LBL)
            For Each st In model.Stories
                If st.ElevTop = 0 AndAlso Not st.IsMaster AndAlso st.Name.ToUpper().Contains("S2") Then Continue For
                Dim sy = Tz(st.ElevTop)
                g.DrawLine(linePen, 0, sy, W, sy)
                g.DrawString($"{st.Name}  ({st.ElevTop:F2}m)", lblFnt, lblBrush, 2, sy - 14)
            Next
        End Using : End Using : End Using
    End Sub

    Private Sub DrawWallsXZ(g As Graphics, model As E2kModel, opt As RenderOptions,
                              Th As Func(Of Double, Single), Tz As Func(Of Double, Single),
                              isXZ As Boolean, scale As Single)
        Using brush As New SolidBrush(Color.FromArgb(150, 130, 130, 145))
        Using pen As New Pen(CLR_WALL_EDGE, 0.8F)
            For Each aa In model.AreaAssigns
                If Not StoryMatch(aa.Story, opt) Then Continue For
                Dim ar = GetArea(model, aa.AreaID)
                If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For
                Dim st = model.GetStory(aa.Story)
                If st Is Nothing Then Continue For
                Dim uniq = UniqueAreaPoints(model, ar)
                If uniq.Count < 2 Then Continue For

                For i = 0 To uniq.Count - 2
                    Dim p1 = uniq(i) : Dim p2 = uniq((i + 1) Mod uniq.Count)
                    Dim h1 = If(isXZ, p1.X, p1.Y)
                    Dim h2 = If(isXZ, p2.X, p2.Y)
                    Dim hMid = (h1 + h2) / 2.0
                    Dim thick = ParseThickness(aa.Section) * scale
                    thick = Math.Max(thick, 2.0F)
                    Dim sh = Th(hMid)
                    Dim sz_bot = Tz(st.ElevBottom)
                    Dim sz_top = Tz(st.ElevTop)
                    Dim rect = New RectangleF(sh - thick / 2, sz_top, thick, Math.Abs(sz_bot - sz_top))
                    g.FillRectangle(brush, rect)
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
                Next i
            Next
        End Using : End Using
    End Sub

    Private Sub DrawColumnsXZ(g As Graphics, model As E2kModel, opt As RenderOptions,
                                Th As Func(Of Double, Single), Tz As Func(Of Double, Single),
                                isXZ As Boolean, scale As Single)
        Using brush As New SolidBrush(Color.FromArgb(180, 40, 80, 180))
        Using pen As New Pen(CLR_COL_EDGE, 0.8F)
            For Each la In model.LineAssigns
                If Not StoryMatch(la.Story, opt) Then Continue For
                Dim ln = GetLine(model, la.LineID)
                If ln Is Nothing OrElse ln.LineType <> E2kLineType.Column Then Continue For
                Dim pt = GetPoint(model, ln.Point1)
                If pt Is Nothing Then Continue For
                Dim st = model.GetStory(la.Story)
                If st Is Nothing Then Continue For
                Dim sec = GetSection(model, la.Section)
                Dim B = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B, 0.3)
                Dim h = If(isXZ, pt.X, pt.Y)
                Dim bPx = CSng(B * scale) : bPx = Math.Max(bPx, 3)
                Dim sh = Th(h)
                Dim sz_bot = Tz(st.ElevBottom)
                Dim sz_top = Tz(st.ElevTop)
                Dim rect = New RectangleF(sh - bPx / 2, sz_top, bPx, Math.Abs(sz_bot - sz_top))
                g.FillRectangle(brush, rect)
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
            Next
        End Using : End Using
    End Sub

    Private Sub DrawBeamsXZ(g As Graphics, model As E2kModel, opt As RenderOptions,
                              Th As Func(Of Double, Single), Tz As Func(Of Double, Single),
                              isXZ As Boolean, scale As Single)
        Using brushV As New SolidBrush(Color.FromArgb(180, CLR_BEAM_FILL))
        Using brushN As New SolidBrush(Color.FromArgb(180, CLR_NERVIO_FILL))
        Using penV As New Pen(CLR_BEAM_EDGE, 0.8F)
        Using penN As New Pen(CLR_NERVIO_EDGE, 0.8F)
            For Each la In model.LineAssigns
                If Not StoryMatch(la.Story, opt) Then Continue For
                Dim ln = GetLine(model, la.LineID)
                If ln Is Nothing OrElse ln.LineType <> E2kLineType.Beam Then Continue For
                If ln.Point1 = ln.Point2 Then Continue For

                Dim esNervio = IsNervio(la.Section)
                If esNervio AndAlso Not opt.ShowNervios Then Continue For
                If Not esNervio AndAlso Not opt.ShowVigas Then Continue For

                Dim p1 = GetPoint(model, ln.Point1)
                Dim p2 = GetPoint(model, ln.Point2)
                If p1 Is Nothing OrElse p2 Is Nothing Then Continue For
                Dim st = model.GetStory(la.Story)
                If st Is Nothing Then Continue For
                Dim sec = GetSection(model, la.Section)
                Dim D = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D, 0.45)
                Dim h1 = If(isXZ, p1.X, p1.Y)
                Dim h2 = If(isXZ, p2.X, p2.Y)
                Dim sh1 = Th(h1) : Dim sh2 = Th(h2)
                Dim sz_top = Tz(st.ElevTop)
                Dim dPx = CSng(D * scale) : dPx = Math.Max(dPx, 3)
                Dim rect = New RectangleF(Math.Min(sh1, sh2), sz_top, Math.Abs(sh2 - sh1), dPx)
                g.FillRectangle(If(esNervio, brushN, brushV), rect)
                g.DrawRectangle(If(esNervio, penN, penV), rect.X, rect.Y, rect.Width, rect.Height)
            Next
        End Using : End Using : End Using : End Using
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  VISTA 3D — Proyección ortográfica con rotación
    ' ════════════════════════════════════════════════════════════════════════

    Public Function Render3D(model As E2kModel, opt As RenderOptions, W As Integer, H As Integer) As Bitmap
        Dim bmp As New Bitmap(Math.Max(W, 10), Math.Max(H, 10))
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.HighSpeed
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(CLR_BG)

            If model.Points.Count = 0 Then Return bmp

            ' Cámara
            Dim azR = opt.Azimuth * Math.PI / 180.0
            Dim elR = opt.Elevation * Math.PI / 180.0
            Dim rX = -Math.Sin(azR), rY = Math.Cos(azR), rZ = 0.0
            Dim uX = -Math.Sin(elR) * Math.Cos(azR)
            Dim uY = -Math.Sin(elR) * Math.Sin(azR)
            Dim uZ = Math.Cos(elR)
            Dim fX = Math.Cos(elR) * Math.Cos(azR)
            Dim fY = Math.Cos(elR) * Math.Sin(azR)
            Dim fZ = Math.Sin(elR)

            Dim bounds = model.GetBoundsXY()
            Dim zMax = model.MaxElevation()
            Dim cx = CDbl(bounds.X + bounds.Width / 2)
            Dim cy = CDbl(bounds.Y + bounds.Height / 2)
            Dim cz = zMax / 2.0
            Dim modelSize = Math.Max(Math.Max(bounds.Width, bounds.Height), zMax)
            Dim baseScale = Math.Min(W, H) * 0.38 / Math.Max(modelSize, 1)
            Dim scale = baseScale * opt.Zoom3D

            Dim Proj As Func(Of V3, PointF) = Function(v)
                Dim dx = v.X - cx, dy = v.Y - cy, dz = v.Z - cz
                Dim sx = (dx * rX + dy * rY + dz * rZ) * scale + W / 2.0 + opt.Pan3DX
                Dim sy = -(dx * uX + dy * uY + dz * uZ) * scale + H / 2.0 + opt.Pan3DY
                Return New PointF(CSng(sx), CSng(sy))
            End Function

            Dim Depth As Func(Of V3, Double) = Function(v)
                Return (v.X - cx) * fX + (v.Y - cy) * fY + (v.Z - cz) * fZ
            End Function

            If opt.ModoExtruido Then
                Render3DExtruido(g, model, opt, Proj, Depth)
            Else
                Render3DWire(g, model, opt, Proj)
            End If

            ' Leyenda de ejes
            DrawAxisLegend(g, rX, rY, rZ, uX, uY, uZ, W, H)
        End Using
        Return bmp
    End Function

    Private Sub Render3DWire(g As Graphics, model As E2kModel, opt As RenderOptions,
                               Proj As Func(Of V3, PointF))
        Using penCol As New Pen(Color.FromArgb(60, 80, 180), 1.2F)
        Using penBeam As New Pen(Color.FromArgb(20, 140, 60), 1.2F)
        Using penNervio As New Pen(CLR_NERVIO_FILL, 1.2F)
        Using penWall As New Pen(Color.FromArgb(100, 100, 115), 1.0F)
        Using penSlab As New Pen(Color.FromArgb(160, 140, 110), 0.8F)
            ' Losas
            If opt.ShowLosas Then
                For Each aa In model.AreaAssigns
                    If Not StoryMatch(aa.Story, opt) Then Continue For
                    Dim ar = GetArea(model, aa.AreaID)
                    If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Floor Then Continue For
                    Dim st = model.GetStory(aa.Story)
                    If st Is Nothing Then Continue For
                    Dim pts = ar.Points.Select(Function(pid) Proj(New V3(GetPX(model, pid), GetPY(model, pid), st.ElevTop))).ToArray()
                    If pts.Length >= 2 Then g.DrawPolygon(penSlab, pts)
                Next
            End If
            ' Muros
            If opt.ShowMuros Then
                For Each aa In model.AreaAssigns
                    If Not StoryMatch(aa.Story, opt) Then Continue For
                    Dim ar = GetArea(model, aa.AreaID)
                    If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For
                    Dim st = model.GetStory(aa.Story)
                    If st Is Nothing Then Continue For
                    Dim uniq = UniqueAreaPoints(model, ar)
                    If uniq.Count < 2 Then Continue For
                    For i = 0 To uniq.Count - 2
                        Dim p1 = uniq(i) : Dim p2 = uniq(i + 1)
                        DrawWallWire(g, penWall, Proj, p1, p2, st.ElevBottom, st.ElevTop)
                    Next i
                Next
            End If
            ' Columnas
            If opt.ShowColumnas Then
                For Each la In model.LineAssigns
                    If Not StoryMatch(la.Story, opt) Then Continue For
                    Dim ln = GetLine(model, la.LineID)
                    If ln Is Nothing OrElse ln.LineType <> E2kLineType.Column Then Continue For
                    Dim pt = GetPoint(model, ln.Point1)
                    If pt Is Nothing Then Continue For
                    Dim st = model.GetStory(la.Story)
                    If st Is Nothing Then Continue For
                    Dim sec = GetSection(model, la.Section)
                    Dim B = If(sec IsNot Nothing, sec.B, 0.3)
                    Dim D = If(sec IsNot Nothing, sec.D, 0.3)
                    If sec IsNot Nothing AndAlso sec.Shape = E2kSectionShape.Circle Then
                        DrawCircleWire(g, penCol, Proj, pt.X, pt.Y, B / 2, st.ElevBottom, st.ElevTop)
                    Else
                        DrawBoxWire(g, penCol, Proj, ColumnBox(pt.X, pt.Y, B, D, la.Angle, st.ElevBottom, st.ElevTop))
                    End If
                Next
            End If
            ' Vigas / Nervios
            If opt.ShowVigas OrElse opt.ShowNervios Then
                For Each la In model.LineAssigns
                    If Not StoryMatch(la.Story, opt) Then Continue For
                    Dim ln = GetLine(model, la.LineID)
                    If ln Is Nothing OrElse ln.LineType <> E2kLineType.Beam Then Continue For
                    If ln.Point1 = ln.Point2 Then Continue For
                    Dim esNervio = IsNervio(la.Section)
                    If esNervio AndAlso Not opt.ShowNervios Then Continue For
                    If Not esNervio AndAlso Not opt.ShowVigas Then Continue For
                    Dim p1 = GetPoint(model, ln.Point1)
                    Dim p2 = GetPoint(model, ln.Point2)
                    If p1 Is Nothing OrElse p2 Is Nothing Then Continue For
                    Dim st = model.GetStory(la.Story)
                    If st Is Nothing Then Continue For
                    Dim sec = GetSection(model, la.Section)
                    Dim B = If(sec IsNot Nothing, sec.B, 0.3)
                    Dim D = If(sec IsNot Nothing, sec.D, 0.45)
                    DrawBoxWire(g, If(esNervio, penNervio, penBeam), Proj, BeamBox(p1.X, p1.Y, p2.X, p2.Y, B, D, st.ElevTop))
                Next
            End If
        End Using : End Using : End Using : End Using : End Using
    End Sub

    Private Sub Render3DExtruido(g As Graphics, model As E2kModel, opt As RenderOptions,
                                   Proj As Func(Of V3, PointF), Depth As Func(Of V3, Double))
        ' Clase interna para cada cara
        Dim faces As New List(Of FaceDrawCmd)()

        ' Losas
        If opt.ShowLosas Then
            For Each aa In model.AreaAssigns
                If Not StoryMatch(aa.Story, opt) Then Continue For
                Dim ar = GetArea(model, aa.AreaID)
                If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Floor Then Continue For
                Dim st = model.GetStory(aa.Story)
                If st Is Nothing Then Continue For
                Dim verts = ar.Points.Select(Function(pid) New V3(GetPX(model, pid), GetPY(model, pid), st.ElevTop)).ToArray()
                If verts.Length >= 3 Then
                    AddFace(faces, verts, CLR_SLAB_FILL, CLR_SLAB_EDGE, Depth)
                End If
            Next
        End If

        ' Muros
        If opt.ShowMuros Then
            For Each aa In model.AreaAssigns
                If Not StoryMatch(aa.Story, opt) Then Continue For
                Dim ar = GetArea(model, aa.AreaID)
                If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For
                Dim st = model.GetStory(aa.Story)
                If st Is Nothing Then Continue For
                Dim uniq = UniqueAreaPoints(model, ar)
                If uniq.Count < 2 Then Continue For
                For i = 0 To uniq.Count - 2
                    Dim pt1 = uniq(i) : Dim pt2 = uniq(i + 1)
                    Dim wallV() = {
                        New V3(pt1.X, pt1.Y, st.ElevBottom),
                        New V3(pt2.X, pt2.Y, st.ElevBottom),
                        New V3(pt2.X, pt2.Y, st.ElevTop),
                        New V3(pt1.X, pt1.Y, st.ElevTop)
                    }
                    AddFace(faces, wallV, Color.FromArgb(160, CLR_WALL_FILL), CLR_WALL_EDGE, Depth)
                Next i
            Next
        End If

        ' Columnas y vigas
        For Each la In model.LineAssigns
            If Not StoryMatch(la.Story, opt) Then Continue For
            Dim ln = GetLine(model, la.LineID)
            If ln Is Nothing Then Continue For
            Dim st = model.GetStory(la.Story)
            If st Is Nothing Then Continue For
            Dim sec = GetSection(model, la.Section)

            If ln.LineType = E2kLineType.Column AndAlso opt.ShowColumnas Then
                Dim pt = GetPoint(model, ln.Point1)
                If pt Is Nothing Then Continue For
                Dim B = If(sec IsNot Nothing, sec.B, 0.3)
                Dim D = If(sec IsNot Nothing, sec.D, 0.3)
                Dim verts = ColumnBox(pt.X, pt.Y, B, D, la.Angle, st.ElevBottom, st.ElevTop)
                If verts.Length = 8 Then AddBoxFaces(faces, verts, Color.FromArgb(200, CLR_COL_FILL), CLR_COL_EDGE, Depth)

            ElseIf ln.LineType = E2kLineType.Beam AndAlso ln.Point1 <> ln.Point2 Then
                Dim esNervio = IsNervio(la.Section)
                If esNervio AndAlso Not opt.ShowNervios Then Continue For
                If Not esNervio AndAlso Not opt.ShowVigas Then Continue For
                Dim p1 = GetPoint(model, ln.Point1)
                Dim p2 = GetPoint(model, ln.Point2)
                If p1 Is Nothing OrElse p2 Is Nothing Then Continue For
                Dim B = If(sec IsNot Nothing, sec.B, 0.3)
                Dim D = If(sec IsNot Nothing, sec.D, 0.45)
                Dim verts = BeamBox(p1.X, p1.Y, p2.X, p2.Y, B, D, st.ElevTop)
                Dim clrF = If(esNervio, Color.FromArgb(200, CLR_NERVIO_FILL), Color.FromArgb(200, CLR_BEAM_FILL))
                Dim clrE = If(esNervio, CLR_NERVIO_EDGE, CLR_BEAM_EDGE)
                If verts.Length = 8 Then AddBoxFaces(faces, verts, clrF, clrE, Depth)
            End If
        Next

        ' Ordenar por profundidad (más lejos primero)
        faces.Sort(Function(a, b) b.Depth.CompareTo(a.Depth))

        ' Dibujar
        For Each fc In faces
            Dim screenPts = fc.Verts.Select(Function(v) Proj(v)).ToArray()
            Using brush As New SolidBrush(fc.Fill)
                g.FillPolygon(brush, screenPts)
            End Using
            Using pen As New Pen(fc.Edge, 0.7F)
                g.DrawPolygon(pen, screenPts)
            End Using
        Next
    End Sub

    Private Class FaceDrawCmd
        Public Verts() As V3
        Public Fill As Color
        Public Edge As Color
        Public Depth As Double
    End Class

    Private Sub AddFace(faces As List(Of FaceDrawCmd), verts() As V3, fill As Color, edge As Color,
                          Depth As Func(Of V3, Double))
        Dim centerZ = verts.Average(Function(v) Depth(v))
        ' Iluminación simple
        Dim n = FaceNormal(verts)
        Dim intensity = LightIntensity(n.X, n.Y, n.Z)
        Dim shadedFill = ShadeColor(fill, intensity)
        faces.Add(New FaceDrawCmd() With {.Verts = verts, .Fill = shadedFill, .Edge = edge, .Depth = centerZ})
    End Sub

    Private Sub AddBoxFaces(faces As List(Of FaceDrawCmd), verts() As V3, fill As Color, edge As Color,
                              Depth As Func(Of V3, Double))
        ' 6 caras de un box con 8 vértices (0-3 base, 4-7 tope)
        Dim faceIdxs()() As Integer = {
            New Integer() {0, 1, 2, 3},
            New Integer() {7, 6, 5, 4},
            New Integer() {0, 4, 5, 1},
            New Integer() {1, 5, 6, 2},
            New Integer() {2, 6, 7, 3},
            New Integer() {3, 7, 4, 0}
        }
        For Each fi In faceIdxs
            Dim fv() As V3 = {verts(fi(0)), verts(fi(1)), verts(fi(2)), verts(fi(3))}
            AddFace(faces, fv, fill, edge, Depth)
        Next
    End Sub

    ' ─── Geometría de elementos ──────────────────────────────────────────────

    Private Function ColumnBox(cx As Double, cy As Double, B As Double, D As Double,
                                ang As Double, zBot As Double, zTop As Double) As V3()
        Dim rad = ang * Math.PI / 180.0
        Dim cos2 = Math.Cos(rad), sin2 = Math.Sin(rad)
        Dim b2 = B / 2, d2 = D / 2
        Dim ox() As Double = {b2 * cos2 - d2 * sin2, b2 * cos2 + d2 * sin2,
                               -b2 * cos2 + d2 * sin2, -b2 * cos2 - d2 * sin2}
        Dim oy() As Double = {b2 * sin2 + d2 * cos2, b2 * sin2 - d2 * cos2,
                               -b2 * sin2 - d2 * cos2, -b2 * sin2 + d2 * cos2}
        Dim v(7) As V3
        For i = 0 To 3
            v(i) = New V3(cx + ox(i), cy + oy(i), zBot)
            v(i + 4) = New V3(cx + ox(i), cy + oy(i), zTop)
        Next i
        Return v
    End Function

    Private Function BeamBox(x1 As Double, y1 As Double, x2 As Double, y2 As Double,
                              B As Double, D As Double, zTop As Double) As V3()
        Dim dx = x2 - x1, dy = y2 - y1
        Dim L = Math.Sqrt(dx * dx + dy * dy)
        If L < 0.001 Then Return New V3() {}
        Dim nx = -dy / L, ny = dx / L
        Dim b2 = B / 2
        Dim zBot = zTop - D
        Dim v(7) As V3
        v(0) = New V3(x1 + nx * b2, y1 + ny * b2, zBot)
        v(1) = New V3(x1 - nx * b2, y1 - ny * b2, zBot)
        v(2) = New V3(x2 - nx * b2, y2 - ny * b2, zBot)
        v(3) = New V3(x2 + nx * b2, y2 + ny * b2, zBot)
        v(4) = New V3(x1 + nx * b2, y1 + ny * b2, zTop)
        v(5) = New V3(x1 - nx * b2, y1 - ny * b2, zTop)
        v(6) = New V3(x2 - nx * b2, y2 - ny * b2, zTop)
        v(7) = New V3(x2 + nx * b2, y2 + ny * b2, zTop)
        Return v
    End Function

    Private Sub DrawBoxWire(g As Graphics, pen As Pen, Proj As Func(Of V3, PointF), verts() As V3)
        If verts.Length < 8 Then Return
        Dim edges()() As Integer = {
            New Integer() {0, 1}, New Integer() {1, 2}, New Integer() {2, 3}, New Integer() {3, 0},
            New Integer() {4, 5}, New Integer() {5, 6}, New Integer() {6, 7}, New Integer() {7, 4},
            New Integer() {0, 4}, New Integer() {1, 5}, New Integer() {2, 6}, New Integer() {3, 7}
        }
        For Each e In edges
            Dim p1 = Proj(verts(e(0))) : Dim p2 = Proj(verts(e(1)))
            g.DrawLine(pen, p1, p2)
        Next
    End Sub

    Private Sub DrawWallWire(g As Graphics, pen As Pen, Proj As Func(Of V3, PointF),
                               p1 As E2kPoint, p2 As E2kPoint, zBot As Double, zTop As Double)
        Dim v00 = Proj(New V3(p1.X, p1.Y, zBot))
        Dim v10 = Proj(New V3(p2.X, p2.Y, zBot))
        Dim v01 = Proj(New V3(p1.X, p1.Y, zTop))
        Dim v11 = Proj(New V3(p2.X, p2.Y, zTop))
        g.DrawLine(pen, v00, v10)
        g.DrawLine(pen, v01, v11)
        g.DrawLine(pen, v00, v01)
        g.DrawLine(pen, v10, v11)
    End Sub

    Private Sub DrawCircleWire(g As Graphics, pen As Pen, Proj As Func(Of V3, PointF),
                                 cx As Double, cy As Double, r As Double, zBot As Double, zTop As Double)
        Const SEGS = 8
        For i = 0 To SEGS - 1
            Dim a1 = i * 2 * Math.PI / SEGS
            Dim a2 = (i + 1) * 2 * Math.PI / SEGS
            Dim x1 = cx + r * Math.Cos(a1), y1 = cy + r * Math.Sin(a1)
            Dim x2 = cx + r * Math.Cos(a2), y2 = cy + r * Math.Sin(a2)
            g.DrawLine(pen, Proj(New V3(x1, y1, zBot)), Proj(New V3(x2, y2, zBot)))
            g.DrawLine(pen, Proj(New V3(x1, y1, zTop)), Proj(New V3(x2, y2, zTop)))
            g.DrawLine(pen, Proj(New V3(x1, y1, zBot)), Proj(New V3(x1, y1, zTop)))
        Next i
    End Sub

    Private Sub DrawAxisLegend(g As Graphics, rX As Double, rY As Double, rZ As Double,
                                uX As Double, uY As Double, uZ As Double,
                                W As Integer, H As Integer)
        Dim ox = 50, oy = H - 50, L = 30.0
        Using penX As New Pen(Color.FromArgb(200, 30, 30), 2)
        Using penY As New Pen(Color.FromArgb(30, 160, 30), 2)
        Using penZ As New Pen(Color.FromArgb(30, 30, 200), 2)
        Using fnt As New Font("Segoe UI", 7.5F)
            ' X axis
            Dim ex = CSng(ox + rX * L) : Dim ey = CSng(oy - uX * L)
            g.DrawLine(penX, ox, oy, ex, ey)
            g.DrawString("X", fnt, penX.Brush, ex, ey - 8)
            ' Y axis
            ex = CSng(ox + rY * L) : ey = CSng(oy - uY * L)
            g.DrawLine(penY, ox, oy, ex, ey)
            g.DrawString("Y", fnt, penY.Brush, ex, ey - 8)
            ' Z axis
            ex = CSng(ox + rZ * L) : ey = CSng(oy - uZ * L)
            g.DrawLine(penZ, ox, oy, ex, ey)
            g.DrawString("Z", fnt, penZ.Brush, ex, ey - 8)
        End Using : End Using : End Using : End Using
    End Sub

    ' ─── Iluminación y sombreado ─────────────────────────────────────────────

    Private Function FaceNormal(verts() As V3) As V3
        If verts.Length < 3 Then Return New V3(0, 0, 1)
        Dim ax = verts(1).X - verts(0).X, ay = verts(1).Y - verts(0).Y, az = verts(1).Z - verts(0).Z
        Dim bx = verts(2).X - verts(0).X, by = verts(2).Y - verts(0).Y, bz = verts(2).Z - verts(0).Z
        Return New V3(ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx)
    End Function

    Private Function LightIntensity(nx As Double, ny As Double, nz As Double) As Single
        Const lx = 0.4082, ly = 0.4082, lz = 0.8165  ' normalizado (1,1,2)
        Dim L = Math.Sqrt(nx * nx + ny * ny + nz * nz)
        If L < 1e-8 Then Return 0.65F
        Dim dot = (nx * lx + ny * ly + nz * lz) / L
        Return CSng(Math.Max(0.3, Math.Min(1.0, 0.35 + 0.65 * Math.Abs(dot))))
    End Function

    Private Function ShadeColor(base As Color, intensity As Single) As Color
        Return Color.FromArgb(base.A,
            CInt(Math.Min(255, base.R * intensity)),
            CInt(Math.Min(255, base.G * intensity)),
            CInt(Math.Min(255, base.B * intensity)))
    End Function

    ' ─── Escala y barra de escala ────────────────────────────────────────────

    Private Function FitScale(worldW As Single, worldH As Single, picW As Integer, picH As Integer) As Single
        Dim usableW = picW - 2 * MARGIN
        Dim usableH = picH - 2 * MARGIN
        If worldW < 0.001F OrElse worldH < 0.001F Then Return 10.0F
        Dim scX = usableW / worldW : Dim scY = usableH / worldH
        Return CSng(Math.Min(scX, scY))
    End Function

    Private Sub DrawScaleBar(g As Graphics, scale As Single, W As Integer, H As Integer)
        ' Elegir longitud "nice"
        Dim niceLengths() As Double = {0.5, 1, 2, 5, 10, 20, 50, 100}
        Dim targetPx = 80
        Dim niceLen = 1.0
        For Each nl In niceLengths
            If nl * scale >= targetPx Then
                niceLen = nl
                Exit For
            End If
        Next
        Dim barPx = CSng(niceLen * scale)
        Dim x0 = W - barPx - MARGIN : Dim y0 = H - 18.0F
        Using pen As New Pen(Color.FromArgb(80, 80, 80), 2)
            g.DrawLine(pen, x0, y0, x0 + barPx, y0)
            g.DrawLine(pen, x0, y0 - 4, x0, y0 + 4)
            g.DrawLine(pen, x0 + barPx, y0 - 4, x0 + barPx, y0 + 4)
        End Using
        Using fnt As New Font("Segoe UI", 7.5F)
        Using brush As New SolidBrush(Color.FromArgb(80, 80, 80))
            Dim lbl = $"{niceLen:G} m"
            Dim tw = g.MeasureString(lbl, fnt)
            g.DrawString(lbl, fnt, brush, x0 + (barPx - tw.Width) / 2, y0 + 4)
        End Using : End Using
    End Sub

    ' ─── Helpers de acceso a datos ───────────────────────────────────────────

    Private Function StoryMatch(story As String, opt As RenderOptions) As Boolean
        Return opt.FilterStory = "" OrElse
               String.Equals(story, opt.FilterStory, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function GetLine(model As E2kModel, id As String) As E2kLine
        Dim v As E2kLine = Nothing
        model.Lines.TryGetValue(id, v)
        Return v
    End Function

    Private Function GetPoint(model As E2kModel, id As String) As E2kPoint
        Dim v As E2kPoint = Nothing
        model.Points.TryGetValue(id, v)
        Return v
    End Function

    Private Function GetArea(model As E2kModel, id As String) As E2kArea
        Dim v As E2kArea = Nothing
        model.Areas.TryGetValue(id, v)
        Return v
    End Function

    Private Function GetSection(model As E2kModel, name As String) As E2kFrameSection
        Dim v As E2kFrameSection = Nothing
        model.FrameSections.TryGetValue(name, v)
        Return v
    End Function

    Private Function GetPX(model As E2kModel, id As String) As Double
        Dim pt = GetPoint(model, id)
        Return If(pt IsNot Nothing, pt.X, 0.0)
    End Function

    Private Function GetPY(model As E2kModel, id As String) As Double
        Dim pt = GetPoint(model, id)
        Return If(pt IsNot Nothing, pt.Y, 0.0)
    End Function

    Private Function AreaScreenPts(model As E2kModel, ar As E2kArea,
                                    Tx As Func(Of Double, Single), Ty As Func(Of Double, Single)) As PointF()
        Return ar.Points.Where(Function(pid) model.Points.ContainsKey(pid)) _
                        .Select(Function(pid) New PointF(Tx(model.Points(pid).X), Ty(model.Points(pid).Y))) _
                        .ToArray()
    End Function

    Private Function UniqueAreaPoints(model As E2kModel, ar As E2kArea) As List(Of E2kPoint)
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim result As New List(Of E2kPoint)
        For Each pid In ar.Points
            If Not seen.Contains(pid) AndAlso model.Points.ContainsKey(pid) Then
                seen.Add(pid)
                result.Add(model.Points(pid))
            End If
        Next
        Return result
    End Function

    Private Function ParseThickness(secName As String) As Single
        If secName = "" Then Return 0.2F
        Dim parts = secName.Split("_"c)
        For Each part In parts
            Dim v As Single
            If Single.TryParse(part, Globalization.NumberStyles.Float,
                               Globalization.CultureInfo.InvariantCulture, v) Then
                If v >= 0.08F AndAlso v <= 2.0F Then Return v
            End If
        Next
        Return 0.2F
    End Function

    Private Sub DrawRotatedRect(g As Graphics, brush As Brush, pen As Pen,
                                  cx As Single, cy As Single, w As Single, h As Single, ang As Single)
        Dim rad = ang * Math.PI / 180.0
        Dim cos2 = CSng(Math.Cos(rad)), sin2 = CSng(Math.Sin(rad))
        Dim w2 = w / 2, h2 = h / 2
        Dim pts() As PointF = {
            New PointF(cx + w2 * cos2 - h2 * sin2, cy + w2 * sin2 + h2 * cos2),
            New PointF(cx + w2 * cos2 + h2 * sin2, cy + w2 * sin2 - h2 * cos2),
            New PointF(cx - w2 * cos2 + h2 * sin2, cy - w2 * sin2 - h2 * cos2),
            New PointF(cx - w2 * cos2 - h2 * sin2, cy - w2 * sin2 + h2 * cos2)
        }
        g.FillPolygon(brush, pts)
        g.DrawPolygon(pen, pts)
    End Sub

    Private Sub GfxLine(g As Graphics, pen As Pen, x1 As Single, y1 As Single, x2 As Single, y2 As Single)
        Try : g.DrawLine(pen, x1, y1, x2, y2) : Catch : End Try
    End Sub

    Private Function IsNervio(sectionName As String) As Boolean
        If String.IsNullOrEmpty(sectionName) Then Return False
        Return sectionName.IndexOf("_N_", StringComparison.OrdinalIgnoreCase) >= 0
    End Function

End Module
