Public Module Diagrama_Interaccion_Rectangular

    'Public Sub Diagrama_Interaccion(ByVal B As Single, ByVal H As Single, ByVal Lista_Refuerzo As List(Of Tramo_Columna.Detalles_Refuerzo_Longitudinal),
    '                                     ByVal fc As Single, ByVal fy As Single, ByVal Es As Single, ByVal Angulo As Single)

    '    Dim Punto_1 As PointF : Punto_1.X = -B / 2 : Punto_1.Y = H / 2
    '    Dim Punto_2 As PointF : Punto_2.X = B / 2 : Punto_2.Y = H / 2
    '    Dim Punto_3 As PointF : Punto_3.X = B / 2 : Punto_3.Y = -H / 2
    '    Dim Punto_4 As PointF : Punto_4.X = -B / 2 : Punto_4.Y = -H / 2

    '    Dim Lista_Vertices As New List(Of PointF) : Lista_Vertices.Add(Punto_1) : Lista_Vertices.Add(Punto_2) : Lista_Vertices.Add(Punto_3) : Lista_Vertices.Add(Punto_4)
    '    Dim Lista_Vertices_Rotados As New List(Of PointF)
    '    Dim Y_max As Single : Dim Y_min As Single : Dim X_max As Single : Dim X_min As Single

    '    Dim Lista_Refuerzo_Rotados As New List(Of PointF)

    '    Dim d As Single '---- Minima coordenada Y

    '    Lista_Refuerzo_Rotados.Clear()

    '    For i = 0 To Lista_Refuerzo.Count - 1
    '        Dim Rotacion = Rotacion_Coordenadas(Lista_Refuerzo(i).Coordenada_X, Lista_Refuerzo(i).Coordenada_Y, Angulo * Math.PI / 180)
    '        Dim Punto As PointF
    '        Punto.X = Rotacion(0)
    '        Punto.Y = Rotacion(1)
    '        If Punto.Y < d Then
    '            d = Punto.Y
    '        End If
    '        Lista_Refuerzo_Rotados.Add(Punto)
    '    Next

    '    Lista_Vertices_Rotados.Clear()

    '    For i = 0 To 3
    '        Dim Rotacion = Rotacion_Coordenadas(Lista_Vertices(i).X, Lista_Vertices(i).Y, Angulo * Math.PI / 180)
    '        Dim Punto As PointF
    '        Punto.X = Rotacion(0)
    '        Punto.Y = Rotacion(1)
    '        If Punto.Y > Y_max Then
    '            Y_max = Punto.Y
    '        End If
    '        If Punto.Y < Y_min Then
    '            Y_min = Punto.Y
    '        End If
    '        If Punto.X > X_max Then
    '            X_max = Punto.X
    '        End If
    '        If Punto.X < X_min Then
    '            X_min = Punto.X
    '        End If
    '        Lista_Vertices_Rotados.Add(Punto)
    '    Next

    '    Dim Area = Area_Coordenadas(Lista_Vertices_Rotados)
    '    Dim X_g As Single = Area(2) / Area(0)
    '    Dim Y_g As Single = Area(1) / Area(0)
    '    '---- Cálculo del Centro Plastico ----------
    '    Dim Fuerza_Concreto As Single = 0.85 * fc * Area(0) * 1000
    '    Dim Fuerza_Refuerzo As Single = Lista_Refuerzo.Sum(Function(p) p.Asb) * (fy - 0.85 * fc) / 1000

    '    Dim M_X As Single : Dim M_Y As Single

    '    For i = 0 To Lista_Refuerzo.Count - 1
    '        Dim d_x As Single = Lista_Refuerzo(i).Coordenada_X - X_g
    '        Dim d_y As Single = Lista_Refuerzo(i).Coordenada_Y - Y_g

    '        M_X += (Lista_Refuerzo(i).Asb * (fy - 0.85 * fc)) * d_x
    '        M_Y += (Lista_Refuerzo(i).Asb * (fy - 0.85 * fc)) * d_y
    '    Next
    '    Dim C_X As Single = M_X / (Fuerza_Concreto + Fuerza_Refuerzo)
    '    Dim C_Y As Single = M_Y / (Fuerza_Concreto + Fuerza_Refuerzo)

    '    Dim Pmax As Single = Fuerza_Concreto + Fuerza_Refuerzo
    '    Dim Pmin As Single = -Fuerza_Refuerzo

    '    Dim Lista_Pn As New List(Of Single) : Lista_Pn.Add(Pmax)
    '    Dim Lista_Phi_Pn As New List(Of Single) : Lista_Phi_Pn.Add(Pmax * 0.65 * 0.8)
    '    Dim Lista_Mn_X As New List(Of Single) : Lista_Mn_X.Add(0)
    '    Dim Lista_Phi_Mn_X As New List(Of Single) : Lista_Phi_Mn_X.Add(0)
    '    Dim Lista_Mn_Y As New List(Of Single) : Lista_Mn_Y.Add(0)
    '    Dim Lista_Phi_Mn_Y As New List(Of Single) : Lista_Phi_Mn_Y.Add(0)

    '    Dim Elemento As New SRectangular.DI
    '    Elemento.Pn = Pmax
    '    Elemento.Mn_X = 0
    '    Elemento.Mn_Y = 0
    '    Elemento.Phi_Pn = Pmax * 0.65 * 0.8
    '    Elemento.Phi_Mn_X = 0
    '    Elemento.Phi_Mn_Y = 0
    '    SRectangular.Lista_Elementos.Add(Elemento)

    '    d = Y_max + Math.Abs(d)
    '    For C = (Y_max - Y_min) To 0.04 Step -(Math.Abs(Y_min) + Y_max - 0.04) / 10
    '        Dim Area_C = Area_Comprimida(Lista_Vertices_Rotados, C)
    '        Dim b1 As Single = 0.85 - (0.05 * (fc - 28) / 7)
    '        If b1 < 0.65 Then
    '            b1 = 0.65
    '        ElseIf b1 > 0.85 Then
    '            b1 = 0.85
    '        End If
    '        Dim F_Concreto As Single = b1 * fc * Area_C(0) * 1000
    '        Dim Mx_C As Single = F_Concreto * ((Area_C(2) / Area_C(0)) - C_X)
    '        Dim My_C As Single = F_Concreto * ((Area_C(1) / Area_C(0)) - C_Y)

    '        Dim Fst As Single = 0
    '        Dim Mst_X As Single = 0
    '        Dim Mst_Y As Single = 0
    '        For i = 0 To Lista_Refuerzo_Rotados.Count - 1
    '            Dim di As Single = (Y_max - Lista_Refuerzo_Rotados(i).Y)
    '            Dim esi As Single = (C - di) * 0.003 / C
    '            Dim fsi As Single = Es * esi
    '            If fsi > fy Then
    '                fsi = fy
    '            ElseIf fsi < -fy Then
    '                fsi = -fy
    '            End If
    '            Dim Fs As Single = fsi * Lista_Refuerzo(i).Asb / 1000
    '            If C >= di Then
    '                Fs = (fsi - 0.85 * fc) * Lista_Refuerzo(i).Asb / 1000
    '            End If
    '            Fst += Fs
    '            Mst_X += Fs * (Lista_Refuerzo_Rotados(i).X - C_X)
    '            Mst_Y += Fs * (Lista_Refuerzo_Rotados(i).Y - C_Y)
    '        Next
    '        Dim es1 As Single = (d - C) * 0.003 / C
    '        Dim Phi As Single
    '        If Math.Abs(es1) <= (fy / Es) Then
    '            Phi = 0.65
    '        ElseIf Math.Abs(es1) > (fy / Es) And Math.Abs(es1) <= (2.5 * (fy / Es)) Then
    '            Phi = 0.65 + (Math.Abs(es1) - 0.002) * (250 / 3)
    '        Else
    '            Phi = 0.9
    '        End If

    '        Dim Pn As Single = Fst + F_Concreto
    '        Dim Mn_X As Single = Mx_C + Mst_X
    '        Dim Mn_Y As Single = My_C + Mst_Y
    '        Dim Phi_Pn As Single = Pn * Phi
    '        If Pn * Phi > Pmax * 0.65 * 0.8 Then
    '            Phi_Pn = Pmax * 0.65 * 0.8
    '        End If
    '        Dim Phi_Mn_X As Single = Mn_X * Phi
    '        Dim Phi_Mn_Y As Single = Mn_Y * Phi

    '        Lista_Pn.Add(Pn)
    '        Lista_Mn_X.Add(Mn_X)
    '        Lista_Mn_Y.Add(Mn_Y)

    '        Lista_Phi_Pn.Add(Phi_Pn)
    '        Lista_Phi_Mn_X.Add(Phi_Mn_X)
    '        Lista_Phi_Mn_Y.Add(Phi_Mn_Y)


    '        Dim Elemento_ As New SRectangular.DI
    '        Elemento_.Pn = Pn
    '        Elemento_.Mn_X = Mn_X
    '        Elemento_.Mn_Y = Mn_Y
    '        Elemento_.Phi_Pn = Phi_Pn
    '        Elemento_.Phi_Mn_X = Phi_Mn_X
    '        Elemento_.Phi_Mn_Y = Phi_Mn_Y
    '        SRectangular.Lista_Elementos.Add(Elemento_)

    '    Next
    '    Lista_Pn.Add(Pmin)
    '    Lista_Mn_X.Add(0)
    '    Lista_Mn_Y.Add(0)

    '    Lista_Phi_Pn.Add(0.9 * Pmin)
    '    Lista_Phi_Mn_X.Add(0)
    '    Lista_Phi_Mn_Y.Add(0)

    '    Dim Elementos As New SRectangular.DI
    '    Elementos.Pn = Pmin
    '    Elementos.Mn_X = 0
    '    Elementos.Mn_Y = 0
    '    Elementos.Phi_Pn = 0.9 * Pmin
    '    Elementos.Phi_Mn_X = 0
    '    Elementos.Phi_Mn_Y = 0
    '    SRectangular.Lista_Elementos.Add(Elementos)

    'End Sub

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


    ' Diagrama de interacción P-M para sección rectangular según NSR-10 / ACI 318.
    ' Usa el bloque rectangular de Whitney directamente sobre la sección rectangular
    ' sin depender de Area_Comprimida (que tiene limitaciones para polígonos).
    ' Retorna: (φMn, φPn, Mn, Pn) — mismo orden que el resto del código.
    Public Function Funcion_Diagrama_Interaccion(ByVal B As Single, ByVal H As Single,
                                                 ByVal Lista_Refuerzo As List(Of RefuerzoSimple),
                                                 ByVal fc As Single, ByVal fy As Single,
                                                 ByVal Es As Single, ByVal Angulo As Single) _
        As (List(Of Single), List(Of Single), List(Of Single), List(Of Single))

        If Lista_Refuerzo Is Nothing OrElse Lista_Refuerzo.Count = 0 Then
            Return (New List(Of Single), New List(Of Single), New List(Of Single), New List(Of Single))
        End If

        ' ── β₁ (NSR-10 C.10.2.7.3) ────────────────────────────────────────
        Dim beta1 As Single = CSng(Math.Max(0.65, Math.Min(0.85, 0.85 - 0.05 * (fc - 28) / 7)))

        ' ── Rotar barras (para Angulo ≠ 0) ───────────────────────────────
        Dim angRad As Double = Angulo * Math.PI / 180.0
        Dim cosA As Single = CSng(Math.Cos(angRad))
        Dim sinA As Single = CSng(Math.Sin(angRad))

        Dim barras As New List(Of (Yr As Single, Asb As Single))
        Dim Y_barra_min As Single = Single.MaxValue

        For Each ref As RefuerzoSimple In Lista_Refuerzo
            Dim yr As Single = CSng(ref.Coordenada_X * sinA + ref.Coordenada_Y * cosA)
            barras.Add((yr, ref.Asb))
            If yr < Y_barra_min Then Y_barra_min = yr
        Next

        ' ── Geometría (fibra comprimida = Y_top = H/2 para Angulo=0) ─────
        Dim Y_top As Single = H / 2.0F
        Dim d_util As Single = Y_top - Y_barra_min   ' profundidad total (fibra → barra más traccionada)

        ' ── Pmax: compresión pura NSR-10 C.10.3.5 ────────────────────────
        '    P₀ = 0.85·f'c·(Ag - ΣAsb) + fy·ΣAsb    [kN]
        Dim Ag As Single = B * H
        Dim Ast_m2 As Single = Lista_Refuerzo.Sum(Function(r) r.Asb) / 1_000_000
        Dim P0 As Single = (0.85 * fc * (Ag - Ast_m2) + fy * Ast_m2) * 1000
        Dim Pmax_phi As Single = 0.65 * 0.8 * P0   ' φ=0.65, límite 0.8·P₀ (muros c/estribos)

        ' ── Pmin: tracción pura ───────────────────────────────────────────
        Dim Pmin As Single = -fy * Ast_m2 * 1000
        Dim Pmin_phi As Single = 0.9 * Pmin

        ' ── Listas de salida ─────────────────────────────────────────────
        Dim Lista_Pn As New List(Of Single)
        Dim Lista_Phi_Pn As New List(Of Single)
        Dim Lista_Mn As New List(Of Single)
        Dim Lista_Phi_Mn As New List(Of Single)

        ' Punto inicial: compresión pura (M=0, P=P₀)
        Lista_Pn.Add(P0) : Lista_Phi_Pn.Add(Pmax_phi)
        Lista_Mn.Add(0) : Lista_Phi_Mn.Add(0)

        ' ── Barrido de eje neutro: c de H hasta 0.04m (50 pasos) ─────────
        Dim n_pasos As Integer = 50
        Dim c_ini As Single = H
        Dim c_fin As Single = 0.04
        Dim paso As Single = (c_ini - c_fin) / n_pasos

        For k As Integer = 0 To n_pasos
            Dim c As Single = c_ini - k * paso

            ' Profundidad del bloque de Whitney: a = β₁·c ≤ H
            Dim a As Single = Math.Min(beta1 * c, H)

            ' Fuerza de concreto [kN] y centroide del bloque respecto a Y=0
            Dim Cc As Single = 0.85 * fc * B * a * 1000
            Dim Yc As Single = Y_top - a / 2.0F

            ' Contribución de las barras de acero
            Dim Fst As Single = 0.0F
            Dim Mst As Single = 0.0F

            For Each bar In barras
                Dim di As Single = Y_top - bar.Yr   ' distancia fibra comprimida → barra (≥0 si está abajo de Y_top)

                ' Deformación unitaria (positivo = compresión)
                Dim esi As Single = (c - di) * 0.003F / c

                ' Esfuerzo limitado a fy
                Dim fsi As Single = Math.Max(-fy, Math.Min(fy, Es * esi))

                ' Fuerza neta [kN] (descuento de concreto para barras en zona comprimida)
                Dim Fs_bar As Single = If(di < c,
                                         (fsi - 0.85F * fc) * bar.Asb / 1000,
                                         fsi * bar.Asb / 1000)

                Fst += Fs_bar
                Mst += Fs_bar * bar.Yr   ' momento respecto a Y=0 (centroide geométrico)
            Next

            ' Resultantes
            Dim Pn As Single = Cc + Fst
            Dim Mn As Single = Math.Abs(Cc * Yc + Mst)

            ' Factor φ (NSR-10 C.9.3.2.2) según deformación en barra más traccionada
            Dim es_t As Single = (d_util - c) * 0.003F / c
            Dim ey As Single = fy / Es
            Dim phi As Single
            If Math.Abs(es_t) <= ey Then
                phi = 0.65F
            ElseIf Math.Abs(es_t) <= 2.5F * ey Then
                phi = 0.65F + (Math.Abs(es_t) - 0.002F) * (250.0F / 3.0F)
            Else
                phi = 0.9F
            End If

            Dim phi_Pn As Single = phi * Pn
            If phi_Pn > Pmax_phi Then phi_Pn = Pmax_phi
            Dim phi_Mn As Single = phi * Mn

            Lista_Pn.Add(Pn)
            Lista_Phi_Pn.Add(phi_Pn)
            Lista_Mn.Add(Mn)
            Lista_Phi_Mn.Add(phi_Mn)
        Next

        ' Punto final: tracción pura (M=0, P=Pmin)
        Lista_Pn.Add(Pmin) : Lista_Phi_Pn.Add(Pmin_phi)
        Lista_Mn.Add(0) : Lista_Phi_Mn.Add(0)

        ' Retorno: (φMn, φPn, Mn, Pn)
        Return (Lista_Phi_Mn, Lista_Phi_Pn, Lista_Mn, Lista_Pn)

    End Function



End Module
