Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports System.Windows.Forms
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

    ' ─── ParsearDisenoColumnasFrame ────────────────────────────────────────
    ' Monta un DGV con la forma de "Conc Col Sum - ACI 318-14" E23:
    ' Row 0 = encabezados, Row 1 = unidades, Row 2+ = datos.
    ' Columnas usadas: A=Story(0) B=Label(1) D=DesignSect(3) E=Station(4) K=As(10).
    Private Shared Function NuevoDGVDiseno() As DataGridView
        Dim dgv As New DataGridView()
        For i = 0 To 10 : dgv.Columns.Add("C" & i, "C" & i) : Next
        dgv.Rows.Add("Story", "Label", "UniqueName", "DesignSect", "Station", "", "", "", "", "AsMin", "As")
        dgv.Rows.Add("", "", "", "", "m", "", "", "", "", "m²", "m²")
        Return dgv
    End Function

    Private Shared Sub AddFila(dgv As DataGridView, piso As String, lbl As String, un As String, sec As String, stn As Double, asm2 As Double)
        dgv.Rows.Add(piso, lbl, un, sec, stn, "Design", "No Message", "", "", asm2, asm2)
    End Sub

    <TestMethod>
    Public Sub ParsearDiseno_ColumnaNormalDeTresStations()
        Dim dgv = NuevoDGVDiseno()
        AddFila(dgv, "Nivel 1", "C1", "100", "0.40x1.00", 0.0, 0.006)
        AddFila(dgv, "Nivel 1", "C1", "100", "0.40x1.00", 1.25, 0.006)
        AddFila(dgv, "Nivel 1", "C1", "100", "0.40x1.00", 2.5, 0.007)

        Dim tramos = ColumnaService.ParsearDisenoColumnasFrame(dgv, 0, 1, 3, 4, 10)

        Assert.AreEqual(1, tramos.Count)
        Assert.AreEqual("C1", tramos(0).Name_Elemento)
        Assert.AreEqual("Nivel 1", tramos(0).Piso)
        Assert.AreEqual("0.40x1.00", tramos(0).Seccion)
        Assert.AreEqual(6000.0F, tramos(0).As_Req_Bottom, 0.1F)  ' 0.006 m² = 6000 mm²
        Assert.AreEqual(7000.0F, tramos(0).As_Req_Top, 0.1F)
    End Sub

    <TestMethod>
    Public Sub ParsearDiseno_ColumnaPartidaEnDosSubFrames()
        ' Caso real del archivo Data-Columnas (C3 en Cubierta): ETABS agrega un
        ' nodo intermedio y exporta dos sub-frames consecutivos. Antes del fix
        ' esto generaba DOS tramos por (Piso, Label) y la segunda con As_Top
        ' tomada de la columna siguiente.
        Dim dgv = NuevoDGVDiseno()
        ' UN=933 (abajo)
        AddFila(dgv, "Cubierta", "C3", "933", "0.40x1.50", 0.0, 0.006)
        AddFila(dgv, "Cubierta", "C3", "933", "0.40x1.50", 0.594, 0.006)
        AddFila(dgv, "Cubierta", "C3", "933", "0.40x1.50", 1.188, 0.006)
        ' UN=934 (arriba)
        AddFila(dgv, "Cubierta", "C3", "934", "0.40x1.50", 0.0, 0.006)
        AddFila(dgv, "Cubierta", "C3", "934", "0.40x1.50", 0.363, 0.006)
        AddFila(dgv, "Cubierta", "C3", "934", "0.40x1.50", 0.726, 0.008)  ' top real
        ' Columna siguiente — si el loop toma As_Top de la fila equivocada, le pega a ésta.
        AddFila(dgv, "Cubierta", "C4", "935", "0.40x1.50", 0.0, 0.009)

        Dim tramos = ColumnaService.ParsearDisenoColumnasFrame(dgv, 0, 1, 3, 4, 10)

        Assert.AreEqual(2, tramos.Count, "un tramo por columna física, no uno por sub-frame")
        Dim c3 = tramos.Find(Function(t) t.Name_Elemento = "C3")
        Assert.IsNotNull(c3)
        Assert.AreEqual(6000.0F, c3.As_Req_Bottom, 0.1F, "Bottom = Station 0 del sub-frame inferior")
        Assert.AreEqual(8000.0F, c3.As_Req_Top, 0.1F, "Top = Station máxima del sub-frame superior (NO la fila de C4)")
    End Sub

    <TestMethod>
    Public Sub ParsearDiseno_MezclaColumnasNormalesYPartidas()
        Dim dgv = NuevoDGVDiseno()
        AddFila(dgv, "Nivel 2", "C1", "10", "S1", 0.0, 0.006)
        AddFila(dgv, "Nivel 2", "C1", "10", "S1", 1.25, 0.006)
        AddFila(dgv, "Nivel 2", "C1", "10", "S1", 2.5, 0.006)
        AddFila(dgv, "Nivel 2", "C3", "11", "S2", 0.0, 0.007)
        AddFila(dgv, "Nivel 2", "C3", "11", "S2", 0.6, 0.007)
        AddFila(dgv, "Nivel 2", "C3", "11", "S2", 1.2, 0.007)
        AddFila(dgv, "Nivel 2", "C3", "12", "S2", 0.0, 0.007)
        AddFila(dgv, "Nivel 2", "C3", "12", "S2", 0.4, 0.007)
        AddFila(dgv, "Nivel 2", "C3", "12", "S2", 0.8, 0.007)
        AddFila(dgv, "Nivel 2", "C4", "13", "S1", 0.0, 0.008)
        AddFila(dgv, "Nivel 2", "C4", "13", "S1", 1.25, 0.008)
        AddFila(dgv, "Nivel 2", "C4", "13", "S1", 2.5, 0.008)
        AddFila(dgv, "Nivel 1", "C1", "14", "S1", 0.0, 0.009)
        AddFila(dgv, "Nivel 1", "C1", "14", "S1", 1.25, 0.009)
        AddFila(dgv, "Nivel 1", "C1", "14", "S1", 2.5, 0.009)

        Dim tramos = ColumnaService.ParsearDisenoColumnasFrame(dgv, 0, 1, 3, 4, 10)

        ' 4 columnas físicas: C1-N2, C3-N2 (partida), C4-N2, C1-N1
        Assert.AreEqual(4, tramos.Count)
        ' Orden de aparición preservado
        Assert.AreEqual("C1", tramos(0).Name_Elemento) : Assert.AreEqual("Nivel 2", tramos(0).Piso)
        Assert.AreEqual("C3", tramos(1).Name_Elemento) : Assert.AreEqual("Nivel 2", tramos(1).Piso)
        Assert.AreEqual("C4", tramos(2).Name_Elemento) : Assert.AreEqual("Nivel 2", tramos(2).Piso)
        Assert.AreEqual("C1", tramos(3).Name_Elemento) : Assert.AreEqual("Nivel 1", tramos(3).Piso)
    End Sub

    <TestMethod>
    Public Sub ParsearDiseno_TablaSoloEncabezadosDevuelveListaVacia()
        Dim dgv = NuevoDGVDiseno()
        Dim tramos = ColumnaService.ParsearDisenoColumnasFrame(dgv, 0, 1, 3, 4, 10)
        Assert.AreEqual(0, tramos.Count)
    End Sub

End Class
