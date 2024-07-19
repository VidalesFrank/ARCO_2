<Serializable>
Public Class Proyecto

    Public Ruta As String

    ' =========== INFORMACIÓN DE PROYECTO =============
    Public Nombre As String
    Public Direccion As String
    Public Ciudad As String
    Public Departamento As String
    Public Propietario As String
    Public Disenador As String
    Public SistemaEstructural As eNumeradores.eSistemaEstructural
    Public Area_Planta As Single
    Public Imagen As Bitmap
    Public Ruta_Imagen As String
    Public Persona_Responsable As eNumeradores.eResponsables
    Public NDE As eNumeradores.eDisipasion
    Public ZAS As eNumeradores.eAMZ

    Public Disipacion As String
    Public Year As Integer
    Public SistemaE As String
    Public GrupoU As String
    Public grupoUso As eNumeradores.eGrupoUso
    Public T_Suelo As String
    Public tipoSuelo As eNumeradores.eGrupoSuelo

    Public Combinacion_SismoX As String
    Public Combinacion_SismoY As String

    Public Lista_Pisos As List(Of cPiso)
    Public NumPisos As Integer

    '===== INFORMACIÓN RECIBIDA =========
    Public Planos_Arq As Boolean
    Public Planos_Est As Boolean
    Public Memorias_Calculo As Boolean
    Public Estudio_Suelos As Boolean

    '===== INFORMACIÓN DINÁMICA ==========
    Public Deriva_X As New List(Of cDeriva)
    Public Deriva_Y As New List(Of cDeriva)
    Public DerivaCr_X As New List(Of cDeriva)
    Public DerivaCr_Y As New List(Of cDeriva)

    '=============== PILAS ==============
    Public Pilas As New cPilas()

    '============== COLUMNAS ==============
    Public Columnas As New cColumnas()

    '============== Muros ==============
    Public Muros As New cMuros()

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
