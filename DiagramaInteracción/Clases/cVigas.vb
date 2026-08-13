Imports System.Runtime.Serialization

<Serializable>
Public Class cVigas

    Public Property Vigas As New List(Of cViga)

    Public Property Tabla_BeamForces As DataTable

    Public Property Lista_Combinaciones As New List(Of String)
    Public Property Lista_Combinaciones_Design As New List(Of String)
    Public Property Lista_Combinaciones_Cortante As New List(Of String)
    Public Property Lista_Combinaciones_CortantePlastico As New List(Of String)

    ' "DMO" → fy×1.0  |  "DES" → fy×1.25
    Public Property NivelDisipacion As String = "DMO"

    Public Property BeamForces As New List(Of cCombinacionBeamForce)

    ''' <summary>Secciones que el usuario eligió incluir en el análisis. Vacío = todas.</summary>
    Public Property SeccionesSeleccionadas As New List(Of String)

    ''' <summary>
    ''' Agrupaciones manuales de frames. Cada elemento es la lista de ObjectLabels
    ''' que forman una viga continua según el usuario. Se aplica después del auto-agrupamiento.
    ''' </summary>
    Public Property GruposManual As New List(Of List(Of String))

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If Vigas Is Nothing Then Vigas = New List(Of cViga)
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)
        If Lista_Combinaciones_Design Is Nothing Then Lista_Combinaciones_Design = New List(Of String)
        If Lista_Combinaciones_Cortante Is Nothing Then Lista_Combinaciones_Cortante = New List(Of String)
        If Lista_Combinaciones_CortantePlastico Is Nothing Then Lista_Combinaciones_CortantePlastico = New List(Of String)
        If NivelDisipacion Is Nothing Then NivelDisipacion = "DMO"
        If BeamForces Is Nothing Then BeamForces = New List(Of cCombinacionBeamForce)
        If SeccionesSeleccionadas Is Nothing Then SeccionesSeleccionadas = New List(Of String)
        If GruposManual Is Nothing Then GruposManual = New List(Of List(Of String))
    End Sub

End Class
