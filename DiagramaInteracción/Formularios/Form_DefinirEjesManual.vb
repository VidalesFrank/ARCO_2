''' <summary>
''' Diálogo para definir manualmente los ejes/grillas del proyecto (GridID, dirección,
''' ordenada) cuando el Excel de ETABS no trae la hoja "Grid Lines"/"Grid Definitions -
''' Grid Lines", o para completar/corregir los ejes ya importados. El resultado se asigna
''' a Proyecto.Elementos.Grids.GridLines — es un dato de proyecto, reutilizable por
''' cualquier módulo (no exclusivo de Nervios).
''' </summary>
Public Class Form_DefinirEjesManual
    Inherits Form

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblInfo As New Label()
    Private ReadOnly btnAceptar As New Button()
    Private ReadOnly btnCancelar As New Button()

    Public Property GridsSalida As New List(Of cGridLine)

    Public Sub New(gridsExistentes As List(Of cGridLine))
        BuildUI()
        CargarGrids(gridsExistentes)
    End Sub

    ''' <summary>Muestra el diálogo. Devuelve True si el usuario confirmó con al menos un eje válido.</summary>
    Public Shared Function Mostrar(gridsExistentes As List(Of cGridLine), ByRef resultado As List(Of cGridLine)) As Boolean
        Using frm As New Form_DefinirEjesManual(gridsExistentes)
            If frm.ShowDialog() = DialogResult.OK Then
                resultado = frm.GridsSalida
                Return True
            End If
            Return False
        End Using
    End Function

    Private Sub BuildUI()
        Me.Text = "Definir ejes / grillas manualmente"
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.ClientSize = New Size(420, 480)
        Me.MinimumSize = New Size(380, 360)
        Me.BackColor = Color.FromArgb(240, 240, 240)
        Me.Font = New Font("Segoe UI", 9)

        ' Encabezado gris — mismo lenguaje visual que el resto de ARCO (Form_00_PaginaPrincipal)
        Dim panHead As New Panel() With {.Dock = DockStyle.Top, .Height = 44, .BackColor = Color.FromArgb(87, 87, 87)}
        Dim lblTitulo As New Label() With {
            .Text = "  Ejes / grillas del proyecto",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 10.5, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panHead.Controls.Add(lblTitulo)

        lblInfo.Text = "Ingrese cada eje: ID (ej. A, 1), dirección (X o Y) y su coordenada (m)."
        lblInfo.Dock = DockStyle.Top
        lblInfo.Height = 32
        lblInfo.Padding = New Padding(8, 8, 8, 0)
        lblInfo.ForeColor = Color.FromArgb(70, 70, 70)

        dgv.Dock = DockStyle.Fill
        dgv.AllowUserToAddRows = True
        dgv.AllowUserToDeleteRows = True
        dgv.RowHeadersVisible = False
        dgv.BorderStyle = BorderStyle.None
        dgv.BackgroundColor = Color.White
        dgv.GridColor = Color.FromArgb(210, 210, 210)
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect
        dgv.MultiSelect = False
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(87, 87, 87)
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8.5, FontStyle.Bold)
        dgv.EnableHeadersVisualStyles = False
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
        dgv.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

        Dim colId As New DataGridViewTextBoxColumn() With {.HeaderText = "Eje (ID)", .Name = "ColId", .FillWeight = 34}
        Dim colDir As New DataGridViewComboBoxColumn() With {.HeaderText = "Dirección", .Name = "ColDir", .FillWeight = 30}
        colDir.Items.AddRange("X", "Y")
        colDir.FlatStyle = FlatStyle.Flat
        Dim colOrd As New DataGridViewTextBoxColumn() With {.HeaderText = "Ordenada (m)", .Name = "ColOrd", .FillWeight = 36}
        colOrd.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        dgv.Columns.AddRange({colId, colDir, colOrd})

        Dim panBot As New Panel() With {.Dock = DockStyle.Bottom, .Height = 44, .BackColor = Color.FromArgb(240, 240, 240)}
        btnAceptar.Text = "Aceptar"
        btnAceptar.Size = New Size(110, 28)
        btnAceptar.FlatStyle = FlatStyle.Flat
        btnAceptar.FlatAppearance.BorderSize = 0
        btnAceptar.BackColor = Color.FromArgb(224, 224, 224)
        btnAceptar.ForeColor = Color.FromArgb(87, 87, 87)
        btnAceptar.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        AddHandler btnAceptar.Click, AddressOf BtnAceptar_Click

        btnCancelar.Text = "Cancelar"
        btnCancelar.Size = New Size(110, 28)
        btnCancelar.FlatStyle = FlatStyle.Flat
        btnCancelar.FlatAppearance.BorderSize = 0
        btnCancelar.BackColor = Color.FromArgb(210, 210, 210)
        AddHandler btnCancelar.Click, Sub() Me.DialogResult = DialogResult.Cancel

        AddHandler panBot.Resize, Sub(s, e)
                                       btnAceptar.Location = New Point(panBot.Width - 230, 8)
                                       btnCancelar.Location = New Point(panBot.Width - 116, 8)
                                   End Sub
        panBot.Controls.AddRange({btnAceptar, btnCancelar})

        Me.Controls.Add(dgv)
        Me.Controls.Add(panBot)
        Me.Controls.Add(lblInfo)
        Me.Controls.Add(panHead)
        Me.AcceptButton = btnAceptar
        Me.CancelButton = btnCancelar
    End Sub

    Private Sub CargarGrids(gridsExistentes As List(Of cGridLine))
        dgv.Rows.Clear()
        If gridsExistentes IsNot Nothing Then
            For Each g In gridsExistentes
                dgv.Rows.Add(g.GridID, g.Direction, g.Ordinate.ToString("F3", Globalization.CultureInfo.InvariantCulture))
            Next
        End If
    End Sub

    Private Sub BtnAceptar_Click(sender As Object, e As EventArgs)
        Dim lst As New List(Of cGridLine)
        For Each row As DataGridViewRow In dgv.Rows
            If row.IsNewRow Then Continue For
            Dim id = row.Cells("ColId").Value?.ToString().Trim()
            Dim dir = row.Cells("ColDir").Value?.ToString().Trim()
            Dim ordTxt = row.Cells("ColOrd").Value?.ToString()
            If String.IsNullOrWhiteSpace(id) OrElse String.IsNullOrWhiteSpace(dir) Then Continue For

            Dim ord As Double
            If Not Double.TryParse(ordTxt, Globalization.NumberStyles.Any,
                                   Globalization.CultureInfo.InvariantCulture, ord) Then Continue For

            lst.Add(New cGridLine With {
                .GridSystem = "Manual",
                .Direction = dir.Trim().ToUpperInvariant().Substring(0, 1),
                .GridID = id,
                .Visible = True,
                .BubbleLocation = "Start",
                .Ordinate = ord
            })
        Next

        If lst.Count = 0 Then
            MessageBox.Show("Ingrese al menos un eje válido (ID, dirección X/Y y ordenada).", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        GridsSalida = lst
        Me.DialogResult = DialogResult.OK
    End Sub

End Class
