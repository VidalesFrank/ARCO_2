Imports System.Drawing.Drawing2D
Imports ARCO.Funciones_10_AnalisisSeccion
Imports ARCO.AnalisisSeccionV2Service
Imports ARCO.Funciones_00_Varias

' ════════════════════════════════════════════════════════════════════════════
'  MÓDULO 10 v2.0 — ANÁLISIS DE SECCIÓN PRO
'
'  Diferencias vs v1:
'   • Eje neutro c(P) por bisección exacta (curva NOMINAL Pn, sin φ).
'   • Motor de dibujo profesional (SeccionDibujoPro) con hatching, bloque de
'     Whitney sombreado, cotas ingenieriles, tags de barras y arco de θ.
'   • Toggles de vista (checkboxes) debajo del dibujo.
'   • Tab nueva "Deformaciones" con perfil ε(profundidad) y εs por barra.
'
'  El motor de cálculo (DI, M-κ, superficie 3D) se comparte con v1 sin cambios.
' ════════════════════════════════════════════════════════════════════════════
Public Class Form_10_AnalisisSeccion_V2
    Inherits Form

    ' ── Resultados calculados ──────────────────────────────────────────────
    Private _datosDI As DatosDI10
    Private _datosMK As DatosMK10
    Private _estadoSec As EstadoSeccion   ' Nuevo: eje neutro por Pu
    Private _barras As List(Of RefuerzoSimple)
    Private _tipoSec As TipoSeccion10 = TipoSeccion10.Rectangular
    Private _fcc As Single, _eps_cc As Single, _eps_cu As Single

    ' ── Controles de sección ───────────────────────────────────────────────
    Private rbRect As New RadioButton With {.Text = "Rectangular / Cuadrada", .AutoSize = True}
    Private rbCirc As New RadioButton With {.Text = "Circular", .AutoSize = True}
    Private lblB As New Label, tbB As New TextBox With {.Text = "0.40"}
    Private lblH As New Label, tbH As New TextBox With {.Text = "0.60"}
    Private lblD As New Label, tbD As New TextBox With {.Text = "0.50"}
    Private tbRec As New TextBox With {.Text = "0.040"}
    Private tbFc As New TextBox With {.Text = "21"}
    Private tbFy As New TextBox With {.Text = "420"}
    Private tbEs As New TextBox With {.Text = "200000"}
    Private dgvRef As New DataGridView
    Private panRefRect As New Panel
    Private panRefCirc As New Panel
    Private cbBarraCirc As New ComboBox
    Private nudNCirc As New NumericUpDown
    Private cbBarraEst As New ComboBox
    Private tbSEst As New TextBox With {.Text = "0.10"}
    Private nudRamasB As New NumericUpDown
    Private nudRamasH As New NumericUpDown
    Private tbEpsSu As New TextBox With {.Text = "0.09"}
    Private tbP As New TextBox With {.Text = "0"}
    Private nudAngulo As New NumericUpDown
    Private btnCalc As New Button
    Private lblMander As New Label
    Private lblAdvertenciaMK As New Label

    ' ── NUEVO: eje neutro por Pu nominal ───────────────────────────────────
    Private tbPuNominal As New TextBox With {.Text = "0"}
    Private btnCalcC As New Button
    Private lblResultadoC As New Label

    ' ── Visualización ──────────────────────────────────────────────────────
    ' picSec es una PictureBox subclase-selectable para recibir MouseWheel sin
    ' que el usuario tenga que hacer clic para dar el foco.
    Private picSec As New PicSecFoco()
    Private picDI As New PictureBox
    Private picMK As New PictureBox
    Private picDef As New PictureBox   ' NUEVO
    Private dgvRes As New DataGridView
    Private tabs As New TabControl

    ' ── Estado de zoom/pan de la sección ──────────────────────────────────
    Private _zoomSec As Single = 1.0F
    Private _panXSec As Single = 0
    Private _panYSec As Single = 0
    Private _dragSec As Boolean = False
    Private _dragStart As Point
    Private _panStartX As Single, _panStartY As Single

    ' ── Toggles del dibujo pro (checkboxes) ────────────────────────────────
    Private cbHatch As New CheckBox With {.Text = "Hatching", .Checked = True, .AutoSize = True}
    Private cbWhitney As New CheckBox With {.Text = "Bloque a", .Checked = True, .AutoSize = True}
    Private cbEN As New CheckBox With {.Text = "Eje neutro", .Checked = True, .AutoSize = True}
    Private cbCotas As New CheckBox With {.Text = "Cotas", .Checked = True, .AutoSize = True}
    Private cbTagsBar As New CheckBox With {.Text = "Tags barra", .Checked = True, .AutoSize = True}
    Private cbEjesXY As New CheckBox With {.Text = "Ejes X-Y", .Checked = True, .AutoSize = True}
    Private cbAngulo As New CheckBox With {.Text = "θ", .Checked = True, .AutoSize = True}
    Private cbLeyenda As New CheckBox With {.Text = "Leyenda", .Checked = True, .AutoSize = True}

    ' ── Transformación de píxeles del dibujo ───────────────────────────────
    Private _cxSec As Single, _cySec As Single, _escSec As Single

    ' ── Editor de barra por clic derecho ───────────────────────────────────
    Private _menuBarra10 As New ContextMenuStrip()
    Private _barraContextIdx As Integer = -1

    ' ── Firma de layout ────────────────────────────────────────────────────
    Private _lastLayoutSig As String = Nothing

    ' ── Verificación puntual P, M ─────────────────────────────────────────
    Private tbDemandaP As New TextBox With {.Text = ""}
    Private tbDemandaM As New TextBox With {.Text = ""}
    Private lblCD10 As New Label
    Private _demandaP As Single = Single.NaN
    Private _demandaM As Single = Single.NaN

    ' ── Constantes ────────────────────────────────────────────────────────
    Private Shared ReadOnly _barTags As String() = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}

    ' ══════════════════════════════════════════════════════════════════════
    Public Sub New()
        Me.SuspendLayout()
        BuildUI()
        Me.ResumeLayout(False)
    End Sub

    Private Sub BuildUI()
        Me.Text = "ARCO — Análisis de Sección 2.0 (Pro)"
        Me.Size = New Size(1450, 880)
        Me.MinimumSize = New Size(1100, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 8.5F)
        Me.BackColor = Color.FromArgb(240, 242, 245)
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try
        AddHandler Me.Load, AddressOf Form_Load

        ' ┌─────────────────────────────────────────────────────────────┐
        ' │  PANEL IZQUIERDO — INPUTS (340px, scrollable)               │
        ' └─────────────────────────────────────────────────────────────┘
        Dim panLeft As New Panel With {
            .Width = 340, .Dock = DockStyle.Left,
            .BackColor = Color.FromArgb(240, 242, 245),
            .AutoScroll = True}

        Dim y As Integer = 8

        ' ── Botones Archivo (guardar / abrir) ──────────────────────────
        Dim btnAbrir As New Button With {
            .Left = 8, .Top = y, .Width = 156, .Height = 30,
            .Text = "📂  Abrir sección…",
            .Font = New Font("Segoe UI", 9F, FontStyle.Bold),
            .BackColor = Color.FromArgb(240, 245, 250),
            .ForeColor = Color.FromArgb(30, 60, 120),
            .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleCenter}
        btnAbrir.FlatAppearance.BorderColor = Color.FromArgb(180, 195, 220)
        Dim btnGuardar As New Button With {
            .Left = 172, .Top = y, .Width = 158, .Height = 30,
            .Text = "💾  Guardar…",
            .Font = New Font("Segoe UI", 9F, FontStyle.Bold),
            .BackColor = Color.FromArgb(240, 250, 245),
            .ForeColor = Color.FromArgb(30, 100, 60),
            .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleCenter}
        btnGuardar.FlatAppearance.BorderColor = Color.FromArgb(180, 220, 195)
        panLeft.Controls.Add(btnAbrir) : panLeft.Controls.Add(btnGuardar)
        AddHandler btnAbrir.Click, AddressOf OnAbrirSeccion_Click
        AddHandler btnGuardar.Click, AddressOf OnGuardarSeccion_Click
        y += 36

        Dim pSec = MkSection(panLeft, "SECCIÓN", y, 156) : y += 162
        rbRect.Location = New Point(10, 28) : rbRect.Checked = True : pSec.Controls.Add(rbRect)
        rbCirc.Location = New Point(10, 52) : pSec.Controls.Add(rbCirc)
        lblB = MkLbl(pSec, "B (m):", 10, 82) : MkTb(tbB, pSec, 90, 80, 72)
        lblH = MkLbl(pSec, "H (m):", 10, 108) : MkTb(tbH, pSec, 90, 106, 72)
        lblD = MkLbl(pSec, "D (m):", 10, 108) : MkTb(tbD, pSec, 90, 106, 72)
        MkLbl(pSec, "Recub. (m):", 170, 82) : MkTb(tbRec, pSec, 250, 80, 56)
        MkLbl(pSec, "(al eje de barra)", 170, 100).ForeColor = Color.Gray

        Dim pMat = MkSection(panLeft, "MATERIALES", y, 102) : y += 108
        MkLbl(pMat, "f'c (MPa):", 10, 28) : MkTb(tbFc, pMat, 92, 26, 60)
        MkLbl(pMat, "fy  (MPa):", 10, 54) : MkTb(tbFy, pMat, 92, 52, 60)
        MkLbl(pMat, "Es  (MPa):", 170, 28) : MkTb(tbEs, pMat, 248, 26, 56)

        Dim pRef = MkSection(panLeft, "REFUERZO LONGITUDINAL", y, 198) : y += 204
        panRefRect.Left = 6 : panRefRect.Top = 24 : panRefRect.Width = 316 : panRefRect.Height = 168
        panRefRect.BackColor = Color.Transparent
        pRef.Controls.Add(panRefRect)
        BuildDgvRef()
        panRefCirc.Left = 6 : panRefCirc.Top = 24 : panRefCirc.Width = 316 : panRefCirc.Height = 50
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
        cbBarraCirc.SelectedIndex = 3
        panRefCirc.Controls.Add(cbBarraCirc)

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
        MkLbl(pConf, "(sólo aplica a columnas con estribos cerrados)", 10, 112).ForeColor = Color.Gray

        Dim pPax = MkSection(panLeft, "CARGA AXIAL — DIAGRAMA M-κ", y, 58) : y += 64
        MkLbl(pPax, "P axial (kN):", 10, 26)
        MkTb(tbP, pPax, 104, 24, 80)
        MkLbl(pPax, "(compresión +)", 192, 28).ForeColor = Color.Gray

        Dim pAng = MkSection(panLeft, "ÁNGULO DE ANÁLISIS", y, 58) : y += 64
        MkLbl(pAng, "θ (grados):", 10, 26)
        MkNud(nudAngulo, pAng, 104, 24, 0, 360, 0)
        MkLbl(pAng, "(0°=eje H, 90°=eje B)", 170, 28).ForeColor = Color.Gray

        ' ── NUEVO: EJE NEUTRO POR Pu NOMINAL ─────────────────────────────
        Dim pEN = MkSection(panLeft, "EJE NEUTRO POR Pu NOMINAL", y, 172) : y += 178
        MkLbl(pEN, "Pu (kN):", 10, 28)
        MkTb(tbPuNominal, pEN, 74, 26, 80)
        MkLbl(pEN, "(sin φ, compresión +)", 162, 30).ForeColor = Color.Gray
        btnCalcC.Left = 8 : btnCalcC.Top = 54 : btnCalcC.Width = 316 : btnCalcC.Height = 26
        btnCalcC.Text = "▶  Calcular c(Pu)"
        btnCalcC.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        btnCalcC.BackColor = Color.FromArgb(180, 30, 30) : btnCalcC.ForeColor = Color.White
        btnCalcC.FlatStyle = FlatStyle.Flat : btnCalcC.FlatAppearance.BorderSize = 0
        btnCalcC.Cursor = Cursors.Hand
        pEN.Controls.Add(btnCalcC)
        AddHandler btnCalcC.Click, AddressOf OnCalcularC_Click

        lblResultadoC.Left = 8 : lblResultadoC.Top = 86 : lblResultadoC.Width = 316 : lblResultadoC.Height = 78
        lblResultadoC.AutoSize = False
        lblResultadoC.Font = New Font("Consolas", 8.5F)
        lblResultadoC.ForeColor = Color.FromArgb(40, 40, 50)
        lblResultadoC.BackColor = Color.FromArgb(250, 245, 240)
        lblResultadoC.BorderStyle = BorderStyle.FixedSingle
        lblResultadoC.Padding = New Padding(6)
        lblResultadoC.TextAlign = ContentAlignment.TopLeft
        lblResultadoC.Text = "Ingrese Pu y presione ""Calcular c(Pu)""."
        pEN.Controls.Add(lblResultadoC)

        Dim pVer = MkSection(panLeft, "VERIFICACIÓN  P, M  EN  DI (con φ)", y, 100) : y += 106
        MkLbl(pVer, "P demanda (kN):", 10, 28)
        MkTb(tbDemandaP, pVer, 120, 26, 90)
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
        lblCD10.Left = 148 : lblCD10.Top = 78 : lblCD10.Width = 168 : lblCD10.Height = 24
        lblCD10.AutoSize = False : lblCD10.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        lblCD10.TextAlign = ContentAlignment.MiddleLeft
        pVer.Controls.Add(lblCD10)

        ' ── Botón CALCULAR ───────────────────────────────────────────────
        btnCalc.Left = 8 : btnCalc.Top = y : btnCalc.Width = 316 : btnCalc.Height = 38
        btnCalc.Text = "▶  CALCULAR"
        btnCalc.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        btnCalc.BackColor = Color.FromArgb(0, 82, 164) : btnCalc.ForeColor = Color.White
        btnCalc.FlatStyle = FlatStyle.Flat : btnCalc.FlatAppearance.BorderSize = 0
        btnCalc.Cursor = Cursors.Hand
        panLeft.Controls.Add(btnCalc) : y += 44

        lblMander.Left = 8 : lblMander.Top = y : lblMander.Width = 316 : lblMander.Height = 46
        lblMander.AutoSize = False : lblMander.Font = New Font("Segoe UI", 7.5F)
        lblMander.ForeColor = Color.FromArgb(0, 100, 50)
        lblMander.BackColor = Color.FromArgb(235, 248, 238)
        lblMander.BorderStyle = BorderStyle.FixedSingle
        lblMander.Padding = New Padding(4)
        panLeft.Controls.Add(lblMander)
        y += 50

        lblAdvertenciaMK.Left = 8 : lblAdvertenciaMK.Top = y : lblAdvertenciaMK.Width = 316 : lblAdvertenciaMK.Height = 40
        lblAdvertenciaMK.AutoSize = False
        lblAdvertenciaMK.Font = New Font("Segoe UI", 7.5F, FontStyle.Bold)
        lblAdvertenciaMK.ForeColor = Color.FromArgb(140, 30, 0)
        lblAdvertenciaMK.BackColor = Color.FromArgb(255, 240, 230)
        lblAdvertenciaMK.BorderStyle = BorderStyle.FixedSingle
        lblAdvertenciaMK.Padding = New Padding(4)
        lblAdvertenciaMK.Text = "⚠ El M-κ no alcanzó el aplastamiento real dentro del barrido."
        lblAdvertenciaMK.Visible = False
        panLeft.Controls.Add(lblAdvertenciaMK)

        ' ┌─────────────────────────────────────────────────────────────┐
        ' │  PANEL CENTRAL — Dibujo pro + toggles                       │
        ' └─────────────────────────────────────────────────────────────┘
        Dim panCenter As New Panel With {
            .Width = 420, .Dock = DockStyle.Left,
            .BackColor = Color.White, .Padding = New Padding(0)}
        Dim hdrSec As New Label With {
            .Text = "  SECCIÓN TRANSVERSAL", .Dock = DockStyle.Top, .Height = 28,
            .BackColor = Color.FromArgb(30, 60, 120), .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft}
        panCenter.Controls.Add(hdrSec)

        Dim panToggles As New Panel With {
            .Dock = DockStyle.Bottom, .Height = 66,
            .BackColor = Color.FromArgb(248, 249, 251),
            .BorderStyle = BorderStyle.FixedSingle}
        panCenter.Controls.Add(panToggles)
        Dim lblToggles As New Label With {
            .Text = "  Vista:", .Left = 4, .Top = 6, .AutoSize = True,
            .Font = New Font("Segoe UI", 8F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(60, 60, 70)}
        panToggles.Controls.Add(lblToggles)
        ' Fila 1
        cbHatch.Left = 50 : cbHatch.Top = 4 : panToggles.Controls.Add(cbHatch)
        cbWhitney.Left = 130 : cbWhitney.Top = 4 : panToggles.Controls.Add(cbWhitney)
        cbEN.Left = 210 : cbEN.Top = 4 : panToggles.Controls.Add(cbEN)
        cbCotas.Left = 300 : cbCotas.Top = 4 : panToggles.Controls.Add(cbCotas)
        ' Fila 2
        cbTagsBar.Left = 4 : cbTagsBar.Top = 24 : panToggles.Controls.Add(cbTagsBar)
        cbEjesXY.Left = 100 : cbEjesXY.Top = 24 : panToggles.Controls.Add(cbEjesXY)
        cbAngulo.Left = 180 : cbAngulo.Top = 24 : panToggles.Controls.Add(cbAngulo)
        cbLeyenda.Left = 220 : cbLeyenda.Top = 24 : panToggles.Controls.Add(cbLeyenda)
        ' Fila 3: leyenda de colores de barra
        Dim lblLeyBar As New Label With {
            .Text = "Barras:  ● tracción≥εy   ● compresión≥εy   ● elástica",
            .Left = 4, .Top = 46, .AutoSize = True,
            .Font = New Font("Segoe UI", 7.5F),
            .ForeColor = Color.FromArgb(80, 80, 90)}
        panToggles.Controls.Add(lblLeyBar)

        For Each cb As CheckBox In {cbHatch, cbWhitney, cbEN, cbCotas, cbTagsBar, cbEjesXY, cbAngulo, cbLeyenda}
            AddHandler cb.CheckedChanged, Sub(s, ev) picSec.Refresh()
        Next

        picSec.Dock = DockStyle.Fill : picSec.BackColor = Color.White
        AddHandler picSec.Paint, AddressOf OnPaintSec
        AddHandler picSec.MouseWheel, AddressOf PicSec_MouseWheel
        AddHandler picSec.MouseMove, AddressOf PicSec_MouseMove
        AddHandler picSec.MouseUp, AddressOf PicSec_MouseUp
        AddHandler picSec.MouseDoubleClick, AddressOf PicSec_MouseDoubleClick
        panCenter.Controls.Add(picSec)

        ' ┌─────────────────────────────────────────────────────────────┐
        ' │  PANEL DERECHO — TABS                                        │
        ' └─────────────────────────────────────────────────────────────┘
        Dim panRight As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.White}
        tabs.Dock = DockStyle.Fill
        tabs.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        tabs.SizeMode = TabSizeMode.Normal
        tabs.ItemSize = New Size(0, 28)

        Dim tDI As New TabPage("  Diagrama P-M  ")
        picDI.Dock = DockStyle.Fill : picDI.BackColor = Color.White
        AddHandler picDI.Paint, AddressOf OnPaintDI
        AddHandler picDI.Resize, Sub(s, ev) picDI.Refresh()
        tDI.Controls.Add(picDI) : tabs.TabPages.Add(tDI)

        Dim tMK As New TabPage("  Momento-Curvatura  ")
        picMK.Dock = DockStyle.Fill : picMK.BackColor = Color.White
        AddHandler picMK.Paint, AddressOf OnPaintMK
        AddHandler picMK.Resize, Sub(s, ev) picMK.Refresh()
        tMK.Controls.Add(picMK) : tabs.TabPages.Add(tMK)

        ' NUEVO: Tab Deformaciones
        Dim tDef As New TabPage("  Deformaciones  ")
        picDef.Dock = DockStyle.Fill : picDef.BackColor = Color.White
        AddHandler picDef.Paint, AddressOf OnPaintDef
        AddHandler picDef.Resize, Sub(s, ev) picDef.Refresh()
        tDef.Controls.Add(picDef) : tabs.TabPages.Add(tDef)

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

        Me.Controls.Add(panRight)
        Me.Controls.Add(panCenter)
        Me.Controls.Add(panLeft)

        AddHandler rbRect.CheckedChanged, AddressOf OnTipoChanged
        AddHandler rbCirc.CheckedChanged, AddressOf OnTipoChanged
        AddHandler btnCalc.Click, AddressOf OnCalcClick

        For Each tb As TextBox In {tbB, tbH, tbD, tbRec}
            AddHandler tb.TextChanged, Sub(s, ev) picSec.Refresh()
        Next
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
        If e.Button = MouseButtons.Right Then
            Dim idx = HitTestBarra10(e.Location)
            If idx >= 0 Then
                _barraContextIdx = idx
                _menuBarra10.Show(picSec, picSec.PointToClient(Cursor.Position))
            End If
            Return
        End If
        ' Click izquierdo → inicia pan
        If e.Button = MouseButtons.Left Then
            _dragSec = True
            _dragStart = e.Location
            _panStartX = _panXSec : _panStartY = _panYSec
            picSec.Cursor = Cursors.SizeAll
        End If
    End Sub

    Private Sub PicSec_MouseMove(sender As Object, e As MouseEventArgs)
        If _dragSec Then
            _panXSec = _panStartX + (e.X - _dragStart.X)
            _panYSec = _panStartY + (e.Y - _dragStart.Y)
            picSec.Refresh()
        End If
    End Sub

    Private Sub PicSec_MouseUp(sender As Object, e As MouseEventArgs)
        If _dragSec Then
            _dragSec = False
            picSec.Cursor = Cursors.Default
        End If
    End Sub

    Private Sub PicSec_MouseWheel(sender As Object, e As MouseEventArgs)
        Dim factor As Single = If(e.Delta > 0, 1.15F, 1.0F / 1.15F)
        Dim zNuevo As Single = Math.Max(0.3F, Math.Min(6.0F, _zoomSec * factor))
        ' Zoom centrado en el cursor: ajusta pan para que el punto bajo el cursor
        ' se mantenga fijo en pantalla.
        Dim k As Single = zNuevo / _zoomSec
        _panXSec = e.X - k * (e.X - _panXSec) - (1 - k) * (picSec.Width / 2.0F)
        _panYSec = e.Y - k * (e.Y - _panYSec) - (1 - k) * (picSec.Height / 2.0F)
        _zoomSec = zNuevo
        picSec.Refresh()
    End Sub

    Private Sub PicSec_MouseDoubleClick(sender As Object, e As MouseEventArgs)
        _zoomSec = 1.0F
        _panXSec = 0 : _panYSec = 0
        picSec.Refresh()
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
            Logger.Error(ex, "Form_10_AnalisisSeccion_V2.MenuBarra10_Click", "Error al reasignar barra")
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  FIRMA DE LAYOUT
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

        Dim colZona As New DataGridViewTextBoxColumn With {
            .Name = "Zona", .HeaderText = "Zona de refuerzo",
            .ReadOnly = True, .FillWeight = 45}
        colZona.DefaultCellStyle.BackColor = Color.FromArgb(240, 242, 248)
        colZona.DefaultCellStyle.Font = New Font("Segoe UI", 8, FontStyle.Bold)
        dgvRef.Columns.Add(colZona)

        Dim colN As New DataGridViewTextBoxColumn With {
            .Name = "N", .HeaderText = "N barras", .FillWeight = 22}
        colN.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgvRef.Columns.Add(colN)

        Dim colBarra As New DataGridViewComboBoxColumn With {
            .Name = "Barra", .HeaderText = "Barra", .FillWeight = 33,
            .DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox}
        For Each s In _barTags : colBarra.Items.Add(s) : Next
        dgvRef.Columns.Add(colBarra)

        dgvRef.Rows.Add("Esquinas (×4 fijas)", "4", "#5")
        dgvRef.Rows.Add("Cara Superior", "2", "#5")
        dgvRef.Rows.Add("Cara Inferior", "2", "#5")
        dgvRef.Rows.Add("Caras Laterales (×2)", "1", "#5")

        dgvRef.Rows(0).Cells(1).ReadOnly = True
        dgvRef.Rows(0).Cells(1).Style.BackColor = Color.FromArgb(235, 235, 235)
        dgvRef.Rows(0).Cells(1).Style.ForeColor = Color.Gray

        dgvRef.RowTemplate.Height = 30
        For Each row As DataGridViewRow In dgvRef.Rows
            row.Height = 30
        Next
        panRefRect.Controls.Add(dgvRef)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  HELPERS DE UI
    ' ══════════════════════════════════════════════════════════════════════
    Private Function MkSection(parent As Panel, titulo As String, top As Integer, height As Integer) As Panel
        Dim hdr As New Label With {
            .Text = "  " & titulo, .Height = 22, .Dock = DockStyle.Top,
            .BackColor = Color.FromArgb(30, 60, 120), .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft}
        Dim pan As New Panel With {
            .Left = 8, .Top = top, .Width = 322, .Height = height,
            .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle}
        pan.Controls.Add(hdr) : parent.Controls.Add(pan)
        Return pan
    End Function

    Private Function MkLbl(parent As Control, text As String, x As Integer, y As Integer) As Label
        Dim lbl As New Label With {
            .Text = text, .Left = x, .Top = y + 2, .AutoSize = True,
            .ForeColor = Color.FromArgb(50, 50, 60)}
        parent.Controls.Add(lbl)
        Return lbl
    End Function

    Private Sub MkTb(tb As TextBox, parent As Control, x As Integer, y As Integer, w As Integer)
        tb.Left = x : tb.Top = y : tb.Width = w
        tb.BorderStyle = BorderStyle.FixedSingle
        parent.Controls.Add(tb)
    End Sub

    Private Sub MkNud(nud As NumericUpDown, parent As Control, x As Integer, y As Integer,
                        minV As Integer, maxV As Integer, defV As Integer)
        nud.Left = x : nud.Top = y : nud.Width = 52
        nud.Minimum = minV : nud.Maximum = maxV : nud.Value = defV
        parent.Controls.Add(nud)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  EVENTOS
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub Form_Load(sender As Object, e As EventArgs)
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
            If Not Single.TryParse(tbEs.Text, Es) OrElse Es <= 0 Then Es = 200000
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
                    MessageBox.Show("No hay barras definidas.", "Sin refuerzo", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If

                Dim b_core = Math.Max(0.01F, secB - 2 * rec)
                Dim h_core = Math.Max(0.01F, secH - 2 * rec)
                CalcularMander10(fc, fy, db_hoop, s_mm, b_core, h_core,
                                  CInt(nudRamasB.Value), CInt(nudRamasH.Value),
                                  _fcc, _eps_cc, _eps_cu)
                _datosDI = DI_Rectangular10(secB, secH, _barras, fc, fy, Es, rec, thetaRad)
                _datosMK = MomentoCurvatura10(secB, secH, TipoSeccion10.Rectangular, rec,
                                               _barras, fc, fy, Es, _fcc, _eps_cc, _eps_cu,
                                               P_ax, thetaRad, eps_su)
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
                    MessageBox.Show("No hay barras definidas.", "Sin refuerzo", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
                End If
                Dim R_core = Math.Max(0.02F, secD / 2 - rec)
                Dim nRamas = CInt(nudRamasB.Value) + CInt(nudRamasH.Value)
                CalcularMander10(fc, fy, db_hoop, s_mm, 2 * R_core, 2 * R_core,
                                  nRamas, nRamas, _fcc, _eps_cc, _eps_cu)
                _datosDI = DI_Circular10(secD, _barras, fc, fy, Es, thetaRad)
                _datosMK = MomentoCurvatura10(secD, secD, TipoSeccion10.Circular, rec,
                                               _barras, fc, fy, Es, _fcc, _eps_cc, _eps_cu,
                                               P_ax, thetaRad, eps_su)
            End If

            _lastLayoutSig = sigActual

            ' Recalcular c(Pu) si el usuario ya dio un Pu válido
            RecalcularEstadoConPuActual()

            lblMander.Text = String.Format(
                "Mander 1988:  f'cc = {0:F2} MPa  (f'cc/f'c = {1:F2})" & vbCrLf &
                "ε_cc = {2:F5}     ε_cu* = {3:F5}",
                _fcc, If(_fcc > 0 AndAlso fc > 0, _fcc / fc, 1),
                _eps_cc, _eps_cu)

            lblAdvertenciaMK.Visible = Not _datosMK.ConvergioPorAplastamiento

            picSec.Refresh() : picDI.Refresh() : picMK.Refresh() : picDef.Refresh()
            RellenarResultados()

        Catch ex As Exception
            Logger.Error(ex, "Form_10_AnalisisSeccion_V2.Calcular", "Error en análisis de sección v2")
            MessageBox.Show("Error en cálculo: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  NUEVO: cálculo del eje neutro dado Pu nominal
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnCalcularC_Click(sender As Object, e As EventArgs)
        Dim Pu As Single
        If Not Single.TryParse(tbPuNominal.Text, Pu) Then
            MessageBox.Show("Pu inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
        End If
        If _barras Is Nothing OrElse _barras.Count = 0 Then
            MessageBox.Show("Presione CALCULAR primero para definir las barras.", "Sin sección",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
        End If
        RecalcularEstadoConPu(Pu)
        picSec.Refresh() : picDef.Refresh()
    End Sub

    Private Sub RecalcularEstadoConPuActual()
        Dim Pu As Single
        If Not Single.TryParse(tbPuNominal.Text, Pu) Then Return
        RecalcularEstadoConPu(Pu)
    End Sub

    Private Sub RecalcularEstadoConPu(Pu As Single)
        Try
            Dim fc As Single, fy As Single, Es As Single, rec As Single
            Single.TryParse(tbFc.Text, fc) : Single.TryParse(tbFy.Text, fy)
            If Not Single.TryParse(tbEs.Text, Es) OrElse Es <= 0 Then Es = 200000
            If Not Single.TryParse(tbRec.Text, rec) OrElse rec < 0 Then rec = 0.04F
            Dim thetaRad As Single = CSng(nudAngulo.Value) * CSng(Math.PI) / 180.0F

            If _tipoSec = TipoSeccion10.Rectangular Then
                Dim secB As Single, secH As Single
                If Not Single.TryParse(tbB.Text, secB) OrElse Not Single.TryParse(tbH.Text, secH) Then Return
                _estadoSec = CalcularEjeNeutroDadoP(Pu, TipoSeccion10.Rectangular,
                                                    secB, secH, rec, _barras, fc, fy, Es, thetaRad)
            Else
                Dim secD As Single
                If Not Single.TryParse(tbD.Text, secD) Then Return
                _estadoSec = CalcularEjeNeutroDadoP(Pu, TipoSeccion10.Circular,
                                                    secD, secD, rec, _barras, fc, fy, Es, thetaRad)
            End If

            ActualizarTarjetaResultadoC()
        Catch ex As Exception
            Logger.Error(ex, "Form_10_AnalisisSeccion_V2.RecalcularEstadoConPu", "Pu=" & Pu.ToString("F1"))
        End Try
    End Sub

    Private Sub ActualizarTarjetaResultadoC()
        If _estadoSec Is Nothing Then
            lblResultadoC.Text = "Ingrese Pu y presione ""Calcular c(Pu)""."
            lblResultadoC.BackColor = Color.FromArgb(250, 245, 240)
            Return
        End If
        If Not _estadoSec.Convergio Then
            lblResultadoC.Text = "⚠ " & _estadoSec.Mensaje
            lblResultadoC.BackColor = Color.FromArgb(255, 235, 230)
            lblResultadoC.ForeColor = Color.FromArgb(140, 30, 0)
            Return
        End If
        Dim s = _estadoSec
        lblResultadoC.Text = String.Format(
            "c   = {0:F4} m  ({1:F2} cm)" & vbCrLf &
            "a   = β₁·c = {2:F4} m  ({3:F2} cm)   β₁={4:F3}" & vbCrLf &
            "Pn  = {5:F1} kN     Mn = {6:F2} kN·m" & vbCrLf &
            "εt  = {7:F5}   ({8})",
            s.c, s.c * 100,
            s.a, s.a * 100, s.Beta1,
            s.Pn, s.Mn,
            s.Es_extrema_tracc, s.Zona)
        lblResultadoC.BackColor = Color.FromArgb(240, 250, 245)
        lblResultadoC.ForeColor = Color.FromArgb(30, 60, 40)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  DIBUJO DE LA SECCIÓN — delega en SeccionDibujoPro
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnPaintSec(sender As Object, e As PaintEventArgs)
        Dim ctx As New SeccionDibujoPro.DibujoCtx()
        ctx.TipoSec = _tipoSec
        Single.TryParse(tbB.Text, ctx.B) : Single.TryParse(tbH.Text, ctx.H)
        Single.TryParse(tbD.Text, ctx.D) : Single.TryParse(tbRec.Text, ctx.Rec)
        Single.TryParse(tbFc.Text, ctx.Fc) : Single.TryParse(tbFy.Text, ctx.Fy)
        Dim EsV As Single = 200000 : Single.TryParse(tbEs.Text, EsV)
        ctx.Ey = If(EsV > 0, ctx.Fy / EsV, 0.0021F)
        ctx.ThetaRad = CSng(nudAngulo.Value) * CSng(Math.PI) / 180.0F
        ctx.Barras = _barras
        ctx.Estado = _estadoSec
        If _datosDI IsNot Nothing Then
            ctx.Ag_cm2 = _datosDI.Ag_cm2
            ctx.Ast_cm2 = _datosDI.Ast_cm2
            ctx.Rho_pct = _datosDI.rho_pct
        End If
        ctx.MostrarHatching = cbHatch.Checked
        ctx.MostrarBloqueWhitney = cbWhitney.Checked
        ctx.MostrarEjeNeutro = cbEN.Checked
        ctx.MostrarCotas = cbCotas.Checked
        ctx.MostrarTagsBarras = cbTagsBar.Checked
        ctx.MostrarEjesCoord = cbEjesXY.Checked
        ctx.MostrarAngulo = cbAngulo.Checked
        ctx.MostrarLeyenda = cbLeyenda.Checked
        ctx.Zoom = _zoomSec
        ctx.PanX = _panXSec
        ctx.PanY = _panYSec

        SeccionDibujoPro.Dibujar(e.Graphics, picSec.Width, picSec.Height,
                                  ctx, _cxSec, _cySec, _escSec)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  TAB DEFORMACIONES — perfil ε(profundidad) con las barras marcadas
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnPaintDef(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.Clear(Color.White)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim w = picDef.Width, h = picDef.Height

        If _estadoSec Is Nothing OrElse Not _estadoSec.Convergio Then
            Dim fntMsg = New Font("Segoe UI", 9.5F, FontStyle.Italic)
            Dim msg = If(_estadoSec Is Nothing,
                          "Ingrese Pu y presione ""Calcular c(Pu)"" para ver el perfil ε(d).",
                          "⚠ " & _estadoSec.Mensaje)
            Dim sz = g.MeasureString(msg, fntMsg)
            g.DrawString(msg, fntMsg, Brushes.Gray, (w - sz.Width) / 2, (h - sz.Height) / 2)
            Return
        End If

        Dim s = _estadoSec
        Dim mL = 90, mR = 46, mT = 40, mB = 62
        Dim pw = w - mL - mR, ph = h - mT - mB
        If pw < 60 OrElse ph < 60 Then Return

        ' Eje X = deformación ε; eje Y = profundidad d desde fibra extrema comprimida (0 arriba, 2·projMax abajo)
        Dim projMax = s.ProjMax
        Dim dMax As Single = 2.0F * projMax
        Dim ey As Single = 0.0021F
        Try
            Dim fyV As Single, EsV As Single
            Single.TryParse(tbFy.Text, fyV) : Single.TryParse(tbEs.Text, EsV)
            If EsV > 0 AndAlso fyV > 0 Then ey = fyV / EsV
        Catch
        End Try

        ' Rango de ε: desde εs mínima (más traccionada, negativa) hasta 0.003 (compresión máxima)
        Dim epsMin As Single = Math.Min(-Math.Abs(s.Es_extrema_tracc) * 1.2F, -0.005F)
        Dim epsMax As Single = 0.0035F
        Dim eRng = epsMax - epsMin

        Dim toX = Function(eps As Single) CSng(mL + (eps - epsMin) / eRng * pw)
        Dim toY = Function(d As Single) CSng(mT + d / dMax * ph)   ' d = profundidad desde fibra sup

        ' Grid
        Dim gPen = New Pen(Color.FromArgb(220, 225, 238))
        For ix = 0 To 6
            Dim xx = CSng(mL + ix / 6.0 * pw)
            g.DrawLine(gPen, xx, mT, xx, mT + ph)
        Next
        For iy = 0 To 6
            Dim yy = CSng(mT + iy / 6.0 * ph)
            g.DrawLine(gPen, mL, yy, mL + pw, yy)
        Next

        ' Zona εy verticales
        g.DrawLine(New Pen(Color.SeaGreen, 1) With {.DashStyle = DashStyle.DashDot},
                   toX(ey), mT, toX(ey), mT + ph)
        g.DrawLine(New Pen(Color.SeaGreen, 1) With {.DashStyle = DashStyle.DashDot},
                   toX(-ey), mT, toX(-ey), mT + ph)
        ' Línea εcu=0.003
        g.DrawLine(New Pen(Color.FromArgb(180, 40, 40), 1) With {.DashStyle = DashStyle.Dash},
                   toX(0.003F), mT, toX(0.003F), mT + ph)
        ' Línea ε=0 (posición del eje neutro)
        g.DrawLine(New Pen(Color.Black, 1) With {.DashStyle = DashStyle.Dot},
                   toX(0), mT, toX(0), mT + ph)

        ' Línea del perfil lineal ε(d) = 0.003·(c − d)/c → ε=0 en d=c
        Dim eArriba As Single = 0.003F
        Dim eAbajo As Single = If(s.c > 0.0001F, 0.003F * (s.c - dMax) / s.c, 0)
        g.DrawLine(New Pen(Color.FromArgb(0, 82, 164), 2.4F),
                   toX(eArriba), toY(0), toX(eAbajo), toY(dMax))

        ' Barras: cada (εs, d)
        Dim fntB As New Font("Segoe UI", 8F, FontStyle.Bold)
        Dim brT As New SolidBrush(Color.FromArgb(30, 30, 40))
        For i = 0 To _barras.Count - 1
            Dim bar = _barras(i)
            Dim sinT = CSng(Math.Sin(s.ThetaRad)), cosT = CSng(Math.Cos(s.ThetaRad))
            Dim p_bar = bar.Coordenada_X * sinT + bar.Coordenada_Y * cosT
            Dim di As Single = projMax - p_bar   ' profundidad desde fibra sup
            Dim eps_i As Single = 0
            If s.Es_por_barra IsNot Nothing AndAlso i < s.Es_por_barra.Length Then eps_i = s.Es_por_barra(i)
            Dim xx = toX(eps_i), yy = toY(di)
            Dim col As Color
            If eps_i <= -ey Then
                col = Color.FromArgb(200, 40, 40)
            ElseIf eps_i >= ey Then
                col = Color.FromArgb(0, 100, 200)
            Else
                col = Color.FromArgb(120, 130, 140)
            End If
            g.FillEllipse(New SolidBrush(col), xx - 5, yy - 5, 10, 10)
            g.DrawEllipse(Pens.Black, xx - 5, yy - 5, 10, 10)
            g.DrawString(bar.Name_Barra, fntB, brT, xx + 8, yy - 8)
        Next

        ' Marca del eje neutro (d = c)
        Dim yEN = toY(s.c)
        g.DrawLine(New Pen(Color.FromArgb(200, 40, 40), 1.6F) With {.DashStyle = DashStyle.DashDot},
                   mL, yEN, mL + pw, yEN)
        g.DrawString(String.Format("EN:  d = c = {0:F3} m", s.c),
                     fntB, New SolidBrush(Color.FromArgb(200, 40, 40)), mL + pw - 140, yEN - 14)

        ' Ejes
        g.DrawLine(New Pen(Color.Black, 1.5F), mL, mT, mL, mT + ph)
        g.DrawLine(New Pen(Color.Black, 1.5F), mL, mT + ph, mL + pw, mT + ph)

        ' Ticks X
        Dim fnt = New Font("Segoe UI", 8.5F)
        Dim sfR As New StringFormat With {.Alignment = StringAlignment.Far}
        Dim sfC As New StringFormat With {.Alignment = StringAlignment.Center}
        For ix = 0 To 6
            Dim eps = epsMin + eRng * ix / 6
            Dim xx = toX(eps)
            g.DrawLine(Pens.Black, xx, mT + ph, xx, mT + ph + 4)
            g.DrawString(eps.ToString("F4"), fnt, Brushes.Black,
                          New RectangleF(xx - 30, mT + ph + 6, 60, 15), sfC)
        Next
        g.DrawString("ε (deformación,  + comp,  − tracc)", New Font("Segoe UI", 10F, FontStyle.Bold),
                     Brushes.Black, CSng(mL + pw / 2 - 100), h - 26)

        ' Ticks Y (profundidad d)
        For iy = 0 To 6
            Dim d = dMax * iy / 6
            Dim yy = toY(d)
            g.DrawLine(Pens.Black, mL, yy, mL - 4, yy)
            g.DrawString(d.ToString("F3") & " m", fnt, Brushes.Black,
                         New RectangleF(2, yy - 8, mL - 8, 16), sfR)
        Next
        Dim st = g.Save()
        g.TranslateTransform(14, mT + ph / 2)
        g.RotateTransform(-90)
        g.DrawString("Profundidad desde fibra sup.", New Font("Segoe UI", 10F, FontStyle.Bold),
                     Brushes.Black, 0, 0, sfC)
        g.Restore(st)

        ' Leyenda
        Dim yL As Single = mT + 8, xL As Single = mL + 8
        g.DrawString(String.Format("Pu = {0:F1} kN     Pn(equil) = {1:F1} kN     Mn = {2:F2} kN·m",
                                    s.P_solicitado, s.Pn, s.Mn),
                     New Font("Segoe UI", 8.5F, FontStyle.Bold), brT, xL, yL)
        g.DrawString(s.Zona, New Font("Segoe UI", 8.5F, FontStyle.Italic),
                     New SolidBrush(Color.FromArgb(80, 80, 100)), xL, yL + 16)
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

        Dim gPen = New Pen(Color.FromArgb(220, 225, 238))
        For ix = 0 To 4
            g.DrawLine(gPen, CSng(mL + ix / 4.0 * pw), mT, CSng(mL + ix / 4.0 * pw), mT + ph)
        Next
        For iy = 0 To 5
            g.DrawLine(gPen, mL, CSng(mT + iy / 5.0 * ph), mL + pw, CSng(mT + iy / 5.0 * ph))
        Next

        If _datosDI.PhiMn.Count > 2 Then
            Dim polyPts As New List(Of PointF)
            For i = 0 To _datosDI.PhiMn.Count - 1
                polyPts.Add(New PointF(toX(_datosDI.PhiMn(i)), toY(_datosDI.PhiPn(i))))
            Next
            polyPts.Add(New PointF(mL, toY(_datosDI.PhiPn.Last())))
            polyPts.Add(New PointF(mL, toY(_datosDI.PhiPn(0))))
            g.FillPolygon(New SolidBrush(Color.FromArgb(20, 0, 100, 200)), polyPts.ToArray())
        End If

        Dim ptsPn(_datosDI.Mn.Count - 1) As PointF
        For i = 0 To _datosDI.Mn.Count - 1
            ptsPn(i) = New PointF(toX(_datosDI.Mn(i)), toY(_datosDI.Pn(i)))
        Next
        g.DrawLines(New Pen(Color.Silver, 1.5F) With {.DashStyle = DashStyle.Dash}, ptsPn)

        Dim ptsPhi(_datosDI.PhiMn.Count - 1) As PointF
        For i = 0 To _datosDI.PhiMn.Count - 1
            ptsPhi(i) = New PointF(toX(_datosDI.PhiMn(i)), toY(_datosDI.PhiPn(i)))
        Next
        g.DrawLines(New Pen(Color.FromArgb(0, 82, 164), 2.5F), ptsPhi)

        If _datosDI.M_bal > 0 Then
            Dim bx = toX(_datosDI.M_bal), by = toY(_datosDI.P_bal)
            g.FillEllipse(Brushes.OrangeRed, bx - 6, by - 6, 12, 12)
            g.DrawEllipse(Pens.DarkRed, bx - 6, by - 6, 12, 12)
            g.DrawString("Bal.", New Font("Segoe UI", 8.5F), Brushes.OrangeRed, bx + 8, by - 10)
        End If

        ' Punto (Pn, Mn) del estado calculado con Pu (verde)
        If _estadoSec IsNot Nothing AndAlso _estadoSec.Convergio Then
            Dim ex = toX(_estadoSec.Mn), ey2 = toY(_estadoSec.Pn)
            g.FillEllipse(New SolidBrush(Color.FromArgb(0, 160, 80)), ex - 7, ey2 - 7, 14, 14)
            g.DrawEllipse(Pens.DarkGreen, ex - 7, ey2 - 7, 14, 14)
            g.DrawString(String.Format("Pu → c={0:F3} m", _estadoSec.c),
                         New Font("Segoe UI", 9F, FontStyle.Bold),
                         New SolidBrush(Color.FromArgb(0, 130, 60)), ex + 10, ey2 - 10)
        End If

        If Not Single.IsNaN(_demandaP) AndAlso Not Single.IsNaN(_demandaM) Then
            Dim phiMnAtP = InterpolatePhiMnAtP(_demandaP)
            Dim cd As Single = 0
            If _demandaM > 0.01F AndAlso phiMnAtP > 0.01F Then
                cd = phiMnAtP / _demandaM
            ElseIf _demandaM <= 0.01F AndAlso _demandaP > 0.01F AndAlso _datosDI.PhiP0 > 0.01F Then
                cd = _datosDI.PhiP0 / _demandaP
            End If
            Dim cumple = cd >= 1.0F
            Dim dotClr = If(cumple, Color.FromArgb(0, 160, 80), Color.FromArgb(210, 40, 40))
            Dim dxP = toX(_demandaM), dyP = toY(_demandaP)
            g.FillEllipse(New SolidBrush(dotClr), dxP - 8, dyP - 8, 16, 16)
            g.DrawEllipse(New Pen(Color.Black, 1.5F), dxP - 8, dyP - 8, 16, 16)
            g.DrawLine(New Pen(dotClr, 1.5F) With {.DashStyle = DashStyle.Dot}, dxP, dyP, dxP, mT + ph)
            g.DrawLine(New Pen(dotClr, 1.5F) With {.DashStyle = DashStyle.Dot}, mL, dyP, dxP, dyP)
            Dim fntCD = New Font("Segoe UI", 10F, FontStyle.Bold)
            g.DrawString(String.Format("C/D = {0:F3}", cd), fntCD, New SolidBrush(dotClr), dxP + 12, dyP - 14)
        End If

        Dim axPen = New Pen(Color.FromArgb(50, 50, 50), 1.5F)
        g.DrawLine(axPen, mL, mT, mL, mT + ph)
        g.DrawLine(axPen, mL, mT + ph, mL + pw, mT + ph)

        Dim fnt = New Font("Segoe UI", 9.5F)
        Dim fntB = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Dim fntTitle = New Font("Segoe UI", 10F, FontStyle.Bold)
        Dim sfR As New StringFormat With {.Alignment = StringAlignment.Far}

        For ix = 0 To 4
            Dim m = maxM * ix / 4
            Dim xx = CSng(mL + m / maxM * pw)
            g.DrawLine(Pens.Black, xx, mT + ph, xx, mT + ph + 5)
            g.DrawString(String.Format("{0:F0}", m), fnt, Brushes.Black, xx - 18, mT + ph + 8)
        Next
        g.DrawString("Momento  M  (kN·m)", fntTitle, Brushes.Black, CSng(mL + pw / 2 - 70), hPx - 24)

        For iy = 0 To 5
            Dim p = minP + pRng * iy / 5
            Dim yy = toY(p)
            g.DrawLine(Pens.Black, mL, yy, mL - 5, yy)
            g.DrawString(String.Format("{0:F0}", p), fnt, Brushes.Black,
                         New RectangleF(2, yy - 10, mL - 9, 20), sfR)
        Next

        Dim st = g.Save()
        g.TranslateTransform(14, mT + ph / 2)
        g.RotateTransform(-90)
        g.DrawString("Fuerza axial  P  (kN)", fntTitle, Brushes.Black, 0, 0,
                     New StringFormat With {.Alignment = StringAlignment.Center})
        g.Restore(st)

        g.DrawString(String.Format("P₀ = {0:F0} kN   φP₀ = {1:F0} kN",
                     _datosDI.P0, _datosDI.PhiP0), fntB, Brushes.DarkBlue, mL + 4, mT + 4)

        Dim lx = mL + pw - 145, ly = mT + 10
        g.DrawLine(New Pen(Color.Silver, 1.5F) With {.DashStyle = DashStyle.Dash}, lx, ly, lx + 22, ly)
        g.DrawString("Nominal", fnt, Brushes.Gray, lx + 26, ly - 7)
        g.DrawLine(New Pen(Color.FromArgb(0, 82, 164), 2.5F), lx, ly + 18, lx + 22, ly + 18)
        g.DrawString("Reducido  φ", fnt, New SolidBrush(Color.FromArgb(0, 82, 164)), lx + 26, ly + 11)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  DIAGRAMA M-κ
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

        ' Rango: incluir Ku aunque quede fuera del último punto del barrido,
        ' para que la marca de κu no salga del área trazable.
        Dim kMaxData = _datosMK.Curvaturas.Max()
        Dim kBase = Math.Max(kMaxData, _datosMK.Ku)
        Dim maxK = kBase * 1.12F : If maxK < 0.0001F Then maxK = 0.01F
        Dim mMaxData = _datosMK.Momentos.Max()
        Dim mBase = Math.Max(mMaxData, Math.Max(_datosMK.My, _datosMK.Mu))
        Dim maxM = mBase * 1.18F : If maxM < 1 Then maxM = 1

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

        ' Zona plástica (entre κy y κu) — relleno suave naranja
        If _datosMK.Ky > 0 AndAlso _datosMK.Ku > _datosMK.Ky Then
            Dim kyX = toX(Math.Min(_datosMK.Ky, maxK / 1.12F))
            Dim kuX = toX(Math.Min(_datosMK.Ku, maxK / 1.12F))
            g.FillRectangle(New SolidBrush(Color.FromArgb(18, 255, 140, 0)),
                            kyX, mT, kuX - kyX, ph)
        End If

        ' Curva M-κ (azul sólido). Prepende el origen (0,0) — físicamente
        ' M-κ arranca ahí y sin este punto el primer valor del barrido
        ' (κ≈0.003) puede caer con M ya alto, dejando el fondo del gráfico
        ' vacío y haciendo ver la curva como una franja compacta arriba.
        Dim n = _datosMK.Curvaturas.Count
        Dim pts(n) As PointF
        pts(0) = New PointF(toX(0), toY(0))
        For i = 0 To n - 1
            pts(i + 1) = New PointF(toX(_datosMK.Curvaturas(i)), toY(_datosMK.Momentos(i)))
        Next
        g.DrawLines(New Pen(Color.FromArgb(0, 82, 164), 2.5F), pts)

        Dim fnt = New Font("Segoe UI", 9.5F)
        Dim fntB = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Dim fntTitle = New Font("Segoe UI", 10F, FontStyle.Bold)

        ' Punto κy (verde) con líneas guía
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

        ' Punto κu (naranja) con línea guía al eje X
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

        ' Caja de ductilidad μφ + fracción κu/κy + causa de falla
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

        ' Etiqueta Y rotada
        Dim st = g.Save()
        g.TranslateTransform(14, mT + ph / 2)
        g.RotateTransform(-90)
        g.DrawString("Momento  M  (kN·m)", fntTitle, Brushes.Black, 0, 0,
                     New StringFormat With {.Alignment = StringAlignment.Center})
        g.Restore(st)

        ' Leyenda "Zona plástica" en la esquina superior izquierda del área
        If _datosMK.Ductilidad > 0 Then
            g.FillRectangle(New SolidBrush(Color.FromArgb(60, 255, 140, 0)),
                            mL + 4, mT + 6, 16, 12)
            g.DrawString("Zona plástica  (κy → κu)", fnt, Brushes.DarkOrange, mL + 24, mT + 4)
        End If
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  RESULTADOS NUMÉRICOS
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
        add("Ast", String.Format("{0:F3}", _datosDI.Ast_cm2), "cm²")
        add("ρ", String.Format("{0:F3}", _datosDI.rho_pct), "%")
        add("N° de barras", CStr(If(_barras IsNot Nothing, _barras.Count, 0)), "")
        addSep("  DIAGRAMA P-M (NOMINAL)")
        add("P₀", String.Format("{0:F1}", _datosDI.P0), "kN")
        add("φP₀", String.Format("{0:F1}", _datosDI.PhiP0), "kN")
        add("P_bal", String.Format("{0:F1}", _datosDI.P_bal), "kN")
        add("M_bal", String.Format("{0:F2}", _datosDI.M_bal), "kN·m")
        add("P_mín (tracc. pura)", String.Format("{0:F1}", _datosDI.Pmin), "kN")

        If _estadoSec IsNot Nothing AndAlso _estadoSec.Convergio Then
            addSep("  EJE NEUTRO POR Pu")
            add("Pu solicitado", String.Format("{0:F1}", _estadoSec.P_solicitado), "kN")
            add("c", String.Format("{0:F4}", _estadoSec.c), "m")
            add("a = β₁·c", String.Format("{0:F4}", _estadoSec.a), "m")
            add("β₁", String.Format("{0:F3}", _estadoSec.Beta1), "")
            add("Pn (equilibrio)", String.Format("{0:F1}", _estadoSec.Pn), "kN")
            add("Mn", String.Format("{0:F2}", _estadoSec.Mn), "kN·m")
            add("εt (barra extrema)", String.Format("{0:F5}", _estadoSec.Es_extrema_tracc), "m/m")
            add("Zona", _estadoSec.Zona, "")
        End If

        addSep("  MANDER 1988")
        add("f'cc", String.Format("{0:F2}", _fcc), "MPa")
        add("ε_cc", String.Format("{0:F5}", _eps_cc), "m/m")
        add("ε_cu*", String.Format("{0:F5}", _eps_cu), "m/m")

        If _datosMK IsNot Nothing AndAlso _datosMK.Curvaturas.Count > 0 Then
            addSep("  M-κ")
            add("κ_y", String.Format("{0:F5}", _datosMK.Ky), "1/m")
            add("My", String.Format("{0:F2}", _datosMK.My), "kN·m")
            add("κ_u", String.Format("{0:F5}", _datosMK.Ku), "1/m")
            add("Mu", String.Format("{0:F2}", _datosMK.Mu), "kN·m")
            add("μφ", String.Format("{0:F3}", _datosMK.Ductilidad), "κu/κy")
        End If
    End Sub

    Private Sub MensajeSinDatos(g As Graphics, w As Integer, h As Integer, msg As String)
        Dim fnt = New Font("Segoe UI", 9, FontStyle.Italic)
        Dim sz = g.MeasureString(msg, fnt)
        g.DrawString(msg, fnt, Brushes.Gray, (w - sz.Width) / 2, (h - sz.Height) / 2)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    '  VERIFICACIÓN P, M (φ) — como en v1
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub OnVerificarClick(sender As Object, e As EventArgs)
        Dim p As Single, m As Single
        If Not Single.TryParse(tbDemandaP.Text, p) Then
            MessageBox.Show("P demanda inválido.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
        End If
        If Not Single.TryParse(tbDemandaM.Text, m) OrElse m < 0 Then
            MessageBox.Show("M demanda inválido (≥0).", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning) : Return
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
    '  PERSISTENCIA — Guardar / Abrir sección
    ' ══════════════════════════════════════════════════════════════════════
    Private _archivoActual As String = Nothing

    Private Sub OnGuardarSeccion_Click(sender As Object, e As EventArgs)
        Try
            Dim snap = CapturarSnapshot()
            Using sfd As New SaveFileDialog()
                sfd.Title = "Guardar sección"
                sfd.InitialDirectory = SeccionV2Snapshot.CarpetaPorDefecto()
                sfd.Filter = SeccionV2Snapshot.FiltroDialogo
                sfd.DefaultExt = SeccionV2Snapshot.Extension.TrimStart("."c)
                sfd.AddExtension = True
                If _archivoActual IsNot Nothing Then
                    sfd.FileName = System.IO.Path.GetFileName(_archivoActual)
                Else
                    sfd.FileName = SugerirNombreArchivo()
                End If
                If sfd.ShowDialog(Me) <> DialogResult.OK Then Return
                snap.Guardar(sfd.FileName)
                _archivoActual = sfd.FileName
                ActualizarTituloVentana()
                MessageBox.Show("Sección guardada:" & vbCrLf & sfd.FileName,
                                "Guardar sección", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Using
        Catch ex As Exception
            Logger.Error(ex, "Form_10_AnalisisSeccion_V2.OnGuardarSeccion", "Error al guardar")
            MessageBox.Show("No se pudo guardar la sección:" & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnAbrirSeccion_Click(sender As Object, e As EventArgs)
        Try
            Using ofd As New OpenFileDialog()
                ofd.Title = "Abrir sección"
                ofd.InitialDirectory = SeccionV2Snapshot.CarpetaPorDefecto()
                ofd.Filter = SeccionV2Snapshot.FiltroDialogo
                If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim snap = SeccionV2Snapshot.Cargar(ofd.FileName)
                AplicarSnapshot(snap)
                _archivoActual = ofd.FileName
                ActualizarTituloVentana()
                Calcular()
            End Using
        Catch ex As Exception
            Logger.Error(ex, "Form_10_AnalisisSeccion_V2.OnAbrirSeccion", "Error al abrir")
            MessageBox.Show("No se pudo abrir la sección:" & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Captura el estado actual de la UI en un Snapshot serializable
    Private Function CapturarSnapshot() As SeccionV2Snapshot
        Dim s As New SeccionV2Snapshot()
        s.NombreSeccion = If(_archivoActual IsNot Nothing,
                              System.IO.Path.GetFileNameWithoutExtension(_archivoActual), "")
        s.TipoSecInt = CInt(_tipoSec)
        Single.TryParse(tbB.Text, s.B)
        Single.TryParse(tbH.Text, s.H)
        Single.TryParse(tbD.Text, s.D)
        Single.TryParse(tbRec.Text, s.Rec)
        Single.TryParse(tbFc.Text, s.Fc)
        Single.TryParse(tbFy.Text, s.Fy)
        Single.TryParse(tbEs.Text, s.Es)

        Dim esp = GetEspec()
        s.BarraEsquinas = esp.BarraEsquinas
        s.BarraSuperior = esp.BarraSuperior
        s.NSuperior = esp.NSuperior
        s.BarraInferior = esp.BarraInferior
        s.NInferior = esp.NInferior
        s.BarraLateral = esp.BarraLateral
        s.NLateralPorCara = esp.NLateralPorCara

        s.NCircular = CInt(nudNCirc.Value)
        s.BarraCircular = CStr(If(cbBarraCirc.SelectedItem, "#5"))

        s.BarraEstribo = CStr(If(cbBarraEst.SelectedItem, "#3"))
        Single.TryParse(tbSEst.Text, s.SEstribo)
        s.RamasB = CInt(nudRamasB.Value)
        s.RamasH = CInt(nudRamasH.Value)
        Single.TryParse(tbEpsSu.Text, s.EpsSu)

        Single.TryParse(tbP.Text, s.P_MK)
        s.AnguloGrados = CInt(nudAngulo.Value)
        Single.TryParse(tbPuNominal.Text, s.Pu_Nominal)
        If Not Single.TryParse(tbDemandaP.Text, s.Demanda_P) Then s.Demanda_P = Single.NaN
        If Not Single.TryParse(tbDemandaM.Text, s.Demanda_M) Then s.Demanda_M = Single.NaN

        ' Barras individuales — copia profunda del estado actual
        If _barras IsNot Nothing Then
            s.Barras = New List(Of RefuerzoSimple)
            For Each b In _barras
                s.Barras.Add(New RefuerzoSimple(b.Id_Patron, b.Name_Barra, b.Db, b.Asb,
                                                 b.Coordenada_X, b.Coordenada_Y))
            Next
        End If
        Return s
    End Function

    ' Vuelca un Snapshot cargado sobre los controles de la UI
    Private Sub AplicarSnapshot(s As SeccionV2Snapshot)
        If s Is Nothing Then Return

        ' Tipo de sección → dispara OnTipoChanged
        rbRect.Checked = (s.TipoSecInt = 0)
        rbCirc.Checked = (s.TipoSecInt = 1)
        _tipoSec = If(rbRect.Checked, TipoSeccion10.Rectangular, TipoSeccion10.Circular)

        tbB.Text = s.B.ToString("F3")
        tbH.Text = s.H.ToString("F3")
        tbD.Text = s.D.ToString("F3")
        tbRec.Text = s.Rec.ToString("F3")
        tbFc.Text = s.Fc.ToString("F0")
        tbFy.Text = s.Fy.ToString("F0")
        tbEs.Text = s.Es.ToString("F0")

        ' dgvRef
        Try
            dgvRef.Rows(0).Cells(2).Value = s.BarraEsquinas
            dgvRef.Rows(1).Cells(1).Value = s.NSuperior.ToString()
            dgvRef.Rows(1).Cells(2).Value = s.BarraSuperior
            dgvRef.Rows(2).Cells(1).Value = s.NInferior.ToString()
            dgvRef.Rows(2).Cells(2).Value = s.BarraInferior
            dgvRef.Rows(3).Cells(1).Value = s.NLateralPorCara.ToString()
            dgvRef.Rows(3).Cells(2).Value = s.BarraLateral
        Catch
        End Try

        Try
            nudNCirc.Value = Math.Max(nudNCirc.Minimum, Math.Min(nudNCirc.Maximum, s.NCircular))
            Dim idxC = cbBarraCirc.Items.IndexOf(s.BarraCircular)
            If idxC >= 0 Then cbBarraCirc.SelectedIndex = idxC
        Catch
        End Try

        Try
            Dim idxE = cbBarraEst.Items.IndexOf(s.BarraEstribo)
            If idxE >= 0 Then cbBarraEst.SelectedIndex = idxE
            tbSEst.Text = s.SEstribo.ToString("F3")
            nudRamasB.Value = Math.Max(nudRamasB.Minimum, Math.Min(nudRamasB.Maximum, s.RamasB))
            nudRamasH.Value = Math.Max(nudRamasH.Minimum, Math.Min(nudRamasH.Maximum, s.RamasH))
            tbEpsSu.Text = s.EpsSu.ToString("F3")
        Catch
        End Try

        tbP.Text = s.P_MK.ToString("F1")
        Try
            nudAngulo.Value = Math.Max(nudAngulo.Minimum, Math.Min(nudAngulo.Maximum, s.AnguloGrados))
        Catch
        End Try
        tbPuNominal.Text = s.Pu_Nominal.ToString("F1")
        tbDemandaP.Text = If(Single.IsNaN(s.Demanda_P), "", s.Demanda_P.ToString("F1"))
        tbDemandaM.Text = If(Single.IsNaN(s.Demanda_M), "", s.Demanda_M.ToString("F1"))

        ' Barras individuales — respetar el snapshot completo. Si no hay
        ' barras guardadas, Calcular() las regenera desde la espec.
        If s.Barras IsNot Nothing AndAlso s.Barras.Count > 0 Then
            _barras = New List(Of RefuerzoSimple)
            For Each b In s.Barras
                _barras.Add(New RefuerzoSimple(b.Id_Patron, b.Name_Barra, b.Db, b.Asb,
                                                b.Coordenada_X, b.Coordenada_Y))
            Next
            ' Firma actual para que Calcular no regenere y respete las barras cargadas
            _lastLayoutSig = ComputeLayoutSig10()
        Else
            _lastLayoutSig = Nothing
        End If

        OnTipoChanged(Nothing, EventArgs.Empty)
        _estadoSec = Nothing
        lblResultadoC.Text = "Ingrese Pu y presione ""Calcular c(Pu)""."
    End Sub

    Private Sub ActualizarTituloVentana()
        Dim titulo As String = "ARCO — Análisis de Sección 2.0 (Pro)"
        If _archivoActual IsNot Nothing Then
            titulo &= "  —  " & System.IO.Path.GetFileName(_archivoActual)
        End If
        Me.Text = titulo
    End Sub

    Private Function SugerirNombreArchivo() As String
        Dim tag As String = If(_tipoSec = TipoSeccion10.Rectangular,
            String.Format("R_{0:F0}x{1:F0}",
                CSng(GetSingle(tbB) * 100), CSng(GetSingle(tbH) * 100)),
            String.Format("C_D{0:F0}", CSng(GetSingle(tbD) * 100)))
        Return tag & "_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".arcosec"
    End Function

    Private Function GetSingle(tb As TextBox) As Single
        Dim v As Single = 0
        Single.TryParse(tb.Text, v)
        Return v
    End Function

End Class

' ─────────────────────────────────────────────────────────────────────────
'  PictureBox subclase-selectable — permite recibir MouseWheel sin necesidad
'  de un clic previo para tomar foco. Al entrar el cursor la caja se enfoca.
' ─────────────────────────────────────────────────────────────────────────
Friend Class PicSecFoco
    Inherits PictureBox

    Public Sub New()
        Me.SetStyle(ControlStyles.Selectable, True)
        Me.TabStop = True
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        If Not Me.Focused Then Me.Focus()
    End Sub
End Class
