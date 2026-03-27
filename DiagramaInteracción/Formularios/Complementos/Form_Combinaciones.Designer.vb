<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormArchivos
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormArchivos))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Tabla_combinaciones = New System.Windows.Forms.DataGridView()
        Me.Column1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Combo_Combinaciones = New System.Windows.Forms.ComboBox()
        Me.Panel1.SuspendLayout()
        CType(Me.Tabla_combinaciones, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.Gainsboro
        Me.Panel1.Controls.Add(Me.Button2)
        Me.Panel1.Controls.Add(Me.Tabla_combinaciones)
        Me.Panel1.Controls.Add(Me.Button1)
        Me.Panel1.Controls.Add(Me.Label2)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Controls.Add(Me.Combo_Combinaciones)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(747, 401)
        Me.Panel1.TabIndex = 0
        '
        'Button2
        '
        Me.Button2.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button2.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.Button2.Font = New System.Drawing.Font("SansSerif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.ForeColor = System.Drawing.Color.White
        Me.Button2.Location = New System.Drawing.Point(308, 355)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(130, 35)
        Me.Button2.TabIndex = 11
        Me.Button2.Text = "OK"
        Me.Button2.UseVisualStyleBackColor = False
        '
        'Tabla_combinaciones
        '
        Me.Tabla_combinaciones.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.Tabla_combinaciones.BackgroundColor = System.Drawing.Color.Gainsboro
        Me.Tabla_combinaciones.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Arial", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.Tabla_combinaciones.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle1
        Me.Tabla_combinaciones.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.Tabla_combinaciones.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Column1})
        DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Arial", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.Tabla_combinaciones.DefaultCellStyle = DataGridViewCellStyle2
        Me.Tabla_combinaciones.Location = New System.Drawing.Point(39, 157)
        Me.Tabla_combinaciones.Name = "Tabla_combinaciones"
        Me.Tabla_combinaciones.RowHeadersVisible = False
        Me.Tabla_combinaciones.Size = New System.Drawing.Size(656, 186)
        Me.Tabla_combinaciones.TabIndex = 10
        '
        'Column1
        '
        Me.Column1.HeaderText = "Combinación"
        Me.Column1.Name = "Column1"
        Me.Column1.ReadOnly = True
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button1.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.Button1.Font = New System.Drawing.Font("SansSerif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(565, 94)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(130, 25)
        Me.Button1.TabIndex = 9
        Me.Button1.Text = "AGREGAR"
        Me.Button1.UseVisualStyleBackColor = False
        '
        'Label2
        '
        Me.Label2.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label2.Font = New System.Drawing.Font("SansSerif", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer))
        Me.Label2.Location = New System.Drawing.Point(35, 95)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(154, 24)
        Me.Label2.TabIndex = 8
        Me.Label2.Text = "COMBINACIÓN"
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
        Me.Label1.Location = New System.Drawing.Point(198, 29)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(351, 29)
        Me.Label1.TabIndex = 7
        Me.Label1.Text = "INGRESAR COMBINACIONES"
        '
        'Combo_Combinaciones
        '
        Me.Combo_Combinaciones.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Combo_Combinaciones.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Combo_Combinaciones.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Combo_Combinaciones.FormattingEnabled = True
        Me.Combo_Combinaciones.Location = New System.Drawing.Point(206, 95)
        Me.Combo_Combinaciones.Name = "Combo_Combinaciones"
        Me.Combo_Combinaciones.Size = New System.Drawing.Size(336, 24)
        Me.Combo_Combinaciones.TabIndex = 6
        '
        'Form_Combinaciones
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(747, 401)
        Me.Controls.Add(Me.Panel1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Form_Combinaciones"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Verificaciones de la Relación de Carga Axial"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.Tabla_combinaciones, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents Combo_Combinaciones As ComboBox
    Friend WithEvents Button1 As Button
    Friend WithEvents Tabla_combinaciones As DataGridView
    Friend WithEvents Button2 As Button
    Friend WithEvents Column1 As DataGridViewTextBoxColumn
End Class
