Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' Fila por sección del reporte resumen de muros (Form_Reporte_Resumen_Muros).
'''
''' Hasta 2026-11-17 cada pestaña calculaba su propio factor de flexión, y con
''' ρ_req = 0 la pestaña Flexión lo tomaba como 0 (Revisar) mientras Resumen
''' Completo lo tomaba como 9.99 (OK): la misma sección salía con veredictos
''' opuestos. Los elementos de borde solo miraban el extremo superior, y los
''' muros sin refuerzo asignado salían en rojo. Estas pruebas fijan lo nuevo.
''' </summary>
<TestClass>
Public Class MurosResumenSeccionTests

    Private Shared Function Seccion(colTop As Single, reqTop As Single,
                                    colBot As Single, reqBot As Single,
                                    Optional fCort As Single = 1.5F) As SeccionMuro
        Return New SeccionMuro With {
            .Piso = "P1",
            .Cuantia_Top_Col = colTop, .Cuantia_Top_Req = reqTop,
            .Cuantia_Bot_Col = colBot, .Cuantia_Bot_Req = reqBot,
            .F_Cortante = fCort,
            .Chequeo_EB_I_Top_Esf = "No requiere", .Chequeo_EB_I_Bot_Esf = "No requiere",
            .Chequeo_EB_D_Top_Esf = "No requiere", .Chequeo_EB_D_Bot_Esf = "No requiere",
            .Chequeo_EB_I_Top_Def = "No requiere", .Chequeo_EB_I_Bot_Def = "No requiere",
            .Chequeo_EB_D_Top_Def = "No requiere", .Chequeo_EB_D_Bot_Def = "No requiere"
        }
    End Function

    Private Shared Function MuroCon(sec As SeccionMuro, Optional calculado As Boolean = True) As Muro
        Dim m As New Muro With {.Label = "M1", .Ref_Modificado_Muros = calculado}
        m.Lista_Secciones.Add(sec)
        Return m
    End Function

    <TestMethod>
    Public Sub Flexion_EsRelacionDeCuantias_YTomaElMenor()
        Dim s = Seccion(0.5F, 0.4F, 0.3F, 0.4F)
        Dim f = MurosResumenService.CalcularFilaSeccion(MuroCon(s), s)
        Assert.AreEqual(1.25, f.FTop, 0.000001)
        Assert.AreEqual(0.75, f.FBot, 0.000001)
        Assert.AreEqual(0.75, f.FFlexMin, 0.000001)
        Assert.AreEqual(EstadoChequeoMuro.Revisar, f.EstadoFlexion)
        Assert.AreEqual("Revisar", f.EstadoGlobal)
    End Sub

    <TestMethod>
    Public Sub CuantiaRequeridaCero_EsSinDato_NoOKNiRevisar()
        Dim s = Seccion(0.5F, 0F, 0.5F, 0F)
        Dim f = MurosResumenService.CalcularFilaSeccion(MuroCon(s), s)
        Assert.IsTrue(Double.IsNaN(f.FFlexMin))
        Assert.AreEqual(EstadoChequeoMuro.SinDato, f.EstadoFlexion)
        Assert.AreEqual("Sin dato", f.EstadoGlobal)
    End Sub

    <TestMethod>
    Public Sub UnExtremoSinDato_GobiernaElOtro()
        Dim s = Seccion(0.5F, 0F, 0.3F, 0.4F)
        Dim f = MurosResumenService.CalcularFilaSeccion(MuroCon(s), s)
        Assert.AreEqual(0.75, f.FFlexMin, 0.000001)
        Assert.AreEqual(EstadoChequeoMuro.Revisar, f.EstadoFlexion)
    End Sub

    <TestMethod>
    Public Sub Umbral_EsElUnicoDelPrograma()
        ' 0.90 exacto cumple (UMBRAL_CD), 0.89 no.
        Dim ok = Seccion(0.9F, 1.0F, 0.9F, 1.0F)
        Assert.AreEqual(EstadoChequeoMuro.OK, MurosResumenService.CalcularFilaSeccion(MuroCon(ok), ok).EstadoFlexion)
        Dim mal = Seccion(0.89F, 1.0F, 0.9F, 1.0F)
        Assert.AreEqual(EstadoChequeoMuro.Revisar, MurosResumenService.CalcularFilaSeccion(MuroCon(mal), mal).EstadoFlexion)
    End Sub

    <TestMethod>
    Public Sub ElementoBorde_RequeridoSoloAbajo_SeReporta()
        Dim s = Seccion(1.0F, 1.0F, 1.0F, 1.0F)
        s.Chequeo_EB_D_Bot_Def = "Requiere"
        s.C_D_Top = 0.2F : s.C_D_Bot = 0.8F
        Dim f = MurosResumenService.CalcularFilaSeccion(MuroCon(s), s)
        Assert.AreEqual("Requiere", f.EB_D_Def)
        Assert.AreEqual("No requiere", f.EB_D_Esf)
        Assert.AreEqual(0.8F, f.C_D)
        Assert.IsTrue(f.RequiereEB)
        Assert.AreEqual("Rev. EB", f.EstadoGlobal)
    End Sub

    <TestMethod>
    Public Sub CombinacionCritica_EsLaDelExtremoQueGobierna()
        Dim s = Seccion(1.0F, 1.0F, 1.0F, 1.0F)
        s.Factor_Demanda_Flexo_Top = 1.4F : s.Combinacion_Demanda_Flexo_Top = "TOP"
        s.Factor_Demanda_Flexo_Bot = 1.1F : s.Combinacion_Demanda_Flexo_Bot = "BOT"
        Dim f = MurosResumenService.CalcularFilaSeccion(MuroCon(s), s)
        Assert.AreEqual(1.1F, f.FDI)
        Assert.AreEqual("BOT", f.CombCritica)
    End Sub

    ' ── Reporte Completo (Form_06_01_ResultadosMuros) ──────────────────────

    <TestMethod>
    Public Sub FlexionGobernante_TomaElMenor_YCalculaAsConAgM()
        ' ρ en %, AgM = 0.60 m² -> 1 % = 6000 mm²
        Dim s = Seccion(0.5F, 0.4F, 0.3F, 0.4F)
        s.AgM = 0.6F : s.Lw_Planos = 99.0F : s.tw_Planos = 99.0F   ' Lw·tw no debe usarse
        Dim r = MurosResumenService.FlexionGobernante(s)
        Assert.AreEqual(0.75, r.F, 0.000001)
        Assert.AreEqual(0.3, r.CuantiaCol, 0.000001)
        Assert.AreEqual(1800.0, r.AsCol, 0.01)
        Assert.AreEqual(2400.0, r.AsReq, 0.01)
    End Sub

    <TestMethod>
    Public Sub FlexionGobernante_SinAgM_UsaLwPorTw()
        Dim s = Seccion(1.0F, 1.0F, 1.0F, 1.0F)
        s.AgM = 0F : s.Lw_Planos = 3.0F : s.tw_Planos = 0.2F
        Assert.AreEqual(6000.0, MurosResumenService.FlexionGobernante(s).AsCol, 0.01)
    End Sub

    <TestMethod>
    Public Sub FlexionGobernante_RequeridoCero_EsNaN_NuncaInfinity()
        ' Antes: 0.5/0 = Infinity en la comparación y en la celda.
        Dim ninguno = Seccion(0.5F, 0F, 0.5F, 0F)
        Assert.IsTrue(Double.IsNaN(MurosResumenService.FlexionGobernante(ninguno).F))

        ' Un solo extremo con requerido: gobierna ese, aunque el otro sea "infinito".
        Dim soloBot = Seccion(0.5F, 0F, 0.3F, 0.4F)
        Dim r = MurosResumenService.FlexionGobernante(soloBot)
        Assert.AreEqual(0.75, r.F, 0.000001)
        Assert.AreEqual(0.4, r.CuantiaReq, 0.000001)
    End Sub

    <TestMethod>
    Public Sub RelacionCuantia_RequeridoCero_EsNaN()
        Assert.IsTrue(Double.IsNaN(MurosResumenService.RelacionCuantia(500.0F, 0F)))
        Assert.AreEqual(1.25, MurosResumenService.RelacionCuantia(500.0F, 400.0F), 0.000001)
    End Sub

    <TestMethod>
    Public Sub MurosSinRefuerzo_NoEntranAlReporte()
        Dim s = Seccion(1.0F, 1.0F, 1.0F, 1.0F)
        Dim lista As New List(Of Muro) From {MuroCon(s, calculado:=True), MuroCon(s, calculado:=False), Nothing}
        Assert.AreEqual(1, MurosResumenService.MurosCalculados(lista).Count)
        Assert.AreEqual(0, MurosResumenService.MurosCalculados(Nothing).Count)
    End Sub

End Class
