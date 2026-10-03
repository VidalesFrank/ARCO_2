''' <summary>
''' Diálogo que muestra los cambios de sección detectados al actualizar demandas
''' contra un nuevo Excel de ETABS. El usuario decide si aplicar los cambios,
''' actualizar solo las fuerzas, o cancelar la operación.
''' </summary>
Public Class Form_ConfirmarCambiosSeccion
    Inherits Form

    ''' Resultado de la interacción del usuario.
    Public Enum AccionActualizacion
        Cancelar = 0
        SoloFuerzas = 1
        AplicarTodo = 2
    End Enum

    Public Class CambioSeccion
        Public Property Piso As String
        Public Property Viga As String
        Public Property SeccionAntes As String
        Public Property SeccionDespues As String
        Public Property Detalle As String   ' descripción "b×h" antes → después
    End Class

    Public Property Accion As AccionActualizacion = AccionActualizacion.Cancelar

    Private ReadOnly _cambios As List(Of CambioSeccion)

    ' ──────────────────────────────────────────────────────────────────────────
    ' Método de entrada
    ' ──────────────────────────────────────────────────────────────────────────

    Public Shared Function Mostrar(cambios As List(Of CambioSeccion)) As AccionActualizacion
        Using frm As New Form_ConfirmarCambiosSeccion(cambios)
            frm.ShowDialog()
            Return frm.Accion
        End Using
    End Function

    Public Sub New(cambios As List(Of CambioSeccion))
        _cambios = If(cambios, New List(Of CambioSeccion))
        BuildUI()
        CargarGrid()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Cambios de sección detectados"
        Me.Size = New Size(720, 500)
        Me.MinimumSize = New Size(600, 380)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 245)
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Encabezado
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 52, .BackColor = Color.FromArgb(50, 75, 115)}
        Dim lblT As New Label With {
            .Text = $"  Se detectaron {_cambios.Count} cambio(s) de sección respecto al modelo actual",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblT)

        ' Nota informativa
        Dim panelInfo As New Panel With {.Dock = DockStyle.Top, .Height = 52, .BackColor = Color.FromArgb(255, 243, 205)}
        Dim lblInfo As New Label With {
            .Text = "  El refuerzo longitudinal y transversal se conserva tal como está." & vbCrLf &
                    "  Verifique el C/D después del recálculo — el prediseño de estribos usa d, que puede haber cambiado.",
            .ForeColor = Color.FromArgb(102, 77, 3),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 8.5)
        }
        panelInfo.Controls.Add(lblInfo)

        ' Grid de cambios
        Dim grid As New DataGridView With {
            .Dock = DockStyle.Fill,
            .Name = "GridCambios",
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            .Font = New Font("Consolas", 9)
        }
        grid.Columns.Add("Piso", "Piso")
        grid.Columns.Add("Viga", "Viga (Label)")
        grid.Columns.Add("Antes", "Sección actual")
        grid.Columns.Add("Despues", "Sección nueva")
        grid.Columns("Piso").FillWeight = 15
        grid.Columns("Viga").FillWeight = 20
        grid.Columns("Antes").FillWeight = 32
        grid.Columns("Despues").FillWeight = 33

        Dim headerStyle = grid.ColumnHeadersDefaultCellStyle
        headerStyle.BackColor = Color.FromArgb(50, 75, 115)
        headerStyle.ForeColor = Color.White
        headerStyle.Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold)
        grid.EnableHeadersVisualStyles = False

        ' Botones inferiores
        Dim panelBot As New Panel With {.Dock = DockStyle.Bottom, .Height = 52, .BackColor = Color.FromArgb(240, 240, 240)}
        Dim btnAplicar As New Button With {
            .Text = "Aplicar cambios + fuerzas",
            .Size = New Size(180, 30),
            .BackColor = Color.FromArgb(50, 130, 90),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI Semibold", 9)
        }
        Dim btnSoloFuerzas As New Button With {
            .Text = "Solo actualizar fuerzas",
            .Size = New Size(160, 30)
        }
        Dim btnCancelar As New Button With {
            .Text = "Cancelar",
            .Size = New Size(90, 30),
            .DialogResult = DialogResult.Cancel
        }
        btnAplicar.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        btnSoloFuerzas.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        btnCancelar.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom

        AddHandler panelBot.Resize, Sub(s, e)
                                        btnCancelar.Location = New Point(panelBot.Width - 106, 11)
                                        btnSoloFuerzas.Location = New Point(panelBot.Width - 274, 11)
                                        btnAplicar.Location = New Point(panelBot.Width - 462, 11)
                                    End Sub

        AddHandler btnAplicar.Click, Sub(s, e)
                                         Me.Accion = AccionActualizacion.AplicarTodo
                                         Me.DialogResult = DialogResult.OK
                                         Me.Close()
                                     End Sub
        AddHandler btnSoloFuerzas.Click, Sub(s, e)
                                             Me.Accion = AccionActualizacion.SoloFuerzas
                                             Me.DialogResult = DialogResult.OK
                                             Me.Close()
                                         End Sub
        AddHandler btnCancelar.Click, Sub(s, e)
                                          Me.Accion = AccionActualizacion.Cancelar
                                          Me.Close()
                                      End Sub

        Me.AcceptButton = btnAplicar
        Me.CancelButton = btnCancelar

        panelBot.Controls.AddRange({btnAplicar, btnSoloFuerzas, btnCancelar})

        Me.Controls.Add(grid)
        Me.Controls.Add(panelInfo)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBot)
    End Sub

    Private Sub CargarGrid()
        Dim grid As DataGridView = TryCast(Me.Controls.Find("GridCambios", True).FirstOrDefault(), DataGridView)
        If grid Is Nothing Then Return

        Dim ordenados = _cambios.OrderBy(Function(c) c.Piso).ThenBy(Function(c) c.Viga).ToList()
        For Each c In ordenados
            grid.Rows.Add(c.Piso, c.Viga, c.SeccionAntes, c.SeccionDespues)
        Next
    End Sub

End Class
