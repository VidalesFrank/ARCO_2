Public Class Form_Opciones_Combinaciones
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public OpcionLlamado As String
    Public Evaluacion As String
    Private Sub Boton_ALR_Click(sender As Object, e As EventArgs) Handles Boton_ALR.Click

        'For i = 0 To Lista_Combinaciones.SelectedItems.Count - 1
        '    Lista_Cargas_Design.Items.Add(Lista_Combinaciones.SelectedItems.Item(i))
        'Next
        'Lista_Combinaciones.ClearSelected()
        Dim itemsMover As New List(Of Object)

        For Each item In Lista_Combinaciones.SelectedItems
            itemsMover.Add(item)
        Next

        For Each item In itemsMover
            If Not Lista_Cargas_Design.Items.Contains(item) Then
                Lista_Cargas_Design.Items.Add(item)
            End If

            Lista_Combinaciones.Items.Remove(item)
        Next

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'For i = 0 To Lista_Cargas_Design.SelectedItems.Count() - 1
        '    If Lista_Cargas_Design.SelectedItems.Count > 0 Then
        '        Dim F As Integer = Lista_Cargas_Design.SelectedItems.Count - 1
        '        Lista_Cargas_Design.Items.Remove(Lista_Cargas_Design.SelectedItems(F))
        '    End If
        'Next

        'Lista_Cargas_Design.ClearSelected()

        Dim itemsRemover As New List(Of Object)

        For Each item In Lista_Cargas_Design.SelectedItems
            itemsRemover.Add(item)
        Next

        For Each item In itemsRemover

            ' 🔹 devolver a lista izquierda
            If Not Lista_Combinaciones.Items.Contains(item) Then
                Lista_Combinaciones.Items.Add(item)
            End If

            Lista_Cargas_Design.Items.Remove(item)

        Next

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        If OpcionLlamado = "Columna" Then
            For i = 0 To Lista_Cargas_Design.Items.Count - 1
                Proyecto.Elementos.Columnas.Lista_Combinaciones_Grafico_ALR.Add(Lista_Cargas_Design.Items(i))
            Next
            Proyecto.Elementos.Columnas.Lista_Combinaciones_Grafico_ALR.Clear()

        ElseIf OpcionLlamado = "Muros" Then
            If Evaluacion = "Design" Then
                Proyecto.Elementos.Muros.ListA_Combinaciones_Design.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Muros.ListA_Combinaciones_Design.Add(Lista_Cargas_Design.Items(i))
                Next
            ElseIf Evaluacion = "Seismic" Then
                Proyecto.Elementos.Muros.ListA_Combinaciones_Sismo.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Muros.ListA_Combinaciones_Sismo.Add(Lista_Cargas_Design.Items(i))
                Next
            End If

        ElseIf OpcionLlamado = "Pilas" Then
            If Evaluacion = "Grav_Servicio" Then
                Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Servicio.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Servicio.Add(Lista_Cargas_Design.Items(i))
                Next

            ElseIf Evaluacion = "Grav_Design" Then
                Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Design.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Design.Add(Lista_Cargas_Design.Items(i))
                Next

            ElseIf Evaluacion = "Sismicas_Servicio" Then
                Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Servicio.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Servicio.Add(Lista_Cargas_Design.Items(i))
                Next

            ElseIf Evaluacion = "Sismicas_Design" Then
                Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Design.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Design.Add(Lista_Cargas_Design.Items(i))
                Next

            ElseIf Evaluacion = "Comb_Traccion" Then
                Proyecto.Elementos.Pilas.Lista_Combinaciones_Traccion.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Pilas.Lista_Combinaciones_Traccion.Add(Lista_Cargas_Design.Items(i))
                Next
            End If
        ElseIf OpcionLlamado = "Zapatas" Then
            If Evaluacion = "Estaticas" Then
                Proyecto.Elementos.Zapatas.Lista_Combinaciones_Estaticas.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Zapatas.Lista_Combinaciones_Estaticas.Add(Lista_Cargas_Design.Items(i))
                Next

            ElseIf Evaluacion = "Dinamico" Then
                Proyecto.Elementos.Zapatas.Lista_Combinaciones_Dinamicas.Clear()
                For i = 0 To Lista_Cargas_Design.Items.Count - 1
                    Proyecto.Elementos.Zapatas.Lista_Combinaciones_Dinamicas.Add(Lista_Cargas_Design.Items(i))
                Next
                'For i = 0 To Lista_Cargas_Diseño.Items.Count - 1
                '    Proyecto.Elementos.Pilas
                '    Columnas.Lista_Combinaciones_Grafico_ALR.Add(Lista_Cargas_Diseño.Items(i))
                'Next
                'Proyecto.Elementos.Columnas.Lista_Combinaciones_Grafico_ALR.Clear()

            End If

        ElseIf OpcionLlamado = "Vigas" Then
            Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Clear()
            For i = 0 To Lista_Cargas_Design.Items.Count - 1
                Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Add(Lista_Cargas_Design.Items(i))
            Next
        ElseIf OpcionLlamado = "ReplicarRefuerzo" Then

            Dim vigaOrigen As cViga = CType(Me.Tag, cViga)

            For i = 0 To Lista_Cargas_Design.Items.Count - 1

                Dim vigaDestino As cViga = CType(Lista_Cargas_Design.Items(i), cViga)

                ' 🔥 VALIDACIÓN (MUY IMPORTANTE)
                If Not ARCO.Form_09_Vigas.SonVigasCompatibles(vigaOrigen, vigaDestino) Then
                    MessageBox.Show($"La viga {vigaDestino.Name_Beam} no es compatible",
                                    "Advertencia",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning)
                    Continue For
                End If

                ' 🔥 COPIAR REFUERZO
                ARCO.Form_09_Vigas.CopiarRefuerzoEntreVigas(vigaOrigen, vigaDestino)

            Next

        End If

        Me.Close()
    End Sub

    Private Sub Btn_OK_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ' Aquí puedes guardar la selección del usuario si es necesario
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub

    Private Sub Form_Opciones_Combinaciones_Load(sender As Object, e As EventArgs) Handles MyBase.Load





    End Sub
End Class