Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Formulario de selección de columnas detectadas desde ETABS (Frame y/o Pier).
''' Muestra los elementos en vista en planta GDI+ con zoom/pan.
''' El usuario selecciona cuáles importar; el resultado queda en SelFrame y SelPier.
''' </summary>
Public Class Form_02_SeleccionColumnas
    Inherits Form

    ' ─── Datos ──────────────────────────────────────────────────────────────────
    Private _candidatos As List(Of cCandidatoColumna)
    Private _jointsXY   As List(Of PointF)
    Private _framesXY   As List(Of Tuple(Of PointF, PointF))

    ' ─── Resultado ──────────────────────────────────────────────────────────────
    Public Property SelFrame As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Public Property SelPier  As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Public Property Confirmado As Boolean = False

    ' ─── Controles ──────────────────────────────────────────────────────────────
    Private Split             As New SplitContainer()
    Private PanelLeft         As New Panel()
    Private PanelRight        As New Panel()
    Private WithEvents PanelDibujo As New Panel()
    Private LabelTituloPlanta As New Label()
    Private LabelInfo         As New Label()
    Private LabelEstadisticas As New Label()
    Private WithEvents DGV    As New DataGridView()
    Private PanelBotones      As New Panel()
    Private WithEvents BtnSelTodo   As New Button()
    Private WithEvents BtnDeselTodo As New Button()
    Private WithEvents BtnConfirmar As New Button()
    Private WithEvents BtnCancelar  As New Button()
    Private PanelZoom         As New Panel()
    Private WithEvents BtnZoomIn    As New Button()
    Private WithEvents BtnZoomOut   As New Button()
    Private WithEvents BtnResetView As New Button()
    Private LabelZoomPct      As New Label()

    ' ─── Columnas DGV ───────────────────────────────────────────────────────────
    Private Const COL_SEL  As Integer = 0
    Private Const COL_TIPO As Integer = 1
    Private Const COL_LBL  As Integer = 2
    Private Const COL_PISO As Integer = 3
    Private Const COL_SEC  As Integer = 4
    Private Const COL_B    As Integer = 5
    Private Const COL_H    As Integer = 6
    Private Const COL_X    As Integer = 7
    Private Const COL_Y    As Integer = 8

    ' ─── Dibujo ─────────────────────────────────────────────────────────────────
    Private Const MARGEN_PCT As Double = 0.12
    Private Const MIN_PX     As Integer = 5
    Private Const MAX_PX     As Integer = 40

    ' ─── Zoom / pan ─────────────────────────────────────────────────────────────
    Private _zoom     As Double = 1.0
    Private _panX     As Double = 0.0
    Private _panY     As Double = 0.0
    Private _isDrag   As Boolean = False
    Private _dragStart As Point

    ' ─── Constructor ────────────────────────────────────────────────────────────
    Public Sub New(candidatos As List(Of cCandidatoColumna),
                   Optional jointsXY As List(Of PointF) = Nothing,
                   Optional framesXY As List(Of Tuple(Of PointF, PointF)) = Nothing)
        _candidatos = If(candidatos, New List(Of cCandidatoColumna)())
        _jointsXY   = If(jointsXY,  New List(Of PointF)())
        _framesXY   = If(framesXY,  New List(Of Tuple(Of PointF, PointF))())
        InitializeComponent()
    End Sub

    ' ─── Construcción de controles ──────────────────────────────────────────────
    Private Sub InitializeComponent()
        Me.SuspendLayout()
        Me.Text             = "Selección de Columnas — Vista en Planta"
        Me.Size             = New Size(1200, 750)
        Me.MinimumSize      = New Size(900, 550)
        Me.StartPosition    = FormStartPosition.CenterParent
        Me.FormBorderStyle  = FormBorderStyle.Sizable
        Me.WindowState      = FormWindowState.Maximized
        Me.BackColor        = Color.White

        ' SplitContainer — garantiza que el panel izquierdo siempre tenga espacio visible
        Split.Dock          = DockStyle.Fill
        Split.Orientation   = Orientation.Vertical
        Split.SplitterWidth = 5
        Split.SplitterDistance = 660       ' se recalcula en OnShown al 58% del ancho real
        Split.FixedPanel    = FixedPanel.None
        Split.BackColor     = Color.FromArgb(200, 210, 220)

        PanelLeft.Dock  = DockStyle.Fill
        PanelRight.Dock = DockStyle.Fill

        ' Título planta
        LabelTituloPlanta.Text      = "Vista en Planta  (Frame = azul  |  Pier = naranja)"
        LabelTituloPlanta.Dock      = DockStyle.Top
        LabelTituloPlanta.Height    = 24
        LabelTituloPlanta.Font      = New Font("Segoe UI", 9, FontStyle.Bold)
        LabelTituloPlanta.ForeColor = Color.FromArgb(45, 45, 45)
        LabelTituloPlanta.TextAlign = ContentAlignment.MiddleLeft
        LabelTituloPlanta.Padding   = New Padding(4, 0, 0, 0)
        LabelTituloPlanta.BackColor = Color.FromArgb(235, 240, 248)

        PanelDibujo.Dock        = DockStyle.Fill
        PanelDibujo.BackColor   = Color.FromArgb(245, 248, 252)
        PanelDibujo.BorderStyle = BorderStyle.None

        LabelInfo.Dock        = DockStyle.Bottom
        LabelInfo.Height      = 36
        LabelInfo.Font        = New Font("Segoe UI", 8.5F)
        LabelInfo.ForeColor   = Color.FromArgb(60, 60, 60)
        LabelInfo.BackColor   = Color.FromArgb(245, 245, 245)
        LabelInfo.TextAlign   = ContentAlignment.MiddleLeft
        LabelInfo.Padding     = New Padding(6, 0, 0, 0)
        LabelInfo.BorderStyle = BorderStyle.FixedSingle
        LabelInfo.Text        = "Seleccione una fila para resaltar el elemento en planta."

        ' Barra zoom
        PanelZoom.Dock      = DockStyle.Top
        PanelZoom.Height    = 30
        PanelZoom.BackColor = Color.FromArgb(220, 228, 242)
        ConfigurarBtnZoom(BtnZoomIn, "+")   : BtnZoomIn.Location   = New Point(4, 3)
        ConfigurarBtnZoom(BtnZoomOut, "−")  : BtnZoomOut.Location  = New Point(34, 3)
        ConfigurarBtnZoom(BtnResetView, "↺"): BtnResetView.Location = New Point(64, 3)
        LabelZoomPct.AutoSize  = True
        LabelZoomPct.Font      = New Font("Segoe UI", 8.5F)
        LabelZoomPct.ForeColor = Color.FromArgb(50, 70, 110)
        LabelZoomPct.Text      = "100%"
        LabelZoomPct.Location  = New Point(98, 8)
        PanelZoom.Controls.Add(BtnZoomIn)
        PanelZoom.Controls.Add(BtnZoomOut)
        PanelZoom.Controls.Add(BtnResetView)
        PanelZoom.Controls.Add(LabelZoomPct)

        ' Orden Dock (Fill primero, luego Bottom, luego Top de abajo a arriba)
        PanelLeft.Controls.Add(PanelDibujo)
        PanelLeft.Controls.Add(LabelInfo)
        PanelLeft.Controls.Add(PanelZoom)
        PanelLeft.Controls.Add(LabelTituloPlanta)

        ' Panel derecho
        LabelEstadisticas.Dock      = DockStyle.Top
        LabelEstadisticas.Height    = 24
        LabelEstadisticas.Font      = New Font("Segoe UI", 8.5F)
        LabelEstadisticas.ForeColor = Color.FromArgb(60, 60, 60)
        LabelEstadisticas.BackColor = Color.FromArgb(235, 240, 248)
        LabelEstadisticas.TextAlign = ContentAlignment.MiddleLeft
        LabelEstadisticas.Padding   = New Padding(4, 0, 0, 0)
        LabelEstadisticas.Text      = "Elementos detectados"

        ConfigurarDGV()
        DGV.Dock = DockStyle.Fill

        PanelBotones.Dock      = DockStyle.Bottom
        PanelBotones.Height    = 70
        PanelBotones.BackColor = Color.FromArgb(245, 245, 245)
        PanelBotones.Padding   = New Padding(8, 10, 8, 10)

        ConfigurarBtn(BtnSelTodo,   "Sel. Todo",     Color.FromArgb(108, 117, 125))
        ConfigurarBtn(BtnDeselTodo, "Desel. Todo",   Color.FromArgb(108, 117, 125))
        ConfigurarBtn(BtnConfirmar, "Importar Sel.", Color.FromArgb(40, 167, 69))
        ConfigurarBtn(BtnCancelar,  "Cancelar",      Color.FromArgb(220, 53, 69))

        PanelBotones.Controls.Add(BtnSelTodo)
        PanelBotones.Controls.Add(BtnDeselTodo)
        PanelBotones.Controls.Add(BtnConfirmar)
        PanelBotones.Controls.Add(BtnCancelar)

        PanelRight.Controls.Add(DGV)
        PanelRight.Controls.Add(PanelBotones)
        PanelRight.Controls.Add(LabelEstadisticas)

        Split.Panel1.Controls.Add(PanelLeft)
        Split.Panel2.Controls.Add(PanelRight)
        Me.Controls.Add(Split)

        Me.ResumeLayout(False)
        AddHandler PanelBotones.Resize, AddressOf PanelBotones_Resize
    End Sub

    Private Sub ConfigurarDGV()
        DGV.AutoGenerateColumns    = False
        DGV.AllowUserToAddRows     = False
        DGV.AllowUserToDeleteRows  = False
        DGV.ReadOnly               = False
        DGV.SelectionMode          = DataGridViewSelectionMode.FullRowSelect
        DGV.MultiSelect            = False
        DGV.RowHeadersVisible      = False
        DGV.BorderStyle            = BorderStyle.None
        DGV.BackgroundColor        = Color.White
        DGV.GridColor              = Color.FromArgb(220, 220, 220)
        DGV.ColumnHeadersHeight    = 28
        DGV.RowTemplate.Height     = 22
        DGV.Font                   = New Font("Segoe UI", 8.5F)
        With DGV.ColumnHeadersDefaultCellStyle
            .BackColor  = Color.FromArgb(52, 73, 94)
            .ForeColor  = Color.White
            .Font       = New Font("Segoe UI", 8.5F, FontStyle.Bold)
            .Alignment  = DataGridViewContentAlignment.MiddleCenter
        End With
        DGV.EnableHeadersVisualStyles = False

        AgregarCol(DGV, New DataGridViewCheckBoxColumn(), "Sel.",     44,  False, False)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "Tipo",     55,  True,  True)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "Label",    80,  True,  True)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "Piso",     70,  True,  False)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "Sección",  120, True,  False)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "B [m]",    58,  True,  True)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "H [m]",    58,  True,  True)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "X [m]",    62,  True,  True)
        AgregarCol(DGV, New DataGridViewTextBoxColumn(),  "Y [m]",    62,  True,  False)
        DGV.Columns(COL_Y).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    End Sub

    Private Shared Sub AgregarCol(dgv As DataGridView, col As DataGridViewColumn,
                                  titulo As String, ancho As Integer,
                                  soloLectura As Boolean, centrado As Boolean)
        col.HeaderText  = titulo
        col.Width       = ancho
        col.ReadOnly    = soloLectura
        col.DefaultCellStyle.Alignment = If(centrado,
            DataGridViewContentAlignment.MiddleCenter,
            DataGridViewContentAlignment.MiddleLeft)
        If col.GetType() IsNot GetType(DataGridViewCheckBoxColumn) Then
            CType(col, DataGridViewTextBoxColumn).DefaultCellStyle.Format = ""
        End If
        dgv.Columns.Add(col)
    End Sub

    Private Shared Sub ConfigurarBtn(btn As Button, texto As String, color As Color)
        btn.Text          = texto
        btn.BackColor     = color
        btn.ForeColor     = Color.White
        btn.FlatStyle     = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.Font          = New Font("Segoe UI", 9, FontStyle.Bold)
        btn.Height        = 34
        btn.Width         = 110
        btn.Cursor        = Cursors.Hand
    End Sub

    Private Shared Sub ConfigurarBtnZoom(btn As Button, texto As String)
        btn.Text          = texto
        btn.BackColor     = Color.FromArgb(65, 90, 130)
        btn.ForeColor     = Color.White
        btn.FlatStyle     = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.Font          = New Font("Segoe UI", 11, FontStyle.Bold)
        btn.Height        = 24
        btn.Width         = 26
        btn.Cursor        = Cursors.Hand
    End Sub

    ' ─── Carga ──────────────────────────────────────────────────────────────────
    Private Sub Form_02_SeleccionColumnas_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CargarDGV()
        ActualizarEstadisticas()
        PosicionarBotones()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ' Ajustar divisor al 58% del ancho real (ya maximizado) con un mínimo razonable
        Dim w58 As Integer = CInt(Me.ClientSize.Width * 0.58)
        If w58 > 100 Then Split.SplitterDistance = w58
        DibujarPlanta()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ' Mantener proporcion ~58% al redimensionar
        If Split Is Nothing Then Return
        Dim w58 As Integer = CInt(Me.ClientSize.Width * 0.58)
        If w58 > 100 AndAlso w58 < Me.ClientSize.Width - 200 Then
            Split.SplitterDistance = w58
        End If
    End Sub

    Private Sub CargarDGV()
        DGV.Rows.Clear()
        For Each c As cCandidatoColumna In _candidatos
            Dim idx As Integer = DGV.Rows.Add()
            Dim row As DataGridViewRow = DGV.Rows(idx)
            row.Cells(COL_SEL).Value  = c.Seleccionado
            row.Cells(COL_TIPO).Value = c.Tipo
            row.Cells(COL_LBL).Value  = c.Label
            row.Cells(COL_PISO).Value = c.Story
            row.Cells(COL_SEC).Value  = c.Seccion
            row.Cells(COL_B).Value    = If(c.B > 0, c.B.ToString("F3"), "—")
            row.Cells(COL_H).Value    = If(c.H > 0, c.H.ToString("F3"), "—")
            row.Cells(COL_X).Value    = If(c.X <> 0 OrElse c.Y <> 0, c.X.ToString("F2"), "—")
            row.Cells(COL_Y).Value    = If(c.X <> 0 OrElse c.Y <> 0, c.Y.ToString("F2"), "—")
            If c.Tipo = "Frame" Then
                row.Cells(COL_TIPO).Style.ForeColor = Color.FromArgb(0, 70, 160)
                row.Cells(COL_TIPO).Style.Font      = New Font("Segoe UI", 8.5F, FontStyle.Bold)
            Else
                row.Cells(COL_TIPO).Style.ForeColor = Color.FromArgb(180, 80, 0)
                row.Cells(COL_TIPO).Style.Font      = New Font("Segoe UI", 8.5F, FontStyle.Bold)
            End If
        Next
    End Sub

    Private Sub ActualizarEstadisticas()
        Dim nFr  = _candidatos.Where(Function(c) c.Tipo = "Frame").Count()
        Dim nPi  = _candidatos.Where(Function(c) c.Tipo = "Pier").Count()
        Dim nSel = _candidatos.Where(Function(c) c.Seleccionado).Count()
        LabelEstadisticas.Text =
            $"Total: {_candidatos.Count}  |  Frame: {nFr}  |  Pier: {nPi}  |  Seleccionados: {nSel}"
    End Sub

    Private Sub PosicionarBotones()
        Dim pad As Integer = 6
        Dim y0  As Integer = (PanelBotones.Height - 34) \ 2
        Dim bw  As Integer = Math.Max(80, (PanelBotones.Width - 2 * pad - 3 * pad) \ 4)
        BtnSelTodo.Width = bw : BtnDeselTodo.Width = bw
        BtnConfirmar.Width = bw : BtnCancelar.Width = bw
        BtnSelTodo.Location   = New Point(pad, y0)
        BtnDeselTodo.Location = New Point(pad + bw + pad, y0)
        BtnConfirmar.Location = New Point(pad + 2 * (bw + pad), y0)
        BtnCancelar.Location  = New Point(pad + 3 * (bw + pad), y0)
    End Sub

    Private Sub PanelBotones_Resize(sender As Object, e As EventArgs)
        PosicionarBotones()
    End Sub

    ' ─── Dibujo en planta ───────────────────────────────────────────────────────
    Private Sub DibujarPlanta()
        PanelDibujo.Invalidate()   ' el Paint usa siempre el tamaño real del panel
    End Sub

    Private Sub PanelDibujo_Paint(sender As Object, e As PaintEventArgs) Handles PanelDibujo.Paint
        Dim g As Graphics = e.Graphics
        Dim w As Integer  = PanelDibujo.ClientSize.Width
        Dim h As Integer  = PanelDibujo.ClientSize.Height
        g.SmoothingMode     = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
        If _candidatos Is Nothing OrElse _candidatos.Count = 0 Then
            g.DrawString("No hay elementos detectados.", New Font("Segoe UI", 10),
                         Brushes.Gray, 20, 20)
        Else
            DibujarElementos(g, w, h)
        End If
    End Sub

    Private Sub DibujarElementos(g As Graphics, w As Integer, h As Integer)
        ' Rango total: candidatos + joints backdrop
        Dim allX As New List(Of Double)
        Dim allY As New List(Of Double)
        For Each c As cCandidatoColumna In _candidatos
            allX.Add(c.X) : allY.Add(c.Y)
        Next
        For Each pt As PointF In _jointsXY
            allX.Add(pt.X) : allY.Add(pt.Y)
        Next
        If allX.Count = 0 Then Return

        Dim xMin = allX.Min() : Dim xMax = allX.Max()
        Dim yMin = allY.Min() : Dim yMax = allY.Max()
        Dim dx = If(xMax - xMin < 0.001, 1.0, xMax - xMin)
        Dim dy = If(yMax - yMin < 0.001, 1.0, yMax - yMin)

        Dim mX = CInt(w * MARGEN_PCT)
        Dim mY = CInt(h * MARGEN_PCT)
        Dim drawW = w - 2 * mX
        Dim drawH = h - 2 * mY - 20

        Dim esc As Double = Math.Min(drawW / dx, drawH / dy) * _zoom
        Dim cxM = (xMin + xMax) / 2.0 : Dim cyM = (yMin + yMax) / 2.0
        Dim cxP = mX + drawW / 2.0    : Dim cyP = mY + drawH / 2.0

        Const CLAMP As Double = 100000.0
        Dim Tx As Func(Of Double, Single) = Function(x) CSng(Math.Max(-CLAMP, Math.Min(CLAMP, cxP + (x - cxM) * esc + _panX)))
        Dim Ty As Func(Of Double, Single) = Function(y) CSng(Math.Max(-CLAMP, Math.Min(CLAMP, cyP - (y - cyM) * esc + _panY)))

        Dim idxSel As Integer = If(DGV.CurrentRow IsNot Nothing, DGV.CurrentRow.Index, -1)

        ' 1. Backdrop: líneas de frames
        If _framesXY IsNot Nothing Then
            Using pen As New Pen(Color.FromArgb(80, 160, 160, 160), 1)
                For Each f As Tuple(Of PointF, PointF) In _framesXY
                    g.DrawLine(pen, Tx(f.Item1.X), Ty(f.Item1.Y), Tx(f.Item2.X), Ty(f.Item2.Y))
                Next
            End Using
        End If

        ' 2. Backdrop: joints
        If _jointsXY IsNot Nothing Then
            Using br As New SolidBrush(Color.FromArgb(100, 150, 150, 150))
                For Each pt As PointF In _jointsXY
                    g.FillEllipse(br, Tx(pt.X) - 2, Ty(pt.Y) - 2, 4, 4)
                Next
            End Using
        End If

        ' 3. Candidatos
        Dim fontLbl As New Font("Segoe UI", 7)
        Dim brLbl   As New SolidBrush(Color.FromArgb(40, 40, 40))

        For i = 0 To _candidatos.Count - 1
            Dim c   As cCandidatoColumna = _candidatos(i)
            Dim px  As Single = Tx(c.X)
            Dim py  As Single = Ty(c.Y)
            Dim sel As Boolean = (i = idxSel)

            ' Tamaño proporcional a la sección, con límites min/max
            Dim bPx As Integer = Math.Max(MIN_PX, Math.Min(MAX_PX, CInt(c.B * esc)))
            Dim hPx As Integer = Math.Max(MIN_PX, Math.Min(MAX_PX, CInt(c.H * esc)))
            If bPx = 0 AndAlso hPx = 0 Then bPx = 8 : hPx = 8

            Dim colorRell As Color
            Dim colorBord As Color
            If Not c.Seleccionado Then
                colorRell = Color.FromArgb(160, 180, 180, 180)
                colorBord = Color.FromArgb(160, 130, 130, 130)
            ElseIf c.Tipo = "Frame" Then
                colorRell = Color.FromArgb(200, 30, 100, 200)
                colorBord = Color.FromArgb(0, 55, 150)
            Else
                colorRell = Color.FromArgb(200, 220, 110, 10)
                colorBord = Color.FromArgb(180, 80, 0)
            End If

            ' Halo de selección
            If sel Then
                Using penHalo As New Pen(Color.Red, 2.5F)
                    g.DrawRectangle(penHalo,
                                    px - bPx \ 2 - 4, py - hPx \ 2 - 4,
                                    bPx + 8, hPx + 8)
                End Using
            End If

            Using br As New SolidBrush(colorRell)
            Using pen As New Pen(colorBord, If(sel, 2.0F, 1.0F))
                g.FillRectangle(br,  px - bPx \ 2, py - hPx \ 2, bPx, hPx)
                g.DrawRectangle(pen, px - bPx \ 2, py - hPx \ 2, bPx, hPx)
            End Using
            End Using

            g.DrawString(c.Label, fontLbl, brLbl, px + bPx \ 2 + 2, py - 8)
        Next

        fontLbl.Dispose()
        brLbl.Dispose()

        DibujarBarraEscala(g, w, h, esc)
    End Sub

    Private Sub DibujarBarraEscala(g As Graphics, w As Integer, h As Integer, esc As Double)
        If esc <= 0 Then Return
        Dim target = w * 0.20
        Dim escalaM = target / esc
        Dim mag = Math.Pow(10, Math.Floor(Math.Log10(escalaM)))
        Dim nice = escalaM / mag
        If nice < 2 Then
            nice = 1
        ElseIf nice < 5 Then
            nice = 2
        Else
            nice = 5
        End If
        Dim barM = nice * mag
        Dim barPx = CInt(barM * esc)
        If barPx < 10 Then Return

        Dim x1 = CInt(w * MARGEN_PCT)
        Dim y1 = h - 14
        Dim x2 = x1 + barPx
        Using pen As New Pen(Color.FromArgb(80, 80, 80), 2)
            g.DrawLine(pen, x1, y1, x2, y1)
            g.DrawLine(pen, x1, y1 - 4, x1, y1 + 4)
            g.DrawLine(pen, x2, y1 - 4, x2, y1 + 4)
        End Using
        Using f As New Font("Segoe UI", 7.5F)
        Using br As New SolidBrush(Color.FromArgb(70, 70, 70))
            Dim sz = g.MeasureString($"{barM:G4} m", f)
            g.DrawString($"{barM:G4} m", f, br, x1 + (barPx - sz.Width) / 2, y1 - 14)
        End Using
        End Using
    End Sub

    ' ─── Eventos DGV ────────────────────────────────────────────────────────────
    Private Sub DGV_SelectionChanged(sender As Object, e As EventArgs) Handles DGV.SelectionChanged
        ActualizarInfoPanel()
        DibujarPlanta()
    End Sub

    Private Sub DGV_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DGV.CellValueChanged
        If e.RowIndex < 0 OrElse e.RowIndex >= _candidatos.Count Then Return
        If e.ColumnIndex = COL_SEL Then
            Dim v = DGV.Rows(e.RowIndex).Cells(COL_SEL).Value
            _candidatos(e.RowIndex).Seleccionado = If(v IsNot Nothing AndAlso CBool(v), True, False)
            ActualizarEstadisticas()
            DibujarPlanta()
        End If
    End Sub

    Private Sub DGV_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles DGV.CurrentCellDirtyStateChanged
        If DGV.IsCurrentCellDirty AndAlso DGV.CurrentCell IsNot Nothing AndAlso
           DGV.CurrentCell.ColumnIndex = COL_SEL Then
            DGV.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub ActualizarInfoPanel()
        Try
            If DGV.CurrentRow Is Nothing Then Return
            Dim idx = DGV.CurrentRow.Index
            If idx < 0 OrElse idx >= _candidatos.Count Then Return
            Dim c = _candidatos(idx)
            LabelInfo.Text =
                $"Tipo: {c.Tipo}  |  Label: {c.Label}  |  Sección: {c.Seccion}" &
                $"  |  B: {c.B:F3} m  H: {c.H:F3} m  |  X: {c.X:F2} m  Y: {c.Y:F2} m" &
                If(c.Story <> "", $"  |  Piso: {c.Story}", "")
        Catch
        End Try
    End Sub

    ' ─── Resize del panel de dibujo ─────────────────────────────────────────────
    Private Sub PanelDibujo_Resize(sender As Object, e As EventArgs) Handles PanelDibujo.Resize
        PanelDibujo.Invalidate()
    End Sub

    ' ─── Zoom ───────────────────────────────────────────────────────────────────
    Private Sub BtnZoomIn_Click(sender As Object, e As EventArgs) Handles BtnZoomIn.Click
        _zoom = Math.Min(_zoom * 1.25, 20.0) : ActualizarZoomLabel() : PanelDibujo.Invalidate()
    End Sub
    Private Sub BtnZoomOut_Click(sender As Object, e As EventArgs) Handles BtnZoomOut.Click
        _zoom = Math.Max(_zoom / 1.25, 0.05) : ActualizarZoomLabel() : PanelDibujo.Invalidate()
    End Sub
    Private Sub BtnResetView_Click(sender As Object, e As EventArgs) Handles BtnResetView.Click
        _zoom = 1.0 : _panX = 0 : _panY = 0 : ActualizarZoomLabel() : PanelDibujo.Invalidate()
    End Sub
    Private Sub ActualizarZoomLabel()
        LabelZoomPct.Text = $"{CInt(_zoom * 100)}%"
    End Sub

    ' ─── Pan con ratón ──────────────────────────────────────────────────────────
    Private Sub PanelDibujo_MouseDown(sender As Object, e As MouseEventArgs) Handles PanelDibujo.MouseDown
        If e.Button = MouseButtons.Left Then
            _isDrag = True : _dragStart = e.Location : PanelDibujo.Cursor = Cursors.SizeAll
        End If
    End Sub
    Private Sub PanelDibujo_MouseMove(sender As Object, e As MouseEventArgs) Handles PanelDibujo.MouseMove
        If _isDrag Then
            _panX += e.X - _dragStart.X : _panY += e.Y - _dragStart.Y
            _dragStart = e.Location : PanelDibujo.Invalidate()
        End If
    End Sub
    Private Sub PanelDibujo_MouseUp(sender As Object, e As MouseEventArgs) Handles PanelDibujo.MouseUp
        _isDrag = False : PanelDibujo.Cursor = Cursors.Default
    End Sub

    ' ─── Zoom con rueda ─────────────────────────────────────────────────────────
    Private Const WM_MOUSEWHEEL As Integer = &H20A
    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_MOUSEWHEEL Then
            Dim pt = PanelDibujo.PointToClient(Cursor.Position)
            If PanelDibujo.ClientRectangle.Contains(pt) Then
                Dim delta As Short = CShort((m.WParam.ToInt64() >> 16) And &HFFFF)
                _zoom = If(delta > 0, Math.Min(_zoom * 1.15, 20.0), Math.Max(_zoom / 1.15, 0.05))
                ActualizarZoomLabel() : PanelDibujo.Invalidate()
                m.Result = IntPtr.Zero : Return
            End If
        End If
        MyBase.WndProc(m)
    End Sub

    ' ─── Botones ────────────────────────────────────────────────────────────────
    Private Sub BtnSelTodo_Click(sender As Object, e As EventArgs) Handles BtnSelTodo.Click
        For i = 0 To _candidatos.Count - 1
            _candidatos(i).Seleccionado = True
            DGV.Rows(i).Cells(COL_SEL).Value = True
        Next
        ActualizarEstadisticas() : DibujarPlanta()
    End Sub

    Private Sub BtnDeselTodo_Click(sender As Object, e As EventArgs) Handles BtnDeselTodo.Click
        For i = 0 To _candidatos.Count - 1
            _candidatos(i).Seleccionado = False
            DGV.Rows(i).Cells(COL_SEL).Value = False
        Next
        ActualizarEstadisticas() : DibujarPlanta()
    End Sub

    Private Sub BtnConfirmar_Click(sender As Object, e As EventArgs) Handles BtnConfirmar.Click
        Dim seleccionados = _candidatos.Where(Function(c) c.Seleccionado).ToList()
        If seleccionados.Count = 0 Then
            MessageBox.Show("No hay elementos seleccionados. Marque al menos uno para importar.",
                            "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        SelFrame.Clear() : SelPier.Clear()
        For Each c In seleccionados
            If c.Tipo = "Frame" Then SelFrame.Add(c.Label) Else SelPier.Add(c.Label)
        Next
        Confirmado = True
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub BtnCancelar_Click(sender As Object, e As EventArgs) Handles BtnCancelar.Click
        Confirmado = False
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ' ─── Limpieza ───────────────────────────────────────────────────────────────
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
    End Sub

End Class
