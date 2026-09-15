Imports System.Runtime.Serialization

<Serializable>
Public Class cInfoProyecto
    Public Property Nombre As String
    Public Property Direccion As String
    Public Property Ciudad As String
    Public Property Departamento As String
    Public Property Propietario As String
    Public Property Designer As String
    Public Property Year As Integer
    Public Property NPisos As Integer
    Public Property Area As Single
    Public Property SistemaEstructural As eNumeradores.eSistemaEstructural
    Public Property GrupoUso As eNumeradores.eGrupoUso
    Public Property TipoSuelo As eNumeradores.eGrupoSuelo
    Public Property Persona_Responsable As eNumeradores.eResponsables
    Public Property Imagen As Bitmap
    Public Property Ruta_Imagen As String

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = ""
        If Direccion Is Nothing Then Direccion = ""
        If Ciudad Is Nothing Then Ciudad = ""
        If Departamento Is Nothing Then Departamento = ""
        If Propietario Is Nothing Then Propietario = ""
        If Designer Is Nothing Then Designer = ""
        If Ruta_Imagen Is Nothing Then Ruta_Imagen = ""
    End Sub

End Class
