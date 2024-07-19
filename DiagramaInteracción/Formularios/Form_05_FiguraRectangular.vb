Public Class SRectangular
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'AddHandler PictureBox5.Paint, AddressOf Me.PictureBox5_Paint
        'PictureBox5.Refresh()

    End Sub
    Public Sub PictureBox5_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim PictureBox5 = PictureBox1
        Dim Columna = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Seccion.Text)
        Dim Tramo = Columna.Lista_Tramos_Columnas.Find(Function(p) p.Piso = Combo_Tramos.Text)
        Dim Estacion = Combo_Estacion.Text
        Dim B As Double = Tramo.B_Plano
        Dim H As Double = Tramo.H_Plano
        Dim R As Double = 0.05
        Dim Cuantia As Double = Math.Round(Tramo.Cuantia_Col_Top * 100, 2)
        Dim Detalles_Refuerzo = Tramo.Lista_Detalles_Refuerzo_Top
        If Estacion = "Bottom" Then
            Cuantia = Math.Round(Tramo.Cuantia_Col_Bottom * 100, 2)
            Detalles_Refuerzo = Tramo.Lista_Detalles_Refuerzo_Bottom
        End If
        LbCuantia.Text = Convert.ToString(Cuantia & " %")

        Dim cenY As Integer = Convert.ToInt16(Math.Round(PictureBox5.Height() / 2, 0))
        Dim cenX As Integer = Convert.ToInt16(Math.Round(PictureBox5.Width() / 2, 0))
        Dim Esc As Double
        If B > H Then
            Esc = (Math.Min(PictureBox5.Width, PictureBox5.Height) - 20) / B
        Else
            Esc = (Math.Min(PictureBox5.Width, PictureBox5.Height) - 20) / H
        End If
        Dim Esc1 As Double
        Dim Besc1 As Integer = B * Esc1
        Dim Hesc1 As Integer = H * Esc1
        Dim Resc1 As Integer = R * Esc1
        Dim Besc As Integer = B * Esc
        Dim Hesc As Integer = H * Esc
        Dim Resc As Integer = R * Esc
        Dim pen As New Pen(Color.FromArgb(74, 74, 74)) : pen.Width = 2
        Dim penG As New Pen(Color.Green) : penG.Width = 1
        Dim penBl As New Pen(Color.Blue) : penBl.Width = 1
        Dim penT As New Pen(Color.FromArgb(121, 121, 121)) : penT.Width = 3
        Dim coorxs As Integer = cenX - Besc / 2
        Dim coorys As Integer = cenY - Hesc / 2
        g.DrawRectangle(pen, coorxs, coorys, Besc, Hesc)
        Dim fillR As New SolidBrush(Color.FromArgb(210, 210, 210))
        g.FillRectangle(fillR, coorxs, coorys, Besc, Hesc)

        Try
            If Detalles_Refuerzo.Count > 1 Then
                Label12.Visible = True
                For i = 0 To Detalles_Refuerzo.Count - 1
                    Dim D As Double = Detalles_Refuerzo(i).Db / 1000
                    Dim Dbb As Integer = D * Esc
                    Dim Cxx As Integer = PictureBox5.Width() / 2 + Detalles_Refuerzo(i).Coordenada_X * Esc - Detalles_Refuerzo(i).Db / 2000 * Esc
                    Dim Cyy As Integer = PictureBox5.Height() / 2 - Detalles_Refuerzo(i).Coordenada_Y * Esc - Detalles_Refuerzo(i).Db / 2000 * Esc
                    Dim solidBruh As New SolidBrush(Color.FromArgb(121, 121, 121))
                    Dim penB As New Pen(Color.FromArgb(21, 21, 21))
                    penB.Width = 1
                    Dim letra As New Font("Arial", 9, FontStyle.Regular, GraphicsUnit.Pixel)
                    Dim cor As New SolidBrush(Color.FromArgb(0, 0, 0))
                    Dim corBl As New SolidBrush(Color.Blue)
                    Dim corG As New SolidBrush(Color.Green)
                    Dim CorN As New SolidBrush(Color.Black)
                    g.DrawLine(penBl, cenX, cenY, cenX + 25, cenY)
                    g.DrawLine(penBl, cenX + 25, cenY, cenX + 20, cenY - 5)
                    g.DrawLine(penBl, cenX + 25, cenY, cenX + 20, cenY + 5)
                    g.DrawLine(penG, cenX, cenY, cenX, cenY - 25)
                    g.DrawLine(penG, cenX, cenY - 25, cenX - 5, cenY - 20)
                    g.DrawLine(penG, cenX, cenY - 25, cenX + 5, cenY - 20)
                    g.DrawString("X (3)", letra, corBl, New PointF(cenX + 26, cenY - 5.5))
                    g.DrawString("Y (2)", letra, corG, New PointF(cenX - 5, cenY - 36))

                    g.FillEllipse(solidBruh, Cxx, Cyy, Dbb, Dbb)
                    g.DrawEllipse(penB, Cxx, Cyy, Dbb, Dbb)
                    g.DrawString(Convert.ToString(Detalles_Refuerzo(i).Name_Barra), letra, CorN, New PointF(Cxx + Dbb, Cyy - Dbb))

                Next
            End If
        Catch ex As Exception

        End Try
        PictureBox5.Update()
    End Sub

    '--------------------------- Función Diagrama de Interacción Sección Rectangular Sentido X -------------------------
    Public Function DiagramaInteraccionRectangularX()
        'Dim B As Double = Convert.ToDouble(SeccionR.Alto.Text)
        'Dim H As Double = Convert.ToDouble(SeccionR.Base.Text)
        'Dim R As Double = Convert.ToDouble(SeccionR.Recubrimiento.Text)
        'Dim NBarras As Double = Lista_Refuerzo(Lista_Refuerzo.Count() - 1).NTBarras
        'Dim Fc As Double = Convert.ToDouble(PagMateriales.Fc.Text)
        'Dim Fy As Double = Convert.ToDouble(PagMateriales.Fy.Text)
        'Dim Es As Double = Convert.ToDouble(PagMateriales.Es.Text)
        'Dim ecu As Double = Convert.ToDouble(PagMateriales.ecu.Text)
        'Dim esy As Double = Fy / Es
        'Dim Acero As Double
        'Dim Ag As Double = B * H
        'Dim es1 As Double
        'Dim Mst As Double
        'Dim Fst As Double
        'Dim c As Double
        'Dim di As Double
        'Dim esi As Double
        'Dim fsi As Double
        'Dim b1 As Double
        'Dim a As Double
        'Dim Acc As Double
        'Dim Mc As Double
        'Dim Fs As Double
        'Dim Phi As Double
        'Dim Cc As Double
        'Dim Mcc As Double
        'Dim P As Double
        'Dim M As Double
        'Dim AceroT As Double = Lista_Refuerzo(0).Ac
        'For i = 1 To Lista_Refuerzo.Count() - 1
        '    AceroT += Lista_Refuerzo(i).Ac
        'Next
        'Dim Pmax As Double = 0.85 * Fc * (Ag - AceroT) * 1000 + Fy * AceroT * 1000
        'Dim Pmin As Double = -Fy * AceroT * 1000
        Dim Resultados(1000, 5) As Double
        'Resultados(1, 1) = Pmax
        'Resultados(1, 2) = 0
        'Resultados(1, 3) = Pmax * 0.8 * 0.65
        'Resultados(1, 4) = 0
        'Dim MinY As Double = 0
        'Dim YBarras(100, 2) As Double
        'Dim k As Double = 1
        'For i = 1 To Lista_Refuerzo.Count()
        '    If Lista_Refuerzo(i - 1).CantBarras = 1 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    ElseIf Lista_Refuerzo(i - 1).CantBarras = 2 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX2
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    ElseIf Lista_Refuerzo(i - 1).CantBarras = 3 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX2
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX3
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    ElseIf Lista_Refuerzo(i - 1).CantBarras = 4 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX2
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX3
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX4
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    ElseIf Lista_Refuerzo(i - 1).CantBarras = 5 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX2
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX3
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX4
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX5
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    ElseIf Lista_Refuerzo(i - 1).CantBarras = 6 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX2
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX3
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX4
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX5
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX6
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    ElseIf Lista_Refuerzo(i - 1).CantBarras = 7 Then
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX1
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX2
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX3
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX4
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX5
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX6
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '        YBarras(k, 1) = (H / 2) - Lista_Refuerzo(i - 1).CoordX7
        '        YBarras(k, 2) = Lista_Refuerzo(i - 1).Ac / Lista_Refuerzo(i - 1).CantBarras
        '        k += 1
        '    End If
        'Next
        'For j = 1 To 100
        '    If MinY <= YBarras(j, 1) Then
        '        MinY = YBarras(j, 1)
        '    End If
        'Next
        'k = 2
        'Dim desy As Double = esy / 100
        'For c = H To 0.04 Step -(H - 0.04) / 10
        '    Mst = 0
        '    Fst = 0
        '    For i = 1 To (Lista_Refuerzo(Lista_Refuerzo.Count() - 1).NTBarras)
        '        Acero = YBarras(i, 2)
        '        di = YBarras(i, 1)
        '        esi = (c - di) * 0.003 / c
        '        fsi = Es * esi
        '        If fsi > Fy Then
        '            fsi = Fy
        '        ElseIf fsi < -Fy Then
        '            fsi = -Fy
        '        End If
        '        b1 = 0.85 - (0.05 * (Fc - 28) / 7)
        '        If b1 < 0.65 Then
        '            b1 = 0.65
        '        End If
        '        If b1 > 0.85 Then
        '            b1 = 0.85
        '        End If
        '        a = b1 * c
        '        If c > H Then
        '            a = 0.85 * H
        '        End If
        '        Acc = B * a
        '        Mc = Acc * (H / 2 - a / 2)
        '        If c < di Then
        '            Fs = fsi * Acero * 1000
        '        End If
        '        If c >= di Then
        '            Fs = (fsi - 0.85 * Fc) * Acero * 1000
        '        End If
        '        Fst = Fs + Fst
        '        Mst = Fs * (H / 2 - di) + Mst
        '    Next
        '    es1 = (MinY - c) * 0.003 / c
        '    If Math.Abs(es1) <= esy Then
        '        Phi = 0.65
        '    ElseIf Math.Abs(es1) > esy And Math.Abs(es1) <= (2.5 * esy) Then
        '        Phi = 0.65 + (Math.Abs(es1) - 0.002) * (250 / 3)
        '    Else
        '        Phi = 0.9
        '    End If
        '    Cc = Acc * Fc * 1000 * 0.85
        '    Mcc = Mc * Fc * 1000 * 0.85
        '    P = Cc + Fst
        '    M = Mcc + Mst
        '    Resultados(k, 1) = P
        '    Resultados(k, 2) = M
        '    Resultados(k, 3) = P * Phi
        '    Resultados(k, 4) = M * Phi
        '    If P * Phi > Pmax * 0.65 * 0.8 Then
        '        Resultados(k, 3) = Pmax * 0.65 * 0.8
        '    End If
        '    k += 1
        'Next
        'Resultados(k, 1) = Pmin
        'Resultados(k, 2) = 0
        'Resultados(k, 3) = Pmin * 0.9
        'Resultados(k, 4) = 0
        'Resultados(1, 5) = k
        DiagramaInteraccionRectangularX = Resultados
    End Function
    '--------------------------- Función Diagrama de Interacción Sección Rectangular Sentido Y -------------------------
    Public Function DiagramaInteraccionRectangular()
        '    'Dim B As Double = Convert.ToDouble(SeccionR.Base.Text)
        '    'Dim H As Double = Convert.ToDouble(SeccionR.Alto.Text)
        '    'Dim R As Double = Convert.ToDouble(SeccionR.Recubrimiento.Text)
        '    'Dim NBarras As Double = Lista_Refuerzo(Lista_Refuerzo.Count() - 1).NTBarras
        '    'Dim Fc As Double = Convert.ToDouble(PagMateriales.Fc.Text)
        '    'Dim Fy As Double = Convert.ToDouble(PagMateriales.Fy.Text)
        '    'Dim Es As Double = Convert.ToDouble(PagMateriales.Es.Text)
        '    'Dim ecu As Double = Convert.ToDouble(PagMateriales.ecu.Text)
        '    'Dim esy As Double = Fy / Es
        '    'Dim Acero As Double
        '    'Dim Cy As Double
        '    'Dim Ag As Double = B * H
        '    'Dim k As Double
        '    'Dim desy As Double
        '    'Dim es1 As Double
        '    'Dim Mst As Double
        '    'Dim Fst As Double
        '    'Dim c As Double
        '    'Dim di As Double
        '    'Dim esi As Double
        '    'Dim fsi As Double
        '    'Dim b1 As Double
        '    'Dim a As Double
        '    'Dim Acc As Double
        '    'Dim Mc As Double
        '    'Dim Fs As Double
        '    'Dim Phi As Double
        '    'Dim Cc As Double
        '    'Dim Mcc As Double
        '    'Dim P As Double
        '    'Dim M As Double
        '    'Dim Ac As Double = Lista_Refuerzo(Lista_Refuerzo.Count() - 1).AsT
        '    'Dim AceroT As Double = Ac
        '    'Dim Pmax As Double = 0.85 * Fc * (Ag - AceroT) * 1000 + Fy * AceroT * 1000
        '    'Dim Pmin As Double = -Fy * AceroT * 1000

        Dim Resultados(1000, 5) As Double
        '    'Resultados(1, 1) = Pmax
        '    'Resultados(1, 2) = 0
        '    'Resultados(1, 3) = Pmax * 0.8 * 0.65
        '    'Resultados(1, 4) = 0
        '    'Dim MinY As Double = 0

        '    'Dim YBarras(100) As Double
        '    'For i = 1 To (Lista_Refuerzo.Count())
        '    '    Cy = Lista_Refuerzo(i - 1).Coordy
        '    '    YBarras(i) = (H / 2) - Cy
        '    '    If MinY <= YBarras(i) Then
        '    '        MinY = YBarras(i)
        '    '    End If
        '    'Next

        '    'k = 2
        '    'desy = esy / 100
        '    'For c = H To 0.04 Step -(H - 0.04) / 10
        '    '    Mst = 0
        '    '    Fst = 0
        '    '    For i = 1 To (Lista_Refuerzo.Count())
        '    '        Acero = Lista_Refuerzo(i - 1).Ac
        '    '        di = YBarras(i)
        '    '        esi = (c - di) * 0.003 / c
        '    '        fsi = Es * esi
        '    '        If fsi > Fy Then
        '    '            fsi = Fy
        '    '        ElseIf fsi < -Fy Then
        '    '            fsi = -Fy
        '    '        End If
        '    '        b1 = 0.85 - (0.05 * (Fc - 28) / 7)
        '    '        If b1 < 0.65 Then
        '    '            b1 = 0.65
        '    '        End If
        '    '        If b1 > 0.85 Then
        '    '            b1 = 0.85
        '    '        End If
        '    '        a = b1 * c
        '    '        If c > H Then
        '    '            a = 0.85 * H
        '    '        End If
        '    '        Acc = B * a
        '    '        Mc = Acc * (H / 2 - a / 2)
        '    '        If c < di Then
        '    '            Fs = fsi * Acero * 1000
        '    '        End If
        '    '        If c >= di Then
        '    '            Fs = (fsi - 0.85 * Fc) * Acero * 1000
        '    '        End If
        '    '        Fst = Fs + Fst
        '    '        Mst = Fs * (H / 2 - di) + Mst
        '    '    Next
        '    '    es1 = (MinY - c) * 0.003 / c
        '    '    If Math.Abs(es1) <= esy Then
        '    '        Phi = 0.65
        '    '    ElseIf Math.Abs(es1) > esy And Math.Abs(es1) <= (2.5 * esy) Then
        '    '        Phi = 0.65 + (Math.Abs(es1) - 0.002) * (250 / 3)
        '    '    Else
        '    '        Phi = 0.9
        '    '    End If
        '    '    Cc = Acc * Fc * 1000 * 0.85
        '    '    Mcc = Mc * Fc * 1000 * 0.85
        '    '    P = Cc + Fst
        '    '    M = Mcc + Mst
        '    '    Resultados(k, 1) = P
        '    '    Resultados(k, 2) = M
        '    '    Resultados(k, 3) = P * Phi
        '    '    Resultados(k, 4) = M * Phi
        '    '    If P * Phi > Pmax * 0.65 * 0.8 Then
        '    '        Resultados(k, 3) = Pmax * 0.65 * 0.8
        '    '    End If
        '    '    k += 1
        '    'Next
        '    'Resultados(k, 1) = Pmin
        '    'Resultados(k, 2) = 0
        '    'Resultados(k, 3) = Pmin * 0.9
        '    'Resultados(k, 4) = 0
        '    'Resultados(1, 5) = k
        DiagramaInteraccionRectangular = Resultados
    End Function
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Reportes.Tabla_DI.Rows.Clear()
        Lista_Elementos.Clear()

        Dim Columna = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Seccion.Text)
        Dim Tramo = Columna.Lista_Tramos_Columnas.Find(Function(p) p.Piso = Combo_Tramos.Text)
        Dim Estacion = Combo_Estacion.Text
        Dim B As Double = Tramo.B_Plano
        Dim H As Double = Tramo.H_Plano
        Dim Detalles_Refuerzo = Tramo.Lista_Detalles_Refuerzo_Top
        If Estacion = "Bottom" Then
            Detalles_Refuerzo = Tramo.Lista_Detalles_Refuerzo_Bottom
        End If

        Diagrama_Interaccion_Rectangular.Diagrama_Interaccion(B, H, Detalles_Refuerzo, Tramo.fc, 420, 200000, 60)

        For i = 0 To Lista_Elementos.Count - 1
            Reportes.Tabla_DI.Rows.Add(Lista_Elementos(i).Pn, Lista_Elementos(i).Mn_X, Lista_Elementos(i).Mn_Y, Lista_Elementos(i).Phi_Pn, Lista_Elementos(i).Phi_Mn_X, Lista_Elementos(i).Phi_Mn_Y)
        Next


        Reportes.Show()
    End Sub

    Private Sub SRectangular_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        PictureBox1.Location = New Point(35, 66)
        PictureBox1.Size = New Size(Panel1.Width - 70, Panel1.Height - 100)
        Label6.Left = Panel1.Width / 2 - Label6.Width / 2
        Dibujar()
    End Sub

    Private Sub Combo_Tramos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Tramos.SelectedIndexChanged
        Dibujar()
    End Sub

    Private Sub Combo_Seccion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Seccion.SelectedIndexChanged
        Dibujar()
    End Sub

    Private Sub Combo_Estacion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Estacion.SelectedIndexChanged
        Dibujar()
    End Sub
    Public Sub Dibujar()
        If Combo_Seccion.Items.Count > 0 And Combo_Tramos.Items.Count > 0 And Combo_Estacion.Items.Count > 0 Then
            Dim Columna = Proyecto.Columnas.Lista_Columnas.Find(Function(p) p.Name_Label = Combo_Seccion.Text)
            Dim Tramo = Columna.Lista_Tramos_Columnas.Find(Function(p) p.Piso = Combo_Tramos.Text)
            Dim Estacion = Combo_Estacion.Text
            If Estacion = "Top" Then
                T_Cantidad_Corto.Text = Tramo.Cantidad_Lado_Corto_Top
                T_Cantidad_Largo.Text = Tramo.Cantidad_Lado_Largo_Top
                T_Acero.Text = Math.Round(Tramo.As_Col_Top / Tramo.Cantidad_Barras_Top, 0)
            Else
                T_Cantidad_Corto.Text = Tramo.Cantidad_Lado_Corto_Bottom
                T_Cantidad_Largo.Text = Tramo.Cantidad_Lado_Largo_Bottom
                T_Acero.Text = Math.Round(Tramo.As_Col_Bottom / Tramo.Cantidad_Barras_Bottom, 0)
            End If
        End If

        PictureBox1.Refresh()
        Dim PictureBox5 = PictureBox1
        AddHandler PictureBox5.Paint, AddressOf Me.PictureBox5_Paint
    End Sub
    Public Lista_Elementos As New List(Of DI)

    Public Class DI

        Public Pn As Single
        Public Mn_X As Single
        Public Mn_Y As Single
        Public Phi_Pn As Single
        Public Phi_Mn_X As Single
        Public Phi_Mn_Y As Single


    End Class

    Private Sub SRectangular_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class