Imports ARCO.Funciones_00_Varias
Imports ARCO.eNumeradores

' ═══════════════════════════════════════════════════════════════════════════════
'  NervioService — lógica de cálculo del Módulo 11 (Nervios / Losas nervadas)
'  NSR-10: C.13, C.8.10, C.22.5
' ═══════════════════════════════════════════════════════════════════════════════
Public Class NervioService

    Private ReadOnly _geo As GeometryService

    Public Sub New()
        _geo = New GeometryService()
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    '  Propiedades de secciones directamente desde los frames del modelo
    '  (b y h ya están cargados en cFrame.Section desde el import ETABS)
    ' ──────────────────────────────────────────────────────────────────────────
    Public Function ObtenerPropiedadesSecciones(frames As List(Of cFrame)) As Dictionary(Of String, PropSeccionNervio)
        Dim result As New Dictionary(Of String, PropSeccionNervio)(StringComparer.OrdinalIgnoreCase)
        For Each f In frames
            If f.Section Is Nothing Then Continue For
            Dim nombre = f.Section.Nombre
            If String.IsNullOrEmpty(nombre) OrElse result.ContainsKey(nombre) Then Continue For
            result(nombre) = New PropSeccionNervio With {
                .Nombre = nombre,
                .B = f.Section.b,
                .H = f.Section.h
            }
        Next
        Return result
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  IMPORT — BeamForces para nervios (misma hoja que vigas)
    ' ──────────────────────────────────────────────────────────────────────────
    Public Function ImportarBeamForcesNervios(rutaExcel As String,
                                               hojasDisponibles As List(Of String),
                                               seccionesNervio As HashSet(Of String),
                                               frames As List(Of cFrame)) _
                                               As List(Of cCombinacionBeamForce)

        Dim nombreHoja = ResolverNombreHoja(hojasDisponibles,
                                            "Element Forces - Beams",
                                            "Beam Forces")

        ' Reutiliza el lector robusto ya usado por el módulo de Vigas: mapea
        ' columnas por nombre (independiente del orden y de E17/E23) en vez de
        ' reimplementar el parseo con detección/índices frágiles.
        Dim posibleTruncamiento As Boolean = False
        Dim todas As List(Of cCombinacionBeamForce)
        Try
            todas = CargarBeamForcesDesdeExcel(rutaExcel, nombreHoja, posibleTruncamiento)
        Catch ex As Exception
            Logger.Error(ex, "NervioService.ImportarBeamForcesNervios")
            Return New List(Of cCombinacionBeamForce)()
        End Try

        ' Labels de nervios para filtrar
        Dim labelsNervios As New HashSet(Of String)(
            frames.Where(Function(f) f.Section IsNot Nothing AndAlso seccionesNervio.Contains(f.Section.Nombre)) _
                  .Select(Function(f) f.ObjectLabel),
            StringComparer.OrdinalIgnoreCase)

        If labelsNervios.Count = 0 Then Return todas

        Return todas.Where(Function(bf) labelsNervios.Contains(bf.Beam)).ToList()
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  AUTO-AGRUPACIÓN: frames de nervio → grupos colineales por piso
    ' ──────────────────────────────────────────────────────────────────────────
    Public Function GenerarNerviosAuto(framesNervio As List(Of cFrame),
                                        joints As Dictionary(Of String, cJoint)) As List(Of cNervio)

        Dim nervios As New List(Of cNervio)
        Dim pendientes As New HashSet(Of cFrame)(framesNervio)
        Dim tolColineal As Double = 0.01
        Dim tolContinuo As Double = 1.5

        Dim contador As Integer = 1

        While pendientes.Any()
            Dim fBase = pendientes.First()
            pendientes.Remove(fBase)

            Dim nervio As New cNervio With {
                .Nombre = $"NR-{contador}",
                .NombrePlano = $"NR-{contador}",
                .Piso = fBase.Story
            }
            contador += 1

            Dim dir = _geo.VectorFrame(fBase, joints)
            dir.Normalize()

            Dim agrego As Boolean
            Dim framesGrupo As New List(Of cFrame) From {fBase}

            Do
                agrego = False
                For Each f In pendientes.ToList()
                    If f.Story <> fBase.Story Then Continue For
                    If framesGrupo.Any(Function(fg)
                                           Return (_geo.CompartenJoint(fg, f) OrElse
                                                   GeometryService.SonContinuos(fg, f, joints, tolContinuo)) AndAlso
                                                   GeometryService.EstanEnLaMismaLinea(fg, f, joints, tolColineal)
                                       End Function) AndAlso
                       _geo.SonColineales(f, dir, joints, tolColineal) Then
                        framesGrupo.Add(f)
                        pendientes.Remove(f)
                        agrego = True
                    End If
                Next
            Loop While agrego

            ' Ordenar frames en dirección del nervio
            framesGrupo = framesGrupo.OrderBy(
                Function(f)
                    Dim pm = _geo.PuntoMedioFrame(f, joints)
                    Return Vector3.Dot(pm, dir)
                End Function).ToList()

            ' Convertir a cFrameNervio
            For Each f In framesGrupo
                Dim fn As New cFrameNervio()
                fn.ObjectLabel = f.ObjectLabel
                fn.ElementLabel = f.ElementLabel
                fn.Story = f.Story
                fn.JointI = f.JointI
                fn.JointJ = f.JointJ
                fn.NombreSeccion = f.Section?.Nombre
                fn.Bw = If(f.Section IsNot Nothing, f.Section.b, 0)
                fn.H = If(f.Section IsNot Nothing, f.Section.h, 0)
                fn.Longitud = _geo.Distancia_fj(f, joints)
                fn.fc = If(f.Section IsNot Nothing, f.Section.fc, 21)
                fn.fy = If(f.Section IsNot Nothing, f.Section.fy, 420)
                fn.Recubrimiento = If(f.Section IsNot Nothing, f.Section.recubrimiento, 0.04)
                nervio.Frames.Add(fn)
            Next

            nervios.Add(nervio)
        End While

        Return nervios
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  DETECCIÓN DE APOYOS: b_apoyo_I / b_apoyo_D por frame
    '  Busca frames NO-nervio que lleguen al joint; toma el más perpendicular
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub DetectarApoyos(nervios As List(Of cNervio),
                               todosLosFrames As List(Of cFrame),
                               joints As Dictionary(Of String, cJoint),
                               seccionesNervio As HashSet(Of String))

        ' Índice: joint → frames que llegan a ese joint
        Dim porJoint As New Dictionary(Of String, List(Of cFrame))(StringComparer.OrdinalIgnoreCase)
        For Each f In todosLosFrames
            For Each jt In {f.JointI, f.JointJ}
                If Not porJoint.ContainsKey(jt) Then porJoint(jt) = New List(Of cFrame)()
                porJoint(jt).Add(f)
            Next
        Next

        For Each nervio In nervios
            For i As Integer = 0 To nervio.Frames.Count - 1
                Dim fn = nervio.Frames(i)

                ' Dirección del tramo
                Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                If Not joints.TryGetValue(fn.JointI, ji) OrElse
                   Not joints.TryGetValue(fn.JointJ, jj) Then Continue For

                Dim dx = jj.GlobalX - ji.GlobalX
                Dim dy = jj.GlobalY - ji.GlobalY
                Dim lng = Math.Sqrt(dx * dx + dy * dy)
                If lng < 0.001 Then Continue For
                dx /= lng : dy /= lng

                fn.B_Apoyo_I = BuscarSemianchoApoyo(fn.JointI, dx, dy, porJoint, joints, seccionesNervio, fn.Frame_Apoyo_I)
                fn.B_Apoyo_D = BuscarSemianchoApoyo(fn.JointJ, dx, dy, porJoint, joints, seccionesNervio, fn.Frame_Apoyo_D)
            Next
        Next
    End Sub

    Private Function BuscarSemianchoApoyo(jointId As String,
                                           dxNervio As Double, dyNervio As Double,
                                           porJoint As Dictionary(Of String, List(Of cFrame)),
                                           joints As Dictionary(Of String, cJoint),
                                           seccionesNervio As HashSet(Of String),
                                           ByRef frameApoyo As String) As Double

        frameApoyo = ""
        If Not porJoint.ContainsKey(jointId) Then Return 0

        Dim jRef As cJoint = Nothing
        If Not joints.TryGetValue(jointId, jRef) Then Return 0

        Dim mejorFrame As cFrame = Nothing
        Dim mayorPerp As Double = 0

        For Each f In porJoint(jointId)
            If seccionesNervio.Contains(f.Section?.Nombre) Then Continue For

            Dim jOtro As cJoint = Nothing
            Dim otroJoint = If(f.JointI = jointId, f.JointJ, f.JointI)
            If Not joints.TryGetValue(otroJoint, jOtro) Then Continue For

            Dim ddx = jOtro.GlobalX - jRef.GlobalX
            Dim ddy = jOtro.GlobalY - jRef.GlobalY
            Dim dl = Math.Sqrt(ddx * ddx + ddy * ddy)
            If dl < 0.001 Then Continue For
            ddx /= dl : ddy /= dl

            Dim perp = Math.Abs(dxNervio * ddy - dyNervio * ddx)
            If perp > mayorPerp Then
                mayorPerp = perp
                mejorFrame = f
            End If
        Next

        If mejorFrame Is Nothing OrElse mayorPerp < 0.5 Then Return 0

        frameApoyo = mejorFrame.ObjectLabel

        If mejorFrame.Section IsNot Nothing AndAlso mejorFrame.Section.b > 0 Then
            Return mejorFrame.Section.b / 2.0
        End If

        Return 0
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  PASO AUTO: distancia mínima c-c entre nervios paralelos del mismo piso
    ' ──────────────────────────────────────────────────────────────────────────
    Public Function CalcularPasoNerviosAuto(nervio As cNervio,
                                             todosNervios As List(Of cNervio),
                                             joints As Dictionary(Of String, cJoint)) As Double

        If nervio.Frames.Count = 0 Then Return 0

        Dim fn0 = nervio.Frames(0)
        Dim ji As cJoint = Nothing, jj As cJoint = Nothing
        If Not joints.TryGetValue(fn0.JointI, ji) OrElse
           Not joints.TryGetValue(fn0.JointJ, jj) Then Return 0

        ' Dirección del nervio
        Dim dx = jj.GlobalX - ji.GlobalX
        Dim dy = jj.GlobalY - ji.GlobalY
        Dim lng = Math.Sqrt(dx * dx + dy * dy)
        If lng < 0.001 Then Return 0
        dx /= lng : dy /= lng

        ' Punto de referencia (primer joint del primer frame)
        Dim px = ji.GlobalX
        Dim py = ji.GlobalY

        Dim menorDist As Double = Double.MaxValue

        For Each otro In todosNervios
            If ReferenceEquals(otro, nervio) Then Continue For
            If otro.Piso <> nervio.Piso Then Continue For
            If otro.Frames.Count = 0 Then Continue For

            Dim fnOtro = otro.Frames(0)
            Dim joOtro As cJoint = Nothing
            If Not joints.TryGetValue(fnOtro.JointI, joOtro) Then Continue For

            ' Distancia perpendicular al eje del nervio de referencia
            Dim wx = joOtro.GlobalX - px
            Dim wy = joOtro.GlobalY - py
            Dim distPerp = Math.Abs(dx * wy - dy * wx)

            If distPerp > 0.05 AndAlso distPerp < menorDist Then
                menorDist = distPerp
            End If
        Next

        Return If(menorDist = Double.MaxValue, 0, menorDist)
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  EJES: asigna el eje estructural más cercano a cada apoyo de cada nervio
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub AsignarEjesANervios(nervios As List(Of cNervio),
                                    grids As List(Of cGridLine),
                                    joints As Dictionary(Of String, cJoint),
                                    Optional tolMax As Double = 0.5)
        _geo.AsignarEjesANervios(nervios, grids, joints, tolMax)
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    '  ANCHO EFECTIVO T — NSR-10 C.8.10.2 (ACI 318 Table 6.3.2.1)
    '  be = bw + 2 × min(8·hf, sw/2, ln/8)
    ' ──────────────────────────────────────────────────────────────────────────
    Public Function CalcularBe(bw As Double, tf As Double,
                                paso As Double, longitud As Double,
                                bApoyoI As Double, bApoyoD As Double) As Double

        Dim ln = longitud - bApoyoI - bApoyoD  ' luz libre
        If ln <= 0 Then ln = longitud

        Dim sw = paso - bw                      ' claro entre almas
        If sw <= 0 Then sw = 8 * tf             ' fallback sin dato de paso

        Dim vola = Math.Min(8 * tf, Math.Min(sw / 2.0, ln / 8.0))
        Return bw + 2.0 * vola
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  FUERZAS EN CARA DEL APOYO — interpolación lineal en la lista de estaciones
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub CalcularFuerzasCaraApoyo(combo As cComboNervio,
                                         bApoyoI As Double,
                                         bApoyoD As Double)

        If combo.Estaciones.Count < 2 Then Return

        Dim L = combo.Estaciones.Last()
        Dim xI = bApoyoI                    ' estación en cara izq
        Dim xD = L - bApoyoD               ' estación en cara der

        combo.M_Cara_I = InterpolateAt(combo.Estaciones, combo.Momentos, xI)
        combo.V_Cara_I = InterpolateAt(combo.Estaciones, combo.Cortantes, xI)
        combo.M_Cara_D = InterpolateAt(combo.Estaciones, combo.Momentos, xD)
        combo.V_Cara_D = InterpolateAt(combo.Estaciones, combo.Cortantes, xD)

        ' Momento positivo máximo en el vano (entre caras)
        Dim mPos As Double = 0
        For i As Integer = 0 To combo.Estaciones.Count - 1
            Dim x = combo.Estaciones(i)
            If x >= xI AndAlso x <= xD Then
                If combo.Momentos(i) > mPos Then mPos = combo.Momentos(i)
            End If
        Next
        combo.M_Max_Pos = mPos
    End Sub

    Private Shared Function InterpolateAt(estaciones As List(Of Double),
                                           valores As List(Of Double),
                                           x As Double) As Double
        If estaciones.Count = 0 Then Return 0
        If x <= estaciones(0) Then Return valores(0)
        If x >= estaciones(estaciones.Count - 1) Then Return valores(estaciones.Count - 1)

        For i As Integer = 0 To estaciones.Count - 2
            If x >= estaciones(i) AndAlso x <= estaciones(i + 1) Then
                Dim t = (x - estaciones(i)) / (estaciones(i + 1) - estaciones(i))
                Return valores(i) + t * (valores(i + 1) - valores(i))
            End If
        Next
        Return valores.Last()
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  ENVOLVENTES: poblar cComboNervio por frame y calcular demandas máximas
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub CalcularEnvolventesNervios(nervios As List(Of cNervio),
                                           beamForces As List(Of cCombinacionBeamForce),
                                           combosDiseno As HashSet(Of String))

        Dim lookup = beamForces _
            .Where(Function(r) combosDiseno.Contains(r.LoadCaseKey)) _
            .GroupBy(Function(r) (r.Beam, r.Story)) _
            .ToDictionary(Function(g) g.Key, Function(g) g.ToList())

        For Each nervio In nervios
            For Each fn In nervio.Frames
                fn.Combinaciones.Clear()

                Dim key = (fn.ObjectLabel, fn.Story)
                Dim bfFrame As List(Of cCombinacionBeamForce) = Nothing
                If Not lookup.TryGetValue(key, bfFrame) Then Continue For
                If bfFrame.Count = 0 Then Continue For

                ' Agrupar por combo
                Dim porCombo = bfFrame.GroupBy(Function(r) r.LoadCaseKey)

                For Each grp In porCombo
                    Dim combo As New cComboNervio With {.Nombre = grp.Key}

                    Dim ordered = grp.OrderBy(Function(r) r.Station).ToList()
                    For Each r In ordered
                        combo.Estaciones.Add(r.Station)
                        combo.Momentos.Add(r.M3)
                        combo.Cortantes.Add(r.V2)
                    Next

                    CalcularFuerzasCaraApoyo(combo, fn.B_Apoyo_I, fn.B_Apoyo_D)
                    fn.Combinaciones.Add(combo)
                Next

                ' Demandas críticas sobre todos los combos de diseño
                fn.Mu_Neg_I = fn.Combinaciones.Max(Function(c) Math.Abs(Math.Min(c.M_Cara_I, 0)))
                fn.Mu_Pos_C = fn.Combinaciones.Max(Function(c) c.M_Max_Pos)
                fn.Mu_Neg_D = fn.Combinaciones.Max(Function(c) Math.Abs(Math.Min(c.M_Cara_D, 0)))
                fn.Vu_I = fn.Combinaciones.Max(Function(c) Math.Abs(c.V_Cara_I))
                fn.Vu_D = fn.Combinaciones.Max(Function(c) Math.Abs(c.V_Cara_D))
            Next
        Next
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    '  DISEÑO A FLEXIÓN
    '  Rectangular o T (positivo en ala, negativo en alma)
    '  NSR-10 C.9.6.1.2: As_min = max(1.4/fy, 0.25√fc/fy) × bw × d
    '  NSR-10 C.9.7.3.4: As_max → c/d ≤ ε_u/(ε_u+ε_t_min) con ε_t_min=0.004
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub CalcularFlexion(fn As cFrameNervio)

        Dim phi_f As Double = 0.9
        Dim bw_mm = fn.Bw * 1000
        Dim h_mm = fn.H * 1000
        Dim rec_mm = fn.Recubrimiento * 1000
        Dim d_mm = h_mm - rec_mm

        If d_mm <= 0 OrElse bw_mm <= 0 Then Return

        ' Área provista por zona (mm²) — Superior en Izq/Der (apoyos), Inferior en Centro (vano)
        Dim asSupI = AreaZona(fn.RefuerzoSuperior, PosicionTramoViga.Izquierda)
        Dim asSupD = AreaZona(fn.RefuerzoSuperior, PosicionTramoViga.Derecha)
        Dim asInfC = AreaZona(fn.RefuerzoInferior, PosicionTramoViga.Centro)
        fn.As_Prov_Sup_I = asSupI / 100.0   ' cm²
        fn.As_Prov_Sup_D = asSupD / 100.0   ' cm²
        fn.As_Prov_Inf_C = asInfC / 100.0   ' cm²

        ' Mínimo y máximo
        Dim rhoMin = Math.Max(1.4 / fn.fy, 0.25 * Math.Sqrt(fn.fc) / fn.fy)
        fn.As_Min = rhoMin * bw_mm * d_mm / 100.0  ' cm²

        Dim eps_u As Double = 0.003
        Dim Es As Double = 200000  ' MPa
        ' c_max = ε_u / (ε_u + 0.004) × d  → As_max rectangular con bw
        Dim c_max = eps_u / (eps_u + 0.004) * d_mm
        Dim a_max = 0.85 * c_max  ' β1≈0.85 para fc≤28MPa
        fn.As_Max = 0.85 * fn.fc * a_max * bw_mm / fn.fy / 100.0  ' cm²

        ' φMn superior por zona (negativo, alma rectangular bw)
        fn.PhiMn_Sup_I = CapacidadFlexion(asSupI, fn.fy, fn.fc, bw_mm, d_mm, phi_f)
        fn.PhiMn_Sup_D = CapacidadFlexion(asSupD, fn.fy, fn.fc, bw_mm, d_mm, phi_f)

        ' φMn inferior en Centro (positivo)
        If fn.EsSeccionT AndAlso fn.Be > 0 Then
            Dim be_mm = fn.Be * 1000
            Dim tf_mm = fn.Tf * 1000
            fn.PhiMn_Inf_C = CapacidadFlexionT(asInfC, fn.fy, fn.fc, be_mm, bw_mm, tf_mm, d_mm, phi_f)
        Else
            fn.PhiMn_Inf_C = CapacidadFlexion(asInfC, fn.fy, fn.fc, bw_mm, d_mm, phi_f)
        End If

        ' C/D por zona (C/D ≥ 1 → cumple) — cada zona con su propia capacidad
        fn.CD_Flex_Sup_I = If(fn.Mu_Neg_I > 0.001, fn.PhiMn_Sup_I / fn.Mu_Neg_I, 99.0)
        fn.CD_Flex_Inf_C = If(fn.Mu_Pos_C > 0.001, fn.PhiMn_Inf_C / fn.Mu_Pos_C, 99.0)
        fn.CD_Flex_Sup_D = If(fn.Mu_Neg_D > 0.001, fn.PhiMn_Sup_D / fn.Mu_Neg_D, 99.0)

        ' Verificación de cuantía mínima (como restricción adicional)
        ' Si As_prov < As_min, el C/D se penaliza a (As_prov/As_min)×C/D
        If asSupI < fn.As_Min * 100 AndAlso fn.Mu_Neg_I > 0.001 Then
            fn.CD_Flex_Sup_I = Math.Min(fn.CD_Flex_Sup_I, (asSupI / 100.0) / fn.As_Min)
        End If
        If asInfC < fn.As_Min * 100 AndAlso fn.Mu_Pos_C > 0.001 Then
            fn.CD_Flex_Inf_C = Math.Min(fn.CD_Flex_Inf_C, (asInfC / 100.0) / fn.As_Min)
        End If
        If asSupD < fn.As_Min * 100 AndAlso fn.Mu_Neg_D > 0.001 Then
            fn.CD_Flex_Sup_D = Math.Min(fn.CD_Flex_Sup_D, (asSupD / 100.0) / fn.As_Min)
        End If
    End Sub

    ''' <summary>Suma el área de acero (mm²) de todos los calibres de la zona indicada.</summary>
    Private Shared Function AreaZona(lista As List(Of cRefuerzoTramo), posicion As PosicionTramoViga) As Double
        If lista Is Nothing Then Return 0
        Dim tramo = lista.FirstOrDefault(Function(t) t.Posicion = posicion)
        If tramo Is Nothing OrElse tramo.Barras Is Nothing Then Return 0
        Return tramo.Barras.Sum(Function(kvp) kvp.Value * AreaRefuerzo(kvp.Key))
    End Function

    ' φMn sección rectangular (kN·m)
    Private Shared Function CapacidadFlexion(As_mm2 As Double, fy As Double, fc As Double,
                                              b_mm As Double, d_mm As Double,
                                              phi As Double) As Double
        If As_mm2 <= 0 OrElse b_mm <= 0 OrElse d_mm <= 0 Then Return 0
        Dim a = As_mm2 * fy / (0.85 * fc * b_mm)
        Return phi * As_mm2 * fy * (d_mm - a / 2.0) / 1.0e6  ' kN·m
    End Function

    ' φMn sección T (positivo, eje neutro puede estar en ala o alma) (kN·m)
    Private Shared Function CapacidadFlexionT(As_mm2 As Double, fy As Double, fc As Double,
                                               be_mm As Double, bw_mm As Double, tf_mm As Double,
                                               d_mm As Double, phi As Double) As Double
        If As_mm2 <= 0 Then Return 0
        ' Verificar si eje neutro queda en el ala
        Dim a_ala = As_mm2 * fy / (0.85 * fc * be_mm)
        If a_ala <= tf_mm Then
            ' Rectangular con be
            Return phi * As_mm2 * fy * (d_mm - a_ala / 2.0) / 1.0e6
        End If
        ' Eje neutro en el alma — método de dos bloques
        Dim Cf = 0.85 * fc * (be_mm - bw_mm) * tf_mm        ' fuerza compresión ala
        Dim Asw = As_mm2 - Cf / fy                           ' acero para el alma
        If Asw <= 0 Then Return phi * As_mm2 * fy * (d_mm - tf_mm / 2.0) / 1.0e6
        Dim a_alma = Asw * fy / (0.85 * fc * bw_mm)
        Dim Mn_alma = Asw * fy * (d_mm - a_alma / 2.0)
        Dim Mn_ala = Cf * (d_mm - tf_mm / 2.0)
        Return phi * (Mn_alma + Mn_ala) / 1.0e6
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  VERIFICACIÓN A CORTANTE
    '  NSR-10 C.22.5.5.1: Vc = 0.17 × λ × √fc × bw × d
    '  NSR-10 C.13.3.5:  Vc × 1.1 si paso ≤ 750 mm
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub CalcularCortante(fn As cFrameNervio)

        Dim phi_v As Double = 0.75
        Dim bw_mm = fn.Bw * 1000
        Dim d_mm = (fn.H - fn.Recubrimiento) * 1000

        If bw_mm <= 0 OrElse d_mm <= 0 Then Return

        ' Factor nervio angosto NSR-10 C.13.3.5
        Dim factorNervio As Double = If(fn.Paso * 1000 <= 750 AndAlso fn.Paso > 0, 1.1, 1.0)

        Dim Vc = factorNervio * 0.17 * Math.Sqrt(fn.fc) * bw_mm * d_mm / 1000.0  ' kN

        ' Estribos por zona: Izquierda protege la cara izq, Derecha la cara der
        fn.PhiVn_I = phi_v * (Vc + VsZona(fn.RefuerzoTransversal, PosicionTramoViga.Izquierda, fn.fy, d_mm))
        fn.PhiVn_D = phi_v * (Vc + VsZona(fn.RefuerzoTransversal, PosicionTramoViga.Derecha, fn.fy, d_mm))

        fn.CD_Cortante_I = If(fn.Vu_I > 0.001, fn.PhiVn_I / fn.Vu_I, 99.0)
        fn.CD_Cortante_D = If(fn.Vu_D > 0.001, fn.PhiVn_D / fn.Vu_D, 99.0)
    End Sub

    ''' <summary>Vs (kN) aportado por los estribos de la zona indicada. Cero si no hay entrada para esa zona.</summary>
    Private Shared Function VsZona(lista As List(Of cRefuerzoTransversalZona), posicion As PosicionTramoViga,
                                    fy As Double, d_mm As Double) As Double
        If lista Is Nothing Then Return 0
        Dim zona = lista.FirstOrDefault(Function(z) z.Posicion = posicion)
        If zona Is Nothing OrElse zona.Separacion <= 0 OrElse zona.CantEstribos <= 0 Then Return 0
        Dim Av = zona.CantEstribos * AreaRefuerzo("#" & zona.NumeroBarra)  ' mm²
        Return Av * fy * d_mm / (zona.Separacion * 1000.0) / 1000.0  ' kN
    End Function

    ' ──────────────────────────────────────────────────────────────────────────
    '  DISEÑO COMPLETO de todos los nervios (llama flexión + cortante + be)
    ' ──────────────────────────────────────────────────────────────────────────
    Public Sub DesignarNervios(nervios As List(Of cNervio),
                                joints As Dictionary(Of String, cJoint))

        For Each nervio In nervios
            ' Paso: usar el del grupo o auto-calculado
            Dim paso = nervio.Paso_Nervios

            For Each fn In nervio.Frames
                fn.Tf = nervio.Tf_Losa
                fn.Paso = paso

                ' Calcular be si es T
                If fn.EsSeccionT Then
                    fn.Be = CalcularBe(fn.Bw, fn.Tf, fn.Paso, fn.Longitud, fn.B_Apoyo_I, fn.B_Apoyo_D)
                End If

                CalcularFlexion(fn)
                CalcularCortante(fn)

                ' Cumple global: todas las zonas ≥ 0.9 (misma regla que vigas/muros)
                fn.Cumple = fn.CD_Flex_Sup_I >= 0.9 AndAlso
                            fn.CD_Flex_Inf_C >= 0.9 AndAlso
                            fn.CD_Flex_Sup_D >= 0.9 AndAlso
                            fn.CD_Cortante_I >= 0.9 AndAlso
                            fn.CD_Cortante_D >= 0.9
            Next
        Next
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    '  UTILIDADES
    ' ──────────────────────────────────────────────────────────────────────────
    Private Shared Function IndexOfColumn(dt As DataTable, ParamArray nombres As String()) As Integer
        For Each nombre In nombres
            For i As Integer = 0 To dt.Columns.Count - 1
                If dt.Columns(i).ColumnName.Trim().Equals(nombre, StringComparison.OrdinalIgnoreCase) Then
                    Return i
                End If
            Next
        Next
        Return -1
    End Function

    Private Shared Function ParseD(obj As Object) As Double
        If obj Is Nothing OrElse obj Is DBNull.Value Then Return 0
        Dim d As Double
        Double.TryParse(obj.ToString(), Globalization.NumberStyles.Any,
                        Globalization.CultureInfo.InvariantCulture, d)
        Return d
    End Function

End Class
