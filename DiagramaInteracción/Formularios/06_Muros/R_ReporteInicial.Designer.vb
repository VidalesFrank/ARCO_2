<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class R_ReporteInicial
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(R_ReporteInicial))
        Me.Figura_Muros = New System.Windows.Forms.PictureBox()
        Me.Button1 = New System.Windows.Forms.Button()
        CType(Me.Figura_Muros, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Figura_Muros
        '
        Me.Figura_Muros.BackColor = System.Drawing.Color.White
        Me.Figura_Muros.Location = New System.Drawing.Point(53, 121)
        Me.Figura_Muros.Name = "Figura_Muros"
        Me.Figura_Muros.Size = New System.Drawing.Size(1257, 529)
        Me.Figura_Muros.TabIndex = 0
        Me.Figura_Muros.TabStop = False
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(565, 666)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(221, 53)
        Me.Button1.TabIndex = 1
        Me.Button1.Text = "Graficar"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'R_ReporteInicial
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1367, 740)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Figura_Muros)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "R_ReporteInicial"
        Me.Text = "Reporte Inicial"
        CType(Me.Figura_Muros, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Figura_Muros As PictureBox
    Friend WithEvents Button1 As Button
End Class
