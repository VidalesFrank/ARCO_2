Imports ARCO.Refuerzo
Imports ARCO.eNumeradores

<Serializable>
Public Class cZapata

    Public Property Nombre As String
    Public Property Label_joint As String

    '----------------------
    ' Geometría 
    '----------------------
    ' Zapata
    '____________________________
    Public Property L_b As Double ' Ancho (m)
    Public Property L_h As Double ' Largo (m)
    Public Property e As Double   ' espesor (m)
    Public Property rec As Double ' recubrimiento (m)
    Public Property d As Double   ' peralte efectivo (m)
    ' Pedestal
    '____________________________
    Public Property b As Double ' Ancho (m)
    Public Property h As Double ' Largo (m)

    '----------------------
    ' Materiales
    '----------------------
    Public Property fc As Double   ' concreto (MPa)
    Public Property fy As Double   ' acero (MPa)

    '----------------------
    ' Suelo
    '----------------------
    Public Property qAdm_Est As Double   ' capacidad admisible (kN/m2)
    Public Property qAdm_Din As Double
    Public Property gammaSuelo As Double
    Public Property mu As Double     ' coef. fricción suelo–concreto

    Public Property FD_E As Double
    Public Property FD_D As Double

    '----------------------
    ' Acero suministrado
    '----------------------
    Public Property Refuerzos As New List(Of cRefuerzo)
    Public Property Rho_L1 As Double
    Public Property Rho_L2 As Double

    Public Property PesoPropio As Double
    Public Property NumeroJoint As String

    ' Combinaciones provenientes de ETABS
    Public Lista_Combinaciones_Estaticas As New List(Of cCombinacionPila)
    Public Lista_Combinaciones_Dinamicas As New List(Of cCombinacionPila)

    ' Resultados por combinación
    Public Property Resultados As New Dictionary(Of String, ResultadoZapata)

End Class

<Serializable>
Public Class cRefuerzo

    Public Property Direccion As eDireccionRefuerzo   ' L1 / L2
    Public Property Tipo As eTipoRefuerzo              ' Inferior / Superior
    Public Property Diametro As String                 '#4, #5, etc
    Public Property Diametro_mm As Double
    Public Property AreaBarra As Double                ' mm2
    Public Property Cantidad As Double
    Public Property Espaciamiento As Double            ' m

    Public ReadOnly Property AsTotal As Double
        Get
            Return AreaBarra * Cantidad
        End Get
    End Property

End Class
