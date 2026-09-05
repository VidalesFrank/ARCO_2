Imports System.Runtime.Serialization

<Serializable>
Public Class cGridLine
    Public Property GridSystem As String
    ''' "X" = X Cartesian (línea vertical), "Y" = Y Cartesian (línea horizontal),
    ''' "G" = General Cartesian (segmento libre definido por X1,Y1,X2,Y2).
    Public Property Direction As String
    Public Property GridID As String         ' A, B, 1, 2, etc.
    Public Property Visible As Boolean
    Public Property BubbleLocation As String ' Start / End
    Public Property Ordinate As Double       ' Coordenada (m) — solo para tipos X e Y

    ' Coordenadas del segmento — solo para tipo General (Cartesian)
    <OptionalField> Public X1 As Double
    <OptionalField> Public Y1 As Double
    <OptionalField> Public X2 As Double
    <OptionalField> Public Y2 As Double

    ''' True si es tipo General (Cartesian): usa X1,Y1,X2,Y2 en lugar de Ordinate.
    Public ReadOnly Property EsTipoGeneral As Boolean
        Get
            Return Direction = "G"
        End Get
    End Property

End Class
