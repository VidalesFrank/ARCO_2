Public Module Diagrama_Interaccion_Rectangular

    Public Sub Diagrama_Interaccion(ByVal B As Single, ByVal H As Single, ByVal Lista_Refuerzo As List(Of Tramo_Columna.Detalles_Refuerzo_Longitudinal),
                                         ByVal fc As Single, ByVal fy As Single, ByVal Es As Single, ByVal Angulo As Single)

        Dim Punto_1 As PointF : Punto_1.X = -B / 2 : Punto_1.Y = H / 2
        Dim Punto_2 As PointF : Punto_2.X = B / 2 : Punto_2.Y = H / 2
        Dim Punto_3 As PointF : Punto_3.X = B / 2 : Punto_3.Y = -H / 2
        Dim Punto_4 As PointF : Punto_4.X = -B / 2 : Punto_4.Y = -H / 2

        Dim Lista_Vertices As New List(Of PointF) : Lista_Vertices.Add(Punto_1) : Lista_Vertices.Add(Punto_2) : Lista_Vertices.Add(Punto_3) : Lista_Vertices.Add(Punto_4)
        Dim Lista_Vertices_Rotados As New List(Of PointF)
        Dim Y_max As Single : Dim Y_min As Single : Dim X_max As Single : Dim X_min As Single

        Dim Lista_Refuerzo_Rotados As New List(Of PointF)
        Dim d As Single '---- Minima coordenada Y
        Lista_Refuerzo_Rotados.Clear()
        For i = 0 To Lista_Refuerzo.Count - 1
            Dim Rotacion = Rotacion_Coordenadas(Lista_Refuerzo(i).Coordenada_X, Lista_Refuerzo(i).Coordenada_Y, Angulo * Math.PI / 180)
            Dim Punto As PointF
            Punto.X = Rotacion(0)
            Punto.Y = Rotacion(1)
            If Punto.Y < d Then
                d = Punto.Y
            End If
            Lista_Refuerzo_Rotados.Add(Punto)
        Next
        Lista_Vertices_Rotados.Clear()
        For i = 0 To 3
            Dim Rotacion = Rotacion_Coordenadas(Lista_Vertices(i).X, Lista_Vertices(i).Y, Angulo * Math.PI / 180)
            Dim Punto As PointF
            Punto.X = Rotacion(0)
            Punto.Y = Rotacion(1)
            If Punto.Y > Y_max Then
                Y_max = Punto.Y
            End If
            If Punto.Y < Y_min Then
                Y_min = Punto.Y
            End If
            If Punto.X > X_max Then
                X_max = Punto.X
            End If
            If Punto.X < X_min Then
                X_min = Punto.X
            End If
            Lista_Vertices_Rotados.Add(Punto)
        Next

        Dim Area = Area_Coordenadas(Lista_Vertices_Rotados)
        Dim X_g As Single = Area(2) / Area(0)
        Dim Y_g As Single = Area(1) / Area(0)
        '---- Cálculo del Centro Plastico ----------
        Dim Fuerza_Concreto As Single = 0.85 * fc * Area(0) * 1000
        Dim Fuerza_Refuerzo As Single = Lista_Refuerzo.Sum(Function(p) p.Asb) * (fy - 0.85 * fc) / 1000

        Dim M_X As Single : Dim M_Y As Single
        For i = 0 To Lista_Refuerzo.Count - 1
            Dim d_x As Single = Lista_Refuerzo(i).Coordenada_X - X_g
            Dim d_y As Single = Lista_Refuerzo(i).Coordenada_Y - Y_g

            M_X += (Lista_Refuerzo(i).Asb * (fy - 0.85 * fc)) * d_x
            M_Y += (Lista_Refuerzo(i).Asb * (fy - 0.85 * fc)) * d_y
        Next
        Dim C_X As Single = M_X / (Fuerza_Concreto + Fuerza_Refuerzo)
        Dim C_Y As Single = M_Y / (Fuerza_Concreto + Fuerza_Refuerzo)

        Dim Pmax As Single = Fuerza_Concreto + Fuerza_Refuerzo
        Dim Pmin As Single = -Fuerza_Refuerzo

        Dim Lista_Pn As New List(Of Single) : Lista_Pn.Add(Pmax)
        Dim Lista_Phi_Pn As New List(Of Single) : Lista_Phi_Pn.Add(Pmax * 0.65 * 0.8)
        Dim Lista_Mn_X As New List(Of Single) : Lista_Mn_X.Add(0)
        Dim Lista_Phi_Mn_X As New List(Of Single) : Lista_Phi_Mn_X.Add(0)
        Dim Lista_Mn_Y As New List(Of Single) : Lista_Mn_Y.Add(0)
        Dim Lista_Phi_Mn_Y As New List(Of Single) : Lista_Phi_Mn_Y.Add(0)

        Dim Elemento As New SRectangular.DI
        Elemento.Pn = Pmax
        Elemento.Mn_X = 0
        Elemento.Mn_Y = 0
        Elemento.Phi_Pn = Pmax * 0.65 * 0.8
        Elemento.Phi_Mn_X = 0
        Elemento.Phi_Mn_Y = 0
        SRectangular.Lista_Elementos.Add(Elemento)

        d = Y_max + Math.Abs(d)
        For C = (Y_max - Y_min) To 0.04 Step -(Math.Abs(Y_min) + Y_max - 0.04) / 10
            Dim Area_C = Area_Comprimida(Lista_Vertices_Rotados, C)
            Dim b1 As Single = 0.85 - (0.05 * (fc - 28) / 7)
            If b1 < 0.65 Then
                b1 = 0.65
            ElseIf b1 > 0.85 Then
                b1 = 0.85
            End If
            Dim F_Concreto As Single = b1 * fc * Area_C(0) * 1000
            Dim Mx_C As Single = F_Concreto * ((Area_C(2) / Area_C(0)) - C_X)
            Dim My_C As Single = F_Concreto * ((Area_C(1) / Area_C(0)) - C_Y)

            Dim Fst As Single = 0
            Dim Mst_X As Single = 0
            Dim Mst_Y As Single = 0
            For i = 0 To Lista_Refuerzo_Rotados.Count - 1
                Dim di As Single = (Y_max - Lista_Refuerzo_Rotados(i).Y)
                Dim esi As Single = (C - di) * 0.003 / C
                Dim fsi As Single = Es * esi
                If fsi > fy Then
                    fsi = fy
                ElseIf fsi < -fy Then
                    fsi = -fy
                End If
                Dim Fs As Single = fsi * Lista_Refuerzo(i).Asb / 1000
                If C >= di Then
                    Fs = (fsi - 0.85 * fc) * Lista_Refuerzo(i).Asb / 1000
                End If
                Fst += Fs
                Mst_X += Fs * (Lista_Refuerzo_Rotados(i).X - C_X)
                Mst_Y += Fs * (Lista_Refuerzo_Rotados(i).Y - C_Y)
            Next
            Dim es1 As Single = (d - C) * 0.003 / C
            Dim Phi As Single
            If Math.Abs(es1) <= (fy / Es) Then
                Phi = 0.65
            ElseIf Math.Abs(es1) > (fy / Es) And Math.Abs(es1) <= (2.5 * (fy / Es)) Then
                Phi = 0.65 + (Math.Abs(es1) - 0.002) * (250 / 3)
            Else
                Phi = 0.9
            End If

            Dim Pn As Single = Fst + F_Concreto
            Dim Mn_X As Single = Mx_C + Mst_X
            Dim Mn_Y As Single = My_C + Mst_Y
            Dim Phi_Pn As Single = Pn * Phi
            If Pn * Phi > Pmax * 0.65 * 0.8 Then
                Phi_Pn = Pmax * 0.65 * 0.8
            End If
            Dim Phi_Mn_X As Single = Mn_X * Phi
            Dim Phi_Mn_Y As Single = Mn_Y * Phi

            Lista_Pn.Add(Pn)
            Lista_Mn_X.Add(Mn_X)
            Lista_Mn_Y.Add(Mn_Y)

            Lista_Phi_Pn.Add(Phi_Pn)
            Lista_Phi_Mn_X.Add(Phi_Mn_X)
            Lista_Phi_Mn_Y.Add(Phi_Mn_Y)


            Dim Elemento_ As New SRectangular.DI
            Elemento_.Pn = Pn
            Elemento_.Mn_X = Mn_X
            Elemento_.Mn_Y = Mn_Y
            Elemento_.Phi_Pn = Phi_Pn
            Elemento_.Phi_Mn_X = Phi_Mn_X
            Elemento_.Phi_Mn_Y = Phi_Mn_Y
            SRectangular.Lista_Elementos.Add(Elemento_)

        Next
        Lista_Pn.Add(Pmin)
        Lista_Mn_X.Add(0)
        Lista_Mn_Y.Add(0)

        Lista_Phi_Pn.Add(0.9 * Pmin)
        Lista_Phi_Mn_X.Add(0)
        Lista_Phi_Mn_Y.Add(0)

        Dim Elementos As New SRectangular.DI
        Elementos.Pn = Pmin
        Elementos.Mn_X = 0
        Elementos.Mn_Y = 0
        Elementos.Phi_Pn = 0.9 * Pmin
        Elementos.Phi_Mn_X = 0
        Elementos.Phi_Mn_Y = 0
        SRectangular.Lista_Elementos.Add(Elementos)

    End Sub

    Public Function Area_Coordenadas(ByVal Lista_Coordenadas As List(Of PointF))
        Lista_Coordenadas.Add(Lista_Coordenadas(0))

        Dim Suma As Single
        Dim Mx As Single
        Dim My As Single
        For i = 0 To Lista_Coordenadas.Count - 2
            Suma += (Lista_Coordenadas(i + 1).X + Lista_Coordenadas(i).X) * (Lista_Coordenadas(i).Y - Lista_Coordenadas(i + 1).Y)
            Mx += (Lista_Coordenadas(i + 1).X - Lista_Coordenadas(i).X) * (Lista_Coordenadas(i + 1).Y ^ 2 + Lista_Coordenadas(i).Y ^ 2 + Lista_Coordenadas(i).Y * Lista_Coordenadas(i + 1).Y) / 6
            My += (Lista_Coordenadas(i + 1).Y - Lista_Coordenadas(i).Y) * (Lista_Coordenadas(i + 1).X ^ 2 + Lista_Coordenadas(i).X ^ 2 + Lista_Coordenadas(i).X * Lista_Coordenadas(i + 1).X) / 6
        Next
        Dim Resultados(2) : Resultados(0) = 0.5 * Suma : Resultados(1) = Mx : Resultados(2) = My

        Area_Coordenadas = Resultados
    End Function

    Public Function Rotacion_Coordenadas(ByVal Coordenada_X As Single, ByVal Coordenada_Y As Single, ByVal Angulo As Single)
        Dim Coordenadas(2)

        Coordenadas(0) = Coordenada_X * Math.Cos(Angulo) - Coordenada_Y * Math.Sin(Angulo)
        Coordenadas(1) = Coordenada_X * Math.Sin(Angulo) + Coordenada_Y * Math.Cos(Angulo)

        Rotacion_Coordenadas = Coordenadas

    End Function

    Public Function Area_Comprimida(ByVal Lista_Puntos As List(Of PointF), ByVal Eje_Neutro As Single)

        Dim Lista_P As New List(Of PointF)
        Lista_P.Clear()
        For i = 0 To Lista_Puntos.Count - 1
            Lista_P.Add(Lista_Puntos(i))
        Next

        Dim Lista As New List(Of PointF)
        Dim Y_max As Single = 0
        Dim Puntos_En_Compresion As Integer

        For i = 0 To Lista_P.Count - 1
            If Lista_P(i).Y > Y_max Then
                Y_max = Lista_P(i).Y
            End If
        Next
        For i = 0 To Lista_P.Count() - 2
            If Lista_P(i).Y > (Y_max - Eje_Neutro) Then
                Puntos_En_Compresion += 1
            End If
        Next

        For i = 0 To Lista_P.Count - 2
            Dim m As Single = (Lista_P(i).Y - Lista_P(i + 1).Y) / (Lista_P(i).X - Lista_P(i + 1).X)
            If m <> 0 Then
                If Lista_P(i).X + (Y_max - Eje_Neutro - Lista_P(i).Y) / m >= Math.Min(Lista_P(i).X, Lista_P(i + 1).X) And Lista_P(i).X + (Y_max - Eje_Neutro - Lista_P(i).Y) / m <= Math.Max(Lista_P(i).X, Lista_P(i + 1).X) Then
                    Dim Punto As New PointF
                    Punto.X = Lista_P(i).X + (Y_max - Eje_Neutro - Lista_P(i).Y) / m
                    Punto.Y = Y_max - Eje_Neutro
                    Lista.Add(Punto)
                    Lista_P.Add(Punto)
                End If
            End If
        Next

        Dim Lista_Puntos_A_Compresion As List(Of PointF) = Lista_P.FindAll(Function(P) P.Y >= Y_max - Eje_Neutro)
        Dim Quitar As List(Of PointF) = Quitar_Duplicados(Lista_Puntos_A_Compresion)
        Quitar.Add(Quitar(0))

        Dim Suma_Area As Single
        Dim Mx As Single
        Dim My As Single
        For i = 0 To Quitar.Count - 2
            Suma_Area += (Quitar(i).X + Quitar(i + 1).X) * (Quitar(i).Y - Quitar(i + 1).Y)
            Mx += (Quitar(i + 1).X - Quitar(i).X) * (Quitar(i + 1).Y ^ 2 + Quitar(i).Y ^ 2 + Quitar(i).Y * Quitar(i + 1).Y) / 6
            My += (Quitar(i + 1).Y - Quitar(i).Y) * (Quitar(i + 1).X ^ 2 + Quitar(i).X ^ 2 + Quitar(i).X * Quitar(i + 1).X) / 6
        Next
        Dim Resultados(2) : Resultados(0) = 0.5 * Suma_Area : Resultados(1) = Mx : Resultados(2) = My

        Area_Comprimida = Resultados

    End Function

    Public Function Quitar_Duplicados(ByVal Lista As List(Of PointF))
        Dim Lista_Pro As List(Of PointF) = Lista
        For j = 0 To Lista_Pro.Count - 1
            If j < Lista_Pro.Count Then
                Dim s = 0
                For i = 0 To Lista.Count - 1
                    If i < Lista.Count Then

                        If Lista_Pro(j).X = Lista(i).X And Lista_Pro(j).Y = Lista(i).Y Then
                            s += 1
                            If s > 1 Then
                                Lista.RemoveAt(i)
                            End If
                        End If
                    End If
                Next
            End If
        Next
        Quitar_Duplicados = Lista

    End Function



End Module
