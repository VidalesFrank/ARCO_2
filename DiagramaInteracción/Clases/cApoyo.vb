Imports ARCO.cZapata

<Serializable>
Public Class cApoyo

    ' Referencia directa al joint existente del proyecto
    Public Property Joint As cJoint

    ' Tipo de zapata asignada
    Public Property Zapata As cZapata

    ' Combinaciones provenientes de ETABS
    Public Property Combinaciones As New List(Of cCombinacionZapata)

    ' Resultados por combinación
    Public Property Resultados As New Dictionary(Of String, ResultadoZapata)



End Class
