Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports ARCO

''' <summary>
''' Pruebas del cálculo de losas nervadas por el método de coeficientes,
''' extraído de Form_03_Losas.Button2_Click.
'''
''' La tabla de casos se trasladó mecánicamente desde el formulario y se
''' verificó comparando las 162 combinaciones posibles (3 estados por borde,
''' elevado a 4 bordes, por 2 orientaciones) contra el código original tomado
''' de git: cero diferencias. Estas pruebas fijan los casos representativos
''' para que esa equivalencia no se pierda con el tiempo.
''' </summary>
<TestClass>
Public Class LosaServiceTests

    Private Const C As LosaService.Borde = LosaService.Borde.Continuo
    Private Const D As LosaService.Borde = LosaService.Borde.Discontinuo
    Private Const S As LosaService.Borde = LosaService.Borde.SinDefinir

    ' =====================================================================
    ' Tabla de casos
    ' =====================================================================

    ''' <summary>Los dos casos que no dependen de la orientación.</summary>
    <TestMethod>
    Public Sub DeterminarCaso_TodosDiscontinuosEsUno_TodosContinuosEsDos()
        Assert.AreEqual(1, LosaService.DeterminarCaso(D, D, D, D, ladoCortoEsX:=False), "todos discontinuos, op 1")
        Assert.AreEqual(1, LosaService.DeterminarCaso(D, D, D, D, ladoCortoEsX:=True), "todos discontinuos, op 2")
        Assert.AreEqual(2, LosaService.DeterminarCaso(C, C, C, C, ladoCortoEsX:=False), "todos continuos, op 1")
        Assert.AreEqual(2, LosaService.DeterminarCaso(C, C, C, C, ladoCortoEsX:=True), "todos continuos, op 2")
    End Sub

    ''' <summary>
    ''' Caso 3: dos bordes opuestos continuos. Cuál combinación es el caso 3
    ''' depende de cuál lado es el corto — por eso importa la orientación.
    ''' </summary>
    <TestMethod>
    Public Sub DeterminarCaso_TresDependeDeLaOrientacion()
        Assert.AreEqual(3, LosaService.DeterminarCaso(C, D, C, D, ladoCortoEsX:=False))
        Assert.AreEqual(3, LosaService.DeterminarCaso(D, C, D, C, ladoCortoEsX:=True))
    End Sub

    ''' <summary>
    ''' La misma combinación da caso distinto según la orientación. Es la
    ''' propiedad que hace que la tabla no se pueda simplificar ignorando el
    ''' lado corto, y la que más fácil se rompería en un refactor.
    ''' </summary>
    <TestMethod>
    Public Sub DeterminarCaso_LaOrientacionCambiaElResultado()
        Dim conOp1 = LosaService.DeterminarCaso(C, D, C, D, ladoCortoEsX:=False)
        Dim conOp2 = LosaService.DeterminarCaso(C, D, C, D, ladoCortoEsX:=True)
        Assert.AreEqual(3, conOp1)
        Assert.AreEqual(5, conOp2)
        Assert.AreNotEqual(conOp1, conOp2)
    End Sub

    ''' <summary>Caso 4: dos bordes adyacentes continuos, en sus cuatro giros.</summary>
    <TestMethod>
    Public Sub DeterminarCaso_CuatroEnSusCuatroGiros()
        Assert.AreEqual(4, LosaService.DeterminarCaso(C, D, D, C, ladoCortoEsX:=False))
        Assert.AreEqual(4, LosaService.DeterminarCaso(C, C, D, D, ladoCortoEsX:=False))
        Assert.AreEqual(4, LosaService.DeterminarCaso(D, D, C, C, ladoCortoEsX:=False))
        Assert.AreEqual(4, LosaService.DeterminarCaso(D, C, C, D, ladoCortoEsX:=False))
    End Sub

    <TestMethod>
    Public Sub DeterminarCaso_UnSoloBordeContinuo()
        ' Caso 6 y 7: un borde continuo, según cuál y la orientación
        Assert.AreEqual(6, LosaService.DeterminarCaso(D, C, D, D, ladoCortoEsX:=False))
        Assert.AreEqual(7, LosaService.DeterminarCaso(C, D, D, D, ladoCortoEsX:=False))
    End Sub

    <TestMethod>
    Public Sub DeterminarCaso_UnSoloBordeDiscontinuo()
        ' Caso 8 y 9: tres continuos y uno discontinuo
        Assert.AreEqual(8, LosaService.DeterminarCaso(C, D, C, C, ladoCortoEsX:=False))
        Assert.AreEqual(9, LosaService.DeterminarCaso(D, C, C, C, ladoCortoEsX:=False))
    End Sub

    ''' <summary>
    ''' Un borde sin marcar no corresponde a ningún caso: devuelve 0. Reproduce
    ''' lo que pasaba antes cuando ninguna rama de la cadena aplicaba.
    ''' </summary>
    <TestMethod>
    Public Sub DeterminarCaso_BordeSinDefinir_DevuelveCero()
        Assert.AreEqual(0, LosaService.DeterminarCaso(S, C, C, C, ladoCortoEsX:=False))
        Assert.AreEqual(0, LosaService.DeterminarCaso(D, D, D, S, ladoCortoEsX:=True))
    End Sub

    ' =====================================================================
    ' Peso propio
    ' =====================================================================

    ''' <summary>
    ''' Losa nervada de 0.05 m de loseta, nervio de 0.30 × 0.10, separación
    ''' 0.50 × 0.50. Verificado a mano:
    '''   módulo    0.60 × 0.60 = 0.36 m²
    '''   loseta    0.05 × 0.36               = 0.0180 m³
    '''   nervios   0.10 × 1.10 × 0.25        = 0.0275 m³
    '''   peso      24 × 0.0455 / 0.36        = 3.0333 kN/m²
    ''' </summary>
    <TestMethod>
    Public Sub PesoPropioLosa_CasoVerificadoAMano()
        Dim p = LosaService.PesoPropioLosa(0.05, 0.3, 0.1, 0.5, 0.5)
        Assert.AreEqual(3.0333, p, 0.001)
    End Sub

    <TestMethod>
    Public Sub PesoPropioLosa_MasEspesorPesaMas()
        Dim delgada = LosaService.PesoPropioLosa(0.05, 0.3, 0.1, 0.5, 0.5)
        Dim gruesa = LosaService.PesoPropioLosa(0.08, 0.3, 0.1, 0.5, 0.5)
        Assert.IsTrue(gruesa > delgada, $"{gruesa} debería superar a {delgada}")
    End Sub

    ''' <summary>Geometría degenerada: no debe dividir por cero.</summary>
    <TestMethod>
    Public Sub PesoPropioLosa_ModuloNulo_DevuelveCero()
        Assert.AreEqual(0.0, LosaService.PesoPropioLosa(0.05, 0.3, 0.0, 0.0, 0.5), 0.0001)
    End Sub

    ' =====================================================================
    ' Carga última
    ' =====================================================================

    <TestMethod>
    Public Sub CargaUltima_AplicaLaCombinacionDeLaNorma()
        ' 1.2 × 5 + 1.6 × 2 = 9.2
        Assert.AreEqual(9.2, LosaService.CargaUltima(5.0, 2.0), 0.0001)
    End Sub

    ' =====================================================================
    ' Demandas
    ' =====================================================================

    ''' <summary>
    ''' Con coeficientes unitarios los momentos se reducen a fórmulas simples,
    ''' así que el resultado se puede verificar sin depender de la tabla:
    '''   Ma- = 1 × cu × Lna² × (Sa + tw) = 9.2 × 16 × 0.6 = 88.32
    '''   Va  = 1 × cu × Lna / 2 × (Sa + tw) = 9.2 × 2 × 0.6 = 11.04
    ''' </summary>
    <TestMethod>
    Public Sub CalcularDemandas_CasoVerificadoAMano()
        Dim coef = New Single() {1, 1, 1, 1, 1, 1, 1, 1}
        Dim d = LosaService.CalcularDemandasFranjaCentral(coef, cargaMuerta:=5, cargaViva:=2,
                                                          lna:=4, lnb:=5, sa:=0.5, sb:=0.5,
                                                          anchoNervio:=0.1)
        Assert.AreEqual(88.32, d.Ma_Neg, 0.01, "Ma-")
        Assert.AreEqual(11.04, d.Va, 0.01, "Va")
        ' Mb- = 9.2 × 25 × 0.6 = 138.0
        Assert.AreEqual(138.0, d.Mb_Neg, 0.01, "Mb-")
    End Sub

    ''' <summary>
    ''' Los momentos positivos separan muerta y viva porque llevan coeficientes
    ''' distintos: (CaD × 1.2 CM + CaL × 1.6 CV) × Lna² × ancho.
    ''' Con CaD = 1 y CaL = 0 solo debe entrar la muerta: 1.2 × 5 × 16 × 0.6 = 57.6
    ''' </summary>
    <TestMethod>
    Public Sub CalcularDemandas_ElPositivoSeparaMuertaDeViva()
        Dim coef = New Single() {0, 0, 1, 0, 0, 0, 0, 0}   ' solo CaD
        Dim d = LosaService.CalcularDemandasFranjaCentral(coef, 5, 2, 4, 5, 0.5, 0.5, 0.1)
        Assert.AreEqual(57.6, d.Ma_Pos, 0.01)
    End Sub

    <TestMethod>
    Public Sub CalcularDemandas_CoeficientesInvalidos_DevuelveCeros()
        Dim d1 = LosaService.CalcularDemandasFranjaCentral(Nothing, 5, 2, 4, 5, 0.5, 0.5, 0.1)
        Assert.AreEqual(0.0, d1.Ma_Neg, 0.0001, "coeficientes nulos")

        Dim d2 = LosaService.CalcularDemandasFranjaCentral(New Single() {1, 2, 3}, 5, 2, 4, 5, 0.5, 0.5, 0.1)
        Assert.AreEqual(0.0, d2.Ma_Neg, 0.0001, "arreglo corto")
    End Sub

    ''' <summary>La franja de borde toma un tercio de la central.</summary>
    <TestMethod>
    Public Sub ATercios_DivideTodoEntreTres()
        Dim coef = New Single() {1, 1, 1, 1, 1, 1, 1, 1}
        Dim central = LosaService.CalcularDemandasFranjaCentral(coef, 5, 2, 4, 5, 0.5, 0.5, 0.1)
        Dim borde = LosaService.ATercios(central)

        Assert.AreEqual(central.Ma_Neg / 3.0, borde.Ma_Neg, 0.0001, "Ma-")
        Assert.AreEqual(central.Mb_Pos / 3.0, borde.Mb_Pos, 0.0001, "Mb+")
        Assert.AreEqual(central.Vb / 3.0, borde.Vb, 0.0001, "Vb")
    End Sub

End Class
