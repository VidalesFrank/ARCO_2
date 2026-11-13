Imports System.Diagnostics
Imports System.IO

''' <summary>
''' Informe Inicial — exporta a Excel las 11 tablas de verificación que replican la
''' plantilla P58501_I001 (sin la narrativa). El servicio lista TODOS los elementos
''' y los ordena de piso inferior → superior. Ver <see cref="InformeInicialExcelService"/>.
''' </summary>
Public Class Form_Informe_Inicial
    Inherits Form

    Private ReadOnly _proyecto As Proyecto

    Private _txtNombreProyecto As TextBox
    Private _txtCodigo As TextBox
    Private _dtpFecha As DateTimePicker
    Private _clbSecciones As CheckedListBox
    Private _chkDetalleColumnas As CheckBox
    Private _chkRVC As CheckBox
    Private _lblEstado As Label

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_Informe_Inicial(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Informe Inicial — Tablas de verificación (.xlsx)"
        Me.Size = New Size(620, 620)
        Me.MinimumSize = New Size(560, 540)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 248)

        ' ── Encabezado ───────────────────────────────────────────────────────
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 60, .BackColor = Color.FromArgb(87, 87, 87)}
        Dim lblTit As New Label With {
            .Text = "  Informe Inicial — Tablas de verificación",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 12, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblTit)

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

        Dim lbl1 = EtiquetaY("Nombre del proyecto")
        _txtNombreProyecto = New TextBox With {.Location = New Point(0, y), .Width = 560,
                                                .Text = If(_proyecto?.Info IsNot Nothing, _proyecto.Info.Nombre, "")}
        y += 32

        Dim lbl2 = EtiquetaY("Fecha")
        _dtpFecha = New DateTimePicker With {.Location = New Point(0, y), .Width = 180, .Format = DateTimePickerFormat.Long}
        Dim lbl3 As New Label With {.Text = "Código de proyecto", .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
                                     .ForeColor = Color.FromArgb(87, 87, 87), .Location = New Point(220, y - 20), .AutoSize = True}
        _txtCodigo = New TextBox With {.Location = New Point(220, y), .Width = 200}
        y += 32

        Dim lbl4 = EtiquetaY("Módulos a incluir")
        _clbSecciones = New CheckedListBox With {.Location = New Point(0, y), .Width = 560, .Height = 130, .CheckOnClick = True}
        Dim tienePilas = Funciones_Programa.ContarElementosMod("Pilas", _proyecto) > 0
        Dim tieneColumnas = Funciones_Programa.ContarElementosMod("Columnas", _proyecto) > 0
        Dim tieneVigas = Funciones_Programa.ContarElementosMod("Vigas", _proyecto) > 0
        Dim tieneNervios = Funciones_Programa.ContarElementosMod("Nervios", _proyecto) > 0
        Dim tieneVF = If(_proyecto?.Elementos?.VigasFundacion?.Elementos, New List(Of cVigaFundacion)).Count > 0

        _clbSecciones.Items.Add("Columnas (T11, T12, T13, T14)", tieneColumnas)
        _clbSecciones.Items.Add("Vigas estructurales (T17)", tieneVigas)
        _clbSecciones.Items.Add("Nervios / Losas nervadas (T19, T20)", tieneNervios)
        _clbSecciones.Items.Add("Pilas (T22, T23, T24)", tienePilas)
        _clbSecciones.Items.Add("Vigas de cimentación (T28)", tieneVF)
        y += 140

        ' Checkbox opcional: agrega T11_Detalle y T12_Detalle (una fila por tramo).
        ' Útil para auditorías internas; no se incluye en el reporte de Curaduría.
        _chkDetalleColumnas = New CheckBox With {
            .Text = "Incluir detalle por tramo de columnas (hojas extras T11_Detalle y T12_Detalle)",
            .Location = New Point(0, y),
            .AutoSize = True,
            .Checked = False,
            .ForeColor = Color.FromArgb(87, 87, 87)
        }
        y += 24

        ' Checkbox opcional: T15 relación viga-columna (NSR-10 C.21.6.2).
        _chkRVC = New CheckBox With {
            .Text = "Incluir T15 — Relación viga-columna (NSR-10 C.21.6.2)",
            .Location = New Point(0, y),
            .AutoSize = True,
            .Checked = tieneColumnas AndAlso tieneVigas,
            .ForeColor = Color.FromArgb(87, 87, 87)
        }
        y += 28

        _lblEstado = New Label With {
            .Text = "",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .Location = New Point(0, y),
            .AutoSize = True,
            .MaximumSize = New Size(560, 0)
        }

        panelInfo.Controls.AddRange({lbl1, _txtNombreProyecto, lbl2, _dtpFecha, lbl3, _txtCodigo,
                                      lbl4, _clbSecciones, _chkDetalleColumnas, _chkRVC, _lblEstado})

        ' ── Botones inferiores ────────────────────────────────────────────────
        Dim panelBot As New Panel With {.Dock = DockStyle.Bottom, .Height = 52, .BackColor = Color.FromArgb(240, 240, 244)}
        Dim btnGenerar As New Button With {
            .Text = "  Exportar a Excel…",
            .Size = New Size(170, 32),
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(21, 130, 70),
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
                                        btnGenerar.Location = New Point(panelBot.Width - 278, 10)
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
        If _proyecto Is Nothing Then
            MessageBox.Show("No hay proyecto cargado.", "Informe Inicial", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim sufijo As String = If(Not String.IsNullOrWhiteSpace(_txtNombreProyecto.Text),
                                   "_" & _txtNombreProyecto.Text.Trim(), "")
        Dim nombreArchivo As String = "InformeInicial" & sufijo & "_" & DateTime.Now.ToString("yyyyMMdd") & ".xlsx"

        Dim dlg As New SaveFileDialog With {
            .Title = "Guardar Informe Inicial",
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = nombreArchivo
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            _lblEstado.Text = "Generando informe…"
            Me.Refresh()

            Dim opciones As New OpcionesInformeInicial With {
                .Fecha = _dtpFecha.Value,
                .CodigoProyecto = _txtCodigo.Text.Trim(),
                .NombreProyecto = _txtNombreProyecto.Text.Trim(),
                .IncluirColumnas = _clbSecciones.GetItemChecked(0),
                .IncluirVigas = _clbSecciones.GetItemChecked(1),
                .IncluirNervios = _clbSecciones.GetItemChecked(2),
                .IncluirPilas = _clbSecciones.GetItemChecked(3),
                .IncluirVigasFundacion = _clbSecciones.GetItemChecked(4),
                .IncluirDetalleColumnas = _chkDetalleColumnas.Checked,
                .IncluirRelacionVigaColumna = _chkRVC.Checked
            }

            InformeInicialExcelService.Generar(_proyecto, opciones, dlg.FileName)

            _lblEstado.Text = "Informe generado correctamente."
            Dim abrir = MessageBox.Show("¿Desea abrir el archivo ahora?", "Informe Inicial generado",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If

        Catch ex As Exception
            _lblEstado.Text = "Error al generar el informe."
            Logger.Error(ex, "Form_Informe_Inicial.Generar_Click")
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Class
