<Serializable>
Public Class Tramo_Columna

    ' ==========================================================
    ' IDENTIFICACIÓN
    ' ==========================================================
    Public Name_Elemento As String
    Public Piso As String
    Public Seccion As String
    Public Coordenadas As cCoordenadasElemento

    ' ==========================================================
    ' FUERZAS DEL ELEMENTO (Pu, V2, V3)
    ' ==========================================================
    Public Pu_V2 As Single
    Public Pu_V3 As Single
    Public V2 As Single
    Public V3 As Single

    ' ==========================================================
    ' FLEXO-COMPRESIÓN – REQUERIMIENTOS
    ' ==========================================================
    Public As_Req_Top As Single
    Public As_Req_Bottom As Single
    Public Cuantia_Req_Top As Single
    Public Cuantia_Req_Bottom As Single
    Public B_Modelo As Single
    Public H_Modelo As Single

    ' ==========================================================
    ' PROPIEDADES DE LA SECCIÓN
    ' ==========================================================
    Public B_Plano As Single
    Public H_Plano As Single
    Public fc As Single

    Public Refuerzo_Col_Top As New Refuerzo_Longitudinal
    Public Refuerzo_Col_Bottom As New Refuerzo_Longitudinal

    Public As_Col_Top As Single
    Public As_Col_Bottom As Single
    Public Cuantia_Col_Top As Single
    Public Cuantia_Col_Bottom As Single
    Public Cantidad_Barras_Top As Integer
    Public Cantidad_Barras_Bottom As Integer

    ' ==========================================================
    ' CORTANTE
    ' ==========================================================
    Public Numero_Barras_Estribo As String
    Public Separacion_Estribos As Single
    Public Separacion_Estribos_ZNC As Single

    Public As_Sent_Largo As Single
    Public As_Sent_Corto As Single
    Public Num_Ramas_Corto As Integer
    Public Num_Ramas_Largo As Integer

    Public Ash_Col_Corto As Single
    Public Ash_Col_Largo As Single

    Public Vc_2 As Single
    Public Vs_2 As Single
    Public Vn_2 As Single
    Public Vu_2 As Single
    Public F_Cortante_2 As Single

    Public Vc_3 As Single
    Public Vs_3 As Single
    Public Vn_3 As Single
    Public Vu_3 As Single
    Public F_Cortante_3 As Single

    ' ==========================================================
    ' CONFINAMIENTO
    ' ==========================================================
    Public Ash_L As Single
    Public S0_L As Single
    Public L0_L As Single
    Public Ramas_Req_L As Single

    Public Ash_C As Single
    Public S0_C As Single
    Public L0_C As Single
    Public Ramas_Req_C As Single

    Public Barra_Long_Min As String
    Public F_Ash_Corto As Single
    Public F_Ash_Largo As Single

    ' ==========================================================
    ' FLEXO-COMPRESIÓN – RESULTADOS FINALES
    ' ==========================================================
    Public F_Flexo_Top As Single
    Public F_Flexo_Bottom As Single
    Public F_Flexo_Modelo_Top As Single
    Public F_Flexo_Modelo_Bottom As Single

    ' ==========================================================
    ' DETALLES DEL REFUERZO LONGITUDINAL
    ' ==========================================================
    Public Cantidad_Lado_Largo_Top As Integer
    Public Cantidad_Lado_Corto_Top As Integer
    Public Cantidad_Lado_Largo_Bottom As Integer
    Public Cantidad_Lado_Corto_Bottom As Integer

    Public Lista_Detalles_Refuerzo_Top As New List(Of Detalles_Refuerzo_Longitudinal)
    Public Lista_Detalles_Refuerzo_Bottom As New List(Of Detalles_Refuerzo_Longitudinal)

    ' ==========================================================
    ' LISTA DE COMBINACIONES POR TRAMO
    ' ==========================================================
    Public Lista_Combinaciones As New List(Of Fuerzas_Elementos)

    ' ==========================================================
    ' LISTA DE REFUERZOS POR TRAMO
    ' ==========================================================
    Public ListaRefuerzos As New List(Of Refuerzo)

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
    Public Class Detalles_Refuerzo_Longitudinal
        Public Name_Barra As Integer
        Public Db As Single
        Public Asb As Single
        Public Coordenada_X As Single
        Public Coordenada_Y As Single
    End Class

    <Serializable>
    Public Class cCoordenadasElemento
        Public Property Xi As Single
        Public Property Yi As Single
        Public Property Zi As Single

        Public Property Xf As Single
        Public Property Yf As Single
        Public Property Zf As Single
    End Class

End Class


