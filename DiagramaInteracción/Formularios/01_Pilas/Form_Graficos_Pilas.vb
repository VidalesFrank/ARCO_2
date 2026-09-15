Imports System.Windows.Forms.DataVisualization.Charting

''' <summary>
''' Dashboard de gráficas resumen del módulo de Pilas. Permite ver de un vistazo
''' el estado de TODAS las pilas del proyecto en las cuatro revisiones del módulo,
''' en vez de revisarlas una a una en las tablas.
'''
''' Reutiliza los valores ya calculados en <see cref="Elemento_Pila"/> (no
''' recalcula nada) y la infraestructura común de <see cref="GraficosResumen"/>,
''' la misma que usa el dashboard de Columnas.
''' </summary>
Public Class Form_Graficos_Pilas
    Inherits Form

    Private ReadOnly _grafico As Chart
    Private WithEvents _btnCargas As Button
    Private WithEvents _btnSuelo As Button
    Private WithEvents _btnCortante As Button
    Private WithEvents _btnInteraccion As Button
    Private WithEvents _btnExportar As Button

    Public Sub New()

        Me.Text = "Gráficas Resumen — Pilas"
        Me.Size = New Size(1180, 680)
        Me.MinimumSize = New Size(900, 520)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        _grafico = GraficosResumen.CrearChart()

        Dim panel = GraficosResumen.CrearPanelLateral()
        _btnCargas = GraficosResumen.AgregarBotonCategoria(panel, "Capacidad de Carga", 0)
        _btnSuelo = GraficosResumen.AgregarBotonCategoria(panel, "Esfuerzos al Suelo", 1)
        _btnCortante = GraficosResumen.AgregarBotonCategoria(panel, "Cortante", 2)
        _btnInteraccion = GraficosResumen.AgregarBotonCategoria(panel, "Flexocompresión", 3)
        _btnExportar = GraficosResumen.AgregarBotonCategoria(panel, "Exportar Imagen", 5, secundario:=True)

        ' El Chart se agrega primero para que el panel Left lo recorte correctamente.
        Me.Controls.Add(_grafico)
        Me.Controls.Add(panel)

        AddHandler Me.Load, AddressOf Form_Load

    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)
        ' Arranca mostrando la revisión más crítica del módulo.
        MostrarCargas()
    End Sub

    ' -----------------------------------------------------------------------
    ' Origen de datos
    ' -----------------------------------------------------------------------
    Private Function ObtenerPilas() As List(Of Elemento_Pila)
        Try
            Dim p = Form_01_PagPilas.Proyecto
            If p Is Nothing OrElse p.Elementos Is Nothing OrElse p.Elementos.Pilas Is Nothing Then
                Return New List(Of Elemento_Pila)
            End If
            Return If(p.Elementos.Pilas.ListaElementos, New List(Of Elemento_Pila))
        Catch ex As Exception
            Logger.Error("Gráficas Pilas — obtener datos: " & ex.Message)
            Return New List(Of Elemento_Pila)
        End Try
    End Function

    Private Function EtiquetaPila(p As Elemento_Pila) As String
        If Not String.IsNullOrWhiteSpace(p.Name_Label) Then Return p.Name_Label
        Return p.Name_Elemento
    End Function

    Private Function HayPilas(pilas As List(Of Elemento_Pila)) As Boolean
        If pilas Is Nothing OrElse pilas.Count = 0 Then
            MessageBox.Show("No hay pilas cargadas." & vbCrLf &
                            "Importe y calcule el módulo de Pilas primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Return True
    End Function

    ' -----------------------------------------------------------------------
    ' Capacidad de carga (esfuerzos en el concreto)
    ' Mismo criterio que el Reporte de Pilas: gobierna el mínimo de Check1..Check4.
    ' Check5 (tracción) se muestra en el tooltip: no siempre aplica.
    ' -----------------------------------------------------------------------
    Private Sub MostrarCargas()

        Dim pilas = ObtenerPilas()
        If Not HayPilas(pilas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each p In pilas
            Dim checks() As Single = {p.Check1_PsE, p.Check2_PsD, p.Check3_PuE, p.Check4_PuD}
            Dim nombres() As String = {"Ps estática", "Ps dinámica", "Pu estática", "Pu dinámica"}

            ' Sin cálculo: todos los checks en cero.
            If checks.All(Function(c) c <= 0) Then
                items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), -1))
                Continue For
            End If

            Dim peor As Single = Single.MaxValue
            Dim quien As String = "—"
            For i = 0 To checks.Length - 1
                If checks(i) > 0 AndAlso checks(i) < peor Then
                    peor = checks(i)
                    quien = nombres(i)
                End If
            Next

            Dim tip As String = "Gobierna: " & quien
            If p.Check5_PuT > 0 Then
                tip &= vbCrLf & "Tracción (Ch5): " & Math.Round(p.Check5_PuT, 2).ToString("F2")
            End If

            items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), peor, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Capacidad de Carga del Concreto (C/D) — Pilas",
                                        "C/D  (mínimo de Ch1…Ch4)",
                                        "Pila")

    End Sub

    ' -----------------------------------------------------------------------
    ' Esfuerzos transmitidos al suelo (σ admisible / σ actuante)
    ' -----------------------------------------------------------------------
    Private Sub MostrarSuelo()

        Dim pilas = ObtenerPilas()
        If Not HayPilas(pilas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each p In pilas
            Dim rE As Double = p.Relacion_EsfE
            Dim rD As Double = p.Relacion_EsfD

            If rE <= 0 AndAlso rD <= 0 Then
                items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), -1))
                Continue For
            End If

            Dim peor As Double
            Dim quien As String
            If rE <= 0 Then
                peor = rD : quien = "condición dinámica"
            ElseIf rD <= 0 Then
                peor = rE : quien = "condición estática"
            ElseIf rE <= rD Then
                peor = rE : quien = "condición estática"
            Else
                peor = rD : quien = "condición dinámica"
            End If

            Dim tip As String = "Gobierna: " & quien & vbCrLf &
                                "σ estático: " & Math.Round(p.EsfE_Trans, 1).ToString("F1") & " kPa" & vbCrLf &
                                "σ dinámico: " & Math.Round(p.EsfD_Trans, 1).ToString("F1") & " kPa"

            items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), peor, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Esfuerzos Transmitidos al Suelo (C/D) — Pilas",
                                        "C/D = σ admisible / σ transmitido",
                                        "Pila")

    End Sub

    ' -----------------------------------------------------------------------
    ' Cortante — φVn / Vu
    ' -----------------------------------------------------------------------
    Private Sub MostrarCortante()

        Dim pilas = ObtenerPilas()
        If Not HayPilas(pilas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each p In pilas
            If p.FactorShear <= 0 Then
                items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), -1))
                Continue For
            End If

            Dim tip As String = "Vu: " & Math.Round(p.Vu, 1).ToString("F1") & " kN" & vbCrLf &
                                "Vn: " & Math.Round(p.Vn, 1).ToString("F1") & " kN" & vbCrLf &
                                "Refuerzo transversal: " & p.N_Barra_Trans & " @ " &
                                Math.Round(p.Separacion_Trans, 3).ToString("F3") & " m"

            items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), p.FactorShear, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Cortante (C/D) — Pilas",
                                        "C/D = φVn / Vu",
                                        "Pila")

    End Sub

    ' -----------------------------------------------------------------------
    ' Flexocompresión — diagrama de interacción.
    ' Respeta el override manual Factor_Manual_DI (>0 = usar ese), igual que el
    ' resto del módulo (Form_01_PagPilas, Form_Reporte_Pilas).
    ' -----------------------------------------------------------------------
    Private Sub MostrarInteraccion()

        Dim pilas = ObtenerPilas()
        If Not HayPilas(pilas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each p In pilas
            Dim fMan As Single = p.Factor_Manual_DI
            Dim fDiag As Single = If(fMan > 0, fMan, p.Factor_Diagonal)
            Dim fCort As Single = If(fMan > 0, fMan, p.Factor_CortesH)

            If fDiag <= 0 AndAlso fCort <= 0 Then
                items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), -1))
                Continue For
            End If

            Dim peor As Double
            Dim quien As String
            Dim combo As String
            If fDiag <= 0 Then
                peor = fCort : quien = "cortes horizontales" : combo = p.combinacion_Factor_CortesH
            ElseIf fCort <= 0 Then
                peor = fDiag : quien = "diagonal" : combo = p.Combinacion_Factor_Diagonal
            ElseIf fDiag <= fCort Then
                peor = fDiag : quien = "diagonal" : combo = p.Combinacion_Factor_Diagonal
            Else
                peor = fCort : quien = "cortes horizontales" : combo = p.combinacion_Factor_CortesH
            End If

            Dim tip As String = "Gobierna: " & quien
            If Not String.IsNullOrWhiteSpace(combo) Then tip &= vbCrLf & "Combinación: " & combo
            If fMan > 0 Then tip &= vbCrLf & "(Factor manual ingresado por el usuario)"

            items.Add(New GraficosResumen.ItemCD(EtiquetaPila(p), peor, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Flexocompresión (C/D) — Pilas",
                                        "C/D  (diagrama de interacción)",
                                        "Pila")

    End Sub

    ' -----------------------------------------------------------------------
    ' Eventos
    ' -----------------------------------------------------------------------
    Private Sub _btnCargas_Click(sender As Object, e As EventArgs) Handles _btnCargas.Click
        MostrarCargas()
    End Sub

    Private Sub _btnSuelo_Click(sender As Object, e As EventArgs) Handles _btnSuelo.Click
        MostrarSuelo()
    End Sub

    Private Sub _btnCortante_Click(sender As Object, e As EventArgs) Handles _btnCortante.Click
        MostrarCortante()
    End Sub

    Private Sub _btnInteraccion_Click(sender As Object, e As EventArgs) Handles _btnInteraccion.Click
        MostrarInteraccion()
    End Sub

    Private Sub _btnExportar_Click(sender As Object, e As EventArgs) Handles _btnExportar.Click
        GraficosResumen.ExportarImagen(_grafico, "Grafico_Pilas")
    End Sub

End Class
