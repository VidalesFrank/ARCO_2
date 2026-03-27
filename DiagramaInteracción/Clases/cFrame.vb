Imports ARCO.eNumeradores

<Serializable>
Public Class cFrame
    Public Property Story As String
    Public Property ElementLabel As String
    Public Property ObjectType As String
    Public Property ObjectLabel As String
    Public Property JointI As String
    Public Property JointJ As String
    Public Property Section As cSeccion
    Public Property Longitud As Double
    Public Property EstacionInicio As Double
    Public Property EstacionFinal As Double
    Public Property EnvolventeMomento As cEnvolventeMomento

    Public Property RefuerzoSuperior As New List(Of cRefuerzoTramo)
    Public Property RefuerzoInferior As New List(Of cRefuerzoTramo)
    Public Property RevisionFlexion As New List(Of cRevisionFlexionZona)


End Class

<Serializable>
Public Class cRefuerzoTramo

    Public Property Frame As String

    Public Property Posicion As ARCO.eNumeradores.PosicionTramoViga

    Public Property Barras As New Dictionary(Of String, Integer)

End Class

<Serializable>
Public Class cRevisionFlexionZona

    Public Property Posicion As PosicionTramoViga

    Public Property MomentoPositivo As Double
    Public Property MomentoNegativo As Double

    Public Property AsReqSup As Double
    Public Property AsReqInf As Double

    Public Property AsProvSup As Double
    Public Property AsProvInf As Double

    Public Property RatioSup As Double
    Public Property RatioInf As Double

    Public Property CumpleSuperior As Boolean
    Public Property CumpleInferior As Boolean

End Class

<Serializable>
Public Class cSeccion

    Public Property Nombre As String

    Public Property LabelSec As String

    ' Geometría
    Public Property b As Double
    Public Property h As Double
    Public Property recubrimiento As Double

    ' Materiales
    Public Property nameMaterial As String
    Public Property fc As Double
    Public Property E As Double
    Public Property G As Double
    Public Property fy As Double

    Public ReadOnly Property d As Double
        Get
            Return h - recubrimiento
        End Get
    End Property

End Class