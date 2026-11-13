Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

' ActualizadorColumnas — pruebas de la reconciliación y la aplicación.
'
' La fase de lectura (Leer) depende de un Excel físico y queda fuera del
' alcance de estos tests. Para Reconciliar y Aplicar construimos
' DatosExcelColumnas en memoria con el contenido que queremos simular
' (ya parseado, igual que lo dejaría el lector).
<TestClass>
Public Class ActualizadorColumnasTests

    Private Shared Function TramoDatos(piso As String, b As Single, h As Single, fc As Single) As ActualizadorColumnas.TramoDatos
        Return New ActualizadorColumnas.TramoDatos With {
            .Piso = piso, .Seccion = "C" & CInt(b * 100) & "x" & CInt(h * 100),
            .B_Modelo = b, .H_Modelo = h, .fc = fc,
            .As_Req_Top = 1000, .As_Req_Bottom = 1000
        }
    End Function

    Private Shared Function Datos(frameLabels As Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)),
                                  pierLabels As Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)),
                                  combos As IEnumerable(Of String)) As ActualizadorColumnas.DatosExcelColumnas
        Dim d As New ActualizadorColumnas.DatosExcelColumnas
        If frameLabels IsNot Nothing Then
            For Each kv In frameLabels
                d.ColumnasFrame(kv.Key) = kv.Value
            Next
            d.TieneDisenoFrame = True
        End If
        If pierLabels IsNot Nothing Then
            For Each kv In pierLabels
                d.ColumnasPier(kv.Key) = kv.Value
            Next
            d.TieneDisenoPier = True
        End If
        If combos IsNot Nothing Then
            For Each c In combos
                d.CombinacionesLeidas.Add(c)
            Next
        End If
        Return d
    End Function

    Private Shared Function ColumnaMock(label As String, piso As String,
                                        b As Single, h As Single, fc As Single) As Columna
        Dim c As New Columna With {.Name_Elemento = label, .Name_Label = label}
        c.Lista_Tramos_Columnas.Add(New Tramo_Columna With {
            .Name_Elemento = label, .Piso = piso,
            .B_Plano = b, .B_Modelo = b, .H_Plano = h, .H_Modelo = h, .fc = fc
        })
        Return c
    End Function

    Private Shared Function ProyectoMock(ParamArray cols As Columna()) As Proyecto
        Dim p As New Proyecto
        For Each c In cols
            p.Elementos.Columnas.Lista_Columnas.Add(c)
        Next
        Return p
    End Function

    ' ─────────────────────── Reconciliar ─────────────────────────────────

    <TestMethod>
    Public Sub Reconciliar_MismaSeccion_NoReportaCambio()
        Dim proy = ProyectoMock(ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F))
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.4F, 0.6F, 28.0F)}}
        }
        Dim rep = ActualizadorColumnas.Reconciliar(Datos(fr, Nothing, Nothing), proy)
        Assert.AreEqual(0, rep.CambiosSeccionFrame.Count)
        Assert.AreEqual(0, rep.LabelsNuevosFrame.Count)
        Assert.AreEqual(0, rep.LabelsSinExcelFrame.Count)
    End Sub

    <TestMethod>
    Public Sub Reconciliar_SeccionCambio_ReportadoConAntesYDespues()
        Dim proy = ProyectoMock(ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F))
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.5F, 0.7F, 35.0F)}}
        }
        Dim rep = ActualizadorColumnas.Reconciliar(Datos(fr, Nothing, Nothing), proy)
        Assert.AreEqual(1, rep.CambiosSeccionFrame.Count)
        Dim cambio = rep.CambiosSeccionFrame(0)
        Assert.AreEqual("C1", cambio.Label)
        ' Formato sale con el separador decimal del CurrentCulture (coma en es-ES,
        ' punto en en-US). Validamos el número crudo sin separador.
        StringAssert.Contains(cambio.Antes.Replace(",", ".").Replace(" ", ""), "0.40")
        StringAssert.Contains(cambio.Despues.Replace(",", ".").Replace(" ", ""), "0.50")
    End Sub

    <TestMethod>
    Public Sub Reconciliar_LabelEnExcelNoEnProyecto_VaANuevos()
        Dim proy = ProyectoMock(ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F))
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.4F, 0.6F, 28.0F)}},
            {"C2_NUEVA", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.3F, 0.3F, 28.0F)}}
        }
        Dim rep = ActualizadorColumnas.Reconciliar(Datos(fr, Nothing, Nothing), proy)
        Assert.AreEqual(1, rep.LabelsNuevosFrame.Count)
        Assert.AreEqual("C2_NUEVA", rep.LabelsNuevosFrame(0))
    End Sub

    <TestMethod>
    Public Sub Reconciliar_LabelEnProyectoNoEnExcel_VaASinExcel()
        Dim proy = ProyectoMock(
            ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F),
            ColumnaMock("C_HUERFANA", "P1", 0.3F, 0.3F, 28.0F))
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.4F, 0.6F, 28.0F)}}
        }
        Dim rep = ActualizadorColumnas.Reconciliar(Datos(fr, Nothing, Nothing), proy)
        Assert.IsTrue(rep.LabelsSinExcelFrame.Contains("C_HUERFANA"))
        Assert.AreEqual(0, rep.LabelsNuevosFrame.Count)
    End Sub

    <TestMethod>
    Public Sub Reconciliar_CombinacionesNuevas_SoloLasQueNoExistian()
        Dim proy = ProyectoMock(ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F))
        proy.Elementos.Columnas.Lista_Combinaciones.Add("1.2D+1.6L")

        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.4F, 0.6F, 28.0F)}}
        }
        Dim rep = ActualizadorColumnas.Reconciliar(
            Datos(fr, Nothing, {"1.2D+1.6L", "1.2D+1.0E+1.0L", "0.9D+1.0E"}), proy)

        Assert.AreEqual(2, rep.CombinacionesNuevas.Count)
        Assert.IsTrue(rep.CombinacionesNuevas.Contains("1.2D+1.0E+1.0L"))
        Assert.IsTrue(rep.CombinacionesNuevas.Contains("0.9D+1.0E"))
        Assert.IsFalse(rep.CombinacionesNuevas.Contains("1.2D+1.6L"))
    End Sub

    <TestMethod>
    Public Sub Reconciliar_PierCambia_VaAListaDePier()
        Dim proy = ProyectoMock(ColumnaMock("P1_EL", "P1", 0.3F, 2.0F, 28.0F))
        Dim pi As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"P1_EL", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.3F, 2.5F, 28.0F)}}
        }
        Dim rep = ActualizadorColumnas.Reconciliar(Datos(Nothing, pi, Nothing), proy)
        Assert.AreEqual(1, rep.CambiosSeccionPier.Count)
        Assert.AreEqual(0, rep.CambiosSeccionFrame.Count)
    End Sub

    ' ─────────────────────── Aplicar ─────────────────────────────────────

    <TestMethod>
    Public Sub Aplicar_ActualizaSeccionYMantieneRefuerzo()
        Dim col = ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F)
        ' Simulamos refuerzo colocado por el usuario
        col.Lista_Tramos_Columnas(0).Cantidad_Barras_Top = 8
        col.Lista_Tramos_Columnas(0).Cantidad_Barras_Bottom = 8
        col.Lista_Tramos_Columnas(0).Separacion_Estribos = 0.1F
        col.Lista_Tramos_Columnas(0).Numero_Barras_Estribo = "#3"
        col.Lista_Tramos_Columnas(0).Distribucion_Personalizada = True

        Dim proy = ProyectoMock(col)
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.5F, 0.7F, 35.0F)}}
        }

        ActualizadorColumnas.Aplicar(Datos(fr, Nothing, Nothing), proy, Nothing)

        Dim tramo = proy.Elementos.Columnas.Lista_Columnas(0).Lista_Tramos_Columnas(0)
        ' Sección actualizada
        Assert.AreEqual(0.5F, tramo.B_Plano, 1.0E-5)
        Assert.AreEqual(0.7F, tramo.H_Plano, 1.0E-5)
        Assert.AreEqual(35.0F, tramo.fc, 1.0E-5)
        ' Refuerzo preservado
        Assert.AreEqual(8, tramo.Cantidad_Barras_Top)
        Assert.AreEqual(8, tramo.Cantidad_Barras_Bottom)
        Assert.AreEqual(0.1F, tramo.Separacion_Estribos, 1.0E-5)
        Assert.AreEqual("#3", tramo.Numero_Barras_Estribo)
        Assert.IsTrue(tramo.Distribucion_Personalizada)
    End Sub

    <TestMethod>
    Public Sub Aplicar_ReemplazaListaCombinaciones_CuandoExcelTraeFuerzas()
        Dim col = ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F)
        col.Lista_Tramos_Columnas(0).Lista_Combinaciones.Add(
            New Tramo_Columna.Fuerzas_Elementos With {.Name = "Viejo", .P = -500})

        Dim proy = ProyectoMock(col)
        Dim td = TramoDatos("P1", 0.4F, 0.6F, 28.0F)
        td.FuerzasEnvolventePorCombo("Nuevo") = New ActualizadorColumnas.FuerzaCruda With {
            .Combo = "Nuevo", .P = -1000, .V2 = 50, .M3 = 100
        }
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {td}}
        }

        ActualizadorColumnas.Aplicar(Datos(fr, Nothing, {"Nuevo"}), proy, Nothing)

        Dim tramo = proy.Elementos.Columnas.Lista_Columnas(0).Lista_Tramos_Columnas(0)
        Assert.AreEqual(1, tramo.Lista_Combinaciones.Count, "Lista_Combinaciones se reemplaza por completo")
        Assert.AreEqual("Nuevo", tramo.Lista_Combinaciones(0).Name)
        Assert.AreEqual(-1000, tramo.Lista_Combinaciones(0).P, 1.0E-3)
    End Sub

    <TestMethod>
    Public Sub Aplicar_NoReemplazaListaCombinaciones_CuandoExcelNoTraeFuerzas()
        Dim col = ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F)
        col.Lista_Tramos_Columnas(0).Lista_Combinaciones.Add(
            New Tramo_Columna.Fuerzas_Elementos With {.Name = "Viejo", .P = -500})

        Dim proy = ProyectoMock(col)
        Dim td = TramoDatos("P1", 0.4F, 0.6F, 28.0F)   ' sin fuerzas
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {td}}
        }

        ActualizadorColumnas.Aplicar(Datos(fr, Nothing, Nothing), proy, Nothing)

        Dim tramo = proy.Elementos.Columnas.Lista_Columnas(0).Lista_Tramos_Columnas(0)
        Assert.AreEqual(1, tramo.Lista_Combinaciones.Count, "lista vieja se preserva si Excel no trajo fuerzas")
        Assert.AreEqual("Viejo", tramo.Lista_Combinaciones(0).Name)
    End Sub

    <TestMethod>
    Public Sub Aplicar_AgregaColumnaNueva_SoloSiEstaEnLabelsAceptados()
        Dim proy = ProyectoMock(ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F))
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.4F, 0.6F, 28.0F)}},
            {"C2_NUEVA", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.3F, 0.3F, 28.0F)}},
            {"C3_IGNORADA", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.3F, 0.3F, 28.0F)}}
        }
        Dim aceptados As New HashSet(Of String) From {"C2_NUEVA"}

        ActualizadorColumnas.Aplicar(Datos(fr, Nothing, Nothing), proy, aceptados)

        Assert.AreEqual(2, proy.Elementos.Columnas.Lista_Columnas.Count)
        Assert.IsTrue(proy.Elementos.Columnas.Lista_Columnas.Any(Function(c) c.Name_Elemento = "C2_NUEVA"))
        Assert.IsFalse(proy.Elementos.Columnas.Lista_Columnas.Any(Function(c) c.Name_Elemento = "C3_IGNORADA"))
    End Sub

    <TestMethod>
    Public Sub Aplicar_AgregaCombinacionesNuevasAlProyecto()
        Dim proy = ProyectoMock(ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F))
        proy.Elementos.Columnas.Lista_Combinaciones.Add("1.2D+1.6L")

        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.4F, 0.6F, 28.0F)}}
        }
        ActualizadorColumnas.Aplicar(Datos(fr, Nothing, {"1.2D+1.6L", "1.2D+1.0E+1.0L"}), proy, Nothing)

        Assert.AreEqual(2, proy.Elementos.Columnas.Lista_Combinaciones.Count)
        Assert.IsTrue(proy.Elementos.Columnas.Lista_Combinaciones.Contains("1.2D+1.0E+1.0L"))
    End Sub

    <TestMethod>
    Public Sub Aplicar_NoTocaColumnasSinExcel()
        ' Columna del proyecto que no está en el Excel — debe quedar intacta.
        Dim colHuer = ColumnaMock("C_HUERFANA", "P1", 0.3F, 0.3F, 28.0F)
        colHuer.Lista_Tramos_Columnas(0).Cantidad_Barras_Top = 4

        Dim proy = ProyectoMock(
            ColumnaMock("C1", "P1", 0.4F, 0.6F, 28.0F),
            colHuer)
        Dim fr As New Dictionary(Of String, List(Of ActualizadorColumnas.TramoDatos)) From {
            {"C1", New List(Of ActualizadorColumnas.TramoDatos) From {TramoDatos("P1", 0.5F, 0.7F, 35.0F)}}
        }
        ActualizadorColumnas.Aplicar(Datos(fr, Nothing, Nothing), proy, Nothing)

        Dim huer = proy.Elementos.Columnas.Lista_Columnas.First(Function(c) c.Name_Elemento = "C_HUERFANA")
        Assert.AreEqual(0.3F, huer.Lista_Tramos_Columnas(0).B_Plano, 1.0E-5, "huérfana no se tocó")
        Assert.AreEqual(4, huer.Lista_Tramos_Columnas(0).Cantidad_Barras_Top, "refuerzo huérfana intacto")
    End Sub

End Class
