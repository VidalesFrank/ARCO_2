Imports System.Reflection
Imports System.Runtime.Serialization

<Serializable>
Public Class Muro
    Implements ISerializable

    Public Name As String
    Public Label As String
    Public Direccion As eNumeradores.eDireccion
    Public TipoMuro As eNumeradores.eTipoMuro

    Public Lista_Secciones As New List(Of SeccionMuro)

    Public Ref_Modificado_Muros As Boolean

    Public N_Pisos As Integer
    Public Lw As Single
    Public tw As Single
    Public Hw As Single
    Public Ar As Single
    Public Ag_M As Single
    Public fc_Base As Single

    Public Coor_X As Single
    Public Coor_Y As Single

    Public LV_EB_I_Col_Esp As Single
    Public LV_EB_I_Col_NoEsp As Single
    Public LV_EB_I_Req_Def As Single
    Public LV_EB_I_Req_Esf As Single

    Public LV_EB_D_Col_Esp As Single
    Public LV_EB_D_Col_NoEsp As Single
    Public LV_EB_D_Req_Def As Single
    Public LV_EB_D_Req_Esf As Single

    Public Pmax_G As Single
    Public Pmax_D As Single
    Public Vmax_S As Single
    Public ALR_G As Single
    Public ALR_D As Single
    Public Porc_Vs As Single
    Public Porc_Vs_Geo As Single

    Public Sub New()
    End Sub

    ' Constructor de deserialización. Lee cada campo del stream por reflexión y, para los
    ' enums que históricamente se guardaron como String (Direccion y TipoMuro), convierte.
    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        Dim t = Me.GetType()
        For Each entry As SerializationEntry In info
            Dim fi = t.GetField(entry.Name, BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)
            If fi Is Nothing Then Continue For
            If fi.IsNotSerialized Then Continue For

            Dim value = entry.Value

            If fi.FieldType Is GetType(eNumeradores.eDireccion) AndAlso TypeOf value Is String Then
                value = ParsearDireccion(CStr(value))
            ElseIf fi.FieldType Is GetType(eNumeradores.eTipoMuro) AndAlso TypeOf value Is String Then
                value = ParsearTipoMuro(CStr(value))
            End If

            Try
                fi.SetValue(Me, value)
            Catch
                ' Si el stream trae un tipo incompatible imprevisto, dejamos el default
                ' del campo (no abortamos la deserialización completa del proyecto).
            End Try
        Next

        If Lista_Secciones Is Nothing Then Lista_Secciones = New List(Of SeccionMuro)
    End Sub

    Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
        Dim t = Me.GetType()
        For Each fi In t.GetFields(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance)
            If fi.IsNotSerialized Then Continue For
            info.AddValue(fi.Name, fi.GetValue(Me))
        Next
    End Sub

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Lista_Secciones Is Nothing Then Lista_Secciones = New List(Of SeccionMuro)
    End Sub

    Private Shared Function ParsearDireccion(s As String) As eNumeradores.eDireccion
        If String.IsNullOrWhiteSpace(s) Then Return eNumeradores.eDireccion.X
        Dim raw = s.Trim()
        If raw.Equals("Y", StringComparison.OrdinalIgnoreCase) Then Return eNumeradores.eDireccion.Y
        If raw.Equals("X", StringComparison.OrdinalIgnoreCase) Then Return eNumeradores.eDireccion.X
        Dim parsed As eNumeradores.eDireccion
        If [Enum].TryParse(Of eNumeradores.eDireccion)(raw, True, parsed) Then Return parsed
        Return eNumeradores.eDireccion.X
    End Function

    Private Shared Function ParsearTipoMuro(s As String) As eNumeradores.eTipoMuro
        If String.IsNullOrWhiteSpace(s) Then Return eNumeradores.eTipoMuro.Protagonico
        Dim raw = s.Trim()
        If raw.Equals("Complemento", StringComparison.OrdinalIgnoreCase) Then Return eNumeradores.eTipoMuro.Complemento
        If raw.Equals("Protagonico", StringComparison.OrdinalIgnoreCase) Then Return eNumeradores.eTipoMuro.Protagonico
        Dim parsed As eNumeradores.eTipoMuro
        If [Enum].TryParse(Of eNumeradores.eTipoMuro)(raw, True, parsed) Then Return parsed
        Return eNumeradores.eTipoMuro.Protagonico
    End Function

End Class
