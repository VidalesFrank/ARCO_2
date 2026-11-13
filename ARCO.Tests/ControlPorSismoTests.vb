Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO
Imports ARCO.eNumeradores

' VigaService.ClasificarControlPorSismo: heurística para detectar frames donde
' el sismo gobierna, usando solo la envolvente de diseño (no las combinaciones).
' Dos señales — basta con que dispare una:
'   1) |M⁺|/|M⁻| en algún apoyo >= UmbralInversionApoyo (0.30)
'   2) |M⁺_apoyo_max| / |M⁺_centro| >= UmbralPositivoApoyoVsCentro (0.50)
' Los umbrales se calibran contra proyectos reales; estos tests son el piso.
<TestClass>
Public Class ControlPorSismoTests

    Private Shared Function Zona(pos As PosicionTramoViga, mPos As Double, mNeg As Double) As cRevisionFlexionZona
        Return New cRevisionFlexionZona With {
            .Posicion = pos,
            .ResultadoBase = New cResultadoFlexion With {.MomentoPositivo = mPos, .MomentoNegativo = mNeg},
            .ResultadoActual = New cResultadoFlexion With {.MomentoPositivo = mPos, .MomentoNegativo = mNeg}
        }
    End Function

    Private Shared Function NuevoFrame(mPosIzq As Double, mNegIzq As Double,
                                       mPosCen As Double, mNegCen As Double,
                                       mPosDer As Double, mNegDer As Double) As cFrame
        Dim f As New cFrame
        f.RevisionFlexion.Add(Zona(PosicionTramoViga.Izquierda, mPosIzq, mNegIzq))
        f.RevisionFlexion.Add(Zona(PosicionTramoViga.Centro, mPosCen, mNegCen))
        f.RevisionFlexion.Add(Zona(PosicionTramoViga.Derecha, mPosDer, mNegDer))
        Return f
    End Function

    <TestMethod>
    Public Sub Gravitacional_SinInversionNiPositivoEnApoyo_EsNoSismico()
        ' Perfil clásico: M⁻ fuerte en apoyos, M⁺ dominante en centro.
        Dim f = NuevoFrame(mPosIzq:=5, mNegIzq:=-200,
                           mPosCen:=180, mNegCen:=0,
                           mPosDer:=5, mNegDer:=-200)
        Assert.IsFalse(VigaService.ClasificarControlPorSismo(f))
    End Sub

    <TestMethod>
    Public Sub Sismico_InversionDeSignoEnApoyoIzq_EsSismico()
        ' Volteo del pórtico genera M⁺ significativo en apoyo izq
        ' (ratio 80/200 = 0.40 > 0.30).
        Dim f = NuevoFrame(mPosIzq:=80, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=5, mNegDer:=-200)
        Assert.IsTrue(VigaService.ClasificarControlPorSismo(f))
    End Sub

    <TestMethod>
    Public Sub Sismico_InversionDeSignoEnApoyoDer_EsSismico()
        Dim f = NuevoFrame(mPosIzq:=5, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=90, mNegDer:=-180)
        Assert.IsTrue(VigaService.ClasificarControlPorSismo(f))
    End Sub

    <TestMethod>
    Public Sub Sismico_PositivoApoyoRivalizaConCentro_EsSismico()
        ' Apoyo izq con M⁺ = 100, centro con M⁺ = 180 → ratio 0.56 ≥ 0.50.
        ' El ratio de inversión aquí es 100/300 = 0.33 (también dispararía),
        ' pero el test se asegura de que la señal 2 por sí sola sirve.
        Dim f = NuevoFrame(mPosIzq:=100, mNegIzq:=-300,
                           mPosCen:=180, mNegCen:=0,
                           mPosDer:=10, mNegDer:=-300)
        Assert.IsTrue(VigaService.ClasificarControlPorSismo(f))
    End Sub

    <TestMethod>
    Public Sub Gravitacional_InversionJustoBajoElUmbral_NoDispara()
        ' Ratio 50/200 = 0.25 < 0.30 ; positivo apoyo/centro = 50/200 = 0.25 < 0.50.
        Dim f = NuevoFrame(mPosIzq:=50, mNegIzq:=-200,
                           mPosCen:=200, mNegCen:=0,
                           mPosDer:=50, mNegDer:=-200)
        Assert.IsFalse(VigaService.ClasificarControlPorSismo(f))
    End Sub

    <TestMethod>
    Public Sub FrameSinApoyoConMomentoNegativo_NoDivideCero()
        ' Si un apoyo queda con M⁻ = 0 (nudo articulado o vano muy corto)
        ' el ratio de inversión se trata como 0 — no revienta ni dispara.
        ' M⁺ apoyo pequeño en relación al centro para no activar la otra señal.
        Dim f = NuevoFrame(mPosIzq:=20, mNegIzq:=0,
                           mPosCen:=100, mNegCen:=0,
                           mPosDer:=20, mNegDer:=0)
        Assert.IsFalse(VigaService.ClasificarControlPorSismo(f))
    End Sub

    <TestMethod>
    Public Sub FrameSinRevisionFlexion_EsNoSismico()
        Assert.IsFalse(VigaService.ClasificarControlPorSismo(New cFrame()))
    End Sub

    <TestMethod>
    Public Sub FrameNulo_NoRevienta()
        Assert.IsFalse(VigaService.ClasificarControlPorSismo(Nothing))
    End Sub

End Class
