Imports ARCO.cZapata

<Serializable>
Public Class cZapatas

    ' Tipos de zapatas definidos por el usuario
    Public Property Tipos As New List(Of cZapata)

    ' Zapatas asociadas a joints (apoyos reales del modelo)
    Public Property Apoyos As New List(Of cApoyo)

    Public Tabla_JointReactions As DataTable

    Public Reactions As New List(Of cCombinacionPila)

    Public Lista_Combinaciones As New List(Of String)

    Public Lista_Combinaciones_Estaticas As New List(Of String)
    Public Lista_Combinaciones_Dinamicas As New List(Of String)


    ' Ejecutar toda la revisión del proyecto
    Public Sub EvaluarTodas()

        For Each apoyo In Apoyos

            apoyo.Resultados.Clear()

            For Each comb In apoyo.Combinaciones

                Dim z = apoyo.Zapata
                Dim Op_Comb As String = "EST"

                Dim resultado =
                    EvaluarZapata(z, comb.FZ, comb.MX, comb.MY, Op_Comb)

                apoyo.Resultados.Add(comb.LoadCase, resultado)

            Next
        Next

    End Sub



End Class
