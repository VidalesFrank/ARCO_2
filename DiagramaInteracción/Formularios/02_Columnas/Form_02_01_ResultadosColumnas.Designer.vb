<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form_02_01_ResultadosColumnas
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_02_01_ResultadosColumnas))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Tabla_Resultados = New System.Windows.Forms.DataGridView()
        Me.Column1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column11 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column12 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column3 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column4 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column14 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column13 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column5 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column6 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column7 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column8 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column9 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column10 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Combo_Elementos = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem3 = New System.Windows.Forms.ToolStripMenuItem()
        Me.Reporte_Col_Excel = New System.Windows.Forms.ToolStripMenuItem()
        Me.Reporte_Col_PDF = New System.Windows.Forms.ToolStripMenuItem()
        Me.Resultados_Modelo_Col = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem2 = New System.Windows.Forms.ToolStripMenuItem()
        Me.Graficos_Resultados = New System.Windows.Forms.ToolStripMenuItem()
        Me.Cortante_Resultados = New System.Windows.Forms.ToolStripMenuItem()
        Me.VerToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CrearReporteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AExcelToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.IngresarResultadosDeModeloToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.MostrarToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.GráficosToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.RevisiónDeCortanteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Panel1.SuspendLayout()
        CType(Me.Tabla_Resultados, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.Gainsboro
        Me.Panel1.Controls.Add(Me.Button1)
        Me.Panel1.Controls.Add(Me.Tabla_Resultados)
        Me.Panel1.Controls.Add(Me.Label2)
        Me.Panel1.Controls.Add(Me.Combo_Elementos)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 31)
        Me.Panel1.Margin = New System.Windows.Forms.Padding(4)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1989, 880)
        Me.Panel1.TabIndex = 0
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button1.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.Button1.Font = New System.Drawing.Font("SansSerif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(417, 60)
        Me.Button1.Margin = New System.Windows.Forms.Padding(4)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(173, 31)
        Me.Button1.TabIndex = 9
        Me.Button1.Text = "Ver ""ALR"""
        Me.Button1.UseVisualStyleBackColor = False
        Me.Button1.Visible = False
        '
        'Tabla_Resultados
        '
        Me.Tabla_Resultados.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Tabla_Resultados.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.Tabla_Resultados.BackgroundColor = System.Drawing.Color.Gainsboro
        Me.Tabla_Resultados.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Arial", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.Tabla_Resultados.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle1
        Me.Tabla_Resultados.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.Tabla_Resultados.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Column1, Me.Column11, Me.Column12, Me.Column3, Me.Column2, Me.Column4, Me.Column14, Me.Column13, Me.Column5, Me.Column6, Me.Column7, Me.Column8, Me.Column9, Me.Column10})
        DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Arial", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Tabla_Resultados.DefaultCellStyle = DataGridViewCellStyle2
        Me.Tabla_Resultados.Location = New System.Drawing.Point(47, 98)
        Me.Tabla_Resultados.Margin = New System.Windows.Forms.Padding(4)
        Me.Tabla_Resultados.Name = "Tabla_Resultados"
        Me.Tabla_Resultados.RowHeadersVisible = False
        Me.Tabla_Resultados.RowHeadersWidth = 51
        Me.Tabla_Resultados.Size = New System.Drawing.Size(1903, 748)
        Me.Tabla_Resultados.TabIndex = 8
        '
        'Column1
        '
        Me.Column1.HeaderText = "Piso"
        Me.Column1.MinimumWidth = 6
        Me.Column1.Name = "Column1"
        '
        'Column11
        '
        Me.Column11.HeaderText = "Sección"
        Me.Column11.MinimumWidth = 6
        Me.Column11.Name = "Column11"
        '
        'Column12
        '
        Me.Column12.HeaderText = "Cuantia (%)"
        Me.Column12.MinimumWidth = 6
        Me.Column12.Name = "Column12"
        '
        'Column3
        '
        Me.Column3.HeaderText = "As Colocado (mm2)"
        Me.Column3.MinimumWidth = 6
        Me.Column3.Name = "Column3"
        '
        'Column2
        '
        Me.Column2.HeaderText = "As Requerido (mm2)"
        Me.Column2.MinimumWidth = 6
        Me.Column2.Name = "Column2"
        '
        'Column4
        '
        Me.Column4.HeaderText = "AsCol/AsReq"
        Me.Column4.MinimumWidth = 6
        Me.Column4.Name = "Column4"
        '
        'Column14
        '
        Me.Column14.HeaderText = "Cap/Dem (Modelo)"
        Me.Column14.MinimumWidth = 6
        Me.Column14.Name = "Column14"
        '
        'Column13
        '
        Me.Column13.HeaderText = "Dirección"
        Me.Column13.MinimumWidth = 6
        Me.Column13.Name = "Column13"
        '
        'Column5
        '
        Me.Column5.HeaderText = "Vn (kN)"
        Me.Column5.MinimumWidth = 6
        Me.Column5.Name = "Column5"
        '
        'Column6
        '
        Me.Column6.HeaderText = "Vu (kN)"
        Me.Column6.MinimumWidth = 6
        Me.Column6.Name = "Column6"
        '
        'Column7
        '
        Me.Column7.HeaderText = "Vn/Vu"
        Me.Column7.MinimumWidth = 6
        Me.Column7.Name = "Column7"
        '
        'Column8
        '
        Me.Column8.HeaderText = "AshColocado (mm2)"
        Me.Column8.MinimumWidth = 6
        Me.Column8.Name = "Column8"
        '
        'Column9
        '
        Me.Column9.HeaderText = "AshRequerido (mm2)"
        Me.Column9.MinimumWidth = 6
        Me.Column9.Name = "Column9"
        '
        'Column10
        '
        Me.Column10.HeaderText = "AshCol/AshReq"
        Me.Column10.MinimumWidth = 6
        Me.Column10.Name = "Column10"
        '
        'Label2
        '
        Me.Label2.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label2.Font = New System.Drawing.Font("SansSerif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer))
        Me.Label2.Location = New System.Drawing.Point(47, 60)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(156, 30)
        Me.Label2.TabIndex = 7
        Me.Label2.Text = "SECCIÓN"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Combo_Elementos
        '
        Me.Combo_Elementos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Combo_Elementos.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Combo_Elementos.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Combo_Elementos.FormattingEnabled = True
        Me.Combo_Elementos.Location = New System.Drawing.Point(213, 60)
        Me.Combo_Elementos.Margin = New System.Windows.Forms.Padding(4)
        Me.Combo_Elementos.Name = "Combo_Elementos"
        Me.Combo_Elementos.Size = New System.Drawing.Size(160, 28)
        Me.Combo_Elementos.TabIndex = 6
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label1.Font = New System.Drawing.Font("SansSerif", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(740, 18)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(352, 35)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "RESULTADOS FINALES"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.MenuStrip1.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.MenuStrip1.ImageScalingSize = New System.Drawing.Size(20, 20)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem1, Me.ToolStripMenuItem2})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(1989, 31)
        Me.MenuStrip1.TabIndex = 1
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem3, Me.Resultados_Modelo_Col})
        Me.ToolStripMenuItem1.ForeColor = System.Drawing.Color.White
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(95, 27)
        Me.ToolStripMenuItem1.Text = "Opciones"
        '
        'ToolStripMenuItem3
        '
        Me.ToolStripMenuItem3.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.ToolStripMenuItem3.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.Reporte_Col_Excel, Me.Reporte_Col_PDF})
        Me.ToolStripMenuItem3.ForeColor = System.Drawing.Color.White
        Me.ToolStripMenuItem3.Name = "ToolStripMenuItem3"
        Me.ToolStripMenuItem3.Size = New System.Drawing.Size(313, 28)
        Me.ToolStripMenuItem3.Text = "Generar Reportes"
        '
        'Reporte_Col_Excel
        '
        Me.Reporte_Col_Excel.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Reporte_Col_Excel.ForeColor = System.Drawing.Color.White
        Me.Reporte_Col_Excel.Name = "Reporte_Col_Excel"
        Me.Reporte_Col_Excel.Size = New System.Drawing.Size(148, 28)
        Me.Reporte_Col_Excel.Text = "A Excel"
        '
        'Reporte_Col_PDF
        '
        Me.Reporte_Col_PDF.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Reporte_Col_PDF.ForeColor = System.Drawing.Color.White
        Me.Reporte_Col_PDF.Name = "Reporte_Col_PDF"
        Me.Reporte_Col_PDF.Size = New System.Drawing.Size(148, 28)
        Me.Reporte_Col_PDF.Text = "A PDF"
        '
        'Resultados_Modelo_Col
        '
        Me.Resultados_Modelo_Col.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Resultados_Modelo_Col.ForeColor = System.Drawing.Color.White
        Me.Resultados_Modelo_Col.Name = "Resultados_Modelo_Col"
        Me.Resultados_Modelo_Col.Size = New System.Drawing.Size(313, 28)
        Me.Resultados_Modelo_Col.Text = "Ingresar Resultados de Etabs"
        '
        'ToolStripMenuItem2
        '
        Me.ToolStripMenuItem2.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.Graficos_Resultados, Me.Cortante_Resultados})
        Me.ToolStripMenuItem2.ForeColor = System.Drawing.Color.White
        Me.ToolStripMenuItem2.Name = "ToolStripMenuItem2"
        Me.ToolStripMenuItem2.Size = New System.Drawing.Size(83, 27)
        Me.ToolStripMenuItem2.Text = "Mostrar"
        '
        'Graficos_Resultados
        '
        Me.Graficos_Resultados.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Graficos_Resultados.ForeColor = System.Drawing.Color.White
        Me.Graficos_Resultados.Name = "Graficos_Resultados"
        Me.Graficos_Resultados.Size = New System.Drawing.Size(252, 28)
        Me.Graficos_Resultados.Text = "Gráficos"
        '
        'Cortante_Resultados
        '
        Me.Cortante_Resultados.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(86, Byte), Integer))
        Me.Cortante_Resultados.ForeColor = System.Drawing.Color.White
        Me.Cortante_Resultados.Name = "Cortante_Resultados"
        Me.Cortante_Resultados.Size = New System.Drawing.Size(252, 28)
        Me.Cortante_Resultados.Text = "Revisión de Cortante"
        '
        'VerToolStripMenuItem
        '
        Me.VerToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CrearReporteToolStripMenuItem, Me.IngresarResultadosDeModeloToolStripMenuItem})
        Me.VerToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.VerToolStripMenuItem.Name = "VerToolStripMenuItem"
        Me.VerToolStripMenuItem.Size = New System.Drawing.Size(95, 27)
        Me.VerToolStripMenuItem.Text = "Opciones"
        '
        'CrearReporteToolStripMenuItem
        '
        Me.CrearReporteToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.CrearReporteToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.AExcelToolStripMenuItem})
        Me.CrearReporteToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.CrearReporteToolStripMenuItem.Name = "CrearReporteToolStripMenuItem"
        Me.CrearReporteToolStripMenuItem.Size = New System.Drawing.Size(298, 26)
        Me.CrearReporteToolStripMenuItem.Text = "Crear Reporte"
        '
        'AExcelToolStripMenuItem
        '
        Me.AExcelToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.AExcelToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.AExcelToolStripMenuItem.Name = "AExcelToolStripMenuItem"
        Me.AExcelToolStripMenuItem.Size = New System.Drawing.Size(140, 26)
        Me.AExcelToolStripMenuItem.Text = "A Excel"
        '
        'IngresarResultadosDeModeloToolStripMenuItem
        '
        Me.IngresarResultadosDeModeloToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.IngresarResultadosDeModeloToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.IngresarResultadosDeModeloToolStripMenuItem.Name = "IngresarResultadosDeModeloToolStripMenuItem"
        Me.IngresarResultadosDeModeloToolStripMenuItem.Size = New System.Drawing.Size(298, 26)
        Me.IngresarResultadosDeModeloToolStripMenuItem.Text = "Ingresar Resultados de Modelo"
        '
        'MostrarToolStripMenuItem
        '
        Me.MostrarToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.GráficosToolStripMenuItem1, Me.RevisiónDeCortanteToolStripMenuItem})
        Me.MostrarToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.MostrarToolStripMenuItem.Name = "MostrarToolStripMenuItem"
        Me.MostrarToolStripMenuItem.Size = New System.Drawing.Size(83, 27)
        Me.MostrarToolStripMenuItem.Text = "Mostrar"
        '
        'GráficosToolStripMenuItem1
        '
        Me.GráficosToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.GráficosToolStripMenuItem1.ForeColor = System.Drawing.Color.White
        Me.GráficosToolStripMenuItem1.Name = "GráficosToolStripMenuItem1"
        Me.GráficosToolStripMenuItem1.Size = New System.Drawing.Size(229, 26)
        Me.GráficosToolStripMenuItem1.Text = "Gráficos"
        '
        'RevisiónDeCortanteToolStripMenuItem
        '
        Me.RevisiónDeCortanteToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.RevisiónDeCortanteToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.RevisiónDeCortanteToolStripMenuItem.Name = "RevisiónDeCortanteToolStripMenuItem"
        Me.RevisiónDeCortanteToolStripMenuItem.Size = New System.Drawing.Size(229, 26)
        Me.RevisiónDeCortanteToolStripMenuItem.Text = "Revisión de Cortante"
        '
        'Form_02_01_ResultadosColumnas
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1989, 911)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.Name = "Form_02_01_ResultadosColumnas"
        Me.Text = "Resultados Columnas"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.Tabla_Resultados, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Combo_Elementos As ComboBox
    Friend WithEvents Tabla_Resultados As DataGridView
    Friend WithEvents Button1 As Button
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents VerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CrearReporteToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AExcelToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Column1 As DataGridViewTextBoxColumn
    Friend WithEvents Column11 As DataGridViewTextBoxColumn
    Friend WithEvents Column12 As DataGridViewTextBoxColumn
    Friend WithEvents Column3 As DataGridViewTextBoxColumn
    Friend WithEvents Column2 As DataGridViewTextBoxColumn
    Friend WithEvents Column4 As DataGridViewTextBoxColumn
    Friend WithEvents Column14 As DataGridViewTextBoxColumn
    Friend WithEvents Column13 As DataGridViewTextBoxColumn
    Friend WithEvents Column5 As DataGridViewTextBoxColumn
    Friend WithEvents Column6 As DataGridViewTextBoxColumn
    Friend WithEvents Column7 As DataGridViewTextBoxColumn
    Friend WithEvents Column8 As DataGridViewTextBoxColumn
    Friend WithEvents Column9 As DataGridViewTextBoxColumn
    Friend WithEvents Column10 As DataGridViewTextBoxColumn
    Friend WithEvents IngresarResultadosDeModeloToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents MostrarToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents GráficosToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents RevisiónDeCortanteToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem3 As ToolStripMenuItem
    Friend WithEvents Reporte_Col_Excel As ToolStripMenuItem
    Friend WithEvents Reporte_Col_PDF As ToolStripMenuItem
    Friend WithEvents Resultados_Modelo_Col As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem2 As ToolStripMenuItem
    Friend WithEvents Graficos_Resultados As ToolStripMenuItem
    Friend WithEvents Cortante_Resultados As ToolStripMenuItem
End Class
