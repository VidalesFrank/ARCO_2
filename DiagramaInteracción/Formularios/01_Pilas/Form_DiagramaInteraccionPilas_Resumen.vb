Imports System.Windows.Forms.DataVisualization.Charting

Public Class Form_DiagramaInteraccionPilas_Resumen
    Inherits System.Windows.Forms.Form

    Private Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    ' ─── Controles ─────────────────────────────────────────────────────────────
    Private WithEvents ComboDf As New ComboBox()
    Private WithEvents ChkEtiquetas As New CheckBox()
    Private WithEvents ChkCriticos As New CheckBox()
    Private Chart1 As New Chart()

    ' Paleta de colores para grupos de refuerzo
    Private ReadOnly Paleta As Color() = {
        Color.FromArgb(31, 119, 180),
        Color.FromArgb(214, 39, 40),
        Color.FromArgb(44, 160, 44),
        Color.FromArgb(148, 103, 189),
        Color.FromArgb(255, 127, 14),
        Color.FromArgb(140, 86, 75),
        Color.FromArgb(227, 119, 194),
        Color.FromArgb(127, 127, 127)
    }

    ' ─── Inicialización ────────────────────────────────────────────────────────
    Private Sub Form_DiagramaInteraccionPilas_Resumen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Proyecto = Form_01_PagPilas.Proyecto
        InicializarUI()
        CargarDiametros()
        If ComboDf.Items.Count > 0 Then ComboDf.SelectedIndex = 0
    End Sub

    Private Sub InicializarUI()
        Me.Text = "Diagrama de Interacción — Resumen por Diámetro de Fuste"
        Me.Size = New Size(1000, 720)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.White

        ' ── Panel superior ──────────────────────────────────────────────────
        Dim panTop As New Panel() With {
            .Dock = DockStyle.Top, .Height = 44,
            .BackColor = Color.FromArgb(57, 57, 57), .Padding = New Padding(8, 6, 8, 6)
        }

        Dim lblDf As New Label() With {
            .Text = "Diámetro de fuste (Df):", .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 9.5, FontStyle.Regular),
            .AutoSize = True, .Left = 8, .Top = 11
        }

        ComboDf.Left = lblDf.Left + lblDf.Width + 8
        ComboDf.Top = 8
        ComboDf.Width = 110
        ComboDf.DropDownStyle = ComboBoxStyle.DropDownList
        ComboDf.Font = New Font("Segoe UI", 9.5)

        ChkEtiquetas.Text = "Mostrar nombres"
        ChkEtiquetas.ForeColor = Color.White
        ChkEtiquetas.Font = New Font("Segoe UI", 9.5)
        ChkEtiquetas.Left = ComboDf.Left + ComboDf.Width + 20
        ChkEtiquetas.Top = 10
        ChkEtiquetas.AutoSize = True
        ChkEtiquetas.Checked = False

        ChkCriticos.Text = "Resaltar puntos críticos"
        ChkCriticos.ForeColor = Color.White
        ChkCriticos.Font = New Font("Segoe UI", 9.5)
        ChkCriticos.Left = ChkEtiquetas.Left + ChkEtiquetas.Width + 20
        ChkCriticos.Top = 10
        ChkCriticos.AutoSize = True
        ChkCriticos.Checked = True

        panTop.Controls.AddRange({lblDf, ComboDf, ChkEtiquetas, ChkCriticos})

        ' ── Chart ───────────────────────────────────────────────────────────
        Chart1.Dock = DockStyle.Fill
        Chart1.BackColor = Color.White

        Dim ca As New ChartArea("CA")
        ca.BackColor = Color.White
        ca.AxisX.Title = "φMn  [kN·m]"
        ca.AxisX.TitleFont = New Font("Segoe UI", 9, FontStyle.Bold)
        ca.AxisX.LabelStyle.Font = New Font("Segoe UI", 8)
        ca.AxisX.MajorGrid.LineColor = Color.FromArgb(220, 220, 220)
        ca.AxisX.Minimum = 0
        ca.AxisY.Title = "φPn  [kN]"
        ca.AxisY.TitleFont = New Font("Segoe UI", 9, FontStyle.Bold)
        ca.AxisY.LabelStyle.Font = New Font("Segoe UI", 8)
        ca.AxisY.MajorGrid.LineColor = Color.FromArgb(220, 220, 220)
        Chart1.ChartAreas.Add(ca)

        Dim leg As New Legend("Leyenda")
        leg.Font = New Font("Segoe UI", 8.5)
        leg.Docking = Docking.Bottom
        leg.Alignment = StringAlignment.Center
        Chart1.Legends.Add(leg)

        Me.Controls.Add(Chart1)
        Me.Controls.Add(panTop)
    End Sub

    ' ─── Llenar combo con los Df distintos ─────────────────────────────────────
    Private Sub CargarDiametros()
        ComboDf.Items.Clear()
        Dim diametros = Proyecto.Elementos.Pilas.ListaElementos _
            .Select(Function(p) Math.Round(CDbl(p.Df), 2)) _
            .Distinct() _
            .OrderBy(Function(d) d) _
            .ToList()
        For Each d In diametros
            ComboDf.Items.Add(d.ToString("F2") & " m")
        Next
    End Sub

    ' ─── Redibujar al cambiar selección ────────────────────────────────────────
    Private Sub ComboDf_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboDf.SelectedIndexChanged
        DibujarDiagrama()
    End Sub

    Private Sub ChkEtiquetas_CheckedChanged(sender As Object, e As EventArgs) Handles ChkEtiquetas.CheckedChanged
        DibujarDiagrama()
    End Sub

    Private Sub ChkCriticos_CheckedChanged(sender As Object, e As EventArgs) Handles ChkCriticos.CheckedChanged
        DibujarDiagrama()
    End Sub

    ' ─── Lógica principal del diagrama ─────────────────────────────────────────
    Private Sub DibujarDiagrama()
        If ComboDf.SelectedIndex < 0 Then Return

        Chart1.Series.Clear()

        ' Df seleccionado (parsear "1.00 m")
        Dim dfStr As String = ComboDf.SelectedItem.ToString().Replace(" m", "").Trim()
        Dim dfSel As Double = Double.Parse(dfStr, System.Globalization.CultureInfo.InvariantCulture)

        ' Pilas con este Df
        Dim pilasDf = Proyecto.Elementos.Pilas.ListaElementos _
            .Where(Function(p) Math.Abs(CDbl(p.Df) - dfSel) < 0.005) _
            .ToList()

        If pilasDf.Count = 0 Then Return

        ' Agrupar por configuración de refuerzo + material
        Dim grupos = pilasDf _
            .GroupBy(Function(p) $"{p.N_Barra_Long}|{p.Cant_Barras_Long}|{Math.Round(CDbl(p.Dc), 2)}|{p.fc}") _
            .OrderBy(Function(g) g.Key) _
            .ToList()

        ' Actualizar título del área
        Chart1.ChartAreas("CA").AxisX.Title = "φMn  [kN·m]"
        Chart1.Titles.Clear()
        Dim titulo As New Title($"Diagrama de Interacción Circular — Df = {dfSel:F2} m")
        titulo.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        titulo.ForeColor = Color.FromArgb(57, 57, 57)
        Chart1.Titles.Add(titulo)

        Dim colorIdx As Integer = 0

        For Each grupo In grupos
            Dim primeraPila = grupo.First()
            Dim grupoLabel As String = $"{primeraPila.N_Barra_Long} × {primeraPila.Cant_Barras_Long} — Dc={Math.Round(CDbl(primeraPila.Dc), 2)} m — fc={primeraPila.fc} MPa"
            Dim col As Color = Paleta(colorIdx Mod Paleta.Length)
            colorIdx += 1

            ' ── Serie 1: Envolvente de capacidad (usar primera pila del grupo) ──
            If primeraPila.Matriz_DI_PhiMn IsNot Nothing AndAlso primeraPila.Matriz_DI_PhiMn.Count > 0 Then
                Dim serCap As New Series($"Capacidad — {grupoLabel}") With {
                    .ChartType = SeriesChartType.Line,
                    .Color = col,
                    .BorderWidth = 2,
                    .ChartArea = "CA",
                    .Legend = "Leyenda",
                    .IsVisibleInLegend = True
                }
                For k = 0 To primeraPila.Matriz_DI_PhiMn.Count - 1
                    serCap.Points.AddXY(primeraPila.Matriz_DI_PhiMn(k), primeraPila.Matriz_DI_PhiPn(k))
                Next
                ' Cerrar la curva
                serCap.Points.AddXY(primeraPila.Matriz_DI_PhiMn(0), primeraPila.Matriz_DI_PhiPn(0))
                Chart1.Series.Add(serCap)
            End If

            ' ── Serie 2: Nube de puntos de demanda ──────────────────────────
            Dim serDem As New Series($"Demanda — {grupoLabel}") With {
                .ChartType = SeriesChartType.Point,
                .Color = Color.FromArgb(180, col.R, col.G, col.B),
                .MarkerStyle = MarkerStyle.Circle,
                .MarkerSize = 7,
                .ChartArea = "CA",
                .Legend = "Leyenda",
                .IsVisibleInLegend = False
            }

            ' ── Serie 3 (opcional): Puntos críticos por pila ────────────────
            Dim serCrit As New Series($"Crítico — {grupoLabel}") With {
                .ChartType = SeriesChartType.Point,
                .Color = col,
                .MarkerStyle = MarkerStyle.Diamond,
                .MarkerSize = 10,
                .BorderWidth = 2,
                .ChartArea = "CA",
                .Legend = "Leyenda",
                .IsVisibleInLegend = False
            }

            For Each pila In grupo
                If pila.Matriz_MU Is Nothing OrElse pila.Matriz_PU Is Nothing Then Continue For
                If pila.Matriz_MU.Count = 0 Then Continue For

                ' Punto de mayor excentricidad relativa (|Mu/Pu| máximo con Pu > 0)
                Dim idxCrit As Integer = 0
                Dim maxExc As Double = Double.MinValue

                For k = 0 To pila.Matriz_MU.Count - 1
                    Dim pu As Double = pila.Matriz_PU(k)
                    Dim mu As Double = pila.Matriz_MU(k)
                    Dim exc As Double = If(Math.Abs(pu) > 1, Math.Abs(mu) / Math.Abs(pu), Math.Abs(mu))
                    If exc > maxExc Then
                        maxExc = exc
                        idxCrit = k
                    End If

                    ' Añadir punto a la nube
                    Dim ptIdx As Integer = serDem.Points.AddXY(Math.Abs(mu), pu)
                    If ChkEtiquetas.Checked Then
                        serDem.Points(ptIdx).Label = pila.Name_Elemento
                        serDem.Points(ptIdx).LabelForeColor = col
                        serDem.Points(ptIdx).Font = New Font("Segoe UI", 7)
                    End If
                Next

                ' Añadir punto crítico
                If ChkCriticos.Checked Then
                    Dim ptC As Integer = serCrit.Points.AddXY(
                        Math.Abs(pila.Matriz_MU(idxCrit)), pila.Matriz_PU(idxCrit))
                    serCrit.Points(ptC).Label = pila.Name_Elemento
                    serCrit.Points(ptC).LabelForeColor = col
                    serCrit.Points(ptC).Font = New Font("Segoe UI", 7.5, FontStyle.Bold)
                End If
            Next

            Chart1.Series.Add(serDem)
            If ChkCriticos.Checked Then Chart1.Series.Add(serCrit)
        Next

        ' Ajustar escala automática
        Chart1.ChartAreas("CA").AxisX.Minimum = 0
        Chart1.ChartAreas("CA").RecalculateAxesScale()
    End Sub

End Class
