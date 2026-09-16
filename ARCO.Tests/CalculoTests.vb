Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Reflection
Imports ARCO

''' <summary>
''' Pruebas de las funciones de cálculo puras de ARCO.
'''
''' Qué se prueba aquí: funciones sin estado ni UI, que son las que un error
''' silencioso vuelve caro — áreas de barra, normalización de combinaciones
''' ETABS, momento nominal, el Vc de C.21.5.4.2 y la regla de rescate por
''' cortante plástico.
'''
''' Varias son Private en su clase. Se llegan por reflexión a propósito: es
''' preferible probarlas que ampliar su visibilidad solo para el test.
''' </summary>
<TestClass>
Public Class CalculoTests

    Private Const TOL As Double = 0.001

    ' =====================================================================
    ' Áreas de barra
    ' =====================================================================

    ''' <summary>
    ''' Fija los valores que el programa usa HOY. Ojo: no coinciden con los
    ''' que documentaba CLAUDE.md (#4 = 129.4, #10 = 817.4). El código usa
    ''' enteros redondeados, y en #8 y #10 redondea hacia ARRIBA, es decir
    ''' sobreestima el acero colocado (#10: 819 vs 817.4, +0.20 %).
    ''' Si algún día se corrigen los valores, este test debe fallar y
    ''' actualizarse a conciencia, no en automático.
    ''' </summary>
    <TestMethod>
    Public Sub AreaRefuerzo_ValoresVigentes()
        Assert.AreEqual(32.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#2")), TOL, "#2")
        Assert.AreEqual(71.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#3")), TOL, "#3")
        Assert.AreEqual(129.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#4")), TOL, "#4")
        Assert.AreEqual(199.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#5")), TOL, "#5")
        Assert.AreEqual(284.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#6")), TOL, "#6")
        Assert.AreEqual(387.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#7")), TOL, "#7")
        Assert.AreEqual(510.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#8")), TOL, "#8")
        Assert.AreEqual(819.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#10")), TOL, "#10")
    End Sub

    ''' <summary>Una barra desconocida devuelve 0, no lanza excepción.</summary>
    <TestMethod>
    Public Sub AreaRefuerzo_BarraDesconocida_DevuelveCero()
        Assert.AreEqual(0.0, CDbl(Funciones_00_Varias.AreaRefuerzo("None")), TOL)
        Assert.AreEqual(0.0, CDbl(Funciones_00_Varias.AreaRefuerzo("#99")), TOL)
        Assert.AreEqual(0.0, CDbl(Funciones_00_Varias.AreaRefuerzo("")), TOL)
    End Sub

    ''' <summary>
    ''' Las áreas tienen que ser estrictamente crecientes con el número de
    ''' barra. Un error de tecleo que ponga #6 por debajo de #5 pasaría
    ''' inadvertido en el test anterior si alguien actualiza ambos valores.
    ''' </summary>
    <TestMethod>
    Public Sub AreaRefuerzo_EsCrecienteConElNumeroDeBarra()
        Dim barras = New String() {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}
        For i = 1 To barras.Length - 1
            Dim previa = CDbl(Funciones_00_Varias.AreaRefuerzo(barras(i - 1)))
            Dim actual = CDbl(Funciones_00_Varias.AreaRefuerzo(barras(i)))
            Assert.IsTrue(actual > previa,
                          $"{barras(i)} ({actual}) debería ser mayor que {barras(i - 1)} ({previa})")
        Next
    End Sub

    ' =====================================================================
    ' Normalización de claves de combinación (E17 vs E23)
    ' =====================================================================

    <TestMethod>
    Public Sub NormalizarClaveCombo_ConvierteFormatoE17()
        Assert.AreEqual("Envolvente (Max)", Funciones_00_Varias.NormalizarClaveCombo("Envolvente Max"))
        Assert.AreEqual("Envolvente (Min)", Funciones_00_Varias.NormalizarClaveCombo("Envolvente Min"))
        ' Sin distinguir mayúsculas y recortando espacios sobrantes
        Assert.AreEqual("ENV (Max)", Funciones_00_Varias.NormalizarClaveCombo("  ENV MAX  "))
    End Sub

    <TestMethod>
    Public Sub NormalizarClaveCombo_DejaIntactoLoQueYaEstaBien()
        Assert.AreEqual("Envolvente (Max)", Funciones_00_Varias.NormalizarClaveCombo("Envolvente (Max)"))
        Assert.AreEqual("1.2D+1.6L", Funciones_00_Varias.NormalizarClaveCombo("1.2D+1.6L"))
    End Sub

    <TestMethod>
    Public Sub NormalizarClaveCombo_ToleraNuloYVacio()
        Assert.IsNull(Funciones_00_Varias.NormalizarClaveCombo(Nothing))
        Assert.AreEqual("", Funciones_00_Varias.NormalizarClaveCombo(""))
    End Sub

    ' =====================================================================
    ' Momento nominal (NSR-10, bloque rectangular ACI)
    ' =====================================================================

    ''' <summary>
    ''' Mn = As·fy_dis·(d − a/2)/10⁶ con a = As·fy_dis/(0.85·f'c·b).
    ''' Caso verificado a mano: As = 1136 mm² (4#6), fy_dis = 525 MPa (420×1.25,
    ''' nivel DES), f'c = 21 MPa, b = 300 mm, d = 440 mm
    '''   a  = 111.3725 mm
    '''   Mn = 229.2047 kN·m
    ''' </summary>
    <TestMethod>
    Public Sub CalcularMnNominal_CasoVerificadoAMano()
        Dim mn = InvocarMnNominal(1136.0, 525.0, 21.0, 300.0, 440.0)
        Assert.AreEqual(229.2047, mn, 0.001)
    End Sub

    <TestMethod>
    Public Sub CalcularMnNominal_SinAceroOSinPeralte_DevuelveCero()
        Assert.AreEqual(0.0, InvocarMnNominal(0.0, 525.0, 21.0, 300.0, 440.0), TOL, "As = 0")
        Assert.AreEqual(0.0, InvocarMnNominal(1136.0, 525.0, 21.0, 300.0, 0.0), TOL, "d = 0")
    End Sub

    ''' <summary>Más acero nunca puede dar menos momento.</summary>
    <TestMethod>
    Public Sub CalcularMnNominal_CreceConElAcero()
        Dim anterior As Double = -1
        For Each As_mm2 In New Double() {284.0, 568.0, 852.0, 1136.0, 1420.0}
            Dim mn = InvocarMnNominal(As_mm2, 525.0, 21.0, 300.0, 440.0)
            Assert.IsTrue(mn > anterior, $"Mn no creció al pasar a As = {As_mm2}")
            anterior = mn
        Next
    End Sub

    ' =====================================================================
    ' Vc dentro de la zona de rótula — NSR-10 C.21.5.4.2
    ' =====================================================================
    ' Vc = 0 dentro de 2h desde la cara del apoyo cuando se cumplen A LA VEZ:
    '   (a) Ve >= 0.5·Vu   y   (b) Pu < Ag·f'c/20
    ' Fuera de 2h, Vc se conserva íntegro.

    <TestMethod>
    Public Sub VcEfectivo_FueraDeDosH_ConservaVc()
        ' h = 0.50 → 2h = 1.00 m; el punto está a 1.40 m de la cara
        Dim vc = InvocarVcEfectivo(102.8, 1.4, 0.5, Ve:=68.0, Vu:=116.0, Pu:=0.0, Ag:=0.15, fc:=21.0)
        Assert.AreEqual(102.8, vc, TOL)
    End Sub

    <TestMethod>
    Public Sub VcEfectivo_DentroDeDosH_ConAmbasCondiciones_AnulaVc()
        ' Ve/Vu = 68/128 = 0.53 >= 0.5 y Pu = 0 < Ag·fc/20 = 157.5 kN
        Dim vc = InvocarVcEfectivo(102.8, 1.0, 0.5, Ve:=68.0, Vu:=128.0, Pu:=0.0, Ag:=0.15, fc:=21.0)
        Assert.AreEqual(0.0, vc, TOL)
    End Sub

    <TestMethod>
    Public Sub VcEfectivo_DentroDeDosH_SiLaGravedadDomina_ConservaVc()
        ' Ve/Vu = 68/144.8 = 0.47 < 0.5 → no se cumple (a)
        Dim vc = InvocarVcEfectivo(102.8, 0.44, 0.5, Ve:=68.0, Vu:=144.8, Pu:=0.0, Ag:=0.15, fc:=21.0)
        Assert.AreEqual(102.8, vc, TOL)
    End Sub

    <TestMethod>
    Public Sub VcEfectivo_ConAxialAlta_ConservaVc()
        ' Ag·fc/20 = 0.15·21·1000/20 = 157.5 kN; Pu = 200 → no se cumple (b)
        Dim vc = InvocarVcEfectivo(102.8, 1.0, 0.5, Ve:=68.0, Vu:=128.0, Pu:=200.0, Ag:=0.15, fc:=21.0)
        Assert.AreEqual(102.8, vc, TOL)
    End Sub

    <TestMethod>
    Public Sub VcEfectivo_SinDemanda_ConservaVc()
        Dim vc = InvocarVcEfectivo(102.8, 0.44, 0.5, Ve:=68.0, Vu:=0.0, Pu:=0.0, Ag:=0.15, fc:=21.0)
        Assert.AreEqual(102.8, vc, TOL)
    End Sub

    ' =====================================================================
    ' Regla de negocio: rescate por cortante plástico
    ' =====================================================================
    ' Una viga que NO cumple a cortante convencional pero SÍ a plástico no se
    ' reporta. Desde 2026-09-15 aplica a las TRES zonas, también al Centro.

    <TestMethod>
    Public Sub CumpleCortantePlastico_SinResultado_DevuelveFalse()
        Assert.IsFalse(VigaService.CumpleCortantePlastico(eNumeradores.PosicionTramoViga.Izquierda, Nothing))
    End Sub

    <TestMethod>
    Public Sub CumpleCortantePlastico_RespondePorZona()
        Dim cp As New cResultadoCortantePlasticoFrame()
        cp.ZonaIzq.Cumple = True
        cp.ZonaDer.Cumple = False
        cp.ZonaCentro = New cRevisionCortantePlasticoZona() With {.Cumple = True}

        Assert.IsTrue(VigaService.CumpleCortantePlastico(eNumeradores.PosicionTramoViga.Izquierda, cp), "Izq")
        Assert.IsFalse(VigaService.CumpleCortantePlastico(eNumeradores.PosicionTramoViga.Derecha, cp), "Der")
        Assert.IsTrue(VigaService.CumpleCortantePlastico(eNumeradores.PosicionTramoViga.Centro, cp), "Centro")
    End Sub

    ''' <summary>
    ''' ZonaCentro queda Nothing en dos casos legítimos: proyectos guardados
    ''' antes de que se calculara, y vanos donde las zonas confinadas se
    ''' solapan. En ambos el centro NO está rescatado.
    ''' </summary>
    <TestMethod>
    Public Sub CumpleCortantePlastico_CentroSinCalcular_DevuelveFalse()
        Dim cp As New cResultadoCortantePlasticoFrame()
        cp.ZonaCentro = Nothing
        Assert.IsFalse(VigaService.CumpleCortantePlastico(eNumeradores.PosicionTramoViga.Centro, cp))
    End Sub

    ' =====================================================================
    ' Helpers de reflexión
    ' =====================================================================

    Private Shared Function InvocarMnNominal(As_mm2 As Double, fy_dis As Double,
                                             fc As Double, b_mm As Double, d_mm As Double) As Double
        Dim t = GetType(VigaService)
        Dim m = t.GetMethod("CalcularMnNominal",
                            BindingFlags.Instance Or BindingFlags.NonPublic)
        Assert.IsNotNull(m, "No se encontró VigaService.CalcularMnNominal (¿cambió de nombre o firma?)")
        Dim svc = Activator.CreateInstance(t, New Object() {Nothing})
        Return CDbl(m.Invoke(svc, New Object() {As_mm2, fy_dis, fc, b_mm, d_mm}))
    End Function

    Private Shared Function InvocarVcEfectivo(Vc As Double, distanciaCara As Double, h As Double,
                                              Ve As Double, Vu As Double, Pu As Double,
                                              Ag As Double, fc As Double) As Double
        Dim m = GetType(VigaService).GetMethod("VcEfectivo",
                                               BindingFlags.Static Or BindingFlags.NonPublic)
        Assert.IsNotNull(m, "No se encontró VigaService.VcEfectivo (¿cambió de nombre o firma?)")
        Return CDbl(m.Invoke(Nothing, New Object() {Vc, distanciaCara, h, Ve, Vu, Pu, Ag, fc}))
    End Function

End Class
