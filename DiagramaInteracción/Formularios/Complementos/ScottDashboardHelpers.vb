Imports ScottPlotNS = ScottPlot

''' <summary>
''' Ítem genérico de un dashboard C/D (una barra). Valor &lt;= 0 significa
''' "sin cálculo": el ítem se omite en vez de dibujarse como barra en cero.
''' </summary>
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

''' <summary>Conteo por categoría para gráficas de barras discretas.</summary>
Public Class CategoriaConteo
    Public Property Etiqueta As String
    Public Property Conteo As Integer
    Public Property Color As Color
    Public Sub New(e As String, c As Integer, col As Color)
        Etiqueta = e : Conteo = c : Color = col
    End Sub
End Class

''' <summary>
''' Composición común de los dashboards premium ScottPlot. Centraliza paletas,
''' tipografías y layout — todos los módulos consumen de aquí para asegurar
''' un look idéntico.
''' </summary>
Public NotInheritable Class ScottDashboardHelpers

    Private Sub New()
    End Sub

    ' ─── Encabezado premium con toggle de tema ─────────────────────────────
    Public Shared Function CrearEncabezado(titulo As String, subtitulo As String,
                                            tema As DashboardTheme,
                                            Optional onToggle As Action = Nothing) As Panel
        Dim p As New Panel() With {.Dock = DockStyle.Top, .Height = 64, .BackColor = tema.Panel}
        Dim barra As New Panel() With {.Dock = DockStyle.Bottom, .Height = 2, .BackColor = tema.Acento}

        Dim lblTit As New Label() With {
            .Text = titulo,
            .ForeColor = tema.Texto,
            .Font = New Font("Segoe UI Semibold", 12.0F),
            .AutoSize = True,
            .Location = New Point(22, 12),
            .BackColor = Color.Transparent}

        Dim lblSub As New Label() With {
            .Text = subtitulo,
            .ForeColor = tema.TextoTenue,
            .Font = New Font("Segoe UI", 9.0F),
            .AutoSize = True,
            .Location = New Point(22, 36),
            .BackColor = Color.Transparent}

        Dim btnTema As New Button() With {
            .Text = If(tema.Modo = DashboardTheme.TemaModo.Dark, "☀  Light", "🌙  Dark"),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .ForeColor = tema.Texto,
            .BackColor = tema.PanelAlt,
            .Size = New Size(100, 32),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .Cursor = Cursors.Hand,
            .TabStop = False}
        btnTema.FlatAppearance.BorderColor = tema.Separador
        btnTema.FlatAppearance.BorderSize = 1
        btnTema.Location = New Point(p.ClientSize.Width - btnTema.Width - 18, 16)
        AddHandler p.Resize, Sub(s, ev) btnTema.Location = New Point(p.ClientSize.Width - btnTema.Width - 18, 16)
        AddHandler btnTema.Click, Sub()
                                      DashboardTheme.Alternar()
                                      If onToggle IsNot Nothing Then onToggle()
                                  End Sub

        p.Controls.Add(btnTema)
        p.Controls.Add(lblSub)
        p.Controls.Add(lblTit)
        p.Controls.Add(barra)
        Return p
    End Function

    ' ─── Tarjeta KPI ──────────────────────────────────────────────────────
    Public Shared Function TarjetaKpi(titulo As String, valor As String,
                                       color As Color, hint As String,
                                       tema As DashboardTheme) As Panel
        Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = tema.Panel,
                                   .Margin = New Padding(6), .Padding = New Padding(16)}
        Dim barra As New Panel() With {.Dock = DockStyle.Left, .Width = 4, .BackColor = color}

        Dim lblTit As New Label() With {
            .Text = titulo,
            .Font = New Font("Segoe UI Semibold", 8.5F),
            .ForeColor = tema.TextoTenue,
            .Dock = DockStyle.Top, .Height = 20,
            .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent}

        Dim lblVal As New Label() With {
            .Text = valor,
            .Font = New Font("Segoe UI Semibold", 28.0F),
            .ForeColor = color,
            .Dock = DockStyle.Top, .Height = 48,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(0, 2, 0, 0),
            .BackColor = Color.Transparent}

        Dim lblHint As New Label() With {
            .Text = hint,
            .Font = New Font("Segoe UI", 8.5F),
            .ForeColor = tema.TextoTenue,
            .Dock = DockStyle.Top, .Height = 18,
            .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent}

        p.Controls.Add(lblHint)
        p.Controls.Add(lblVal)
        p.Controls.Add(lblTit)
        p.Controls.Add(barra)
        Return p
    End Function

    ' ─── Panel contenedor con título ───────────────────────────────────────
    Public Shared Function PanelConTitulo(titulo As String, control As Control,
                                           tema As DashboardTheme) As Panel
        Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = tema.Panel,
                                   .Margin = New Padding(6), .Padding = New Padding(14)}
        Dim lbl As New Label() With {
            .Text = titulo,
            .Font = New Font("Segoe UI Semibold", 10.0F),
            .ForeColor = tema.Texto,
            .Dock = DockStyle.Top, .Height = 26,
            .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent}
        control.Dock = DockStyle.Fill
        p.Controls.Add(control)
        p.Controls.Add(lbl)
        Return p
    End Function

    ' ─── Mensaje de "sin datos" ────────────────────────────────────────────
    Public Shared Function Vacio(mensaje As String, tema As DashboardTheme) As Label
        Return New Label() With {
            .Dock = DockStyle.Fill,
            .Text = mensaje,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = tema.TextoTenue,
            .Font = New Font("Segoe UI", 12.0F),
            .BackColor = Color.Transparent}
    End Function

    ' ─── Estilizado base del Plot ──────────────────────────────────────────
    Public Shared Sub EstilizarPlot(fp As ScottPlotNS.FormsPlot, tema As DashboardTheme)
        fp.BackColor = tema.Panel
        fp.Plot.Style(figureBackground:=tema.Panel,
                      dataBackground:=tema.Panel,
                      grid:=tema.Grilla,
                      tick:=tema.TextoTenue,
                      axisLabel:=tema.TextoTenue,
                      titleLabel:=tema.Texto)
        fp.Plot.Grid(enable:=True, color:=tema.Grilla, lineStyle:=ScottPlotNS.LineStyle.Dot)
        fp.Plot.XAxis.Color(tema.TextoTenue)
        fp.Plot.YAxis.Color(tema.TextoTenue)
        fp.Plot.XAxis2.Color(tema.TextoTenue)
        fp.Plot.YAxis2.Color(tema.TextoTenue)
    End Sub

    ' ─── Barras C/D verticales con semáforo + línea de umbral ──────────────
    Public Shared Function BarrasCDVertical(items As List(Of ItemCD),
                                             tema As DashboardTheme,
                                             Optional tituloY As String = "C/D",
                                             Optional etiquetasRotadas As Boolean = True) As ScottPlotNS.FormsPlot
        Dim fp As New ScottPlotNS.FormsPlot()
        EstilizarPlot(fp, tema)

        Dim validos = items.Where(Function(i) i IsNot Nothing AndAlso i.Valor > 0).ToList()
        If validos.Count = 0 Then
            fp.Plot.Title("Sin elementos para mostrar", color:=tema.TextoTenue, size:=11)
            fp.Refresh()
            Return fp
        End If

        Dim ordenados = validos.OrderBy(Function(i) i.Valor).ToList()
        Dim n = ordenados.Count
        Dim xs = Enumerable.Range(0, n).Select(Function(i) CDbl(i)).ToArray()
        Dim ys = ordenados.Select(Function(i) i.Valor).ToArray()
        Dim colores = ordenados.Select(Function(i) tema.ColorPorCD(i.Valor)).ToArray()
        Dim etiq = ordenados.Select(Function(i) i.Etiqueta).ToArray()

        For i = 0 To n - 1
            Dim bar = fp.Plot.AddBar({ys(i)}, {xs(i)}, colores(i))
            bar.BarWidth = 0.72
            bar.BorderColor = Color.Transparent
            bar.ShowValuesAboveBars = True
            bar.ValueFormatter = Function(v) v.ToString("F2")
            bar.Font.Color = tema.Texto
            bar.Font.Size = 13.5F
            bar.Font.Name = "Segoe UI"
        Next

        Dim hl = fp.Plot.AddHorizontalLine(DashboardTheme.UMBRAL_CD, tema.Rojo, 2, ScottPlotNS.LineStyle.Dash,
                                           "Umbral " & DashboardTheme.UMBRAL_CD.ToString("F2"))
        hl.PositionLabel = True
        hl.PositionLabelBackground = tema.Rojo
        hl.PositionLabelFont.Color = Color.White

        fp.Plot.XTicks(xs, etiq)
        fp.Plot.XAxis.TickLabelStyle(rotation:=If(etiquetasRotadas, 45, 0), fontSize:=14.0F)
        fp.Plot.YAxis.Label(tituloY, color:=tema.TextoTenue, size:=16)
        fp.Plot.SetAxisLimitsY(0, Math.Max(1.5, ys.Max() * 1.15))
        fp.Plot.Legend(enable:=False)
        fp.Refresh()
        Return fp
    End Function

    ' ─── Barras "menor es mejor" (ALR y similares, con límite superior) ───
    Public Shared Function BarrasLimiteSuperior(items As List(Of ItemCD),
                                                 limite As Double,
                                                 tema As DashboardTheme,
                                                 Optional tituloY As String = "ALR") As ScottPlotNS.FormsPlot
        Dim fp As New ScottPlotNS.FormsPlot()
        EstilizarPlot(fp, tema)

        Dim validos = items.Where(Function(i) i IsNot Nothing AndAlso i.Valor >= 0).OrderByDescending(Function(i) i.Valor).ToList()
        If validos.Count = 0 Then
            fp.Plot.Title("Sin elementos", color:=tema.TextoTenue, size:=11)
            fp.Refresh()
            Return fp
        End If

        Dim n = validos.Count
        Dim xs = Enumerable.Range(0, n).Select(Function(i) CDbl(i)).ToArray()
        Dim etiq = validos.Select(Function(i) i.Etiqueta).ToArray()
        For i = 0 To n - 1
            Dim v = validos(i).Valor
            Dim col = If(v <= limite, tema.Verde, If(v <= limite * 1.1, tema.Ambar, tema.Rojo))
            Dim bar = fp.Plot.AddBar({v}, {CDbl(i)}, col)
            bar.BarWidth = 0.72
            bar.BorderColor = Color.Transparent
            bar.ShowValuesAboveBars = True
            bar.ValueFormatter = Function(x) x.ToString("F3")
            bar.Font.Color = tema.Texto
            bar.Font.Size = 13.5F
            bar.Font.Name = "Segoe UI"
        Next

        Dim hl = fp.Plot.AddHorizontalLine(limite, tema.Rojo, 2, ScottPlotNS.LineStyle.Dash,
                                           "Límite " & limite.ToString("F2"))
        hl.PositionLabel = True
        hl.PositionLabelBackground = tema.Rojo
        hl.PositionLabelFont.Color = Color.White

        fp.Plot.XTicks(xs, etiq)
        fp.Plot.XAxis.TickLabelStyle(rotation:=45, fontSize:=14.0F)
        fp.Plot.YAxis.Label(tituloY, color:=tema.TextoTenue, size:=16)
        fp.Plot.SetAxisLimitsY(0, Math.Max(limite * 1.5, validos.Max(Function(i) i.Valor) * 1.15))
        fp.Plot.Legend(enable:=False)
        fp.Refresh()
        Return fp
    End Function

    ' ─── Top-N peores (barras horizontales) ────────────────────────────────
    ''' <param name="menorEsMejor">
    ''' False (default, C/D): "peor" = valor más BAJO. Umbral default 0.90.
    ''' True (ALR y similares): "peor" = valor más ALTO. Pasar el límite real.
    ''' </param>
    Public Shared Function TopPeores(items As List(Of ItemCD), topN As Integer,
                                      tema As DashboardTheme,
                                      Optional menorEsMejor As Boolean = False,
                                      Optional umbral As Double = DashboardTheme.UMBRAL_CD,
                                      Optional formato As String = "F2") As ScottPlotNS.FormsPlot
        Dim fp As New ScottPlotNS.FormsPlot()
        EstilizarPlot(fp, tema)

        Dim conValor = items.Where(Function(i) i IsNot Nothing AndAlso i.Valor > 0)
        Dim validos = If(menorEsMejor,
                          conValor.OrderByDescending(Function(i) i.Valor).Take(topN).ToList(),
                          conValor.OrderBy(Function(i) i.Valor).Take(topN).ToList())
        If validos.Count = 0 Then
            fp.Plot.Title("Sin elementos", color:=tema.TextoTenue, size:=11)
            fp.Refresh()
            Return fp
        End If

        Dim n = validos.Count
        Dim ys = validos.Select(Function(i) i.Valor).ToArray()
        Dim xs = Enumerable.Range(0, n).Select(Function(i) CDbl(i)).ToArray()
        Dim etiq = validos.Select(Function(i) i.Etiqueta).ToArray()

        For i = 0 To n - 1
            Dim v = ys(i)
            Dim col As Color
            If menorEsMejor Then
                col = If(v <= umbral, tema.Verde, If(v <= umbral * 1.1, tema.Ambar, tema.Rojo))
            Else
                col = tema.ColorPorCD(v)
            End If
            Dim bar = fp.Plot.AddBar({v}, {CDbl(i)}, col)
            bar.Orientation = ScottPlotNS.Orientation.Horizontal
            bar.BarWidth = 0.68
            bar.BorderColor = Color.Transparent
            bar.ShowValuesAboveBars = True
            bar.ValueFormatter = Function(x) x.ToString(formato)
            bar.Font.Color = tema.Texto
            bar.Font.Size = 16.5F
            bar.Font.Name = "Segoe UI"
        Next

        fp.Plot.AddVerticalLine(umbral, tema.Rojo, 2, ScottPlotNS.LineStyle.Dash)
        fp.Plot.YTicks(xs, etiq)
        fp.Plot.YAxis.TickLabelStyle(fontSize:=17.0F)
        fp.Plot.XAxis.TickLabelStyle(fontSize:=16.5F)
        Dim xMax = Math.Max(If(menorEsMejor, umbral * 1.5, 1.5), ys.Max() * 1.2)
        fp.Plot.SetAxisLimitsX(0, xMax)
        fp.Plot.Legend(enable:=False)
        fp.Refresh()
        Return fp
    End Function

    ' ─── Donut de cumplimiento ─────────────────────────────────────────────
    Public Shared Function DonutCumplimiento(cumplen As Integer, noCumplen As Integer, sinCalc As Integer,
                                              tema As DashboardTheme) As ScottPlotNS.FormsPlot
        Dim fp As New ScottPlotNS.FormsPlot()
        EstilizarPlot(fp, tema)

        Dim vals As New List(Of Double)
        Dim lbls As New List(Of String)
        Dim cols As New List(Of Color)

        If cumplen > 0 Then vals.Add(cumplen) : lbls.Add("Cumplen (" & cumplen & ")") : cols.Add(tema.Verde)
        If noCumplen > 0 Then vals.Add(noCumplen) : lbls.Add("No cumplen (" & noCumplen & ")") : cols.Add(tema.Rojo)
        If sinCalc > 0 Then vals.Add(sinCalc) : lbls.Add("Sin cálculo (" & sinCalc & ")") : cols.Add(tema.TextoTenue)

        If vals.Count = 0 Then
            fp.Plot.Title("Sin elementos", color:=tema.TextoTenue, size:=11)
            fp.Refresh()
            Return fp
        End If

        Dim pie = fp.Plot.AddPie(vals.ToArray())
        pie.SliceLabels = lbls.ToArray()
        pie.SliceFillColors = cols.ToArray()
        pie.DonutSize = 0.55
        pie.ShowLabels = False
        pie.ShowValues = False
        pie.ShowPercentages = True
        pie.SliceFont.Color = Color.White
        pie.SliceFont.Size = 15.0F

        Dim total = cumplen + noCumplen + sinCalc
        Dim ann = fp.Plot.AddAnnotation(total.ToString() & "  elementos", ScottPlotNS.Alignment.MiddleCenter)
        ann.Font.Size = 17
        ann.Font.Color = tema.Texto
        ann.Background = False
        ann.BorderWidth = 0
        ann.Shadow = False

        fp.Plot.Legend(enable:=True, location:=ScottPlotNS.Alignment.LowerCenter)
        fp.Plot.XAxis.Hide()
        fp.Plot.YAxis.Hide()
        fp.Plot.XAxis2.Hide()
        fp.Plot.YAxis2.Hide()
        fp.Plot.Frameless()
        fp.Refresh()
        Return fp
    End Function

    ' ─── Barras por categoría (conteos) ────────────────────────────────────
    Public Shared Function BarrasCategorias(categorias As List(Of CategoriaConteo),
                                             tema As DashboardTheme,
                                             Optional tituloY As String = "Cantidad") As ScottPlotNS.FormsPlot
        Dim fp As New ScottPlotNS.FormsPlot()
        EstilizarPlot(fp, tema)

        If categorias Is Nothing OrElse categorias.Count = 0 Then
            fp.Plot.Title("Sin datos", color:=tema.TextoTenue, size:=11)
            fp.Refresh()
            Return fp
        End If

        Dim n = categorias.Count
        For i = 0 To n - 1
            Dim bar = fp.Plot.AddBar({CDbl(categorias(i).Conteo)}, {CDbl(i)}, categorias(i).Color)
            bar.BarWidth = 0.6
            bar.BorderColor = Color.Transparent
            bar.ShowValuesAboveBars = True
            bar.ValueFormatter = Function(v) CInt(v).ToString()
            bar.Font.Color = tema.Texto
            bar.Font.Size = 10
            bar.Font.Name = "Segoe UI"
        Next

        Dim xs = Enumerable.Range(0, n).Select(Function(i) CDbl(i)).ToArray()
        Dim etiq = categorias.Select(Function(c) c.Etiqueta).ToArray()
        Dim maxVal = Math.Max(1, categorias.Max(Function(c) c.Conteo))

        fp.Plot.XTicks(xs, etiq)
        fp.Plot.XAxis.TickLabelStyle(fontSize:=15.0F)
        fp.Plot.YAxis.TickLabelStyle(fontSize:=13.5F)
        fp.Plot.YAxis.Label(tituloY, color:=tema.TextoTenue, size:=16)
        fp.Plot.SetAxisLimitsY(0, maxVal * 1.25)
        fp.Plot.Legend(enable:=False)
        fp.Refresh()
        Return fp
    End Function

    ''' <summary>
    ''' Barras agrupadas Cumple / No cumple / Sin cálculo por piso. Formato
    ''' premium (fondo Panel, grilla sutil, labels encima de barras).
    ''' Las etiquetas de piso son más largas → se rotan 30°.
    ''' </summary>
    Public Shared Function BarrasPorPiso(filas As List(Of PisoFila), tema As DashboardTheme,
                                          Optional titulo As String = "") As ScottPlotNS.FormsPlot
        Dim fp As New ScottPlotNS.FormsPlot()
        EstilizarPlot(fp, tema)

        If filas Is Nothing OrElse filas.Count = 0 Then
            fp.Plot.Title("Sin datos", color:=tema.TextoTenue, size:=11)
            fp.Refresh()
            Return fp
        End If

        Dim n = filas.Count
        Dim xs = Enumerable.Range(0, n).Select(Function(i) CDbl(i)).ToArray()
        Dim etiq = filas.Select(Function(f) f.Piso).ToArray()
        Dim yC = filas.Select(Function(f) CDbl(f.Cumple)).ToArray()
        Dim yN = filas.Select(Function(f) CDbl(f.NoCumple)).ToArray()
        Dim ySd = filas.Select(Function(f) CDbl(f.SinCalc)).ToArray()
        Dim tot = Enumerable.Range(0, n).Select(Function(i) yC(i) + yN(i) + ySd(i)).ToArray()

        ' Columnas APILADAS por piso: Cumple en la base, No cumple encima,
        ' Sin cálculo arriba. ScottPlot 4.1 soporta apilado vía BarPlot.ValueBase:
        ' la barra parte desde ese offset en vez de 0. El Label se asigna solo a
        ' la PRIMERA barra visible de cada color para que la leyenda muestre
        ' una sola entrada por categoría.
        Dim ancho As Double = 0.68
        Dim lblCumple = True, lblNo = True, lblSd = True
        For i = 0 To n - 1
            If yC(i) > 0 Then
                Dim b = fp.Plot.AddBar({yC(i)}, {xs(i)}, tema.Verde)
                b.BarWidth = ancho : b.BorderColor = Color.Transparent
                b.Font.Color = tema.Texto : b.Font.Size = 15 : b.Font.Name = "Segoe UI"
                If lblCumple Then
                    b.Label = "Cumple"
                    lblCumple = False
                End If
            End If
            If yN(i) > 0 Then
                Dim b = fp.Plot.AddBar({yN(i)}, {xs(i)}, tema.Rojo)
                b.BarWidth = ancho : b.BorderColor = Color.Transparent
                ' ScottPlot 4.1: ValueOffsets apila (uno por barra). ValueBase
                ' es piso común y NO apila — por eso antes la barra "sobreescribía"
                ' en lugar de montarse sobre la verde.
                b.ValueOffsets = New Double() {yC(i)}
                b.Font.Color = tema.Texto : b.Font.Size = 15 : b.Font.Name = "Segoe UI"
                If lblNo Then
                    b.Label = "No cumple"
                    lblNo = False
                End If
            End If
            If ySd(i) > 0 Then
                Dim b = fp.Plot.AddBar({ySd(i)}, {xs(i)}, tema.TextoTenue)
                b.BarWidth = ancho : b.BorderColor = Color.Transparent
                b.ValueOffsets = New Double() {yC(i) + yN(i)}
                b.Font.Color = tema.Texto : b.Font.Size = 15 : b.Font.Name = "Segoe UI"
                If lblSd Then
                    b.Label = "Sin cálculo"
                    lblSd = False
                End If
            End If

            ' Total encima de la pila: una sola etiqueta clara con el conteo total.
            If tot(i) > 0 Then
                Dim lblTot = fp.Plot.AddText(CInt(tot(i)).ToString(), xs(i), tot(i),
                                              size:=18, color:=tema.Texto)
                lblTot.Alignment = ScottPlotNS.Alignment.LowerCenter
                lblTot.Font.Bold = True
                lblTot.Font.Name = "Segoe UI"
            End If
        Next

        fp.Plot.XTicks(xs, etiq)
        fp.Plot.XAxis.TickLabelStyle(rotation:=30, fontSize:=19.0F)
        fp.Plot.YAxis.TickLabelStyle(fontSize:=18.0F)
        fp.Plot.YAxis.Label("elementos", color:=tema.TextoTenue, size:=21)
        Dim yMax = tot.DefaultIfEmpty(0).Max()
        fp.Plot.SetAxisLimitsY(0, Math.Max(1, yMax) * 1.22)
        Dim lgnd = fp.Plot.Legend(enable:=True, location:=ScottPlotNS.Alignment.UpperRight)
        lgnd.FontSize = 18
        lgnd.FillColor = tema.Panel
        lgnd.OutlineColor = tema.Separador
        lgnd.FontColor = tema.Texto
        If Not String.IsNullOrWhiteSpace(titulo) Then
            fp.Plot.Title(titulo, color:=tema.Texto, size:=21)
        End If
        fp.Refresh()
        Return fp
    End Function

    Public Class PisoFila
        Public Property Piso As String
        Public Property Cumple As Integer
        Public Property NoCumple As Integer
        Public Property SinCalc As Integer
    End Class

    Public Shared Function PngBarrasPorPiso(filas As List(Of PisoFila), tema As DashboardTheme,
                                             Optional titulo As String = "",
                                             Optional width As Integer = 1200,
                                             Optional height As Integer = 560) As Byte()
        Using fp = BarrasPorPiso(filas, tema, titulo)
            Return PlotAPng(fp, width, height)
        End Using
    End Function

    ' ─── Render a PNG (para embeber en informes .docx y .pdf) ──────────────
    ''' <summary>
    ''' Renderiza un FormsPlot a PNG en memoria, sin necesidad de montarlo en un
    ''' Form visible. Preserva paleta y tipografías del dashboard.
    ''' </summary>
    Public Shared Function PlotAPng(fp As ScottPlotNS.FormsPlot, width As Integer, height As Integer) As Byte()
        If fp Is Nothing Then Return Nothing
        Try
            Using bmp As Bitmap = fp.Plot.Render(width, height)
                If bmp Is Nothing Then Return Nothing
                Using ms As New IO.MemoryStream()
                    bmp.Save(ms, Imaging.ImageFormat.Png)
                    Return ms.ToArray()
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    Public Shared Function PngBarrasCD(items As List(Of ItemCD), tema As DashboardTheme,
                                        Optional tituloY As String = "C/D",
                                        Optional width As Integer = 1200,
                                        Optional height As Integer = 500) As Byte()
        Using fp = BarrasCDVertical(items, tema, tituloY)
            Return PlotAPng(fp, width, height)
        End Using
    End Function

    Public Shared Function PngDonut(cumplen As Integer, noCumplen As Integer, sinCalc As Integer,
                                     tema As DashboardTheme,
                                     Optional width As Integer = 700,
                                     Optional height As Integer = 500) As Byte()
        Using fp = DonutCumplimiento(cumplen, noCumplen, sinCalc, tema)
            Return PlotAPng(fp, width, height)
        End Using
    End Function

    Public Shared Function PngTopPeores(items As List(Of ItemCD), topN As Integer,
                                         tema As DashboardTheme,
                                         Optional menorEsMejor As Boolean = False,
                                         Optional umbral As Double = DashboardTheme.UMBRAL_CD,
                                         Optional formato As String = "F2",
                                         Optional width As Integer = 900,
                                         Optional height As Integer = 500) As Byte()
        Using fp = TopPeores(items, topN, tema, menorEsMejor, umbral, formato)
            Return PlotAPng(fp, width, height)
        End Using
    End Function

    Public Shared Function PngCategorias(cat As List(Of CategoriaConteo), tema As DashboardTheme,
                                          Optional tituloY As String = "cantidad",
                                          Optional width As Integer = 900,
                                          Optional height As Integer = 500) As Byte()
        Using fp = BarrasCategorias(cat, tema, tituloY)
            Return PlotAPng(fp, width, height)
        End Using
    End Function

End Class
