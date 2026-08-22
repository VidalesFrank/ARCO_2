Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_02_Columnas
Imports System.Drawing.Drawing2D

Public Class Form_02_02_DiagramaCircular
    Inherits Form

    Private ReadOnly Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Const FY As Single = 420.0F
    Private Const ES As Single = 200000.0F
    Private Const REC As Single = 0.05F

    Private Shared ReadOnly BarNombres As String() = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}

    Private _tramo As Tramo_Columna = Nothing
    Private _estacion As String = "Top"
    Private _combos As List(Of (Nombre As String, Pu As Single, Mu As Single, PhiMn As Single, DC As Single))

    ' Top strip
    Private ReadOnly panelTop As New Panel()
    Private ReadOnly cmbCol As New ComboBox()
    Private ReadOnly cmbTramo As New ComboBox()
    Private ReadOnly cmbEst As New ComboBox()
    Private ReadOnly btnCalc As New Button()

    ' Left panel
    Private ReadOnly panelLeft As New Panel()
    Private ReadOnly picSeccion As New PictureBox()
    Private ReadOnly lblDims As New Label()
    Private ReadOnly lblCuantia As New Label()
    Private ReadOnly nudNBarras As New NumericUpDown()
    Private ReadOnly cmbBarraLong As New ComboBox()
    Private ReadOnly cmbTipoTrans As New ComboBox()
    Private ReadOnly cmbBarraTrans As New ComboBox()
    Private ReadOnly txtSepZC As New TextBox()
    Private ReadOnly txtSepZNC As New TextBox()

    ' Right panel
    Private ReadOnly panelRight As New Panel()
    Private ReadOnly lblDiagTitulo As New Label()
    Private ReadOnly picDI As New PictureBox()
    Private ReadOnly panelResultados As New Panel()
    Private ReadOnly tablaResultados As New DataGridView()
    Private ReadOnly lblResumen As New Label()

    Public Sub New()
        BuildUI()
    End Sub

    ' ────────────────────────────────────────────────────────
    '  BUILD UI
    ' ────────────────────────────────────────────────────────
    Private Sub BuildUI()
        Me.Text = "Diagrama de Interacción – Columnas Circulares"
        Me.Size = New Size(1080, 720)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 8.5F)
        BuildTopStrip()
        BuildLeftPanel()
        BuildRightPanel()
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelLeft)
        Me.Controls.Add(panelRight)
        AddHandler Me.Resize, AddressOf OnFormResize
        AddHandler Me.Load, AddressOf OnFormLoad
    End Sub

    Private Sub BuildTopStrip()
        panelTop.Dock = DockStyle.Top
        panelTop.Height = 44
        panelTop.BackColor = Color.FromArgb(240, 243, 248)

        Dim px As Integer = 10
        Dim lbl0 As New Label() With {.Text = "Columna:", .AutoSize = True, .Top = 14, .Left = px}
        panelTop.Controls.Add(lbl0) : px += lbl0.PreferredWidth + 4
        cmbCol.DropDownStyle = ComboBoxStyle.DropDownList
        cmbCol.Width = 140 : cmbCol.Top = 8 : cmbCol.Left = px
        panelTop.Controls.Add(cmbCol) : px += 148

        Dim lbl1 As New Label() With {.Text = "Tramo:", .AutoSize = True, .Top = 14, .Left = px}
        panelTop.Controls.Add(lbl1) : px += lbl1.PreferredWidth + 4
        cmbTramo.DropDownStyle = ComboBoxStyle.DropDownList
        cmbTramo.Width = 100 : cmbTramo.Top = 8 : cmbTramo.Left = px
        panelTop.Controls.Add(cmbTramo) : px += 108

        Dim lbl2 As New Label() With {.Text = "Estación:", .AutoSize = True, .Top = 14, .Left = px}
        panelTop.Controls.Add(lbl2) : px += lbl2.PreferredWidth + 4
        cmbEst.DropDownStyle = ComboBoxStyle.DropDownList
        cmbEst.Items.AddRange({"Top", "Bottom"})
        cmbEst.SelectedIndex = 0
        cmbEst.Width = 80 : cmbEst.Top = 8 : cmbEst.Left = px
        panelTop.Controls.Add(cmbEst) : px += 88

        btnCalc.Text = "Calcular Diagrama"
        btnCalc.Size = New Size(155, 28) : btnCalc.Top = 8 : btnCalc.Left = px + 20
        btnCalc.BackColor = Color.FromArgb(30, 80, 160) : btnCalc.ForeColor = Color.White
        btnCalc.FlatStyle = FlatStyle.Flat : btnCalc.FlatAppearance.BorderSize = 0
        panelTop.Controls.Add(btnCalc)

        AddHandler cmbCol.SelectedIndexChanged, AddressOf CmbCol_Changed
        AddHandler cmbTramo.SelectedIndexChanged, AddressOf CmbTramo_Changed
        AddHandler cmbEst.SelectedIndexChanged, AddressOf CmbEst_Changed
        AddHandler btnCalc.Click, AddressOf BtnCalc_Click
    End Sub

    Private Sub BuildLeftPanel()
        panelLeft.Width = 310
        panelLeft.BackColor = Color.FromArgb(248, 249, 252)

        Dim lblSec As New Label() With {
            .Text = "Sección Circular",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .AutoSize = True, .Location = New Point(8, 6)}
        panelLeft.Controls.Add(lblSec)

        picSeccion.BorderStyle = BorderStyle.FixedSingle
        picSeccion.BackColor = Color.White
        picSeccion.Location = New Point(8, 26)
        picSeccion.Size = New Size(292, 220)
        AddHandler picSeccion.Paint, AddressOf DibujarSeccion
        panelLeft.Controls.Add(picSeccion)

        lblDims.AutoSize = True : lblDims.Location = New Point(8, 252)
        lblDims.ForeColor = Color.FromArgb(50, 50, 80)
        panelLeft.Controls.Add(lblDims)

        lblCuantia.AutoSize = True : lblCuantia.Location = New Point(8, 268)
        lblCuantia.ForeColor = Color.FromArgb(50, 50, 80)
        panelLeft.Controls.Add(lblCuantia)

        ' ── Panel refuerzo longitudinal ───────────────────────
        Dim pLong As New Panel() With {
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.FromArgb(242, 244, 250),
            .Size = New Size(292, 95),
            .Location = New Point(8, 292)}
        panelLeft.Controls.Add(pLong)

        pLong.Controls.Add(New Label() With {
            .Text = "─  Refuerzo Longitudinal  ─",
            .AutoSize = True, .Location = New Point(6, 5),
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 70, 130)})

        pLong.Controls.Add(New Label() With {.Text = "N barras:", .AutoSize = True, .Location = New Point(6, 32)})
        nudNBarras.Minimum = 4 : nudNBarras.Maximum = 40 : nudNBarras.Value = 8
        nudNBarras.Location = New Point(88, 29) : nudNBarras.Width = 58
        pLong.Controls.Add(nudNBarras)

        pLong.Controls.Add(New Label() With {.Text = "Barra:", .AutoSize = True, .Location = New Point(6, 63)})
        cmbBarraLong.DropDownStyle = ComboBoxStyle.DropDownList
        cmbBarraLong.Items.AddRange(BarNombres)
        cmbBarraLong.SelectedIndex = 3
        cmbBarraLong.Location = New Point(88, 60) : cmbBarraLong.Width = 90
        pLong.Controls.Add(cmbBarraLong)

        AddHandler nudNBarras.ValueChanged, AddressOf OnRefuerzoChanged
        AddHandler cmbBarraLong.SelectedIndexChanged, AddressOf OnRefuerzoChanged

        ' ── Panel refuerzo transversal ────────────────────────
        Dim pTrans As New Panel() With {
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.FromArgb(242, 244, 250),
            .Size = New Size(292, 128),
            .Location = New Point(8, 397)}
        panelLeft.Controls.Add(pTrans)

        pTrans.Controls.Add(New Label() With {
            .Text = "─  Refuerzo Transversal  ─",
            .AutoSize = True, .Location = New Point(6, 5),
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(40, 70, 130)})

        pTrans.Controls.Add(New Label() With {.Text = "Tipo:", .AutoSize = True, .Location = New Point(6, 32)})
        cmbTipoTrans.DropDownStyle = ComboBoxStyle.DropDownList
        cmbTipoTrans.Items.AddRange({"Espiral", "Estribos"})
        cmbTipoTrans.SelectedIndex = 0
        cmbTipoTrans.Location = New Point(88, 29) : cmbTipoTrans.Width = 100
        pTrans.Controls.Add(cmbTipoTrans)

        pTrans.Controls.Add(New Label() With {.Text = "Barra:", .AutoSize = True, .Location = New Point(6, 63)})
        cmbBarraTrans.DropDownStyle = ComboBoxStyle.DropDownList
        cmbBarraTrans.Items.AddRange(BarNombres)
        cmbBarraTrans.SelectedIndex = 1
        cmbBarraTrans.Location = New Point(88, 60) : cmbBarraTrans.Width = 90
        pTrans.Controls.Add(cmbBarraTrans)

        pTrans.Controls.Add(New Label() With {.Text = "Sep. ZC (m):", .AutoSize = True, .Location = New Point(6, 95)})
        txtSepZC.Location = New Point(104, 92) : txtSepZC.Width = 68 : txtSepZC.Text = "0.100"
        pTrans.Controls.Add(txtSepZC)

        pTrans.Controls.Add(New Label() With {.Text = "Sep. ZNC (m):", .AutoSize = True, .Location = New Point(166, 95)})
        txtSepZNC.Location = New Point(256, 92) : txtSepZNC.Width = 28 : txtSepZNC.Text = "0.200"
        pTrans.Controls.Add(txtSepZNC)

        ' ── Botón actualizar tramo ────────────────────────────
        Dim btnGuardar As New Button() With {
            .Text = "Actualizar datos en tramo",
            .Size = New Size(292, 28),
            .Location = New Point(8, 536),
            .BackColor = Color.FromArgb(30, 110, 55),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat}
        btnGuardar.FlatAppearance.BorderSize = 0
        AddHandler btnGuardar.Click, AddressOf BtnGuardar_Click
        panelLeft.Controls.Add(btnGuardar)
    End Sub

    Private Sub BuildRightPanel()
        panelRight.BackColor = Color.White

        lblDiagTitulo.Text = "P – φMn  (sección circular isótropa: M3 = M2)"
        lblDiagTitulo.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        lblDiagTitulo.AutoSize = True : lblDiagTitulo.Location = New Point(4, 4)
        panelRight.Controls.Add(lblDiagTitulo)

        picDI.BorderStyle = BorderStyle.FixedSingle
        picDI.BackColor = Color.White
        picDI.Location = New Point(4, 26)
        AddHandler picDI.Paint, AddressOf DibujarDiagrama
        panelRight.Controls.Add(picDI)

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

        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cCombo", .HeaderText = "Combinación", .Width = 185})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cPu", .HeaderText = "Pu (kN)", .Width = 75, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F1"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cMu", .HeaderText = "Mu (kN·m)", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F2"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cPhiMn", .HeaderText = "φMn (kN·m)", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F2"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cDC", .HeaderText = "C/D", .Width = 60, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "F3"}})
        tablaResultados.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "cEstado", .HeaderText = "Estado", .Width = 75})

        panelResultados.Controls.Add(tablaResultados)
    End Sub

    ' ────────────────────────────────────────────────────────
    '  LAYOUT
    ' ────────────────────────────────────────────────────────
    Private Sub OnFormLoad(sender As Object, e As EventArgs)
        LayoutControls()
        CargarColumnas()
    End Sub

    Private Sub OnFormResize(sender As Object, e As EventArgs)
        LayoutControls()
    End Sub

    Private Sub LayoutControls()
        Dim Wf = Me.ClientSize.Width
        Dim Hf = Me.ClientSize.Height
        Dim topH = panelTop.Height

        panelLeft.SetBounds(0, topH, 310, Hf - topH)
        panelRight.SetBounds(310, topH, Wf - 310, Hf - topH)

        Dim secW = panelLeft.Width - 18
        picSeccion.Width = secW
        picSeccion.Height = Math.Min(secW, 220)

        Dim rW = panelRight.Width - 8
        Dim rH = panelRight.Height
        Dim diagH = CInt(rH * 0.56)
        Dim resH = rH - diagH - 30

        picDI.SetBounds(4, 26, rW, diagH - 30)
        panelResultados.SetBounds(4, diagH + 4, rW, resH - 20)
        lblResumen.Location = New Point(4, diagH + 4 + resH - 18)
    End Sub

    ' ────────────────────────────────────────────────────────
    '  CARGA DE DATOS
    ' ────────────────────────────────────────────────────────
    Private Sub CargarColumnas()
        cmbCol.Items.Clear()
        For Each col In Proyecto.Elementos.Columnas.Lista_Columnas
            If col.Lista_Tramos_Columnas.Any(Function(t) t.EsCircular) Then
                cmbCol.Items.Add(col.Name_Label)
            End If
        Next
        If cmbCol.Items.Count > 0 Then cmbCol.SelectedIndex = 0
    End Sub

    Private Sub CmbCol_Changed(sender As Object, e As EventArgs)
        cmbTramo.Items.Clear()
        Dim col = GetColumnaActual()
        If col Is Nothing Then Return
        For Each t In col.Lista_Tramos_Columnas
            If t.EsCircular Then cmbTramo.Items.Add(t.Piso)
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
        If _tramo Is Nothing Then Return

        Dim nB = If(_estacion = "Bottom", _tramo.Cantidad_Barras_Bottom, _tramo.Cantidad_Barras_Top)
        If nB >= 4 Then nudNBarras.Value = nB

        Dim refLong = If(_estacion = "Bottom", _tramo.Refuerzo_Col_Bottom, _tramo.Refuerzo_Col_Top)
        Dim barDom = BarraDominante(refLong)
        Dim idxBar = Array.IndexOf(BarNombres, barDom)
        If idxBar >= 0 Then cmbBarraLong.SelectedIndex = idxBar

        If Not String.IsNullOrEmpty(_tramo.Numero_Barras_Estribo) Then
            Dim idxT = Array.IndexOf(BarNombres, _tramo.Numero_Barras_Estribo)
            If idxT >= 0 Then cmbBarraTrans.SelectedIndex = idxT
        End If
        cmbTipoTrans.SelectedIndex = If(_tramo.TipoTransversal = "Espiral", 0, 1)
        If _tramo.Separacion_Estribos > 0 Then txtSepZC.Text = _tramo.Separacion_Estribos.ToString("F3")
        If _tramo.Separacion_Estribos_ZNC > 0 Then txtSepZNC.Text = _tramo.Separacion_Estribos_ZNC.ToString("F3")

        ActualizarEtiquetas()
        picSeccion.Invalidate()

        If _tramo.Lista_DI_M3_P_Phi IsNot Nothing AndAlso _tramo.Lista_DI_M3_P_Phi.Count > 0 Then
            CalcularCombosParaTabla()
            ActualizarTabla()
            ActualizarResumen()
            picDI.Invalidate()
        Else
            tablaResultados.Rows.Clear()
            lblResumen.Text = "Presione 'Calcular Diagrama' para calcular el DI."
            lblResumen.ForeColor = Color.FromArgb(100, 100, 120)
            picDI.Invalidate()
        End If
    End Sub

    Private Sub OnRefuerzoChanged(sender As Object, e As EventArgs)
        ActualizarEtiquetas()
        picSeccion.Invalidate()
    End Sub

    Private Sub ActualizarEtiquetas()
        If _tramo Is Nothing Then Return
        Dim Dcm = _tramo.Diametro * 100
        lblDims.Text = $"Ø = {Dcm:F0} cm     f'c = {_tramo.fc:F0} MPa     fy = {FY:F0} MPa"
        Dim N = CInt(nudNBarras.Value)
        Dim barNom = If(cmbBarraLong.SelectedIndex >= 0, cmbBarraLong.Text, "#5")
        Dim asb = CSng(AreaRefuerzo(barNom))
        Dim AsTotal = N * asb
        Dim Ag = CSng(Math.PI * _tramo.Diametro ^ 2 / 4.0)
        Dim rho = If(Ag > 0, AsTotal / (Ag * 1_000_000.0F) * 100, 0)
        lblCuantia.Text = $"N = {N} barras {barNom}     As = {AsTotal:F0} mm²     ρ = {rho:F2}%"
    End Sub

    ' ────────────────────────────────────────────────────────
    '  GUARDAR EN TRAMO
    ' ────────────────────────────────────────────────────────
    Private Sub BtnGuardar_Click(sender As Object, e As EventArgs)
        If _tramo Is Nothing Then Return
        Dim N = CInt(nudNBarras.Value)
        Dim barNom = If(cmbBarraLong.SelectedIndex >= 0, cmbBarraLong.Text, "#5")
        Dim asb = CSng(AreaRefuerzo(barNom))

        Dim refLong = If(_estacion = "Bottom", _tramo.Refuerzo_Col_Bottom, _tramo.Refuerzo_Col_Top)
        refLong.Barras_2 = 0 : refLong.Barras_3 = 0 : refLong.Barras_4 = 0
        refLong.Barras_5 = 0 : refLong.Barras_6 = 0 : refLong.Barras_7 = 0
        refLong.Barras_8 = 0 : refLong.Barras_10 = 0
        Select Case barNom
            Case "#2" : refLong.Barras_2 = N
            Case "#3" : refLong.Barras_3 = N
            Case "#4" : refLong.Barras_4 = N
            Case "#5" : refLong.Barras_5 = N
            Case "#6" : refLong.Barras_6 = N
            Case "#7" : refLong.Barras_7 = N
            Case "#8" : refLong.Barras_8 = N
            Case "#10" : refLong.Barras_10 = N
        End Select

        If _estacion = "Bottom" Then
            _tramo.Cantidad_Barras_Bottom = N
            _tramo.As_Col_Bottom = N * asb
        Else
            _tramo.Cantidad_Barras_Top = N
            _tramo.As_Col_Top = N * asb
        End If

        _tramo.TipoTransversal = cmbTipoTrans.Text
        _tramo.Numero_Barras_Estribo = If(cmbBarraTrans.SelectedIndex >= 0, cmbBarraTrans.Text, "#3")
        Dim sepVal As Single
        If Single.TryParse(txtSepZC.Text.Replace(",", "."),
                           System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, sepVal) AndAlso sepVal > 0 Then
            _tramo.Separacion_Estribos = sepVal
        End If
        Dim sepZNCVal As Single
        If Single.TryParse(txtSepZNC.Text.Replace(",", "."),
                           System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, sepZNCVal) AndAlso sepZNCVal > 0 Then
            _tramo.Separacion_Estribos_ZNC = sepZNCVal
        End If

        MessageBox.Show("Datos actualizados en el tramo. Presione 'Calcular Diagrama' para recalcular.",
                        "Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' ────────────────────────────────────────────────────────
    '  CÁLCULO
    ' ────────────────────────────────────────────────────────
    Private Sub BtnCalc_Click(sender As Object, e As EventArgs)
        If _tramo Is Nothing Then
            MessageBox.Show("Seleccione una columna circular y un tramo.", "Sin selección",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Me.Cursor = Cursors.WaitCursor
        Try
            Dim combosD = Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
            Dim ok = FuncionDiagramaColumnaCircular(_tramo, FY, ES, _estacion, combosD)
            If Not ok Then
                MessageBox.Show("No se pudo calcular el diagrama. Verifique que el tramo tenga refuerzo definido.",
                                "Error de cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            CalcularCombosParaTabla()
            ActualizarTabla()
            ActualizarResumen()
            picDI.Invalidate()
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub CalcularCombosParaTabla()
        _combos = New List(Of (String, Single, Single, Single, Single))
        If _tramo Is Nothing OrElse
           _tramo.Lista_DI_M3_P_Phi Is Nothing OrElse
           _tramo.Lista_DI_M3_P_Phi.Count = 0 Then Return

        Dim combosD = Proyecto.Elementos.Columnas.ListA_Combinaciones_Design
        Dim combosEval = If(combosD IsNot Nothing AndAlso combosD.Any(),
                            _tramo.Lista_Combinaciones.Where(Function(c) combosD.Contains(c.Name)).ToList(),
                            _tramo.Lista_Combinaciones)

        For Each combo In combosEval
            Dim Pu_dia As Single = -combo.P
            Dim Mu As Single = CSng(Math.Sqrt(combo.M3 ^ 2 + combo.M2 ^ 2))
            Dim phiMn As Single = InterpolarMnEnPu(_tramo.Lista_DI_M3_P_Phi, _tramo.Lista_DI_M3_Phi, Pu_dia)
            Dim DC As Single = If(phiMn > 0.001F, Mu / phiMn, 999.0F)
            _combos.Add((combo.Name, combo.P, Mu, phiMn, CSng(Math.Round(DC, 3))))
        Next
        _combos = _combos.OrderByDescending(Function(c) c.DC).ToList()
    End Sub

    Private Sub ActualizarTabla()
        tablaResultados.Rows.Clear()
        If _combos Is Nothing Then Return
        For Each cc In _combos
            Dim estado = If(cc.DC <= 1.0F, "Cumple", "No cumple")
            Dim cdVal As Single = If(cc.DC > 0, 1.0F / cc.DC, 0.0F)
            Dim rowIdx = tablaResultados.Rows.Add(cc.Nombre,
                                                   Math.Round(cc.Pu, 1),
                                                   Math.Round(cc.Mu, 2),
                                                   Math.Round(cc.PhiMn, 2),
                                                   cdVal, estado)
            tablaResultados.Rows(rowIdx).DefaultCellStyle.BackColor =
                If(cc.DC <= 1.0F, Color.FromArgb(232, 255, 232), Color.FromArgb(255, 228, 228))
        Next
    End Sub

    Private Sub ActualizarResumen()
        If _tramo Is Nothing Then lblResumen.Text = "" : Return
        Dim dc = _tramo.F_Interaccion
        Dim cd = If(dc > 0, 1.0F / dc, 0)
        Dim ok = dc <= 1.0F
        lblResumen.Text = $"Combo gobernante: {_tramo.Combo_Gobernante_DI}   " &
                          $"C/D = {cd:F3}   " &
                          $"{If(ok, "CUMPLE", "NO CUMPLE")}   " &
                          $"Pu = {_tramo.Pu_Gob_DI:F0} kN   " &
                          $"M3u = {_tramo.M3u_Gob_DI:F2} kN·m   M2u = {_tramo.M2u_Gob_DI:F2} kN·m"
        lblResumen.ForeColor = If(ok, Color.FromArgb(0, 130, 50), Color.FromArgb(180, 30, 30))
    End Sub

    ' ────────────────────────────────────────────────────────
    '  DIBUJO SECCIÓN CIRCULAR
    ' ────────────────────────────────────────────────────────
    Private Sub DibujarSeccion(sender As Object, e As PaintEventArgs)
        Dim gfx = e.Graphics
        gfx.Clear(Color.White)
        If _tramo Is Nothing OrElse _tramo.Diametro <= 0 Then Return
        gfx.SmoothingMode = SmoothingMode.AntiAlias

        Dim D = _tramo.Diametro
        Dim W = picSeccion.Width, H = picSeccion.Height
        Dim margen = 24
        Dim sc = Math.Min((W - 2 * margen) / D, (H - 2 * margen) / D)
        Dim cx = W / 2.0F, cy = H / 2.0F
        Dim Dpx = CInt(D * sc)

        ' Concreto
        gfx.FillEllipse(New SolidBrush(Color.FromArgb(218, 228, 245)),
                        cx - Dpx / 2.0F, cy - Dpx / 2.0F, Dpx, Dpx)
        gfx.DrawEllipse(New Pen(Color.FromArgb(45, 58, 88), 2.2F),
                        cx - Dpx / 2.0F, cy - Dpx / 2.0F, Dpx, Dpx)

        ' Recubrimiento (punteado)
        Dim Dcore = D - 2.0F * REC
        Dim Dcorepx = CInt(Dcore * sc)
        gfx.DrawEllipse(New Pen(Color.FromArgb(130, 148, 185), 1) With {.DashStyle = DashStyle.Dash},
                        cx - Dcorepx / 2.0F, cy - Dcorepx / 2.0F, Dcorepx, Dcorepx)

        ' Barras en corona
        Dim N = CInt(nudNBarras.Value)
        Dim Rb = D / 2.0F - REC
        Dim asbUnit = CSng(AreaRefuerzo(If(cmbBarraLong.SelectedIndex >= 0, cmbBarraLong.Text, "#5")))
        Dim dbM = CSng(Math.Sqrt(4.0 * asbUnit / Math.PI)) / 1000.0F
        Dim dbPx = Math.Max(4, CInt(dbM * sc * 1.2F))

        For i = 0 To N - 1
            Dim ang = 2.0 * Math.PI * i / N + Math.PI / 2.0
            Dim bx = cx + CSng(Rb * sc * Math.Cos(ang))
            Dim by = cy - CSng(Rb * sc * Math.Sin(ang))
            gfx.FillEllipse(New SolidBrush(Color.FromArgb(55, 55, 55)), bx - dbPx, by - dbPx, 2 * dbPx, 2 * dbPx)
            gfx.DrawEllipse(New Pen(Color.FromArgb(20, 20, 20), 0.5F), bx - dbPx, by - dbPx, 2 * dbPx, 2 * dbPx)
        Next

        ' Eje de referencia (punteado)
        Dim axisPen = New Pen(Color.FromArgb(80, 120, 200), 1) With {.DashStyle = DashStyle.Dot}
        gfx.DrawLine(axisPen, cx - Dpx / 2.0F - 6, cy, cx + Dpx / 2.0F + 6, cy)
        gfx.DrawLine(axisPen, cx, cy - Dpx / 2.0F - 6, cx, cy + Dpx / 2.0F + 6)

        ' Etiquetas
        Dim f7 = New Font("Segoe UI", 6.5F)
        Dim barNom = If(cmbBarraLong.SelectedIndex >= 0, cmbBarraLong.Text, "")
        gfx.DrawString($"Ø = {D * 100:F0} cm", f7, Brushes.DimGray, 4, H - 26)
        gfx.DrawString($"{N} barras {barNom}  —  rec. = {REC * 100:F0} cm", f7, Brushes.DimGray, 4, H - 14)
    End Sub

    ' ────────────────────────────────────────────────────────
    '  DIBUJO DIAGRAMA P-M
    ' ────────────────────────────────────────────────────────
    Private Sub DibujarDiagrama(sender As Object, e As PaintEventArgs)
        Dim pic = CType(sender, PictureBox)
        If pic.Width < 20 OrElse pic.Height < 20 Then Return

        Dim listaPhi_Pn = _tramo?.Lista_DI_M3_P_Phi
        Dim listaPhi_Mn = _tramo?.Lista_DI_M3_Phi
        Dim listaPn = _tramo?.Lista_DI_Pn_M3
        Dim listaMn = _tramo?.Lista_DI_Mn_M3

        Dim bmp As New Bitmap(pic.Width, pic.Height)
        Dim gfx = Graphics.FromImage(bmp)
        gfx.Clear(Color.White)
        gfx.SmoothingMode = SmoothingMode.AntiAlias

        Const mL = 58, mR = 14, mT = 20, mB = 38
        Dim pW = pic.Width - mL - mR
        Dim pH = pic.Height - mT - mB

        If listaPhi_Pn Is Nothing OrElse listaPhi_Pn.Count < 2 Then
            gfx.DrawString("Sin datos – presione  Calcular Diagrama",
                           New Font("Segoe UI", 9), Brushes.Gray, mL + 20, mT + pH \ 2 - 8)
            pic.Image = bmp : gfx.Dispose() : Return
        End If

        Dim maxM = Math.Max(If(listaMn?.Count > 0, listaMn.Max(), 0.0F), listaPhi_Mn.Max()) * 1.2F
        If maxM < 1 Then maxM = 1
        Dim maxP = listaPhi_Pn.Max() * 1.1F
        Dim minP = listaPhi_Pn.Min() * 1.1F
        Dim rangeP = maxP - minP : If rangeP < 1 Then rangeP = 1

        Dim sX = Function(m As Single) mL + CInt(m / maxM * pW)
        Dim sY = Function(pp As Single) mT + CInt((maxP - pp) / rangeP * pH)

        ' Cuadrícula
        Dim grdPen = New Pen(Color.FromArgb(232, 232, 238))
        For k = 0 To 5
            gfx.DrawLine(grdPen, mL + CInt(k / 5.0 * pW), mT, mL + CInt(k / 5.0 * pW), mT + pH)
            gfx.DrawLine(grdPen, mL, mT + CInt(k / 5.0 * pH), mL + pW, mT + CInt(k / 5.0 * pH))
        Next
        gfx.DrawLine(New Pen(Color.FromArgb(185, 190, 210)), mL, sY(0), mL + pW, sY(0))
        gfx.DrawLine(Pens.Black, mL, mT, mL, mT + pH)
        gfx.DrawLine(Pens.Black, mL, mT + pH, mL + pW, mT + pH)

        ' Curva nominal Pn-Mn (sin phi, línea punteada)
        If listaPn IsNot Nothing AndAlso listaPn.Count >= 2 Then
            Dim ptsN(listaPn.Count - 1) As Point
            For i = 0 To listaPn.Count - 1
                ptsN(i) = New Point(sX(listaMn(i)), sY(listaPn(i)))
            Next
            gfx.DrawLines(New Pen(Color.FromArgb(155, 155, 195), 1.2F) With {.DashStyle = DashStyle.Dash}, ptsN)
        End If

        ' Curva de diseño φPn-φMn (línea sólida)
        Dim ptsD(listaPhi_Pn.Count - 1) As Point
        For i = 0 To listaPhi_Pn.Count - 1
            ptsD(i) = New Point(sX(listaPhi_Mn(i)), sY(listaPhi_Pn(i)))
        Next
        gfx.DrawLines(New Pen(Color.FromArgb(30, 80, 160), 2.5F), ptsD)

        ' Puntos de combinaciones de diseño
        If _combos IsNot Nothing Then
            For Each cc In _combos
                Dim Pu_dia As Single = -cc.Pu
                Dim ptX = sX(cc.Mu) : Dim ptY = sY(Pu_dia)
                If ptX >= mL AndAlso ptX <= mL + pW AndAlso ptY >= mT AndAlso ptY <= mT + pH Then
                    Dim col = If(cc.DC <= 1.0F, Color.FromArgb(0, 155, 70), Color.FromArgb(195, 45, 45))
                    gfx.FillEllipse(New SolidBrush(col), ptX - 4, ptY - 4, 9, 9)
                    gfx.DrawEllipse(Pens.White, ptX - 4, ptY - 4, 9, 9)
                End If
            Next
        End If

        ' Leyenda
        Dim fLeg = New Font("Segoe UI", 6.5F)
        gfx.DrawLine(New Pen(Color.FromArgb(155, 155, 195), 1.2F) With {.DashStyle = DashStyle.Dash},
                     mL + 10, mT + pH + 24, mL + 34, mT + pH + 24)
        gfx.DrawString("Pn nominal", fLeg, Brushes.Gray, mL + 38, mT + pH + 18)
        gfx.DrawLine(New Pen(Color.FromArgb(30, 80, 160), 2.5F), mL + 130, mT + pH + 24, mL + 154, mT + pH + 24)
        gfx.DrawString("φPn diseño", fLeg, New SolidBrush(Color.FromArgb(30, 80, 160)), mL + 158, mT + pH + 18)

        ' Etiquetas de ejes
        Dim fSmall = New Font("Segoe UI", 6.5F)
        gfx.DrawString("φMn (kN·m)", fSmall, Brushes.Black, mL + pW \ 2 - 28, pic.Height - 14)
        Dim st2 = gfx.Save()
        gfx.TranslateTransform(14, mT + pH \ 2 + 22)
        gfx.RotateTransform(-90)
        gfx.DrawString("φPn (kN)", fSmall, Brushes.Black, 0, 0)
        gfx.Restore(st2)
        gfx.DrawString("0", fSmall, Brushes.Black, mL - 9, mT + pH - 8)
        gfx.DrawString($"{CInt(maxM)}", fSmall, Brushes.Black, mL + pW - 18, mT + pH + 3)
        gfx.DrawString($"{CInt(maxP)}", fSmall, Brushes.Black, 2, mT - 2)
        If minP < 0 Then gfx.DrawString($"{CInt(minP)}", fSmall, Brushes.Black, 2, mT + pH - 8)

        gfx.Dispose()
        pic.Image = bmp
    End Sub

    ' ────────────────────────────────────────────────────────
    '  UTILIDADES
    ' ────────────────────────────────────────────────────────
    Private Function GetColumnaActual() As Columna
        If cmbCol.SelectedIndex < 0 Then Return Nothing
        Return Proyecto.Elementos.Columnas.Lista_Columnas.Find(Function(c) c.Name_Label = cmbCol.Text)
    End Function

    Private Function GetTramoActual() As Tramo_Columna
        Dim col = GetColumnaActual()
        If col Is Nothing OrElse cmbTramo.SelectedIndex < 0 Then Return Nothing
        Return col.Lista_Tramos_Columnas.Find(Function(t) t.EsCircular AndAlso t.Piso = cmbTramo.Text)
    End Function

    Private Function BarraDominante(ref As Tramo_Columna.Refuerzo_Longitudinal) As String
        If ref Is Nothing Then Return "#5"
        If ref.Barras_10 > 0 Then Return "#10"
        If ref.Barras_8 > 0 Then Return "#8"
        If ref.Barras_7 > 0 Then Return "#7"
        If ref.Barras_6 > 0 Then Return "#6"
        If ref.Barras_5 > 0 Then Return "#5"
        If ref.Barras_4 > 0 Then Return "#4"
        If ref.Barras_3 > 0 Then Return "#3"
        Return "#2"
    End Function

End Class
