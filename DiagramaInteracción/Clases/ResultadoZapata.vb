<Serializable>
Public Class ResultadoZapata

    ' Capacidad del suelo
    Public Property CumpleCapacidad As Boolean
    Public Property qMax As Double
    Public Property qMin As Double
    Public Property g1 As Double
    Public Property g2 As Double
    Public Property g3 As Double
    Public Property g4 As Double

    ' Excentricidad — quinta revisión (NSR-10 C.15). El signo de ex, ey se
    ' conserva para poder dibujar hacia qué lado se corre la resultante; la
    ' verificación se hace sobre |e|.
    <Runtime.Serialization.OptionalField> Public ex As Double
    <Runtime.Serialization.OptionalField> Public ey As Double
    <Runtime.Serialization.OptionalField> Public Lim_x_base As Double
    <Runtime.Serialization.OptionalField> Public Lim_y_base As Double
    <Runtime.Serialization.OptionalField> Public Lim_x_usado As Double
    <Runtime.Serialization.OptionalField> Public Lim_y_usado As Double
    <Runtime.Serialization.OptionalField> Public CumpleExcentricidad_X As Boolean
    <Runtime.Serialization.OptionalField> Public CumpleExcentricidad_Y As Boolean
    <Runtime.Serialization.OptionalField> Public CumpleExcentricidad As Boolean

    ' Pesos estabilizantes que sumaron a P para las revisiones que los usan
    ' (suelo y excentricidad). Se guardan aunque el flag esté apagado, con W = 0,
    ' para poder auditar por qué salió tal o cual factor.
    <Runtime.Serialization.OptionalField> Public W_Zapata As Double
    <Runtime.Serialization.OptionalField> Public W_Pedestal As Double
    <Runtime.Serialization.OptionalField> Public W_Suelo As Double
    <Runtime.Serialization.OptionalField> Public P_Efectivo As Double
    <Runtime.Serialization.OptionalField> Public UsoPesoEstabilizante As Boolean

    ' Cargas de entrada guardadas explícitamente para poder auditar. Antes se
    ' tenía que "recuperar" Mx = ey·P para dibujar, y una zapata con g1..g4
    ' iguales podía significar tanto "moments = 0 en el modelo" como "algo se
    ' perdió en el camino"; con estos campos se distingue.
    <Runtime.Serialization.OptionalField> Public P_Reactivo As Double
    <Runtime.Serialization.OptionalField> Public Mx_Entrada As Double
    <Runtime.Serialization.OptionalField> Public My_Entrada As Double

    ' Punzonamiento
    Public Property CumplePunzonamiento As Boolean
    Public Property Vu_p As Double
    Public Property Vc1_p As Double
    Public Property Vc2_p As Double
    Public Property Vc3_p As Double
    Public Property Vc_p As Double
    Public Property g5 As Double
    Public Property g6 As Double
    Public Property g7 As Double
    Public Property g8 As Double


    ' Cortante
    Public Property CumpleCortante_1 As Boolean
    Public Property CumpleCortante_2 As Boolean
    Public Property CumpleCortante_3 As Boolean
    Public Property CumpleCortante_4 As Boolean
    Public Property Vu1_C As Double
    Public Property Vu2_C As Double
    Public Property Vu3_C As Double
    Public Property Vu4_C As Double
    Public Property Vc1_C As Double
    Public Property Vc2_C As Double
    Public Property gf_C As Double
    Public Property ga_C As Double
    Public Property gi_C As Double
    Public Property ge_C As Double
    Public Property gk_C As Double
    Public Property gg_C As Double
    Public Property gc_C As Double
    Public Property gj_C As Double


    ' Flexión
    Public Property Mu_1 As Double
    Public Property Mu_2 As Double
    Public Property Rho_1 As Double
    Public Property Rho_2 As Double
    Public Property gf_F As Double
    Public Property ga_F As Double
    Public Property gi_F As Double
    Public Property ge_F As Double
    Public Property gk_F As Double
    Public Property gg_F As Double
    Public Property gc_F As Double
    Public Property gj_F As Double
    Public Property Cumple_L1 As Boolean
    Public Property Cumple_L2 As Boolean


    ' Resultado general
    Public Property CumpleGeneral As Boolean


End Class
