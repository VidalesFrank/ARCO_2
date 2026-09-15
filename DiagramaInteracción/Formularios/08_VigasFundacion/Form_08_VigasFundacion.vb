Imports System.Drawing
Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.Funciones_00_Varias

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 08 — Vigas de Fundación (puntales)
'  Diagrama de Interacción: C/D compresión + C/D tracción + requisitos NSR-10
'  Pu diseño = 0.25 × Aa × P_Columna
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_08_VigasFundacion
    Inherits Form

    Public Property Proyecto As Proyecto

    Private _cargando As Boolean = False

    Private ReadOnly BARRAS As String() = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}

    ' ── Colores ───────────────────────────────────────────────────────────────
    Private ReadOnly ClrOK     As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ClrOKTxt  As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ClrMal    As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ClrMalTxt As Color = ColorTranslator.FromHtml("#9C0006")

    ' ── Controles — lista ─────────────────────────────────────────────────────
    Private WithEvents _lst As New ListBox()
    Private _btnNueva    As New Button()
    Private _btnEliminar As New Button()

    ' ── Controles — datos ─────────────────────────────────────────────────────
    Private _txtNombre      As New TextBox()
    Private _txtNombrePlano As New TextBox()
    Private _txtB   As New TextBox()
    Private _txtH   As New TextBox()
    Private _txtL   As New TextBox()
    Private _txtRec As New TextBox()
    Private _txtFc  As New TextBox()
    Private _txtFy  As New TextBox()
    ' Demanda: P_Columna y Aa → Pu calculado
    Private _txtPColumna As New TextBox()   ' carga máx. columna (kN)
    Private _txtAa       As New TextBox()   ' área tributaria (m²)
    Private _lblPuCalc   As New Label()     ' muestra Pu = 0.25 × Aa × P
    ' Refuerzo
    Private _cmbSup     As New ComboBox()
    Private _nudCantSup As New NumericUpDown()
    Private _cmbInf     As New ComboBox()
    Private _nudCantInf As New NumericUpDown()
    Private _cmbEst     As New ComboBox()
    Private _txtSepEst  As New TextBox()
    Private _nudRamas   As New NumericUpDown()

    ' ── Controles — resultados ────────────────────────────────────────────────
    Private _chart      As New Chart()               ' Diagrama de Interacción
    Private WithEvents _dgvDI   As New DataGridView()   ' C/D compresión + tracción
    Private WithEvents _dgvNorm As New DataGridView()   ' Requisitos normativos
    Private _lblStatus As New Label()

    ' =========================================================================
    Public Sub New()
        Me.Text = "Módulo 08 — Vigas de Fundación (Puntales)"
        Me.Size = New Size(1300, 820)
        Me.MinimumSize = New Size(1000, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        ' ── MenuStrip ─────────────────────────────────────────────────────────
        Dim menu As New MenuStrip() With {
            .BackColor = Color.FromArgb(87, 87, 87), .ForeColor = Color.White,
            .RenderMode = ToolStripRenderMode.Professional}
        menu.Renderer = New ARCOMenuRenderer()

        ' Archivo → Guardar
        Dim mnuArchivo As New ToolStripMenuItem("Archivo") With {.ForeColor = Color.White}
        Dim mnuGuardar As New ToolStripMenuItem("Guardar") With {
            .BackColor = Color.FromArgb(57, 57, 57), .ForeColor = Color.White,
            .ShortcutKeys = Keys.Control Or Keys.S}
        AddHandler mnuGuardar.Click, AddressOf BtnCalcular_Click
        mnuArchivo.DropDownItems.Add(mnuGuardar)
        menu.Items.Add(mnuArchivo)

        ' Reportes
        Dim mnuRep As New ToolStripMenuItem("Reportes") With {.ForeColor = Color.White}
        AddHandler mnuRep.Click, AddressOf BtnReportes_Click
        menu.Items.Add(mnuRep)

        Me.Controls.Add(menu)
        Me.MainMenuStrip = menu

        ' ── Split principal ───────────────────────────────────────────────────
        Dim split As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .SplitterDistance = 240,
            .FixedPanel = FixedPanel.Panel1,
            .BackColor = Color.FromArgb(200, 200, 200)
        }
        Me.Controls.Add(split)
        split.BringToFront()

        ' ── Panel izquierdo (lista) ────────────────────────────────────────────
        Dim pLeft As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(87, 87, 87)
        }
        split.Panel1.Controls.Add(pLeft)

        Dim lblHead As New Label() With {
            .Text = "VIGAS DE FUNDACIÓN",
            .Dock = DockStyle.Top, .Height = 44,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.FromArgb(60, 60, 60)
        }
        pLeft.Controls.Add(lblHead)

        Dim pBtns As New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 50,
            .BackColor = Color.FromArgb(87, 87, 87),
            .Padding = New Padding(8, 8, 8, 8)
        }
        pLeft.Controls.Add(pBtns)

        _btnNueva.Text = "+ Nueva" : _btnNueva.Size = New Size(100, 34)
        _btnNueva.Location = New Point(8, 8)
        EstilarBtn(_btnNueva, Color.FromArgb(33, 150, 90))
        AddHandler _btnNueva.Click, AddressOf BtnNueva_Click
        pBtns.Controls.Add(_btnNueva)

        _btnEliminar.Text = "Eliminar" : _btnEliminar.Size = New Size(100, 34)
        _btnEliminar.Location = New Point(118, 8)
        EstilarBtn(_btnEliminar, Color.FromArgb(180, 50, 50))
        AddHandler _btnEliminar.Click, AddressOf BtnEliminar_Click
        pBtns.Controls.Add(_btnEliminar)

        _lst.Dock = DockStyle.Fill
        _lst.BackColor = Color.FromArgb(65, 65, 65)
        _lst.ForeColor = Color.White
        _lst.Font = New Font("Segoe UI", 10)
        _lst.BorderStyle = BorderStyle.None
        _lst.ItemHeight = 28
        pLeft.Controls.Add(_lst)

        ' ── Panel derecho ─────────────────────────────────────────────────────
        Dim pRight As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White}
        split.Panel2.Controls.Add(pRight)

        ' Barra inferior
        Dim pBar As New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 52,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 8, 10, 8)
        }
        pRight.Controls.Add(pBar)

        Dim btnCalc As New Button() With {
            .Text = "Calcular y Guardar", .Size = New Size(180, 36), .Location = New Point(10, 8)}
        EstilarBtn(btnCalc, Color.FromArgb(0, 150, 70))
        AddHandler btnCalc.Click, AddressOf BtnCalcular_Click
        pBar.Controls.Add(btnCalc)

        Dim btnRep As New Button() With {
            .Text = "Reportes", .Size = New Size(120, 36), .Location = New Point(200, 8)}
        EstilarBtn(btnRep, Color.FromArgb(21, 130, 70))
        AddHandler btnRep.Click, AddressOf BtnReportes_Click
        pBar.Controls.Add(btnRep)

        _lblStatus.Location = New Point(335, 14) : _lblStatus.Size = New Size(500, 24)
        _lblStatus.ForeColor = Color.FromArgb(90, 90, 90)
        pBar.Controls.Add(_lblStatus)

        ' Contenido: entrada de datos (izq) + resultados (der)
        Dim tbl As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2}
        tbl.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 420))
        tbl.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        pRight.Controls.Add(tbl)

        Dim pData As New Panel() With {
            .Dock = DockStyle.Fill, .AutoScroll = True, .Padding = New Padding(12, 8, 12, 0)}
        tbl.Controls.Add(pData, 0, 0)
        ConstruirEntradaDatos(pData)

        Dim pRes As New Panel() With {
            .Dock = DockStyle.Fill, .AutoScroll = True, .Padding = New Padding(4, 8, 8, 4)}
        tbl.Controls.Add(pRes, 1, 0)
        ConstruirResultados(pRes)

        AddHandler Me.Load, AddressOf Form_Load

        ' Actualizar etiqueta Pu cuando cambian los campos de demanda
        AddHandler _txtPColumna.TextChanged, AddressOf ActualizarPuCalc
        AddHandler _txtAa.TextChanged, AddressOf ActualizarPuCalc
    End Sub

    ' =========================================================================
    ' CONSTRUCCIÓN DE UI — ENTRADA DE DATOS
    ' =========================================================================

    Private Sub ConstruirEntradaDatos(pnl As Panel)
        Dim y As Integer = 0

        y = AgregarGrupo(pnl, "IDENTIFICACIÓN", y)
        AgregarFila(pnl, "Nombre (ID):", _txtNombre, y) : y += 32
        AgregarFila(pnl, "Nombre en plano:", _txtNombrePlano, y) : y += 40

        y = AgregarGrupo(pnl, "SECCIÓN Y MATERIALES", y)
        AgregarFila2(pnl, "B (m):", _txtB, "H (m):", _txtH, y) : y += 32
        AgregarFila2(pnl, "L (m):", _txtL, "Rec. (m):", _txtRec, y) : y += 32
        AgregarFila2(pnl, "f'c (MPa):", _txtFc, "fy (MPa):", _txtFy, y) : y += 40

        y = AgregarGrupo(pnl, "DEMANDA AXIAL", y)
        AgregarFila2(pnl, "P_max (kN):", _txtPColumna, "Aa (m²):", _txtAa, y) : y += 32

        ' Etiqueta Pu calculado
        _lblPuCalc.Location = New Point(4, y + 2)
        _lblPuCalc.Size = New Size(390, 22)
        _lblPuCalc.Font = New Font("Segoe UI", 9, FontStyle.Italic)
        _lblPuCalc.ForeColor = Color.FromArgb(50, 90, 160)
        _lblPuCalc.Text = "Pu = 0.25 × Aa × P_max = 0.00 kN"
        pnl.Controls.Add(_lblPuCalc)
        y += 32

        ' Nota
        Dim nota As New Label() With {
            .Text = "  Pu = 0.25 × Aa × P_max  (NSR-10)",
            .Location = New Point(4, y),
            .Size = New Size(390, 18),
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Italic),
            .ForeColor = Color.Gray
        }
        pnl.Controls.Add(nota)
        y += 26

        y = AgregarGrupo(pnl, "REFUERZO LONGITUDINAL Y TRANSVERSAL", y)

        For Each cmb In {_cmbSup, _cmbInf, _cmbEst}
            cmb.Items.AddRange(BARRAS)
            cmb.DropDownStyle = ComboBoxStyle.DropDownList
        Next

        _nudCantSup.Minimum = 1 : _nudCantSup.Maximum = 20 : _nudCantSup.Value = 2
        _nudCantInf.Minimum = 1 : _nudCantInf.Maximum = 20 : _nudCantInf.Value = 3
        _nudRamas.Minimum = 1 : _nudRamas.Maximum = 8 : _nudRamas.Value = 2

        AgregarFilaRefuerzo(pnl, "Sup.:", _cmbSup, "cant:", _nudCantSup, y) : y += 32
        AgregarFilaRefuerzo(pnl, "Inf.:", _cmbInf, "cant:", _nudCantInf, y) : y += 32
        AgregarFilaEstribo(pnl, y)
    End Sub

    ' ── Helpers de layout ─────────────────────────────────────────────────────

    Private Function AgregarGrupo(pnl As Panel, titulo As String, y As Integer) As Integer
        Dim lbl As New Label() With {
            .Text = "  " & titulo,
            .Location = New Point(0, y),
            .Size = New Size(400, 22),
            .BackColor = Color.FromArgb(87, 87, 87),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        }
        pnl.Controls.Add(lbl)
        Return y + 26
    End Function

    Private Sub AgregarFila(pnl As Panel, etiqueta As String, ctrl As Control, y As Integer)
        Dim lbl As New Label() With {
            .Text = etiqueta, .Location = New Point(4, y + 3),
            .Size = New Size(130, 20), .Font = New Font("Segoe UI", 9)
        }
        ctrl.Location = New Point(138, y)
        ctrl.Size = New Size(200, 26)
        If TypeOf ctrl Is TextBox Then DirectCast(ctrl, TextBox).Font = New Font("Segoe UI", 9.5F)
        pnl.Controls.Add(lbl) : pnl.Controls.Add(ctrl)
    End Sub

    Private Sub AgregarFila2(pnl As Panel, lbl1 As String, c1 As Control, lbl2 As String, c2 As Control, y As Integer)
        Dim l1 As New Label() With {.Text = lbl1, .Location = New Point(4, y + 3), .Size = New Size(80, 20), .Font = New Font("Segoe UI", 9)}
        c1.Location = New Point(86, y) : c1.Size = New Size(80, 26)
        If TypeOf c1 Is TextBox Then DirectCast(c1, TextBox).Font = New Font("Segoe UI", 9.5F)
        Dim l2 As New Label() With {.Text = lbl2, .Location = New Point(176, y + 3), .Size = New Size(76, 20), .Font = New Font("Segoe UI", 9)}
        c2.Location = New Point(254, y) : c2.Size = New Size(80, 26)
        If TypeOf c2 Is TextBox Then DirectCast(c2, TextBox).Font = New Font("Segoe UI", 9.5F)
        pnl.Controls.AddRange({l1, c1, l2, c2})
    End Sub

    Private Sub AgregarFilaRefuerzo(pnl As Panel, lbl1 As String, cmb As ComboBox, lbl2 As String, nud As NumericUpDown, y As Integer)
        Dim l1 As New Label() With {.Text = lbl1, .Location = New Point(4, y + 3), .Size = New Size(32, 20), .Font = New Font("Segoe UI", 9)}
        cmb.Location = New Point(38, y) : cmb.Size = New Size(68, 26) : cmb.Font = New Font("Segoe UI", 9.5F)
        Dim l2 As New Label() With {.Text = lbl2, .Location = New Point(112, y + 3), .Size = New Size(38, 20), .Font = New Font("Segoe UI", 9)}
        nud.Location = New Point(152, y) : nud.Size = New Size(60, 26) : nud.Font = New Font("Segoe UI", 9.5F)
        pnl.Controls.AddRange({l1, cmb, l2, nud})
    End Sub

    Private Sub AgregarFilaEstribo(pnl As Panel, y As Integer)
        Dim l1 As New Label() With {.Text = "Est.:", .Location = New Point(4, y + 3), .Size = New Size(32, 20), .Font = New Font("Segoe UI", 9)}
        _cmbEst.Location = New Point(38, y) : _cmbEst.Size = New Size(60, 26) : _cmbEst.Font = New Font("Segoe UI", 9.5F)
        Dim l2 As New Label() With {.Text = "sep(m):", .Location = New Point(104, y + 3), .Size = New Size(52, 20), .Font = New Font("Segoe UI", 9)}
        _txtSepEst.Location = New Point(158, y) : _txtSepEst.Size = New Size(60, 26) : _txtSepEst.Font = New Font("Segoe UI", 9.5F)
        Dim l3 As New Label() With {.Text = "ramas:", .Location = New Point(224, y + 3), .Size = New Size(50, 20), .Font = New Font("Segoe UI", 9)}
        _nudRamas.Location = New Point(276, y) : _nudRamas.Size = New Size(50, 26)
        pnl.Controls.AddRange({l1, _cmbEst, l2, _txtSepEst, l3, _nudRamas})
    End Sub

    ' =========================================================================
    ' CONSTRUCCIÓN DE UI — RESULTADOS
    ' =========================================================================

    Private Sub ConstruirResultados(pnl As Panel)
        ' ── 1. Diagrama de Interacción (chart) ───────────────────────────────
        Dim lblChart As New Label() With {
            .Text = "  DIAGRAMA DE INTERACCIÓN",
            .Dock = DockStyle.Top, .Height = 24,
            .BackColor = Color.FromArgb(87, 87, 87),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        }
        pnl.Controls.Add(lblChart)

        ConfigurarChart()
        _chart.Dock = DockStyle.Top
        _chart.Height = 240
        pnl.Controls.Add(_chart)

        Dim sep1 As New Panel() With {.Dock = DockStyle.Top, .Height = 8, .BackColor = Color.White}
        pnl.Controls.Add(sep1)

        ' ── 2. Tabla C/D ─────────────────────────────────────────────────────
        Dim lblDI As New Label() With {
            .Text = "  VERIFICACIÓN C/D — PUNTAL",
            .Dock = DockStyle.Top, .Height = 24,
            .BackColor = Color.FromArgb(87, 87, 87),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        }
        pnl.Controls.Add(lblDI)

        EstilarGrid(_dgvDI)
        _dgvDI.Dock = DockStyle.Top
        _dgvDI.Height = 96
        pnl.Controls.Add(_dgvDI)

        _dgvDI.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Verif",    .HeaderText = "VERIFICACIÓN",        .Width = 155})
        _dgvDI.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "PhiPn",    .HeaderText = "φPn capacidad (kN)",  .Width = 140})
        _dgvDI.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "PuDem",    .HeaderText = "Pu demanda (kN)",     .Width = 120})
        _dgvDI.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "CD",       .HeaderText = "C/D",                  .Width = 68})
        _dgvDI.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "EstadoDI", .HeaderText = "Estado",               .Width = 80})

        Dim sep2 As New Panel() With {.Dock = DockStyle.Top, .Height = 8, .BackColor = Color.White}
        pnl.Controls.Add(sep2)

        ' ── 3. Requisitos normativos ──────────────────────────────────────────
        Dim lblNorm As New Label() With {
            .Text = "  REQUISITOS NORMATIVOS — NSR-10",
            .Dock = DockStyle.Top, .Height = 24,
            .BackColor = Color.FromArgb(87, 87, 87),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        }
        pnl.Controls.Add(lblNorm)

        EstilarGrid(_dgvNorm)
        _dgvNorm.Dock = DockStyle.Top
        _dgvNorm.Height = 148
        pnl.Controls.Add(_dgvNorm)

        _dgvNorm.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Req",        .HeaderText = "REQUISITO",  .Width = 155})
        _dgvNorm.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Detalle",    .HeaderText = "DETALLE",    .Width = 310})
        _dgvNorm.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "EstadoNorm", .HeaderText = "Estado",     .Width = 80})

        _dgvNorm.Columns("Detalle").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
    End Sub

    Private Sub ConfigurarChart()
        _chart.BackColor = Color.White
        _chart.BorderlineColor = Color.FromArgb(180, 180, 180)
        _chart.BorderlineWidth = 1
        _chart.BorderlineDashStyle = ChartDashStyle.Solid

        Dim area As New ChartArea("DI")
        area.BackColor = Color.White
        area.AxisX.Title = "φMn (kN·m)"
        area.AxisX.TitleFont = New Font("Segoe UI", 8.5F)
        area.AxisY.Title = "φPn (kN)"
        area.AxisY.TitleFont = New Font("Segoe UI", 8.5F)
        area.AxisX.MajorGrid.LineColor = Color.FromArgb(220, 220, 220)
        area.AxisY.MajorGrid.LineColor = Color.FromArgb(220, 220, 220)
        area.AxisX.LabelStyle.Font = New Font("Segoe UI", 7.5F)
        area.AxisY.LabelStyle.Font = New Font("Segoe UI", 7.5F)
        _chart.ChartAreas.Add(area)

        Dim leg As New Legend("L")
        leg.Docking = Docking.Top
        leg.Alignment = StringAlignment.Far
        leg.BackColor = Color.Transparent
        leg.Font = New Font("Segoe UI", 8)
        _chart.Legends.Add(leg)
    End Sub

    ' =========================================================================
    ' CARGA Y GUARDADO
    ' =========================================================================

    Private Sub Form_Load(sender As Object, e As EventArgs)
        ' Ajusta la ventana al monitor y habilita scroll vertical: la maqueta
        ' de este formulario tiene Y y altos fijos y no cabe en pantallas bajas.
        PilaVerticalAdaptable.AjustarAPantallaConScroll(Me)
        If Proyecto Is Nothing Then Return
        RefrescarLista()
        If _lst.Items.Count > 0 Then _lst.SelectedIndex = 0
    End Sub

    Private Sub RefrescarLista()
        Dim sel = _lst.SelectedIndex
        _lst.Items.Clear()
        For Each vf In Proyecto.Elementos.VigasFundacion.Elementos
            _lst.Items.Add(vf)
        Next
        If sel >= 0 AndAlso sel < _lst.Items.Count Then _lst.SelectedIndex = sel
    End Sub

    Private Sub LstVigas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles _lst.SelectedIndexChanged
        If _cargando Then Return
        Dim vf = VigaSeleccionada()
        If vf Is Nothing Then LimpiarForm() : Return
        CargarViga(vf)
    End Sub

    Private Function VigaSeleccionada() As cVigaFundacion
        If _lst.SelectedItem Is Nothing Then Return Nothing
        Return DirectCast(_lst.SelectedItem, cVigaFundacion)
    End Function

    Private Sub CargarViga(vf As cVigaFundacion)
        _cargando = True
        _txtNombre.Text      = vf.Nombre
        _txtNombrePlano.Text = vf.NombrePlano
        _txtB.Text   = vf.B.ToString("F3")
        _txtH.Text   = vf.H.ToString("F3")
        _txtL.Text   = vf.L.ToString("F2")
        _txtRec.Text = vf.Recubrimiento.ToString("F3")
        _txtFc.Text  = vf.fc.ToString("F0")
        _txtFy.Text  = vf.fy.ToString("F0")
        _txtPColumna.Text = vf.P_Columna.ToString("F2")
        _txtAa.Text       = vf.Aa_Tribu.ToString("F4")
        _lblPuCalc.Text = $"Pu = 0.25 × {vf.Aa_Tribu:F4} × {vf.P_Columna:F2} = {vf.Pu:F2} kN"
        SeleccionarBarra(_cmbSup, vf.BarraSup)
        _nudCantSup.Value = Math.Max(1, Math.Min(20, vf.CantSup))
        SeleccionarBarra(_cmbInf, vf.BarraInf)
        _nudCantInf.Value = Math.Max(1, Math.Min(20, vf.CantInf))
        SeleccionarBarra(_cmbEst, vf.BarraEstribo)
        _txtSepEst.Text = vf.SepEstribo.ToString("F3")
        _nudRamas.Value = Math.Max(1, Math.Min(8, vf.RamasEstribo))
        If vf.Calculado Then
            MostrarResultados(vf)
            _lblStatus.Text      = If(vf.Cumple, "Calculado — Cumple", "Calculado — Revisar (C/D < 0.90)")
            _lblStatus.ForeColor = If(vf.Cumple, ClrOKTxt, ClrMalTxt)
        Else
            _dgvDI.Rows.Clear()
            _dgvNorm.Rows.Clear()
            LimpiarChart()
            _lblStatus.Text      = "Sin calcular"
            _lblStatus.ForeColor = Color.FromArgb(130, 130, 130)
        End If
        _cargando = False
    End Sub

    Private Sub LimpiarForm()
        _txtNombre.Text = "" : _txtNombrePlano.Text = ""
        _txtB.Text = "0.300" : _txtH.Text = "0.500" : _txtL.Text = "3.00" : _txtRec.Text = "0.050"
        _txtFc.Text = "21" : _txtFy.Text = "420"
        _txtPColumna.Text = "0" : _txtAa.Text = "0"
        _lblPuCalc.Text = "Pu = 0.25 × Aa × P_max = 0.00 kN"
        _cmbSup.SelectedIndex = 2 : _nudCantSup.Value = 2
        _cmbInf.SelectedIndex = 2 : _nudCantInf.Value = 3
        _cmbEst.SelectedIndex = 1 : _txtSepEst.Text = "0.150" : _nudRamas.Value = 2
        _dgvDI.Rows.Clear() : _dgvNorm.Rows.Clear()
        LimpiarChart()
        _lblStatus.Text = ""
    End Sub

    Private Sub LeerFormEnViga(vf As cVigaFundacion)
        vf.Nombre      = _txtNombre.Text.Trim()
        vf.NombrePlano = _txtNombrePlano.Text.Trim()
        If String.IsNullOrWhiteSpace(vf.Nombre) Then vf.Nombre = "VF"
        vf.B            = ParseDbl(_txtB.Text, 0.3)
        vf.H            = ParseDbl(_txtH.Text, 0.5)
        vf.L            = ParseDbl(_txtL.Text, 3.0)
        vf.Recubrimiento = ParseDbl(_txtRec.Text, 0.05)
        vf.fc           = ParseDbl(_txtFc.Text, 21)
        vf.fy           = ParseDbl(_txtFy.Text, 420)
        vf.P_Columna    = ParseDbl(_txtPColumna.Text, 0)
        vf.Aa_Tribu     = ParseDbl(_txtAa.Text, 0)
        vf.Pu           = 0.25 * vf.Aa_Tribu * vf.P_Columna
        vf.BarraSup     = If(_cmbSup.SelectedItem?.ToString(), "#4")
        vf.CantSup      = CInt(_nudCantSup.Value)
        vf.BarraInf     = If(_cmbInf.SelectedItem?.ToString(), "#4")
        vf.CantInf      = CInt(_nudCantInf.Value)
        vf.BarraEstribo = If(_cmbEst.SelectedItem?.ToString(), "#3")
        vf.SepEstribo   = ParseDbl(_txtSepEst.Text, 0.15)
        vf.RamasEstribo = CInt(_nudRamas.Value)
    End Sub

    ' Actualiza la etiqueta Pu en tiempo real al editar los campos de demanda
    Private Sub ActualizarPuCalc(sender As Object, e As EventArgs)
        If _cargando Then Return
        Dim pCol = ParseDbl(_txtPColumna.Text, 0)
        Dim aa   = ParseDbl(_txtAa.Text, 0)
        Dim pu   = 0.25 * aa * pCol
        _lblPuCalc.Text = $"Pu = 0.25 × {aa:F4} × {pCol:F2} = {pu:F2} kN"
    End Sub

    ' =========================================================================
    ' HANDLERS DE BOTONES
    ' =========================================================================

    Private Sub BtnNueva_Click(sender As Object, e As EventArgs)
        Dim contador = Proyecto.Elementos.VigasFundacion.Elementos.Count + 1
        Dim vf As New cVigaFundacion() With {.Nombre = $"VF-{contador}"}
        Proyecto.Elementos.VigasFundacion.Elementos.Add(vf)
        RefrescarLista()
        _lst.SelectedIndex = _lst.Items.Count - 1
    End Sub

    Private Sub BtnEliminar_Click(sender As Object, e As EventArgs)
        Dim vf = VigaSeleccionada()
        If vf Is Nothing Then Return
        Dim res = MessageBox.Show($"¿Eliminar la viga ""{vf.Nombre}""?",
                                  "Eliminar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If res <> DialogResult.Yes Then Return
        Proyecto.Elementos.VigasFundacion.Elementos.Remove(vf)
        RefrescarLista()
        If _lst.Items.Count > 0 Then _lst.SelectedIndex = 0 Else LimpiarForm()
    End Sub

    Private Sub BtnCalcular_Click(sender As Object, e As EventArgs)
        Dim vf = VigaSeleccionada()
        If vf Is Nothing Then
            MessageBox.Show("Seleccione o cree una viga primero.", "Sin selección",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Try
            LeerFormEnViga(vf)
            Dim phiMnLst As List(Of Single) = Nothing
            Dim phiPnLst As List(Of Single) = Nothing
            CalcularViga(vf, phiMnLst, phiPnLst)
            Dim idx = _lst.SelectedIndex
            _cargando = True
            _lst.Items(idx) = vf
            _lst.SelectedIndex = idx
            _cargando = False
            MostrarResultados(vf, phiMnLst, phiPnLst)
            _lblStatus.Text      = If(vf.Cumple, "Calculado — Cumple", "Calculado — Revisar (C/D < 0.90)")
            _lblStatus.ForeColor = If(vf.Cumple, ClrOKTxt, ClrMalTxt)
        Catch ex As Exception
            MessageBox.Show("Error al calcular: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnReportes_Click(sender As Object, e As EventArgs)
        Dim hayCalculado = Proyecto.Elementos.VigasFundacion.Elementos.Any(Function(v) v.Calculado)
        If Not hayCalculado Then
            MessageBox.Show("Calcule al menos una viga antes de abrir el reporte.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim rep As New Form_Reporte_VigasFundacion()
        rep.VigasFundacion = Proyecto.Elementos.VigasFundacion
        rep.Show(Me)
    End Sub

    ' =========================================================================
    ' CÁLCULO — Diagrama de Interacción + Requisitos NSR-10
    ' =========================================================================

    Private Sub CalcularViga(vf As cVigaFundacion,
                              ByRef phiMnOut As List(Of Single),
                              ByRef phiPnOut As List(Of Single))
        Dim B   As Single = CSng(vf.B)
        Dim H   As Single = CSng(vf.H)
        Dim rec As Double = vf.Recubrimiento
        Dim fc  As Single = CSng(vf.fc)
        Dim fy  As Single = CSng(vf.fy)

        ' ── Armar lista de refuerzo ───────────────────────────────────────────
        Dim lista As New List(Of RefuerzoSimple)()
        Dim id As Integer = 0

        Dim dbSup  As Single = CSng(DiametroRefuerzo(vf.BarraSup))
        Dim asbSup As Single = CSng(AreaRefuerzo(vf.BarraSup))
        Dim ySup   As Single = CSng(H / 2.0 - rec)
        Dim bEfSup As Single = CSng(vf.B - 2.0 * rec)
        Dim sepSup As Single = If(vf.CantSup > 1, bEfSup / (vf.CantSup - 1), 0)
        For i = 0 To vf.CantSup - 1
            Dim xb = If(vf.CantSup > 1, CSng(-vf.B / 2.0 + rec + i * sepSup), 0.0F)
            lista.Add(New RefuerzoSimple(id, vf.BarraSup, dbSup, asbSup, xb, ySup))
            id += 1
        Next

        Dim dbInf  As Single = CSng(DiametroRefuerzo(vf.BarraInf))
        Dim asbInf As Single = CSng(AreaRefuerzo(vf.BarraInf))
        Dim yInf   As Single = CSng(-(H / 2.0 - rec))
        Dim bEfInf As Single = CSng(vf.B - 2.0 * rec)
        Dim sepInf As Single = If(vf.CantInf > 1, bEfInf / (vf.CantInf - 1), 0)
        For i = 0 To vf.CantInf - 1
            Dim xb = If(vf.CantInf > 1, CSng(-vf.B / 2.0 + rec + i * sepInf), 0.0F)
            lista.Add(New RefuerzoSimple(id, vf.BarraInf, dbInf, asbInf, xb, yInf))
            id += 1
        Next

        ' ── Diagrama de interacción ───────────────────────────────────────────
        Dim result   = Funcion_Diagrama_Interaccion(B, H, lista, fc, fy, 200000, 0)
        phiMnOut = result.Item1
        phiPnOut = result.Item2

        vf.PhiPn_Max = phiPnOut.Max()
        vf.PhiPn_Min = phiPnOut.Min()

        ' Pu diseño = 0.25 × Aa × P_Columna (ya calculado en LeerFormEnViga)
        Dim pu As Double = vf.Pu
        If pu > 0.1 Then
            vf.CD_Comp = vf.PhiPn_Max / pu
            vf.CD_Trac = Math.Abs(vf.PhiPn_Min) / pu
        Else
            vf.CD_Comp = 9.99
            vf.CD_Trac = 9.99
        End If

        ' ── Áreas de acero ────────────────────────────────────────────────────
        vf.As_Prov_Sup = vf.CantSup * AreaRefuerzo(vf.BarraSup) / 100.0
        vf.As_Prov_Inf = vf.CantInf * AreaRefuerzo(vf.BarraInf) / 100.0

        ' ── Requisito 1: dimensiones (max(B,H) ≥ L/20) ───────────────────────
        Dim dimMax = Math.Max(vf.B, vf.H)
        Dim hMin   = vf.L / 20.0
        vf.CumpleDim  = (dimMax >= hMin)
        vf.DetalleDim = $"max(B,H) = {dimMax * 100:F0} cm,  L/20 = {hMin * 100:F0} cm"

        ' ── Requisito 2: cuantía mín. ρ ≥ 0.33% ──────────────────────────────
        Dim agEfect  = vf.B * (vf.H - vf.Recubrimiento)
        Dim rhoSup   = (vf.As_Prov_Sup / 10000.0) / agEfect
        Dim rhoInf   = (vf.As_Prov_Inf / 10000.0) / agEfect
        Dim rhoMin   = Math.Min(rhoSup, rhoInf)
        vf.CumpleCuantia  = (rhoMin >= 0.0033)
        vf.DetalleCuantia = $"ρ_mín = {rhoMin * 100:F3}%  (mín = 0.33%)"

        ' ── Requisito 3: separación estribos ──────────────────────────────────
        Dim sepMax = Math.Min(Math.Min(vf.B, vf.H) / 2.0, 0.3)
        vf.CumpleEstribo  = (vf.SepEstribo <= sepMax)
        vf.DetalleEstribo = $"s = {vf.SepEstribo * 100:F0} cm,  s_máx = {sepMax * 100:F0} cm"

        ' ── Estado general ────────────────────────────────────────────────────
        vf.CumpleDI        = (vf.CD_Comp >= 0.9) AndAlso (vf.CD_Trac >= 0.9)
        vf.CumpleNormativo = vf.CumpleDim AndAlso vf.CumpleCuantia AndAlso vf.CumpleEstribo
        vf.Cumple    = vf.CumpleDI AndAlso vf.CumpleNormativo
        vf.Calculado = True
    End Sub

    ' =========================================================================
    ' RESULTADOS EN GRIDS + CHART
    ' =========================================================================

    ' Llamado desde BtnCalcular (tiene las listas) o desde CargarViga (recalcula curva)
    Private Sub MostrarResultados(vf As cVigaFundacion,
                                   Optional phiMnLst As List(Of Single) = Nothing,
                                   Optional phiPnLst As List(Of Single) = Nothing)
        ' Si no se pasan las listas (carga de viga guardada) se recalcula la curva
        If phiMnLst Is Nothing OrElse phiPnLst Is Nothing Then
            Dim dummy1 As List(Of Single) = Nothing
            Dim dummy2 As List(Of Single) = Nothing
            CalcularViga(vf, dummy1, dummy2)
            phiMnLst = dummy1
            phiPnLst = dummy2
        End If

        ' Grilla C/D
        _dgvDI.Rows.Clear()
        AgregarFilaDI("Compresión (φPn_max)", vf.PhiPn_Max, vf.Pu, vf.CD_Comp)
        AgregarFilaDI("Tracción  (φPn_min)",  Math.Abs(vf.PhiPn_Min), vf.Pu, vf.CD_Trac)

        ' Grilla normativa
        _dgvNorm.Rows.Clear()
        AgregarFilaNorm("Dimensiones mín.",  vf.DetalleDim,     vf.CumpleDim)
        AgregarFilaNorm("Cuantía mín. (ρ)",  vf.DetalleCuantia, vf.CumpleCuantia)
        AgregarFilaNorm("Sep. estribos",      vf.DetalleEstribo, vf.CumpleEstribo)

        ' Chart
        If phiMnLst IsNot Nothing AndAlso phiMnLst.Count > 0 Then
            ActualizarChart(vf, phiMnLst, phiPnLst)
        End If
    End Sub

    Private Sub AgregarFilaDI(verif As String, phiPn As Double, pu As Double, cd As Double)
        Dim r   = _dgvDI.Rows.Add()
        Dim row = _dgvDI.Rows(r)
        row.Cells("Verif").Value  = verif
        row.Cells("PhiPn").Value  = Math.Round(phiPn, 2).ToString("F2")
        row.Cells("PuDem").Value  = Math.Round(pu, 2).ToString("F2")
        PintarCD(row.Cells("CD"), cd)
        Dim cumple = (cd >= 0.9)
        row.Cells("EstadoDI").Value                = If(cumple, "OK", "Revisar")
        row.Cells("EstadoDI").Style.BackColor      = If(cumple, ClrOK, ClrMal)
        row.Cells("EstadoDI").Style.ForeColor      = If(cumple, ClrOKTxt, ClrMalTxt)
        row.Cells("EstadoDI").Style.Font           = New Font("Segoe UI", 9.5F, FontStyle.Bold)
    End Sub

    Private Sub AgregarFilaNorm(req As String, detalle As String, cumple As Boolean)
        Dim r   = _dgvNorm.Rows.Add()
        Dim row = _dgvNorm.Rows(r)
        row.Cells("Req").Value                 = req
        row.Cells("Detalle").Value             = detalle
        row.Cells("EstadoNorm").Value          = If(cumple, "OK", "Revisar")
        row.Cells("EstadoNorm").Style.BackColor = If(cumple, ClrOK, ClrMal)
        row.Cells("EstadoNorm").Style.ForeColor = If(cumple, ClrOKTxt, ClrMalTxt)
        row.Cells("EstadoNorm").Style.Font      = New Font("Segoe UI", 9.5F, FontStyle.Bold)
    End Sub

    Private Sub PintarCD(cell As DataGridViewCell, cd As Double)
        Dim v = Math.Min(cd, 9.99)
        cell.Value = v.ToString("F2")
        If cd >= 0.9 Then
            cell.Style.BackColor = ClrOK  : cell.Style.ForeColor = ClrOKTxt
        Else
            cell.Style.BackColor = ClrMal : cell.Style.ForeColor = ClrMalTxt
        End If
        cell.Style.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
    End Sub

    ' ── Chart DI ──────────────────────────────────────────────────────────────

    Private Sub ActualizarChart(vf As cVigaFundacion,
                                  phiMnLst As List(Of Single),
                                  phiPnLst As List(Of Single))
        _chart.Series.Clear()

        ' Curva DI
        Dim sDI As New Series("Diagrama DI")
        sDI.ChartType   = SeriesChartType.Spline
        sDI.BorderWidth = 2
        sDI.Color       = Color.SteelBlue
        sDI.ChartArea   = "DI"
        sDI.Legend      = "L"
        For i = 0 To phiMnLst.Count - 1
            sDI.Points.AddXY(phiMnLst(i), phiPnLst(i))
        Next
        _chart.Series.Add(sDI)

        ' Punto de demanda Pu (a M=0)
        Dim sPu As New Series("Pu diseño")
        sPu.ChartType   = SeriesChartType.Point
        sPu.MarkerStyle = MarkerStyle.Cross
        sPu.MarkerSize  = 12
        sPu.Color       = Color.Red
        sPu.ChartArea   = "DI"
        sPu.Legend      = "L"
        sPu.Points.AddXY(0, vf.Pu)
        _chart.Series.Add(sPu)

        ' Ajustar ejes
        Dim area = _chart.ChartAreas("DI")
        Dim pnMax = CSng(phiPnLst.Max() * 1.15)
        Dim pnMin = CSng(phiPnLst.Min() * 1.15)
        Dim mnMax = CSng(phiMnLst.Max() * 1.2)
        Dim stepY = Math.Max(100, Math.Round((pnMax - pnMin) / 8 / 100.0) * 100)
        Dim stepX = Math.Max(10, Math.Round(mnMax / 5 / 10.0) * 10)

        area.AxisY.Minimum = Math.Floor(pnMin / stepY) * stepY
        area.AxisY.Maximum = Math.Ceiling(pnMax / stepY) * stepY
        area.AxisY.Interval = stepY
        area.AxisX.Minimum  = 0
        area.AxisX.Maximum  = Math.Ceiling(mnMax / stepX) * stepX
        area.AxisX.Interval = stepX
    End Sub

    Private Sub LimpiarChart()
        _chart.Series.Clear()
        Dim area = _chart.ChartAreas("DI")
        area.AxisX.Minimum = Double.NaN
        area.AxisX.Maximum = Double.NaN
        area.AxisY.Minimum = Double.NaN
        area.AxisY.Maximum = Double.NaN
    End Sub

    ' =========================================================================
    ' HELPERS
    ' =========================================================================

    Private Shared Function ParseDbl(s As String, def As Double) As Double
        Dim v As Double
        Return If(Double.TryParse(s.Replace(",", "."),
                  Globalization.NumberStyles.Any,
                  Globalization.CultureInfo.InvariantCulture, v), v, def)
    End Function

    Private Sub SeleccionarBarra(cmb As ComboBox, barra As String)
        Dim idx = Array.IndexOf(BARRAS, barra)
        cmb.SelectedIndex = If(idx >= 0, idx, 2)
    End Sub

    Private Sub EstilarBtn(btn As Button, color As Color)
        btn.FlatStyle = FlatStyle.Flat
        btn.BackColor = color
        btn.ForeColor = Color.White
        btn.Font      = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        btn.Cursor    = Cursors.Hand
        btn.FlatAppearance.BorderSize = 0
    End Sub

    Private Sub EstilarGrid(dgv As DataGridView)
        dgv.AllowUserToAddRows = False
        dgv.ReadOnly           = True
        dgv.SelectionMode      = DataGridViewSelectionMode.FullRowSelect
        dgv.MultiSelect        = False
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.RowHeadersVisible  = False
        dgv.BackgroundColor    = Color.White
        dgv.BorderStyle        = BorderStyle.None
        dgv.CellBorderStyle    = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.GridColor          = Color.FromArgb(210, 210, 210)
        dgv.ColumnHeadersHeight = 36
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.RowTemplate.Height = 30
        dgv.EnableHeadersVisualStyles = False
        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor = Color.FromArgb(87, 87, 87)
            .ForeColor = Color.White
            .Font      = New Font("Segoe UI", 9, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
        With dgv.DefaultCellStyle
            .BackColor = Color.White
            .Font      = New Font("Segoe UI", 9.5F)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

End Class
