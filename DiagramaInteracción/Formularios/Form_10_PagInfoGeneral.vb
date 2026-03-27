Imports ARCO.eNumeradores

Public Class PagInfoGeneral
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            Form_00_PaginaPrincipal.proyecto.Info.Nombre = T_NameProjet.Text
            Form_00_PaginaPrincipal.proyecto.Info.Direccion = T_Direction.Text
            Form_00_PaginaPrincipal.proyecto.Info.Ciudad = T_City.Text
            Form_00_PaginaPrincipal.proyecto.Info.Departamento = T_Department.Text
            Form_00_PaginaPrincipal.proyecto.Info.Year = Convert.ToInt16(C_YearBuilding.Text)
            Form_00_PaginaPrincipal.proyecto.ParametrosSismicos.NDE = C_DM.Text
            Form_00_PaginaPrincipal.proyecto.Info.Propietario = T_Propietario.Text
            Form_00_PaginaPrincipal.proyecto.Info.Designer = T_Disenador.Text
            Form_00_PaginaPrincipal.proyecto.Info.SistemaEstructural = C_SE.Text

            If C_SE.Text = "MCR" Then
                Form_00_PaginaPrincipal.proyecto.Info.SistemaEstructural = eNumeradores.eSistemaEstructural.MCR
            ElseIf C_SE.Text = "Porticos" Then
                Form_00_PaginaPrincipal.proyecto.Info.SistemaEstructural = eNumeradores.eSistemaEstructural.Porticos
            Else
                Form_00_PaginaPrincipal.proyecto.Info.SistemaEstructural = eNumeradores.eSistemaEstructural.Combinado
            End If

            Form_00_PaginaPrincipal.proyecto.Info.GrupoUso = C_GU.Text
            Form_00_PaginaPrincipal.proyecto.Info.TipoSuelo = C_TS.Text
            Form_00_PaginaPrincipal.proyecto.Info.Area = Convert.ToSingle(T_AreaPlanta.Text)

            Dim nombreSeleccionado As String = C_Responsable.SelectedItem.ToString()

            Dim responsableEnum As eResponsables = DirectorioResponsables.dResponsables.FirstOrDefault(Function(kvp) kvp.Value.NombreCompleto = nombreSeleccionado).Key

            Form_00_PaginaPrincipal.proyecto.Info.Persona_Responsable = responsableEnum

        Catch ex As Exception
        Finally
            Me.Close()
        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If IsNothing(Form_00_PaginaPrincipal.proyecto.Info.Imagen) Then
            Dim Dialog As New OpenFileDialog
            Dialog.Filter = "Imagenes |*.jpg"
            Dialog.Title = "Insertar Imagen del Proyecto"
            Dialog.ShowDialog()
            Form_00_PaginaPrincipal.proyecto.Info.Ruta_Imagen = Dialog.FileName
            If Dialog.FileName <> String.Empty Then
                Form_ImagenProyecto.P_ImagenProyecto.ImageLocation = Dialog.FileName
                Form_ImagenProyecto.P_ImagenProyecto.SizeMode = PictureBoxSizeMode.StretchImage
                Form_ImagenProyecto.Show()
            End If
        Else
            Form_ImagenProyecto.P_ImagenProyecto.Image = Form_00_PaginaPrincipal.proyecto.Info.Imagen
            Form_ImagenProyecto.P_ImagenProyecto.SizeMode = PictureBoxSizeMode.StretchImage
            Form_ImagenProyecto.Show()
        End If
    End Sub

    Private Sub PagInfoGeneral_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If C_Responsable.Items.Count > 0 Then
            C_Responsable.Items.Clear()
        End If

        Dim diccionario As Dictionary(Of eResponsables, Persona) = DirectorioResponsables.dResponsables

        For Each responsable As KeyValuePair(Of eResponsables, Persona) In diccionario
            C_Responsable.Items.Add(responsable.Value.NombreCompleto)
        Next

        If C_SE.Items.Count > 0 Then
            C_SE.Items.Clear()
        End If

        Dim diccionario_SE As Dictionary(Of eSistemaEstructural, Sistema) = DirectorioSistemaEstructural.dSistemaEstructural

        For Each SE As KeyValuePair(Of eSistemaEstructural, Sistema) In diccionario_SE
            C_SE.Items.Add(SE.Value.NameSistema)
        Next

    End Sub

End Class