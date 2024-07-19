<Serializable>
Public Class Elemento_Pila

    Public Name_Elemento As String
    Public Name_Label As String
    Public Df As Single
    Public Dc As Single
    Public fc As Integer
    Public Ps_Estatica As Single
    Public Ps_Dinamica As Single
    Public Pu_Estatica As Single
    Public Pu_Dinamica As Single
    Public Check1_PsE As Single
    Public Check2_PsD As Single
    Public Check3_PuE As Single
    Public Check4_PuD As Single
    Public EsfE_Trans As Single
    Public EsfD_Trans As Single
    Public Relacion_EsfE As Double
    Public Relacion_EsfD As Double
    Public Vn As Single
    Public Vu As Single
    Public Check_V2 As String
    Public Check_V3 As String
    Public FactorShear As Single
    Public Cuantia As Single
    Public Factor_Diagonal As Single
    Public Combinacion_Factor_Diagonal As String
    Public Factor_CortesH As Single
    Public combinacion_Factor_CortesH As String
    Public Matriz_PS As List(Of Single)
    Public Matriz_MS As List(Of Single)
    Public Matriz_PU As List(Of Single)
    Public Matriz_MU As List(Of Single)
    Public Matriz_V2 As List(Of Single)
    Public Matriz_V3 As List(Of Single)

    Public Matriz_Combinaciones As List(Of String)

    Public Ag_C As Single
    Public Ag_F As Single

    Public N_Barra_Long As String
    Public Cant_Barras_Long As Integer
    Public Acero_Long As Single
    Public N_Barra_Trans As String
    Public Separacion_Trans As Single

    Public Opcion_Hueca As String
    Public Esp_Anillo As Single

    Public Matriz_DI_Mn As List(Of Single)
    Public Matriz_DI_Pn As List(Of Single)
    Public Matriz_DI_PhiMn As List(Of Single)
    Public Matriz_DI_PhiPn As List(Of Single)
End Class
