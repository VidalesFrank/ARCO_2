Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports ARCO.Funciones_10_AnalisisSeccion

' ════════════════════════════════════════════════════════════════════════════
'  Módulo 10 v2.0 — Persistencia de secciones
'
'  Formato de archivo:  .arcosec
'  Codificación:        BinaryFormatter (consistente con .esm del proyecto).
'                       No es texto legible, pero permite serializar la lista
'                       de RefuerzoSimple sin duplicar tipos y sobrevive a
'                       cambios menores del formulario gracias a OptionalField
'                       + OnDeserialized.
'
'  El snapshot captura TODOS los inputs de la UI y también las barras
'  individuales — esto preserva las ediciones por clic derecho que el usuario
'  hizo sobre barras específicas (distintas al patrón).
' ════════════════════════════════════════════════════════════════════════════
<Serializable>
Public Class SeccionV2Snapshot

    ' Metadatos
    Public Version As Integer = 1
    Public FechaCreacion As DateTime = DateTime.Now
    Public NombreSeccion As String = ""
    Public Descripcion As String = ""

    ' Tipo y geometría
    Public TipoSecInt As Integer = 0      ' 0 = Rectangular, 1 = Circular
    Public B As Single = 0.4F
    Public H As Single = 0.6F
    Public D As Single = 0.5F
    Public Rec As Single = 0.04F

    ' Materiales
    Public Fc As Single = 21
    Public Fy As Single = 420
    Public Es As Single = 200000

    ' Refuerzo rectangular (dgvRef, 4 filas)
    Public BarraEsquinas As String = "#5"
    Public BarraSuperior As String = "#5"
    Public NSuperior As Integer = 2
    Public BarraInferior As String = "#5"
    Public NInferior As Integer = 2
    Public BarraLateral As String = "#5"
    Public NLateralPorCara As Integer = 1

    ' Refuerzo circular
    Public NCircular As Integer = 8
    Public BarraCircular As String = "#5"

    ' Confinamiento (Mander)
    Public BarraEstribo As String = "#3"
    Public SEstribo As Single = 0.1F
    Public RamasB As Integer = 2
    Public RamasH As Integer = 2
    Public EpsSu As Single = 0.09F

    ' Análisis / cargas
    Public P_MK As Single = 0                ' P axial para M-κ (kN)
    Public AnguloGrados As Integer = 0
    Public Pu_Nominal As Single = 0
    Public Demanda_P As Single = Single.NaN
    Public Demanda_M As Single = Single.NaN

    ' Barras individuales — snapshot preciso incluyendo ediciones manuales
    Public Barras As New List(Of RefuerzoSimple)

    ' ── Reinicialización de campos opcionales al abrir versiones antiguas ──
    <Runtime.Serialization.OnDeserialized>
    Private Sub OnDeserialized(ctx As Runtime.Serialization.StreamingContext)
        If Barras Is Nothing Then Barras = New List(Of RefuerzoSimple)
        If String.IsNullOrEmpty(NombreSeccion) Then NombreSeccion = ""
        If String.IsNullOrEmpty(Descripcion) Then Descripcion = ""
        If BarraEsquinas Is Nothing Then BarraEsquinas = "#5"
        If BarraSuperior Is Nothing Then BarraSuperior = "#5"
        If BarraInferior Is Nothing Then BarraInferior = "#5"
        If BarraLateral Is Nothing Then BarraLateral = "#5"
        If BarraCircular Is Nothing Then BarraCircular = "#5"
        If BarraEstribo Is Nothing Then BarraEstribo = "#3"
    End Sub

    ' ─────────────────────────────────────────────────────────────────────
    '  Guardar / Abrir
    ' ─────────────────────────────────────────────────────────────────────

    Public Const Extension As String = ".arcosec"
    Public Const FiltroDialogo As String = "Sección ARCO (*.arcosec)|*.arcosec|Todos los archivos (*.*)|*.*"

    Public Shared Function CarpetaPorDefecto() As String
        Dim raiz = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Dim ruta = Path.Combine(raiz, "ARCO_2", "Secciones")
        Try
            If Not Directory.Exists(ruta) Then Directory.CreateDirectory(ruta)
        Catch
        End Try
        Return ruta
    End Function

    Public Sub Guardar(rutaArchivo As String)
        Dim bf As New BinaryFormatter()
        Using fs As New FileStream(rutaArchivo, FileMode.Create, FileAccess.Write)
            bf.Serialize(fs, Me)
        End Using
    End Sub

    Public Shared Function Cargar(rutaArchivo As String) As SeccionV2Snapshot
        Dim bf As New BinaryFormatter()
        Using fs As New FileStream(rutaArchivo, FileMode.Open, FileAccess.Read)
            Return DirectCast(bf.Deserialize(fs), SeccionV2Snapshot)
        End Using
    End Function

End Class
