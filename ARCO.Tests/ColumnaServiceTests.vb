Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Helpers del módulo Columnas que consume la planta.
'''
''' PeorALR devuelve el máximo ALR calculado para una columna. Sentinela 0 =
''' sin cálculo (misma convención que MinChequeoEsfuerzos en Pilas): la planta
''' pinta gris, no rojo, cuando no hay valor que mostrar.
''' </summary>
<TestClass>
Public Class ColumnaServiceTests

    Private Shared Function Col(ParamArray alrs As Single()) As Columna
        Dim c As New Columna With {.Name_Label = "C1", .Lista_ALR = New List(Of Columna.ALR)}
        For i As Integer = 0 To alrs.Length - 1
            c.Lista_ALR.Add(New Columna.ALR With {.Combinacion = "C" & i, .ALR = alrs(i)})
        Next
        Return c
    End Function

    <TestMethod>
    Public Sub PeorALR_TomaElMaximoDeLaLista()
        Dim c = Col(0.12F, 0.28F, 0.18F)
        Assert.AreEqual(0.28F, ColumnaService.PeorALR(c), 0.0001F)
    End Sub

    <TestMethod>
    Public Sub PeorALR_SinListaEsCero()
        Assert.AreEqual(0.0F, ColumnaService.PeorALR(Nothing))
        Assert.AreEqual(0.0F, ColumnaService.PeorALR(Col()))
    End Sub

    <TestMethod>
    Public Sub PeorALR_DescartaValoresNoCalculados()
        ' 0, Infinity y NaN son sentinelas de "no aplica" en .esm viejos.
        Dim c = Col(0.0F, Single.PositiveInfinity, Single.NaN, 0.22F)
        Assert.AreEqual(0.22F, ColumnaService.PeorALR(c), 0.0001F)
    End Sub

    <TestMethod>
    Public Sub PeorALR_DetectaColumnasQueExcedenElLimite()
        ' Límite NSR-10 C.21.6.3.1 para columnas DES es 0.30. La planta pinta rojo arriba de eso.
        Dim c = Col(0.15F, 0.35F)
        Assert.IsTrue(ColumnaService.PeorALR(c) > 0.3F, "la planta debe reconocer que excede")
    End Sub

End Class
