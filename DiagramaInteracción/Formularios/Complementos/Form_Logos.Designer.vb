<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form_Logos
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_Logos))
        Me.P_Logo_EstrucMed = New System.Windows.Forms.PictureBox()
        CType(Me.P_Logo_EstrucMed, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'P_Logo_EstrucMed
        '
        Me.P_Logo_EstrucMed.Image = CType(resources.GetObject("P_Logo_EstrucMed.Image"), System.Drawing.Image)
        Me.P_Logo_EstrucMed.Location = New System.Drawing.Point(12, 22)
        Me.P_Logo_EstrucMed.Name = "P_Logo_EstrucMed"
        Me.P_Logo_EstrucMed.Size = New System.Drawing.Size(288, 82)
        Me.P_Logo_EstrucMed.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.P_Logo_EstrucMed.TabIndex = 0
        Me.P_Logo_EstrucMed.TabStop = False
        '
        'Form_Logos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.Controls.Add(Me.P_Logo_EstrucMed)
        Me.Name = "Form_Logos"
        Me.Text = "Form_Logos"
        CType(Me.P_Logo_EstrucMed, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents P_Logo_EstrucMed As PictureBox
End Class
