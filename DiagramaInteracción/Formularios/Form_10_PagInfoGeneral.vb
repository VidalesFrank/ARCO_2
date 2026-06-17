Imports ARCO.eNumeradores

Public Class PagInfoGeneral
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            Dim p = Form_00_PaginaPrincipal.proyecto

            p.Info.Nombre = T_NameProjet.Text
            p.Info.Direccion = T_Direction.Text
            p.Info.Ciudad = T_City.Text
            p.Info.Departamento = T_Department.Text
            p.Info.Propietario = T_Propietario.Text
            p.Info.Designer = T_Disenador.Text

            If Not String.IsNullOrEmpty(C_YearBuilding.Text) Then
                p.Info.Year = Convert.ToInt16(C_YearBuilding.Text)
            End If

            Dim areaVal As Single
            If Single.TryParse(T_AreaPlanta.Text, areaVal) Then
                p.Info.Area = areaVal
            End If

            If C_SE.Text = "MCR" Then
                p.Info.SistemaEstructural = eNumeradores.eSistemaEstructural.MCR
            ElseIf C_SE.Text = "Porticos" Then
                p.Info.SistemaEstructural = eNumeradores.eSistemaEstructural.Porticos
            Else
                p.Info.SistemaEstructural = eNumeradores.eSistemaEstructural.Combinado
            End If

            If Not String.IsNullOrEmpty(C_DM.Text) Then
                p.ParametrosSismicos.NDE = DirectCast([Enum].Parse(GetType(eDisipasion), C_DM.Text), eDisipasion)
            End If

            If Not String.IsNullOrEmpty(C_GU.Text) Then
                p.Info.GrupoUso = DirectCast([Enum].Parse(GetType(eGrupoUso), C_GU.Text), eGrupoUso)
            End If

            If Not String.IsNullOrEmpty(C_TS.Text) Then
                p.Info.TipoSuelo = DirectCast([Enum].Parse(GetType(eGrupoSuelo), C_TS.Text), eGrupoSuelo)
            End If

            If C_Responsable.SelectedItem IsNot Nothing Then
                Dim nombreSel As String = C_Responsable.SelectedItem.ToString()
                p.Info.Persona_Responsable = DirectorioResponsables.dResponsables.FirstOrDefault(Function(kvp) kvp.Value.NombreCompleto = nombreSel).Key
            End If

        Catch ex As Exception
            MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Close()
        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If IsNothing(Form_00_PaginaPrincipal.proyecto.Info.Imagen) Then
            Dim Dialog As New OpenFileDialog
            Dialog.Filter = "Imagenes|*.jpg;*.jpeg;*.png"
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
        C_Responsable.Items.Clear()
        For Each kv As KeyValuePair(Of eResponsables, Persona) In DirectorioResponsables.dResponsables
            C_Responsable.Items.Add(kv.Value.NombreCompleto)
        Next

        C_SE.Items.Clear()
        For Each kv As KeyValuePair(Of eSistemaEstructural, Sistema) In DirectorioSistemaEstructural.dSistemaEstructural
            C_SE.Items.Add(kv.Value.NameSistema)
        Next
    End Sub

End Class