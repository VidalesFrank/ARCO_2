Imports System.Diagnostics
Imports System.IO

''' <summary>
''' Captura los datos de portada y los módulos a incluir en el Informe Técnico ejecutivo
''' (.docx) — ver <see cref="InformeTecnicoService"/>. Se abre desde el menú principal
''' (Form_00_PaginaPrincipal).
''' </summary>
Public Class Form_Informe_Tecnico
    Inherits Form

    Private ReadOnly _proyecto As Proyecto

    Private _txtCliente As TextBox
    Private _txtResponsable As TextBox
    Private _txtAsunto As TextBox
    Private _dtpFecha As DateTimePicker
    Private _txtCodigo As TextBox
    Private _clbModulos As CheckedListBox
    Private _btnGenerar As Button
    Private _lblEstado As Label

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_Informe_Tecnico(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Informe Técnico Ejecutivo"
        Me.Size = New Size(580, 560)
        Me.MinimumSize = New Size(520, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 248)

        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ' ── Encabezado ───────────────────────────────────────────────────────
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 60, .BackColor = Color.FromArgb(87, 87, 87)}
        Dim lblTit As New Label With {
            .Text = "  Informe Técnico Ejecutivo",
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

        Dim lb1 = EtiquetaY("Cliente")
        _txtCliente = New TextBox With {.Location = New Point(0, y), .Width = 520}
        y += 32

        Dim lb2 = EtiquetaY("Responsable")
        _txtResponsable = New TextBox With {.Location = New Point(0, y), .Width = 520}
        y += 32

        Dim lb3 = EtiquetaY("Asunto / nombre del proyecto")
        _txtAsunto = New TextBox With {
            .Location = New Point(0, y), .Width = 520,
            .Text = If(_proyecto?.Info IsNot Nothing, _proyecto.Info.Nombre, "")
        }
        y += 32

        Dim lb4 = EtiquetaY("Fecha")
        _dtpFecha = New DateTimePicker With {
            .Location = New Point(0, y), .Width = 200,
            .Format = DateTimePickerFormat.Long
        }
        Dim lb5 As New Label With {
            .Text = "Código de proyecto",
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .Location = New Point(220, y - 20), .AutoSize = True
        }
        _txtCodigo = New TextBox With {.Location = New Point(220, y), .Width = 200}
        y += 36

        Dim lb6 = EtiquetaY("Módulos a incluir")
        _clbModulos = New CheckedListBox With {
            .Location = New Point(0, y), .Width = 420, .Height = 110,
            .CheckOnClick = True
        }

        Dim nPilas = Funciones_Programa.ContarElementosMod("Pilas", _proyecto)
        Dim nCols = Funciones_Programa.ContarElementosMod("Columnas", _proyecto)
        Dim nVigas = Funciones_Programa.ContarElementosMod("Vigas", _proyecto)
        Dim nNervios = Funciones_Programa.ContarElementosMod("Nervios", _proyecto)

        _clbModulos.Items.Add($"Pilas ({nPilas})", nPilas > 0)
        _clbModulos.Items.Add($"Columnas ({nCols})", nCols > 0)
        _clbModulos.Items.Add($"Vigas ({nVigas})", nVigas > 0)
        _clbModulos.Items.Add($"Nervios ({nNervios})", nNervios > 0)
        y += 118

        _lblEstado = New Label With {
            .Location = New Point(0, y), .Width = 520, .Height = 36,
            .Font = New Font("Segoe UI", 8.5, FontStyle.Italic),
            .ForeColor = Color.FromArgb(120, 120, 120),
            .Text = "El informe incluye: portada, resumen ejecutivo, planta por módulo, " &
                    "gráficos de cumplimiento, barras por piso y top-10 elementos críticos."
        }
        y += 42

        panelInfo.Controls.AddRange(New Control() {
            lb1, _txtCliente, lb2, _txtResponsable, lb3, _txtAsunto,
            lb4, lb5, _dtpFecha, _txtCodigo, lb6, _clbModulos, _lblEstado
        })

        ' ── Botones ──────────────────────────────────────────────────────────
        Dim panelBtn As New Panel With {.Dock = DockStyle.Bottom, .Height = 60, .BackColor = Color.FromArgb(240, 240, 240)}

        _btnGenerar = New Button With {
            .Text = "Generar informe (.docx)",
            .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
            .BackColor = Color.FromArgb(27, 175, 122),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(300, 12),
            .Size = New Size(250, 36),
            .Cursor = Cursors.Hand
        }
        _btnGenerar.FlatAppearance.BorderSize = 0
        AddHandler _btnGenerar.Click, AddressOf BtnGenerar_Click

        Dim btnCancelar As New Button With {
            .Text = "Cancelar",
            .Font = New Font("Segoe UI", 10),
            .BackColor = Color.FromArgb(230, 230, 230),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .FlatStyle = FlatStyle.Flat,
            .Location = New Point(200, 12),
            .Size = New Size(90, 36),
            .Cursor = Cursors.Hand
        }
        btnCancelar.FlatAppearance.BorderSize = 0
        AddHandler btnCancelar.Click, Sub() Me.Close()

        panelBtn.Controls.Add(_btnGenerar)
        panelBtn.Controls.Add(btnCancelar)

        Me.Controls.Add(panelInfo)
        Me.Controls.Add(panelBtn)
        Me.Controls.Add(panelTop)
    End Sub

    ' =========================================================================
    Private Sub BtnGenerar_Click(sender As Object, e As EventArgs)
        If _proyecto Is Nothing Then
            MessageBox.Show(Me, "No hay proyecto cargado.", "ARCO",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim seleccionados = Enumerable.Range(0, _clbModulos.Items.Count).
                            Where(Function(i) _clbModulos.GetItemChecked(i)).ToList()
        If seleccionados.Count = 0 Then
            MessageBox.Show(Me, "Seleccioná al menos un módulo para incluir en el informe.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim opciones As New OpcionesInformeTecnico With {
            .Fecha = _dtpFecha.Value,
            .CodigoProyecto = _txtCodigo.Text.Trim(),
            .Cliente = _txtCliente.Text.Trim(),
            .Responsable = _txtResponsable.Text.Trim(),
            .AsuntoProyecto = _txtAsunto.Text.Trim(),
            .IncluirPilas = _clbModulos.GetItemChecked(0),
            .IncluirColumnas = _clbModulos.GetItemChecked(1),
            .IncluirVigas = _clbModulos.GetItemChecked(2),
            .IncluirNervios = _clbModulos.GetItemChecked(3)
        }

        Dim nombreBase = NombreArchivoBase(opciones)
        Dim ruta As String
        Using dlg As New SaveFileDialog() With {
            .Filter = "Documento de Word (*.docx)|*.docx",
            .DefaultExt = "docx",
            .FileName = nombreBase
        }
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            ruta = dlg.FileName
        End Using

        Me.Cursor = Cursors.WaitCursor
        _btnGenerar.Enabled = False
        Try
            InformeTecnicoService.Generar(_proyecto, opciones, ruta)
            MessageBox.Show(Me,
                "Informe generado correctamente:" & vbCrLf & ruta,
                "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Try
                Process.Start(New ProcessStartInfo(ruta) With {.UseShellExecute = True})
            Catch
                ' Si Word no está asociado, no es un error del informe.
            End Try
            Me.Close()
        Catch ex As Exception
            Logger.Error(ex, "Form_Informe_Tecnico.BtnGenerar_Click", ruta)
            MessageBox.Show(Me,
                "No se pudo generar el informe:" & vbCrLf & ex.Message,
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Default
            _btnGenerar.Enabled = True
        End Try
    End Sub

    Private Function NombreArchivoBase(op As OpcionesInformeTecnico) As String
        Dim codigo = If(String.IsNullOrWhiteSpace(op.CodigoProyecto), "ARCO", op.CodigoProyecto.Trim())
        Dim fecha = op.Fecha.ToString("yyyy-MM-dd")
        Return $"{codigo}_InformeTecnico_{fecha}"
    End Function

End Class
