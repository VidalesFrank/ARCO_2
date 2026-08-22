Imports System.Runtime.Serialization

<Serializable>
Public Class cCombinacionPila

    Public Property Story As String
    Public Property JointLabel As String
    Public Property UniqueName As String
    Public Property LoadCase As String
    Public Property FX As Single
    Public Property FY As Single
    Public Property FZ As Single
    Public Property MX As Single
    Public Property MY As Single
    Public Property MZ As Single

    ' Trazabilidad de origen — campos opcionales para retrocompatibilidad con proyectos guardados
    <OptionalField> Public SourceType As String  ' "Frame" | "Pier"
    <OptionalField> Public SourceName As String  ' Label del joint o nombre del pier
    <OptionalField> Public X As Double           ' Coordenada global X del apoyo [m]
    <OptionalField> Public Y As Double           ' Coordenada global Y del apoyo [m]
    <OptionalField> Public Z As Double           ' Coordenada global Z del apoyo [m]

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If SourceType Is Nothing Then SourceType = ""
        If SourceName Is Nothing Then SourceName = ""
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Story} - {JointLabel} - {UniqueName} ({LoadCase}, {FX}, {FY}, {FZ}, {MX}, {MY}, {MZ})"
    End Function

End Class
