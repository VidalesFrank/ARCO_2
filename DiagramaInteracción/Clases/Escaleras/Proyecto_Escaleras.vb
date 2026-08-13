Imports System.Runtime.Serialization

<Serializable>
Public Class Proyecto_Escaleras

    Public Nombre As String
    Public Ruta As String

    Public fc As Single
    Public fy As Single
    Public C_Imp As Single
    Public C_Viv As Single
    Public P_ConR As Single
    Public P_Con As Single

    Public Huella As Single
    Public Contrahuella As Single
    Public N_Peldanos As Integer
    Public L_Peldanos As Single
    Public L_Descanso As Single
    Public L_Total As Single
    Public A_Escalera As Single
    Public A_Estudio As Single
    Public Recubrimiento As Single
    Public h As Single

    Public Wu_Inclinada As Single
    Public Wu_Descanso As Single

    Public Abscisas As New List(Of Single)
    Public Momentos As New List(Of Single)
    Public Cortantes As New List(Of Single)

    '-- Diseno temperatura --
    Public Acero_Temperatura As Single
    Public Cuantia_Temperatura As Single
    Public Cuantia_Temperaruta_Colocada As Single
    Public Barra_Temperatura As Integer
    Public S_Temperatura As Single
    Public S_Temperatura_Colocada As Single
    Public Verificacion_Temperatura As Single

    '-- Diseno flexion (capa inferior) --
    Public Mu As Single
    Public Acero_Flexion As Single
    Public Cuantia_Flexion As Single
    Public Acero_Flexion_Colocado As Single
    Public Barra_Flexion As Integer
    Public S_Flexion As Single
    Public S_Flexion_Colocada As Single
    Public Verificacion_Flexion As Single

    '-- Diseno cortante --
    Public Vu As Single
    Public Vc As Single
    Public Verificacion_Cortante As Boolean

    '-- Refuerzo superior (doble capa) --
    <OptionalField>
    Public RequiereDobleRefuerzo As Boolean
    <OptionalField>
    Public Acero_Superior_Requerido As Single
    <OptionalField>
    Public Cuantia_Superior As Single
    <OptionalField>
    Public Acero_Superior_Colocado As Single
    <OptionalField>
    Public Cuantia_Superior_Colocada As Single
    <OptionalField>
    Public Barra_Superior As Integer
    <OptionalField>
    Public S_Superior As Single
    <OptionalField>
    Public S_Superior_Colocada As Single
    <OptionalField>
    Public Verificacion_Superior As Boolean

    '-- Condicion de susceptibilidad para limite de deflexion --
    <OptionalField>
    Public ElementosSusceptibles As Boolean   ' True → L/480 | False → L/240

    '-- Deflexiones --
    <OptionalField>
    Public Ig As Single
    <OptionalField>
    Public Icr As Single
    <OptionalField>
    Public Ie As Single
    <OptionalField>
    Public Mcr As Single
    <OptionalField>
    Public fr As Single
    <OptionalField>
    Public Ec As Single
    <OptionalField>
    Public W_Equivalente As Single
    <OptionalField>
    Public Delta_Inmediata As Single
    <OptionalField>
    Public Delta_LP As Single
    <OptionalField>
    Public Delta_Adm_360 As Single
    <OptionalField>
    Public Delta_Adm_480 As Single
    <OptionalField>
    Public Verif_Deflexion_360 As Boolean
    <OptionalField>
    Public Verif_Deflexion_480 As Boolean

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Abscisas Is Nothing Then Abscisas = New List(Of Single)
        If Momentos Is Nothing Then Momentos = New List(Of Single)
        If Cortantes Is Nothing Then Cortantes = New List(Of Single)
        ' Proyectos antiguos sin este campo: asumir condición conservadora (L/480)
        ' ElementosSusceptibles = False (default Boolean) en proyectos viejos → corregir a True
        ' No es posible distinguir "no guardado" de "guardado como False", así que se deja al usuario.
    End Sub

End Class
