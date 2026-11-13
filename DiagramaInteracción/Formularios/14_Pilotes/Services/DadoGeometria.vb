''' <summary>
''' Utilidades geométricas puras para TipoDado. Sin dependencias WPF/WinForms:
''' trabaja con Punto2D (clase propia serializable). Todo el código está en
''' coordenadas locales del dado — el caller rota a global cuando sea necesario.
'''
''' El tensor de inercia del grupo de pilotes es la pieza crítica: para formas
''' irregulares (pentágonos, hexágonos, L) el producto Σxy ≠ 0 y hay que
''' resolver el sistema acoplado al calcular fuerzas por flexión biaxial.
''' </summary>
Public Module DadoGeometria

    ''' <summary>
    ''' Área de un polígono por la fórmula del zapatero (shoelace). Devuelve
    ''' valor positivo si los vértices están en CCW, negativo en CW.
    ''' Toma el valor absoluto para no sorprender al caller.
    ''' </summary>
    Public Function AreaPoligono(poligono As IList(Of Punto2D)) As Double
        If poligono Is Nothing OrElse poligono.Count < 3 Then Return 0
        Dim a As Double = 0
        Dim n As Integer = poligono.Count
        For i = 0 To n - 1
            Dim j As Integer = (i + 1) Mod n
            a += poligono(i).X * poligono(j).Y
            a -= poligono(j).X * poligono(i).Y
        Next
        Return Math.Abs(a) * 0.5
    End Function

    ''' <summary>
    ''' Centroide geométrico del polígono. Si el polígono es degenerado, devuelve
    ''' el centroide de los vértices como fallback.
    ''' </summary>
    Public Function CentroidePoligono(poligono As IList(Of Punto2D)) As Punto2D
        If poligono Is Nothing OrElse poligono.Count = 0 Then Return New Punto2D(0, 0)
        If poligono.Count < 3 Then
            Dim mx = poligono.Average(Function(p) p.X)
            Dim my = poligono.Average(Function(p) p.Y)
            Return New Punto2D(mx, my)
        End If

        Dim a As Double = 0, cx As Double = 0, cy As Double = 0
        Dim n As Integer = poligono.Count
        For i = 0 To n - 1
            Dim j As Integer = (i + 1) Mod n
            Dim cross = poligono(i).X * poligono(j).Y - poligono(j).X * poligono(i).Y
            a += cross
            cx += (poligono(i).X + poligono(j).X) * cross
            cy += (poligono(i).Y + poligono(j).Y) * cross
        Next
        a *= 0.5
        If Math.Abs(a) < 1.0E-12 Then
            Return New Punto2D(poligono.Average(Function(p) p.X),
                               poligono.Average(Function(p) p.Y))
        End If
        Return New Punto2D(cx / (6 * a), cy / (6 * a))
    End Function

    ''' <summary>
    ''' Test de punto dentro de polígono (ray casting). Útil para validar que
    ''' cada pilote queda dentro del cabezal con la holgura de recubrimiento.
    ''' </summary>
    Public Function PuntoDentroPoligono(p As Punto2D, poligono As IList(Of Punto2D)) As Boolean
        If poligono Is Nothing OrElse poligono.Count < 3 Then Return False
        Dim inside As Boolean = False
        Dim n As Integer = poligono.Count
        Dim j As Integer = n - 1
        For i = 0 To n - 1
            Dim pi = poligono(i), pj = poligono(j)
            If ((pi.Y > p.Y) <> (pj.Y > p.Y)) AndAlso
               (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y + 1.0E-15) + pi.X) Then
                inside = Not inside
            End If
            j = i
        Next
        Return inside
    End Function

    ''' <summary>
    ''' Distancia mínima de un punto al borde del polígono (al segmento más
    ''' cercano). Cuando el punto está adentro devuelve distancia positiva;
    ''' cuando está afuera, también positiva — para signo usar PuntoDentroPoligono.
    ''' </summary>
    Public Function DistanciaAlBordePoligono(p As Punto2D, poligono As IList(Of Punto2D)) As Double
        If poligono Is Nothing OrElse poligono.Count < 2 Then Return 0
        Dim dmin As Double = Double.MaxValue
        Dim n As Integer = poligono.Count
        For i = 0 To n - 1
            Dim j As Integer = (i + 1) Mod n
            Dim d = DistanciaPuntoSegmento(p, poligono(i), poligono(j))
            If d < dmin Then dmin = d
        Next
        Return dmin
    End Function

    Private Function DistanciaPuntoSegmento(p As Punto2D, a As Punto2D, b As Punto2D) As Double
        Dim vx = b.X - a.X, vy = b.Y - a.Y
        Dim wx = p.X - a.X, wy = p.Y - a.Y
        Dim l2 = vx * vx + vy * vy
        If l2 < 1.0E-15 Then Return Math.Sqrt(wx * wx + wy * wy)
        Dim t = (wx * vx + wy * vy) / l2
        If t < 0 Then t = 0
        If t > 1 Then t = 1
        Dim px = a.X + t * vx, py = a.Y + t * vy
        Dim dx = p.X - px, dy = p.Y - py
        Return Math.Sqrt(dx * dx + dy * dy)
    End Function

    ''' <summary>
    ''' Centroide del grupo de pilotes (promedio simple de posiciones).
    ''' Importante: NO coincide con el centroide del polígono del dado en formas
    ''' irregulares o cuando el layout de pilotes está desplazado.
    ''' </summary>
    Public Function CentroideGrupoPilotes(pilotes As IList(Of PilotePosicion)) As Punto2D
        If pilotes Is Nothing OrElse pilotes.Count = 0 Then Return New Punto2D(0, 0)
        Dim mx = pilotes.Average(Function(p) p.X_Local)
        Dim my = pilotes.Average(Function(p) p.Y_Local)
        Return New Punto2D(mx, my)
    End Function

    ''' <summary>
    ''' Tensor de inercia 2x2 del grupo de pilotes respecto al centroide del grupo.
    ''' [Suu   Suv]
    ''' [Suv   Svv]
    ''' donde Suu=Σu², Svv=Σv², Suv=Σuv con u,v medidos desde el centroide.
    ''' Para un grupo simétrico Suv=0 y el sistema se desacopla a la fórmula
    ''' clásica. Para pentágonos o layouts no simétricos Suv≠0 y hay que resolver
    ''' el sistema 2x2 completo.
    ''' </summary>
    Public Function TensorInerciaGrupo(pilotes As IList(Of PilotePosicion)) As TensorInercia2D
        Dim r As New TensorInercia2D()
        If pilotes Is Nothing OrElse pilotes.Count = 0 Then Return r

        Dim centro = CentroideGrupoPilotes(pilotes)
        r.Centroide = centro

        For Each p In pilotes
            Dim u = p.X_Local - centro.X
            Dim v = p.Y_Local - centro.Y
            r.Suu += u * u
            r.Svv += v * v
            r.Suv += u * v
        Next

        r.Determinante = r.Suu * r.Svv - r.Suv * r.Suv
        Return r
    End Function

    ''' <summary>
    ''' Devuelve pares de índices de pilotes cuya separación centro a centro es
    ''' menor que `separacionMinima`. Útil para validación geométrica (3·d típico).
    ''' </summary>
    Public Function PilotesDemasiadoCerca(pilotes As IList(Of PilotePosicion),
                                          separacionMinima As Double) _
                                          As List(Of Tuple(Of Integer, Integer))
        Dim pares As New List(Of Tuple(Of Integer, Integer))
        If pilotes Is Nothing OrElse pilotes.Count < 2 Then Return pares
        Dim sMin2 = separacionMinima * separacionMinima
        For i = 0 To pilotes.Count - 2
            For j = i + 1 To pilotes.Count - 1
                Dim dx = pilotes(i).X_Local - pilotes(j).X_Local
                Dim dy = pilotes(i).Y_Local - pilotes(j).Y_Local
                If (dx * dx + dy * dy) < sMin2 Then
                    pares.Add(Tuple.Create(i, j))
                End If
            Next
        Next
        Return pares
    End Function

End Module

''' <summary>
''' Tensor de inercia 2D del grupo de pilotes respecto a su centroide.
''' No es serializable — se calcula on-demand desde los pilotes.
''' </summary>
Public Class TensorInercia2D
    Public Centroide As Punto2D = New Punto2D(0, 0)
    Public Suu As Double     ' Σu²  (u medido desde el centroide)
    Public Svv As Double     ' Σv²
    Public Suv As Double     ' Σuv
    Public Determinante As Double ' Suu·Svv − Suv²

    ''' <summary>Casi-singular → momento no se puede repartir confiablemente.</summary>
    Public ReadOnly Property Singular As Boolean
        Get
            Dim traza = Suu + Svv
            If traza < 1.0E-12 Then Return True
            Return Math.Abs(Determinante) < 1.0E-6 * traza * traza
        End Get
    End Property
End Class
