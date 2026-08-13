<Serializable>
Public Class Proyecto

    ' Versión del formato de archivo. 0 = archivos anteriores a ARCO 2.1.
    <System.Runtime.Serialization.OptionalField> Public VersionFormato As Integer
    Public Const VersionActual As Integer = 1

    ' Ruta del archivo principal
    Public Property Ruta As String

    ' Información general
    Public Property Info As New cInfoProyecto()

    ' Parámetros sísmicos del diseño
    Public Property ParametrosSismicos As New cParametrosSismicos()

    ' Análisis global estructural
    Public Property ResultadosGlobales As New cResultadosGlobales()

    ' Elementos estructurales
    Public Property Elementos As New cElementos()

    ' Tablas importadas de ETABS
    Public Property TablasEtabs As New cTablasETABS()

    ' Pisos del modelo
    Public Property Pisos As New List(Of cPiso)

    Public Property NumeroPisos As Integer

    Public Property Grids As New cGrids()

    ' ===================================================================
    ' Campos legados (formato anterior al refactor cElementos/cInfoProyecto).
    ' BinaryFormatter los popula al abrir archivos viejos; OnDeserialized
    ' los migra a la nueva estructura y los limpia.
    ' ===================================================================
    <System.Runtime.Serialization.OptionalField> Public Nombre As String
    <System.Runtime.Serialization.OptionalField> Public Direccion As String
    <System.Runtime.Serialization.OptionalField> Public Ciudad As String
    <System.Runtime.Serialization.OptionalField> Public Departamento As String
    <System.Runtime.Serialization.OptionalField> Public Propietario As String
    <System.Runtime.Serialization.OptionalField> Public Disenador As String
    <System.Runtime.Serialization.OptionalField> Public SistemaEstructural As eNumeradores.eSistemaEstructural
    <System.Runtime.Serialization.OptionalField> Public Area_Planta As Single
    <System.Runtime.Serialization.OptionalField> Public Imagen As System.Drawing.Bitmap
    <System.Runtime.Serialization.OptionalField> Public Ruta_Imagen As String
    <System.Runtime.Serialization.OptionalField> Public Persona_Responsable As eNumeradores.eResponsables
    <System.Runtime.Serialization.OptionalField> Public NDE As eNumeradores.eDisipasion
    <System.Runtime.Serialization.OptionalField> Public ZAS As eNumeradores.eAMZ
    <System.Runtime.Serialization.OptionalField> Public Disipacion As String
    <System.Runtime.Serialization.OptionalField> Public Year As Integer
    <System.Runtime.Serialization.OptionalField> Public SistemaE As String
    <System.Runtime.Serialization.OptionalField> Public GrupoU As String
    <System.Runtime.Serialization.OptionalField> Public grupoUso As eNumeradores.eGrupoUso
    <System.Runtime.Serialization.OptionalField> Public T_Suelo As String
    <System.Runtime.Serialization.OptionalField> Public tipoSuelo As eNumeradores.eGrupoSuelo
    <System.Runtime.Serialization.OptionalField> Public Combinacion_SismoX As String
    <System.Runtime.Serialization.OptionalField> Public Combinacion_SismoY As String
    <System.Runtime.Serialization.OptionalField> Public Lista_Pisos As List(Of cPiso)
    <System.Runtime.Serialization.OptionalField> Public NumPisos As Integer
    <System.Runtime.Serialization.OptionalField> Public Planos_Arq As Boolean
    <System.Runtime.Serialization.OptionalField> Public Planos_Est As Boolean
    <System.Runtime.Serialization.OptionalField> Public Memorias_Calculo As Boolean
    <System.Runtime.Serialization.OptionalField> Public Estudio_Suelos As Boolean
    <System.Runtime.Serialization.OptionalField> Public Deriva_X As List(Of cDeriva)
    <System.Runtime.Serialization.OptionalField> Public Deriva_Y As List(Of cDeriva)
    <System.Runtime.Serialization.OptionalField> Public DerivaCr_X As List(Of cDeriva)
    <System.Runtime.Serialization.OptionalField> Public DerivaCr_Y As List(Of cDeriva)
    <System.Runtime.Serialization.OptionalField> Public Pilas As cPilas
    <System.Runtime.Serialization.OptionalField> Public Columnas As cColumnas
    <System.Runtime.Serialization.OptionalField> Public Muros As cMuros

    <System.Runtime.Serialization.OnDeserialized>
    Private Sub OnDeserialized(ctx As System.Runtime.Serialization.StreamingContext)
        ' Asegurar que todos los objetos nuevos estén inicializados
        If Info Is Nothing Then Info = New cInfoProyecto()
        If ParametrosSismicos Is Nothing Then ParametrosSismicos = New cParametrosSismicos()
        If ResultadosGlobales Is Nothing Then ResultadosGlobales = New cResultadosGlobales()
        If Elementos Is Nothing Then Elementos = New cElementos()
        If TablasEtabs Is Nothing Then TablasEtabs = New cTablasETABS()
        If Pisos Is Nothing Then Pisos = New List(Of cPiso)()
        If Grids Is Nothing Then Grids = New cGrids()

        ' Migrar archivo de formato anterior (pre-refactor cElementos)
        Dim esFormatoAntiguo As Boolean = (Columnas IsNot Nothing OrElse Muros IsNot Nothing)
        If Not esFormatoAntiguo Then Return

        If Columnas IsNot Nothing Then
            Elementos.Columnas = Columnas
            ' Secciones_Principal cambió de campo público a auto-propiedad entre versiones.
            ' Al migrar, se restaura como True para que las columnas sean visibles en la UI.
            For Each col In Columnas.Lista_Columnas
                If col IsNot Nothing AndAlso col.Lista_Tramos_Columnas IsNot Nothing AndAlso col.Lista_Tramos_Columnas.Count > 0 Then
                    col.Secciones_Principal = True
                End If
            Next
            Columnas = Nothing
        End If
        If Muros IsNot Nothing Then
            Elementos.Muros = Muros
            Muros = Nothing
        End If
        If Pilas IsNot Nothing Then
            Elementos.Pilas = Pilas
            Pilas = Nothing
        End If
        If Lista_Pisos IsNot Nothing Then
            Pisos = Lista_Pisos
            Lista_Pisos = Nothing
        End If

        If Nombre IsNot Nothing Then
            Info.Nombre = Nombre
            Info.Direccion = If(Direccion, "")
            Info.Ciudad = If(Ciudad, "")
            Info.Departamento = If(Departamento, "")
            Info.Propietario = If(Propietario, "")
            Info.Designer = If(Disenador, "")
            Info.Year = Year
            Info.SistemaEstructural = SistemaEstructural
            Info.Area = Area_Planta
            Info.GrupoUso = grupoUso
            Info.TipoSuelo = tipoSuelo
            Info.Persona_Responsable = Persona_Responsable
            If Imagen IsNot Nothing Then Info.Imagen = Imagen
            Info.Ruta_Imagen = If(Ruta_Imagen, "")
        End If

        ' Limpiar campos legados para que no persistan en futuros guardados
        Nombre = Nothing : Direccion = Nothing : Ciudad = Nothing
        Departamento = Nothing : Propietario = Nothing : Disenador = Nothing
        Imagen = Nothing : Ruta_Imagen = Nothing : Disipacion = Nothing
        SistemaE = Nothing : GrupoU = Nothing : T_Suelo = Nothing
        Combinacion_SismoX = Nothing : Combinacion_SismoY = Nothing
        Deriva_X = Nothing : Deriva_Y = Nothing
        DerivaCr_X = Nothing : DerivaCr_Y = Nothing
    End Sub

End Class

<Serializable>
Public Class cPiso

    Public Nombre As String
    Public CoorZ As Single

End Class

<Serializable>
Public Class cDeriva

    Public Piso As cPiso
    Public Deriva As Single

End Class
