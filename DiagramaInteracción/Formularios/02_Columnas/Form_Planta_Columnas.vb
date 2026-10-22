Imports System.Drawing
Imports System.Drawing.Drawing2D

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_Planta_Columnas — vista en planta por piso. Cada columna es un
'  rectángulo B_Plano × H_Plano escalado (o círculo con Diametro si la sección
'  es circular), posicionado en Columna.Coordenadas.X,Y.
'
'  Dos modos de color:
'    Estado      cumple C/D ≥ 0.90 (verde) / no cumple (rojo) / sin cálculo (gris).
'    Factor C/D  rampa 3 bandas, igual criterio que los reportes.
'
'  C/D compuesto del tramo = min(flexo-compresión, cortante V2, cortante V3):
'    Flexo   1 / F_Interaccion     (F_Interaccion almacena D/C Bresler; CLAUDE.md)
'    Cort V2 F_Cortante_2          (ya es C/D)
'    Cort V3 F_Cortante_3          (ya es C/D)
'
'  Un valor ≤ 0 significa "no calculado", no "no cumple" — mismo criterio que
'  el resto del módulo. Si TODOS los chequeos del tramo son <= 0, se pinta gris.
'
'  Limitación conocida: no se usa la rotación local de la columna (ETABS la
'  guarda como "Local Axis Angle" pero no está en el modelo). Las columnas se
'  dibujan alineadas al global X/Y. Si en el futuro se parsea del E2K, cambia
'  solo DimOrientadas sin tocar nada más.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Planta_Columnas
    Inherits Form

    Public Property Proyecto As Proyecto
    Public Property GridLines As List(Of cGridLine)
    Public Property Seleccionada As Columna
        Get
            Return TryCast(_canvas.SelectedElement, Columna)
        End Get
        Set(value As Columna)
            _canvas.SelectedElement = value
        End Set
    End Property

    Public Event ColumnaSeleccionada(c As Columna)

    Private _canvas As PlantaCanvas
    Private _cmbPiso As ComboBox
    Private _reporte As GeometriaPlantasService.ReporteFaltantes

    Private Enum ModoColor
        PeorGlobal = 0
        FlexoCompresion = 1
        Cortante = 2
        ALR = 3
    End Enum

    Private Shared ReadOnly ModoLabels As String() = {
        "Chequeo: peor global",
        "Chequeo: flexo-compresión",
        "Chequeo: cortante",
        "Chequeo: ALR (Pu / Ag·fc)"
    }

    ' ── Paleta ────────────────────────────────────────────────
    Private Shared ReadOnly ColorCumple As Color = Color.FromArgb(180, 27, 175, 122)
    Private Shared ReadOnly ColorCumpleHolgado As Color = Color.FromArgb(180, 109, 196, 128)
    Private Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(190, 227, 73, 72)
    Private Shared ReadOnly ColorAmbar As Color = Color.FromArgb(200, 237, 161, 0)
    Private Shared ReadOnly ColorSinCalcular As Color = Color.FromArgb(90, 150, 150, 150)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly ColorBoton As Color = Color.FromArgb(224, 224, 224)

    ' =====================================================================
    Public Sub New()

        Me.Text = "Planta de Columnas"
        Me.Size = New Size(1200, 820)
        Me.MinimumSize = New Size(700, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = GrisARCO
        Me.Font = New Font("Segoe UI", 9)

        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ' ── Canvas ─────────────────────────────────────────────────
        _canvas = New PlantaCanvas With {
            .Dock = DockStyle.Fill,
            .TituloVacio = "Sin columnas para dibujar en este piso",
            .TextoAyuda = "Pasa el cursor sobre una columna para ver sus resultados; haz clic para seleccionarla.",
            .Modos = New List(Of String)(ModoLabels)
        }

        _canvas.OnResolverBounds = AddressOf ResolverBounds
        _canvas.OnDrawElementos = AddressOf DibujarColumnas
        _canvas.OnHitTest = AddressOf HitColumna
        _canvas.OnBuildFicha = AddressOf BuildFicha
        _canvas.OnBuildLeyenda = AddressOf BuildLeyenda

        ' ── Selector de piso en la toolbar inferior del canvas ─────
        ' Mismo patrón que Vigas y Nervios: combo flotando a la derecha,
        ' al lado del combo de modo de chequeo.
        _cmbPiso = New ComboBox With {
            .Dock = DockStyle.Right,
            .Width = 170,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = ColorBoton,
            .ForeColor = GrisARCO,
            .Font = New Font("Segoe UI", 9)
        }
        AddHandler _cmbPiso.SelectedIndexChanged, Sub() _canvas.Refrescar()
        _canvas.ToolBar.Controls.Add(_cmbPiso)
        ' Lo colocamos entre el Zoom (índice 1, extremo derecho) y el separador
        ' del modo (índice 2 originalmente). Así queda al lado del combo "Chequeo".
        _canvas.ToolBar.Controls.SetChildIndex(_cmbPiso, 2)

        AddHandler _canvas.ElementoSeleccionado, AddressOf OnCanvasSeleccion
        AddHandler _canvas.ModoCambio, Sub(idx) _canvas.Refrescar()
        AddHandler Me.Shown,
            Sub()
                ResolverGeometria()
                PoblarComboPiso()
                If _cmbPiso.Items.Count > 0 Then _cmbPiso.SelectedIndex = 0
                _canvas.GridLines = GridLines
                _canvas.AjustarVista()
            End Sub

        Me.Controls.Add(_canvas)
    End Sub

    ' =====================================================================
    ' Resolución y data externa
    ' =====================================================================
    Private Sub ResolverGeometria()
        If Proyecto Is Nothing Then
            _canvas.MensajeVacio = "Primero importá las columnas y calculá verificaciones."
            _canvas.OcultarAviso()
            Return
        End If

        _reporte = GeometriaPlantasService.ResolverYValidar(Proyecto)

        If Not _reporte.PlantaColumnasListaParaDibujar Then
            Dim msg = _reporte.MensajePara("columnas")
            _canvas.MensajeVacio = If(String.IsNullOrEmpty(msg),
                                      "No hay columnas con coordenadas para dibujar.",
                                      msg)
        End If

        ' Si hay columnas sin coords (típico en proyectos cuyo flujo de import
        ' nunca pobló Coordenadas), ofrecer recargar el Excel ETABS.
        If _reporte.ColumnasSinCoord > 0 Then
            Dim n = _reporte.ColumnasSinCoord
            Dim txt = $"⚠ Faltan coordenadas de {n} columna{If(n = 1, "", "s")}. " &
                      "Recargá el Excel ETABS para resolverlas."
            _canvas.MostrarAviso(txt, "Recargar modelo ETABS", AddressOf RecargarModeloETABS)
        Else
            _canvas.OcultarAviso()
        End If
    End Sub

    ' =====================================================================
    ' Recarga no destructiva: lee un Excel ETABS, extrae Joints y Frames y
    ' llama a ResolverColumnas para poblar Coordenadas. No toca refuerzos ni
    ' resultados; solo rellena geometría.
    ' =====================================================================
    Private Sub RecargarModeloETABS()
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing Then Return

        Using ofd As New OpenFileDialog() With {
            .Title = "Seleccionar Excel ETABS del modelo",
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*",
            .Multiselect = False
        }
            If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
            Me.Cursor = Cursors.WaitCursor
            Try
                ' Hojas: E23 primero, fallback a E17.
                Dim dtJoints = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Objects and Elements - Joints")
                If dtJoints Is Nothing Then
                    dtJoints = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Joint Coordinates")
                End If
                Dim dtFrames = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Objects and Elements - Frames")
                If dtFrames Is Nothing Then
                    dtFrames = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Connectivity - Frame")
                End If
                ' Pier Section Properties — necesaria para resolver columnas Pier
                ' (Name_Elemento = pier name, no cruza con Joints/Frames) y para
                ' obtener la columna "AxisAngle".
                Dim dtPierS = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Pier Section Properties")
                ' Frame Assigns - Local Axes — opcional, aporta la columna "Angle"
                ' (ETABS "Local Axis Angle") para orientar la planta de columnas.
                Dim dtFrameLA = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Frame Assigns - Local Axes")
                If dtFrameLA Is Nothing Then
                    dtFrameLA = Funciones_00_Varias.LeerHojaExcel(ofd.FileName, "Frame Assignments - Local Axes")
                End If

                Dim hayJF As Boolean = dtJoints IsNot Nothing AndAlso dtFrames IsNot Nothing
                Dim hayPier As Boolean = dtPierS IsNot Nothing

                If Not hayJF AndAlso Not hayPier Then
                    MessageBox.Show(Me,
                        "El Excel no contiene las hojas necesarias. Para columnas Frame se requieren " &
                        "Objects and Elements - Joints/Frames (o Joint Coordinates / Connectivity - Frame). " &
                        "Para columnas Pier se requiere Pier Section Properties. Exportalas desde ETABS y volvé a intentar.",
                        "Faltan hojas requeridas", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim joints As List(Of cJoint) = Nothing
                Dim frames As List(Of cFrame) = Nothing
                If hayJF Then
                    joints = Funciones_00_Varias.DataTableToJoints(dtJoints)
                    frames = Funciones_00_Varias.DataTableToFrames(dtFrames)
                End If

                ' Pier Section Properties → CoordsPorLabel (cols: Pier(0 o buscar), X(10), Y(11)).
                ' Mismo criterio que el import inicial.
                Dim coordsAgregadas As Integer = 0
                If hayPier Then
                    If Proyecto.Elementos.Columnas.CoordsPorLabel Is Nothing Then
                        Proyecto.Elementos.Columnas.CoordsPorLabel =
                            New Dictionary(Of String, System.Drawing.PointF)(StringComparer.OrdinalIgnoreCase)
                    End If
                    Dim mapa = Proyecto.Elementos.Columnas.CoordsPorLabel

                    ' Pier Section Properties layout E23/E17: col 0 = Story, col 1 = Pier
                    ' name (SectionName). En este caso el nombre del "elemento" en ARCO
                    ' es el Pier name. Las coords vivien en cols 10,11.
                    Const colPierName As Integer = 1
                    Const colCoordX As Integer = 10
                    Const colCoordY As Integer = 11
                    If colCoordY < dtPierS.Columns.Count Then
                        For r As Integer = 1 To dtPierS.Rows.Count - 1
                            Dim nm = Convert.ToString(dtPierS.Rows(r)(colPierName)).Trim()
                            If nm = "" OrElse mapa.ContainsKey(nm) Then Continue For
                            Dim xv As Single, yv As Single
                            If Single.TryParse(Convert.ToString(dtPierS.Rows(r)(colCoordX)),
                                               Globalization.NumberStyles.Float,
                                               Globalization.CultureInfo.InvariantCulture, xv) AndAlso
                               Single.TryParse(Convert.ToString(dtPierS.Rows(r)(colCoordY)),
                                               Globalization.NumberStyles.Float,
                                               Globalization.CultureInfo.InvariantCulture, yv) Then
                                mapa(nm) = New System.Drawing.PointF(xv, yv)
                                coordsAgregadas += 1
                            End If
                        Next
                    End If
                End If

                ' Refrescamos los snapshots del proyecto para que futuras aperturas
                ' de la planta ya puedan resolver sin volver a leer el Excel.
                If joints IsNot Nothing AndAlso joints.Count > 0 Then
                    Proyecto.Elementos.Joints = joints
                    Proyecto.Elementos.Frames = frames
                    Proyecto.Elementos.Columnas.Joints = New List(Of cJoint)(joints)
                    Proyecto.Elementos.Columnas.Frames = New List(Of cFrame)(frames)
                Else
                    ' Sin Joints/Frames nuevos — usar los que ya tiene el proyecto para
                    ' que ResolverColumnas aún pueda resolver las columnas Frame existentes.
                    joints = Proyecto.Elementos.Columnas.Joints
                    If joints Is Nothing OrElse joints.Count = 0 Then joints = Proyecto.Elementos.Joints
                    frames = Proyecto.Elementos.Columnas.Frames
                    If frames Is Nothing OrElse frames.Count = 0 Then frames = Proyecto.Elementos.Frames
                End If

                Dim antes = _reporte.ColumnasSinCoord
                GeometriaPlantasService.ResolverColumnas(Proyecto.Elementos.Columnas, joints, frames)
                Dim angulosAplicados = ColumnaService.AplicarAngulosOrientacion(Proyecto, dtFrameLA, dtPierS)
                ResolverGeometria()
                PoblarComboPiso()
                If _cmbPiso.Items.Count > 0 AndAlso _cmbPiso.SelectedIndex < 0 Then _cmbPiso.SelectedIndex = 0
                _canvas.AjustarVista()
                _canvas.Refrescar()

                Dim resueltas = antes - _reporte.ColumnasSinCoord
                Dim msg As String = $"Se completaron las coordenadas de {resueltas} columna(s)."
                If angulosAplicados > 0 Then
                    msg &= vbCrLf & $"Orientación local aplicada a {angulosAplicados} tramo(s)."
                End If
                MessageBox.Show(Me, msg,
                    "Recarga completada", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Catch ex As Exception
                Logger.Error(ex, "Form_Planta_Columnas.RecargarModeloETABS", ofd.FileName)
                MessageBox.Show(Me,
                    "Error al recargar el modelo:" & vbCrLf & ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Using
    End Sub

    Private Sub PoblarComboPiso()
        _cmbPiso.Items.Clear()
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Columnas Is Nothing OrElse
           Proyecto.Elementos.Columnas.Lista_Columnas Is Nothing Then
            Return
        End If

        Dim pisos = Proyecto.Elementos.Columnas.Lista_Columnas.
                    Where(Function(c) c IsNot Nothing AndAlso c.Lista_Tramos_Columnas IsNot Nothing).
                    SelectMany(Function(c) c.Lista_Tramos_Columnas.Select(Function(t) t.Piso)).
                    Where(Function(p) Not String.IsNullOrWhiteSpace(p)).
                    Distinct().
                    ToList()

        For Each p In pisos
            _cmbPiso.Items.Add(p)
        Next
    End Sub

    Private ReadOnly Property PisoSeleccionado As String
        Get
            If _cmbPiso Is Nothing OrElse _cmbPiso.SelectedItem Is Nothing Then Return ""
            Return _cmbPiso.SelectedItem.ToString()
        End Get
    End Property

    Private Function ColumnasDibujables() As List(Of Columna)
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Columnas Is Nothing OrElse
           Proyecto.Elementos.Columnas.Lista_Columnas Is Nothing Then
            Return New List(Of Columna)
        End If
        Return Proyecto.Elementos.Columnas.Lista_Columnas.Where(
            Function(c) c IsNot Nothing AndAlso
                        c.Coordenadas IsNot Nothing AndAlso
                        Not (c.Coordenadas.X = 0 AndAlso c.Coordenadas.Y = 0)).ToList()
    End Function

    Private Shared Function TramoDelPiso(c As Columna, piso As String) As Tramo_Columna
        If c Is Nothing OrElse c.Lista_Tramos_Columnas Is Nothing Then Return Nothing
        Return c.Lista_Tramos_Columnas.FirstOrDefault(Function(t) t IsNot Nothing AndAlso t.Piso = piso)
    End Function

    ''' <summary>
    ''' Devuelve ancho y alto de la sección en el sistema del dibujo cuando el
    ''' ángulo local es 0, y el ángulo en grados antihorarios desde el global X
    ''' (ETABS "Local Axis Angle").
    '''
    ''' Convención ETABS: la dimensión larga (Depth, t2) está a lo largo del eje
    ''' local 2, que con ángulo 0 coincide con +X global. Por eso, con ángulo 0,
    ''' el lado largo va a lo largo de la X de la planta (columna "horizontal"
    ''' en vista en planta). Columnas con "Angle = 90°" quedan verticales,
    ''' coincidiendo con lo que ETABS dibuja.
    '''
    ''' Para columnas circulares anguloDeg se ignora.
    ''' </summary>
    Private Shared Sub DimOrientadas(t As Tramo_Columna,
                                     ByRef anchoW As Single, ByRef altoW As Single,
                                     ByRef esCircular As Boolean,
                                     Optional ByRef anguloDeg As Single = 0)
        esCircular = t.EsCircular
        If esCircular Then
            anchoW = t.Diametro
            altoW = t.Diametro
            anguloDeg = 0
        Else
            ' H_Plano = max(Depth, Width) → va en X cuando ángulo=0 (dim larga sobre X).
            ' B_Plano = min(Depth, Width) → va en Y cuando ángulo=0.
            anchoW = t.H_Plano
            altoW = t.B_Plano
            anguloDeg = t.AnguloOrientacion
        End If
    End Sub

    ' Devuelve bounding box en coord mundo de un rectángulo centrado en (cx,cy),
    ' con lados (b,h) en ejes locales, rotado anguloDeg antihorario.
    Private Shared Function BoundsRotado(cx As Single, cy As Single,
                                          b As Single, h As Single, anguloDeg As Single) _
                                      As (minX As Single, minY As Single, maxX As Single, maxY As Single)
        If Math.Abs(anguloDeg) < 0.01F Then
            Return (cx - b / 2, cy - h / 2, cx + b / 2, cy + h / 2)
        End If
        Dim rad As Double = anguloDeg * Math.PI / 180.0
        Dim c As Single = CSng(Math.Cos(rad)), s As Single = CSng(Math.Sin(rad))
        Dim hx As Single = b / 2, hy As Single = h / 2
        ' Esquinas en locales → mundo
        Dim corners As (x As Single, y As Single)() = {
            (cx + c * hx - s * hy, cy + s * hx + c * hy),
            (cx + c * hx - s * (-hy), cy + s * hx + c * (-hy)),
            (cx + c * (-hx) - s * (-hy), cy + s * (-hx) + c * (-hy)),
            (cx + c * (-hx) - s * hy, cy + s * (-hx) + c * hy)
        }
        Dim mnX As Single = Single.MaxValue, mnY As Single = Single.MaxValue
        Dim mxX As Single = Single.MinValue, mxY As Single = Single.MinValue
        For Each p In corners
            If p.x < mnX Then mnX = p.x
            If p.x > mxX Then mxX = p.x
            If p.y < mnY Then mnY = p.y
            If p.y > mxY Then mxY = p.y
        Next
        Return (mnX, mnY, mxX, mxY)
    End Function

    ' Hit test de un rectángulo rotado — se trabaja en el sistema local del rect.
    Private Shared Function PuntoDentroRectRotado(wx As Double, wy As Double,
                                                   cx As Single, cy As Single,
                                                   b As Single, h As Single,
                                                   anguloDeg As Single) As Boolean
        Dim dx As Double = wx - cx
        Dim dy As Double = wy - cy
        If Math.Abs(anguloDeg) >= 0.01F Then
            Dim rad As Double = -anguloDeg * Math.PI / 180.0   ' rotación inversa
            Dim cs As Double = Math.Cos(rad), sn As Double = Math.Sin(rad)
            Dim xr As Double = dx * cs - dy * sn
            Dim yr As Double = dx * sn + dy * cs
            dx = xr : dy = yr
        End If
        Return Math.Abs(dx) <= b / 2 AndAlso Math.Abs(dy) <= h / 2
    End Function

    ' =====================================================================
    ' Callbacks al canvas
    ' =====================================================================
    Private ReadOnly Property Modo As ModoColor
        Get
            Return CType(_canvas.ModoActualIndex, ModoColor)
        End Get
    End Property

    Private Function ResolverBounds() As RectangleF
        Dim pisoSel = PisoSeleccionado
        If String.IsNullOrEmpty(pisoSel) Then Return RectangleF.Empty

        Dim minX = Single.MaxValue, minY = Single.MaxValue
        Dim maxX = Single.MinValue, maxY = Single.MinValue
        Dim encontrado As Boolean = False

        For Each c In ColumnasDibujables()
            Dim t = TramoDelPiso(c, pisoSel)
            If t Is Nothing Then Continue For

            Dim anchoW As Single, altoW As Single, esCirc As Boolean, ang As Single
            DimOrientadas(t, anchoW, altoW, esCirc, ang)
            If anchoW <= 0 OrElse altoW <= 0 Then Continue For

            Dim b = BoundsRotado(c.Coordenadas.X, c.Coordenadas.Y, anchoW, altoW, ang)
            If b.minX < minX Then minX = b.minX
            If b.maxX > maxX Then maxX = b.maxX
            If b.minY < minY Then minY = b.minY
            If b.maxY > maxY Then maxY = b.maxY
            encontrado = True
        Next

        If Not encontrado Then Return RectangleF.Empty

        Return New RectangleF(minX, minY,
                              Math.Max(maxX - minX, 0.1F),
                              Math.Max(maxY - minY, 0.1F))
    End Function

    Private Function HitColumna(wx As Double, wy As Double) As Object
        Dim pisoSel = PisoSeleccionado
        If String.IsNullOrEmpty(pisoSel) Then Return Nothing

        For Each c In ColumnasDibujables()
            Dim t = TramoDelPiso(c, pisoSel)
            If t Is Nothing Then Continue For

            Dim anchoW As Single, altoW As Single, esCirc As Boolean, ang As Single
            DimOrientadas(t, anchoW, altoW, esCirc, ang)
            If anchoW <= 0 OrElse altoW <= 0 Then Continue For

            If esCirc Then
                Dim r = anchoW / 2.0
                Dim dx = wx - c.Coordenadas.X
                Dim dy = wy - c.Coordenadas.Y
                If dx * dx + dy * dy <= r * r Then Return c
            Else
                If PuntoDentroRectRotado(wx, wy, c.Coordenadas.X, c.Coordenadas.Y,
                                          anchoW, altoW, ang) Then
                    Return c
                End If
            End If
        Next
        Return Nothing
    End Function

    Private Function BuildFicha(el As Object) As IList(Of String)
        Dim c = TryCast(el, Columna)
        If c Is Nothing Then Return New List(Of String)()

        Dim pisoSel = PisoSeleccionado
        Dim t = TramoDelPiso(c, pisoSel)

        Dim lineas As New List(Of String) From {
            $"{c.Name_Label}   (piso {pisoSel})"
        }

        If t Is Nothing Then
            lineas.Add("Sin tramo en este piso")
            Return lineas
        End If

        If t.EsCircular Then
            lineas.Add($"Circular   D = {t.Diametro:F2} m   fc = {t.fc:F0} MPa")
        Else
            lineas.Add($"Rectangular   B × H = {t.B_Plano:F2} × {t.H_Plano:F2} m   fc = {t.fc:F0} MPa")
        End If
        lineas.Add("")

        ' Breakdown de los chequeos. Se marca con › el chequeo activo. ALR va en
        ' su escala natural (0..0.30+), el resto en C/D.
        Dim cdFlexo = ColumnaService.CDFlexoEnvolvente(t)
        Dim cdFlexoBi = ColumnaService.CDFlexoBiaxial(t)
        Dim cdFlexoAs = ColumnaService.CDFlexoAceros(t)
        Dim cdCort = CDCortante(t)
        Dim alr = ColumnaService.PeorALR(c)
        Dim criterio = ColumnaService.CriterioFlexoGobernante(t)

        Dim lineaFlexo = $"{Marcador(ModoColor.FlexoCompresion)}Flexo-compresión   {FormatearCD(cdFlexo)}"
        If criterio <> "" Then lineaFlexo &= $"   ({criterio})"
        lineas.Add(lineaFlexo)
        ' Desglose solo cuando los dos aportan algo (facilita entender la envolvente).
        If cdFlexoBi > 0 OrElse cdFlexoAs > 0 Then
            lineas.Add($"    · Bresler biaxial  {FormatearCD(cdFlexoBi)}")
            lineas.Add($"    · Rel. aceros     {FormatearCD(cdFlexoAs)}")
        End If
        lineas.Add($"{Marcador(ModoColor.Cortante)}Cortante           {FormatearCD(cdCort)}")
        lineas.Add($"{Marcador(ModoColor.ALR)}ALR                {FormatearALR(alr)}")

        Dim peor = PeorCDGlobalColumna(c, t)
        If peor > 0 Then
            Dim veredicto = If(peor >= Funciones_00_Varias.UMBRAL_CD, "cumple", "no cumple")
            lineas.Add("")
            lineas.Add($"Peor global (sin ALR)  {peor:F2}   ({veredicto})")
        End If

        Return lineas
    End Function

    Private Function Marcador(destino As ModoColor) As String
        Return If(Modo = destino, "› ", "  ")
    End Function

    Private Shared Function FormatearCD(v As Single) As String
        If v <= 0 Then Return "—"
        If Single.IsInfinity(v) OrElse Single.IsNaN(v) Then Return "—"
        Return v.ToString("F2")
    End Function

    Private Shared Function FormatearALR(v As Single) As String
        If v <= 0 Then Return "—"
        If Single.IsInfinity(v) OrElse Single.IsNaN(v) Then Return "—"
        Return v.ToString("F3")
    End Function

    Private Function BuildLeyenda() As IList(Of PlantaLeyendaItem)
        Dim items As New List(Of PlantaLeyendaItem)
        Dim pisoSel = PisoSeleccionado
        Dim lista = ColumnasDibujables()

        If Modo = ModoColor.ALR Then
            Dim nBajo = 0, nMedio = 0, nAlto = 0, nExcede = 0, nSin = 0
            For Each c In lista
                If TramoDelPiso(c, pisoSel) Is Nothing Then Continue For
                Dim alr = ColumnaService.PeorALR(c)
                If alr <= 0 Then
                    nSin += 1
                ElseIf alr < 0.1 Then
                    nBajo += 1
                ElseIf alr < 0.2 Then
                    nMedio += 1
                ElseIf alr <= 0.3 Then
                    nAlto += 1
                Else
                    nExcede += 1
                End If
            Next
            items.Add(New PlantaLeyendaItem(ColorCumple, "ALR < 0.10", nBajo))
            items.Add(New PlantaLeyendaItem(ColorCumpleHolgado, "ALR 0.10 a 0.20", nMedio))
            items.Add(New PlantaLeyendaItem(ColorAmbar, "ALR 0.20 a 0.30", nAlto))
            items.Add(New PlantaLeyendaItem(ColorNoCumple, "ALR > 0.30 (excede NSR-10)", nExcede))
            items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular", nSin))
            Return items
        End If

        ' Modos basados en C/D (Peor global / Flexo / Cortante): gradiente estándar.
        Dim nCumple = 0, nAmbarItems = 0, nNo = 0, nSinC = 0
        For Each c In lista
            Dim t = TramoDelPiso(c, pisoSel)
            If t Is Nothing Then Continue For
            Dim cd = CDModo(c, t)
            If cd <= 0 Then
                nSinC += 1
            ElseIf cd < Funciones_00_Varias.UMBRAL_CD Then
                nNo += 1
            ElseIf cd < 1.15 Then
                nAmbarItems += 1
            Else
                nCumple += 1
            End If
        Next
        items.Add(New PlantaLeyendaItem(ColorCumple, "C/D >= 1.15", nCumple))
        items.Add(New PlantaLeyendaItem(ColorAmbar, $"C/D {Funciones_00_Varias.UMBRAL_CD:F2} a 1.15", nAmbarItems))
        items.Add(New PlantaLeyendaItem(ColorNoCumple, $"C/D < {Funciones_00_Varias.UMBRAL_CD:F2}", nNo))
        items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular", nSinC))
        Return items
    End Function

    ''' <summary>
    ''' C/D del chequeo activo para un tramo. Para ALR devuelve 0 porque ALR no
    ''' es C/D y usa semáforo propio — nunca llamar en modo ALR.
    ''' </summary>
    Private Function CDModo(c As Columna, tr As Tramo_Columna) As Single
        Select Case Modo
            Case ModoColor.FlexoCompresion : Return ColumnaService.CDFlexoEnvolvente(tr)
            Case ModoColor.Cortante        : Return CDCortante(tr)
            Case ModoColor.ALR             : Return 0
            Case Else                      : Return PeorCDGlobalColumna(c, tr)
        End Select
    End Function

    ''' <summary>C/D del peor cortante (min entre V2 y V3). 0 si ninguno aplica.</summary>
    Private Shared Function CDCortante(tr As Tramo_Columna) As Single
        Return ColumnaService.CDCortanteTramo(tr)
    End Function

    Private Shared Function PeorCDGlobalColumna(c As Columna, tr As Tramo_Columna) As Single
        Return ColumnaService.PeorCDGlobalTramo(tr)
    End Function

    Private Sub OnCanvasSeleccion(el As Object)
        Dim c = TryCast(el, Columna)
        If c IsNot Nothing Then RaiseEvent ColumnaSeleccionada(c)
    End Sub

    ' =====================================================================
    ' Dibujo específico de columnas
    ' =====================================================================
    Private Sub DibujarColumnas(g As Graphics, t As PlantaTransform)
        Dim pisoSel = PisoSeleccionado
        If String.IsNullOrEmpty(pisoSel) Then Return

        Dim sel = TryCast(_canvas.SelectedElement, Columna)
        Dim hov = TryCast(_canvas.HoverElement, Columna)

        Using fEtiqueta As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              penBorde As New Pen(Color.FromArgb(70, 70, 70), 1.2F),
              penSelec As New Pen(Color.FromArgb(20, 90, 200), 2.5F),
              penHover As New Pen(Color.FromArgb(255, 140, 0), 2.0F)

            For Each c In ColumnasDibujables()
                Dim tr = TramoDelPiso(c, pisoSel)
                If tr Is Nothing Then Continue For

                Dim anchoW As Single, altoW As Single, esCirc As Boolean, angDeg As Single
                DimOrientadas(tr, anchoW, altoW, esCirc, angDeg)
                If anchoW <= 0 OrElse altoW <= 0 Then Continue For

                ' Screen center + size en pixeles. Dibujamos siempre centrados en
                ' el origen tras translate/rotate — así la rotación local es trivial.
                Dim pc = t.W2S(c.Coordenadas.X, c.Coordenadas.Y)
                Dim wPx As Single = CSng(anchoW * t.Zoom)
                Dim hPx As Single = CSng(altoW * t.Zoom)
                If wPx < 1.5F AndAlso hPx < 1.5F Then
                    wPx = 4 : hPx = 4
                End If
                Dim rect As New RectangleF(-wPx / 2, -hPx / 2, wPx, hPx)

                ' La Y de pantalla crece hacia abajo; para que un giro antihorario
                ' en mundo se vea antihorario en pantalla, se niega el ángulo.
                Dim angScreen As Single = -angDeg

                Dim state = g.Save()
                Try
                    g.TranslateTransform(pc.X, pc.Y)
                    If Not esCirc AndAlso Math.Abs(angScreen) > 0.01F Then
                        g.RotateTransform(angScreen)
                    End If

                    ' Sombra sutil
                    Using bSombra As New SolidBrush(Color.FromArgb(50, 0, 0, 0))
                        Dim rectSombra As New RectangleF(rect.X + 1.5F, rect.Y + 1.5F, rect.Width, rect.Height)
                        If esCirc Then
                            g.FillEllipse(bSombra, rectSombra)
                        Else
                            g.FillRectangle(bSombra, rectSombra)
                        End If
                    End Using

                    Using b As New SolidBrush(ColorDe(c, tr))
                        If esCirc Then
                            g.FillEllipse(b, rect)
                        Else
                            g.FillRectangle(b, rect)
                        End If
                    End Using

                    If esCirc Then
                        g.DrawEllipse(penBorde, rect)
                    Else
                        g.DrawRectangle(penBorde, rect.X, rect.Y, rect.Width, rect.Height)
                    End If

                    If ReferenceEquals(c, sel) Then
                        If esCirc Then
                            g.DrawEllipse(penSelec, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4)
                        Else
                            g.DrawRectangle(penSelec, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4)
                        End If
                    ElseIf ReferenceEquals(c, hov) Then
                        If esCirc Then
                            g.DrawEllipse(penHover, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                        Else
                            g.DrawRectangle(penHover, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                        End If
                    End If

                    ' Label dentro si cabe (centrado en el origen local).
                    If Not String.IsNullOrEmpty(c.Name_Label) Then
                        Dim sz = g.MeasureString(c.Name_Label, fEtiqueta)
                        If sz.Width < rect.Width - 4 AndAlso sz.Height < rect.Height - 2 Then
                            g.DrawString(c.Name_Label, fEtiqueta, Brushes.White,
                                         -sz.Width / 2, -sz.Height / 2)
                        End If
                    End If
                Finally
                    g.Restore(state)
                End Try

                ' Label debajo (sin rotar) si no cabía adentro: trabajamos otra vez
                ' en screen coords absolutas, pero necesitamos el bbox real para
                ' saber dónde está "abajo" del rect rotado.
                If Not String.IsNullOrEmpty(c.Name_Label) AndAlso Not esCirc Then
                    Dim sz = g.MeasureString(c.Name_Label, fEtiqueta)
                    If (sz.Width >= wPx - 4 OrElse sz.Height >= hPx - 2) AndAlso
                       t.Zoom > 10 AndAlso sz.Width < 140 Then
                        Dim bbW = BoundsRotado(c.Coordenadas.X, c.Coordenadas.Y, anchoW, altoW, angDeg)
                        Dim pBtm = t.W2S((bbW.minX + bbW.maxX) / 2, bbW.minY)   ' esquina inf en mundo
                        g.DrawString(c.Name_Label, fEtiqueta, Brushes.Black,
                                     pBtm.X - sz.Width / 2, pBtm.Y + 2)
                    End If
                End If
            Next
        End Using
    End Sub

    Private Function ColorDe(c As Columna, tr As Tramo_Columna) As Color
        If Modo = ModoColor.ALR Then
            Dim alr = ColumnaService.PeorALR(c)
            If alr <= 0 Then Return ColorSinCalcular
            If alr < 0.1 Then Return ColorCumple
            If alr < 0.2 Then Return ColorCumpleHolgado
            If alr <= 0.3 Then Return ColorAmbar
            Return ColorNoCumple
        End If

        Dim cd = CDModo(c, tr)
        If cd <= 0 Then Return ColorSinCalcular
        If cd < Funciones_00_Varias.UMBRAL_CD Then Return ColorNoCumple
        If cd < 1.15 Then Return ColorAmbar
        Return ColorCumple
    End Function

    ''' <summary>
    ''' Devuelve la lista de pisos (en el orden de poblado del combo) que tienen
    ''' tramos de columna — para que el Informe Técnico pueda iterar.
    ''' </summary>
    Public Shared Function PisosConColumnas(proyecto As Proyecto) As List(Of String)
        Dim r As New List(Of String)
        If proyecto Is Nothing OrElse proyecto.Elementos Is Nothing OrElse
           proyecto.Elementos.Columnas Is Nothing OrElse
           proyecto.Elementos.Columnas.Lista_Columnas Is Nothing Then Return r
        Return proyecto.Elementos.Columnas.Lista_Columnas.
                    Where(Function(c) c IsNot Nothing AndAlso c.Lista_Tramos_Columnas IsNot Nothing).
                    SelectMany(Function(c) c.Lista_Tramos_Columnas.Select(Function(t) t.Piso)).
                    Where(Function(p) Not String.IsNullOrWhiteSpace(p)).
                    Distinct().ToList()
    End Function

    ''' <summary>
    ''' Render headless de la planta de columnas en el piso indicado. Para el Informe Técnico:
    ''' crea el form sin mostrarlo, selecciona el piso y usa PlantaCanvas.RenderBitmap.
    ''' modoIndex: 0 = Peor global, 1 = Flexo, 2 = Cortante, 3 = ALR.
    ''' </summary>
    Public Function RenderBitmapPlanta(ancho As Integer, alto As Integer,
                                       piso As String,
                                       Optional modoIndex As Integer = 0) As Bitmap
        ResolverGeometria()
        PoblarComboPiso()
        If _cmbPiso.Items.Count = 0 Then Return Nothing
        Dim idx = _cmbPiso.Items.IndexOf(piso)
        _cmbPiso.SelectedIndex = If(idx >= 0, idx, 0)
        _canvas.GridLines = GridLines
        Return _canvas.RenderBitmap(ancho, alto, modoIndex)
    End Function

End Class
