Imports System.Linq
Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias
'Imports DocumentFormat.OpenXml.Presentation
'Imports DocumentFormat.OpenXml.Drawing
'Imports DocumentFormat.OpenXml.Drawing.Charts
'Imports DocumentFormat.OpenXml.Drawing.Diagrams


Public Class Form_09_Vigas
    Public Shared Proyecto As Proyecto

    Private _vigas As List(Of cViga)
    Private _joints As Dictionary(Of String, cJoint)

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'Try
        '    ' 1. Cargar datos (si aplica)

        Dim Joints = Proyecto.Elementos.Joints
        Dim Frames = Proyecto.Elementos.Frames
        Dim beams As List(Of cFrame) = Frames.Where(Function(f) f.ObjectLabel.StartsWith("B")).ToList()

        Dim jointsDict As Dictionary(Of String, cJoint) = Proyecto.Elementos.Joints.ToDictionary(Function(j) j.ElementLabel)

        ' 2. Generar vigas a partir de joints y frames
        Dim vigas As List(Of cViga) = GenerarVigas(beams, jointsDict)

        For Each v In vigas
            OrdenarFramesViga(v, jointsDict)
        Next

        Proyecto.Elementos.Vigas.Vigas = vigas

        _vigas = GenerarVigas(beams, jointsDict)
        _joints = jointsDict

        For Each v In _vigas
            OrdenarFramesViga(v, _joints)
        Next

        CalcularEnvolventesVigas()

        designVigas()

        Lista_Vigas.DataSource = Nothing
        Lista_Vigas.DataSource = vigas
        Lista_Vigas.DisplayMember = "Nombre"

        Dim stories As List(Of String) = Frames.Select(Function(f) f.Story).Distinct().OrderBy(Function(s) s).ToList()

        Lista_Pisos.DataSource = Nothing
        Lista_Pisos.DataSource = stories

        If Lista_Vigas.SelectedItem IsNot Nothing Then

            Dim vigaSel As cViga = CType(Lista_Vigas.SelectedItem, cViga)

            Lista_Pisos.SelectedItem = vigaSel.Piso

        End If


        MessageBox.Show("Proceso finalizado correctamente", "OK",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)

        'Catch ex As Exception
        '    MessageBox.Show(ex.Message, "Error",
        '                    MessageBoxButtons.OK, MessageBoxIcon.Error)
        'End Try



    End Sub

    Function PuntoMedioFrame(f As cFrame,
                          joints As Dictionary(Of String, cJoint)) As Vector3

        Dim ji = joints(f.JointI)
        Dim jj = joints(f.JointJ)

        Return New Vector3(
        (ji.GlobalX + jj.GlobalX) / 2.0,
        (ji.GlobalY + jj.GlobalY) / 2.0,
        (ji.GlobalZ + jj.GlobalZ) / 2.0
    )
    End Function

    Sub OrdenarFramesViga(viga As cViga,
                      joints As Dictionary(Of String, cJoint))

        Dim dir = viga.Direccion
        dir.Normalize()

        viga.Frames = viga.Frames _
        .OrderBy(Function(f)
                     Dim p = PuntoMedioFrame(f, joints)
                     Return Vector3.Dot(p, dir)
                 End Function) _
        .ToList()
    End Sub

    Function PuntoJoint(id As String, joints As Dictionary(Of String, cJoint)) As Vector3
        Dim j = joints(id)
        Return New Vector3(j.GlobalX, j.GlobalY, j.GlobalZ)
    End Function

    Function VectorFrame(f As cFrame, joints As Dictionary(Of String, cJoint)) As Vector3
        Return PuntoJoint(f.JointJ, joints) - PuntoJoint(f.JointI, joints)
    End Function

    Function SonColineales(f As cFrame,
                       dirViga As Vector3,
                       joints As Dictionary(Of String, cJoint),
                       tol As Double) As Boolean

        Dim v = VectorFrame(f, joints)
        v.Normalize()

        ' Producto cruz con la dirección de la viga
        Dim cross = Vector3.Cross(v, dirViga)

        Return cross.Length < tol
    End Function

    Function CompartenJoint(f1 As cFrame, f2 As cFrame) As Boolean
        Return f1.JointI = f2.JointI OrElse
           f1.JointI = f2.JointJ OrElse
           f1.JointJ = f2.JointI OrElse
           f1.JointJ = f2.JointJ
    End Function

    Private Function Distancia(f As cFrame,
                       joints As Dictionary(Of String, cJoint)) As Double

        ' Obtener joints I y J desde el diccionario
        Dim j1 As cJoint = Nothing
        Dim j2 As cJoint = Nothing

        If Not joints.TryGetValue(f.JointI, j1) Then
            Throw New Exception($"No se encontró el Joint I: {f.JointI}")
        End If

        If Not joints.TryGetValue(f.JointJ, j2) Then
            Throw New Exception($"No se encontró el Joint J: {f.JointJ}")
        End If

        ' Calcular distancia 3D
        Return Math.Sqrt(
        (j1.GlobalX - j2.GlobalX) ^ 2 +
        (j1.GlobalY - j2.GlobalY) ^ 2 +
        (j1.GlobalZ - j2.GlobalZ) ^ 2
    )
    End Function

    Function GenerarVigas(frames As List(Of cFrame),
                      joints As Dictionary(Of String, cJoint)) _
                      As List(Of cViga)

        Dim vigas As New List(Of cViga)
        Dim framesPendientes As New HashSet(Of cFrame)(frames)
        Dim tol As Double = 0.01

        While framesPendientes.Any()

            Dim fBase = framesPendientes.First()
            framesPendientes.Remove(fBase)

            Dim viga As New cViga With {
            .Nombre = $"VIGA-{vigas.Count + 1}",
            .Piso = fBase.Story
        }

            viga.Name_Beam = viga.Nombre

            viga.Frames.Add(fBase)

            Dim dir = VectorFrame(fBase, joints)
            dir.Normalize()
            viga.Direccion = dir

            Dim agrego As Boolean

            Do
                agrego = False

                For Each f In framesPendientes.ToList()

                    If f.Section.Nombre <> fBase.Section.Nombre Then Continue For
                    If f.Story <> fBase.Story Then Continue For

                    If viga.Frames.Any(Function(fv) CompartenJoint(fv, f)) AndAlso
                                                    SonColineales(f, viga.Direccion, joints, tol) Then

                        viga.Frames.Add(f)
                        framesPendientes.Remove(f)
                        agrego = True
                    End If

                Next

            Loop While agrego

            vigas.Add(viga)

        End While

        Return vigas
    End Function

    Private Sub Lista_Pisos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Lista_Pisos.SelectedIndexChanged

        If Me.DesignMode Then Return

        If Lista_Pisos.SelectedItem Is Nothing Then Exit Sub

        DibujarPlanta()

    End Sub

    Private Sub DibujarPlanta()

        If _vigas Is Nothing OrElse _vigas.Count = 0 Then Exit Sub
        If Lista_Pisos.SelectedItem Is Nothing Then Exit Sub
        Dim vigaSeleccionada As cViga = TryCast(Lista_Vigas.SelectedItem, cViga)

        Dim pisoSeleccionado As String = Lista_Pisos.SelectedItem.ToString()

        ' Filtrar vigas del piso seleccionado
        Dim vigasPiso = _vigas.Where(Function(v) v.Frames.Any(Function(f) f.Story = pisoSeleccionado)).ToList()
        If vigasPiso.Count = 0 Then Exit Sub

        ' Crear imagen
        PictureBox1.Image = New Bitmap(PictureBox1.Width, PictureBox1.Height)

        Using g As Graphics = Graphics.FromImage(PictureBox1.Image)

            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.White)

            ' Calcular Bounding Box de todos los joints del piso
            Dim bbox = CalcularBoundingBox(vigasPiso)

            ' 🔹 1. Dibujar ejes
            DibujarGridLines(g, Proyecto.Elementos.Grids.GridLines, bbox)

            ' Dibujar cada viga
            For Each v In vigasPiso
                Dim esSeleccionada As Boolean = False

                If vigaSeleccionada IsNot Nothing Then
                    esSeleccionada = (v.Name_Beam = vigaSeleccionada.Name_Beam)
                End If

                DibujarViga(g, v, bbox, esSeleccionada)
            Next

        End Using

        PictureBox1.Refresh()
    End Sub

    Private Sub DibujarGridLines(
    g As Graphics,
    grids As List(Of cGridLine),
    bbox As RectangleF)

        If grids Is Nothing OrElse grids.Count = 0 Then Exit Sub

        Dim penGrid As New Pen(Color.Gray, 1)
        penGrid.DashStyle = Drawing2D.DashStyle.Dash

        Dim fontGrid As New Font("Segoe UI", 8.5F, FontStyle.Bold)
        Dim brushText As Brush = Brushes.Black
        Dim brushBubble As Brush = Brushes.White
        Dim penBubble As New Pen(Color.Gray, 1)

        ' 🔹 Rango real del modelo
        Dim minX As Double = _joints.Values.Min(Function(j) j.GlobalX)
        Dim maxX As Double = _joints.Values.Max(Function(j) j.GlobalX)
        Dim minY As Double = _joints.Values.Min(Function(j) j.GlobalY)
        Dim maxY As Double = _joints.Values.Max(Function(j) j.GlobalY)

        ' 🔹 Extensión real del grid (metros)
        Dim margen As Double = 1.5
        Dim radioBurbuja As Single = 7.5

        For Each gl In grids

            If Not gl.Visible Then Continue For
            If String.IsNullOrWhiteSpace(gl.GridID) Then Continue For

            If gl.Direction = "X" Then
                ' ============================================
                ' 🔹 GRID X → LÍNEA VERTICAL
                ' ============================================
                Dim p1 = Transformar(New Vector3(gl.Ordinate, minY - margen, 0), bbox)
                Dim p2 = Transformar(New Vector3(gl.Ordinate, maxY + margen, 0), bbox)

                g.DrawLine(penGrid, p1, p2)

                ' 🔹 Posición del ID (arriba o abajo)
                Dim yTexto As Single =
                If(gl.BubbleLocation.ToLower() = "start", p2.Y + 15, p1.Y - 15)

                Dim pTexto As New PointF(p1.X, yTexto)

                DibujarBurbujaGrid(g, pTexto, gl.GridID, fontGrid, penBubble, brushBubble, brushText, gl.Direction)

            ElseIf gl.Direction = "Y" Then
                ' ============================================
                ' 🔹 GRID Y → LÍNEA HORIZONTAL
                ' ============================================
                Dim p1 = Transformar(New Vector3(minX - margen, gl.Ordinate, 0), bbox)
                Dim p2 = Transformar(New Vector3(maxX + margen, gl.Ordinate, 0), bbox)

                g.DrawLine(penGrid, p1, p2)

                ' 🔹 Posición del ID (izq o der)
                Dim xTexto As Single =
                If(gl.BubbleLocation.ToLower() = "start", p1.X - 15, p2.X + 15)

                Dim pTexto As New PointF(xTexto, p1.Y)

                DibujarBurbujaGrid(g, pTexto, gl.GridID, fontGrid, penBubble, brushBubble, brushText, gl.Direction)
            End If
        Next
    End Sub

    Private Sub DibujarBurbujaGrid(g As Graphics, centro As PointF, texto As String, font As Font, pen As Pen, fondo As Brush, textoBrush As Brush, Gl_Dir As String)

        Dim radio As Single = 10

        Dim Sig_Dir = 1

        If Gl_Dir = "Y" Then
            Sig_Dir = -1
        End If

        Dim rect As New RectangleF(
        centro.X - radio,
        centro.Y + Sig_Dir * radio,
        radio * 2,
        radio * 2)

        ' Burbuja
        g.FillEllipse(fondo, rect)
        g.DrawEllipse(pen, rect)

        ' Texto centrado
        Dim sf As New StringFormat With {
        .Alignment = StringAlignment.Center,
        .LineAlignment = StringAlignment.Center
    }

        g.DrawString(texto, font, textoBrush, rect, sf)
    End Sub


    Private Function CalcularBoundingBox(vigasPiso As List(Of cViga)) As RectangleF
        Dim xs As New List(Of Double)
        Dim ys As New List(Of Double)

        For Each v In vigasPiso
            For Each f In v.Frames
                Dim ji = _joints(f.JointI)
                Dim jj = _joints(f.JointJ)
                xs.Add(ji.GlobalX) : xs.Add(jj.GlobalX)
                ys.Add(ji.GlobalY) : ys.Add(jj.GlobalY)
            Next
        Next

        Return New RectangleF(CSng(xs.Min()), CSng(ys.Min()),
                          CSng(xs.Max() - xs.Min()), CSng(ys.Max() - ys.Min()))
    End Function


    Private Sub DibujarViga(g As Graphics, viga As cViga, bbox As RectangleF, Optional seleccionada As Boolean = False)

        Dim font As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Dim brushTexto As Brush = Brushes.Black

        Dim colorViga As Color = Color.DarkBlue
        Dim espesor As Single = 2

        If seleccionada Then
            colorViga = Color.Red
            espesor = 3
        End If

        ' =====================================================
        ' 🔹 Dibujar frames de la viga
        ' =====================================================
        For Each f In viga.Frames

            Dim ji = _joints(f.JointI)
            Dim jj = _joints(f.JointJ)

            Dim p1 As PointF = Transformar(New Vector3(ji.GlobalX, ji.GlobalY, 0), bbox)
            Dim p2 As PointF = Transformar(New Vector3(jj.GlobalX, jj.GlobalY, 0), bbox)

            ' Halo de selección (más profesional)
            If seleccionada Then
                Using penHalo As New Pen(Color.Yellow, espesor + 4)
                    g.DrawLine(penHalo, p1, p2)
                End Using
            End If

            Using pen As New Pen(colorViga, espesor)
                g.DrawLine(pen, p1, p2)
            End Using

        Next

        ' =====================================================
        ' 🔹 Calcular punto medio de la viga
        ' =====================================================
        Dim minX As Single = Single.MaxValue
        Dim maxX As Single = Single.MinValue
        Dim minY As Single = Single.MaxValue
        Dim maxY As Single = Single.MinValue

        For Each f In viga.Frames

            Dim ji = _joints(f.JointI)
            Dim jj = _joints(f.JointJ)

            minX = Math.Min(minX, Math.Min(ji.GlobalX, jj.GlobalX))
            maxX = Math.Max(maxX, Math.Max(ji.GlobalX, jj.GlobalX))
            minY = Math.Min(minY, Math.Min(ji.GlobalY, jj.GlobalY))
            maxY = Math.Max(maxY, Math.Max(ji.GlobalY, jj.GlobalY))

        Next

        Dim midPoint As New Vector3((minX + maxX) / 2, (minY + maxY) / 2, 0)
        Dim pMid As PointF = Transformar(midPoint, bbox)

        ' =====================================================
        ' 🔹 Calcular orientación de la viga
        ' =====================================================
        Dim fRef = viga.Frames(0)

        Dim jiRef = _joints(fRef.JointI)
        Dim jjRef = _joints(fRef.JointJ)

        Dim dx As Single = jjRef.GlobalX - jiRef.GlobalX
        Dim dy As Single = jjRef.GlobalY - jiRef.GlobalY

        Dim angle As Single = CSng(Math.Atan2(dy, dx) * 180 / Math.PI)

        ' =====================================================
        ' 🔹 Dibujar nombre de la viga
        ' =====================================================
        g.TranslateTransform(pMid.X, pMid.Y)
        g.RotateTransform(angle)

        Dim size = g.MeasureString(viga.Nombre, font)

        Dim rectTexto As New RectangleF(
        -size.Width / 2,
        -size.Height / 2,
        size.Width,
        size.Height)

        ' Fondo blanco para legibilidad
        Using brushFondo As New SolidBrush(Color.FromArgb(200, Color.White))
            g.FillRectangle(brushFondo, rectTexto)
        End Using

        g.DrawString(viga.Nombre, font, brushTexto, rectTexto.Location)

        g.ResetTransform()

    End Sub

    Private Function Transformar(p As Vector3, bbox As RectangleF) As PointF
        Dim canvasWidth As Single = PictureBox1.Width
        Dim canvasHeight As Single = PictureBox1.Height
        Dim margin As Single = 40

        ' Escala proporcional
        Dim scaleX = (canvasWidth - 2 * margin) / bbox.Width
        Dim scaleY = (canvasHeight - 2 * margin) / bbox.Height
        Dim scale = Math.Min(scaleX, scaleY)

        ' Ajustar para centrar
        Dim x As Single = (p.X - bbox.Left) * scale + margin + (canvasWidth - 2 * margin - bbox.Width * scale) / 2
        Dim y As Single = canvasHeight - ((p.Y - bbox.Top) * scale + margin + (canvasHeight - 2 * margin - bbox.Height * scale) / 2)

        Return New PointF(x, y)
    End Function

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
                    ' Leer cada hoja

                    If Proyecto.Elementos.Frames.Count = 0 Then
                        Proyecto.TablasEtabs.TablaOEJoints = LeerHojaExcel(path, "Objects and Elements - Joints")
                        Proyecto.TablasEtabs.TablaOEFrames = LeerHojaExcel(path, "Objects and Elements - Frames")

                        Proyecto.Elementos.Joints = DataTableToJoints(Proyecto.TablasEtabs.TablaOEJoints)
                        Proyecto.Elementos.Frames = DataTableToFrames(Proyecto.TablasEtabs.TablaOEFrames)

                        ' APLICAR SECCIONES A FRAMES
                        'Dim Data_Asig_Frame As DataTable = LeerHojaExcel(path, "Frame Assignments - Sections")
                        'Dim Data_Frame_Section As DataTable = LeerHojaExcel(path, "Frame Sections")
                        'Dim Data_Material_Concrete As DataTable = LeerHojaExcel(path, "Material Properties - Concrete")

                        Dim Data_Asig_Frame As DataTable = LeerHojaExcel(path, "Frame Assigns - Sect Prop")
                        Dim Data_Frame_Section As DataTable = LeerHojaExcel(path, "Frame Sec Def - Conc Rect")
                        Dim Data_Material_Concrete As DataTable = LeerHojaExcel(path, "Mat Prop - Concrete Data")

                        DataTableToAsignFrame(Proyecto.Elementos.Frames, Data_Asig_Frame, Data_Frame_Section, Data_Material_Concrete)

                    End If

                    'Proyecto.Elementos.Vigas.Tabla_BeamForces = LeerHojaExcel(path, "Beam Forces")
                    Proyecto.Elementos.Vigas.Tabla_BeamForces = LeerHojaExcel(path, "Element Forces - Beams")

                    MsgBox("Importación completada correctamente.", MsgBoxStyle.Information)

                    'Dim Table_Grids As DataTable = LeerHojaExcel(path, "Grid Lines")
                    Dim Table_Grids As DataTable = LeerHojaExcel(path, "Grid Definitions - Grid Lines")

                    Proyecto.Elementos.Vigas.BeamForces = DataTableToBeamForces(Proyecto.Elementos.Vigas.Tabla_BeamForces)

                    Proyecto.Elementos.Grids.GridLines = DataTableToGridLines(Table_Grids)


                    ' 🔹 Extraer combinaciones únicas
                    Proyecto.Elementos.Vigas.Lista_Combinaciones = Proyecto.Elementos.Vigas.BeamForces.Select(Function(r) r.LoadCaseCombo) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

                    For Each Combinacion As String In Proyecto.Elementos.Vigas.Lista_Combinaciones
                        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(Combinacion)
                    Next

                    Form_Opciones_Combinaciones.OpcionLlamado = "Vigas"

                    Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones Diseño de Vigas"

                    Form_Opciones_Combinaciones.ShowDialog()


                    MsgBox("Importación completada.", MsgBoxStyle.Information)

                Catch ex As Exception
                    MsgBox("Error al importar: " & ex.Message, MsgBoxStyle.Critical)
                Finally
                    Me.Cursor = Cursors.Arrow
                End Try
            End If
        End With


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        If Lista_Vigas.SelectedItem Is Nothing Then Exit Sub
        Dim Elemento As cViga = CType(Lista_Vigas.SelectedItem, cViga)
        Elemento.Name_Beam = Nombre_Viga.Text

        Lista_Vigas.Refresh()

        If Elemento Is Nothing Then Return

        DibujarDiagramaMomentoFrames(Elemento, Diagrama_Momento)
        DibujarDiagramaCortanteFrames(Elemento, Diagrama_Cortante)

        ConstruirTablaResumen(Elemento, Tabla_Demandas)

        ConstruirTablaRefuerzo(Elemento, Ref_Superior)
        ConstruirTablaRefuerzo(Elemento, Ref_Inferior)

        CargarRefuerzoTabla(Elemento, Ref_Superior, eTipoRefuerzo.Superior)
        CargarRefuerzoTabla(Elemento, Ref_Inferior, eTipoRefuerzo.Inferior)

        ConstruirTablaResultadosFlexion(Elemento, Tabla_Resultados_Flexion)

        LlenarTablaResumen(Elemento, Tabla_Demandas)
        LlenarTablaResultados(Elemento, Tabla_Resultados_Flexion)

    End Sub

    Private Sub CargarVigaSeleccionada()

        If Lista_Vigas.SelectedItem Is Nothing Then Exit Sub

        Dim Elemento As cViga = CType(Lista_Vigas.SelectedItem, cViga)

        DibujarDiagramaMomentoFrames(Elemento, Diagrama_Momento)
        DibujarDiagramaCortanteFrames(Elemento, Diagrama_Cortante)

        ConstruirTablaResumen(Elemento, Tabla_Demandas)

        ConstruirTablaRefuerzo(Elemento, Ref_Superior)
        ConstruirTablaRefuerzo(Elemento, Ref_Inferior)

        ConstruirTablaResultadosFlexion(Elemento, Tabla_Resultados_Flexion)

        LlenarTablaResumen(Elemento, Tabla_Demandas)
        LlenarTablaResultados(Elemento, Tabla_Resultados_Flexion)

        ' 🔥 cargar refuerzo guardado
        CargarRefuerzoTabla(Elemento, Ref_Superior, eTipoRefuerzo.Superior)
        CargarRefuerzoTabla(Elemento, Ref_Inferior, eTipoRefuerzo.Inferior)

    End Sub

    Private Sub CargarVigaCompleta(viga As cViga)

        If viga Is Nothing Then Exit Sub

        ' Dibujos
        DibujarDiagramaMomentoFrames(viga, Diagrama_Momento)
        'DibujarDiagramaCortanteFrames(viga, Diagrama_Cortante)

        ' Tablas
        ConstruirTablaResumen(viga, Tabla_Demandas)

        ConstruirTablaRefuerzo(viga, Ref_Superior)
        ConstruirTablaRefuerzo(viga, Ref_Inferior)

        ConstruirTablaResultadosFlexion(viga, Tabla_Resultados_Flexion)

        LlenarTablaResumen(viga, Tabla_Demandas)
        LlenarTablaResultados(viga, Tabla_Resultados_Flexion)

        ' 🔥 MUY IMPORTANTE: recargar refuerzo guardado
        CargarRefuerzoTabla(viga, Ref_Superior, eTipoRefuerzo.Superior)
        CargarRefuerzoTabla(viga, Ref_Inferior, eTipoRefuerzo.Inferior)

        If ExisteRefuerzo(viga) Then
            GuardarRefuerzoTabla(viga, Ref_Superior, eTipoRefuerzo.Superior)
            GuardarRefuerzoTabla(viga, Ref_Inferior, eTipoRefuerzo.Inferior)
            CalcularFlexionViga(viga)
            MostrarResultadosFlexion(viga)
        End If

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

    Private Sub CalcularEnvolventesVigas()

        For Each viga In Proyecto.Elementos.Vigas.Vigas

            For Each frame In viga.Frames

                Dim bfFrame = Proyecto.Elementos.Vigas.BeamForces _
                .Where(Function(r) r.Beam = frame.ObjectLabel _
                AndAlso r.Story = frame.Story _
                AndAlso Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Contains(r.LoadCaseCombo)) _
                .ToList()

                If bfFrame.Count = 0 Then Continue For

                Dim estaciones As List(Of Double)
                Dim envMax As List(Of Double)
                Dim envMin As List(Of Double)

                ConstruirEnvolventeAnalisis(bfFrame, estaciones, envMax, envMin)

                EnvolventeNSR10(estaciones, envMax, envMin)

                frame.EnvolventeMomento = New cEnvolventeMomento With {
                .Estaciones = estaciones,
                .MmaxDesign = envMax,
                .MminDesign = envMin
            }

                ' =====================================
                ' 🔹 Guardar momentos por zona
                ' =====================================

                frame.RevisionFlexion.Clear()

                Dim iIzq As Integer = 0
                Dim iDer As Integer = estaciones.Count - 1

                ' buscar estación más cercana al centro
                Dim L As Double = estaciones.Last()

                Dim iCen As Integer = 0
                Dim distMin As Double = Double.MaxValue

                For i As Integer = 0 To estaciones.Count - 1

                    Dim dist = Math.Abs(estaciones(i) - L / 2)

                    If dist < distMin Then
                        distMin = dist
                        iCen = i
                    End If

                Next

                ' ==============================
                ' IZQUIERDA
                ' ==============================

                frame.RevisionFlexion.Add(New cRevisionFlexionZona With {
                    .Posicion = PosicionTramoViga.Izquierda,
                    .MomentoPositivo = envMax(iIzq),
                    .MomentoNegativo = envMin(iIzq)
                })

                ' ==============================
                ' CENTRO
                ' ==============================

                frame.RevisionFlexion.Add(New cRevisionFlexionZona With {
                    .Posicion = PosicionTramoViga.Centro,
                    .MomentoPositivo = envMax(iCen),
                    .MomentoNegativo = envMin(iCen)
                })

                ' ==============================
                ' DERECHA
                ' ==============================

                frame.RevisionFlexion.Add(New cRevisionFlexionZona With {
                    .Posicion = PosicionTramoViga.Derecha,
                    .MomentoPositivo = envMax(iDer),
                    .MomentoNegativo = envMin(iDer)
                })



            Next

        Next

    End Sub

    Private Function ObtenerCombinacionesFrame(frame As cFrame) _
    As Dictionary(Of String, List(Of cCombinacionBeamForce))

        Dim bfFrame = Proyecto.Elementos.Vigas.BeamForces _
        .Where(Function(r) r.Beam = frame.ObjectLabel _
        AndAlso r.Story = frame.Story _
        AndAlso Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Contains(r.LoadCaseCombo)) _
        .ToList()

        Return bfFrame _
        .GroupBy(Function(bf) bf.LoadCaseCombo) _
        .ToDictionary(Function(gp) gp.Key,
                      Function(gp) gp.OrderBy(Function(bf) bf.ElementStation).ToList())

    End Function

    Private Sub DibujarDiagramaMomentoFrames(viga As cViga, pictureBox As PictureBox)

        If viga.Frames Is Nothing OrElse viga.Frames.Count = 0 Then Exit Sub

        Dim bmp As New Bitmap(pictureBox.Width, pictureBox.Height)
        Dim g As Graphics = Graphics.FromImage(bmp)

        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        g.Clear(Color.White)

        Dim margin As Single = 50

        Dim colores As Color() =
    {
        Color.Red, Color.Blue, Color.DarkOrange,
        Color.Purple, Color.Brown, Color.DarkCyan
    }

        Dim datosFrame As New List(Of Object)
        Dim todasX As New List(Of Single)
        Dim todasY As New List(Of Double)

        Dim xOffset As Single = 0

        ' =========================================================
        ' 🔹 Preparar datos
        ' =========================================================
        For Each frame In viga.Frames

            If frame.EnvolventeMomento Is Nothing Then Continue For

            Dim estaciones = frame.EnvolventeMomento.Estaciones
            Dim envMax = frame.EnvolventeMomento.MmaxDesign
            Dim envMin = frame.EnvolventeMomento.MminDesign

            Dim combos = ObtenerCombinacionesFrame(frame)

            Dim L As Double = estaciones.Last()

            For Each s In estaciones
                todasX.Add(xOffset + s)
            Next

            For Each m In envMax
                todasY.Add(m)
            Next

            For Each m In envMin
                todasY.Add(m)
            Next

            datosFrame.Add(New With {
            .Frame = frame,
            .Estaciones = estaciones,
            .EnvMax = envMax,
            .EnvMin = envMin,
            .Combos = combos,
            .L = L,
            .Offset = xOffset
        })

            xOffset += L

        Next

        If todasX.Count = 0 Then Exit Sub

        ' =========================================================
        ' 🔹 Escala
        ' =========================================================
        Dim minX = todasX.Min()
        Dim maxX = todasX.Max()

        Dim maxAbsY = Math.Max(Math.Abs(todasY.Min()), Math.Abs(todasY.Max()))

        Dim scaleX As Single = CSng((pictureBox.Width - 2 * margin) / (maxX - minX))
        Dim scaleY As Single = CSng((pictureBox.Height / 2 - margin) / maxAbsY)

        Dim yZero As Single = pictureBox.Height / 2

        Dim TransformX = Function(x As Single) margin + (x - minX) * scaleX
        Dim TransformY = Function(y As Single) yZero + y * scaleY

        ' =========================================================
        ' 🔹 Eje cero
        ' =========================================================
        Using penZero As New Pen(Color.Black, 1)
            penZero.DashStyle = Drawing2D.DashStyle.Dash
            g.DrawLine(penZero, margin, yZero, pictureBox.Width - margin, yZero)
        End Using

        ' =========================================================
        ' 🔹 Dibujar combinaciones
        ' =========================================================
        For iComb = 0 To Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Count - 1

            Dim combo = Proyecto.Elementos.Vigas.Lista_Combinaciones_Design(iComb)
            Dim colorCombo = colores(iComb Mod colores.Length)

            For Each df In datosFrame

                If Not df.Combos.ContainsKey(combo) Then Continue For

                'Dim lista = df.Combos(combo)
                Dim listaTotal = df.Combos(combo)

                ' 🔹 Separar por StepType
                Dim listaMax As New List(Of cCombinacionBeamForce)
                Dim listaMin As New List(Of cCombinacionBeamForce)
                Dim listaNormal As New List(Of cCombinacionBeamForce)

                For Each item In listaTotal
                    Select Case item.stepType
                        Case "Max"
                            listaMax.Add(item)
                        Case "Min"
                            listaMin.Add(item)
                        Case Else
                            listaNormal.Add(item)
                    End Select
                Next

                ' 🔸 Dibujar función reutilizable
                Dim Dibujar = Sub(lista As List(Of cCombinacionBeamForce), pen As Pen)

                                  If lista Is Nothing OrElse lista.Count < 2 Then Exit Sub

                                  ' 🔹 Ordenar por estación (CLAVE)
                                  lista = lista.OrderBy(Function(x) x.ElementStation).ToList()

                                  For i = 0 To lista.Count - 2

                                      Dim x1 = df.Offset + lista(i).ElementStation
                                      Dim y1 = lista(i).M3
                                      Dim x2 = df.Offset + lista(i + 1).ElementStation
                                      Dim y2 = lista(i + 1).M3

                                      ' 🔹 Validar valores (evita overflow)
                                      ' 🔹 Validación compatible con .NET Framework
                                      If (Double.IsNaN(x1) OrElse Double.IsInfinity(x1) OrElse
                                            Double.IsNaN(y1) OrElse Double.IsInfinity(y1)) Then Continue For

                                      If (Double.IsNaN(x2) OrElse Double.IsInfinity(x2) OrElse
                                            Double.IsNaN(y2) OrElse Double.IsInfinity(y2)) Then Continue For

                                      ' 🔹 Protección adicional (valores absurdos)
                                      If Math.Abs(y1) > 1000000000.0 OrElse Math.Abs(y2) > 1000000000.0 Then Continue For

                                      Try
                                          g.DrawLine(pen, TransformX(x1), TransformY(y1), TransformX(x2), TransformY(y2))
                                      Catch ex As OverflowException
                                          ' Puedes loguear si quieres depurar
                                          Debug.Print("Overflow en dibujo: " & ex.Message)
                                      End Try

                                  Next
                              End Sub

                ' 🔹 Dibujar según exista
                Using pen As New Pen(colorCombo, 2)

                    If listaMax.Count > 0 Then Dibujar(listaMax, pen)
                    If listaMin.Count > 0 Then Dibujar(listaMin, pen)

                    ' Si no hay max/min, dibuja la normal
                    If listaMax.Count = 0 AndAlso listaMin.Count = 0 Then
                        Dibujar(listaNormal, pen)
                    End If

                End Using

            Next

        Next

        ' =========================================================
        ' 🔹 Dibujar envolvente NSR
        ' =========================================================
        Using penEnv As New Pen(Color.Green, 3)

            For Each df In datosFrame

                Dim estaciones = df.Estaciones
                Dim envMax = df.EnvMax
                Dim envMin = df.EnvMin

                Dim puntosSup As New List(Of PointF)
                Dim puntosInf As New List(Of PointF)

                For i = 0 To estaciones.Count - 1

                    puntosSup.Add(New PointF(
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMax(i))))

                Next

                For i = estaciones.Count - 1 To 0 Step -1

                    puntosInf.Add(New PointF(
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMin(i))))

                Next

                Dim puntos = puntosSup.Concat(puntosInf).ToArray()

                Using brushEnv As New SolidBrush(Color.FromArgb(60, Color.Green))
                    g.FillPolygon(brushEnv, puntos)
                End Using

                For i = 0 To estaciones.Count - 2

                    g.DrawLine(penEnv,
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMax(i)),
                    TransformX(df.Offset + estaciones(i + 1)),
                    TransformY(envMax(i + 1)))

                    g.DrawLine(penEnv,
                    TransformX(df.Offset + estaciones(i)),
                    TransformY(envMin(i)),
                    TransformX(df.Offset + estaciones(i + 1)),
                    TransformY(envMin(i + 1)))

                Next

                ' =========================================================
                ' 🔹 Etiquetas
                ' =========================================================
                Dim sInicio = estaciones(0)
                Dim sFinal = estaciones(estaciones.Count - 1)

                Dim MnegIzq = envMin(0)
                Dim MnegDer = envMin(envMin.Count - 1)

                DibujarEtiquetaMomento(g,
                Math.Round(MnegIzq, 1).ToString(),
                TransformX(df.Offset + sInicio / 2),
                TransformY(MnegIzq))

                DibujarEtiquetaMomento(g,
                Math.Round(MnegDer, 1).ToString(),
                TransformX(df.Offset + df.L - (df.L - sFinal) / 2),
                TransformY(MnegDer))

                Dim Mpos As Double = Double.MinValue
                Dim iMax As Integer = 0

                For i = 0 To envMax.Count - 1

                    If envMax(i) > Mpos Then
                        Mpos = envMax(i)
                        iMax = i
                    End If

                Next

                Dim sPos = estaciones(iMax)

                DibujarEtiquetaMomento(g,
                Math.Round(Mpos, 1).ToString(),
                TransformX(df.Offset + sPos),
                TransformY(Mpos))

            Next

        End Using

        ' =========================================================
        ' 🔹 Dibujar apoyos
        ' =========================================================
        For i = 0 To datosFrame.Count - 1

            Dim df = datosFrame(i)

            Dim estaciones = df.Estaciones
            Dim L = df.L
            Dim offset = df.Offset

            Dim sInicio = estaciones(0)
            Dim sFinal = estaciones(estaciones.Count - 1)

            If i = 0 Then

                DibujarApoyo(g,
                TransformX(offset),
                TransformX(offset + sInicio),
                pictureBox.Height,
                margin)

            End If

            If i < datosFrame.Count - 1 Then

                Dim dfNext = datosFrame(i + 1)
                Dim estacionesNext = dfNext.Estaciones
                Dim sInicioNext = estacionesNext(0)

                Dim x1 = offset + sFinal
                Dim x2 = offset + L + sInicioNext

                DibujarApoyo(g,
                TransformX(x1),
                TransformX(x2),
                pictureBox.Height,
                margin)

            Else

                DibujarApoyo(g,
                TransformX(offset + sFinal),
                TransformX(offset + L),
                pictureBox.Height,
                margin)

            End If

            Using penTramo As New Pen(Color.Gray, 1)

                penTramo.DashStyle = Drawing2D.DashStyle.Dot

                g.DrawLine(penTramo,
                TransformX(df.Offset + df.L),
                margin,
                TransformX(df.Offset + df.L),
                pictureBox.Height - margin)

            End Using

        Next

        pictureBox.Image = bmp

    End Sub


    Private Sub DibujarApoyo(g As Graphics,
                         x1 As Single,
                         x2 As Single,
                         pictureHeight As Integer,
                         margin As Single)

        Dim rect As New RectangleF(
        x1,
        margin,
        x2 - x1,
        pictureHeight - 2 * margin)

        Using brush As New SolidBrush(Color.FromArgb(90, Color.Gray))
            g.FillRectangle(brush, rect)
        End Using

    End Sub

    Private Sub DibujarEtiquetaMomento(
    g As Graphics,
    texto As String,
    x As Single,
    y As Single)

        Using f As New Font("Segoe UI", 8, FontStyle.Bold)

            Dim size = g.MeasureString(texto, f)

            g.DrawString(texto, f, Brushes.Black,
                     x - size.Width / 2,
                     y - size.Height - 4)

        End Using

    End Sub


    Private Sub ConstruirEnvolventeAnalisis(
    bfFrame As List(Of cCombinacionBeamForce),
    ByRef estaciones As List(Of Double),
    ByRef envMax As List(Of Double),
    ByRef envMin As List(Of Double))


        Dim dictMax As New Dictionary(Of Double, List(Of Double))
        Dim dictMin As New Dictionary(Of Double, List(Of Double))

        For Each bf In bfFrame

            Dim s As Double = bf.Station
            Dim m As Double = bf.M3
            Dim stepType As String = If(bf.stepType, "").Trim().ToUpper()

            Select Case stepType

                Case "MAX"
                    If Not dictMax.ContainsKey(s) Then
                        dictMax(s) = New List(Of Double)
                    End If
                    dictMax(s).Add(m)

                Case "MIN"
                    If Not dictMin.ContainsKey(s) Then
                        dictMin(s) = New List(Of Double)
                    End If

                    dictMin(s).Add(m)
                Case Else
                    ' Opcional: ignorar o usar como fallback
                    ' Aquí lo ignoramos
            End Select

        Next

        ' Unir estaciones de ambos diccionarios
        estaciones = dictMax.Keys.Union(dictMin.Keys).OrderBy(Function(x) x).ToList()

        envMax = New List(Of Double)
        envMin = New List(Of Double)

        For Each s In estaciones

            ' Si no existe, puedes poner 0 o Double.NaN
            envMax.Add(If(dictMax.ContainsKey(s), dictMax(s).Max(), 0))
            envMin.Add(If(dictMin.ContainsKey(s), dictMin(s).Min(), 0))

        Next

        ''Dim dict As New Dictionary(Of Double, (Mmax As Double, Mmin As Double))

        'Dim dictMax As New Dictionary(Of Double, Double)
        'Dim dictMin As New Dictionary(Of Double, Double)


        'For Each bf In bfFrame

        '    Dim s As Double = bf.ElementStation
        '    Dim m As Double = bf.M3
        '    Dim stepType As String = If(bf.stepType, "").Trim().ToUpper()

        '    If Not dict.ContainsKey(s) Then

        '        dict(s) = (m, m)

        '    Else

        '        Dim maxM = Math.Max(dict(s).Mmax, m)
        '        Dim minM = Math.Min(dict(s).Mmin, m)

        '        dict(s) = (maxM, minM)

        '    End If

        'Next

        'estaciones = dict.Keys.OrderBy(Function(x) x).ToList()

        'envMax = New List(Of Double)
        'envMin = New List(Of Double)

        'For Each s In estaciones

        '    envMax.Add(dict(s).Mmax)
        '    envMin.Add(dict(s).Mmin)

        'Next

    End Sub

    Private Sub EnvolventeNSR10(
    estaciones As List(Of Double),
    envMax As List(Of Double),
    envMin As List(Of Double))

        Dim n As Integer = estaciones.Count - 1

        If n < 1 Then Exit Sub

        ' momentos en apoyos
        Dim M1n As Double = envMin(0)
        Dim M1p As Double = envMax(0)

        Dim M3n As Double = envMin(n)
        Dim M3p As Double = envMax(n)

        ' máximo positivo del tramo
        Dim M2p As Double = envMax.Max()

        ' ============================
        ' REGLA NSR-10 EN APOYOS
        ' ============================
        M1p = Math.Max(M1p, Math.Abs(M1n) / 3)
        M3p = Math.Max(M3p, Math.Abs(M3n) / 3)

        envMax(0) = M1p
        envMax(n) = M3p

        ' ============================
        ' MOMENTO DE REFERENCIA
        ' ============================
        Dim Mref As Double = Math.Max(
        Math.Abs(M1n),
        Math.Max(Math.Abs(M1p),
        Math.Max(Math.Abs(M3n),
        Math.Max(Math.Abs(M3p), Math.Abs(M2p)))))

        Dim Mlim As Double = Mref / 5

        ' ============================
        ' AJUSTE EN TODO EL TRAMO
        ' ============================
        For i As Integer = 1 To n - 1

            If envMax(i) < Mlim Then
                envMax(i) = Mlim
            End If

            If envMin(i) > -Mlim Then
                envMin(i) = -Mlim
            End If

        Next

    End Sub

    Private Sub DibujarDiagramaCortanteFrames(viga As cViga, pictureBox As PictureBox)

        If viga.Frames Is Nothing OrElse viga.Frames.Count = 0 Then Return

        Dim bmp As New Bitmap(pictureBox.Width, pictureBox.Height)
        Dim g As Graphics = Graphics.FromImage(bmp)
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        g.Clear(Color.White)

        Dim margin As Single = 50

        ' 🔹 Colores por combinación
        Dim colores As Color() = {
        Color.Red, Color.Blue, Color.Green, Color.DarkOrange,
        Color.Purple, Color.Brown, Color.DarkCyan
    }

        ' =====================================================
        ' 🔹 1. OBTENER RANGO GLOBAL (X y V)
        ' =====================================================
        Dim todasX As New List(Of Single)
        Dim todasV As New List(Of Single)

        For Each combo In Proyecto.Elementos.Vigas.Lista_Combinaciones_Design
            Dim xOffset As Single = 0

            For Each frame In viga.Frames.OrderBy(Function(f) f.JointI)

                Dim bfFrame = Proyecto.Elementos.Vigas.BeamForces.
                Where(Function(r) r.LoadCaseCombo = combo AndAlso r.Beam = frame.ObjectLabel AndAlso r.Story = frame.Story).
                ToList()

                If bfFrame.Count = 0 Then Continue For

                Dim L As Single = bfFrame.Max(Function(bf) bf.ElementStation)

                For Each bf In bfFrame
                    todasX.Add(xOffset + bf.ElementStation)
                    todasV.Add(bf.V2)   ' 👈 CORTANTE (cambia a V3 si aplica)
                Next

                xOffset += L
            Next
        Next

        If todasX.Count < 2 Then Return

        Dim minX As Single = todasX.Min()
        Dim maxX As Single = todasX.Max()
        Dim maxAbsV As Single = Math.Max(Math.Abs(todasV.Min()), Math.Abs(todasV.Max()))

        Dim scaleX As Single = (pictureBox.Width - 2 * margin) / (maxX - minX)
        Dim scaleY As Single = (pictureBox.Height / 2 - margin) / maxAbsV
        Dim yZero As Single = pictureBox.Height / 2

        Dim TransformX = Function(x As Single) margin + (x - minX) * scaleX
        Dim TransformY = Function(v As Single) yZero + v * scaleY   ' + abajo / - arriba

        ' =====================================================
        ' 🔹 2. EJE HORIZONTAL V = 0
        ' =====================================================
        Using penZero As New Pen(Color.Black, 1)
            penZero.DashStyle = Drawing2D.DashStyle.Dash
            g.DrawLine(penZero, margin, yZero, pictureBox.Width - margin, yZero)
        End Using

        ' =====================================================
        ' 🔹 3. DIBUJAR COMBINACIONES (FRAME POR FRAME)
        ' =====================================================
        For iComb As Integer = 0 To Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Count - 1

            Dim combo = Proyecto.Elementos.Vigas.Lista_Combinaciones_Design(iComb)
            Dim colorCombo As Color = colores(iComb Mod colores.Length)

            Dim xOffset As Single = 0

            For Each frame In viga.Frames

                Dim bfFrame = Proyecto.Elementos.Vigas.BeamForces.
                Where(Function(r) r.LoadCaseCombo = combo AndAlso r.Beam = frame.ObjectLabel AndAlso r.Story = frame.Story).
                OrderBy(Function(bf) bf.ElementStation).
                ToList()

                If bfFrame.Count = 0 Then Continue For

                Dim L As Single = bfFrame.Max(Function(bf) bf.ElementStation)

                ' 🔹 Línea vertical en apoyo
                If xOffset > 0 Then
                    Using penApoyo As New Pen(Color.Gray, 1)
                        penApoyo.DashStyle = Drawing2D.DashStyle.Dot
                        g.DrawLine(penApoyo,
                               TransformX(xOffset), margin,
                               TransformX(xOffset), pictureBox.Height - margin)
                    End Using
                End If

                ' 🔹 Dibujar cortante SOLO del frame
                Using penV As New Pen(colorCombo, 2)
                    For i As Integer = 0 To bfFrame.Count - 2
                        g.DrawLine(penV,
                        TransformX(xOffset + bfFrame(i).ElementStation),
                        TransformY(bfFrame(i).V2),
                        TransformX(xOffset + bfFrame(i + 1).ElementStation),
                        TransformY(bfFrame(i + 1).V2))
                    Next
                End Using

                ' 🔹 Máximo y mínimo del frame
                Dim bfMax = bfFrame.OrderByDescending(Function(bf) bf.V2).First()
                Dim bfMin = bfFrame.OrderBy(Function(bf) bf.V2).First()

                DibujarEtiquetaCortante(g, TransformX, TransformY, xOffset, bfMax)
                DibujarEtiquetaCortante(g, TransformX, TransformY, xOffset, bfMin)

                xOffset += L
            Next
        Next

        pictureBox.Image = bmp
    End Sub

    Private Sub DibujarEtiquetaCortante(
    g As Graphics,
    TransformX As Func(Of Single, Single),
    TransformY As Func(Of Single, Single),
    xOffset As Single,
    bf As cCombinacionBeamForce)

        Dim x As Single = TransformX(xOffset + bf.ElementStation)
        Dim y As Single = TransformY(bf.V2)   ' 👈 V2 o V3

        g.FillEllipse(Brushes.Black, x - 3, y - 3, 6, 6)

        g.DrawString(Math.Round(bf.V2, 2).ToString(),
                 New System.Drawing.Font("Arial", 8.0F, FontStyle.Bold),
                 Brushes.Black,
                 x + 5,
                 If(bf.V2 < 0, y - 15, y + 2))
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

        ActivarCopiarPegar(Ref_Inferior)
        ActivarCopiarPegar(Ref_Superior)
        CentrarBotonesRefuerzo()

    End Sub

    Private Sub CentrarBotonesRefuerzo()

        Dim espacio As Integer = 15 ' espacio entre botones

        Dim anchoTotal As Integer =
        Boton_Aplicar.Width +
        Boton_Copiar.Width +
        Boton_Replicar.Width +
        espacio * 2

        Dim xInicio As Integer = (TabPage2.ClientSize.Width - anchoTotal) \ 2

        Boton_Aplicar.Left = xInicio

        Boton_Copiar.Left =
        Boton_Aplicar.Right + espacio

        Boton_Replicar.Left =
        Boton_Copiar.Right + espacio

    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        CentrarBotonesRefuerzo()
    End Sub

    Private Sub ConstruirTablaResumen(viga As cViga, dgv As DataGridView)

        dgv.Columns.Clear()
        dgv.Rows.Clear()
        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = True
        dgv.RowHeadersWidth = 160
        'dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect

        dgv.ScrollBars = ScrollBars.Both

        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Regular)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)

        ' =================================================
        ' 🔹 PRIMERA COLUMNA (CONCEPTO)
        ' =================================================
        dgv.Columns.Add("Concepto", "Resultado")

        ' =================================================
        ' 🔹 COLUMNAS POR FRAME (3 POR FRAME)
        ' =================================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")
        Next

        ' =================================================
        ' 🔹 FILAS (RESULTADOS)
        ' =================================================
        dgv.Rows.Add("M− (kN·m)")
        dgv.Rows.Add("M+ (kN·m)")
        dgv.Rows.Add("M- ENV (kN·m)")
        dgv.Rows.Add("M+ ENV (kN·m)")
        dgv.Rows.Add("V (kN)")

        ' Estilo
        dgv.Rows(0).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(1).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
    End Sub

    Private Sub ConstruirTablaRefuerzo(viga As cViga, dgv As DataGridView)

        dgv.Columns.Clear()
        dgv.Rows.Clear()

        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = False
        dgv.RowHeadersWidth = 70
        'dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect
        dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Regular)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)


        ' ============================================
        ' COLUMNAS POR FRAME
        ' ============================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")

        Next

        ' ============================================
        ' FILAS = DIÁMETROS DE BARRAS
        ' ============================================
        Dim barras = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10"}

        For Each barra In barras

            Dim rowIndex As Integer = dgv.Rows.Add()

            ' título de fila
            dgv.Rows(rowIndex).HeaderCell.Value = barra

            ' inicializar en 0
            For i As Integer = 0 To dgv.Columns.Count - 1
                dgv.Rows(rowIndex).Cells(i).Value = 0
            Next

        Next

    End Sub

    Private Sub ConstruirTablaResultadosFlexion(viga As cViga, dgv As DataGridView)

        dgv.Columns.Clear()
        dgv.Rows.Clear()
        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = True
        dgv.RowHeadersWidth = 160
        'dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect

        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Regular)
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        dgv.RowHeadersDefaultCellStyle.Font = New Font("Segoe UI", 11, FontStyle.Bold)

        ' =================================================
        ' 🔹 PRIMERA COLUMNA (CONCEPTO)
        ' =================================================
        dgv.Columns.Add("Concepto", "Resultado")

        ' =================================================
        ' 🔹 COLUMNAS POR FRAME (3 POR FRAME)
        ' =================================================
        For Each frame In viga.Frames

            Dim frameName As String = frame.ObjectLabel

            dgv.Columns.Add($"{frameName}_L", $"{frameName}" & vbCrLf & "Izq")
            dgv.Columns.Add($"{frameName}_C", $"{frameName}" & vbCrLf & "Centro")
            dgv.Columns.Add($"{frameName}_R", $"{frameName}" & vbCrLf & "Der")
        Next

        ' =================================================
        ' 🔹 FILAS (RESULTADOS)
        ' =================================================
        dgv.Rows.Add("Sección")
        dgv.Rows.Add("Longitud (m)")
        dgv.Rows.Add("M- ENV (kN·m)")
        dgv.Rows.Add("M+ ENV (kN·m)")
        dgv.Rows.Add("As req (-)")
        dgv.Rows.Add("As req (+)")
        dgv.Rows.Add("As col (-)")
        dgv.Rows.Add("As col (+)")
        dgv.Rows.Add("F (-)")
        dgv.Rows.Add("F (-)")

        ' Estilo
        dgv.Rows(0).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(1).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(2).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(3).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(4).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(5).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(6).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(7).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(8).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)
        dgv.Rows(9).DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240)

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

        Dim colBase As Integer = 1 ' después de la columna "Resultado"

        For Each frame In viga.Frames

            ' BeamForces del frame para combinaciones de diseño
            Dim bfFrame As List(Of cCombinacionBeamForce) =
                Proyecto.Elementos.Vigas.BeamForces _
                .Where(Function(r) r.Beam = frame.ObjectLabel _
                AndAlso r.Story = frame.Story _
                AndAlso Proyecto.Elementos.Vigas.Lista_Combinaciones_Design.Contains(r.LoadCaseCombo)) _
                .ToList()

            If bfFrame.Count = 0 Then
                colBase += 3
                Continue For
            End If

            ' Longitud del frame
            Dim L As Single = bfFrame.Max(Function(bf) bf.ElementStation)

            ' Estaciones objetivo
            Dim sIzq As Single = 0
            Dim sCen As Single = L / 2
            Dim sDer As Single = L

            ' ==================================================
            ' 🔹 MOMENTO NEGATIVO (M−)
            ' ==================================================
            dgv.Rows(0).Cells(colBase).Value = Math.Abs(Math.Round(ObtenerMomentoNegativo(bfFrame, sIzq), 2))
            dgv.Rows(0).Cells(colBase + 1).Value = Math.Abs(Math.Round(ObtenerMomentoNegativo(bfFrame, sCen), 2))
            dgv.Rows(0).Cells(colBase + 2).Value = Math.Abs(Math.Round(ObtenerMomentoNegativo(bfFrame, sDer), 2))

            ' ==================================================
            ' 🔹 MOMENTO POSITIVO (M+)
            ' ==================================================
            dgv.Rows(1).Cells(colBase).Value = Math.Round(ObtenerMomentoPositivo(bfFrame, sIzq), 2)
            dgv.Rows(1).Cells(colBase + 1).Value = Math.Round(ObtenerMomentoPositivo(bfFrame, sCen), 2)
            dgv.Rows(1).Cells(colBase + 2).Value = Math.Round(ObtenerMomentoPositivo(bfFrame, sDer), 2)

            Dim M1n = ObtenerMomentoNegativo(bfFrame, sIzq)
            Dim M2n = ObtenerMomentoNegativo(bfFrame, sCen)
            Dim M3n = ObtenerMomentoNegativo(bfFrame, sDer)

            Dim M1p = ObtenerMomentoPositivo(bfFrame, sIzq)
            Dim M2p = ObtenerMomentoPositivo(bfFrame, sCen)
            Dim M3p = ObtenerMomentoPositivo(bfFrame, sDer)

            AplicarEnvolventeNSR(M1n, M1p, M2n, M2p, M3n, M3p)

            dgv.Rows(2).Cells(colBase).Value = Math.Abs(Math.Round(M1n, 2))
            dgv.Rows(2).Cells(colBase + 1).Value = Math.Abs(Math.Round(M2n, 2))
            dgv.Rows(2).Cells(colBase + 2).Value = Math.Abs(Math.Round(M3n, 2))

            dgv.Rows(3).Cells(colBase).Value = Math.Round(M1p, 2)
            dgv.Rows(3).Cells(colBase + 1).Value = Math.Round(M2p, 2)
            dgv.Rows(3).Cells(colBase + 2).Value = Math.Round(M3p, 2)

            ' ==================================================
            ' 🔹 CORTANTE (V) → valor absoluto máximo
            ' ==================================================
            dgv.Rows(4).Cells(colBase).Value = Math.Round(ObtenerCortante(bfFrame, sIzq), 2)
            dgv.Rows(4).Cells(colBase + 1).Value = Math.Round(ObtenerCortante(bfFrame, sCen), 2)
            dgv.Rows(4).Cells(colBase + 2).Value = Math.Round(ObtenerCortante(bfFrame, sDer), 2)

            colBase += 3

        Next
    End Sub

    Private Sub LlenarTablaResultados(viga As cViga, dgv As DataGridView)

        Dim colBase As Integer = 1 ' después de la columna "Resultado"

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

                dgv.Rows(2).Cells(colBase).Value = Math.Abs(Math.Round(revision.MomentoNegativo, 2))
                dgv.Rows(3).Cells(colBase).Value = Math.Round(revision.MomentoPositivo, 2)
                dgv.Rows(4).Cells(colBase).Value = Math.Round(revision.AsReqSup, 0)
                dgv.Rows(5).Cells(colBase).Value = Math.Round(revision.AsReqInf, 0)

                colBase += 1

            Next

        Next
    End Sub


    Private Sub designVigas()

        Dim jointsDict As Dictionary(Of String, cJoint) = Proyecto.Elementos.Joints.ToDictionary(Function(j) j.ElementLabel)

        For Each viga In Proyecto.Elementos.Vigas.Vigas

            For Each frame In viga.Frames

                Dim h As Double = frame.Section.h
                Dim b As Double = frame.Section.b
                Dim d As Double = frame.Section.d
                Dim fc As Double = frame.Section.fc
                Dim fy As Double = frame.Section.fy
                Dim phi As Double = 0.9

                Dim factorComun As Double = 0.85 * fc / fy
                Dim denominador As Double = 0.85 * phi * b * (d * d) * (fc * 1000)

                ' Lista de posiciones a evaluar
                Dim posiciones = {
                        PosicionTramoViga.Izquierda,
                        PosicionTramoViga.Centro,
                        PosicionTramoViga.Derecha
                    }

                For Each pos In posiciones

                    ' Buscar o crear revisión
                    Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

                    If revision Is Nothing Then
                        revision = New cRevisionFlexionZona With {
                    .Posicion = pos
                }
                        frame.RevisionFlexion.Add(revision)
                    End If

                    ' Momentos
                    Dim Mu_neg As Double = Math.Abs(revision.MomentoNegativo)
                    Dim Mu_pos As Double = Math.Abs(revision.MomentoPositivo)

                    ' Cálculo rho
                    Dim raiz_neg = Math.Sqrt(1 - (2 * Mu_neg / denominador))
                    Dim raiz_pos = Math.Sqrt(1 - (2 * Mu_pos / denominador))

                    Dim rho_neg As Double = factorComun * (1 - raiz_neg)
                    Dim rho_pos As Double = factorComun * (1 - raiz_pos)

                    ' Acero requerido
                    revision.AsReqSup = rho_neg * b * d * 1000000.0
                    revision.AsReqInf = rho_pos * b * d * 1000000.0

                Next

                frame.Longitud = Distancia(frame, jointsDict)

            Next
        Next

    End Sub


    Private Sub AplicarEnvolventeNSR(
    ByRef M1n As Double,
    ByRef M1p As Double,
    ByRef M2n As Double,
    ByRef M2p As Double,
    ByRef M3n As Double,
    ByRef M3p As Double)

        ' ===============================
        ' REGLA 1 – MOMENTO POSITIVO APOYO
        ' ===============================
        M1p = Math.Max(M1p, Math.Abs(M1n) / 3)
        M3p = Math.Max(M3p, Math.Abs(M3n) / 3)

        ' ===============================
        ' REGLA 2 – MOMENTO MINIMO TRAMO
        ' ===============================
        Dim Mmax As Double =
        Math.Max(Math.Abs(M1n),
        Math.Max(Math.Abs(M1p),
        Math.Max(Math.Abs(M3n), Math.Abs(M3p))))

        Dim Mmin As Double = Mmax / 5

        If Math.Abs(M2n) < Mmin Then
            M2n = -Mmin
        End If

        If Math.Abs(M2p) < Mmin Then
            M2p = Mmin
        End If

    End Sub

    Private Sub Form_09_Vigas_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Proyecto = Form_00_PaginaPrincipal.proyecto
    End Sub

    Private Sub Lista_Vigas_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Lista_Vigas.SelectedIndexChanged

        If Me.DesignMode Then Return
        If Lista_Vigas.SelectedItem Is Nothing Then Exit Sub

        Dim vigaSel As cViga = CType(Lista_Vigas.SelectedItem, cViga)

        Lista_Pisos.SelectedItem = vigaSel.Piso
        Nombre_Viga.Text = vigaSel.Name_Beam
        DibujarPlanta()

        CargarVigaCompleta(vigaSel)

    End Sub

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
        GuardarRefuerzoTabla(viga, Ref_Superior, eTipoRefuerzo.Superior)

        ' Guardar refuerzo inferior
        GuardarRefuerzoTabla(viga, Ref_Inferior, eTipoRefuerzo.Inferior)

        CalcularFlexionViga(viga)

        MostrarResultadosFlexion(viga)


        'Dim dgv As DataGridView = Tabla_Resultados_Flexion
        'Dim colBase As Integer = 1

        'For Each frame In viga.Frames
        '    ' Lista de posiciones a evaluar
        '    Dim posiciones = {
        '            PosicionTramoViga.Izquierda,
        '            PosicionTramoViga.Centro,
        '            PosicionTramoViga.Derecha
        '        }

        '    For Each pos In posiciones

        '        Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

        '        dgv.Rows(6).Cells(colBase).Value = Math.Round(revision.AsProvSup, 0)
        '        dgv.Rows(7).Cells(colBase).Value = Math.Round(revision.AsProvInf, 0)

        '        dgv.Rows(8).Cells(colBase).Value = Math.Round(Math.Min(revision.AsProvSup / revision.AsReqSup, 9.99), 2)
        '        dgv.Rows(9).Cells(colBase).Value = Math.Round(Math.Min(revision.AsProvInf / revision.AsReqInf, 9.99), 2)

        '        Dim cell = dgv.Rows(8).Cells(colBase)
        '        If Math.Min(revision.AsProvSup / revision.AsReqSup, 9.99) >= 0.9 Then
        '            cell.Style.BackColor = ColorTranslator.FromHtml("#C6EFCE")
        '            cell.Style.ForeColor = ColorTranslator.FromHtml("#006100")
        '        Else
        '            cell.Style.BackColor = ColorTranslator.FromHtml("#FFC7CE")
        '            cell.Style.ForeColor = ColorTranslator.FromHtml("#9C0006")
        '        End If

        '        cell = dgv.Rows(9).Cells(colBase)
        '        If Math.Min(revision.AsProvInf / revision.AsReqInf, 9.99) >= 0.9 Then
        '            cell.Style.BackColor = ColorTranslator.FromHtml("#C6EFCE")
        '            cell.Style.ForeColor = ColorTranslator.FromHtml("#006100")
        '        Else
        '            cell.Style.BackColor = ColorTranslator.FromHtml("#FFC7CE")
        '            cell.Style.ForeColor = ColorTranslator.FromHtml("#9C0006")
        '        End If


        '        colBase += 1

        '    Next

        'Next

        MessageBox.Show("Refuerzo guardado correctamente",
                    "Información",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)


    End Sub

    Private Sub GuardarRefuerzoTabla(viga As cViga,
                                 dgv As DataGridView,
                                 tipo As eTipoRefuerzo)

        For col As Integer = 0 To dgv.Columns.Count - 1

            Dim header As String = dgv.Columns(col).HeaderText
            Dim partes = header.Split({vbCrLf}, StringSplitOptions.None)

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
            End Select

            Dim frame = viga.Frames.Find(Function(f) f.ObjectLabel = frameLabel)

            If frame Is Nothing Then Continue For

            Dim tramo As New cRefuerzoTramo
            tramo.Posicion = posicion

            Dim AsTotal As Double = 0

            For row As Integer = 0 To dgv.Rows.Count - 1

                Dim barra As String = dgv.Rows(row).HeaderCell.Value.ToString()
                Dim valor = dgv.Rows(row).Cells(col).Value

                Dim cantidad As Integer = 0

                If valor IsNot Nothing Then
                    Integer.TryParse(valor.ToString(), cantidad)
                End If

                If cantidad > 0 Then
                    tramo.Barras(barra) = cantidad

                    ' 🔹 calcular área de acero
                    Dim areaBarra As Double = AreaRefuerzo(barra)

                    AsTotal += cantidad * areaBarra

                End If

            Next

            ' 🔹 guardar lista de barras
            If tipo = eTipoRefuerzo.Superior Then
                frame.RefuerzoSuperior.Add(tramo)
            Else
                frame.RefuerzoInferior.Add(tramo)
            End If

            '' 🔹 guardar As total en el frame según posición
            'If tipo = eTipoRefuerzo.Superior Then

            '    Select Case posicion
            '        Case PosicionTramoViga.Izquierda
            '            frame.AsSupIzq = AsTotal

            '        Case PosicionTramoViga.Centro
            '            frame.AsSupCen = AsTotal

            '        Case PosicionTramoViga.Derecha
            '            frame.AsSupDer = AsTotal
            '    End Select

            'Else

            '    Select Case posicion
            '        Case PosicionTramoViga.Izquierda
            '            frame.AsInfIzq = AsTotal

            '        Case PosicionTramoViga.Centro
            '            frame.AsInfCen = AsTotal

            '        Case PosicionTramoViga.Derecha
            '            frame.AsInfDer = AsTotal
            '    End Select

            'End If


            ' ================================
            ' Guardar As provisto en revisión
            ' ================================

            Dim revision = frame.RevisionFlexion.Find(Function(r) r.Posicion = posicion)

            If revision Is Nothing Then

                revision = New cRevisionFlexionZona
                revision.Posicion = posicion

                frame.RevisionFlexion.Add(revision)

            End If

            If tipo = eTipoRefuerzo.Superior Then
                revision.AsProvSup = AsTotal
            Else
                revision.AsProvInf = AsTotal
            End If

        Next

    End Sub


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

        If Lista_Vigas.SelectedItem Is Nothing Then Exit Sub

        Dim vigaOrigen As cViga = CType(Lista_Vigas.SelectedItem, cViga)

        Dim form As New Form_Opciones_Combinaciones

        ' 🔹 Llenar vigas
        form.Lista_Combinaciones.Items.Clear()

        For Each viga As cViga In Proyecto.Elementos.Vigas.Vigas
            If viga IsNot vigaOrigen Then ' evitar copiarse a sí misma
                form.Lista_Combinaciones.Items.Add(viga)
            End If
        Next

        form.Lista_Combinaciones.DisplayMember = "Name_Beam"
        form.OpcionLlamado = "ReplicarRefuerzo"

        ' 🔥 pasar viga origen
        form.Tag = vigaOrigen

        form.ShowDialog()



    End Sub


    Public Function SonVigasCompatibles(v1 As cViga, v2 As cViga) As Boolean

        If v1.Frames.Count <> v2.Frames.Count Then Return False

        For i = 0 To v1.Frames.Count - 1

            Dim f1 = v1.Frames(i)
            Dim f2 = v2.Frames(i)

            ' Puedes hacer esto más estricto si quieres
            If Math.Abs(f1.Longitud - f2.Longitud) > 0.01 Then Return False

        Next

        Return True

    End Function


    Public Sub CopiarRefuerzoEntreVigas(origen As cViga, destino As cViga)

        For i = 0 To origen.Frames.Count - 1

            Dim fOrigen = origen.Frames(i)
            Dim fDestino = destino.Frames(i)

            ' Limpiar
            fDestino.RefuerzoSuperior.Clear()
            fDestino.RefuerzoInferior.Clear()

            ' 🔹 Copiar superior
            For Each tramo In fOrigen.RefuerzoSuperior
                fDestino.RefuerzoSuperior.Add(ClonarTramo(tramo))
            Next

            ' 🔹 Copiar inferior
            For Each tramo In fOrigen.RefuerzoInferior
                fDestino.RefuerzoInferior.Add(ClonarTramo(tramo))
            Next

        Next

    End Sub

    Public Function ClonarTramo(origen As cRefuerzoTramo) As cRefuerzoTramo

        Dim nuevo As New cRefuerzoTramo
        nuevo.Posicion = origen.Posicion

        For Each kvp In origen.Barras
            nuevo.Barras(kvp.Key) = kvp.Value
        Next

        Return nuevo

    End Function

    Private Sub CalcularFlexionViga(viga As cViga)

        For Each frame In viga.Frames

            Dim posiciones = {
                PosicionTramoViga.Izquierda,
                PosicionTramoViga.Centro,
                PosicionTramoViga.Derecha
            }

            For Each pos In posiciones

                Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

                If revision Is Nothing Then Continue For

                ' 🔥 Aquí ya tienes AsProv calculado desde GuardarRefuerzoTabla
                ' Solo aseguras que exista comparación
                If revision.AsReqSup > 0 Then
                    revision.RatioSup = Math.Min(revision.AsProvSup / revision.AsReqSup, 9.99)
                End If

                If revision.AsReqInf > 0 Then
                    revision.RatioInf = Math.Min(revision.AsProvInf / revision.AsReqInf, 9.99)
                End If

            Next

        Next

    End Sub

    Private Sub AplicarYCalcularViga(viga As cViga,
                                dgvSup As DataGridView,
                                dgvInf As DataGridView)

        ' Limpiar
        For Each f In viga.Frames
            f.RefuerzoSuperior.Clear()
            f.RefuerzoInferior.Clear()
        Next

        ' Guardar desde tablas
        GuardarRefuerzoTabla(viga, dgvSup, eTipoRefuerzo.Superior)
        GuardarRefuerzoTabla(viga, dgvInf, eTipoRefuerzo.Inferior)

        ' 🔥 Calcular
        CalcularFlexionViga(viga)

    End Sub

    Private Sub MostrarResultadosFlexion(viga As cViga)

        Dim dgv As DataGridView = Tabla_Resultados_Flexion
        Dim colBase As Integer = 1

        For Each frame In viga.Frames

            Dim posiciones = {
                PosicionTramoViga.Izquierda,
                PosicionTramoViga.Centro,
                PosicionTramoViga.Derecha
            }

            For Each pos In posiciones

                Dim revision = frame.RevisionFlexion.FirstOrDefault(Function(r) r.Posicion = pos)

                dgv.Rows(6).Cells(colBase).Value = Math.Round(revision.AsProvSup, 0)
                dgv.Rows(7).Cells(colBase).Value = Math.Round(revision.AsProvInf, 0)

                dgv.Rows(8).Cells(colBase).Value = Math.Round(revision.RatioSup, 2)
                dgv.Rows(9).Cells(colBase).Value = Math.Round(revision.RatioInf, 2)

                ' Colores (igual que ya tienes)
                PintarCelda(dgv.Rows(8).Cells(colBase), revision.RatioSup)
                PintarCelda(dgv.Rows(9).Cells(colBase), revision.RatioInf)

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


End Class