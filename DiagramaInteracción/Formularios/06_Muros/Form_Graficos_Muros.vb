''' <summary>
''' Dashboard premium del módulo de Muros — Flexo-compresión, Cortante, ALR
''' y Elementos de Borde. Mismo look y toggle Dark/Light que los demás
''' módulos. Reutiliza <see cref="MurosResumenService.CalcularFila"/> para
''' no duplicar la agregación "peor sección de toda la altura".
''' </summary>
Public Class Form_Graficos_Muros
    Inherits Form

    Private Const LIMITE_ALR As Double = 0.35

    Private _tema As DashboardTheme = DashboardTheme.Actual
    Private _vista As Vista = Vista.Flexo
    Private _filas As List(Of FilaMuro)

    Private Enum Vista
        Flexo
        Cortante
        ALR
        Borde
    End Enum

    Public Sub New()
        Me.Text = "ARCO · Gráficas Resumen — Muros"
        Me.Size = New Size(1480, 920)
        Me.MinimumSize = New Size(1100, 680)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.BackColor = _tema.Fondo

        AddHandler Me.Shown, Sub() Reconstruir()
        AddHandler DashboardTheme.TemaCambiado, AddressOf OnTemaCambiado
        AddHandler Me.FormClosed, Sub() RemoveHandler DashboardTheme.TemaCambiado, AddressOf OnTemaCambiado
    End Sub

    Private Sub OnTemaCambiado(sender As Object, e As EventArgs)
        _tema = DashboardTheme.Actual
        Me.BackColor = _tema.Fondo
        Reconstruir()
    End Sub

    Private Sub Reconstruir()
        Me.SuspendLayout()
        Me.Controls.Clear()
        CargarFilas()

        Dim cuerpo = ArmarCuerpo()
        Dim barraBotones = ArmarBarraBotones()
        Dim header = ScottDashboardHelpers.CrearEncabezado(
            "DASHBOARD DE MUROS",
            "Flexo-compresión · Cortante · ALR · Elementos de Borde",
            _tema)

        Me.Controls.Add(cuerpo)
        Me.Controls.Add(barraBotones)
        Me.Controls.Add(header)

        Me.ResumeLayout()
    End Sub

    Private Sub CargarFilas()
        _filas = New List(Of FilaMuro)
        Try
            Dim p = Form_06_PagMuros.proyecto
            If p Is Nothing OrElse p.Elementos Is Nothing OrElse p.Elementos.Muros Is Nothing Then Return
            For Each m In p.Elementos.Muros.Lista_Muros
                If m Is Nothing Then Continue For
                _filas.Add(MurosResumenService.CalcularFila(m))
            Next
        Catch ex As Exception
            Logger.Error(ex, "Form_Graficos_Muros.CargarFilas")
        End Try
    End Sub

    Private Function ArmarBarraBotones() As FlowLayoutPanel
        Dim f As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top, .Height = 48, .BackColor = _tema.Fondo,
            .Padding = New Padding(14, 8, 14, 4), .WrapContents = False}
        f.Controls.Add(BotonVista("Flexo-compresión", Vista.Flexo))
        f.Controls.Add(BotonVista("Cortante", Vista.Cortante))
        f.Controls.Add(BotonVista("ALR", Vista.ALR))
        f.Controls.Add(BotonVista("Elem. de Borde", Vista.Borde))
        Return f
    End Function

    Private Function BotonVista(txt As String, v As Vista) As Button
        Dim activa = (v = _vista)
        Dim b As New Button() With {
            .Text = txt, .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .ForeColor = If(activa, Color.White, _tema.Texto),
            .BackColor = If(activa, _tema.Acento, _tema.Panel),
            .Size = New Size(170, 32), .Cursor = Cursors.Hand,
            .Margin = New Padding(0, 2, 8, 0), .Tag = v}
        b.FlatAppearance.BorderColor = If(activa, _tema.Acento, _tema.Separador)
        b.FlatAppearance.BorderSize = 1
        AddHandler b.Click, Sub()
                                _vista = CType(b.Tag, Vista)
                                Reconstruir()
                            End Sub
        Return b
    End Function

    Private Function ArmarCuerpo() As TableLayoutPanel
        Dim g As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill, .BackColor = _tema.Fondo,
            .ColumnCount = 4, .RowCount = 3, .Padding = New Padding(14)}
        For i = 0 To 3 : g.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25)) : Next
        g.RowStyles.Add(New RowStyle(SizeType.Absolute, 108))
        g.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
        g.RowStyles.Add(New RowStyle(SizeType.Percent, 40))

        If _vista = Vista.Borde Then
            ArmarVistaBorde(g)
        Else
            ArmarVistaNumerica(g)
        End If
        Return g
    End Function

    Private Sub ArmarVistaNumerica(g As TableLayoutPanel)
        Dim items = DatosParaVista(_vista)
        Dim validos = items.Where(Function(i) i.Valor >= 0).ToList()
        Dim conValor = validos.Where(Function(i) i.Valor > 0).ToList()
        Dim sinCalc = items.Count - validos.Count

        Dim esAlr = (_vista = Vista.ALR)
        Dim cumplen As Integer
        Dim noCumplen As Integer
        Dim prom As Double
        Dim peor As Double
        Dim labelPeor As String
        If esAlr Then
            cumplen = conValor.Where(Function(i) i.Valor <= LIMITE_ALR).Count()
            noCumplen = conValor.Count - cumplen
            prom = If(conValor.Count = 0, 0.0, conValor.Average(Function(i) i.Valor))
            peor = If(conValor.Count = 0, 0.0, conValor.Max(Function(i) i.Valor))
            labelPeor = "máx: " & peor.ToString("F3")
        Else
            cumplen = conValor.Where(Function(i) i.Valor >= DashboardTheme.UMBRAL_CD).Count()
            noCumplen = conValor.Count - cumplen
            prom = If(conValor.Count = 0, 0.0, conValor.Average(Function(i) i.Valor))
            peor = If(conValor.Count = 0, 0.0, conValor.Min(Function(i) i.Valor))
            labelPeor = "peor: " & peor.ToString("F2")
        End If

        Dim txtCumple = If(esAlr, "ALR ≤ " & LIMITE_ALR.ToString("F2"),
                                  "C/D ≥ " & DashboardTheme.UMBRAL_CD.ToString("F2"))
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL MUROS", items.Count.ToString(), _tema.Acento, "evaluados", _tema), 0, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("CUMPLEN", cumplen.ToString(), _tema.Verde, txtCumple, _tema), 1, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("NO CUMPLEN", noCumplen.ToString(), _tema.Rojo, "requieren revisión", _tema), 2, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi(If(esAlr, "ALR PROMEDIO", "C/D PROMEDIO"),
                                                        prom.ToString(If(esAlr, "F3", "F2")),
                                                        If(esAlr, If(prom <= LIMITE_ALR, _tema.Verde, _tema.Ambar),
                                                                  If(prom >= DashboardTheme.UMBRAL_CD, _tema.Verde, _tema.Ambar)),
                                                        labelPeor, _tema), 3, 0)

        Dim principal As ScottPlot.FormsPlot
        If esAlr Then
            principal = ScottDashboardHelpers.BarrasLimiteSuperior(conValor, LIMITE_ALR, _tema, "ALR")
        Else
            principal = ScottDashboardHelpers.BarrasCDVertical(conValor, _tema, "C/D")
        End If
        Dim panelBarras = ScottDashboardHelpers.PanelConTitulo(TituloVista(_vista), principal, _tema)
        g.Controls.Add(panelBarras, 0, 1)
        g.SetColumnSpan(panelBarras, 3)

        Dim panelDonut = ScottDashboardHelpers.PanelConTitulo("Distribución de cumplimiento",
                                                               ScottDashboardHelpers.DonutCumplimiento(cumplen, noCumplen, sinCalc, _tema),
                                                               _tema)
        g.Controls.Add(panelDonut, 3, 1)

        Dim topPlot As ScottPlot.FormsPlot
        If esAlr Then
            topPlot = ScottDashboardHelpers.TopPeores(conValor, 10, _tema, menorEsMejor:=True, umbral:=LIMITE_ALR, formato:="F3")
        Else
            topPlot = ScottDashboardHelpers.TopPeores(conValor, 10, _tema)
        End If
        Dim panelTop = ScottDashboardHelpers.PanelConTitulo("Top 10 peores", topPlot, _tema)
        g.Controls.Add(panelTop, 0, 2)
        g.SetColumnSpan(panelTop, 2)

        Dim panelResumen = ScottDashboardHelpers.PanelConTitulo("Resumen por revisión",
                                                                 ScottDashboardHelpers.BarrasCategorias(CategoriasResumen(), _tema, "promedio"),
                                                                 _tema)
        g.Controls.Add(panelResumen, 2, 2)
        g.SetColumnSpan(panelResumen, 2)
    End Sub

    Private Sub ArmarVistaBorde(g As TableLayoutPanel)
        Dim catConteo = ConteoElemBorde()
        Dim totalEb = catConteo.Sum(Function(c) c.Conteo)
        Dim especial = catConteo.FirstOrDefault(Function(c) c.Etiqueta.Contains("Especial"))?.Conteo
        Dim noReq = catConteo.FirstOrDefault(Function(c) c.Etiqueta.Contains("No Requiere"))?.Conteo

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL MUROS", _filas.Count.ToString(), _tema.Acento, "evaluados", _tema), 0, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("ELEMENTOS BORDE TOTAL", totalEb.ToString(), _tema.Acento, "Izq + Der", _tema), 1, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("ESPECIALIZADO", If(especial.HasValue, especial.Value, 0).ToString(), _tema.Rojo, "mayor confinamiento", _tema), 2, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("NO REQUIERE", If(noReq.HasValue, noReq.Value, 0).ToString(), _tema.Verde, "sin demanda", _tema), 3, 0)

        Dim panelCat = ScottDashboardHelpers.PanelConTitulo("Elementos de Borde — distribución",
                                                              ScottDashboardHelpers.BarrasCategorias(catConteo, _tema, "cantidad"),
                                                              _tema)
        g.Controls.Add(panelCat, 0, 1)
        g.SetColumnSpan(panelCat, 4)
        g.SetRowSpan(panelCat, 2)
    End Sub

    Private Function TituloVista(v As Vista) As String
        Select Case v
            Case Vista.Flexo : Return "Flexo-compresión (C/D)  ·  peor sección de la altura"
            Case Vista.Cortante : Return "Cortante (C/D)  ·  peor sección de la altura"
            Case Vista.ALR : Return "ALR (máx G, D)  ·  límite " & LIMITE_ALR.ToString("F2")
            Case Else : Return "Elementos de Borde"
        End Select
    End Function

    Private Function DatosParaVista(v As Vista) As List(Of ItemCD)
        Dim res As New List(Of ItemCD)
        For Each f In _filas
            Dim etiq = f.Label & "  (" & f.Direccion & ")"
            Select Case v
                Case Vista.Flexo
                    res.Add(New ItemCD(etiq, If(f.FFlexMin > 0, CDbl(f.FFlexMin), -1.0)))
                Case Vista.Cortante
                    res.Add(New ItemCD(etiq, If(f.FCortMin > 0, CDbl(f.FCortMin), -1.0)))
                Case Vista.ALR
                    Dim alr = Math.Max(f.ALR_G, f.ALR_D)
                    res.Add(New ItemCD(etiq, If(alr > 0, CDbl(alr), -1.0)))
            End Select
        Next
        Return res
    End Function

    Private Function ConteoElemBorde() As List(Of CategoriaConteo)
        Dim esp As Integer, no As Integer, noReq As Integer
        For Each f In _filas
            For Each eb In {f.EBIzq, f.EBDer}
                If String.IsNullOrWhiteSpace(eb) Then Continue For
                If eb.Contains("Especial") Then
                    esp += 1
                ElseIf eb.Contains("No Requiere") OrElse eb.Contains("No requiere") Then
                    noReq += 1
                Else
                    no += 1
                End If
            Next
        Next
        Dim res As New List(Of CategoriaConteo) From {
            New CategoriaConteo("Especializado", esp, _tema.Rojo),
            New CategoriaConteo("No Especial", no, _tema.Ambar),
            New CategoriaConteo("No Requiere", noReq, _tema.Verde)}
        Return res
    End Function

    Private Function CategoriasResumen() As List(Of CategoriaConteo)
        Dim nombres = {"Flexo", "Cort.", "ALR"}
        Dim vistas = {Vista.Flexo, Vista.Cortante, Vista.ALR}
        Dim res As New List(Of CategoriaConteo)
        For i = 0 To 2
            Dim items = DatosParaVista(vistas(i))
            Dim pos = items.Where(Function(x) x.Valor > 0).Select(Function(x) x.Valor).ToList()
            Dim prom = If(pos.Count = 0, 0.0, pos.Average())
            If vistas(i) = Vista.ALR Then
                Dim col = If(prom <= LIMITE_ALR, _tema.Verde, If(prom <= LIMITE_ALR * 1.1, _tema.Ambar, _tema.Rojo))
                res.Add(New CategoriaConteo(nombres(i), CInt(prom * 1000) / 10, col))
            Else
                res.Add(New CategoriaConteo(nombres(i), CInt(prom * 100), _tema.ColorPorCD(prom)))
            End If
        Next
        Return res
    End Function

End Class
