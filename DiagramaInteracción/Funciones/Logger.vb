Imports System.IO

''' ===========================================================================
''' MÓDULO: Logger
''' PROPÓSITO: Registro centralizado de eventos y errores para toda la aplicación ARCO_2.
'''
''' PROBLEMA QUE RESUELVE:
'''   Antes de este módulo, los bloques Catch en los formularios estaban vacíos.
'''   Cuando algo fallaba (error al guardar el proyecto, error al leer tabla ETABS,
'''   fallo en un cálculo), la aplicación continuaba silenciosamente sin ningún aviso
'''   ni trazabilidad. Era imposible saber QUÉ falló, DÓNDE falló y en qué condiciones.
'''
''' CÓMO FUNCIONA:
'''   Cada llamada al Logger genera una entrada con:
'''     - Timestamp exacto (fecha, hora, milisegundos)
'''     - Nivel de severidad (INFO / WARN / ERROR / CRIT)
'''     - Contexto (formulario + método donde ocurrió el evento)
'''     - Mensaje descriptivo
'''     - Para errores: tipo de excepción, mensaje y stack trace completo
'''
'''   La salida va a DOS destinos simultáneamente:
'''     1. Debug.Print  → Ventana "Output" de Visual Studio (visible durante debugging en el IDE)
'''     2. Archivo .log → [CarpetaEjecutable]\Logs\ARCO_yyyy-MM-dd.log
'''                       (un archivo por día, se crea automáticamente)
'''
''' NIVELES DE SEVERIDAD:
'''   INFO  → Flujo normal: inicio/fin de operaciones, datos cargados correctamente.
'''           No indica problema. Útil para trazar la secuencia de eventos.
'''
'''   WARN  → Situación inusual que no impide la operación pero merece revisión.
'''           Ejemplos: dato vacío reemplazado por defecto, valor fuera del rango esperado,
'''           muro sin fuerzas asignadas, combinación no reconocida.
'''
'''   ERROR → Excepción capturada en un Catch. La operación falló pero la app puede continuar.
'''           Incluye tipo de excepción, mensaje y stack trace completo.
'''           Ejemplos: error al leer tabla ETABS, fallo al calcular una sección.
'''
'''   CRIT  → Error grave que puede dejar datos en estado inconsistente.
'''           Además de registrar, muestra un MessageBox al usuario para que sepa que algo
'''           salió mal y no asuma que la operación se completó.
'''           Ejemplos: error al guardar/abrir el proyecto, fallo en cálculo estructural crítico.
'''
''' USO EN CÓDIGO:
'''   ' En cualquier formulario o función:
'''
'''   ' Evento informativo (inicio de cálculo, dato cargado)
'''   Logger.Info("Form_02_PagColumnas.Button2_Click", "Iniciando cálculo de columnas Frame+Pier")
'''
'''   ' Advertencia (dato inesperado pero no fatal)
'''   Logger.Warning("Form_06_PagMuros.ImportarFuerzas", "Muro sin fuerzas asignadas: " & muro.Name)
'''
'''   ' Error en un Catch (reemplaza los Catch vacíos)
'''   Catch ex As Exception
'''       Logger.Error(ex, "Form_02_PagColumnas.Button2_Click")
'''   End Try
'''
'''   ' Error con contexto adicional de qué se estaba haciendo
'''   Catch ex As Exception
'''       Logger.Error(ex, "Form_06_00_PagInfoMuros.Combo_Elementos_SelectedIndexChanged",
'''                    "Error al cargar secciones del muro: " & Combo_Elementos.Text)
'''   End Try
'''
'''   ' Error crítico que debe notificar al usuario
'''   Catch ex As Exception
'''       Logger.Critical(ex, "Form_00_PaginaPrincipal.GuardarComo",
'''                       "No se pudo guardar el proyecto. Verifique permisos de escritura.")
'''   End Try
'''
''' LECTURA DE LOGS:
'''   Los archivos .log se guardan en:  [DirectorioEjecutable]\Logs\
'''   Abrir con cualquier editor de texto. Cada error tiene un separador ─── para ubicarlo.
'''   El nombre incluye la fecha: ARCO_2026-07-06.log
''' ===========================================================================
Public Module Logger

    ' ── Constantes de configuración ───────────────────────────────────────────

    ''' <summary>Nombre de la subcarpeta donde se guardan los archivos de log.</summary>
    Private Const CARPETA_LOGS As String = "Logs"

    ''' <summary>Prefijo del nombre de archivo de log (se agrega _yyyy-MM-dd.log).</summary>
    Private Const NOMBRE_BASE As String = "ARCO"

    ''' <summary>Ancho de la línea separadora entre entradas de error.</summary>
    Private Const ANCHO_SEPARADOR As Integer = 72

    ' ── Sincronización thread-safe ─────────────────────────────────────────────
    ' Necesario porque los formularios pueden generar logs desde distintos contextos.
    Private ReadOnly _lockArchivo As New Object()

    ' ── Propiedades de acceso a rutas ─────────────────────────────────────────

    ''' <summary>
    ''' Ruta completa de la carpeta de logs. Se crea automáticamente la primera vez que se usa.
    ''' </summary>
    Public ReadOnly Property RutaCarpetaLogs As String
        Get
            Return Path.Combine(Application.StartupPath, CARPETA_LOGS)
        End Get
    End Property

    ''' <summary>
    ''' Ruta del archivo de log del día actual. Un archivo nuevo por día.
    ''' Ejemplo: C:\...\bin\Debug\Logs\ARCO_2026-07-06.log
    ''' </summary>
    Public ReadOnly Property RutaArchivoLog As String
        Get
            Return Path.Combine(RutaCarpetaLogs,
                                $"{NOMBRE_BASE}_{DateTime.Now:yyyy-MM-dd}.log")
        End Get
    End Property

    ' ═════════════════════════════════════════════════════════════════════════
    '  API PÚBLICA — Estos son los métodos que se usan en toda la aplicación
    ' ═════════════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Registra un evento informativo del flujo normal de la aplicación.
    ''' No indica ningún problema. Usar para marcar hitos importantes:
    ''' inicio de cálculo, importación completada, número de elementos procesados, etc.
    ''' </summary>
    ''' <param name="contexto">
    ''' Identificador de dónde se genera el evento.
    ''' Convención: "NombreFormulario.NombreMetodo"
    ''' Ejemplo: "Form_02_PagColumnas.Button2_Click"
    ''' </param>
    ''' <param name="mensaje">Descripción del evento.</param>
    Public Sub Info(ByVal contexto As String, ByVal mensaje As String)
        EscribirEntrada("INFO ", contexto, mensaje, Nothing)
    End Sub

    ''' <summary>
    ''' Registra una situación inusual que no impide la operación pero merece atención.
    ''' Usar cuando:
    '''   - Un campo llega vacío y se aplica un valor por defecto
    '''   - Un valor numérico está fuera del rango esperado (columna con cuantía > 8%)
    '''   - Una combinación de carga no se reconoce en la lista
    '''   - Un elemento no tiene datos de fuerza asignados
    ''' </summary>
    ''' <param name="contexto">Identificador de dónde ocurre la advertencia.</param>
    ''' <param name="mensaje">Descripción de la situación y qué dato es inusual.</param>
    Public Sub Warning(ByVal contexto As String, ByVal mensaje As String)
        EscribirEntrada("WARN ", contexto, mensaje, Nothing)
    End Sub

    ''' <summary>
    ''' Registra una excepción capturada en un bloque Catch.
    ''' Este método REEMPLAZA los bloques Catch vacíos de toda la aplicación.
    ''' Registra tipo de excepción, mensaje de error y stack trace completo.
    '''
    ''' USO TÍPICO:
    '''   Catch ex As Exception
    '''       Logger.Error(ex, "Form_02_PagColumnas.Button2_Click")
    '''   End Try
    '''
    ''' USO CON CONTEXTO ADICIONAL (recomendado cuando hay variables relevantes):
    '''   Catch ex As Exception
    '''       Logger.Error(ex, "Form_06_PagMuros.ImportarFuerzas",
    '''                    "Fila problemática: " & i & " — Combinación: " & nombreCombo)
    '''   End Try
    ''' </summary>
    ''' <param name="ex">La excepción capturada (objeto 'ex' del bloque Catch).</param>
    ''' <param name="contexto">Formulario y método donde ocurrió el error.</param>
    ''' <param name="detalleExtra">
    ''' Información adicional opcional: variables en juego, dato que se procesaba,
    ''' nombre del elemento, número de fila, etc. Ayuda mucho al diagnosticar.
    ''' </param>
    Public Sub [Error](ByVal ex As Exception,
                       ByVal contexto As String,
                       Optional ByVal detalleExtra As String = "")
        EscribirEntrada("ERROR", contexto, detalleExtra, ex)
    End Sub

    ''' <summary>
    ''' Registra un fallo grave que puede dejar el proyecto en estado inconsistente
    ''' y NOTIFICA AL USUARIO con un MessageBox.
    '''
    ''' Diferencia con Error(): Error() es silencioso (solo al log). Critical() además
    ''' muestra un diálogo al usuario para que sepa que la operación no se completó.
    '''
    ''' Usar en:
    '''   - Error al guardar o abrir el archivo de proyecto (.esm)
    '''   - Error al importar datos de ETABS cuando la tabla queda sin datos
    '''   - Fallo en un cálculo estructural que deja resultados en cero o inconsistentes
    ''' </summary>
    ''' <param name="ex">La excepción capturada.</param>
    ''' <param name="contexto">Formulario y método donde ocurrió el error.</param>
    ''' <param name="mensajeUsuario">
    ''' Mensaje claro para mostrar al usuario. Debe explicar QUÉ falló
    ''' y sugerir qué hacer (verificar permisos, revisar el archivo, etc.).
    ''' </param>
    Public Sub Critical(ByVal ex As Exception,
                        ByVal contexto As String,
                        ByVal mensajeUsuario As String)
        EscribirEntrada("CRIT ", contexto, mensajeUsuario, ex)

        ' Un error crítico NUNCA debe pasar silenciosamente.
        ' El usuario debe saber que la operación no se completó correctamente.
        MessageBox.Show(
            mensajeUsuario & Environment.NewLine & Environment.NewLine &
            "Detalle técnico: " & ex.Message & Environment.NewLine & Environment.NewLine &
            "Se ha guardado el registro del error en:" & Environment.NewLine & RutaArchivoLog,
            "Error — ARCO",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)
    End Sub

    ' ═════════════════════════════════════════════════════════════════════════
    '  IMPLEMENTACIÓN INTERNA — No llamar directamente desde formularios
    ' ═════════════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Punto central de construcción y escritura de entradas de log.
    ''' Todos los métodos públicos (Info, Warning, Error, Critical) pasan por aquí.
    ''' </summary>
    Private Sub EscribirEntrada(ByVal nivel As String,
                                 ByVal contexto As String,
                                 ByVal mensaje As String,
                                 ByVal ex As Exception)
        Try
            Dim sb As New System.Text.StringBuilder()
            Dim timestamp As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")

            If ex Is Nothing Then
                '── Formato compacto para INFO y WARN ─────────────────────────────
                ' [2026-07-06 14:23:45.123] [INFO ] Form_02_PagColumnas.Button2_Click
                '   → Iniciando cálculo de columnas Frame+Pier
                sb.AppendLine($"[{timestamp}] [{nivel}] {contexto}")
                If Not String.IsNullOrWhiteSpace(mensaje) Then
                    sb.AppendLine($"  → {mensaje}")
                End If
            Else
                '── Formato detallado para ERROR y CRIT ───────────────────────────
                ' [2026-07-06 14:23:45.123] [ERROR] Form_02_PagColumnas.Button2_Click
                '   Contexto : Procesando sección i=4, columna C7-Piso3
                '   Tipo     : System.InvalidCastException
                '   Mensaje  : Unable to cast object of type 'DBNull' to 'System.String'
                '   Inner    : (mensaje de excepción interna si existe)
                '   Stack    :
                '     at ARCO.Form_02_PagColumnas.Button2_Click(...)
                '     at System.Windows.Forms.Button.OnClick(...)
                ' ────────────────────────────────────────────────────────────────
                sb.AppendLine($"[{timestamp}] [{nivel}] {contexto}")

                If Not String.IsNullOrWhiteSpace(mensaje) Then
                    sb.AppendLine($"  Contexto : {mensaje}")
                End If

                sb.AppendLine($"  Tipo     : {ex.GetType().FullName}")
                sb.AppendLine($"  Mensaje  : {ex.Message}")

                ' Incluir excepción interna si existe (común en errores de serialización y OleDb)
                If ex.InnerException IsNot Nothing Then
                    sb.AppendLine($"  Inner    : {ex.InnerException.GetType().Name}: {ex.InnerException.Message}")
                End If

                sb.AppendLine($"  Stack    :")
                sb.AppendLine(FormatearStackTrace(ex.StackTrace))

                ' Línea separadora para facilitar la lectura del archivo de log
                sb.AppendLine(New String("─"c, ANCHO_SEPARADOR))
            End If

            Dim entradaFinal As String = sb.ToString()

            ' ── Destino 1: Ventana Output del IDE ─────────────────────────────
            ' Visible en Visual Studio → menú Ver → Salida (Output) durante debugging.
            ' En producción (sin IDE adjunto) este llamado no hace nada, no penaliza.
            Debug.Print(entradaFinal)

            ' ── Destino 2: Archivo de log en disco ────────────────────────────
            EscribirAlArchivo(entradaFinal)

        Catch logEx As Exception
            ' Si el Logger mismo falla, no generar un loop de errores.
            ' Solo intentamos Debug.Print como último recurso.
            Debug.Print($"[LOGGER INTERNO — FALLO AL REGISTRAR] {logEx.Message}")
            Debug.Print($"  Contexto original: {contexto}")
        End Try
    End Sub

    ''' <summary>
    ''' Escribe el texto al archivo de log del día actual.
    ''' Crea la carpeta Logs/ automáticamente si no existe.
    ''' Operación protegida con SyncLock para ser thread-safe.
    ''' </summary>
    Private Sub EscribirAlArchivo(ByVal entrada As String)
        SyncLock _lockArchivo
            Try
                ' Crear la carpeta de logs la primera vez (no falla si ya existe)
                If Not Directory.Exists(RutaCarpetaLogs) Then
                    Directory.CreateDirectory(RutaCarpetaLogs)
                End If

                ' AppendAllText: agrega al final del archivo, lo crea si no existe.
                ' UTF-8 para soportar caracteres especiales (tildes, ñ, etc.)
                File.AppendAllText(RutaArchivoLog, entrada, System.Text.Encoding.UTF8)

            Catch ioEx As Exception
                ' No se puede escribir al archivo (sin permisos, disco lleno, ruta inválida).
                ' Solo reportar por Debug para no generar un loop de errores en el Logger.
                Debug.Print($"[LOGGER] No se pudo escribir al archivo de log: {ioEx.Message}")
                Debug.Print($"[LOGGER] Ruta intentada: {RutaArchivoLog}")
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Formatea el stack trace con indentación para mayor legibilidad en el archivo de log.
    ''' Cada línea del stack (cada "at ...") queda indentada con 4 espacios.
    ''' </summary>
    Private Function FormatearStackTrace(ByVal stackTrace As String) As String
        If String.IsNullOrWhiteSpace(stackTrace) Then
            Return "    (sin información de stack trace)"
        End If

        Dim separadores = {Environment.NewLine, vbLf, vbCr}
        Dim lineas = stackTrace.Split(separadores, StringSplitOptions.RemoveEmptyEntries)
        Return String.Join(Environment.NewLine,
                           lineas.Select(Function(l) "    " & l.Trim()))
    End Function

End Module
