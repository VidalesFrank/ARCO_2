Imports System.Windows.Input

''' <summary>
''' Implementación compacta de ICommand para bindear comandos desde XAML a
''' métodos de un ViewModel. Soporta parámetros opcionales via lambda.
''' </summary>
Public Class RelayCommand
    Implements ICommand

    Private ReadOnly _execute As Action(Of Object)
    Private ReadOnly _canExecute As Func(Of Object, Boolean)

    Public Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged

    Public Sub New(execute As Action)
        Me.New(Sub(x) execute())
    End Sub

    Public Sub New(execute As Action, canExecute As Func(Of Boolean))
        Me.New(Sub(x) execute(), Function(x) canExecute())
    End Sub

    Public Sub New(execute As Action(Of Object),
                   Optional canExecute As Func(Of Object, Boolean) = Nothing)
        _execute = execute
        _canExecute = canExecute
    End Sub

    Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
        Return _canExecute Is Nothing OrElse _canExecute(parameter)
    End Function

    Public Sub Execute(parameter As Object) Implements ICommand.Execute
        _execute?.Invoke(parameter)
    End Sub

    Public Sub RaiseCanExecuteChanged()
        RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
    End Sub

End Class
