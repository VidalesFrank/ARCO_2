Imports System.Windows.Forms.DataVisualization.Charting

''' <summary>
''' Infraestructura compartida por los dashboards de gráficas resumen de cada
''' módulo (Columnas, Pilas, Muros, Vigas). Concentra la paleta corporativa, el
''' estilizado del área de gráfico y los constructores de barras con semáforo
''' C/D, para que todos los módulos se vean y se comporten igual.
'''
''' Paleta: gris ARCO (87,87,87) como acento de marca — ver
''' Form_00_PaginaPrincipal.Designer.vb. El semáforo verde/naranja/rojo es un
''' estándar aparte (cumple / no cumple) usado en todo el programa.
''' </summary>
Public NotInheritable Class GraficosResumen

    Private Sub New()
        ' Clase de utilidades: no se instancia.
    End Sub

    ' -----------------------------------------------------------------------
    ' Paleta
    ' -----------------------------------------------------------------------
    Public Shared ReadOnly ColAcento As Color = Color.FromArgb(87, 87, 87)
    Public Shared ReadOnly ColAcentoClaro As Color = Color.FromArgb(224, 224, 224)
    Public Shared ReadOnly ColVerde As Color = Color.FromArgb(56, 142, 60)
    Public Shared ReadOnly ColVerdeTexto As Color = Color.FromArgb(30, 100, 30)
    Public Shared ReadOnly ColRojo As Color = Color.FromArgb(198, 40, 40)
    Public Shared ReadOnly ColNaranja As Color = Color.FromArgb(204, 102, 0)
    Public Shared ReadOnly ColGris As Color = Color.FromArgb(130, 130, 130)
    Public Shared ReadOnly ColNeutro As Color = Color.FromArgb(120, 144, 156)
    Public Shared ReadOnly ColLimite As Color = Color.FromArgb(170, 0, 0)
    Public Shared ReadOnly ColTexto As Color = Color.FromArgb(40, 40, 40)

    ''' <summary>Umbral universal de cumplimiento C/D (ver CLAUDE.md).</summary>
    Public Const UMBRAL_CD As Double = 0.9

    ' -----------------------------------------------------------------------
    ' Ítem genérico de un gráfico de barras por elemento.
    ' Valor < 0 significa "sin cálculo": el ítem se omite del gráfico en vez
    ' de dibujarse como una barra en cero (sería engañoso).
    ' -----------------------------------------------------------------------
    Public Class ItemCD
        Public Property Etiqueta As String
        Public Property Valor As Double
        Public Property Tooltip As String

        Public Sub New()
        End Sub

        Public Sub New(etiqueta As String, valor As Double, Optional tooltip As String = Nothing)
            Me.Etiqueta = etiqueta
            Me.Valor = valor
            Me.Tooltip = tooltip
        End Sub
    End Class

    ' -----------------------------------------------------------------------
    ' Construye un Chart listo para usar en formularios code-only (con el
    ' ChartArea y la Legend que espera EstilizarGrafico).
    ' -----------------------------------------------------------------------
    Public Shared Function CrearChart() As Chart
        Dim ch As New Chart()
        ch.ChartAreas.Add(New ChartArea("ChartArea1"))
        ch.Legends.Add(New Legend("Legend1"))
        ch.Dock = DockStyle.Fill
        ch.BackColor = Color.White
        Return ch
    End Function

    ' -----------------------------------------------------------------------
    ' Aplica estilo profesional al área del gráfico y agrega título.
    ' `nItems` permite rotar las etiquetas del eje X cuando hay muchos
    ' elementos para que no se encimen y queden ilegibles.
    ' -----------------------------------------------------------------------
    Public Shared Sub EstilizarGrafico(chart As Chart,
                                       titulo As String,
                                       tituloY As String,
                                       Optional tituloX As String = "Elemento",
                                       Optional nItems As Integer = 0)

        If chart Is Nothing OrElse chart.ChartAreas.Count = 0 Then Return

        Dim area = chart.ChartAreas(0)

        ' Fondo blanco (apto para reporte)
        chart.BackColor = Color.White
        area.BackColor = Color.White
        area.BorderColor = Color.FromArgb(200, 200, 200)
        area.BorderWidth = 1

        ' Grid: solo horizontal, sutil
        area.AxisY.MajorGrid.LineColor = Color.FromArgb(215, 215, 215)
        area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot
        area.AxisY.MajorGrid.Enabled = True
        area.AxisX.MajorGrid.Enabled = False
        area.AxisX.MajorTickMark.Enabled = False
        area.AxisX.MinorTickMark.Enabled = False

        Dim fntLabel As New Font("Segoe UI", 8.5F, FontStyle.Regular)
        Dim fntAxisTitle As New Font("Segoe UI", 9.5F, FontStyle.Bold)

        area.AxisX.LabelStyle.Font = fntLabel
        area.AxisY.LabelStyle.Font = fntLabel
        area.AxisX.LabelStyle.ForeColor = Color.FromArgb(60, 60, 60)
        area.AxisY.LabelStyle.ForeColor = Color.FromArgb(60, 60, 60)

        ' Con muchos elementos las etiquetas horizontales se superponen.
        area.AxisX.LabelStyle.Angle = If(nItems > 12, -90, 0)
        If nItems > 0 Then area.AxisX.LabelStyle.Interval = 1

        area.AxisX.Title = tituloX
        area.AxisX.TitleFont = fntAxisTitle
        area.AxisX.TitleForeColor = ColTexto

        area.AxisY.Title = tituloY
        area.AxisY.TitleFont = fntAxisTitle
        area.AxisY.TitleForeColor = ColTexto
        area.AxisY.TextOrientation = TextOrientation.Rotated270
        area.AxisY.Minimum = 0

        If chart.Legends.Count > 0 Then
            With chart.Legends(0)
                .Font = New Font("Segoe UI", 8.5F, FontStyle.Regular)
                .BackColor = Color.Transparent
                .BorderColor = Color.Transparent
                .ForeColor = Color.FromArgb(50, 50, 50)
                .Docking = Docking.Bottom
                .Alignment = StringAlignment.Center
            End With
        End If

        chart.Titles.Clear()
        Dim t As New Title(titulo)
        t.Font = New Font("Segoe UI", 12.5F, FontStyle.Bold)
        t.ForeColor = ColAcento
        t.Docking = Docking.Top
        t.Alignment = ContentAlignment.MiddleCenter
        chart.Titles.Add(t)

    End Sub

    ' -----------------------------------------------------------------------
    ' Máximo del eje Y con rango mínimo garantizado.
    ' -----------------------------------------------------------------------
    Public Shared Function YMax(fmax As Double, minRango As Double) As Double
        Return Math.Max(minRango, Math.Ceiling(fmax / 0.1 + 1) * 0.1)
    End Function

    ' -----------------------------------------------------------------------
    ' Barras de C/D con semáforo (verde cumple / rojo no cumple) y línea de
    ' umbral. Criterio: valor >= umbral → cumple (mayor es mejor).
    '
    ' Devuelve False si no hay ningún ítem con cálculo (ya avisó al usuario).
    ' -----------------------------------------------------------------------
    Public Shared Function DibujarBarrasCD(chart As Chart,
                                           items As List(Of ItemCD),
                                           titulo As String,
                                           tituloY As String,
                                           Optional tituloX As String = "Elemento",
                                           Optional umbral As Double = UMBRAL_CD,
                                           Optional minRangoY As Double = 1.2,
                                           Optional tope As Double = 9.99) As Boolean

        chart.Series.Clear()

        Dim datos = If(items Is Nothing,
                       New List(Of ItemCD),
                       items.Where(Function(x) x IsNot Nothing AndAlso x.Valor >= 0).ToList())

        If datos.Count = 0 Then
            MessageBox.Show("No hay elementos con este cálculo ejecutado." & vbCrLf &
                            "Ejecute el cálculo del módulo antes de ver las gráficas.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Dim sCumple = NuevaSerieBarras("C/D ≥ " & umbral.ToString("F2") & "  (Cumple)", ColVerde, ColVerdeTexto)
        Dim sNoCumple = NuevaSerieBarras("C/D < " & umbral.ToString("F2") & "  (No cumple)", ColRojo, ColRojo)

        Dim fmax As Double = 0
        Dim n = datos.Count

        For i = 0 To n - 1
            Dim it = datos(i)
            Dim v As Double = Math.Min(it.Valor, tope)

            Dim pt As New DataPoint()
            pt.AxisLabel = it.Etiqueta
            pt.XValue = i + 1
            pt.YValues(0) = v
            If Not String.IsNullOrEmpty(it.Tooltip) Then pt.ToolTip = it.Tooltip

            ' El truco del cero en la serie contraria mantiene alineadas las
            ' dos series de barras sobre la misma posición del eje X.
            If v >= umbral Then
                sCumple.Points.Add(pt)
                sNoCumple.Points.AddXY(i + 1, 0)
            Else
                sCumple.Points.AddXY(i + 1, 0)
                sNoCumple.Points.Add(pt)
            End If

            If v > fmax Then fmax = v
        Next

        chart.Series.Add(sCumple)
        chart.Series.Add(sNoCumple)
        chart.Series.Add(LineaLimite("C/D mínimo = " & umbral.ToString("F2"),
                                     umbral, n, Color.FromArgb(70, 70, 70), ChartDashStyle.DashDot))

        AjustarEjes(chart, n, fmax, minRangoY)
        EstilizarGrafico(chart, titulo, tituloY, tituloX, n)
        Return True

    End Function

    ' -----------------------------------------------------------------------
    ' Barras contra un límite superior (menor es mejor): ALR, derivas, etc.
    ' Criterio: valor > limite → fuera de límite (rojo).
    ' -----------------------------------------------------------------------
    Public Shared Function DibujarBarrasLimiteSuperior(chart As Chart,
                                                       items As List(Of ItemCD),
                                                       titulo As String,
                                                       tituloY As String,
                                                       limite As Double,
                                                       Optional tituloX As String = "Elemento",
                                                       Optional etiquetaLimite As String = Nothing,
                                                       Optional minRangoY As Double = 0.4,
                                                       Optional colorBarra As Color? = Nothing) As Boolean

        chart.Series.Clear()

        Dim datos = If(items Is Nothing,
                       New List(Of ItemCD),
                       items.Where(Function(x) x IsNot Nothing AndAlso x.Valor >= 0).ToList())

        If datos.Count = 0 Then
            MessageBox.Show("No hay elementos con este cálculo ejecutado." & vbCrLf &
                            "Ejecute el cálculo del módulo antes de ver las gráficas.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Dim colBase As Color = If(colorBarra.HasValue, colorBarra.Value, ColNeutro)
        Dim serie = NuevaSerieBarras("Dentro del límite", colBase, ColTexto)
        Dim serieMal = NuevaSerieBarras("Excede el límite", ColRojo, ColRojo)

        Dim fmax As Double = 0
        Dim n = datos.Count

        For i = 0 To n - 1
            Dim it = datos(i)
            Dim pt As New DataPoint()
            pt.AxisLabel = it.Etiqueta
            pt.XValue = i + 1
            pt.YValues(0) = it.Valor
            If Not String.IsNullOrEmpty(it.Tooltip) Then pt.ToolTip = it.Tooltip

            If it.Valor > limite Then
                serie.Points.AddXY(i + 1, 0)
                serieMal.Points.Add(pt)
            Else
                serie.Points.Add(pt)
                serieMal.Points.AddXY(i + 1, 0)
            End If

            If it.Valor > fmax Then fmax = it.Valor
        Next

        Dim etq As String = If(String.IsNullOrEmpty(etiquetaLimite),
                               "Límite = " & limite.ToString("F2"),
                               etiquetaLimite)

        chart.Series.Add(serie)
        chart.Series.Add(serieMal)
        chart.Series.Add(LineaLimite(etq, limite, n, ColLimite, ChartDashStyle.Dash))

        AjustarEjes(chart, n, Math.Max(fmax, limite), minRangoY)
        EstilizarGrafico(chart, titulo, tituloY, tituloX, n)
        Return True

    End Function

    ' -----------------------------------------------------------------------
    ' Barras de conteo por categoría (no C/D): p.ej. cuántos elementos de
    ' borde requieren confinamiento especial, no especial o ninguno.
    ' -----------------------------------------------------------------------
    Public Shared Function DibujarBarrasCategorias(chart As Chart,
                                                   categorias As List(Of String),
                                                   conteos As List(Of Integer),
                                                   colores As List(Of Color),
                                                   titulo As String,
                                                   tituloY As String,
                                                   Optional tituloX As String = "Categoría") As Boolean

        chart.Series.Clear()

        If categorias Is Nothing OrElse categorias.Count = 0 OrElse conteos Is Nothing Then Return False
        If conteos.Sum() = 0 Then
            MessageBox.Show("No hay elementos con este cálculo ejecutado." & vbCrLf &
                            "Ejecute el cálculo del módulo antes de ver las gráficas.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Dim serie As New Series()
        serie.ChartType = SeriesChartType.Column
        serie.IsValueShownAsLabel = True
        serie.LabelFormat = "F0"
        serie.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        serie.LabelForeColor = ColTexto
        serie.IsVisibleInLegend = False

        Dim maxV As Integer = 0
        For i = 0 To categorias.Count - 1
            Dim c As Integer = If(i < conteos.Count, conteos(i), 0)
            Dim idx = serie.Points.AddXY(i + 1, c)
            serie.Points(idx).AxisLabel = categorias(i)
            If colores IsNot Nothing AndAlso i < colores.Count Then
                serie.Points(idx).Color = colores(i)
            Else
                serie.Points(idx).Color = ColNeutro
            End If
            If c > maxV Then maxV = c
        Next

        chart.Series.Add(serie)

        Dim area = chart.ChartAreas(0)
        area.AxisX.Minimum = 0
        area.AxisX.Maximum = categorias.Count + 1
        area.AxisY.Maximum = Math.Max(1, Math.Ceiling(maxV * 1.2))
        area.AxisY.Interval = Math.Max(1, Math.Ceiling(area.AxisY.Maximum / 8))

        EstilizarGrafico(chart, titulo, tituloY, tituloX, categorias.Count)
        Return True

    End Function

    ' -----------------------------------------------------------------------
    ' Helpers internos
    ' -----------------------------------------------------------------------
    Private Shared Function NuevaSerieBarras(leyenda As String,
                                             colBarra As Color,
                                             colorEtiqueta As Color) As Series
        Dim s As New Series()
        s.ChartType = SeriesChartType.Column
        s.Color = colBarra
        s.IsValueShownAsLabel = True
        s.LabelFormat = "F2"
        s.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        s.LabelForeColor = colorEtiqueta
        s.LegendText = leyenda
        Return s
    End Function

    Private Shared Function LineaLimite(leyenda As String,
                                        valor As Double,
                                        nItems As Integer,
                                        colLinea As Color,
                                        estilo As ChartDashStyle) As Series
        Dim s As New Series()
        s.ChartType = SeriesChartType.Line
        s.Color = colLinea
        s.BorderWidth = 2
        s.BorderDashStyle = estilo
        s.IsVisibleInLegend = True
        s.LegendText = leyenda
        s.Points.AddXY(0, valor)
        s.Points.AddXY(nItems + 1, valor)
        Return s
    End Function

    Private Shared Sub AjustarEjes(chart As Chart,
                                   nItems As Integer,
                                   fmax As Double,
                                   minRangoY As Double)
        Dim area = chart.ChartAreas(0)
        area.AxisX.Minimum = 0
        area.AxisX.Maximum = nItems + 1
        Dim ym As Double = YMax(fmax, minRangoY)
        area.AxisY.Maximum = ym
        area.AxisY.Interval = Math.Round(ym / 8, 2)
    End Sub

    ' -----------------------------------------------------------------------
    ' Exporta el gráfico visible como PNG o JPEG.
    ' -----------------------------------------------------------------------
    Public Shared Sub ExportarImagen(chart As Chart, nombreBase As String)

        If chart Is Nothing OrElse chart.Series.Count = 0 OrElse
           chart.Series.All(Function(s) s.Points.Count = 0) Then
            MessageBox.Show("Genere un gráfico antes de exportar.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New SaveFileDialog()
            dlg.Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg"
            dlg.DefaultExt = "png"
            dlg.FileName = nombreBase & "_" & DateTime.Now.ToString("yyyyMMdd_HHmm")
            If dlg.ShowDialog() = DialogResult.OK Then
                Try
                    Dim fmt = If(dlg.FilterIndex = 2,
                                 Drawing.Imaging.ImageFormat.Jpeg,
                                 Drawing.Imaging.ImageFormat.Png)
                    chart.SaveImage(dlg.FileName, fmt)
                    MessageBox.Show("Imagen guardada correctamente." & vbCrLf & dlg.FileName,
                                    "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    Logger.Error(ex, "GraficosResumen.ExportarImagen", dlg.FileName)
                    MessageBox.Show("No se pudo guardar la imagen." & vbCrLf & ex.Message,
                                    "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            End If
        End Using

    End Sub

    ' -----------------------------------------------------------------------
    ' Panel lateral gris ARCO con los botones de categoría del dashboard.
    ' Devuelve el panel; los botones se agregan con AgregarBotonCategoria.
    ' -----------------------------------------------------------------------
    Public Shared Function CrearPanelLateral() As Panel
        Dim p As New Panel()
        p.Dock = DockStyle.Left
        p.Width = 175
        p.BackColor = ColAcento
        p.Padding = New Padding(0, 12, 0, 12)
        Return p
    End Function

    ''' <summary>
    ''' Agrega un botón de categoría al panel lateral. `indice` es el orden
    ''' vertical (0, 1, 2...); `exportar` lo pinta como botón secundario.
    ''' </summary>
    Public Shared Function AgregarBotonCategoria(panel As Panel,
                                                 texto As String,
                                                 indice As Integer,
                                                 Optional secundario As Boolean = False) As Button
        Dim b As New Button()
        b.Text = texto
        b.Size = New Size(160, 40)
        b.Location = New Point(7, 14 + indice * 46)
        b.FlatStyle = FlatStyle.Flat
        b.FlatAppearance.BorderSize = 1
        b.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        b.Cursor = Cursors.Hand
        b.TextAlign = ContentAlignment.MiddleCenter
        b.UseVisualStyleBackColor = False

        If secundario Then
            b.BackColor = ColAcentoClaro
            b.ForeColor = ColAcento
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)
        Else
            b.BackColor = ColAcento
            b.ForeColor = Color.White
            b.FlatAppearance.BorderColor = Color.FromArgb(150, 150, 150)
        End If

        panel.Controls.Add(b)
        Return b
    End Function

End Class
