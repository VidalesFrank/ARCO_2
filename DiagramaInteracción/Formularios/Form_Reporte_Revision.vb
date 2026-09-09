Imports System.Diagnostics

''' <summary>
''' Reporte de Revisión (.docx) con el estilo de EstrucMed — ver <see cref="ReporteRevisionService"/>.
''' Sigue el mismo patrón de construcción por código de <see cref="Form_Reporte_Proyecto_Completo"/>.
''' </summary>
Public Class Form_Reporte_Revision
    Inherits Form

    Private ReadOnly _proyecto As Proyecto

    Private _txtDestinatarioNombre As TextBox
    Private _txtDestinatarioCargo As TextBox
    Private _txtCliente As TextBox
    Private _txtAsunto As TextBox
    Private _dtpFecha As DateTimePicker
    Private _txtCodigo As TextBox
    Private _clbSecciones As CheckedListBox
    Private _txtDetallesPlanos As TextBox
    Private _lblEstado As Label

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_Reporte_Revision(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Reporte de Revisión"
        Me.Size = New Size(620, 640)
        Me.MinimumSize = New Size(560, 560)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 248)

        ' ── Encabezado ───────────────────────────────────────────────────────
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 60, .BackColor = Color.FromArgb(87, 87, 87)}
        Dim lblTit As New Label With {
            .Text = "  Reporte de Revisión — EstrucMed",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 12, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblTit)

        ' ── Panel central (con scroll) ───────────────────────────────────────
        Dim panelInfo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .AutoScroll = True}
        panelInfo.BackColor = Color.White

        Dim y As Integer = 8
        Dim EtiquetaY = Function(texto As String) As Label
                            Dim lbl As New Label With {
                                .Text = texto,
                                .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
                                .ForeColor = Color.FromArgb(87, 87, 87),
                                .Location = New Point(0, y),
                                .AutoSize = True
                            }
                            y += 20
                            Return lbl
                        End Function

        Dim lbl1 = EtiquetaY("Destinatario (nombre y cargo)")
        _txtDestinatarioNombre = New TextBox With {.Location = New Point(0, y), .Width = 560}
        y += 26
        _txtDestinatarioCargo = New TextBox With {.Location = New Point(0, y), .Width = 560}
        y += 32

        Dim lbl2 = EtiquetaY("Cliente")
        _txtCliente = New TextBox With {.Location = New Point(0, y), .Width = 560}
        y += 32

        Dim lbl3 = EtiquetaY("Asunto / nombre del proyecto")
        _txtAsunto = New TextBox With {.Location = New Point(0, y), .Width = 560,
                                        .Text = If(_proyecto?.Info IsNot Nothing, _proyecto.Info.Nombre, "")}
        y += 32

        Dim lbl4 = EtiquetaY("Fecha")
        _dtpFecha = New DateTimePicker With {.Location = New Point(0, y), .Width = 180, .Format = DateTimePickerFormat.Long}
        Dim lbl5 As New Label With {.Text = "Código de proyecto", .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
                                     .ForeColor = Color.FromArgb(87, 87, 87), .Location = New Point(220, y - 20), .AutoSize = True}
        _txtCodigo = New TextBox With {.Location = New Point(220, y), .Width = 200}
        y += 32

        Dim lbl6 = EtiquetaY("Secciones a incluir")
        _clbSecciones = New CheckedListBox With {.Location = New Point(0, y), .Width = 300, .Height = 96, .CheckOnClick = True}
        _clbSecciones.Items.Add("Cimentaciones (Pilas + Vigas de cimentación)", True)
        _clbSecciones.Items.Add("Muros", True)
        _clbSecciones.Items.Add("Vigas", True)
        _clbSecciones.Items.Add("Losas", True)
        _clbSecciones.Items.Add("Detalles en Planos", True)
        y += 104

        Dim lbl7 = EtiquetaY("Detalles en Planos (uno por línea, se listan como viñetas)")
        _txtDetallesPlanos = New TextBox With {
            .Location = New Point(0, y), .Width = 560, .Height = 120,
            .Multiline = True, .ScrollBars = ScrollBars.Vertical, .AcceptsReturn = True
        }
        y += 130

        _lblEstado = New Label With {
            .Text = "",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .Location = New Point(0, y),
            .AutoSize = True
        }

        panelInfo.Controls.AddRange({lbl1, _txtDestinatarioNombre, _txtDestinatarioCargo, lbl2, _txtCliente,
                                      lbl3, _txtAsunto, lbl4, _dtpFecha, lbl5, _txtCodigo, lbl6, _clbSecciones,
                                      lbl7, _txtDetallesPlanos, _lblEstado})

        ' ── Botones inferiores ────────────────────────────────────────────────
        Dim panelBot As New Panel With {.Dock = DockStyle.Bottom, .Height = 52, .BackColor = Color.FromArgb(240, 240, 244)}
        Dim btnGenerar As New Button With {
            .Text = "  Generar reporte…",
            .Size = New Size(160, 32),
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(87, 87, 87),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnGenerar.FlatAppearance.BorderSize = 0
        Dim btnCerrar As New Button With {
            .Text = "Cerrar",
            .Size = New Size(80, 32),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.Cancel
        }
        AddHandler panelBot.Resize, Sub(s, e)
                                        btnCerrar.Location = New Point(panelBot.Width - 96, 10)
                                        btnGenerar.Location = New Point(panelBot.Width - 268, 10)
                                    End Sub
        AddHandler btnGenerar.Click, AddressOf Generar_Click
        panelBot.Controls.AddRange({btnGenerar, btnCerrar})
        Me.CancelButton = btnCerrar

        Me.Controls.Add(panelInfo)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBot)
    End Sub

    ' =========================================================================
    Private Sub Generar_Click(sender As Object, e As EventArgs)
        Dim nombreArchivo As String = "Reporte_Revision_" & DateTime.Now.ToString("yyyyMMdd")
        If _proyecto IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(_proyecto.Info.Nombre) Then
            nombreArchivo = "Reporte_Revision_" & _proyecto.Info.Nombre & "_" & DateTime.Now.ToString("yyyyMMdd")
        End If

        Dim dlg As New SaveFileDialog With {
            .Title = "Guardar Reporte de Revisión",
            .Filter = "Word (*.docx)|*.docx",
            .FileName = nombreArchivo
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            _lblEstado.Text = "Generando reporte…"
            Me.Refresh()

            Dim opciones As New OpcionesReporteRevision With {
                .Fecha = _dtpFecha.Value,
                .CodigoProyecto = _txtCodigo.Text.Trim(),
                .DestinatarioNombre = _txtDestinatarioNombre.Text.Trim(),
                .DestinatarioCargo = _txtDestinatarioCargo.Text.Trim(),
                .Cliente = _txtCliente.Text.Trim(),
                .AsuntoProyecto = _txtAsunto.Text.Trim(),
                .IncluirCimentaciones = _clbSecciones.GetItemChecked(0),
                .IncluirMuros = _clbSecciones.GetItemChecked(1),
                .IncluirVigas = _clbSecciones.GetItemChecked(2),
                .IncluirLosas = _clbSecciones.GetItemChecked(3),
                .IncluirDetallesPlanos = _clbSecciones.GetItemChecked(4),
                .DetallesPlanos = _txtDetallesPlanos.Lines.ToList()
            }

            ReporteRevisionService.GenerarReporte(_proyecto, opciones, dlg.FileName)

            _lblEstado.Text = "Reporte generado correctamente."
            Dim abrir = MessageBox.Show("¿Desea abrir el archivo ahora?", "Reporte generado",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If

        Catch ex As Exception
            _lblEstado.Text = "Error al generar el reporte."
            Logger.Error(ex, "Form_Reporte_Revision.Generar_Click")
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Class
