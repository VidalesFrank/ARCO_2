Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Pruebas del modelo puntal-tensor (NSR-10 C.23) del módulo de Pilotes.
'''
''' Escenario base: dado 2.6 × 2.6 m, Espesor 0.8 m, Recubrimiento 0.075 m
'''   → d = 0.725 m.
''' 4 pilotes Ø0.60 m en las esquinas (±0.9, ±0.9) locales.
''' Columna 0.4×0.4 m sobre el dado, fc = 21 MPa, fy = 420 MPa, βs = 0.75.
''' Combinación única: FZ = 4000 kN de compresión (1000 kN por pilote).
'''
''' Geometría del puntal a esquina:
'''   r_horiz = √(0.9² + 0.9²) ≈ 1.273 m
'''   L_puntal = √(1.273² + 0.725²) ≈ 1.465 m
'''   sin(θ) = 0.725 / 1.465 ≈ 0.495 → θ ≈ 29.6°
'''   Fuerza puntal = 1000 / 0.495 ≈ 2020 kN
'''
''' Tensores: 2 en X (una por fila) y 2 en Y (una por columna).
'''   |Hx_pilote| = 1000 · 0.9 / 0.725 ≈ 1241 kN
'''   As_X_req = 1241 · 10 / (0.75 · 420) ≈ 39.4 cm²
''' </summary>
<TestClass>
Public Class PiloteSTMTests

    Private Function ContenedorBase() As cPilotes
        Dim c As New cPilotes()
        c.TiposPilote.Add(New TipoPilote With {
            .Nombre = "PP60", .Tipo = eTipoPilote.Perforado,
            .Diametro = 0.6, .Longitud = 15,
            .Qc_Admisible = 2000, .Qt_Admisible = 0,
            .Fc = 21, .Fy = 420, .NumBarras_Long = 6, .Diametro_Barra_Long = "#5"
        })
        Dim td As New TipoDado With {
            .Nombre = "D1",
            .Espesor = 0.8, .Recubrimiento = 0.075,
            .Fc = 21, .Fy = 420,
            .LadoColumna_X = 0.4, .LadoColumna_Y = 0.4,
            .Beta_Puntal = 0.75
        }
        td.Pilotes.Add(New PilotePosicion(-0.9, -0.9, "PP60"))
        td.Pilotes.Add(New PilotePosicion(0.9, -0.9, "PP60"))
        td.Pilotes.Add(New PilotePosicion(-0.9, 0.9, "PP60"))
        td.Pilotes.Add(New PilotePosicion(0.9, 0.9, "PP60"))
        td.Poligono = PiloteService.GenerarPoligonoRectangular(2.6, 2.6)
        c.TiposDado.Add(td)

        Dim d As New Dado With {
            .JointLabel = "J1", .NombreTipoDado = "D1",
            .X_Global = 0, .Y_Global = 0
        }
        d.Combinaciones.Add(New CombinacionDado With {
            .LoadCase = "Du1", .FZ = 4000, .MX = 0, .MY = 0
        })
        c.Dados.Add(d)
        c.Lista_Combinaciones.Add("Du1")
        c.ListA_Combinaciones_Design.Add("Du1")
        Return c
    End Function

    Private Function Evaluar(c As cPilotes) As STMService.ResultadoSTM
        Dim td = c.TiposDado(0)
        Dim tp = c.TiposPilote(0)
        Dim rGeot = PiloteService.EvaluarDado(c.Dados(0), td, tp, c)
        Dim modelo = STMService.GenerarModelo(td, rGeot, c)
        Return STMService.Evaluar(modelo, td)
    End Function

    <TestMethod>
    Public Sub Modelo_DadoSimetrico_GeneraCuatroPuntales()
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        Assert.AreEqual(4, r.Modelo.Puntales.Count,
            "Cuatro pilotes en compresión deben generar cuatro puntales.")
    End Sub

    <TestMethod>
    Public Sub Modelo_DadoSimetrico_GeneraCuatroTensores()
        ' 2 filas (Y=±0.9) × 2 pilotes → 2 tensores X.
        ' 2 columnas (X=±0.9) × 2 pilotes → 2 tensores Y.
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        Assert.AreEqual(2, r.Modelo.TensoresX.Count)
        Assert.AreEqual(2, r.Modelo.TensoresY.Count)
    End Sub

    <TestMethod>
    Public Sub Modelo_DadoSimetrico_AnguloPuntalEs30Aproximadamente()
        ' θ = atan(0.725 / 1.273) ≈ 29.65°
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        For Each p In r.Modelo.Puntales
            Assert.IsTrue(p.Angulo > 29 AndAlso p.Angulo < 31,
                $"Ángulo del puntal fuera de rango: {p.Angulo:0.00}°")
        Next
    End Sub

    <TestMethod>
    Public Sub Modelo_DadoSimetrico_FuerzaPuntalAproximadamente2020kN()
        ' F = 1000 / sin(29.65°) = 1000 / 0.4947 ≈ 2021 kN
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        For Each p In r.Modelo.Puntales
            Assert.IsTrue(p.Fuerza > 2000 AndAlso p.Fuerza < 2050,
                $"Fuerza del puntal fuera de rango: {p.Fuerza:0}")
        Next
    End Sub

    <TestMethod>
    Public Sub Modelo_DadoSimetrico_AsXYSimetricos()
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        ' As_X = 1241·10 / (0.75·420) ≈ 39.4 cm²
        Assert.IsTrue(r.As_X_cm2 > 38 AndAlso r.As_X_cm2 < 41,
            $"As_X fuera de rango: {r.As_X_cm2:0.0}")
        Assert.AreEqual(r.As_X_cm2, r.As_Y_cm2, 0.01,
            "Dado simétrico debe dar As_X = As_Y.")
    End Sub

    <TestMethod>
    Public Sub Modelo_NodoSuperiorCCC_RecibeSumaCompleta()
        ' Σ compresiones = 4000 kN sobre el nodo superior.
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        Assert.IsTrue(Math.Abs(r.Modelo.NodoSuperior.FuerzaCompresion - 4000) < 1,
            $"Nodo superior debe recibir 4000 kN, recibió {r.Modelo.NodoSuperior.FuerzaCompresion:0}.")
        Assert.AreEqual(eTipoNodoSTM.CCC, r.Modelo.NodoSuperior.Tipo)
        Assert.AreEqual(1.0, r.Modelo.NodoSuperior.BetaN, 0.001)
    End Sub

    <TestMethod>
    Public Sub Modelo_NodoInferiorCCT_RecibeCargaDelPilote()
        ' Cada nodo inferior recibe la componente vertical de su puntal = Pu.
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        For Each n In r.Modelo.NodosInferiores
            Assert.IsTrue(Math.Abs(n.FuerzaCompresion - 1000) < 1,
                $"Nodo inferior debe recibir 1000 kN, recibió {n.FuerzaCompresion:0}.")
            Assert.AreEqual(eTipoNodoSTM.CCT, n.Tipo)
            Assert.AreEqual(0.80, n.BetaN, 0.001)
        Next
    End Sub

    <TestMethod>
    Public Sub Modelo_NodoSuperior_CDUsaLadoColumna()
        ' φFn = 0.75 · (0.85·1.0·21) · (0.4·0.4) · 1000 ≈ 2142 kN
        ' C/D = 2142 / 4000 ≈ 0.54 (no cumple — columna pequeña para esta carga)
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        Assert.IsTrue(r.Modelo.NodoSuperior.CD > 0.50 AndAlso r.Modelo.NodoSuperior.CD < 0.58,
            $"C/D nodo superior fuera de rango: {r.Modelo.NodoSuperior.CD:0.00}")
    End Sub

    <TestMethod>
    Public Sub Modelo_DadoProfundo_NoEsEsbelto()
        ' Claro máximo entre pilotes = diagonal ≈ 2.546 m, h = 0.8 m
        ' L/h ≈ 3.18 — ESTE SÍ es esbelto (>2) y debe marcar warning.
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        Assert.IsTrue(r.DadoEsbelto, "L/h > 2 debe activar la bandera DadoEsbelto.")
        Assert.IsTrue(r.Advertencias.Any(Function(s) s.Contains("esbelto")),
            "Debe haber advertencia explícita sobre esbeltez.")
    End Sub

    <TestMethod>
    Public Sub Modelo_DadoRealProfundo_NoMarcaEsbelto()
        ' Dado compacto: pilotes en (±0.4, ±0.4), espesor 1.2 m.
        ' Claro diagonal ≈ 1.131 m, L/h ≈ 0.94 → NO esbelto.
        Dim c = ContenedorBase()
        Dim td = c.TiposDado(0)
        td.Espesor = 1.2
        td.Pilotes.Clear()
        td.Pilotes.Add(New PilotePosicion(-0.4, -0.4, "PP60"))
        td.Pilotes.Add(New PilotePosicion(0.4, -0.4, "PP60"))
        td.Pilotes.Add(New PilotePosicion(-0.4, 0.4, "PP60"))
        td.Pilotes.Add(New PilotePosicion(0.4, 0.4, "PP60"))
        td.Poligono = PiloteService.GenerarPoligonoRectangular(1.6, 1.6)
        Dim r = Evaluar(c)
        Assert.IsFalse(r.DadoEsbelto, "Dado compacto no debe marcar esbelto.")
    End Sub

    <TestMethod>
    Public Sub Modelo_PilotesEnTraccion_NoGeneranPuntales()
        ' 4 pilotes 2×2 con MX grande: la fila inferior entra en tracción fuerte
        ' (P ≈ -530 kN) y la superior se comprime (P ≈ +580 kN). Deben aparecer
        ' SOLO dos puntales (los dos pilotes comprimidos).
        Dim c = ContenedorBase()
        c.Dados(0).Combinaciones.Clear()
        c.Dados(0).Combinaciones.Add(New CombinacionDado With {
            .LoadCase = "Du1", .FZ = 100, .MX = 2000
        })
        Dim r = Evaluar(c)
        Assert.AreEqual(2, r.Modelo.Puntales.Count,
            "Solo deben aparecer puntales para los pilotes comprimidos.")
    End Sub

    <TestMethod>
    Public Sub Tensor_SinRefuerzoProvisto_CDEsPequeñoNoCero()
        ' TipoDado.NumBarras_Inf_X = 0 (default) → As_provisto = 0.
        ' Convención: C/D = 0.01 (sentinela "no cumple por ausencia"), no 0,
        ' para que el usuario vea rojo en pantalla y note que falta meter acero.
        Dim c = ContenedorBase()
        Dim r = Evaluar(c)
        Assert.IsTrue(r.As_X_cm2 > 0, "Debe haber As requerido.")
        Assert.AreEqual(0.0, r.As_X_Provisto_cm2, 0.001)
        Assert.IsTrue(r.CD_Tensor_X > 0 AndAlso r.CD_Tensor_X < 0.1,
            $"C/D tensor X debe marcar falla por falta de refuerzo: {r.CD_Tensor_X}")
    End Sub

    <TestMethod>
    Public Sub Tensor_ConRefuerzoSuficiente_CDMayorA1()
        ' As_req ≈ 39.4 cm². Metemos 10 barras #6 (284 mm² c/u) en X e Y →
        ' As_prov = 10 × 2.84 = 28.4 cm² (insuficiente),
        ' 15 barras #6 → 42.6 cm² (justo), 20 barras #6 → 56.8 cm² (holgura).
        Dim c = ContenedorBase()
        Dim td = c.TiposDado(0)
        td.NumBarras_Inf_X = 20 : td.Diametro_Barras_Inf_X = "#6"
        td.NumBarras_Inf_Y = 20 : td.Diametro_Barras_Inf_Y = "#6"
        Dim r = Evaluar(c)
        Assert.IsTrue(r.As_X_Provisto_cm2 > 55 AndAlso r.As_X_Provisto_cm2 < 58,
            $"As_X_prov esperado ≈ 56.8, obtuvo {r.As_X_Provisto_cm2}")
        Assert.IsTrue(r.CD_Tensor_X > 1.3 AndAlso r.CD_Tensor_X < 1.6,
            $"C/D tensor X esperado ≈ 1.44, obtuvo {r.CD_Tensor_X}")
    End Sub

    <TestMethod>
    Public Sub Tensor_RefuerzoApenasSuficiente_CDCercanoA1()
        ' 15 × #6 = 42.6 cm² / 39.4 ≈ 1.08
        Dim c = ContenedorBase()
        Dim td = c.TiposDado(0)
        td.NumBarras_Inf_X = 15 : td.Diametro_Barras_Inf_X = "#6"
        td.NumBarras_Inf_Y = 15 : td.Diametro_Barras_Inf_Y = "#6"
        Dim r = Evaluar(c)
        Assert.IsTrue(r.CD_Tensor_X > 1.0 AndAlso r.CD_Tensor_X < 1.15,
            $"C/D tensor X ≈ 1.08 esperado, obtuvo {r.CD_Tensor_X}")
    End Sub

    <TestMethod>
    Public Sub Tensor_RefuerzoInsuficiente_CDMenorA1()
        ' 8 × #5 = 8 × 1.997 ≈ 15.98 cm² / 39.4 ≈ 0.41 → no cumple.
        Dim c = ContenedorBase()
        Dim td = c.TiposDado(0)
        td.NumBarras_Inf_X = 8 : td.Diametro_Barras_Inf_X = "#5"
        td.NumBarras_Inf_Y = 8 : td.Diametro_Barras_Inf_Y = "#5"
        Dim r = Evaluar(c)
        Assert.IsTrue(r.CD_Tensor_X < 0.5,
            $"C/D debe ser < 0.5 con solo 8#5, obtuvo {r.CD_Tensor_X}")
    End Sub

    <TestMethod>
    Public Sub DadoDetalleVM_ExponePuntalesTensoresYNodosParaUI()
        ' Las propiedades que la pestaña STM del DadoDetalleWindow binda
        ' deben devolver listas pobladas (no Nothing, no vacías) cuando hay
        ' modelo válido. Esto protege los bindings XAML.
        Dim c = ContenedorBase()
        Dim vm As New DadoDetalleVM(c.Dados(0), c)
        Assert.IsNotNull(vm.STM, "STM no debe ser Nothing.")
        Assert.IsNotNull(vm.STM.Modelo, "STM.Modelo no debe ser Nothing.")
        Assert.AreEqual(4, vm.Puntales.Count)
        Assert.AreEqual(4, vm.Tensores.Count, "2 tensores X + 2 tensores Y.")
        Assert.AreEqual(5, vm.Nodos.Count, "1 nodo CCC + 4 CCT.")
        Assert.IsTrue(vm.CD_STM_Puntal > 0)
        Assert.IsTrue(vm.As_STM_X > 0)
    End Sub

    <TestMethod>
    Public Sub EvaluarTodos_PersisteCDEnDado()
        Dim c = ContenedorBase()
        PiloteService.EvaluarTodos(c)
        STMService.EvaluarTodos(c)
        Dim d = c.Dados(0)
        Assert.IsTrue(d.CD_STM_Puntal > 0, "CD_STM_Puntal debe quedar persistido.")
        Assert.IsTrue(d.CD_STM_Nodo > 0, "CD_STM_Nodo debe quedar persistido.")
        Assert.IsTrue(d.As_STM_X_cm2 > 0, "As_STM_X_cm2 debe quedar persistido.")
        Assert.AreEqual(d.As_STM_X_cm2, d.As_STM_Y_cm2, 0.01,
            "Dado simétrico persiste As_X = As_Y.")
    End Sub

End Class
