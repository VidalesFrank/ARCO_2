Imports System.Drawing
Imports ARCO.eNumeradores
Imports ClosedXML.Excel

Public Class Form_Reporte_Resumen
    Inherits Form

    ' ── Datos del modelo ──────────────────────────────────────────────────────
    Public Property Vigas As List(Of cViga)

    ' Punto único de verdad del umbral de cumplimiento: Funciones_00_Varias.UMBRAL_CD (0.90).
    ' Antes las pestañas/hojas "Resumen Completo" usaban 1.0 y el resto 0.9, así que
    ' una misma viga con C/D entre 0.90 y 0.99 salía OK en una vista y "Revisar" en otra.
    Private Const UMBRAL_CD As Double = Funciones_00_Varias.UMBRAL_CD

    ' ── Paleta (igual que la app) ─────────────────────────────────────────────
    Private ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ColorEncabezadoTexto As Color = Color.White
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ColorAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ColorAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    ' Colores ClosedXML equivalentes
    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlAlertaFondo As XLColor = XLColor.FromHtml("#FFEB9C")
    Private ReadOnly XlAlertaTexto As XLColor = XLColor.FromHtml("#9C5700")
    Private ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F8F8F8")

    ' ── Grids ─────────────────────────────────────────────────────────────────
    Private WithEvents DgvFlexion As New DataGridView()
    Private WithEvents DgvCortanteTodas As New DataGridView()
    Private WithEvents DgvCortanteNoCumple As New DataGridView()
    Private WithEvents DgvCompleto As New DataGridView()

    ' ── Controles toolbar ─────────────────────────────────────────────────────
    Private WithEvents _chkSoloObs As New CheckBox()

    ' ── Tab activo para saber qué exportar ────────────────────────────────────
    Private _tabs As TabControl

    ' =========================================================================
    Public Sub New()

        Me.Text = "Reporte Resumen — Diseño de Vigas"
        Me.Size = New Size(1320, 800)
        Me.MinimumSize = New Size(900, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' ── TabControl ────────────────────────────────────────────────────────
        _tabs = New TabControl()
        _tabs.Dock = DockStyle.Fill
        _tabs.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        _tabs.Padding = New Point(16, 6)

        ' Tab 1 — Flexión
        Dim tabFlex As New TabPage("  Revisión Flexión  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvFlexion.Dock = DockStyle.Fill
        EstilarGrid(DgvFlexion)
        tabFlex.Controls.Add(DgvFlexion)
        _tabs.TabPages.Add(tabFlex)

        ' Tab 2 — Cortante
        Dim tabCor As New TabPage("  Resumen Cortante  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        Dim split As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Horizontal,
            .SplitterDistance = 340,
            .BackColor = Color.FromArgb(200, 200, 200)
        }
        split.Panel1.Controls.Add(DgvCortanteTodas)
        split.Panel1.Controls.Add(CrearPanelHeader("Todas las Vigas"))
        DgvCortanteTodas.Dock = DockStyle.Fill
        EstilarGrid(DgvCortanteTodas)
        split.Panel2.Controls.Add(DgvCortanteNoCumple)
        split.Panel2.Controls.Add(CrearPanelHeader("Vigas que No Cumplen a Cortante"))
        DgvCortanteNoCumple.Dock = DockStyle.Fill
        EstilarGrid(DgvCortanteNoCumple)
        tabCor.Controls.Add(split)
        _tabs.TabPages.Add(tabCor)

        ' Tab 3 — Completo
        Dim tabComp As New TabPage("  Resumen Completo  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvCompleto.Dock = DockStyle.Fill
        EstilarGrid(DgvCompleto)
        tabComp.Controls.Add(DgvCompleto)
        _tabs.TabPages.Add(tabComp)

        Me.Controls.Add(_tabs)

        ' ── Barra inferior ────────────────────────────────────────────────────
        Dim barra As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 9, 10, 9)
        }

        _chkSoloObs.Text = "Solo elementos con observaciones"
        _chkSoloObs.AutoSize = True
        _chkSoloObs.Location = New Point(10, 16)
        _chkSoloObs.Font = New Font("Segoe UI", 10)
        barra.Controls.Add(_chkSoloObs)

        Dim btnActualizar As New Button() With {
            .Text = "Actualizar",
            .Size = New Size(120, 36),
            .Location = New Point(280, 9),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = ColorEncabezado,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnActualizar.FlatAppearance.BorderSize = 0
        AddHandler btnActualizar.Click, Sub(s, ev) CargarTodo()
        barra.Controls.Add(btnActualizar)

        Dim btnExportar As New Button() With {
            .Text = "Exportar a Excel",
            .Size = New Size(160, 36),
            .Location = New Point(410, 9),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(21, 130, 70),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnExportar.FlatAppearance.BorderSize = 0
        AddHandler btnExportar.Click, AddressOf BtnExportar_Click
        barra.Controls.Add(btnExportar)

        Me.Controls.Add(barra)

        AddHandler Me.Load, AddressOf Form_Load
        AddHandler _chkSoloObs.CheckedChanged, Sub(s, ev) CargarResumenFlexion()

    End Sub

    Private Function CrearPanelHeader(titulo As String) As Panel
        Dim p As New Panel() With {.Dock = DockStyle.Top, .Height = 36, .BackColor = Color.FromArgb(60, 60, 60)}
        Dim lbl As New Label() With {
            .Text = "  " & titulo,
            .Dock = DockStyle.Fill,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        p.Controls.Add(lbl)
        Return p
    End Function

    ' =========================================================================
    ' CARGA
    ' =========================================================================

    Private Sub Form_Load(sender As Object, e As EventArgs)
        CargarTodo()
    End Sub

    Private Sub CargarTodo()
        If Vigas Is Nothing Then Return
        CargarResumenFlexion()
        CargarResumenCortante()
        CargarResumenCompleto()
    End Sub

    ' ── REVISIÓN FLEXIÓN (por apoyo, solo vigas con refuerzo ingresado) ────────

    Private Sub CargarResumenFlexion()
        Dim dgv = DgvFlexion
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Piso", "Piso", 68)
        AgregarColumna(dgv, "Viga", "Viga", 115)
        AgregarColumna(dgv, "Tramo", "Tramo", 82)
        AgregarColumna(dgv, "AsColSupI", "As Col Sup I (cm²)", 132)
        AgregarColumna(dgv, "AsReqSupI", "As Req Sup I (cm²)", 132)
        AgregarColumna(dgv, "CDSupI", "C/D Sup I", 78)
        AgregarColumna(dgv, "AsColSupJ", "As Col Sup J (cm²)", 132)
        AgregarColumna(dgv, "AsReqSupJ", "As Req Sup J (cm²)", 132)
        AgregarColumna(dgv, "CDSupJ", "C/D Sup J", 78)
        AgregarColumna(dgv, "AsColInf", "As Col Inf (cm²)", 115)
        AgregarColumna(dgv, "AsReqInf", "As Req Inf (cm²)", 115)
        AgregarColumna(dgv, "CDInf", "C/D Inf", 78)
        AgregarColumna(dgv, "Obs", "Observaciones", 230)

        Dim idx As Integer = 0

        For Each viga In Vigas
            For Each frame In viga.Frames
                If Not (frame.RefuerzoSuperior.Any() OrElse frame.RefuerzoInferior.Any()) Then Continue For

                Dim revIzq = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Izquierda)
                Dim revCen = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Centro)
                Dim revDer = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Derecha)

                Dim asColSupI As Double = If(revIzq IsNot Nothing, revIzq.ResultadoActual.AsProvSup / 100.0, 0)
                Dim asReqSupI As Double = If(revIzq IsNot Nothing, revIzq.ResultadoActual.AsReqSup / 100.0, 0)
                Dim cdSupI As Double = If(revIzq IsNot Nothing AndAlso revIzq.ResultadoActual.RatioSup > 0,
                                          revIzq.ResultadoActual.RatioSup, -1)

                Dim asColSupJ As Double = If(revDer IsNot Nothing, revDer.ResultadoActual.AsProvSup / 100.0, 0)
                Dim asReqSupJ As Double = If(revDer IsNot Nothing, revDer.ResultadoActual.AsReqSup / 100.0, 0)
                Dim cdSupJ As Double = If(revDer IsNot Nothing AndAlso revDer.ResultadoActual.RatioSup > 0,
                                          revDer.ResultadoActual.RatioSup, -1)

                Dim asColInf As Double = If(revCen IsNot Nothing, revCen.ResultadoActual.AsProvInf / 100.0, 0)
                Dim asReqInf As Double = If(revCen IsNot Nothing, revCen.ResultadoActual.AsReqInf / 100.0, 0)
                Dim cdInf As Double = If(revCen IsNot Nothing AndAlso revCen.ResultadoActual.RatioInf > 0,
                                          revCen.ResultadoActual.RatioInf, -1)

                Dim tieneObs = (cdSupI > 0 AndAlso cdSupI < UMBRAL_CD) OrElse
                               (cdSupJ > 0 AndAlso cdSupJ < UMBRAL_CD) OrElse
                               (cdInf > 0 AndAlso cdInf < UMBRAL_CD)
                If _chkSoloObs.Checked AndAlso Not tieneObs Then Continue For

                Dim tramoStr As String
                If Not String.IsNullOrWhiteSpace(frame.EjeApoyo_I) OrElse Not String.IsNullOrWhiteSpace(frame.EjeApoyo_J) Then
                    tramoStr = $"{frame.EjeApoyo_I}-{frame.EjeApoyo_J}"
                Else
                    tramoStr = frame.ObjectLabel
                End If

                Dim obs As New List(Of String)
                If cdSupI > 0 AndAlso cdSupI < UMBRAL_CD Then obs.Add("apoyo I por M(-)")
                If cdSupJ > 0 AndAlso cdSupJ < UMBRAL_CD Then obs.Add("apoyo J por M(-)")
                If cdInf > 0 AndAlso cdInf < UMBRAL_CD Then obs.Add("centro por M(+)")
                Dim obsStr = If(obs.Count > 0, "En " & String.Join(" y ", obs), "")

                Dim r = dgv.Rows.Add()
                Dim row = dgv.Rows(r)
                If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)

                row.Cells("Piso").Value = viga.Piso
                row.Cells("Viga").Value = NombreReporte(viga)
                row.Cells("Tramo").Value = tramoStr
                row.Cells("AsColSupI").Value = If(asColSupI > 0, CObj(Math.Round(asColSupI, 2)), "—")
                row.Cells("AsReqSupI").Value = If(asReqSupI > 0, CObj(Math.Round(asReqSupI, 2)), "—")
                AsignarFactorCelda(row.Cells("CDSupI"), If(cdSupI >= 0, cdSupI, Double.MaxValue))
                row.Cells("AsColSupJ").Value = If(asColSupJ > 0, CObj(Math.Round(asColSupJ, 2)), "—")
                row.Cells("AsReqSupJ").Value = If(asReqSupJ > 0, CObj(Math.Round(asReqSupJ, 2)), "—")
                AsignarFactorCelda(row.Cells("CDSupJ"), If(cdSupJ >= 0, cdSupJ, Double.MaxValue))
                row.Cells("AsColInf").Value = If(asColInf > 0, CObj(Math.Round(asColInf, 2)), "—")
                row.Cells("AsReqInf").Value = If(asReqInf > 0, CObj(Math.Round(asReqInf, 2)), "—")
                AsignarFactorCelda(row.Cells("CDInf"), If(cdInf >= 0, cdInf, Double.MaxValue))
                row.Cells("Obs").Value = obsStr
                row.Cells("Obs").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
                idx += 1
            Next
        Next
    End Sub

    ' ── REVISIÓN CORTANTE (por frame, zona gobernante, todas las vigas) ────────

    Private Sub CargarResumenCortante()
        For Each dgv In {DgvCortanteTodas, DgvCortanteNoCumple}
            dgv.Columns.Clear()
            dgv.Rows.Clear()
            AgregarColumna(dgv, "Piso", "Piso", 68)
            AgregarColumna(dgv, "Viga", "Viga", 115)
            AgregarColumna(dgv, "Tramo", "Tramo", 82)
            AgregarColumna(dgv, "Vu", "Vu (kN)", 88)
            AgregarColumna(dgv, "Vn", "φVn (kN)", 88)
            AgregarColumna(dgv, "Factor", "C/D", 78)
            AgregarColumna(dgv, "Estado", "Estado", 85)
        Next

        Dim idxT As Integer = 0
        Dim idxN As Integer = 0

        For Each viga In Vigas
            For Each frame In viga.Frames
                If Not (frame.RefuerzoSuperior.Any() OrElse frame.RefuerzoInferior.Any()) Then Continue For
                Dim zonaGob = frame.RevisionCortante.Where(Function(z) z.phiVn > 0).
                                                      OrderBy(Function(z) z.Factor).
                                                      FirstOrDefault()
                If zonaGob Is Nothing Then Continue For

                Dim tramoStr As String
                If Not String.IsNullOrWhiteSpace(frame.EjeApoyo_I) OrElse Not String.IsNullOrWhiteSpace(frame.EjeApoyo_J) Then
                    tramoStr = $"{frame.EjeApoyo_I}-{frame.EjeApoyo_J}"
                Else
                    tramoStr = frame.ObjectLabel
                End If

                ' Falla "real": alguna zona falla el estándar Y no está cubierta por cortante plástico
                Dim failReal = frame.RevisionCortante.Any(Function(z)
                    Return z.phiVn > 0 AndAlso z.Factor > 0 AndAlso z.Factor < UMBRAL_CD AndAlso
                           Not CumpleCortantePlastico(z.Posicion, frame.CortantePlastico)
                End Function)

                Dim cumple = Not failReal
                Dim vuShow = zonaGob.Vu
                Dim vnShow = zonaGob.phiVn
                Dim factorShow = zonaGob.Factor
                Dim etiqueta As String

                If failReal Then
                    ' Mostrar la zona con la peor falla real
                    Dim peor = frame.RevisionCortante.Where(Function(z)
                        Return z.phiVn > 0 AndAlso z.Factor > 0 AndAlso z.Factor < UMBRAL_CD AndAlso
                               Not CumpleCortantePlastico(z.Posicion, frame.CortantePlastico)
                    End Function).OrderBy(Function(z) z.Factor).First()
                    vuShow = peor.Vu : vnShow = peor.phiVn : factorShow = peor.Factor
                    etiqueta = "Revisar"
                ElseIf zonaGob.Factor < UMBRAL_CD Then
                    etiqueta = "OK (Plást.)"
                Else
                    etiqueta = "OK"
                End If

                AgregarFilaCortanteFrame(DgvCortanteTodas, idxT, viga.Piso, NombreReporte(viga),
                                         tramoStr, vuShow, vnShow, factorShow, cumple, etiqueta)
                idxT += 1

                If Not cumple Then
                    AgregarFilaCortanteFrame(DgvCortanteNoCumple, idxN, viga.Piso, NombreReporte(viga),
                                             tramoStr, vuShow, vnShow, factorShow, cumple, etiqueta)
                    idxN += 1
                End If
            Next
        Next

        If idxN = 0 Then
            Dim r = DgvCortanteNoCumple.Rows.Add()
            DgvCortanteNoCumple.Rows(r).Cells("Piso").Value = "Todas las vigas cumplen a cortante"
            DgvCortanteNoCumple.Rows(r).DefaultCellStyle.BackColor = ColorOK
            DgvCortanteNoCumple.Rows(r).DefaultCellStyle.ForeColor = ColorOKTexto
        End If
    End Sub

    ''' Regla convencional-vs-plástico centralizada en VigaService (único punto de verdad).
    ''' Ver VigaService.CumpleCortantePlastico para el criterio y por qué la zona Centro
    ''' siempre devuelve False.
    Private Shared Function CumpleCortantePlastico(pos As PosicionTramoViga,
                                                    cp As cResultadoCortantePlasticoFrame) As Boolean
        Return VigaService.CumpleCortantePlastico(pos, cp)
    End Function

    ''' Devuelve True si alguna zona Centro de la viga no alcanza UMBRAL_CD en el chequeo
    ''' convencional. Se usa en el "Resumen Completo" para impedir que el cortante plástico
    ''' (que solo cubre las rótulas de los extremos) marque la viga como OK.
    Private Shared Function FallaZonaCentral(viga As cViga) As Boolean
        If viga Is Nothing OrElse viga.Frames Is Nothing Then Return False
        For Each frame In viga.Frames
            If frame.RevisionCortante Is Nothing Then Continue For
            For Each z In frame.RevisionCortante
                If z.Posicion = PosicionTramoViga.Centro AndAlso
                   z.phiVn > 0 AndAlso z.Factor > 0 AndAlso z.Factor < UMBRAL_CD Then
                    Return True
                End If
            Next
        Next
        Return False
    End Function

    Private Sub AgregarFilaCortanteFrame(dgv As DataGridView, idx As Integer,
                                          piso As String, viga As String, tramo As String,
                                          vu As Double, vn As Double, factor As Double,
                                          cumple As Boolean, Optional etiqueta As String = Nothing)
        Dim r = dgv.Rows.Add()
        Dim row = dgv.Rows(r)
        If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
        row.Cells("Piso").Value = piso
        row.Cells("Viga").Value = viga
        row.Cells("Tramo").Value = tramo
        row.Cells("Vu").Value = Math.Round(vu, 2).ToString("F2")
        row.Cells("Vn").Value = Math.Round(vn, 2).ToString("F2")
        AsignarFactorCelda(row.Cells("Factor"), factor)
        Dim lbl = If(etiqueta IsNot Nothing, etiqueta, If(cumple, "OK", "Revisar"))
        row.Cells("Estado").Value = lbl
        row.Cells("Estado").Style.BackColor = If(cumple, ColorOK, ColorMal)
        row.Cells("Estado").Style.ForeColor = If(cumple, ColorOKTexto, ColorMalTexto)
        row.Cells("Estado").Style.Font = New Font("Segoe UI", 10, FontStyle.Bold)
    End Sub

    ' ── RESUMEN COMPLETO ──────────────────────────────────────────────────────

    Private Sub CargarResumenCompleto()

        Dim dgv = DgvCompleto
        dgv.Columns.Clear()
        dgv.Rows.Clear()

        AgregarColumna(dgv, "Piso", "Piso", 80)
        AgregarColumna(dgv, "Eje", "Eje", 60)
        AgregarColumna(dgv, "Viga", "Nombre (plano)", 160)
        AgregarColumna(dgv, "Frames", "Frames ETABS", 180)
        AgregarColumna(dgv, "FNeg", "F M-  mín", 100)
        AgregarColumna(dgv, "FPos", "F M+  mín", 100)
        AgregarColumna(dgv, "FCor", "F Cor Final", 110)
        AgregarColumna(dgv, "EstFlex", "Flexión", 90)
        AgregarColumna(dgv, "EstCor", "Cortante", 90)
        AgregarColumna(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0

        For Each viga In Vigas

            ' Solo vigas con refuerzo longitudinal colocado (implica que fue revisada)
            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())
            Dim tieneCor = viga.Frames.Any(Function(f) f.RevisionCortante.Any(Function(z) z.phiVn > 0))
            If Not tieneRef Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue
            Dim fConMin As Double = Double.MaxValue
            Dim cumpleFlex As Boolean = True

            For Each frame In viga.Frames
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act.AsReqSup > 0 AndAlso act.RatioSup > 0 Then
                        fNegMin = Math.Min(fNegMin, act.RatioSup)
                        If Not act.CumpleSuperior Then cumpleFlex = False
                    End If
                    If act.AsReqInf > 0 AndAlso act.RatioInf > 0 Then
                        fPosMin = Math.Min(fPosMin, act.RatioInf)
                        If Not act.CumpleInferior Then cumpleFlex = False
                    End If
                Next
                For Each zona In frame.RevisionCortante
                    If zona.phiVn > 0 Then fConMin = Math.Min(fConMin, zona.Factor)
                Next
            Next

            ' Cortante plástico — envolvente
            Dim fPlas As Double = Double.MaxValue
            For Each frame In viga.Frames
                If frame.CortantePlastico Is Nothing Then Continue For
                Dim cp = frame.CortantePlastico
                If cp.ZonaIzq.phiVn > 0 Then fPlas = Math.Min(fPlas, cp.ZonaIzq.Factor)
                If cp.ZonaDer.phiVn > 0 Then fPlas = Math.Min(fPlas, cp.ZonaDer.Factor)
            Next
            Dim tienePlastico = (fPlas < Double.MaxValue)
            Dim fallaCentro As Boolean = FallaZonaCentral(viga)

            Dim cumpleConv = (fConMin <> Double.MaxValue AndAlso fConMin >= UMBRAL_CD)
            ' El cortante plástico (C.21.5.4) solo cubre las rótulas de los extremos: si la
            ' zona Centro falla el chequeo convencional, no puede "rescatar" a la viga.
            Dim cumplePlas = (tienePlastico AndAlso fPlas >= UMBRAL_CD AndAlso Not fallaCentro)
            Dim cumpleCor = cumpleConv OrElse cumplePlas

            Dim fFin As Double
            If cumpleConv Then
                fFin = fConMin
            ElseIf cumplePlas Then
                fFin = fPlas
            ElseIf tienePlastico Then
                fFin = Math.Max(If(fConMin = Double.MaxValue, 0.0, fConMin), fPlas)
            Else
                fFin = fConMin
            End If

            Dim r = dgv.Rows.Add()
            Dim row = dgv.Rows(r)

            row.Cells("Piso").Value = viga.Piso
            row.Cells("Eje").Value = If(String.IsNullOrWhiteSpace(viga.EjeParalelo), "-", viga.EjeParalelo)
            row.Cells("Viga").Value = NombreReporte(viga)
            row.Cells("Frames").Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            AsignarFactorCelda(row.Cells("FNeg"), fNegMin)
            AsignarFactorCelda(row.Cells("FPos"), fPosMin)
            AsignarFactorCelda(row.Cells("FCor"), fFin)

            ' Flexión
            If Not tieneRef Then
                AplicarEstado(row.Cells("EstFlex"), "Sin ref.", ColorAlerta, ColorAlertaTexto)
            ElseIf cumpleFlex Then
                AplicarEstado(row.Cells("EstFlex"), "OK", ColorOK, ColorOKTexto)
            Else
                AplicarEstado(row.Cells("EstFlex"), "Revisar", ColorMal, ColorMalTexto)
            End If

            ' Cortante
            If fFin = Double.MaxValue Then
                AplicarEstado(row.Cells("EstCor"), "Sin datos", ColorAlerta, ColorAlertaTexto)
            ElseIf cumpleCor Then
                AplicarEstado(row.Cells("EstCor"), "OK", ColorOK, ColorOKTexto)
            Else
                AplicarEstado(row.Cells("EstCor"), "Revisar", ColorMal, ColorMalTexto)
            End If

            ' Estado general
            Dim ok = tieneRef AndAlso cumpleFlex AndAlso tieneCor AndAlso cumpleCor
            Dim pendiente = Not tieneRef OrElse Not tieneCor

            If ok Then
                AplicarEstado(row.Cells("Estado"), "OK", ColorOK, ColorOKTexto)
            ElseIf pendiente Then
                AplicarEstado(row.Cells("Estado"), "Pendiente", ColorAlerta, ColorAlertaTexto)
            Else
                AplicarEstado(row.Cells("Estado"), "Revisar", ColorMal, ColorMalTexto)
            End If

            If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            idx += 1

        Next

    End Sub

    ' =========================================================================
    ' EXPORTAR EXCEL — ClosedXML
    ' =========================================================================

    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)

        If Vigas Is Nothing OrElse Vigas.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim dlg As New SaveFileDialog() With {
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = $"ARCO_Vigas_Reporte_{DateTime.Now:yyyyMMdd}",
            .Title = "Exportar Reporte de Vigas"
        }

        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Me.Cursor = Cursors.WaitCursor

            Using wb As New XLWorkbook()

                ExportarHojaFlexion(wb)
                ExportarHojaCortante(wb)
                ExportarHojaCompleto(wb)

                wb.SaveAs(dlg.FileName)

            End Using

            Me.Cursor = Cursors.Default
            MessageBox.Show("Reporte exportado correctamente." & vbCrLf & dlg.FileName,
                            "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            MessageBox.Show("Error al exportar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    ' ── Hoja Flexión ──────────────────────────────────────────────────────────

    Private Sub ExportarHojaFlexion(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Revisión Flexión")
        Dim enc = {"Piso", "Viga", "Tramo",
                   "As Col Sup I (cm2)", "As Req Sup I (cm2)", "C/D Sup I",
                   "As Col Sup J (cm2)", "As Req Sup J (cm2)", "C/D Sup J",
                   "As Col Inf (cm2)", "As Req Inf (cm2)", "C/D Inf",
                   "Observaciones"}
        EscribirEncabezados(ws, 1, enc)
        Dim fila As Integer = 2

        For Each viga In Vigas
            For Each frame In viga.Frames
                If Not (frame.RefuerzoSuperior.Any() OrElse frame.RefuerzoInferior.Any()) Then Continue For

                Dim revIzq = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Izquierda)
                Dim revCen = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Centro)
                Dim revDer = frame.RevisionFlexion.FirstOrDefault(Function(x) x.Posicion = PosicionTramoViga.Derecha)

                Dim asColSupI As Double = If(revIzq IsNot Nothing, revIzq.ResultadoActual.AsProvSup / 100.0, 0)
                Dim asReqSupI As Double = If(revIzq IsNot Nothing, revIzq.ResultadoActual.AsReqSup / 100.0, 0)
                Dim cdSupI As Double = If(revIzq IsNot Nothing, revIzq.ResultadoActual.RatioSup, -1)

                Dim asColSupJ As Double = If(revDer IsNot Nothing, revDer.ResultadoActual.AsProvSup / 100.0, 0)
                Dim asReqSupJ As Double = If(revDer IsNot Nothing, revDer.ResultadoActual.AsReqSup / 100.0, 0)
                Dim cdSupJ As Double = If(revDer IsNot Nothing, revDer.ResultadoActual.RatioSup, -1)

                Dim asColInf As Double = If(revCen IsNot Nothing, revCen.ResultadoActual.AsProvInf / 100.0, 0)
                Dim asReqInf As Double = If(revCen IsNot Nothing, revCen.ResultadoActual.AsReqInf / 100.0, 0)
                Dim cdInf As Double = If(revCen IsNot Nothing, revCen.ResultadoActual.RatioInf, -1)

                Dim tramoStr As String
                If Not String.IsNullOrWhiteSpace(frame.EjeApoyo_I) OrElse Not String.IsNullOrWhiteSpace(frame.EjeApoyo_J) Then
                    tramoStr = $"{frame.EjeApoyo_I}-{frame.EjeApoyo_J}"
                Else
                    tramoStr = frame.ObjectLabel
                End If

                Dim obs As New List(Of String)
                If cdSupI > 0 AndAlso cdSupI < UMBRAL_CD Then obs.Add("apoyo I por M(-)")
                If cdSupJ > 0 AndAlso cdSupJ < UMBRAL_CD Then obs.Add("apoyo J por M(-)")
                If cdInf > 0 AndAlso cdInf < UMBRAL_CD Then obs.Add("centro por M(+)")
                Dim obsStr = If(obs.Count > 0, "En " & String.Join(" y ", obs), "")

                ws.Cell(fila, 1).Value = viga.Piso
                ws.Cell(fila, 2).Value = NombreReporte(viga)
                ws.Cell(fila, 3).Value = tramoStr
                ws.Cell(fila, 4).Value = If(asColSupI > 0, CObj(Math.Round(asColSupI, 2)), "-")
                ws.Cell(fila, 5).Value = If(asReqSupI > 0, CObj(Math.Round(asReqSupI, 2)), "-")
                If cdSupI > 0 Then EscribirFactor(ws.Cell(fila, 6), cdSupI) Else ws.Cell(fila, 6).Value = "-"
                ws.Cell(fila, 7).Value = If(asColSupJ > 0, CObj(Math.Round(asColSupJ, 2)), "-")
                ws.Cell(fila, 8).Value = If(asReqSupJ > 0, CObj(Math.Round(asReqSupJ, 2)), "-")
                If cdSupJ > 0 Then EscribirFactor(ws.Cell(fila, 9), cdSupJ) Else ws.Cell(fila, 9).Value = "-"
                ws.Cell(fila, 10).Value = If(asColInf > 0, CObj(Math.Round(asColInf, 2)), "-")
                ws.Cell(fila, 11).Value = If(asReqInf > 0, CObj(Math.Round(asReqInf, 2)), "-")
                If cdInf > 0 Then EscribirFactor(ws.Cell(fila, 12), cdInf) Else ws.Cell(fila, 12).Value = "-"
                ws.Cell(fila, 13).Value = obsStr
                ws.Cell(fila, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left

                EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1
            Next
        Next

        AjustarColumnas(ws, enc.Length)
        AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
    End Sub

    ' ── Hoja Cortante ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCortante(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Revisión Cortante")
        Dim enc = {"Piso", "Viga", "Tramo", "Vu (kN)", "φVn (kN)", "C/D", "Estado"}

        ' Bloque 1: Todas las vigas
        ws.Cell(1, 1).Value = "REVISIÓN CORTANTE — TODAS LAS VIGAS"
        With ws.Cell(1, 1).Style
            .Font.Bold = True : .Font.FontSize = 11 : .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XLColor.FromHtml("#3C3C3C")
        End With
        ws.Range(1, 1, 1, enc.Length).Merge()

        EscribirEncabezados(ws, 2, enc)
        Dim fila As Integer = 3

        Dim filasNoCumplen As New List(Of (Piso As String, Viga As String, Tramo As String,
                                           Vu As Double, Vn As Double, Factor As Double))

        For Each viga In Vigas
            For Each frame In viga.Frames
                If Not (frame.RefuerzoSuperior.Any() OrElse frame.RefuerzoInferior.Any()) Then Continue For
                Dim zonaGob = frame.RevisionCortante.Where(Function(z) z.phiVn > 0).
                                                      OrderBy(Function(z) z.Factor).
                                                      FirstOrDefault()
                If zonaGob Is Nothing Then Continue For

                Dim tramoStr As String
                If Not String.IsNullOrWhiteSpace(frame.EjeApoyo_I) OrElse Not String.IsNullOrWhiteSpace(frame.EjeApoyo_J) Then
                    tramoStr = $"{frame.EjeApoyo_I}-{frame.EjeApoyo_J}"
                Else
                    tramoStr = frame.ObjectLabel
                End If

                Dim failReal = frame.RevisionCortante.Any(Function(z)
                    Return z.phiVn > 0 AndAlso z.Factor > 0 AndAlso z.Factor < UMBRAL_CD AndAlso
                           Not CumpleCortantePlastico(z.Posicion, frame.CortantePlastico)
                End Function)

                Dim cumple = Not failReal
                Dim vuShow = zonaGob.Vu : Dim vnShow = zonaGob.phiVn : Dim factorShow = zonaGob.Factor
                Dim etiqExcel As String

                If failReal Then
                    Dim peor = frame.RevisionCortante.Where(Function(z)
                        Return z.phiVn > 0 AndAlso z.Factor > 0 AndAlso z.Factor < UMBRAL_CD AndAlso
                               Not CumpleCortantePlastico(z.Posicion, frame.CortantePlastico)
                    End Function).OrderBy(Function(z) z.Factor).First()
                    vuShow = peor.Vu : vnShow = peor.phiVn : factorShow = peor.Factor
                    etiqExcel = "NO"
                ElseIf zonaGob.Factor < UMBRAL_CD Then
                    etiqExcel = "OK (Plást.)"
                Else
                    etiqExcel = "SI"
                End If

                ws.Cell(fila, 1).Value = viga.Piso
                ws.Cell(fila, 2).Value = NombreReporte(viga)
                ws.Cell(fila, 3).Value = tramoStr
                ws.Cell(fila, 4).Value = Math.Round(vuShow, 2)
                ws.Cell(fila, 5).Value = Math.Round(vnShow, 2)
                EscribirFactor(ws.Cell(fila, 6), factorShow)
                ws.Cell(fila, 7).Value = etiqExcel
                ws.Cell(fila, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                ws.Cell(fila, 7).Style.Font.Bold = True
                ws.Cell(fila, 7).Style.Fill.BackgroundColor = If(cumple, XLOKFondo, XLMalFondo)
                ws.Cell(fila, 7).Style.Font.FontColor = If(cumple, XLOKTexto, XLMalTexto)
                EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1

                If Not cumple Then
                    filasNoCumplen.Add((viga.Piso, NombreReporte(viga), tramoStr,
                                        vuShow, vnShow, factorShow))
                End If
            Next
        Next

        AgregarBordesTabla(ws, 2, fila - 1, enc.Length)
        fila += 1

        ' Bloque 2: Solo las que no cumplen
        ws.Cell(fila, 1).Value = "VIGAS QUE NO CUMPLEN A CORTANTE"
        With ws.Cell(fila, 1).Style
            .Font.Bold = True : .Font.FontSize = 11 : .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XLColor.FromHtml("#9C0006")
        End With
        ws.Range(fila, 1, fila, enc.Length).Merge()
        fila += 1

        EscribirEncabezados(ws, fila, enc)
        fila += 1

        If filasNoCumplen.Count = 0 Then
            ws.Cell(fila, 1).Value = "Todas las vigas cumplen a cortante"
            ws.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C6EFCE")
            ws.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#006100")
            ws.Range(fila, 1, fila, enc.Length).Merge()
        Else
            For Each item In filasNoCumplen
                ws.Cell(fila, 1).Value = item.Piso
                ws.Cell(fila, 2).Value = item.Viga
                ws.Cell(fila, 3).Value = item.Tramo
                ws.Cell(fila, 4).Value = Math.Round(item.Vu, 2)
                ws.Cell(fila, 5).Value = Math.Round(item.Vn, 2)
                EscribirFactor(ws.Cell(fila, 6), item.Factor)
                EscribirCeldaCumple(ws.Cell(fila, 7), False)
                EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)
                fila += 1
            Next
            AgregarBordesTabla(ws, fila - filasNoCumplen.Count - 1, fila - 1, enc.Length)
        End If

        AjustarColumnas(ws, enc.Length)
    End Sub

    ' ── Hoja Completo ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCompleto(wb As XLWorkbook)

        Dim ws = wb.Worksheets.Add("Resumen Completo")

        Dim encabezados = {"Piso", "Eje", "Nombre (plano)", "Frames ETABS", "F M- mín", "F M+ mín", "F Cor Final", "Flexión", "Cortante", "Estado"}
        EscribirEncabezados(ws, 1, encabezados)

        Dim fila As Integer = 2

        For Each viga In Vigas

            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())
            Dim tieneCor = viga.Frames.Any(Function(f) f.RevisionCortante.Any(Function(z) z.phiVn > 0))
            If Not tieneRef Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue
            Dim fConMin As Double = Double.MaxValue
            Dim cumpleFlex As Boolean = True

            For Each frame In viga.Frames
                For Each rev In frame.RevisionFlexion
                    Dim act = rev.ResultadoActual
                    If act.AsReqSup > 0 AndAlso act.RatioSup > 0 Then
                        fNegMin = Math.Min(fNegMin, act.RatioSup)
                        If Not act.CumpleSuperior Then cumpleFlex = False
                    End If
                    If act.AsReqInf > 0 AndAlso act.RatioInf > 0 Then
                        fPosMin = Math.Min(fPosMin, act.RatioInf)
                        If Not act.CumpleInferior Then cumpleFlex = False
                    End If
                Next
                For Each zona In frame.RevisionCortante
                    If zona.phiVn > 0 Then fConMin = Math.Min(fConMin, zona.Factor)
                Next
            Next

            ' Cortante plástico — envolvente
            Dim fPlas As Double = Double.MaxValue
            For Each frame In viga.Frames
                If frame.CortantePlastico Is Nothing Then Continue For
                Dim cp = frame.CortantePlastico
                If cp.ZonaIzq.phiVn > 0 Then fPlas = Math.Min(fPlas, cp.ZonaIzq.Factor)
                If cp.ZonaDer.phiVn > 0 Then fPlas = Math.Min(fPlas, cp.ZonaDer.Factor)
            Next
            Dim tienePlastico = (fPlas < Double.MaxValue)
            Dim fallaCentro As Boolean = FallaZonaCentral(viga)

            Dim cumpleConv = (fConMin <> Double.MaxValue AndAlso fConMin >= UMBRAL_CD)
            ' Ver nota en CargarResumenCompleto: el plástico no cubre la zona Centro.
            Dim cumplePlas = (tienePlastico AndAlso fPlas >= UMBRAL_CD AndAlso Not fallaCentro)
            Dim cumpleCor = cumpleConv OrElse cumplePlas

            Dim fFin As Double
            If cumpleConv Then
                fFin = fConMin
            ElseIf cumplePlas Then
                fFin = fPlas
            ElseIf tienePlastico Then
                fFin = Math.Max(If(fConMin = Double.MaxValue, 0.0, fConMin), fPlas)
            Else
                fFin = fConMin
            End If

            ws.Cell(fila, 1).Value = viga.Piso
            ws.Cell(fila, 2).Value = If(String.IsNullOrWhiteSpace(viga.EjeParalelo), "-", viga.EjeParalelo)
            ws.Cell(fila, 3).Value = NombreReporte(viga)
            ws.Cell(fila, 4).Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            EscribirFactor(ws.Cell(fila, 5), fNegMin)
            EscribirFactor(ws.Cell(fila, 6), fPosMin)
            EscribirFactor(ws.Cell(fila, 7), fFin)

            ' Col 8 — Flexión
            If Not tieneRef Then
                EscribirEstado(ws.Cell(fila, 8), "Sin ref.", XLAlertaFondo, XLAlertaTexto)
            ElseIf cumpleFlex Then
                EscribirEstado(ws.Cell(fila, 8), "OK", XLOKFondo, XLOKTexto)
            Else
                EscribirEstado(ws.Cell(fila, 8), "Revisar", XLMalFondo, XLMalTexto)
            End If

            ' Col 9 — Cortante
            If fFin = Double.MaxValue Then
                EscribirEstado(ws.Cell(fila, 9), "Sin datos", XLAlertaFondo, XLAlertaTexto)
            ElseIf cumpleCor Then
                EscribirEstado(ws.Cell(fila, 9), "OK", XLOKFondo, XLOKTexto)
            Else
                EscribirEstado(ws.Cell(fila, 9), "Revisar", XLMalFondo, XLMalTexto)
            End If

            ' Col 10 — Estado general
            Dim ok = tieneRef AndAlso cumpleFlex AndAlso tieneCor AndAlso cumpleCor
            Dim pendiente = Not tieneRef OrElse Not tieneCor
            If ok Then
                EscribirEstado(ws.Cell(fila, 10), "OK", XLOKFondo, XLOKTexto)
            ElseIf pendiente Then
                EscribirEstado(ws.Cell(fila, 10), "Pendiente", XLAlertaFondo, XLAlertaTexto)
            Else
                EscribirEstado(ws.Cell(fila, 10), "Revisar", XLMalFondo, XLMalTexto)
            End If

            EstilarFilaDatos(ws, fila, encabezados.Length, fila Mod 2 = 1)
            fila += 1

        Next

        AjustarColumnas(ws, encabezados.Length)
        AgregarBordesTabla(ws, 1, fila - 1, encabezados.Length)

    End Sub

    ' =========================================================================
    ' HELPERS CLOSEDXML
    ' =========================================================================

    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, encabezados As String())
        For i As Integer = 0 To encabezados.Length - 1
            Dim cell = ws.Cell(fila, i + 1)
            cell.Value = encabezados(i)
            With cell.Style
                .Fill.BackgroundColor = XlEncabezado
                .Font.FontColor = XLColor.White
                .Font.Bold = True
                .Font.FontSize = 11
                .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                .Alignment.Vertical = XLAlignmentVerticalValues.Center
                .Border.OutsideBorder = XLBorderStyleValues.Thin
                .Border.OutsideBorderColor = XLColor.White
            End With
            ws.Row(fila).Height = 22
        Next
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        If valor = Double.MaxValue Then
            cell.Value = "-"
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            Return
        End If

        Dim v = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center

        If v >= UMBRAL_CD Then
            cell.Style.Fill.BackgroundColor = XLOKFondo
            cell.Style.Font.FontColor = XLOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XLMalFondo
            cell.Style.Font.FontColor = XLMalTexto
        End If
        cell.Style.Font.Bold = True
    End Sub

    Private Sub EscribirCeldaCumple(cell As IXLCell, cumple As Boolean)
        cell.Value = If(cumple, "SI", "NO")
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        cell.Style.Font.Bold = True
        If cumple Then
            cell.Style.Fill.BackgroundColor = XLOKFondo
            cell.Style.Font.FontColor = XLOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XLMalFondo
            cell.Style.Font.FontColor = XLMalTexto
        End If
    End Sub

    Private Sub EscribirEstado(cell As IXLCell, texto As String, fondo As XLColor, textoColor As XLColor)
        cell.Value = texto
        cell.Style.Fill.BackgroundColor = fondo
        cell.Style.Font.FontColor = textoColor
        cell.Style.Font.Bold = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
    End Sub

    Private Sub EstilarFilaDatos(ws As IXLWorksheet, fila As Integer, numCols As Integer, esPar As Boolean)
        Dim row = ws.Row(fila)
        row.Height = 18
        For col = 1 To numCols
            Dim cell = ws.Cell(fila, col)
            If esPar AndAlso cell.Style.Fill.BackgroundColor = XLColor.NoColor Then
                cell.Style.Fill.BackgroundColor = XlFilaPar
            End If
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
            If col <= 3 Then
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            End If
        Next
    End Sub

    Private Sub AgregarBordesTabla(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        If filaFin < filaIni Then Return
        Dim rango = ws.Range(filaIni, 1, filaFin, numCols)
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair
        rango.Style.Border.InsideBorderColor = XLColor.FromHtml("#CCCCCC")
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Medium
        rango.Style.Border.OutsideBorderColor = XlEncabezado
    End Sub

    Private Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer)
        ws.Columns(1, numCols).AdjustToContents()
        ' Mínimo para columnas de Frames ETABS (col 3)
        If ws.Column(3).Width < 25 Then ws.Column(3).Width = 25
        ' Máximo para evitar columnas muy anchas
        For c = 1 To numCols
            If ws.Column(c).Width > 45 Then ws.Column(c).Width = 45
        Next
        ' Fijar hoja: congelar primera fila de encabezados
        ws.SheetView.FreezeRows(1)
    End Sub

    ' =========================================================================
    ' HELPERS UI (DataGridView)
    ' =========================================================================

    Private Sub EstilarGrid(dgv As DataGridView)
        dgv.AllowUserToAddRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.MultiSelect = False
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.RowHeadersVisible = False
        dgv.BackgroundColor = Color.White
        dgv.BorderStyle = BorderStyle.None
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.GridColor = Color.FromArgb(210, 210, 210)
        dgv.ColumnHeadersHeight = 42
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.RowTemplate.Height = 28
        dgv.EnableHeadersVisualStyles = False

        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor = ColorEncabezado
            .ForeColor = ColorEncabezadoTexto
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With

        With dgv.DefaultCellStyle
            .BackColor = Color.White
            .ForeColor = Color.Black
            .SelectionBackColor = Color.FromArgb(200, 225, 255)
            .SelectionForeColor = Color.Black
            .Font = New Font("Segoe UI", 10)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

    Private Sub AgregarColumna(dgv As DataGridView, nombre As String, header As String, ancho As Integer)
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = nombre, .HeaderText = header, .Width = ancho})
    End Sub

    Private Sub AsignarFactorCelda(cell As DataGridViewCell, factor As Double)
        If factor = Double.MaxValue Then
            cell.Value = "-"
            Return
        End If
        Dim v = Math.Round(Math.Min(factor, 9.99), 2)
        cell.Value = v.ToString("F2")
        PintarCeldaFactor(cell, factor)
    End Sub

    Private Sub PintarCeldaFactor(cell As DataGridViewCell, factor As Double)
        If factor >= UMBRAL_CD Then
            cell.Style.BackColor = ColorOK : cell.Style.ForeColor = ColorOKTexto
        Else
            cell.Style.BackColor = ColorMal : cell.Style.ForeColor = ColorMalTexto
        End If
    End Sub

    Private Sub AplicarEstado(cell As DataGridViewCell, texto As String, fondo As Color, textoColor As Color)
        cell.Value = texto
        cell.Style.BackColor = fondo
        cell.Style.ForeColor = textoColor
    End Sub

    Private Function NombreReporte(viga As cViga) As String
        Return If(String.IsNullOrWhiteSpace(viga.NombrePlano), viga.Nombre, viga.NombrePlano)
    End Function

    Private Function PosTexto(pos As PosicionTramoViga) As String
        Select Case pos
            Case PosicionTramoViga.Izquierda : Return "Izq"
            Case PosicionTramoViga.Centro : Return "Cen"
            Case PosicionTramoViga.Derecha : Return "Der"
            Case Else : Return "?"
        End Select
    End Function

End Class
