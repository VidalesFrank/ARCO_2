Imports System.Runtime.Serialization

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 13 — Muro de Contención tipo Cortina (MC. Cortina)
'
'  Pantalla vertical apoyada en altura por losas u otras estructuras. A
'  diferencia del muro cantilever (módulo 12), aquí el empuje se transmite
'  por reacciones horizontales en los apoyos, no por el peso propio del muro.
'
'  Convenciones:
'    - El origen del muro es la BASE (y = 0). Las alturas se miden hacia
'      arriba.
'    - Los Tramos se listan en orden ascendente (primero el de la base).
'    - Los Apoyos son siempre Tramos.Count + 1. El apoyo 0 es el de la base,
'      el último es el de la corona. Los intermedios tienen Y = suma de las
'      alturas de los tramos inferiores.
'    - La cara trasera (lado del relleno) es la que recibe el empuje activo.
'
'  Unidades: m, kN, kPa, MPa (igual que el módulo 12 — reusa enums y clases).
'
'  Reusos del módulo 12 (MuroContencion.vb):
'    - eTeoriaEmpuje        : Rankine / Coulomb
'    - eNormativaMuro       : Residencial / Vial
'    - DisenoElementoMuro   : estructura de refuerzo con factores C/D y
'                              propiedad RecalcularAsProv
' ═══════════════════════════════════════════════════════════════════════════════

''' <summary>Tipo de restricción en un apoyo del muro cortina. Determina las
''' condiciones de borde del cálculo matricial.</summary>
Public Enum eTipoApoyoCortina
    ''' <summary>Impide rotación y traslación horizontal (p. ej., unión monolítica a una zapata).</summary>
    Empotrado = 0
    ''' <summary>Impide solo traslación horizontal (p. ej., una losa que lo arrostra).</summary>
    SimplementeApoyado = 1
    ''' <summary>Sin restricción — extremo libre (voladizo).</summary>
    Libre = 2
End Enum

' =====================================================================
'  Contenedor principal — se serializa como .esm independiente
' =====================================================================
<Serializable>
Public Class MuroCortina

    <OptionalField> Public VersionFormato As Integer
    Public Const VersionActual As Integer = 1

    <OptionalField> Public Ruta As String = ""

    <OptionalField> Public Info As New InfoMuroCortina()
    <OptionalField> Public Normativa As eNormativaMuro = eNormativaMuro.Residencial

    ''' <summary>Tramos de la pantalla, en orden ascendente desde la base.</summary>
    <OptionalField> Public Tramos As New List(Of TramoCortina)()

    ''' <summary>Apoyos de la pantalla. Siempre debe ser Tramos.Count + 1:
    ''' Apoyos(0) = base, Apoyos(last) = corona.</summary>
    <OptionalField> Public Apoyos As New List(Of ApoyoCortina)()

    <OptionalField> Public Suelo As New PropsSueloCortina()
    <OptionalField> Public Sobrecarga As New PropsSobrecargaCortina()
    <OptionalField> Public Materiales As New PropsMaterialesCortina()
    <OptionalField> Public Opciones As New OpcionesCortinaMuro()

    <OptionalField> Public Resultados As ResultadosCortina

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Info Is Nothing Then Info = New InfoMuroCortina()
        If Tramos Is Nothing Then Tramos = New List(Of TramoCortina)()
        If Apoyos Is Nothing Then Apoyos = New List(Of ApoyoCortina)()
        If Suelo Is Nothing Then Suelo = New PropsSueloCortina()
        If Sobrecarga Is Nothing Then Sobrecarga = New PropsSobrecargaCortina()
        If Materiales Is Nothing Then Materiales = New PropsMaterialesCortina()
        If Opciones Is Nothing Then Opciones = New OpcionesCortinaMuro()
        If Ruta Is Nothing Then Ruta = ""
    End Sub

    ''' <summary>Altura total = suma de alturas de todos los tramos.</summary>
    Public ReadOnly Property AlturaTotal As Double
        Get
            If Tramos Is Nothing Then Return 0
            Return Tramos.Sum(Function(t) t.Altura)
        End Get
    End Property

End Class

' =====================================================================
'  Metadatos — portada del reporte
' =====================================================================
<Serializable>
Public Class InfoMuroCortina
    <OptionalField> Public Nombre As String = ""
    <OptionalField> Public Proyecto As String = ""
    <OptionalField> Public Cliente As String = ""
    <OptionalField> Public Ubicacion As String = ""
    <OptionalField> Public Responsable As String = ""
    <OptionalField> Public Codigo As String = ""
    <OptionalField> Public Fecha As Date = Date.Today
    <OptionalField> Public Descripcion As String = ""

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = ""
        If Proyecto Is Nothing Then Proyecto = ""
        If Cliente Is Nothing Then Cliente = ""
        If Ubicacion Is Nothing Then Ubicacion = ""
        If Responsable Is Nothing Then Responsable = ""
        If Codigo Is Nothing Then Codigo = ""
        If Descripcion Is Nothing Then Descripcion = ""
    End Sub
End Class

' =====================================================================
'  Tramo — un segmento vertical del muro
' =====================================================================
<Serializable>
Public Class TramoCortina
    ''' <summary>Altura del tramo (m).</summary>
    <OptionalField> Public Altura As Double = 2.5
    ''' <summary>Espesor del muro en este tramo (m). Puede variar entre tramos.</summary>
    <OptionalField> Public Espesor As Double = 0.25
    ''' <summary>Nombre amigable del tramo para reportes. Vacío = "Tramo 1", "Tramo 2"…</summary>
    <OptionalField> Public Nombre As String = ""

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = ""
    End Sub
End Class

' =====================================================================
'  Apoyo — un soporte lateral del muro
' =====================================================================
<Serializable>
Public Class ApoyoCortina
    ''' <summary>Posición Y desde la base (m).</summary>
    <OptionalField> Public Nivel As Double
    <OptionalField> Public Tipo As eTipoApoyoCortina = eTipoApoyoCortina.SimplementeApoyado
    ''' <summary>Etiqueta para el reporte: "Base", "Losa piso 1", "Corona"…</summary>
    <OptionalField> Public Etiqueta As String = ""

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Etiqueta Is Nothing Then Etiqueta = ""
    End Sub
End Class

' =====================================================================
'  Suelo — propiedades para calcular empuje activo y, opcionalmente,
'  presión hidrostática si NivelAguaFreatica > 0.
' =====================================================================
<Serializable>
Public Class PropsSueloCortina
    ''' <summary>Ángulo de fricción interna, grados.</summary>
    <OptionalField> Public Phi As Double = 30.0
    ''' <summary>Fricción muro-suelo (grados). Para pantallas de sótano suele adoptarse 0 (conservador).</summary>
    <OptionalField> Public Delta As Double = 0.0
    ''' <summary>Peso específico del suelo seco/húmedo (kN/m³).</summary>
    <OptionalField> Public Gamma As Double = 18.0
    ''' <summary>Peso específico saturado (kN/m³). Se usa debajo del nivel freático.</summary>
    <OptionalField> Public GammaSat As Double = 20.0
    ''' <summary>Cohesión (kPa). Suele ser 0 para arenas.</summary>
    <OptionalField> Public Cohesion As Double = 0.0

    <OptionalField> Public CalcularKaAutomatico As Boolean = True
    <OptionalField> Public KaManual As Double = 0.333
    <OptionalField> Public Teoria As eTeoriaEmpuje = eTeoriaEmpuje.Rankine
End Class

' =====================================================================
'  Sobrecarga en la superficie del relleno
' =====================================================================
<Serializable>
Public Class PropsSobrecargaCortina
    <OptionalField> Public IncluirSobrecarga As Boolean = False
    ''' <summary>Sobrecarga uniforme en la superficie (kPa).</summary>
    <OptionalField> Public SobrecargaKPa As Double = 10.0
End Class

' =====================================================================
'  Materiales — recubrimientos distintos por cara (la cara en contacto con
'  el suelo típicamente requiere más recubrimiento que la cara expuesta).
' =====================================================================
<Serializable>
Public Class PropsMaterialesCortina
    <OptionalField> Public Fc As Single = 21.0
    <OptionalField> Public Fy As Single = 420.0
    <OptionalField> Public PesoConcreto As Single = 24.0
    ''' <summary>Recubrimiento en la cara EXTERIOR (lado libre), m.</summary>
    <OptionalField> Public RecubrimientoExterior As Single = 0.05
    ''' <summary>Recubrimiento en la cara INTERIOR (lado del relleno), m.
    ''' NSR-10 C.7.7 sugiere mayor para elementos en contacto con suelo.</summary>
    <OptionalField> Public RecubrimientoInterior As Single = 0.075

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If RecubrimientoExterior <= 0 Then RecubrimientoExterior = 0.05
        If RecubrimientoInterior <= 0 Then RecubrimientoInterior = 0.075
    End Sub
End Class

' =====================================================================
'  Opciones del análisis
' =====================================================================
<Serializable>
Public Class OpcionesCortinaMuro
    ''' <summary>Si True, se agrega la presión hidrostática por encima del NivelAguaFreatica.
    ''' Importante para muros de sótano con nivel freático sobre la base.</summary>
    <OptionalField> Public IncluirHidrostatica As Boolean = False

    ''' <summary>Nivel del agua freática medido desde la base (m). 0 = sin agua.
    ''' Encima de este nivel el suelo se trata como saturado y se suma presión hidrostática.</summary>
    <OptionalField> Public NivelAguaFreatica As Double = 0.0

    ''' <summary>Si True, se amplifica el Ka por FactorSismico para el caso de resistencia extrema.</summary>
    <OptionalField> Public IncluirSismo As Boolean = False
    ''' <summary>Factor multiplicador del Ka para el caso sísmico (simplificación de Mononobe-Okabe).</summary>
    <OptionalField> Public FactorSismico As Double = 1.3

    ''' <summary>Peso específico del agua (kN/m³) — valor por defecto 9.81 (10 redondeado está también
    ''' permitido por norma colombiana).</summary>
    <OptionalField> Public GammaAgua As Double = 9.81
End Class

' =====================================================================
'  Resultados — se repueblan en cada corrida del servicio (Fase C)
' =====================================================================
<Serializable>
Public Class ResultadosCortina
    <OptionalField> Public FechaCalculo As Date = Date.Now

    ' Coeficientes efectivos
    <OptionalField> Public Ka As Double
    <OptionalField> Public KaSismico As Double

    ''' <summary>Diagrama de momento M(y) muestreado a lo largo del muro.
    ''' Elemento: (Y en m desde la base, M en kN·m/m).</summary>
    <OptionalField> Public DiagramaM As New List(Of PuntoDiagrama)()

    ''' <summary>Diagrama de cortante V(y). (Y, V en kN/m).</summary>
    <OptionalField> Public DiagramaV As New List(Of PuntoDiagrama)()

    ''' <summary>Reacciones horizontales en cada apoyo (kN/m, misma cuenta que Apoyos).</summary>
    <OptionalField> Public Reacciones As New List(Of Double)()

    <OptionalField> Public MomentoMaximo As Double
    <OptionalField> Public CortanteMaximo As Double
    <OptionalField> Public YMomentoMax As Double

    ''' <summary>Diseño y revisión de refuerzo por tramo (una capa por cara).</summary>
    <OptionalField> Public DisenosTramos As New List(Of DisenoTramoCortina)()

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If DiagramaM Is Nothing Then DiagramaM = New List(Of PuntoDiagrama)()
        If DiagramaV Is Nothing Then DiagramaV = New List(Of PuntoDiagrama)()
        If Reacciones Is Nothing Then Reacciones = New List(Of Double)()
        If DisenosTramos Is Nothing Then DisenosTramos = New List(Of DisenoTramoCortina)()
    End Sub
End Class

' =====================================================================
'  Diseño por tramo del muro cortina — una capa por cada cara.
'
'  Convención de signos (coherente con MuroCortinaService):
'    - M > 0 → curvatura concavidad hacia +y (lado del relleno)
'              → tracción en la cara EXTERIOR (lado libre)
'              → refuerzo en CaraPositiva (= cara externa / lado libre)
'    - M < 0 → curvatura concavidad hacia −y (lado libre)
'              → tracción en la cara INTERIOR (lado del relleno)
'              → refuerzo en CaraNegativa (= cara interna / lado del relleno)
'
'  Doble capa requerida cuando se cumple cualquiera de:
'    1. El análisis muestra M+ y M- en el mismo tramo (DobleCapaPorAnalisis).
'    2. NSR-10 C.14.3.4: t ≥ 25 cm (DobleCapaPorNorma).
'    3. El usuario fuerza manualmente (DobleCapaForzada), ej. muros de 20 cm
'       en los que el usuario prefiere doble capa por práctica.
' =====================================================================

''' <summary>Diseño de refuerzo de UNA cara del muro cortina.</summary>
<Serializable>
Public Class DisenoCaraCortina
    ''' <summary>Peralte efectivo de esta cara (m). d = t − recubrimiento_de_la_cara.</summary>
    <OptionalField> Public d As Double
    ''' <summary>Recubrimiento usado (m) — guardado para trazabilidad en el reporte.</summary>
    <OptionalField> Public Recubrimiento As Double
    ''' <summary>Momento último de diseño (positivo = magnitud), kN·m/m.</summary>
    <OptionalField> Public MuMax As Double
    <OptionalField> Public AsReq As Double              ' mm²/m (acero principal)
    <OptionalField> Public BarraPrincipal As String = "#5"
    <OptionalField> Public SeparacionCm As Double
    <OptionalField> Public AsProv As Double             ' mm²/m

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If BarraPrincipal Is Nothing Then BarraPrincipal = "#5"
    End Sub

    ''' <summary>C/D de flexión: AsProv / AsReq. ≥ 0.90 cumple. 0 = sin cálculo.</summary>
    Public ReadOnly Property FactorFlexion As Double
        Get
            If AsReq <= 0 Then Return 0
            If AsProv <= 0 Then Return 0
            Return AsProv / AsReq
        End Get
    End Property

    Public ReadOnly Property CumpleFlexion As Boolean
        Get
            Return FactorFlexion >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property
End Class

''' <summary>Diseño y revisión completos de un tramo del muro cortina.</summary>
<Serializable>
Public Class DisenoTramoCortina
    <OptionalField> Public Nombre As String = ""
    <OptionalField> Public B As Double = 1.0
    <OptionalField> Public H As Double           ' espesor (m)
    ''' <summary>Peralte efectivo mínimo entre las dos caras (m) — usado para φVc.
    ''' Las flexiones usan el d específico de cada cara.</summary>
    <OptionalField> Public d As Double

    ' Demanda de cortante (común — el cortante no cambia de cara)
    <OptionalField> Public Vu As Double
    <OptionalField> Public PhiVc As Double

    ' Refuerzo por temperatura y retracción (común — una capa por cara)
    <OptionalField> Public AsReqTemperatura As Double  ' mm²/m total
    <OptionalField> Public BarraTemperatura As String = "#4"
    <OptionalField> Public SeparacionTempCm As Double
    <OptionalField> Public AsProvTemperatura As Double

    ' Refuerzo principal — una capa por cara
    <OptionalField> Public CaraPositiva As New DisenoCaraCortina()   ' M+ → tracción exterior
    <OptionalField> Public CaraNegativa As New DisenoCaraCortina()   ' M− → tracción interior

    ' Flags de doble capa (ver sección de documentación arriba)
    <OptionalField> Public DobleCapaForzada As Boolean = False
    <OptionalField> Public DobleCapaPorNorma As Boolean = False
    <OptionalField> Public DobleCapaPorAnalisis As Boolean = False

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = ""
        If CaraPositiva Is Nothing Then CaraPositiva = New DisenoCaraCortina()
        If CaraNegativa Is Nothing Then CaraNegativa = New DisenoCaraCortina()
        If BarraTemperatura Is Nothing Then BarraTemperatura = "#4"
    End Sub

    Public ReadOnly Property RequiereDosCapas As Boolean
        Get
            Return DobleCapaForzada OrElse DobleCapaPorNorma OrElse DobleCapaPorAnalisis
        End Get
    End Property

    ''' <summary>Texto descriptivo del origen de la necesidad de doble capa.</summary>
    Public ReadOnly Property RazonDobleCapa As String
        Get
            If Not RequiereDosCapas Then Return "—"
            Dim razones As New List(Of String)
            If DobleCapaPorAnalisis Then razones.Add("análisis")
            If DobleCapaPorNorma Then razones.Add("norma t≥25cm")
            If DobleCapaForzada Then razones.Add("forzada")
            Return String.Join(" + ", razones)
        End Get
    End Property

    Public ReadOnly Property FactorCortante As Double
        Get
            If Vu <= 0 Then Return 0
            If PhiVc <= 0 Then Return 0
            Return PhiVc / Vu
        End Get
    End Property

    Public ReadOnly Property CumpleCortante As Boolean
        Get
            Return FactorCortante >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    ''' <summary>True si el tramo cumple flexión en todas las caras con momento + cortante.</summary>
    Public ReadOnly Property CumpleTodo As Boolean
        Get
            Dim okPos = (CaraPositiva.AsReq <= 0) OrElse CaraPositiva.CumpleFlexion
            Dim okNeg = (CaraNegativa.AsReq <= 0) OrElse CaraNegativa.CumpleFlexion
            Return okPos AndAlso okNeg AndAlso CumpleCortante
        End Get
    End Property
End Class

''' <summary>Punto muestreado en un diagrama (M o V). Se usa un objeto en vez
''' de una ValueTuple para que el BinaryFormatter lo serialice correctamente.</summary>
<Serializable>
Public Class PuntoDiagrama
    <OptionalField> Public Y As Double
    <OptionalField> Public Valor As Double
End Class
