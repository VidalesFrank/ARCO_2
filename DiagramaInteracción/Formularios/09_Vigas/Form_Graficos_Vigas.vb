''' <summary>
''' Dashboard premium del módulo de Vigas — Flexión negativa, positiva, Cortante
''' y Estado General, con filtro por piso. Mismo look y toggle Dark/Light que
''' los demás módulos. Agrega por viga tomando el frame más crítico.
''' </summary>
Public Class Form_Graficos_Vigas
    Inherits Form

    Public Property Vigas As List(Of cViga)

    Private _tema As DashboardTheme = DashboardTheme.Actual
    Private _vista As Vista = Vista.FlexNeg
    Private _pisoSel As String = "<Todos>"
    Private _cboPiso As ComboBox

    Private Enum Vista
        FlexNeg
        FlexPos
        Cortante
        Estado
    End Enum

    Public Sub New()
        Me.Text = "ARCO · Gráficas Resumen — Vigas"
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
            "DASHBOARD DE VIGAS",
            "Flexión M(-) · Flexión M(+) · Cortante · Estado",
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
        f.Controls.Add(BotonVista("Flexión M(-)", Vista.FlexNeg))
        f.Controls.Add(BotonVista("Flexión M(+)", Vista.FlexPos))
        f.Controls.Add(BotonVista("Cortante", Vista.Cortante))
        f.Controls.Add(BotonVista("Estado General", Vista.Estado))

        Dim sep As New Panel() With {.Width = 20, .Height = 1, .BackColor = Color.Transparent}
        f.Controls.Add(sep)

        Dim lblPiso As New Label() With {
            .Text = "Piso:", .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = _tema.TextoTenue, .AutoSize = True,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Margin = New Padding(0, 9, 6, 0), .BackColor = Color.Transparent}
        f.Controls.Add(lblPiso)

        _cboPiso = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = _tema.Texto, .BackColor = _tema.Panel,
            .FlatStyle = FlatStyle.Flat,
            .Width = 160, .Margin = New Padding(0, 4, 0, 0)}
        _cboPiso.Items.Add("<Todos>")
        For Each p In PisosDisponibles()
            _cboPiso.Items.Add(p)
        Next
        _cboPiso.SelectedItem = If(_cboPiso.Items.Contains(_pisoSel), _pisoSel, "<Todos>")
        AddHandler _cboPiso.SelectedIndexChanged, Sub()
                                                      _pisoSel = _cboPiso.SelectedItem.ToString()
                                                      Reconstruir()
                                                  End Sub
        f.Controls.Add(_cboPiso)
        Return f
    End Function

    Private Function BotonVista(txt As String, v As Vista) As Button
        Dim activa = (v = _vista)
        Dim b As New Button() With {
            .Text = txt, .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .ForeColor = If(activa, Color.White, _tema.Texto),
            .BackColor = If(activa, _tema.Acento, _tema.Panel),
            .Size = New Size(140, 32), .Cursor = Cursors.Hand,
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

        If _vista = Vista.Estado Then
            ArmarVistaEstado(g)
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

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL VIGAS", items.Count.ToString(), _tema.Acento, "evaluadas", _tema), 0, 0)
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

    Private Sub ArmarVistaEstado(g As TableLayoutPanel)
        Dim cat = ConteoEstado()
        Dim total = cat.Sum(Function(c) c.Conteo)
        Dim ok = cat.FirstOrDefault(Function(c) c.Etiqueta = "Cumple")?.Conteo
        Dim flex = cat.FirstOrDefault(Function(c) c.Etiqueta.StartsWith("Rev. flex"))?.Conteo
        Dim cort = cat.FirstOrDefault(Function(c) c.Etiqueta.StartsWith("Rev. cort"))?.Conteo

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL VIGAS", total.ToString(), _tema.Acento, "evaluadas", _tema), 0, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("CUMPLE", If(ok.HasValue, ok.Value, 0).ToString(), _tema.Verde, "flex + cort OK", _tema), 1, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("REV. FLEXIÓN", If(flex.HasValue, flex.Value, 0).ToString(), _tema.Ambar, "M(-) o M(+) < umbral", _tema), 2, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("REV. CORTANTE", If(cort.HasValue, cort.Value, 0).ToString(), _tema.Rojo, "φVn/Vu < umbral", _tema), 3, 0)

        Dim panel = ScottDashboardHelpers.PanelConTitulo("Estado General — distribución",
                                                           ScottDashboardHelpers.BarrasCategorias(cat, _tema, "cantidad"),
                                                           _tema)
        g.Controls.Add(panel, 0, 1)
        g.SetColumnSpan(panel, 4)
        g.SetRowSpan(panel, 2)
    End Sub

    ' ─── Datos ─────────────────────────────────────────────────────────────
    Private Function ObtenerVigas() As List(Of cViga)
        Try
            If Vigas Is Nothing Then Return New List(Of cViga)
            Dim res = Vigas.Where(Function(v) v IsNot Nothing AndAlso v.IncluirEnRevision AndAlso
                                              v.Frames IsNot Nothing AndAlso v.Frames.Count > 0).ToList()
            If _pisoSel IsNot Nothing AndAlso _pisoSel <> "<Todos>" Then
                res = res.Where(Function(v) v.Piso = _pisoSel).ToList()
            End If
            Return res
        Catch ex As Exception
            Logger.Error(ex, "Form_Graficos_Vigas.ObtenerVigas")
            Return New List(Of cViga)
        End Try
    End Function

    Private Function PisosDisponibles() As List(Of String)
        Try
            If Vigas Is Nothing Then Return New List(Of String)
            Return Vigas.Where(Function(v) Not String.IsNullOrEmpty(v.Piso)).
                         Select(Function(v) v.Piso).Distinct().OrderBy(Function(s) s).ToList()
        Catch
            Return New List(Of String)
        End Try
    End Function

    Private Function NombreViga(v As cViga) As String
        If Not String.IsNullOrWhiteSpace(v.NombrePlano) Then Return v.NombrePlano
        If Not String.IsNullOrWhiteSpace(v.Nombre) Then Return v.Nombre
        Return v.Name_Beam
    End Function

    Private Function DatosParaVista(v As Vista) As List(Of ItemCD)
        Dim res As New List(Of ItemCD)
        For Each via In ObtenerVigas()
            Dim etiq = NombreViga(via)
            Dim val As Double = -1
            Try
                Select Case v
                    Case Vista.FlexNeg : val = PeorFlexion(via, True)
                    Case Vista.FlexPos : val = PeorFlexion(via, False)
                    Case Vista.Cortante : val = PeorCortante(via)
                End Select
            Catch
            End Try
            res.Add(New ItemCD(etiq, val))
        Next
        Return res
    End Function

    Private Function PeorFlexion(via As cViga, negativo As Boolean) As Double
        Dim peor As Double = Double.MaxValue
        For Each fr In via.Frames
            If fr Is Nothing OrElse fr.RevisionFlexion Is Nothing Then Continue For
            For Each rf In fr.RevisionFlexion
                If rf Is Nothing OrElse rf.ResultadoActual Is Nothing Then Continue For
                Dim ratio = If(negativo, rf.ResultadoActual.RatioSup, rf.ResultadoActual.RatioInf)
                If ratio > 0 AndAlso ratio < peor Then peor = ratio
            Next
        Next
        Return If(peor = Double.MaxValue, -1.0, peor)
    End Function

    Private Function PeorCortante(via As cViga) As Double
        Dim peor As Double = Double.MaxValue
        For Each fr In via.Frames
            If fr Is Nothing OrElse fr.RevisionCortante Is Nothing Then Continue For
            For Each rc In fr.RevisionCortante
                If rc Is Nothing Then Continue For
                If rc.Factor > 0 AndAlso rc.phiVn > 0 AndAlso rc.Factor < peor Then peor = rc.Factor
            Next
        Next
        Return If(peor = Double.MaxValue, -1.0, peor)
    End Function

    Private Function ConteoEstado() As List(Of CategoriaConteo)
        Dim ok = 0, revF = 0, revC = 0, revAmbos = 0, sinC = 0
        For Each via In ObtenerVigas()
            Dim fn = PeorFlexion(via, True)
            Dim fp = PeorFlexion(via, False)
            Dim cort = PeorCortante(via)
            Dim fPeor = {fn, fp}.Where(Function(x) x > 0).DefaultIfEmpty(-1).Min()
            If fPeor <= 0 AndAlso cort <= 0 Then
                sinC += 1
                Continue For
            End If
            Dim okFlex = (fPeor > 0 AndAlso fPeor >= DashboardTheme.UMBRAL_CD)
            Dim okCort = (cort > 0 AndAlso cort >= DashboardTheme.UMBRAL_CD)
            If okFlex AndAlso okCort Then
                ok += 1
            ElseIf Not okFlex AndAlso Not okCort Then
                revAmbos += 1
            ElseIf Not okFlex Then
                revF += 1
            Else
                revC += 1
            End If
        Next
        Return New List(Of CategoriaConteo) From {
            New CategoriaConteo("Cumple", ok, _tema.Verde),
            New CategoriaConteo("Rev. flexión", revF, _tema.Ambar),
            New CategoriaConteo("Rev. cortante", revC, _tema.Rojo),
            New CategoriaConteo("Rev. ambas", revAmbos, _tema.Rojo),
            New CategoriaConteo("Sin cálculo", sinC, _tema.TextoTenue)}
    End Function

    Private Function CategoriasResumen() As List(Of CategoriaConteo)
        Dim nombres = {"M(-)", "M(+)", "Cort."}
        Dim vistas = {Vista.FlexNeg, Vista.FlexPos, Vista.Cortante}
        Dim res As New List(Of CategoriaConteo)
        For i = 0 To 2
            Dim items = DatosParaVista(vistas(i))
            Dim pos = items.Where(Function(x) x.Valor > 0).Select(Function(x) x.Valor).ToList()
            Dim prom = If(pos.Count = 0, 0.0, pos.Average())
            res.Add(New CategoriaConteo(nombres(i), CInt(prom * 100), _tema.ColorPorCD(prom)))
        Next
        Return res
    End Function

    Private Function TituloVista(v As Vista) As String
        Select Case v
            Case Vista.FlexNeg : Return "Flexión M(-)  ·  C/D = As_prov / As_req (superior), peor frame"
            Case Vista.FlexPos : Return "Flexión M(+)  ·  C/D = As_prov / As_req (inferior), peor frame"
            Case Else : Return "Cortante  ·  C/D = φVn / Vu, peor zona"
        End Select
    End Function

End Class
