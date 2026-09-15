''' <summary>
''' Reparte la altura de un contenedor entre varios controles apilados en
''' vertical, para que un formulario diseñado en una pantalla grande siga
''' siendo usable en una más baja.
'''
''' El problema que resuelve: los GroupBox de los formularios de ARCO están
''' anclados Top+Left+Right, así que se estiran a lo ancho pero conservan su
''' alto y su Y de diseño. En un portátil de 768 px de alto, todo lo que en
''' el diseño quedaba por debajo de ~620 px simplemente no se ve, y no hay
''' forma de llegar a ello.
'''
''' Cómo reparte (de mayor a menor espacio disponible):
'''   1. Sobra espacio  → cada control crece en proporción a su alto de diseño.
'''   2. Espacio justo   → se interpola entre el alto mínimo y el de diseño.
'''   3. No alcanza ni con los mínimos → todos al mínimo y se activa el scroll
'''      del contenedor, de modo que nada quede fuera de alcance.
'''
''' Uso:
'''     _pila = New PilaVerticalAdaptable(TabPage3)
'''     _pila.Agregar(GroupBox7, altoMinimo:=170)
'''     _pila.Agregar(GroupBox_Cortante, altoMinimo:=170)
'''     _pila.Activar()                 ' en el Load
'''     ...
'''     _pila.Aplicar()                 ' en el Resize del formulario
'''
''' La geometría de diseño (Y, alto y separaciones) se captura en Activar(),
''' así que el Designer sigue siendo la única fuente de la maqueta original:
''' esta clase solo la escala.
''' </summary>
Public Class PilaVerticalAdaptable

    Private Class Elemento
        Public Ctrl As Control
        Public AltoDiseno As Integer
        Public AltoMinimo As Integer
        Public Separacion As Integer     ' hueco respecto al elemento anterior
    End Class

    Private ReadOnly _contenedor As ScrollableControl
    Private ReadOnly _elementos As New List(Of Elemento)
    Private ReadOnly _inferiores As New List(Of Control)

    Private _topInicial As Integer = 0
    Private _margenInferior As Integer = 12
    Private _sepInferiores As Integer = 14
    Private _altoInferiores As Integer = 0

    Private _activa As Boolean = False
    Private _aplicando As Boolean = False
    Private _scrollActivo As Boolean = False
    Private _enganchado As Boolean = False

    ''' <param name="contenedor">
    ''' TabPage, Panel o cualquier ScrollableControl: se necesita AutoScroll
    ''' para el modo "no alcanza ni con los mínimos".
    ''' </param>
    Public Sub New(contenedor As ScrollableControl)
        _contenedor = contenedor
    End Sub

    ''' <summary>Margen libre bajo el último control de la pila.</summary>
    Public Property MargenInferior As Integer
        Get
            Return _margenInferior
        End Get
        Set(value As Integer)
            _margenInferior = Math.Max(0, value)
        End Set
    End Property

    ''' <summary>
    ''' Reparte la altura disponible en PARTES IGUALES entre los controles de la
    ''' pila, ignorando altos de diseño y mínimos, y sin activar el scroll del
    ''' contenedor.
    '''
    ''' Es el modo adecuado cuando cada control sabe desplazarse por su cuenta
    ''' — un DataGridView con ScrollBars.Both, por ejemplo. En vez de intentar
    ''' que quepa todo el contenido, cada bloque recibe su mitad (o su tercio) de
    ''' la ventana y el usuario se desplaza DENTRO de la tabla que esté mirando.
    ''' Predecible y sin sorpresas: el reparto no depende de cuántas filas traiga
    ''' la viga seleccionada.
    ''' </summary>
    Public Property RepartoEquitativo As Boolean = False

    ''' <summary>Hueco entre el último control de la pila y los controles inferiores.</summary>
    Public Property SeparacionInferiores As Integer
        Get
            Return _sepInferiores
        End Get
        Set(value As Integer)
            _sepInferiores = Math.Max(0, value)
        End Set
    End Property

    ''' <summary>
    ''' Agrega un control a la pila. <paramref name="altoMinimo"/> es el alto por
    ''' debajo del cual el control deja de ser legible (una tabla con dos filas,
    ''' un diagrama que ya no se entiende); llegado ahí aparece el scroll en vez
    ''' de seguir encogiendo.
    ''' </summary>
    Public Sub Agregar(ctrl As Control, altoMinimo As Integer)

        If ctrl Is Nothing Then Exit Sub

        Dim sep As Integer
        If _elementos.Count = 0 Then
            _topInicial = ctrl.Top
            sep = 0
        Else
            Dim ant = _elementos(_elementos.Count - 1)
            sep = ctrl.Top - (ant.Ctrl.Top + ant.AltoDiseno)
            If sep < 0 Then sep = 0
        End If

        _elementos.Add(New Elemento With {
            .Ctrl = ctrl,
            .AltoDiseno = ctrl.Height,
            .AltoMinimo = Math.Min(altoMinimo, ctrl.Height),
            .Separacion = sep
        })

    End Sub

    ''' <summary>
    ''' Controles que van pegados DEBAJO de la pila (una fila de botones, por
    ''' ejemplo). Conservan su alto y bajan o suben junto con la pila; su
    ''' posición horizontal no se toca.
    ''' </summary>
    Public Sub AgregarInferior(ParamArray ctrls() As Control)
        For Each c In ctrls
            If c Is Nothing Then Continue For
            _inferiores.Add(c)
            _altoInferiores = Math.Max(_altoInferiores, c.Height)
        Next
    End Sub

    ''' <summary>
    ''' Cambia el alto mínimo de un control ya agregado y vuelve a repartir.
    '''
    ''' Se usa cuando el contenido solo se conoce en tiempo de ejecución: una
    ''' tabla de resultados no sabe cuánto alto necesita hasta que se construye
    ''' con las filas de la viga seleccionada. Ver AltoNaturalGrid().
    ''' </summary>
    Public Sub ActualizarAltoMinimo(ctrl As Control, altoMinimo As Integer)

        If ctrl Is Nothing Then Exit Sub

        Dim elem = _elementos.FirstOrDefault(Function(x) x.Ctrl Is ctrl)
        If elem Is Nothing Then Exit Sub

        elem.AltoMinimo = Math.Max(0, altoMinimo)

        ' El mínimo real puede superar al alto de diseño (una tabla con más filas
        ' de las que cabían en la maqueta original). En ese caso el alto de diseño
        ' deja de ser un techo y pasa a ser el mínimo, para no romper la invariante
        ' sumaDiseno >= sumaMinima de la que depende el reparto.
        If elem.AltoMinimo > elem.AltoDiseno Then elem.AltoDiseno = elem.AltoMinimo

        Aplicar()

    End Sub

    ''' <summary>
    ''' Alto que necesita un DataGridView para mostrar TODAS sus filas sin
    ''' barra vertical propia: encabezado de columnas + alto de cada fila +
    ''' bordes, más la barra horizontal, que también come alto.
    ''' </summary>
    ''' <param name="chrome">
    ''' Alto extra del contenedor que envuelve la tabla. Para un GroupBox son
    ''' unos 30 px entre el título y los bordes.
    ''' </param>
    Public Shared Function AltoNaturalGrid(dgv As DataGridView,
                                           Optional chrome As Integer = 0) As Integer

        If dgv Is Nothing Then Return 0

        Dim alto As Integer = dgv.ColumnHeadersHeight + 2
        For Each fila As DataGridViewRow In dgv.Rows
            alto += fila.Height
        Next

        alto += SystemInformation.HorizontalScrollBarHeight

        Return alto + chrome

    End Function

    Private Function RepartiendoEnPartesIguales() As Boolean
        Return RepartoEquitativo AndAlso _elementos.Count > 0
    End Function

    ''' <summary>
    ''' Captura la maqueta de diseño, se engancha al contenedor y hace el primer
    ''' reparto.
    '''
    ''' El enganche a SizeChanged es lo que hace fiable todo esto. Activar() se
    ''' llama desde el Load del formulario, y en ese momento el contenedor todavía
    ''' reporta su tamaño de DISEÑO, no el real: una TabPage diseñada a 917 px de
    ''' alto sigue diciendo 917 hasta que el formulario se muestra y el TabControl
    ''' la redimensiona a lo que de verdad hay (~594). Repartir sobre 917 coloca
    ''' el último bloque más abajo del borde visible, y como cada tabla cree que
    ''' su contenido le cabe, ni siquiera saca barra de desplazamiento: las
    ''' últimas filas quedan fuera y sin forma de llegar a ellas.
    '''
    ''' Escuchando SizeChanged el reparto se rehace solo en cuanto el contenedor
    ''' toma su tamaño real, sin depender de que el formulario dispare Shown o
    ''' Resize en el momento oportuno.
    ''' </summary>
    Public Sub Activar()

        If _elementos.Count = 0 Then Exit Sub
        _activa = True

        If Not _enganchado Then
            AddHandler _contenedor.SizeChanged, AddressOf ContenedorRedimensionado
            _enganchado = True
        End If

        Aplicar()

    End Sub

    Private Sub ContenedorRedimensionado(sender As Object, e As EventArgs)
        Aplicar()
    End Sub

    ''' <summary>
    ''' Reparte la altura disponible. Llamar desde el Resize del formulario.
    ''' Es reentrante-segura y no hace nada si el contenedor no es visible aún.
    ''' </summary>
    Public Sub Aplicar()

        If Not _activa OrElse _aplicando Then Exit Sub
        If _contenedor Is Nothing OrElse _contenedor.IsDisposed Then Exit Sub
        If _contenedor.ClientSize.Height <= 0 Then Exit Sub

        _aplicando = True
        _contenedor.SuspendLayout()

        Try
            Dim sepTotal As Integer = _elementos.Sum(Function(e) e.Separacion)
            Dim reservaInferior As Integer = If(_inferiores.Count > 0,
                                                _sepInferiores + _altoInferiores,
                                                0)

            Dim disponible As Integer = _contenedor.ClientSize.Height -
                                        _topInicial - sepTotal -
                                        reservaInferior - _margenInferior

            Dim sumaDiseno As Integer = _elementos.Sum(Function(e) e.AltoDiseno)
            Dim sumaMinima As Integer = _elementos.Sum(Function(e) e.AltoMinimo)

            Dim alturas(_elementos.Count - 1) As Integer
            Dim necesitaScroll As Boolean = False

            If RepartiendoEnPartesIguales() Then
                ' Partes iguales: cada bloque recibe la misma fracción de la
                ' altura disponible y se apoya en su propia barra interna.
                Dim cuotaIgual As Integer = disponible \ _elementos.Count
                Dim asignado As Integer = 0
                For i = 0 To _elementos.Count - 1
                    If i = _elementos.Count - 1 Then
                        alturas(i) = disponible - asignado   ' el último absorbe el resto
                    Else
                        alturas(i) = cuotaIgual
                        asignado += cuotaIgual
                    End If
                    If alturas(i) < 1 Then alturas(i) = 1
                Next

            ElseIf disponible >= sumaDiseno Then
                ' Sobra espacio: el extra se reparte en proporción al diseño, de
                ' modo que el bloque que era más alto siga siendo el más alto.
                Dim extra As Integer = disponible - sumaDiseno
                Dim repartido As Integer = 0
                For i = 0 To _elementos.Count - 1
                    Dim cuota As Integer
                    If i = _elementos.Count - 1 Then
                        cuota = extra - repartido          ' el último absorbe el redondeo
                    Else
                        cuota = CInt(Math.Floor(extra * (_elementos(i).AltoDiseno / sumaDiseno)))
                        repartido += cuota
                    End If
                    alturas(i) = _elementos(i).AltoDiseno + cuota
                Next

            ElseIf disponible <= sumaMinima Then
                ' No alcanza ni con los mínimos: se respeta el mínimo y el
                ' contenedor pasa a tener scroll, para que nada quede inalcanzable.
                For i = 0 To _elementos.Count - 1
                    alturas(i) = _elementos(i).AltoMinimo
                Next
                necesitaScroll = True

            Else
                ' Espacio intermedio: interpolación lineal mínimo → diseño.
                Dim rango As Integer = sumaDiseno - sumaMinima
                Dim t As Double = If(rango > 0, (disponible - sumaMinima) / rango, 0.0)
                Dim acumulado As Integer = 0
                For i = 0 To _elementos.Count - 1
                    Dim e = _elementos(i)
                    If i = _elementos.Count - 1 Then
                        alturas(i) = disponible - acumulado
                    Else
                        alturas(i) = e.AltoMinimo + CInt(Math.Floor((e.AltoDiseno - e.AltoMinimo) * t))
                        acumulado += alturas(i)
                    End If
                Next
            End If

            ' El scroll solo se conmuta cuando cambia el modo: hacerlo en cada
            ' Resize reiniciaría la posición de scroll en mitad de un arrastre.
            If necesitaScroll <> _scrollActivo Then
                _contenedor.AutoScroll = necesitaScroll
                _scrollActivo = necesitaScroll
            End If

            ' Con AutoScroll activo las coordenadas de los hijos van desplazadas
            ' por AutoScrollPosition (que es <= 0). Sin AutoScroll vale (0,0),
            ' así que la misma fórmula sirve en los dos modos.
            Dim desfase As Integer = _contenedor.AutoScrollPosition.Y

            Dim y As Integer = _topInicial
            For i = 0 To _elementos.Count - 1
                y += _elementos(i).Separacion
                Dim c = _elementos(i).Ctrl
                c.Top = y + desfase
                c.Height = Math.Max(1, alturas(i))
                y += alturas(i)
            Next

            If _inferiores.Count > 0 Then
                Dim yInf As Integer = y + _sepInferiores + desfase
                For Each c In _inferiores
                    c.Top = yInf
                Next
            End If

        Catch ex As Exception
            Logger.Error(ex, "PilaVerticalAdaptable.Aplicar", _contenedor.Name)
        Finally
            _contenedor.ResumeLayout()
            _aplicando = False
        End Try

    End Sub

    ' -----------------------------------------------------------------------
    ' Ajuste del formulario al monitor donde se abre.
    ' -----------------------------------------------------------------------

    ''' <summary>
    ''' Adapta el tamaño inicial de un formulario al monitor donde se abre. Si
    ''' el tamaño de diseño no cabe en el área de trabajo (descontando barra de
    ''' tareas), lo maximiza; si cabe, lo respeta.
    '''
    ''' Los formularios de ARCO están diseñados a 1920×1080 o más, y en un
    ''' portátil de 1366×768 nacían más altos que la pantalla: la parte de
    ''' abajo quedaba por fuera del escritorio, sin scroll ni forma de llegar.
    ''' </summary>
    ''' <param name="frm">Formulario a ajustar.</param>
    ''' <param name="anchoMinimo">Ancho mínimo utilizable.</param>
    ''' <param name="altoMinimo">Alto mínimo utilizable.</param>
    Public Shared Sub AjustarAPantalla(frm As Form,
                                       Optional anchoMinimo As Integer = 1024,
                                       Optional altoMinimo As Integer = 600)

        If frm Is Nothing Then Exit Sub

        Try
            Dim area As Rectangle = Screen.FromControl(frm).WorkingArea

            ' El mínimo nunca puede exceder lo que da la pantalla, o el usuario
            ' no podría ni redimensionar la ventana.
            frm.MinimumSize = New Size(Math.Min(anchoMinimo, area.Width),
                                       Math.Min(altoMinimo, area.Height))

            If frm.Width > area.Width OrElse frm.Height > area.Height Then
                frm.WindowState = FormWindowState.Maximized
            End If

        Catch ex As Exception
            Logger.Error(ex, "PilaVerticalAdaptable.AjustarAPantalla", frm.Name)
        End Try

    End Sub

    ''' <summary>
    ''' Ajuste mínimo para formularios cuya maqueta interna NO es adaptable: los
    ''' controles conservan su Y y su alto de diseño, pero al menos se puede
    ''' llegar a todos.
    '''
    ''' Maximizar por sí solo NO basta: al maximizar el alto llega como mucho al
    ''' del área de trabajo, y si el contenido se diseñó para 1061 px en una
    ''' pantalla de 816 lo de abajo sigue sin verse. De ahí el scroll.
    '''
    ''' No usar en formularios que ya reparten su alto con PilaVerticalAdaptable
    ''' (como Form_09_Vigas): esas pestañas gestionan su propio AutoScroll.
    ''' </summary>
    Public Shared Sub AjustarAPantallaConScroll(frm As Form,
                                                Optional anchoMinimo As Integer = 1024,
                                                Optional altoMinimo As Integer = 600)

        If frm Is Nothing Then Exit Sub

        AjustarAPantalla(frm, anchoMinimo, altoMinimo)
        HabilitarScrollEnContenedores(frm)

    End Sub

    ''' <summary>
    ''' Activa AutoScroll en los contenedores desplazables del árbol que tengan
    ''' hijos con posición fija (Dock = None).
    '''
    ''' Por qué recorrer el árbol y no poner AutoScroll en el formulario: casi
    ''' todos los formularios de ARCO tienen un TabControl o un Panel con
    ''' Dock = Fill colgando directamente del formulario. Ese hijo siempre mide
    ''' exactamente lo que mide el área cliente, así que nada lo desborda y el
    ''' AutoScroll del formulario no se dispararía jamás. El recorte ocurre más
    ''' adentro, en las TabPage y paneles que sí contienen los GroupBox con Y y
    ''' alto fijos, y es ahí donde hay que habilitarlo.
    '''
    ''' Activarlo es gratis cuando el contenido cabe: WinForms solo dibuja la
    ''' barra cuando algún hijo desborda el área cliente.
    '''
    ''' GroupBox, DataGridView y Chart derivan de Control, no de
    ''' ScrollableControl, así que quedan fuera del recorrido por sí solos.
    ''' </summary>
    Public Shared Sub HabilitarScrollEnContenedores(raiz As Control)

        If raiz Is Nothing Then Exit Sub

        Try
            Dim cont = TryCast(raiz, ScrollableControl)
            If cont IsNot Nothing Then
                Dim tieneHijosFijos As Boolean = False
                For Each c As Control In cont.Controls
                    If c.Dock = DockStyle.None Then
                        tieneHijosFijos = True
                        Exit For
                    End If
                Next
                If tieneHijosFijos Then cont.AutoScroll = True
            End If

            For Each c As Control In raiz.Controls
                HabilitarScrollEnContenedores(c)
            Next

        Catch ex As Exception
            Logger.Error(ex, "PilaVerticalAdaptable.HabilitarScrollEnContenedores", raiz.Name)
        End Try

    End Sub

End Class
