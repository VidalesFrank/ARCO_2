Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports ARCO.eNumeradores

' =============================================================================
' Form_07_Zapata_Detalle
'
' Vista de análisis fino de una zapata. Se abre desde la planta interactiva
' (doble clic) o desde el menú "Ver" del módulo. El objetivo es ver de forma
' gráfica, para una zapata concreta y bajo la combinación seleccionada:
'
'   1) La distribución de presiones bajo la huella (heatmap o cuadrícula 10x10)
'   2) Las 5 relaciones C/D como tarjetas de capacidad vs demanda
'   3) La tabla envolvente con todas las combinaciones y sus C/D
'   4) Las secciones críticas dibujadas sobre la planta esquemática de la zapata
'
' Todos los valores vienen del ResultadoZapata almacenado en z.Resultados y de
' ZapataService.FactoresPorCombinacion, no se recalcula nada aquí. La única
' operación numérica es recuperar Mx, My a partir de ex, ey, P_Efectivo para
' poder dibujar el plano de presiones q(x,y).
' =============================================================================
Public Class Form_07_Zapata_Detalle

    Private _zapata As cZapata
    Private _factores As List(Of ZapataService.FactoresCombinacion)
    Private _resumen As ZapataService.ResumenZapata
    Private _combosDinamicas As HashSet(Of String)
    Private _actualizando As Boolean = False

    ' Panel de dibujo de presiones (Tab 1)
    Private _pnlPresiones As PanelDobleBuffer
    Private _pnlPresionesInfo As Panel
    Private _rdoHeatmap As RadioButton
    Private _rdoGrilla As RadioButton

    ' Contenedor de tarjetas (Tab 2)
    Private _flowTarjetas As FlowLayoutPanel

    ' Grid envolvente (Tab 3)
    Private _dgvEnvolvente As DataGridView

    ' Panel Tab 4
    Private _pnlSecciones As PanelDobleBuffer

    Private Shared ReadOnly ColorARCO As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly ColorCumple As Color = Color.FromArgb(198, 239, 206)
    Private Shared ReadOnly ColorCumpleTexto As Color = Color.FromArgb(0, 97, 0)
    Private Shared ReadOnly ColorMal As Color = Color.FromArgb(255, 199, 206)
    Private Shared ReadOnly ColorMalTexto As Color = Color.FromArgb(156, 0, 6)

    ''' <summary>Constructor por defecto, para poder instanciar desde el diseñador.</summary>
    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>Constructor con la zapata a mostrar.</summary>
    Public Sub New(z As cZapata)
        InitializeComponent()
        _zapata = z
    End Sub

    ''' <summary>Punto de entrada desde otros formularios.</summary>
    Public Shared Sub Mostrar(z As cZapata, Optional owner As Form = Nothing)
        If z Is Nothing Then
            MessageBox.Show("No hay zapata seleccionada.",
                            "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Try
            Dim f As New Form_07_Zapata_Detalle(z)
            If owner IsNot Nothing Then
                f.Show(owner)
            Else
                f.Show()
            End If
        Catch ex As Exception
            Logger.Error(ex, "Form_07_Zapata_Detalle.Mostrar", z.Label_joint)
        End Try
    End Sub

    Private Sub Form_07_Zapata_Detalle_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Try
            PilaVerticalAdaptable.AjustarAPantallaConScroll(Me)
        Catch
        End Try

        If _zapata Is Nothing Then
            LblTitulo.Text = "Sin zapata"
            LblSubtitulo.Text = "No se recibió una zapata para mostrar."
            LblEstadoGeneral.Text = ""
            Return
        End If

        If _zapata.Resultados Is Nothing OrElse _zapata.Resultados.Count = 0 Then
            LblTitulo.Text = _zapata.Label_joint
            LblSubtitulo.Text = "Esta zapata no se ha calculado."
            LblEstadoGeneral.Text = "Ejecute la revisión antes de abrir el detalle."
            LblEstadoGeneral.ForeColor = Color.FromArgb(255, 210, 210)
            CmbCombinacion.Enabled = False
            Return
        End If

        Try
            Inicializar()
        Catch ex As Exception
            Logger.Error(ex, "Form_07_Zapata_Detalle.Load", _zapata.Label_joint)
            MessageBox.Show("No se pudo preparar la vista: " & ex.Message,
                            "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub Inicializar()

        _factores = ZapataService.FactoresPorCombinacion(_zapata)
        _resumen = ZapataService.Resumir(_zapata)

        _combosDinamicas = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If _zapata.Lista_Combinaciones_Dinamicas IsNot Nothing Then
            For Each c In _zapata.Lista_Combinaciones_Dinamicas
                If Not String.IsNullOrEmpty(c.LoadCase) Then _combosDinamicas.Add(c.LoadCase)
            Next
        End If

        LlenarEncabezado()
        ConstruirTabPresiones()
        ConstruirTabCapacidadDemanda()
        ConstruirTabEnvolvente()
        ConstruirTabSeccionesCriticas()
        LlenarComboCombinaciones()

    End Sub

    ' -------------------------------------------------------------------------
    ' Encabezado
    ' -------------------------------------------------------------------------
    Private Sub LlenarEncabezado()

        LblTitulo.Text = $"{_zapata.Label_joint}   —   {_zapata.Nombre}"

        Dim tipoTxt = ZapataService.NombreTipo(_zapata.TipoApoyo) &
                      If(_zapata.TipoApoyoManual, "  (fijado a mano)", "")

        LblSubtitulo.Text = String.Format(
            "Apoyo: {0}   |   Zapata: {1:F2} x {2:F2} x {3:F2} m   |   Pedestal: {4:F2} x {5:F2} m   |   " &
            "fc = {6:F0} MPa   |   qAdm est = {7:F1} kN/m²   |   qAdm din = {8:F1} kN/m²",
            tipoTxt, _zapata.L_b, _zapata.L_h, _zapata.e, _zapata.b, _zapata.h,
            _zapata.fc, _zapata.qAdm_Est, _zapata.qAdm_Din)

        If _resumen.TieneResultados Then
            Dim cumple As Boolean = _resumen.Cumple
            LblEstadoGeneral.Text = String.Format(
                "Peor C/D = {0:F2}   ({1}, combo {2}) — {3}",
                _resumen.PeorFactor, _resumen.Revision, _resumen.Combinacion,
                If(cumple, "CUMPLE", "NO CUMPLE"))
            LblEstadoGeneral.ForeColor = If(cumple,
                                            Color.FromArgb(200, 240, 200),
                                            Color.FromArgb(255, 210, 210))
        Else
            LblEstadoGeneral.Text = "Sin resultados"
            LblEstadoGeneral.ForeColor = Color.FromArgb(220, 220, 220)
        End If

    End Sub

    ' -------------------------------------------------------------------------
    ' Combo de combinaciones
    ' -------------------------------------------------------------------------
    Private Sub LlenarComboCombinaciones()

        _actualizando = True
        Try
            CmbCombinacion.Items.Clear()
            For Each key As String In _zapata.Resultados.Keys
                Dim prefijo As String = If(_combosDinamicas.Contains(key), "[D] ", "[E] ")
                CmbCombinacion.Items.Add(prefijo & key)
            Next

            ' Selecciona por defecto la combinación gobernante, si existe
            If Not String.IsNullOrEmpty(_resumen.Combinacion) Then
                For i As Integer = 0 To CmbCombinacion.Items.Count - 1
                    Dim txt As String = Convert.ToString(CmbCombinacion.Items(i))
                    If txt.EndsWith(" " & _resumen.Combinacion) OrElse txt.EndsWith(_resumen.Combinacion) Then
                        CmbCombinacion.SelectedIndex = i
                        Exit For
                    End If
                Next
            End If
            If CmbCombinacion.SelectedIndex < 0 AndAlso CmbCombinacion.Items.Count > 0 Then
                CmbCombinacion.SelectedIndex = 0
            End If
        Finally
            _actualizando = False
        End Try

        RefrescarPorCombinacion()

    End Sub

    Private Sub CmbCombinacion_SelectedIndexChanged(sender As Object, e As EventArgs) _
        Handles CmbCombinacion.SelectedIndexChanged

        If _actualizando Then Return
        RefrescarPorCombinacion()

    End Sub

    ''' <summary>Devuelve el nombre real de la combinación seleccionada, sin el prefijo [E]/[D].</summary>
    Private Function ComboSeleccionado() As String
        If CmbCombinacion.SelectedIndex < 0 Then Return ""
        Dim txt As String = Convert.ToString(CmbCombinacion.SelectedItem)
        If String.IsNullOrEmpty(txt) Then Return ""
        If txt.StartsWith("[E] ") OrElse txt.StartsWith("[D] ") Then
            Return txt.Substring(4)
        End If
        Return txt
    End Function

    Private Function ResultadoActual() As ResultadoZapata
        Dim combo As String = ComboSeleccionado()
        If String.IsNullOrEmpty(combo) Then Return Nothing
        Dim r As ResultadoZapata = Nothing
        _zapata.Resultados.TryGetValue(combo, r)
        Return r
    End Function

    Private Function FactoresActual() As ZapataService.FactoresCombinacion
        Dim combo As String = ComboSeleccionado()
        If String.IsNullOrEmpty(combo) OrElse _factores Is Nothing Then Return Nothing
        Return _factores.FirstOrDefault(Function(f) String.Equals(f.Combinacion, combo, StringComparison.OrdinalIgnoreCase))
    End Function

    Private Sub RefrescarPorCombinacion()
        Try
            If _pnlPresiones IsNot Nothing Then _pnlPresiones.Invalidate()
            RellenarTarjetas()
            If _pnlSecciones IsNot Nothing Then _pnlSecciones.Invalidate()
        Catch ex As Exception
            Logger.Error(ex, "Form_07_Zapata_Detalle.RefrescarPorCombinacion", _zapata?.Label_joint)
        End Try
    End Sub

    ' =========================================================================
    ' TAB 1 — Distribución de presiones
    ' =========================================================================
    Private Sub ConstruirTabPresiones()

        TabPresiones.Controls.Clear()

        Dim pnlOpciones As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 40,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(8, 6, 8, 6)
        }

        _rdoHeatmap = New RadioButton With {
            .Text = "Heatmap continuo",
            .Location = New Point(10, 8),
            .AutoSize = True,
            .Checked = True,
            .Font = New Font("Segoe UI", 9.5!)
        }
        _rdoGrilla = New RadioButton With {
            .Text = "Cuadrícula 10 x 10",
            .Location = New Point(190, 8),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9.5!)
        }
        AddHandler _rdoHeatmap.CheckedChanged, Sub() _pnlPresiones?.Invalidate()
        AddHandler _rdoGrilla.CheckedChanged, Sub() _pnlPresiones?.Invalidate()
        pnlOpciones.Controls.Add(_rdoHeatmap)
        pnlOpciones.Controls.Add(_rdoGrilla)

        _pnlPresionesInfo = New Panel With {
            .Dock = DockStyle.Right,
            .Width = 280,
            .BackColor = Color.FromArgb(250, 250, 250),
            .AutoScroll = True,
            .Padding = New Padding(8)
        }

        _pnlPresiones = New PanelDobleBuffer With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White
        }
        AddHandler _pnlPresiones.Paint, AddressOf PanelPresiones_Paint

        TabPresiones.Controls.Add(_pnlPresiones)
        TabPresiones.Controls.Add(_pnlPresionesInfo)
        TabPresiones.Controls.Add(pnlOpciones)

    End Sub

    Private Sub PanelPresiones_Paint(sender As Object, e As PaintEventArgs)

        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Color.White)

        Dim res = ResultadoActual()
        RellenarInfoPresiones(res)
        If res Is Nothing Then
            Using f As New Font("Segoe UI", 11),
                  b As New SolidBrush(Color.Gray),
                  sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                g.DrawString("Sin datos para esta combinación.", f, b,
                             New RectangleF(0, 0, _pnlPresiones.Width, _pnlPresiones.Height), sf)
            End Using
            Return
        End If

        ' Área disponible del panel, dejando margen para la leyenda a la derecha
        ' y las etiquetas de eje.
        Dim margL As Single = 40, margT As Single = 30, margR As Single = 80, margB As Single = 40
        Dim availW As Single = Math.Max(50, _pnlPresiones.Width - margL - margR)
        Dim availH As Single = Math.Max(50, _pnlPresiones.Height - margT - margB)
        Dim escala As Single = CSng(Math.Min(availW / _zapata.L_b, availH / _zapata.L_h))
        Dim wPx As Single = CSng(_zapata.L_b * escala)
        Dim hPx As Single = CSng(_zapata.L_h * escala)
        Dim x0 As Single = margL + (availW - wPx) / 2
        Dim y0 As Single = margT + (availH - hPx) / 2

        ' --------------------------------------------------------------------
        ' Transformada Mundo → Pantalla.
        ' Mundo: origen en el centro de la zapata, +X hacia la derecha, +Y hacia
        '        arriba, unidades metros. Coincide con la convención de la
        '        fórmula (g1 en (+X,+Y), g2 en (+X,-Y), g3 en (-X,-Y), g4 en (-X,+Y))
        '        y con los ejes globales de ETABS mirando la planta desde arriba.
        ' Pantalla: origen arriba-izquierda, +X derecha, +Y hacia abajo. Se invierte
        '        el eje Y en la transformada.
        ' Todos los puntos que se dibujan pasan por W2S — ninguna posición se
        ' calcula "a mano" para evitar desfases entre elementos.
        ' --------------------------------------------------------------------
        Dim cx As Single = x0 + wPx / 2
        Dim cy As Single = y0 + hPx / 2
        Dim W2S As Func(Of Double, Double, PointF) =
            Function(wx, wy) New PointF(cx + CSng(wx * escala),
                                        cy - CSng(wy * escala))

        Dim halfB As Double = _zapata.L_b / 2.0
        Dim halfH As Double = _zapata.L_h / 2.0

        ' Esquinas de la zapata en coords locales, mapeadas a los g_i por la fórmula.
        Dim eTR As PointF = W2S(+halfB, +halfH)   ' g1 (+Mx, +My)
        Dim eBR As PointF = W2S(+halfB, -halfH)   ' g2 (-Mx, +My)
        Dim eBL As PointF = W2S(-halfB, -halfH)   ' g3 (-Mx, -My)
        Dim eTL As PointF = W2S(-halfB, +halfH)   ' g4 (+Mx, -My)

        Dim qMin As Double = res.qMin
        Dim qMax As Double = res.qMax

        ' --------------------------------------------------------------------
        ' 1. Heatmap o cuadrícula, rellenando el rectángulo de la zapata.
        ' --------------------------------------------------------------------
        If _rdoHeatmap.Checked Then
            DibujarHeatmap(g, x0, y0, wPx, hPx, res, qMin, qMax)
        Else
            DibujarGrilla(g, x0, y0, wPx, hPx, res, qMin, qMax)
        End If

        ' --------------------------------------------------------------------
        ' 2. Contorno de la zapata (polígono cerrado con las 4 esquinas locales)
        ' --------------------------------------------------------------------
        Using pen As New Pen(Color.FromArgb(60, 60, 60), 1.5F)
            g.DrawPolygon(pen, {eTL, eTR, eBR, eBL})
        End Using

        ' --------------------------------------------------------------------
        ' 3. Aviso "sin gradiente apreciable". Umbral relativo (0.5 % de la
        '    presión promedio) y ubicado arriba, fuera de la huella.
        ' --------------------------------------------------------------------
        Dim qProm As Double = (res.g1 + res.g2 + res.g3 + res.g4) / 4.0
        Dim rangoEsq As Double = Math.Max(res.g1, Math.Max(res.g2, Math.Max(res.g3, res.g4))) -
                                 Math.Min(res.g1, Math.Min(res.g2, Math.Min(res.g3, res.g4)))
        If rangoEsq < 0.005 * Math.Max(1.0, Math.Abs(qProm)) Then
            Using fNota As New Font("Segoe UI", 8.5!, FontStyle.Italic),
                  bNota As New SolidBrush(Color.FromArgb(140, 90, 40))
                Dim msg As String = $"Sin gradiente apreciable — Mx = {res.Mx_Entrada:F2}, My = {res.My_Entrada:F2} kN·m"
                g.DrawString(msg, fNota, bNota, margL, 6)
            End Using
        End If

        ' --------------------------------------------------------------------
        ' 4. Pedestal centrado en el origen local.
        ' --------------------------------------------------------------------
        If _zapata.b > 0 AndAlso _zapata.h > 0 Then
            Dim pTL As PointF = W2S(-_zapata.b / 2.0, +_zapata.h / 2.0)
            Dim pBR As PointF = W2S(+_zapata.b / 2.0, -_zapata.h / 2.0)
            Using bp As New SolidBrush(Color.FromArgb(90, 60, 60, 60)),
                  pp As New Pen(Color.FromArgb(50, 50, 50), 1.2F)
                g.FillRectangle(bp, pTL.X, pTL.Y, pBR.X - pTL.X, pBR.Y - pTL.Y)
                g.DrawRectangle(pp, pTL.X, pTL.Y, pBR.X - pTL.X, pBR.Y - pTL.Y)
            End Using
        End If

        ' --------------------------------------------------------------------
        ' 5. Etiquetas de las esquinas: cada g_i sale sobre la esquina que le
        '    corresponde por la fórmula. Presiones negativas en rojo con (T).
        ' --------------------------------------------------------------------
        Using bWhite As New SolidBrush(Color.FromArgb(220, 255, 255, 255))
            DibujarEtiquetaEsquina(g, bWhite, eTL, "g4", res.g4, alaIzquierda:=True, arriba:=True)
            DibujarEtiquetaEsquina(g, bWhite, eTR, "g1", res.g1, alaIzquierda:=False, arriba:=True)
            DibujarEtiquetaEsquina(g, bWhite, eBR, "g2", res.g2, alaIzquierda:=False, arriba:=False)
            DibujarEtiquetaEsquina(g, bWhite, eBL, "g3", res.g3, alaIzquierda:=True, arriba:=False)
        End Using

        ' --------------------------------------------------------------------
        ' 6. Ejes con flechas y sentido del incremento de presión.
        '    +X y +Y coinciden con los ejes globales de ETABS. Se dibujan las
        '    dos líneas punteadas por los ejes locales y una flecha corta al
        '    final de cada uno, para que el lector no tenga que adivinar hacia
        '    dónde crece cada eje.
        ' --------------------------------------------------------------------
        Dim oScr As PointF = W2S(0, 0)
        Dim tipX As PointF = W2S(+halfB, 0)
        Dim tipY As PointF = W2S(0, +halfH)
        Dim finX As PointF = W2S(-halfB, 0)
        Dim finY As PointF = W2S(0, -halfH)

        Using penEje As New Pen(Color.DarkGray, 1)
            penEje.DashStyle = DashStyle.Dot
            g.DrawLine(penEje, finX, tipX)
            g.DrawLine(penEje, finY, tipY)
        End Using

        Using penFlecha As New Pen(Color.FromArgb(80, 80, 80), 1.5F),
              fEje As New Font("Segoe UI", 8.5!, FontStyle.Bold),
              bEje As New SolidBrush(Color.FromArgb(60, 60, 60))
            penFlecha.CustomEndCap = New Drawing2D.AdjustableArrowCap(4, 5)
            ' Flecha X (hacia +X, más allá del borde derecho)
            Dim xArrIni As PointF = W2S(+halfB - 0.15 * _zapata.L_b, 0)
            Dim xArrFin As PointF = New PointF(tipX.X + 14, tipX.Y)
            g.DrawLine(penFlecha, xArrIni, xArrFin)
            g.DrawString("+X (b)", fEje, bEje, xArrFin.X + 2, xArrFin.Y - 7)
            ' Flecha Y (hacia +Y, más allá del borde superior)
            Dim yArrIni As PointF = W2S(0, +halfH - 0.15 * _zapata.L_h)
            Dim yArrFin As PointF = New PointF(tipY.X, tipY.Y - 14)
            g.DrawLine(penFlecha, yArrIni, yArrFin)
            g.DrawString("+Y (h)", fEje, bEje, yArrFin.X - 20, yArrFin.Y - 14)
        End Using

        ' Marcador del origen — punto pequeño en el centro
        Using bOrig As New SolidBrush(Color.FromArgb(60, 60, 60))
            g.FillEllipse(bOrig, oScr.X - 2.5F, oScr.Y - 2.5F, 5, 5)
        End Using

        ' --------------------------------------------------------------------
        ' 7. Leyenda vertical con la escala de color.
        ' --------------------------------------------------------------------
        DibujarLeyendaHeatmap(g, eTR.X + 20, y0, hPx, qMin, qMax)

    End Sub

    ''' <summary>
    ''' Interpolación bilineal de la presión sobre la huella, tomando los cuatro
    ''' valores de esquina de VerificarCapacidadSuelo. La convención de esos gᵢ
    ''' viene fija por la fórmula original:
    '''
    '''   g1 = P/A + Mx + My      ⇒ esquina (+x, +y)   TOP-RIGHT
    '''   g2 = P/A − Mx + My      ⇒ esquina (+x, −y)   BOTTOM-RIGHT
    '''   g3 = P/A − Mx − My      ⇒ esquina (−x, −y)   BOTTOM-LEFT
    '''   g4 = P/A + Mx − My      ⇒ esquina (−x, +y)   TOP-LEFT
    '''
    ''' Antes se asumía que iban en contrarreloj desde TL (g1 TL, g2 TR, g3 BR,
    ''' g4 BL), lo que dejaba el mapa "rotado" respecto a los valores reales y
    ''' escondía la variación por Mx.
    '''
    ''' Como la distribución de presión bajo una zapata rígida es un plano, la
    ''' interpolación bilineal de las esquinas es exacta.
    ''' </summary>
    ''' <param name="frx">Fracción horizontal 0..1 (0 = borde izquierdo).</param>
    ''' <param name="fry">Fracción vertical 0..1 (0 = borde superior en pantalla).</param>
    Private Shared Function InterpolarQ(res As ResultadoZapata, frx As Double, fry As Double) As Double
        ' Fila superior (fry=0): de TL (g4) a TR (g1)
        Dim qTop As Double = (1.0 - frx) * res.g4 + frx * res.g1
        ' Fila inferior (fry=1): de BL (g3) a BR (g2)
        Dim qBot As Double = (1.0 - frx) * res.g3 + frx * res.g2
        Return (1.0 - fry) * qTop + fry * qBot
    End Function

    Private Sub DibujarHeatmap(g As Graphics, x0 As Single, y0 As Single, wPx As Single, hPx As Single,
                                res As ResultadoZapata, qMin As Double, qMax As Double)

        ' Bitmap a resolución reducida (muestreo cada 3 px). El plano bilineal se
        ' interpola linealmente en cada eje, así que resolución alta no aporta.
        Dim wi As Integer = CInt(Math.Max(1, wPx))
        Dim hi As Integer = CInt(Math.Max(1, hPx))
        Dim paso As Integer = 3
        Using bmp As New Bitmap(Math.Max(1, wi \ paso), Math.Max(1, hi \ paso))
            For py As Integer = 0 To bmp.Height - 1
                For px As Integer = 0 To bmp.Width - 1
                    Dim frx As Double = (px + 0.5) / bmp.Width
                    Dim fry As Double = (py + 0.5) / bmp.Height
                    Dim q As Double = InterpolarQ(res, frx, fry)
                    bmp.SetPixel(px, py, ColorPresion(q, qMin, qMax))
                Next
            Next
            Dim modoAnterior = g.InterpolationMode
            g.InterpolationMode = InterpolationMode.Bilinear
            g.DrawImage(bmp, New RectangleF(x0, y0, wPx, hPx))
            g.InterpolationMode = modoAnterior
        End Using

    End Sub

    Private Sub DibujarGrilla(g As Graphics, x0 As Single, y0 As Single, wPx As Single, hPx As Single,
                              res As ResultadoZapata, qMin As Double, qMax As Double)

        Const n As Integer = 10
        Dim dx As Single = wPx / n
        Dim dy As Single = hPx / n
        For j As Integer = 0 To n - 1
            For i As Integer = 0 To n - 1
                Dim frx As Double = (i + 0.5) / n
                Dim fry As Double = (j + 0.5) / n
                Dim q As Double = InterpolarQ(res, frx, fry)
                Using b As New SolidBrush(ColorPresion(q, qMin, qMax))
                    g.FillRectangle(b, x0 + i * dx, y0 + j * dy, dx + 0.5F, dy + 0.5F)
                End Using
            Next
        Next
        Using penGrid As New Pen(Color.FromArgb(80, 100, 100, 100), 0.5F)
            For k As Integer = 0 To n
                g.DrawLine(penGrid, x0 + k * dx, y0, x0 + k * dx, y0 + hPx)
                g.DrawLine(penGrid, x0, y0 + k * dy, x0 + wPx, y0 + k * dy)
            Next
        End Using

    End Sub

    ''' <summary>
    ''' Mapeo de presión a color en dos regímenes:
    '''  • Todo compresión (qMin ≥ 0): gradiente cálido azul→verde→amarillo→rojo
    '''    normalizado entre qMin y qMax, así se ve el detalle del gradiente aun
    '''    cuando la variación es pequeña respecto a la presión media.
    '''  • Con tracción (qMin &lt; 0): escala divergente anclada en 0. La compresión
    '''    va blanco→amarillo→rojo, la tracción blanco→celeste→azul intenso,
    '''    normalizada a max(|qMin|, |qMax|) para que el cero siempre esté en el
    '''    color neutro. Así el signo se reconoce sin mirar la leyenda.
    ''' </summary>
    Private Shared Function ColorPresion(q As Double, qMin As Double, qMax As Double) As Color

        If qMin < 0 Then
            ' Régimen divergente centrado en 0.
            Dim norm As Double = Math.Max(Math.Abs(qMin), Math.Abs(qMax))
            If norm < 0.0001 Then Return Color.White
            Dim t As Double = Math.Max(-1.0, Math.Min(1.0, q / norm))

            Dim Rd As Integer, Gd As Integer, Bd As Integer
            If t >= 0 Then
                ' Compresión: blanco → amarillo → rojo intenso
                If t < 0.5 Then
                    Dim s As Double = t / 0.5
                    Rd = 255
                    Gd = CInt(255 - (255 - 220) * s)  ' 255 → 220
                    Bd = CInt(240 - (240 - 60) * s)   ' 240 → 60
                Else
                    Dim s As Double = (t - 0.5) / 0.5
                    Rd = CInt(255 - (255 - 200) * s)  ' 255 → 200
                    Gd = CInt(220 - (220 - 20) * s)   ' 220 → 20
                    Bd = CInt(60 - 60 * s)            ' 60 → 0
                End If
            Else
                ' Tracción: blanco → celeste → azul intenso (uplift)
                Dim s As Double = Math.Min(1.0, -t)
                If s < 0.5 Then
                    Dim u As Double = s / 0.5
                    Rd = CInt(255 - (255 - 180) * u)  ' 255 → 180
                    Gd = CInt(255 - (255 - 220) * u)  ' 255 → 220
                    Bd = 255                          ' se mantiene 255
                Else
                    Dim u As Double = (s - 0.5) / 0.5
                    Rd = CInt(180 - (180 - 20) * u)   ' 180 → 20
                    Gd = CInt(220 - (220 - 60) * u)   ' 220 → 60
                    Bd = CInt(255 - (255 - 180) * u)  ' 255 → 180
                End If
            End If
            Return Color.FromArgb(255,
                                  Math.Max(0, Math.Min(255, Rd)),
                                  Math.Max(0, Math.Min(255, Gd)),
                                  Math.Max(0, Math.Min(255, Bd)))
        End If

        ' Régimen todo compresión: gradiente cálido normalizado al rango real.
        Dim rango As Double = qMax - qMin
        If rango < 0.0001 Then Return Color.FromArgb(255, 120, 220, 120)
        Dim frac As Double = Math.Max(0, Math.Min(1, (q - qMin) / rango))

        Dim R As Integer, G As Integer, B As Integer
        If frac < 0.33 Then
            Dim tt As Double = frac / 0.33
            R = CInt(30 + (60 - 30) * tt)
            G = CInt(100 + (200 - 100) * tt)
            B = CInt(220 - (220 - 90) * tt)
        ElseIf frac < 0.66 Then
            Dim tt As Double = (frac - 0.33) / 0.33
            R = CInt(60 + (240 - 60) * tt)
            G = CInt(200 + (220 - 200) * tt)
            B = CInt(90 - 60 * tt)
        Else
            Dim tt As Double = (frac - 0.66) / 0.34
            R = CInt(240 + (215 - 240) * tt)
            G = CInt(220 - (220 - 40) * tt)
            B = CInt(30 - 30 * tt)
        End If
        Return Color.FromArgb(255,
                              Math.Max(0, Math.Min(255, R)),
                              Math.Max(0, Math.Min(255, G)),
                              Math.Max(0, Math.Min(255, B)))

    End Function

    ''' <summary>
    ''' Barra de leyenda vertical. Recorre el rango real (qMin puede ser negativo)
    ''' y, si el rango cruza 0, dibuja una marca horizontal etiquetada "0" en la
    ''' altura correspondiente. Así el usuario reconoce a simple vista dónde
    ''' termina la tracción y empieza la compresión.
    ''' </summary>
    Private Sub DibujarLeyendaHeatmap(g As Graphics, x As Single, y As Single, alto As Single,
                                       qMin As Double, qMax As Double)
        Dim ancho As Single = 18
        Dim pasos As Integer = 60
        Dim dyLoc As Single = alto / pasos
        Dim qTop As Double = qMax
        Dim qBot As Double = qMin
        Dim rango As Double = qTop - qBot
        If rango < 0.0001 Then rango = 1.0

        For k As Integer = 0 To pasos - 1
            Dim frac As Double = 1.0 - k / CDbl(pasos - 1)
            Dim q As Double = qBot + frac * (qTop - qBot)
            Using b As New SolidBrush(ColorPresion(q, qMin, qMax))
                g.FillRectangle(b, x, y + k * dyLoc, ancho, dyLoc + 0.5F)
            End Using
        Next
        Using pen As New Pen(Color.DimGray, 1)
            g.DrawRectangle(pen, x, y, ancho, alto)
        End Using
        Using f As New Font("Segoe UI", 7.5F),
              b As New SolidBrush(Color.Black)
            g.DrawString($"{qTop:F1}", f, b, x + ancho + 3, y - 4)
            g.DrawString($"{qBot:F1}", f, b, x + ancho + 3, y + alto - 8)
            g.DrawString("kN/m²", f, b, x - 5, y + alto + 4)
            ' Marca del cero cuando la escala cruza tracción/compresión.
            If qMin < 0 AndAlso qMax > 0 Then
                Dim frac0 As Double = (qTop - 0.0) / rango
                Dim y0 As Single = y + CSng(frac0 * alto)
                Using penZero As New Pen(Color.Black, 1.2F)
                    g.DrawLine(penZero, x - 3, y0, x + ancho + 3, y0)
                End Using
                g.DrawString("0", f, b, x + ancho + 3, y0 - 6)
            End If
        End Using
    End Sub

    ''' <summary>
    ''' Etiqueta de esquina. Recibe la posición de la esquina en pantalla
    ''' (calculada por W2S sobre las coords locales) y decide si el texto
    ''' se pinta hacia arriba/abajo/izq/der para que caiga por fuera de la huella.
    ''' </summary>
    Private Sub DibujarEtiquetaEsquina(g As Graphics, bg As Brush, esquina As PointF,
                                        nombre As String, valor As Double,
                                        alaIzquierda As Boolean, arriba As Boolean)
        Dim traccion As Boolean = (valor < 0)
        Dim txt As String = If(traccion,
                               $"{nombre} = {valor:F1} (T)",
                               $"{nombre} = {valor:F1}")
        Using f As New Font("Segoe UI", 8, FontStyle.Bold),
              b As New SolidBrush(If(traccion, Color.FromArgb(180, 0, 0), Color.Black))
            Dim sz As SizeF = g.MeasureString(txt, f)
            Dim rx As Single = If(alaIzquierda, esquina.X - sz.Width - 5, esquina.X + 3)
            Dim ry As Single = If(arriba, esquina.Y - sz.Height - 3, esquina.Y + 3)
            g.FillRectangle(bg, rx, ry, sz.Width + 4, sz.Height + 2)
            g.DrawString(txt, f, b, rx + 2, ry + 1)
        End Using
    End Sub

    Private Sub RellenarInfoPresiones(res As ResultadoZapata)

        _pnlPresionesInfo.Controls.Clear()
        If res Is Nothing Then Return

        Dim y As Integer = 4
        Dim addLbl As Action(Of String, Font, Color) =
            Sub(txt, fn, cl)
                Dim lbl As New Label With {
                    .Text = txt,
                    .Font = fn,
                    .ForeColor = cl,
                    .AutoSize = False,
                    .Location = New Point(4, y),
                    .Size = New Size(_pnlPresionesInfo.Width - 12, fn.Height + 6),
                    .TextAlign = ContentAlignment.MiddleLeft
                }
                _pnlPresionesInfo.Controls.Add(lbl)
                y += lbl.Height + 2
            End Sub

        Dim fTit As New Font("Segoe UI", 10.0!, FontStyle.Bold)
        Dim fBod As New Font("Segoe UI", 9.5!)
        Dim fBold As New Font("Segoe UI", 9.5!, FontStyle.Bold)

        addLbl("Cargas de entrada", fTit, ColorARCO)
        addLbl($"P    = {res.P_Reactivo:F1} kN", fBod, Color.Black)
        Dim cMx As Color = If(Math.Abs(res.Mx_Entrada) < 0.01, Color.Gray, Color.Black)
        Dim cMy As Color = If(Math.Abs(res.My_Entrada) < 0.01, Color.Gray, Color.Black)
        addLbl($"Mx   = {res.Mx_Entrada:F2} kN·m", fBold, cMx)
        addLbl($"My   = {res.My_Entrada:F2} kN·m", fBold, cMy)
        If Math.Abs(res.Mx_Entrada) < 0.01 AndAlso Math.Abs(res.My_Entrada) < 0.01 Then
            addLbl("Ambos momentos ≈ 0:", fBod, ColorMalTexto)
            addLbl("la presión es P/A en toda la huella.", fBod, ColorMalTexto)
        End If

        y += 4
        Dim rangoG As Double = Math.Max(res.g1, Math.Max(res.g2, Math.Max(res.g3, res.g4))) -
                               Math.Min(res.g1, Math.Min(res.g2, Math.Min(res.g3, res.g4)))
        Dim colRango As Color = If(rangoG < 0.5, Color.Gray, ColorARCO)
        addLbl($"Rango esquinas = {rangoG:F2} kN/m²", fBold, colRango)

        ' Contribución de cada término al gradiente. Descompone la fórmula para
        ' que sea inmediato ver qué domina: si ΔMx y ΔMy salen chicos frente a
        ' P/A, el mapa parecerá plano por más que Mx/My no sean cero.
        Dim L1 As Double = _zapata.L_b   ' dimensión en X
        Dim L2 As Double = _zapata.L_h   ' dimensión en Y
        Dim area As Double = If(L1 > 0 AndAlso L2 > 0, L1 * L2, 0)
        If area > 0 Then
            Dim pMedia As Double = res.P_Efectivo / area
            Dim dMx As Double = 6.0 * res.Mx_Entrada / (L1 * L2 * L2)     ' aporte a borde en Y
            Dim dMy As Double = 6.0 * res.My_Entrada / (L2 * L1 * L1)     ' aporte a borde en X
            y += 4
            addLbl("Descomposición del gradiente", fTit, ColorARCO)
            addLbl($"P/A    = {pMedia:F2} kN/m²", fBod, Color.Black)
            addLbl($"±6Mx/(b·h²) = ±{Math.Abs(dMx):F2}", fBod, Color.Black)
            addLbl($"±6My/(h·b²) = ±{Math.Abs(dMy):F2}", fBod, Color.Black)
        End If

        y += 8
        addLbl("Presiones bajo la zapata", fTit, ColorARCO)
        addLbl($"qMax  = {res.qMax:F2} kN/m²", fBod, Color.Black)
        Dim qMinTxt As String = If(res.qMin < 0, $"qMin  = {res.qMin:F2} kN/m² (tracción)",
                                                  $"qMin  = {res.qMin:F2} kN/m²")
        addLbl(qMinTxt, fBold,
               If(res.qMin < 0, ColorMalTexto, Color.Black))
        Dim qProm As Double = (res.g1 + res.g2 + res.g3 + res.g4) / 4.0
        addLbl($"Promedio ≈ {qProm:F2} kN/m²", fBod, Color.Black)

        y += 8
        addLbl("Esquinas", fTit, ColorARCO)
        addLbl($"g1 = {res.g1:F2}   g2 = {res.g2:F2}", fBod, Color.Black)
        addLbl($"g3 = {res.g3:F2}   g4 = {res.g4:F2}", fBod, Color.Black)

        y += 8
        addLbl("Excentricidad", fTit, ColorARCO)
        addLbl($"ex = {res.ex:F3} m   (lim {res.Lim_x_usado:F3})", fBod, Color.Black)
        addLbl($"ey = {res.ey:F3} m   (lim {res.Lim_y_usado:F3})", fBod, Color.Black)

        If res.UsoPesoEstabilizante Then
            y += 8
            addLbl("Peso estabilizante activo", fTit, Color.FromArgb(0, 97, 0))
            Dim wTot As Double = res.W_Zapata + res.W_Pedestal + res.W_Suelo
            addLbl($"W_zap  = {res.W_Zapata:F1} kN", fBod, Color.Black)
            addLbl($"W_ped  = {res.W_Pedestal:F1} kN", fBod, Color.Black)
            addLbl($"W_sue  = {res.W_Suelo:F1} kN", fBod, Color.Black)
            addLbl($"P efec = {res.P_Efectivo:F1} kN  (+{wTot:F1})", fBold, Color.Black)
        Else
            y += 8
            addLbl($"P = {res.P_Efectivo:F1} kN", fBold, Color.Black)
        End If

        If res.qMin < 0 Then
            y += 8
            addLbl("Hay tracción bajo la zapata.", fBold, ColorMalTexto)
            addLbl("Revise excentricidad y peso.", fBod, ColorMalTexto)
        End If

    End Sub

    ' =========================================================================
    ' TAB 2 — Capacidad vs demanda (tarjetas)
    ' =========================================================================
    Private Sub ConstruirTabCapacidadDemanda()

        TabCapacidadDemanda.Controls.Clear()

        _flowTarjetas = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = True,
            .BackColor = Color.White,
            .Padding = New Padding(10, 10, 10, 10)
        }
        TabCapacidadDemanda.Controls.Add(_flowTarjetas)

    End Sub

    Private Sub RellenarTarjetas()

        If _flowTarjetas Is Nothing Then Return
        _flowTarjetas.Controls.Clear()

        Dim res = ResultadoActual()
        Dim fac = FactoresActual()
        If res Is Nothing Then
            _flowTarjetas.Controls.Add(New Label With {
                .Text = "Sin datos para esta combinación.",
                .Font = New Font("Segoe UI", 10, FontStyle.Italic),
                .AutoSize = True,
                .Margin = New Padding(8)
            })
            Return
        End If

        Dim esDin As Boolean = If(fac IsNot Nothing, fac.EsDinamica, False)
        Dim qAdm As Double = If(esDin, _zapata.qAdm_Din, _zapata.qAdm_Est)

        ' Tarjeta 1: Capacidad del suelo
        Dim cdSuelo As Double = If(fac IsNot Nothing, fac.Suelo, 0)
        AgregarTarjeta("Capacidad del suelo",
                       $"qMax = {res.qMax:F2} kN/m²",
                       $"qAdm = {qAdm:F2} kN/m²",
                       res.qMax, qAdm, cdSuelo,
                       If(esDin, "dinámico", "estático"))

        ' Tarjeta 2: Excentricidad
        Dim cdExc As Double = If(fac IsNot Nothing, fac.Excentricidad, 0)
        Dim eMax As Double = Math.Max(Math.Abs(res.ex), Math.Abs(res.ey))
        Dim limMin As Double = Math.Min(
            If(res.Lim_x_usado > 0, res.Lim_x_usado, Double.MaxValue),
            If(res.Lim_y_usado > 0, res.Lim_y_usado, Double.MaxValue))
        If limMin = Double.MaxValue Then limMin = 0
        AgregarTarjeta("Excentricidad",
                       $"|e| = {eMax:F3} m",
                       $"lim = {limMin:F3} m",
                       eMax, limMin, cdExc,
                       $"ex = {res.ex:F3}, ey = {res.ey:F3}")

        ' Tarjeta 3: Punzonamiento
        Dim cdPunz As Double = If(fac IsNot Nothing, fac.Punzonamiento, 0)
        AgregarTarjeta("Punzonamiento",
                       $"Vu = {Math.Abs(res.Vu_p):F1} kN",
                       $"φVc = {res.Vc_p:F1} kN",
                       Math.Abs(res.Vu_p), res.Vc_p, cdPunz,
                       $"b0={ZapataService.PerimetroCritico(_zapata.b, _zapata.h, _zapata.d, _zapata.TipoApoyo):F2} m — {ZapataService.NombreTipo(_zapata.TipoApoyo)}")

        ' Tarjeta 4: Cortante (una dirección — la más crítica)
        Dim cdCort As Double = If(fac IsNot Nothing, fac.Cortante, 0)
        Dim vuA As Double = Math.Max(res.Vu1_C, res.Vu3_C)
        Dim vuB As Double = Math.Max(res.Vu2_C, res.Vu4_C)
        Dim usaA As Boolean = (vuA * res.Vc1_C >= vuB * res.Vc2_C)  ' heurística inversa
        Dim vuCort As Double = If(usaA, vuA, vuB)
        Dim vcCort As Double = If(usaA, res.Vc2_C, res.Vc1_C)
        AgregarTarjeta("Cortante",
                       $"Vu = {vuCort:F1} kN",
                       $"φVc = {vcCort:F1} kN",
                       vuCort, vcCort, cdCort,
                       "sección a d de la cara del pedestal")

        ' Tarjeta 5: Flexión
        Dim cdFlex As Double = If(fac IsNot Nothing, fac.Flexion, 0)
        Dim rhoReq As Double = Math.Max(res.Rho_1, res.Rho_2)
        Dim rhoCol As Double = If(res.Rho_1 >= res.Rho_2, _zapata.Rho_L1, _zapata.Rho_L2)
        AgregarTarjeta("Flexión",
                       $"ρ req = {rhoReq:F5}",
                       $"ρ col = {rhoCol:F5}",
                       rhoReq, rhoCol, cdFlex,
                       $"Mu1 = {res.Mu_1:F1}, Mu2 = {res.Mu_2:F1} kN·m")

    End Sub

    ''' <summary>
    ''' Tarjeta con barra de demanda vs capacidad. Verde si C/D >= UMBRAL, rojo si no.
    ''' Si C/D = 0 (revisión sin demanda) la tarjeta va gris.
    ''' </summary>
    Private Sub AgregarTarjeta(titulo As String, txtDemanda As String, txtCapacidad As String,
                                demanda As Double, capacidad As Double, cd As Double,
                                detalle As String)

        Dim aplica As Boolean = (cd > 0)
        Dim cumple As Boolean = (cd >= Funciones_00_Varias.UMBRAL_CD)
        Dim colorBorde As Color = If(Not aplica, Color.FromArgb(180, 180, 180),
                                     If(cumple, Color.FromArgb(120, 180, 120), Color.FromArgb(200, 80, 80)))
        Dim colorFondo As Color = If(Not aplica, Color.FromArgb(248, 248, 248),
                                     If(cumple, Color.FromArgb(240, 250, 240), Color.FromArgb(252, 240, 240)))

        Dim tarjeta As New Panel With {
            .Width = Math.Max(700, _flowTarjetas.ClientSize.Width - 40),
            .Height = 120,
            .Margin = New Padding(0, 0, 0, 12),
            .BackColor = colorFondo,
            .BorderStyle = BorderStyle.None,
            .Padding = New Padding(12, 8, 12, 8)
        }
        AddHandler tarjeta.Paint, Sub(s, ev)
                                      Using pen As New Pen(colorBorde, 2)
                                          ev.Graphics.DrawRectangle(pen, 0, 0, tarjeta.Width - 1, tarjeta.Height - 1)
                                      End Using
                                      ' Barra
                                      Dim bx As Integer = 12
                                      Dim by As Integer = 55
                                      Dim bw As Integer = tarjeta.Width - 260
                                      Dim bh As Integer = 22
                                      Using bBg As New SolidBrush(Color.FromArgb(230, 230, 230))
                                          ev.Graphics.FillRectangle(bBg, bx, by, bw, bh)
                                      End Using
                                      If aplica Then
                                          ' escala: demanda toma 100 %, la barra representa el ratio C/D (donde 1 = borde)
                                          ' Se dibuja demanda en gris oscuro y capacidad extendida en verde/rojo.
                                          Dim maxRef As Double = Math.Max(demanda, capacidad)
                                          If maxRef < 0.0000001 Then maxRef = 1
                                          Dim wDem As Integer = CInt(bw * Math.Min(1.0, demanda / maxRef))
                                          Dim wCap As Integer = CInt(bw * Math.Min(1.0, capacidad / maxRef))
                                          Using bCap As New SolidBrush(If(cumple, Color.FromArgb(150, 220, 150),
                                                                                    Color.FromArgb(240, 170, 170)))
                                              ev.Graphics.FillRectangle(bCap, bx, by, wCap, bh)
                                          End Using
                                          Using bDem As New SolidBrush(Color.FromArgb(90, 90, 90))
                                              ev.Graphics.FillRectangle(bDem, bx, by, wDem, bh)
                                          End Using
                                      End If
                                      Using penB As New Pen(Color.FromArgb(120, 120, 120), 1)
                                          ev.Graphics.DrawRectangle(penB, bx, by, bw, bh)
                                      End Using
                                  End Sub

        Dim lblTit As New Label With {
            .Text = titulo,
            .Font = New Font("Segoe UI", 11.5!, FontStyle.Bold),
            .ForeColor = ColorARCO,
            .AutoSize = False,
            .Location = New Point(12, 6),
            .Size = New Size(400, 24)
        }
        Dim lblDet As New Label With {
            .Text = detalle,
            .Font = New Font("Segoe UI", 8.5!, FontStyle.Italic),
            .ForeColor = Color.FromArgb(100, 100, 100),
            .AutoSize = False,
            .Location = New Point(12, 30),
            .Size = New Size(tarjeta.Width - 260, 20)
        }

        Dim lblCD As New Label With {
            .Text = If(aplica, $"C/D = {Math.Min(cd, 9.99):F2}", "no aplica"),
            .Font = New Font("Segoe UI", 20.0!, FontStyle.Bold),
            .ForeColor = If(Not aplica, Color.Gray, If(cumple, Color.FromArgb(0, 120, 0), Color.FromArgb(180, 0, 0))),
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleRight,
            .Location = New Point(tarjeta.Width - 230, 20),
            .Size = New Size(220, 42),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        Dim lblEstado As New Label With {
            .Text = If(Not aplica, "sin demanda", If(cumple, "CUMPLE", "NO CUMPLE")),
            .Font = New Font("Segoe UI", 9.5!, FontStyle.Bold),
            .ForeColor = If(Not aplica, Color.Gray, If(cumple, Color.FromArgb(0, 97, 0), ColorMalTexto)),
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleRight,
            .Location = New Point(tarjeta.Width - 230, 62),
            .Size = New Size(220, 20),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }

        Dim lblDem As New Label With {
            .Text = txtDemanda,
            .Font = New Font("Segoe UI", 9.5!),
            .ForeColor = Color.Black,
            .AutoSize = False,
            .Location = New Point(12, 84),
            .Size = New Size(300, 22)
        }
        Dim lblCap As New Label With {
            .Text = txtCapacidad,
            .Font = New Font("Segoe UI", 9.5!),
            .ForeColor = Color.Black,
            .AutoSize = False,
            .Location = New Point(320, 84),
            .Size = New Size(300, 22)
        }

        tarjeta.Controls.Add(lblTit)
        tarjeta.Controls.Add(lblDet)
        tarjeta.Controls.Add(lblCD)
        tarjeta.Controls.Add(lblEstado)
        tarjeta.Controls.Add(lblDem)
        tarjeta.Controls.Add(lblCap)

        _flowTarjetas.Controls.Add(tarjeta)

    End Sub

    ' =========================================================================
    ' TAB 3 — Envolvente por combinación
    ' =========================================================================
    Private Sub ConstruirTabEnvolvente()

        TabEnvolvente.Controls.Clear()

        _dgvEnvolvente = New DataGridView With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .BackgroundColor = Color.White,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .Font = New Font("Segoe UI", 9.5!),
            .ColumnHeadersHeight = 32,
            .ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = ColorARCO,
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 9.5!, FontStyle.Bold),
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            .EnableHeadersVisualStyles = False
        }

        _dgvEnvolvente.Columns.Add("colCombo", "Combinación")
        _dgvEnvolvente.Columns.Add("colTipo", "Tipo")
        _dgvEnvolvente.Columns.Add("colSuelo", "C/D Suelo")
        _dgvEnvolvente.Columns.Add("colExc", "C/D Exc")
        _dgvEnvolvente.Columns.Add("colPunz", "C/D Punz")
        _dgvEnvolvente.Columns.Add("colCort", "C/D Cort")
        _dgvEnvolvente.Columns.Add("colFlex", "C/D Flex")
        _dgvEnvolvente.Columns.Add("colPeor", "Peor")
        _dgvEnvolvente.Columns.Add("colGob", "Gobierna")
        _dgvEnvolvente.Columns("colCombo").FillWeight = 22
        _dgvEnvolvente.Columns("colTipo").FillWeight = 8
        For Each nm In {"colSuelo", "colExc", "colPunz", "colCort", "colFlex", "colPeor"}
            _dgvEnvolvente.Columns(nm).FillWeight = 8
            _dgvEnvolvente.Columns(nm).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Next
        _dgvEnvolvente.Columns("colGob").FillWeight = 14

        If _factores IsNot Nothing Then
            For Each f In _factores
                Dim idx As Integer = _dgvEnvolvente.Rows.Add(
                    f.Combinacion,
                    If(f.EsDinamica, "Din", "Est"),
                    "", "", "", "", "",
                    "", f.Revision)
                Dim fila = _dgvEnvolvente.Rows(idx)
                Try
                    ReporteGridHelpers.AsignarCD(fila.Cells("colSuelo"), f.Suelo, True)
                    ReporteGridHelpers.AsignarCD(fila.Cells("colExc"), f.Excentricidad, True)
                    ReporteGridHelpers.AsignarCD(fila.Cells("colPunz"), f.Punzonamiento, True)
                    ReporteGridHelpers.AsignarCD(fila.Cells("colCort"), f.Cortante, True)
                    ReporteGridHelpers.AsignarCD(fila.Cells("colFlex"), f.Flexion, True)
                    ReporteGridHelpers.AsignarCD(fila.Cells("colPeor"), f.Peor, True, True)
                Catch ex As Exception
                    Logger.Error(ex, "Form_07_Zapata_Detalle.ConstruirTabEnvolvente", f.Combinacion)
                End Try
            Next
        End If

        AddHandler _dgvEnvolvente.CellDoubleClick, AddressOf DgvEnvolvente_DoubleClick

        TabEnvolvente.Controls.Add(_dgvEnvolvente)

    End Sub

    Private Sub DgvEnvolvente_DoubleClick(sender As Object, e As DataGridViewCellEventArgs)

        If e.RowIndex < 0 Then Return
        Try
            Dim combo As String = Convert.ToString(_dgvEnvolvente.Rows(e.RowIndex).Cells("colCombo").Value)
            If String.IsNullOrEmpty(combo) Then Return
            For i As Integer = 0 To CmbCombinacion.Items.Count - 1
                Dim txt As String = Convert.ToString(CmbCombinacion.Items(i))
                If txt.EndsWith(" " & combo) OrElse txt.EndsWith(combo) Then
                    CmbCombinacion.SelectedIndex = i
                    TabDetalle.SelectedTab = TabPresiones
                    Exit For
                End If
            Next
        Catch ex As Exception
            Logger.Error(ex, "Form_07_Zapata_Detalle.DgvEnvolvente_DoubleClick")
        End Try

    End Sub

    ' =========================================================================
    ' TAB 4 — Secciones críticas
    ' =========================================================================
    Private Sub ConstruirTabSeccionesCriticas()

        TabSeccionesCriticas.Controls.Clear()

        Dim pnlLeyenda As New Panel With {
            .Dock = DockStyle.Right,
            .Width = 240,
            .BackColor = Color.FromArgb(250, 250, 250),
            .Padding = New Padding(10)
        }
        Dim lblLeyendaTit As New Label With {
            .Text = "Leyenda",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = ColorARCO,
            .Dock = DockStyle.Top,
            .Height = 26
        }
        pnlLeyenda.Controls.Add(lblLeyendaTit)
        Dim leyendas As String() = {
            "Rectángulo azul: zapata (L_b × L_h)",
            "Rectángulo oscuro: pedestal (b × h)",
            "Línea magenta: perímetro crítico de",
            "  punzonamiento (a d/2 del pedestal)",
            "Líneas naranjas: secciones de",
            "  cortante (a d de la cara)",
            "Líneas verdes: secciones de flexión",
            "  (en la cara del pedestal)",
            "",
            "Los valores mostrados corresponden",
            "a la combinación seleccionada."
        }
        Dim yLoc As Integer = 30
        For Each txt In leyendas
            Dim l As New Label With {
                .Text = txt,
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.FromArgb(60, 60, 60),
                .AutoSize = False,
                .Location = New Point(4, yLoc),
                .Size = New Size(pnlLeyenda.Width - 12, 18)
            }
            pnlLeyenda.Controls.Add(l)
            yLoc += 18
        Next

        _pnlSecciones = New PanelDobleBuffer With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White
        }
        AddHandler _pnlSecciones.Paint, AddressOf PanelSecciones_Paint

        TabSeccionesCriticas.Controls.Add(_pnlSecciones)
        TabSeccionesCriticas.Controls.Add(pnlLeyenda)

    End Sub

    Private Sub PanelSecciones_Paint(sender As Object, e As PaintEventArgs)

        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Color.White)

        Dim res = ResultadoActual()

        Dim margen As Single = 60
        Dim availW As Single = Math.Max(50, _pnlSecciones.Width - 2 * margen)
        Dim availH As Single = Math.Max(50, _pnlSecciones.Height - 2 * margen)
        Dim escala As Single = CSng(Math.Min(availW / _zapata.L_b, availH / _zapata.L_h))
        Dim wPx As Single = CSng(_zapata.L_b * escala)
        Dim hPx As Single = CSng(_zapata.L_h * escala)
        Dim x0 As Single = margen + (availW - wPx) / 2
        Dim y0 As Single = margen + (availH - hPx) / 2

        ' Misma transformada Mundo → Pantalla que en el tab de presiones. Todos
        ' los elementos (zapata, pedestal, perímetro de punzonamiento, secciones
        ' de cortante y de flexión) se definen en coords locales metros y luego
        ' se mapean a píxeles con W2S — así ningún lado queda "corrido" respecto
        ' a otro por aritmética manual.
        Dim cx As Single = x0 + wPx / 2
        Dim cy As Single = y0 + hPx / 2
        Dim W2S As Func(Of Double, Double, PointF) =
            Function(wx, wy) New PointF(cx + CSng(wx * escala),
                                        cy - CSng(wy * escala))

        Dim Lb2 As Double = _zapata.L_b / 2.0
        Dim Lh2 As Double = _zapata.L_h / 2.0
        Dim b2 As Double = _zapata.b / 2.0
        Dim h2 As Double = _zapata.h / 2.0
        Dim d As Double = _zapata.d
        Dim d2 As Double = d / 2.0

        ' Zapata (huella)
        Dim zTL As PointF = W2S(-Lb2, +Lh2)
        Dim zBR As PointF = W2S(+Lb2, -Lh2)
        Using bZap As New SolidBrush(Color.FromArgb(30, 120, 170, 220)),
              penZap As New Pen(Color.FromArgb(40, 90, 140), 1.5F)
            g.FillRectangle(bZap, zTL.X, zTL.Y, zBR.X - zTL.X, zBR.Y - zTL.Y)
            g.DrawRectangle(penZap, zTL.X, zTL.Y, zBR.X - zTL.X, zBR.Y - zTL.Y)
        End Using

        ' Pedestal
        Dim pTL As PointF = W2S(-b2, +h2)
        Dim pBR As PointF = W2S(+b2, -h2)
        Using bp As New SolidBrush(Color.FromArgb(150, 70, 70, 70)),
              penp As New Pen(Color.FromArgb(30, 30, 30), 1.2F)
            g.FillRectangle(bp, pTL.X, pTL.Y, pBR.X - pTL.X, pBR.Y - pTL.Y)
            g.DrawRectangle(penp, pTL.X, pTL.Y, pBR.X - pTL.X, pBR.Y - pTL.Y)
        End Using

        ' Perímetro de punzonamiento — a d/2 del pedestal
        ' Central: rectángulo cerrado
        ' Medianera: se abre el borde libre (asumido = borde IZQUIERDO -X, es decir el
        '            que da al exterior; los otros 3 lados llevan reacción del suelo)
        ' Esquinera: solo 2 lados (los interiores) — asumido esquina superior-izquierda
        Dim ppTL As PointF = W2S(-(b2 + d2), +(h2 + d2))
        Dim ppTR As PointF = W2S(+(b2 + d2), +(h2 + d2))
        Dim ppBR As PointF = W2S(+(b2 + d2), -(h2 + d2))
        Dim ppBL As PointF = W2S(-(b2 + d2), -(h2 + d2))
        Using penPz As New Pen(Color.FromArgb(200, 40, 160), 2)
            penPz.DashStyle = DashStyle.Dash
            Select Case _zapata.TipoApoyo
                Case eTipoApoyoZapata.Central
                    g.DrawPolygon(penPz, {ppTL, ppTR, ppBR, ppBL})
                Case eTipoApoyoZapata.Medianera
                    ' Cierra por arriba, derecha y abajo; abre el borde izquierdo
                    Dim ppTLopen As PointF = W2S(-b2, +(h2 + d2))
                    Dim ppBLopen As PointF = W2S(-b2, -(h2 + d2))
                    g.DrawLine(penPz, ppTLopen, ppTR)
                    g.DrawLine(penPz, ppTR, ppBR)
                    g.DrawLine(penPz, ppBR, ppBLopen)
                Case eTipoApoyoZapata.Esquinera
                    ' Solo los dos lados interiores (derecha y abajo, esquina superior-izq)
                    Dim aTop As PointF = W2S(-b2, +(h2 + d2))
                    Dim aRig As PointF = W2S(+(b2 + d2), +h2)
                    g.DrawLine(penPz, aTop, ppTR)
                    g.DrawLine(penPz, ppTR, aRig)
            End Select
        End Using

        ' Secciones críticas de cortante — a d de la cara del pedestal.
        ' Cada línea abarca todo el borde perpendicular de la zapata.
        Using penCr As New Pen(Color.FromArgb(230, 120, 20), 2.2F)
            Dim xR As Double = +(b2 + d)   ' línea vertical (crítica del ala derecha, Vu_1)
            Dim xL As Double = -(b2 + d)   ' línea vertical (crítica del ala izquierda, Vu_3)
            Dim yT As Double = +(h2 + d)   ' línea horizontal (crítica del ala superior, Vu_4)
            Dim yB As Double = -(h2 + d)   ' línea horizontal (crítica del ala inferior, Vu_2)
            If xR < Lb2 Then g.DrawLine(penCr, W2S(xR, +Lh2), W2S(xR, -Lh2))
            If -xL < Lb2 Then g.DrawLine(penCr, W2S(xL, +Lh2), W2S(xL, -Lh2))
            If yT < Lh2 Then g.DrawLine(penCr, W2S(-Lb2, yT), W2S(+Lb2, yT))
            If -yB < Lh2 Then g.DrawLine(penCr, W2S(-Lb2, yB), W2S(+Lb2, yB))
        End Using

        ' Secciones de flexión — en la cara del pedestal.
        Using penFx As New Pen(Color.FromArgb(30, 150, 60), 2.2F)
            g.DrawLine(penFx, W2S(-b2, +Lh2), W2S(-b2, -Lh2))
            g.DrawLine(penFx, W2S(+b2, +Lh2), W2S(+b2, -Lh2))
            g.DrawLine(penFx, W2S(-Lb2, +h2), W2S(+Lb2, +h2))
            g.DrawLine(penFx, W2S(-Lb2, -h2), W2S(+Lb2, -h2))
        End Using

        ' Etiquetas con Vu / Mu.
        ' Convención de VerificarCortante:
        '   Vu_1 = ala derecha (+X)   → sección crítica x = +b/2+d
        '   Vu_2 = ala inferior (-Y)  → sección crítica y = -h/2-d
        '   Vu_3 = ala izquierda (-X) → sección crítica x = -b/2-d
        '   Vu_4 = ala superior (+Y)  → sección crítica y = +h/2+d
        ' Convención de VerificarFlexion:
        '   Mu_1 = ala derecha (+X)   → cara del pedestal x = +b/2
        '   Mu_2 = ala inferior (-Y)  → cara del pedestal y = -h/2
        If res IsNot Nothing Then
            Using bNaranja As New SolidBrush(Color.FromArgb(180, 90, 10)),
                  bVerde As New SolidBrush(Color.FromArgb(20, 110, 40)),
                  bPunz As New SolidBrush(Color.FromArgb(160, 20, 130)),
                  bFondo As New SolidBrush(Color.FromArgb(230, 255, 255, 255)),
                  f As New Font("Segoe UI", 8.5!, FontStyle.Bold)
                ' Cortante — etiqueta pegada al lado correspondiente
                Dim eVu1 As PointF = W2S(+Lb2, 0) : DibujarEtiqueta(g, f, bNaranja, bFondo, eVu1.X + 6, eVu1.Y - 8, $"Vu1 = {res.Vu1_C:F0}")
                Dim eVu2 As PointF = W2S(0, -Lh2) : DibujarEtiqueta(g, f, bNaranja, bFondo, eVu2.X - 30, eVu2.Y + 4, $"Vu2 = {res.Vu2_C:F0}")
                Dim eVu3 As PointF = W2S(-Lb2, 0) : DibujarEtiqueta(g, f, bNaranja, bFondo, eVu3.X - 70, eVu3.Y - 8, $"Vu3 = {res.Vu3_C:F0}")
                Dim eVu4 As PointF = W2S(0, +Lh2) : DibujarEtiqueta(g, f, bNaranja, bFondo, eVu4.X - 30, eVu4.Y - 20, $"Vu4 = {res.Vu4_C:F0}")
                ' Flexión — etiqueta hacia el ala correspondiente
                Dim eMu1 As PointF = W2S(+b2, -Lh2 + 0.15) : DibujarEtiqueta(g, f, bVerde, bFondo, eMu1.X + 6, eMu1.Y, $"Mu1 = {res.Mu_1:F1}")
                Dim eMu2 As PointF = W2S(-Lb2 + 0.15, -h2) : DibujarEtiqueta(g, f, bVerde, bFondo, eMu2.X, eMu2.Y + 6, $"Mu2 = {res.Mu_2:F1}")
                ' Punzonamiento — nota interior sobre el pedestal
                Dim ePunz As PointF = W2S(0, +h2 + d2 + 0.05)
                DibujarEtiqueta(g, f, bPunz, bFondo, ePunz.X - 60, ePunz.Y - 16, $"Vu = {Math.Abs(res.Vu_p):F0}  φVc = {res.Vc_p:F0}")
            End Using
        End If

        ' Ejes de referencia
        Using penEje As New Pen(Color.DarkGray, 1),
              fEje As New Font("Segoe UI", 8.5!, FontStyle.Bold),
              bEje As New SolidBrush(Color.FromArgb(60, 60, 60))
            penEje.DashStyle = DashStyle.Dot
            g.DrawLine(penEje, W2S(-Lb2, 0), W2S(+Lb2, 0))
            g.DrawLine(penEje, W2S(0, -Lh2), W2S(0, +Lh2))
            Dim tipX As PointF = W2S(+Lb2, 0)
            Dim tipY As PointF = W2S(0, +Lh2)
            g.DrawString("+X", fEje, bEje, tipX.X + 4, tipX.Y - 7)
            g.DrawString("+Y", fEje, bEje, tipY.X - 18, tipY.Y - 14)
        End Using

        ' Título
        Using f As New Font("Segoe UI", 11, FontStyle.Bold),
              b As New SolidBrush(ColorARCO)
            Dim titulo As String = $"Secciones críticas — {ZapataService.NombreTipo(_zapata.TipoApoyo)}   " &
                                   $"d = {_zapata.d:F3} m"
            g.DrawString(titulo, f, b, 10, 8)
        End Using

    End Sub

    Private Sub DibujarEtiqueta(g As Graphics, f As Font, b As Brush, bg As Brush,
                                 x As Single, y As Single, txt As String)
        Dim sz As SizeF = g.MeasureString(txt, f)
        g.FillRectangle(bg, x - 2, y - 1, sz.Width + 4, sz.Height + 2)
        g.DrawString(txt, f, b, x, y)
    End Sub

    ' =========================================================================
    ' Panel con doble buffer, para pintado suave
    ' =========================================================================
    Private Class PanelDobleBuffer
        Inherits Panel
        Sub New()
            Me.DoubleBuffered = True
            Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                        ControlStyles.UserPaint Or
                        ControlStyles.OptimizedDoubleBuffer, True)
        End Sub
    End Class

End Class
