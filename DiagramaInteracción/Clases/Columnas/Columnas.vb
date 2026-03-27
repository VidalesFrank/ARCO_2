Imports ARCO.eNumeradores

<Serializable>
Public Class Columna

    ' ===== Identificación del elemento =====
    Public Name_Elemento As String
    Public Name_Label As String

    ' ===== Sección =====
    Public Property TipoSeccion As T_Seccion           ' Rectangular, T, L, Circular
    Public Property Dimensiones As cDimensionesSeccion
    Public Property Secciones_Principal As Boolean
    Public Property Secciones_Similar As Boolean

    ' ===== Geometría espacial =====
    Public Property Coordenadas As cCoordenadasElemento   ' Coordenadas de Inicio / Fin
    Public Property PisoInicial As String
    Public Property PisoFinal As String

    ' ===== Estado y edición =====
    Public Ref_Modificado As Boolean

    ' ===== Tramos =====
    Public Lista_Tramos_Columnas As New List(Of Tramo_Columna)

    ' ===== Resultados ALR =====
    Public Lista_ALR As New List(Of ALR)

    ' ===== Fuerzas verticales =====
    Public Lista_F As New List(Of Single)
    Public Lista_F_Piso As New List(Of String)

    <Serializable>
    Public Class ALR
        Public Combinacion As String
        Public ALR As Single
    End Class

    <Serializable>
    Public Class cDimensionesSeccion
        Public Property B As Single       ' Ancho
        Public Property H As Single       ' Alto
        Public Property D As Single       ' Diámetro (solo circular)
        Public Property EspesorAla As Single
        Public Property EspesorAlma As Single

        Public Function Area() As Single
            ' Puedes programar la fórmula dependiendo del tipo de sección
            Return 0
        End Function

    End Class

    <Serializable>
    Public Class cCoordenadasElemento
        Public Property X As Single
        Public Property Y As Single
    End Class

End Class
