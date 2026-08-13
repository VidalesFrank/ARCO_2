Imports System.Runtime.Serialization

<Serializable>
Public Class cElementos
    Public Property Pilas As New cPilas()
    Public Property Columnas As New cColumnas()
    Public Property Muros As New cMuros()
    Public Property Frames As New List(Of cFrame)
    Public Property Joints As New List(Of cJoint)
    Public Property Zapatas As New cZapatas()
    Public Property Vigas As New cVigas()
    Public Property Grids As New cGrids()

    <OptionalField> Public Nervios As New cNervios()

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Pilas Is Nothing Then Pilas = New cPilas()
        If Columnas Is Nothing Then Columnas = New cColumnas()
        If Muros Is Nothing Then Muros = New cMuros()
        If Frames Is Nothing Then Frames = New List(Of cFrame)
        If Joints Is Nothing Then Joints = New List(Of cJoint)
        If Zapatas Is Nothing Then Zapatas = New cZapatas()
        If Vigas Is Nothing Then Vigas = New cVigas()
        If Grids Is Nothing Then Grids = New cGrids()
        If Nervios Is Nothing Then Nervios = New cNervios()
    End Sub

End Class

<Serializable>
Public Class cGrids
    Public Property GridLines As New List(Of cGridLine)
End Class
