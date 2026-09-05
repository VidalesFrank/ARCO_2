Imports System.Linq

''' Diálogo para crear un Grupo de Réplica de vigas.
''' Muestra la viga patrón, detecta automáticamente los pisos donde existen
''' los mismos frame labels y permite al usuario seleccionar en cuáles aplicar.
Public Class Form_09_GrupoReplica
    Inherits Form

    ' --- Entradas ---
    Public Patron As cViga
    Public TodasVigas As List(Of cViga)
    Public Compatibilidades As List(Of VigaService.CompatibilidadPiso)

    ' --- Salida ---
    Public GrupoCreado As GrupoReplicaViga = Nothing
    Public PisosSeleccionados As New List(Of String)()

    ' --- Controles ---
    Private txtNombreGrupo As TextBox
    Private lblPatron As Label
    Private lblFrames As Label
    Private dgvPisos As DataGridView
    Private lblSeleccionados As Label
    Private btnCrear As Button
    Private btnCancelar As Button

    Public Sub New(patron As cViga, todasVigas As List(Of cViga),
                   compatibilidades As List(Of VigaService.CompatibilidadPiso))
        Me.Patron = patron
        Me.TodasVigas = todasVigas
        Me.Compatibilidades = compatibilidades
        InitializeUI()
        PopulatePisos()
    End Sub

    Private Sub InitializeUI()
        Me.SuspendLayout()

        Me.Text = "Crear Grupo de Réplica"
        Me.Size = New Size(600, 540)
        Me.MinimumSize = New Size(520, 460)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.FromArgb(45, 45, 48)
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 9)

        Dim COLOR_PANEL = Color.FromArgb(37, 37, 38)
        Dim COLOR_BTN = Color.FromArgb(87, 87, 87)

        ' ═══════════════════════════════════════════════════════════════════════
        ' ESTRATEGIA DE LAYOUT:
        '   Nivel formulario: panelBot [Bottom, 64px] + tlp [Fill]
        '   TableLayoutPanel: fila 0 [138px absolutos] = panelTop (encabezado)
        '                     fila 1 [Fill 100%]       = dgvPisos (tabla)
        '
        ' TableLayoutPanel garantiza el reparto exacto sin depender del orden
        ' de procesamiento de DockStyle del formulario, que con tres paneles
        ' (Bottom+Top+Fill) calcula incorrectamente la posición del Fill.
        ' ═══════════════════════════════════════════════════════════════════════

        ' ── 1. PANEL INFERIOR (botones) ──────────────────────────────────────
        Dim panelBot As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 64,
            .BackColor = COLOR_PANEL
        }

        lblSeleccionados = New Label With {
            .Text = "0 pisos seleccionados",
            .Location = New Point(12, 20),
            .AutoSize = True,
            .ForeColor = Color.FromArgb(200, 200, 200)
        }
        panelBot.Controls.Add(lblSeleccionados)

        btnCancelar = New Button With {
            .Text = "Cancelar",
            .Size = New Size(100, 34),
            .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
            .Location = New Point(panelBot.Width - 226, 15),
            .BackColor = COLOR_BTN,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel
        }
        btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100)
        panelBot.Controls.Add(btnCancelar)

        btnCrear = New Button With {
            .Text = "Crear Grupo",
            .Size = New Size(110, 34),
            .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
            .Location = New Point(panelBot.Width - 120, 15),
            .BackColor = Color.FromArgb(0, 120, 215),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        btnCrear.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 190)
        panelBot.Controls.Add(btnCrear)

        Me.CancelButton = btnCancelar

        ' ── 2. PANEL ENCABEZADO (fila 0 del TLP) ────────────────────────────
        ' Dock = Fill dentro de su celda TLP; altura controlada por RowStyle.
        Dim panelTop As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = COLOR_PANEL
        }

        Dim lblTit As New Label With {
            .Text = "Viga Patrón",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(0, 170, 255),
            .Location = New Point(12, 10),
            .AutoSize = True
        }
        panelTop.Controls.Add(lblTit)

        lblPatron = New Label With {
            .Text = "",
            .Location = New Point(12, 30),
            .Size = New Size(560, 20),
            .ForeColor = Color.White
        }
        panelTop.Controls.Add(lblPatron)

        lblFrames = New Label With {
            .Text = "",
            .Location = New Point(12, 52),
            .Size = New Size(560, 36),
            .ForeColor = Color.FromArgb(200, 200, 200)
        }
        panelTop.Controls.Add(lblFrames)

        Dim lblNomGrupo As New Label With {
            .Text = "Nombre del grupo:",
            .Location = New Point(12, 92),
            .AutoSize = True,
            .ForeColor = Color.White
        }
        panelTop.Controls.Add(lblNomGrupo)

        txtNombreGrupo = New TextBox With {
            .Location = New Point(140, 89),
            .Width = 280,
            .BackColor = Color.FromArgb(60, 60, 60),
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }
        panelTop.Controls.Add(txtNombreGrupo)

        Dim lblPisosTit As New Label With {
            .Text = "Pisos donde existen los mismos frames — marca los que quieres incluir:",
            .Location = New Point(0, 116),
            .Size = New Size(600, 22),
            .Padding = New Padding(12, 3, 0, 0),
            .ForeColor = Color.FromArgb(200, 200, 200),
            .Font = New Font("Segoe UI", 8.5),
            .BackColor = Color.FromArgb(37, 37, 38),
            .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
        }
        panelTop.Controls.Add(lblPisosTit)

        ' ── 3. DATAGRIDVIEW (fila 1 del TLP) ────────────────────────────────
        dgvPisos = New DataGridView With {
            .Dock = DockStyle.Fill,
            .BackgroundColor = COLOR_PANEL,
            .GridColor = Color.FromArgb(70, 70, 70),
            .BorderStyle = BorderStyle.None,
            .RowHeadersVisible = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
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

        Dim colCheck As New DataGridViewCheckBoxColumn With {
            .HeaderText = "✓",
            .Width = 38,
            .MinimumWidth = 38,
            .Resizable = DataGridViewTriState.False
        }
        Dim colPiso As New DataGridViewTextBoxColumn With {
            .HeaderText = "Piso",
            .Width = 130,
            .ReadOnly = True
        }
        Dim colEstado As New DataGridViewTextBoxColumn With {
            .HeaderText = "Estado",
            .Width = 170,
            .ReadOnly = True
        }
        Dim colDetalle As New DataGridViewTextBoxColumn With {
            .HeaderText = "Detalle",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .ReadOnly = True
        }
        dgvPisos.Columns.AddRange(colCheck, colPiso, colEstado, colDetalle)

        ' ── 4. TABLE LAYOUT PANEL ────────────────────────────────────────────
        ' Divide el espacio disponible en dos filas sin ambigüedad:
        '   fila 0 = 138 px fijos  → panelTop
        '   fila 1 = resto (100 %) → dgvPisos
        Dim tlp As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = Color.FromArgb(45, 45, 48),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Padding = New Padding(0)
        }
        tlp.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        tlp.RowStyles.Add(New RowStyle(SizeType.Absolute, 138))  ' encabezado
        tlp.RowStyles.Add(New RowStyle(SizeType.Percent, 100))   ' tabla

        tlp.Controls.Add(panelTop, 0, 0)
        tlp.Controls.Add(dgvPisos, 0, 1)

        ' ── 5. AGREGAR AL FORMULARIO ─────────────────────────────────────────
        ' Solo dos controles al nivel del formulario: Bottom + Fill.
        ' No hay ambigüedad de procesamiento de DockStyle.
        Me.Controls.Add(panelBot)  ' Bottom: reserva 64 px inferiores
        Me.Controls.Add(tlp)       ' Fill:   ocupa todo lo restante

        ' Handlers
        AddHandler btnCrear.Click, AddressOf BtnCrear_Click
        AddHandler dgvPisos.CellValueChanged, AddressOf DgvPisos_CellValueChanged
        AddHandler dgvPisos.CurrentCellDirtyStateChanged, AddressOf DgvPisos_DirtyStateChanged
        AddHandler Me.Resize, AddressOf Form_Resize

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub PopulatePisos()
        Dim labels = String.Join(", ", Patron.Frames.Select(Function(f) f.ObjectLabel))
        lblPatron.Text = $"{Patron.NombreDisplay}   —   Piso: {Patron.Piso}"
        lblFrames.Text = $"Frames ({Patron.Frames.Count}): {labels}"

        Dim displayBase As String = Patron.NombreDisplay
        If displayBase.StartsWith("[") Then
            Dim idx = displayBase.IndexOf("]")
            If idx >= 0 Then displayBase = displayBase.Substring(idx + 1).Trim()
        End If
        txtNombreGrupo.Text = displayBase

        dgvPisos.Rows.Clear()
        For Each c In Compatibilidades
            Dim i = dgvPisos.Rows.Add()
            Dim row = dgvPisos.Rows(i)
            row.Cells(0).Value = c.EsCompatible
            row.Cells(1).Value = c.Piso
            row.Cells(2).Value = If(c.EsCompatible, "Compatible ✓", "Incompleto ✗")
            row.Cells(3).Value = c.Resumen

            If Not c.EsCompatible Then
                row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 100, 100)
                row.Cells(0).Value = False
            End If
        Next

        ActualizarResumen()
    End Sub

    Private Sub DgvPisos_DirtyStateChanged(sender As Object, e As EventArgs)
        If dgvPisos.IsCurrentCellDirty Then
            dgvPisos.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub DgvPisos_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        If e.ColumnIndex = 0 Then ActualizarResumen()
    End Sub

    Private Sub ActualizarResumen()
        Dim n = dgvPisos.Rows.Cast(Of DataGridViewRow)() _
                    .Count(Function(r) CBool(r.Cells(0).Value) = True)
        lblSeleccionados.Text = $"{n} piso{If(n = 1, "", "s")} seleccionado{If(n = 1, "", "s")}"
        btnCrear.Enabled = (n > 0)
    End Sub

    Private Sub BtnCrear_Click(sender As Object, e As EventArgs)
        Dim nombre = txtNombreGrupo.Text.Trim()
        If String.IsNullOrWhiteSpace(nombre) Then
            MessageBox.Show("Ingresa un nombre para el grupo.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        PisosSeleccionados.Clear()
        For Each row As DataGridViewRow In dgvPisos.Rows
            If CBool(row.Cells(0).Value) = True Then
                PisosSeleccionados.Add(row.Cells(1).Value.ToString())
            End If
        Next

        If PisosSeleccionados.Count = 0 Then
            MessageBox.Show("Selecciona al menos un piso similar.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        GrupoCreado = New GrupoReplicaViga With {
            .NombreGrupo = nombre,
            .Piso_Patron = Patron.Piso,
            .Nombre_Patron = Patron.Name_Beam
        }
        GrupoCreado.Labels_Patron.AddRange(Patron.Frames.Select(Function(f) f.ObjectLabel))
        For Each piso In PisosSeleccionados
            GrupoCreado.Similares.Add(New MiembroGrupoViga With {.Piso = piso})
        Next

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Form_Resize(sender As Object, e As EventArgs)
        If btnCrear Is Nothing OrElse btnCancelar Is Nothing Then Return
        Dim panelBot = TryCast(btnCrear.Parent, Panel)
        If panelBot Is Nothing Then Return
        btnCrear.Left = panelBot.Width - 120
        btnCancelar.Left = panelBot.Width - 226
    End Sub

End Class
