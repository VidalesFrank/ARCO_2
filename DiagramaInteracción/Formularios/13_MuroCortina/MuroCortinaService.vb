' ═══════════════════════════════════════════════════════════════════════════════
'  MuroCortinaService — análisis de pantallas por el método de rigidez.
'
'  La pantalla se modela como una viga de Euler-Bernoulli con N tramos y
'  (N+1) nodos. En cada nodo hay 2 DOF: deflexión transversal (v) y rotación (θ).
'
'  Convención de ejes:
'    - y crece hacia ARRIBA (y = 0 en la base del muro, y = H en la corona).
'    - En el elemento (= tramo), el nodo 1 es el INFERIOR (menor y) y
'      el nodo 2 es el SUPERIOR.
'    - La carga distribuida w(y) es la presión horizontal que empuja el
'      muro desde el relleno hacia el lado libre; positiva en esa dirección.
'
'  Cargas que suma w(y):
'    - Empuje activo del suelo (triangular, 0 en corona, Ka·γ·H en la base).
'    - Si hay nivel freático con hidrostática activa, debajo del NF el
'      empuje usa γ' = γ_sat − γ_agua y se suma presión hidrostática.
'    - Sobrecarga uniforme Ka·q (si incluida).
'    - Caso sísmico: el solver corre dos veces y toma envolvente para el
'      diseño; el Ka sísmico es Ka_estático · FactorSismico.
'
'  Factores de carga (igual que MuroContencionService):
'    - Residencial  : 1.60 EH, 1.60 LS, 1.20 DC/EV, 1.0 EQ en sísmico.
'    - Vial (LRFD)  : 1.50 EH, 1.75 LS, 1.25 DC, 1.35 EV, 1.0 EQ en sísmico.
'
'  Diseño por tramo: para cada tramo, se toma |M_max| y |V_max| del
'  diagrama envolvente (estático ∪ sísmico) dentro del tramo y se dimensiona
'  flexión + cortante con las fórmulas NSR-10 (reusa AreaAceroFlexion y
'  AceroTemperatura del MuroContencionService).
' ═══════════════════════════════════════════════════════════════════════════════
Public Class MuroCortinaService

    Private Const PHI_CORTANTE As Double = 0.75

    ' =====================================================================
    ' Punto de entrada
    ' =====================================================================
    Public Shared Sub AnalizarMuro(muro As MuroCortina)
        If muro Is Nothing Then Throw New ArgumentNullException(NameOf(muro))
        Validar(muro)

        Dim res As New ResultadosCortina() With {.FechaCalculo = Date.Now}

        ' Ka estático y sísmico
        Dim kaEst = CalcularKa(muro.Suelo)
        Dim kaSis As Double = 0
        If muro.Opciones.IncluirSismo Then kaSis = kaEst * muro.Opciones.FactorSismico
        res.Ka = kaEst
        res.KaSismico = kaSis

        ' --- Corrida estática --------------------------------------------
        Dim fcEst = FactoresCarga(muro.Normativa, eCasoCargaMuro.Estatico)
        Dim diagEstM As List(Of PuntoDiagrama) = Nothing
        Dim diagEstV As List(Of PuntoDiagrama) = Nothing
        Dim reaccionesEst As Double() = Nothing
        CorrerAnalisis(muro, kaEst, fcEst, False, diagEstM, diagEstV, reaccionesEst)

        res.DiagramaM = diagEstM
        res.DiagramaV = diagEstV
        res.Reacciones = reaccionesEst.ToList()

        ' --- Corrida sísmica (si aplica) ---------------------------------
        Dim diagSisM As List(Of PuntoDiagrama) = Nothing
        Dim diagSisV As List(Of PuntoDiagrama) = Nothing
        If muro.Opciones.IncluirSismo Then
            Dim fcSis = FactoresCarga(muro.Normativa, eCasoCargaMuro.Sismico)
            Dim reaccionesSis As Double() = Nothing
            CorrerAnalisis(muro, kaSis, fcSis, True, diagSisM, diagSisV, reaccionesSis)
        End If

        ' Envolvente para el diseño: max(|M|, |V|) por tramo entre estática y sísmica
        res.DisenosTramos = DisenarPorTramo(muro, diagEstM, diagEstV, diagSisM, diagSisV)

        ' Resumen
        res.MomentoMaximo = diagEstM.Max(Function(p) Math.Abs(p.Valor))
        res.CortanteMaximo = diagEstV.Max(Function(p) Math.Abs(p.Valor))
        res.YMomentoMax = diagEstM.OrderByDescending(Function(p) Math.Abs(p.Valor)).First().Y

        muro.Resultados = res
    End Sub

    ' =====================================================================
    ' Validación
    ' =====================================================================
    Private Shared Sub Validar(muro As MuroCortina)
        If muro.Tramos Is Nothing OrElse muro.Tramos.Count = 0 Then
            Throw New InvalidOperationException("El muro no tiene tramos.")
        End If
        If muro.Apoyos Is Nothing OrElse muro.Apoyos.Count <> muro.Tramos.Count + 1 Then
            Throw New InvalidOperationException(
                $"Se esperaban {muro.Tramos.Count + 1} apoyos y hay {If(muro.Apoyos?.Count, 0)}.")
        End If
        If muro.Tramos.Any(Function(t) t.Altura <= 0) Then
            Throw New InvalidOperationException("Todos los tramos deben tener altura mayor a 0.")
        End If
        If muro.Tramos.Any(Function(t) t.Espesor <= 0) Then
            Throw New InvalidOperationException("Todos los tramos deben tener espesor mayor a 0.")
        End If

        ' Estabilidad: debe haber al menos 1 empotramiento, o al menos 2 apoyos simples.
        Dim nEmp = muro.Apoyos.Where(Function(a) a.Tipo = eTipoApoyoCortina.Empotrado).Count()
        Dim nSim = muro.Apoyos.Where(Function(a) a.Tipo = eTipoApoyoCortina.SimplementeApoyado).Count()
        If nEmp = 0 AndAlso nSim < 2 Then
            Throw New InvalidOperationException(
                "La estructura no es estable: se necesita al menos un apoyo empotrado o dos apoyos simples.")
        End If
    End Sub

    ' =====================================================================
    ' Ka
    ' =====================================================================
    Public Shared Function CalcularKa(s As PropsSueloCortina) As Double
        If Not s.CalcularKaAutomatico Then Return s.KaManual
        Dim phi = s.Phi * Math.PI / 180.0
        Select Case s.Teoria
            Case eTeoriaEmpuje.Rankine
                Return Math.Tan(Math.PI / 4.0 - phi / 2.0) ^ 2
            Case Else   ' Coulomb (δ ≠ 0)
                Dim delta = s.Delta * Math.PI / 180.0
                Dim num = Math.Cos(phi) ^ 2
                Dim arg = Math.Sin(phi + delta) * Math.Sin(phi) / Math.Cos(delta)
                If arg < 0 Then arg = 0
                Dim den = Math.Cos(delta) * (1 + Math.Sqrt(arg)) ^ 2
                If den <= 0 Then Return 0.333
                Return num / den
        End Select
    End Function

    ' =====================================================================
    ' Carga w(y) — presión horizontal a altura y, en kN/m² (para 1 m de ancho).
    ' =====================================================================
    Public Shared Function CargaEnY(y As Double, muro As MuroCortina, ka As Double,
                                     fcEH As Double, fcLS As Double) As Double
        If y < 0 OrElse y > muro.AlturaTotal Then Return 0
        Dim H = muro.AlturaTotal
        Dim profundidad = H - y

        ' --- Empuje del suelo (triangular por profundidad) --------------
        Dim wSuelo As Double
        If muro.Opciones.IncluirHidrostatica AndAlso muro.Opciones.NivelAguaFreatica > 0 Then
            Dim yNF = Math.Min(muro.Opciones.NivelAguaFreatica, H)
            If y >= yNF Then
                ' Encima del NF: suelo seco/húmedo (γ)
                wSuelo = ka * muro.Suelo.Gamma * profundidad
            Else
                ' Debajo del NF: γ·H_dry + γ'·H_wet, con γ' = γ_sat − γ_agua
                Dim hDry = H - yNF
                Dim hWet = yNF - y
                Dim gammaEf = muro.Suelo.GammaSat - muro.Opciones.GammaAgua
                wSuelo = ka * (muro.Suelo.Gamma * hDry + gammaEf * hWet)
            End If
        Else
            wSuelo = ka * muro.Suelo.Gamma * profundidad
        End If

        ' --- Sobrecarga uniforme (Ka·q) ---------------------------------
        Dim wSob As Double = 0
        If muro.Sobrecarga.IncluirSobrecarga Then
            wSob = ka * muro.Sobrecarga.SobrecargaKPa
        End If

        ' --- Hidrostática del agua libre (triangular bajo NF) -----------
        Dim wAgua As Double = 0
        If muro.Opciones.IncluirHidrostatica AndAlso muro.Opciones.NivelAguaFreatica > 0 AndAlso y < muro.Opciones.NivelAguaFreatica Then
            wAgua = muro.Opciones.GammaAgua * (muro.Opciones.NivelAguaFreatica - y)
        End If

        ' Aplicar factores de carga por tipo
        Return (wSuelo * fcEH) + (wSob * fcLS) + (wAgua * fcEH)
    End Function

    ' =====================================================================
    ' Orquestador de una corrida (estática o sísmica).
    ' Devuelve diagramas M, V y vector de reacciones. w(y) se evalúa con el
    ' Ka y los factores de carga pasados.
    ' =====================================================================
    Private Shared Sub CorrerAnalisis(muro As MuroCortina, ka As Double,
                                       fc As FactoresLRFD, esSismico As Boolean,
                                       ByRef diagM As List(Of PuntoDiagrama),
                                       ByRef diagV As List(Of PuntoDiagrama),
                                       ByRef reacciones As Double())

        Dim nTramos = muro.Tramos.Count
        Dim nNodos = nTramos + 1
        Dim nDofs = 2 * nNodos

        ' Posición Y de cada nodo (acumulado desde la base)
        Dim yNodo(nNodos - 1) As Double
        Dim yAcum As Double = 0
        yNodo(0) = 0
        For i = 0 To nTramos - 1
            yAcum += muro.Tramos(i).Altura
            yNodo(i + 1) = yAcum
        Next

        ' Módulo de elasticidad del concreto (NSR-10 C.8.5.1):
        '   E_c = 4700·√fc'  [MPa]   →   E_c = 4700·√fc·1000  [kPa]
        Dim fc_MPa = CDbl(muro.Materiales.Fc)
        Dim Ec_kPa = 4700.0 * Math.Sqrt(fc_MPa) * 1000.0

        ' Ensamblar K global (densa) y F global
        Dim K(nDofs - 1, nDofs - 1) As Double
        Dim F(nDofs - 1) As Double

        ' Carga w(y) evaluada para esta corrida
        Dim fcEH = fc.EH, fcLS = fc.LS
        Dim wFunc = Function(y As Double) CargaEnY(y, muro, ka, fcEH, fcLS)

        For iT = 0 To nTramos - 1
            Dim L = muro.Tramos(iT).Altura
            Dim t = muro.Tramos(iT).Espesor
            Dim I = (1.0 * t * t * t) / 12.0    ' b=1 m → I = t³/12 [m^4]
            Dim EI = Ec_kPa * I                 ' kN·m²

            Dim w1 = wFunc(yNodo(iT))           ' en nodo inferior (y menor)
            Dim w2 = wFunc(yNodo(iT + 1))       ' en nodo superior

            Dim kE = MatrizRigidezViga(EI, L)
            Dim fE = CargasEquivalentesTrapecio(w1, w2, L)

            ' Ensamblar en K global y F global
            Dim gdl = {2 * iT, 2 * iT + 1, 2 * (iT + 1), 2 * (iT + 1) + 1}
            For i = 0 To 3
                F(gdl(i)) += fE(i)
                For j = 0 To 3
                    K(gdl(i), gdl(j)) += kE(i, j)
                Next
            Next
        Next

        ' --- Condiciones de borde ---------------------------------------
        '  Empotrado → DOFs v y θ restringidos.
        '  Simple    → solo v restringido.
        '  Libre     → nada.
        Dim restringido(nDofs - 1) As Boolean
        For iNodo = 0 To nNodos - 1
            Dim ap = muro.Apoyos(iNodo)
            Select Case ap.Tipo
                Case eTipoApoyoCortina.Empotrado
                    restringido(2 * iNodo) = True
                    restringido(2 * iNodo + 1) = True
                Case eTipoApoyoCortina.SimplementeApoyado
                    restringido(2 * iNodo) = True
            End Select
        Next

        ' --- Reducir sistema y resolver ---------------------------------
        Dim dofsLibres As New List(Of Integer)
        For i = 0 To nDofs - 1
            If Not restringido(i) Then dofsLibres.Add(i)
        Next
        Dim nFree = dofsLibres.Count
        If nFree = 0 Then
            ' Totalmente restringido — no hay desplazamientos a resolver
            Dim uZero(nDofs - 1) As Double
            CalcularSalida(muro, ka, fc, uZero, K, wFunc, yNodo, Ec_kPa, diagM, diagV, reacciones)
            Return
        End If

        Dim Kff(nFree - 1, nFree - 1) As Double
        Dim Ff(nFree - 1) As Double
        For i = 0 To nFree - 1
            Ff(i) = F(dofsLibres(i))
            For j = 0 To nFree - 1
                Kff(i, j) = K(dofsLibres(i), dofsLibres(j))
            Next
        Next

        Dim uFree = ResolverGauss(Kff, Ff)

        ' Reconstruir vector u completo
        Dim u(nDofs - 1) As Double
        For i = 0 To nFree - 1
            u(dofsLibres(i)) = uFree(i)
        Next

        CalcularSalida(muro, ka, fc, u, K, wFunc, yNodo, Ec_kPa, diagM, diagV, reacciones)
    End Sub

    ''' <summary>Dado el vector u resuelto, muestrea los diagramas M y V y calcula las reacciones.</summary>
    Private Shared Sub CalcularSalida(muro As MuroCortina, ka As Double, fc As FactoresLRFD,
                                       u As Double(), K As Double(,), wFunc As Func(Of Double, Double),
                                       yNodo As Double(), Ec_kPa As Double,
                                       ByRef diagM As List(Of PuntoDiagrama),
                                       ByRef diagV As List(Of PuntoDiagrama),
                                       ByRef reacciones As Double())
        Dim nTramos = muro.Tramos.Count
        Dim nNodos = nTramos + 1
        Dim nDofs = 2 * nNodos

        diagM = New List(Of PuntoDiagrama)()
        diagV = New List(Of PuntoDiagrama)()

        Const NMUESTRAS As Integer = 20

        For iT = 0 To nTramos - 1
            Dim L = muro.Tramos(iT).Altura
            Dim t = muro.Tramos(iT).Espesor
            Dim I = (t * t * t) / 12.0
            Dim EI = Ec_kPa * I

            Dim v1 = u(2 * iT), th1 = u(2 * iT + 1)
            Dim v2 = u(2 * (iT + 1)), th2 = u(2 * (iT + 1) + 1)

            Dim w1 = wFunc(yNodo(iT))
            Dim w2 = wFunc(yNodo(iT + 1))

            ' Fuerzas y momentos en el extremo inferior del elemento
            ' (M y V internos justo a la derecha del nodo 1)
            '
            '   Para una viga de Euler-Bernoulli con DOFs {v1, θ1, v2, θ2} y
            '   carga trapezoidal (w1, w2), las fuerzas internas en los
            '   extremos son:  f_int = [k_e]·u − f_eq
            '
            '   El vector resultante es [V1, M1, V2, M2] en la convención FEM.
            '
            '   Para los diagramas:
            '     V(x_local) = V1 − ∫₀^x w(ξ)·dξ
            '     M(x_local) = M1 + V1·x − ∫₀^x w(ξ)·(x−ξ)·dξ
            Dim kE = MatrizRigidezViga(EI, L)
            Dim fE = CargasEquivalentesTrapecio(w1, w2, L)
            Dim uE = {v1, th1, v2, th2}
            Dim Vint1 = kE(0, 0) * uE(0) + kE(0, 1) * uE(1) + kE(0, 2) * uE(2) + kE(0, 3) * uE(3) - fE(0)
            Dim Mint1 = kE(1, 0) * uE(0) + kE(1, 1) * uE(1) + kE(1, 2) * uE(2) + kE(1, 3) * uE(3) - fE(1)

            ' Muestreo del elemento
            For iM = 0 To NMUESTRAS
                Dim x = iM * L / NMUESTRAS   ' coord local, 0 en nodo inferior
                Dim y = yNodo(iT) + x        ' coord global desde la base

                ' Integrales de la carga trapezoidal w(ξ) = w1 + (w2−w1)·ξ/L
                Dim intW = w1 * x + (w2 - w1) * x * x / (2 * L)
                Dim intWx = w1 * x * x / 2.0 + (w2 - w1) * x * x * x / (6 * L)

                ' M interno = M1_int − V1_int·x + ∫w·(x−x')·dx'  (derivado por equilibrio
                ' del segmento [0, x] respecto al punto x, con V1_int = fuerza del
                ' elemento al nodo 1 hacia +y y M+ antihorario).
                Dim V = Vint1 - intW
                Dim M = Mint1 - Vint1 * x + intWx

                ' Evitar duplicar nodos interiores (el final de un elemento = inicio del siguiente)
                If iT > 0 AndAlso iM = 0 Then Continue For
                diagV.Add(New PuntoDiagrama With {.Y = y, .Valor = V})
                diagM.Add(New PuntoDiagrama With {.Y = y, .Valor = M})
            Next
        Next

        ' Reacciones = K · u (en los DOFs restringidos del vector completo)
        '
        '   R_dof = Σ_j K[dof, j]·u[j]  − F[dof]
        '
        '   donde F[dof] son las cargas nodales equivalentes aplicadas a ese
        '   DOF por las cargas distribuidas. El resultado es la REACCIÓN que
        '   el apoyo ejerce sobre el muro en ese DOF.
        '
        '   Para los apoyos (DOF v): R es la fuerza horizontal del apoyo en kN/m.
        reacciones = New Double(nNodos - 1) {}
        For iNodo = 0 To nNodos - 1
            Dim ap = muro.Apoyos(iNodo)
            If ap.Tipo = eTipoApoyoCortina.Libre Then Continue For
            Dim dofV = 2 * iNodo
            Dim suma As Double = 0
            For j = 0 To nDofs - 1
                suma += K(dofV, j) * u(j)
            Next
            ' Restar las cargas nodales equivalentes aplicadas al dof v de este nodo
            ' (están distribuidas sobre los elementos adyacentes).
            Dim fNodal As Double = 0
            For iT = 0 To nTramos - 1
                If iT = iNodo Then
                    ' Nodo iNodo es el nodo 1 del elemento iT
                    Dim L = muro.Tramos(iT).Altura
                    fNodal += CargasEquivalentesTrapecio(wFunc(yNodo(iT)), wFunc(yNodo(iT + 1)), L)(0)
                ElseIf iT + 1 = iNodo Then
                    ' Nodo iNodo es el nodo 2 del elemento iT
                    Dim L = muro.Tramos(iT).Altura
                    fNodal += CargasEquivalentesTrapecio(wFunc(yNodo(iT)), wFunc(yNodo(iT + 1)), L)(2)
                End If
            Next
            reacciones(iNodo) = suma - fNodal
        Next
    End Sub

    ' =====================================================================
    ' Matriz de rigidez de un elemento viga Euler-Bernoulli (4x4)
    '
    '   k = EI/L³ · [[  12    6L  -12   6L  ]
    '                [  6L   4L²  -6L  2L² ]
    '                [ -12   -6L   12  -6L  ]
    '                [  6L   2L²  -6L  4L² ]]
    ' =====================================================================
    Private Shared Function MatrizRigidezViga(EI As Double, L As Double) As Double(,)
        Dim c = EI / (L * L * L)
        Dim k(3, 3) As Double
        k(0, 0) = 12 * c : k(0, 1) = 6 * L * c : k(0, 2) = -12 * c : k(0, 3) = 6 * L * c
        k(1, 0) = 6 * L * c : k(1, 1) = 4 * L * L * c : k(1, 2) = -6 * L * c : k(1, 3) = 2 * L * L * c
        k(2, 0) = -12 * c : k(2, 1) = -6 * L * c : k(2, 2) = 12 * c : k(2, 3) = -6 * L * c
        k(3, 0) = 6 * L * c : k(3, 1) = 2 * L * L * c : k(3, 2) = -6 * L * c : k(3, 3) = 4 * L * L * c
        Return k
    End Function

    ''' <summary>
    ''' Vector de cargas nodales equivalentes para una carga lineal
    ''' w(ξ) = w1(1−ξ) + w2·ξ con ξ=x/L (nodo 1 abajo, nodo 2 arriba).
    ''' Orden: {F_v1, M_θ1, F_v2, M_θ2}. Convención FEM Logan:
    '''   Para w constante positiva (+y), f_eq = [wL/2, +wL²/12, wL/2, −wL²/12].
    '''   Los momentos tienen signos opuestos entre nodo 1 y nodo 2.
    ''' </summary>
    Private Shared Function CargasEquivalentesTrapecio(w1 As Double, w2 As Double, L As Double) As Double()
        ' Convención: carga w+ en dirección +y transversal, v+ en +y, θ+ antihorario.
        ' fe = cargas nodales equivalentes (Logan).
        ' Para w uniforme hacia +y: fe = [−wL/2, −wL²/12, −wL/2, +wL²/12].
        ' Validado contra:
        '   Simple-apoyada: Mint1=0, Vint1=+wL/2, M(L/2)=-wL²/8 ✓
        '   Empotrada-apoyada: Mint1=+wL²/8, Vint1=+5wL/8 ✓
        '   Cantilever (emp-libre): Mint1=+wL²/2, Vint1=+wL ✓
        Dim fe(3) As Double
        fe(0) = -L * (7 * w1 + 3 * w2) / 20.0        ' F_v1
        fe(1) = -L * L * (3 * w1 + 2 * w2) / 60.0    ' M_θ1
        fe(2) = -L * (3 * w1 + 7 * w2) / 20.0        ' F_v2
        fe(3) = +L * L * (2 * w1 + 3 * w2) / 60.0    ' M_θ2
        Return fe
    End Function

    ' =====================================================================
    ' Eliminación gaussiana con pivoteo parcial
    ' =====================================================================
    Private Shared Function ResolverGauss(K As Double(,), b As Double()) As Double()
        Dim n = b.Length
        ' Matriz ampliada [K | b]
        Dim A(n - 1, n) As Double
        For i = 0 To n - 1
            For j = 0 To n - 1
                A(i, j) = K(i, j)
            Next
            A(i, n) = b(i)
        Next

        ' Eliminación hacia adelante con pivoteo parcial
        For p = 0 To n - 1
            Dim maxRow = p
            Dim maxVal = Math.Abs(A(p, p))
            For r = p + 1 To n - 1
                If Math.Abs(A(r, p)) > maxVal Then
                    maxVal = Math.Abs(A(r, p))
                    maxRow = r
                End If
            Next
            If maxVal < 1.0E-12 Then
                Throw New InvalidOperationException(
                    "Matriz de rigidez singular — la estructura es inestable o está mal planteada.")
            End If
            If maxRow <> p Then
                For j = p To n
                    Dim tmp = A(p, j) : A(p, j) = A(maxRow, j) : A(maxRow, j) = tmp
                Next
            End If
            For r = p + 1 To n - 1
                Dim factor = A(r, p) / A(p, p)
                If factor = 0 Then Continue For
                For j = p To n
                    A(r, j) -= factor * A(p, j)
                Next
            Next
        Next

        ' Sustitución hacia atrás
        Dim x(n - 1) As Double
        For i = n - 1 To 0 Step -1
            Dim suma = A(i, n)
            For j = i + 1 To n - 1
                suma -= A(i, j) * x(j)
            Next
            x(i) = suma / A(i, i)
        Next
        Return x
    End Function

    ' =====================================================================
    ' Factores LRFD por normativa y caso — misma tabla que MuroContencionService
    ' =====================================================================
    Private Shared Function FactoresCarga(norma As eNormativaMuro, caso As eCasoCargaMuro) As FactoresLRFD
        If caso = eCasoCargaMuro.Sismico Then
            Return New FactoresLRFD With {.EH = 1.0, .EV = 1.0, .LS = 0.5, .DC = 1.0, .EQ = 1.0}
        End If
        Select Case norma
            Case eNormativaMuro.Vial
                Return New FactoresLRFD With {.EH = 1.50, .EV = 1.35, .LS = 1.75, .DC = 1.25, .EQ = 0.0}
            Case Else
                Return New FactoresLRFD With {.EH = 1.60, .EV = 1.20, .LS = 1.60, .DC = 1.20, .EQ = 0.0}
        End Select
    End Function

    Private Structure FactoresLRFD
        Public EH As Double
        Public EV As Double
        Public LS As Double
        Public DC As Double
        Public EQ As Double
    End Structure

    ' =====================================================================
    ' Diseño por tramo — doble capa cuando aplica (envolvente est∪sís)
    '
    '   Separa M+_max (hacia +, demanda cara exterior) y |M−|_max (hacia −,
    '   demanda cara interior). Diseña independientemente cada cara con la
    '   envolvente estática ∪ sísmica.
    '
    '   DobleCapaPorAnalisis = True cuando M+_max > 0 Y |M−|_max > 0.
    '   DobleCapaPorNorma    = True cuando t ≥ 0.25 m (NSR-10 C.14.3.4).
    '   Si solo una cara tiene momento Y se requiere doble capa por norma,
    '   la "cara vacía" se asigna refuerzo mínimo por temperatura.
    ' =====================================================================
    Private Shared Function DisenarPorTramo(muro As MuroCortina,
                                             diagEstM As List(Of PuntoDiagrama),
                                             diagEstV As List(Of PuntoDiagrama),
                                             diagSisM As List(Of PuntoDiagrama),
                                             diagSisV As List(Of PuntoDiagrama)) As List(Of DisenoTramoCortina)
        Dim res As New List(Of DisenoTramoCortina)
        Dim yAcum As Double = 0
        Dim fc As Single = muro.Materiales.Fc
        Dim fy As Single = muro.Materiales.Fy
        Dim recExt As Double = muro.Materiales.RecubrimientoExterior
        Dim recInt As Double = muro.Materiales.RecubrimientoInterior

        For iT = 0 To muro.Tramos.Count - 1
            Dim t = muro.Tramos(iT)
            Dim yA = yAcum
            Dim yB = yAcum + t.Altura

            ' Momento máximo positivo (cara exterior) y máximo negativo (cara interior)
            ' en el tramo, envolviendo caso estático y sísmico.
            Dim mPosEst = MaxEnTramo(diagEstM, yA, yB, True)   ' max(M, 0)
            Dim mNegEst = MaxEnTramo(diagEstM, yA, yB, False)  ' max(-M, 0)
            Dim mPosSis As Double = 0, mNegSis As Double = 0
            If diagSisM IsNot Nothing Then
                mPosSis = MaxEnTramo(diagSisM, yA, yB, True)
                mNegSis = MaxEnTramo(diagSisM, yA, yB, False)
            End If
            Dim mPos = Math.Max(mPosEst, mPosSis)       ' demanda cara positiva (M+)
            Dim mNeg = Math.Max(mNegEst, mNegSis)       ' demanda cara negativa (M-, en magnitud)

            Dim vEst = MaxAbsEnTramo(diagEstV, yA, yB)
            Dim vSis = If(diagSisV IsNot Nothing, MaxAbsEnTramo(diagSisV, yA, yB), 0)
            Dim vU = Math.Max(vEst, vSis)

            ' Peraltes efectivos por cara (depende del recubrimiento de cada cara).
            Dim dPos = Math.Max(t.Espesor - recExt, 0.001)
            Dim dNeg = Math.Max(t.Espesor - recInt, 0.001)
            ' Para el cortante se usa el d menor (más conservador).
            Dim dMin = Math.Min(dPos, dNeg)

            Dim dto As New DisenoTramoCortina With {
                .Nombre = If(String.IsNullOrWhiteSpace(t.Nombre), $"Tramo {iT + 1}", t.Nombre),
                .B = 1.0, .H = t.Espesor, .d = dMin, .Vu = vU
            }
            dto.PhiVc = PHI_CORTANTE * 0.17 * Math.Sqrt(fc) * 1.0 * dMin * 1000
            dto.AsReqTemperatura = MuroContencionService.AceroTemperatura(1.0, t.Espesor, fy)

            ' Flags de doble capa
            dto.DobleCapaPorAnalisis = (mPos > 0 AndAlso mNeg > 0)
            dto.DobleCapaPorNorma = (t.Espesor >= 0.25)

            ' --- Diseño de cada cara ----------------------------------
            dto.CaraPositiva.MuMax = mPos
            dto.CaraPositiva.d = dPos
            dto.CaraPositiva.Recubrimiento = recExt
            If mPos > 0 Then
                dto.CaraPositiva.AsReq = MuroContencionService.AreaAceroFlexion(1.0, dPos, mPos, fc, fy)
            End If

            dto.CaraNegativa.MuMax = mNeg
            dto.CaraNegativa.d = dNeg
            dto.CaraNegativa.Recubrimiento = recInt
            If mNeg > 0 Then
                dto.CaraNegativa.AsReq = MuroContencionService.AreaAceroFlexion(1.0, dNeg, mNeg, fc, fy)
            End If

            ' Si solo UNA cara tiene momento y se requiere doble capa por norma,
            ' la otra cara se diseña por refuerzo mínimo por temperatura.
            Dim asMinTempPorCara = dto.AsReqTemperatura / 2.0   ' se reparte en 2 caras
            If (dto.DobleCapaPorNorma OrElse dto.DobleCapaForzada) AndAlso Not dto.DobleCapaPorAnalisis Then
                If mPos = 0 AndAlso mNeg > 0 Then dto.CaraPositiva.AsReq = asMinTempPorCara
                If mNeg = 0 AndAlso mPos > 0 Then dto.CaraNegativa.AsReq = asMinTempPorCara
            End If

            PropuestaRefuerzoCara(dto.CaraPositiva)
            PropuestaRefuerzoCara(dto.CaraNegativa)

            ' Refuerzo de temperatura (común para ambas caras)
            dto.BarraTemperatura = "#4"
            dto.SeparacionTempCm = 20
            dto.AsProvTemperatura = Funciones_00_Varias.AreaRefuerzo("#4") * 100.0 / 20.0

            res.Add(dto)
            yAcum = yB
        Next
        Return res
    End Function

    ''' <summary>Máximo del valor del momento en un rango de alturas, con filtro
    ''' <paramref name="positivos"/> = True toma Max(M, 0); False toma Max(−M, 0).
    ''' Devuelve siempre una magnitud ≥ 0.</summary>
    Private Shared Function MaxEnTramo(diag As List(Of PuntoDiagrama), yA As Double, yB As Double,
                                        positivos As Boolean) As Double
        If diag Is Nothing OrElse diag.Count = 0 Then Return 0
        Dim m As Double = 0
        For Each p In diag
            If p.Y >= yA - 1.0E-6 AndAlso p.Y <= yB + 1.0E-6 Then
                Dim v = If(positivos, p.Valor, -p.Valor)
                If v > m Then m = v
            End If
        Next
        Return m
    End Function

    Private Shared Function MaxAbsEnTramo(diag As List(Of PuntoDiagrama), yA As Double, yB As Double) As Double
        If diag Is Nothing OrElse diag.Count = 0 Then Return 0
        Dim m As Double = 0
        For Each p In diag
            If p.Y >= yA - 1.0E-6 AndAlso p.Y <= yB + 1.0E-6 Then
                Dim a = Math.Abs(p.Valor)
                If a > m Then m = a
            End If
        Next
        Return m
    End Function

    ''' <summary>Propuesta inicial de barra y separación para una cara.</summary>
    Public Shared Sub PropuestaRefuerzoCara(cara As DisenoCaraCortina)
        If cara Is Nothing Then Return
        Dim barras = {"#4", "#5", "#6"}
        Dim mejorBarra As String = "#5"
        Dim mejorSep As Double = 20.0

        For Each b In barras
            Dim area = Funciones_00_Varias.AreaRefuerzo(b)
            If area <= 0 Then Continue For
            If cara.AsReq > 0 Then
                Dim sep = Math.Floor(area * 100.0 / cara.AsReq / 2.5) * 2.5
                If sep < 7.5 Then Continue For
                If sep > 20 Then sep = 20
                mejorBarra = b
                mejorSep = sep
                Exit For
            End If
        Next
        cara.BarraPrincipal = mejorBarra
        cara.SeparacionCm = mejorSep
        cara.AsProv = Funciones_00_Varias.AreaRefuerzo(mejorBarra) * 100.0 / mejorSep
    End Sub

    ''' <summary>Recalcula AsProv de una cara después de que el usuario cambia barra o separación.</summary>
    Public Shared Sub RecalcularAsProvCara(cara As DisenoCaraCortina)
        If cara Is Nothing Then Return
        Dim area = Funciones_00_Varias.AreaRefuerzo(cara.BarraPrincipal)
        If cara.SeparacionCm > 0 Then
            cara.AsProv = area * 100.0 / cara.SeparacionCm
        End If
    End Sub

End Class
