Imports System.Runtime.Serialization

<Serializable>
Public Class cViga

    Public Property Nombre As String
    Public Property Name_Beam As String
    Public Property NombrePlano As String = ""
    ''' Eje estructural paralelo a la viga (el que la viga "sigue"), ej. "B", "3".
    ''' Asignado automáticamente por GeometryService.AsignarEjesParalelosAVigas().
    Public Property EjeParalelo As String = ""
    Public Property Frames As New List(Of cFrame)
    Public Property Piso As String
    Public Property LongitudTotal As Double
    Public Property Direccion As Funciones_00_Varias.Vector3

    Public Property BeamForces As New List(Of cCombinacionBeamForce)
    Public Property EnvolventeGlobal As cEnvolventeMomento

    Public Property AsRequerido As Double
    Public Property AsProvisto As Double
    Public Property CumpleFlexion As Boolean

    ' --- Grupo de réplica ---
    <OptionalField>
    Public GrupoReplicaID As String = ""
    <OptionalField>
    Public EsPatronGrupo As Boolean = False
    ''' True cuando el refuerzo del patrón fue modificado pero aún no se propagó a este similar.
    <OptionalField>
    Public RefuerzoDesincronizado As Boolean = False

    ''' Nombre para mostrar en la interfaz con indicadores de grupo.
    ''' [P] = patrón, [S] = similar sincronizado, [S!] = similar desincronizado.
    Public ReadOnly Property NombreDisplay As String
        Get
            Dim base As String = If(String.IsNullOrWhiteSpace(NombrePlano), Nombre, NombrePlano)
            If EsPatronGrupo Then Return "[P] " & base
            If Not String.IsNullOrWhiteSpace(GrupoReplicaID) Then
                If RefuerzoDesincronizado Then Return "[S!] " & base
                Return "[S] " & base
            End If
            Return base
        End Get
    End Property

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If NombrePlano Is Nothing Then NombrePlano = ""
        If EjeParalelo Is Nothing Then EjeParalelo = ""
        If GrupoReplicaID Is Nothing Then GrupoReplicaID = ""
        If Frames Is Nothing Then Frames = New List(Of cFrame)
        If BeamForces Is Nothing Then BeamForces = New List(Of cCombinacionBeamForce)
    End Sub

    Public Overrides Function ToString() As String
        Return Name_Beam
    End Function

End Class
