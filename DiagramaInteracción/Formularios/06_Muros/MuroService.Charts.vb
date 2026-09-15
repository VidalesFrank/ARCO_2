Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Drawing
Imports ARCO.eNumeradores
Imports ARCO.Form_06_PagMuros
Imports ARCO.Funciones_00_Varias
Imports Colores = ARCO.ColoresProyecto

' Partial de MuroService: renderizado de gráficos del módulo Muros.
' Contiene los 6 métodos de dibujo (porcentaje sísmico, aspect ratio, planta,
' derivas, ALR, tw vs altura) + el helper de configuración de ejes (OrganizaEje)
' + CalcularGeometriaMuros (usado por rendering pero también por reportes).
Partial Public Class MuroService

    Public Shared Sub GraficoPorcentajeMuros(chart1 As Chart, Size_Title_Axis As Integer, Size_Value_Axis As Integer, Size_Legend As Integer)

        chart1.Series.Clear()

        Dim Lista_MurosProtagonicos_L As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Protagonico And p.Direccion = eNumeradores.eDireccion.X).OrderByDescending(Function(p) p.Porc_Vs).ToList()
        Dim Lista_MurosComplemento_L As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Complemento And p.Direccion = eNumeradores.eDireccion.X).OrderByDescending(Function(p) p.Porc_Vs).ToList()

        Dim series_Muros_L As New Series("Muros")
        With series_Muros_L
            .ChartType = SeriesChartType.Column
            .Color = Color.Silver
        End With

        series_Muros_L("PointWidth") = "0.9"

        Dim Protagonicos_L As New Series("Protagónicos Longitudinal")
        With Protagonicos_L
            .ChartType = SeriesChartType.Column
            .Color = ColorTranslator.FromHtml("#1874CD")
            .BorderColor = Color.Black
            .BorderWidth = 1
        End With
        Protagonicos_L("PointWidth") = "0.9"

        Dim Acumulado_Longitudinal As New Series("%Vbasal acumulado")
        With Acumulado_Longitudinal
            .ChartType = SeriesChartType.Spline
            .Color = Color.Black
            .BorderWidth = 3
            .XAxisType = AxisType.Secondary
            .YAxisType = AxisType.Secondary
        End With

        Acumulado_Longitudinal.Points.AddXY(0, 0)

        Dim val_X_L As Single = 1
        Dim Acum_Y_L As Single = 0

        For i = 0 To Lista_MurosProtagonicos_L.Count - 1
            series_Muros_L.Points.AddXY(Lista_MurosProtagonicos_L(i).Name, 0)
            Protagonicos_L.Points.AddXY(Lista_MurosProtagonicos_L(i).Name, Lista_MurosProtagonicos_L(i).Porc_Vs * 100)
            Acum_Y_L += Lista_MurosProtagonicos_L(i).Porc_Vs * 100
            val_X_L += 1
            Acumulado_Longitudinal.Points.AddXY(val_X_L, Acum_Y_L)
        Next

        For i = 0 To Lista_MurosComplemento_L.Count - 1
            series_Muros_L.Points.AddXY(Lista_MurosComplemento_L(i).Name, Lista_MurosComplemento_L(i).Porc_Vs * 100)
            Protagonicos_L.Points.AddXY(Lista_MurosComplemento_L(i).Name, 0)
            Acum_Y_L += Lista_MurosComplemento_L(i).Porc_Vs * 100
            val_X_L += 1
            Acumulado_Longitudinal.Points.AddXY(val_X_L, Acum_Y_L)
        Next

        ' =========== DIRECCION TRANSVERSAL ===============
        Dim Lista_MurosProtagonicos_T As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Protagonico And p.Direccion = eNumeradores.eDireccion.Y).OrderByDescending(Function(p) p.Porc_Vs).ToList()
        Dim Lista_MurosComplemento_T As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Complemento And p.Direccion = eNumeradores.eDireccion.Y).OrderByDescending(Function(p) p.Porc_Vs).ToList()

        Dim series_Muros_T As New Series("Muros-T")
        With series_Muros_T
            .ChartType = SeriesChartType.Column
            .Color = Color.Silver
            .IsVisibleInLegend = False
        End With

        series_Muros_T("PointWidth") = "0.9"

        Dim Protagonicos_T As New Series("Protagónicos Transversal")
        With Protagonicos_T
            .ChartType = SeriesChartType.Column
            .Color = ColorTranslator.FromHtml("#FC4E07")
            .BorderColor = Color.Black
            .BorderWidth = 1
        End With

        Protagonicos_T("PointWidth") = "0.9"

        Dim Acumulado_Transversal As New Series("%Vbasal acumulado-T")
        With Acumulado_Transversal
            .ChartType = SeriesChartType.Spline
            .Color = Color.Black
            .BorderWidth = 3
            .XAxisType = AxisType.Secondary
            .YAxisType = AxisType.Secondary
            .IsVisibleInLegend = False
        End With

        Acumulado_Transversal.Points.AddXY(0, 0)

        Dim val_X_T As Single = 1
        Dim Acum_Y_T As Single = 0

        For i = 0 To Lista_MurosProtagonicos_T.Count - 1
            series_Muros_T.Points.AddXY(Lista_MurosProtagonicos_T(i).Name, 0)
            Protagonicos_T.Points.AddXY(Lista_MurosProtagonicos_T(i).Name, Lista_MurosProtagonicos_T(i).Porc_Vs * 100)
            Acum_Y_T += Lista_MurosProtagonicos_T(i).Porc_Vs * 100
            val_X_T += 1
            Acumulado_Transversal.Points.AddXY(val_X_T, Acum_Y_T)
        Next

        For i = 0 To Lista_MurosComplemento_T.Count - 1
            series_Muros_T.Points.AddXY(Lista_MurosComplemento_T(i).Name, Lista_MurosComplemento_T(i).Porc_Vs * 100)
            Protagonicos_T.Points.AddXY(Lista_MurosComplemento_T(i).Name, 0)
            Acum_Y_T += Lista_MurosComplemento_T(i).Porc_Vs * 100
            val_X_T += 1
            Acumulado_Transversal.Points.AddXY(val_X_T, Acum_Y_T)
        Next

        chart1.Series.Add(series_Muros_L)
        chart1.Series.Add(Protagonicos_L)
        chart1.Series.Add(series_Muros_T)
        chart1.Series.Add(Protagonicos_T)

        chart1.Series.Add(Acumulado_Longitudinal)
        chart1.Series.Add(Acumulado_Transversal)

        series_Muros_L.ChartArea = "ChartArea1"
        Protagonicos_L.ChartArea = "ChartArea1"
        series_Muros_T.ChartArea = "ChartArea2"
        Protagonicos_T.ChartArea = "ChartArea2"
        Acumulado_Longitudinal.ChartArea = "ChartArea1"
        Acumulado_Transversal.ChartArea = "ChartArea2"

        '=========== AJUSTE DE EJES DE GRAFICOS DE MUROS PROTAGONICOS =============

        '======== GRAFICO LONGITUDINAL ===========
        Dim myfontFamily As FontFamily = New FontFamily("Times New Roman")

        '====== EJE X - PRINCIPAL =======

        Dim axis_x = chart1.ChartAreas("ChartArea1").AxisX
        OrganizaEje(axis_x, myfontFamily, "Muros", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x
            .LabelStyle.Enabled = False
        End With

        '====== EJE Y - PRINCIPAL =======
        Dim Valor_Ymax As Single = Math.Max(Lista_MurosProtagonicos_L.Max(Function(p) p.Porc_Vs), Lista_MurosProtagonicos_T.Max(Function(p) p.Porc_Vs))

        Dim axis_y = chart1.ChartAreas("ChartArea1").AxisY
        OrganizaEje(axis_y, myfontFamily, "Porcentaje de corte sísmico (%)", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y
            .Minimum = 0
            .Maximum = 1.1 * Valor_Ymax * 100
            .Interval = 2
            .MajorTickMark.Enabled = True
        End With

        '====== EJE X - SECUNDARIO =======
        Dim axis_x2 = chart1.ChartAreas("ChartArea1").AxisX2
        OrganizaEje(axis_x2, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x2
            .Minimum = 0
            .Maximum = val_X_L + 1
            .Interval = 20
            .LabelStyle.Enabled = False
        End With

        '======== EJE Y - SECUNDARIO ======
        Dim axis_y2 = chart1.ChartAreas("ChartArea1").AxisY2
        OrganizaEje(axis_y2, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y2
            .Minimum = 0
            .Maximum = 100
            .Interval = 20
            .LabelStyle.Enabled = False
            .MajorTickMark.Enabled = True
        End With

        '======== GRAFICO TRANSVERSAL ===========

        '====== EJE X - PRINCIPAL =======
        Dim axis_x_T = chart1.ChartAreas("ChartArea2").AxisX
        OrganizaEje(axis_x_T, myfontFamily, "Muros", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x_T
            .LabelStyle.Enabled = False
        End With

        '====== EJE Y - PRINCIPAL =======
        Dim axis_y_T = chart1.ChartAreas("ChartArea2").AxisY
        OrganizaEje(axis_y_T, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y_T
            .Minimum = 0
            .Maximum = 1.1 * Valor_Ymax * 100
            .Interval = 2
            .LabelStyle.Enabled = False
            .MajorTickMark.Enabled = True
        End With

        '====== EJE X - SECUNDARIO =======
        Dim axis_x2_T = chart1.ChartAreas("ChartArea2").AxisX2
        OrganizaEje(axis_x2_T, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x2_T
            .Minimum = 0
            .Maximum = val_X_T + 1
            .Interval = 20
            .LabelStyle.Enabled = False
        End With

        '======== EJE Y - SECUNDARIO ======
        Dim axis_y2_T = chart1.ChartAreas("ChartArea2").AxisY2
        OrganizaEje(axis_y2_T, myfontFamily, "Porcentaje acumulado (%)", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y2_T
            .Minimum = 0
            .Maximum = 100
            .Interval = 20
            .MajorTickMark.Enabled = True
        End With

        '==== AJUSTE DE LEYENDA ==========
        chart1.Legends.Clear()
        Dim Legend1 As New Legend("Legend1")
        chart1.Legends.Add(Legend1)
        chart1.Legends(0).Font = New Font(myfontFamily, Size_Legend, FontStyle.Regular)
        chart1.Legends(0).Docking = Docking.Bottom
        chart1.Legends(0).Alignment = StringAlignment.Center

        Dim ChartArea_1 = chart1.ChartAreas("ChartArea1")
        With ChartArea_1
            .Position.Height = 95
            .Position.Width = 49
            .Position.X = 5
            .Position.Y = 2
            .BorderDashStyle = ChartDashStyle.Solid
            .BorderColor = Color.Black
            .BorderWidth = 1
            .InnerPlotPosition.Height = 80
            .InnerPlotPosition.Width = 80
            .InnerPlotPosition.X = 5
            .InnerPlotPosition.Y = 2
        End With

        Dim ChartArea_2 = chart1.ChartAreas("ChartArea2")
        With ChartArea_2
            .Position.Height = 95
            .Position.Width = 49
            .Position.X = 47
            .Position.Y = 2
            .BorderDashStyle = ChartDashStyle.Solid
            .BorderColor = Color.Black
            .BorderWidth = 1
            .InnerPlotPosition.Height = 80
            .InnerPlotPosition.Width = 85
            .InnerPlotPosition.X = 5
            .InnerPlotPosition.Y = 2
        End With

    End Sub
    Public Shared Sub GraficoArMuros(chart1 As Chart, Size_Title_Axis As Integer, Size_Value_Axis As Integer, Size_Legend As Integer)

        chart1.Series.Clear()

        Dim Lista_MurosProtagonicos_L As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Protagonico And p.Direccion = eNumeradores.eDireccion.X).OrderByDescending(Function(p) p.Porc_Vs).ToList()
        Dim Lista_MurosComplemento_L As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Complemento And p.Direccion = eNumeradores.eDireccion.X).OrderByDescending(Function(p) p.Porc_Vs).ToList()

        Dim series_Muros_L As New Series("Muros")
        With series_Muros_L
            .ChartType = SeriesChartType.Column
            .Color = Color.Silver
        End With

        series_Muros_L("PointWidth") = "0.9"

        Dim Protagonicos_L As New Series("Protagónicos Longitudinal")
        With Protagonicos_L
            .ChartType = SeriesChartType.Column
            .Color = ColorTranslator.FromHtml("#1874CD")
            .BorderColor = Color.Black
            .BorderWidth = 1
        End With
        Protagonicos_L("PointWidth") = "0.9"


        Dim val_X_L As Single = 1
        Dim Acum_Y_L As Single = 0

        For i = 0 To Lista_MurosProtagonicos_L.Count - 1
            series_Muros_L.Points.AddXY(Lista_MurosProtagonicos_L(i).Name, 0)
            Protagonicos_L.Points.AddXY(Lista_MurosProtagonicos_L(i).Name, Lista_MurosProtagonicos_L(i).Ar)
            val_X_L += 1
        Next

        For i = 0 To Lista_MurosComplemento_L.Count - 1
            series_Muros_L.Points.AddXY(Lista_MurosComplemento_L(i).Name, Lista_MurosComplemento_L(i).Ar)
            Protagonicos_L.Points.AddXY(Lista_MurosComplemento_L(i).Name, 0)
            val_X_L += 1
        Next

        ' =========== DIRECCION TRANSVERSAL ===============
        Dim Lista_MurosProtagonicos_T As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Protagonico And p.Direccion = eNumeradores.eDireccion.Y).OrderByDescending(Function(p) p.Porc_Vs).ToList()
        Dim Lista_MurosComplemento_T As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Complemento And p.Direccion = eNumeradores.eDireccion.Y).OrderByDescending(Function(p) p.Porc_Vs).ToList()

        Dim series_Muros_T As New Series("Muros-T")
        With series_Muros_T
            .ChartType = SeriesChartType.Column
            .Color = Color.Silver
            .IsVisibleInLegend = False
        End With

        series_Muros_T("PointWidth") = "0.9"

        Dim Protagonicos_T As New Series("Protagónicos Transversal")
        With Protagonicos_T
            .ChartType = SeriesChartType.Column
            .Color = ColorTranslator.FromHtml("#FC4E07")
            .BorderColor = Color.Black
            .BorderWidth = 1
        End With

        Protagonicos_T("PointWidth") = "0.9"

        Dim val_X_T As Single = 1

        For i = 0 To Lista_MurosProtagonicos_T.Count - 1
            series_Muros_T.Points.AddXY(Lista_MurosProtagonicos_T(i).Name, 0)
            Protagonicos_T.Points.AddXY(Lista_MurosProtagonicos_T(i).Name, Lista_MurosProtagonicos_T(i).Ar)
            val_X_T += 1
        Next

        For i = 0 To Lista_MurosComplemento_T.Count - 1
            series_Muros_T.Points.AddXY(Lista_MurosComplemento_T(i).Name, Lista_MurosComplemento_T(i).Ar)
            Protagonicos_T.Points.AddXY(Lista_MurosComplemento_T(i).Name, 0)
            val_X_T += 1
        Next

        chart1.Series.Add(series_Muros_L)
        chart1.Series.Add(Protagonicos_L)
        chart1.Series.Add(series_Muros_T)
        chart1.Series.Add(Protagonicos_T)

        series_Muros_L.ChartArea = "ChartArea1"
        Protagonicos_L.ChartArea = "ChartArea1"
        series_Muros_T.ChartArea = "ChartArea2"
        Protagonicos_T.ChartArea = "ChartArea2"

        '=========== AJUSTE DE EJES DE GRAFICOS DE MUROS PROTAGONICOS =============

        '======== GRAFICO LONGITUDINAL ===========
        Dim myfontFamily As FontFamily = New FontFamily("Times New Roman")

        '====== EJE X - PRINCIPAL =======

        Dim axis_x = chart1.ChartAreas("ChartArea1").AxisX
        OrganizaEje(axis_x, myfontFamily, "Muros", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x
            .LabelStyle.Enabled = False
        End With

        '====== EJE Y - PRINCIPAL =======
        Dim Valor_Ymax As Single = Math.Min(Math.Max(Lista_MurosComplemento_L.Max(Function(p) p.Ar), Lista_MurosComplemento_T.Max(Function(p) p.Ar)), 20)

        Dim axis_y = chart1.ChartAreas("ChartArea1").AxisY
        OrganizaEje(axis_y, myfontFamily, "Relación de Aspecto, Ar", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y
            .Minimum = 0
            .Maximum = 1.1 * Valor_Ymax
            .Interval = 2
            .MajorTickMark.Enabled = True
        End With

        '====== EJE X - SECUNDARIO =======
        Dim axis_x2 = chart1.ChartAreas("ChartArea1").AxisX2
        OrganizaEje(axis_x2, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x2
            .Minimum = 0
            .Maximum = val_X_L + 1
            .Interval = 20
            .LabelStyle.Enabled = False
        End With

        '======== EJE Y - SECUNDARIO ======
        Dim axis_y2 = chart1.ChartAreas("ChartArea1").AxisY2
        OrganizaEje(axis_y2, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y2
            .Minimum = 0
            .Maximum = 100
            .Interval = 20
            .LabelStyle.Enabled = False
            .MajorTickMark.Enabled = True
        End With

        '======== GRAFICO TRANSVERSAL ===========

        '====== EJE X - PRINCIPAL =======
        Dim axis_x_T = chart1.ChartAreas("ChartArea2").AxisX
        OrganizaEje(axis_x_T, myfontFamily, "Muros", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_x_T
            .LabelStyle.Enabled = False
        End With

        '====== EJE Y - PRINCIPAL =======
        Dim axis_y_T = chart1.ChartAreas("ChartArea2").AxisY
        OrganizaEje(axis_y_T, myfontFamily, "", Size_Title_Axis, Size_Value_Axis, Size_Legend)
        With axis_y_T
            .Minimum = 0
            .Maximum = 1.1 * Valor_Ymax
            .Interval = 2
            .LabelStyle.Enabled = False
            .MajorTickMark.Enabled = True
        End With

        '==== AJUSTE DE LEYENDA ==========
        chart1.Legends.Clear()
        Dim Legend1 As New Legend("Legend1")
        chart1.Legends.Add(Legend1)
        chart1.Legends(0).Font = New Font(myfontFamily, Size_Legend, FontStyle.Regular)
        chart1.Legends(0).Docking = Docking.Bottom
        chart1.Legends(0).Alignment = StringAlignment.Center

        Dim ChartArea_1 = chart1.ChartAreas("ChartArea1")
        With ChartArea_1
            .Position.Height = 95
            .Position.Width = 49
            .Position.X = 5
            .Position.Y = 2
            .BorderDashStyle = ChartDashStyle.Solid
            .BorderColor = Color.Black
            .BorderWidth = 1
            .InnerPlotPosition.Height = 80
            .InnerPlotPosition.Width = 80
            .InnerPlotPosition.X = 5
            .InnerPlotPosition.Y = 2
        End With

        Dim ChartArea_2 = chart1.ChartAreas("ChartArea2")
        With ChartArea_2
            .Position.Height = 95
            .Position.Width = 49
            .Position.X = 47
            .Position.Y = 2
            .BorderDashStyle = ChartDashStyle.Solid
            .BorderColor = Color.Black
            .BorderWidth = 1
            .InnerPlotPosition.Height = 80
            .InnerPlotPosition.Width = 85
            .InnerPlotPosition.X = 5
            .InnerPlotPosition.Y = 2
        End With

    End Sub

    Public Shared Sub OrganizaEje(axis As Axis, fuente As FontFamily, Title As String, size_Title As Integer, size_Value_axis As Integer,
                            size_legend As Integer)

        With axis
            .TitleFont = New Font(fuente, size_Title, System.Drawing.FontStyle.Bold)
            .LabelStyle.Font = New Font(fuente, size_Value_axis, FontStyle.Regular)
            .Title = Title
            .MajorGrid.Enabled = False
            .MajorTickMark.TickMarkStyle = TickMarkStyle.AcrossAxis
            .MajorTickMark.Size = 1
            .MajorTickMark.Enabled = False
        End With

    End Sub
    Public Shared Rectangulos As New List(Of Rectangulo)
    Public Shared MaxCoorX As Single = 0
    Public Shared MinCoorX As Single = 0
    Public Shared MaxCoorY As Single = 0
    Public Shared MinCoorY As Single = 0

    Public Shared Sub CalcularGeometriaMuros()

        MaxCoorX = 0
        MinCoorX = 0
        MaxCoorY = 0
        MinCoorY = 0

        For i = 0 To proyecto.Elementos.Muros.Lista_Muros.Count() - 1
            Dim Muro_ As Muro = proyecto.Elementos.Muros.Lista_Muros(i)

            If Muro_.Lista_Secciones(0).Direccion_Muro = eNumeradores.eDireccion.X Then
                Rectangulos.Add(New Rectangulo() With {.Name = Muro_.Name, .Direccion = Muro_.Direccion, .Tipo_M = Muro_.TipoMuro, .CoorX = Muro_.Coor_X, .CoorY = Muro_.Coor_Y, .Largo = Muro_.Lw, .Espesor = Muro_.tw})
                If (Muro_.Coor_X - Muro_.Lw / 2) < MinCoorX Then
                    MinCoorX = Muro_.Coor_X - Muro_.Lw / 2
                End If
                If (Muro_.Coor_X + Muro_.Lw / 2) > MaxCoorX Then
                    MaxCoorX = Muro_.Coor_X + Muro_.Lw / 2
                End If
                If (Muro_.Coor_Y - Muro_.tw / 2) < MinCoorY Then
                    MinCoorY = Muro_.Coor_Y - Muro_.tw / 2
                End If
                If (Muro_.Coor_Y + Muro_.tw / 2) > MaxCoorY Then
                    MaxCoorY = Muro_.Coor_Y + Muro_.tw / 2
                End If

            Else
                Rectangulos.Add(New Rectangulo() With {.Name = Muro_.Name, .Direccion = Muro_.Direccion, .Tipo_M = Muro_.TipoMuro, .CoorX = Muro_.Coor_X, .CoorY = Muro_.Coor_Y, .Largo = Muro_.tw, .Espesor = Muro_.Lw})
                If (Muro_.Coor_X - Muro_.tw / 2) < MinCoorX Then
                    MinCoorX = Muro_.Coor_X - Muro_.tw / 2
                End If
                If (Muro_.Coor_X + Muro_.tw / 2) > MaxCoorX Then
                    MaxCoorX = Muro_.Coor_X + Muro_.tw / 2
                End If
                If (Muro_.Coor_Y - Muro_.Lw / 2) < MinCoorY Then
                    MinCoorY = Muro_.Coor_Y - Muro_.Lw / 2
                End If
                If (Muro_.Coor_Y + Muro_.Lw / 2) > MaxCoorY Then
                    MaxCoorY = Muro_.Coor_Y + Muro_.Lw / 2
                End If
            End If
        Next

    End Sub

    Public Shared Sub GraficosMurosPlanta(ByVal Figura_Tw As PictureBox, ByVal Figura_Protagonicos As PictureBox)

        Figura_Tw.Refresh()
        Figura_Protagonicos.Refresh()

        '================== DEFINICIÓN DE COLORES, ESTILOS Y LAPICES PARA DIBUJO ==========================
        Dim Letra As New System.Drawing.Font("Arial", 10, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_12 As New System.Drawing.Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_14 As New System.Drawing.Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_16 As New System.Drawing.Font("Arial", 16, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_18 As New System.Drawing.Font("Arial", 18, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim CorR As New SolidBrush(Color.Red)
        Dim Pen_R As New Pen(Color.Red)
        Dim Pen_Black As New Pen(Color.Black)
        Dim Pen_Black_Line As New Pen(Color.Black)
        Pen_Black_Line.Width = 2
        Dim Pen_Blue As New Pen(Color.FromArgb(0, 0, 255))
        Dim Pen_Green As New Pen(Color.Green)
        Dim Pen_Orange As New Pen(Color.Orange)
        Dim Pen_Orangered As New Pen(Color.OrangeRed)
        Dim Pen_Magenta As New Pen(Color.Magenta)
        Dim Pen_Skyblue As New Pen(Color.SkyBlue)

        '=================== DEFINICIÓN DE DIMENSIONES DE EDIFICIO Y ESPACIO DE TRABAJO ===================
        Dim Len_X_Edificio As Single = MaxCoorX - MinCoorX
        Dim Len_Y_Edificio As Single = MaxCoorY - MinCoorY
        Dim W_Tablero As Single = Figura_Tw.Width
        Dim H_Tablero As Single = Figura_Tw.Height
        Dim W_Grafico As Single = W_Tablero
        Dim H_Grafico As Single = Figura_Tw.Height * 10 / 11
        Dim W_Leyenda As Single = W_Tablero
        Dim H_Leyenda As Single = Figura_Tw.Height / 11

        Dim W_Medida_X As Single = W_Grafico * 14 / 15
        Dim H_Medida_X As Single = H_Grafico / 15

        Dim W_Medida_Y As Single = W_Grafico / 15
        Dim H_Medida_Y As Single = H_Grafico * 12 / 13

        Dim W_Grafico_Final As Single = W_Grafico * 14 / 15
        Dim CooX_Grafico_Final As Single = W_Grafico / 15
        Dim H_Grafico_Final As Single = H_Grafico * 14 / 15


        Dim Rel_Aspect = Len_X_Edificio / Len_Y_Edificio
        Dim F_escala As Single = W_Grafico_Final / Len_X_Edificio
        If Len_X_Edificio < Len_Y_Edificio Then
            F_escala = H_Grafico_Final / Len_Y_Edificio
        End If

        Dim Coor_X_Base As Single = W_Medida_Y
        Dim Coor_Y_Base As Single = H_Medida_Y
        Dim Coor_X_Edificio As Single = MinCoorX
        Dim Coor_Y_Edificio As Single = MaxCoorY - MinCoorY

        '====================  GRÁFICO DE MUROS CON VARIACIÓN DE ESPESOR =====================
        Dim bmp As New Bitmap(Figura_Tw.Width, Figura_Tw.Height)

        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.White)

            Dim List_tw As New List(Of String)
            Dim List_Colors As New List(Of Brush)

            Dim Coor_X_Min = 0.95 * CooX_Grafico_Final + Coor_X_Edificio * F_escala
            Dim Coor_X_Max = 0.95 * CooX_Grafico_Final + Len_X_Edificio * F_escala

            Dim Coor_Y_Min = 0
            Dim Coor_Y_Max = Coor_Y_Edificio * F_escala

            For Each rectangulo In Rectangulos
                Dim CoorX_Real As Single = rectangulo.CoorX - Coor_X_Edificio
                Dim CoorY_Real As Single = Coor_Y_Edificio - (rectangulo.CoorY - MinCoorY)

                Dim x_Centro As Single = 0.95 * CooX_Grafico_Final + CoorX_Real * F_escala
                Dim x As Single = 0.95 * CooX_Grafico_Final + (CoorX_Real - rectangulo.Largo / 2) * F_escala

                Dim y_Centro As Single = CoorY_Real * F_escala
                Dim y As Single = (CoorY_Real - rectangulo.Espesor / 2) * F_escala

                Dim Pencil As Pen
                Dim Colors As Brush

                If Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.2 Then
                    Pencil = Pen_Green
                    List_tw.Add("0.20")
                    Colors = Brushes.Green
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.15 Then
                    Pencil = Pen_Blue
                    List_tw.Add("0.15")
                    Colors = Brushes.Blue
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.12 Then
                    Pencil = Pen_Magenta
                    List_tw.Add("0.12")
                    Colors = Brushes.Magenta
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.1 Then
                    Pencil = Pen_Skyblue
                    List_tw.Add("0.10")
                    Colors = Brushes.SkyBlue
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.25 Then
                    Pencil = Pen_Orangered
                    List_tw.Add("0.25")
                    Colors = Brushes.OrangeRed
                Else
                    Pencil = Pen_Black
                    List_tw.Add("Other")
                    Colors = Brushes.Black
                End If
                g.FillRectangle(Colors, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)
                g.DrawRectangle(Pencil, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)

                g.DrawString(rectangulo.Name, Letra_12, Brushes.Black, New PointF(x_Centro, y_Centro))

                Dim Y_Linea As Single = Coor_Y_Max * 1.1
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea), New PointF(Coor_X_Max, Y_Linea))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea + 10), New PointF(Coor_X_Min, Y_Linea - 10))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Max, Y_Linea + 10), New PointF(Coor_X_Max, Y_Linea - 10))
                g.DrawString(Convert.ToString(Math.Round(Len_X_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF((Coor_X_Min + Coor_X_Max) / 2, Y_Linea - 20))

                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2, 0), New PointF(W_Medida_Y / 2, Coor_Y_Max))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, 0), New PointF(W_Medida_Y / 2 - 10, 0))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, Coor_Y_Max), New PointF(W_Medida_Y / 2 - 10, Coor_Y_Max))
                g.DrawString(Convert.ToString(Math.Round(Len_Y_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF(W_Medida_Y / 2, Coor_Y_Max / 2))

            Next

            Dim Uniques_Tw As List(Of String) = List_tw.Distinct().ToList()
            H_Leyenda = Coor_Y_Max * 1.1 + 50

            For i = 0 To Uniques_Tw.Count - 1
                Dim x_1 As Single = W_Leyenda / 4 + (i + 1) * (Figura_Tw.Width / 2 - (60 * Uniques_Tw.Count) / 2) / (Uniques_Tw.Count + 1) + i * 60
                Dim y_1 As Single = H_Leyenda
                Dim Colors As Brush

                If Uniques_Tw(i) = "0.20" Then
                    Colors = Brushes.Green
                ElseIf Uniques_Tw(i) = "0.15" Then
                    Colors = Brushes.Blue
                ElseIf Uniques_Tw(i) = "0.12" Then
                    Colors = Brushes.Magenta
                ElseIf Uniques_Tw(i) = "0.10" Then
                    Colors = Brushes.SkyBlue
                ElseIf Uniques_Tw(i) = "0.25" Then
                    Colors = Brushes.OrangeRed
                Else
                    Colors = Brushes.Black
                End If

                g.FillRectangle(Colors, x_1, y_1, 25, 25)
                g.DrawString(Convert.ToString("Tw = " + Uniques_Tw(i) + " m"), Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1))
            Next
            g.Dispose()

        End Using
        Figura_Tw.Image = bmp


        Figura_Protagonicos.Refresh()
        Dim bmp_mp As New Bitmap(Figura_Protagonicos.Width, Figura_Protagonicos.Height)

        Using g As Graphics = Graphics.FromImage(bmp_mp)
            g.Clear(Color.White)

            Dim list_tw As New List(Of String)
            Dim list_colors As New List(Of Brush)

            Dim Coor_X_Min = 0.95 * CooX_Grafico_Final + Coor_X_Edificio * F_escala
            Dim Coor_X_Max = 0.95 * CooX_Grafico_Final + Len_X_Edificio * F_escala

            Dim Coor_Y_Min = 0
            Dim Coor_Y_Max = Coor_Y_Edificio * F_escala

            Dim colorrgb_Pr_X As Color = ColorTranslator.FromHtml("#1874cd")
            Dim colorrgb_Pr_Y As Color = ColorTranslator.FromHtml("#fc4e07")

            For Each rectangulo In Rectangulos
                Dim CoorX_Real As Single = rectangulo.CoorX - Coor_X_Edificio
                Dim CoorY_Real As Single = Coor_Y_Edificio - (rectangulo.CoorY - MinCoorY)

                Dim x_Centro As Single = 0.95 * CooX_Grafico_Final + CoorX_Real * F_escala
                Dim x As Single = 0.95 * CooX_Grafico_Final + (CoorX_Real - rectangulo.Largo / 2) * F_escala

                Dim y_Centro As Single = CoorY_Real * F_escala
                Dim y As Single = (CoorY_Real - rectangulo.Espesor / 2) * F_escala

                Dim pencil As Pen
                Dim colors As Brush

                If rectangulo.Tipo_M = eNumeradores.eTipoMuro.Protagonico Then
                    If rectangulo.Direccion = eNumeradores.eDireccion.X Then

                        Dim pen As New Pen(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
                        pencil = pen

                        Dim col As New SolidBrush(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
                        colors = col

                    Else

                        Dim pen As New Pen(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))
                        pencil = pen

                        Dim col As New SolidBrush(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))
                        colors = col

                    End If
                Else
                    pencil = New Pen(Color.Silver)
                    colors = Brushes.Silver
                End If

                g.FillRectangle(colors, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)
                g.DrawRectangle(pencil, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)

                g.DrawString(rectangulo.Name, Letra_12, Brushes.Black, New PointF(x_Centro, y_Centro))

                Dim Y_Linea As Single = Coor_Y_Max * 1.1
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea), New PointF(Coor_X_Max, Y_Linea))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea + 10), New PointF(Coor_X_Min, Y_Linea - 10))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Max, Y_Linea + 10), New PointF(Coor_X_Max, Y_Linea - 10))
                g.DrawString(Convert.ToString(Math.Round(Len_X_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF((Coor_X_Min + Coor_X_Max) / 2, Y_Linea - 20))

                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2, 0), New PointF(W_Medida_Y / 2, Coor_Y_Max))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, 0), New PointF(W_Medida_Y / 2 - 10, 0))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, Coor_Y_Max), New PointF(W_Medida_Y / 2 - 10, Coor_Y_Max))
                g.DrawString(Convert.ToString(Math.Round(Len_Y_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF(W_Medida_Y / 2, Coor_Y_Max / 2))

            Next

            Dim Uniques_Tw As List(Of String) = list_tw.Distinct().ToList()
            H_Leyenda = Coor_Y_Max * 1.1 + 50

            Dim pen_PrX As New Pen(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
            Dim pen_PrY As New Pen(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))
            Dim colB_PrX As New SolidBrush(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
            Dim colB_PrY As New SolidBrush(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))

            Dim Texto_1 As String = "Muros Protagónicos Dir. Longitudinal (X)"
            Dim Texto_2 As String = "Muros Protagónicos Dir. Transversal (Y)"
            Dim Texto_3 As String = "Muros"

            Dim AnchoText_Total As Single = g.MeasureString(Texto_1, Letra_16).Width + g.MeasureString(Texto_2, Letra_16).Width + g.MeasureString(Texto_3, Letra_16).Width + 70
            Dim AltoTest As Single = g.MeasureString(Texto_1, Letra_16).Height

            Dim x_1 As Single = (W_Leyenda - AnchoText_Total) / 2
            Dim y_1 As Single = H_Leyenda
            Dim y_1_T As Single = H_Leyenda + (25 - AltoTest) / 2

            g.FillRectangle(colB_PrX, x_1, y_1, 25, 25)
            g.DrawString(Texto_1, Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1_T))
            Dim AnchoText As Single = g.MeasureString(Texto_1, Letra_16).Width

            x_1 = x_1 + AnchoText + 35
            g.FillRectangle(colB_PrY, x_1, y_1, 25, 25)
            g.DrawString(Texto_2, Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1_T))
            AnchoText = g.MeasureString(Texto_2, Letra_16).Width

            x_1 = x_1 + AnchoText + 35
            g.FillRectangle(Brushes.Silver, x_1, y_1, 25, 25)
            g.DrawString("Muros", Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1_T))

            g.Dispose()

        End Using
        Figura_Protagonicos.Image = bmp_mp
    End Sub

    Public Shared Sub FiguraMurosPlanta(ByVal Figura_Muros As PictureBox)

        Figura_Muros.Refresh()
        Figura_Muros.Refresh()

        '================== DEFINICIÓN DE COLORES, ESTILOS Y LAPICES PARA DIBUJO ==========================
        Dim Letra_8 As New System.Drawing.Font("Arial", 8, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_9 As New System.Drawing.Font("Arial", 9, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim CorR As New SolidBrush(Color.Red)
        Dim Pen_R As New Pen(Color.Red)
        Dim Pen_Black As New Pen(Color.Black)
        Dim Pen_Black_Line As New Pen(Color.Black)
        Pen_Black_Line.Width = 1
        Dim Pen_Blue As New Pen(Color.FromArgb(0, 0, 255))
        Dim Pen_Green As New Pen(Color.Green)
        Dim Pen_Orange As New Pen(Color.Orange)
        Dim Pen_Orangered As New Pen(Color.OrangeRed)
        Dim Pen_Magenta As New Pen(Color.Magenta)
        Dim Pen_Skyblue As New Pen(Color.SkyBlue)

        '=================== DEFINICIÓN DE DIMENSIONES DE EDIFICIO Y ESPACIO DE TRABAJO ===================
        Dim Len_X_Edificio As Single = MaxCoorX - MinCoorX
        Dim Len_Y_Edificio As Single = MaxCoorY - MinCoorY
        Dim W_Tablero As Single = Figura_Muros.Width
        Dim H_Tablero As Single = Figura_Muros.Height
        Dim W_Grafico As Single = W_Tablero
        Dim H_Grafico As Single = Figura_Muros.Height * 10 / 11
        Dim W_Leyenda As Single = W_Tablero
        Dim H_Leyenda As Single = Figura_Muros.Height / 11

        Dim W_Medida_X As Single = W_Grafico * 14 / 15
        Dim H_Medida_X As Single = H_Grafico / 15

        Dim W_Medida_Y As Single = W_Grafico / 15
        Dim H_Medida_Y As Single = H_Grafico * 12 / 13

        Dim W_Grafico_Final As Single = W_Grafico * 14 / 15
        Dim CooX_Grafico_Final As Single = W_Grafico / 15
        Dim H_Grafico_Final As Single = H_Grafico * 14 / 15


        Dim Rel_Aspect = Len_X_Edificio / Len_Y_Edificio
        Dim F_escala As Single = W_Grafico_Final / Len_X_Edificio
        If Len_X_Edificio < Len_Y_Edificio Then
            F_escala = H_Grafico_Final / Len_Y_Edificio
        End If

        Dim Coor_X_Base As Single = W_Medida_Y
        Dim Coor_Y_Base As Single = H_Medida_Y
        Dim Coor_X_Edificio As Single = MinCoorX
        Dim Coor_Y_Edificio As Single = MaxCoorY - MinCoorY

        '====================  GRÁFICO DE MUROS EN LA PLANTA =====================
        Dim bmp As New Bitmap(Figura_Muros.Width, Figura_Muros.Height)

        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.White)

            Dim List_tw As New List(Of String)
            Dim List_Colors As New List(Of Brush)

            Dim Coor_X_Min = 0.95 * CooX_Grafico_Final + Coor_X_Edificio * F_escala
            Dim Coor_X_Max = 0.95 * CooX_Grafico_Final + Len_X_Edificio * F_escala

            Dim Coor_Y_Min = 0
            Dim Coor_Y_Max = Coor_Y_Edificio * F_escala

            For Each rectangulo In Rectangulos
                Dim CoorX_Real As Single = rectangulo.CoorX - Coor_X_Edificio
                Dim CoorY_Real As Single = Coor_Y_Edificio - (rectangulo.CoorY - MinCoorY)

                Dim x_Centro As Single = 0.95 * CooX_Grafico_Final + CoorX_Real * F_escala
                Dim x As Single = 0.95 * CooX_Grafico_Final + (CoorX_Real - rectangulo.Largo / 2) * F_escala

                Dim y_Centro As Single = CoorY_Real * F_escala
                Dim y As Single = (CoorY_Real - rectangulo.Espesor / 2) * F_escala

                Dim Pencil As Pen
                Dim Colors As Brush

                Pencil = Pen_Black
                List_tw.Add("Other")
                Colors = Brushes.Silver

                g.FillRectangle(Colors, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)
                g.DrawRectangle(Pencil, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)

                'g.DrawString(rectangulo.Name, Letra_12, Brushes.Black, New PointF(x_Centro, y_Centro))

                Dim Y_Linea As Single = Coor_Y_Max * 1.1
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea), New PointF(Coor_X_Max, Y_Linea))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea + 6), New PointF(Coor_X_Min, Y_Linea - 6))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Max, Y_Linea + 6), New PointF(Coor_X_Max, Y_Linea - 6))
                g.DrawString(Convert.ToString(Math.Round(Len_X_Edificio, 1) & " m"), Letra_9, Brushes.Black, New PointF((Coor_X_Min + Coor_X_Max) / 2 - 5, Y_Linea + 5))

                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2, 0), New PointF(W_Medida_Y / 2, Coor_Y_Max))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 6, 0), New PointF(W_Medida_Y / 2 - 6, 0))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 6, Coor_Y_Max), New PointF(W_Medida_Y / 2 - 6, Coor_Y_Max))

                ' Guarda la transformación actual
                Dim oldTransform As Drawing2D.Matrix = g.Transform.Clone()

                ' Rotar el texto 90°
                g.TranslateTransform(W_Medida_Y / 2, Coor_Y_Max / 2 + 10)
                g.RotateTransform(-90)
                g.DrawString(Convert.ToString(Math.Round(Len_Y_Edificio, 1) & " m"), Letra_9, Brushes.Black, New PointF(0, 0))

                ' Restablece la transformación original
                g.Transform = oldTransform

            Next

        End Using

        Figura_Muros.Image = bmp

    End Sub

    Public Shared Sub GraficarDeriva(Grafico_Derivas As Chart)

        Grafico_Derivas.Series.Clear()

        Dim valores_X_Der_X As New List(Of Single)
        Dim valores_X_Der_Y As New List(Of Single)
        Dim valores_Y As New List(Of Single)

        For i = 0 To proyecto.ResultadosGlobales.DerivasX.Count - 1
            valores_X_Der_X.Add(proyecto.ResultadosGlobales.DerivasX(i).Deriva * 100)
            valores_X_Der_Y.Add(proyecto.ResultadosGlobales.DerivasX(i).Deriva * 100)
            valores_Y.Add(proyecto.ResultadosGlobales.DerivasX(i).Piso.CoorZ)
        Next
        valores_X_Der_X.Add(0)
        valores_X_Der_Y.Add(0)
        valores_Y.Add(0)

        Dim Serie_X As New Series("Deriva X")
        With Serie_X
            .Points.DataBindXY(valores_X_Der_X, valores_Y)
            .ChartType = SeriesChartType.Spline
            .Color = Color.Green
            .BorderWidth = 2
            .MarkerStyle = MarkerStyle.Circle
            .MarkerSize = 8
        End With

        Dim Serie_Y As New Series("Deriva Y")
        With Serie_Y
            .Points.DataBindXY(valores_X_Der_Y, valores_Y)
            .ChartType = SeriesChartType.Spline
            .Color = Color.OrangeRed
            .BorderWidth = 2
            .MarkerStyle = MarkerStyle.Diamond
            .MarkerSize = 8
        End With

        Grafico_Derivas.Series.Add(Serie_X)
        Grafico_Derivas.Series.Add(Serie_Y)

        Dim existeArea As Boolean = Grafico_Derivas.ChartAreas.IndexOf("ChartArea1") >= 0
        If existeArea Then
            Grafico_Derivas.ChartAreas.Remove(Grafico_Derivas.ChartAreas("ChartArea1"))
        End If

        Dim nuevaChartArea As New System.Windows.Forms.DataVisualization.Charting.ChartArea("ChartArea1")
        Grafico_Derivas.ChartAreas.Add(nuevaChartArea)

        Serie_X.ChartArea = "ChartArea1"
        Serie_Y.ChartArea = "ChartArea1"

        With Grafico_Derivas.ChartAreas(0)
            .AxisX.Title = "Deriva (%)"
            .AxisY.Title = "Altura del Edificio (m)"

            .AxisX.TitleFont = New Font("Arial", 11, FontStyle.Bold)
            .AxisY.TitleFont = New Font("Arial", 11, FontStyle.Bold)

            .AxisX.LabelStyle.Font = New Font("Arial", 10)
            .AxisY.LabelStyle.Font = New Font("Arial", 10)

            .AxisX.MajorGrid.LineColor = Color.LightGray
            .AxisY.MajorGrid.LineColor = Color.LightGray
            .AxisX.LabelStyle.Format = "0.00"
            .AxisX.Interval = 0.2
            .AxisX.Minimum = 0
            .AxisX.Maximum = 1.1
        End With

        Grafico_Derivas.Update()

    End Sub

    Public Shared Sub GraficarALRMuros(Grafico_ALR As Chart)

        Grafico_ALR.Series.Clear()

        Dim Serie_Grav As New Series("Gravitacional")
        With Serie_Grav
            .ChartType = SeriesChartType.StackedColumn
            .Color = Color.FromArgb(225, 236, 221)
            .BorderColor = Color.Black
            .BorderWidth = 1
        End With

        Dim Serie_Din As New Series("Envolvente")
        With Serie_Din
            .ChartType = SeriesChartType.StackedColumn
            .Color = Color.FromArgb(161, 197, 144)
            .BorderColor = Color.Black
            .BorderWidth = 1
        End With

        Dim Serie_LimMod As New Series("Límite ALR Moderado")
        With Serie_LimMod
            .ChartType = SeriesChartType.Spline
            .Color = Color.Gold
            .BorderDashStyle = ChartDashStyle.Dash
            .BorderWidth = 1
        End With

        Dim Serie_LimAlto As New Series("Límite ALR Alto")
        With Serie_LimAlto
            .ChartType = SeriesChartType.Spline
            .Color = Color.OrangeRed
            .BorderDashStyle = ChartDashStyle.Dash
            .BorderWidth = 1
        End With

        For i = 0 To proyecto.Elementos.Muros.Lista_Muros.Count - 1
            Serie_Grav.Points.AddXY(proyecto.Elementos.Muros.Lista_Muros(i).Name, proyecto.Elementos.Muros.Lista_Muros(i).ALR_G * 100)
            Serie_Din.Points.AddXY(proyecto.Elementos.Muros.Lista_Muros(i).Name, (proyecto.Elementos.Muros.Lista_Muros(i).ALR_D - proyecto.Elementos.Muros.Lista_Muros(i).ALR_G) * 100)
            Serie_LimMod.Points.AddXY(proyecto.Elementos.Muros.Lista_Muros(i).Name, 20)
            Serie_LimAlto.Points.AddXY(proyecto.Elementos.Muros.Lista_Muros(i).Name, 35)
        Next

        Grafico_ALR.Series.Add(Serie_Grav)
        Grafico_ALR.Series.Add(Serie_Din)
        Grafico_ALR.Series.Add(Serie_LimMod)
        Grafico_ALR.Series.Add(Serie_LimAlto)

        Dim existeArea As Boolean = Grafico_ALR.ChartAreas.IndexOf("ChartArea1") >= 0
        If existeArea Then
            Grafico_ALR.ChartAreas.Remove(Grafico_ALR.ChartAreas("ChartArea1"))
        End If

        Dim nuevaChartArea As New System.Windows.Forms.DataVisualization.Charting.ChartArea("ChartArea1")
        Grafico_ALR.ChartAreas.Add(nuevaChartArea)

        Serie_Grav.ChartArea = "ChartArea1"
        Serie_Din.ChartArea = "ChartArea1"
        Serie_LimMod.ChartArea = "ChartArea1"
        Serie_LimAlto.ChartArea = "ChartArea1"

        Grafico_ALR.Series(0)("PointWidth") = "0.6"
        Grafico_ALR.Series(1)("PointWidth") = "0.6"

        With Grafico_ALR.ChartAreas(0)
            .AxisX.Title = ""
            .AxisY.Title = ""

            .AxisX.TitleFont = New Font("Arial", 11, FontStyle.Bold)
            .AxisY.TitleFont = New Font("Arial", 11, FontStyle.Bold)

            .AxisX.LabelStyle.Font = New Font("Arial", 10)

            .AxisY.LabelStyle.Font = New Font("Arial", 10)

            .AxisX.MajorGrid.LineColor = Color.LightGray
            .AxisY.MajorGrid.LineColor = Color.LightGray
            .AxisY.LabelStyle.Format = "00.0"
            .AxisY.Interval = 5
            .AxisY.Minimum = 0
            .AxisY.Maximum = Math.Max(proyecto.Elementos.Muros.Lista_Muros.Max(Function(p) p.ALR_D) * 120, 41)

        End With

        Dim axis_x = Grafico_ALR.ChartAreas("ChartArea1").AxisX
        With axis_x
            .LabelStyle.Enabled = False
            .MajorTickMark.Enabled = False
        End With

        Dim myfontFamily As FontFamily = New FontFamily("Arial")

        Grafico_ALR.Legends.Clear()
        Dim Legend1 As New Legend("Legend1")
        Grafico_ALR.Legends.Add(Legend1)

        With Legend1
            .Font = New Font(myfontFamily, 8, FontStyle.Regular)
            .Docking = Docking.Bottom
            .Alignment = StringAlignment.Center
            .IsTextAutoFit = True
            .MaximumAutoSize = 100
        End With

        Dim ChartArea_1 = Grafico_ALR.ChartAreas("ChartArea1")
        With ChartArea_1
            .Position.Height = 95
            .Position.Width = 95
            .Position.X = 5
            .Position.Y = 2
            .BorderDashStyle = ChartDashStyle.Solid
            .BorderColor = Color.Black
            .BorderWidth = 1
            .InnerPlotPosition.Height = 85
            .InnerPlotPosition.Width = 95
            .InnerPlotPosition.X = 5
            .InnerPlotPosition.Y = 2
        End With

        Grafico_ALR.Update()

    End Sub

    Public Shared Sub GraficarTwAltura(Grafico As Chart)

        Grafico.Series.Clear()

        Dim list_Tw As List(Of Single) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.TipoMuro = eNumeradores.eTipoMuro.Protagonico).Select(Function(k) k.tw).Distinct().OrderByDescending(Function(t) t).ToList()

        Dim list_Series As New List(Of Series)

        For i = 0 To list_Tw.Count - 1

            Dim Serie_i As New Series("Tw = " & list_Tw(i).ToString("N2") & " m")
            With Serie_i
                .ChartType = SeriesChartType.StackedColumn
                .Color = Colores.ListaColores(i Mod ColoresProyecto.ListaColores.Count)
                .BorderColor = Color.Black
                .BackHatchStyle = ChartHatchStyle.DarkUpwardDiagonal
                .BorderWidth = 1
                .CustomProperties = "PointWidth=0.6"
            End With

            list_Series.Add(Serie_i)
        Next

        Dim list_Muros As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(m) m.TipoMuro = eNumeradores.eTipoMuro.Protagonico)

        For i = 0 To list_Tw.Count - 1
            For Each MuroProt In list_Muros
                Dim Tw As Double = list_Tw(i)
                Dim Cont_NPisos As Integer = MuroProt.Lista_Secciones.Where(Function(t) t.tw_Planos = Tw).Count()
                list_Series(i).Points.AddXY(MuroProt.Name, Cont_NPisos)
            Next
        Next

        Dim existeArea As Boolean = Grafico.ChartAreas.IndexOf("ChartArea1") >= 0
        If existeArea Then
            Grafico.ChartAreas.Remove(Grafico.ChartAreas("ChartArea1"))
        End If

        Dim nuevaChartArea As New System.Windows.Forms.DataVisualization.Charting.ChartArea("ChartArea1")
        Grafico.ChartAreas.Add(nuevaChartArea)

        For Each Serie In list_Series
            Grafico.Series.Add(Serie)
            Serie.ChartArea = "ChartArea1"
        Next

        proyecto.Info.NPisos = 20

        With Grafico.ChartAreas(0)
            .AxisX.Title = "Muros"
            .AxisX.TitleFont = New Font("Arial", 12, FontStyle.Bold)
            .AxisX.LabelStyle.Font = New Font("Arial", 9)
            .AxisX.MajorGrid.LineColor = Color.LightGray
            .AxisX.MajorGrid.Enabled = False
            .AxisX.Interval = 1

            .AxisY.Title = "Pisos"
            .AxisY.TitleFont = New Font("Arial", 12, FontStyle.Bold)
            .AxisY.LabelStyle.Font = New Font("Arial", 10)
            .AxisY.MajorGrid.LineColor = Color.LightGray
            .AxisY.Interval = 5
            .AxisY.Minimum = 0
            .AxisY.Maximum = proyecto.Info.NPisos

        End With

        Dim myfontFamily As FontFamily = New FontFamily("Arial")

        Grafico.Legends.Clear()
        Dim Legend1 As New Legend("Legend1")
        Grafico.Legends.Add(Legend1)
        Grafico.Legends(0).Font = New Font(myfontFamily, 11, FontStyle.Regular)
        Grafico.Legends(0).Docking = Docking.Bottom
        Grafico.Legends(0).Alignment = StringAlignment.Center

        Dim ChartArea_1 = Grafico.ChartAreas("ChartArea1")
        With ChartArea_1
            .Position.Height = 90
            .Position.Width = 95
            .Position.X = 4
            .Position.Y = 2
            .BorderDashStyle = ChartDashStyle.Solid
            .BorderColor = Color.Black
            .BorderWidth = 1
            .InnerPlotPosition.Height = 85
            .InnerPlotPosition.Width = 95
            .InnerPlotPosition.X = 5
            .InnerPlotPosition.Y = 5
        End With

        Grafico.Update()

    End Sub

End Class
