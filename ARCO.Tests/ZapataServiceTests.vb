Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO
Imports ARCO.eNumeradores

''' <summary>
''' Punzonamiento de zapatas según la posición del apoyo.
'''
''' Hasta 2026-09-15 el programa calculaba SIEMPRE el perímetro crítico cerrado
''' de una zapata interior y alfa_s = 40, sin importar dónde estuviera la
''' zapata. Estas pruebas fijan las tres condiciones de NSR-10 C.11.11 con
''' números verificados a mano, para que la corrección no se pierda y para que
''' quede escrito cuánto valía el error: la capacidad de una esquinera es el
''' 37 % de la que se venía usando.
'''
''' Caso de referencia, el mismo en todas las pruebas de capacidad:
'''   zapata 2.00 x 2.00, pedestal 0.40 x 0.40, d = 0.45 m, fc = 21 MPa
''' </summary>
<TestClass>
Public Class ZapataServiceTests

    Private Const B_PED As Double = 0.4
    Private Const H_PED As Double = 0.4
    Private Const D_EF As Double = 0.45
    Private Const FC As Double = 21.0

    ' =====================================================================
    ' alfa_s — NSR-10 C.11.11.2.1
    ' =====================================================================

    <TestMethod>
    Public Sub AlfaS_CadaPosicionTieneSuValor()
        Assert.AreEqual(40.0, ZapataService.AlfaS(eTipoApoyoZapata.Central), 0.0001, "interior")
        Assert.AreEqual(30.0, ZapataService.AlfaS(eTipoApoyoZapata.Medianera), 0.0001, "borde")
        Assert.AreEqual(20.0, ZapataService.AlfaS(eTipoApoyoZapata.Esquinera), 0.0001, "esquina")
    End Sub

    ' =====================================================================
    ' Perímetro crítico b0
    ' =====================================================================

    ''' <summary>
    ''' Cerrado: 2(b + h) + 4d = 2(0.8) + 1.8 = 3.40 m.
    ''' Es exactamente la expresión que ya tenía el programa — una zapata
    ''' central tiene que seguir dando el mismo número que antes del cambio.
    ''' </summary>
    <TestMethod>
    Public Sub PerimetroCritico_Central_EsElCerradoDeSiempre()
        Assert.AreEqual(3.4, ZapataService.PerimetroCritico(B_PED, H_PED, D_EF, eTipoApoyoZapata.Central), 0.0001)
    End Sub

    ''' <summary>Tres lados: (h + d) + 2(b + d/2) = 0.85 + 1.25 = 2.10 m.</summary>
    <TestMethod>
    Public Sub PerimetroCritico_Medianera_PierdeUnLado()
        Assert.AreEqual(2.1, ZapataService.PerimetroCritico(B_PED, H_PED, D_EF, eTipoApoyoZapata.Medianera), 0.0001)
    End Sub

    ''' <summary>Dos lados: (b + d/2) + (h + d/2) = 0.625 + 0.625 = 1.25 m.</summary>
    <TestMethod>
    Public Sub PerimetroCritico_Esquinera_PierdeDosLados()
        Assert.AreEqual(1.25, ZapataService.PerimetroCritico(B_PED, H_PED, D_EF, eTipoApoyoZapata.Esquinera), 0.0001)
    End Sub

    ''' <summary>
    ''' La propiedad que da sentido a todo esto: cuanto más expuesto está el
    ''' apoyo, menos perímetro queda para desarrollar el cono de falla.
    ''' </summary>
    <TestMethod>
    Public Sub PerimetroCritico_DecreceAlAcercarseAlBorde()
        Dim cen = ZapataService.PerimetroCritico(B_PED, H_PED, D_EF, eTipoApoyoZapata.Central)
        Dim med = ZapataService.PerimetroCritico(B_PED, H_PED, D_EF, eTipoApoyoZapata.Medianera)
        Dim esq = ZapataService.PerimetroCritico(B_PED, H_PED, D_EF, eTipoApoyoZapata.Esquinera)
        Assert.IsTrue(cen > med, $"central {cen} debería superar a medianera {med}")
        Assert.IsTrue(med > esq, $"medianera {med} debería superar a esquinera {esq}")
    End Sub

    ''' <summary>Geometría inválida: no debe devolver un perímetro utilizable.</summary>
    <TestMethod>
    Public Sub PerimetroCritico_GeometriaInvalida_DevuelveCero()
        Assert.AreEqual(0.0, ZapataService.PerimetroCritico(0, H_PED, D_EF, eTipoApoyoZapata.Central), 0.0001, "b nulo")
        Assert.AreEqual(0.0, ZapataService.PerimetroCritico(B_PED, 0, D_EF, eTipoApoyoZapata.Central), 0.0001, "h nulo")
        Assert.AreEqual(0.0, ZapataService.PerimetroCritico(B_PED, H_PED, 0, eTipoApoyoZapata.Central), 0.0001, "d nulo")
        Assert.AreEqual(0.0, ZapataService.PerimetroCritico(B_PED, H_PED, -0.3, eTipoApoyoZapata.Esquinera), 0.0001, "d negativo")
    End Sub

    ' =====================================================================
    ' Capacidad a punzonamiento
    ' =====================================================================

    ''' <summary>
    ''' Central. Gobierna el tercer límite:
    '''   phiVc3 = 0.75 x 0.33 x raiz(21) x 3.40 x 0.45 x 1000 = 1735 kN
    ''' </summary>
    <TestMethod>
    Public Sub CapacidadPunzonamiento_Central_1735kN()
        Dim c = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, eTipoApoyoZapata.Central)
        Assert.AreEqual(3.4, c.b0, 0.0001, "b0")
        Assert.AreEqual(1735.3, c.Vc, 1.0, "capacidad")
        Assert.AreEqual(c.Vc3, c.Vc, 0.0001, "debe gobernar el tercer límite")
    End Sub

    ''' <summary>Medianera: 1072 kN, el 62 % de la central.</summary>
    <TestMethod>
    Public Sub CapacidadPunzonamiento_Medianera_1072kN()
        Dim c = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, eTipoApoyoZapata.Medianera)
        Assert.AreEqual(2.1, c.b0, 0.0001, "b0")
        Assert.AreEqual(1071.8, c.Vc, 1.0, "capacidad")
    End Sub

    ''' <summary>Esquinera: 638 kN, el 37 % de la central.</summary>
    <TestMethod>
    Public Sub CapacidadPunzonamiento_Esquinera_638kN()
        Dim c = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, eTipoApoyoZapata.Esquinera)
        Assert.AreEqual(1.25, c.b0, 0.0001, "b0")
        Assert.AreEqual(638.0, c.Vc, 1.0, "capacidad")
    End Sub

    ''' <summary>
    ''' Esta es la prueba que justifica todo el cambio: si alguien volviera a
    ''' calcular todas las zapatas como interiores, la diferencia sería esta.
    ''' </summary>
    <TestMethod>
    Public Sub CapacidadPunzonamiento_ElBordeNoConservadorQueSeCorrigio()
        Dim cen = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, eTipoApoyoZapata.Central).Vc
        Dim med = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, eTipoApoyoZapata.Medianera).Vc
        Dim esq = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, eTipoApoyoZapata.Esquinera).Vc

        Assert.AreEqual(0.62, med / cen, 0.01, "una medianera vale ~62 % de la central")
        Assert.AreEqual(0.37, esq / cen, 0.01, "una esquinera vale ~37 % de la central")
    End Sub

    ''' <summary>La capacidad es el MENOR de los tres límites de la norma.</summary>
    <TestMethod>
    Public Sub CapacidadPunzonamiento_TomaElMenorDeLosTresLimites()
        For Each tipoApoyo In {eTipoApoyoZapata.Central,
                          eTipoApoyoZapata.Medianera,
                          eTipoApoyoZapata.Esquinera}
            Dim c = ZapataService.CapacidadPunzonamiento(FC, B_PED, H_PED, D_EF, tipoApoyo)
            Dim menor = Math.Min(c.Vc1, Math.Min(c.Vc2, c.Vc3))
            Assert.AreEqual(menor, c.Vc, 0.0001, $"{tipoApoyo}")
        Next
    End Sub

    ''' <summary>
    ''' Pedestal alargado: beta = largo/corto entra en el primer límite y lo baja.
    ''' Los dos pedestales tienen el mismo semiperímetro (0.16 + 0.64 = 0.40 + 0.40),
    ''' así que dan el mismo b0 y la única diferencia entre ellos es beta: 1 contra 4.
    ''' </summary>
    <TestMethod>
    Public Sub CapacidadPunzonamiento_PedestalAlargadoReduceElPrimerLimite()
        Dim cuadrado = ZapataService.CapacidadPunzonamiento(FC, 0.4, 0.4, D_EF, eTipoApoyoZapata.Central)
        Dim alargado = ZapataService.CapacidadPunzonamiento(FC, 0.16, 0.64, D_EF, eTipoApoyoZapata.Central)

        Assert.AreEqual(cuadrado.b0, alargado.b0, 0.0001, "el b0 no cambia")
        Assert.IsTrue(alargado.Vc1 < cuadrado.Vc1,
                      $"con beta mayor el primer límite debe bajar: {alargado.Vc1} vs {cuadrado.Vc1}")
    End Sub

    <TestMethod>
    Public Sub CapacidadPunzonamiento_DatosInvalidos_DevuelveCero()
        Dim sinFc = ZapataService.CapacidadPunzonamiento(0, B_PED, H_PED, D_EF, eTipoApoyoZapata.Central)
        Assert.AreEqual(0.0, sinFc.Vc, 0.0001, "sin resistencia del concreto")

        Dim sinGeom = ZapataService.CapacidadPunzonamiento(FC, 0, H_PED, D_EF, eTipoApoyoZapata.Central)
        Assert.AreEqual(0.0, sinGeom.Vc, 0.0001, "sin geometría")
    End Sub

    ' =====================================================================
    ' Clasificación automática por coordenadas
    ' =====================================================================

    ''' <summary>Malla 3 x 3 a 5 m: un centro, cuatro bordes, cuatro esquinas.</summary>
    Private Shared Function MallaTresPorTres() As List(Of cZapata)
        Dim lista As New List(Of cZapata)()
        For Each x In New Double() {0, 5, 10}
            For Each y In New Double() {0, 5, 10}
                lista.Add(New cZapata() With {
                    .Label_joint = $"J{x}-{y}",
                    .CoordX = x,
                    .CoordY = y,
                    .TieneCoordenadas = True
                })
            Next
        Next
        Return lista
    End Function

    Private Shared Function Buscar(lista As List(Of cZapata), x As Double, y As Double) As cZapata
        Return lista.FirstOrDefault(Function(z) z.CoordX = x AndAlso z.CoordY = y)
    End Function

    ''' <summary>
    ''' El reparto que se haría a ojo sobre la planta: el nodo interior tiene
    ''' vecinos en las cuatro direcciones, los de borde en tres, los de esquina
    ''' en dos.
    ''' </summary>
    <TestMethod>
    Public Sub ClasificarApoyos_MallaRegular_RepartoEsperado()
        Dim lista = MallaTresPorTres()
        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Central, Buscar(lista, 5, 5).TipoApoyo, "centro")

        For Each p In New Double()() {New Double() {5, 0}, New Double() {5, 10},
                                      New Double() {0, 5}, New Double() {10, 5}}
            Assert.AreEqual(eTipoApoyoZapata.Medianera, Buscar(lista, p(0), p(1)).TipoApoyo,
                            $"borde ({p(0)},{p(1)})")
        Next

        For Each p In New Double()() {New Double() {0, 0}, New Double() {0, 10},
                                      New Double() {10, 0}, New Double() {10, 10}}
            Assert.AreEqual(eTipoApoyoZapata.Esquinera, Buscar(lista, p(0), p(1)).TipoApoyo,
                            $"esquina ({p(0)},{p(1)})")
        Next
    End Sub

    ''' <summary>
    ''' Lo que el ingeniero marca a mano manda. Un voladizo, una junta de
    ''' dilatación o una zapata combinada rompen la inferencia geométrica, y esa
    ''' corrección tiene que sobrevivir a un recálculo.
    ''' </summary>
    <TestMethod>
    Public Sub ClasificarApoyos_NoPisaLoMarcadoAMano()
        Dim lista = MallaTresPorTres()
        Dim centro = Buscar(lista, 5, 5)
        centro.TipoApoyo = eTipoApoyoZapata.Esquinera
        centro.TipoApoyoManual = True

        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Esquinera, centro.TipoApoyo,
                        "la clasificación automática pisó una corrección manual")
        Assert.AreEqual(eTipoApoyoZapata.Esquinera, Buscar(lista, 0, 0).TipoApoyo,
                        "las demás sí deben clasificarse")
    End Sub

    ''' <summary>Solo cuenta las que cambiaron: volver a correrlo no cambia nada.</summary>
    <TestMethod>
    Public Sub ClasificarApoyos_EsIdempotente()
        Dim lista = MallaTresPorTres()
        Dim primera = ZapataService.ClasificarApoyos(lista)
        Dim segunda = ZapataService.ClasificarApoyos(lista)

        Assert.IsTrue(primera > 0, "la primera pasada debe clasificar algo")
        Assert.AreEqual(0, segunda, "la segunda pasada no debería cambiar nada")
    End Sub

    ''' <summary>Las zapatas sin coordenadas no se tocan: quedan como estaban.</summary>
    <TestMethod>
    Public Sub ClasificarApoyos_SinCoordenadas_NoHaceNada()
        Dim lista = MallaTresPorTres()
        For Each z In lista
            z.TieneCoordenadas = False
        Next

        Assert.AreEqual(0, ZapataService.ClasificarApoyos(lista))
        Assert.IsTrue(lista.All(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central),
                      "deberían haber quedado en su valor inicial")
    End Sub

    <TestMethod>
    Public Sub ClasificarApoyos_ListaVaciaONula_NoRevienta()
        Assert.AreEqual(0, ZapataService.ClasificarApoyos(Nothing), "nula")
        Assert.AreEqual(0, ZapataService.ClasificarApoyos(New List(Of cZapata)()), "vacía")
    End Sub

    ''' <summary>
    ''' LIMITACIÓN CONOCIDA, fijada a propósito.
    '''
    ''' Una sola línea de columnas es un plano degenerado: ningún nodo tiene
    ''' vecinos en Y, así que el criterio de "cuántas direcciones están ocupadas"
    ''' no llega a tres en ninguno y los marca a todos como esquineras. En un
    ''' edificio real esto no se da — basta que haya dos líneas de columnas para
    ''' que los intermedios salgan bien (ver la malla 3 x 3) — pero si alguna vez
    ''' aparece, la clasificación queda del lado conservador y hay que corregirla
    ''' a mano.
    '''
    ''' Se deja escrito para que, si alguien cambia el criterio, vea de entrada
    ''' cuál era el comportamiento anterior y no lo cambie por accidente.
    ''' </summary>
    <TestMethod>
    Public Sub ClasificarApoyos_UnaSolaLineaDeColumnas_QuedaConservadora()
        Dim lista As New List(Of cZapata)()
        For Each x In New Double() {0, 5, 10}
            lista.Add(New cZapata() With {.Label_joint = $"J{x}", .CoordX = x, .CoordY = 0, .TieneCoordenadas = True})
        Next

        ZapataService.ClasificarApoyos(lista)

        Assert.IsTrue(lista.All(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera),
                      "en un plano degenerado el criterio geométrico no distingue; debe quedar del lado seguro")
    End Sub

    ''' <summary>
    ''' Dos líneas de columnas — un edificio de una sola crujía — ya basta para
    ''' que el criterio funcione: los intermedios tienen vecinos en tres
    ''' direcciones y salen como medianeras, no como esquineras.
    ''' </summary>
    <TestMethod>
    Public Sub ClasificarApoyos_UnaSolaCrujia_LosIntermediosSonMedianeras()
        Dim lista As New List(Of cZapata)()
        For Each x In New Double() {0, 5, 10}
            For Each y In New Double() {0, 6}
                lista.Add(New cZapata() With {
                    .Label_joint = $"J{x}-{y}", .CoordX = x, .CoordY = y, .TieneCoordenadas = True})
            Next
        Next

        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Medianera, Buscar(lista, 5, 0).TipoApoyo, "intermedio inferior")
        Assert.AreEqual(eTipoApoyoZapata.Medianera, Buscar(lista, 5, 6).TipoApoyo, "intermedio superior")
        Assert.AreEqual(eTipoApoyoZapata.Esquinera, Buscar(lista, 0, 0).TipoApoyo, "esquina")
        Assert.AreEqual(eTipoApoyoZapata.Esquinera, Buscar(lista, 10, 6).TipoApoyo, "esquina")
    End Sub

    ''' <summary>
    ''' La tolerancia absorbe el desalineamiento real de un modelo: columnas que
    ''' no quedan exactamente sobre el eje. Con 5 cm de desfase la malla se debe
    ''' seguir leyendo igual que si fuera perfecta.
    ''' </summary>
    <TestMethod>
    Public Sub ClasificarApoyos_ToleraColumnasLigeramenteDesalineadas()
        Dim lista = MallaTresPorTres()
        Buscar(lista, 5, 5).CoordX = 5.05

        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Central,
                        lista.First(Function(z) z.CoordX = 5.05).TipoApoyo,
                        "el centro desalineado 5 cm sigue siendo interior")
    End Sub

    ' =====================================================================
    ' Etiquetas y valores por defecto
    ' =====================================================================

    ''' <summary>
    ''' Los textos son los que se escriben en la tabla y se leen de vuelta al
    ''' editarla, así que cambiarlos rompe la edición manual.
    ''' </summary>
    <TestMethod>
    Public Sub NombreTipo_TextosDeLaTabla()
        Assert.AreEqual("Central", ZapataService.NombreTipo(eTipoApoyoZapata.Central))
        Assert.AreEqual("Medianera", ZapataService.NombreTipo(eTipoApoyoZapata.Medianera))
        Assert.AreEqual("Esquinera", ZapataService.NombreTipo(eTipoApoyoZapata.Esquinera))
    End Sub

    ''' <summary>
    ''' Una zapata recién creada se asume interior, que es lo que hacía el
    ''' programa antes del cambio. Importa porque los proyectos guardados con
    ''' versiones anteriores llegan sin este campo.
    ''' </summary>
    <TestMethod>
    Public Sub CZapata_NuevaEsCentralYNoManual()
        Dim z As New cZapata()
        Assert.AreEqual(eTipoApoyoZapata.Central, z.TipoApoyo)
        Assert.IsFalse(z.TipoApoyoManual)
        Assert.IsFalse(z.TieneCoordenadas)
    End Sub

    ' =====================================================================
    ' Peso estabilizante (zapata + pedestal + suelo)
    ' =====================================================================
    ' El peso se suma al P para revisar suelo y excentricidad SOLO cuando el
    ' proyecto lo activa. Se usan las fórmulas típicas del cálculo manual:
    '
    '   W_zapata   = L_b · L_h · e · γ_concreto
    '   W_pedestal = b · h · (Df − e) · γ_concreto
    '   W_suelo    = (L_b · L_h − b · h) · (Df − e) · γ_suelo
    '
    ' Caso de referencia (2×2, pedestal 0.4×0.4, e=0.5, Df=1.5, γc=24, γs=18):
    '   W_zapata   = 2·2·0.5·24         = 48 kN
    '   W_pedestal = 0.4·0.4·1.0·24     = 3.84 kN
    '   W_suelo    = (4 − 0.16)·1.0·18  = 69.12 kN
    '   Total                            = 120.96 kN

    Private Shared Function ZapataConDesplante() As cZapata
        Return New cZapata() With {
            .L_b = 2.0, .L_h = 2.0, .e = 0.5, .b = 0.4, .h = 0.4,
            .Df = 1.5, .gammaConcreto = 24.0, .gammaSuelo = 18.0
        }
    End Function

    <TestMethod>
    Public Sub Pesos_LosTresComponentesDelCasoDeReferencia()
        Dim p = ZapataService.CalcularPesosEstabilizantes(ZapataConDesplante())
        Assert.AreEqual(48.0, p.W_Zapata, 0.01, "zapata")
        Assert.AreEqual(3.84, p.W_Pedestal, 0.01, "pedestal")
        Assert.AreEqual(69.12, p.W_Suelo, 0.01, "suelo (huella descontando pedestal)")
        Assert.AreEqual(120.96, p.Total, 0.01, "total")
    End Sub

    ''' <summary>
    ''' Sin desplante mayor que el espesor, no hay pedestal ni suelo por encima
    ''' de la cara superior de la zapata: solo pesa la zapata. Es el caso de una
    ''' zapata al ras.
    ''' </summary>
    <TestMethod>
    Public Sub Pesos_DfIgualAEspesor_SoloPesaLaZapata()
        Dim z = ZapataConDesplante() : z.Df = z.e
        Dim p = ZapataService.CalcularPesosEstabilizantes(z)
        Assert.AreEqual(48.0, p.W_Zapata, 0.01)
        Assert.AreEqual(0.0, p.W_Pedestal, 0.001)
        Assert.AreEqual(0.0, p.W_Suelo, 0.001)
    End Sub

    ''' <summary>
    ''' Un Df más chico que el espesor no puede dar peso negativo — pasa cuando
    ''' se importa una zapata a la que no se le puso desplante.
    ''' </summary>
    <TestMethod>
    Public Sub Pesos_DfMenorQueEspesor_PesoDePedestalYSueloNoNegativo()
        Dim z = ZapataConDesplante() : z.Df = 0.1
        Dim p = ZapataService.CalcularPesosEstabilizantes(z)
        Assert.IsTrue(p.W_Pedestal >= 0)
        Assert.IsTrue(p.W_Suelo >= 0)
    End Sub

    <TestMethod>
    Public Sub Pesos_ZapataNula_NoRevienta()
        Dim p = ZapataService.CalcularPesosEstabilizantes(Nothing)
        Assert.AreEqual(0.0, p.Total, 0.001)
    End Sub

    ''' <summary>
    ''' Un proyecto viejo llega con gammaConcreto = 0 en el .esm: al deserializar
    ''' se corrige a 24. Sin este arreglo, activar el peso estabilizante daría
    ''' W_zapata = 0 y W_pedestal = 0.
    ''' </summary>
    <TestMethod>
    Public Sub Pesos_GammaConcretoCeroSeReparaConDefault()
        Dim z = ZapataConDesplante() : z.gammaConcreto = 0
        Dim p = ZapataService.CalcularPesosEstabilizantes(z)
        Assert.AreEqual(48.0, p.W_Zapata, 0.01, "usa el default de 24 cuando el campo es 0")
    End Sub

    ' =====================================================================
    ' EvaluarZapata con peso estabilizante
    ' =====================================================================
    ' El peso estabilizante debe SUBIR el P que ven la revisión de suelo y la
    ' de excentricidad, y no debe afectar punzonamiento/cortante/flexión.

    Private Shared Function ZapataParaEvaluar() As cZapata
        Return New cZapata() With {
            .L_b = 2.0, .L_h = 2.0, .e = 0.5, .rec = 0.075, .d = 0.425,
            .b = 0.4, .h = 0.4,
            .Df = 1.5, .gammaConcreto = 24.0, .gammaSuelo = 18.0,
            .fc = 21.0, .fy = 420.0,
            .qAdm_Est = 200.0, .qAdm_Din = 300.0,
            .Rho_L1 = 0.003, .Rho_L2 = 0.003,
            .TipoApoyo = eTipoApoyoZapata.Central
        }
    End Function

    <TestMethod>
    Public Sub EvaluarZapata_ConPesoEstabilizante_SumaAlPEfectivo()
        Dim z = ZapataParaEvaluar()
        Dim P As Double = 500 : Dim My As Double = 200

        Dim resSin = Funciones_Zapatas.EvaluarZapata(z, P, 0, My, "EST", usarPesoEstabilizante:=False)
        Dim resCon = Funciones_Zapatas.EvaluarZapata(z, P, 0, My, "EST", usarPesoEstabilizante:=True)

        Assert.AreEqual(500.0, resSin.P_Efectivo, 0.01, "sin peso, P efectivo = P")
        Assert.AreEqual(500.0 + 120.96, resCon.P_Efectivo, 0.05, "con peso, P efectivo = P + pesos")
        Assert.IsFalse(resSin.UsoPesoEstabilizante)
        Assert.IsTrue(resCon.UsoPesoEstabilizante)
    End Sub

    <TestMethod>
    Public Sub EvaluarZapata_PesoEstabilizante_BajaLaExcentricidad()
        Dim z = ZapataParaEvaluar()
        Dim P As Double = 500 : Dim My As Double = 200   ' ex = My/P

        Dim resSin = Funciones_Zapatas.EvaluarZapata(z, P, 0, My, "EST", usarPesoEstabilizante:=False)
        Dim resCon = Funciones_Zapatas.EvaluarZapata(z, P, 0, My, "EST", usarPesoEstabilizante:=True)

        Assert.AreEqual(200.0 / 500.0, resSin.ex, 0.0001, "ex sin peso")
        Assert.AreEqual(200.0 / (500.0 + 120.96), resCon.ex, 0.0001, "ex con peso: baja porque P efectivo sube")
        Assert.IsTrue(Math.Abs(resCon.ex) < Math.Abs(resSin.ex), "el peso reduce |ex|")
    End Sub

    <TestMethod>
    Public Sub EvaluarZapata_PesoEstabilizante_NoTocaPunzonamiento()
        Dim z = ZapataParaEvaluar()
        Dim P As Double = 500

        Dim resSin = Funciones_Zapatas.EvaluarZapata(z, P, 0, 100, "EST", usarPesoEstabilizante:=False)
        Dim resCon = Funciones_Zapatas.EvaluarZapata(z, P, 0, 100, "EST", usarPesoEstabilizante:=True)

        ' Vu depende de P reactivo, no de P efectivo. Un cambio de flag no debe
        ' inflar la demanda de punzonamiento — sería del lado inseguro.
        Assert.AreEqual(resSin.Vu_p, resCon.Vu_p, 0.01,
                        "Vu de punzonamiento debe seguir dependiendo solo del P reactivo")
        Assert.AreEqual(resSin.Vc_p, resCon.Vc_p, 0.01, "y Vc tampoco cambia")
    End Sub

    ' =====================================================================
    ' Excentricidad como quinta revisión
    ' =====================================================================
    ' L/6 en estático (regla del núcleo central), L/N en dinámico (por defecto
    ' L/4). Se aplica sobre |e|.

    <TestMethod>
    Public Sub Excentricidad_Estatica_LimiteEsLPor6()
        Dim z = ZapataParaEvaluar()
        Dim res = Funciones_Zapatas.EvaluarZapata(z, 1000, 0, 200, "EST")
        ' L_b = 2.0 → L_b/6 = 0.3333, ex = 200/1000 = 0.20 → cumple (con margen)
        Assert.AreEqual(2.0 / 6.0, res.Lim_x_usado, 0.0001, "estático usa L/6")
        Assert.IsTrue(res.CumpleExcentricidad, "0.20 < 0.333, cumple")
    End Sub

    <TestMethod>
    Public Sub Excentricidad_Dinamica_LimiteEsLPorN()
        Dim z = ZapataParaEvaluar()
        Dim res = Funciones_Zapatas.EvaluarZapata(z, 1000, 0, 200, "DIN",
                                                  limiteDinamicoN:=4.0)
        ' L_b = 2.0 → L_b/4 = 0.5, ex = 0.20 → cumple
        Assert.AreEqual(2.0 / 4.0, res.Lim_x_usado, 0.0001, "dinámico usa L/N")
        Assert.IsTrue(res.CumpleExcentricidad)
    End Sub

    <TestMethod>
    Public Sub Excentricidad_LibraEnDinamicoLoQueFallariaEnEstatico()
        Dim z = ZapataParaEvaluar()
        ' ex = 400/1000 = 0.40 → falla L/6 (0.333), cumple L/3 (0.667)
        Dim resEst = Funciones_Zapatas.EvaluarZapata(z, 1000, 0, 400, "EST")
        Dim resDin = Funciones_Zapatas.EvaluarZapata(z, 1000, 0, 400, "DIN",
                                                     limiteDinamicoN:=3.0)
        Assert.IsFalse(resEst.CumpleExcentricidad, "no cumple L/6 en estático")
        Assert.IsTrue(resDin.CumpleExcentricidad, "cumple L/3 en dinámico")
    End Sub

    ''' <summary>
    ''' La excentricidad se cuela como quinta revisión en FactoresPorCombinacion
    ''' y en Resumir: si es la peor, gobierna.
    ''' </summary>
    <TestMethod>
    Public Sub Excentricidad_ApareceComoRevisionEnFactoresYResumen()
        Dim z = ZapataParaEvaluar()
        z.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "EST1"})
        z.Resultados("EST1") = Funciones_Zapatas.EvaluarZapata(z, 1000, 0, 250, "EST")

        Dim f = ZapataService.FactoresPorCombinacion(z).First()
        ' ex = 250/1000 = 0.25, lim = 0.333, C/D = 0.333/0.25 = 1.333
        Assert.AreEqual(1.333, f.Excentricidad, 0.01, "C/D de excentricidad")
        Assert.IsTrue(f.Peor >= 0.001, "hay algún factor")
    End Sub

    ' =====================================================================
    ' Grupos de zapatas — patrón + hijas comparten geometría
    ' =====================================================================

    ''' <summary>Crea una zapata con propiedades particulares para verificar cambios.</summary>
    Private Shared Function ZapataDeReferencia(nombre As String, grupo As String) As cZapata
        Dim z = ZapataParaEvaluar()
        z.Label_joint = nombre
        z.Nombre = nombre
        z.Grupo = grupo
        Return z
    End Function

    <TestMethod>
    Public Sub Agrupar_SoloIncluyeLasQueTienenGrupo()
        Dim zapatas = New List(Of cZapata) From {
            ZapataDeReferencia("A", "Z1"),
            ZapataDeReferencia("B", "Z1"),
            ZapataDeReferencia("C", "Z2"),
            ZapataDeReferencia("D", "")
        }
        Dim grupos = ZapataService.AgruparZapatas(zapatas)
        Assert.AreEqual(2, grupos.Count, "solo Z1 y Z2, la sin grupo se omite")
        Assert.AreEqual(2, grupos("Z1").Count)
        Assert.AreEqual(1, grupos("Z2").Count)
    End Sub

    <TestMethod>
    Public Sub PatronDelGrupo_DevuelveLaMarcada()
        Dim a = ZapataDeReferencia("A", "Z1")
        Dim b = ZapataDeReferencia("B", "Z1") : b.EsPatron = True
        Dim c = ZapataDeReferencia("C", "Z1")
        Dim patron = ZapataService.PatronDelGrupo(New cZapata() {a, b, c})
        Assert.AreSame(b, patron)
    End Sub

    ''' <summary>
    ''' Si nadie está marcado, el fallback es la primera por Label_joint.
    ''' Se elige un orden estable (alfabético) en vez del orden de inserción
    ''' para que la vista sea la misma sin importar cómo se ordene la tabla.
    ''' </summary>
    <TestMethod>
    Public Sub PatronDelGrupo_FallbackAlfabetico()
        Dim c = ZapataDeReferencia("C", "Z1")
        Dim a = ZapataDeReferencia("A", "Z1")
        Dim b = ZapataDeReferencia("B", "Z1")
        Dim patron = ZapataService.PatronDelGrupo(New cZapata() {c, a, b})
        Assert.AreSame(a, patron, "debe elegir 'A' aunque no vino primero")
    End Sub

    <TestMethod>
    Public Sub AsegurarPatron_MarcaUnaSiNoHay()
        Dim zapatas = New List(Of cZapata) From {
            ZapataDeReferencia("A", "Z1"),
            ZapataDeReferencia("B", "Z1")
        }
        Dim cambios = ZapataService.AsegurarPatronPorGrupo(zapatas)
        Assert.AreEqual(1, cambios)
        Assert.IsTrue(zapatas.Any(Function(z) z.EsPatron), "debería quedar una marcada")
        Assert.AreEqual(1, zapatas.Where(Function(z) z.EsPatron).Count(), "exactamente una")
    End Sub

    <TestMethod>
    Public Sub AsegurarPatron_DesmarcaLasExtraSiVarias()
        Dim a = ZapataDeReferencia("A", "Z1") : a.EsPatron = True
        Dim b = ZapataDeReferencia("B", "Z1") : b.EsPatron = True
        Dim c = ZapataDeReferencia("C", "Z1") : c.EsPatron = True
        Dim zapatas = New List(Of cZapata) From {a, b, c}
        ZapataService.AsegurarPatronPorGrupo(zapatas)
        Assert.AreEqual(1, zapatas.Where(Function(z) z.EsPatron).Count(), "solo una queda")
    End Sub

    ''' <summary>
    ''' EsPatron marcado sobre una zapata SIN grupo es un estado inválido; se
    ''' limpia. Pasa cuando el usuario primero marca y después vacía el nombre
    ''' del grupo.
    ''' </summary>
    <TestMethod>
    Public Sub AsegurarPatron_LimpiaMarcaEnZapatasSinGrupo()
        Dim suelta = ZapataDeReferencia("X", "") : suelta.EsPatron = True
        Dim zapatas = New List(Of cZapata) From {suelta}
        ZapataService.AsegurarPatronPorGrupo(zapatas)
        Assert.IsFalse(suelta.EsPatron)
    End Sub

    <TestMethod>
    Public Sub SincronizarConPatron_CopiaGeometriaYRefuerzo()
        Dim patron = ZapataDeReferencia("P", "Z1")
        patron.L_b = 2.5 : patron.L_h = 3.0 : patron.e = 0.6 : patron.d = 0.525
        patron.b = 0.5 : patron.h = 0.5
        patron.fc = 28 : patron.fy = 420
        patron.Rho_L1 = 0.005 : patron.Rho_L2 = 0.006
        patron.Df = 2.0 : patron.gammaConcreto = 25.0

        Dim hija = ZapataDeReferencia("H", "Z1")
        hija.L_b = 1.0   ' distinta a propósito
        hija.CoordX = 5.5 : hija.CoordY = 3.3 : hija.TieneCoordenadas = True

        ZapataService.SincronizarConPatron(hija, patron)

        Assert.AreEqual(2.5, hija.L_b, 0.001, "L_b se copia")
        Assert.AreEqual(3.0, hija.L_h, 0.001)
        Assert.AreEqual(0.6, hija.e, 0.001)
        Assert.AreEqual(28, hija.fc, 0.001, "fc se copia")
        Assert.AreEqual(0.005, hija.Rho_L1, 0.0001)
        Assert.AreEqual(2.0, hija.Df, 0.001)
        Assert.AreEqual(25.0, hija.gammaConcreto, 0.001)
        Assert.AreEqual(5.5, hija.CoordX, 0.001, "coords no se tocan")
        Assert.IsTrue(hija.TieneCoordenadas, "TieneCoordenadas se preserva")
    End Sub

    ''' <summary>
    ''' El caso central del feature: cada apoyo del grupo se evalúa con SUS
    ''' propias combinaciones aunque comparta geometría con la patrón.
    ''' </summary>
    <TestMethod>
    Public Sub SincronizarTodosLosGrupos_MantieneCombinacionesYResultadosPorApoyo()
        Dim patron = ZapataDeReferencia("P", "Z1") : patron.EsPatron = True
        patron.L_b = 2.0 : patron.L_h = 2.0
        patron.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "COMB_P"})
        patron.Resultados("COMB_P") = New ResultadoZapata() With {.qMax = 100}

        Dim hija = ZapataDeReferencia("H", "Z1")
        hija.L_b = 999   ' distinta a propósito, para que la sincronización se note
        hija.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "COMB_H"})
        hija.Resultados("COMB_H") = New ResultadoZapata() With {.qMax = 250}

        ZapataService.SincronizarTodosLosGrupos(New List(Of cZapata) From {patron, hija})

        Assert.AreEqual(2.0, hija.L_b, 0.001, "geometría sincronizada")
        Assert.AreEqual(1, hija.Lista_Combinaciones_Estaticas.Count, "combinación propia intacta")
        Assert.AreEqual("COMB_H", hija.Lista_Combinaciones_Estaticas.First().LoadCase)
        Assert.AreEqual(250.0, hija.Resultados("COMB_H").qMax, 0.001, "resultados propios intactos")
        Assert.IsFalse(hija.Resultados.ContainsKey("COMB_P"), "no se contaminan resultados de la patrón")
    End Sub

    <TestMethod>
    Public Sub ResumirGrupo_ElijeElPeorEntreLosApoyos()
        Dim a = ZapataDeReferencia("A", "Z1") : a.EsPatron = True
        a.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "C1"})
        a.Resultados("C1") = New ResultadoZapata() With {
            .qMax = 100, .Vu_p = 500, .Vc_p = 1000,
            .Vu1_C = 100, .Vu3_C = 100, .Vc2_C = 200,
            .Vu2_C = 100, .Vu4_C = 100, .Vc1_C = 200,
            .Rho_1 = 0.002, .Rho_2 = 0.002
        }
        a.qAdm_Est = 200 : a.Rho_L1 = 0.004 : a.Rho_L2 = 0.004

        Dim b = ZapataDeReferencia("B", "Z1")
        b.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = "C1"})
        b.Resultados("C1") = New ResultadoZapata() With {
            .qMax = 400, .Vu_p = 500, .Vc_p = 1000,
            .Vu1_C = 100, .Vu3_C = 100, .Vc2_C = 200,
            .Vu2_C = 100, .Vu4_C = 100, .Vc1_C = 200,
            .Rho_1 = 0.002, .Rho_2 = 0.002
        }
        b.qAdm_Est = 200 : b.Rho_L1 = 0.004 : b.Rho_L2 = 0.004
        ' A: 200/100 = 2.00. B: 200/400 = 0.50. Gobierna B.

        Dim r = ZapataService.ResumirGrupo("Z1", New List(Of cZapata) From {a, b})

        Assert.IsTrue(r.TieneResultados)
        Assert.AreEqual(2, r.Cantidad)
        Assert.AreEqual(0.5, r.PeorFactor, 0.001, "peor factor del grupo")
        Assert.AreSame(b, r.PeorZapata, "apoyo que gobierna")
        Assert.IsFalse(r.Cumple, "0.50 no cumple")
    End Sub

End Class
