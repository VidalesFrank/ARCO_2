Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.Windows.Input

''' <summary>
''' ViewModel raíz de la ventana principal del módulo. Orquesta el catálogo
''' de pilotes, el catálogo de dados, la asignación por apoyo y los resultados.
''' Mantiene al Proyecto como fuente de verdad; todas las ediciones mutan
''' Proyecto.Elementos.Pilotes in-place.
''' </summary>
Public Class PiloteOverviewVM
    Inherits ObservableVM

    Public ReadOnly Proyecto As Proyecto
    Public ReadOnly Modelo As cPilotes

    ' WPF Binding reflexiona sobre PROPIEDADES; con campos (aun Public ReadOnly)
    ' falla silenciosamente y los ItemsSource quedan vacíos. Se exponen como
    ' ReadOnly Property para que DataGrid/ListBox las vean.
    Public ReadOnly Property TiposPilote As New ObservableCollection(Of TipoPiloteVM)
    Public ReadOnly Property TiposDado As New ObservableCollection(Of TipoDadoVM)
    Public ReadOnly Property Dados As New ObservableCollection(Of DadoVM)

    Private _tipoPiloteSeleccionado As TipoPiloteVM
    Private _tipoDadoSeleccionado As TipoDadoVM
    Private _dadoSeleccionado As DadoVM
    Private _vistaActiva As String = "Inicio"
    Private _mostrarCombosFueraDeSeleccion As Boolean = False
    Private _modoColorPlanta As String = "CDMin"
    Private _mostrarMallaMetrica As Boolean = True
    Private _mostrarEjesETABS As Boolean = True

    ''' <summary>
    ''' Visibilidad de la malla métrica (líneas tenues cada 1 m). Se desactiva
    ''' cuando hay ejes ETABS importados y se vuelve redundante.
    ''' </summary>
    Public Property MostrarMallaMetrica As Boolean
        Get
            Return _mostrarMallaMetrica
        End Get
        Set(value As Boolean)
            If SetProperty(_mostrarMallaMetrica, value) Then
                OnPropertyChanged(NameOf(MostrarMallaMetrica))
            End If
        End Set
    End Property

    ''' <summary>
    ''' Visibilidad de los ejes de ETABS (A, B, 1, 2, …). Independiente de que
    ''' el proyecto tenga grids — si no tiene, el flag no hace nada.
    ''' </summary>
    Public Property MostrarEjesETABS As Boolean
        Get
            Return _mostrarEjesETABS
        End Get
        Set(value As Boolean)
            If SetProperty(_mostrarEjesETABS, value) Then
                OnPropertyChanged(NameOf(MostrarEjesETABS))
            End If
        End Set
    End Property

    ''' <summary>Verdadero si el proyecto tiene al menos un GridLine ETABS.</summary>
    Public ReadOnly Property TieneEjesETABS As Boolean
        Get
            Return Proyecto IsNot Nothing AndAlso
                   Proyecto.Elementos IsNot Nothing AndAlso
                   Proyecto.Elementos.Grids IsNot Nothing AndAlso
                   Proyecto.Elementos.Grids.GridLines IsNot Nothing AndAlso
                   Proyecto.Elementos.Grids.GridLines.Count > 0
        End Get
    End Property

    ''' <summary>
    ''' Título de la ventana. Incluye el nombre del proyecto (o el archivo de
    ''' respaldo) para que el usuario identifique sin ambigüedad qué proyecto
    ''' tiene abierto. Formato: "ARCO · Pilotes — {nombre}".
    ''' </summary>
    Public ReadOnly Property TituloVentana As String
        Get
            Dim nombre As String = ""
            If Proyecto IsNot Nothing Then
                If Proyecto.Info IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(Proyecto.Info.Nombre) Then
                    nombre = Proyecto.Info.Nombre
                ElseIf Not String.IsNullOrWhiteSpace(Proyecto.Ruta) Then
                    nombre = System.IO.Path.GetFileNameWithoutExtension(Proyecto.Ruta)
                End If
            End If
            If String.IsNullOrWhiteSpace(nombre) Then
                Return "ARCO · Pilotes — Vista nueva"
            End If
            Return $"ARCO · Pilotes — {nombre}"
        End Get
    End Property

    ''' <summary>
    ''' Opciones del combo "Colorear por" de la planta de cimentación.
    ''' Las claves coinciden con lo que lee ColorDePorEstado en el code-behind:
    '''   CDMin        · Peor C/D entre compresión y tracción (default).
    '''   CDCompresion · Solo C/D compresión.
    '''   CDTraccion   · Solo C/D tracción (muchos apoyos quedarán grises por N/A).
    '''   PuMax        · Gradiente azul por magnitud de Pu máximo.
    '''   TipoDado     · Colores categóricos: un color distinto por NombreTipoDado.
    ''' </summary>
    Public ReadOnly Property ModosColorPlanta As New ObservableCollection(Of String) From {
        "C/D mínimo",
        "C/D compresión",
        "C/D tracción",
        "Pu máximo",
        "Tipo de dado"
    }

    Public Property ModoColorPlantaLabel As String
        Get
            Return LabelDesdeClave(_modoColorPlanta)
        End Get
        Set(value As String)
            Dim nuevo = ClaveDesdeLabel(value)
            If SetProperty(_modoColorPlanta, nuevo) Then
                OnPropertyChanged(NameOf(ModoColorPlanta))
                OnPropertyChanged(NameOf(ModoColorPlantaLabel))
            End If
        End Set
    End Property

    ''' <summary>Clave interna del modo activo (CDMin / CDCompresion / CDTraccion / PuMax / TipoDado).</summary>
    Public ReadOnly Property ModoColorPlanta As String
        Get
            Return _modoColorPlanta
        End Get
    End Property

    Private Shared Function ClaveDesdeLabel(label As String) As String
        Select Case label
            Case "C/D compresión" : Return "CDCompresion"
            Case "C/D tracción" : Return "CDTraccion"
            Case "Pu máximo" : Return "PuMax"
            Case "Tipo de dado" : Return "TipoDado"
            Case Else : Return "CDMin"
        End Select
    End Function

    Private Shared Function LabelDesdeClave(clave As String) As String
        Select Case clave
            Case "CDCompresion" : Return "C/D compresión"
            Case "CDTraccion" : Return "C/D tracción"
            Case "PuMax" : Return "Pu máximo"
            Case "TipoDado" : Return "Tipo de dado"
            Case Else : Return "C/D mínimo"
        End Select
    End Function

    Public Sub New(proyecto As Proyecto)
        Me.Proyecto = proyecto
        Me.Modelo = proyecto.Elementos.Pilotes
        RecargarDesdeModelo()

        CmdNuevoTipoPilote = New RelayCommand(AddressOf NuevoTipoPilote)
        CmdBorrarTipoPilote = New RelayCommand(AddressOf BorrarTipoPilote,
                                               Function() TipoPiloteSeleccionado IsNot Nothing)
        CmdNuevoTipoDado = New RelayCommand(AddressOf NuevoTipoDado)
        CmdBorrarTipoDado = New RelayCommand(AddressOf BorrarTipoDado,
                                             Function() TipoDadoSeleccionado IsNot Nothing)
        CmdIrInicio = New RelayCommand(Sub() VistaActiva = "Inicio")
        CmdIrPilotes = New RelayCommand(Sub() VistaActiva = "Pilotes")
        CmdIrDados = New RelayCommand(Sub() VistaActiva = "Dados")
        CmdIrApoyos = New RelayCommand(Sub() VistaActiva = "Apoyos")
        CmdIrResultados = New RelayCommand(Sub() VistaActiva = "Resultados")

        CmdAplicarGeneradorRectangular = New RelayCommand(AddressOf AplicarGeneradorRectangular,
                                                           Function() TipoDadoSeleccionado IsNot Nothing)
        CmdAplicarGeneradorCircular = New RelayCommand(AddressOf AplicarGeneradorCircular,
                                                        Function() TipoDadoSeleccionado IsNot Nothing)
        CmdAplicarGeneradorPentagonal = New RelayCommand(AddressOf AplicarGeneradorPentagonal,
                                                         Function() TipoDadoSeleccionado IsNot Nothing)
        CmdAplicarGeneradorHexagonal = New RelayCommand(AddressOf AplicarGeneradorHexagonal,
                                                        Function() TipoDadoSeleccionado IsNot Nothing)
        CmdRecalcular = New RelayCommand(Sub()
                                             PiloteService.EvaluarTodos(Modelo)
                                             RefrescarDadosVM()
                                             OnPropertyChanged(NameOf(NumApoyosCumplen))
                                             OnPropertyChanged(NameOf(PorcentajeCumplimiento))
                                         End Sub)

        CmdImportarETABS = New RelayCommand(AddressOf ImportarETABS)
        CmdImportarEjesETABS = New RelayCommand(AddressOf ImportarEjesETABS)
        CmdAsignarTipoDadoATodos = New RelayCommand(AddressOf AsignarTipoDadoATodos,
                                                    Function() TiposDado.Count > 0)
        CmdSeleccionarCombinaciones = New RelayCommand(AddressOf SeleccionarCombinaciones,
                                                        Function() NumCombinacionesTotal > 0)
        CmdExportarExcel = New RelayCommand(AddressOf ExportarExcel,
                                             Function() NumApoyos > 0)

        If Modelo.TiposPilote.Count = 0 Then SembrarDefaults()
    End Sub

    ' ── Navegación ────────────────────────────────────────────────

    Public Property VistaActiva As String
        Get
            Return _vistaActiva
        End Get
        Set(value As String)
            If SetProperty(_vistaActiva, value) Then
                OnPropertyChanged(NameOf(EsInicio))
                OnPropertyChanged(NameOf(EsPilotes))
                OnPropertyChanged(NameOf(EsDados))
                OnPropertyChanged(NameOf(EsApoyos))
                OnPropertyChanged(NameOf(EsResultados))
            End If
        End Set
    End Property

    Public ReadOnly Property EsInicio As Boolean
        Get
            Return VistaActiva = "Inicio"
        End Get
    End Property
    Public ReadOnly Property EsPilotes As Boolean
        Get
            Return VistaActiva = "Pilotes"
        End Get
    End Property
    Public ReadOnly Property EsDados As Boolean
        Get
            Return VistaActiva = "Dados"
        End Get
    End Property
    Public ReadOnly Property EsApoyos As Boolean
        Get
            Return VistaActiva = "Apoyos"
        End Get
    End Property
    Public ReadOnly Property EsResultados As Boolean
        Get
            Return VistaActiva = "Resultados"
        End Get
    End Property

    ' ── Selección ────────────────────────────────────────────────

    Public Property TipoPiloteSeleccionado As TipoPiloteVM
        Get
            Return _tipoPiloteSeleccionado
        End Get
        Set(value As TipoPiloteVM)
            If SetProperty(_tipoPiloteSeleccionado, value) Then
                CmdBorrarTipoPilote.RaiseCanExecuteChanged()
            End If
        End Set
    End Property

    Public Property TipoDadoSeleccionado As TipoDadoVM
        Get
            Return _tipoDadoSeleccionado
        End Get
        Set(value As TipoDadoVM)
            If SetProperty(_tipoDadoSeleccionado, value) Then
                CmdBorrarTipoDado.RaiseCanExecuteChanged()
                CmdAplicarGeneradorRectangular.RaiseCanExecuteChanged()
                CmdAplicarGeneradorCircular.RaiseCanExecuteChanged()
                CmdAplicarGeneradorPentagonal.RaiseCanExecuteChanged()
                CmdAplicarGeneradorHexagonal.RaiseCanExecuteChanged()
                OnPropertyChanged(NameOf(ErroresValidacion))
                OnPropertyChanged(NameOf(DadoEsValido))
            End If
        End Set
    End Property

    ''' <summary>
    ''' Apoyo seleccionado en la tabla de Apoyos. Al cambiar, se notifica la
    ''' propiedad CombinacionesDelSeleccionado para que el panel detalle refresque.
    ''' </summary>
    Public Property DadoSeleccionado As DadoVM
        Get
            Return _dadoSeleccionado
        End Get
        Set(value As DadoVM)
            If SetProperty(_dadoSeleccionado, value) Then
                OnPropertyChanged(NameOf(CombinacionesDelSeleccionado))
                OnPropertyChanged(NameOf(TieneApoyoSeleccionado))
                OnPropertyChanged(NameOf(TextoHeaderDetalle))
            End If
        End Set
    End Property

    Public ReadOnly Property TieneApoyoSeleccionado As Boolean
        Get
            Return _dadoSeleccionado IsNot Nothing
        End Get
    End Property

    Public ReadOnly Property TextoHeaderDetalle As String
        Get
            If _dadoSeleccionado Is Nothing Then
                Return "Combinaciones del apoyo — selecciona un apoyo para ver sus fuerzas de ETABS"
            End If
            Dim nTot = If(_dadoSeleccionado.Modelo.Combinaciones Is Nothing, 0,
                          _dadoSeleccionado.Modelo.Combinaciones.Count)
            Dim nDiseno = _dadoSeleccionado.Combinaciones.Count(Function(c) c.EstaEnSeleccionDiseno)
            Dim mostradas = If(_mostrarCombosFueraDeSeleccion, nTot, nDiseno)
            Return $"Combinaciones del apoyo {_dadoSeleccionado.JointLabel} " &
                   $"({_dadoSeleccionado.SourceType})  ·  mostrando {mostradas} de {nTot}  ·  " &
                   $"{nDiseno} en diseño"
        End Get
    End Property

    ''' <summary>
    ''' Cuando False (default) solo se listan las combinaciones que están en
    ''' la lista de diseño; con True se agregan también las fuera de selección
    ''' (en gris atenuado) para auditoría puntual. El usuario alterna con un
    ''' CheckBox en el header de la tabla detalle.
    ''' </summary>
    Public Property MostrarCombosFueraDeSeleccion As Boolean
        Get
            Return _mostrarCombosFueraDeSeleccion
        End Get
        Set(value As Boolean)
            If SetProperty(_mostrarCombosFueraDeSeleccion, value) Then
                OnPropertyChanged(NameOf(CombinacionesDelSeleccionado))
                OnPropertyChanged(NameOf(TextoHeaderDetalle))
            End If
        End Set
    End Property

    ''' <summary>
    ''' Filas para la tabla detalle. Por defecto se filtran las fuera de
    ''' selección (reducen ruido visual); con MostrarCombosFueraDeSeleccion=True
    ''' se listan todas y las no-seleccionadas aparecen en gris.
    ''' </summary>
    Public ReadOnly Property CombinacionesDelSeleccionado As IEnumerable(Of CombinacionConContexto)
        Get
            If _dadoSeleccionado Is Nothing Then
                Return Enumerable.Empty(Of CombinacionConContexto)()
            End If
            Dim todas = _dadoSeleccionado.Combinaciones
            If _mostrarCombosFueraDeSeleccion Then Return todas
            Return todas.Where(Function(c) c.EstaEnSeleccionDiseno).ToList()
        End Get
    End Property

    ' ── Resumen general (card de inicio) ─────────────────────────

    Public ReadOnly Property NumTiposPilote As Integer
        Get
            Return TiposPilote.Count
        End Get
    End Property
    Public ReadOnly Property NumTiposDado As Integer
        Get
            Return TiposDado.Count
        End Get
    End Property
    Public ReadOnly Property NumApoyos As Integer
        Get
            Return Modelo.Dados.Count
        End Get
    End Property
    Public ReadOnly Property NumApoyosCumplen As Integer
        Get
            ' VB.NET: List(Of T).Count es Property sin parámetros, no acepta
            ' lambda; se usa Where+Count() (feedback_vbnet_count_lambda).
            Return Modelo.Dados.Where(Function(d) EsCumplimientoOk(d)).Count()
        End Get
    End Property
    Public ReadOnly Property PorcentajeCumplimiento As Double
        Get
            If Modelo.Dados.Count = 0 Then Return 0
            Return 100.0 * NumApoyosCumplen / Modelo.Dados.Count
        End Get
    End Property

    ''' <summary>
    ''' Apoyos con al menos uno de los 4 chequeos activos por debajo del umbral
    ''' 0.90 (rojo). Incluye el chequeo estructural axial además del geotécnico.
    ''' </summary>
    Public ReadOnly Property NumNoCumplen As Integer
        Get
            Return Modelo.Dados.Where(Function(d) NoCumpleAlguno(d)).Count()
        End Get
    End Property

    Private Shared Function NoCumpleAlguno(d As Dado) As Boolean
        Dim u = PiloteService.UMBRAL_CD
        If d.CD_Compresion > 0 AndAlso d.CD_Compresion < u Then Return True
        If d.CD_Traccion > 0 AndAlso d.CD_Traccion < u Then Return True
        If d.CD_Compresion_Estructural > 0 AndAlso d.CD_Compresion_Estructural < u Then Return True
        If d.CD_Traccion_Estructural > 0 AndAlso d.CD_Traccion_Estructural < u Then Return True
        Return False
    End Function

    ''' <summary>Apoyos sin TipoDado asignado (no calculados).</summary>
    Public ReadOnly Property NumSinTipo As Integer
        Get
            Return Modelo.Dados.Where(Function(d) String.IsNullOrEmpty(d.NombreTipoDado)).Count()
        End Get
    End Property

    ''' <summary>Top 10 apoyos ordenados por menor C/D compresión (los más críticos).</summary>
    Public ReadOnly Property Top10Criticos As List(Of DadoVM)
        Get
            Return Dados.
                Where(Function(d) d.CD_Compresion > 0).
                OrderBy(Function(d) d.CD_Compresion).
                Take(10).
                ToList()
        End Get
    End Property

    Private Function EsCumplimientoOk(d As Dado) As Boolean
        Dim u = PiloteService.UMBRAL_CD
        If d.CD_Compresion > 0 AndAlso d.CD_Compresion < u Then Return False
        If d.CD_Traccion > 0 AndAlso d.CD_Traccion < u Then Return False
        If d.CD_Compresion_Estructural > 0 AndAlso d.CD_Compresion_Estructural < u Then Return False
        If d.CD_Traccion_Estructural > 0 AndAlso d.CD_Traccion_Estructural < u Then Return False
        Return True
    End Function

    ' ── Validación del dado seleccionado ─────────────────────────

    Public ReadOnly Property ErroresValidacion As List(Of String)
        Get
            If TipoDadoSeleccionado Is Nothing Then Return New List(Of String)
            Return PiloteService.ValidarGeometria(TipoDadoSeleccionado.Modelo, Modelo)
        End Get
    End Property

    Public ReadOnly Property DadoEsValido As Boolean
        Get
            Return ErroresValidacion.Count = 0
        End Get
    End Property

    ' ── Comandos ─────────────────────────────────────────────────

    Public ReadOnly Property CmdNuevoTipoPilote As RelayCommand
    Public ReadOnly Property CmdBorrarTipoPilote As RelayCommand
    Public ReadOnly Property CmdNuevoTipoDado As RelayCommand
    Public ReadOnly Property CmdBorrarTipoDado As RelayCommand
    Public ReadOnly Property CmdIrInicio As RelayCommand
    Public ReadOnly Property CmdIrPilotes As RelayCommand
    Public ReadOnly Property CmdIrDados As RelayCommand
    Public ReadOnly Property CmdIrApoyos As RelayCommand
    Public ReadOnly Property CmdIrResultados As RelayCommand
    Public ReadOnly Property CmdAplicarGeneradorRectangular As RelayCommand
    Public ReadOnly Property CmdAplicarGeneradorCircular As RelayCommand
    Public ReadOnly Property CmdAplicarGeneradorPentagonal As RelayCommand
    Public ReadOnly Property CmdAplicarGeneradorHexagonal As RelayCommand
    Public ReadOnly Property CmdRecalcular As RelayCommand
    Public ReadOnly Property CmdImportarETABS As RelayCommand
    Public ReadOnly Property CmdImportarEjesETABS As RelayCommand
    Public ReadOnly Property CmdAsignarTipoDadoATodos As RelayCommand
    Public ReadOnly Property CmdSeleccionarCombinaciones As RelayCommand
    Public ReadOnly Property CmdExportarExcel As RelayCommand

    ' ── Selección de combinaciones ──────────────────────────────

    Public ReadOnly Property NumCombinacionesTotal As Integer
        Get
            Return If(Modelo.Lista_Combinaciones Is Nothing, 0, Modelo.Lista_Combinaciones.Count)
        End Get
    End Property

    Public ReadOnly Property NumCombinacionesSeleccionadas As Integer
        Get
            Return If(Modelo.ListA_Combinaciones_Design Is Nothing, 0, Modelo.ListA_Combinaciones_Design.Count)
        End Get
    End Property

    ''' <summary>"X / Y combos" para el chip del toolbar.</summary>
    Public ReadOnly Property TextoCombinaciones As String
        Get
            If NumCombinacionesTotal = 0 Then Return "Sin combinaciones"
            Return $"{NumCombinacionesSeleccionadas} de {NumCombinacionesTotal} combinaciones"
        End Get
    End Property

    ''' <summary>True si todavía no se eligieron combos pero sí hay importadas.</summary>
    Public ReadOnly Property RequiereSeleccionDeCombos As Boolean
        Get
            Return NumCombinacionesTotal > 0 AndAlso NumCombinacionesSeleccionadas = 0
        End Get
    End Property

    ''' <summary>
    ''' Abre el dialog para elegir combinaciones; al confirmar, guarda la
    ''' selección en el modelo y re-evalúa todos los dados.
    ''' </summary>
    Private Sub SeleccionarCombinaciones()
        If Modelo.Lista_Combinaciones Is Nothing OrElse Modelo.Lista_Combinaciones.Count = 0 Then
            System.Windows.MessageBox.Show(
                "No hay combinaciones cargadas. Importa primero un modelo de ETABS.",
                "Combinaciones de diseño", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information)
            Return
        End If

        Dim dlg As New SeleccionarCombinacionesDialog(Modelo.Lista_Combinaciones,
                                                      Modelo.ListA_Combinaciones_Design)
        dlg.Owner = System.Windows.Application.Current?.Windows.OfType(Of System.Windows.Window) _
                     .FirstOrDefault(Function(w) w.IsActive)
        If dlg.ShowDialog() <> True Then Return

        Modelo.ListA_Combinaciones_Design.Clear()
        For Each nombre In dlg.SeleccionResultado
            Modelo.ListA_Combinaciones_Design.Add(nombre)
        Next

        PiloteService.EvaluarTodos(Modelo)
        RefrescarDadosVM()
        OnPropertyChanged(NameOf(NumCombinacionesSeleccionadas))
        OnPropertyChanged(NameOf(TextoCombinaciones))
        OnPropertyChanged(NameOf(RequiereSeleccionDeCombos))
        OnPropertyChanged(NameOf(NumApoyosCumplen))
        OnPropertyChanged(NameOf(NumNoCumplen))
        OnPropertyChanged(NameOf(NumSinTipo))
        OnPropertyChanged(NameOf(PorcentajeCumplimiento))
        OnPropertyChanged(NameOf(Top10Criticos))
        OnPropertyChanged(NameOf(CombinacionesDelSeleccionado))
        OnPropertyChanged(NameOf(TextoHeaderDetalle))
    End Sub

    ''' <summary>
    ''' Abre un OpenFileDialog, delega la lectura a PiloteImportService y
    ''' actualiza la UI. No bloquea el hilo — el OpenFileDialog es modal.
    ''' </summary>
    Private Sub ImportarETABS()
        Dim ofd As New Microsoft.Win32.OpenFileDialog() With {
            .Filter = "Excel ETABS (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
            .Title = "Importar apoyos desde ETABS"
        }
        If ofd.ShowDialog() <> True Then Return

        Dim r = PiloteImportService.Importar(Modelo, ofd.FileName, proyecto:=Proyecto)

        If Not r.Exito Then
            System.Windows.MessageBox.Show(
                "No se pudo importar:" & vbCrLf & vbCrLf & r.ErrorFatal,
                "Importación ETABS", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning)
            Return
        End If

        ' Paso crítico post-importación: elegir qué combinaciones participan
        ' del análisis. Sin esto, mezclar servicio y último distorsiona el
        ' Pu_max contra Q_admisible. Se abre el selector automáticamente.
        OnPropertyChanged(NameOf(NumCombinacionesTotal))
        OnPropertyChanged(NameOf(NumCombinacionesSeleccionadas))
        OnPropertyChanged(NameOf(TextoCombinaciones))
        OnPropertyChanged(NameOf(TieneEjesETABS))     ' La importación pudo traer grids nuevos.

        If NumCombinacionesTotal > 0 AndAlso NumCombinacionesSeleccionadas = 0 Then
            ' Pre-selecciona todo como punto de partida y abre el dialog para
            ' que el usuario confirme / recorte. Si el usuario cancela, dejamos
            ' la lista vacía y la UI muestra el callout "selecciona combinaciones".
            Dim dlg As New SeleccionarCombinacionesDialog(Modelo.Lista_Combinaciones,
                                                          Modelo.Lista_Combinaciones)
            dlg.Owner = System.Windows.Application.Current?.Windows.OfType(Of System.Windows.Window) _
                         .FirstOrDefault(Function(w) w.IsActive)
            If dlg.ShowDialog() = True Then
                Modelo.ListA_Combinaciones_Design.Clear()
                For Each nombre In dlg.SeleccionResultado
                    Modelo.ListA_Combinaciones_Design.Add(nombre)
                Next
            End If
            OnPropertyChanged(NameOf(NumCombinacionesSeleccionadas))
            OnPropertyChanged(NameOf(TextoCombinaciones))
            OnPropertyChanged(NameOf(RequiereSeleccionDeCombos))
        End If

        ' Si por cualquier motivo no quedó ningún TipoDado (proyecto viejo sin
        ' seed de defaults, catálogo borrado, etc.), creamos uno rectangular
        ' con 4 pilotes para que la auto-asignación de abajo tenga algo. Es
        ' la única forma de que el usuario vea números al abrir por primera
        ' vez — editable en la vista Dados.
        If TiposPilote.Count = 0 Then
            SembrarDefaults()
        ElseIf TiposDado.Count = 0 Then
            Dim nombrePilote = TiposPilote.First().Nombre
            Dim d As New TipoDado With {
                .Nombre = "D1",
                .Descripcion = "Dado default creado automáticamente al importar",
                .Espesor = 0.9,
                .Recubrimiento = 0.075,
                .Poligono = PiloteService.GenerarPoligonoRectangular(2.6, 2.6),
                .Pilotes = PiloteService.GenerarLayoutRectangular(2, 2, 1.8, nombrePilote)
            }
            Modelo.TiposDado.Add(d)
            TiposDado.Add(New TipoDadoVM(d))
            If TipoDadoSeleccionado Is Nothing Then TipoDadoSeleccionado = TiposDado.First()
            OnPropertyChanged(NameOf(NumTiposDado))
        End If

        ' Auto-asignar un TipoDado default a los apoyos sin tipo para que el
        ' usuario vea C/D inmediatamente después de importar.
        Dim nAutoAsignados As Integer = 0
        If TiposDado.Count > 0 Then
            Dim tdDefault As TipoDado = If(TipoDadoSeleccionado?.Modelo, TiposDado.First().Modelo)
            For Each d In Modelo.Dados
                If d.AsignacionManual Then Continue For
                If String.IsNullOrEmpty(d.NombreTipoDado) Then
                    d.NombreTipoDado = tdDefault.Nombre
                    nAutoAsignados += 1
                End If
            Next
            PiloteService.EvaluarTodos(Modelo)
        End If

        RefrescarDadosVM()
        OnPropertyChanged(NameOf(NumApoyos))
        OnPropertyChanged(NameOf(NumApoyosCumplen))
        OnPropertyChanged(NameOf(NumNoCumplen))
        OnPropertyChanged(NameOf(NumSinTipo))
        OnPropertyChanged(NameOf(PorcentajeCumplimiento))
        OnPropertyChanged(NameOf(Top10Criticos))

        Dim mensaje = $"✓ Importación correcta." & vbCrLf & vbCrLf &
                      $"Apoyos detectados: {r.Total}" & vbCrLf &
                      $"   · Frame (Joint Reactions):  {r.FrameCount}" & vbCrLf &
                      $"   · Pier (Pier Forces Bottom): {r.PierCount}" & vbCrLf &
                      $"   · Nuevos: {r.Nuevos}   · Actualizados: {r.Actualizados}" & vbCrLf &
                      $"Combinaciones de carga: {r.TotalCombinaciones}"

        If nAutoAsignados > 0 Then
            mensaje &= vbCrLf & vbCrLf &
                       $"✓ Se auto-asignó el dado '{TiposDado.First().Nombre}' a {nAutoAsignados} " &
                       "apoyos y se calcularon las C/D."
        End If

        If r.NotasInformativas IsNot Nothing AndAlso r.NotasInformativas.Count > 0 Then
            mensaje &= vbCrLf & vbCrLf & "Notas:" & vbCrLf &
                      String.Join(vbCrLf, r.NotasInformativas.Select(Function(n) "• " & n))
        End If

        mensaje &= vbCrLf & vbCrLf &
                   "► Siguiente paso: haz clic en 'Ver dashboard de resultados' en la " &
                   "barra superior, o entra a la pestaña 'Resultados' del rail izquierdo."

        System.Windows.MessageBox.Show(
            mensaje, "Importación ETABS", System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information)

        ' Saltamos directo a Resultados para que el usuario vea el dashboard
        ' calculado de inmediato. Si quiere revisar apoyo por apoyo, el rail
        ' tiene la pestaña Apoyos al lado.
        VistaActiva = "Resultados"
    End Sub

    ''' <summary>
    ''' Lee SOLO la hoja de Grid Lines de un Excel ETABS y la vuelca en
    ''' Proyecto.Elementos.Grids.GridLines. Pensado para usuarios que ya
    ''' importaron apoyos y necesitan agregar los ejes de referencia sin
    ''' re-ejecutar la importación completa (que reemplazaría combos).
    ''' </summary>
    Private Sub ImportarEjesETABS()
        Dim ofd As New Microsoft.Win32.OpenFileDialog() With {
            .Filter = "Excel ETABS (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
            .Title = "Importar ejes estructurales (Grid Lines) desde ETABS"
        }
        If ofd.ShowDialog() <> True Then Return
        If Proyecto Is Nothing Then Return

        Try
            Dim hojas = Funciones_00_Varias.ObtenerHojasExcel(ofd.FileName)
            Dim hGrids = Funciones_00_Varias.ResolverNombreHoja(hojas,
                                                                 "Grid Definitions - Grid Lines",
                                                                 "Grid Lines")
            If String.IsNullOrEmpty(hGrids) Then
                System.Windows.MessageBox.Show(
                    "El archivo no contiene la hoja 'Grid Definitions - Grid Lines' ni 'Grid Lines'." &
                    vbCrLf & vbCrLf &
                    "Verifica que al exportar desde ETABS hayas incluido la tabla de ejes.",
                    "Importar ejes", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning)
                Return
            End If

            Dim tabla = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, hGrids)
            Dim ejes = Funciones_00_Varias.DataTableToGridLines(tabla)
            If ejes Is Nothing OrElse ejes.Count = 0 Then
                System.Windows.MessageBox.Show(
                    "No se detectaron ejes en la hoja '" & hGrids & "'.",
                    "Importar ejes", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning)
                Return
            End If

            Proyecto.Elementos.Grids.GridLines = ejes
            OnPropertyChanged(NameOf(TieneEjesETABS))

            System.Windows.MessageBox.Show(
                $"Se importaron {ejes.Count} ejes estructurales." & vbCrLf & vbCrLf &
                "Actívalos con el toggle 'Ejes ETABS' sobre la planta.",
                "Importar ejes", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information)
        Catch ex As Exception
            Logger.Error(ex, "PiloteOverviewVM.ImportarEjesETABS", ofd.FileName)
            System.Windows.MessageBox.Show(
                "No se pudieron importar los ejes:" & vbCrLf & vbCrLf & ex.Message,
                "Importar ejes", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Abre SaveFileDialog y delega la exportación a PiloteReporteExcelService.
    ''' Muestra un resumen al usuario y, si fue exitoso, ofrece abrir el archivo
    ''' con Excel inmediatamente. Toda la lógica de armado del libro vive en el
    ''' servicio — aquí sólo UI y manejo de errores.
    ''' </summary>
    Private Sub ExportarExcel()
        Dim nombreDefault As String
        If Proyecto IsNot Nothing AndAlso Proyecto.Info IsNot Nothing AndAlso
           Not String.IsNullOrWhiteSpace(Proyecto.Info.Nombre) Then
            nombreDefault = $"ARCO_Pilotes_{SanearNombre(Proyecto.Info.Nombre)}_{DateTime.Now:yyyyMMdd}"
        Else
            nombreDefault = $"ARCO_Pilotes_{DateTime.Now:yyyyMMdd}"
        End If

        Dim sfd As New Microsoft.Win32.SaveFileDialog() With {
            .Filter = "Libro Excel (*.xlsx)|*.xlsx",
            .FileName = nombreDefault,
            .Title = "Exportar reporte de pilotes a Excel"
        }
        If sfd.ShowDialog() <> True Then Return

        Dim r = PiloteReporteExcelService.Exportar(Modelo, Proyecto, sfd.FileName)
        If Not r.Exito Then
            System.Windows.MessageBox.Show(
                "No se pudo generar el reporte:" & vbCrLf & vbCrLf & r.Mensaje,
                "Exportar a Excel", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error)
            Return
        End If

        Dim respuesta = System.Windows.MessageBox.Show(
            $"{r.Mensaje}" & vbCrLf & vbCrLf &
            $"Ruta: {r.Ruta}" & vbCrLf & vbCrLf &
            "¿Quieres abrir el archivo ahora?",
            "Exportar a Excel", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Information)
        If respuesta = System.Windows.MessageBoxResult.Yes Then
            Try
                Process.Start(New ProcessStartInfo(r.Ruta) With {.UseShellExecute = True})
            Catch ex As Exception
                Logger.Warning("PiloteOverviewVM.ExportarExcel",
                               "No se pudo abrir el archivo: " & ex.Message)
            End Try
        End If
    End Sub

    ''' <summary>Elimina caracteres inválidos para un nombre de archivo.</summary>
    Private Shared Function SanearNombre(nombre As String) As String
        If String.IsNullOrEmpty(nombre) Then Return ""
        For Each ch In IO.Path.GetInvalidFileNameChars()
            nombre = nombre.Replace(ch, "_"c)
        Next
        Return nombre.Trim()
    End Function

    ''' <summary>
    ''' Asigna el TipoDado seleccionado (o el primero disponible) a TODOS
    ''' los apoyos que aún no tengan uno, respetando AsignacionManual=True.
    ''' Útil para arrancar: tras importar se asigna un dado default y luego
    ''' se refinan los que lo ameriten.
    ''' </summary>
    Private Sub AsignarTipoDadoATodos()
        If TiposDado.Count = 0 Then Return
        Dim td = TipoDadoSeleccionado
        If td Is Nothing Then td = TiposDado.First()

        Dim n = 0
        For Each d In Modelo.Dados
            If d.AsignacionManual Then Continue For
            If String.IsNullOrEmpty(d.NombreTipoDado) Then
                d.NombreTipoDado = td.Nombre
                n += 1
            End If
        Next

        PiloteService.EvaluarTodos(Modelo)
        RefrescarDadosVM()
        OnPropertyChanged(NameOf(NumApoyosCumplen))
        OnPropertyChanged(NameOf(NumNoCumplen))
        OnPropertyChanged(NameOf(NumSinTipo))
        OnPropertyChanged(NameOf(PorcentajeCumplimiento))
        OnPropertyChanged(NameOf(Top10Criticos))

        System.Windows.MessageBox.Show(
            $"Se asignaron {n} apoyos sin tipo al dado '{td.Nombre}'.",
            "Asignación masiva", System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information)
    End Sub

    Private Sub RefrescarDadosVM()
        Dados.Clear()
        For Each d In Modelo.Dados
            Dados.Add(New DadoVM(d, Me))
        Next
    End Sub

    Private Sub NuevoTipoPilote()
        Dim nombre = NombreUnico("PP", Modelo.TiposPilote.Select(Function(x) x.Nombre))
        Dim t As New TipoPilote With {
            .Nombre = nombre,
            .Tipo = eTipoPilote.Perforado,
            .Diametro = 0.6,
            .Longitud = 12.0,
            .Qc_Admisible = 1500,
            .Qt_Admisible = 0
        }
        Modelo.TiposPilote.Add(t)
        Dim vm As New TipoPiloteVM(t)
        TiposPilote.Add(vm)
        TipoPiloteSeleccionado = vm
        OnPropertyChanged(NameOf(NumTiposPilote))
    End Sub

    Private Sub BorrarTipoPilote()
        If TipoPiloteSeleccionado Is Nothing Then Return
        Modelo.TiposPilote.Remove(TipoPiloteSeleccionado.Modelo)
        TiposPilote.Remove(TipoPiloteSeleccionado)
        TipoPiloteSeleccionado = TiposPilote.FirstOrDefault()
        OnPropertyChanged(NameOf(NumTiposPilote))
    End Sub

    Private Sub NuevoTipoDado()
        Dim nombre = NombreUnico("D", Modelo.TiposDado.Select(Function(x) x.Nombre))
        Dim t As New TipoDado With {
            .Nombre = nombre,
            .Espesor = 0.8,
            .Recubrimiento = 0.075,
            .Poligono = PiloteService.GenerarPoligonoRectangular(2.5, 2.5)
        }
        Dim nombrePilote = TiposPilote.FirstOrDefault()?.Nombre
        If nombrePilote IsNot Nothing Then
            t.Pilotes = PiloteService.GenerarLayoutRectangular(2, 2, 1.8, nombrePilote)
        End If
        Modelo.TiposDado.Add(t)
        Dim vm As New TipoDadoVM(t)
        TiposDado.Add(vm)
        TipoDadoSeleccionado = vm
        OnPropertyChanged(NameOf(NumTiposDado))
    End Sub

    Private Sub BorrarTipoDado()
        If TipoDadoSeleccionado Is Nothing Then Return
        Modelo.TiposDado.Remove(TipoDadoSeleccionado.Modelo)
        TiposDado.Remove(TipoDadoSeleccionado)
        TipoDadoSeleccionado = TiposDado.FirstOrDefault()
        OnPropertyChanged(NameOf(NumTiposDado))
    End Sub

    Private Sub AplicarGeneradorRectangular()
        If TipoDadoSeleccionado Is Nothing Then Return
        Dim nombrePilote = TiposPilote.FirstOrDefault()?.Nombre
        If nombrePilote Is Nothing Then Return
        TipoDadoSeleccionado.ReemplazarGeometria(
            PiloteService.GenerarPoligonoRectangular(3.0, 3.0),
            PiloteService.GenerarLayoutRectangular(3, 3, 1.0, nombrePilote))
        OnPropertyChanged(NameOf(ErroresValidacion))
        OnPropertyChanged(NameOf(DadoEsValido))
    End Sub

    Private Sub AplicarGeneradorCircular()
        If TipoDadoSeleccionado Is Nothing Then Return
        Dim nombrePilote = TiposPilote.FirstOrDefault()?.Nombre
        If nombrePilote Is Nothing Then Return
        TipoDadoSeleccionado.ReemplazarGeometria(
            PiloteService.GenerarPoligonoRegular(32, 1.6),
            PiloteService.GenerarLayoutCircular(6, 1.0, nombrePilote))
        OnPropertyChanged(NameOf(ErroresValidacion))
        OnPropertyChanged(NameOf(DadoEsValido))
    End Sub

    Private Sub AplicarGeneradorPentagonal()
        If TipoDadoSeleccionado Is Nothing Then Return
        Dim nombrePilote = TiposPilote.FirstOrDefault()?.Nombre
        If nombrePilote Is Nothing Then Return
        TipoDadoSeleccionado.ReemplazarGeometria(
            PiloteService.GenerarPoligonoRegular(5, 1.75),
            PiloteService.GenerarLayoutCircular(5, 1.0, nombrePilote))
        OnPropertyChanged(NameOf(ErroresValidacion))
        OnPropertyChanged(NameOf(DadoEsValido))
    End Sub

    Private Sub AplicarGeneradorHexagonal()
        If TipoDadoSeleccionado Is Nothing Then Return
        Dim nombrePilote = TiposPilote.FirstOrDefault()?.Nombre
        If nombrePilote Is Nothing Then Return
        Dim pilotes = PiloteService.GenerarLayoutCircular(6, 1.0, nombrePilote)
        pilotes.Insert(0, New PilotePosicion(0, 0, nombrePilote))
        TipoDadoSeleccionado.ReemplazarGeometria(
            PiloteService.GenerarPoligonoRegular(6, 1.75), pilotes)
        OnPropertyChanged(NameOf(ErroresValidacion))
        OnPropertyChanged(NameOf(DadoEsValido))
    End Sub

    ''' <summary>
    ''' Puente público para que el code-behind del Window pueda re-notificar
    ''' propiedades derivadas (p.ej. ErroresValidacion) tras mutar colecciones.
    ''' </summary>
    Public Sub OnPropertyChangedProxy(propertyName As String)
        OnPropertyChanged(propertyName)
        If propertyName = NameOf(ErroresValidacion) Then
            OnPropertyChanged(NameOf(DadoEsValido))
        End If
    End Sub

    ' ── Utilidades ───────────────────────────────────────────────

    Private Sub RecargarDesdeModelo()
        TiposPilote.Clear()
        For Each t In Modelo.TiposPilote
            TiposPilote.Add(New TipoPiloteVM(t))
        Next
        TiposDado.Clear()
        For Each t In Modelo.TiposDado
            TiposDado.Add(New TipoDadoVM(t))
        Next

        ' Pre-seleccionar el primer elemento para que al abrir un proyecto
        ' con pilotes guardados el panel de edición tenga DataContext. Sin
        ' esto los TextBox a la derecha quedan sin binding y parecen
        ' bloqueados — el usuario tiene que acordarse de hacer clic en la
        ' lista, cosa no obvia cuando el panel ya muestra labels.
        If TiposPilote.Count > 0 AndAlso TipoPiloteSeleccionado Is Nothing Then
            TipoPiloteSeleccionado = TiposPilote.First()
        End If
        If TiposDado.Count > 0 AndAlso TipoDadoSeleccionado Is Nothing Then
            TipoDadoSeleccionado = TiposDado.First()
        End If

        RefrescarDadosVM()
    End Sub

    Private Sub SembrarDefaults()
        ' Precarga un catálogo típico para que al abrir por primera vez el
        ' módulo haya algo con lo que jugar sin pelearse con formularios vacíos.
        Dim pp As New TipoPilote With {
            .Nombre = "PP60",
            .Tipo = eTipoPilote.Perforado,
            .Diametro = 0.6, .Longitud = 15,
            .Qc_Admisible = 1500, .Qt_Admisible = 0
        }
        Dim mp As New TipoPilote With {
            .Nombre = "MP200",
            .Tipo = eTipoPilote.Micropilote,
            .Diametro = 0.2, .Longitud = 20,
            .Qc_Admisible = 400, .Qt_Admisible = 300
        }
        Modelo.TiposPilote.Add(pp)
        Modelo.TiposPilote.Add(mp)
        TiposPilote.Add(New TipoPiloteVM(pp))
        TiposPilote.Add(New TipoPiloteVM(mp))

        Dim d As New TipoDado With {
            .Nombre = "D1",
            .Descripcion = "Dado típico 2x2 PP60",
            .Espesor = 0.9,
            .Recubrimiento = 0.075,
            .Poligono = PiloteService.GenerarPoligonoRectangular(2.6, 2.6),
            .Pilotes = PiloteService.GenerarLayoutRectangular(2, 2, 1.8, pp.Nombre)
        }
        Modelo.TiposDado.Add(d)
        TiposDado.Add(New TipoDadoVM(d))

        TipoPiloteSeleccionado = TiposPilote.First()
        TipoDadoSeleccionado = TiposDado.First()
        OnPropertyChanged(NameOf(NumTiposPilote))
        OnPropertyChanged(NameOf(NumTiposDado))
    End Sub

    Private Function NombreUnico(prefijo As String, existentes As IEnumerable(Of String)) As String
        Dim set1 = New HashSet(Of String)(existentes)
        Dim i = 1
        Do
            Dim candidato = $"{prefijo}{i}"
            If Not set1.Contains(candidato) Then Return candidato
            i += 1
        Loop
    End Function

End Class
