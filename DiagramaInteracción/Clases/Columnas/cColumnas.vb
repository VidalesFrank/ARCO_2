Imports System.Runtime.Serialization

<Serializable>
Public Class cColumnas

    Public Elementos_Frame As Boolean
    Public Elementos_Pier As Boolean

    Public Verificacion_Flexo_Compresion As Boolean
    Public Verificacion_Cortante As Boolean
    Public Verificacion_Confinamiento As Boolean
    Public Verificacion_ALR As Boolean
    Public Verificacion_Pandeo As Boolean

    Public Info_Diseño As Boolean
    Public Info_Secciones As Boolean
    Public Info_Cortante As Boolean
    Public Info_Fuerzas As Boolean

    <OptionalField> Public Lista_Pisos As New List(Of String)
    <OptionalField> Public Lista_fc As New List(Of Single)
    <OptionalField> Public Lista_Columnas As New List(Of Columna)

    <OptionalField> Public Lista_Combinaciones As New List(Of String)
    <OptionalField> Public Lista_Combinaciones_ALR As New List(Of String)
    <OptionalField> Public Lista_Combinaciones_Grafico_ALR As New List(Of String)

    <OptionalField> Public ListA_Combinaciones_Design As New List(Of String)
    <OptionalField> Public Lista_Combinaciones_Cortante As New List(Of String)

    ' "Espiral" → usa ρs_a = 0.45(Ag/Ach-1)fc/fy además del mínimo NSR-10 C.7.10.4.3
    ' "Estribo cerrado" → solo ρs_b = 0.08|0.12 fc/fy (sin término Ag/Ach)
    <OptionalField> Public Trans_Circular As String = "Espiral"

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If Lista_Pisos Is Nothing Then Lista_Pisos = New List(Of String)
        If Lista_fc Is Nothing Then Lista_fc = New List(Of Single)
        If Lista_Columnas Is Nothing Then Lista_Columnas = New List(Of Columna)
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)
        If Lista_Combinaciones_ALR Is Nothing Then Lista_Combinaciones_ALR = New List(Of String)
        If Lista_Combinaciones_Grafico_ALR Is Nothing Then Lista_Combinaciones_Grafico_ALR = New List(Of String)
        If ListA_Combinaciones_Design Is Nothing Then ListA_Combinaciones_Design = New List(Of String)
        If Lista_Combinaciones_Cortante Is Nothing Then Lista_Combinaciones_Cortante = New List(Of String)
        If String.IsNullOrEmpty(Trans_Circular) Then Trans_Circular = "Espiral"
    End Sub

End Class
