<Serializable>
Public Class Columna
    Public Name_Elemento As String
    Public Name_Label As String
    Public Secciones_Principal As Boolean
    Public Secciones_Similar As Boolean

    Public Ref_Modificado As Boolean
    Public Lista_Tramos_Columnas As New List(Of Tramo_Columna)

    Public Lista_ALR As New List(Of ALR)

    Public Lista_F As New List(Of Single)
    Public Lista_F_Piso As New List(Of String)

    <Serializable>
    Public Class ALR
        Public Combinacion As String
        Public ALR As Single
    End Class

End Class
