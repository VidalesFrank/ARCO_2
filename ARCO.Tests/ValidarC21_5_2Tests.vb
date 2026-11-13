Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports ARCO
Imports ARCO.eNumeradores

' VigaService.ValidarC21_5_2 — advertencia post-redistribución.
'
' NSR-10:
'   • DES (C.21.5.2.2):
'       - En apoyos: Mn⁺ ≥ Mn⁻ / 2
'       - En cualquier sección: Mn ≥ max(Mn⁻_apoyos) / 4
'   • DMO (C.21.3.4.1):
'       - En apoyos: Mn⁺ ≥ Mn⁻ / 3
'       - En cualquier sección: Mn ≥ max(Mn⁻_apoyos) / 5
'
' Se evalúa Mn sobre AsProv (refuerzo colocado), no sobre AsReq. No bloquea:
' solo retorna un string si hay violación.
<TestClass>
Public Class ValidarC21_5_2Tests

    Private Shared Function Seccion() As cSeccion
        Return New cSeccion With {
            .b = 0.3, .h = 0.5, .recubrimiento = 0.05,
            .fc = 28.0, .fy = 420.0
        }
    End Function

    Private Shared Function Zona(pos As PosicionTramoViga, asSup As Double, asInf As Double) As cRevisionFlexionZona
        Return New cRevisionFlexionZona With {
            .Posicion = pos,
            .ResultadoActual = New cResultadoFlexion With {.AsProvSup = asSup, .AsProvInf = asInf},
            .ResultadoBase = New cResultadoFlexion With {.AsProvSup = asSup, .AsProvInf = asInf}
        }
    End Function

    Private Shared Function Frame(asSupIzq As Double, asInfIzq As Double,
                                  asSupCen As Double, asInfCen As Double,
                                  asSupDer As Double, asInfDer As Double) As cFrame
        Dim f As New cFrame With {.Section = Seccion()}
        f.RevisionFlexion.Add(Zona(PosicionTramoViga.Izquierda, asSupIzq, asInfIzq))
        f.RevisionFlexion.Add(Zona(PosicionTramoViga.Centro, asSupCen, asInfCen))
        f.RevisionFlexion.Add(Zona(PosicionTramoViga.Derecha, asSupDer, asInfDer))
        Return f
    End Function

    Private Shared ReadOnly svc As New VigaService(New GeometryService())

    <TestMethod>
    Public Sub SinRefuerzoColocado_NoValida()
        ' AsProv = 0 en todas las zonas → el chequeo no aplica aún.
        Dim f = Frame(0, 0, 0, 0, 0, 0)
        Assert.IsNull(svc.ValidarC21_5_2(f, esDES:=True))
        Assert.IsNull(svc.ValidarC21_5_2(f, esDES:=False))
    End Sub

    <TestMethod>
    Public Sub DES_ApoyoCumpleRelacion1A2_NoAdvierte()
        ' Mn⁺/Mn⁻ ≥ 1/2 en apoyos. Con As_sup=1000 y As_inf=600 el ratio
        ' Mn⁺/Mn⁻ queda muy por encima de 0.5 (los ratios de Mn son casi
        ' proporcionales a As cuando a/2 ≪ d).
        Dim f = Frame(1000, 600, 500, 500, 1000, 600)
        Assert.IsNull(svc.ValidarC21_5_2(f, esDES:=True))
    End Sub

    <TestMethod>
    Public Sub DES_ApoyoIzqConPocoInferior_Advierte()
        ' As_sup grande, As_inf chico → Mn⁺ < Mn⁻/2. Debe advertir.
        Dim f = Frame(asSupIzq:=1500, asInfIzq:=200,
                      asSupCen:=500, asInfCen:=500,
                      asSupDer:=1000, asInfDer:=600)
        Dim adv = svc.ValidarC21_5_2(f, esDES:=True)
        Assert.IsNotNull(adv)
        StringAssert.Contains(adv, "Apoyo izq")
        StringAssert.Contains(adv, "C.21.5.2.2")
    End Sub

    <TestMethod>
    Public Sub DMO_MismoCaso_NoAdvierteSiCumpleRelacion1A3()
        ' Mn⁺/Mn⁻ necesita ≥ 1/3 solo. Con As_sup=600, As_inf=250 el ratio
        ' queda ≈ 0.42 — pasa DMO pero falla DES.
        Dim f = Frame(asSupIzq:=600, asInfIzq:=250,
                      asSupCen:=300, asInfCen:=300,
                      asSupDer:=600, asInfDer:=250)
        Assert.IsNull(svc.ValidarC21_5_2(f, esDES:=False), "DMO acepta Mn⁺/Mn⁻ ≥ 1/3")
        Dim advDes = svc.ValidarC21_5_2(f, esDES:=True)
        Assert.IsNotNull(advDes, "DES rechaza porque el mismo ratio < 1/2")
    End Sub

    <TestMethod>
    Public Sub DES_CentroPobre_AdvierteRelacion1A4()
        ' Mn centro+ debe ser ≥ Mn⁻_max_apoyo / 4. Con As_sup_izq grande y
        ' As_inf_cen muy chico, el centro queda corto.
        Dim f = Frame(asSupIzq:=2000, asInfIzq:=1200,
                      asSupCen:=200, asInfCen:=100,
                      asSupDer:=2000, asInfDer:=1200)
        Dim adv = svc.ValidarC21_5_2(f, esDES:=True)
        Assert.IsNotNull(adv)
        StringAssert.Contains(adv, "Centro")
    End Sub

    <TestMethod>
    Public Sub DMO_CentroSinRefuerzoSuperior_NoAdvierteSobreEsaCara()
        ' Si el usuario no armó la cara superior del centro (AsProvSup = 0),
        ' la norma no exige valor mínimo para una cara ausente. Solo se
        ' chequean las caras con refuerzo colocado > 0.
        Dim f = Frame(asSupIzq:=800, asInfIzq:=400,
                      asSupCen:=0, asInfCen:=800,
                      asSupDer:=800, asInfDer:=400)
        Dim adv = svc.ValidarC21_5_2(f, esDES:=False)
        ' Si hay advertencia no debe mencionar "Centro: Mn⁻" porque esa cara
        ' no tiene refuerzo. Si en su lugar menciona apoyo, es por otro motivo.
        If adv IsNot Nothing Then
            Assert.IsFalse(adv.Contains("Centro: Mn⁻"),
                           "no debe advertir sobre cara centro sin refuerzo: " & adv)
        End If
    End Sub

    <TestMethod>
    Public Sub FrameNulo_NoRevienta()
        Assert.IsNull(svc.ValidarC21_5_2(Nothing, esDES:=True))
    End Sub

    <TestMethod>
    Public Sub FrameSinRevisionFlexion_NoRevienta()
        Dim f As New cFrame With {.Section = Seccion()}
        Assert.IsNull(svc.ValidarC21_5_2(f, esDES:=True))
    End Sub

End Class
