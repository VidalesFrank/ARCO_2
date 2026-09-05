''' <summary>
''' Candidato a columna detectado desde ETABS (Frame o Pier).
''' Usado en Form_02_SeleccionColumnas para la vista en planta y selección previa a la importación.
''' </summary>
Public Class cCandidatoColumna
    Public Property Label As String = ""
    Public Property Tipo As String = ""          ' "Frame" o "Pier"
    Public Property Story As String = ""
    Public Property Seccion As String = ""
    Public Property B As Double = 0              ' dimensión menor [m]
    Public Property H As Double = 0              ' dimensión mayor [m]
    Public Property X As Double = 0              ' centroide X en planta [m]
    Public Property Y As Double = 0              ' centroide Y en planta [m]
    Public Property Seleccionado As Boolean = True
    Public Property NCombos As Integer = 0
End Class
