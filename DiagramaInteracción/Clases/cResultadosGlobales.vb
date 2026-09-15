Imports System.Runtime.Serialization

<Serializable>
Public Class cResultadosGlobales
    Public DerivasX As New List(Of cDeriva)
    Public DerivasY As New List(Of cDeriva)
    Public DerivaCrX As New List(Of cDeriva)
    Public DerivaCrY As New List(Of cDeriva)

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If DerivasX Is Nothing Then DerivasX = New List(Of cDeriva)
        If DerivasY Is Nothing Then DerivasY = New List(Of cDeriva)
        If DerivaCrX Is Nothing Then DerivaCrX = New List(Of cDeriva)
        If DerivaCrY Is Nothing Then DerivaCrY = New List(Of cDeriva)
    End Sub
End Class
