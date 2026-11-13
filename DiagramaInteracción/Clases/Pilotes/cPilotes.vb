Imports System.Runtime.Serialization

''' <summary>
''' Contenedor del módulo 14 — Pilotes. Mismo patrón de cZapatas / cColumnas:
''' catálogo de tipos (plantillas) + lista de instancias por apoyo +
''' combinaciones de diseño seleccionadas.
''' </summary>
<Serializable>
Public Class cPilotes

    ''' <summary>Catálogo de tipos de pilote disponibles en el proyecto.</summary>
    Public Property TiposPilote As New List(Of TipoPilote)

    ''' <summary>Catálogo de tipos de dado (plantillas de cabezal).</summary>
    Public Property TiposDado As New List(Of TipoDado)

    ''' <summary>Dados asociados a los apoyos (uno por joint).</summary>
    Public Property Dados As New List(Of Dado)

    ''' <summary>Lista completa de combinaciones disponibles.</summary>
    Public Property Lista_Combinaciones As New List(Of String)

    ''' <summary>Combinaciones seleccionadas para el diseño.</summary>
    Public Property ListA_Combinaciones_Design As New List(Of String)

    Public Property Lista_Combinaciones_Estaticas As New List(Of String)
    Public Property Lista_Combinaciones_Dinamicas As New List(Of String)

    ''' <summary>
    ''' Si True, suma al Pu el peso propio del dado y del suelo encima al
    ''' chequear compresión. Mismo patrón de cZapatas.UsarPesoEstabilizante.
    ''' </summary>
    Public Property UsarPesoEstabilizante As Boolean = False

    ''' <summary>Densidad del suelo encima del dado [kN/m³] si se activa el peso.</summary>
    Public Property DensidadSuelo As Double = 18.0

    ''' <summary>Profundidad promedio de desplante [m] (para peso de suelo).</summary>
    Public Property ProfundidadDesplante As Double = 2.0

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If TiposPilote Is Nothing Then TiposPilote = New List(Of TipoPilote)
        If TiposDado Is Nothing Then TiposDado = New List(Of TipoDado)
        If Dados Is Nothing Then Dados = New List(Of Dado)
        If Lista_Combinaciones Is Nothing Then Lista_Combinaciones = New List(Of String)
        If ListA_Combinaciones_Design Is Nothing Then ListA_Combinaciones_Design = New List(Of String)
        If Lista_Combinaciones_Estaticas Is Nothing Then Lista_Combinaciones_Estaticas = New List(Of String)
        If Lista_Combinaciones_Dinamicas Is Nothing Then Lista_Combinaciones_Dinamicas = New List(Of String)
        If DensidadSuelo <= 0 Then DensidadSuelo = 18.0
        If ProfundidadDesplante < 0 Then ProfundidadDesplante = 2.0
    End Sub

    ''' <summary>Busca un TipoPilote por nombre. Devuelve Nothing si no existe.</summary>
    Public Function BuscarTipoPilote(nombre As String) As TipoPilote
        If String.IsNullOrEmpty(nombre) Then Return Nothing
        Return TiposPilote.FirstOrDefault(Function(t) t.Nombre = nombre)
    End Function

    ''' <summary>Busca un TipoDado por nombre. Devuelve Nothing si no existe.</summary>
    Public Function BuscarTipoDado(nombre As String) As TipoDado
        If String.IsNullOrEmpty(nombre) Then Return Nothing
        Return TiposDado.FirstOrDefault(Function(t) t.Nombre = nombre)
    End Function

End Class
