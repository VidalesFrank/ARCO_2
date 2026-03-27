<Serializable>
Public Class Proyecto

    ' Ruta del archivo principal
    Public Property Ruta As String

    ' Información general
    Public Property Info As New cInfoProyecto()

    ' Parámetros sísmicos del diseño
    Public Property ParametrosSismicos As New cParametrosSismicos()

    ' Análisis global estructural
    Public Property ResultadosGlobales As New cResultadosGlobales()

    ' Elementos estructurales
    Public Property Elementos As New cElementos()

    ' Tablas importadas de ETABS
    Public Property TablasEtabs As New cTablasETABS()

    ' Pisos del modelo
    Public Property Pisos As New List(Of cPiso)

    Public Property NumeroPisos As Integer

    Public Property Grids As New cGrids()


    'Public Ruta As String

    '' =========== INFORMACIÓN DE PROYECTO =============
    'Public Nombre As String
    'Public Direccion As String
    'Public Ciudad As String
    'Public Departamento As String
    'Public Propietario As String
    'Public Disenador As String
    'Public SistemaEstructural As eNumeradores.eSistemaEstructural
    'Public Area_Planta As Single
    'Public Imagen As Bitmap
    'Public Ruta_Imagen As String
    'Public Persona_Responsable As eNumeradores.eResponsables
    'Public NDE As eNumeradores.eDisipasion
    'Public ZAS As eNumeradores.eAMZ

    'Public Disipacion As String
    'Public Year As Integer
    'Public SistemaE As String
    'Public GrupoU As String
    'Public grupoUso As eNumeradores.eGrupoUso
    'Public T_Suelo As String
    'Public tipoSuelo As eNumeradores.eGrupoSuelo

    'Public Combinacion_SismoX As String
    'Public Combinacion_SismoY As String

    'Public Lista_Pisos As List(Of cPiso)
    'Public NumPisos As Integer

    ''===== INFORMACIÓN RECIBIDA =========
    'Public Planos_Arq As Boolean
    'Public Planos_Est As Boolean
    'Public Memorias_Calculo As Boolean
    'Public Estudio_Suelos As Boolean

    ''===== INFORMACIÓN DINÁMICA ==========
    'Public Deriva_X As New List(Of cDeriva)
    'Public Deriva_Y As New List(Of cDeriva)
    'Public DerivaCr_X As New List(Of cDeriva)
    'Public DerivaCr_Y As New List(Of cDeriva)

    ''=============== PILAS ==============
    'Public Pilas As New cPilas()

    ''============== COLUMNAS ==============
    'Public Columnas As New cColumnas()

    ''============== Muros ==============
    'Public Muros As New cMuros()

    ''========== NUEVA INFORMACIÓN DE LA ESTRUCTURA ===============
    '' ====== NUEVAS TABLAS DE RESULTADOS ======
    'Public Tabla_Joints As DataTable
    'Public Tabla_Frames As DataTable
    'Public Tabla_Shells As DataTable
    'Public Tabla_StoryDrifts As DataTable

    '' ================ JOINTS ================
    'Public Joints As New List(Of cJoint)

    '' ================ FRAMES ================
    'Public Frames As New List(Of cFrame)


End Class

<Serializable>
Public Class cPiso

    Public Nombre As String
    Public CoorZ As Single

End Class

<Serializable>
Public Class cDeriva

    Public Piso As cPiso
    Public Deriva As Single

End Class
