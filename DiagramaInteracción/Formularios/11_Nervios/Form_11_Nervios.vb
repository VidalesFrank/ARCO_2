Imports ARCO.Funciones_00_Varias
Imports ARCO.eNumeradores
Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class Form_11_Nervios

    Public Shared Proyecto As Proyecto

    Private _svc As New NervioService()
    Private _diagSvc As New NervioDiagramaService()
    Private _joints As Dictionary(Of String, cJoint)
    Private _nervioActual As cNervio
    Private _framesActuales As List(Of cFrameNervio)
    Private _cargando As Boolean = False
    Private _plantaAmpliada As Form_PlantaInteractivaNervios = Nothing

    ' ── Portapapeles interno para copiar/pegar columnas de refuerzo ──────────
    Private _cbSup As Integer() = Nothing    ' 7 cantidades por calibre (Ref_Superior)
    Private _cbInf As Integer() = Nothing    ' 7 cantidades por calibre (Ref_Inferior)
    Private _cbCor As String() = Nothing     ' 4 valores (Tiene/Calibre/Ramas/Sep)
    Private _ctxDgv As DataGridView = Nothing
    Private _ctxCol As Integer = -1

    ' ── Constantes filas/zonas tablas de refuerzo (3 columnas por tramo: Izq/Centro/Der) ─
    Private Shared ReadOnly BarSizes() As String = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}
    Private Shared ReadOnly ZonaTexto() As String = {"Izq", "Centro", "Der"}

    Private Const FILA_COR_TIENE = 0
    Private Const FILA_COR_CALIBRE = 1
    Private Const FILA_COR_RAMAS = 2
    Private Const FILA_COR_SEP = 3

    ' ── Constantes filas tabla demandas (1 columna por tramo) ─────────────────
    Private Const FILA_BW = 0
    Private Const FILA_H = 1
    Private Const FILA_L = 2
    Private Const FILA_BAPO_I = 3
    Private Const FILA_BAPO_D = 4
    Private Const FILA_MUI = 5
    Private Const FILA_MUC = 6
    Private Const FILA_MUD = 7
    Private Const FILA_VUI = 8
    Private Const FILA_VUD = 9
    Private Const FILA_ESECT = 10
    Private Const FILA_EJE_I = 11
    Private Const FILA_EJE_D = 12

    ' ── Constantes filas tabla resultados FLEXIÓN (3 col por tramo: Izq/Centro/Der) ─
    Private Const FILA_RES_MU_NEG = 0      ' Momento negativo (kN·m)
    Private Const FILA_RES_MU_POS = 1      ' Momento positivo (kN·m)
    Private Const FILA_RES_ASMIN_NEG = 2   ' As_min zona neg (cm²)
    Private Const FILA_RES_ASMIN_POS = 3   ' As_min zona pos (cm²)
    Private Const FILA_RES_ASPROV_NEG = 4  ' As prov zona neg (cm²)
    Private Const FILA_RES_ASPROV_POS = 5  ' As prov zona pos (cm²)
    Private Const FILA_RES_PHIMN_NEG = 6   ' φMn zona neg (kN·m)
    Private Const FILA_RES_PHIMN_POS = 7   ' φMn zona pos (kN·m)
    Private Const FILA_RES_CD_AS_NEG = 8   ' C/D Rel As zona neg
    Private Const FILA_RES_CD_AS_POS = 9   ' C/D Rel As zona pos
    Private Const FILA_RES_CD_M_NEG = 10   ' C/D Rel M zona neg (φMn/Mu-)
    Private Const FILA_RES_CD_M_POS = 11   ' C/D Rel M zona pos (φMn/Mu+)
    Private Const FILA_RES_REDIST = 12        ' Redistrib M- apoyos → vano (editable Izq/Der)
    Private Const FILA_RES_REDIST_POS = 13   ' Redistrib M+ vano → apoyos (editable Centro)
    Private Const FILA_RES_CD_M_NEG_REDIST = 14  ' C/D Rel M neg post-redistrib ("como quedaría")
    Private Const FILA_RES_CD_M_POS_REDIST = 15  ' C/D Rel M pos post-redistrib ("como quedaría")
    ' Be (si T) se muestra en la fila de As_min pos

    ' ── Constantes filas tabla resultados CORTANTE ─────────────────────────────
    Private Const FILA_RES_VU = 0           ' Vu (kN)
    Private Const FILA_RES_PHIVC = 1        ' φVc (kN)
    Private Const FILA_RES_PHIVS = 2        ' φVs (kN)
    Private Const FILA_RES_PHIVN = 3        ' φVn (kN)
    Private Const FILA_RES_CDV = 4          ' C/D cortante
    Private Const FILA_RES_CUMPLE = 5       ' Cumple / No cumple

    ' ── Colores planta — paleta categórica validada (contraste + daltonismo) sobre fondo claro ──
    Private Shared ReadOnly ColoresNervio As Color() = {
        Color.FromArgb(42, 120, 214),   ' azul
        Color.FromArgb(235, 104, 52),   ' naranja
        Color.FromArgb(27, 175, 122),   ' aqua
        Color.FromArgb(237, 161, 0),    ' amarillo
        Color.FromArgb(232, 123, 164),  ' magenta
        Color.FromArgb(0, 131, 0),      ' verde
        Color.FromArgb(74, 58, 167),    ' violeta
        Color.FromArgb(227, 73, 72)     ' rojo
    }

    ' ══════════════════════════════════════════════════════════════════════════
    '  CARGA / INICIALIZACIÓN
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Form_11_Nervios_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Me.DesignMode Then Return
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try
        ' Aplicar renderer ARCO (dropdown oscuro, hover verde)
        MenuStrip1.Renderer = New ARCOMenuRenderer()
        ' Agregar "Datos generales..." al menú Opciones (no está en el Designer para mantenerlo limpio)
        Dim miDatos As New ToolStripMenuItem("Datos generales...")
        AddHandler miDatos.Click, AddressOf DatosGeneralesToolStripMenuItem_Click
        OpcionesToolStripMenuItem.DropDownItems.Insert(0, miDatos)
        OpcionesToolStripMenuItem.DropDownItems.Insert(1, New ToolStripSeparator())

        ' Archivo: Guardar como... + Limpiar datos
        Archivo_Zapatas.DropDownItems.Add(New ToolStripSeparator())
        Dim miGuardarComo As New ToolStripMenuItem("Guardar como...")
        miGuardarComo.ForeColor = Color.White
        miGuardarComo.BackColor = Color.FromArgb(57, 57, 57)
        AddHandler miGuardarComo.Click, AddressOf GuardarComoNervios_Click
        Archivo_Zapatas.DropDownItems.Add(miGuardarComo)
        Archivo_Zapatas.DropDownItems.Add(New ToolStripSeparator())
        Dim miLimpiar As New ToolStripMenuItem("Limpiar datos de nervios...")
        miLimpiar.ForeColor = Color.White
        miLimpiar.BackColor = Color.FromArgb(57, 57, 57)
        AddHandler miLimpiar.Click, AddressOf LimpiarDatosNervios_Click
        Archivo_Zapatas.DropDownItems.Add(miLimpiar)

        ' Menú Reportes (antes de Exportar)
        Dim menuReportes As New ToolStripMenuItem("Reportes")
        menuReportes.ForeColor = Color.White
        menuReportes.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuReportes.Click, AddressOf AbrirReportesNervios_Click
        MenuStrip1.Items.Insert(MenuStrip1.Items.IndexOf(Exportar_Zapatas), menuReportes)

        ConstruirTablasNavegacion()
        SincronizarDesdeProyecto()
    End Sub

    Private Sub Form_11_Nervios_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If SplitDiag.Height > 0 Then
            SplitDiag.SplitterDistance = SplitDiag.Height \ 2
        End If
    End Sub

    Public Sub RefrescarDesdeProyecto()
        SincronizarDesdeProyecto()
    End Sub

    Private Sub SincronizarDesdeProyecto()
        If Proyecto Is Nothing Then Return
        If Proyecto.Elementos.Nervios Is Nothing Then
            Proyecto.Elementos.Nervios = New cNervios()
        End If

        Dim nerv0 = Proyecto.Elementos.Nervios
        Dim srcJoints As List(Of cJoint) = If(nerv0 IsNot Nothing AndAlso nerv0.Joints.Count > 0,
                                               nerv0.Joints,
                                               Proyecto.Elementos.Joints)
        _joints = srcJoints.ToDictionary(Function(j) j.ElementLabel)

        _cargando = True
        RefrescarListaPisos()
        _cargando = False

        Dim nerv = Proyecto.Elementos.Nervios
        If nerv.Elementos.Count > 0 Then
            If nerv.Elementos(0).Tf_Losa > 0 Then
                Try
                    NudTf.Value = CDec(nerv.Elementos(0).Tf_Losa)
                Catch
                End Try
            End If
        End If

        Label1.Text = If(nerv.Elementos.Count > 0,
                         $"{nerv.Elementos.Count} nervios cargados — {nerv.ListA_Combinaciones_Design.Count} combos de diseño.",
                         "Sin datos. Use Importar → Importar demandas ETABS...")
    End Sub

    Private Sub RefrescarListaPisos()
        Dim nerv = Proyecto.Elementos.Nervios
        Dim pisos = nerv.Elementos.Select(Function(n) n.Piso).Distinct().OrderBy(Function(s) s).ToList()

        Dim pisoAnt = If(Lista_Pisos.SelectedItem?.ToString(), "")
        Lista_Pisos.DataSource = Nothing
        Lista_Pisos.DataSource = pisos
        Lista_Pisos.DisplayMember = ""

        Dim idx = pisos.IndexOf(pisoAnt)
        If idx >= 0 Then Lista_Pisos.SelectedIndex = idx
        If Lista_Pisos.Items.Count > 0 AndAlso Lista_Pisos.SelectedIndex < 0 Then Lista_Pisos.SelectedIndex = 0
    End Sub

    Private Sub RefrescarListaNervios()
        Dim nerv = Proyecto.Elementos.Nervios
        Dim pisoSel = If(Lista_Pisos.SelectedItem?.ToString(), "")

        Dim lista = If(String.IsNullOrEmpty(pisoSel),
                       nerv.Elementos,
                       nerv.Elementos.Where(Function(n) n.Piso = pisoSel).ToList())

        Dim nombre = If(_nervioActual IsNot Nothing, _nervioActual.Nombre, "")
        _cargando = True
        Lista_Nervios.DataSource = Nothing
        Lista_Nervios.DataSource = lista
        Lista_Nervios.DisplayMember = "NombrePlano"
        _cargando = False

        Dim idx = lista.IndexOf(lista.FirstOrDefault(Function(n) n.Nombre = nombre))
        If idx >= 0 Then
            Lista_Nervios.SelectedIndex = idx
        ElseIf Lista_Nervios.Items.Count > 0 Then
            Lista_Nervios.SelectedIndex = 0
        Else
            LimpiarTablas()
        End If

        LlenarTablaNervios()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  IMPORTAR ETABS
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ImportarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles ImportarDemandasToolStripMenuItem.Click

        Try
            Dim ruta As String
            Using dlg As New OpenFileDialog()
                dlg.Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls"
                dlg.Title = "Seleccionar archivo ETABS"
                If dlg.ShowDialog() <> DialogResult.OK Then Return
                ruta = dlg.FileName
            End Using

            Cursor = Cursors.WaitCursor
            Label1.Text = "Importando..."
            Application.DoEvents()

            Dim hojas = ObtenerHojasExcel(ruta)

            Dim nerv = Proyecto.Elementos.Nervios

            ' Leer joints y frames PROPIOS de Nervios desde el Excel importado,
            ' sin depender de lo que cargaron Vigas u otros módulos.
            Dim hJoints = ResolverNombreHoja(hojas, "Objects and Elements - Joints", "Joint Coordinates")
            Dim hFrames = ResolverNombreHoja(hojas, "Objects and Elements - Frames", "Connectivity - Frame")
            nerv.Joints = DataTableToJoints(LeerHojaExcel(ruta, hJoints))
            nerv.Frames = DataTableToFrames(LeerHojaExcel(ruta, hFrames))

            Dim hAsigFrame = ResolverNombreHoja(hojas, "Frame Assigns - Sect Prop", "Frame Assignments - Sections")
            Dim hSecDef = ResolverNombreHoja(hojas, "Frame Sec Def - Conc Rect", "Frame Sections")
            Dim hMaterial = ResolverNombreHoja(hojas, "Mat Prop - Concrete Data", "Material Properties - Concrete")

            DataTableToAsignFrame(nerv.Frames,
                                  LeerHojaExcel(ruta, hAsigFrame),
                                  LeerHojaExcel(ruta, hSecDef),
                                  LeerHojaExcel(ruta, hMaterial))

            _joints = nerv.Joints.ToDictionary(Function(j) j.ElementLabel)

            Dim todosFrames = nerv.Frames

            ' Selección de secciones nervio
            Dim resultado As New List(Of String)
            If Not Form_FiltroSecciones.Mostrar(todosFrames, nerv.Secciones_Nervio, resultado) Then
                Return
            End If
            If resultado.Count = 0 Then
                MessageBox.Show("Debe seleccionar al menos una sección de nervio.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            nerv.Secciones_Nervio = resultado

            Dim secNervioSet As New HashSet(Of String)(nerv.Secciones_Nervio, StringComparer.OrdinalIgnoreCase)

            ' Frames nervio
            Dim framesNervio = todosFrames.Where(
                Function(f) f.Section IsNot Nothing AndAlso secNervioSet.Contains(f.Section.Nombre)).ToList()

            If framesNervio.Count = 0 Then
                MessageBox.Show("No se encontraron frames con las secciones seleccionadas en el modelo.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Importar fuerzas
            nerv.BeamForces = _svc.ImportarBeamForcesNervios(ruta, hojas, secNervioSet, todosFrames)

            ' Combinaciones disponibles
            nerv.Lista_Combinaciones = nerv.BeamForces _
                .Select(Function(r) r.LoadCaseKey).Distinct().OrderBy(Function(c) c).ToList()

            ' Auto-agrupar nervios
            nerv.Elementos = _svc.GenerarNerviosAuto(framesNervio, _joints)

            ' Aplicar agrupaciones manuales previas (si el usuario las había definido)
            If nerv.GruposManual.Count > 0 Then
                _svc.AplicarGruposManual(nerv.Elementos, nerv.GruposManual, nerv.Frames, _joints)
            End If

            ' Detectar apoyos
            _svc.DetectarApoyos(nerv.Elementos, todosFrames, _joints, secNervioSet)

            ' Ejes/grillas: cargar desde ETABS si aún no existen; si el archivo no
            ' trae la hoja (o quedó vacía tras el filtro de diagonales), ofrecer
            ' entrada manual — sin ejes no se puede identificar el apoyo en reportes.
            If Proyecto.Elementos.Grids.GridLines.Count = 0 Then
                Dim hGrids = ResolverNombreHoja(hojas, "Grid Definitions - Grid Lines", "Grid Lines")
                Dim tablaGrids As DataTable = LeerHojaExcel(ruta, hGrids)
                Proyecto.Elementos.Grids.GridLines = DataTableToGridLines(tablaGrids)
            End If

            If Proyecto.Elementos.Grids.GridLines.Count = 0 Then
                Dim quiereManual = MessageBox.Show(
                    "El archivo ETABS no incluye la definición de ejes/grillas." & vbCrLf &
                    "¿Desea definir los ejes manualmente para poder identificarlos en los reportes?",
                    "Ejes no encontrados", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If quiereManual = DialogResult.Yes Then
                    Dim gridsManual As List(Of cGridLine) = Nothing
                    If Form_DefinirEjesManual.Mostrar(Proyecto.Elementos.Grids.GridLines, gridsManual) Then
                        Proyecto.Elementos.Grids.GridLines = gridsManual
                    End If
                End If
            End If

            _svc.AsignarEjesANervios(nerv.Elementos, Proyecto.Elementos.Grids.GridLines, _joints)

            ' Calcular paso auto y asignar tf
            Dim tf = CDbl(NudTf.Value)
            For Each n In nerv.Elementos
                n.Tf_Losa = tf
                Dim paso = _svc.CalcularPasoNerviosAuto(n, nerv.Elementos, _joints)
                If paso > 0 Then n.Paso_Nervios = paso
            Next

            ' Selección de combinaciones de diseño
            Dim fOpc As New Form_Opciones_Combinaciones()
            fOpc.OpcionLlamado = "Nervios"
            For Each combo In nerv.Lista_Combinaciones
                fOpc.Lista_Combinaciones.Items.Add(combo)
            Next
            For Each combo In nerv.ListA_Combinaciones_Design
                If nerv.Lista_Combinaciones.Contains(combo) Then
                    fOpc.Lista_Cargas_Design.Items.Add(combo)
                    fOpc.Lista_Combinaciones.Items.Remove(combo)
                End If
            Next
            fOpc.ShowDialog()

            Dim nFrames = nerv.Elementos.Sum(Function(n) n.Frames.Count)
            Label1.Text = $"Importado: {nerv.Elementos.Count} nervios | {nFrames} tramos | " &
                          $"{nerv.Lista_Combinaciones.Count} combinaciones | " &
                          $"{nerv.ListA_Combinaciones_Design.Count} combos de diseño."

            _cargando = True
            RefrescarListaPisos()
            RefrescarListaNervios()
            _cargando = False

        Catch ex As Exception
            Logger.Error(ex, "Form_11_Nervios.ImportarDemandasToolStripMenuItem_Click")
            MessageBox.Show(ex.Message, "Error al importar", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  OPCIONES — Combinaciones de diseño
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ActualizarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles ActualizarDemandasToolStripMenuItem.Click

        Dim nerv = Proyecto.Elementos.Nervios
        If nerv.Lista_Combinaciones.Count = 0 Then
            MessageBox.Show("Primero importe las demandas ETABS.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim fOpc As New Form_Opciones_Combinaciones()
        fOpc.OpcionLlamado = "Nervios"
        For Each combo In nerv.Lista_Combinaciones
            If Not nerv.ListA_Combinaciones_Design.Contains(combo) Then
                fOpc.Lista_Combinaciones.Items.Add(combo)
            End If
        Next
        For Each combo In nerv.ListA_Combinaciones_Design
            fOpc.Lista_Cargas_Design.Items.Add(combo)
        Next
        fOpc.ShowDialog()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  OPCIONES — Datos generales (recubrimiento global)
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub DatosGeneralesToolStripMenuItem_Click(sender As Object, e As EventArgs)
        Dim nerv = Proyecto.Elementos.Nervios
        Dim recActual = If(nerv.Recubrimiento > 0, nerv.Recubrimiento * 100, 4.0)  ' cm

        Dim prompt = $"Recubrimiento (cm):{vbCrLf}(Valor actual: {recActual:F1} cm   |   0 = usar valor de sección ETABS)"
        Dim respuesta = InputBox(prompt, "Datos generales — Módulo Nervios", recActual.ToString("F1"))

        If String.IsNullOrWhiteSpace(respuesta) Then Return
        Dim recCm As Double = 0
        If Not Double.TryParse(respuesta.Replace(",", "."),
                               Globalization.NumberStyles.Any,
                               Globalization.CultureInfo.InvariantCulture, recCm) Then
            MessageBox.Show("Valor no válido. Ingrese un número.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        nerv.Recubrimiento = Math.Max(0, recCm / 100.0)  ' guardar en metros

        ' Propagarlo inmediatamente a todos los frames cargados y recalcular
        If _framesActuales IsNot Nothing Then
            For Each fn In _framesActuales
                If nerv.Recubrimiento > 0 Then fn.Recubrimiento = nerv.Recubrimiento
            Next
            _cargando = True
            LlenarResultados()
            _cargando = False
        End If

        Dim msg = If(nerv.Recubrimiento > 0,
                     $"Recubrimiento global ajustado a {nerv.Recubrimiento * 100:F1} cm.{vbCrLf}Recalcule (Botón Calcular) para aplicar.",
                     "Recubrimiento global eliminado. Se usará el valor de la sección ETABS.")
        MessageBox.Show(msg, "Datos generales", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub DefinirEjesManualmenteToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles DefinirEjesManualmenteToolStripMenuItem.Click

        Dim gridsResultado As List(Of cGridLine) = Nothing
        If Not Form_DefinirEjesManual.Mostrar(Proyecto.Elementos.Grids.GridLines, gridsResultado) Then Return

        Proyecto.Elementos.Grids.GridLines = gridsResultado

        Dim nerv = Proyecto.Elementos.Nervios
        If nerv.Elementos.Count > 0 Then
            _svc.AsignarEjesANervios(nerv.Elementos, Proyecto.Elementos.Grids.GridLines, _joints)
            _cargando = True
            LlenarTablas()
            _cargando = False
            DibujarPlanta()
        End If

        MessageBox.Show("Ejes actualizados.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  CALCULAR
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Try
            Cursor = Cursors.WaitCursor
            Label1.Text = "Calculando..."
            Application.DoEvents()

            Dim nerv = Proyecto.Elementos.Nervios

            If nerv.Elementos.Count = 0 Then
                MessageBox.Show("Primero importe los datos de ETABS.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If nerv.ListA_Combinaciones_Design.Count = 0 Then
                MessageBox.Show("Seleccione combinaciones de diseño en Opciones → Combinaciones de diseño.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Actualizar tf_losa
            Dim tf = CDbl(NudTf.Value)
            For Each n In nerv.Elementos
                n.Tf_Losa = tf
            Next

            Dim combosSet As New HashSet(Of String)(
                nerv.ListA_Combinaciones_Design.Select(Function(c) NormalizarClaveCombo(c)))

            _svc.CalcularEnvolventesNervios(nerv.Elementos, nerv.BeamForces, combosSet)
            _svc.DesignarNervios(nerv.Elementos, _joints, nerv.Recubrimiento)

            _cargando = True
            RefrescarListaPisos()
            RefrescarListaNervios()
            _cargando = False

            Dim nCumplen = nerv.Elementos.SelectMany(Function(n) n.Frames).Where(Function(f) f.Cumple).Count()
            Dim nTotal = nerv.Elementos.Sum(Function(n) n.Frames.Count)
            Label1.Text = $"Cálculo completado — {nCumplen}/{nTotal} tramos cumplen."

            ' Cargar el nervio seleccionado
            Dim sel = TryCast(Lista_Nervios.SelectedItem, cNervio)
            If sel IsNot Nothing Then CargarNervioCompleto(sel)

        Catch ex As Exception
            Logger.Error(ex, "Form_11_Nervios.Button1_Click")
            MessageBox.Show(ex.Message, "Error en cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  NAVEGACIÓN
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Lista_Pisos_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles Lista_Pisos.SelectedIndexChanged
        If _cargando Then Return
        _cargando = True
        RefrescarListaNervios()
        _cargando = False
        DibujarPlanta()
    End Sub

    Private Sub Lista_Nervios_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles Lista_Nervios.SelectedIndexChanged
        If _cargando Then Return
        Dim n = TryCast(Lista_Nervios.SelectedItem, cNervio)
        If n Is Nothing Then Return
        CargarNervioCompleto(n)
    End Sub

    Private Sub CargarNervioCompleto(nervio As cNervio)
        _nervioActual = nervio
        _framesActuales = nervio.Frames

        Nombre_Nervio.Text = If(Not String.IsNullOrWhiteSpace(nervio.NombrePlano),
                                nervio.NombrePlano, nervio.Nombre)

        ActualizarCmbTipoNervio(nervio)
        ActualizarInfoGrupo(nervio)

        ' Sync Tabla_Nervios selection without triggering its handler
        _cargando = True
        For Each row As DataGridViewRow In Tabla_Nervios.Rows
            Dim sel = ReferenceEquals(row.Tag, nervio)
            If row.Selected <> sel Then row.Selected = sel
            If sel AndAlso Not row.Displayed Then
                Tabla_Nervios.FirstDisplayedScrollingRowIndex = row.Index
            End If
        Next
        _cargando = False

        ConstruirTablas()
        LlenarTablas()
        LlenarTablaFrames(nervio)
        DibujarPlanta()
        DibujarDiagramas()
        If _plantaAmpliada IsNot Nothing AndAlso Not _plantaAmpliada.IsDisposed Then
            _plantaAmpliada.ActualizarSeleccion(nervio)
        End If
    End Sub

    Private Sub DibujarDiagramas()
        If _nervioActual Is Nothing Then Return
        Dim nerv = Proyecto.Elementos.Nervios
        Dim combosDiseno As New HashSet(Of String)(nerv.ListA_Combinaciones_Design)
        _diagSvc.DibujarDiagramaMomento(_nervioActual, Diagrama_Momento, combosDiseno,
                                         mostrarCapacidad:=ChkMostrarCapacidad.Checked)
        _diagSvc.DibujarDiagramaCortante(_nervioActual, Diagrama_Cortante, combosDiseno)
    End Sub

    Private Sub ChkMostrarCapacidad_CheckedChanged(sender As Object, e As EventArgs) _
        Handles ChkMostrarCapacidad.CheckedChanged
        DibujarDiagramas()
    End Sub

    Private Sub Diagrama_Momento_Resize(sender As Object, e As EventArgs) Handles Diagrama_Momento.Resize
        DibujarDiagramas()
    End Sub

    Private Sub Diagrama_Cortante_Resize(sender As Object, e As EventArgs) Handles Diagrama_Cortante.Resize
        DibujarDiagramas()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  CONSTRUCCIÓN DE TABLAS
    '  Refuerzo/Resultados: 3 columnas por tramo (Izq/Centro/Der), igual que Vigas.
    '  Demandas: 1 columna por tramo (valores no discretizados por zona).
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ConstruirTablas()
        If _framesActuales Is Nothing Then Return

        ConstruirTablaRefuerzo(Ref_Superior)
        ConstruirTablaRefuerzo(Ref_Inferior)
        ConstruirTablaRefuerzoCortante()
        ConstruirTablaDemandas()
        ConstruirTablaResultados()
    End Sub

    ''' <summary>Agrega 3 columnas por tramo (Izq/Centro/Der), header "{frame}\n{zona}".</summary>
    Private Sub AgregarColumnasPorZona(dgv As DataGridView, ancho As Integer)
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True

        For Each fn In _framesActuales
            For Each zonaTxt In ZonaTexto
                Dim col As New DataGridViewTextBoxColumn()
                col.HeaderText = fn.ObjectLabel & vbCrLf & zonaTxt
                col.Width = ancho
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                dgv.Columns.Add(col)
            Next
        Next
    End Sub

    Private Sub ConstruirTablaRefuerzo(dgv As DataGridView)
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumnasPorZona(dgv, 55)

        For Each barra In BarSizes
            Dim rowIdx = dgv.Rows.Add()
            dgv.Rows(rowIdx).HeaderCell.Value = barra
        Next
    End Sub

    Private Sub ConstruirTablaRefuerzoCortante()
        Ref_Cortante.Columns.Clear()
        Ref_Cortante.Rows.Clear()

        AgregarColumnasPorZona(Ref_Cortante, 62)

        Dim etiquetas = {"Tiene est.", "Calibre", "Ramas", "Sep (m)"}
        For Each lbl In etiquetas
            Dim rowIdx = Ref_Cortante.Rows.Add()
            Ref_Cortante.Rows(rowIdx).HeaderCell.Value = lbl
        Next
    End Sub

    Private Sub ConstruirTablaDemandas()
        Tabla_Demandas.Columns.Clear()
        Tabla_Demandas.Rows.Clear()
        Tabla_Demandas.RowHeadersWidth = 145   ' suficiente para "Redistrib I (0–0.20)"

        For Each fn In _framesActuales
            Dim col As New DataGridViewTextBoxColumn()
            col.HeaderText = fn.ObjectLabel
            col.Width = 80
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Tabla_Demandas.Columns.Add(col)
        Next

        Dim etiquetas = {"Bw (m)", "H (m)", "L (m)", "b_apoyo I (m)", "b_apoyo D (m)",
                         "Mu neg I (kN·m)", "Mu pos C (kN·m)", "Mu neg D (kN·m)",
                         "Vu I (kN)", "Vu D (kN)", "Es Sección T (S/N)", "Eje Izq", "Eje Der"}
        For Each lbl In etiquetas
            Dim rowIdx = Tabla_Demandas.Rows.Add()
            Tabla_Demandas.Rows(rowIdx).HeaderCell.Value = lbl
        Next

        ' Lectura: BW, H, L, Mu/Vu
        Dim filasLectura = {FILA_BW, FILA_H, FILA_L, FILA_MUI, FILA_MUC, FILA_MUD, FILA_VUI, FILA_VUD}
        For Each row In filasLectura
            For col As Integer = 0 To Tabla_Demandas.Columns.Count - 1
                Tabla_Demandas.Rows(row).Cells(col).Style.BackColor = Color.FromArgb(240, 240, 240)
                Tabla_Demandas.Rows(row).Cells(col).ReadOnly = True
            Next
        Next
        ' b_apoyo: editable, fondo amarillo
        For col As Integer = 0 To Tabla_Demandas.Columns.Count - 1
            Tabla_Demandas.Rows(FILA_BAPO_I).Cells(col).Style.BackColor = Color.FromArgb(255, 250, 210)
            Tabla_Demandas.Rows(FILA_BAPO_D).Cells(col).Style.BackColor = Color.FromArgb(255, 250, 210)
        Next
    End Sub

    Private Sub ConstruirTablaResultados()
        Tabla_Resultados_Flexion.Columns.Clear()
        Tabla_Resultados_Flexion.Rows.Clear()
        Tabla_Resultados_Cortante.Columns.Clear()
        Tabla_Resultados_Cortante.Rows.Clear()

        AgregarColumnasPorZona(Tabla_Resultados_Flexion, 72)
        AgregarColumnasPorZona(Tabla_Resultados_Cortante, 72)

        ' 12 filas de flexión: negativo/positivo para cada parámetro
        Dim etqFlex = {
            "Mu neg (kN·m)",
            "Mu pos (kN·m)",
            "As_min neg (cm²)",
            "As_min pos (cm²)",
            "As prov neg (cm²)",
            "As prov pos (cm²)",
            "φMn neg (kN·m)",
            "φMn pos (kN·m)",
            "C/D Rel As neg",
            "C/D Rel As pos",
            "C/D Rel M neg",
            "C/D Rel M pos"
        }
        For Each lbl In etqFlex
            Dim r = Tabla_Resultados_Flexion.Rows.Add()
            Tabla_Resultados_Flexion.Rows(r).HeaderCell.Value = lbl
        Next
        ' Filas 0-11: solo lectura
        For r As Integer = 0 To FILA_RES_CD_M_POS
            Tabla_Resultados_Flexion.Rows(r).ReadOnly = True
        Next
        ' Fila 12: redistribución M- (editable en col Izq/Der de cada tramo)
        Dim rRed = Tabla_Resultados_Flexion.Rows.Add()
        Tabla_Resultados_Flexion.Rows(rRed).HeaderCell.Value = "Redistrib M- (0–0.20)"
        Tabla_Resultados_Flexion.Rows(rRed).DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220)
        Tabla_Resultados_Flexion.Rows(rRed).DefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)

        ' Fila 13: redistribución M+ (editable en col Centro de cada tramo)
        Dim rRedPos = Tabla_Resultados_Flexion.Rows.Add()
        Tabla_Resultados_Flexion.Rows(rRedPos).HeaderCell.Value = "Redistrib M+ (0–0.20)"
        Tabla_Resultados_Flexion.Rows(rRedPos).DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220)
        Tabla_Resultados_Flexion.Rows(rRedPos).DefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)

        ' Filas 14-15: C/D post-redistribución ("como quedaría") — solo lectura
        Dim rCDNegR = Tabla_Resultados_Flexion.Rows.Add()
        Tabla_Resultados_Flexion.Rows(rCDNegR).HeaderCell.Value = "C/D M neg (Redistrib)"
        Tabla_Resultados_Flexion.Rows(rCDNegR).ReadOnly = True

        Dim rCDPosR = Tabla_Resultados_Flexion.Rows.Add()
        Tabla_Resultados_Flexion.Rows(rCDPosR).HeaderCell.Value = "C/D M pos (Redistrib)"
        Tabla_Resultados_Flexion.Rows(rCDPosR).ReadOnly = True

        Tabla_Resultados_Flexion.RowHeadersWidth = 165

        ' 6 filas de cortante
        Dim etqCor = {"Vu (kN)", "φVc (kN)", "φVs (kN)", "φVn (kN)", "C/D cortante", "Cumple"}
        For Each lbl In etqCor
            Dim r = Tabla_Resultados_Cortante.Rows.Add()
            Tabla_Resultados_Cortante.Rows(r).HeaderCell.Value = lbl
        Next
        Tabla_Resultados_Cortante.RowHeadersWidth = 110
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  LLENAR TABLAS desde datos del proyecto
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub LlenarTablas()
        If _framesActuales Is Nothing OrElse _framesActuales.Count = 0 Then Return
        _cargando = True
        Try
            LlenarRefuerzo()
            LlenarDemandas()
            LlenarResultados()
        Finally
            _cargando = False
        End Try
    End Sub

    Private Sub LlenarRefuerzo()
        For fi As Integer = 0 To _framesActuales.Count - 1
            Dim fn = _framesActuales(fi)

            For zi As Integer = 0 To 2
                Dim col = fi * 3 + zi
                Dim posicion = ObtenerPosicion(zi)

                LlenarColumnaZona(Ref_Superior, col, fn.RefuerzoSuperior, posicion)
                LlenarColumnaZona(Ref_Inferior, col, fn.RefuerzoInferior, posicion)
                LlenarColumnaCortante(col, fn.RefuerzoTransversal, posicion)

                ColorizarCeldasConValor(Ref_Superior, col)
                ColorizarCeldasConValor(Ref_Inferior, col)
            Next
        Next
    End Sub

    ''' <summary>Convierte el índice de columna zonal (0/1/2) a Izquierda/Centro/Derecha.</summary>
    Private Shared Function ObtenerPosicion(zi As Integer) As PosicionTramoViga
        Select Case zi
            Case 0 : Return PosicionTramoViga.Izquierda
            Case 1 : Return PosicionTramoViga.Centro
            Case Else : Return PosicionTramoViga.Derecha
        End Select
    End Function

    Private Sub LlenarColumnaZona(dgv As DataGridView, col As Integer,
                                   lista As List(Of cRefuerzoTramo), posicion As PosicionTramoViga)
        Dim tramo = If(lista, New List(Of cRefuerzoTramo)).FirstOrDefault(Function(t) t.Posicion = posicion)
        For row As Integer = 0 To BarSizes.Length - 1
            Dim cant As Integer = 0
            If tramo IsNot Nothing AndAlso tramo.Barras IsNot Nothing Then
                tramo.Barras.TryGetValue(BarSizes(row), cant)
            End If
            dgv.Rows(row).Cells(col).Value = cant
        Next
    End Sub

    Private Sub LlenarColumnaCortante(col As Integer, lista As List(Of cRefuerzoTransversalZona), posicion As PosicionTramoViga)
        Dim zona = If(lista, New List(Of cRefuerzoTransversalZona)).FirstOrDefault(Function(z) z.Posicion = posicion)
        Ref_Cortante.Rows(FILA_COR_TIENE).Cells(col).Value = If(zona IsNot Nothing AndAlso zona.CantEstribos > 0, "S", "N")
        Ref_Cortante.Rows(FILA_COR_CALIBRE).Cells(col).Value = "#" & If(zona IsNot Nothing AndAlso zona.NumeroBarra > 0, zona.NumeroBarra, 3)
        Ref_Cortante.Rows(FILA_COR_RAMAS).Cells(col).Value = If(zona IsNot Nothing AndAlso zona.CantEstribos > 0, zona.CantEstribos, 2)
        Ref_Cortante.Rows(FILA_COR_SEP).Cells(col).Value = If(zona IsNot Nothing AndAlso zona.Separacion > 0, zona.Separacion, 0.15).ToString("F3")
    End Sub

    ''' <summary>Resalta las celdas de calibre con cantidad > 0 — verde Excel con texto verde oscuro en bold.</summary>
    Private Sub ColorizarCeldasConValor(dgv As DataGridView, col As Integer)
        For row As Integer = 0 To BarSizes.Length - 1
            Dim v As Integer = 0
            Integer.TryParse(dgv.Rows(row).Cells(col).Value?.ToString(), v)
            Dim esActivo = v > 0
            dgv.Rows(row).Cells(col).Style.BackColor =
                If(esActivo, ColorTranslator.FromHtml("#C6EFCE"), Color.Empty)
            dgv.Rows(row).Cells(col).Style.ForeColor =
                If(esActivo, ColorTranslator.FromHtml("#006100"), Color.Empty)
            dgv.Rows(row).Cells(col).Style.Font =
                If(esActivo, New Font("Segoe UI", 9, FontStyle.Bold), Nothing)
        Next
    End Sub

    ' ── Separadores de vano (igual que Vigas) ────────────────────────────────────
    Private Sub PintarLineasCada3Columnas(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 Then Return
        e.Paint(e.CellBounds, DataGridViewPaintParts.All)
        If (e.ColumnIndex + 1) Mod 3 = 0 Then
            Using pen As New Pen(Color.Black, 2)
                Dim x = e.CellBounds.Right - 1
                e.Graphics.DrawLine(pen, x, e.CellBounds.Top, x, e.CellBounds.Bottom)
            End Using
        End If
        e.Handled = True
    End Sub

    Private Sub Ref_Superior_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) _
        Handles Ref_Superior.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Ref_Inferior_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) _
        Handles Ref_Inferior.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Ref_Cortante_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) _
        Handles Ref_Cortante.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Tabla_Resultados_Flexion_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) _
        Handles Tabla_Resultados_Flexion.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Tabla_Resultados_Cortante_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) _
        Handles Tabla_Resultados_Cortante.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Tabla_Demandas_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) _
        Handles Tabla_Demandas.CellPainting
        ' Tabla_Demandas tiene 1 columna por tramo (no 3), no aplica separador de vano
    End Sub

    Private Sub LlenarDemandas()
        For col As Integer = 0 To _framesActuales.Count - 1
            Dim fn = _framesActuales(col)
            Tabla_Demandas.Rows(FILA_BW).Cells(col).Value = fn.Bw.ToString("F3")
            Tabla_Demandas.Rows(FILA_H).Cells(col).Value = fn.H.ToString("F3")
            Tabla_Demandas.Rows(FILA_L).Cells(col).Value = fn.Longitud.ToString("F2")
            Tabla_Demandas.Rows(FILA_ESECT).Cells(col).Value = If(fn.EsSeccionT, "S", "N")
            Tabla_Demandas.Rows(FILA_EJE_I).Cells(col).Value = If(String.IsNullOrWhiteSpace(fn.EjeApoyo_I), "—", fn.EjeApoyo_I)
            Tabla_Demandas.Rows(FILA_EJE_D).Cells(col).Value = If(String.IsNullOrWhiteSpace(fn.EjeApoyo_D), "—", fn.EjeApoyo_D)
            LlenarDemandasColumna(col)
        Next
    End Sub

    Private Sub Tabla_Demandas_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Tabla_Demandas.CellValueChanged
        If _cargando OrElse _framesActuales Is Nothing Then Return
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 OrElse e.ColumnIndex >= _framesActuales.Count Then Return

        Dim fn = _framesActuales(e.ColumnIndex)
        Select Case e.RowIndex
            Case FILA_BAPO_I
                Dim valI As Double = 0
                Double.TryParse(Tabla_Demandas.Rows(FILA_BAPO_I).Cells(e.ColumnIndex).Value?.ToString(),
                                Globalization.NumberStyles.Any,
                                Globalization.CultureInfo.InvariantCulture, valI)
                fn.B_Apoyo_I = Math.Max(0, valI)
                RecalcularDemandas(fn)
                _cargando = True
                LlenarDemandasColumna(e.ColumnIndex)
                LlenarResultadosColumna(e.ColumnIndex)
                _cargando = False

            Case FILA_BAPO_D
                Dim valD As Double = 0
                Double.TryParse(Tabla_Demandas.Rows(FILA_BAPO_D).Cells(e.ColumnIndex).Value?.ToString(),
                                Globalization.NumberStyles.Any,
                                Globalization.CultureInfo.InvariantCulture, valD)
                fn.B_Apoyo_D = Math.Max(0, valD)
                RecalcularDemandas(fn)
                _cargando = True
                LlenarDemandasColumna(e.ColumnIndex)
                LlenarResultadosColumna(e.ColumnIndex)
                _cargando = False

            Case FILA_ESECT
                Dim v = Tabla_Demandas.Rows(FILA_ESECT).Cells(e.ColumnIndex).Value?.ToString().ToUpperInvariant()
                fn.EsSeccionT = (v = "S" OrElse v = "SI" OrElse v = "Y" OrElse v = "TRUE")
                fn.Ref_Modificado = True
                RecalcularFrame(fn)
                _cargando = True
                LlenarResultadosColumna(e.ColumnIndex)
                _cargando = False
                DibujarPlanta()
            Case FILA_EJE_I
                fn.EjeApoyo_I = Tabla_Demandas.Rows(FILA_EJE_I).Cells(e.ColumnIndex).Value?.ToString().Trim()
                DibujarPlanta()
            Case FILA_EJE_D
                fn.EjeApoyo_D = Tabla_Demandas.Rows(FILA_EJE_D).Cells(e.ColumnIndex).Value?.ToString().Trim()
                DibujarPlanta()
        End Select
    End Sub

    Private Sub LlenarResultados()
        For fi As Integer = 0 To _framesActuales.Count - 1
            LlenarResultadosColumna(fi)
        Next
    End Sub

    Private Shared Sub ColorearCD(cell As DataGridViewCell, cd As Double)
        If cd >= 99.0 Then
            cell.Style.BackColor = Color.Empty
            cell.Style.ForeColor = Color.DimGray
        ElseIf cd >= 1.0 Then
            cell.Style.BackColor = Color.FromArgb(200, 240, 200)
            cell.Style.ForeColor = Color.DarkGreen
        ElseIf cd >= 0.9 Then
            cell.Style.BackColor = Color.FromArgb(255, 240, 180)
            cell.Style.ForeColor = Color.DarkOrange
        Else
            cell.Style.BackColor = Color.FromArgb(255, 200, 200)
            cell.Style.ForeColor = Color.DarkRed
        End If
    End Sub

    Private Sub LimpiarTablas()
        Ref_Superior.Columns.Clear() : Ref_Superior.Rows.Clear()
        Ref_Inferior.Columns.Clear() : Ref_Inferior.Rows.Clear()
        Ref_Cortante.Columns.Clear() : Ref_Cortante.Rows.Clear()
        Tabla_Demandas.Columns.Clear() : Tabla_Demandas.Rows.Clear()
        Tabla_Resultados_Flexion.Columns.Clear() : Tabla_Resultados_Flexion.Rows.Clear()
        Tabla_Resultados_Cortante.Columns.Clear() : Tabla_Resultados_Cortante.Rows.Clear()
        Tabla_Frames_Nervio.Rows.Clear()
        Nombre_Nervio.Text = ""
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  CELDA CAMBIADA — guardar y recalcular
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Ref_Superior_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Ref_Superior.CellValueChanged
        If _cargando OrElse _framesActuales Is Nothing Then Return
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        GuardarRefuerzoSupYRecalcular(e.ColumnIndex)
    End Sub

    Private Sub Ref_Inferior_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Ref_Inferior.CellValueChanged
        If _cargando OrElse _framesActuales Is Nothing Then Return
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        GuardarRefuerzoInfYRecalcular(e.ColumnIndex)
    End Sub

    Private Sub Ref_Cortante_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Ref_Cortante.CellValueChanged
        If _cargando OrElse _framesActuales Is Nothing Then Return
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        GuardarCortanteYRecalcular(e.ColumnIndex)
    End Sub

    Private Sub Tabla_Resultados_Flexion_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Tabla_Resultados_Flexion.CellEndEdit
        If _framesActuales Is Nothing Then Return
        If e.ColumnIndex < 0 Then Return

        Dim col = e.ColumnIndex
        Dim fi = col \ 3
        Dim zona = col Mod 3
        If fi >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(fi)

        If e.RowIndex = FILA_RES_REDIST Then
            ' Redistrib M- : editable en Izq (zona=0) y Der (zona=2), ignorar Centro
            If zona = 1 Then Return
            Dim factor As Double = 0
            Double.TryParse(Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(col).Value?.ToString(),
                            Globalization.NumberStyles.Any,
                            Globalization.CultureInfo.InvariantCulture, factor)
            factor = Math.Min(Math.Max(factor, 0.0), 0.20)
            Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(col).Value = factor.ToString("F2")
            If zona = 0 Then fn.FactorRedist_I = factor Else fn.FactorRedist_D = factor

        ElseIf e.RowIndex = FILA_RES_REDIST_POS Then
            ' Redistrib M+ : editable solo en Centro (zona=1), ignorar Izq/Der
            If zona <> 1 Then Return
            Dim factor As Double = 0
            Double.TryParse(Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(col).Value?.ToString(),
                            Globalization.NumberStyles.Any,
                            Globalization.CultureInfo.InvariantCulture, factor)
            factor = Math.Min(Math.Max(factor, 0.0), 0.20)
            Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(col).Value = factor.ToString("F2")
            fn.FactorRedist_C = factor

        Else
            Return
        End If

        RecalcularFrame(fn)
        _cargando = True
        LlenarResultadosColumna(fi)
        _cargando = False
    End Sub

    Private Sub GuardarRefuerzoSupYRecalcular(col As Integer)
        GuardarZonaLongitudinalYRecalcular(Ref_Superior, col, Function(fn) fn.RefuerzoSuperior)
    End Sub

    Private Sub GuardarRefuerzoInfYRecalcular(col As Integer)
        GuardarZonaLongitudinalYRecalcular(Ref_Inferior, col, Function(fn) fn.RefuerzoInferior)
    End Sub

    ''' <summary>Lee la columna (calibre → cantidad, puede haber varios calibres combinados) y la
    ''' guarda en la zona correspondiente de la lista seleccionada (RefuerzoSuperior/Inferior).</summary>
    Private Sub GuardarZonaLongitudinalYRecalcular(dgv As DataGridView, col As Integer,
                                                    listaSelector As Func(Of cFrameNervio, List(Of cRefuerzoTramo)))
        Dim fi = col \ 3
        If fi >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(fi)
        Dim posicion = ObtenerPosicion(col Mod 3)
        Dim lista = listaSelector(fn)

        Dim barras As New Dictionary(Of String, Integer)
        For row As Integer = 0 To BarSizes.Length - 1
            Dim v As Integer = 0
            Integer.TryParse(dgv.Rows(row).Cells(col).Value?.ToString(), v)
            If v > 0 Then barras(BarSizes(row)) = v
        Next

        Dim tramo = lista.FirstOrDefault(Function(t) t.Posicion = posicion)
        If tramo Is Nothing Then
            tramo = New cRefuerzoTramo With {.Frame = fn.ObjectLabel, .Posicion = posicion}
            lista.Add(tramo)
        End If
        tramo.Barras = barras

        fn.Ref_Modificado = True
        RecalcularFrame(fn)
        _cargando = True
        ColorizarCeldasConValor(dgv, col)
        LlenarResultadosColumna(fi)
        _cargando = False
        DibujarPlanta()
    End Sub

    Private Sub GuardarCortanteYRecalcular(col As Integer)
        Dim fi = col \ 3
        If fi >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(fi)
        Dim posicion = ObtenerPosicion(col Mod 3)

        Dim tieneStr = Ref_Cortante.Rows(FILA_COR_TIENE).Cells(col).Value?.ToString().ToUpperInvariant()
        Dim tiene = (tieneStr = "S" OrElse tieneStr = "SI" OrElse tieneStr = "Y")

        Dim cal = Ref_Cortante.Rows(FILA_COR_CALIBRE).Cells(col).Value?.ToString()
        Dim numBarra As Integer = 3
        If Not String.IsNullOrWhiteSpace(cal) Then
            Integer.TryParse(cal.Trim().TrimStart("#"c), numBarra)
            If numBarra <= 0 Then numBarra = 3
        End If

        Dim ramas As Integer = 2
        Integer.TryParse(Ref_Cortante.Rows(FILA_COR_RAMAS).Cells(col).Value?.ToString(), ramas)
        ramas = Math.Max(1, ramas)

        Dim sep As Double = 0.15
        Double.TryParse(Ref_Cortante.Rows(FILA_COR_SEP).Cells(col).Value?.ToString(),
                        Globalization.NumberStyles.Any,
                        Globalization.CultureInfo.CurrentCulture, sep)
        If sep <= 0 Then sep = 0.15

        Dim zona = fn.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = posicion)
        If zona Is Nothing Then
            zona = New cRefuerzoTransversalZona With {.Posicion = posicion}
            fn.RefuerzoTransversal.Add(zona)
        End If
        zona.NumeroBarra = numBarra
        zona.CantEstribos = If(tiene, ramas, 0)
        zona.Separacion = sep

        fn.Ref_Modificado = True
        RecalcularFrame(fn)
        _cargando = True
        LlenarResultadosColumna(fi)
        _cargando = False
        DibujarPlanta()
    End Sub

    ''' <summary>
    ''' Re-interpola las fuerzas en cara de apoyo para el frame dado usando los
    ''' B_Apoyo_I/D actuales y actualiza las demandas Mu/Vu del frame.
    ''' Se llama cuando el usuario edita manualmente b_apoyo en la tabla.
    ''' </summary>
    Private Sub RecalcularDemandas(fn As cFrameNervio)
        Dim nerv = Proyecto.Elementos.Nervios
        Dim combosSet As New HashSet(Of String)(
            nerv.ListA_Combinaciones_Design.Select(Function(c) NormalizarClaveCombo(c)))

        Dim d_m = fn.H - fn.Recubrimiento  ' profundidad efectiva (m)

        ' Re-interpolar fuerzas en cara para cada combo de este frame
        For Each combo In fn.Combinaciones
            If Not combosSet.Contains(combo.Nombre) Then Continue For
            _svc.CalcularFuerzasCaraApoyo(combo, fn.B_Apoyo_I, fn.B_Apoyo_D, d_m)
        Next

        ' Recalcular demandas máximas
        Dim combosDiseno = fn.Combinaciones.Where(Function(c) combosSet.Contains(c.Nombre)).ToList()
        If combosDiseno.Count > 0 Then
            fn.Mu_Neg_I = combosDiseno.Max(Function(c) Math.Abs(Math.Min(c.M_Cara_I, 0)))
            fn.Mu_Pos_C = combosDiseno.Max(Function(c) c.M_Max_Pos)
            fn.Mu_Neg_D = combosDiseno.Max(Function(c) Math.Abs(Math.Min(c.M_Cara_D, 0)))
            fn.Vu_I = combosDiseno.Max(Function(c) Math.Abs(c.V_d_I))
            fn.Vu_D = combosDiseno.Max(Function(c) Math.Abs(c.V_d_D))
            ' Actualizar bases para redistribución (b_apoyo cambió, recalcula bases)
            NervioService.GuardarBasesMomentos(fn)
        End If

        ' Recalcular capacidad con las nuevas demandas
        RecalcularFrame(fn)
    End Sub

    ''' <summary>Actualiza filas de demandas Mu/Vu/b_apoyo/redistribución en la columna indicada.</summary>
    Private Sub LlenarDemandasColumna(col As Integer)
        If col >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(col)
        Tabla_Demandas.Rows(FILA_BAPO_I).Cells(col).Value = fn.B_Apoyo_I.ToString("F3")
        Tabla_Demandas.Rows(FILA_BAPO_D).Cells(col).Value = fn.B_Apoyo_D.ToString("F3")
        Tabla_Demandas.Rows(FILA_MUI).Cells(col).Value = fn.Mu_Neg_I.ToString("F2")
        Tabla_Demandas.Rows(FILA_MUC).Cells(col).Value = fn.Mu_Pos_C.ToString("F2")
        Tabla_Demandas.Rows(FILA_MUD).Cells(col).Value = fn.Mu_Neg_D.ToString("F2")
        Tabla_Demandas.Rows(FILA_VUI).Cells(col).Value = fn.Vu_I.ToString("F2")
        Tabla_Demandas.Rows(FILA_VUD).Cells(col).Value = fn.Vu_D.ToString("F2")
        ' Colorear b_apoyo: naranja si está en 0 (posible fallo de detección)
        Dim colorI = If(fn.B_Apoyo_I < 0.001, Color.FromArgb(255, 220, 180), Color.FromArgb(255, 250, 210))
        Dim colorD = If(fn.B_Apoyo_D < 0.001, Color.FromArgb(255, 220, 180), Color.FromArgb(255, 250, 210))
        Tabla_Demandas.Rows(FILA_BAPO_I).Cells(col).Style.BackColor = colorI
        Tabla_Demandas.Rows(FILA_BAPO_D).Cells(col).Style.BackColor = colorD
    End Sub

    Private Sub RecalcularFrame(fn As cFrameNervio)
        If _nervioActual IsNot Nothing Then
            fn.Tf = _nervioActual.Tf_Losa
            fn.Paso = _nervioActual.Paso_Nervios
            If fn.EsSeccionT Then
                fn.Be = _svc.CalcularBe(fn.Bw, fn.Tf, fn.Paso, fn.Longitud, fn.B_Apoyo_I, fn.B_Apoyo_D)
            End If
        End If
        ' Recubrimiento global del módulo si está definido
        Dim recG = Proyecto.Elementos.Nervios.Recubrimiento
        If recG > 0 Then fn.Recubrimiento = recG

        ' Aplicar redistribución si hay factores activos
        If fn.FactorRedist_I > 0 OrElse fn.FactorRedist_D > 0 OrElse fn.FactorRedist_C > 0 Then
            _svc.AplicarRedistribucionNervio(fn)
        End If

        _svc.CalcularFlexion(fn)
        _svc.CalcularCortante(fn)

        ' Cumple: C/D Rel M ≥ 0.9 en todas las zonas con demanda
        fn.Cumple = fn.CD_M_Sup_I >= 0.9 AndAlso
                    fn.CD_M_Inf_C >= 0.9 AndAlso
                    fn.CD_M_Sup_D >= 0.9 AndAlso
                    fn.CD_Cortante_I >= 0.9 AndAlso
                    fn.CD_Cortante_D >= 0.9
    End Sub

    ''' <summary>Escribe las 3 columnas (Izq/Centro/Der) de resultados del tramo fi.</summary>
    Private Sub LlenarResultadosColumna(fi As Integer)
        If fi >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(fi)
        Dim colIzq = fi * 3 + 0
        Dim colCentro = fi * 3 + 1
        Dim colDer = fi * 3 + 2

        ' Filas 0-11 muestran valores BASE ("como estaba"); fallback a actuales si bases aún no calculadas.
        Dim muNegI_b = If(fn.Mu_Neg_I_Base > 0.001, fn.Mu_Neg_I_Base, fn.Mu_Neg_I)
        Dim muPosC_b = If(fn.Mu_Pos_C_Base > 0.001, fn.Mu_Pos_C_Base, fn.Mu_Pos_C)
        Dim muNegD_b = If(fn.Mu_Neg_D_Base > 0.001, fn.Mu_Neg_D_Base, fn.Mu_Neg_D)
        Dim cdNegI_b = If(fn.CD_M_Sup_I_Base > 0.0, fn.CD_M_Sup_I_Base, fn.CD_M_Sup_I)
        Dim cdPosC_b = If(fn.CD_M_Inf_C_Base > 0.0, fn.CD_M_Inf_C_Base, fn.CD_M_Inf_C)
        Dim cdNegD_b = If(fn.CD_M_Sup_D_Base > 0.0, fn.CD_M_Sup_D_Base, fn.CD_M_Sup_D)

        ' Izquierda — zona negativa (M-I, refuerzo superior)
        EscribirColumnaFlexionNeg(colIzq, muNegI_b, fn.As_Min, fn.As_Prov_Sup_I, fn.PhiMn_Sup_I,
                                   fn.CD_As_Sup_I, cdNegI_b)
        ' Centro — zona positiva (M+C, refuerzo inferior)
        Dim beStr = If(fn.EsSeccionT, $" Be={fn.Be:F3}m", "")
        EscribirColumnaFlexionPos(colCentro, muPosC_b, fn.As_Min_Pos, fn.As_Prov_Inf_C, fn.PhiMn_Inf_C,
                                   fn.CD_As_Inf_C, cdPosC_b, beStr)
        ' Derecha — zona negativa (M-D, refuerzo superior)
        EscribirColumnaFlexionNeg(colDer, muNegD_b, fn.As_Min, fn.As_Prov_Sup_D, fn.PhiMn_Sup_D,
                                   fn.CD_As_Sup_D, cdNegD_b)

        EscribirColumnaCortante(colIzq, fn.Vu_I, fn.PhiVc_I, fn.PhiVs_I, fn.PhiVn_I, fn.CD_Cortante_I, fn.Cumple)
        EscribirColumnaCortante(colCentro, Nothing, Nothing, Nothing, Nothing, Nothing, fn.Cumple)
        EscribirColumnaCortante(colDer, fn.Vu_D, fn.PhiVc_D, fn.PhiVs_D, fn.PhiVn_D, fn.CD_Cortante_D, fn.Cumple)

        ' Fila 12: Redistrib M- (editable Izq/Der, bloqueado Centro)
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(colIzq).Value = fn.FactorRedist_I.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(colIzq).ReadOnly = False
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(colCentro).Value = "—"
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(colCentro).ReadOnly = True
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(colDer).Value = fn.FactorRedist_D.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST).Cells(colDer).ReadOnly = False

        ' Fila 13: Redistrib M+ (bloqueado Izq/Der, editable Centro)
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(colIzq).Value = "—"
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(colIzq).ReadOnly = True
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(colCentro).Value = fn.FactorRedist_C.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(colCentro).ReadOnly = False
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(colDer).Value = "—"
        Tabla_Resultados_Flexion.Rows(FILA_RES_REDIST_POS).Cells(colDer).ReadOnly = True

        ' Filas 14-15: C/D post-redistribución ("como quedaría") — visibles solo si hay algún factor activo
        Dim hayRedist = (fn.FactorRedist_I > 0 OrElse fn.FactorRedist_D > 0 OrElse fn.FactorRedist_C > 0)
        ' colIzq: neg zone → fila 14 activa, fila 15 = "—"
        EscribirCDFilaRedist(colIzq, FILA_RES_CD_M_NEG_REDIST, fn.CD_M_Sup_I, hayRedist)
        EscribirCDFilaRedist(colIzq, FILA_RES_CD_M_POS_REDIST, 99.0, False)
        ' colCentro: pos zone → fila 15 activa, fila 14 = "—"
        EscribirCDFilaRedist(colCentro, FILA_RES_CD_M_NEG_REDIST, 99.0, False)
        EscribirCDFilaRedist(colCentro, FILA_RES_CD_M_POS_REDIST, fn.CD_M_Inf_C, hayRedist)
        ' colDer: neg zone → fila 14 activa, fila 15 = "—"
        EscribirCDFilaRedist(colDer, FILA_RES_CD_M_NEG_REDIST, fn.CD_M_Sup_D, hayRedist)
        EscribirCDFilaRedist(colDer, FILA_RES_CD_M_POS_REDIST, 99.0, False)
    End Sub

    ''' <summary>Escribe un valor C/D en una fila específica de la tabla de redistribución.
    ''' Si showRedist=False o cd≥99, escribe "—" con fondo gris (zona no activa).</summary>
    Private Sub EscribirCDFilaRedist(col As Integer, fila As Integer, cd As Double, showRedist As Boolean)
        Dim cell = Tabla_Resultados_Flexion.Rows(fila).Cells(col)
        If showRedist AndAlso cd < 99.0 Then
            cell.Value = cd.ToString("F2")
            ColorearCD(cell, cd)
        Else
            cell.Value = "—"
            cell.Style.BackColor = Color.FromArgb(245, 245, 245)
            cell.Style.ForeColor = Color.DimGray
        End If
    End Sub

    ''' <summary>Escribe las 12 filas de flexión para una zona negativa (apoyos).</summary>
    Private Sub EscribirColumnaFlexionNeg(col As Integer,
                                           mu As Double, asMin As Double, asProv As Double,
                                           phiMn As Double, cdAs As Double, cdM As Double)
        Dim dash = "—"
        Tabla_Resultados_Flexion.Rows(FILA_RES_MU_NEG).Cells(col).Value = If(mu > 0.001, mu.ToString("F2"), dash)
        Tabla_Resultados_Flexion.Rows(FILA_RES_MU_POS).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN_NEG).Cells(col).Value = asMin.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN_POS).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV_NEG).Cells(col).Value = asProv.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV_POS).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN_NEG).Cells(col).Value = If(phiMn > 0, phiMn.ToString("F1"), dash)
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN_POS).Cells(col).Value = dash
        EscribirCDFlexion(col, FILA_RES_CD_AS_NEG, FILA_RES_CD_AS_POS, cdAs)
        EscribirCDFlexion(col, FILA_RES_CD_M_NEG, FILA_RES_CD_M_POS, cdM)
        ' Colorear filas de demanda según si hay demanda
        Dim bgSin = Color.FromArgb(245, 245, 245)
        Tabla_Resultados_Flexion.Rows(FILA_RES_MU_POS).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN_POS).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV_POS).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN_POS).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_CD_AS_POS).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_CD_M_POS).Cells(col).Style.BackColor = bgSin
    End Sub

    ''' <summary>Escribe las 12 filas de flexión para una zona positiva (vano).</summary>
    Private Sub EscribirColumnaFlexionPos(col As Integer,
                                           mu As Double, asMin As Double, asProv As Double,
                                           phiMn As Double, cdAs As Double, cdM As Double,
                                           beInfo As String)
        Dim dash = "—"
        Tabla_Resultados_Flexion.Rows(FILA_RES_MU_NEG).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_MU_POS).Cells(col).Value = If(mu > 0.001, mu.ToString("F2"), dash)
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN_NEG).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN_POS).Cells(col).Value = asMin.ToString("F2") & beInfo
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV_NEG).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV_POS).Cells(col).Value = asProv.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN_NEG).Cells(col).Value = dash
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN_POS).Cells(col).Value = If(phiMn > 0, phiMn.ToString("F1"), dash)
        ' POS es la zona activa → filaActiva=POS, filaOtra=NEG
        EscribirCDFlexion(col, FILA_RES_CD_AS_POS, FILA_RES_CD_AS_NEG, cdAs)
        EscribirCDFlexion(col, FILA_RES_CD_M_POS, FILA_RES_CD_M_NEG, cdM)
        Dim bgSin = Color.FromArgb(245, 245, 245)
        Tabla_Resultados_Flexion.Rows(FILA_RES_MU_NEG).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN_NEG).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV_NEG).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN_NEG).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_CD_AS_NEG).Cells(col).Style.BackColor = bgSin
        Tabla_Resultados_Flexion.Rows(FILA_RES_CD_M_NEG).Cells(col).Style.BackColor = bgSin
    End Sub

    ''' <summary>Escribe el C/D en la fila activa y "—" en la otra.</summary>
    Private Sub EscribirCDFlexion(col As Integer, filaActiva As Integer, filaOtra As Integer, cd As Double)
        Tabla_Resultados_Flexion.Rows(filaOtra).Cells(col).Value = "—"
        Tabla_Resultados_Flexion.Rows(filaOtra).Cells(col).Style.BackColor = Color.FromArgb(245, 245, 245)
        Tabla_Resultados_Flexion.Rows(filaActiva).Cells(col).Value = If(cd >= 99.0, "—", cd.ToString("F2"))
        ColorearCD(Tabla_Resultados_Flexion.Rows(filaActiva).Cells(col), cd)
    End Sub

    Private Sub EscribirColumnaCortante(col As Integer,
                                         vu As Double?, phiVc As Double?, phiVs As Double?,
                                         phiVn As Double?, cd As Double?, cumple As Boolean)
        Dim dash = "—"
        Tabla_Resultados_Cortante.Rows(FILA_RES_VU).Cells(col).Value = If(vu.HasValue, vu.Value.ToString("F1"), dash)
        Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVC).Cells(col).Value = If(phiVc.HasValue, phiVc.Value.ToString("F1"), dash)
        Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVS).Cells(col).Value = If(phiVs.HasValue, phiVs.Value.ToString("F1"), dash)
        Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVN).Cells(col).Value = If(phiVn.HasValue, phiVn.Value.ToString("F1"), dash)
        Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col).Value = If(cd.HasValue, cd.Value.ToString("F2"), dash)
        If cd.HasValue Then
            ColorearCD(Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col), cd.Value)
        Else
            Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col).Style.BackColor = Color.FromArgb(245, 245, 245)
            Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col).Style.ForeColor = Color.DimGray
        End If
        Dim cumpleCell = Tabla_Resultados_Cortante.Rows(FILA_RES_CUMPLE).Cells(col)
        cumpleCell.Value = If(cumple, "✓ Cumple", "✗ No cumple")
        cumpleCell.Style.BackColor = If(cumple, Color.FromArgb(200, 240, 200), Color.FromArgb(255, 200, 200))
        cumpleCell.Style.ForeColor = If(cumple, Color.DarkGreen, Color.DarkRed)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  TABLA DE NAVEGACIÓN — Tabla_Nervios + Tabla_Frames_Nervio (TabPage5)
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ConstruirTablasNavegacion()
        ' Tabla_Nervios — columnas fijas, una fila por nervio del piso
        Tabla_Nervios.Columns.Clear()
        Tabla_Nervios.Rows.Clear()
        Tabla_Nervios.Columns.Add("Nombre", "Nombre")
        Tabla_Nervios.Columns.Add("Piso", "Piso")
        Tabla_Nervios.Columns.Add("Tramos", "Tramos")
        Tabla_Nervios.Columns.Add("Long_m", "Long (m)")
        Tabla_Nervios.Columns.Add("Estado", "Estado")
        Tabla_Nervios.Columns.Add("Tipo", "Tipo")
        Tabla_Nervios.Columns.Add("Rige", "Rige")
        For Each c As DataGridViewColumn In Tabla_Nervios.Columns
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        Tabla_Nervios.Columns(0).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        Tabla_Nervios.Columns("Tipo").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft

        ' Tabla_Frames_Nervio — columnas fijas, una fila por frame del nervio seleccionado
        Tabla_Frames_Nervio.Columns.Clear()
        Tabla_Frames_Nervio.Rows.Clear()
        Tabla_Frames_Nervio.Columns.Add("Frame", "Frame")
        Tabla_Frames_Nervio.Columns.Add("Seccion", "Sección")
        Tabla_Frames_Nervio.Columns.Add("Bw", "Bw (m)")
        Tabla_Frames_Nervio.Columns.Add("H", "H (m)")
        Tabla_Frames_Nervio.Columns.Add("L", "L (m)")
        Tabla_Frames_Nervio.Columns.Add("bI", "b_I (m)")
        Tabla_Frames_Nervio.Columns.Add("bD", "b_D (m)")
        Tabla_Frames_Nervio.Columns.Add("EjeI", "Eje I")
        Tabla_Frames_Nervio.Columns.Add("EjeD", "Eje D")
        Tabla_Frames_Nervio.Columns.Add("CD", "C/D mín")
        For Each c As DataGridViewColumn In Tabla_Frames_Nervio.Columns
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        Tabla_Frames_Nervio.Columns(0).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        Tabla_Frames_Nervio.Columns(1).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
    End Sub

    Private Sub LlenarTablaNervios()
        If Tabla_Nervios.Columns.Count = 0 Then ConstruirTablasNavegacion()
        If Proyecto Is Nothing Then Return

        Dim pisoSel = If(Lista_Pisos.SelectedItem?.ToString(), "")
        Dim nerv = Proyecto.Elementos.Nervios
        Dim lista = If(String.IsNullOrEmpty(pisoSel),
                       nerv.Elementos,
                       nerv.Elementos.Where(Function(n) n.Piso = pisoSel).ToList())

        ' Calcular qué nervio "rige" en cada familia Patrón
        Dim rigePorFamilia As New Dictionary(Of String, cNervio)(StringComparer.OrdinalIgnoreCase)
        For Each n In Proyecto.Elementos.Nervios.Elementos
            If n.EsPatron Then
                Dim familia = Proyecto.Elementos.Nervios.Elementos.
                    Where(Function(x) x.EsPatron AndAlso ReferenceEquals(x, n) OrElse
                                      Not String.IsNullOrEmpty(x.PatronRef) AndAlso
                                      x.PatronRef.Equals(n.Nombre, StringComparison.OrdinalIgnoreCase)).ToList()
                Dim conCalculo = familia.Where(Function(x) x.Frames.Any(Function(f) f.Ref_Modificado)).ToList()
                If conCalculo.Count > 0 Then
                    Dim gobernante = conCalculo.OrderBy(Function(x)
                        Dim vals() As Double = x.Frames.Where(Function(f) f.Ref_Modificado).
                            SelectMany(Function(f) {f.CD_Flex_Sup_I, f.CD_Flex_Inf_C, f.CD_Flex_Sup_D,
                                                    f.CD_Cortante_I, f.CD_Cortante_D}).
                            Where(Function(v) v > 0).DefaultIfEmpty(99.0).ToArray()
                        Return vals.Min()
                    End Function).First()
                    rigePorFamilia(n.Nombre) = gobernante
                End If
            End If
        Next

        _cargando = True
        Try
            Tabla_Nervios.Rows.Clear()
            For Each n In lista
                Dim longTotal = n.Frames.Sum(Function(f) f.Longitud)
                Dim tieneCalculo = n.Frames.Any(Function(f) f.Ref_Modificado)
                Dim todoCumple = tieneCalculo AndAlso n.Frames.All(Function(f) f.Cumple)
                Dim estadoTxt = If(tieneCalculo, If(todoCumple, "✓ Cumple", "✗ Falla"), "—")

                Dim tipoTxt As String = "—"
                If n.EsPatron Then
                    tipoTxt = "PATRÓN"
                ElseIf Not String.IsNullOrEmpty(n.PatronRef) Then
                    tipoTxt = "Similar: " & n.PatronRef
                End If

                Dim rigeTxt As String = ""
                Dim familiaRef = If(n.EsPatron, n.Nombre,
                                    If(Not String.IsNullOrEmpty(n.PatronRef), n.PatronRef, ""))
                If Not String.IsNullOrEmpty(familiaRef) AndAlso
                   rigePorFamilia.ContainsKey(familiaRef) AndAlso
                   ReferenceEquals(rigePorFamilia(familiaRef), n) Then
                    rigeTxt = "★"
                End If

                Dim r = Tabla_Nervios.Rows.Add(
                    If(Not String.IsNullOrWhiteSpace(n.NombrePlano), n.NombrePlano, n.Nombre),
                    n.Piso,
                    n.Frames.Count,
                    longTotal.ToString("F2"),
                    estadoTxt,
                    tipoTxt,
                    rigeTxt)

                Tabla_Nervios.Rows(r).Tag = n

                Dim cellEstado = Tabla_Nervios.Rows(r).Cells(4)
                If tieneCalculo Then
                    cellEstado.Style.BackColor = If(todoCumple,
                        Color.FromArgb(200, 240, 200), Color.FromArgb(255, 200, 200))
                    cellEstado.Style.ForeColor = If(todoCumple, Color.DarkGreen, Color.DarkRed)
                Else
                    cellEstado.Style.BackColor = Color.Empty
                    cellEstado.Style.ForeColor = Color.DimGray
                End If

                ' Estilo fila patrón: fondo azul muy suave
                If n.EsPatron Then
                    Tabla_Nervios.Rows(r).DefaultCellStyle.BackColor = Color.FromArgb(225, 235, 255)
                    Tabla_Nervios.Rows(r).DefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
                ElseIf Not String.IsNullOrEmpty(n.PatronRef) Then
                    Tabla_Nervios.Rows(r).DefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255)
                End If

                If rigeTxt = "★" Then
                    Tabla_Nervios.Rows(r).Cells(6).Style.ForeColor = Color.FromArgb(200, 100, 0)
                    Tabla_Nervios.Rows(r).Cells(6).Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                End If
            Next

            ' Restaurar selección en la fila del nervio activo
            If _nervioActual IsNot Nothing Then
                For Each row As DataGridViewRow In Tabla_Nervios.Rows
                    If ReferenceEquals(row.Tag, _nervioActual) Then
                        row.Selected = True
                        Exit For
                    End If
                Next
            End If
        Finally
            _cargando = False
        End Try
    End Sub

    Private Sub LlenarTablaFrames(nervio As cNervio)
        If Tabla_Frames_Nervio.Columns.Count = 0 Then ConstruirTablasNavegacion()
        Tabla_Frames_Nervio.Rows.Clear()
        If nervio Is Nothing Then Return

        For Each fn In nervio.Frames
            Dim cdStr As String = "—"
            If fn.Ref_Modificado Then
                Dim vals() As Double = {fn.CD_Flex_Sup_I, fn.CD_Flex_Inf_C, fn.CD_Flex_Sup_D,
                                        fn.CD_Cortante_I, fn.CD_Cortante_D}
                Dim cdMin = vals.Where(Function(v) v > 0).DefaultIfEmpty(0).Min()
                cdStr = cdMin.ToString("F2")
            End If

            Dim r = Tabla_Frames_Nervio.Rows.Add(
                fn.ObjectLabel,
                fn.NombreSeccion,
                fn.Bw.ToString("F3"),
                fn.H.ToString("F3"),
                fn.Longitud.ToString("F2"),
                fn.B_Apoyo_I.ToString("F3"),
                fn.B_Apoyo_D.ToString("F3"),
                If(String.IsNullOrWhiteSpace(fn.EjeApoyo_I), "—", fn.EjeApoyo_I),
                If(String.IsNullOrWhiteSpace(fn.EjeApoyo_D), "—", fn.EjeApoyo_D),
                cdStr)

            If fn.Ref_Modificado AndAlso cdStr <> "—" Then
                Dim cdVal As Double = 0
                Double.TryParse(cdStr, cdVal)
                ColorearCD(Tabla_Frames_Nervio.Rows(r).Cells(9), cdVal)
            End If
        Next
    End Sub

    Private Sub Tabla_Nervios_SelectionChanged(sender As Object, e As EventArgs) _
        Handles Tabla_Nervios.SelectionChanged
        If _cargando Then Return
        If Tabla_Nervios.SelectedRows.Count = 0 Then Return
        Dim nervio = TryCast(Tabla_Nervios.SelectedRows(0).Tag, cNervio)
        If nervio Is Nothing OrElse ReferenceEquals(nervio, _nervioActual) Then Return

        ' Sync Lista_Nervios without triggering its handler
        Dim src = TryCast(Lista_Nervios.DataSource, List(Of cNervio))
        If src IsNot Nothing Then
            Dim idx = src.IndexOf(nervio)
            If idx >= 0 Then
                _cargando = True
                Lista_Nervios.SelectedIndex = idx
                _cargando = False
            End If
        End If

        CargarNervioCompleto(nervio)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  DIAGRAMA DE PLANTA (GDI+)
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub DibujarPlanta()
        If Proyecto Is Nothing OrElse _joints Is Nothing Then Return

        Dim pisoSel = Lista_Pisos.SelectedItem?.ToString()
        Dim nerv = Proyecto.Elementos.Nervios
        Dim nerviosPiso = nerv.Elementos _
            .Where(Function(n) String.IsNullOrEmpty(pisoSel) OrElse n.Piso = pisoSel).ToList()

        Dim bmp As New Bitmap(Math.Max(PicPlanta.Width, 100), Math.Max(PicPlanta.Height, 100))
        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.FromArgb(245, 248, 252))
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

            If nerviosPiso.Count = 0 Then
                g.DrawString("No hay nervios para el piso seleccionado.",
                             New Font("Segoe UI", 10), Brushes.Gray, 20, 20)
            Else
                DibujarNerviosEnPlanta(g, nerviosPiso, bmp.Size)
            End If
        End Using

        Dim viejo = PicPlanta.Image
        PicPlanta.Image = bmp
        If viejo IsNot Nothing Then viejo.Dispose()
    End Sub

    Private Sub DibujarNerviosEnPlanta(g As Graphics, nervios As List(Of cNervio), sz As Size)
        Dim xs As New List(Of Double)
        Dim ys As New List(Of Double)
        For Each nerv In nervios
            For Each fn In nerv.Frames
                Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                If _joints.TryGetValue(fn.JointI, ji) Then xs.Add(ji.GlobalX) : ys.Add(ji.GlobalY)
                If _joints.TryGetValue(fn.JointJ, jj) Then xs.Add(jj.GlobalX) : ys.Add(jj.GlobalY)
            Next
        Next
        If xs.Count = 0 Then Return

        Dim margen = 30
        Dim xMin = xs.Min(), xMax = xs.Max()
        Dim yMin = ys.Min(), yMax = ys.Max()
        Dim dx = xMax - xMin, dy = yMax - yMin
        If dx < 0.01 Then dx = 1 : If dy < 0.01 Then dy = 1
        Dim escX = (sz.Width - 2 * margen) / dx
        Dim escY = (sz.Height - 2 * margen) / dy
        Dim esc = Math.Min(escX, escY)
        Dim Tx = Function(x As Double) CSng(margen + (x - xMin) * esc)
        Dim Ty = Function(y As Double) CSng(sz.Height - margen - (y - yMin) * esc)

        ' Fondo de estructura (frames del modelo propio de Nervios)
        Dim framesTodos = Proyecto.Elementos.Nervios.Frames _
            .Where(Function(f) nervios.Any(Function(n) n.Piso = f.Story)).ToList()
        Using penFondo As New Pen(Color.FromArgb(210, 210, 210), 1)
            For Each f In framesTodos
                Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                If _joints.TryGetValue(f.JointI, ji) AndAlso _joints.TryGetValue(f.JointJ, jj) Then
                    g.DrawLine(penFondo, Tx(ji.GlobalX), Ty(ji.GlobalY), Tx(jj.GlobalX), Ty(jj.GlobalY))
                End If
            Next
        End Using

        Dim fontLabel As New Font("Segoe UI", 7)
        Dim fontEje As New Font("Segoe UI", 7, FontStyle.Bold)
        For idx As Integer = 0 To nervios.Count - 1
            Dim nerv = nervios(idx)
            Dim color = ColoresNervio(idx Mod ColoresNervio.Length)
            Dim esSeleccionado = ReferenceEquals(nerv, _nervioActual)
            Dim grosor = If(esSeleccionado, 4.0F, 2.5F)

            If esSeleccionado Then
                Using penHalo As New Pen(Color.FromArgb(70, color), grosor + 5)
                    For Each fn In nerv.Frames
                        Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                        If Not _joints.TryGetValue(fn.JointI, ji) OrElse
                           Not _joints.TryGetValue(fn.JointJ, jj) Then Continue For
                        g.DrawLine(penHalo, Tx(ji.GlobalX), Ty(ji.GlobalY), Tx(jj.GlobalX), Ty(jj.GlobalY))
                    Next
                End Using
            End If

            Using penNervio As New Pen(color, grosor)
                For Each fn In nerv.Frames
                    Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                    If Not _joints.TryGetValue(fn.JointI, ji) OrElse
                       Not _joints.TryGetValue(fn.JointJ, jj) Then Continue For

                    Dim p1 = New PointF(Tx(ji.GlobalX), Ty(ji.GlobalY))
                    Dim p2 = New PointF(Tx(jj.GlobalX), Ty(jj.GlobalY))
                    g.DrawLine(penNervio, p1, p2)

                    If fn.Ref_Modificado Then
                        Dim cdMin = Math.Min(fn.CD_Flex_Inf_C, Math.Min(fn.CD_Cortante_I, fn.CD_Cortante_D))
                        Dim clrCD = If(cdMin >= 1.0, Color.FromArgb(0, 131, 0),
                                     If(cdMin >= 0.9, Color.FromArgb(237, 161, 0), Color.FromArgb(227, 73, 72)))
                        Dim xm = (p1.X + p2.X) / 2
                        Dim ym = (p1.Y + p2.Y) / 2
                        Using penCD As New Pen(Color.White, 1.5F), brushCD As New SolidBrush(clrCD)
                            g.FillEllipse(brushCD, xm - 5, ym - 5, 10, 10)
                            g.DrawEllipse(penCD, xm - 5, ym - 5, 10, 10)
                        End Using
                    End If

                    If ChkMostrarEtiquetas.Checked Then
                        Dim xm = (p1.X + p2.X) / 2
                        Dim ym = (p1.Y + p2.Y) / 2
                        DibujarEtiquetaConFondo(g, fn.ObjectLabel, fontLabel, color, xm + 4, ym - 16)
                    End If

                    If ChkMostrarApoyos.Checked Then
                        Using penApoyo As New Pen(Color.White, 1.2F), brushApoyo As New SolidBrush(Color.FromArgb(235, 104, 52))
                            If fn.B_Apoyo_I > 0 Then
                                g.FillEllipse(brushApoyo, p1.X - 4, p1.Y - 4, 8, 8)
                                g.DrawEllipse(penApoyo, p1.X - 4, p1.Y - 4, 8, 8)
                            End If
                            If fn.B_Apoyo_D > 0 Then
                                g.FillEllipse(brushApoyo, p2.X - 4, p2.Y - 4, 8, 8)
                                g.DrawEllipse(penApoyo, p2.X - 4, p2.Y - 4, 8, 8)
                            End If
                        End Using
                        If Not String.IsNullOrWhiteSpace(fn.EjeApoyo_I) Then
                            DibujarEtiquetaConFondo(g, fn.EjeApoyo_I, fontEje, Color.FromArgb(150, 90, 20), p1.X + 6, p1.Y + 4)
                        End If
                        If Not String.IsNullOrWhiteSpace(fn.EjeApoyo_D) Then
                            DibujarEtiquetaConFondo(g, fn.EjeApoyo_D, fontEje, Color.FromArgb(150, 90, 20), p2.X + 6, p2.Y + 4)
                        End If
                    End If
                Next
            End Using

            If nerv.Frames.Count > 0 Then
                Dim fn0 = nerv.Frames(0)
                Dim ji As cJoint = Nothing
                If _joints.TryGetValue(fn0.JointI, ji) Then
                    Using fontNombre As New Font("Segoe UI", 8, FontStyle.Bold)
                        DibujarEtiquetaConFondo(g, nerv.ToString(), fontNombre, color, Tx(ji.GlobalX), Ty(ji.GlobalY) - 16)
                    End Using
                End If
            End If
        Next
        fontLabel.Dispose()
        fontEje.Dispose()
    End Sub

    ''' <summary>Dibuja texto con una placa de fondo blanco translúcido detrás — legible sobre
    ''' cualquier elemento del plano, sea cual sea su color.</summary>
    Private Shared Sub DibujarEtiquetaConFondo(g As Graphics, texto As String, font As Font,
                                                colorTexto As Color, x As Single, y As Single)
        If String.IsNullOrEmpty(texto) Then Return
        Dim sz = g.MeasureString(texto, font)
        Using bBg As New SolidBrush(Color.FromArgb(215, Color.White))
            g.FillRectangle(bBg, x - 2, y - 1, sz.Width + 4, sz.Height + 2)
        End Using
        Using brTexto As New SolidBrush(colorTexto)
            g.DrawString(texto, font, brTexto, x, y)
        End Using
    End Sub

    Private Sub PicPlanta_Resize(sender As Object, e As EventArgs) Handles PicPlanta.Resize
        DibujarPlanta()
    End Sub

    Private Sub PicPlanta_MouseClick(sender As Object, e As MouseEventArgs) Handles PicPlanta.MouseClick
        Dim pisoSel = Lista_Pisos.SelectedItem?.ToString()
        Dim nerviosPiso = Proyecto.Elementos.Nervios.Elementos _
            .Where(Function(n) String.IsNullOrEmpty(pisoSel) OrElse n.Piso = pisoSel).ToList()
        If nerviosPiso.Count = 0 Then Return

        Dim xs As New List(Of Double), ys As New List(Of Double)
        For Each n In nerviosPiso
            For Each fn In n.Frames
                Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                If _joints.TryGetValue(fn.JointI, ji) Then xs.Add(ji.GlobalX) : ys.Add(ji.GlobalY)
                If _joints.TryGetValue(fn.JointJ, jj) Then xs.Add(jj.GlobalX) : ys.Add(jj.GlobalY)
            Next
        Next
        If xs.Count = 0 Then Return

        Dim margen = 30
        Dim xMin = xs.Min(), xMax = xs.Max(), yMin = ys.Min(), yMax = ys.Max()
        Dim dx = xMax - xMin, dy = yMax - yMin
        If dx < 0.01 Then dx = 1 : If dy < 0.01 Then dy = 1
        Dim escX = (PicPlanta.Width - 2 * margen) / dx
        Dim escY = (PicPlanta.Height - 2 * margen) / dy
        Dim esc = Math.Min(escX, escY)
        Dim Tx = Function(x As Double) CSng(margen + (x - xMin) * esc)
        Dim Ty = Function(y As Double) CSng(PicPlanta.Height - margen - (y - yMin) * esc)

        Dim mejor As cNervio = Nothing
        Dim menorDist As Single = 15

        For Each n In nerviosPiso
            For Each fn In n.Frames
                Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                If Not _joints.TryGetValue(fn.JointI, ji) OrElse Not _joints.TryGetValue(fn.JointJ, jj) Then Continue For
                Dim p1 = New PointF(Tx(ji.GlobalX), Ty(ji.GlobalY))
                Dim p2 = New PointF(Tx(jj.GlobalX), Ty(jj.GlobalY))
                Dim dist = DistanciaPuntoSegmento(e.X, e.Y, p1, p2)
                If dist < menorDist Then
                    menorDist = dist
                    mejor = n
                End If
            Next
        Next

        If mejor IsNot Nothing Then
            Dim idx = CType(Lista_Nervios.DataSource, List(Of cNervio)).IndexOf(mejor)
            If idx >= 0 Then
                Lista_Nervios.SelectedIndex = idx
            Else
                CargarNervioCompleto(mejor)
            End If
        End If
    End Sub

    Private Shared Function DistanciaPuntoSegmento(px As Single, py As Single,
                                                    p1 As PointF, p2 As PointF) As Single
        Dim dx = p2.X - p1.X, dy = p2.Y - p1.Y
        Dim t = ((px - p1.X) * dx + (py - p1.Y) * dy) / (dx * dx + dy * dy + 1.0E-9F)
        t = Math.Max(0, Math.Min(1, t))
        Dim cx = p1.X + t * dx, cy = p1.Y + t * dy
        Return CSng(Math.Sqrt((px - cx) ^ 2 + (py - cy) ^ 2))
    End Function

    Private Sub ChkMostrarEtiquetas_CheckedChanged(sender As Object, e As EventArgs) _
        Handles ChkMostrarEtiquetas.CheckedChanged
        DibujarPlanta()
    End Sub

    Private Sub ChkMostrarApoyos_CheckedChanged(sender As Object, e As EventArgs) _
        Handles ChkMostrarApoyos.CheckedChanged
        DibujarPlanta()
    End Sub

    Private Sub BtnVerPlantaAmpliada_Click(sender As Object, e As EventArgs) Handles BtnVerPlantaAmpliada.Click
        If Proyecto Is Nothing OrElse Proyecto.Elementos.Nervios.Elementos.Count = 0 Then
            MessageBox.Show("Primero importe y calcule los nervios.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        If _plantaAmpliada IsNot Nothing AndAlso Not _plantaAmpliada.IsDisposed Then
            _plantaAmpliada.ActualizarSeleccion(_nervioActual)
            _plantaAmpliada.Activate()
            Return
        End If

        Dim frm As New Form_PlantaInteractivaNervios()
        frm.Nervios = Proyecto.Elementos.Nervios.Elementos
        frm.Joints = _joints
        frm.GridLines = Proyecto.Elementos.Grids.GridLines
        frm.NervioSeleccionado = _nervioActual
        frm.PisoActual = If(Lista_Pisos.SelectedItem IsNot Nothing, Lista_Pisos.SelectedItem.ToString(), "")
        AddHandler frm.NervioSeleccionada, AddressOf PlantaAmpliada_NervioSeleccionada
        AddHandler frm.FormClosed, Sub(s, ev) _plantaAmpliada = Nothing
        _plantaAmpliada = frm
        frm.Show(Me)
    End Sub

    Private Sub PlantaAmpliada_NervioSeleccionada(nervio As cNervio)
        If _cargando Then Return
        If ReferenceEquals(nervio, _nervioActual) Then Return

        _cargando = True
        Try
            ' Sync floor selector if needed
            Dim pisoNervio = nervio.Piso
            Dim pisoCurrent = If(Lista_Pisos.SelectedItem IsNot Nothing, Lista_Pisos.SelectedItem.ToString(), "")
            If Not pisoNervio.Equals(pisoCurrent, StringComparison.OrdinalIgnoreCase) Then
                Dim idxPiso = Lista_Pisos.Items.IndexOf(pisoNervio)
                If idxPiso >= 0 Then Lista_Pisos.SelectedIndex = idxPiso
            End If

            ' Rebuild nervio list for the now-selected floor (suppressed via _cargando)
            Dim nerv = Proyecto.Elementos.Nervios
            Dim pisoSel = If(Lista_Pisos.SelectedItem?.ToString(), "")
            Dim lista = If(String.IsNullOrEmpty(pisoSel), nerv.Elementos,
                           nerv.Elementos.Where(Function(n) n.Piso = pisoSel).ToList())
            Lista_Nervios.DataSource = Nothing
            Lista_Nervios.DataSource = lista
            Lista_Nervios.DisplayMember = "NombrePlano"

            ' Select the target nervio by reference
            Dim idxNervio = lista.IndexOf(nervio)
            If idxNervio >= 0 Then Lista_Nervios.SelectedIndex = idxNervio
        Finally
            _cargando = False
        End Try

        LlenarTablaNervios()
        CargarNervioCompleto(nervio)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  EDICIÓN DE NERVIO
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub BtnRenombrar_Click(sender As Object, e As EventArgs) Handles BtnRenombrar.Click
        If _nervioActual Is Nothing Then Return
        Dim nombre = InputBox("Nuevo nombre para el nervio:", "Renombrar", _nervioActual.NombrePlano)
        If String.IsNullOrWhiteSpace(nombre) Then Return
        _nervioActual.NombrePlano = nombre
        _nervioActual.Nombre = nombre
        Nombre_Nervio.Text = nombre
        RefrescarListaNervios()
        DibujarPlanta()
    End Sub

    Private Sub BtnSeparar_Click(sender As Object, e As EventArgs) Handles BtnSeparar.Click
        If _nervioActual Is Nothing OrElse _nervioActual.Frames.Count <= 1 Then
            MessageBox.Show("Seleccione un nervio con más de un tramo.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using dlg As New Form_11_SepararNervio(_nervioActual)
            If dlg.ShowDialog() = DialogResult.OK Then
                Dim nuevoNervio = dlg.NuevoNervio
                If nuevoNervio IsNot Nothing AndAlso nuevoNervio.Frames.Count > 0 Then
                    Proyecto.Elementos.Nervios.Elementos.Add(nuevoNervio)
                    _cargando = True
                    RefrescarListaPisos()
                    RefrescarListaNervios()
                    _cargando = False
                    DibujarPlanta()
                End If
            End If
        End Using
    End Sub

    Private Sub BtnUnir_Click(sender As Object, e As EventArgs) Handles BtnUnir.Click
        If _nervioActual Is Nothing Then Return

        Dim pisoSel = _nervioActual.Piso
        Dim candidatos = Proyecto.Elementos.Nervios.Elementos.
            Where(Function(n) n.Piso.Equals(pisoSel, StringComparison.OrdinalIgnoreCase) AndAlso
                               Not ReferenceEquals(n, _nervioActual)).ToList()

        If candidatos.Count = 0 Then
            MessageBox.Show("No hay otros nervios en el mismo piso para unir.", "Sin candidatos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim seleccionado As cNervio = Nothing

        If candidatos.Count = 1 Then
            Dim r = MessageBox.Show($"¿Unir ""{_nervioActual}"" con ""{candidatos(0)}""?{Environment.NewLine}" &
                                    "Los tramos del segundo nervio pasarán al primero y el segundo será eliminado.",
                                    "Confirmar unión", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If r = DialogResult.Yes Then seleccionado = candidatos(0)
        Else
            seleccionado = ElegirNervioParaUnir(candidatos)
        End If

        If seleccionado Is Nothing Then Return

        For Each fn In seleccionado.Frames
            _nervioActual.Frames.Add(fn)
        Next
        Proyecto.Elementos.Nervios.Elementos.Remove(seleccionado)

        _cargando = True
        RefrescarListaPisos()
        RefrescarListaNervios()
        _cargando = False
        DibujarPlanta()
    End Sub

    Private Function ElegirNervioParaUnir(candidatos As List(Of cNervio)) As cNervio
        Dim result As cNervio = Nothing
        Dim frm As New Form With {
            .Text = "Elegir nervio a absorber",
            .Size = New Size(360, 320),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False
        }
        Dim lbl As New Label With {
            .Text = $"Seleccione el nervio que se unirá a ""{_nervioActual}"":" & Environment.NewLine &
                    "(sus tramos pasarán al nervio actual y será eliminado)",
            .Location = New Point(10, 10),
            .Size = New Size(330, 40),
            .AutoSize = False
        }
        Dim lst As New ListBox With {
            .Location = New Point(10, 56),
            .Size = New Size(330, 180),
            .SelectionMode = SelectionMode.One
        }
        For Each c In candidatos
            lst.Items.Add(c)
        Next
        lst.DisplayMember = "NombrePlano"
        If lst.Items.Count > 0 Then lst.SelectedIndex = 0

        Dim btnOk As New Button With {
            .Text = "Unir",
            .Location = New Point(10, 248),
            .Size = New Size(100, 28),
            .DialogResult = DialogResult.OK
        }
        Dim btnCancel As New Button With {
            .Text = "Cancelar",
            .Location = New Point(120, 248),
            .Size = New Size(100, 28),
            .DialogResult = DialogResult.Cancel
        }
        frm.Controls.AddRange(New Control() {lbl, lst, btnOk, btnCancel})
        frm.AcceptButton = btnOk
        frm.CancelButton = btnCancel

        If frm.ShowDialog(Me) = DialogResult.OK AndAlso lst.SelectedIndex >= 0 Then
            result = TryCast(lst.SelectedItem, cNervio)
        End If
        frm.Dispose()
        Return result
    End Function

    Private Sub BtnEliminar_Click(sender As Object, e As EventArgs) Handles BtnEliminar.Click
        If _nervioActual Is Nothing Then Return
        Dim nombre = If(Not String.IsNullOrWhiteSpace(_nervioActual.NombrePlano),
                        _nervioActual.NombrePlano, _nervioActual.Nombre)
        Dim r = MessageBox.Show($"¿Eliminar el nervio ""{nombre}""?{Environment.NewLine}" &
                                "Esta acción no se puede deshacer.",
                                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If r <> DialogResult.Yes Then Return

        Proyecto.Elementos.Nervios.Elementos.Remove(_nervioActual)
        _nervioActual = Nothing
        _framesActuales = Nothing
        _cargando = True
        RefrescarListaPisos()
        RefrescarListaNervios()
        _cargando = False
        DibujarPlanta()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  SISTEMA PATRÓN / SIMILAR
    ' ══════════════════════════════════════════════════════════════════════════

    ''' <summary>Actualiza LblInfoGrupo con el resumen del grupo y el tramo más solicitado.</summary>
    Private Sub ActualizarInfoGrupo(nervio As cNervio)
        If nervio Is Nothing Then
            LblInfoGrupo.Text = ""
            Return
        End If

        Dim nerv = Proyecto.Elementos.Nervios

        If nervio.EsPatron Then
            Dim similares = nerv.Elementos.Where(
                Function(n) Not String.IsNullOrEmpty(n.PatronRef) AndAlso
                            n.PatronRef.Equals(nervio.Nombre, StringComparison.OrdinalIgnoreCase)).ToList()

            ' Buscar tramo más solicitado en todo el grupo (Patrón + Similares)
            Dim grupo = similares.Concat({nervio}).ToList()
            Dim gobernante As (Frame As cFrameNervio, Nervio As cNervio) = Nothing
            Dim cdMin As Double = 99.0

            For Each ng In grupo
                For Each fn In ng.Frames
                    If Not fn.Ref_Modificado Then Continue For
                    Dim vals As New List(Of Double)()
                    If fn.Mu_Neg_I > 0.001 Then vals.Add(fn.CD_Flex_Sup_I)
                    If fn.Mu_Pos_C > 0.001 Then vals.Add(fn.CD_Flex_Inf_C)
                    If fn.Mu_Neg_D > 0.001 Then vals.Add(fn.CD_Flex_Sup_D)
                    If fn.Vu_I > 0.001 Then vals.Add(fn.CD_Cortante_I)
                    If fn.Vu_D > 0.001 Then vals.Add(fn.CD_Cortante_D)
                    If vals.Count = 0 Then Continue For
                    Dim cdFrame = vals.Min()
                    If cdFrame < cdMin Then
                        cdMin = cdFrame
                        gobernante = (fn, ng)
                    End If
                Next
            Next

            Dim nSim = similares.Count
            If gobernante.Frame IsNot Nothing Then
                Dim icon = If(cdMin >= 1.0, "✓", If(cdMin >= 0.9, "⚠", "✗"))
                LblInfoGrupo.Text = $"★ Patrón · {nSim} sim. · {icon} Crit: [{gobernante.Frame.ObjectLabel}] C/D={cdMin:F2}"
                LblInfoGrupo.ForeColor = If(cdMin >= 1.0,
                    Color.FromArgb(100, 255, 150),
                    If(cdMin >= 0.9, Color.FromArgb(255, 220, 100), Color.FromArgb(255, 120, 100)))
            Else
                LblInfoGrupo.Text = $"★ Patrón · {nSim} similares"
                LblInfoGrupo.ForeColor = Color.FromArgb(200, 200, 200)
            End If

        ElseIf Not String.IsNullOrEmpty(nervio.PatronRef) Then
            LblInfoGrupo.Text = $"Similar de: {nervio.PatronRef}"
            LblInfoGrupo.ForeColor = Color.FromArgb(180, 200, 255)
        Else
            LblInfoGrupo.Text = "— Independiente —"
            LblInfoGrupo.ForeColor = Color.FromArgb(160, 160, 160)
        End If
    End Sub

    Private Sub ActualizarCmbTipoNervio(nervio As cNervio)
        _cargando = True
        CmbTipoNervio.Items.Clear()
        CmbTipoNervio.Items.Add("— Independiente —")
        CmbTipoNervio.Items.Add("★  Es Patrón")

        Dim piso = nervio.Piso
        Dim patronesDisponibles = Proyecto.Elementos.Nervios.Elementos.
            Where(Function(n) n.EsPatron AndAlso
                              n.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase) AndAlso
                              Not ReferenceEquals(n, nervio)).ToList()
        For Each p In patronesDisponibles
            CmbTipoNervio.Items.Add("Similar a: " & p.Nombre)
        Next

        If nervio.EsPatron Then
            CmbTipoNervio.SelectedIndex = 1
        ElseIf Not String.IsNullOrEmpty(nervio.PatronRef) Then
            Dim idx = CmbTipoNervio.Items.IndexOf("Similar a: " & nervio.PatronRef)
            CmbTipoNervio.SelectedIndex = If(idx >= 0, idx, 0)
        Else
            CmbTipoNervio.SelectedIndex = 0
        End If
        _cargando = False
    End Sub

    Private Sub CmbTipoNervio_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles CmbTipoNervio.SelectedIndexChanged
        If _cargando OrElse _nervioActual Is Nothing OrElse CmbTipoNervio.SelectedIndex < 0 Then Return
        Dim txt = CmbTipoNervio.SelectedItem?.ToString()

        If CmbTipoNervio.SelectedIndex = 0 Then
            ' Independiente
            _nervioActual.EsPatron = False
            _nervioActual.PatronRef = ""
        ElseIf CmbTipoNervio.SelectedIndex = 1 Then
            ' Es Patrón
            _nervioActual.EsPatron = True
            _nervioActual.PatronRef = ""
        ElseIf txt IsNot Nothing AndAlso txt.StartsWith("Similar a: ") Then
            Dim patronNombre = txt.Substring("Similar a: ".Length)
            _nervioActual.EsPatron = False
            _nervioActual.PatronRef = patronNombre
            ' Copiar refuerzo del Patrón automáticamente
            Dim patron = Proyecto.Elementos.Nervios.Elementos.
                FirstOrDefault(Function(n) n.Nombre.Equals(patronNombre, StringComparison.OrdinalIgnoreCase))
            If patron IsNot Nothing Then CopiarRefuerzoDePatron(patron, _nervioActual)
        End If

        LlenarTablaNervios()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  REAGRUPACIÓN MANUAL DE FRAMES
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub BtnReagrupar_Click(sender As Object, e As EventArgs) Handles BtnReagrupar.Click
        If _nervioActual Is Nothing Then
            MessageBox.Show("Seleccione un nervio primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim nerv = Proyecto.Elementos.Nervios
        If nerv.Frames.Count = 0 Then
            MessageBox.Show("Primero importe las demandas ETABS.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Frames del mismo piso según el modelo propio de Nervios
        Dim pisoSel = _nervioActual.Piso
        Dim framesPiso = nerv.Frames.Where(Function(f) f.Story = pisoSel).ToList()

        ' Frames ocupados en OTROS nervios del mismo piso
        Dim framesOcupados As New HashSet(Of String)(
            nerv.Elementos.Where(Function(n) n.Piso = pisoSel AndAlso Not ReferenceEquals(n, _nervioActual)) _
                          .SelectMany(Function(n) n.Frames.Select(Function(f) f.ObjectLabel)),
            StringComparer.OrdinalIgnoreCase)

        ' Buscar objetos cFrame para los frames del nervio actual
        Dim framesEnNervio As New List(Of cFrame)()
        For Each fn In _nervioActual.Frames
            Dim cf = nerv.Frames.FirstOrDefault(Function(f) f.ObjectLabel.Equals(fn.ObjectLabel, StringComparison.OrdinalIgnoreCase))
            If cf IsNot Nothing Then framesEnNervio.Add(cf)
        Next

        Using dlg As New Form_AgrupacionManualNervios(
                If(Not String.IsNullOrWhiteSpace(_nervioActual.NombrePlano), _nervioActual.NombrePlano, _nervioActual.Nombre),
                framesEnNervio, framesPiso, framesOcupados)

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Dim labelsResultantes = dlg.FramesResultantes
            If labelsResultantes.Count = 0 Then Return

            ' Registrar agrupación manual para persistencia (se reaplica en recalcular)
            Dim gruposManual = nerv.GruposManual
            ' Remover entrada anterior de este nervio
            gruposManual.RemoveAll(Function(g) g.Count > 0 AndAlso
                _nervioActual.Frames.Any(Function(f) g.Contains(f.ObjectLabel, StringComparer.OrdinalIgnoreCase)))
            gruposManual.Add(labelsResultantes)

            ' Aplicar inmediatamente: rearmar los cFrameNervio del nervio actual
            AplicarReagrupacionInmediata(_nervioActual, labelsResultantes, nerv)

            _framesActuales = _nervioActual.Frames
            _cargando = True
            LlenarTablaFrames(_nervioActual)
            ConstruirTablas()
            LlenarTablas()
            _cargando = False
            DibujarPlanta()

            MessageBox.Show($"Nervio reagrupado: {_nervioActual.Frames.Count} frames.", "Listo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ''' <summary>
    ''' Reasigna los cFrameNervio del nervio activo según los labels seleccionados.
    ''' Frames que se mueven desde otros nervios del mismo piso se trasladan a este.
    ''' Frames que se quitan de este nervio se crean como nervio nuevo (1 frame).
    ''' </summary>
    Private Sub AplicarReagrupacionInmediata(nervio As cNervio, labelsDestino As List(Of String), nerv As cNervios)
        Dim pisoSel = nervio.Piso
        Dim setDestino As New HashSet(Of String)(labelsDestino, StringComparer.OrdinalIgnoreCase)
        Dim setActual As New HashSet(Of String)(nervio.Frames.Select(Function(f) f.ObjectLabel), StringComparer.OrdinalIgnoreCase)

        ' Frames que salen de este nervio → crear nervio individual por cada uno
        Dim salen = nervio.Frames.Where(Function(f) Not setDestino.Contains(f.ObjectLabel)).ToList()
        For Each fn In salen
            nervio.Frames.Remove(fn)
            ' Crear nervio individual para el frame huérfano
            Dim contador = nerv.Elementos.Count + 1
            Dim nuevoNervio As New cNervio With {
                .Nombre = $"NR-{contador}",
                .NombrePlano = $"NR-{contador}",
                .Piso = pisoSel,
                .Tf_Losa = nervio.Tf_Losa,
                .Paso_Nervios = nervio.Paso_Nervios
            }
            nuevoNervio.Frames.Add(fn)
            nerv.Elementos.Add(nuevoNervio)
        Next

        ' Frames que entran de otros nervios → moverlos aquí
        For Each label In labelsDestino
            If setActual.Contains(label) Then Continue For  ' Ya estaba en este nervio
            ' Buscar el frame en otro nervio del mismo piso
            Dim donante = nerv.Elementos.FirstOrDefault(
                Function(n) n.Piso = pisoSel AndAlso Not ReferenceEquals(n, nervio) AndAlso
                            n.Frames.Any(Function(f) f.ObjectLabel.Equals(label, StringComparison.OrdinalIgnoreCase)))
            If donante Is Nothing Then Continue For
            Dim fn = donante.Frames.First(Function(f) f.ObjectLabel.Equals(label, StringComparison.OrdinalIgnoreCase))
            donante.Frames.Remove(fn)
            nervio.Frames.Add(fn)
            ' Si el donante quedó vacío, eliminarlo
            If donante.Frames.Count = 0 Then nerv.Elementos.Remove(donante)
        Next

        ' Reordenar frames del nervio según el orden especificado por el usuario
        Dim orden = labelsDestino.Select(Function(l, i) (Label := l, Idx := i)).ToDictionary(Function(x) x.Label, Function(x) x.Idx, StringComparer.OrdinalIgnoreCase)
        nervio.Frames = nervio.Frames.OrderBy(Function(f) If(orden.ContainsKey(f.ObjectLabel), orden(f.ObjectLabel), 999)).ToList()
    End Sub

    Private Sub BtnPropagar_Click(sender As Object, e As EventArgs) Handles BtnPropagar.Click
        If _nervioActual Is Nothing Then Return
        If Not _nervioActual.EsPatron Then
            MessageBox.Show("El nervio actual no está marcado como Patrón.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim similares = Proyecto.Elementos.Nervios.Elementos.
            Where(Function(n) Not String.IsNullOrEmpty(n.PatronRef) AndAlso
                               n.PatronRef.Equals(_nervioActual.Nombre, StringComparison.OrdinalIgnoreCase)).ToList()
        If similares.Count = 0 Then
            MessageBox.Show("No hay nervios marcados como 'Similar a este Patrón'.", "Sin similares",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        For Each sim In similares
            CopiarRefuerzoDePatron(_nervioActual, sim)
        Next
        LlenarTablaNervios()
        MessageBox.Show($"Refuerzo propagado a {similares.Count} nervio(s) similar(es).", "Propagación completa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub CopiarRefuerzoDePatron(patron As cNervio, destino As cNervio)
        ' Hace la copia frame a frame, emparejando por índice de posición relativa
        Dim nFrames = Math.Min(patron.Frames.Count, destino.Frames.Count)
        For i As Integer = 0 To nFrames - 1
            Dim src = patron.Frames(i)
            Dim dst = destino.Frames(i)
            ' Copia profunda refuerzo longitudinal
            dst.RefuerzoSuperior = CopiarListaRefuerzoTramo(src.RefuerzoSuperior)
            dst.RefuerzoInferior = CopiarListaRefuerzoTramo(src.RefuerzoInferior)
            ' Copia refuerzo transversal
            dst.RefuerzoTransversal = New List(Of cRefuerzoTransversalZona)(
                src.RefuerzoTransversal.Select(Function(z) New cRefuerzoTransversalZona With {
                    .Posicion = z.Posicion,
                    .NumeroBarra = z.NumeroBarra,
                    .CantEstribos = z.CantEstribos,
                    .NumEstribos = z.NumEstribos,
                    .Separacion = z.Separacion
                }))
            dst.Ref_Modificado = True
            RecalcularFrame(dst)
        Next
    End Sub

    Private Shared Function CopiarListaRefuerzoTramo(origen As List(Of cRefuerzoTramo)) As List(Of cRefuerzoTramo)
        Return New List(Of cRefuerzoTramo)(
            origen.Select(Function(t) New cRefuerzoTramo With {
                .Frame = t.Frame,
                .Posicion = t.Posicion,
                .Barras = New Dictionary(Of String, Integer)(t.Barras, StringComparer.OrdinalIgnoreCase)
            }))
    End Function

    ' ══════════════════════════════════════════════════════════════════════════
    '  COPIAR / PEGAR COLUMNA DE REFUERZO
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub CtxRefuerzo_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) _
        Handles CtxRefuerzo.Opening
        Dim dgv = TryCast(CtxRefuerzo.SourceControl, DataGridView)
        If dgv Is Nothing Then e.Cancel = True : Return
        Dim hit = dgv.HitTest(dgv.PointToClient(Cursor.Position).X, dgv.PointToClient(Cursor.Position).Y)
        If hit.ColumnIndex < 0 Then e.Cancel = True : Return
        _ctxDgv = dgv
        _ctxCol = hit.ColumnIndex
        ' Habilitar Pegar solo si hay datos compatibles en el portapapeles
        CtxPegarCol.Enabled = (dgv Is Ref_Superior AndAlso _cbSup IsNot Nothing) OrElse
                               (dgv Is Ref_Inferior AndAlso _cbInf IsNot Nothing) OrElse
                               (dgv Is Ref_Cortante AndAlso _cbCor IsNot Nothing)
    End Sub

    Private Sub CtxCopiarCol_Click(sender As Object, e As EventArgs) Handles CtxCopiarCol.Click
        If _ctxDgv Is Nothing OrElse _ctxCol < 0 Then Return
        If _ctxDgv Is Ref_Superior Then
            _cbSup = LeerColumnaDgvInt(Ref_Superior, _ctxCol, BarSizes.Length)
        ElseIf _ctxDgv Is Ref_Inferior Then
            _cbInf = LeerColumnaDgvInt(Ref_Inferior, _ctxCol, BarSizes.Length)
        ElseIf _ctxDgv Is Ref_Cortante Then
            _cbCor = LeerColumnaDgvStr(Ref_Cortante, _ctxCol, 4)
        End If
    End Sub

    Private Sub CtxPegarCol_Click(sender As Object, e As EventArgs) Handles CtxPegarCol.Click
        If _ctxDgv Is Nothing OrElse _ctxCol < 0 Then Return
        If _ctxDgv Is Ref_Superior AndAlso _cbSup IsNot Nothing Then
            EscribirColumnaDgvInt(Ref_Superior, _ctxCol, _cbSup)
            GuardarRefuerzoSupYRecalcular(_ctxCol)
            ColorizarCeldasConValor(Ref_Superior, _ctxCol)
        ElseIf _ctxDgv Is Ref_Inferior AndAlso _cbInf IsNot Nothing Then
            EscribirColumnaDgvInt(Ref_Inferior, _ctxCol, _cbInf)
            GuardarRefuerzoInfYRecalcular(_ctxCol)
            ColorizarCeldasConValor(Ref_Inferior, _ctxCol)
        ElseIf _ctxDgv Is Ref_Cortante AndAlso _cbCor IsNot Nothing Then
            _cargando = True
            EscribirColumnaDgvStr(Ref_Cortante, _ctxCol, _cbCor)
            _cargando = False
            GuardarCortanteYRecalcular(_ctxCol)
        End If
        DibujarDiagramas()
    End Sub

    Private Shared Function LeerColumnaDgvInt(dgv As DataGridView, col As Integer, nRows As Integer) As Integer()
        Dim buf(nRows - 1) As Integer
        For row As Integer = 0 To nRows - 1
            Integer.TryParse(dgv.Rows(row).Cells(col).Value?.ToString(), buf(row))
        Next
        Return buf
    End Function

    Private Shared Function LeerColumnaDgvStr(dgv As DataGridView, col As Integer, nRows As Integer) As String()
        Dim buf(nRows - 1) As String
        For row As Integer = 0 To nRows - 1
            buf(row) = dgv.Rows(row).Cells(col).Value?.ToString()
        Next
        Return buf
    End Function

    Private Sub EscribirColumnaDgvInt(dgv As DataGridView, col As Integer, buf As Integer())
        _cargando = True
        For row As Integer = 0 To Math.Min(buf.Length - 1, dgv.Rows.Count - 1)
            dgv.Rows(row).Cells(col).Value = buf(row)
        Next
        _cargando = False
    End Sub

    Private Sub EscribirColumnaDgvStr(dgv As DataGridView, col As Integer, buf As String())
        For row As Integer = 0 To Math.Min(buf.Length - 1, dgv.Rows.Count - 1)
            dgv.Rows(row).Cells(col).Value = buf(row)
        Next
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  GUARDAR / EXPORTAR
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Save_Pilas_Click(sender As Object, e As EventArgs) Handles Save_Pilas.Click
        GuardarProyecto(Proyecto, "ARCO_2")
    End Sub

    Private Sub Exportar_Excel_Click(sender As Object, e As EventArgs) Handles Exportar_Excel.Click
        Form_11_01_Resultados.Proyecto = Proyecto
        Form_11_01_Resultados.Show()
    End Sub

    Private Sub GuardarComoNervios_Click(sender As Object, e As EventArgs)
        GuardarProyecto(Proyecto, "ARCO_2")
    End Sub

    Private Sub LimpiarDatosNervios_Click(sender As Object, e As EventArgs)
        Dim res = MessageBox.Show(
            "¿Eliminar todos los datos importados del módulo Nervios?" & vbCrLf &
            "Esta acción no se puede deshacer.",
            "Limpiar datos de nervios",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If res <> DialogResult.Yes Then Return
        Proyecto.Elementos.Nervios = New cNervios()
        _nervioActual = Nothing
        _framesActuales = Nothing
        _joints = New Dictionary(Of String, cJoint)()
        _cargando = True
        Lista_Pisos.DataSource = Nothing
        Lista_Nervios.DataSource = Nothing
        LimpiarTablas()
        Diagrama_Momento.Image = Nothing
        Diagrama_Cortante.Image = Nothing
        _cargando = False
        Label1.Text = "Datos eliminados. Use Importar → Importar demandas ETABS..."
    End Sub

    Private Sub AbrirReportesNervios_Click(sender As Object, e As EventArgs)
        Dim nerv = Proyecto.Elementos.Nervios
        If nerv Is Nothing OrElse nerv.Elementos.Count = 0 Then
            MessageBox.Show("Primero importe y calcule los nervios.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim rep As New Form_Reporte_Resumen_Nervios()
        rep.Nervios = nerv
        rep.Show(Me)
    End Sub

    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles TabControl1.SelectedIndexChanged
        If TabControl1.SelectedTab Is TabPage4 Then
            If SplitDiag.Height > 0 Then
                SplitDiag.SplitterDistance = SplitDiag.Height \ 2
            End If
            DibujarDiagramas()
        End If
    End Sub

End Class
