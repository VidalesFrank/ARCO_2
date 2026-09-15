Imports System.Windows.Forms.DataVisualization.Charting

''' <summary>
''' Dashboard de gráficas resumen del módulo 11 (Nervios / Losas Nervadas).
''' Completa la réplica a los cinco módulos: Columnas, Pilas, Muros, Vigas y
''' Nervios.
'''
''' No recalcula nada: lee los C/D que ya dejó el cálculo del módulo en
''' cFrameNervio, con los mismos criterios de "calculado" que usa
''' Form_Reporte_Resumen_Nervios, para que gráfica y reporte no se contradigan.
'''
''' Como en Vigas, incluye filtro por piso: una losa nervada tiene muchos más
''' elementos que columnas o muros y con todos a la vez el eje X es ilegible.
''' </summary>
Public Class Form_Graficos_Nervios
    Inherits Form

    Public Property Nervios As cNervios

    Private ReadOnly _grafico As Chart
    Private WithEvents _cboPiso As ComboBox
    Private WithEvents _btnFlexion As Button
    Private WithEvents _btnAcero As Button
    Private WithEvents _btnCortante As Button
    Private WithEvents _btnEstado As Button
    Private WithEvents _btnExportar As Button

    Private _vistaActual As String = "Flexion"
    Private _cargando As Boolean = False

    Public Sub New()

        Me.Text = "Gráficas Resumen — Nervios"
        Me.Size = New Size(1180, 700)
        Me.MinimumSize = New Size(900, 540)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        _grafico = GraficosResumen.CrearChart()

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

        Dim panel = GraficosResumen.CrearPanelLateral()
        _btnFlexion = GraficosResumen.AgregarBotonCategoria(panel, "Flexión (Mn)", 0)
        _btnAcero = GraficosResumen.AgregarBotonCategoria(panel, "Acero (As)", 1)
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
    ' Origen de datos
    ' -----------------------------------------------------------------------
    Private Sub CargarPisos()

        _cargando = True
        _cboPiso.Items.Clear()
        _cboPiso.Items.Add("Todos los pisos")

        If Nervios IsNot Nothing AndAlso Nervios.Elementos IsNot Nothing Then
            Dim pisos = Nervios.Elementos.Select(Function(n) n.Piso) _
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

    Private Function NerviosFiltrados() As List(Of cNervio)

        If Nervios Is Nothing OrElse Nervios.Elementos Is Nothing Then Return New List(Of cNervio)

        Dim lista = Nervios.Elementos.ToList()

        If _cboPiso IsNot Nothing AndAlso _cboPiso.SelectedIndex > 0 Then
            Dim piso As String = _cboPiso.SelectedItem.ToString()
            lista = lista.Where(Function(n) n.Piso = piso).ToList()
        End If

        Return lista

    End Function

    Private Function HayNervios(lista As List(Of cNervio)) As Boolean
        If lista Is Nothing OrElse lista.Count = 0 Then
            MessageBox.Show("No hay nervios para mostrar." & vbCrLf &
                            "Importe y calcule el módulo de Nervios primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Return True
    End Function

    Private Function NombreNervio(n As cNervio) As String
        Return If(String.IsNullOrWhiteSpace(n.NombrePlano), n.Nombre, n.NombrePlano)
    End Function

    ' Mismos criterios de "calculado" que Form_Reporte_Resumen_Nervios.
    Private Function CalculadoFlex(fn As cFrameNervio) As Boolean
        Return fn.PhiMn_Sup_I > 0 OrElse fn.PhiMn_Inf_C > 0 OrElse fn.PhiMn_Sup_D > 0
    End Function

    Private Function CalculadoCortante(fn As cFrameNervio) As Boolean
        Return fn.PhiVn_I > 0 OrElse fn.PhiVn_D > 0
    End Function

    ' Etiqueta del tramo, igual que Form_Reporte_Resumen_Nervios: ejes de apoyo
    ' si los hay, y si no el label del objeto ETABS.
    Private Function TramoLabel(fn As cFrameNervio) As String
        If Not String.IsNullOrWhiteSpace(fn.EjeApoyo_I) OrElse
           Not String.IsNullOrWhiteSpace(fn.EjeApoyo_D) Then
            Return $"{fn.EjeApoyo_I}-{fn.EjeApoyo_D}"
        End If
        Return fn.ObjectLabel
    End Function

    ''' <summary>
    ''' Peor C/D del nervio entre todos sus tramos y posiciones. Devuelve -1 si
    ''' ninguno está calculado — GraficosResumen omite esos elementos en vez de
    ''' dibujarlos como una barra en cero.
    ''' </summary>
    Private Function PeorCD(n As cNervio,
                            calculado As Func(Of cFrameNervio, Boolean),
                            valores As Func(Of cFrameNervio, Double()),
                            ByRef tramoCritico As String) As Double

        Dim peor As Double = Double.MaxValue
        tramoCritico = "—"

        If n.Frames Is Nothing Then Return -1

        For Each fn In n.Frames
            If Not calculado(fn) Then Continue For
            For Each v In valores(fn)
                If v > 0 AndAlso v < peor Then
                    peor = v
                    tramoCritico = TramoLabel(fn)
                End If
            Next
        Next

        Return If(peor = Double.MaxValue, -1, peor)

    End Function

    ' -----------------------------------------------------------------------
    ' Flexión — C/D por momento: φMn / Mu en apoyo izq, centro y apoyo der
    ' -----------------------------------------------------------------------
    Private Sub MostrarFlexion()

        Dim lista = NerviosFiltrados()
        If Not HayNervios(lista) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each n In lista
            Dim tramo As String = "—"
            Dim v = PeorCD(n, AddressOf CalculadoFlex,
                           Function(fn) New Double() {fn.CD_M_Sup_I, fn.CD_M_Inf_C, fn.CD_M_Sup_D},
                           tramo)

            Dim tip As String = "Piso: " & n.Piso
            If v >= 0 Then tip &= vbCrLf & "Tramo crítico: " & tramo

            items.Add(New GraficosResumen.ItemCD(NombreNervio(n), v, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Flexión (C/D por momento) — Nervios",
                                        "C/D = φMn / Mu",
                                        "Nervio")

    End Sub

    ' -----------------------------------------------------------------------
    ' Acero — C/D por área: As colocado / As requerido
    ' -----------------------------------------------------------------------
    Private Sub MostrarAcero()

        Dim lista = NerviosFiltrados()
        If Not HayNervios(lista) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each n In lista
            Dim tramo As String = "—"
            Dim v = PeorCD(n, AddressOf CalculadoFlex,
                           Function(fn) New Double() {fn.CD_As_Sup_I, fn.CD_As_Inf_C, fn.CD_As_Sup_D},
                           tramo)

            Dim tip As String = "Piso: " & n.Piso
            If v >= 0 Then tip &= vbCrLf & "Tramo crítico: " & tramo

            items.Add(New GraficosResumen.ItemCD(NombreNervio(n), v, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Acero a Flexión (C/D por área) — Nervios",
                                        "C/D = As colocado / As requerido",
                                        "Nervio")

    End Sub

    ' -----------------------------------------------------------------------
    ' Cortante — φVn / Vu en los dos apoyos
    ' -----------------------------------------------------------------------
    Private Sub MostrarCortante()

        Dim lista = NerviosFiltrados()
        If Not HayNervios(lista) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each n In lista
            Dim tramo As String = "—"
            Dim v = PeorCD(n, AddressOf CalculadoCortante,
                           Function(fn) New Double() {fn.CD_Cortante_I, fn.CD_Cortante_D},
                           tramo)

            Dim tip As String = "Piso: " & n.Piso
            If v >= 0 Then tip &= vbCrLf & "Tramo crítico: " & tramo

            items.Add(New GraficosResumen.ItemCD(NombreNervio(n), v, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Cortante (C/D) — Nervios",
                                        "C/D = φVn / Vu",
                                        "Nervio")

    End Sub

    ' -----------------------------------------------------------------------
    ' Estado general — conteo por motivo de revisión
    ' -----------------------------------------------------------------------
    Private Sub MostrarEstado()

        Dim lista = NerviosFiltrados()
        If Not HayNervios(lista) Then Return

        Dim nOK As Integer = 0
        Dim nFlex As Integer = 0
        Dim nCor As Integer = 0
        Dim nAmbas As Integer = 0
        Dim nSin As Integer = 0

        For Each n In lista
            Dim t As String = ""
            Dim vFlex = PeorCD(n, AddressOf CalculadoFlex,
                               Function(fn) New Double() {fn.CD_M_Sup_I, fn.CD_M_Inf_C, fn.CD_M_Sup_D,
                                                          fn.CD_As_Sup_I, fn.CD_As_Inf_C, fn.CD_As_Sup_D}, t)
            Dim vCor = PeorCD(n, AddressOf CalculadoCortante,
                              Function(fn) New Double() {fn.CD_Cortante_I, fn.CD_Cortante_D}, t)

            If vFlex < 0 AndAlso vCor < 0 Then
                nSin += 1
                Continue For
            End If

            Dim okFlex = (vFlex < 0) OrElse (vFlex >= GraficosResumen.UMBRAL_CD)
            Dim okCor = (vCor < 0) OrElse (vCor >= GraficosResumen.UMBRAL_CD)

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

        Dim categorias As New List(Of String) From {"Cumple", "Revisar flexión", "Revisar cortante",
                                                    "Revisar ambas", "Sin calcular"}
        Dim conteos As New List(Of Integer) From {nOK, nFlex, nCor, nAmbas, nSin}
        Dim colores As New List(Of Color) From {GraficosResumen.ColVerde,
                                                GraficosResumen.ColNaranja,
                                                GraficosResumen.ColNaranja,
                                                GraficosResumen.ColRojo,
                                                GraficosResumen.ColGris}

        GraficosResumen.DibujarBarrasCategorias(_grafico, categorias, conteos, colores,
                                                "Estado General — Nervios",
                                                "Cantidad de nervios",
                                                "Resultado de la revisión")

    End Sub

    ' -----------------------------------------------------------------------
    ' Eventos
    ' -----------------------------------------------------------------------
    Private Sub Redibujar()
        If _cargando Then Return
        Select Case _vistaActual
            Case "Acero" : MostrarAcero()
            Case "Cortante" : MostrarCortante()
            Case "Estado" : MostrarEstado()
            Case Else : MostrarFlexion()
        End Select
    End Sub

    Private Sub _cboPiso_SelectedIndexChanged(sender As Object, e As EventArgs) Handles _cboPiso.SelectedIndexChanged
        Redibujar()
    End Sub

    Private Sub _btnFlexion_Click(sender As Object, e As EventArgs) Handles _btnFlexion.Click
        _vistaActual = "Flexion"
        Redibujar()
    End Sub

    Private Sub _btnAcero_Click(sender As Object, e As EventArgs) Handles _btnAcero.Click
        _vistaActual = "Acero"
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
        GraficosResumen.ExportarImagen(_grafico, "Grafico_Nervios")
    End Sub

End Class
