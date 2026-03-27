<Serializable>
Public Class SeccionMuro__

    ' Propiedades y otras clases como antes...

    ' Lista para almacenar los refuerzos
    Public ListaRefuerzos As New List(Of Refuerzos)

    ' Clase para almacenar los puntos del diagrama de interacción
    Public Class PuntoInteraccion
        Public Property Axial As Double
        Public Property Moment As Double

        Public Sub New(axial As Double, moment As Double)
            Me.Axial = axial
            Me.Moment = moment
        End Sub
    End Class

    ' Función para generar el diagrama de interacción
    Public Function GenerarDiagramaInteraccion(Lw_Model As Single, tw_Model As Single, fc As Single) As List(Of PuntoInteraccion)
        Dim diagrama As New List(Of PuntoInteraccion)

        ' Supón que tienes un rango de ángulos de inclinación del eje neutro (theta) que varían de -90 a 90 grados
        For theta As Double = -90 To 90 Step 5
            Dim capacidadAxial As Double = CalcularCapacidadAxial(theta, Lw_Model, tw_Model, fc)
            Dim capacidadFlexural As Double = CalcularCapacidadFlexural(theta, Lw_Model, tw_Model, fc)
            diagrama.Add(New PuntoInteraccion(capacidadAxial, capacidadFlexural))
        Next

        Return diagrama
    End Function

    ' Función auxiliar para calcular la capacidad axial
    Private Function CalcularCapacidadAxial(theta As Double, Lw_Model As Single, tw_Model As Single, fc As Single) As Double
        ' Aquí implementarías el cálculo de la capacidad axial basado en theta
        ' Esto podría incluir la suma de las contribuciones de concreto y acero
        Dim fy As Single = 420
        Dim Pn As Double = 0.0

        ' Ejemplo simplificado de cálculo
        For Each refuerzo In ListaRefuerzos
            Pn += refuerzo.Area * fy * Math.Cos(theta * Math.PI / 180)
        Next

        ' Agregar contribución del concreto (simplificado)
        Dim Ac As Double = Lw_Model * tw_Model ' Área del concreto
        Pn += Ac * fc * 0.85 * Math.Cos(theta * Math.PI / 180)

        Return Pn
    End Function

    ' Función auxiliar para calcular la capacidad flexural
    Private Function CalcularCapacidadFlexural(theta As Double, Lw_Model As Single, tw_Model As Single, fc As Single) As Double
        ' Aquí implementarías el cálculo de la capacidad flexural basado en theta
        ' Esto podría incluir la integración de tensiones a lo largo de la sección
        Dim fy As Single = 420
        Dim Mn As Double = 0.0

        ' Ejemplo simplificado de cálculo
        For Each refuerzo In ListaRefuerzos
            Dim d As Double = refuerzo.Y ' Supone que Y es la distancia desde el eje neutro
            Mn += refuerzo.Area * fy * d * Math.Sin(theta * Math.PI / 180)
        Next

        ' Agregar contribución del concreto (simplificado)
        Dim Ac As Double = Lw_Model * tw_Model ' Área del concreto
        Dim d_conc As Double = Lw_Model / 2 ' Supone que H es la altura total de la sección
        Mn += Ac * fc * 0.85 * d_conc * Math.Sin(theta * Math.PI / 180)

        Return Mn
    End Function

    ' Función para dibujar el diagrama de interacción
    Public Sub DibujarDiagramaInteraccion(g As Graphics, diagrama As List(Of PuntoInteraccion))
        ' Configuración básica del gráfico
        Dim pen As New Pen(Color.Blue, 2)

        ' Dibujar ejes
        g.DrawLine(Pens.Black, 0, g.ClipBounds.Height / 2, g.ClipBounds.Width, g.ClipBounds.Height / 2) ' Eje X
        g.DrawLine(Pens.Black, g.ClipBounds.Width / 2, 0, g.ClipBounds.Width / 2, g.ClipBounds.Height) ' Eje Y

        ' Dibujar el diagrama de interacción
        Dim scaleFactor As Double = 10.0 ' Escalar los valores para que se ajusten al gráfico
        For i As Integer = 1 To diagrama.Count - 1
            Dim p1 As New PointF(CSng(diagrama(i - 1).Axial * scaleFactor) + g.ClipBounds.Width / 2,
                                 g.ClipBounds.Height / 2 - CSng(diagrama(i - 1).Moment * scaleFactor))
            Dim p2 As New PointF(CSng(diagrama(i).Axial * scaleFactor) + g.ClipBounds.Width / 2,
                                 g.ClipBounds.Height / 2 - CSng(diagrama(i).Moment * scaleFactor))
            g.DrawLine(pen, p1, p2)
        Next
    End Sub
End Class

<Serializable>
Public Class Refuerzos
    Public Property X As Double
    Public Property Y As Double
    Public Property Material As String
    Public Property Area As Double

    ' Constructor para refuerzo simple
    Public Sub New(x As Double, y As Double, material As String, area As Double)
        Me.X = x
        Me.Y = y
        Me.Material = material
        Me.Area = area
    End Sub

    ' Constructor para refuerzo lineal
    Public Sub New(xi As Double, yi As Double, xf As Double, yf As Double, separacion As Double, cantidad As Integer, material As String, area As Double)
        ' Implementar lógica para definir múltiples barras entre los puntos (xi, yi) y (xf, yf)
        ' Por simplicidad, asumimos que solo se usa la primera barra
        Me.X = xi
        Me.Y = yi
        Me.Material = material
        Me.Area = area
    End Sub

    ' Constructor para patrón de refuerzo rectangular
    Public Sub New(x As Double, y As Double, areaBorde As Double, cantidadBorde As Integer, areaLadoB As Double, cantidadLadoB As Integer, areaLadoH As Double, cantidadLadoH As Integer, material As String)
        ' Implementar lógica para definir el patrón rectangular
        ' Por simplicidad, asumimos que solo se usa la primera barra
        Me.X = x
        Me.Y = y
        Me.Material = material
        Me.Area = areaBorde
    End Sub
End Class

