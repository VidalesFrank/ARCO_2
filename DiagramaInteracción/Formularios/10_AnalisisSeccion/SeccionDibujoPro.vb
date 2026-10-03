Imports System.Drawing.Drawing2D
Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_10_AnalisisSeccion

' ════════════════════════════════════════════════════════════════════════════
'  Módulo 10 v2.0 — Motor de dibujo profesional de la sección transversal
'
'  Un solo Sub Dibujar() con contexto explícito de qué mostrar. Todo el
'  posicionamiento vive aquí; el formulario sólo pinta el resultado y usa
'  la transformación devuelta para el hit-test de barras.
'
'  Convención de coords: los datos vienen en metros con origen en el
'  centroide geométrico de la sección; +X a la derecha, +Y arriba.
'  Se aplica esc [px/m] y se centra en (_cx, _cy) invirtiendo Y de pantalla.
' ════════════════════════════════════════════════════════════════════════════
Public Module SeccionDibujoPro

    ' Contexto de dibujo — el formulario llena estos campos antes de pintar
    Public Class DibujoCtx
        ' Geometría
        Public TipoSec As TipoSeccion10
        Public B As Single = 0.4F, H As Single = 0.6F, D As Single = 0.5F
        Public Rec As Single = 0.04F
        Public Barras As List(Of RefuerzoSimple)
        Public ThetaRad As Single = 0

        ' Estado del eje neutro (opcional; si Nothing → sólo la sección)
        Public Estado As EstadoSeccion

        ' Materiales / cuantías
        Public Fc As Single = 21, Fy As Single = 420
        Public Ey As Single = 420 / 200000.0F
        Public Ag_cm2 As Single, Ast_cm2 As Single, Rho_pct As Single

        ' Toggles de visualización
        Public MostrarHatching As Boolean = True
        Public MostrarBloqueWhitney As Boolean = True
        Public MostrarEjeNeutro As Boolean = True
        Public MostrarCotas As Boolean = True
        Public MostrarTagsBarras As Boolean = True
        Public MostrarEjesCoord As Boolean = True
        Public MostrarAngulo As Boolean = True
        Public MostrarLeyenda As Boolean = True

        ' Zoom / pan (para futuros gestos)
        Public Zoom As Single = 1.0F
        Public PanX As Single = 0, PanY As Single = 0
    End Class

    ' Paleta profesional (tipo Section Designer / spColumn)
    Private ReadOnly _colBg As Color = Color.White
    Private ReadOnly _colConcreto As Color = Color.FromArgb(215, 218, 222)
    Private ReadOnly _colConcHatch As Color = Color.FromArgb(180, 185, 195)
    Private ReadOnly _colBordeSec As Color = Color.FromArgb(35, 45, 60)
    Private ReadOnly _colNucleo As Color = Color.FromArgb(80, 110, 160)
    Private ReadOnly _colWhitney As Color = Color.FromArgb(80, 30, 120, 200)      ' semi-transparente
    Private ReadOnly _colWhitneyBorde As Color = Color.FromArgb(0, 100, 180)
    Private ReadOnly _colEjeNeutro As Color = Color.FromArgb(200, 40, 40)
    Private ReadOnly _colBarraNeutra As Color = Color.FromArgb(60, 60, 65)
    Private ReadOnly _colBarraFluTracc As Color = Color.FromArgb(200, 40, 40)
    Private ReadOnly _colBarraFluComp As Color = Color.FromArgb(0, 100, 200)
    Private ReadOnly _colBarraElast As Color = Color.FromArgb(120, 130, 140)
    Private ReadOnly _colCota As Color = Color.FromArgb(60, 60, 65)
    Private ReadOnly _colEjeX As Color = Color.FromArgb(180, 30, 30)
    Private ReadOnly _colEjeY As Color = Color.FromArgb(30, 130, 40)
    Private ReadOnly _colTexto As Color = Color.FromArgb(30, 30, 40)

    ' ─────────────────────────────────────────────────────────────────────
    '  ENTRADA PÚBLICA — devuelve (cx, cy, esc) para hit-test en el form
    ' ─────────────────────────────────────────────────────────────────────
    Public Sub Dibujar(g As Graphics, wPx As Integer, hPx As Integer,
                       ctx As DibujoCtx,
                       ByRef cxOut As Single, ByRef cyOut As Single,
                       ByRef escOut As Single)
        g.Clear(_colBg)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        If ctx Is Nothing Then Return

        ' Márgenes internos para dejar espacio a cotas, leyenda y ejes.
        ' Con un factor de 0.85 sobre el fit natural, la sección queda con aire
        ' visual alrededor a zoom=1 y la etiqueta de leyenda no la tapa.
        Const mgL As Integer = 44
        Const mgR As Integer = 52
        Const mgT As Integer = 26
        Const mgB As Integer = 52
        Const fitFactor As Single = 0.85F

        Dim secW As Single, secH As Single
        If ctx.TipoSec = TipoSeccion10.Rectangular Then
            secW = ctx.B : secH = ctx.H
        Else
            secW = ctx.D : secH = ctx.D
        End If
        If secW <= 0 OrElse secH <= 0 Then Return

        ' Escala + centro (fit + zoom + pan)
        Dim usableW As Single = wPx - mgL - mgR
        Dim usableH As Single = hPx - mgT - mgB
        If usableW < 40 OrElse usableH < 40 Then Return
        Dim escBase As Single = Math.Min(usableW / secW, usableH / secH) * fitFactor
        Dim esc As Single = escBase * ctx.Zoom
        Dim cx As Single = mgL + usableW / 2.0F + ctx.PanX
        Dim cy As Single = mgT + usableH / 2.0F + ctx.PanY

        cxOut = cx : cyOut = cy : escOut = esc

        ' ── 1. Cuerpo de concreto (con hatch) ────────────────────────────
        DibujarCuerpoConcreto(g, ctx, cx, cy, esc)

        ' ── 2. Bloque de Whitney sombreado (si hay estado) ───────────────
        If ctx.MostrarBloqueWhitney AndAlso ctx.Estado IsNot Nothing AndAlso ctx.Estado.Convergio Then
            DibujarBloqueWhitney(g, ctx, cx, cy, esc)
        End If

        ' ── 3. Borde de la sección + núcleo (recubrimiento) ──────────────
        DibujarBordes(g, ctx, cx, cy, esc)

        ' ── 4. Eje neutro (a la profundidad c real, si hay estado) ───────
        If ctx.MostrarEjeNeutro AndAlso ctx.Estado IsNot Nothing AndAlso ctx.Estado.Convergio Then
            DibujarEjeNeutroReal(g, ctx, cx, cy, esc)
        End If

        ' ── 5. Barras (color según estado de deformación) ────────────────
        DibujarBarras(g, ctx, cx, cy, esc)

        ' ── 6. Cotas ingenieriles ────────────────────────────────────────
        If ctx.MostrarCotas Then
            DibujarCotas(g, ctx, cx, cy, esc, wPx, hPx)
        End If

        ' ── 7. Ejes coordenados X, Y (esquina inferior izquierda) ────────
        If ctx.MostrarEjesCoord Then
            DibujarEjesCoord(g, wPx, hPx)
        End If

        ' ── 8. Arco del ángulo θ (esquina inferior derecha) ──────────────
        If ctx.MostrarAngulo Then
            DibujarArcoAngulo(g, ctx.ThetaRad, wPx, hPx)
        End If

        ' ── 9. Leyenda de cuantías (esquina superior izquierda) ─────────
        If ctx.MostrarLeyenda Then
            DibujarLeyenda(g, ctx, wPx, hPx)
        End If

        ' ── 10. Indicador de zoom (esquina superior derecha) ────────────
        DibujarIndicadorZoom(g, ctx.Zoom, wPx, hPx)
    End Sub

    Private Sub DibujarIndicadorZoom(g As Graphics, zoom As Single, wPx As Integer, hPx As Integer)
        Dim texto As String
        Dim mostrar As Boolean = Math.Abs(zoom - 1.0F) > 0.01F
        If mostrar Then
            texto = String.Format("Zoom {0:F0}%   |  rueda: zoom · arrastre: pan · doble-clic: reset", zoom * 100)
        Else
            texto = "rueda: zoom  ·  arrastre: pan  ·  doble-clic: reset"
        End If
        Dim fnt As New Font("Segoe UI", 7.5F)
        Dim sz = g.MeasureString(texto, fnt)
        Dim x = wPx - sz.Width - 8, y As Single = 6
        Dim brBg As New SolidBrush(Color.FromArgb(210, 250, 250, 250))
        g.FillRectangle(brBg, x - 4, y - 2, sz.Width + 8, sz.Height + 3)
        g.DrawString(texto, fnt,
                     If(mostrar, New SolidBrush(Color.FromArgb(0, 82, 164)),
                                  New SolidBrush(Color.FromArgb(120, 130, 140))),
                     x, y)
        brBg.Dispose() : fnt.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Cuerpo de concreto con hatch
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarCuerpoConcreto(g As Graphics, ctx As DibujoCtx,
                                       cx As Single, cy As Single, esc As Single)
        Dim brushConc As Brush
        If ctx.MostrarHatching Then
            brushConc = New HatchBrush(HatchStyle.LightUpwardDiagonal, _colConcHatch, _colConcreto)
        Else
            brushConc = New SolidBrush(_colConcreto)
        End If

        If ctx.TipoSec = TipoSeccion10.Rectangular Then
            Dim wPx As Single = ctx.B * esc
            Dim hPx As Single = ctx.H * esc
            g.FillRectangle(brushConc, cx - wPx / 2, cy - hPx / 2, wPx, hPx)
        Else
            Dim Rpx As Single = (ctx.D / 2.0F) * esc
            g.FillEllipse(brushConc, cx - Rpx, cy - Rpx, 2 * Rpx, 2 * Rpx)
        End If
        brushConc.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Bordes: sección exterior + línea de recubrimiento (núcleo)
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarBordes(g As Graphics, ctx As DibujoCtx,
                                cx As Single, cy As Single, esc As Single)
        Dim penBorde As New Pen(_colBordeSec, 2.2F)
        Dim penNucleo As New Pen(_colNucleo, 1.0F) With {.DashStyle = DashStyle.Dash}

        If ctx.TipoSec = TipoSeccion10.Rectangular Then
            Dim wPx As Single = ctx.B * esc
            Dim hPx As Single = ctx.H * esc
            Dim recPx As Single = ctx.Rec * esc
            g.DrawRectangle(penBorde, cx - wPx / 2, cy - hPx / 2, wPx, hPx)
            g.DrawRectangle(penNucleo,
                            cx - wPx / 2 + recPx, cy - hPx / 2 + recPx,
                            wPx - 2 * recPx, hPx - 2 * recPx)
        Else
            Dim Rpx As Single = (ctx.D / 2.0F) * esc
            Dim recPx As Single = ctx.Rec * esc
            g.DrawEllipse(penBorde, cx - Rpx, cy - Rpx, 2 * Rpx, 2 * Rpx)
            g.DrawEllipse(penNucleo,
                          cx - Rpx + recPx, cy - Rpx + recPx,
                          2 * (Rpx - recPx), 2 * (Rpx - recPx))
        End If
        penBorde.Dispose() : penNucleo.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Bloque de Whitney sombreado (semi-transparente cyan)
    '  Zona en compresión: {(X,Y) tales que X·sinθ + Y·cosθ > projMax − a}
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarBloqueWhitney(g As Graphics, ctx As DibujoCtx,
                                      cx As Single, cy As Single, esc As Single)
        Dim est = ctx.Estado
        Dim sinT As Single = CSng(Math.Sin(ctx.ThetaRad))
        Dim cosT As Single = CSng(Math.Cos(ctx.ThetaRad))
        Dim projMax As Single = est.ProjMax
        Dim pLim As Single = projMax - est.a

        Dim brushW As New SolidBrush(_colWhitney)
        Dim penW As New Pen(_colWhitneyBorde, 1.4F) With {.DashStyle = DashStyle.Solid}

        If ctx.TipoSec = TipoSeccion10.Rectangular Then
            ' Poligono de la sección
            Dim halfB = ctx.B / 2.0F, halfH = ctx.H / 2.0F
            Dim poly = New List(Of PointF) From {
                New PointF(-halfB, -halfH),
                New PointF(halfB, -halfH),
                New PointF(halfB, halfH),
                New PointF(-halfB, halfH)
            }
            Dim clip = ClipHalfPlane(poly, sinT, cosT, pLim)
            If clip.Count >= 3 Then
                Dim ptsPx(clip.Count - 1) As PointF
                For i = 0 To clip.Count - 1
                    ptsPx(i) = New PointF(cx + clip(i).X * esc, cy - clip(i).Y * esc)
                Next
                g.FillPolygon(brushW, ptsPx)
                ' Contorno del bloque (línea del borde de compresión) — más gruesa
                ' Sólo los segmentos internos a la sección; DrawPolygon los pinta todos.
                g.DrawPolygon(penW, ptsPx)
            End If
        Else
            ' Segmento circular
            Dim R As Single = ctx.D / 2.0F
            Dim ptsSeg = SegmentoCircularPoly(R, sinT, cosT, pLim, 64)
            If ptsSeg.Count >= 3 Then
                Dim ptsPx(ptsSeg.Count - 1) As PointF
                For i = 0 To ptsSeg.Count - 1
                    ptsPx(i) = New PointF(cx + ptsSeg(i).X * esc, cy - ptsSeg(i).Y * esc)
                Next
                g.FillPolygon(brushW, ptsPx)
                g.DrawPolygon(penW, ptsPx)
            End If
        End If

        ' Etiqueta "a = β1·c" flotante junto al borde superior de la sección
        Dim secHalfH As Single = If(ctx.TipoSec = TipoSeccion10.Rectangular, ctx.H / 2, ctx.D / 2)
        Dim fnt = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        Dim texto = String.Format("a = β₁·c = {0:F3} m", est.a)
        Dim sz = g.MeasureString(texto, fnt)
        g.DrawString(texto, fnt, New SolidBrush(_colWhitneyBorde),
                     cx - sz.Width / 2, cy - secHalfH * esc - 18)
        fnt.Dispose()
        brushW.Dispose() : penW.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Eje neutro REAL: línea perpendicular a (sinθ, cosθ)
    '  a signed distance (projMax − c) del origen (centroide).
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarEjeNeutroReal(g As Graphics, ctx As DibujoCtx,
                                      cx As Single, cy As Single, esc As Single)
        Dim est = ctx.Estado
        Dim sinT As Single = CSng(Math.Sin(ctx.ThetaRad))
        Dim cosT As Single = CSng(Math.Cos(ctx.ThetaRad))
        Dim projMax As Single = est.ProjMax
        Dim dEN As Single = projMax - est.c    ' distancia con signo desde el origen a la línea EN

        ' Punto sobre la recta más cercano al origen: (dEN·sinθ, dEN·cosθ)
        Dim px0 = dEN * sinT
        Dim py0 = dEN * cosT
        ' Dirección de la línea (perpendicular a (sinθ,cosθ)) = (cosθ, −sinθ)
        Dim dirX = cosT, dirY = -sinT

        ' Recortar contra la sección para longitud visual
        Dim tMin As Single, tMax As Single
        RecortarLineaContraSeccion(ctx, px0, py0, dirX, dirY, tMin, tMax)
        If tMax <= tMin Then Return

        ' Extender un poco visualmente (25%) fuera de la sección
        Dim margen As Single = (tMax - tMin) * 0.12F
        tMin -= margen : tMax += margen

        Dim x1m = px0 + dirX * tMin, y1m = py0 + dirY * tMin
        Dim x2m = px0 + dirX * tMax, y2m = py0 + dirY * tMax
        Dim x1p = cx + x1m * esc, y1p = cy - y1m * esc
        Dim x2p = cx + x2m * esc, y2p = cy - y2m * esc

        Dim pen As New Pen(_colEjeNeutro, 2.0F) With {.DashStyle = DashStyle.DashDot}
        g.DrawLine(pen, x1p, y1p, x2p, y2p)

        ' Marca "EN" y el valor de c cerca del extremo superior
        Dim fnt = New Font("Segoe UI", 9F, FontStyle.Bold)
        Dim texto = String.Format("EN:  c = {0:F3} m  ({1:F1} cm)", est.c, est.c * 100)
        Dim brT As New SolidBrush(_colEjeNeutro)
        g.DrawString(texto, fnt, brT, x2p + 6, y2p - 8)

        ' Flecha corta hacia el lado de compresión (dirección (sinθ,cosθ) desde
        ' el centro de la línea)
        Dim xm = (x1p + x2p) / 2, ym = (y1p + y2p) / 2
        Dim flechaL As Single = 22
        Dim fx = xm + sinT * flechaL, fy = ym - cosT * flechaL
        Dim penFlecha As New Pen(_colEjeNeutro, 1.8F)
        penFlecha.CustomEndCap = New AdjustableArrowCap(4, 5)
        g.DrawLine(penFlecha, xm, ym, fx, fy)
        Dim fnt2 = New Font("Segoe UI", 7.5F, FontStyle.Italic)
        g.DrawString("Compresión", fnt2, brT, fx + 3, fy - 6)

        pen.Dispose() : penFlecha.Dispose() : fnt.Dispose() : fnt2.Dispose() : brT.Dispose()
    End Sub

    ' Recorta una recta paramétrica (px0+t·dirX, py0+t·dirY) contra la sección.
    Private Sub RecortarLineaContraSeccion(ctx As DibujoCtx,
                                            px0 As Single, py0 As Single,
                                            dirX As Single, dirY As Single,
                                            ByRef tMin As Single, ByRef tMax As Single)
        tMin = -1.0E10F : tMax = 1.0E10F
        If ctx.TipoSec = TipoSeccion10.Rectangular Then
            Dim halfB = ctx.B / 2.0F, halfH = ctx.H / 2.0F
            RecortarContraFranja(px0, dirX, -halfB, halfB, tMin, tMax)
            RecortarContraFranja(py0, dirY, -halfH, halfH, tMin, tMax)
        Else
            ' Círculo: |P0 + t·D|^2 = R^2  →  t^2 + 2 t (P0·D) + |P0|^2 − R^2 = 0
            Dim R As Single = ctx.D / 2.0F
            Dim bC = 2.0F * (px0 * dirX + py0 * dirY)
            Dim cC = px0 * px0 + py0 * py0 - R * R
            Dim disc = bC * bC - 4 * cC
            If disc < 0 Then tMin = 0 : tMax = 0 : Return
            Dim s = CSng(Math.Sqrt(disc))
            tMin = (-bC - s) / 2.0F
            tMax = (-bC + s) / 2.0F
        End If
    End Sub

    Private Sub RecortarContraFranja(p0 As Single, d As Single, pmin As Single, pmax As Single,
                                       ByRef tMin As Single, ByRef tMax As Single)
        If Math.Abs(d) < 0.000001F Then
            If p0 < pmin OrElse p0 > pmax Then tMin = 0 : tMax = 0
            Return
        End If
        Dim t1 = (pmin - p0) / d, t2 = (pmax - p0) / d
        If t1 > t2 Then Dim tmp = t1 : t1 = t2 : t2 = tmp
        If t1 > tMin Then tMin = t1
        If t2 < tMax Then tMax = t2
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  BARRAS: círculo a escala + tag + color según estado
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarBarras(g As Graphics, ctx As DibujoCtx,
                                cx As Single, cy As Single, esc As Single)
        If ctx.Barras Is Nothing Then Return
        Dim est = ctx.Estado
        Dim ey As Single = ctx.Ey
        If ey <= 0 Then ey = 0.0021F

        Dim fnt As New Font("Segoe UI", 7.0F, FontStyle.Bold)
        Dim brText As New SolidBrush(_colTexto)

        For i = 0 To ctx.Barras.Count - 1
            Dim bar = ctx.Barras(i)
            Dim dbPx As Single = Math.Max(6.0F, bar.Db * esc)
            Dim bx = cx + bar.Coordenada_X * esc - dbPx / 2
            Dim by = cy - bar.Coordenada_Y * esc - dbPx / 2

            ' Color según deformación (si hay estado)
            Dim col As Color = _colBarraNeutra
            If est IsNot Nothing AndAlso est.Convergio AndAlso
               est.Es_por_barra IsNot Nothing AndAlso i < est.Es_por_barra.Length Then
                Dim esi = est.Es_por_barra(i)
                If esi <= -ey Then
                    col = _colBarraFluTracc
                ElseIf esi >= ey Then
                    col = _colBarraFluComp
                Else
                    col = _colBarraElast
                End If
            End If

            g.FillEllipse(New SolidBrush(col), bx, by, dbPx, dbPx)
            g.DrawEllipse(New Pen(Color.Black, 0.9F), bx, by, dbPx, dbPx)

            ' Tag junto a la barra (offset radial hacia afuera del centroide)
            If ctx.MostrarTagsBarras Then
                Dim dx0 = bar.Coordenada_X, dy0 = bar.Coordenada_Y
                Dim mod0 As Single = CSng(Math.Sqrt(dx0 * dx0 + dy0 * dy0))
                Dim ox As Single = 0, oy As Single = 0
                If mod0 > 0.001F Then
                    ox = dx0 / mod0 : oy = dy0 / mod0
                End If
                Dim tx = bx + dbPx / 2 + ox * (dbPx * 0.55F + 3) - 8
                Dim ty = by + dbPx / 2 - oy * (dbPx * 0.55F + 3) - 6
                g.DrawString(bar.Name_Barra, fnt, brText, tx, ty)
            End If
        Next
        fnt.Dispose() : brText.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  COTAS ingenieriles con líneas de extensión y puntas de flecha
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarCotas(g As Graphics, ctx As DibujoCtx,
                              cx As Single, cy As Single, esc As Single,
                              wPx As Integer, hPx As Integer)
        Dim penCota As New Pen(_colCota, 1.0F)
        penCota.StartCap = LineCap.ArrowAnchor
        penCota.EndCap = LineCap.ArrowAnchor
        Dim penExt As New Pen(_colCota, 0.6F) With {.DashStyle = DashStyle.Dot}
        Dim fnt As New Font("Segoe UI", 8.0F)
        Dim br As New SolidBrush(_colCota)
        Dim sfCenter As New StringFormat With {
            .Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}

        If ctx.TipoSec = TipoSeccion10.Rectangular Then
            Dim wPxSec As Single = ctx.B * esc
            Dim hPxSec As Single = ctx.H * esc
            ' Cota B (inferior)
            Dim yCota As Single = cy + hPxSec / 2 + 22
            g.DrawLine(penExt, cx - wPxSec / 2, cy + hPxSec / 2, cx - wPxSec / 2, yCota + 3)
            g.DrawLine(penExt, cx + wPxSec / 2, cy + hPxSec / 2, cx + wPxSec / 2, yCota + 3)
            g.DrawLine(penCota, cx - wPxSec / 2, yCota, cx + wPxSec / 2, yCota)
            Dim txtB = String.Format("B = {0:F3} m", ctx.B)
            Dim sz = g.MeasureString(txtB, fnt)
            g.FillRectangle(Brushes.White, cx - sz.Width / 2, yCota - sz.Height / 2, sz.Width, sz.Height)
            g.DrawString(txtB, fnt, br, cx, yCota, sfCenter)

            ' Cota H (lateral derecha)
            Dim xCota As Single = cx + wPxSec / 2 + 28
            g.DrawLine(penExt, cx + wPxSec / 2, cy - hPxSec / 2, xCota + 3, cy - hPxSec / 2)
            g.DrawLine(penExt, cx + wPxSec / 2, cy + hPxSec / 2, xCota + 3, cy + hPxSec / 2)
            g.DrawLine(penCota, xCota, cy - hPxSec / 2, xCota, cy + hPxSec / 2)
            Dim txtH = String.Format("H = {0:F3} m", ctx.H)
            Dim st = g.Save()
            g.TranslateTransform(xCota + 12, cy)
            g.RotateTransform(-90)
            Dim szH = g.MeasureString(txtH, fnt)
            g.FillRectangle(Brushes.White, -szH.Width / 2, -szH.Height / 2, szH.Width, szH.Height)
            g.DrawString(txtH, fnt, br, 0, 0, sfCenter)
            g.Restore(st)
        Else
            Dim R As Single = ctx.D / 2.0F
            Dim wPxSec As Single = ctx.D * esc
            Dim yCota As Single = cy + wPxSec / 2 + 22
            g.DrawLine(penExt, cx - wPxSec / 2, cy + wPxSec / 2, cx - wPxSec / 2, yCota + 3)
            g.DrawLine(penExt, cx + wPxSec / 2, cy + wPxSec / 2, cx + wPxSec / 2, yCota + 3)
            g.DrawLine(penCota, cx - wPxSec / 2, yCota, cx + wPxSec / 2, yCota)
            Dim txt = String.Format("D = {0:F3} m", ctx.D)
            Dim sz = g.MeasureString(txt, fnt)
            g.FillRectangle(Brushes.White, cx - sz.Width / 2, yCota - sz.Height / 2, sz.Width, sz.Height)
            g.DrawString(txt, fnt, br, cx, yCota, sfCenter)
        End If

        ' Cota de recubrimiento
        Dim txtR = String.Format("recub. = {0:F3} m", ctx.Rec)
        g.DrawString(txtR, fnt, br, cx - 60, cy + (If(ctx.TipoSec = TipoSeccion10.Rectangular, ctx.H, ctx.D)) * esc / 2 + 4)

        penCota.Dispose() : penExt.Dispose() : fnt.Dispose() : br.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Ejes coordenados X, Y (esquina inferior izquierda)
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarEjesCoord(g As Graphics, wPx As Integer, hPx As Integer)
        Dim x0 As Single = 22, y0 As Single = hPx - 22
        Dim L As Single = 26
        Dim pX As New Pen(_colEjeX, 1.8F)
        pX.EndCap = LineCap.ArrowAnchor
        Dim pY As New Pen(_colEjeY, 1.8F)
        pY.EndCap = LineCap.ArrowAnchor
        g.DrawLine(pX, x0, y0, x0 + L, y0)
        g.DrawLine(pY, x0, y0, x0, y0 - L)
        Dim fnt As New Font("Segoe UI", 8.5F, FontStyle.Bold)
        g.DrawString("X", fnt, New SolidBrush(_colEjeX), x0 + L + 2, y0 - 7)
        g.DrawString("Y", fnt, New SolidBrush(_colEjeY), x0 - 4, y0 - L - 13)
        pX.Dispose() : pY.Dispose() : fnt.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Arco del ángulo θ (esquina inferior derecha)
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarArcoAngulo(g As Graphics, thetaRad As Single, wPx As Integer, hPx As Integer)
        Dim cx As Single = wPx - 60, cy As Single = hPx - 30
        Dim R As Single = 22
        Dim penArc As New Pen(Color.FromArgb(80, 90, 110), 1.3F)
        Dim penRad As New Pen(Color.FromArgb(80, 90, 110), 1.0F)
        ' Y de referencia: hacia arriba (θ=0)
        g.DrawLine(penRad, cx, cy, cx, cy - R - 4)
        ' Radio girado θ (medido desde Y hacia X, sentido horario en pantalla = antihorario matemático)
        Dim sinT = CSng(Math.Sin(thetaRad)), cosT = CSng(Math.Cos(thetaRad))
        Dim rx = cx + sinT * (R + 4), ry = cy - cosT * (R + 4)
        Dim penGiro As New Pen(_colEjeNeutro, 1.6F) With {.CustomEndCap = New AdjustableArrowCap(4, 5)}
        g.DrawLine(penGiro, cx, cy, rx, ry)
        ' Arco desde Y (0°) hasta θ. En GDI+ los ángulos de arco van desde eje +X en sentido horario:
        Dim startAng As Single = -90            ' eje +Y de la matemática
        Dim sweep As Single = CSng(thetaRad * 180.0 / Math.PI)
        If Math.Abs(sweep) > 0.5F Then
            g.DrawArc(penArc, cx - R, cy - R, 2 * R, 2 * R, startAng, sweep)
        End If
        Dim fnt As New Font("Segoe UI", 8F, FontStyle.Bold)
        Dim txt = String.Format("θ = {0:F0}°", CSng(thetaRad * 180.0 / Math.PI))
        g.DrawString(txt, fnt, New SolidBrush(_colEjeNeutro), cx - 18, cy + R + 2)
        penArc.Dispose() : penRad.Dispose() : penGiro.Dispose() : fnt.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Leyenda con cuantías (esquina superior izquierda)
    ' ═════════════════════════════════════════════════════════════════════
    Private Sub DibujarLeyenda(g As Graphics, ctx As DibujoCtx, wPx As Integer, hPx As Integer)
        Dim x As Single = 8, y As Single = 6
        Dim w As Single = 168, h As Single = 62
        Dim brBg As New SolidBrush(Color.FromArgb(235, 245, 250, 255))
        Dim penBox As New Pen(Color.FromArgb(180, 190, 200), 0.8F)
        g.FillRectangle(brBg, x, y, w, h)
        g.DrawRectangle(penBox, x, y, w, h)

        Dim fntTit As New Font("Segoe UI", 8F, FontStyle.Bold)
        Dim fnt As New Font("Segoe UI", 7.5F)
        Dim brT As New SolidBrush(_colTexto)
        g.DrawString("PROPIEDADES DE LA SECCIÓN", fntTit, brT, x + 6, y + 3)
        Dim yy As Single = y + 18
        g.DrawString(String.Format("Ag = {0:F1} cm²    Ast = {1:F2} cm²", ctx.Ag_cm2, ctx.Ast_cm2),
                     fnt, brT, x + 6, yy) : yy += 11
        g.DrawString(String.Format("ρ = {0:F3}%    β₁ = {1:F3}", ctx.Rho_pct,
                                    Math.Max(0.65, Math.Min(0.85, 0.85 - 0.05 * (ctx.Fc - 28.0) / 7.0))),
                     fnt, brT, x + 6, yy) : yy += 11
        g.DrawString(String.Format("f'c = {0:F0} MPa    fy = {1:F0} MPa    εy = {2:F4}",
                                    ctx.Fc, ctx.Fy, ctx.Ey), fnt, brT, x + 6, yy)
        brBg.Dispose() : penBox.Dispose() : fntTit.Dispose() : fnt.Dispose() : brT.Dispose()
    End Sub

    ' ═════════════════════════════════════════════════════════════════════
    '  Utilidades geométricas: clipping de polígono con semiplano
    '  y aproximación poligonal de segmento circular
    ' ═════════════════════════════════════════════════════════════════════

    ' Sutherland-Hodgman: recorta polygon con {(X,Y) : X·nx + Y·ny > d}
    Private Function ClipHalfPlane(poly As List(Of PointF),
                                     nx As Single, ny As Single, d As Single) As List(Of PointF)
        Dim salida As New List(Of PointF)
        If poly.Count = 0 Then Return salida
        Dim n = poly.Count
        For i = 0 To n - 1
            Dim A = poly(i)
            Dim B = poly((i + 1) Mod n)
            Dim vA = A.X * nx + A.Y * ny - d
            Dim vB = B.X * nx + B.Y * ny - d
            Dim inA = vA > 0
            Dim inB = vB > 0
            If inA Then salida.Add(A)
            If inA <> inB Then
                Dim t = vA / (vA - vB)
                salida.Add(New PointF(A.X + t * (B.X - A.X), A.Y + t * (B.Y - A.Y)))
            End If
        Next
        Return salida
    End Function

    ' Segmento circular: {(X,Y): X² + Y² ≤ R² Y X·nx + Y·ny > d}
    ' Devuelve un polígono con nArc+2 vértices (arco + los dos cortes de cuerda).
    Private Function SegmentoCircularPoly(R As Single, nx As Single, ny As Single,
                                            d As Single, nArc As Integer) As List(Of PointF)
        Dim res As New List(Of PointF)
        ' Distancia desde el origen a la cuerda es |d| (con (nx,ny) unitario).
        Dim mag = CSng(Math.Sqrt(nx * nx + ny * ny))
        If mag < 0.0001F Then Return res
        Dim nx1 = nx / mag, ny1 = ny / mag
        Dim d1 = d / mag
        If d1 >= R Then Return res       ' semiplano no toca el círculo
        If d1 <= -R Then                 ' círculo entero adentro → círculo completo
            For i = 0 To nArc - 1
                Dim ang = 2.0 * Math.PI * i / nArc
                res.Add(New PointF(CSng(R * Math.Cos(ang)), CSng(R * Math.Sin(ang))))
            Next
            Return res
        End If

        ' Puntos de la cuerda: proyección del origen sobre la línea + ± sqrt(R²-d²) en dir perpendicular
        Dim halfChord = CSng(Math.Sqrt(R * R - d1 * d1))
        Dim px0 = nx1 * d1, py0 = ny1 * d1
        Dim tX = -ny1, tY = nx1         ' perpendicular unitaria a (nx1,ny1)
        Dim P1 = New PointF(px0 - tX * halfChord, py0 - tY * halfChord)
        Dim P2 = New PointF(px0 + tX * halfChord, py0 + tY * halfChord)

        ' Ángulos: partimos del extremo P1 y arqueamos hasta P2 por el lado donde
        ' el signo del centro proyectado sobre (nx1,ny1) es positivo.
        Dim a1 = CSng(Math.Atan2(P1.Y, P1.X))
        Dim a2 = CSng(Math.Atan2(P2.Y, P2.X))
        ' Elegimos la dirección de arco que pasa por el punto sobre la normal (nx1·R, ny1·R) (que está dentro)
        Dim aTest = CSng(Math.Atan2(ny1, nx1))
        ' Normaliza a2 respecto de a1 por el lado del test:
        Dim signo As Integer = If(NormAng(a2 - a1) > 0, 1, -1)
        If NormAng(aTest - a1) * signo < 0 Then signo = -signo

        res.Add(P1)
        For k = 1 To nArc - 1
            Dim frac = CSng(k / CSng(nArc))
            Dim delta = signo * frac * Math.Abs(NormAng(a2 - a1))
            If signo * NormAng(a2 - a1) < 0 Then
                delta = signo * frac * (2 * CSng(Math.PI) - Math.Abs(NormAng(a2 - a1)))
            End If
            Dim ang = a1 + delta
            res.Add(New PointF(CSng(R * Math.Cos(ang)), CSng(R * Math.Sin(ang))))
        Next
        res.Add(P2)
        Return res
    End Function

    Private Function NormAng(a As Single) As Single
        Do While a > Math.PI : a -= CSng(2 * Math.PI) : Loop
        Do While a < -Math.PI : a += CSng(2 * Math.PI) : Loop
        Return a
    End Function

End Module
