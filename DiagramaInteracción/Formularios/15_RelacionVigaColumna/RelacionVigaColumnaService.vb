Imports ARCO.eNumeradores

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 15 — Relación Viga-Columna (NSR-10 C.21.6.2)
'
'  "Columna fuerte / viga débil". En cada nudo beam-column y en las dos
'  direcciones principales de la columna (LL = paralela al lado largo, LC =
'  paralela al lado corto), la suma de capacidades a flexión de las columnas
'  que llegan al nudo (arriba + abajo) debe exceder la suma de capacidades de
'  las vigas que entran al nudo, por el factor 6/5:
'
'       Σ Mnc  ≥  (6/5) · Σ Mnv              (DES, NSR-10 C.21.6.2.2)
'       Σ Mnc  ≥          Σ Mnv              (DMO, informativo)
'
'  Convenciones usadas:
'
'  1) **Direcciones LL / LC** (locales a la columna del nudo). El ángulo α
'     (`AnguloOrientacion`) define la rotación del eje local 2 respecto al eje
'     global X, antihorario. Para una columna rectangular (convención ARCO/ETABS):
'
'         Lado largo (LL) ∥ eje local 2   ∥ (cos α, sin α)
'         Lado corto (LC) ∥ eje local 3   ∥ (−sin α, cos α)
'
'     Cuando sismo empuja en LL, la columna flexa alrededor del eje 3 (eje
'     perpendicular a LL) → Mn resistente = Mn3 = eje FUERTE. Cuando empuja en
'     LC → Mn2 = eje débil.
'
'     Si el nudo tiene dos columnas (arriba y abajo) con orientaciones distintas
'     (caso raro), se usa la orientación de la columna de ARRIBA como
'     referencia. El tramo de abajo proyecta sus Mn3/Mn2 al sistema LL/LC del
'     nudo mediante la fórmula cuadrática:
'
'            Mnc_LL = √( (Mn3·cos Δα)² + (Mn2·sin Δα)² )
'            Mnc_LC = √( (Mn3·sin Δα)² + (Mn2·cos Δα)² )
'
'     con Δα = α_tramo − α_nudo (rad). Caso normal (Δα = 0): Mnc_LL = Mn3,
'     Mnc_LC = Mn2 directamente, sin proyección.
'
'  2) **Mnc por columna** se interpola del diagrama de interacción nominal
'     (Lista_DI_Pn_M3 / Lista_DI_Mn_M3, sin φ) en el Pu axial gobernante que
'     ARCO guarda como `Tramo_Columna.Pu_Gob_DI`. ¡Convención crítica!: ETABS
'     reporta P con compresión NEGATIVA, el DI usa compresión POSITIVA. El Pu
'     se invierte en `PuDiseno` para mapear al extremo correcto del DI.
'
'  3) **Mnv por viga** se calcula con la fórmula cerrada NSR-10 C.10.2 bloque
'     rectangular ACI, φ = 1, fy = 420 MPa nominal. La sección (b, h, d) viene
'     del `cFrame.Section`. As provisto superior/inferior se arma sumando las
'     barras del `cFrame.RefuerzoSuperior`/`RefuerzoInferior` de la zona
'     Izquierda o Derecha según en qué extremo del frame cae el nudo.
'
'  4) **Mn de viga en el chequeo** = max(Mn⁻, Mn⁺). ACI 318-19 §18.7.3 /
'     NSR-10 C.21.6.2: cada viga aporta UN momento al nudo para una dirección
'     de sismo (Mn⁻ o Mn⁺ según el lado del nudo), no la suma de los dos.
'
'  5) **Proyección de vigas** al sistema LL/LC del nudo:
'
'         viga a ángulo θ_global con respecto al eje X global
'         Δθ = θ − α_nudo              (ángulo respecto al LL del nudo)
'         proy_LL = |cos Δθ|            viga paralela a LL → aporta todo
'         proy_LC = |sin Δθ|            viga perpendicular a LL → aporta todo a LC
'
'     Umbral mínimo 0.05: vigas dentro de ±3° del eje perpendicular no aportan
'     (evita ratios absurdos por ruido de punto flotante).
'
'  6) **Umbrales de cumplimiento**: 1.2 para DES, 1.0 para DMO. Si Σ Mnv ≤ 0
'     el ratio queda en 0 ("no evaluable") — misma convención que el resto de
'     ARCO (C/D = 0 significa "sin dato").
'
'  7) **Reconciliación de joints**: GeometriaPlantasService puebla
'     Columna.Coordenadas.X/Y. El Z se obtiene por moda de los Joints por Story.
'     Vigas resuelven JointI/JointJ por ElementLabel (no ObjectLabel). Nodos
'     Pier se detectan por punto-en-footprint rotado (no por coord exacta).
' ═══════════════════════════════════════════════════════════════════════════════

Public Enum eDirColumna
    LL = 0   ' paralela al lado largo (eje 2 local de la columna de referencia)
    LC = 1   ' paralela al lado corto (eje 3 local)
End Enum

''' <summary>Una viga que entra a un nudo, con todas las variables usadas en el
''' cálculo: sección, aceros provistos en el extremo que llega, Mn⁻/Mn⁺, ángulo
''' con respecto al eje X global, y las proyecciones a X e Y. Pensado para que
''' la UI pueda mostrar la traza completa.</summary>
Public Class AporteVigaRVC
    Public Property Viga As cViga
    Public Property Frame As cFrame
    ''' <summary>True si el nudo coincide con JointI del frame; false si JointJ.</summary>
    Public Property EsExtremoI As Boolean

    ''' <summary>Ángulo de la viga respecto al eje X global, en radianes (saliente del nudo).</summary>
    Public Property AnguloRad As Double

    ' Sección en el extremo que llega al nudo
    Public Property B_m As Double
    Public Property H_m As Double
    Public Property D_m As Double          ' h - recubrimiento (brazo interno)
    Public Property Fc_MPa As Double
    Public Property Fy_MPa As Double

    ' Aceros provistos en el extremo que llega al nudo (sumados sobre cRefuerzoTramo).
    Public Property As_Sup_mm2 As Double
    Public Property As_Inf_mm2 As Double

    ' Capacidades resultantes (φ = 1, bloque rectangular ACI, kN·m)
    Public Property MnNeg As Double
    Public Property MnPos As Double

    Public ReadOnly Property Nombre As String
        Get
            If Viga Is Nothing Then Return ""
            If Not String.IsNullOrWhiteSpace(Viga.NombrePlano) Then Return Viga.NombrePlano
            Return If(Viga.Nombre, "")
        End Get
    End Property

    Public ReadOnly Property AnguloDeg As Double
        Get
            Return AnguloRad * 180.0 / Math.PI
        End Get
    End Property

    ''' <summary>Capacidad total del extremo (Mn⁻ + Mn⁺) — magnitud que entra al nudo,
    ''' útil como info descriptiva. NO se usa en el chequeo: ver MnChequeo.</summary>
    Public ReadOnly Property MnTotal As Double
        Get
            Return MnNeg + MnPos
        End Get
    End Property

    ''' <summary>Mn que entra en la suma ΣMnv del chequeo NSR-10 C.21.6.2.
    ''' Simplificación conservadora común: max(Mn⁻, Mn⁺) por viga. Esto refleja
    ''' que en un sismo dado cada viga aporta UN momento al nudo (Mn⁻ ó Mn⁺
    ''' según el lado del nudo), no la suma de los dos. Ver ACI 318-19 §18.7.3
    ''' comentario R18.7.3 y Paulay & Priestley.</summary>
    Public ReadOnly Property MnChequeo As Double
        Get
            Return Math.Max(MnNeg, MnPos)
        End Get
    End Property

    ''' <summary>Ángulo del eje LL del nudo (en global, radianes). Lo setea
    ''' RelacionVigaColumnaService al asociar la viga al nudo; usado por las
    ''' proyecciones LL/LC.</summary>
    Public Property AnguloLLNudoRad As Double

    ''' <summary>Proyección al eje LL del nudo: |cos(θ − α_nudo)|.</summary>
    Public ReadOnly Property ProyeccionLL As Double
        Get
            Return Math.Abs(Math.Cos(AnguloRad - AnguloLLNudoRad))
        End Get
    End Property

    ''' <summary>Proyección al eje LC del nudo: |sen(θ − α_nudo)|.</summary>
    Public ReadOnly Property ProyeccionLC As Double
        Get
            Return Math.Abs(Math.Sin(AnguloRad - AnguloLLNudoRad))
        End Get
    End Property

    ''' <summary>Aporte efectivo a LL = MnChequeo · ProyeccionLL.</summary>
    Public ReadOnly Property ContribucionLL As Double
        Get
            Return MnChequeo * ProyeccionLL
        End Get
    End Property

    ''' <summary>Aporte efectivo a LC.</summary>
    Public ReadOnly Property ContribucionLC As Double
        Get
            Return MnChequeo * ProyeccionLC
        End Get
    End Property
End Class

''' <summary>Un tramo de columna que converge al nudo (arriba o abajo), con el
''' Pu axial usado para interpolar y los Mn locales 2/3 resultantes, ya
''' proyectados al sistema LL/LC del nudo.</summary>
Public Class AporteColumnaRVC
    Public Property Tramo As Tramo_Columna
    ''' <summary>"arriba" si el tramo sale del nodo hacia arriba, "abajo" si llega al nodo desde abajo.</summary>
    Public Property Posicion As String

    ' Material + sección (del tramo)
    Public Property B_Plano As Single
    Public Property H_Plano As Single
    Public Property AnguloDeg As Single
    Public Property Fc As Single

    ' Carga axial y DI nominal (en convención diagrama: compresión positiva)
    Public Property Pu As Double
    Public Property Mn3 As Double          ' alrededor del eje local 3 (kN·m)
    Public Property Mn2 As Double          ' alrededor del eje local 2 (kN·m)

    ' Diferencia angular entre la orientación del tramo y el eje LL del nudo (rad)
    Public Property DeltaAlphaRad As Double

    ' Mnc proyectado al sistema LL/LC del nudo. Cuando DeltaAlpha ≈ 0 (caso
    ' normal, columna arriba y abajo con misma orientación): MncLL = Mn3,
    ' MncLC = Mn2 directamente.
    Public Property MncLL As Double
    Public Property MncLC As Double

    Public ReadOnly Property Nombre As String
        Get
            If Tramo Is Nothing Then Return ""
            Return If(Tramo.Name_Elemento, "")
        End Get
    End Property

    Public ReadOnly Property Piso As String
        Get
            If Tramo Is Nothing Then Return ""
            Return If(Tramo.Piso, "")
        End Get
    End Property
End Class

''' <summary>Capacidades proyectadas y verdictos en una dirección local del
''' nudo (LL o LC). Expone las listas estructuradas de aportantes para que la
''' UI pueda mostrar el desglose línea por línea.</summary>
Public Class ResultadoRVCDireccion
    Public Property Direccion As eDirColumna
    Public Property SumMnc As Double        ' kN·m
    Public Property SumMnv As Double        ' kN·m

    ''' <summary>Aportes columna en esta dirección (típicamente 1 arriba + 1 abajo).</summary>
    Public Property AportesColumna As New List(Of AporteColumnaRVC)

    ''' <summary>Aportes viga proyectados en esta dirección (solo los que contribuyen > 0).</summary>
    Public Property AportesViga As New List(Of AporteVigaRVC)

    Public ReadOnly Property Ratio As Double
        Get
            If SumMnv <= 0 Then Return 0
            Return SumMnc / SumMnv
        End Get
    End Property

    Public ReadOnly Property CumpleDES As Boolean
        Get
            Return Ratio >= RelacionVigaColumnaService.RATIO_DES
        End Get
    End Property
    Public ReadOnly Property CumpleDMO As Boolean
        Get
            Return Ratio >= RelacionVigaColumnaService.RATIO_DMO
        End Get
    End Property
End Class

''' <summary>Un nudo evaluable (hay al menos una columna y una viga llegando).</summary>
Public Class NodoRVC
    Public Property Label As String
    Public Property Piso As String
    Public Property X As Double
    Public Property Y As Double
    Public Property Z As Double

    Public Property ColumnaArriba As Tramo_Columna
    Public Property ColumnaAbajo As Tramo_Columna
    Public Property Vigas As New List(Of AporteVigaRVC)

    ''' <summary>Ángulo (grados, antihorario desde X global) del eje LL del nudo,
    ''' tomado de la columna de referencia (arriba si existe, sino abajo). LL es
    ''' paralelo al lado largo de esa columna (= eje local 2).</summary>
    Public Property AnguloRefDeg As Double

    Public Property DireccionLL As ResultadoRVCDireccion
    Public Property DireccionLC As ResultadoRVCDireccion

    ''' <summary>True si cada dirección CON DATO cumple DES (ratio ≥ 1.2).
    ''' Direcciones sin dato (Ratio = 0 → no hay vigas con proyección > 0.05)
    ''' NO cuentan contra el verdict. Nudo con ambas direcciones sin dato =>
    ''' False (no hay chequeo real).</summary>
    Public ReadOnly Property CumpleDES As Boolean
        Get
            If DireccionLL.Ratio <= 0 AndAlso DireccionLC.Ratio <= 0 Then Return False
            Dim ll = DireccionLL.Ratio <= 0 OrElse DireccionLL.CumpleDES
            Dim lc = DireccionLC.Ratio <= 0 OrElse DireccionLC.CumpleDES
            Return ll AndAlso lc
        End Get
    End Property

    Public ReadOnly Property CumpleSoloDMO As Boolean
        Get
            If DireccionLL.Ratio <= 0 AndAlso DireccionLC.Ratio <= 0 Then Return False
            Dim ll = DireccionLL.Ratio <= 0 OrElse DireccionLL.CumpleDMO
            Dim lc = DireccionLC.Ratio <= 0 OrElse DireccionLC.CumpleDMO
            Return ll AndAlso lc AndAlso Not CumpleDES
        End Get
    End Property

    Public ReadOnly Property PeorRatio As Double
        Get
            Dim rLL = If(DireccionLL.Ratio > 0, DireccionLL.Ratio, Double.MaxValue)
            Dim rLC = If(DireccionLC.Ratio > 0, DireccionLC.Ratio, Double.MaxValue)
            Dim m = Math.Min(rLL, rLC)
            Return If(m = Double.MaxValue, 0, m)
        End Get
    End Property
End Class

Public Class RelacionVigaColumnaService

    ''' <summary>Factor de cumplimiento NSR-10 C.21.6.2.2 (DES): 6/5.</summary>
    Public Const RATIO_DES As Double = 1.2
    ''' <summary>Umbral informativo para DMO (no normativo).</summary>
    Public Const RATIO_DMO As Double = 1.0

    ' Tolerancia de coincidencia de coordenadas (m) al agrupar nudos.
    Private Const TOL_COORD As Double = 0.01

    ' =========================================================================
    '  Validación previa
    ' =========================================================================
    Public Shared Function PuedeEvaluar(proyecto As Proyecto) As (OK As Boolean, Mensaje As String)
        If proyecto Is Nothing Then Return (False, "No hay proyecto cargado.")

        Dim cols = If(proyecto.Elementos?.Columnas?.Lista_Columnas, New List(Of Columna))
        If cols.Count = 0 Then
            Return (False, "Importá las columnas antes de evaluar la relación viga-columna." & vbCrLf & vbCrLf &
                           "Hojas ETABS requeridas: Objects and Elements - Joints / Frames, Frame Assigns - Sect Prop, " &
                           "Element Forces - Columns (o Column Forces en E17).")
        End If

        If cols.All(Function(c) c.Lista_Tramos_Columnas Is Nothing OrElse c.Lista_Tramos_Columnas.All(
                        Function(t) t.Lista_DI_Mn_M3 Is Nothing OrElse t.Lista_DI_Mn_M3.Count < 2)) Then
            Return (False, "Ninguna columna tiene su diagrama de interacción calculado. " &
                           "Entrá al módulo de Columnas, ingresá el refuerzo longitudinal y guardá — " &
                           "ARCO calcula el DI automáticamente al guardar.")
        End If

        Dim vigas = If(proyecto.Elementos?.Vigas?.Vigas, New List(Of cViga))
        If vigas.Count = 0 Then
            Return (False, "Importá las vigas antes de evaluar la relación viga-columna." & vbCrLf & vbCrLf &
                           "Hojas ETABS requeridas: Objects and Elements - Frames, Frame Assigns - Sect Prop, " &
                           "Element Forces - Beams (o Beam Forces en E17).")
        End If

        Dim conRef = vigas.Where(Function(v) v.Frames IsNot Nothing AndAlso v.Frames.Any(AddressOf VigaService.TieneRefuerzoFlexion)).Count()
        If conRef = 0 Then
            Return (False, "Ninguna viga tiene refuerzo a flexión. " &
                           "Entrá al módulo de Vigas y asigná refuerzo superior/inferior en los apoyos antes de evaluar.")
        End If

        Dim joints = If(proyecto.Elementos?.Joints, New List(Of cJoint))
        If joints.Count = 0 Then
            Return (False, "No hay joints en el proyecto. " &
                           "El módulo necesita las coordenadas de los nodos para cruzar columnas y vigas.")
        End If

        Return (True, "")
    End Function

    ' =========================================================================
    '  Entrada pública: construye todos los nudos beam-column y evalúa.
    ' =========================================================================
    Public Shared Function Evaluar(proyecto As Proyecto) As List(Of NodoRVC)
        Dim nodos = ConstruirNodos(proyecto)
        For Each n In nodos
            EvaluarNodo(n)
        Next
        Return nodos
    End Function

    ' =========================================================================
    '  1) Construcción de nudos: agrupa coordenadas tramos+joints de vigas
    '
    '  Dos obstáculos de ARCO que resolvemos acá:
    '    (a) Tramo_Columna.Coordenadas es Nothing en todos los .esm — nunca se
    '        pobló en el flujo de importación. Ubicamos X,Y vía GeometriaPlantasService
    '        (que setea Columna.Coordenadas.X/Y a partir de Joints+Frames) y Z vía
    '        un mapa Story→CoorZ construido desde los Joints globales.
    '    (b) Los frames de VIGAS traen JointI/JointJ como ELEMENT_LABEL (número
    '        de Element Name en ETABS), NO como Object Label — misma distinción
    '        que CLAUDE.md anota para pilas/zapatas vs vigas/nervios. Construimos
    '        el índice por ElementLabel con fallback a ObjectLabel.
    ' =========================================================================
    Private Shared Function ConstruirNodos(proyecto As Proyecto) As List(Of NodoRVC)
        Dim mapa As New Dictionary(Of String, NodoRVC)

        ' --- Resolver X/Y de columnas (idempotente) -----------------------------
        GeometriaPlantasService.ResolverYValidar(proyecto)

        ' --- Mapa Story → Z usando los Joints globales ---------------------------
        ' Para cada Story, tomamos la moda de GlobalZ (el valor más común entre
        ' todos los joints con ese Story). Protege contra joints desplazados.
        Dim joints = If(proyecto.Elementos?.Joints, New List(Of cJoint))
        Dim storyToZ As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        For Each grp In joints.Where(Function(j) j IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(j.Story)) _
                              .GroupBy(Function(j) j.Story.Trim())
            Dim modaZ = grp.GroupBy(Function(j) Math.Round(j.GlobalZ, 2)) _
                           .OrderByDescending(Function(g) g.Count()) _
                           .First().Key
            storyToZ(grp.Key) = modaZ
        Next

        ' --- Lista de pisos ordenados por Z para resolver "piso previo" ---------
        Dim pisosOrdenados = storyToZ.OrderBy(Function(kv) kv.Value).Select(Function(kv) kv.Key).ToList()

        ' --- Índice de joints por label (ElementLabel primero, ObjectLabel fallback) ---
        Dim idxByElem As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
        Dim idxByObj As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
        For Each j In joints.Where(Function(x) x IsNot Nothing)
            If Not String.IsNullOrWhiteSpace(j.ElementLabel) AndAlso Not idxByElem.ContainsKey(j.ElementLabel) Then
                idxByElem(j.ElementLabel) = j
            End If
            If Not String.IsNullOrWhiteSpace(j.ObjectLabel) AndAlso Not idxByObj.ContainsKey(j.ObjectLabel) Then
                idxByObj(j.ObjectLabel) = j
            End If
        Next

        ' --- Sembrar nodos con los extremos de cada tramo de columna ------------
        For Each col In proyecto.Elementos.Columnas.Lista_Columnas
            If col.Lista_Tramos_Columnas Is Nothing OrElse col.Coordenadas Is Nothing Then Continue For
            Dim cx = CDbl(col.Coordenadas.X), cy = CDbl(col.Coordenadas.Y)
            If cx = 0 AndAlso cy = 0 Then Continue For   ' Sin X,Y → sin posibilidad de match

            For Each tr In col.Lista_Tramos_Columnas
                If tr Is Nothing OrElse String.IsNullOrWhiteSpace(tr.Piso) Then Continue For
                Dim zTop As Double
                If Not storyToZ.TryGetValue(tr.Piso.Trim(), zTop) Then Continue For

                ' Piso previo: el inmediatamente inferior a zTop.
                Dim idxPiso = pisosOrdenados.IndexOf(tr.Piso.Trim())
                Dim zBase As Double = 0
                If idxPiso > 0 Then zBase = storyToZ(pisosOrdenados(idxPiso - 1))

                ' Nudo tope (Zf = zTop): el tramo llega "desde abajo" → ColumnaAbajo.
                Dim nTop = GetOrAddNodo(mapa, cx, cy, zTop, tr.Piso)
                If nTop.ColumnaAbajo Is Nothing Then nTop.ColumnaAbajo = tr

                ' Nudo base (Zi = zBase): el tramo "sale hacia arriba" → ColumnaArriba.
                ' Para tramos del primer piso (idxPiso = 0) no sembramos nudo base;
                ' no hay columna debajo y las bases de cimentación no se chequean.
                If idxPiso > 0 Then
                    Dim nBase = GetOrAddNodo(mapa, cx, cy, zBase, pisosOrdenados(idxPiso - 1))
                    If nBase.ColumnaArriba Is Nothing Then nBase.ColumnaArriba = tr
                End If
            Next
        Next

        ' --- Asociar vigas ------------------------------------------------------
        ' El match NO puede ser por coordenada exacta porque las columnas Pier son
        ' muros-columna de 1-3 m de lado, y las vigas aterrizan en los bordes del
        ' pier (no en el centroide). Usamos "punto-en-footprint" del tramo que
        ' llega al nodo (B_Plano × H_Plano rotado por AnguloOrientacion). Para
        ' columnas Frame regulares (0.4×0.4) el centroide y los bordes coinciden
        ' con la tolerancia habitual.
        '
        ' Agrupamos los nodos por Z (rounded a 1 cm) para acotar la búsqueda:
        ' cada joint de viga solo se compara contra los nodos de su altura.
        Dim nodosPorZ As New Dictionary(Of Double, List(Of NodoRVC))
        For Each kv In mapa.Values
            Dim kz = Math.Round(kv.Z, 2)
            Dim lst As List(Of NodoRVC) = Nothing
            If Not nodosPorZ.TryGetValue(kz, lst) Then
                lst = New List(Of NodoRVC) : nodosPorZ(kz) = lst
            End If
            lst.Add(kv)
        Next

        For Each v In proyecto.Elementos.Vigas.Vigas
            If v.Frames Is Nothing OrElse Not v.IncluirEnRevision Then Continue For
            If Not v.Frames.Any(AddressOf VigaService.TieneRefuerzoFlexion) Then Continue For

            For Each fr In v.Frames
                If fr Is Nothing OrElse Not VigaService.TieneRefuerzoFlexion(fr) Then Continue For

                Dim jI = ResolverJoint(idxByElem, idxByObj, fr.JointI)
                Dim jJ = ResolverJoint(idxByElem, idxByObj, fr.JointJ)
                If jI Is Nothing OrElse jJ Is Nothing Then Continue For

                Dim nI = BuscarNodoParaViga(nodosPorZ, jI.GlobalX, jI.GlobalY, jI.GlobalZ)
                Dim nJ = BuscarNodoParaViga(nodosPorZ, jJ.GlobalX, jJ.GlobalY, jJ.GlobalZ)

                If nI IsNot Nothing Then
                    Dim ang = Math.Atan2(jJ.GlobalY - jI.GlobalY, jJ.GlobalX - jI.GlobalX)
                    Dim ap = CalcularMnViga(fr, esExtremoI:=True)
                    ap.AnguloRad = ang : ap.Viga = v : ap.Frame = fr : ap.EsExtremoI = True
                    nI.Vigas.Add(ap)
                End If

                If nJ IsNot Nothing Then
                    Dim ang = Math.Atan2(jI.GlobalY - jJ.GlobalY, jI.GlobalX - jJ.GlobalX)
                    Dim ap = CalcularMnViga(fr, esExtremoI:=False)
                    ap.AnguloRad = ang : ap.Viga = v : ap.Frame = fr : ap.EsExtremoI = False
                    nJ.Vigas.Add(ap)
                End If
            Next
        Next

        ' Filtrar nodos que no son beam-column (al menos una columna y una viga).
        Return mapa.Values.Where(Function(n)
                                     Return (n.ColumnaArriba IsNot Nothing OrElse n.ColumnaAbajo IsNot Nothing) AndAlso
                                            n.Vigas.Count > 0
                                 End Function).ToList()
    End Function

    ' Devuelve el nodo cuyo tramo de columna (arriba o abajo) tiene un footprint
    ' que contiene (x, y) a la cota z dada. Fallback a match por centroide ± 1 cm
    ' por si el tramo trae B/H en 0 (proyectos a los que no se les ingresó refuerzo).
    Private Shared Function BuscarNodoParaViga(nodosPorZ As Dictionary(Of Double, List(Of NodoRVC)),
                                                 x As Double, y As Double, z As Double) As NodoRVC
        Dim lst As List(Of NodoRVC) = Nothing
        If Not nodosPorZ.TryGetValue(Math.Round(z, 2), lst) Then Return Nothing

        ' Pase 1: footprint de la columna arriba o abajo (prefiere arriba si existe).
        For Each n In lst
            Dim tr = If(n.ColumnaArriba, n.ColumnaAbajo)
            If tr Is Nothing OrElse tr.B_Plano <= 0 OrElse tr.H_Plano <= 0 Then Continue For
            If PuntoEnFootprint(x, y, n.X, n.Y, tr.H_Plano, tr.B_Plano, tr.AnguloOrientacion) Then
                Return n
            End If
        Next

        ' Pase 2 (fallback): match por centroide ± 1 cm (columnas sin dimensiones
        ' persistidas en el tramo — raro, pero posible en legacy).
        For Each n In lst
            If Math.Abs(n.X - x) < 0.01 AndAlso Math.Abs(n.Y - y) < 0.01 Then Return n
        Next
        Return Nothing
    End Function

    ' Punto (x,y) dentro del rectángulo centrado en (cx,cy), con 'largo' sobre el
    ' eje local X y 'corto' sobre el eje local Y, rotado alphaDeg antihorario desde
    ' el eje global X.
    '
    ' Convención ARCO (ver Form_Planta_Columnas:361 y DimOrientadas):
    '   - H_Plano = max(Depth, Width) → va sobre el eje local X cuando α=0.
    '   - B_Plano = min(Depth, Width) → va sobre el eje local Y cuando α=0.
    ' Los callers DEBEN pasar `largo = tr.H_Plano, corto = tr.B_Plano` en ese orden.
    ' Pasar al revés (como estaba antes) hace que Pier con H>>B no detecten los
    ' joints de vigas que entran por sus extremos.
    Private Shared Function PuntoEnFootprint(x As Double, y As Double,
                                              cx As Double, cy As Double,
                                              largo As Single, corto As Single,
                                              alphaDeg As Single) As Boolean
        Dim dx = x - cx, dy = y - cy
        Dim a = -CDbl(alphaDeg) * Math.PI / 180.0   ' rotación inversa
        Dim cosA = Math.Cos(a), sinA = Math.Sin(a)
        Dim xl = dx * cosA - dy * sinA              ' componente sobre eje local X (eje largo)
        Dim yl = dx * sinA + dy * cosA              ' componente sobre eje local Y (eje corto)
        ' Margen 5 cm para absorber redondeo de ETABS en bordes.
        Const MARGEN As Double = 0.05
        Return Math.Abs(xl) <= largo / 2.0 + MARGEN AndAlso Math.Abs(yl) <= corto / 2.0 + MARGEN
    End Function

    Private Shared Function ClaveCoord(x As Double, y As Double, z As Double) As String
        ' Redondeo a 1 cm para absorber ruido numérico entre módulos.
        Return $"{Math.Round(x, 2).ToString(Globalization.CultureInfo.InvariantCulture)}|" &
               $"{Math.Round(y, 2).ToString(Globalization.CultureInfo.InvariantCulture)}|" &
               $"{Math.Round(z, 2).ToString(Globalization.CultureInfo.InvariantCulture)}"
    End Function

    Private Shared Function GetOrAddNodo(mapa As Dictionary(Of String, NodoRVC), x As Double, y As Double, z As Double, piso As String) As NodoRVC
        Dim k = ClaveCoord(x, y, z)
        Dim n As NodoRVC = Nothing
        If mapa.TryGetValue(k, n) Then Return n
        n = New NodoRVC With {.Label = $"({Math.Round(x, 2)}, {Math.Round(y, 2)}, {Math.Round(z, 2)})", .Piso = piso, .X = x, .Y = y, .Z = z}
        mapa(k) = n
        Return n
    End Function

    Private Shared Function BuscarNodo(mapa As Dictionary(Of String, NodoRVC), x As Double, y As Double, z As Double) As NodoRVC
        Dim n As NodoRVC = Nothing
        If mapa.TryGetValue(ClaveCoord(x, y, z), n) Then Return n
        Return Nothing
    End Function

    ' Las vigas resuelven por ElementLabel; si no hay match (proyectos híbridos),
    ' caemos a ObjectLabel para no romper. Ver CLAUDE.md sobre la distinción.
    Private Shared Function ResolverJoint(idxElem As Dictionary(Of String, cJoint),
                                           idxObj As Dictionary(Of String, cJoint),
                                           label As String) As cJoint
        If String.IsNullOrWhiteSpace(label) Then Return Nothing
        Dim j As cJoint = Nothing
        If idxElem.TryGetValue(label, j) Then Return j
        If idxObj.TryGetValue(label, j) Then Return j
        Return Nothing
    End Function

    ' =========================================================================
    '  2) Mn de viga: cerrada NSR-10 C.10.2, φ=1, fy nominal
    ' =========================================================================
    Private Shared Function CalcularMnViga(fr As cFrame, esExtremoI As Boolean) As AporteVigaRVC
        Dim ap As New AporteVigaRVC()

        Dim sec = fr.Section
        If sec Is Nothing OrElse sec.b <= 0 OrElse sec.h <= 0 Then Return ap

        Dim b_mm = sec.b * 1000.0
        Dim h_mm = sec.h * 1000.0
        Dim rec_mm = sec.recubrimiento * 1000.0
        Dim d_mm = h_mm - rec_mm
        Dim fc = sec.fc
        Dim fy = If(sec.fy > 0, sec.fy, 420.0)
        If fc <= 0 Then fc = 21.0

        Dim pos As PosicionTramoViga = If(esExtremoI, PosicionTramoViga.Izquierda, PosicionTramoViga.Derecha)

        Dim asSup_mm2 = AreaRefuerzoEnZona(fr.RefuerzoSuperior, pos)
        Dim asInf_mm2 = AreaRefuerzoEnZona(fr.RefuerzoInferior, pos)

        ap.B_m = sec.b
        ap.H_m = sec.h
        ap.D_m = (h_mm - rec_mm) / 1000.0
        ap.Fc_MPa = fc
        ap.Fy_MPa = fy
        ap.As_Sup_mm2 = asSup_mm2
        ap.As_Inf_mm2 = asInf_mm2
        ap.MnNeg = MnCerrado(asSup_mm2, fy, fc, b_mm, d_mm)
        ap.MnPos = MnCerrado(asInf_mm2, fy, fc, b_mm, d_mm)
        Return ap
    End Function

    Private Shared Function AreaRefuerzoEnZona(lista As List(Of cRefuerzoTramo), pos As PosicionTramoViga) As Double
        If lista Is Nothing Then Return 0
        Dim total As Double = 0
        For Each r In lista.Where(Function(x) x IsNot Nothing AndAlso x.Posicion = pos)
            If r.Barras Is Nothing Then Continue For
            For Each kv In r.Barras
                If kv.Value <= 0 Then Continue For
                total += kv.Value * Funciones_00_Varias.AreaRefuerzo(kv.Key)
            Next
        Next
        Return total
    End Function

    ' Mn = As·fy·(d - a/2) / 10⁶ [kN·m]. Replica VigaService.CalcularMnNominal (privada).
    Private Shared Function MnCerrado(as_mm2 As Double, fy As Double, fc As Double, b_mm As Double, d_mm As Double) As Double
        If as_mm2 <= 0 OrElse d_mm <= 0 OrElse b_mm <= 0 OrElse fc <= 0 Then Return 0
        Dim a = as_mm2 * fy / (0.85 * fc * b_mm)   ' mm
        Return Math.Max(as_mm2 * fy * (d_mm - a / 2.0) / 1.0E6, 0.0)
    End Function

    ' =========================================================================
    '  3) Mn de columna: interpolar DI nominal y proyectar a X/Y globales.
    '
    '  Retorna un AporteColumnaRVC con TODAS las variables intermedias (Pu, Mn3,
    '  Mn2, α, MncX, MncY) — así la UI puede mostrar la traza completa y el
    '  usuario diagnosticar valores extraños.
    ' =========================================================================
    ' Interpola DI nominal y proyecta al sistema LL/LC del nudo. Caso normal
    ' (alphaNudo = α del tramo): MncLL = Mn3, MncLC = Mn2. Para Δα ≠ 0 (tramos
    ' arriba/abajo con orientaciones distintas, raro), fórmula cuadrática.
    Private Shared Function CalcularMnColumna(tr As Tramo_Columna, posicion As String, alphaNudoRad As Double) As AporteColumnaRVC
        Dim a As New AporteColumnaRVC With {
            .Tramo = tr, .Posicion = posicion
        }
        If tr Is Nothing Then Return a

        a.B_Plano = tr.B_Plano
        a.H_Plano = tr.H_Plano
        a.AnguloDeg = tr.AnguloOrientacion
        a.Fc = tr.fc
        a.Pu = PuDiseno(tr)

        ' DI nominal (sin φ). Pu ya viene invertido para convención diagrama.
        a.Mn3 = ColumnaService.InterpolarMnEnPu(tr.Lista_DI_Pn_M3, tr.Lista_DI_Mn_M3, CSng(a.Pu))
        a.Mn2 = ColumnaService.InterpolarMnEnPu(tr.Lista_DI_Pn_M2, tr.Lista_DI_Mn_M2, CSng(a.Pu))

        Dim alphaTramoRad = CDbl(tr.AnguloOrientacion) * Math.PI / 180.0
        a.DeltaAlphaRad = alphaTramoRad - alphaNudoRad
        Dim cosD = Math.Cos(a.DeltaAlphaRad), sinD = Math.Sin(a.DeltaAlphaRad)

        ' Convención: LL del nudo ∥ eje 2 del tramo de referencia (lado largo).
        ' Para Δα=0 → MncLL = Mn3 (sismo en LL → flexión alrededor del eje 3).
        ' Para Δα≠0 → magnitud del vector resistencia proyectado.
        a.MncLL = Math.Sqrt((a.Mn3 * cosD) ^ 2 + (a.Mn2 * sinD) ^ 2)
        a.MncLC = Math.Sqrt((a.Mn3 * sinD) ^ 2 + (a.Mn2 * cosD) ^ 2)
        Return a
    End Function

    ' CRÍTICO — Convención de signos.
    '
    ' ETABS reporta P: compresión NEGATIVA, tracción POSITIVA.
    ' Las listas Lista_DI_P*_M* del diagrama de interacción están en convención
    ' DIAGRAMA: compresión POSITIVA (ver ColumnaService:405 "compresión positiva").
    '
    ' Pu_Gob_DI se guarda en ColumnaService:420 SIN invertir, es decir en signo
    ' ETABS. Al interpolar contra el DI hay que invertirlo. ColumnaService ya
    ' hace esto internamente (Pu_dia = -combo.P en línea 406); replicamos acá
    ' para no equivocar el extremo del DI usado — un Pu de -3725 ETABS (columna
    ' de sótano, fuerte compresión) se debe mapear a +3725 diagrama o el Mn
    ' interpolado viene del lado de tracción del DI y sale artificialmente bajo.
    Private Shared Function PuDiseno(tr As Tramo_Columna) As Double
        If tr.Pu_Gob_DI <> 0 Then Return -CDbl(tr.Pu_Gob_DI)
        Dim p2 = Math.Abs(CDbl(tr.Pu_V2)), p3 = Math.Abs(CDbl(tr.Pu_V3))
        Return -Math.Sign(If(tr.Pu_V2 <> 0, tr.Pu_V2, tr.Pu_V3)) * Math.Max(p2, p3)
    End Function

    ' =========================================================================
    '  4) Evaluación del nudo: define el eje LL del nudo (de la columna de
    '     referencia), lo propaga a las vigas, y evalúa en direcciones LL y LC.
    ' =========================================================================
    Private Shared Sub EvaluarNodo(n As NodoRVC)
        ' Orientación de referencia: columna arriba si existe, sino abajo.
        Dim refTramo = If(n.ColumnaArriba, n.ColumnaAbajo)
        If refTramo Is Nothing Then Return
        n.AnguloRefDeg = refTramo.AnguloOrientacion
        Dim alphaRefRad = CDbl(refTramo.AnguloOrientacion) * Math.PI / 180.0

        ' Propagar el ángulo LL del nudo a cada aporte-viga para que las
        ' proyecciones LL/LC usen el ángulo correcto.
        For Each v In n.Vigas
            v.AnguloLLNudoRad = alphaRefRad
        Next

        n.DireccionLL = EvaluarDireccion(n, eDirColumna.LL, alphaRefRad)
        n.DireccionLC = EvaluarDireccion(n, eDirColumna.LC, alphaRefRad)
    End Sub

    Private Shared Function EvaluarDireccion(n As NodoRVC, dir As eDirColumna, alphaRefRad As Double) As ResultadoRVCDireccion
        Dim res As New ResultadoRVCDireccion With {.Direccion = dir}

        ' --- Columnas (arriba + abajo) ----------------------------------------
        For Each par In {(Tr:=n.ColumnaArriba, Pos:="arriba"), (Tr:=n.ColumnaAbajo, Pos:="abajo")}
            If par.Tr Is Nothing Then Continue For
            Dim aporte = CalcularMnColumna(par.Tr, par.Pos, alphaRefRad)
            Dim mnc = If(dir = eDirColumna.LL, aporte.MncLL, aporte.MncLC)
            res.SumMnc += mnc
            res.AportesColumna.Add(aporte)
        Next

        ' --- Vigas (proyectadas al sistema LL/LC del nudo) -------------------
        Const UMBRAL_PROY As Double = 0.05
        For Each v In n.Vigas
            Dim proj = If(dir = eDirColumna.LL, v.ProyeccionLL, v.ProyeccionLC)
            If proj < UMBRAL_PROY Then Continue For
            Dim contrib = v.MnChequeo * proj
            If contrib <= 0 Then Continue For
            res.SumMnv += contrib
            res.AportesViga.Add(v)
        Next

        Return res
    End Function

End Class
