<Serializable>
Public Class Muro

    Public Name As String
    Public Label As String
    Public Direccion As eNumeradores.eDireccion
    Public TipoMuro As eNumeradores.eTipoMuro

    Public Lista_Secciones As New List(Of SeccionMuro)

    Public Ref_Modificado_Muros As Boolean

    Public N_Pisos As Integer
    Public Lw As Single
    Public tw As Single
    Public Hw As Single
    Public Ar As Single
    Public Ag_M As Single
    Public fc_Base As Single

    Public Coor_X As Single
    Public Coor_Y As Single

    Public LV_EB_I_Col_Esp As Single
    Public LV_EB_I_Col_NoEsp As Single
    Public LV_EB_I_Req_Def As Single
    Public LV_EB_I_Req_Esf As Single

    Public LV_EB_D_Col_Esp As Single
    Public LV_EB_D_Col_NoEsp As Single
    Public LV_EB_D_Req_Def As Single
    Public LV_EB_D_Req_Esf As Single

    Public Pmax_G As Single
    Public Pmax_D As Single
    Public Vmax_S As Single
    Public ALR_G As Single
    Public ALR_D As Single
    Public Porc_Vs As Single
    Public Porc_Vs_Geo As Single

End Class

