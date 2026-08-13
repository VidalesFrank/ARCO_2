''' <summary>Diálogo simple para mover frames de un nervio a un nuevo grupo.</summary>
Public Class Form_11_SepararNervio
    Inherits Form

    Public Property NuevoNervio As cNervio

    Private _nervio As cNervio
    Private _listDisponibles As New CheckedListBox()
    Private _btnOk As New Button()
    Private _btnCancel As New Button()
    Private _lbl As New Label()

    Public Sub New(nervio As cNervio)
        _nervio = nervio
        InitUI()
    End Sub

    Private Sub InitUI()
        Me.Text = $"Separar tramos de {_nervio.Nombre}"
        Me.Size = New System.Drawing.Size(400, 450)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False

        _lbl.Text = "Seleccione los tramos que pasarán al NUEVO nervio:"
        _lbl.Location = New System.Drawing.Point(10, 10)
        _lbl.Size = New System.Drawing.Size(370, 30)
        _lbl.AutoSize = False

        _listDisponibles.Location = New System.Drawing.Point(10, 46)
        _listDisponibles.Size = New System.Drawing.Size(370, 320)
        _listDisponibles.CheckOnClick = True
        For Each fn In _nervio.Frames
            _listDisponibles.Items.Add(fn, False)
        Next

        _btnOk.Text = "Separar"
        _btnOk.Location = New System.Drawing.Point(10, 380)
        _btnOk.Size = New System.Drawing.Size(100, 28)
        _btnOk.DialogResult = DialogResult.OK
        AddHandler _btnOk.Click, AddressOf BtnOk_Click

        _btnCancel.Text = "Cancelar"
        _btnCancel.Location = New System.Drawing.Point(120, 380)
        _btnCancel.Size = New System.Drawing.Size(100, 28)
        _btnCancel.DialogResult = DialogResult.Cancel

        Me.Controls.AddRange(New Control() {_lbl, _listDisponibles, _btnOk, _btnCancel})
        Me.AcceptButton = _btnOk
        Me.CancelButton = _btnCancel
    End Sub

    Private Sub BtnOk_Click(sender As Object, e As EventArgs)
        Dim seleccionados As New List(Of cFrameNervio)
        For i As Integer = 0 To _listDisponibles.Items.Count - 1
            If _listDisponibles.GetItemChecked(i) Then
                seleccionados.Add(CType(_listDisponibles.Items(i), cFrameNervio))
            End If
        Next

        If seleccionados.Count = 0 OrElse seleccionados.Count = _nervio.Frames.Count Then
            MessageBox.Show("Seleccione al menos uno pero no todos los tramos.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.DialogResult = DialogResult.None
            Return
        End If

        Dim nuevoIdx = 1
        Dim nombreBase = _nervio.Nombre
        NuevoNervio = New cNervio With {
            .Nombre = $"{nombreBase}-B",
            .NombrePlano = $"{nombreBase}-B",
            .Piso = _nervio.Piso,
            .Tf_Losa = _nervio.Tf_Losa,
            .Paso_Nervios = _nervio.Paso_Nervios
        }

        For Each fn In seleccionados
            _nervio.Frames.Remove(fn)
            NuevoNervio.Frames.Add(fn)
        Next
    End Sub
End Class
