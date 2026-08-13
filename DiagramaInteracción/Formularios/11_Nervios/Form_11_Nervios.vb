Imports ARCO.Funciones_00_Varias
Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class Form_11_Nervios

    Public Shared Proyecto As Proyecto

    Private _svc As New NervioService()
    Private _joints As Dictionary(Of String, cJoint)
    Private _nervioActual As cNervio
    Private _framesActuales As List(Of cFrameNervio)
    Private _cargando As Boolean = False

    ' ── Constantes filas tablas de refuerzo ───────────────────────────────────
    Private Shared ReadOnly BarSizes() As String = {"#3", "#4", "#5", "#6", "#8"}
    Private Const FILA_CAPAS_SUP = 5   ' fila "Capas" en Ref_Superior
    Private Const FILA_CAPAS_INF = 5   ' fila "Capas" en Ref_Inferior
    Private Const FILA_EST_T = 6        ' fila "Es Sec. T" en Ref_Inferior

    Private Const FILA_COR_TIENE = 0
    Private Const FILA_COR_CALIBRE = 1
    Private Const FILA_COR_RAMAS = 2
    Private Const FILA_COR_SEP = 3

    ' ── Constantes filas tabla demandas ───────────────────────────────────────
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

    ' ── Constantes filas tabla resultados ─────────────────────────────────────
    Private Const FILA_RES_ASMIN = 0
    Private Const FILA_RES_ASSUP = 1
    Private Const FILA_RES_PHMNSUP = 2
    Private Const FILA_RES_ASINF = 3
    Private Const FILA_RES_PHIMNINF = 4
    Private Const FILA_RES_CDI = 5
    Private Const FILA_RES_CDC = 6
    Private Const FILA_RES_CDD = 7
    Private Const FILA_RES_BE = 8

    Private Const FILA_RES_PHIVNI = 0
    Private Const FILA_RES_PHIVND = 1
    Private Const FILA_RES_CDVI = 2
    Private Const FILA_RES_CDVD = 3
    Private Const FILA_RES_CUMPLE = 4

    ' ── Colores planta ────────────────────────────────────────────────────────
    Private Shared ReadOnly ColoresNervio As Color() = {
        Color.FromArgb(80, 180, 255),
        Color.FromArgb(255, 160, 60),
        Color.FromArgb(80, 220, 120),
        Color.FromArgb(220, 100, 200),
        Color.FromArgb(255, 220, 60),
        Color.FromArgb(100, 220, 220),
        Color.FromArgb(255, 100, 100),
        Color.FromArgb(180, 255, 100)
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
            Dim hojas = ObtenerHojasExcel(ruta)
            nerv.BeamForces = _svc.ImportarBeamForcesNervios(ruta, hojas, secNervioSet, todosFrames)

            ' Combinaciones disponibles
            nerv.Lista_Combinaciones = nerv.BeamForces _
                .Select(Function(r) r.LoadCaseKey).Distinct().OrderBy(Function(c) c).ToList()

            ' Auto-agrupar nervios
            nerv.Elementos = _svc.GenerarNerviosAuto(framesNervio, _joints)

            ' Detectar apoyos
            _svc.DetectarApoyos(nerv.Elementos, todosFrames, _joints, secNervioSet)

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

        ConstruirTablas()
        LlenarTablas()
        DibujarPlanta()
    End Sub

    ' ══════════════════════════════════════════════════════════════════════════
    '  CONSTRUCCIÓN DE TABLAS (columnas = 1 por tramo)
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub ConstruirTablas()
        If _framesActuales Is Nothing Then Return

        ConstruirTablaRefuerzo(Ref_Superior, FILA_CAPAS_SUP + 1)  ' 6 filas (bar #3..#8 + Capas)
        ConstruirTablaRefuerzo(Ref_Inferior, FILA_EST_T + 1)       ' 7 filas (+ Es Sec. T)
        ConstruirTablaRefuerzoCortante()
        ConstruirTablaDemandas()
        ConstruirTablaResultados()
    End Sub

    Private Sub ConstruirTablaRefuerzo(dgv As DataGridView, nFilas As Integer)
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        ' Columnas: 1 por tramo
        For Each fn In _framesActuales
            Dim col As New DataGridViewTextBoxColumn()
            col.HeaderText = fn.ObjectLabel
            col.Width = 60
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            dgv.Columns.Add(col)
        Next

        ' Filas: calibres + Capas [+ Es Sec. T]
        For i As Integer = 0 To nFilas - 1
            Dim rowIdx = dgv.Rows.Add()
            If i < BarSizes.Length Then
                dgv.Rows(rowIdx).HeaderCell.Value = BarSizes(i)
            ElseIf i = FILA_CAPAS_SUP Then
                dgv.Rows(rowIdx).HeaderCell.Value = "Capas"
            Else
                dgv.Rows(rowIdx).HeaderCell.Value = "Es T (S/N)"
            End If
        Next
    End Sub

    Private Sub ConstruirTablaRefuerzoCortante()
        Ref_Cortante.Columns.Clear()
        Ref_Cortante.Rows.Clear()

        For Each fn In _framesActuales
            Dim col As New DataGridViewTextBoxColumn()
            col.HeaderText = fn.ObjectLabel
            col.Width = 70
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Ref_Cortante.Columns.Add(col)
        Next

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
                         "Vu I (kN)", "Vu D (kN)"}
        For Each lbl In etiquetas
            Dim rowIdx = Tabla_Demandas.Rows.Add()
            Tabla_Demandas.Rows(rowIdx).HeaderCell.Value = lbl
        Next
    End Sub

    Private Sub ConstruirTablaResultados()
        Tabla_Resultados_Flexion.Columns.Clear()
        Tabla_Resultados_Flexion.Rows.Clear()
        Tabla_Resultados_Cortante.Columns.Clear()
        Tabla_Resultados_Cortante.Rows.Clear()

        For Each fn In _framesActuales
            Dim c1 As New DataGridViewTextBoxColumn()
            c1.HeaderText = fn.ObjectLabel : c1.Width = 80
            c1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Tabla_Resultados_Flexion.Columns.Add(c1)

            Dim c2 As New DataGridViewTextBoxColumn()
            c2.HeaderText = fn.ObjectLabel : c2.Width = 80
            c2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Tabla_Resultados_Cortante.Columns.Add(c2)
        Next

        Dim etqFlex = {"As_min (cm²)", "As sup (cm²)", "φMn sup (kN·m)",
                       "As inf (cm²)", "φMn inf (kN·m)",
                       "C/D flex I", "C/D flex C", "C/D flex D", "Be (m)"}
        For Each lbl In etqFlex
            Dim r = Tabla_Resultados_Flexion.Rows.Add()
            Tabla_Resultados_Flexion.Rows(r).HeaderCell.Value = lbl
        Next

        Dim etqCor = {"φVn I (kN)", "φVn D (kN)", "C/D cort I", "C/D cort D", "Cumple"}
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
        For col As Integer = 0 To _framesActuales.Count - 1
            Dim fn = _framesActuales(col)

            ' Ref_Superior
            For row As Integer = 0 To BarSizes.Length - 1
                Ref_Superior.Rows(row).Cells(col).Value =
                    If(BarSizes(row) = fn.Calibre_Sup, fn.Barras_Sup, 0)
            Next
            Ref_Superior.Rows(FILA_CAPAS_SUP).Cells(col).Value = fn.Capas_Sup

            ' Ref_Inferior
            For row As Integer = 0 To BarSizes.Length - 1
                Ref_Inferior.Rows(row).Cells(col).Value =
                    If(BarSizes(row) = fn.Calibre_Inf, fn.Barras_Inf, 0)
            Next
            Ref_Inferior.Rows(FILA_CAPAS_INF).Cells(col).Value = fn.Capas_Inf
            Ref_Inferior.Rows(FILA_EST_T).Cells(col).Value = If(fn.EsSeccionT, "S", "N")

            ' Ref_Cortante
            Ref_Cortante.Rows(FILA_COR_TIENE).Cells(col).Value = If(fn.TieneEstribos, "S", "N")
            Ref_Cortante.Rows(FILA_COR_CALIBRE).Cells(col).Value = fn.Estribo_Calibre
            Ref_Cortante.Rows(FILA_COR_RAMAS).Cells(col).Value = fn.Estribo_Ramas
            Ref_Cortante.Rows(FILA_COR_SEP).Cells(col).Value = fn.Estribo_Sep.ToString("F3")

            ' Resaltar calibre activo en Ref_Superior
            ColorizarFilaActiva(Ref_Superior, fn.Calibre_Sup, col)
            ColorizarFilaActiva(Ref_Inferior, fn.Calibre_Inf, col)
        Next
    End Sub

    Private Sub ColorizarFilaActiva(dgv As DataGridView, calibreActivo As String, col As Integer)
        For row As Integer = 0 To BarSizes.Length - 1
            Dim esActivo = BarSizes(row) = calibreActivo
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
        Next
    End Sub

    Private Sub LlenarResultados()
        For col As Integer = 0 To _framesActuales.Count - 1
            Dim fn = _framesActuales(col)

            ' Flexión
            Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN).Cells(col).Value = fn.As_Min.ToString("F2")
            Tabla_Resultados_Flexion.Rows(FILA_RES_ASSUP).Cells(col).Value = fn.As_Prov_Sup.ToString("F2")
            Tabla_Resultados_Flexion.Rows(FILA_RES_PHMNSUP).Cells(col).Value = fn.PhiMn_Sup.ToString("F1")
            Tabla_Resultados_Flexion.Rows(FILA_RES_ASINF).Cells(col).Value = fn.As_Prov_Inf.ToString("F2")
            Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMNINF).Cells(col).Value = fn.PhiMn_Inf.ToString("F1")
            Tabla_Resultados_Flexion.Rows(FILA_RES_CDI).Cells(col).Value = fn.CD_Flex_Sup_I.ToString("F2")
            Tabla_Resultados_Flexion.Rows(FILA_RES_CDC).Cells(col).Value = fn.CD_Flex_Inf_C.ToString("F2")
            Tabla_Resultados_Flexion.Rows(FILA_RES_CDD).Cells(col).Value = fn.CD_Flex_Sup_D.ToString("F2")
            Tabla_Resultados_Flexion.Rows(FILA_RES_BE).Cells(col).Value =
                If(fn.EsSeccionT, fn.Be.ToString("F3"), "—")

            ' Semáforo C/D flexión
            ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CDI).Cells(col), fn.CD_Flex_Sup_I)
            ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CDC).Cells(col), fn.CD_Flex_Inf_C)
            ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CDD).Cells(col), fn.CD_Flex_Sup_D)

            ' Cortante
            Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVNI).Cells(col).Value = fn.PhiVn_I.ToString("F1")
            Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVND).Cells(col).Value = fn.PhiVn_D.ToString("F1")
            Tabla_Resultados_Cortante.Rows(FILA_RES_CDVI).Cells(col).Value = fn.CD_Cortante_I.ToString("F2")
            Tabla_Resultados_Cortante.Rows(FILA_RES_CDVD).Cells(col).Value = fn.CD_Cortante_D.ToString("F2")
            Tabla_Resultados_Cortante.Rows(FILA_RES_CUMPLE).Cells(col).Value =
                If(fn.Cumple, "✓ Cumple", "✗ No cumple")

            ColorearCD(Tabla_Resultados_Cortante.Rows(FILA_RES_CDVI).Cells(col), fn.CD_Cortante_I)
            ColorearCD(Tabla_Resultados_Cortante.Rows(FILA_RES_CDVD).Cells(col), fn.CD_Cortante_D)

            Dim cumpleCell = Tabla_Resultados_Cortante.Rows(FILA_RES_CUMPLE).Cells(col)
            cumpleCell.Style.BackColor = If(fn.Cumple, Color.FromArgb(200, 240, 200), Color.FromArgb(255, 200, 200))
            cumpleCell.Style.ForeColor = If(fn.Cumple, Color.DarkGreen, Color.DarkRed)
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
        If col >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(col)

        ' Leer calibre activo (primera fila con valor > 0)
        For row As Integer = 0 To BarSizes.Length - 1
            Dim v As Integer = 0
            Integer.TryParse(Ref_Superior.Rows(row).Cells(col).Value?.ToString(), v)
            If v > 0 Then
                fn.Calibre_Sup = BarSizes(row)
                fn.Barras_Sup = v
                Exit For
            End If
        Next

        Dim capas As Integer = 1
        Integer.TryParse(Ref_Superior.Rows(FILA_CAPAS_SUP).Cells(col).Value?.ToString(), capas)
        fn.Capas_Sup = Math.Max(1, capas)
        fn.Ref_Modificado = True

        RecalcularFrame(fn)
        _cargando = True
        ColorizarFilaActiva(Ref_Superior, fn.Calibre_Sup, col)
        LlenarResultadosColumna(col)
        _cargando = False
        DibujarPlanta()
    End Sub

    Private Sub GuardarRefuerzoInfYRecalcular(col As Integer)
        If col >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(col)

        For row As Integer = 0 To BarSizes.Length - 1
            Dim v As Integer = 0
            Integer.TryParse(Ref_Inferior.Rows(row).Cells(col).Value?.ToString(), v)
            If v > 0 Then
                fn.Calibre_Inf = BarSizes(row)
                fn.Barras_Inf = v
                Exit For
            End If
        Next

        Dim capas As Integer = 1
        Integer.TryParse(Ref_Inferior.Rows(FILA_CAPAS_INF).Cells(col).Value?.ToString(), capas)
        fn.Capas_Inf = Math.Max(1, capas)

        Dim esT = Ref_Inferior.Rows(FILA_EST_T).Cells(col).Value?.ToString().ToUpperInvariant()
        fn.EsSeccionT = (esT = "S" OrElse esT = "SI" OrElse esT = "Y" OrElse esT = "TRUE")

        fn.Ref_Modificado = True
        RecalcularFrame(fn)
        _cargando = True
        ColorizarFilaActiva(Ref_Inferior, fn.Calibre_Inf, col)
        LlenarResultadosColumna(col)
        _cargando = False
        DibujarPlanta()
    End Sub

    Private Sub GuardarCortanteYRecalcular(col As Integer)
        If col >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(col)

        Dim tieneStr = Ref_Cortante.Rows(FILA_COR_TIENE).Cells(col).Value?.ToString().ToUpperInvariant()
        fn.TieneEstribos = (tieneStr = "S" OrElse tieneStr = "SI" OrElse tieneStr = "Y")

        Dim cal = Ref_Cortante.Rows(FILA_COR_CALIBRE).Cells(col).Value?.ToString()
        If Not String.IsNullOrWhiteSpace(cal) Then fn.Estribo_Calibre = cal.Trim()

        Dim ramas As Integer = 2
        Integer.TryParse(Ref_Cortante.Rows(FILA_COR_RAMAS).Cells(col).Value?.ToString(), ramas)
        fn.Estribo_Ramas = Math.Max(1, ramas)

        Dim sep As Double = 0.15
        Double.TryParse(Ref_Cortante.Rows(FILA_COR_SEP).Cells(col).Value?.ToString(),
                        Globalization.NumberStyles.Any,
                        Globalization.CultureInfo.CurrentCulture, sep)
        fn.Estribo_Sep = If(sep > 0, sep, 0.15)
        fn.Ref_Modificado = True

        RecalcularFrame(fn)
        _cargando = True
        LlenarResultadosColumna(col)
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

    Private Sub LlenarResultadosColumna(col As Integer)
        If col >= _framesActuales.Count Then Return
        Dim fn = _framesActuales(col)

        Tabla_Resultados_Flexion.Rows(FILA_RES_ASMIN).Cells(col).Value = fn.As_Min.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASSUP).Cells(col).Value = fn.As_Prov_Sup.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHMNSUP).Cells(col).Value = fn.PhiMn_Sup.ToString("F1")
        Tabla_Resultados_Flexion.Rows(FILA_RES_ASINF).Cells(col).Value = fn.As_Prov_Inf.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_PHIMNINF).Cells(col).Value = fn.PhiMn_Inf.ToString("F1")
        Tabla_Resultados_Flexion.Rows(FILA_RES_CDI).Cells(col).Value = fn.CD_Flex_Sup_I.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_CDC).Cells(col).Value = fn.CD_Flex_Inf_C.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_CDD).Cells(col).Value = fn.CD_Flex_Sup_D.ToString("F2")
        Tabla_Resultados_Flexion.Rows(FILA_RES_BE).Cells(col).Value =
            If(fn.EsSeccionT, fn.Be.ToString("F3"), "—")

        ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CDI).Cells(col), fn.CD_Flex_Sup_I)
        ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CDC).Cells(col), fn.CD_Flex_Inf_C)
        ColorearCD(Tabla_Resultados_Flexion.Rows(FILA_RES_CDD).Cells(col), fn.CD_Flex_Sup_D)

        Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVNI).Cells(col).Value = fn.PhiVn_I.ToString("F1")
        Tabla_Resultados_Cortante.Rows(FILA_RES_PHIVND).Cells(col).Value = fn.PhiVn_D.ToString("F1")
        Tabla_Resultados_Cortante.Rows(FILA_RES_CDVI).Cells(col).Value = fn.CD_Cortante_I.ToString("F2")
        Tabla_Resultados_Cortante.Rows(FILA_RES_CDVD).Cells(col).Value = fn.CD_Cortante_D.ToString("F2")
        Tabla_Resultados_Cortante.Rows(FILA_RES_CUMPLE).Cells(col).Value =
            If(fn.Cumple, "✓ Cumple", "✗ No cumple")

        ColorearCD(Tabla_Resultados_Cortante.Rows(FILA_RES_CDVI).Cells(col), fn.CD_Cortante_I)
        ColorearCD(Tabla_Resultados_Cortante.Rows(FILA_RES_CDVD).Cells(col), fn.CD_Cortante_D)

        Dim cumpleCell = Tabla_Resultados_Cortante.Rows(FILA_RES_CUMPLE).Cells(col)
        cumpleCell.Style.BackColor = If(fn.Cumple, Color.FromArgb(200, 240, 200), Color.FromArgb(255, 200, 200))
        cumpleCell.Style.ForeColor = If(fn.Cumple, Color.DarkGreen, Color.DarkRed)
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
            g.Clear(Color.FromArgb(16, 20, 34))
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
        Using penFondo As New Pen(Color.FromArgb(40, 60, 80), 1)
            For Each f In framesTodos
                Dim ji As cJoint = Nothing, jj As cJoint = Nothing
                If _joints.TryGetValue(f.JointI, ji) AndAlso _joints.TryGetValue(f.JointJ, jj) Then
                    g.DrawLine(penFondo, Tx(ji.GlobalX), Ty(ji.GlobalY), Tx(jj.GlobalX), Ty(jj.GlobalY))
                End If
            Next
        End Using

        Dim fontLabel As New Font("Segoe UI", 7)
        For idx As Integer = 0 To nervios.Count - 1
            Dim nerv = nervios(idx)
            Dim color = ColoresNervio(idx Mod ColoresNervio.Length)
            Dim esSeleccionado = ReferenceEquals(nerv, _nervioActual)
            Dim grosor = If(esSeleccionado, 4.0F, 2.5F)

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
                        Dim clrCD = If(cdMin >= 1.0, Color.LimeGreen, If(cdMin >= 0.9, Color.Orange, Color.Red))
                        Dim xm = (p1.X + p2.X) / 2
                        Dim ym = (p1.Y + p2.Y) / 2
                        Using brushCD As New SolidBrush(clrCD)
                            g.FillEllipse(brushCD, xm - 5, ym - 5, 10, 10)
                        End Using
                    End If

                    If ChkMostrarEtiquetas.Checked Then
                        Dim xm = (p1.X + p2.X) / 2
                        Dim ym = (p1.Y + p2.Y) / 2
                        g.DrawString(fn.ObjectLabel, fontLabel, Brushes.White, xm + 4, ym - 8)
                    End If

                    If ChkMostrarApoyos.Checked Then
                        If fn.B_Apoyo_I > 0 Then g.FillEllipse(Brushes.Orange, p1.X - 4, p1.Y - 4, 8, 8)
                        If fn.B_Apoyo_D > 0 Then g.FillEllipse(Brushes.Orange, p2.X - 4, p2.Y - 4, 8, 8)
                    End If
                Next
            End Using

            If nerv.Frames.Count > 0 Then
                Dim fn0 = nerv.Frames(0)
                Dim ji As cJoint = Nothing
                If _joints.TryGetValue(fn0.JointI, ji) Then
                    Using brNombre As New SolidBrush(color)
                        g.DrawString(nerv.ToString(), New Font("Segoe UI", 8, FontStyle.Bold),
                                     brNombre, Tx(ji.GlobalX), Ty(ji.GlobalY) - 14)
                    End Using
                End If
            End If
        Next
        fontLabel.Dispose()
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

    ' ══════════════════════════════════════════════════════════════════════════
    '  GUARDAR / EXPORTAR
    ' ══════════════════════════════════════════════════════════════════════════

    Private Sub Save_Pilas_Click(sender As Object, e As EventArgs) Handles Save_Pilas.Click
        GuardarProyecto(Proyecto, "ARCO_2")
    End Sub

    Private Sub Exportar_Excel_Click(sender As Object, e As EventArgs) Handles Exportar_Excel.Click
        MessageBox.Show("Exportación a Excel — próximamente.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class
