Imports System.Collections.ObjectModel

''' <summary>
''' Wrapper observable sobre TipoDado. Mantiene colecciones observables de
''' vértices y pilotes para que el editor WPF se refresque automáticamente.
''' </summary>
Public Class TipoDadoVM
    Inherits ObservableVM

    Public ReadOnly Modelo As TipoDado

    ''' <summary>
    ''' IMPORTANTE: son Property (no Field). El Binding WPF reflexiona sobre
    ''' properties; si fueran `Public ReadOnly ... As ObservableCollection`
    ''' el DataGrid quedaría silenciosamente vacío sin error en Debug Output.
    ''' </summary>
    Public ReadOnly Property Vertices As New ObservableCollection(Of VerticeVM)
    Public ReadOnly Property Pilotes As New ObservableCollection(Of PilotePosicionVM)

    ''' <summary>
    ''' Se dispara cada vez que la geometría del dado cambia por cualquier
    ''' vía: edición numérica en tablas, drag en canvas, generador, agregar
    ''' o borrar un vértice o un pilote. El code-behind del Window se
    ''' suscribe para redibujar el canvas sin tener que hookear cada VerticeVM.
    ''' </summary>
    Public Event GeometriaCambiada As EventHandler

    Public Sub New(modelo As TipoDado)
        Me.Modelo = modelo
        For Each v In modelo.Poligono
            EnlazarVertice(New VerticeVM(v))
        Next
        For Each p In modelo.Pilotes
            EnlazarPilote(New PilotePosicionVM(p))
        Next
    End Sub

    Private Sub EnlazarVertice(vm As VerticeVM)
        AddHandler vm.PropertyChanged, AddressOf OnHijoCambiado
        Vertices.Add(vm)
    End Sub

    Private Sub EnlazarPilote(vm As PilotePosicionVM)
        AddHandler vm.PropertyChanged, AddressOf OnHijoCambiado
        Pilotes.Add(vm)
    End Sub

    Private Sub OnHijoCambiado(sender As Object, e As ComponentModel.PropertyChangedEventArgs)
        RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
    End Sub

    Public Property Nombre As String
        Get
            Return Modelo.Nombre
        End Get
        Set(value As String)
            If Modelo.Nombre <> value Then
                Modelo.Nombre = If(value, "")
                OnPropertyChanged()
                OnPropertyChanged(NameOf(Resumen))
            End If
        End Set
    End Property

    Public Property Descripcion As String
        Get
            Return Modelo.Descripcion
        End Get
        Set(value As String)
            If Modelo.Descripcion <> value Then
                Modelo.Descripcion = If(value, "")
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Espesor As Double
        Get
            Return Modelo.Espesor
        End Get
        Set(value As Double)
            If Modelo.Espesor <> value Then
                Modelo.Espesor = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Recubrimiento As Double
        Get
            Return Modelo.Recubrimiento
        End Get
        Set(value As Double)
            If Modelo.Recubrimiento <> value Then
                Modelo.Recubrimiento = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Fc As Double
        Get
            Return Modelo.Fc
        End Get
        Set(value As Double)
            If Modelo.Fc <> value Then
                Modelo.Fc = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Fy As Double
        Get
            Return Modelo.Fy
        End Get
        Set(value As Double)
            If Modelo.Fy <> value Then
                Modelo.Fy = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property LadoColumna_X As Double
        Get
            Return Modelo.LadoColumna_X
        End Get
        Set(value As Double)
            If Modelo.LadoColumna_X <> value Then
                Modelo.LadoColumna_X = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property LadoColumna_Y As Double
        Get
            Return Modelo.LadoColumna_Y
        End Get
        Set(value As Double)
            If Modelo.LadoColumna_Y <> value Then
                Modelo.LadoColumna_Y = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Beta_Puntal As Double
        Get
            Return Modelo.Beta_Puntal
        End Get
        Set(value As Double)
            If Modelo.Beta_Puntal <> value Then
                Modelo.Beta_Puntal = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    ' ── Refuerzo inferior del dado (tensores del STM) ─────────────
    Public Property NumBarras_Inf_X As Integer
        Get
            Return Modelo.NumBarras_Inf_X
        End Get
        Set(value As Integer)
            If Modelo.NumBarras_Inf_X <> value Then
                Modelo.NumBarras_Inf_X = Math.Max(0, value)
                OnPropertyChanged()
                RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property Diametro_Barras_Inf_X As String
        Get
            Return Modelo.Diametro_Barras_Inf_X
        End Get
        Set(value As String)
            If Modelo.Diametro_Barras_Inf_X <> value Then
                Modelo.Diametro_Barras_Inf_X = If(value, "")
                OnPropertyChanged()
                RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property NumBarras_Inf_Y As Integer
        Get
            Return Modelo.NumBarras_Inf_Y
        End Get
        Set(value As Integer)
            If Modelo.NumBarras_Inf_Y <> value Then
                Modelo.NumBarras_Inf_Y = Math.Max(0, value)
                OnPropertyChanged()
                RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public Property Diametro_Barras_Inf_Y As String
        Get
            Return Modelo.Diametro_Barras_Inf_Y
        End Get
        Set(value As String)
            If Modelo.Diametro_Barras_Inf_Y <> value Then
                Modelo.Diametro_Barras_Inf_Y = If(value, "")
                OnPropertyChanged()
                RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Public ReadOnly Property NumPilotes As Integer
        Get
            Return Pilotes.Count
        End Get
    End Property

    Public ReadOnly Property Resumen As String
        Get
            Return $"{Nombre} — {Pilotes.Count} pilotes"
        End Get
    End Property

    ''' <summary>Fuerza refresh del resumen cuando se agrega/quita un pilote.</summary>
    Public Sub NotificarCambioPilotes()
        OnPropertyChanged(NameOf(NumPilotes))
        OnPropertyChanged(NameOf(Resumen))
    End Sub

    ''' <summary>
    ''' Agrega un pilote y lo refleja en el modelo subyacente.
    ''' </summary>
    Public Sub AgregarPilote(x As Double, y As Double, nombreTipo As String)
        Dim p As New PilotePosicion(x, y, nombreTipo)
        Modelo.Pilotes.Add(p)
        EnlazarPilote(New PilotePosicionVM(p))
        NotificarCambioPilotes()
        RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
    End Sub

    Public Sub BorrarPilote(vm As PilotePosicionVM)
        If vm Is Nothing Then Return
        RemoveHandler vm.PropertyChanged, AddressOf OnHijoCambiado
        Modelo.Pilotes.Remove(vm.Modelo)
        Pilotes.Remove(vm)
        NotificarCambioPilotes()
        RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Inserta un nuevo vértice en el polígono después del índice dado y lo
    ''' refleja en el modelo subyacente. Usado para insertar vértice al hacer
    ''' doble clic sobre una arista del polígono en el editor.
    ''' </summary>
    Public Sub InsertarVertice(indiceDespues As Integer, x As Double, y As Double)
        If indiceDespues < -1 Then indiceDespues = -1
        If indiceDespues > Modelo.Poligono.Count - 1 Then indiceDespues = Modelo.Poligono.Count - 1
        Dim p As New Punto2D(x, y)
        Modelo.Poligono.Insert(indiceDespues + 1, p)
        Dim vm As New VerticeVM(p)
        AddHandler vm.PropertyChanged, AddressOf OnHijoCambiado
        Vertices.Insert(indiceDespues + 1, vm)
        RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Agrega un vértice al final del polígono en una posición razonable
    ''' (duplica el último vértice desplazado 0.5 m en X). El usuario luego
    ''' edita X/Y a su gusto en la tabla.
    ''' </summary>
    Public Sub AgregarVerticeAlFinal()
        Dim xNew As Double = 0, yNew As Double = 0
        If Modelo.Poligono.Count > 0 Then
            Dim ult = Modelo.Poligono(Modelo.Poligono.Count - 1)
            xNew = ult.X + 0.5
            yNew = ult.Y
        End If
        InsertarVertice(Modelo.Poligono.Count - 1, xNew, yNew)
    End Sub

    ''' <summary>
    ''' Borra un vértice del polígono. Protege el mínimo geométrico (3 vértices)
    ''' para que el dado no deje de ser un polígono.
    ''' </summary>
    Public Function BorrarVertice(vm As VerticeVM) As Boolean
        If vm Is Nothing Then Return False
        If Modelo.Poligono.Count <= 3 Then Return False
        RemoveHandler vm.PropertyChanged, AddressOf OnHijoCambiado
        Modelo.Poligono.Remove(vm.Modelo)
        Vertices.Remove(vm)
        RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
        Return True
    End Function

    ''' <summary>Reemplaza polígono + pilotes (p.ej. al aplicar un generador).</summary>
    Public Sub ReemplazarGeometria(nuevoPoligono As List(Of Punto2D),
                                   nuevosPilotes As List(Of PilotePosicion))
        For Each v In Vertices : RemoveHandler v.PropertyChanged, AddressOf OnHijoCambiado : Next
        For Each p In Pilotes : RemoveHandler p.PropertyChanged, AddressOf OnHijoCambiado : Next
        Modelo.Poligono = nuevoPoligono
        Modelo.Pilotes = nuevosPilotes
        Vertices.Clear()
        For Each v In Modelo.Poligono
            EnlazarVertice(New VerticeVM(v))
        Next
        Pilotes.Clear()
        For Each p In Modelo.Pilotes
            EnlazarPilote(New PilotePosicionVM(p))
        Next
        NotificarCambioPilotes()
        RaiseEvent GeometriaCambiada(Me, EventArgs.Empty)
    End Sub

End Class

''' <summary>
''' VM de un vértice del polígono. Permite edición vía binding al Canvas.
''' </summary>
Public Class VerticeVM
    Inherits ObservableVM

    Public ReadOnly Modelo As Punto2D

    Public Sub New(modelo As Punto2D)
        Me.Modelo = modelo
    End Sub

    Public Property X As Double
        Get
            Return Modelo.X
        End Get
        Set(value As Double)
            If Modelo.X <> value Then
                Modelo.X = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Y As Double
        Get
            Return Modelo.Y
        End Get
        Set(value As Double)
            If Modelo.Y <> value Then
                Modelo.Y = value
                OnPropertyChanged()
            End If
        End Set
    End Property

End Class

''' <summary>
''' VM de una posición de pilote dentro de un TipoDado.
''' </summary>
Public Class PilotePosicionVM
    Inherits ObservableVM

    Public ReadOnly Modelo As PilotePosicion

    Public Sub New(modelo As PilotePosicion)
        Me.Modelo = modelo
    End Sub

    Public Property X As Double
        Get
            Return Modelo.X_Local
        End Get
        Set(value As Double)
            If Modelo.X_Local <> value Then
                Modelo.X_Local = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property Y As Double
        Get
            Return Modelo.Y_Local
        End Get
        Set(value As Double)
            If Modelo.Y_Local <> value Then
                Modelo.Y_Local = value
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property NombreTipoPilote As String
        Get
            Return Modelo.NombreTipoPilote
        End Get
        Set(value As String)
            If Modelo.NombreTipoPilote <> value Then
                Modelo.NombreTipoPilote = If(value, "")
                OnPropertyChanged()
            End If
        End Set
    End Property

End Class
