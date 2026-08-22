Imports System.Linq

''' <summary>
''' Representa un candidato a pila detectado automáticamente desde ETABS
''' (puede ser tipo Frame o tipo Pier). El usuario valida cuáles corresponden
''' a pilas reales en Form_01_ImportarETABS antes de confirmar la importación.
''' </summary>
Public Class cCandidatoPila

    ''' <summary>Nombre que identifica al apoyo (editable por el usuario).</summary>
    Public Property Nombre As String = ""

    ''' <summary>"Frame" o "Pier".</summary>
    Public Property Tipo As String = ""

    ''' <summary>Etiqueta de origen en ETABS (Joint label o Pier label).</summary>
    Public Property SourceLabel As String = ""

    ''' <summary>Piso/story al que pertenece el elemento.</summary>
    Public Property Story As String = ""

    ''' <summary>Coordenada global X en metros (sistema ETABS).</summary>
    Public Property X As Double

    ''' <summary>Coordenada global Y en metros (sistema ETABS).</summary>
    Public Property Y As Double

    ''' <summary>Coordenada global Z en metros (sistema ETABS).</summary>
    Public Property Z As Double

    ''' <summary>Lista de reacciones/fuerzas importadas desde ETABS.</summary>
    Public Property Reactions As New List(Of cCombinacionPila)

    ''' <summary>True si el usuario ha marcado este candidato para importar.</summary>
    Public Property Seleccionado As Boolean = True

    ''' <summary>Estado de validación ("OK", "Sin datos", "Advertencia", etc.).</summary>
    Public Property Estado As String = "OK"

    ''' <summary>Número de combinaciones de carga disponibles.</summary>
    Public ReadOnly Property NumeroCombinaciones As Integer
        Get
            Return If(Reactions Is Nothing, 0,
                      Reactions.Select(Function(r) r.LoadCase) _
                               .Where(Function(lc) Not String.IsNullOrWhiteSpace(lc)) _
                               .Distinct() _
                               .Count())
        End Get
    End Property

End Class
