<Serializable>
Public Class Proyecto_Losas

    Public Nombre As String
    Public Ruta As String

    Public e_losa As Single
    Public H_Nervio As Single
    Public tw_Nervio As Single
    Public Separacion_X As Single
    Public Separacion_Y As Single
    Public Longitud_X As Single
    Public Longitud_Y As Single
    Public C_Impuesta As Single
    Public C_Viva As Single
    Public fc As Single
    Public fy As Single

    '----------- Resultados --------
    Public CM As Single
    Public CV As Single
    Public CU As Single

    Public Lna As Single
    Public Lnb As Single
    Public m As Single

    Public Titulos_Coeficientes As New List(Of String)
    Public Coeficientes As New List(Of Single)

    Public Titulos_Demandas As New List(Of String)
    Public Momentos_Franja_Central As New List(Of Single)
    Public Momentos_Franja_Borde As New List(Of Single)

    Public Op_Continua_L As Boolean
    Public Op_Continua_T As Boolean
    Public Op_Continua_R As Boolean
    Public Op_Continua_B As Boolean

End Class
