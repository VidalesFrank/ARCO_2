Imports ARCO.eNumeradores

''' <summary>
''' Dashboard premium del módulo de Zapatas — Peor Global, Suelo, Excentricidad,
''' Punzonamiento, Cortante+Flexión y Tipo de Apoyo. Mismo look y toggle
''' Dark/Light. Filtros por tipo de apoyo y "solo las que no cumplen".
''' Reutiliza <see cref="ZapataService.FactoresPorCombinacion"/> y
''' <see cref="ZapataService.Resumir"/> — no recalcula.
''' </summary>
Public Class Form_Graficos_Zapatas
    Inherits Form

    Public Property Zapatas As List(Of cZapata)

    Private _tema As DashboardTheme = DashboardTheme.Actual
    Private _vista As Vista = Vista.PeorGlobal
    Private _filtroTipo As Integer = 0  ' 0=Todas 1=Central 2=Medianera 3=Esquinera
    Private _soloNoCumplen As Boolean = False
    Private _cboTipo As ComboBox
    Private _chkNoCumplen As CheckBox

    Private Enum Vista
        PeorGlobal
        Suelo
        Excentricidad
        Punzonamiento
        CortFlex
        TipoApoyo
    End Enum

    Public Sub New()
        Me.Text = "ARCO · Gráficas Resumen — Zapatas"
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

        Dim cuerpo = ArmarCuerpo()
        Dim barraBotones = ArmarBarraBotones()
        Dim header = ScottDashboardHelpers.CrearEncabezado(
            "DASHBOARD DE ZAPATAS",
            "Peor global · Suelo · Excentricidad · Punzonamiento · Cortante+Flexión · Tipo de apoyo",
            _tema)

        Me.Controls.Add(cuerpo)
        Me.Controls.Add(barraBotones)
        Me.Controls.Add(header)

        Me.ResumeLayout()
    End Sub

    Private Function ArmarBarraBotones() As FlowLayoutPanel
        Dim f As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top, .Height = 48, .BackColor = _tema.Fondo,
            .Padding = New Padding(14, 8, 14, 4), .WrapContents = False}
        f.Controls.Add(BotonVista("Peor global", Vista.PeorGlobal))
        f.Controls.Add(BotonVista("Suelo", Vista.Suelo))
        f.Controls.Add(BotonVista("Excentricidad", Vista.Excentricidad))
        f.Controls.Add(BotonVista("Punzonamiento", Vista.Punzonamiento))
        f.Controls.Add(BotonVista("Cort+Flex", Vista.CortFlex))
        f.Controls.Add(BotonVista("Tipo apoyo", Vista.TipoApoyo))

        Dim sep As New Panel() With {.Width = 20, .Height = 1, .BackColor = Color.Transparent}
        f.Controls.Add(sep)

        Dim lblT As New Label() With {
            .Text = "Tipo:", .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = _tema.TextoTenue, .AutoSize = True,
            .Margin = New Padding(0, 9, 6, 0), .BackColor = Color.Transparent}
        f.Controls.Add(lblT)

        _cboTipo = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = _tema.Texto, .BackColor = _tema.Panel,
            .FlatStyle = FlatStyle.Flat,
            .Width = 130, .Margin = New Padding(0, 4, 10, 0)}
        For Each o In New String() {"<Todas>", "Central", "Medianera", "Esquinera"}
            _cboTipo.Items.Add(o)
        Next
        _cboTipo.SelectedIndex = _filtroTipo
        AddHandler _cboTipo.SelectedIndexChanged, Sub()
                                                      _filtroTipo = _cboTipo.SelectedIndex
                                                      Reconstruir()
                                                  End Sub
        f.Controls.Add(_cboTipo)

        _chkNoCumplen = New CheckBox() With {
            .Text = "Solo las que no cumplen",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = _tema.Texto, .BackColor = Color.Transparent,
            .AutoSize = True, .Margin = New Padding(4, 9, 0, 0),
            .Checked = _soloNoCumplen}
        AddHandler _chkNoCumplen.CheckedChanged, Sub()
                                                     _soloNoCumplen = _chkNoCumplen.Checked
                                                     Reconstruir()
                                                 End Sub
        f.Controls.Add(_chkNoCumplen)

        Return f
    End Function

    Private Function BotonVista(txt As String, v As Vista) As Button
        Dim activa = (v = _vista)
        Dim b As New Button() With {
            .Text = txt, .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .ForeColor = If(activa, Color.White, _tema.Texto),
            .BackColor = If(activa, _tema.Acento, _tema.Panel),
            .Size = New Size(130, 32), .Cursor = Cursors.Hand,
            .Margin = New Padding(0, 2, 6, 0), .Tag = v}
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

        If _vista = Vista.TipoApoyo Then
            ArmarVistaTipoApoyo(g)
        Else
            ArmarVistaNumerica(g)
        End If
        Return g
    End Function

    Private Sub ArmarVistaNumerica(g As TableLayoutPanel)
        Dim items = DatosParaVista(_vista)
        Dim validos = items.Where(Function(i) i.Valor > 0).ToList()
        Dim sinCalc = items.Count - validos.Count
        Dim cumplen = validos.Where(Function(i) i.Valor >= DashboardTheme.UMBRAL_CD).Count()
        Dim noCumplen = validos.Count - cumplen
        Dim prom = If(validos.Count = 0, 0.0, validos.Average(Function(i) i.Valor))
        Dim peor = If(validos.Count = 0, 0.0, validos.Min(Function(i) i.Valor))

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL ZAPATAS", items.Count.ToString(), _tema.Acento, "en filtro", _tema), 0, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("CUMPLEN", cumplen.ToString(), _tema.Verde, "C/D ≥ " & DashboardTheme.UMBRAL_CD.ToString("F2"), _tema), 1, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("NO CUMPLEN", noCumplen.ToString(), _tema.Rojo, "requieren revisión", _tema), 2, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("C/D PROMEDIO", prom.ToString("F2"),
                                                        If(prom >= DashboardTheme.UMBRAL_CD, _tema.Verde, _tema.Ambar),
                                                        "peor: " & peor.ToString("F2"), _tema), 3, 0)

        Dim panelBarras = ScottDashboardHelpers.PanelConTitulo(TituloVista(_vista),
                                                                ScottDashboardHelpers.BarrasCDVertical(validos, _tema, "C/D"),
                                                                _tema)
        g.Controls.Add(panelBarras, 0, 1)
        g.SetColumnSpan(panelBarras, 3)

        g.Controls.Add(ScottDashboardHelpers.PanelConTitulo("Distribución de cumplimiento",
                                                             ScottDashboardHelpers.DonutCumplimiento(cumplen, noCumplen, sinCalc, _tema),
                                                             _tema), 3, 1)

        Dim panelTop = ScottDashboardHelpers.PanelConTitulo("Top 10 peores",
                                                             ScottDashboardHelpers.TopPeores(validos, 10, _tema),
                                                             _tema)
        g.Controls.Add(panelTop, 0, 2)
        g.SetColumnSpan(panelTop, 2)

        Dim panelResumen = ScottDashboardHelpers.PanelConTitulo("Resumen por revisión",
                                                                 ScottDashboardHelpers.BarrasCategorias(CategoriasResumen(), _tema, "C/D promedio × 100"),
                                                                 _tema)
        g.Controls.Add(panelResumen, 2, 2)
        g.SetColumnSpan(panelResumen, 2)
    End Sub

    Private Sub ArmarVistaTipoApoyo(g As TableLayoutPanel)
        Dim lista = ZapatasFiltradas()
        Dim nCen = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central).Count()
        Dim nMed = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera).Count()
        Dim nEsq = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera).Count()

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL ZAPATAS", lista.Count.ToString(), _tema.Acento, "en filtro", _tema), 0, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("CENTRAL", nCen.ToString(), _tema.Verde, "interior, φVc 100%", _tema), 1, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("MEDIANERA", nMed.ToString(), _tema.Ambar, "borde, φVc 62%", _tema), 2, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("ESQUINERA", nEsq.ToString(), _tema.Rojo, "esquina, φVc 37%", _tema), 3, 0)

        Dim cat As New List(Of CategoriaConteo) From {
            New CategoriaConteo("Central", nCen, _tema.Verde),
            New CategoriaConteo("Medianera", nMed, _tema.Ambar),
            New CategoriaConteo("Esquinera", nEsq, _tema.Rojo)}
        Dim panel = ScottDashboardHelpers.PanelConTitulo("Tipo de apoyo — distribución",
                                                           ScottDashboardHelpers.BarrasCategorias(cat, _tema, "cantidad"),
                                                           _tema)
        g.Controls.Add(panel, 0, 1)
        g.SetColumnSpan(panel, 4)
        g.SetRowSpan(panel, 2)
    End Sub

    ' ─── Datos ─────────────────────────────────────────────────────────────
    Private Function ZapatasFiltradas() As List(Of cZapata)
        If Zapatas Is Nothing Then Return New List(Of cZapata)
        Dim lista = Zapatas.AsEnumerable()
        Select Case _filtroTipo
            Case 1 : lista = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central)
            Case 2 : lista = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera)
            Case 3 : lista = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera)
        End Select
        If _soloNoCumplen Then
            lista = lista.Where(Function(z) Not ZapataService.Resumir(z).Cumple)
        End If
        Return lista.ToList()
    End Function

    Private Function NombreZapata(z As cZapata) As String
        If Not String.IsNullOrWhiteSpace(z.Nombre) Then Return z.Nombre
        Return z.Label_joint
    End Function

    Private Function DatosParaVista(v As Vista) As List(Of ItemCD)
        Dim res As New List(Of ItemCD)
        For Each z In ZapatasFiltradas()
            Dim etiq = NombreZapata(z)
            Dim val As Double = -1
            Try
                If v = Vista.PeorGlobal Then
                    Dim r = ZapataService.Resumir(z)
                    val = If(r.TieneResultados AndAlso r.PeorFactor > 0, r.PeorFactor, -1)
                Else
                    Dim factores = ZapataService.FactoresPorCombinacion(z)
                    If factores IsNot Nothing AndAlso factores.Count > 0 Then
                        Select Case v
                            Case Vista.Suelo : val = MinPos(factores.Select(Function(f) f.Suelo))
                            Case Vista.Excentricidad : val = MinPos(factores.Select(Function(f) f.Excentricidad))
                            Case Vista.Punzonamiento : val = MinPos(factores.Select(Function(f) f.Punzonamiento))
                            Case Vista.CortFlex
                                Dim c = MinPos(factores.Select(Function(f) f.Cortante))
                                Dim fl = MinPos(factores.Select(Function(f) f.Flexion))
                                Dim arr = {c, fl}.Where(Function(x) x > 0).ToArray()
                                val = If(arr.Length = 0, -1, arr.Min())
                        End Select
                    End If
                End If
            Catch
            End Try
            res.Add(New ItemCD(etiq, val))
        Next
        Return res
    End Function

    Private Function MinPos(seq As IEnumerable(Of Double)) As Double
        Dim pos = seq.Where(Function(v) v > 0).ToList()
        If pos.Count = 0 Then Return -1
        Return pos.Min()
    End Function

    Private Function CategoriasResumen() As List(Of CategoriaConteo)
        Dim nombres = {"Suelo", "Excentr.", "Punzon.", "Cort+Fl"}
        Dim vistas = {Vista.Suelo, Vista.Excentricidad, Vista.Punzonamiento, Vista.CortFlex}
        Dim res As New List(Of CategoriaConteo)
        For i = 0 To 3
            Dim items = DatosParaVista(vistas(i))
            Dim pos = items.Where(Function(x) x.Valor > 0).Select(Function(x) x.Valor).ToList()
            Dim prom = If(pos.Count = 0, 0.0, pos.Average())
            res.Add(New CategoriaConteo(nombres(i), CInt(prom * 100), _tema.ColorPorCD(prom)))
        Next
        Return res
    End Function

    Private Function TituloVista(v As Vista) As String
        Select Case v
            Case Vista.PeorGlobal : Return "Peor global (C/D)  ·  menor de todas las revisiones"
            Case Vista.Suelo : Return "Esfuerzo al suelo  ·  C/D = qa / qmax"
            Case Vista.Excentricidad : Return "Excentricidad  ·  C/D"
            Case Vista.Punzonamiento : Return "Punzonamiento  ·  C/D = φVc / Vu"
            Case Else : Return "Cortante y Flexión  ·  C/D mínimo de ambas"
        End Select
    End Function

End Class
