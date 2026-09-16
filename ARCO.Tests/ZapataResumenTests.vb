Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' ZapataService.Resumir: cuál de las cinco revisiones gobierna una zapata.
'''
''' Es lo que colorea la vista en planta, así que tiene que decir exactamente lo
''' mismo que la tabla de reporte del módulo. Las cinco relaciones
''' capacidad/demanda se tomaron de esa tabla, no se inventaron aquí:
'''
'''   Suelo estático    qAdm_Est / qMax
'''   Suelo dinámico    qAdm_Din / qMax
'''   Punzonamiento     Vc_p / |Vu_p|
'''   Cortante          Vc2 / max(Vu1, Vu3)  y  Vc1 / max(Vu2, Vu4)
'''   Flexión           rho colocado / rho requerido, en cada dirección
''' </summary>
<TestClass>
Public Class ZapataResumenTests

    ''' <summary>
    ''' Zapata holgada en todo: suelo 2.00, punzonamiento 2.00, cortante 2.00,
    ''' flexión 2.00. Sirve de punto de partida para aflojar una sola revisión
    ''' por prueba y ver cuál termina gobernando.
    ''' </summary>
    Private Shared Function ZapataHolgada() As cZapata

        Dim z As New cZapata() With {
            .Label_joint = "J1",
            .Nombre = "Z-1",
            .qAdm_Est = 200,
            .qAdm_Din = 300,
            .Rho_L1 = 0.004,
            .Rho_L2 = 0.004
        }

        z.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "EST1"})
        z.Lista_Combinaciones_Dinamicas.Add(New cCombinacionPila() With {.LoadCase = "DIN1"})

        z.Resultados("EST1") = ResultadoHolgado(100)      ' 200/100 = 2.00
        z.Resultados("DIN1") = ResultadoHolgado(150)      ' 300/150 = 2.00

        Return z

    End Function

    Private Shared Function ResultadoHolgado(qMax As Double) As ResultadoZapata
        Return New ResultadoZapata() With {
            .qMax = qMax,
            .Vu_p = 500, .Vc_p = 1000,
            .Vu1_C = 100, .Vu3_C = 100, .Vc2_C = 200,
            .Vu2_C = 100, .Vu4_C = 100, .Vc1_C = 200,
            .Rho_1 = 0.002, .Rho_2 = 0.002
        }
    End Function

    ' =====================================================================

    <TestMethod>
    Public Sub Resumir_SinResultados_NoCumpleNiFalla()
        Dim r = ZapataService.Resumir(New cZapata())
        Assert.IsFalse(r.TieneResultados, "no debería reportar resultados")
        Assert.IsFalse(r.Cumple, "sin cálculo no puede decir que cumple")
    End Sub

    <TestMethod>
    Public Sub Resumir_ZapataNula_NoRevienta()
        Dim r = ZapataService.Resumir(Nothing)
        Assert.IsFalse(r.TieneResultados)
    End Sub

    ''' <summary>Todas las revisiones al 2.00: ese es el peor factor.</summary>
    <TestMethod>
    Public Sub Resumir_TodoHolgado_ElPeorEsDos()
        Dim r = ZapataService.Resumir(ZapataHolgada())
        Assert.IsTrue(r.TieneResultados)
        Assert.AreEqual(2.0, r.PeorFactor, 0.0001)
        Assert.IsTrue(r.Cumple)
    End Sub

    ''' <summary>
    ''' El punzonamiento se aprieta a 0.80: tiene que gobernar, reportar su
    ''' nombre y su combinación, y marcar que no cumple.
    ''' </summary>
    <TestMethod>
    Public Sub Resumir_PunzonamientoGobierna()
        Dim z = ZapataHolgada()
        z.Resultados("DIN1").Vc_p = 400          ' 400/500 = 0.80

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(0.8, r.PeorFactor, 0.0001, "factor")
        Assert.AreEqual("Punzonamiento", r.Revision, "revisión")
        Assert.AreEqual("DIN1", r.Combinacion, "combinación")
        Assert.IsFalse(r.Cumple, "0.80 está por debajo del umbral")
    End Sub

    <TestMethod>
    Public Sub Resumir_SueloEstaticoGobierna()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").qMax = 250          ' 200/250 = 0.80

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(0.8, r.PeorFactor, 0.0001)
        Assert.AreEqual("Suelo estático", r.Revision)
        Assert.AreEqual("EST1", r.Combinacion)
    End Sub

    ''' <summary>
    ''' La misma qMax se juzga distinto según de qué lista venga la combinación,
    ''' porque la capacidad admisible dinámica es mayor. Si Resumir dejara de
    ''' distinguirlas, esta prueba lo detecta.
    ''' </summary>
    <TestMethod>
    Public Sub Resumir_LoDinamicoUsaSuPropiaAdmisible()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").qMax = 200          ' estático: 200/200 = 1.00
        z.Resultados("DIN1").qMax = 200          ' dinámico: 300/200 = 1.50

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(1.0, r.PeorFactor, 0.0001, "debe gobernar el estático")
        Assert.AreEqual("Suelo estático", r.Revision)
    End Sub

    <TestMethod>
    Public Sub Resumir_CortanteGobierna()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").Vc2_C = 70          ' 70/100 = 0.70

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(0.7, r.PeorFactor, 0.0001)
        Assert.AreEqual("Cortante", r.Revision)
    End Sub

    ''' <summary>La segunda dirección de cortante cuenta igual que la primera.</summary>
    <TestMethod>
    Public Sub Resumir_CortanteEnLaOtraDireccionTambienCuenta()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").Vc1_C = 60          ' 60/100 = 0.60

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(0.6, r.PeorFactor, 0.0001)
        Assert.AreEqual("Cortante", r.Revision)
    End Sub

    <TestMethod>
    Public Sub Resumir_FlexionGobierna()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").Rho_1 = 0.008       ' 0.004/0.008 = 0.50

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(0.5, r.PeorFactor, 0.0001)
        Assert.AreEqual("Flexión", r.Revision)
    End Sub

    ''' <summary>
    ''' Demanda nula NO es capacidad infinita: significa que esa revisión no
    ''' aplica en esa combinación. Si se colara como un cociente enorme daría
    ''' igual, pero si se colara como división por cero (NaN o infinito) podría
    ''' arrastrar el resultado. Se ignora.
    ''' </summary>
    <TestMethod>
    Public Sub Resumir_DemandaNula_SeIgnoraEsaRevision()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").Vu_p = 0
        z.Resultados("EST1").Vu1_C = 0 : z.Resultados("EST1").Vu3_C = 0
        z.Resultados("EST1").Vu2_C = 0 : z.Resultados("EST1").Vu4_C = 0
        z.Resultados("EST1").Rho_1 = 0 : z.Resultados("EST1").Rho_2 = 0

        Dim r = ZapataService.Resumir(z)

        Assert.IsTrue(r.TieneResultados, "sigue habiendo resultados")
        Assert.IsFalse(Double.IsNaN(r.PeorFactor), "no debe salir NaN")
        Assert.IsFalse(Double.IsInfinity(r.PeorFactor), "no debe salir infinito")
        Assert.AreEqual(2.0, r.PeorFactor, 0.0001, "queda gobernando lo que sí tenía demanda")
    End Sub

    ''' <summary>
    ''' Una combinación que no se calculó no aporta: no debe tumbar el resumen
    ''' ni hacerlo explotar. Pasa al abrir proyectos donde se cambió la lista de
    ''' combinaciones sin recalcular.
    ''' </summary>
    <TestMethod>
    Public Sub Resumir_CombinacionSinResultado_SeSalta()
        Dim z = ZapataHolgada()
        z.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "NO_CALCULADA"})

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(2.0, r.PeorFactor, 0.0001)
    End Sub

    ''' <summary>
    ''' El umbral del resumen es el único del programa (0.90), no un 1.0 propio.
    ''' Una zapata justo en el umbral cumple; justo por debajo, no.
    ''' </summary>
    <TestMethod>
    Public Sub Resumir_ElUmbralEsElDelPrograma()
        Dim justo = ZapataHolgada()
        justo.Resultados("EST1").Vc_p = Funciones_00_Varias.UMBRAL_CD * 500
        Assert.IsTrue(ZapataService.Resumir(justo).Cumple, "justo en el umbral debe cumplir")

        Dim porDebajo = ZapataHolgada()
        porDebajo.Resultados("EST1").Vc_p = (Funciones_00_Varias.UMBRAL_CD - 0.01) * 500
        Assert.IsFalse(ZapataService.Resumir(porDebajo).Cumple, "por debajo no")
    End Sub

    ''' <summary>
    ''' Con varias combinaciones gana la peor, no la última recorrida ni el
    ''' promedio.
    ''' </summary>
    <TestMethod>
    Public Sub Resumir_ConVariasCombinaciones_GanaLaPeor()
        Dim z = ZapataHolgada()
        z.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "EST2"})
        z.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "EST3"})
        z.Resultados("EST2") = ResultadoHolgado(160)      ' 200/160 = 1.25
        z.Resultados("EST3") = ResultadoHolgado(400)      ' 200/400 = 0.50

        Dim r = ZapataService.Resumir(z)

        Assert.AreEqual(0.5, r.PeorFactor, 0.0001)
        Assert.AreEqual("EST3", r.Combinacion)
    End Sub

    ' =====================================================================
    ' FactoresPorCombinacion
    ' =====================================================================

    ''' <summary>Una fila por combinacion calculada, no por combinacion listada.</summary>
    <TestMethod>
    Public Sub Factores_UnaFilaPorCombinacionCalculada()
        Dim f = ZapataService.FactoresPorCombinacion(ZapataHolgada())
        Assert.AreEqual(2, f.Count)
        CollectionAssert.AreEquivalent(New String() {"EST1", "DIN1"},
                                       f.Select(Function(x) x.Combinacion).ToArray())
    End Sub

    ''' <summary>
    ''' Marca cuales vienen de la lista dinamica. De ahi sale que use una
    ''' admisible u otra, y es lo que muestra la columna "Tipo" del reporte.
    ''' </summary>
    <TestMethod>
    Public Sub Factores_MarcaLasDinamicas()
        Dim f = ZapataService.FactoresPorCombinacion(ZapataHolgada())
        Assert.IsFalse(f.First(Function(x) x.Combinacion = "EST1").EsDinamica)
        Assert.IsTrue(f.First(Function(x) x.Combinacion = "DIN1").EsDinamica)
    End Sub

    ''' <summary>
    ''' Una combinacion que no esta en ninguna de las dos listas se trata como
    ''' estatica: la admisible menor deja el resultado del lado seguro en vez de
    ''' omitir la revision del suelo.
    ''' </summary>
    <TestMethod>
    Public Sub Factores_CombinacionHuerfana_SeTrataComoEstatica()
        Dim z = ZapataHolgada()
        z.Resultados("HUERFANA") = ResultadoHolgado(100)

        Dim f = ZapataService.FactoresPorCombinacion(z).First(Function(x) x.Combinacion = "HUERFANA")

        Assert.IsFalse(f.EsDinamica)
        Assert.AreEqual(2.0, f.Suelo, 0.0001, "200/100, la admisible estatica")
    End Sub

    ''' <summary>Cada revision con su propio cociente, no un solo numero mezclado.</summary>
    <TestMethod>
    Public Sub Factores_CadaRevisionPorSeparado()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").Vc_p = 400        ' punz 0.80
        z.Resultados("EST1").Vc2_C = 150       ' cort 1.50
        z.Resultados("EST1").Rho_1 = 0.008     ' flex 0.50

        Dim f = ZapataService.FactoresPorCombinacion(z).First(Function(x) x.Combinacion = "EST1")

        Assert.AreEqual(2.0, f.Suelo, 0.0001, "suelo")
        Assert.AreEqual(0.8, f.Punzonamiento, 0.0001, "punzonamiento")
        Assert.AreEqual(1.5, f.Cortante, 0.0001, "cortante")
        Assert.AreEqual(0.5, f.Flexion, 0.0001, "flexion")
        Assert.AreEqual(0.5, f.Peor, 0.0001, "peor")
        Assert.AreEqual("Flexion", f.Revision.Replace(ChrW(243), "o"), "gobierna")
    End Sub

    ''' <summary>
    ''' Cero es "no aplica", y por eso no puede ganar el minimo: si contara, una
    ''' revision ausente se reportaria como la peor de todas.
    ''' </summary>
    <TestMethod>
    Public Sub Factores_ElCeroNoGanaElMinimo()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").Vu_p = 0          ' punzonamiento no aplica

        Dim f = ZapataService.FactoresPorCombinacion(z).First(Function(x) x.Combinacion = "EST1")

        Assert.AreEqual(0.0, f.Punzonamiento, 0.0001, "queda en cero")
        Assert.AreEqual(2.0, f.Peor, 0.0001, "pero no gobierna")
        Assert.AreNotEqual("Punzonamiento", f.Revision)
    End Sub

    ''' <summary>Sin ninguna revision aplicable, Peor es 0 y no hay quien gobierne.</summary>
    <TestMethod>
    Public Sub Factores_SinNingunaRevisionAplicable()
        Dim f As New ZapataService.FactoresCombinacion()
        Assert.AreEqual(0.0, f.Peor, 0.0001)
        Assert.AreEqual("", f.Revision)
    End Sub

    ''' <summary>El nombre de la revision de suelo cambia con el tipo de combinacion.</summary>
    <TestMethod>
    Public Sub Factores_ElNombreDelSueloDistingueEstaticoDeDinamico()
        Dim z = ZapataHolgada()
        z.Resultados("EST1").qMax = 400        ' suelo estatico 0.50, gobierna
        z.Resultados("DIN1").qMax = 600        ' suelo dinamico 0.50, gobierna

        Dim f = ZapataService.FactoresPorCombinacion(z)
        Assert.AreEqual("Suelo estatico",
                        f.First(Function(x) x.Combinacion = "EST1").Revision.Replace(ChrW(225), "a"))
        Assert.AreEqual("Suelo dinamico",
                        f.First(Function(x) x.Combinacion = "DIN1").Revision.Replace(ChrW(225), "a"))
    End Sub

End Class
