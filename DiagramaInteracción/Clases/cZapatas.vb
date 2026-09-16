Imports ARCO.cZapata
Imports System.Runtime.Serialization

<Serializable>
Public Class cZapatas

    ' Tipos de zapatas definidos por el usuario
    Public Property Tipos As New List(Of cZapata)

    ' Zapatas asociadas a joints (apoyos reales del modelo)
    Public Property Apoyos As New List(Of cApoyo)

    Public Tabla_JointReactions As DataTable

    Public Reactions As New List(Of cCombinacionPila)

    Public Lista_Combinaciones As New List(Of String)

    Public Lista_Combinaciones_Estaticas As New List(Of String)
    Public Lista_Combinaciones_Dinamicas As New List(Of String)

    ''' <summary>
    ''' Cuando está en True, la revisión de excentricidad y la de capacidad del
    ''' suelo suman al P reactivo el peso propio de la zapata, del pedestal y del
    ''' suelo por encima de la zapata. Se aplica a todo el proyecto: activarlo o
    ''' desactivarlo afecta a todos los apoyos. Punzonamiento, cortante y flexión
    ''' NO se modifican: esas revisiones trabajan con la carga que baja del
    ''' pedestal, no con la reacción total del terreno.
    ''' </summary>
    <OptionalField> Public UsarPesoEstabilizante As Boolean = False

    ''' <summary>
    ''' Denominador N para el límite de excentricidad en combinaciones dinámicas
    ''' (|e| ≤ L/N). Estático queda fijo en L/6 por norma; en sismo se admite
    ''' relajarlo. Por defecto 4 (L/4); típicos 4 o 3.
    ''' </summary>
    <OptionalField> Public LimiteExcentricidadDinamicaN As Double = 4.0

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If Tipos Is Nothing Then Tipos = New List(Of cZapata)
        If Apoyos Is Nothing Then Apoyos = New List(Of cApoyo)
        If Reactions Is Nothing Then Reactions = New List(Of cCombinacionPila)
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)
        If Lista_Combinaciones_Estaticas Is Nothing Then Lista_Combinaciones_Estaticas = New List(Of String)
        If Lista_Combinaciones_Dinamicas Is Nothing Then Lista_Combinaciones_Dinamicas = New List(Of String)
        ' Un archivo viejo llega con LimiteExcentricidadDinamicaN = 0 y dividir por
        ' cero al armar los límites daría infinito. Se toma el default del programa.
        If LimiteExcentricidadDinamicaN <= 0 Then LimiteExcentricidadDinamicaN = 4.0
    End Sub

    ' Ejecutar toda la revisión del proyecto
    Public Sub EvaluarTodas()

        For Each apoyo In Apoyos

            apoyo.Resultados.Clear()

            For Each comb In apoyo.Combinaciones

                Dim z = apoyo.Zapata
                Dim Op_Comb As String = "EST"

                Dim resultado =
                    EvaluarZapata(z, comb.FZ, comb.MX, comb.MY, Op_Comb)

                apoyo.Resultados.Add(comb.LoadCase, resultado)

            Next
        Next

    End Sub

End Class
