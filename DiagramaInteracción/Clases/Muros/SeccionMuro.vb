<Serializable>
Public Class SeccionMuro

    Public Name As String
    Public Piso As String
    Public Altura As Single
    Public Seccion As String

    '------------ INFORMACION GENERAL DE LA SECCION ----------------
    Public fc As Single
    Public fy As Single
    Public Lw_Model As Single
    Public tw_Model As Single
    Public Direccion_Muro As eNumeradores.eDireccion
    Public Lw_Planos As Single
    Public tw_Planos As Single
    Public H_Libre As Single
    Public H As Single
    Public Coor_Z_Top As Single
    Public Coor_Z_Bot As Single

    Public AgM As Single

    Public S_Patron As Boolean
    Public S_Similar As String

    '--------------- VERIFICACIONES INICIALES ---------------
    Public V_Limite_Cuantia As Single

    '---------------------------- FLEXO-COMPRESION -----------------------------
    '----------- INFORMACION DE ENTRADA (REQUERIDA) ------------
    Public Malla As New Malla_Patron
    Public As_Top_Req As Single
    Public As_Bot_Req As Single
    Public Cuantia_Top_Req As Single
    Public Cuantia_Bot_Req As Single

    '----------- INFORMACION DE LEIDA (COLOCADA) ------------
    Public Refuerzo_Muro_Top As New Refuerzo_Longitudinal
    Public Cuantia_Top_Col As Single
    Public AsT_Top_Col As Single

    Public Refuerzo_Muro_Bottom As New Refuerzo_Longitudinal
    Public Cuantia_Bot_Col As Single
    Public AsT_Bot_Col As Single

    '-------- VERIFICACION DE CAPACIDAD FLEXO-COMPRESION -----------
    Public Rho_Min_L As Single
    Public F_Flexo_Top As Single
    Public F_Flexo_Bot As Single

    '-------------------------- CORTANTE --------------------------
    '----------- INFORMACION DE ENTRADA (REQUERIDA) ------------
    Public AsH_Req_Top As Single
    Public AsH_Req_Bot As Single

    ' ------------ INFORMACION LEIDA DE PLANOS ----------------
    Public RefH_W_Col As New Refuerzo_Patron
    Public AsH_Col As Single
    Public Rho_H_col As Single

    '--------------- VERIFICACION DE LA CAPACIDAD A CORTANTE ---------------
    Public Rho_Min_T As Single
    Public F_Cortante As Single
    Public Vu As Single
    Public Vc As Single
    Public Vs As Single
    Public Vn As Single

    '--------------- ELEMENTOS DE BORDE ---------------
    '----------------- VERIFICACION DE EB POR ESFUERZO ---------------
    Public Esf_Lim As Single
    Public Esf_max As Single

    Public Esf_I_Top As Single
    Public Esf_I_Bot As Single
    Public Chequeo_EB_I_Top_Esf As String
    Public Chequeo_EB_I_Bot_Esf As String

    Public Esf_D_Top As Single
    Public Esf_D_Bot As Single
    Public Chequeo_EB_D_Top_Esf As String
    Public Chequeo_EB_D_Bot_Esf As String

    Public Req_EB_I_Top_Esp As Boolean
    Public Req_EB_I_Top_NoEsp As Boolean
    Public Req_EB_I_Bot_Esp As Boolean
    Public Req_EB_I_Bot_NoEsp As Boolean

    Public Req_EB_D_Top_Esp As Boolean
    Public Req_EB_D_Top_NoEsp As Boolean
    Public Req_EB_D_Bot_Esp As Boolean
    Public Req_EB_D_Bot_NoEsp As Boolean

    '----------------- VERIFICACION DE EB POR DEFORMACIONES ---------------
    Public C_Limite As Single

    '------------ PROFUNDIDAD DE EJE NEUTRO LEIDA DEL ETABS -------------
    Public C_I_Top As Single
    Public C_I_Bot As Single
    Public C_D_Top As Single
    Public C_D_Bot As Single

    Public Chequeo_EB_I_Top_Def As String
    Public Chequeo_EB_I_Bot_Def As String
    Public Chequeo_EB_D_Top_Def As String
    Public Chequeo_EB_D_Bot_Def As String

    '------------ CREACION DE LOS EB -------------
    Public EB_I_Top As New ElementoBorde
    Public EB_I_Bot As New ElementoBorde
    Public EB_D_Top As New ElementoBorde
    Public EB_D_Bot As New ElementoBorde

    Public Lista_ALR As New List(Of ALR)
    Public Lista_Combinaciones As New List(Of Fuerzas_Elementos)

    Public ListaRefuerzos As New List(Of Refuerzo)

    <Serializable>
    Public Class ElementoBorde

        Public Tipo_EB_Col As String
        Public Tipo_EB_Req As String
        Public L_EB As Single
        Public L_EB_Req As Single
        Public Barras_L As New Refuerzo_Longitudinal
        Public Cuantia_L As Single
        Public Ash As Single
        Public Ash_Req As Single

        Public RefH As New Refuerzo_Patron
        Public RefH_Req As Single

    End Class

    <Serializable>
    Public Class Refuerzo_Longitudinal

        Public Barras_2 As Integer
        Public Barras_3 As Integer
        Public Barras_4 As Integer
        Public Barras_5 As Integer
        Public Barras_6 As Integer
        Public Barras_7 As Integer
        Public Barras_8 As Integer
        Public Barras_10 As Integer

    End Class

    <Serializable>
    Public Class Refuerzo_Patron

        Public Acero As String
        Public Capas As Integer
        Public Separacion As Single
        Public Acero_T As Single

    End Class

    <Serializable>
    Public Class Malla_Patron

        Public Malla As String
        Public Capas As Integer
        Public Acero_T As Single

    End Class

    <Serializable>
    Public Class Fuerzas_Elementos

        Public Name As String
        Public P As Single
        Public V2 As Single
        Public V3 As Single
        Public T As Single
        Public M2 As Single
        Public M3 As Single

    End Class

    <Serializable>
    Public Class ALR

        Public Combinacion As String
        Public ALR As Single

    End Class

End Class
