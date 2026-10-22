' ═══════════════════════════════════════════════════════════════════════════════
'  MuroContencionService — lógica pura del módulo 12.
'
'  Entrada: MuroContencion con geometría + suelo + materiales + opciones.
'  Salida : muro.Resultados poblado con empujes, pesos, chequeos y diseños.
'
'  Secciones:
'    1. Punto de entrada: AnalizarMuro
'    2. Coeficientes de empuje (Rankine, Coulomb, Mononobe-Okabe)
'    3. Empujes (estático, sobrecarga, sísmico, pasivo)
'    4. Pesos de los cuerpos (zapata, vástago, relleno, sobrecarga)
'    5. Estabilidad externa (volcamiento, deslizamiento, capacidad portante)
'    6. Diseño interno (vástago, talón, pie, llave) — envolvente estático∪sísmico
'    7. Helpers (AASHTO Heq, factores por normativa, flexión y cortante NSR-10)
'
'  Unidades (ver MuroContencion.vb): m, kN, kPa, MPa.
'  Análisis por metro de longitud de muro.
'  Convención de ejes: X desde el pie (lado del aire) hacia el talón; Y hacia arriba.
'  Momentos tomados respecto al PIE (punta externa) para FS volcamiento.
' ═══════════════════════════════════════════════════════════════════════════════

Public Class MuroContencionService

    ' Factores de reducción de resistencia (NSR-10 C.9.3.2 — mismos que ACI 318).
    Private Const PHI_FLEXION As Double = 0.90
    Private Const PHI_CORTANTE As Double = 0.75

    ' =====================================================================
    ' 1. Punto de entrada
    ' =====================================================================
    Public Shared Sub AnalizarMuro(muro As MuroContencion)
        If muro Is Nothing Then Throw New ArgumentNullException(NameOf(muro))
        If muro.Geometria Is Nothing OrElse muro.Suelo Is Nothing OrElse muro.Materiales Is Nothing Then
            Throw New InvalidOperationException("Muro sin geometría, suelo o materiales.")
        End If

        Dim res As New ResultadosMuro() With {.FechaCalculo = Date.Now}

        ' --- Coeficientes --------------------------------------------------
        Dim ka As Double, kp As Double, kae As Double
        CalcularCoeficientes(muro, ka, kp, kae)
        res.Ka = ka
        res.Kp = kp
        res.Kae = kae

        ' --- Empujes -------------------------------------------------------
        res.Empujes = CalcularEmpujes(muro, ka, kp, kae)

        ' --- Pesos ---------------------------------------------------------
        res.Pesos = CalcularPesos(muro)

        ' --- Estabilidad ---------------------------------------------------
        res.Estabilidad = New ChequeosEstabilidad()
        res.Estabilidad.CasoEstatico = ChequearCaso(muro, res, eCasoCargaMuro.Estatico)
        If muro.SueloSismico IsNot Nothing AndAlso muro.SueloSismico.IncluirSismo Then
            res.Estabilidad.CasoSismico = ChequearCaso(muro, res, eCasoCargaMuro.Sismico)
        End If

        ' --- Diseño interno ------------------------------------------------
        res.Disenos = DisenarElementos(muro, res)

        muro.Resultados = res
    End Sub

    ' =====================================================================
    ' 2. Coeficientes de empuje
    ' =====================================================================
    Public Shared Sub CalcularCoeficientes(muro As MuroContencion,
                                            ByRef ka As Double, ByRef kp As Double, ByRef kae As Double)
        Dim phi = Grados(muro.Suelo.Phi)
        Dim delta = Grados(muro.Suelo.Delta)

        Dim autoCalc As Boolean = muro.Opciones.CalcularKsAutomatico AndAlso muro.Suelo.CalcularKsAutomatico

        If Not autoCalc Then
            ka = muro.Suelo.KaManual
            kp = muro.Suelo.KpManual
        Else
            Select Case muro.Suelo.Teoria
                Case eTeoriaEmpuje.Rankine
                    ' Rankine: muro vertical, relleno horizontal, sin fricción.
                    ka = Math.Tan(Math.PI / 4 - phi / 2) ^ 2
                    kp = Math.Tan(Math.PI / 4 + phi / 2) ^ 2
                Case Else
                    ' Coulomb (muro vertical α=0, relleno horizontal β=0, con δ).
                    ka = CoulombKa(phi, delta)
                    kp = CoulombKp(phi, delta)
            End Select
        End If

        ' Mononobe-Okabe (dinámico).
        kae = 0
        If muro.SueloSismico IsNot Nothing AndAlso muro.SueloSismico.IncluirSismo Then
            Dim kh = muro.SueloSismico.Kh
            Dim kv = muro.SueloSismico.Kv
            Dim theta = Math.Atan(kh / (1 - kv))
            kae = MononobeOkabeKae(phi, delta, theta)
        End If
    End Sub

    Private Shared Function CoulombKa(phi As Double, delta As Double) As Double
        ' α=0 (muro vertical), β=0 (relleno horizontal).
        '          cos²(φ)
        ' Ka = ────────────────────────────────────────────
        '       cos(δ)·[1 + sqrt(sin(φ+δ)·sin(φ) / cos(δ))]²
        Dim num = Math.Cos(phi) ^ 2
        Dim arg = Math.Sin(phi + delta) * Math.Sin(phi) / Math.Cos(delta)
        If arg < 0 Then arg = 0
        Dim den = Math.Cos(delta) * (1 + Math.Sqrt(arg)) ^ 2
        If den <= 0 Then Return 0.333
        Return num / den
    End Function

    Private Shared Function CoulombKp(phi As Double, delta As Double) As Double
        Dim num = Math.Cos(phi) ^ 2
        Dim arg = Math.Sin(phi + delta) * Math.Sin(phi) / Math.Cos(delta)
        If arg < 0 Then arg = 0
        ' Coulomb pasivo con signo opuesto en delta — usamos la forma estable (δ bajo).
        Dim den = Math.Cos(delta) * (1 - Math.Sqrt(arg)) ^ 2
        If den <= 0 Then Return 3.0
        Return num / den
    End Function

    Private Shared Function MononobeOkabeKae(phi As Double, delta As Double, theta As Double) As Double
        ' α=0, β=0, muro vertical, relleno horizontal.
        '              cos²(φ − θ)
        ' Kae = ─────────────────────────────────────────────────────────────
        '        cos(θ)·cos(δ+θ)·[1 + sqrt(sin(φ+δ)·sin(φ−θ)/cos(δ+θ))]²
        If phi - theta <= 0 Then Return 1.0   ' licuefacción teórica — fuera de validez
        Dim num = Math.Cos(phi - theta) ^ 2
        Dim arg = Math.Sin(phi + delta) * Math.Sin(phi - theta) / Math.Cos(delta + theta)
        If arg < 0 Then arg = 0
        Dim den = Math.Cos(theta) * Math.Cos(delta + theta) * (1 + Math.Sqrt(arg)) ^ 2
        If den <= 0 Then Return 0
        Return num / den
    End Function

    ' =====================================================================
    ' 3. Empujes
    ' =====================================================================
    Friend Shared Function CalcularEmpujes(muro As MuroContencion, ka As Double, kp As Double, kae As Double) As EmpujesMuro
        Dim g = muro.Geometria
        Dim s = muro.Suelo
        Dim sob = muro.Sobrecarga
        Dim e As New EmpujesMuro()

        Dim gamma = s.GammaSuelo
        Dim H = g.H
        Dim delta = DeltaEfectivo(s)

        ' Empuje activo estático sobre toda la altura H.
        ' Pa = 0.5·γ·H²·Ka
        Dim pa = 0.5 * gamma * H * H * ka
        e.Ea_est = pa
        e.EaH_est = pa * Math.Cos(delta)
        e.EaV_est = pa * Math.Sin(delta)
        e.BrazoY_Ea_est = H / 3.0

        ' Sobrecarga: altura equivalente Heq (AASHTO auto para Vial, manual para Residencial).
        If sob IsNot Nothing AndAlso sob.IncluirSobrecarga Then
            Dim heq As Double = ResolverHeq(muro)
            Dim qeq = gamma * heq
            Dim esc = ka * qeq * H
            e.Esc_est = esc
            e.BrazoY_Esc_est = H / 2.0
        End If

        ' Incremento sísmico (ΔPae = Pae − Pa).
        If kae > 0 AndAlso muro.SueloSismico.IncluirSismo Then
            Dim kv = muro.SueloSismico.Kv
            Dim pae = 0.5 * gamma * H * H * (1 - kv) * kae
            Dim deltaPae = Math.Max(0, pae - pa)
            e.DeltaEae = deltaPae
            ' Punto de aplicación del incremento: 0.6·H (Seed & Whitman, aceptado por AASHTO).
            e.BrazoY_DeltaEae = 0.6 * H
        End If

        ' Empuje pasivo frontal (si se activa + llave de cortante incluida).
        If muro.Opciones.IncluirRellenoFrontal Then
            Dim hp = g.h1 + If(muro.Opciones.IncluirLlave, g.H_LL, 0.0)
            Dim ep = 0.5 * gamma * hp * hp * kp
            e.Ep = ep
            e.BrazoY_Ep = hp / 3.0
        End If

        Return e
    End Function

    ''' <summary>
    ''' Altura equivalente de suelo que representa la sobrecarga vehicular. Para
    ''' normativa Vial se interpola la AASHTO LRFD Tabla 3.11.6.4-2 (muro paralelo
    ''' al tráfico); para Residencial se usa el valor manual del usuario o la
    ''' equivalencia simple q/γ si fue dejado en 0.
    ''' </summary>
    Friend Shared Function ResolverHeq(muro As MuroContencion) As Double
        If muro.Sobrecarga Is Nothing OrElse Not muro.Sobrecarga.IncluirSobrecarga Then Return 0

        If muro.Normativa = eNormativaMuro.Vial Then
            Return HeqAASHTO(muro.Geometria.H, muro.Sobrecarga.DistanciaTraficoMm)
        End If

        ' Residencial: si el usuario ingresó Heq lo respetamos; si no, derivamos de q/γ.
        If muro.Sobrecarga.Heq > 0 Then Return muro.Sobrecarga.Heq
        If muro.Suelo.GammaSuelo <= 0 Then Return 0
        Return muro.Sobrecarga.SobrecargaKPa / muro.Suelo.GammaSuelo
    End Function

    ''' <summary>
    ''' AASHTO LRFD Tabla 3.11.6.4-2 (muros paralelos al tráfico). Interpolación lineal.
    ''' Para distancia ≥ 300 mm (1 ft), Heq = 0.6 m constante.
    ''' Para distancia 0 mm (junto al tráfico), Heq varía con la altura del muro:
    ''' H ≤ 1.5 m → 1.5 m; H = 3.0 m → 1.05 m; H ≥ 6.0 m → 0.6 m.
    ''' Entre esos valores, interpolación lineal. Valores en metros.
    ''' </summary>
    Public Shared Function HeqAASHTO(hMuro As Double, distanciaMm As Double) As Double
        If distanciaMm >= 300.0 Then Return 0.6
        If hMuro <= 1.5 Then Return 1.5
        If hMuro <= 3.0 Then Return 1.5 - (hMuro - 1.5) * (1.5 - 1.05) / (3.0 - 1.5)
        If hMuro <= 6.0 Then Return 1.05 - (hMuro - 3.0) * (1.05 - 0.6) / (6.0 - 3.0)
        Return 0.6
    End Function

    ' =====================================================================
    ' 4. Pesos de los cuerpos del muro
    ' =====================================================================
    Friend Shared Function CalcularPesos(muro As MuroContencion) As List(Of ElementoPeso)
        Dim g = muro.Geometria
        Dim m = muro.Materiales
        Dim s = muro.Suelo
        Dim pesos As New List(Of ElementoPeso)()

        ' Convención: X desde el PIE (lado del aire) hacia el talón.
        '   Zapata: de x=0 (pie) a x=B (talón).
        '   Vástago: cara TRASERA siempre vertical en x = b1 + e1.
        '            cara DELANTERA en base: x = b1.
        '            cara DELANTERA en corona: x = b1 + (e1 - e2).
        '   Si e2 < e1 → la cara frontal se retrae hacia atrás con la altura.
        Dim b = g.B
        Dim xCaraTras = g.b1 + g.e1          ' cara trasera (vertical)

        ' 1. ZAPATA (losa inferior) — rectángulo b × h1
        Dim pZap As New ElementoPeso With {
            .Nombre = "Zapata",
            .Area = b * g.h1,
            .PesoEspecifico = m.PesoConcreto,
            .BrazoX = b / 2.0
        }
        pZap.Peso = pZap.Area * pZap.PesoEspecifico
        pZap.MomentoResistente = pZap.Peso * pZap.BrazoX
        pesos.Add(pZap)

        ' 2. VÁSTAGO — cara trasera recta en x = b1 + e1
        Dim h2 = If(g.h2 > 0, g.h2, g.H - g.h1)
        If Math.Abs(g.e1 - g.e2) < 0.001 Then
            ' Rectángulo simple
            Dim pVast As New ElementoPeso With {
                .Nombre = "Vástago",
                .Area = g.e1 * h2,
                .PesoEspecifico = m.PesoConcreto,
                .BrazoX = g.b1 + g.e1 / 2.0
            }
            pVast.Peso = pVast.Area * pVast.PesoEspecifico
            pVast.MomentoResistente = pVast.Peso * pVast.BrazoX
            pesos.Add(pVast)
        Else
            ' Trapecio con cara trasera recta — se descompone en:
            '   (a) Rectángulo de ancho e2 contra la cara trasera.
            '       De x = b1 + (e1 - e2) a x = b1 + e1.
            '       Centro en x = b1 + e1 - e2/2.
            '   (b) Triángulo rectángulo del lado del pie (si e1 > e2).
            '       Vértices: (b1, h1), (b1 + (e1-e2), h1), (b1 + (e1-e2), H).
            '       Baricentro: x = (b1 + 2·(b1 + (e1-e2))) / 3 = b1 + 2(e1-e2)/3
            '                   y = (2·h1 + H) / 3
            Dim deltaE = g.e1 - g.e2
            Dim eMin = Math.Min(g.e1, g.e2)

            Dim pRect As New ElementoPeso With {
                .Nombre = "Vástago (rect.)",
                .Area = eMin * h2,
                .PesoEspecifico = m.PesoConcreto,
                .BrazoX = xCaraTras - eMin / 2.0
            }
            pRect.Peso = pRect.Area * pRect.PesoEspecifico
            pRect.MomentoResistente = pRect.Peso * pRect.BrazoX
            pesos.Add(pRect)

            If deltaE > 0 Then
                ' e1 > e2 (típico): triángulo frontal con cara trasera lisa.
                Dim pTri As New ElementoPeso With {
                    .Nombre = "Vástago (trian.)",
                    .Area = 0.5 * deltaE * h2,
                    .PesoEspecifico = m.PesoConcreto
                }
                pTri.BrazoX = g.b1 + 2.0 * deltaE / 3.0
                pTri.Peso = pTri.Area * pTri.PesoEspecifico
                pTri.MomentoResistente = pTri.Peso * pTri.BrazoX
                pesos.Add(pTri)
            End If
        End If

        ' 3. RELLENO sobre el talón (lado opuesto al aire).
        '    Ancho del talón: desde el borde derecho del vástago hasta B.
        Dim xTalonInicio = g.b1 + g.e1
        Dim talonLargo = b - xTalonInicio
        If talonLargo > 0 Then
            Dim pRel As New ElementoPeso With {
                .Nombre = "Relleno sobre talón",
                .Area = talonLargo * h2,
                .PesoEspecifico = s.GammaSuelo
            }
            pRel.BrazoX = xTalonInicio + talonLargo / 2.0
            pRel.Peso = pRel.Area * pRel.PesoEspecifico
            pRel.MomentoResistente = pRel.Peso * pRel.BrazoX
            pesos.Add(pRel)
        End If

        ' 4. SOBRECARGA sobre el talón (como peso resistente adicional).
        If muro.Sobrecarga IsNot Nothing AndAlso muro.Sobrecarga.IncluirSobrecarga Then
            Dim heq = ResolverHeq(muro)
            Dim qeq = s.GammaSuelo * heq
            If talonLargo > 0 AndAlso qeq > 0 Then
                Dim pSob As New ElementoPeso With {
                    .Nombre = "Sobrecarga sobre talón",
                    .Area = talonLargo,
                    .PesoEspecifico = qeq   ' kPa — se guarda aquí por diagnóstico; Peso ya es final
                }
                pSob.Peso = talonLargo * qeq
                pSob.BrazoX = xTalonInicio + talonLargo / 2.0
                pSob.MomentoResistente = pSob.Peso * pSob.BrazoX
                pesos.Add(pSob)
            End If
        End If

        ' 5. SUELO sobre el pie (si enterrado — Df > h1). Opcional, mejora deslizamiento.
        If muro.Opciones.IncluirRellenoFrontal AndAlso s.Df > g.h1 Then
            Dim hFrente = s.Df - g.h1
            Dim pieLargo = g.b1 - g.e1 / 2.0
            If pieLargo > 0 AndAlso hFrente > 0 Then
                Dim pFr As New ElementoPeso With {
                    .Nombre = "Suelo sobre pie",
                    .Area = pieLargo * hFrente,
                    .PesoEspecifico = s.GammaCimentacion
                }
                pFr.BrazoX = pieLargo / 2.0
                pFr.Peso = pFr.Area * pFr.PesoEspecifico
                pFr.MomentoResistente = pFr.Peso * pFr.BrazoX
                pesos.Add(pFr)
            End If
        End If

        ' 6. LLAVE DE CORTANTE (peso del hormigón bajo la zapata).
        If muro.Opciones.IncluirLlave AndAlso g.H_LL > 0 AndAlso g.B_LL > 0 Then
            Dim pLl As New ElementoPeso With {
                .Nombre = "Llave de cortante",
                .Area = g.B_LL * g.H_LL,
                .PesoEspecifico = m.PesoConcreto
            }
            ' Posición del centro de la llave: usualmente centrada bajo el vástago (base).
            pLl.BrazoX = g.b1 + g.e1 / 2.0
            pLl.Peso = pLl.Area * pLl.PesoEspecifico
            pLl.MomentoResistente = pLl.Peso * pLl.BrazoX
            pesos.Add(pLl)
        End If

        ' 7. Componente vertical del empuje activo (Ea·sin(δ)) actúa sobre el talón —
        '    se suma como peso resistente al momento. La UI lo mostrará por separado.
        '    Aquí se incluye como "cuerpo" virtual para que la tabla de pesos ya cuadre.
        Dim ka As Double, kp As Double, kae As Double
        CalcularCoeficientes(muro, ka, kp, kae)
        Dim pa_est = 0.5 * s.GammaSuelo * g.H * g.H * ka
        Dim delta = DeltaEfectivo(s)
        Dim eaV = pa_est * Math.Sin(delta)
        If eaV > 0 Then
            Dim pEaV As New ElementoPeso With {
                .Nombre = "Componente vertical Ea",
                .Area = 0,
                .PesoEspecifico = 0,
                .Peso = eaV,
                .BrazoX = b   ' aplicada en la cara trasera del muro (X = B)
            }
            pEaV.MomentoResistente = pEaV.Peso * pEaV.BrazoX
            pesos.Add(pEaV)
        End If

        Return pesos
    End Function

    ' =====================================================================
    ' 5. Estabilidad externa — chequea un caso de carga
    ' =====================================================================
    Friend Shared Function ChequearCaso(muro As MuroContencion, res As ResultadosMuro, caso As eCasoCargaMuro) As ChequeoCaso
        Dim g = muro.Geometria
        Dim s = muro.Suelo
        Dim e = res.Empujes
        Dim c As New ChequeoCaso()

        ' --- Suma de pesos y momento resistente ---------------------------
        Dim w = 0.0
        Dim mr = 0.0
        For Each p In res.Pesos
            w += p.Peso
            mr += p.MomentoResistente
        Next

        ' --- Fuerzas horizontales deslizantes -----------------------------
        Dim fh = e.EaH_est
        Dim ms = e.Ea_est * Math.Cos(DeltaEfectivo(s)) * e.BrazoY_Ea_est   ' momento del Ea_h
        '  (equiv. a e.EaH_est * e.BrazoY_Ea_est — explícito por legibilidad)
        If e.Esc_est > 0 Then
            fh += e.Esc_est
            ms += e.Esc_est * e.BrazoY_Esc_est
        End If
        If caso = eCasoCargaMuro.Sismico AndAlso e.DeltaEae > 0 Then
            fh += e.DeltaEae
            ms += e.DeltaEae * e.BrazoY_DeltaEae
        End If

        ' Momento volcante respecto al PIE (X=0, Y=0).
        c.MomentoVolcante = ms
        c.MomentoResistente = mr
        c.FsVolcamiento = If(ms > 0, mr / ms, Double.PositiveInfinity)

        ' --- Deslizamiento ------------------------------------------------
        Dim mu = muro.Suelo.MuBase
        If mu <= 0 Then mu = Math.Tan(Grados(muro.Suelo.Phi * 2.0 / 3.0))
        Dim fResist = w * mu + If(muro.Opciones.IncluirRellenoFrontal, e.Ep, 0.0)
        c.FuerzaResistente = fResist
        c.FuerzaDeslizante = fh
        c.FsDeslizamiento = If(fh > 0, fResist / fh, Double.PositiveInfinity)

        ' --- Excentricidad y presiones (Meyerhof) -------------------------
        '  e = B/2 − x̄, con x̄ = (MR − MV) / W. x̄ se mide desde el pie.
        Dim xBar = If(w > 0, (mr - ms) / w, g.B / 2.0)
        Dim exc = g.B / 2.0 - xBar
        c.Excentricidad = exc
        c.PesoTotal = w

        Dim b = g.B
        If Math.Abs(exc) <= b / 6.0 Then
            ' Distribución trapezoidal
            c.PresionMax = (w / b) * (1 + 6 * Math.Abs(exc) / b)
            c.PresionMin = (w / b) * (1 - 6 * Math.Abs(exc) / b)
        Else
            ' Distribución triangular reducida
            Dim bEff = 3 * (b / 2.0 - Math.Abs(exc))
            If bEff <= 0 Then
                c.PresionMax = Double.PositiveInfinity
            Else
                c.PresionMax = 2 * w / bEff
            End If
            c.PresionMin = 0
        End If

        ' --- Admisibles según normativa + caso ----------------------------
        Dim adm = AdmisiblesEstabilidad(muro.Normativa, caso)
        c.FsVolcamientoAdmisible = adm.FsVol
        c.FsDeslizamientoAdmisible = adm.FsDes
        c.ExcentricidadAdmisible = b * adm.ExcAdm
        c.PresionAdmisible = muro.Suelo.CapacidadPortante

        Return c
    End Function

    ' =====================================================================
    ' 6. Diseño interno — envolvente REAL estático∪sísmico
    ' =====================================================================
    ' Flujo:
    '   1. Para cada elemento (vástago, talón, pie, llave), calcular (Vu, Mu)
    '      en el caso estático con factores de Strength I.
    '   2. Si hay sismo, calcular (Vu, Mu) en Extreme I con factores 1.0 +
    '      incremento Mononobe-Okabe sobre EH.
    '   3. Tomar envolvente Max(Vu_est, Vu_sis) y Max(Mu_est, Mu_sis).
    '   4. Dimensionar flexión y cortante con la envolvente.
    '   5. Proponer barra + separación tal que As_prov ≥ As_req.
    ' =====================================================================
    Friend Shared Function DisenarElementos(muro As MuroContencion, res As ResultadosMuro) As DisenosElementos
        Dim d As New DisenosElementos()
        Dim m = muro.Materiales
        Dim g = muro.Geometria
        Dim rec = m.Recubrimiento
        Dim incluirSismo = muro.SueloSismico IsNot Nothing AndAlso muro.SueloSismico.IncluirSismo

        ' --- Factores por caso --------------------------------------------
        Dim fEst = FactoresCarga(muro.Normativa, eCasoCargaMuro.Estatico)
        Dim fSis = FactoresCarga(muro.Normativa, eCasoCargaMuro.Sismico)

        ' --- Vástago ------------------------------------------------------
        Dim vast = New DisenoElementoMuro With {
            .Nombre = "Vástago", .B = 1.0, .H = g.e1,
            .d = Math.Max(g.e1 - rec, 0.001)
        }
        Dim demVastEst = DemandaVastago(muro, res, eCasoCargaMuro.Estatico, fEst)
        vast.Vu = demVastEst.V
        vast.Mu = demVastEst.M
        If incluirSismo Then
            Dim demVastSis = DemandaVastago(muro, res, eCasoCargaMuro.Sismico, fSis)
            vast.Vu = Math.Max(vast.Vu, demVastSis.V)
            vast.Mu = Math.Max(vast.Mu, demVastSis.M)
        End If
        DisenarFlexionCortante(vast, m)
        PropuestaRefuerzo(vast)
        d.Vastago = vast

        ' --- Talón --------------------------------------------------------
        Dim talon = New DisenoElementoMuro With {
            .Nombre = "Talón", .B = 1.0, .H = g.h1,
            .d = Math.Max(g.h1 - rec, 0.001)
        }
        Dim demTalEst = DemandaTalon(muro, res, eCasoCargaMuro.Estatico, fEst)
        talon.Vu = demTalEst.V
        talon.Mu = demTalEst.M
        If incluirSismo AndAlso res.Estabilidad.CasoSismico IsNot Nothing Then
            Dim demTalSis = DemandaTalon(muro, res, eCasoCargaMuro.Sismico, fSis)
            talon.Vu = Math.Max(talon.Vu, demTalSis.V)
            talon.Mu = Math.Max(talon.Mu, demTalSis.M)
        End If
        DisenarFlexionCortante(talon, m)
        PropuestaRefuerzo(talon)
        d.Talon = talon

        ' --- Pie ----------------------------------------------------------
        Dim pie = New DisenoElementoMuro With {
            .Nombre = "Pie", .B = 1.0, .H = g.h1,
            .d = Math.Max(g.h1 - rec, 0.001)
        }
        Dim demPieEst = DemandaPie(muro, res, eCasoCargaMuro.Estatico, fEst)
        pie.Vu = demPieEst.V
        pie.Mu = demPieEst.M
        If incluirSismo AndAlso res.Estabilidad.CasoSismico IsNot Nothing Then
            Dim demPieSis = DemandaPie(muro, res, eCasoCargaMuro.Sismico, fSis)
            pie.Vu = Math.Max(pie.Vu, demPieSis.V)
            pie.Mu = Math.Max(pie.Mu, demPieSis.M)
        End If
        DisenarFlexionCortante(pie, m)
        PropuestaRefuerzo(pie)
        d.Pie = pie

        ' --- Llave de cortante --------------------------------------------
        If muro.Opciones.IncluirLlave AndAlso g.H_LL > 0 Then
            Dim llave = New DisenoElementoMuro With {
                .Nombre = "Llave de cortante", .B = 1.0, .H = g.B_LL,
                .d = Math.Max(g.B_LL - rec, 0.001)
            }
            Dim demLlaveEst = DemandaLlave(muro, res, eCasoCargaMuro.Estatico, fEst)
            llave.Vu = demLlaveEst.V
            llave.Mu = demLlaveEst.M
            If incluirSismo Then
                Dim demLlaveSis = DemandaLlave(muro, res, eCasoCargaMuro.Sismico, fSis)
                llave.Vu = Math.Max(llave.Vu, demLlaveSis.V)
                llave.Mu = Math.Max(llave.Mu, demLlaveSis.M)
            End If
            DisenarFlexionCortante(llave, m)
            PropuestaRefuerzo(llave)
            d.Llave = llave
        End If

        Return d
    End Function

    ' --- Demanda VÁSTAGO en el caso dado ----------------------------------
    Private Shared Function DemandaVastago(muro As MuroContencion, res As ResultadosMuro,
                                            caso As eCasoCargaMuro, f As FactoresLRFD) As DemandaUltima
        Dim s = muro.Suelo
        Dim g = muro.Geometria
        Dim hv = If(g.h2 > 0, g.h2, g.H - g.h1)
        Dim delta = DeltaEfectivo(s)
        Dim r As DemandaUltima

        ' Empuje activo estático sobre el vástago (sin incluir la parte en la zapata)
        Dim paV = 0.5 * s.GammaSuelo * hv * hv * res.Ka
        Dim paV_h = paV * Math.Cos(delta)
        r.V = paV_h * f.EH
        r.M = paV_h * (hv / 3.0) * f.EH

        ' Sobrecarga
        If muro.Sobrecarga IsNot Nothing AndAlso muro.Sobrecarga.IncluirSobrecarga Then
            Dim heq = ResolverHeq(muro)
            Dim esc = res.Ka * s.GammaSuelo * heq * hv
            r.V += esc * f.LS
            r.M += esc * (hv / 2.0) * f.LS
        End If

        ' Incremento sísmico Mononobe-Okabe
        If caso = eCasoCargaMuro.Sismico AndAlso res.Kae > 0 Then
            Dim kv = muro.SueloSismico.Kv
            Dim paeV = 0.5 * s.GammaSuelo * hv * hv * (1 - kv) * res.Kae
            Dim deltaPaeV = Math.Max(0, paeV - paV)
            r.V += deltaPaeV * f.EQ
            r.M += deltaPaeV * (0.6 * hv) * f.EQ
        End If
        Return r
    End Function

    ' --- Demanda TALÓN en el caso dado ------------------------------------
    Private Shared Function DemandaTalon(muro As MuroContencion, res As ResultadosMuro,
                                          caso As eCasoCargaMuro, f As FactoresLRFD) As DemandaUltima
        Dim g = muro.Geometria
        Dim s = muro.Suelo
        Dim m = muro.Materiales
        Dim r As DemandaUltima

        Dim talonLargo = g.B - g.b1 - g.e1
        If talonLargo <= 0 Then Return r

        Dim hv = If(g.h2 > 0, g.h2, g.H - g.h1)
        Dim wRelleno = s.GammaSuelo * hv * talonLargo * f.EV
        Dim wLosa = m.PesoConcreto * g.h1 * talonLargo * f.DC
        Dim wSob = 0.0
        If muro.Sobrecarga IsNot Nothing AndAlso muro.Sobrecarga.IncluirSobrecarga Then
            Dim heq = ResolverHeq(muro)
            wSob = s.GammaSuelo * heq * talonLargo * f.LS
        End If

        Dim chequeo = If(caso = eCasoCargaMuro.Sismico AndAlso res.Estabilidad.CasoSismico IsNot Nothing,
                          res.Estabilidad.CasoSismico, res.Estabilidad.CasoEstatico)
        Dim qPromTalon = PromedioPresionBajo(chequeo, g, enTalon:=True)
        Dim wReac = qPromTalon * talonLargo * f.EV

        Dim wNeta = Math.Max((wRelleno + wLosa + wSob) - wReac, 0)
        r.V = wNeta
        r.M = wNeta * (talonLargo / 2.0)
        Return r
    End Function

    ' --- Demanda PIE en el caso dado --------------------------------------
    Private Shared Function DemandaPie(muro As MuroContencion, res As ResultadosMuro,
                                        caso As eCasoCargaMuro, f As FactoresLRFD) As DemandaUltima
        Dim g = muro.Geometria
        Dim m = muro.Materiales
        Dim r As DemandaUltima

        Dim pieLargo = g.b1 - g.e1 / 2.0
        If pieLargo <= 0 Then Return r

        Dim chequeo = If(caso = eCasoCargaMuro.Sismico AndAlso res.Estabilidad.CasoSismico IsNot Nothing,
                          res.Estabilidad.CasoSismico, res.Estabilidad.CasoEstatico)
        Dim qPromPie = PromedioPresionBajo(chequeo, g, enTalon:=False)
        Dim wReac = qPromPie * pieLargo * f.EV
        Dim wLosa = m.PesoConcreto * g.h1 * pieLargo * f.DC

        Dim wNeta = Math.Max(wReac - wLosa, 0)
        r.V = wNeta
        r.M = wNeta * (pieLargo / 2.0)
        Return r
    End Function

    ' --- Demanda LLAVE en el caso dado ------------------------------------
    Private Shared Function DemandaLlave(muro As MuroContencion, res As ResultadosMuro,
                                          caso As eCasoCargaMuro, f As FactoresLRFD) As DemandaUltima
        Dim s = muro.Suelo
        Dim g = muro.Geometria
        Dim r As DemandaUltima

        Dim fp = 0.5 * s.GammaSuelo * g.H_LL * g.H_LL * res.Kp
        r.V = fp * f.EH
        r.M = fp * (g.H_LL / 3.0) * f.EH
        Return r
    End Function

    ' Estructura liviana para pasar factores entre funciones.
    Private Structure FactoresLRFD
        Public EH As Double
        Public EV As Double
        Public LS As Double
        Public DC As Double
        Public EQ As Double
    End Structure

    Private Structure DemandaUltima
        Public V As Double
        Public M As Double
    End Structure

    ' --- Diseño flexión + cortante por sección ------------------------
    Private Shared Sub DisenarFlexionCortante(elem As DisenoElementoMuro, mat As PropsMateriales)
        If elem.Mu > 0 Then
            elem.AsReq = AreaAceroFlexion(elem.B, elem.d, elem.Mu, mat.Fc, mat.Fy)
        End If
        elem.AsReqTemperatura = AceroTemperatura(elem.B, elem.H, mat.Fy)

        ' φVc = 0.75 × 0.17 × √fc × b × d × 1000 (kN) — fc en MPa, b/d en m.
        elem.PhiVc = PHI_CORTANTE * 0.17 * Math.Sqrt(mat.Fc) * elem.B * elem.d * 1000
    End Sub

    ''' <summary>
    ''' Área de acero por flexión en una sección rectangular. Entrada b, d en m, Mu
    ''' en kN·m. Salida en mm²/m. Aplica cuantía mínima NSR-10 C.10.5 (max(1.4/fy,
    ''' 0.25·√fc/fy)) y verifica que la regla 1.33·ρ_calc no sea más desfavorable.
    ''' </summary>
    Public Shared Function AreaAceroFlexion(b As Double, d As Double, Mu As Double,
                                             fc As Double, fy As Double) As Double
        If Mu <= 0 OrElse b <= 0 OrElse d <= 0 Then Return 0
        Dim mn = Mu / PHI_FLEXION * 1000000.0   ' N·mm (de kN·m)
        Dim bMm = b * 1000.0
        Dim dMm = d * 1000.0

        Dim m2 = fy / (0.85 * fc)
        Dim rn = Mn / (bMm * dMm * dMm)
        Dim radicando = 1 - 2 * m2 * rn / fy
        If radicando < 0 Then Return Double.PositiveInfinity   ' sección insuficiente

        Dim rho = (1 - Math.Sqrt(radicando)) / m2

        ' Cuantía mínima
        Dim rhoMin = Math.Max(1.4 / fy, 0.25 * Math.Sqrt(fc) / fy)
        If rho < rhoMin Then
            ' Regla 1.33·ρ_calc (NSR-10 C.10.5.3)
            If 1.33 * rho < rhoMin Then rho = 1.33 * rho Else rho = rhoMin
        End If

        Return rho * bMm * dMm    ' mm²/m (si b=1 m)
    End Function

    ''' <summary>
    ''' Acero por temperatura y retracción en losas (NSR-10 C.7.12 / ACI 7.6.1).
    ''' Para fy ≥ 420 MPa usa 0.0018·b·h. Devuelve mm²/m total — se reparte en dos caras.
    ''' </summary>
    Public Shared Function AceroTemperatura(b As Double, h As Double, fy As Double) As Double
        If b <= 0 OrElse h <= 0 Then Return 0
        Dim rho As Double = 0.0018
        If fy < 420 Then rho = 0.002
        Return rho * (b * 1000) * (h * 1000)
    End Function

    ''' <summary>Propuesta inicial de refuerzo: barra #5 con separación tal que As_prov cubra As_req
    ''' sin pasarse de 20 cm (típico muros). Se puede sobrescribir en la UI.</summary>
    Private Shared Sub PropuestaRefuerzo(elem As DisenoElementoMuro)
        Dim barras = {"#4", "#5", "#6"}
        Dim mejor As (Barra As String, Sep As Double) = ("#5", 20.0)

        For Each b In barras
            Dim area = Funciones_00_Varias.AreaRefuerzo(b)
            If area <= 0 Then Continue For
            If elem.AsReq > 0 Then
                Dim sep = Math.Floor(area * 100.0 / elem.AsReq / 2.5) * 2.5
                If sep < 7.5 Then Continue For
                If sep > 20 Then sep = 20
                mejor = (b, sep)
                Exit For
            End If
        Next
        elem.BarraPrincipal = mejor.Barra
        elem.SeparacionCm = mejor.Sep
        elem.AsProv = Funciones_00_Varias.AreaRefuerzo(mejor.Barra) * 100.0 / mejor.Sep

        ' Temperatura — #4 cada 20 cm por default
        elem.BarraTemperatura = "#4"
        elem.SeparacionTempCm = 20
        elem.AsProvTemperatura = Funciones_00_Varias.AreaRefuerzo("#4") * 100.0 / 20.0
    End Sub

    ''' <summary>
    ''' Recalcula AsProv a partir de la barra y separación actuales. La UI la llama
    ''' tras que el usuario cambia barra o separación — permite ver inmediatamente el C/D.
    ''' </summary>
    Public Shared Sub RecalcularAsProv(elem As DisenoElementoMuro)
        If elem Is Nothing Then Return
        Dim area = Funciones_00_Varias.AreaRefuerzo(elem.BarraPrincipal)
        If elem.SeparacionCm > 0 Then
            elem.AsProv = area * 100.0 / elem.SeparacionCm
        End If
        Dim areaT = Funciones_00_Varias.AreaRefuerzo(elem.BarraTemperatura)
        If elem.SeparacionTempCm > 0 Then
            elem.AsProvTemperatura = areaT * 100.0 / elem.SeparacionTempCm
        End If
    End Sub

    ' =====================================================================
    ' 7. Helpers de normativa
    ' =====================================================================
    Private Shared Function AdmisiblesEstabilidad(norma As eNormativaMuro, caso As eCasoCargaMuro) _
                As (FsVol As Double, FsDes As Double, ExcAdm As Double)
        ' Valores "convencionales" vs "INVIAS/LRFD" tomados de la app fuente
        ' DesignPuentes (sala técnica del usuario) — práctica colombiana estándar
        ' para muros de contención y estribos.
        Select Case norma
            Case eNormativaMuro.Residencial
                If caso = eCasoCargaMuro.Sismico Then
                    Return (2.0, 1.05, 1.0 / 6.0)
                Else
                    Return (3.0, 1.6, 1.0 / 6.0)
                End If
            Case eNormativaMuro.Vial
                ' CCP-14 / AASHTO LRFD — cargas factorizadas en el diseño; en
                ' estabilidad el admisible es ya 1.0 (FS "formal" tras factores).
                If caso = eCasoCargaMuro.Sismico Then
                    Return (1.0, 1.0, 1.0 / 3.0)
                Else
                    Return (1.0, 1.5, 1.0 / 3.0)
                End If
        End Select
        Return (3.0, 1.6, 1.0 / 6.0)
    End Function

    ''' <summary>
    ''' Factores de carga por tipo y caso (DC = peso concreto, EV = peso suelo,
    ''' EH = empuje horizontal, LS = sobrecarga viva, EQ = sismo).
    ''' Caso Estático → Strength I. Caso Sísmico → Extreme I (todos los factores
    ''' se reducen a 1.0 excepto LS que baja a 0.5 — típico AASHTO/CCP-14 extremo).
    ''' </summary>
    Private Shared Function FactoresCarga(norma As eNormativaMuro, caso As eCasoCargaMuro) As FactoresLRFD
        If caso = eCasoCargaMuro.Sismico Then
            Return New FactoresLRFD With {.EH = 1.0, .EV = 1.0, .LS = 0.5, .DC = 1.0, .EQ = 1.0}
        End If
        Select Case norma
            Case eNormativaMuro.Vial
                Return New FactoresLRFD With {.EH = 1.50, .EV = 1.35, .LS = 1.75, .DC = 1.25, .EQ = 0.0}
            Case Else   ' Residencial — NSR-10 B.2.4 colapsado por tipo (1.2D + 1.6L)
                Return New FactoresLRFD With {.EH = 1.60, .EV = 1.20, .LS = 1.60, .DC = 1.20, .EQ = 0.0}
        End Select
    End Function

    ''' <summary>
    ''' Presión promedio bajo una parte de la zapata (talón o pie) interpolando
    ''' linealmente la distribución entre PresionMax (en el pie) y PresionMin (en el talón).
    ''' </summary>
    Private Shared Function PromedioPresionBajo(c As ChequeoCaso, g As GeometriaMuro, enTalon As Boolean) As Double
        If c Is Nothing OrElse g.B <= 0 Then Return 0
        Dim qPie = c.PresionMax
        Dim qTal = c.PresionMin
        ' Si la excentricidad es negativa (más carga del lado del talón), invertir.
        If c.Excentricidad < 0 Then
            qPie = c.PresionMin
            qTal = c.PresionMax
        End If
        Dim pieLargo = g.b1 - g.e1 / 2.0
        Dim talonLargo = g.B - g.b1 - g.e1
        Dim qCaraVast_desdePie = qPie - (qPie - qTal) * (pieLargo / g.B)
        Dim qCaraVast_desdeTalon = qTal + (qPie - qTal) * (1 - (pieLargo + g.e1) / g.B)
        If enTalon Then
            Return (qCaraVast_desdeTalon + qTal) / 2.0
        Else
            Return (qPie + qCaraVast_desdePie) / 2.0
        End If
    End Function

    Private Shared Function Grados(gradosDeg As Double) As Double
        Return gradosDeg * Math.PI / 180.0
    End Function

    ''' <summary>
    ''' δ efectivo según la teoría. En Rankine δ = 0 (relleno horizontal, muro vertical,
    ''' sin fricción); el empuje queda enteramente horizontal. En Coulomb δ = Suelo.Delta.
    ''' </summary>
    Private Shared Function DeltaEfectivo(s As PropsSuelo) As Double
        If s.Teoria = eTeoriaEmpuje.Rankine Then Return 0.0
        Return Grados(s.Delta)
    End Function

End Class
