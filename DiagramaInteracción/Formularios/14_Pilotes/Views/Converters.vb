Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data
Imports System.Windows.Media

''' <summary>
''' Convierte un C/D (Double) al brush correspondiente del semáforo:
'''   = 0           → BrushNoAplica (gris)  — sentinela "no calculado"
'''   &lt; 0.90      → BrushNoCumple (rojo)
'''   &lt; 1.00      → BrushAdvertencia (amarillo)
'''   &gt;= 1.00    → BrushCumple (verde)
''' </summary>
Public Class CDColorConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.Convert
        Dim cd As Double = 0
        If value IsNot Nothing AndAlso Not Double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, cd) Then cd = 0
        If TypeOf value Is Double Then cd = DirectCast(value, Double)

        Dim key As String
        If cd <= 0 Then
            key = "BrushNoAplica"
        ElseIf cd < 0.90 Then
            key = "BrushNoCumple"
        ElseIf cd < 1.00 Then
            key = "BrushAdvertencia"
        Else
            key = "BrushCumple"
        End If

        Dim app = Application.Current
        If app IsNot Nothing AndAlso app.Resources.Contains(key) Then
            Return app.Resources(key)
        End If
        Return New SolidColorBrush(Colors.Gray)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>
''' Convierte C/D a texto. 0 → "—" (sentinela "no aplica"), lo demás con dos
''' decimales. Mismo criterio que PilaService: el veredicto de la tabla tiene
''' que leerse igual que el veredicto del cálculo.
''' </summary>
Public Class CDTextConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.Convert
        If value Is Nothing Then Return "—"
        Dim cd As Double = 0
        If TypeOf value Is Double Then cd = DirectCast(value, Double)
        If cd <= 0 Then Return "—"
        Return cd.ToString("0.00", CultureInfo.InvariantCulture)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class

''' <summary>
''' Valor truthy → Visibility.Visible. Soporta Boolean, Integer, Double y String
''' porque en XAML terminamos bindeando contadores (NumApoyos) y flags por igual.
''' Pasar ConverterParameter="Invert" para invertir el sentido (útil para los
''' empty states).
''' </summary>
Public Class BoolToVisibilityConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.Convert
        Dim b As Boolean = EsTruthy(value)
        If parameter IsNot Nothing AndAlso parameter.ToString() = "Invert" Then b = Not b
        Return If(b, Visibility.Visible, Visibility.Collapsed)
    End Function

    Private Shared Function EsTruthy(value As Object) As Boolean
        If value Is Nothing Then Return False
        If TypeOf value Is Boolean Then Return DirectCast(value, Boolean)
        If TypeOf value Is Integer Then Return DirectCast(value, Integer) <> 0
        If TypeOf value Is Long Then Return DirectCast(value, Long) <> 0
        If TypeOf value Is Double Then Return DirectCast(value, Double) <> 0
        If TypeOf value Is Single Then Return DirectCast(value, Single) <> 0
        If TypeOf value Is String Then Return Not String.IsNullOrEmpty(DirectCast(value, String))
        ' Fallback por si viene un numérico empaquetado en Object
        Dim d As Double
        If Double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, d) Then
            Return d <> 0
        End If
        Return True
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.ConvertBack
        Return Visibility.Visible.Equals(value)
    End Function
End Class

''' <summary>Boolean invertido para IsEnabled.</summary>
Public Class InverseBoolConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.Convert
        If TypeOf value Is Boolean Then Return Not DirectCast(value, Boolean)
        Return True
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.ConvertBack
        If TypeOf value Is Boolean Then Return Not DirectCast(value, Boolean)
        Return False
    End Function
End Class

''' <summary>Convierte List(Of String) en un string multilínea.</summary>
Public Class ErroresTextConverter
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.Convert
        Dim lista = TryCast(value, IEnumerable(Of String))
        If lista Is Nothing Then Return ""
        Return String.Join(Environment.NewLine, lista.Select(Function(x) "• " & x))
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object _
        Implements IValueConverter.ConvertBack
        Throw New NotSupportedException()
    End Function
End Class
