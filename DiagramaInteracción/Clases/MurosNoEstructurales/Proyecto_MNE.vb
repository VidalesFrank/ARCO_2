<Serializable>
Public Class Proyecto_MNE

    Public Nombre As String
    Public Ruta As String

    Public Muro_Divisorio As Boolean
    Public Muro_Antepecho As Boolean

    Public Aa As Single
    Public Av As Single
    Public Fa As Single
    Public Fv As Single
    Public Imp As Single
    Public Sa As Single
    Public As_ As Single
    Public Rp As Single
    Public ap As Single
    Public fm As Single
    Public Peso_Especifico As Single
    Public B As Single
    Public B_Efect As Single
    Public N_Pisos As Single
    Public Hx As Single
    Public Hn As Single
    Public Heq As Single

    Public Vn As Single
    Public Vu_Divisorio As Single
    Public Vu_Antepecho As Single

    Public Opcion_Barra_Flexion As Integer

    ' <OptionalField> permite abrir archivos guardados antes de que este campo existiera.
    <System.Runtime.Serialization.OptionalField>
    Public Lista_Alturas_Pisos As List(Of Single)

    Public Lista_Divisorios As New List(Of Divisorio)
    Public Lista_Antepechos As New List(Of Antepecho)

    <System.Runtime.Serialization.OnDeserialized>
    Private Sub OnDeserialized(ctx As System.Runtime.Serialization.StreamingContext)
        If Lista_Alturas_Pisos Is Nothing Then Lista_Alturas_Pisos = New List(Of Single)
        If Lista_Divisorios Is Nothing Then Lista_Divisorios = New List(Of Divisorio)
        If Lista_Antepechos Is Nothing Then Lista_Antepechos = New List(Of Antepecho)
    End Sub

    <Serializable>
    Public Class Divisorio

        Public Piso As String
        Public Hw As Single
        Public Hx As Single
        Public b As Single
        Public Wm As Single
        Public ax As Single
        Public Fp As Single
        Public Presion As Single
        Public Mu As Single
        Public Vu As Single
        Public Acero As Single
        Public Separacion As Single

    End Class
    <Serializable>
    Public Class Antepecho

        Public Altura As Single
        Public b As Single
        Public B_Efec As Single
        Public Am As Single
        Public Mu As Single
        Public Vu As Single
        Public Acero As Single
        Public Separacion As Single

    End Class

End Class
