
<Serializable>
Public Class cViga

    Public Property Nombre As String
    Public Property Name_Beam As String
    Public Property Frames As New List(Of cFrame)
    Public Property Piso As String
    Public Property LongitudTotal As Double
    Public Property Direccion As Funciones_00_Varias.Vector3

    Public Property BeamForces As New List(Of cCombinacionBeamForce)
    Public Property EnvolventeGlobal As cEnvolventeMomento

    Public Property AsRequerido As Double
    Public Property AsProvisto As Double
    Public Property CumpleFlexion As Boolean

    Public Overrides Function ToString() As String
        Return Name_Beam
    End Function


End Class
