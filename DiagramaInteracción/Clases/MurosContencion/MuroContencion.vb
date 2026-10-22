Imports System.Runtime.Serialization

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 12 — Muros de Contención
'
'  Modelo serializable del muro de contención tipo zapata (cantilever). El
'  archivo .esm es un único MuroContencion volcado con BinaryFormatter. La
'  variante estribo-con-cortina del fuente DesignPuentes se dejó fuera del
'  alcance; cuando se adopte, se extiende GeometriaMuro con h3/h4/d/e2/e3 y
'  se agrega una propiedad TipoMuro al contenedor.
'
'  Unidades (coherentes a lo largo del módulo):
'    Longitud  → m
'    Peso      → kN
'    Presión   → kPa
'    Material  → MPa (fc, fy)
'
'  Convenciones:
'    - fc, fy, pesoConcreto, pesoSuelo son Single (coherencia con el resto de ARCO).
'    - Dimensiones y resultados son Double para no acumular error.
'    - <OptionalField> + <OnDeserialized> en todas las clases → compat futura.
' ═══════════════════════════════════════════════════════════════════════════════

''' <summary>Normativa aplicable al muro — determina factores de carga y FS admisibles.</summary>
Public Enum eNormativaMuro
    ''' <summary>NSR-10 Título H + Título C. Combinaciones ACI 1.4D / 1.2D+1.6L / 1.2D+1.0E.
    ''' FS volcamiento 2.0 (estático) / 1.5 (sismo). FS deslizamiento 1.5 / 1.1.</summary>
    Residencial = 0

    ''' <summary>CCP-14 (Código Colombiano de Puentes) que adopta AASHTO LRFD íntegro.
    ''' Combinaciones 1.25DC + 1.35EV + 1.50EH + 1.75LS + 1.0EQ. FS volcamiento e(max)=B/3
    ''' bajo cargas factorizadas. FS deslizamiento 1.0 (ya factorizado).</summary>
    Vial = 1
End Enum

''' <summary>Caso de carga en el análisis. Estático = resistencia I, Sísmico = extremo I.</summary>
Public Enum eCasoCargaMuro
    Estatico = 0
    Sismico = 1
End Enum

''' <summary>Teoría de empuje activo. Rankine aplica cuando el muro es vertical y el relleno
''' horizontal; Coulomb permite inclinación muro-vertical (α) y relleno (β) + fricción muro-suelo (δ).</summary>
Public Enum eTeoriaEmpuje
    Rankine = 0
    Coulomb = 1
End Enum

' =====================================================================
'  Contenedor principal
' =====================================================================
<Serializable>
Public Class MuroContencion

    ''' <summary>Versión del formato .esm. 0 = archivos anteriores a esta release.</summary>
    <OptionalField> Public VersionFormato As Integer
    Public Const VersionActual As Integer = 1

    ''' <summary>Ruta del archivo .esm en disco (vacía mientras no se guarde).</summary>
    <OptionalField> Public Ruta As String = ""

    <OptionalField> Public Info As New InfoMuroContencion()
    <OptionalField> Public Normativa As eNormativaMuro = eNormativaMuro.Residencial
    <OptionalField> Public Geometria As New GeometriaMuro()
    <OptionalField> Public Suelo As New PropsSuelo()
    <OptionalField> Public SueloSismico As New PropsSueloSismico()
    <OptionalField> Public Sobrecarga As New PropsSobrecarga()
    <OptionalField> Public Materiales As New PropsMateriales()
    <OptionalField> Public Opciones As New OpcionesAnalisisMuro()

    ''' <summary>Resultados del último análisis — Nothing hasta que el usuario corra Analizar.</summary>
    <OptionalField> Public Resultados As ResultadosMuro

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Info Is Nothing Then Info = New InfoMuroContencion()
        If Geometria Is Nothing Then Geometria = New GeometriaMuro()
        If Suelo Is Nothing Then Suelo = New PropsSuelo()
        If SueloSismico Is Nothing Then SueloSismico = New PropsSueloSismico()
        If Sobrecarga Is Nothing Then Sobrecarga = New PropsSobrecarga()
        If Materiales Is Nothing Then Materiales = New PropsMateriales()
        If Opciones Is Nothing Then Opciones = New OpcionesAnalisisMuro()
        If Ruta Is Nothing Then Ruta = ""
    End Sub

End Class

' =====================================================================
'  Metadatos — portada del reporte
' =====================================================================
<Serializable>
Public Class InfoMuroContencion
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
'  Geometría del muro cantilever
'
'  Convención profesional: la cara TRASERA del vástago (lado del relleno)
'  es siempre VERTICAL (plomada). Cuando e1 ≠ e2 (vástago variable), la
'  cara DELANTERA es la que inclina — mejor transmisión de cargas,
'  conserva el ancho mínimo arriba donde no hace falta.
'
'      ◄─────────────── B ──────────────►
'          Puntera        │TRASERA
'                e2   ◄──►│  VERTICAL
'            ┌──┐ ◄── vástago en corona (ancho e2)
'            │  │              h2
'           /   │ ◄── cara frontal inclinada si e1 > e2
'          /    │                  h2
'         ┌─────┤
'    h1 ┌─│e1   │─────────┐  ← losa (zapata)
'       │ │     │  talón  │
'       └─┴──┬──┴─────────┘
'            │ │
'            │ │   ← llave de cortante opcional (B_LL × H_LL)
'            └─┘
'
'  H  = altura total
'  h1 = espesor de la losa
'  h2 = altura del vástago
'  B  = ancho total de la zapata
'  b1 = puntera: distancia del pie a la cara DELANTERA del vástago en la base.
'  e1 = espesor del vástago en la BASE (mayor si varía)
'  e2 = espesor del vástago en la CORONA (= e1 si rectangular)
'  a  = legado (se mantiene para compat .esm; no usado en el cálculo).
'       Cara trasera siempre en x = b1 + e1.
'  Talón = B − b1 − e1  (se deriva automáticamente)
'  B_LL, H_LL = dimensiones de la llave de cortante
' =====================================================================
<Serializable>
Public Class GeometriaMuro
    <OptionalField> Public H As Double = 4.0           ' Altura total
    <OptionalField> Public h1 As Double = 0.4          ' Espesor losa
    <OptionalField> Public h2 As Double = 3.6          ' Altura vástago (puede derivarse de H-h1 o ingresarse)
    <OptionalField> Public B As Double = 2.5           ' Ancho zapata
    <OptionalField> Public a As Double = 0.6           ' Distancia talón → eje vástago
    <OptionalField> Public b1 As Double = 1.6          ' Distancia eje vástago → pie (puntera)
    <OptionalField> Public e1 As Double = 0.3          ' Espesor vástago base
    <OptionalField> Public e2 As Double = 0.3          ' Espesor vástago corona

    ' Llave de cortante (si Opciones.IncluirLlave)
    <OptionalField> Public B_LL As Double = 0.3        ' Ancho
    <OptionalField> Public H_LL As Double = 0.4        ' Profundidad bajo la zapata

    ''' <summary>Lado del vástago (fácil derivar sin cálculo).</summary>
    Public ReadOnly Property TalonLargo As Double
        Get
            Return B - a - e1 / 2
        End Get
    End Property

    ''' <summary>Puntera (pie) — proyección hacia el lado del aire.</summary>
    Public ReadOnly Property PieLargo As Double
        Get
            Return a - e1 / 2
        End Get
    End Property

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        ' Si h2 llegó como 0 (archivos legacy), derivarlo.
        If h2 <= 0 AndAlso H > h1 Then h2 = H - h1
        If e2 <= 0 Then e2 = e1
    End Sub
End Class

' =====================================================================
'  Suelo — propiedades estáticas (bajo relleno + bajo cimentación)
' =====================================================================
<Serializable>
Public Class PropsSuelo
    ''' <summary>Ángulo de fricción interna del suelo del relleno, en grados.</summary>
    <OptionalField> Public Phi As Double = 30.0

    ''' <summary>Ángulo de fricción muro-suelo, en grados (0 ≤ delta ≤ phi). 2/3·phi es común.</summary>
    <OptionalField> Public Delta As Double = 20.0

    ''' <summary>Peso específico del suelo de relleno (saturado o húmedo), kN/m³.</summary>
    <OptionalField> Public GammaSuelo As Double = 18.0

    ''' <summary>Peso específico del suelo bajo la zapata (puede diferir del relleno), kN/m³.</summary>
    <OptionalField> Public GammaCimentacion As Double = 18.0

    ''' <summary>Cohesión del suelo bajo la zapata, kPa (usualmente 0 para arenas).</summary>
    <OptionalField> Public Cohesion As Double = 0.0

    ''' <summary>Profundidad de desplante de la zapata (nivel inferior zapata → superficie), m.</summary>
    <OptionalField> Public Df As Double = 1.0

    ''' <summary>Capacidad portante admisible del suelo (kPa) — del estudio geotécnico.</summary>
    <OptionalField> Public CapacidadPortante As Double = 200.0

    ''' <summary>Coeficiente de fricción base-suelo. Si 0, se usa tan(2/3·phi).</summary>
    <OptionalField> Public MuBase As Double = 0.0

    ''' <summary>Si True, Ka y Kp se calculan por teoría; si False, se usan los valores manuales.</summary>
    <OptionalField> Public CalcularKsAutomatico As Boolean = True

    <OptionalField> Public KaManual As Double = 0.333
    <OptionalField> Public KpManual As Double = 3.0
    <OptionalField> Public KoManual As Double = 0.5

    <OptionalField> Public Teoria As eTeoriaEmpuje = eTeoriaEmpuje.Coulomb

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        ' Nada que inicializar — todos los campos tienen valor por default razonable.
    End Sub
End Class

' =====================================================================
'  Suelo sísmico — coeficientes para Mononobe-Okabe
' =====================================================================
<Serializable>
Public Class PropsSueloSismico
    ''' <summary>Si True, se agrega el caso sísmico (Extremo I / Mononobe-Okabe) al análisis.</summary>
    <OptionalField> Public IncluirSismo As Boolean = False

    ''' <summary>Coeficiente sísmico horizontal (aceleración horizontal / g).</summary>
    <OptionalField> Public Kh As Double = 0.15

    ''' <summary>Coeficiente sísmico vertical (aceleración vertical / g); típico 0 o Kh/2.</summary>
    <OptionalField> Public Kv As Double = 0.0

    ''' <summary>Factor de amplificación del empuje activo bajo sismo (si se usa simplificación por Ks).</summary>
    <OptionalField> Public Ks As Double = 1.5

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
    End Sub
End Class

' =====================================================================
'  Sobrecarga de tráfico / vehicular
' =====================================================================
<Serializable>
Public Class PropsSobrecarga
    ''' <summary>Si True, se considera sobrecarga vehicular / variable sobre el relleno.</summary>
    <OptionalField> Public IncluirSobrecarga As Boolean = False

    ''' <summary>Distancia desde el muro al tráfico, en mm (0 = junto al muro, ≥300 = alejado).
    ''' AASHTO Tabla 3.11.6.4 varía Heq con esta distancia.</summary>
    <OptionalField> Public DistanciaTraficoMm As Double = 0.0

    ''' <summary>Sobrecarga variable adicional kPa (uso residencial / parqueadero / etc.).</summary>
    <OptionalField> Public SobrecargaKPa As Double = 10.0

    ''' <summary>Altura equivalente de suelo que representa la sobrecarga (m).
    ''' Para Vial se calcula según AASHTO Tabla 3.11.6.4; para Residencial se deriva de SobrecargaKPa/γ.</summary>
    <OptionalField> Public Heq As Double = 0.6

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
    End Sub
End Class

' =====================================================================
'  Materiales
' =====================================================================
<Serializable>
Public Class PropsMateriales
    <OptionalField> Public Fc As Single = 21.0            ' MPa — resistencia a compresión del concreto
    <OptionalField> Public Fy As Single = 420.0           ' MPa — fluencia del acero longitudinal
    <OptionalField> Public FyEstribo As Single = 420.0    ' MPa — fluencia del acero transversal
    <OptionalField> Public PesoConcreto As Single = 24.0  ' kN/m³
    <OptionalField> Public Recubrimiento As Single = 0.05 ' m — recubrimiento al centro de la barra

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
    End Sub
End Class

' =====================================================================
'  Opciones de análisis — flags que modulan el cálculo
' =====================================================================
<Serializable>
Public Class OpcionesAnalisisMuro
    <OptionalField> Public IncluirLlave As Boolean = False
    <OptionalField> Public IncluirRellenoFrontal As Boolean = True
    ''' <summary>Si True, se recalculan Ka/Kp con fórmula al analizar;
    ''' si False, se usan Suelo.KaManual/KpManual.</summary>
    <OptionalField> Public CalcularKsAutomatico As Boolean = True

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
    End Sub
End Class

' =====================================================================
'  Resultados — se repueblan en cada corrida del servicio
' =====================================================================
<Serializable>
Public Class ResultadosMuro
    <OptionalField> Public FechaCalculo As Date = Date.Now

    ' Coeficientes efectivos usados en el análisis (calculados o manuales)
    <OptionalField> Public Ka As Double
    <OptionalField> Public Kp As Double
    <OptionalField> Public Kae As Double     ' Coulomb-Mononobe-Okabe (dinámico)

    <OptionalField> Public Empujes As New EmpujesMuro()
    <OptionalField> Public Pesos As New List(Of ElementoPeso)()
    <OptionalField> Public Estabilidad As New ChequeosEstabilidad()
    <OptionalField> Public Disenos As New DisenosElementos()

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Empujes Is Nothing Then Empujes = New EmpujesMuro()
        If Pesos Is Nothing Then Pesos = New List(Of ElementoPeso)()
        If Estabilidad Is Nothing Then Estabilidad = New ChequeosEstabilidad()
        If Disenos Is Nothing Then Disenos = New DisenosElementos()
    End Sub
End Class

' Un cuerpo (losa, vástago, relleno, cuña, sobrecarga, etc.) con su peso y brazos.
<Serializable>
Public Class ElementoPeso
    <OptionalField> Public Nombre As String = ""
    <OptionalField> Public Area As Double          ' m²
    <OptionalField> Public PesoEspecifico As Double ' kN/m³
    <OptionalField> Public Peso As Double           ' kN/m (análisis por metro de longitud)
    <OptionalField> Public BrazoX As Double         ' m — desde el talón (lado del relleno)
    <OptionalField> Public MomentoResistente As Double ' kN·m/m — Peso * BrazoX
End Class

<Serializable>
Public Class EmpujesMuro
    ' Caso estático
    <OptionalField> Public Ea_est As Double            ' kN/m — empuje activo total estático
    <OptionalField> Public EaH_est As Double           ' kN/m — componente horizontal
    <OptionalField> Public EaV_est As Double           ' kN/m — componente vertical (friction wall-soil)
    <OptionalField> Public BrazoY_Ea_est As Double     ' m — punto de aplicación (desde la base)
    <OptionalField> Public Esc_est As Double           ' kN/m — empuje adicional por sobrecarga estática
    <OptionalField> Public BrazoY_Esc_est As Double

    ' Caso sísmico (Mononobe-Okabe)
    <OptionalField> Public DeltaEae As Double          ' kN/m — incremento sísmico ΔPae
    <OptionalField> Public BrazoY_DeltaEae As Double

    ' Empuje pasivo frontal (si IncluirRellenoFrontal)
    <OptionalField> Public Ep As Double
    <OptionalField> Public BrazoY_Ep As Double
End Class

<Serializable>
Public Class ChequeosEstabilidad
    <OptionalField> Public CasoEstatico As New ChequeoCaso()
    <OptionalField> Public CasoSismico As ChequeoCaso ' Nothing si no aplica

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If CasoEstatico Is Nothing Then CasoEstatico = New ChequeoCaso()
    End Sub
End Class

<Serializable>
Public Class ChequeoCaso
    <OptionalField> Public FsVolcamiento As Double
    <OptionalField> Public FsVolcamientoAdmisible As Double
    <OptionalField> Public FsDeslizamiento As Double
    <OptionalField> Public FsDeslizamientoAdmisible As Double
    <OptionalField> Public Excentricidad As Double
    <OptionalField> Public ExcentricidadAdmisible As Double     ' B/6 (trapecio) o B/3 (triangular LRFD)
    <OptionalField> Public PresionMax As Double                 ' kPa
    <OptionalField> Public PresionMin As Double                 ' kPa
    <OptionalField> Public PresionAdmisible As Double           ' kPa
    <OptionalField> Public PesoTotal As Double                  ' kN/m
    <OptionalField> Public MomentoResistente As Double          ' kN·m/m
    <OptionalField> Public MomentoVolcante As Double            ' kN·m/m
    <OptionalField> Public FuerzaResistente As Double           ' kN/m (W·μ + Fp llave)
    <OptionalField> Public FuerzaDeslizante As Double           ' kN/m (empuje horizontal)

    ' Factores C/D normalizados — todos ≥ 0.90 cumplen (convención ARCO,
    ' punto único de verdad en Funciones_00_Varias.UMBRAL_CD).
    ' Convención: C/D = capacidad / demanda. Para la capacidad portante y la
    ' excentricidad el admisible es un "techo", así que se invierten para dar
    ' un número en el mismo sentido (≥ 1.0 cumple antes de aplicar el 0.90).

    Public ReadOnly Property FactorVolcamiento As Double
        Get
            If FsVolcamientoAdmisible <= 0 Then Return 0
            If Double.IsInfinity(FsVolcamiento) Then Return 99.0
            Return FsVolcamiento / FsVolcamientoAdmisible
        End Get
    End Property

    Public ReadOnly Property FactorDeslizamiento As Double
        Get
            If FsDeslizamientoAdmisible <= 0 Then Return 0
            If Double.IsInfinity(FsDeslizamiento) Then Return 99.0
            Return FsDeslizamiento / FsDeslizamientoAdmisible
        End Get
    End Property

    Public ReadOnly Property FactorCapacidadPortante As Double
        Get
            If PresionMax <= 0 OrElse PresionAdmisible <= 0 Then Return 0
            Return PresionAdmisible / PresionMax
        End Get
    End Property

    Public ReadOnly Property FactorExcentricidad As Double
        Get
            Dim e = Math.Abs(Excentricidad)
            If ExcentricidadAdmisible <= 0 Then Return 0
            If e <= 0 Then Return 99.0
            Return ExcentricidadAdmisible / e
        End Get
    End Property

    Public ReadOnly Property CumpleVolcamiento As Boolean
        Get
            Return FactorVolcamiento >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleDeslizamiento As Boolean
        Get
            Return FactorDeslizamiento >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleCapacidadPortante As Boolean
        Get
            Return FactorCapacidadPortante >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleExcentricidad As Boolean
        Get
            Return FactorExcentricidad >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleTodo As Boolean
        Get
            Return CumpleVolcamiento AndAlso CumpleDeslizamiento AndAlso
                   CumpleCapacidadPortante AndAlso CumpleExcentricidad
        End Get
    End Property
End Class

<Serializable>
Public Class DisenosElementos
    <OptionalField> Public Vastago As New DisenoElementoMuro()
    <OptionalField> Public Talon As New DisenoElementoMuro()
    <OptionalField> Public Pie As New DisenoElementoMuro()
    <OptionalField> Public Llave As DisenoElementoMuro      ' Nothing si no aplica

    <OnDeserialized>
    Private Sub Rehidratar(ctx As StreamingContext)
        If Vastago Is Nothing Then Vastago = New DisenoElementoMuro()
        If Talon Is Nothing Then Talon = New DisenoElementoMuro()
        If Pie Is Nothing Then Pie = New DisenoElementoMuro()
    End Sub
End Class

''' <summary>
''' Resultado de diseño/revisión para un elemento del muro (vástago, talón, pie, llave).
''' Programa de revisión: el servicio calcula As_req y Vu; la UI permite al usuario
''' ingresar barra + separación (As_prov) para que las propiedades FactorFlexion y
''' FactorCortante den el C/D de revisión que es el entregable al cliente.
''' Mu / Vu son últimos (ya con factores de carga aplicados según la normativa).
''' </summary>
<Serializable>
Public Class DisenoElementoMuro
    <OptionalField> Public Nombre As String = ""
    <OptionalField> Public B As Double          ' m — ancho de la sección (típico 1.0 para análisis por metro)
    <OptionalField> Public H As Double          ' m — espesor
    <OptionalField> Public d As Double          ' m — peralte efectivo

    ' Demandas calculadas por el servicio (envolvente estático / sísmico)
    <OptionalField> Public Vu As Double         ' kN — cortante último
    <OptionalField> Public Mu As Double         ' kN·m — momento último
    <OptionalField> Public AsReq As Double      ' mm²/m — acero principal requerido por Mu
    <OptionalField> Public AsReqTemperatura As Double ' mm²/m — acero mínimo por temperatura y retracción

    ' Capacidades del concreto (independientes del acero colocado)
    <OptionalField> Public PhiVc As Double      ' kN — capacidad a cortante del concreto solo (sin estribos)

    ' Refuerzo PROPUESTO por el servicio (el usuario puede sobrescribirlo en la UI)
    <OptionalField> Public BarraPrincipal As String = "#5"
    <OptionalField> Public SeparacionCm As Double
    <OptionalField> Public BarraTemperatura As String = "#4"
    <OptionalField> Public SeparacionTempCm As Double

    ' Acero COLOCADO — se calcula desde Barra + Separación. El servicio los setea con la
    ' propuesta inicial; si el usuario cambia barra/separación, llamar RecalcularAsProv().
    <OptionalField> Public AsProv As Double             ' mm²/m principal colocado
    <OptionalField> Public AsProvTemperatura As Double  ' mm²/m temperatura por cara

    ''' <summary>C/D de flexión: AsProv / AsReq. ≥ 1.0 cumple. 0 = sin cálculo.</summary>
    Public ReadOnly Property FactorFlexion As Double
        Get
            If AsReq <= 0 Then Return 0
            If AsProv <= 0 Then Return 0
            Return AsProv / AsReq
        End Get
    End Property

    ''' <summary>C/D de cortante: PhiVc / Vu. ≥ 1.0 cumple. 0 = sin cálculo.</summary>
    Public ReadOnly Property FactorCortante As Double
        Get
            If Vu <= 0 Then Return 0
            If PhiVc <= 0 Then Return 0
            Return PhiVc / Vu
        End Get
    End Property

    ''' <summary>C/D de temperatura: AsProvTemperatura / AsReqTemperatura.</summary>
    Public ReadOnly Property FactorTemperatura As Double
        Get
            If AsReqTemperatura <= 0 Then Return 0
            If AsProvTemperatura <= 0 Then Return 0
            Return AsProvTemperatura / AsReqTemperatura
        End Get
    End Property

    Public ReadOnly Property CumpleCortante As Boolean
        Get
            Return FactorCortante >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleFlexion As Boolean
        Get
            Return FactorFlexion >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleTemperatura As Boolean
        Get
            Return FactorTemperatura >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property CumpleTodo As Boolean
        Get
            Return CumpleFlexion AndAlso CumpleCortante
        End Get
    End Property
End Class
