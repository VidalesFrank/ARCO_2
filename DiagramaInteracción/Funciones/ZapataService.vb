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
    ' =====================================================================
    ' UBICACIÓN EN PLANTA — de qué nodo saca cada zapata su X,Y
    ' =====================================================================

    ''' <summary>
    ''' Resultado de ubicar las zapatas: cuántas quedaron con coordenadas, qué
    ''' etiquetas no se encontraron y qué zapatas comparten posición.
    ''' </summary>
    Public Class ResultadoUbicacion
        Public Property Ubicadas As Integer
        ''' <summary>Labels de "Joint Reactions" que no existen en la hoja de nodos.</summary>
        Public Property SinNodo As New List(Of String)
        ''' <summary>Grupos de zapatas que cayeron en el mismo punto, ya formateados.</summary>
        Public Property Superpuestas As New List(Of String)
        Public ReadOnly Property HayProblemas As Boolean
            Get
                Return SinNodo.Count > 0 OrElse Superpuestas.Count > 0
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Índice de nodos de cimentación por etiqueta.
    '''
    ''' EL VÍNCULO CORRECTO ES "Object Label", NO "Element Name". En la hoja de
    ''' nodos de E23, "Object Label" es la etiqueta del nodo por piso — la misma
    ''' que trae la columna Label de "Joint Reactions" — mientras que
    ''' "Element Name" es el nombre único del elemento, que es lo que
    ''' referencian los frames. En modelos simples los dos corren parejos y el
    ''' error pasa desapercibido; en cuanto divergen (nodos agregados o
    ''' borrados, mallado) unas pocas zapatas toman la coordenada de OTRO nodo
    ''' y aparecen superpuestas sobre sus vecinas. Así lo resuelve también el
    ''' importador de Pilas (Funciones_00_Varias, diccionarios byLabel/byElem).
    '''
    ''' Una etiqueta se repite en todos los pisos, así que se conserva el nodo
    ''' de MENOR Z: el de la cimentación.
    ''' </summary>
    Public Shared Function IndexarJointsDeCimentacion(joints As List(Of cJoint),
                                                      porElementName As Boolean) As Dictionary(Of String, cJoint)

        Dim idx As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
        If joints Is Nothing Then Return idx

        For Each j As cJoint In joints
            If j Is Nothing Then Continue For
            Dim clave As String = If(porElementName, j.ElementLabel, j.ObjectLabel)
            If String.IsNullOrWhiteSpace(clave) Then Continue For
            clave = clave.Trim()

            Dim ex As cJoint = Nothing
            If Not idx.TryGetValue(clave, ex) OrElse j.GlobalZ < ex.GlobalZ Then
                idx(clave) = j
            End If
        Next

        Return idx
    End Function

    ''' <summary>
    ''' Copia a cada zapata la X,Y de su nodo y reporta lo que no cuadró. Se
    ''' llama con TODAS las zapatas a la vez, no una por una, porque la
    ''' superposición solo se ve mirando el conjunto.
    '''
    ''' Orden de resolución por etiqueta: primero "Object Label" (el vínculo de
    ''' "Joint Reactions"), y si ahí no está, "Element Name" como respaldo —
    ''' cubre los archivos donde coinciden y el formato E17.
    ''' </summary>
    Public Shared Function UbicarZapatas(zapatas As List(Of cZapata),
                                         joints As List(Of cJoint),
                                         Optional tolerancia As Double = 0.001) As ResultadoUbicacion

        Dim res As New ResultadoUbicacion()
        If zapatas Is Nothing OrElse zapatas.Count = 0 Then Return res

        Dim porLabel = IndexarJointsDeCimentacion(joints, porElementName:=False)
        Dim porElem = IndexarJointsDeCimentacion(joints, porElementName:=True)

        For Each z In zapatas
            If z Is Nothing Then Continue For

            Dim etiqueta As String = Convert.ToString(z.Label_joint)
            If etiqueta Is Nothing Then etiqueta = ""
            etiqueta = etiqueta.Trim()

            Dim j As cJoint = Nothing
            If etiqueta.Length > 0 Then
                If Not porLabel.TryGetValue(etiqueta, j) Then
                    porElem.TryGetValue(etiqueta, j)
                End If
            End If

            If j Is Nothing Then
                ' Sin nodo no se puede ubicar ni clasificar. Se limpia la marca
                ' para que no quede una coordenada vieja de un calculo anterior.
                z.TieneCoordenadas = False
                res.SinNodo.Add(If(etiqueta.Length > 0, etiqueta, "(sin etiqueta)"))
                Continue For
            End If

            z.CoordX = j.GlobalX
            z.CoordY = j.GlobalY
            z.TieneCoordenadas = True
            res.Ubicadas += 1
        Next

        res.Superpuestas.AddRange(DetectarSuperpuestas(zapatas, tolerancia))
        Return res
    End Function

    ''' <summary>
    ''' Zapatas que quedaron en el mismo punto. Dos apoyos del modelo no pueden
    ''' compartir coordenada: si pasa, la etiqueta se resolvió contra el nodo
    ''' equivocado y en la planta una tapa a la otra.
    ''' </summary>
    Public Shared Function DetectarSuperpuestas(zapatas As List(Of cZapata),
                                                Optional tolerancia As Double = 0.001) As List(Of String)

        Dim avisos As New List(Of String)
        If zapatas Is Nothing Then Return avisos

        Dim ubicadas = zapatas.Where(Function(z) z IsNot Nothing AndAlso z.TieneCoordenadas).ToList()
        If ubicadas.Count < 2 Then Return avisos

        ' Redondeo a la tolerancia para agrupar: con 0.001 m son milimetros.
        Dim paso As Double = If(tolerancia > 0, tolerancia, 0.001)
        Dim grupos = ubicadas.GroupBy(Function(z) New With {
                                          Key .X = Math.Round(z.CoordX / paso),
                                          Key .Y = Math.Round(z.CoordY / paso)
                                      })

        For Each g In grupos
            If g.Count() < 2 Then Continue For
            Dim nombres = String.Join(", ", g.Select(Function(z) NombreCorto(z)))
            Dim primera = g.First()
            avisos.Add(nombres & "  ->  (" & primera.CoordX.ToString("0.00") & ", " &
                       primera.CoordY.ToString("0.00") & ")")
        Next

        Return avisos
    End Function

    Private Shared Function NombreCorto(z As cZapata) As String
        If Not String.IsNullOrWhiteSpace(z.Nombre) Then Return z.Nombre
        Return Convert.ToString(z.Label_joint)
    End Function

    ''' <summary>
    ''' Coherencia mínima para creerle a la dirección medida de la nube de
    ''' apoyos. 0.5 = la mitad de los segmentos al vecino más cercano apuntan en
    ''' la misma dirección de malla. Por debajo no hay una malla que medir.
    ''' </summary>
    Public Const COHERENCIA_MINIMA As Double = 0.5

    ''' <summary>
    ''' A partir de cuántas veces el vano típico un hueco deja de ser un vano.
    ''' Un apoyo suelto a 50 m de una malla de 5 m abre un hueco de 50 m en la
    ''' lista de ordenadas, pero eso no es una crujía: es otro bloque, o un
    ''' apoyo aislado. Si contara como vano, el alcance se estiraría hasta él y
    ''' volvería a tapar el borde, que es justo lo que se quiso evitar.
    ''' </summary>
    Public Const VANO_ATIPICO As Double = 4.0

    ' ---------------------------------------------------------------------
    ' Marco de la malla: dirección dominante y escala
    ' ---------------------------------------------------------------------

    ''' <summary>
    ''' Marco y escala de la malla de apoyos, medidos de la propia planta.
    ''' </summary>
    Public Class MallaInfo
        ''' <summary>Dirección dominante de la malla respecto al eje X global, en grados, dentro de [0, 90).</summary>
        Public Property AnguloGrados As Double
        ''' <summary>Qué tan definida está la dirección de la malla, de 0 a 1. Ver AnguloDeLaNube.</summary>
        Public Property Coherencia As Double
        ''' <summary>De dónde salió el ángulo: "apoyos", "ejes" (líneas tipo G), "forzado" o "global".</summary>
        Public Property Origen As String = "apoyos"

        ''' <summary>
        ''' Distancia típica entre apoyos vecinos (m): mediana de la distancia al
        ''' más cercano. Mide el desalineamiento admisible, no el vano.
        ''' </summary>
        Public Property SeparacionTipica As Double
        ''' <summary>Vano típico (m): mediana de la separación entre ejes de apoyos consecutivos.</summary>
        Public Property LuzTipica As Double
        ''' <summary>Vano ancho (m): percentil 90 de esas separaciones. Es lo que fija el alcance.</summary>
        Public Property LuzMaxima As Double

        ''' <summary>Semiancho de la franja para considerar dos apoyos alineados (m).</summary>
        Public Property Banda As Double
        ''' <summary>Distancia máxima a la que otro apoyo cuenta como vecino (m).</summary>
        Public Property Alcance As Double
        ''' <summary>Apoyos con coordenadas que entraron en la medición.</summary>
        Public Property Apoyos As Integer

        Public Overrides Function ToString() As String
            Return $"malla a {AnguloGrados:0.0}° (según {Origen}), vano típico " &
                   $"{LuzTipica:0.00} m, banda {Banda:0.00} m, alcance {Alcance:0.00} m"
        End Function
    End Class

    ''' <summary>
    ''' Mide la malla antes de clasificar: su dirección y sus dos escalas.
    '''
    ''' POR QUÉ NO SE SACA DEL GRID DE ETABS: las líneas de eje tipo X e Y solo
    ''' traen una Ordinate, expresada en el sistema de ejes de ETABS, que puede
    ''' estar rotado respecto al global — no hay ángulo que leer, y así las dibuja
    ''' la planta. Solo las de tipo General (Cartesian) traen X1,Y1,X2,Y2 y por
    ''' tanto una dirección real.
    '''
    ''' SON DOS ESCALAS DISTINTAS, a propósito:
    '''   - la BANDA (qué tanto desalineamiento se admite para seguir contando a
    '''     dos apoyos como de la misma fila) sale de la distancia al vecino más
    '''     cercano, que es robusta: una columna corrida dentro de su vano no la
    '''     mueve.
    '''   - el ALCANCE (hasta dónde se busca vecino) sale del VANO, es decir de la
    '''     separación entre ejes consecutivos de apoyos. No puede salir de la
    '''     distancia al vecino más cercano: en una malla de filas juntas y vanos
    '''     largos — 3 m entre filas y 9 m entre ejes — el vecino más cercano
    '''     siempre está a 3 m, el alcance quedaría en 4.5 m y los apoyos
    '''     interiores de esos vanos largos saldrían como de borde.
    ''' </summary>
    ''' <param name="anguloGradosForzado">
    ''' Ángulo impuesto a mano. NaN (el valor normal) significa detectarlo.
    ''' </param>
    ''' <param name="tolerancia">Banda mínima en metros, para mallas muy apretadas.</param>
    ''' <param name="factorBanda">
    ''' Fracción de la distancia al vecino que se admite de desalineamiento.
    ''' 0.5 = media luz: una columna corrida medio vano sigue contando como
    ''' vecina de su fila. Pasar de 0.5 empezaría a contar las diagonales.
    ''' </param>
    ''' <param name="factorAlcance">
    ''' Múltiplo del vano ancho hasta donde se busca vecino. Acota el caso de un
    ''' apoyo suelto a 50 m que, por estar alineado, tapaba el borde.
    ''' </param>
    Public Shared Function AnalizarMalla(zapatas As List(Of cZapata),
                                         Optional gridLines As List(Of cGridLine) = Nothing,
                                         Optional anguloGradosForzado As Double = Double.NaN,
                                         Optional tolerancia As Double = 0.3,
                                         Optional factorBanda As Double = 0.5,
                                         Optional factorAlcance As Double = 1.5) As MallaInfo

        Dim info As New MallaInfo()
        Dim pts = PuntosConCoordenadas(zapatas)
        info.Apoyos = pts.Count

        info.SeparacionTipica = Percentil(DistanciasAlVecinoMasCercano(pts), 0.5)

        ' Ángulo: forzado > nube de apoyos (si es coherente) > ejes tipo G > 0.
        ' La nube manda porque es justo lo que se está clasificando. Las líneas
        ' de eje solo entran cuando la nube no define una dirección — dos bloques
        ' con orientaciones distintas, una planta irregular — porque un eje tipo
        ' General puede ser cualquier cosa: una rampa, una diagonal de fachada.
        Dim coherencia As Double = 0
        Dim angNube As Double = AnguloDeLaNube(pts, coherencia)
        info.Coherencia = coherencia

        If Not Double.IsNaN(anguloGradosForzado) Then
            info.AnguloGrados = NormalizarACuadrante(anguloGradosForzado)
            info.Origen = "forzado"
        ElseIf coherencia >= COHERENCIA_MINIMA Then
            info.AnguloGrados = angNube
            info.Origen = "apoyos"
        Else
            Dim angEjes As Double = AnguloDeEjesGenerales(gridLines)
            If Not Double.IsNaN(angEjes) Then
                info.AnguloGrados = angEjes
                info.Origen = "ejes"
            Else
                ' Ni la nube ni los ejes dicen nada: se queda en los ejes
                ' globales, que es el comportamiento de siempre.
                info.AnguloGrados = 0
                info.Origen = "global"
            End If
        End If

        ' Vanos: ya con el ángulo se pueden proyectar los apoyos sobre las dos
        ' direcciones de la malla y medir la separación entre ejes consecutivos.
        Dim vanos = VanosDeLaMalla(pts, info.AnguloGrados, tolerancia)
        info.LuzTipica = Percentil(vanos, 0.5)
        info.LuzMaxima = VanoAnchoPlausible(vanos, info.LuzTipica)

        info.Banda = Math.Max(tolerancia, factorBanda * info.SeparacionTipica)
        Dim luz As Double = Math.Max(info.SeparacionTipica, info.LuzMaxima)
        info.Alcance = If(luz > 0, factorAlcance * luz, Double.MaxValue)

        Return info
    End Function

    Private Shared Function PuntosConCoordenadas(zapatas As List(Of cZapata)) As List(Of cZapata)
        If zapatas Is Nothing Then Return New List(Of cZapata)()
        Return zapatas.Where(Function(z) z IsNot Nothing AndAlso z.TieneCoordenadas).ToList()
    End Function

    Private Shared Function DistanciasAlVecinoMasCercano(pts As List(Of cZapata)) As List(Of Double)
        Dim dists As New List(Of Double)
        If pts Is Nothing Then Return dists
        For i = 0 To pts.Count - 1
            Dim mejor As Double = Double.MaxValue
            For j = 0 To pts.Count - 1
                If i = j Then Continue For
                Dim dx = pts(j).CoordX - pts(i).CoordX
                Dim dy = pts(j).CoordY - pts(i).CoordY
                Dim d2 = dx * dx + dy * dy
                If d2 > 0.000001 AndAlso d2 < mejor Then mejor = d2
            Next
            If mejor < Double.MaxValue Then dists.Add(Math.Sqrt(mejor))
        Next
        Return dists
    End Function

    ''' <summary>
    ''' Separaciones entre ejes consecutivos de apoyos, en las dos direcciones de
    ''' la malla. Los apoyos se proyectan sobre cada dirección, se agrupan los
    ''' que caen dentro de la tolerancia (ese grupo es un eje) y se devuelven las
    ''' distancias entre ejes vecinos. Es la medida del VANO.
    ''' </summary>
    Private Shared Function VanosDeLaMalla(pts As List(Of cZapata),
                                           anguloGrados As Double,
                                           tolerancia As Double) As List(Of Double)

        Dim vanos As New List(Of Double)
        If pts Is Nothing OrElse pts.Count < 2 Then Return vanos

        Dim rad As Double = anguloGrados * Math.PI / 180.0
        Dim cosT As Double = Math.Cos(rad)
        Dim senT As Double = Math.Sin(rad)

        Dim us As New List(Of Double)
        Dim vs As New List(Of Double)
        For Each p In pts
            us.Add(p.CoordX * cosT + p.CoordY * senT)
            vs.Add(-p.CoordX * senT + p.CoordY * cosT)
        Next

        vanos.AddRange(SeparacionesEntreEjes(us, tolerancia))
        vanos.AddRange(SeparacionesEntreEjes(vs, tolerancia))
        Return vanos
    End Function

    ''' <summary>
    ''' Agrupa ordenadas que caen dentro de la tolerancia (cada grupo es un eje)
    ''' y devuelve la distancia entre ejes consecutivos.
    ''' </summary>
    Private Shared Function SeparacionesEntreEjes(ordenadas As List(Of Double),
                                                  tolerancia As Double) As List(Of Double)

        Dim seps As New List(Of Double)
        If ordenadas Is Nothing OrElse ordenadas.Count < 2 Then Return seps

        Dim tol As Double = If(tolerancia > 0, tolerancia, 0.3)
        Dim orden = ordenadas.OrderBy(Function(v) v).ToList()

        Dim anterior As Double = orden(0)
        For i = 1 To orden.Count - 1
            Dim d As Double = orden(i) - anterior
            If d > tol Then
                seps.Add(d)
                anterior = orden(i)
            End If
        Next

        Return seps
    End Function

    ''' <summary>
    ''' El vano más ancho que sigue siendo creíble como crujía: el mayor que no
    ''' pase de VANO_ATIPICO veces el típico. Un percentil no sirve aquí — con
    ''' tres o cuatro vanos medidos, el p90 ES el hueco del apoyo suelto.
    ''' </summary>
    Private Shared Function VanoAnchoPlausible(vanos As List(Of Double),
                                               vanoTipico As Double) As Double
        If vanos Is Nothing OrElse vanos.Count = 0 Then Return 0
        If vanoTipico <= 0 Then Return vanos.Max()
        Dim techo As Double = VANO_ATIPICO * vanoTipico
        Dim creibles = vanos.Where(Function(v) v <= techo).ToList()
        If creibles.Count = 0 Then Return vanoTipico
        Return creibles.Max()
    End Function

    Private Shared Function Percentil(valores As List(Of Double), q As Double) As Double
        If valores Is Nothing OrElse valores.Count = 0 Then Return 0
        Dim orden = valores.OrderBy(Function(v) v).ToList()
        Dim idx As Integer = CInt(Math.Ceiling(q * orden.Count)) - 1
        If idx < 0 Then idx = 0
        If idx > orden.Count - 1 Then idx = orden.Count - 1
        Return orden(idx)
    End Function

    ''' <summary>
    ''' Ángulo de la malla medido de la nube: el segmento de cada apoyo a su
    ''' vecino más cercano va a lo largo de una de las dos direcciones de la
    ''' malla. El ángulo se toma módulo 90° (una malla ortogonal no distingue sus
    ''' dos direcciones) promediando en el círculo con el truco del cuádruple:
    ''' 4 x 0° y 4 x 90° caen en el mismo punto, así que el promedio es correcto.
    ''' </summary>
    ''' <param name="coherencia">
    ''' Qué tan de acuerdo están esos segmentos, de 0 a 1. Una malla ortogonal
    ''' regular da 1; dos bloques con orientaciones distintas, o una planta sin
    ''' malla, dan cerca de 0 y el ángulo no significa nada.
    ''' </param>
    Private Shared Function AnguloDeLaNube(pts As List(Of cZapata),
                                           ByRef coherencia As Double) As Double

        coherencia = 0
        If pts Is Nothing OrElse pts.Count < 2 Then Return 0

        Dim sx As Double = 0, sy As Double = 0, n As Integer = 0

        For i = 0 To pts.Count - 1
            Dim mejor As Integer = -1, d2Mejor As Double = Double.MaxValue
            For j = 0 To pts.Count - 1
                If i = j Then Continue For
                Dim dx = pts(j).CoordX - pts(i).CoordX
                Dim dy = pts(j).CoordY - pts(i).CoordY
                Dim d2 = dx * dx + dy * dy
                If d2 > 0.000001 AndAlso d2 < d2Mejor Then
                    d2Mejor = d2
                    mejor = j
                End If
            Next
            If mejor < 0 Then Continue For
            Dim ang = Math.Atan2(pts(mejor).CoordY - pts(i).CoordY,
                                 pts(mejor).CoordX - pts(i).CoordX)
            sx += Math.Cos(4.0 * ang)
            sy += Math.Sin(4.0 * ang)
            n += 1
        Next

        If n = 0 Then Return 0

        Dim resultante As Double = Math.Sqrt(sx * sx + sy * sy)
        coherencia = resultante / n
        If resultante < 0.000001 Then Return 0

        Return NormalizarACuadrante(Math.Atan2(sy, sx) / 4.0 * 180.0 / Math.PI)
    End Function

    ''' <summary>
    ''' Ángulo de las líneas de eje tipo General (Cartesian), las únicas que
    ''' traen coordenadas y por tanto dirección. NaN si el proyecto no tiene.
    ''' </summary>
    Private Shared Function AnguloDeEjesGenerales(gridLines As List(Of cGridLine)) As Double

        If gridLines Is Nothing Then Return Double.NaN

        Dim sx As Double = 0, sy As Double = 0, n As Integer = 0

        For Each gl In gridLines
            If gl Is Nothing OrElse Not gl.EsTipoGeneral Then Continue For
            Dim dx = gl.X2 - gl.X1
            Dim dy = gl.Y2 - gl.Y1
            Dim largo = Math.Sqrt(dx * dx + dy * dy)
            If largo < 0.001 Then Continue For
            ' Pesado por la longitud: un eje largo define la malla mejor que el
            ' tramo corto de una rampa o un voladizo.
            Dim ang = Math.Atan2(dy, dx)
            sx += largo * Math.Cos(4.0 * ang)
            sy += largo * Math.Sin(4.0 * ang)
            n += 1
        Next

        If n = 0 OrElse (Math.Abs(sx) < 0.000001 AndAlso Math.Abs(sy) < 0.000001) Then Return Double.NaN

        Return NormalizarACuadrante(Math.Atan2(sy, sx) / 4.0 * 180.0 / Math.PI)
    End Function

    ''' <summary>Lleva un ángulo en grados al rango [0, 90).</summary>
    Private Shared Function NormalizarACuadrante(grados As Double) As Double
        If Double.IsNaN(grados) OrElse Double.IsInfinity(grados) Then Return 0
        Dim g As Double = grados - Math.Floor(grados / 90.0) * 90.0
        If g < 0 OrElse g >= 90.0 Then g = 0
        Return g
    End Function

    ' ---------------------------------------------------------------------
    ' Clasificación
    ' ---------------------------------------------------------------------

    ''' <summary>
    ''' Propone Central / Medianera / Esquinera por la posición del apoyo en la
    ''' planta, contando LADOS LIBRES: direcciones de la malla en las que no hay
    ''' ningún otro apoyo más allá.
    '''
    '''     0 lados libres  -> interior  -> Central
    '''     1 lado libre    -> borde     -> Medianera
    '''     2 o más         -> esquina   -> Esquinera
    '''
    ''' Dos lados libres opuestos (una sola línea de columnas) no es ninguno de
    ''' los tres casos de NSR-10 C.11.11; queda como esquinera, que es el lado
    ''' seguro. Lo mismo un apoyo aislado, con tres o cuatro lados libres.
    '''
    ''' La búsqueda se hace EN EL MARCO DE LA MALLA, no en los ejes X,Y globales:
    ''' hasta 2026-10-05 un edificio girado respecto al origen de ETABS no tenía
    ''' ningún par de apoyos alineado dentro de la tolerancia y salía entero como
    ''' esquineras (phi-Vc al 37 %). Y el vecino tiene que estar DENTRO DEL
    ''' ALCANCE: antes un apoyo suelto a 50 m contaba igual que uno a 5 m y podía
    ''' ascender a Central un apoyo que estaba en el borde — ese error sí es del
    ''' lado no conservador.
    '''
    ''' Lo que el ingeniero marcó a mano (TipoApoyoManual) no se toca: un
    ''' voladizo, una junta de dilatación, un lindero o una zapata combinada
    ''' rompen cualquier inferencia geométrica.
    ''' </summary>
    ''' <returns>Cuántas zapatas cambiaron de tipo.</returns>
    Public Shared Function ClasificarApoyos(zapatas As List(Of cZapata),
                                            Optional tolerancia As Double = 0.3,
                                            Optional gridLines As List(Of cGridLine) = Nothing,
                                            Optional anguloGradosForzado As Double = Double.NaN) As Integer

        Dim info As MallaInfo = Nothing
        Return ClasificarApoyos(zapatas, info, tolerancia, gridLines, anguloGradosForzado)
    End Function

    ''' <summary>
    ''' Igual que la anterior, pero devuelve además cómo quedó medida la malla,
    ''' para registrarla y que el ingeniero la vea.
    ''' </summary>
    Public Shared Function ClasificarApoyos(zapatas As List(Of cZapata),
                                            ByRef malla As MallaInfo,
                                            Optional tolerancia As Double = 0.3,
                                            Optional gridLines As List(Of cGridLine) = Nothing,
                                            Optional anguloGradosForzado As Double = Double.NaN) As Integer

        malla = Nothing
        If zapatas Is Nothing Then Return 0

        Dim conCoord = PuntosConCoordenadas(zapatas)
        If conCoord.Count < 2 Then Return 0

        malla = AnalizarMalla(zapatas, gridLines, anguloGradosForzado, tolerancia)

        Dim rad As Double = malla.AnguloGrados * Math.PI / 180.0
        Dim cosT As Double = Math.Cos(rad)
        Dim senT As Double = Math.Sin(rad)
        Dim banda As Double = malla.Banda
        Dim alcance As Double = malla.Alcance

        Dim cambios As Integer = 0

        For Each z In conCoord

            If z.TipoApoyoManual Then Continue For

            Dim hayIzq As Boolean = False, hayDer As Boolean = False
            Dim hayAbajo As Boolean = False, hayArriba As Boolean = False

            For Each otra In conCoord
                If otra Is z Then Continue For

                Dim dxg As Double = otra.CoordX - z.CoordX
                Dim dyg As Double = otra.CoordY - z.CoordY
                If Math.Sqrt(dxg * dxg + dyg * dyg) > alcance Then Continue For

                ' Al marco de la malla: u a lo largo de la dirección dominante.
                Dim du As Double = dxg * cosT + dyg * senT
                Dim dv As Double = -dxg * senT + dyg * cosT

                If Math.Abs(dv) <= banda Then
                    If du < -tolerancia Then hayIzq = True
                    If du > tolerancia Then hayDer = True
                End If

                If Math.Abs(du) <= banda Then
                    If dv < -tolerancia Then hayAbajo = True
                    If dv > tolerancia Then hayArriba = True
                End If
            Next

            Dim libres As Integer = 0
            If Not hayIzq Then libres += 1
            If Not hayDer Then libres += 1
            If Not hayAbajo Then libres += 1
            If Not hayArriba Then libres += 1

            Dim propuesto As eTipoApoyoZapata
            Select Case libres
                Case 0 : propuesto = eTipoApoyoZapata.Central
                Case 1 : propuesto = eTipoApoyoZapata.Medianera
                Case Else : propuesto = eTipoApoyoZapata.Esquinera
            End Select

            If z.TipoApoyo <> propuesto Then
                z.TipoApoyo = propuesto
                cambios += 1
            End If

        Next

        Return cambios

    End Function

    ' =====================================================================
    ' Pesos estabilizantes
    ' =====================================================================

    ''' <summary>
    ''' Los tres pesos verticales que suman al P reactivo cuando el proyecto
    ''' activa el peso estabilizante. Van en kN.
    ''' </summary>
    Public Class PesosEstabilizantes

        Public Property W_Zapata As Double
        Public Property W_Pedestal As Double
        Public Property W_Suelo As Double

        Public ReadOnly Property Total As Double
            Get
                Return W_Zapata + W_Pedestal + W_Suelo
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Peso propio de la zapata, del pedestal y del suelo por encima de la
    ''' zapata, siguiendo el criterio típico del cálculo manual:
    '''
    '''   W_zapata   = L_b · L_h · e · γ_concreto
    '''   W_pedestal = b · h · (Df − e) · γ_concreto
    '''   W_suelo    = (L_b · L_h − b · h) · (Df − e) · γ_suelo
    '''
    ''' Df es la profundidad desde el terreno hasta el fondo de la zapata, así
    ''' (Df − e) es la altura de material que hay entre la cara superior de la
    ''' zapata y el terreno. Ese volumen se reparte entre el pedestal (dentro
    ''' del pedestal) y el suelo (fuera del pedestal, dentro de la huella de la
    ''' zapata). Con Df ≤ e el peso extra es solo el de la zapata.
    ''' </summary>
    Public Shared Function CalcularPesosEstabilizantes(z As cZapata) As PesosEstabilizantes

        Dim p As New PesosEstabilizantes()
        If z Is Nothing Then Return p

        Dim gc As Double = If(z.gammaConcreto > 0, z.gammaConcreto, 24.0)
        Dim gs As Double = If(z.gammaSuelo > 0, z.gammaSuelo, 0.0)
        Dim alturaSobreZapata As Double = Math.Max(0, z.Df - z.e)
        Dim huellaZapata As Double = Math.Max(0, z.L_b * z.L_h)
        Dim huellaPedestal As Double = Math.Max(0, z.b * z.h)
        Dim huellaSuelo As Double = Math.Max(0, huellaZapata - huellaPedestal)

        p.W_Zapata = huellaZapata * z.e * gc
        p.W_Pedestal = huellaPedestal * alturaSobreZapata * gc
        p.W_Suelo = huellaSuelo * alturaSobreZapata * gs

        Return p

    End Function

    ' =====================================================================
    ' Grupos de zapatas (patrón + hijas)
    ' =====================================================================

    ''' <summary>
    ''' El nombre del grupo, normalizado (recortado y sin distinguir mayúsculas
    ''' en los casos degenerados). Cadena vacía = zapata suelta, no agrupada.
    ''' </summary>
    Public Shared Function ClaveGrupo(z As cZapata) As String
        If z Is Nothing OrElse String.IsNullOrWhiteSpace(z.Grupo) Then Return ""
        Return z.Grupo.Trim()
    End Function

    ''' <summary>
    ''' Zapatas agrupadas por el string "Grupo". Las que tienen grupo vacío no
    ''' aparecen: se manejan como apoyos sueltos.
    ''' </summary>
    Public Shared Function AgruparZapatas(zapatas As List(Of cZapata)) _
                                          As Dictionary(Of String, List(Of cZapata))

        Dim res As New Dictionary(Of String, List(Of cZapata))(StringComparer.OrdinalIgnoreCase)
        If zapatas Is Nothing Then Return res

        For Each z In zapatas
            Dim clave = ClaveGrupo(z)
            If clave.Length = 0 Then Continue For
            If Not res.ContainsKey(clave) Then res(clave) = New List(Of cZapata)()
            res(clave).Add(z)
        Next

        Return res

    End Function

    ''' <summary>
    ''' Devuelve la zapata patrón del grupo. Si nadie está marcado como patrón,
    ''' se toma la primera por Label_joint (orden alfabético) como convención
    ''' estable, así el usuario ve siempre la misma sin importar cómo se
    ''' ordene la tabla.
    ''' </summary>
    Public Shared Function PatronDelGrupo(zapatasDelGrupo As IEnumerable(Of cZapata)) As cZapata

        If zapatasDelGrupo Is Nothing Then Return Nothing
        Dim lista = zapatasDelGrupo.Where(Function(z) z IsNot Nothing).ToList()
        If lista.Count = 0 Then Return Nothing

        Dim marcada = lista.FirstOrDefault(Function(z) z.EsPatron)
        If marcada IsNot Nothing Then Return marcada

        Return lista.OrderBy(Function(z) If(z.Label_joint, "")).First()

    End Function

    ''' <summary>
    ''' Después de que el usuario mueve zapatas entre grupos, asegura que cada
    ''' grupo tenga EXACTAMENTE una patrón. Si un grupo tiene varias marcadas,
    ''' se conserva la primera y se desmarca el resto; si no tiene ninguna, se
    ''' marca la primera por Label_joint. Devuelve la cantidad de cambios que
    ''' hizo, útil para saber si conviene refrescar la UI.
    ''' </summary>
    Public Shared Function AsegurarPatronPorGrupo(zapatas As List(Of cZapata)) As Integer

        If zapatas Is Nothing Then Return 0

        Dim cambios As Integer = 0
        Dim grupos = AgruparZapatas(zapatas)

        For Each kv In grupos
            Dim marcadas = kv.Value.Where(Function(z) z.EsPatron).ToList()

            If marcadas.Count = 0 Then
                Dim primera = PatronDelGrupo(kv.Value)
                If primera IsNot Nothing AndAlso Not primera.EsPatron Then
                    primera.EsPatron = True
                    cambios += 1
                End If
            ElseIf marcadas.Count > 1 Then
                ' Deja solo la primera marcada; el resto queda como hija
                For i As Integer = 1 To marcadas.Count - 1
                    marcadas(i).EsPatron = False
                    cambios += 1
                Next
            End If
        Next

        ' Zapatas sueltas (sin grupo) no deberían quedar marcadas como patrón:
        ' EsPatron sin grupo es un estado sin sentido. Se limpia.
        For Each z In zapatas
            If ClaveGrupo(z).Length = 0 AndAlso z.EsPatron Then
                z.EsPatron = False
                cambios += 1
            End If
        Next

        Return cambios

    End Function

    ''' <summary>
    ''' Copia de la zapata patrón a la hija los campos que definen "cómo es" la
    ''' zapata: geometría, pedestal, materiales, refuerzo, profundidad de
    ''' desplante, pesos específicos, capacidad admisible y factores. Se
    ''' preservan sin tocar: Label_joint, Nombre, coordenadas, tipo de apoyo
    ''' (que depende de la posición y puede diferir dentro del grupo),
    ''' combinaciones y Resultados.
    ''' </summary>
    Public Shared Sub SincronizarConPatron(hija As cZapata, patron As cZapata)

        If hija Is Nothing OrElse patron Is Nothing OrElse hija Is patron Then Exit Sub

        ' Geometría zapata
        hija.L_b = patron.L_b
        hija.L_h = patron.L_h
        hija.e = patron.e
        hija.rec = patron.rec
        hija.d = patron.d

        ' Pedestal
        hija.b = patron.b
        hija.h = patron.h

        ' Materiales
        hija.fc = patron.fc
        hija.fy = patron.fy

        ' Suelo y factores
        hija.qAdm_Est = patron.qAdm_Est
        hija.qAdm_Din = patron.qAdm_Din
        hija.gammaSuelo = patron.gammaSuelo
        hija.mu = patron.mu
        hija.FD_E = patron.FD_E
        hija.FD_D = patron.FD_D

        ' Peso estabilizante
        hija.Df = patron.Df
        hija.gammaConcreto = patron.gammaConcreto

        ' Cuantías y refuerzo
        hija.Rho_L1 = patron.Rho_L1
        hija.Rho_L2 = patron.Rho_L2

        If patron.Refuerzos Is Nothing Then
            hija.Refuerzos = New List(Of cRefuerzo)
        Else
            hija.Refuerzos = New List(Of cRefuerzo)
            For Each r In patron.Refuerzos
                If r Is Nothing Then Continue For
                hija.Refuerzos.Add(New cRefuerzo With {
                    .Direccion = r.Direccion,
                    .Tipo = r.Tipo,
                    .Diametro = r.Diametro,
                    .Diametro_mm = r.Diametro_mm,
                    .AreaBarra = r.AreaBarra,
                    .Cantidad = r.Cantidad,
                    .Espaciamiento = r.Espaciamiento
                })
            Next
        End If

    End Sub

    ''' <summary>
    ''' Sincroniza todas las hijas de todos los grupos con su patrón, previa
    ''' llamada a AsegurarPatronPorGrupo para que ninguna quede huérfana.
    ''' Devuelve cuántas hijas fueron actualizadas.
    ''' </summary>
    Public Shared Function SincronizarTodosLosGrupos(zapatas As List(Of cZapata)) As Integer

        If zapatas Is Nothing Then Return 0

        AsegurarPatronPorGrupo(zapatas)

        Dim n As Integer = 0
        For Each kv In AgruparZapatas(zapatas)
            Dim patron = PatronDelGrupo(kv.Value)
            If patron Is Nothing Then Continue For
            For Each hija In kv.Value
                If hija Is patron Then Continue For
                SincronizarConPatron(hija, patron)
                n += 1
            Next
        Next
        Return n

    End Function

    ''' <summary>Etiqueta legible, para tablas y reportes.</summary>
    Public Shared Function NombreTipo(tipo As eTipoApoyoZapata) As String
        Select Case tipo
            Case eTipoApoyoZapata.Esquinera : Return "Esquinera"
            Case eTipoApoyoZapata.Medianera : Return "Medianera"
            Case Else : Return "Central"
        End Select
    End Function

    ' =====================================================================
    ' Resumen de una zapata: la revisión que gobierna
    ' =====================================================================

    ''' <summary>
    ''' La peor de las cinco revisiones de una zapata, con el nombre de la que
    ''' gobierna y la combinación que la produce. Es lo que se pinta en la vista
    ''' en planta y lo que se puede llevar a un resumen ejecutivo.
    ''' </summary>
    Public Class ResumenZapata

        ''' <summary>False cuando la zapata aún no se ha calculado.</summary>
        Public Property TieneResultados As Boolean

        ''' <summary>Menor C/D de todas las revisiones y todas las combinaciones.</summary>
        Public Property PeorFactor As Double = Double.MaxValue

        ''' <summary>Cuál revisión da ese peor factor.</summary>
        Public Property Revision As String = ""

        ''' <summary>Qué combinación lo produce.</summary>
        Public Property Combinacion As String = ""

        Public ReadOnly Property Cumple As Boolean
            Get
                Return TieneResultados AndAlso PeorFactor >= Funciones_00_Varias.UMBRAL_CD
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Las cinco relaciones capacidad/demanda de una zapata bajo UNA
    ''' combinación. Cero significa "esta revisión no aplica en esta
    ''' combinación" (no hay demanda), no "capacidad infinita".
    ''' </summary>
    Public Class FactoresCombinacion

        Public Property Combinacion As String = ""

        ''' <summary>
        ''' De qué lista viene. Importa porque la capacidad admisible del suelo
        ''' es distinta en estático y en dinámico, y el límite de excentricidad
        ''' también.
        ''' </summary>
        Public Property EsDinamica As Boolean

        Public Property Suelo As Double
        Public Property Excentricidad As Double
        Public Property Punzonamiento As Double
        Public Property Cortante As Double
        Public Property Flexion As Double

        ''' <summary>Nombre de la revisión que gobierna, o cadena vacía si ninguna aplica.</summary>
        Public ReadOnly Property Revision As String
            Get
                Dim mejor As String = ""
                Dim menor As Double = Double.MaxValue
                For Each p In Pares()
                    If p.Item2 > 0 AndAlso p.Item2 < menor Then
                        menor = p.Item2
                        mejor = p.Item1
                    End If
                Next
                Return mejor
            End Get
        End Property

        ''' <summary>Menor de las cinco, ignorando las que no aplican. 0 si ninguna aplica.</summary>
        Public ReadOnly Property Peor As Double
            Get
                Dim menor As Double = Double.MaxValue
                For Each p In Pares()
                    If p.Item2 > 0 AndAlso p.Item2 < menor Then menor = p.Item2
                Next
                Return If(menor = Double.MaxValue, 0, menor)
            End Get
        End Property

        Private Function Pares() As List(Of Tuple(Of String, Double))
            Return New List(Of Tuple(Of String, Double)) From {
                Tuple.Create(If(EsDinamica, "Suelo dinámico", "Suelo estático"), Suelo),
                Tuple.Create("Excentricidad", Excentricidad),
                Tuple.Create("Punzonamiento", Punzonamiento),
                Tuple.Create("Cortante", Cortante),
                Tuple.Create("Flexión", Flexion)
            }
        End Function

    End Class

    ''' <summary>
    ''' Las relaciones capacidad/demanda de cada combinación calculada. Son las
    ''' mismas que arma la tabla de reporte del módulo:
    '''
    '''   Suelo             qAdm / qMax, con la admisible estática o dinámica
    '''                     según de qué lista venga la combinación
    '''   Punzonamiento     Vc_p / |Vu_p|
    '''   Cortante          el menor de Vc2/max(Vu1,Vu3) y Vc1/max(Vu2,Vu4)
    '''   Flexión           el menor de rho colocado / rho requerido en cada dirección
    '''
    ''' Una revisión sin demanda queda en 0. Es deliberado: dividir por cero da
    ''' infinito, y un infinito arrastrado a un mínimo haría creer que la zapata
    ''' está sobrada cuando lo que pasa es que esa revisión no aplica.
    '''
    ''' Una combinación que no aparece en ninguna de las dos listas se trata como
    ''' estática. Pasa cuando se cambian las listas sin recalcular; usar la
    ''' admisible menor deja el resultado del lado seguro en vez de omitir la
    ''' revisión del suelo.
    ''' </summary>
    Public Shared Function FactoresPorCombinacion(z As cZapata) As List(Of FactoresCombinacion)

        Dim lista As New List(Of FactoresCombinacion)()
        If z Is Nothing OrElse z.Resultados Is Nothing Then Return lista

        Dim dinamicas As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If z.Lista_Combinaciones_Dinamicas IsNot Nothing Then
            For Each c In z.Lista_Combinaciones_Dinamicas
                If Not String.IsNullOrEmpty(c.LoadCase) Then dinamicas.Add(c.LoadCase)
            Next
        End If

        For Each par In z.Resultados

            Dim res = par.Value
            If res Is Nothing Then Continue For

            Dim f As New FactoresCombinacion() With {
                .Combinacion = par.Key,
                .EsDinamica = dinamicas.Contains(par.Key)
            }

            Dim qAdm As Double = If(f.EsDinamica, z.qAdm_Din, z.qAdm_Est)
            If qAdm > 0 AndAlso res.qMax > 0 Then f.Suelo = qAdm / res.qMax

            ' Excentricidad: menor de los dos cocientes por dirección. Solo
            ' aplica si ex/ey se calcularon (P > 0 y límites > 0). Cuando la
            ' excentricidad en una dirección es prácticamente nula, esa dirección
            ' no aporta al mínimo (queda holgada, no se cuela como cero).
            Dim exc As Double = Double.MaxValue
            If res.Lim_x_usado > 0 AndAlso Math.Abs(res.ex) > 0.000000001 Then
                exc = Math.Min(exc, res.Lim_x_usado / Math.Abs(res.ex))
            End If
            If res.Lim_y_usado > 0 AndAlso Math.Abs(res.ey) > 0.000000001 Then
                exc = Math.Min(exc, res.Lim_y_usado / Math.Abs(res.ey))
            End If
            If exc <> Double.MaxValue Then f.Excentricidad = exc

            Dim vu As Double = Math.Abs(res.Vu_p)
            If vu > 0 Then f.Punzonamiento = res.Vc_p / vu

            Dim vuA As Double = Math.Max(res.Vu1_C, res.Vu3_C)
            Dim vuB As Double = Math.Max(res.Vu2_C, res.Vu4_C)
            Dim cort As Double = Double.MaxValue
            If vuA > 0 Then cort = Math.Min(cort, res.Vc2_C / vuA)
            If vuB > 0 Then cort = Math.Min(cort, res.Vc1_C / vuB)
            If cort <> Double.MaxValue Then f.Cortante = cort

            Dim flex As Double = Double.MaxValue
            If res.Rho_1 > 0 Then flex = Math.Min(flex, z.Rho_L1 / res.Rho_1)
            If res.Rho_2 > 0 Then flex = Math.Min(flex, z.Rho_L2 / res.Rho_2)
            If flex <> Double.MaxValue Then f.Flexion = flex

            lista.Add(f)

        Next

        Return lista

    End Function

    ''' <summary>
    ''' La peor de todas las revisiones en todas las combinaciones. Es lo que
    ''' colorea la vista en planta y lo que va al resumen del reporte.
    ''' </summary>
    Public Shared Function Resumir(z As cZapata) As ResumenZapata

        Dim r As New ResumenZapata()

        For Each f In FactoresPorCombinacion(z)
            r.TieneResultados = True
            Dim peor = f.Peor
            If peor <= 0 OrElse peor >= r.PeorFactor Then Continue For
            r.PeorFactor = peor
            r.Revision = f.Revision
            r.Combinacion = f.Combinacion
        Next

        If r.PeorFactor = Double.MaxValue Then r.PeorFactor = 0

        Return r

    End Function

    ' =====================================================================
    ' Resumen por grupo
    ' =====================================================================

    ''' <summary>
    ''' El peor factor entre todos los apoyos de un grupo. Sirve para el modo
    ''' "una fila por grupo" del reporte: el grupo cumple si su peor apoyo
    ''' cumple.
    ''' </summary>
    Public Class ResumenGrupo

        Public Property Grupo As String = ""
        Public Property Patron As cZapata
        Public Property Cantidad As Integer
        Public Property PeorZapata As cZapata
        Public Property PeorFactor As Double = Double.MaxValue
        Public Property Revision As String = ""
        Public Property Combinacion As String = ""
        Public Property TieneResultados As Boolean

        Public ReadOnly Property Cumple As Boolean
            Get
                Return TieneResultados AndAlso PeorFactor >= Funciones_00_Varias.UMBRAL_CD
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Recorre las zapatas del grupo y encuentra la de peor C/D, guardando qué
    ''' revisión y combinación gobiernan. Es lo que va en el modo "por grupo"
    ''' del reporte.
    ''' </summary>
    Public Shared Function ResumirGrupo(grupo As String, zapatasDelGrupo As List(Of cZapata)) As ResumenGrupo

        Dim r As New ResumenGrupo() With {.Grupo = grupo}
        If zapatasDelGrupo Is Nothing OrElse zapatasDelGrupo.Count = 0 Then Return r

        r.Cantidad = zapatasDelGrupo.Count
        r.Patron = PatronDelGrupo(zapatasDelGrupo)

        For Each z In zapatasDelGrupo
            Dim res = Resumir(z)
            If Not res.TieneResultados Then Continue For
            r.TieneResultados = True
            If res.PeorFactor <= 0 OrElse res.PeorFactor >= r.PeorFactor Then Continue For
            r.PeorFactor = res.PeorFactor
            r.PeorZapata = z
            r.Revision = res.Revision
            r.Combinacion = res.Combinacion
        Next

        If r.PeorFactor = Double.MaxValue Then r.PeorFactor = 0

        Return r

    End Function

End Class
