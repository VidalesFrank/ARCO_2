
Imports System.Linq
Imports ADODB
Imports ARCO.cZapata
Imports ARCO.eNumeradores
Imports DocumentFormat.OpenXml.Office2016.Drawing.Charts
Imports iTextSharp.awt.geom.Point2D
Imports Org.BouncyCastle.Crypto.Digests

Public Module Funciones_Zapatas

    Public Function EvaluarZapata(z As cZapata,
                              P As Double,
                              Mx As Double,
                              My As Double,
                                  Op_Comb As String) As ResultadoZapata

        Dim R As New ResultadoZapata

        '-----------------------------
        ' 1. Capacidad del suelo
        '-----------------------------
        Dim g_Adm As Double = z.qAdm_Est
        If Op_Comb = "DIN" Then
            g_Adm = z.qAdm_Din
        End If

        Dim resCap = VerificarCapacidadSuelo(z, P, Mx, My, g_Adm)
        R.CumpleCapacidad = resCap.Cumple
        R.qMax = resCap.qMax
        R.qMin = resCap.qMin
        R.g1 = resCap.g1
        R.g2 = resCap.g2
        R.g3 = resCap.g3
        R.g4 = resCap.g4

        '-----------------------------
        ' 2. Punzonamiento
        '-----------------------------
        Dim resPun = VerificarPunzonamiento(z, P, Mx, My)
        R.CumplePunzonamiento = resPun.Cumple
        R.Vu_p = resPun.Vu
        R.Vc1_p = resPun.vc_1
        R.Vc2_p = resPun.vc_2
        R.Vc3_p = resPun.vc_3
        R.Vc_p = resPun.Vc
        R.g5 = resPun.g5
        R.g6 = resPun.g6
        R.g7 = resPun.g7
        R.g8 = resPun.g8

        '-----------------------------
        ' 3. Cortante
        '-----------------------------
        Dim resCort = VerificarCortante(z, P, Mx, My)
        R.CumpleCortante_1 = resCort.Cumple_1
        R.CumpleCortante_2 = resCort.Cumple_2
        R.CumpleCortante_3 = resCort.Cumple_3
        R.CumpleCortante_4 = resCort.Cumple_4
        R.Vu1_C = resCort.Vu_1
        R.Vu2_C = resCort.Vu_2
        R.Vu3_C = resCort.Vu_3
        R.Vu4_C = resCort.Vu_4
        R.Vc1_C = resCort.Vc_1
        R.Vc2_C = resCort.Vc_2
        R.gf_C = resCort.gf
        R.ga_C = resCort.ga
        R.gi_C = resCort.gi
        R.ge_C = resCort.ge
        R.gk_C = resCort.gk
        R.gg_C = resCort.gg
        R.gc_C = resCort.gc
        R.gj_C = resCort.gj

        '-----------------------------
        ' 4. Flexión
        '-----------------------------
        Dim resFlex = VerificarFlexion(z, P, Mx, My)
        R.Mu_1 = resFlex.Mu_1
        R.Mu_2 = resFlex.Mu_2
        R.Rho_1 = resFlex.Rho_1
        R.Rho_2 = resFlex.Rho_2
        R.gf_F = resFlex.gf
        R.ga_F = resFlex.ga
        R.gi_F = resFlex.gi
        R.ge_F = resFlex.ge
        R.gk_F = resFlex.gk
        R.gg_F = resFlex.gg
        R.gc_F = resFlex.gc
        R.gj_F = resFlex.gj
        R.Cumple_L1 = resFlex.Cumple_L1
        R.Cumple_L2 = resFlex.Cumple_L2

        '-----------------------------
        ' Resultado global
        '-----------------------------
        R.CumpleGeneral =
        R.CumpleCapacidad And
        R.CumplePunzonamiento And
        R.CumpleCortante_1 And R.CumpleCortante_2 And R.CumpleCortante_3 And R.CumpleCortante_4 And
        R.Cumple_L1 And R.Cumple_L2

        Return R

    End Function


    Public Function VerificarCapacidadSuelo(z As cZapata,
                                        P As Double,
                                        Mx As Double,
                                        My As Double,
                                        g_Adm As Double) As (Cumple As Boolean,
                                        qMax As Double,
                                        qMin As Double,
                                        g1 As Double, g2 As Double, g3 As Double, g4 As Double)

        Dim L1 = z.L_b
        Dim L2 = z.L_h
        Dim e = z.e
        Dim d = z.d
        Dim b = z.b
        Dim h = z.h

        Dim A_z = L1 * L2

        Dim g1 = P / A_z + 6 * Mx / (L1 * L2 ^ 2) + 6 * My / (L2 * L1 ^ 2)
        Dim g2 = P / A_z - 6 * Mx / (L1 * L2 ^ 2) + 6 * My / (L2 * L1 ^ 2)
        Dim g3 = P / A_z - 6 * Mx / (L1 * L2 ^ 2) - 6 * My / (L2 * L1 ^ 2)
        Dim g4 = P / A_z + 6 * Mx / (L1 * L2 ^ 2) - 6 * My / (L2 * L1 ^ 2)

        ' Presión máxima y mínima
        Dim qMax As Double = Math.Max(Math.Max(g1, g2), Math.Max(g3, g4))
        Dim qMin As Double = Math.Min(Math.Min(g1, g2), Math.Min(g3, g4))

        ' Criterio NSR / ACI típico:
        ' 1) qMax ≤ qAdm
        ' 2) qMin ≥ 0  → no hay tracción
        Dim cumple As Boolean = (0.9 * qMax <= g_Adm AndAlso qMin >= 0)

        Return (cumple, qMax, qMin, g1, g2, g3, g4)

    End Function


    Public Function VerificacionExcentricidad(z As cZapata,
                                         P As Double,
                                         Mx As Double,
                                         My As Double,
                                         tipo As TipoCombinacion,
                                         Optional factorDinamico As Double = 1.5) _
    As (ex As Double,
        ey As Double,
        LimX As Double,
        LimY As Double,
        LimX_Usado As Double,
        LimY_Usado As Double,
        CumpleX As Boolean,
        CumpleY As Boolean,
        Cumple As Boolean,
        Mensaje As String)

        ' Evitar división por cero en caso de P = 0
        If Math.Abs(P) < 0.000000001 Then
            Return (Double.NaN, Double.NaN, Double.NaN, Double.NaN, Double.NaN, Double.NaN, False, False, False, "P = 0 (sin carga vertical)")
        End If

        ' Según tu convención: ey = Mx / P  (Mx produce excentricidad en Y)
        Dim ey As Double = Mx / P
        Dim ex As Double = My / P

        ' Límites base (estáticos)
        Dim Lim_x_base As Double = z.L_b / 6.0
        Dim Lim_y_base As Double = z.L_h / 6.0

        ' Determinar límites efectivos según tipo de combinación
        Dim Lim_x_usado As Double = Lim_x_base
        Dim Lim_y_usado As Double = Lim_y_base

        If tipo = TipoCombinacion.ServicioDinamica Then
            ' aplicar factor dinámico (configurable)
            Lim_x_usado = Lim_x_base * factorDinamico
            Lim_y_usado = Lim_y_base * factorDinamico
        End If

        ' Comprobaciones
        Dim cumpleX As Boolean = (Math.Abs(ex) <= Lim_x_usado)
        Dim cumpleY As Boolean = (Math.Abs(ey) <= Lim_y_usado)
        Dim cumple As Boolean = (cumpleX AndAlso cumpleY)

        ' Mensaje explicativo
        Dim msg As New System.Text.StringBuilder()
        msg.AppendLine($"ex = {ex:F4} m  (L_b/6 = {Lim_x_base:F4} m; usado = {Lim_x_usado:F4} m)")
        msg.AppendLine($"ey = {ey:F4} m  (L_h/6 = {Lim_y_base:F4} m; usado = {Lim_y_usado:F4} m)")
        If cumple Then
            msg.Append("Cumple límite de excentricidad.")
        Else
            msg.Append("No cumple límite de excentricidad en: ")
            If Not cumpleX Then msg.Append("ex ")
            If Not cumpleY Then msg.Append("ey")
        End If

        Return (ex, ey, Lim_x_base, Lim_y_base, Lim_x_usado, Lim_y_usado, cumpleX, cumpleY, cumple, msg.ToString())

    End Function


    Public Function VerificarPunzonamiento(z As cZapata,
                                        P As Double,
                                        Mx As Double,
                                        My As Double,
                                           Optional FD As Double = 1.5) As (Cumple As Boolean,
                                           Vu As Double,
                                           vc_1 As Double, vc_2 As Double, vc_3 As Double, Vc As Double,
                                           g5 As Double, g6 As Double, g7 As Double, g8 As Double)

        Dim L1 = z.L_b
        Dim L2 = z.L_h
        Dim e = z.e
        Dim d = z.d
        Dim b = z.b
        Dim h = z.h

        Dim A_z = L1 * L2

        Dim g5 = P / A_z + 6 * Mx * (h + d) / (L1 * L2 ^ 3) + 6 * My * (b + d) / (L2 * L1 ^ 3)
        Dim g6 = P / A_z - 6 * Mx * (h + d) / (L1 * L2 ^ 3) + 6 * My * (b + d) / (L2 * L1 ^ 3)
        Dim g7 = P / A_z - 6 * Mx * (h + d) / (L1 * L2 ^ 3) - 6 * My * (b + d) / (L2 * L1 ^ 3)
        Dim g8 = P / A_z + 6 * Mx * (h + d) / (L1 * L2 ^ 3) - 6 * My * (b + d) / (L2 * L1 ^ 3)

        Dim valores() As Double = {g5, g6, g7, g8}
        Dim g_prom As Double = valores.Average()

        Dim Vu As Double = (P - g_prom * (b + d) * (h + d)) * FD

        Dim betha As Double = Math.Max(h, b) / Math.Min(h, b)
        Dim bo As Double = 2 * (h + b) + 4 * d
        Dim alpha_s = 40

        Dim vc_1 As Double = 0.75 * 0.17 * Math.Sqrt(z.fc) * (1 + 2 / betha) * bo * d * 1000
        Dim vc_2 As Double = 0.75 * 0.083 * (alpha_s * d / bo + 2) * Math.Sqrt(z.fc) * bo * d * 1000
        Dim vc_3 As Double = 0.75 * 0.33 * Math.Sqrt(z.fc) * bo * d * 1000

        Dim Vc As Double = {vc_1, vc_2, vc_3}.Min()

        Dim cumple = (0.9 * Vu <= Vc)

        Return (cumple, Vu, vc_1, vc_2, vc_3, Vc, g5, g6, g7, g8)

    End Function


    Public Function VerificarCortante(z As cZapata,
                                  P As Double,
                                      Mx As Double,
                                      My As Double,
                                      Optional FD As Double = 1.5) As (Cumple_1 As Boolean,
                                      Cumple_2 As Boolean,
                                      Cumple_3 As Boolean,
                                      Cumple_4 As Boolean,
                                      Vu_1 As Double,
                                      Vu_2 As Double,
                                      Vu_3 As Double,
                                      Vu_4 As Double,
                                      Vc_1 As Double,
                                      Vc_2 As Double,
                                      gf As Double,
                                      ga As Double,
                                      gi As Double,
                                      ge As Double,
                                      gk As Double,
                                      gg As Double,
                                      gc As Double,
                                      gj As Double)

        Dim L1 = z.L_b
        Dim L2 = z.L_h
        Dim e = z.e
        Dim d = z.d
        Dim b = z.b
        Dim h = z.h

        Dim A_z = L1 * L2

        '___ Tensiones en 1  {1-2-f-a}
        Dim g1 = P / A_z + 6 * Mx / (L1 * L2 ^ 2) + 6 * My / (L2 * L1 ^ 2)
        Dim g2 = P / A_z - 6 * Mx / (L1 * L2 ^ 2) + 6 * My / (L2 * L1 ^ 2)
        Dim gf = P / A_z - 6 * Mx / (L1 * L2 ^ 2) + 12 * My * (b / 2 + d) / (L2 * L1 ^ 3)
        Dim ga = P / A_z + 6 * Mx / (L1 * L2 ^ 2) + 12 * My * (b / 2 + d) / (L2 * L1 ^ 3)

        Dim valores_t1() As Double = {g1, g2, gf, ga}
        Dim g_prom_t1 As Double = valores_t1.Average()
        Dim Vu_1 As Double = g_prom_t1 * L2 * (L1 / 2 - b / 2 - d) * FD

        '___ Tensiones en 2  {2-3-i-e}
        Dim g3 = P / A_z - 6 * Mx / (L1 * L2 ^ 2) - 6 * My / (L2 * L1 ^ 2)
        Dim gi = P / A_z - 12 * Mx * (h / 2 + d) / (L1 * L2 ^ 3) - 6 * My / (L2 * L1 ^ 2)
        Dim ge = P / A_z - 12 * Mx * (h / 2 + d) / (L1 * L2 ^ 3) + 6 * My / (L2 * L1 ^ 2)

        Dim valores_t2() As Double = {g2, g3, gi, ge}
        Dim g_prom_t2 As Double = valores_t2.Average()
        Dim Vu_2 As Double = g_prom_t2 * L1 * (L2 / 2 - h / 2 - d) * FD

        '___ Tensiones en 3  {3-4-k-g}
        Dim g4 = P / A_z + 6 * Mx / (L1 * L2 ^ 2) - 6 * My / (L2 * L1 ^ 2)
        Dim gk = P / A_z + 6 * Mx / (L1 * L2 ^ 2) - 12 * My * (b / 2 + d) / (L2 * L1 ^ 3)
        Dim gg = P / A_z - 6 * Mx / (L1 * L2 ^ 2) - 12 * My * (b / 2 + d) / (L2 * L1 ^ 3)

        Dim valores_t3() As Double = {g3, g4, gk, gg}
        Dim g_prom_t3 As Double = valores_t3.Average()
        Dim Vu_3 As Double = g_prom_t3 * L2 * (L1 / 2 - b / 2 - d) * FD

        '___ Tensiones en 4  {4-1-c-j}
        Dim gc = P / A_z + 12 * Mx * (h / 2 + d) / (L1 * L2 ^ 3) + 6 * My / (L2 * L1 ^ 2)
        Dim gj = P / A_z + 12 * Mx * (h / 2 + d) / (L1 * L2 ^ 3) - 6 * My / (L2 * L1 ^ 2)

        Dim valores_t4() As Double = {g4, g1, gc, gj}
        Dim g_prom_t4 As Double = valores_t4.Average()
        Dim Vu_4 As Double = g_prom_t4 * L1 * (L2 / 2 - h / 2 - d) * FD

        Dim vc_1 As Double = 0.75 * 0.17 * Math.Sqrt(z.fc) * L1 * d * 1000
        Dim vc_2 As Double = 0.75 * 0.17 * Math.Sqrt(z.fc) * L2 * d * 1000

        Dim cumple_1 = (0.9 * Vu_1 <= vc_2)
        Dim cumple_2 = (0.9 * Vu_2 <= vc_1)
        Dim cumple_3 = (0.9 * Vu_3 <= vc_2)
        Dim cumple_4 = (0.9 * Vu_4 <= vc_1)

        Return (cumple_1, cumple_2, cumple_3, cumple_4,
            Vu_1, Vu_2, Vu_3, Vu_4,
            vc_1, vc_2,
            gf, ga,
            gi, ge,
            gk, gg,
            gc, gj)

    End Function

    Public Function VerificarFlexion(z As cZapata,
                                     P As Double,
                                     Mx As Double,
                                     My As Double,
                                      Optional FD As Double = 1.5) As (
                                      Mu_1 As Double,
                                      Mu_2 As Double,
                                      Rho_1 As Double,
                                      Rho_2 As Double,
                                      gf As Double,
                                      ga As Double,
                                      gi As Double,
                                      ge As Double,
                                      gk As Double,
                                      gg As Double,
                                      gc As Double,
                                      gj As Double,
                                      Cumple_L1 As Boolean,
                                      Cumple_L2 As Boolean)

        Dim L1 = z.L_b
        Dim L2 = z.L_h
        Dim e = z.e
        Dim d = z.d
        Dim b = z.b
        Dim h = z.h
        Dim hd = h - 0.075

        Dim A_z = L1 * L2

        Dim Lo_1 As Double = (L1 - b) / 2
        Dim Lo_2 As Double = (L2 - h) / 2

        '___ Tensiones en 1  {1-2-f-a}
        Dim g1 = P / A_z + 6 * Mx / (L1 * L2 ^ 2) + 6 * My / (L2 * L1 ^ 2)
        Dim g2 = P / A_z - 6 * Mx / (L1 * L2 ^ 2) + 6 * My / (L2 * L1 ^ 2)
        Dim gf = P / A_z - 6 * Mx / (L1 * L2 ^ 2) + 12 * My * (b / 2) / (L2 * L1 ^ 3)
        Dim ga = P / A_z + 6 * Mx / (L1 * L2 ^ 2) + 12 * My * (b / 2) / (L2 * L1 ^ 3)

        Dim valores_t1() As Double = {g1, g2, gf, ga}
        Dim g_prom_t1 As Double = valores_t1.Average()

        Dim R As Double = g_prom_t1 * Lo_1 * L2
        Dim x_1 As Double = Lo_1 * (ga + (2 * g1)) / (3 * (ga + g1))
        Dim x_2 As Double = Lo_1 * (gf + (2 * g2)) / (3 * (gf + g2))
        Dim x As Double = {x_1, x_2}.Average()
        Dim Mu_1 As Double = R * x


        '___ Tensiones en 2  {2-3-i-e}
        Dim g3 = P / A_z - 6 * Mx / (L1 * L2 ^ 2) - 6 * My / (L2 * L1 ^ 2)
        Dim gi = P / A_z - 12 * Mx * (h / 2) / (L1 * L2 ^ 3) - 6 * My / (L2 * L1 ^ 2)
        Dim ge = P / A_z - 12 * Mx * (h / 2) / (L1 * L2 ^ 3) + 6 * My / (L2 * L1 ^ 2)

        Dim valores_t2() As Double = {g2, g3, gi, ge}
        Dim g_prom_t2 As Double = valores_t2.Average()

        R = g_prom_t2 * Lo_2 * L1
        x_1 = Lo_2 * (ge + (2 * g2)) / (3 * (ge + g2))
        x_2 = Lo_2 * (gi + (2 * g3)) / (3 * (gi + g3))
        x = {x_1, x_2}.Average()
        Dim Mu_2 As Double = R * x

        '___ Tensiones en 3  {3-4-k-g}
        Dim g4 = P / A_z + 6 * Mx / (L1 * L2 ^ 2) - 6 * My / (L2 * L1 ^ 2)
        Dim gk = P / A_z + 6 * Mx / (L1 * L2 ^ 2) - 12 * My * (b / 2) / (L2 * L1 ^ 3)
        Dim gg = P / A_z - 6 * Mx / (L1 * L2 ^ 2) - 12 * My * (b / 2) / (L2 * L1 ^ 3)

        Dim valores_t3() As Double = {g3, g4, gk, gg}
        Dim g_prom_t3 As Double = valores_t3.Average()

        R = g_prom_t3 * Lo_1 * L2
        x_1 = Lo_1 * (gg + (2 * g3)) / (3 * (gg + g3))
        x_2 = Lo_1 * (gk + (2 * g4)) / (3 * (gk + g4))
        x = {x_1, x_2}.Average()
        Dim Mu_3 As Double = R * x

        '___ Tensiones en 4  {4-1-c-j}
        Dim gc = P / A_z + 12 * Mx * (h / 2) / (L1 * L2 ^ 3) + 6 * My / (L2 * L1 ^ 2)
        Dim gj = P / A_z + 12 * Mx * (h / 2) / (L1 * L2 ^ 3) - 6 * My / (L2 * L1 ^ 2)

        Dim valores_t4() As Double = {g4, g1, gc, gj}
        Dim g_prom_t4 As Double = valores_t4.Average()

        R = g_prom_t4 * Lo_2 * L1
        x_1 = Lo_2 * (gj + (2 * g4)) / (3 * (gj + g4))
        x_2 = Lo_2 * (gc + (2 * g1)) / (3 * (gc + g1))
        x = {x_1, x_2}.Average()
        Dim Mu_4 As Double = R * x


        Dim Mu_L1 As Double = Math.Max(Mu_1, Mu_3) * FD
        Dim Rho_1 As Double = 0.85 * z.fc / z.fy * (1 - Math.Sqrt(1 - (2 * Mu_L1 / (0.9 * 0.85 * z.fc * 1000 * L2 * hd ^ 2))))

        Dim Mu_L2 As Double = Math.Max(Mu_2, Mu_4) * FD
        Dim Rho_2 As Double = 0.85 * z.fc / z.fy * (1 - Math.Sqrt(1 - (2 * Mu_L2 / (0.9 * 0.85 * z.fc * 1000 * L1 * hd ^ 2))))


        Dim cumple_L1 = (0.9 * Rho_1 <= z.Rho_L1)
        Dim cumple_L2 = (0.9 * Rho_2 <= z.Rho_L2)


        Return (Mu_L1, Mu_L2,
            Rho_1, Rho_2,
            gf, ga,
            gi, ge,
            gk, gg,
            gc, gj,
            cumple_L1, cumple_L2)

    End Function





End Module
