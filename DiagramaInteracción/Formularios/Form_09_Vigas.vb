Imports System.IO
Imports System.Linq
Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias
Imports DocumentFormat.OpenXml.Math
Imports iTextSharp


Public Class Form_09_Vigas
    Public Shared Proyecto As Proyecto

    Private _vigas As List(Of cViga)
    Private _joints As Dictionary(Of String, cJoint)

    Private _vigaActual As cViga

    Private Const FILA_MNEG = 2
    Private Const FILA_MPOS = 3
    Private Const FILA_AS_SUP = 4
    Private Const FILA_AS_INF = 5
    Private Const FILA_F_NEG = 8
    Private Const FILA_F_POS = 9
    Private Const FILA_RED_NEG = 10
    Private Const FILA_RED_POS = 11

    Private Const FILA_COR_SECCION = 0
    Private Const FILA_COR_LONGITUD = 1
    Private Const FILA_COR_H = 2
    Private Const FILA_COR_D = 3
    Private Const FILA_COR_ZONA = 4
    Private Const FILA_COR_VU = 5
    Private Const FILA_COR_VC = 6
    Private Const FILA_COR_VS = 7
    Private Const FILA_COR_PHIVN = 8
    Private Const FILA_COR_FACTOR = 9

    Private HayCambios As Boolean = False
    Private UltimoGuardado As DateTime = DateTime.Now
    Private _cargando As Boolean = False

    Private _geo As New GeometryService()
    Private _vigaService As VigaService
    Private _DiagramaService As DiagramaService

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Try
            '    ' 1. Cargar datos (si aplica)

            _cargando = True

            ' Si el DataTable original está almacenado, re-procesar con el algoritmo
            ' corregido (incluye el tercer fallback para joints complejos) para recuperar
            ' frames de pisos como P1, P10-P13 que versiones anteriores descartaban.
            If Proyecto.TablasEtabs?.TablaOEFrames?.Rows.Count > 0 Then
                Dim framesRefrescados = DataTableToFrames(Proyecto.TablasEtabs.TablaOEFrames)
                If framesRefrescados.Count <> Proyecto.Elementos.Vigas.Frames.Count Then
                    ' Índice por Story+Label (coincidencia exacta)
                    Dim dicSec As New Dictionary(Of String, cSeccion)(StringComparer.OrdinalIgnoreCase)
                    ' Índice por Label sólo (herencia entre pisos con el mismo nombre de barra)
                    Dim dicSecLabel As New Dictionary(Of String, cSeccion)(StringComparer.OrdinalIgnoreCase)
                    For Each f In Proyecto.Elementos.Vigas.Frames
                        If f.Section Is Nothing Then Continue For
                        Dim k = $"{f.Story}|{f.ObjectLabel}".ToUpperInvariant()
                        If Not dicSec.ContainsKey(k) Then dicSec(k) = f.Section
                        Dim lbl = f.ObjectLabel.Trim().ToUpperInvariant()
                        If Not dicSecLabel.ContainsKey(lbl) Then dicSecLabel(lbl) = f.Section
                    Next
                    For Each f In framesRefrescados
                        Dim k = $"{f.Story}|{f.ObjectLabel}".ToUpperInvariant()
                        Dim sec As cSeccion = Nothing
                        If dicSec.TryGetValue(k, sec) Then
                            f.Section = sec
                        ElseIf dicSecLabel.TryGetValue(f.ObjectLabel.Trim().ToUpperInvariant(), sec) Then
                            f.Section = sec   ' mismo label en otro piso → misma sección
                        End If
                    Next
                    Proyecto.Elementos.Vigas.Frames = framesRefrescados
                End If
            End If

            Dim Joints = Proyecto.Elementos.Vigas.Joints
            Dim Frames = Proyecto.Elementos.Vigas.Frames

            ' Filtrar frames tipo beam y aplicar filtro de secciones del usuario.
            ' Los frames que pertenecen a grupos de réplica se incluyen siempre para que
            ' la agrupación funcione aunque el filtro de secciones los excluya.
            Dim seccFiltro = Proyecto.Elementos.Vigas.SeccionesSeleccionadas
            Dim grupoLabels As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If Proyecto.Elementos.Vigas.GruposReplica IsNot Nothing Then
                For Each gr In Proyecto.Elementos.Vigas.GruposReplica
                    For Each lbl In gr.Labels_Patron
                        grupoLabels.Add(lbl.Trim())
                    Next
                Next
            End If
            Dim beams As List(Of cFrame) = Frames _
                .Where(Function(f) f.ObjectLabel.StartsWith("B", StringComparison.OrdinalIgnoreCase)) _
                .Where(Function(f) seccFiltro.Count = 0 OrElse
                                   grupoLabels.Contains(f.ObjectLabel.Trim()) OrElse
                                   (f.Section IsNot Nothing AndAlso seccFiltro.Contains(f.Section.Nombre))) _
                .ToList()

            Dim jointsDict As Dictionary(Of String, cJoint) = Proyecto.Elementos.Vigas.Joints.ToDictionary(Function(j) j.ElementLabel)

            ' 2. Generar vigas a partir de joints y frames
            Dim vigas As List(Of cViga) = _vigaService.GenerarVigas(beams, jointsDict)

            For Each v In vigas
                _vigaService.OrdenarFramesViga(v, jointsDict)
            Next

            ' Asignar eje estructural más cercano a cada apoyo (requiere grids de ETABS)
            Dim gridsEjes = Proyecto?.Elementos?.Grids?.GridLines
            If gridsEjes IsNot Nothing AndAlso gridsEjes.Count > 0 Then
                _geo.AsignarEjesAVigas(vigas, gridsEjes, jointsDict)
            End If

            ' Reaplicar agrupaciones manuales previas del usuario
            If Proyecto.Elementos.Vigas.GruposManual.Count > 0 Then
                _vigaService.AplicarGruposManual(vigas, Proyecto.Elementos.Vigas.GruposManual, jointsDict)
            End If

            ' Reaplicar grupos de réplica patrón/similar
            If Proyecto.Elementos.Vigas.GruposReplica IsNot Nothing AndAlso
               Proyecto.Elementos.Vigas.GruposReplica.Count > 0 Then
                _vigaService.AplicarGruposReplicaEnVigas(vigas, Proyecto.Elementos.Vigas.GruposReplica, jointsDict)
            End If

            ' Eje paralelo (eje que la viga "sigue") y nombres significativos
            If gridsEjes IsNot Nothing AndAlso gridsEjes.Count > 0 Then
                _geo.AsignarEjesParalelosAVigas(vigas, gridsEjes, jointsDict)
            End If
            _vigaService.GenerarNombresPlano(vigas, Proyecto.Elementos.Vigas.PrefijoNombreViga)

            ' Propagar NombrePlano del patrón a sus similares (después de GenerarNombresPlano)
            PropagateNombresGrupo(vigas, Proyecto.Elementos.Vigas.GruposReplica)

            Proyecto.Elementos.Vigas.Vigas = vigas

            _vigas = vigas
            _joints = jointsDict

            _vigaService.CalcularEnvolventesVigas(vigas,
                                                  Proyecto.Elementos.Vigas.BeamForces,
                                                  New HashSet(Of String)(
                                                      Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Select(Function(c) NormalizarClaveCombo(c)),
                                                      StringComparer.OrdinalIgnoreCase))

            Dim diagMomentos As String = _vigaService.DiagnosticarFuerzasAsignadas(vigas)
            If diagMomentos IsNot Nothing Then
                Logger.Warning("Form_09_Vigas.Button1_Click", diagMomentos)
                MessageBox.Show(diagMomentos, "Advertencia — Fuerzas sin asignar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            _vigaService.designVigas(vigas, Proyecto.Elementos.Vigas.Joints)

            _vigaService.AsignarRefuerzoTransversalAutomatico(vigas)

            If Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Count > 0 Then
                _vigaService.CalcularEnvolventeCortante(vigas,
                                                        Proyecto.Elementos.Vigas.BeamForces,
                                                        New HashSet(Of String)(
                                                            Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Select(Function(c) NormalizarClaveCombo(c)),
                                                            StringComparer.OrdinalIgnoreCase))
                _vigaService.CalcularCapacidadCortante(vigas)
            End If

            TriggerCortantePlastico(vigas)

            Dim stories As List(Of String) = Frames.Select(Function(f) f.Story).Distinct().OrderBy(Function(s) s).ToList()

            Lista_Pisos.DataSource = Nothing
            Lista_Pisos.DataSource = stories

            ' Mostrar solo las vigas del primer piso en la lista
            Dim pisoInicial As String = If(stories.Count > 0, stories(0), "")
            Dim vigasPisoI = If(pisoInicial <> "",
                                vigas.Where(Function(v) v.Piso = pisoInicial).ToList(),
                                vigas)

            Lista_Vigas.DataSource = Nothing
            Lista_Vigas.DataSource = vigasPisoI
            Lista_Vigas.DisplayMember = "NombreDisplay"

            If vigasPisoI.Count > 0 Then Lista_Vigas.SelectedIndex = 0

            _cargando = False

            ' Carga manual inicial (el evento SelectedIndexChanged está bloqueado por _cargando)
            Dim vigaSel As cViga = TryCast(Lista_Vigas.SelectedItem, cViga)
            If vigaSel IsNot Nothing Then
                _vigaActual = vigaSel
                _cargando = True
                Nombre_Viga.Text = If(String.IsNullOrWhiteSpace(vigaSel.NombrePlano), vigaSel.Nombre, vigaSel.NombrePlano)
                _cargando = False

                Dim grids = Proyecto?.Elementos?.Grids?.GridLines
                If grids IsNot Nothing Then
                    _DiagramaService.DibujarPlanta(PictureBox1, _vigas, _joints, grids,
                                                   Lista_Pisos.SelectedItem?.ToString(), vigaSel)
                End If
                CargarVigaCompleta(vigaSel)
            End If

            MessageBox.Show("Proceso finalizado correctamente", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

        End Try

    End Sub

    Private Sub Lista_Pisos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Lista_Pisos.SelectedIndexChanged

        If _cargando Then Exit Sub
        If Me.DesignMode Then Return
        If Lista_Pisos.SelectedItem Is Nothing Then Exit Sub
        If _vigas Is Nothing OrElse _vigas.Count = 0 Then Exit Sub
        If _joints Is Nothing OrElse _joints.Count = 0 Then Exit Sub

        Dim pisoSel = Lista_Pisos.SelectedItem.ToString()

        ' Filtrar lista de vigas al piso seleccionado
        Dim vigasPiso = _vigas.Where(Function(v) v.Piso = pisoSel).ToList()
        _cargando = True
        Lista_Vigas.DataSource = Nothing
        Lista_Vigas.DataSource = vigasPiso
        Lista_Vigas.DisplayMember = "NombreDisplay"
        If vigasPiso.Count > 0 Then Lista_Vigas.SelectedIndex = 0
        _cargando = False

        Dim vigaSel = TryCast(Lista_Vigas.SelectedItem, cViga)
        If vigaSel Is Nothing Then Exit Sub

        _vigaActual = vigaSel
        _cargando = True
        Nombre_Viga.Text = If(String.IsNullOrWhiteSpace(vigaSel.NombrePlano), vigaSel.Nombre, vigaSel.NombrePlano)
        _cargando = False

        Dim grids = Proyecto?.Elementos?.Grids?.GridLines
        If grids IsNot Nothing Then
            _DiagramaService.DibujarPlanta(PictureBox1, _vigas, _joints, grids, pisoSel, vigaSel)
        End If
        CargarVigaCompleta(vigaSel)

    End Sub

    Private Sub ImportarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ImportarDemandasToolStripMenuItem.Click

        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivo de resultados ETABS"
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*"
            .Multiselect = False

            If .ShowDialog() = DialogResult.OK Then
                Dim path As String = .FileName
                Me.Cursor = Cursors.WaitCursor

                Try
                    ' Detectar hojas disponibles (E23 vs E17)
                    Dim hojas = ObtenerHojasExcel(path)

                    ' Vigas siempre reimporta su propia geometría — independiente de otros módulos
                    Dim hJoints = ResolverNombreHoja(hojas, "Objects and Elements - Joints", "Joint Coordinates")
                    Dim hFrames = ResolverNombreHoja(hojas, "Objects and Elements - Frames", "Connectivity - Frame")
                    Proyecto.TablasEtabs.TablaOEJoints = LeerHojaExcel(path, hJoints)
                    Proyecto.TablasEtabs.TablaOEFrames = LeerHojaExcel(path, hFrames)

                    Proyecto.Elementos.Vigas.Joints = DataTableToJoints(Proyecto.TablasEtabs.TablaOEJoints)
                    Proyecto.Elementos.Vigas.Frames = DataTableToFrames(Proyecto.TablasEtabs.TablaOEFrames)

                    Dim hAsigFrame = ResolverNombreHoja(hojas, "Frame Assigns - Sect Prop", "Frame Assignments - Sections")
                    Dim hSecDef = ResolverNombreHoja(hojas, "Frame Sec Def - Conc Rect", "Frame Sections")
                    Dim hMaterial = ResolverNombreHoja(hojas, "Mat Prop - Concrete Data", "Material Properties - Concrete")

                    Dim Data_Asig_Frame As DataTable = LeerHojaExcel(path, hAsigFrame)
                    Dim Data_Frame_Section As DataTable = LeerHojaExcel(path, hSecDef)
                    Dim Data_Material_Concrete As DataTable = LeerHojaExcel(path, hMaterial)

                    DataTableToAsignFrame(Proyecto.Elementos.Vigas.Frames, Data_Asig_Frame, Data_Frame_Section, Data_Material_Concrete)

                    Dim hBeamForces = ResolverNombreHoja(hojas, "Element Forces - Beams", "Beam Forces")
                    Dim posibleTruncamiento As Boolean = False
                    Proyecto.Elementos.Vigas.BeamForces = CargarBeamForcesDesdeExcel(path, hBeamForces, posibleTruncamiento)
                    Proyecto.Elementos.Vigas.Tabla_BeamForces = Nothing  ' liberar memoria

                    If posibleTruncamiento Then
                        MessageBox.Show(
                            "Advertencia: la hoja de fuerzas de vigas está cerca del límite de 1.048.576 filas de Excel." &
                            vbCrLf & vbCrLf &
                            "Es probable que los resultados de las combinaciones de diseño (MAX/MIN) hayan quedado cortados." &
                            vbCrLf & vbCrLf &
                            "Para obtener datos completos, re-exporte desde ETABS seleccionando únicamente las combinaciones de " &
                            "envolvente de diseño en la configuración de salida (Output Options).",
                            "Datos de vigas posiblemente incompletos",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If

                    Dim hGrids = ResolverNombreHoja(hojas, "Grid Definitions - Grid Lines", "Grid Lines")
                    Dim Table_Grids As DataTable = LeerHojaExcel(path, hGrids)

                    Proyecto.Elementos.Grids.GridLines = DataTableToGridLines(Table_Grids)


                    ' 🔹 Extraer combinaciones únicas usando clave canónica (Output Case + Step Type)
                    Proyecto.Elementos.Vigas.Lista_Combinaciones = Proyecto.Elementos.Vigas.BeamForces.Select(Function(r) r.LoadCaseKey) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

                    ' Flexión — pre-popula selecciones anteriores
                    Dim formDiseno As New Form_Opciones_Combinaciones()
                    For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
                        If Not Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Contains(comb) Then
                            formDiseno.Lista_Combinaciones.Items.Add(comb)
                        End If
                    Next
                    For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones_Design
                        formDiseno.Lista_Cargas_Design.Items.Add(comb)
                    Next
                    formDiseno.OpcionLlamado = "Vigas"
                    formDiseno.GroupBox2.Text = "Combinaciones Diseño a Flexión de Vigas"
                    formDiseno.ShowDialog()

                    ' Cortante — pre-popula selecciones anteriores
                    Dim formCortante As New Form_Opciones_Combinaciones()
                    For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
                        If Not Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Contains(comb) Then
                            formCortante.Lista_Combinaciones.Items.Add(comb)
                        End If
                    Next
                    For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante
                        formCortante.Lista_Cargas_Design.Items.Add(comb)
                    Next
                    formCortante.OpcionLlamado = "VigasCortante"
                    formCortante.GroupBox2.Text = "Combinaciones Diseño a Cortante de Vigas"
                    formCortante.ShowDialog()

                    ' Cortante Plástico — pre-popula selecciones anteriores
                    Dim formPlastico As New Form_Opciones_Combinaciones()
                    For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
                        If Not Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico.Contains(comb) Then
                            formPlastico.Lista_Combinaciones.Items.Add(comb)
                        End If
                    Next
                    For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico
                        formPlastico.Lista_Cargas_Design.Items.Add(comb)
                    Next
                    formPlastico.OpcionLlamado = "CortantePlastico"
                    formPlastico.GroupBox2.Text = "Combinación Gravitacional — Cortante Plástico (wu)"
                    formPlastico.ShowDialog()

                    ' Filtro de tipos de sección
                    Dim seccionesActuales = Proyecto.Elementos.Vigas.SeccionesSeleccionadas
                    Dim seccionesNuevas As List(Of String) = Nothing
                    Form_FiltroSecciones.Mostrar(Proyecto.Elementos.Vigas.Frames,
                                                 seccionesActuales,
                                                 seccionesNuevas)
                    If seccionesNuevas IsNot Nothing Then
                        Proyecto.Elementos.Vigas.SeccionesSeleccionadas = seccionesNuevas
                    End If

                    HayCambios = True
                    MsgBox("Importación completada.", MsgBoxStyle.Information)

                Catch ex As Exception
                    MsgBox("Error al importar: " & ex.Message, MsgBoxStyle.Critical)
                Finally
                    Me.Cursor = Cursors.Arrow
                End Try
            End If
        End With


    End Sub

    ''' Actualiza solo las fuerzas de vigas (BeamForces) desde un nuevo Excel de ETABS,
    ''' sin modificar Joints, Frames, secciones, agrupaciones ni refuerzo asignado.
    Private Sub ActualizarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ActualizarDemandasToolStripMenuItem.Click

        If Proyecto.Elementos.Vigas.Frames Is Nothing OrElse Proyecto.Elementos.Vigas.Frames.Count = 0 Then
            MessageBox.Show("Primero debe importar el modelo (Importar → Importar Demandas) para contar con la geometría de frames.",
                            "Sin geometría", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim openFD As New OpenFileDialog() With {
            .Title = "Seleccionar Excel ETABS con fuerzas actualizadas",
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*"
        }
        If openFD.ShowDialog() <> DialogResult.OK Then Return

        Dim path As String = openFD.FileName
        Me.Cursor = Cursors.WaitCursor

        Try
            Dim hojas = ObtenerHojasExcel(path)
            Dim hBeamForces = ResolverNombreHoja(hojas, "Element Forces - Beams", "Beam Forces")

            Dim posibleTruncamiento As Boolean = False
            Dim nuevasForces = CargarBeamForcesDesdeExcel(path, hBeamForces, posibleTruncamiento)

            ' Actualizar solo BeamForces — Joints/Frames/Grupos/Refuerzo intactos
            Proyecto.Elementos.Vigas.BeamForces = nuevasForces
            Proyecto.Elementos.Vigas.Tabla_BeamForces = Nothing

            ' Reconstruir lista de combinaciones desde los nuevos datos
            Dim nuevasCombos = nuevasForces.Select(Function(r) r.LoadCaseKey) _
                                           .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                           .Distinct() _
                                           .OrderBy(Function(x) x) _
                                           .ToList()
            Proyecto.Elementos.Vigas.Lista_Combinaciones = nuevasCombos

            ' Advertir si alguna combinación seleccionada ya no está en los nuevos datos
            Dim combsSet As New HashSet(Of String)(nuevasCombos, StringComparer.OrdinalIgnoreCase)
            Dim perdidas As New List(Of String)()
            For Each c In Proyecto.Elementos.Vigas.Lista_Combinaciones_Design
                If Not combsSet.Contains(c) Then perdidas.Add("Flexión: " & c)
            Next
            For Each c In Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante
                If Not combsSet.Contains(c) Then perdidas.Add("Cortante: " & c)
            Next
            For Each c In Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico
                If Not combsSet.Contains(c) Then perdidas.Add("C.Plástico: " & c)
            Next

            If posibleTruncamiento Then
                MessageBox.Show("Advertencia: la hoja de fuerzas está cerca del límite de Excel (1.048.576 filas). " &
                                "Los resultados MAX/MIN pueden estar cortados. Filtre las combinaciones en ETABS antes de exportar.",
                                "Datos posiblemente incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            If perdidas.Count > 0 Then
                MessageBox.Show("Las siguientes combinaciones seleccionadas no están en el nuevo archivo:" & vbCrLf &
                                String.Join(vbCrLf, perdidas) & vbCrLf & vbCrLf &
                                "Vaya a Combinaciones para actualizar la selección.",
                                "Combinaciones no encontradas", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            HayCambios = True

            Dim resp = MessageBox.Show(
                "Fuerzas actualizadas correctamente." & vbCrLf & vbCrLf &
                "¿Desea recalcular ahora con las demandas actualizadas?" & vbCrLf &
                "(Equivale a presionar 'Procesar'. El refuerzo asignado y los grupos se conservan.)",
                "Actualizar Demandas", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If resp = DialogResult.Yes Then
                Button1_Click(Nothing, EventArgs.Empty)
            End If

        Catch ex As Exception
            MessageBox.Show("Error al actualizar demandas: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Arrow
        End Try

    End Sub

    Private Sub CargarVigaCompleta(viga As cViga)

        If viga Is Nothing Then Exit Sub

        Dim combinaciones = New HashSet(Of String)(
            Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Select(Function(c) NormalizarClaveCombo(c)),
            StringComparer.OrdinalIgnoreCase)

        Dim beamForces = Proyecto.Elementos.Vigas.BeamForces _
            .Where(Function(bf) combinaciones.Contains(bf.LoadCaseKey)) _
            .ToList()

        ' Dibujos (fuera del SuspendLayout para no bloquear el render)
        _DiagramaService.DibujarDiagramaMomentoFrames(viga, Diagrama_Momento, beamForces, combinaciones)

        If Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Count > 0 Then
            Dim combsCortante_cv = New HashSet(Of String)(
                Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Select(Function(c) NormalizarClaveCombo(c)),
                StringComparer.OrdinalIgnoreCase)
            Dim bfCortante = Proyecto.Elementos.Vigas.BeamForces _
                .Where(Function(bf) combsCortante_cv.Contains(bf.LoadCaseKey)) _
                .ToList()
            _DiagramaService.DibujarDiagramaCortanteFrames(viga, Diagrama_Cortante, bfCortante, combsCortante_cv)
        End If

        ' Todas las operaciones de tabla en un solo bloque suspendido
        Tabla_Demandas.SuspendLayout()
        Ref_Superior.SuspendLayout()
        Ref_Inferior.SuspendLayout()
        Tabla_Resultados_Flexion.SuspendLayout()
        Ref_Transversal.SuspendLayout()
        Tabla_Resultados_Cortante.SuspendLayout()

        ConstruirTablaResumen(viga, Tabla_Demandas)
        ConstruirTablaRefuerzo(viga, Ref_Superior)
        ConstruirTablaRefuerzo(viga, Ref_Inferior)
        ConstruirTablaResultadosFlexion(viga, Tabla_Resultados_Flexion)
        ConstruirTablaRefuerzoTransversal(viga, Ref_Transversal)
        ConstruirTablaCortante(viga, Tabla_Resultados_Cortante)

        LlenarTablaResumen(viga, Tabla_Demandas)
        LlenarTablaResultados(viga, Tabla_Resultados_Flexion)

        CargarRefuerzoTabla(viga, Ref_Superior, eTipoRefuerzo.Superior)
        CargarRefuerzoTabla(viga, Ref_Inferior, eTipoRefuerzo.Inferior)
        CargarRefuerzoTransversalTabla(viga, Ref_Transversal)

        Tabla_Demandas.ResumeLayout(False)
        Ref_Superior.ResumeLayout(False)
        Ref_Inferior.ResumeLayout(False)
        Tabla_Resultados_Flexion.ResumeLayout(False)
        Ref_Transversal.ResumeLayout(False)
        Tabla_Resultados_Cortante.ResumeLayout(False)

        If viga.Frames.Any(Function(f) f.RevisionCortante.Count > 0) Then
            MostrarResultadosCortante(viga)
        End If

        If ExisteRefuerzo(viga) Then
            ' Limpiar primero para evitar acumulación de tramos duplicados en cada navegación
            For Each f In viga.Frames
                f.RefuerzoSuperior.Clear()
                f.RefuerzoInferior.Clear()
            Next
            Dim datosSup = ExtraerRefuerzoDesdeGrid(Ref_Superior)
            Dim datosInf = ExtraerRefuerzoDesdeGrid(Ref_Inferior)
            _vigaService.GuardarRefuerzo(viga, datosSup, eTipoRefuerzo.Superior)
            _vigaService.GuardarRefuerzo(viga, datosInf, eTipoRefuerzo.Inferior)
            _vigaService.CalcularFlexionViga(viga)
            MostrarResultadosFlexion(viga)
        End If

        ActualizarBotonReplica(viga)

    End Sub

    ''' Actualiza texto y visibilidad de los botones de grupo según el rol de la viga.
    Private Sub ActualizarBotonReplica(viga As cViga)
        If viga Is Nothing Then
            Boton_Replicar.Text = "Crear Grupo de Réplica"
            Boton_VerGrupo.Visible = False
            CentrarBotonesRefuerzo()
            Return
        End If
        If viga.EsPatronGrupo Then
            Dim grupo = Proyecto?.Elementos?.Vigas?.GruposReplica?.FirstOrDefault(
                Function(g) g.ID = viga.GrupoReplicaID)
            Dim nSim = If(grupo IsNot Nothing, grupo.Similares.Count, 0)
            Boton_Replicar.Text = $"Propagar Refuerzo → {nSim} Similar{If(nSim = 1, "", "es")}"
            Boton_VerGrupo.Visible = True
        ElseIf Not String.IsNullOrEmpty(viga.GrupoReplicaID) Then
            Boton_Replicar.Text = "Ver Patrón del Grupo"
            Boton_VerGrupo.Visible = True
        Else
            Boton_Replicar.Text = "Crear Grupo de Réplica"
            Boton_VerGrupo.Visible = False
        End If
        CentrarBotonesRefuerzo()
    End Sub

    ''' Cuando el usuario edita refuerzo en el patrón, marca todos los similares como desincronizados.
    Private Sub MarcarSimilaresDesincronizados()
        If _vigaActual Is Nothing OrElse Not _vigaActual.EsPatronGrupo Then Return
        If _vigas Is Nothing Then Return
        Dim similares = _vigas.Where(
            Function(v) v.GrupoReplicaID = _vigaActual.GrupoReplicaID AndAlso Not v.EsPatronGrupo).ToList()
        If similares.Count = 0 Then Return
        For Each sim In similares
            sim.RefuerzoDesincronizado = True
        Next
        ActualizarBotonReplica(_vigaActual)
    End Sub

    Private Function ExisteRefuerzo(viga As cViga) As Boolean

        For Each f In viga.Frames
            If f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any() Then
                Return True
            End If
        Next

        Return False

    End Function

    Private Sub CargarRefuerzoTabla(viga As cViga,
                               dgv As DataGridView,
                               tipo As eTipoRefuerzo)

        ' Limpiar tabla primero
        For Each row As DataGridViewRow In dgv.Rows
            For Each cell As DataGridViewCell In row.Cells
                cell.Value = Nothing
            Next
        Next

        For col As Integer = 0 To dgv.Columns.Count - 1

            Dim header As String = dgv.Columns(col).HeaderText
            Dim partes = header.Split({vbCrLf}, StringSplitOptions.None)

            If partes.Length < 2 Then Continue For

            Dim frameLabel As String = partes(0).Trim()
            Dim posicionTexto As String = partes(1).Trim()

            Dim posicion As PosicionTramoViga

            Select Case posicionTexto
                Case "Izq"
                    posicion = PosicionTramoViga.Izquierda
                Case "Centro"
                    posicion = PosicionTramoViga.Centro
                Case "Der"
                    posicion = PosicionTramoViga.Derecha
                Case Else
                    Continue For
            End Select

            ' Buscar frame
            Dim frame = viga.Frames.Find(Function(f) f.ObjectLabel = frameLabel)
            If frame Is Nothing Then Continue For

            ' Seleccionar lista correcta
            Dim lista As List(Of cRefuerzoTramo)

            If tipo = eTipoRefuerzo.Superior Then
                lista = frame.RefuerzoSuperior
            Else
                lista = frame.RefuerzoInferior
            End If

            If lista Is Nothing OrElse lista.Count = 0 Then Continue For

            ' Buscar tramo por posición
            Dim tramo = lista.Find(Function(t) t.Posicion = posicion)
            If tramo Is Nothing Then Continue For

            ' ============================
            ' Llenar las barras en la tabla
            ' ============================
            For row As Integer = 0 To dgv.Rows.Count - 1

                Dim barra As String = dgv.Rows(row).HeaderCell.Value.ToString()

                If tramo.Barras.ContainsKey(barra) Then
                    dgv.Rows(row).Cells(col).Value = tramo.Barras(barra)
                End If

            Next

        Next

    End Sub


    Private Sub Ref_Superior_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles Ref_Superior.CellValueChanged

        If e.RowIndex < 0 Or e.ColumnIndex < 0 Then Exit Sub

        Dim dgv As DataGridView = CType(sender, DataGridView)
        Dim cell = dgv.Rows(e.RowIndex).Cells(e.ColumnIndex)

        Dim valor As Integer

        If Integer.TryParse(Convert.ToString(cell.Value), valor) Then

            If valor > 0 Then
                cell.Style.BackColor = ColorTranslator.FromHtml("#E2EFDA")
                cell.Style.ForeColor = ColorTranslator.FromHtml("#FF0000")
            Else
                cell.Style.BackColor = Color.White
                cell.Style.ForeColor = Color.Black
            End If

        End If

        MarcarSimilaresDesincronizados()

    End Sub

    Private Sub Ref_Superior_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles Ref_Superior.CurrentCellDirtyStateChanged

        If Ref_Superior.IsCurrentCellDirty Then
            Ref_Superior.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If

    End Sub

    Private Sub Ref_Inferior_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles Ref_Inferior.CurrentCellDirtyStateChanged

        If Ref_Inferior.IsCurrentCellDirty Then
            Ref_Inferior.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If

    End Sub

    Private Sub Ref_Inferior_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles Ref_Inferior.CellValueChanged

        If e.RowIndex < 0 Or e.ColumnIndex < 0 Then Exit Sub

        Dim dgv As DataGridView = CType(sender, DataGridView)
        Dim cell = dgv.Rows(e.RowIndex).Cells(e.ColumnIndex)

        Dim valor As Integer

        If Integer.TryParse(Convert.ToString(cell.Value), valor) Then

            If valor > 0 Then
                cell.Style.BackColor = ColorTranslator.FromHtml("#E2EFDA")
                cell.Style.ForeColor = ColorTranslator.FromHtml("#FF0000")
            Else
                cell.Style.BackColor = Color.White
                cell.Style.ForeColor = Color.Black
            End If

        End If

        MarcarSimilaresDesincronizados()

    End Sub

    Public Sub ActivarCopiarPegar(dgv As DataGridView)

        AddHandler dgv.KeyDown, AddressOf DataGrid_KeyDown
        AddHandler dgv.CurrentCellDirtyStateChanged, AddressOf DataGrid_CurrentCellDirtyStateChanged

        dgv.MultiSelect = True
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect

    End Sub

    Private Sub DataGrid_KeyDown(sender As Object, e As KeyEventArgs)

        Dim dgv As DataGridView = CType(sender, DataGridView)

        ' COPIAR
        If e.Control AndAlso e.KeyCode = Keys.C Then

            If dgv.CurrentCell IsNot Nothing Then
                Clipboard.SetText(Convert.ToString(dgv.CurrentCell.Value))
            End If

            e.Handled = True
        End If

        ' PEGAR
        If e.Control AndAlso e.KeyCode = Keys.V Then

            If dgv.SelectedCells.Count = 0 Then Exit Sub

            Dim texto As String = Clipboard.GetText()
            Dim valor As Integer

            If Not Integer.TryParse(texto, valor) Then Exit Sub

            For Each cell As DataGridViewCell In dgv.SelectedCells
                cell.Value = valor
            Next

            e.Handled = True
        End If

    End Sub

    Private Sub DataGrid_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs)

        Dim dgv As DataGridView = CType(sender, DataGridView)

        If dgv.IsCurrentCellDirty Then
            dgv.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If

    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        _vigaService = New VigaService(_geo)
        _DiagramaService = New DiagramaService(_geo)

        ActivarCopiarPegar(Ref_Inferior)
        ActivarCopiarPegar(Ref_Superior)
        CentrarBotonesRefuerzo()
        TimerAutoSave.Start()

        Dim menuVista As New ToolStripMenuItem("Vista Interactiva Planta")
        menuVista.ForeColor = Color.White
        menuVista.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuVista.Click, AddressOf AbrirVistaInteractiva
        OpcionesToolStripMenuItem.DropDownItems.Add(menuVista)

        Dim menuFiltroSec As New ToolStripMenuItem("Filtro de Secciones...")
        menuFiltroSec.ForeColor = Color.White
        menuFiltroSec.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuFiltroSec.Click, AddressOf AbrirFiltroSecciones
        OpcionesToolStripMenuItem.DropDownItems.Add(menuFiltroSec)

        Dim menuAgrupacion As New ToolStripMenuItem("Editar Agrupación de Viga...")
        menuAgrupacion.ForeColor = Color.White
        menuAgrupacion.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuAgrupacion.Click, AddressOf AbrirEditorAgrupacion
        OpcionesToolStripMenuItem.DropDownItems.Add(menuAgrupacion)

        OpcionesToolStripMenuItem.DropDownItems.Add(New ToolStripSeparator())

        Dim menuPrefijo As New ToolStripMenuItem("Prefijo de Nombre de Viga...")
        menuPrefijo.ForeColor = Color.White
        menuPrefijo.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuPrefijo.Click, AddressOf ConfigurarPrefijoViga
        OpcionesToolStripMenuItem.DropDownItems.Add(menuPrefijo)

        Dim menuEjes As New ToolStripMenuItem("Definir Ejes Estructurales...")
        menuEjes.ForeColor = Color.White
        menuEjes.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuEjes.Click, AddressOf AbrirEditorEjesManual
        OpcionesToolStripMenuItem.DropDownItems.Add(menuEjes)

        Dim menuActualizar As New ToolStripMenuItem("Actualizar Nombres por Eje")
        menuActualizar.ForeColor = Color.White
        menuActualizar.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuActualizar.Click, AddressOf ActualizarNombresPorEje_Click
        OpcionesToolStripMenuItem.DropDownItems.Add(menuActualizar)

        Dim menuReportes As New ToolStripMenuItem("Reportes")
        menuReportes.ForeColor = Color.White
        menuReportes.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuReportes.Click, AddressOf AbrirReportes
        MenuStrip1.Items.Add(menuReportes)

        ' ── Nivel de Disipación (DMO / DES) ──────────────────────────────────
        ' Declarar ambas variables antes de asignar handlers (los lambdas se capturan mutuamente)
        Dim menuDisipacion As New ToolStripMenuItem("Disipación: DMO")
        menuDisipacion.ForeColor = Color.White
        menuDisipacion.BackColor = Color.FromArgb(87, 87, 87)
        menuDisipacion.Name = "MenuDisipacion"

        Dim itemDMO As New ToolStripMenuItem("DMO  (fy × 1.0)")
        itemDMO.BackColor = Color.FromArgb(87, 87, 87)
        itemDMO.ForeColor = Color.White
        itemDMO.Checked = True

        Dim itemDES As New ToolStripMenuItem("DES  (fy × 1.25)")
        itemDES.BackColor = Color.FromArgb(87, 87, 87)
        itemDES.ForeColor = Color.White

        AddHandler itemDMO.Click,
            Sub(s, ev)
                Proyecto.Elementos.Vigas.NivelDisipacion = "DMO"
                itemDMO.Checked = True
                itemDES.Checked = False
                menuDisipacion.Text = "Disipación: DMO"
            End Sub

        AddHandler itemDES.Click,
            Sub(s, ev)
                Proyecto.Elementos.Vigas.NivelDisipacion = "DES"
                itemDES.Checked = True
                itemDMO.Checked = False
                menuDisipacion.Text = "Disipación: DES"
            End Sub

        menuDisipacion.DropDownItems.Add(itemDMO)
        menuDisipacion.DropDownItems.Add(itemDES)
        MenuStrip1.Items.Add(menuDisipacion)

        ' ── Combinaciones (recarga sin re-importar) ───────────────────────────
        Dim menuCombinaciones As New ToolStripMenuItem("Combinaciones")
        menuCombinaciones.ForeColor = Color.White
        menuCombinaciones.BackColor = Color.FromArgb(87, 87, 87)

        Dim itemCombDiseno As New ToolStripMenuItem("Diseño a Flexión...")
        itemCombDiseno.BackColor = Color.FromArgb(87, 87, 87)
        itemCombDiseno.ForeColor = Color.White
        AddHandler itemCombDiseno.Click, AddressOf ReseleccionarCombinacionesDiseno

        Dim itemCombCortante As New ToolStripMenuItem("Diseño a Cortante...")
        itemCombCortante.BackColor = Color.FromArgb(87, 87, 87)
        itemCombCortante.ForeColor = Color.White
        AddHandler itemCombCortante.Click, AddressOf ReseleccionarCombinacionesCortante

        Dim itemCombPlastico As New ToolStripMenuItem("Cortante Plástico...")
        itemCombPlastico.BackColor = Color.FromArgb(87, 87, 87)
        itemCombPlastico.ForeColor = Color.White
        AddHandler itemCombPlastico.Click, AddressOf ReseleccionarCombinacionesPlastico

        menuCombinaciones.DropDownItems.Add(itemCombDiseno)
        menuCombinaciones.DropDownItems.Add(itemCombCortante)
        menuCombinaciones.DropDownItems.Add(itemCombPlastico)
        MenuStrip1.Items.Add(menuCombinaciones)

        ' ── Ayuda: Tablas requeridas ──────────────────────────────────────────
        Dim menuAyuda As New ToolStripMenuItem("? Tablas ETABS")
        menuAyuda.ForeColor = Color.White
        menuAyuda.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuAyuda.Click, Sub(s, ev) Form_AyudaImportacion.MostrarModulo("Vigas")
        MenuStrip1.Items.Add(menuAyuda)

        ' ── TabPage4: Cortante Plástico ───────────────────────────────────────
        CrearTabCortantePlastico()

    End Sub

    ' ── Variables del TabPage4 ────────────────────────────────────────────────
    Private _tabPlastico As TabPage
    Private _dgvPlastico As DataGridView
    Private _lblDisipacionPlastico As Label

    Private Sub CrearTabCortantePlastico()

        _tabPlastico = New TabPage("Cortante Plástico") With {
            .BackColor = SystemColors.Control,
            .Name = "TabPage4"
        }

        ' Panel de información superior
        Dim panelInfo As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 42,
            .BackColor = Color.FromArgb(87, 87, 87),
            .Padding = New Padding(10, 8, 10, 8)
        }

        _lblDisipacionPlastico = New Label() With {
            .AutoSize = True,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(10, 10),
            .Text = "Chequeo por Capacidad — NSR-10 C.21.5.4  |  Nivel de disipación: DMO (fy × 1.0)"
        }
        panelInfo.Controls.Add(_lblDisipacionPlastico)

        ' DataGridView principal
        _dgvPlastico = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            .GridColor = Color.FromArgb(210, 210, 210),
            .ScrollBars = ScrollBars.Both,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .ColumnHeadersHeight = 52,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .EnableHeadersVisualStyles = False
        }
        _dgvPlastico.RowTemplate.Height = 26

        With _dgvPlastico.ColumnHeadersDefaultCellStyle
            .BackColor = Color.FromArgb(87, 87, 87)
            .ForeColor = Color.White
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
            .WrapMode = DataGridViewTriState.True
        End With
        With _dgvPlastico.DefaultCellStyle
            .Font = New Font("Segoe UI", 9)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
            .SelectionBackColor = Color.FromArgb(200, 225, 255)
            .SelectionForeColor = Color.Black
        End With

        ConstruirColumnasCortantePlastico()

        _tabPlastico.Controls.Add(_dgvPlastico)
        _tabPlastico.Controls.Add(panelInfo)

        TabControl1.TabPages.Add(_tabPlastico)

    End Sub

    Private Sub ConstruirColumnasCortantePlastico()

        _dgvPlastico.Columns.Clear()

        Dim cols As (Name As String, Header As String, W As Integer)() = {
            ("Piso", "Piso", 80),
            ("Viga", "Viga", 105),
            ("Frame", "Frame", 75),
            ("b", "b" & vbCrLf & "(m)", 65),
            ("d", "d" & vbCrLf & "(m)", 65),
            ("Ln", "Ln" & vbCrLf & "(m)", 65),
            ("fyFactor", "fy ×", 60),
            ("MnNegIzq", "Mn⁻ izq" & vbCrLf & "(kN·m)", 90),
            ("MnPosIzq", "Mn⁺ izq" & vbCrLf & "(kN·m)", 90),
            ("MnNegDer", "Mn⁻ der" & vbCrLf & "(kN·m)", 90),
            ("MnPosDer", "Mn⁺ der" & vbCrLf & "(kN·m)", 90),
            ("VeA", "Ve_A (→)" & vbCrLf & "(kN)", 80),
            ("VeB", "Ve_B (←)" & vbCrLf & "(kN)", 80),
            ("VgIzq", "Vg izq" & vbCrLf & "(kN)", 75),
            ("VgDer", "Vg der" & vbCrLf & "(kN)", 75),
            ("VuIzq", "Vu izq" & vbCrLf & "(kN)", 80),
            ("VcVsIzq", "Vc+Vs izq" & vbCrLf & "(kN)", 90),
            ("phiVnIzq", "φVn izq" & vbCrLf & "(kN)", 80),
            ("FIzq", "F izq", 65),
            ("CumpleIzq", "✓ izq", 55),
            ("VuDer", "Vu der" & vbCrLf & "(kN)", 80),
            ("VcVsDer", "Vc+Vs der" & vbCrLf & "(kN)", 90),
            ("phiVnDer", "φVn der" & vbCrLf & "(kN)", 80),
            ("FDer", "F der", 65),
            ("CumpleDer", "✓ der", 55)
        }

        For Each c In cols
            Dim col As New DataGridViewTextBoxColumn() With {.Name = c.Name, .HeaderText = c.Header, .Width = c.W}
            _dgvPlastico.Columns.Add(col)
        Next

    End Sub

    ' =========================================================================
    ' CORTANTE PLÁSTICO — Orquestación y tabla
    ' =========================================================================

    Private Sub TriggerCortantePlastico(vigas As List(Of cViga))

        If Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico.Count = 0 Then Return

        Dim fy_factor As Double = If(Proyecto.Elementos.Vigas.NivelDisipacion = "DES", 1.25, 1.0)

        _vigaService.CalcularCortantePlastico(
            vigas,
            Proyecto.Elementos.Vigas.BeamForces,
            New HashSet(Of String)(
                Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico.Select(Function(c) NormalizarClaveCombo(c)),
                StringComparer.OrdinalIgnoreCase),
            fy_factor)

        ActualizarTablaCortantePlastico()

    End Sub

    Private Sub ActualizarTablaCortantePlastico()

        If _dgvPlastico Is Nothing Then Return

        Dim nivelTexto = If(Proyecto.Elementos.Vigas.NivelDisipacion = "DES",
                            "DES (fy × 1.25)", "DMO (fy × 1.0)")
        _lblDisipacionPlastico.Text =
            "Chequeo por Capacidad — NSR-10 C.21.5.4  |  Nivel de disipación: " & nivelTexto

        _dgvPlastico.Rows.Clear()

        If _vigas Is Nothing Then Return

        Dim fmtN2 = "F2"
        Dim idx As Integer = 0

        For Each viga In _vigas

            For Each frame In viga.Frames

                If frame.CortantePlastico Is Nothing Then Continue For

                Dim cp = frame.CortantePlastico
                Dim r = _dgvPlastico.Rows.Add()
                Dim row = _dgvPlastico.Rows(r)

                row.Cells("Piso").Value = viga.Piso
                row.Cells("Viga").Value = If(String.IsNullOrWhiteSpace(viga.NombrePlano), viga.Nombre, viga.NombrePlano)
                row.Cells("Frame").Value = frame.ObjectLabel
                row.Cells("b").Value = Math.Round(frame.Section.b, 3).ToString(fmtN2)
                row.Cells("d").Value = Math.Round(frame.Section.d, 3).ToString(fmtN2)
                row.Cells("Ln").Value = Math.Round(cp.Ln, 3).ToString(fmtN2)
                row.Cells("fyFactor").Value = cp.fy_factor.ToString("F2")
                row.Cells("MnNegIzq").Value = Math.Round(cp.Mn_neg_izq, 1).ToString(fmtN2)
                row.Cells("MnPosIzq").Value = Math.Round(cp.Mn_pos_izq, 1).ToString(fmtN2)
                row.Cells("MnNegDer").Value = Math.Round(cp.Mn_neg_der, 1).ToString(fmtN2)
                row.Cells("MnPosDer").Value = Math.Round(cp.Mn_pos_der, 1).ToString(fmtN2)
                row.Cells("VeA").Value = Math.Round(cp.Ve_A, 1).ToString(fmtN2)
                row.Cells("VeB").Value = Math.Round(cp.Ve_B, 1).ToString(fmtN2)
                row.Cells("VgIzq").Value = Math.Round(cp.ZonaIzq.Vg, 1).ToString(fmtN2)
                row.Cells("VgDer").Value = Math.Round(cp.ZonaDer.Vg, 1).ToString(fmtN2)

                PintarZonaCortantePlastico(row, cp.ZonaIzq, "VuIzq", "VcVsIzq", "phiVnIzq", "FIzq", "CumpleIzq")
                PintarZonaCortantePlastico(row, cp.ZonaDer, "VuDer", "VcVsDer", "phiVnDer", "FDer", "CumpleDer")

                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
                idx += 1

            Next
        Next

        ' Resaltar cabeceras de grupo por viga (primer frame de cada viga → fondo más oscuro)
        Dim vigaAnterior As String = ""
        For Each row As DataGridViewRow In _dgvPlastico.Rows
            Dim nombreViga = Convert.ToString(row.Cells("Viga").Value)
            If nombreViga <> vigaAnterior Then
                row.DefaultCellStyle.BackColor = Color.FromArgb(230, 235, 245)
                row.DefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
                vigaAnterior = nombreViga
            End If
        Next

    End Sub

    Private Sub PintarZonaCortantePlastico(row As DataGridViewRow,
                                            zona As cRevisionCortantePlasticoZona,
                                            colVu As String, colVcVs As String,
                                            colPhiVn As String, colF As String, colCumple As String)

        Dim fmtN2 = "F2"

        row.Cells(colVu).Value = Math.Round(zona.Vu_diseno, 1).ToString(fmtN2)
        row.Cells(colVcVs).Value = Math.Round(zona.Vc + zona.Vs, 1).ToString(fmtN2)
        row.Cells(colPhiVn).Value = Math.Round(zona.phiVn, 1).ToString(fmtN2)

        Dim f = Math.Round(zona.Factor, 2)
        row.Cells(colF).Value = f.ToString(fmtN2)
        row.Cells(colCumple).Value = If(zona.Cumple, "OK", "NO")

        Dim fondoF = If(zona.Cumple, ColorTranslator.FromHtml("#C6EFCE"), ColorTranslator.FromHtml("#FFC7CE"))
        Dim textoF = If(zona.Cumple, ColorTranslator.FromHtml("#006100"), ColorTranslator.FromHtml("#9C0006"))

        row.Cells(colF).Style.BackColor = fondoF
        row.Cells(colF).Style.ForeColor = textoF
        row.Cells(colF).Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        row.Cells(colCumple).Style.BackColor = fondoF
        row.Cells(colCumple).Style.ForeColor = textoF
        row.Cells(colCumple).Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)

    End Sub

    Private Sub AbrirFiltroSecciones(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Vigas.Frames.Count = 0 Then
            MessageBox.Show("Primero importe los datos de ETABS.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim seccionesNuevas As List(Of String) = Nothing
        If Form_FiltroSecciones.Mostrar(Proyecto.Elementos.Vigas.Frames,
                                        Proyecto.Elementos.Vigas.SeccionesSeleccionadas,
                                        seccionesNuevas) Then
            Proyecto.Elementos.Vigas.SeccionesSeleccionadas = seccionesNuevas
            HayCambios = True
            MessageBox.Show($"Filtro actualizado: {seccionesNuevas.Count} sección(es) seleccionada(s)." & vbCrLf &
                            "Haz clic en 'Generar Vigas' para aplicar el nuevo filtro.",
                            "Filtro actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub AbrirReportes(sender As Object, e As EventArgs)

        If _vigas Is Nothing OrElse _vigas.Count = 0 Then
            MessageBox.Show("Primero calcula las vigas.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim form As New Form_Reporte_Resumen()
        form.Vigas = _vigas
        form.Show(Me)

    End Sub

    Private Sub AbrirVistaInteractiva(sender As Object, e As EventArgs)
        If _vigas Is Nothing OrElse _vigas.Count = 0 Then
            MessageBox.Show("Primero carga y calcula las vigas.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim form As New Form_PlantaInteractiva()
        form.Vigas = _vigas
        form.Joints = _joints
        form.GridLines = Proyecto?.Elementos?.Grids?.GridLines
        form.VigaSeleccionada = _vigaActual
        form.PisoActual = If(Lista_Pisos.SelectedItem IsNot Nothing,
                              Lista_Pisos.SelectedItem.ToString(), "")
        AddHandler form.VigaSeleccionadaPorDobleClick, AddressOf SeleccionarVigaDesdePlanta
        form.Show(Me)
    End Sub

    Private Sub SeleccionarVigaDesdePlanta(viga As cViga)
        If viga Is Nothing Then Return
        Me.BringToFront()
        Me.Activate()

        ' Seleccionar el piso en Lista_Pisos (dispara el filtro de Lista_Vigas)
        For i As Integer = 0 To Lista_Pisos.Items.Count - 1
            If Lista_Pisos.Items(i).ToString().Equals(viga.Piso, StringComparison.OrdinalIgnoreCase) Then
                Lista_Pisos.SelectedIndex = i
                Exit For
            End If
        Next

        ' Seleccionar la viga en Lista_Vigas (ya filtrada por piso)
        For i As Integer = 0 To Lista_Vigas.Items.Count - 1
            Dim v = TryCast(Lista_Vigas.Items(i), cViga)
            If v IsNot Nothing AndAlso v.Name_Beam = viga.Name_Beam Then
                Lista_Vigas.SelectedIndex = i
                Exit For
            End If
        Next
    End Sub

    ' =========================================================================
    ' EJES ESTRUCTURALES — EDITOR MANUAL Y ACTUALIZACIÓN DE NOMBRES
    ' =========================================================================

    Private Sub ConfigurarPrefijoViga(sender As Object, e As EventArgs)
        If Proyecto Is Nothing Then Return
        Dim actual = If(String.IsNullOrWhiteSpace(Proyecto.Elementos.Vigas.PrefijoNombreViga),
                        "V", Proyecto.Elementos.Vigas.PrefijoNombreViga)
        Dim nuevo = InputBox(
            "Prefijo para nombres de viga." & vbCrLf & vbCrLf &
            "Ejemplos:  V  →  V-B, V-3" & vbCrLf &
            "           Viga  →  Viga-B, Viga-3" & vbCrLf &
            "           VIGA  →  VIGA-B, VIGA-3",
            "Prefijo de nombre", actual)
        If String.IsNullOrWhiteSpace(nuevo) Then Return
        Proyecto.Elementos.Vigas.PrefijoNombreViga = nuevo.Trim()
        HayCambios = True

        If _vigas IsNot Nothing AndAlso _vigas.Count > 0 Then
            Dim res = MessageBox.Show("¿Actualizar nombres de vigas con el nuevo prefijo ahora?",
                                      "Prefijo actualizado", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If res = DialogResult.Yes Then ActualizarNombresPorEje()
        End If
    End Sub

    Private Sub AbrirEditorEjesManual(sender As Object, e As EventArgs)
        If Proyecto Is Nothing Then
            MessageBox.Show("Abra o importe un proyecto primero.", "Sin proyecto",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim gridsActuales = If(Proyecto.Elementos?.Grids?.GridLines, New List(Of cGridLine)())
        Dim resultado As List(Of cGridLine) = Nothing
        If Not Form_DefinirEjesManual.Mostrar(gridsActuales, resultado) Then Return

        Proyecto.Elementos.Grids.GridLines = resultado
        HayCambios = True

        Dim res = MessageBox.Show(
            "Ejes guardados correctamente." & vbCrLf & vbCrLf &
            "¿Desea actualizar los nombres de las vigas con los nuevos ejes ahora?",
            "Ejes definidos", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If res = DialogResult.Yes Then ActualizarNombresPorEje()
    End Sub

    Private Sub ActualizarNombresPorEje_Click(sender As Object, e As EventArgs)
        ActualizarNombresPorEje()
    End Sub

    ''' Reasigna EjeApoyo_I/J, EjeParalelo y NombrePlano a todas las vigas cargadas.
    ''' Útil después de definir ejes manualmente o al abrir proyectos viejos.
    Private Sub ActualizarNombresPorEje()
        If _vigas Is Nothing OrElse _vigas.Count = 0 Then
            MessageBox.Show("No hay vigas cargadas. Use 'Calcular' primero.", "Sin vigas",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim grids = Proyecto?.Elementos?.Grids?.GridLines
        If grids Is Nothing OrElse grids.Count = 0 Then
            MessageBox.Show(
                "No hay ejes definidos." & vbCrLf & vbCrLf &
                "Use 'Opciones → Definir Ejes Estructurales...' para ingresarlos manualmente," & vbCrLf &
                "o reimporte desde ETABS incluyendo la hoja 'Grid Definitions - Grid Lines'.",
                "Sin ejes", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If _joints Is Nothing OrElse _joints.Count = 0 Then
            MessageBox.Show("No hay geometría cargada. Recalcule las vigas primero.",
                            "Sin geometría", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Me.Cursor = Cursors.WaitCursor
        Try
            _geo.AsignarEjesAVigas(_vigas, grids, _joints)
            _geo.AsignarEjesParalelosAVigas(_vigas, grids, _joints)
            _vigaService.GenerarNombresPlano(_vigas, Proyecto.Elementos.Vigas.PrefijoNombreViga)

            Dim pisoSel = Lista_Pisos.SelectedItem?.ToString()
            Dim vigasPiso = If(Not String.IsNullOrEmpty(pisoSel),
                _vigas.Where(Function(v) v.Piso = pisoSel).ToList(), _vigas)

            _cargando = True
            Lista_Vigas.DataSource = Nothing
            Lista_Vigas.DataSource = vigasPiso
            Lista_Vigas.DisplayMember = "NombreDisplay"
            _cargando = False

            If _vigaActual IsNot Nothing Then
                Dim idx = vigasPiso.IndexOf(_vigaActual)
                If idx >= 0 Then Lista_Vigas.SelectedIndex = idx
                _cargando = True
                Nombre_Viga.Text = If(String.IsNullOrWhiteSpace(_vigaActual.NombrePlano),
                                      _vigaActual.Nombre, _vigaActual.NombrePlano)
                _cargando = False
            End If

            Proyecto.Elementos.Vigas.Vigas = _vigas
            HayCambios = True
            MessageBox.Show("Nombres actualizados correctamente.", "Listo",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al actualizar: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
    End Sub

    ' =========================================================================
    ' AGRUPACIÓN MANUAL DE VIGAS
    ' =========================================================================

    Private Sub AbrirEditorAgrupacion(sender As Object, e As EventArgs)

        If _vigas Is Nothing OrElse _vigas.Count = 0 Then
            MessageBox.Show("Ejecute el cálculo de vigas primero.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If _vigaActual Is Nothing Then
            MessageBox.Show("Seleccione una viga en la lista antes de editar su agrupación.",
                            "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim piso = _vigaActual.Piso

        ' Abrir vista interactiva en modo agrupación
        Dim frmPlanta As New Form_PlantaInteractiva()
        frmPlanta.Vigas = _vigas
        frmPlanta.Joints = _joints
        frmPlanta.GridLines = Proyecto?.Elementos?.Grids?.GridLines
        frmPlanta.PisoActual = piso
        frmPlanta.VigaEditada = _vigaActual
        frmPlanta.Text = $"Agrupación Manual — {_vigaActual.Nombre}  |  Piso: {piso}"
        frmPlanta.ModoAgrupacion = True   ' pre-selecciona los frames de la viga y activa barra

        If frmPlanta.ShowDialog(Me) <> DialogResult.OK Then Return

        Dim nuevosLabels = New HashSet(Of String)(frmPlanta.FramesResultantes)
        Dim labelsOriginales = New HashSet(Of String)(_vigaActual.Frames.Select(Function(f) f.ObjectLabel))

        Dim labelsAgregar = nuevosLabels.Except(labelsOriginales).ToList()
        Dim labelsQuitar = labelsOriginales.Except(nuevosLabels).ToList()

        If labelsAgregar.Count = 0 AndAlso labelsQuitar.Count = 0 Then Return

        ' Mover frames desde otras vigas hacia la viga actual (mismo piso solamente)
        For Each label In labelsAgregar
            Dim srcViga = _vigas.FirstOrDefault(
                Function(v) v.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase) AndAlso
                            v.Frames.Any(Function(f) f.ObjectLabel = label))
            If srcViga Is Nothing Then Continue For
            Dim frame = srcViga.Frames.First(Function(f) f.ObjectLabel = label)
            srcViga.Frames.Remove(frame)
            _vigaActual.Frames.Add(frame)
        Next

        ' Retirar frames de la viga actual; cada uno queda en su propia viga residual
        For Each label In labelsQuitar
            Dim frame = _vigaActual.Frames.FirstOrDefault(Function(f) f.ObjectLabel = label)
            If frame Is Nothing Then Continue For
            _vigaActual.Frames.Remove(frame)

            Dim residual As New cViga With {
                .Piso = frame.Story,
                .Nombre = "VIGA-TMP",
                .Name_Beam = "VIGA-TMP"
            }
            residual.Frames.Add(frame)
            Dim dir = _geo.VectorFrame(frame, _joints)
            dir.Normalize()
            residual.Direccion = dir
            _vigas.Add(residual)
        Next

        ' Eliminar vigas que quedaron sin frames
        _vigas.RemoveAll(Function(v) v.Frames.Count = 0)

        ' Renumerar todas las vigas
        For i = 0 To _vigas.Count - 1
            _vigas(i).Nombre = "VIGA-" & (i + 1)
            _vigas(i).Name_Beam = _vigas(i).Nombre
        Next

        ' Re-ordenar la viga editada
        _vigaService.OrdenarFramesViga(_vigaActual, _joints)

        ' Persistir el grupo manual (reemplaza si ya existía para esta viga)
        Dim gruposManual = Proyecto.Elementos.Vigas.GruposManual
        Dim currentLabels = _vigaActual.Frames.Select(Function(f) f.ObjectLabel).ToList()
        gruposManual.RemoveAll(Function(g) g.Any(Function(lbl) currentLabels.Contains(lbl)))
        If currentLabels.Count > 0 Then
            gruposManual.Add(currentLabels)
        End If

        ' Recalcular todas las vigas afectadas
        RecalcularListaVigas(_vigas)

        ' Re-asignar eje paralelo y regenerar nombres tras la reagrupación
        Dim gridsEjesM = Proyecto?.Elementos?.Grids?.GridLines
        If gridsEjesM IsNot Nothing AndAlso gridsEjesM.Count > 0 Then
            _geo.AsignarEjesParalelosAVigas(_vigas, gridsEjesM, _joints)
        End If
        _vigaService.GenerarNombresPlano(_vigas, Proyecto.Elementos.Vigas.PrefijoNombreViga)

        ' Actualizar lista en UI (solo vigas del piso activo)
        Dim pisoActivo = Lista_Pisos.SelectedItem?.ToString()
        Dim vigasActivas = If(Not String.IsNullOrEmpty(pisoActivo),
            _vigas.Where(Function(v) v.Piso = pisoActivo).ToList(), _vigas)
        _cargando = True
        Lista_Vigas.DataSource = Nothing
        Lista_Vigas.DataSource = vigasActivas
        Lista_Vigas.DisplayMember = "NombreDisplay"
        _cargando = False

        Proyecto.Elementos.Vigas.Vigas = _vigas
        HayCambios = True

        ' Reseleccionar la viga editada
        Dim idx = vigasActivas.IndexOf(_vigaActual)
        If idx >= 0 Then Lista_Vigas.SelectedIndex = idx

    End Sub

    ''' <summary>
    ''' Recalcula envolventes, diseño, refuerzo transversal y cortante para la lista dada.
    ''' Útil después de modificar agrupaciones manualmente sin re-ejecutar todo el Calcular.
    ''' </summary>
    Private Sub RecalcularListaVigas(vigas As List(Of cViga))

        Dim combsDesign = New HashSet(Of String)(
            Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Select(Function(c) NormalizarClaveCombo(c)),
            StringComparer.OrdinalIgnoreCase)

        _vigaService.CalcularEnvolventesVigas(vigas, Proyecto.Elementos.Vigas.BeamForces, combsDesign)
        _vigaService.designVigas(vigas, Proyecto.Elementos.Vigas.Joints)
        _vigaService.AsignarRefuerzoTransversalAutomatico(vigas)

        If Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Count > 0 Then
            Dim combsCortante = New HashSet(Of String)(
                Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Select(Function(c) NormalizarClaveCombo(c)),
                StringComparer.OrdinalIgnoreCase)
            _vigaService.CalcularEnvolventeCortante(vigas, Proyecto.Elementos.Vigas.BeamForces, combsCortante)
            _vigaService.CalcularCapacidadCortante(vigas)
        End If

        TriggerCortantePlastico(vigas)

    End Sub

    Private Sub CentrarBotonesRefuerzo()

        Dim espacio As Integer = 15

        If Boton_VerGrupo.Visible Then
            Dim anchoTotal As Integer =
                Boton_Aplicar.Width + Boton_Copiar.Width +
                Boton_Replicar.Width + Boton_VerGrupo.Width +
                espacio * 3
            Dim xInicio As Integer = (TabPage2.ClientSize.Width - anchoTotal) \ 2
            Boton_Aplicar.Left = xInicio
            Boton_Copiar.Left = Boton_Aplicar.Right + espacio
            Boton_Replicar.Left = Boton_Copiar.Right + espacio
            Boton_VerGrupo.Left = Boton_Replicar.Right + espacio
            Boton_VerGrupo.Top = Boton_Replicar.Top
        Else
            Dim anchoTotal As Integer =
                Boton_Aplicar.Width + Boton_Copiar.Width +
                Boton_Replicar.Width + espacio * 2
            Dim xInicio As Integer = (TabPage2.ClientSize.Width - anchoTotal) \ 2
            Boton_Aplicar.Left = xInicio
            Boton_Copiar.Left = Boton_Aplicar.Right + espacio
            Boton_Replicar.Left = Boton_Copiar.Right + espacio
        End If

    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        CentrarBotonesRefuerzo()
    End Sub

    Private Sub ConstruirTablaResumen(viga As cViga, dgv As DataGridView)

        ' =================================================
        ' 🔹 CONFIGURACIÓN GENERAL
        ' =================================================
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect

        dgv.RowHeadersVisible = True
        dgv.RowHeadersWidth = 180

        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.ScrollBars = ScrollBars.Both

        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single
        dgv.GridColor = Color.FromArgb(210, 210, 210)

        dgv.EnableHeadersVisualStyles = False

        ' =================================================
        ' 🔹 TIPOGRAFÍA
        ' =================================================
        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)

        ' =================================================
        ' 🔹 COLORES (estilo software)
        ' =================================================
        dgv.BackgroundColor = Color.White

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black

        dgv.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)

        dgv.DefaultCellStyle.BackColor = Color.White
        dgv.DefaultCellStyle.ForeColor = Color.Black
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240)
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black

        ' =================================================
        ' 🔹 ALINEACIÓN
        ' =================================================
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft

        ' =================================================
        ' 🔹 ALTURA DE FILAS Y HEADER
        ' =================================================
        dgv.ColumnHeadersHeight = 45
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

        dgv.RowTemplate.Height = 28

        ' =================================================
        ' 🔹 COLUMNAS (3 por frame)
        ' =================================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            Dim colL = dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            Dim colC = dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            Dim colR = dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")

            dgv.Columns(colL).Width = 70
            dgv.Columns(colC).Width = 70
            dgv.Columns(colR).Width = 70
        Next

        ' =================================================
        ' 🔹 FILAS (RESULTADOS)
        ' =================================================
        Dim nombresFilas As String() = {
        "M− (kN·m)",
        "M+ (kN·m)",
        "M- ENV (kN·m)",
        "M+ ENV (kN·m)",
        "V (kN)"
    }

        For Each nombre In nombresFilas
            Dim idx = dgv.Rows.Add()
            dgv.Rows(idx).HeaderCell.Value = nombre
        Next

        ' =================================================
        ' 🔹 EFECTO ZEBRA (lectura fácil)
        ' =================================================
        For i As Integer = 0 To dgv.Rows.Count - 1
            If i Mod 2 = 0 Then
                dgv.Rows(i).DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            End If
        Next

    End Sub

    Private Sub ConstruirTablaRefuerzo(viga As cViga, dgv As DataGridView)

        dgv.Columns.Clear()
        dgv.Rows.Clear()

        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = False

        dgv.RowHeadersVisible = True
        dgv.RowHeadersWidth = 120

        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.ScrollBars = ScrollBars.Both

        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect
        dgv.EnableHeadersVisualStyles = False

        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single
        dgv.GridColor = Color.FromArgb(210, 210, 210)

        ' ============================================
        ' TIPOGRAFÍA
        ' ============================================
        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)

        ' ============================================
        ' COLORES
        ' ============================================
        dgv.BackgroundColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)

        dgv.DefaultCellStyle.BackColor = Color.White
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240)

        ' ============================================
        ' ALINEACIÓN
        ' ============================================
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' ============================================
        ' ALTURAS
        ' ============================================
        dgv.ColumnHeadersHeight = 45
        dgv.RowTemplate.Height = 26

        ' ============================================
        ' COLUMNAS
        ' ============================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            Dim c1 = dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            Dim c2 = dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            Dim c3 = dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")

            dgv.Columns(c1).Width = 65
            dgv.Columns(c2).Width = 65
            dgv.Columns(c3).Width = 65

        Next

        ' ============================================
        ' FILAS (BARRAS)
        ' ============================================
        Dim barras = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10"}

        For Each barra In barras

            Dim rowIndex As Integer = dgv.Rows.Add()
            dgv.Rows(rowIndex).HeaderCell.Value = barra

            For i As Integer = 0 To dgv.Columns.Count - 1
                dgv.Rows(rowIndex).Cells(i).Value = 0
            Next

        Next

        ' ============================================
        ' ZEBRA SUAVE
        ' ============================================
        For i As Integer = 0 To dgv.Rows.Count - 1
            If i Mod 2 = 0 Then
                dgv.Rows(i).DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            End If
        Next

    End Sub

    Private Sub ConstruirTablaRefuerzoTransversal(viga As cViga, dgv As DataGridView)

        dgv.Columns.Clear()
        dgv.Rows.Clear()

        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = False

        dgv.RowHeadersVisible = True
        dgv.RowHeadersWidth = 120

        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.ScrollBars = ScrollBars.Both

        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect
        dgv.EnableHeadersVisualStyles = False

        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single
        dgv.GridColor = Color.FromArgb(210, 210, 210)

        ' ============================================
        ' TIPOGRAFÍA
        ' ============================================
        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)

        ' ============================================
        ' COLORES
        ' ============================================
        dgv.BackgroundColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)

        dgv.DefaultCellStyle.BackColor = Color.White
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240)

        ' ============================================
        ' ALINEACIÓN
        ' ============================================
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' ============================================
        ' ALTURAS
        ' ============================================
        dgv.ColumnHeadersHeight = 45
        dgv.RowTemplate.Height = 26

        ' ============================================
        ' COLUMNAS (una por tramo: Izq, Centro, Der)
        ' ============================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            Dim c1 = dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            Dim c2 = dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            Dim c3 = dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")

            dgv.Columns(c1).Width = 65
            dgv.Columns(c2).Width = 65
            dgv.Columns(c3).Width = 65

        Next

        ' ============================================
        ' FILAS (propiedades del estribo)
        ' ============================================
        Dim filas() As String = {"Num. Estribos", "# Barra", "Cant. Ramas", "Separación (m)"}
        Dim valoresDefault() As Object = {10, 3, 2, 0.1}

        For f As Integer = 0 To filas.Length - 1

            Dim rowIndex As Integer = dgv.Rows.Add()
            dgv.Rows(rowIndex).HeaderCell.Value = filas(f)

            For i As Integer = 0 To dgv.Columns.Count - 1
                dgv.Rows(rowIndex).Cells(i).Value = valoresDefault(f)
            Next

        Next

        ' ============================================
        ' VALIDACIÓN FILA #Barra (solo enteros 2–10)
        ' ============================================
        For i As Integer = 0 To dgv.Columns.Count - 1
            Dim cell = TryCast(dgv.Rows(0).Cells(i), DataGridViewTextBoxCell)
            If cell IsNot Nothing Then
                cell.Style.ForeColor = Color.FromArgb(30, 100, 200)
                cell.Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            End If
        Next

        ' ============================================
        ' ZEBRA SUAVE
        ' ============================================
        For i As Integer = 0 To dgv.Rows.Count - 1
            If i Mod 2 = 0 Then
                dgv.Rows(i).DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            End If
        Next

    End Sub

    Private Sub ConstruirTablaResultadosFlexion(viga As cViga, dgv As DataGridView)

        ' =================================================
        ' 🔹 CONFIGURACIÓN GENERAL
        ' =================================================
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = False ' ← editable
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect

        dgv.RowHeadersVisible = True
        dgv.RowHeadersWidth = 180

        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.ScrollBars = ScrollBars.Both

        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single
        dgv.GridColor = Color.FromArgb(210, 210, 210)

        dgv.EnableHeadersVisualStyles = False

        ' =================================================
        ' 🔹 TIPOGRAFÍA
        ' =================================================
        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)

        ' =================================================
        ' 🔹 COLORES
        ' =================================================
        dgv.BackgroundColor = Color.White

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)

        dgv.DefaultCellStyle.BackColor = Color.White
        dgv.DefaultCellStyle.ForeColor = Color.Black

        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240)
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black

        ' =================================================
        ' 🔹 ALINEACIÓN
        ' =================================================
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft

        ' =================================================
        ' 🔹 ALTURAS
        ' =================================================
        dgv.ColumnHeadersHeight = 45
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

        dgv.RowTemplate.Height = 28

        ' =================================================
        ' 🔹 COLUMNAS (3 POR FRAME)
        ' =================================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            Dim colL = dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            Dim colC = dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            Dim colR = dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")

            dgv.Columns(colL).Width = 75
            dgv.Columns(colC).Width = 75
            dgv.Columns(colR).Width = 75
        Next

        ' =================================================
        ' 🔹 FILAS
        ' =================================================
        Dim nombresFilas As String() = {
        "Sección",
        "Longitud (m)",
        "M- ENV (kN·m)",
        "M+ ENV (kN·m)",
        "As req (-)",
        "As req (+)",
        "As col (-)",
        "As col (+)",
        "F (-)",
        "F (+)",
        "Redistribución (-)",
        "Redistribución (+)",
        "F_Final (-)",
        "F_Final (+)"
    }

        For Each nombre In nombresFilas
            Dim idx = dgv.Rows.Add()
            dgv.Rows(idx).HeaderCell.Value = nombre
        Next

        ' =================================================
        ' 🔹 ZEBRA (mejora lectura)
        ' =================================================
        For i As Integer = 0 To dgv.Rows.Count - 1
            If i Mod 2 = 0 Then
                dgv.Rows(i).DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            End If
        Next

        ' =================================================
        ' 🔹 FILAS EDITABLES (REDISTRIBUCIÓN)
        ' =================================================
        dgv.Rows(FILA_RED_NEG).DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220) ' amarillo suave
        dgv.Rows(FILA_RED_POS).DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220)

        dgv.Rows(FILA_RED_NEG).DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        dgv.Rows(FILA_RED_POS).DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)

        ' =================================================
        ' 🔹 OPCIONAL: BLOQUE VISUAL POR FRAME
        ' =================================================
        For i = 0 To dgv.Columns.Count - 1
            If (i \ 3) Mod 2 = 0 Then
                dgv.Columns(i).DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            End If
        Next

    End Sub


    Private Function ObtenerMomentoPositivo(
    bfFrame As List(Of cCombinacionBeamForce),
    estacionObjetivo As Single,
    Optional tolerancia As Single = 0.25) As Single
        ' tolerancia = fracción de la longitud (5%)

        Dim L As Single = bfFrame.Max(Function(bf) bf.ElementStation)
        Dim tolAbs As Single = tolerancia * L

        Dim candidatos = bfFrame _
        .Where(Function(bf) bf.M3 > 0 AndAlso Math.Abs(bf.ElementStation - estacionObjetivo) <= tolAbs) _
        .ToList()

        If candidatos.Count = 0 Then Return 0

        Return candidatos.Max(Function(bf) bf.M3)
    End Function

    Private Function ObtenerMomentoNegativo(bfFrame As List(Of cCombinacionBeamForce), estacionObjetivo As Single, Optional tolerancia As Single = 0.3) As Single

        Dim L As Single = bfFrame.Max(Function(bf) bf.ElementStation)
        Dim tolAbs As Single = tolerancia * L

        Dim candidatos = bfFrame _
        .Where(Function(bf) bf.M3 < 0 AndAlso Math.Abs(bf.ElementStation - estacionObjetivo) <= tolAbs) _
        .ToList()

        If candidatos.Count = 0 Then Return 0

        Return candidatos.Min(Function(bf) bf.M3)

    End Function

    Private Function ObtenerCortante(
    bfFrame As List(Of cCombinacionBeamForce),
    estacionObjetivo As Single,
    Optional tolerancia As Single = 0.2) As Single

        Dim L As Single = bfFrame.Max(Function(bf) bf.ElementStation)
        Dim tolAbs As Single = tolerancia * L

        Dim candidatos = bfFrame _
        .Where(Function(bf) Math.Abs(bf.ElementStation - estacionObjetivo) <= tolAbs) _
        .ToList()

        If candidatos.Count = 0 Then Return 0

        Return candidatos.Max(Function(bf) Math.Abs(bf.V2))
    End Function

    Private Sub LlenarTablaResumen(viga As cViga, dgv As DataGridView)

        Dim colBase As Integer = 0

        For Each frame In viga.Frames

            Dim env = frame.EnvolventeMomento
            If env Is Nothing OrElse env.Estaciones Is Nothing OrElse env.Estaciones.Count = 0 Then
                colBase += 3
                Continue For
            End If

            Dim estaciones = env.Estaciones

            Dim L As Double = estaciones.Last()
            Dim sIzq As Double = 0
            Dim sCen As Double = L / 2
            Dim sDer As Double = L

            ' ==================================================
            ' 🔹 ANALISIS
            ' ==================================================

            Dim tol As Double = 0.15 * L

            ' Negativos (usar MminAnalisis)
            Dim M1n = ObtenerValorEnEstacion(estaciones, env.MminAnalisis, sIzq)
            Dim M2n = ObtenerValorEnEstacion(estaciones, env.MminAnalisis, sCen)
            Dim M3n = ObtenerValorEnEstacion(estaciones, env.MminAnalisis, sDer)

            ' Positivos (usar MmaxAnalisis)
            Dim M1p = ObtenerValorEnEstacion(estaciones, env.MmaxAnalisis, sIzq)
            Dim M2p = ObtenerValorEnEstacion(estaciones, env.MmaxAnalisis, sCen)
            Dim M2p_Ventana = ObtenerExtremoEnVentana(estaciones, env.MmaxAnalisis, sCen, tol, True)
            M2p = Math.Max(M2p, M2p_Ventana)
            Dim M3p = ObtenerValorEnEstacion(estaciones, env.MmaxAnalisis, sDer)

            ' Si no hay signo, poner 0
            M1n = If(M1n < 0, M1n, 0)
            M2n = If(M2n < 0, M2n, 0)
            M3n = If(M3n < 0, M3n, 0)

            M1p = If(M1p > 0, M1p, 0)
            M2p = If(M2p > 0, M2p, 0)
            M3p = If(M3p > 0, M3p, 0)

            dgv.Rows(0).Cells(colBase).Value = Math.Abs(Math.Round(M1n, 2))
            dgv.Rows(0).Cells(colBase + 1).Value = Math.Abs(Math.Round(M2n, 2))
            dgv.Rows(0).Cells(colBase + 2).Value = Math.Abs(Math.Round(M3n, 2))

            dgv.Rows(1).Cells(colBase).Value = Math.Round(M1p, 2)
            dgv.Rows(1).Cells(colBase + 1).Value = Math.Round(M2p, 2)
            dgv.Rows(1).Cells(colBase + 2).Value = Math.Round(M3p, 2)

            ' ==================================================
            ' 🔹 DISEÑO (NSR-10)
            ' ==================================================

            Dim D1n = ObtenerValorEnEstacion(estaciones, env.MminDesign, sIzq)
            Dim D2n = ObtenerValorEnEstacion(estaciones, env.MminDesign, sCen)
            Dim D3n = ObtenerValorEnEstacion(estaciones, env.MminDesign, sDer)

            Dim D1p = ObtenerValorEnEstacion(estaciones, env.MmaxDesign, sIzq)
            Dim D2p = ObtenerValorEnEstacion(estaciones, env.MmaxDesign, sCen)
            Dim D2p_Ventana = ObtenerExtremoEnVentana(estaciones, env.MmaxDesign, sCen, tol, True)
            D2p = Math.Max(D2p, D2p_Ventana)

            Dim D3p = ObtenerValorEnEstacion(estaciones, env.MmaxDesign, sDer)

            ' Aplicar condición de signo
            D1n = If(D1n < 0, D1n, 0)
            D2n = If(D2n < 0, D2n, 0)
            D3n = If(D3n < 0, D3n, 0)

            D1p = If(D1p > 0, D1p, 0)
            D2p = If(D2p > 0, D2p, 0)
            D3p = If(D3p > 0, D3p, 0)

            dgv.Rows(2).Cells(colBase).Value = Math.Abs(Math.Round(D1n, 2))
            dgv.Rows(2).Cells(colBase + 1).Value = Math.Abs(Math.Round(D2n, 2))
            dgv.Rows(2).Cells(colBase + 2).Value = Math.Abs(Math.Round(D3n, 2))

            dgv.Rows(3).Cells(colBase).Value = Math.Round(D1p, 2)
            dgv.Rows(3).Cells(colBase + 1).Value = Math.Round(D2p, 2)
            dgv.Rows(3).Cells(colBase + 2).Value = Math.Round(D3p, 2)

            ' ==================================================
            ' 🔹 CORTANTE (puedes dejarlo como lo tienes o migrarlo igual)
            ' ==================================================

            colBase += 3

        Next

    End Sub

    Private Function ObtenerValorEnEstacion(estaciones As List(Of Double),
                                        valores As List(Of Double),
                                        sObjetivo As Double) As Double

        If estaciones Is Nothing OrElse valores Is Nothing OrElse estaciones.Count = 0 Then Return 0

        ' Buscar el índice más cercano a la estación objetivo
        Dim idx = estaciones _
        .Select(Function(s, i) New With {.Dist = Math.Abs(s - sObjetivo), .Index = i}) _
        .OrderBy(Function(x) x.Dist) _
        .First().Index

        Return valores(idx)

    End Function

    Private Function ObtenerExtremoEnVentana(estaciones As List(Of Double),
                                         valores As List(Of Double),
                                         sCentro As Double,
                                         tolerancia As Double,
                                         buscarMax As Boolean) As Double

        If estaciones Is Nothing OrElse valores Is Nothing OrElse estaciones.Count = 0 Then Return 0

        ' Filtrar puntos dentro de la ventana
        Dim indices = estaciones _
        .Select(Function(s, i) New With {.s = s, .i = i}) _
        .Where(Function(x) Math.Abs(x.s - sCentro) <= tolerancia) _
        .Select(Function(x) x.i) _
        .ToList()

        ' Si no hay puntos en la ventana, usar el más cercano
        If indices.Count = 0 Then
            Return ObtenerValorEnEstacion(estaciones, valores, sCentro)
        End If

        ' Buscar extremo dentro de la ventana
        Dim subset = indices.Select(Function(i) valores(i))

        If buscarMax Then
            Return subset.Max()
        Else
            Return subset.Min()
        End If

    End Function


    Private Sub LlenarTablaResultados(viga As cViga, dgv As DataGridView)

        Dim colBase As Integer = 0 ' después de la columna "Resultado"

        For Each frame In viga.Frames

            Dim posiciones = {
                        PosicionTramoViga.Izquierda,
                        PosicionTramoViga.Centro,
                        PosicionTramoViga.Derecha
                    }

            For Each pos In posiciones

                ' Buscar revisión
                Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

                If revision.Posicion = PosicionTramoViga.Centro Then
                    dgv.Rows(0).Cells(colBase).Value = frame.Section.LabelSec
                    dgv.Rows(1).Cells(colBase).Value = Math.Round(frame.Longitud, 2)
                End If

                Dim res = revision.ResultadoBase

                dgv.Rows(2).Cells(colBase).Value = Math.Abs(Math.Round(res.MomentoNegativo, 2))
                dgv.Rows(3).Cells(colBase).Value = Math.Round(res.MomentoPositivo, 2)
                dgv.Rows(4).Cells(colBase).Value = Math.Round(res.AsReqSup, 0)
                dgv.Rows(5).Cells(colBase).Value = Math.Round(res.AsReqInf, 0)

                colBase += 1

            Next

        Next
    End Sub




    Private Sub Form_09_Vigas_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Proyecto = Form_00_PaginaPrincipal.proyecto
        If Proyecto.Elementos.Vigas.Vigas IsNot Nothing AndAlso Proyecto.Elementos.Vigas.Vigas.Count > 0 Then
            RefrescarDesdeProyecto()
        End If
    End Sub

    Private Sub Lista_Vigas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Lista_Vigas.SelectedIndexChanged

        If _cargando Then Exit Sub
        If Me.DesignMode Then Return
        If Lista_Vigas.SelectedItem Is Nothing Then Exit Sub

        Dim vigaSel As cViga = CType(Lista_Vigas.SelectedItem, cViga)
        _vigaActual = vigaSel

        _cargando = True
        Nombre_Viga.Text = If(String.IsNullOrWhiteSpace(vigaSel.NombrePlano), vigaSel.Nombre, vigaSel.NombrePlano)
        _cargando = False

        If _vigas Is Nothing OrElse _joints Is Nothing Then Exit Sub

        Dim pisoSel = Lista_Pisos.SelectedItem?.ToString()
        If String.IsNullOrEmpty(pisoSel) Then Exit Sub

        Dim grids = Proyecto?.Elementos?.Grids?.GridLines
        If grids IsNot Nothing Then
            _DiagramaService.DibujarPlanta(PictureBox1, _vigas, _joints, grids, pisoSel, vigaSel)
        End If

        CargarVigaCompleta(vigaSel)

    End Sub

    Private Sub Nombre_Viga_TextChanged(sender As Object, e As EventArgs) Handles Nombre_Viga.TextChanged
        If _cargando Then Return
        Dim viga = TryCast(Lista_Vigas.SelectedItem, cViga)
        If viga Is Nothing Then Return
        viga.NombrePlano = Nombre_Viga.Text.Trim()
    End Sub

    ' Devuelve el nombre para reportes: NombrePlano si el usuario lo definió, si no Nombre.
    Private Function NombreReporte(viga As cViga) As String
        Return If(String.IsNullOrWhiteSpace(viga.NombrePlano), viga.Nombre, viga.NombrePlano)
    End Function

    Private Sub Boton_Aplicar_Click(sender As Object, e As EventArgs) Handles Boton_Aplicar.Click
        '====================================================================
        '====================== BOTON APLICAR REFUERZO ======================
        '====================================================================

        If Lista_Vigas.SelectedItem Is Nothing Then Exit Sub

        Dim viga As cViga = CType(Lista_Vigas.SelectedItem, cViga)

        ' Limpiar refuerzo previo
        For Each f In viga.Frames
            f.RefuerzoSuperior.Clear()
            f.RefuerzoInferior.Clear()
        Next

        ' Guardar refuerzo superior
        Dim datosSup = ExtraerRefuerzoDesdeGrid(Ref_Superior)
        _vigaService.GuardarRefuerzo(viga, datosSup, eTipoRefuerzo.Superior)

        ' Guardar refuerzo inferior
        Dim datosInf = ExtraerRefuerzoDesdeGrid(Ref_Inferior)
        _vigaService.GuardarRefuerzo(viga, datosInf, eTipoRefuerzo.Inferior)

        ' Guardar refuerzo transversal
        Dim datosTransv = ExtraerEstribosDesdeGrid(Ref_Transversal)
        _vigaService.GuardarRefuerzoTransversal(viga, datosTransv)

        _vigaService.CalcularFlexionViga(viga)
        MostrarResultadosFlexion(viga)

        ' Recalcular cortante con el nuevo refuerzo transversal (zonas N×s)
        If Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Count > 0 Then
            Dim combsCortante_ap = New HashSet(Of String)(
                Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Select(Function(c) NormalizarClaveCombo(c)),
                StringComparer.OrdinalIgnoreCase)
            Dim listaViga As New List(Of cViga) From {viga}
            _vigaService.CalcularEnvolventeCortante(listaViga,
                                                    Proyecto.Elementos.Vigas.BeamForces,
                                                    combsCortante_ap)
            _vigaService.CalcularCapacidadCortante(listaViga)
            MostrarResultadosCortante(viga)

            Dim bfCortante = Proyecto.Elementos.Vigas.BeamForces _
                .Where(Function(bf) combsCortante_ap.Contains(bf.LoadCaseKey)) _
                .ToList()
            _DiagramaService.DibujarDiagramaCortanteFrames(viga, Diagrama_Cortante, bfCortante, combsCortante_ap)
        End If

        ' Cortante plástico para la viga actual (si aplica)
        TriggerCortantePlastico(New List(Of cViga) From {viga})

        HayCambios = True

        MessageBox.Show("Refuerzo guardado correctamente",
                    "Información",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

    End Sub


    'Private Sub AplicarYCalcularViga(viga As cViga,
    '                        dgvSup As DataGridView,
    '                        dgvInf As DataGridView)

    '    ' Limpiar
    '    For Each f In viga.Frames
    '        f.RefuerzoSuperior.Clear()
    '        f.RefuerzoInferior.Clear()
    '    Next


    '    ' Guardar desde tablas
    '    GuardarRefuerzoTabla(viga, dgvSup, eTipoRefuerzo.Superior)
    '    GuardarRefuerzoTabla(viga, dgvInf, eTipoRefuerzo.Inferior)

    '    ' 🔥 Calcular
    '    _vigaService.CalcularFlexionViga(viga)

    'End Sub

    Private Sub AplicarYCalcularViga(viga As cViga,
                                dgvSup As DataGridView,
                                dgvInf As DataGridView)

        ' 🔹 limpiar
        For Each f In viga.Frames
            f.RefuerzoSuperior.Clear()
            f.RefuerzoInferior.Clear()
        Next

        ' 🔹 extraer
        Dim datosSup = ExtraerRefuerzoDesdeGrid(dgvSup)
        Dim datosInf = ExtraerRefuerzoDesdeGrid(dgvInf)

        ' 🔹 guardar
        _vigaService.GuardarRefuerzo(viga, datosSup, eTipoRefuerzo.Superior)
        _vigaService.GuardarRefuerzo(viga, datosInf, eTipoRefuerzo.Inferior)

        ' 🔥 calcular
        _vigaService.CalcularFlexionViga(viga)

    End Sub

    Private Function ExtraerRefuerzoDesdeGrid(dgv As DataGridView) _
    As List(Of (FrameLabel As String, Posicion As PosicionTramoViga, Barras As Dictionary(Of String, Integer)))

        Dim lista As New List(Of (String, PosicionTramoViga, Dictionary(Of String, Integer)))

        For col As Integer = 0 To dgv.Columns.Count - 1

            Dim partes = dgv.Columns(col).HeaderText.Split({vbCrLf}, StringSplitOptions.None)

            Dim frameLabel = partes(0).Trim()
            Dim posicionTexto = partes(1).Trim()

            Dim posicion As PosicionTramoViga =
            If(posicionTexto = "Izq", PosicionTramoViga.Izquierda,
            If(posicionTexto = "Centro", PosicionTramoViga.Centro,
                                          PosicionTramoViga.Derecha))

            Dim barras As New Dictionary(Of String, Integer)

            For row As Integer = 0 To dgv.Rows.Count - 1

                Dim barra = dgv.Rows(row).HeaderCell.Value.ToString()
                Dim valor = dgv.Rows(row).Cells(col).Value

                Dim cantidad As Integer = 0
                If valor IsNot Nothing Then Integer.TryParse(valor.ToString(), cantidad)

                If cantidad > 0 Then barras(barra) = cantidad

            Next

            lista.Add((frameLabel, posicion, barras))

        Next

        Return lista

    End Function
    Private Function ObtenerAsProvista(lista As List(Of cRefuerzoTramo),
                                   posicion As PosicionTramoViga) As Double

        Dim tramo = lista.Find(Function(r) r.Posicion = posicion)

        If tramo Is Nothing Then Return 0

        Dim AsTotal As Double = 0

        For Each kv In tramo.Barras

            Dim barra As String = kv.Key
            Dim cantidad As Integer = kv.Value

            Dim area As Double = AreaRefuerzo(barra)

            AsTotal += cantidad * area

        Next

        Return AsTotal

    End Function

    Private Sub Boton_Copiar_Click(sender As Object, e As EventArgs) Handles Boton_Copiar.Click

        Dim origen As DataGridView = Ref_Superior
        Dim destino As DataGridView = Ref_Inferior

        ' Validación básica
        If origen.Columns.Count <> destino.Columns.Count OrElse
           origen.Rows.Count <> destino.Rows.Count Then

            MessageBox.Show("Las tablas no tienen la misma estructura",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            Exit Sub
        End If

        ' Recorrer celdas
        For col As Integer = 0 To origen.Columns.Count - 1
            For row As Integer = 0 To origen.Rows.Count - 1

                Dim valor = origen.Rows(row).Cells(col).Value

                ' Copiar valor directamente
                destino.Rows(row).Cells(col).Value = valor

            Next
        Next

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        If Lista_Vigas.Items.Count = 0 Then Exit Sub

        Dim indexActual As Integer = Lista_Vigas.SelectedIndex

        If indexActual < Lista_Vigas.Items.Count - 1 Then
            Lista_Vigas.SelectedIndex = indexActual + 1
        Else
            MessageBox.Show("Ya estás en la última viga",
                            "Información",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
        End If

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        If Lista_Vigas.Items.Count = 0 Then Exit Sub

        Dim indexActual As Integer = Lista_Vigas.SelectedIndex

        If indexActual > 0 Then
            Lista_Vigas.SelectedIndex = indexActual - 1
        Else
            MessageBox.Show("Ya estás en la primera viga",
                            "Información",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
        End If

    End Sub

    Private Sub Boton_Replicar_Click(sender As Object, e As EventArgs) Handles Boton_Replicar.Click

        If _vigaActual Is Nothing Then Exit Sub
        If _vigas Is Nothing OrElse _joints Is Nothing Then
            MessageBox.Show("Primero calcula las vigas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        ' --- Caso 1: viga es PATRÓN → propagar refuerzo ---
        If _vigaActual.EsPatronGrupo Then
            PropagateGrupoActual()
            Exit Sub
        End If

        ' --- Caso 2: viga es SIMILAR → navegar al patrón ---
        If Not String.IsNullOrEmpty(_vigaActual.GrupoReplicaID) Then
            Dim grupo = Proyecto.Elementos.Vigas.GruposReplica.FirstOrDefault(
                Function(g) g.ID = _vigaActual.GrupoReplicaID)
            Dim patron = _vigas.FirstOrDefault(
                Function(v) v.EsPatronGrupo AndAlso v.GrupoReplicaID = _vigaActual.GrupoReplicaID)
            If patron IsNot Nothing Then
                Dim msg = $"Esta viga es un SIMILAR del grupo ""{grupo?.NombreGrupo}"".{vbCrLf}" &
                          $"Patrón: {patron.NombreDisplay} — Piso {patron.Piso}{vbCrLf}{vbCrLf}" &
                          "¿Deseas navegar al patrón?"
                If MessageBox.Show(msg, "Grupo de Réplica", MessageBoxButtons.YesNo, MessageBoxIcon.Information) = DialogResult.Yes Then
                    ' Cambiar al piso del patrón y seleccionar la viga
                    Lista_Pisos.SelectedItem = patron.Piso
                    Lista_Vigas.SelectedItem = patron
                End If
            End If
            Exit Sub
        End If

        ' --- Caso 3: sin grupo → crear grupo de réplica ---
        CrearGrupoReplicaDesdeActual()

    End Sub

    Private Sub Boton_VerGrupo_Click(sender As Object, e As EventArgs) Handles Boton_VerGrupo.Click
        If _vigaActual Is Nothing OrElse _vigas Is Nothing Then Exit Sub
        GestionarGrupoActual()
    End Sub

    Private Sub GestionarGrupoActual()
        Dim grupoID = _vigaActual.GrupoReplicaID
        If String.IsNullOrEmpty(grupoID) Then Exit Sub

        Dim grupo = Proyecto?.Elementos?.Vigas?.GruposReplica?.FirstOrDefault(
            Function(g) g.ID = grupoID)
        If grupo Is Nothing Then Exit Sub

        Dim patron = _vigas.FirstOrDefault(
            Function(v) v.EsPatronGrupo AndAlso v.GrupoReplicaID = grupoID)
        Dim similares = _vigas.Where(
            Function(v) v.GrupoReplicaID = grupoID AndAlso Not v.EsPatronGrupo).ToList()

        Using dlg As New Form_09_GestionGrupo(grupo, patron, similares)
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            ' Desvincula los similares desmarcados
            For Each piso In dlg.PisosAEliminar
                Dim sim = _vigas.FirstOrDefault(
                    Function(v) v.GrupoReplicaID = grupoID AndAlso
                                Not v.EsPatronGrupo AndAlso
                                v.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase))
                If sim IsNot Nothing Then
                    sim.GrupoReplicaID = ""
                    sim.RefuerzoDesincronizado = False
                End If
                grupo.Similares.RemoveAll(
                    Function(m) m.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase))
            Next

            ' Si no quedan similares, disuelve el grupo
            If grupo.Similares.Count = 0 Then
                If patron IsNot Nothing Then
                    patron.GrupoReplicaID = ""
                    patron.EsPatronGrupo = False
                End If
                Proyecto.Elementos.Vigas.GruposReplica.Remove(grupo)
            End If

            HayCambios = True
            ActualizarBotonReplica(_vigaActual)

            ' Refrescar lista del piso actual
            Dim pisoActual = Lista_Pisos.SelectedItem?.ToString()
            Dim vigasPiso = _vigas.Where(Function(v) v.Piso = pisoActual).ToList()
            _cargando = True
            Lista_Vigas.DataSource = Nothing
            Lista_Vigas.DataSource = vigasPiso
            Lista_Vigas.DisplayMember = "NombreDisplay"
            Lista_Vigas.SelectedItem = _vigaActual
            _cargando = False
            CargarVigaCompleta(_vigaActual)
        End Using
    End Sub

    Private Sub CrearGrupoReplicaDesdeActual()

        Dim patron = _vigaActual
        If patron.Frames.Count = 0 Then
            MessageBox.Show("La viga no tiene frames asignados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Detectar pisos compatibles.
        ' rawLabelsPorPiso se construye directamente del DataTable de ETABS (sin pasar por
        ' DataTableToFrames) para capturar pisos cuyos frames son descartados por la
        ' detección de joints (ej. P1, P10-P13 en ciertos modelos de ETABS).
        Dim todosFramesProyecto = Proyecto?.Elementos?.Vigas?.Frames
        If todosFramesProyecto Is Nothing Then todosFramesProyecto = New List(Of cFrame)()
        Dim rawLabels = BuildRawLabelsPorPiso(Proyecto?.TablasEtabs?.TablaOEFrames)
        Dim compatibilidades = _vigaService.DetectarPisosCompatibles(patron, _vigas, todosFramesProyecto, rawLabels)

        If compatibilidades.Count = 0 Then
            MessageBox.Show("No se encontraron otros pisos en este modelo.", "Aviso",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New Form_09_GrupoReplica(patron, _vigas, compatibilidades)
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim grupo = dlg.GrupoCreado
            Dim pisosDestino = dlg.PisosSeleccionados

            ' Marcar el patrón
            patron.GrupoReplicaID = grupo.ID
            patron.EsPatronGrupo = True
            patron.RefuerzoDesincronizado = False

            ' Construir combos cortante para recalcular similares
            Dim combosCortante As New HashSet(Of String)(
                Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Select(Function(c) NormalizarClaveCombo(c)),
                StringComparer.OrdinalIgnoreCase)

            ' Aplicar agrupación en cada piso y copiar refuerzo
            Dim similares As New List(Of cViga)()
            Dim pisosFallidos As New List(Of String)()
            Dim labelsGrupo As New HashSet(Of String)(grupo.Labels_Patron.Select(Function(l) l.Trim().ToUpperInvariant()))
            For Each piso In pisosDestino
                _vigaService._AplicarUnGrupoEnPiso(grupo.Labels_Patron, piso, _vigas, _joints)

                Dim sim = _vigas.FirstOrDefault(
                    Function(v) v.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase) AndAlso
                                labelsGrupo.SetEquals(v.Frames.Select(Function(f) f.ObjectLabel.Trim().ToUpperInvariant())))
                If sim Is Nothing Then
                    pisosFallidos.Add(piso)
                    Continue For
                End If

                sim.GrupoReplicaID = grupo.ID
                sim.EsPatronGrupo = False
                ' Heredar nombre del patrón para que el similar se llame igual
                sim.NombrePlano = patron.NombrePlano
                sim.EjeParalelo = patron.EjeParalelo
                similares.Add(sim)

                ' Actualizar nombre en el miembro del grupo
                Dim miembro = grupo.Similares.FirstOrDefault(Function(m) m.Piso.Equals(piso, StringComparison.OrdinalIgnoreCase))
                If miembro IsNot Nothing Then miembro.NombreViga = sim.Name_Beam
            Next

            ' Propagar refuerzo inmediatamente
            _vigaService.PropagateRefuerzoGrupo(patron, similares, combosCortante)

            ' Guardar grupo en el proyecto
            If Proyecto.Elementos.Vigas.GruposReplica Is Nothing Then
                Proyecto.Elementos.Vigas.GruposReplica = New List(Of GrupoReplicaViga)()
            End If
            Proyecto.Elementos.Vigas.GruposReplica.Add(grupo)

            HayCambios = True

            ' Refrescar lista del piso actual
            Dim pisoActual = Lista_Pisos.SelectedItem?.ToString()
            Dim vigasPiso = _vigas.Where(Function(v) v.Piso = pisoActual).ToList()
            _cargando = True
            Lista_Vigas.DataSource = Nothing
            Lista_Vigas.DataSource = vigasPiso
            Lista_Vigas.DisplayMember = "NombreDisplay"
            Lista_Vigas.SelectedItem = patron
            _cargando = False
            CargarVigaCompleta(patron)

            Dim msg = $"Grupo ""{grupo.NombreGrupo}"" creado.{vbCrLf}" &
                      $"Patrón: {patron.NombreDisplay} (Piso {patron.Piso}){vbCrLf}" &
                      $"Similares: {similares.Count} piso{If(similares.Count = 1, "", "s")} — refuerzo propagado y C/D recalculado."
            If pisosFallidos.Count > 0 Then
                msg &= $"{vbCrLf}{vbCrLf}Nota: {pisosFallidos.Count} piso{If(pisosFallidos.Count = 1, "", "s")} no se pudo agrupar en esta sesión " &
                       $"({String.Join(", ", pisosFallidos)}). Haz clic en ""Calcular"" para aplicar el grupo completo."
            End If
            MessageBox.Show(msg, "Grupo de Réplica creado", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using

    End Sub

    Private Sub PropagateGrupoActual()

        Dim patron = _vigaActual
        Dim grupo = Proyecto?.Elementos?.Vigas?.GruposReplica?.FirstOrDefault(
            Function(g) g.ID = patron.GrupoReplicaID)
        If grupo Is Nothing Then
            MessageBox.Show("No se encontró el grupo de réplica en el proyecto.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Dim similares = _vigas.Where(
            Function(v) v.GrupoReplicaID = patron.GrupoReplicaID AndAlso Not v.EsPatronGrupo).ToList()

        If similares.Count = 0 Then
            MessageBox.Show("No se encontraron similares vinculados. ¿Recalculaste las vigas?",
                            "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim msg = $"¿Propagar el refuerzo de ""{patron.NombreDisplay}"" a {similares.Count} similar{If(similares.Count = 1, "", "es")}?{vbCrLf}{vbCrLf}" &
                  String.Join(vbCrLf, similares.Select(Function(s) $"  • {s.NombreDisplay} — {s.Piso}"))
        If MessageBox.Show(msg, "Propagar Refuerzo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Dim combosCortante As New HashSet(Of String)(
            Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Select(Function(c) NormalizarClaveCombo(c)),
            StringComparer.OrdinalIgnoreCase)

        _vigaService.PropagateRefuerzoGrupo(patron, similares, combosCortante)

        HayCambios = True

        ' Refrescar la lista para actualizar [S!] → [S]
        Dim pisoActual = Lista_Pisos.SelectedItem?.ToString()
        Dim vigasPiso = _vigas.Where(Function(v) v.Piso = pisoActual).ToList()
        _cargando = True
        Lista_Vigas.DataSource = Nothing
        Lista_Vigas.DataSource = vigasPiso
        Lista_Vigas.DisplayMember = "NombreDisplay"
        Lista_Vigas.SelectedItem = patron
        _cargando = False
        ActualizarBotonReplica(patron)

        MessageBox.Show($"Refuerzo propagado a {similares.Count} similar{If(similares.Count = 1, "", "es")}. C/D recalculado.",
                        "OK", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    ''' Extrae un inventario Story → Set(ObjectLabel) directamente del DataTable de ETABS,
    ''' sin depender de DataTableToFrames. Garantiza que aparezcan pisos cuyos frames
    ''' tienen geometría de joints problemática (meshes complejos, pisos de transferencia).
    Private Function BuildRawLabelsPorPiso(dt As DataTable) As Dictionary(Of String, HashSet(Of String))
        Dim result As New Dictionary(Of String, HashSet(Of String))(StringComparer.OrdinalIgnoreCase)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return result

        Dim cols = dt.Columns.Cast(Of DataColumn) _
                     .ToDictionary(Function(c) c.ColumnName.Trim().ToLower().Replace(" ", "").Replace(vbTab, ""),
                                   Function(c) c.ColumnName)

        ' Buscar la columna Story
        Dim colStory As String = Nothing
        For Each kv In cols
            If kv.Key = "story" Then colStory = kv.Value : Exit For
        Next

        ' Buscar la columna de label: preferir "Object Label" (E23) sobre "Label"
        Dim colLabel As String = Nothing
        For Each kv In cols
            If kv.Key = "objectlabel" Then colLabel = kv.Value : Exit For
        Next
        If colLabel Is Nothing Then
            For Each kv In cols
                If kv.Key = "label" Then colLabel = kv.Value : Exit For
            Next
        End If
        ' Fallback: columna "Frame" (E17 "Connectivity - Frame")
        If colLabel Is Nothing Then
            For Each kv In cols
                If kv.Key = "frame" Then colLabel = kv.Value : Exit For
            Next
        End If

        If colStory Is Nothing OrElse colLabel Is Nothing Then Return result

        For Each r As DataRow In dt.Rows
            If r.IsNull(colStory) OrElse r.IsNull(colLabel) Then Continue For
            Dim story As String = r(colStory).ToString().Trim()
            Dim label As String = r(colLabel).ToString().Trim().ToUpperInvariant()
            If String.IsNullOrEmpty(story) OrElse String.IsNullOrEmpty(label) Then Continue For
            If Not result.ContainsKey(story) Then
                result(story) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            End If
            result(story).Add(label)
        Next
        Return result
    End Function

    ''' Copia NombrePlano y EjeParalelo del patrón a todos sus similares.
    ''' Se llama en Button1_Click (después de GenerarNombresPlano) y al crear el grupo.
    Private Sub PropagateNombresGrupo(vigas As List(Of cViga),
                                       gruposReplica As List(Of GrupoReplicaViga))

        If gruposReplica Is Nothing OrElse gruposReplica.Count = 0 Then Return

        For Each grupo In gruposReplica
            Dim patron = vigas.FirstOrDefault(
                Function(v) v.EsPatronGrupo AndAlso v.GrupoReplicaID = grupo.ID)
            If patron Is Nothing Then Continue For

            For Each sim In vigas.Where(
                Function(v) Not v.EsPatronGrupo AndAlso v.GrupoReplicaID = grupo.ID)
                sim.NombrePlano = patron.NombrePlano
                sim.EjeParalelo = patron.EjeParalelo
            Next
        Next

    End Sub



    Public Sub CopiarRefuerzoEntreVigas(origen As cViga, destino As cViga)

        For i = 0 To origen.Frames.Count - 1

            Dim fOrigen = origen.Frames(i)
            Dim fDestino = destino.Frames(i)

            ' Limpiar longitudinal y transversal
            fDestino.RefuerzoSuperior.Clear()
            fDestino.RefuerzoInferior.Clear()
            fDestino.RefuerzoTransversal.Clear()

            ' Copiar superior
            For Each tramo In fOrigen.RefuerzoSuperior
                fDestino.RefuerzoSuperior.Add(ClonarTramo(tramo))
            Next

            ' Copiar inferior
            For Each tramo In fOrigen.RefuerzoInferior
                fDestino.RefuerzoInferior.Add(ClonarTramo(tramo))
            Next

            ' Copiar estribos (refuerzo transversal por zona)
            For Each zona In fOrigen.RefuerzoTransversal
                fDestino.RefuerzoTransversal.Add(New cRefuerzoTransversalZona With {
                    .Posicion = zona.Posicion,
                    .NumEstribos = zona.NumEstribos,
                    .NumeroBarra = zona.NumeroBarra,
                    .CantEstribos = zona.CantEstribos,
                    .Separacion = zona.Separacion
                })
            Next

        Next

        ' Recalcular flexión y cortante para la viga destino con el refuerzo copiado
        _vigaService.CalcularFlexionViga(destino)
        If Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Count > 0 Then
            _vigaService.CalcularCapacidadCortante(New List(Of cViga) From {destino})
        End If

    End Sub

    Public Function ClonarTramo(origen As cRefuerzoTramo) As cRefuerzoTramo

        Dim nuevo As New cRefuerzoTramo
        nuevo.Posicion = origen.Posicion

        For Each kvp In origen.Barras
            nuevo.Barras(kvp.Key) = kvp.Value
        Next

        Return nuevo

    End Function


    Private Sub MostrarResultadosFlexion(viga As cViga)

        Dim dgv As DataGridView = Tabla_Resultados_Flexion
        Dim colBase As Integer = 0

        For Each frame In viga.Frames

            Dim posiciones = {
                PosicionTramoViga.Izquierda,
                PosicionTramoViga.Centro,
                PosicionTramoViga.Derecha
            }

            For Each pos In posiciones

                Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

                Dim res = revision.ResultadoBase

                dgv.Rows(6).Cells(colBase).Value = Math.Round(res.AsProvSup, 0)
                dgv.Rows(7).Cells(colBase).Value = Math.Round(res.AsProvInf, 0)

                dgv.Rows(8).Cells(colBase).Value = Math.Round(res.RatioSup, 2)
                dgv.Rows(9).Cells(colBase).Value = Math.Round(res.RatioInf, 2)

                ' Colores (igual que ya tienes)
                PintarCelda(dgv.Rows(8).Cells(colBase), res.RatioSup)
                PintarCelda(dgv.Rows(9).Cells(colBase), res.RatioInf)

                Dim factor As Double = Math.Round(revision.FactorRedistribucion, 2)

                ' Limpiar primero (evita basura visual)
                dgv.Rows(10).Cells(colBase).Value = ""
                dgv.Rows(11).Cells(colBase).Value = ""

                Select Case pos

                    Case PosicionTramoViga.Izquierda, PosicionTramoViga.Derecha
                        dgv.Rows(10).Cells(colBase).Value = factor

                    Case PosicionTramoViga.Centro
                        dgv.Rows(11).Cells(colBase).Value = factor

                End Select

                Dim res_act = revision.ResultadoActual

                dgv.Rows(12).Cells(colBase).Value = Math.Round(res_act.RatioSup, 2)
                dgv.Rows(13).Cells(colBase).Value = Math.Round(res_act.RatioInf, 2)

                ' 🔹 Color 
                PintarCelda(dgv.Rows(12).Cells(colBase), res_act.RatioSup)
                PintarCelda(dgv.Rows(13).Cells(colBase), res_act.RatioInf)

                colBase += 1

            Next

        Next

    End Sub

    Private Sub PintarCelda(cell As DataGridViewCell, val As Double)

        If Math.Min(val, 9.99) >= 0.9 Then
            cell.Style.BackColor = ColorTranslator.FromHtml("#C6EFCE")
            cell.Style.ForeColor = ColorTranslator.FromHtml("#006100")
        Else
            cell.Style.BackColor = ColorTranslator.FromHtml("#FFC7CE")
            cell.Style.ForeColor = ColorTranslator.FromHtml("#9C0006")
        End If

    End Sub

    Private Sub SaveAs_Pilas_Click(sender As Object, e As EventArgs) Handles SaveAs_Pilas.Click
        SaveAs(Proyecto)
    End Sub

    Private Sub SaveAs(ByVal Objeto As Object)
        Try
            Dim SaveAs As New SaveFileDialog
            SaveAs.Filter = "Archivo|*.esm"
            SaveAs.Title = "Guardar Archivo"
            SaveAs.FileName = Convert.ToString("RevisiónVigas_Proyecto-" & Proyecto.Info.Nombre)

            If SaveAs.ShowDialog() = DialogResult.OK Then

                Proyecto.Ruta = Path.GetFullPath(SaveAs.FileName)
                Form_00_PaginaPrincipal.proyecto = Proyecto
                Funciones_Programa.Serializar(SaveAs.FileName, Objeto)

                ' ✅ Mensaje de éxito
                MessageBox.Show("El archivo se guardó correctamente.", "Guardar", MessageBoxButtons.OK, MessageBoxIcon.Information)

            End If

        Catch ex As Exception
            ' ❌ Mensaje de error (MUY IMPORTANTE)
            MessageBox.Show("Error al guardar el archivo: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

        End Try

    End Sub

    Private Sub Guardar()

        Try

            If String.IsNullOrEmpty(Proyecto.Ruta) Then
                SaveAs(Proyecto)
                Exit Sub
            End If

            Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)

            UltimoGuardado = DateTime.Now
            HayCambios = False

            MessageBox.Show("Cambios guardados correctamente.", "Guardar", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception

            MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

        End Try

    End Sub

    Private Sub Open_Pilas_Click(sender As Object, e As EventArgs) Handles Open_Pilas.Click
        Open()
    End Sub

    Public Sub Open()

        Dim OpenFile As New OpenFileDialog
        OpenFile.Filter = "Archivo|*.esm"
        OpenFile.Title = "Abrir Archivo"

        If OpenFile.ShowDialog() <> DialogResult.OK Then Exit Sub

        Try
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(OpenFile.FileName)
        Catch
            ' Fallback: archivos guardados antes de la clase Proyecto (solo cElementos)
            Try
                Dim elementos = Funciones_Programa.DeSerializar(Of cElementos)(OpenFile.FileName)
                Proyecto = New Proyecto()
                Proyecto.Elementos = elementos
            Catch ex As Exception
                MessageBox.Show("Error al abrir el archivo. Puede estar dañado o ser incompatible." & vbCrLf & ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Exit Sub
            End Try
        End Try

        Proyecto.Ruta = OpenFile.FileName
        Form_00_PaginaPrincipal.proyecto = Proyecto
        Form_00_PaginaPrincipal.SincronizarModulos()
        CargarCombos(Proyecto)
        HayCambios = False
        MessageBox.Show("El archivo se abrió correctamente.", "Abrir", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub CargarCombos(Proyecto As ARCO.Proyecto)

        _cargando = True
        Me.SuspendLayout()

        Dim vigas As List(Of cViga) = Proyecto.Elementos.Vigas.Vigas
        _vigas = vigas
        _vigaActual = vigas.FirstOrDefault()
        _joints = Proyecto.Elementos.Vigas.Joints.ToDictionary(Function(j) j.ElementLabel)

        ' Compatibilidad: proyectos guardados antes de la versión con EjeParalelo.
        ' Si TODAS las vigas tienen EjeParalelo vacío y hay grids disponibles,
        ' se asignan automáticamente los ejes y se generan nombres significativos.
        Dim gridsAutoUpdate = Proyecto?.Elementos?.Grids?.GridLines
        If gridsAutoUpdate IsNot Nothing AndAlso gridsAutoUpdate.Count > 0 AndAlso
           _joints.Count > 0 AndAlso vigas.Count > 0 AndAlso
           vigas.All(Function(v) String.IsNullOrEmpty(v.EjeParalelo)) Then
            _geo.AsignarEjesAVigas(vigas, gridsAutoUpdate, _joints)
            _geo.AsignarEjesParalelosAVigas(vigas, gridsAutoUpdate, _joints)
            _vigaService.GenerarNombresPlano(vigas, Proyecto.Elementos.Vigas.PrefijoNombreViga)
        End If

        Dim stories As List(Of String) = Proyecto.Elementos.Vigas.Frames _
            .Select(Function(f) f.Story) _
            .Distinct() _
            .OrderBy(Function(s) s) _
            .ToList()

        Lista_Pisos.BeginUpdate()
        Lista_Pisos.DataSource = Nothing
        Lista_Pisos.DataSource = stories
        Lista_Pisos.EndUpdate()

        ' Filtrar vigas por el piso de la primera viga (o primer piso si no hay vigaActual)
        Dim pisoInicial As String = If(_vigaActual IsNot Nothing, _vigaActual.Piso, stories.FirstOrDefault())
        Dim vigasPisoI = If(Not String.IsNullOrEmpty(pisoInicial),
                            vigas.Where(Function(v) v.Piso = pisoInicial).ToList(),
                            vigas)

        Lista_Vigas.BeginUpdate()
        Lista_Vigas.DataSource = Nothing
        Lista_Vigas.DataSource = vigasPisoI
        Lista_Vigas.DisplayMember = "NombreDisplay"
        Lista_Vigas.EndUpdate()

        ' Sincronizar piso seleccionado en Lista_Pisos
        If Not String.IsNullOrEmpty(pisoInicial) Then
            Dim idx = Lista_Pisos.Items.IndexOf(pisoInicial)
            If idx >= 0 Then Lista_Pisos.SelectedIndex = idx
        End If

        If vigasPisoI.Count > 0 Then Lista_Vigas.SelectedIndex = 0

        _cargando = False
        Me.ResumeLayout(False)

        If _vigaActual IsNot Nothing Then
            Dim piso = _vigaActual.Piso
            Dim grids = Proyecto.Elementos?.Grids?.GridLines
            If grids IsNot Nothing Then
                _DiagramaService.DibujarPlanta(PictureBox1, _vigas, _joints, grids, piso, _vigaActual)
            End If
            CargarVigaCompleta(_vigaActual)
        End If

    End Sub

    Private Sub Tabla_Resultados_Flexion_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Resultados_Flexion.CellEndEdit

        If e.RowIndex = FILA_RED_NEG Or e.RowIndex = FILA_RED_POS Then

            Dim val As Double = 0
            Double.TryParse(Tabla_Resultados_Flexion.Rows(e.RowIndex).Cells(e.ColumnIndex).Value?.ToString(), val)

            ' limitar
            If val < 0 Then val = 0
            If val > 0.2 Then val = 0.2

            Tabla_Resultados_Flexion.Rows(e.RowIndex).Cells(e.ColumnIndex).Value = val

            ' =========================================
            ' 🔥 IDENTIFICAR FRAME Y POSICIÓN
            ' =========================================

            Dim frameIndex As Integer = e.ColumnIndex \ 3
            Dim posIndex As Integer = e.ColumnIndex Mod 3

            Dim viga As cViga = _vigaActual
            Dim frame As cFrame = viga.Frames(frameIndex)

            Dim posicion As PosicionTramoViga

            Select Case posIndex
                Case 0
                    posicion = PosicionTramoViga.Izquierda
                Case 1
                    posicion = PosicionTramoViga.Centro
                Case 2
                    posicion = PosicionTramoViga.Derecha
            End Select

            ' =========================================
            ' 🔥 APLICAR REDISTRIBUCIÓN
            ' =========================================

            Dim mensaje = _vigaService.AplicarRedistribucion(frame, posicion, val, e.RowIndex = FILA_RED_NEG)

            If mensaje IsNot Nothing Then
                MessageBox.Show(mensaje, "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End If

            _vigaService.CalcularFlexionViga(viga)
            MostrarResultadosFlexion(viga)

            HayCambios = True

        End If

    End Sub

    Private Function ObtenerValor(fila As Integer, col As Integer) As Double
        Dim val As Double = 0
        If Tabla_Resultados_Flexion.Rows(fila).Cells(col).Value IsNot Nothing Then
            Double.TryParse(Tabla_Resultados_Flexion.Rows(fila).Cells(col).Value.ToString(), val)
        End If
        Return val
    End Function

    Private Sub Tabla_Demandas_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles Tabla_Demandas.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Ref_Superior_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles Ref_Superior.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Ref_Inferior_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles Ref_Inferior.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub
    Private Sub Tabla_Resultados_Flexion_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles Tabla_Resultados_Flexion.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub PintarLineasCada3Columnas(sender As Object, e As DataGridViewCellPaintingEventArgs)

        If e.RowIndex < 0 Then Return

        e.Paint(e.CellBounds, DataGridViewPaintParts.All)

        ' Línea gruesa cada 3 columnas
        If (e.ColumnIndex + 1) Mod 3 = 0 Then
            Using pen As New Pen(Color.Black, 2)
                Dim x = e.CellBounds.Right - 1
                e.Graphics.DrawLine(pen, x, e.CellBounds.Top, x, e.CellBounds.Bottom)
            End Using
        End If

        e.Handled = True

    End Sub

    Private Sub TimerAutoSave_Tick(sender As Object, e As EventArgs) Handles TimerAutoSave.Tick

        If HayCambios Then

            Dim minutos As Double = (DateTime.Now - UltimoGuardado).TotalMinutes

            If minutos >= 10 Then

                If Not String.IsNullOrEmpty(Proyecto.Ruta) Then
                    Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)

                    UltimoGuardado = DateTime.Now
                    HayCambios = False

                    ' Opcional: indicador en UI
                    ' LabelEstado.Text = "AutoGuardado ✔"
                End If

            End If

        End If

    End Sub

    Public Sub RefrescarDesdeProyecto()
        Proyecto = Form_00_PaginaPrincipal.proyecto
        If Proyecto.Elementos.Vigas.Vigas Is Nothing OrElse Proyecto.Elementos.Vigas.Vigas.Count = 0 Then Return

        ' Migración: archivos guardados antes de que Vigas tuviera geometría propia.
        ' Si Vigas.Frames está vacío pero Elementos.Frames tiene datos, los copiamos.
        If Proyecto.Elementos.Vigas.Frames.Count = 0 AndAlso Proyecto.Elementos.Frames.Count > 0 Then
            Proyecto.Elementos.Vigas.Frames = Proyecto.Elementos.Frames
            Proyecto.Elementos.Vigas.Joints = Proyecto.Elementos.Joints
        End If

        If _vigaService Is Nothing Then _vigaService = New VigaService(_geo)
        If _DiagramaService Is Nothing Then _DiagramaService = New DiagramaService(_geo)
        CargarCombos(Proyecto)
    End Sub

    Private Sub Save_Pilas_Click(sender As Object, e As EventArgs) Handles Save_Pilas.Click
        Guardar()
    End Sub

    Private Sub New_Pilas_Click(sender As Object, e As EventArgs) Handles New_Pilas.Click

        Dim res = MessageBox.Show(
            "Esta acción eliminará TODA la información de vigas del proyecto actual:" & vbCrLf & vbCrLf &
            "  • Vigas y tramos generados" & vbCrLf &
            "  • Fuerzas importadas desde ETABS" & vbCrLf &
            "  • Combinaciones de diseño seleccionadas" & vbCrLf &
            "  • Secciones y agrupaciones configuradas" & vbCrLf & vbCrLf &
            "La información de columnas, muros y pilas NO se verá afectada." & vbCrLf & vbCrLf &
            "¿Desea continuar?",
            "Reemplazar datos de vigas",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning)

        If res <> DialogResult.Yes Then Return

        ' Resetear solo el módulo de vigas
        Proyecto.Elementos.Vigas = New cVigas()

        ' Limpiar estado local del formulario
        _vigas = Nothing
        _joints = Nothing
        _vigaActual = Nothing

        ' Limpiar controles visuales
        _cargando = True

        Lista_Vigas.DataSource = Nothing
        Lista_Pisos.DataSource = Nothing

        Tabla_Demandas.Rows.Clear()
        Tabla_Demandas.Columns.Clear()
        Ref_Superior.Rows.Clear()
        Ref_Superior.Columns.Clear()
        Ref_Inferior.Rows.Clear()
        Ref_Inferior.Columns.Clear()
        Ref_Transversal.Rows.Clear()
        Ref_Transversal.Columns.Clear()
        Tabla_Resultados_Flexion.Rows.Clear()
        Tabla_Resultados_Flexion.Columns.Clear()
        Tabla_Resultados_Cortante.Rows.Clear()
        Tabla_Resultados_Cortante.Columns.Clear()

        PictureBox1.Image = Nothing
        Diagrama_Momento.Image = Nothing
        Diagrama_Cortante.Image = Nothing

        _cargando = False

        HayCambios = True
        MessageBox.Show("Los datos de vigas han sido eliminados. Puede importar una nueva exportación de ETABS.",
                        "Datos limpiados", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If e.CloseReason <> CloseReason.UserClosing Then Return

        If HayCambios Then
            Dim result As DialogResult = MessageBox.Show(
                "Hay cambios sin guardar. ¿Deseas guardarlos antes de salir?",
                "Salir",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning)

            If result = DialogResult.Yes Then
                Guardar()
            ElseIf result = DialogResult.Cancel Then
                e.Cancel = True
                Return
            End If
        End If

        e.Cancel = True
        Me.Hide()
    End Sub

    Private Sub Ref_Transversal_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles Ref_Transversal.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    Private Sub Tabla_Resultados_Cortante_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles Tabla_Resultados_Cortante.CellPainting
        PintarLineasCada3Columnas(sender, e)
    End Sub

    ' =========================================================
    ' EXTRACCION ESTRIBOS DESDE GRID
    ' =========================================================

    Private Function ExtraerEstribosDesdeGrid(dgv As DataGridView) _
        As List(Of (FrameLabel As String, Posicion As PosicionTramoViga, NumEstribos As Integer, NumBarra As Integer, CantEstribos As Integer, Separacion As Double))

        Dim lista As New List(Of (String, PosicionTramoViga, Integer, Integer, Integer, Double))

        For col As Integer = 0 To dgv.Columns.Count - 1

            Dim partes = dgv.Columns(col).HeaderText.Split({vbCrLf}, StringSplitOptions.None)
            If partes.Length < 2 Then Continue For

            Dim frameLabel = partes(0).Trim()
            Dim posicionTexto = partes(1).Trim()

            Dim posicion As PosicionTramoViga = If(posicionTexto = "Izq", PosicionTramoViga.Izquierda,
                                                If(posicionTexto = "Centro", PosicionTramoViga.Centro,
                                                                              PosicionTramoViga.Derecha))

            Dim numEstribos As Integer = 10
            Dim numBarra As Integer = 3
            Dim cantEstribos As Integer = 2
            Dim separacion As Double = 0.1

            If dgv.Rows(0).Cells(col).Value IsNot Nothing Then Integer.TryParse(dgv.Rows(0).Cells(col).Value.ToString(), numEstribos)
            If dgv.Rows(1).Cells(col).Value IsNot Nothing Then Integer.TryParse(dgv.Rows(1).Cells(col).Value.ToString(), numBarra)
            If dgv.Rows(2).Cells(col).Value IsNot Nothing Then Integer.TryParse(dgv.Rows(2).Cells(col).Value.ToString(), cantEstribos)
            If dgv.Rows(3).Cells(col).Value IsNot Nothing Then
                ' Normaliza punto/coma para que funcione independiente de la cultura del sistema
                Dim strSep = dgv.Rows(3).Cells(col).Value.ToString().Replace(",", ".")
                Double.TryParse(strSep, Globalization.NumberStyles.Any,
                                Globalization.CultureInfo.InvariantCulture, separacion)
            End If

            lista.Add((frameLabel, posicion, numEstribos, numBarra, cantEstribos, separacion))

        Next

        Return lista

    End Function

    ' =========================================================
    ' CARGAR REFUERZO TRANSVERSAL DESDE MODELO
    ' =========================================================

    Private Sub CargarRefuerzoTransversalTabla(viga As cViga, dgv As DataGridView)

        For col As Integer = 0 To dgv.Columns.Count - 1

            Dim partes = dgv.Columns(col).HeaderText.Split({vbCrLf}, StringSplitOptions.None)
            If partes.Length < 2 Then Continue For

            Dim frameLabel As String = partes(0).Trim()
            Dim posicionTexto As String = partes(1).Trim()

            Dim posicion As PosicionTramoViga
            Select Case posicionTexto
                Case "Izq" : posicion = PosicionTramoViga.Izquierda
                Case "Centro" : posicion = PosicionTramoViga.Centro
                Case "Der" : posicion = PosicionTramoViga.Derecha
                Case Else : Continue For
            End Select

            Dim frame = viga.Frames.Find(Function(f) f.ObjectLabel = frameLabel)
            If frame Is Nothing Then Continue For

            Dim zona = frame.RefuerzoTransversal.FirstOrDefault(Function(z) z.Posicion = posicion)
            If zona Is Nothing Then Continue For

            dgv.Rows(0).Cells(col).Value = zona.NumEstribos
            dgv.Rows(1).Cells(col).Value = zona.NumeroBarra
            dgv.Rows(2).Cells(col).Value = zona.CantEstribos
            dgv.Rows(3).Cells(col).Value = zona.Separacion

        Next

    End Sub

    ' =========================================================
    ' TABLA RESULTADOS CORTANTE
    ' =========================================================

    Private Sub ConstruirTablaCortante(viga As cViga, dgv As DataGridView)

        dgv.Columns.Clear()
        dgv.Rows.Clear()

        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect

        dgv.RowHeadersVisible = True
        dgv.RowHeadersWidth = 180

        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.ScrollBars = ScrollBars.Both

        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single
        dgv.GridColor = Color.FromArgb(210, 210, 210)

        dgv.EnableHeadersVisualStyles = False

        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)

        dgv.BackgroundColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black
        dgv.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
        dgv.DefaultCellStyle.BackColor = Color.White
        dgv.DefaultCellStyle.ForeColor = Color.Black
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240)
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black

        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        dgv.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft

        dgv.ColumnHeadersHeight = 45
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.RowTemplate.Height = 28

        For Each frame In viga.Frames
            Dim frameName As String = frame.ObjectLabel
            Dim c1 = dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            Dim c2 = dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            Dim c3 = dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")
            dgv.Columns(c1).Width = 75
            dgv.Columns(c2).Width = 75
            dgv.Columns(c3).Width = 75
        Next

        Dim nombresFilas As String() = {
            "Sección",
            "Longitud (m)",
            "H (m)",
            "d (m)",
            "Zona 2H (m)",
            "Vu (kN)",
            "Vc (kN)",
            "Vs (kN)",
            "φVn (kN)",
            "F = φVn/Vu"
        }

        For Each nombre In nombresFilas
            Dim idx = dgv.Rows.Add()
            dgv.Rows(idx).HeaderCell.Value = nombre
        Next

        For i As Integer = 0 To dgv.Rows.Count - 1
            If i Mod 2 = 0 Then
                dgv.Rows(i).DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            End If
        Next

    End Sub

    Private Sub MostrarResultadosCortante(viga As cViga)

        Dim dgv As DataGridView = Tabla_Resultados_Cortante
        Dim colBase As Integer = 0

        For Each frame In viga.Frames

            Dim posiciones = {
                PosicionTramoViga.Izquierda,
                PosicionTramoViga.Centro,
                PosicionTramoViga.Derecha
            }

            For Each pos In posiciones

                Dim zonaCor = frame.RevisionCortante.FirstOrDefault(Function(z) z.Posicion = pos)

                If pos = PosicionTramoViga.Centro Then
                    dgv.Rows(FILA_COR_SECCION).Cells(colBase).Value = frame.Section.LabelSec
                    dgv.Rows(FILA_COR_LONGITUD).Cells(colBase).Value = Math.Round(frame.Longitud, 2)
                    dgv.Rows(FILA_COR_H).Cells(colBase).Value = Math.Round(frame.Section.h, 3)
                    dgv.Rows(FILA_COR_D).Cells(colBase).Value = Math.Round(frame.Section.d, 3)
                    dgv.Rows(FILA_COR_ZONA).Cells(colBase).Value = Math.Round(2.0 * frame.Section.h, 3)
                End If

                If zonaCor IsNot Nothing Then

                    dgv.Rows(FILA_COR_VU).Cells(colBase).Value = Math.Round(zonaCor.Vu, 2)

                    If zonaCor.phiVn > 0 Then
                        dgv.Rows(FILA_COR_VC).Cells(colBase).Value = Math.Round(zonaCor.Vc, 2)
                        dgv.Rows(FILA_COR_VS).Cells(colBase).Value = Math.Round(zonaCor.Vs, 2)
                        dgv.Rows(FILA_COR_PHIVN).Cells(colBase).Value = Math.Round(zonaCor.phiVn, 2)

                        dgv.Rows(FILA_COR_FACTOR).Cells(colBase).Value = Math.Round(zonaCor.Factor, 2)
                        PintarCelda(dgv.Rows(FILA_COR_FACTOR).Cells(colBase), zonaCor.Factor)
                    End If

                End If

                colBase += 1
            Next

        Next

    End Sub

    ' =========================================================
    ' RECARGA DE COMBINACIONES (sin re-importar)
    ' =========================================================

    Private Sub ReseleccionarCombinacionesDiseno(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Vigas.Lista_Combinaciones.Count = 0 Then
            MessageBox.Show("No hay combinaciones cargadas. Importe las demandas primero.", "Combinaciones", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim form As New Form_Opciones_Combinaciones()
        For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
            If Not Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Contains(comb) Then
                form.Lista_Combinaciones.Items.Add(comb)
            End If
        Next
        For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones_Design
            form.Lista_Cargas_Design.Items.Add(comb)
        Next
        form.OpcionLlamado = "Vigas"
        form.GroupBox2.Text = "Combinaciones Diseño a Flexión de Vigas"
        form.ShowDialog()
    End Sub

    Private Sub ReseleccionarCombinacionesCortante(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Vigas.Lista_Combinaciones.Count = 0 Then
            MessageBox.Show("No hay combinaciones cargadas. Importe las demandas primero.", "Combinaciones", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim form As New Form_Opciones_Combinaciones()
        For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
            If Not Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante.Contains(comb) Then
                form.Lista_Combinaciones.Items.Add(comb)
            End If
        Next
        For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones_Cortante
            form.Lista_Cargas_Design.Items.Add(comb)
        Next
        form.OpcionLlamado = "VigasCortante"
        form.GroupBox2.Text = "Combinaciones Diseño a Cortante de Vigas"
        form.ShowDialog()
    End Sub

    Private Sub ReseleccionarCombinacionesPlastico(sender As Object, e As EventArgs)
        If Proyecto.Elementos.Vigas.Lista_Combinaciones.Count = 0 Then
            MessageBox.Show("No hay combinaciones cargadas. Importe las demandas primero.", "Combinaciones", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim form As New Form_Opciones_Combinaciones()
        For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
            If Not Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico.Contains(comb) Then
                form.Lista_Combinaciones.Items.Add(comb)
            End If
        Next
        For Each comb As String In Proyecto.Elementos.Vigas.Lista_Combinaciones_CortantePlastico
            form.Lista_Cargas_Design.Items.Add(comb)
        Next
        form.OpcionLlamado = "CortantePlastico"
        form.GroupBox2.Text = "Combinación Gravitacional — Cortante Plástico (wu)"
        form.ShowDialog()
    End Sub

End Class