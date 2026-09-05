Imports System.Runtime.Serialization

' ---------------------------------------------------------------------------
' Grupo de réplica: vincula una viga patrón con sus similares en otros pisos.
' El refuerzo fluye del patrón → similares vía "Propagar Grupo".
' Las demandas permanecen individuales por piso — nunca se reemplazan.
' ---------------------------------------------------------------------------
<Serializable>
Public Class GrupoReplicaViga

    Public Property ID As String = Guid.NewGuid().ToString()
    Public Property NombreGrupo As String = ""
    Public Property Piso_Patron As String = ""
    Public Property Nombre_Patron As String = ""
    ''' Labels de frames ordenados (ObjectLabel) que definen la agrupación.
    Public Property Labels_Patron As New List(Of String)()
    Public Property Similares As New List(Of MiembroGrupoViga)()

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If ID Is Nothing Then ID = Guid.NewGuid().ToString()
        If NombreGrupo Is Nothing Then NombreGrupo = ""
        If Piso_Patron Is Nothing Then Piso_Patron = ""
        If Nombre_Patron Is Nothing Then Nombre_Patron = ""
        If Labels_Patron Is Nothing Then Labels_Patron = New List(Of String)()
        If Similares Is Nothing Then Similares = New List(Of MiembroGrupoViga)()
    End Sub

End Class

<Serializable>
Public Class MiembroGrupoViga

    Public Property Piso As String = ""
    Public Property NombreViga As String = ""

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Piso Is Nothing Then Piso = ""
        If NombreViga Is Nothing Then NombreViga = ""
    End Sub

End Class

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

    ''' Prefijo para los nombres automáticos de viga: "V", "Viga", "VIGA", etc.
    Public Property PrefijoNombreViga As String = "V"

    Public Property BeamForces As New List(Of cCombinacionBeamForce)

    ''' <summary>Secciones que el usuario eligió incluir en el análisis. Vacío = todas.</summary>
    Public Property SeccionesSeleccionadas As New List(Of String)

    ''' <summary>
    ''' Agrupaciones manuales de frames. Cada elemento es la lista de ObjectLabels
    ''' que forman una viga continua según el usuario. Se aplica después del auto-agrupamiento.
    ''' </summary>
    Public Property GruposManual As New List(Of List(Of String))

    <OptionalField>
    Private _GruposReplica As List(Of GrupoReplicaViga)

    ''' Grupos de réplica patrón/similar definidos por el usuario.
    Public Property GruposReplica As List(Of GrupoReplicaViga)
        Get
            Return _GruposReplica
        End Get
        Set(value As List(Of GrupoReplicaViga))
            _GruposReplica = value
        End Set
    End Property

    ' Backing fields con OptionalField para compatibilidad con archivos .esm anteriores
    <OptionalField>
    Private _Frames As List(Of cFrame)
    <OptionalField>
    Private _Joints As List(Of cJoint)

    ''' <summary>Geometría propia del módulo de vigas — independiente de otros módulos.</summary>
    Public Property Frames As List(Of cFrame)
        Get
            Return _Frames
        End Get
        Set(value As List(Of cFrame))
            _Frames = value
        End Set
    End Property

    Public Property Joints As List(Of cJoint)
        Get
            Return _Joints
        End Get
        Set(value As List(Of cJoint))
            _Joints = value
        End Set
    End Property

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If Vigas Is Nothing Then Vigas = New List(Of cViga)
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)
        If Lista_Combinaciones_Design Is Nothing Then Lista_Combinaciones_Design = New List(Of String)
        If Lista_Combinaciones_Cortante Is Nothing Then Lista_Combinaciones_Cortante = New List(Of String)
        If Lista_Combinaciones_CortantePlastico Is Nothing Then Lista_Combinaciones_CortantePlastico = New List(Of String)
        If NivelDisipacion Is Nothing Then NivelDisipacion = "DMO"
        If PrefijoNombreViga Is Nothing Then PrefijoNombreViga = "V"
        If BeamForces Is Nothing Then BeamForces = New List(Of cCombinacionBeamForce)
        If SeccionesSeleccionadas Is Nothing Then SeccionesSeleccionadas = New List(Of String)
        If GruposManual Is Nothing Then GruposManual = New List(Of List(Of String))
        If _GruposReplica Is Nothing Then _GruposReplica = New List(Of GrupoReplicaViga)
        If _Frames Is Nothing Then _Frames = New List(Of cFrame)
        If _Joints Is Nothing Then _Joints = New List(Of cJoint)
    End Sub

End Class
