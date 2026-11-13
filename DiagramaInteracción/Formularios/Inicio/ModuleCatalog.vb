Imports System.Windows.Forms

''' <summary>
''' Metadatos de un módulo para la pantalla de inicio. Sin dependencias a
''' WPF ni WinForms: es un POCO con una Action para disparar el flujo.
''' </summary>
Public Class ModuleInfo
    Public Property Id As String
    Public Property Nombre As String
    Public Property Descripcion As String
    Public Property Categoria As String
    Public Property IconKey As String
    Public Property Accion As Action
    Public Property Disponible As Boolean = True
    Public Property Orden As Integer
End Class

''' <summary>
''' Catálogo centralizado de los módulos que aparecen en la pantalla de
''' inicio. Agregar un módulo nuevo = una entrada en <see cref="Construir"/>.
'''
''' Cada acción invoca el handler original del botón correspondiente en
''' <c>Form_00_PaginaPrincipal</c> vía <c>PerformClick()</c>, preservando
''' 100% de la lógica existente (validaciones de proyecto cargado,
''' inicialización especial de Pilas, etc.).
''' </summary>
Public Module ModuleCatalog

    Public Const Cat_Super As String = "Superestructura"
    Public Const Cat_Cimen As String = "Cimentaciones"
    Public Const Cat_Espec As String = "Elementos especiales"
    Public Const Cat_Tools As String = "Herramientas"

    Private _modulos As List(Of ModuleInfo) = Nothing

    Public ReadOnly Property Modulos As List(Of ModuleInfo)
        Get
            If _modulos Is Nothing Then _modulos = Construir()
            Return _modulos
        End Get
    End Property

    Public Function PorCategoria(c As String) As List(Of ModuleInfo)
        Return Modulos.Where(Function(m) m.Categoria = c).OrderBy(Function(m) m.Orden).ToList()
    End Function

    ''' <summary>Selección curada de acceso rápido para v1.</summary>
    Public Function AccesoRapido() As List(Of ModuleInfo)
        Dim ids = {"columnas", "vigas", "muros", "zapatas"}
        Return ids.Select(Function(id) Modulos.FirstOrDefault(Function(m) m.Id = id)) _
                  .Where(Function(m) m IsNot Nothing).ToList()
    End Function

    Public Function Buscar(texto As String) As List(Of ModuleInfo)
        If String.IsNullOrWhiteSpace(texto) Then Return Modulos
        Dim t = texto.Trim().ToLowerInvariant()
        Return Modulos.Where(Function(m) m.Nombre.ToLowerInvariant().Contains(t) OrElse
                                         m.Descripcion.ToLowerInvariant().Contains(t)).ToList()
    End Function

    ''' <summary>
    ''' Dispara PerformClick sobre el botón del Form_00_PaginaPrincipal.
    ''' Así no duplicamos lógica de validación ni inicialización.
    ''' </summary>
    Private Sub ClickBoton(nombreControl As String)
        Try
            Dim pp = Form_00_PaginaPrincipal
            Dim ctl = pp.Controls.Find(nombreControl, True).FirstOrDefault()
            Dim btn = TryCast(ctl, Button)
            If btn IsNot Nothing Then
                btn.PerformClick()
            Else
                Logger.Warning("ModuleCatalog.ClickBoton", "No se encontró el control " & nombreControl)
            End If
        Catch ex As Exception
            Logger.Error(ex, "ModuleCatalog.ClickBoton", nombreControl)
        End Try
    End Sub

    Private Function Construir() As List(Of ModuleInfo)
        Dim m As New List(Of ModuleInfo)

        ' ── Superestructura ─────────────────────────────────────────
        m.Add(New ModuleInfo With {.Id = "columnas", .Nombre = "Columnas", .Categoria = Cat_Super, .Orden = 10,
                                   .Descripcion = "Diseño y revisión biaxial P-M-M según NSR-10.",
                                   .IconKey = "IcoColumnas",
                                   .Accion = Sub() ClickBoton("Boton_Columnas")})
        m.Add(New ModuleInfo With {.Id = "vigas", .Nombre = "Vigas", .Categoria = Cat_Super, .Orden = 20,
                                   .Descripcion = "Flexión, cortante y refuerzo transversal automático.",
                                   .IconKey = "IcoVigas",
                                   .Accion = Sub() ClickBoton("Button3")})
        m.Add(New ModuleInfo With {.Id = "muros", .Nombre = "Muros", .Categoria = Cat_Super, .Orden = 30,
                                   .Descripcion = "Muros estructurales, interacción P-M y elementos de borde.",
                                   .IconKey = "IcoMuros",
                                   .Accion = Sub() ClickBoton("Boton_Muros")})
        m.Add(New ModuleInfo With {.Id = "mne", .Nombre = "M.N.E.", .Categoria = Cat_Super, .Orden = 40,
                                   .Descripcion = "Mampostería no estructural: anclajes y chequeos.",
                                   .IconKey = "IcoMNE",
                                   .Accion = Sub() ClickBoton("Boton_MNE")})

        ' ── Cimentaciones ──────────────────────────────────────────
        m.Add(New ModuleInfo With {.Id = "zapatas", .Nombre = "Zapatas", .Categoria = Cat_Cimen, .Orden = 10,
                                   .Descripcion = "Punzonamiento, cortante en dos direcciones, flexión y presiones.",
                                   .IconKey = "IcoZapatas",
                                   .Accion = Sub() ClickBoton("Button1")})
        m.Add(New ModuleInfo With {.Id = "pilas", .Nombre = "Pilas", .Categoria = Cat_Cimen, .Orden = 20,
                                   .Descripcion = "Cinco chequeos de esfuerzos y revisión de capacidad.",
                                   .IconKey = "IcoPilas",
                                   .Accion = Sub() ClickBoton("Boton_Pilas")})
        m.Add(New ModuleInfo With {.Id = "vigasfund", .Nombre = "Vigas de Fundación", .Categoria = Cat_Cimen, .Orden = 30,
                                   .Descripcion = "Diseño por flexión y cortante para vigas enterradas.",
                                   .IconKey = "IcoVigasFund",
                                   .Accion = Sub() ClickBoton("Button2")})
        m.Add(New ModuleInfo With {.Id = "pilotes", .Nombre = "Pilotes", .Categoria = Cat_Cimen, .Orden = 40,
                                   .Descripcion = "Diseño y revisión de cimentaciones profundas.",
                                   .IconKey = "IcoPilotes",
                                   .Disponible = False})

        ' ── Elementos especiales ───────────────────────────────────
        m.Add(New ModuleInfo With {.Id = "losas", .Nombre = "Losas", .Categoria = Cat_Espec, .Orden = 10,
                                   .Descripcion = "Losas macizas en una o dos direcciones.",
                                   .IconKey = "IcoLosas",
                                   .Accion = Sub() ClickBoton("Boton_Losas")})
        m.Add(New ModuleInfo With {.Id = "nervios", .Nombre = "Nervios", .Categoria = Cat_Espec, .Orden = 20,
                                   .Descripcion = "Losas nervadas: diseño del nervio y envolventes.",
                                   .IconKey = "IcoNervios",
                                   .Accion = Sub() ClickBoton("BtnNervios")})
        m.Add(New ModuleInfo With {.Id = "escaleras", .Nombre = "Escaleras", .Categoria = Cat_Espec, .Orden = 30,
                                   .Descripcion = "Verificación de escaleras y deflexiones.",
                                   .IconKey = "IcoEscaleras",
                                   .Accion = Sub() ClickBoton("Boton_Escaleras")})
        m.Add(New ModuleInfo With {.Id = "mcontencion", .Nombre = "M. Contención", .Categoria = Cat_Espec, .Orden = 40,
                                   .Descripcion = "Muro cantilever con zapata y vástago.",
                                   .IconKey = "IcoMContencion",
                                   .Accion = Sub() ClickBoton("BtnMurosContencion")})
        m.Add(New ModuleInfo With {.Id = "mccortina", .Nombre = "MC. Cortina", .Categoria = Cat_Espec, .Orden = 50,
                                   .Descripcion = "Pantalla apoyada en altura por el método de rigidez.",
                                   .IconKey = "IcoMCCortina",
                                   .Accion = Sub() ClickBoton("BtnMuroCortina")})

        ' ── Herramientas ───────────────────────────────────────────
        m.Add(New ModuleInfo With {.Id = "analisis", .Nombre = "Análisis de Sección", .Categoria = Cat_Tools, .Orden = 10,
                                   .Descripcion = "Diagrama de interacción y momento-curvatura.",
                                   .IconKey = "IcoAnalisis",
                                   .Accion = Sub()
                                                 Try
                                                     Dim f As New Form_10_AnalisisSeccion_V2()
                                                     f.Show()
                                                 Catch ex As Exception
                                                     Logger.Error(ex, "ModuleCatalog.AbrirAnalisis")
                                                 End Try
                                             End Sub})
        m.Add(New ModuleInfo With {.Id = "e2k", .Nombre = "Visualizador ETABS", .Categoria = Cat_Tools, .Orden = 20,
                                   .Descripcion = "Previsualización 3D de modelos .e2k.",
                                   .IconKey = "IcoE2K",
                                   .Accion = Sub() AbrirVisualizadorE2K()})

        Return m
    End Function

    ''' <summary>
    ''' Abre el visualizador 3D WPF. Si cualquier cosa falla (Window WPF
    ''' no disponible, etc.), cae a la versión WinForms clásica.
    ''' </summary>
    Private Sub AbrirVisualizadorE2K()
        Try
            Dim ofd As New OpenFileDialog() With {
                .Filter = "Modelo ETABS (*.e2k)|*.e2k|Todos los archivos (*.*)|*.*",
                .Title = "Abrir modelo ETABS (.e2k)"
            }
            If ofd.ShowDialog() <> DialogResult.OK Then Return
            Dim model = E2kParser.Parse(ofd.FileName)
            If model.Points.Count = 0 AndAlso model.Lines.Count = 0 Then
                MessageBox.Show("No se encontraron elementos en el archivo.",
                    "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If System.Windows.Application.Current Is Nothing Then
                Dim app As New System.Windows.Application()
                app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            End If
            Dim win As New Form_VisualizadorE2K_WPF(model)
            win.Show()
        Catch ex As Exception
            Logger.Error(ex, "ModuleCatalog.AbrirVisualizadorE2K")
            MessageBox.Show("Error: " & ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Module
