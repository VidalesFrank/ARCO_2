<Serializable>
Public Class Proyecto_Escaleras

    Public Nombre As String
    Public Ruta As String

    Public fc As Single
    Public fy As Single
    Public C_Imp As Single
    Public C_Viv As Single
    Public P_ConR As Single
    Public P_Con As Single

    Public Huella As Single
    Public Contrahuella As Single
    Public N_Peldanos As Integer
    Public L_Peldanos As Single
    Public L_Descanso As Single
    Public L_Total As Single
    Public A_Escalera As Single
    Public A_Estudio As Single
    Public Recubrimiento As Single
    Public h As Single

    Public Wu_Inclinada As Single
    Public Wu_Descanso As Single

    Public Abscisas As New List(Of Single)
    Public Momentos As New List(Of Single)
    Public Cortantes As New List(Of Single)

    '-- Diseno temperatura --
    Public Acero_Temperatura As Single
    Public Cuantia_Temperatura As Single
    Public Cuantia_Temperaruta_Colocada As Single
    Public Barra_Temperatura As Integer
    Public S_Temperatura As Single
    Public S_Temperatura_Colocada As Single
    Public Verificacion_Temperatura As Single

    '-- Diseno flexion --
    Public Mu As Single
    Public Acero_Flexion As Single
    Public Cuantia_Flexion As Single
    Public Acero_Flexion_Colocado As Single
    Public Barra_Flexion As Integer
    Public S_Flexion As Single
    Public S_Flexion_Colocada As Single
    Public Verificacion_Flexion As Single

    '-- Diseno cortante --
    Public Vu As Single
    Public Vc As Single
    Public Verificacion_Cortante As Boolean


End Class
