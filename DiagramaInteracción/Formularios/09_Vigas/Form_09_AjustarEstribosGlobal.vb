''' <summary>
''' Ajuste masivo de refuerzo transversal por SECCIÓN. Una fila por cada
''' sección distinta del edificio con el refuerzo que el usuario quiere
''' aplicar en ZC y ZNC. Al aplicar: guarda las preferencias en el
''' proyecto (se persisten con el .esm), asigna el refuerzo a TODAS las
''' vigas que compartan sección y recalcula el cortante de todo el edificio
''' (incluye cortante plástico si ya estaba calculado).
''' </summary>
Public Class Form_09_AjustarEstribosGlobal
    Inherits Form

    Private ReadOnly _proyecto As Proyecto
    Private ReadOnly _vigas As List(Of cViga)
    Private ReadOnly _svc As New VigaService(New GeometryService())
    Private _grid As DataGridView
    Private _secciones As List(Of cSeccion)

    Public Sub New(proyecto As Proyecto, vigas As List(Of cViga))
        _proyecto = proyecto
        _vigas = If(vigas, New List(Of cViga))
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Ajustar refuerzo transversal — por sección del modelo"
        Me.Size = New Size(1120, 620)
        Me.MinimumSize = New Size(900, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.BackColor = Color.FromArgb(245, 245, 248)

        Dim panelTop As New Panel() With {.Dock = DockStyle.Top, .Height = 56, .BackColor = Color.FromArgb(87, 87, 87)}
        panelTop.Controls.Add(New Label() With {
            .Text = "  Ajustar refuerzo transversal — por sección",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 12.0F, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft})

        Dim lblAyuda As New Label() With {
            .Dock = DockStyle.Top, .Height = 46,
            .Padding = New Padding(12, 8, 12, 8),
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(60, 60, 60),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
            .Text = "Separación en metros. Lo (confinamiento) = 0.05 + (N−1)·s en cada extremo." & vbCrLf &
                    "Al aplicar, se asigna el refuerzo a todas las vigas de cada sección y se recalcula el cortante del edificio."}

        _grid = New DataGridView() With {
            .Dock = DockStyle.Fill, .BackgroundColor = Color.White,
            .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.CellSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(230, 230, 232),
                .Font = New Font("Segoe UI Semibold", 9.0F),
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .WrapMode = DataGridViewTriState.True},
            .ColumnHeadersHeight = 48,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing}

        AgregarColumnas()
        LlenarFilas()

        Dim panelBtn As New Panel() With {.Dock = DockStyle.Bottom, .Height = 56, .BackColor = Color.FromArgb(240, 240, 240)}
        Dim btnAplicar As New Button() With {
            .Text = "Aplicar y recalcular",
            .Font = New Font("Segoe UI Semibold", 10.0F, FontStyle.Bold),
            .BackColor = Color.FromArgb(27, 175, 122), .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat, .Location = New Point(820, 11),
            .Size = New Size(190, 34), .Cursor = Cursors.Hand}
        btnAplicar.FlatAppearance.BorderSize = 0
        AddHandler btnAplicar.Click, AddressOf BtnAplicar_Click
        Dim btnCancelar As New Button() With {
            .Text = "Cancelar",
            .Font = New Font("Segoe UI", 10.0F),
            .BackColor = Color.FromArgb(230, 230, 230),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(720, 11), .Size = New Size(90, 34),
            .Cursor = Cursors.Hand}
        btnCancelar.FlatAppearance.BorderSize = 0
        AddHandler btnCancelar.Click, Sub() Me.Close()
        panelBtn.Controls.Add(btnAplicar)
        panelBtn.Controls.Add(btnCancelar)
        AddHandler panelBtn.Resize, Sub()
                                        btnAplicar.Location = New Point(panelBtn.ClientSize.Width - 200, 11)
                                        btnCancelar.Location = New Point(panelBtn.ClientSize.Width - 300, 11)
                                    End Sub

        Me.Controls.Add(_grid)
        Me.Controls.Add(lblAyuda)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBtn)
    End Sub

    Private Sub AgregarColumnas()
        Dim c As DataGridViewColumn

        c = New DataGridViewTextBoxColumn() With {.Name = "Seccion", .HeaderText = "Sección", .ReadOnly = True}
        c.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 250)
        _grid.Columns.Add(c)
        c = New DataGridViewTextBoxColumn() With {.Name = "B", .HeaderText = "b (m)", .ReadOnly = True}
        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        c.DefaultCellStyle.Format = "0.00"
        c.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 250)
        _grid.Columns.Add(c)
        c = New DataGridViewTextBoxColumn() With {.Name = "H", .HeaderText = "h (m)", .ReadOnly = True}
        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        c.DefaultCellStyle.Format = "0.00"
        c.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 250)
        _grid.Columns.Add(c)

        _grid.Columns.Add(ColComboBarra("BarraZC", "ZC — Ø (#)"))
        _grid.Columns.Add(ColEnteroText("RamasZC", "ZC — ramas"))
        _grid.Columns.Add(ColEnteroText("NumEstZC", "ZC — N° estribos"))
        _grid.Columns.Add(ColDecimalText("SepZC", "ZC — s (m)"))
        _grid.Columns.Add(ColComboBarra("BarraZNC", "ZNC — Ø (#)"))
        _grid.Columns.Add(ColEnteroText("RamasZNC", "ZNC — ramas"))
        _grid.Columns.Add(ColDecimalText("SepZNC", "ZNC — s (m)"))
    End Sub

    Private Function ColComboBarra(name As String, header As String) As DataGridViewComboBoxColumn
        Dim col As New DataGridViewComboBoxColumn() With {.Name = name, .HeaderText = header, .FlatStyle = FlatStyle.Flat}
        For Each b In {3, 4, 5}
            col.Items.Add(b)
        Next
        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Return col
    End Function

    Private Function ColComboEntero(name As String, header As String, minVal As Integer, maxVal As Integer) As DataGridViewComboBoxColumn
        Dim col As New DataGridViewComboBoxColumn() With {.Name = name, .HeaderText = header, .FlatStyle = FlatStyle.Flat}
        For i = minVal To maxVal
            col.Items.Add(i)
        Next
        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Return col
    End Function

    Private Function ColEnteroText(name As String, header As String) As DataGridViewTextBoxColumn
        Dim col As New DataGridViewTextBoxColumn() With {.Name = name, .HeaderText = header}
        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Return col
    End Function

    Private Function ColDecimalText(name As String, header As String) As DataGridViewTextBoxColumn
        Dim col As New DataGridViewTextBoxColumn() With {.Name = name, .HeaderText = header}
        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        col.DefaultCellStyle.Format = "0.000"
        Return col
    End Function

    Private Sub LlenarFilas()
        _secciones = SeccionesDistintas()
        Dim prefs = _proyecto.Elementos.Vigas.PreferenciasEstribos

        For Each sec In _secciones
            Dim p As PreferenciasEstribosSeccion = Nothing
            If prefs IsNot Nothing Then prefs.TryGetValue(sec.Nombre, p)
            If p Is Nothing Then p = PreferenciaDesdeAuto(sec)

            Dim row = _grid.Rows(_grid.Rows.Add())
            row.Cells("Seccion").Value = sec.Nombre
            row.Cells("B").Value = sec.b
            row.Cells("H").Value = sec.h
            row.Cells("BarraZC").Value = p.BarraZC
            row.Cells("RamasZC").Value = p.RamasZC
            row.Cells("NumEstZC").Value = p.NumEstribosZC
            row.Cells("SepZC").Value = p.SepZC
            row.Cells("BarraZNC").Value = p.BarraZNC
            row.Cells("RamasZNC").Value = p.RamasZNC
            row.Cells("SepZNC").Value = p.SepZNC
        Next
    End Sub

    ''' <summary>
    ''' Secciones distintas presentes en el edificio (clave = sec.Nombre).
    ''' Toma la primera cSeccion encontrada por nombre; asume que b, h y materiales
    ''' coinciden entre vigas de la misma sección (invariante del modelo ETABS).
    ''' </summary>
    Private Function SeccionesDistintas() As List(Of cSeccion)
        Dim res As New Dictionary(Of String, cSeccion)(StringComparer.OrdinalIgnoreCase)
        For Each v In _vigas
            If v Is Nothing OrElse v.Frames Is Nothing Then Continue For
            For Each f In v.Frames
                If f Is Nothing OrElse f.Section Is Nothing Then Continue For
                If String.IsNullOrWhiteSpace(f.Section.Nombre) Then Continue For
                If Not res.ContainsKey(f.Section.Nombre) Then res(f.Section.Nombre) = f.Section
            Next
        Next
        Return res.Values.OrderBy(Function(s) s.Nombre).ToList()
    End Function

    ''' <summary>Default sensato cuando la sección no tiene preferencia aún.</summary>
    Private Function PreferenciaDesdeAuto(sec As cSeccion) As PreferenciasEstribosSeccion
        Dim ramasAuto = If(sec.b <= 0.45, 2, 3)
        Dim sepZC = Math.Round(sec.d / 4.0, 3)
        Dim nZC = Math.Max(CInt(Math.Ceiling(2.0 * sec.h / Math.Max(sepZC, 0.001))), 1)
        Dim sepZNC = Math.Round(sec.d / 2.0, 3)
        Return New PreferenciasEstribosSeccion With {
            .NombreSeccion = sec.Nombre,
            .BarraZC = 3, .RamasZC = ramasAuto,
            .NumEstribosZC = nZC, .SepZC = sepZC,
            .BarraZNC = 3, .RamasZNC = 2, .SepZNC = sepZNC}
    End Function

    Private Sub BtnAplicar_Click(sender As Object, e As EventArgs)
        Me.Cursor = Cursors.WaitCursor
        Try
            Dim nuevas As New Dictionary(Of String, PreferenciasEstribosSeccion)(StringComparer.OrdinalIgnoreCase)
            For Each row As DataGridViewRow In _grid.Rows
                If row.IsNewRow Then Continue For
                Dim nombre = TryCast(row.Cells("Seccion").Value, String)
                If String.IsNullOrWhiteSpace(nombre) Then Continue For

                Dim p As New PreferenciasEstribosSeccion With {
                    .NombreSeccion = nombre,
                    .BarraZC = CInt(row.Cells("BarraZC").Value),
                    .RamasZC = CInt(row.Cells("RamasZC").Value),
                    .NumEstribosZC = CInt(row.Cells("NumEstZC").Value),
                    .SepZC = CDbl(row.Cells("SepZC").Value),
                    .BarraZNC = CInt(row.Cells("BarraZNC").Value),
                    .RamasZNC = CInt(row.Cells("RamasZNC").Value),
                    .SepZNC = CDbl(row.Cells("SepZNC").Value)}

                If p.NumEstribosZC < 1 OrElse p.SepZC <= 0 OrElse p.SepZNC <= 0 OrElse
                   p.RamasZC < 2 OrElse p.RamasZNC < 2 Then
                    MessageBox.Show(Me, $"Valores inválidos en la fila '{nombre}'. Revise N° estribos, ramas y separaciones.",
                                    "Datos inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                nuevas(nombre) = p
            Next

            _proyecto.Elementos.Vigas.PreferenciasEstribos = nuevas

            _svc.AsignarRefuerzoDesdePreferencias(_vigas, nuevas, sobrescribir:=True)
            _svc.RecalcularCortanteVigas(_vigas)

            Me.Cursor = Cursors.Default
            MessageBox.Show(Me,
                $"Refuerzo transversal asignado a {_vigas.Count} vigas en {nuevas.Count} secciones distintas." & vbCrLf &
                "Cortante del edificio recalculado.",
                "Ajuste global aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            Logger.Error(ex, "Form_09_AjustarEstribosGlobal.BtnAplicar")
            MessageBox.Show(Me, "Error al aplicar:" & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

End Class
