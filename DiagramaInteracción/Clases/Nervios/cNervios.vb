Imports System.Runtime.Serialization

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 11 — Nervios / Losas nervadas
'  Diseño por cargas gravitacionales según NSR-10 C.13 y C.8.10
' ═══════════════════════════════════════════════════════════════════════════════

' ── Contenedor principal ──────────────────────────────────────────────────────
<Serializable>
Public Class cNervios

    <OptionalField> Public Elementos As New List(Of cNervio)()
    <OptionalField> Public Lista_Combinaciones As New List(Of String)()
    <OptionalField> Public ListA_Combinaciones_Design As New List(Of String)()
    <OptionalField> Public Secciones_Nervio As New List(Of String)()
    <OptionalField> Public Propiedades_Secciones As New Dictionary(Of String, PropSeccionNervio)(StringComparer.OrdinalIgnoreCase)
    <OptionalField> Public GruposManual As New List(Of List(Of String))()
    <OptionalField> Public BeamForces As New List(Of cCombinacionBeamForce)()

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Elementos Is Nothing Then Elementos = New List(Of cNervio)()
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)()
        If ListA_Combinaciones_Design Is Nothing Then ListA_Combinaciones_Design = New List(Of String)()
        If Secciones_Nervio Is Nothing Then Secciones_Nervio = New List(Of String)()
        If Propiedades_Secciones Is Nothing Then Propiedades_Secciones = New Dictionary(Of String, PropSeccionNervio)(StringComparer.OrdinalIgnoreCase)
        If GruposManual Is Nothing Then GruposManual = New List(Of List(Of String))()
        If BeamForces Is Nothing Then BeamForces = New List(Of cCombinacionBeamForce)()
    End Sub

End Class

' ── Dimensiones de una sección de ETABS (para detectar ancho de apoyos) ───────
<Serializable>
Public Class PropSeccionNervio
    <OptionalField> Public Nombre As String
    <OptionalField> Public B As Double   ' m (dimensión t2, ancho)
    <OptionalField> Public H As Double   ' m (dimensión t3, alto)
End Class

' ── Nervio continuo (agrupación de frames colineales) ─────────────────────────
<Serializable>
Public Class cNervio

    <OptionalField> Public Nombre As String
    <OptionalField> Public NombrePlano As String
    <OptionalField> Public Piso As String
    <OptionalField> Public Frames As New List(Of cFrameNervio)()

    ' Sección de losa típica — aplica a todos los tramos del nervio
    ' El usuario puede ajustar tf y paso; paso=0 → se calcula automáticamente
    <OptionalField> Public Tf_Losa As Double      ' m, espesor losa (default 0.05)
    <OptionalField> Public Paso_Nervios As Double  ' m, separación c-c entre nervios (0 = auto)

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Frames Is Nothing Then Frames = New List(Of cFrameNervio)()
        If Tf_Losa = 0 Then Tf_Losa = 0.05
    End Sub

    Public Overrides Function ToString() As String
        Return If(Not String.IsNullOrWhiteSpace(NombrePlano), NombrePlano, Nombre)
    End Function

End Class

' ── Tramo (frame individual dentro de un nervio) ──────────────────────────────
<Serializable>
Public Class cFrameNervio

    ' ── Identificación ETABS ──────────────────────────────────────────────────
    <OptionalField> Public ObjectLabel As String
    <OptionalField> Public ElementLabel As String
    <OptionalField> Public Story As String
    <OptionalField> Public JointI As String
    <OptionalField> Public JointJ As String

    ' ── Geometría ─────────────────────────────────────────────────────────────
    <OptionalField> Public NombreSeccion As String
    <OptionalField> Public Bw As Double         ' m — ancho del alma (rectangular)
    <OptionalField> Public H As Double          ' m — alto total
    <OptionalField> Public Longitud As Double   ' m — centroide a centroide

    ' ── Apoyo (detectado automáticamente de frames perpendiculares) ───────────
    <OptionalField> Public B_Apoyo_I As Double      ' m — semiancho apoyo izquierdo
    <OptionalField> Public B_Apoyo_D As Double      ' m — semiancho apoyo derecho
    <OptionalField> Public Frame_Apoyo_I As String  ' label del frame soporte izq
    <OptionalField> Public Frame_Apoyo_D As String  ' label del frame soporte der

    ' ── Eje estructural más cercano al apoyo (GridID de ETABS o manual) ───────
    <OptionalField>
    Private _ejeApoyoI As String
    <OptionalField>
    Private _ejeApoyoD As String

    Public Property EjeApoyo_I As String
        Get
            Return If(_ejeApoyoI, "")
        End Get
        Set(value As String)
            _ejeApoyoI = value
        End Set
    End Property

    Public Property EjeApoyo_D As String
        Get
            Return If(_ejeApoyoD, "")
        End Get
        Set(value As String)
            _ejeApoyoD = value
        End Set
    End Property

    ' ── Sección T (activada por usuario) ──────────────────────────────────────
    <OptionalField> Public EsSeccionT As Boolean
    <OptionalField> Public Be As Double    ' m — ancho efectivo calculado NSR-10 C.8.10.2
    <OptionalField> Public Tf As Double    ' m — espesor losa (heredado de cNervio.Tf_Losa)
    <OptionalField> Public Paso As Double  ' m — paso nervios (heredado o auto)

    ' ── Materiales ────────────────────────────────────────────────────────────
    <OptionalField> Public fc As Double           ' MPa
    <OptionalField> Public fy As Double           ' MPa
    <OptionalField> Public Recubrimiento As Double ' m

    ' ── Refuerzo longitudinal discretizado por zona (Izquierda/Centro/Derecha) ─
    ' Reutiliza cRefuerzoTramo (Clases/cFrame.vb): Posicion + Barras(calibre→cant.)
    ' Superior = momentos negativos (apoyos); Inferior = momentos positivos (vano)
    <OptionalField> Public RefuerzoSuperior As New List(Of cRefuerzoTramo)()
    <OptionalField> Public RefuerzoInferior As New List(Of cRefuerzoTramo)()

    ' ── Refuerzo transversal (estribos) discretizado por zona ─────────────────
    ' Reutiliza cRefuerzoTransversalZona (Clases/cFrame.vb)
    <OptionalField> Public RefuerzoTransversal As New List(Of cRefuerzoTransversalZona)()

    ' ── Indica si el usuario ya modificó el refuerzo ─────────────────────────
    <OptionalField> Public Ref_Modificado As Boolean

    ' ── Demandas en cara del apoyo (máximas sobre combos de diseño) ───────────
    <OptionalField> Public Mu_Neg_I As Double   ' kN·m — momento negativo cara izq
    <OptionalField> Public Mu_Pos_C As Double   ' kN·m — momento positivo máximo vano
    <OptionalField> Public Mu_Neg_D As Double   ' kN·m — momento negativo cara der
    <OptionalField> Public Vu_I As Double       ' kN   — cortante cara izq
    <OptionalField> Public Vu_D As Double       ' kN   — cortante cara der

    ' ── Capacidades calculadas (por zona: Izq/Centro/Der con refuerzo propio) ──
    <OptionalField> Public As_Prov_Sup_I As Double  ' cm² — refuerzo superior zona Izquierda
    <OptionalField> Public As_Prov_Sup_D As Double  ' cm² — refuerzo superior zona Derecha
    <OptionalField> Public As_Prov_Inf_C As Double  ' cm² — refuerzo inferior zona Centro
    <OptionalField> Public As_Min As Double         ' cm²
    <OptionalField> Public As_Max As Double         ' cm²
    <OptionalField> Public PhiMn_Sup_I As Double    ' kN·m
    <OptionalField> Public PhiMn_Sup_D As Double    ' kN·m
    <OptionalField> Public PhiMn_Inf_C As Double    ' kN·m
    <OptionalField> Public PhiVn_I As Double        ' kN
    <OptionalField> Public PhiVn_D As Double        ' kN

    ' ── C/D por zona ──────────────────────────────────────────────────────────
    <OptionalField> Public CD_Flex_Sup_I As Double
    <OptionalField> Public CD_Flex_Inf_C As Double
    <OptionalField> Public CD_Flex_Sup_D As Double
    <OptionalField> Public CD_Cortante_I As Double
    <OptionalField> Public CD_Cortante_D As Double
    <OptionalField> Public Cumple As Boolean

    ' ── Fuerzas por combinación (del import ETABS) ────────────────────────────
    <OptionalField> Public Combinaciones As New List(Of cComboNervio)()

    ' ── Propiedades derivadas ─────────────────────────────────────────────────
    Public ReadOnly Property d_sup As Double
        Get
            Return H - Recubrimiento
        End Get
    End Property

    Public ReadOnly Property d_inf As Double
        Get
            Return H - Recubrimiento
        End Get
    End Property

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Combinaciones Is Nothing Then Combinaciones = New List(Of cComboNervio)()
        If RefuerzoSuperior Is Nothing Then RefuerzoSuperior = New List(Of cRefuerzoTramo)()
        If RefuerzoInferior Is Nothing Then RefuerzoInferior = New List(Of cRefuerzoTramo)()
        If RefuerzoTransversal Is Nothing Then RefuerzoTransversal = New List(Of cRefuerzoTransversalZona)()
        If _ejeApoyoI Is Nothing Then _ejeApoyoI = ""
        If _ejeApoyoD Is Nothing Then _ejeApoyoD = ""
        If fy = 0 Then fy = 420
        If fc = 0 Then fc = 21
        If Recubrimiento = 0 Then Recubrimiento = 0.04
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Story} - {ObjectLabel} ({NombreSeccion})"
    End Function

End Class

' ── Fuerzas de una combinación en un tramo ────────────────────────────────────
<Serializable>
Public Class cComboNervio

    <OptionalField> Public Nombre As String

    ' Estaciones y fuerzas tal como las exporta ETABS (centroide a centroide)
    <OptionalField> Public Estaciones As New List(Of Double)()  ' m
    <OptionalField> Public Momentos As New List(Of Double)()    ' kN·m (M3)
    <OptionalField> Public Cortantes As New List(Of Double)()   ' kN  (V2)

    ' Valores en cara del apoyo (calculados por interpolación)
    <OptionalField> Public M_Cara_I As Double  ' kN·m
    <OptionalField> Public M_Cara_D As Double  ' kN·m
    <OptionalField> Public V_Cara_I As Double  ' kN
    <OptionalField> Public V_Cara_D As Double  ' kN
    <OptionalField> Public M_Max_Pos As Double ' kN·m — máximo positivo en el vano

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Estaciones Is Nothing Then Estaciones = New List(Of Double)()
        If Momentos Is Nothing Then Momentos = New List(Of Double)()
        If Cortantes Is Nothing Then Cortantes = New List(Of Double)()
    End Sub

End Class
