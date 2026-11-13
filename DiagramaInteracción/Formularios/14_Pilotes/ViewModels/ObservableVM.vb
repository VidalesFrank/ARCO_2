Imports System.ComponentModel
Imports System.Runtime.CompilerServices

''' <summary>
''' Base para todos los ViewModels del módulo de Pilotes. Implementa
''' INotifyPropertyChanged con un setter conveniente (SetProperty). Es el
''' equivalente ligero de ObservableObject de CommunityToolkit.Mvvm pero
''' escrito en VB puro — así evitamos un paquete NuGet para dos métodos.
''' </summary>
Public MustInherit Class ObservableVM
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler _
        Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    ''' <summary>
    ''' Asigna un campo y dispara PropertyChanged solo si cambió. Devuelve True
    ''' si hubo cambio real — útil para encadenar lógica adicional.
    ''' </summary>
    Protected Function SetProperty(Of T)(ByRef field As T, value As T,
                                         <CallerMemberName> Optional propertyName As String = Nothing) As Boolean
        If Object.Equals(field, value) Then Return False
        field = value
        OnPropertyChanged(propertyName)
        Return True
    End Function

End Class
