<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form_02_00_PagInfoColumnas
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_02_00_PagInfoColumnas))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.C_Lista_Secciones_Principales = New System.Windows.Forms.ComboBox()
        Me.Op_SeccionSimilar = New System.Windows.Forms.RadioButton()
        Me.Op_SeccionPrincipal = New System.Windows.Forms.RadioButton()
        Me.T_Seccion = New System.Windows.Forms.TextBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Combo_Elementos = New System.Windows.Forms.ComboBox()
        Me.Tabla_Info_Seccion = New System.Windows.Forms.DataGridView()
        Me.Column1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column3 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column12 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column13 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column4 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column5 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column6 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column7 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column8 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column9 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column10 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column11 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column14 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column15 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column17 = New System.Windows.Forms.DataGridViewComboBoxColumn()
        Me.Column16 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.VerToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.SecciónToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.Panel1.SuspendLayout()
        CType(Me.Tabla_Info_Seccion, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel2.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.Gainsboro
        Me.Panel1.Controls.Add(Me.C_Lista_Secciones_Principales)
        Me.Panel1.Controls.Add(Me.Op_SeccionSimilar)
        Me.Panel1.Controls.Add(Me.Op_SeccionPrincipal)
        Me.Panel1.Controls.Add(Me.T_Seccion)
        Me.Panel1.Controls.Add(Me.Button2)
        Me.Panel1.Controls.Add(Me.Button1)
        Me.Panel1.Controls.Add(Me.Label2)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Controls.Add(Me.Combo_Elementos)
        Me.Panel1.Controls.Add(Me.Tabla_Info_Seccion)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 31)
        Me.Panel1.Margin = New System.Windows.Forms.Padding(4)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1467, 648)
        Me.Panel1.TabIndex = 0
        '
        'C_Lista_Secciones_Principales
        '
        Me.C_Lista_Secciones_Principales.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.C_Lista_Secciones_Principales.Enabled = False
        Me.C_Lista_Secciones_Principales.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.C_Lista_Secciones_Principales.FormattingEnabled = True
        Me.C_Lista_Secciones_Principales.Location = New System.Drawing.Point(701, 129)
        Me.C_Lista_Secciones_Principales.Margin = New System.Windows.Forms.Padding(4)
        Me.C_Lista_Secciones_Principales.Name = "C_Lista_Secciones_Principales"
        Me.C_Lista_Secciones_Principales.Size = New System.Drawing.Size(160, 28)
        Me.C_Lista_Secciones_Principales.TabIndex = 11
        '
        'Op_SeccionSimilar
        '
        Me.Op_SeccionSimilar.AutoSize = True
        Me.Op_SeccionSimilar.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Op_SeccionSimilar.Location = New System.Drawing.Point(472, 130)
        Me.Op_SeccionSimilar.Margin = New System.Windows.Forms.Padding(4)
        Me.Op_SeccionSimilar.Name = "Op_SeccionSimilar"
        Me.Op_SeccionSimilar.Size = New System.Drawing.Size(182, 27)
        Me.Op_SeccionSimilar.TabIndex = 10
        Me.Op_SeccionSimilar.Text = "Sección Similar a"
        Me.Op_SeccionSimilar.UseVisualStyleBackColor = True
        '
        'Op_SeccionPrincipal
        '
        Me.Op_SeccionPrincipal.AutoSize = True
        Me.Op_SeccionPrincipal.Checked = True
        Me.Op_SeccionPrincipal.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Op_SeccionPrincipal.Location = New System.Drawing.Point(472, 94)
        Me.Op_SeccionPrincipal.Margin = New System.Windows.Forms.Padding(4)
        Me.Op_SeccionPrincipal.Name = "Op_SeccionPrincipal"
        Me.Op_SeccionPrincipal.Size = New System.Drawing.Size(180, 27)
        Me.Op_SeccionPrincipal.TabIndex = 9
        Me.Op_SeccionPrincipal.TabStop = True
        Me.Op_SeccionPrincipal.Text = "Sección Principal"
        Me.Op_SeccionPrincipal.UseVisualStyleBackColor = True
        '
        'T_Seccion
        '
        Me.T_Seccion.Font = New System.Drawing.Font("Arial", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T_Seccion.Location = New System.Drawing.Point(216, 129)
        Me.T_Seccion.Margin = New System.Windows.Forms.Padding(4)
        Me.T_Seccion.Multiline = True
        Me.T_Seccion.Name = "T_Seccion"
        Me.T_Seccion.Size = New System.Drawing.Size(160, 29)
        Me.T_Seccion.TabIndex = 8
        Me.T_Seccion.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Button2
        '
        Me.Button2.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button2.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button2.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Font = New System.Drawing.Font("SansSerif", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.ForeColor = System.Drawing.Color.White
        Me.Button2.Location = New System.Drawing.Point(753, 590)
        Me.Button2.Margin = New System.Windows.Forms.Padding(4)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(173, 43)
        Me.Button2.TabIndex = 7
        Me.Button2.Text = "CALCULAR"
        Me.Button2.UseVisualStyleBackColor = False
        '
        'Button1
        '
        Me.Button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button1.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("SansSerif", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(551, 590)
        Me.Button1.Margin = New System.Windows.Forms.Padding(4)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(173, 43)
        Me.Button1.TabIndex = 6
        Me.Button1.Text = "AGREGAR"
        Me.Button1.UseVisualStyleBackColor = False
        '
        'Label2
        '
        Me.Label2.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label2.Font = New System.Drawing.Font("SansSerif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer))
        Me.Label2.Location = New System.Drawing.Point(52, 110)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(156, 30)
        Me.Label2.TabIndex = 5
        Me.Label2.Text = "SECCIÓN"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label1
        '
        Me.Label1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label1.AutoSize = True
        Me.Label1.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label1.Font = New System.Drawing.Font("SansSerif", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(387, 33)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(630, 35)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "INGRESAR INFORMACIÓN DE SECCIONES"
        '
        'Combo_Elementos
        '
        Me.Combo_Elementos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Combo_Elementos.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Combo_Elementos.FormattingEnabled = True
        Me.Combo_Elementos.Location = New System.Drawing.Point(216, 92)
        Me.Combo_Elementos.Margin = New System.Windows.Forms.Padding(4)
        Me.Combo_Elementos.Name = "Combo_Elementos"
        Me.Combo_Elementos.Size = New System.Drawing.Size(160, 28)
        Me.Combo_Elementos.TabIndex = 3
        '
        'Tabla_Info_Seccion
        '
        Me.Tabla_Info_Seccion.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Tabla_Info_Seccion.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.Tabla_Info_Seccion.BackgroundColor = System.Drawing.Color.Gainsboro
        Me.Tabla_Info_Seccion.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.Tabla_Info_Seccion.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle1
        Me.Tabla_Info_Seccion.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.Tabla_Info_Seccion.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Column1, Me.Column3, Me.Column12, Me.Column13, Me.Column2, Me.Column4, Me.Column5, Me.Column6, Me.Column7, Me.Column8, Me.Column9, Me.Column10, Me.Column11, Me.Column14, Me.Column15, Me.Column17, Me.Column16})
        DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Tabla_Info_Seccion.DefaultCellStyle = DataGridViewCellStyle2
        Me.Tabla_Info_Seccion.Location = New System.Drawing.Point(21, 174)
        Me.Tabla_Info_Seccion.Margin = New System.Windows.Forms.Padding(4)
        Me.Tabla_Info_Seccion.Name = "Tabla_Info_Seccion"
        Me.Tabla_Info_Seccion.RowHeadersVisible = False
        Me.Tabla_Info_Seccion.RowHeadersWidth = 51
        Me.Tabla_Info_Seccion.Size = New System.Drawing.Size(1429, 395)
        Me.Tabla_Info_Seccion.TabIndex = 0
        '
        'Column1
        '
        Me.Column1.FillWeight = 108.1635!
        Me.Column1.HeaderText = "Piso"
        Me.Column1.MinimumWidth = 6
        Me.Column1.Name = "Column1"
        '
        'Column3
        '
        Me.Column3.FillWeight = 80.29931!
        Me.Column3.HeaderText = "f'c"
        Me.Column3.MinimumWidth = 6
        Me.Column3.Name = "Column3"
        '
        'Column12
        '
        Me.Column12.FillWeight = 111.1769!
        Me.Column12.HeaderText = "Base (m)"
        Me.Column12.MinimumWidth = 6
        Me.Column12.Name = "Column12"
        '
        'Column13
        '
        Me.Column13.FillWeight = 111.1769!
        Me.Column13.HeaderText = "Alto (m)"
        Me.Column13.MinimumWidth = 6
        Me.Column13.Name = "Column13"
        '
        'Column2
        '
        Me.Column2.FillWeight = 97.58421!
        Me.Column2.HeaderText = "Estación"
        Me.Column2.MinimumWidth = 6
        Me.Column2.Name = "Column2"
        '
        'Column4
        '
        Me.Column4.FillWeight = 80.29931!
        Me.Column4.HeaderText = "#2"
        Me.Column4.MinimumWidth = 6
        Me.Column4.Name = "Column4"
        '
        'Column5
        '
        Me.Column5.FillWeight = 80.29931!
        Me.Column5.HeaderText = "#3"
        Me.Column5.MinimumWidth = 6
        Me.Column5.Name = "Column5"
        '
        'Column6
        '
        Me.Column6.FillWeight = 80.29931!
        Me.Column6.HeaderText = "#4"
        Me.Column6.MinimumWidth = 6
        Me.Column6.Name = "Column6"
        '
        'Column7
        '
        Me.Column7.FillWeight = 80.29931!
        Me.Column7.HeaderText = "#5"
        Me.Column7.MinimumWidth = 6
        Me.Column7.Name = "Column7"
        '
        'Column8
        '
        Me.Column8.FillWeight = 80.29931!
        Me.Column8.HeaderText = "#6"
        Me.Column8.MinimumWidth = 6
        Me.Column8.Name = "Column8"
        '
        'Column9
        '
        Me.Column9.FillWeight = 80.29931!
        Me.Column9.HeaderText = "#7"
        Me.Column9.MinimumWidth = 6
        Me.Column9.Name = "Column9"
        '
        'Column10
        '
        Me.Column10.FillWeight = 80.29931!
        Me.Column10.HeaderText = "#8"
        Me.Column10.MinimumWidth = 6
        Me.Column10.Name = "Column10"
        '
        'Column11
        '
        Me.Column11.FillWeight = 80.29931!
        Me.Column11.HeaderText = "#10"
        Me.Column11.MinimumWidth = 6
        Me.Column11.Name = "Column11"
        '
        'Column14
        '
        Me.Column14.FillWeight = 111.1769!
        Me.Column14.HeaderText = "Ramas Sentido Largo"
        Me.Column14.MinimumWidth = 6
        Me.Column14.Name = "Column14"
        '
        'Column15
        '
        Me.Column15.FillWeight = 111.1769!
        Me.Column15.HeaderText = "Ramas Sentido Corto"
        Me.Column15.MinimumWidth = 6
        Me.Column15.Name = "Column15"
        '
        'Column17
        '
        Me.Column17.HeaderText = "# Barra"
        Me.Column17.Items.AddRange(New Object() {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"})
        Me.Column17.MinimumWidth = 6
        Me.Column17.Name = "Column17"
        '
        'Column16
        '
        Me.Column16.FillWeight = 155.1581!
        Me.Column16.HeaderText = "Separación (m)"
        Me.Column16.MinimumWidth = 6
        Me.Column16.Name = "Column16"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.MenuStrip1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MenuStrip1.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.MenuStrip1.ImageScalingSize = New System.Drawing.Size(20, 20)
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(1467, 31)
        Me.MenuStrip1.TabIndex = 9
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'VerToolStripMenuItem
        '
        Me.VerToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SecciónToolStripMenuItem})
        Me.VerToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.VerToolStripMenuItem.Name = "VerToolStripMenuItem"
        Me.VerToolStripMenuItem.Size = New System.Drawing.Size(49, 27)
        Me.VerToolStripMenuItem.Text = "Ver"
        '
        'SecciónToolStripMenuItem
        '
        Me.SecciónToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.SecciónToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.SecciónToolStripMenuItem.Name = "SecciónToolStripMenuItem"
        Me.SecciónToolStripMenuItem.Size = New System.Drawing.Size(143, 26)
        Me.SecciónToolStripMenuItem.Text = "Sección"
        '
        'Panel2
        '
        Me.Panel2.Controls.Add(Me.MenuStrip1)
        Me.Panel2.Dock = System.Windows.Forms.DockStyle.Top
        Me.Panel2.Location = New System.Drawing.Point(0, 0)
        Me.Panel2.Margin = New System.Windows.Forms.Padding(4)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(1467, 31)
        Me.Panel2.TabIndex = 1
        '
        'Form_02_00_PagInfoColumnas
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1467, 679)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.Panel2)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.Name = "Form_02_00_PagInfoColumnas"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Información de sección"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.Tabla_Info_Seccion, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents Tabla_Info_Seccion As DataGridView
    Friend WithEvents Combo_Elementos As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents Column1 As DataGridViewTextBoxColumn
    Friend WithEvents Column3 As DataGridViewTextBoxColumn
    Friend WithEvents Column12 As DataGridViewTextBoxColumn
    Friend WithEvents Column13 As DataGridViewTextBoxColumn
    Friend WithEvents Column2 As DataGridViewTextBoxColumn
    Friend WithEvents Column4 As DataGridViewTextBoxColumn
    Friend WithEvents Column5 As DataGridViewTextBoxColumn
    Friend WithEvents Column6 As DataGridViewTextBoxColumn
    Friend WithEvents Column7 As DataGridViewTextBoxColumn
    Friend WithEvents Column8 As DataGridViewTextBoxColumn
    Friend WithEvents Column9 As DataGridViewTextBoxColumn
    Friend WithEvents Column10 As DataGridViewTextBoxColumn
    Friend WithEvents Column11 As DataGridViewTextBoxColumn
    Friend WithEvents Column14 As DataGridViewTextBoxColumn
    Friend WithEvents Column15 As DataGridViewTextBoxColumn
    Friend WithEvents Column17 As DataGridViewComboBoxColumn
    Friend WithEvents Column16 As DataGridViewTextBoxColumn
    Friend WithEvents T_Seccion As TextBox
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents VerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SecciónToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Panel2 As Panel
    Friend WithEvents Op_SeccionPrincipal As RadioButton
    Friend WithEvents Op_SeccionSimilar As RadioButton
    Friend WithEvents C_Lista_Secciones_Principales As ComboBox
End Class
