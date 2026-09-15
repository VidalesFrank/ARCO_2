Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores

''' <summary>
''' Dashboard de gráficas resumen del módulo de Vigas. Cierra la réplica del
''' dashboard de Columnas a los cuatro módulos (Columnas, Pilas, Muros, Vigas).
'''
''' A diferencia de los otros módulos, un edificio tiene muchas más vigas que
''' columnas o muros, así que el dashboard incluye un filtro por piso: con
''' "Todos" las etiquetas del eje X se vuelven ilegibles en proyectos grandes.
'''
''' Criterios: los mismos de Form_Reporte_Resumen (mínimo por viga entre todos
''' sus frames y zonas). En cortante se aplica la regla de la empresa: si una
''' zona de extremo no cumple a cortante convencional pero sí cumple a cortante
''' plástico, la viga cumple y no se reporta.
''' </summary>
Public Class Form_Graficos_Vigas
    Inherits Form

    Public Property Vigas As List(Of cViga)

    Private ReadOnly _grafico As Chart
    Private WithEvents _cboPiso As ComboBox
    Private WithEvents _btnFlexNeg As Button
    Private WithEvents _btnFlexPos As Button
    Private WithEvents _btnCortante As Button
    Private WithEvents _btnEstado As Button
    Private WithEvents _btnExportar As Button

    ''' Última vista seleccionada, para redibujar al cambiar el filtro de piso.
    Private _vistaActual As String = "FlexNeg"

    ''' Evita redibujar (y avisar dos veces) mientras se llena el combo de pisos.
    Private _cargando As Boolean = False

    Public Sub New()

        Me.Text = "Gráficas Resumen — Vigas"
        Me.Size = New Size(1180, 700)
        Me.MinimumSize = New Size(900, 540)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        _grafico = GraficosResumen.CrearChart()

        ' ── Barra superior: filtro de piso ────────────────────────────────
        Dim barra As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 44,
            .BackColor = Color.FromArgb(245, 245, 245)
        }

        Dim lbl As New Label() With {
            .Text = "Piso:",
            .AutoSize = True,
            .Location = New Point(12, 14),
            .ForeColor = GraficosResumen.ColAcento,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        }

        _cboPiso = New ComboBox() With {
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Location = New Point(56, 10),
            .Width = 220,
            .Font = New Font("Segoe UI", 9.5F)
        }

        barra.Controls.Add(lbl)
        barra.Controls.Add(_cboPiso)

        ' ── Panel lateral de categorías ───────────────────────────────────
        Dim panel = GraficosResumen.CrearPanelLateral()
        _btnFlexNeg = GraficosResumen.AgregarBotonCategoria(panel, "Flexión M(−)", 0)
        _btnFlexPos = GraficosResumen.AgregarBotonCategoria(panel, "Flexión M(+)", 1)
        _btnCortante = GraficosResumen.AgregarBotonCategoria(panel, "Cortante", 2)
        _btnEstado = GraficosResumen.AgregarBotonCategoria(panel, "Estado General", 3)
        _btnExportar = GraficosResumen.AgregarBotonCategoria(panel, "Exportar Imagen", 5, secundario:=True)

        Me.Controls.Add(_grafico)
        Me.Controls.Add(barra)
        Me.Controls.Add(panel)

        AddHandler Me.Load, AddressOf Form_Load

    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)
        CargarPisos()
        Redibujar()
    End Sub

    ' -----------------------------------------------------------------------
    ' Filtro de piso
    ' -----------------------------------------------------------------------
    Private Sub CargarPisos()

        _cargando = True
        _cboPiso.Items.Clear()
        _cboPiso.Items.Add("Todos los pisos")

        If Vigas IsNot Nothing Then
            Dim pisos = Vigas.Select(Function(v) v.Piso) _
                             .Where(Function(p) Not String.IsNullOrWhiteSpace(p)) _
                             .Distinct() _
                             .OrderBy(Function(p) p) _
                             .ToList()
            For Each p In pisos
                _cboPiso.Items.Add(p)
            Next
        End If

        _cboPiso.SelectedIndex = 0
        _cargando = False

    End Sub

    Private Function VigasFiltradas() As List(Of cViga)

        If Vigas Is Nothing Then Return New List(Of cViga)

        ' Solo vigas revisadas (con refuerzo longitudinal colocado), mismo
        ' criterio que Form_Reporte_Resumen.
        Dim lista = Vigas.Where(Function(v) v.Frames IsNot Nothing AndAlso
                                            v.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse
                                                                     f.RefuerzoInferior.Any())).ToList()

        If _cboPiso IsNot Nothing AndAlso _cboPiso.SelectedIndex > 0 Then
            Dim piso As String = _cboPiso.SelectedItem.ToString()
            lista = lista.Where(Function(v) v.Piso = piso).ToList()
        End If

        Return lista

    End Function

    Private Function HayVigas(lista As List(Of cViga)) As Boolean
        If lista Is Nothing OrElse lista.Count = 0 Then
            MessageBox.Show("No hay vigas revisadas para mostrar." & vbCrLf &
                            "Calcule las vigas y coloque el refuerzo antes de ver las gráficas.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Return True
    End Function

    Private Function NombreViga(v As cViga) As String
        Return If(String.IsNullOrWhiteSpace(v.NombrePlano), v.Nombre, v.NombrePlano)
    End Function

    ' -----------------------------------------------------------------------
    ' Regla convencional-vs-plástico.
    '
    ' NOTA: la versión centralizada de este criterio vive en
    ' VigaService.CumpleCortantePlastico (rama fix/vigas-cortante-zona-central).
    ' Cuando esa rama esté en main, reemplazar el cuerpo de FactorEfectivoZona
    ' por una llamada a ese helper en vez de repetir la lógica aquí.
    '
    ' Las tres zonas tienen contraparte plástica: Ve es constante a lo largo del
    ' vano, así que el centro también recibe demanda por capacidad.
    ' -----------------------------------------------------------------------
    Private Function FactorEfectivoZona(zona As cRevisionCortanteZona,
                                        cp As cResultadoCortantePlasticoFrame) As Double

        Dim conv As Double = zona.Factor
        If cp Is Nothing Then Return conv

        Select Case zona.Posicion
            Case PosicionTramoViga.Izquierda
                If cp.ZonaIzq IsNot Nothing AndAlso cp.ZonaIzq.phiVn > 0 Then
                    Return Math.Max(conv, cp.ZonaIzq.Factor)
                End If
            Case PosicionTramoViga.Derecha
                If cp.ZonaDer IsNot Nothing AndAlso cp.ZonaDer.phiVn > 0 Then
                    Return Math.Max(conv, cp.ZonaDer.Factor)
                End If
            Case PosicionTramoViga.Centro
                If cp.ZonaCentro IsNot Nothing AndAlso cp.ZonaCentro.phiVn > 0 Then
                    Return Math.Max(conv, cp.ZonaCentro.Factor)
                End If
        End Select

        Return conv

    End Function

    ' -----------------------------------------------------------------------
    ' Flexión — mínimo RatioSup / RatioInf de todos los frames de la viga
    ' -----------------------------------------------------------------------
    Private Sub MostrarFlexion(negativo As Boolean)

        Dim lista = VigasFiltradas()
        If Not HayVigas(lista) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each v In lista
            Dim peor As Double = Double.MaxValue
            Dim frameCritico As String = "—"

            For Each frame In v.Frames
                If frame.RevisionFlexion Is Nothing Then Continue For
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act Is Nothing Then Continue For

                    Dim asReq As Double = If(negativo, act.AsReqSup, act.AsReqInf)
                    Dim ratio As Double = If(negativo, act.RatioSup, act.RatioInf)

                    If asReq > 0 AndAlso ratio > 0 AndAlso ratio < peor Then
                        peor = ratio
                        frameCritico = frame.ObjectLabel
                    End If
                Next
            Next

            Dim valor As Double = If(peor = Double.MaxValue, -1, peor)
            Dim tip As String = "Piso: " & v.Piso
            If valor >= 0 Then tip &= vbCrLf & "Frame crítico: " & frameCritico

            items.Add(New GraficosResumen.ItemCD(NombreViga(v), valor, tip))
        Next

        Dim titulo As String = If(negativo,
                                  "Flexión M(−) — Refuerzo Superior (C/D) — Vigas",
                                  "Flexión M(+) — Refuerzo Inferior (C/D) — Vigas")

        GraficosResumen.DibujarBarrasCD(_grafico, items, titulo,
                                        "C/D = As colocado / As requerido",
                                        "Viga")

    End Sub

    ' -----------------------------------------------------------------------
    ' Cortante — mínimo C/D efectivo por viga (con rescate plástico aplicado)
    ' -----------------------------------------------------------------------
    Private Sub MostrarCortante()

        Dim lista = VigasFiltradas()
        If Not HayVigas(lista) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each v In lista
            Dim peor As Double = Double.MaxValue
            Dim frameCritico As String = "—"
            Dim zonaCritica As String = "—"
            Dim rescatada As Boolean = False

            For Each frame In v.Frames
                If frame.RevisionCortante Is Nothing Then Continue For
                For Each zona In frame.RevisionCortante
                    If zona.phiVn <= 0 Then Continue For

                    Dim efec As Double = FactorEfectivoZona(zona, frame.CortantePlastico)
                    If efec < peor Then
                        peor = efec
                        frameCritico = frame.ObjectLabel
                        zonaCritica = TextoZona(zona.Posicion)
                        rescatada = (efec > zona.Factor)
                    End If
                Next
            Next

            Dim valor As Double = If(peor = Double.MaxValue, -1, Math.Min(peor, 9.99))
            Dim tip As String = "Piso: " & v.Piso
            If valor >= 0 Then
                tip &= vbCrLf & "Frame crítico: " & frameCritico &
                       vbCrLf & "Zona: " & zonaCritica
                If rescatada Then tip &= vbCrLf & "Gobierna el cortante plástico"
            End If

            items.Add(New GraficosResumen.ItemCD(NombreViga(v), valor, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Cortante (C/D) — Vigas",
                                        "C/D = φVn / Vu  (rescate plástico incluido)",
                                        "Viga")

    End Sub

    Private Function TextoZona(pos As PosicionTramoViga) As String
        Select Case pos
            Case PosicionTramoViga.Izquierda : Return "Izquierda"
            Case PosicionTramoViga.Derecha : Return "Derecha"
            Case Else : Return "Centro"
        End Select
    End Function

    ' -----------------------------------------------------------------------
    ' Estado general — cuántas vigas cumplen y por qué fallan las demás
    ' -----------------------------------------------------------------------
    Private Sub MostrarEstado()

        Dim lista = VigasFiltradas()
        If Not HayVigas(lista) Then Return

        Dim nOK As Integer = 0
        Dim nFlex As Integer = 0
        Dim nCor As Integer = 0
        Dim nAmbas As Integer = 0

        For Each v In lista
            Dim okFlex As Boolean = True
            Dim okCor As Boolean = True

            For Each frame In v.Frames
                If frame.RevisionFlexion IsNot Nothing Then
                    For Each rev In frame.RevisionFlexion
                        Dim act = rev.ResultadoActual
                        If act Is Nothing Then Continue For
                        If act.AsReqSup > 0 AndAlso Not act.CumpleSuperior Then okFlex = False
                        If act.AsReqInf > 0 AndAlso Not act.CumpleInferior Then okFlex = False
                    Next
                End If

                If frame.RevisionCortante IsNot Nothing Then
                    For Each zona In frame.RevisionCortante
                        If zona.phiVn <= 0 Then Continue For
                        If FactorEfectivoZona(zona, frame.CortantePlastico) < GraficosResumen.UMBRAL_CD Then
                            okCor = False
                        End If
                    Next
                End If
            Next

            If okFlex AndAlso okCor Then
                nOK += 1
            ElseIf Not okFlex AndAlso Not okCor Then
                nAmbas += 1
            ElseIf Not okFlex Then
                nFlex += 1
            Else
                nCor += 1
            End If
        Next

        Dim categorias As New List(Of String) From {"Cumple", "Revisar flexión", "Revisar cortante", "Revisar ambas"}
        Dim conteos As New List(Of Integer) From {nOK, nFlex, nCor, nAmbas}
        Dim colores As New List(Of Color) From {GraficosResumen.ColVerde,
                                                GraficosResumen.ColNaranja,
                                                GraficosResumen.ColNaranja,
                                                GraficosResumen.ColRojo}

        GraficosResumen.DibujarBarrasCategorias(_grafico, categorias, conteos, colores,
                                                "Estado General — Vigas",
                                                "Cantidad de vigas",
                                                "Resultado de la revisión")

    End Sub

    ' -----------------------------------------------------------------------
    ' Eventos
    ' -----------------------------------------------------------------------
    Private Sub Redibujar()
        If _cargando Then Return
        Select Case _vistaActual
            Case "FlexPos" : MostrarFlexion(negativo:=False)
            Case "Cortante" : MostrarCortante()
            Case "Estado" : MostrarEstado()
            Case Else : MostrarFlexion(negativo:=True)
        End Select
    End Sub

    Private Sub _cboPiso_SelectedIndexChanged(sender As Object, e As EventArgs) Handles _cboPiso.SelectedIndexChanged
        Redibujar()
    End Sub

    Private Sub _btnFlexNeg_Click(sender As Object, e As EventArgs) Handles _btnFlexNeg.Click
        _vistaActual = "FlexNeg"
        Redibujar()
    End Sub

    Private Sub _btnFlexPos_Click(sender As Object, e As EventArgs) Handles _btnFlexPos.Click
        _vistaActual = "FlexPos"
        Redibujar()
    End Sub

    Private Sub _btnCortante_Click(sender As Object, e As EventArgs) Handles _btnCortante.Click
        _vistaActual = "Cortante"
        Redibujar()
    End Sub

    Private Sub _btnEstado_Click(sender As Object, e As EventArgs) Handles _btnEstado.Click
        _vistaActual = "Estado"
        Redibujar()
    End Sub

    Private Sub _btnExportar_Click(sender As Object, e As EventArgs) Handles _btnExportar.Click
        GraficosResumen.ExportarImagen(_grafico, "Grafico_Vigas")
    End Sub

End Class
