Imports System.Runtime.Serialization

''' <summary>
''' Plantilla de un cabezal (dado) de pilotes. Es el "patrón" al estilo
''' SeccionMuro.S_Patron: una sola instancia se referencia por nombre desde
''' múltiples Dado (uno por apoyo), eliminando duplicación de datos.
'''
''' Guarda la geometría de planta como una lista arbitraria de vértices
''' (soporta pentágonos, hexágonos, formas en L, etc.) y los pilotes como
''' una lista de posiciones en coordenadas locales. El origen local está en
''' el centroide del polígono.
''' </summary>
<Serializable>
Public Class TipoDado

    ''' <summary>Nombre único del tipo. Clave para referencia por nombre.</summary>
    Public Property Nombre As String = ""

    ''' <summary>Descripción opcional (uso, criterio de diseño, etc.).</summary>
    Public Property Descripcion As String = ""

    ''' <summary>
    ''' Vértices del polígono de planta del dado, en coordenadas locales [m].
    ''' Orden CCW para que las áreas salgan positivas.
    ''' </summary>
    Public Property Poligono As New List(Of Punto2D)

    ''' <summary>Espesor (altura) del dado [m].</summary>
    Public Property Espesor As Double = 0.8

    ''' <summary>Recubrimiento desde borde a cara de pilote [m].</summary>
    Public Property Recubrimiento As Double = 0.075

    ''' <summary>Pilotes contenidos en el dado.</summary>
    Public Property Pilotes As New List(Of PilotePosicion)

    ''' <summary>Separación mínima requerida entre pilotes (múltiplo de d).</summary>
    Public Property FactorSeparacionMinima As Double = 3.0

    ''' <summary>fc del concreto del dado [MPa]. Reservado para STM v1.1.</summary>
    Public Property Fc As Double = 21.0

    ''' <summary>fy del acero del dado [MPa]. Reservado para STM v1.1.</summary>
    Public Property Fy As Double = 420.0

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = ""
        If Descripcion Is Nothing Then Descripcion = ""
        If Poligono Is Nothing Then Poligono = New List(Of Punto2D)
        If Pilotes Is Nothing Then Pilotes = New List(Of PilotePosicion)
        If Espesor <= 0 Then Espesor = 0.8
        If FactorSeparacionMinima <= 0 Then FactorSeparacionMinima = 3.0
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Nombre} ({Pilotes.Count} pilotes)"
    End Function

End Class

''' <summary>
''' Punto 2D serializable. No se usa System.Windows.Point o PointF porque
''' queremos que la clase quede libre de dependencias WPF/WinForms para que
''' los Services y las Classes puedan referenciarla sin cargar UI.
''' </summary>
<Serializable>
Public Class Punto2D

    Public Property X As Double
    Public Property Y As Double

    Public Sub New()
    End Sub

    Public Sub New(x As Double, y As Double)
        Me.X = x
        Me.Y = y
    End Sub

    Public Overrides Function ToString() As String
        Return $"({X:0.000}, {Y:0.000})"
    End Function

End Class
