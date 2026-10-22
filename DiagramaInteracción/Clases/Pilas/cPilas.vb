Imports System.Runtime.Serialization

<Serializable>
Public Class cPilas

    <OptionalField> Public ListaElementos As New List(Of Elemento_Pila)

    Public Esf_Adm_Est As Single
    Public Esf_Adm_Din As Single
    Public Esf_Frccion As Single
    Public ModuloE_Acero As Single
    Public Def_Uni_ConcAs As Single
    Public Fy As Single
    Public Opcion_Elemento As String

    Public Tabla_JointReactions As DataTable

    <OptionalField> Public Reactions As New List(Of cCombinacionPila)

    <OptionalField> Public Lista_Combinaciones As New List(Of String)

    <OptionalField> Public Lista_Combinaciones_Gravitacionales_Servicio As New List(Of String)
    <OptionalField> Public Lista_Combinaciones_Gravitacionales_Design As New List(Of String)

    <OptionalField> Public Lista_Combinaciones_Sismicas_Servicio As New List(Of String)
    <OptionalField> Public Lista_Combinaciones_Sismicas_Design As New List(Of String)

    <OptionalField> Public Lista_Combinaciones_Traccion As New List(Of String)

    ' Geometría propia del módulo — snapshot del import ETABS, independiente de otros módulos
    <OptionalField> Public Joints As New List(Of cJoint)()
    <OptionalField> Public Frames As New List(Of cFrame)()

    ' Coordenadas X,Y en planta por label de la pila. Se setea al confirmar la
    ' importación con el mismo SourceLabel que termina siendo Elemento_Pila.Name_Label:
    '   - Pilas Frame : SourceLabel = ElementLabel del frame   → también matcheable vía Joints.
    '   - Pilas Pier  : SourceLabel = pierName (NO es ObjectLabel de ningún joint, no cruza con Joints).
    '
    ' GeometriaPlantasService lo usa como fallback cuando el cruce por Joints no encuentra.
    <OptionalField> Public CoordsPorLabel As Dictionary(Of String, System.Drawing.PointF)

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If ListaElementos Is Nothing Then ListaElementos = New List(Of Elemento_Pila)
        If Reactions Is Nothing Then Reactions = New List(Of cCombinacionPila)
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)
        If Lista_Combinaciones_Gravitacionales_Servicio Is Nothing Then Lista_Combinaciones_Gravitacionales_Servicio = New List(Of String)
        If Lista_Combinaciones_Gravitacionales_Design Is Nothing Then Lista_Combinaciones_Gravitacionales_Design = New List(Of String)
        If Lista_Combinaciones_Sismicas_Servicio Is Nothing Then Lista_Combinaciones_Sismicas_Servicio = New List(Of String)
        If Lista_Combinaciones_Sismicas_Design Is Nothing Then Lista_Combinaciones_Sismicas_Design = New List(Of String)
        If Lista_Combinaciones_Traccion Is Nothing Then Lista_Combinaciones_Traccion = New List(Of String)
        If Joints Is Nothing Then Joints = New List(Of cJoint)()
        If Frames Is Nothing Then Frames = New List(Of cFrame)()
    End Sub

End Class
