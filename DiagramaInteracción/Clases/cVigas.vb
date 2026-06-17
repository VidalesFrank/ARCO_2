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


End Class
