Imports ARCO.Refuerzo
Imports ARCO.eNumeradores
Imports System.Runtime.Serialization

<Serializable>
Public Class cZapata

    Public Property Nombre As String
    Public Property Label_joint As String

    '----------------------
    ' Geometría 
    '----------------------
    ' Zapata
    '____________________________
    Public Property L_b As Double ' Ancho (m)
    Public Property L_h As Double ' Largo (m)
    Public Property e As Double   ' espesor (m)
    Public Property rec As Double ' recubrimiento (m)
    Public Property d As Double   ' peralte efectivo (m)
    ' Pedestal
    '____________________________
    Public Property b As Double ' Ancho (m)
    Public Property h As Double ' Largo (m)

    '----------------------
    ' Materiales
    '----------------------
    Public Property fc As Double   ' concreto (MPa)
    Public Property fy As Double   ' acero (MPa)

    '----------------------
    ' Suelo
    '----------------------
    Public Property qAdm_Est As Double   ' capacidad admisible (kN/m2)
    Public Property qAdm_Din As Double
    Public Property gammaSuelo As Double
    Public Property mu As Double     ' coef. fricción suelo–concreto

    Public Property FD_E As Double
    Public Property FD_D As Double

    ''' <summary>
    ''' Profundidad de desplante desde el terreno hasta el fondo de la zapata (m).
    ''' Determina la altura de suelo y de pedestal por encima de la cara superior
    ''' de la zapata cuando se considera peso estabilizante.
    ''' </summary>
    <OptionalField> Public Df As Double

    ''' <summary>
    ''' Peso específico del concreto (kN/m³) para el peso de zapata y pedestal.
    ''' Por defecto 24, se corrige en OnDeserialized cuando el archivo viene sin
    ''' este campo.
    ''' </summary>
    <OptionalField> Public gammaConcreto As Double = 24.0

    '----------------------
    ' Acero suministrado
    '----------------------
    Public Property Refuerzos As New List(Of cRefuerzo)
    Public Property Rho_L1 As Double
    Public Property Rho_L2 As Double

    Public Property PesoPropio As Double
    Public Property NumeroJoint As String

    '----------------------
    ' Posición en la cimentación
    '----------------------
    ''' <summary>
    ''' Central, Medianera o Esquinera. Determina el perímetro crítico de
    ''' punzonamiento y el factor alfa_s (NSR-10 C.11.11). Campo, no propiedad,
    ''' porque OptionalField no admite propiedades.
    ''' Los proyectos guardados antes de esta versión abren como Central, que es
    ''' lo que el programa asumía para todas — ver OnDeserialized.
    ''' </summary>
    <OptionalField> Public TipoApoyo As eTipoApoyoZapata = eTipoApoyoZapata.Central

    ''' <summary>
    ''' True cuando el ingeniero fijó el tipo a mano. La clasificación
    ''' automática por geometría no lo sobrescribe: un voladizo, una junta de
    ''' dilatación o una zapata combinada rompen la inferencia y la corrección
    ''' manual tiene que sobrevivir a un recálculo.
    ''' </summary>
    <OptionalField> Public TipoApoyoManual As Boolean = False

    ''' <summary>Coordenadas del nodo en planta (m). Vienen de la hoja de Joints.</summary>
    <OptionalField> Public CoordX As Double
    <OptionalField> Public CoordY As Double
    <OptionalField> Public TieneCoordenadas As Boolean = False

    ' Combinaciones provenientes de ETABS
    Public Lista_Combinaciones_Estaticas As New List(Of cCombinacionPila)
    Public Lista_Combinaciones_Dinamicas As New List(Of cCombinacionPila)

    ' Resultados por combinación
    Public Property Resultados As New Dictionary(Of String, ResultadoZapata)

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Refuerzos Is Nothing Then Refuerzos = New List(Of cRefuerzo)
        If Lista_Combinaciones_Estaticas Is Nothing Then Lista_Combinaciones_Estaticas = New List(Of cCombinacionPila)
        If Lista_Combinaciones_Dinamicas Is Nothing Then Lista_Combinaciones_Dinamicas = New List(Of cCombinacionPila)
        If Resultados Is Nothing Then Resultados = New Dictionary(Of String, ResultadoZapata)
        ' TipoApoyo = Central por defecto en archivos anteriores: es exactamente
        ' lo que el cálculo asumía antes, así que abrir un proyecto viejo no
        ' cambia ningún resultado hasta que se clasifique o se marque a mano.
        ' gammaConcreto = 24 kN/m³ si el archivo no lo trae: BinaryFormatter no
        ' aplica el inicializador en línea al deserializar, así que hay que
        ' repararlo aquí. Sin esto, activar peso estabilizante en un proyecto
        ' viejo daría W_zapata = 0.
        If gammaConcreto <= 0 Then gammaConcreto = 24.0
    End Sub

End Class

<Serializable>
Public Class cRefuerzo

    Public Property Direccion As eDireccionRefuerzo   ' L1 / L2
    Public Property Tipo As eTipoRefuerzo              ' Inferior / Superior
    Public Property Diametro As String                 '#4, #5, etc
    Public Property Diametro_mm As Double
    Public Property AreaBarra As Double                ' mm2
    Public Property Cantidad As Double
    Public Property Espaciamiento As Double            ' m

    Public ReadOnly Property AsTotal As Double
        Get
            Return AreaBarra * Cantidad
        End Get
    End Property

End Class
