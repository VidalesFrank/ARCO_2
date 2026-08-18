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

    ' ── Constantes filas/zonas tablas de refuerzo (3 columnas por tramo: Izq/Centro/Der) ─
    Private Shared ReadOnly BarSizes() As String = {"#3", "#4", "#5", "#6", "#7", "#8", "#10"}
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

    ' ── Constantes filas tabla resultados (3 columnas por tramo: Izq/Centro/Der) ─
    Private Const FILA_RES_ASMIN = 0
    Private Const FILA_RES_ASPROV = 1
    Private Const FILA_RES_PHIMN = 2
    Private Const FILA_RES_CD = 3
    Private Const FILA_RES_BE = 4

    Private Const FILA_RES_PHIVN = 0
    Private Const FILA_RES_CDV = 1
    Private Const FILA_RES_CUMPLE = 2

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
        ConstruirTablasNavegacion()
        SincronizarDesdeProyecto()
    End Sub

    Public Sub RefrescarDesdeProyecto()
        SincronizarDesdeProyecto()
    End Sub

    Private Sub SincronizarDesdeProyecto()
        If Proyecto Is Nothing Then Return
        If Proyecto.Elementos.Nervios Is Nothing Then
            Proyecto.Elementos.Nervios = New cNervios()
        End If

        _joints = Proyecto.Elementos.Joints.ToDictionary(Function(j) j.ElementLabel)

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

            ' Bootstrap del modelo base (Joints/Frames/Secciones) si aún no se ha
            ' cargado desde otro módulo (Vigas/Columnas/Muros). Sin esto, Frames
            ' queda vacío y el diálogo de selección de secciones no muestra nada.
            If Proyecto.Elementos.Frames.Count = 0 Then
                Dim hJoints = ResolverNombreHoja(hojas, "Objects and Elements - Joints", "Joint Coordinates")
                Dim hFrames = ResolverNombreHoja(hojas, "Objects and Elements - Frames", "Connectivity - Frame")
                Proyecto.TablasEtabs.TablaOEJoints = LeerHojaExcel(ruta, hJoints)
                Proyecto.TablasEtabs.TablaOEFrames = LeerHojaExcel(ruta, hFrames)

                Proyecto.Elementos.Joints = DataTableToJoints(Proyecto.TablasEtabs.TablaOEJoints)
                Proyecto.Elementos.Frames = DataTableToFrames(Proyecto.TablasEtabs.TablaOEFrames)

                Dim hAsigFrame = ResolverNombreHoja(hojas, "Frame Assigns - Sect Prop", "Frame Assignments - Sections")
                Dim hSecDef = ResolverNombreHoja(hojas, "Frame Sec Def - Conc Rect", "Frame Sections")
                Dim hMaterial = ResolverNombreHoja(hojas, "Mat Prop - Concrete Data", "Material Properties - Concrete")

                Dim Data_Asig_Frame As DataTable = LeerHojaExcel(ruta, hAsigFrame)
                Dim Data_Frame_Section As DataTable = LeerHojaExcel(ruta, hSecDef)
                Dim Data_Material_Concrete As DataTable = LeerHojaExcel(ruta, hMaterial)

                DataTableToAsignFrame(Proyecto.Elementos.Frames, Data_Asig_Frame, Data_Frame_Section, Data_Material_Concrete)

                _joints = Proyecto.Elementos.Joints.ToDictionary(Function(j) j.ElementLabel)
            End If

            Dim nerv = Proyecto.Elementos.Nervios
            Dim todosFrames = Proyecto.Elementos.Frames

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
            _svc.DesignarNervios(nerv.Elementos, _joints)

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

        ' Solo Es T / Eje Izq / Eje Der son editables; el resto son valores calculados/importados
        For row As Integer = FILA_BW To FILA_VUD
            For col As Integer = 0 To Tabla_Demandas.Columns.Count - 1
                Tabla_Demandas.Rows(row).Cells(col).Style.BackColor = Color.FromArgb(240, 240, 240)
                Tabla_Demandas.Rows(row).Cells(col).ReadOnly = True
            Next
        Next
    End Sub

    Private Sub ConstruirTablaResultados()
        Tabla_Resultados_Flexion.Columns.Clear()
        Tabla_Resultados_Flexion.Rows.Clear()
        Tabla_Resultados_Cortante.Columns.Clear()
        Tabla_Resultados_Cortante.Rows.Clear()

        AgregarColumnasPorZona(Tabla_Resultados_Flexion, 70)
        AgregarColumnasPorZona(Tabla_Resultados_Cortante, 70)

        Dim etqFlex = {"As_min (cm²)", "As prov (cm²)", "φMn (kN·m)", "C/D flexión", "Be (m)"}
        For Each lbl In etqFlex
            Dim r = Tabla_Resultados_Flexion.Rows.Add()
            Tabla_Resultados_Flexion.Rows(r).HeaderCell.Value = lbl
        Next

        Dim etqCor = {"φVn (kN)", "C/D cortante", "Cumple"}
        For Each lbl In etqCor
            Dim r = Tabla_Resultados_Cortante.Rows.Add()
            Tabla_Resultados_Cortante.Rows(r).HeaderCell.Value = lbl
        Next
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

    ''' <summary>Resalta las celdas de calibre con cantidad > 0 (puede haber varios calibres combinados por zona).</summary>
    Private Sub ColorizarCeldasConValor(dgv As DataGridView, col As Integer)
        For row As Integer = 0 To BarSizes.Length - 1
            Dim v As Integer = 0
            Integer.TryParse(dgv.Rows(row).Cells(col).Value?.ToString(), v)
            Dim esActivo = v > 0
            dgv.Rows(row).Cells(col).Style.BackColor =
                If(esActivo, Color.FromArgb(200, 230, 255), Color.Empty)
            dgv.Rows(row).Cells(col).Style.Font =
                If(esActivo, New Font("Segoe UI", 9, FontStyle.Bold), Nothing)
        Next
    End Sub

    Private Sub LlenarDemandas()
        For col As Integer = 0 To _framesActuales.Count - 1
            Dim fn = _framesActuales(col)
            Tabla_Demandas.Rows(FILA_BW).Cells(col).Value = fn.Bw.ToString("F3")
            Tabla_Demandas.Rows(FILA_H).Cells(col).Value = fn.H.ToString("F3")
            Tabla_Demandas.Rows(FILA_L).Cells(col).Value = fn.Longitud.ToString("F2")
            Tabla_Demandas.Rows(FILA_BAPO_I).Cells(col).Value = fn.B_Apoyo_I.ToString("F3")
            Tabla_Demandas.Rows(FILA_BAPO_D).Cells(col).Value = fn.B_Apoyo_D.ToString("F3")
            Tabla_Demandas.Rows(FILA_MUI).Cells(col).Value = fn.Mu_Neg_I.ToString("F2")
            Tabla_Demandas.Rows(FILA_MUC).Cells(col).Value = fn.Mu_Pos_C.ToString("F2")
            Tabla_Demandas.Rows(FILA_MUD).Cells(col).Value = fn.Mu_Neg_D.ToString("F2")
            Tabla_Demandas.Rows(FILA_VUI).Cells(col).Value = fn.Vu_I.ToString("F2")
            Tabla_Demandas.Rows(FILA_VUD).Cells(col).Value = fn.Vu_D.ToString("F2")
            Tabla_Demandas.Rows(FILA_ESECT).Cells(col).Value = If(fn.EsSeccionT, "S", "N")
            Tabla_Demandas.Rows(FILA_EJE_I).Cells(col).Value = If(String.IsNullOrWhiteSpace(fn.EjeApoyo_I), "—", fn.EjeApoyo_I)
            Tabla_Demandas.Rows(FILA_EJE_D).Cells(col).Value = If(String.IsNullOrWhiteSpace(fn.EjeApoyo_D), "—", fn.EjeApoyo_D)
        Next
    End Sub

    Private Sub Tabla_Demandas_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Tabla_Demandas.CellValueChanged
        If _cargando OrElse _framesActuales Is Nothing Then Return
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 OrElse e.ColumnIndex >= _framesActuales.Count Then Return

        Dim fn = _framesActuales(e.ColumnIndex)
        Select Case e.RowIndex
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

    Private Sub RecalcularFrame(fn As cFrameNervio)
        If _nervioActual IsNot Nothing Then
            fn.Tf = _nervioActual.Tf_Losa
            fn.Paso = _nervioActual.Paso_Nervios
            If fn.EsSeccionT Then
                fn.Be = _svc.CalcularBe(fn.Bw, fn.Tf, fn.Paso, fn.Longitud, fn.B_Apoyo_I, fn.B_Apoyo_D)
            End If
        End If
        _svc.CalcularFlexion(fn)
        _svc.CalcularCortante(fn)
        fn.Cumple = fn.CD_Flex_Sup_I >= 0.9 AndAlso
                    fn.CD_Flex_Inf_C >= 0.9 AndAlso
                    fn.CD_Flex_Sup_D >= 0.9 AndAlso
                    fn.CD_Cortante_I >= 0.9 AndAlso
                    fn.CD_Cortante_D >= 0.9
    End Sub

    ''' <summary>Escribe las 3 columnas (Izq/Centro/Der) de resultados del tramo fi, cada una
    ''' con la capacidad de su propia zona de refuerzo.</summary>
    Private Sub LlenarResultadosColumna(fi As Integer)
        If fi >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(fi)
        Dim colIzq = fi * 3 + 0
        Dim colCentro = fi * 3 + 1
        Dim colDer = fi * 3 + 2

        EscribirColumnaFlexion(colIzq, fn.As_Min, fn.As_Prov_Sup_I, fn.PhiMn_Sup_I, fn.CD_Flex_Sup_I, Nothing)
        EscribirColumnaFlexion(colCentro, fn.As_Min, fn.As_Prov_Inf_C, fn.PhiMn_Inf_C, fn.CD_Flex_Inf_C,
                                If(fn.EsSeccionT, CType(fn.Be, Double?), Nothing))
        EscribirColumnaFlexion(colDer, fn.As_Min, fn.As_Prov_Sup_D, fn.PhiMn_Sup_D, fn.CD_Flex_Sup_D, Nothing)

        EscribirColumnaCortante(colIzq, fn.PhiVn_I, fn.CD_Cortante_I, fn.Cumple)
        EscribirColumnaCortante(colCentro, Nothing, Nothing, fn.Cumple)
        EscribirColumnaCortante(colDer, fn.PhiVn_D, fn.CD_Cortante_D, fn.Cumple)
    End Sub

    Private Sub EscribirColumnaFlexion(col As Integer, asMin As Double, asProv As Double,
                                        phiMn As Double, cd As Double, be As Double?)
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN).Cells(col).Value = asMin.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASPROV).Cells(col).Value = asProv.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMN).Cells(col).Value = phiMn.ToString("F1")
        Tabla_Resultados_Flexion.Rows(FILA_RES_CD).Cells(col).Value = cd.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_BE).Cells(col).Value = If(be.HasValue, be.Value.ToString("F3"), "—")
        ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CD).Cells(col), cd)
    End Sub

    Private Sub EscribirColumnaCortante(col As Integer, phiVn As Double?, cd As Double?, cumple As Boolean)
        Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVN).Cells(col).Value = If(phiVn.HasValue, phiVn.Value.ToString("F1"), "—")
        Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col).Value = If(cd.HasValue, cd.Value.ToString("F2"), "—")
        If cd.HasValue Then
            ColorearCD(Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col), cd.Value)
        Else
            Tabla_Resultados_Cortante.Rows(FILA_RES_CDV).Cells(col).Style.BackColor = Color.Empty
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
        For Each c As DataGridViewColumn In Tabla_Nervios.Columns
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        Tabla_Nervios.Columns(0).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft

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

        _cargando = True
        Try
            Tabla_Nervios.Rows.Clear()
            For Each n In lista
                Dim longTotal = n.Frames.Sum(Function(f) f.Longitud)
                Dim tieneCalculo = n.Frames.Any(Function(f) f.Ref_Modificado)
                Dim todoCumple = tieneCalculo AndAlso n.Frames.All(Function(f) f.Cumple)
                Dim estadoTxt = If(tieneCalculo, If(todoCumple, "✓ Cumple", "✗ Falla"), "—")

                Dim r = Tabla_Nervios.Rows.Add(
                    If(Not String.IsNullOrWhiteSpace(n.NombrePlano), n.NombrePlano, n.Nombre),
                    n.Piso,
                    n.Frames.Count,
                    longTotal.ToString("F2"),
                    estadoTxt)

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

        ' Fondo de estructura
        Dim framesTodos = Proyecto.Elementos.Frames _
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
    '  GUARDAR / EXPORTAR
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Save_Pilas_Click(sender As Object, e As EventArgs) Handles Save_Pilas.Click
        GuardarProyecto(Proyecto, "ARCO_2")
    End Sub

    Private Sub Exportar_Excel_Click(sender As Object, e As EventArgs) Handles Exportar_Excel.Click
        Form_11_01_Resultados.Proyecto = Proyecto
        Form_11_01_Resultados.Show()
    End Sub

End Class
