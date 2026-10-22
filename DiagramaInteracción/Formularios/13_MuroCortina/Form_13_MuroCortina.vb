Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 13 — MC. Cortina (muro de contención tipo cortina)
'
'  Pantalla vertical apoyada en altura por losas u otras estructuras. UI similar
'  al Form_12_MuroContencion pero con dos tablas distintivas:
'
'    - Tramos: altura + espesor (editables).
'    - Apoyos: tipo (Empotrado/Simple/Libre). Niveles son derivados —
'      la UI mantiene siempre Apoyos.Count = Tramos.Count + 1 y los niveles
'      son la suma acumulada de las alturas.
'
'  Nombres default: "Base", "P02", "P03"… para el apoyo superior. Editables.
'
'  Dibujo orientado VERTICALMENTE (pantalla vertical) con zoom in/out con la
'  rueda del mouse y pan con drag, igual que el Form_12.
' ═══════════════════════════════════════════════════════════════════════════════
<DesignerCategory("Code")>
Public Class Form_13_MuroCortina
    Inherits Form

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Estado
    ' ═══════════════════════════════════════════════════════════════════════════
    Private _muro As MuroCortina
    Private _rutaArchivo As String = ""
    Private _dirty As Boolean = False
    Private _cargandoControles As Boolean = False

    ' Paleta (consistente con el Form_12)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly VerdeARCO As Color = Color.FromArgb(27, 175, 122)
    Private Shared ReadOnly RojoARCO As Color = Color.FromArgb(227, 73, 72)
    Private Shared ReadOnly ColorBoton As Color = Color.FromArgb(224, 224, 224)
    Private Shared ReadOnly ColorBg As Color = Color.FromArgb(245, 248, 252)

    ' Colores del dibujo
    Private Shared ReadOnly ColorConcreto As Color = Color.FromArgb(205, 205, 212)
    Private Shared ReadOnly ColorConcretoBorde As Color = Color.FromArgb(55, 55, 65)
    Private Shared ReadOnly ColorTierra As Color = Color.FromArgb(185, 155, 95)
    Private Shared ReadOnly ColorTierraFondo As Color = Color.FromArgb(245, 232, 196)
    Private Shared ReadOnly ColorAgua As Color = Color.FromArgb(80, 130, 200)
    Private Shared ReadOnly ColorCota As Color = Color.FromArgb(50, 50, 60)
    Private Shared ReadOnly ColorEmpuje As Color = Color.FromArgb(227, 73, 72)
    Private Shared ReadOnly ColorEmpujeAgua As Color = Color.FromArgb(60, 110, 185)

    ' Controles principales
    Private _lblRuta As Label
    Private _lblEstado As Label
    Private _tabs As TabControl
    Private _dibujo As PanelDobleBuffer
    Private _layoutMain As TableLayoutPanel
    Private _layoutDer As TableLayoutPanel

    ' Zoom/pan
    Private _zoomUser As Double = 1.0
    Private _panUserX As Double = 0
    Private _panUserY As Double = 0
    Private _arrastrando As Boolean = False
    Private _ultimoMouse As Point
    Private _btnZoomIn, _btnZoomOut, _btnFit As Button
    Private _lblZoomInfo As Label

    ' Entradas — Info
    Private _txtNombre, _txtProyecto, _txtCliente, _txtResponsable, _txtCodigo, _txtDescripcion As TextBox
    Private _dtpFecha As DateTimePicker

    ' Tramos y Apoyos (DataGridView)
    Private _gridTramos As DataGridView
    Private _gridApoyos As DataGridView
    Private _btnAgregarTramo, _btnEliminarTramo As Button

    ' Suelo
    Private _nudPhi, _nudDelta, _nudGamma, _nudGammaSat, _nudKaManual As NumericUpDown
    Private _cmbTeoria As ComboBox
    Private _chkCalcAuto As CheckBox
    Private _chkSobrecarga As CheckBox
    Private _nudSobrecarga As NumericUpDown
    Private _chkHidrostatica As CheckBox
    Private _nudNivelFreatico As NumericUpDown
    Private _chkSismo As CheckBox
    Private _nudFactorSismico As NumericUpDown

    ' Materiales / Normativa
    Private _cmbNormativa As ComboBox
    Private _nudFc, _nudFy, _nudPesoC, _nudRecubExt, _nudRecubInt As NumericUpDown

    ' Grilla de diseño
    Private _gridDiseno As DataGridView
    Private _lblVeredicto As Label

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Punto de entrada
    ' ═══════════════════════════════════════════════════════════════════════════
    Public Shared Sub Mostrar()
        Dim f As New Form_13_MuroCortina()
        f.Show()
    End Sub

    Public Sub New()
        _muro = CrearMuroDefault()
        BuildUI()
        CargarControlesDesdeMuro()
        _dirty = False
        ActualizarTitulo()
    End Sub

    Private Shared Function CrearMuroDefault() As MuroCortina
        Dim m As New MuroCortina()
        ' 1 tramo de 2.5 m × 0.20 m
        m.Tramos.Add(New TramoCortina With {.Nombre = "Tramo 1", .Altura = 2.5, .Espesor = 0.20})
        ' 2 apoyos: Base (empotrado) + P02 (simplemente apoyado)
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "Base", .Nivel = 0, .Tipo = eTipoApoyoCortina.Empotrado})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "P02", .Nivel = 2.5, .Tipo = eTipoApoyoCortina.SimplementeApoyado})
        Return m
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Construcción UI
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub BuildUI()
        Me.Text = "MC. Cortina — ARCO"
        Me.Size = New Size(1280, 820)
        Me.MinimumSize = New Size(1080, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = ColorBg
        Me.Font = New Font("Segoe UI", 9)
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ' --- Header ---
        Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 56, .BackColor = GrisARCO}
        Dim lblTit As New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 13, FontStyle.Bold),
            .Text = "   Análisis y Revisión de Muros de Contención Tipo Cortina",
            .TextAlign = ContentAlignment.MiddleLeft
        }
        header.Controls.Add(lblTit)

        ' --- Toolbar ---
        Dim toolbar As New Panel With {.Dock = DockStyle.Top, .Height = 48, .BackColor = Color.FromArgb(60, 60, 60), .Padding = New Padding(6)}
        Dim btnNuevo = CrearBoton("Nuevo", ColorBoton, GrisARCO, 90)
        Dim btnAbrir = CrearBoton("Abrir…", ColorBoton, GrisARCO, 90)
        Dim btnGuardar = CrearBoton("Guardar", ColorBoton, GrisARCO, 90)
        Dim btnGuardarComo = CrearBoton("Guardar como…", ColorBoton, GrisARCO, 110)
        Dim btnAnalizar = CrearBoton("ANALIZAR", VerdeARCO, Color.White, 110)
        btnAnalizar.Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold)
        Dim btnReporte = CrearBoton("Reporte .xlsx", ColorBoton, GrisARCO, 110)
        _lblRuta = New Label With {
            .Dock = DockStyle.Right, .Width = 400,
            .ForeColor = Color.FromArgb(210, 210, 210),
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .TextAlign = ContentAlignment.MiddleRight,
            .Padding = New Padding(0, 0, 10, 0),
            .Text = "(sin guardar)"
        }
        AddHandler btnNuevo.Click, AddressOf CmdNuevo
        AddHandler btnAbrir.Click, AddressOf CmdAbrir
        AddHandler btnGuardar.Click, AddressOf CmdGuardar
        AddHandler btnGuardarComo.Click, AddressOf CmdGuardarComo
        AddHandler btnAnalizar.Click, AddressOf CmdAnalizar
        AddHandler btnReporte.Click, AddressOf CmdReporte
        btnNuevo.Location = New Point(6, 6)
        btnAbrir.Location = New Point(100, 6)
        btnGuardar.Location = New Point(194, 6)
        btnGuardarComo.Location = New Point(288, 6)
        btnAnalizar.Location = New Point(410, 6)
        btnReporte.Location = New Point(524, 6)
        toolbar.Controls.AddRange(New Control() {btnNuevo, btnAbrir, btnGuardar, btnGuardarComo, btnAnalizar, btnReporte, _lblRuta})

        ' --- Status bar ---
        Dim statusBar As New Panel With {.Dock = DockStyle.Bottom, .Height = 36, .BackColor = Color.FromArgb(50, 50, 50)}
        _lblEstado = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.FromArgb(220, 220, 220),
            .Font = New Font("Segoe UI", 9),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(10, 0, 0, 0),
            .Text = "Listo. Ajuste las entradas y pulse ANALIZAR."
        }
        _lblVeredicto = New Label With {
            .Dock = DockStyle.Right, .Width = 300,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Padding = New Padding(0, 0, 10, 0),
            .Text = ""
        }
        statusBar.Controls.Add(_lblEstado)
        statusBar.Controls.Add(_lblVeredicto)

        ' --- Layout principal ---
        _layoutMain = New TableLayoutPanel With {
            .Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .BackColor = ColorBg
        }
        _layoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 440))
        _layoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        _layoutMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        _tabs = ConstruirTabs()
        _tabs.Dock = DockStyle.Fill
        _layoutMain.Controls.Add(_tabs, 0, 0)

        _layoutDer = New TableLayoutPanel With {
            .Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .BackColor = ColorBg
        }
        _layoutDer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        _layoutDer.RowStyles.Add(New RowStyle(SizeType.Percent, 65))     ' dibujo
        _layoutDer.RowStyles.Add(New RowStyle(SizeType.Absolute, 28))    ' título diseño
        _layoutDer.RowStyles.Add(New RowStyle(SizeType.Percent, 35))     ' grid diseño

        _dibujo = New PanelDobleBuffer With {
            .Dock = DockStyle.Fill, .BackColor = Color.White, .Cursor = Cursors.Hand
        }
        AddHandler _dibujo.Paint, AddressOf Dibujo_Paint
        AddHandler _dibujo.Resize, AddressOf Dibujo_Resize
        AddHandler _dibujo.MouseWheel, AddressOf Dibujo_MouseWheel
        AddHandler _dibujo.MouseDown, AddressOf Dibujo_MouseDown
        AddHandler _dibujo.MouseMove, AddressOf Dibujo_MouseMove
        AddHandler _dibujo.MouseUp, AddressOf Dibujo_MouseUp
        AddHandler _dibujo.MouseEnter, Sub() _dibujo.Focus()

        ' Botones flotantes
        _btnZoomIn = CrearBotonFlotante("+", "Zoom +")
        _btnZoomOut = CrearBotonFlotante("−", "Zoom −")
        _btnFit = CrearBotonFlotante("⤢", "Ajustar vista")
        AddHandler _btnZoomIn.Click, Sub() AplicarZoom(1.25, CentroDibujo())
        AddHandler _btnZoomOut.Click, Sub() AplicarZoom(1.0 / 1.25, CentroDibujo())
        AddHandler _btnFit.Click, AddressOf AjustarVistaDibujo
        _lblZoomInfo = New Label With {
            .Text = "Zoom: 100%",
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .ForeColor = GrisARCO,
            .BackColor = Color.FromArgb(230, 240, 250),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(84, 22),
            .BorderStyle = BorderStyle.FixedSingle
        }
        _dibujo.Controls.Add(_btnZoomIn)
        _dibujo.Controls.Add(_btnZoomOut)
        _dibujo.Controls.Add(_btnFit)
        _dibujo.Controls.Add(_lblZoomInfo)

        _layoutDer.Controls.Add(_dibujo, 0, 0)

        ' Título del bloque "Diseño"
        Dim lblTitDiseno As New Label With {
            .Text = "  Diseño y revisión de refuerzo (edite barra y separación)",
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(70, 70, 70),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        _layoutDer.Controls.Add(lblTitDiseno, 0, 1)

        _gridDiseno = ConstruirGridDiseno()
        _layoutDer.Controls.Add(_gridDiseno, 0, 2)

        _layoutMain.Controls.Add(_layoutDer, 1, 0)

        Me.Controls.Add(_layoutMain)
        Me.Controls.Add(statusBar)
        Me.Controls.Add(toolbar)
        Me.Controls.Add(header)
    End Sub

    Private Function CrearBoton(texto As String, bg As Color, fg As Color, ancho As Integer) As Button
        Dim b As New Button With {
            .Text = texto, .BackColor = bg, .ForeColor = fg,
            .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 9),
            .Size = New Size(ancho, 32), .Cursor = Cursors.Hand
        }
        b.FlatAppearance.BorderSize = 0
        Return b
    End Function

    Private Function CrearBotonFlotante(texto As String, tooltip As String) As Button
        Dim b As New Button With {
            .Text = texto, .Size = New Size(30, 30),
            .BackColor = Color.White, .ForeColor = GrisARCO,
            .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Cursor = Cursors.Hand, .TabStop = False
        }
        b.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
        b.FlatAppearance.BorderSize = 1
        Dim tt As New ToolTip() : tt.SetToolTip(b, tooltip)
        Return b
    End Function

    Private Function ConstruirTabs() As TabControl
        Dim tc As New TabControl With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 9)}

        ' --- INFO ---
        Dim tInfo As New TabPage("Información")
        Dim pInfo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim y As Integer = 8
        pInfo.Controls.Add(Et("Nombre del muro", y)) : y += 20
        _txtNombre = Tx(y, 400) : pInfo.Controls.Add(_txtNombre) : y += 32
        pInfo.Controls.Add(Et("Proyecto", y)) : y += 20
        _txtProyecto = Tx(y, 400) : pInfo.Controls.Add(_txtProyecto) : y += 32
        pInfo.Controls.Add(Et("Cliente", y)) : y += 20
        _txtCliente = Tx(y, 400) : pInfo.Controls.Add(_txtCliente) : y += 32
        pInfo.Controls.Add(Et("Responsable", y)) : y += 20
        _txtResponsable = Tx(y, 400) : pInfo.Controls.Add(_txtResponsable) : y += 32
        pInfo.Controls.Add(Et("Código", y)) : y += 20
        _txtCodigo = Tx(y, 200) : pInfo.Controls.Add(_txtCodigo) : y += 32
        pInfo.Controls.Add(Et("Fecha", y)) : y += 20
        _dtpFecha = New DateTimePicker With {.Location = New Point(0, y), .Width = 200, .Format = DateTimePickerFormat.Long}
        AddHandler _dtpFecha.ValueChanged, AddressOf OnInputChanged
        pInfo.Controls.Add(_dtpFecha) : y += 32
        pInfo.Controls.Add(Et("Descripción", y)) : y += 20
        _txtDescripcion = New TextBox With {.Location = New Point(0, y), .Width = 400, .Height = 60, .Multiline = True}
        AddHandler _txtDescripcion.TextChanged, AddressOf OnInputChanged
        pInfo.Controls.Add(_txtDescripcion)
        tInfo.Controls.Add(pInfo)

        ' --- TRAMOS ---
        Dim tTramos As New TabPage("Tramos")
        Dim pTramos As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .BackColor = Color.White}
        Dim lbl1 As New Label With {
            .Text = "Tramos de la pantalla (de abajo hacia arriba)",
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = GrisARCO, .Dock = DockStyle.Top, .Height = 24
        }
        pTramos.Controls.Add(lbl1)

        _gridTramos = ConstruirGridTramos()
        _gridTramos.Dock = DockStyle.Fill
        pTramos.Controls.Add(_gridTramos)
        lbl1.BringToFront()

        Dim pBtnsT As New Panel With {.Dock = DockStyle.Bottom, .Height = 40, .BackColor = Color.White, .Padding = New Padding(0, 8, 0, 0)}
        _btnAgregarTramo = CrearBoton("+ Agregar tramo", VerdeARCO, Color.White, 160)
        _btnEliminarTramo = CrearBoton("− Eliminar último", ColorBoton, GrisARCO, 160)
        _btnAgregarTramo.Location = New Point(0, 4)
        _btnEliminarTramo.Location = New Point(170, 4)
        AddHandler _btnAgregarTramo.Click, AddressOf CmdAgregarTramo
        AddHandler _btnEliminarTramo.Click, AddressOf CmdEliminarTramo
        pBtnsT.Controls.AddRange(New Control() {_btnAgregarTramo, _btnEliminarTramo})
        pTramos.Controls.Add(pBtnsT)
        tTramos.Controls.Add(pTramos)

        ' --- APOYOS ---
        Dim tApoyos As New TabPage("Apoyos")
        Dim pApoyos As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .BackColor = Color.White}
        Dim lbl2 As New Label With {
            .Text = "Apoyos de la pantalla (orden ascendente; nivel se calcula desde los tramos)",
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = GrisARCO, .Dock = DockStyle.Top, .Height = 24
        }
        pApoyos.Controls.Add(lbl2)
        _gridApoyos = ConstruirGridApoyos()
        _gridApoyos.Dock = DockStyle.Fill
        pApoyos.Controls.Add(_gridApoyos)
        lbl2.BringToFront()
        tApoyos.Controls.Add(pApoyos)

        ' --- SUELO ---
        Dim tSuelo As New TabPage("Suelo y empuje")
        Dim pSuelo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim yS As Integer = 8
        pSuelo.Controls.Add(Et("Teoría de empuje", yS)) : yS += 20
        _cmbTeoria = New ComboBox With {.Location = New Point(0, yS), .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
        _cmbTeoria.Items.AddRange(New String() {"Rankine (sin fricción muro-suelo)", "Coulomb (con fricción)"})
        AddHandler _cmbTeoria.SelectedIndexChanged, AddressOf OnInputChanged
        pSuelo.Controls.Add(_cmbTeoria) : yS += 36
        _chkCalcAuto = New CheckBox With {.Text = "Calcular Ka automáticamente", .Location = New Point(0, yS), .AutoSize = True, .Checked = True}
        AddHandler _chkCalcAuto.CheckedChanged, Sub()
                                                     OnInputChanged(Nothing, Nothing)
                                                     _nudKaManual.Enabled = Not _chkCalcAuto.Checked
                                                 End Sub
        pSuelo.Controls.Add(_chkCalcAuto) : yS += 28
        _nudKaManual = Nud(pSuelo, "Ka manual (si cálculo auto está desactivado)", yS, 0.1, 1, 0.01, 3) : yS += 48
        _nudPhi = Nud(pSuelo, "Ángulo de fricción φ (°)", yS, 10, 50, 1, 1) : yS += 48
        _nudDelta = Nud(pSuelo, "Fricción muro-suelo δ (°)", yS, 0, 40, 1, 1) : yS += 48
        _nudGamma = Nud(pSuelo, "Peso suelo seco γ (kN/m³)", yS, 10, 25, 0.5, 1) : yS += 48
        _nudGammaSat = Nud(pSuelo, "Peso suelo saturado γsat (kN/m³)", yS, 10, 25, 0.5, 1) : yS += 48

        pSuelo.Controls.Add(Sep(yS)) : yS += 10
        _chkSobrecarga = New CheckBox With {.Text = "Incluir sobrecarga en superficie", .Location = New Point(0, yS), .AutoSize = True}
        AddHandler _chkSobrecarga.CheckedChanged, Sub()
                                                       OnInputChanged(Nothing, Nothing)
                                                       _nudSobrecarga.Enabled = _chkSobrecarga.Checked
                                                   End Sub
        pSuelo.Controls.Add(_chkSobrecarga) : yS += 28
        _nudSobrecarga = Nud(pSuelo, "Sobrecarga q (kPa)", yS, 0, 100, 1, 1) : yS += 48

        pSuelo.Controls.Add(Sep(yS)) : yS += 10
        _chkHidrostatica = New CheckBox With {.Text = "Incluir presión hidrostática", .Location = New Point(0, yS), .AutoSize = True}
        AddHandler _chkHidrostatica.CheckedChanged, Sub()
                                                         OnInputChanged(Nothing, Nothing)
                                                         _nudNivelFreatico.Enabled = _chkHidrostatica.Checked
                                                     End Sub
        pSuelo.Controls.Add(_chkHidrostatica) : yS += 28
        _nudNivelFreatico = Nud(pSuelo, "Nivel freático desde la base (m)", yS, 0, 30, 0.1, 2) : yS += 48

        pSuelo.Controls.Add(Sep(yS)) : yS += 10
        _chkSismo = New CheckBox With {.Text = "Incluir caso sísmico (Ka amplificado)", .Location = New Point(0, yS), .AutoSize = True}
        AddHandler _chkSismo.CheckedChanged, Sub()
                                                  OnInputChanged(Nothing, Nothing)
                                                  _nudFactorSismico.Enabled = _chkSismo.Checked
                                              End Sub
        pSuelo.Controls.Add(_chkSismo) : yS += 28
        _nudFactorSismico = Nud(pSuelo, "Factor de amplificación Ka (típico 1.2–1.5)", yS, 1, 2, 0.05, 2) : yS += 48

        tSuelo.Controls.Add(pSuelo)

        ' --- MATERIALES Y NORMATIVA ---
        Dim tMat As New TabPage("Materiales y normativa")
        Dim pMat As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim yM As Integer = 8
        pMat.Controls.Add(Et("Normativa aplicable", yM)) : yM += 20
        _cmbNormativa = New ComboBox With {.Location = New Point(0, yM), .Width = 300, .DropDownStyle = ComboBoxStyle.DropDownList}
        _cmbNormativa.Items.AddRange(New String() {"Residencial (NSR-10)", "Vial (CCP-14 / AASHTO LRFD)"})
        AddHandler _cmbNormativa.SelectedIndexChanged, AddressOf OnInputChanged
        pMat.Controls.Add(_cmbNormativa) : yM += 40
        _nudFc = Nud(pMat, "f'c concreto (MPa)", yM, 14, 50, 1, 0) : yM += 48
        _nudFy = Nud(pMat, "fy acero (MPa)", yM, 240, 500, 10, 0) : yM += 48
        _nudPesoC = Nud(pMat, "Peso concreto (kN/m³)", yM, 20, 28, 0.5, 1) : yM += 48
        _nudRecubExt = Nud(pMat, "Recubrimiento cara exterior / aire (m)", yM, 0.02, 0.1, 0.005, 3) : yM += 48
        _nudRecubInt = Nud(pMat, "Recubrimiento cara interior / relleno (m)", yM, 0.02, 0.1, 0.005, 3) : yM += 48
        tMat.Controls.Add(pMat)

        tc.TabPages.AddRange(New TabPage() {tInfo, tTramos, tApoyos, tSuelo, tMat})
        Return tc
    End Function

    Private Function Et(texto As String, y As Integer) As Label
        Return New Label With {
            .Text = texto, .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = GrisARCO, .Location = New Point(0, y), .AutoSize = True
        }
    End Function

    Private Function Tx(y As Integer, ancho As Integer) As TextBox
        Dim t As New TextBox With {.Location = New Point(0, y), .Width = ancho}
        AddHandler t.TextChanged, AddressOf OnInputChanged
        Return t
    End Function

    Private Function Nud(parent As Control, label As String, y As Integer,
                         mn As Decimal, mx As Decimal, step_ As Decimal, dec As Integer) As NumericUpDown
        parent.Controls.Add(Et(label, y))
        Dim ctrl As New NumericUpDown With {
            .Location = New Point(0, y + 20), .Width = 240,
            .Minimum = mn, .Maximum = mx, .Increment = step_, .DecimalPlaces = dec
        }
        AddHandler ctrl.ValueChanged, AddressOf OnInputChanged
        parent.Controls.Add(ctrl)
        Return ctrl
    End Function

    Private Function Sep(y As Integer) As Label
        Return New Label With {
            .Location = New Point(0, y), .Width = 400, .Height = 1,
            .BackColor = Color.FromArgb(220, 220, 220), .BorderStyle = BorderStyle.FixedSingle
        }
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Grillas de Tramos y Apoyos
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Function ConstruirGridTramos() As DataGridView
        Dim g = CrearGridBase()
        g.Columns.Add("Nombre", "Nombre")
        g.Columns.Add("Altura", "Altura (m)")
        g.Columns.Add("Espesor", "Espesor (m)")
        g.Columns("Nombre").Width = 120
        g.Columns("Altura").Width = 100
        g.Columns("Espesor").Width = 100
        g.Columns("Nombre").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        AddHandler g.CellEndEdit, AddressOf GridTramos_CellEndEdit
        Return g
    End Function

    Private Function ConstruirGridApoyos() As DataGridView
        Dim g = CrearGridBase()
        g.Columns.Add("Nombre", "Nombre")
        Dim colNivel As New DataGridViewTextBoxColumn With {.Name = "Nivel", .HeaderText = "Nivel (m)", .ReadOnly = True}
        g.Columns.Add(colNivel)
        Dim cmbTipo As New DataGridViewComboBoxColumn With {.Name = "Tipo", .HeaderText = "Tipo"}
        cmbTipo.Items.AddRange(New String() {"Empotrado", "Simplemente apoyado", "Libre"})
        g.Columns.Add(cmbTipo)
        g.Columns("Nombre").Width = 120
        g.Columns("Nivel").Width = 100
        g.Columns("Nivel").DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
        g.Columns("Tipo").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        AddHandler g.CellEndEdit, AddressOf GridApoyos_CellEndEdit
        Return g
    End Function

    Private Function CrearGridBase() As DataGridView
        Return New DataGridView With {
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .Font = New Font("Segoe UI", 9),
            .ColumnHeadersHeight = 28,
            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
                .BackColor = GrisARCO,
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .SelectionBackColor = GrisARCO,
                .SelectionForeColor = Color.White
            },
            .EnableHeadersVisualStyles = False
        }
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Gestión de Tramos y Apoyos
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub CmdAgregarTramo(sender As Object, e As EventArgs)
        Dim nuevoNombre = $"Tramo {_muro.Tramos.Count + 1}"
        _muro.Tramos.Add(New TramoCortina With {.Nombre = nuevoNombre, .Altura = 2.5, .Espesor = 0.20})
        ' Apoyo adicional al tope — nombre P02, P03...
        Dim nuevoApoyo As New ApoyoCortina With {
            .Etiqueta = EtiquetaApoyoAutomatica(_muro.Apoyos.Count),
            .Tipo = eTipoApoyoCortina.SimplementeApoyado
        }
        _muro.Apoyos.Add(nuevoApoyo)
        RecalcularNivelesApoyos()
        PintarGrids()
        _dirty = True
        ActualizarTitulo()
        _dibujo.Invalidate()
    End Sub

    Private Sub CmdEliminarTramo(sender As Object, e As EventArgs)
        If _muro.Tramos.Count <= 1 Then
            MessageBox.Show(Me, "Debe conservar al menos un tramo.",
                             "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        _muro.Tramos.RemoveAt(_muro.Tramos.Count - 1)
        If _muro.Apoyos.Count > 0 Then _muro.Apoyos.RemoveAt(_muro.Apoyos.Count - 1)
        RecalcularNivelesApoyos()
        PintarGrids()
        _dirty = True
        ActualizarTitulo()
        _dibujo.Invalidate()
    End Sub

    ''' <summary>Devuelve el nombre default para el apoyo en el índice indicado:
    ''' 0 = "Base", 1 = "P02", 2 = "P03"…</summary>
    Private Shared Function EtiquetaApoyoAutomatica(indice As Integer) As String
        If indice = 0 Then Return "Base"
        Return $"P{indice + 1:00}"
    End Function

    ''' <summary>Recalcula los niveles de los apoyos como suma acumulada de las alturas
    ''' de los tramos inferiores. Base = 0; superior = AlturaTotal.</summary>
    Private Sub RecalcularNivelesApoyos()
        If _muro.Apoyos Is Nothing OrElse _muro.Apoyos.Count = 0 Then Return
        Dim yAcum As Double = 0
        _muro.Apoyos(0).Nivel = 0
        For i = 1 To _muro.Apoyos.Count - 1
            yAcum += _muro.Tramos(i - 1).Altura
            _muro.Apoyos(i).Nivel = yAcum
        Next
    End Sub

    Private Sub PintarGrids()
        _cargandoControles = True
        Try
            ' Tramos
            _gridTramos.Rows.Clear()
            For i = 0 To _muro.Tramos.Count - 1
                Dim t = _muro.Tramos(i)
                _gridTramos.Rows.Add(t.Nombre, t.Altura.ToString("0.00"), t.Espesor.ToString("0.00"))
            Next
            ' Apoyos
            _gridApoyos.Rows.Clear()
            For i = 0 To _muro.Apoyos.Count - 1
                Dim a = _muro.Apoyos(i)
                _gridApoyos.Rows.Add(a.Etiqueta, a.Nivel.ToString("0.00"), TextoTipoApoyo(a.Tipo))
            Next
        Finally
            _cargandoControles = False
        End Try
    End Sub

    Private Shared Function TextoTipoApoyo(t As eTipoApoyoCortina) As String
        Select Case t
            Case eTipoApoyoCortina.Empotrado : Return "Empotrado"
            Case eTipoApoyoCortina.SimplementeApoyado : Return "Simplemente apoyado"
            Case eTipoApoyoCortina.Libre : Return "Libre"
        End Select
        Return "Simplemente apoyado"
    End Function

    Private Shared Function TipoApoyoDeTexto(t As String) As eTipoApoyoCortina
        Select Case t
            Case "Empotrado" : Return eTipoApoyoCortina.Empotrado
            Case "Libre" : Return eTipoApoyoCortina.Libre
            Case Else : Return eTipoApoyoCortina.SimplementeApoyado
        End Select
    End Function

    Private Sub GridTramos_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If _cargandoControles OrElse e.RowIndex < 0 OrElse e.RowIndex >= _muro.Tramos.Count Then Return
        Dim fila = _gridTramos.Rows(e.RowIndex)
        Dim t = _muro.Tramos(e.RowIndex)
        Try
            t.Nombre = CStr(fila.Cells("Nombre").Value)
            Dim h As Double
            If Double.TryParse(CStr(fila.Cells("Altura").Value), h) AndAlso h > 0 Then t.Altura = h
            Dim esp As Double
            If Double.TryParse(CStr(fila.Cells("Espesor").Value), esp) AndAlso esp > 0 Then t.Espesor = esp
            RecalcularNivelesApoyos()
            PintarGrids()
            _dirty = True
            ActualizarTitulo()
            _dibujo.Invalidate()
        Catch ex As Exception
            Logger.Error(ex, "Form_13_MuroCortina.GridTramos_CellEndEdit", t?.Nombre)
        End Try
    End Sub

    Private Sub GridApoyos_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If _cargandoControles OrElse e.RowIndex < 0 OrElse e.RowIndex >= _muro.Apoyos.Count Then Return
        Dim fila = _gridApoyos.Rows(e.RowIndex)
        Dim a = _muro.Apoyos(e.RowIndex)
        Try
            a.Etiqueta = CStr(fila.Cells("Nombre").Value)
            Dim tipoTxt = CStr(fila.Cells("Tipo").Value)
            If Not String.IsNullOrEmpty(tipoTxt) Then a.Tipo = TipoApoyoDeTexto(tipoTxt)
            _dirty = True
            ActualizarTitulo()
            _dibujo.Invalidate()
        Catch ex As Exception
            Logger.Error(ex, "Form_13_MuroCortina.GridApoyos_CellEndEdit", a?.Etiqueta)
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Comandos (Nuevo / Abrir / Guardar / Analizar / Reporte)
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub CmdNuevo(sender As Object, e As EventArgs)
        If Not ConfirmarDescartar() Then Return
        _muro = CrearMuroDefault()
        _rutaArchivo = ""
        CargarControlesDesdeMuro()
        _dirty = False
        _dibujo.Invalidate()
        _lblEstado.Text = "Muro nuevo. Ajuste las entradas y pulse ANALIZAR."
        _lblVeredicto.Text = ""
        _lblVeredicto.BackColor = Color.Transparent
        ActualizarTitulo()
    End Sub

    Private Sub CmdAbrir(sender As Object, e As EventArgs)
        If Not ConfirmarDescartar() Then Return
        Using ofd As New OpenFileDialog() With {
            .Filter = "Muro cortina (*.esm)|*.esm",
            .Title = "Abrir muro cortina"
        }
            If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                _muro = Funciones_Programa.DeSerializar(Of MuroCortina)(ofd.FileName)
                If _muro Is Nothing Then
                    MessageBox.Show(Me, "No se pudo leer el archivo.", "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                _rutaArchivo = ofd.FileName
                CargarControlesDesdeMuro()
                _dirty = False
                _dibujo.Invalidate()
                ActualizarTitulo()
                _lblEstado.Text = "Muro cargado desde " & Path.GetFileName(_rutaArchivo)
            Catch ex As Exception
                Logger.Error(ex, "Form_13_MuroCortina.CmdAbrir", ofd.FileName)
                MessageBox.Show(Me, "Error al abrir: " & ex.Message, "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub CmdGuardar(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(_rutaArchivo) Then
            CmdGuardarComo(sender, e)
            Return
        End If
        Guardar(_rutaArchivo)
    End Sub

    Private Sub CmdGuardarComo(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog() With {
            .Filter = "Muro cortina (*.esm)|*.esm",
            .DefaultExt = "esm",
            .FileName = If(String.IsNullOrWhiteSpace(_muro.Info.Nombre), "MuroCortina", _muro.Info.Nombre)
        }
            If sfd.ShowDialog(Me) <> DialogResult.OK Then Return
            Guardar(sfd.FileName)
            _rutaArchivo = sfd.FileName
            ActualizarTitulo()
        End Using
    End Sub

    Private Sub Guardar(ruta As String)
        Try
            VolcarControlesAMuro()
            Funciones_Programa.Serializar(ruta, _muro)
            _dirty = False
            ActualizarTitulo()
            _lblEstado.Text = "Guardado en " & Path.GetFileName(ruta)
        Catch ex As Exception
            Logger.Error(ex, "Form_13_MuroCortina.Guardar", ruta)
            MessageBox.Show(Me, "Error al guardar: " & ex.Message, "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CmdAnalizar(sender As Object, e As EventArgs)
        Try
            VolcarControlesAMuro()
            RecalcularNivelesApoyos()
            MuroCortinaService.AnalizarMuro(_muro)
            PintarResultados()
            _dibujo.Invalidate()
            _lblEstado.Text = "Análisis completado — " & DateTime.Now.ToString("HH:mm:ss")
        Catch ex As Exception
            Logger.Error(ex, "Form_13_MuroCortina.CmdAnalizar", "")
            MessageBox.Show(Me,
                "Error en el análisis:" & vbCrLf & ex.Message,
                "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Grilla de diseño por tramo (una fila por tramo, columnas por región + y −)
    '
    '   Región positiva: cara EXTERIOR (lado libre), resiste M+ → tracción exterior.
    '   Región negativa: cara INTERIOR (lado del relleno), resiste M− → tracción interior.
    '   Columnas editables: Barra+, Sep+, Barra−, Sep−, Forzar2capas (checkbox).
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Shared ReadOnly ColorFondoPos As Color = Color.FromArgb(230, 240, 252)
    Private Shared ReadOnly ColorFondoNeg As Color = Color.FromArgb(252, 240, 230)

    Private Function ConstruirGridDiseno() As DataGridView
        Dim g = CrearGridBase()
        g.Dock = DockStyle.Fill
        g.ScrollBars = ScrollBars.Both

        g.Columns.Add("Tramo", "Tramo")
        g.Columns.Add("Vu", "Vu (kN)")
        g.Columns.Add("PhiVc", "φVc (kN)")
        g.Columns.Add("CDCort", "C/D V")

        ' Cara positiva (M+)
        g.Columns.Add("MuPos", "Mu⁺ (kN·m)")
        g.Columns.Add(ComboBarraCol("BarraPos", "⌀⁺"))
        g.Columns.Add("SepPos", "@⁺ cm")
        g.Columns.Add("AsProvPos", "As prov⁺")
        g.Columns.Add("AsReqPos", "As req⁺")
        g.Columns.Add("CDPos", "C/D⁺")

        ' Cara negativa (M-)
        g.Columns.Add("MuNeg", "Mu⁻ (kN·m)")
        g.Columns.Add(ComboBarraCol("BarraNeg", "⌀⁻"))
        g.Columns.Add("SepNeg", "@⁻ cm")
        g.Columns.Add("AsProvNeg", "As prov⁻")
        g.Columns.Add("AsReqNeg", "As req⁻")
        g.Columns.Add("CDNeg", "C/D⁻")

        ' Doble capa (checkbox editable + razón descriptiva)
        Dim chkForzar As New DataGridViewCheckBoxColumn With {.Name = "ForzarDoble", .HeaderText = "Forzar 2 capas"}
        g.Columns.Add(chkForzar)
        g.Columns.Add("Razon2Capas", "2 capas por")
        g.Columns.Add("Veredicto", "Veredicto")

        ' Marcar readonly todas excepto las editables
        Dim editables = {"BarraPos", "SepPos", "BarraNeg", "SepNeg", "ForzarDoble"}
        For Each col As DataGridViewColumn In g.Columns
            col.ReadOnly = Not editables.Contains(col.Name)
        Next

        ' Anchos y colores por región
        g.Columns("Tramo").Width = 80
        g.Columns("Vu").Width = 70
        g.Columns("PhiVc").Width = 70
        g.Columns("CDCort").Width = 60
        For Each nombre In {"MuPos", "BarraPos", "SepPos", "AsProvPos", "AsReqPos", "CDPos"}
            g.Columns(nombre).DefaultCellStyle.BackColor = ColorFondoPos
            g.Columns(nombre).Width = 70
        Next
        For Each nombre In {"MuNeg", "BarraNeg", "SepNeg", "AsProvNeg", "AsReqNeg", "CDNeg"}
            g.Columns(nombre).DefaultCellStyle.BackColor = ColorFondoNeg
            g.Columns(nombre).Width = 70
        Next
        g.Columns("BarraPos").Width = 50 : g.Columns("BarraNeg").Width = 50
        g.Columns("SepPos").Width = 55 : g.Columns("SepNeg").Width = 55
        g.Columns("CDPos").Width = 55 : g.Columns("CDNeg").Width = 55
        g.Columns("ForzarDoble").Width = 55
        g.Columns("Razon2Capas").Width = 130
        g.Columns("Veredicto").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

        AddHandler g.CellEndEdit, AddressOf GridDiseno_CellEndEdit
        AddHandler g.CurrentCellDirtyStateChanged,
            Sub(sender, e)
                ' Confirmar inmediatamente el cambio del CheckBox para disparar CellEndEdit.
                If TypeOf g.CurrentCell Is DataGridViewCheckBoxCell AndAlso g.IsCurrentCellDirty Then
                    g.CommitEdit(DataGridViewDataErrorContexts.Commit)
                End If
            End Sub
        Return g
    End Function

    Private Function ComboBarraCol(nombre As String, header As String) As DataGridViewComboBoxColumn
        Dim col As New DataGridViewComboBoxColumn With {.Name = nombre, .HeaderText = header}
        col.Items.AddRange(New String() {"#3", "#4", "#5", "#6", "#7", "#8"})
        Return col
    End Function

    Private Sub PintarResultados()
        If _gridDiseno Is Nothing Then Return
        _gridDiseno.Rows.Clear()
        If _muro.Resultados Is Nothing OrElse _muro.Resultados.DisenosTramos Is Nothing Then Return
        For Each d In _muro.Resultados.DisenosTramos
            AgregarFilaDiseno(d)
        Next
        PintarVeredictoGlobal()
    End Sub

    Private Sub AgregarFilaDiseno(d As DisenoTramoCortina)
        If d Is Nothing Then Return
        Dim idx = _gridDiseno.Rows.Add(
            d.Nombre,
            d.Vu.ToString("0.0"),
            d.PhiVc.ToString("0.0"),
            FormatearCD(d.FactorCortante),
            d.CaraPositiva.MuMax.ToString("0.00"),
            d.CaraPositiva.BarraPrincipal,
            d.CaraPositiva.SeparacionCm.ToString("0.0"),
            d.CaraPositiva.AsProv.ToString("0"),
            d.CaraPositiva.AsReq.ToString("0"),
            FormatearCD(d.CaraPositiva.FactorFlexion),
            d.CaraNegativa.MuMax.ToString("0.00"),
            d.CaraNegativa.BarraPrincipal,
            d.CaraNegativa.SeparacionCm.ToString("0.0"),
            d.CaraNegativa.AsProv.ToString("0"),
            d.CaraNegativa.AsReq.ToString("0"),
            FormatearCD(d.CaraNegativa.FactorFlexion),
            d.DobleCapaForzada,
            d.RazonDobleCapa,
            If(d.CumpleTodo, "✓ Cumple", "✗ No cumple"))
        Dim fila = _gridDiseno.Rows(idx)
        fila.Tag = d
        For Each c As DataGridViewCell In fila.Cells
            c.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        fila.Cells("Tramo").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
        PintarCeldaVeredicto(fila, d.CumpleTodo)
    End Sub

    Private Sub PintarCeldaVeredicto(fila As DataGridViewRow, cumple As Boolean)
        fila.Cells("Veredicto").Style.BackColor = If(cumple, Color.FromArgb(220, 237, 220), Color.FromArgb(248, 215, 215))
        fila.Cells("Veredicto").Style.ForeColor = If(cumple, Color.FromArgb(27, 94, 32), Color.FromArgb(176, 0, 32))
        fila.Cells("Veredicto").Style.Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold)
    End Sub

    Private Sub GridDiseno_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return
        Dim fila = _gridDiseno.Rows(e.RowIndex)
        Dim d = TryCast(fila.Tag, DisenoTramoCortina)
        If d Is Nothing Then Return
        Try
            ' Cara positiva
            Dim barraPos = CStr(fila.Cells("BarraPos").Value)
            Dim sepPos As Double = 0
            Double.TryParse(CStr(fila.Cells("SepPos").Value), sepPos)
            If Not String.IsNullOrEmpty(barraPos) Then d.CaraPositiva.BarraPrincipal = barraPos
            If sepPos > 0 Then d.CaraPositiva.SeparacionCm = sepPos
            MuroCortinaService.RecalcularAsProvCara(d.CaraPositiva)

            ' Cara negativa
            Dim barraNeg = CStr(fila.Cells("BarraNeg").Value)
            Dim sepNeg As Double = 0
            Double.TryParse(CStr(fila.Cells("SepNeg").Value), sepNeg)
            If Not String.IsNullOrEmpty(barraNeg) Then d.CaraNegativa.BarraPrincipal = barraNeg
            If sepNeg > 0 Then d.CaraNegativa.SeparacionCm = sepNeg
            MuroCortinaService.RecalcularAsProvCara(d.CaraNegativa)

            ' Flag "forzar doble capa"
            Dim forzarVal = fila.Cells("ForzarDoble").Value
            If forzarVal IsNot Nothing Then
                Dim forzarBool As Boolean
                If Boolean.TryParse(forzarVal.ToString(), forzarBool) Then
                    d.DobleCapaForzada = forzarBool
                End If
            End If

            ' Refrescar celdas calculadas de la fila
            fila.Cells("AsProvPos").Value = d.CaraPositiva.AsProv.ToString("0")
            fila.Cells("CDPos").Value = FormatearCD(d.CaraPositiva.FactorFlexion)
            fila.Cells("AsProvNeg").Value = d.CaraNegativa.AsProv.ToString("0")
            fila.Cells("CDNeg").Value = FormatearCD(d.CaraNegativa.FactorFlexion)
            fila.Cells("Razon2Capas").Value = d.RazonDobleCapa
            fila.Cells("Veredicto").Value = If(d.CumpleTodo, "✓ Cumple", "✗ No cumple")
            PintarCeldaVeredicto(fila, d.CumpleTodo)
            PintarVeredictoGlobal()
            _dirty = True
            ActualizarTitulo()
        Catch ex As Exception
            Logger.Error(ex, "Form_13_MuroCortina.GridDiseno_CellEndEdit", d?.Nombre)
        End Try
    End Sub

    Private Sub PintarVeredictoGlobal()
        If _lblVeredicto Is Nothing Then Return
        If _muro.Resultados Is Nothing OrElse _muro.Resultados.DisenosTramos Is Nothing OrElse _muro.Resultados.DisenosTramos.Count = 0 Then
            _lblVeredicto.Text = ""
            _lblVeredicto.BackColor = Color.Transparent
            Return
        End If
        Dim cumple = _muro.Resultados.DisenosTramos.All(Function(d) d.CumpleTodo)
        _lblVeredicto.Text = If(cumple, "✓  MURO CONFORME", "✗  REQUIERE REVISIÓN")
        _lblVeredicto.BackColor = If(cumple, VerdeARCO, RojoARCO)
    End Sub

    Private Shared Function FormatearCD(v As Double) As String
        If v <= 0 Then Return "—"
        If Double.IsInfinity(v) Then Return "∞"
        Return v.ToString("0.00")
    End Function

    Private Sub CmdReporte(sender As Object, e As EventArgs)
        MessageBox.Show(Me, "Reporte — se habilita cuando esté el análisis (Fase D).",
                         "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Function ConfirmarDescartar() As Boolean
        If Not _dirty Then Return True
        Return MessageBox.Show(Me, "Hay cambios sin guardar. ¿Descartar?",
                                "ARCO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
    End Function

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _dirty AndAlso Not ConfirmarDescartar() Then
            e.Cancel = True
            Return
        End If
        MyBase.OnFormClosing(e)
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Carga ↔ Volcado controles / modelo
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub CargarControlesDesdeMuro()
        _cargandoControles = True
        Try
            Dim m = _muro
            _txtNombre.Text = m.Info.Nombre
            _txtProyecto.Text = m.Info.Proyecto
            _txtCliente.Text = m.Info.Cliente
            _txtResponsable.Text = m.Info.Responsable
            _txtCodigo.Text = m.Info.Codigo
            _dtpFecha.Value = m.Info.Fecha
            _txtDescripcion.Text = m.Info.Descripcion

            _cmbTeoria.SelectedIndex = If(m.Suelo.Teoria = eTeoriaEmpuje.Coulomb, 1, 0)
            _chkCalcAuto.Checked = m.Suelo.CalcularKaAutomatico
            _nudKaManual.Value = AClamp(m.Suelo.KaManual, _nudKaManual)
            _nudKaManual.Enabled = Not _chkCalcAuto.Checked
            _nudPhi.Value = AClamp(m.Suelo.Phi, _nudPhi)
            _nudDelta.Value = AClamp(m.Suelo.Delta, _nudDelta)
            _nudGamma.Value = AClamp(m.Suelo.Gamma, _nudGamma)
            _nudGammaSat.Value = AClamp(m.Suelo.GammaSat, _nudGammaSat)

            _chkSobrecarga.Checked = m.Sobrecarga.IncluirSobrecarga
            _nudSobrecarga.Value = AClamp(m.Sobrecarga.SobrecargaKPa, _nudSobrecarga)
            _nudSobrecarga.Enabled = _chkSobrecarga.Checked

            _chkHidrostatica.Checked = m.Opciones.IncluirHidrostatica
            _nudNivelFreatico.Value = AClamp(m.Opciones.NivelAguaFreatica, _nudNivelFreatico)
            _nudNivelFreatico.Enabled = _chkHidrostatica.Checked

            _chkSismo.Checked = m.Opciones.IncluirSismo
            _nudFactorSismico.Value = AClamp(m.Opciones.FactorSismico, _nudFactorSismico)
            _nudFactorSismico.Enabled = _chkSismo.Checked

            _cmbNormativa.SelectedIndex = If(m.Normativa = eNormativaMuro.Vial, 1, 0)
            _nudFc.Value = AClamp(m.Materiales.Fc, _nudFc)
            _nudFy.Value = AClamp(m.Materiales.Fy, _nudFy)
            _nudPesoC.Value = AClamp(m.Materiales.PesoConcreto, _nudPesoC)
            _nudRecubExt.Value = AClamp(m.Materiales.RecubrimientoExterior, _nudRecubExt)
            _nudRecubInt.Value = AClamp(m.Materiales.RecubrimientoInterior, _nudRecubInt)

            RecalcularNivelesApoyos()
            PintarGrids()
            PintarResultados()
        Finally
            _cargandoControles = False
        End Try
    End Sub

    Private Function AClamp(valor As Double, nud As NumericUpDown) As Decimal
        Dim v = CDec(valor)
        If v < nud.Minimum Then Return nud.Minimum
        If v > nud.Maximum Then Return nud.Maximum
        Return v
    End Function

    Private Sub VolcarControlesAMuro()
        Dim m = _muro
        m.Info.Nombre = _txtNombre.Text
        m.Info.Proyecto = _txtProyecto.Text
        m.Info.Cliente = _txtCliente.Text
        m.Info.Responsable = _txtResponsable.Text
        m.Info.Codigo = _txtCodigo.Text
        m.Info.Fecha = _dtpFecha.Value
        m.Info.Descripcion = _txtDescripcion.Text

        m.Suelo.Teoria = If(_cmbTeoria.SelectedIndex = 1, eTeoriaEmpuje.Coulomb, eTeoriaEmpuje.Rankine)
        m.Suelo.CalcularKaAutomatico = _chkCalcAuto.Checked
        m.Suelo.KaManual = CDbl(_nudKaManual.Value)
        m.Suelo.Phi = CDbl(_nudPhi.Value)
        m.Suelo.Delta = CDbl(_nudDelta.Value)
        m.Suelo.Gamma = CDbl(_nudGamma.Value)
        m.Suelo.GammaSat = CDbl(_nudGammaSat.Value)

        m.Sobrecarga.IncluirSobrecarga = _chkSobrecarga.Checked
        m.Sobrecarga.SobrecargaKPa = CDbl(_nudSobrecarga.Value)

        m.Opciones.IncluirHidrostatica = _chkHidrostatica.Checked
        m.Opciones.NivelAguaFreatica = CDbl(_nudNivelFreatico.Value)
        m.Opciones.IncluirSismo = _chkSismo.Checked
        m.Opciones.FactorSismico = CDbl(_nudFactorSismico.Value)

        m.Normativa = If(_cmbNormativa.SelectedIndex = 1, eNormativaMuro.Vial, eNormativaMuro.Residencial)
        m.Materiales.Fc = CSng(_nudFc.Value)
        m.Materiales.Fy = CSng(_nudFy.Value)
        m.Materiales.PesoConcreto = CSng(_nudPesoC.Value)
        m.Materiales.RecubrimientoExterior = CSng(_nudRecubExt.Value)
        m.Materiales.RecubrimientoInterior = CSng(_nudRecubInt.Value)
    End Sub

    Private Sub OnInputChanged(sender As Object, e As EventArgs)
        If _cargandoControles Then Return
        _dirty = True
        ActualizarTitulo()
        _dibujo.Invalidate()
    End Sub

    Private Sub ActualizarTitulo()
        Dim archivo = If(String.IsNullOrEmpty(_rutaArchivo), "(sin guardar)", Path.GetFileName(_rutaArchivo))
        _lblRuta.Text = archivo & If(_dirty, "   •", "")
        Me.Text = "MC. Cortina — " & archivo & If(_dirty, " *", "") & "   —   ARCO"
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Dibujo del muro (orientado vertical) + zoom/pan
    ' ═══════════════════════════════════════════════════════════════════════════
    Private _w2s As Func(Of Double, Double, PointF)

    Private Sub Dibujo_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(Color.White)

        VolcarControlesAMuro()
        RecalcularNivelesApoyos()
        Dim H = _muro.AlturaTotal

        If H <= 0 OrElse _muro.Tramos.Count = 0 Then
            Using fMsg As New Font("Segoe UI", 11, FontStyle.Italic), bMsg As New SolidBrush(Color.Gray)
                Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                g.DrawString("Agregue al menos un tramo con altura > 0.", fMsg, bMsg,
                             New RectangleF(0, 0, _dibujo.Width, _dibujo.Height), sf)
            End Using
            Return
        End If

        ' Marco sutil
        Using penMarco As New Pen(Color.FromArgb(220, 225, 232), 1)
            g.DrawRectangle(penMarco, 0, 0, _dibujo.Width - 1, _dibujo.Height - 1)
        End Using

        ' --- Fit view + zoom del usuario ---------------------------------
        ' Mundo: X = ancho horizontal (espesor), Y = altura (0 abajo).
        ' bbox: izquierda −1.5 m (para empuje + cotas), derecha +1.5, inferior −0.3, superior H+0.3.
        Dim espesorMax = _muro.Tramos.Max(Function(t) t.Espesor)
        Dim xMin = -1.8
        Dim xMax = espesorMax + 1.8
        Dim yMin = -0.3
        Dim yMax = H + 0.5

        Dim margenPx As Integer = 70
        Dim escX = (_dibujo.Width - 2 * margenPx) / (xMax - xMin)
        Dim escY = (_dibujo.Height - 2 * margenPx) / (yMax - yMin)
        Dim escFit = Math.Min(escX, escY)
        If escFit <= 0 Then Return
        Dim esc = escFit * _zoomUser

        Dim cx = _dibujo.Width / 2.0
        Dim cy = _dibujo.Height / 2.0
        Dim bbcx = (xMin + xMax) / 2.0
        Dim bbcy = (yMin + yMax) / 2.0
        Dim panX = _panUserX, panY = _panUserY
        _w2s = Function(wx As Double, wy As Double) New PointF(
            CSng(cx + (wx - bbcx) * esc + panX),
            CSng(cy - (wy - bbcy) * esc + panY))

        ' --- Suelo detrás del muro (hatching) ----------------------------
        Dim xParedTrasera = espesorMax
        Using hb As New HatchBrush(HatchStyle.ForwardDiagonal, ColorTierra, ColorTierraFondo)
            Dim ptos As PointF() = {
                _w2s(xParedTrasera, 0),
                _w2s(xParedTrasera + 1.5, 0),
                _w2s(xParedTrasera + 1.5, H),
                _w2s(xParedTrasera, H)
            }
            g.FillPolygon(hb, ptos)
        End Using
        Using penSup As New Pen(ColorTierra, 1.5F)
            g.DrawLine(penSup, _w2s(xParedTrasera, H), _w2s(xParedTrasera + 1.5, H))
        End Using

        ' --- Nivel freático (si aplica) ----------------------------------
        If _muro.Opciones.IncluirHidrostatica AndAlso _muro.Opciones.NivelAguaFreatica > 0 AndAlso _muro.Opciones.NivelAguaFreatica < H Then
            Dim yNF = _muro.Opciones.NivelAguaFreatica
            Using penNF As New Pen(ColorAgua, 1.3F)
                penNF.DashPattern = New Single() {5, 3}
                g.DrawLine(penNF, _w2s(xParedTrasera, yNF), _w2s(xParedTrasera + 1.5, yNF))
            End Using
            ' Overlay azul semitransparente abajo del nivel freático
            Using bAgua As New SolidBrush(Color.FromArgb(50, ColorAgua))
                Dim ptos As PointF() = {
                    _w2s(xParedTrasera, 0),
                    _w2s(xParedTrasera + 1.5, 0),
                    _w2s(xParedTrasera + 1.5, yNF),
                    _w2s(xParedTrasera, yNF)
                }
                g.FillPolygon(bAgua, ptos)
            End Using
            Using fNF As New Font("Segoe UI", 7.5F, FontStyle.Bold Or FontStyle.Italic), bNF As New SolidBrush(ColorAgua)
                Dim p = _w2s(xParedTrasera + 1.5, yNF)
                g.DrawString($"N.F.  y = {yNF:0.00} m", fNF, bNF, p.X - 105, p.Y - 14)
            End Using
        End If

        ' --- Pantalla (tramos de concreto apilados) ----------------------
        Dim yAcum As Double = 0
        For Each t In _muro.Tramos
            Dim yBase = yAcum
            Dim yTope = yAcum + t.Altura
            Dim xIzq = xParedTrasera - t.Espesor
            Dim xDer = xParedTrasera

            Using bConcreto As New SolidBrush(ColorConcreto), penBorde As New Pen(ColorConcretoBorde, 1.6F)
                Dim rect As PointF() = {
                    _w2s(xIzq, yBase), _w2s(xDer, yBase),
                    _w2s(xDer, yTope), _w2s(xIzq, yTope)
                }
                g.FillPolygon(bConcreto, rect)
                g.DrawPolygon(penBorde, rect)
            End Using

            ' Etiqueta del tramo (dentro)
            Using fT As New Font("Segoe UI", 8, FontStyle.Bold), bT As New SolidBrush(GrisARCO)
                Dim pMid = _w2s((xIzq + xDer) / 2.0, (yBase + yTope) / 2.0)
                Dim sz = g.MeasureString(t.Nombre, fT)
                g.DrawString(t.Nombre, fT, bT, pMid.X - sz.Width / 2, pMid.Y - sz.Height / 2)
            End Using

            yAcum = yTope
        Next

        ' --- Apoyos (triangulito simple, hatch empotrado) ----------------
        For i = 0 To _muro.Apoyos.Count - 1
            DibujarApoyo(g, _muro.Apoyos(i), xParedTrasera - _muro.Tramos(Math.Min(i, _muro.Tramos.Count - 1)).Espesor)
        Next

        ' --- Diagrama de empuje activo (triángulo lado del relleno) ------
        DibujarDiagramaEmpuje(g, xParedTrasera + 0.05, H)

        ' --- Cotas acotadas ----------------------------------------------
        DibujarCotas(g, H, xParedTrasera, espesorMax)

        ' --- Diagramas M y V (si hay resultados) + reacciones + armado ---
        If _muro.Resultados IsNot Nothing AndAlso _muro.Resultados.DiagramaM IsNot Nothing AndAlso _muro.Resultados.DiagramaM.Count > 0 Then
            DibujarDiagramasMV(g, H, xParedTrasera, espesorMax)
            DibujarReacciones(g, xParedTrasera, espesorMax)
            DibujarArmado(g, xParedTrasera)
        End If
    End Sub

    Private Sub DibujarApoyo(g As Graphics, a As ApoyoCortina, xMuro As Double)
        Dim pApoyo = _w2s(xMuro, a.Nivel)
        Select Case a.Tipo
            Case eTipoApoyoCortina.Empotrado
                ' Hatch cross al lado del muro (lado libre)
                Using hb As New HatchBrush(HatchStyle.BackwardDiagonal, GrisARCO, Color.FromArgb(240, 240, 240))
                    Dim rect As New RectangleF(pApoyo.X - 24, pApoyo.Y - 10, 20, 20)
                    g.FillRectangle(hb, rect)
                    Using pen As New Pen(ColorConcretoBorde, 1.5F)
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
                    End Using
                End Using
            Case eTipoApoyoCortina.SimplementeApoyado
                ' Triangulito lado libre, apuntando al muro
                Using b As New SolidBrush(ColorConcretoBorde)
                    Dim tri As PointF() = {
                        pApoyo,
                        New PointF(pApoyo.X - 14, pApoyo.Y - 8),
                        New PointF(pApoyo.X - 14, pApoyo.Y + 8)
                    }
                    g.FillPolygon(b, tri)
                End Using
                ' Línea base del triángulo
                Using pen As New Pen(ColorConcretoBorde, 1.5F)
                    g.DrawLine(pen, pApoyo.X - 18, pApoyo.Y - 10, pApoyo.X - 18, pApoyo.Y + 10)
                End Using
            Case eTipoApoyoCortina.Libre
                ' Nada visualmente, pero una etiqueta pequeña
                Using fL As New Font("Segoe UI", 7, FontStyle.Italic), bL As New SolidBrush(Color.FromArgb(140, 140, 140))
                    g.DrawString("libre", fL, bL, pApoyo.X - 32, pApoyo.Y - 7)
                End Using
        End Select
        ' Etiqueta del apoyo
        Using fE As New Font("Segoe UI", 7.5F, FontStyle.Bold), bE As New SolidBrush(ColorConcretoBorde)
            Dim txt = $"{a.Etiqueta}  (y={a.Nivel:0.00})"
            g.DrawString(txt, fE, bE, pApoyo.X - 100, pApoyo.Y + 10)
        End Using
    End Sub

    ''' <summary>Dibuja el triángulo del empuje activo del lado del relleno.
    ''' Si hidrostática activa, dibuja también el triángulo de agua debajo del NF.</summary>
    Private Sub DibujarDiagramaEmpuje(g As Graphics, xBase As Double, H As Double)
        Dim xMaxDiagrama = xBase + 0.9   ' extensión máxima del triángulo

        ' Empuje activo del suelo (triángulo lineal desde la corona hasta la base)
        Using b As New SolidBrush(Color.FromArgb(70, ColorEmpuje))
            Dim tri As PointF() = {
                _w2s(xBase, H),
                _w2s(xBase, 0),
                _w2s(xMaxDiagrama, 0)
            }
            g.FillPolygon(b, tri)
            Using pen As New Pen(ColorEmpuje, 1.2F)
                g.DrawPolygon(pen, tri)
            End Using
        End Using
        Using fE As New Font("Segoe UI", 7, FontStyle.Bold Or FontStyle.Italic), bE As New SolidBrush(ColorEmpuje)
            Dim p = _w2s(xMaxDiagrama, 0.05)
            g.DrawString("Ka·γ·H", fE, bE, p.X + 2, p.Y - 10)
        End Using

        ' Hidrostática (debajo del NF)
        If _muro.Opciones.IncluirHidrostatica AndAlso _muro.Opciones.NivelAguaFreatica > 0 AndAlso _muro.Opciones.NivelAguaFreatica < H Then
            Dim yNF = _muro.Opciones.NivelAguaFreatica
            Dim xMaxAgua = xBase + 0.5
            Using b As New SolidBrush(Color.FromArgb(90, ColorEmpujeAgua))
                Dim tri As PointF() = {
                    _w2s(xBase, yNF),
                    _w2s(xBase, 0),
                    _w2s(xMaxAgua, 0)
                }
                g.FillPolygon(b, tri)
                Using pen As New Pen(ColorEmpujeAgua, 1.2F)
                    g.DrawPolygon(pen, tri)
                End Using
            End Using
        End If
    End Sub

    Private Sub DibujarCotas(g As Graphics, H As Double, xMuro As Double, espesorMax As Double)
        Dim fCota As New Font("Segoe UI", 8, FontStyle.Bold)
        Dim bCota As New SolidBrush(ColorCota)
        Dim penExt As New Pen(ColorCota, 0.75F)
        Dim penCota As New Pen(ColorCota, 1.0F)

        Try
            ' H total — vertical a la izquierda del muro
            CotaVertical(g, xMuro - espesorMax - 0.4, 0, H, 30, False,
                          $"H = {H:0.00} m", fCota, bCota, penExt, penCota)

            ' Alturas de cada tramo (vertical, pegado al muro, lado izquierdo)
            Dim yAcum As Double = 0
            For Each t In _muro.Tramos
                Dim yA = yAcum, yB = yAcum + t.Altura
                CotaVertical(g, xMuro - t.Espesor - 0.15, yA, yB, 8, False,
                              $"{t.Altura:0.00}", fCota, bCota, penExt, penCota)
                yAcum = yB
            Next

            ' Espesor de cada tramo (horizontal arriba del muro) — solo el primero
            If _muro.Tramos.Count > 0 Then
                Dim t0 = _muro.Tramos(0)
                CotaHorizontal(g, xMuro - t0.Espesor, xMuro, 0, 25, True,
                                $"t = {t0.Espesor:0.00} m", fCota, bCota, penExt, penCota)
            End If
        Finally
            fCota.Dispose() : bCota.Dispose() : penExt.Dispose() : penCota.Dispose()
        End Try
    End Sub

    Private Sub CotaHorizontal(g As Graphics, xA As Double, xB As Double, yMundo As Double,
                                offsetPx As Integer, abajo As Boolean, texto As String,
                                fCota As Font, bCota As SolidBrush, penExt As Pen, penCota As Pen)
        If xB <= xA Then Return
        Dim pA = _w2s(xA, yMundo)
        Dim pB = _w2s(xB, yMundo)
        Dim dirY = If(abajo, 1, -1)
        Dim yLinea = pA.Y + offsetPx * dirY
        Dim pAc As New PointF(pA.X, yLinea)
        Dim pBc As New PointF(pB.X, yLinea)
        g.DrawLine(penExt, pA, New PointF(pA.X, yLinea + 3 * dirY))
        g.DrawLine(penExt, pB, New PointF(pB.X, yLinea + 3 * dirY))
        g.DrawLine(penCota, pAc, pBc)
        DibujarFlechaH(g, pAc, True, bCota)
        DibujarFlechaH(g, pBc, False, bCota)
        Dim sz = g.MeasureString(texto, fCota)
        Dim yTexto = yLinea + If(abajo, 2, -(sz.Height + 2))
        g.DrawString(texto, fCota, bCota, (pAc.X + pBc.X) / 2 - sz.Width / 2, yTexto)
    End Sub

    Private Sub CotaVertical(g As Graphics, xMundo As Double, yA As Double, yB As Double,
                              offsetPx As Integer, derecha As Boolean, texto As String,
                              fCota As Font, bCota As SolidBrush, penExt As Pen, penCota As Pen)
        If yB <= yA Then Return
        Dim pA = _w2s(xMundo, yA)
        Dim pB = _w2s(xMundo, yB)
        Dim dirX = If(derecha, 1, -1)
        Dim xLinea = pA.X + offsetPx * dirX
        Dim pAc As New PointF(xLinea, pA.Y)
        Dim pBc As New PointF(xLinea, pB.Y)
        g.DrawLine(penExt, pA, New PointF(xLinea + 3 * dirX, pA.Y))
        g.DrawLine(penExt, pB, New PointF(xLinea + 3 * dirX, pB.Y))
        g.DrawLine(penCota, pAc, pBc)
        DibujarFlechaV(g, pAc, True, bCota)
        DibujarFlechaV(g, pBc, False, bCota)
        Dim sz = g.MeasureString(texto, fCota)
        Dim state = g.Save()
        g.TranslateTransform(xLinea + If(derecha, 4, -4), (pAc.Y + pBc.Y) / 2)
        g.RotateTransform(-90)
        g.DrawString(texto, fCota, bCota, -sz.Width / 2, If(derecha, -sz.Height, 0))
        g.Restore(state)
    End Sub

    Private Sub DibujarFlechaH(g As Graphics, pt As PointF, haciaDerecha As Boolean, b As Brush)
        Dim dx = If(haciaDerecha, 6, -6)
        g.FillPolygon(b, New PointF() {pt, New PointF(pt.X + dx, pt.Y - 2.5), New PointF(pt.X + dx, pt.Y + 2.5)})
    End Sub

    Private Sub DibujarFlechaV(g As Graphics, pt As PointF, haciaAbajo As Boolean, b As Brush)
        Dim dy = If(haciaAbajo, 6, -6)
        g.FillPolygon(b, New PointF() {pt, New PointF(pt.X - 2.5, pt.Y + dy), New PointF(pt.X + 2.5, pt.Y + dy)})
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Diagramas M y V al costado del muro (lado izquierdo)
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Shared ReadOnly ColorM As Color = Color.FromArgb(60, 110, 185)   ' azul
    Private Shared ReadOnly ColorV As Color = Color.FromArgb(140, 60, 170)   ' morado

    Private Sub DibujarDiagramasMV(g As Graphics, H As Double, xMuro As Double, espesorMax As Double)
        Dim diagM = _muro.Resultados.DiagramaM
        Dim diagV = _muro.Resultados.DiagramaV
        If diagM Is Nothing OrElse diagM.Count = 0 Then Return

        Dim mMax = diagM.Max(Function(p) Math.Abs(p.Valor))
        Dim vMax = diagV.Max(Function(p) Math.Abs(p.Valor))
        If mMax < 0.001 Then mMax = 1
        If vMax < 0.001 Then vMax = 1

        ' Dos columnas de diagramas al lado izquierdo del muro (en coord mundo).
        ' Cada diagrama ocupa 0.6 m de ancho; el eje vertical está en xCol0.
        Dim xColM = -0.6                               ' columna del M (más cerca del muro)
        Dim xColV = xColM - 0.9                        ' columna de V
        Dim anchoDiagrama = 0.55                       ' ancho mundo del diagrama

        ' --- Diagrama M ---
        DibujarDiagrama(g, diagM, xColM, anchoDiagrama, mMax, ColorM, "M")

        ' --- Diagrama V ---
        DibujarDiagrama(g, diagV, xColV, anchoDiagrama, vMax, ColorV, "V")

        ' Valores máximos en texto arriba
        Using fMax As New Font("Segoe UI", 8.5F, FontStyle.Bold)
            Dim pTopM = _w2s(xColM, H)
            Using b As New SolidBrush(ColorM)
                g.DrawString($"M máx = {mMax:0.00} kN·m/m", fMax, b, pTopM.X - 50, pTopM.Y - 32)
            End Using
            Dim pTopV = _w2s(xColV, H)
            Using b As New SolidBrush(ColorV)
                g.DrawString($"V máx = {vMax:0.00} kN/m", fMax, b, pTopV.X - 50, pTopV.Y - 32)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Dibuja un diagrama (M o V) con eje vertical en xEje y curva que se extiende
    ''' hacia la izquierda (valores negativos) o la derecha (valores positivos) hasta
    ''' un ancho máximo mundoAncho. valoresMax es el máximo absoluto usado para escalar.
    ''' </summary>
    Private Sub DibujarDiagrama(g As Graphics, puntos As List(Of PuntoDiagrama),
                                 xEje As Double, mundoAncho As Double, valorMax As Double,
                                 colorCurva As Color, etiqueta As String)
        ' Eje vertical de referencia
        Dim yMin = puntos.Min(Function(p) p.Y)
        Dim yMax = puntos.Max(Function(p) p.Y)
        Using penEje As New Pen(Color.FromArgb(130, 130, 130), 0.8F)
            g.DrawLine(penEje, _w2s(xEje, yMin), _w2s(xEje, yMax))
        End Using

        ' Curva y relleno semi-transparente
        Dim ptsCurva(puntos.Count - 1) As PointF
        Dim ptsRelleno(puntos.Count + 1) As PointF
        For i = 0 To puntos.Count - 1
            Dim p = puntos(i)
            Dim offset = p.Valor / valorMax * mundoAncho
            Dim x = xEje + offset
            ptsCurva(i) = _w2s(x, p.Y)
            ptsRelleno(i) = ptsCurva(i)
        Next
        ptsRelleno(puntos.Count) = _w2s(xEje, yMax)
        ptsRelleno(puntos.Count + 1) = _w2s(xEje, yMin)

        Using bFill As New SolidBrush(Color.FromArgb(70, colorCurva))
            g.FillPolygon(bFill, ptsRelleno)
        End Using
        Using penCurva As New Pen(colorCurva, 1.4F)
            g.DrawLines(penCurva, ptsCurva)
        End Using

        ' Etiqueta inferior
        Using fLbl As New Font("Segoe UI", 8, FontStyle.Bold Or FontStyle.Italic)
            Using b As New SolidBrush(colorCurva)
                Dim pBot = _w2s(xEje, yMin)
                g.DrawString(etiqueta, fLbl, b, pBot.X - 4, pBot.Y + 6)
            End Using
        End Using
    End Sub

    ''' <summary>Muestra una pequeña flecha + valor numérico al lado de cada apoyo con reacción.</summary>
    Private Sub DibujarReacciones(g As Graphics, xMuro As Double, espesorMax As Double)
        If _muro.Resultados.Reacciones Is Nothing OrElse _muro.Resultados.Reacciones.Count = 0 Then Return
        Using fR As New Font("Segoe UI", 7.5F, FontStyle.Bold), bR As New SolidBrush(Color.FromArgb(27, 94, 32))
            For i = 0 To _muro.Apoyos.Count - 1
                Dim r = _muro.Resultados.Reacciones(i)
                If Math.Abs(r) < 0.01 Then Continue For
                Dim p = _w2s(xMuro - espesorMax - 0.05, _muro.Apoyos(i).Nivel)
                g.DrawString($"R = {r:0.0} kN/m", fR, bR, p.X - 95, p.Y - 24)
            Next
        End Using
    End Sub

    ''' <summary>
    ''' Dibuja las líneas de refuerzo colocadas en cada cara del muro, por tramo.
    ''' Posición de las líneas: a la distancia "recubrimiento" desde la cara.
    '''   - Cara interior (lado del relleno) → línea cerca de x = xMuroTras.
    '''   - Cara exterior (lado libre) → línea cerca de x = xMuroTras − espesor.
    ''' La etiqueta del armado va al medio del tramo, centrada entre las líneas
    ''' si hay doble capa, al costado si hay una sola.
    ''' </summary>
    Private Sub DibujarArmado(g As Graphics, xMuroTras As Double)
        If _muro.Resultados.DisenosTramos Is Nothing OrElse _muro.Resultados.DisenosTramos.Count = 0 Then Return

        Dim colorRef As Color = Color.FromArgb(220, 50, 50)
        Dim yAcum As Double = 0
        For iT = 0 To _muro.Tramos.Count - 1
            Dim t = _muro.Tramos(iT)
            Dim yA = yAcum, yB = yAcum + t.Altura
            yAcum = yB

            If iT >= _muro.Resultados.DisenosTramos.Count Then Continue For
            Dim dto = _muro.Resultados.DisenosTramos(iT)

            Dim xCaraExt = xMuroTras - t.Espesor
            Dim xCaraInt = xMuroTras

            ' La "cara positiva" resiste M+ → tracción exterior → acero en cara EXTERIOR.
            Dim hayCaraExt = dto.CaraPositiva.AsReq > 0 OrElse dto.RequiereDosCapas
            ' La "cara negativa" resiste M− → tracción interior → acero en cara INTERIOR.
            Dim hayCaraInt = dto.CaraNegativa.AsReq > 0 OrElse dto.RequiereDosCapas

            Using penRef As New Pen(colorRef, 1.8F)
                penRef.DashPattern = New Single() {4, 2}

                If hayCaraExt Then
                    Dim xRef = xCaraExt + dto.CaraPositiva.Recubrimiento
                    g.DrawLine(penRef, _w2s(xRef, yA), _w2s(xRef, yB))
                End If
                If hayCaraInt Then
                    Dim xRef = xCaraInt - dto.CaraNegativa.Recubrimiento
                    g.DrawLine(penRef, _w2s(xRef, yA), _w2s(xRef, yB))
                End If
            End Using

            ' Etiquetas del armado (al medio del tramo)
            Dim yMid = (yA + yB) / 2
            Using fLbl As New Font("Segoe UI", 7, FontStyle.Bold), bLbl As New SolidBrush(colorRef)
                If hayCaraExt Then
                    Dim xRef = xCaraExt + dto.CaraPositiva.Recubrimiento
                    Dim p = _w2s(xRef, yMid)
                    Dim txt = $"{dto.CaraPositiva.BarraPrincipal}@{dto.CaraPositiva.SeparacionCm:0}"
                    Dim sz = g.MeasureString(txt, fLbl)
                    g.FillRectangle(Brushes.White, p.X - sz.Width - 6, p.Y - sz.Height / 2, sz.Width + 4, sz.Height)
                    g.DrawString(txt, fLbl, bLbl, p.X - sz.Width - 4, p.Y - sz.Height / 2)
                End If
                If hayCaraInt Then
                    Dim xRef = xCaraInt - dto.CaraNegativa.Recubrimiento
                    Dim p = _w2s(xRef, yMid)
                    Dim txt = $"{dto.CaraNegativa.BarraPrincipal}@{dto.CaraNegativa.SeparacionCm:0}"
                    Dim sz = g.MeasureString(txt, fLbl)
                    g.FillRectangle(Brushes.White, p.X + 2, p.Y - sz.Height / 2, sz.Width + 4, sz.Height)
                    g.DrawString(txt, fLbl, bLbl, p.X + 4, p.Y - sz.Height / 2)
                End If
            End Using
        Next
    End Sub

    ' --- Zoom/pan ---
    Private Function CentroDibujo() As Point
        Return New Point(_dibujo.Width \ 2, _dibujo.Height \ 2)
    End Function

    Private Sub AplicarZoom(factor As Double, cursorPt As Point)
        Dim nuevoZoom = _zoomUser * factor
        If nuevoZoom < 0.2 OrElse nuevoZoom > 30 Then Return
        Dim cx = _dibujo.Width / 2.0, cy = _dibujo.Height / 2.0
        _panUserX = (cursorPt.X - cx) * (1 - factor) + _panUserX * factor
        _panUserY = (cursorPt.Y - cy) * (1 - factor) + _panUserY * factor
        _zoomUser = nuevoZoom
        If _lblZoomInfo IsNot Nothing Then _lblZoomInfo.Text = $"Zoom: {_zoomUser * 100:0}%"
        _dibujo.Invalidate()
    End Sub

    Private Sub AjustarVistaDibujo()
        _zoomUser = 1.0
        _panUserX = 0
        _panUserY = 0
        If _lblZoomInfo IsNot Nothing Then _lblZoomInfo.Text = "Zoom: 100%"
        _dibujo.Invalidate()
    End Sub

    Private Sub Dibujo_Resize(sender As Object, e As EventArgs)
        If _btnZoomIn Is Nothing Then Return
        Dim margen As Integer = 10, gap As Integer = 4
        _btnFit.Location = New Point(_dibujo.Width - 30 - margen, margen)
        _btnZoomIn.Location = New Point(_dibujo.Width - 60 - margen - gap, margen)
        _btnZoomOut.Location = New Point(_dibujo.Width - 90 - margen - 2 * gap, margen)
        _lblZoomInfo.Location = New Point(_dibujo.Width - 90 - margen - 2 * gap, margen + 32)
        _dibujo.Invalidate()
    End Sub

    Private Sub Dibujo_MouseWheel(sender As Object, e As MouseEventArgs)
        AplicarZoom(If(e.Delta > 0, 1.2, 1.0 / 1.2), e.Location)
    End Sub

    Private Sub Dibujo_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left OrElse e.Button = MouseButtons.Middle Then
            _arrastrando = True
            _ultimoMouse = e.Location
            _dibujo.Cursor = Cursors.SizeAll
        End If
    End Sub

    Private Sub Dibujo_MouseMove(sender As Object, e As MouseEventArgs)
        If _arrastrando Then
            _panUserX += e.X - _ultimoMouse.X
            _panUserY += e.Y - _ultimoMouse.Y
            _ultimoMouse = e.Location
            _dibujo.Invalidate()
        End If
    End Sub

    Private Sub Dibujo_MouseUp(sender As Object, e As MouseEventArgs)
        _arrastrando = False
        _dibujo.Cursor = Cursors.Hand
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    Private Class PanelDobleBuffer
        Inherits Panel
        Sub New()
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        End Sub
    End Class

End Class
