Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO
Imports ARCO.eNumeradores

' VigaService.AplicarGruposManual — fix 2026-11-05.
'
' Historia: Frank reportó que al "Actualizar demandas" (y, en general, al
' Procesar) las vigas que había separado a mano volvían a quedar unidas. Causa:
' GenerarVigas auto-agrupa frames alineados y GruposManual solo guardaba la
' viga "positiva" editada — los residuales (frames que el usuario sacó) no
' quedaban como grupos singletes, así que la auto-agrupación los re-unía con
' sus vecinos.
'
' Fix: cada residual se persiste como [label] en GruposManual. AplicarGruposManual
' con singletes los saca a su propia viga. Además, si el label está en
' GruposManualExcluidos, la viga nueva nace con ExcluirDeRevision = True.
<TestClass>
Public Class AplicarGruposManualTests

    Private Shared Function Joint(label As String, x As Double, y As Double) As cJoint
        Return New cJoint With {
            .Story = "N1",
            .ObjectLabel = label,
            .ElementLabel = label,
            .GlobalX = x, .GlobalY = y, .GlobalZ = 3.0
        }
    End Function

    Private Shared Function Frame(label As String, jointI As String, jointJ As String) As cFrame
        Return New cFrame With {
            .Story = "N1",
            .ObjectLabel = label,
            .JointI = jointI,
            .JointJ = jointJ,
            .Longitud = 1.0
        }
    End Function

    ' Viga pre-existente con los 4 frames auto-agrupados (como los dejaría
    ' GenerarVigas). Los joints van a lo largo del eje X.
    Private Shared Function VigaAutoAgrupada() As (viga As cViga, jointsDict As Dictionary(Of String, cJoint))
        Dim jd As New Dictionary(Of String, cJoint) From {
            {"J0", Joint("J0", 0.0, 0.0)},
            {"J1", Joint("J1", 1.0, 0.0)},
            {"J2", Joint("J2", 2.0, 0.0)},
            {"J3", Joint("J3", 3.0, 0.0)},
            {"J4", Joint("J4", 4.0, 0.0)}
        }

        Dim v As New cViga With {.Piso = "N1", .Nombre = "VIGA-1", .Name_Beam = "VIGA-1"}
        v.Frames.Add(Frame("B1", "J0", "J1"))
        v.Frames.Add(Frame("B2", "J1", "J2"))
        v.Frames.Add(Frame("B3", "J2", "J3"))
        v.Frames.Add(Frame("B4", "J3", "J4"))
        Return (v, jd)
    End Function

    Private Shared ReadOnly svc As New VigaService(New GeometryService())

    <TestMethod>
    Public Sub SingletePrevisto_SacaFrameASuPropiaVigaYDejaElResto()
        ' Escenario: usuario separó B3 de [B1,B2,B3,B4]. Al Procesar,
        ' GenerarVigas los volvió a unir en una sola viga. AplicarGruposManual
        ' con el grupo [B3] (singlete) debe sacarlo a una viga nueva y dejar
        ' la original con [B1,B2,B4].
        Dim datos = VigaAutoAgrupada()
        Dim vigas As New List(Of cViga) From {datos.viga}
        Dim grupos As New List(Of List(Of String)) From {
            New List(Of String) From {"B3"}
        }

        svc.AplicarGruposManual(vigas, grupos, datos.jointsDict)

        Assert.AreEqual(2, vigas.Count, "deben quedar 2 vigas (la restante + el singlete)")
        Dim vSinglete = vigas.First(Function(v) v.Frames.Count = 1)
        Dim vResto = vigas.First(Function(v) v.Frames.Count > 1)

        Assert.AreEqual("B3", vSinglete.Frames(0).ObjectLabel)
        Assert.AreEqual(3, vResto.Frames.Count)
        CollectionAssert.AreEquivalent(
            New String() {"B1", "B2", "B4"},
            vResto.Frames.Select(Function(f) f.ObjectLabel).ToList())
    End Sub

    <TestMethod>
    Public Sub VariosSingletes_CadaUnoASuPropiaVigaYLaContenedoraNoLosUne()
        ' Caso central del bug: usuario separó B3 Y B4 → la viga quedó con
        ' [B1,B2] + residuales [B3] y [B4]. GenerarVigas al refresh los
        ' autovence en [B1..B4]; AplicarGruposManual debe dejar 3 vigas:
        ' [B1,B2], [B3] y [B4] por separado.
        Dim datos = VigaAutoAgrupada()
        Dim vigas As New List(Of cViga) From {datos.viga}
        Dim grupos As New List(Of List(Of String)) From {
            New List(Of String) From {"B1", "B2"},
            New List(Of String) From {"B3"},
            New List(Of String) From {"B4"}
        }

        svc.AplicarGruposManual(vigas, grupos, datos.jointsDict)

        Assert.AreEqual(3, vigas.Count, "deben quedar 3 vigas")
        Dim vPorFrame = vigas.ToDictionary(
            Function(v) String.Join("|", v.Frames.Select(Function(f) f.ObjectLabel)))
        Assert.IsTrue(vPorFrame.ContainsKey("B1|B2"), "viga positiva [B1,B2]")
        Assert.IsTrue(vPorFrame.ContainsKey("B3"), "singlete B3")
        Assert.IsTrue(vPorFrame.ContainsKey("B4"), "singlete B4")
    End Sub

    <TestMethod>
    Public Sub SingleteConExcluido_VigaNaceConExcluirDeRevision()
        Dim datos = VigaAutoAgrupada()
        Dim vigas As New List(Of cViga) From {datos.viga}
        Dim grupos As New List(Of List(Of String)) From {
            New List(Of String) From {"B3"}
        }
        Dim excluidos As New HashSet(Of String) From {"B3"}

        svc.AplicarGruposManual(vigas, grupos, datos.jointsDict, excluidos)

        Dim vB3 = vigas.First(Function(v) v.Frames.Any(Function(f) f.ObjectLabel = "B3"))
        Assert.IsTrue(vB3.ExcluirDeRevision, "el singlete marcado por el usuario debe nacer excluido")
    End Sub

    <TestMethod>
    Public Sub SingleteSinExcluidos_VigaQuedaIncluida()
        ' Si el usuario no marcó "excluir" al separar, el residual queda como
        ' viga normal — IncluirEnRevision = True (ExcluirDeRevision = False).
        Dim datos = VigaAutoAgrupada()
        Dim vigas As New List(Of cViga) From {datos.viga}
        Dim grupos As New List(Of List(Of String)) From {
            New List(Of String) From {"B3"}
        }

        svc.AplicarGruposManual(vigas, grupos, datos.jointsDict, excluidos:=Nothing)

        Dim vB3 = vigas.First(Function(v) v.Frames.Any(Function(f) f.ObjectLabel = "B3"))
        Assert.IsFalse(vB3.ExcluirDeRevision, "sin set de excluidos la viga nace incluida")
        Assert.IsTrue(vB3.IncluirEnRevision)
    End Sub

    <TestMethod>
    Public Sub LabelDeOtroGrupoNoEstaEnExcluidos_NoAfectaAEsteGrupo()
        ' El set de excluidos aplica label-por-label. Si B3 está en excluidos
        ' pero este grupo es [B1,B2], la viga nueva NO debe heredar la marca.
        Dim datos = VigaAutoAgrupada()
        Dim vigas As New List(Of cViga) From {datos.viga}
        Dim grupos As New List(Of List(Of String)) From {
            New List(Of String) From {"B1", "B2"}
        }
        Dim excluidos As New HashSet(Of String) From {"B3"}

        svc.AplicarGruposManual(vigas, grupos, datos.jointsDict, excluidos)

        Dim vB1B2 = vigas.First(Function(v) v.Frames.Any(Function(f) f.ObjectLabel = "B1"))
        Assert.IsFalse(vB1B2.ExcluirDeRevision, "B3 excluido no debe contagiar al grupo [B1,B2]")
    End Sub

    <TestMethod>
    Public Sub GrupoConLabelInexistente_NoRevienta()
        ' Si un .esm viejo tiene un label que ya no existe en el modelo (el
        ' usuario borró vigas), AplicarGruposManual debe ignorarlo sin excepción.
        Dim datos = VigaAutoAgrupada()
        Dim vigas As New List(Of cViga) From {datos.viga}
        Dim grupos As New List(Of List(Of String)) From {
            New List(Of String) From {"B_INEXISTENTE"}
        }

        svc.AplicarGruposManual(vigas, grupos, datos.jointsDict)

        ' Nada cambió: una sola viga con los 4 frames originales.
        Assert.AreEqual(1, vigas.Count)
        Assert.AreEqual(4, vigas(0).Frames.Count)
    End Sub

End Class
