Imports System.Windows.Forms.DataVisualization.Charting

Public Class Form_Graficos

    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Private Sub Form_Graficos_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        Proyecto = Form_00_PaginaPrincipal.proyecto
    End Sub

    ' -----------------------------------------------------------------------
    ' Paleta corporativa ARCO
    ' -----------------------------------------------------------------------
    Private Shared ReadOnly ColAzul As Color = Color.FromArgb(31, 73, 125)
    Private Shared ReadOnly ColAzulClaro As Color = Color.FromArgb(91, 155, 213)
    Private Shared ReadOnly ColVerde As Color = Color.FromArgb(56, 142, 60)
    Private Shared ReadOnly ColRojo As Color = Color.FromArgb(198, 40, 40)
    Private Shared ReadOnly ColNaranja As Color = Color.FromArgb(204, 102, 0)
    Private Shared ReadOnly ColGris As Color = Color.FromArgb(130, 130, 130)
    Private Shared ReadOnly ColLimite As Color = Color.FromArgb(170, 0, 0)

    ' -----------------------------------------------------------------------
    ' Aplica estilo profesional al área del gráfico y agrega título.
    ' `nCols` permite rotar las etiquetas del eje X cuando hay muchas columnas
    ' para que no se encimen y queden ilegibles.
    ' -----------------------------------------------------------------------
    Private Sub EstilizarGrafico(titulo As String, tituloY As String,
                                 Optional tituloX As String = "Columna",
                                 Optional nCols As Integer = 0)

        Dim area = Grafico.ChartAreas("ChartArea1")

        ' Fondo blanco (apto para reporte)
        Grafico.BackColor = Color.White
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

        ' Fuentes
        Dim fntLabel As New Font("Segoe UI", 8.5F, FontStyle.Regular)
        Dim fntAxisTitle As New Font("Segoe UI", 9.5F, FontStyle.Bold)

        area.AxisX.LabelStyle.Font = fntLabel
        area.AxisY.LabelStyle.Font = fntLabel
        area.AxisX.LabelStyle.ForeColor = Color.FromArgb(60, 60, 60)
        area.AxisY.LabelStyle.ForeColor = Color.FromArgb(60, 60, 60)

        ' Con muchas columnas, las etiquetas horizontales se superponen.
        ' Se rotan a vertical y se fuerza a mostrar todas (Interval = 1).
        area.AxisX.LabelStyle.Angle = If(nCols > 12, -90, 0)
        If nCols > 0 Then area.AxisX.LabelStyle.Interval = 1

        area.AxisX.Title = tituloX
        area.AxisX.TitleFont = fntAxisTitle
        area.AxisX.TitleForeColor = Color.FromArgb(40, 40, 40)

        area.AxisY.Title = tituloY
        area.AxisY.TitleFont = fntAxisTitle
        area.AxisY.TitleForeColor = Color.FromArgb(40, 40, 40)
        area.AxisY.TextOrientation = TextOrientation.Rotated270
        area.AxisY.Minimum = 0

        ' Leyenda
        With Grafico.Legends("Legend1")
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Regular)
            .BackColor = Color.Transparent
            .BorderColor = Color.Transparent
            .ForeColor = Color.FromArgb(50, 50, 50)
            .Docking = Docking.Bottom
            .Alignment = StringAlignment.Center
        End With

        ' Título del gráfico
        Grafico.Titles.Clear()
        Dim t As New Title(titulo)
        t.Font = New Font("Segoe UI", 12.5F, FontStyle.Bold)
        t.ForeColor = ColAzul
        t.Docking = Docking.Top
        t.Alignment = ContentAlignment.MiddleCenter
        Grafico.Titles.Add(t)
    End Sub

    ' -----------------------------------------------------------------------
    ' Calcula máximo del eje Y con rango mínimo garantizado.
    ' -----------------------------------------------------------------------
    Private Shared Function YMax(fmax As Single, minRango As Double) As Double
        Return Math.Max(minRango, Math.Ceiling(CDbl(fmax) / 0.1 + 1) * 0.1)
    End Function

    ' -----------------------------------------------------------------------
    ' Columnas con refuerzo definido y cálculo ejecutado (Button2 en
    ' Form_02_00_PagInfoColumnas). Las columnas sin refuerzo se omiten de los
    ' gráficos en vez de mostrarse como una barra en cero (sería engañoso).
    ' -----------------------------------------------------------------------
    Private Function ColumnasCalculadas(cols As cColumnas) As List(Of Columna)
        Return cols.Lista_Columnas.Where(Function(c) c.Ref_Modificado).ToList()
    End Function

    ' -----------------------------------------------------------------------
    ' ALR — Relación de Carga Axial
    ' -----------------------------------------------------------------------
    Private Sub Boton_ALR_Click(sender As Object, e As EventArgs) Handles Boton_ALR.Click

        Grafico.Series.Clear()

        Dim cols = Proyecto.Elementos.Columnas
        If cols Is Nothing OrElse cols.Lista_Columnas.Count = 0 Then
            MessageBox.Show("No hay columnas cargadas." & vbCrLf &
                            "Ejecute el cálculo de columnas (Módulo 02) primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim colsCalc = ColumnasCalculadas(cols)
        If colsCalc.Count = 0 Then
            MessageBox.Show("Ninguna columna tiene refuerzo definido y calculado." & vbCrLf &
                            "Ejecute el cálculo de columnas (Módulo 02) primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim combosGrafico As List(Of String) = If(cols.Lista_Combinaciones_Grafico_ALR,
                                                  New List(Of String))
        Dim paleta() As Color = {ColAzul, ColVerde, ColGris, ColNaranja, ColAzulClaro}
        Dim fmax As Single = 0
        Dim nCols = colsCalc.Count

        If combosGrafico.Count = 0 Then
            ' ── Modo automático: ALR máxima de todas las combinaciones ────────
            Dim serie As New Series
            serie.ChartType = SeriesChartType.Column
            serie.Color = ColAzul
            serie.IsValueShownAsLabel = True
            serie.LabelFormat = "F2"
            serie.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
            serie.LabelForeColor = Color.FromArgb(40, 40, 40)
            serie.LegendText = "ALR Máxima (peor combinación)"

            Dim tieneData As Boolean = False
            For j = 0 To nCols - 1
                Dim col = colsCalc(j)
                Dim alrVal As Single = 0
                Dim comboCritico As String = ""

                ' Prioridad 1: Lista_ALR pre-calculada
                If col.Lista_ALR IsNot Nothing AndAlso col.Lista_ALR.Count > 0 Then
                    Dim peor = col.Lista_ALR.OrderByDescending(Function(a) a.ALR).First()
                    alrVal = peor.ALR
                    comboCritico = peor.Combinacion
                    tieneData = True
                    ' Prioridad 2: calcular en el momento desde fuerzas de los tramos
                ElseIf col.Lista_Tramos_Columnas IsNot Nothing AndAlso col.Lista_Tramos_Columnas.Count > 0 Then
                    For Each tramo In col.Lista_Tramos_Columnas
                        If tramo.Lista_Combinaciones Is Nothing OrElse tramo.Lista_Combinaciones.Count = 0 Then Continue For
                        Dim Ag As Single = If(tramo.EsCircular,
                                             CSng(Math.PI * tramo.Diametro ^ 2 / 4),
                                             tramo.B_Plano * tramo.H_Plano)
                        If Ag <= 0 OrElse tramo.fc <= 0 Then Continue For
                        Dim alrTramo = tramo.Lista_Combinaciones.Max(
                            Function(c) Math.Abs(c.P) / (Ag * tramo.fc * 1000))
                        If alrTramo > alrVal Then alrVal = CSng(alrTramo)
                    Next
                    If alrVal > 0 Then tieneData = True
                    comboCritico = "Estimado desde fuerzas de tramos"
                End If

                Dim pt As New DataPoint
                pt.AxisLabel = col.Name_Label
                pt.XValue = j + 1
                pt.YValues(0) = alrVal
                pt.Color = If(alrVal > 0.3F, ColRojo, ColAzul)
                pt.LabelForeColor = If(alrVal > 0.3F, ColRojo, Color.FromArgb(40, 40, 40))
                If comboCritico <> "" Then pt.ToolTip = "Combinación crítica: " & comboCritico
                If alrVal > fmax Then fmax = alrVal
                serie.Points.Add(pt)
            Next

            If Not tieneData Then
                MessageBox.Show("No se encontraron datos de ALR." & vbCrLf &
                                "Ejecute el cálculo de columnas primero.",
                                "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Grafico.Series.Add(serie)
        Else
            ' ── Modo combinaciones: una serie por combinación seleccionada ────
            For i = 0 To combosGrafico.Count - 1
                Dim serie As New Series
                serie.ChartType = SeriesChartType.Column
                serie.Color = paleta(i Mod paleta.Length)
                serie.IsValueShownAsLabel = True
                serie.LabelFormat = "F2"
                serie.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
                serie.LabelForeColor = Color.FromArgb(40, 40, 40)
                serie.LegendText = combosGrafico(i)

                Dim combNombre = combosGrafico(i)
                For j = 0 To nCols - 1
                    Dim col = colsCalc(j)
                    Dim entrada = If(col.Lista_ALR IsNot Nothing,
                                    col.Lista_ALR.Find(Function(p) p.Combinacion = combNombre),
                                    Nothing)
                    Dim alrVal As Single = If(entrada IsNot Nothing, CSng(entrada.ALR), 0)

                    Dim pt As New DataPoint
                    pt.AxisLabel = col.Name_Label
                    pt.XValue = j + 1
                    pt.YValues(0) = alrVal
                    pt.ToolTip = combNombre
                    If alrVal > 0.3F Then
                        pt.Color = ColRojo
                        pt.LabelForeColor = ColRojo
                    Else
                        pt.Color = paleta(i Mod paleta.Length)
                    End If
                    If alrVal > fmax Then fmax = alrVal
                    serie.Points.Add(pt)
                Next
                Grafico.Series.Add(serie)
            Next
        End If

        ' Línea de límite ALR = 0.30
        Dim sLim As New Series
        sLim.ChartType = SeriesChartType.Line
        sLim.Color = ColLimite
        sLim.BorderWidth = 2
        sLim.BorderDashStyle = ChartDashStyle.Dash
        sLim.IsVisibleInLegend = True
        sLim.LegendText = "Límite ALR = 0.30"
        sLim.Points.AddXY(0, 0.3)
        sLim.Points.AddXY(nCols + 1, 0.3)
        Grafico.Series.Add(sLim)

        Dim area = Grafico.ChartAreas("ChartArea1")
        area.AxisX.Minimum = 0
        area.AxisX.Maximum = nCols + 1
        Dim ym As Double = YMax(fmax, 0.4)
        area.AxisY.Maximum = ym
        area.AxisY.Interval = Math.Round(ym / 8, 2)

        EstilizarGrafico("Relación de Carga Axial (ALR) — Columnas", "ALR = Pu / (Ag · f'c)", "Columna", nCols)
    End Sub

    ' -----------------------------------------------------------------------
    ' Flexo-Compresión — C/D biaxial (Bresler)
    ' -----------------------------------------------------------------------
    Private Sub Boton_Flexo_Click(sender As Object, e As EventArgs) Handles Boton_Flexo.Click

        Grafico.Series.Clear()

        Dim cols = Proyecto.Elementos.Columnas
        If cols Is Nothing OrElse cols.Lista_Columnas.Count = 0 Then Return

        Dim colsCalc = ColumnasCalculadas(cols)
        If colsCalc.Count = 0 Then
            MessageBox.Show("Ninguna columna tiene refuerzo definido y calculado." & vbCrLf &
                            "Ejecute el cálculo de columnas (Módulo 02) primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim sCumple As New Series
        Dim sNoCumple As New Series
        sCumple.ChartType = SeriesChartType.Column
        sNoCumple.ChartType = SeriesChartType.Column
        sCumple.Color = ColVerde
        sNoCumple.Color = ColRojo
        sCumple.IsValueShownAsLabel = True
        sNoCumple.IsValueShownAsLabel = True
        sCumple.LabelFormat = "F2"
        sNoCumple.LabelFormat = "F2"
        sCumple.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        sNoCumple.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        sCumple.LabelForeColor = Color.FromArgb(30, 100, 30)
        sNoCumple.LabelForeColor = ColRojo
        sCumple.LegendText = "C/D ≥ 0.90  (Cumple)"
        sNoCumple.LegendText = "C/D < 0.90  (No cumple)"

        Dim sLim As New Series
        sLim.ChartType = SeriesChartType.Line
        sLim.Color = Color.FromArgb(70, 70, 70)
        sLim.BorderWidth = 2
        sLim.BorderDashStyle = ChartDashStyle.DashDot
        sLim.LegendText = "C/D mínimo = 0.90"

        Dim fmax As Single = 0
        Dim nCols = colsCalc.Count

        For i = 0 To nCols - 1
            Dim col = colsCalc(i)
            If col.Lista_F Is Nothing OrElse col.Lista_F.Count = 0 Then
                sCumple.Points.AddXY(i + 1, 0)
                sNoCumple.Points.AddXY(i + 1, 0)
                Continue For
            End If

            Dim cd As Single = col.Lista_F(0)
            Dim piso As String = If(col.Lista_F_Piso IsNot Nothing AndAlso col.Lista_F_Piso.Count > 0,
                                    col.Lista_F_Piso(0), "—")
            Dim pt As New DataPoint
            pt.AxisLabel = col.Name_Label
            pt.XValue = i + 1
            pt.YValues(0) = cd
            pt.ToolTip = "Piso crítico: " & piso

            If cd >= 0.9F Then
                sCumple.Points.Add(pt)
                sNoCumple.Points.AddXY(i + 1, 0)
            Else
                sCumple.Points.AddXY(i + 1, 0)
                sNoCumple.Points.Add(pt)
            End If

            If cd > fmax Then fmax = cd
        Next

        sLim.Points.AddXY(0, 0.9)
        sLim.Points.AddXY(nCols + 1, 0.9)

        Grafico.Series.Add(sCumple)
        Grafico.Series.Add(sNoCumple)
        Grafico.Series.Add(sLim)

        Dim area = Grafico.ChartAreas("ChartArea1")
        area.AxisX.Minimum = 0
        area.AxisX.Maximum = nCols + 1
        Dim ym As Double = YMax(fmax, 1.2)
        area.AxisY.Maximum = ym
        area.AxisY.Interval = Math.Round(ym / 8, 2)

        EstilizarGrafico("Flexo-Compresión Biaxial (C/D) — Columnas", "C/D  (Criterio Bresler)", "Columna", nCols)
    End Sub

    ' -----------------------------------------------------------------------
    ' Cortante — φVn/Vu sentidos largo y corto
    ' -----------------------------------------------------------------------
    Private Sub Boton_Cortante_Click(sender As Object, e As EventArgs) Handles Boton_Cortante.Click

        Grafico.Series.Clear()

        Dim cols = Proyecto.Elementos.Columnas
        If cols Is Nothing OrElse cols.Lista_Columnas.Count = 0 Then Return

        Dim colsCalc = ColumnasCalculadas(cols)
        If colsCalc.Count = 0 Then
            MessageBox.Show("Ninguna columna tiene refuerzo definido y calculado." & vbCrLf &
                            "Ejecute el cálculo de columnas (Módulo 02) primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim sLargo As New Series
        Dim sCorto As New Series
        sLargo.ChartType = SeriesChartType.Column
        sCorto.ChartType = SeriesChartType.Column
        sLargo.Color = ColAzul
        sCorto.Color = ColAzulClaro
        sLargo.IsValueShownAsLabel = True
        sCorto.IsValueShownAsLabel = True
        sLargo.LabelFormat = "F2"
        sCorto.LabelFormat = "F2"
        sLargo.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        sCorto.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        sLargo.LegendText = "φVn/Vu — Sentido Largo (V2)"
        sCorto.LegendText = "φVn/Vu — Sentido Corto (V3)"

        Dim sLim As New Series
        sLim.ChartType = SeriesChartType.Line
        sLim.Color = Color.FromArgb(70, 70, 70)
        sLim.BorderWidth = 2
        sLim.BorderDashStyle = ChartDashStyle.DashDot
        sLim.LegendText = "C/D mínimo = 0.90"

        Dim fmax As Single = 0
        Dim nCols = colsCalc.Count

        For i = 0 To nCols - 1
            Dim col = colsCalc(i)
            If col.Lista_F Is Nothing OrElse col.Lista_F.Count < 3 Then
                sLargo.Points.AddXY(i + 1, 0)
                sCorto.Points.AddXY(i + 1, 0)
                Continue For
            End If

            Dim fV2 As Single = col.Lista_F(1)
            Dim fV3 As Single = col.Lista_F(2)
            Dim pisoV2 As String = If(col.Lista_F_Piso IsNot Nothing AndAlso col.Lista_F_Piso.Count > 1,
                                      col.Lista_F_Piso(1), "—")
            Dim pisoV3 As String = If(col.Lista_F_Piso IsNot Nothing AndAlso col.Lista_F_Piso.Count > 2,
                                      col.Lista_F_Piso(2), "—")

            Dim ptL As New DataPoint
            ptL.AxisLabel = col.Name_Label
            ptL.XValue = i + 1
            ptL.YValues(0) = fV2
            ptL.ToolTip = "Piso crítico: " & pisoV2
            If fV2 < 0.9F Then
                ptL.Color = ColRojo
                ptL.LabelForeColor = ColRojo
            End If

            Dim ptC As New DataPoint
            ptC.AxisLabel = col.Name_Label
            ptC.XValue = i + 1
            ptC.YValues(0) = fV3
            ptC.ToolTip = "Piso crítico: " & pisoV3
            If fV3 < 0.9F Then
                ptC.Color = ColRojo
                ptC.LabelForeColor = ColRojo
            End If

            sLargo.Points.Add(ptL)
            sCorto.Points.Add(ptC)

            If Math.Max(fV2, fV3) > fmax Then fmax = Math.Max(fV2, fV3)
        Next

        sLim.Points.AddXY(0, 0.9)
        sLim.Points.AddXY(nCols + 1, 0.9)

        Grafico.Series.Add(sLargo)
        Grafico.Series.Add(sCorto)
        Grafico.Series.Add(sLim)

        Dim area = Grafico.ChartAreas("ChartArea1")
        area.AxisX.Minimum = 0
        area.AxisX.Maximum = nCols + 1
        Dim ym As Double = YMax(fmax, 1.2)
        area.AxisY.Maximum = ym
        area.AxisY.Interval = Math.Round(ym / 8, 2)

        EstilizarGrafico("Verificación de Cortante (φVn/Vu) — Columnas", "φVn / Vu", "Columna", nCols)
    End Sub

    ' -----------------------------------------------------------------------
    ' Confinamiento NSR-10 — Ash (estribos) y L0 (longitud zona confinada)
    ' Se calcula el peor caso por columna recorriendo todos sus tramos, igual
    ' que hace Form_02_Reporte_Columnas en su pestaña "Confinamiento NSR-10",
    ' ya que estos factores no se guardan agregados en Columna.Lista_F.
    ' -----------------------------------------------------------------------
    Private Sub Boton_Confinamiento_Click(sender As Object, e As EventArgs) Handles Boton_Confinamiento.Click

        Grafico.Series.Clear()

        Dim cols = Proyecto.Elementos.Columnas
        If cols Is Nothing OrElse cols.Lista_Columnas.Count = 0 Then Return

        Dim colsCalc = ColumnasCalculadas(cols).Where(
            Function(c) c.Lista_Tramos_Columnas IsNot Nothing AndAlso c.Lista_Tramos_Columnas.Count > 0).ToList()
        If colsCalc.Count = 0 Then
            MessageBox.Show("Ninguna columna tiene refuerzo definido y calculado." & vbCrLf &
                            "Ejecute el cálculo de columnas (Módulo 02) primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim sAsh As New Series
        Dim sL0 As New Series
        sAsh.ChartType = SeriesChartType.Column
        sL0.ChartType = SeriesChartType.Column
        sAsh.Color = ColAzul
        sL0.Color = ColNaranja
        sAsh.IsValueShownAsLabel = True
        sL0.IsValueShownAsLabel = True
        sAsh.LabelFormat = "F2"
        sL0.LabelFormat = "F2"
        sAsh.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        sL0.Font = New Font("Segoe UI", 7.5F, FontStyle.Regular)
        sAsh.LegendText = "Ash provisto/requerido (mín. entre sentidos)"
        sL0.LegendText = "L0 provisto/requerido"

        Dim sLim As New Series
        sLim.ChartType = SeriesChartType.Line
        sLim.Color = Color.FromArgb(70, 70, 70)
        sLim.BorderWidth = 2
        sLim.BorderDashStyle = ChartDashStyle.DashDot
        sLim.LegendText = "Mínimo = 0.90"

        Dim fmax As Single = 0
        Dim nCols = colsCalc.Count

        For i = 0 To nCols - 1
            Dim col = colsCalc(i)
            Dim fMinAsh As Single = Single.MaxValue
            Dim fMinL0 As Single = Single.MaxValue
            Dim pisoAsh As String = "—"
            Dim pisoL0 As String = "—"

            For Each tr In col.Lista_Tramos_Columnas
                If tr.F_Ash_Largo > 0 AndAlso tr.F_Ash_Largo < fMinAsh Then fMinAsh = tr.F_Ash_Largo : pisoAsh = tr.Piso
                If tr.F_Ash_Corto > 0 AndAlso tr.F_Ash_Corto < fMinAsh Then fMinAsh = tr.F_Ash_Corto : pisoAsh = tr.Piso

                Dim l0Req As Single = Math.Max(tr.L0_L, tr.L0_C)
                If l0Req > 0 AndAlso tr.L0_Prov > 0 Then
                    Dim fl0 As Single = CSng(Math.Round(tr.L0_Prov / l0Req, 2))
                    If fl0 < fMinL0 Then fMinL0 = fl0 : pisoL0 = tr.Piso
                End If
            Next

            If fMinAsh = Single.MaxValue Then fMinAsh = 0
            If fMinL0 = Single.MaxValue Then fMinL0 = 0

            Dim ptAsh As New DataPoint
            ptAsh.AxisLabel = col.Name_Label
            ptAsh.XValue = i + 1
            ptAsh.YValues(0) = fMinAsh
            ptAsh.ToolTip = "Piso crítico: " & pisoAsh
            If fMinAsh > 0 AndAlso fMinAsh < 0.9F Then
                ptAsh.Color = ColRojo
                ptAsh.LabelForeColor = ColRojo
            End If
            sAsh.Points.Add(ptAsh)

            Dim ptL0 As New DataPoint
            ptL0.AxisLabel = col.Name_Label
            ptL0.XValue = i + 1
            ptL0.YValues(0) = fMinL0
            ptL0.ToolTip = "Piso crítico: " & pisoL0
            If fMinL0 > 0 AndAlso fMinL0 < 0.9F Then
                ptL0.Color = ColRojo
                ptL0.LabelForeColor = ColRojo
            End If
            sL0.Points.Add(ptL0)

            If Math.Max(fMinAsh, fMinL0) > fmax Then fmax = Math.Max(fMinAsh, fMinL0)
        Next

        sLim.Points.AddXY(0, 0.9)
        sLim.Points.AddXY(nCols + 1, 0.9)

        Grafico.Series.Add(sAsh)
        Grafico.Series.Add(sL0)
        Grafico.Series.Add(sLim)

        Dim area = Grafico.ChartAreas("ChartArea1")
        area.AxisX.Minimum = 0
        area.AxisX.Maximum = nCols + 1
        Dim ym As Double = YMax(fmax, 1.2)
        area.AxisY.Maximum = ym
        area.AxisY.Interval = Math.Round(ym / 8, 2)

        EstilizarGrafico("Confinamiento NSR-10 (Ash y L0) — Columnas", "Provisto / Requerido", "Columna", nCols)
    End Sub

    ' -----------------------------------------------------------------------
    ' Exportar gráfico como PNG o JPEG
    ' -----------------------------------------------------------------------
    Private Sub Boton_Exportar_Click(sender As Object, e As EventArgs) Handles Boton_Exportar.Click
        If Grafico.Series.Count = 0 OrElse
           Grafico.Series.All(Function(s) s.Points.Count = 0) Then
            MessageBox.Show("Genere un gráfico antes de exportar.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New SaveFileDialog
            dlg.Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg"
            dlg.DefaultExt = "png"
            dlg.FileName = "Grafico_Columnas_" & DateTime.Now.ToString("yyyyMMdd_HHmm")
            If dlg.ShowDialog() = DialogResult.OK Then
                Dim fmt = If(dlg.FilterIndex = 2,
                             Drawing.Imaging.ImageFormat.Jpeg,
                             Drawing.Imaging.ImageFormat.Png)
                Grafico.SaveImage(dlg.FileName, fmt)
                MessageBox.Show("Imagen guardada correctamente." & vbCrLf & dlg.FileName,
                                "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    ' -----------------------------------------------------------------------
    ' Combinaciones de análisis para gráfico ALR
    ' -----------------------------------------------------------------------
    Private Sub CombinacionesDeAnálisisToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CombinacionesDeAnálisisToolStripMenuItem.Click
        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
        Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()

        Dim alrList = Proyecto.Elementos.Columnas.Lista_Combinaciones_ALR
        Dim grafList = Proyecto.Elementos.Columnas.Lista_Combinaciones_Grafico_ALR
        If alrList IsNot Nothing Then
            For i = 0 To alrList.Count - 1
                Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(alrList(i))
            Next
        End If
        If grafList IsNot Nothing Then
            For i = 0 To grafList.Count - 1
                Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Add(grafList(i))
            Next
        End If

        ' El diálogo es compartido por todos los módulos (Columnas, Muros, Pilas,
        ' Vigas...) y decide qué lista guardar según OpcionLlamado. Debe fijarse
        ' aquí explícitamente y mostrarse modal: de lo contrario, si antes se
        ' abrió el diálogo desde otro módulo, "Guardar" sobrescribiría la lista
        ' equivocada (el diálogo conserva el valor de la última vez que se usó).
        Form_Opciones_Combinaciones.OpcionLlamado = "Columna"
        If Form_Opciones_Combinaciones.ShowDialog() = DialogResult.OK Then
            ' Refrescar el gráfico de ALR con la nueva selección de combinaciones
            Boton_ALR_Click(sender, e)
        End If
    End Sub

End Class
