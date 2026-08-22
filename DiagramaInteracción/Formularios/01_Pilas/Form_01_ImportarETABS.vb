Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Formulario de validación de candidatos a pila detectados desde ETABS.
''' Muestra Frame (círculo azul) y Pier (cuadrado naranja) en vista en planta GDI+.
''' El usuario confirma cuáles corresponden a pilas antes de importar al proyecto.
''' </summary>
Public Class Form_01_ImportarETABS
    Inherits Form

    ' ─── Datos ──────────────────────────────────────────────────────────────────
    Private _candidatos As List(Of cCandidatoPila)

    ' ─── Propiedad de resultado ─────────────────────────────────────────────────
    Public Property ImportacionConfirmada As Boolean = False

    ' ─── Controles ──────────────────────────────────────────────────────────────
    Private WithEvents SplitContainer1 As New SplitContainer()
    Private WithEvents PicPlanta As New PictureBox()
    Private LabelTituloPlanta As New Label()
    Private LabelInfo As New Label()
    Private LabelEstadisticas As New Label()
    Private WithEvents DGV_Candidatos As New DataGridView()
    Private PanelBotones As New Panel()
    Private WithEvents BtnSelTodo As New Button()
    Private WithEvents BtnDeselTodo As New Button()
    Private WithEvents BtnConfirmar As New Button()
    Private WithEvents BtnCancelar As New Button()

    ' ─── Constantes de dibujo ───────────────────────────────────────────────────
    Private Const RADIO_FRAME As Integer = 8       ' px, círculo Frame
    Private Const LADO_PIER As Integer = 16        ' px, cuadrado Pier
    Private Const MARGEN_PORCENTAJE As Double = 0.15

    ' ─── Índices columnas DGV ───────────────────────────────────────────────────
    Private Const COL_SEL As Integer = 0
    Private Const COL_NOMBRE As Integer = 1
    Private Const COL_TIPO As Integer = 2
    Private Const COL_STORY As Integer = 3
    Private Const COL_X As Integer = 4
    Private Const COL_Y As Integer = 5
    Private Const COL_NCOMBOS As Integer = 6
    Private Const COL_ESTADO As Integer = 7

    ' ─── Constructor ────────────────────────────────────────────────────────────
    Public Sub New(candidatos As List(Of cCandidatoPila))
        _candidatos = If(candidatos, New List(Of cCandidatoPila)())
        InitializeComponent()
    End Sub

    ' ─── Construcción de controles (sin Designer) ───────────────────────────────
    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Formulario
        Me.Text = "Importar desde ETABS — Detección de Apoyos"
        Me.Size = New Size(1000, 650)
        Me.MinimumSize = New Size(800, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.White

        ' ── SplitContainer ────────────────────────────────────────────────────
        SplitContainer1.Dock = DockStyle.Fill
        SplitContainer1.Orientation = Orientation.Vertical
        SplitContainer1.SplitterWidth = 4
        Me.Controls.Add(SplitContainer1)

        ' ── Panel izquierdo: Vista en planta ──────────────────────────────────
        LabelTituloPlanta.Text = "Vista en Planta"
        LabelTituloPlanta.Dock = DockStyle.Top
        LabelTituloPlanta.Height = 24
        LabelTituloPlanta.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        LabelTituloPlanta.ForeColor = Color.FromArgb(45, 45, 45)
        LabelTituloPlanta.TextAlign = ContentAlignment.MiddleLeft
        LabelTituloPlanta.Padding = New Padding(4, 0, 0, 0)
        LabelTituloPlanta.BackColor = Color.FromArgb(235, 240, 248)

        PicPlanta.Dock = DockStyle.Fill
        PicPlanta.BackColor = Color.FromArgb(245, 248, 252)
        PicPlanta.BorderStyle = BorderStyle.None

        LabelInfo.Dock = DockStyle.Bottom
        LabelInfo.Height = 44
        LabelInfo.Font = New Font("Segoe UI", 8.5F)
        LabelInfo.ForeColor = Color.FromArgb(60, 60, 60)
        LabelInfo.BackColor = Color.FromArgb(245, 245, 245)
        LabelInfo.TextAlign = ContentAlignment.MiddleLeft
        LabelInfo.Padding = New Padding(6, 0, 0, 0)
        LabelInfo.BorderStyle = BorderStyle.FixedSingle
        LabelInfo.Text = "Seleccione una fila para ver el detalle del candidato."

        SplitContainer1.Panel1.Controls.Add(PicPlanta)
        SplitContainer1.Panel1.Controls.Add(LabelInfo)
        SplitContainer1.Panel1.Controls.Add(LabelTituloPlanta)

        ' ── Panel derecho: Tabla + botones ────────────────────────────────────
        LabelEstadisticas.Dock = DockStyle.Top
        LabelEstadisticas.Height = 24
        LabelEstadisticas.Font = New Font("Segoe UI", 8.5F)
        LabelEstadisticas.ForeColor = Color.FromArgb(60, 60, 60)
        LabelEstadisticas.BackColor = Color.FromArgb(235, 240, 248)
        LabelEstadisticas.TextAlign = ContentAlignment.MiddleLeft
        LabelEstadisticas.Padding = New Padding(4, 0, 0, 0)
        LabelEstadisticas.Text = "Candidatos detectados"

        ConfigurarDGV()
        DGV_Candidatos.Dock = DockStyle.Fill

        ' Panel de botones (Dock=Bottom, Height=80)
        PanelBotones.Dock = DockStyle.Bottom
        PanelBotones.Height = 80
        PanelBotones.BackColor = Color.FromArgb(245, 245, 245)
        PanelBotones.Padding = New Padding(8, 12, 8, 12)

        ConfigurarBoton(BtnSelTodo, "Seleccionar Todo", Color.FromArgb(108, 117, 125))
        ConfigurarBoton(BtnDeselTodo, "Deseleccionar Todo", Color.FromArgb(108, 117, 125))
        ConfigurarBoton(BtnConfirmar, "Confirmar Importar", Color.FromArgb(40, 167, 69))
        ConfigurarBoton(BtnCancelar, "Cancelar", Color.FromArgb(220, 53, 69))

        ' Posición de botones dentro del panel (se calculan en Resize)
        PanelBotones.Controls.Add(BtnSelTodo)
        PanelBotones.Controls.Add(BtnDeselTodo)
        PanelBotones.Controls.Add(BtnConfirmar)
        PanelBotones.Controls.Add(BtnCancelar)

        SplitContainer1.Panel2.Controls.Add(DGV_Candidatos)
        SplitContainer1.Panel2.Controls.Add(PanelBotones)
        SplitContainer1.Panel2.Controls.Add(LabelEstadisticas)

        Me.ResumeLayout(False)

        AddHandler PanelBotones.Resize, AddressOf PanelBotones_Resize
    End Sub

    Private Sub ConfigurarDGV()
        DGV_Candidatos.AutoGenerateColumns = False
        DGV_Candidatos.AllowUserToAddRows = False
        DGV_Candidatos.AllowUserToDeleteRows = False
        DGV_Candidatos.ReadOnly = False
        DGV_Candidatos.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        DGV_Candidatos.MultiSelect = False
        DGV_Candidatos.RowHeadersVisible = False
        DGV_Candidatos.BorderStyle = BorderStyle.None
        DGV_Candidatos.BackgroundColor = Color.White
        DGV_Candidatos.GridColor = Color.FromArgb(220, 220, 220)
        DGV_Candidatos.ColumnHeadersHeight = 28
        DGV_Candidatos.RowTemplate.Height = 24
        DGV_Candidatos.Font = New Font("Segoe UI", 9)

        With DGV_Candidatos.ColumnHeadersDefaultCellStyle
            .BackColor = Color.FromArgb(52, 73, 94)
            .ForeColor = Color.White
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
        DGV_Candidatos.EnableHeadersVisualStyles = False

        ' COL 0 – CheckBox "Sel."
        Dim colSel As New DataGridViewCheckBoxColumn()
        colSel.HeaderText = "Sel."
        colSel.Name = "colSel"
        colSel.Width = 44
        colSel.ReadOnly = False
        colSel.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        DGV_Candidatos.Columns.Add(colSel)

        ' COL 1 – Nombre (editable)
        Dim colNombre As New DataGridViewTextBoxColumn()
        colNombre.HeaderText = "Nombre"
        colNombre.Name = "colNombre"
        colNombre.Width = 90
        colNombre.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        DGV_Candidatos.Columns.Add(colNombre)

        ' COL 2 – Tipo
        Dim colTipo As New DataGridViewTextBoxColumn()
        colTipo.HeaderText = "Tipo"
        colTipo.Name = "colTipo"
        colTipo.Width = 55
        colTipo.ReadOnly = True
        colTipo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        DGV_Candidatos.Columns.Add(colTipo)

        ' COL 3 – Story
        Dim colStory As New DataGridViewTextBoxColumn()
        colStory.HeaderText = "Story"
        colStory.Name = "colStory"
        colStory.Width = 70
        colStory.ReadOnly = True
        colStory.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        DGV_Candidatos.Columns.Add(colStory)

        ' COL 4 – X [m]
        Dim colX As New DataGridViewTextBoxColumn()
        colX.HeaderText = "X [m]"
        colX.Name = "colX"
        colX.Width = 65
        colX.ReadOnly = True
        colX.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        colX.DefaultCellStyle.Format = "F2"
        DGV_Candidatos.Columns.Add(colX)

        ' COL 5 – Y [m]
        Dim colY As New DataGridViewTextBoxColumn()
        colY.HeaderText = "Y [m]"
        colY.Name = "colY"
        colY.Width = 65
        colY.ReadOnly = True
        colY.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        colY.DefaultCellStyle.Format = "F2"
        DGV_Candidatos.Columns.Add(colY)

        ' COL 6 – N° Combos
        Dim colCombos As New DataGridViewTextBoxColumn()
        colCombos.HeaderText = "N° Combos"
        colCombos.Name = "colCombos"
        colCombos.Width = 75
        colCombos.ReadOnly = True
        colCombos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        DGV_Candidatos.Columns.Add(colCombos)

        ' COL 7 – Estado
        Dim colEstado As New DataGridViewTextBoxColumn()
        colEstado.HeaderText = "Estado"
        colEstado.Name = "colEstado"
        colEstado.Width = 60
        colEstado.ReadOnly = True
        colEstado.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        DGV_Candidatos.Columns.Add(colEstado)

        ' Última columna se expande
        DGV_Candidatos.Columns(COL_ESTADO).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
    End Sub

    Private Sub ConfigurarBoton(btn As Button, texto As String, color As Color)
        btn.Text = texto
        btn.BackColor = color
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        btn.Height = 36
        btn.Width = 130
        btn.Cursor = Cursors.Hand
    End Sub

    ' ─── Carga del formulario ───────────────────────────────────────────────────
    Private Sub Form_01_ImportarETABS_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CargarDGV()
        ActualizarEstadisticas()
        PosicionarBotones()
        DibujarPlanta()
    End Sub

    Private Sub CargarDGV()
        DGV_Candidatos.Rows.Clear()
        For Each c As cCandidatoPila In _candidatos
            Dim idx As Integer = DGV_Candidatos.Rows.Add()
            Dim row As DataGridViewRow = DGV_Candidatos.Rows(idx)
            row.Cells(COL_SEL).Value = c.Seleccionado
            row.Cells(COL_NOMBRE).Value = c.Nombre
            row.Cells(COL_TIPO).Value = c.Tipo
            row.Cells(COL_STORY).Value = c.Story
            row.Cells(COL_X).Value = c.X
            row.Cells(COL_Y).Value = c.Y
            row.Cells(COL_NCOMBOS).Value = c.NumeroCombinaciones
            row.Cells(COL_ESTADO).Value = c.Estado
            AplicarColorEstado(row, c)
        Next
    End Sub

    Private Sub AplicarColorEstado(row As DataGridViewRow, c As cCandidatoPila)
        If String.Equals(c.Estado, "OK", StringComparison.OrdinalIgnoreCase) Then
            row.Cells(COL_ESTADO).Style.BackColor = Color.FromArgb(198, 239, 206)
            row.Cells(COL_ESTADO).Style.ForeColor = Color.FromArgb(0, 97, 0)
        ElseIf Not String.IsNullOrEmpty(c.Estado) Then
            row.Cells(COL_ESTADO).Style.BackColor = Color.FromArgb(255, 199, 206)
            row.Cells(COL_ESTADO).Style.ForeColor = Color.FromArgb(156, 0, 6)
        End If

        If c.Tipo = "Frame" Then
            row.Cells(COL_TIPO).Style.ForeColor = Color.FromArgb(0, 70, 160)
        ElseIf c.Tipo = "Pier" Then
            row.Cells(COL_TIPO).Style.ForeColor = Color.FromArgb(180, 90, 0)
        End If
    End Sub

    Private Sub ActualizarEstadisticas()
        Dim total As Integer = _candidatos.Count
        Dim frames As Integer = _candidatos.Where(Function(c) c.Tipo = "Frame").Count()
        Dim piers As Integer = _candidatos.Where(Function(c) c.Tipo = "Pier").Count()
        Dim sel As Integer = _candidatos.Where(Function(c) c.Seleccionado).Count()
        LabelEstadisticas.Text =
            $"Candidatos: {total}  |  Frame: {frames}  |  Pier: {piers}  |  Seleccionados: {sel}"
    End Sub

    Private Sub PosicionarBotones()
        Dim pad As Integer = 6
        Dim y0 As Integer = (PanelBotones.Height - 36) \ 2
        Dim totalW As Integer = PanelBotones.Width - 2 * pad
        Dim bw As Integer = Math.Max(80, (totalW - 3 * pad) \ 4)

        BtnSelTodo.Width = bw : BtnDeselTodo.Width = bw
        BtnConfirmar.Width = bw : BtnCancelar.Width = bw

        BtnSelTodo.Location = New Point(pad, y0)
        BtnDeselTodo.Location = New Point(pad + bw + pad, y0)
        BtnConfirmar.Location = New Point(pad + 2 * (bw + pad), y0)
        BtnCancelar.Location = New Point(pad + 3 * (bw + pad), y0)
    End Sub

    Private Sub PanelBotones_Resize(sender As Object, e As EventArgs)
        PosicionarBotones()
    End Sub

    ' ─── Dibujo de la planta ────────────────────────────────────────────────────
    Private Sub DibujarPlanta()
        Dim w As Integer = Math.Max(PicPlanta.Width, 50)
        Dim h As Integer = Math.Max(PicPlanta.Height, 50)

        Dim bmp As New Bitmap(w, h)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit
            g.Clear(Color.FromArgb(245, 248, 252))

            If _candidatos.Count = 0 Then
                Using br As New SolidBrush(Color.Gray)
                    g.DrawString("No hay candidatos detectados.",
                                 New Font("Segoe UI", 10), br, 20, 20)
                End Using
            Else
                DibujarCandidatosEnPlanta(g, w, h)
            End If
        End Using

        Dim viejo = PicPlanta.Image
        PicPlanta.Image = bmp
        If viejo IsNot Nothing Then viejo.Dispose()
    End Sub

    Private Sub DibujarCandidatosEnPlanta(g As Graphics, w As Integer, h As Integer)
        ' ── Calcular rango de coordenadas ────────────────────────────────────
        Dim xs = _candidatos.Select(Function(c) c.X).ToList()
        Dim ys = _candidatos.Select(Function(c) c.Y).ToList()

        Dim xMin As Double = xs.Min()
        Dim xMax As Double = xs.Max()
        Dim yMin As Double = ys.Min()
        Dim yMax As Double = ys.Max()

        Dim dx As Double = xMax - xMin
        Dim dy As Double = yMax - yMin
        If dx < 0.001 Then dx = 1.0
        If dy < 0.001 Then dy = 1.0

        ' Margen del 15%
        Dim mX As Integer = CInt(w * MARGEN_PORCENTAJE)
        Dim mY As Integer = CInt(h * MARGEN_PORCENTAJE)
        Dim drawW As Integer = w - 2 * mX
        Dim drawH As Integer = h - 2 * mY - 20  ' 20 px para barra de escala

        ' Escala uniforme (preservar aspecto)
        Dim escX As Double = drawW / dx
        Dim escY As Double = drawH / dy
        Dim esc As Double = Math.Min(escX, escY)

        ' Funciones de transformación: X ETABS → pantalla derecha; Y ETABS → pantalla arriba
        Dim Tx As Func(Of Double, Single) = Function(x) CSng(mX + (x - xMin) * esc)
        Dim Ty As Func(Of Double, Single) = Function(y) CSng(mY + drawH - (y - yMin) * esc)

        ' ── Obtener fila seleccionada en DGV ─────────────────────────────────
        Dim idxSel As Integer = -1
        If DGV_Candidatos.CurrentRow IsNot Nothing Then
            idxSel = DGV_Candidatos.CurrentRow.Index
        End If

        ' ── Dibujar cada candidato ───────────────────────────────────────────
        Dim fontLabel As New Font("Segoe UI", 7)
        Dim brushLabel As New SolidBrush(Color.FromArgb(50, 50, 50))

        For i As Integer = 0 To _candidatos.Count - 1
            Dim c As cCandidatoPila = _candidatos(i)
            Dim px As Single = Tx(c.X)
            Dim py As Single = Ty(c.Y)
            Dim esSel As Boolean = (i = idxSel)

            ' Color base según tipo y estado seleccionado
            Dim colorRelleno As Color
            Dim colorBorde As Color

            If Not c.Seleccionado Then
                colorRelleno = Color.FromArgb(160, 180, 180, 180)
                colorBorde = Color.FromArgb(160, 130, 130, 130)
            ElseIf c.Tipo = "Frame" Then
                colorRelleno = Color.FromArgb(200, 30, 100, 200)
                colorBorde = Color.FromArgb(0, 55, 150)
            Else ' Pier
                colorRelleno = Color.FromArgb(200, 220, 110, 10)
                colorBorde = Color.FromArgb(180, 80, 0)
            End If

            ' Resaltado de selección en DGV (anillo rojo)
            If esSel Then
                Using penHalo As New Pen(Color.Red, 3)
                    If c.Tipo = "Frame" Then
                        g.DrawEllipse(penHalo,
                                      px - RADIO_FRAME - 4, py - RADIO_FRAME - 4,
                                      (RADIO_FRAME + 4) * 2, (RADIO_FRAME + 4) * 2)
                    Else
                        g.DrawRectangle(penHalo,
                                        px - LADO_PIER \ 2 - 4, py - LADO_PIER \ 2 - 4,
                                        LADO_PIER + 8, LADO_PIER + 8)
                    End If
                End Using
            End If

            ' Dibujar forma
            Using brRelleno As New SolidBrush(colorRelleno)
            Using penBorde As New Pen(colorBorde, If(esSel, 2.0F, 1.0F))
                If c.Tipo = "Frame" Then
                    ' Círculo azul
                    g.FillEllipse(brRelleno,
                                  px - RADIO_FRAME, py - RADIO_FRAME,
                                  RADIO_FRAME * 2, RADIO_FRAME * 2)
                    g.DrawEllipse(penBorde,
                                  px - RADIO_FRAME, py - RADIO_FRAME,
                                  RADIO_FRAME * 2, RADIO_FRAME * 2)
                Else
                    ' Cuadrado naranja
                    g.FillRectangle(brRelleno,
                                    px - LADO_PIER \ 2, py - LADO_PIER \ 2,
                                    LADO_PIER, LADO_PIER)
                    g.DrawRectangle(penBorde,
                                    px - LADO_PIER \ 2, py - LADO_PIER \ 2,
                                    LADO_PIER, LADO_PIER)
                End If
            End Using
            End Using

            ' Nombre (pequeño, desplazado arriba y a la derecha del símbolo)
            Dim offset As Integer = If(c.Tipo = "Frame", RADIO_FRAME + 2, LADO_PIER \ 2 + 2)
            g.DrawString(c.Nombre, fontLabel, brushLabel, px + offset, py - 9)
        Next

        fontLabel.Dispose()
        brushLabel.Dispose()

        ' ── Barra de escala ──────────────────────────────────────────────────
        DibujarBarraEscala(g, w, h, esc)
    End Sub

    Private Sub DibujarBarraEscala(g As Graphics, w As Integer, h As Integer, esc As Double)
        If esc <= 0 Then Return

        ' Calcular longitud de barra "bonita" (1, 2, 5, 10, 20 m...)
        Dim targetPx As Double = w * 0.20   ' ~20% del ancho
        Dim escalaM As Double = targetPx / esc
        Dim mag As Double = Math.Pow(10, Math.Floor(Math.Log10(escalaM)))
        Dim nice As Double = escalaM / mag
        If nice < 2 Then
            nice = 1
        ElseIf nice < 5 Then
            nice = 2
        Else
            nice = 5
        End If
        Dim barraMetros As Double = nice * mag
        Dim barraPx As Integer = CInt(barraMetros * esc)

        If barraPx < 10 Then Return

        Dim x1 As Integer = CInt(w * MARGEN_PORCENTAJE)
        Dim y1 As Integer = h - 14
        Dim x2 As Integer = x1 + barraPx

        Using penEscala As New Pen(Color.FromArgb(80, 80, 80), 2)
            g.DrawLine(penEscala, x1, y1, x2, y1)
            g.DrawLine(penEscala, x1, y1 - 4, x1, y1 + 4)
            g.DrawLine(penEscala, x2, y1 - 4, x2, y1 + 4)
        End Using

        Dim texto As String = $"{barraMetros:G4} m"
        Using fontEsc As New Font("Segoe UI", 7.5F)
        Using brEsc As New SolidBrush(Color.FromArgb(70, 70, 70))
            Dim sz As SizeF = g.MeasureString(texto, fontEsc)
            g.DrawString(texto, fontEsc, brEsc,
                         x1 + (barraPx - sz.Width) / 2, y1 - 14)
        End Using
        End Using
    End Sub

    ' ─── Eventos DGV ────────────────────────────────────────────────────────────
    Private Sub DGV_Candidatos_SelectionChanged(sender As Object, e As EventArgs) _
        Handles DGV_Candidatos.SelectionChanged
        ActualizarInfoPanel()
        DibujarPlanta()
    End Sub

    Private Sub DGV_Candidatos_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles DGV_Candidatos.CellValueChanged
        If e.RowIndex < 0 OrElse e.RowIndex >= _candidatos.Count Then Return

        If e.ColumnIndex = COL_SEL Then
            Dim val As Object = DGV_Candidatos.Rows(e.RowIndex).Cells(COL_SEL).Value
            _candidatos(e.RowIndex).Seleccionado = If(val IsNot Nothing AndAlso CBool(val), True, False)
            ActualizarEstadisticas()
            DibujarPlanta()
        ElseIf e.ColumnIndex = COL_NOMBRE Then
            Dim val As Object = DGV_Candidatos.Rows(e.RowIndex).Cells(COL_NOMBRE).Value
            _candidatos(e.RowIndex).Nombre = If(val IsNot Nothing, val.ToString(), "")
        End If
    End Sub

    ' Necesario para que el CellValueChanged del CheckBox se dispare de inmediato
    Private Sub DGV_Candidatos_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) _
        Handles DGV_Candidatos.CurrentCellDirtyStateChanged
        If DGV_Candidatos.IsCurrentCellDirty AndAlso
           DGV_Candidatos.CurrentCell IsNot Nothing AndAlso
           DGV_Candidatos.CurrentCell.ColumnIndex = COL_SEL Then
            DGV_Candidatos.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub ActualizarInfoPanel()
        Try
            If DGV_Candidatos.CurrentRow Is Nothing Then
                LabelInfo.Text = "Seleccione una fila para ver el detalle del candidato."
                Return
            End If
            Dim idx As Integer = DGV_Candidatos.CurrentRow.Index
            If idx < 0 OrElse idx >= _candidatos.Count Then Return
            Dim c As cCandidatoPila = _candidatos(idx)
            LabelInfo.Text =
                $"Tipo: {c.Tipo}  |  Fuente: {c.SourceLabel}  |  " &
                $"X: {c.X:F2} m  |  Y: {c.Y:F2} m  |  N° combos: {c.NumeroCombinaciones}  |  Story: {c.Story}"
        Catch ex As Exception
            LabelInfo.Text = "Error al obtener información del candidato."
        End Try
    End Sub

    ' ─── Evento Resize del PicPlanta ────────────────────────────────────────────
    Private Sub PicPlanta_Resize(sender As Object, e As EventArgs) Handles PicPlanta.Resize
        DibujarPlanta()
    End Sub

    ' ─── Botones ────────────────────────────────────────────────────────────────
    Private Sub BtnSelTodo_Click(sender As Object, e As EventArgs) Handles BtnSelTodo.Click
        For i As Integer = 0 To _candidatos.Count - 1
            _candidatos(i).Seleccionado = True
            DGV_Candidatos.Rows(i).Cells(COL_SEL).Value = True
        Next
        ActualizarEstadisticas()
        DibujarPlanta()
    End Sub

    Private Sub BtnDeselTodo_Click(sender As Object, e As EventArgs) Handles BtnDeselTodo.Click
        For i As Integer = 0 To _candidatos.Count - 1
            _candidatos(i).Seleccionado = False
            DGV_Candidatos.Rows(i).Cells(COL_SEL).Value = False
        Next
        ActualizarEstadisticas()
        DibujarPlanta()
    End Sub

    Private Sub BtnConfirmar_Click(sender As Object, e As EventArgs) Handles BtnConfirmar.Click
        Try
            Dim seleccionados As List(Of cCandidatoPila) =
                _candidatos.Where(Function(c) c.Seleccionado).ToList()

            If seleccionados.Count = 0 Then
                MessageBox.Show("No hay candidatos seleccionados. Seleccione al menos uno para importar.",
                                "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Sincronizar nombres editados desde el DGV a los candidatos
            For i As Integer = 0 To DGV_Candidatos.Rows.Count - 1
                If i >= _candidatos.Count Then Exit For
                Dim valNombre As Object = DGV_Candidatos.Rows(i).Cells(COL_NOMBRE).Value
                If valNombre IsNot Nothing Then
                    _candidatos(i).Nombre = valNombre.ToString()
                End If
            Next

            ' Acceder al proyecto mediante la referencia estándar del proyecto
            Dim Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

            ' Agregar reacciones al proyecto
            Proyecto.Elementos.Pilas.Reactions.Clear()
            For Each cand As cCandidatoPila In seleccionados
                For Each rxn As cCombinacionPila In cand.Reactions
                    Proyecto.Elementos.Pilas.Reactions.Add(rxn)
                Next
            Next

            ' Extraer lista de combinaciones únicas
            Proyecto.Elementos.Pilas.Lista_Combinaciones =
                Proyecto.Elementos.Pilas.Reactions _
                    .Select(Function(r) r.LoadCase) _
                    .Where(Function(lc) Not String.IsNullOrWhiteSpace(lc)) _
                    .Distinct() _
                    .OrderBy(Function(lc) lc) _
                    .ToList()

            ImportacionConfirmada = True
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show("Error al confirmar la importación:" & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnCancelar_Click(sender As Object, e As EventArgs) Handles BtnCancelar.Click
        ImportacionConfirmada = False
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ' ─── Limpieza de recursos GDI+ ──────────────────────────────────────────────
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        If PicPlanta.Image IsNot Nothing Then
            PicPlanta.Image.Dispose()
            PicPlanta.Image = Nothing
        End If
    End Sub

End Class
