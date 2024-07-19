<Serializable>
Public Class cColumnas

    Public Elementos_Frame As Boolean
    Public Elementos_Pier As Boolean

    Public Verificacion_Flexo_Compresion As Boolean
    Public Verificacion_Cortante As Boolean
    Public Verificacion_Confinamiento As Boolean
    Public Verificacion_ALR As Boolean
    Public Verificacion_Pandeo As Boolean

    Public Info_Diseño As Boolean
    Public Info_Secciones As Boolean
    Public Info_Cortante As Boolean
    Public Info_Fuerzas As Boolean

    Public Lista_Pisos As List(Of String)
    Public Lista_fc As List(Of Single)
    Public Lista_Columnas As New List(Of Columna)
    Public Lista_Combinaciones As New List(Of String)
    Public Lista_Combinaciones_ALR As New List(Of String)
    Public Lista_Combinaciones_Grafico_ALR As New List(Of String)

End Class
