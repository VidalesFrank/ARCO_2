''' <summary>
''' Wrapper observable de un Dado para la tabla de asignación. Tiene un
''' puntero al PiloteOverviewVM padre para resolver nombres de TipoDado al
''' bindear el ComboBox de la columna "Tipo de dado".
''' </summary>
Public Class DadoVM
    Inherits ObservableVM

    Public ReadOnly Modelo As Dado
    Private ReadOnly _parent As PiloteOverviewVM

    Public Sub New(modelo As Dado, parent As PiloteOverviewVM)
        Me.Modelo = modelo
        Me._parent = parent
    End Sub

    Public ReadOnly Property JointLabel As String
        Get
            Return Modelo.JointLabel
        End Get
    End Property

    Public ReadOnly Property SourceType As String
        Get
            Return If(String.IsNullOrEmpty(Modelo.SourceType), "—", Modelo.SourceType)
        End Get
    End Property

    ''' <summary>Verdadero si el apoyo viene de un Frame (columna).</summary>
    Public ReadOnly Property EsFrame As Boolean
        Get
            Return Modelo.SourceType IsNot Nothing AndAlso
                   Modelo.SourceType.Equals("Frame", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    ''' <summary>Verdadero si el apoyo viene de un Pier (muro).</summary>
    Public ReadOnly Property EsPier As Boolean
        Get
            Return Modelo.SourceType IsNot Nothing AndAlso
                   Modelo.SourceType.Equals("Pier", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Public ReadOnly Property XGlobal As Double
        Get
            Return Modelo.X_Global
        End Get
    End Property

    Public ReadOnly Property YGlobal As Double
        Get
            Return Modelo.Y_Global
        End Get
    End Property

    Public ReadOnly Property ZBase As Double
        Get
            Return Modelo.Z_Base
        End Get
    End Property

    Public ReadOnly Property NumCombinaciones As Integer
        Get
            Return If(Modelo.Combinaciones Is Nothing, 0, Modelo.Combinaciones.Count)
        End Get
    End Property

    ''' <summary>
    ''' Combinaciones crudas tal como vinieron de ETABS (Joint Reactions o
    ''' Pier Forces invertido). Es la entrada que luego alimenta el análisis.
    ''' Se expone para que la UI pueda mostrar la trazabilidad al usuario.
    ''' </summary>
    Public ReadOnly Property Combinaciones As IEnumerable(Of CombinacionConContexto)
        Get
            If Modelo.Combinaciones Is Nothing Then
                Return Enumerable.Empty(Of CombinacionConContexto)()
            End If
            Dim seleccion As HashSet(Of String) = Nothing
            If _parent IsNot Nothing AndAlso
               _parent.Modelo IsNot Nothing AndAlso
               _parent.Modelo.ListA_Combinaciones_Design IsNot Nothing AndAlso
               _parent.Modelo.ListA_Combinaciones_Design.Count > 0 Then
                seleccion = New HashSet(Of String)(_parent.Modelo.ListA_Combinaciones_Design,
                                                   StringComparer.OrdinalIgnoreCase)
            End If
            Return Modelo.Combinaciones.Select(Function(c) New CombinacionConContexto With {
                .LoadCase = c.LoadCase,
                .FZ = c.FZ,
                .VX = c.VX,
                .VY = c.VY,
                .MX = c.MX,
                .MY = c.MY,
                .EstaEnSeleccionDiseno = (seleccion Is Nothing) OrElse seleccion.Contains(c.LoadCase)
            }).ToList()
        End Get
    End Property

    ''' <summary>Nombre del TipoDado asignado — bindeado en el ComboBox.</summary>
    Public Property NombreTipoDado As String
        Get
            Return Modelo.NombreTipoDado
        End Get
        Set(value As String)
            If Modelo.NombreTipoDado <> value Then
                Modelo.NombreTipoDado = If(value, "")
                OnPropertyChanged()
                ' Recalcular este Dado solo (opcional).
                RecalcularEste()
            End If
        End Set
    End Property

    Public Property AsignacionManual As Boolean
        Get
            Return Modelo.AsignacionManual
        End Get
        Set(value As Boolean)
            If Modelo.AsignacionManual <> value Then
                Modelo.AsignacionManual = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    ''' <summary>C/D compresión del último cálculo (0 si no se ha calculado).</summary>
    Public ReadOnly Property CD_Compresion As Double
        Get
            Return Modelo.CD_Compresion
        End Get
    End Property

    Public ReadOnly Property CD_Traccion As Double
        Get
            Return Modelo.CD_Traccion
        End Get
    End Property

    ''' <summary>C/D estructural compresión del último cálculo (0 = N/A).</summary>
    Public ReadOnly Property CD_Compresion_Estructural As Double
        Get
            Return Modelo.CD_Compresion_Estructural
        End Get
    End Property

    ''' <summary>C/D estructural tracción del último cálculo (0 = N/A).</summary>
    Public ReadOnly Property CD_Traccion_Estructural As Double
        Get
            Return Modelo.CD_Traccion_Estructural
        End Get
    End Property

    ''' <summary>
    ''' Peor C/D estructural entre compresión y tracción, ignorando los
    ''' sentinelas 0 "no aplica". Se expone para mostrar como columna única
    ''' compacta en la tabla Top-10 sin romper el layout.
    ''' </summary>
    Public ReadOnly Property CD_Estructural_Minimo As Double
        Get
            Dim c = Modelo.CD_Compresion_Estructural
            Dim t = Modelo.CD_Traccion_Estructural
            If c > 0 AndAlso t > 0 Then Return Math.Min(c, t)
            If c > 0 Then Return c
            If t > 0 Then Return t
            Return 0
        End Get
    End Property

    Public ReadOnly Property Pu_Max As Double
        Get
            Return Modelo.Pu_Pilote_Max
        End Get
    End Property

    Public ReadOnly Property Pu_Min As Double
        Get
            Return Modelo.Pu_Pilote_Min
        End Get
    End Property

    Public ReadOnly Property ComboGobCompresion As String
        Get
            Return If(Modelo.Combo_Gob_Compresion, "")
        End Get
    End Property

    Private Sub RecalcularEste()
        If _parent Is Nothing Then Return
        Dim td = _parent.Modelo.BuscarTipoDado(Modelo.NombreTipoDado)
        If td Is Nothing OrElse td.Pilotes.Count = 0 Then
            Modelo.CD_Compresion = 0 : Modelo.CD_Traccion = 0
            NotificarResultados()
            Return
        End If
        Dim tp = _parent.Modelo.BuscarTipoPilote(td.Pilotes.First().NombreTipoPilote)
        If tp Is Nothing Then Return

        Dim r = PiloteService.EvaluarDado(Modelo, td, tp, _parent.Modelo)
        Modelo.Pu_Pilote_Max = r.Pu_Max_Compresion
        Modelo.Pu_Pilote_Min = r.Pu_Min_Traccion
        Modelo.CD_Compresion = r.CD_Compresion
        Modelo.CD_Traccion = r.CD_Traccion
        Modelo.Combo_Gob_Compresion = r.Combo_Gob_Compresion
        Modelo.Combo_Gob_Traccion = r.Combo_Gob_Traccion

        Dim rEstr = PiloteService.EvaluarEstructuralDado(r, td, _parent.Modelo)
        Modelo.CD_Compresion_Estructural = rEstr.CD_Compresion_Minima
        Modelo.CD_Traccion_Estructural = rEstr.CD_Traccion_Minima

        NotificarResultados()
    End Sub

    Private Sub NotificarResultados()
        OnPropertyChanged(NameOf(CD_Compresion))
        OnPropertyChanged(NameOf(CD_Traccion))
        OnPropertyChanged(NameOf(CD_Compresion_Estructural))
        OnPropertyChanged(NameOf(CD_Traccion_Estructural))
        OnPropertyChanged(NameOf(Pu_Max))
        OnPropertyChanged(NameOf(Pu_Min))
        OnPropertyChanged(NameOf(ComboGobCompresion))
    End Sub

End Class

''' <summary>
''' Fila para la tabla "Combinaciones del apoyo" en la vista Apoyos. Es un
''' wrapper plano de CombinacionDado + bandera de "esta combinación está en
''' la lista de diseño". Pensado solo para mostrar.
''' </summary>
Public Class CombinacionConContexto
    Public Property LoadCase As String = ""
    Public Property FZ As Double
    Public Property VX As Double
    Public Property VY As Double
    Public Property MX As Double
    Public Property MY As Double
    Public Property EstaEnSeleccionDiseno As Boolean
End Class
