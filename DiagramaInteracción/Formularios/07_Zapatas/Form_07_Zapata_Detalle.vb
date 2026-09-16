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

        ' Rectángulo escalado de la zapata en pantalla, con margen para leyenda.
        Dim margL As Single = 40, margT As Single = 30, margR As Single = 70, margB As Single = 40
        Dim availW As Single = Math.Max(50, _pnlPresiones.Width - margL - margR)
        Dim availH As Single = Math.Max(50, _pnlPresiones.Height - margT - margB)
        Dim escala As Single = CSng(Math.Min(availW / _zapata.L_b, availH / _zapata.L_h))
        Dim wPx As Single = CSng(_zapata.L_b * escala)
        Dim hPx As Single = CSng(_zapata.L_h * escala)
        Dim x0 As Single = margL + (availW - wPx) / 2
        Dim y0 As Single = margT + (availH - hPx) / 2

        ' Para una zapata rígida bajo axial + biaxial la distribución de presión
        ' es un plano, así que basta interpolar bilinealmente los cuatro valores
        ' de esquina ya calculados (g1..g4). Así el mapa coincide exactamente
        ' con qMin, qMax y con lo que muestra la tabla — sin depender de una
        ' fórmula paramétrica que sería fácil equivocar.
        Dim qMin As Double = res.qMin
        Dim qMax As Double = res.qMax
        Dim rango As Double = qMax - qMin
        If rango < 0.0001 Then rango = 1.0

        Dim usarHeat As Boolean = _rdoHeatmap.Checked

        If usarHeat Then
            DibujarHeatmap(g, x0, y0, wPx, hPx, res, qMin, qMax)
        Else
            DibujarGrilla(g, x0, y0, wPx, hPx, res, qMin, qMax)
        End If

        ' Contorno de la zapata
        Using pen As New Pen(Color.FromArgb(60, 60, 60), 1.5F)
            g.DrawRectangle(pen, x0, y0, wPx, hPx)
        End Using

        ' Diagnóstico visual: si el rango entre esquinas es despreciable, se
        ' hace explícito en el dibujo por qué no se ve variación. Antes el
        ' usuario tenía que ir al panel lateral a mirar Mx y My.
        Dim rangoEsq As Double = Math.Max(res.g1, Math.Max(res.g2, Math.Max(res.g3, res.g4))) -
                                 Math.Min(res.g1, Math.Min(res.g2, Math.Min(res.g3, res.g4)))
        If rangoEsq < 0.5 Then
            Using fBig As New Font("Segoe UI", 12, FontStyle.Bold),
                  bBg As New SolidBrush(Color.FromArgb(230, 255, 255, 200)),
                  bTxt As New SolidBrush(Color.FromArgb(140, 40, 20)),
                  sf As New StringFormat() With {
                      .Alignment = StringAlignment.Center,
                      .LineAlignment = StringAlignment.Center
                  }
                Dim msg As String = $"Sin gradiente en esta combinación" & vbCrLf &
                                    $"Mx = {res.Mx_Entrada:F2}  |  My = {res.My_Entrada:F2} kN·m" & vbCrLf &
                                    $"q ≈ {res.qMax:F1} kN/m² en toda la huella"
                Dim rect As New RectangleF(x0 + 8, y0 + hPx / 2 - 40, wPx - 16, 80)
                g.FillRectangle(bBg, rect)
                Using penBox As New Pen(Color.FromArgb(180, 130, 50), 1.2F)
                    g.DrawRectangle(penBox, rect.X, rect.Y, rect.Width, rect.Height)
                End Using
                g.DrawString(msg, fBig, bTxt, rect, sf)
            End Using
        End If

        ' Pedestal como referencia
        If _zapata.b > 0 AndAlso _zapata.h > 0 Then
            Dim wp As Single = CSng(_zapata.b * escala)
            Dim hp As Single = CSng(_zapata.h * escala)
            Dim xp As Single = x0 + (wPx - wp) / 2
            Dim yp As Single = y0 + (hPx - hp) / 2
            Using bp As New SolidBrush(Color.FromArgb(90, 60, 60, 60)),
                  pp As New Pen(Color.FromArgb(50, 50, 50), 1.2F)
                g.FillRectangle(bp, xp, yp, wp, hp)
                g.DrawRectangle(pp, xp, yp, wp, hp)
            End Using
        End If

        ' Etiquetas de las esquinas con presión — la convención de gᵢ viene de
        ' la fórmula (ver InterpolarQ para el detalle): g1 TR, g2 BR, g3 BL, g4 TL.
        Using f As New Font("Segoe UI", 8, FontStyle.Bold),
              b As New SolidBrush(Color.Black),
              bWhite As New SolidBrush(Color.FromArgb(220, 255, 255, 255))
            DibujarEtiquetaEsquina(g, f, b, bWhite, x0 - 2, y0 - 2, $"g4 = {res.g4:F1}", True, True)
            DibujarEtiquetaEsquina(g, f, b, bWhite, x0 + wPx + 2, y0 - 2, $"g1 = {res.g1:F1}", False, True)
            DibujarEtiquetaEsquina(g, f, b, bWhite, x0 + wPx + 2, y0 + hPx + 2, $"g2 = {res.g2:F1}", False, False)
            DibujarEtiquetaEsquina(g, f, b, bWhite, x0 - 2, y0 + hPx + 2, $"g3 = {res.g3:F1}", True, False)
        End Using

        ' Escala vertical (leyenda)
        DibujarLeyendaHeatmap(g, x0 + wPx + 10, y0, hPx, qMin, qMax)

        ' Ejes locales
        Using penEje As New Pen(Color.DarkGray, 1),
              fEje As New Font("Segoe UI", 8, FontStyle.Italic),
              bEje As New SolidBrush(Color.DarkGray)
            Dim ex As Single = x0 + wPx / 2
            Dim ey As Single = y0 + hPx / 2
            penEje.DashStyle = DashStyle.Dot
            g.DrawLine(penEje, x0, ey, x0 + wPx, ey)
            g.DrawLine(penEje, ex, y0, ex, y0 + hPx)
            g.DrawString("X (L_b)", fEje, bEje, x0 + wPx + 14, y0 + hPx + 20)
            g.DrawString("Y (L_h)", fEje, bEje, x0 - 32, y0 - 18)
        End Using

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

    ''' <summary>Gradiente azul → verde → amarillo → rojo. Tracción (&lt;0) en magenta.</summary>
    Private Shared Function ColorPresion(q As Double, qMin As Double, qMax As Double) As Color

        If q < 0 Then
            ' Tracción intensa. VB.NET es case-insensitive: r y R en la misma
            ' función son el mismo nombre, así que este canal se llama distinto.
            Dim t As Double = Math.Min(1.0, Math.Abs(q) / Math.Max(0.001, Math.Abs(qMin)))
            Dim rojo As Integer = 200 + CInt(55 * t)
            Return Color.FromArgb(255, rojo, 40, 200 - CInt(120 * t))
        End If

        ' Base del gradiente: cuando toda la zapata está en compresión, el azul
        ' arranca en qMin real. Cuando hay tracción (qMin < 0), la parte positiva
        ' del gradiente arranca en 0 y la negativa se pinta con la rama magenta
        ' de arriba.
        Dim baseQ As Double = Math.Max(0.0, qMin)
        Dim rango As Double = qMax - baseQ
        If rango < 0.0001 Then Return Color.FromArgb(255, 120, 220, 120)

        Dim frac As Double = Math.Max(0, Math.Min(1, (q - baseQ) / rango))

        Dim R As Integer, G As Integer, B As Integer
        If frac < 0.33 Then
            Dim t As Double = frac / 0.33
            R = CInt(30 + (60 - 30) * t)
            G = CInt(100 + (200 - 100) * t)
            B = CInt(220 - (220 - 90) * t)
        ElseIf frac < 0.66 Then
            Dim t As Double = (frac - 0.33) / 0.33
            R = CInt(60 + (240 - 60) * t)
            G = CInt(200 + (220 - 200) * t)
            B = CInt(90 - 60 * t)
        Else
            Dim t As Double = (frac - 0.66) / 0.34
            R = CInt(240 + (215 - 240) * t)
            G = CInt(220 - (220 - 40) * t)
            B = CInt(30 - 30 * t)
        End If
        R = Math.Max(0, Math.Min(255, R))
        G = Math.Max(0, Math.Min(255, G))
        B = Math.Max(0, Math.Min(255, B))
        Return Color.FromArgb(255, R, G, B)

    End Function

    Private Sub DibujarLeyendaHeatmap(g As Graphics, x As Single, y As Single, alto As Single,
                                       qMin As Double, qMax As Double)
        Dim ancho As Single = 18
        Dim pasos As Integer = 60
        Dim dyLoc As Single = alto / pasos
        For k As Integer = 0 To pasos - 1
            Dim frac As Double = 1.0 - k / CDbl(pasos - 1)
            Dim q As Double = Math.Max(0, qMin) + frac * (qMax - Math.Max(0, qMin))
            Using b As New SolidBrush(ColorPresion(q, qMin, qMax))
                g.FillRectangle(b, x, y + k * dyLoc, ancho, dyLoc + 0.5F)
            End Using
        Next
        Using pen As New Pen(Color.DimGray, 1)
            g.DrawRectangle(pen, x, y, ancho, alto)
        End Using
        Using f As New Font("Segoe UI", 7.5F),
              b As New SolidBrush(Color.Black)
            g.DrawString($"{qMax:F1}", f, b, x + ancho + 3, y - 4)
            g.DrawString($"{Math.Max(0, qMin):F1}", f, b, x + ancho + 3, y + alto - 8)
            g.DrawString("kN/m²", f, b, x - 5, y + alto + 4)
        End Using
    End Sub

    Private Sub DibujarEtiquetaEsquina(g As Graphics, f As Font, b As Brush, bg As Brush,
                                        x As Single, y As Single, txt As String,
                                        alaIzquierda As Boolean, arriba As Boolean)
        Dim sz As SizeF = g.MeasureString(txt, f)
        Dim rx As Single = If(alaIzquierda, x - sz.Width - 3, x)
        Dim ry As Single = If(arriba, y - sz.Height - 2, y + 2)
        g.FillRectangle(bg, rx, ry, sz.Width + 4, sz.Height + 2)
        g.DrawString(txt, f, b, rx + 2, ry + 1)
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

        ' Zapata
        Using bZap As New SolidBrush(Color.FromArgb(30, 120, 170, 220)),
              penZap As New Pen(Color.FromArgb(40, 90, 140), 1.5F)
            g.FillRectangle(bZap, x0, y0, wPx, hPx)
            g.DrawRectangle(penZap, x0, y0, wPx, hPx)
        End Using

        ' Pedestal
        Dim bPx As Single = CSng(_zapata.b * escala)
        Dim hpPx As Single = CSng(_zapata.h * escala)
        Dim xp As Single = x0 + (wPx - bPx) / 2
        Dim yp As Single = y0 + (hPx - hpPx) / 2
        Using bp As New SolidBrush(Color.FromArgb(150, 70, 70, 70)),
              penp As New Pen(Color.FromArgb(30, 30, 30), 1.2F)
            g.FillRectangle(bp, xp, yp, bPx, hpPx)
            g.DrawRectangle(penp, xp, yp, bPx, hpPx)
        End Using

        Dim dPx As Single = CSng(_zapata.d * escala)
        Dim dMedPx As Single = dPx / 2

        ' Perímetro de punzonamiento (líneas magenta a d/2 del pedestal)
        Using penPz As New Pen(Color.FromArgb(200, 40, 160), 2)
            penPz.DashStyle = DashStyle.Dash
            Dim xL As Single = xp - dMedPx
            Dim xR As Single = xp + bPx + dMedPx
            Dim yT As Single = yp - dMedPx
            Dim yB As Single = yp + hpPx + dMedPx

            Select Case _zapata.TipoApoyo
                Case eTipoApoyoZapata.Central
                    g.DrawLine(penPz, xL, yT, xR, yT)
                    g.DrawLine(penPz, xR, yT, xR, yB)
                    g.DrawLine(penPz, xR, yB, xL, yB)
                    g.DrawLine(penPz, xL, yB, xL, yT)
                Case eTipoApoyoZapata.Medianera
                    ' Se toma el borde derecho como interior: se abre el lado izquierdo
                    g.DrawLine(penPz, xp, yT, xR, yT)
                    g.DrawLine(penPz, xR, yT, xR, yB)
                    g.DrawLine(penPz, xR, yB, xp, yB)
                Case eTipoApoyoZapata.Esquinera
                    ' Solo dos lados: derecha y abajo, como esquina superior-izquierda
                    g.DrawLine(penPz, xp + bPx / 2, yp, xR, yp)
                    g.DrawLine(penPz, xR, yp, xR, yp + hpPx / 2)
            End Select
        End Using

        ' Secciones críticas de cortante (a d de la cara)
        Using penCr As New Pen(Color.FromArgb(230, 120, 20), 2.2F)
            Dim xIzq As Single = xp - dPx
            Dim xDer As Single = xp + bPx + dPx
            Dim yArr As Single = yp - dPx
            Dim yAba As Single = yp + hpPx + dPx
            If xIzq > x0 Then g.DrawLine(penCr, xIzq, y0, xIzq, y0 + hPx)
            If xDer < x0 + wPx Then g.DrawLine(penCr, xDer, y0, xDer, y0 + hPx)
            If yArr > y0 Then g.DrawLine(penCr, x0, yArr, x0 + wPx, yArr)
            If yAba < y0 + hPx Then g.DrawLine(penCr, x0, yAba, x0 + wPx, yAba)
        End Using

        ' Secciones de flexión (en la cara del pedestal)
        Using penFx As New Pen(Color.FromArgb(30, 150, 60), 2.2F)
            g.DrawLine(penFx, xp, y0, xp, y0 + hPx)
            g.DrawLine(penFx, xp + bPx, y0, xp + bPx, y0 + hPx)
            g.DrawLine(penFx, x0, yp, x0 + wPx, yp)
            g.DrawLine(penFx, x0, yp + hpPx, x0 + wPx, yp + hpPx)
        End Using

        ' Etiquetas con Vu, Mu de la combinación
        If res IsNot Nothing Then
            Using f As New Font("Segoe UI", 8.5!, FontStyle.Bold),
                  bNaranja As New SolidBrush(Color.FromArgb(180, 90, 10)),
                  bVerde As New SolidBrush(Color.FromArgb(20, 110, 40)),
                  bPunz As New SolidBrush(Color.FromArgb(160, 20, 130)),
                  bFondo As New SolidBrush(Color.FromArgb(230, 255, 255, 255))
                ' Vu de cortante (4 lados)
                DibujarEtiqueta(g, f, bNaranja, bFondo, x0 - 40, y0 + hPx / 2, $"Vu4 = {res.Vu4_C:F0}")
                DibujarEtiqueta(g, f, bNaranja, bFondo, x0 + wPx + 6, y0 + hPx / 2, $"Vu2 = {res.Vu2_C:F0}")
                DibujarEtiqueta(g, f, bNaranja, bFondo, x0 + wPx / 2 - 40, y0 - 18, $"Vu1 = {res.Vu1_C:F0}")
                DibujarEtiqueta(g, f, bNaranja, bFondo, x0 + wPx / 2 - 40, y0 + hPx + 4, $"Vu3 = {res.Vu3_C:F0}")
                ' Mu flexión
                DibujarEtiqueta(g, f, bVerde, bFondo, xp + bPx + 4, y0 + 6, $"Mu2 = {res.Mu_2:F1}")
                DibujarEtiqueta(g, f, bVerde, bFondo, x0 + 4, yp + hpPx + 4, $"Mu1 = {res.Mu_1:F1}")
                ' Punzonamiento
                DibujarEtiqueta(g, f, bPunz, bFondo, xp - 6, yp - 22, $"Vu = {Math.Abs(res.Vu_p):F0}  φVc = {res.Vc_p:F0}")
            End Using
        End If

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
