Imports ARCO.eNumeradores

''' <summary>
''' Punzonamiento de zapatas según la posición del apoyo, y clasificación
''' automática de esa posición a partir de las coordenadas de los nodos.
'''
''' Por qué existe: hasta 2026-09-15 el cálculo usaba siempre el perímetro
''' crítico cerrado de una zapata central (b0 = 2(b+h) + 4d) y alfa_s = 40, sin
''' importar dónde estuviera la zapata. NSR-10 C.11.11 pide perímetro abierto en
''' los apoyos de borde, donde el cono de falla no puede desarrollarse hacia
''' afuera del edificio. La diferencia no es menor: para una zapata de 2x2 con
''' pedestal 0.40x0.40 y d = 0.45, la capacidad real de una esquinera es el 37 %
''' de la que se estaba usando, y la de una medianera el 62 %. El cálculo
''' anterior quedaba del lado NO conservador en todo el perímetro del edificio.
''' </summary>
Public NotInheritable Class ZapataService

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Factor alfa_s de NSR-10 C.11.11.2.1, según la posición del apoyo.
    ''' </summary>
    Public Shared Function AlfaS(tipo As eTipoApoyoZapata) As Double
        Select Case tipo
            Case eTipoApoyoZapata.Esquinera : Return 20.0
            Case eTipoApoyoZapata.Medianera : Return 30.0
            Case Else : Return 40.0
        End Select
    End Function

    ''' <summary>
    ''' Perímetro crítico b0, medido a d/2 de la cara del pedestal.
    '''
    '''   Central     cerrado, los cuatro lados:      2(b + d/2) + 2(h + d/2) = 2(b+h) + 4d...
    '''               en la forma que ya usaba el programa: 2(h + b) + 4d
    '''   Medianera   abierto, tres lados: un lado completo más dos medios lados
    '''   Esquinera   abierto, dos lados: dos medios lados
    '''
    ''' El lado que se pierde es el que da contra el borde del edificio: ahí no
    ''' hay concreto más allá de la cara de la zapata para desarrollar el cono.
    ''' </summary>
    ''' <param name="b">Ancho del pedestal (m).</param>
    ''' <param name="h">Largo del pedestal (m).</param>
    ''' <param name="d">Peralte efectivo de la zapata (m).</param>
    Public Shared Function PerimetroCritico(b As Double, h As Double, d As Double,
                                            tipo As eTipoApoyoZapata) As Double

        If b <= 0 OrElse h <= 0 OrElse d <= 0 Then Return 0

        Select Case tipo

            Case eTipoApoyoZapata.Esquinera
                ' Dos lados, cada uno a medio d de la cara
                Return (b + d / 2.0) + (h + d / 2.0)

            Case eTipoApoyoZapata.Medianera
                ' Un lado completo (el interior) más dos medios lados
                Return (h + d) + 2.0 * (b + d / 2.0)

            Case Else
                ' Cerrado. Se conserva la expresión original del programa para
                ' que una zapata central dé exactamente el mismo número que antes.
                Return 2.0 * (h + b) + 4.0 * d

        End Select

    End Function

    ''' <summary>
    ''' Capacidad a punzonamiento: el menor de los tres límites de NSR-10
    ''' C.11.11.2.1, ya afectados por phi = 0.75. En kN.
    ''' </summary>
    Public Shared Function CapacidadPunzonamiento(fc As Double, b As Double, h As Double,
                                                  d As Double, tipo As eTipoApoyoZapata) _
                                                  As (Vc As Double, Vc1 As Double, Vc2 As Double, Vc3 As Double, b0 As Double)

        Dim b0 As Double = PerimetroCritico(b, h, d, tipo)
        If b0 <= 0 OrElse fc <= 0 Then Return (0, 0, 0, 0, b0)

        Dim beta As Double = Math.Max(h, b) / Math.Min(h, b)
        Dim alfa As Double = AlfaS(tipo)
        Dim raizFc As Double = Math.Sqrt(fc)

        Dim vc1 As Double = 0.75 * 0.17 * raizFc * (1 + 2 / beta) * b0 * d * 1000
        Dim vc2 As Double = 0.75 * 0.083 * (alfa * d / b0 + 2) * raizFc * b0 * d * 1000
        Dim vc3 As Double = 0.75 * 0.33 * raizFc * b0 * d * 1000

        Return ({vc1, vc2, vc3}.Min(), vc1, vc2, vc3, b0)

    End Function

    ' =====================================================================
    ' Clasificación automática por geometría
    ' =====================================================================

    ''' <summary>
    ''' Propone el tipo de apoyo de cada zapata mirando si tiene vecinos a cada
    ''' lado dentro del conjunto de nodos de cimentación.
    '''
    ''' Criterio: se cuenta en cuántas de las cuatro direcciones (±X, ±Y) existe
    ''' al menos otro nodo. Cuatro direcciones ocupadas es una zapata interior;
    ''' tres, una medianera; dos o menos, una esquinera. Es el mismo razonamiento
    ''' que se hace a ojo sobre la planta.
    '''
    ''' NO sobrescribe las zapatas marcadas a mano: un voladizo, una junta de
    ''' dilatación o una zapata combinada rompen esta inferencia, y la corrección
    ''' del ingeniero tiene que sobrevivir a un recálculo.
    ''' </summary>
    ''' <param name="tolerancia">
    ''' Holgura en metros para considerar que dos nodos están alineados en un eje.
    ''' </param>
    ''' <returns>Cuántas zapatas cambiaron de tipo.</returns>
    Public Shared Function ClasificarApoyos(zapatas As List(Of cZapata),
                                            Optional tolerancia As Double = 0.3) As Integer

        If zapatas Is Nothing Then Return 0

        Dim conCoord = zapatas.Where(Function(z) z.TieneCoordenadas).ToList()
        If conCoord.Count < 2 Then Return 0

        Dim cambios As Integer = 0

        For Each z In conCoord

            If z.TipoApoyoManual Then Continue For

            Dim hayIzq As Boolean = False, hayDer As Boolean = False
            Dim hayAbajo As Boolean = False, hayArriba As Boolean = False

            For Each otra In conCoord
                If otra Is z Then Continue For

                Dim dx As Double = otra.CoordX - z.CoordX
                Dim dy As Double = otra.CoordY - z.CoordY

                ' Vecino en X: alineado en Y dentro de la tolerancia
                If Math.Abs(dy) <= tolerancia Then
                    If dx < -tolerancia Then hayIzq = True
                    If dx > tolerancia Then hayDer = True
                End If

                ' Vecino en Y: alineado en X dentro de la tolerancia
                If Math.Abs(dx) <= tolerancia Then
                    If dy < -tolerancia Then hayAbajo = True
                    If dy > tolerancia Then hayArriba = True
                End If
            Next

            Dim lados As Integer = 0
            If hayIzq Then lados += 1
            If hayDer Then lados += 1
            If hayAbajo Then lados += 1
            If hayArriba Then lados += 1

            Dim propuesto As eTipoApoyoZapata
            Select Case lados
                Case 4 : propuesto = eTipoApoyoZapata.Central
                Case 3 : propuesto = eTipoApoyoZapata.Medianera
                Case Else : propuesto = eTipoApoyoZapata.Esquinera
            End Select

            If z.TipoApoyo <> propuesto Then
                z.TipoApoyo = propuesto
                cambios += 1
            End If

        Next

        Return cambios

    End Function

    ''' <summary>Etiqueta legible, para tablas y reportes.</summary>
    Public Shared Function NombreTipo(tipo As eTipoApoyoZapata) As String
        Select Case tipo
            Case eTipoApoyoZapata.Esquinera : Return "Esquinera"
            Case eTipoApoyoZapata.Medianera : Return "Medianera"
            Case Else : Return "Central"
        End Select
    End Function

End Class
