''' <summary>
''' Dashboard premium del módulo de Pilas. Cuatro revisiones (Cargas al concreto,
''' Suelo, Cortante, Flexo-compresión) con la misma lógica de datos de siempre;
''' lo que cambió aquí es el look. Toggle Dark/Light compartido con los otros
''' módulos vía <see cref="DashboardTheme"/>; se propaga al vuelo sin reabrir.
''' </summary>
Public Class Form_Graficos_Pilas
    Inherits Form

    Private _tema As DashboardTheme = DashboardTheme.Actual

    Private _header As Panel
    Private _barraBotones As FlowLayoutPanel
    Private _cuerpo As TableLayoutPanel
    Private _vista As Vista = Vista.Cargas

    Private Enum Vista
        Cargas
        Suelo
        Cortante
        FlexoComp
    End Enum

    Public Sub New()
        Me.Text = "ARCO · Gráficas Resumen — Pilas"
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

    ' ─── Composición ───────────────────────────────────────────────────────
    Private Sub Reconstruir()
        Me.SuspendLayout()
        Me.Controls.Clear()

        _cuerpo = ArmarCuerpo()
        _barraBotones = ArmarBarraBotones()
        _header = ScottDashboardHelpers.CrearEncabezado(
            "DASHBOARD DE PILAS",
            "Capacidad de carga · Suelo · Cortante · Flexo-compresión",
            _tema)

        Me.Controls.Add(_cuerpo)
        Me.Controls.Add(_barraBotones)
        Me.Controls.Add(_header)

        Me.ResumeLayout()
    End Sub

    Private Function ArmarBarraBotones() As FlowLayoutPanel
        Dim f As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top, .Height = 48, .BackColor = _tema.Fondo,
            .Padding = New Padding(14, 8, 14, 4), .WrapContents = False}

        f.Controls.Add(BotonVista("Capacidad de Carga", Vista.Cargas))
        f.Controls.Add(BotonVista("Esfuerzos al Suelo", Vista.Suelo))
        f.Controls.Add(BotonVista("Cortante", Vista.Cortante))
        f.Controls.Add(BotonVista("Flexo-compresión", Vista.FlexoComp))

        Dim btnExport As New Button() With {
            .Text = "⤓  Exportar imagen",
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.0F),
            .ForeColor = _tema.Texto,
            .BackColor = _tema.PanelAlt,
            .Size = New Size(140, 32),
            .Cursor = Cursors.Hand,
            .Margin = New Padding(12, 2, 0, 0)}
        btnExport.FlatAppearance.BorderColor = _tema.Separador
        btnExport.FlatAppearance.BorderSize = 1
        AddHandler btnExport.Click, AddressOf Exportar_Click
        f.Controls.Add(btnExport)
        Return f
    End Function

    Private Function BotonVista(txt As String, v As Vista) As Button
        Dim activa = (v = _vista)
        Dim b As New Button() With {
            .Text = txt,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI Semibold", 9.0F),
            .ForeColor = If(activa, Color.White, _tema.Texto),
            .BackColor = If(activa, _tema.Acento, _tema.Panel),
            .Size = New Size(170, 32),
            .Cursor = Cursors.Hand,
            .Margin = New Padding(0, 2, 8, 0),
            .Tag = v}
        b.FlatAppearance.BorderColor = If(activa, _tema.Acento, _tema.Separador)
        b.FlatAppearance.BorderSize = 1
        AddHandler b.Click, Sub()
                                _vista = CType(b.Tag, Vista)
                                Reconstruir()
                            End Sub
        Return b
    End Function

    Private Function ArmarCuerpo() As TableLayoutPanel
        Dim items = DatosParaVista(_vista)
        Dim validos = items.Where(Function(i) i.Valor > 0).ToList()
        Dim sinCalc = items.Count - validos.Count
        Dim cumplen = validos.Where(Function(i) i.Valor >= DashboardTheme.UMBRAL_CD).Count()
        Dim noCumplen = validos.Count - cumplen
        Dim prom = If(validos.Count = 0, 0.0, validos.Average(Function(i) i.Valor))
        Dim peor = If(validos.Count = 0, 0.0, validos.Min(Function(i) i.Valor))

        Dim g As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill, .BackColor = _tema.Fondo,
            .ColumnCount = 4, .RowCount = 3, .Padding = New Padding(14)}
        g.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        g.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        g.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        g.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
        g.RowStyles.Add(New RowStyle(SizeType.Absolute, 108))
        g.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
        g.RowStyles.Add(New RowStyle(SizeType.Percent, 40))

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL PILAS", items.Count.ToString(), _tema.Acento, "evaluadas", _tema), 0, 0)
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

        Dim panelDonut = ScottDashboardHelpers.PanelConTitulo("Distribución de cumplimiento",
                                          ScottDashboardHelpers.DonutCumplimiento(cumplen, noCumplen, sinCalc, _tema),
                                          _tema)
        g.Controls.Add(panelDonut, 3, 1)

        Dim panelTop = ScottDashboardHelpers.PanelConTitulo("Top 10 peores",
                                        ScottDashboardHelpers.TopPeores(validos, 10, _tema),
                                        _tema)
        g.Controls.Add(panelTop, 0, 2)
        g.SetColumnSpan(panelTop, 2)

        Dim panelResumen = ScottDashboardHelpers.PanelConTitulo("Resumen por revisión",
                                            ScottDashboardHelpers.BarrasCategorias(CategoriasResumen(), _tema, "C/D promedio"),
                                            _tema)
        g.Controls.Add(panelResumen, 2, 2)
        g.SetColumnSpan(panelResumen, 2)

        Return g
    End Function

    ' ─── Datos (misma lógica que el dashboard anterior) ────────────────────
    Private Function TituloVista(v As Vista) As String
        Select Case v
            Case Vista.Cargas : Return "Capacidad de Carga del Concreto (C/D)  ·  mínimo Ch1…Ch4"
            Case Vista.Suelo : Return "Esfuerzos Transmitidos al Suelo (C/D)"
            Case Vista.Cortante : Return "Cortante (C/D) = φVn / Vu"
            Case Else : Return "Flexo-compresión (C/D)  ·  diagrama de interacción"
        End Select
    End Function

    Private Function ObtenerPilas() As List(Of Elemento_Pila)
        Try
            Dim p = Form_01_PagPilas.Proyecto
            If p Is Nothing OrElse p.Elementos Is Nothing OrElse p.Elementos.Pilas Is Nothing Then
                Return New List(Of Elemento_Pila)
            End If
            Return If(p.Elementos.Pilas.ListaElementos, New List(Of Elemento_Pila))
        Catch ex As Exception
            Logger.Error(ex, "Form_Graficos_Pilas.ObtenerPilas")
            Return New List(Of Elemento_Pila)
        End Try
    End Function

    Private Function EtiquetaPila(p As Elemento_Pila) As String
        If Not String.IsNullOrWhiteSpace(p.Name_Label) Then Return p.Name_Label
        Return p.Name_Elemento
    End Function

    Private Function MinPos(vals As Single()) As Double
        Dim pos = vals.Where(Function(v) v > 0).Select(Function(v) CDbl(v)).ToArray()
        If pos.Length = 0 Then Return -1
        Return pos.Min()
    End Function

    Private Function DatosParaVista(v As Vista) As List(Of ItemCD)
        Dim pilas = ObtenerPilas()
        Dim res As New List(Of ItemCD)
        For Each p In pilas
            Select Case v
                Case Vista.Cargas
                    Dim cd = MinPos({p.Check1_PsE, p.Check2_PsD, p.Check3_PuE, p.Check4_PuD})
                    res.Add(New ItemCD(EtiquetaPila(p), cd))
                Case Vista.Suelo
                    Dim cd = MinPos({CSng(p.Relacion_EsfE), CSng(p.Relacion_EsfD)})
                    res.Add(New ItemCD(EtiquetaPila(p), cd))
                Case Vista.Cortante
                    Dim cd = If(p.FactorShear > 0, CDbl(p.FactorShear), -1)
                    res.Add(New ItemCD(EtiquetaPila(p), cd))
                Case Else
                    Dim cd = MinPos({p.F_Diagonal_Efectivo, p.F_CortesH_Efectivo})
                    res.Add(New ItemCD(EtiquetaPila(p), cd))
            End Select
        Next
        Return res
    End Function

    Private Function CategoriasResumen() As List(Of CategoriaConteo)
        Dim nombres = {"Cargas", "Suelo", "Cortante", "Flexo"}
        Dim vistas = {Vista.Cargas, Vista.Suelo, Vista.Cortante, Vista.FlexoComp}
        Dim res As New List(Of CategoriaConteo)
        For i = 0 To 3
            Dim items = DatosParaVista(vistas(i))
            Dim pos = items.Where(Function(x) x.Valor > 0).Select(Function(x) x.Valor).ToList()
            Dim prom = If(pos.Count = 0, 0.0, pos.Average())
            res.Add(New CategoriaConteo(nombres(i), CInt(prom * 100), _tema.ColorPorCD(prom)))
        Next
        Return res
    End Function

    ' ─── Exportar ──────────────────────────────────────────────────────────
    Private Sub Exportar_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog() With {
            .Filter = "PNG|*.png", .FileName = "Dashboard_Pilas_" & _vista.ToString() & ".png"}
            If sfd.ShowDialog() = DialogResult.OK Then
                Try
                    Using bmp As New Bitmap(_cuerpo.ClientSize.Width, _cuerpo.ClientSize.Height)
                        _cuerpo.DrawToBitmap(bmp, New Rectangle(Point.Empty, bmp.Size))
                        bmp.Save(sfd.FileName, Imaging.ImageFormat.Png)
                    End Using
                    MessageBox.Show("Imagen exportada.", "ARCO")
                Catch ex As Exception
                    Logger.Error(ex, "Form_Graficos_Pilas.Exportar_Click")
                End Try
            End If
        End Using
    End Sub

End Class
