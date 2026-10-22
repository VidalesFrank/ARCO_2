Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 12 — Muros de Contención · formulario principal
'
'  Flujo:
'    1. El usuario abre el form (desde el menú principal) con un muro default.
'    2. Rellena las entradas (Info, Geometría, Suelo, Materiales/Normativa, Opciones).
'    3. Pulsa "Analizar" → corre MuroContencionService → se repintan planta y tablas.
'    4. En la tabla de diseño edita Barra / Separación → RecalcularAsProv →
'       se refresca el C/D de flexión al instante (programa de revisión).
'    5. Guarda a .esm con BinaryFormatter (reusa Funciones_Programa.Serializar).
'       Comparte la extensión con el .esm del edificio, pero el contenido es un
'       MuroContencion — se abre solo desde este form; abrir desde Form_00 tira
'       InvalidCastException porque los tipos no coinciden.
'
'  Layout:
'    - Toolbar superior (Nuevo/Abrir/Guardar/Analizar/Reporte + nombre archivo).
'    - SplitContainer horizontal:
'       · Izquierda: TabControl con 5 tabs de entrada.
'       · Derecha: SplitContainer vertical con dibujo del muro arriba + tablas abajo.
'    - Status bar (resultado del último análisis, veredicto global).
' ═══════════════════════════════════════════════════════════════════════════════
<DesignerCategory("Code")>
Public Class Form_12_MuroContencion
    Inherits Form

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Estado
    ' ═══════════════════════════════════════════════════════════════════════════
    Private _muro As MuroContencion
    Private _rutaArchivo As String = ""
    Private _dirty As Boolean = False
    Private _cargandoControles As Boolean = False   ' evita el loop de eventos TextChanged → Dirty

    ' Paleta ARCO
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly VerdeARCO As Color = Color.FromArgb(27, 175, 122)
    Private Shared ReadOnly RojoARCO As Color = Color.FromArgb(227, 73, 72)
    Private Shared ReadOnly AmbarARCO As Color = Color.FromArgb(237, 161, 0)
    Private Shared ReadOnly ColorBoton As Color = Color.FromArgb(224, 224, 224)
    Private Shared ReadOnly ColorBg As Color = Color.FromArgb(245, 248, 252)

    ' Controles principales
    Private _lblTitulo As Label
    Private _lblEstado As Label
    Private _lblVeredicto As Label
    Private _lblRuta As Label
    Private _tabs As TabControl
    Private _dibujo As PanelDobleBuffer
    Private _gridEstabilidad As DataGridView
    Private _gridDiseno As DataGridView
    Private _layoutMain As TableLayoutPanel
    Private _layoutDer As TableLayoutPanel

    ' Estado del zoom/pan del dibujo
    Private _zoomUser As Double = 1.0
    Private _panUserX As Double = 0
    Private _panUserY As Double = 0
    Private _arrastrando As Boolean = False
    Private _ultimoMouse As Point
    Private _btnZoomIn As Button
    Private _btnZoomOut As Button
    Private _btnFit As Button
    Private _lblZoomInfo As Label

    ' Controles de entrada — Info
    Private _txtNombre As TextBox
    Private _txtProyecto As TextBox
    Private _txtCliente As TextBox
    Private _txtResponsable As TextBox
    Private _txtCodigo As TextBox
    Private _dtpFecha As DateTimePicker
    Private _txtDescripcion As TextBox

    ' Geometría
    Private _nudH As NumericUpDown
    Private _nudh1 As NumericUpDown
    Private _nudh2 As NumericUpDown
    Private _nudB As NumericUpDown
    Private _nuda As NumericUpDown
    Private _nudb1 As NumericUpDown
    Private _nude1 As NumericUpDown
    Private _nude2 As NumericUpDown
    Private _chkLlave As CheckBox
    Private _nudBLL As NumericUpDown
    Private _nudHLL As NumericUpDown

    ' Suelo
    Private _nudPhi As NumericUpDown
    Private _nudDelta As NumericUpDown
    Private _nudGammaS As NumericUpDown
    Private _nudGammaC As NumericUpDown
    Private _nudDf As NumericUpDown
    Private _nudQadm As NumericUpDown
    Private _nudMuBase As NumericUpDown
    Private _cmbTeoria As ComboBox
    Private _chkRellenoFrontal As CheckBox
    Private _chkIncluirSismo As CheckBox
    Private _nudKh As NumericUpDown
    Private _nudKv As NumericUpDown
    Private _chkSobrecarga As CheckBox
    Private _nudSobrecarga As NumericUpDown
    Private _nudDistanciaTrafico As NumericUpDown

    ' Materiales / Normativa
    Private _cmbNormativa As ComboBox
    Private _nudFc As NumericUpDown
    Private _nudFy As NumericUpDown
    Private _nudPesoConcreto As NumericUpDown
    Private _nudRecubrimiento As NumericUpDown

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Punto de entrada
    ' ═══════════════════════════════════════════════════════════════════════════
    Public Shared Sub Mostrar()
        Dim f As New Form_12_MuroContencion()
        f.Show()
    End Sub

    Public Sub New()
        _muro = New MuroContencion()
        BuildUI()
        CargarControlesDesdeMuro()
        _dirty = False
        ActualizarTitulo()
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Construcción UI
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub BuildUI()
        Me.Text = "Muros de Contención — ARCO"
        Me.Size = New Size(1280, 820)
        Me.MinimumSize = New Size(1080, 680)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = ColorBg
        Me.Font = New Font("Segoe UI", 9)
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ' --- Header (franja gris con título) ------------------------------
        Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 56, .BackColor = GrisARCO}
        _lblTitulo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 13, FontStyle.Bold),
            .Text = "   Análisis y Revisión de Muros de Contención",
            .TextAlign = ContentAlignment.MiddleLeft
        }
        header.Controls.Add(_lblTitulo)

        ' --- Toolbar ------------------------------------------------------
        Dim toolbar As New Panel With {.Dock = DockStyle.Top, .Height = 48, .BackColor = Color.FromArgb(60, 60, 60), .Padding = New Padding(6, 6, 6, 6)}
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
        ' Posicionar
        btnNuevo.Location = New Point(6, 6)
        btnAbrir.Location = New Point(100, 6)
        btnGuardar.Location = New Point(194, 6)
        btnGuardarComo.Location = New Point(288, 6)
        btnAnalizar.Location = New Point(410, 6)
        btnReporte.Location = New Point(524, 6)
        toolbar.Controls.AddRange(New Control() {btnNuevo, btnAbrir, btnGuardar, btnGuardarComo, btnAnalizar, btnReporte, _lblRuta})

        ' --- Status bar inferior ------------------------------------------
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

        ' --- Layout principal (izquierda = tabs, derecha = visualización) ---
        ' Se usa TableLayoutPanel y no SplitContainer: SplitContainer anidados en
        ' construcción programática sin Designer disparan InvalidOperationException
        ' al setear SplitterDistance mientras los paneles no tienen tamaño real.
        _layoutMain = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = ColorBg
        }
        _layoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 400))
        _layoutMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        _layoutMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        ' --- Tabs de entrada ---
        _tabs = ConstruirTabs()
        _tabs.Dock = DockStyle.Fill
        _layoutMain.Controls.Add(_tabs, 0, 0)

        ' --- Panel derecho: dibujo (arriba) + grillas (abajo) ---
        _layoutDer = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = ColorBg
        }
        _layoutDer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        _layoutDer.RowStyles.Add(New RowStyle(SizeType.Percent, 45))
        _layoutDer.RowStyles.Add(New RowStyle(SizeType.Percent, 55))

        _dibujo = New PanelDobleBuffer With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Cursor = Cursors.Hand
        }
        AddHandler _dibujo.Paint, AddressOf Dibujo_Paint
        AddHandler _dibujo.Resize, AddressOf Dibujo_Resize
        AddHandler _dibujo.MouseWheel, AddressOf Dibujo_MouseWheel
        AddHandler _dibujo.MouseDown, AddressOf Dibujo_MouseDown
        AddHandler _dibujo.MouseMove, AddressOf Dibujo_MouseMove
        AddHandler _dibujo.MouseUp, AddressOf Dibujo_MouseUp
        AddHandler _dibujo.MouseEnter, Sub() _dibujo.Focus()  ' habilita la rueda sin click previo

        ' --- Controles flotantes en la esquina sup-der del dibujo ---------
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

        Dim panelGrids As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .BackColor = ColorBg
        }
        panelGrids.RowStyles.Add(New RowStyle(SizeType.Absolute, 28))
        panelGrids.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
        panelGrids.RowStyles.Add(New RowStyle(SizeType.Absolute, 28))
        panelGrids.RowStyles.Add(New RowStyle(SizeType.Percent, 50))

        panelGrids.Controls.Add(TituloBloque("Estabilidad externa"), 0, 0)
        _gridEstabilidad = ConstruirGridEstabilidad()
        panelGrids.Controls.Add(_gridEstabilidad, 0, 1)
        panelGrids.Controls.Add(TituloBloque("Diseño y revisión de refuerzo (edite barra y separación)"), 0, 2)
        _gridDiseno = ConstruirGridDiseno()
        panelGrids.Controls.Add(_gridDiseno, 0, 3)

        _layoutDer.Controls.Add(_dibujo, 0, 0)
        _layoutDer.Controls.Add(panelGrids, 0, 1)

        _layoutMain.Controls.Add(_layoutDer, 1, 0)

        ' --- Ensamblar ---
        Me.Controls.Add(_layoutMain)
        Me.Controls.Add(statusBar)
        Me.Controls.Add(toolbar)
        Me.Controls.Add(header)
    End Sub

    Private Function CrearBoton(texto As String, bg As Color, fg As Color, ancho As Integer) As Button
        Dim b As New Button With {
            .Text = texto,
            .BackColor = bg,
            .ForeColor = fg,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9),
            .Size = New Size(ancho, 32),
            .Cursor = Cursors.Hand
        }
        b.FlatAppearance.BorderSize = 0
        Return b
    End Function

    Private Function TituloBloque(texto As String) As Label
        Return New Label With {
            .Text = "  " & texto,
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(70, 70, 70),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
    End Function

    Private Function ConstruirTabs() As TabControl
        Dim tc As New TabControl With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 9)}

        ' --- TAB: INFO ---------------------------------------------------
        Dim tInfo As New TabPage("Información")
        Dim pInfo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim yI As Integer = 8
        pInfo.Controls.Add(EtiquetaIn("Nombre del muro", yI)) : yI += 20
        _txtNombre = TxtIn(yI, 400) : pInfo.Controls.Add(_txtNombre) : yI += 32
        pInfo.Controls.Add(EtiquetaIn("Proyecto", yI)) : yI += 20
        _txtProyecto = TxtIn(yI, 400) : pInfo.Controls.Add(_txtProyecto) : yI += 32
        pInfo.Controls.Add(EtiquetaIn("Cliente", yI)) : yI += 20
        _txtCliente = TxtIn(yI, 400) : pInfo.Controls.Add(_txtCliente) : yI += 32
        pInfo.Controls.Add(EtiquetaIn("Responsable", yI)) : yI += 20
        _txtResponsable = TxtIn(yI, 400) : pInfo.Controls.Add(_txtResponsable) : yI += 32
        pInfo.Controls.Add(EtiquetaIn("Código", yI)) : yI += 20
        _txtCodigo = TxtIn(yI, 200) : pInfo.Controls.Add(_txtCodigo) : yI += 32
        pInfo.Controls.Add(EtiquetaIn("Fecha", yI)) : yI += 20
        _dtpFecha = New DateTimePicker With {.Location = New Point(0, yI), .Width = 200, .Format = DateTimePickerFormat.Long}
        AddHandler _dtpFecha.ValueChanged, AddressOf OnInputChanged
        pInfo.Controls.Add(_dtpFecha) : yI += 32
        pInfo.Controls.Add(EtiquetaIn("Descripción", yI)) : yI += 20
        _txtDescripcion = New TextBox With {.Location = New Point(0, yI), .Width = 400, .Height = 60, .Multiline = True}
        AddHandler _txtDescripcion.TextChanged, AddressOf OnInputChanged
        pInfo.Controls.Add(_txtDescripcion)
        tInfo.Controls.Add(pInfo)

        ' --- TAB: GEOMETRÍA ----------------------------------------------
        Dim tGeo As New TabPage("Geometría")
        Dim pGeo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim yG As Integer = 8
        _nudH = NumIn(pGeo, "Altura total H (m)", yG, 0.5, 15, 0.1, 2) : yG += 48
        _nudh1 = NumIn(pGeo, "Espesor losa h1 (m)", yG, 0.1, 2, 0.05, 2) : yG += 48
        _nudh2 = NumIn(pGeo, "Altura vástago h2 (m)", yG, 0.1, 15, 0.1, 2) : yG += 48
        _nudB = NumIn(pGeo, "Ancho zapata B (m)", yG, 0.3, 10, 0.1, 2) : yG += 48
        _nuda = NumIn(pGeo, "Distancia talón → eje vástago a (m)", yG, 0.1, 10, 0.05, 2) : yG += 48
        _nudb1 = NumIn(pGeo, "Puntera: eje vástago → pie b1 (m)", yG, 0.0, 10, 0.05, 2) : yG += 48
        _nude1 = NumIn(pGeo, "Espesor vástago base e1 (m)", yG, 0.1, 2, 0.05, 2) : yG += 48
        _nude2 = NumIn(pGeo, "Espesor vástago corona e2 (m)", yG, 0.1, 2, 0.05, 2) : yG += 48
        _chkLlave = New CheckBox With {.Text = "Incluir llave de cortante", .Location = New Point(0, yG), .AutoSize = True}
        AddHandler _chkLlave.CheckedChanged, Sub()
                                                 OnInputChanged(Nothing, Nothing)
                                                 _nudBLL.Enabled = _chkLlave.Checked
                                                 _nudHLL.Enabled = _chkLlave.Checked
                                             End Sub
        pGeo.Controls.Add(_chkLlave) : yG += 28
        _nudBLL = NumIn(pGeo, "Ancho llave B_LL (m)", yG, 0.1, 2, 0.05, 2) : yG += 48
        _nudHLL = NumIn(pGeo, "Profundidad llave H_LL (m)", yG, 0.1, 2, 0.05, 2) : yG += 48
        tGeo.Controls.Add(pGeo)

        ' --- TAB: SUELO ---------------------------------------------------
        Dim tSuelo As New TabPage("Suelo")
        Dim pSuelo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim yS As Integer = 8
        _cmbTeoria = New ComboBox With {.Location = New Point(0, yS + 20), .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList}
        _cmbTeoria.Items.AddRange(New String() {"Coulomb (con fricción muro-suelo)", "Rankine (sin fricción)"})
        AddHandler _cmbTeoria.SelectedIndexChanged, AddressOf OnInputChanged
        pSuelo.Controls.Add(EtiquetaIn("Teoría de empuje", yS))
        pSuelo.Controls.Add(_cmbTeoria) : yS += 56
        _nudPhi = NumIn(pSuelo, "Ángulo de fricción φ (°)", yS, 10, 50, 1, 1) : yS += 48
        _nudDelta = NumIn(pSuelo, "Fricción muro-suelo δ (°)", yS, 0, 40, 1, 1) : yS += 48
        _nudGammaS = NumIn(pSuelo, "Peso suelo relleno γ (kN/m³)", yS, 10, 25, 0.5, 1) : yS += 48
        _nudGammaC = NumIn(pSuelo, "Peso suelo cimentación (kN/m³)", yS, 10, 25, 0.5, 1) : yS += 48
        _nudDf = NumIn(pSuelo, "Profundidad desplante Df (m)", yS, 0.3, 10, 0.1, 2) : yS += 48
        _nudQadm = NumIn(pSuelo, "Capacidad portante admisible (kPa)", yS, 50, 1000, 10, 0) : yS += 48
        _nudMuBase = NumIn(pSuelo, "μ base-suelo (0 = usar tan(2/3·φ))", yS, 0, 1, 0.05, 2) : yS += 48
        _chkRellenoFrontal = New CheckBox With {.Text = "Incluir empuje pasivo frontal", .Location = New Point(0, yS), .AutoSize = True, .Checked = True}
        AddHandler _chkRellenoFrontal.CheckedChanged, AddressOf OnInputChanged
        pSuelo.Controls.Add(_chkRellenoFrontal) : yS += 32
        pSuelo.Controls.Add(SeparadorIn(yS)) : yS += 10
        _chkIncluirSismo = New CheckBox With {.Text = "Incluir caso sísmico (Mononobe-Okabe)", .Location = New Point(0, yS), .AutoSize = True}
        AddHandler _chkIncluirSismo.CheckedChanged, Sub()
                                                        OnInputChanged(Nothing, Nothing)
                                                        _nudKh.Enabled = _chkIncluirSismo.Checked
                                                        _nudKv.Enabled = _chkIncluirSismo.Checked
                                                    End Sub
        pSuelo.Controls.Add(_chkIncluirSismo) : yS += 32
        _nudKh = NumIn(pSuelo, "Coef. sísmico horizontal Kh", yS, 0, 0.5, 0.01, 2) : yS += 48
        _nudKv = NumIn(pSuelo, "Coef. sísmico vertical Kv", yS, 0, 0.3, 0.01, 2) : yS += 48
        pSuelo.Controls.Add(SeparadorIn(yS)) : yS += 10
        _chkSobrecarga = New CheckBox With {.Text = "Incluir sobrecarga sobre relleno", .Location = New Point(0, yS), .AutoSize = True}
        AddHandler _chkSobrecarga.CheckedChanged, Sub()
                                                      OnInputChanged(Nothing, Nothing)
                                                      _nudSobrecarga.Enabled = _chkSobrecarga.Checked
                                                      _nudDistanciaTrafico.Enabled = _chkSobrecarga.Checked
                                                  End Sub
        pSuelo.Controls.Add(_chkSobrecarga) : yS += 32
        _nudSobrecarga = NumIn(pSuelo, "Sobrecarga (kPa) — para residencial", yS, 0, 50, 1, 1) : yS += 48
        _nudDistanciaTrafico = NumIn(pSuelo, "Distancia al tráfico (mm) — para vial", yS, 0, 1000, 50, 0) : yS += 48
        tSuelo.Controls.Add(pSuelo)

        ' --- TAB: MATERIALES Y NORMATIVA ---------------------------------
        Dim tMat As New TabPage("Materiales y normativa")
        Dim pMat As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12), .AutoScroll = True, .BackColor = Color.White}
        Dim yM As Integer = 8
        pMat.Controls.Add(EtiquetaIn("Normativa aplicable", yM)) : yM += 20
        _cmbNormativa = New ComboBox With {.Location = New Point(0, yM), .Width = 300, .DropDownStyle = ComboBoxStyle.DropDownList}
        _cmbNormativa.Items.AddRange(New String() {"Residencial (NSR-10)", "Vial (CCP-14 / AASHTO LRFD)"})
        AddHandler _cmbNormativa.SelectedIndexChanged, AddressOf OnInputChanged
        pMat.Controls.Add(_cmbNormativa) : yM += 40
        _nudFc = NumIn(pMat, "f'c concreto (MPa)", yM, 14, 50, 1, 0) : yM += 48
        _nudFy = NumIn(pMat, "fy acero (MPa)", yM, 240, 500, 10, 0) : yM += 48
        _nudPesoConcreto = NumIn(pMat, "Peso concreto (kN/m³)", yM, 20, 28, 0.5, 1) : yM += 48
        _nudRecubrimiento = NumIn(pMat, "Recubrimiento (m)", yM, 0.03, 0.1, 0.005, 3) : yM += 48
        tMat.Controls.Add(pMat)

        tc.TabPages.AddRange(New TabPage() {tInfo, tGeo, tSuelo, tMat})
        Return tc
    End Function

    Private Function EtiquetaIn(texto As String, y As Integer) As Label
        Return New Label With {
            .Text = texto, .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = GrisARCO, .Location = New Point(0, y), .AutoSize = True
        }
    End Function

    Private Function TxtIn(y As Integer, ancho As Integer) As TextBox
        Dim t As New TextBox With {.Location = New Point(0, y), .Width = ancho}
        AddHandler t.TextChanged, AddressOf OnInputChanged
        Return t
    End Function

    Private Function NumIn(parent As Control, label As String, y As Integer,
                           minV As Decimal, maxV As Decimal, step_ As Decimal, decimales As Integer) As NumericUpDown
        parent.Controls.Add(EtiquetaIn(label, y))
        Dim nud As New NumericUpDown With {
            .Location = New Point(0, y + 20), .Width = 200,
            .Minimum = minV, .Maximum = maxV, .Increment = step_,
            .DecimalPlaces = decimales
        }
        AddHandler nud.ValueChanged, AddressOf OnInputChanged
        parent.Controls.Add(nud)
        Return nud
    End Function

    Private Function SeparadorIn(y As Integer) As Label
        Return New Label With {
            .Location = New Point(0, y), .Width = 400, .Height = 1,
            .BackColor = Color.FromArgb(220, 220, 220),
            .BorderStyle = BorderStyle.FixedSingle
        }
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Grillas de resultados
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Function ConstruirGridEstabilidad() As DataGridView
        Dim g As New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .Font = New Font("Segoe UI", 8.5),
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
        g.Columns.Add("Caso", "Caso")
        g.Columns.Add("Chequeo", "Chequeo")
        g.Columns.Add("Calculado", "Calculado")
        g.Columns.Add("Admisible", "Admisible")
        g.Columns.Add("CD", "C/D")
        g.Columns.Add("Veredicto", "Veredicto")
        g.Columns("Caso").Width = 70
        g.Columns("Chequeo").Width = 150
        g.Columns("Calculado").Width = 90
        g.Columns("Admisible").Width = 90
        g.Columns("CD").Width = 60
        g.Columns("Veredicto").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        Return g
    End Function

    Private Function ConstruirGridDiseno() As DataGridView
        Dim g As New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .Font = New Font("Segoe UI", 8.5),
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
        g.Columns.Add("Elemento", "Elemento")
        Dim cmbBarra As New DataGridViewComboBoxColumn With {.Name = "Barra", .HeaderText = "Barra"}
        cmbBarra.Items.AddRange(New String() {"#3", "#4", "#5", "#6", "#7", "#8"})
        g.Columns.Add(cmbBarra)
        g.Columns.Add("Sep", "Sep (cm)")
        g.Columns.Add("AsProv", "As prov (mm²/m)")
        g.Columns.Add("AsReq", "As req (mm²/m)")
        g.Columns.Add("CDFlex", "C/D Flex")
        g.Columns.Add("Vu", "Vu (kN)")
        g.Columns.Add("PhiVc", "φVc (kN)")
        g.Columns.Add("CDCort", "C/D Cort")
        g.Columns.Add("Veredicto", "Veredicto")
        For Each col As DataGridViewColumn In g.Columns
            If col.Name = "Elemento" OrElse col.Name = "Barra" OrElse col.Name = "Sep" Then
                col.ReadOnly = False
            Else
                col.ReadOnly = True
            End If
        Next
        g.Columns("Elemento").ReadOnly = True
        g.Columns("Elemento").Width = 100
        g.Columns("Barra").Width = 60
        g.Columns("Sep").Width = 70
        g.Columns("Veredicto").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

        AddHandler g.CellEndEdit, AddressOf GridDiseno_CellEndEdit
        Return g
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Comandos (Nuevo / Abrir / Guardar / Analizar / Reporte)
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub CmdNuevo(sender As Object, e As EventArgs)
        If Not ConfirmarDescartar() Then Return
        _muro = New MuroContencion()
        _rutaArchivo = ""
        CargarControlesDesdeMuro()
        _dirty = False
        _dibujo.Invalidate()
        LimpiarGrids()
        _lblEstado.Text = "Muro nuevo. Ajuste las entradas y pulse ANALIZAR."
        _lblVeredicto.Text = ""
        _lblVeredicto.BackColor = Color.Transparent
        ActualizarTitulo()
    End Sub

    Private Sub CmdAbrir(sender As Object, e As EventArgs)
        If Not ConfirmarDescartar() Then Return
        Using ofd As New OpenFileDialog() With {
            .Filter = "Muro de contención (*.esm)|*.esm",
            .Title = "Abrir muro de contención"
        }
            If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                _muro = Funciones_Programa.DeSerializar(Of MuroContencion)(ofd.FileName)
                If _muro Is Nothing Then
                    MessageBox.Show(Me, "No se pudo leer el archivo.", "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                _rutaArchivo = ofd.FileName
                CargarControlesDesdeMuro()
                _dirty = False
                PintarResultados()
                _dibujo.Invalidate()
                ActualizarTitulo()
                _lblEstado.Text = "Muro cargado desde " & Path.GetFileName(_rutaArchivo)
            Catch ex As Exception
                Logger.Error(ex, "Form_12_MuroContencion.CmdAbrir", ofd.FileName)
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
            .Filter = "Muro de contención (*.esm)|*.esm",
            .DefaultExt = "esm",
            .FileName = If(String.IsNullOrWhiteSpace(_muro.Info.Nombre), "Muro", _muro.Info.Nombre)
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
            Logger.Error(ex, "Form_12_MuroContencion.Guardar", ruta)
            MessageBox.Show(Me, "Error al guardar: " & ex.Message, "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CmdAnalizar(sender As Object, e As EventArgs)
        Try
            VolcarControlesAMuro()
            MuroContencionService.AnalizarMuro(_muro)
            PintarResultados()
            _dibujo.Invalidate()
            _lblEstado.Text = "Análisis completado — " & DateTime.Now.ToString("HH:mm:ss")
        Catch ex As Exception
            Logger.Error(ex, "Form_12_MuroContencion.CmdAnalizar", "")
            MessageBox.Show(Me, "Error en el análisis:" & vbCrLf & ex.Message, "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CmdReporte(sender As Object, e As EventArgs)
        If _muro.Resultados Is Nothing Then
            MessageBox.Show(Me, "Primero pulse ANALIZAR para generar los resultados.",
                             "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using sfd As New SaveFileDialog() With {
            .Filter = "Libro de Excel (*.xlsx)|*.xlsx",
            .DefaultExt = "xlsx",
            .FileName = NombreArchivoReporte()
        }
            If sfd.ShowDialog(Me) <> DialogResult.OK Then Return

            Me.Cursor = Cursors.WaitCursor
            Try
                VolcarControlesAMuro()
                MuroContencionReporteService.Exportar(_muro, sfd.FileName)
                _lblEstado.Text = "Reporte generado — " & Path.GetFileName(sfd.FileName)
                Try
                    Process.Start(New ProcessStartInfo(sfd.FileName) With {.UseShellExecute = True})
                Catch
                    ' Si no hay app asociada al .xlsx, no es un error del reporte.
                End Try
            Catch ex As Exception
                Logger.Error(ex, "Form_12_MuroContencion.CmdReporte", sfd.FileName)
                MessageBox.Show(Me, "Error al generar el reporte:" & vbCrLf & ex.Message,
                                 "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Using
    End Sub

    Private Function NombreArchivoReporte() As String
        Dim nombre As String = If(String.IsNullOrWhiteSpace(_muro.Info.Nombre), "MuroContencion", _muro.Info.Nombre.Trim())
        Dim codigo As String = _muro.Info.Codigo.Trim()
        Dim prefijo As String = If(String.IsNullOrWhiteSpace(codigo), "", codigo & "_")
        Return $"{prefijo}{nombre}_Reporte_{Date.Now:yyyy-MM-dd}"
    End Function

    Private Function ConfirmarDescartar() As Boolean
        If Not _dirty Then Return True
        Dim r = MessageBox.Show(Me, "Hay cambios sin guardar. ¿Descartar?",
                                 "ARCO", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        Return r = DialogResult.Yes
    End Function

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _dirty AndAlso Not ConfirmarDescartar() Then
            e.Cancel = True
            Return
        End If
        MyBase.OnFormClosing(e)
    End Sub

    ' TableLayoutPanel se auto-dimensiona, no necesita ajuste en OnShown.

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

            _nudH.Value = AClamp(m.Geometria.H, _nudH)
            _nudh1.Value = AClamp(m.Geometria.h1, _nudh1)
            _nudh2.Value = AClamp(m.Geometria.h2, _nudh2)
            _nudB.Value = AClamp(m.Geometria.B, _nudB)
            _nuda.Value = AClamp(m.Geometria.a, _nuda)
            _nudb1.Value = AClamp(m.Geometria.b1, _nudb1)
            _nude1.Value = AClamp(m.Geometria.e1, _nude1)
            _nude2.Value = AClamp(m.Geometria.e2, _nude2)
            _chkLlave.Checked = m.Opciones.IncluirLlave
            _nudBLL.Value = AClamp(m.Geometria.B_LL, _nudBLL)
            _nudHLL.Value = AClamp(m.Geometria.H_LL, _nudHLL)
            _nudBLL.Enabled = _chkLlave.Checked
            _nudHLL.Enabled = _chkLlave.Checked

            _cmbTeoria.SelectedIndex = If(m.Suelo.Teoria = eTeoriaEmpuje.Coulomb, 0, 1)
            _nudPhi.Value = AClamp(m.Suelo.Phi, _nudPhi)
            _nudDelta.Value = AClamp(m.Suelo.Delta, _nudDelta)
            _nudGammaS.Value = AClamp(m.Suelo.GammaSuelo, _nudGammaS)
            _nudGammaC.Value = AClamp(m.Suelo.GammaCimentacion, _nudGammaC)
            _nudDf.Value = AClamp(m.Suelo.Df, _nudDf)
            _nudQadm.Value = AClamp(m.Suelo.CapacidadPortante, _nudQadm)
            _nudMuBase.Value = AClamp(m.Suelo.MuBase, _nudMuBase)
            _chkRellenoFrontal.Checked = m.Opciones.IncluirRellenoFrontal

            _chkIncluirSismo.Checked = m.SueloSismico.IncluirSismo
            _nudKh.Value = AClamp(m.SueloSismico.Kh, _nudKh)
            _nudKv.Value = AClamp(m.SueloSismico.Kv, _nudKv)
            _nudKh.Enabled = _chkIncluirSismo.Checked
            _nudKv.Enabled = _chkIncluirSismo.Checked

            _chkSobrecarga.Checked = m.Sobrecarga.IncluirSobrecarga
            _nudSobrecarga.Value = AClamp(m.Sobrecarga.SobrecargaKPa, _nudSobrecarga)
            _nudDistanciaTrafico.Value = AClamp(m.Sobrecarga.DistanciaTraficoMm, _nudDistanciaTrafico)
            _nudSobrecarga.Enabled = _chkSobrecarga.Checked
            _nudDistanciaTrafico.Enabled = _chkSobrecarga.Checked

            _cmbNormativa.SelectedIndex = If(m.Normativa = eNormativaMuro.Vial, 1, 0)
            _nudFc.Value = AClamp(m.Materiales.Fc, _nudFc)
            _nudFy.Value = AClamp(m.Materiales.Fy, _nudFy)
            _nudPesoConcreto.Value = AClamp(m.Materiales.PesoConcreto, _nudPesoConcreto)
            _nudRecubrimiento.Value = AClamp(m.Materiales.Recubrimiento, _nudRecubrimiento)
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

        m.Geometria.H = CDbl(_nudH.Value)
        m.Geometria.h1 = CDbl(_nudh1.Value)
        m.Geometria.h2 = CDbl(_nudh2.Value)
        m.Geometria.B = CDbl(_nudB.Value)
        m.Geometria.a = CDbl(_nuda.Value)
        m.Geometria.b1 = CDbl(_nudb1.Value)
        m.Geometria.e1 = CDbl(_nude1.Value)
        m.Geometria.e2 = CDbl(_nude2.Value)
        m.Geometria.B_LL = CDbl(_nudBLL.Value)
        m.Geometria.H_LL = CDbl(_nudHLL.Value)

        m.Suelo.Teoria = If(_cmbTeoria.SelectedIndex = 0, eTeoriaEmpuje.Coulomb, eTeoriaEmpuje.Rankine)
        m.Suelo.Phi = CDbl(_nudPhi.Value)
        m.Suelo.Delta = CDbl(_nudDelta.Value)
        m.Suelo.GammaSuelo = CDbl(_nudGammaS.Value)
        m.Suelo.GammaCimentacion = CDbl(_nudGammaC.Value)
        m.Suelo.Df = CDbl(_nudDf.Value)
        m.Suelo.CapacidadPortante = CDbl(_nudQadm.Value)
        m.Suelo.MuBase = CDbl(_nudMuBase.Value)

        m.SueloSismico.IncluirSismo = _chkIncluirSismo.Checked
        m.SueloSismico.Kh = CDbl(_nudKh.Value)
        m.SueloSismico.Kv = CDbl(_nudKv.Value)

        m.Sobrecarga.IncluirSobrecarga = _chkSobrecarga.Checked
        m.Sobrecarga.SobrecargaKPa = CDbl(_nudSobrecarga.Value)
        m.Sobrecarga.DistanciaTraficoMm = CDbl(_nudDistanciaTrafico.Value)

        m.Opciones.IncluirLlave = _chkLlave.Checked
        m.Opciones.IncluirRellenoFrontal = _chkRellenoFrontal.Checked

        m.Normativa = If(_cmbNormativa.SelectedIndex = 1, eNormativaMuro.Vial, eNormativaMuro.Residencial)
        m.Materiales.Fc = CSng(_nudFc.Value)
        m.Materiales.Fy = CSng(_nudFy.Value)
        m.Materiales.PesoConcreto = CSng(_nudPesoConcreto.Value)
        m.Materiales.Recubrimiento = CSng(_nudRecubrimiento.Value)
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
        Me.Text = "Muros de Contención — " & archivo & If(_dirty, " *", "") & "   —   ARCO"
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Pintado de resultados
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Sub PintarResultados()
        PintarGridEstabilidad()
        PintarGridDiseno()
        PintarVeredictoGlobal()
    End Sub

    Private Sub PintarGridEstabilidad()
        _gridEstabilidad.Rows.Clear()
        If _muro.Resultados Is Nothing OrElse _muro.Resultados.Estabilidad Is Nothing Then Return
        Dim est = _muro.Resultados.Estabilidad
        AgregarFilaEst("Estático", est.CasoEstatico)
        If est.CasoSismico IsNot Nothing Then AgregarFilaEst("Sísmico", est.CasoSismico)
    End Sub

    Private Sub AgregarFilaEst(nombreCaso As String, c As ChequeoCaso)
        AgregarFilaChequeo(nombreCaso, "Volcamiento",
                           c.FsVolcamiento.ToString("0.00"),
                           c.FsVolcamientoAdmisible.ToString("0.00"),
                           c.FactorVolcamiento, c.CumpleVolcamiento)
        AgregarFilaChequeo(nombreCaso, "Deslizamiento",
                           c.FsDeslizamiento.ToString("0.00"),
                           c.FsDeslizamientoAdmisible.ToString("0.00"),
                           c.FactorDeslizamiento, c.CumpleDeslizamiento)
        AgregarFilaChequeo(nombreCaso, "Capacidad portante (kPa)",
                           c.PresionMax.ToString("0.0"),
                           c.PresionAdmisible.ToString("0.0"),
                           c.FactorCapacidadPortante, c.CumpleCapacidadPortante)
        AgregarFilaChequeo(nombreCaso, "Excentricidad (m)",
                           c.Excentricidad.ToString("0.000"),
                           c.ExcentricidadAdmisible.ToString("0.000"),
                           c.FactorExcentricidad, c.CumpleExcentricidad)
    End Sub

    Private Sub AgregarFilaChequeo(caso As String, nombre As String,
                                    valCalc As String, valAdm As String,
                                    cd As Double, cumple As Boolean)
        Dim idx = _gridEstabilidad.Rows.Add(caso, nombre, valCalc, valAdm,
                                              FormatearCD(cd),
                                              If(cumple, "✓ Cumple", "✗ No cumple"))
        Dim fila = _gridEstabilidad.Rows(idx)
        For Each col As DataGridViewCell In fila.Cells
            col.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        fila.Cells("Caso").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
        fila.Cells("Chequeo").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
        fila.Cells("Veredicto").Style.BackColor = If(cumple, Color.FromArgb(220, 237, 220), Color.FromArgb(248, 215, 215))
        fila.Cells("Veredicto").Style.ForeColor = If(cumple, Color.FromArgb(27, 94, 32), Color.FromArgb(176, 0, 32))
        fila.Cells("Veredicto").Style.Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold)
    End Sub

    Private Sub PintarGridDiseno()
        _gridDiseno.Rows.Clear()
        If _muro.Resultados Is Nothing OrElse _muro.Resultados.Disenos Is Nothing Then Return
        AgregarFilaDiseno(_muro.Resultados.Disenos.Vastago)
        AgregarFilaDiseno(_muro.Resultados.Disenos.Talon)
        AgregarFilaDiseno(_muro.Resultados.Disenos.Pie)
        If _muro.Resultados.Disenos.Llave IsNot Nothing Then
            AgregarFilaDiseno(_muro.Resultados.Disenos.Llave)
        End If
    End Sub

    Private Sub AgregarFilaDiseno(d As DisenoElementoMuro)
        If d Is Nothing Then Return
        Dim veredicto = If(d.CumpleTodo, "✓ Cumple", "✗ No cumple")
        Dim idx = _gridDiseno.Rows.Add(d.Nombre, d.BarraPrincipal, d.SeparacionCm.ToString("0.0"),
                                        d.AsProv.ToString("0"), d.AsReq.ToString("0"),
                                        FormatearCD(d.FactorFlexion),
                                        d.Vu.ToString("0.0"), d.PhiVc.ToString("0.0"),
                                        FormatearCD(d.FactorCortante),
                                        veredicto)
        Dim fila = _gridDiseno.Rows(idx)
        fila.Tag = d
        For Each col As DataGridViewCell In fila.Cells
            col.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        fila.Cells("Elemento").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
        fila.Cells("Veredicto").Style.BackColor = If(d.CumpleTodo, Color.FromArgb(220, 237, 220), Color.FromArgb(248, 215, 215))
        fila.Cells("Veredicto").Style.ForeColor = If(d.CumpleTodo, Color.FromArgb(27, 94, 32), Color.FromArgb(176, 0, 32))
        fila.Cells("Veredicto").Style.Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold)
    End Sub

    Private Sub GridDiseno_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return
        Dim fila = _gridDiseno.Rows(e.RowIndex)
        Dim d = TryCast(fila.Tag, DisenoElementoMuro)
        If d Is Nothing Then Return
        Try
            Dim nuevaBarra = CStr(fila.Cells("Barra").Value)
            Dim nuevaSep As Double = 0
            Double.TryParse(CStr(fila.Cells("Sep").Value), nuevaSep)
            If Not String.IsNullOrEmpty(nuevaBarra) Then d.BarraPrincipal = nuevaBarra
            If nuevaSep > 0 Then d.SeparacionCm = nuevaSep
            MuroContencionService.RecalcularAsProv(d)
            ' Refrescar las celdas de la fila con el nuevo AsProv y C/D
            fila.Cells("AsProv").Value = d.AsProv.ToString("0")
            fila.Cells("CDFlex").Value = FormatearCD(d.FactorFlexion)
            Dim vd = If(d.CumpleTodo, "✓ Cumple", "✗ No cumple")
            fila.Cells("Veredicto").Value = vd
            fila.Cells("Veredicto").Style.BackColor = If(d.CumpleTodo, Color.FromArgb(220, 237, 220), Color.FromArgb(248, 215, 215))
            fila.Cells("Veredicto").Style.ForeColor = If(d.CumpleTodo, Color.FromArgb(27, 94, 32), Color.FromArgb(176, 0, 32))
            PintarVeredictoGlobal()
            _dirty = True
            ActualizarTitulo()
        Catch ex As Exception
            Logger.Error(ex, "Form_12_MuroContencion.GridDiseno_CellEndEdit", d?.Nombre)
        End Try
    End Sub

    Private Sub PintarVeredictoGlobal()
        If _muro.Resultados Is Nothing Then
            _lblVeredicto.Text = ""
            _lblVeredicto.BackColor = Color.Transparent
            Return
        End If
        Dim est = _muro.Resultados.Estabilidad
        Dim dis = _muro.Resultados.Disenos
        Dim cumpleEst = est.CasoEstatico IsNot Nothing AndAlso est.CasoEstatico.CumpleTodo
        If est.CasoSismico IsNot Nothing Then cumpleEst = cumpleEst AndAlso est.CasoSismico.CumpleTodo
        Dim cumpleDis = dis.Vastago.CumpleTodo AndAlso dis.Talon.CumpleTodo AndAlso dis.Pie.CumpleTodo
        If dis.Llave IsNot Nothing Then cumpleDis = cumpleDis AndAlso dis.Llave.CumpleTodo

        If cumpleEst AndAlso cumpleDis Then
            _lblVeredicto.Text = "✓  MURO CONFORME"
            _lblVeredicto.BackColor = VerdeARCO
        Else
            _lblVeredicto.Text = "✗  REQUIERE REVISIÓN"
            _lblVeredicto.BackColor = RojoARCO
        End If
    End Sub

    Private Sub LimpiarGrids()
        _gridEstabilidad.Rows.Clear()
        _gridDiseno.Rows.Clear()
    End Sub

    Private Shared Function FormatearCD(v As Double) As String
        If v <= 0 Then Return "—"
        If Double.IsInfinity(v) Then Return "∞"
        Return v.ToString("0.00")
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Dibujo del muro — estilo plano estructural
    '
    '   Capas (bottom-up):
    '     1. Fondo blanco con marco sutil.
    '     2. Hatching del suelo (relleno sobre talón + suelo frontal).
    '     3. Línea de nivel de terreno (NT) con etiqueta.
    '     4. Zapata, vástago, llave — rellenos con contorno fuerte.
    '     5. Cotas acotadas con flechas y labels (H, B, h1, h2, e1, e2,
    '        puntera, talón, D_f, llave).
    ' ═══════════════════════════════════════════════════════════════════════════

    ' Paleta del dibujo (distintos tonos del tierra/concreto)
    Private Shared ReadOnly ColorConcreto As Color = Color.FromArgb(205, 205, 212)
    Private Shared ReadOnly ColorConcretoBorde As Color = Color.FromArgb(55, 55, 65)
    Private Shared ReadOnly ColorTierra As Color = Color.FromArgb(185, 155, 95)
    Private Shared ReadOnly ColorTierraFondo As Color = Color.FromArgb(245, 232, 196)
    Private Shared ReadOnly ColorCota As Color = Color.FromArgb(50, 50, 60)
    Private Shared ReadOnly ColorNT As Color = Color.FromArgb(130, 100, 50)

    ' Función delegada para transformar coord mundo → pantalla (se construye en Paint).
    Private _w2s As Func(Of Double, Double, PointF)

    Private Sub Dibujo_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(Color.White)

        VolcarControlesAMuro()
        Dim geo = _muro.Geometria

        If geo.B <= 0 OrElse geo.H <= 0 Then
            Using fMsg As New Font("Segoe UI", 11, FontStyle.Italic),
                  bMsg As New SolidBrush(Color.Gray)
                Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                g.DrawString("Defina H y B en la pestaña Geometría.", fMsg, bMsg,
                             New RectangleF(0, 0, _dibujo.Width, _dibujo.Height), sf)
            End Using
            Return
        End If

        ' --- Marco sutil del dibujo --------------------------------------
        Using penMarco As New Pen(Color.FromArgb(220, 225, 232), 1)
            g.DrawRectangle(penMarco, 0, 0, _dibujo.Width - 1, _dibujo.Height - 1)
        End Using

        ' --- Fit view + zoom del usuario ---------------------------------
        '     bbox se amplía para dejar lugar a las cotas (margen generoso).
        '     esc_fit = tamaño que cabe en el panel; se multiplica por _zoomUser
        '     y se suma el _panUserX/Y para que la vista responda a la rueda
        '     del mouse y al drag.
        Dim bbox = BoundingBoxMuro(geo, _muro.Opciones.IncluirLlave)
        Dim margenPx As Integer = 90   ' espacio para cotas y labels
        Dim escX = (_dibujo.Width - 2 * margenPx) / bbox.Width
        Dim escY = (_dibujo.Height - 2 * margenPx) / bbox.Height
        Dim escFit = Math.Min(escX, escY)
        If escFit <= 0 Then Return
        Dim esc = escFit * _zoomUser
        Dim cx = _dibujo.Width / 2.0
        Dim cy = _dibujo.Height / 2.0
        Dim bbcx = bbox.Left + bbox.Width / 2.0
        Dim bbcy = bbox.Top + bbox.Height / 2.0
        Dim panX = _panUserX, panY = _panUserY
        _w2s = Function(wx As Double, wy As Double) New PointF(
            CSng(cx + (wx - bbcx) * esc + panX),
            CSng(cy - (wy - bbcy) * esc + panY))

        ' --- Posiciones clave del muro ----------------------------------
        Dim xCaraFrontBase = geo.b1
        Dim xCaraTras = geo.b1 + geo.e1
        Dim xCaraFrontCorona = geo.b1 + (geo.e1 - geo.e2)
        Dim talonLargo = geo.B - geo.b1 - geo.e1
        Dim ntY = _muro.Suelo.Df

        ' --- Hatching del relleno (sobre el talón) -----------------------
        If talonLargo > 0 Then
            Using hb As New HatchBrush(HatchStyle.ForwardDiagonal, ColorTierra, ColorTierraFondo)
                Dim ptos As PointF() = {
                    _w2s(xCaraTras, geo.h1),
                    _w2s(geo.B, geo.h1),
                    _w2s(geo.B, geo.H),
                    _w2s(xCaraTras, geo.H)
                }
                g.FillPolygon(hb, ptos)
            End Using
            ' Línea superior del relleno más marcada
            Using penTop As New Pen(ColorTierra, 1.5F)
                g.DrawLine(penTop, _w2s(xCaraTras, geo.H), _w2s(geo.B, geo.H))
            End Using
        End If

        ' --- Suelo frontal (bajo nivel de terreno, antes del muro) -------
        If _muro.Opciones.IncluirRellenoFrontal AndAlso ntY > geo.h1 Then
            Dim hFrente = ntY - geo.h1
            Using hb As New HatchBrush(HatchStyle.ForwardDiagonal, ColorTierra, ColorTierraFondo)
                Dim ptos As PointF() = {
                    _w2s(0, geo.h1),
                    _w2s(xCaraFrontBase, geo.h1),
                    _w2s(xCaraFrontBase, geo.h1 + hFrente),
                    _w2s(0, geo.h1 + hFrente)
                }
                g.FillPolygon(hb, ptos)
            End Using
            Using penTop As New Pen(ColorTierra, 1.5F)
                g.DrawLine(penTop, _w2s(0, ntY), _w2s(xCaraFrontBase, ntY))
            End Using
        End If

        ' --- Línea nivel de terreno (NT) ---------------------------------
        Using penNT As New Pen(ColorNT, 1.3F)
            penNT.DashPattern = New Single() {6, 3}
            Dim p1 = _w2s(-0.4, ntY)
            Dim p2 = _w2s(geo.B + 0.4, ntY)
            g.DrawLine(penNT, p1, p2)
            Using fNT As New Font("Segoe UI", 7.5F, FontStyle.Bold Or FontStyle.Italic),
                  bNT As New SolidBrush(ColorNT)
                g.DrawString($"NT  (Df = {ntY:0.00} m)", fNT, bNT, p2.X - 110, p2.Y - 14)
            End Using
        End Using

        ' --- Zapata, vástago, llave --------------------------------------
        Using bConcreto As New SolidBrush(ColorConcreto),
              pBorde As New Pen(ColorConcretoBorde, 1.6F)
            ' Zapata
            Dim rectZap As PointF() = {
                _w2s(0, 0), _w2s(geo.B, 0),
                _w2s(geo.B, geo.h1), _w2s(0, geo.h1)
            }
            g.FillPolygon(bConcreto, rectZap)
            g.DrawPolygon(pBorde, rectZap)

            ' Vástago — cara trasera SIEMPRE recta en x = xCaraTras
            Dim ptsVast As PointF() = {
                _w2s(xCaraFrontBase, geo.h1),
                _w2s(xCaraTras, geo.h1),
                _w2s(xCaraTras, geo.H),
                _w2s(xCaraFrontCorona, geo.H)
            }
            g.FillPolygon(bConcreto, ptsVast)
            g.DrawPolygon(pBorde, ptsVast)

            ' Llave de cortante
            If _muro.Opciones.IncluirLlave AndAlso geo.H_LL > 0 Then
                Dim xcL = (xCaraFrontBase + xCaraTras) / 2.0
                Dim ptsLl As PointF() = {
                    _w2s(xcL - geo.B_LL / 2.0, 0),
                    _w2s(xcL + geo.B_LL / 2.0, 0),
                    _w2s(xcL + geo.B_LL / 2.0, -geo.H_LL),
                    _w2s(xcL - geo.B_LL / 2.0, -geo.H_LL)
                }
                g.FillPolygon(bConcreto, ptsLl)
                g.DrawPolygon(pBorde, ptsLl)
            End If
        End Using

        ' --- Cotas acotadas ---------------------------------------------
        DibujarCotas(g, geo, xCaraFrontBase, xCaraTras, xCaraFrontCorona, talonLargo)
    End Sub

    ''' <summary>Dibuja la red de cotas estilo plano estructural.</summary>
    Private Sub DibujarCotas(g As Graphics, geo As GeometriaMuro,
                              xCaraFrontBase As Double, xCaraTras As Double,
                              xCaraFrontCorona As Double, talonLargo As Double)
        Dim fCota As New Font("Segoe UI", 8, FontStyle.Bold)
        Dim bCota As New SolidBrush(ColorCota)
        Dim penExt As New Pen(ColorCota, 0.75F)      ' línea de extensión fina
        Dim penCota As New Pen(ColorCota, 1.0F)      ' línea de cota principal

        Try
            ' H — vertical a la DERECHA del muro, cota de la altura total
            CotaVertical(g, geo.B + 0.3, 0, geo.H, 32, True,
                          $"H = {geo.H:0.00} m", fCota, bCota, penExt, penCota)

            ' h1 — vertical a la DERECHA, pegado a la zapata
            CotaVertical(g, geo.B + 0.1, 0, geo.h1, 10, True,
                          $"h₁ = {geo.h1:0.00}", fCota, bCota, penExt, penCota)

            ' h2 — vertical a la DERECHA, desde h1 a H
            Dim h2 = If(geo.h2 > 0, geo.h2, geo.H - geo.h1)
            CotaVertical(g, geo.B + 0.1, geo.h1, geo.H, 10, True,
                          $"h₂ = {h2:0.00}", fCota, bCota, penExt, penCota)

            ' B — horizontal ABAJO de todo
            CotaHorizontal(g, 0, geo.B, If(_muro.Opciones.IncluirLlave AndAlso geo.H_LL > 0,
                                            -geo.H_LL - 0.15, -0.15),
                            32, True, $"B = {geo.B:0.00} m",
                            fCota, bCota, penExt, penCota)

            ' Puntera (b1), ancho vástago (e1), talón — horizontal ARRIBA de la zapata
            CotaHorizontal(g, 0, xCaraFrontBase, geo.h1, 20, False,
                            $"b₁ = {geo.b1:0.00}", fCota, bCota, penExt, penCota)
            CotaHorizontal(g, xCaraFrontBase, xCaraTras, geo.h1, 20, False,
                            $"e₁ = {geo.e1:0.00}", fCota, bCota, penExt, penCota)
            If talonLargo > 0 Then
                CotaHorizontal(g, xCaraTras, geo.B, geo.h1, 20, False,
                                $"talón = {talonLargo:0.00}", fCota, bCota, penExt, penCota)
            End If

            ' e2 — horizontal ARRIBA del vástago (solo si distinto de e1)
            If Math.Abs(geo.e1 - geo.e2) > 0.001 Then
                CotaHorizontal(g, xCaraFrontCorona, xCaraTras, geo.H, 15, False,
                                $"e₂ = {geo.e2:0.00}", fCota, bCota, penExt, penCota)
            Else
                ' Etiqueta corta de e2 en la corona cuando es igual (línea de referencia breve)
                Using fRef As New Font("Segoe UI", 7.5F, FontStyle.Italic)
                    Dim pTop = _w2s(xCaraTras, geo.H)
                    g.DrawString($"e₂ = {geo.e2:0.00}", fRef, bCota, pTop.X + 6, pTop.Y - 6)
                End Using
            End If

            ' Llave — cota B_LL y H_LL si está incluida
            If _muro.Opciones.IncluirLlave AndAlso geo.H_LL > 0 Then
                Dim xcL = (xCaraFrontBase + xCaraTras) / 2.0
                CotaHorizontal(g, xcL - geo.B_LL / 2.0, xcL + geo.B_LL / 2.0,
                                -geo.H_LL - 0.05, 10, True,
                                $"B_LL = {geo.B_LL:0.00}", fCota, bCota, penExt, penCota)
                CotaVertical(g, -0.2, -geo.H_LL, 0, 10, False,
                              $"H_LL = {geo.H_LL:0.00}", fCota, bCota, penExt, penCota)
            End If

        Finally
            fCota.Dispose()
            bCota.Dispose()
            penExt.Dispose()
            penCota.Dispose()
        End Try
    End Sub

    ' --- Helpers de cota ----------------------------------------------
    ''' <summary>Cota horizontal (A y B comparten Y). <paramref name="abajo"/> True
    ''' coloca la línea de cota a yMundo − offsetPx (en pantalla, hacia abajo); False
    ''' la coloca hacia arriba del punto de referencia.</summary>
    Private Sub CotaHorizontal(g As Graphics, xA As Double, xB As Double, yMundo As Double,
                                offsetPx As Integer, abajo As Boolean, texto As String,
                                fCota As Font, bCota As SolidBrush, penExt As Pen, penCota As Pen)
        If xB <= xA Then Return
        Dim pA = _w2s(xA, yMundo)
        Dim pB = _w2s(xB, yMundo)
        Dim dirY = If(abajo, 1, -1)
        Dim yLinea = pA.Y + offsetPx * dirY
        Dim pAcota As New PointF(pA.X, yLinea)
        Dim pBcota As New PointF(pB.X, yLinea)

        ' Líneas de extensión (desde el punto al nivel de la línea de cota)
        g.DrawLine(penExt, pA, New PointF(pA.X, yLinea + 3 * dirY))
        g.DrawLine(penExt, pB, New PointF(pB.X, yLinea + 3 * dirY))
        ' Línea de cota principal
        g.DrawLine(penCota, pAcota, pBcota)
        ' Flechas en los extremos (triángulos)
        DibujarFlechaH(g, pAcota, True, penCota, bCota)
        DibujarFlechaH(g, pBcota, False, penCota, bCota)
        ' Texto centrado por encima/debajo de la línea
        Dim centroX = (pAcota.X + pBcota.X) / 2
        Dim sz = g.MeasureString(texto, fCota)
        Dim yTexto = yLinea + If(abajo, 2, -(sz.Height + 2))
        g.DrawString(texto, fCota, bCota, centroX - sz.Width / 2, yTexto)
    End Sub

    ''' <summary>Cota vertical (A y B comparten X). <paramref name="derecha"/> True
    ''' coloca la línea de cota a xMundo + offsetPx hacia la derecha en pantalla.</summary>
    Private Sub CotaVertical(g As Graphics, xMundo As Double, yA As Double, yB As Double,
                              offsetPx As Integer, derecha As Boolean, texto As String,
                              fCota As Font, bCota As SolidBrush, penExt As Pen, penCota As Pen)
        If yB <= yA Then Return
        Dim pA = _w2s(xMundo, yA)
        Dim pB = _w2s(xMundo, yB)
        Dim dirX = If(derecha, 1, -1)
        Dim xLinea = pA.X + offsetPx * dirX
        Dim pAcota As New PointF(xLinea, pA.Y)
        Dim pBcota As New PointF(xLinea, pB.Y)

        g.DrawLine(penExt, pA, New PointF(xLinea + 3 * dirX, pA.Y))
        g.DrawLine(penExt, pB, New PointF(xLinea + 3 * dirX, pB.Y))
        g.DrawLine(penCota, pAcota, pBcota)
        DibujarFlechaV(g, pAcota, True, penCota, bCota)
        DibujarFlechaV(g, pBcota, False, penCota, bCota)
        ' Texto rotado 90° a lo largo de la línea
        Dim centroY = (pAcota.Y + pBcota.Y) / 2
        Dim sz = g.MeasureString(texto, fCota)
        Dim state = g.Save()
        g.TranslateTransform(xLinea + If(derecha, 4, -4), centroY)
        g.RotateTransform(-90)
        g.DrawString(texto, fCota, bCota, -sz.Width / 2, If(derecha, -sz.Height, 0))
        g.Restore(state)
    End Sub

    ''' <summary>Pequeña flecha triangular en la punta de una línea horizontal.</summary>
    Private Sub DibujarFlechaH(g As Graphics, pt As PointF, haciaDerecha As Boolean, pen As Pen, b As Brush)
        Dim dx = If(haciaDerecha, 6, -6)
        Dim tri As PointF() = {
            pt,
            New PointF(pt.X + dx, pt.Y - 2.5),
            New PointF(pt.X + dx, pt.Y + 2.5)
        }
        g.FillPolygon(b, tri)
    End Sub

    Private Sub DibujarFlechaV(g As Graphics, pt As PointF, haciaAbajo As Boolean, pen As Pen, b As Brush)
        Dim dy = If(haciaAbajo, 6, -6)
        Dim tri As PointF() = {
            pt,
            New PointF(pt.X - 2.5, pt.Y + dy),
            New PointF(pt.X + 2.5, pt.Y + dy)
        }
        g.FillPolygon(b, tri)
    End Sub

    Private Function BoundingBoxMuro(geo As GeometriaMuro, incluirLlave As Boolean) As RectangleF
        Dim xMin As Double = -0.7
        Dim xMax As Double = geo.B + 0.7
        Dim yMin As Double = If(incluirLlave AndAlso geo.H_LL > 0, -geo.H_LL - 0.4, -0.4)
        Dim yMax As Double = geo.H + 0.4
        Return New RectangleF(CSng(xMin), CSng(yMin), CSng(xMax - xMin), CSng(yMax - yMin))
    End Function

    ' ═══════════════════════════════════════════════════════════════════════════
    ' Zoom y pan del dibujo
    ' ═══════════════════════════════════════════════════════════════════════════

    Private Function CrearBotonFlotante(texto As String, tooltip As String) As Button
        Dim b As New Button With {
            .Text = texto,
            .Size = New Size(30, 30),
            .BackColor = Color.White,
            .ForeColor = GrisARCO,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .Cursor = Cursors.Hand,
            .TabStop = False   ' que la rueda del mouse no se pierda al focus
        }
        b.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
        b.FlatAppearance.BorderSize = 1
        Dim tt As New ToolTip()
        tt.SetToolTip(b, tooltip)
        Return b
    End Function

    Private Function CentroDibujo() As Point
        Return New Point(_dibujo.Width \ 2, _dibujo.Height \ 2)
    End Function

    ''' <summary>
    ''' Aplica un factor de zoom conservando la posición del punto bajo el cursor.
    ''' Fórmula estándar: panX_nuevo = (cursor − centro)·(1 − f) + panX·f.
    ''' </summary>
    Private Sub AplicarZoom(factor As Double, cursorPt As Point)
        Dim nuevoZoom = _zoomUser * factor
        If nuevoZoom < 0.2 OrElse nuevoZoom > 30 Then Return
        Dim cx = _dibujo.Width / 2.0
        Dim cy = _dibujo.Height / 2.0
        _panUserX = (cursorPt.X - cx) * (1 - factor) + _panUserX * factor
        _panUserY = (cursorPt.Y - cy) * (1 - factor) + _panUserY * factor
        _zoomUser = nuevoZoom
        ActualizarEtiquetaZoom()
        _dibujo.Invalidate()
    End Sub

    Private Sub AjustarVistaDibujo()
        _zoomUser = 1.0
        _panUserX = 0
        _panUserY = 0
        ActualizarEtiquetaZoom()
        _dibujo.Invalidate()
    End Sub

    Private Sub ActualizarEtiquetaZoom()
        If _lblZoomInfo IsNot Nothing Then
            _lblZoomInfo.Text = $"Zoom: {_zoomUser * 100:0}%"
        End If
    End Sub

    Private Sub Dibujo_Resize(sender As Object, e As EventArgs)
        ' Posicionar los botones flotantes en la esquina sup-der.
        If _btnZoomIn Is Nothing Then Return
        Dim margen As Integer = 10
        Dim gap As Integer = 4
        _btnFit.Location = New Point(_dibujo.Width - 30 - margen, margen)
        _btnZoomIn.Location = New Point(_dibujo.Width - 60 - margen - gap, margen)
        _btnZoomOut.Location = New Point(_dibujo.Width - 90 - margen - 2 * gap, margen)
        _lblZoomInfo.Location = New Point(_dibujo.Width - 90 - margen - 2 * gap, margen + 32)
        _dibujo.Invalidate()
    End Sub

    Private Sub Dibujo_MouseWheel(sender As Object, e As MouseEventArgs)
        Dim factor As Double = If(e.Delta > 0, 1.2, 1.0 / 1.2)
        AplicarZoom(factor, e.Location)
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
    ' Panel doble buffer (idéntico al PanelDobleBuffer de ARCO)
    ' ═══════════════════════════════════════════════════════════════════════════
    Private Class PanelDobleBuffer
        Inherits Panel
        Sub New()
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                        ControlStyles.UserPaint Or
                        ControlStyles.OptimizedDoubleBuffer, True)
        End Sub
    End Class

End Class
