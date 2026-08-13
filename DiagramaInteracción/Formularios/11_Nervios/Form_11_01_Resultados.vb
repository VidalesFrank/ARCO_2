Imports ARCO.Funciones_00_Varias

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_11_01_Resultados — Tabla de resultados Módulo 11 Nervios
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_11_01_Resultados
    Inherits Form

    Public Shared Proyecto As Proyecto

    Private WithEvents Tabla As New DataGridView()
    Private WithEvents BtnExportar As New Button()
    Private WithEvents BtnFiltrar As New Button()
    Private WithEvents ChkSoloFallan As New CheckBox()
    Private WithEvents CmbPiso As New ComboBox()

    ' Índices de columnas
    Private Const COL_NERVIO = 0
    Private Const COL_PISO = 1
    Private Const COL_FRAME = 2
    Private Const COL_SEC = 3
    Private Const COL_BW = 4
    Private Const COL_H = 5
    Private Const COL_BEF = 6
    Private Const COL_MUI = 7
    Private Const COL_MUC = 8
    Private Const COL_MUD = 9
    Private Const COL_VUI = 10
    Private Const COL_VUD = 11
    Private Const COL_ASMIN = 12
    Private Const COL_ASSUP = 13
    Private Const COL_ASINF = 14
    Private Const COL_CDI = 15
    Private Const COL_CDC = 16
    Private Const COL_CDD = 17
    Private Const COL_CDVI = 18
    Private Const COL_CDVD = 19
    Private Const COL_CUMPLE = 20

    Public Sub New()
        InitUI()
    End Sub

    Private Sub InitUI()
        Me.Text = "Módulo 11 — Resultados Nervios"
        Me.Size = New System.Drawing.Size(1400, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New System.Drawing.Font("Segoe UI", 9)

        ' Panel superior de filtros
        Dim panFiltros As New Panel() With {.Dock = DockStyle.Top, .Height = 44, .Padding = New Padding(6)}

        Dim lblPiso As New Label() With {.Text = "Piso:", .Location = New Point(6, 12), .AutoSize = True}
        CmbPiso.Location = New Point(40, 8)
        CmbPiso.Size = New Size(160, 22)
        CmbPiso.DropDownStyle = ComboBoxStyle.DropDownList
        CmbPiso.Items.Add("Todos")
        CmbPiso.SelectedIndex = 0

        ChkSoloFallan.Text = "Solo tramos que NO cumplen"
        ChkSoloFallan.Location = New Point(210, 12)
        ChkSoloFallan.AutoSize = True

        BtnFiltrar.Text = "Aplicar filtro"
        BtnFiltrar.Location = New Point(430, 8)
        BtnFiltrar.Size = New Size(120, 26)

        BtnExportar.Text = "Exportar Excel"
        BtnExportar.Location = New Point(560, 8)
        BtnExportar.Size = New Size(120, 26)
        BtnExportar.BackColor = Drawing.Color.FromArgb(0, 120, 215)
        BtnExportar.ForeColor = Drawing.Color.White

        panFiltros.Controls.AddRange(New Control() {lblPiso, CmbPiso, ChkSoloFallan, BtnFiltrar, BtnExportar})

        ' Tabla
        Tabla.Dock = DockStyle.Fill
        Tabla.ReadOnly = True
        Tabla.AllowUserToAddRows = False
        Tabla.AllowUserToDeleteRows = False
        Tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        Tabla.RowHeadersVisible = False
        Tabla.EnableHeadersVisualStyles = False
        Tabla.ColumnHeadersDefaultCellStyle.BackColor = Drawing.Color.FromArgb(30, 60, 100)
        Tabla.ColumnHeadersDefaultCellStyle.ForeColor = Drawing.Color.White
        Tabla.ColumnHeadersDefaultCellStyle.Font = New Drawing.Font("Segoe UI", 8.5F, Drawing.FontStyle.Bold)
        Tabla.AlternatingRowsDefaultCellStyle.BackColor = Drawing.Color.FromArgb(245, 248, 255)

        InicializarColumnas()

        Me.Controls.Add(Tabla)
        Me.Controls.Add(panFiltros)
    End Sub

    Private Sub InicializarColumnas()
        Dim cols() As String = {
            "Nervio", "Piso", "Frame", "Sección", "bw(m)", "h(m)", "be(m)",
            "Mu_neg_I", "Mu_pos_C", "Mu_neg_D",
            "Vu_I(kN)", "Vu_D(kN)",
            "As_min(cm²)", "As_sup(cm²)", "As_inf(cm²)",
            "C/D FI", "C/D FC", "C/D FD", "C/D VI", "C/D VD", "Cumple"}

        Tabla.Columns.Clear()
        For Each col In cols
            Tabla.Columns.Add(col, col)
        Next

        ' Alineación numérica
        For i As Integer = COL_BW To COL_CDVD
            Tabla.Columns(i).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        Next
    End Sub

    Private Sub Form_11_01_Resultados_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Me.DesignMode Then Return
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ' Cargar pisos
        Dim pisos = Proyecto.Elementos.Nervios.Elementos _
            .Select(Function(n) n.Piso).Distinct().OrderBy(Function(p) p).ToList()
        _cargando = True
        CmbPiso.Items.Clear()
        CmbPiso.Items.Add("Todos")
        For Each p In pisos
            CmbPiso.Items.Add(p)
        Next
        CmbPiso.SelectedIndex = 0
        _cargando = False

        CargarTabla()
    End Sub

    Private _cargando As Boolean = False

    Private Sub CargarTabla()
        Tabla.Rows.Clear()

        Dim pisoFiltro = If(CmbPiso.SelectedItem?.ToString() = "Todos", "", CmbPiso.SelectedItem?.ToString())
        Dim soloFallan = ChkSoloFallan.Checked

        For Each nerv In Proyecto.Elementos.Nervios.Elementos
            If Not String.IsNullOrEmpty(pisoFiltro) AndAlso nerv.Piso <> pisoFiltro Then Continue For

            For Each fn In nerv.Frames
                If Not fn.Ref_Modificado Then Continue For
                If soloFallan AndAlso fn.Cumple Then Continue For

                Dim cdMin = Math.Min(fn.CD_Flex_Sup_I,
                           Math.Min(fn.CD_Flex_Inf_C,
                           Math.Min(fn.CD_Flex_Sup_D,
                           Math.Min(fn.CD_Cortante_I, fn.CD_Cortante_D))))

                Dim rowIdx = Tabla.Rows.Add(
                    nerv.ToString(), nerv.Piso, fn.ObjectLabel, fn.NombreSeccion,
                    fn.Bw.ToString("F3"), fn.H.ToString("F3"),
                    If(fn.EsSeccionT, fn.Be.ToString("F3"), "—"),
                    fn.Mu_Neg_I.ToString("F2"), fn.Mu_Pos_C.ToString("F2"), fn.Mu_Neg_D.ToString("F2"),
                    fn.Vu_I.ToString("F2"), fn.Vu_D.ToString("F2"),
                    fn.As_Min.ToString("F2"), fn.As_Prov_Sup.ToString("F2"), fn.As_Prov_Inf.ToString("F2"),
                    fn.CD_Flex_Sup_I.ToString("F2"), fn.CD_Flex_Inf_C.ToString("F2"),
                    fn.CD_Flex_Sup_D.ToString("F2"), fn.CD_Cortante_I.ToString("F2"),
                    fn.CD_Cortante_D.ToString("F2"),
                    If(fn.Cumple, "✓", "✗"))

                Dim row = Tabla.Rows(rowIdx)

                ' Colorear columnas C/D
                For Each colIdx In {COL_CDI, COL_CDC, COL_CDD, COL_CDVI, COL_CDVD}
                    Dim val As Double = 0
                    Double.TryParse(row.Cells(colIdx).Value?.ToString(), val)
                    row.Cells(colIdx).Style.ForeColor = If(val >= 1.0, Drawing.Color.DarkGreen,
                                                          If(val >= 0.9, Drawing.Color.DarkOrange,
                                                                         Drawing.Color.Red))
                    row.Cells(colIdx).Style.Font = New Drawing.Font("Segoe UI", 8.5F, Drawing.FontStyle.Bold)
                Next

                ' Fila completa si no cumple
                If Not fn.Cumple Then
                    row.DefaultCellStyle.BackColor = Drawing.Color.FromArgb(255, 235, 235)
                End If
                row.Cells(COL_CUMPLE).Style.ForeColor = If(fn.Cumple, Drawing.Color.DarkGreen, Drawing.Color.Red)
                row.Cells(COL_CUMPLE).Style.Font = New Drawing.Font("Segoe UI", 9, Drawing.FontStyle.Bold)
            Next
        Next
    End Sub

    Private Sub BtnFiltrar_Click(sender As Object, e As EventArgs) Handles BtnFiltrar.Click
        CargarTabla()
    End Sub

    Private Sub ChkSoloFallan_CheckedChanged(sender As Object, e As EventArgs) Handles ChkSoloFallan.CheckedChanged
        CargarTabla()
    End Sub

    Private Sub CmbPiso_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CmbPiso.SelectedIndexChanged
        If _cargando Then Return
        CargarTabla()
    End Sub

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs) Handles BtnExportar.Click
        Try
            Using dlg As New SaveFileDialog()
                dlg.Filter = "Excel (*.xlsx)|*.xlsx"
                dlg.FileName = "Resultados_Nervios.xlsx"
                If dlg.ShowDialog() <> DialogResult.OK Then Return

                Using wb As New ClosedXML.Excel.XLWorkbook()
                    Dim ws = wb.Worksheets.Add("Nervios")
                    ' Encabezados
                    For c As Integer = 0 To Tabla.Columns.Count - 1
                        ws.Cell(1, c + 1).Value = Tabla.Columns(c).HeaderText
                        ws.Cell(1, c + 1).Style.Font.Bold = True
                        ws.Cell(1, c + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(30, 60, 100)
                        ws.Cell(1, c + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White
                    Next
                    ' Filas
                    For r As Integer = 0 To Tabla.Rows.Count - 1
                        For c As Integer = 0 To Tabla.Columns.Count - 1
                            ws.Cell(r + 2, c + 1).Value = Tabla.Rows(r).Cells(c).Value?.ToString()
                        Next
                    Next
                    ws.Columns().AdjustToContents()
                    wb.SaveAs(dlg.FileName)
                End Using

                MessageBox.Show($"Exportado: {dlg.FileName}", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Using
        Catch ex As Exception
            Logger.Error(ex, "Form_11_01_Resultados.BtnExportar_Click")
            MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
