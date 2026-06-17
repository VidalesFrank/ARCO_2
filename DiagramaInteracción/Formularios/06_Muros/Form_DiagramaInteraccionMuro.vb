Imports System.Drawing
Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores

Public Class Form_DiagramaInteraccionMuro
    Inherits Form

    Public Property Seccion As SeccionMuro
    Public Property NombreMuro As String = ""
    Public Property NombrePiso As String = ""
    Public Property ListaCombinacionesDesign As List(Of String)
    ' Combinaciones con sus fuerzas reales (M3, P) para graficar la demanda
    Public Property Combinaciones As List(Of SeccionMuro.Fuerzas_Elementos)

    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorCapacidad As Color = Color.FromArgb(21, 96, 130)
    Private ReadOnly ColorDemanda As Color = Color.FromArgb(220, 50, 50)
    Private ReadOnly ColorCritico As Color = Color.FromArgb(230, 120, 0)
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#9C0006")

    Private _chart As Chart
    Private _lblInfo As Label
    Private _radioTop As RadioButton
    Private _radioBot As RadioButton
    Private _lblCritica As Label

    Public Sub New()
        Me.Text = "Diagrama de Interacción P-M — Muro"
        Me.Size = New Size(900, 680)
        Me.MinimumSize = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' ── Panel superior ────────────────────────────────────────────────────
        Dim panelTop As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 50,
            .BackColor = ColorEncabezado,
            .Padding = New Padding(10, 8, 10, 8)
        }

        _lblInfo = New Label() With {
            .AutoSize = True,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Location = New Point(10, 12)
        }
        panelTop.Controls.Add(_lblInfo)

        _radioTop = New RadioButton() With {
            .Text = "Top",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Checked = True,
            .Location = New Point(500, 14),
            .AutoSize = True
        }
        _radioBot = New RadioButton() With {
            .Text = "Bot",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Location = New Point(560, 14),
            .AutoSize = True
        }
        AddHandler _radioTop.CheckedChanged, AddressOf Radio_Changed
        AddHandler _radioBot.CheckedChanged, AddressOf Radio_Changed
        panelTop.Controls.Add(_radioTop)
        panelTop.Controls.Add(_radioBot)
        Me.Controls.Add(panelTop)

        ' ── Panel inferior con etiqueta de combinación crítica ─────────────────
        Dim panelBot As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 36,
            .BackColor = Color.FromArgb(245, 245, 245)
        }
        _lblCritica = New Label() With {
            .AutoSize = False,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 10),
            .Padding = New Padding(10, 0, 0, 0)
        }
        panelBot.Controls.Add(_lblCritica)
        Me.Controls.Add(panelBot)

        ' ── Chart ─────────────────────────────────────────────────────────────
        _chart = New Chart() With {.Dock = DockStyle.Fill}
        _chart.ChartAreas.Add(New ChartArea("Principal"))

        With _chart.ChartAreas("Principal")
            .BackColor = Color.White
            .AxisX.Title = "Momento Mn  (kN·m)"
            .AxisX.TitleFont = New Font("Segoe UI", 10, FontStyle.Bold)
            .AxisX.LabelStyle.Font = New Font("Segoe UI", 9)
            .AxisX.MajorGrid.LineColor = Color.FromArgb(220, 220, 220)
            .AxisX.Minimum = 0
            .AxisY.Title = "Carga axial Pn  (kN)"
            .AxisY.TitleFont = New Font("Segoe UI", 10, FontStyle.Bold)
            .AxisY.LabelStyle.Font = New Font("Segoe UI", 9)
            .AxisY.MajorGrid.LineColor = Color.FromArgb(220, 220, 220)
            .InnerPlotPosition = New ElementPosition(8, 5, 87, 88)
        End With

        _chart.Legends.Add(New Legend("Leyenda") With {
            .Font = New Font("Segoe UI", 9),
            .BackColor = Color.FromArgb(245, 245, 245),
            .BorderColor = Color.FromArgb(200, 200, 200)
        })

        Me.Controls.Add(_chart)

        AddHandler Me.Load, AddressOf Form_Load

    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)
        _lblInfo.Text = $"Muro: {NombreMuro}   |   Piso: {NombrePiso}"
        DibujarDiagrama()
    End Sub

    Private Sub Radio_Changed(sender As Object, e As EventArgs)
        DibujarDiagrama()
    End Sub

    Private Sub DibujarDiagrama()

        If Seccion Is Nothing Then Return

        _chart.Series.Clear()

        Dim esTop = _radioTop.Checked
        Dim listaMn = If(esTop, Seccion.Lista_Mn_Top, Seccion.Lista_Mn_Bot)
        Dim listaPn = If(esTop, Seccion.Lista_Pn_Top, Seccion.Lista_Pn_Bot)
        Dim listaM = If(esTop, Seccion.Lista_M_Top, Seccion.Lista_M_Bot)
        Dim listaP = If(esTop, Seccion.Lista_P_Top, Seccion.Lista_P_Bot)
        Dim factorDI = If(esTop, Seccion.Factor_Demanda_Flexo_Top, Seccion.Factor_Demanda_Flexo_Bot)
        Dim combCritica = If(esTop, Seccion.Combinacion_Demanda_Flexo_Top, Seccion.Combinacion_Demanda_Flexo_Bot)

        If listaMn Is Nothing OrElse listaMn.Count = 0 Then
            _lblCritica.Text = "Sin diagrama calculado. Aplique el refuerzo en la pestaña de Información."
            _lblCritica.ForeColor = Color.FromArgb(150, 0, 0)
            Return
        End If

        ' ── Serie 1: Envolvente de capacidad ──────────────────────────────────
        Dim sCapacidad As New Series("Envolvente de Capacidad") With {
            .ChartType = SeriesChartType.Line,
            .Color = ColorCapacidad,
            .BorderWidth = 3,
            .BorderDashStyle = ChartDashStyle.Solid,
            .LegendText = "Envolvente φMn-φPn"
        }
        sCapacidad.MarkerStyle = MarkerStyle.None

        For k = 0 To listaMn.Count - 1
            sCapacidad.Points.AddXY(listaMn(k), listaPn(k))
        Next
        _chart.Series.Add(sCapacidad)

        ' ── Serie 2: Puntos de demanda (M3, P de cada combinación de diseño) ──
        Dim sDemanda As New Series("Demanda") With {
            .ChartType = SeriesChartType.Point,
            .Color = ColorDemanda,
            .MarkerStyle = MarkerStyle.Circle,
            .MarkerSize = 8,
            .LegendText = "Demanda por combinación"
        }

        Dim sCritico As New Series("Crítico") With {
            .ChartType = SeriesChartType.Point,
            .Color = ColorCritico,
            .MarkerStyle = MarkerStyle.Diamond,
            .MarkerSize = 14,
            .LegendText = $"Caso crítico (F={Math.Round(factorDI, 2)})"
        }

        Dim combsDesign As New HashSet(Of String)(If(ListaCombinacionesDesign, New List(Of String)()))

        ' Filtrar combinaciones de diseño y graficar sus pares (|M3|, |P|)
        Dim combsFiltradas = If(Combinaciones IsNot Nothing,
                                Combinaciones.Where(Function(c) combsDesign.Count = 0 OrElse combsDesign.Contains(c.Name)).ToList(),
                                New List(Of SeccionMuro.Fuerzas_Elementos)())

        Dim maxDist As Double = Double.MinValue
        Dim mCrit As Single = 0
        Dim pCrit As Single = 0

        For Each comb In combsFiltradas
            Dim m = Math.Abs(comb.M3)
            Dim p = Math.Abs(comb.P)
            sDemanda.Points.AddXY(m, p)
            ' Tooltip con nombre de combinación
            sDemanda.Points(sDemanda.Points.Count - 1).ToolTip = comb.Name

            Dim dist = Math.Sqrt(m ^ 2 + p ^ 2)
            If dist > maxDist Then
                maxDist = dist
                mCrit = m
                pCrit = p
            End If
        Next

        sCritico.Points.AddXY(mCrit, pCrit)

        _chart.Series.Add(sDemanda)
        _chart.Series.Add(sCritico)

        ' ── Ajustar escala ────────────────────────────────────────────────────
        Dim xMax = If(listaMn.Count > 0, listaMn.Max() * 1.15, 100)
        Dim yMin = If(listaPn.Count > 0, listaPn.Min() * 1.1, -1000)
        Dim yMax = If(listaPn.Count > 0, listaPn.Max() * 1.1, 10000)

        With _chart.ChartAreas("Principal")
            .AxisX.Maximum = xMax
            .AxisX.Minimum = 0
            .AxisY.Minimum = yMin
            .AxisY.Maximum = yMax
        End With

        ' ── Etiqueta estado ───────────────────────────────────────────────────
        Dim cumple = factorDI >= 0.9
        _lblCritica.Text = $"  Combinación crítica: {combCritica}   |   Factor de capacidad: {Math.Round(factorDI, 2)}   |   " &
                           If(cumple, "✓ Cumple", "✗ No cumple")
        _lblCritica.ForeColor = If(cumple, ColorOK, ColorMal)
        _lblCritica.Font = New Font("Segoe UI", 10, FontStyle.Bold)

    End Sub

End Class
