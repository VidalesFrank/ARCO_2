Imports ARCO.Funciones_00_Varias
Public Class Funciones_02_Columnas
    Public Shared Function FuncionCortante(ByVal B As Single, ByVal H As Single, ByVal fc As Single, ByVal Fy As Single, ByVal s As Single,
                        ByVal Ref_Trans As String, ByVal Num_Ramas As Integer, ByVal Vu As Single, ByVal Pu As Single)

        Dim Revision(5)

        Dim Ass As Single = AreaRefuerzo(Ref_Trans)
        Dim Ae As Single = B * (H - 0.04)
        Dim Vs As Single = 0.75 * Num_Ramas * Ass * Fy * (H - 0.04) / (s * 1000)
        Dim Vc0 As Single = 0.17 * 0.75 * Math.Sqrt(fc) * Ae * 1000                                                   'C.11-3
        Dim Vc1 As Single = 0.17 * 0.75 * (1 + (Pu / (14000 * Ae))) * Math.Sqrt(fc) * Ae * 1000                       'C.11-4
        Dim Vc2 As Single = 0.29 * Math.Sqrt(fc) * Ae * 1000 * Math.Sqrt(1 + (0.29 * Pu / Ae))                        'C.11-7
        Dim Vc As Single = Math.Min(Math.Max(Vc0, Vc1), Vc2)
        Dim Vn As Single = Vc + Vs
        Dim Fmax As Single = Vn / Vu

        Revision(1) = Math.Round(Vc, 2)
        Revision(2) = Math.Round(Vs, 2)
        Revision(3) = Math.Round(Vn, 2)
        Revision(4) = Math.Round(Vu, 2)
        Revision(5) = Math.Round(Fmax, 2)

        FuncionCortante = Revision

    End Function
    Public Shared Function FuncionConfinamiento(ByVal B As Single, ByVal H As Single, ByVal fc As Single, ByVal fy As Single, ByVal s As Single,
                                   ByVal Numero_Barra_Min_Long As String, ByVal Numero_Barra_Estribo As String, ByVal Disipacion As String)

        Dim bc As Single = B - 0.08
        Dim hc As Single = H - 0.08

        Dim Ag As Single = B * H
        Dim Ach As Single = bc * hc

        Dim Ash1 As Single = 0.2 * s * bc * fc * ((Ag / Ach) - 1) / fy
        Dim Ash2 As Single = 0.06 * s * bc * fc / fy

        Dim Db_Barra_Long As Single = DiametroRefuerzo(Numero_Barra_Min_Long)
        Dim As_Barra_Long As Single = AreaRefuerzo(Numero_Barra_Min_Long)

        Dim Db_Estribo As Single = DiametroRefuerzo(Numero_Barra_Estribo)
        Dim As_Estribo As Single = AreaRefuerzo(Numero_Barra_Estribo)

        Dim S0 As Single = Math.Min(Math.Min(Math.Min(8 * Db_Barra_Long, 16 * Db_Estribo), Math.Min(B, H) / 3), 0.15)
        Dim L0 As Single = Math.Max(Math.Max(2.3 / 6, Math.Max(B, H)), 0.5)

        If Disipacion = "DES" Then
            Ash1 = 0.3 * s * bc * fc * ((Ag / Ach) - 1) / fy
            Ash2 = 0.09 * s * bc * fc / fy

            S0 = Math.Min(Math.Min(Math.Min(B, H) / 4, Db_Barra_Long * 6), 0.15)
            L0 = Math.Max(2.3 / 6, 0.45)
        End If

        Dim Ash As Single = Math.Max(Ash1, Ash2) * 1000000
        Dim Cantidad_Ramas_Req As Single = Ash / As_Estribo

        Dim Resultados_Confinamiento(4)

        Resultados_Confinamiento(1) = Ash
        Resultados_Confinamiento(2) = Cantidad_Ramas_Req
        Resultados_Confinamiento(3) = S0
        Resultados_Confinamiento(4) = L0

        FuncionConfinamiento = Resultados_Confinamiento
    End Function

    Public Shared Function FuncionALR(ByVal B As Single, ByVal H As Single, ByVal fc As Single, ByVal Pu As Single)

        Dim Ag As Single = B * H
        FuncionALR = Pu / (Ag * fc * 1000)

    End Function

    Public Shared Function ColumnasDiseno(ByVal Opcion As String)
        Dim Vector_Columnas(5)

        Dim Piso As Integer = 0
        Dim Label As Integer = 1
        Dim Seccion As Integer
        Dim Salto As Integer
        Dim As_Req As Integer

        If Opcion = "Frame" Then
            Seccion = 3
            Salto = 3
            As_Req = 10
        Else
            Seccion = 1
            Salto = 2
            As_Req = 9
        End If

        Vector_Columnas(0) = Piso
        Vector_Columnas(1) = Label
        Vector_Columnas(2) = Seccion
        Vector_Columnas(3) = Salto
        Vector_Columnas(4) = As_Req

        ColumnasDiseno = Vector_Columnas

    End Function

    ' Índices en el vector: 0=Piso, 1=Label, 2=Combinacion, 3=Salto, 4=P, 5=V2, 6=V3,
    '                       7=T, 8=M2, 9=M3, 10=StepType
    ' Formato ETABS E23. E17 tenía columnas desplazadas (sin UniqueName/StepType).
    Public Shared Function ColumnasFuerzas(ByVal Opcion As String)
        Dim Vector_Columnas(11)

        Dim Piso As Integer = 0
        Dim Label As Integer = 1
        Dim Combinacion As Integer
        Dim Salto As Integer
        Dim P As Integer
        Dim V2 As Integer
        Dim V3 As Integer
        Dim T As Integer
        Dim M2 As Integer
        Dim M3 As Integer
        Dim StepType As Integer

        If Opcion = "Frame" Then
            ' Element Forces - Columns (E23):
            ' Story(0) Column(1) UniqueName(2) OutputCase(3) CaseType(4) StepType(5) StepNum(6) Station(7) P(8) V2(9) V3(10) T(11) M2(12) M3(13)
            Combinacion = 3
            Salto = 3
            P = 8
            V2 = 9
            V3 = 10
            T = 11
            M2 = 12
            M3 = 13
            StepType = 5
        Else
            ' Pier Forces (E23):
            ' Story(0) Pier(1) OutputCase(2) CaseType(3) StepType(4) StepNum(5) Location(6) P(7) V2(8) V3(9) T(10) M2(11) M3(12)
            Combinacion = 2
            Salto = 2
            P = 7
            V2 = 8
            V3 = 9
            T = 10
            M2 = 11
            M3 = 12
            StepType = 4
        End If

        Vector_Columnas(0) = Piso
        Vector_Columnas(1) = Label
        Vector_Columnas(2) = Combinacion
        Vector_Columnas(3) = Salto
        Vector_Columnas(4) = P
        Vector_Columnas(5) = V2
        Vector_Columnas(6) = V3
        Vector_Columnas(7) = T
        Vector_Columnas(8) = M2
        Vector_Columnas(9) = M3
        Vector_Columnas(10) = StepType

        ColumnasFuerzas = Vector_Columnas

    End Function

    ''' <summary>
    ''' Escanea la fila 0 (encabezados) de una tabla ETABS de fuerzas y retorna un diccionario
    ''' nombre_canónico→índice_columna. Funciona con cualquier versión de ETABS (E17/E23) y con
    ''' tablas de 12 o 13 columnas según las cargas exportadas.
    ''' Claves garantizadas: Story, Label, OutputCase, P, V2, V3, T, M2, M3, StepType (-1 si ausente).
    ''' </summary>
    Public Shared Function IndicesColumnasFuerzas(Tabla As DataGridView) As Dictionary(Of String, Integer)
        Dim dict As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        dict("StepType") = -1

        If Tabla Is Nothing OrElse Tabla.Rows.Count = 0 Then Return dict

        For ci = 0 To Tabla.Columns.Count - 1
            Dim h As String = If(Tabla.Rows(0).Cells(ci).Value?.ToString().Trim(), "")
            Select Case h.ToUpperInvariant()
                Case "STORY"                          : dict("Story") = ci
                Case "PIER", "COLUMN", "ELEMENT"      : If Not dict.ContainsKey("Label") Then dict("Label") = ci
                Case "OUTPUT CASE", "LOAD CASE/COMBO" : dict("OutputCase") = ci
                Case "CASE TYPE"                      : dict("CaseType") = ci
                Case "STEP TYPE"                      : dict("StepType") = ci
                Case "STEP NUMBER"                    : dict("StepNumber") = ci
                Case "LOCATION", "STATION"            : dict("Location") = ci
                Case "P"                              : dict("P") = ci
                Case "V2"                             : dict("V2") = ci
                Case "V3"                             : dict("V3") = ci
                Case "T"                              : dict("T") = ci
                Case "M2"                             : dict("M2") = ci
                Case "M3"                             : dict("M3") = ci
            End Select
        Next
        Return dict
    End Function

    ''' <summary>Construye la clave de combinación canónica. Normaliza E17 ("Envolvente Min" → "Envolvente (Min)").</summary>
    Public Shared Function ConstruirClaveCombo(ByVal outputCase As String, ByVal stepType As String) As String
        If Not String.IsNullOrWhiteSpace(stepType) Then
            Return outputCase.Trim() & " (" & stepType.Trim() & ")"
        End If
        Dim t = outputCase.Trim()
        If t.EndsWith(" Max", StringComparison.OrdinalIgnoreCase) Then
            Return t.Substring(0, t.Length - 4).TrimEnd() & " (Max)"
        ElseIf t.EndsWith(" Min", StringComparison.OrdinalIgnoreCase) Then
            Return t.Substring(0, t.Length - 4).TrimEnd() & " (Min)"
        End If
        Return t
    End Function

    Public Shared Function ColumnasSecciones(ByVal Opcion As String)

        Dim Vector_Columnas(5)

        Dim Piso As Integer
        Dim Name As Integer
        Dim Material As Integer
        Dim B As Integer
        Dim H As Integer

        If Opcion = "Frame" Then
            Name = 0
            Material = 1
            B = 3
            H = 4
        Else
            Piso = 0
            Name = 1
            Material = 9
            B = 5
            H = 6
        End If

        Vector_Columnas(0) = Piso
        Vector_Columnas(1) = Name
        Vector_Columnas(2) = Material
        Vector_Columnas(3) = B
        Vector_Columnas(4) = H

        ColumnasSecciones = Vector_Columnas

    End Function

    Public Shared Sub FuncionColorCumple(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer, ByVal Opcion As String)

        If Opcion = "Cumple" Then
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 239, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(0, 97, 0)
        Else
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(255, 199, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(156, 0, 6)
        End If

    End Sub


    ' Interpola φMn en el diagrama P-M a un Pu dado (convención diagrama: compresión positiva).
    ' Retorna 0 si Pu está fuera del rango del diagrama.
    Public Shared Function InterpolarMnEnPu(ByVal Lista_Phi_Pn As List(Of Single),
                                                ByVal Lista_Phi_Mn As List(Of Single),
                                                ByVal Pu As Single) As Single
        If Lista_Phi_Pn Is Nothing OrElse Lista_Phi_Pn.Count < 2 Then Return 0
        For i = 0 To Lista_Phi_Pn.Count - 2
            Dim P1 = Lista_Phi_Pn(i)
            Dim P2 = Lista_Phi_Pn(i + 1)
            If P1 >= Pu AndAlso Pu >= P2 Then
                Dim dP = P1 - P2
                If Math.Abs(dP) < 0.001 Then Return Lista_Phi_Mn(i)
                Dim t = (P1 - Pu) / dP
                Return Lista_Phi_Mn(i) + t * (Lista_Phi_Mn(i + 1) - Lista_Phi_Mn(i))
            End If
        Next
        Return 0
    End Function

    ' Adapta Lista_Detalles_Refuerzo a List(Of RefuerzoSimple) para el diagrama.
    ' intercambiarXY=True produce la lista para el eje débil (M2).
    Public Shared Function AdaptarRefuerzoDI(
        ByVal lista As List(Of Tramo_Columna.Detalles_Refuerzo_Longitudinal),
        ByVal intercambiarXY As Boolean) As List(Of RefuerzoSimple)

        Dim ref As New List(Of RefuerzoSimple)
        If lista Is Nothing Then Return ref
        Dim idx As Integer = 0
        For Each b In lista
            Dim cx = If(intercambiarXY, b.Coordenada_Y, b.Coordenada_X)
            Dim cy = If(intercambiarXY, b.Coordenada_X, b.Coordenada_Y)
            ref.Add(New RefuerzoSimple(idx, "", b.Db / 1000.0F, b.Asb, cx, cy))
            idx += 1
        Next
        Return ref
    End Function

    ' Genera diagramas P-M biaxiales (M3 eje fuerte, M2 eje débil) y verifica
    ' todas las combinaciones del tramo usando interacción lineal (Bresler α=1).
    ' fy y Es en MPa. Estacion = "Top" o "Bottom".
    ' Retorna True si los diagramas se calcularon, False si no hay refuerzo.
    Public Shared Function FuncionDiagramaColumna(
        ByVal tramo As Tramo_Columna,
        ByVal fy As Single,
        ByVal Es As Single,
        ByVal estacion As String,
        Optional ByVal combosDiseno As IEnumerable(Of String) = Nothing) As Boolean

        Dim barras As List(Of Tramo_Columna.Detalles_Refuerzo_Longitudinal)
        If tramo.Distribucion_Personalizada AndAlso tramo.Lista_Barras_Seccion.Count > 0 Then
            ' Convertir Barra_Seccion → Detalles_Refuerzo_Longitudinal
            Dim temp As New List(Of Tramo_Columna.Detalles_Refuerzo_Longitudinal)
            For Each bs In tramo.Lista_Barras_Seccion
                Dim d As New Tramo_Columna.Detalles_Refuerzo_Longitudinal
                d.Asb = bs.Asb : d.Db = bs.Db * 1000.0F
                d.Coordenada_X = bs.X : d.Coordenada_Y = bs.Y
                temp.Add(d)
            Next
            barras = temp
        ElseIf estacion = "Top" Then
            barras = tramo.Lista_Detalles_Refuerzo_Top
        Else
            barras = tramo.Lista_Detalles_Refuerzo_Bottom
        End If

        If barras Is Nothing OrElse barras.Count = 0 Then Return False

        Dim B = tramo.B_Plano
        Dim H = tramo.H_Plano
        Dim fc = tramo.fc

        ' M3 (eje fuerte, bending en H): width=B, depth=H, barras con coords originales
        Dim refM3 = AdaptarRefuerzoDI(barras, False)
        Dim diM3 = Funcion_Diagrama_Interaccion(B, H, refM3, fc, fy, Es, 0)

        ' M2 (eje débil, bending en B): width=H, depth=B, barras con X↔Y
        Dim refM2 = AdaptarRefuerzoDI(barras, True)
        Dim diM2 = Funcion_Diagrama_Interaccion(H, B, refM2, fc, fy, Es, 0)

        ' Guardar curvas en el tramo
        tramo.Lista_DI_M3_Phi = diM3.Item1
        tramo.Lista_DI_M3_P_Phi = diM3.Item2
        tramo.Lista_DI_Mn_M3 = diM3.Item3
        tramo.Lista_DI_Pn_M3 = diM3.Item4

        tramo.Lista_DI_M2_Phi = diM2.Item1
        tramo.Lista_DI_M2_P_Phi = diM2.Item2
        tramo.Lista_DI_Mn_M2 = diM2.Item3
        tramo.Lista_DI_Pn_M2 = diM2.Item4

        ' Verificar combinaciones de diseño (Bresler lineal)
        Dim maxDC As Single = 0
        Dim comboGob As String = ""
        Dim combosAEvaluar = If(combosDiseno IsNot Nothing AndAlso combosDiseno.Any(),
                                tramo.Lista_Combinaciones.Where(Function(c) combosDiseno.Contains(c.Name)).ToList(),
                                tramo.Lista_Combinaciones)
        For Each combo In combosAEvaluar
            ' Convención diagrama: compresión positiva (ETABS: compresión negativa)
            Dim Pu_dia As Single = -combo.P
            Dim M3u = Math.Abs(combo.M3)
            Dim M2u = Math.Abs(combo.M2)

            Dim phiMn3 = InterpolarMnEnPu(tramo.Lista_DI_M3_P_Phi, tramo.Lista_DI_M3_Phi, Pu_dia)
            Dim phiMn2 = InterpolarMnEnPu(tramo.Lista_DI_M2_P_Phi, tramo.Lista_DI_M2_Phi, Pu_dia)

            Dim DC As Single = 0
            If phiMn3 > 0.001 Then DC += M3u / phiMn3
            If phiMn2 > 0.001 Then DC += M2u / phiMn2

            If DC > maxDC Then
                maxDC = DC
                comboGob = combo.Name
                tramo.Pu_Gob_DI = combo.P
                tramo.M3u_Gob_DI = combo.M3
                tramo.M2u_Gob_DI = combo.M2
            End If
        Next

        tramo.F_Interaccion = Math.Round(maxDC, 3)
        tramo.Combo_Gobernante_DI = comboGob
        Return True
    End Function

    ' Distribución uniforme: N barras igualmente espaciadas en el perímetro de la zona de
    ' recubrimiento. Parte de la esquina superior-izquierda y avanza en sentido horario.
    ' Retorna coords(0..N-1, 1..2) donde (i,1)=X, (i,2)=Y en metros.
    Public Shared Function DistribuirBarrasPerimetro(B As Single, H As Single, N As Integer) As Single(,)
        Dim coords(Math.Max(N - 1, 0), 2) As Single
        If N <= 0 OrElse B <= 0.1F OrElse H <= 0.1F Then Return coords
        Dim Bci = B / 2.0F - 0.05F   ' medio ancho libre (centro a borde)
        Dim Hci = H / 2.0F - 0.05F   ' medio alto libre
        Dim Lx = 2.0F * Bci           ' longitud cara horizontal
        Dim Ly = 2.0F * Hci           ' longitud cara vertical
        Dim perim = 2.0F * (Lx + Ly)
        Dim s = perim / N
        For i = 0 To N - 1
            Dim d = CSng(i) * s
            Dim x, y As Single
            If d < Lx Then                           ' cara superior (izq→der)
                x = -Bci + d : y = Hci
            ElseIf d < Lx + Ly Then                  ' cara derecha (arr→abj)
                x = Bci : y = Hci - (d - Lx)
            ElseIf d < 2.0F * Lx + Ly Then           ' cara inferior (der→izq)
                x = Bci - (d - Lx - Ly) : y = -Hci
            Else                                      ' cara izquierda (abj→arr)
                x = -Bci : y = -Hci + (d - 2.0F * Lx - Ly)
            End If
            coords(i, 1) = x : coords(i, 2) = y
        Next
        Return coords
    End Function

    ' Distribuye N barras garantizando una en cada esquina y el resto repartido
    ' proporcionalmente en los 4 lados según la longitud de cada cara.
    ' Retorna coords(0..N-1, 1..2) donde (i,1)=X, (i,2)=Y en metros.
    Public Shared Function DistribuirBarrasConEsquinas(B As Single, H As Single, N As Integer) As Single(,)
        If N < 4 Then N = 4
        Dim coords(N - 1, 2) As Single
        If B <= 0.1F OrElse H <= 0.1F Then Return coords

        Dim Bci = B / 2.0F - 0.05F   ' medio ancho libre
        Dim Hci = H / 2.0F - 0.05F   ' medio alto libre
        Dim Lx = 2.0F * Bci           ' longitud cara horizontal (B)
        Dim Ly = 2.0F * Hci           ' longitud cara vertical (H)

        ' Barras intermedias (sin esquinas): repartir en pares por proporción de cara
        Dim nRem As Integer = N - 4
        Dim nL As Integer = 0   ' intermedias por cara vertical (H)
        Dim nC As Integer = 0   ' intermedias por cara horizontal (B)
        If nRem > 0 Then
            Dim nPares As Integer = nRem \ 2
            nL = CInt(Math.Round(CDbl(nPares) * Ly / (Lx + Ly)))
            nC = nPares - nL
        End If

        Dim idx As Integer = 0

        ' 4 esquinas
        coords(idx, 1) = -Bci : coords(idx, 2) =  Hci : idx += 1  ' sup-izq
        coords(idx, 1) =  Bci : coords(idx, 2) =  Hci : idx += 1  ' sup-der
        coords(idx, 1) =  Bci : coords(idx, 2) = -Hci : idx += 1  ' inf-der
        coords(idx, 1) = -Bci : coords(idx, 2) = -Hci : idx += 1  ' inf-izq

        ' Intermedias cara superior e inferior (B direction)
        For k = 1 To nC
            Dim xPos = CSng(-Bci + k * 2.0F * Bci / (nC + 1))
            coords(idx, 1) = xPos : coords(idx, 2) =  Hci : idx += 1
            coords(idx, 1) = xPos : coords(idx, 2) = -Hci : idx += 1
        Next

        ' Intermedias cara izquierda y derecha (H direction)
        For k = 1 To nL
            Dim yPos = CSng(-Hci + k * 2.0F * Hci / (nL + 1))
            coords(idx, 1) = -Bci : coords(idx, 2) = yPos : idx += 1
            coords(idx, 1) =  Bci : coords(idx, 2) = yPos : idx += 1
        Next

        ' Si N era impar (inusual), barra extra al centro de la cara superior
        Do While idx < N
            coords(idx, 1) = 0 : coords(idx, 2) = Hci : idx += 1
        Loop

        Return coords
    End Function

    Public Shared Function CoordenadasBarras(ByVal B As Single, ByVal H As Single, ByVal Cantidad As Integer, ByVal Cantidad_Corto As Integer, ByVal Cantidad_Largo As Integer)

        Dim Perimetro As Single = (B - 2 * 0.05) * 2 + (H - 2 * 0.05) * 2
        Dim Dist_Barras As Single = Perimetro / Cantidad

        Dim Bc As Single = B - 0.1
        Dim Hc As Single = H - 0.1

        Dim Lista_Coordendas(Cantidad, 2)

        Lista_Coordendas(0, 1) = 0.05 - B / 2
        Lista_Coordendas(0, 2) = H / 2 - 0.05
        Lista_Coordendas(1, 1) = B / 2 - 0.05
        Lista_Coordendas(1, 2) = H / 2 - 0.05
        Lista_Coordendas(2, 1) = 0.05 - B / 2
        Lista_Coordendas(2, 2) = 0.05 - H / 2
        Lista_Coordendas(3, 1) = B / 2 - 0.05
        Lista_Coordendas(3, 2) = 0.05 - H / 2

        For i = 0 To Cantidad_Corto - 1
            Lista_Coordendas(4 + i, 1) = Bc * ((i + 1) / (Cantidad_Corto + 1) - 0.5)
            Lista_Coordendas(4 + i, 2) = Hc / 2
        Next
        For i = 0 To Cantidad_Corto - 1
            Lista_Coordendas(4 + Cantidad_Corto + i, 1) = Bc * ((i + 1) / (Cantidad_Corto + 1) - 0.5)
            Lista_Coordendas(4 + Cantidad_Corto + i, 2) = -1 * Hc / 2
        Next
        For i = 0 To Cantidad_Largo - 1
            Lista_Coordendas(4 + 2 * Cantidad_Corto + i, 1) = -1 * Bc / 2
            Lista_Coordendas(4 + 2 * Cantidad_Corto + i, 2) = Hc * ((i + 1) / (Cantidad_Largo + 1) - 0.5)
        Next
        For i = 0 To Cantidad_Largo - 1
            Lista_Coordendas(4 + 2 * Cantidad_Corto + Cantidad_Largo + i, 1) = Bc / 2
            Lista_Coordendas(4 + 2 * Cantidad_Corto + Cantidad_Largo + i, 2) = Hc * ((i + 1) / (Cantidad_Largo + 1) - 0.5)
        Next

        CoordenadasBarras = Lista_Coordendas
    End Function






End Class
