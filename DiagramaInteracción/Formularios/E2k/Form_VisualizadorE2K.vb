Imports System.Drawing
Imports System.Windows.Forms
Imports System.Linq

' ════════════════════════════════════════════════════════════════════════════
'  Form_VisualizadorE2K — Visualizador de modelo ETABS (.e2k)
'  Tres vistas: Planta (XY), Alzado (XZ/YZ), Vista 3D
'  Construido programáticamente (sin Designer)
' ════════════════════════════════════════════════════════════════════════════

Public Class Form_VisualizadorE2K
    Inherits Form

    ' ─── Datos ───────────────────────────────────────────────────────────────
    Private ReadOnly _model As E2kModel
    Private ReadOnly _opt As E2kRenderer.RenderOptions

    ' ─── Controles principales ───────────────────────────────────────────────
    Private WithEvents _tabs As TabControl
    Private WithEvents _picPlanta As PictureBox
    Private WithEvents _picAlzado As PictureBox
    Private WithEvents _pic3D As PictureBox
    Private _tabPlanta As TabPage
    Private _tabAlzado As TabPage
    Private _tab3D As TabPage

    ' ─── Panel izquierdo ─────────────────────────────────────────────────────
    Private WithEvents _cmbStory As ComboBox
    Private WithEvents _cmbAlzDir As ComboBox
    Private WithEvents _chkColumnas As CheckBox
    Private WithEvents _chkVigas As CheckBox
    Private WithEvents _chkMuros As CheckBox
    Private WithEvents _chkLosas As CheckBox
    Private WithEvents _chkNervios As CheckBox
    Private WithEvents _chkGrillas As CheckBox
    Private WithEvents _chkLblFrame As CheckBox
    Private WithEvents _chkLblPier As CheckBox
    Private WithEvents _chkLblJoint As CheckBox
    Private WithEvents _rbAlambre As RadioButton
    Private WithEvents _rbExtruido As RadioButton
    Private WithEvents _btnReset As Button
    Private _lblInfo As Label
    Private _lblStatus As Label

    ' ─── Estado de interacción ratón ─────────────────────────────────────────
    Private _plDragging As Boolean
    Private _plLast As Point
    Private _alDragging As Boolean
    Private _alLast As Point
    Private _3dRotating As Boolean
    Private _3dPanning As Boolean
    Private _3dLast As Point
    Private _ready As Boolean = False
    Private _split As SplitContainer
    ' Cache de bitmaps para pan rápido
    Private _bmpPlantaFull As Bitmap
    Private _plFullPanX As Single = 0
    Private _plFullPanY As Single = 0
    Private _bmpAlzadoFull As Bitmap
    Private _alFullPanX As Single = 0
    Private _alFullPanY As Single = 0
    ' Timer de debounce para zoom y rotación 3D
    Private _renderTimer As Timer

    ' ════════════════════════════════════════════════════════════════════════
    '  Constructor
    ' ════════════════════════════════════════════════════════════════════════

    Public Sub New(model As E2kModel)
        _model = model
        _opt = New E2kRenderer.RenderOptions()
        _renderTimer = New Timer() With {.Interval = 80}
        AddHandler _renderTimer.Tick, AddressOf OnRenderTimerTick
        InitializeComponent()
        LoadModel()
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Construcción de la interfaz
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub InitializeComponent()
        Me.SuspendLayout()
        Me.Text = "Visualizador ETABS — " & IO.Path.GetFileName(_model.FilePath)
        Me.Size = New Size(1300, 850)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.FromArgb(240, 242, 248)

        ' ── SplitContainer principal ────────────────────────────────────────
        _split = New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .SplitterWidth = 5,
            .FixedPanel = FixedPanel.Panel1
        }
        Me.Controls.Add(_split)

        ' ── Panel izquierdo ─────────────────────────────────────────────────
        Dim leftPanel As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(245, 247, 252),
            .Padding = New Padding(6)
        }
        _split.Panel1.Controls.Add(leftPanel)
        ' SplitterDistance se asigna en OnLoad cuando el form ya tiene ancho real
        BuildLeftPanel(leftPanel)

        ' ── Pestañas ────────────────────────────────────────────────────────
        _tabs = New TabControl() With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
        }
        _split.Panel2.Controls.Add(_tabs)

        BuildTabPlanta()
        BuildTabAlzado()
        BuildTab3D()

        _tabs.TabPages.Add(_tabPlanta)
        _tabs.TabPages.Add(_tabAlzado)
        _tabs.TabPages.Add(_tab3D)

        ' ── Barra de estado ─────────────────────────────────────────────────
        _lblStatus = New Label() With {
            .Dock = DockStyle.Bottom,
            .Height = 22,
            .Font = New Font("Segoe UI", 8F),
            .ForeColor = Color.FromArgb(80, 80, 95),
            .BackColor = Color.FromArgb(230, 233, 240),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 0, 0),
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(_lblStatus)
        _lblStatus.BringToFront()
        _split.BringToFront()

        Me.ResumeLayout(False)
    End Sub

    Private Sub BuildLeftPanel(p As Panel)
        Dim y = 6

        ' Info modelo
        _lblInfo = New Label() With {
            .Location = New Point(4, y), .Size = New Size(200, 55),
            .Font = New Font("Segoe UI", 8F),
            .ForeColor = Color.FromArgb(60, 60, 80),
            .BackColor = Color.Transparent,
            .AutoEllipsis = True
        }
        p.Controls.Add(_lblInfo)
        y += 60

        ' GroupBox Piso
        Dim gbPiso = MakeGroupBox("Piso", 4, y, 208, 58)
        p.Controls.Add(gbPiso)
        _cmbStory = New ComboBox() With {
            .Location = New Point(8, 22), .Width = 188,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 9F)
        }
        gbPiso.Controls.Add(_cmbStory)
        y += 64

        ' GroupBox Elementos
        Dim gbElem = MakeGroupBox("Elementos", 4, y, 208, 134)
        p.Controls.Add(gbElem)
        _chkColumnas = AddCheck(gbElem, "Columnas / Frames", 8, 20, True)
        _chkVigas    = AddCheck(gbElem, "Vigas / Beams", 8, 42, True)
        _chkNervios  = AddCheck(gbElem, "Nervios", 8, 64, True)
        _chkMuros    = AddCheck(gbElem, "Muros / Piers", 8, 86, True)
        _chkLosas    = AddCheck(gbElem, "Losas / Floors", 8, 108, False)
        y += 140

        ' GroupBox Etiquetas
        Dim gbLbl = MakeGroupBox("Etiquetas", 4, y, 208, 90)
        p.Controls.Add(gbLbl)
        _chkLblFrame = AddCheck(gbLbl, "Etiq. Frame (C/B)", 8, 20, False)
        _chkLblPier  = AddCheck(gbLbl, "Etiq. Pier / Muro", 8, 42, False)
        _chkLblJoint = AddCheck(gbLbl, "Etiq. Joint", 8, 64, False)
        y += 96

        ' GroupBox Vista
        Dim gbVista = MakeGroupBox("Modo de vista", 4, y, 208, 90)
        p.Controls.Add(gbVista)
        _rbAlambre = New RadioButton() With {
            .Text = "Alambre (rápido)", .Location = New Point(8, 18),
            .AutoSize = True, .Checked = True, .Font = New Font("Segoe UI", 8.5F)
        }
        _rbExtruido = New RadioButton() With {
            .Text = "Extruido (3D sólido)", .Location = New Point(8, 40),
            .AutoSize = True, .Font = New Font("Segoe UI", 8.5F)
        }
        _chkGrillas = AddCheck(gbVista, "Mostrar grillas", 8, 62, True)
        gbVista.Controls.AddRange({_rbAlambre, _rbExtruido})
        y += 96

        ' Botón reset
        _btnReset = New Button() With {
            .Text = "Restablecer Vista", .Location = New Point(4, y),
            .Size = New Size(208, 30), .Font = New Font("Segoe UI", 9F),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(60, 100, 180),
            .ForeColor = Color.White
        }
        p.Controls.Add(_btnReset)
        y += 36

        ' Instrucciones
        Dim lblHint = New Label() With {
            .Location = New Point(4, y), .Size = New Size(208, 80),
            .Font = New Font("Segoe UI", 7.5F),
            .ForeColor = Color.FromArgb(100, 100, 120),
            .Text = "Planta/Alzado:" & vbCrLf &
                    "  Arrastrar = Pan" & vbCrLf &
                    "  Rueda = Zoom" & vbCrLf & vbCrLf &
                    "3D:" & vbCrLf &
                    "  Arrastrar izq = Rotar" & vbCrLf &
                    "  Arrastrar der = Pan" & vbCrLf &
                    "  Rueda = Zoom"
        }
        p.Controls.Add(lblHint)
    End Sub

    Private Sub BuildTabPlanta()
        _tabPlanta = New TabPage("Planta (XY)")
        _picPlanta = New PictureBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(248, 250, 253),
            .Cursor = Cursors.SizeAll
        }
        _tabPlanta.Controls.Add(_picPlanta)
    End Sub

    Private Sub BuildTabAlzado()
        _tabAlzado = New TabPage("Alzado")
        ' Barra de opciones
        Dim bar = New Panel() With {
            .Dock = DockStyle.Top, .Height = 34,
            .BackColor = Color.FromArgb(235, 238, 248),
            .Padding = New Padding(6, 4, 0, 0)
        }
        Dim lblDir = New Label() With {
            .Text = "Vista:", .Location = New Point(6, 8), .AutoSize = True,
            .Font = New Font("Segoe UI", 9F)
        }
        _cmbAlzDir = New ComboBox() With {
            .Location = New Point(50, 5), .Width = 190,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 9F)
        }
        _cmbAlzDir.Items.AddRange({"Corte X-Z  (Vista lateral)", "Corte Y-Z  (Vista frontal)"})
        _cmbAlzDir.SelectedIndex = 0
        bar.Controls.AddRange({lblDir, _cmbAlzDir})

        _picAlzado = New PictureBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(248, 250, 253),
            .Cursor = Cursors.SizeAll
        }
        _tabAlzado.Controls.Add(_picAlzado)
        _tabAlzado.Controls.Add(bar)
    End Sub

    Private Sub BuildTab3D()
        _tab3D = New TabPage("Vista 3D")
        _pic3D = New PictureBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(248, 250, 253),
            .Cursor = Cursors.SizeAll
        }
        _tab3D.Controls.Add(_pic3D)
    End Sub

    ' ─── Helpers de construcción de controles ────────────────────────────────

    Private Shared Function MakeGroupBox(title As String, x As Integer, y As Integer,
                                          w As Integer, h As Integer) As GroupBox
        Return New GroupBox() With {
            .Text = title, .Location = New Point(x, y), .Size = New Size(w, h),
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(50, 70, 130)
        }
    End Function

    Private Shared Function AddCheck(parent As Control, text As String,
                                      x As Integer, y As Integer, checked As Boolean) As CheckBox
        Dim cb = New CheckBox() With {
            .Text = text, .Location = New Point(x, y), .AutoSize = True,
            .Checked = checked, .Font = New Font("Segoe UI", 8.5F)
        }
        parent.Controls.Add(cb)
        Return cb
    End Function

    ' ════════════════════════════════════════════════════════════════════════
    '  Carga del modelo
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub LoadModel()
        ' Story combobox
        _cmbStory.Items.Add("— Todos los pisos —")
        For Each st In _model.Stories
            _cmbStory.Items.Add(st.Name)
        Next
        _cmbStory.SelectedIndex = 0

        ' Info label
        Dim fn = IO.Path.GetFileName(_model.FilePath)
        _lblInfo.Text = $"{fn}" & vbCrLf &
                        $"Pisos: {_model.StoryCount}  |  H: {_model.MaxElevation():F1} m" & vbCrLf &
                        $"Frames: {_model.FrameCount}  |  Áreas: {_model.AreaCount}" & vbCrLf &
                        $"Unidades: {_model.Units}"

        _ready = True
        UpdateStatus("Modelo cargado. Listo.")
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Renderizado
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub SyncOptions()
        If Not _ready Then Return  ' no quitarlo — también protege llamadas directas
        _opt.FilterStory = If(_cmbStory.SelectedIndex <= 0, "",
                              DirectCast(_cmbStory.SelectedItem, String))
        _opt.ShowColumnas = _chkColumnas.Checked
        _opt.ShowVigas    = _chkVigas.Checked
        _opt.ShowNervios  = _chkNervios.Checked
        _opt.ShowMuros    = _chkMuros.Checked
        _opt.ShowLosas    = _chkLosas.Checked
        _opt.ShowGrillas  = _chkGrillas.Checked
        _opt.ShowLblFrame = _chkLblFrame.Checked
        _opt.ShowLblPier  = _chkLblPier.Checked
        _opt.ShowLblJoint = _chkLblJoint.Checked
        _opt.ModoExtruido = _rbExtruido.Checked
        _opt.AlzDir = If(_cmbAlzDir.SelectedIndex = 0, "XZ", "YZ")
    End Sub

    Private Sub RefreshCurrentView()
        If Not _ready Then Return
        SyncOptions()
        Dim idx = _tabs.SelectedIndex
        If idx = 0 Then RefreshPlanta()
        If idx = 1 Then RefreshAlzado()
        If idx = 2 Then Refresh3D()
    End Sub

    Private Sub RefreshPlanta()
        If Not _ready Then Return
        SyncOptions()
        If _picPlanta.Width < 10 OrElse _picPlanta.Height < 10 Then Return
        Dim t = DateTime.Now
        Dim bmp = E2kRenderer.RenderPlanta(_model, _opt, _picPlanta.Width, _picPlanta.Height)
        _bmpPlantaFull?.Dispose()
        _bmpPlantaFull = bmp
        _plFullPanX = _opt.PanX
        _plFullPanY = _opt.PanY
        Dim old = _picPlanta.Image
        _picPlanta.Image = DirectCast(bmp.Clone(), Bitmap)
        old?.Dispose()
        UpdateStatus($"Planta en {(DateTime.Now - t).TotalMilliseconds:F0} ms  " &
                     $"| Zoom: {_opt.UserZoom * 100:F0}%  | Piso: {If(_opt.FilterStory = "", "Todos", _opt.FilterStory)}")
    End Sub

    Private Sub ShiftPanPlanta()
        If _bmpPlantaFull Is Nothing Then RefreshPlanta() : Return
        Dim dx = CInt(_opt.PanX - _plFullPanX)
        Dim dy = CInt(_opt.PanY - _plFullPanY)
        Dim shifted As New Bitmap(_picPlanta.Width, _picPlanta.Height)
        Using gr = Graphics.FromImage(shifted)
            gr.Clear(_picPlanta.BackColor)
            gr.DrawImageUnscaled(_bmpPlantaFull, dx, dy)
        End Using
        Dim old = _picPlanta.Image
        _picPlanta.Image = shifted
        If old IsNot Nothing AndAlso Not Object.ReferenceEquals(old, _bmpPlantaFull) Then old.Dispose()
    End Sub

    Private Sub RefreshAlzado()
        If Not _ready Then Return
        SyncOptions()
        If _picAlzado.Width < 10 OrElse _picAlzado.Height < 10 Then Return
        Dim t = DateTime.Now
        Dim bmp = E2kRenderer.RenderAlzado(_model, _opt, _picAlzado.Width, _picAlzado.Height)
        _bmpAlzadoFull?.Dispose()
        _bmpAlzadoFull = bmp
        _alFullPanX = _opt.PanX
        _alFullPanY = _opt.PanY
        Dim old = _picAlzado.Image
        _picAlzado.Image = DirectCast(bmp.Clone(), Bitmap)
        old?.Dispose()
        UpdateStatus($"Alzado {_opt.AlzDir} en {(DateTime.Now - t).TotalMilliseconds:F0} ms  " &
                     $"| Zoom: {_opt.UserZoom * 100:F0}%")
    End Sub

    Private Sub ShiftPanAlzado()
        If _bmpAlzadoFull Is Nothing Then RefreshAlzado() : Return
        Dim dx = CInt(_opt.PanX - _alFullPanX)
        Dim dy = CInt(_opt.PanY - _alFullPanY)
        Dim shifted As New Bitmap(_picAlzado.Width, _picAlzado.Height)
        Using gr = Graphics.FromImage(shifted)
            gr.Clear(_picAlzado.BackColor)
            gr.DrawImageUnscaled(_bmpAlzadoFull, dx, dy)
        End Using
        Dim old = _picAlzado.Image
        _picAlzado.Image = shifted
        If old IsNot Nothing AndAlso Not Object.ReferenceEquals(old, _bmpAlzadoFull) Then old.Dispose()
    End Sub

    Private Sub Refresh3D()
        If Not _ready Then Return
        SyncOptions()
        If _pic3D.Width < 10 OrElse _pic3D.Height < 10 Then Return
        Dim t = DateTime.Now
        Dim bmp = E2kRenderer.Render3D(_model, _opt, _pic3D.Width, _pic3D.Height)
        Dim old = _pic3D.Image
        _pic3D.Image = bmp
        old?.Dispose()
        UpdateStatus($"3D renderizado en {(DateTime.Now - t).TotalMilliseconds:F0} ms  " &
                     $"| Az: {_opt.Azimuth:F1}°  El: {_opt.Elevation:F1}°  Zoom: {_opt.Zoom3D * 100:F0}%")
    End Sub

    Private Sub UpdateStatus(msg As String)
        If _lblStatus IsNot Nothing Then _lblStatus.Text = "  " & msg
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Eventos de formulario
    ' ════════════════════════════════════════════════════════════════════════

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Try
            _split.SplitterDistance = 220
            _split.Panel1MinSize = 200
            _split.Panel2MinSize = 400
        Catch
        End Try
        Me.BeginInvoke(New Action(AddressOf RefreshCurrentView))
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        If Me.WindowState <> FormWindowState.Minimized Then
            RefreshCurrentView()
        End If
    End Sub

    ' ─── Controles del panel ─────────────────────────────────────────────────

    Private Sub _cmbStory_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles _cmbStory.SelectedIndexChanged
        RefreshCurrentView()
    End Sub

    Private Sub _cmbAlzDir_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles _cmbAlzDir.SelectedIndexChanged
        RefreshAlzado()
    End Sub

    Private Sub AnyFilter_Changed(sender As Object, e As EventArgs) _
        Handles _chkColumnas.CheckedChanged, _chkVigas.CheckedChanged,
                _chkNervios.CheckedChanged, _chkMuros.CheckedChanged,
                _chkLosas.CheckedChanged, _chkGrillas.CheckedChanged,
                _chkLblFrame.CheckedChanged, _chkLblPier.CheckedChanged,
                _chkLblJoint.CheckedChanged, _rbAlambre.CheckedChanged,
                _rbExtruido.CheckedChanged
        RefreshCurrentView()
    End Sub

    Private Sub _btnReset_Click(sender As Object, e As EventArgs) _
        Handles _btnReset.Click
        _opt.PanX = 0 : _opt.PanY = 0 : _opt.UserZoom = 1.0F
        _opt.Zoom3D = 1.0F : _opt.Pan3DX = 0 : _opt.Pan3DY = 0
        _opt.Azimuth = 215.0 : _opt.Elevation = 28.0
        RefreshCurrentView()
    End Sub

    Private Sub _tabs_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles _tabs.SelectedIndexChanged
        RefreshCurrentView()
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Interacción — Planta (XY)
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub _picPlanta_MouseDown(sender As Object, e As MouseEventArgs) _
        Handles _picPlanta.MouseDown
        If e.Button = MouseButtons.Left Then
            _plDragging = True
            _plLast = e.Location
        End If
    End Sub

    Private Sub _picPlanta_MouseMove(sender As Object, e As MouseEventArgs) _
        Handles _picPlanta.MouseMove
        If _plDragging Then
            _opt.PanX += e.X - _plLast.X
            _opt.PanY += e.Y - _plLast.Y
            _plLast = e.Location
            ShiftPanPlanta()
        End If
    End Sub

    Private Sub _picPlanta_MouseUp(sender As Object, e As MouseEventArgs) _
        Handles _picPlanta.MouseUp
        If _plDragging Then
            _plDragging = False
            RefreshPlanta()
        End If
    End Sub

    Private Sub _picPlanta_MouseWheel(sender As Object, e As MouseEventArgs) _
        Handles _picPlanta.MouseWheel
        Dim factor = If(e.Delta > 0, 1.18F, 0.847F)
        _opt.UserZoom = Math.Max(0.05F, Math.Min(50.0F, _opt.UserZoom * factor))
        _renderTimer.Stop() : _renderTimer.Start()
    End Sub

    Private Sub _picPlanta_Paint(sender As Object, e As PaintEventArgs) _
        Handles _picPlanta.Paint
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Interacción — Alzado
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub _picAlzado_MouseDown(sender As Object, e As MouseEventArgs) _
        Handles _picAlzado.MouseDown
        If e.Button = MouseButtons.Left Then
            _alDragging = True
            _alLast = e.Location
        End If
    End Sub

    Private Sub _picAlzado_MouseMove(sender As Object, e As MouseEventArgs) _
        Handles _picAlzado.MouseMove
        If _alDragging Then
            _opt.PanX += e.X - _alLast.X
            _opt.PanY += e.Y - _alLast.Y
            _alLast = e.Location
            ShiftPanAlzado()
        End If
    End Sub

    Private Sub _picAlzado_MouseUp(sender As Object, e As MouseEventArgs) _
        Handles _picAlzado.MouseUp
        If _alDragging Then
            _alDragging = False
            RefreshAlzado()
        End If
    End Sub

    Private Sub _picAlzado_MouseWheel(sender As Object, e As MouseEventArgs) _
        Handles _picAlzado.MouseWheel
        Dim factor = If(e.Delta > 0, 1.18F, 0.847F)
        _opt.UserZoom = Math.Max(0.05F, Math.Min(50.0F, _opt.UserZoom * factor))
        _renderTimer.Stop() : _renderTimer.Start()
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Interacción — Vista 3D
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub _pic3D_MouseDown(sender As Object, e As MouseEventArgs) _
        Handles _pic3D.MouseDown
        _3dLast = e.Location
        If e.Button = MouseButtons.Left Then
            _3dRotating = True
            _pic3D.Cursor = Cursors.Cross
        ElseIf e.Button = MouseButtons.Right Then
            _3dPanning = True
            _pic3D.Cursor = Cursors.SizeAll
        End If
    End Sub

    Private Sub _pic3D_MouseMove(sender As Object, e As MouseEventArgs) _
        Handles _pic3D.MouseMove
        Dim dx = e.X - _3dLast.X
        Dim dy = e.Y - _3dLast.Y
        If _3dRotating Then
            _opt.Azimuth -= dx * 0.45
            _opt.Elevation = Math.Max(-85, Math.Min(85, _opt.Elevation + dy * 0.35))
            _3dLast = e.Location
            _renderTimer.Stop() : _renderTimer.Start()
        ElseIf _3dPanning Then
            _opt.Pan3DX += dx
            _opt.Pan3DY += dy
            _3dLast = e.Location
            _renderTimer.Stop() : _renderTimer.Start()
        End If
    End Sub

    Private Sub _pic3D_MouseUp(sender As Object, e As MouseEventArgs) _
        Handles _pic3D.MouseUp
        _3dRotating = False
        _3dPanning = False
        _pic3D.Cursor = Cursors.SizeAll
    End Sub

    Private Sub _pic3D_MouseWheel(sender As Object, e As MouseEventArgs) _
        Handles _pic3D.MouseWheel
        Dim factor = If(e.Delta > 0, 1.18F, 0.847F)
        _opt.Zoom3D = Math.Max(0.05F, Math.Min(50.0F, _opt.Zoom3D * factor))
        _renderTimer.Stop() : _renderTimer.Start()
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    '  Limpieza
    ' ════════════════════════════════════════════════════════════════════════

    Private Sub OnRenderTimerTick(sender As Object, e As EventArgs)
        _renderTimer.Stop()
        RefreshCurrentView()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _renderTimer.Stop()
        _renderTimer.Dispose()
        _bmpPlantaFull?.Dispose()
        _bmpAlzadoFull?.Dispose()
        _picPlanta.Image?.Dispose()
        _picAlzado.Image?.Dispose()
        _pic3D.Image?.Dispose()
        MyBase.OnFormClosed(e)
    End Sub

End Class
