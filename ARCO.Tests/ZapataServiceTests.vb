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

End Class
