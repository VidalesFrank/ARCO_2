Public Class Funciones_04_Escalera

    Public Shared Function MomentoEscalera(ByVal x As Single, ByVal X1 As Single, ByVal X2 As Single, ByVal WU1 As Single, ByVal WU2 As Single) As Single

        Dim R1 As Single = (WU1 * X1 * (X2 + X1 / 2) + WU2 * X2 ^ 2 / 2) / (X1 + X2)

        If x < X1 Then
            MomentoEscalera = R1 * x - WU1 * x ^ 2 / 2
        Else
            MomentoEscalera = R1 * x - WU2 * (x ^ 2 - 2 * x * X1 + X1 ^ 2) / 2 - WU1 * X1 * (x - X1 / 2)
        End If

    End Function

    Public Shared Function CortanteEscalera(ByVal x As Single, ByVal X1 As Single, ByVal X2 As Single, ByVal WU1 As Single, ByVal WU2 As Single) As Single

        Dim R1 As Single = (WU1 * X1 * (X2 + X1 / 2) + WU2 * X2 ^ 2 / 2) / (X1 + X2)

        If x < X1 Then
            CortanteEscalera = R1 - WU1 * x
        Else
            CortanteEscalera = R1 - WU2 * (x - X1) - WU1 * X1
        End If

    End Function

    Public Shared Function Interpolar(ByVal D As Single, ByVal X0 As Single, ByVal X1 As Single, ByVal Y0 As Single, ByVal Y1 As Single) As Single
        Interpolar = (D * X0) * (Y1 - Y0) / (X1 - X0) + Y0
    End Function

    ''' <summary>
    ''' Inercia bruta de la sección rectangular por metro de ancho (m^4/m).
    ''' b en metros, h en metros. Retorna m^4.
    ''' </summary>
    Public Shared Function CalcularIg(ByVal b As Single, ByVal h As Single) As Single
        Return b * h ^ 3 / 12.0F
    End Function

    ''' <summary>
    ''' Inercia de la sección fisurada (solo zona en tensión ignorada).
    ''' b, h, d, recubrimiento en metros; As en m^2; n = Es/Ec (adimensional).
    ''' </summary>
    Public Shared Function CalcularIcr(ByVal b As Single, ByVal d As Single, ByVal As_m2 As Single, ByVal n As Single) As Single
        ' Profundidad del eje neutro fisurado: b*c^2/2 = n*As*(d-c)
        Dim a As Single = b / 2.0F
        Dim bCoef As Single = n * As_m2
        Dim cConst As Single = -n * As_m2 * d
        Dim disc As Single = bCoef ^ 2 - 4 * a * cConst
        If disc < 0 Then Return 0
        Dim c As Single = (-bCoef + Math.Sqrt(disc)) / (2 * a)
        If c <= 0 Then c = 0.001F
        Dim Icr As Single = b * c ^ 3 / 3.0F + n * As_m2 * (d - c) ^ 2
        Return Icr
    End Function

    ''' <summary>
    ''' Inercia efectiva de Branson (NSR-10 C.9.5.2.3).
    ''' Ma en kN·m, Mcr en kN·m, Ig y Icr en m^4.
    ''' </summary>
    Public Shared Function CalcularIe(ByVal Mcr As Single, ByVal Ma As Single, ByVal Ig As Single, ByVal Icr As Single) As Single
        If Ma <= 0 Then Return Ig
        Dim ratio As Single = Math.Min(Mcr / Ma, 1.0F)
        Dim Ie As Single = ratio ^ 3 * Ig + (1 - ratio ^ 3) * Icr
        Return Math.Min(Ie, Ig)
    End Function

    ''' <summary>
    ''' Deflexión máxima de viga simplemente apoyada con carga uniforme.
    ''' w en kN/m, L en m, Ec en kN/m^2, I en m^4. Retorna deflexión en m.
    ''' </summary>
    Public Shared Function CalcularDeflexion(ByVal w As Single, ByVal L As Single, ByVal Ec As Single, ByVal I As Single) As Single
        If Ec <= 0 OrElse I <= 0 Then Return 0
        Return (5.0F / 384.0F) * (w * L ^ 4) / (Ec * I)
    End Function

    ''' <summary>
    ''' Carga uniforme equivalente para tramo con dos cargas distintas (peldaños + descanso).
    ''' Energía de deformación equivalente: w_eq·L = w1·L1 + w2·L2.
    ''' </summary>
    Public Shared Function CargaEquivalente(ByVal w1 As Single, ByVal L1 As Single, ByVal w2 As Single, ByVal L2 As Single) As Single
        Dim L As Single = L1 + L2
        If L <= 0 Then Return 0
        Return (w1 * L1 + w2 * L2) / L
    End Function

End Class
