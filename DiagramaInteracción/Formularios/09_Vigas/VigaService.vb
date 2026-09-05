Imports System.Linq
Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias

Public Class VigaService

    Private ReadOnly _geo As GeometryService
    Public Sub New(geo As GeometryService)
        _geo = geo
    End Sub


    Function GenerarVigas(frames As List(Of cFrame),
                      joints As Dictionary(Of String, cJoint)) _
                      As List(Of cViga)

        Dim vigas As New List(Of cViga)
        Dim framesPendientes As New HashSet(Of cFrame)(frames)
        Dim tol As Double = 1.5
        Dim tol_col As Double = 0.01

        While framesPendientes.Any()

            Dim fBase = framesPendientes.First()
            framesPendientes.Remove(fBase)

            Dim viga As New cViga With {
            .Nombre = $"VIGA-{vigas.Count + 1}",
            .Piso = fBase.Story
        }

            viga.Name_Beam = viga.Nombre

            viga.Frames.Add(fBase)

            Dim dir = _geo.VectorFrame(fBase, joints)
            dir.Normalize()
            viga.Direccion = dir

            Dim agrego As Boolean

            Do
                agrego = False

                For Each f In framesPendientes.ToList()

                    'If f.Section.Nombre <> fBase.Section.Nombre Then Continue For
                    If Not String.Equals(f.Story.Trim(), fBase.Story.Trim(), StringComparison.OrdinalIgnoreCase) Then Continue For

                    'If viga.Frames.Any(Function(fv) CompartenJoint(fv, f)) AndAlso
                    '                                SonColineales(f, viga.Direccion, joints, tol) Then
                    If viga.Frames.Any(Function(fv) (_geo.CompartenJoint(fv, f) OrElse _geo.SonContinuos(fv, f, joints, tol)) AndAlso
                                    _geo.EstanEnLaMismaLinea(fv, f, joints, tol_col)) AndAlso
                                _geo.SonColineales(f, viga.Direccion, joints, tol_col) Then
                        viga.Frames.Add(f)
                        framesPendientes.Remove(f)
                        agrego = True
                    End If

                Next

            Loop While agrego

            vigas.Add(viga)

        End While

        Return vigas
    End Function


    Public Sub OrdenarFramesViga(viga As cViga,
                  joints As Dictionary(Of String, cJoint))

        Dim dir = viga.Direccion
        dir.Normalize()

        viga.Frames = viga.Frames _
        .OrderBy(Function(f)
                     Dim p = _geo.PuntoMedioFrame(f, joints)
                     Return Vector3.Dot(p, dir)
                 End Function) _
        .ToList()
    End Sub

    Public Sub CalcularEnvolventesVigas(Vigas As List(Of cViga),
                                        beamForces As List(Of cCombinacionBeamForce),
                                        combinaciones As HashSet(Of String))

        'Dim combinaciones = Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.ToHashSet()

        Dim lookup = beamForces.Where(Function(r) combinaciones.Contains(r.LoadCaseKey)) _
                                .GroupBy(Function(r) (r.Beam.Trim().ToUpperInvariant(),
                                                      r.Story.Trim().ToUpperInvariant())) _
                                .ToDictionary(Function(g) g.Key,
                                                Function(g) g.ToList())

        For Each viga In Vigas

            For Each frame In viga.Frames

                Dim key = (frame.ObjectLabel.Trim().ToUpperInvariant(),
                           frame.Story.Trim().ToUpperInvariant())

                Dim bfFrame As List(Of cCombinacionBeamForce) = Nothing

                If lookup.TryGetValue(key, bfFrame) = False Then
                    Continue For
                End If

                If bfFrame.Count = 0 Then Continue For

                Dim estaciones As List(Of Double)
                Dim envMax As List(Of Double)
                Dim envMin As List(Of Double)

                ConstruirEnvolventeAnalisis(bfFrame, estaciones, envMax, envMin)

                Dim MmaxAnalisis = New List(Of Double)(envMax)
                Dim MminAnalisis = New List(Of Double)(envMin)

                EnvolventeNSR10(estaciones, envMax, envMin)

                Dim MmaxDesign = New List(Of Double)(envMax)
                Dim MminDesign = New List(Of Double)(envMin)

                frame.EnvolventeMomento = New cEnvolventeMomento With {
                .Estaciones = estaciones,
                .MmaxAnalisis = MmaxAnalisis,
                .MminAnalisis = MminAnalisis,
                .MmaxDesign = MmaxDesign,
                .MminDesign = MminDesign
            }

                CalcularValoresResumen(frame.EnvolventeMomento)

                ' =====================================
                ' 🔹 Guardar momentos por zona
                ' =====================================

                frame.RevisionFlexion.Clear()

                Dim iIzq As Integer = 0
                Dim iDer As Integer = estaciones.Count - 1

                Dim L As Double = estaciones.Last()

                ' =====================================
                ' 🔥 CENTRO → VENTANA (NO punto)
                ' =====================================

                Dim factorIni As Double = 0.3
                Dim factorFin As Double = 0.7

                Dim xMin As Double = factorIni * L
                Dim xMax As Double = factorFin * L

                Dim MposMax As Double = Double.MinValue
                Dim MnegMinCentro As Double = Double.MaxValue

                For i As Integer = 0 To estaciones.Count - 1

                    Dim x = estaciones(i)

                    If x >= xMin AndAlso x <= xMax Then

                        If envMax(i) > MposMax Then
                            MposMax = envMax(i)
                        End If

                        If envMin(i) < MnegMinCentro Then
                            MnegMinCentro = envMin(i)
                        End If

                    End If

                Next

                ' =====================================
                ' IZQUIERDA
                ' =====================================
                Dim zonaIzq As New cRevisionFlexionZona With {.Posicion = PosicionTramoViga.Izquierda}

                ' 🔹 Asignar valores BASE (análisis)
                zonaIzq.ResultadoBase.MomentoPositivo = envMax(iIzq)
                zonaIzq.ResultadoBase.MomentoNegativo = envMin(iIzq)

                ' 🔹 Inicializar ACTUAL igual al BASE
                zonaIzq.ResultadoActual.MomentoPositivo = zonaIzq.ResultadoBase.MomentoPositivo
                zonaIzq.ResultadoActual.MomentoNegativo = zonaIzq.ResultadoBase.MomentoNegativo

                frame.RevisionFlexion.Add(zonaIzq)

                ' =====================================
                ' CENTRO (VENTANA)
                ' =====================================
                Dim zonaCen As New cRevisionFlexionZona With {.Posicion = PosicionTramoViga.Centro}

                ' 🔹 Asignar valores BASE (análisis)
                zonaCen.ResultadoBase.MomentoPositivo = MposMax
                zonaCen.ResultadoBase.MomentoNegativo = MnegMinCentro

                ' 🔹 Inicializar ACTUAL igual al BASE
                zonaCen.ResultadoActual.MomentoPositivo = zonaCen.ResultadoBase.MomentoPositivo
                zonaCen.ResultadoActual.MomentoNegativo = zonaCen.ResultadoBase.MomentoNegativo

                frame.RevisionFlexion.Add(zonaCen)

                ' =====================================
                ' DERECHA
                ' =====================================
                Dim zonaDer As New cRevisionFlexionZona With {.Posicion = PosicionTramoViga.Derecha}

                ' 🔹 Asignar valores BASE (análisis)
                zonaDer.ResultadoBase.MomentoPositivo = envMax(iDer)
                zonaDer.ResultadoBase.MomentoNegativo = envMin(iDer)

                ' 🔹 Inicializar ACTUAL igual al BASE
                zonaDer.ResultadoActual.MomentoPositivo = zonaDer.ResultadoBase.MomentoPositivo
                zonaDer.ResultadoActual.MomentoNegativo = zonaDer.ResultadoBase.MomentoNegativo

                frame.RevisionFlexion.Add(zonaDer)

            Next

        Next

    End Sub

    Public Sub CalcularValoresResumen(env As cEnvolventeMomento)

        If env Is Nothing OrElse env.Estaciones.Count = 0 Then Exit Sub

        Dim estaciones = env.Estaciones
        Dim Mmax = env.MmaxDesign ' o Analisis según lo que quieras usar
        Dim Mmin = env.MminDesign

        ' ==================================================
        ' 🔹 MOMENTO POSITIVO MÁXIMO
        ' ==================================================
        Dim maxVal As Double = Double.MinValue
        Dim idxMax As Integer = -1

        For i = 0 To Mmax.Count - 1
            If Mmax(i) > maxVal Then
                maxVal = Mmax(i)
                idxMax = i
            End If
        Next

        env.MomentoPositivoMax = If(maxVal > 0, maxVal, 0)
        env.EstacionMomentoPositivoMax = If(idxMax >= 0, estaciones(idxMax), 0)

        ' ==================================================
        ' 🔹 MOMENTOS NEGATIVOS EN EXTREMOS
        ' ==================================================
        ' Izquierda → primer punto
        Dim MnegIzq = Mmin.First()
        env.MomentoNegativoIzq = If(MnegIzq < 0, MnegIzq, 0)

        ' Derecha → último punto
        Dim MnegDer = Mmin.Last()
        env.MomentoNegativoDer = If(MnegDer < 0, MnegDer, 0)

    End Sub

    Public Sub ConstruirEnvolventeAnalisis(bfFrame As List(Of cCombinacionBeamForce),
                                           ByRef estaciones As List(Of Double),
                                           ByRef envMax As List(Of Double),
                                           ByRef envMin As List(Of Double))


        Dim dictMax As New Dictionary(Of Double, List(Of Double))
        Dim dictMin As New Dictionary(Of Double, List(Of Double))

        For Each bf In bfFrame

            Dim s As Double = bf.Station
            Dim m As Double = bf.M3
            Dim stepType As String = If(bf.stepType, "").Trim().ToUpper()

            Select Case stepType

                Case "MAX"
                    If Not dictMax.ContainsKey(s) Then
                        dictMax(s) = New List(Of Double)
                    End If
                    dictMax(s).Add(m)

                Case "MIN"
                    If Not dictMin.ContainsKey(s) Then
                        dictMin(s) = New List(Of Double)
                    End If

                    dictMin(s).Add(m)
                Case Else
                    ' Opcional: ignorar o usar como fallback
                    ' Aquí lo ignoramos
            End Select

        Next

        ' Unir estaciones de ambos diccionarios
        estaciones = dictMax.Keys.Union(dictMin.Keys).OrderBy(Function(x) x).ToList()

        envMax = New List(Of Double)
        envMin = New List(Of Double)

        For Each s In estaciones

            ' Si no existe, puedes poner 0 o Double.NaN
            envMax.Add(If(dictMax.ContainsKey(s), dictMax(s).Max(), 0))
            envMin.Add(If(dictMin.ContainsKey(s), dictMin(s).Min(), 0))

        Next

    End Sub

    Public Sub EnvolventeNSR10(estaciones As List(Of Double),
                               envMax As List(Of Double),
                               envMin As List(Of Double))

        Dim n As Integer = estaciones.Count - 1

        If n < 1 Then Exit Sub

        ' momentos en apoyos
        Dim M1n As Double = envMin(0)
        Dim M1p As Double = envMax(0)

        Dim M3n As Double = envMin(n)
        Dim M3p As Double = envMax(n)

        ' máximo positivo del tramo
        Dim M2p As Double = envMax.Max()

        ' ============================
        ' REGLA NSR-10 EN APOYOS
        ' ============================
        M1p = Math.Max(M1p, Math.Abs(M1n) / 3)
        M3p = Math.Max(M3p, Math.Abs(M3n) / 3)

        envMax(0) = M1p
        envMax(n) = M3p

        ' ============================
        ' MOMENTO DE REFERENCIA
        ' ============================
        Dim Mref As Double = Math.Max(
        Math.Abs(M1n),
        Math.Max(Math.Abs(M1p),
        Math.Max(Math.Abs(M3n),
        Math.Max(Math.Abs(M3p), Math.Abs(M2p)))))

        Dim Mlim As Double = Mref / 5

        ' ============================
        ' AJUSTE EN TODO EL TRAMO
        ' ============================
        For i As Integer = 1 To n - 1

            If envMax(i) < Mlim Then
                envMax(i) = Mlim
            End If

            If envMin(i) > -Mlim Then
                envMin(i) = -Mlim
            End If

        Next

    End Sub

    Public Sub designVigas(vigas As List(Of cViga),
                           joints As List(Of cJoint))

        Dim jointsDict As Dictionary(Of String, cJoint) =
        joints.ToDictionary(Function(j) j.ElementLabel)

        For Each viga In vigas
            For Each frame In viga.Frames

                frame.Longitud = _geo.Distancia_fj(frame, jointsDict)

                DesignFrame(frame)

            Next
        Next

    End Sub

    Public Sub DesignFrame(frame As cFrame)

        Dim posiciones = {
        PosicionTramoViga.Izquierda,
        PosicionTramoViga.Centro,
        PosicionTramoViga.Derecha
    }

        For Each pos In posiciones

            Dim zona = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

            If zona Is Nothing Then
                zona = New cRevisionFlexionZona With {.Posicion = pos}
                frame.RevisionFlexion.Add(zona)
            End If

            DesignZona(frame, zona, TipoResultadoFlexionViga.Base)
            DesignZona(frame, zona, TipoResultadoFlexionViga.Actual)

        Next

    End Sub

    Public Sub DesignZona(frame As cFrame, zona As cRevisionFlexionZona, tipo As TipoResultadoFlexionViga)

        Dim sec = frame.Section

        Dim b As Double = sec.b
        Dim d As Double = sec.d
        Dim fc As Double = sec.fc
        Dim fy As Double = sec.fy
        Dim phi As Double = 0.9

        Dim rho_min As Double = Math.Max(1.4 / fy, 0.25 * Math.Sqrt(fc) / fy)
        Dim rho_temp As Double = 0.0018

        Dim factorComun As Double = 0.85 * fc / fy
        Dim denominador As Double = 0.85 * phi * b * (d * d) * (fc * 1000)

        Dim res = zona.ResultadoBase

        If tipo = TipoResultadoFlexionViga.Actual Then
            res = zona.ResultadoActual
        End If

        ' 🔹 Momentos (SIEMPRE del ACTUAL)
        Dim Mu_neg As Double = Math.Abs(res.MomentoNegativo)
        Dim Mu_pos As Double = Math.Abs(res.MomentoPositivo)

        ' 🔹 Rho
        Dim rho_neg = CalcularRho(Mu_neg, denominador, factorComun, rho_min, rho_temp)
        Dim rho_pos = CalcularRho(Mu_pos, denominador, factorComun, rho_min, rho_temp)

        ' 🔹 Acero requerido
        res.AsReqSup = rho_neg * b * d * 1000000.0
        res.AsReqInf = rho_pos * b * d * 1000000.0

    End Sub

    Private Function CalcularRho(Mu As Double, denominador As Double, factorComun As Double, rho_min As Double, rho_temp As Double) As Double

        Dim term = Math.Max(0, 1 - (2 * Mu / denominador))
        Dim rho = factorComun * (1 - Math.Sqrt(term))

        Dim rhoAmplificado = 1.33 * rho

        Dim rho_design = Math.Max(rho, rho_min)

        If rhoAmplificado <= rho_temp Then
            Return rho_temp
        ElseIf rhoAmplificado < rho_design Then
            Return rhoAmplificado
        Else
            Return rho_design
        End If

    End Function


    Private Sub AplicarEnvolventeNSR(
    ByRef M1n As Double,
    ByRef M1p As Double,
    ByRef M2n As Double,
    ByRef M2p As Double,
    ByRef M3n As Double,
    ByRef M3p As Double)

        ' ===============================
        ' REGLA 1 – MOMENTO POSITIVO APOYO
        ' ===============================
        M1p = Math.Max(M1p, Math.Abs(M1n) / 3)
        M3p = Math.Max(M3p, Math.Abs(M3n) / 3)

        ' ===============================
        ' REGLA 2 – MOMENTO MINIMO TRAMO
        ' ===============================
        Dim Mmax As Double =
        Math.Max(Math.Abs(M1n),
        Math.Max(Math.Abs(M1p),
        Math.Max(Math.Abs(M3n), Math.Abs(M3p))))

        Dim Mmin As Double = Mmax / 5

        If Math.Abs(M2n) < Mmin Then
            M2n = -Mmin
        End If

        If Math.Abs(M2p) < Mmin Then
            M2p = Mmin
        End If

    End Sub


    Public Function AplicarRedistribucion(frame As cFrame,
                                          posicion As PosicionTramoViga,
                                          factor As Double,
                                          esNegativo As Boolean) As String

        'Dim viga As cViga = CType(Form_09_Vigas.Lista_Vigas.SelectedItem, cViga)

        Dim zona = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = posicion)
        If zona Is Nothing Then Return "No se encontró la zona correspondiente."

        ' Guardar factor
        zona.FactorRedistribucion = factor

        ' 🔹 Buscar zonas claves
        Dim zonaIzq = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Izquierda)
        Dim zonaCentro = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Centro)
        Dim zonaDer = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Derecha)

        Dim toleranciaVoladizo As Double = 5.0

        ' =========================================
        ' 🔥 VALIDACIÓN DE VOLADIZO
        ' =========================================

        'If posicion = PosicionTramoViga.Derecha Then

        '    If zonaIzq IsNot Nothing Then
        '        Dim M_izq = Math.Abs(zonaIzq.ResultadoBase.MomentoNegativo)

        '        If M_izq < toleranciaVoladizo Then
        '            MessageBox.Show("No se puede aplicar redistribución en el apoyo derecho porque el apoyo izquierdo se comporta como voladizo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        '            Exit Sub
        '        End If
        '    End If

        'ElseIf posicion = PosicionTramoViga.Izquierda Then

        '    If zonaDer IsNot Nothing Then
        '        Dim M_der = Math.Abs(zonaDer.ResultadoBase.MomentoNegativo)

        '        If M_der < toleranciaVoladizo Then
        '            MessageBox.Show("No se puede aplicar redistribución en el apoyo izquierdo porque el apoyo derecho se comporta como voladizo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        '            Exit Sub
        '        End If
        '    End If

        'End If
        ' 🔹 VALIDACIÓN
        If posicion = PosicionTramoViga.Derecha AndAlso zonaIzq IsNot Nothing Then

            If Math.Abs(zonaIzq.ResultadoBase.MomentoNegativo) < toleranciaVoladizo Then
                Return "No se puede redistribuir en apoyo derecho (voladizo en izquierda)"
            End If

        ElseIf posicion = PosicionTramoViga.Izquierda AndAlso zonaDer IsNot Nothing Then

            If Math.Abs(zonaDer.ResultadoBase.MomentoNegativo) < toleranciaVoladizo Then
                Return "No se puede redistribuir en apoyo izquierdo (voladizo en derecha)"
            End If

        End If

        ' 🔥 Recalcular TODO correctamente
        RecalcularRedistribucion(frame)

        ' =========================================
        ' 🔥 REDISEÑAR ACERO
        ' =========================================

        DesignZona(frame, zonaIzq, TipoResultadoFlexionViga.Actual)
        DesignZona(frame, zonaCentro, TipoResultadoFlexionViga.Actual)
        DesignZona(frame, zonaDer, TipoResultadoFlexionViga.Actual)

        Return Nothing ' todo OK

        'CalcularFlexionViga(viga)

        'MostrarResultadosFlexion(viga)

    End Function

    Public Sub RecalcularRedistribucion(frame As cFrame)

        Dim zonaIzq = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Izquierda)
        Dim zonaCentro = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Centro)
        Dim zonaDer = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Derecha)

        ' 🔹 1. RESET GLOBAL (CLAVE)
        For Each z In frame.RevisionFlexion
            z.ResultadoActual.MomentoNegativo = z.ResultadoBase.MomentoNegativo
            z.ResultadoActual.MomentoPositivo = z.ResultadoBase.MomentoPositivo
        Next

        ' =========================================
        ' 🔥 NEGATIVOS → CENTRO
        ' =========================================

        Dim aporteCentro As Double = 0.0

        If zonaIzq IsNot Nothing Then
            Dim f = zonaIzq.FactorRedistribucion
            Dim M = zonaIzq.ResultadoBase.MomentoNegativo
            Dim delta = M * f

            zonaIzq.ResultadoActual.MomentoNegativo = M * (1 - f)

            aporteCentro += Math.Abs(delta)

        End If

        If zonaDer IsNot Nothing Then
            Dim f = zonaDer.FactorRedistribucion
            Dim M = zonaDer.ResultadoBase.MomentoNegativo
            Dim delta = M * f

            zonaDer.ResultadoActual.MomentoNegativo = M * (1 - f)

            aporteCentro += Math.Abs(delta)

        End If

        ' =========================================
        ' 🔥 POSITIVO → APOYOS
        ' =========================================

        If zonaCentro IsNot Nothing Then
            Dim f = zonaCentro.FactorRedistribucion
            Dim M = zonaCentro.ResultadoBase.MomentoPositivo
            Dim delta = M * f

            Dim M_Reducido = M * (1 - f)

            zonaCentro.ResultadoActual.MomentoPositivo = M_Reducido + aporteCentro

            Dim mitad = delta / 2.0

            If zonaIzq IsNot Nothing Then
                zonaIzq.ResultadoActual.MomentoNegativo -= mitad
            End If

            If zonaDer IsNot Nothing Then
                zonaDer.ResultadoActual.MomentoNegativo -= mitad
            End If
        End If

    End Sub

    Public Sub CalcularFlexionViga(viga As cViga)

        For Each frame In viga.Frames

            Dim posiciones = {
                PosicionTramoViga.Izquierda,
                PosicionTramoViga.Centro,
                PosicionTramoViga.Derecha
            }

            For Each pos In posiciones

                Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

                If revision Is Nothing Then Continue For

                Dim base = revision.ResultadoBase
                Dim act = revision.ResultadoActual

                ' =========================================
                ' 🔹 BASE (SIEMPRE)
                ' =========================================
                If base.AsReqSup > 0 Then
                    base.RatioSup = Math.Min(base.AsProvSup / base.AsReqSup, 9.99)
                    base.CumpleSuperior = base.AsProvSup >= 0.9 * base.AsReqSup
                End If

                If base.AsReqInf > 0 Then
                    base.RatioInf = Math.Min(base.AsProvInf / base.AsReqInf, 9.99)
                    base.CumpleInferior = base.AsProvInf >= 0.9 * base.AsReqInf
                End If

                ' =========================================
                ' 🔥 ACTUAL (SOLO SI HAY REDISTRIBUCIÓN)
                ' =========================================

                'If revision.FactorRedistribucion > 0 Then

                If act.AsReqSup > 0 Then
                    act.RatioSup = Math.Min(act.AsProvSup / act.AsReqSup, 9.99)
                    act.CumpleSuperior = act.AsProvSup >= 0.9 * act.AsReqSup
                End If

                If act.AsReqInf > 0 Then
                    act.RatioInf = Math.Min(act.AsProvInf / act.AsReqInf, 9.99)
                    act.CumpleInferior = act.AsProvInf >= 0.9 * act.AsReqInf
                End If

                'Else
                '    ' 🔹 Si no hay redistribución → actual = base
                '    act.RatioSup = base.RatioSup
                '    act.RatioInf = base.RatioInf
                '    act.CumpleSuperior = base.CumpleSuperior
                '    act.CumpleInferior = base.CumpleInferior
                'End If

            Next

        Next

    End Sub

    Public Sub GuardarRefuerzo(
    viga As cViga,
    datos As List(Of (FrameLabel As String, Posicion As PosicionTramoViga, Barras As Dictionary(Of String, Integer))),
    tipo As eTipoRefuerzo
)

        For Each item In datos

            Dim frame = viga.Frames.Find(Function(f) f.ObjectLabel = item.FrameLabel)
            If frame Is Nothing Then Continue For

            Dim tramo As New cRefuerzoTramo With {
                .Posicion = item.Posicion,
                .Frame = frame.ObjectLabel
            }

            Dim AsTotal As Double = 0

            For Each kvp In item.Barras

                tramo.Barras(kvp.Key) = kvp.Value
                AsTotal += kvp.Value * AreaRefuerzo(kvp.Key)

            Next

            ' 🔹 Guardar refuerzo
            If tipo = eTipoRefuerzo.Superior Then
                frame.RefuerzoSuperior.Add(tramo)
            Else
                frame.RefuerzoInferior.Add(tramo)
            End If

            ' 🔹 Actualizar revisión
            Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = item.Posicion)

            If revision Is Nothing Then
                revision = New cRevisionFlexionZona With {.Posicion = item.Posicion}
                frame.RevisionFlexion.Add(revision)
            End If

            If tipo = eTipoRefuerzo.Superior Then
                revision.ResultadoBase.AsProvSup = AsTotal
                revision.ResultadoActual.AsProvSup = AsTotal
            Else
                revision.ResultadoBase.AsProvInf = AsTotal
                revision.ResultadoActual.AsProvInf = AsTotal
            End If

        Next

    End Sub


    ' =========================================================
    ' CORTANTE
    ' =========================================================

    Public Sub CalcularEnvolventeCortante(vigas As List(Of cViga),
                                          beamForces As List(Of cCombinacionBeamForce),
                                          combinaciones As HashSet(Of String))

        Dim lookup = beamForces.Where(Function(r) combinaciones.Contains(r.LoadCaseKey)) _
                               .GroupBy(Function(r) (r.Beam.Trim().ToUpperInvariant(),
                                                     r.Story.Trim().ToUpperInvariant())) _
                               .ToDictionary(Function(g) g.Key, Function(g) g.ToList())

        For Each viga In vigas
            For Each frame In viga.Frames

                Dim key = (frame.ObjectLabel.Trim().ToUpperInvariant(),
                           frame.Story.Trim().ToUpperInvariant())
                Dim bfFrame As List(Of cCombinacionBeamForce) = Nothing
                If Not lookup.TryGetValue(key, bfFrame) Then Continue For
                If bfFrame.Count = 0 Then Continue For

                Dim d As Double = frame.Section.d

                ' Support faces defined by first/last ETABS-reported station
                Dim sFirst As Double = bfFrame.Min(Function(bf) bf.Station)
                Dim sLast As Double = bfFrame.Max(Function(bf) bf.Station)

                ' Zone lengths measured from support faces (N×s or 2H)
                Dim refIzq = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Izquierda)
                Dim refDer = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Derecha)

                Dim zoneIzqLen As Double = If(refIzq IsNot Nothing AndAlso refIzq.NumEstribos > 0,
                                               CDbl(refIzq.NumEstribos) * refIzq.Separacion,
                                               2.0 * frame.Section.h)

                Dim zoneDerLen As Double = If(refDer IsNot Nothing AndAlso refDer.NumEstribos > 0,
                                               CDbl(refDer.NumEstribos) * refDer.Separacion,
                                               2.0 * frame.Section.h)

                frame.RevisionCortante.Clear()

                For Each pos In {PosicionTramoViga.Izquierda, PosicionTramoViga.Centro, PosicionTramoViga.Derecha}

                    Dim zona As New cRevisionCortanteZona With {.Posicion = pos}

                    Select Case pos

                        Case PosicionTramoViga.Izquierda
                            ' d measured from support face (sFirst)
                            zona.Vu = InterpolarCortanteEnEstacion(bfFrame, sFirst + d)

                        Case PosicionTramoViga.Derecha
                            ' d measured inward from right support face (sLast)
                            zona.Vu = InterpolarCortanteEnEstacion(bfFrame, sLast - d)

                        Case Else
                            Dim limIzq As Double = sFirst + zoneIzqLen
                            Dim limDer As Double = sLast - zoneDerLen

                            If limIzq < limDer Then
                                Dim candidatos = bfFrame.Where(Function(bf) bf.Station > limIzq AndAlso bf.Station < limDer)
                                If candidatos.Any() Then
                                    zona.Vu = candidatos.Max(Function(bf) Math.Abs(bf.V2))
                                End If
                            Else
                                zona.Vu = InterpolarCortanteEnEstacion(bfFrame, (sFirst + sLast) / 2.0)
                            End If

                    End Select

                    frame.RevisionCortante.Add(zona)
                Next

            Next
        Next

    End Sub

    Private Function InterpolarCortanteEnEstacion(bfFrame As List(Of cCombinacionBeamForce), targetStation As Double) As Double

        If bfFrame Is Nothing OrElse bfFrame.Count = 0 Then Return 0

        ' Max |V2| per station across all combinations and step types
        Dim perStation = bfFrame _
            .GroupBy(Function(bf) Math.Round(bf.Station, 6)) _
            .ToDictionary(Function(g) g.Key,
                          Function(g) g.Max(Function(bf) Math.Abs(bf.V2)))

        Dim stations = perStation.Keys.OrderBy(Function(s) s).ToList()

        If stations.Count = 0 Then Return 0
        If targetStation <= stations.First() Then Return perStation(stations.First())
        If targetStation >= stations.Last() Then Return perStation(stations.Last())

        Dim prevIdx As Integer = -1
        For i As Integer = 0 To stations.Count - 2
            If stations(i) <= targetStation AndAlso stations(i + 1) > targetStation Then
                prevIdx = i
                Exit For
            End If
        Next

        If prevIdx < 0 Then Return perStation(stations.Last())

        Dim s1 As Double = stations(prevIdx)
        Dim s2 As Double = stations(prevIdx + 1)
        Dim v1 As Double = perStation(s1)
        Dim v2 As Double = perStation(s2)

        If Math.Abs(s2 - s1) < 0.0001 Then Return Math.Max(v1, v2)

        Dim t As Double = (targetStation - s1) / (s2 - s1)
        Return v1 + t * (v2 - v1)

    End Function

    Public Sub GuardarRefuerzoTransversal(viga As cViga,
                                          datos As List(Of (FrameLabel As String, Posicion As PosicionTramoViga, NumEstribos As Integer, NumBarra As Integer, CantEstribos As Integer, Separacion As Double)))

        For Each item In datos

            Dim frame = viga.Frames.Find(Function(f) f.ObjectLabel = item.FrameLabel)
            If frame Is Nothing Then Continue For

            Dim zona = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = item.Posicion)

            If zona Is Nothing Then
                zona = New cRefuerzoTransversalZona With {.Posicion = item.Posicion}
                frame.RefuerzoTransversal.Add(zona)
            End If

            zona.NumEstribos = Math.Max(item.NumEstribos, 1)
            zona.NumeroBarra = Math.Max(item.NumBarra, 2)
            zona.CantEstribos = Math.Max(item.CantEstribos, 1)
            zona.Separacion = Math.Max(item.Separacion, 0.001)

        Next

    End Sub

    Public Sub CalcularCapacidadCortante(vigas As List(Of cViga))

        Const phi As Double = 0.75

        For Each viga In vigas
            For Each frame In viga.Frames

                If frame.RevisionCortante.Count = 0 Then Continue For
                If frame.RefuerzoTransversal.Count = 0 Then Continue For

                Dim sec = frame.Section
                Dim b As Double = sec.b
                Dim d As Double = sec.d
                Dim fc As Double = sec.fc
                Dim fy As Double = sec.fy

                ' Vc = 0.17 * sqrt(fc[MPa]) * b[m] * d[m] * 1000  → kN
                Dim Vc As Double = 0.17 * Math.Sqrt(fc) * b * d * 1000.0

                For Each zonaRef In frame.RefuerzoTransversal

                    Dim zonaCor = frame.RevisionCortante.FirstOrDefault(Function(z) z.Posicion = zonaRef.Posicion)
                    If zonaCor Is Nothing Then Continue For

                    Dim barraLabel As String = "#" & zonaRef.NumeroBarra
                    Dim Av As Double = zonaRef.CantEstribos * AreaRefuerzo(barraLabel)  ' mm²

                    ' Vs = Av[mm²] * fy[MPa] * d[m] / s[m] / 1000  → kN
                    Dim Vs As Double = Av * fy * d / zonaRef.Separacion / 1000.0

                    zonaCor.Vc = Vc
                    zonaCor.Vs = Vs
                    zonaCor.phiVn = phi * (Vc + Vs)

                    If zonaCor.Vu > 0 Then
                        zonaCor.Factor = Math.Min(zonaCor.phiVn / zonaCor.Vu, 9.99)
                        zonaCor.Cumple = zonaCor.Factor >= 0.9
                    Else
                        zonaCor.Factor = 9.99
                        zonaCor.Cumple = True
                    End If

                Next

            Next
        Next

    End Sub

    ' =========================================================
    ' CORTANTE PLÁSTICO — Diseño por capacidad (NSR-10 C.21.5.4)
    ' =========================================================
    ' Aplica a todas las vigas que tienen refuerzo a flexión asignado
    ' (necesario para calcular Mn nominal). El usuario puede verificar
    ' cualquier viga, independientemente del resultado del chequeo estándar.
    '
    ' fy_factor: 1.0 para DMO, 1.25 para DES
    '
    ' Convenio de signos para Vg (de ETABS, con signo):
    '   Zona Izq (a d de la cara izq): Vg_izq ≈ +|V| (positivo, sentido de reacción)
    '   Zona Der (a d de la cara der): Vg_der ≈ -|V| (negativo)
    '
    ' Casos de volteo sísmico:
    '   Caso A (volteo →): Ve_A = (Mn⁻_izq + Mn⁺_der) / Ln
    '   Caso B (volteo ←): Ve_B = (Mn⁺_izq + Mn⁻_der) / Ln
    '
    ' Vu de diseño por zona:
    '   Zona Izq: Vu_A = +Ve_A + Vg_izq  |  Vu_B = -Ve_B + Vg_izq
    '   Zona Der: Vu_A = +Ve_A + Vg_der  |  Vu_B = -Ve_B + Vg_der
    '   Vu_diseno = max(|Vu_A|, |Vu_B|)

    Public Sub CalcularCortantePlastico(vigas As List(Of cViga),
                                        beamForces As List(Of cCombinacionBeamForce),
                                        combinaciones As HashSet(Of String),
                                        fy_factor As Double)

        Const phi As Double = 0.75

        Dim lookup = beamForces _
            .Where(Function(r) combinaciones.Contains(r.LoadCaseKey)) _
            .GroupBy(Function(r) (r.Beam.Trim().ToUpperInvariant(),
                                  r.Story.Trim().ToUpperInvariant())) _
            .ToDictionary(Function(g) g.Key, Function(g) g.ToList())

        For Each viga In vigas

            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())
            If Not tieneRef Then Continue For

            For Each frame In viga.Frames

                Dim sec = frame.Section
                Dim b_mm As Double = sec.b * 1000.0
                Dim d_mm As Double = sec.d * 1000.0
                Dim fc As Double = sec.fc
                Dim fy As Double = sec.fy
                Dim fy_dis As Double = fy * fy_factor

                ' Acero provisto en apoyos (mm²)
                Dim revIzq = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = PosicionTramoViga.Izquierda)
                Dim revDer = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = PosicionTramoViga.Derecha)
                If revIzq Is Nothing OrElse revDer Is Nothing Then Continue For

                Dim As_sup_izq = revIzq.ResultadoActual.AsProvSup
                Dim As_inf_izq = revIzq.ResultadoActual.AsProvInf
                Dim As_sup_der = revDer.ResultadoActual.AsProvSup
                Dim As_inf_der = revDer.ResultadoActual.AsProvInf

                ' Momentos nominales (kN·m), φ = 1.0
                Dim Mn_neg_izq = CalcularMnNominal(As_sup_izq, fy_dis, fc, b_mm, d_mm)
                Dim Mn_pos_izq = CalcularMnNominal(As_inf_izq, fy_dis, fc, b_mm, d_mm)
                Dim Mn_neg_der = CalcularMnNominal(As_sup_der, fy_dis, fc, b_mm, d_mm)
                Dim Mn_pos_der = CalcularMnNominal(As_inf_der, fy_dis, fc, b_mm, d_mm)

                ' Luz libre Ln (m): primera y última estación ETABS = caras de columnas
                Dim key = (frame.ObjectLabel.Trim().ToUpperInvariant(),
                           frame.Story.Trim().ToUpperInvariant())
                Dim bfFrame As List(Of cCombinacionBeamForce) = Nothing
                If Not lookup.TryGetValue(key, bfFrame) Then bfFrame = New List(Of cCombinacionBeamForce)()

                Dim sFirst = If(bfFrame.Count > 0, bfFrame.Min(Function(bf) bf.Station), 0.0)
                Dim sLast = If(bfFrame.Count > 0, bfFrame.Max(Function(bf) bf.Station), frame.Longitud)
                Dim Ln = If(sLast > sFirst, sLast - sFirst, frame.Longitud)

                ' Cortante plástico por caso de volteo (kN)
                Dim Ve_A = (Mn_neg_izq + Mn_pos_der) / Ln
                Dim Ve_B = (Mn_pos_izq + Mn_neg_der) / Ln

                ' Cortante gravitacional con signo en secciones críticas (kN)
                Dim d = sec.d
                Dim Vg_izq = InterpolarCortanteSignado(bfFrame, sFirst + d)
                Dim Vg_der = InterpolarCortanteSignado(bfFrame, sLast - d)

                ' Cortante de diseño por zona
                ' Izquierda: volteo → agrava, volteo ← puede aliviar o agravar según magnitudes
                Dim Vu_A_izq = Ve_A + Vg_izq
                Dim Vu_B_izq = -Ve_B + Vg_izq
                Dim Vu_dis_izq = Math.Max(Math.Abs(Vu_A_izq), Math.Abs(Vu_B_izq))

                ' Derecha: volteo ← agrava (Vg_der < 0 suma a -Ve_B)
                Dim Vu_A_der = Ve_A + Vg_der
                Dim Vu_B_der = -Ve_B + Vg_der
                Dim Vu_dis_der = Math.Max(Math.Abs(Vu_A_der), Math.Abs(Vu_B_der))

                ' Capacidad Vc (igual que chequeo estándar)
                Dim Vc = 0.17 * Math.Sqrt(fc) * sec.b * sec.d * 1000.0   ' kN

                Dim res As New cResultadoCortantePlasticoFrame With {
                    .Ln = Ln,
                    .fy_factor = fy_factor,
                    .Mn_neg_izq = Mn_neg_izq,
                    .Mn_pos_izq = Mn_pos_izq,
                    .Mn_neg_der = Mn_neg_der,
                    .Mn_pos_der = Mn_pos_der,
                    .Ve_A = Ve_A,
                    .Ve_B = Ve_B
                }

                ' Zona Izquierda
                With res.ZonaIzq
                    .Posicion = PosicionTramoViga.Izquierda
                    .Vg = Vg_izq
                    .Vu_A = Vu_A_izq
                    .Vu_B = Vu_B_izq
                    .Vu_diseno = Vu_dis_izq
                    .Vc = Vc
                    Dim refIzq = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Izquierda)
                    If refIzq IsNot Nothing Then
                        Dim Av = refIzq.CantEstribos * AreaRefuerzo("#" & refIzq.NumeroBarra)
                        .Vs = Av * fy * sec.d / refIzq.Separacion / 1000.0
                        .phiVn = phi * (.Vc + .Vs)
                        .Factor = If(Vu_dis_izq > 0, Math.Min(.phiVn / Vu_dis_izq, 9.99), 9.99)
                        .Cumple = .Factor >= 0.9
                    End If
                End With

                ' Zona Derecha
                With res.ZonaDer
                    .Posicion = PosicionTramoViga.Derecha
                    .Vg = Vg_der
                    .Vu_A = Vu_A_der
                    .Vu_B = Vu_B_der
                    .Vu_diseno = Vu_dis_der
                    .Vc = Vc
                    Dim refDer = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = PosicionTramoViga.Derecha)
                    If refDer IsNot Nothing Then
                        Dim Av = refDer.CantEstribos * AreaRefuerzo("#" & refDer.NumeroBarra)
                        .Vs = Av * fy * sec.d / refDer.Separacion / 1000.0
                        .phiVn = phi * (.Vc + .Vs)
                        .Factor = If(Vu_dis_der > 0, Math.Min(.phiVn / Vu_dis_der, 9.99), 9.99)
                        .Cumple = .Factor >= 0.9
                    End If
                End With

                frame.CortantePlastico = res

            Next
        Next

    End Sub

    ' Mn = As·fy·(d − a/2) / 10⁶  [kN·m], φ = 1.0, bloque rectangular ACI
    Private Function CalcularMnNominal(As_mm2 As Double, fy_dis As Double,
                                       fc As Double, b_mm As Double, d_mm As Double) As Double
        If As_mm2 <= 0 OrElse d_mm <= 0 Then Return 0.0
        Dim a = As_mm2 * fy_dis / (0.85 * fc * b_mm)      ' mm
        Return Math.Max(As_mm2 * fy_dis * (d_mm - a / 2.0) / 1.0E6, 0.0)
    End Function

    ' Interpolación del cortante V2 conservando el signo del valor de mayor |V2|
    Private Function InterpolarCortanteSignado(bfFrame As List(Of cCombinacionBeamForce),
                                               targetStation As Double) As Double
        If bfFrame Is Nothing OrElse bfFrame.Count = 0 Then Return 0.0

        Dim perStation = bfFrame _
            .GroupBy(Function(bf) Math.Round(bf.Station, 6)) _
            .ToDictionary(Function(g) g.Key,
                          Function(g) g.OrderByDescending(Function(bf) Math.Abs(bf.V2)).First().V2)

        Dim stations = perStation.Keys.OrderBy(Function(s) s).ToList()
        If stations.Count = 0 Then Return 0.0
        If targetStation <= stations.First() Then Return perStation(stations.First())
        If targetStation >= stations.Last() Then Return perStation(stations.Last())

        Dim prevIdx As Integer = -1
        For i As Integer = 0 To stations.Count - 2
            If stations(i) <= targetStation AndAlso stations(i + 1) > targetStation Then
                prevIdx = i : Exit For
            End If
        Next
        If prevIdx < 0 Then Return perStation(stations.Last())

        Dim s1 = stations(prevIdx) : Dim s2 = stations(prevIdx + 1)
        Dim v1 = perStation(s1) : Dim v2 = perStation(s2)
        If Math.Abs(s2 - s1) < 0.0001 Then Return If(Math.Abs(v1) >= Math.Abs(v2), v1, v2)
        Return v1 + (v2 - v1) * (targetStation - s1) / (s2 - s1)
    End Function

    ' =========================================================
    ' REFUERZO TRANSVERSAL AUTOMÁTICO (criterio normativo)
    ' =========================================================
    ' Barras #3 en todas las zonas.
    ' Zonas confinadas (Izq / Der): 2H desde el apoyo, sep = d/4.
    '   Ramas: 2 si b ≤ 0.45 m, 3 si b > 0.45 m.
    ' Zona central (Centro): sep = d/2, siempre 2 ramas.
    Public Sub AsignarRefuerzoTransversalAutomatico(vigas As List(Of cViga))

        For Each viga In vigas
            For Each frame In viga.Frames

                Dim sec = frame.Section
                Dim b As Double = sec.b
                Dim d As Double = sec.d
                Dim h As Double = sec.h

                Dim cantConfinada As Integer = If(b <= 0.45, 2, 3)
                Dim sepConfinada As Double = Math.Round(d / 4.0, 3)
                Dim numEstribosConfinado As Integer = Math.Max(CInt(Math.Ceiling(2.0 * h / sepConfinada)), 1)
                Dim sepCentro As Double = Math.Round(d / 2.0, 3)

                frame.RefuerzoTransversal.Clear()

                For Each pos In {PosicionTramoViga.Izquierda, PosicionTramoViga.Centro, PosicionTramoViga.Derecha}

                    Dim zona As New cRefuerzoTransversalZona With {.Posicion = pos}

                    If pos = PosicionTramoViga.Centro Then
                        zona.NumeroBarra = 3
                        zona.CantEstribos = 2
                        zona.Separacion = sepCentro
                        zona.NumEstribos = 10
                    Else
                        zona.NumeroBarra = 3
                        zona.CantEstribos = cantConfinada
                        zona.Separacion = sepConfinada
                        zona.NumEstribos = numEstribosConfinado
                    End If

                    frame.RefuerzoTransversal.Add(zona)

                Next

            Next
        Next

    End Sub

    Public Function SonVigasCompatibles(v1 As cViga, v2 As cViga) As Boolean

        If v1.Frames.Count <> v2.Frames.Count Then Return False

        For i = 0 To v1.Frames.Count - 1

            Dim f1 = v1.Frames(i)
            Dim f2 = v2.Frames(i)

            If Math.Abs(f1.Longitud - f2.Longitud) > 0.01 Then Return False

        Next

        Return True

    End Function

    ''' <summary>
    ''' Aplica agrupaciones manuales definidas por el usuario sobre las vigas ya auto-generadas.
    ''' Para cada grupo manual, extrae los frames de sus vigas actuales y los une en una nueva viga.
    ''' Las vigas que quedan vacías se eliminan y todas se renumeran al final.
    ''' </summary>
    Public Sub AplicarGruposManual(vigas As List(Of cViga),
                                    gruposManual As List(Of List(Of String)),
                                    jointsDict As Dictionary(Of String, cJoint))

        If gruposManual Is Nothing OrElse gruposManual.Count = 0 Then Return

        For Each grupo In gruposManual

            If grupo Is Nothing OrElse grupo.Count = 0 Then Continue For

            Dim framesGrupo As New List(Of cFrame)
            Dim storeyGrupo As String = Nothing

            For Each label In grupo
                Dim vigaContenedora As cViga
                If storeyGrupo Is Nothing Then
                    ' First frame: accept any story, then lock to that story
                    vigaContenedora = vigas.FirstOrDefault(
                        Function(v) v.Frames.Any(Function(f) f.ObjectLabel = label))
                Else
                    ' Subsequent frames: must match the story already determined
                    vigaContenedora = vigas.FirstOrDefault(
                        Function(v) v.Piso.Equals(storeyGrupo, StringComparison.OrdinalIgnoreCase) AndAlso
                                    v.Frames.Any(Function(f) f.ObjectLabel = label))
                End If
                If vigaContenedora Is Nothing Then Continue For

                Dim frame = vigaContenedora.Frames.First(Function(f) f.ObjectLabel = label)
                If storeyGrupo Is Nothing Then storeyGrupo = frame.Story
                vigaContenedora.Frames.Remove(frame)
                framesGrupo.Add(frame)
            Next

            If framesGrupo.Count = 0 Then Continue For

            Dim nuevaViga As New cViga With {.Piso = framesGrupo.First().Story}
            nuevaViga.Frames.AddRange(framesGrupo)

            Dim dir = _geo.VectorFrame(framesGrupo.First(), jointsDict)
            dir.Normalize()
            nuevaViga.Direccion = dir

            OrdenarFramesViga(nuevaViga, jointsDict)
            vigas.Add(nuevaViga)
        Next

        vigas.RemoveAll(Function(v) v.Frames.Count = 0)

        For i = 0 To vigas.Count - 1
            vigas(i).Nombre = "VIGA-" & (i + 1)
            vigas(i).Name_Beam = vigas(i).Nombre
        Next

    End Sub

    ''' Genera NombrePlano para cada viga usando su EjeParalelo y el prefijo del proyecto.
    ''' Formato: "{prefijo}-{EjeParalelo}"  →  "V-B", "VIGA-3", "Viga-A", etc.
    ''' Si hay múltiples vigas en el mismo eje/piso se añade sufijo: "V-B-1", "V-B-2".
    ''' Vigas sin EjeParalelo dejan NombrePlano vacío (Lista muestra Nombre = "VIGA-N").
    Public Sub GenerarNombresPlano(vigas As List(Of cViga), Optional prefijo As String = "V")

        If vigas Is Nothing OrElse vigas.Count = 0 Then Exit Sub
        If String.IsNullOrWhiteSpace(prefijo) Then prefijo = "V"

        For Each grupo In vigas.GroupBy(Function(v) v.Piso)

            Dim contadores = grupo _
                .Where(Function(v) Not String.IsNullOrEmpty(v.EjeParalelo)) _
                .GroupBy(Function(v) v.EjeParalelo) _
                .ToDictionary(Function(g) g.Key, Function(g) g.Count())

            Dim asignados As New Dictionary(Of String, Integer)()

            For Each viga In grupo
                viga.NombrePlano = ""
                If String.IsNullOrEmpty(viga.EjeParalelo) Then Continue For

                Dim eje = viga.EjeParalelo
                If Not asignados.ContainsKey(eje) Then asignados(eje) = 0
                asignados(eje) += 1

                viga.NombrePlano = If(contadores(eje) = 1,
                                      prefijo & "-" & eje,
                                      prefijo & "-" & eje & "-" & asignados(eje))
            Next
        Next

    End Sub

    ''' <summary>
    ''' Valida cuántos frames quedaron sin fuerzas asignadas tras CalcularEnvolventesVigas.
    ''' Retorna un resumen de diagnóstico. Retorna Nothing si todo está correcto.
    ''' </summary>
    Public Function DiagnosticarFuerzasAsignadas(vigas As List(Of cViga)) As String

        Dim totalFrames As Integer = 0
        Dim framesSinFuerzas As Integer = 0
        Dim vigasSinFuerzas As Integer = 0

        For Each viga In vigas
            Dim vigaTieneAlgunaFuerza As Boolean = False
            For Each frame In viga.Frames
                totalFrames += 1
                If frame.EnvolventeMomento Is Nothing OrElse frame.EnvolventeMomento.Estaciones.Count = 0 Then
                    framesSinFuerzas += 1
                Else
                    vigaTieneAlgunaFuerza = True
                End If
            Next
            If Not vigaTieneAlgunaFuerza Then vigasSinFuerzas += 1
        Next

        If framesSinFuerzas = 0 Then Return Nothing

        Return $"Diagnóstico: {framesSinFuerzas} de {totalFrames} frames sin fuerzas " &
               $"({vigasSinFuerzas} vigas completamente vacías). " &
               "Verifique que los nombres de los frames y los pisos coincidan entre la geometría y los resultados."

    End Function

    ' =========================================================================
    ' SISTEMA DE GRUPOS DE RÉPLICA
    ' =========================================================================

    ''' Resultado de detección de compatibilidad para un piso candidato.
    Public Class CompatibilidadPiso
        Public Property Piso As String
        Public Property EsCompatible As Boolean       ' True = todos los frames del patrón existen
        Public Property LabelsEncontrados As List(Of String)
        Public Property LabelsFaltantes As List(Of String)
        ''' Viga que ya tiene exactamente los mismos labels agrupados (puede ser Nothing).
        Public Property VigaCoincidente As cViga
        Public ReadOnly Property Resumen As String
            Get
                If EsCompatible Then
                    Return If(VigaCoincidente IsNot Nothing,
                              $"Agrupado ({LabelsEncontrados.Count} frames)",
                              $"Compatible ({LabelsEncontrados.Count} frames)")
                Else
                    Return $"Faltan: {String.Join(", ", LabelsFaltantes)}"
                End If
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Para cada piso (excepto el del patrón) detecta si existen los mismos frame labels.
    ''' rawLabelsPorPiso: inventario Story→Labels extraído directamente del DataTable de ETABS
    ''' (sin el filtro de detección de extremos). Si se provee, se usa en lugar de todosFrames
    ''' para la lista de pisos y el índice de labels — garantiza que aparezcan pisos que
    ''' DataTableToFrames descarta por fallo en la detección de joints (e.g. pisos de transferencia).
    ''' </summary>
    Public Function DetectarPisosCompatibles(patron As cViga,
                                              todasVigas As List(Of cViga),
                                              todosFrames As List(Of cFrame),
                                              Optional rawLabelsPorPiso As Dictionary(Of String, HashSet(Of String)) = Nothing) As List(Of CompatibilidadPiso)

        Dim resultado As New List(Of CompatibilidadPiso)()
        Dim labelsP = patron.Frames _
                           .Select(Function(f) f.ObjectLabel.Trim().ToUpperInvariant()) _
                           .ToList()
        Dim setP = New HashSet(Of String)(labelsP, StringComparer.OrdinalIgnoreCase)

        Dim pisoPatron = patron.Piso

        ' Construir inventario label→piso desde TODAS las fuentes (unión).
        ' Garantiza que pisos como P1, P10-P13 aparezcan aunque alguna fuente
        ' los haya perdido por problemas de joints o versiones previas del algoritmo.
        Dim labelsPorPiso As New Dictionary(Of String, HashSet(Of String))(StringComparer.OrdinalIgnoreCase)

        ' Fuente 1: vigas ya procesadas (_vigas) — siempre disponible
        For Each v In todasVigas
            If String.IsNullOrEmpty(v.Piso) Then Continue For
            For Each f In v.Frames
                Dim lbl = f.ObjectLabel.Trim().ToUpperInvariant()
                If String.IsNullOrEmpty(lbl) Then Continue For
                If Not labelsPorPiso.ContainsKey(v.Piso) Then
                    labelsPorPiso(v.Piso) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                End If
                labelsPorPiso(v.Piso).Add(lbl)
            Next
        Next

        ' Fuente 2: todos los frames procesados (Proyecto.Elementos.Vigas.Frames)
        For Each f In todosFrames
            If String.IsNullOrEmpty(f.Story) Then Continue For
            Dim lbl = f.ObjectLabel.Trim().ToUpperInvariant()
            If String.IsNullOrEmpty(lbl) Then Continue For
            If Not labelsPorPiso.ContainsKey(f.Story) Then
                labelsPorPiso(f.Story) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            End If
            labelsPorPiso(f.Story).Add(lbl)
        Next

        ' Fuente 3: DataTable crudo (cubre frames descartados por joints problemáticos)
        If rawLabelsPorPiso IsNot Nothing Then
            For Each kv In rawLabelsPorPiso
                If String.IsNullOrEmpty(kv.Key) Then Continue For
                If Not labelsPorPiso.ContainsKey(kv.Key) Then
                    labelsPorPiso(kv.Key) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                End If
                For Each lbl In kv.Value
                    labelsPorPiso(kv.Key).Add(lbl)
                Next
            Next
        End If

        Dim pisos = labelsPorPiso.Keys _
                        .Where(Function(s) Not String.IsNullOrEmpty(s) AndAlso
                                           Not s.Equals(pisoPatron, StringComparison.OrdinalIgnoreCase)) _
                        .OrderBy(Function(p) p) _
                        .ToList()

        For Each piso In pisos
            Dim labelsEnPiso As HashSet(Of String) = Nothing
            If Not labelsPorPiso.TryGetValue(piso, labelsEnPiso) Then
                labelsEnPiso = New HashSet(Of String)()
            End If

            Dim encontrados = labelsP.Where(Function(l) labelsEnPiso.Contains(l)).ToList()
            Dim faltantes = labelsP.Where(Function(l) Not labelsEnPiso.Contains(l)).ToList()

            ' Verificar si ya existe una viga agrupada con exactamente esos labels en _vigas
            Dim vigasPiso = todasVigas _
                                .Where(Function(v) v.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase)) _
                                .ToList()
            Dim vigaCoinc = vigasPiso.FirstOrDefault(
                Function(v) setP.SetEquals(v.Frames.Select(Function(f) f.ObjectLabel.Trim().ToUpperInvariant())))

            resultado.Add(New CompatibilidadPiso With {
                .Piso = piso,
                .EsCompatible = (faltantes.Count = 0),
                .LabelsEncontrados = encontrados,
                .LabelsFaltantes = faltantes,
                .VigaCoincidente = vigaCoinc
            })
        Next

        Return resultado

    End Function

    ''' <summary>
    ''' Tras un recalcular (Button1_Click), restaura los vínculos patrón/similar en la nueva
    ''' lista de vigas y aplica las agrupaciones de réplica para los pisos similares.
    ''' No copia refuerzo — el usuario lo propaga explícitamente.
    ''' </summary>
    Public Sub AplicarGruposReplicaEnVigas(vigas As List(Of cViga),
                                            gruposReplica As List(Of GrupoReplicaViga),
                                            jointsDict As Dictionary(Of String, cJoint))

        If gruposReplica Is Nothing OrElse gruposReplica.Count = 0 Then Return

        For Each grupo In gruposReplica

            ' --- Aplicar agrupación en cada piso similar ---
            For Each miembro In grupo.Similares
                _AplicarUnGrupoEnPiso(grupo.Labels_Patron, miembro.Piso, vigas, jointsDict)
            Next

            ' --- Reasignar vínculos ---
            Dim pisoPatron = grupo.Piso_Patron
            For Each v In vigas
                Dim labelsV As New HashSet(Of String)(v.Frames.Select(Function(f) f.ObjectLabel.Trim().ToUpperInvariant()))
                Dim labelsG As New HashSet(Of String)(grupo.Labels_Patron.Select(Function(l) l.Trim().ToUpperInvariant()))
                If Not labelsG.SetEquals(labelsV) Then Continue For

                If v.Piso.Equals(pisoPatron, StringComparison.OrdinalIgnoreCase) Then
                    v.GrupoReplicaID = grupo.ID
                    v.EsPatronGrupo = True
                    v.RefuerzoDesincronizado = False
                ElseIf grupo.Similares.Any(Function(m) m.Piso.Equals(v.Piso, StringComparison.OrdinalIgnoreCase)) Then
                    v.GrupoReplicaID = grupo.ID
                    v.EsPatronGrupo = False
                    v.RefuerzoDesincronizado = True  ' requiere nueva propagación tras recalcular
                End If
            Next

        Next

        ' Renombrar para que VIGA-N sea consistente
        For i = 0 To vigas.Count - 1
            vigas(i).Nombre = "VIGA-" & (i + 1)
            vigas(i).Name_Beam = vigas(i).Nombre
        Next

    End Sub

    ''' Aplica la agrupación de un conjunto de labels a un piso específico.
    ''' Si los frames ya están juntos en una viga, no hace nada.
    Public Sub _AplicarUnGrupoEnPiso(labels As List(Of String),
                                       piso As String,
                                       vigas As List(Of cViga),
                                       jointsDict As Dictionary(Of String, cJoint))

        Dim setL = New HashSet(Of String)(labels.Select(Function(l) l.Trim().ToUpperInvariant()), StringComparer.OrdinalIgnoreCase)

        ' ¿Ya existe una viga con exactamente esos labels en ese piso?
        Dim existente = vigas.FirstOrDefault(
            Function(v) v.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase) AndAlso
                        setL.SetEquals(v.Frames.Select(Function(f) f.ObjectLabel.Trim().ToUpperInvariant())))
        If existente IsNot Nothing Then Return

        ' Extraer frames del piso por label
        Dim framesGrupo As New List(Of cFrame)()
        For Each label In labels
            Dim labelUp = label.Trim().ToUpperInvariant()
            Dim contenedora = vigas.FirstOrDefault(
                Function(v) v.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase) AndAlso
                            v.Frames.Any(Function(f) f.ObjectLabel.Trim().ToUpperInvariant() = labelUp))
            If contenedora Is Nothing Then Continue For
            Dim frame = contenedora.Frames.First(Function(f) f.ObjectLabel.Trim().ToUpperInvariant() = labelUp)
            contenedora.Frames.Remove(frame)
            framesGrupo.Add(frame)
        Next

        If framesGrupo.Count = 0 Then Return

        Dim nueva As New cViga With {.Piso = piso}
        nueva.Frames.AddRange(framesGrupo)
        Dim dir = _geo.VectorFrame(framesGrupo.First(), jointsDict)
        dir.Normalize()
        nueva.Direccion = dir
        OrdenarFramesViga(nueva, jointsDict)
        vigas.Add(nueva)
        vigas.RemoveAll(Function(v) v.Frames.Count = 0)

    End Sub

    ''' <summary>
    ''' Propaga el refuerzo del patrón a todos sus similares y recalcula C/D para cada uno.
    ''' </summary>
    Public Sub PropagateRefuerzoGrupo(patron As cViga,
                                       similares As List(Of cViga),
                                       combosCortante As HashSet(Of String))

        For Each sim In similares
            _CopiarRefuerzoViga(patron, sim)
            CalcularFlexionViga(sim)
            If combosCortante IsNot Nothing AndAlso combosCortante.Count > 0 Then
                CalcularCapacidadCortante(New List(Of cViga) From {sim})
            End If
            sim.RefuerzoDesincronizado = False
        Next

    End Sub

    ''' Copia el refuerzo frame a frame del origen al destino (empareja por ObjectLabel).
    ''' También actualiza RevisionFlexion.AsProvSup/AsProvInf para que los ratios sean correctos.
    Public Sub _CopiarRefuerzoViga(origen As cViga, destino As cViga)

        For Each fO In origen.Frames
            ' Emparejar por ObjectLabel, no por índice — más robusto si el orden difiere
            Dim fD = destino.Frames.Find(Function(f) f.ObjectLabel.Equals(fO.ObjectLabel, StringComparison.OrdinalIgnoreCase))
            If fD Is Nothing Then Continue For

            fD.RefuerzoSuperior.Clear()
            fD.RefuerzoInferior.Clear()
            fD.RefuerzoTransversal.Clear()

            For Each tramo In fO.RefuerzoSuperior
                fD.RefuerzoSuperior.Add(_ClonarTramoRef(tramo))
            Next
            For Each tramo In fO.RefuerzoInferior
                fD.RefuerzoInferior.Add(_ClonarTramoRef(tramo))
            Next
            For Each zona In fO.RefuerzoTransversal
                fD.RefuerzoTransversal.Add(New cRefuerzoTransversalZona With {
                    .Posicion = zona.Posicion,
                    .NumEstribos = zona.NumEstribos,
                    .NumeroBarra = zona.NumeroBarra,
                    .CantEstribos = zona.CantEstribos,
                    .Separacion = zona.Separacion
                })
            Next

            ' Actualizar AsProvSup/AsProvInf en RevisionFlexion para que los ratios C/D sean correctos
            _ActualizarRevisionAsProvEnFrame(fD)
        Next

    End Sub

    ''' Recalcula AsProvSup/AsProvInf en cada RevisionFlexion del frame a partir de los tramos copiados.
    Private Sub _ActualizarRevisionAsProvEnFrame(frame As cFrame)

        Dim posiciones = {PosicionTramoViga.Izquierda, PosicionTramoViga.Centro, PosicionTramoViga.Derecha}

        For Each pos In posiciones
            Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)
            If revision Is Nothing Then Continue For

            Dim tramoSup = frame.RefuerzoSuperior.FirstOrDefault(Function(t) t.Posicion = pos)
            If tramoSup IsNot Nothing Then
                Dim asS = tramoSup.Barras.Sum(Function(kvp) CDbl(kvp.Value) * AreaRefuerzo(kvp.Key))
                revision.ResultadoBase.AsProvSup = asS
                revision.ResultadoActual.AsProvSup = asS
            End If

            Dim tramoInf = frame.RefuerzoInferior.FirstOrDefault(Function(t) t.Posicion = pos)
            If tramoInf IsNot Nothing Then
                Dim asI = tramoInf.Barras.Sum(Function(kvp) CDbl(kvp.Value) * AreaRefuerzo(kvp.Key))
                revision.ResultadoBase.AsProvInf = asI
                revision.ResultadoActual.AsProvInf = asI
            End If
        Next

    End Sub

    Private Function _ClonarTramoRef(origen As cRefuerzoTramo) As cRefuerzoTramo
        Dim nuevo As New cRefuerzoTramo
        nuevo.Posicion = origen.Posicion
        For Each kvp In origen.Barras
            nuevo.Barras(kvp.Key) = kvp.Value
        Next
        Return nuevo
    End Function

End Class
