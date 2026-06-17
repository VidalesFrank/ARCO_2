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

    Public Property Redistribucion As New cRedistribucionFrame
    Public Property RefuerzoTransversal As New List(Of cRefuerzoTransversalZona)
    Public Property RevisionCortante As New List(Of cRevisionCortanteZona)
    Public Property CortantePlastico As cResultadoCortantePlasticoFrame

    Public Overrides Function ToString() As String
        Return $"{Story} - {ElementLabel} - {ObjectLabel} ({JointI}, {JointJ})"
    End Function

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

    ' 🔹 Datos originales (análisis)
    Public Property ResultadoBase As New cResultadoFlexion

    ' 🔹 Datos actuales (con redistribución aplicada)
    Public Property ResultadoActual As New cResultadoFlexion

    ' 🔹 Factor de redistribución
    Public Property FactorRedistribucion As Double = 0

End Class

<Serializable>
Public Class cResultadoFlexion

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
Public Class cRedistribucionFrame

    ' 🔴 reducción en apoyo izquierdo
    Public Property RedNegIzq As Double = 0

    ' 🔴 reducción en apoyo derecho
    Public Property RedNegDer As Double = 0

    ' 🔵 reducción en centro
    Public Property RedPosCentro As Double = 0

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

<Serializable>
Public Class cRefuerzoTransversalZona
    Public Property Posicion As ARCO.eNumeradores.PosicionTramoViga
    Public Property NumEstribos As Integer = 10
    Public Property NumeroBarra As Integer = 3
    Public Property CantEstribos As Integer = 2
    Public Property Separacion As Double = 0.1
End Class

<Serializable>
Public Class cRevisionCortanteZona
    Public Property Posicion As ARCO.eNumeradores.PosicionTramoViga
    Public Property Vu As Double
    Public Property Vc As Double
    Public Property Vs As Double
    Public Property phiVn As Double
    Public Property Factor As Double
    Public Property Cumple As Boolean
End Class

' ── CORTANTE PLÁSTICO (diseño por capacidad, NSR-10 C.21.5.4) ────────────────

<Serializable>
Public Class cResultadoCortantePlasticoFrame
    ' Luz libre entre caras de apoyos (m)
    Public Property Ln As Double
    ' Factor fy aplicado: 1.0 (DMO) o 1.25 (DES)
    Public Property fy_factor As Double
    ' Momentos nominales en apoyos (kN·m), φ = 1.0
    Public Property Mn_neg_izq As Double   ' As_sup izquierda
    Public Property Mn_pos_izq As Double   ' As_inf izquierda
    Public Property Mn_neg_der As Double   ' As_sup derecha
    Public Property Mn_pos_der As Double   ' As_inf derecha
    ' Cortante plástico por caso de volteo (kN)
    Public Property Ve_A As Double   ' (Mn_neg_izq + Mn_pos_der) / Ln  → volteo derecho
    Public Property Ve_B As Double   ' (Mn_pos_izq + Mn_neg_der) / Ln  → volteo izquierdo
    ' Resultados por zona confinada
    Public Property ZonaIzq As New cRevisionCortantePlasticoZona
    Public Property ZonaDer As New cRevisionCortantePlasticoZona
End Class

<Serializable>
Public Class cRevisionCortantePlasticoZona
    Public Property Posicion As ARCO.eNumeradores.PosicionTramoViga
    ' Cortante gravitacional en sección crítica (con signo ETABS, kN)
    Public Property Vg As Double
    ' Cortante de diseño por cada caso de volteo (kN)
    Public Property Vu_A As Double     ' Ve_A + Vg  (volteo que maximiza esta zona)
    Public Property Vu_B As Double     ' -Ve_B + Vg (volteo opuesto)
    ' Demanda de diseño = max(|Vu_A|, |Vu_B|)
    Public Property Vu_diseno As Double
    ' Capacidad (mismos estribos del chequeo estándar)
    Public Property Vc As Double
    Public Property Vs As Double
    Public Property phiVn As Double
    Public Property Factor As Double
    Public Property Cumple As Boolean
End Class