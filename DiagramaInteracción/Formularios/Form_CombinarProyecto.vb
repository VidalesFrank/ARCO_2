Public Class Form_CombinarProyecto

    Public Property ProyectoDestino As Proyecto

    Private _proyectoFuente As Proyecto = Nothing

    Private ReadOnly Modulos() As String = {"Pilas", "Columnas", "Muros", "Vigas", "Nervios", "Zapatas"}

    Private ReadOnly NombrePlural() As String = {"pilas", "columnas", "muros", "frames", "nervios", "zapatas"}

    ' ══════════════════════════════════════════════════════════════════════════
    '  CARGA DEL ARCHIVO FUENTE
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub BtnExaminar_Click(sender As Object, e As EventArgs) Handles BtnExaminar.Click
        Using dlg As New OpenFileDialog()
            dlg.Filter = "Archivo ARCO (*.esm)|*.esm"
            dlg.Title = "Seleccionar archivo fuente"
            If dlg.ShowDialog() <> DialogResult.OK Then Return
            TxtRuta.Text = dlg.FileName
            CargarArchivoFuente(dlg.FileName)
        End Using
    End Sub

    Private Sub CargarArchivoFuente(ruta As String)
        Try
            Cursor = Cursors.WaitCursor
            _proyectoFuente = Funciones_Programa.DeSerializar(Of Proyecto)(ruta)
            ActualizarListaModulos()
        Catch ex As Exception
            _proyectoFuente = Nothing
            ClbModulos.Items.Clear()
            BtnCombinar.Enabled = False
            MessageBox.Show("No se pudo abrir el archivo:" & Environment.NewLine & ex.Message,
                            "Error al abrir", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  POBLACIÓN DE LA LISTA
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ActualizarListaModulos()
        ClbModulos.Items.Clear()
        If _proyectoFuente Is Nothing Then Return

        For i = 0 To Modulos.Length - 1
            Dim modulo = Modulos(i)
            Dim cntFuente = Funciones_Programa.ContarElementosMod(modulo, _proyectoFuente)
            Dim cntDest = Funciones_Programa.ContarElementosMod(modulo, ProyectoDestino)

            Dim linea As String
            If cntFuente = 0 Then
                linea = $"{modulo,-14} sin datos"
            ElseIf cntDest > 0 Then
                linea = $"{modulo,-14} {cntFuente} {NombrePlural(i)}  →  reemplaza {cntDest} actuales"
            Else
                linea = $"{modulo,-14} {cntFuente} {NombrePlural(i)}"
            End If

            ClbModulos.Items.Add(linea, cntFuente > 0)
        Next

        ActualizarEstado()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  ESTADO — advertencia y botón Combinar
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ClbModulos_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles ClbModulos.ItemCheck
        BeginInvoke(Sub() ActualizarEstado())
    End Sub

    Private Sub ActualizarEstado()
        If _proyectoFuente Is Nothing Then
            BtnCombinar.Enabled = False
            PanelAdvertencia.Visible = False
            Return
        End If

        Dim haySeleccionados = ClbModulos.CheckedItems.Count > 0
        BtnCombinar.Enabled = haySeleccionados

        Dim advertencias As New List(Of String)
        For i = 0 To Modulos.Length - 1
            If ClbModulos.GetItemChecked(i) Then
                Dim cntDest = Funciones_Programa.ContarElementosMod(Modulos(i), ProyectoDestino)
                If cntDest > 0 Then
                    Dim cntFuente = Funciones_Programa.ContarElementosMod(Modulos(i), _proyectoFuente)
                    advertencias.Add($"• {Modulos(i)}: {cntFuente} {NombrePlural(i)} del archivo reemplazarán {cntDest} elemento(s) existentes.")
                End If
            End If
        Next

        If advertencias.Count > 0 Then
            LblAdvertencia.Text = "⚠ Los siguientes módulos ya tienen datos en el proyecto actual:" &
                                  Environment.NewLine & String.Join(Environment.NewLine, advertencias)
            PanelAdvertencia.Visible = True
        Else
            PanelAdvertencia.Visible = False
        End If
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  ACCIÓN — COMBINAR
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub BtnCombinar_Click(sender As Object, e As EventArgs) Handles BtnCombinar.Click
        Dim seleccionados As New HashSet(Of String)()
        For i = 0 To Modulos.Length - 1
            If ClbModulos.GetItemChecked(i) Then
                seleccionados.Add(Modulos(i))
            End If
        Next
        If seleccionados.Count = 0 Then Return

        Try
            Cursor = Cursors.WaitCursor
            Funciones_Programa.CombinarDesdeArchivo(ProyectoDestino, TxtRuta.Text, seleccionados)
            Cursor = Cursors.Default

            Dim lista = String.Join(", ", seleccionados)
            MessageBox.Show($"Módulos combinados correctamente: {lista}." &
                            Environment.NewLine & Environment.NewLine &
                            "Guarde el proyecto (Archivo → Guardar) para conservar los cambios.",
                            "Combinación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information)
            DialogResult = DialogResult.OK
            Close()
        Catch ex As Exception
            Cursor = Cursors.Default
            MessageBox.Show("Error al combinar:" & Environment.NewLine & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnCancelar_Click(sender As Object, e As EventArgs) Handles BtnCancelar.Click
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

End Class
