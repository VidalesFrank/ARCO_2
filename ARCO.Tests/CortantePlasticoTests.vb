Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO
Imports ARCO.eNumeradores

' VigaService.CalcularCortantePlastico: guardia por frame y limpieza de residuo.
'
' Regla: el chequeo plástico solo se corre en frames que tengan refuerzo a flexión
' ingresado en AMBAS caras (RefuerzoSuperior Y RefuerzoInferior con al menos un
' tramo cada una). Sin superior Mn⁻ = 0 y sin inferior Mn⁺ = 0, y el Ve que sale
' de (Mn⁻_izq + Mn⁺_der)/Ln no corresponde a ningún mecanismo real de volteo
' plástico — lo que se reporta es un cortante convencional disfrazado de plástico.
' Hasta 2026-11-04 la guarda era a nivel de VIGA (any frame con refuerzo), así que
' vigas mixtas y vigas con solo una cara armada igualmente se chequeaban.
<TestClass>
Public Class CortantePlasticoTests

    Private Shared Function NuevaSeccion() As cSeccion
        Return New cSeccion With {
            .b = 0.3, .h = 0.5, .recubrimiento = 0.05,
            .fc = 28.0, .fy = 420.0
        }
    End Function

    Private Shared Function RefTramo(pos As PosicionTramoViga, barras As Integer) As cRefuerzoTramo
        Dim t As New cRefuerzoTramo With {.Posicion = pos, .Frame = ""}
        t.Barras("#5") = barras
        Return t
    End Function

    Private Shared Function RevFlex(pos As PosicionTramoViga, asProvSup As Double, asProvInf As Double) As cRevisionFlexionZona
        Return New cRevisionFlexionZona With {
            .Posicion = pos,
            .ResultadoActual = New cResultadoFlexion With {.AsProvSup = asProvSup, .AsProvInf = asProvInf},
            .ResultadoBase = New cResultadoFlexion With {.AsProvSup = asProvSup, .AsProvInf = asProvInf}
        }
    End Function

    Private Shared Function RefTrans(pos As PosicionTramoViga) As cRefuerzoTransversalZona
        Return New cRefuerzoTransversalZona With {
            .Posicion = pos, .NumEstribos = 10, .NumeroBarra = 3,
            .CantEstribos = 2, .Separacion = 0.1
        }
    End Function

    Private Shared Function NuevoFrame(label As String, piso As String, conRefuerzo As Boolean) As cFrame
        Dim f As New cFrame With {
            .Story = piso, .ObjectLabel = label, .Section = NuevaSeccion(),
            .Longitud = 5.0, .EstacionInicio = 0.0, .EstacionFinal = 5.0
        }
        ' Revisión de flexión y refuerzo transversal SIEMPRE existen: el flujo de
        ' DesignVigas y AsignarRefuerzoTransversalAutomatico los pone. Lo que
        ' distingue "frame con refuerzo" de "frame sin refuerzo" es que
        ' RefuerzoSuperior/Inferior tengan tramos y los AsProv* no sean cero.
        f.RevisionFlexion.Add(RevFlex(PosicionTramoViga.Izquierda, If(conRefuerzo, 1000.0, 0.0), If(conRefuerzo, 600.0, 0.0)))
        f.RevisionFlexion.Add(RevFlex(PosicionTramoViga.Derecha, If(conRefuerzo, 1000.0, 0.0), If(conRefuerzo, 600.0, 0.0)))
        f.RefuerzoTransversal.Add(RefTrans(PosicionTramoViga.Izquierda))
        f.RefuerzoTransversal.Add(RefTrans(PosicionTramoViga.Derecha))
        f.RefuerzoTransversal.Add(RefTrans(PosicionTramoViga.Centro))
        If conRefuerzo Then
            f.RefuerzoSuperior.Add(RefTramo(PosicionTramoViga.Izquierda, 4))
            f.RefuerzoSuperior.Add(RefTramo(PosicionTramoViga.Derecha, 4))
            f.RefuerzoInferior.Add(RefTramo(PosicionTramoViga.Izquierda, 3))
            f.RefuerzoInferior.Add(RefTramo(PosicionTramoViga.Derecha, 3))
        End If
        Return f
    End Function

    Private Shared Function NuevasBeamForces(labels As String(), piso As String) As List(Of cCombinacionBeamForce)
        Dim bf As New List(Of cCombinacionBeamForce)
        For Each lbl In labels
            ' 3 estaciones por frame con una combinación sísmica
            For Each stn As Double In {0.0, 2.5, 5.0}
                bf.Add(New cCombinacionBeamForce With {
                    .Story = piso, .Beam = lbl, .LoadCaseCombo = "1.2D+1.0E+1.0L", .stepType = "",
                    .Station = stn, .V2 = -40.0, .P = 0.0
                })
            Next
        Next
        Return bf
    End Function

    <TestMethod>
    Public Sub FrameSinRefuerzo_NoRecibeCortantePlastico()
        Dim viga As New cViga With {.Nombre = "V1", .Piso = "N1"}
        Dim fConRef = NuevoFrame("B10", "N1", conRefuerzo:=True)
        Dim fSinRef = NuevoFrame("B11", "N1", conRefuerzo:=False)
        viga.Frames.Add(fConRef)
        viga.Frames.Add(fSinRef)

        Dim bf = NuevasBeamForces({"B10", "B11"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, 1.0, esDES:=False)

        Assert.IsNotNull(fConRef.CortantePlastico, "frame con refuerzo debe tener plástico calculado")
        Assert.IsNull(fSinRef.CortantePlastico, "frame sin refuerzo NO debe tener plástico")
    End Sub

    <TestMethod>
    Public Sub FramePerdioRefuerzo_LimpiaResultadoViejo()
        ' El usuario calcula con refuerzo, luego lo borra y recalcula. CortantePlastico
        ' anterior no puede sobrevivir — reportes y planta lo tomarían como vigente.
        Dim viga As New cViga With {.Nombre = "V2", .Piso = "N1"}
        Dim fConRef = NuevoFrame("B20", "N1", conRefuerzo:=True)
        Dim fPerdioRef = NuevoFrame("B21", "N1", conRefuerzo:=True)
        viga.Frames.Add(fConRef)
        viga.Frames.Add(fPerdioRef)

        Dim bf = NuevasBeamForces({"B20", "B21"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        ' Primera corrida: ambos tienen plástico.
        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, 1.0, esDES:=False)
        Assert.IsNotNull(fPerdioRef.CortantePlastico, "precondición: la primera corrida debe llenarlo")

        ' El usuario borra el refuerzo de fPerdioRef y recalcula.
        fPerdioRef.RefuerzoSuperior.Clear()
        fPerdioRef.RefuerzoInferior.Clear()
        fPerdioRef.RevisionFlexion.Clear()
        fPerdioRef.RevisionFlexion.Add(RevFlex(PosicionTramoViga.Izquierda, 0, 0))
        fPerdioRef.RevisionFlexion.Add(RevFlex(PosicionTramoViga.Derecha, 0, 0))

        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, 1.0, esDES:=False)

        Assert.IsNotNull(fConRef.CortantePlastico, "el frame con refuerzo sigue teniendo plástico")
        Assert.IsNull(fPerdioRef.CortantePlastico, "el frame que perdió refuerzo queda sin plástico")
    End Sub

    <TestMethod>
    Public Sub FrameSoloConRefuerzoInferior_NoRecibeCortantePlastico()
        ' Sin refuerzo superior → Mn⁻ = 0 en apoyos → el volteo plástico no puede
        ' formar rótulas negativas. La fórmula de Ve queda parcial y no aplica.
        Dim viga As New cViga With {.Nombre = "V4", .Piso = "N1"}
        Dim f As cFrame = NuevoFrame("B40", "N1", conRefuerzo:=True)
        f.RefuerzoSuperior.Clear()
        ' La revisión también se deja como si no hubiera superior (AsProvSup = 0).
        For Each r In f.RevisionFlexion : r.ResultadoActual.AsProvSup = 0 : r.ResultadoBase.AsProvSup = 0 : Next
        viga.Frames.Add(f)

        Dim bf = NuevasBeamForces({"B40"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, 1.0, esDES:=False)

        Assert.IsNull(f.CortantePlastico, "sin refuerzo superior el plástico no aplica")
    End Sub

    <TestMethod>
    Public Sub FrameSoloConRefuerzoSuperior_NoRecibeCortantePlastico()
        ' Mn⁺ = 0 en apoyos — el volteo plástico tampoco puede formar rótulas
        ' positivas. Mismo razonamiento, simétrico al caso anterior.
        Dim viga As New cViga With {.Nombre = "V5", .Piso = "N1"}
        Dim f As cFrame = NuevoFrame("B50", "N1", conRefuerzo:=True)
        f.RefuerzoInferior.Clear()
        For Each r In f.RevisionFlexion : r.ResultadoActual.AsProvInf = 0 : r.ResultadoBase.AsProvInf = 0 : Next
        viga.Frames.Add(f)

        Dim bf = NuevasBeamForces({"B50"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, 1.0, esDES:=False)

        Assert.IsNull(f.CortantePlastico, "sin refuerzo inferior el plástico no aplica")
    End Sub

    <TestMethod>
    Public Sub TieneRefuerzoFlexion_TramoConDiccionarioBarrasVacio_EsNoTieneRefuerzo()
        ' .esm viejo: existe cRefuerzoTramo pero su Barras está vacío (quedó por
        ' un GuardarRefuerzo sin barras). El helper no debe aceptarlo como refuerzo.
        Dim f As New cFrame
        f.RefuerzoSuperior.Add(New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda})
        f.RefuerzoInferior.Add(New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda})
        Assert.IsFalse(VigaService.TieneRefuerzoFlexion(f))
    End Sub

    <TestMethod>
    Public Sub TieneRefuerzoFlexion_TramoConCantidadCeroDeBarras_EsNoTieneRefuerzo()
        Dim f As New cFrame
        Dim tSup As New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda}
        tSup.Barras("#5") = 0  ' usuario que puso una fila pero no indicó cantidad
        f.RefuerzoSuperior.Add(tSup)
        Dim tInf As New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda}
        tInf.Barras("#5") = 0
        f.RefuerzoInferior.Add(tInf)
        Assert.IsFalse(VigaService.TieneRefuerzoFlexion(f))
    End Sub

    <TestMethod>
    Public Sub TieneRefuerzoFlexion_SoloUnaCaraConBarras_EsNoTieneRefuerzo()
        Dim f As New cFrame
        Dim tSup As New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda}
        tSup.Barras("#5") = 3
        f.RefuerzoSuperior.Add(tSup)
        ' Inferior ausente
        Assert.IsFalse(VigaService.TieneRefuerzoFlexion(f))
    End Sub

    <TestMethod>
    Public Sub TieneRefuerzoFlexion_AmbasCarasConBarras_EsTieneRefuerzo()
        Dim f As New cFrame
        Dim tSup As New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda}
        tSup.Barras("#5") = 3
        f.RefuerzoSuperior.Add(tSup)
        Dim tInf As New cRefuerzoTramo With {.Posicion = PosicionTramoViga.Izquierda}
        tInf.Barras("#5") = 2
        f.RefuerzoInferior.Add(tInf)
        Assert.IsTrue(VigaService.TieneRefuerzoFlexion(f))
    End Sub

    <TestMethod>
    Public Sub CortantePlasticoVigente_IgnoraResultadoSerializadoSinRefuerzo()
        ' Simula el caso de un .esm viejo: CortantePlastico asignado aunque el
        ' frame no tenga refuerzo (el filtro por frame no existía antes del
        ' 2026-11-04, así que el resultado quedó serializado). Todos los
        ' consumidores deben pasar por este helper y verlo como Nothing.
        Dim f As cFrame = NuevoFrame("B99", "N1", conRefuerzo:=False)
        f.CortantePlastico = New cResultadoCortantePlasticoFrame With {.Ln = 5.0, .Mn_neg_izq = 0, .Mn_pos_der = 0}
        Assert.IsNotNull(f.CortantePlastico, "precondición: viene asignado")
        Assert.IsNull(VigaService.CortantePlasticoVigente(f), "sin refuerzo, no debe reportarse")

        ' Con solo refuerzo superior tampoco se reporta
        f.RefuerzoSuperior.Add(RefTramo(PosicionTramoViga.Izquierda, 4))
        Assert.IsNull(VigaService.CortantePlasticoVigente(f), "solo superior no basta")

        ' Con ambos sí
        f.RefuerzoInferior.Add(RefTramo(PosicionTramoViga.Izquierda, 3))
        Assert.IsNotNull(VigaService.CortantePlasticoVigente(f), "con sup + inf se reporta")
    End Sub

    ' ═════════════════════════════════════════════════════════════════════════
    '  Regla NSR-10 C.21.5.4.2 → Vc = 0 dentro de 2h si Ve ≥ 0.5·Vu.
    '  Aplica a DES. DMO (C.21.3.4.2) conserva Vc íntegro.
    '  Hasta 2026-11-05 la regla se aplicaba sin distinguir, por lo que en
    '  proyectos DMO Vc salía en 0 en apoyos sin fundamento normativo.
    ' ═════════════════════════════════════════════════════════════════════════

    <TestMethod>
    Public Sub DMO_Apoyo_VcNoSeAnulaAunConVeDominante()
        ' Frame base: Ve ≈ 57 kN, Vg = -40 kN → Vu ≈ 97, Ve/Vu ≈ 0.59 > 0.5.
        ' En DES anularía Vc por C.21.5.4.2; en DMO no.
        Dim viga As New cViga With {.Nombre = "V_DMO", .Piso = "N1"}
        viga.Frames.Add(NuevoFrame("B_DMO", "N1", conRefuerzo:=True))

        Dim bf = NuevasBeamForces({"B_DMO"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, fy_factor:=1.0, esDES:=False)

        Dim cp = viga.Frames(0).CortantePlastico
        Assert.IsNotNull(cp, "precondición: el frame tiene refuerzo y debe calcular plástico")
        Assert.IsTrue(cp.ZonaIzq.Vc > 0, $"DMO: Vc en apoyo izq debe conservarse > 0, salió {cp.ZonaIzq.Vc}")
        Assert.IsTrue(cp.ZonaDer.Vc > 0, $"DMO: Vc en apoyo der debe conservarse > 0, salió {cp.ZonaDer.Vc}")
    End Sub

    <TestMethod>
    Public Sub DES_Apoyo_VcSeAnulaCuandoVeDomina()
        ' Mismo frame bajo DES — ahora sí aplica C.21.5.4.2 y Vc=0 en apoyos.
        Dim viga As New cViga With {.Nombre = "V_DES", .Piso = "N1"}
        viga.Frames.Add(NuevoFrame("B_DES", "N1", conRefuerzo:=True))

        Dim bf = NuevasBeamForces({"B_DES"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, fy_factor:=1.25, esDES:=True)

        Dim cp = viga.Frames(0).CortantePlastico
        Assert.IsNotNull(cp)
        Assert.AreEqual(0.0, cp.ZonaIzq.Vc, 1.0E-6, "DES: Vc debe anularse en apoyo izq (Ve ≥ 0.5·Vu, dentro de 2h)")
        Assert.AreEqual(0.0, cp.ZonaDer.Vc, 1.0E-6, "DES: Vc debe anularse en apoyo der")
    End Sub

    <TestMethod>
    Public Sub VigaCompletamenteSinRefuerzo_NoCalculaPlasticoEnNingunFrame()
        Dim viga As New cViga With {.Nombre = "V3", .Piso = "N1"}
        viga.Frames.Add(NuevoFrame("B30", "N1", conRefuerzo:=False))
        viga.Frames.Add(NuevoFrame("B31", "N1", conRefuerzo:=False))

        Dim bf = NuevasBeamForces({"B30", "B31"}, "N1")
        Dim combos As New HashSet(Of String)({"1.2D+1.0E+1.0L"}, StringComparer.OrdinalIgnoreCase)

        Dim svc As New VigaService(New GeometryService())
        svc.CalcularCortantePlastico(New List(Of cViga) From {viga}, bf, combos, 1.0, esDES:=False)

        For Each f In viga.Frames
            Assert.IsNull(f.CortantePlastico, $"frame {f.ObjectLabel} no debería tener plástico")
        Next
    End Sub

End Class
