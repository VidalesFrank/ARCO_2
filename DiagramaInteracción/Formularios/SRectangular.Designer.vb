<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Public Class SRectangular
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
        Dim ChartArea1 As System.Windows.Forms.DataVisualization.Charting.ChartArea = New System.Windows.Forms.DataVisualization.Charting.ChartArea()
        Dim Legend1 As System.Windows.Forms.DataVisualization.Charting.Legend = New System.Windows.Forms.DataVisualization.Charting.Legend()
        Dim Series1 As System.Windows.Forms.DataVisualization.Charting.Series = New System.Windows.Forms.DataVisualization.Charting.Series()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SRectangular))
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.Grafico_DI = New System.Windows.Forms.DataVisualization.Charting.Chart()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.Tabla_Solicitaciones = New System.Windows.Forms.DataGridView()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
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
        Me.Op_IdBarra = New System.Windows.Forms.ComboBox()
        Me.T_Cantidad_Corto = New System.Windows.Forms.TextBox()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.Combo_Estacion = New System.Windows.Forms.ComboBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Combo_Seccion = New System.Windows.Forms.ComboBox()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.Combo_Tramos = New System.Windows.Forms.ComboBox()
        Me.Panel3 = New System.Windows.Forms.Panel()
        Me.Panel4 = New System.Windows.Forms.Panel()
        Me.GroupBox3.SuspendLayout()
        CType(Me.Grafico_DI, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox4.SuspendLayout()
        CType(Me.Tabla_Solicitaciones, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox1.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.Panel2.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.Panel4.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.Grafico_DI)
        Me.GroupBox3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBox3.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox3.Location = New System.Drawing.Point(0, 0)
        Me.GroupBox3.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.GroupBox3.Size = New System.Drawing.Size(612, 530)
        Me.GroupBox3.TabIndex = 13
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Diagrama Interacción"
        '
        'Grafico_DI
        '
        ChartArea1.Name = "ChartArea1"
        Me.Grafico_DI.ChartAreas.Add(ChartArea1)
        Me.Grafico_DI.Dock = System.Windows.Forms.DockStyle.Fill
        Legend1.Name = "Legend1"
        Me.Grafico_DI.Legends.Add(Legend1)
        Me.Grafico_DI.Location = New System.Drawing.Point(2, 18)
        Me.Grafico_DI.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.Grafico_DI.Name = "Grafico_DI"
        Series1.ChartArea = "ChartArea1"
        Series1.Legend = "Legend1"
        Series1.Name = "Series1"
        Me.Grafico_DI.Series.Add(Series1)
        Me.Grafico_DI.Size = New System.Drawing.Size(608, 510)
        Me.Grafico_DI.TabIndex = 0
        Me.Grafico_DI.Text = "Chart1"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.Tabla_Solicitaciones)
        Me.GroupBox4.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.GroupBox4.Location = New System.Drawing.Point(0, 530)
        Me.GroupBox4.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.GroupBox4.Size = New System.Drawing.Size(612, 161)
        Me.GroupBox4.TabIndex = 1
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Solicitaciones"
        '
        'Tabla_Solicitaciones
        '
        Me.Tabla_Solicitaciones.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.Tabla_Solicitaciones.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Tabla_Solicitaciones.Location = New System.Drawing.Point(2, 15)
        Me.Tabla_Solicitaciones.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.Tabla_Solicitaciones.Name = "Tabla_Solicitaciones"
        Me.Tabla_Solicitaciones.RowHeadersWidth = 51
        Me.Tabla_Solicitaciones.RowTemplate.Height = 24
        Me.Tabla_Solicitaciones.Size = New System.Drawing.Size(608, 144)
        Me.Tabla_Solicitaciones.TabIndex = 0
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.PictureBox1)
        Me.GroupBox1.Dock = System.Windows.Forms.DockStyle.Left
        Me.GroupBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(236, 25)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.GroupBox1.Size = New System.Drawing.Size(244, 691)
        Me.GroupBox1.TabIndex = 12
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Sección"
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.White
        Me.PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PictureBox1.Location = New System.Drawing.Point(2, 18)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(240, 671)
        Me.PictureBox1.TabIndex = 11
        Me.PictureBox1.TabStop = False
        '
        'Button2
        '
        Me.Button2.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button2.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.ForeColor = System.Drawing.Color.White
        Me.Button2.Location = New System.Drawing.Point(64, 338)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(90, 35)
        Me.Button2.TabIndex = 13
        Me.Button2.Text = "Dibujar"
        Me.Button2.UseVisualStyleBackColor = False
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button1.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Button1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(55, 126)
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
        Me.T_Acero.Location = New System.Drawing.Point(125, 211)
        Me.T_Acero.Name = "T_Acero"
        Me.T_Acero.Size = New System.Drawing.Size(61, 24)
        Me.T_Acero.TabIndex = 7
        Me.T_Acero.Text = "0"
        Me.T_Acero.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'T_Cantidad_Largo
        '
        Me.T_Cantidad_Largo.Font = New System.Drawing.Font("Arial", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T_Cantidad_Largo.Location = New System.Drawing.Point(107, 63)
        Me.T_Cantidad_Largo.Name = "T_Cantidad_Largo"
        Me.T_Cantidad_Largo.Size = New System.Drawing.Size(74, 24)
        Me.T_Cantidad_Largo.TabIndex = 4
        Me.T_Cantidad_Largo.Text = "1"
        Me.T_Cantidad_Largo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(19, 29)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(62, 18)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "ID Barra"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, CType((System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Underline), System.Drawing.FontStyle), System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(44, 249)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(152, 18)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "Cantidad de Barras"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(39, 91)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(44, 18)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Y (m)"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(39, 65)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(45, 18)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "X (m)"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.MenuStrip1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MenuStrip1.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.MenuStrip1.ImageScalingSize = New System.Drawing.Size(20, 20)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.RefuerzoToolStripMenuItem})
        Me.MenuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Padding = New System.Windows.Forms.Padding(4, 2, 0, 2)
        Me.MenuStrip1.Size = New System.Drawing.Size(1092, 25)
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
        Me.GroupBox2.Controls.Add(Me.Op_IdBarra)
        Me.GroupBox2.Controls.Add(Me.T_Cantidad_Corto)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.Label3)
        Me.GroupBox2.Controls.Add(Me.Label4)
        Me.GroupBox2.Controls.Add(Me.T_Cantidad_Largo)
        Me.GroupBox2.Controls.Add(Me.T_Acero)
        Me.GroupBox2.Controls.Add(Me.Button1)
        Me.GroupBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox2.ForeColor = System.Drawing.Color.White
        Me.GroupBox2.Location = New System.Drawing.Point(10, 138)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(215, 178)
        Me.GroupBox2.TabIndex = 12
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Refuerzo Longitudinal"
        '
        'Op_IdBarra
        '
        Me.Op_IdBarra.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Op_IdBarra.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Op_IdBarra.FormattingEnabled = True
        Me.Op_IdBarra.Location = New System.Drawing.Point(107, 28)
        Me.Op_IdBarra.Name = "Op_IdBarra"
        Me.Op_IdBarra.Size = New System.Drawing.Size(74, 24)
        Me.Op_IdBarra.TabIndex = 26
        '
        'T_Cantidad_Corto
        '
        Me.T_Cantidad_Corto.Font = New System.Drawing.Font("Arial", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.T_Cantidad_Corto.Location = New System.Drawing.Point(107, 89)
        Me.T_Cantidad_Corto.Name = "T_Cantidad_Corto"
        Me.T_Cantidad_Corto.Size = New System.Drawing.Size(74, 24)
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
        Me.Panel2.Size = New System.Drawing.Size(236, 691)
        Me.Panel2.TabIndex = 1
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.Label15.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label15.ForeColor = System.Drawing.Color.White
        Me.Label15.Location = New System.Drawing.Point(16, 98)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(79, 20)
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
        Me.Label14.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label14.ForeColor = System.Drawing.Color.White
        Me.Label14.Location = New System.Drawing.Point(16, 32)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(73, 20)
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
        Me.Label13.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label13.ForeColor = System.Drawing.Color.White
        Me.Label13.Location = New System.Drawing.Point(16, 65)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(43, 20)
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
        Me.Panel3.Size = New System.Drawing.Size(1092, 25)
        Me.Panel3.TabIndex = 2
        '
        'Panel4
        '
        Me.Panel4.Controls.Add(Me.GroupBox3)
        Me.Panel4.Controls.Add(Me.GroupBox4)
        Me.Panel4.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel4.Location = New System.Drawing.Point(480, 25)
        Me.Panel4.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.Panel4.Name = "Panel4"
        Me.Panel4.Size = New System.Drawing.Size(612, 691)
        Me.Panel4.TabIndex = 14
        '
        'SRectangular
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1092, 716)
        Me.Controls.Add(Me.Panel4)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.Panel2)
        Me.Controls.Add(Me.Panel3)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "SRectangular"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Sección Rectangular"
        Me.GroupBox3.ResumeLayout(False)
        CType(Me.Grafico_DI, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox4.ResumeLayout(False)
        CType(Me.Tabla_Solicitaciones, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox1.ResumeLayout(False)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.Panel3.PerformLayout()
        Me.Panel4.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Label4 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents T_Acero As TextBox
    Friend WithEvents T_Cantidad_Largo As TextBox
    Friend WithEvents Button2 As Button
    Friend WithEvents Button1 As Button
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
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Grafico_DI As DataVisualization.Charting.Chart
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents Tabla_Solicitaciones As DataGridView
    Friend WithEvents Panel4 As Panel
    Friend WithEvents Op_IdBarra As ComboBox
End Class
