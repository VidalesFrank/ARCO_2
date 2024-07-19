<Serializable>
Public Class Refuerzo
    Public Property Tipo As TipoRefuerzo
    Public Property CoordenadaX As Double
    Public Property CoordenadaY As Double
    Public Property Material As String
    Public Property AreaRefuerzo As Double

    ' Propiedades específicas para Refuerzo Lineal
    Public Property CoordenadaXFinal As Double?
    Public Property CoordenadaYFinal As Double?
    Public Property Separacion As Double?
    Public Property CantidadVarillas As Integer?

    ' Propiedades específicas para Patrón de Refuerzo Rectangular
    Public Property CoordenadaXCentro As Double?
    Public Property CoordenadaYCentro As Double?
    Public Property AreaRefuerzoBorde As Double?
    Public Property CantidadBarrasBorde As Integer?
    Public Property AreaRefuerzoLadoB As Double?
    Public Property CantidadVarillasLadoB As Integer?
    Public Property AreaRefuerzoLadoH As Double?
    Public Property CantidadVarillasLadoH As Integer?

    Public Enum TipoRefuerzo
        Simple
        Lineal
        PatronRectangular
    End Enum

    ' Constructor para Refuerzo Simple
    Public Sub New(coordenadaX As Double, coordenadaY As Double, material As String, areaRefuerzo As Double)
        Me.Tipo = TipoRefuerzo.Simple
        Me.CoordenadaX = coordenadaX
        Me.CoordenadaY = coordenadaY
        Me.Material = material
        Me.AreaRefuerzo = areaRefuerzo
    End Sub

    ' Constructor para Refuerzo Lineal
    Public Sub New(coordenadaX As Double, coordenadaY As Double, coordenadaXFinal As Double, coordenadaYFinal As Double, separacion As Double, cantidadVarillas As Integer, material As String, areaRefuerzo As Double)
        Me.Tipo = TipoRefuerzo.Lineal
        Me.CoordenadaX = coordenadaX
        Me.CoordenadaY = coordenadaY
        Me.CoordenadaXFinal = coordenadaXFinal
        Me.CoordenadaYFinal = coordenadaYFinal
        Me.Separacion = separacion
        Me.CantidadVarillas = cantidadVarillas
        Me.Material = material
        Me.AreaRefuerzo = areaRefuerzo
    End Sub

    ' Constructor para Patrón de Refuerzo Rectangular
    Public Sub New(coordenadaXCentro As Double, coordenadaYCentro As Double, areaRefuerzoBorde As Double, cantidadBarrasBorde As Integer, areaRefuerzoLadoB As Double, cantidadVarillasLadoB As Integer, areaRefuerzoLadoH As Double, cantidadVarillasLadoH As Integer, material As String)
        Me.Tipo = TipoRefuerzo.PatronRectangular
        Me.CoordenadaXCentro = coordenadaXCentro
        Me.CoordenadaYCentro = coordenadaYCentro
        Me.AreaRefuerzoBorde = areaRefuerzoBorde
        Me.CantidadBarrasBorde = cantidadBarrasBorde
        Me.AreaRefuerzoLadoB = areaRefuerzoLadoB
        Me.CantidadVarillasLadoB = cantidadVarillasLadoB
        Me.AreaRefuerzoLadoH = areaRefuerzoLadoH
        Me.CantidadVarillasLadoH = cantidadVarillasLadoH
        Me.Material = material
    End Sub
End Class

