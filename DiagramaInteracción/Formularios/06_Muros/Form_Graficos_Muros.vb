Imports System.Windows.Forms.DataVisualization.Charting

''' <summary>
''' Dashboard de gráficas resumen del módulo de Muros. Muestra el estado de
''' TODOS los muros del proyecto en las revisiones del módulo, sin tener que
''' recorrerlos uno a uno.
'''
''' No recalcula nada: reutiliza la agregación por muro (peor sección de toda la
''' altura) ya validada en <see cref="MurosResumenService.CalcularFila"/>,
''' de modo que el gráfico y el resumen ejecutivo nunca puedan contradecirse.
''' </summary>
Public Class Form_Graficos_Muros
    Inherits Form

    ''' <summary>Límite de ALR alto ya usado por GraficarALRMuros (35 %).</summary>
    Private Const LIMITE_ALR As Double = 0.35

    Private ReadOnly _grafico As Chart
    Private WithEvents _btnFlexo As Button
    Private WithEvents _btnCortante As Button
    Private WithEvents _btnALR As Button
    Private WithEvents _btnBorde As Button
    Private WithEvents _btnExportar As Button

    Private _filas As List(Of FilaMuro)

    Public Sub New()

        Me.Text = "Gráficas Resumen — Muros Estructurales"
        Me.Size = New Size(1180, 680)
        Me.MinimumSize = New Size(900, 520)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9.5F)

        _grafico = GraficosResumen.CrearChart()

        Dim panel = GraficosResumen.CrearPanelLateral()
        _btnFlexo = GraficosResumen.AgregarBotonCategoria(panel, "Flexocompresión", 0)
        _btnCortante = GraficosResumen.AgregarBotonCategoria(panel, "Cortante", 1)
        _btnALR = GraficosResumen.AgregarBotonCategoria(panel, "ALR", 2)
        _btnBorde = GraficosResumen.AgregarBotonCategoria(panel, "Elementos de Borde", 3)
        _btnExportar = GraficosResumen.AgregarBotonCategoria(panel, "Exportar Imagen", 5, secundario:=True)

        Me.Controls.Add(_grafico)
        Me.Controls.Add(panel)

        AddHandler Me.Load, AddressOf Form_Load

    End Sub

    Private Sub Form_Load(sender As Object, e As EventArgs)
        MostrarFlexo()
    End Sub

    ' -----------------------------------------------------------------------
    ' Origen de datos — misma agregación que el Resumen Ejecutivo de Muros.
    ' Se calcula una sola vez por apertura del formulario.
    ' -----------------------------------------------------------------------
    Private Function ObtenerFilas() As List(Of FilaMuro)

        If _filas IsNot Nothing Then Return _filas

        Try
            Dim p = Form_06_PagMuros.proyecto
            If p Is Nothing OrElse p.Elementos Is Nothing OrElse p.Elementos.Muros Is Nothing Then
                _filas = New List(Of FilaMuro)
                Return _filas
            End If

            Dim muros = p.Elementos.Muros.Lista_Muros
            If muros Is Nothing OrElse muros.Count = 0 Then
                _filas = New List(Of FilaMuro)
                Return _filas
            End If

            _filas = muros.Select(AddressOf MurosResumenService.CalcularFila).ToList()

        Catch ex As Exception
            Logger.Error("Gráficas Muros — obtener datos: " & ex.Message)
            _filas = New List(Of FilaMuro)
        End Try

        Return _filas

    End Function

    Private Function HayMuros(filas As List(Of FilaMuro)) As Boolean
        If filas Is Nothing OrElse filas.Count = 0 Then
            MessageBox.Show("No hay muros cargados." & vbCrLf &
                            "Importe y procese el módulo de Muros primero.",
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Return True
    End Function

    ' -----------------------------------------------------------------------
    ' Flexocompresión — peor C/D entre Top y Bot de todas las secciones
    ' -----------------------------------------------------------------------
    Private Sub MostrarFlexo()

        Dim filas = ObtenerFilas()
        If Not HayMuros(filas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each f In filas
            ' FFlexMin = -1 significa "sin cálculo"; ItemCD usa la misma convención.
            Dim tip As String = "Dirección: " & f.Direccion
            If f.FFlexMin >= 0 Then tip &= vbCrLf & "Piso crítico: " & f.PisoCriticoFlex
            items.Add(New GraficosResumen.ItemCD(f.Label, f.FFlexMin, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Flexocompresión (C/D) — Muros",
                                        "C/D  (peor sección de la altura)",
                                        "Muro")

    End Sub

    ' -----------------------------------------------------------------------
    ' Cortante — peor C/D entre todas las secciones
    ' -----------------------------------------------------------------------
    Private Sub MostrarCortante()

        Dim filas = ObtenerFilas()
        If Not HayMuros(filas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each f In filas
            Dim tip As String = "Dirección: " & f.Direccion
            If f.FCortMin >= 0 Then
                tip &= vbCrLf & "Piso crítico: " & f.PisoCriticoCort
                If f.PorcVs > 0 Then
                    tip &= vbCrLf & "% Vs tomado: " & Math.Round(f.PorcVs * 100, 1).ToString("F1") & " %"
                End If
            End If
            items.Add(New GraficosResumen.ItemCD(f.Label, f.FCortMin, tip))
        Next

        GraficosResumen.DibujarBarrasCD(_grafico, items,
                                        "Cortante (C/D) — Muros",
                                        "C/D  (peor sección de la altura)",
                                        "Muro")

    End Sub

    ' -----------------------------------------------------------------------
    ' ALR — Relación de carga axial (menor es mejor).
    ' Se grafica la condición dinámica (ALR_D), que es la que gobierna, contra
    ' el límite alto de 0.35 que ya usa GraficarALRMuros.
    ' -----------------------------------------------------------------------
    Private Sub MostrarALR()

        Dim filas = ObtenerFilas()
        If Not HayMuros(filas) Then Return

        Dim items As New List(Of GraficosResumen.ItemCD)

        For Each f In filas
            Dim alr As Double = Math.Max(f.ALR_D, f.ALR_G)
            If alr <= 0 Then
                items.Add(New GraficosResumen.ItemCD(f.Label, -1))
                Continue For
            End If

            Dim tip As String = "Dirección: " & f.Direccion & vbCrLf &
                                "ALR gravitacional: " & Math.Round(f.ALR_G, 3).ToString("F3") & vbCrLf &
                                "ALR dinámica: " & Math.Round(f.ALR_D, 3).ToString("F3")

            items.Add(New GraficosResumen.ItemCD(f.Label, alr, tip))
        Next

        GraficosResumen.DibujarBarrasLimiteSuperior(_grafico, items,
                                                    "Relación de Carga Axial (ALR) — Muros",
                                                    "ALR = Pu / (Ag · f'c)",
                                                    LIMITE_ALR,
                                                    "Muro",
                                                    "Límite ALR alto = 0.35")

    End Sub

    ' -----------------------------------------------------------------------
    ' Elementos de Borde — conteo de muros por la peor condición de EB
    ' (se cuenta el peor entre el borde izquierdo y el derecho).
    ' -----------------------------------------------------------------------
    Private Sub MostrarElementosBorde()

        Dim filas = ObtenerFilas()
        If Not HayMuros(filas) Then Return

        Dim nEsp As Integer = 0
        Dim nNoEsp As Integer = 0
        Dim nNoReq As Integer = 0

        For Each f In filas
            If f.EBIzq = "Especializado" OrElse f.EBDer = "Especializado" Then
                nEsp += 1
            ElseIf f.EBIzq = "No Especial" OrElse f.EBDer = "No Especial" Then
                nNoEsp += 1
            Else
                nNoReq += 1
            End If
        Next

        Dim categorias As New List(Of String) From {"Especializado", "No Especial", "No Requiere"}
        Dim conteos As New List(Of Integer) From {nEsp, nNoEsp, nNoReq}
        Dim colores As New List(Of Color) From {GraficosResumen.ColRojo,
                                                GraficosResumen.ColNaranja,
                                                GraficosResumen.ColVerde}

        GraficosResumen.DibujarBarrasCategorias(_grafico, categorias, conteos, colores,
                                                "Elementos de Borde requeridos — Muros",
                                                "Cantidad de muros",
                                                "Condición de borde (la peor de los dos extremos)")

    End Sub

    ' -----------------------------------------------------------------------
    ' Eventos
    ' -----------------------------------------------------------------------
    Private Sub _btnFlexo_Click(sender As Object, e As EventArgs) Handles _btnFlexo.Click
        MostrarFlexo()
    End Sub

    Private Sub _btnCortante_Click(sender As Object, e As EventArgs) Handles _btnCortante.Click
        MostrarCortante()
    End Sub

    Private Sub _btnALR_Click(sender As Object, e As EventArgs) Handles _btnALR.Click
        MostrarALR()
    End Sub

    Private Sub _btnBorde_Click(sender As Object, e As EventArgs) Handles _btnBorde.Click
        MostrarElementosBorde()
    End Sub

    Private Sub _btnExportar_Click(sender As Object, e As EventArgs) Handles _btnExportar.Click
        GraficosResumen.ExportarImagen(_grafico, "Grafico_Muros")
    End Sub

End Class
