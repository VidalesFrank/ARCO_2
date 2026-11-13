Imports System.Collections.ObjectModel

''' <summary>
''' Wrapper observable sobre TipoDado. Mantiene colecciones observables de
''' vértices y pilotes para que el editor WPF se refresque automáticamente.
''' </summary>
Public Class TipoDadoVM
    Inherits ObservableVM

    Public ReadOnly Modelo As TipoDado
    Public ReadOnly Vertices As New ObservableCollection(Of VerticeVM)
    Public ReadOnly Pilotes As New ObservableCollection(Of PilotePosicionVM)

    Public Sub New(modelo As TipoDado)
        Me.Modelo = modelo
        For Each v In modelo.Poligono
            Vertices.Add(New VerticeVM(v))
        Next
        For Each p In modelo.Pilotes
            Pilotes.Add(New PilotePosicionVM(p))
        Next
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
        Pilotes.Add(New PilotePosicionVM(p))
        NotificarCambioPilotes()
    End Sub

    Public Sub BorrarPilote(vm As PilotePosicionVM)
        If vm Is Nothing Then Return
        Modelo.Pilotes.Remove(vm.Modelo)
        Pilotes.Remove(vm)
        NotificarCambioPilotes()
    End Sub

    ''' <summary>Reemplaza polígono + pilotes (p.ej. al aplicar un generador).</summary>
    Public Sub ReemplazarGeometria(nuevoPoligono As List(Of Punto2D),
                                   nuevosPilotes As List(Of PilotePosicion))
        Modelo.Poligono = nuevoPoligono
        Modelo.Pilotes = nuevosPilotes
        Vertices.Clear()
        For Each v In Modelo.Poligono
            Vertices.Add(New VerticeVM(v))
        Next
        Pilotes.Clear()
        For Each p In Modelo.Pilotes
            Pilotes.Add(New PilotePosicionVM(p))
        Next
        NotificarCambioPilotes()
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
