Imports System.Diagnostics
Imports System.Drawing.Drawing2D
Imports System.IO

''' <summary>
''' Módulo 15 — Relación Viga-Columna (NSR-10 C.21.6.2).
'''
''' Planta estructural + panel detalle. La planta dibuja TODAS las columnas
''' del piso seleccionado (Frame y Pier) con su sección real B_Plano × H_Plano
''' rotada por AnguloOrientacion, sobre el grid de ejes del proyecto. Cada
''' columna se colorea por el verdict del nudo beam-column asociado:
'''
'''    verde    — cumple DES (ratio ≥ 1,20 en LL y LC)
'''    amarillo — cumple solo DMO (ratio ≥ 1,0 pero < 1,2)
'''    rojo     — no cumple ni DMO
'''    gris     — sin dato o sin nudo beam-column en este piso
'''
''' Al hacer clic sobre una columna, el panel derecho muestra todas las
''' variables intermedias (Pu usado, Mn3/Mn2 interpolados del DI, α,
''' proyecciones LL/LC, refuerzo provisto de cada viga, ratios). Si la
''' columna no tiene nudo beam-column en este piso, el panel explica por qué.
''' </summary>
Public Class Form_15_RelacionVigaColumna
    Inherits Form

    Private ReadOnly _proyecto As Proyecto
    Private _nodos As List(Of NodoRVC)
    Private _colsPiso As List(Of ColumnaEnPiso)   ' cache del piso actual

    Private _canvas As PlantaCanvas
    Private _cmbPiso As ComboBox
    Private _cmbFiltro As ComboBox
    Private _txtDetalle As TextBox
    Private _lblResumen As Label

    ' Paleta
    Private Shared ReadOnly ColorCumpleDES As Color = Color.FromArgb(220, 27, 175, 122)
    Private Shared ReadOnly ColorSoloDMO As Color = Color.FromArgb(230, 237, 161, 0)
    Private Shared ReadOnly ColorNoCumple As Color = Color.FromArgb(230, 227, 73, 72)
    Private Shared ReadOnly ColorSinDato As Color = Color.FromArgb(140, 170, 170, 170)
    Private Shared ReadOnly ColorSel As Color = Color.FromArgb(230, 25, 110, 200)
    Private Shared ReadOnly GrisARCO As Color = Color.FromArgb(87, 87, 87)

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Dim validar = RelacionVigaColumnaService.PuedeEvaluar(proyecto)
        If Not validar.OK Then
            MessageBox.Show(validar.Mensaje,
                            "Relación Viga-Columna — faltan datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using frm As New Form_15_RelacionVigaColumna(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
        AddHandler Me.Shown, AddressOf OnShown
    End Sub

    ' =========================================================================
    '  Modelo de UI: una columna dibujable en el piso actual
    ' =========================================================================
    Private Class ColumnaEnPiso
        Public Col As Columna
        Public Tramo As Tramo_Columna
        Public X As Double
        Public Y As Double
        Public B As Double     ' B_Plano del tramo (o fallback a Dimensiones.B)
        Public H As Double     ' H_Plano del tramo
        Public AnguloDeg As Double
        Public EsCircular As Boolean
        Public Diametro As Double
        Public Nodo As NodoRVC  ' puede ser Nothing si no hay beam-column aquí
    End Class

    ' =========================================================================
    Private Sub BuildUI()
        Me.Text = "Relación Viga-Columna — NSR-10 C.21.6.2"
        Me.Size = New Size(1480, 850)
        Me.MinimumSize = New Size(1000, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9)

        ' ── Encabezado ─────────────────────────────────────────────
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 48, .BackColor = GrisARCO}
        Dim lblTit As New Label With {
            .Text = "  Relación Viga-Columna",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 12, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblTit)

        ' ── Toolbar de filtros ──────────────────────────────────────
        Dim panelToolbar As New Panel With {.Dock = DockStyle.Top, .Height = 44,
                                             .BackColor = Color.FromArgb(240, 240, 244)}

        Dim lblPiso As New Label With {
            .Text = "Piso:", .Location = New Point(10, 14), .AutoSize = True,
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold), .ForeColor = GrisARCO
        }
        _cmbPiso = New ComboBox With {
            .Location = New Point(50, 10), .Width = 160,
            .DropDownStyle = ComboBoxStyle.DropDownList, .FlatStyle = FlatStyle.Flat
        }
        AddHandler _cmbPiso.SelectedIndexChanged, Sub(s, e) RefrescarPiso()

        Dim lblFiltro As New Label With {
            .Text = "Filtrar:", .Location = New Point(230, 14), .AutoSize = True,
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold), .ForeColor = GrisARCO
        }
        _cmbFiltro = New ComboBox With {
            .Location = New Point(282, 10), .Width = 200,
            .DropDownStyle = ComboBoxStyle.DropDownList, .FlatStyle = FlatStyle.Flat
        }
        _cmbFiltro.Items.AddRange({"Todos", "Cumple DES", "Solo DMO", "No cumple", "Sin dato"})
        _cmbFiltro.SelectedIndex = 0
        AddHandler _cmbFiltro.SelectedIndexChanged, Sub(s, e) _canvas?.Refrescar()

        _lblResumen = New Label With {
            .Location = New Point(500, 14), .AutoSize = True, .ForeColor = GrisARCO,
            .Font = New Font("Segoe UI", 9, FontStyle.Italic)
        }

        Dim btnExportar As New Button With {
            .Text = "  Exportar a Excel…", .Size = New Size(170, 28),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(21, 130, 70), .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold), .Cursor = Cursors.Hand
        }
        btnExportar.FlatAppearance.BorderSize = 0
        AddHandler btnExportar.Click, AddressOf BtnExportar_Click
        AddHandler panelToolbar.Resize, Sub(s, e) btnExportar.Location = New Point(panelToolbar.Width - 182, 8)

        panelToolbar.Controls.AddRange({lblPiso, _cmbPiso, lblFiltro, _cmbFiltro, _lblResumen, btnExportar})

        ' ── Split: planta (izq) + detalle (der) ─────────────────────
        Dim split As New SplitContainer With {
            .Dock = DockStyle.Fill, .Orientation = Orientation.Vertical,
            .BackColor = Color.White
        }
        AddHandler split.Layout, Sub(s, e)
                                     If split.Width > 300 Then split.SplitterDistance = CInt(split.Width * 0.58)
                                 End Sub

        _canvas = New PlantaCanvas() With {
            .Dock = DockStyle.Fill,
            .TituloVacio = "Sin columnas en este piso",
            .TextoAyuda = "Pasá el cursor sobre una columna para ver su resumen. Click para ver el detalle completo en el panel derecho."
        }
        _canvas.OnResolverBounds = AddressOf ResolverBounds
        _canvas.OnDrawElementos = AddressOf DibujarPlanta
        _canvas.OnHitTest = AddressOf HitTest
        _canvas.OnBuildFicha = AddressOf BuildFicha
        _canvas.OnBuildLeyenda = AddressOf BuildLeyenda
        AddHandler _canvas.ElementoSeleccionado, Sub(el) MostrarDetalle(TryCast(el, ColumnaEnPiso))
        split.Panel1.Controls.Add(_canvas)

        _txtDetalle = New TextBox With {
            .Dock = DockStyle.Fill, .Multiline = True, .ReadOnly = True,
            .ScrollBars = ScrollBars.Both, .WordWrap = False,
            .Font = New Font("Consolas", 9),
            .BackColor = Color.White, .BorderStyle = BorderStyle.None,
            .Text = "(Hacé clic en una columna para ver el detalle)"
        }
        Dim panelDet As New Panel With {
            .Dock = DockStyle.Fill, .Padding = New Padding(10),
            .BackColor = Color.FromArgb(252, 252, 252)
        }
        panelDet.Controls.Add(_txtDetalle)
        split.Panel2.Controls.Add(panelDet)

        Me.Controls.Add(split)
        Me.Controls.Add(panelToolbar)
        Me.Controls.Add(panelTop)
    End Sub

    ' =========================================================================
    Private Sub OnShown(sender As Object, e As EventArgs)
        Me.Cursor = Cursors.WaitCursor
        Try
            _nodos = RelacionVigaColumnaService.Evaluar(_proyecto)
        Catch ex As Exception
            Logger.Error(ex, "Form_15_RelacionVigaColumna.OnShown")
            MessageBox.Show("Error al evaluar: " & ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
            Me.Close()
            Return
        Finally
            Me.Cursor = Cursors.Default
        End Try

        ' Pasar grids al canvas para dibujar ejes estructurales.
        If _proyecto?.Elementos?.Grids?.GridLines IsNot Nothing Then
            _canvas.GridLines = _proyecto.Elementos.Grids.GridLines
        ElseIf _proyecto?.Grids?.GridLines IsNot Nothing Then
            _canvas.GridLines = _proyecto.Grids.GridLines
        End If

        PoblarCmbPiso()
        ActualizarResumen()
    End Sub

    Private Sub PoblarCmbPiso()
        _cmbPiso.Items.Clear()
        If _proyecto?.Elementos?.Columnas?.Lista_Columnas Is Nothing Then Return

        ' Pisos únicos a partir de los tramos de columnas (no solo nodos), para
        ' que el usuario pueda inspeccionar pisos donde ninguna columna tiene
        ' nudo beam-column también.
        Dim pisos As New List(Of String)
        For Each col In _proyecto.Elementos.Columnas.Lista_Columnas
            If col.Lista_Tramos_Columnas Is Nothing Then Continue For
            For Each tr In col.Lista_Tramos_Columnas
                If Not String.IsNullOrWhiteSpace(tr?.Piso) AndAlso Not pisos.Contains(tr.Piso) Then
                    pisos.Add(tr.Piso)
                End If
            Next
        Next

        ' Ordenar por Z (de nodos o joints)
        Dim zPorPiso As New Dictionary(Of String, Double)
        For Each p In pisos
            Dim n = _nodos.FirstOrDefault(Function(x) x.Piso = p)
            If n IsNot Nothing Then
                zPorPiso(p) = n.Z
            Else
                zPorPiso(p) = Double.MaxValue  ' al final si no hay nodos del piso
            End If
        Next
        pisos.Sort(Function(a, b) zPorPiso(a).CompareTo(zPorPiso(b)))

        For Each p In pisos
            _cmbPiso.Items.Add(p)
        Next
        If _cmbPiso.Items.Count > 0 Then
            ' Default: el primer piso que tenga nodos beam-column
            Dim idxDefault = 0
            For i = 0 To _cmbPiso.Items.Count - 1
                If _nodos.Any(Function(n) String.Equals(n.Piso, _cmbPiso.Items(i).ToString(), StringComparison.OrdinalIgnoreCase)) Then
                    idxDefault = i : Exit For
                End If
            Next
            _cmbPiso.SelectedIndex = idxDefault
        End If
    End Sub

    Private Sub ActualizarResumen()
        If _nodos Is Nothing Then Return
        Dim des = _nodos.Where(Function(n) n.CumpleDES).Count()
        Dim dmo = _nodos.Where(Function(n) n.CumpleSoloDMO).Count()
        Dim no = _nodos.Where(Function(n) (n.DireccionLL.Ratio > 0 AndAlso Not n.DireccionLL.CumpleDMO) OrElse
                                           (n.DireccionLC.Ratio > 0 AndAlso Not n.DireccionLC.CumpleDMO)).Count()
        _lblResumen.Text = $"{_nodos.Count} nudos  |  DES ok: {des}  |  Solo DMO: {dmo}  |  No cumple: {no}"
    End Sub

    Private Sub RefrescarPiso()
        If _cmbPiso Is Nothing OrElse _cmbPiso.SelectedIndex < 0 Then Return
        Dim piso = _cmbPiso.SelectedItem.ToString()
        _colsPiso = ConstruirColumnasDelPiso(piso)
        _canvas.SelectedElement = Nothing
        _txtDetalle.Text = "(Hacé clic en una columna para ver el detalle)"
        _canvas.Refrescar()
        _canvas.AjustarVista()
    End Sub

    Private Function ConstruirColumnasDelPiso(piso As String) As List(Of ColumnaEnPiso)
        Dim result As New List(Of ColumnaEnPiso)
        If _proyecto?.Elementos?.Columnas?.Lista_Columnas Is Nothing Then Return result

        For Each col In _proyecto.Elementos.Columnas.Lista_Columnas
            If col.Coordenadas Is Nothing Then Continue For
            If col.Coordenadas.X = 0 AndAlso col.Coordenadas.Y = 0 Then Continue For
            If col.Lista_Tramos_Columnas Is Nothing Then Continue For

            Dim tr = col.Lista_Tramos_Columnas.FirstOrDefault(Function(t) String.Equals(t.Piso, piso, StringComparison.OrdinalIgnoreCase))
            If tr Is Nothing Then Continue For

            Dim entry As New ColumnaEnPiso With {
                .Col = col, .Tramo = tr,
                .X = col.Coordenadas.X, .Y = col.Coordenadas.Y,
                .B = tr.B_Plano, .H = tr.H_Plano,
                .AnguloDeg = tr.AnguloOrientacion,
                .EsCircular = tr.EsCircular, .Diametro = tr.Diametro
            }
            ' Fallback a Columna.Dimensiones si el tramo no trae B/H
            If entry.B <= 0 AndAlso entry.H <= 0 AndAlso col.Dimensiones IsNot Nothing Then
                entry.B = col.Dimensiones.B
                entry.H = col.Dimensiones.H
                If entry.Diametro <= 0 Then entry.Diametro = col.Dimensiones.D
            End If
            ' Columna circular sin dimensiones rectangulares — usar diámetro
            If entry.EsCircular AndAlso entry.Diametro > 0 AndAlso entry.B <= 0 Then
                entry.B = entry.Diametro
                entry.H = entry.Diametro
            End If
            ' Sin ninguna dimensión → usar 0.3×0.3 como placeholder para que se vea
            If entry.B <= 0 Then entry.B = 0.3
            If entry.H <= 0 Then entry.H = 0.3

            ' Buscar nodo beam-column asociado: el que tiene ColumnaAbajo = este tramo
            ' (es decir, el nudo del TECHO del tramo, donde aterrizan las vigas del piso).
            entry.Nodo = _nodos.FirstOrDefault(Function(n) ReferenceEquals(n.ColumnaAbajo, tr))

            result.Add(entry)
        Next
        Return result
    End Function

    ' =========================================================================
    '  Callbacks del PlantaCanvas
    ' =========================================================================
    Private Function ColumnasFiltradas() As IEnumerable(Of ColumnaEnPiso)
        If _colsPiso Is Nothing Then Return Enumerable.Empty(Of ColumnaEnPiso)()
        Select Case _cmbFiltro.SelectedIndex
            Case 1 : Return _colsPiso.Where(Function(c) c.Nodo IsNot Nothing AndAlso c.Nodo.CumpleDES)
            Case 2 : Return _colsPiso.Where(Function(c) c.Nodo IsNot Nothing AndAlso c.Nodo.CumpleSoloDMO)
            Case 3 : Return _colsPiso.Where(Function(c) c.Nodo IsNot Nothing AndAlso
                                                       ((c.Nodo.DireccionLL.Ratio > 0 AndAlso Not c.Nodo.DireccionLL.CumpleDMO) OrElse
                                                        (c.Nodo.DireccionLC.Ratio > 0 AndAlso Not c.Nodo.DireccionLC.CumpleDMO)))
            Case 4 : Return _colsPiso.Where(Function(c) c.Nodo Is Nothing OrElse (c.Nodo.DireccionLL.Ratio <= 0 AndAlso c.Nodo.DireccionLC.Ratio <= 0))
            Case Else : Return _colsPiso
        End Select
    End Function

    Private Function ResolverBounds() As RectangleF
        Dim vis = ColumnasFiltradas().ToList()
        If vis.Count = 0 Then Return RectangleF.Empty
        Dim margen As Double = 1.5
        Dim xMin = vis.Min(Function(c) c.X - Math.Max(c.B, c.H))
        Dim xMax = vis.Max(Function(c) c.X + Math.Max(c.B, c.H))
        Dim yMin = vis.Min(Function(c) c.Y - Math.Max(c.B, c.H))
        Dim yMax = vis.Max(Function(c) c.Y + Math.Max(c.B, c.H))
        Return RectangleF.FromLTRB(CSng(xMin - margen), CSng(yMin - margen),
                                    CSng(xMax + margen), CSng(yMax + margen))
    End Function

    Private Sub DibujarPlanta(g As Graphics, t As PlantaTransform)
        g.SmoothingMode = SmoothingMode.AntiAlias

        For Each c In ColumnasFiltradas()
            Dim col = ColorPorVerdict(c)
            DibujarColumna(g, t, c, col, ReferenceEquals(_canvas.SelectedElement, c))
        Next
    End Sub

    Private Sub DibujarColumna(g As Graphics, t As PlantaTransform,
                                c As ColumnaEnPiso, fondo As Color, seleccionada As Boolean)
        Dim nombre = If(Not String.IsNullOrWhiteSpace(c.Col.Name_Label), c.Col.Name_Label, c.Col.Name_Elemento)

        If c.EsCircular Then
            Dim d = c.Diametro
            If d <= 0 Then d = Math.Max(c.B, c.H)
            Dim r = t.W2SRect(c.X, c.Y, d, d)
            Using b As New SolidBrush(fondo)
                g.FillEllipse(b, r)
            End Using
            Using pen As New Pen(Color.FromArgb(230, 40, 40, 40), 1.4F)
                g.DrawEllipse(pen, r)
            End Using
            If seleccionada Then
                Dim rSel = t.W2SRect(c.X, c.Y, d * 1.25, d * 1.25)
                Using penSel As New Pen(ColorSel, 2.5F)
                    g.DrawEllipse(penSel, rSel)
                End Using
            End If
            DibujarEtiqueta(g, r, c, nombre)
            Return
        End If

        ' Convención ARCO (ver Form_Planta_Columnas:361):
        '   H_Plano = dim larga → sobre eje local X (= "ancho" del rect)
        '   B_Plano = dim corta → sobre eje local Y (= "alto" del rect)
        ' Con α = 0 el rectángulo queda "acostado" con la dim larga en X global.
        ' Patrón de dibujo: TranslateTransform + RotateTransform (más limpio que
        ' calcular 4 puntos a mano). La Y de pantalla crece hacia abajo, así que
        ' el ángulo se niega para que un giro antihorario en mundo se vea
        ' antihorario en pantalla.
        Dim pc = t.W2S(c.X, c.Y)
        Dim anchoM = c.H           ' H_Plano sobre eje local X
        Dim altoM = c.B            ' B_Plano sobre eje local Y
        Dim wPx = CSng(anchoM * t.Zoom)
        Dim hPx = CSng(altoM * t.Zoom)
        If wPx < 1.5F AndAlso hPx < 1.5F Then wPx = 4 : hPx = 4
        Dim rect As New RectangleF(-wPx / 2, -hPx / 2, wPx, hPx)
        Dim angScreen = CSng(-c.AnguloDeg)

        Dim state = g.Save()
        Try
            g.TranslateTransform(pc.X, pc.Y)
            If Math.Abs(angScreen) > 0.01F Then
                g.RotateTransform(angScreen)
            End If
            Using b As New SolidBrush(fondo)
                g.FillRectangle(b, rect)
            End Using
            Using pen As New Pen(Color.FromArgb(230, 40, 40, 40), 1.4F)
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height)
            End Using
            If seleccionada Then
                Using penSel As New Pen(ColorSel, 2.5F)
                    g.DrawRectangle(penSel, rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2)
                End Using
            End If
        Finally
            g.Restore(state)
        End Try

        ' Etiqueta: la pintamos SIN rotar (más legible) centrada en el pixel del
        ' centroide. Usamos un bbox cuadrado de tamaño min(w,h) para ubicar texto.
        Dim lado = Math.Min(wPx, hPx)
        Dim rectTxt As New RectangleF(pc.X - lado / 2, pc.Y - lado / 2, lado, lado)
        DibujarEtiqueta(g, rectTxt, c, nombre)
    End Sub

    Private Sub DibujarEtiqueta(g As Graphics, rect As RectangleF, c As ColumnaEnPiso, nombre As String)
        Dim ratioTxt As String = ""
        If c.Nodo IsNot Nothing Then
            Dim peor = c.Nodo.PeorRatio
            If peor > 0 Then ratioTxt = peor.ToString("0.00")
        End If

        Using fontN As New Font("Segoe UI Semibold", 8, FontStyle.Bold)
            Dim szN = g.MeasureString(nombre, fontN)
            Using bTxt As New SolidBrush(Color.Black)
                g.DrawString(nombre, fontN, bTxt,
                             New PointF(rect.X + rect.Width / 2 - szN.Width / 2,
                                        rect.Y + rect.Height / 2 - szN.Height))
            End Using
        End Using
        If ratioTxt <> "" Then
            Using fontR As New Font("Segoe UI", 7, FontStyle.Bold)
                Dim szR = g.MeasureString(ratioTxt, fontR)
                Using bTxt As New SolidBrush(Color.FromArgb(40, 40, 40))
                    g.DrawString(ratioTxt, fontR, bTxt,
                                 New PointF(rect.X + rect.Width / 2 - szR.Width / 2,
                                            rect.Y + rect.Height / 2 + 2))
                End Using
            End Using
        End If
    End Sub

    Private Shared Function ColorPorVerdict(c As ColumnaEnPiso) As Color
        If c.Nodo Is Nothing Then Return ColorSinDato
        Dim n = c.Nodo
        If n.DireccionLL.Ratio <= 0 AndAlso n.DireccionLC.Ratio <= 0 Then Return ColorSinDato
        If n.CumpleDES Then Return ColorCumpleDES
        Dim llDMO = n.DireccionLL.Ratio <= 0 OrElse n.DireccionLL.CumpleDMO
        Dim lcDMO = n.DireccionLC.Ratio <= 0 OrElse n.DireccionLC.CumpleDMO
        If llDMO AndAlso lcDMO Then Return ColorSoloDMO
        Return ColorNoCumple
    End Function

    Private Function HitTest(wx As Double, wy As Double) As Object
        ' Punto-en-footprint rotado para cada columna visible.
        ' Convención: H_Plano (dim larga) sobre eje local X, B_Plano sobre eje local Y.
        For Each c In ColumnasFiltradas()
            If c.EsCircular Then
                Dim d = If(c.Diametro > 0, c.Diametro, Math.Max(c.B, c.H))
                Dim dx = c.X - wx, dy = c.Y - wy
                If dx * dx + dy * dy <= (d / 2.0) * (d / 2.0) Then Return c
            Else
                If PuntoEnRectRotado(wx, wy, c.X, c.Y, c.H, c.B, c.AnguloDeg) Then Return c
            End If
        Next
        Return Nothing
    End Function

    ' largo = dim sobre eje local X, corto = dim sobre eje local Y.
    Private Shared Function PuntoEnRectRotado(wx As Double, wy As Double, cx As Double, cy As Double,
                                                largo As Double, corto As Double, alphaDeg As Double) As Boolean
        Dim dx = wx - cx, dy = wy - cy
        Dim a = -alphaDeg * Math.PI / 180.0
        Dim cosA = Math.Cos(a), sinA = Math.Sin(a)
        Dim xl = dx * cosA - dy * sinA
        Dim yl = dx * sinA + dy * cosA
        Return Math.Abs(xl) <= largo / 2.0 AndAlso Math.Abs(yl) <= corto / 2.0
    End Function

    Private Function BuildFicha(obj As Object) As IList(Of String)
        Dim c = TryCast(obj, ColumnaEnPiso)
        If c Is Nothing Then Return New List(Of String)()
        Dim nombre = If(Not String.IsNullOrWhiteSpace(c.Col.Name_Label), c.Col.Name_Label, c.Col.Name_Elemento)
        Dim lineas As New List(Of String)
        lineas.Add($"Col {nombre}  piso {c.Tramo.Piso}")
        lineas.Add($"B×H = {c.B:0.00}×{c.H:0.00}  α = {c.AnguloDeg:0}°  f'c = {c.Tramo.fc:0}")
        If c.Nodo IsNot Nothing Then
            Dim rLL = If(c.Nodo.DireccionLL.Ratio > 0, c.Nodo.DireccionLL.Ratio.ToString("0.00"), "—")
            Dim rLC = If(c.Nodo.DireccionLC.Ratio > 0, c.Nodo.DireccionLC.Ratio.ToString("0.00"), "—")
            lineas.Add($"Mc/Mv  LL = {rLL}   LC = {rLC}")
            lineas.Add($"Vigas al nudo: {c.Nodo.Vigas.Count}")
        Else
            lineas.Add("Sin nudo beam-column en este piso")
        End If
        Return lineas
    End Function

    Private Function BuildLeyenda() As IList(Of PlantaLeyendaItem)
        Return New List(Of PlantaLeyendaItem) From {
            New PlantaLeyendaItem With {.Color = ColorCumpleDES, .Texto = "Cumple DES (≥ 1,20)"},
            New PlantaLeyendaItem With {.Color = ColorSoloDMO, .Texto = "Solo cumple DMO"},
            New PlantaLeyendaItem With {.Color = ColorNoCumple, .Texto = "No cumple ni DMO"},
            New PlantaLeyendaItem With {.Color = ColorSinDato, .Texto = "Sin nudo / sin dato"}
        }
    End Function

    ' =========================================================================
    '  Panel detalle
    ' =========================================================================
    Private Sub MostrarDetalle(c As ColumnaEnPiso)
        If c Is Nothing Then
            _txtDetalle.Text = "(Hacé clic en una columna para ver el detalle)"
            Return
        End If

        Dim sb As New Text.StringBuilder()
        Dim nombre = If(Not String.IsNullOrWhiteSpace(c.Col.Name_Label), c.Col.Name_Label, c.Col.Name_Elemento)
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════")
        sb.AppendLine($"  COLUMNA  {nombre}   piso = {c.Tramo.Piso}   ({c.X:0.00}, {c.Y:0.00})")
        sb.AppendLine($"  Sección B × H = {c.B:0.00} × {c.H:0.00} m   α = {c.AnguloDeg:0}°   f'c = {c.Tramo.fc:0} MPa")
        sb.AppendLine("═══════════════════════════════════════════════════════════════════════")
        sb.AppendLine()

        If c.Nodo Is Nothing Then
            sb.AppendLine("ESTA COLUMNA NO TIENE NUDO BEAM-COLUMN EN ESTE PISO")
            sb.AppendLine()
            sb.AppendLine("Posibles causas:")
            sb.AppendLine("  • Ninguna viga con refuerzo real termina en esta columna.")
            sb.AppendLine("  • Es un muro aislado (Pier) que solo da rigidez lateral.")
            sb.AppendLine("  • El diagrama de interacción no está calculado todavía.")
            sb.AppendLine()
            sb.AppendLine("Cómo revisarlo:")
            sb.AppendLine("  • Abrí el módulo de Vigas y verificá que las vigas aledañas")
            sb.AppendLine("    tengan refuerzo superior E inferior real en el apoyo.")
            sb.AppendLine("  • Abrí el módulo de Columnas y guardá el refuerzo longitudinal")
            sb.AppendLine("    para que ARCO calcule el DI de este tramo.")
            _txtDetalle.Text = sb.ToString()
            _txtDetalle.SelectionStart = 0
            _canvas.Refrescar()
            Return
        End If

        Dim n = c.Nodo
        sb.AppendLine($"NUDO BEAM-COLUMN  (Z = {n.Z:0.2} m)")
        sb.AppendLine($"  Columna arriba : {If(n.ColumnaArriba Is Nothing, "—", n.ColumnaArriba.Name_Elemento)}")
        sb.AppendLine($"  Columna abajo  : {If(n.ColumnaAbajo Is Nothing, "—", n.ColumnaAbajo.Name_Elemento)}")
        sb.AppendLine($"  Vigas al nudo  : {n.Vigas.Count}")
        sb.AppendLine($"  Eje LL del nudo: dirección α = {n.AnguloRefDeg:0}° global  (LC = α + 90°)")
        sb.AppendLine()

        sb.Append(RenderDireccion(n.DireccionLL, "LL — lado largo (eje fuerte)"))
        sb.AppendLine()
        sb.Append(RenderDireccion(n.DireccionLC, "LC — lado corto (eje débil)"))
        sb.AppendLine()
        sb.AppendLine("CRITERIOS DE CUMPLIMIENTO")
        sb.AppendLine("  DES (NSR-10 C.21.6.2.2):  ΣMnc ≥ 1,20 · ΣMnv   — mandatorio")
        sb.AppendLine("  DMO (informativo):        ΣMnc ≥ 1,00 · ΣMnv")
        sb.AppendLine()
        sb.AppendLine("CONVENCIONES")
        sb.AppendLine("  LL = eje paralelo al lado LARGO de la columna (= eje 2 local).")
        sb.AppendLine("  LC = eje paralelo al lado CORTO (= eje 3 local).")
        sb.AppendLine("  Para sismo en LL la columna flexa alrededor del eje 3 → Mnc_LL = Mn3 (fuerte).")
        sb.AppendLine("  Para sismo en LC → Mnc_LC = Mn2 (eje débil).")
        sb.AppendLine("  Mn columna: interpolado del DI nominal (sin φ) con Pu del combo gobernante.")
        sb.AppendLine("  Mn viga = max(Mn⁻, Mn⁺) — regla ACI/NSR-10.")

        _txtDetalle.Text = sb.ToString()
        _txtDetalle.SelectionStart = 0
        _canvas.Refrescar()
    End Sub

    Private Shared Function RenderDireccion(r As ResultadoRVCDireccion, titulo As String) As String
        Dim sb As New Text.StringBuilder()
        Dim dirN = If(r.Direccion = eDirColumna.LL, "LL", "LC")
        sb.AppendLine($"── DIRECCIÓN {titulo} ──")
        sb.AppendLine()
        sb.AppendLine($"  Σ Mnc  = {r.SumMnc,10:0.0} kN·m")
        sb.AppendLine($"  Σ Mnv  = {r.SumMnv,10:0.0} kN·m")
        Dim ratioTxt As String = If(r.Ratio > 0, r.Ratio.ToString("0.00"), "— (sin dato)")
        sb.AppendLine($"  ratio  = {ratioTxt,10}       ← Mc/Mv")
        If r.Ratio > 0 Then
            sb.AppendLine($"  DES:   {If(r.CumpleDES, "✓ cumple", "✗ no cumple")} (umbral 1,20)")
            sb.AppendLine($"  DMO:   {If(r.CumpleDMO, "✓ cumple", "✗ no cumple")} (umbral 1,00)")
        End If
        sb.AppendLine()

        If r.AportesColumna.Count > 0 Then
            sb.AppendLine("  COLUMNAS QUE APORTAN:")
            sb.AppendLine($"  {"NOMBRE",-10} {"PISO",-10} {"POS",-8} {"B×H",-12} {"α",-5} {"Δα",-5} {"Pu",10} {"Mn3",8} {"Mn2",8} {"Mnc",8}")
            For Each a In r.AportesColumna
                Dim mnc = If(r.Direccion = eDirColumna.LL, a.MncLL, a.MncLC)
                Dim dAlpha = a.DeltaAlphaRad * 180.0 / Math.PI
                sb.AppendLine($"  {a.Nombre,-10} {a.Piso,-10} {a.Posicion,-8} {a.B_Plano:0.00}×{a.H_Plano:0.00}  {a.AnguloDeg,3:0}° {dAlpha,3:0}° {a.Pu,10:0} {a.Mn3,8:0} {a.Mn2,8:0} {mnc,8:0.0}")
            Next
            sb.AppendLine()
        End If

        If r.AportesViga.Count > 0 Then
            sb.AppendLine("  VIGAS QUE APORTAN:")
            sb.AppendLine($"  {"NOMBRE",-12} {"EXT",-4} {"θ",-5} {"b×h",-12} {"As⁺/As⁻ (mm²)",-17} {"Mn⁻",6} {"Mn⁺",6} {"max",6} {"proy",6} {"aporta",10}")
            For Each v In r.AportesViga
                Dim lado = If(v.EsExtremoI, "I", "J")
                Dim p = If(r.Direccion = eDirColumna.LL, v.ProyeccionLL, v.ProyeccionLC)
                Dim contr = If(r.Direccion = eDirColumna.LL, v.ContribucionLL, v.ContribucionLC)
                Dim asInfo = $"{v.As_Inf_mm2:0}/{v.As_Sup_mm2:0}"
                sb.AppendLine($"  {v.Nombre,-12} {lado,-4} {v.AnguloDeg,3:0}° {v.B_m:0.00}×{v.H_m:0.00}  {asInfo,-17} {v.MnNeg,6:0} {v.MnPos,6:0} {v.MnChequeo,6:0} {p,6:0.00} {contr,10:0.0}")
            Next
        End If

        Return sb.ToString()
    End Function

    ' =========================================================================
    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)
        If _nodos Is Nothing OrElse _nodos.Count = 0 Then
            MessageBox.Show("No hay nodos evaluados para exportar.", "Exportar",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim sufijo As String = If(_proyecto?.Info IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(_proyecto.Info.Nombre),
                                   "_" & _proyecto.Info.Nombre, "")
        Dim nombreArchivo = "RelacionVigaColumna" & sufijo & "_" & DateTime.Now.ToString("yyyyMMdd") & ".xlsx"

        Dim dlg As New SaveFileDialog With {
            .Title = "Exportar — Relación Viga-Columna",
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = nombreArchivo
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Me.Cursor = Cursors.WaitCursor
            InformeInicialExcelService.GenerarSoloRVC(_nodos, dlg.FileName, _proyecto)
            Dim abrir = MessageBox.Show("Archivo generado. ¿Desea abrirlo ahora?",
                                         "Exportar", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If
        Catch ex As Exception
            Logger.Error(ex, "Form_15_RelacionVigaColumna.BtnExportar_Click")
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Me.Cursor = Cursors.Default
        End Try
    End Sub

End Class
