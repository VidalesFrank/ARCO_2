<Serializable>
Public Class cElementos
    Public Property Pilas As New cPilas()
    Public Property Columnas As New cColumnas()
    Public Property Muros As New cMuros()
    Public Property Frames As New List(Of cFrame)
    Public Property Joints As New List(Of cJoint)
    Public Property Zapatas As New cZapatas()
    Public Property Vigas As New cVigas()

    Public Property Grids As New cGrids()

End Class
<Serializable>
Public Class cGrids
    Public Property GridLines As New List(Of cGridLine)
End Class
