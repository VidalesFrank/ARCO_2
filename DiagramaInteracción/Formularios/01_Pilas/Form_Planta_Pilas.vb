Imports System.Drawing
Imports System.Drawing.Drawing2D

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_Planta_Pilas — planta de cimentación con las pilas dibujadas a escala.
'
'  Cada pila es un CÍRCULO de diámetro Dc (el diámetro real de la sección en el
'  plano de cimentación), posicionada en (CoordX, CoordY) resueltas desde los
'  Joints importados por GeometriaPlantasService.
'
'  Dos modos de color, equivalentes a los que tiene la planta de Zapatas:
'
'    Estado      cumple el conjunto de chequeos de esfuerzo (verde),
'                no cumple (rojo), o sin calcular (gris).
'    Factor C/D  rampa continua: verde si C/D ≥ 1.15, ámbar entre 0.90 y 1.15,
'                rojo bajo 0.90 (UMBRAL_CD). Igual criterio que los reportes
'                para que planta y Excel cuenten lo mismo.
'
'  Sentinela: C/D = 0 significa "no aplica", no "no cumple" — misma convención
'  que todo el módulo (PilaService.MinChequeoEsfuerzos, Form_Graficos_Pilas).
'
'  Si no hay nodos importados o ninguna pila cruza con su label, el canvas
'  muestra un mensaje explicando qué falta.
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_Planta_Pilas
    Inherits Form

    Public Property Proyecto As Proyecto
    Public Property GridLines As List(Of cGridLine)
    Public Property Seleccionada As Elemento_Pila
        Get
            Return TryCast(_canvas.SelectedElement, Elemento_Pila)
        End Get
        Set(value As Elemento_Pila)
            _canvas.SelectedElement = value
        End Set
    End Property

    Public Event PilaSeleccionada(p As Elemento_Pila)

    Private _canvas As PlantaCanvas
    Private _reporte As GeometriaPlantasService.ReporteFaltantes

    ' Modos de visualización por chequeo. "Peor global" compara los 4; el resto
    ' aísla un chequeo específico. Mismo orden que la enum.
    Private Enum ModoColor
        PeorGlobal = 0
        EsfuerzosConcreto = 1
        EsfuerzosSuelo = 2
        Cortante = 3
        FlexoCompresion = 4
    End Enum

    Private Shared ReadOnly ModoLabels As String() = {
        "Chequeo: peor global",
        "Chequeo: esfuerzos en concreto",
        "Chequeo: esf. transmitidos al suelo",
        "Chequeo: cortante",
        "Chequeo: flexo-compresión"
    }

    ' ── Paleta ────────────────────────────────────────────────
    Private Shared ReadOnly ColorCumple As Color = Color.FromArgb(180, 27, 175, 122)
    Private Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(190, 227, 73, 72)
    Private Shared ReadOnly ColorAmbar As Color = Color.FromArgb(200, 237, 161, 0)
    Private Shared ReadOnly ColorSinCalcular As Color = Color.FromArgb(90, 150, 150, 150)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)

    ' =====================================================================
    Public Sub New()
        Me.Text = "Planta de cimentación — Pilas"
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
            .TituloVacio = "Sin pilas para dibujar",
            .TextoAyuda = "Pasa el cursor sobre una pila para ver sus resultados; haz clic para seleccionarla.",
            .Modos = New List(Of String)(ModoLabels)
        }

        _canvas.OnResolverBounds = AddressOf ResolverBounds
        _canvas.OnDrawElementos = AddressOf DibujarPilas
        _canvas.OnHitTest = AddressOf HitPila
        _canvas.OnBuildFicha = AddressOf BuildFicha
        _canvas.OnBuildLeyenda = AddressOf BuildLeyenda

        AddHandler _canvas.ElementoSeleccionado, AddressOf OnCanvasSeleccion
        AddHandler _canvas.ModoCambio, Sub(idx) _canvas.Refrescar()
        AddHandler Me.Shown,
            Sub()
                ResolverGeometria()
                _canvas.GridLines = GridLines
                _canvas.AjustarVista()
            End Sub

        Me.Controls.Add(_canvas)
    End Sub

    ' =====================================================================
    ' Resolución de geometría y data externa
    ' =====================================================================
    Private Sub ResolverGeometria()
        If Proyecto Is Nothing Then
            _canvas.MensajeVacio = "Primero importá y calculá las pilas."
            _canvas.OcultarAviso()
            Return
        End If

        _reporte = GeometriaPlantasService.ResolverYValidar(Proyecto)

        If Not _reporte.PlantaPilasListaParaDibujar Then
            Dim msg = _reporte.MensajePara("pilas")
            _canvas.MensajeVacio = If(String.IsNullOrEmpty(msg),
                                      "No hay pilas con coordenadas para dibujar.",
                                      msg)
        End If

        ' Si hay pilas sin coords (típico de proyectos viejos con Pier), ofrecer
        ' recargar el Excel ETABS. Se dispara aun cuando algunas sí tienen coords
        ' para que el usuario sepa que el edificio está incompleto.
        If _reporte.PilasSinCoord > 0 Then
            Dim n = _reporte.PilasSinCoord
            Dim txt = $"⚠ Faltan coordenadas de {n} pila{If(n = 1, "", "s")}. " &
                      "El modelo guardado no las incluye — recargá el Excel ETABS para resolverlas."
            _canvas.MostrarAviso(txt, "Recargar modelo ETABS", AddressOf RecargarModeloETABS)
        Else
            _canvas.OcultarAviso()
        End If
    End Sub

    ' =====================================================================
    ' Recarga no destructiva: lee un Excel ETABS, detecta candidatos y completa
    ' CoordsPorLabel con los pares SourceLabel → (X,Y) que falten. No toca
    ' reactions, refuerzos ni elementos; solo rellena coordenadas faltantes.
    ' =====================================================================
    Private Sub RecargarModeloETABS()
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Pilas Is Nothing Then Return

        Using ofd As New OpenFileDialog() With {
            .Title = "Seleccionar Excel ETABS del modelo",
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*",
            .Multiselect = False
        }
            If ofd.ShowDialog(Me) <> DialogResult.OK Then Return
            Me.Cursor = Cursors.WaitCursor
            Try
                Dim candidatos = Funciones_00_Varias.DetectarCandidatosETABS(ofd.FileName)
                If candidatos Is Nothing OrElse candidatos.Count = 0 Then
                    MessageBox.Show(Me,
                        "El Excel no contiene las hojas necesarias para detectar apoyos (Joint Reactions, " &
                        "Pier Forces, Pier Section Properties, Objects and Elements - Joints/Frames). " &
                        "Exportalas desde ETABS y volvé a intentar.",
                        "Faltan hojas requeridas", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                ' Garantizar que CoordsPorLabel existe (proyectos viejos pueden tenerlo en Nothing).
                If Proyecto.Elementos.Pilas.CoordsPorLabel Is Nothing Then
                    Proyecto.Elementos.Pilas.CoordsPorLabel =
                        New Dictionary(Of String, PointF)(StringComparer.OrdinalIgnoreCase)
                End If
                Dim mapa = Proyecto.Elementos.Pilas.CoordsPorLabel

                Dim labelsFaltantes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each p In Proyecto.Elementos.Pilas.ListaElementos
                    If p IsNot Nothing AndAlso Not p.TieneCoordenadas AndAlso
                       Not String.IsNullOrWhiteSpace(p.Name_Label) Then
                        labelsFaltantes.Add(p.Name_Label.Trim())
                    End If
                Next

                Dim agregados As Integer = 0
                For Each c In candidatos
                    Dim lbl = If(c.SourceLabel, "").Trim()
                    If lbl.Length = 0 Then Continue For
                    If Not labelsFaltantes.Contains(lbl) Then Continue For
                    mapa(lbl) = New PointF(CSng(c.X), CSng(c.Y))
                    agregados += 1
                Next

                If agregados = 0 Then
                    MessageBox.Show(Me,
                        $"Se leyeron {candidatos.Count} apoyos del Excel, pero ninguno coincide con los " &
                        $"nombres de las {labelsFaltantes.Count} pila(s) sin coordenadas de este proyecto. " &
                        "Verificá que el Excel sea del mismo modelo.",
                        "Sin coincidencias", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                ' Re-resolver y repintar.
                GeometriaPlantasService.ResolverPilas(Proyecto.Elementos.Pilas,
                                                     If(Proyecto.Elementos.Joints, New List(Of cJoint)))
                ResolverGeometria()
                _canvas.AjustarVista()
                _canvas.Refrescar()

                MessageBox.Show(Me,
                    $"Se completaron las coordenadas de {agregados} pila(s).",
                    "Recarga completada", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Catch ex As Exception
                Logger.Error(ex, "Form_Planta_Pilas.RecargarModeloETABS", ofd.FileName)
                MessageBox.Show(Me,
                    "Error al recargar el modelo:" & vbCrLf & ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Using
    End Sub

    Private Function PilasDibujables() As List(Of Elemento_Pila)
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Pilas Is Nothing OrElse
           Proyecto.Elementos.Pilas.ListaElementos Is Nothing Then
            Return New List(Of Elemento_Pila)
        End If
        Return Proyecto.Elementos.Pilas.ListaElementos.
               Where(Function(p) p IsNot Nothing AndAlso p.TieneCoordenadas).ToList()
    End Function

    ' =====================================================================
    ' Callbacks al canvas
    ' =====================================================================
    Private ReadOnly Property Modo As ModoColor
        Get
            Return CType(_canvas.ModoActualIndex, ModoColor)
        End Get
    End Property

    Private Function ResolverBounds() As RectangleF
        Dim lista = PilasDibujables()
        If lista.Count = 0 Then Return RectangleF.Empty

        ' Dc viene de PilaService como diámetro de la sección en metros.
        Dim minX = lista.Min(Function(p) p.CoordX - p.Dc / 2)
        Dim maxX = lista.Max(Function(p) p.CoordX + p.Dc / 2)
        Dim minY = lista.Min(Function(p) p.CoordY - p.Dc / 2)
        Dim maxY = lista.Max(Function(p) p.CoordY + p.Dc / 2)

        Return New RectangleF(minX, minY,
                              Math.Max(maxX - minX, 0.1F),
                              Math.Max(maxY - minY, 0.1F))
    End Function

    Private Function HitPila(wx As Double, wy As Double) As Object
        ' Hit-test circular: distancia al centro ≤ radio.
        For Each p In PilasDibujables()
            Dim r As Double = Math.Max(p.Dc / 2.0, 0.05)   ' mínimo 5 cm para labels sin diámetro.
            Dim dx = wx - p.CoordX
            Dim dy = wy - p.CoordY
            If dx * dx + dy * dy <= r * r Then Return p
        Next
        Return Nothing
    End Function

    ''' <summary>C/D del chequeo activo para una pila. Despacha según Modo.</summary>
    Private Function CDModo(p As Elemento_Pila) As Double
        If p Is Nothing Then Return 0
        Select Case Modo
            Case ModoColor.EsfuerzosConcreto : Return PilaService.MinChequeoEsfuerzos(p)
            Case ModoColor.EsfuerzosSuelo    : Return PilaService.PeorCDSuelo(p)
            Case ModoColor.Cortante          : Return PilaService.PeorCDCortante(p)
            Case ModoColor.FlexoCompresion   : Return PilaService.PeorCDFlexo(p)
            Case Else                        : Return PilaService.PeorCDGlobal(p)
        End Select
    End Function

    Private Function BuildFicha(el As Object) As IList(Of String)
        Dim p = TryCast(el, Elemento_Pila)
        If p Is Nothing Then Return New List(Of String)()

        Dim lineas As New List(Of String) From {
            $"{p.Name_Label}   {p.Name_Elemento}",
            $"Dc = {p.Dc:F2} m   Df = {p.Df:F2} m   L = {p.L_Pila:F1} m",
            $"Refuerzo long: {p.Cant_Barras_Long} {p.N_Barra_Long}   Trans: {p.N_Barra_Trans} @ {p.Separacion_Trans:F2} m",
            ""
        }

        ' Breakdown de los 4 chequeos. Se marca con › el chequeo activo.
        Dim mEsf = PilaService.MinChequeoEsfuerzos(p)
        Dim mSue = PilaService.PeorCDSuelo(p)
        Dim mCor = PilaService.PeorCDCortante(p)
        Dim mFlx = PilaService.PeorCDFlexo(p)

        lineas.Add($"{Marcador(ModoColor.EsfuerzosConcreto)}Esf. concreto      {FormatearCD(mEsf)}")
        lineas.Add($"{Marcador(ModoColor.EsfuerzosSuelo)}Esf. al suelo      {FormatearCD(mSue)}")
        lineas.Add($"{Marcador(ModoColor.Cortante)}Cortante           {FormatearCD(mCor)}")
        lineas.Add($"{Marcador(ModoColor.FlexoCompresion)}Flexo-compresión   {FormatearCD(mFlx)}")

        Dim peor = PilaService.PeorCDGlobal(p)
        If peor > 0 Then
            Dim veredicto = If(peor >= Funciones_00_Varias.UMBRAL_CD, "cumple", "no cumple")
            lineas.Add("")
            lineas.Add($"Peor global  {peor:F2}   ({veredicto})")
        Else
            lineas.Add("")
            lineas.Add("Sin calcular")
        End If

        Return lineas
    End Function

    Private Function Marcador(destino As ModoColor) As String
        Return If(Modo = destino, "› ", "  ")
    End Function

    Private Shared Function FormatearCD(v As Double) As String
        If v <= 0 Then Return "—"
        If Double.IsInfinity(v) Then Return "—"
        Return v.ToString("F2")
    End Function

    Private Shared Function FormatearCD(v As Single) As String
        Return FormatearCD(CDbl(v))
    End Function

    Private Function BuildLeyenda() As IList(Of PlantaLeyendaItem)
        Dim items As New List(Of PlantaLeyendaItem)
        Dim lista = PilasDibujables()

        Dim nCumple = 0, nAmbar = 0, nNo = 0, nSin = 0
        For Each p In lista
            Dim cd = CDModo(p)
            If cd <= 0 Then
                nSin += 1
            ElseIf cd < Funciones_00_Varias.UMBRAL_CD Then
                nNo += 1
            ElseIf cd < 1.15 Then
                nAmbar += 1
            Else
                nCumple += 1
            End If
        Next

        items.Add(New PlantaLeyendaItem(ColorCumple, "C/D >= 1.15", nCumple))
        items.Add(New PlantaLeyendaItem(ColorAmbar, $"C/D {Funciones_00_Varias.UMBRAL_CD:F2} a 1.15", nAmbar))
        items.Add(New PlantaLeyendaItem(ColorNoCumple, $"C/D < {Funciones_00_Varias.UMBRAL_CD:F2}", nNo))
        items.Add(New PlantaLeyendaItem(ColorSinCalcular, "Sin calcular", nSin))
        Return items
    End Function

    Private Sub OnCanvasSeleccion(el As Object)
        Dim p = TryCast(el, Elemento_Pila)
        If p IsNot Nothing Then RaiseEvent PilaSeleccionada(p)
    End Sub

    ' =====================================================================
    ' Dibujo específico de pilas (círculos con diámetro real + label)
    ' =====================================================================
    Private Sub DibujarPilas(g As Graphics, t As PlantaTransform)
        Dim lista = PilasDibujables()
        If lista.Count = 0 Then Return

        Dim sel = TryCast(_canvas.SelectedElement, Elemento_Pila)
        Dim hov = TryCast(_canvas.HoverElement, Elemento_Pila)

        Using fEtiqueta As New Font("Segoe UI", 7.5F, FontStyle.Bold),
              penBorde As New Pen(Color.FromArgb(70, 70, 70), 1.2F),
              penSelec As New Pen(Color.FromArgb(20, 90, 200), 2.5F),
              penHover As New Pen(Color.FromArgb(255, 140, 0), 2.0F)

            For Each p In lista
                Dim rect = t.W2SRect(p.CoordX, p.CoordY, p.Dc, p.Dc)
                If rect.Width < 1.0F OrElse rect.Height < 1.0F Then
                    ' Si zoom es muy chico igual mostramos un punto para no perder la pila.
                    rect = New RectangleF(rect.X - 2, rect.Y - 2, 4, 4)
                End If

                ' Sombra sutil
                Using bSombra As New SolidBrush(Color.FromArgb(50, 0, 0, 0))
                    g.FillEllipse(bSombra, rect.X + 1.5F, rect.Y + 1.5F, rect.Width, rect.Height)
                End Using

                Using b As New SolidBrush(ColorDe(p))
                    g.FillEllipse(b, rect)
                End Using
                g.DrawEllipse(penBorde, rect)

                If ReferenceEquals(p, sel) Then
                    g.DrawEllipse(penSelec, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4)
                ElseIf ReferenceEquals(p, hov) Then
                    g.DrawEllipse(penHover, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                End If

                ' Label dentro del círculo si cabe, abajo si no.
                If Not String.IsNullOrEmpty(p.Name_Label) Then
                    Dim sz = g.MeasureString(p.Name_Label, fEtiqueta)
                    Dim textoCabe As Boolean = sz.Width < rect.Width - 4
                    Dim textoVisible As Boolean = sz.Width < rect.Width + 20 AndAlso t.Zoom > 15

                    If textoCabe Then
                        g.DrawString(p.Name_Label, fEtiqueta, Brushes.White,
                                     rect.X + rect.Width / 2 - sz.Width / 2,
                                     rect.Y + rect.Height / 2 - sz.Height / 2)
                    ElseIf textoVisible Then
                        g.DrawString(p.Name_Label, fEtiqueta, Brushes.Black,
                                     rect.X + rect.Width / 2 - sz.Width / 2,
                                     rect.Y + rect.Height + 2)
                    End If
                End If
            Next
        End Using
    End Sub

    Private Function ColorDe(p As Elemento_Pila) As Color
        Dim cd = CDModo(p)
        If cd <= 0 Then Return ColorSinCalcular
        If cd < Funciones_00_Varias.UMBRAL_CD Then Return ColorNoCumple
        If cd < 1.15 Then Return ColorAmbar
        Return ColorCumple
    End Function

    ''' <summary>
    ''' Render headless de la planta al tamaño pedido. Para el Informe Técnico:
    ''' el caller crea el form con Proyecto+GridLines seteados, llama a este método
    ''' y descarta el form. El form NO necesita mostrarse — resuelve la geometría
    ''' y usa PlantaCanvas.RenderBitmap para pintar sin UI.
    ''' modoIndex: ver ModoColor (0 = Peor global).
    ''' </summary>
    Public Function RenderBitmapPlanta(ancho As Integer, alto As Integer,
                                       Optional modoIndex As Integer = 0) As Bitmap
        ResolverGeometria()
        _canvas.GridLines = GridLines
        Return _canvas.RenderBitmap(ancho, alto, modoIndex)
    End Function

End Class
