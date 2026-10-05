Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports System.Linq
Imports ARCO
Imports ARCO.eNumeradores

''' <summary>
''' Las dos mejoras del 2026-10-05 a la clasificación de apoyos.
'''
''' 1) MARCO DE LA MALLA. El criterio buscaba vecinos sobre los ejes X,Y
'''    GLOBALES con una tolerancia de 0.30 m. Un edificio girado respecto al
'''    origen de ETABS no tenía ningún par de apoyos alineado dentro de esa
'''    franja, así que TODAS las zapatas salían esquineras — phi-Vc al 37 % de
'''    una central. Ahora la dirección dominante se mide y la búsqueda se hace
'''    en ese marco.
'''
''' 2) ALCANCE. La distancia no entraba en la cuenta: un apoyo suelto a 50 m,
'''    por estar alineado, contaba como vecino y podía ascender a Central una
'''    zapata que estaba en el borde. Ese error es del lado NO conservador.
'''
''' El reparto sigue siendo el mismo de siempre, contado por lados libres:
''' 0 libres = Central, 1 = Medianera, 2 o más = Esquinera.
''' </summary>
<TestClass>
Public Class ZapataMallaTests

    Private Const LUZ As Double = 5.0

    ''' <summary>Malla 3 x 3 a 5 m, girada los grados que se pidan. Etiquetas "i-j".</summary>
    Private Shared Function Malla3x3(Optional grados As Double = 0) As List(Of cZapata)
        Dim lista As New List(Of cZapata)()
        Dim r As Double = grados * Math.PI / 180.0
        For i = 0 To 2
            For j = 0 To 2
                Dim x As Double = i * LUZ
                Dim y As Double = j * LUZ
                lista.Add(New cZapata() With {
                    .Label_joint = $"{i}-{j}",
                    .Nombre = $"Z{i}{j}",
                    .CoordX = x * Math.Cos(r) - y * Math.Sin(r),
                    .CoordY = x * Math.Sin(r) + y * Math.Cos(r),
                    .TieneCoordenadas = True
                })
            Next
        Next
        Return lista
    End Function

    Private Shared Function Buscar(lista As List(Of cZapata), i As Integer, j As Integer) As cZapata
        Return lista.First(Function(w) w.Label_joint = $"{i}-{j}")
    End Function

    ''' <summary>Comprueba el reparto de una malla 3 x 3, gire como gire.</summary>
    Private Shared Sub AssertRepartoTresPorTres(lista As List(Of cZapata), contexto As String)
        Assert.AreEqual(eTipoApoyoZapata.Central, Buscar(lista, 1, 1).TipoApoyo, contexto & " — centro")

        For Each ij In New Integer()() {New Integer() {1, 0}, New Integer() {0, 1},
                                        New Integer() {2, 1}, New Integer() {1, 2}}
            Assert.AreEqual(eTipoApoyoZapata.Medianera, Buscar(lista, ij(0), ij(1)).TipoApoyo,
                            $"{contexto} — borde ({ij(0)},{ij(1)})")
        Next

        For Each ij In New Integer()() {New Integer() {0, 0}, New Integer() {2, 0},
                                        New Integer() {0, 2}, New Integer() {2, 2}}
            Assert.AreEqual(eTipoApoyoZapata.Esquinera, Buscar(lista, ij(0), ij(1)).TipoApoyo,
                            $"{contexto} — esquina ({ij(0)},{ij(1)})")
        Next
    End Sub

    ' =====================================================================
    ' 1) Marco de la malla — edificios girados
    ' =====================================================================

    ''' <summary>Sin girar: la referencia, igual que antes del cambio.</summary>
    <TestMethod>
    Public Sub Clasificar_MallaSinGirar_RepartoDeSiempre()
        Dim lista = Malla3x3(0)
        ZapataService.ClasificarApoyos(lista)
        AssertRepartoTresPorTres(lista, "0°")
    End Sub

    ''' <summary>
    ''' LA MEJORA. Con 30° de giro, el criterio anterior daba las nueve
    ''' esquineras; el reparto tiene que ser idéntico al de la malla sin girar,
    ''' porque es el mismo edificio.
    ''' </summary>
    <TestMethod>
    Public Sub Clasificar_MallaGirada30_MismoRepartoQueSinGirar()
        Dim lista = Malla3x3(30)
        ZapataService.ClasificarApoyos(lista)
        AssertRepartoTresPorTres(lista, "30°")

        Assert.IsFalse(lista.All(Function(w) w.TipoApoyo = eTipoApoyoZapata.Esquinera),
                       "con el criterio viejo salían las nueve esquineras")
    End Sub

    ''' <summary>45° es el caso límite del módulo 90°: tiene que seguir dando igual.</summary>
    <TestMethod>
    Public Sub Clasificar_MallaGirada45_MismoReparto()
        Dim lista = Malla3x3(45)
        ZapataService.ClasificarApoyos(lista)
        AssertRepartoTresPorTres(lista, "45°")
    End Sub

    ''' <summary>Giros variados, incluido uno negativo y uno mayor de 90°.</summary>
    <TestMethod>
    Public Sub Clasificar_CualquierGiro_MismoReparto()
        For Each g In New Double() {7.5, -12.0, 63.4, 118.0, 180.0}
            Dim lista = Malla3x3(g)
            ZapataService.ClasificarApoyos(lista)
            AssertRepartoTresPorTres(lista, $"{g}°")
        Next
    End Sub

    <TestMethod>
    Public Sub AnalizarMalla_DetectaElAnguloDeLaNube()
        For Each g In New Double() {0.0, 15.0, 30.0, 45.0, 72.0}
            Dim info = ZapataService.AnalizarMalla(Malla3x3(g))
            Assert.AreEqual(g, info.AnguloGrados, 0.01, $"ángulo medido para {g}°")
            Assert.AreEqual("apoyos", info.Origen)
            Assert.AreEqual(1.0, info.Coherencia, 0.01, "una malla ortogonal regular es totalmente coherente")
        Next
    End Sub

    ''' <summary>El ángulo se reporta siempre en [0, 90): 120° es la misma malla que 30°.</summary>
    <TestMethod>
    Public Sub AnalizarMalla_AnguloSiempreEnElPrimerCuadrante()
        Dim info = ZapataService.AnalizarMalla(Malla3x3(120))
        Assert.AreEqual(30.0, info.AnguloGrados, 0.01)
    End Sub

    ' =====================================================================
    ' 2) Alcance — el vecino lejano ya no cuenta
    ' =====================================================================

    ''' <summary>
    ''' LA SEGUNDA MEJORA, y la del lado peligroso. Un apoyo suelto a 50 m,
    ''' alineado con la columna central, hacía que la zapata del borde superior
    ''' tuviera "vecino arriba" y saliera Central: perímetro cerrado y alfa_s 40
    ''' para una zapata que está en el borde del edificio.
    ''' </summary>
    <TestMethod>
    Public Sub Clasificar_ApoyoLejanoNoAsciendeElBordeACentral()
        Dim lista = Malla3x3(0)
        lista.Add(New cZapata() With {
            .Label_joint = "suelta", .Nombre = "Zs",
            .CoordX = LUZ, .CoordY = 60.0, .TieneCoordenadas = True})

        ZapataService.ClasificarApoyos(lista)

        ' El hueco de 50 m no se cuenta como vano: si se contara, el alcance se
        ' estiraría hasta la suelta y volveríamos al error.
        Assert.AreEqual(5.0, ZapataService.AnalizarMalla(lista).LuzMaxima, 0.001,
                        "el vano ancho sigue siendo 5 m, no 50")
        Assert.AreEqual(eTipoApoyoZapata.Medianera, Buscar(lista, 1, 2).TipoApoyo,
                        "el borde superior sigue siendo borde: lo de arriba está a 50 m")
        AssertRepartoTresPorTres(lista, "con apoyo suelto")
    End Sub

    ''' <summary>La suelta no tiene vecinos al alcance: cuatro lados libres, esquinera.</summary>
    <TestMethod>
    Public Sub Clasificar_ApoyoAisladoEsEsquinero()
        Dim lista = Malla3x3(0)
        Dim suelta As New cZapata() With {
            .Label_joint = "suelta", .Nombre = "Zs",
            .CoordX = LUZ, .CoordY = 60.0, .TieneCoordenadas = True}
        lista.Add(suelta)

        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Esquinera, suelta.TipoApoyo)
    End Sub

    ''' <summary>
    ''' El alcance se mide de la propia malla, no es un número fijo: una malla
    ''' de 20 m de luz no se puede leer con el alcance de una de 5 m.
    ''' </summary>
    <TestMethod>
    Public Sub AnalizarMalla_LaEscalaSaleDeLaMalla()
        Dim info5 = ZapataService.AnalizarMalla(Malla3x3(0))
        Assert.AreEqual(5.0, info5.SeparacionTipica, 0.001)
        Assert.AreEqual(5.0, info5.LuzTipica, 0.001)
        Assert.AreEqual(2.5, info5.Banda, 0.001, "media luz")
        Assert.AreEqual(7.5, info5.Alcance, 0.001, "1.5 luces")

        ' La misma malla al cuádruple de luz: banda y alcance se escalan.
        Dim grande = Malla3x3(0)
        For Each w In grande
            w.CoordX *= 4 : w.CoordY *= 4
        Next
        Dim info20 = ZapataService.AnalizarMalla(grande)
        Assert.AreEqual(20.0, info20.SeparacionTipica, 0.001)
        Assert.AreEqual(20.0, info20.LuzTipica, 0.001)
        Assert.AreEqual(10.0, info20.Banda, 0.001)
        Assert.AreEqual(30.0, info20.Alcance, 0.001)
    End Sub

    ''' <summary>
    ''' Con luces mezcladas el alcance se toma de la luz grande (percentil 90),
    ''' no de la mediana: si no, el vano ancho de una malla irregular quedaría
    ''' fuera de alcance y los apoyos interiores saldrían como de borde.
    ''' </summary>
    <TestMethod>
    Public Sub AnalizarMalla_LucesMezcladas_ElAlcanceCubreLaLuzGrande()
        ' Vanos de 3 m y de 9 m en X; 3 m en Y.
        Dim lista As New List(Of cZapata)()
        For Each x In New Double() {0, 3, 12}
            For Each y In New Double() {0, 3}
                lista.Add(New cZapata() With {
                    .Label_joint = $"{x}-{y}", .CoordX = x, .CoordY = y, .TieneCoordenadas = True})
            Next
        Next

        Dim info = ZapataService.AnalizarMalla(lista)

        Assert.AreEqual(3.0, info.SeparacionTipica, 0.001, "el vecino más cercano está a 3 m")
        Assert.AreEqual(9.0, info.LuzMaxima, 0.001, "el vano ancho es de 9 m")
        Assert.IsTrue(info.Alcance >= 9.0,
                      $"el alcance ({info.Alcance:0.00} m) debe cubrir el vano de 9 m")
    End Sub

    ' =====================================================================
    ' Banda: desalineamiento real de columnas
    ' =====================================================================

    ''' <summary>
    ''' Una columna corrida 2 m dentro de un vano de 5 m sigue siendo de su fila.
    ''' Con la banda fija de 0.30 m, la central de la malla 3 x 3 salía ESQUINERA
    ''' por estar desalineada: perdía el 63 % de su capacidad a punzonamiento.
    ''' </summary>
    <TestMethod>
    Public Sub Clasificar_ColumnaCorridaDentroDelVano_SigueSiendoDeSuFila()
        Dim lista = Malla3x3(0)
        Buscar(lista, 1, 1).CoordX = LUZ + 2.0

        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Central, Buscar(lista, 1, 1).TipoApoyo)
    End Sub

    ''' <summary>Los 5 cm de desfase de un modelo real, que ya estaban cubiertos.</summary>
    <TestMethod>
    Public Sub Clasificar_DesfasePequeno_NoCambiaNada()
        Dim lista = Malla3x3(0)
        Buscar(lista, 1, 1).CoordX = LUZ + 0.05

        ZapataService.ClasificarApoyos(lista)

        AssertRepartoTresPorTres(lista, "con 5 cm de desfase")
    End Sub

    ' =====================================================================
    ' De dónde sale el ángulo: nube, ejes o forzado
    ' =====================================================================

    Private Shared Function EjeGeneral(x1 As Double, y1 As Double,
                                       x2 As Double, y2 As Double) As cGridLine
        Return New cGridLine() With {
            .Direction = "G", .GridID = "G1", .Visible = True,
            .X1 = x1, .Y1 = y1, .X2 = x2, .Y2 = y2}
    End Function

    ''' <summary>
    ''' La nube manda cuando es coherente. Un eje tipo General puede ser
    ''' cualquier cosa — una rampa, una diagonal de fachada — y no debe
    ''' secuestrar el marco de una malla que está clara.
    ''' </summary>
    <TestMethod>
    Public Sub AnalizarMalla_LaNubeManaSobreLosEjesCuandoEsCoherente()
        Dim ejes As New List(Of cGridLine) From {EjeGeneral(0, 0, 10, 5.77)}  ' ~30°

        Dim info = ZapataService.AnalizarMalla(Malla3x3(0), ejes)

        Assert.AreEqual("apoyos", info.Origen)
        Assert.AreEqual(0.0, info.AnguloGrados, 0.01)
    End Sub

    ''' <summary>
    ''' Dos bloques con orientaciones distintas: la nube no define dirección
    ''' (coherencia baja) y entonces sí se usan los ejes.
    ''' </summary>
    <TestMethod>
    Public Sub AnalizarMalla_SinMallaClaraUsaLosEjes()
        ' Un par alineado a 0° y otro par a 45°: las direcciones se cancelan.
        Dim lista As New List(Of cZapata) From {
            New cZapata() With {.Label_joint = "A", .CoordX = 0, .CoordY = 0, .TieneCoordenadas = True},
            New cZapata() With {.Label_joint = "B", .CoordX = 2, .CoordY = 0, .TieneCoordenadas = True},
            New cZapata() With {.Label_joint = "C", .CoordX = 50, .CoordY = 50, .TieneCoordenadas = True},
            New cZapata() With {.Label_joint = "D", .CoordX = 51.414, .CoordY = 51.414, .TieneCoordenadas = True}
        }
        Dim sinEjes = ZapataService.AnalizarMalla(lista)
        Assert.IsTrue(sinEjes.Coherencia < ZapataService.COHERENCIA_MINIMA,
                      $"la coherencia debía salir baja, salió {sinEjes.Coherencia:0.00}")
        Assert.AreEqual("global", sinEjes.Origen, "sin ejes se queda en los ejes globales")

        Dim ejes As New List(Of cGridLine) From {EjeGeneral(0, 0, 20, 11.547)}  ' 30°
        Dim conEjes = ZapataService.AnalizarMalla(lista, ejes)
        Assert.AreEqual("ejes", conEjes.Origen)
        Assert.AreEqual(30.0, conEjes.AnguloGrados, 0.1)
    End Sub

    ''' <summary>Los ejes tipo X e Y no traen dirección: no deben influir.</summary>
    <TestMethod>
    Public Sub AnalizarMalla_LosEjesXYNoAportanDireccion()
        Dim ejes As New List(Of cGridLine) From {
            New cGridLine() With {.Direction = "X", .GridID = "1", .Ordinate = 0, .Visible = True},
            New cGridLine() With {.Direction = "Y", .GridID = "A", .Ordinate = 0, .Visible = True}
        }
        Dim lista As New List(Of cZapata) From {
            New cZapata() With {.Label_joint = "A", .CoordX = 0, .CoordY = 0, .TieneCoordenadas = True},
            New cZapata() With {.Label_joint = "B", .CoordX = 2, .CoordY = 0, .TieneCoordenadas = True},
            New cZapata() With {.Label_joint = "C", .CoordX = 50, .CoordY = 50, .TieneCoordenadas = True},
            New cZapata() With {.Label_joint = "D", .CoordX = 51.414, .CoordY = 51.414, .TieneCoordenadas = True}
        }

        Assert.AreEqual("global", ZapataService.AnalizarMalla(lista, ejes).Origen)
    End Sub

    ''' <summary>El ángulo a mano manda sobre todo lo demás.</summary>
    <TestMethod>
    Public Sub AnalizarMalla_AnguloForzadoManda()
        Dim info = ZapataService.AnalizarMalla(Malla3x3(0), Nothing, 30.0)
        Assert.AreEqual("forzado", info.Origen)
        Assert.AreEqual(30.0, info.AnguloGrados, 0.001)
    End Sub

    ''' <summary>
    ''' La banda de media luz hace que el marco no tenga que ser exacto: con
    ''' 10° de error en el ángulo, una malla ortogonal se sigue leyendo igual.
    ''' Importa porque el ángulo se MIDE, y medirlo con unos grados de error en
    ''' una planta irregular no debe mover la clasificación.
    ''' </summary>
    <TestMethod>
    Public Sub Clasificar_ErrorModeradoDeAngulo_NoCambiaElReparto()
        Dim lista = Malla3x3(0)
        ZapataService.ClasificarApoyos(lista, 0.3, Nothing, 10.0)
        AssertRepartoTresPorTres(lista, "marco con 10° de error")
    End Sub

    ' =====================================================================
    ' Planta en L — una forma real, no una malla de libro
    ' =====================================================================

    ''' <summary>
    ''' Brazo vertical de dos columnas de ancho y brazo horizontal de una sola.
    ''' Interesa el nodo (5,5): está metido en la L y tiene vecinos a izquierda,
    ''' arriba y abajo, pero nada a la derecha — es borde, y así sale.
    '''
    ''' En el brazo de una sola columna de ancho todos salen esquineros: tienen
    ''' dos lados libres opuestos. Es la misma limitación del plano degenerado
    ''' que ya estaba documentada, y queda del lado conservador.
    ''' </summary>
    <TestMethod>
    Public Sub Clasificar_PlantaEnL()
        Dim pares = New Double()() {
            New Double() {0, 0}, New Double() {5, 0}, New Double() {10, 0}, New Double() {15, 0},
            New Double() {0, 5}, New Double() {5, 5},
            New Double() {0, 10}, New Double() {5, 10}
        }
        Dim lista As New List(Of cZapata)()
        For Each pr In pares
            lista.Add(New cZapata() With {
                .Label_joint = $"{pr(0)}-{pr(1)}", .CoordX = pr(0), .CoordY = pr(1), .TieneCoordenadas = True})
        Next

        ZapataService.ClasificarApoyos(lista)

        Dim En = Function(x As Double, y As Double) lista.First(
            Function(w) w.CoordX = x AndAlso w.CoordY = y).TipoApoyo

        Assert.AreEqual(eTipoApoyoZapata.Medianera, En(5, 5), "el nodo interior de la L es borde")
        Assert.AreEqual(eTipoApoyoZapata.Medianera, En(5, 0), "borde inferior del brazo vertical")
        Assert.AreEqual(eTipoApoyoZapata.Esquinera, En(0, 0), "esquina")
        Assert.AreEqual(eTipoApoyoZapata.Esquinera, En(15, 0), "punta del brazo")
        Assert.AreEqual(eTipoApoyoZapata.Esquinera, En(10, 0), "brazo de una columna de ancho: conservador")
    End Sub

    ' =====================================================================
    ' Lo que no debe cambiar
    ' =====================================================================

    ''' <summary>Una corrección manual sigue sobreviviendo, gire la malla o no.</summary>
    <TestMethod>
    Public Sub Clasificar_MallaGirada_NoPisaLoMarcadoAMano()
        Dim lista = Malla3x3(30)
        Dim centro = Buscar(lista, 1, 1)
        centro.TipoApoyo = eTipoApoyoZapata.Esquinera
        centro.TipoApoyoManual = True

        ZapataService.ClasificarApoyos(lista)

        Assert.AreEqual(eTipoApoyoZapata.Esquinera, centro.TipoApoyo)
    End Sub

    <TestMethod>
    Public Sub Clasificar_MallaGirada_EsIdempotente()
        Dim lista = Malla3x3(30)
        Assert.IsTrue(ZapataService.ClasificarApoyos(lista) > 0, "la primera pasada clasifica")
        Assert.AreEqual(0, ZapataService.ClasificarApoyos(lista), "la segunda no cambia nada")
    End Sub

    ''' <summary>Sin coordenadas no hay malla que medir y no se toca nada.</summary>
    <TestMethod>
    Public Sub AnalizarMalla_SinApoyosNoRevienta()
        Dim vacia = ZapataService.AnalizarMalla(New List(Of cZapata)())
        Assert.AreEqual(0, vacia.Apoyos)
        Assert.AreEqual(0.0, vacia.AnguloGrados, 0.001)

        Dim nula = ZapataService.AnalizarMalla(Nothing)
        Assert.AreEqual(0, nula.Apoyos)
    End Sub

    ''' <summary>El texto de la malla es lo que va al log y al mensaje: debe decir el ángulo.</summary>
    <TestMethod>
    Public Sub MallaInfo_SeDescribeEnTexto()
        Dim txt = ZapataService.AnalizarMalla(Malla3x3(30)).ToString()
        Assert.IsTrue(txt.Contains("30"), txt)
        Assert.IsTrue(txt.Contains("apoyos"), txt)
    End Sub

End Class
