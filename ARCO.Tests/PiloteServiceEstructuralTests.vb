Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Chequeo estructural axial del módulo de Pilotes. Pruebas de las fórmulas
''' NSR-10 y de la convención C/D=0 "no aplica" cuando no hay demanda.
'''
''' Pilote de referencia: perforado Ø0.60 m, fc = 21 MPa, 6 barras #5
''' (199 mm² c/u, As_t = 1194 mm²), fy = 420 MPa.
'''   Ag  = π·0.60²/4 × 10⁶ = 282 743 mm²
'''   Ast = 6 × 199 = 1194 mm²  → cuantía 0.42% (bajo el 1% → warning)
'''   φPn = 0.80 × 0.65 × (0.85·21·(Ag−Ast) + 420·Ast) / 1000
'''       = 0.52 × (5 019 918 + 501 480) / 1000
'''       = 0.52 × 5 521 ≈ 2 871 kN
'''   φTn = 0.90 × 420 × 1194 / 1000 = 451 kN
''' </summary>
<TestClass>
Public Class PiloteServiceEstructuralTests

    Private Const DIAM As Double = 0.60
    Private Const FC As Double = 21.0
    Private Const FY As Double = 420.0

    Private Function PiloteBase() As TipoPilote
        Return New TipoPilote With {
            .Nombre = "P60",
            .Tipo = eTipoPilote.Perforado,
            .Diametro = DIAM,
            .Longitud = 10.0,
            .Qc_Admisible = 2000.0,
            .Qt_Admisible = 0.0,
            .Fc = FC,
            .Fy = FY,
            .NumBarras_Long = 6,
            .Diametro_Barra_Long = "#5",
            .Recubrimiento = 0.075
        }
    End Function

    <TestMethod>
    Public Sub EvaluarEstructural_Compresion_CalculaPhiPn_Aproximadamente()
        Dim tp = PiloteBase()
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 2000.0, 0.0)

        ' Área bruta y de acero
        Assert.AreEqual(Math.PI * DIAM * DIAM / 4.0 * 1000000.0, r.Ag_mm2, 1.0)
        Assert.AreEqual(6 * 199.0, r.Ast_mm2, 0.5)

        ' φPn esperado ≈ 2871 kN (ver cabecera). Tolerancia de 5 kN por
        ' redondeos de AreaRefuerzo (199 real ≈ 199.7).
        Assert.IsTrue(r.PhiPn_kN > 2800 AndAlso r.PhiPn_kN < 2900,
                      $"φPn fuera de rango esperado: {r.PhiPn_kN:0}")
        ' Pu = 2000 kN → C/D ≈ 1.43
        Assert.IsTrue(r.CD_Compresion > 1.4 AndAlso r.CD_Compresion < 1.5,
                      $"C/D compresión fuera de rango: {r.CD_Compresion:0.00}")
    End Sub

    <TestMethod>
    Public Sub EvaluarEstructural_SinTraccion_CDEsCero()
        ' Convención del módulo: Pu_min >= 0 → C/D tracción = 0 (no aplica).
        ' Mismo sentinela que PilaService.
        Dim tp = PiloteBase()
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 1000.0, 0.0)
        Assert.AreEqual(0.0, r.CD_Traccion, 1.0E-9,
            "Sin demanda de tracción, C/D debe ser 0 (sentinela 'no aplica').")
    End Sub

    <TestMethod>
    Public Sub EvaluarEstructural_ConTraccion_DivideValorAbsoluto()
        ' φTn = 0.9 × 420 × 1194 / 1000 ≈ 451 kN.
        ' Con Pu_min = -200 kN → C/D ≈ 2.26.
        Dim tp = PiloteBase()
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 500.0, -200.0)
        Assert.IsTrue(r.PhiTn_kN > 440 AndAlso r.PhiTn_kN < 460)
        Assert.IsTrue(r.CD_Traccion > 2.0 AndAlso r.CD_Traccion < 2.5,
                      $"C/D tracción fuera de rango: {r.CD_Traccion}")
    End Sub

    <TestMethod>
    Public Sub EvaluarEstructural_FaltanDatosRefuerzo_RetornaCDCero()
        Dim tp = New TipoPilote With {
            .Nombre = "P60_vacio",
            .Diametro = DIAM, .Fc = FC, .Fy = FY,
            .NumBarras_Long = 0, .Diametro_Barra_Long = ""
        }
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 1000.0, 0.0)
        Assert.AreEqual(0.0, r.CD_Compresion, 1.0E-9)
        Assert.IsTrue(r.Advertencias.Count > 0, "Debe reportar advertencia por falta de refuerzo.")
    End Sub

    <TestMethod>
    Public Sub EvaluarEstructural_FcCero_RetornaCDCero()
        Dim tp = PiloteBase()
        tp.Fc = 0
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 1000.0, 0.0)
        Assert.AreEqual(0.0, r.CD_Compresion, 1.0E-9)
        Assert.IsTrue(r.Advertencias.Any(Function(s) s.Contains("fc")))
    End Sub

    <TestMethod>
    Public Sub EvaluarEstructural_CuantiaAlta_AdvierteNoFalla()
        ' Cuantía > 8% debe advertir pero igual calcular el φPn. Diseñamos a
        ' propósito un caso que la sobrepasa: 60 barras #10 en Ø0.30 m.
        ' Ag = π·0.30²/4 × 10⁶ ≈ 70 686 mm²
        ' Ast = 60 × 819 = 49 140 mm² → cuantía 69.5%
        Dim tp = New TipoPilote With {
            .Nombre = "P30_sobrecargado",
            .Diametro = 0.30, .Fc = FC, .Fy = FY,
            .NumBarras_Long = 60, .Diametro_Barra_Long = "#10"
        }
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 1000.0, 0.0)
        Assert.IsTrue(r.CuantiaPct > 8.0)
        Assert.IsTrue(r.Advertencias.Any(Function(s) s.Contains("8%")),
            "Debe advertir por cuantía superior al 8%.")
        Assert.IsTrue(r.PhiPn_kN > 0, "φPn se calcula aun con cuantía excesiva.")
    End Sub

    <TestMethod>
    Public Sub EvaluarEstructural_SinDemandaCompresion_CDEsCero()
        ' Si Pu_max <= 0, el chequeo de compresión no aplica.
        Dim tp = PiloteBase()
        Dim r = PiloteService.EvaluarEstructuralPilote(tp, 0.0, -100.0)
        Assert.AreEqual(0.0, r.CD_Compresion, 1.0E-9)
    End Sub

    ' ── Agregado por dado: EvaluarEstructuralDado ────────────────────

    <TestMethod>
    Public Sub EvaluarEstructuralDado_MinimoGobierna()
        ' Dado con dos tipos de pilote: el peor C/D entre los dos gobierna el
        ' agregado. Se construye un ResultadoDado "a mano" con dos pilotes.
        Dim tpBueno = PiloteBase()
        tpBueno.Nombre = "P60"
        Dim tpDebil = PiloteBase()
        tpDebil.Nombre = "P30"
        tpDebil.Diametro = 0.30   ' φPn más chico → gobernará

        Dim contenedor As New cPilotes()
        contenedor.TiposPilote.Add(tpBueno)
        contenedor.TiposPilote.Add(tpDebil)

        Dim td As New TipoDado With {.Nombre = "D1"}
        td.Pilotes.Add(New PilotePosicion(-0.4, 0, "P60"))
        td.Pilotes.Add(New PilotePosicion(0.4, 0, "P30"))
        contenedor.TiposDado.Add(td)

        ' ResultadoDado simulado con un PorPilote por tipo. Cada pilote ve
        ' el mismo Pu_max (800 kN) para que el chequeo lo controlen fc/Ag.
        Dim rDado As New PiloteService.ResultadoDado()
        rDado.PorPilote.Add(New PiloteService.ResumenPilote With {
            .Indice = 0, .Numero = 1, .X_Local = -0.4,
            .NombreTipoPilote = "P60", .Pu_Max = 800.0, .Pu_Min = 0.0
        })
        rDado.PorPilote.Add(New PiloteService.ResumenPilote With {
            .Indice = 1, .Numero = 2, .X_Local = 0.4,
            .NombreTipoPilote = "P30", .Pu_Max = 800.0, .Pu_Min = 0.0
        })

        Dim r = PiloteService.EvaluarEstructuralDado(rDado, td, contenedor)

        Assert.AreEqual(2, r.PorTipoPilote.Count)
        Assert.IsTrue(r.PorTipoPilote("P60").CD_Compresion > r.PorTipoPilote("P30").CD_Compresion,
                      "El pilote Ø60 debe tener mejor C/D que el Ø30.")
        Assert.AreEqual(r.PorTipoPilote("P30").CD_Compresion, r.CD_Compresion_Minima, 1.0E-9,
                      "El mínimo entre tipos debe gobernar el agregado.")
    End Sub

    <TestMethod>
    Public Sub EvaluarTodos_PropagaCDEstructuralAlDado()
        ' Integración: EvaluarTodos debe escribir CD_Compresion_Estructural
        ' y CD_Traccion_Estructural en cada Dado del contenedor.
        Dim contenedor As New cPilotes()
        contenedor.TiposPilote.Add(PiloteBase())

        Dim td As New TipoDado With {.Nombre = "D1"}
        td.Pilotes.Add(New PilotePosicion(-0.5, -0.5, "P60"))
        td.Pilotes.Add(New PilotePosicion(0.5, -0.5, "P60"))
        td.Pilotes.Add(New PilotePosicion(-0.5, 0.5, "P60"))
        td.Pilotes.Add(New PilotePosicion(0.5, 0.5, "P60"))
        contenedor.TiposDado.Add(td)

        Dim dado As New Dado With {
            .JointLabel = "J1", .NombreTipoDado = "D1",
            .SourceType = "Frame", .X_Global = 0, .Y_Global = 0
        }
        dado.Combinaciones.Add(New CombinacionDado With {
            .LoadCase = "Du1", .FZ = 4000, .MX = 0, .MY = 0
        })
        contenedor.Dados.Add(dado)

        PiloteService.EvaluarTodos(contenedor)

        Assert.IsTrue(dado.CD_Compresion > 0, "Geot C/D debe propagarse.")
        Assert.IsTrue(dado.CD_Compresion_Estructural > 0,
                      "Estructural C/D debe propagarse al Dado.")
    End Sub

End Class
