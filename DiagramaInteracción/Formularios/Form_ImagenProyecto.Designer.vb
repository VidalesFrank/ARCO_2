<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form_ImagenProyecto
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_ImagenProyecto))
        Me.P_ImagenProyecto = New System.Windows.Forms.PictureBox()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        CType(Me.P_ImagenProyecto, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'P_ImagenProyecto
        '
        Me.P_ImagenProyecto.Location = New System.Drawing.Point(9, 51)
        Me.P_ImagenProyecto.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.P_ImagenProyecto.Name = "P_ImagenProyecto"
        Me.P_ImagenProyecto.Size = New System.Drawing.Size(574, 312)
        Me.P_ImagenProyecto.TabIndex = 0
        Me.P_ImagenProyecto.TabStop = False
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label11.Location = New System.Drawing.Point(115, -58)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(414, 24)
        Me.Label11.TabIndex = 30
        Me.Label11.Text = "ASPECTOS GENERALES DEL PROYECTO"
        '
        'Button1
        '
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer), CType(CType(87, Byte), Integer))
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.Button1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Location = New System.Drawing.Point(250, 374)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(94, 38)
        Me.Button1.TabIndex = 29
        Me.Button1.Text = "OK"
        Me.Button1.UseVisualStyleBackColor = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(182, 18)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(254, 24)
        Me.Label1.TabIndex = 31
        Me.Label1.Text = "IMAGEN DEL PROYECTO"
        '
        'Form_ImagenProyecto
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(600, 427)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.P_ImagenProyecto)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.Name = "Form_ImagenProyecto"
        Me.Text = "Imagen del Proyecto"
        CType(Me.P_ImagenProyecto, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents P_ImagenProyecto As PictureBox
    Friend WithEvents Label11 As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Label1 As Label
End Class
