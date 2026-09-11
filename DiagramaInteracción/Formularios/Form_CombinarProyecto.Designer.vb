<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form_CombinarProyecto
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso (components IsNot Nothing) Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub InitializeComponent()
        Me.PanelTop = New System.Windows.Forms.Panel()
        Me.LblRuta = New System.Windows.Forms.Label()
        Me.TxtRuta = New System.Windows.Forms.TextBox()
        Me.BtnExaminar = New System.Windows.Forms.Button()
        Me.GrpModulos = New System.Windows.Forms.GroupBox()
        Me.ClbModulos = New System.Windows.Forms.CheckedListBox()
        Me.PanelAdvertencia = New System.Windows.Forms.Panel()
        Me.LblAdvertencia = New System.Windows.Forms.Label()
        Me.PanelBotones = New System.Windows.Forms.Panel()
        Me.BtnCancelar = New System.Windows.Forms.Button()
        Me.BtnCombinar = New System.Windows.Forms.Button()
        Me.LblNota = New System.Windows.Forms.Label()

        Me.PanelTop.SuspendLayout()
        Me.GrpModulos.SuspendLayout()
        Me.PanelAdvertencia.SuspendLayout()
        Me.PanelBotones.SuspendLayout()
        Me.SuspendLayout()

        ' ── PanelTop ───────────────────────────────────────────────────────────
        Me.PanelTop.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelTop.Height = 60
        Me.PanelTop.Padding = New System.Windows.Forms.Padding(10, 8, 10, 4)
        Me.PanelTop.Controls.Add(Me.BtnExaminar)
        Me.PanelTop.Controls.Add(Me.TxtRuta)
        Me.PanelTop.Controls.Add(Me.LblRuta)

        ' ── LblRuta ────────────────────────────────────────────────────────────
        Me.LblRuta.AutoSize = True
        Me.LblRuta.Font = New System.Drawing.Font("Segoe UI", 9)
        Me.LblRuta.Location = New System.Drawing.Point(10, 18)
        Me.LblRuta.Text = "Archivo fuente:"

        ' ── TxtRuta ────────────────────────────────────────────────────────────
        Me.TxtRuta.Location = New System.Drawing.Point(115, 15)
        Me.TxtRuta.Width = 280
        Me.TxtRuta.ReadOnly = True
        Me.TxtRuta.Font = New System.Drawing.Font("Segoe UI", 9)
        Me.TxtRuta.BackColor = System.Drawing.Color.WhiteSmoke

        ' ── BtnExaminar ────────────────────────────────────────────────────────
        Me.BtnExaminar.Text = "Examinar..."
        Me.BtnExaminar.Location = New System.Drawing.Point(402, 13)
        Me.BtnExaminar.Width = 90
        Me.BtnExaminar.Height = 26
        Me.BtnExaminar.Font = New System.Drawing.Font("Segoe UI", 9)
        Me.BtnExaminar.FlatStyle = System.Windows.Forms.FlatStyle.System

        ' ── GrpModulos ─────────────────────────────────────────────────────────
        Me.GrpModulos.Text = "Módulos del archivo fuente"
        Me.GrpModulos.Font = New System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Regular)
        Me.GrpModulos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GrpModulos.Padding = New System.Windows.Forms.Padding(8)
        Me.GrpModulos.Controls.Add(Me.ClbModulos)

        ' ── ClbModulos ─────────────────────────────────────────────────────────
        Me.ClbModulos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ClbModulos.Font = New System.Drawing.Font("Consolas", 9.5F)
        Me.ClbModulos.CheckOnClick = True
        Me.ClbModulos.IntegralHeight = False
        Me.ClbModulos.BorderStyle = System.Windows.Forms.BorderStyle.None

        ' ── PanelAdvertencia ───────────────────────────────────────────────────
        Me.PanelAdvertencia.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.PanelAdvertencia.Height = 80
        Me.PanelAdvertencia.BackColor = System.Drawing.Color.FromArgb(255, 248, 220)
        Me.PanelAdvertencia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PanelAdvertencia.Padding = New System.Windows.Forms.Padding(8, 4, 8, 4)
        Me.PanelAdvertencia.Visible = False
        Me.PanelAdvertencia.Controls.Add(Me.LblAdvertencia)

        ' ── LblAdvertencia ─────────────────────────────────────────────────────
        Me.LblAdvertencia.Dock = System.Windows.Forms.DockStyle.Fill
        Me.LblAdvertencia.Font = New System.Drawing.Font("Segoe UI", 8.5F)
        Me.LblAdvertencia.ForeColor = System.Drawing.Color.FromArgb(160, 80, 0)
        Me.LblAdvertencia.Text = ""

        ' ── LblNota ────────────────────────────────────────────────────────────
        Me.LblNota.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.LblNota.Height = 28
        Me.LblNota.Font = New System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic)
        Me.LblNota.ForeColor = System.Drawing.Color.Gray
        Me.LblNota.Text = "  Cada módulo lleva su propia geometría (Joints/Frames) — no afecta otros módulos."
        Me.LblNota.TextAlign = System.Drawing.ContentAlignment.MiddleLeft

        ' ── PanelBotones ───────────────────────────────────────────────────────
        Me.PanelBotones.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.PanelBotones.Height = 48
        Me.PanelBotones.Padding = New System.Windows.Forms.Padding(0, 8, 10, 4)
        Me.PanelBotones.Controls.Add(Me.BtnCombinar)
        Me.PanelBotones.Controls.Add(Me.BtnCancelar)

        ' ── BtnCancelar ────────────────────────────────────────────────────────
        Me.BtnCancelar.Text = "Cancelar"
        Me.BtnCancelar.Size = New System.Drawing.Size(90, 28)
        Me.BtnCancelar.Location = New System.Drawing.Point(310, 8)
        Me.BtnCancelar.Font = New System.Drawing.Font("Segoe UI", 9)
        Me.BtnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.BtnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel

        ' ── BtnCombinar ────────────────────────────────────────────────────────
        Me.BtnCombinar.Text = "Combinar →"
        Me.BtnCombinar.Size = New System.Drawing.Size(100, 28)
        Me.BtnCombinar.Location = New System.Drawing.Point(406, 8)
        Me.BtnCombinar.Font = New System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        Me.BtnCombinar.FlatStyle = System.Windows.Forms.FlatStyle.System
        Me.BtnCombinar.Enabled = False

        ' ── Form ───────────────────────────────────────────────────────────────
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0F, 15.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(510, 420)
        Me.Font = New System.Drawing.Font("Segoe UI", 9)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Combinar módulos desde archivo"

        Me.Controls.Add(Me.GrpModulos)
        Me.Controls.Add(Me.LblNota)
        Me.Controls.Add(Me.PanelAdvertencia)
        Me.Controls.Add(Me.PanelBotones)
        Me.Controls.Add(Me.PanelTop)

        Me.PanelTop.ResumeLayout(False)
        Me.GrpModulos.ResumeLayout(False)
        Me.PanelAdvertencia.ResumeLayout(False)
        Me.PanelBotones.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents PanelTop As System.Windows.Forms.Panel
    Friend WithEvents LblRuta As System.Windows.Forms.Label
    Friend WithEvents TxtRuta As System.Windows.Forms.TextBox
    Friend WithEvents BtnExaminar As System.Windows.Forms.Button
    Friend WithEvents GrpModulos As System.Windows.Forms.GroupBox
    Friend WithEvents ClbModulos As System.Windows.Forms.CheckedListBox
    Friend WithEvents PanelAdvertencia As System.Windows.Forms.Panel
    Friend WithEvents LblAdvertencia As System.Windows.Forms.Label
    Friend WithEvents LblNota As System.Windows.Forms.Label
    Friend WithEvents PanelBotones As System.Windows.Forms.Panel
    Friend WithEvents BtnCancelar As System.Windows.Forms.Button
    Friend WithEvents BtnCombinar As System.Windows.Forms.Button

End Class
