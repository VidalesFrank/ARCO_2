Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_02_Columnas
Imports System.Drawing.Drawing2D

Public Class Form_02_02_DiagramaColumna
    Inherits Form

    Private ReadOnly Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Const FY As Single = 420.0F
    Private Const ES As Single = 200000.0F
    Private Const REC As Single = 0.05F

    Private Shared ReadOnly BarNombres As String() = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}
    Private Shared ReadOnly BarAreas As Single() = {31.67F, 71.26F, 126.7F, 198.1F, 284.5F, 387.1F, 509.7F, 819.4F}

    ' ── Estado ──────────────────────────────────────────────────
    Private _tramo As Tramo_Columna = Nothing
    Private _estacion As String = "Top"
    Private _combos As List(Of (Nombre As String, Pu As Single, M3u As Single, M2u As Single, DC As Single))
    Private _barrasEdicion As New List(Of Tramo_Columna.Barra_Seccion)

    ' ── Edición gráfica ─────────────────────────────────────────
    Private _selIdx As Integer = -1
    Private _dragging As Boolean = False
    Private _dragOfsX As Single, _dragOfsY As Single
    Private _scalePic As Single = 1
    Private _cxPic As Single, _cyPic As Single
    Private _rightClkPt As Point

    ' ── Controles top ───────────────────────────────────────────
    Private ReadOnly panelTop As New Panel()
    Private ReadOnly lblCol As New Label()
    Private ReadOnly cmbCol As New ComboBox()
    Private ReadOnly lblTramo As New Label()
    Private ReadOnly cmbTramo As New ComboBox()
    Private ReadOnly lblEst As New Label()
    Private ReadOnly cmbEst As New ComboBox()
    Private ReadOnly btnCalc As New Button()
    Private ReadOnly btnVer3D As New Button()

    ' ── Panel izquierdo ─────────────────────────────────────────
    Private ReadOnly panelLeft As New Panel()
    Private ReadOnly lblSecTitulo As New Label()
    Private ReadOnly picSeccion As New PictureBox()
    Private ReadOnly lblDims As New Label()
    Private ReadOnly lblCuantia As New Label()

    ' Patrón
    Private ReadOnly panelPatron As New Panel()
    Private ReadOnly cmbEsquina As New ComboBox()
    Private ReadOnly nudCaraB As New NumericUpDown()
    Private ReadOnly cmbCaraB As New ComboBox()
    Private ReadOnly nudCaraH As New NumericUpDown()
    Private ReadOnly cmbCaraH As New ComboBox()
    Private ReadOnly btnAplicarPatron As New Button()
    Private ReadOnly btnResetAuto As New Button()

    ' Edición barra seleccionada
    Private ReadOnly panelBarraEdit As New Panel()
    Private ReadOnly cmbBarraSel As New ComboBox()
    Private ReadOnly txtXSel As New TextBox()
    Private ReadOnly txtYSel As New TextBox()
    Private ReadOnly btnActSel As New Button()
    Private ReadOnly btnDelSel As New Button()
    Private ReadOnly lblSelInfo As New Label()

    ' Menú contextual
    Private ReadOnly ctxSec As New ContextMenuStrip()
    Private ReadOnly ctxItemAgregar As New ToolStripMenuItem("Agregar barra #4 aquí")
    Private ReadOnly ctxItemSimetrica As New ToolStripMenuItem("Agregar barra simétrica (espejo X)")
    Private ReadOnly ctxItemEliminar As New ToolStripMenuItem("Eliminar barra seleccionada")
    Private ReadOnly ctxItemReset As New ToolStripMenuItem("Restaurar distribución automática")

    ' ── Panel derecho ────────────────────────────────────────────
    Private ReadOnly panelRight As New Panel()
    Private ReadOnly lblM3Titulo As New Label()
    Private ReadOnly picDIM3 As New PictureBox()
    Private ReadOnly lblM2Titulo As New Label()
    Private ReadOnly picDIM2 As New PictureBox()
    Private ReadOnly panelResultados As New Panel()
    Private ReadOnly tablaResultados As New DataGridView()
    Private ReadOnly lblResumen As New Label()

    ' ══════════════════════════════════════════════════════════
    Public Sub New()
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Diagrama de Interacción Biaxial – Columnas"
        Me.Size = New Size(1210, 760)
        Me.MinimumSize = New Size(1000, 640)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 8.5F)

        BuildTopStrip()
        BuildLeftPanel()
        BuildRightPanel()

        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelLeft)
        Me.Controls.Add(panelRight)

        AddHandler Me.Resize, AddressOf Form_Resize
        AddHandler Me.Load, AddressOf Form_Load_Handler
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  TOP STRIP
    ' ─────────────────────────────────────────────────────────────
    Private Sub BuildTopStrip()
        panelTop.Dock = DockStyle.Top
        panelTop.Height = 44
        panelTop.BackColor = Color.FromArgb(240, 243, 248)

        Dim px As Integer = 10
        lblCol.Text = "Columna:" : lblCol.AutoSize = True : lblCol.Top = 14 : lblCol.Left = px
        panelTop.Controls.Add(lblCol) : px += lblCol.PreferredWidth + 4
        cmbCol.DropDownStyle = ComboBoxStyle.DropDownList
        cmbCol.Width = 140 : cmbCol.Top = 8 : cmbCol.Left = px : panelTop.Controls.Add(cmbCol) : px += 146

        lblTramo.Text = "Tramo:" : lblTramo.AutoSize = True : lblTramo.Top = 14 : lblTramo.Left = px
        panelTop.Controls.Add(lblTramo) : px += lblTramo.PreferredWidth + 4
        cmbTramo.DropDownStyle = ComboBoxStyle.DropDownList
        cmbTramo.Width = 100 : cmbTramo.Top = 8 : cmbTramo.Left = px : panelTop.Controls.Add(cmbTramo) : px += 106

        lblEst.Text = "Estación:" : lblEst.AutoSize = True : lblEst.Top = 14 : lblEst.Left = px
        panelTop.Controls.Add(lblEst) : px += lblEst.PreferredWidth + 4
        cmbEst.DropDownStyle = ComboBoxStyle.DropDownList
        cmbEst.Items.AddRange({"Top", "Bottom"})
        cmbEst.SelectedIndex = 0
        cmbEst.Width = 80 : cmbEst.Top = 8 : cmbEst.Left = px : panelTop.Controls.Add(cmbEst) : px += 86

        btnCalc.Text = "Calcular Diagrama"
        btnCalc.Size = New Size(140, 28) : btnCalc.Top = 8 : btnCalc.Left = px + 20
        btnCalc.BackColor = Color.FromArgb(30, 80, 160) : btnCalc.ForeColor = Color.White
        btnCalc.FlatStyle = FlatStyle.Flat : btnCalc.FlatAppearance.BorderSize = 0
        panelTop.Controls.Add(btnCalc)

        btnVer3D.Text = "Ver 3D"
        btnVer3D.Size = New Size(80, 28) : btnVer3D.Top = 8 : btnVer3D.Left = px + 20 + 148
        btnVer3D.BackColor = Color.FromArgb(60, 140, 90) : btnVer3D.ForeColor = Color.White
        btnVer3D.FlatStyle = FlatStyle.Flat : btnVer3D.FlatAppearance.BorderSize = 0
        btnVer3D.Enabled = False
        panelTop.Controls.Add(btnVer3D)

        AddHandler cmbCol.SelectedIndexChanged, AddressOf CmbCol_Changed
        AddHandler cmbTramo.SelectedIndexChanged, AddressOf CmbTramo_Changed
        AddHandler cmbEst.SelectedIndexChanged, AddressOf CmbEst_Changed
        AddHandler btnCalc.Click, AddressOf BtnCalc_Click
        AddHandler btnVer3D.Click, AddressOf BtnVer3D_Click
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  PANEL IZQUIERDO
    ' ─────────────────────────────────────────────────────────────
    Private Sub BuildLeftPanel()
        panelLeft.Width = 336
        panelLeft.BackColor = Color.FromArgb(248, 249, 252)

        lblSecTitulo.Text = "Sección Transversal"
        lblSecTitulo.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        lblSecTitulo.AutoSize = True : lblSecTitulo.Location = New Point(8, 6)
        panelLeft.Controls.Add(lblSecTitulo)

        picSeccion.BorderStyle = BorderStyle.FixedSingle
        picSeccion.BackColor = Color.White
        picSeccion.Location = New Point(8, 26)
        picSeccion.Size = New Size(318, 230)
        picSeccion.Cursor = Cursors.Hand
        picSeccion.ContextMenuStrip = ctxSec
        AddHandler picSeccion.Paint, AddressOf PicSeccion_Paint
        AddHandler picSeccion.MouseDown, AddressOf PicSec_MouseDown
        AddHandler picSeccion.MouseMove, AddressOf PicSec_MouseMove
        AddHandler picSeccion.MouseUp, AddressOf PicSec_MouseUp
        AddHandler picSeccion.MouseDoubleClick, AddressOf PicSec_DblClick
        panelLeft.Controls.Add(picSeccion)

        lblDims.AutoSize = True : lblDims.Location = New Point(8, 262)
        lblDims.ForeColor = Color.FromArgb(50, 50, 80)
        panelLeft.Controls.Add(lblDims)

        lblCuantia.AutoSize = True : lblCuantia.Location = New Point(8, 278)
        lblCuantia.ForeColor = Color.FromArgb(50, 50, 80)
        panelLeft.Controls.Add(lblCuantia)

        BuildPatronPanel()
        panelLeft.Controls.Add(panelPatron)

        BuildBarraEditPanel()
        panelLeft.Controls.Add(panelBarraEdit)

        lblSelInfo.Text = "  Doble clic en barra = cambiar tipo  |  Clic derecho = menú"
        lblSelInfo.AutoSize = False : lblSelInfo.Width = 318
        lblSelInfo.ForeColor = Color.FromArgb(110, 110, 140)
        lblSelInfo.Font = New Font("Segoe UI", 7.0F, FontStyle.Italic)
        panelLeft.Controls.Add(lblSelInfo)

        BuildContextMenu()
    End Sub

    Private Sub BuildPatronPanel()
        panelPatron.BorderStyle = BorderStyle.FixedSingle
        panelPatron.BackColor = Color.FromArgb(242, 244, 250)
        panelPatron.Size = New Size(320, 144)

        Dim lblTit = New Label() With {
            .Text = "─  Patrón de refuerzo  ─",
            .AutoSize = True, .Location = New Point(6, 5),
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 70, 130)}
        panelPatron.Controls.Add(lblTit)

        ' Fila: Esquinas
        panelPatron.Controls.Add(New Label() With {.Text = "Esquinas (4):", .AutoSize = True, .Location = New Point(6, 26)})
        cmbEsquina.DropDownStyle = ComboBoxStyle.DropDownList
        cmbEsquina.Items.AddRange(BarNombres) : cmbEsquina.SelectedIndex = 3
        cmbEsquina.Location = New Point(106, 23) : cmbEsquina.Width = 72
        panelPatron.Controls.Add(cmbEsquina)

        ' Fila: Cara B (arriba/abajo) — barras a lo largo del eje X
        panelPatron.Controls.Add(New Label() With {.Text = "Cara B ×2:", .AutoSize = True, .Location = New Point(6, 52)})
        nudCaraB.Location = New Point(106, 50) : nudCaraB.Width = 42
        nudCaraB.Minimum = 0 : nudCaraB.Maximum = 10 : nudCaraB.Value = 0
        panelPatron.Controls.Add(nudCaraB)
        panelPatron.Controls.Add(New Label() With {.Text = "×", .AutoSize = True, .Location = New Point(152, 54)})
        cmbCaraB.DropDownStyle = ComboBoxStyle.DropDownList
        cmbCaraB.Items.AddRange(BarNombres) : cmbCaraB.SelectedIndex = 2
        cmbCaraB.Location = New Point(164, 50) : cmbCaraB.Width = 72
        panelPatron.Controls.Add(cmbCaraB)

        ' Fila: Cara H (izq/der) — barras a lo largo del eje Y
        panelPatron.Controls.Add(New Label() With {.Text = "Cara H ×2:", .AutoSize = True, .Location = New Point(6, 78)})
        nudCaraH.Location = New Point(106, 76) : nudCaraH.Width = 42
        nudCaraH.Minimum = 0 : nudCaraH.Maximum = 10 : nudCaraH.Value = 0
        panelPatron.Controls.Add(nudCaraH)
        panelPatron.Controls.Add(New Label() With {.Text = "×", .AutoSize = True, .Location = New Point(152, 80)})
        cmbCaraH.DropDownStyle = ComboBoxStyle.DropDownList
        cmbCaraH.Items.AddRange(BarNombres) : cmbCaraH.SelectedIndex = 2
        cmbCaraH.Location = New Point(164, 76) : cmbCaraH.Width = 72
        panelPatron.Controls.Add(cmbCaraH)

        btnAplicarPatron.Text = "Aplicar patrón"
        btnAplicarPatron.Size = New Size(148, 26) : btnAplicarPatron.Location = New Point(4, 108)
        btnAplicarPatron.BackColor = Color.FromArgb(30, 110, 55) : btnAplicarPatron.ForeColor = Color.White
        btnAplicarPatron.FlatStyle = FlatStyle.Flat : btnAplicarPatron.FlatAppearance.BorderSize = 0
        AddHandler btnAplicarPatron.Click, AddressOf BtnAplicarPatron_Click
        panelPatron.Controls.Add(btnAplicarPatron)

        btnResetAuto.Text = "Distribución auto."
        btnResetAuto.Size = New Size(148, 26) : btnResetAuto.Location = New Point(158, 108)
        btnResetAuto.BackColor = Color.FromArgb(80, 80, 100) : btnResetAuto.ForeColor = Color.White
        btnResetAuto.FlatStyle = FlatStyle.Flat : btnResetAuto.FlatAppearance.BorderSize = 0
        AddHandler btnResetAuto.Click, AddressOf BtnResetAuto_Click
        panelPatron.Controls.Add(btnResetAuto)
    End Sub

    Private Sub BuildBarraEditPanel()
        panelBarraEdit.BorderStyle = BorderStyle.FixedSingle
        panelBarraEdit.BackColor = Color.FromArgb(242, 244, 250)
        panelBarraEdit.Size = New Size(320, 104)

        Dim lblTit = New Label() With {
            .Text = "─  Barra seleccionada  ─",
            .AutoSize = True, .Location = New Point(6, 5),
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 70, 130)}
        panelBarraEdit.Controls.Add(lblTit)

        panelBarraEdit.Controls.Add(New Label() With {.Text = "Tipo:", .AutoSize = True, .Location = New Point(6, 28)})
        cmbBarraSel.DropDownStyle = ComboBoxStyle.DropDownList
        cmbBarraSel.Items.AddRange(BarNombres)
        cmbBarraSel.Location = New Point(44, 26) : cmbBarraSel.Width = 72

        panelBarraEdit.Controls.Add(New Label() With {.Text = "X:", .AutoSize = True, .Location = New Point(126, 28)})
        txtXSel.Location = New Point(142, 26) : txtXSel.Width = 60

        panelBarraEdit.Controls.Add(New Label() With {.Text = "Y:", .AutoSize = True, .Location = New Point(210, 28)})
        txtYSel.Location = New Point(226, 26) : txtYSel.Width = 60

        panelBarraEdit.Controls.Add(New Label() With {
            .Text = "(metros)", .AutoSize = True, .Location = New Point(294, 28),
            .ForeColor = Color.Gray, .Font = New Font("Segoe UI", 7)})

        panelBarraEdit.Controls.Add(cmbBarraSel)
        panelBarraEdit.Controls.Add(txtXSel)
        panelBarraEdit.Controls.Add(txtYSel)

        btnActSel.Text = "Actualizar"
        btnActSel.Size = New Size(100, 26) : btnActSel.Location = New Point(4, 58)
        btnActSel.BackColor = Color.FromArgb(30, 80, 160) : btnActSel.ForeColor = Color.White
        btnActSel.FlatStyle = FlatStyle.Flat : btnActSel.FlatAppearance.BorderSize = 0
        AddHandler btnActSel.Click, AddressOf BtnActSel_Click
        panelBarraEdit.Controls.Add(btnActSel)

        btnDelSel.Text = "Eliminar"
        btnDelSel.Size = New Size(80, 26) : btnDelSel.Location = New Point(110, 58)
        btnDelSel.BackColor = Color.FromArgb(160, 40, 40) : btnDelSel.ForeColor = Color.White
        btnDelSel.FlatStyle = FlatStyle.Flat : btnDelSel.FlatAppearance.BorderSize = 0
        AddHandler btnDelSel.Click, AddressOf BtnDelSel_Click
        panelBarraEdit.Controls.Add(btnDelSel)
    End Sub

    Private Sub BuildContextMenu()
        ctxSec.Items.Add(ctxItemAgregar)
        ctxSec.Items.Add(ctxItemSimetrica)
        ctxSec.Items.Add(ctxItemEliminar)
        ctxSec.Items.Add(New ToolStripSeparator())
        ctxSec.Items.Add(ctxItemReset)

        AddHandler ctxItemAgregar.Click, AddressOf Ctx_Agregar
        AddHandler ctxItemSimetrica.Click, AddressOf Ctx_AgregarSimetrica
        AddHandler ctxItemEliminar.Click, AddressOf Ctx_Eliminar
        AddHandler ctxItemReset.Click, AddressOf BtnResetAuto_Click
        AddHandler ctxSec.Opening, AddressOf Ctx_Opening
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  PANEL DERECHO
    ' ─────────────────────────────────────────────────────────────
    Private Sub BuildRightPanel()
        panelRight.BackColor = Color.White

        lblM3Titulo.Text = "P – M3  (eje fuerte, dirección H)"
        lblM3Titulo.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        lblM3Titulo.AutoSize = True : lblM3Titulo.Location = New Point(4, 4)
        panelRight.Controls.Add(lblM3Titulo)

        picDIM3.BorderStyle = BorderStyle.FixedSingle : picDIM3.BackColor = Color.White
        picDIM3.Location = New Point(4, 22)
        AddHandler picDIM3.Paint, AddressOf PicDIM3_Paint
        panelRight.Controls.Add(picDIM3)

        lblM2Titulo.Text = "P – M2  (eje débil, dirección B)"
        lblM2Titulo.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        lblM2Titulo.AutoSize = True
        panelRight.Controls.Add(lblM2Titulo)

        picDIM2.BorderStyle = BorderStyle.FixedSingle : picDIM2.BackColor = Color.White
        AddHandler picDIM2.Paint, AddressOf PicDIM2_Paint
        panelRight.Controls.Add(picDIM2)

        panelResultados.BackColor = Color.FromArgb(248, 249, 252)
        panelRight.Controls.Add(panelResultados)
        BuildTablaResultados()

        lblResumen.AutoSize = True
        lblResumen.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        lblResumen.ForeColor = Color.FromArgb(30, 80, 160)
        panelRight.Controls.Add(lblResumen)
    End Sub

    Private Sub BuildTablaResultados()
        tablaResultados.Dock = DockStyle.Fill
        tablaResultados.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        tablaResultados.ColumnHeadersHeight = 22
        tablaResultados.RowHeadersVisible = False
        tablaResultados.AllowUserToAddRows = False
        tablaResultados.ReadOnly = True
        tablaResultados.BackgroundColor = Color.White
        tablaResultados.BorderStyle = BorderStyle.None
        tablaResultados.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        tablaResultados.MultiSelect = False

        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cCombo", .HeaderText = "Combinación", .Width = 160})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cPu", .HeaderText = "Pu (kN)", .Width = 70, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F1"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cM3u", .HeaderText = "M3u (kN·m)", .Width = 85, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F2"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cM2u", .HeaderText = "M2u (kN·m)", .Width = 85, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F2"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cPhiMn3", .HeaderText = "φMn3 (kN·m)", .Width = 88, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F2"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cPhiMn2", .HeaderText = "φMn2 (kN·m)", .Width = 88, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F2"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cDC", .HeaderText = "D/C", .Width = 55, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F3"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cEstado", .HeaderText = "Estado", .Width = 70})

        panelResultados.Controls.Add(tablaResultados)
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  LAYOUT
    ' ─────────────────────────────────────────────────────────────
    Private Sub Form_Resize(sender As Object, e As EventArgs)
        LayoutControls()
    End Sub

    Private Sub LayoutControls()
        Dim Wf = Me.ClientSize.Width
        Dim Hf = Me.ClientSize.Height
        Dim topH = panelTop.Height

        panelLeft.SetBounds(0, topH, 336, Hf - topH)
        panelRight.SetBounds(336, topH, Wf - 336, Hf - topH)

        Dim secW = panelLeft.Width - 18
        picSeccion.Width = secW
        picSeccion.Height = Math.Min(secW, 230)

        Dim picBot = picSeccion.Bottom + 4
        lblDims.Location = New Point(8, picBot)
        lblCuantia.Location = New Point(8, picBot + 16)
        panelPatron.Location = New Point(8, picBot + 34)
        panelBarraEdit.Location = New Point(8, picBot + 184)
        lblSelInfo.Location = New Point(8, picBot + 296)

        Dim rW = panelRight.Width - 8
        Dim rH = panelRight.Height
        Dim diagH = CInt(rH * 0.56)
        Dim resH = rH - diagH - 30
        Dim halfW = (rW - 12) \ 2

        lblM3Titulo.Location = New Point(4, 4)
        picDIM3.SetBounds(4, 22, halfW, diagH - 26)
        lblM2Titulo.Location = New Point(halfW + 12, 4)
        picDIM2.SetBounds(halfW + 12, 22, halfW, diagH - 26)
        panelResultados.SetBounds(4, diagH + 4, rW, resH - 20)
        lblResumen.Location = New Point(4, diagH + 4 + resH - 18)
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  CARGA DE DATOS
    ' ─────────────────────────────────────────────────────────────
    Private Sub Form_Load_Handler(sender As Object, e As EventArgs)
        LayoutControls()
        CargarColumnas()
    End Sub

    Private Sub CargarColumnas()
        cmbCol.Items.Clear()
        For Each col In Proyecto.Elementos.Columnas.Lista_Columnas
            cmbCol.Items.Add(col.Name_Label)
        Next
        If cmbCol.Items.Count > 0 Then cmbCol.SelectedIndex = 0
    End Sub

    Private Sub CmbCol_Changed(sender As Object, e As EventArgs)
        cmbTramo.Items.Clear()
        Dim col = GetColumnaActual()
        If col Is Nothing Then Return
        For Each t In col.Lista_Tramos_Columnas
            cmbTramo.Items.Add(t.Piso)
        Next
        If cmbTramo.Items.Count > 0 Then cmbTramo.SelectedIndex = 0
    End Sub

    Private Sub CmbTramo_Changed(sender As Object, e As EventArgs)
        ActualizarTramo()
    End Sub

    Private Sub CmbEst_Changed(sender As Object, e As EventArgs)
        _estacion = If(cmbEst.SelectedIndex = 1, "Bottom", "Top")
        ActualizarTramo()
    End Sub

    Private Sub ActualizarTramo()
        _tramo = GetTramoActual()
        _selIdx = -1 : _dragging = False
        If _tramo Is Nothing Then Return
        CargarBarrasEnEdicion()
        SincronizarPatronUI()
        ActualizarEtiquetas()
        ActualizarUIBarraSel()
        ActualizarResumen()
        picSeccion.Invalidate()
        ' Si el tramo ya tiene diagrama calculado, mostrar resultados inmediatamente
        If _tramo.Lista_DI_M3_P_Phi.Count > 0 Then
            CalcularCombosVerificadas()
            ActualizarTablaResultados()
            picDIM3.Invalidate()
            picDIM2.Invalidate()
            btnVer3D.Enabled = True
        End If
    End Sub

    Private Sub CargarBarrasEnEdicion()
        _barrasEdicion.Clear()
        If _tramo Is Nothing Then Return

        If _tramo.Distribucion_Personalizada AndAlso _tramo.Lista_Barras_Seccion.Count > 0 Then
            For Each bs In _tramo.Lista_Barras_Seccion
                _barrasEdicion.Add(New Tramo_Columna.Barra_Seccion() With {
                    .Name_Barra = bs.Name_Barra, .Asb = bs.Asb, .Db = bs.Db, .X = bs.X, .Y = bs.Y})
            Next
            Return
        End If

        Dim lista = If(_estacion = "Bottom",
                       _tramo.Lista_Detalles_Refuerzo_Bottom,
                       _tramo.Lista_Detalles_Refuerzo_Top)
        If lista IsNot Nothing AndAlso lista.Count > 0 Then
            For Each d In lista
                _barrasEdicion.Add(New Tramo_Columna.Barra_Seccion() With {
                    .Name_Barra = NombreBarraPorArea(d.Asb),
                    .Asb = d.Asb, .Db = d.Db / 1000.0F,
                    .X = d.Coordenada_X, .Y = d.Coordenada_Y})
            Next
            Return
        End If

        ' Fallback: generar distribución uniforme en el perímetro
        Dim N = If(_estacion = "Bottom", _tramo.Cantidad_Barras_Bottom, _tramo.Cantidad_Barras_Top)
        If N = 0 Then Return
        Dim As_eq = If(_estacion = "Bottom", _tramo.As_Col_Bottom, _tramo.As_Col_Top)
        Dim Asb = If(N > 0, As_eq / N, 0.0F)
        Dim coords = DistribuirBarrasConEsquinas(_tramo.B_Plano, _tramo.H_Plano, N)
        For j = 0 To N - 1
            _barrasEdicion.Add(New Tramo_Columna.Barra_Seccion() With {
                .Name_Barra = NombreBarraPorArea(Asb),
                .Asb = Asb, .Db = DbMts(Asb),
                .X = coords(j, 1), .Y = coords(j, 2)})
        Next
    End Sub

    Private Sub GuardarEnTramo()
        If _tramo Is Nothing Then Return
        _tramo.Lista_Barras_Seccion.Clear()
        For Each bs In _barrasEdicion
            _tramo.Lista_Barras_Seccion.Add(New Tramo_Columna.Barra_Seccion() With {
                .Name_Barra = bs.Name_Barra, .Asb = bs.Asb, .Db = bs.Db, .X = bs.X, .Y = bs.Y})
        Next
        _tramo.Distribucion_Personalizada = (_tramo.Lista_Barras_Seccion.Count > 0)
    End Sub

    Private Sub ActualizarEtiquetas()
        If _tramo Is Nothing Then Return
        Dim Bcm = _tramo.B_Plano * 100 : Dim Hcm = _tramo.H_Plano * 100 : Dim fc = _tramo.fc
        lblDims.Text = $"B={Bcm:F0}cm  H={Hcm:F0}cm  f'c={fc:F0}MPa  fy={FY:F0}MPa"
        If _barrasEdicion.Count > 0 Then
            Dim AsT = _barrasEdicion.Sum(Function(bItem) bItem.Asb)
            Dim rho = AsT / (_tramo.B_Plano * _tramo.H_Plano * 1_000_000.0F) * 100
            lblCuantia.Text = $"N={_barrasEdicion.Count}  As={AsT:F0}mm²  ρ={rho:F2}%"
            lblCuantia.ForeColor = Color.FromArgb(50, 50, 80)
        Else
            lblCuantia.Text = "Sin refuerzo – defina en Info Columnas"
            lblCuantia.ForeColor = Color.DarkRed
        End If
    End Sub

    Private Sub SincronizarPatronUI()
        ' Intentar leer tipo de esquina desde la primera barra (si existe)
        If _barrasEdicion.Count >= 4 Then
            Dim tipEsq = _barrasEdicion(0).Name_Barra
            Dim idxEsq = Array.IndexOf(BarNombres, tipEsq)
            If idxEsq >= 0 Then cmbEsquina.SelectedIndex = idxEsq
        End If
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  PATRÓN DE REFUERZO
    ' ─────────────────────────────────────────────────────────────
    Private Sub BtnAplicarPatron_Click(sender As Object, e As EventArgs)
        If _tramo Is Nothing Then Return
        Dim Bsec = _tramo.B_Plano : Dim Hsec = _tramo.H_Plano
        Dim Bci = Bsec / 2.0F - REC
        Dim Hci = Hsec / 2.0F - REC

        _barrasEdicion.Clear()

        ' 4 esquinas
        Dim tipEsq = cmbEsquina.Text
        Dim asbEsq = AreaBarra(tipEsq) : Dim dbEsq = DbMts(asbEsq)
        AgregarBarra(-Bci, Hci, tipEsq, asbEsq, dbEsq)
        AgregarBarra(Bci, Hci, tipEsq, asbEsq, dbEsq)
        AgregarBarra(Bci, -Hci, tipEsq, asbEsq, dbEsq)
        AgregarBarra(-Bci, -Hci, tipEsq, asbEsq, dbEsq)

        ' Cara B (arriba y abajo): barras intermedias a lo largo de X
        Dim nB = CInt(nudCaraB.Value)
        If nB > 0 Then
            Dim tipB = cmbCaraB.Text
            Dim asbB = AreaBarra(tipB) : Dim dbB = DbMts(asbB)
            For i = 1 To nB
                Dim xB = CSng(-Bci + i * 2.0F * Bci / (nB + 1))
                AgregarBarra(xB, Hci, tipB, asbB, dbB)
                AgregarBarra(xB, -Hci, tipB, asbB, dbB)
            Next
        End If

        ' Cara H (izquierda y derecha): barras intermedias a lo largo de Y
        Dim nH = CInt(nudCaraH.Value)
        If nH > 0 Then
            Dim tipH = cmbCaraH.Text
            Dim asbH = AreaBarra(tipH) : Dim dbH = DbMts(asbH)
            For i = 1 To nH
                Dim yH = CSng(-Hci + i * 2.0F * Hci / (nH + 1))
                AgregarBarra(-Bci, yH, tipH, asbH, dbH)
                AgregarBarra(Bci, yH, tipH, asbH, dbH)
            Next
        End If

        _selIdx = -1
        GuardarEnTramo()
        ActualizarEtiquetas()
        ActualizarUIBarraSel()
        picSeccion.Invalidate()
    End Sub

    Private Sub BtnResetAuto_Click(sender As Object, e As EventArgs)
        If _tramo Is Nothing Then Return
        _tramo.Distribucion_Personalizada = False
        _tramo.Lista_Barras_Seccion.Clear()
        _selIdx = -1
        CargarBarrasEnEdicion()
        ActualizarEtiquetas()
        ActualizarUIBarraSel()
        picSeccion.Invalidate()
    End Sub

    Private Sub AgregarBarra(x As Single, y As Single, nombre As String, asb As Single, db As Single)
        _barrasEdicion.Add(New Tramo_Columna.Barra_Seccion() With {
            .Name_Barra = nombre, .Asb = asb, .Db = db, .X = x, .Y = y})
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  EDICIÓN DE BARRA SELECCIONADA
    ' ─────────────────────────────────────────────────────────────
    Private Sub ActualizarUIBarraSel()
        Dim valid = _selIdx >= 0 AndAlso _selIdx < _barrasEdicion.Count
        cmbBarraSel.Enabled = valid : txtXSel.Enabled = valid
        txtYSel.Enabled = valid : btnActSel.Enabled = valid : btnDelSel.Enabled = valid
        If valid Then
            Dim bs = _barrasEdicion(_selIdx)
            Dim idxN = Array.IndexOf(BarNombres, bs.Name_Barra)
            cmbBarraSel.SelectedIndex = If(idxN >= 0, idxN, 2)
            txtXSel.Text = bs.X.ToString("F4")
            txtYSel.Text = bs.Y.ToString("F4")
        Else
            cmbBarraSel.SelectedIndex = -1
            txtXSel.Text = "" : txtYSel.Text = ""
        End If
    End Sub

    Private Sub BtnActSel_Click(sender As Object, e As EventArgs)
        If _selIdx < 0 OrElse _selIdx >= _barrasEdicion.Count Then Return
        Try
            Dim nombre = cmbBarraSel.Text
            Dim newX = CSng(Double.Parse(txtXSel.Text))
            Dim newY = CSng(Double.Parse(txtYSel.Text))
            If _tramo IsNot Nothing Then
                Dim Bci = _tramo.B_Plano / 2.0F - REC
                Dim Hci = _tramo.H_Plano / 2.0F - REC
                newX = Math.Max(-Bci, Math.Min(Bci, newX))
                newY = Math.Max(-Hci, Math.Min(Hci, newY))
            End If
            Dim asb = AreaBarra(nombre)
            _barrasEdicion(_selIdx).Name_Barra = nombre
            _barrasEdicion(_selIdx).Asb = asb
            _barrasEdicion(_selIdx).Db = DbMts(asb)
            _barrasEdicion(_selIdx).X = newX
            _barrasEdicion(_selIdx).Y = newY
            GuardarEnTramo()
            ActualizarEtiquetas()
            ActualizarUIBarraSel()
            picSeccion.Invalidate()
        Catch
        End Try
    End Sub

    Private Sub BtnDelSel_Click(sender As Object, e As EventArgs)
        If _selIdx < 0 OrElse _selIdx >= _barrasEdicion.Count Then Return
        _barrasEdicion.RemoveAt(_selIdx)
        _selIdx = -1
        GuardarEnTramo()
        ActualizarEtiquetas()
        ActualizarUIBarraSel()
        picSeccion.Invalidate()
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  MOUSE – SECCIÓN INTERACTIVA
    ' ─────────────────────────────────────────────────────────────
    Private Sub PicSec_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            Dim idx = HitTestBar(e.Location)
            If idx >= 0 Then
                _selIdx = idx
                Dim bs = _barrasEdicion(idx)
                _dragOfsX = e.X - (_cxPic + bs.X * _scalePic)
                _dragOfsY = e.Y - (_cyPic - bs.Y * _scalePic)
                _dragging = True
            Else
                _selIdx = -1 : _dragging = False
            End If
            ActualizarUIBarraSel()
            picSeccion.Invalidate()
        ElseIf e.Button = MouseButtons.Right Then
            _rightClkPt = e.Location
            Dim idx = HitTestBar(e.Location)
            If idx >= 0 Then
                _selIdx = idx
                ActualizarUIBarraSel()
                picSeccion.Invalidate()
            End If
        End If
    End Sub

    Private Sub PicSec_MouseMove(sender As Object, e As MouseEventArgs)
        If _dragging AndAlso _selIdx >= 0 AndAlso _scalePic > 0 Then
            Dim newX = CSng((e.X - _dragOfsX - _cxPic) / _scalePic)
            Dim newY = CSng((_cyPic - (e.Y - _dragOfsY)) / _scalePic)
            If _tramo IsNot Nothing Then
                Dim Bci = _tramo.B_Plano / 2.0F - REC
                Dim Hci = _tramo.H_Plano / 2.0F - REC
                newX = Math.Max(-Bci, Math.Min(Bci, newX))
                newY = Math.Max(-Hci, Math.Min(Hci, newY))
            End If
            _barrasEdicion(_selIdx).X = newX
            _barrasEdicion(_selIdx).Y = newY
            ActualizarUIBarraSel()
            picSeccion.Invalidate()
        End If
    End Sub

    Private Sub PicSec_MouseUp(sender As Object, e As MouseEventArgs)
        If _dragging Then
            _dragging = False
            GuardarEnTramo()
            ActualizarEtiquetas()
        End If
    End Sub

    Private Sub PicSec_DblClick(sender As Object, e As MouseEventArgs)
        Dim idx = HitTestBar(e.Location)
        If idx < 0 Then Return
        _selIdx = idx
        ' Mini-diálogo para cambiar tipo
        Dim dlg As New Form() With {
            .Text = "Cambiar tipo de barra",
            .Size = New Size(230, 120),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False, .MinimizeBox = False}
        Dim cmb As New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Location = New Point(10, 22), .Width = 100}
        cmb.Items.AddRange(BarNombres)
        cmb.Text = _barrasEdicion(idx).Name_Barra
        Dim btnOK As New Button() With {
            .Text = "Aceptar", .Location = New Point(10, 52),
            .Size = New Size(80, 26), .DialogResult = DialogResult.OK}
        Dim btnCan As New Button() With {
            .Text = "Cancelar", .Location = New Point(100, 52),
            .Size = New Size(80, 26), .DialogResult = DialogResult.Cancel}
        dlg.Controls.AddRange({cmb, btnOK, btnCan})
        dlg.AcceptButton = btnOK : dlg.CancelButton = btnCan
        If dlg.ShowDialog(Me) = DialogResult.OK Then
            Dim asb = AreaBarra(cmb.Text)
            _barrasEdicion(idx).Name_Barra = cmb.Text
            _barrasEdicion(idx).Asb = asb
            _barrasEdicion(idx).Db = DbMts(asb)
            GuardarEnTramo()
            ActualizarEtiquetas()
            ActualizarUIBarraSel()
            picSeccion.Invalidate()
        End If
    End Sub

    Private Function HitTestBar(pt As Point) As Integer
        Const HIT_R As Integer = 9
        For i = 0 To _barrasEdicion.Count - 1
            Dim bs = _barrasEdicion(i)
            Dim sx = _cxPic + bs.X * _scalePic
            Dim sy = _cyPic - bs.Y * _scalePic
            Dim dx = pt.X - sx : Dim dy = pt.Y - sy
            If dx * dx + dy * dy <= HIT_R * HIT_R Then Return i
        Next
        Return -1
    End Function

    ' ─────────────────────────────────────────────────────────────
    '  MENÚ CONTEXTUAL
    ' ─────────────────────────────────────────────────────────────
    Private Sub Ctx_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs)
        Dim hasSel = _selIdx >= 0 AndAlso _selIdx < _barrasEdicion.Count
        ctxItemSimetrica.Enabled = hasSel
        ctxItemEliminar.Enabled = hasSel
    End Sub

    Private Sub Ctx_Agregar(sender As Object, e As EventArgs)
        If _tramo Is Nothing Then Return
        Dim secPt = ScreenToSection(_rightClkPt)
        Dim Bci = _tramo.B_Plano / 2.0F - REC
        Dim Hci = _tramo.H_Plano / 2.0F - REC
        Dim xNew = CSng(Math.Max(-Bci, Math.Min(Bci, secPt.X)))
        Dim yNew = CSng(Math.Max(-Hci, Math.Min(Hci, secPt.Y)))
        AgregarBarra(xNew, yNew, "#4", AreaBarra("#4"), DbMts(AreaBarra("#4")))
        _selIdx = _barrasEdicion.Count - 1
        GuardarEnTramo()
        ActualizarEtiquetas()
        ActualizarUIBarraSel()
        picSeccion.Invalidate()
    End Sub

    Private Sub Ctx_AgregarSimetrica(sender As Object, e As EventArgs)
        If _selIdx < 0 OrElse _selIdx >= _barrasEdicion.Count Then Return
        Dim bs = _barrasEdicion(_selIdx)
        AgregarBarra(-bs.X, bs.Y, bs.Name_Barra, bs.Asb, bs.Db)
        _selIdx = _barrasEdicion.Count - 1
        GuardarEnTramo()
        ActualizarEtiquetas()
        ActualizarUIBarraSel()
        picSeccion.Invalidate()
    End Sub

    Private Sub Ctx_Eliminar(sender As Object, e As EventArgs)
        BtnDelSel_Click(sender, e)
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  CÁLCULO PRINCIPAL
    ' ─────────────────────────────────────────────────────────────
    Private Sub BtnCalc_Click(sender As Object, e As EventArgs)
        _tramo = GetTramoActual()
        If _tramo Is Nothing Then
            MessageBox.Show("Seleccione una columna.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If _barrasEdicion.Count = 0 Then
            MessageBox.Show("Sin refuerzo definido. Defínalo en 'Info Columnas' o aplique un patrón.",
                            "Sin refuerzo", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        GuardarEnTramo()
        Me.Cursor = Cursors.WaitCursor
        Try
            Dim ok = FuncionDiagramaColumna(_tramo, FY, ES, _estacion, Proyecto.Elementos.Columnas.ListA_Combinaciones_Design)
            If Not ok Then
                MessageBox.Show("No se pudo calcular. Verifique los datos de la sección.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            CalcularCombosVerificadas()
            ActualizarTablaResultados()
            ActualizarResumen()
            picDIM3.Invalidate()
            picDIM2.Invalidate()
            btnVer3D.Enabled = True
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub BtnVer3D_Click(sender As Object, e As EventArgs)
        If _tramo Is Nothing Then Return
        Using frm As New Form_02_03_DI_3D(_tramo)
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub CalcularCombosVerificadas()
        _combos = New List(Of (String, Single, Single, Single, Single))
        If _tramo Is Nothing Then Return
        Dim combosD = Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
        Dim combosAEvaluar = If(combosD IsNot Nothing AndAlso combosD.Any(),
                                _tramo.Lista_Combinaciones.Where(Function(c) combosD.Contains(c.Name)).ToList(),
                                _tramo.Lista_Combinaciones)
        For Each combo In combosAEvaluar
            Dim Pu_dia As Single = -combo.P
            Dim M3u = Math.Abs(combo.M3)
            Dim M2u = Math.Abs(combo.M2)
            Dim phiMn3 = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, Pu_dia)
            Dim phiMn2 = InterpolarMnEnPu(_tramo.Lista_DI_M2_P_Phi, _tramo.Lista_DI_M2_Phi, Pu_dia)
            Dim DC As Single = 0
            If phiMn3 > 0.001 Then DC += M3u / phiMn3
            If phiMn2 > 0.001 Then DC += M2u / phiMn2
            _combos.Add((combo.Name, combo.P, combo.M3, combo.M2, Math.Round(DC, 3)))
        Next
        _combos = _combos.OrderByDescending(Function(cc) cc.Item5).ToList()
    End Sub

    Private Sub ActualizarTablaResultados()
        tablaResultados.Rows.Clear()
        If _combos Is Nothing Then Return
        For Each cc In _combos
            Dim Pu_dia As Single = -cc.Pu
            Dim phiMn3 = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, Pu_dia)
            Dim phiMn2 = InterpolarMnEnPu(_tramo.Lista_DI_M2_P_Phi, _tramo.Lista_DI_M2_Phi, Pu_dia)
            Dim estado = If(cc.DC <= 1.0, "Cumple", "No cumple")
            Dim rowIdx = tablaResultados.Rows.Add(cc.Nombre, Math.Round(cc.Pu, 1),
                                                   Math.Round(Math.Abs(cc.M3u), 2),
                                                   Math.Round(Math.Abs(cc.M2u), 2),
                                                   Math.Round(phiMn3, 2), Math.Round(phiMn2, 2),
                                                   cc.DC, estado)
            tablaResultados.Rows(rowIdx).DefaultCellStyle.BackColor =
                If(cc.DC <= 1.0, Color.FromArgb(220, 245, 222), Color.FromArgb(255, 220, 220))
        Next
    End Sub

    Private Sub ActualizarResumen()
        If _tramo Is Nothing Then lblResumen.Text = "" : Return
        Dim fi = _tramo.F_Interaccion
        If fi = 0 Then
            lblResumen.ForeColor = Color.Gray
            lblResumen.Text = "Sin calcular – presione 'Calcular Diagrama'"
            Return
        End If
        Dim cumple = fi <= 1.0F
        lblResumen.ForeColor = If(cumple, Color.FromArgb(0, 120, 50), Color.FromArgb(180, 30, 30))
        lblResumen.Text = $"D/C gobernante = {fi:F3}  [{If(cumple, "CUMPLE ✓", "NO CUMPLE ✗")}]   " &
                          $"Combo crítica: {_tramo.Combo_Gobernante_DI}   " &
                          $"Pu = {_tramo.Pu_Gob_DI:F0} kN   " &
                          $"M3u = {_tramo.M3u_Gob_DI:F2} kN·m   M2u = {_tramo.M2u_Gob_DI:F2} kN·m"
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  DIBUJO – SECCIÓN
    ' ─────────────────────────────────────────────────────────────
    Private Sub PicSeccion_Paint(sender As Object, e As PaintEventArgs)
        Dim gfx = e.Graphics
        gfx.Clear(Color.White)
        If _tramo Is Nothing Then Return
        Dim Bsec = _tramo.B_Plano : Dim Hsec = _tramo.H_Plano
        If Bsec <= 0 OrElse Hsec <= 0 Then Return

        gfx.SmoothingMode = SmoothingMode.AntiAlias
        Dim pic = CType(sender, PictureBox)
        Dim margen = 22
        Dim sc = Math.Min((pic.Width - 2 * margen) / Bsec, (pic.Height - 2 * margen) / Hsec)
        _scalePic = sc
        _cxPic = pic.Width / 2.0F
        _cyPic = pic.Height / 2.0F

        ' Conversión coordenadas
        Dim toX = Function(wx As Single) CSng(_cxPic + wx * sc)
        Dim toY = Function(wy As Single) CSng(_cyPic - wy * sc)

        ' Sección
        Dim rX = CInt(toX(-Bsec / 2)) : Dim rY = CInt(toY(Hsec / 2))
        Dim rW = CInt(Bsec * sc) : Dim rHpx = CInt(Hsec * sc)
        gfx.FillRectangle(New SolidBrush(Color.FromArgb(232, 236, 245)), rX, rY, rW, rHpx)
        gfx.DrawRectangle(New Pen(Color.FromArgb(50, 60, 80), 2), rX, rY, rW, rHpx)

        ' Zona de recubrimiento (punteado)
        Dim crX = CInt(toX(-Bsec / 2 + REC)) : Dim crY = CInt(toY(Hsec / 2 - REC))
        Dim crW = CInt((Bsec - 2 * REC) * sc) : Dim crH = CInt((Hsec - 2 * REC) * sc)
        gfx.DrawRectangle(New Pen(Color.FromArgb(170, 180, 200), 1) With {.DashStyle = DashStyle.Dash}, crX, crY, crW, crH)

        ' Barras
        For idx = 0 To _barrasEdicion.Count - 1
            Dim bs = _barrasEdicion(idx)
            Dim dbPx = Math.Max(6, CInt(bs.Db * sc))
            Dim sx = CInt(toX(bs.X)) : Dim sy = CInt(toY(bs.Y))
            Dim fillColor = If(idx = _selIdx,
                               Color.FromArgb(255, 160, 0),
                               Color.FromArgb(155, 35, 35))
            gfx.FillEllipse(New SolidBrush(fillColor), sx - dbPx, sy - dbPx, 2 * dbPx, 2 * dbPx)
            gfx.DrawEllipse(New Pen(Color.Black, 0.8F), sx - dbPx, sy - dbPx, 2 * dbPx, 2 * dbPx)
            ' Resaltar barra seleccionada con anillo
            If idx = _selIdx Then
                gfx.DrawEllipse(New Pen(Color.FromArgb(255, 130, 0), 2), sx - dbPx - 3, sy - dbPx - 3, 2 * dbPx + 6, 2 * dbPx + 6)
            End If
        Next

        ' Ejes M2/M3
        Dim axisPen = New Pen(Color.FromArgb(100, 140, 200), 1) With {.DashStyle = DashStyle.Dot}
        gfx.DrawLine(axisPen, CInt(toX(-Bsec / 2 - 0.01F)), CInt(toY(0)), CInt(toX(Bsec / 2 + 0.01F)), CInt(toY(0)))
        gfx.DrawLine(axisPen, CInt(toX(0)), CInt(toY(-Hsec / 2 - 0.01F)), CInt(toX(0)), CInt(toY(Hsec / 2 + 0.01F)))

        Dim f7 = New Font("Segoe UI", 6.5F)
        gfx.DrawString($"B={Bsec * 100:F0}cm", f7, Brushes.DimGray, 2, pic.Height - 28)
        gfx.DrawString($"H={Hsec * 100:F0}cm", f7, Brushes.DimGray, 2, pic.Height - 16)
        gfx.DrawString("M3→", f7, New SolidBrush(Color.FromArgb(80, 130, 200)), pic.Width - 32, CInt(toY(0)) - 10)
        Dim st = gfx.Save()
        gfx.TranslateTransform(CInt(toX(0)) + 4, CInt(toY(0)) - 36)
        gfx.RotateTransform(-90)
        gfx.DrawString("←M2", f7, New SolidBrush(Color.FromArgb(80, 130, 200)), 0, 0)
        gfx.Restore(st)

        ' Contador de barras
        If _barrasEdicion.Count > 0 Then
            gfx.DrawString($"{_barrasEdicion.Count} barras", f7, Brushes.DimGray, pic.Width - 50, 4)
        End If
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  DIBUJO – DIAGRAMAS P-M
    ' ─────────────────────────────────────────────────────────────
    Private Sub PicDIM3_Paint(sender As Object, e As PaintEventArgs)
        DibujarDiagrama(CType(sender, PictureBox),
                        _tramo?.Lista_DI_M3_P_Phi, _tramo?.Lista_DI_M3_Phi,
                        _tramo?.Lista_DI_Pn_M3, _tramo?.Lista_DI_Mn_M3, esDirM3:=True)
    End Sub

    Private Sub PicDIM2_Paint(sender As Object, e As PaintEventArgs)
        DibujarDiagrama(CType(sender, PictureBox),
                        _tramo?.Lista_DI_M2_P_Phi, _tramo?.Lista_DI_M2_Phi,
                        _tramo?.Lista_DI_Pn_M2, _tramo?.Lista_DI_Mn_M2, esDirM3:=False)
    End Sub

    Private Sub DibujarDiagrama(pic As PictureBox,
                                 listaPhi_Pn As List(Of Single), listaPhi_Mn As List(Of Single),
                                 listaPn As List(Of Single), listaMn As List(Of Single),
                                 esDirM3 As Boolean)
        If pic.Width < 20 OrElse pic.Height < 20 Then Return
        Dim bmp As New Bitmap(pic.Width, pic.Height)
        Dim gfx = Graphics.FromImage(bmp)
        gfx.Clear(Color.White)
        gfx.SmoothingMode = SmoothingMode.AntiAlias

        Const mL = 52, mR = 10, mT = 18, mB = 32
        Dim pW = pic.Width - mL - mR
        Dim pH = pic.Height - mT - mB

        If listaPhi_Pn Is Nothing OrElse listaPhi_Pn.Count < 2 Then
            gfx.DrawString("Sin datos – presione Calcular", New Font("Segoe UI", 8), Brushes.Gray, mL + 10, mT + pH \ 2)
            pic.Image = bmp : Return
        End If

        Dim maxM = Math.Max(If(listaMn?.Count > 0, listaMn.Max(), 0.0F), listaPhi_Mn.Max()) * 1.2F
        If maxM < 1 Then maxM = 1
        Dim maxP = listaPhi_Pn.Max() * 1.1F
        Dim minP = listaPhi_Pn.Min() * 1.1F
        Dim rangeP = maxP - minP
        If rangeP < 1 Then rangeP = 1

        Dim sX = Function(m As Single) mL + CInt(m / maxM * pW)
        Dim sY = Function(pp As Single) mT + CInt((maxP - pp) / rangeP * pH)

        Dim gPen = New Pen(Color.FromArgb(235, 235, 240))
        For k = 0 To 4
            gfx.DrawLine(gPen, mL + CInt(k / 4.0 * pW), mT, mL + CInt(k / 4.0 * pW), mT + pH)
            gfx.DrawLine(gPen, mL, mT + CInt(k / 4.0 * pH), mL + pW, mT + CInt(k / 4.0 * pH))
        Next

        gfx.DrawLine(New Pen(Color.FromArgb(200, 200, 210)), mL, sY(0), mL + pW, sY(0))
        gfx.DrawLine(Pens.Black, mL, mT, mL, mT + pH)
        gfx.DrawLine(Pens.Black, mL, mT + pH, mL + pW, mT + pH)

        If listaPn IsNot Nothing AndAlso listaPn.Count >= 2 Then
            Dim pts(listaPn.Count - 1) As Point
            For i = 0 To listaPn.Count - 1
                pts(i) = New Point(sX(listaMn(i)), sY(listaPn(i)))
            Next
            gfx.DrawLines(New Pen(Color.FromArgb(160, 160, 200), 1) With {.DashStyle = DashStyle.Dash}, pts)
        End If

        Dim ptsD(listaPhi_Pn.Count - 1) As Point
        For i = 0 To listaPhi_Pn.Count - 1
            ptsD(i) = New Point(sX(listaPhi_Mn(i)), sY(listaPhi_Pn(i)))
        Next
        gfx.DrawLines(New Pen(Color.FromArgb(30, 80, 160), 2), ptsD)

        If _combos IsNot Nothing Then
            For Each cc In _combos
                Dim Pu_dia As Single = -cc.Pu
                Dim Mu = If(esDirM3, Math.Abs(cc.M3u), Math.Abs(cc.M2u))
                Dim ptX = sX(Mu) : Dim ptY = sY(Pu_dia)
                If ptX >= mL AndAlso ptX <= mL + pW AndAlso ptY >= mT AndAlso ptY <= mT + pH Then
                    Dim col = If(cc.DC <= 1.0, Color.FromArgb(0, 160, 80), Color.FromArgb(200, 50, 50))
                    gfx.FillEllipse(New SolidBrush(col), ptX - 3, ptY - 3, 7, 7)
                    gfx.DrawEllipse(Pens.White, ptX - 3, ptY - 3, 7, 7)
                End If
            Next
        End If

        Dim fSmall = New Font("Segoe UI", 6.5F)
        gfx.DrawString("φMn (kN·m)", fSmall, Brushes.Black, mL + pW \ 2 - 20, pic.Height - 14)
        Dim st2 = gfx.Save()
        gfx.TranslateTransform(11, mT + pH \ 2 + 15)
        gfx.RotateTransform(-90)
        gfx.DrawString("φPn (kN)", fSmall, Brushes.Black, 0, 0)
        gfx.Restore(st2)
        gfx.DrawString("0", fSmall, Brushes.Black, mL - 8, mT + pH - 8)
        gfx.DrawString($"{CInt(maxM)}", fSmall, Brushes.Black, mL + pW - 16, mT + pH + 3)
        gfx.DrawString($"{CInt(maxP)}", fSmall, Brushes.Black, 2, mT - 2)
        If minP < 0 Then gfx.DrawString($"{CInt(minP)}", fSmall, Brushes.Black, 2, mT + pH - 8)

        gfx.Dispose()
        pic.Image = bmp
    End Sub

    ' ─────────────────────────────────────────────────────────────
    '  UTILIDADES
    ' ─────────────────────────────────────────────────────────────
    Private Function GetColumnaActual() As Columna
        If cmbCol.SelectedIndex < 0 Then Return Nothing
        Return Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(c) c.Name_Label = cmbCol.Text)
    End Function

    Private Function GetTramoActual() As Tramo_Columna
        Dim col = GetColumnaActual()
        If col Is Nothing OrElse cmbTramo.SelectedIndex < 0 Then Return Nothing
        Return col.Lista_Tramos_Columnas.Find(Function(t) t.Piso = cmbTramo.Text)
    End Function

    Private Function ScreenToSection(pt As Point) As (X As Single, Y As Single)
        If _scalePic <= 0 Then Return (0, 0)
        Return (CSng((pt.X - _cxPic) / _scalePic), CSng((_cyPic - pt.Y) / _scalePic))
    End Function

    Private Function AreaBarra(nombre As String) As Single
        Dim idx = Array.IndexOf(BarNombres, nombre)
        If idx >= 0 Then Return BarAreas(idx)
        Return AreaRefuerzo(nombre)
    End Function

    Private Function DbMts(asb As Single) As Single
        Return CSng(Math.Sqrt(4.0 * asb / Math.PI)) / 1000.0F
    End Function

    Private Function NombreBarraPorArea(asb As Single) As String
        Dim minDiff = Single.MaxValue : Dim mejor = "#4"
        For i = 0 To BarNombres.Length - 1
            Dim diff = Math.Abs(BarAreas(i) - asb)
            If diff < minDiff Then minDiff = diff : mejor = BarNombres(i)
        Next
        Return mejor
    End Function

End Class
