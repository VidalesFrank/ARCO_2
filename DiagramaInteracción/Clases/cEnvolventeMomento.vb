Imports System.Runtime.Serialization

<Serializable>
Public Class cEnvolventeMomento

    Public Property Estaciones As New List(Of Double)

    Public Property MmaxAnalisis As New List(Of Double)
    Public Property MminAnalisis As New List(Of Double)

    Public Property MmaxDesign As New List(Of Double)
    Public Property MminDesign As New List(Of Double)

    Public Property MomentoPositivoMax As Double
    Public Property EstacionMomentoPositivoMax As Double

    Public Property MomentoNegativoIzq As Double
    Public Property MomentoNegativoDer As Double

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Estaciones Is Nothing Then Estaciones = New List(Of Double)
        If MmaxAnalisis Is Nothing Then MmaxAnalisis = New List(Of Double)
        If MminAnalisis Is Nothing Then MminAnalisis = New List(Of Double)
        If MmaxDesign Is Nothing Then MmaxDesign = New List(Of Double)
        If MminDesign Is Nothing Then MminDesign = New List(Of Double)
    End Sub

End Class
