<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form_Graficos
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_Graficos))
        Dim ChartArea1 As System.Windows.Forms.DataVisualization.Charting.ChartArea = New System.Windows.Forms.DataVisualization.Charting.ChartArea()
        Dim Legend1 As System.Windows.Forms.DataVisualization.Charting.Legend = New System.Windows.Forms.DataVisualization.Charting.Legend()
        Dim Series1 As System.Windows.Forms.DataVisualization.Charting.Series = New System.Windows.Forms.DataVisualization.Charting.Series()
        Dim Series2 As System.Windows.Forms.DataVisualization.Charting.Series = New System.Windows.Forms.DataVisualization.Charting.Series()
        Dim DataPoint1 As System.Windows.Forms.DataVisualization.Charting.DataPoint = New System.Windows.Forms.DataVisualization.Charting.DataPoint(0R, 60.0R)
        Dim DataPoint2 As System.Windows.Forms.DataVisualization.Charting.DataPoint = New System.Windows.Forms.DataVisualization.Charting.DataPoint(7.5R, 60.0R)
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Boton_Cortante = New System.Windows.Forms.Button()
        Me.Boton_Flexo = New System.Windows.Forms.Button()
        Me.Boton_ALR = New System.Windows.Forms.Button()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.Grafico = New System.Windows.Forms.DataVisualization.Charting.Chart()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.EditarToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CombinacionesDeAnálisisToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Panel1.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel2.SuspendLayout()
        CType(Me.Grafico, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Panel1.Controls.Add(Me.PictureBox1)
        Me.Panel1.Controls.Add(Me.Label2)
        Me.Panel1.Controls.Add(Me.Boton_Cortante)
        Me.Panel1.Controls.Add(Me.Boton_Flexo)
        Me.Panel1.Controls.Add(Me.Boton_ALR)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Left
        Me.Panel1.Location = New System.Drawing.Point(0, 27)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(167, 632)
        Me.Panel1.TabIndex = 0
        '
        'PictureBox1
        '
        Me.PictureBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), System.Drawing.Image)
        Me.PictureBox1.Location = New System.Drawing.Point(0, 465)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(167, 167)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 18
        Me.PictureBox1.TabStop = False
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Label2.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.Label2.Font = New System.Drawing.Font("SansSerif", 16.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.White
        Me.Label2.Location = New System.Drawing.Point(0, 2)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(167, 76)
        Me.Label2.TabIndex = 17
        Me.Label2.Text = "OPCIONES DE GRÁFICOS"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Boton_Cortante
        '
        Me.Boton_Cortante.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Boton_Cortante.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Boton_Cortante.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Boton_Cortante.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Boton_Cortante.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Boton_Cortante.ForeColor = System.Drawing.Color.White
        Me.Boton_Cortante.Location = New System.Drawing.Point(3, 182)
        Me.Boton_Cortante.Name = "Boton_Cortante"
        Me.Boton_Cortante.Size = New System.Drawing.Size(160, 40)
        Me.Boton_Cortante.TabIndex = 16
        Me.Boton_Cortante.Text = "Cortante"
        Me.Boton_Cortante.UseVisualStyleBackColor = False
        '
        'Boton_Flexo
        '
        Me.Boton_Flexo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Boton_Flexo.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Boton_Flexo.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Boton_Flexo.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Boton_Flexo.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Boton_Flexo.ForeColor = System.Drawing.Color.White
        Me.Boton_Flexo.Location = New System.Drawing.Point(3, 141)
        Me.Boton_Flexo.Name = "Boton_Flexo"
        Me.Boton_Flexo.Size = New System.Drawing.Size(160, 40)
        Me.Boton_Flexo.TabIndex = 15
        Me.Boton_Flexo.Text = "Flexo-Compresión"
        Me.Boton_Flexo.UseVisualStyleBackColor = False
        '
        'Boton_ALR
        '
        Me.Boton_ALR.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Boton_ALR.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Boton_ALR.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control
        Me.Boton_ALR.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.Boton_ALR.Font = New System.Drawing.Font("SansSerif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Boton_ALR.ForeColor = System.Drawing.Color.White
        Me.Boton_ALR.Location = New System.Drawing.Point(3, 100)
        Me.Boton_ALR.Name = "Boton_ALR"
        Me.Boton_ALR.Size = New System.Drawing.Size(160, 40)
        Me.Boton_ALR.TabIndex = 14
        Me.Boton_ALR.Text = "ALR"
        Me.Boton_ALR.UseVisualStyleBackColor = False
        '
        'Panel2
        '
        Me.Panel2.Controls.Add(Me.Grafico)
        Me.Panel2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel2.Location = New System.Drawing.Point(167, 27)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(936, 632)
        Me.Panel2.TabIndex = 1
        '
        'Grafico
        '
        Me.Grafico.BackColor = System.Drawing.Color.FromArgb(CType(CType(210, Byte), Integer), CType(CType(210, Byte), Integer), CType(CType(210, Byte), Integer))
        ChartArea1.AxisX.MajorGrid.Enabled = False
        ChartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot
        ChartArea1.AxisX.MajorTickMark.Enabled = False
        ChartArea1.AxisX.MajorTickMark.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dash
        ChartArea1.AxisX.Maximum = 7.5R
        ChartArea1.AxisX.Minimum = 0R
        ChartArea1.AxisX.Title = "Columna"
        ChartArea1.AxisX.TitleFont = New System.Drawing.Font("Arial", 11.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        ChartArea1.AxisX2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.[False]
        ChartArea1.AxisX2.MajorGrid.Enabled = False
        ChartArea1.AxisX2.MajorTickMark.Enabled = False
        ChartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(CType(CType(74, Byte), Integer), CType(CType(74, Byte), Integer), CType(CType(74, Byte), Integer))
        ChartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot
        ChartArea1.AxisY.MajorTickMark.LineColor = System.Drawing.Color.FromArgb(CType(CType(74, Byte), Integer), CType(CType(74, Byte), Integer), CType(CType(74, Byte), Integer))
        ChartArea1.AxisY.TextOrientation = System.Windows.Forms.DataVisualization.Charting.TextOrientation.Rotated270
        ChartArea1.AxisY.Title = "Relación de Carga Axial"
        ChartArea1.AxisY.TitleFont = New System.Drawing.Font("Arial", 11.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        ChartArea1.AxisY2.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.[False]
        ChartArea1.AxisY2.MajorGrid.Enabled = False
        ChartArea1.AxisY2.MajorTickMark.Enabled = False
        ChartArea1.Name = "ChartArea1"
        Me.Grafico.ChartAreas.Add(ChartArea1)
        Me.Grafico.Dock = System.Windows.Forms.DockStyle.Fill
        Legend1.Alignment = System.Drawing.StringAlignment.Center
        Legend1.BackColor = System.Drawing.Color.Transparent
        Legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom
        Legend1.Font = New System.Drawing.Font("Arial", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Legend1.IsTextAutoFit = False
        Legend1.Name = "Legend1"
        Me.Grafico.Legends.Add(Legend1)
        Me.Grafico.Location = New System.Drawing.Point(0, 0)
        Me.Grafico.Name = "Grafico"
        Series1.ChartArea = "ChartArea1"
        Series1.Legend = "Legend1"
        Series1.Name = "Series1"
        Series2.ChartArea = "ChartArea1"
        Series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line
        Series2.Legend = "Legend1"
        Series2.Name = "Series2"
        Series2.Points.Add(DataPoint1)
        Series2.Points.Add(DataPoint2)
        Me.Grafico.Series.Add(Series1)
        Me.Grafico.Series.Add(Series2)
        Me.Grafico.Size = New System.Drawing.Size(936, 632)
        Me.Grafico.TabIndex = 0
        Me.Grafico.Text = "Chart1"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.MenuStrip1.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.EditarToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional
        Me.MenuStrip1.Size = New System.Drawing.Size(1103, 27)
        Me.MenuStrip1.TabIndex = 2
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'EditarToolStripMenuItem
        '
        Me.EditarToolStripMenuItem.AutoToolTip = True
        Me.EditarToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.EditarToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.EditarToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CombinacionesDeAnálisisToolStripMenuItem})
        Me.EditarToolStripMenuItem.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.EditarToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.EditarToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Transparent
        Me.EditarToolStripMenuItem.Name = "EditarToolStripMenuItem"
        Me.EditarToolStripMenuItem.Size = New System.Drawing.Size(56, 23)
        Me.EditarToolStripMenuItem.Text = "Editar"
        '
        'CombinacionesDeAnálisisToolStripMenuItem
        '
        Me.CombinacionesDeAnálisisToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.CombinacionesDeAnálisisToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.CombinacionesDeAnálisisToolStripMenuItem.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.CombinacionesDeAnálisisToolStripMenuItem.ForeColor = System.Drawing.Color.White
        Me.CombinacionesDeAnálisisToolStripMenuItem.Name = "CombinacionesDeAnálisisToolStripMenuItem"
        Me.CombinacionesDeAnálisisToolStripMenuItem.ShortcutKeys = CType((System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.F1), System.Windows.Forms.Keys)
        Me.CombinacionesDeAnálisisToolStripMenuItem.Size = New System.Drawing.Size(313, 24)
        Me.CombinacionesDeAnálisisToolStripMenuItem.Text = "Combinaciones de Análisis"
        '
        'Form_Graficos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1103, 659)
        Me.Controls.Add(Me.Panel2)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "Form_Graficos"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Gráficos"
        Me.Panel1.ResumeLayout(False)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel2.ResumeLayout(False)
        CType(Me.Grafico, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Panel1 As Panel
    Friend WithEvents Panel2 As Panel
    Friend WithEvents Boton_ALR As Button
    Friend WithEvents Boton_Flexo As Button
    Friend WithEvents Grafico As DataVisualization.Charting.Chart
    Friend WithEvents EditarToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CombinacionesDeAnálisisToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Boton_Cortante As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents MenuStrip1 As MenuStrip
End Class
