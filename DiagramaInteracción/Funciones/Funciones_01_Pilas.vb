Imports ARCO.Funciones_00_Varias
Public Class Funciones_01_Pilas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto


    '-------------------------- FUNCIÓN PARA DETERMINAR EL DIAGRAMA DE INTERACCIÓN EN UNA SECCIÓN CIRCULAR --------------------------
    Public Shared Function DiagramaInteraccionCircular(ByVal D As Single, ByVal R As Single, ByVal Barra As String, ByVal AreaBarraL As Single, ByVal NBarras As Single, ByVal Fc As Single, ByVal Es As Single, ByVal ecu As Single, ByVal Fy As Single)
        Dim esy As Single = Fy / Es
        Dim Rb As Single = D / 2 - R
        Dim Ag As Single = Math.PI * D ^ 2 / 4
        Dim es1 As Single
        Dim Mst As Single
        Dim Fst As Single
        Dim c As Single
        Dim di As Single
        Dim esi As Single
        Dim fsi As Single
        Dim b1 As Single
        Dim a As Single
        Dim x As Single
        Dim Beta As Single
        Dim Tetha As Single
        Dim Acc As Single
        Dim Mc As Single
        Dim Fs As Single
        Dim Phi As Single
        Dim Cc As Single
        Dim Mcc As Single
        Dim P As Single
        Dim M As Single
        Dim Db As Single = DiametroRefuerzo(Barra)
        Dim Ac As Single = AreaRefuerzo(Barra)
        Dim Acero As Single = Ac / 1000000
        Dim AceroT As Single = Acero * NBarras
        Dim Pmax As Single = 0.85 * Fc * (Ag - AceroT) * 1000 + Fy * AceroT * 1000
        Dim Pmin As Single = -Fy * AceroT * 1000
        Dim Resultados(1000, 5) As Single
        Resultados(1, 1) = Pmax
        Resultados(1, 2) = 0
        Resultados(1, 3) = Pmax * 0.8 * 0.65
        Resultados(1, 4) = 0
        Dim MinY As Single = 0
        Dim YBarras(100) As Single
        For i = 1 To NBarras
            Beta = (360 / NBarras) * i
            YBarras(i) = (D / 2) - Rb * Math.Sin(Beta * Math.PI / 180)
            If MinY <= YBarras(i) Then
                MinY = YBarras(i)
            End If
        Next
        Dim k As Single = 2
        Dim desy As Single = esy / 100
        For c = D To 0.04 Step -(D - 0.04) / 10
            Mst = 0
            Fst = 0
            For i = 1 To NBarras
                di = YBarras(i)
                esi = (c - di) * 0.003 / c
                fsi = Es * esi
                If fsi > Fy Then
                    fsi = Fy
                ElseIf fsi < -Fy Then
                    fsi = -Fy
                End If
                b1 = 0.85 - (0.05 * (Fc - 28) / 7)
                If b1 < 0.65 Then
                    b1 = 0.65
                End If
                If b1 > 0.85 Then
                    b1 = 0.85
                End If
                a = b1 * c
                If a <= (D / 2) Then
                    x = ((D / 2) - a) / (D / 2)
                    Tetha = Math.Acos(x)
                Else
                    If a > D Then
                        x = 1
                    Else
                        x = (a - (D / 2)) / (D / 2)
                    End If
                    If x = 1 Then
                        Tetha = Math.PI
                    Else
                        Tetha = Math.PI - Math.Acos(x)
                    End If
                End If
                Acc = D ^ 2 * (Tetha - Math.Sin(Tetha) * Math.Cos(Tetha)) / 4
                Mc = D ^ 3 * (Math.Sin(Tetha) ^ 3) / 12
                If c < di Then
                    Fs = fsi * Acero * 1000
                End If
                If c >= di Then
                    Fs = (fsi - 0.85 * Fc) * Acero * 1000
                End If
                Fst = Fs + Fst
                Mst = Fs * (D / 2 - di) + Mst
            Next
            es1 = (MinY - c) * 0.003 / c
            If Math.Abs(es1) <= esy Then
                Phi = 0.65
            ElseIf Math.Abs(es1) > esy And Math.Abs(es1) <= (2.5 * esy) Then
                Phi = 0.65 + (Math.Abs(es1) - 0.002) * (250 / 3)
            Else
                Phi = 0.9
            End If
            If Math.Abs(es1) > 0.015 Then
                Exit For
            End If
            Cc = Acc * Fc * 1000 * 0.85
            Mcc = Mc * Fc * 1000 * 0.85
            P = Cc + Fst
            M = Mcc + Mst
            Resultados(k, 1) = P
            Resultados(k, 2) = M
            Resultados(k, 3) = P * Phi
            Resultados(k, 4) = M * Phi
            If P * Phi > Pmax * 0.65 * 0.8 Then
                Resultados(k, 3) = Pmax * 0.65 * 0.8
            End If
            k += 1
        Next
        Resultados(k, 1) = Pmin
        Resultados(k, 2) = 0
        Resultados(k, 3) = Pmin * 0.9
        Resultados(k, 4) = 0
        Resultados(1, 5) = k
        DiagramaInteraccionCircular = Resultados
    End Function

    '-------------------------- REVISIÓN A CORTANTE --------------------------
    Public Shared Function ShearCheck(ByVal D As Single, ByVal fc As Single, ByVal Fy As Single, ByVal s As Single, ByVal Ref_Trans As String, ByVal Lista_V2 As List(Of Single),
                                      ByVal Lista_V3 As List(Of Single), ByVal Lista_Pu As List(Of Single), ByVal Opcion_Elemento As String, ByVal Esp_Anillo As Single)
        Dim Vc As Single
        Dim Vc1 As Single
        Dim Vc2 As Single
        Dim Vuu As Single
        Dim Vu As Single
        Dim Vnmax As Single
        Dim Nu As Single
        Dim Revision(1, 7)
        Dim Fmax As Single = 100
        Dim DbT As Single = DiametroRefuerzo(Ref_Trans)
        Dim Ass As Single = AreaRefuerzo(Ref_Trans)
        Dim FT As Single = -1

        If Proyecto.Pilas.Opcion_Elemento = "Punto" Then
            FT = 1
        End If

        Dim Ag As Single = Math.PI * D ^ 2 / 4
        Dim D2 As Single
        Dim RelA_E As Single

        If Opcion_Elemento = "Si" Then
            D2 = D - 2 * Esp_Anillo
            Ag = Math.PI * (D ^ 2 ^ -D2 ^ 2) / 4
            RelA_E = D / Esp_Anillo
        End If

        Dim Ae As Single = D * 0.8 * D
        Dim Vc0 As Single = 0.17 * 0.75 * Math.Sqrt(fc) * Ag * 1000                                                   'C.11-3
        Dim Vs As Single = 0.75 * 2 * Ass * Fy * (0.8 * D) / (s * 1000)
        Dim Vn As Single

        Revision(1, 1) = "φVc"
        Revision(1, 2) = "Chequeo V2"
        Revision(1, 3) = "Chequeo V3"
        Revision(1, 4) = "φVc/Vu"

        For i = 0 To Lista_V2.Count - 1
            Nu = FT * Lista_Pu(i)
            Vc1 = 0.17 * 0.75 * (1 + (Nu / (14000 * Ag))) * Math.Sqrt(fc) * Ae * 1000                       'C.11-4
            Vc2 = 0.29 * Math.Sqrt(fc) * Ae * 1000 * Math.Sqrt(1 + (0.29 * Nu / (1000 * Ag)))
            Vc = Math.Min(Vc1, Vc0)

            If Opcion_Elemento = "Si" Then
                'Vc = RelA_E / (D * Esp_Anillo ^ 2)
            End If

            Vn = Vc + Vs
            Vuu = Math.Max(Lista_V2(i), Lista_V3(i))

            If Fmax > Vn / Vuu Then
                Fmax = Vn / Vuu
                Vu = Vuu
                Vnmax = Vn
                If Lista_V2(i) <= Vn And Lista_V3(i) <= Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "Cumple V2"
                    Revision(1, 3) = "Cumple V3"
                ElseIf Lista_V2(i) > Vn And Lista_V3(i) <= Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "No Cumple V2"
                    Revision(1, 3) = "Cumple V3"
                ElseIf Lista_V2(i) <= Vn And Lista_V3(i) > Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "Cumple V2"
                    Revision(1, 3) = "No Cumple V3"
                ElseIf Lista_V2(i) > Vn And Lista_V3(i) > Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "No Cumple V2"
                    Revision(1, 3) = "No Cumple V3"
                End If
            End If
        Next

        Revision(1, 4) = Math.Round(Fmax, 2)
        Revision(1, 5) = Math.Round(Vu, 3)
        Revision(1, 6) = Math.Round(Vs, 3)
        Revision(1, 7) = Math.Round(Vnmax, 3)
        ShearCheck = Revision
    End Function

    '---------------------------- CALCULO DE RELACIÓN CAPACIDAD/DEMANDA POR MEDIO DE RECTA DIAGONAL EN EL DI -----------------------
    Public Shared Function FDiagonal(ByVal Psol As Double, ByVal Msol As Double, ByVal Lista_Pn As List(Of Single), ByVal Lista_Mn As List(Of Single))
        Dim m_sol As Double
        Dim d_sol As Double
        If Msol > 0 Then
            m_sol = Psol / Msol
            d_sol = Math.Sqrt(Psol ^ 2 + Msol ^ 2)
        ElseIf Msol = 0 And Psol >= 0 Then
            m_sol = 9999
            d_sol = Psol
        ElseIf Msol = 0 And Psol < 0 Then
            m_sol = -9999
            d_sol = Psol
        End If
        Dim d_cap As Double
        For i = 0 To Lista_Pn.Count - 1
            Dim P1 As Double
            Dim P2 As Double
            Dim M1 As Double
            Dim M2 As Double
            If i <= Lista_Pn.Count - 2 Then
                P1 = Lista_Pn(i)
                P2 = Lista_Pn(i + 1)
                M1 = Lista_Mn(i)
                M2 = Lista_Mn(i + 1)
            End If
            Dim m_1 As Double
            Dim m_2 As Double
            If M1 = 0 Then
                m_1 = 9999
            Else
                m_1 = P1 / M1
            End If
            If M2 = 0 Then
                m_2 = -9999
            Else
                m_2 = P2 / M2
            End If
            If m_sol <= m_1 And m_sol >= m_2 Then
                Dim m As Double = (P1 - P2) / (M1 - M2)
                Dim x As Double = (P1 - m * M1) / (m_sol - m)
                Dim y As Double = m_sol * x
                d_cap = Math.Sqrt(x ^ 2 + y ^ 2)
                Exit For
            Else
                If Psol > 0 Then
                    d_cap = Math.Abs(Lista_Pn(0))
                Else
                    d_cap = Math.Abs(Lista_Pn(Lista_Pn.Count() - 1))
                End If
            End If
        Next
        Dim F = Math.Abs(d_cap / d_sol)
        FDiagonal = F
    End Function

    '---------------------------- CALCULO DE RELACIÓN CAPACIDAD/DEMANDA POR MEDIO DE CORTES HORIZONTALES EN EL DI -----------------------
    Public Shared Function FCorte(ByVal Psol As Double, ByVal Msol As Double, ByVal Lista_Pn As List(Of Single), ByVal Lista_Mn As List(Of Single))
        Dim F
        If Psol > Lista_Pn.Max Or Psol < Lista_Pn.Min Then
            F = 0
        Else
            Dim d_sol As Double = Msol
            Dim d_cap As Double
            For i = 0 To Lista_Pn.Count - 1
                Dim P1 As Double
                Dim P2 As Double
                Dim M1 As Double
                Dim M2 As Double
                If i <= Lista_Pn.Count - 2 Then
                    P1 = Lista_Pn(i)
                    P2 = Lista_Pn(i + 1)
                    M1 = Lista_Mn(i)
                    M2 = Lista_Mn(i + 1)
                End If

                Dim m As Double = (P1 - P2) / (M1 - M2)
                If P1 >= Psol And Psol > P2 Then
                    Dim x As Double = (Psol - P1) / m
                    d_cap = x + M1
                    Exit For
                End If
            Next

            If d_sol > 0 Then
                F = d_cap / d_sol
            Else
                F = 99
            End If
        End If
        FCorte = F
    End Function

End Class
