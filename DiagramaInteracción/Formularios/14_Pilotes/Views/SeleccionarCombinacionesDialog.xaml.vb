Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Windows
Imports System.Windows.Controls

''' <summary>
''' Dialog modal que permite al usuario elegir cuáles combinaciones participan
''' del análisis. Trabaja sobre una lista de ComboSelectable (string + bool) y
''' expone los nombres finales en la propiedad SeleccionResultado tras OK.
''' </summary>
Partial Public Class SeleccionarCombinacionesDialog
    Inherits Window

    Private ReadOnly _items As New ObservableCollection(Of ComboSelectable)

    ''' <summary>Nombres seleccionados cuando DialogResult = True.</summary>
    Public ReadOnly Property SeleccionResultado As New List(Of String)

    Public Sub New(disponibles As IEnumerable(Of String),
                   yaSeleccionadas As IEnumerable(Of String))
        InitializeComponent()

        Dim selSet As New HashSet(Of String)(
            If(yaSeleccionadas, Enumerable.Empty(Of String)()),
            StringComparer.OrdinalIgnoreCase)

        For Each c In If(disponibles, Enumerable.Empty(Of String)())
            Dim vm As New ComboSelectable With {
                .Nombre = c,
                .Seleccionado = selSet.Contains(c)
            }
            AddHandler vm.PropertyChanged, AddressOf OnItemChanged
            _items.Add(vm)
        Next

        LstCombos.ItemsSource = _items
        ActualizarContador()
    End Sub

    Private Sub OnItemChanged(sender As Object, e As PropertyChangedEventArgs)
        If e.PropertyName = NameOf(ComboSelectable.Seleccionado) Then
            ActualizarContador()
        End If
    End Sub

    Private Sub ActualizarContador()
        Dim sel = _items.Where(Function(x) x.Seleccionado).Count()
        LblContador.Text = $"{sel} de {_items.Count} seleccionadas"
    End Sub

    Private Sub TxtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim q = If(TxtBuscar.Text, "").Trim()
        If String.IsNullOrEmpty(q) Then
            LstCombos.ItemsSource = _items
        Else
            LstCombos.ItemsSource = _items.Where(
                Function(x) x.Nombre.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList()
        End If
    End Sub

    Private Sub BtnMarcarTodas_Click(sender As Object, e As RoutedEventArgs)
        For Each it In _items
            it.Seleccionado = True
        Next
        ActualizarContador()
    End Sub

    Private Sub BtnDesmarcarTodas_Click(sender As Object, e As RoutedEventArgs)
        For Each it In _items
            it.Seleccionado = False
        Next
        ActualizarContador()
    End Sub

    Private Sub BtnAplicar_Click(sender As Object, e As RoutedEventArgs)
        SeleccionResultado.Clear()
        For Each it In _items
            If it.Seleccionado Then SeleccionResultado.Add(it.Nombre)
        Next
        DialogResult = True
        Close()
    End Sub

    Private Sub BtnCancelar_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

End Class

''' <summary>Fila del selector: un nombre de combo + su estado de selección.</summary>
Public Class ComboSelectable
    Inherits ObservableVM

    Private _seleccionado As Boolean

    Public Property Nombre As String = ""

    Public Property Seleccionado As Boolean
        Get
            Return _seleccionado
        End Get
        Set(value As Boolean)
            SetProperty(_seleccionado, value)
        End Set
    End Property

End Class
