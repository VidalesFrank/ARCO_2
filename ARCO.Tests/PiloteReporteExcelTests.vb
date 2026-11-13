Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.IO
Imports ClosedXML.Excel
Imports ARCO

''' <summary>
''' Pruebas del PiloteReporteExcelService. Verifican que el archivo se genere,
''' tenga las 5 hojas esperadas, los encabezados correctos, y que no aborte
''' con un modelo vacío (caso sin apoyos).
''' </summary>
<TestClass>
Public Class PiloteReporteExcelTests

    Private _tempFile As String

    <TestInitialize>
    Public Sub Setup()
        _tempFile = Path.Combine(Path.GetTempPath(),
                                 "arco_test_pilotes_" & Guid.NewGuid().ToString("N") & ".xlsx")
    End Sub

    <TestCleanup>
    Public Sub Teardown()
        Try
            If File.Exists(_tempFile) Then File.Delete(_tempFile)
        Catch
            ' Best-effort; no bloquea la prueba si Windows aún tiene el archivo.
        End Try
    End Sub

    Private Function ContenedorConDatos() As cPilotes
        Dim c As New cPilotes()
        c.TiposPilote.Add(New TipoPilote With {
            .Nombre = "PP60", .Tipo = eTipoPilote.Perforado,
            .Diametro = 0.6, .Longitud = 15,
            .Qc_Admisible = 2000, .Qt_Admisible = 0,
            .Fc = 21, .Fy = 420, .NumBarras_Long = 6, .Diametro_Barra_Long = "#5"
        })
        Dim td As New TipoDado With {.Nombre = "D1", .Descripcion = "Dado 2x2 PP60"}
        td.Pilotes.Add(New PilotePosicion(-0.9, -0.9, "PP60"))
        td.Pilotes.Add(New PilotePosicion(0.9, -0.9, "PP60"))
        td.Pilotes.Add(New PilotePosicion(-0.9, 0.9, "PP60"))
        td.Pilotes.Add(New PilotePosicion(0.9, 0.9, "PP60"))
        td.Poligono = PiloteService.GenerarPoligonoRectangular(2.6, 2.6)
        c.TiposDado.Add(td)

        Dim d As New Dado With {
            .JointLabel = "J1", .NombreTipoDado = "D1",
            .SourceType = "Frame", .X_Global = 0, .Y_Global = 0
        }
        d.Combinaciones.Add(New CombinacionDado With {
            .LoadCase = "Du1", .FZ = 4000, .MX = 0, .MY = 0
        })
        c.Dados.Add(d)
        c.Lista_Combinaciones.Add("Du1")
        c.ListA_Combinaciones_Design.Add("Du1")
        PiloteService.EvaluarTodos(c)
        Return c
    End Function

    <TestMethod>
    Public Sub Exportar_GeneraArchivoCon5Hojas()
        Dim c = ContenedorConDatos()
        Dim r = PiloteReporteExcelService.Exportar(c, Nothing, _tempFile)

        Assert.IsTrue(r.Exito, "La exportación debe tener éxito: " & r.Mensaje)
        Assert.IsTrue(File.Exists(_tempFile), "El archivo debe existir en disco.")
        Assert.AreEqual(5, r.NumHojas)
    End Sub

    <TestMethod>
    Public Sub Exportar_IncluyeHojasEsperadas()
        Dim c = ContenedorConDatos()
        PiloteReporteExcelService.Exportar(c, Nothing, _tempFile)

        Using wb As New XLWorkbook(_tempFile)
            Dim nombres = wb.Worksheets.Select(Function(w) w.Name).ToList()
            CollectionAssert.Contains(nombres, "Resumen")
            CollectionAssert.Contains(nombres, "Apoyos")
            CollectionAssert.Contains(nombres, "Catálogo Pilotes")
            CollectionAssert.Contains(nombres, "Catálogo Dados")
            CollectionAssert.Contains(nombres, "Combinaciones")
        End Using
    End Sub

    <TestMethod>
    Public Sub Exportar_HojaApoyos_UsaDatosDelDado()
        Dim c = ContenedorConDatos()
        PiloteReporteExcelService.Exportar(c, Nothing, _tempFile)

        Using wb As New XLWorkbook(_tempFile)
            Dim ws = wb.Worksheet("Apoyos")
            Assert.AreEqual("J1", ws.Cell(2, 1).GetString(),
                "Primera fila de datos debe ser el apoyo J1.")
            Assert.AreEqual("Frame", ws.Cell(2, 2).GetString())
            Assert.AreEqual("D1", ws.Cell(2, 7).GetString())
        End Using
    End Sub

    <TestMethod>
    Public Sub Exportar_HojaCatalogoPilotes_CalculaChequeoEstructural()
        Dim c = ContenedorConDatos()
        PiloteReporteExcelService.Exportar(c, Nothing, _tempFile)

        Using wb As New XLWorkbook(_tempFile)
            Dim ws = wb.Worksheet("Catálogo Pilotes")
            Assert.AreEqual("PP60", ws.Cell(2, 1).GetString())
            ' φPn columna 15 debe ser > 0 (el chequeo estructural se corre vivo)
            Dim phiPn = ws.Cell(2, 15).GetDouble()
            Assert.IsTrue(phiPn > 0, "φPn debe calcularse en la hoja del catálogo. Obtenido: " & phiPn)
        End Using
    End Sub

    <TestMethod>
    Public Sub Exportar_ContenedorVacio_GeneraArchivoConMensaje()
        ' Caso borde: contenedor sin apoyos/tipos. No debe fallar — genera el
        ' archivo con mensajes "sin datos" en cada hoja, para que el usuario
        ' igual tenga el reporte "vacío" como placeholder.
        Dim c As New cPilotes()
        Dim r = PiloteReporteExcelService.Exportar(c, Nothing, _tempFile)
        Assert.IsTrue(r.Exito, "Un modelo vacío igual debe generar un archivo válido.")
        Assert.AreEqual(5, r.NumHojas)
    End Sub

    <TestMethod>
    Public Sub Exportar_HojaCombinaciones_DistingueCombosEnDiseno()
        Dim c = ContenedorConDatos()
        c.Lista_Combinaciones.Add("Du2")   ' no seleccionada para diseño
        PiloteReporteExcelService.Exportar(c, Nothing, _tempFile)

        Using wb As New XLWorkbook(_tempFile)
            Dim ws = wb.Worksheet("Combinaciones")
            ' Du1 está en diseño ("Sí"); Du2 no ("No").
            Dim filaDu1 = -1, filaDu2 = -1
            For fila = 2 To 10
                Dim nombre = ws.Cell(fila, 1).GetString()
                If nombre = "Du1" Then filaDu1 = fila
                If nombre = "Du2" Then filaDu2 = fila
            Next
            Assert.IsTrue(filaDu1 > 0 AndAlso filaDu2 > 0, "Deben aparecer Du1 y Du2.")
            Assert.AreEqual("Sí", ws.Cell(filaDu1, 2).GetString())
            Assert.AreEqual("No", ws.Cell(filaDu2, 2).GetString())
        End Using
    End Sub

End Class
