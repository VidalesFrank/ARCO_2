Imports System.Runtime.Serialization

<Serializable>
Public Class cMuros

    <OptionalField> Public Lista_Muros As New List(Of Muro)
    <OptionalField> Public Lista_Pisos_Muros As List(Of String)
    <OptionalField> Public Lista_fc_Muros As List(Of Single)

    <OptionalField> Public Lista_Combinaciones_Muros As New List(Of String)
    <OptionalField> Public Lista_Combinaciones_ALR_Muros As New List(Of String)
    <OptionalField> Public ListA_Combinaciones_Design As New List(Of String)
    <OptionalField> Public ListA_Combinaciones_Sismo As New List(Of String)

    Public Info_Diseño As Boolean
    Public Info_Secciones As Boolean
    Public Info_Cortante As Boolean
    Public Info_Fuerzas As Boolean

    Public D_Techo_X As Single
    Public D_Techo_Y As Single

    '========= MACROPARAMETROS ========
    Public Factor_Forma As Single
    Public Densidad_X As Single
    Public Densidad_Y As Single
    Public IM_X As Single
    Public IM_Y As Single
    Public ArMean_X As Single
    Public ArMean_Y As Single

    <OnDeserialized>
    Private Sub InicializarDefaults(ctx As StreamingContext)
        If Lista_Muros Is Nothing Then Lista_Muros = New List(Of Muro)
        If Lista_Pisos_Muros Is Nothing Then Lista_Pisos_Muros = New List(Of String)
        If Lista_fc_Muros Is Nothing Then Lista_fc_Muros = New List(Of Single)
        If Lista_Combinaciones_Muros Is Nothing Then Lista_Combinaciones_Muros = New List(Of String)
        If Lista_Combinaciones_ALR_Muros Is Nothing Then Lista_Combinaciones_ALR_Muros = New List(Of String)
        If ListA_Combinaciones_Design Is Nothing Then ListA_Combinaciones_Design = New List(Of String)
        If ListA_Combinaciones_Sismo Is Nothing Then ListA_Combinaciones_Sismo = New List(Of String)
    End Sub

End Class
