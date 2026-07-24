Imports ARCO.Funciones_00_Varias

Public Module Funciones_10_AnalisisSeccion

    ' ─────────────────────────────────────────────────────────────────────
    '  TIPOS DE DATOS
    ' ─────────────────────────────────────────────────────────────────────

    Public Enum TipoSeccion10
        Rectangular = 0
        Circular = 1
    End Enum

    Public Class DatosDI10
        Public PhiMn As New List(Of Single)
        Public PhiPn As New List(Of Single)
        Public Mn As New List(Of Single)
        Public Pn As New List(Of Single)
        Public P0 As Single
        Public PhiP0 As Single
        Public Pmin As Single
        Public PhiPmin As Single
        Public P_bal As Single
        Public M_bal As Single
        Public PhiP_bal As Single
        Public PhiM_bal As Single
        Public Ast_cm2 As Single
        Public rho_pct As Single
        Public Ag_cm2 As Single
        Public ThetaGrados As Single                ' ángulo de análisis usado (informativo)
    End Class

    Public Class DatosMK10
        Public Curvaturas As New List(Of Single)   ' 1/m
        Public Momentos As New List(Of Single)     ' kN·m
        Public Ky As Single                        ' curvatura fluencia
        Public My As Single                        ' momento fluencia kN·m
        Public Ku As Single                        ' curvatura última
        Public Mu As Single                        ' momento último kN·m
        Public Ductilidad As Single                ' μφ = κu/κy
        Public ConvergioPorAplastamiento As Boolean = True  ' False = se agotó el barrido sin detectar falla real
        Public ThetaGrados As Single                ' ángulo de análisis usado (informativo)
        Public CausaFalla As String = ""            ' "Aplastamiento de concreto" / "Rotura de acero"
    End Class

    ' ─────────────────────────────────────────────────────────────────────
    '  ESPECIFICACIÓN DE REFUERZO POR CARAS (rectangular)
    ' ─────────────────────────────────────────────────────────────────────

    Public Class EspecRefuerzo
        Public BarraEsquinas As String = "#5"
        Public BarraSuperior As String = "#5"
        Public NSuperior As Integer = 2
        Public BarraInferior As String = "#5"
        Public NInferior As Integer = 2
        Public BarraLateral As String = "#5"
        Public NLateralPorCara As Integer = 1
    End Class

    ' ─────────────────────────────────────────────────────────────────────
    '  DISTRIBUCIÓN POR CARAS (4 esquinas + intermedias por cara)
    ' ─────────────────────────────────────────────────────────────────────

    Public Function DistribuirBarrasPorLados(B As Single, H As Single,
                                              esp As EspecRefuerzo,
                                              rec As Single) As List(Of RefuerzoSimple)
        Dim lista As New List(Of RefuerzoSimple)
        Dim idx As Integer = 0

        ' Distancias al eje de cada tipo de barra desde el centroide de la sección
        Dim db_esq = DiametroRefuerzo(esp.BarraEsquinas)
        Dim db_sup = DiametroRefuerzo(esp.BarraSuperior)
        Dim db_inf = DiametroRefuerzo(esp.BarraInferior)
        Dim db_lat = DiametroRefuerzo(esp.BarraLateral)

        ' Nota: DiametroRefuerzo devuelve el diámetro en METROS (no mm), por
        ' eso el offset de borde es db/2, no db/2000.
        Dim Xci_esq = Math.Max(0.01F, B / 2 - rec - db_esq / 2.0F)
        Dim Yci_esq = Math.Max(0.01F, H / 2 - rec - db_esq / 2.0F)
        Dim Xci_sup = Math.Max(0.01F, B / 2 - rec - db_sup / 2.0F)
        Dim Yci_sup = Math.Max(0.01F, H / 2 - rec - db_sup / 2.0F)
        Dim Xci_inf = Math.Max(0.01F, B / 2 - rec - db_inf / 2.0F)
        Dim Yci_inf = Math.Max(0.01F, H / 2 - rec - db_inf / 2.0F)
        Dim Xci_lat = Math.Max(0.01F, B / 2 - rec - db_lat / 2.0F)
        Dim Yci_lat = Math.Max(0.01F, H / 2 - rec - db_lat / 2.0F)

        Dim As_esq = AreaRefuerzo(esp.BarraEsquinas)
        Dim As_sup = AreaRefuerzo(esp.BarraSuperior)
        Dim As_inf = AreaRefuerzo(esp.BarraInferior)
        Dim As_lat = AreaRefuerzo(esp.BarraLateral)

        ' 4 esquinas (siempre)
        For Each pt As PointF In {New PointF(-Xci_esq, Yci_esq), New PointF(Xci_esq, Yci_esq),
                                    New PointF(Xci_esq, -Yci_esq), New PointF(-Xci_esq, -Yci_esq)}
            lista.Add(New RefuerzoSimple(idx, esp.BarraEsquinas, db_esq, As_esq, pt.X, pt.Y))
            idx += 1
        Next

        ' Intermedias cara superior
        For k = 1 To esp.NSuperior
            Dim xPos = CSng(-Xci_esq + k * 2 * Xci_esq / (esp.NSuperior + 1))
            lista.Add(New RefuerzoSimple(idx, esp.BarraSuperior, db_sup, As_sup, xPos, Yci_sup))
            idx += 1
        Next

        ' Intermedias cara inferior
        For k = 1 To esp.NInferior
            Dim xPos = CSng(-Xci_esq + k * 2 * Xci_esq / (esp.NInferior + 1))
            lista.Add(New RefuerzoSimple(idx, esp.BarraInferior, db_inf, As_inf, xPos, -Yci_inf))
            idx += 1
        Next

        ' Intermedias caras laterales (izquierda + derecha simétricas)
        For k = 1 To esp.NLateralPorCara
            Dim yPos = CSng(-Yci_esq + k * 2 * Yci_esq / (esp.NLateralPorCara + 1))
            lista.Add(New RefuerzoSimple(idx, esp.BarraLateral, db_lat, As_lat, -Xci_lat, yPos))
            idx += 1
            lista.Add(New RefuerzoSimple(idx, esp.BarraLateral, db_lat, As_lat, Xci_lat, yPos))
            idx += 1
        Next

        Return lista
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  DISTRIBUCIÓN RECTANGULAR DE BARRAS (con recubrimiento real)
    ' ─────────────────────────────────────────────────────────────────────

    Public Function DistribuirBarrasRectangular10(B As Single, H As Single, n As Integer,
                                                   barraTag As String,
                                                   recubrimiento As Single) As List(Of RefuerzoSimple)
        Dim lista As New List(Of RefuerzoSimple)
        If n < 4 Then n = 4
        Dim db_mm As Single = DiametroRefuerzo(barraTag)
        Dim asb As Single = AreaRefuerzo(barraTag)
        ' distancia del centroide de la sección al eje de la barra
        Dim Bci As Single = B / 2.0F - recubrimiento - db_mm / 2000.0F
        Dim Hci As Single = H / 2.0F - recubrimiento - db_mm / 2000.0F
        Bci = Math.Max(0.01F, Bci) : Hci = Math.Max(0.01F, Hci)

        Dim Lx As Single = 2.0F * Bci
        Dim Ly As Single = 2.0F * Hci
        Dim nRem As Integer = n - 4
        Dim nL As Integer = 0, nC As Integer = 0
        If nRem > 0 Then
            Dim nPares = nRem \ 2
            nL = CInt(Math.Round(CDbl(nPares) * Ly / (Lx + Ly)))
            nC = nPares - nL
        End If

        Dim idx As Integer = 0

        ' 4 esquinas
        lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb, -Bci,  Hci)) : idx += 1
        lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb,  Bci,  Hci)) : idx += 1
        lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb,  Bci, -Hci)) : idx += 1
        lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb, -Bci, -Hci)) : idx += 1

        ' Intermedias cara superior e inferior
        For k = 1 To nC
            Dim xPos = CSng(-Bci + k * 2.0F * Bci / (nC + 1))
            lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb, xPos,  Hci)) : idx += 1
            lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb, xPos, -Hci)) : idx += 1
        Next

        ' Intermedias cara izquierda y derecha
        For k = 1 To nL
            Dim yPos = CSng(-Hci + k * 2.0F * Hci / (nL + 1))
            lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb, -Bci, yPos)) : idx += 1
            lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb,  Bci, yPos)) : idx += 1
        Next

        ' Barras extras si n es impar
        Do While lista.Count < n
            lista.Add(New RefuerzoSimple(idx, barraTag, db_mm, asb, 0, Hci)) : idx += 1
        Loop

        Return lista
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  DISTRIBUCIÓN CIRCULAR DE BARRAS
    ' ─────────────────────────────────────────────────────────────────────

    Public Function DistribuirBarrasCirculo10(D As Single, n As Integer,
                                               barraTag As String,
                                               recubrimiento As Single) As List(Of RefuerzoSimple)
        Dim lista As New List(Of RefuerzoSimple)
        If n < 3 OrElse D <= 0 Then Return lista
        Dim db_m As Single = DiametroRefuerzo(barraTag)   ' ya en metros
        Dim asb As Single = AreaRefuerzo(barraTag)
        Dim R_ring As Single = D / 2.0F - recubrimiento - db_m / 2.0F
        R_ring = Math.Max(0.02F, R_ring)
        For i = 0 To n - 1
            Dim theta As Single = CSng(2.0 * Math.PI * i / n)
            lista.Add(New RefuerzoSimple(i, barraTag, db_m, asb,
                                         CSng(R_ring * Math.Sin(theta)),
                                         CSng(R_ring * Math.Cos(theta))))
        Next
        Return lista
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  MANDER (1988): fcc, ε_cc, ε_cu confinado para sección rectangular
    ' ─────────────────────────────────────────────────────────────────────

    Public Sub CalcularMander10(fc As Single, fyh As Single,
                                 db_hoop_m As Single, s_mm As Single,
                                 b_core_m As Single, h_core_m As Single,
                                 n_ramas_b As Integer, n_ramas_h As Integer,
                                 ByRef fcc As Single, ByRef eps_cc As Single,
                                 ByRef eps_cu_conf As Single)
        ' Valores por defecto (sin confinamiento efectivo)
        fcc = fc : eps_cc = 0.002F : eps_cu_conf = 0.003F
        If s_mm <= 0 OrElse db_hoop_m <= 0 OrElse b_core_m <= 0 OrElse h_core_m <= 0 Then Return

        ' db_hoop_m ya viene en METROS (DiametroRefuerzo lo devuelve así, no en
        ' mm) — dividir de nuevo por 1000 aquí hacía que Ash quedara ~10⁶ veces
        ' más chica de lo real, y por tanto fl≈0 siempre → CalcularMander10
        ' tomaba SIEMPRE la rama "sin confinamiento", sin importar ramas/separación.
        Dim s_m As Single = s_mm / 1000.0F
        Dim Ash As Single = CSng(Math.PI / 4.0 * db_hoop_m ^ 2)  ' m²

        ' Relación de refuerzo transversal por cada dirección
        Dim rho_x As Single = If(h_core_m > 0, n_ramas_b * Ash / (h_core_m * s_m), 0)
        Dim rho_y As Single = If(b_core_m > 0, n_ramas_h * Ash / (b_core_m * s_m), 0)

        ' Presión de confinamiento efectiva (Mander, simplificado ke=0.75)
        Dim fl As Single = 0.75F * (rho_x + rho_y) / 2.0F * fyh  ' MPa

        If fl <= 0 Then Return

        Dim ratio As Single = fl / fc
        Dim innerSqrt As Double = 1.0 + 7.94 * ratio
        fcc = fc * CSng(-1.254 + 2.254 * Math.Sqrt(innerSqrt) - 2.0 * ratio)
        fcc = Math.Max(fcc, fc)

        eps_cc = 0.002F * (1.0F + 5.0F * (fcc / fc - 1.0F))
        eps_cu_conf = Math.Max(0.003F, 0.004F + 1.4F * (rho_x + rho_y) * fyh * 0.12F / fcc)
    End Sub

    ' ─────────────────────────────────────────────────────────────────────
    '  CONSTITUTIVOS DE CONCRETO
    ' ─────────────────────────────────────────────────────────────────────

    ' Modelo de Mander para concreto confinado (Popovics / Mander 1988)
    Private Function SigConf(eps As Single, fcc As Single, eps_cc As Single) As Single
        If eps <= 0 Then Return 0
        Dim Ec As Single = 4700.0F * CSng(Math.Sqrt(fcc))    ' MPa
        Dim E_sec As Single = If(eps_cc > 0, fcc / eps_cc, 1.0F)
        Dim r As Single = If(Ec > E_sec, Ec / (Ec - E_sec), 50.0F)
        r = Math.Max(1.001F, r)
        Dim x As Single = eps / eps_cc
        Dim denom As Single = CSng(r - 1.0 + Math.Pow(x, r))
        If denom < 0.0001F Then Return 0
        Return Math.Max(0, fcc * x * r / denom)
    End Function

    ' Curva parábola-descenso lineal para concreto no confinado
    Private Function SigNoCon(eps As Single, fc As Single,
                               eps_c0 As Single, eps_cu As Single) As Single
        If eps <= 0 Then Return 0
        If eps <= eps_c0 Then Return fc * (2.0F * eps / eps_c0 - (eps / eps_c0) ^ 2)
        If eps <= eps_cu Then Return fc * (1.0F - (eps - eps_c0) / (eps_cu - eps_c0))
        Return 0
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  SEGMENTO CIRCULAR: área y centroide (zona superior desde y=y0 hasta y=+R)
    ' ─────────────────────────────────────────────────────────────────────

    Private Sub SegCircular(R As Single, y0 As Single,
                             ByRef Ac As Single, ByRef Yc As Single)
        y0 = Math.Max(-R, Math.Min(R, y0))
        Dim cosT As Double = y0 / R
        cosT = Math.Max(-1.0, Math.Min(1.0, cosT))
        Dim theta As Double = Math.Acos(cosT)
        Dim sinT As Double = Math.Sin(theta)
        Ac = CSng(R ^ 2 * (theta - sinT * Math.Cos(theta)))
        Dim denom As Double = 2.0 * theta - Math.Sin(2.0 * theta)
        Yc = If(Math.Abs(denom) < 1.0E-12, 0, CSng(4.0 * R * sinT ^ 3 / (3.0 * denom)))
    End Sub

    ' ─────────────────────────────────────────────────────────────────────
    '  DIAGRAMA DE INTERACCIÓN — RECTANGULAR (envuelve motor existente)
    ' ─────────────────────────────────────────────────────────────────────

    Public Function DI_Rectangular10(B As Single, H As Single,
                                      barras As List(Of RefuerzoSimple),
                                      fc As Single, fy As Single,
                                      Es As Single,
                                      Optional rec As Single = 0.04F,
                                      Optional thetaRad As Single = 0.0F) As DatosDI10
        Dim res As New DatosDI10
        res.ThetaGrados = CSng(thetaRad * 180.0 / Math.PI)
        If barras Is Nothing OrElse barras.Count = 0 Then Return res

        Dim Ag As Single = B * H
        Dim Ast_m2 As Single = barras.Sum(Function(rr) rr.Asb) / 1_000_000.0F
        res.Ag_cm2 = Ag * 10000.0F
        res.Ast_cm2 = Ast_m2 * 10000.0F
        res.rho_pct = If(Ag > 0, Ast_m2 / Ag * 100.0F, 0)

        Dim ey As Single = fy / Es
        Dim beta1 As Single = CSng(Math.Max(0.65, Math.Min(0.85, 0.85 - 0.05 * (fc - 28.0) / 7.0)))

        If Math.Abs(thetaRad) < 0.0001F Then
            ' ── Ruta actual: motor compartido con columnas (Ángulo=0), intacta ──
            Dim raw = Funcion_Diagrama_Interaccion(B, H, barras, fc, fy, Es, 0)
            res.PhiMn = raw.Item1 : res.PhiPn = raw.Item2
            res.Mn = raw.Item3 : res.Pn = raw.Item4
        Else
            ' ── Nueva ruta: bloque de Whitney rotado sobre grilla 2D. Estribos
            '    cerrados (tied) → φ=0.65·0.8·P0, misma convención que el motor
            '    compartido usa para rectangulares. El DI ignora confinamiento
            '    a propósito (convención NSR-10/ACI: no se acredita en el
            '    diagrama nominal, solo en el M-κ) ─────────────────────────────
            Dim P0 As Single = (0.85F * fc * (Ag - Ast_m2) + fy * Ast_m2) * 1000.0F
            Dim Pmax_phi As Single = 0.65F * 0.8F * P0
            Dim Pmin As Single = -fy * Ast_m2 * 1000.0F
            Dim Pmin_phi As Single = 0.9F * Pmin

            Dim grilla = ConstruirGrillaRectangular10(B, H, rec)
            Dim projMax = ProyeccionMaxima10(TipoSeccion10.Rectangular, B, H, thetaRad)
            Dim sinT = CSng(Math.Sin(thetaRad)) : Dim cosT = CSng(Math.Cos(thetaRad))
            Dim p_barra_min = barras.Min(Function(bb) bb.Coordenada_X * sinT + bb.Coordenada_Y * cosT)
            Dim d_util = projMax - p_barra_min

            res.Pn.Add(P0) : res.PhiPn.Add(Pmax_phi)
            res.Mn.Add(0) : res.PhiMn.Add(0)

            Const NP As Integer = 50
            Dim c_ini = 2.0F * projMax, c_fin = 0.04F
            Dim dPaso = (c_ini - c_fin) / NP

            For k = 0 To NP
                Dim c = c_ini - k * dPaso
                Dim a = CSng(Math.Min(beta1 * c, 2.0F * projMax))
                Dim pLimite = projMax - a   ' celdas con proyección > pLimite están en el bloque

                Dim Cc As Single = 0, Mcc As Single = 0
                For Each celda In grilla
                    Dim p = celda.X * sinT + celda.Y * cosT
                    If p > pLimite Then
                        Dim Fi = 0.85F * fc * celda.Area * 1000.0F
                        Cc += Fi : Mcc += Fi * p
                    End If
                Next

                Dim Fst As Single = 0, Mst As Single = 0
                For Each bar As RefuerzoSimple In barras
                    Dim p_bar = bar.Coordenada_X * sinT + bar.Coordenada_Y * cosT
                    Dim di = projMax - p_bar
                    Dim esi = If(c > 0.001F, (c - di) * 0.003F / c, 0)
                    Dim fsi = Math.Max(-fy, Math.Min(fy, Es * esi))
                    Dim Fs = If(di < c, (fsi - 0.85F * fc), fsi) * bar.Asb / 1000.0F
                    Fst += Fs : Mst += Fs * p_bar
                Next

                Dim Pn = Cc + Fst
                Dim Mn = CSng(Math.Abs(Mcc + Mst))
                Dim es_t = If(c > 0.001F, (d_util - c) * 0.003F / c, 0.01F)
                Dim phi = If(Math.Abs(es_t) <= ey, 0.65F,
                             If(Math.Abs(es_t) <= 2.5F * ey,
                                0.65F + (Math.Abs(es_t) - ey) / (1.5F * ey) * 0.25F, 0.9F))
                Dim phi_Pn = CSng(Math.Min(phi * Pn, Pmax_phi))
                Dim phi_Mn = phi * Mn

                res.Pn.Add(Pn) : res.PhiPn.Add(phi_Pn)
                res.Mn.Add(Mn) : res.PhiMn.Add(phi_Mn)
            Next

            res.Pn.Add(Pmin) : res.PhiPn.Add(Pmin_phi)
            res.Mn.Add(0) : res.PhiMn.Add(0)
        End If

        res.P0 = If(res.Pn.Count > 0, res.Pn(0), 0)
        res.PhiP0 = If(res.PhiPn.Count > 0, res.PhiPn(0), 0)
        res.Pmin = If(res.Pn.Count > 0, res.Pn.Last(), 0)
        res.PhiPmin = If(res.PhiPn.Count > 0, res.PhiPn.Last(), 0)

        ' Punto balance: c_bal = 0.003/(0.003+ey)*(profundidad total en la
        ' dirección de análisis) — 2·projMax generaliza H (idéntico en θ=0)
        Dim profTotal = 2.0F * ProyeccionMaxima10(TipoSeccion10.Rectangular, B, H, thetaRad)
        Dim c_bal As Single = 0.003F / (0.003F + ey) * profTotal
        CalcBalanceRect(B, H, barras, fc, fy, Es, beta1, c_bal, ey,
                        res.P_bal, res.M_bal, res.PhiP_bal, res.PhiM_bal, thetaRad)
        Return res
    End Function

    Private Sub CalcBalanceRect(B As Single, H As Single,
                                 barras As List(Of RefuerzoSimple),
                                 fc As Single, fy As Single, Es As Single,
                                 beta1 As Single, c_bal As Single, ey As Single,
                                 ByRef Pb As Single, ByRef Mb As Single,
                                 ByRef PhiPb As Single, ByRef PhiMb As Single,
                                 Optional thetaRad As Single = 0.0F)
        Dim sinT = CSng(Math.Sin(thetaRad)) : Dim cosT = CSng(Math.Cos(thetaRad))
        Dim projMax = ProyeccionMaxima10(TipoSeccion10.Rectangular, B, H, thetaRad)
        Dim a_bal = Math.Min(beta1 * c_bal, 2.0F * projMax)

        Dim Cc As Single, Mcc As Single
        If Math.Abs(thetaRad) < 0.0001F Then
            Cc = 0.85F * fc * B * a_bal * 1000.0F
            Mcc = Cc * (projMax - a_bal / 2.0F)
        Else
            Dim grilla = ConstruirGrillaRectangular10(B, H, 0.0F)
            Dim pLimite = projMax - a_bal
            Cc = 0 : Mcc = 0
            For Each celda In grilla
                Dim p = celda.X * sinT + celda.Y * cosT
                If p > pLimite Then
                    Dim Fi = 0.85F * fc * celda.Area * 1000.0F
                    Cc += Fi : Mcc += Fi * p
                End If
            Next
        End If

        Dim Fst As Single = 0, Mst As Single = 0
        Dim p_barra_min As Single = Single.MaxValue
        For Each bar As RefuerzoSimple In barras
            Dim p_bar = bar.Coordenada_X * sinT + bar.Coordenada_Y * cosT
            If p_bar < p_barra_min Then p_barra_min = p_bar
            Dim di = projMax - p_bar
            Dim esi = If(c_bal > 0, (c_bal - di) * 0.003F / c_bal, 0)
            Dim fsi = Math.Max(-fy, Math.Min(fy, Es * esi))
            Dim Fs = If(di < c_bal, (fsi - 0.85F * fc), fsi) * bar.Asb / 1000.0F
            Fst += Fs : Mst += Fs * p_bar
        Next

        Pb = Cc + Fst
        Mb = Math.Abs(Mcc + Mst)
        Dim d_util = projMax - p_barra_min
        Dim es_t = If(c_bal > 0, (d_util - c_bal) * 0.003F / c_bal, 0)
        Dim phi = If(Math.Abs(es_t) <= ey, 0.65F,
                     If(Math.Abs(es_t) <= 2.5F * ey,
                        0.65F + (Math.Abs(es_t) - ey) / (1.5F * ey) * 0.25F, 0.9F))
        PhiPb = phi * Pb : PhiMb = phi * Mb
    End Sub

    ' ─────────────────────────────────────────────────────────────────────
    '  DIAGRAMA DE INTERACCIÓN — CIRCULAR
    ' ─────────────────────────────────────────────────────────────────────

    Public Function DI_Circular10(D As Single,
                                   barras As List(Of RefuerzoSimple),
                                   fc As Single, fy As Single,
                                   Es As Single,
                                   Optional thetaRad As Single = 0.0F) As DatosDI10
        Dim res As New DatosDI10
        res.ThetaGrados = CSng(thetaRad * 180.0 / Math.PI)
        If barras Is Nothing OrElse barras.Count = 0 OrElse D <= 0 Then Return res

        Dim sinT = CSng(Math.Sin(thetaRad)) : Dim cosT = CSng(Math.Cos(thetaRad))
        Dim R = D / 2.0F
        Dim beta1 = CSng(Math.Max(0.65, Math.Min(0.85, 0.85 - 0.05 * (fc - 28.0) / 7.0)))
        Dim Ag = CSng(Math.PI * R ^ 2)
        Dim Ast_m2 = barras.Sum(Function(rr) rr.Asb) / 1_000_000.0F
        res.Ag_cm2 = Ag * 10000.0F
        res.Ast_cm2 = Ast_m2 * 10000.0F
        res.rho_pct = If(Ag > 0, Ast_m2 / Ag * 100.0F, 0)

        Dim ey = fy / Es
        Dim P0 = (0.85F * fc * (Ag - Ast_m2) + fy * Ast_m2) * 1000.0F
        ' φ=0.65 para espiral en NSR-10; límite 0.85·P₀
        Dim Pmax_phi = 0.65F * 0.85F * P0
        Dim Pmin = -fy * Ast_m2 * 1000.0F
        Dim Pmin_phi = 0.9F * Pmin

        res.P0 = P0 : res.PhiP0 = Pmax_phi
        res.Pmin = Pmin : res.PhiPmin = Pmin_phi

        ' Punto inicial: compresión pura
        res.Pn.Add(P0) : res.PhiPn.Add(Pmax_phi)
        res.Mn.Add(0) : res.PhiMn.Add(0)

        Dim Y_top = R
        Dim p_barra_min = barras.Min(Function(b) b.Coordenada_X * sinT + b.Coordenada_Y * cosT)
        Dim d_util = Y_top - p_barra_min

        ' Barrido de eje neutro de c=D hasta c~0
        Const NP As Integer = 60
        Dim c_ini = D, c_fin = 0.02F
        Dim dPaso = (c_ini - c_fin) / NP
        Dim balSet As Boolean = False
        Dim es_t_prev As Single = 0

        For k = 0 To NP
            Dim c = c_ini - k * dPaso
            Dim a = CSng(Math.Min(beta1 * c, D))
            Dim y0 = R - a
            Dim Ac As Single, Yc As Single
            SegCircular(R, y0, Ac, Yc)
            Dim Cc = 0.85F * fc * Ac * 1000.0F

            Dim Fst As Single = 0, Mst As Single = 0
            For Each bar As RefuerzoSimple In barras
                Dim p_bar = bar.Coordenada_X * sinT + bar.Coordenada_Y * cosT
                Dim di = Y_top - p_bar
                Dim esi = If(c > 0.001F, (c - di) * 0.003F / c, 0)
                Dim fsi = Math.Max(-fy, Math.Min(fy, Es * esi))
                Dim Fs = If(di < c, (fsi - 0.85F * fc), fsi) * bar.Asb / 1000.0F
                Fst += Fs : Mst += Fs * p_bar
            Next

            Dim Pn = Cc + Fst
            Dim Mn = CSng(Math.Abs(Cc * Yc + Mst))
            Dim es_t = If(c > 0.001F AndAlso d_util > 0, (d_util - c) * 0.003F / c, 0.01F)
            Dim phi = If(Math.Abs(es_t) <= ey, 0.65F,
                         If(Math.Abs(es_t) <= 2.5F * ey,
                            0.65F + (Math.Abs(es_t) - ey) / (1.5F * ey) * 0.25F, 0.9F))
            Dim phi_Pn = CSng(Math.Min(phi * Pn, Pmax_phi))
            Dim phi_Mn = phi * Mn

            ' Detectar punto de balance (transición compresión → tracción en barra extrema)
            If Not balSet AndAlso k > 0 Then
                If (es_t_prev <= ey AndAlso es_t > ey) OrElse
                   (es_t_prev >= -ey AndAlso es_t < -ey) Then
                    res.P_bal = Pn : res.M_bal = Mn
                    res.PhiP_bal = phi_Pn : res.PhiM_bal = phi_Mn
                    balSet = True
                End If
            End If
            es_t_prev = es_t

            res.Pn.Add(Pn) : res.PhiPn.Add(phi_Pn)
            res.Mn.Add(Mn) : res.PhiMn.Add(phi_Mn)
        Next

        ' Punto final: tracción pura
        res.Pn.Add(Pmin) : res.PhiPn.Add(Pmin_phi)
        res.Mn.Add(0) : res.PhiMn.Add(0)
        Return res
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  ÁNGULO DE ANÁLISIS: proyección generalizada + grilla 2D (rectangular)
    ' ─────────────────────────────────────────────────────────────────────

    ' Proyección de la fibra extrema de compresión sobre la dirección de
    ' flexión asumida θ. En θ=0 reproduce Y_top=H/2 (idéntico a hoy). En
    ' círculo es invariante = R por simetría rotacional (no depende de θ).
    Public Function ProyeccionMaxima10(tipoSec As TipoSeccion10, B As Single, H As Single,
                                        thetaRad As Single) As Single
        If tipoSec = TipoSeccion10.Circular Then Return B / 2.0F
        Return (B / 2.0F) * CSng(Math.Abs(Math.Sin(thetaRad))) +
               (H / 2.0F) * CSng(Math.Abs(Math.Cos(thetaRad)))
    End Function

    Private Structure CeldaFibra10
        Public X As Single
        Public Y As Single
        Public Area As Single       ' m²
        Public EsConfinada As Boolean
    End Structure

    ' Grilla 2D de fibras de concreto para sección rectangular. Se usa solo
    ' cuando el ángulo de análisis impide reducir la integración a franjas 1D:
    ' el núcleo confinado es fijo a la sección física (los estribos no rotan
    ' con el ángulo asumido), así que una franja de proyección constante en
    ' θ≠0 cruza núcleo y recubrimiento de forma no reducible a 1D.
    Private Function ConstruirGrillaRectangular10(B As Single, H As Single, rec As Single,
                                                   Optional nx As Integer = 50,
                                                   Optional ny As Integer = 50) As List(Of CeldaFibra10)
        Dim celdas As New List(Of CeldaFibra10)
        If B <= 0 OrElse H <= 0 Then Return celdas
        Dim dx = B / nx, dy = H / ny
        Dim areaCelda = dx * dy
        Dim xCore = B / 2.0F - rec, yCore = H / 2.0F - rec
        For i = 0 To nx - 1
            Dim xc = -B / 2.0F + (i + 0.5F) * dx
            For j = 0 To ny - 1
                Dim yc = -H / 2.0F + (j + 0.5F) * dy
                Dim confinada = Math.Abs(xc) <= xCore AndAlso Math.Abs(yc) <= yCore
                celdas.Add(New CeldaFibra10 With {.X = xc, .Y = yc, .Area = areaCelda, .EsConfinada = confinada})
            Next
        Next
        Return celdas
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  MOMENTO–CURVATURA POR FIBRAS (Mander: confinado + no confinado)
    ' ─────────────────────────────────────────────────────────────────────

    Public Function MomentoCurvatura10(B As Single, H As Single,
                                        tipoSec As TipoSeccion10,
                                        recubrimiento As Single,
                                        barras As List(Of RefuerzoSimple),
                                        fc As Single, fy As Single, Es As Single,
                                        fcc As Single, eps_cc As Single,
                                        eps_cu_conf As Single,
                                        P_axial As Single,
                                        Optional thetaRad As Single = 0.0F,
                                        Optional eps_su As Single = 0.09F) As DatosMK10
        Dim res As New DatosMK10
        res.ThetaGrados = CSng(thetaRad * 180.0 / Math.PI)
        If barras Is Nothing OrElse barras.Count = 0 Then Return res

        Dim ey = fy / Es
        Dim projMax = ProyeccionMaxima10(tipoSec, B, H, thetaRad)
        Dim grillaRect As List(Of CeldaFibra10) = Nothing
        If tipoSec = TipoSeccion10.Rectangular AndAlso Math.Abs(thetaRad) > 0.0001F Then
            grillaRect = ConstruirGrillaRectangular10(B, H, recubrimiento)
        End If

        ' Semilla de paso (misma heurística de siempre) — ya NO es un techo:
        ' solo fija la resolución inicial del barrido, no dónde termina.
        Dim c_min_est = Math.Max(recubrimiento + 0.02F, 0.03F)
        Dim kappaSeed = Math.Min(eps_cu_conf / c_min_est, eps_cu_conf / (0.03F * H) * 5)
        If kappaSeed < 0.0001F Then kappaSeed = 0.01F

        Const NK_TARGET As Integer = 120
        Const MAX_EXPAND_MULT As Single = 20.0F   ' techo de seguridad generoso
        Const MAX_ITER_ABS As Integer = 4000      ' guarda dura contra loops descontrolados
        Dim dkappa = kappaSeed / NK_TARGET
        Dim kappaAbsMax = kappaSeed * MAX_EXPAND_MULT

        Dim firstYield As Boolean = False
        Dim stopped As Boolean = False
        Dim kappa As Single = 0.0F
        Dim iterCount As Integer = 0

        ' Estado del paso anterior, para interpolar Ky/Ku linealmente en vez de
        ' reportar el punto discreto del barrido. Es necesario porque dkappa
        ' escala con el confinamiento (kappaSeed) — sin interpolar, secciones
        ' con más confinamiento usan un paso más grueso, lo que sobrestima Ky
        ' de forma desigual entre corridas y puede hacer que μφ = κu/κy
        ' PAREZCA bajar al aumentar el confinamiento, siendo puro ruido de
        ' discretización y no un efecto físico real.
        Dim kappa_prev As Single = 0.0F
        Dim ecc_max_prev As Single = 0.0F
        Dim ems_max_prev As Single = 0.0F
        Dim Mf_prev As Single = 0.0F

        Do
            kappa += dkappa : iterCount += 1

            ' Bisección para eje neutro (equilibrio de fuerzas P = P_axial)
            Dim c_lo = 0.001F, c_hi = projMax * 2.0F
            For iter = 0 To 60
                Dim c_try = (c_lo + c_hi) / 2.0F
                Dim Pc As Single, Mc As Single, ems As Single, ecc As Single
                EvalFibras(B, H, tipoSec, recubrimiento, barras,
                           fc, fy, Es, fcc, eps_cc, eps_cu_conf,
                           kappa, c_try, Pc, Mc, ems, ecc, thetaRad, grillaRect)
                If Pc > P_axial Then c_hi = c_try Else c_lo = c_try
                If Math.Abs(c_hi - c_lo) < 0.0001F Then Exit For
            Next

            Dim c_final = (c_lo + c_hi) / 2.0F
            Dim Pf As Single, Mf As Single, ems_max As Single, ecc_max As Single
            EvalFibras(B, H, tipoSec, recubrimiento, barras,
                       fc, fy, Es, fcc, eps_cc, eps_cu_conf,
                       kappa, c_final, Pf, Mf, ems_max, ecc_max, thetaRad, grillaRect)

            res.Curvaturas.Add(kappa)
            res.Momentos.Add(Mf)

            ' Detectar fluencia (primer punto donde alguna barra llega a ey),
            ' interpolando linealmente entre el paso anterior y este.
            If Not firstYield AndAlso ems_max >= ey Then
                Dim frac = If(ems_max > ems_max_prev, (ey - ems_max_prev) / (ems_max - ems_max_prev), 1.0F)
                frac = Math.Max(0.0F, Math.Min(1.0F, frac))
                res.Ky = kappa_prev + frac * (kappa - kappa_prev)
                res.My = Mf_prev + frac * (Mf - Mf_prev)
                firstYield = True
            End If

            ' Condición de falla real: la que ocurra primero entre aplastamiento
            ' del concreto (fibra comprimida supera eps_cu_conf) y rotura del
            ' acero a tracción (barra extrema supera su deformación última) —
            ' sin este segundo criterio, secciones muy confinadas "aguantan"
            ' curvaturas que en la realidad ya fracturaron la barra de tracción,
            ' inflando artificialmente κu y μφ. También interpolado linealmente.
            If ecc_max >= eps_cu_conf Then
                Dim frac = If(ecc_max > ecc_max_prev, (eps_cu_conf - ecc_max_prev) / (ecc_max - ecc_max_prev), 1.0F)
                frac = Math.Max(0.0F, Math.Min(1.0F, frac))
                res.Ku = kappa_prev + frac * (kappa - kappa_prev)
                res.Mu = Mf_prev + frac * (Mf - Mf_prev)
                res.CausaFalla = "Aplastamiento de concreto confinado"
                stopped = True
            ElseIf ems_max >= eps_su Then
                Dim frac = If(ems_max > ems_max_prev, (eps_su - ems_max_prev) / (ems_max - ems_max_prev), 1.0F)
                frac = Math.Max(0.0F, Math.Min(1.0F, frac))
                res.Ku = kappa_prev + frac * (kappa - kappa_prev)
                res.Mu = Mf_prev + frac * (Mf - Mf_prev)
                res.CausaFalla = "Rotura del acero a tracción"
                stopped = True
            End If

            kappa_prev = kappa : ecc_max_prev = ecc_max : ems_max_prev = ems_max : Mf_prev = Mf
        Loop While Not stopped AndAlso kappa < kappaAbsMax AndAlso iterCount < MAX_ITER_ABS

        If Not stopped Then
            ' No se alcanzó ninguna condición de falla dentro del techo de
            ' seguridad: Ku/Mu quedan como límite de barrido, no como punto
            ' físico de falla real.
            res.Ku = res.Curvaturas.Last()
            res.Mu = res.Momentos.Last()
            res.ConvergioPorAplastamiento = False
            res.CausaFalla = "No convergió (límite de barrido)"
            Logger.Warning("Funciones_10_AnalisisSeccion.MomentoCurvatura10",
                           "No se alcanzó ninguna condición de falla dentro del barrido de curvatura " &
                           "(κ hasta " & kappaAbsMax.ToString("F4") & "); Ku/Mu reportados " &
                           "son un límite de barrido, no el punto de falla real.")
        End If

        ' Fluencia por defecto si no se detectó
        If Not firstYield AndAlso res.Curvaturas.Count > 5 Then
            Dim i25 = CInt(res.Curvaturas.Count * 0.2)
            res.Ky = res.Curvaturas(i25) : res.My = res.Momentos(i25)
        End If
        res.Ductilidad = If(res.Ky > 0.00001F, res.Ku / res.Ky, 0)
        Return res
    End Function

    ' Evalúa fuerzas y momentos en la sección para (κ, c) dados, en la
    ' dirección de análisis θ (thetaRad=0 reproduce exactamente el
    ' comportamiento de siempre; sin/cos exactos de IEEE-754 en 0 rad).
    ' P [kN], M [kN·m] respecto al centroide geométrico.
    ' ems_max = deformación máxima en tracción del acero (positivo = tracción).
    ' ecc_max = deformación máxima en compresión del concreto.
    Private Sub EvalFibras(B As Single, H As Single,
                            tipoSec As TipoSeccion10, rec As Single,
                            barras As List(Of RefuerzoSimple),
                            fc As Single, fy As Single, Es As Single,
                            fcc As Single, eps_cc As Single, eps_cu_conf As Single,
                            kappa As Single, c As Single,
                            ByRef P As Single, ByRef M As Single,
                            ByRef ems_max As Single, ByRef ecc_max As Single,
                            Optional thetaRad As Single = 0.0F,
                            Optional grillaRect As List(Of CeldaFibra10) = Nothing)
        P = 0 : M = 0 : ems_max = 0 : ecc_max = 0
        Dim sinT As Single = CSng(Math.Sin(thetaRad))
        Dim cosT As Single = CSng(Math.Cos(thetaRad))
        Dim projMax As Single = ProyeccionMaxima10(tipoSec, B, H, thetaRad)
        Dim usarGrilla As Boolean = (tipoSec = TipoSeccion10.Rectangular) AndAlso
                                     (Math.Abs(thetaRad) > 0.0001F) AndAlso
                                     (grillaRect IsNot Nothing)

        If usarGrilla Then
            ' ── Concreto: grilla 2D (rectangular, θ≠0) ─────────────────────
            For Each celda As CeldaFibra10 In grillaRect
                Dim pProj = celda.X * sinT + celda.Y * cosT
                Dim d_i = projMax - pProj
                Dim eps_i = If(c > 0.0001F, kappa * (c - d_i), 0)
                If eps_i > 0 Then
                    Dim sig = If(celda.EsConfinada, SigConf(eps_i, fcc, eps_cc),
                                                     SigNoCon(eps_i, fc, 0.002F, 0.003F))
                    Dim Fi = sig * celda.Area * 1000.0F   ' kN
                    P += Fi : M += Fi * pProj
                    ecc_max = Math.Max(ecc_max, eps_i)
                End If
            Next
        Else
            ' ── Concreto: franjas 1D (circular en cualquier θ por simetría
            '    rotacional; rectangular solo cuando θ≈0 — bit-idéntico a hoy) ──
            Dim R_circ = B / 2.0F   ' radio para sección circular (B=D en ese caso)
            Const NSTRIP As Integer = 80
            Dim dy = H / NSTRIP

            For i = 0 To NSTRIP - 1
                Dim pProj = projMax - (i + 0.5F) * dy   ' desde centroide, + = lado de compresión
                Dim d_i = projMax - pProj                ' profundidad desde fibra comprimida
                Dim eps_i = If(c > 0.0001F, kappa * (c - d_i), 0)  ' + = compresión

                Dim b_full As Single, b_core As Single
                If tipoSec = TipoSeccion10.Circular Then
                    b_full = 2.0F * CSng(Math.Sqrt(Math.Max(0, R_circ ^ 2 - pProj ^ 2)))
                    Dim R_core = R_circ - rec
                    b_core = 2.0F * CSng(Math.Sqrt(Math.Max(0, R_core ^ 2 - pProj ^ 2)))
                Else
                    b_full = B
                    b_core = If(Math.Abs(pProj) <= projMax - rec, B - 2.0F * rec, 0)
                End If
                Dim b_cover = b_full - b_core

                Dim sig_c As Single = 0
                If eps_i > 0 Then
                    Dim sig_core = If(b_core > 0, SigConf(eps_i, fcc, eps_cc), 0)
                    Dim sig_cov = If(b_cover > 0, SigNoCon(eps_i, fc, 0.002F, 0.003F), 0)
                    sig_c = If(b_full > 0, (sig_core * b_core + sig_cov * b_cover) / b_full, 0)
                    ecc_max = Math.Max(ecc_max, eps_i)
                End If

                Dim Fstrip = sig_c * b_full * dy * 1000.0F    ' kN
                P += Fstrip
                M += Fstrip * pProj
            Next
        End If

        ' Contribución del acero (proyección generalizada; bit-idéntica a hoy
        ' en θ=0 porque sin(0)=0.0 y cos(0)=1.0 son exactos)
        For Each bar As RefuerzoSimple In barras
            Dim p_bar = bar.Coordenada_X * sinT + bar.Coordenada_Y * cosT
            Dim d_bar = projMax - p_bar
            Dim eps_s = If(c > 0.0001F, kappa * (c - d_bar), 0)
            Dim f_steel = Math.Max(-fy, Math.Min(fy, Es * eps_s))
            ' Descuento de concreto en zona de la barra (si está en compresión)
            Dim eps_c_bar = If(d_bar >= 0 AndAlso d_bar <= c AndAlso c > 0.0001F, kappa * (c - d_bar), 0)
            Dim sig_c_bar = SigNoCon(Math.Max(0, eps_c_bar), fc, 0.002F, 0.003F)
            Dim Fbar = (f_steel - sig_c_bar) * bar.Asb / 1_000_000.0F * 1000.0F  ' kN
            P += Fbar
            M += Fbar * p_bar
            If eps_s < 0 Then ems_max = Math.Max(ems_max, -eps_s)  ' tracción positiva
        Next

        M = CSng(Math.Abs(M))
    End Sub

End Module
