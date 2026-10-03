Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_10_AnalisisSeccion

' ════════════════════════════════════════════════════════════════════════════
'  Servicio del Módulo 10 v2.0 — Análisis de Sección Pro
'
'  Motor de cálculo del eje neutro c dado un Pu nominal, por bisección exacta
'  sobre las mismas ecuaciones del DI (bloque de Whitney + compatibilidad de
'  deformaciones lineal con eps_cu=0.003 en la fibra extrema comprimida).
'
'  Convención: se trabaja SIEMPRE con Pn (sin φ). El usuario pasa Pu ya sin
'  factorar y compara contra la curva nominal del DI.
' ════════════════════════════════════════════════════════════════════════════
Public Module AnalisisSeccionV2Service

    ' Estado completo de la sección para un (P, θ) dados
    Public Class EstadoSeccion
        Public c As Single                    ' m — profundidad del eje neutro desde fibra extrema comprimida
        Public a As Single                    ' m — profundidad del bloque de Whitney = β1·c
        Public Beta1 As Single
        Public Pn As Single                   ' kN — axial de equilibrio (debe = P_solicitado)
        Public Mn As Single                   ' kN·m — momento nominal resultante
        Public P_solicitado As Single         ' kN — el P que dio el usuario
        Public Es_extrema_tracc As Single     ' + = deformación en la barra más traccionada
        Public Es_extrema_comp As Single      ' + = deformación en la barra más comprimida
        Public Es_por_barra() As Single       ' εs de cada barra (+ = compresión, − = tracción)
        Public Sig_por_barra() As Single      ' fs de cada barra (MPa, saturado a ±fy)
        Public Zona As String                 ' clasificación ACI/NSR-10
        Public Convergio As Boolean
        Public Mensaje As String = ""
        Public ThetaRad As Single
        Public ProjMax As Single              ' proyección máxima de la sección en dirección θ

        ' Rango físico del axial para esta sección (para reportar en pantalla)
        Public P0_nom As Single               ' kN — compresión pura nominal
        Public Pmin_nom As Single             ' kN — tracción pura nominal
    End Class

    ' ─────────────────────────────────────────────────────────────────────
    '  ENTRADA PÚBLICA — bisección exacta de c dado Pu nominal
    ' ─────────────────────────────────────────────────────────────────────
    Public Function CalcularEjeNeutroDadoP(
            P_nominal As Single,
            tipoSec As TipoSeccion10,
            B As Single, H As Single,
            rec As Single,
            barras As List(Of RefuerzoSimple),
            fc As Single, fy As Single, Es As Single,
            Optional thetaRad As Single = 0.0F) As EstadoSeccion

        Dim r As New EstadoSeccion
        r.P_solicitado = P_nominal
        r.ThetaRad = thetaRad
        r.Convergio = False

        If barras Is Nothing OrElse barras.Count = 0 Then
            r.Mensaje = "Sección sin refuerzo definido"
            Return r
        End If
        If B <= 0 OrElse (tipoSec = TipoSeccion10.Rectangular AndAlso H <= 0) Then
            r.Mensaje = "Geometría inválida"
            Return r
        End If

        Dim beta1 As Single = CSng(Math.Max(0.65, Math.Min(0.85, 0.85 - 0.05 * (fc - 28.0) / 7.0)))
        r.Beta1 = beta1
        r.ProjMax = ProyeccionMaxima10(tipoSec, B, H, thetaRad)

        ' P0 y Pmin nominales (idénticos a la convención del DI)
        Dim Ag As Single
        If tipoSec = TipoSeccion10.Circular Then
            Ag = CSng(Math.PI * (B / 2.0F) ^ 2)
        Else
            Ag = B * H
        End If
        Dim Ast_m2 As Single = barras.Sum(Function(bb) bb.Asb) / 1_000_000.0F
        r.P0_nom = (0.85F * fc * (Ag - Ast_m2) + fy * Ast_m2) * 1000.0F
        r.Pmin_nom = -fy * Ast_m2 * 1000.0F

        ' Chequeo de rango físico
        If P_nominal > r.P0_nom * 1.001F Then
            r.Mensaje = String.Format("Pu = {0:F0} kN excede P0 nominal = {1:F0} kN. Fuera del diagrama.",
                                       P_nominal, r.P0_nom)
            Return r
        End If
        If P_nominal < r.Pmin_nom * 1.001F Then
            r.Mensaje = String.Format("Pu = {0:F0} kN < Pmin nominal = {1:F0} kN. Sección en tracción pura: no aplica c.",
                                       P_nominal, r.Pmin_nom)
            Return r
        End If

        ' ── Bisección sobre c ──────────────────────────────────────────────
        ' Pn(c) es monótonamente creciente con c (más c → más zona en compresión).
        ' Rango: c muy pequeño → Pn ≈ Pmin (tracción). c muy grande → Pn ≈ P0.
        Dim c_lo As Single = 0.0001F
        Dim c_hi As Single = 6.0F * r.ProjMax  ' techo generoso: garantiza a saturar en 2·projMax

        Dim c_try As Single = 0
        For iter = 0 To 80
            c_try = (c_lo + c_hi) / 2.0F
            Dim P_try = EvalPnRapido(c_try, tipoSec, B, H, rec, barras,
                                      fc, fy, Es, beta1, r.ProjMax, thetaRad)
            If P_try > P_nominal Then c_hi = c_try Else c_lo = c_try
            If Math.Abs(c_hi - c_lo) < 0.00002F Then Exit For
        Next

        Dim c_final As Single = (c_lo + c_hi) / 2.0F
        r.c = c_final
        r.a = Math.Min(beta1 * c_final, 2.0F * r.ProjMax)

        ' Evaluación completa (Pn, Mn, εs por barra)
        EvalCompleto(c_final, tipoSec, B, H, rec, barras,
                     fc, fy, Es, beta1, r.ProjMax, thetaRad,
                     r.Pn, r.Mn,
                     r.Es_por_barra, r.Sig_por_barra,
                     r.Es_extrema_tracc, r.Es_extrema_comp)

        ' Clasificación ACI 318 / NSR-10 C.10.3.4
        Dim ey As Single = fy / Es
        If r.Es_extrema_tracc <= ey * 1.0001F Then
            r.Zona = "Compresión controlada (εt ≤ εy)"
        ElseIf r.Es_extrema_tracc >= 0.005F Then
            r.Zona = "Tracción controlada (εt ≥ 0.005)"
        Else
            r.Zona = "Transición (εy < εt < 0.005)"
        End If

        r.Convergio = True
        r.Mensaje = "OK"
        Return r
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  Evaluación de Pn(c) — versión rápida para la bisección
    ' ─────────────────────────────────────────────────────────────────────
    Private Function EvalPnRapido(c As Single, tipoSec As TipoSeccion10,
                                   B As Single, H As Single, rec As Single,
                                   barras As List(Of RefuerzoSimple),
                                   fc As Single, fy As Single, Es As Single,
                                   beta1 As Single, projMax As Single,
                                   thetaRad As Single) As Single
        Dim Pn As Single, Mn As Single
        Dim esArr() As Single, sigArr() As Single
        Dim esTrac As Single, esComp As Single
        EvalCompleto(c, tipoSec, B, H, rec, barras, fc, fy, Es, beta1, projMax, thetaRad,
                     Pn, Mn, esArr, sigArr, esTrac, esComp)
        Return Pn
    End Function

    ' ─────────────────────────────────────────────────────────────────────
    '  Evaluación completa Pn, Mn, εs, fs para c dado
    '
    '  Reproduce EXACTAMENTE las hipótesis de DI_Rectangular10 y DI_Circular10:
    '   • Bloque de Whitney con β1
    '   • Deformación lineal desde fibra extrema comprimida, εcu=0.003
    '   • Barra en zona comprimida: descuento de 0.85·fc a fsi
    '   • Barras saturadas a ±fy
    ' ─────────────────────────────────────────────────────────────────────
    Public Sub EvalCompleto(c As Single, tipoSec As TipoSeccion10,
                             B As Single, H As Single, rec As Single,
                             barras As List(Of RefuerzoSimple),
                             fc As Single, fy As Single, Es As Single,
                             beta1 As Single, projMax As Single, thetaRad As Single,
                             ByRef Pn As Single, ByRef Mn As Single,
                             ByRef Es_arr() As Single, ByRef Sig_arr() As Single,
                             ByRef Es_extrema_tracc As Single,
                             ByRef Es_extrema_comp As Single)

        Dim sinT As Single = CSng(Math.Sin(thetaRad))
        Dim cosT As Single = CSng(Math.Cos(thetaRad))
        Dim a As Single = Math.Min(beta1 * c, 2.0F * projMax)

        ' ── Contribución del concreto ─────────────────────────────────────
        Dim Cc As Single = 0, Mcc As Single = 0
        If tipoSec = TipoSeccion10.Circular Then
            Dim R = B / 2.0F           ' B guarda el diámetro D cuando circular
            Dim y0 = R - a             ' límite inferior del segmento comprimido
            Dim Ac As Single, Yc As Single
            SegCircularLocal(R, y0, Ac, Yc)
            Cc = 0.85F * fc * Ac * 1000.0F           ' kN
            Mcc = Cc * Yc                             ' kN·m (Yc respecto al centroide)
        ElseIf Math.Abs(thetaRad) < 0.0001F Then
            ' Rectangular θ=0 → integración analítica: bloque de ancho B, altura a
            Cc = 0.85F * fc * B * a * 1000.0F         ' kN
            Mcc = Cc * (H / 2.0F - a / 2.0F)          ' kN·m (brazo al centroide)
        Else
            ' Rectangular θ≠0 → grilla 2D
            Dim celdas = ConstruirGrillaLocal(B, H)
            Dim pLimite = projMax - a
            For Each cel In celdas
                Dim p = cel.X * sinT + cel.Y * cosT
                If p > pLimite Then
                    Dim Fi = 0.85F * fc * cel.Area * 1000.0F
                    Cc += Fi
                    Mcc += Fi * p
                End If
            Next
        End If

        ' ── Contribución del acero ────────────────────────────────────────
        Dim nBar = barras.Count
        ReDim Es_arr(nBar - 1)
        ReDim Sig_arr(nBar - 1)
        Es_extrema_tracc = 0
        Es_extrema_comp = 0
        Dim Fst As Single = 0, Mst As Single = 0

        For i = 0 To nBar - 1
            Dim bar = barras(i)
            Dim p_bar = bar.Coordenada_X * sinT + bar.Coordenada_Y * cosT
            Dim di = projMax - p_bar
            Dim esi As Single = 0
            If c > 0.001F Then esi = (c - di) * 0.003F / c    ' + = compresión

            Dim fsi As Single = Math.Max(-fy, Math.Min(fy, Es * esi))          ' MPa
            Dim descuento As Single = If(di < c, 0.85F * fc, 0)                ' concreto que ocupa el área de la barra
            Dim Fs = (fsi - descuento) * bar.Asb / 1000.0F                     ' kN  (Asb en mm² → MPa·mm²=N → /1000 = kN)
            Fst += Fs
            Mst += Fs * p_bar

            Es_arr(i) = esi
            Sig_arr(i) = fsi
            If esi < 0 Then Es_extrema_tracc = Math.Max(Es_extrema_tracc, -esi)
            If esi > 0 Then Es_extrema_comp = Math.Max(Es_extrema_comp, esi)
        Next

        Pn = Cc + Fst
        Mn = CSng(Math.Abs(Mcc + Mst))
    End Sub

    ' ─────────────────────────────────────────────────────────────────────
    '  Auxiliares locales (evita depender de Private del módulo de motor)
    ' ─────────────────────────────────────────────────────────────────────

    ' Segmento circular: área y centroide (zona superior desde y=y0 hasta y=+R)
    Private Sub SegCircularLocal(R As Single, y0 As Single,
                                  ByRef Ac As Single, ByRef Yc As Single)
        y0 = Math.Max(-R, Math.Min(R, y0))
        Dim cosT As Double = y0 / R
        cosT = Math.Max(-1.0, Math.Min(1.0, cosT))
        Dim theta As Double = Math.Acos(cosT)
        Dim sinT As Double = Math.Sin(theta)
        Ac = CSng(R ^ 2 * (theta - sinT * Math.Cos(theta)))
        Dim denom As Double = 2.0 * theta - Math.Sin(2.0 * theta)
        Yc = If(Math.Abs(denom) < 1.0E-12, 0, CSng(4.0 * R * sinT ^ 3 / (3.0 * denom)))
    End Sub

    Private Structure CeldaLocal
        Public X As Single
        Public Y As Single
        Public Area As Single
    End Structure

    Private Function ConstruirGrillaLocal(B As Single, H As Single,
                                           Optional nx As Integer = 50,
                                           Optional ny As Integer = 50) As List(Of CeldaLocal)
        Dim res As New List(Of CeldaLocal)
        If B <= 0 OrElse H <= 0 Then Return res
        Dim dx = B / nx, dy = H / ny
        Dim areaCelda = dx * dy
        For i = 0 To nx - 1
            Dim xc = -B / 2.0F + (i + 0.5F) * dx
            For j = 0 To ny - 1
                Dim yc = -H / 2.0F + (j + 0.5F) * dy
                res.Add(New CeldaLocal With {.X = xc, .Y = yc, .Area = areaCelda})
            Next
        Next
        Return res
    End Function

End Module
