<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class SRectangular
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SRectangular))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.LbCuantia = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.T_Acero = New System.Windows.Forms.TextBox()
        Me.T_Cantidad_Largo = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.RefuerzoToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.PorCantidadDeBarrasToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.T_Cantidad_Corto = New System.Windows.Forms.TextBox()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.Combo_Estacion = New System.Windows.Forms.ComboBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Combo_Seccion = New System.Windows.Forms.ComboBox()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Combo_Tramos = New System.Windows.Forms.ComboBox()
        Me.Panel3 = New System.Windows.Forms.Panel()
        Me.Panel1.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.Panel2.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.Gainsboro
        Me.Panel1.Controls.Add(Me.LbCuantia)
        Me.Panel1.Controls.Add(Me.Label12)
        Me.Panel1.Controls.Add(Me.PictureBox1)
        Me.Panel1.Controls.Add(Me.Label6)
        Me.Panel1.Controls.Add(Me.Label11)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel1.Location = New System.Drawing.Point(236, 25)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(640, 700)
        Me.Panel1.TabIndex = 0
        '
        'LbCuantia
        '
        Me.LbCuantia.AutoSize = True
        Me.LbCuantia.BackColor = System.Drawing.Color.Transparent
        Me.LbCuantia.Font = New System.Drawing.Font("SansSerif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LbCuantia.Location = New System.Drawing.Point(68, 75)
        Me.LbCuantia.Name = "LbCuantia"
        Me.LbCuantia.Size = New System.Drawing.Size(0, 16)
        Me.LbCuantia.TabIndex = 9
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.BackColor = System.Drawing.Color.White
        Me.Label12.Font = New System.Drawing.Font("SansSerif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label12.Location = New System.Drawing.Point(46, 75)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(19, 16)
        Me.Label12.TabIndex = 10
        Me.Label12.Text = "ρ:"
        Me.Label12.Visible = False
        '
        'PictureBox1
        '
        Me.PictureBox1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PictureBox1.BackColor = System.Drawing.Color.White
        Me.PictureBox1.Location = New System.Drawing.Point(35, 66)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(572, 592)
        Me.PictureBox1.TabIndex = 11
        Me.PictureBox1.TabStop = False
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Font = New System.Drawing.Font("SansSerif", 16.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.ForeColor = System.Drawing.Color.FromArgb(CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer), CType(CType(61, Byte), Integer))
        Me.Label6.Location = New System.Drawing.Point(252, 22)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(112, 26)
        Me.Label6.TabIndex = 3
        Me.Label6.Text = "SECCIÓN"
        '
        'Label11
        '
        Me.Label11.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label11.AutoSize = True
        Me.Label11.Font = New System.Drawing.Font("SansSerif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label11.ForeColor = System.Drawing.Color.Black
        Me.Label11.Location = New System.Drawing.Point(484, 677)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(148, 16)
        Me.Label11.TabIndex = 8
        Me.Label11.Text = "Nota: Unidades en ""m"""
        '
        'Button2
        '
        Me.Button2.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.ForeColor = System.Drawing.Color.White
        Me.Button2.Location = New System.Drawing.Point(73, 365)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(90, 35)
        Me.Button2.TabIndex = 13
        Me.Button2.Text = "Hecho"
        Me.Button2.UseVisualStyleBackColor = False
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button1.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(63, 142)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(90, 32)
        Me.Button1.TabIndex = 12
        Me.Button1.Text = "Modificar"
        Me.Button1.UseVisualStyleBackColor = False
        '
        'T_Acero
        '
        Me.T_Acero.Enabled = False
        Me.T_Acero.Font = New System.Drawing.Font("Arial", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T_Acero.Location = New System.Drawing.Point(142, 25)
        Me.T_Acero.Name = "T_Acero"
        Me.T_Acero.Size = New System.Drawing.Size(61, 24)
        Me.T_Acero.TabIndex = 7
        Me.T_Acero.Text = "0"
        Me.T_Acero.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'T_Cantidad_Largo
        '
        Me.T_Cantidad_Largo.Font = New System.Drawing.Font("Arial", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T_Cantidad_Largo.Location = New System.Drawing.Point(142, 85)
        Me.T_Cantidad_Largo.Name = "T_Cantidad_Largo"
        Me.T_Cantidad_Largo.Size = New System.Drawing.Size(61, 24)
        Me.T_Cantidad_Largo.TabIndex = 4
        Me.T_Cantidad_Largo.Text = "1"
        Me.T_Cantidad_Largo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("SansSerif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(7, 29)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(86, 17)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Área (mm2)"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("SansSerif", 11.0!, CType((System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Underline), System.Drawing.FontStyle), System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(34, 61)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(144, 18)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "Cantidad de Barras"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("SansSerif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(7, 115)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(80, 17)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Lado Corto"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("SansSerif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(7, 89)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(81, 17)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Lado Largo"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.MenuStrip1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MenuStrip1.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.RefuerzoToolStripMenuItem})
        Me.MenuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(876, 25)
        Me.MenuStrip1.TabIndex = 11
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'RefuerzoToolStripMenuItem
        '
        Me.RefuerzoToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.PorCantidadDeBarrasToolStripMenuItem})
        Me.RefuerzoToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.RefuerzoToolStripMenuItem.Name = "RefuerzoToolStripMenuItem"
        Me.RefuerzoToolStripMenuItem.Size = New System.Drawing.Size(74, 21)
        Me.RefuerzoToolStripMenuItem.Text = "Refuerzo"
        '
        'PorCantidadDeBarrasToolStripMenuItem
        '
        Me.PorCantidadDeBarrasToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.PorCantidadDeBarrasToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.PorCantidadDeBarrasToolStripMenuItem.Name = "PorCantidadDeBarrasToolStripMenuItem"
        Me.PorCantidadDeBarrasToolStripMenuItem.Size = New System.Drawing.Size(215, 24)
        Me.PorCantidadDeBarrasToolStripMenuItem.Text = "Por cantidad de barras"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.T_Cantidad_Corto)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.Label3)
        Me.GroupBox2.Controls.Add(Me.Label4)
        Me.GroupBox2.Controls.Add(Me.T_Cantidad_Largo)
        Me.GroupBox2.Controls.Add(Me.T_Acero)
        Me.GroupBox2.Controls.Add(Me.Button1)
        Me.GroupBox2.Font = New System.Drawing.Font("SansSerif", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox2.ForeColor = System.Drawing.Color.White
        Me.GroupBox2.Location = New System.Drawing.Point(10, 138)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(215, 192)
        Me.GroupBox2.TabIndex = 12
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Refuerzo Longitudinal"
        '
        'T_Cantidad_Corto
        '
        Me.T_Cantidad_Corto.Font = New System.Drawing.Font("Arial", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T_Cantidad_Corto.Location = New System.Drawing.Point(142, 112)
        Me.T_Cantidad_Corto.Name = "T_Cantidad_Corto"
        Me.T_Cantidad_Corto.Size = New System.Drawing.Size(61, 24)
        Me.T_Cantidad_Corto.TabIndex = 13
        Me.T_Cantidad_Corto.Text = "1"
        Me.T_Cantidad_Corto.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Panel2
        '
        Me.Panel2.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Panel2.Controls.Add(Me.Label15)
        Me.Panel2.Controls.Add(Me.Combo_Estacion)
        Me.Panel2.Controls.Add(Me.Label14)
        Me.Panel2.Controls.Add(Me.Combo_Seccion)
        Me.Panel2.Controls.Add(Me.Label13)
        Me.Panel2.Controls.Add(Me.Combo_Tramos)
        Me.Panel2.Controls.Add(Me.Button2)
        Me.Panel2.Controls.Add(Me.GroupBox2)
        Me.Panel2.Dock = System.Windows.Forms.DockStyle.Left
        Me.Panel2.Location = New System.Drawing.Point(0, 25)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(236, 700)
        Me.Panel2.TabIndex = 1
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label15.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label15.ForeColor = System.Drawing.Color.White
        Me.Label15.Location = New System.Drawing.Point(16, 98)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(76, 19)
        Me.Label15.TabIndex = 26
        Me.Label15.Text = "Estación"
        Me.Label15.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Combo_Estacion
        '
        Me.Combo_Estacion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Combo_Estacion.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Combo_Estacion.FormattingEnabled = True
        Me.Combo_Estacion.Location = New System.Drawing.Point(101, 95)
        Me.Combo_Estacion.Name = "Combo_Estacion"
        Me.Combo_Estacion.Size = New System.Drawing.Size(121, 24)
        Me.Combo_Estacion.TabIndex = 25
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label14.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label14.ForeColor = System.Drawing.Color.White
        Me.Label14.Location = New System.Drawing.Point(16, 32)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(71, 19)
        Me.Label14.TabIndex = 24
        Me.Label14.Text = "Sección"
        Me.Label14.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Combo_Seccion
        '
        Me.Combo_Seccion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Combo_Seccion.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Combo_Seccion.FormattingEnabled = True
        Me.Combo_Seccion.Location = New System.Drawing.Point(101, 29)
        Me.Combo_Seccion.Name = "Combo_Seccion"
        Me.Combo_Seccion.Size = New System.Drawing.Size(121, 24)
        Me.Combo_Seccion.TabIndex = 23
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label13.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label13.ForeColor = System.Drawing.Color.White
        Me.Label13.Location = New System.Drawing.Point(16, 65)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(43, 19)
        Me.Label13.TabIndex = 22
        Me.Label13.Text = "Piso"
        Me.Label13.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Combo_Tramos
        '
        Me.Combo_Tramos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Combo_Tramos.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Combo_Tramos.FormattingEnabled = True
        Me.Combo_Tramos.Location = New System.Drawing.Point(101, 62)
        Me.Combo_Tramos.Name = "Combo_Tramos"
        Me.Combo_Tramos.Size = New System.Drawing.Size(121, 24)
        Me.Combo_Tramos.TabIndex = 21
        '
        'Panel3
        '
        Me.Panel3.Controls.Add(Me.MenuStrip1)
        Me.Panel3.Dock = System.Windows.Forms.DockStyle.Top
        Me.Panel3.Location = New System.Drawing.Point(0, 0)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(876, 25)
        Me.Panel3.TabIndex = 2
        '
        'SRectangular
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(876, 725)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.Panel2)
        Me.Controls.Add(Me.Panel3)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "SRectangular"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Sección Rectangular"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel3.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label4 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents T_Acero As TextBox
    Friend WithEvents T_Cantidad_Largo As TextBox
    Friend WithEvents Button2 As Button
    Friend WithEvents Button1 As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents Label11 As Label
    Friend WithEvents LbCuantia As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents RefuerzoToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents PorCantidadDeBarrasToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents Panel2 As Panel
    Friend WithEvents Panel3 As Panel
    Friend WithEvents Combo_Tramos As ComboBox
    Friend WithEvents Label13 As Label
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Label15 As Label
    Friend WithEvents Combo_Estacion As ComboBox
    Friend WithEvents Label14 As Label
    Friend WithEvents Combo_Seccion As ComboBox
    Friend WithEvents T_Cantidad_Corto As TextBox
End Class
