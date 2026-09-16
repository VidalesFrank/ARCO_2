Imports System.Diagnostics

''' <summary>
''' Diálogo para capturar las opciones del "Informe de Estado del Proyecto" (revisión conceptual/
''' preliminar de Muros: densidad, derivas, ALR, muros protagónicos, espesores, factor de forma) y
''' generarlo en .docx — ver <see cref="InformeEstadoMurosService"/>. Sigue el mismo patrón de
''' construcción por código que <see cref="Form_Reporte_Revision"/>.
''' </summary>
Public Class Form_InformeEstadoMuros
    Inherits Form

    Private ReadOnly _proyecto As Proyecto

    ' Información general
    Private _txtNombre As TextBox
    Private _txtDireccion As TextBox
    Private _txtPropietario As TextBox
    Private _txtSistema As TextBox
    Private _txtPisos As TextBox
    Private _txtArea As TextBox
    Private _txtElaboradoPor As TextBox
    Private _txtRevisadoPor As TextBox
    Private _dtpFecha As DateTimePicker
    Private _txtCodigo As TextBox

    ' Checklists
    Private _filasInfoRecibida As New List(Of (Estado As ComboBox, Observ As TextBox))
    Private _filasAvance As New List(Of (Estado As ComboBox, Observ As TextBox))

    ' Análisis conceptual
    Private _txtObsDensidad As TextBox
    Private _txtObsDerivas As TextBox
    Private _txtObsALR As TextBox

    ' Recomendaciones
    Private _txtRecomendaciones As TextBox

    Private _lblEstado As Label

    Private Shared ReadOnly OpcionesEstadoRecibida() As String = {"Recibido", "Parcial", "Pendiente"}
    Private Shared ReadOnly OpcionesEstadoAvance() As String = {"Completo", "En proceso", "Pendiente"}

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_InformeEstadoMuros(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
        PrellenarDesdeProyecto()
    End Sub

    ' =========================================================================
    '  UI
    ' =========================================================================
    Private Sub BuildUI()
        Me.Text = "Informe de Estado del Proyecto — Muros"
        Me.Size = New Size(720, 680)
        Me.MinimumSize = New Size(640, 520)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 248)

        ' ── Encabezado ───────────────────────────────────────────────────────
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 60, .BackColor = Color.FromArgb(87, 87, 87)}
        Dim lblTit As New Label With {
            .Text = "  Informe de Estado del Proyecto — EstrucMed",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 12, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblTit)

        ' ── Pestañas ────────────────────────────────────────────────────────
        Dim tabs As New TabControl With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 9)}
        tabs.TabPages.Add(ConstruirTabGeneral())
        tabs.TabPages.Add(ConstruirTabChecklists())
        tabs.TabPages.Add(ConstruirTabAnalisisConceptual())
        tabs.TabPages.Add(ConstruirTabRecomendaciones())

        ' ── Botones inferiores ────────────────────────────────────────────────
        Dim panelBot As New Panel With {.Dock = DockStyle.Bottom, .Height = 52, .BackColor = Color.FromArgb(240, 240, 244)}
        _lblEstado = New Label With {
            .Text = "",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .AutoSize = True,
            .Location = New Point(12, 18)
        }
        Dim btnGenerar As New Button With {
            .Text = "  Generar informe…",
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
        panelBot.Controls.AddRange({btnGenerar, btnCerrar, _lblEstado})
        Me.CancelButton = btnCerrar

        Me.Controls.Add(tabs)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBot)
    End Sub

    Private Function EtiquetaCampo(texto As String, x As Integer, y As Integer) As Label
        Return New Label With {
            .Text = texto,
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .Location = New Point(x, y),
            .AutoSize = True
        }
    End Function

    Private Function ConstruirTabGeneral() As TabPage
        Dim tab As New TabPage("Información General") With {.BackColor = Color.White}
        Dim panel As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .AutoScroll = True}
        Dim y As Integer = 8
        Dim controles As New List(Of Control)

        Dim CrearCampo = Function(etiqueta As String, ancho As Integer) As TextBox
                             controles.Add(EtiquetaCampo(etiqueta, 0, y))
                             y += 20
                             Dim caja As New TextBox With {.Location = New Point(0, y), .Width = ancho}
                             controles.Add(caja)
                             y += 32
                             Return caja
                         End Function

        _txtNombre = CrearCampo("Nombre del proyecto", 620)
        _txtDireccion = CrearCampo("Dirección", 620)
        _txtPropietario = CrearCampo("Propietario", 620)
        _txtSistema = CrearCampo("Sistema estructural", 620)

        controles.Add(EtiquetaCampo("Número de pisos", 0, y))
        controles.Add(EtiquetaCampo("Área en planta típica (m²)", 220, y))
        controles.Add(EtiquetaCampo("Fecha del informe", 440, y))
        y += 20
        _txtPisos = New TextBox With {.Location = New Point(0, y), .Width = 190}
        _txtArea = New TextBox With {.Location = New Point(220, y), .Width = 190}
        _dtpFecha = New DateTimePicker With {.Location = New Point(440, y), .Width = 180, .Format = DateTimePickerFormat.Long}
        controles.AddRange({_txtPisos, _txtArea, _dtpFecha})
        y += 32

        _txtElaboradoPor = CrearCampo("Elaborado por", 300)
        controles.Add(EtiquetaCampo("Revisado por", 320, y - 32))
        _txtRevisadoPor = New TextBox With {.Location = New Point(320, y - 12), .Width = 300}
        controles.Add(_txtRevisadoPor)

        controles.Add(EtiquetaCampo("Código de proyecto", 0, y))
        y += 20
        _txtCodigo = New TextBox With {.Location = New Point(0, y), .Width = 200}
        controles.Add(_txtCodigo)

        panel.Controls.AddRange(controles.ToArray())
        tab.Controls.Add(panel)
        Return tab
    End Function

    Private Function ConstruirTabChecklists() As TabPage
        Dim tab As New TabPage("Info. Recibida y Avance") With {.BackColor = Color.White}
        Dim panel As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .AutoScroll = True}
        Dim y As Integer = 8
        Dim controles As New List(Of Control)

        controles.Add(New Label With {.Text = "Información recibida", .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
                                       .ForeColor = Color.FromArgb(87, 87, 87), .Location = New Point(0, y), .AutoSize = True})
        y += 26
        Dim detallesInfoRecibida = {"Planos Arquitectónicos", "Planos Estructurales", "Memorias de cálculo", "Estudio de suelos"}
        y = ConstruirFilasChecklist(controles, y, detallesInfoRecibida, OpcionesEstadoRecibida, _filasInfoRecibida)

        y += 16
        controles.Add(New Label With {.Text = "Detalles de avance", .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
                                       .ForeColor = Color.FromArgb(87, 87, 87), .Location = New Point(0, y), .AutoSize = True})
        y += 26
        Dim detallesAvance = {"Modelo estructural", "Análisis dinámico", "Verificación de derivas", "Verificación de diseño de elementos estructurales"}
        y = ConstruirFilasChecklist(controles, y, detallesAvance, OpcionesEstadoAvance, _filasAvance)

        panel.Controls.AddRange(controles.ToArray())
        tab.Controls.Add(panel)
        Return tab
    End Function

    ''' <summary>Construye una fila (Detalle fijo + ComboBox Estado + TextBox Observaciones) por cada ítem,
    ''' y devuelve los controles editables en <paramref name="destino"/> en el mismo orden.</summary>
    Private Function ConstruirFilasChecklist(controles As List(Of Control), yInicial As Integer, detalles() As String,
                                              opcionesEstado() As String, destino As List(Of (Estado As ComboBox, Observ As TextBox))) As Integer
        Dim y As Integer = yInicial
        For Each detalle In detalles
            controles.Add(New Label With {.Text = detalle, .Location = New Point(0, y + 3), .Width = 260, .AutoSize = False})
            Dim cbo As New ComboBox With {.Location = New Point(265, y), .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cbo.Items.AddRange(opcionesEstado)
            cbo.SelectedIndex = opcionesEstado.Length - 1 ' "Pendiente" por defecto
            Dim txt As New TextBox With {.Location = New Point(395, y), .Width = 260}
            controles.Add(cbo)
            controles.Add(txt)
            destino.Add((cbo, txt))
            y += 30
        Next
        Return y
    End Function

    Private Function ConstruirTabAnalisisConceptual() As TabPage
        Dim tab As New TabPage("Análisis Conceptual") With {.BackColor = Color.White}
        Dim panel As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .AutoScroll = True}
        Dim y As Integer = 8
        Dim controles As New List(Of Control)

        controles.Add(New Label With {
            .Text = "Los gráficos (densidad, derivas, ALR, muros protagónicos, espesores y factor de forma) se generan " &
                    "automáticamente a partir de los macroparámetros ya calculados en el módulo de Muros. " &
                    "Complemente aquí la interpretación técnica que se incluirá junto a cada uno.",
            .Location = New Point(0, y), .Width = 640, .Height = 44
        })
        y += 52

        Dim CrearObservacion = Function(etiqueta As String, alto As Integer) As TextBox
                                   controles.Add(EtiquetaCampo(etiqueta, 0, y))
                                   y += 20
                                   Dim caja As New TextBox With {.Location = New Point(0, y), .Width = 640, .Height = alto,
                                                                  .Multiline = True, .ScrollBars = ScrollBars.Vertical}
                                   controles.Add(caja)
                                   y += alto + 16
                                   Return caja
                               End Function

        _txtObsDensidad = CrearObservacion("Observación — Densidad de muros", 60)
        _txtObsDerivas = CrearObservacion("Observación — Variación de derivas en altura", 80)
        _txtObsALR = CrearObservacion("Observación — Relación de carga axial (ALR)", 80)

        panel.Controls.AddRange(controles.ToArray())
        tab.Controls.Add(panel)
        Return tab
    End Function

    Private Function ConstruirTabRecomendaciones() As TabPage
        Dim tab As New TabPage("Recomendaciones") With {.BackColor = Color.White}
        Dim panel As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .AutoScroll = True}
        Dim lbl = EtiquetaCampo("Análisis preliminar y recomendaciones (uno o más párrafos)", 0, 8)
        _txtRecomendaciones = New TextBox With {
            .Location = New Point(0, 32), .Width = 640, .Height = 380,
            .Multiline = True, .ScrollBars = ScrollBars.Vertical, .AcceptsReturn = True
        }
        panel.Controls.AddRange({lbl, _txtRecomendaciones})
        tab.Controls.Add(panel)
        Return tab
    End Function

    ' =========================================================================
    '  Prellenado desde el proyecto ya calculado
    ' =========================================================================
    Private Sub PrellenarDesdeProyecto()
        If _proyecto?.Info Is Nothing Then Return
        Dim info = _proyecto.Info

        _txtNombre.Text = info.Nombre
        _txtDireccion.Text = info.Direccion
        _txtPropietario.Text = info.Propietario
        _txtPisos.Text = If(info.NPisos > 0, info.NPisos.ToString(), "")
        _txtArea.Text = If(info.Area > 0, info.Area.ToString("0.##"), "")
        _txtRevisadoPor.Text = "Ricardo León Bonett Díaz"

        Try
            _txtSistema.Text = DirectorioSistemaEstructural.dSistemaEstructural(info.SistemaEstructural).NameSistema
        Catch
            _txtSistema.Text = ""
        End Try

        Try
            _txtElaboradoPor.Text = DirectorioResponsables.dResponsables(info.Persona_Responsable).NombreCompleto
        Catch
            _txtElaboradoPor.Text = ""
        End Try

        If _proyecto.Elementos?.Muros IsNot Nothing Then
            Dim muros = _proyecto.Elementos.Muros
            Dim densX As Double = muros.Densidad_X * 100
            Dim densY As Double = muros.Densidad_Y * 100
            If densX > 0 OrElse densY > 0 Then
                _txtObsDensidad.Text = "El proyecto presenta una densidad de muros de " & densX.ToString("0.00") &
                    "% en X y " & densY.ToString("0.00") & "% en Y."
            End If

            Dim protagonicos = muros.Lista_Muros.Where(Function(m) m.TipoMuro = eNumeradores.eTipoMuro.Protagonico).ToList()
            If protagonicos.Count > 0 Then
                Dim alrMax As Double = protagonicos.Max(Function(m) m.ALR_D) * 100
                _txtObsALR.Text = "La relación de carga axial (ALR) máxima entre los muros protagónicos, para condiciones últimas de diseño, es de " &
                    alrMax.ToString("0.00") & "%."
            End If
        End If
    End Sub

    ' =========================================================================
    Private Sub Generar_Click(sender As Object, e As EventArgs)
        Dim nombreArchivo As String = "Informe_Estado_" & DateTime.Now.ToString("yyyyMMdd")
        If Not String.IsNullOrWhiteSpace(_txtNombre.Text) Then
            nombreArchivo = "Informe_Estado_" & _txtNombre.Text & "_" & DateTime.Now.ToString("yyyyMMdd")
        End If

        Dim dlg As New SaveFileDialog With {
            .Title = "Guardar Informe de Estado del Proyecto",
            .Filter = "Word (*.docx)|*.docx",
            .FileName = nombreArchivo
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            _lblEstado.Text = "Generando informe…"
            Me.Refresh()

            Dim opciones As New OpcionesInformeEstadoMuros With {
                .Fecha = _dtpFecha.Value,
                .CodigoProyecto = _txtCodigo.Text.Trim(),
                .NombreProyecto = _txtNombre.Text.Trim(),
                .Direccion = _txtDireccion.Text.Trim(),
                .Propietario = _txtPropietario.Text.Trim(),
                .SistemaEstructural = _txtSistema.Text.Trim(),
                .NPisos = _txtPisos.Text.Trim(),
                .AreaPlanta = _txtArea.Text.Trim(),
                .ElaboradoPor = _txtElaboradoPor.Text.Trim(),
                .RevisadoPor = _txtRevisadoPor.Text.Trim(),
                .ObservacionDensidad = _txtObsDensidad.Text.Trim(),
                .ObservacionDerivas = _txtObsDerivas.Text.Trim(),
                .ObservacionALR = _txtObsALR.Text.Trim(),
                .AnalisisPreliminarRecomendaciones = _txtRecomendaciones.Text.Trim()
            }

            Dim detallesInfoRecibida = {"Planos Arquitectónicos", "Planos Estructurales", "Memorias de cálculo", "Estudio de suelos"}
            opciones.InfoRecibida = LeerChecklist(detallesInfoRecibida, _filasInfoRecibida)

            Dim detallesAvance = {"Modelo estructural", "Análisis dinámico", "Verificación de derivas", "Verificación de diseño de elementos estructurales"}
            opciones.DetallesAvance = LeerChecklist(detallesAvance, _filasAvance)

            InformeEstadoMurosService.GenerarInforme(_proyecto, opciones, dlg.FileName)

            _lblEstado.Text = "Informe generado correctamente."
            Dim abrir = MessageBox.Show("¿Desea abrir el archivo ahora?", "Informe generado",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If

        Catch ex As Exception
            _lblEstado.Text = "Error al generar el informe."
            Logger.Error(ex, "Form_InformeEstadoMuros.Generar_Click")
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function LeerChecklist(detalles() As String, filas As List(Of (Estado As ComboBox, Observ As TextBox))) As List(Of ItemChecklistInforme)
        Dim resultado As New List(Of ItemChecklistInforme)
        For i = 0 To detalles.Length - 1
            resultado.Add(New ItemChecklistInforme With {
                .Detalle = detalles(i),
                .Estado = filas(i).Estado.SelectedItem?.ToString(),
                .Observaciones = filas(i).Observ.Text.Trim()
            })
        Next
        Return resultado
    End Function

End Class
