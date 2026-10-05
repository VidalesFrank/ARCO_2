Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Los cinco chequeos de esfuerzos de Pilas, y sobre todo sus SIGNOS.
'''
''' Convenio del módulo: FZ de Joint Reactions es positivo en COMPRESIÓN (al
''' leer "Pier Forces" se invierte el P de ETABS justamente para eso). De ahí
''' que los chequeos 1 a 4 tomen Max(FZ) y el 5, el de tracción, tome Min(FZ).
'''
''' Hasta 2026-10-05 el Chequeo 5 dividía por el valor con signo:
'''     Check5 = 0.9 x fy x As x n / (P_Traccion x 1000)
''' Con P_Traccion negativo —que es el único caso en que el chequeo existe— el
''' C/D salía NEGATIVO, y la celda se pintaba de rojo siempre, sin importar
''' cuánto acero hubiera. Y cuando no se habían elegido combinaciones de
''' tracción, P_Traccion era 0, la división daba Infinity, la celda se pintaba
''' de VERDE y la exportación a Excel abortaba (ClosedXML rechaza Infinity).
''' Estas pruebas fijan las dos puntas.
'''
''' Pila de referencia: Df = 1.00 m (Ag = 0.785398 m2), fc = 21 MPa,
''' 8 barras #8 (509.7 mm2 cada una), fy = 420 MPa.
''' </summary>
<TestClass>
Public Class PilaServiceTests

    Private Const AG As Double = 0.7853982      ' pi x 1.00^2 / 4  [m2]
    Private Const FC As Double = 21.0
    Private Const FY As Double = 420.0
    Private Const AREA_8 As Double = 509.7      ' mm2
    Private Const N_BARRAS As Double = 8

    ' phi x fy x As_total = 0.9 x 420 x 509.7 x 8 = 1 541 332.8 N = 1541.33 kN
    Private Const PHI_TN_KN As Double = 1541.3328

    ' =====================================================================
    ' Chequeo 5 — TRACCIÓN
    ' =====================================================================

    ''' <summary>
    ''' El bug: con tracción real el C/D tiene que ser POSITIVO. Antes salía
    ''' -1.93 y el semáforo lo leía como "no cumple".
    ''' </summary>
    <TestMethod>
    Public Sub ChequeoTraccion_ConFZNegativoDevuelveCDPositivo()
        Dim cd = PilaService.ChequeoTraccion(-800.0, FY, AREA_8, N_BARRAS)
        Assert.IsTrue(cd > 0, "el C/D de tracción nunca puede ser negativo: " & cd.ToString())
        Assert.AreEqual(PHI_TN_KN / 800.0, cd, 0.001)
    End Sub

    ''' <summary>
    ''' FZ positivo es compresión: la pila no se levanta y el chequeo no aplica.
    ''' 0 es la sentinela de "no aplica" que ya leían el dashboard y el reporte
    ''' de revisión.
    ''' </summary>
    <TestMethod>
    Public Sub ChequeoTraccion_SinTraccionNoAplica()
        Assert.AreEqual(0.0F, PilaService.ChequeoTraccion(500.0, FY, AREA_8, N_BARRAS), "todo en compresión")
        Assert.AreEqual(0.0F, PilaService.ChequeoTraccion(0.0, FY, AREA_8, N_BARRAS), "sin combinaciones de tracción")
    End Sub

    ''' <summary>Sin combinaciones de tracción no hay Infinity: ClosedXML lo rechazaba.</summary>
    <TestMethod>
    Public Sub ChequeoTraccion_NuncaDevuelveInfinito()
        Dim cd = PilaService.ChequeoTraccion(0.0, FY, AREA_8, N_BARRAS)
        Assert.IsFalse(Single.IsInfinity(cd), "Infinity abortaba la exportación a Excel")
        Assert.IsFalse(Single.IsNaN(cd))
    End Sub

    ''' <summary>A más tracción, menos C/D. Con 2000 kN de tirón no alcanza.</summary>
    <TestMethod>
    Public Sub ChequeoTraccion_MasTraccionMenosCD()
        Dim cdFlojo = PilaService.ChequeoTraccion(-800.0, FY, AREA_8, N_BARRAS)
        Dim cdDuro = PilaService.ChequeoTraccion(-2000.0, FY, AREA_8, N_BARRAS)
        Assert.IsTrue(cdDuro < cdFlojo)
        Assert.AreEqual(PHI_TN_KN / 2000.0, cdDuro, 0.001)
        Assert.IsTrue(cdDuro < Funciones_00_Varias.UMBRAL_CD, "0.77 no cumple")
    End Sub

    ''' <summary>Sin acero declarado no se inventa capacidad.</summary>
    <TestMethod>
    Public Sub ChequeoTraccion_SinAceroNoAplica()
        Assert.AreEqual(0.0F, PilaService.ChequeoTraccion(-800.0, FY, 0.0, N_BARRAS), "sin área de barra")
        Assert.AreEqual(0.0F, PilaService.ChequeoTraccion(-800.0, FY, AREA_8, 0.0), "sin barras")
        Assert.AreEqual(0.0F, PilaService.ChequeoTraccion(-800.0, 0.0, AREA_8, N_BARRAS), "sin fy")
    End Sub

    ''' <summary>phi = 0.90 (NSR-10 C.9.3.2.2). El 10 % tiene que estar descontado.</summary>
    <TestMethod>
    Public Sub ChequeoTraccion_AplicaPhiDeNoventa()
        Dim cd = PilaService.ChequeoTraccion(-PHI_TN_KN, FY, AREA_8, N_BARRAS)
        Assert.AreEqual(1.0, cd, 0.001, "la demanda igual a phi x Tn debe dar C/D = 1.00")
        Dim sinPhi = FY * AREA_8 * N_BARRAS / 1000.0 / PHI_TN_KN
        Assert.IsTrue(cd < sinPhi, "sin phi el C/D sería 1.11")
    End Sub

    ' =====================================================================
    ' Chequeos 1 a 4 — COMPRESIÓN
    ' =====================================================================

    ''' <summary>0.25 x 21 x 0.785398 x 1000 / 2000 = 2.0617</summary>
    <TestMethod>
    Public Sub ChequeoCompresion_CasoDeReferencia()
        Assert.AreEqual(2.0617, PilaService.ChequeoCompresion(0.25, FC, AG, 2000.0), 0.001, "Ps estática")
        Assert.AreEqual(2.7215, PilaService.ChequeoCompresion(0.33, FC, AG, 2000.0), 0.001, "Ps dinámica")
        Assert.AreEqual(2.8864, PilaService.ChequeoCompresion(0.35, FC, AG, 2000.0), 0.001, "Pu")
    End Sub

    ''' <summary>
    ''' Sin combinaciones de esa familia, Max(FZ) devuelve 0 y el chequeo
    ''' dividía por cero: Infinity, celda verde y Excel roto.
    ''' </summary>
    <TestMethod>
    Public Sub ChequeoCompresion_SinDemandaNoAplica()
        Dim cd = PilaService.ChequeoCompresion(0.25, FC, AG, 0.0)
        Assert.AreEqual(0.0F, cd)
        Assert.IsFalse(Single.IsInfinity(cd))
    End Sub

    ''' <summary>
    ''' Un Max(FZ) negativo significa que la pila está en tracción en TODAS las
    ''' combinaciones de esa familia: no hay compresión que chequear. Eso lo
    ''' mira el Chequeo 5, no el 1.
    ''' </summary>
    <TestMethod>
    Public Sub ChequeoCompresion_DemandaEnTraccionNoAplica()
        Assert.AreEqual(0.0F, PilaService.ChequeoCompresion(0.25, FC, AG, -500.0))
    End Sub

    ''' <summary>Sección sin definir: no se afirma nada.</summary>
    <TestMethod>
    Public Sub ChequeoCompresion_SinSeccionNoAplica()
        Assert.AreEqual(0.0F, PilaService.ChequeoCompresion(0.25, 0.0, AG, 2000.0), "sin fc")
        Assert.AreEqual(0.0F, PilaService.ChequeoCompresion(0.25, FC, 0.0, 2000.0), "sin Ag")
    End Sub

    ' =====================================================================
    ' Envolvente de los cinco y veredicto
    ' =====================================================================

    Private Shared Function Pila(c1 As Single, c2 As Single, c3 As Single, c4 As Single, c5 As Single) As Elemento_Pila
        Return New Elemento_Pila With {
            .Name_Elemento = "P-1",
            .Check1_PsE = c1, .Check2_PsD = c2, .Check3_PuE = c3, .Check4_PuD = c4, .Check5_PuT = c5
        }
    End Function

    <TestMethod>
    Public Sub MinChequeoEsfuerzos_IgnoraLosQueNoAplican()
        ' Solo hay dos chequeos calculados: gobierna el menor de esos dos.
        Assert.AreEqual(1.4, PilaService.MinChequeoEsfuerzos(Pila(2.0F, 1.4F, 0.0F, 0.0F, 0.0F)), 0.0001)
    End Sub

    <TestMethod>
    Public Sub MinChequeoEsfuerzos_LaTraccionPuedeGobernar()
        Assert.AreEqual(0.77, PilaService.MinChequeoEsfuerzos(Pila(2.0F, 2.1F, 1.8F, 1.8F, 0.77F)), 0.0001)
    End Sub

    ''' <summary>
    ''' Proyectos .esm guardados antes del arreglo tienen Check5 = Infinity
    ''' serializado. No debe contaminar la envolvente.
    ''' </summary>
    <TestMethod>
    Public Sub MinChequeoEsfuerzos_DescartaInfinitosGuardados()
        Dim p = Pila(1.5F, 2.0F, 1.8F, 1.8F, Single.PositiveInfinity)
        Assert.AreEqual(1.5, PilaService.MinChequeoEsfuerzos(p), 0.0001)
    End Sub

    <TestMethod>
    Public Sub MinChequeoEsfuerzos_SinNingunCalculoEsCero()
        Assert.AreEqual(0.0, PilaService.MinChequeoEsfuerzos(Pila(0.0F, 0.0F, 0.0F, 0.0F, 0.0F)), 0.0001)
        Assert.AreEqual(0.0, PilaService.MinChequeoEsfuerzos(Nothing), 0.0001)
    End Sub

    ''' <summary>
    ''' El veredicto "Cargas" del resumen miraba solo los chequeos 1 a 4: una
    ''' pila que se levanta y no tiene acero para el tirón decía "Ok".
    ''' </summary>
    <TestMethod>
    Public Sub CumpleEsfuerzos_LaTraccionReprueba()
        Assert.IsFalse(PilaService.CumpleEsfuerzos(Pila(2.0F, 2.1F, 1.8F, 1.8F, 0.77F)))
        Assert.IsTrue(PilaService.CumpleEsfuerzos(Pila(2.0F, 2.1F, 1.8F, 1.8F, 1.93F)))
    End Sub

    ''' <summary>Sin tracción el chequeo no vota, y la pila puede cumplir.</summary>
    <TestMethod>
    Public Sub CumpleEsfuerzos_SinTraccionNoEstorba()
        Assert.IsTrue(PilaService.CumpleEsfuerzos(Pila(2.0F, 2.1F, 1.8F, 1.8F, 0.0F)))
    End Sub

    <TestMethod>
    Public Sub CumpleEsfuerzos_SinCalculoEsRevisar()
        Assert.IsFalse(PilaService.CumpleEsfuerzos(Pila(0.0F, 0.0F, 0.0F, 0.0F, 0.0F)),
                       "no se puede afirmar que cumple algo que no se calculó")
    End Sub

    ''' <summary>El umbral es el único del programa (0.90), no 1.00.</summary>
    <TestMethod>
    Public Sub CumpleEsfuerzos_UsaElUmbralUnico()
        Assert.IsTrue(PilaService.CumpleEsfuerzos(Pila(0.9F, 1.0F, 1.0F, 1.0F, 0.0F)), "0.90 cumple")
        Assert.IsFalse(PilaService.CumpleEsfuerzos(Pila(0.89F, 1.0F, 1.0F, 1.0F, 0.0F)), "0.89 no")
    End Sub

    ' =====================================================================
    ' De dónde sale cada demanda — Max(FZ) vs Min(FZ)
    ' =====================================================================

    Private Shared Function Reaccion(combo As String, fz As Single) As cCombinacionPila
        Return New cCombinacionPila With {.JointLabel = "12", .LoadCase = combo, .FZ = fz}
    End Function

    ''' <summary>
    ''' La compresión es el máximo y la tracción el mínimo, porque FZ positivo
    ''' es compresión. Si algún día se invierte el convenio al importar, esta
    ''' prueba cae antes que el Chequeo 5.
    ''' </summary>
    <TestMethod>
    Public Sub ExtraccionFZ_MaximoEsCompresionMinimoEsTraccion()
        Dim reacciones As New List(Of cCombinacionPila) From {
            Reaccion("C1", 1200.0F), Reaccion("C2", 300.0F), Reaccion("C3", -450.0F)
        }
        Dim combos As New List(Of String) From {"C1", "C2", "C3"}

        Assert.AreEqual(1200.0F,
            Funciones_00_Varias.ObtenerFZMaximoPorElemento(reacciones, combos, "12", False, False),
            "compresión máxima")
        Assert.AreEqual(-450.0F,
            Funciones_00_Varias.ObtenerFZMaximoPorElemento(reacciones, combos, "12", True, False),
            "tracción máxima, con el signo de ETABS")
    End Sub

    ''' <summary>Sin combinaciones elegidas devuelve 0, que es "no aplica".</summary>
    <TestMethod>
    Public Sub ExtraccionFZ_SinCombinacionesDevuelveCero()
        Dim reacciones As New List(Of cCombinacionPila) From {Reaccion("C1", 1200.0F)}
        Dim ninguna As New List(Of String)
        Assert.AreEqual(0.0F,
            Funciones_00_Varias.ObtenerFZMaximoPorElemento(reacciones, ninguna, "12", True, False))
    End Sub

End Class
