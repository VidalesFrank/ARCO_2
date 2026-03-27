<Serializable>
Public Class cCombinacionPila

    Public Property Story As String
    Public Property JointLabel As String
    Public Property UniqueName As String
    Public Property LoadCase As String
    Public Property FX As Single
    Public Property FY As Single
    Public Property FZ As Single
    Public Property MX As Single
    Public Property MY As Single
    Public Property MZ As Single


    Public Overrides Function ToString() As String
        Return $"{Story} - {JointLabel} - {UniqueName} ({LoadCase}, {FX}, {FY}, {FZ}, {MX}, {MY}, {MZ})"
    End Function


End Class
