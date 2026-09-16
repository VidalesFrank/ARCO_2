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
