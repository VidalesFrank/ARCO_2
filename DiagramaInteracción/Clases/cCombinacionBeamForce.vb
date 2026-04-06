
<Serializable>
Public Class cCombinacionBeamForce

    ' Propiedades que reflejan la estructura de la tabla de Beam Forces
    Public Property Story As String
    Public Property Beam As String
    Public Property UniqueName As String
    Public Property LoadCaseCombo As String
    Public Property stepType As String
    Public Property Station As Double
    Public Property P As Double
    Public Property V2 As Double
    Public Property V3 As Double
    Public Property T As Double
    Public Property M2 As Double
    Public Property M3 As Double
    Public Property Element As String
    Public Property ElementStation As Double

    Public Overrides Function ToString() As String
        Return $"{Story} - {Beam} - {UniqueName} ({LoadCaseCombo}, {stepType}, Station={Station}, P={P}, V2={V2}, V3={V3}, T={T}, M2={M2}, M3={M3}, Element={Element}, ElementStation={ElementStation})"
    End Function



End Class
