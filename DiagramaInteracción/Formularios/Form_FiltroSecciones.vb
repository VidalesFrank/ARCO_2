''' <summary>
''' Diálogo para seleccionar qué tipos de sección se incluyen en el análisis de vigas.
''' El usuario puede incluir/excluir secciones (ej: nervios, vigas de borde, secciones especiales).
''' </summary>
Public Class Form_FiltroSecciones
    Inherits Form

    ' Resultado: secciones marcadas para incluir en el análisis
    Public Property SeccionesSeleccionadas As List(Of String)

    Private _seccionesInfo As List(Of InfoSeccion)
    Private _clb As CheckedListBox
    Private _lblConteo As Label

    Private Structure InfoSeccion
        Public Property Nombre As String
        Public Property Descripcion As String    ' e.g. "0.30 × 0.50 m"
        Public Property CantFrames As Integer
        Public Sub New(n As String, desc As String, cnt As Integer)
            Nombre = n : Descripcion = desc : CantFrames = cnt
        End Sub
        Public Overrides Function ToString() As String
            Return $"{Nombre}   ({Descripcion})   — {CantFrames} frame(s)"
        End Function
    End Structure

    ' ──────────────────────────────────────────────────────────────────────────
    ' Método de entrada
    ' ──────────────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Muestra el diálogo. Devuelve True si el usuario confirmó la selección.
    ''' </summary>
    Public Shared Function Mostrar(frames As List(Of cFrame),
                                   seleccionActual As List(Of String),
                                   ByRef resultado As List(Of String)) As Boolean
        Using frm As New Form_FiltroSecciones(frames, seleccionActual)
            If frm.ShowDialog() = DialogResult.OK Then
                resultado = frm.SeccionesSeleccionadas
                Return True
            End If
            Return False
        End Using
    End Function

    Public Sub New(frames As List(Of cFrame), seleccionActual As List(Of String))
        _seccionesInfo = ExtraerSecciones(frames)
        SeccionesSeleccionadas = If(seleccionActual, New List(Of String))
        BuildUI()
        CargarSecciones()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Filtro de tipos de sección — Vigas"
        Me.Size = New Size(580, 460)
        Me.MinimumSize = New Size(480, 380)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 245)

        ' Panel superior
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 52, .BackColor = Color.FromArgb(50, 75, 115)}
        Dim lblT As New Label With {
            .Text = "  Seleccione las secciones que entrarán al análisis de vigas",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblT)

        ' Nota informativa
        Dim panelInfo As New Panel With {.Dock = DockStyle.Top, .Height = 34, .BackColor = Color.FromArgb(255, 243, 205)}
        Dim lblInfo As New Label With {
            .Text = "  Tip: Desmarca nervios u otras secciones que no sean vigas principales.",
            .ForeColor = Color.FromArgb(102, 77, 3),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 8.5)
        }
        panelInfo.Controls.Add(lblInfo)

        ' Botones de selección masiva
        Dim panelBtnSel As New Panel With {.Dock = DockStyle.Top, .Height = 36, .BackColor = Color.FromArgb(240, 240, 240)}
        Dim btnTodos As New Button With {.Text = "Seleccionar todo", .Size = New Size(130, 24), .Location = New Point(8, 6)}
        Dim btnNinguno As New Button With {.Text = "Quitar selección", .Size = New Size(130, 24), .Location = New Point(146, 6)}
        _lblConteo = New Label With {.Text = "", .Location = New Point(290, 9), .AutoSize = True, .ForeColor = Color.FromArgb(50, 75, 115)}
        AddHandler btnTodos.Click, Sub(s, e)
                                       For i = 0 To _clb.Items.Count - 1
                                           _clb.SetItemChecked(i, True)
                                       Next
                                       ActualizarConteo()
                                   End Sub
        AddHandler btnNinguno.Click, Sub(s, e)
                                         For i = 0 To _clb.Items.Count - 1
                                             _clb.SetItemChecked(i, False)
                                         Next
                                         ActualizarConteo()
                                     End Sub
        panelBtnSel.Controls.AddRange({btnTodos, btnNinguno, _lblConteo})

        ' Lista de secciones
        _clb = New CheckedListBox With {
            .Dock = DockStyle.Fill,
            .CheckOnClick = True,
            .Font = New Font("Consolas", 9),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.None
        }
        AddHandler _clb.ItemCheck, Sub(s, e)
                                       If Me.IsHandleCreated Then
                                           Me.BeginInvoke(New Action(AddressOf ActualizarConteo))
                                       Else
                                           ActualizarConteo()
                                       End If
                                   End Sub

        ' Panel botones OK/Cancel
        Dim panelBot As New Panel With {.Dock = DockStyle.Bottom, .Height = 44, .BackColor = Color.FromArgb(240, 240, 240)}
        Dim btnOK As New Button With {.Text = "Aplicar", .Size = New Size(90, 28), .DialogResult = DialogResult.OK}
        Dim btnCancel As New Button With {.Text = "Cancelar", .Size = New Size(90, 28), .DialogResult = DialogResult.Cancel}
        btnOK.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        btnCancel.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        AddHandler panelBot.Resize, Sub(s, e)
                                        btnCancel.Location = New Point(panelBot.Width - 106, 8)
                                        btnOK.Location = New Point(panelBot.Width - 204, 8)
                                    End Sub
        AddHandler btnOK.Click, AddressOf OK_Click
        Me.AcceptButton = btnOK
        Me.CancelButton = btnCancel
        panelBot.Controls.AddRange({btnOK, btnCancel})

        Me.Controls.Add(_clb)
        Me.Controls.Add(panelBtnSel)
        Me.Controls.Add(panelInfo)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBot)
    End Sub

    Private Sub CargarSecciones()
        _clb.Items.Clear()
        For Each sec In _seccionesInfo
            Dim marcada As Boolean = SeccionesSeleccionadas.Count = 0 OrElse
                                     SeccionesSeleccionadas.Contains(sec.Nombre)
            _clb.Items.Add(sec, marcada)
        Next
        ActualizarConteo()
    End Sub

    Private Sub ActualizarConteo()
        Dim checked = _clb.CheckedItems.Count
        Dim total = _clb.Items.Count
        Dim frames = _clb.CheckedItems.Cast(Of InfoSeccion).Sum(Function(s) s.CantFrames)
        _lblConteo.Text = $"{checked}/{total} secciones  |  {frames} frames seleccionados"
    End Sub

    Private Sub OK_Click(sender As Object, e As EventArgs)
        SeccionesSeleccionadas = _clb.CheckedItems.Cast(Of InfoSeccion).Select(Function(s) s.Nombre).ToList()
    End Sub

    Private Shared Function ExtraerSecciones(frames As List(Of cFrame)) As List(Of InfoSeccion)
        Dim resultado As New List(Of InfoSeccion)
        If frames Is Nothing Then Return resultado

        Dim grupos = frames _
            .Where(Function(f) f.Section IsNot Nothing) _
            .GroupBy(Function(f) f.Section.Nombre) _
            .OrderBy(Function(g) g.Key)

        For Each g In grupos
            Dim f0 = g.First()
            Dim desc = $"{f0.Section.b * 100:0}×{f0.Section.h * 100:0} cm"
            resultado.Add(New InfoSeccion(g.Key, desc, g.Count()))
        Next
        Return resultado
    End Function

End Class
