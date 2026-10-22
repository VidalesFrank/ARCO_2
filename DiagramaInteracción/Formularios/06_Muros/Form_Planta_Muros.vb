Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports ARCO.eNumeradores

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_Planta_Muros — vista en planta por piso. Cada muro es un rectángulo
'  tw×Lw escalado y orientado por Direccion_Muro (X u Y según la convención
'  del módulo: X = corre a lo largo del eje global X; Y = corre en Y).
'
'  Dos modos de color:
'    Estado      cumple C/D ≥ 0.90 (verde) / no cumple (rojo) / sin cálculo (gris).
'    Factor C/D  rampa 3 bandas, igual criterio que los reportes.
'
'  C/D compuesto por sección = min(peor flexo-compresión, cortante):
'    Flex   min(Factor_Demanda_Flexo_Top, Factor_Demanda_Flexo_Bot)   [C/D]
'    Cort   F_Cortante                                                [C/D]
'  Misma convención que MurosResumenService: valores > 0 y < 99 = calculado;
'  fuera de ese rango = sin cálculo (NO = no cumple).
'
'  Limitación conocida: Direccion_Muro es un enum X/Y (no un ángulo real),
'  así que un muro oblicuo se dibuja alineado al eje más cercano. Si en el
'  futuro se parsea el ángulo real del E2K, cambia solo el helper DimOrientadas.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Planta_Muros
    Inherits Form

    Public Property Proyecto As Proyecto
    Public Property GridLines As List(Of cGridLine)
    Public Property Seleccionado As Muro
        Get
            Return TryCast(_canvas.SelectedElement, Muro)
        End Get
        Set(value As Muro)
            _canvas.SelectedElement = value
        End Set
    End Property

    Public Event MuroSeleccionado(m As Muro)

    Private _canvas As PlantaCanvas
    Private _cmbPiso As ComboBox
    Private _lblPiso As Label
    Private _reporte As GeometriaPlantasService.ReporteFaltantes

    Private Enum ModoColor
        Estado = 0
        FactorCD = 1
    End Enum

    ' ── Paleta ────────────────────────────────────────────────
    Private Shared ReadOnly ColorCumple As Color = Color.FromArgb(180, 27, 175, 122)
    Private Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(190, 227, 73, 72)
    Private Shared ReadOnly ColorAmbar As Color = Color.FromArgb(200, 237, 161, 0)
    Private Shared ReadOnly ColorSinCalcular As Color = Color.FromArgb(90, 150, 150, 150)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly ColorBoton As Color = Color.FromArgb(224, 224, 224)

    ' =====================================================================
    Public Sub New()

        Me.Text = "Planta de Muros"
        Me.Size = New Size(1200, 820)
        Me.MinimumSize = New Size(700, 480)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = GrisARCO
        Me.Font = New Font("Segoe UI", 9)

        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        ' ── Panel superior con selector de piso ─────────────────────
        Dim pnlPiso As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 42,
            .BackColor = GrisARCO,
            .Padding = New Padding(12, 8, 12, 8)
        }

        _lblPiso = New Label With {
            .Location = New Point(14, 11),
            .Size = New Size(50, 22),
            .Text = "Piso:",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }

        _cmbPiso = New ComboBox With {
            .Location = New Point(60, 8),
            .Size = New Size(220, 28),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .FlatStyle = FlatStyle.Flat,
            .BackColor = ColorBoton,
            .ForeColor = GrisARCO,
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler _cmbPiso.SelectedIndexChanged, Sub() _canvas.Refrescar()

        pnlPiso.Controls.Add(_lblPiso)
        pnlPiso.Controls.Add(_cmbPiso)

        ' ── Canvas ─────────────────────────────────────────────────
        _canvas = New PlantaCanvas With {
            .Dock = DockStyle.Fill,
            .TituloVacio = "Sin muros para dibujar en este piso",
            .TextoAyuda = "Pasa el cursor sobre un muro para ver sus resultados; haz clic para seleccionarlo.",
            .Modos = New List(Of String) From {
                "Color: estado",
                "Color: factor C/D"
            }
        }

        _canvas.OnResolverBounds = AddressOf ResolverBounds
        _canvas.OnDrawElementos = AddressOf DibujarMuros
        _canvas.OnHitTest = AddressOf HitMuro
        _canvas.OnBuildFicha = AddressOf BuildFicha
        _canvas.OnBuildLeyenda = AddressOf BuildLeyenda

        AddHandler _canvas.ElementoSeleccionado, AddressOf OnCanvasSeleccion
        AddHandler _canvas.ModoCambio, Sub(idx) _canvas.Refrescar()
        AddHandler Me.Shown,
            Sub()
                ResolverGeometria()
                PoblarComboPiso()
                If _cmbPiso.Items.Count > 0 Then _cmbPiso.SelectedIndex = 0
                _canvas.GridLines = GridLines
                _canvas.AjustarVista()
            End Sub

        ' El canvas se agrega PRIMERO (Fill) y el panel Top DESPUÉS para que
        ' el Dock=Top se posicione correctamente arriba del Fill.
        Me.Controls.Add(_canvas)
        Me.Controls.Add(pnlPiso)
    End Sub

    ' =====================================================================
    ' Resolución y data externa
    ' =====================================================================
    Private Sub ResolverGeometria()
        If Proyecto Is Nothing Then
            _canvas.MensajeVacio = "Primero importá los muros y calculá verificaciones."
            Return
        End If

        _reporte = GeometriaPlantasService.ResolverYValidar(Proyecto)

        If Not _reporte.PlantaMurosListaParaDibujar Then
            Dim msg = _reporte.MensajePara("muros")
            _canvas.MensajeVacio = If(String.IsNullOrEmpty(msg),
                                      "No hay muros con coordenadas para dibujar.",
                                      msg)
        End If
    End Sub

    Private Sub PoblarComboPiso()
        _cmbPiso.Items.Clear()
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Muros Is Nothing OrElse
           Proyecto.Elementos.Muros.Lista_Muros Is Nothing Then
            Return
        End If

        Dim pisos = Proyecto.Elementos.Muros.Lista_Muros.
                    Where(Function(m) m IsNot Nothing AndAlso m.Lista_Secciones IsNot Nothing).
                    SelectMany(Function(m) m.Lista_Secciones.Select(Function(s) s.Piso)).
                    Where(Function(p) Not String.IsNullOrWhiteSpace(p)).
                    Distinct().
                    ToList()

        For Each p In pisos
            _cmbPiso.Items.Add(p)
        Next
    End Sub

    Private ReadOnly Property PisoSeleccionado As String
        Get
            If _cmbPiso Is Nothing OrElse _cmbPiso.SelectedItem Is Nothing Then Return ""
            Return _cmbPiso.SelectedItem.ToString()
        End Get
    End Property

    Private Function MurosDibujables() As List(Of Muro)
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Muros Is Nothing OrElse
           Proyecto.Elementos.Muros.Lista_Muros Is Nothing Then
            Return New List(Of Muro)
        End If
        ' TieneCoordenadas efectiva: Coor_X/Y seteados y Lw > 0.
        Return Proyecto.Elementos.Muros.Lista_Muros.Where(
            Function(m) m IsNot Nothing AndAlso
                        Not (m.Coor_X = 0 AndAlso m.Coor_Y = 0) AndAlso
                        m.Lw > 0).ToList()
    End Function

    ''' <summary>
    ''' Devuelve el ancho y alto en el sistema global del rectángulo que
    ''' representa al muro, según su dirección. Direccion.X = corre en X,
    ''' entonces el ancho (en X) es Lw y el alto (en Y) es tw. En Y, al revés.
    ''' </summary>
    Private Shared Sub DimOrientadas(sec As SeccionMuro,
                                     ByRef anchoW As Single, ByRef altoW As Single)
        If sec.Direccion_Muro = eDireccion.X Then
            anchoW = sec.Lw_Planos
            altoW = sec.tw_Planos
        Else
            anchoW = sec.tw_Planos
            altoW = sec.Lw_Planos
        End If
    End Sub

    ' =====================================================================
    ' Callbacks al canvas
    ' =====================================================================
    Private ReadOnly Property Modo As ModoColor
        Get
            Return CType(_canvas.ModoActualIndex, ModoColor)
        End Get
    End Property

    Private Function ResolverBounds() As RectangleF
        Dim pisoSel = PisoSeleccionado
        If String.IsNullOrEmpty(pisoSel) Then Return RectangleF.Empty

        Dim minX = Single.MaxValue, minY = Single.MaxValue
        Dim maxX = Single.MinValue, maxY = Single.MinValue
        Dim encontrado As Boolean = False

        For Each m In MurosDibujables()
            Dim sec = SeccionDelPiso(m, pisoSel)
            If sec Is Nothing OrElse sec.Lw_Planos <= 0 Then Continue For

            Dim anchoW As Single, altoW As Single
            DimOrientadas(sec, anchoW, altoW)
            Dim x1 = m.Coor_X - anchoW / 2
            Dim x2 = m.Coor_X + anchoW / 2
            Dim y1 = m.Coor_Y - altoW / 2
            Dim y2 = m.Coor_Y + altoW / 2

            If x1 < minX Then minX = x1
            If x2 > maxX Then maxX = x2
            If y1 < minY Then minY = y1
            If y2 > maxY Then maxY = y2
            encontrado = True
        Next

        If Not encontrado Then Return RectangleF.Empty

        Return New RectangleF(minX, minY,
                              Math.Max(maxX - minX, 0.1F),
                              Math.Max(maxY - minY, 0.1F))
    End Function

    Private Function HitMuro(wx As Double, wy As Double) As Object
        Dim pisoSel = PisoSeleccionado
        If String.IsNullOrEmpty(pisoSel) Then Return Nothing

        For Each m In MurosDibujables()
            Dim sec = SeccionDelPiso(m, pisoSel)
            If sec Is Nothing Then Continue For

            Dim anchoW As Single, altoW As Single
            DimOrientadas(sec, anchoW, altoW)
            If Math.Abs(wx - m.Coor_X) <= anchoW / 2 AndAlso
               Math.Abs(wy - m.Coor_Y) <= altoW / 2 Then
                Return m
            End If
        Next
        Return Nothing
    End Function

    Private Function BuildFicha(el As Object) As IList(Of String)
        Dim m = TryCast(el, Muro)
        If m Is Nothing Then Return New List(Of String)()

        Dim pisoSel = PisoSeleccionado
        Dim sec = SeccionDelPiso(m, pisoSel)

        Dim lineas As New List(Of String) From {
            $"{m.Label}   (piso {pisoSel})"
        }

        If sec Is Nothing Then
            lineas.Add("Sin sección en este piso")
            Return lineas
        End If

        lineas.Add($"Dirección: {sec.Direccion_Muro}   tw = {sec.tw_Planos:F2} m   Lw = {sec.Lw_Planos:F2} m")
        lineas.Add($"fc = {sec.fc:F0} MPa   Vu = {sec.Vu:F1} kN")

        Dim flexMin As Single = Math.Min(sec.Factor_Demanda_Flexo_Top, sec.Factor_Demanda_Flexo_Bot)
        If flexMin > 0 AndAlso flexMin < 99 Then
            lineas.Add($"Flexo-compresión  C/D = {flexMin:F2}   (Top {sec.Factor_Demanda_Flexo_Top:F2} / Bot {sec.Factor_Demanda_Flexo_Bot:F2})")
        Else
            lineas.Add("Flexo-compresión  sin cálculo")
        End If

        If sec.F_Cortante > 0 AndAlso sec.F_Cortante < 99 Then
            lineas.Add($"Cortante  C/D = {sec.F_Cortante:F2}")
        Else
            lineas.Add("Cortante  sin cálculo")
        End If

        Dim peor = PeorCD(sec)
        If peor > 0 Then
            Dim veredicto = If(peor >= Funciones_00_Varias.UMBRAL_CD, "cumple", "no cumple")
            lineas.Add($"Peor C/D de la sección  {peor:F2}   ({veredicto})")
        End If

        Return lineas
    End Function

    Private Function BuildLeyenda() As IList(Of PlantaLeyendaItem)
        Dim items As New List(Of PlantaLeyendaItem)
        Dim pisoSel = PisoSeleccionado
        Dim lista = MurosDibujables()

        Select Case Modo
            Case ModoColor.Estado
                Dim nOk As Integer = 0, nMal As Integer = 0, nSinCalc As Integer = 0
                For Each m In lista
                    Dim sec = SeccionDelPiso(m, pisoSel)
                    If sec Is Nothing Then Continue For
                    Dim p = PeorCD(sec)
                    If p < 0 Then
                        nSinCalc += 1
                    ElseIf p >= Funciones_00_Varias.UMBRAL_CD Then
                        nOk += 1
                    Else
                        nMal += 1
                    End If
                Next
                items.Add(New PlantaLeyendaItem(ColorCumple, "Cumple", nOk))
                items.Add(New PlantaLeyendaItem(ColorNoCumple, "No cumple", nMal))
                items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular", nSinCalc))

            Case Else
                items.Add(New PlantaLeyendaItem(ColorCumple, "C/D >= 1.15"))
                items.Add(New PlantaLeyendaItem(ColorAmbar, $"C/D {Funciones_00_Varias.UMBRAL_CD:F2} a 1.15"))
                items.Add(New PlantaLeyendaItem(ColorNoCumple, $"C/D < {Funciones_00_Varias.UMBRAL_CD:F2}"))
                items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular"))
        End Select
        Return items
    End Function

    Private Sub OnCanvasSeleccion(el As Object)
        Dim m = TryCast(el, Muro)
        If m IsNot Nothing Then RaiseEvent MuroSeleccionado(m)
    End Sub

    ' =====================================================================
    ' Dibujo específico de muros
    ' =====================================================================
    Private Sub DibujarMuros(g As Graphics, t As PlantaTransform)
        Dim pisoSel = PisoSeleccionado
        If String.IsNullOrEmpty(pisoSel) Then Return

        Dim sel = TryCast(_canvas.SelectedElement, Muro)
        Dim hov = TryCast(_canvas.HoverElement, Muro)

        Using fEtiqueta As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              penBorde As New Pen(Color.FromArgb(70, 70, 70), 1.2F),
              penSelec As New Pen(Color.FromArgb(20, 90, 200), 2.5F),
              penHover As New Pen(Color.FromArgb(255, 140, 0), 2.0F)

            For Each m In MurosDibujables()
                Dim sec = SeccionDelPiso(m, pisoSel)
                If sec Is Nothing OrElse sec.Lw_Planos <= 0 Then Continue For

                Dim anchoW As Single, altoW As Single
                DimOrientadas(sec, anchoW, altoW)
                Dim rect = t.W2SRect(m.Coor_X, m.Coor_Y, anchoW, altoW)

                ' Umbral de visibilidad: si el rect es muy chico, lo expandimos a
                ' un punto mínimo para no perder el muro en zoom bajo.
                If rect.Width < 1.5F AndAlso rect.Height < 1.5F Then
                    rect = New RectangleF(rect.X - 2, rect.Y - 2, 4, 4)
                End If

                ' Sombra sutil
                Using bSombra As New SolidBrush(Color.FromArgb(50, 0, 0, 0))
                    g.FillRectangle(bSombra, rect.X + 1.5F, rect.Y + 1.5F, rect.Width, rect.Height)
                End Using

                Using b As New SolidBrush(ColorDe(sec))
                    g.FillRectangle(b, rect)
                End Using
                g.DrawRectangle(penBorde, rect.X, rect.Y, rect.Width, rect.Height)

                If ReferenceEquals(m, sel) Then
                    g.DrawRectangle(penSelec, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4)
                ElseIf ReferenceEquals(m, hov) Then
                    g.DrawRectangle(penHover, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                End If

                ' Label dentro del rectángulo si cabe, abajo si no.
                If Not String.IsNullOrEmpty(m.Label) Then
                    Dim sz = g.MeasureString(m.Label, fEtiqueta)
                    If sz.Width < rect.Width - 4 AndAlso sz.Height < rect.Height - 2 Then
                        g.DrawString(m.Label, fEtiqueta, Brushes.White,
                                     rect.X + rect.Width / 2 - sz.Width / 2,
                                     rect.Y + rect.Height / 2 - sz.Height / 2)
                    ElseIf t.Zoom > 10 AndAlso sz.Width < 160 Then
                        g.DrawString(m.Label, fEtiqueta, Brushes.Black,
                                     rect.X + rect.Width / 2 - sz.Width / 2,
                                     rect.Y + rect.Height + 2)
                    End If
                End If
            Next
        End Using
    End Sub

    Private Function ColorDe(sec As SeccionMuro) As Color
        Dim peor = PeorCD(sec)
        Select Case Modo
            Case ModoColor.Estado
                If peor < 0 Then Return ColorSinCalcular
                Return If(peor >= Funciones_00_Varias.UMBRAL_CD, ColorCumple, ColorNoCumple)
            Case Else
                If peor < 0 Then Return ColorSinCalcular
                If peor < Funciones_00_Varias.UMBRAL_CD Then Return ColorNoCumple
                If peor < 1.15 Then Return ColorAmbar
                Return ColorCumple
        End Select
    End Function

    ''' <summary>
    ''' Peor C/D compuesto de una sección: mínimo entre peor flexo-compresión y
    ''' cortante. Devuelve -1 si ningún chequeo se calculó — misma convención
    ''' que MurosResumenService.CalcularFila (FFlexMin/FCortMin = -1).
    ''' </summary>
    Private Shared Function PeorCD(sec As SeccionMuro) As Single
        Dim valores As New List(Of Single)

        Dim flexMin As Single = Math.Min(sec.Factor_Demanda_Flexo_Top, sec.Factor_Demanda_Flexo_Bot)
        If flexMin > 0 AndAlso flexMin < 99 Then valores.Add(flexMin)
        If sec.F_Cortante > 0 AndAlso sec.F_Cortante < 99 Then valores.Add(sec.F_Cortante)

        If valores.Count = 0 Then Return -1
        Return valores.Min()
    End Function

    Private Shared Function SeccionDelPiso(m As Muro, piso As String) As SeccionMuro
        If m Is Nothing OrElse m.Lista_Secciones Is Nothing Then Return Nothing
        Return m.Lista_Secciones.FirstOrDefault(Function(s) s IsNot Nothing AndAlso s.Piso = piso)
    End Function

End Class
