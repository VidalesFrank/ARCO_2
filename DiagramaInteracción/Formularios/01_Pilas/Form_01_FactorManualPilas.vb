Public Class Form_01_FactorManualPilas
    Inherits Form

    Private WithEvents _dgv As New DataGridView()
    Private WithEvents _btnAceptar As New Button()
    Private WithEvents _btnLimpiar As New Button()
    Private WithEvents _btnCancelar As New Button()

    Private ReadOnly _colPila As Integer = 0
    Private ReadOnly _colFDiag As Integer = 1
    Private ReadOnly _colFCortes As Integer = 2
    Private ReadOnly _colManual As Integer = 3

    Public Sub New()
        Me.Text = "Factor Manual de Flexo-Compresión — Pilas"
        Me.Size = New Size(680, 500)
        Me.MinimumSize = New Size(560, 380)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        Dim lbl As New Label() With {
            .Text = "Ingrese un factor C/D manual para las pilas que lo requieran (0 = usar el calculado automáticamente).",
            .Dock = DockStyle.Top,
            .Height = 46,
            .Padding = New Padding(10, 8, 10, 4),
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.FromArgb(60, 60, 60)
        }

        _dgv.Dock = DockStyle.Fill
        _dgv.AllowUserToAddRows = False
        _dgv.RowHeadersVisible = False
        _dgv.BorderStyle = BorderStyle.None
        _dgv.GridColor = Color.FromArgb(220, 220, 220)
        _dgv.BackgroundColor = Color.White
        _dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        _dgv.Font = New Font("Segoe UI", 9.5F)

        Dim hdStyle As New DataGridViewCellStyle() With {
            .BackColor = Color.FromArgb(87, 87, 87),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        }
        _dgv.ColumnHeadersDefaultCellStyle = hdStyle
        _dgv.ColumnHeadersHeight = 34
        _dgv.EnableHeadersVisualStyles = False

        Dim cPila As New DataGridViewTextBoxColumn() With {.Name = "Pila", .HeaderText = "Pila", .Width = 140, .ReadOnly = True, .SortMode = DataGridViewColumnSortMode.NotSortable}
        Dim cDiag As New DataGridViewTextBoxColumn() With {.Name = "FDiag", .HeaderText = "F. Diagonal (calc.)", .Width = 145, .ReadOnly = True, .SortMode = DataGridViewColumnSortMode.NotSortable}
        Dim cCort As New DataGridViewTextBoxColumn() With {.Name = "FCortes", .HeaderText = "F. Cortes H (calc.)", .Width = 145, .ReadOnly = True, .SortMode = DataGridViewColumnSortMode.NotSortable}
        Dim cMan As New DataGridViewTextBoxColumn() With {.Name = "FManual", .HeaderText = "F. Manual (0=auto)", .Width = 145, .ReadOnly = False, .SortMode = DataGridViewColumnSortMode.NotSortable}
        cDiag.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        cCort.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        cMan.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        _dgv.Columns.Add(cPila)
        _dgv.Columns.Add(cDiag)
        _dgv.Columns.Add(cCort)
        _dgv.Columns.Add(cMan)

        Dim barra As New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 9, 10, 9)
        }

        _btnAceptar.Text = "Aceptar"
        _btnAceptar.Size = New Size(110, 36)
        _btnAceptar.Location = New Point(10, 9)
        _btnAceptar.FlatStyle = FlatStyle.Flat
        _btnAceptar.BackColor = Color.FromArgb(21, 130, 70)
        _btnAceptar.ForeColor = Color.White
        _btnAceptar.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _btnAceptar.FlatAppearance.BorderSize = 0
        _btnAceptar.Cursor = Cursors.Hand

        _btnLimpiar.Text = "Limpiar todo"
        _btnLimpiar.Size = New Size(130, 36)
        _btnLimpiar.Location = New Point(130, 9)
        _btnLimpiar.FlatStyle = FlatStyle.Flat
        _btnLimpiar.BackColor = Color.FromArgb(87, 87, 87)
        _btnLimpiar.ForeColor = Color.White
        _btnLimpiar.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _btnLimpiar.FlatAppearance.BorderSize = 0
        _btnLimpiar.Cursor = Cursors.Hand

        _btnCancelar.Text = "Cancelar"
        _btnCancelar.Size = New Size(110, 36)
        _btnCancelar.Location = New Point(270, 9)
        _btnCancelar.FlatStyle = FlatStyle.Flat
        _btnCancelar.BackColor = Color.FromArgb(170, 50, 50)
        _btnCancelar.ForeColor = Color.White
        _btnCancelar.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _btnCancelar.FlatAppearance.BorderSize = 0
        _btnCancelar.Cursor = Cursors.Hand

        barra.Controls.Add(_btnAceptar)
        barra.Controls.Add(_btnLimpiar)
        barra.Controls.Add(_btnCancelar)

        Me.Controls.Add(_dgv)
        Me.Controls.Add(lbl)
        Me.Controls.Add(barra)

        AddHandler Me.Load, AddressOf Frm_Load
    End Sub

    Private Sub Frm_Load(sender As Object, e As EventArgs)
        CargarDatos()
    End Sub

    Private Sub CargarDatos()
        _dgv.Rows.Clear()
        Dim pilas = Form_01_PagPilas.Proyecto.Elementos.Pilas.ListaElementos
        For Each p In pilas
            Dim r = _dgv.Rows.Add()
            _dgv.Rows(r).Cells(_colPila).Value = p.Name_Elemento
            _dgv.Rows(r).Cells(_colFDiag).Value = Math.Round(CDbl(p.Factor_Diagonal), 2)
            _dgv.Rows(r).Cells(_colFCortes).Value = Math.Round(CDbl(p.Factor_CortesH), 2)
            _dgv.Rows(r).Cells(_colManual).Value = If(p.Factor_Manual_DI > 0, CObj(Math.Round(CDbl(p.Factor_Manual_DI), 2)), CObj(""))
            ColorearFila(r, CDbl(p.Factor_Diagonal), CDbl(p.Factor_CortesH), p.Factor_Manual_DI)
        Next
    End Sub

    Private Sub ColorearFila(r As Integer, fDiag As Double, fCortes As Double, fManual As Single)
        Dim colorOK As Color = Color.FromArgb(198, 239, 206)
        Dim colorMal As Color = Color.FromArgb(255, 199, 206)
        Dim textoOK As Color = Color.FromArgb(0, 97, 0)
        Dim textoMal As Color = Color.FromArgb(156, 0, 6)

        ' F. Diagonal
        _dgv.Rows(r).Cells(_colFDiag).Style.BackColor = If(fDiag >= 0.9, colorOK, colorMal)
        _dgv.Rows(r).Cells(_colFDiag).Style.ForeColor = If(fDiag >= 0.9, textoOK, textoMal)
        ' F. Cortes H
        _dgv.Rows(r).Cells(_colFCortes).Style.BackColor = If(fCortes >= 0.9, colorOK, colorMal)
        _dgv.Rows(r).Cells(_colFCortes).Style.ForeColor = If(fCortes >= 0.9, textoOK, textoMal)
        ' F. Manual: fondo azul claro + negrita cuando hay valor ingresado
        If fManual > 0 Then
            _dgv.Rows(r).Cells(_colManual).Style.BackColor = Color.FromArgb(180, 215, 255)
            _dgv.Rows(r).Cells(_colManual).Style.ForeColor = Color.FromArgb(0, 50, 120)
            _dgv.Rows(r).Cells(_colManual).Style.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Else
            _dgv.Rows(r).Cells(_colManual).Style.BackColor = Color.White
            _dgv.Rows(r).Cells(_colManual).Style.ForeColor = Color.Black
            _dgv.Rows(r).Cells(_colManual).Style.Font = Nothing
        End If
    End Sub

    Private Sub _btnAceptar_Click(sender As Object, e As EventArgs) Handles _btnAceptar.Click
        Dim pilas = Form_01_PagPilas.Proyecto.Elementos.Pilas.ListaElementos
        For i = 0 To _dgv.Rows.Count - 1
            If i >= pilas.Count Then Exit For
            Dim celVal As Object = _dgv.Rows(i).Cells(_colManual).Value
            Dim txt As String = If(celVal IsNot Nothing, celVal.ToString().Trim(), "")
            Dim v As Single = 0
            If Not String.IsNullOrEmpty(txt) Then Single.TryParse(txt.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, v)
            pilas(i).Factor_Manual_DI = If(v > 0, v, 0)
        Next
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub _btnLimpiar_Click(sender As Object, e As EventArgs) Handles _btnLimpiar.Click
        For i = 0 To _dgv.Rows.Count - 1
            _dgv.Rows(i).Cells(_colManual).Value = ""
            _dgv.Rows(i).Cells(_colManual).Style.BackColor = Color.White
            _dgv.Rows(i).Cells(_colManual).Style.ForeColor = Color.Black
            _dgv.Rows(i).Cells(_colManual).Style.Font = Nothing
        Next
    End Sub

    Private Sub _btnCancelar_Click(sender As Object, e As EventArgs) Handles _btnCancelar.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub _dgv_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles _dgv.CellEndEdit
        If e.ColumnIndex <> _colManual OrElse e.RowIndex < 0 Then Return
        Dim pilas = Form_01_PagPilas.Proyecto.Elementos.Pilas.ListaElementos
        If e.RowIndex >= pilas.Count Then Return
        Dim p = pilas(e.RowIndex)
        Dim celVal As Object = _dgv.Rows(e.RowIndex).Cells(_colManual).Value
        Dim txt As String = If(celVal IsNot Nothing, celVal.ToString().Trim(), "")
        Dim v As Single = 0
        If Not String.IsNullOrEmpty(txt) Then Single.TryParse(txt.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, v)
        ColorearFila(e.RowIndex, CDbl(p.Factor_Diagonal), CDbl(p.Factor_CortesH), v)
    End Sub

End Class
