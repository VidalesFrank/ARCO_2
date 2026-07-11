Public Class Form_AlturasPisos
    Inherits Form

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblHnTotal As New Label()
    Private ReadOnly btnGenerar As New Button()
    Private ReadOnly btnAceptar As New Button()
    Private ReadOnly btnCancelar As New Button()

    Public Property HxTipico As Single = 2.95F
    Public Property NPisos As Integer = 5
    Public Property AlturasSalida As New List(Of Single)

    Public Sub New(nPisos As Integer, hxTipico As Single, alturasPrevias As List(Of Single))
        ' Me. prefix is required: VB.NET is case-insensitive, so without Me. the parameter
        ' name (nPisos) shadows the property (NPisos) and the assignment becomes a no-op.
        Me.NPisos = nPisos
        Me.HxTipico = hxTipico
        BuildUI()
        CargarAlturas(alturasPrevias)
    End Sub

    Private Sub BuildUI()
        Me.Text = "Alturas por piso"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = System.Drawing.Color.FromArgb(200, 200, 200)
        Me.Font = New Font("SansSerif", 11, FontStyle.Regular)

        ' Form height adapts to NPisos: DGV rows ≈30px each, header 30px, capped at 360px.
        ' Buttons are always placed 10px below the DGV so they are never cut off.
        Dim dgvH As Integer = Math.Max(80, Math.Min(360, 30 + NPisos * 30))
        Dim btnY As Integer = 80 + dgvH + 10
        Me.ClientSize = New Size(296, btnY + 34 + 10)

        lblHnTotal.AutoSize = True
        lblHnTotal.Font = New Font("SansSerif", 11, FontStyle.Bold)
        lblHnTotal.Location = New Point(10, 12)

        btnGenerar.Text = "Generar uniforme (Hx típica)"
        btnGenerar.Location = New Point(10, 38)
        btnGenerar.Size = New Size(276, 32)
        btnGenerar.FlatStyle = FlatStyle.Flat
        btnGenerar.FlatAppearance.BorderSize = 0
        btnGenerar.BackColor = System.Drawing.Color.FromArgb(87, 87, 87)
        btnGenerar.ForeColor = System.Drawing.Color.White
        btnGenerar.Font = New Font("SansSerif", 11, FontStyle.Bold)
        AddHandler btnGenerar.Click, AddressOf BtnGenerar_Click

        dgv.Location = New Point(10, 80)
        dgv.Size = New Size(276, dgvH)
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.RowHeadersVisible = False
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect
        dgv.MultiSelect = False
        dgv.BackgroundColor = System.Drawing.Color.FromArgb(200, 200, 200)
        dgv.BorderStyle = BorderStyle.Fixed3D
        dgv.Font = New Font("SansSerif", 11, FontStyle.Regular)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("SansSerif", 11, FontStyle.Bold)
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        Dim colPiso As New DataGridViewTextBoxColumn()
        colPiso.HeaderText = "Piso"
        colPiso.Name = "ColPiso"
        colPiso.ReadOnly = True
        colPiso.FillWeight = 40

        Dim colHw As New DataGridViewTextBoxColumn()
        colHw.HeaderText = "Hw (m)"
        colHw.Name = "ColHw"
        colHw.ReadOnly = False
        colHw.FillWeight = 60
        colHw.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        dgv.Columns.Add(colPiso)
        dgv.Columns.Add(colHw)
        AddHandler dgv.CellEndEdit, AddressOf Dgv_CellEndEdit

        btnAceptar.Text = "Aceptar"
        btnAceptar.Size = New Size(132, 34)
        btnAceptar.Location = New Point(10, btnY)
        btnAceptar.FlatStyle = FlatStyle.Flat
        btnAceptar.FlatAppearance.BorderSize = 0
        btnAceptar.BackColor = System.Drawing.Color.FromArgb(87, 87, 87)
        btnAceptar.ForeColor = System.Drawing.Color.White
        btnAceptar.Font = New Font("SansSerif", 11, FontStyle.Bold)
        AddHandler btnAceptar.Click, AddressOf BtnAceptar_Click

        btnCancelar.Text = "Cancelar"
        btnCancelar.Size = New Size(132, 34)
        btnCancelar.Location = New Point(154, btnY)
        btnCancelar.FlatStyle = FlatStyle.Flat
        btnCancelar.FlatAppearance.BorderSize = 0
        btnCancelar.BackColor = System.Drawing.Color.FromArgb(87, 87, 87)
        btnCancelar.ForeColor = System.Drawing.Color.White
        btnCancelar.Font = New Font("SansSerif", 11, FontStyle.Bold)
        AddHandler btnCancelar.Click, Sub() Me.DialogResult = DialogResult.Cancel

        Me.Controls.AddRange({lblHnTotal, btnGenerar, dgv, btnAceptar, btnCancelar})
    End Sub

    Private Sub CargarAlturas(alturasPrevias As List(Of Single))
        dgv.Rows.Clear()
        For i = 1 To NPisos
            ' Reutiliza la altura previa si existe para ese piso; si no, usa Hx típica.
            Dim hw As Single
            If alturasPrevias IsNot Nothing AndAlso i <= alturasPrevias.Count Then
                hw = alturasPrevias(i - 1)
            Else
                hw = HxTipico
            End If
            dgv.Rows.Add($"Piso {i}", Format(hw, "0.000"))
        Next
        ActualizarHnTotal()
    End Sub

    Private Sub BtnGenerar_Click(sender As Object, e As EventArgs)
        For i = 0 To dgv.Rows.Count - 1
            dgv.Rows(i).Cells("ColHw").Value = Format(HxTipico, "0.000")
        Next
        ActualizarHnTotal()
    End Sub

    Private Sub Dgv_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        ActualizarHnTotal()
    End Sub

    Private Sub ActualizarHnTotal()
        Dim total As Single = 0
        For i = 0 To dgv.Rows.Count - 1
            Dim cell = dgv.Rows(i).Cells("ColHw").Value
            If cell IsNot Nothing Then
                Dim v As Single
                If Single.TryParse(cell.ToString(),
                                   Globalization.NumberStyles.Float,
                                   Globalization.CultureInfo.InvariantCulture, v) OrElse
                   Single.TryParse(cell.ToString(), v) Then
                    total += v
                End If
            End If
        Next
        lblHnTotal.Text = $"Hn total: {Format(total, "0.00")} m"
    End Sub

    Private Function LeerAlturas() As List(Of Single)
        Dim lst As New List(Of Single)
        For i = 0 To dgv.Rows.Count - 1
            Dim cell = dgv.Rows(i).Cells("ColHw").Value
            Dim v As Single = HxTipico
            If cell IsNot Nothing Then
                If Not (Single.TryParse(cell.ToString(),
                                        Globalization.NumberStyles.Float,
                                        Globalization.CultureInfo.InvariantCulture, v) OrElse
                        Single.TryParse(cell.ToString(), v)) Then
                    v = HxTipico
                End If
            End If
            If v <= 0 Then v = HxTipico
            lst.Add(v)
        Next
        Return lst
    End Function

    Private Sub BtnAceptar_Click(sender As Object, e As EventArgs)
        AlturasSalida = LeerAlturas()
        Me.DialogResult = DialogResult.OK
    End Sub

End Class
