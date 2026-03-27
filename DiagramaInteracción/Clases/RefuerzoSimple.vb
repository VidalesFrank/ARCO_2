
<Serializable>
Public Class RefuerzoSimple
    Public Id_Patron As Integer
    Public Name_Barra As String
    Public Db As Single
    Public Asb As Single
    Public Coordenada_X As Single
    Public Coordenada_Y As Single

    Public Sub New(id_patron As Integer, name_Barra As String, db As Single, asb As Single, coord_X As Single, coord_Y As Single)
        Me.Id_Patron = id_patron
        Me.Name_Barra = name_Barra
        Me.Db = db
        Me.Asb = asb
        Me.Coordenada_X = coord_X
        Me.Coordenada_Y = coord_Y
    End Sub

End Class
