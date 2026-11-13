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

''' <summary>
''' Refuerzo transversal definido por el usuario para una sección específica.
''' Se usa en <see cref="VigaService.AsignarRefuerzoDesdePreferencias"/> para
''' aplicar el mismo set a todas las vigas que compartan sección.
'''
''' La longitud de confinamiento Lo queda totalmente controlada por el usuario:
''' Lo = 0.05 + (NumEstribosZC − 1) · SepZC. La ZNC se auto-calcula a partir
''' de la longitud restante del frame y SepZNC.
''' </summary>
<Serializable>
Public Class PreferenciasEstribosSeccion
    Public Property NombreSeccion As String = ""
    Public Property BarraZC As Integer = 3
    Public Property RamasZC As Integer = 2
    Public Property NumEstribosZC As Integer = 10
    Public Property SepZC As Double = 0.1
    Public Property BarraZNC As Integer = 3
    Public Property RamasZNC As Integer = 2
    Public Property SepZNC As Double = 0.2
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

    ' Recubrimiento global del módulo — m. 0 = usar valor por sección ETABS.
    <OptionalField> Public Recubrimiento As Double

    ''' Prefijo para los nombres automáticos de viga: "V", "Viga", "VIGA", etc.
    Public Property PrefijoNombreViga As String = "V"

    Public Property BeamForces As New List(Of cCombinacionBeamForce)

    ''' <summary>Secciones que el usuario eligió incluir en el análisis. Vacío = todas.</summary>
    Public Property SeccionesSeleccionadas As New List(Of String)

    ''' <summary>
    ''' Agrupaciones manuales de frames. Cada elemento es la lista de ObjectLabels
    ''' que forman una viga continua según el usuario. Se aplica después del auto-agrupamiento.
    '''
    ''' Incluye tanto grupos positivos (varios labels unidos por el usuario) como
    ''' singletes (un label que el usuario separó de una auto-agrupación). Los
    ''' singletes son clave para que al Procesar un proyecto, la auto-agrupación
    ''' de GenerarVigas no vuelva a unir frames alineados que el usuario ya separó.
    ''' </summary>
    Public Property GruposManual As New List(Of List(Of String))

    ''' <summary>
    ''' Labels de frames que el usuario marcó como "excluir de la revisión" al
    ''' separarlos de un grupo. Al replay en AplicarGruposManual, la viga nueva
    ''' que contenga cualquier label aquí nace con ExcluirDeRevision=True — así
    ''' las vigas residuales (fragmentos que no son vigas reales) siguen filtrándose
    ''' en reportes, plantas y dashboards tras cada Procesar.
    ''' </summary>
    <OptionalField>
    Public GruposManualExcluidos As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

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
    ''' <summary>
    ''' Preferencias de estribos por sección (clave = cSeccion.Nombre). Vacío
    ''' significa "usar refuerzo automático en todas las secciones". Se persiste
    ''' con el .esm para que el usuario no las vuelva a ingresar.
    ''' </summary>
    <OptionalField>
    Public PreferenciasEstribos As Dictionary(Of String, PreferenciasEstribosSeccion)

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
        If GruposManualExcluidos Is Nothing Then GruposManualExcluidos = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If _GruposReplica Is Nothing Then _GruposReplica = New List(Of GrupoReplicaViga)
        If _Frames Is Nothing Then _Frames = New List(Of cFrame)
        If _Joints Is Nothing Then _Joints = New List(Of cJoint)
        If PreferenciasEstribos Is Nothing Then
            PreferenciasEstribos = New Dictionary(Of String, PreferenciasEstribosSeccion)(StringComparer.OrdinalIgnoreCase)
        End If
    End Sub

End Class
