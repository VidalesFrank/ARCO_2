Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Drawing.Drawing2D
Imports System.Drawing
Imports Excel = Microsoft.Office.Interop.Excel
Imports System.Data.OleDb
Public Class SeccionR
    Public PictureBox5 As New PictureBox()
    Class Eleccion
        Public Tipo As String
    End Class
    Public Opcion As New List(Of Eleccion)
    Function FactoresSobreEsfuerzoUltimas()
        Dim Diagrama = SRectangular.DiagramaInteraccionRectangularX()
        If RadioButton2.Checked = True Then
            Diagrama = SRectangular.DiagramaInteraccionRectangular()
        End If
        Dim F1 As Double
        Dim Mmax As Double
        Dim Factores(4, 1)
        Factores(1, 1) = 100
        Dim k As Double
        Dim ColComb As Integer
        Dim ColP As Integer
        Dim ColM2 As Integer
        Dim ColM3 As Integer
        Dim ColLab As Integer
        If Opcion(Opcion.Count() - 1).Tipo = "Frame" Then
            ColComb = 3
            ColP = 5
            ColM2 = 9
            ColM3 = 10
            ColLab = 1
        End If
        If Opcion(Opcion.Count() - 1).Tipo = "Pier" Then
            ColLab = 1
            ColComb = 2
            ColP = 4
            ColM2 = 8
            ColM3 = 9
        End If
        k = 0
        For D = 2 To (TablaCUltimas.Rows(0).Cells(15).Value + 1)
            For i = 1 To Diagrama(1, 5)
                If Math.Abs((Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColP).Value)) - Diagrama(i, 3))) <= 100 Then
                    If RadioButton1.Checked = True Then
                        Mmax = Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM2).Value))
                    ElseIf RadioButton2.Checked = True Then
                        Mmax = Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM3).Value))
                    End If
                    If Mmax <> 0 Then
                        F1 = Math.Abs(Diagrama(i, 4)) / Mmax
                        If Factores(1, 1) >= F1 And F1 > 0 Then
                            Factores(1, 1) = F1
                            Factores(2, 1) = TablaCUltimas.Rows(D).Cells(ColLab).Value.ToString
                            Factores(3, 1) = TablaCUltimas.Rows(D).Cells(ColComb).Value.ToString
                        End If
                    End If
                End If
            Next
            k += 1
        Next
        Factores(4, 1) = k
        FactoresSobreEsfuerzoUltimas = Factores
    End Function
    Function FactoresDiagonalUltimas()
        Dim Diagrama = SRectangular.DiagramaInteraccionRectangularX()
        If RadioButton2.Checked = True Then
            Diagrama = SRectangular.DiagramaInteraccionRectangular()
        End If
        Dim F2 As Double
        Dim P As Double
        Dim M2 As Double
        Dim M3 As Double
        Dim Mmax As Double
        Dim m_Sol As Double
        Dim PCap As Double
        Dim MCap As Double
        Dim PCap1 As Double
        Dim MCap1 As Double
        Dim m_Cap As Double
        Dim d_Sol As Double
        Dim ColComb As Integer
        Dim ColP As Integer
        Dim ColM2 As Integer
        Dim ColM3 As Integer
        Dim ColLab As Integer
        If Opcion(Opcion.Count() - 1).Tipo = "Frame" Or Opcion(Opcion.Count() - 1).Tipo = "Punto" Then
            ColComb = 3
            ColP = 5
            ColM2 = 9
            ColM3 = 10
            ColLab = 1
        End If
        If Opcion(Opcion.Count() - 1).Tipo = "Pier" Then
            ColLab = 1
            ColComb = 2
            ColP = 4
            ColM2 = 8
            ColM3 = 9
        End If
        Dim Factores(3, 1)
        Factores(1, 1) = 100
        Name = TablaCUltimas.Rows(0).Cells(ColComb).Value.ToString
        For D = 2 To (TablaCUltimas.Rows(0).Cells(15).Value + 1)
            P = -Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColP).Value)
            M2 = Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM2).Value)
            M3 = Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM3).Value)
            For t = 0 To 6
                If D + t < (TablaCUltimas.Rows(0).Cells(15).Value + 1) Then
                    If Name = TablaCUltimas.Rows(D + t).Cells(ColComb).Value.ToString And Math.Abs(P) > Math.Abs(-Convert.ToDouble(TablaCUltimas.Rows(D + t).Cells(ColP).Value)) Then
                        P = -Convert.ToDouble(TablaCUltimas.Rows(D + t).Cells(ColP).Value)
                    End If
                    If Name = TablaCUltimas.Rows(D + t).Cells(ColComb).Value.ToString And Math.Abs(M2) > Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM2).Value)) Then
                        M2 = Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM2).Value)
                    End If
                    If Name = TablaCUltimas.Rows(D + t).Cells(ColComb).Value.ToString And Math.Abs(M3) > Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM3).Value)) Then
                        M3 = Convert.ToDouble(TablaCUltimas.Rows(D).Cells(ColM3).Value)
                    End If
                End If
            Next
            If RadioButton1.Checked = True Then
                Mmax = Math.Abs(M2)
            ElseIf RadioButton2.Checked = True Then
                Mmax = Math.Abs(M3)
            End If
            If Mmax > 0 Then
                m_Sol = P / Mmax
                Dim cont As Double = 0
                For i = 1 To Diagrama(1, 5)
                    If Diagrama(i, 1) <> 0 Then
                        PCap = Diagrama(i, 3)
                        MCap = Diagrama(i, 4)
                        m_Cap = PCap / MCap
                        If m_Cap <= m_Sol And cont = 0 Then
                            cont = 1
                            PCap1 = Diagrama(i - 1, 3)
                            MCap1 = Diagrama(i - 1, 4)
                            Dim xn As Double = MCap1 - MCap
                            Dim yn As Double = PCap1 - PCap
                            Dim Yi As Double
                            Dim dx As Double
                            For x = 0 To xn Step xn / 100
                                Yi = PCap + m_Sol * x
                                Dim xi As Double = MCap + x
                                Dim mi As Double = Yi / xi
                                If Math.Abs(mi - m_Sol) < 3 Then
                                    dx = Math.Sqrt(Yi ^ 2 + xi ^ 2)
                                End If
                            Next
                            d_Sol = Math.Sqrt(P ^ 2 + Mmax ^ 2)
                            F2 = dx / d_Sol
                            If F2 < Factores(1, 1) And F2 > 0 Then
                                Factores(1, 1) = F2
                                Factores(2, 1) = TablaCUltimas.Rows(D).Cells(ColLab).Value.ToString
                                Factores(3, 1) = TablaCUltimas.Rows(D).Cells(ColComb).Value.ToString
                            End If
                        End If
                    End If
                Next
            End If
        Next
        FactoresDiagonalUltimas = Factores
    End Function
    '----------------------------- INICIAR ANÁLISIS -------------------------------------
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Cursor = Cursors.WaitCursor
        Grafica1.Series.Clear()
        ResumenDI.Rows.Clear()
        ResumenDI.Rows.Add()
        ResumenDI.Rows.Add()
        TablaDI.Rows.Clear()
        Dim Diagrama = SRectangular.DiagramaInteraccionRectangularX()
        If RadioButton2.Checked = True Then
            Diagrama = SRectangular.DiagramaInteraccionRectangular()
        End If
        Dim series As New Series
        Dim Serie2 As New Series
        Dim Demandas As New Series
        'Dim Pmax As Double
        'Dim Mmax As Double
        Dim M0max As Double
        Dim P0max As Double
        Grafica1.Series.Add(series)
        Grafica1.Series.Add(Serie2)
        Grafica1.Series.Add(Demandas)
        Dim ancho As Double = Panel2.Width
        Dim alto_ As Double = Panel2.Height
        Grafica1.Location = New Point(30, 200)
        Grafica1.Size = New Size(ancho - 30, alto_ - 200)
        Grafica1.ChartAreas(0).BackColor = Color.White
        series.ChartType = SeriesChartType.Spline
        Serie2.ChartType = SeriesChartType.Spline
        Demandas.ChartType = SeriesChartType.Point
        series.BorderWidth = 2
        Serie2.BorderWidth = 2
        Serie2.Color = Color.Gray
        series.Color = Color.Orange
        Demandas.Color = Color.Black
        Demandas.BorderWidth = 2
        For i = 1 To Diagrama(1, 5)
            If Diagrama(i, 1) <> 0 Then
                series.Points.AddXY(Diagrama(i, 4), Diagrama(i, 3))
                Serie2.Points.AddXY(Diagrama(i, 2), Diagrama(i, 1))
                TablaDI.Rows.Add(Math.Round(Diagrama(i, 1), 3), Math.Round(Diagrama(i, 2), 3), Math.Round(Diagrama(i, 3), 3), Math.Round(Diagrama(i, 4), 3))
                If Diagrama(i, 4) > M0max Then
                    M0max = Diagrama(i, 4)
                    P0max = Diagrama(i, 3)
                End If
            End If
        Next
        If RadioButton3.Checked = True Then
            Dim Diagrama1 = SRectangular.DiagramaInteraccionRectangularX()
            Dim Diagrama2 = SRectangular.DiagramaInteraccionRectangular()
            Grafica1.Series.Clear()
            Dim series1 As New Series
            Dim M2 As Double
            Dim M3 As Double
            Grafica1.Series.Add(series1)
            series1.ChartType = SeriesChartType.Line
            series1.BorderWidth = 2
            series1.Color = Color.Orange
            For i = 1 To Diagrama1(1, 5)
                If Diagrama1(i + 1, 3) * Diagrama1(i, 3) < 0 Then
                    M2 = Diagrama1(i, 4) - Diagrama1(i, 3) * ((Diagrama1(i, 4) - Diagrama1(i + 1, 4)) / (Diagrama1(i, 3) - Diagrama1(i + 1, 3)))
                End If
            Next
            For i = 1 To Diagrama2(1, 5)
                If Diagrama1(i + 1, 3) * Diagrama1(i, 3) < 0 Then
                    M3 = Diagrama2(i, 4) - Diagrama2(i, 3) * ((Diagrama2(i, 4) - Diagrama2(i + 1, 4)) / (Diagrama2(i, 3) - Diagrama2(i + 1, 3)))
                End If
            Next
            series1.Points.AddXY(M2, 0)
            series1.Points.AddXY(0, M3)
            series1.Points.AddXY(-M2, 0)
            series1.Points.AddXY(0, -M3)
            series1.Points.AddXY(M2, 0)
        End If
        ResumenDI.Rows(0).Cells(0).Value = Math.Round(Convert.ToDouble(Diagrama(1, 3)), 3)
        ResumenDI.Rows(0).Cells(1).Value = Math.Round(Convert.ToDouble(Diagrama(1, 4)), 3)
        ResumenDI.Rows(1).Cells(0).Value = Math.Round(P0max, 3)
        ResumenDI.Rows(1).Cells(1).Value = Math.Round(M0max, 3)
        ResumenDI.Rows(2).Cells(0).Value = Math.Round(Convert.ToDouble(Diagrama(Diagrama(1, 5), 3)), 3)
        ResumenDI.Rows(2).Cells(1).Value = Math.Round(Convert.ToDouble(Diagrama(Diagrama(1, 5), 4)), 3)
        ResumenDI.Visible = True
        ResumenDI.Location = New Point(Panel2.Width / 2 - ResumenDI.Width / 2, 50)
        'Try
        '    TablaRevision.Rows.Clear()
        '    TablaRevision.Rows.Add()
        '    Dim Fc As Double = Convert.ToDouble(PagMateriales.Fc.Text)
        '    Dim Fy As Double = Convert.ToDouble(PagMateriales.Fy.Text)
        '    Dim B As Double = Convert.ToDouble(Base.Text)
        '    Dim H As Double = Convert.ToDouble(Alto.Text)
        '    Dim R As Double = Convert.ToDouble(Recubrimiento.Text)
        '    Dim Ln As Double = Convert.ToDouble(LuzLibre.Text)
        '    Dim d As Double = H - R
        '    Dim Ag As Double = B * H
        '    Dim dimMin As Double = Math.Min(B, H)
        '    Dim dimMax As Double = Math.Max(B, H)
        '    '----- REFUERZO ------
        '    Dim sCon As Double = Convert.ToDouble(SeparaciónCon.Text)
        '    Dim sNoCon As Double = Convert.ToDouble(SeparaciónNoCon.Text)
        '    Dim AsTCon As Double = SeccionC.AreaRefuerzo(RefuerzoTransvCon.Text)
        '    Dim dbTCon As Double = SeccionC.DiametroRefuerzo(RefuerzoTransvCon.Text) * 1000
        '    Dim dbmin As Double = SRectangular.Lista_Refuerzo.Min(Function(x) x.Db)
        '    Dim AsTNoCon As Double = SeccionC.AreaRefuerzo(RefuerzoTransvNoCon.Text)
        '    Dim dbTNoCon As Double = SeccionC.DiametroRefuerzo(RefuerzoTransvNoCon.Text) * 1000
        '    Dim ColComb
        '    Dim ColP = 5
        '    Dim V2 = 6
        '    Dim V3 = 7
        '    Dim ColM2 = 9
        '    Dim ColM3 = 10
        '    If Opcion(Opcion.Count() - 1).Tipo = "Frame" Then
        '        ColComb = 3
        '        ColP = 5
        '        V2 = 6
        '        V3 = 7
        '        ColM2 = 9
        '        ColM3 = 10
        '    End If
        '    If Opcion(Opcion.Count() - 1).Tipo = "Pier" Then
        '        ColComb = 2
        '        ColP = 4
        '        V2 = 5
        '        V3 = 6
        '        ColM2 = 8
        '        ColM3 = 9
        '    End If
        '    If RadioButton3.Checked = False Then
        '        Grafica1.ChartAreas(0).AxisX.Minimum = 0
        '        Grafica1.ChartAreas(0).AxisX.Interval = 50
        '        TablaRevision.Columns(1).HeaderText = "φVc [kN]"
        '        TablaRevision.Columns(2).HeaderText = "φVs [kN]"
        '        TablaRevision.Columns(3).HeaderText = "φVn [kN]"
        '        TablaRevision.Columns(4).HeaderText = "Vu [kN]"
        '        TablaRevision.Columns(5).HeaderText = "φVn/Vu"
        '        TablaRevision.Columns(6).HeaderText = "Capacidad/Demanda (Cortes)"
        '        TablaRevision.Columns(7).HeaderText = "Capacidad/Demanda (Recta)"
        '        TablaRevision.Columns(8).HeaderText = "l0 [m]"
        '        TablaRevision.Columns(9).HeaderText = "s0 [m]"
        '        TablaRevision.Columns(10).HeaderText = "ρcol [%]"
        '        TablaRevision.Columns(11).HeaderText = "Ash [cm2]"
        '        TablaRevision.Columns(12).HeaderText = "Ash Colocado [cm2]"
        '        Dim Vc As Double = 0.17 * 0.75 * Math.Sqrt(Fc) * B * d * 1000
        '        Dim VsCon As Double
        '        Dim VsNoCon As Double
        '        Dim bc As Double = B - 2 * R
        '        Dim hc As Double = H - 2 * R
        '        Dim Ach As Double = bc * hc
        '        Dim Ash As Double
        '        Dim VmaxCon As Double = 0
        '        Dim VmaxNoCon As Double = 0
        '        Dim AshCol As Double
        '        Dim s_0 As Double
        '        Dim L_0 As Double
        '        If RadioButton1.Checked = True Then
        '            VsCon = 0.75 * AsTCon * SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).RamasX * Fy * d / (sCon * 1000)
        '            VsNoCon = 0.75 * AsTNoCon * SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).RamasX * Fy * d / (sNoCon * 1000)
        '            Dim Ash1 As Double
        '            Dim Ash2 As Double
        '            If CDiscipacion.Text = "DMO" Then
        '                Ash1 = 0.2 * sCon * bc * Fc * ((Ag / Ach) - 1) / Fy
        '                Ash2 = 0.06 * sCon * bc * Fc / Fy
        '                Ash = Math.Max(Ash1, Ash2)
        '                Dim p As Double = 0.08 * Fc / Fy
        '                Dim s01 As Double = 8 * dbmin
        '                Dim s02 As Double = 16 * dbTCon
        '                Dim s03 As Double = dimMin * 1000 / 3
        '                Dim s04 As Double = 150
        '                Dim L01 As Double = Ln / 6
        '                Dim L02 As Double = dimMax
        '                Dim L03 As Double = 0.5
        '                If s01 <= s02 And s01 <= s03 And s01 <= s04 Then
        '                    s_0 = s01
        '                ElseIf s02 <= s01 And s02 <= s03 And s02 <= s04 Then
        '                    s_0 = s02
        '                ElseIf s03 <= s01 And s03 <= s02 And s03 <= s04 Then
        '                    s_0 = s03
        '                ElseIf s04 <= s01 And s04 <= s02 And s04 <= s03 Then
        '                    s_0 = s04
        '                End If
        '                If L01 >= L02 And L01 >= L03 Then
        '                    L_0 = L01
        '                ElseIf L02 >= L01 And L02 >= L03 Then
        '                    L_0 = L02
        '                Else
        '                    L_0 = L03
        '                End If
        '            ElseIf CDiscipacion.Text = "DES" Then
        '                Ash1 = 0.3 * sCon * bc * Fc * ((Ag / Ach) - 1) / Fy
        '                Ash2 = 0.09 * sCon * bc * Fc / Fy
        '                Ash = Math.Max(Ash1, Ash2)
        '                Dim p As Double = 0.12 * Fc / Fy
        '                Dim s01 As Double = 6 * dbmin
        '                Dim s02 As Double = dimMin * 1000 / 4
        '                Dim L01 As Double = Ln / 6
        '                Dim L02 As Double = dimMax
        '                Dim L03 As Double = 0.45
        '                s_0 = Math.Min(s01, s02)
        '                If L01 >= L02 And L01 >= L03 Then
        '                    L_0 = L01
        '                ElseIf L02 >= L01 And L02 >= L03 Then
        '                    L_0 = L02
        '                Else
        '                    L_0 = L03
        '                End If
        '            End If
        '            For i = 2 To (TablaCCortante.Rows(0).Cells(15).Value + 1)
        '                If Math.Abs(VmaxCon) < Math.Abs(Convert.ToDouble(TablaCCortante.Rows(i).Cells(V2).Value)) Then
        '                    VmaxCon = Math.Abs(Convert.ToDouble(TablaCCortante.Rows(i).Cells(V2).Value))
        '                End If
        '            Next
        '            AshCol = SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).RamasX * AsTCon / 100
        '        Else
        '            VsCon = 0.75 * AsTCon * SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).RamasY * Fy * d / (sCon * 1000)
        '            VsNoCon = 0.75 * AsTNoCon * SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).RamasY * Fy * d / (sNoCon * 1000)
        '            If CDiscipacion.Text = "DMO" Then
        '                Dim Ash1 As Double = 0.2 * sCon * hc * Fc * ((Ag / Ach) - 1) / Fy
        '                Dim Ash2 As Double = 0.06 * sCon * hc * Fc / Fy
        '                Ash = Math.Max(Ash1, Ash2)
        '                Dim p As Double = 0.08 * Fc / Fy
        '                Dim s01 As Double = 8 * dbmin
        '                Dim s02 As Double = 16 * dbTCon
        '                Dim s03 As Double = dimMin * 1000 / 3
        '                Dim s04 As Double = 150
        '                Dim L01 As Double = Ln / 6
        '                Dim L02 As Double = dimMax
        '                Dim L03 As Double = 0.5
        '                If s01 <= s02 And s01 <= s03 And s01 <= s04 Then
        '                    s_0 = s01
        '                ElseIf s02 <= s01 And s02 <= s03 And s02 <= s04 Then
        '                    s_0 = s02
        '                ElseIf s03 <= s01 And s03 <= s02 And s03 <= s04 Then
        '                    s_0 = s03
        '                ElseIf s04 <= s01 And s04 <= s02 And s04 <= s03 Then
        '                    s_0 = s04
        '                End If
        '                If L01 >= L02 And L01 >= L03 Then
        '                    L_0 = L01
        '                ElseIf L02 >= L01 And L02 >= L03 Then
        '                    L_0 = L02
        '                Else
        '                    L_0 = L03
        '                End If
        '            ElseIf CDiscipacion.Text = "DES" Then
        '                Dim Ash1 As Double = 0.3 * sCon * hc * Fc * ((Ag / Ach) - 1) / Fy
        '                Dim Ash2 As Double = 0.09 * sCon * hc * Fc / Fy
        '                Ash = Math.Max(Ash1, Ash2)
        '                Dim p As Double = 0.12 * Fc / Fy
        '                Dim s01 As Double = 6 * dbmin
        '                Dim s02 As Double = dimMin * 1000 / 4
        '                Dim L01 As Double = Ln / 6
        '                Dim L02 As Double = dimMax
        '                Dim L03 As Double = 0.45
        '                s_0 = Math.Min(s01, s02)
        '                If L01 >= L02 And L01 >= L03 Then
        '                    L_0 = L01
        '                ElseIf L02 >= L01 And L02 >= L03 Then
        '                    L_0 = L02
        '                Else
        '                    L_0 = L03
        '                End If
        '            End If
        '            For i = 2 To (TablaCCortante.Rows(0).Cells(15).Value + 1)
        '                If Math.Abs(VmaxCon) < Math.Abs(Convert.ToDouble(TablaCCortante.Rows(i).Cells(V3).Value)) Then
        '                    VmaxCon = Math.Abs(Convert.ToDouble(TablaCCortante.Rows(i).Cells(V3).Value))
        '                End If
        '            Next
        '            AshCol = SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).RamasY * 0.71
        '        End If
        '        Dim VnCon As Double = Vc + VsCon
        '        Dim VnNoCon As Double = Vc + VsNoCon
        '        Dim VnCon_Vu As Double = VnCon / VmaxCon
        '        For f0 = 2 To (TablaCUltimas.Rows(0).Cells(15).Value + 1)
        '            If TablaCUltimas.Rows(f0).Cells(0).Value <> "" Then
        '                Pmax = -Convert.ToDouble(TablaCUltimas.Rows(f0).Cells(ColP).Value)
        '                If RadioButton1.Checked = True Then
        '                    Mmax = Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(f0).Cells(ColM2).Value))
        '                Else
        '                    Mmax = Math.Abs(Convert.ToDouble(TablaCUltimas.Rows(f0).Cells(ColM3).Value))
        '                End If
        '                Demandas.Points.AddXY(Mmax, Pmax)
        '            End If
        '        Next
        '        Dim pcol As Double = SRectangular.Lista_Refuerzo(SRectangular.Lista_Refuerzo.Count() - 1).AsT / (Convert.ToDouble(Base.Text) * Convert.ToDouble(Alto.Text))
        '        If pcol < 0.01 Then
        '            MsgBox("Tiene una cuantía menor a la mínima (1.0 %) especificada por la NSR-10")
        '        End If
        '        TablaRevision.Rows(0).Cells(0).Value = PagInfoGeneral.NameElement.Text()
        '        TablaRevision.Rows(0).Cells(1).Value = Math.Round(Vc, 3)
        '        TablaRevision.Rows(0).Cells(2).Value = Math.Round(VsCon, 3)
        '        TablaRevision.Rows(0).Cells(3).Value = Math.Round(VnCon, 3)
        '        TablaRevision.Rows(0).Cells(4).Value = Math.Round(VmaxCon, 3)
        '        TablaRevision.Rows(0).Cells(5).Value = Math.Round(VnCon_Vu, 2)
        '        TablaRevision.Rows(0).Cells(8).Value = Math.Round(L_0, 3)
        '        TablaRevision.Rows(0).Cells(9).Value = Math.Round(s_0 / 1000, 3)
        '        TablaRevision.Rows(0).Cells(10).Value = Math.Round(pcol * 100, 2)
        '        TablaRevision.Rows(0).Cells(11).Value = Math.Round(Ash * 10000, 2)
        '        TablaRevision.Rows(0).Cells(12).Value = Math.Round(AshCol, 2)
        '    End If
        '    Dim RevUltimoDiagonal = FactoresDiagonalUltimas()
        '    Dim RevUltimoCortes = FactoresSobreEsfuerzoUltimas()
        '    TablaRevision.Rows(0).Cells(6).Value = Math.Round(RevUltimoCortes(1, 1), 2)
        '    TablaRevision.Rows(0).Cells(7).Value = Math.Round(RevUltimoDiagonal(1, 1), 2)
        '    ExportarToolStripMenuItem.Enabled = True
        '    VerToolStripMenuItem.Enabled = True
        'Catch ex As Exception
        'End Try
        'Grafica1.Update()
        'Me.Cursor = Cursors.Arrow
    End Sub
    '----------------------------- EXPORTAR RESUMEN A EXCEL -------------------------------------
    Private Sub DatosAExcelToolStripMenuItem_Click_1(sender As Object, e As EventArgs) Handles DatosAExcelToolStripMenuItem.Click
        Me.Cursor = Cursors.WaitCursor
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDb.OleDbConnection(connection)
        Dim Archivo As String = "Revisión de Columna"
        'Exportar datos a Excel
        Try
            Dim c As Color
            Dim c1 As Color
            c = Color.FromArgb(200, 200, 200)
            c1 = Color.FromArgb(220, 220, 220)
            Dim appXL As Excel.Application
            Dim wbXL As Excel.Workbook
            Dim shXL As Excel.Worksheet
            Dim shXL2 As Excel.Worksheet
            Dim shXL3 As Excel.Worksheet
            Dim indice As Integer = 2
            Dim objExcelChart As Excel.Chart
            Dim objRange As Excel.Range
            Dim FactoresDiagonal = FactoresDiagonalUltimas()
            Dim FactoresCortes = FactoresSobreEsfuerzoUltimas()

            '---------------------------- Exportar Datos en la hoja 1 - (DIAGRAMA DE INTERACCIÓN) -----------------------------
            appXL = CreateObject("Excel.Application")
            appXL.Visible = False
            wbXL = appXL.Workbooks.Add()
            shXL = wbXL.Sheets("Hoja1")
            shXL.Name = "Diagrama de Interacción"
            shXL.Range("A1:D1").Font.Bold = True
            shXL.Range("A1:D1").Font.Size = 11
            shXL.Range("A2:D1000").Font.Size = 10
            shXL.Range("A1:D1000").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
            shXL.Range("A1:D1").Interior.Color = c
            shXL.Cells(1, 1) = "Mn [kN.m]"
            shXL.Cells(1, 2) = "Pn [kN]"
            shXL.Cells(1, 3) = "φMn [kN.m]"
            shXL.Cells(1, 4) = "φPn [kN]"
            For i = 1 To TablaDI.Rows.Count() - 1
                shXL.Cells(i + 1, 1) = TablaDI.Rows(i - 1).Cells(1).Value
                shXL.Cells(i + 1, 2) = TablaDI.Rows(i - 1).Cells(0).Value
                shXL.Cells(i + 1, 3) = TablaDI.Rows(i - 1).Cells(3).Value
                shXL.Cells(i + 1, 4) = TablaDI.Rows(i - 1).Cells(2).Value
            Next
            shXL.Range("F1:F4").Font.Bold = True
            shXL.Range("A1:H1000").Font.Name = "Arial"
            shXL.Range("F1:F4").Font.Size = 11
            shXL.Range("G1:H1").Font.Bold = True
            shXL.Range("G1:H1").Font.Size = 11
            shXL.Range("G2:H4").Font.Size = 11
            shXL.Range("F1:H4").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
            shXL.Range("F1:F4").Interior.Color = c
            shXL.Range("G1:H1").Interior.Color = c
            shXL.Range("G2:H2").BorderAround2(1, 2, 1)
            shXL.Range("G3:H3").BorderAround2(1, 2, 1)
            shXL.Range("G4:H4").BorderAround2(1, 2, 1)
            shXL.Range("G2:G4").BorderAround2(1, 2, 1)
            shXL.Range("H2:H4").BorderAround2(1, 2, 1)
            shXL.Cells(2, 6) = "Capacidad/Demanda"
            shXL.Cells(3, 6) = "Sección"
            shXL.Cells(4, 6) = "Combinación"
            shXL.Cells(1, 7) = "φMc/Mu"
            shXL.Cells(2, 7) = FactoresCortes(1, 1)
            shXL.Cells(3, 7) = FactoresCortes(2, 1)
            shXL.Cells(4, 7) = FactoresCortes(3, 1)
            shXL.Cells(1, 8) = "φdc/du"
            shXL.Cells(2, 8) = FactoresDiagonal(1, 1)
            shXL.Cells(3, 8) = FactoresDiagonal(2, 1)
            shXL.Cells(4, 8) = FactoresDiagonal(3, 1)
            shXL.Columns("F").ColumnWidth = 21
            shXL.Range("A:D").ColumnWidth = 15
            shXL.Range("G:H").ColumnWidth = 48

            shXL.PageSetup.Application.ActiveWindow.DisplayGridlines = False

            '--------------------------- Graficar Diagrama de Interacción ----------------------------
            Dim xlCharts As Excel.ChartObjects
            Dim MyChart As Excel.ChartObject
            xlCharts = shXL.ChartObjects
            MyChart = xlCharts.Add(390, 75, 595.14, 340.08)
            objExcelChart = MyChart.Chart
            objExcelChart.ChartType = Excel.XlChartType.xlXYScatterSmoothNoMarkers
            objRange = shXL.Range("C1:D600")
            objExcelChart.SetSourceData(objRange)

            '---------------------------- Exportar Datos en la hoja 2 - (RESUMEN PROYECTO) -----------------------------
            shXL2 = wbXL.Sheets.Add()
            shXL2 = wbXL.Sheets("Hoja2")
            shXL2.Name = "Resumen de Análisis"
            shXL2.Range("A1:M1").Merge(True)
            shXL2.Cells(1, 1) = "REVISIÓN CAPACIDAD DEL ELEMENTO"
            shXL2.Range("A1:M2").Font.Bold = True
            shXL2.Range("A1:M2").Interior.Color = c
            shXL2.Range("A1:M100").Font.Name = "Arial"
            shXL2.Range("A1:M1").Font.Size = 11
            shXL2.Range("A2:M100").Font.Size = 10
            shXL2.Range("A1:M100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
            shXL2.Cells(1, 1) = "Elemento"
            shXL2.Cells(1, 2) = "φVc [kN]"
            shXL2.Cells(1, 3) = "φVs [kN]"
            shXL2.Cells(1, 4) = "φVn [kN]"
            shXL2.Cells(1, 5) = "Vu [kN]"
            shXL2.Cells(1, 6) = "φVn/Vu"
            shXL2.Cells(1, 7) = "Capacidad/Demanda (Cortes)"
            shXL2.Cells(1, 8) = "Capacidad/Demanda (Recta Diagonal)"
            shXL2.Cells(1, 9) = "l0 [m]"
            shXL2.Cells(1, 10) = "s0 [m]"
            shXL2.Cells(1, 11) = "ρ Col [%]"
            shXL2.Cells(1, 12) = "Ash [cm2]"
            shXL2.Cells(1, 13) = "Ash Colocado [cm2]"
            shXL2.Range("A:M").ColumnWidth = 15
            shXL2.Range("G:H").ColumnWidth = 27
            shXL2.Range("I:M").ColumnWidth = 15
            For j = 0 To TablaRevision.Rows.Count() - 2
                For i = 0 To 12
                    shXL2.Cells(2, i + 1) = TablaRevision.Columns(i).HeaderText
                    shXL2.Cells(j + 3, i + 1) = TablaRevision.Rows(j).Cells(i).Value
                Next
            Next
            shXL2.PageSetup.Application.ActiveWindow.DisplayGridlines = False

            '--------------------------- Imprimir resumen de Datos ----------------------------
            shXL3 = wbXL.Sheets.Add()
            shXL3 = wbXL.Sheets("Hoja3")
            shXL3.Name = "Resumen de Datos"
            shXL3.Range("A1:C1").Merge(True)
            shXL3.Range("A3:C3").Merge(True)
            shXL3.Range("A8:C8").Merge(True)
            shXL3.Range("A1").Font.Bold = True
            shXL3.Range("A3").Font.Bold = True
            shXL3.Range("A8").Font.Bold = True
            shXL3.Range("A1").Interior.Color = c
            shXL3.Range("A3").Interior.Color = c1
            shXL3.Range("A8").Interior.Color = c1
            shXL3.Range("A1:D100").Font.Name = "Arial"
            shXL3.Range("A1:D100").Font.Size = 11
            shXL3.Range("A1:D100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
            shXL3.Range("A1:C1").BorderAround2(1, 3, 1)
            shXL3.Range("A3:C3").BorderAround2(1, 3, 1)
            shXL3.Range("A3:C6").BorderAround2(1, 3, 1)
            shXL3.Range("A8:C8").BorderAround2(1, 3, 1)
            shXL3.Range("A8:C10").BorderAround2(1, 3, 1)
            shXL3.Range("A4:C4").BorderAround2(1, 2, 1)
            shXL3.Range("A5:C5").BorderAround2(1, 2, 1)
            shXL3.Range("A6:C6").BorderAround2(1, 2, 1)
            shXL3.Range("A4:A6").BorderAround2(1, 2, 1)
            shXL3.Range("B4:B6").BorderAround2(1, 2, 1)
            shXL3.Range("C4:C6").BorderAround2(1, 2, 1)
            shXL3.Range("A9:C9").BorderAround2(1, 2, 1)
            shXL3.Range("A10:C10").BorderAround2(1, 2, 1)
            shXL3.Range("A9:A10").BorderAround2(1, 2, 1)
            shXL3.Range("B9:B10").BorderAround2(1, 2, 1)
            shXL3.Range("C9:C10").BorderAround2(1, 2, 1)

            shXL3.Cells(1, 1) = "Análisis de Sección Rectangular"
            shXL3.Cells(3, 1) = "Geometria"
            shXL3.Cells(4, 1) = "Ancho"
            shXL3.Cells(4, 2) = Base.Text
            shXL3.Cells(4, 3) = "m"
            shXL3.Cells(5, 1) = "Alto"
            shXL3.Cells(5, 2) = Alto.Text
            shXL3.Cells(5, 3) = "m"
            shXL3.Cells(6, 1) = "Recubrimiento"
            shXL3.Cells(6, 2) = Recubrimiento.Text
            shXL3.Cells(6, 3) = "m"
            shXL3.Range("A:C").ColumnWidth = 15
            shXL3.PageSetup.Application.ActiveWindow.DisplayGridlines = False

            shXL3.Cells(8, 1) = "Propiedades de los Materiales"
            shXL3.Cells(9, 1) = "F'c"
            shXL3.Cells(9, 2) = PagMateriales.Fc.Text
            shXL3.Cells(9, 3) = "MPa"
            shXL3.Cells(10, 1) = "Fy"
            shXL3.Cells(10, 2) = PagMateriales.Fy.Text
            shXL3.Cells(10, 3) = "MPa"

            Dim saveFileDialog1 As New SaveFileDialog()
            saveFileDialog1.Title = "Guardar documento Excel"
            saveFileDialog1.Filter = "Excel File|*.xlsx"
            saveFileDialog1.FileName = Convert.ToString(Archivo & " " & PagInfoGeneral.NameElement.Text & "_" & PagInfoGeneral.NameProject.Text)
            saveFileDialog1.ShowDialog()
            wbXL.SaveAs(saveFileDialog1.FileName)
            appXL.Workbooks.Close()
            appXL.Quit()
            System.Diagnostics.Process.Start(saveFileDialog1.FileName)
        Catch ex As Exception
            MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            conexion.Close()
            Me.Cursor = Cursors.Arrow
        End Try
    End Sub

    Public Function ImportExcellToDataGridView_CCortante(ByRef path As String, ByVal Datagrid As DataGridView)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            Dim Cmd As New OleDbCommand("Select * From [CargasCortante$]")
            cnConex.Open()
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function
    Public Function ImportExcellToDataGridView_CUltimas(ByRef path As String, ByVal Datagrid As DataGridView)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            Dim Cmd As New OleDbCommand("Select * From [CargasUltimas$]")
            cnConex.Open()
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function
    Private Sub TipoFrameToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TipoFrameToolStripMenuItem.Click
        Dim Desicion As New Eleccion
        Desicion.Tipo = "Frame"
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            .InitialDirectory = My.Computer.FileSystem.SpecialDirectories.Desktop
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                ImportExcellToDataGridView_CCortante(.FileName, TablaCCortante)
                ImportExcellToDataGridView_CUltimas(.FileName, TablaCUltimas)
            End If
        End With
        Opcion.Add(Desicion)
    End Sub
    Private Sub TipoPierToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TipoPierToolStripMenuItem.Click
        Dim Desicion As New Eleccion
        Desicion.Tipo = "Pier"
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            .InitialDirectory = My.Computer.FileSystem.SpecialDirectories.Desktop
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                ImportExcellToDataGridView_CCortante(.FileName, TablaCCortante)
                ImportExcellToDataGridView_CUltimas(.FileName, TablaCUltimas)
            End If
        End With
        Opcion.Add(Desicion)
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        PictureBox5.Location = New Point(25, 70)
        PictureBox5.Size = New Size(SRectangular.Panel1.Width - 50, SRectangular.Panel1.Height - 100)
        PictureBox5.BackColor = Color.White
        PictureBox5.Anchor = AnchorStyles.Left And AnchorStyles.Top And AnchorStyles.Right And AnchorStyles.Bottom
        SRectangular.Panel1.Controls.Add(PictureBox5)
        AddHandler PictureBox5.Paint, AddressOf SRectangular.PictureBox5_Paint
        PictureBox5.Refresh()
        SRectangular.Show()
    End Sub
End Class