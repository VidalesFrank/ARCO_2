Imports System.Runtime.Serialization

''' <summary>
''' Catálogo de tipos de pilote disponibles en el proyecto. Un TipoPilote es
''' una especificación reutilizable (diámetro, longitud, capacidad admisible)
''' que luego las PilotePosicion dentro de cada TipoDado referencian por nombre.
'''
''' Los micropilotes caen aquí como un eTipoPilote más: la diferencia práctica
''' es que su Qt_Admisible suele ser > 0 (resisten tracción significativa)
''' mientras que un pilote convencional en compresión-solamente tiene Qt = 0.
''' </summary>
<Serializable>
Public Class TipoPilote

    ''' <summary>Nombre único del tipo. Clave para referencia por nombre.</summary>
    Public Property Nombre As String = ""

    Public Property Tipo As eTipoPilote = eTipoPilote.Perforado

    ''' <summary>Diámetro del pilote [m].</summary>
    Public Property Diametro As Double = 0.6

    ''' <summary>Longitud total empotrada en terreno [m].</summary>
    Public Property Longitud As Double = 10.0

    ''' <summary>Capacidad admisible a compresión por pilote [kN]. Servicio.</summary>
    Public Property Qc_Admisible As Double = 1000.0

    ''' <summary>
    ''' Capacidad admisible a tracción por pilote [kN]. Para pilote convencional
    ''' típicamente 0; para micropilotes y pilotes con conexión a tracción, > 0.
    ''' </summary>
    Public Property Qt_Admisible As Double = 0.0

    ''' <summary>Capacidad admisible lateral por pilote [kN]. Reservado v1.1.</summary>
    Public Property Qlat_Admisible As Double = 0.0

    ' ── Reservado para chequeo estructural futuro (v1.2) ──────────
    ' Van como fields (no Property) para que <OptionalField> aplique y así
    ' agregar campos adicionales a este tipo sobreviva deserializaciones viejas.
    <OptionalField> Public Fc As Double = 21.0
    <OptionalField> Public Fy As Double = 420.0
    <OptionalField> Public NumBarras_Long As Integer = 6
    <OptionalField> Public Diametro_Barra_Long As String = "#5"
    <OptionalField> Public Recubrimiento As Double = 0.075

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = ""
        If Diametro <= 0 Then Diametro = 0.6
        If Longitud <= 0 Then Longitud = 10.0
        If Qc_Admisible <= 0 Then Qc_Admisible = 1000.0
        If Diametro_Barra_Long Is Nothing Then Diametro_Barra_Long = "#5"
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Nombre} · Ø{Diametro * 1000:0}mm · Qc={Qc_Admisible:0} kN"
    End Function

End Class

Public Enum eTipoPilote
    Perforado = 0
    Hincado = 1
    Micropilote = 2
End Enum
