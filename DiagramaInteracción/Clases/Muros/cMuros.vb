<Serializable>
Public Class cMuros

    Public Lista_Muros As New List(Of Muro)
    Public Lista_Pisos_Muros As List(Of String)
    Public Lista_fc_Muros As List(Of Single)

    Public Lista_Combinaciones_Muros As New List(Of String)
    Public Lista_Combinaciones_ALR_Muros As New List(Of String)
    Public ListA_Combinaciones_Design As New List(Of String)
    Public ListA_Combinaciones_Sismo As New List(Of String)

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

End Class
