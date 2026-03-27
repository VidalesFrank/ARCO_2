<Serializable>
Public Class cJoint
    Public Property Story As String
    Public Property ElementLabel As String
    Public Property ObjectType As String
    Public Property ObjectLabel As String
    Public Property GlobalX As Double
    Public Property GlobalY As Double
    Public Property GlobalZ As Double

    Public Overrides Function ToString() As String
        Return $"{Story} - {ElementLabel} - {ObjectLabel} ({GlobalX}, {GlobalY}, {GlobalZ})"
    End Function

End Class
