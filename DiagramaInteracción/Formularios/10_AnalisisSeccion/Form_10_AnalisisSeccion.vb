Imports System.Drawing.Drawing2D
Imports ARCO.Funciones_10_AnalisisSeccion
Imports ARCO.Funciones_00_Varias

' ════════════════════════════════════════════════════════════════════════════
'  MÓDULO 10 — ANÁLISIS DE SECCIÓN INDEPENDIENTE
'  Diagrama de interacción P-M + Diagrama Momento-Curvatura (Mander 1988)
'  Soporta: Rectangular / Cuadrada / Circular
' ════════════════════════════════════════════════════════════════════════════
Public Class Form_10_AnalisisSeccion
    Inherits Form

    ' ── Resultados calculados ──────────────────────────────────────────────
    Private _datosDI As DatosDI10
    Private _datosMK As DatosMK10
    Private _barras As List(Of RefuerzoSimple)
    Private _tipoSec As TipoSeccion10 = TipoSeccion10.Rectangular
    Private _fcc As Single, _eps_cc As Single, _eps_cu As Single

    ' ── Controles: tipo de sección ─────────────────────────────────────────
    Private rbRect As New RadioButton With {.Text = "Rectangular / Cuadrada", .AutoSize = True}
    Private rbCirc As New RadioButton With {.Text = "Circular", .AutoSize = True}

    ' ── Controles: geometría ───────────────────────────────────────────────
    Private lblB As New Label, tbB As New TextBox With {.Text = "0.40"}
    Private lblH As New Label, tbH As New TextBox With {.Text = "0.60"}
    Private lblD As New Label, tbD As New TextBox With {.Text = "0.50"}
    Private tbRec As New TextBox With {.Text = "0.040"}

    ' ── Controles: materiales ──────────────────────────────────────────────
    Private tbFc As New TextBox With {.Text = "21"}
    Private tbFy As New TextBox With {.Text = "420"}
    Private tbEs As New TextBox With {.Text = "200000"}

    ' ── Controles: refuerzo rectangular (DataGridView por caras) ──────────
    Private dgvRef As New DataGridView
    Private panRefRect As New Panel
    Private panRefCirc As New Panel

    ' ── Controles: refuerzo circular ───────────────────────────────────────
    Private cbBarraCirc As New ComboBox
    Private nudNCirc As New NumericUpDown

    ' ── Controles: confinamiento Mander ────────────────────────────────────
    Private cbBarraEst As New ComboBox
    Private tbSEst As New TextBox With {.Text = "0.10"}
    Private nudRamasB As New NumericUpDown
    Private nudRamasH As New NumericUpDown
    Private tbEpsSu As New TextBox With {.Text = "0.09"}

    ' ── Controles: P axial M-κ ─────────────────────────────────────────────
    Private tbP As New TextBox With {.Text = "0"}

    ' ── Controles: ángulo de análisis (estilo Section Designer) ────────────
    Private nudAngulo As New NumericUpDown

    ' ── Controles: botón y etiqueta Mander ────────────────────────────────
    Private btnCalc As New Button
    Private lblMander As New Label
    Private lblAdvertenciaMK As New Label

    ' ── Visualización ──────────────────────────────────────────────────────
    Private picSec As New PictureBox
    Private picDI As New PictureBox
    Private picMK As New PictureBox
    Private dgvRes As New DataGridView
    Private tabs As New TabControl

    ' ── Transformación de píxeles del dibujo de sección (para clic derecho) ──
    Private _cxSec As Single, _cySec As Single, _escSec As Single

    ' ── Editor de barra por clic derecho ───────────────────────────────────
    Private _menuBarra10 As New ContextMenuStrip()
    Private _barraContextIdx As Integer = -1

    ' ── Firma de layout: evita que CALCULAR borre ediciones manuales ──────
    Private _lastLayoutSig As String = Nothing

    ' ── Verificación puntual P, M ─────────────────────────────────────────
    Private tbDemandaP As New TextBox With {.Text = ""}
    Private tbDemandaM As New TextBox With {.Text = ""}
    Private lblCD10 As New Label
    Private _demandaP As Single = Single.NaN
    Private _demandaM As Single = Single.NaN

    ' ── Superficie DI 3D (array [pIdx * nAng + aIdx]) ─────────────────────
    Private picDI3D As New PictureBox
    Private _surf3D As Pt3D10()
    Private ReadOnly _nP3D As Integer = 40
    Private ReadOnly _nAng3D As Integer = 36
    Private _azim3D As Double = 0.6
    Private _elev3D As Double = 0.5
    Private _zoom3D As Double = 1.0
    Private _drag3D As Boolean = False
    Private _drag3DStart As Point
    Private _azim3DStart As Double, _elev3DStart As Double
    Private _maxM3D As Double = 1.0, _maxP3D As Double = 1.0, _minP3D As Double = 0.0

    Private Structure Pt3D10
        Public X As Single, Y As Single, Z As Single
        Public Sub New(xx As Single, yy As Single, zz As Single)
            X = xx : Y = yy : Z = zz
        End Sub
    End Structure

    ' ── Constantes de barra disponibles ───────────────────────────────────
    Private Shared ReadOnly _barTags As String() = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}

    ' ══════════════════════════════════════════════════════════════════════
    Public Sub New()
        Me.SuspendLayout()
        BuildUI()
        Me.ResumeLayout(False)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  CONSTRUCCIÓN PROGRAMÁTICA DE LA UI
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub BuildUI()
        Me.Text = "ARCO — Análisis de Sección"
        Me.Size = New Size(1350, 840)
        Me.MinimumSize = New Size(1000, 650)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 8.5F)
        Me.BackColor = Color.FromArgb(240, 242, 245)
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try
        AddHandler Me.Load, AddressOf OnLoad

        ' ┌─────────────────────────────────────────────────────────────┐
        ' │  PANEL IZQUIERDO — INPUTS (320px, scrollable)               │
        ' └─────────────────────────────────────────────────────────────┘
        Dim panLeft As New Panel With {
            .Width = 320, .Dock = DockStyle.Left,
            .BackColor = Color.FromArgb(240, 242, 245),
            .AutoScroll = True}

        Dim y As Integer = 8  ' cursor vertical dentro de panLeft

        ' ── SECCIÓN ────────────────────────────────────────────────────
        Dim pSec = MkSection(panLeft, "SECCIÓN", y, 156) : y += 162
        rbRect.Location = New Point(10, 28) : rbRect.Checked = True : pSec.Controls.Add(rbRect)
        rbCirc.Location = New Point(10, 52) : pSec.Controls.Add(rbCirc)

        lblB = MkLbl(pSec, "B (m):", 10, 82) : MkTb(tbB, pSec, 90, 80, 72)
        lblH = MkLbl(pSec, "H (m):", 10, 108) : MkTb(tbH, pSec, 90, 106, 72)
        lblD = MkLbl(pSec, "D (m):", 10, 108) : MkTb(tbD, pSec, 90, 106, 72)
        MkLbl(pSec, "Recub. (m):", 170, 82) : MkTb(tbRec, pSec, 250, 80, 56)
        MkLbl(pSec, "(al eje de barra)", 170, 100).ForeColor = Color.Gray

        ' ── MATERIALES ─────────────────────────────────────────────────
        Dim pMat = MkSection(panLeft, "MATERIALES", y, 102) : y += 108
        MkLbl(pMat, "f'c (MPa):", 10, 28) : MkTb(tbFc, pMat, 92, 26, 60)
        MkLbl(pMat, "fy  (MPa):", 10, 54) : MkTb(tbFy, pMat, 92, 52, 60)
        MkLbl(pMat, "Es  (MPa):", 170, 28) : MkTb(tbEs, pMat, 248, 26, 56)

        ' ── REFUERZO LONGITUDINAL ─────────────────────────────────────
        Dim pRef = MkSection(panLeft, "REFUERZO LONGITUDINAL", y, 198) : y += 204

        ' Panel rectangular: DataGridView por caras
        panRefRect.Left = 6 : panRefRect.Top = 24 : panRefRect.Width = 296 : panRefRect.Height = 168
        panRefRect.BackColor = Color.Transparent
        pRef.Controls.Add(panRefRect)
        BuildDgvRef()

        ' Panel circular: simple N + barra
        panRefCirc.Left = 6 : panRefCirc.Top = 24 : panRefCirc.Width = 296 : panRefCirc.Height = 50
        panRefCirc.BackColor = Color.Transparent : panRefCirc.Visible = False
        pRef.Controls.Add(panRefCirc)
        MkLbl(panRefCirc, "N barras:", 4, 8)
        nudNCirc.Left = 72 : nudNCirc.Top = 6 : nudNCirc.Width = 54
        nudNCirc.Minimum = 4 : nudNCirc.Maximum = 60 : nudNCirc.Value = 8
        panRefCirc.Controls.Add(nudNCirc)
        MkLbl(panRefCirc, "Barra:", 140, 8)
        cbBarraCirc.Left = 182 : cbBarraCirc.Top = 4 : cbBarraCirc.Width = 70
        cbBarraCirc.DropDownStyle = ComboBoxStyle.DropDownList
        For Each s In _barTags : cbBarraCirc.Items.Add(s) : Next
        cbBarraCirc.SelectedIndex = 3  ' #5
        panRefCirc.Controls.Add(cbBarraCirc)

        ' ── CONFINAMIENTO MANDER ───────────────────────────────────────
        Dim pConf = MkSection(panLeft, "CONFINAMIENTO — MANDER 1988", y, 148) : y += 154
        MkLbl(pConf, "Estribo:", 10, 28)
        cbBarraEst.Left = 68 : cbBarraEst.Top = 25 : cbBarraEst.Width = 60
        cbBarraEst.DropDownStyle = ComboBoxStyle.DropDownList
        For Each s In {"#2", "#3", "#4", "#5"} : cbBarraEst.Items.Add(s) : Next
        cbBarraEst.SelectedIndex = 1 : pConf.Controls.Add(cbBarraEst)
        MkLbl(pConf, "s (m):", 140, 28) : MkTb(tbSEst, pConf, 178, 26, 58)
        MkLbl(pConf, "Ramas en B:", 10, 58) : MkNud(nudRamasB, pConf, 88, 56, 2, 10, 2)
        MkLbl(pConf, "Ramas en H:", 150, 58) : MkNud(nudRamasH, pConf, 228, 56, 2, 10, 2)
        MkLbl(pConf, "ε_su (rotura acero):", 10, 88) : MkTb(tbEpsSu, pConf, 140, 86, 56)
        MkLbl(pConf, "(solo aplica a columnas con estribos cerrados)", 10, 112).ForeColor = Color.Gray

        ' ── P AXIAL PARA M-κ ──────────────────────────────────────────
        Dim pPax = MkSection(panLeft, "CARGA AXIAL — DIAGRAMA M-κ", y, 58) : y += 64
        MkLbl(pPax, "P axial (kN):", 10, 26)
        MkTb(tbP, pPax, 104, 24, 80)
        MkLbl(pPax, "(compresión +)", 192, 28).ForeColor = Color.Gray

        ' ── ÁNGULO DE ANÁLISIS (estilo Section Designer) ────────────────
        Dim pAng = MkSection(panLeft, "ÁNGULO DE ANÁLISIS", y, 58) : y += 64
        MkLbl(pAng, "θ (grados):", 10, 26)
        MkNud(nudAngulo, pAng, 104, 24, 0, 360, 0)
        MkLbl(pAng, "(0°=eje fuerte H, 90°=eje débil B)", 170, 28).ForeColor = Color.Gray

        ' ── VERIFICACIÓN PUNTUAL P, M ────────────────────────────────
        Dim pVer = MkSection(panLeft, "VERIFICACIÓN  P, M  EN  DIAGRAMA", y, 100) : y += 106
        MkLbl(pVer, "P demanda (kN):", 10, 28)
        MkTb(tbDemandaP, pVer, 120, 26, 90)
        MkLbl(pVer, "comp. +", 218, 30).ForeColor = Color.Gray
        MkLbl(pVer, "M demanda (kN·m):", 10, 56)
        MkTb(tbDemandaM, pVer, 140, 54, 70)
        Dim btnVer As New Button With {
            .Left = 8, .Top = 78, .Width = 130, .Height = 24,
            .Text = "▶ Marcar en DI",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .BackColor = Color.FromArgb(0, 82, 164), .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
        btnVer.FlatAppearance.BorderSize = 0
        pVer.Controls.Add(btnVer)
        AddHandler btnVer.Click, AddressOf OnVerificarClick
        lblCD10.Left = 148 : lblCD10.Top = 78 : lblCD10.Width = 148 : lblCD10.Height = 24
        lblCD10.AutoSize = False : lblCD10.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        lblCD10.TextAlign = ContentAlignment.MiddleLeft
        pVer.Controls.Add(lblCD10)

        ' ── BOTÓN CALCULAR ─────────────────────────────────────────────
        btnCalc.Left = 8 : btnCalc.Top = y : btnCalc.Width = 296 : btnCalc.Height = 38
        btnCalc.Text = "▶  CALCULAR" : btnCalc.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        btnCalc.BackColor = Color.FromArgb(0, 82, 164) : btnCalc.ForeColor = Color.White
        btnCalc.FlatStyle = FlatStyle.Flat : btnCalc.FlatAppearance.BorderSize = 0
        btnCalc.Cursor = Cursors.Hand
        panLeft.Controls.Add(btnCalc) : y += 44

        ' ── ETIQUETA MANDER RESULTADO ─────────────────────────────────
        lblMander.Left = 8 : lblMander.Top = y : lblMander.Width = 296 : lblMander.Height = 46
        lblMander.AutoSize = False : lblMander.Font = New Font("Segoe UI", 7.5F)
        lblMander.ForeColor = Color.FromArgb(0, 100, 50)
        lblMander.BackColor = Color.FromArgb(235, 248, 238)
        lblMander.BorderStyle = BorderStyle.FixedSingle
        lblMander.Padding = New Padding(4)
        panLeft.Controls.Add(lblMander)
        y += 50

        ' ── ADVERTENCIA: M-κ no convergió por aplastamiento real ───────
        lblAdvertenciaMK.Left = 8 : lblAdvertenciaMK.Top = y : lblAdvertenciaMK.Width = 296 : lblAdvertenciaMK.Height = 40
        lblAdvertenciaMK.AutoSize = False : lblAdvertenciaMK.Font = New Font("Segoe UI", 7.5F, FontStyle.Bold)
        lblAdvertenciaMK.ForeColor = Color.FromArgb(140, 30, 0)
        lblAdvertenciaMK.BackColor = Color.FromArgb(255, 240, 230)
        lblAdvertenciaMK.BorderStyle = BorderStyle.FixedSingle
        lblAdvertenciaMK.Padding = New Padding(4)
        lblAdvertenciaMK.Text = "⚠ El M-κ no alcanzó el aplastamiento real dentro del barrido (κu/μφ son un límite de barrido)."
        lblAdvertenciaMK.Visible = False
        panLeft.Controls.Add(lblAdvertenciaMK)

        ' ┌─────────────────────────────────────────────────────────────┐
        ' │  PANEL CENTRAL — SECCIÓN TRANSVERSAL (280px)                │
        ' └─────────────────────────────────────────────────────────────┘
        Dim panCenter As New Panel With {
            .Width = 280, .Dock = DockStyle.Left,
            .BackColor = Color.White,
            .Padding = New Padding(0)}

        Dim hdrSec As New Label With {
            .Text = "  SECCIÓN TRANSVERSAL", .Dock = DockStyle.Top, .Height = 28,
            .BackColor = Color.FromArgb(30, 60, 120), .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft}
        panCenter.Controls.Add(hdrSec)

        picSec.Dock = DockStyle.Fill : picSec.BackColor = Color.White
        AddHandler picSec.Paint, AddressOf OnPaintSec
        panCenter.Controls.Add(picSec)

        ' ┌─────────────────────────────────────────────────────────────┐
        ' │  PANEL DERECHO — TABS DE RESULTADOS (fill)                  │
        ' └─────────────────────────────────────────────────────────────┘
        Dim panRight As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.White}

        tabs.Dock = DockStyle.Fill
        tabs.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        tabs.SizeMode = TabSizeMode.Normal
        tabs.ItemSize = New Size(0, 28)

        ' Tab 1: Diagrama de Interacción
        Dim tDI As New TabPage("  Diagrama P-M  ")
        picDI.Dock = DockStyle.Fill : picDI.BackColor = Color.White
        AddHandler picDI.Paint, AddressOf OnPaintDI
        AddHandler picDI.Resize, Sub(s, ev) picDI.Refresh()
        tDI.Controls.Add(picDI) : tabs.TabPages.Add(tDI)

        ' Tab 2: Momento-Curvatura
        Dim tMK As New TabPage("  Momento-Curvatura  ")
        picMK.Dock = DockStyle.Fill : picMK.BackColor = Color.White
        AddHandler picMK.Paint, AddressOf OnPaintMK
        AddHandler picMK.Resize, Sub(s, ev) picMK.Refresh()
        tMK.Controls.Add(picMK) : tabs.TabPages.Add(tMK)

        ' Tab 3: Superficie DI 3D
        Dim t3D As New TabPage("  DI Superficie 3D  ")
        t3D.BackColor = Color.FromArgb(16, 20, 34)
        picDI3D.Dock = DockStyle.Fill : picDI3D.BackColor = Color.FromArgb(16, 20, 34)
        picDI3D.Cursor = Cursors.SizeAll
        AddHandler picDI3D.Paint, AddressOf OnPaintDI3D
        AddHandler picDI3D.Resize, Sub(s, ev) picDI3D.Refresh()
        AddHandler picDI3D.MouseDown, AddressOf OnDI3D_MouseDown
        AddHandler picDI3D.MouseMove, AddressOf OnDI3D_MouseMove
        AddHandler picDI3D.MouseUp, AddressOf OnDI3D_MouseUp
        AddHandler picDI3D.MouseWheel, AddressOf OnDI3D_MouseWheel
        t3D.Controls.Add(picDI3D) : tabs.TabPages.Add(t3D)

        ' Tab 4: Resultados numéricos
        Dim tRes As New TabPage("  Resultados  ")
        dgvRes.Dock = DockStyle.Fill : dgvRes.ReadOnly = True : dgvRes.AllowUserToAddRows = False
        dgvRes.RowHeadersVisible = False : dgvRes.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvRes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvRes.GridColor = Color.FromArgb(200, 210, 230)
        dgvRes.BorderStyle = BorderStyle.None
        dgvRes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120)
        dgvRes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgvRes.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        dgvRes.EnableHeadersVisualStyles = False
        dgvRes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255)
        tRes.Controls.Add(dgvRes) : tabs.TabPages.Add(tRes)

        panRight.Controls.Add(tabs)

        ' ── Orden de adición a Me.Controls (determina z-order para DockStyle) ──
        ' WinForms: el último en agregarse queda en el frente (índice 0) y se
        ' procesa primero en el layout. Para Left+Left+Fill correctos:
        '   Fill debe quedar al fondo (agregado primero → empujado por los siguientes)
        '   Left-izq debe quedar al frente (agregado último → procesa primero)
        Me.Controls.Add(panRight)   ' Fill — va al fondo cuando se agregan los Left
        Me.Controls.Add(panCenter)  ' Left 280px — procesado segundo
        Me.Controls.Add(panLeft)    ' Left 320px — al frente, procesa primero ✓

        ' ── Eventos de cambio de tipo ──────────────────────────────────
        AddHandler rbRect.CheckedChanged, AddressOf OnTipoChanged
        AddHandler rbCirc.CheckedChanged, AddressOf OnTipoChanged
        AddHandler btnCalc.Click, AddressOf OnCalcClick

        ' Redibuja sección al cambiar geometría (preview live)
        For Each tb As TextBox In {tbB, tbH, tbD, tbRec}
            AddHandler tb.TextChanged, Sub(s, ev) picSec.Refresh()
        Next

        ' El eje neutro se ve en vivo al mover el ángulo, sin esperar a CALCULAR
        AddHandler nudAngulo.ValueChanged, Sub(s, ev) picSec.Refresh()

        ConfigurarMenuBarra10()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  EDITOR DE BARRA POR CLIC DERECHO
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub ConfigurarMenuBarra10()
        _menuBarra10.Font = New Font("Segoe UI", 9)
        For Each tag As String In _barTags
            Dim item As New ToolStripMenuItem(tag)
            AddHandler item.Click, AddressOf MenuBarra10_Click
            _menuBarra10.Items.Add(item)
        Next
        AddHandler picSec.MouseDown, AddressOf PicSec_MouseDown
    End Sub

    Private Function HitTestBarra10(pt As Point) As Integer
        If _barras Is Nothing OrElse _escSec <= 0 Then Return -1
        Const HIT_R As Single = 9.0F
        For i = 0 To _barras.Count - 1
            Dim bar = _barras(i)
            Dim sx = _cxSec + bar.Coordenada_X * _escSec
            Dim sy = _cySec - bar.Coordenada_Y * _escSec
            Dim dx = pt.X - sx, dy = pt.Y - sy
            If dx * dx + dy * dy <= HIT_R * HIT_R Then Return i
        Next
        Return -1
    End Function

    Private Sub PicSec_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Right Then Return
        Dim idx = HitTestBarra10(e.Location)
        If idx < 0 Then Return
        _barraContextIdx = idx
        _menuBarra10.Show(picSec, picSec.PointToClient(Cursor.Position))
    End Sub

    Private Sub MenuBarra10_Click(sender As Object, e As EventArgs)
        Try
            If _barraContextIdx < 0 OrElse _barras Is Nothing OrElse _barraContextIdx >= _barras.Count Then Return
            Dim tag = CStr(DirectCast(sender, ToolStripMenuItem).Text)
            Dim bar = _barras(_barraContextIdx)
            bar.Name_Barra = tag
            bar.Db = DiametroRefuerzo(tag)
            bar.Asb = AreaRefuerzo(tag)
            _barraContextIdx = -1
            Calcular()
        Catch ex As Exception
            Logger.Error(ex, "Form_10_AnalisisSeccion.MenuBarra10_Click", "Error al reasignar barra")
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  FIRMA DE LAYOUT: para que CALCULAR no borre ediciones manuales
    ' ══════════════════════════════════════════════════════════════════════
    Private Function ComputeLayoutSig10() As String
        Dim sb As New System.Text.StringBuilder
        sb.Append(_tipoSec.ToString()).Append("|")
        If _tipoSec = TipoSeccion10.Rectangular Then
            sb.Append(tbB.Text).Append("|").Append(tbH.Text).Append("|")
            Dim esp = GetEspec()
            sb.Append(esp.BarraEsquinas).Append("|")
            sb.Append(esp.BarraSuperior).Append("|").Append(esp.NSuperior).Append("|")
            sb.Append(esp.BarraInferior).Append("|").Append(esp.NInferior).Append("|")
            sb.Append(esp.BarraLateral).Append("|").Append(esp.NLateralPorCara)
        Else
            sb.Append(tbD.Text).Append("|").Append(nudNCirc.Value).Append("|")
            sb.Append(If(cbBarraCirc.SelectedItem, "").ToString())
        End If
        sb.Append("|").Append(tbRec.Text)
        Return sb.ToString()
    End Function

    ' ══════════════════════════════════════════════════════════════════════
    '  DataGridView de refuerzo por caras
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub BuildDgvRef()
        dgvRef.Dock = DockStyle.Fill
        dgvRef.AllowUserToAddRows = False : dgvRef.AllowUserToDeleteRows = False
        dgvRef.RowHeadersVisible = False : dgvRef.MultiSelect = False
        dgvRef.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvRef.GridColor = Color.FromArgb(200, 210, 230)
        dgvRef.BorderStyle = BorderStyle.FixedSingle
        dgvRef.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120)
        dgvRef.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgvRef.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8F, FontStyle.Bold)
        dgvRef.EnableHeadersVisualStyles = False
        dgvRef.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255)
        dgvRef.DefaultCellStyle.SelectionForeColor = Color.Black

        ' Columna 0: Zona (read-only)
        Dim colZona As New DataGridViewTextBoxColumn With {
            .Name = "Zona", .HeaderText = "Zona de refuerzo",
            .ReadOnly = True, .FillWeight = 45}
        colZona.DefaultCellStyle.BackColor = Color.FromArgb(240, 242, 248)
        colZona.DefaultCellStyle.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        dgvRef.Columns.Add(colZona)

        ' Columna 1: N barras (editable, numérico)
        Dim colN As New DataGridViewTextBoxColumn With {
            .Name = "N", .HeaderText = "N barras",
            .FillWeight = 22}
        colN.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgvRef.Columns.Add(colN)

        ' Columna 2: Barra (combo)
        Dim colBarra As New DataGridViewComboBoxColumn With {
            .Name = "Barra", .HeaderText = "Barra",
            .FillWeight = 33, .DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox}
        For Each s In _barTags : colBarra.Items.Add(s) : Next
        dgvRef.Columns.Add(colBarra)

        ' Filas: 4 zonas
        dgvRef.Rows.Add("Esquinas (×4 fijas)", "4", "#5")
        dgvRef.Rows.Add("Cara Superior", "2", "#5")
        dgvRef.Rows.Add("Cara Inferior", "2", "#5")
        dgvRef.Rows.Add("Caras Laterales (×2)", "1", "#5")

        ' Esquinas: N no editable
        dgvRef.Rows(0).Cells(1).ReadOnly = True
        dgvRef.Rows(0).Cells(1).Style.BackColor = Color.FromArgb(235, 235, 235)
        dgvRef.Rows(0).Cells(1).Style.ForeColor = Color.Gray

        ' Altura de filas
        dgvRef.RowTemplate.Height = 30
        For Each row As DataGridViewRow In dgvRef.Rows
            row.Height = 30
        Next

        panRefRect.Controls.Add(dgvRef)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  HELPERS DE UI
    ' ══════════════════════════════════════════════════════════════════════

    ' Panel con header coloreado — reemplaza GroupBox (mejor visibilidad en Win11)
    Private Function MkSection(parent As Panel, titulo As String,
                                 top As Integer, height As Integer) As Panel
        Dim hdr As New Label With {
            .Text = "  " & titulo, .Height = 22, .Dock = DockStyle.Top,
            .BackColor = Color.FromArgb(30, 60, 120), .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft}
        Dim pan As New Panel With {
            .Left = 8, .Top = top, .Width = 302, .Height = height,
            .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle}
        pan.Controls.Add(hdr)
        parent.Controls.Add(pan)
        Return pan
    End Function

    Private Function MkLbl(parent As Control, text As String,
                             x As Integer, y As Integer) As Label
        Dim lbl As New Label With {
            .Text = text, .Left = x, .Top = y + 2, .AutoSize = True,
            .ForeColor = Color.FromArgb(50, 50, 60)}
        parent.Controls.Add(lbl)
        Return lbl
    End Function

    Private Sub MkTb(tb As TextBox, parent As Control,
                      x As Integer, y As Integer, w As Integer)
        tb.Left = x : tb.Top = y : tb.Width = w
        tb.BorderStyle = BorderStyle.FixedSingle
        parent.Controls.Add(tb)
    End Sub

    Private Sub MkNud(nud As NumericUpDown, parent As Control,
                       x As Integer, y As Integer,
                       minV As Integer, maxV As Integer, defV As Integer)
        nud.Left = x : nud.Top = y : nud.Width = 52
        nud.Minimum = minV : nud.Maximum = maxV : nud.Value = defV
        parent.Controls.Add(nud)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  EVENTOS
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnLoad(sender As Object, e As EventArgs)
        OnTipoChanged(Nothing, EventArgs.Empty)
        Calcular()
    End Sub

    Private Sub OnTipoChanged(sender As Object, e As EventArgs)
        _tipoSec = If(rbRect.Checked, TipoSeccion10.Rectangular, TipoSeccion10.Circular)
        Dim esRect = (_tipoSec = TipoSeccion10.Rectangular)
        lblB.Visible = esRect : tbB.Visible = esRect
        lblH.Visible = esRect : tbH.Visible = esRect
        lblD.Visible = Not esRect : tbD.Visible = Not esRect
        panRefRect.Visible = esRect : panRefCirc.Visible = Not esRect
        nudRamasH.Enabled = esRect
        picSec.Refresh()
    End Sub

    Private Sub OnCalcClick(sender As Object, e As EventArgs)
        Calcular()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  LEER ESPECIFICACIÓN DE REFUERZO DESDE dgvRef
    ' ══════════════════════════════════════════════════════════════════════
    Private Function GetEspec() As EspecRefuerzo
        Dim esp As New EspecRefuerzo
        Try
            esp.BarraEsquinas = CStr(dgvRef.Rows(0).Cells(2).Value)
            esp.BarraSuperior = CStr(dgvRef.Rows(1).Cells(2).Value)
            esp.NSuperior = Math.Max(0, CInt(dgvRef.Rows(1).Cells(1).Value))
            esp.BarraInferior = CStr(dgvRef.Rows(2).Cells(2).Value)
            esp.NInferior = Math.Max(0, CInt(dgvRef.Rows(2).Cells(1).Value))
            esp.BarraLateral = CStr(dgvRef.Rows(3).Cells(2).Value)
            esp.NLateralPorCara = Math.Max(0, CInt(dgvRef.Rows(3).Cells(1).Value))
        Catch
            ' Valores por defecto si hay error de lectura
        End Try
        Return esp
    End Function

    ' ══════════════════════════════════════════════════════════════════════
    '  CALCULAR
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub Calcular()
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim fc As Single, fy As Single, Es As Single, P_ax As Single, rec As Single

            If Not Single.TryParse(tbFc.Text, fc) OrElse fc <= 0 Then
                MessageBox.Show("f'c inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
            End If
            If Not Single.TryParse(tbFy.Text, fy) OrElse fy <= 0 Then
                MessageBox.Show("fy inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
            End If
            If Not Single.TryParse(tbEs.Text, Es) OrElse Es <= 0 Then
                Es = 200000
            End If
            Single.TryParse(tbP.Text, P_ax)
            If Not Single.TryParse(tbRec.Text, rec) OrElse rec < 0 Then rec = 0.04F

            Dim s_mm As Single = 100
            Dim s_est As Single
            If Single.TryParse(tbSEst.Text, s_est) AndAlso s_est > 0 Then s_mm = s_est * 1000.0F
            Dim db_hoop = DiametroRefuerzo(CStr(cbBarraEst.SelectedItem))
            Dim thetaRad As Single = CSng(nudAngulo.Value) * CSng(Math.PI) / 180.0F
            Dim eps_su As Single = 0.09F
            If Not Single.TryParse(tbEpsSu.Text, eps_su) OrElse eps_su <= 0 Then eps_su = 0.09F

            Dim sigActual = ComputeLayoutSig10()
            Dim regenerar = (_barras Is Nothing) OrElse (sigActual <> _lastLayoutSig)

            If _tipoSec = TipoSeccion10.Rectangular Then
                Dim secB As Single, secH As Single
                If Not Single.TryParse(tbB.Text, secB) OrElse secB <= 0 Then
                    MessageBox.Show("B inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If
                If Not Single.TryParse(tbH.Text, secH) OrElse secH <= 0 Then
                    MessageBox.Show("H inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If

                If regenerar Then
                    Dim esp = GetEspec()
                    _barras = DistribuirBarrasPorLados(secB, secH, esp, rec)
                End If

                If _barras Is Nothing OrElse _barras.Count = 0 Then
                    MessageBox.Show("No hay barras definidas. Verifique el refuerzo.", "Sin refuerzo",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If

                Dim b_core = Math.Max(0.01F, secB - 2 * rec)
                Dim h_core = Math.Max(0.01F, secH - 2 * rec)
                CalcularMander10(fc, fy, db_hoop, s_mm, b_core, h_core,
                                  CInt(nudRamasB.Value), CInt(nudRamasH.Value),
                                  _fcc, _eps_cc, _eps_cu)

                _datosDI = DI_Rectangular10(secB, secH, _barras, fc, fy, Es, rec, thetaRad)
                _datosMK = MomentoCurvatura10(secB, secH, TipoSeccion10.Rectangular, rec,
                                               _barras, fc, fy, Es, _fcc, _eps_cc, _eps_cu, P_ax, thetaRad, eps_su)
            Else
                Dim secD As Single
                If Not Single.TryParse(tbD.Text, secD) OrElse secD <= 0 Then
                    MessageBox.Show("D inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If

                If regenerar Then
                    _barras = DistribuirBarrasCirculo10(secD, CInt(nudNCirc.Value),
                                                        CStr(cbBarraCirc.SelectedItem), rec)
                End If

                If _barras Is Nothing OrElse _barras.Count = 0 Then
                    MessageBox.Show("No hay barras definidas. Verifique el refuerzo.", "Sin refuerzo",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If

                Dim R_core = Math.Max(0.02F, secD / 2 - rec)
                ' Para circular: ramas equivalentes = (n_ramas_b + n_ramas_h) en cada dirección
                Dim nRamas = CInt(nudRamasB.Value) + CInt(nudRamasH.Value)
                CalcularMander10(fc, fy, db_hoop, s_mm, 2 * R_core, 2 * R_core,
                                  nRamas, nRamas, _fcc, _eps_cc, _eps_cu)

                _datosDI = DI_Circular10(secD, _barras, fc, fy, Es, thetaRad)
                _datosMK = MomentoCurvatura10(secD, secD, TipoSeccion10.Circular, rec,
                                               _barras, fc, fy, Es, _fcc, _eps_cc, _eps_cu, P_ax, thetaRad, eps_su)
            End If

            _lastLayoutSig = sigActual

            ' Superficie DI 3D (en background para no bloquear UI)
            Try
                If _tipoSec = TipoSeccion10.Rectangular Then
                    Dim secB2 As Single, secH2 As Single
                    Single.TryParse(tbB.Text, secB2) : Single.TryParse(tbH.Text, secH2)
                    GenerarSuperficie3D(fc, fy, Es, rec, secB2, secH2, 0)
                Else
                    Dim secD2 As Single
                    Single.TryParse(tbD.Text, secD2)
                    GenerarSuperficie3D(fc, fy, Es, rec, secD2, secD2, secD2)
                End If
            Catch ex As Exception
                Logger.Warning("Form_10_AnalisisSeccion", "Superficie 3D no generada: " & ex.Message)
            End Try

            ' Etiqueta Mander
            lblMander.Text = String.Format(
                "Mander 1988:  f'cc = {0:F2} MPa  (f'cc/f'c = {1:F2})" & vbCrLf &
                "ε_cc = {2:F5}     ε_cu* = {3:F5}",
                _fcc, If(_fcc > 0 AndAlso fc > 0, _fcc / fc, 1),
                _eps_cc, _eps_cu)

            lblAdvertenciaMK.Visible = Not _datosMK.ConvergioPorAplastamiento

            picSec.Refresh() : picDI.Refresh() : picMK.Refresh() : picDI3D.Refresh()
            RellenarResultados()

        Catch ex As Exception
            Logger.Error(ex, "Form_10_AnalisisSeccion.Calcular", "Error en análisis de sección")
            MessageBox.Show("Error en cálculo: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  DIBUJO DE LA SECCIÓN TRANSVERSAL
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnPaintSec(sender As Object, e As PaintEventArgs)
        DibujarSeccion(e.Graphics, picSec.Width, picSec.Height)
    End Sub

    Private Sub DibujarSeccion(g As Graphics, wPx As Integer, hPx As Integer)
        g.Clear(Color.White)
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim rec As Single = 0.04F
        Single.TryParse(tbRec.Text, rec)
        Dim mg = 32
        Dim rPx As Single = 0   ' se asigna en cada rama

        If _tipoSec = TipoSeccion10.Rectangular Then
            Dim secB As Single = 0.4F, secH As Single = 0.6F
            Single.TryParse(tbB.Text, secB) : Single.TryParse(tbH.Text, secH)
            If secB <= 0 OrElse secH <= 0 Then Return

            _escSec = Math.Min((wPx - 2 * mg) / secB, (hPx - 2 * mg - 40) / secH)
            _cxSec = wPx / 2.0F : _cySec = (hPx - 30) / 2.0F
            Dim secBpx = secB * _escSec, secHpx = secH * _escSec
            rPx = rec * _escSec

            ' Sección bruta (concreto de recubrimiento)
            g.FillRectangle(New SolidBrush(Color.FromArgb(195, 195, 195)),
                            _cxSec - secBpx / 2, _cySec - secHpx / 2, secBpx, secHpx)
            ' Núcleo confinado
            g.FillRectangle(New SolidBrush(Color.FromArgb(220, 228, 240)),
                            _cxSec - secBpx / 2 + rPx, _cySec - secHpx / 2 + rPx,
                            secBpx - 2 * rPx, secHpx - 2 * rPx)
            g.DrawRectangle(New Pen(Color.FromArgb(70, 70, 70), 2),
                            _cxSec - secBpx / 2, _cySec - secHpx / 2, secBpx, secHpx)
            g.DrawRectangle(New Pen(Color.FromArgb(100, 130, 180), 1) With {.DashStyle = DashStyle.Dash},
                            _cxSec - secBpx / 2 + rPx, _cySec - secHpx / 2 + rPx,
                            secBpx - 2 * rPx, secHpx - 2 * rPx)

            DibujarBarrasGDI(g, _cxSec, _cySec, _escSec)
            DibujarCotas(g, _cxSec, _cySec, secB, secH, _escSec, wPx, hPx)
            DibujarEjeNeutro10(g, _cxSec, _cySec, _escSec,
                                CSng(nudAngulo.Value) * CSng(Math.PI) / 180.0F,
                                CSng(Math.Sqrt((secB / 2.0F) ^ 2 + (secH / 2.0F) ^ 2)))

        Else  ' Circular
            Dim secD As Single = 0.5F
            Single.TryParse(tbD.Text, secD)
            If secD <= 0 Then Return
            Dim R = secD / 2.0F
            _escSec = Math.Min((wPx - 2 * mg) / secD, (hPx - 2 * mg - 40) / secD)
            _cxSec = wPx / 2.0F : _cySec = (hPx - 30) / 2.0F
            Dim Rcirc = R * _escSec
            rPx = rec * _escSec

            ' Sección bruta
            g.FillEllipse(New SolidBrush(Color.FromArgb(195, 195, 195)),
                          _cxSec - Rcirc, _cySec - Rcirc, 2 * Rcirc, 2 * Rcirc)
            ' Núcleo confinado
            g.FillEllipse(New SolidBrush(Color.FromArgb(220, 228, 240)),
                          _cxSec - Rcirc + rPx, _cySec - Rcirc + rPx,
                          2 * (Rcirc - rPx), 2 * (Rcirc - rPx))
            g.DrawEllipse(New Pen(Color.FromArgb(70, 70, 70), 2),
                          _cxSec - Rcirc, _cySec - Rcirc, 2 * Rcirc, 2 * Rcirc)
            g.DrawEllipse(New Pen(Color.FromArgb(100, 130, 180), 1) With {.DashStyle = DashStyle.Dash},
                          _cxSec - Rcirc + rPx, _cySec - Rcirc + rPx,
                          2 * (Rcirc - rPx), 2 * (Rcirc - rPx))

            DibujarBarrasGDI(g, _cxSec, _cySec, _escSec)
            DibujarEjeNeutro10(g, _cxSec, _cySec, _escSec,
                                CSng(nudAngulo.Value) * CSng(Math.PI) / 180.0F, R)

            ' Etiqueta D
            Dim fntD = New Font("Segoe UI", 7.5F)
            g.DrawString(String.Format("D = {0:F3} m", secD), fntD, Brushes.Black, 4, 4)
        End If

        ' Leyenda de barras si hay datos
        If _barras IsNot Nothing AndAlso _barras.Count > 0 AndAlso _datosDI IsNot Nothing Then
            Dim fntL = New Font("Segoe UI", 7.5F)
            g.DrawString(String.Format("Ast = {0:F2} cm²     ρ = {1:F3}%",
                         _datosDI.Ast_cm2, _datosDI.rho_pct),
                         fntL, Brushes.Black, 4, hPx - 28)
            g.DrawString(String.Format("{0} barras   Ag = {1:F1} cm²",
                         _barras.Count, _datosDI.Ag_cm2),
                         fntL, Brushes.Black, 4, hPx - 14)
        End If
    End Sub

    ' Dibuja el eje neutro asumido (línea punteada por el centroide, dirección
    ' (cosθ,-senθ)) y una flecha hacia el lado de compresión asumido
    ' (dirección (senθ,cosθ)), igual a como Section Designer indica el ángulo
    ' de análisis. thetaRad=0 → línea horizontal, flecha hacia arriba (+Y).
    Private Sub DibujarEjeNeutro10(g As Graphics, cx As Single, cy As Single, esc As Single,
                                     thetaRad As Single, radioM As Single)
        If radioM <= 0 OrElse esc <= 0 Then Return
        Dim sinT = CSng(Math.Sin(thetaRad)) : Dim cosT = CSng(Math.Cos(thetaRad))
        Dim largo = radioM * esc * 1.15F

        ' Eje neutro: dirección (cosθ, -senθ) en píxeles (Y de pantalla invertida)
        Dim x1 = cx - cosT * largo, y1 = cy - sinT * largo
        Dim x2 = cx + cosT * largo, y2 = cy + sinT * largo
        Dim penEje As New Pen(Color.FromArgb(200, 30, 30), 1.5F) With {.DashStyle = DashStyle.DashDot}
        g.DrawLine(penEje, x1, y1, x2, y2)

        ' Flecha hacia el lado de compresión asumido: dirección (senθ, cosθ)
        Dim flechaLen = radioM * esc * 0.35F
        Dim fx = cx + sinT * flechaLen, fy2 = cy - cosT * flechaLen
        Dim penFlecha As New Pen(Color.FromArgb(200, 30, 30), 2.0F)
        penFlecha.CustomEndCap = New AdjustableArrowCap(4, 5)
        g.DrawLine(penFlecha, cx, cy, fx, fy2)

        Dim fnt = New Font("Segoe UI", 7.5F, FontStyle.Bold)
        Dim thetaDeg = CSng(thetaRad * 180.0 / Math.PI)
        g.DrawString(String.Format("θ = {0:F0}°", thetaDeg), fnt,
                     New SolidBrush(Color.FromArgb(200, 30, 30)), fx + 4, fy2 - 12)
    End Sub

    Private Sub DibujarBarrasGDI(g As Graphics, cx As Single, cy As Single, esc As Single)
        If _barras Is Nothing Then Return
        For Each bar As RefuerzoSimple In _barras
            ' bar.Db ya está en metros (DiametroRefuerzo), no en mm
            Dim db_px = Math.Max(5.0F, bar.Db * esc)
            Dim bx = cx + bar.Coordenada_X * esc - db_px / 2
            Dim by = cy - bar.Coordenada_Y * esc - db_px / 2
            g.FillEllipse(New SolidBrush(Color.FromArgb(50, 50, 50)), bx, by, db_px, db_px)
            g.DrawEllipse(New Pen(Color.Black, 0.8F), bx, by, db_px, db_px)
        Next
    End Sub

    Private Sub DibujarCotas(g As Graphics, cx As Single, cy As Single,
                               secB As Single, secH As Single, esc As Single,
                               wPx As Integer, hPx As Integer)
        Dim fnt = New Font("Segoe UI", 7.5F)
        Dim cotaBpx = secB * esc, cotaHpx = secH * esc
        ' Cota B (inferior)
        Dim yCota = cy + cotaHpx / 2 + 8
        g.DrawLine(Pens.DimGray, cx - cotaBpx / 2, yCota, cx + cotaBpx / 2, yCota)
        g.DrawString(String.Format("B={0:F3}m", secB), fnt, Brushes.DimGray,
                     cx - 24, yCota + 2)
        ' Cota H (lateral)
        Dim xCota = cx + cotaBpx / 2 + 6
        g.DrawLine(Pens.DimGray, xCota, cy - cotaHpx / 2, xCota, cy + cotaHpx / 2)
        Dim sfCota As New StringFormat With {.Alignment = StringAlignment.Center}
        Dim state = g.Save()
        g.TranslateTransform(xCota + 10, cy)
        g.RotateTransform(90)
        g.DrawString(String.Format("H={0:F3}m", secH), fnt, Brushes.DimGray, 0, 0, sfCota)
        g.Restore(state)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  DIAGRAMA DE INTERACCIÓN P-M
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnPaintDI(sender As Object, e As PaintEventArgs)
        DibujarDI(e.Graphics, picDI.Width, picDI.Height)
    End Sub

    Private Sub DibujarDI(g As Graphics, wPx As Integer, hPx As Integer)
        g.Clear(Color.White)
        g.SmoothingMode = SmoothingMode.AntiAlias
        If _datosDI Is Nothing OrElse _datosDI.Mn.Count < 2 Then
            MensajeSinDatos(g, wPx, hPx, "Presione CALCULAR para generar el diagrama de interacción")
            Return
        End If

        Dim mL = 105, mR = 24, mT = 36, mB = 62
        Dim pw = wPx - mL - mR, ph = hPx - mT - mB
        If pw < 50 OrElse ph < 50 Then Return

        Dim maxM = Math.Max(_datosDI.Mn.Max(), _datosDI.PhiMn.Max()) * 1.12F
        If maxM < 1 Then maxM = 1
        Dim maxP = _datosDI.P0 * 1.12F
        Dim minP = Math.Min(_datosDI.Pmin, _datosDI.PhiPmin) * 1.12F
        Dim pRng = maxP - minP : If pRng < 1 Then pRng = 1

        Dim toX = Function(m As Single) CSng(mL + m / maxM * pw)
        Dim toY = Function(p As Single) CSng(mT + (1 - (p - minP) / pRng) * ph)

        ' Grid
        Dim gPen = New Pen(Color.FromArgb(220, 225, 238))
        For ix = 0 To 4
            Dim xx = CSng(mL + ix / 4.0 * pw)
            g.DrawLine(gPen, xx, mT, xx, mT + ph)
        Next
        For iy = 0 To 5
            Dim yy = CSng(mT + iy / 5.0 * ph)
            g.DrawLine(gPen, mL, yy, mL + pw, yy)
        Next

        ' Zona de diseño válida (P < φP0) — relleno suave
        If _datosDI.PhiMn.Count > 2 Then
            Dim polyPts As New List(Of PointF)
            For i = 0 To _datosDI.PhiMn.Count - 1
                polyPts.Add(New PointF(toX(_datosDI.PhiMn(i)), toY(_datosDI.PhiPn(i))))
            Next
            polyPts.Add(New PointF(mL, toY(_datosDI.PhiPn.Last())))
            polyPts.Add(New PointF(mL, toY(_datosDI.PhiPn(0))))
            g.FillPolygon(New SolidBrush(Color.FromArgb(20, 0, 100, 200)), polyPts.ToArray())
        End If

        ' Curva nominal Mn-Pn (gris punteado)
        Dim ptsPn(_datosDI.Mn.Count - 1) As PointF
        For i = 0 To _datosDI.Mn.Count - 1
            ptsPn(i) = New PointF(toX(_datosDI.Mn(i)), toY(_datosDI.Pn(i)))
        Next
        g.DrawLines(New Pen(Color.Silver, 1.5F) With {.DashStyle = DashStyle.Dash}, ptsPn)

        ' Curva reducida φ (azul sólido)
        Dim ptsPhi(_datosDI.PhiMn.Count - 1) As PointF
        For i = 0 To _datosDI.PhiMn.Count - 1
            ptsPhi(i) = New PointF(toX(_datosDI.PhiMn(i)), toY(_datosDI.PhiPn(i)))
        Next
        g.DrawLines(New Pen(Color.FromArgb(0, 82, 164), 2.5F), ptsPhi)

        ' Punto de balance
        If _datosDI.M_bal > 0 Then
            Dim bx = toX(_datosDI.M_bal), by = toY(_datosDI.P_bal)
            g.FillEllipse(Brushes.OrangeRed, bx - 6, by - 6, 12, 12)
            g.DrawEllipse(Pens.DarkRed, bx - 6, by - 6, 12, 12)
            g.DrawString("Bal.", New Font("Segoe UI", 8.5F), Brushes.OrangeRed, bx + 8, by - 10)
        End If

        ' ── Punto de demanda P, M ────────────────────────────────────
        If Not Single.IsNaN(_demandaP) AndAlso Not Single.IsNaN(_demandaM) Then
            Dim phiMnAtP = InterpolatePhiMnAtP(_demandaP)
            Dim cd As Single = 0
            If _demandaM > 0.01F AndAlso phiMnAtP > 0.01F Then
                cd = phiMnAtP / _demandaM
            ElseIf _demandaM <= 0.01F AndAlso _demandaP > 0.01F AndAlso _datosDI.PhiP0 > 0.01F Then
                cd = _datosDI.PhiP0 / _demandaP
            End If
            Dim cumpleDemanda = cd >= 1.0F
            Dim dotClr = If(cumpleDemanda, Color.FromArgb(0, 160, 80), Color.FromArgb(210, 40, 40))
            Dim dxP = toX(_demandaM), dyP = toY(_demandaP)
            g.FillEllipse(New SolidBrush(dotClr), dxP - 8, dyP - 8, 16, 16)
            g.DrawEllipse(New Pen(Color.Black, 1.5F), dxP - 8, dyP - 8, 16, 16)
            g.DrawLine(New Pen(dotClr, 1.5F) With {.DashStyle = DashStyle.Dot},
                       dxP, dyP, dxP, mT + ph)
            g.DrawLine(New Pen(dotClr, 1.5F) With {.DashStyle = DashStyle.Dot},
                       mL, dyP, dxP, dyP)
            Dim fntCD = New Font("Segoe UI", 10F, FontStyle.Bold)
            g.DrawString(String.Format("C/D = {0:F3}", cd),
                         fntCD, New SolidBrush(dotClr), dxP + 12, dyP - 14)
            g.DrawString("Demanda", New Font("Segoe UI", 8.5F),
                         Brushes.Black, dxP + 12, dyP + 4)
        End If

        ' Ejes
        Dim axPen = New Pen(Color.FromArgb(50, 50, 50), 1.5F)
        g.DrawLine(axPen, mL, mT, mL, mT + ph)
        g.DrawLine(axPen, mL, mT + ph, mL + pw, mT + ph)

        Dim fnt = New Font("Segoe UI", 9.5F)
        Dim fntB = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Dim fntTitle = New Font("Segoe UI", 10F, FontStyle.Bold)
        Dim sfR As New StringFormat With {.Alignment = StringAlignment.Far}

        ' Ticks X
        For ix = 0 To 4
            Dim m = maxM * ix / 4
            Dim xx = CSng(mL + m / maxM * pw)
            g.DrawLine(Pens.Black, xx, mT + ph, xx, mT + ph + 5)
            g.DrawString(String.Format("{0:F0}", m), fnt, Brushes.Black, xx - 18, mT + ph + 8)
        Next
        g.DrawString("Momento  M  (kN·m)", fntTitle, Brushes.Black,
                     CSng(mL + pw / 2 - 70), hPx - 24)

        ' Ticks Y
        For iy = 0 To 5
            Dim p = minP + pRng * iy / 5
            Dim yy = toY(p)
            g.DrawLine(Pens.Black, mL, yy, mL - 5, yy)
            g.DrawString(String.Format("{0:F0}", p), fnt, Brushes.Black,
                         New RectangleF(2, yy - 10, mL - 9, 20), sfR)
        Next

        ' Etiqueta Y rotada
        Dim st = g.Save()
        g.TranslateTransform(14, mT + ph / 2)
        g.RotateTransform(-90)
        g.DrawString("Fuerza axial  P  (kN)", fntTitle, Brushes.Black, 0, 0,
                     New StringFormat With {.Alignment = StringAlignment.Center})
        g.Restore(st)

        ' Info en esquinas
        g.DrawString(String.Format("P₀ = {0:F0} kN   φP₀ = {1:F0} kN",
                     _datosDI.P0, _datosDI.PhiP0), fntB, Brushes.DarkBlue, mL + 4, mT + 4)

        ' Leyenda
        Dim lx = mL + pw - 145, ly = mT + 10
        g.DrawLine(New Pen(Color.Silver, 1.5F) With {.DashStyle = DashStyle.Dash}, lx, ly, lx + 22, ly)
        g.DrawString("Nominal", fnt, Brushes.Gray, lx + 26, ly - 7)
        g.DrawLine(New Pen(Color.FromArgb(0, 82, 164), 2.5F), lx, ly + 18, lx + 22, ly + 18)
        g.DrawString("Reducido  φ", fnt, New SolidBrush(Color.FromArgb(0, 82, 164)), lx + 26, ly + 11)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  DIAGRAMA MOMENTO-CURVATURA
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnPaintMK(sender As Object, e As PaintEventArgs)
        DibujarMK(e.Graphics, picMK.Width, picMK.Height)
    End Sub

    Private Sub DibujarMK(g As Graphics, wPx As Integer, hPx As Integer)
        g.Clear(Color.White)
        g.SmoothingMode = SmoothingMode.AntiAlias
        If _datosMK Is Nothing OrElse _datosMK.Curvaturas.Count < 2 Then
            MensajeSinDatos(g, wPx, hPx, "Presione CALCULAR para generar el diagrama M-κ")
            Return
        End If

        Dim mL = 105, mR = 24, mT = 36, mB = 62
        Dim pw = wPx - mL - mR, ph = hPx - mT - mB
        If pw < 50 OrElse ph < 50 Then Return

        Dim maxK = _datosMK.Curvaturas.Max() * 1.12F : If maxK < 0.0001F Then maxK = 0.01F
        Dim maxM = _datosMK.Momentos.Max() * 1.18F : If maxM < 1 Then maxM = 1

        Dim toX = Function(k As Single) CSng(mL + k / maxK * pw)
        Dim toY = Function(m As Single) CSng(mT + (1 - m / maxM) * ph)

        ' Grid
        Dim gPen = New Pen(Color.FromArgb(220, 225, 238))
        For ix = 0 To 4
            g.DrawLine(gPen, CSng(mL + ix / 4.0 * pw), mT, CSng(mL + ix / 4.0 * pw), mT + ph)
        Next
        For iy = 0 To 5
            g.DrawLine(gPen, mL, CSng(mT + iy / 5.0 * ph), mL + pw, CSng(mT + iy / 5.0 * ph))
        Next

        ' Zona plástica (entre κy y κu) — relleno suave
        If _datosMK.Ky > 0 AndAlso _datosMK.Ku > _datosMK.Ky Then
            Dim kyX = toX(Math.Min(_datosMK.Ky, maxK / 1.12F))
            Dim kuX = toX(Math.Min(_datosMK.Ku, maxK / 1.12F))
            g.FillRectangle(New SolidBrush(Color.FromArgb(18, 255, 140, 0)),
                            kyX, mT, kuX - kyX, ph)
        End If

        ' Curva M-κ (azul sólido)
        Dim pts(_datosMK.Curvaturas.Count - 1) As PointF
        For i = 0 To _datosMK.Curvaturas.Count - 1
            pts(i) = New PointF(toX(_datosMK.Curvaturas(i)), toY(_datosMK.Momentos(i)))
        Next
        g.DrawLines(New Pen(Color.FromArgb(0, 82, 164), 2.5F), pts)

        Dim fnt = New Font("Segoe UI", 9.5F)
        Dim fntB = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Dim fntTitle = New Font("Segoe UI", 10F, FontStyle.Bold)

        ' Punto κy (verde)
        If _datosMK.Ky > 0 AndAlso _datosMK.My > 0 Then
            Dim kyCl = Math.Min(_datosMK.Ky, maxK / 1.12F)
            Dim kypx = toX(kyCl), kypy = toY(_datosMK.My)
            g.FillEllipse(Brushes.SeaGreen, kypx - 7, kypy - 7, 14, 14)
            g.DrawEllipse(New Pen(Color.DarkGreen, 1.2F), kypx - 7, kypy - 7, 14, 14)
            g.DrawLine(New Pen(Color.SeaGreen, 1.2F) With {.DashStyle = DashStyle.Dot},
                       kypx, kypy, kypx, mT + ph)
            g.DrawLine(New Pen(Color.SeaGreen, 1.2F) With {.DashStyle = DashStyle.Dot},
                       mL, kypy, kypx, kypy)
            g.DrawString(String.Format("κy = {0:F4} /m", _datosMK.Ky),
                         fntB, Brushes.SeaGreen, kypx + 10, kypy - 18)
            g.DrawString(String.Format("My = {0:F1} kN·m", _datosMK.My),
                         fnt, Brushes.SeaGreen, mL + 4, kypy - 16)
        End If

        ' Punto κu (naranja)
        If _datosMK.Ku > 0 AndAlso _datosMK.Mu > 0 Then
            Dim kuCl = Math.Min(_datosMK.Ku, maxK / 1.12F)
            Dim kupx = toX(kuCl), kupy = toY(_datosMK.Mu)
            g.FillEllipse(Brushes.OrangeRed, kupx - 7, kupy - 7, 14, 14)
            g.DrawEllipse(New Pen(Color.DarkOrange, 1.2F), kupx - 7, kupy - 7, 14, 14)
            g.DrawLine(New Pen(Color.OrangeRed, 1.2F) With {.DashStyle = DashStyle.Dot},
                       kupx, kupy, kupx, mT + ph)
            g.DrawString(String.Format("κu = {0:F4} /m", _datosMK.Ku),
                         fntB, Brushes.OrangeRed, kupx - 100, mT + ph - 30)
            g.DrawString(String.Format("Mu = {0:F1} kN·m", _datosMK.Mu),
                         fnt, Brushes.OrangeRed, kupx - 100, mT + ph - 14)
        End If

        ' ── Ductilidad μφ = κu/κy ── caja destacada ──────────────────
        If _datosMK.Ductilidad > 0 Then
            Dim fntDuct = New Font("Segoe UI", 14, FontStyle.Bold)
            Dim fntFrac = New Font("Segoe UI", 8.5F, FontStyle.Italic)
            Dim txtDuct = String.Format("μφ = {0:F2}", _datosMK.Ductilidad)
            Dim txtFrac As String
            If _datosMK.Ky > 0 Then
                txtFrac = String.Format("κu / κy  =  {0:F4} / {1:F4}  (1/m)",
                                        _datosMK.Ku, _datosMK.Ky)
            Else
                txtFrac = "κy no determinado"
            End If
            Dim szD = g.MeasureString(txtDuct, fntDuct)
            Dim szF = g.MeasureString(txtFrac, fntFrac)
            Dim boxW = Math.Max(szD.Width, szF.Width) + 16
            Dim boxH = szD.Height + szF.Height + 10
            Dim rx = mL + pw - boxW - 6
            g.FillRectangle(New SolidBrush(Color.FromArgb(240, 248, 255)),
                            rx - 4, mT + 6, boxW, boxH)
            g.DrawRectangle(New Pen(Color.FromArgb(0, 82, 164), 1.2F),
                            rx - 4, mT + 6, boxW, boxH)
            g.DrawString(txtDuct, fntDuct,
                         New SolidBrush(Color.FromArgb(0, 82, 164)), rx, mT + 8)
            g.DrawString(txtFrac, fntFrac, Brushes.DimGray,
                         rx, CSng(mT + 8 + szD.Height + 2))

            If Not String.IsNullOrEmpty(_datosMK.CausaFalla) Then
                Dim fntCausa = New Font("Segoe UI", 8.5F, FontStyle.Italic)
                Dim szC = g.MeasureString(_datosMK.CausaFalla, fntCausa)
                g.DrawString(_datosMK.CausaFalla, fntCausa, Brushes.DimGray,
                             mL + pw - szC.Width - 8, CSng(mT + 8 + boxH + 4))
            End If
        End If

        ' Ejes
        g.DrawLine(New Pen(Color.FromArgb(50, 50, 50), 1.5F), mL, mT, mL, mT + ph)
        g.DrawLine(New Pen(Color.FromArgb(50, 50, 50), 1.5F), mL, mT + ph, mL + pw, mT + ph)

        Dim sfR As New StringFormat With {.Alignment = StringAlignment.Far}

        ' Ticks X
        For ix = 0 To 4
            Dim k = maxK * ix / 4
            Dim xx = CSng(mL + k / maxK * pw)
            g.DrawLine(Pens.Black, xx, mT + ph, xx, mT + ph + 5)
            g.DrawString(String.Format("{0:F4}", k), fnt, Brushes.Black, xx - 20, mT + ph + 8)
        Next
        g.DrawString("Curvatura  κ  (1/m)", fntTitle, Brushes.Black,
                     CSng(mL + pw / 2 - 68), hPx - 24)

        ' Ticks Y
        For iy = 0 To 5
            Dim m = maxM * iy / 5
            Dim yy = toY(m)
            g.DrawLine(Pens.Black, mL, yy, mL - 5, yy)
            g.DrawString(String.Format("{0:F0}", m), fnt, Brushes.Black,
                         New RectangleF(2, yy - 10, mL - 9, 20), sfR)
        Next

        ' Etiqueta Y
        Dim st = g.Save()
        g.TranslateTransform(14, mT + ph / 2)
        g.RotateTransform(-90)
        g.DrawString("Momento  M  (kN·m)", fntTitle, Brushes.Black, 0, 0,
                     New StringFormat With {.Alignment = StringAlignment.Center})
        g.Restore(st)

        ' Leyenda zona plástica
        If _datosMK.Ductilidad > 0 Then
            g.FillRectangle(New SolidBrush(Color.FromArgb(60, 255, 140, 0)), mL + 4, mT + 6, 16, 12)
            g.DrawString("Zona plástica  (κy → κu)", fnt, Brushes.DarkOrange, mL + 24, mT + 4)
        End If
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  TABLA DE RESULTADOS NUMÉRICOS
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub RellenarResultados()
        dgvRes.Rows.Clear() : dgvRes.Columns.Clear()
        dgvRes.Columns.Add("par", "Parámetro")
        dgvRes.Columns.Add("val", "Valor")
        dgvRes.Columns.Add("uni", "Unidades")
        If _datosDI Is Nothing Then Return

        Dim add = Sub(a As String, b As String, c As String) dgvRes.Rows.Add(a, b, c)

        Dim addSep = Sub(t As String)
                         Dim ri = dgvRes.Rows.Add(t, "", "")
                         dgvRes.Rows(ri).DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120)
                         dgvRes.Rows(ri).DefaultCellStyle.ForeColor = Color.White
                         dgvRes.Rows(ri).DefaultCellStyle.Font = New Font("Segoe UI", 8, FontStyle.Bold)
                         dgvRes.Rows(ri).Height = 22
                     End Sub

        Dim fc_val As Single : Single.TryParse(tbFc.Text, fc_val)

        addSep("  SECCIÓN")
        add("Ángulo de análisis θ", String.Format("{0:F0}", _datosDI.ThetaGrados), "°")
        add("Ag", String.Format("{0:F2}", _datosDI.Ag_cm2), "cm²")
        add("Ast  (total)", String.Format("{0:F3}", _datosDI.Ast_cm2), "cm²")
        add("ρ  (cuantía)", String.Format("{0:F3}", _datosDI.rho_pct), "%")
        add("N° de barras", CStr(If(_barras IsNot Nothing, _barras.Count, 0)), "")

        addSep("  DIAGRAMA DE INTERACCIÓN  P-M")
        add("P₀  (nominal)", String.Format("{0:F1}", _datosDI.P0), "kN")
        add("φP₀  (máx. diseño)", String.Format("{0:F1}", _datosDI.PhiP0), "kN")
        add("P_bal  (nominal)", String.Format("{0:F1}", _datosDI.P_bal), "kN")
        add("M_bal  (nominal)", String.Format("{0:F2}", _datosDI.M_bal), "kN·m")
        add("φP_bal", String.Format("{0:F1}", _datosDI.PhiP_bal), "kN")
        add("φM_bal", String.Format("{0:F2}", _datosDI.PhiM_bal), "kN·m")
        add("P_mín  (tracción pura)", String.Format("{0:F1}", _datosDI.Pmin), "kN")
        add("φP_mín", String.Format("{0:F1}", _datosDI.PhiPmin), "kN")

        addSep("  CONFINAMIENTO — MANDER 1988")
        add("f'cc", String.Format("{0:F2}", _fcc), "MPa")
        add("f'cc / f'c", String.Format("{0:F3}", If(fc_val > 0, _fcc / fc_val, 0)), "")
        add("ε_cc  (conf.)", String.Format("{0:F5}", _eps_cc), "m/m")
        add("ε_cu* (conf.)", String.Format("{0:F5}", _eps_cu), "m/m")

        If _datosMK IsNot Nothing AndAlso _datosMK.Curvaturas.Count > 0 Then
            addSep("  DIAGRAMA MOMENTO-CURVATURA")
            add("κ_y  (fluencia)", String.Format("{0:F5}", _datosMK.Ky), "1/m")
            add("My", String.Format("{0:F2}", _datosMK.My), "kN·m")
            add("κ_u  (último)", String.Format("{0:F5}", _datosMK.Ku), "1/m")
            add("Mu", String.Format("{0:F2}", _datosMK.Mu), "kN·m")
            add("μφ  (ductilidad)", String.Format("{0:F3}", _datosMK.Ductilidad), "κu / κy")
            add("Causa de falla", _datosMK.CausaFalla, "")
        End If
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  HELPER: mensaje centrado cuando no hay datos
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub MensajeSinDatos(g As Graphics, w As Integer, h As Integer, msg As String)
        Dim fnt = New Font("Segoe UI", 9, FontStyle.Italic)
        Dim sz = g.MeasureString(msg, fnt)
        g.DrawString(msg, fnt, Brushes.Gray,
                     (w - sz.Width) / 2, (h - sz.Height) / 2)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  VERIFICACIÓN PUNTUAL P, M
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnVerificarClick(sender As Object, e As EventArgs)
        Dim p As Single, m As Single
        If Not Single.TryParse(tbDemandaP.Text, p) Then
            MessageBox.Show("P demanda inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
        End If
        If Not Single.TryParse(tbDemandaM.Text, m) OrElse m < 0 Then
            MessageBox.Show("M demanda inválido (debe ser ≥ 0).", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
        End If
        If _datosDI Is Nothing OrElse _datosDI.PhiPn.Count < 2 Then
            MessageBox.Show("Calcule el diagrama primero.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
        End If
        _demandaP = p : _demandaM = m
        Dim phiMnAtP = InterpolatePhiMnAtP(p)
        Dim cd As Single = 0
        If m > 0.01F AndAlso phiMnAtP > 0.01F Then
            cd = phiMnAtP / m
        ElseIf m <= 0.01F AndAlso p > 0.01F AndAlso _datosDI.PhiP0 > 0.01F Then
            cd = _datosDI.PhiP0 / p
        End If
        Dim cumple = cd >= 1.0F
        lblCD10.Text = String.Format("C/D = {0:F3}  {1}", cd, If(cumple, "✓", "✗"))
        lblCD10.ForeColor = If(cumple, Color.FromArgb(0, 130, 60), Color.FromArgb(190, 30, 30))
        tabs.SelectedIndex = 0
        picDI.Refresh()
    End Sub

    Private Function InterpolatePhiMnAtP(Pu As Single) As Single
        If _datosDI Is Nothing OrElse _datosDI.PhiPn.Count < 2 Then Return 0
        Dim bestM As Single = 0
        For i = 0 To _datosDI.PhiPn.Count - 2
            Dim p1 = _datosDI.PhiPn(i), p2 = _datosDI.PhiPn(i + 1)
            Dim m1 = _datosDI.PhiMn(i), m2 = _datosDI.PhiMn(i + 1)
            Dim pLo = Math.Min(p1, p2), pHi = Math.Max(p1, p2)
            If Pu >= pLo AndAlso Pu <= pHi AndAlso Math.Abs(p2 - p1) > 0.001F Then
                Dim t = (Pu - p1) / (p2 - p1)
                Dim mInterp = m1 + t * (m2 - m1)
                If mInterp > bestM Then bestM = mInterp
            End If
        Next
        Return bestM
    End Function

    ' ══════════════════════════════════════════════════════════════════════
    '  SUPERFICIE DI 3D
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub GenerarSuperficie3D(fc As Single, fy As Single, Es As Single, rec As Single,
                                     B As Single, H As Single, D As Single)
        _surf3D = Nothing
        _maxM3D = 0 : _maxP3D = 0 : _minP3D = 0

        Dim nAng = _nAng3D : Dim nP = _nP3D
        Dim disPorAngulo(nAng - 1) As DatosDI10

        For a = 0 To nAng - 1
            Dim theta = CSng(a * 2.0 * Math.PI / nAng)
            Try
                If _tipoSec = TipoSeccion10.Rectangular Then
                    disPorAngulo(a) = DI_Rectangular10(B, H, _barras, fc, fy, Es, rec, theta)
                Else
                    disPorAngulo(a) = DI_Circular10(D, _barras, fc, fy, Es, theta)
                End If
            Catch
                disPorAngulo(a) = New DatosDI10()
            End Try
        Next

        ' Determinar rango global de P para la grilla uniforme
        Dim globalPmax = Single.MinValue, globalPmin = Single.MaxValue
        For a = 0 To nAng - 1
            Dim di = disPorAngulo(a)
            If di Is Nothing OrElse di.PhiPn.Count < 2 Then Continue For
            If di.PhiPn.Max() > globalPmax Then globalPmax = di.PhiPn.Max()
            If di.PhiPn.Min() < globalPmin Then globalPmin = di.PhiPn.Min()
        Next
        If globalPmax <= globalPmin Then Return
        _maxP3D = globalPmax : _minP3D = globalPmin

        ReDim _surf3D(nP * nAng - 1)
        Dim maxMsofar As Double = 0

        For pIdx = 0 To nP - 1
            Dim pVal = CSng(globalPmin + (globalPmax - globalPmin) * pIdx / (nP - 1))
            For a = 0 To nAng - 1
                Dim di = disPorAngulo(a)
                Dim phiMn As Single = 0
                If di IsNot Nothing AndAlso di.PhiPn.Count > 1 Then
                    phiMn = InterpolarPhiMnDI3D(di, pVal)
                End If
                Dim theta = CSng(a * 2.0 * Math.PI / nAng)
                ' X = Mx (eje fuerte, cosθ), Y = My (eje débil, sinθ)
                Dim mx = phiMn * CSng(Math.Cos(theta))
                Dim my = phiMn * CSng(Math.Sin(theta))
                _surf3D(pIdx * nAng + a) = New Pt3D10(mx, my, pVal)
                If Math.Abs(mx) > maxMsofar Then maxMsofar = Math.Abs(mx)
                If Math.Abs(my) > maxMsofar Then maxMsofar = Math.Abs(my)
            Next
        Next
        _maxM3D = If(maxMsofar < 0.1, 1.0, maxMsofar)
    End Sub

    Private Function InterpolarPhiMnDI3D(di As DatosDI10, pVal As Single) As Single
        Dim bestM As Single = 0
        For i = 0 To di.PhiPn.Count - 2
            Dim p1 = di.PhiPn(i), p2 = di.PhiPn(i + 1)
            Dim pLo = Math.Min(p1, p2), pHi = Math.Max(p1, p2)
            If pVal >= pLo AndAlso pVal <= pHi AndAlso Math.Abs(p2 - p1) > 0.001F Then
                Dim t = (pVal - p1) / (p2 - p1)
                Dim m = di.PhiMn(i) + t * (di.PhiMn(i + 1) - di.PhiMn(i))
                If m > bestM Then bestM = m
            End If
        Next
        Return bestM
    End Function

    ' ── Paint 3D ────────────────────────────────────────────────────────
    Private Sub OnPaintDI3D(sender As Object, e As PaintEventArgs)
        Dim gfx = e.Graphics
        gfx.Clear(Color.FromArgb(16, 20, 34))
        gfx.SmoothingMode = SmoothingMode.AntiAlias
        If _surf3D Is Nothing OrElse _maxM3D < 0.01 Then
            Dim fntMsg = New Font("Segoe UI", 9, FontStyle.Italic)
            gfx.DrawString("Presione CALCULAR para generar la superficie 3D",
                           fntMsg, New SolidBrush(Color.FromArgb(140, 160, 200)),
                           30, picDI3D.Height / 2 - 10)
            Return
        End If
        Dim W = picDI3D.Width, H = picDI3D.Height
        Dim cx = W / 2.0F, cy = H / 2.0F
        Dim baseScale As Double = Math.Min(W, H) * 0.36 * _zoom3D
        Dim cosAz = Math.Cos(_azim3D), sinAz = Math.Sin(_azim3D)
        Dim cosEl = Math.Cos(_elev3D), sinEl = Math.Sin(_elev3D)
        Dim pRange = _maxP3D - _minP3D : If pRange < 1 Then Return

        Dim nP = _nP3D, nAng = _nAng3D
        Dim screenPts(nP * nAng - 1) As PointF
        Dim depths(nP * nAng - 1) As Double
        For idx = 0 To _surf3D.Length - 1
            Dim nx = _surf3D(idx).X / _maxM3D
            Dim ny = _surf3D(idx).Y / _maxM3D
            Dim nz = (_surf3D(idx).Z - (_maxP3D + _minP3D) / 2.0) / pRange
            screenPts(idx) = Project3D10(nx, ny, nz, cosAz, sinAz, cosEl, sinEl, cx, cy, baseScale, depths(idx))
        Next

        ' Anillos horizontales (P = cte)
        Dim ringOrder = Enumerable.Range(0, nP).
            OrderByDescending(Function(pi) (Enumerable.Range(0, nAng).
                Sum(Function(ai) depths(pi * nAng + ai)) / nAng)).ToList()
        For Each pIdx In ringOrder
            Dim ring(nAng) As Point
            For aIdx = 0 To nAng - 1
                Dim sp = screenPts(pIdx * nAng + aIdx)
                ring(aIdx) = New Point(CInt(sp.X), CInt(sp.Y))
            Next
            ring(nAng) = ring(0)
            Dim avgDepth = Enumerable.Range(0, nAng).Average(Function(ai) depths(pIdx * nAng + ai))
            Dim alpha = CInt(Math.Max(35, Math.Min(155, 35 + avgDepth * 95)))
            gfx.DrawLines(New Pen(Color.FromArgb(alpha, 80, 140, 220), 0.7F), ring)
        Next
        ' Meridianos (ángulo = cte)
        For aIdx = 0 To nAng - 1
            If aIdx Mod 3 <> 0 Then Continue For
            Dim merid(nP - 1) As Point
            For pIdx = 0 To nP - 1
                Dim sp = screenPts(pIdx * nAng + aIdx)
                merid(pIdx) = New Point(CInt(sp.X), CInt(sp.Y))
            Next
            gfx.DrawLines(New Pen(Color.FromArgb(50, 100, 160, 220), 0.5F), merid)
        Next

        DrawAxes3D10(gfx, cx, cy, baseScale, cosAz, sinAz, cosEl, sinEl)

        ' Leyenda
        Dim fntL = New Font("Segoe UI", 8.5F)
        Dim fntLB = New Font("Segoe UI", 9F, FontStyle.Bold)
        gfx.DrawString("Superficie  φ·Pn – φ·Mn (biaxial)", fntLB,
                        New SolidBrush(Color.FromArgb(180, 210, 255)), 10, 10)
        gfx.DrawString("Arrastre: rotar   Rueda: zoom", fntL,
                        New SolidBrush(Color.FromArgb(130, 150, 200)), 10, 28)
    End Sub

    Private Function Project3D10(nx As Double, ny As Double, nz As Double,
                                   cosAz As Double, sinAz As Double,
                                   cosEl As Double, sinEl As Double,
                                   cx As Single, cy As Single, scale As Double,
                                   ByRef depth As Double) As PointF
        Dim rx = nx * cosAz - ny * sinAz
        Dim ry = nx * sinAz + ny * cosAz
        Dim rz = nz
        Dim px = rx
        Dim py2 = ry * sinEl + rz * cosEl
        Dim pz = -ry * cosEl + rz * sinEl
        depth = pz
        Return New PointF(CSng(cx + px * scale), CSng(cy - py2 * scale))
    End Function

    Private Sub DrawAxes3D10(gfx As Graphics, cx As Single, cy As Single, scale As Double,
                               cosAz As Double, sinAz As Double, cosEl As Double, sinEl As Double)
        Const axLen As Double = 1.3
        Dim fnt = New Font("Segoe UI", 9F, FontStyle.Bold)
        Dim dOrg As Double = 0
        Dim origin = Project3D10(0, 0, 0, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dOrg)
        Dim tips(2) As PointF : Dim dDummy As Double = 0
        tips(0) = Project3D10(axLen, 0, 0, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dDummy)
        tips(1) = Project3D10(0, axLen, 0, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dDummy)
        tips(2) = Project3D10(0, 0, axLen, cosAz, sinAz, cosEl, sinEl, cx, cy, scale, dDummy)
        Dim lbls = {"φ·Mn (eje H)", "φ·Mn (eje B)", "P (+comp)"}
        Dim clrs = {Color.FromArgb(255, 80, 80), Color.FromArgb(80, 200, 80), Color.FromArgb(100, 160, 255)}
        For i = 0 To 2
            gfx.DrawLine(New Pen(clrs(i), 1.5F), origin.X, origin.Y, tips(i).X, tips(i).Y)
            gfx.DrawString(lbls(i), fnt, New SolidBrush(clrs(i)), tips(i).X + 4, tips(i).Y - 6)
        Next
    End Sub

    ' ── Eventos mouse 3D ────────────────────────────────────────────────
    Private Sub OnDI3D_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            _drag3D = True : _drag3DStart = e.Location
            _azim3DStart = _azim3D : _elev3DStart = _elev3D
        End If
    End Sub

    Private Sub OnDI3D_MouseMove(sender As Object, e As MouseEventArgs)
        If _drag3D Then
            _azim3D = _azim3DStart + (e.X - _drag3DStart.X) / 200.0
            _elev3D = Math.Max(-1.4, Math.Min(1.4, _elev3DStart - (e.Y - _drag3DStart.Y) / 200.0))
            picDI3D.Refresh()
        End If
    End Sub

    Private Sub OnDI3D_MouseUp(sender As Object, e As MouseEventArgs)
        _drag3D = False
    End Sub

    Private Sub OnDI3D_MouseWheel(sender As Object, e As MouseEventArgs)
        _zoom3D = Math.Max(0.3, Math.Min(4.0, _zoom3D * If(e.Delta > 0, 1.12, 0.89)))
        picDI3D.Refresh()
    End Sub

End Class
