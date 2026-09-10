Imports System.Drawing

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_AgrupacionManualNervios
'  Permite al usuario rearmar manualmente los frames de un nervio.
'  Patrón idéntico al de Form_AgrupacionManualVigas en el módulo 09.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_AgrupacionManualNervios
    Inherits System.Windows.Forms.Form

    ' ── Control principal ─────────────────────────────────────────────────────
    Private _lstDisponibles As New System.Windows.Forms.ListBox()
    Private _lstEnNervio As New System.Windows.Forms.ListBox()
    Private _lblTitulo As New System.Windows.Forms.Label()

    ' ── Resultado ─────────────────────────────────────────────────────────────
    ''' <summary>ObjectLabels de frames que quedarán en el nervio (resultado OK).</summary>
    Public Property FramesResultantes As New List(Of String)()

    ' ═══════════════════════════════════════════════════════════════════════════
    '  CONSTRUCTOR
    ' ═══════════════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Abre el diálogo de reagrupación.
    ''' </summary>
    ''' <param name="nombreNervio">Nombre del nervio que se está editando.</param>
    ''' <param name="framesEnNervio">Frames que actualmente pertenecen al nervio.</param>
    ''' <param name="framesDisponibles">Todos los frames del piso (del modelo de Nervios).</param>
    ''' <param name="framesOcupados">Frames ya asignados a otros nervios (se excluyen de disponibles).</param>
    Public Sub New(nombreNervio As String,
                   framesEnNervio As List(Of cFrame),
                   framesDisponibles As List(Of cFrame),
                   framesOcupados As HashSet(Of String))
        Me.Text = $"Reagrupar frames — {nombreNervio}"
        Me.Size = New Size(760, 420)
        Me.MinimumSize = New Size(660, 360)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(240, 240, 240)

        Dim labelsEnNervio As New HashSet(Of String)(
            framesEnNervio.Select(Function(f) f.ObjectLabel),
            StringComparer.OrdinalIgnoreCase)

        BuildUI()
        PopularListas(framesEnNervio, framesDisponibles, framesOcupados, labelsEnNervio)
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    '  CONSTRUCCIÓN DE UI
    ' ═══════════════════════════════════════════════════════════════════════════

    Private Sub BuildUI()
        Dim layout As New System.Windows.Forms.TableLayoutPanel()
        layout.Dock = System.Windows.Forms.DockStyle.Fill
        layout.RowCount = 3
        layout.ColumnCount = 1
        layout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40))
        layout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100))
        layout.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44))
        layout.Padding = New System.Windows.Forms.Padding(8)

        ' ── Header ─────────────────────────────────────────────────────────
        _lblTitulo.Dock = System.Windows.Forms.DockStyle.Fill
        _lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        _lblTitulo.Text = "Doble clic o use los botones para mover frames entre las listas."
        _lblTitulo.Font = New Font("Segoe UI", 9, FontStyle.Italic)
        _lblTitulo.ForeColor = Color.FromArgb(87, 87, 87)
        layout.Controls.Add(_lblTitulo, 0, 0)

        ' ── Cuerpo: ListBox izquierda | botones | ListBox derecha ──────────
        Dim panelCentral As New System.Windows.Forms.TableLayoutPanel()
        panelCentral.Dock = System.Windows.Forms.DockStyle.Fill
        panelCentral.ColumnCount = 3
        panelCentral.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45))
        panelCentral.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110))
        panelCentral.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55))
        panelCentral.Padding = New System.Windows.Forms.Padding(0, 4, 0, 4)

        ' Lista disponibles
        Dim gbDisp As New System.Windows.Forms.GroupBox()
        gbDisp.Text = "Frames disponibles (del piso)"
        gbDisp.Dock = System.Windows.Forms.DockStyle.Fill
        gbDisp.ForeColor = Color.FromArgb(87, 87, 87)
        gbDisp.Padding = New System.Windows.Forms.Padding(4, 14, 4, 4)

        _lstDisponibles.Dock = System.Windows.Forms.DockStyle.Fill
        _lstDisponibles.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended
        _lstDisponibles.Font = New Font("Consolas", 8.5)
        AddHandler _lstDisponibles.DoubleClick, AddressOf OnAgregar
        gbDisp.Controls.Add(_lstDisponibles)
        panelCentral.Controls.Add(gbDisp, 0, 0)

        ' Panel de botones centrales
        Dim panelBotones As New System.Windows.Forms.FlowLayoutPanel()
        panelBotones.Dock = System.Windows.Forms.DockStyle.Fill
        panelBotones.FlowDirection = System.Windows.Forms.FlowDirection.TopDown
        panelBotones.Padding = New System.Windows.Forms.Padding(8, 60, 8, 0)
        panelBotones.AutoScroll = False

        Dim btnAgregar As New System.Windows.Forms.Button()
        btnAgregar.Text = "Agregar →"
        btnAgregar.Size = New Size(90, 30)
        btnAgregar.Margin = New System.Windows.Forms.Padding(2, 6, 2, 6)
        btnAgregar.BackColor = Color.FromArgb(42, 120, 214)
        btnAgregar.ForeColor = Color.White
        btnAgregar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        btnAgregar.FlatAppearance.BorderSize = 0
        btnAgregar.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        AddHandler btnAgregar.Click, AddressOf OnAgregar

        Dim btnQuitar As New System.Windows.Forms.Button()
        btnQuitar.Text = "← Quitar"
        btnQuitar.Size = New Size(90, 30)
        btnQuitar.Margin = New System.Windows.Forms.Padding(2, 6, 2, 6)
        btnQuitar.BackColor = Color.FromArgb(180, 60, 50)
        btnQuitar.ForeColor = Color.White
        btnQuitar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        btnQuitar.FlatAppearance.BorderSize = 0
        btnQuitar.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        AddHandler btnQuitar.Click, AddressOf OnQuitar

        panelBotones.Controls.Add(btnAgregar)
        panelBotones.Controls.Add(btnQuitar)
        panelCentral.Controls.Add(panelBotones, 1, 0)

        ' Lista en nervio
        Dim gbNervio As New System.Windows.Forms.GroupBox()
        gbNervio.Text = "Frames en este nervio"
        gbNervio.Dock = System.Windows.Forms.DockStyle.Fill
        gbNervio.ForeColor = Color.FromArgb(87, 87, 87)
        gbNervio.Padding = New System.Windows.Forms.Padding(4, 14, 4, 4)

        _lstEnNervio.Dock = System.Windows.Forms.DockStyle.Fill
        _lstEnNervio.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended
        _lstEnNervio.Font = New Font("Consolas", 8.5)
        AddHandler _lstEnNervio.DoubleClick, AddressOf OnQuitar
        gbNervio.Controls.Add(_lstEnNervio)
        panelCentral.Controls.Add(gbNervio, 2, 0)

        layout.Controls.Add(panelCentral, 0, 1)

        ' ── Footer: botones Aceptar / Cancelar ─────────────────────────────
        Dim panelFooter As New System.Windows.Forms.Panel()
        panelFooter.Dock = System.Windows.Forms.DockStyle.Fill
        panelFooter.Padding = New System.Windows.Forms.Padding(0, 6, 0, 0)

        Dim btnOk As New System.Windows.Forms.Button()
        btnOk.Text = "Aceptar"
        btnOk.Size = New Size(100, 30)
        btnOk.Location = New Point(0, 0)
        btnOk.BackColor = Color.FromArgb(0, 150, 70)
        btnOk.ForeColor = Color.White
        btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        btnOk.FlatAppearance.BorderSize = 0
        btnOk.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        AddHandler btnOk.Click, AddressOf OnAceptar

        Dim btnCancelar As New System.Windows.Forms.Button()
        btnCancelar.Text = "Cancelar"
        btnCancelar.Size = New Size(100, 30)
        btnCancelar.Location = New Point(108, 0)
        btnCancelar.BackColor = Color.FromArgb(200, 200, 200)
        btnCancelar.ForeColor = Color.FromArgb(87, 87, 87)
        btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        btnCancelar.FlatAppearance.BorderSize = 0
        btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel
        AddHandler btnCancelar.Click, Sub() Me.Close()

        panelFooter.Controls.Add(btnOk)
        panelFooter.Controls.Add(btnCancelar)
        layout.Controls.Add(panelFooter, 0, 2)

        Me.Controls.Add(layout)
        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancelar
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    '  POPULAR LISTAS
    ' ═══════════════════════════════════════════════════════════════════════════

    Private Sub PopularListas(framesEnNervio As List(Of cFrame),
                               todosFramesPiso As List(Of cFrame),
                               framesOcupados As HashSet(Of String),
                               labelsEnNervio As HashSet(Of String))
        ' Frames en el nervio actual (panel derecho)
        For Each f In framesEnNervio
            _lstEnNervio.Items.Add(New FrameListItem(f))
        Next

        ' Frames disponibles: mismos piso, no en otro nervio, no ya en este nervio
        For Each f In todosFramesPiso
            If labelsEnNervio.Contains(f.ObjectLabel) Then Continue For
            If framesOcupados.Contains(f.ObjectLabel) Then Continue For
            _lstDisponibles.Items.Add(New FrameListItem(f))
        Next
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    '  EVENTOS DE BOTONES
    ' ═══════════════════════════════════════════════════════════════════════════

    Private Sub OnAgregar(sender As Object, e As EventArgs)
        Dim seleccionados = _lstDisponibles.SelectedItems.Cast(Of FrameListItem)().ToList()
        For Each item In seleccionados
            _lstDisponibles.Items.Remove(item)
            _lstEnNervio.Items.Add(item)
        Next
    End Sub

    Private Sub OnQuitar(sender As Object, e As EventArgs)
        Dim seleccionados = _lstEnNervio.SelectedItems.Cast(Of FrameListItem)().ToList()
        For Each item In seleccionados
            _lstEnNervio.Items.Remove(item)
            _lstDisponibles.Items.Add(item)
        Next
    End Sub

    Private Sub OnAceptar(sender As Object, e As EventArgs)
        If _lstEnNervio.Items.Count = 0 Then
            System.Windows.Forms.MessageBox.Show(
                "El nervio debe contener al menos un frame.",
                "Aviso", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning)
            Return
        End If
        FramesResultantes.Clear()
        For Each item In _lstEnNervio.Items.Cast(Of FrameListItem)()
            FramesResultantes.Add(item.Frame.ObjectLabel)
        Next
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════════
    '  CLASE INTERNA — wrapper de cFrame para ListBox
    ' ═══════════════════════════════════════════════════════════════════════════

    Private Class FrameListItem
        Public ReadOnly Frame As cFrame

        Public Sub New(f As cFrame)
            Frame = f
        End Sub

        Public Overrides Function ToString() As String
            Dim bw = If(Frame.Section IsNot Nothing, $"{Frame.Section.b * 100:F0}×{Frame.Section.h * 100:F0}cm", "—")
            Dim sec = If(Frame.Section IsNot Nothing, Frame.Section.Nombre, "—")
            Return $"{Frame.ObjectLabel,-10}  {sec,-16}  [{bw}]"
        End Function
    End Class

End Class
