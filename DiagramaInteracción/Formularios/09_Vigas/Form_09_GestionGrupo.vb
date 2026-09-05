Imports System.Linq

''' Diálogo para gestionar los miembros de un Grupo de Réplica existente.
''' Muestra el patrón (no removible) y todos los similares con checkbox para
''' mantener o quitar cada uno del grupo.
Public Class Form_09_GestionGrupo
    Inherits Form

    ' --- Entradas ---
    Public Grupo As GrupoReplicaViga
    Public Patron As cViga
    Public Similares As List(Of cViga)

    ' --- Salida ---
    ''' Pisos de similares que el usuario marcó para quitar del grupo.
    Public PisosAEliminar As New List(Of String)()

    ' --- Controles ---
    Private dgvMiembros As DataGridView
    Private lblResumen As Label
    Private btnGuardar As Button
    Private btnCancelar As Button

    Public Sub New(grupo As GrupoReplicaViga, patron As cViga, similares As List(Of cViga))
        Me.Grupo = grupo
        Me.Patron = patron
        Me.Similares = similares
        InitializeUI()
        PopularMiembros()
    End Sub

    Private Sub InitializeUI()
        Me.SuspendLayout()

        Me.Text = "Gestionar Grupo de Réplica"
        Me.Size = New Size(560, 460)
        Me.MinimumSize = New Size(480, 360)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.FromArgb(45, 45, 48)
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 9)

        Dim COLOR_PANEL = Color.FromArgb(37, 37, 38)
        Dim COLOR_BTN = Color.FromArgb(87, 87, 87)

        ' ── PANEL INFERIOR (botones) ─────────────────────────────────────────
        Dim panelBot As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 64,
            .BackColor = COLOR_PANEL
        }

        lblResumen = New Label With {
            .Text = "",
            .Location = New Point(12, 20),
            .AutoSize = True,
            .ForeColor = Color.FromArgb(200, 200, 200)
        }
        panelBot.Controls.Add(lblResumen)

        btnCancelar = New Button With {
            .Text = "Cancelar",
            .Size = New Size(100, 34),
            .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
            .Location = New Point(panelBot.Width - 232, 15),
            .BackColor = COLOR_BTN,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel
        }
        btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100)
        panelBot.Controls.Add(btnCancelar)

        btnGuardar = New Button With {
            .Text = "Guardar cambios",
            .Size = New Size(126, 34),
            .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
            .Location = New Point(panelBot.Width - 126, 15),
            .BackColor = Color.FromArgb(0, 120, 215),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        btnGuardar.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 190)
        panelBot.Controls.Add(btnGuardar)

        Me.CancelButton = btnCancelar

        ' ── PANEL ENCABEZADO (fila 0 del TLP, 88 px) ────────────────────────
        Dim panelTop As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = COLOR_PANEL
        }

        Dim lblTit As New Label With {
            .Text = $"Grupo: {Grupo.NombreGrupo}",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = Color.FromArgb(0, 170, 255),
            .Location = New Point(12, 10),
            .AutoSize = True
        }
        panelTop.Controls.Add(lblTit)

        Dim lblPatronInfo As New Label With {
            .Text = $"Patrón: {If(Patron IsNot Nothing, Patron.NombreDisplay, Grupo.Nombre_Patron)}  —  Piso {Grupo.Piso_Patron}",
            .Location = New Point(12, 34),
            .Size = New Size(520, 20),
            .ForeColor = Color.FromArgb(200, 200, 200)
        }
        panelTop.Controls.Add(lblPatronInfo)

        Dim lblInstruccion As New Label With {
            .Text = "Desmarca los similares que deseas quitar del grupo:",
            .Location = New Point(0, 62),
            .Size = New Size(560, 22),
            .Padding = New Padding(12, 3, 0, 0),
            .ForeColor = Color.FromArgb(200, 200, 200),
            .Font = New Font("Segoe UI", 8.5),
            .BackColor = Color.FromArgb(37, 37, 38),
            .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        }
        panelTop.Controls.Add(lblInstruccion)

        ' ── DATAGRIDVIEW (fila 1 del TLP, Fill) ─────────────────────────────
        dgvMiembros = New DataGridView With {
            .Dock = DockStyle.Fill,
            .BackgroundColor = COLOR_PANEL,
            .GridColor = Color.FromArgb(70, 70, 70),
            .BorderStyle = BorderStyle.None,
            .RowHeadersVisible = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .DefaultCellStyle = New DataGridViewCellStyle With {
                .BackColor = COLOR_PANEL,
                .ForeColor = Color.White,
                .SelectionBackColor = Color.FromArgb(0, 100, 180),
                .SelectionForeColor = Color.White
            },
            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 8.5, FontStyle.Bold)
            },
            .EnableHeadersVisualStyles = False,
            .ColumnHeadersHeight = 26,
            .RowTemplate = New DataGridViewRow With {.Height = 22}
        }

        Dim colMantener As New DataGridViewCheckBoxColumn With {
            .HeaderText = "Mantener",
            .Width = 72,
            .MinimumWidth = 60,
            .Resizable = DataGridViewTriState.False
        }
        Dim colRol As New DataGridViewTextBoxColumn With {
            .HeaderText = "Rol",
            .Width = 90,
            .ReadOnly = True
        }
        Dim colPiso As New DataGridViewTextBoxColumn With {
            .HeaderText = "Piso",
            .Width = 100,
            .ReadOnly = True
        }
        Dim colNombre As New DataGridViewTextBoxColumn With {
            .HeaderText = "Nombre",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .ReadOnly = True
        }
        dgvMiembros.Columns.AddRange(colMantener, colRol, colPiso, colNombre)

        ' ── TABLE LAYOUT PANEL ───────────────────────────────────────────────
        ' Fila 0 = encabezado fijo (88 px), Fila 1 = tabla (Fill)
        Dim tlp As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.FromArgb(45, 45, 48),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Padding = New Padding(0)
        }
        tlp.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        tlp.RowStyles.Add(New RowStyle(SizeType.Absolute, 88))   ' encabezado
        tlp.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' tabla

        tlp.Controls.Add(panelTop, 0, 0)
        tlp.Controls.Add(dgvMiembros, 0, 1)

        ' Solo dos controles al nivel del formulario: Bottom + Fill
        Me.Controls.Add(panelBot)
        Me.Controls.Add(tlp)

        AddHandler btnGuardar.Click, AddressOf BtnGuardar_Click
        AddHandler dgvMiembros.CellValueChanged, AddressOf DgvMiembros_CellValueChanged
        AddHandler dgvMiembros.CurrentCellDirtyStateChanged, AddressOf DgvMiembros_DirtyChanged
        AddHandler Me.Resize, AddressOf Form_Resize

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub PopularMiembros()
        dgvMiembros.Rows.Clear()

        ' Fila del patrón — siempre marcada, no editable
        If Patron IsNot Nothing Then
            Dim i = dgvMiembros.Rows.Add()
            Dim row = dgvMiembros.Rows(i)
            row.Cells(0).Value = True
            row.Cells(0).ReadOnly = True
            row.Cells(1).Value = "Patrón"
            row.Cells(2).Value = Patron.Piso
            row.Cells(3).Value = Patron.NombreDisplay
            row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 170, 255)
        End If

        ' Filas de similares — checkbox editable
        For Each sim In Similares.OrderBy(Function(v) v.Piso)
            Dim i = dgvMiembros.Rows.Add()
            Dim row = dgvMiembros.Rows(i)
            row.Cells(0).Value = True
            row.Cells(1).Value = If(sim.RefuerzoDesincronizado, "Similar (!)", "Similar")
            row.Cells(2).Value = sim.Piso
            row.Cells(3).Value = sim.NombreDisplay
            If sim.RefuerzoDesincronizado Then
                row.DefaultCellStyle.ForeColor = Color.FromArgb(220, 160, 80)
            End If
        Next

        ActualizarResumen()
    End Sub

    Private Sub DgvMiembros_DirtyChanged(sender As Object, e As EventArgs)
        If dgvMiembros.IsCurrentCellDirty Then
            dgvMiembros.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub DgvMiembros_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        If e.ColumnIndex = 0 Then ActualizarResumen()
    End Sub

    Private Sub ActualizarResumen()
        Dim nTotal = Similares.Count
        Dim nBorrar = dgvMiembros.Rows.Cast(Of DataGridViewRow)() _
                          .Skip(1) _
                          .Count(Function(r) CBool(r.Cells(0).Value) = False)
        If nBorrar = 0 Then
            lblResumen.Text = $"{nTotal} similar{If(nTotal = 1, "", "es")} en el grupo"
        Else
            lblResumen.Text = $"Se quitarán {nBorrar} de {nTotal} similar{If(nTotal = 1, "", "es")}"
            lblResumen.ForeColor = Color.FromArgb(220, 140, 60)
        End If
        If nBorrar = 0 Then lblResumen.ForeColor = Color.FromArgb(200, 200, 200)
    End Sub

    Private Sub BtnGuardar_Click(sender As Object, e As EventArgs)
        PisosAEliminar.Clear()
        ' Las filas de similares empiezan en el índice 1 (índice 0 = patrón)
        For i As Integer = 1 To dgvMiembros.Rows.Count - 1
            If CBool(dgvMiembros.Rows(i).Cells(0).Value) = False Then
                PisosAEliminar.Add(dgvMiembros.Rows(i).Cells(2).Value.ToString())
            End If
        Next
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Form_Resize(sender As Object, e As EventArgs)
        If btnGuardar Is Nothing OrElse btnCancelar Is Nothing Then Return
        Dim pBot = TryCast(btnGuardar.Parent, Panel)
        If pBot Is Nothing Then Return
        btnGuardar.Left = pBot.Width - 126
        btnCancelar.Left = pBot.Width - 232
    End Sub

End Class
