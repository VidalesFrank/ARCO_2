Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports ARCO
Imports ARCO.eNumeradores

' Redistribución de momentos — dos esquemas según frame.EsControladoPorSismo.
'
' Gravitacional (existente, no se altera):
'   M⁻izq y M⁻der  →  M⁺centro
'   M⁺centro       →  mitad a M⁻izq, mitad a M⁻der
'
' Sísmica (nueva, 2026-11-05):
'   M⁻izq  →  M⁺der   (factor zonaIzq.FactorRedistribucion)
'   M⁻der  →  M⁺izq   (factor zonaDer.FactorRedistribucion)
'   M⁺izq  →  M⁻der   (factor zonaIzq.FactorRedPosApoyo)
'   M⁺der  →  M⁻izq   (factor zonaDer.FactorRedPosApoyo)
'
' Las transferencias diagonales se aplican sobre los valores BASE para que las
' cuatro no se interfieran al sumarse en ResultadoActual.
<TestClass>
Public Class RedistribucionDiagonalTests

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

    Private Shared Function Z(f As cFrame, pos As PosicionTramoViga) As cRevisionFlexionZona
        Return f.RevisionFlexion.First(Function(x) x.Posicion = pos)
    End Function

    Private Shared ReadOnly svc As New VigaService(New GeometryService())

    ' ───────────────── Gravitacional: el comportamiento no cambia ─────────────────

    <TestMethod>
    Public Sub Gravitacional_FactorApoyoReduce_MNeg_Y_AumentaMPosCentro()
        Dim f = NuevoFrame(mPosIzq:=10, mNegIzq:=-100,
                           mPosCen:=80, mNegCen:=0,
                           mPosDer:=10, mNegDer:=-100)
        Z(f, PosicionTramoViga.Izquierda).FactorRedistribucion = 0.2
        svc.RecalcularRedistribucion(f)

        ' |M⁻izq| pasó de 100 a 80 (reducción 20).
        Assert.AreEqual(-80, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        ' Centro M⁺ pasó de 80 a 80 + 20 = 100.
        Assert.AreEqual(100, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
        ' Derecha intacta.
        Assert.AreEqual(-100, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoNegativo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Gravitacional_FactorCentroReduce_MPos_Y_AumentaMitadEnCadaApoyo()
        Dim f = NuevoFrame(mPosIzq:=10, mNegIzq:=-100,
                           mPosCen:=80, mNegCen:=0,
                           mPosDer:=10, mNegDer:=-100)
        Z(f, PosicionTramoViga.Centro).FactorRedistribucion = 0.25
        svc.RecalcularRedistribucion(f)

        ' Centro M⁺ pasó de 80 a 60 (reducción 20).
        Assert.AreEqual(60, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
        ' Cada apoyo recibió 10 de magnitud → |M⁻| pasó de 100 a 110.
        Assert.AreEqual(-110, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        Assert.AreEqual(-110, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoNegativo, 1.0E-6)
    End Sub

    ' ───────────────── Sísmica: 4 diagonales independientes ─────────────────

    <TestMethod>
    Public Sub Sismico_FactorMNegIzq_Mueve_A_MPosDer()
        Dim f = NuevoFrame(mPosIzq:=50, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=50, mNegDer:=-200)
        f.EsControladoPorSismo = True
        Z(f, PosicionTramoViga.Izquierda).FactorRedistribucion = 0.15   ' M⁻izq → M⁺der
        svc.RecalcularRedistribucion(f)

        ' |M⁻izq| pasó de 200 a 170.
        Assert.AreEqual(-170, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        ' M⁺der ganó 30 → 80.
        Assert.AreEqual(80, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoPositivo, 1.0E-6)
        ' Centro intacto.
        Assert.AreEqual(150, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
        Assert.AreEqual(0, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoNegativo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Sismico_FactorMNegDer_Mueve_A_MPosIzq()
        Dim f = NuevoFrame(mPosIzq:=40, mNegIzq:=-180,
                           mPosCen:=120, mNegCen:=0,
                           mPosDer:=40, mNegDer:=-180)
        f.EsControladoPorSismo = True
        Z(f, PosicionTramoViga.Derecha).FactorRedistribucion = 0.2   ' M⁻der → M⁺izq
        svc.RecalcularRedistribucion(f)

        Assert.AreEqual(-144, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoNegativo, 1.0E-6)
        Assert.AreEqual(40 + 36, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoPositivo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Sismico_FactorMPosIzq_Mueve_A_MNegDer()
        Dim f = NuevoFrame(mPosIzq:=100, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=100, mNegDer:=-200)
        f.EsControladoPorSismo = True
        Z(f, PosicionTramoViga.Izquierda).FactorRedPosApoyo = 0.1   ' M⁺izq → M⁻der
        svc.RecalcularRedistribucion(f)

        ' M⁺izq: de 100 a 90 (reducción 10).
        Assert.AreEqual(90, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoPositivo, 1.0E-6)
        ' |M⁻der|: de 200 a 210 → -210.
        Assert.AreEqual(-210, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoNegativo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Sismico_FactorMPosDer_Mueve_A_MNegIzq()
        Dim f = NuevoFrame(mPosIzq:=100, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=100, mNegDer:=-200)
        f.EsControladoPorSismo = True
        Z(f, PosicionTramoViga.Derecha).FactorRedPosApoyo = 0.2   ' M⁺der → M⁻izq
        svc.RecalcularRedistribucion(f)

        Assert.AreEqual(80, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoPositivo, 1.0E-6)
        Assert.AreEqual(-220, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Sismico_CuatroTransferenciasSimultaneas_NoSePisanEntreSi()
        ' Caso con los 4 factores activos — cada ResultadoActual debe reflejar
        ' la reducción propia Y el aporte del apoyo opuesto, calculados desde
        ' los valores BASE (no sobre un ResultadoActual parcialmente escrito).
        Dim f = NuevoFrame(mPosIzq:=100, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=80, mNegDer:=-180)
        f.EsControladoPorSismo = True
        Z(f, PosicionTramoViga.Izquierda).FactorRedistribucion = 0.1   ' M⁻izq → M⁺der (|·|·f = 20)
        Z(f, PosicionTramoViga.Izquierda).FactorRedPosApoyo = 0.2       ' M⁺izq → M⁻der (= 20)
        Z(f, PosicionTramoViga.Derecha).FactorRedistribucion = 0.1       ' M⁻der → M⁺izq (= 18)
        Z(f, PosicionTramoViga.Derecha).FactorRedPosApoyo = 0.1          ' M⁺der → M⁻izq (= 8)
        svc.RecalcularRedistribucion(f)

        ' Izquierda M⁻: base -200, reducción propia 10% → -180; aporte M⁺der = 8 → -188.
        Assert.AreEqual(-188, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        ' Izquierda M⁺: base 100, reducción propia 20% → 80; aporte M⁻der = 18 → 98.
        Assert.AreEqual(98, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoPositivo, 1.0E-6)
        ' Derecha M⁻: base -180, reducción propia 10% → -162; aporte M⁺izq = 20 → -182.
        Assert.AreEqual(-182, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoNegativo, 1.0E-6)
        ' Derecha M⁺: base 80, reducción propia 10% → 72; aporte M⁻izq = 20 → 92.
        Assert.AreEqual(92, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoPositivo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Sismico_CentroNoParticipa_QuedaEnBase()
        ' Aunque el usuario haya dejado un factor en la zona Centro, el esquema
        ' sísmico no lo lee. Centro debe salir idéntico al base.
        Dim f = NuevoFrame(mPosIzq:=50, mNegIzq:=-150,
                           mPosCen:=120, mNegCen:=0,
                           mPosDer:=50, mNegDer:=-150)
        f.EsControladoPorSismo = True
        Z(f, PosicionTramoViga.Centro).FactorRedistribucion = 0.3   ' se debe ignorar
        Z(f, PosicionTramoViga.Izquierda).FactorRedistribucion = 0.1
        svc.RecalcularRedistribucion(f)

        Assert.AreEqual(120, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
        Assert.AreEqual(0, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoNegativo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub Gravitacional_FactorRedPosApoyoNoActua()
        ' FactorRedPosApoyo solo lo lee el esquema sísmico. En gravitacional el
        ' valor queda guardado pero no debe alterar ResultadoActual.
        Dim f = NuevoFrame(mPosIzq:=10, mNegIzq:=-100,
                           mPosCen:=80, mNegCen:=0,
                           mPosDer:=10, mNegDer:=-100)
        f.EsControladoPorSismo = False
        Z(f, PosicionTramoViga.Izquierda).FactorRedPosApoyo = 0.5   ' se debe ignorar
        svc.RecalcularRedistribucion(f)

        ' Sin FactorRedistribucion en ninguna zona, Actual = Base.
        Assert.AreEqual(10, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoPositivo, 1.0E-6)
        Assert.AreEqual(-100, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        Assert.AreEqual(80, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
    End Sub

    <TestMethod>
    Public Sub CambioDeEsquema_ReaplicaConLogicaDelNuevoTipo()
        ' Mismo frame y mismos factores, al cambiar el flag el ResultadoActual
        ' debe recomputarse con la lógica del nuevo esquema.
        Dim f = NuevoFrame(mPosIzq:=50, mNegIzq:=-200,
                           mPosCen:=150, mNegCen:=0,
                           mPosDer:=50, mNegDer:=-200)
        Z(f, PosicionTramoViga.Izquierda).FactorRedistribucion = 0.1

        ' Primero gravitacional: M⁻izq → M⁺centro.
        f.EsControladoPorSismo = False
        svc.RecalcularRedistribucion(f)
        Assert.AreEqual(-180, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        Assert.AreEqual(170, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
        Assert.AreEqual(50, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoPositivo, 1.0E-6)

        ' Ahora sísmica: M⁻izq → M⁺der. Centro no debe tocarse, der sí.
        f.EsControladoPorSismo = True
        svc.RecalcularRedistribucion(f)
        Assert.AreEqual(-180, Z(f, PosicionTramoViga.Izquierda).ResultadoActual.MomentoNegativo, 1.0E-6)
        Assert.AreEqual(150, Z(f, PosicionTramoViga.Centro).ResultadoActual.MomentoPositivo, 1.0E-6)
        Assert.AreEqual(70, Z(f, PosicionTramoViga.Derecha).ResultadoActual.MomentoPositivo, 1.0E-6)
    End Sub

End Class
