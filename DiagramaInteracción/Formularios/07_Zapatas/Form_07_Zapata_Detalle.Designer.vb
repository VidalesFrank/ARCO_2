<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form_07_Zapata_Detalle
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()

        Me.PanelEncabezado = New System.Windows.Forms.Panel()
        Me.LblTitulo = New System.Windows.Forms.Label()
        Me.LblSubtitulo = New System.Windows.Forms.Label()
        Me.LblEstadoGeneral = New System.Windows.Forms.Label()
        Me.PanelSeleccionCombo = New System.Windows.Forms.Panel()
        Me.LblCombo = New System.Windows.Forms.Label()
        Me.CmbCombinacion = New System.Windows.Forms.ComboBox()
        Me.TabDetalle = New System.Windows.Forms.TabControl()
        Me.TabPresiones = New System.Windows.Forms.TabPage()
        Me.TabCapacidadDemanda = New System.Windows.Forms.TabPage()
        Me.TabEnvolvente = New System.Windows.Forms.TabPage()
        Me.TabSeccionesCriticas = New System.Windows.Forms.TabPage()

        '
        'PanelEncabezado
        '
        Me.PanelEncabezado.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelEncabezado.Height = 90
        Me.PanelEncabezado.BackColor = System.Drawing.Color.FromArgb(87, 87, 87)
        Me.PanelEncabezado.Padding = New System.Windows.Forms.Padding(12, 8, 12, 8)
        '
        'LblTitulo
        '
        Me.LblTitulo.AutoSize = False
        Me.LblTitulo.Dock = System.Windows.Forms.DockStyle.Top
        Me.LblTitulo.Height = 28
        Me.LblTitulo.Font = New System.Drawing.Font("Segoe UI", 13.0!, System.Drawing.FontStyle.Bold)
        Me.LblTitulo.ForeColor = System.Drawing.Color.White
        Me.LblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LblTitulo.Text = "Detalle de zapata"
        '
        'LblSubtitulo
        '
        Me.LblSubtitulo.AutoSize = False
        Me.LblSubtitulo.Dock = System.Windows.Forms.DockStyle.Top
        Me.LblSubtitulo.Height = 24
        Me.LblSubtitulo.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.LblSubtitulo.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220)
        Me.LblSubtitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LblSubtitulo.Text = ""
        '
        'LblEstadoGeneral
        '
        Me.LblEstadoGeneral.AutoSize = False
        Me.LblEstadoGeneral.Dock = System.Windows.Forms.DockStyle.Top
        Me.LblEstadoGeneral.Height = 22
        Me.LblEstadoGeneral.Font = New System.Drawing.Font("Segoe UI", 9.5!, System.Drawing.FontStyle.Bold)
        Me.LblEstadoGeneral.ForeColor = System.Drawing.Color.FromArgb(200, 240, 200)
        Me.LblEstadoGeneral.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.LblEstadoGeneral.Text = ""
        '
        Me.PanelEncabezado.Controls.Add(Me.LblEstadoGeneral)
        Me.PanelEncabezado.Controls.Add(Me.LblSubtitulo)
        Me.PanelEncabezado.Controls.Add(Me.LblTitulo)

        '
        'PanelSeleccionCombo
        '
        Me.PanelSeleccionCombo.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelSeleccionCombo.Height = 44
        Me.PanelSeleccionCombo.BackColor = System.Drawing.Color.FromArgb(240, 240, 240)
        Me.PanelSeleccionCombo.Padding = New System.Windows.Forms.Padding(12, 8, 12, 8)
        '
        'LblCombo
        '
        Me.LblCombo.AutoSize = True
        Me.LblCombo.Location = New System.Drawing.Point(12, 14)
        Me.LblCombo.Font = New System.Drawing.Font("Segoe UI", 9.5!, System.Drawing.FontStyle.Bold)
        Me.LblCombo.Text = "Combinación:"
        '
        'CmbCombinacion
        '
        Me.CmbCombinacion.Location = New System.Drawing.Point(110, 10)
        Me.CmbCombinacion.Size = New System.Drawing.Size(400, 26)
        Me.CmbCombinacion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.CmbCombinacion.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        '
        Me.PanelSeleccionCombo.Controls.Add(Me.CmbCombinacion)
        Me.PanelSeleccionCombo.Controls.Add(Me.LblCombo)

        '
        'TabDetalle
        '
        Me.TabDetalle.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabDetalle.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.TabDetalle.Controls.Add(Me.TabPresiones)
        Me.TabDetalle.Controls.Add(Me.TabCapacidadDemanda)
        Me.TabDetalle.Controls.Add(Me.TabEnvolvente)
        Me.TabDetalle.Controls.Add(Me.TabSeccionesCriticas)

        '
        'TabPresiones
        '
        Me.TabPresiones.Text = "Presiones bajo la zapata"
        Me.TabPresiones.BackColor = System.Drawing.Color.White
        Me.TabPresiones.Padding = New System.Windows.Forms.Padding(6)
        '
        'TabCapacidadDemanda
        '
        Me.TabCapacidadDemanda.Text = "Capacidad vs demanda"
        Me.TabCapacidadDemanda.BackColor = System.Drawing.Color.White
        Me.TabCapacidadDemanda.AutoScroll = True
        Me.TabCapacidadDemanda.Padding = New System.Windows.Forms.Padding(6)
        '
        'TabEnvolvente
        '
        Me.TabEnvolvente.Text = "Envolvente por combinación"
        Me.TabEnvolvente.BackColor = System.Drawing.Color.White
        Me.TabEnvolvente.Padding = New System.Windows.Forms.Padding(6)
        '
        'TabSeccionesCriticas
        '
        Me.TabSeccionesCriticas.Text = "Secciones críticas"
        Me.TabSeccionesCriticas.BackColor = System.Drawing.Color.White
        Me.TabSeccionesCriticas.Padding = New System.Windows.Forms.Padding(6)

        '
        'Form_07_Zapata_Detalle
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1200, 850)
        Me.MinimumSize = New System.Drawing.Size(900, 620)
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.BackColor = System.Drawing.Color.White
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.Text = "Detalle de zapata"
        Me.Controls.Add(Me.TabDetalle)
        Me.Controls.Add(Me.PanelSeleccionCombo)
        Me.Controls.Add(Me.PanelEncabezado)

    End Sub

    Friend WithEvents PanelEncabezado As System.Windows.Forms.Panel
    Friend WithEvents LblTitulo As System.Windows.Forms.Label
    Friend WithEvents LblSubtitulo As System.Windows.Forms.Label
    Friend WithEvents LblEstadoGeneral As System.Windows.Forms.Label
    Friend WithEvents PanelSeleccionCombo As System.Windows.Forms.Panel
    Friend WithEvents LblCombo As System.Windows.Forms.Label
    Friend WithEvents CmbCombinacion As System.Windows.Forms.ComboBox
    Friend WithEvents TabDetalle As System.Windows.Forms.TabControl
    Friend WithEvents TabPresiones As System.Windows.Forms.TabPage
    Friend WithEvents TabCapacidadDemanda As System.Windows.Forms.TabPage
    Friend WithEvents TabEnvolvente As System.Windows.Forms.TabPage
    Friend WithEvents TabSeccionesCriticas As System.Windows.Forms.TabPage

End Class
