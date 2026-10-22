Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports ARCO.eNumeradores

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_Planta_Zapatas — planta de cimentación con pan / zoom / hover.
'
'  Desde oct-2026 el chrome visual (pan/zoom/grid/leyenda/ejes/escala/ficha) lo
'  provee PlantaCanvas. Este form solo mantiene lo específico de zapatas:
'  los tres modos de color, el dibujo del rectángulo y pedestal, la marca de
'  tipo manual y la navegación a Form_07_Zapata_Detalle con doble clic.
'
'  Tres modos de color, porque son tres preguntas distintas que el ingeniero se
'  hace mirando la misma planta:
'
'    Tipo de apoyo   dónde está cada zapata — es la revisión visual de la
'                    clasificación automática de medianeras y esquineras, que
'                    es justo lo que hay que verificar antes de confiar en el
'                    punzonamiento
'    Estado          cuáles cumplen y cuáles no
'    C/D             qué tan justas van las que cumplen
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Planta_Zapatas
    Inherits Form

    ' ── Datos del modelo ──────────────────────────────────────
    Public Property Zapatas As List(Of cZapata)
    Public Property GridLines As List(Of cGridLine)
    Public Property Seleccionada As cZapata
        Get
            Return CType(_canvas.SelectedElement, cZapata)
        End Get
        Set(value As cZapata)
            _canvas.SelectedElement = value
        End Set
    End Property

    Public Event ZapataSeleccionada(z As cZapata)

    ' Recorrer todas las combinaciones de todas las zapatas en cada repintado
    ' haría inusable el pan y el zoom.
    Private _resumenes As Dictionary(Of cZapata, ZapataService.ResumenZapata)

    Private Enum ModoColor
        TipoApoyo = 0
        Estado = 1
        FactorCD = 2
    End Enum

    ' ── Paleta ────────────────────────────────────────────────
    Private Shared ReadOnly ColorCentral As Color = Color.FromArgb(120, 42, 120, 214)
    Private Shared ReadOnly ColorMedianera As Color = Color.FromArgb(140, 237, 161, 0)
    Private Shared ReadOnly ColorEsquinera As Color = Color.FromArgb(150, 227, 73, 72)
    Private Shared ReadOnly ColorCumple As Color = Color.FromArgb(130, 27, 175, 122)
    Private Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(150, 227, 73, 72)
    Private Shared ReadOnly ColorSinCalcular As Color = Color.FromArgb(70, 150, 150, 150)
    Private Shared ReadOnly ColorAmbar As Color = Color.FromArgb(150, 237, 161, 0)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)

    Private _canvas As PlantaCanvas

    ' =====================================================================
    Public Sub New()

        Me.Text = "Planta de cimentación — Zapatas"
        Me.Size = New Size(1150, 780)
        Me.MinimumSize = New Size(640, 440)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = GrisARCO
        Me.Font = New Font("Segoe UI", 9)

        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try

        _canvas = New PlantaCanvas With {
            .Dock = DockStyle.Fill,
            .TituloVacio = "Sin coordenadas para dibujar la planta",
            .TextoAyuda = "Pasa el cursor sobre una zapata para ver su información; haz clic para seleccionarla.",
            .Modos = New List(Of String) From {
                "Color: tipo de apoyo",
                "Color: estado",
                "Color: factor C/D"
            }
        }

        _canvas.OnResolverBounds = AddressOf ResolverBounds
        _canvas.OnDrawElementos = AddressOf DibujarZapatas
        _canvas.OnHitTest = AddressOf HitZapata
        _canvas.OnBuildFicha = AddressOf BuildFicha
        _canvas.OnBuildLeyenda = AddressOf BuildLeyenda

        AddHandler _canvas.ElementoSeleccionado, AddressOf OnCanvasSeleccion
        AddHandler _canvas.ElementoDoubleClick, AddressOf OnCanvasDoubleClick
        AddHandler _canvas.ModoCambio, Sub(idx) _canvas.Refrescar()
        AddHandler Me.Shown,
            Sub()
                CalcularResumenes()
                _canvas.GridLines = GridLines
                _canvas.AjustarVista()
            End Sub

        Me.Controls.Add(_canvas)
    End Sub

    ' =====================================================================
    ' API pública
    ' =====================================================================
    Public Sub ActualizarSeleccion(z As cZapata)
        _canvas.SelectedElement = z
    End Sub

    Public Sub Refrescar()
        CalcularResumenes()
        _canvas.Refrescar()
    End Sub

    ' =====================================================================
    ' Callbacks al canvas
    ' =====================================================================
    Private ReadOnly Property Modo As ModoColor
        Get
            Return CType(_canvas.ModoActualIndex, ModoColor)
        End Get
    End Property

    Private Function Dibujables() As List(Of cZapata)
        If Zapatas Is Nothing Then Return New List(Of cZapata)()
        Return Zapatas.Where(Function(z) z.TieneCoordenadas).ToList()
    End Function

    Private Function ResolverBounds() As RectangleF
        Dim lista = Dibujables()
        If lista.Count = 0 Then Return RectangleF.Empty

        Dim minX = lista.Min(Function(z) z.CoordX - z.L_b / 2)
        Dim maxX = lista.Max(Function(z) z.CoordX + z.L_b / 2)
        Dim minY = lista.Min(Function(z) z.CoordY - z.L_h / 2)
        Dim maxY = lista.Max(Function(z) z.CoordY + z.L_h / 2)

        Return New RectangleF(CSng(minX), CSng(minY),
                              CSng(Math.Max(maxX - minX, 0.1)),
                              CSng(Math.Max(maxY - minY, 0.1)))
    End Function

    Private Function HitZapata(wx As Double, wy As Double) As Object
        For Each z In Dibujables()
            If Math.Abs(wx - z.CoordX) <= z.L_b / 2 AndAlso
               Math.Abs(wy - z.CoordY) <= z.L_h / 2 Then
                Return z
            End If
        Next
        Return Nothing
    End Function

    Private Function BuildFicha(el As Object) As IList(Of String)
        Dim z = TryCast(el, cZapata)
        If z Is Nothing Then Return New List(Of String)()

        Dim r = Resumen(z)
        Dim lineas As New List(Of String) From {
            $"{z.Label_joint}   {z.Nombre}",
            $"Zapata   {z.L_b:F2} x {z.L_h:F2} x {z.e:F2} m",
            $"Pedestal  {z.b:F2} x {z.h:F2} m",
            $"Apoyo    {ZapataService.NombreTipo(z.TipoApoyo)}{If(z.TipoApoyoManual, "  (fijado a mano)", "")}",
            $"alfa_s = {ZapataService.AlfaS(z.TipoApoyo):F0}   b0 = {ZapataService.PerimetroCritico(z.b, z.h, z.d, z.TipoApoyo):F2} m"
        }
        If r.TieneResultados Then
            lineas.Add($"Peor C/D  {r.PeorFactor:F2}   ({r.Revision})")
            lineas.Add($"Combinación  {r.Combinacion}")
        Else
            lineas.Add("Sin calcular")
        End If
        Return lineas
    End Function

    Private Function BuildLeyenda() As IList(Of PlantaLeyendaItem)
        Dim items As New List(Of PlantaLeyendaItem)
        Select Case Modo
            Case ModoColor.TipoApoyo
                Dim lista = Dibujables()
                items.Add(New PlantaLeyendaItem(ColorCentral, "Central",
                    lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central).Count))
                items.Add(New PlantaLeyendaItem(ColorMedianera, "Medianera",
                    lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera).Count))
                items.Add(New PlantaLeyendaItem(ColorEsquinera, "Esquinera",
                    lista.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera).Count))

            Case ModoColor.Estado
                items.Add(New PlantaLeyendaItem(ColorCumple, "Cumple"))
                items.Add(New PlantaLeyendaItem(ColorNoCumple, "No cumple"))
                items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular"))

            Case Else
                items.Add(New PlantaLeyendaItem(ColorCumple, "C/D >= 1.15"))
                items.Add(New PlantaLeyendaItem(ColorAmbar, $"C/D {Funciones_00_Varias.UMBRAL_CD:F2} a 1.15"))
                items.Add(New PlantaLeyendaItem(ColorNoCumple, $"C/D < {Funciones_00_Varias.UMBRAL_CD:F2}"))
                items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular"))
        End Select
        Return items
    End Function

    Private Sub OnCanvasSeleccion(el As Object)
        Dim z = TryCast(el, cZapata)
        If z IsNot Nothing Then RaiseEvent ZapataSeleccionada(z)
    End Sub

    Private Sub OnCanvasDoubleClick(el As Object)
        Dim z = TryCast(el, cZapata)
        If z Is Nothing Then Return
        Try
            RaiseEvent ZapataSeleccionada(z)
            Form_07_Zapata_Detalle.Mostrar(z, Me)
        Catch ex As Exception
            Logger.Error(ex, "Form_Planta_Zapatas.OnCanvasDoubleClick", z?.Label_joint)
        End Try
    End Sub

    ' =====================================================================
    ' Dibujo específico de las zapatas (rectángulo + pedestal + highlights)
    ' =====================================================================
    Private Sub DibujarZapatas(g As Graphics, t As PlantaTransform)
        Dim lista = Dibujables()
        If lista.Count = 0 Then Return

        Dim sel = TryCast(_canvas.SelectedElement, cZapata)
        Dim hov = TryCast(_canvas.HoverElement, cZapata)

        Using fEtiqueta As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              penBorde As New Pen(Color.FromArgb(90, 90, 90), 1.2F),
              penPedestal As New Pen(Color.FromArgb(70, 70, 70), 1.0F),
              penSelec As New Pen(Color.FromArgb(20, 90, 200), 2.5F),
              penHover As New Pen(Color.FromArgb(255, 140, 0), 2.0F)

            For Each z In lista
                Dim rect = t.W2SRect(z.CoordX, z.CoordY, z.L_b, z.L_h)
                If rect.Width < 0.5F OrElse rect.Height < 0.5F Then Continue For

                Using b As New SolidBrush(ColorDe(z))
                    g.FillRectangle(b, rect)
                End Using
                g.DrawRectangle(penBorde, rect.X, rect.Y, rect.Width, rect.Height)

                ' Pedestal: sin él no se ve dónde está la columna, que es
                ' justamente lo que define el perímetro de punzonamiento.
                If z.b > 0 AndAlso z.h > 0 AndAlso t.Zoom > 12 Then
                    Dim rp = t.W2SRect(z.CoordX, z.CoordY, z.b, z.h)
                    Using bp As New SolidBrush(Color.FromArgb(90, 60, 60, 60))
                        g.FillRectangle(bp, rp)
                    End Using
                    g.DrawRectangle(penPedestal, rp.X, rp.Y, rp.Width, rp.Height)
                End If

                ' Una zapata marcada a mano lleva una esquina achaflanada: se
                ' distingue de un vistazo de las que propuso el programa.
                If z.TipoApoyoManual Then DibujarMarcaManual(g, rect)

                If ReferenceEquals(z, sel) Then
                    g.DrawRectangle(penSelec, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4)
                ElseIf ReferenceEquals(z, hov) Then
                    g.DrawRectangle(penHover, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                End If

                If t.Zoom > 18 AndAlso Not String.IsNullOrEmpty(z.Label_joint) Then
                    Dim sz = g.MeasureString(z.Label_joint, fEtiqueta)
                    If sz.Width < rect.Width + 14 Then
                        g.DrawString(z.Label_joint, fEtiqueta, Brushes.Black,
                                     rect.X + rect.Width / 2 - sz.Width / 2,
                                     rect.Y + rect.Height + 1)
                    End If
                End If
            Next
        End Using
    End Sub

    Private Shared Sub DibujarMarcaManual(g As Graphics, rect As RectangleF)
        Dim lado As Single = Math.Min(10.0F, Math.Min(rect.Width, rect.Height) / 3.0F)
        If lado < 3 Then Exit Sub

        Using b As New SolidBrush(Color.FromArgb(200, 50, 50, 50))
            g.FillPolygon(b, New PointF() {
                New PointF(rect.Right - lado, rect.Y),
                New PointF(rect.Right, rect.Y),
                New PointF(rect.Right, rect.Y + lado)})
        End Using
    End Sub

    Private Function ColorDe(z As cZapata) As Color
        Select Case Modo
            Case ModoColor.TipoApoyo
                Select Case z.TipoApoyo
                    Case eTipoApoyoZapata.Esquinera : Return ColorEsquinera
                    Case eTipoApoyoZapata.Medianera : Return ColorMedianera
                    Case Else : Return ColorCentral
                End Select

            Case ModoColor.Estado
                Dim r = Resumen(z)
                If Not r.TieneResultados Then Return ColorSinCalcular
                Return If(r.Cumple, ColorCumple, ColorNoCumple)

            Case Else
                Return ColorPorFactor(Resumen(z))
        End Select
    End Function

    ''' <summary>
    ''' Rampa continua: rojo por debajo del umbral, ámbar justo encima, verde
    ''' cuando sobra capacidad. Las tres bandas son las mismas del semáforo de
    ''' los reportes, para que la planta y el Excel cuenten lo mismo.
    ''' </summary>
    Private Shared Function ColorPorFactor(r As ZapataService.ResumenZapata) As Color
        If Not r.TieneResultados Then Return ColorSinCalcular
        Dim f = r.PeorFactor
        If f < Funciones_00_Varias.UMBRAL_CD Then Return ColorNoCumple
        If f < 1.15 Then Return ColorAmbar
        Return ColorCumple
    End Function

    Private Sub CalcularResumenes()
        _resumenes = New Dictionary(Of cZapata, ZapataService.ResumenZapata)()
        If Zapatas Is Nothing Then Exit Sub
        For Each z In Zapatas
            _resumenes(z) = ZapataService.Resumir(z)
        Next
    End Sub

    Private Function Resumen(z As cZapata) As ZapataService.ResumenZapata
        If _resumenes Is Nothing Then Return New ZapataService.ResumenZapata()
        Dim r As ZapataService.ResumenZapata = Nothing
        If _resumenes.TryGetValue(z, r) Then Return r
        Return New ZapataService.ResumenZapata()
    End Function

End Class
