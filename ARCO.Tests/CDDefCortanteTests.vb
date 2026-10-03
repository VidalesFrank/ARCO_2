Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports ARCO
Imports ARCO.eNumeradores

' Tests de la envolvente C/D (Def) para cortante (VigaService.CDDefZona,
' EvaluarCDDefFrame, EvaluarCDDefViga). La regla se aplica en TODOS los
' reportes de cortante de vigas — cambios acá impactan tabla resumen,
' Excel, Word y planta interactiva.
<TestClass>
Public Class CDDefCortanteTests

    Private Const U As Double = 0.9  ' UMBRAL_CD

    ' ── Helpers ───────────────────────────────────────────────────────────

    Private Function ZTip(pos As PosicionTramoViga, factor As Double,
                          Optional vu As Double = 100, Optional phivn As Double = 100) As cRevisionCortanteZona
        Return New cRevisionCortanteZona With {
            .Posicion = pos, .Factor = factor, .Vu = vu, .phiVn = phivn,
            .Cumple = factor >= U
        }
    End Function

    Private Function ZPla(pos As PosicionTramoViga, factor As Double,
                          Optional vu As Double = 120, Optional phivn As Double = 100) As cRevisionCortantePlasticoZona
        Return New cRevisionCortantePlasticoZona With {
            .Posicion = pos, .Factor = factor, .Vu_diseno = vu, .phiVn = phivn,
            .Cumple = factor >= U
        }
    End Function

    ' ── Zona: reglas de envolvente ────────────────────────────────────────

    <TestMethod>
    Public Sub CDDefZona_CumplenAmbos_TomaElMinimo()
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Izquierda, 1.20),
                                      ZPla(PosicionTramoViga.Izquierda, 1.05))
        Assert.AreEqual(1.05, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.Cumple, r.Estado)
        Assert.IsTrue(r.TienePlastico)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_CumpleSoloTipico_TomaTipico()
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Centro, 1.10),
                                      ZPla(PosicionTramoViga.Centro, 0.80))
        Assert.AreEqual(1.10, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.Cumple, r.Estado)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_CumpleSoloPlastico_TomaPlasticoYMarcaCumplePlastico()
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Derecha, 0.75),
                                      ZPla(PosicionTramoViga.Derecha, 1.10))
        Assert.AreEqual(1.10, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.CumplePlastico, r.Estado)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_NoCumpleNinguno_TomaElMayor()
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Izquierda, 0.60),
                                      ZPla(PosicionTramoViga.Izquierda, 0.80))
        Assert.AreEqual(0.80, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.NoCumple, r.Estado)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_SinPlastico_UsaSoloTipico()
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Centro, 0.95), Nothing)
        Assert.AreEqual(0.95, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.Cumple, r.Estado)
        Assert.IsFalse(r.TienePlastico)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_PlasticoConPhiVnCero_LoTrataComoSinPlastico()
        Dim pla = ZPla(PosicionTramoViga.Izquierda, 1.5, phivn:=0)
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Izquierda, 0.85), pla)
        Assert.AreEqual(0.85, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.NoCumple, r.Estado)
        Assert.IsFalse(r.TienePlastico)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_SinDatosNiTipicoNiPlastico_MarcaSinDatos()
        Dim r = VigaService.CDDefZona(Nothing, Nothing)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.SinDatos, r.Estado)
        Assert.AreEqual(0.0, r.CD_Def, 1.0E-9)
    End Sub

    <TestMethod>
    Public Sub CDDefZona_FronteraExactamenteEnElUmbral_Cumple()
        Dim r = VigaService.CDDefZona(ZTip(PosicionTramoViga.Izquierda, U),
                                      ZPla(PosicionTramoViga.Izquierda, U))
        Assert.AreEqual(U, r.CD_Def, 1.0E-9)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.Cumple, r.Estado)
    End Sub

    ' ── Frame: agregación por zonas ──────────────────────────────────────

    <TestMethod>
    Public Sub EvaluarCDDefFrame_TodasLasZonasCumplen_EtiquetaCumpleYGobiernaLaMenor()
        Dim f As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {
                ZTip(PosicionTramoViga.Izquierda, 1.30),
                ZTip(PosicionTramoViga.Centro, 1.10),
                ZTip(PosicionTramoViga.Derecha, 1.50)
            }
        }
        Dim r = VigaService.EvaluarCDDefFrame(f)
        Assert.AreEqual(1.10, r.CD_Def, 1.0E-9)
        Assert.AreEqual(PosicionTramoViga.Centro, r.ZonaGobernante.Posicion)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.Cumple, r.Estado)
    End Sub

    <TestMethod>
    Public Sub EvaluarCDDefFrame_UnaZonaRescatadaConPlastico_EtiquetaCumplePlastico()
        ' Izquierda: típ=0.85 (falla) + plás=1.20 (rescate) → CD_Def = 1.20
        ' Centro:    típ=1.30 + plás=1.40 → cumplen ambos → CD_Def = min = 1.30
        ' Derecha:   típ=1.50 + plás=1.60 → cumplen ambos → CD_Def = min = 1.50
        ' Zona gobernante = la de menor CD_Def = Izquierda (que fue la rescatada).
        Dim cp As New cResultadoCortantePlasticoFrame With {
            .ZonaIzq = ZPla(PosicionTramoViga.Izquierda, 1.20),
            .ZonaDer = ZPla(PosicionTramoViga.Derecha, 1.60),
            .ZonaCentro = ZPla(PosicionTramoViga.Centro, 1.40)
        }
        Dim f As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {
                ZTip(PosicionTramoViga.Izquierda, 0.85),
                ZTip(PosicionTramoViga.Centro, 1.30),
                ZTip(PosicionTramoViga.Derecha, 1.50)
            },
            .CortantePlastico = cp
        }
        Dim r = VigaService.EvaluarCDDefFrame(f)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.CumplePlastico, r.Estado)
        Assert.AreEqual(PosicionTramoViga.Izquierda, r.ZonaGobernante.Posicion)
        Assert.AreEqual(1.20, r.CD_Def, 1.0E-9)
    End Sub

    <TestMethod>
    Public Sub EvaluarCDDefFrame_CentroFallaTipYNoTienePlastico_MarcaNoCumple()
        ' El bug histórico: antes se decía "plástico nunca rescata Centro". Con la
        ' nueva regla zona-a-zona, si NO hay plástico para Centro, el estado
        ' depende únicamente del típico — pero si hay plástico para Centro, sí
        ' puede rescatar. Este test verifica el caso "sin plástico en Centro".
        Dim cp As New cResultadoCortantePlasticoFrame With {
            .ZonaIzq = ZPla(PosicionTramoViga.Izquierda, 1.50),
            .ZonaDer = ZPla(PosicionTramoViga.Derecha, 1.50),
            .ZonaCentro = Nothing
        }
        Dim f As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {
                ZTip(PosicionTramoViga.Izquierda, 1.20),
                ZTip(PosicionTramoViga.Centro, 0.70),   ' falla y sin plástico
                ZTip(PosicionTramoViga.Derecha, 1.20)
            },
            .CortantePlastico = cp
        }
        Dim r = VigaService.EvaluarCDDefFrame(f)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.NoCumple, r.Estado)
        Assert.AreEqual(PosicionTramoViga.Centro, r.ZonaGobernante.Posicion)
    End Sub

    <TestMethod>
    Public Sub EvaluarCDDefFrame_CentroRescatadoConPlastico_Cumple()
        ' Contraparte del anterior: el plástico SÍ rescata Centro cuando existe.
        Dim cp As New cResultadoCortantePlasticoFrame With {
            .ZonaIzq = ZPla(PosicionTramoViga.Izquierda, 1.50),
            .ZonaDer = ZPla(PosicionTramoViga.Derecha, 1.50),
            .ZonaCentro = ZPla(PosicionTramoViga.Centro, 1.10)
        }
        Dim f As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {
                ZTip(PosicionTramoViga.Izquierda, 1.20),
                ZTip(PosicionTramoViga.Centro, 0.75),
                ZTip(PosicionTramoViga.Derecha, 1.20)
            },
            .CortantePlastico = cp
        }
        Dim r = VigaService.EvaluarCDDefFrame(f)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.CumplePlastico, r.Estado)
        Assert.AreEqual(PosicionTramoViga.Centro, r.ZonaGobernante.Posicion)
        Assert.AreEqual(1.10, r.CD_Def, 1.0E-9)
    End Sub

    <TestMethod>
    Public Sub EvaluarCDDefFrame_SinRevisionCortante_MarcaSinDatos()
        Dim f As New cFrame
        Dim r = VigaService.EvaluarCDDefFrame(f)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.SinDatos, r.Estado)
    End Sub

    <TestMethod>
    Public Sub EvaluarCDDefFrame_FrameNulo_NoRevienta()
        Dim r = VigaService.EvaluarCDDefFrame(Nothing)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.SinDatos, r.Estado)
    End Sub

    ' ── Viga: envolvente entre frames ─────────────────────────────────────

    <TestMethod>
    Public Sub EvaluarCDDefViga_TomaElMinimoEntreFrames()
        Dim f1 As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {
                ZTip(PosicionTramoViga.Izquierda, 1.30)
            }
        }
        Dim f2 As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {
                ZTip(PosicionTramoViga.Centro, 0.95)
            }
        }
        Dim v As New cViga With {.Frames = New List(Of cFrame) From {f1, f2}}
        Dim r = VigaService.EvaluarCDDefViga(v)
        Assert.AreEqual(0.95, r.CD_Def, 1.0E-9)
        Assert.AreEqual(PosicionTramoViga.Centro, r.ZonaGobernante.Posicion)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.Cumple, r.Estado)
    End Sub

    <TestMethod>
    Public Sub EvaluarCDDefViga_UnFrameConPlasticoContagiaLaEtiqueta()
        Dim f1 As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {ZTip(PosicionTramoViga.Izquierda, 1.30)}
        }
        Dim f2 As New cFrame With {
            .RevisionCortante = New List(Of cRevisionCortanteZona) From {ZTip(PosicionTramoViga.Derecha, 0.85)},
            .CortantePlastico = New cResultadoCortantePlasticoFrame With {
                .ZonaDer = ZPla(PosicionTramoViga.Derecha, 1.15)
            }
        }
        Dim v As New cViga With {.Frames = New List(Of cFrame) From {f1, f2}}
        Dim r = VigaService.EvaluarCDDefViga(v)
        Assert.AreEqual(VigaService.EstadoEnvolventeCortante.CumplePlastico, r.Estado)
    End Sub

    <TestMethod>
    Public Sub EtiquetaZona_TextoCortoPorPosicion()
        Assert.AreEqual("Izq", VigaService.EtiquetaZona(PosicionTramoViga.Izquierda))
        Assert.AreEqual("Cen", VigaService.EtiquetaZona(PosicionTramoViga.Centro))
        Assert.AreEqual("Der", VigaService.EtiquetaZona(PosicionTramoViga.Derecha))
    End Sub

End Class
