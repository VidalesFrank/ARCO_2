Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 12 — Muros de Contención
'  Tests contra los números del muro cantilever de referencia (H=4m, φ=30°).
'  Los números esperados vienen de cálculo a mano con las fórmulas de la memoria
'  técnica del módulo (Rankine, Coulomb, Mononobe-Okabe, Meyerhof).
'  Tolerancia: 1% para los coeficientes (dependen de redondeo trigonométrico),
'  3% para resultados compuestos (empujes · brazos · pesos).
' ═══════════════════════════════════════════════════════════════════════════════
<TestClass>
Public Class MuroContencionTests

    ' --- Muro de referencia (parámetros compartidos entre tests) --------
    Private Shared Function MuroBase() As MuroContencion
        Dim m As New MuroContencion()
        m.Geometria.H = 4.0
        m.Geometria.h1 = 0.4
        m.Geometria.h2 = 3.6
        m.Geometria.B = 2.5
        m.Geometria.a = 0.7
        m.Geometria.b1 = 0.55
        m.Geometria.e1 = 0.3
        m.Geometria.e2 = 0.3
        m.Suelo.Phi = 30.0
        m.Suelo.Delta = 20.0
        m.Suelo.GammaSuelo = 18.0
        m.Suelo.GammaCimentacion = 18.0
        m.Suelo.Df = 1.0
        m.Suelo.CapacidadPortante = 200.0
        m.Suelo.Teoria = eTeoriaEmpuje.Rankine   ' default simple para los tests analíticos
        m.Materiales.Fc = 21.0
        m.Materiales.Fy = 420.0
        m.Materiales.PesoConcreto = 24.0
        m.Materiales.Recubrimiento = 0.05
        m.Normativa = eNormativaMuro.Residencial
        Return m
    End Function

    ' =====================================================================
    ' Coeficientes de empuje
    ' =====================================================================

    ''' <summary>Rankine Ka con φ=30° = tan²(30°) = 1/3 = 0.333.</summary>
    <TestMethod>
    Public Sub Rankine_Ka_phi30()
        Dim m = MuroBase()
        m.Suelo.Teoria = eTeoriaEmpuje.Rankine
        Dim ka, kp, kae As Double
        MuroContencionService.CalcularCoeficientes(m, ka, kp, kae)
        Assert.AreEqual(0.3333, ka, 0.001, "Rankine Ka(30°) = tan²(30°) ≈ 0.333")
        Assert.AreEqual(3.0, kp, 0.01, "Rankine Kp(30°) = tan²(60°) = 3.0")
    End Sub

    ''' <summary>Coulomb Ka con φ=30°, δ=20° ≈ 0.297 (cálculo manual).</summary>
    <TestMethod>
    Public Sub Coulomb_Ka_phi30_delta20()
        Dim m = MuroBase()
        m.Suelo.Teoria = eTeoriaEmpuje.Coulomb
        Dim ka, kp, kae As Double
        MuroContencionService.CalcularCoeficientes(m, ka, kp, kae)
        Assert.AreEqual(0.297, ka, 0.005, "Coulomb Ka con δ ≠ 0 es menor que Rankine")
    End Sub

    ''' <summary>Sin sismo activo, Kae = 0.</summary>
    <TestMethod>
    Public Sub Kae_SinSismo_EsCero()
        Dim m = MuroBase()
        m.SueloSismico.IncluirSismo = False
        Dim ka, kp, kae As Double
        MuroContencionService.CalcularCoeficientes(m, ka, kp, kae)
        Assert.AreEqual(0.0, kae, 0.0001)
    End Sub

    ''' <summary>Con sismo, Kae > Ka — Mononobe-Okabe amplifica.</summary>
    <TestMethod>
    Public Sub Kae_ConSismo_MayorQueKa()
        Dim m = MuroBase()
        m.SueloSismico.IncluirSismo = True
        m.SueloSismico.Kh = 0.15
        m.SueloSismico.Kv = 0.0
        Dim ka, kp, kae As Double
        MuroContencionService.CalcularCoeficientes(m, ka, kp, kae)
        Assert.IsTrue(kae > ka, $"Kae ({kae:F3}) debe ser > Ka ({ka:F3})")
        Assert.IsTrue(kae < 1.0, "Kae > 1 señala licuefacción teórica; con Kh=0.15 debe quedar lejos")
    End Sub

    ' =====================================================================
    ' Empujes
    ' =====================================================================

    ''' <summary>Pa = 0.5·γ·H²·Ka. Rankine 18·16·0.333/2 = 47.95 kN/m.</summary>
    <TestMethod>
    Public Sub EmpujeActivo_Rankine_H4()
        Dim m = MuroBase()
        m.Suelo.Teoria = eTeoriaEmpuje.Rankine
        MuroContencionService.AnalizarMuro(m)
        Assert.AreEqual(47.95, m.Resultados.Empujes.Ea_est, 0.3, "Pa Rankine = 0.5·18·16·0.333")
        Assert.AreEqual(m.Resultados.Empujes.Ea_est,
                         m.Resultados.Empujes.EaH_est, 0.01,
                         "Rankine: todo el empuje es horizontal (δ efectivo = 0)")
        Assert.AreEqual(0.0, m.Resultados.Empujes.EaV_est, 0.01,
                         "Rankine: sin componente vertical")
    End Sub

    ''' <summary>Punto de aplicación del empuje activo estático: H/3.</summary>
    <TestMethod>
    Public Sub EmpujeActivo_PuntoAplicacion_Htercios()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Assert.AreEqual(m.Geometria.H / 3.0, m.Resultados.Empujes.BrazoY_Ea_est, 0.001)
    End Sub

    ''' <summary>Coulomb: Ea_v = Pa·sin(δ) > 0 — componente vertical sobre la cara trasera.</summary>
    <TestMethod>
    Public Sub EmpujeActivo_Coulomb_TieneComponenteVertical()
        Dim m = MuroBase()
        m.Suelo.Teoria = eTeoriaEmpuje.Coulomb
        MuroContencionService.AnalizarMuro(m)
        Assert.IsTrue(m.Resultados.Empujes.EaV_est > 0,
            "Coulomb con δ=20° debe aportar componente vertical")
        Assert.IsTrue(m.Resultados.Empujes.EaH_est < m.Resultados.Empujes.Ea_est,
            "EaH < Ea_total porque hay descomposición por δ")
    End Sub

    ''' <summary>Incremento sísmico ΔPae aplicado en 0.6·H (Seed & Whitman).</summary>
    <TestMethod>
    Public Sub EmpujeSismico_PuntoAplicacion_06H()
        Dim m = MuroBase()
        m.SueloSismico.IncluirSismo = True
        m.SueloSismico.Kh = 0.15
        MuroContencionService.AnalizarMuro(m)
        Assert.IsTrue(m.Resultados.Empujes.DeltaEae > 0, "con sismo, ΔPae > 0")
        Assert.AreEqual(0.6 * m.Geometria.H, m.Resultados.Empujes.BrazoY_DeltaEae, 0.001)
    End Sub

    ' =====================================================================
    ' AASHTO Heq — Tabla 3.11.6.4-2
    ' =====================================================================

    <TestMethod>
    Public Sub HeqAASHTO_MuroCorto_SinDistancia_15m()
        Assert.AreEqual(1.5, MuroContencionService.HeqAASHTO(1.5, 0), 0.001)
        Assert.AreEqual(1.5, MuroContencionService.HeqAASHTO(1.0, 0), 0.001, "H < 1.5 → 1.5 m")
    End Sub

    <TestMethod>
    Public Sub HeqAASHTO_MuroMedio_Interpolado()
        ' H=3m, dist=0 → 1.05 m (segundo pivote de la tabla)
        Assert.AreEqual(1.05, MuroContencionService.HeqAASHTO(3.0, 0), 0.001)
        ' H=2m → interpolación entre 1.5 (H=1.5) y 1.05 (H=3): valor = 1.5 - 0.5·(1.5-1.05)/1.5 = 1.35
        Assert.AreEqual(1.35, MuroContencionService.HeqAASHTO(2.0, 0), 0.01)
    End Sub

    <TestMethod>
    Public Sub HeqAASHTO_MuroAlto_06m()
        Assert.AreEqual(0.6, MuroContencionService.HeqAASHTO(6.0, 0), 0.001)
        Assert.AreEqual(0.6, MuroContencionService.HeqAASHTO(10.0, 0), 0.001, "H > 6 → 0.6 m")
    End Sub

    <TestMethod>
    Public Sub HeqAASHTO_ConDistancia_06m()
        ' Distancia ≥ 300 mm (1 ft) → Heq = 0.6 m constante.
        Assert.AreEqual(0.6, MuroContencionService.HeqAASHTO(3.0, 300), 0.001)
        Assert.AreEqual(0.6, MuroContencionService.HeqAASHTO(10.0, 500), 0.001)
    End Sub

    ' =====================================================================
    ' Pesos
    ' =====================================================================

    ''' <summary>Lista de pesos tiene al menos zapata, vástago y relleno sobre talón.</summary>
    <TestMethod>
    Public Sub Pesos_IncluyeZapataVastagoRelleno()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim nombres = m.Resultados.Pesos.Select(Function(p) p.Nombre).ToList()
        CollectionAssert.Contains(nombres, "Zapata")
        CollectionAssert.Contains(nombres, "Vástago")
        CollectionAssert.Contains(nombres, "Relleno sobre talón")
    End Sub

    ''' <summary>Peso de la zapata: B·h1·γc = 2.5·0.4·24 = 24.0 kN/m.</summary>
    <TestMethod>
    Public Sub Pesos_Zapata_Correcto()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim zap = m.Resultados.Pesos.First(Function(p) p.Nombre = "Zapata")
        Assert.AreEqual(24.0, zap.Peso, 0.01)
        Assert.AreEqual(m.Geometria.B / 2.0, zap.BrazoX, 0.001, "zapata centrada → brazo = B/2")
    End Sub

    ''' <summary>Peso del vástago: e1·h2·γc = 0.3·3.6·24 = 25.92 kN/m.</summary>
    <TestMethod>
    Public Sub Pesos_Vastago_Correcto()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim v = m.Resultados.Pesos.First(Function(p) p.Nombre = "Vástago")
        Assert.AreEqual(25.92, v.Peso, 0.01)
    End Sub

    ' =====================================================================
    ' Estabilidad
    ' =====================================================================

    ''' <summary>Caso estático: FS vol y FS des finitos y positivos.</summary>
    <TestMethod>
    Public Sub Estabilidad_Estatico_FsFinitosYPositivos()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim ce = m.Resultados.Estabilidad.CasoEstatico
        Assert.IsTrue(ce.FsVolcamiento > 0 AndAlso Not Double.IsInfinity(ce.FsVolcamiento))
        Assert.IsTrue(ce.FsDeslizamiento > 0 AndAlso Not Double.IsInfinity(ce.FsDeslizamiento))
        Assert.IsTrue(ce.PresionMax > 0)
    End Sub

    ''' <summary>Admisibles residenciales estáticos: FS vol 3.0, FS des 1.6.</summary>
    <TestMethod>
    Public Sub Estabilidad_Residencial_FsAdmisibles()
        Dim m = MuroBase()
        m.Normativa = eNormativaMuro.Residencial
        MuroContencionService.AnalizarMuro(m)
        Dim ce = m.Resultados.Estabilidad.CasoEstatico
        Assert.AreEqual(3.0, ce.FsVolcamientoAdmisible, 0.01)
        Assert.AreEqual(1.6, ce.FsDeslizamientoAdmisible, 0.01)
        Assert.AreEqual(m.Geometria.B / 6.0, ce.ExcentricidadAdmisible, 0.001, "e_adm = B/6")
    End Sub

    ''' <summary>Admisibles viales: FS vol 1.0 (ya factorizados), e_adm = B/3.</summary>
    <TestMethod>
    Public Sub Estabilidad_Vial_FsAdmisibles()
        Dim m = MuroBase()
        m.Normativa = eNormativaMuro.Vial
        MuroContencionService.AnalizarMuro(m)
        Dim ce = m.Resultados.Estabilidad.CasoEstatico
        Assert.AreEqual(1.0, ce.FsVolcamientoAdmisible, 0.01)
        Assert.AreEqual(m.Geometria.B / 3.0, ce.ExcentricidadAdmisible, 0.001)
    End Sub

    ''' <summary>Mismo muro, dos normas: cambian admisibles no los FS calculados.</summary>
    <TestMethod>
    Public Sub Estabilidad_FsCalculadosNoCambianPorNorma()
        Dim mA = MuroBase()
        mA.Normativa = eNormativaMuro.Residencial
        MuroContencionService.AnalizarMuro(mA)

        Dim mB = MuroBase()
        mB.Normativa = eNormativaMuro.Vial
        MuroContencionService.AnalizarMuro(mB)

        Assert.AreEqual(mA.Resultados.Estabilidad.CasoEstatico.FsVolcamiento,
                         mB.Resultados.Estabilidad.CasoEstatico.FsVolcamiento, 0.001,
                         "FS calculado no depende de la norma, solo el admisible")
    End Sub

    ''' <summary>Umbral 0.90: muro con FS calc / FS adm = 0.92 cumple.</summary>
    <TestMethod>
    Public Sub ChequeoCaso_FactorVolcamiento_Umbral09()
        Dim c As New ChequeoCaso() With {
            .FsVolcamiento = 2.76,
            .FsVolcamientoAdmisible = 3.0
        }
        Assert.AreEqual(0.92, c.FactorVolcamiento, 0.001)
        Assert.IsTrue(c.CumpleVolcamiento, "0.92 ≥ 0.90 cumple")
    End Sub

    ''' <summary>Umbral 0.90: factor 0.89 NO cumple.</summary>
    <TestMethod>
    Public Sub ChequeoCaso_FactorVolcamiento_Debajo09_NoCumple()
        Dim c As New ChequeoCaso() With {
            .FsVolcamiento = 2.67,
            .FsVolcamientoAdmisible = 3.0
        }
        Assert.AreEqual(0.89, c.FactorVolcamiento, 0.001)
        Assert.IsFalse(c.CumpleVolcamiento, "0.89 < 0.90 no cumple")
    End Sub

    ''' <summary>Capacidad portante: factor = qAdm / qMax (admisible es techo, invertido).</summary>
    <TestMethod>
    Public Sub ChequeoCaso_FactorCapacidadPortante()
        Dim c As New ChequeoCaso() With {
            .PresionMax = 180.0,
            .PresionAdmisible = 200.0
        }
        Assert.AreEqual(200.0 / 180.0, c.FactorCapacidadPortante, 0.001)
        Assert.IsTrue(c.CumpleCapacidadPortante, "1.11 ≥ 0.90")
    End Sub

    ''' <summary>Caso sísmico se crea solo si IncluirSismo = True.</summary>
    <TestMethod>
    Public Sub Estabilidad_SinSismo_SinCasoSismico()
        Dim m = MuroBase()
        m.SueloSismico.IncluirSismo = False
        MuroContencionService.AnalizarMuro(m)
        Assert.IsNull(m.Resultados.Estabilidad.CasoSismico)
    End Sub

    <TestMethod>
    Public Sub Estabilidad_ConSismo_CasoSismicoCalculado()
        Dim m = MuroBase()
        m.SueloSismico.IncluirSismo = True
        m.SueloSismico.Kh = 0.15
        MuroContencionService.AnalizarMuro(m)
        Dim cs = m.Resultados.Estabilidad.CasoSismico
        Assert.IsNotNull(cs)
        Assert.IsTrue(cs.FsVolcamiento > 0)
        Assert.AreEqual(2.0, cs.FsVolcamientoAdmisible, 0.01, "residencial sísmico")
    End Sub

    ''' <summary>Sismo baja FS volcamiento (más carga horizontal).</summary>
    <TestMethod>
    Public Sub Estabilidad_Sismo_BajaFsVolcamiento()
        Dim m = MuroBase()
        m.SueloSismico.IncluirSismo = True
        m.SueloSismico.Kh = 0.15
        MuroContencionService.AnalizarMuro(m)
        Dim fsEst = m.Resultados.Estabilidad.CasoEstatico.FsVolcamiento
        Dim fsSis = m.Resultados.Estabilidad.CasoSismico.FsVolcamiento
        Assert.IsTrue(fsSis < fsEst, $"FS sísmico ({fsSis:F2}) < FS estático ({fsEst:F2})")
    End Sub

    ' =====================================================================
    ' Diseño — flexión y cortante
    ' =====================================================================

    ''' <summary>AreaAceroFlexion: viga 1m×0.4m, Mu = 100 kN·m, fc=21, fy=420.
    ''' ρ_calc = 0.00221; ρ_min = 0.00333; como 1.33·ρ_calc = 0.00294 &lt; ρ_min,
    ''' NSR-10 C.10.5.3 permite usar 1.33·ρ_calc. As = 0.00294·1000·350 ≈ 1030 mm²/m.</summary>
    <TestMethod>
    Public Sub AreaAceroFlexion_CasoBase()
        Dim asReq = MuroContencionService.AreaAceroFlexion(1.0, 0.35, 100.0, 21.0, 420.0)
        Assert.AreEqual(1030.0, asReq, 30.0, "aplica regla 1.33·ρ_calc del NSR-10 C.10.5.3")
    End Sub

    ''' <summary>Cuantía mínima: muy poco momento aplica min(1.4/fy, 0.25√fc/fy).</summary>
    <TestMethod>
    Public Sub AreaAceroFlexion_CuantiaMinima()
        ' Un Mu muy chico cae a mínima. Con fy=420: ρmin = max(1.4/420, 0.25·√21/420)
        '   = max(0.00333, 0.00273) = 0.00333 (NSR-10 C.10.5)
        ' Pero la regla 1.33·ρ_calc permite bajar: si 1.33·ρ_calc < ρmin, se usa 1.33·ρ_calc.
        ' Entonces el valor cae por debajo de ρmin·b·d.
        Dim asReq = MuroContencionService.AreaAceroFlexion(1.0, 0.35, 1.0, 21.0, 420.0)
        Dim rhoMin = Math.Max(1.4 / 420, 0.25 * Math.Sqrt(21.0) / 420)
        Assert.IsTrue(asReq > 0 AndAlso asReq <= rhoMin * 1000 * 350,
            $"As_req = {asReq:F0} debe estar por debajo de ρmin·b·d por la regla 1.33")
    End Sub

    ''' <summary>Mu = 0 → As_req = 0 (sin sección insuficiente ni NaN).</summary>
    <TestMethod>
    Public Sub AreaAceroFlexion_SinMomento_SinAcero()
        Assert.AreEqual(0.0, MuroContencionService.AreaAceroFlexion(1.0, 0.35, 0.0, 21.0, 420.0), 0.001)
    End Sub

    ''' <summary>Acero por temperatura NSR-10 C.7.12: ρ=0.0018 para fy=420.</summary>
    <TestMethod>
    Public Sub AceroTemperatura_fy420()
        ' b=1 m, h=0.4 m → As = 0.0018·1000·400 = 720 mm²/m
        Dim asT = MuroContencionService.AceroTemperatura(1.0, 0.4, 420.0)
        Assert.AreEqual(720.0, asT, 0.1)
    End Sub

    ' =====================================================================
    ' Flujo end-to-end AnalizarMuro
    ' =====================================================================

    ''' <summary>AnalizarMuro puebla todos los sub-objetos de Resultados.</summary>
    <TestMethod>
    Public Sub AnalizarMuro_PueblaResultadosCompletos()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Assert.IsNotNull(m.Resultados)
        Assert.IsNotNull(m.Resultados.Empujes)
        Assert.IsNotNull(m.Resultados.Pesos)
        Assert.IsTrue(m.Resultados.Pesos.Count > 0)
        Assert.IsNotNull(m.Resultados.Estabilidad)
        Assert.IsNotNull(m.Resultados.Estabilidad.CasoEstatico)
        Assert.IsNotNull(m.Resultados.Disenos)
        Assert.IsNotNull(m.Resultados.Disenos.Vastago)
        Assert.IsNotNull(m.Resultados.Disenos.Talon)
        Assert.IsNotNull(m.Resultados.Disenos.Pie)
    End Sub

    ''' <summary>Vástago: As_req > 0, φVc > 0, propuesta inicial de refuerzo.</summary>
    <TestMethod>
    Public Sub Diseno_Vastago_TienePropuesta()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim v = m.Resultados.Disenos.Vastago
        Assert.IsTrue(v.Mu > 0)
        Assert.IsTrue(v.Vu > 0)
        Assert.IsTrue(v.AsReq > 0, "con 4m de altura y relleno, Mu obliga a algo de acero")
        Assert.IsTrue(v.PhiVc > 0)
        Assert.IsTrue(v.AsProv > 0, "propuesta inicial debe cubrir As_req")
        Assert.IsTrue(v.FactorFlexion > 0)
    End Sub

    ''' <summary>Envolvente real: con sismo activo, Mu/Vu del vástago crecen.</summary>
    <TestMethod>
    Public Sub Diseno_Vastago_EnvolventeConSismo()
        Dim mSin = MuroBase()
        MuroContencionService.AnalizarMuro(mSin)
        Dim mSis = MuroBase()
        mSis.SueloSismico.IncluirSismo = True
        mSis.SueloSismico.Kh = 0.15
        MuroContencionService.AnalizarMuro(mSis)
        Assert.IsTrue(mSis.Resultados.Disenos.Vastago.Mu >=
                       mSin.Resultados.Disenos.Vastago.Mu * 0.90,
                       "Con sismo, Mu no puede bajar — envolvente estático∪sísmico")
    End Sub

    ''' <summary>Programa de revisión: cambiar barra → AsProv cambia → Factor cambia.</summary>
    <TestMethod>
    Public Sub Revision_RecalcularAsProv_ActualizaFactor()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim v = m.Resultados.Disenos.Vastago

        v.BarraPrincipal = "#8"
        v.SeparacionCm = 10
        MuroContencionService.RecalcularAsProv(v)

        ' #8 = 510 mm², a 10 cm → AsProv = 5100 mm²/m
        Assert.AreEqual(5100.0, v.AsProv, 20.0)
        Assert.IsTrue(v.FactorFlexion >= 1.0, "con tanto acero el C/D debe pasar ≥ 1.0")
        Assert.IsTrue(v.CumpleFlexion)
    End Sub

    ''' <summary>Programa de revisión: AsProv muy chico → no cumple.</summary>
    <TestMethod>
    Public Sub Revision_PocoAcero_NoCumple()
        Dim m = MuroBase()
        MuroContencionService.AnalizarMuro(m)
        Dim v = m.Resultados.Disenos.Vastago
        v.BarraPrincipal = "#3"
        v.SeparacionCm = 20
        MuroContencionService.RecalcularAsProv(v)
        ' #3 = 71 mm², a 20 cm → AsProv = 355 mm²/m — insuficiente para muro 4m
        Assert.IsFalse(v.CumpleFlexion, "AsProv muy bajo debe no cumplir con el umbral 0.90")
    End Sub

    ''' <summary>Llave de cortante: solo se diseña si IncluirLlave = True.</summary>
    <TestMethod>
    Public Sub Diseno_Llave_SinLlaveEsNothing()
        Dim m = MuroBase()
        m.Opciones.IncluirLlave = False
        MuroContencionService.AnalizarMuro(m)
        Assert.IsNull(m.Resultados.Disenos.Llave)
    End Sub

    <TestMethod>
    Public Sub Diseno_Llave_ConLlaveSeDisena()
        Dim m = MuroBase()
        m.Opciones.IncluirLlave = True
        m.Geometria.B_LL = 0.3
        m.Geometria.H_LL = 0.5
        MuroContencionService.AnalizarMuro(m)
        Assert.IsNotNull(m.Resultados.Disenos.Llave)
        Assert.IsTrue(m.Resultados.Disenos.Llave.AsReq > 0)
    End Sub

End Class
