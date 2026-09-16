Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Reflection
Imports ClosedXML.Excel
Imports ARCO

''' <summary>
''' Regresión del reporte de zapatas.
'''
''' El detalle pasaba las presiones de contacto (g1..g8, gf, ga, gi, ge — kN/m²)
''' por el helper de factores C/D. Ese helper recorta en 9.99, así que en el
''' Excel exportado cualquier presión real salía escrita como 9.99: el dato era
''' falso. Y una presión negativa, que es tracción bajo la zapata, se escribía
''' como "-" por la convención de "sin dato" de los factores.
'''
''' Estas pruebas construyen el libro de verdad — llamando a los mismos métodos
''' de exportación que usa el botón — y leen las celdas.
''' </summary>
<TestClass>
Public Class ReporteZapatasTests

    Private Const COMBO As String = "1.2D+1.6L"

    ''' <summary>
    ''' Una zapata con presiones del orden real: 183 kN/m² en la esquina más
    ''' cargada y una tracción de -12.5 en la opuesta.
    ''' </summary>
    Private Shared Function ProyectoDeUnaZapata() As Proyecto

        Dim z As New cZapata() With {
            .Nombre = "Z-1", .Label_joint = "12",
            .L_b = 2.0, .L_h = 2.0, .e = 0.5, .d = 0.45, .b = 0.4, .h = 0.4, .fc = 21,
            .qAdm_Est = 200, .qAdm_Din = 300, .Rho_L1 = 0.004, .Rho_L2 = 0.004
        }
        z.Lista_Combinaciones_Estaticas.Add(New cCombinacionPila() With {.LoadCase = COMBO})

        z.Resultados(COMBO) = New ResultadoZapata() With {
            .qMax = 183.47, .qMin = -12.5,
            .g1 = 183.47, .g2 = 150.0, .g3 = -12.5, .g4 = 120.0,
            .g5 = 160.0, .g6 = 150.0, .g7 = 140.0, .g8 = 130.0,
            .gf_C = 170.0, .ga_C = 140.0, .gi_C = 100.0, .ge_C = 90.0,
            .gf_F = 175.0, .ga_F = 145.0, .gi_F = 105.0, .ge_F = 95.0,
            .Vu_p = 500, .Vc_p = 1000,
            .Vu1_C = 100, .Vu3_C = 100, .Vc2_C = 200,
            .Vu2_C = 100, .Vu4_C = 100, .Vc1_C = 200,
            .Rho_1 = 0.002, .Rho_2 = 0.002,
            .CumpleCapacidad = False, .CumplePunzonamiento = True,
            .CumpleCortante_1 = True, .CumpleCortante_2 = True,
            .CumpleCortante_3 = True, .CumpleCortante_4 = True,
            .Cumple_L1 = True, .Cumple_L2 = True, .CumpleGeneral = False
        }

        Dim p As New Proyecto()
        p.Elementos.Zapatas.Tipos.Add(z)
        Return p

    End Function

    ''' <summary>
    ''' Arma el libro llamando a los métodos privados de exportación: son los
    ''' mismos que corre el botón "Exportar a Excel", así que la prueba cubre lo
    ''' que el usuario recibe, no una reimplementación.
    ''' </summary>
    Private Shared Function Exportar(hoja As String) As IXLWorksheet

        Dim proy = ProyectoDeUnaZapata()
        Dim frm As New Form_Reporte_Zapatas(proy)
        Dim wb As New XLWorkbook()

        Dim flags = BindingFlags.Instance Or BindingFlags.NonPublic
        Dim args = New Object() {wb, proy.Elementos.Zapatas.Tipos}
        frm.GetType().GetMethod("ExportarHojaResumen", flags).Invoke(frm, args)
        frm.GetType().GetMethod("ExportarHojaDetalle", flags).Invoke(frm, args)

        frm.Dispose()
        Return wb.Worksheet(hoja)

    End Function

    Private Shared Function ColumnaDe(ws As IXLWorksheet, encabezado As String) As Integer
        For c As Integer = 1 To 40
            If ws.Cell(1, c).GetString() = encabezado Then Return c
        Next
        Assert.Fail($"No existe la columna '{encabezado}'")
        Return 0
    End Function

    ' =====================================================================
    ' Detalle por combinación
    ' =====================================================================

    ''' <summary>
    ''' EL bug. Una presión de 183.47 kN/m² tiene que salir 183.47, no 9.99.
    ''' </summary>
    <TestMethod>
    Public Sub Detalle_LaPresionMaximaNoSeRecortaEn999()
        Dim ws = Exportar("Detalle Combinaciones")
        Dim c = ColumnaDe(ws, "qMax (kN/m2)")
        Assert.AreEqual(183.47, ws.Cell(2, c).GetDouble(), 0.001)
    End Sub

    ''' <summary>
    ''' Tracción bajo la zapata: se escribe con su signo, no como "-". Es una de
    ''' las dos condiciones de la revisión de capacidad (qMin >= 0), o sea
    ''' exactamente el dato que hay que ver.
    ''' </summary>
    <TestMethod>
    Public Sub Detalle_LaTraccionSeEscribeConSuSigno()
        Dim ws = Exportar("Detalle Combinaciones")
        Dim c = ColumnaDe(ws, "qMin (kN/m2)")
        Assert.AreEqual(-12.5, ws.Cell(2, c).GetDouble(), 0.001)
        Assert.AreEqual(ReporteHelpers.XlAlertaFondo, ws.Cell(2, c).Style.Fill.BackgroundColor,
                        "debería quedar resaltada")
    End Sub

    ''' <summary>Las presiones de las secciones críticas, tampoco recortadas.</summary>
    <TestMethod>
    Public Sub Detalle_LasPresionesDeLasSeccionesCriticas()
        Dim ws = Exportar("Detalle Combinaciones")

        ' media de g5..g8 = (160+150+140+130)/4 = 145
        Assert.AreEqual(145.0, ws.Cell(2, ColumnaDe(ws, "q media perim. punz. (kN/m2)")).GetDouble(), 0.001, "punzonamiento")
        ' la mayor en magnitud de gf_C..ge_C
        Assert.AreEqual(170.0, ws.Cell(2, ColumnaDe(ws, "q seccion cortante (kN/m2)")).GetDouble(), 0.001, "cortante")
        Assert.AreEqual(175.0, ws.Cell(2, ColumnaDe(ws, "q cara pedestal (kN/m2)")).GetDouble(), 0.001, "flexión")
    End Sub

    ''' <summary>
    ''' Las columnas de C/D sí son C/D y sí se recortan: suelo 200/183.47 = 1.09,
    ''' punzonamiento 1000/500 = 2.00, cortante 200/100 = 2.00,
    ''' flexión 0.004/0.002 = 2.00.
    ''' </summary>
    <TestMethod>
    Public Sub Detalle_LasColumnasDeCDSonRelacionesDeVerdad()
        Dim ws = Exportar("Detalle Combinaciones")
        Assert.AreEqual(1.09, ws.Cell(2, ColumnaDe(ws, "C/D suelo")).GetDouble(), 0.01, "suelo")
        Assert.AreEqual(2.0, ws.Cell(2, ColumnaDe(ws, "C/D punz.")).GetDouble(), 0.01, "punzonamiento")
        Assert.AreEqual(2.0, ws.Cell(2, ColumnaDe(ws, "C/D cortante")).GetDouble(), 0.01, "cortante")
        Assert.AreEqual(2.0, ws.Cell(2, ColumnaDe(ws, "C/D flexion")).GetDouble(), 0.01, "flexión")
    End Sub

    ''' <summary>La combinación se identifica como estática o dinámica.</summary>
    <TestMethod>
    Public Sub Detalle_DiceSiLaCombinacionEsEstaticaODinamica()
        Dim ws = Exportar("Detalle Combinaciones")
        Assert.AreEqual("Estatica", ws.Cell(2, ColumnaDe(ws, "Tipo")).GetString())
    End Sub

    ' =====================================================================
    ' Resumen
    ' =====================================================================

    ''' <summary>
    ''' El tipo de apoyo va en el resumen: de él dependen alfa_s y el perímetro
    ''' de punzonamiento, así que quien lea el reporte tiene que poder
    ''' verificarlo sin abrir el programa.
    ''' </summary>
    <TestMethod>
    Public Sub Resumen_TraeElTipoDeApoyo()
        Dim ws = Exportar("Resumen")
        Assert.AreEqual("Central", ws.Cell(2, ColumnaDe(ws, "Tipo de apoyo")).GetString())
    End Sub

    ''' <summary>
    ''' El peor C/D y cuál revisión gobierna. Antes el resumen solo decía
    ''' "Cumple / No cumple" y no se sabía cuál zapata iba más justa.
    ''' </summary>
    <TestMethod>
    Public Sub Resumen_TraeElPeorFactorYQuienGobierna()
        Dim ws = Exportar("Resumen")
        Assert.AreEqual(1.09, ws.Cell(2, ColumnaDe(ws, "Peor C/D")).GetDouble(), 0.01)
        Assert.AreEqual("Suelo estático", ws.Cell(2, ColumnaDe(ws, "Gobierna")).GetString())
    End Sub

    ''' <summary>Las dimensiones también son magnitudes, no factores.</summary>
    <TestMethod>
    Public Sub Resumen_LasDimensionesSalenCompletas()
        Dim ws = Exportar("Resumen")
        Assert.AreEqual(200.0, ws.Cell(2, ColumnaDe(ws, "qAdm Est. (kN/m2)")).GetDouble(), 0.001)
        Assert.AreEqual(300.0, ws.Cell(2, ColumnaDe(ws, "qAdm Din. (kN/m2)")).GetDouble(), 0.001)
    End Sub

    ''' <summary>El estado general sigue saliendo del cálculo, no del resumen.</summary>
    <TestMethod>
    Public Sub Resumen_ElEstadoGeneralReflejaElCalculo()
        Dim ws = Exportar("Resumen")
        Assert.AreEqual("NO CUMPLE", ws.Cell(2, ColumnaDe(ws, "General")).GetString())
        Assert.AreEqual("NO CUMPLE", ws.Cell(2, ColumnaDe(ws, "Capacidad")).GetString())
        Assert.AreEqual("CUMPLE", ws.Cell(2, ColumnaDe(ws, "Punzonamiento")).GetString())
    End Sub

End Class
