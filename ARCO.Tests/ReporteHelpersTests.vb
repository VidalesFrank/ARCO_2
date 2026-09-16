Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports ClosedXML.Excel
Imports ARCO

''' <summary>
''' Fija el comportamiento de ReporteHelpers, la clase que unificó los helpers
''' de exportación a Excel que estaban duplicados en seis formularios de reporte.
'''
''' Estas pruebas son la red de la unificación: los cuerpos originales no eran
''' copias iguales — EscribirFactor tenía seis variantes con tres convenciones
''' distintas de "sin dato", dos esquemas de color y dos umbrales. Cada prueba
''' comprueba una de esas configuraciones tal como la usaba su reporte, así que
''' si la centralización hubiera cambiado el resultado de alguno, falla.
''' </summary>
<TestClass>
Public Class ReporteHelpersTests

    Private _wb As XLWorkbook
    Private _ws As IXLWorksheet

    <TestInitialize>
    Public Sub Preparar()
        _wb = New XLWorkbook()
        _ws = _wb.Worksheets.Add("Prueba")
    End Sub

    <TestCleanup>
    Public Sub Limpiar()
        If _wb IsNot Nothing Then _wb.Dispose()
    End Sub

    Private Function Celda() As IXLCell
        Return _ws.Cell(1, 1)
    End Function

    ' =====================================================================
    ' EscribirFactor — configuración de Muros y Vigas de Fundación
    ' dos bandas, umbral 0.90, no contempla "sin dato"
    ' =====================================================================

    <TestMethod>
    Public Sub EscribirFactor_DosBandas_CumpleVaEnVerde()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.95)
        Assert.AreEqual(0.95, c.GetDouble(), 0.001)
        Assert.AreEqual(ReporteHelpers.XlOKFondo, c.Style.Fill.BackgroundColor)
        Assert.AreEqual(ReporteHelpers.XlOKTexto, c.Style.Font.FontColor)
    End Sub

    <TestMethod>
    Public Sub EscribirFactor_DosBandas_NoCumpleVaEnRojo()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.89)
        Assert.AreEqual(ReporteHelpers.XlMalFondo, c.Style.Fill.BackgroundColor)
        Assert.AreEqual(ReporteHelpers.XlMalTexto, c.Style.Font.FontColor)
    End Sub

    ''' <summary>Justo en el umbral, 0.90 cumple.</summary>
    <TestMethod>
    Public Sub EscribirFactor_EnElUmbralExacto_Cumple()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.9)
        Assert.AreEqual(ReporteHelpers.XlOKFondo, c.Style.Fill.BackgroundColor)
    End Sub

    ''' <summary>El valor se recorta a 9.99: un C/D enorme no aporta y descuadra la columna.</summary>
    <TestMethod>
    Public Sub EscribirFactor_RecortaEn999()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 1234.5)
        Assert.AreEqual(9.99, c.GetDouble(), 0.001)
    End Sub

    ''' <summary>
    ''' Sin convención de "sin dato" (Muros y Vigas de Fundación), un 0 se
    ''' escribe como 0.00 en rojo. Se fija tal cual para que la unificación no
    ''' lo cambie sin que nadie lo decida.
    ''' </summary>
    <TestMethod>
    Public Sub EscribirFactor_SinConvencion_ElCeroSeEscribeComoNumero()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.0)
        Assert.AreEqual(0.0, c.GetDouble(), 0.001)
        Assert.AreEqual(ReporteHelpers.XlMalFondo, c.Style.Fill.BackgroundColor)
    End Sub

    ' =====================================================================
    ' EscribirFactor — configuración de Nervios y Resumen de Vigas
    ' centinela Double.MaxValue
    ' =====================================================================

    <TestMethod>
    Public Sub EscribirFactor_CentinelaMaxValue_EscribeGuion()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, Double.MaxValue, ReporteHelpers.SinDato.MaxValue)
        Assert.AreEqual("-", c.GetString())
    End Sub

    <TestMethod>
    Public Sub EscribirFactor_CentinelaMaxValue_NoAfectaAlCero()
        ' Con esta convención el 0 SÍ es un valor: va como número, no como "-"
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.0, ReporteHelpers.SinDato.MaxValue)
        Assert.AreEqual(0.0, c.GetDouble(), 0.001)
    End Sub

    ' =====================================================================
    ' EscribirFactor — configuración de Zapatas y Proyecto Completo
    ' tres bandas, verde desde 1.0, ámbar entre 0.90 y 1.0, y "-" si <= 0
    ' =====================================================================

    <TestMethod>
    Public Sub EscribirFactor_TresBandas_SobreUnoVaEnVerde()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 1.05, ReporteHelpers.SinDato.CeroOMenor, umbralOK:=1.0, conBandaAlerta:=True)
        Assert.AreEqual(ReporteHelpers.XlOKFondo, c.Style.Fill.BackgroundColor)
    End Sub

    ''' <summary>Cumple, pero justo: banda ámbar. Es el caso que distingue este semáforo.</summary>
    <TestMethod>
    Public Sub EscribirFactor_TresBandas_EntreNoventaYUno_VaEnAmbar()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.95, ReporteHelpers.SinDato.CeroOMenor, umbralOK:=1.0, conBandaAlerta:=True)
        Assert.AreEqual(ReporteHelpers.XlAlertaFondo, c.Style.Fill.BackgroundColor)
        Assert.AreEqual(ReporteHelpers.XlAlertaTexto, c.Style.Font.FontColor)
    End Sub

    <TestMethod>
    Public Sub EscribirFactor_TresBandas_BajoNoventa_VaEnRojo()
        Dim c = Celda()
        ReporteHelpers.EscribirFactor(c, 0.80, ReporteHelpers.SinDato.CeroOMenor, umbralOK:=1.0, conBandaAlerta:=True)
        Assert.AreEqual(ReporteHelpers.XlMalFondo, c.Style.Fill.BackgroundColor)
    End Sub

    <TestMethod>
    Public Sub EscribirFactor_CeroOMenor_EscribeGuion()
        Dim c1 = _ws.Cell(1, 1)
        ReporteHelpers.EscribirFactor(c1, 0.0, ReporteHelpers.SinDato.CeroOMenor)
        Assert.AreEqual("-", c1.GetString(), "cero")

        Dim c2 = _ws.Cell(2, 1)
        ReporteHelpers.EscribirFactor(c2, -3.0, ReporteHelpers.SinDato.CeroOMenor)
        Assert.AreEqual("-", c2.GetString(), "negativo")
    End Sub

    ' =====================================================================
    ' EscribirEncabezados
    ' =====================================================================

    <TestMethod>
    Public Sub EscribirEncabezados_EscribeYEstiliza()
        ReporteHelpers.EscribirEncabezados(_ws, 1, New String() {"Piso", "Viga", "C/D"})
        Assert.AreEqual("Piso", _ws.Cell(1, 1).GetString())
        Assert.AreEqual("C/D", _ws.Cell(1, 3).GetString())
        Assert.AreEqual(ReporteHelpers.XlEncabezado, _ws.Cell(1, 1).Style.Fill.BackgroundColor)
        Assert.IsTrue(_ws.Cell(1, 1).Style.Font.Bold)
        Assert.AreEqual(22.0, _ws.Row(1).Height, 0.01)
        Assert.AreEqual(11.0, _ws.Cell(1, 1).Style.Font.FontSize, 0.01)
    End Sub

    ''' <summary>Zapatas y Proyecto Completo usan 10 pt y fila de 20.</summary>
    <TestMethod>
    Public Sub EscribirEncabezados_TamanoYAltoParametrizables()
        ReporteHelpers.EscribirEncabezados(_ws, 1, New String() {"A"}, tamanoFuente:=10, altoFila:=20)
        Assert.AreEqual(10.0, _ws.Cell(1, 1).Style.Font.FontSize, 0.01)
        Assert.AreEqual(20.0, _ws.Row(1).Height, 0.01)
    End Sub

    ''' <summary>Muros era el único reporte sin borde blanco entre encabezados.</summary>
    <TestMethod>
    Public Sub EscribirEncabezados_SinBorde_NoDibujaBorde()
        ReporteHelpers.EscribirEncabezados(_ws, 1, New String() {"A"}, conBorde:=False)
        Assert.AreNotEqual(XLBorderStyleValues.Thin, _ws.Cell(1, 1).Style.Border.TopBorder)
    End Sub

    ' =====================================================================
    ' AjustarColumnas
    ' =====================================================================

    <TestMethod>
    Public Sub AjustarColumnas_RecortaElAnchoMaximo()
        _ws.Cell(1, 1).Value = New String("X"c, 200)
        ReporteHelpers.AjustarColumnas(_ws, 1, anchoMaximo:=45)
        Assert.IsTrue(_ws.Column(1).Width <= 45.001, $"ancho fue {_ws.Column(1).Width}")
    End Sub

    ''' <summary>Muros recorta a 40 en vez de 45.</summary>
    <TestMethod>
    Public Sub AjustarColumnas_AnchoMaximoParametrizable()
        _ws.Cell(1, 1).Value = New String("X"c, 200)
        ReporteHelpers.AjustarColumnas(_ws, 1, anchoMaximo:=40)
        Assert.IsTrue(_ws.Column(1).Width <= 40.001, $"ancho fue {_ws.Column(1).Width}")
    End Sub

    ''' <summary>
    ''' Nervios y Resumen garantizan un mínimo en la columna de Frames de ETABS,
    ''' que trae etiquetas largas.
    ''' </summary>
    <TestMethod>
    Public Sub AjustarColumnas_GarantizaMinimoEnLaColumnaIndicada()
        _ws.Cell(1, 1).Value = "a"
        _ws.Cell(1, 2).Value = "b"
        _ws.Cell(1, 3).Value = "c"
        ReporteHelpers.AjustarColumnas(_ws, 3, columnaAncha:=3, anchoMinimo:=25)
        Assert.IsTrue(_ws.Column(3).Width >= 25.0, $"ancho fue {_ws.Column(3).Width}")
    End Sub

    <TestMethod>
    Public Sub AjustarColumnas_CongelaLaFilaDeEncabezado()
        _ws.Cell(1, 1).Value = "a"
        ReporteHelpers.AjustarColumnas(_ws, 1)
        Assert.AreEqual(1, _ws.SheetView.SplitRow)
    End Sub

    ' =====================================================================
    ' AgregarBordesTabla
    ' =====================================================================

    <TestMethod>
    Public Sub AgregarBordesTabla_RangoInvertido_NoHaceNada()
        ' filaFin < filaIni: no debe lanzar
        ReporteHelpers.AgregarBordesTabla(_ws, 5, 2, 3)
    End Sub

    <TestMethod>
    Public Sub AgregarBordesTabla_DibujaElContorno()
        ReporteHelpers.AgregarBordesTabla(_ws, 1, 3, 3)
        Assert.AreEqual(XLBorderStyleValues.Medium, _ws.Cell(1, 1).Style.Border.TopBorder)
    End Sub

    ' =====================================================================
    ' EscribirEstado
    ' =====================================================================

    <TestMethod>
    Public Sub EscribirEstado_Booleano_UsaElSemaforoEstandar()
        Dim ok = _ws.Cell(1, 1)
        ReporteHelpers.EscribirEstado(ok, True)
        Assert.AreEqual("OK", ok.GetString())
        Assert.AreEqual(ReporteHelpers.XlOKFondo, ok.Style.Fill.BackgroundColor)

        Dim mal = _ws.Cell(2, 1)
        ReporteHelpers.EscribirEstado(mal, False)
        Assert.AreEqual("Revisar", mal.GetString())
        Assert.AreEqual(ReporteHelpers.XlMalFondo, mal.Style.Fill.BackgroundColor)
    End Sub

End Class
