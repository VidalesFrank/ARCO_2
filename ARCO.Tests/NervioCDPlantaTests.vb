Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Helpers de C/D por chequeo para la planta de Nervios.
'''
''' Sentinelas: 0 = sin cálculo (consistente con PilaService/ColumnaService);
''' 99.0 es "no aplica" porque CalcularFlexion/CalcularCortante lo usan cuando
''' el momento o cortante demandado es ~0 (ver NervioService líneas 526-528 y
''' 633-634). Si ningún valor es válido, los helpers devuelven 0.
''' </summary>
<TestClass>
Public Class NervioCDPlantaTests

    Private Shared Function Frame(cdMI As Double, cdMC As Double, cdMD As Double,
                                  cdVI As Double, cdVD As Double,
                                  Optional refMod As Boolean = True) As cFrameNervio
        Return New cFrameNervio With {
            .ObjectLabel = "N1",
            .Ref_Modificado = refMod,
            .CD_M_Sup_I = cdMI, .CD_M_Inf_C = cdMC, .CD_M_Sup_D = cdMD,
            .CD_Cortante_I = cdVI, .CD_Cortante_D = cdVD
        }
    End Function

    <TestMethod>
    Public Sub CDFlexionFrame_SinRefModificadoDevuelveCero()
        Dim fn = Frame(1.5, 1.2, 1.3, 2.0, 2.0, refMod:=False)
        Assert.AreEqual(0.0, NervioService.CDFlexionFrame(fn))
    End Sub

    <TestMethod>
    Public Sub CDFlexionFrame_SentinelaDe99EsIgnorada()
        ' 99.0 = "momento demandado ~0" → el chequeo no aplica.
        Dim fn = Frame(99.0, 1.1, 99.0, 2.0, 2.0)
        Assert.AreEqual(1.1, NervioService.CDFlexionFrame(fn), 0.001)
    End Sub

    <TestMethod>
    Public Sub CDFlexionFrame_TomaElPeorDeLosTres()
        Dim fn = Frame(1.4, 0.88, 1.6, 2.0, 2.0)
        Assert.AreEqual(0.88, NervioService.CDFlexionFrame(fn), 0.001)
    End Sub

    <TestMethod>
    Public Sub CDFlexionFrame_DescartaInfinitosYNaN()
        Dim fn = Frame(Double.PositiveInfinity, 1.0, Double.NaN, 2.0, 2.0)
        Assert.AreEqual(1.0, NervioService.CDFlexionFrame(fn), 0.001)
    End Sub

    <TestMethod>
    Public Sub CDCortanteFrame_TomaElPeorYRespetaSentinelas()
        Dim fn = Frame(1.5, 1.3, 1.5, 99.0, 0.75)
        Assert.AreEqual(0.75, NervioService.CDCortanteFrame(fn), 0.001)
    End Sub

    <TestMethod>
    Public Sub PeorCDFrame_EsElMinimoEntreFlexionYCortante()
        Dim fn = Frame(1.3, 1.3, 1.3, 1.0, 0.85)
        Assert.AreEqual(0.85, NervioService.PeorCDFrame(fn), 0.001)
    End Sub

    <TestMethod>
    Public Sub PeorCDFrame_FallbackCuandoFaltaUnChequeo()
        ' Flex válida, cortante en sentinela ambos → usar solo flex.
        Dim fn = Frame(1.0, 1.5, 1.2, 99.0, 99.0)
        Assert.AreEqual(1.0, NervioService.PeorCDFrame(fn), 0.001)
    End Sub

    <TestMethod>
    Public Sub PeorCDNervio_EsElMinEntreTodosLosFrames()
        Dim n As New cNervio With {.Frames = New List(Of cFrameNervio)}
        n.Frames.Add(Frame(1.5, 1.4, 1.3, 2.0, 2.0))   ' peor = 1.3
        n.Frames.Add(Frame(1.0, 0.88, 1.2, 1.5, 1.6))  ' peor = 0.88
        n.Frames.Add(Frame(1.1, 1.2, 1.3, 1.4, 1.5))   ' peor = 1.1
        Assert.AreEqual(0.88, NervioService.PeorCDNervio(n), 0.001)
    End Sub

    <TestMethod>
    Public Sub PeorCDNervio_NervioSinFramesDevuelveCero()
        Assert.AreEqual(0.0, NervioService.PeorCDNervio(Nothing))
        Dim vacio As New cNervio With {.Frames = New List(Of cFrameNervio)}
        Assert.AreEqual(0.0, NervioService.PeorCDNervio(vacio))
    End Sub

End Class
