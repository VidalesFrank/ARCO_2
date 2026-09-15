Imports ARCO.cZapata
Imports System.Runtime.Serialization

<Serializable>
Public Class cApoyo

    ' Referencia directa al joint existente del proyecto
    Public Property Joint As cJoint

    ' Tipo de zapata asignada
    Public Property Zapata As cZapata

    ' Combinaciones provenientes de ETABS
    Public Property Combinaciones As New List(Of cCombinacionZapata)

    ' Resultados por combinación
    Public Property Resultados As New Dictionary(Of String, ResultadoZapata)

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Combinaciones Is Nothing Then Combinaciones = New List(Of cCombinacionZapata)
        If Resultados Is Nothing Then Resultados = New Dictionary(Of String, ResultadoZapata)
    End Sub

End Class
