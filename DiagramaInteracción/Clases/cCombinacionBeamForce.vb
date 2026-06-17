
<Serializable>
Public Class cCombinacionBeamForce

    ' Propiedades que reflejan la estructura de la tabla de Beam Forces
    Public Property Story As String
    Public Property Beam As String
    Public Property UniqueName As String
    Public Property LoadCaseCombo As String
    Public Property stepType As String

    ''' <summary>Clave canónica: "Output Case (Step Type)". Normaliza E17 ("Envolvente Min" → "Envolvente (Min)").</summary>
    Public ReadOnly Property LoadCaseKey As String
        Get
            If Not String.IsNullOrWhiteSpace(stepType) Then
                Return LoadCaseCombo.Trim() & " (" & stepType.Trim() & ")"
            End If
            Return NormalizarClaveE17(LoadCaseCombo)
        End Get
    End Property

    Private Shared Function NormalizarClaveE17(nombre As String) As String
        If String.IsNullOrWhiteSpace(nombre) Then Return nombre
        Dim t = nombre.Trim()
        If t.EndsWith(" Max", StringComparison.OrdinalIgnoreCase) Then
            Return t.Substring(0, t.Length - 4).TrimEnd() & " (Max)"
        ElseIf t.EndsWith(" Min", StringComparison.OrdinalIgnoreCase) Then
            Return t.Substring(0, t.Length - 4).TrimEnd() & " (Min)"
        End If
        Return t
    End Function

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
