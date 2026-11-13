''' <summary>
''' Dashboard premium del módulo de Columnas — ALR, Flexo-compresión, Cortante
''' y Confinamiento NSR-10. Reemplaza el Chart viejo por ScottPlot + tema
''' Dark/Light compartido. El Designer.vb anterior sigue existiendo con sus
''' campos, pero no se usan: en Load limpiamos Me.Controls y construimos el
''' layout premium code-only.
''' </summary>
Public Class Form_Graficos

    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Private _tema As DashboardTheme = DashboardTheme.Actual
    Private _vista As Vista = Vista.Flexo

    Private Enum Vista
        ALR
        Flexo
        Cortante
        Confinamiento
    End Enum

    Private Sub Form_Graficos_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        Proyecto = Form_00_PaginaPrincipal.proyecto
    End Sub

    Private Sub Form_Graficos_Load(sender As Object, e As EventArgs) Handles Me.Load
        Me.Text = "ARCO · Gráficas Resumen — Columnas"
        Me.Size = New Size(1480, 920)
        Me.MinimumSize = New Size(1100, 680)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Segoe UI", 9.5F)
        Me.BackColor = _tema.Fondo
        Me.MenuStrip1.Visible = False  ' Designer dejó el MenuStrip; lo ocultamos

        AddHandler DashboardTheme.TemaCambiado, AddressOf OnTemaCambiado
        AddHandler Me.FormClosed, Sub() RemoveHandler DashboardTheme.TemaCambiado, AddressOf OnTemaCambiado

        Reconstruir()
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

        Dim cuerpo = ArmarCuerpo()
        Dim barraBotones = ArmarBarraBotones()
        Dim header = ScottDashboardHelpers.CrearEncabezado(
            "DASHBOARD DE COLUMNAS",
            "ALR · Flexo-compresión · Cortante · Confinamiento NSR-10",
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

        f.Controls.Add(BotonVista("ALR", Vista.ALR))
        f.Controls.Add(BotonVista("Flexo-compresión", Vista.Flexo))
        f.Controls.Add(BotonVista("Cortante", Vista.Cortante))
        f.Controls.Add(BotonVista("Confinamiento", Vista.Confinamiento))
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
        Dim validos = items.Where(Function(i) i.Valor >= 0).ToList()
        Dim sinCalc = items.Count - validos.Count
        Dim cumplen As Integer
        Dim noCumplen As Integer
        Dim prom As Double
        Dim peor As Double
        Dim labelPeor As String

        If _vista = Vista.ALR Then
            Dim conValor = validos.Where(Function(i) i.Valor > 0).ToList()
            cumplen = conValor.Where(Function(i) i.Valor <= 0.3).Count()
            noCumplen = conValor.Count - cumplen
            prom = If(conValor.Count = 0, 0.0, conValor.Average(Function(i) i.Valor))
            peor = If(conValor.Count = 0, 0.0, conValor.Max(Function(i) i.Valor))
            labelPeor = "máx: " & peor.ToString("F3")
        Else
            Dim conValor = validos.Where(Function(i) i.Valor > 0).ToList()
            cumplen = conValor.Where(Function(i) i.Valor >= DashboardTheme.UMBRAL_CD).Count()
            noCumplen = conValor.Count - cumplen
            prom = If(conValor.Count = 0, 0.0, conValor.Average(Function(i) i.Valor))
            peor = If(conValor.Count = 0, 0.0, conValor.Min(Function(i) i.Valor))
            labelPeor = "peor: " & peor.ToString("F2")
        End If

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

        Dim txtCumple As String = If(_vista = Vista.ALR, "ALR ≤ 0.30", "C/D ≥ " & DashboardTheme.UMBRAL_CD.ToString("F2"))

        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("TOTAL COLUMNAS", items.Count.ToString(), _tema.Acento, "evaluadas", _tema), 0, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("CUMPLEN", cumplen.ToString(), _tema.Verde, txtCumple, _tema), 1, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi("NO CUMPLEN", noCumplen.ToString(), _tema.Rojo, "requieren revisión", _tema), 2, 0)
        g.Controls.Add(ScottDashboardHelpers.TarjetaKpi(If(_vista = Vista.ALR, "ALR PROMEDIO", "C/D PROMEDIO"),
                                                        prom.ToString(If(_vista = Vista.ALR, "F3", "F2")),
                                                        If(_vista = Vista.ALR, If(prom <= 0.3, _tema.Verde, _tema.Ambar),
                                                                               If(prom >= DashboardTheme.UMBRAL_CD, _tema.Verde, _tema.Ambar)),
                                                        labelPeor, _tema), 3, 0)

        Dim principal As ScottPlot.FormsPlot
        If _vista = Vista.ALR Then
            principal = ScottDashboardHelpers.BarrasLimiteSuperior(validos, 0.3, _tema, "ALR = P/(fc·Ag)")
        Else
            principal = ScottDashboardHelpers.BarrasCDVertical(validos, _tema, "C/D")
        End If
        Dim panelBarras = ScottDashboardHelpers.PanelConTitulo(TituloVista(_vista), principal, _tema)
        g.Controls.Add(panelBarras, 0, 1)
        g.SetColumnSpan(panelBarras, 3)

        Dim panelDonut = ScottDashboardHelpers.PanelConTitulo("Distribución de cumplimiento",
                                                               ScottDashboardHelpers.DonutCumplimiento(cumplen, noCumplen, sinCalc, _tema),
                                                               _tema)
        g.Controls.Add(panelDonut, 3, 1)

        Dim topPlot As ScottPlot.FormsPlot
        If _vista = Vista.ALR Then
            topPlot = ScottDashboardHelpers.TopPeores(validos, 10, _tema, menorEsMejor:=True, umbral:=0.3, formato:="F3")
        Else
            topPlot = ScottDashboardHelpers.TopPeores(validos, 10, _tema)
        End If
        Dim panelTop = ScottDashboardHelpers.PanelConTitulo("Top 10 peores", topPlot, _tema)
        g.Controls.Add(panelTop, 0, 2)
        g.SetColumnSpan(panelTop, 2)

        Dim panelResumen = ScottDashboardHelpers.PanelConTitulo("Resumen por revisión",
                                                                  ScottDashboardHelpers.BarrasCategorias(CategoriasResumen(), _tema, "C/D promedio × 100"),
                                                                  _tema)
        g.Controls.Add(panelResumen, 2, 2)
        g.SetColumnSpan(panelResumen, 2)

        Return g
    End Function

    Private Function TituloVista(v As Vista) As String
        Select Case v
            Case Vista.ALR : Return "Carga Axial Relativa (ALR)  ·  P / (fc · Ag), límite 0.30"
            Case Vista.Flexo : Return "Flexo-compresión (C/D)  ·  diagrama de interacción"
            Case Vista.Cortante : Return "Cortante (C/D)  ·  mínimo entre V2 y V3"
            Case Else : Return "Confinamiento NSR-10 (C/D)  ·  mínimo Ash largo/corto por tramo"
        End Select
    End Function

    ' ─── Datos ─────────────────────────────────────────────────────────────
    Private Function ObtenerColumnas() As List(Of Columna)
        Try
            If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse Proyecto.Elementos.Columnas Is Nothing Then
                Return New List(Of Columna)
            End If
            Dim lst = Proyecto.Elementos.Columnas.Lista_Columnas
            If lst Is Nothing Then Return New List(Of Columna)
            Return lst.Where(Function(c) c IsNot Nothing AndAlso c.Ref_Modificado).ToList()
        Catch ex As Exception
            Logger.Error(ex, "Form_Graficos.ObtenerColumnas")
            Return New List(Of Columna)
        End Try
    End Function

    Private Function DatosParaVista(v As Vista) As List(Of ItemCD)
        Dim cols = ObtenerColumnas()
        Dim res As New List(Of ItemCD)

        For Each col In cols
            Dim nombre = col.Name_Elemento
            Select Case v
                Case Vista.ALR
                    Dim alr = AlrMaximo(col)
                    res.Add(New ItemCD(nombre, alr))
                Case Vista.Flexo
                    ' Envolvente Bresler ∪ Rel. aceros ETABS. Una columna cumple
                    ' si cualquiera de los dos criterios pasa (ColumnaService.CDFlexoEnvolvente).
                    ' Antes se leía Lista_F(0) que es solo el biaxial — por eso
                    ' una columna con biaxial 0.88 y aceros 0.90 aparecía como
                    ' no cumple, cuando por envolvente sí cumple.
                    Dim f = ColumnaService.PeorCDFlexoColumna(col)
                    res.Add(New ItemCD(nombre, If(f > 0, CDbl(f), -1.0)))
                Case Vista.Cortante
                    Dim fV2 = SafeF(col, 1)
                    Dim fV3 = SafeF(col, 2)
                    Dim arr = {fV2, fV3}.Where(Function(x) x > 0).ToArray()
                    res.Add(New ItemCD(nombre, If(arr.Length = 0, -1.0, CDbl(arr.Min()))))
                Case Else
                    Dim fMin = Single.MaxValue
                    If col.Lista_Tramos_Columnas IsNot Nothing Then
                        For Each tr In col.Lista_Tramos_Columnas
                            If tr Is Nothing Then Continue For
                            If tr.F_Ash_Largo > 0 AndAlso tr.F_Ash_Largo < fMin Then fMin = tr.F_Ash_Largo
                            If tr.F_Ash_Corto > 0 AndAlso tr.F_Ash_Corto < fMin Then fMin = tr.F_Ash_Corto
                        Next
                    End If
                    res.Add(New ItemCD(nombre, If(fMin = Single.MaxValue, -1.0, CDbl(fMin))))
            End Select
        Next
        Return res
    End Function

    Private Function AlrMaximo(col As Columna) As Double
        Try
            If col.Lista_ALR IsNot Nothing AndAlso col.Lista_ALR.Count > 0 Then
                Return col.Lista_ALR.Max(Function(a) CDbl(a.ALR))
            End If
            If col.Lista_Tramos_Columnas IsNot Nothing Then
                Dim mejor = 0.0
                For Each tr In col.Lista_Tramos_Columnas
                    If tr Is Nothing OrElse tr.Lista_Combinaciones Is Nothing Then Continue For
                    If tr.fc <= 0 OrElse tr.B_Plano <= 0 OrElse tr.H_Plano <= 0 Then Continue For
                    Dim ag = tr.B_Plano * tr.H_Plano
                    For Each cc In tr.Lista_Combinaciones
                        If cc Is Nothing Then Continue For
                        Dim p = Math.Abs(cc.P)
                        Dim alr = p / (tr.fc * ag * 1000.0)
                        If alr > mejor Then mejor = alr
                    Next
                Next
                Return mejor
            End If
        Catch
        End Try
        Return -1
    End Function

    Private Function SafeF(col As Columna, idx As Integer) As Single
        Try
            If col.Lista_F IsNot Nothing AndAlso col.Lista_F.Count > idx Then Return col.Lista_F(idx)
        Catch
        End Try
        Return 0
    End Function

    Private Function CategoriasResumen() As List(Of CategoriaConteo)
        Dim nombres = {"ALR", "Flexo", "Cort.", "Confin."}
        Dim vistas = {Vista.ALR, Vista.Flexo, Vista.Cortante, Vista.Confinamiento}
        Dim res As New List(Of CategoriaConteo)
        For i = 0 To 3
            Dim items = DatosParaVista(vistas(i))
            Dim pos = items.Where(Function(x) x.Valor > 0).Select(Function(x) x.Valor).ToList()
            If vistas(i) = Vista.ALR Then
                Dim alrProm = If(pos.Count = 0, 0.0, pos.Average())
                Dim col = If(alrProm <= 0.3, _tema.Verde, If(alrProm <= 0.33, _tema.Ambar, _tema.Rojo))
                res.Add(New CategoriaConteo(nombres(i), CInt(alrProm * 1000) / 10, col))
            Else
                Dim prom = If(pos.Count = 0, 0.0, pos.Average())
                res.Add(New CategoriaConteo(nombres(i), CInt(prom * 100), _tema.ColorPorCD(prom)))
            End If
        Next
        Return res
    End Function

End Class
