Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Helpers de C/D por chequeo para la planta de Vigas. Sentinela 0 = sin
''' cálculo. Flexión: min(RatioSup/RatioInf) sobre las zonas con AsReq > 0.
''' Cortante: EvaluarCDDefViga ya respeta Estado.SinDatos.
''' </summary>
<TestClass>
Public Class VigaCDPlantaTests

    Private Shared Function Zona(ratioSup As Double, ratioInf As Double,
                                 asReqSup As Double, asReqInf As Double) As cRevisionFlexionZona
        Dim z As New cRevisionFlexionZona()
        z.ResultadoActual = New cResultadoFlexion With {
            .RatioSup = ratioSup, .RatioInf = ratioInf,
            .AsReqSup = asReqSup, .AsReqInf = asReqInf
        }
        Return z
    End Function

    Private Shared Function Frame(ParamArray zonas As cRevisionFlexionZona()) As cFrame
        Dim f As New cFrame With {.RevisionFlexion = New List(Of cRevisionFlexionZona)}
        For Each z In zonas
            f.RevisionFlexion.Add(z)
        Next
        Return f
    End Function

    <TestMethod>
    Public Sub CDFlexionFrame_FrameSinRevisionDevuelveCero()
        Dim f As New cFrame With {.RevisionFlexion = New List(Of cRevisionFlexionZona)}
        Assert.AreEqual(0.0, VigaService.CDFlexionFrame(f))
        Assert.AreEqual(0.0, VigaService.CDFlexionFrame(Nothing))
    End Sub

    <TestMethod>
    Public Sub CDFlexionFrame_IgnoraZonasConAsReqCero()
        ' AsReq = 0 → el chequeo no aplica a esa zona, aunque RatioSup tenga valor.
        Dim f = Frame(Zona(1.2, 1.5, 0.0, 2.0), Zona(0.95, 1.3, 3.0, 2.5))
        ' Solo RatioInf de la primera (asReqInf > 0) y los dos de la segunda cuentan.
        ' min(1.5, 0.95, 1.3) = 0.95
        Assert.AreEqual(0.95, VigaService.CDFlexionFrame(f), 0.001)
    End Sub

    <TestMethod>
    Public Sub CDFlexionFrame_DescartaInfinitosYNaN()
        Dim f = Frame(Zona(Double.PositiveInfinity, Double.NaN, 2.0, 2.0),
                      Zona(1.1, 1.3, 3.0, 2.5))
        Assert.AreEqual(1.1, VigaService.CDFlexionFrame(f), 0.001)
    End Sub

    <TestMethod>
    Public Sub CDFlexionViga_MinSobreTodosLosFrames()
        Dim v As New cViga With {.Frames = New List(Of cFrame)}
        v.Frames.Add(Frame(Zona(1.4, 1.3, 2.0, 2.0)))   ' peor 1.3
        v.Frames.Add(Frame(Zona(0.85, 1.1, 2.0, 2.0)))  ' peor 0.85
        v.Frames.Add(Frame(Zona(1.0, 1.2, 2.0, 2.0)))   ' peor 1.0
        Assert.AreEqual(0.85, VigaService.CDFlexionViga(v), 0.001)
    End Sub

    <TestMethod>
    Public Sub CDFlexionViga_VigaSinDatosEsCero()
        Dim v As New cViga With {.Frames = New List(Of cFrame)}
        Assert.AreEqual(0.0, VigaService.CDFlexionViga(v))
        Assert.AreEqual(0.0, VigaService.CDFlexionViga(Nothing))
    End Sub

    <TestMethod>
    Public Sub PeorCDViga_UsaFlexionCuandoCortanteEsSinDatos()
        ' Sin RevisionCortante en el frame, EvaluarCDDefViga devuelve SinDatos
        ' y CDCortanteViga = 0. PeorCDViga cae en flexión.
        Dim v As New cViga With {.Frames = New List(Of cFrame)}
        v.Frames.Add(Frame(Zona(1.1, 1.3, 2.0, 2.0)))
        Assert.AreEqual(0.0, VigaService.CDCortanteViga(v), "cortante sin datos = 0")
        Assert.AreEqual(1.1, VigaService.PeorCDViga(v), 0.001)
    End Sub

End Class
