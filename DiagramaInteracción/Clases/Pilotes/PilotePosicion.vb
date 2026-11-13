Imports System.Runtime.Serialization

''' <summary>
''' Posición de un pilote dentro de un TipoDado, en coordenadas locales del
''' dado (origen en el centroide del polígono de planta del dado, en metros).
''' Referencia al TipoPilote por nombre — sobrevive a BinaryFormatter mejor que
''' un puntero, y es el mismo patrón que usan SeccionMuro y cZapata.
''' </summary>
<Serializable>
Public Class PilotePosicion

    Public Property X_Local As Double
    Public Property Y_Local As Double

    ''' <summary>Nombre del TipoPilote en el catálogo del proyecto.</summary>
    Public Property NombreTipoPilote As String = ""

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If NombreTipoPilote Is Nothing Then NombreTipoPilote = ""
    End Sub

    Public Sub New()
    End Sub

    Public Sub New(x As Double, y As Double, nombreTipo As String)
        Me.X_Local = x
        Me.Y_Local = y
        Me.NombreTipoPilote = nombreTipo
    End Sub

End Class
