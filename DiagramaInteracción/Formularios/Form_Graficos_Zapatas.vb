Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores

''' <summary>
''' Dashboard de gráficas resumen del módulo de Zapatas. Completa la réplica del
''' patrón a los seis módulos calculables (Columnas, Pilas, Muros, Vigas, Nervios,
''' Zapatas).
'''
''' No recalcula nada: lee las relaciones C/D que ya deja el cálculo del módulo
''' a través de <see cref="ZapataService.FactoresPorCombinacion"/> y
''' <see cref="ZapataService.Resumir"/>, con los mismos criterios que usa el
''' reporte, para que gráfica y reporte no se contradigan.
'''
''' A diferencia de los otros dashboards, aquí se muestran cinco gráficas C/D en
''' un TableLayoutPanel al mismo tiempo (peor global, suelo, excentricidad,
''' punzonamiento, cortante+flexión) más dos gráficas de conteo (tipo de apoyo,
''' cumple/no cumple). Es útil ver todas las revisiones a la vez porque en una
''' zapata una revisión típicamente hala más que otra según la posición.
''' </summary>
Public Class Form_Graficos_Zapatas
    Inherits Form

    ''' <summary>Fuente de datos. La setea quien abre el formulario.</summary>
    Public Property Zapatas As List(Of cZapata)

    Private ReadOnly _cboTipo As ComboBox
    Private ReadOnly _chkSoloNoCumple As CheckBox
    Private ReadOnly _btnActualizar As Button
    Private ReadOnly _btnExportar As Button
    Private ReadOnly _cboExport As ComboBox

    Private ReadOnly _chPeor As Chart
    Private ReadOnly _chSuelo As Chart
    Private ReadOnly _chExc As Chart
    Private ReadOnly _chPun As Chart
    Private ReadOnly _chCorFlex As Chart
    Private ReadOnly _chTipo As Chart
    Private ReadOnly _chCumple As Chart

    Private _cargando As Boolean = False

    Public Sub New()

        Me.Text = "Gráficas Resumen — Zapatas"
        Me.Size = New Size(1400, 800)
        Me.MinimumSize = New Size(1050, 620)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        ' Barra superior con filtros y botones
        Dim barra As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 48,
            .BackColor = Color.FromArgb(245, 245, 245)
        }

        Dim lblTipo As New Label() With {
            .Text = "Filtrar por tipo de apoyo:",
            .AutoSize = True,
            .Location = New Point(12, 16),
            .ForeColor = GraficosResumen.ColAcento,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        }
        _cboTipo = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Location = New Point(170, 12),
            .Width = 170,
            .Font = New Font("Segoe UI", 9.5F)
        }
        _cboTipo.Items.AddRange(New Object() {"Todos", "Centrales", "Medianeras", "Esquineras"})
        _cboTipo.SelectedIndex = 0

        _chkSoloNoCumple = New CheckBox() With {
            .Text = "Solo las que no cumplen",
            .Location = New Point(360, 14),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = GraficosResumen.ColTexto
        }

        _btnActualizar = New Button() With {
            .Text = "Actualizar",
            .Location = New Point(560, 10),
            .Size = New Size(110, 28),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = GraficosResumen.ColAcento,
            .ForeColor = Color.White,
            .UseVisualStyleBackColor = False,
            .Cursor = Cursors.Hand
        }
        _btnActualizar.FlatAppearance.BorderColor = Color.FromArgb(150, 150, 150)

        Dim lblExp As New Label() With {
            .Text = "Exportar:",
            .AutoSize = True,
            .Location = New Point(690, 16),
            .ForeColor = GraficosResumen.ColAcento,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        }
        _cboExport = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Location = New Point(755, 12),
            .Width = 210,
            .Font = New Font("Segoe UI", 9.5F)
        }
        _cboExport.Items.AddRange(New Object() {
            "1. Peor C/D",
            "2. Suelo",
            "3. Excentricidad",
            "4. Punzonamiento",
            "5. Cortante y Flexión",
            "6. Conteo por tipo de apoyo",
            "7. Cumple vs. No cumple"
        })
        _cboExport.SelectedIndex = 0

        _btnExportar = New Button() With {
            .Text = "Exportar PNG",
            .Location = New Point(975, 10),
            .Size = New Size(120, 28),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .BackColor = GraficosResumen.ColAcentoClaro,
            .ForeColor = GraficosResumen.ColAcento,
            .UseVisualStyleBackColor = False,
            .Cursor = Cursors.Hand
        }
        _btnExportar.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200)

        barra.Controls.Add(lblTipo)
        barra.Controls.Add(_cboTipo)
        barra.Controls.Add(_chkSoloNoCumple)
        barra.Controls.Add(_btnActualizar)
        barra.Controls.Add(lblExp)
        barra.Controls.Add(_cboExport)
        barra.Controls.Add(_btnExportar)

        ' TableLayoutPanel: 4 filas x 2 columnas. Últimos dos slots para conteos.
        Dim grid As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 4,
            .BackColor = Color.White,
            .Padding = New Padding(4)
        }
        grid.ColumnStyles.Clear()
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        grid.RowStyles.Clear()
        For i = 0 To 3
            grid.RowStyles.Add(New RowStyle(SizeType.Percent, 25.0F))
        Next

        _chPeor = GraficosResumen.CrearChart()
        _chSuelo = GraficosResumen.CrearChart()
        _chExc = GraficosResumen.CrearChart()
        _chPun = GraficosResumen.CrearChart()
        _chCorFlex = GraficosResumen.CrearChart()
        _chTipo = GraficosResumen.CrearChart()
        _chCumple = GraficosResumen.CrearChart()

        grid.Controls.Add(_chPeor, 0, 0)
        grid.Controls.Add(_chSuelo, 1, 0)
        grid.Controls.Add(_chExc, 0, 1)
        grid.Controls.Add(_chPun, 1, 1)
        grid.Controls.Add(_chCorFlex, 0, 2)
        grid.SetColumnSpan(_chCorFlex, 2)
        grid.Controls.Add(_chTipo, 0, 3)
        grid.Controls.Add(_chCumple, 1, 3)

        Me.Controls.Add(grid)
        Me.Controls.Add(barra)

        AddHandler Me.Load, AddressOf Form_Load
        AddHandler _btnActualizar.Click, AddressOf Actualizar_Click
        AddHandler _btnExportar.Click, AddressOf Exportar_Click
        AddHandler _cboTipo.SelectedIndexChanged, AddressOf Filtro_Changed
        AddHandler _chkSoloNoCumple.CheckedChanged, AddressOf Filtro_Changed

    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)

        Try
            PilaVerticalAdaptable.AjustarAPantallaConScroll(Me)
        Catch
            ' El ajuste es opcional; si algo falla no impide ver las gráficas.
        End Try

        If Zapatas Is Nothing OrElse Zapatas.Count = 0 Then
            MessageBox.Show("Primero calcule las zapatas antes de abrir las gráficas.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Redibujar()
    End Sub

    ' =====================================================================
    ' Filtrado y utilidades
    ' =====================================================================

    Private Function ZapatasFiltradas() As List(Of cZapata)

        If Zapatas Is Nothing Then Return New List(Of cZapata)

        Dim lista = Zapatas.Where(Function(z) z IsNot Nothing).ToList()

        Select Case _cboTipo.SelectedIndex
            Case 1 : lista = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central).ToList()
            Case 2 : lista = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera).ToList()
            Case 3 : lista = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera).ToList()
        End Select

        If _chkSoloNoCumple.Checked Then
            lista = lista.Where(Function(z)
                                    Dim r = ZapataService.Resumir(z)
                                    Return r.TieneResultados AndAlso Not r.Cumple
                                End Function).ToList()
        End If

        Return lista
    End Function

    ''' <summary>Etiqueta del eje X: nombre si lo hay, si no el joint.</summary>
    Private Shared Function Etiqueta(z As cZapata) As String
        If Not String.IsNullOrWhiteSpace(z.Nombre) Then Return z.Nombre
        If Not String.IsNullOrWhiteSpace(z.Label_joint) Then Return z.Label_joint
        Return "?"
    End Function

    ''' <summary>
    ''' Mínimo positivo entre todas las combinaciones para el selector dado.
    ''' Cero significa "esta revisión no aplica en esa combinación" y no aporta al
    ''' mínimo. Si la zapata no tiene combinaciones calculadas o todas están en
    ''' cero, devuelve -1 para que <see cref="GraficosResumen.DibujarBarrasCD"/>
    ''' omita la barra en lugar de dibujarla como no-cumple.
    ''' </summary>
    Private Shared Function MinRevision(z As cZapata,
                                        selector As Func(Of ZapataService.FactoresCombinacion, Double)) As Double

        Dim factores = ZapataService.FactoresPorCombinacion(z)
        If factores Is Nothing OrElse factores.Count = 0 Then Return -1

        Dim menor As Double = Double.MaxValue
        For Each f In factores
            Dim v = selector(f)
            If v > 0 AndAlso v < menor Then menor = v
        Next

        Return If(menor = Double.MaxValue, -1, menor)
    End Function

    ''' <summary>
    ''' Mínimo positivo del cortante y de la flexión combinados en una sola
    ''' métrica ("cortante y flexión, lo que gobierne").
    ''' </summary>
    Private Shared Function MinCortanteFlexion(z As cZapata) As Double

        Dim factores = ZapataService.FactoresPorCombinacion(z)
        If factores Is Nothing OrElse factores.Count = 0 Then Return -1

        Dim menor As Double = Double.MaxValue
        For Each f In factores
            If f.Cortante > 0 AndAlso f.Cortante < menor Then menor = f.Cortante
            If f.Flexion > 0 AndAlso f.Flexion < menor Then menor = f.Flexion
        Next

        Return If(menor = Double.MaxValue, -1, menor)
    End Function

    ' =====================================================================
    ' Constructores de cada gráfica
    ' =====================================================================

    Private Sub GraficarPeor(lista As List(Of cZapata))

        Dim items As New List(Of GraficosResumen.ItemCD)

        ' Orden ascendente por peor factor (críticas primero). Sin cálculo al final.
        Dim ordenadas = lista.
            Select(Function(z) New With {.Z = z, .R = ZapataService.Resumir(z)}).
            OrderBy(Function(x) If(x.R.TieneResultados AndAlso x.R.PeorFactor > 0, x.R.PeorFactor, Double.MaxValue)).
            ToList()

        For Each x In ordenadas
            Dim v As Double = -1
            Dim tip As String = "Tipo: " & ZapataService.NombreTipo(x.Z.TipoApoyo)
            If x.R.TieneResultados AndAlso x.R.PeorFactor > 0 Then
                v = x.R.PeorFactor
                tip &= vbCrLf & "Revisión crítica: " & x.R.Revision
                tip &= vbCrLf & "Combinación: " & x.R.Combinacion
            End If
            items.Add(New GraficosResumen.ItemCD(Etiqueta(x.Z), v, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_chPeor, items,
                                        "Peor C/D por zapata",
                                        "C/D global (mínimo)",
                                        "Zapata")
    End Sub

    Private Sub GraficarRevision(chart As Chart,
                                 lista As List(Of cZapata),
                                 titulo As String,
                                 tituloY As String,
                                 selector As Func(Of ZapataService.FactoresCombinacion, Double))

        Dim items As New List(Of GraficosResumen.ItemCD)
        For Each z In lista
            Dim v = MinRevision(z, selector)
            Dim tip As String = "Tipo: " & ZapataService.NombreTipo(z.TipoApoyo)
            items.Add(New GraficosResumen.ItemCD(Etiqueta(z), v, tip))
        Next

        GraficosResumen.DibujarBarrasCD(chart, items, titulo, tituloY, "Zapata")
    End Sub

    Private Sub GraficarCorFlex(lista As List(Of cZapata))

        Dim items As New List(Of GraficosResumen.ItemCD)
        For Each z In lista
            Dim v = MinCortanteFlexion(z)
            Dim tip As String = "Tipo: " & ZapataService.NombreTipo(z.TipoApoyo)
            items.Add(New GraficosResumen.ItemCD(Etiqueta(z), v, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_chCorFlex, items,
                                        "Cortante y Flexión (mín. de ambos)",
                                        "C/D = mín(cortante, flexión)",
                                        "Zapata")
    End Sub

    Private Sub GraficarTipo(lista As List(Of cZapata))

        ' Enumerable.Count con lambda queda ambiguo frente a la propiedad Count
        ' de List(Of T) en VB.NET; se cuenta filtrando primero y contando después.
        Dim nCen As Integer = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central).Count()
        Dim nMed As Integer = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera).Count()
        Dim nEsq As Integer = lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera).Count()

        Dim cats As New List(Of String) From {"Central", "Medianera", "Esquinera"}
        Dim conts As New List(Of Integer) From {nCen, nMed, nEsq}
        Dim cols As New List(Of Color) From {
            GraficosResumen.ColNeutro,
            GraficosResumen.ColAcento,
            GraficosResumen.ColGris
        }

        GraficosResumen.DibujarBarrasCategorias(_chTipo, cats, conts, cols,
                                                "Conteo por tipo de apoyo",
                                                "Cantidad de zapatas",
                                                "Tipo de apoyo")
    End Sub

    Private Sub GraficarCumple(lista As List(Of cZapata))

        Dim nOK As Integer = 0
        Dim nMal As Integer = 0
        Dim nSin As Integer = 0

        For Each z In lista
            Dim r = ZapataService.Resumir(z)
            If Not r.TieneResultados OrElse r.PeorFactor <= 0 Then
                nSin += 1
            ElseIf r.Cumple Then
                nOK += 1
            Else
                nMal += 1
            End If
        Next

        Dim cats As New List(Of String) From {"Cumple", "No cumple", "Sin calcular"}
        Dim conts As New List(Of Integer) From {nOK, nMal, nSin}
        Dim cols As New List(Of Color) From {
            GraficosResumen.ColVerde,
            GraficosResumen.ColRojo,
            GraficosResumen.ColGris
        }

        GraficosResumen.DibujarBarrasCategorias(_chCumple, cats, conts, cols,
                                                "Cumple vs. No cumple",
                                                "Cantidad de zapatas",
                                                "Estado")
    End Sub

    ' =====================================================================
    ' Orquestación
    ' =====================================================================

    Private Sub Redibujar()

        If _cargando Then Return
        _cargando = True

        Try
            Dim lista = ZapatasFiltradas()
            If lista.Count = 0 Then
                LimpiarTodos()
                Return
            End If

            GraficarPeor(lista)
            GraficarRevision(_chSuelo, lista,
                             "Suelo (mín. combinaciones)",
                             "C/D = qAdm / qMax",
                             Function(f) f.Suelo)
            GraficarRevision(_chExc, lista,
                             "Excentricidad (mín. combinaciones)",
                             "C/D = e_lim / e",
                             Function(f) f.Excentricidad)
            GraficarRevision(_chPun, lista,
                             "Punzonamiento (mín. combinaciones)",
                             "C/D = φVc / Vu",
                             Function(f) f.Punzonamiento)
            GraficarCorFlex(lista)
            GraficarTipo(lista)
            GraficarCumple(lista)

        Catch ex As Exception
            Logger.Error(ex, "Form_Graficos_Zapatas.Redibujar")
            MessageBox.Show("No se pudieron actualizar las gráficas." & vbCrLf & ex.Message,
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Finally
            _cargando = False
        End Try
    End Sub

    Private Sub LimpiarTodos()
        For Each ch In New Chart() {_chPeor, _chSuelo, _chExc, _chPun, _chCorFlex, _chTipo, _chCumple}
            ch.Series.Clear()
            ch.Titles.Clear()
        Next
    End Sub

    ' =====================================================================
    ' Eventos
    ' =====================================================================

    Private Sub Filtro_Changed(sender As Object, e As EventArgs)
        Redibujar()
    End Sub

    Private Sub Actualizar_Click(sender As Object, e As EventArgs)
        Redibujar()
    End Sub

    Private Sub Exportar_Click(sender As Object, e As EventArgs)

        Dim ch As Chart = Nothing
        Dim nombre As String = "Grafico_Zapatas"
        Select Case _cboExport.SelectedIndex
            Case 0 : ch = _chPeor : nombre = "Zapatas_PeorCD"
            Case 1 : ch = _chSuelo : nombre = "Zapatas_Suelo"
            Case 2 : ch = _chExc : nombre = "Zapatas_Excentricidad"
            Case 3 : ch = _chPun : nombre = "Zapatas_Punzonamiento"
            Case 4 : ch = _chCorFlex : nombre = "Zapatas_CortanteFlexion"
            Case 5 : ch = _chTipo : nombre = "Zapatas_TipoApoyo"
            Case 6 : ch = _chCumple : nombre = "Zapatas_CumpleNoCumple"
        End Select

        If ch IsNot Nothing Then GraficosResumen.ExportarImagen(ch, nombre)
    End Sub

End Class
