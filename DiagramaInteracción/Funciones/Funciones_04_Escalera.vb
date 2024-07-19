Public Class Funciones_04_Escalera

    Public Shared Function Momento_Escalera(ByVal x As Single, ByVal X1 As Single, ByVal X2 As Single, ByVal WU1 As Single, ByVal WU2 As Single) As Single

        Dim R1 As Single = (WU1 * X1 * (X2 + X1 / 2) + WU2 * X2 ^ 2 / 2) / (X1 + X2)

        If x < X1 Then
            Momento_Escalera = R1 * x - WU1 * x ^ 2 / 2
        Else
            Momento_Escalera = R1 * x - WU2 * (x ^ 2 - 2 * x * X1 + X1 ^ 2) / 2 - WU1 * X1 * (x - X1 / 2)
        End If


    End Function

    Public Shared Function Cortante_Escalera(ByVal x As Single, ByVal X1 As Single, ByVal X2 As Single, ByVal WU1 As Single, ByVal WU2 As Single) As Single

        Dim R1 As Single = (WU1 * X1 * (X2 + X1 / 2) + WU2 * X2 ^ 2 / 2) / (X1 + X2)

        If x < X1 Then
            Cortante_Escalera = R1 - WU1 * x
        Else
            Cortante_Escalera = R1 - WU2 * (x - X1) - WU1 * X1
        End If

    End Function

    Public Shared Function Interpolar(ByVal D As Single, ByVal X0 As Single, ByVal X1 As Single, ByVal Y0 As Single, ByVal Y1 As Single) As Single
        Interpolar = (D * X0) * (Y1 - Y0) / (X1 - X0) + Y0
    End Function


End Class
