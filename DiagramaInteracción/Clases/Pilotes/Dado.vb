Imports System.Runtime.Serialization

''' <summary>
''' Instancia de dado asociada a un apoyo (joint) del modelo estructural.
''' Referencia a su TipoDado por nombre — un TipoDado alimenta N Dado.
''' Guarda las combinaciones de carga del apoyo y los resultados calculados.
''' </summary>
<Serializable>
Public Class Dado

    ''' <summary>Label del joint de ETABS asociado.</summary>
    Public Property JointLabel As String = ""

    ''' <summary>Coordenada global X del apoyo [m].</summary>
    Public Property X_Global As Double

    ''' <summary>Coordenada global Y del apoyo [m].</summary>
    Public Property Y_Global As Double

    ''' <summary>
    ''' Rotación del dado en planta respecto al norte del modelo [grados].
    ''' Cuando el dado no está orientado con los ejes globales, se rota antes
    ''' de aplicar las cargas (los momentos M2/M3 son globales).
    ''' </summary>
    Public Property AnguloPlanta As Double = 0.0

    ''' <summary>Nombre del TipoDado asignado (catálogo cPilotes.TiposDado).</summary>
    Public Property NombreTipoDado As String = ""

    ''' <summary>
    ''' Cuando True, blindaje contra la clasificación automática: ningún
    ''' recálculo automático pisará la asignación. Mismo mecanismo que
    ''' cApoyo.TipoApoyoManual en el módulo de zapatas.
    ''' </summary>
    <OptionalField> Public AsignacionManual As Boolean = False

    ''' <summary>
    ''' Origen del apoyo en ETABS: "Frame" (Joint Reactions en nodo base) o
    ''' "Pier" (Pier Forces Location=Bottom). Mismo campo que existe en
    ''' cCombinacionPila para trazabilidad.
    ''' </summary>
    <OptionalField> Public SourceType As String = ""

    ''' <summary>Label del elemento ETABS (Column, Frame o Pier).</summary>
    <OptionalField> Public SourceName As String = ""

    ''' <summary>Z global del nodo base [m]. Útil para visualizaciones 3D.</summary>
    <OptionalField> Public Z_Base As Double

    ''' <summary>
    ''' Combinaciones de carga del apoyo (importadas de Joint Reactions o
    ''' ingresadas manualmente). FZ > 0 = compresión sobre el apoyo.
    ''' </summary>
    Public Property Combinaciones As New List(Of CombinacionDado)

    ' ── Resultados del último cálculo (fields para soportar OptionalField) ─
    <OptionalField> Public Pu_Pilote_Max As Double
    <OptionalField> Public Pu_Pilote_Min As Double
    <OptionalField> Public Combo_Gob_Compresion As String = ""
    <OptionalField> Public Combo_Gob_Traccion As String = ""
    <OptionalField> Public CD_Compresion As Double
    <OptionalField> Public CD_Traccion As Double

    ''' <summary>
    ''' C/D del chequeo estructural axial (φPn vs Pu_max) del pilote más cargado.
    ''' 0 si no se pudo evaluar (TipoPilote sin fc/fy/barras).
    ''' </summary>
    <OptionalField> Public CD_Compresion_Estructural As Double

    ''' <summary>
    ''' C/D del chequeo estructural a tracción (φTn vs |Pu_min|) del pilote más
    ''' traccionado. 0 (sentinela) si no hay tracción o no se pudo evaluar.
    ''' </summary>
    <OptionalField> Public CD_Traccion_Estructural As Double

    ' ── STM (NSR-10 C.23): peor C/D de cada categoría del modelo ──────────
    ''' <summary>C/D mínimo entre todos los puntales del modelo STM. 0 = no evaluado.</summary>
    <OptionalField> Public CD_STM_Puntal As Double

    ''' <summary>C/D mínimo entre todos los tensores del modelo STM. 0 = no evaluado.</summary>
    <OptionalField> Public CD_STM_Tensor As Double

    ''' <summary>C/D mínimo entre todos los nodos del modelo STM. 0 = no evaluado.</summary>
    <OptionalField> Public CD_STM_Nodo As Double

    ''' <summary>Área de acero requerida por STM para tensores en dirección X [cm²].</summary>
    <OptionalField> Public As_STM_X_cm2 As Double

    ''' <summary>Área de acero requerida por STM para tensores en dirección Y [cm²].</summary>
    <OptionalField> Public As_STM_Y_cm2 As Double

    ''' <summary>C/D del tensor en X = As_provisto_X / As_requerido_X. 0 = no aplica.</summary>
    <OptionalField> Public CD_STM_Tensor_X As Double

    ''' <summary>C/D del tensor en Y = As_provisto_Y / As_requerido_Y. 0 = no aplica.</summary>
    <OptionalField> Public CD_STM_Tensor_Y As Double

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If JointLabel Is Nothing Then JointLabel = ""
        If NombreTipoDado Is Nothing Then NombreTipoDado = ""
        If Combinaciones Is Nothing Then Combinaciones = New List(Of CombinacionDado)
        If Combo_Gob_Compresion Is Nothing Then Combo_Gob_Compresion = ""
        If Combo_Gob_Traccion Is Nothing Then Combo_Gob_Traccion = ""
    End Sub

End Class

''' <summary>
''' Combinación de carga de un apoyo con pilotes. Convención: FZ > 0 =
''' compresión sobre el apoyo (coincidente con Joint Reactions de ETABS).
''' MX, MY, VX, VY en ejes globales del modelo.
''' </summary>
<Serializable>
Public Class CombinacionDado

    Public Property LoadCase As String = ""
    Public Property FZ As Double
    Public Property MX As Double
    Public Property MY As Double
    Public Property VX As Double
    Public Property VY As Double

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If LoadCase Is Nothing Then LoadCase = ""
    End Sub

End Class
