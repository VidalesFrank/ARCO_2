<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form_Documentacion
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso (components IsNot Nothing) Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub InitializeComponent()
        Me.PanelHeader = New System.Windows.Forms.Panel()
        Me.LblTitulo = New System.Windows.Forms.Label()
        Me.PanelToolbar = New System.Windows.Forms.Panel()
        Me.BtnInicio = New System.Windows.Forms.Button()
        Me.BtnAtras = New System.Windows.Forms.Button()
        Me.BtnAdelante = New System.Windows.Forms.Button()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.TreeView1 = New System.Windows.Forms.TreeView()
        Me.WebBrowser1 = New System.Windows.Forms.WebBrowser()
        Me.PanelHeader.SuspendLayout()
        Me.PanelToolbar.SuspendLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.SuspendLayout()

        ' PanelHeader
        Me.PanelHeader.BackColor = System.Drawing.Color.FromArgb(87, 87, 86)
        Me.PanelHeader.Controls.Add(Me.LblTitulo)
        Me.PanelHeader.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelHeader.Height = 48
        Me.PanelHeader.Padding = New System.Windows.Forms.Padding(12, 0, 0, 0)

        ' LblTitulo
        Me.LblTitulo.AutoSize = False
        Me.LblTitulo.Dock = System.Windows.Forms.DockStyle.Fill
        Me.LblTitulo.Font = New System.Drawing.Font("Segoe UI", 13, System.Drawing.FontStyle.Bold)
        Me.LblTitulo.ForeColor = System.Drawing.Color.White
        Me.LblTitulo.Text = "ARCO — Documentacion"
        Me.LblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft

        ' PanelToolbar
        Me.PanelToolbar.BackColor = System.Drawing.Color.FromArgb(234, 234, 234)
        Me.PanelToolbar.Controls.Add(Me.BtnAdelante)
        Me.PanelToolbar.Controls.Add(Me.BtnAtras)
        Me.PanelToolbar.Controls.Add(Me.BtnInicio)
        Me.PanelToolbar.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelToolbar.Height = 36
        Me.PanelToolbar.Padding = New System.Windows.Forms.Padding(6, 4, 0, 0)

        ' BtnAtras
        Me.BtnAtras.Text = "< Atras"
        Me.BtnAtras.Location = New System.Drawing.Point(6, 5)
        Me.BtnAtras.Size = New System.Drawing.Size(75, 26)
        Me.BtnAtras.Font = New System.Drawing.Font("Segoe UI", 8.5F)
        Me.BtnAtras.Enabled = False

        ' BtnAdelante
        Me.BtnAdelante.Text = "Adelante >"
        Me.BtnAdelante.Location = New System.Drawing.Point(86, 5)
        Me.BtnAdelante.Size = New System.Drawing.Size(80, 26)
        Me.BtnAdelante.Font = New System.Drawing.Font("Segoe UI", 8.5F)
        Me.BtnAdelante.Enabled = False

        ' BtnInicio
        Me.BtnInicio.Text = "Inicio"
        Me.BtnInicio.Location = New System.Drawing.Point(172, 5)
        Me.BtnInicio.Size = New System.Drawing.Size(70, 26)
        Me.BtnInicio.Font = New System.Drawing.Font("Segoe UI", 8.5F)

        ' SplitContainer1
        Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer1.SplitterDistance = 220
        Me.SplitContainer1.Panel1.Controls.Add(Me.TreeView1)
        Me.SplitContainer1.Panel2.Controls.Add(Me.WebBrowser1)

        ' TreeView1
        Me.TreeView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TreeView1.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TreeView1.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)

        ' WebBrowser1
        Me.WebBrowser1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.WebBrowser1.ScriptErrorsSuppressed = True

        ' Form_Documentacion
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0F, 13.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Controls.Add(Me.PanelToolbar)
        Me.Controls.Add(Me.PanelHeader)
        Me.MinimumSize = New System.Drawing.Size(800, 560)
        Me.Name = "Form_Documentacion"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "ARCO — Documentacion"

        Me.PanelHeader.ResumeLayout(False)
        Me.PanelToolbar.ResumeLayout(False)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents PanelHeader As System.Windows.Forms.Panel
    Friend WithEvents LblTitulo As System.Windows.Forms.Label
    Friend WithEvents PanelToolbar As System.Windows.Forms.Panel
    Friend WithEvents BtnInicio As System.Windows.Forms.Button
    Friend WithEvents BtnAtras As System.Windows.Forms.Button
    Friend WithEvents BtnAdelante As System.Windows.Forms.Button
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents TreeView1 As System.Windows.Forms.TreeView
    Friend WithEvents WebBrowser1 As System.Windows.Forms.WebBrowser
End Class
