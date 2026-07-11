Imports System.Drawing.Drawing2D
Imports ARCO.Funciones_02_Columnas

''' <summary>
''' Superficie de interacción P–M2–M3 en 3D con rotación interactiva.
''' Muestra solo las combinaciones de diseño del tramo activo.
''' Criterio de color: C/D = 1 / sqrt((M3/φMn3)² + (M2/φMn2)²) — geométricamente
''' consistente con la superficie elíptica dibujada.
''' </summary>
Public Class Form_02_03_DI_3D
    Inherits Form

    ' ── Datos ────────────────────────────────────────────────────
    Private ReadOnly _tramo As Tramo_Columna
    Private ReadOnly Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    ' ── Superficie: array [pIdx, angleIdx] ───────────────────────
    Private _surf() As Pt3
    Private _nP As Integer = 52
    Private _nAngles As Integer = 36
    Private _maxP As Double, _minP As Double, _maxM As Double

    ' ── Puntos de demanda ─────────────────────────────────────────
    ' Campos: M3, M2, P (diagrama), CD (C/D elíptico), Nombre combo
    Private _demand As List(Of (M3 As Single, M2 As Single, P As Single, CD As Single, Nombre As String))
    Private _screenDemandPts As New List(Of PointF)   ' actualizado en cada Paint
    Private _hoverIdx As Integer = -1                 ' índice hover (-1 = ninguno)

    ' ── Vista ────────────────────────────────────────────────────
    Private _azim As Double = 0.6
    Private _elev As Double = 0.5
    Private _zoom As Double = 1.0
    Private _dragging As Boolean = False
    Private _dragStart As Point
    Private _azStart As Double, _elStart As Double

    ' ── Controles ────────────────────────────────────────────────
    Private ReadOnly picMain As New PictureBox()
    Private ReadOnly panelRight As New Panel()
    Private ReadOnly lblColInfo As New Label()
    Private ReadOnly lblInfo As New Label()
    Private ReadOnly lblHover As New Label()
    Private ReadOnly trkP As New TrackBar()
    Private ReadOnly lblPLevel As New Label()
    Private ReadOnly chkDemands As New CheckBox()
    Private ReadOnly chkSurface As New CheckBox()
    Private ReadOnly btnReset As New Button()

    Private Structure Pt3
        Public X As Single, Y As Single, Z As Single
        Public Sub New(x As Single, y As Single, z As Single)
            Me.X = x : Me.Y = y : Me.Z = z
        End Sub
    End Structure

    ' ══════════════════════════════════════════════════════════════
    Public Sub New(tramo As Tramo_Columna)
        _tramo = tramo
        BuildUI()
        AddHandler Me.Load, AddressOf OnLoad
    End Sub

    Private Sub BuildUI()
        Me.Text = "Diagrama de Interacción Biaxial 3D – P / M3 / M2"
        Me.Size = New Size(980, 700)
        Me.MinimumSize = New Size(720, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 8.5F)

        picMain.BackColor = Color.FromArgb(16, 20, 34)
        picMain.BorderStyle = BorderStyle.None
        picMain.Cursor = Cursors.SizeAll
        AddHandler picMain.Paint, AddressOf MainPaint
        AddHandler picMain.MouseDown, AddressOf MainMouseDown
        AddHandler picMain.MouseMove, AddressOf MainMouseMove
        AddHandler picMain.MouseUp, AddressOf MainMouseUp
        AddHandler picMain.MouseWheel, AddressOf MainMouseWheel
        Me.Controls.Add(picMain)

        panelRight.BackColor = Color.FromArgb(240, 243, 248)
        panelRight.Width = 220
        Me.Controls.Add(panelRight)

        Dim yy = 10

        Dim lblTit = New Label() With {
            .Text = "Superficie P–M3–M2",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .AutoSize = True, .Location = New Point(8, yy),
            .ForeColor = Color.FromArgb(30, 60, 120)}
        panelRight.Controls.Add(lblTit) : yy += 22

        lblColInfo.AutoSize = False : lblColInfo.Size = New Size(204, 50)
        lblColInfo.Location = New Point(8, yy) : lblColInfo.ForeColor = Color.FromArgb(60, 60, 80)
        panelRight.Controls.Add(lblColInfo) : yy += 56

        PanelSep(panelRight, yy) : yy += 16

        chkSurface.Text = "Mostrar superficie"
        chkSurface.Checked = True : chkSurface.Location = New Point(8, yy) : chkSurface.AutoSize = True
        AddHandler chkSurface.CheckedChanged, Sub(s, ev) picMain.Invalidate()
        panelRight.Controls.Add(chkSurface) : yy += 22

        chkDemands.Text = "Mostrar combinaciones"
        chkDemands.Checked = True : chkDemands.Location = New Point(8, yy) : chkDemands.AutoSize = True
        AddHandler chkDemands.CheckedChanged, Sub(s, ev) picMain.Invalidate()
        panelRight.Controls.Add(chkDemands) : yy += 30

        PanelSep(panelRight, yy) : yy += 16

        Dim lblTrk = New Label() With {.Text = "Corte horizontal (P):", .AutoSize = True, .Location = New Point(8, yy)}
        panelRight.Controls.Add(lblTrk) : yy += 18

        lblPLevel.AutoSize = True : lblPLevel.Location = New Point(8, yy)
        lblPLevel.ForeColor = Color.FromArgb(30, 80, 160)
        lblPLevel.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        panelRight.Controls.Add(lblPLevel) : yy += 18

        trkP.Minimum = 0 : trkP.Maximum = 100 : trkP.Value = 50
        trkP.TickFrequency = 10 : trkP.Orientation = Orientation.Vertical
        trkP.Location = New Point(80, yy - 6) : trkP.Height = 160
        AddHandler trkP.ValueChanged, AddressOf TrkP_Changed
        panelRight.Controls.Add(trkP) : yy += 170

        PanelSep(panelRight, yy) : yy += 16

        ' Resultado global del tramo
        lblInfo.AutoSize = False : lblInfo.Size = New Size(204, 80)
        lblInfo.Location = New Point(8, yy)
        lblInfo.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        panelRight.Controls.Add(lblInfo) : yy += 90

        PanelSep(panelRight, yy) : yy += 16

        ' Info del punto hover
        Dim lblHoverTit = New Label() With {
            .Text = "Combinación activa:",
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .AutoSize = True, .Location = New Point(8, yy),
            .ForeColor = Color.FromArgb(50, 50, 80)}
        panelRight.Controls.Add(lblHoverTit) : yy += 18

        lblHover.AutoSize = False : lblHover.Size = New Size(204, 90)
        lblHover.Location = New Point(8, yy)
        lblHover.Font = New Font("Segoe UI", 7.8F)
        lblHover.ForeColor = Color.FromArgb(40, 40, 80)
        lblHover.Text = "(pase el cursor sobre un punto)"
        panelRight.Controls.Add(lblHover) : yy += 96

        Dim btnReset = New Button()
        btnReset.Text = "Restablecer vista"
        btnReset.Size = New Size(150, 26) : btnReset.Location = New Point(8, yy)
        btnReset.BackColor = Color.FromArgb(30, 80, 160) : btnReset.ForeColor = Color.White
        btnReset.FlatStyle = FlatStyle.Flat : btnReset.FlatAppearance.BorderSize = 0
        AddHandler btnReset.Click, Sub(s, ev)
                                       _azim = 0.6 : _elev = 0.5 : _zoom = 1.0
                                       picMain.Invalidate()
                                   End Sub
        panelRight.Controls.Add(btnReset)

        AddHandler Me.Resize, AddressOf OnResize
    End Sub

    Private Sub PanelSep(panel As Panel, y As Integer)
        panel.Controls.Add(New Label() With {
            .Text = "─────────────────────",
            .AutoSize = True, .Location = New Point(4, y), .ForeColor = Color.Silver})
    End Sub

    Private Sub OnLoad(sender As Object, e As EventArgs)
        GenerarSuperficie()
        GenerarDemanda()
        ActualizarInfo()
        OnResize(Nothing, Nothing)
        picMain.Invalidate()
    End Sub

    Private Sub OnResize(sender As Object, e As EventArgs)
        Dim W = Me.ClientSize.Width : Dim H = Me.ClientSize.Height
        panelRight.SetBounds(W - 220, 0, 220, H)
        picMain.SetBounds(0, 0, W - 220, H)
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  GENERACIÓN DE SUPERFICIE (envolvente elíptica φ·Mn)
    ' ══════════════════════════════════════════════════════════════
    Private Sub GenerarSuperficie()
        If _tramo Is Nothing OrElse
           _tramo.Lista_DI_M3_P_Phi Is Nothing OrElse
           _tramo.Lista_DI_M3_P_Phi.Count < 2 Then Return

        Dim pList = _tramo.Lista_DI_M3_P_Phi
        Dim pMax = pList.Max() : Dim pMin = pList.Min()
        _maxP = pMax : _minP = pMin

        ReDim _surf(_nP * _nAngles - 1)
        Dim maxMsofar As Double = 0
        For pIdx = 0 To _nP - 1
            Dim pVal = CSng(pMin + (pMax - pMin) * pIdx / (_nP - 1))
            Dim phiMn3 = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, pVal)
            Dim phiMn2 = InterpolarMnEnPu(_tramo.Lista_DI_M2_P_Phi, _tramo.Lista_DI_M2_Phi, pVal)
            For aIdx = 0 To _nAngles - 1
                Dim theta = 2.0 * Math.PI * aIdx / _nAngles
                Dim m3 = CSng(phiMn3 * Math.Cos(theta))
                Dim m2 = CSng(phiMn2 * Math.Sin(theta))
                _surf(pIdx * _nAngles + aIdx) = New Pt3(m3, m2, pVal)
                If Math.Abs(m3) > maxMsofar Then maxMsofar = Math.Abs(m3)
                If Math.Abs(m2) > maxMsofar Then maxMsofar = Math.Abs(m2)
            Next
        Next
        _maxM = If(maxMsofar < 1, 1, maxMsofar)
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  GENERACIÓN DE DEMANDA — solo combinaciones de diseño
    '  Criterio de color: C/D elíptico = 1/sqrt((M3/φMn3)²+(M2/φMn2)²)
    ' ══════════════════════════════════════════════════════════════
    Private Sub GenerarDemanda()
        _demand = New List(Of (Single, Single, Single, Single, String))
        If _tramo Is Nothing OrElse _tramo.Lista_Combinaciones Is Nothing Then Return

        Dim combosD = Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
        Dim combosAEvaluar = If(combosD IsNot Nothing AndAlso combosD.Any(),
                                _tramo.Lista_Combinaciones.Where(Function(c) combosD.Contains(c.Name)).ToList(),
                                _tramo.Lista_Combinaciones)

        For Each combo In combosAEvaluar
            Dim Pu_dia As Single = -combo.P          ' convención diagrama: compresión positiva
            Dim M3u = Math.Abs(combo.M3)
            Dim M2u = Math.Abs(combo.M2)
            Dim phiMn3 = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, Pu_dia)
            Dim phiMn2 = InterpolarMnEnPu(_tramo.Lista_DI_M2_P_Phi, _tramo.Lista_DI_M2_Phi, Pu_dia)

            ' Criterio elíptico: DC_e = sqrt((M3/φMn3)² + (M2/φMn2)²)
            ' Consistente con la superficie mostrada → punto rojo = fuera de la elipse
            Dim r3 = If(phiMn3 > 0.001F, CDbl(M3u) / phiMn3, If(M3u > 0.001F, 999.0, 0.0))
            Dim r2 = If(phiMn2 > 0.001F, CDbl(M2u) / phiMn2, If(M2u > 0.001F, 999.0, 0.0))
            Dim DC_e = CSng(Math.Sqrt(r3 * r3 + r2 * r2))
            Dim CD = If(DC_e > 0.001F, CSng(Math.Round(1.0F / DC_e, 3)), 99.999F)

            ' Conservar signo original para plotear en el cuadrante correcto
            Dim m3s = If(combo.M3 >= 0, M3u, -M3u)
            Dim m2s = If(combo.M2 >= 0, M2u, -M2u)
            _demand.Add((m3s, m2s, Pu_dia, CD, combo.Name))
        Next
    End Sub

    Private Sub ActualizarInfo()
        If _tramo Is Nothing Then Return
        Dim Bcm = _tramo.B_Plano * 100 : Dim Hcm = _tramo.H_Plano * 100
        lblColInfo.Text = $"B={Bcm:F0}cm  H={Hcm:F0}cm" & Environment.NewLine &
                          $"f'c={_tramo.fc:F0}MPa  fy=420MPa" & Environment.NewLine &
                          $"N barras: {_tramo.Lista_Detalles_Refuerzo_Top.Count}"

        If _tramo.F_Interaccion > 0 Then
            ' F_Interaccion almacena D/C (Bresler lineal); convertir a C/D para mostrar
            Dim dc = _tramo.F_Interaccion
            Dim cd = If(dc > 0.001F, Math.Round(1.0 / dc, 3), 99.999)
            Dim cumple = dc <= 1.0
            lblInfo.Text = $"C/D = {cd:F3}" & Environment.NewLine &
                           If(cumple, "✓ CUMPLE", "✗ NO CUMPLE") & Environment.NewLine &
                           $"Combo: {_tramo.Combo_Gobernante_DI}" & Environment.NewLine &
                           $"Pu = {_tramo.Pu_Gob_DI:F0} kN"
            lblInfo.ForeColor = If(cumple, Color.FromArgb(0, 120, 50), Color.FromArgb(180, 30, 30))
        End If
    End Sub

    Private Sub TrkP_Changed(sender As Object, e As EventArgs)
        If _maxP = _minP Then Return
        Dim pVal = CSng(_minP + (_maxP - _minP) * trkP.Value / 100.0)
        Dim phiMn3 = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, pVal)
        Dim phiMn2 = InterpolarMnEnPu(_tramo.Lista_DI_M2_P_Phi, _tramo.Lista_DI_M2_Phi, pVal)
        lblPLevel.Text = $"P = {pVal:F0} kN" & Environment.NewLine &
                         $"φMn3 = {phiMn3:F0}" & Environment.NewLine &
                         $"φMn2 = {phiMn2:F0} kN·m"
        picMain.Invalidate()
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  MOUSE
    ' ══════════════════════════════════════════════════════════════
    Private Sub MainMouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            _dragging = True : _dragStart = e.Location
            _azStart = _azim : _elStart = _elev
        End If
    End Sub

    Private Sub MainMouseMove(sender As Object, e As MouseEventArgs)
        If _dragging Then
            _azim = _azStart + (e.X - _dragStart.X) / 200.0
            _elev = Math.Max(-1.4, Math.Min(1.4, _elStart - (e.Y - _dragStart.Y) / 200.0))
            picMain.Invalidate()
            Return
        End If

        ' Detectar hover sobre puntos de demanda
        Const HIT_R As Integer = 8
        Dim best As Integer = -1
        Dim bestD2 As Double = HIT_R * HIT_R
        For k = 0 To _screenDemandPts.Count - 1
            Dim sp = _screenDemandPts(k)
            Dim dx = e.X - sp.X : Dim dy = e.Y - sp.Y
            Dim d2 = dx * dx + dy * dy
            If d2 < bestD2 Then bestD2 = d2 : best = k
        Next

        If best <> _hoverIdx Then
            _hoverIdx = best
            If best >= 0 AndAlso best < _demand.Count Then
                Dim d = _demand(best)
                Dim cumple = d.CD >= 1.0
                lblHover.Text = $"Combo: {d.Nombre}" & Environment.NewLine &
                                $"Pu = {-d.P:F1} kN" & Environment.NewLine &
                                $"M3u = {d.M3:F2} kN·m" & Environment.NewLine &
                                $"M2u = {d.M2:F2} kN·m" & Environment.NewLine &
                                $"C/D = {d.CD:F3}  {If(cumple, "✓", "✗")}"
                lblHover.ForeColor = If(cumple, Color.FromArgb(0, 120, 50), Color.FromArgb(180, 30, 30))
            Else
                lblHover.Text = "(pase el cursor sobre un punto)"
                lblHover.ForeColor = Color.FromArgb(40, 40, 80)
            End If
            picMain.Invalidate()
        End If
    End Sub

    Private Sub MainMouseUp(sender As Object, e As MouseEventArgs)
        _dragging = False
    End Sub

    Private Sub MainMouseWheel(sender As Object, e As MouseEventArgs)
        _zoom = Math.Max(0.3, Math.Min(4.0, _zoom * If(e.Delta > 0, 1.12, 0.89)))
        picMain.Invalidate()
    End Sub

    ' ══════════════════════════════════════════════════════════════
    '  PINTURA
    ' ══════════════════════════════════════════════════════════════
    Private Sub MainPaint(sender As Object, e As PaintEventArgs)
        Dim gfx = e.Graphics
        gfx.Clear(Color.FromArgb(16, 20, 34))
        gfx.SmoothingMode = SmoothingMode.AntiAlias

        If _surf Is Nothing OrElse _maxM = 0 Then
            gfx.DrawString("Sin datos – presione 'Calcular Diagrama' primero",
                           New Font("Segoe UI", 10), Brushes.Gray, 30, picMain.Height / 2 - 10)
            Return
        End If

        Dim W = picMain.Width : Dim H = picMain.Height
        Dim cx = W / 2.0F : Dim cy = H / 2.0F
        Dim baseScale As Double = Math.Min(W, H) * 0.38 * _zoom

        Dim cosAz = Math.Cos(_azim) : Dim sinAz = Math.Sin(_azim)
        Dim cosEl = Math.Cos(_elev) : Dim sinEl = Math.Sin(_elev)
        Dim pRange = _maxP - _minP : If pRange = 0 Then Return

        ' ── Proyecciones de superficie ──────────────────────────
        Dim screenPts((_nP * _nAngles) - 1) As PointF
        Dim depths((_nP * _nAngles) - 1) As Double
        For idx = 0 To _surf.Length - 1
            Dim nx = _surf(idx).X / _maxM
            Dim ny = _surf(idx).Y / _maxM
            Dim nz = (_surf(idx).Z - (_maxP + _minP) / 2.0F) / pRange
            screenPts(idx) = Project(nx, ny, nz, cosAz, sinAz, cosEl, sinEl, cx, cy, baseScale, depths(idx))
        Next

        ' ── Superficie ─────────────────────────────────────────
        If chkSurface.Checked Then
            Dim ringOrder = Enumerable.Range(0, _nP).OrderByDescending(Function(pi) RingDepth(pi, depths)).ToList()
            For Each pIdx In ringOrder
                Dim pts(_nAngles) As Point
                For aIdx = 0 To _nAngles - 1
                    Dim sp = screenPts(pIdx * _nAngles + aIdx)
                    pts(aIdx) = New Point(CInt(sp.X), CInt(sp.Y))
                Next
                pts(_nAngles) = pts(0)
                Dim depth = RingDepth(pIdx, depths)
                Dim alpha = CInt(Math.Max(40, Math.Min(160, 40 + depth * 100)))
                gfx.DrawLines(New Pen(Color.FromArgb(alpha, 80, 140, 220), 0.7F), pts)
            Next
            For aIdx = 0 To _nAngles - 1
                If aIdx Mod 3 <> 0 Then Continue For
                Dim pts(_nP - 1) As Point
                For pIdx = 0 To _nP - 1
                    Dim sp = screenPts(pIdx * _nAngles + aIdx)
                    pts(pIdx) = New Point(CInt(sp.X), CInt(sp.Y))
                Next
                gfx.DrawLines(New Pen(Color.FromArgb(50, 100, 160, 220), 0.5F), pts)
            Next
        End If

        ' ── Corte horizontal ────────────────────────────────────
        Dim sliceP = CSng(_minP + (_maxP - _minP) * trkP.Value / 100.0)
        DrawHorizontalSlice(gfx, sliceP, cx, cy, baseScale, cosAz, sinAz, cosEl, sinEl)

        ' ── Ejes ────────────────────────────────────────────────
        DrawAxes(gfx, cx, cy, baseScale, cosAz, sinAz, cosEl, sinEl)

        ' ── Puntos de demanda ───────────────────────────────────
        _screenDemandPts.Clear()
        If chkDemands.Checked AndAlso _demand IsNot Nothing Then
            For k = 0 To _demand.Count - 1
                Dim d = _demand(k)
                Dim nx = CDbl(d.M3) / _maxM
                Dim ny = CDbl(d.M2) / _maxM
                Dim nz = (CDbl(d.P) - (_maxP + _minP) / 2.0) / pRange
                Dim dep As Double = 0
                Dim sp = Project(nx, ny, nz, cosAz, sinAz, cosEl, sinEl, cx, cy, baseScale, dep)
                _screenDemandPts.Add(sp)

                Dim isCumple = d.CD >= 1.0
                Dim isHover = (k = _hoverIdx)
                Dim dotColor = If(isCumple, Color.FromArgb(80, 220, 100), Color.FromArgb(255, 80, 80))
                Dim r = If(isHover, 6, If(isCumple, 3, 4))

                gfx.FillEllipse(New SolidBrush(dotColor), sp.X - r, sp.Y - r, 2 * r, 2 * r)
                If isHover Then
                    gfx.DrawEllipse(New Pen(Color.White, 1.5F), sp.X - r, sp.Y - r, 2 * r, 2 * r)
                Else
                    gfx.DrawEllipse(New Pen(Color.FromArgb(120, Color.White), 0.5F), sp.X - r, sp.Y - r, 2 * r, 2 * r)
                End If
            Next
        End If

        ' ── Leyenda ─────────────────────────────────────────────
        Dim fSmall = New Font("Segoe UI", 7.5F)
        gfx.DrawString("● Cumple (C/D ≥ 1)", fSmall, New SolidBrush(Color.FromArgb(80, 220, 100)), 8, H - 36)
        gfx.DrawString("● No cumple (C/D < 1)", fSmall, New SolidBrush(Color.FromArgb(255, 80, 80)), 8, H - 20)
        gfx.DrawString("Arrastrar: rotar  |  Rueda: zoom  |  Hover: info combo", fSmall, Brushes.DimGray, 200, H - 18)
    End Sub

    Private Sub DrawHorizontalSlice(gfx As Graphics, pVal As Single,
                                     cx As Single, cy As Single, scale As Double,
                                     cosAz As Double, sinAz As Double,
                                     cosEl As Double, sinEl As Double)
        Dim phiMn3 = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, pVal)
        Dim phiMn2 = InterpolarMnEnPu(_tramo.Lista_DI_M2_P_Phi, _tramo.Lista_DI_M2_Phi, pVal)
        If phiMn3 < 0.1 AndAlso phiMn2 < 0.1 Then Return
        Dim pRange = _maxP - _minP : If pRange = 0 Then Return
        Dim nz = (pVal - (_maxP + _minP) / 2.0) / pRange
        Dim pts(_nAngles) As Point
        For aIdx = 0 To _nAngles - 1
            Dim theta = 2.0 * Math.PI * aIdx / _nAngles
            Dim dep As Double = 0
            Dim sp = Project(phiMn3 * Math.Cos(theta) / _maxM,
                             phiMn2 * Math.Sin(theta) / _maxM,
                             nz, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dep)
            pts(aIdx) = New Point(CInt(sp.X), CInt(sp.Y))
        Next
        pts(_nAngles) = pts(0)
        gfx.DrawLines(New Pen(Color.FromArgb(255, 220, 60), 2), pts)
    End Sub

    Private Sub DrawAxes(gfx As Graphics, cx As Single, cy As Single, scale As Double,
                          cosAz As Double, sinAz As Double, cosEl As Double, sinEl As Double)
        Const LEN = 1.2
        Dim dep As Double = 0
        Dim orig = Project(0, 0, 0, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dep)
        Dim ptM3 = Project(LEN, 0, 0, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dep)
        gfx.DrawLine(New Pen(Color.FromArgb(200, 80, 80), 1.5F), PointFToPoint(orig), PointFToPoint(ptM3))
        gfx.DrawString("M3", New Font("Segoe UI", 7.5F), Brushes.Tomato, ptM3.X + 3, ptM3.Y - 6)
        Dim ptM2 = Project(0, LEN, 0, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dep)
        gfx.DrawLine(New Pen(Color.FromArgb(80, 200, 120), 1.5F), PointFToPoint(orig), PointFToPoint(ptM2))
        gfx.DrawString("M2", New Font("Segoe UI", 7.5F), New SolidBrush(Color.FromArgb(80, 200, 120)), ptM2.X + 3, ptM2.Y - 6)
        Dim ptP = Project(0, 0, 0.5, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dep)
        gfx.DrawLine(New Pen(Color.FromArgb(220, 200, 60), 1.5F), PointFToPoint(orig), PointFToPoint(ptP))
        gfx.DrawString("P", New Font("Segoe UI", 7.5F), Brushes.Khaki, ptP.X + 3, ptP.Y - 6)
    End Sub

    Private Function Project(nx As Double, ny As Double, nz As Double,
                              cosAz As Double, sinAz As Double,
                              cosEl As Double, sinEl As Double,
                              cx As Single, cy As Single,
                              scale As Double, ByRef depth As Double) As PointF
        Dim rx = nx * cosAz - ny * sinAz
        Dim ry = nx * sinAz + ny * cosAz
        Dim rz = nz
        Dim sx = rx
        Dim sy = rz * cosEl - ry * sinEl
        depth = rz * sinEl + ry * cosEl
        Return New PointF(CSng(cx + sx * scale), CSng(cy - sy * scale))
    End Function

    Private Function RingDepth(pIdx As Integer, depths() As Double) As Double
        Dim sum = 0.0
        For aIdx = 0 To _nAngles - 1
            sum += depths(pIdx * _nAngles + aIdx)
        Next
        Return sum / _nAngles
    End Function

    Private Function PointFToPoint(pf As PointF) As Point
        Return New Point(CInt(pf.X), CInt(pf.Y))
    End Function

End Class
