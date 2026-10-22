Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 13 — MC. Cortina · tests del solver (método de rigidez).
'
'  Verifica contra soluciones analíticas clásicas de vigas:
'   - Viga simplemente apoyada con carga uniforme (M_max = wL²/8).
'   - Viga empotrada-apoyada (M_emp = -wL²/8 teórico, M_vano = 9wL²/128).
'   - Viga empotrada-libre (cantilever) con carga uniforme (M_emp = -wL²/2).
'   - Dos tramos iguales continuos con carga uniforme (M_apoyo_central = -wL²/8).
'
'  Para "cargar" una viga uniforme en nuestro modelo (que carga triangular por
'  empuje de suelo + sobrecarga + agua) usamos solo la sobrecarga Ka·q =
'  constante y seteamos φ=0 para que no haya empuje de suelo. Con Ka manual
'  = 1.0 y γ = 0, la carga a lo largo del muro es constante Ka·q = q.
'
'  Tolerancia: 2% por errores de muestreo + redondeo trigonométrico.
' ═══════════════════════════════════════════════════════════════════════════════
<TestClass>
Public Class MuroCortinaTests

    ''' <summary>Fabrica un muro con 1 tramo con carga uniforme w = q.</summary>
    Private Shared Function MuroCargaUniforme(L As Double, t As Double, w As Double,
                                                tipoBase As eTipoApoyoCortina,
                                                tipoCorona As eTipoApoyoCortina) As MuroCortina
        Dim m As New MuroCortina()
        m.Tramos.Clear()
        m.Apoyos.Clear()
        m.Tramos.Add(New TramoCortina With {.Nombre = "T1", .Altura = L, .Espesor = t})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "Base", .Nivel = 0, .Tipo = tipoBase})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "P02", .Nivel = L, .Tipo = tipoCorona})
        ' Trick: con γ = 0 y Ka manual = 1.0, la única carga es la sobrecarga Ka·q = q constante.
        m.Suelo.CalcularKaAutomatico = False
        m.Suelo.KaManual = 1.0
        m.Suelo.Gamma = 0
        m.Suelo.GammaSat = 0
        m.Sobrecarga.IncluirSobrecarga = True
        m.Sobrecarga.SobrecargaKPa = w
        m.Materiales.Fc = 21
        ' Residencial estática: fcLS = 1.60. Para que la carga factorizada sea
        ' exactamente w, dividimos: ingresamos w/1.6 como SobrecargaKPa.
        m.Sobrecarga.SobrecargaKPa = w / 1.60
        Return m
    End Function

    ' =====================================================================
    ' Caso 1: Viga simplemente apoyada ambos extremos, carga uniforme w
    '         M_max teórico = wL²/8 en el centro.
    ' =====================================================================
    <TestMethod>
    Public Sub SimpleApoyada_CargaUniforme_MomentoCentral()
        Const w As Double = 10.0   ' kN/m²
        Const L As Double = 4.0    ' m
        Dim m = MuroCargaUniforme(L, 0.30, w,
                                   eTipoApoyoCortina.SimplementeApoyado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)

        Dim mTeorico = w * L * L / 8.0   ' = 20
        Dim mNumerico = m.Resultados.MomentoMaximo
        Assert.AreEqual(mTeorico, mNumerico, mTeorico * 0.02,
            $"M_max teórico = {mTeorico:F2}, numérico = {mNumerico:F2}")
    End Sub

    <TestMethod>
    Public Sub SimpleApoyada_CargaUniforme_Reacciones()
        Const w As Double = 10.0
        Const L As Double = 4.0
        Dim m = MuroCargaUniforme(L, 0.30, w,
                                   eTipoApoyoCortina.SimplementeApoyado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)
        ' R = wL/2 = 20 kN/m en cada apoyo
        Dim rTeorico = w * L / 2.0
        Assert.AreEqual(rTeorico, Math.Abs(m.Resultados.Reacciones(0)), rTeorico * 0.03)
        Assert.AreEqual(rTeorico, Math.Abs(m.Resultados.Reacciones(1)), rTeorico * 0.03)
    End Sub

    ' =====================================================================
    ' Caso 2: Viga empotrada-apoyada, carga uniforme w
    '         M_empotramiento teórico = -wL²/8
    '         M_vano máximo = +9wL²/128 (a 5L/8 del empotramiento)
    ' =====================================================================
    <TestMethod>
    Public Sub EmpotradaApoyada_CargaUniforme_Momentos()
        Const w As Double = 10.0
        Const L As Double = 4.0
        Dim m = MuroCargaUniforme(L, 0.30, w,
                                   eTipoApoyoCortina.Empotrado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)

        Dim mEmpTeorico = w * L * L / 8.0   ' valor absoluto
        ' El M máximo (en valor absoluto) debe estar cerca del empotramiento.
        Dim mNumMax = m.Resultados.MomentoMaximo
        Assert.AreEqual(mEmpTeorico, mNumMax, mEmpTeorico * 0.03,
            $"M_empotramiento teórico = {mEmpTeorico:F2}, numérico = {mNumMax:F2}")
    End Sub

    ' =====================================================================
    ' Caso 3: Viga empotrada-libre (cantilever), carga uniforme
    '         M_empotramiento = wL²/2 (abajo), V_empotramiento = wL
    ' =====================================================================
    <TestMethod>
    Public Sub Cantilever_CargaUniforme_MomentoEmpotramiento()
        Const w As Double = 10.0
        Const L As Double = 3.0
        Dim m = MuroCargaUniforme(L, 0.30, w,
                                   eTipoApoyoCortina.Empotrado,
                                   eTipoApoyoCortina.Libre)
        MuroCortinaService.AnalizarMuro(m)

        Dim mTeorico = w * L * L / 2.0   ' = 45
        Dim mNumerico = m.Resultados.MomentoMaximo
        Assert.AreEqual(mTeorico, mNumerico, mTeorico * 0.02,
            $"Cantilever M_emp teórico = {mTeorico:F2}, numérico = {mNumerico:F2}")

        ' V en el empotramiento = wL
        Dim vTeorico = w * L
        Dim vNumerico = m.Resultados.CortanteMaximo
        Assert.AreEqual(vTeorico, vNumerico, vTeorico * 0.02,
            $"Cantilever V_emp teórico = {vTeorico:F2}, numérico = {vNumerico:F2}")
    End Sub

    ' =====================================================================
    ' Caso 4: Dos tramos iguales continuos, apoyos simples en ambos extremos
    '         y un apoyo simple intermedio.
    '         M_apoyo_central_teórico = -wL²/8 (para cada vano de longitud L).
    ' =====================================================================
    <TestMethod>
    Public Sub DosTramosContinuos_CargaUniforme_MomentoApoyoCentral()
        Const w As Double = 10.0
        Const L As Double = 4.0   ' longitud de CADA vano
        Dim m As New MuroCortina()
        m.Tramos.Clear()
        m.Apoyos.Clear()
        m.Tramos.Add(New TramoCortina With {.Nombre = "T1", .Altura = L, .Espesor = 0.30})
        m.Tramos.Add(New TramoCortina With {.Nombre = "T2", .Altura = L, .Espesor = 0.30})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "Base", .Nivel = 0, .Tipo = eTipoApoyoCortina.SimplementeApoyado})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "P02", .Nivel = L, .Tipo = eTipoApoyoCortina.SimplementeApoyado})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "P03", .Nivel = 2 * L, .Tipo = eTipoApoyoCortina.SimplementeApoyado})
        m.Suelo.CalcularKaAutomatico = False
        m.Suelo.KaManual = 1.0
        m.Suelo.Gamma = 0
        m.Suelo.GammaSat = 0
        m.Sobrecarga.IncluirSobrecarga = True
        m.Sobrecarga.SobrecargaKPa = w / 1.60   ' deshacer el fcLS
        m.Materiales.Fc = 21
        MuroCortinaService.AnalizarMuro(m)

        Dim mTeorico = w * L * L / 8.0   ' = 20
        ' El M máximo del sistema debe estar en el apoyo central = -wL²/8
        Dim mNumerico = m.Resultados.MomentoMaximo
        Assert.AreEqual(mTeorico, mNumerico, mTeorico * 0.03,
            $"2 tramos continuos — M_apoyo_central teórico = {mTeorico:F2}, numérico = {mNumerico:F2}")
    End Sub

    ' =====================================================================
    ' Caso 5: Validación de Ka Rankine con φ = 30° (= 0.333)
    ' =====================================================================
    <TestMethod>
    Public Sub Ka_Rankine_phi30()
        Dim s As New PropsSueloCortina() With {
            .Phi = 30, .CalcularKaAutomatico = True, .Teoria = eTeoriaEmpuje.Rankine
        }
        Dim ka = MuroCortinaService.CalcularKa(s)
        Assert.AreEqual(1.0 / 3.0, ka, 0.005)
    End Sub

    <TestMethod>
    Public Sub Ka_Manual_SeRespeta()
        Dim s As New PropsSueloCortina() With {
            .Phi = 30, .CalcularKaAutomatico = False, .KaManual = 0.5
        }
        Dim ka = MuroCortinaService.CalcularKa(s)
        Assert.AreEqual(0.5, ka, 0.0001)
    End Sub

    ' =====================================================================
    ' Caso 6: Validación — carga de empuje de suelo (triangular) sobre
    '         muro cantilever genera M = Ka·γ·H³/6 en la base.
    ' =====================================================================
    <TestMethod>
    Public Sub Cantilever_EmpujeTierraTriangular_MomentoEmpotramiento()
        ' M_teorico (sin factores) = Ka · γ · H³ / 6
        ' Residencial estática: fcEH = 1.60
        Const H As Double = 3.0
        Const gamma As Double = 18.0
        Const ka As Double = 1.0 / 3.0
        Const fcEH As Double = 1.60

        Dim m As New MuroCortina()
        m.Tramos.Clear()
        m.Apoyos.Clear()
        m.Tramos.Add(New TramoCortina With {.Nombre = "T1", .Altura = H, .Espesor = 0.30})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "Base", .Nivel = 0, .Tipo = eTipoApoyoCortina.Empotrado})
        m.Apoyos.Add(New ApoyoCortina With {.Etiqueta = "P02", .Nivel = H, .Tipo = eTipoApoyoCortina.Libre})
        m.Suelo.CalcularKaAutomatico = False
        m.Suelo.KaManual = ka
        m.Suelo.Gamma = gamma
        m.Suelo.Phi = 30
        m.Materiales.Fc = 21
        MuroCortinaService.AnalizarMuro(m)

        Dim mTeorico = ka * gamma * H * H * H / 6.0 * fcEH   ' con fcEH ya incluido
        Dim mNumerico = m.Resultados.MomentoMaximo
        Assert.AreEqual(mTeorico, mNumerico, mTeorico * 0.03,
            $"M teórico (Ka·γ·H³/6)·fcEH = {mTeorico:F2}, numérico = {mNumerico:F2}")
    End Sub

    ' =====================================================================
    ' Caso 7: Validación de errores
    ' =====================================================================
    <TestMethod>
    <ExpectedException(GetType(InvalidOperationException))>
    Public Sub MuroInestable_UnSoloApoyoSimple_Falla()
        Dim m As New MuroCortina()
        m.Tramos.Clear()
        m.Apoyos.Clear()
        m.Tramos.Add(New TramoCortina With {.Altura = 2, .Espesor = 0.2})
        m.Apoyos.Add(New ApoyoCortina With {.Tipo = eTipoApoyoCortina.SimplementeApoyado})
        m.Apoyos.Add(New ApoyoCortina With {.Tipo = eTipoApoyoCortina.Libre})
        MuroCortinaService.AnalizarMuro(m)
    End Sub

    <TestMethod>
    Public Sub Analisis_PueblaResultados()
        Dim m = MuroCargaUniforme(3.0, 0.25, 10,
                                   eTipoApoyoCortina.Empotrado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)
        Assert.IsNotNull(m.Resultados)
        Assert.IsTrue(m.Resultados.DiagramaM.Count > 0)
        Assert.IsTrue(m.Resultados.DiagramaV.Count > 0)
        Assert.AreEqual(2, m.Resultados.Reacciones.Count)
        Assert.AreEqual(1, m.Resultados.DisenosTramos.Count)
        ' Empotrada-apoyada: hay M+ en el empotramiento y M- en el vano → doble capa por análisis.
        Dim dto = m.Resultados.DisenosTramos(0)
        Assert.IsTrue(dto.DobleCapaPorAnalisis, "empotrada-apoyada debe exigir doble capa por análisis")
        Assert.IsTrue(dto.CaraPositiva.AsReq > 0 OrElse dto.CaraNegativa.AsReq > 0,
            "al menos una cara debe tener As requerido")
    End Sub

    ' =====================================================================
    ' Doble capa (M+ y M-)
    ' =====================================================================

    <TestMethod>
    Public Sub SimpleApoyada_SoloMenos_NoRequiereDobleCapaPorAnalisis()
        ' Simple-apoyada con carga uniforme: M siempre ≤ 0 → solo cara interior.
        Dim m = MuroCargaUniforme(4, 0.20, 10,
                                   eTipoApoyoCortina.SimplementeApoyado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)
        Dim dto = m.Resultados.DisenosTramos(0)
        Assert.IsFalse(dto.DobleCapaPorAnalisis, "simple-simple no requiere doble capa por análisis")
        Assert.IsTrue(dto.CaraNegativa.AsReq > 0, "cara interior sí tiene acero (M-)")
        Assert.AreEqual(0, dto.CaraPositiva.AsReq, 0.1, "cara exterior sin momento → sin acero")
    End Sub

    <TestMethod>
    Public Sub Espesor25cm_RequiereDobleCapaPorNorma()
        Dim m = MuroCargaUniforme(4, 0.25, 10,
                                   eTipoApoyoCortina.SimplementeApoyado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)
        Dim dto = m.Resultados.DisenosTramos(0)
        Assert.IsTrue(dto.DobleCapaPorNorma, "t=0.25 m → doble capa por NSR-10 C.14.3.4")
        Assert.IsTrue(dto.RequiereDosCapas)
    End Sub

    <TestMethod>
    Public Sub Espesor20cm_NoRequiereDobleCapaSalvoQueElUsuarioLaFuerce()
        Dim m = MuroCargaUniforme(4, 0.20, 10,
                                   eTipoApoyoCortina.SimplementeApoyado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)
        Dim dto = m.Resultados.DisenosTramos(0)
        Assert.IsFalse(dto.DobleCapaPorNorma)
        Assert.IsFalse(dto.DobleCapaPorAnalisis)
        Assert.IsFalse(dto.RequiereDosCapas)
        ' Si el usuario fuerza: la propiedad RequiereDosCapas pasa a True.
        dto.DobleCapaForzada = True
        Assert.IsTrue(dto.RequiereDosCapas)
        Assert.AreEqual("forzada", dto.RazonDobleCapa)
    End Sub

    <TestMethod>
    Public Sub EmpotradaApoyada_RequiereDobleCapaPorAnalisis()
        ' M+ en el empotramiento (wL²/8) y M- en el vano (9wL²/128).
        Dim m = MuroCargaUniforme(4, 0.20, 10,
                                   eTipoApoyoCortina.Empotrado,
                                   eTipoApoyoCortina.SimplementeApoyado)
        MuroCortinaService.AnalizarMuro(m)
        Dim dto = m.Resultados.DisenosTramos(0)
        Assert.IsTrue(dto.DobleCapaPorAnalisis, "empotrada-apoyada debe pedir doble capa por análisis")
        Assert.IsTrue(dto.CaraPositiva.AsReq > 0, "cara exterior resiste el M+ del empotramiento")
        Assert.IsTrue(dto.CaraNegativa.AsReq > 0, "cara interior resiste el M- del vano")
    End Sub

End Class
