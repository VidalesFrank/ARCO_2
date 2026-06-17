Imports System.Linq

Public Class Form_AgrupacionManualVigas
    Inherits Form

    Public Property FramesResultantes As New List(Of String)

    Private ReadOnly _lstDisponibles As New ListBox()
    Private ReadOnly _lstEnViga As New ListBox()

    Public Sub New(nombreViga As String,
                   framesEnViga As List(Of cFrame),
                   framesDisponibles As List(Of cFrame))

        InitializeUI(nombreViga)

        For Each f In framesEnViga
            _lstEnViga.Items.Add(New FrameListItem(f))
        Next
        For Each f In framesDisponibles.OrderBy(Function(f2) f2.ObjectLabel)
            _lstDisponibles.Items.Add(New FrameListItem(f))
        Next
    End Sub

    Private Sub InitializeUI(nombreViga As String)
        Me.Text = "Agrupación Manual — " & nombreViga
        Me.Size = New Size(960, 640)
        Me.MinimumSize = New Size(720, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(45, 45, 48)
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        ' Outer layout: header / content / footer
        Dim outer As New TableLayoutPanel()
        outer.Dock = DockStyle.Fill
        outer.RowCount = 3
        outer.ColumnCount = 1
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
        outer.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        outer.RowStyles.Add(New RowStyle(SizeType.Absolute, 56))
        outer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        outer.Margin = New Padding(0)
        outer.Padding = New Padding(0)
        Me.Controls.Add(outer)

        ' Row 0 — Header
        Dim lblHeader As New Label()
        lblHeader.Text = "Doble clic o botón para mover frames entre las listas"
        lblHeader.Dock = DockStyle.Fill
        lblHeader.TextAlign = ContentAlignment.MiddleCenter
        lblHeader.ForeColor = Color.White
        lblHeader.BackColor = Color.FromArgb(87, 87, 86)
        lblHeader.Font = New Font("Segoe UI", 10)
        lblHeader.Margin = New Padding(0)
        outer.Controls.Add(lblHeader, 0, 0)

        ' Row 1 — Content: three columns (left list | buttons | right list)
        Dim inner As New TableLayoutPanel()
        inner.Dock = DockStyle.Fill
        inner.RowCount = 1
        inner.ColumnCount = 3
        inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 46))
        inner.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 148))
        inner.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 54))
        inner.Padding = New Padding(10)
        inner.BackColor = Color.FromArgb(45, 45, 48)
        inner.Margin = New Padding(0)
        outer.Controls.Add(inner, 0, 1)

        ' Left — frames disponibles
        Dim grpDisp As New GroupBox()
        grpDisp.Text = "Frames disponibles en el piso"
        grpDisp.ForeColor = Color.LightGray
        grpDisp.Dock = DockStyle.Fill
        grpDisp.Margin = New Padding(3)
        _lstDisponibles.Dock = DockStyle.Fill
        _lstDisponibles.BackColor = Color.FromArgb(30, 30, 30)
        _lstDisponibles.ForeColor = Color.White
        _lstDisponibles.SelectionMode = SelectionMode.MultiExtended
        _lstDisponibles.Font = New Font("Consolas", 9.5F)
        _lstDisponibles.BorderStyle = BorderStyle.None
        AddHandler _lstDisponibles.DoubleClick, AddressOf OnAgregar
        grpDisp.Controls.Add(_lstDisponibles)
        inner.Controls.Add(grpDisp, 0, 0)

        ' Center — buttons
        Dim pnlCenter As New FlowLayoutPanel()
        pnlCenter.FlowDirection = FlowDirection.TopDown
        pnlCenter.Dock = DockStyle.Fill
        pnlCenter.BackColor = Color.Transparent
        pnlCenter.Padding = New Padding(12, 60, 12, 12)
        pnlCenter.WrapContents = False
        inner.Controls.Add(pnlCenter, 1, 0)

        Dim btnAgregar As New Button()
        btnAgregar.Text = "Agregar  →"
        btnAgregar.Size = New Size(122, 42)
        btnAgregar.BackColor = Color.FromArgb(0, 122, 204)
        btnAgregar.ForeColor = Color.White
        btnAgregar.FlatStyle = FlatStyle.Flat
        btnAgregar.FlatAppearance.BorderSize = 0
        btnAgregar.Margin = New Padding(0, 8, 0, 6)
        AddHandler btnAgregar.Click, AddressOf OnAgregar
        pnlCenter.Controls.Add(btnAgregar)

        Dim btnQuitar As New Button()
        btnQuitar.Text = "←  Quitar"
        btnQuitar.Size = New Size(122, 42)
        btnQuitar.BackColor = Color.FromArgb(170, 50, 50)
        btnQuitar.ForeColor = Color.White
        btnQuitar.FlatStyle = FlatStyle.Flat
        btnQuitar.FlatAppearance.BorderSize = 0
        btnQuitar.Margin = New Padding(0, 0, 0, 20)
        AddHandler btnQuitar.Click, AddressOf OnQuitar
        pnlCenter.Controls.Add(btnQuitar)

        Dim lblHint As New Label()
        lblHint.Text = "* Doble clic" & Environment.NewLine & "  para mover"
        lblHint.Size = New Size(122, 36)
        lblHint.ForeColor = Color.DimGray
        lblHint.Font = New Font("Segoe UI", 8)
        lblHint.TextAlign = ContentAlignment.TopCenter
        pnlCenter.Controls.Add(lblHint)

        ' Right — frames en la viga
        Dim grpViga As New GroupBox()
        grpViga.Text = "Frames en esta viga"
        grpViga.ForeColor = Color.FromArgb(144, 238, 144)
        grpViga.Dock = DockStyle.Fill
        grpViga.Margin = New Padding(3)
        _lstEnViga.Dock = DockStyle.Fill
        _lstEnViga.BackColor = Color.FromArgb(22, 38, 22)
        _lstEnViga.ForeColor = Color.FromArgb(144, 238, 144)
        _lstEnViga.SelectionMode = SelectionMode.MultiExtended
        _lstEnViga.Font = New Font("Consolas", 9.5F)
        _lstEnViga.BorderStyle = BorderStyle.None
        AddHandler _lstEnViga.DoubleClick, AddressOf OnQuitar
        grpViga.Controls.Add(_lstEnViga)
        inner.Controls.Add(grpViga, 2, 0)

        ' Row 2 — Footer buttons
        Dim pnlFooter As New FlowLayoutPanel()
        pnlFooter.Dock = DockStyle.Fill
        pnlFooter.FlowDirection = FlowDirection.RightToLeft
        pnlFooter.BackColor = Color.FromArgb(58, 58, 58)
        pnlFooter.Padding = New Padding(10, 9, 10, 9)
        pnlFooter.Margin = New Padding(0)
        outer.Controls.Add(pnlFooter, 0, 2)

        Dim btnCancel As New Button()
        btnCancel.Text = "Cancelar"
        btnCancel.Size = New Size(120, 36)
        btnCancel.BackColor = Color.FromArgb(87, 87, 86)
        btnCancel.ForeColor = Color.White
        btnCancel.FlatStyle = FlatStyle.Flat
        btnCancel.FlatAppearance.BorderSize = 0
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.Margin = New Padding(0, 0, 0, 0)
        pnlFooter.Controls.Add(btnCancel)

        Dim btnOK As New Button()
        btnOK.Text = "Aceptar"
        btnOK.Size = New Size(120, 36)
        btnOK.BackColor = Color.FromArgb(0, 122, 204)
        btnOK.ForeColor = Color.White
        btnOK.FlatStyle = FlatStyle.Flat
        btnOK.FlatAppearance.BorderSize = 0
        btnOK.Margin = New Padding(0, 0, 8, 0)
        AddHandler btnOK.Click, AddressOf OnAceptar
        pnlFooter.Controls.Add(btnOK)

        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel
    End Sub

    Private Sub OnAgregar(sender As Object, e As EventArgs)
        Dim toMove = _lstDisponibles.SelectedItems.Cast(Of FrameListItem)().ToList()
        For Each item In toMove
            _lstDisponibles.Items.Remove(item)
            _lstEnViga.Items.Add(item)
        Next
    End Sub

    Private Sub OnQuitar(sender As Object, e As EventArgs)
        Dim toMove = _lstEnViga.SelectedItems.Cast(Of FrameListItem)().ToList()
        For Each item In toMove
            _lstEnViga.Items.Remove(item)
            _lstDisponibles.Items.Add(item)
        Next
    End Sub

    Private Sub OnAceptar(sender As Object, e As EventArgs)
        FramesResultantes = _lstEnViga.Items.Cast(Of FrameListItem)().
            Select(Function(fi) fi.Frame.ObjectLabel).ToList()
        Me.DialogResult = DialogResult.OK
    End Sub

    Private Class FrameListItem
        Public ReadOnly Frame As cFrame

        Public Sub New(f As cFrame)
            Frame = f
        End Sub

        Public Overrides Function ToString() As String
            Dim sec As String = ""
            If Frame.Section IsNot Nothing Then
                sec = String.Format("  [{0}×{1}cm]",
                                    CInt(Frame.Section.b * 100),
                                    CInt(Frame.Section.h * 100))
            End If
            Dim L As String = If(Frame.Longitud > 0.01,
                                  String.Format("  L={0:F2}m", Frame.Longitud), "")
            Return Frame.ObjectLabel & L & sec
        End Function
    End Class

End Class
