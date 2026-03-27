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
