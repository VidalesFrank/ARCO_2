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
    Private ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ColorAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ColorAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    ' Colores ClosedXML equivalentes

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

                ' Alerta visual "cuantía > máxima" en las celdas de As Req.
                Dim rhoMaxSec = VigaService.RhoMaxViga(frame.Section.fc, frame.Section.fy)
                Dim sobreI = revIzq IsNot Nothing AndAlso revIzq.ResultadoActual.SobreRhoMaxSup
                Dim sobreJ = revDer IsNot Nothing AndAlso revDer.ResultadoActual.SobreRhoMaxSup
                Dim sobreC = revCen IsNot Nothing AndAlso revCen.ResultadoActual.SobreRhoMaxInf
                Dim excI = revIzq IsNot Nothing AndAlso revIzq.ResultadoActual.RhoExcesivoSup
                Dim excJ = revDer IsNot Nothing AndAlso revDer.ResultadoActual.RhoExcesivoSup
                Dim excC = revCen IsNot Nothing AndAlso revCen.ResultadoActual.RhoExcesivoInf
                MarcarCeldaNivelCuantia(row.Cells("AsReqSupI"),
                                        If(revIzq IsNot Nothing, revIzq.ResultadoActual.RhoReqSup, 0),
                                        rhoMaxSec, sobreI, excI)
                MarcarCeldaNivelCuantia(row.Cells("AsReqSupJ"),
                                        If(revDer IsNot Nothing, revDer.ResultadoActual.RhoReqSup, 0),
                                        rhoMaxSec, sobreJ, excJ)
                MarcarCeldaNivelCuantia(row.Cells("AsReqInf"),
                                        If(revCen IsNot Nothing, revCen.ResultadoActual.RhoReqInf, 0),
                                        rhoMaxSec, sobreC, excC)
                Dim zonasAlerta As New List(Of String)
                If excI Then
                    zonasAlerta.Add("apoyo I (excesivo)")
                ElseIf sobreI Then
                    zonasAlerta.Add("apoyo I")
                End If
                If excJ Then
                    zonasAlerta.Add("apoyo J (excesivo)")
                ElseIf sobreJ Then
                    zonasAlerta.Add("apoyo J")
                End If
                If excC Then
                    zonasAlerta.Add("centro (excesivo)")
                ElseIf sobreC Then
                    zonasAlerta.Add("centro")
                End If
                If zonasAlerta.Count > 0 Then
                    Dim extra = "ρ > ρ_max en " & String.Join(", ", zonasAlerta)
                    row.Cells("Obs").Value = If(String.IsNullOrEmpty(obsStr), extra, obsStr & ". " & extra)
                End If

                row.Cells("Obs").Style.Alignment = DataGridViewContentAlignment.MiddleLeft
                idx += 1
            Next
        Next
    End Sub

    ''' <summary>
    ''' Marca de cuantia por nivel:
    '''   rho excesivo   -> rojo intenso (NSR-10 C.21.5.2.1)
    '''   rho sobre max  -> amarillo (NSR-10 C.10.3.5)
    '''   caso normal    -> sin alerta
    ''' El tooltip incluye los valores numericos concretos.
    ''' </summary>
    Private Sub MarcarCeldaNivelCuantia(cell As DataGridViewCell, rhoReq As Double, rhoMax As Double,
                                        sobreMax As Boolean, excesivo As Boolean)
        If excesivo Then
            cell.Style.BackColor = ColorTranslator.FromHtml("#C00000")
            cell.Style.ForeColor = ColorTranslator.FromHtml("#FFFFFF")
            cell.ToolTipText = $"Cuantía excesiva (NSR-10 C.21.5.2.1)." & vbCrLf &
                               $"ρ requerido = {rhoReq * 100:F2} %  >  2.50 %." & vbCrLf &
                               $"ρ_max NSR-10 C.10.3.5 = {rhoMax * 100:F2} %."
        ElseIf sobreMax Then
            cell.Style.BackColor = ColorAlerta
            cell.Style.ForeColor = ColorAlertaTexto
            cell.ToolTipText = $"Cuantía mayor a la máxima (NSR-10 C.10.3.5)." & vbCrLf &
                               $"ρ requerido = {rhoReq * 100:F2} %  >  ρ_max = {rhoMax * 100:F2} %."
        End If
    End Sub

    ' ── REVISIÓN CORTANTE (por frame, zona gobernante, todas las vigas) ────────

    Private Sub CargarResumenCortante()
        For Each dgv In {DgvCortanteTodas, DgvCortanteNoCumple}
            dgv.Columns.Clear()
            dgv.Rows.Clear()
            AgregarColumna(dgv, "Piso", "Piso", 60)
            AgregarColumna(dgv, "Viga", "Viga", 105)
            AgregarColumna(dgv, "Tramo", "Tramo", 80)
            AgregarColumna(dgv, "Zona", "Zona", 55)
            AgregarColumna(dgv, "VuT", "Vu (kN)", 82)
            AgregarColumna(dgv, "Vn", "φVn (kN)", 82)
            AgregarColumna(dgv, "CDT", "C/D", 70)
            AgregarColumna(dgv, "VuP", "Vu Plást. (kN)", 100)
            AgregarColumna(dgv, "CDP", "C/D Plást.", 82)
            AgregarColumna(dgv, "CDDef", "C/D (Def)", 82)
            AgregarColumna(dgv, "Estado", "Estado", 90)
        Next

        Dim idxT As Integer = 0
        Dim idxN As Integer = 0

        For Each viga In Vigas
            For Each frame In viga.Frames
                If Not (frame.RefuerzoSuperior.Any() OrElse frame.RefuerzoInferior.Any()) Then Continue For

                Dim eval = VigaService.EvaluarCDDefFrame(frame, UMBRAL_CD)
                If eval.Estado = VigaService.EstadoEnvolventeCortante.SinDatos Then Continue For

                Dim tramoStr = TramoLabel(frame)
                Dim zona = eval.ZonaGobernante
                Dim cumple = (eval.Estado <> VigaService.EstadoEnvolventeCortante.NoCumple)
                Dim etiqueta = EtiquetaEstado(eval.Estado)

                AgregarFilaCortanteFrame(DgvCortanteTodas, idxT, viga.Piso, NombreReporte(viga),
                                         tramoStr, zona, cumple, etiqueta)
                idxT += 1
                If Not cumple Then
                    AgregarFilaCortanteFrame(DgvCortanteNoCumple, idxN, viga.Piso, NombreReporte(viga),
                                             tramoStr, zona, cumple, etiqueta)
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

    Private Shared Function TramoLabel(frame As cFrame) As String
        If Not String.IsNullOrWhiteSpace(frame.EjeApoyo_I) OrElse Not String.IsNullOrWhiteSpace(frame.EjeApoyo_J) Then
            Return $"{frame.EjeApoyo_I}-{frame.EjeApoyo_J}"
        End If
        Return frame.ObjectLabel
    End Function

    Private Shared Function EtiquetaEstado(e As VigaService.EstadoEnvolventeCortante) As String
        Select Case e
            Case VigaService.EstadoEnvolventeCortante.Cumple : Return "OK"
            Case VigaService.EstadoEnvolventeCortante.CumplePlastico : Return "OK (Plást.)"
            Case VigaService.EstadoEnvolventeCortante.NoCumple : Return "Revisar"
            Case Else : Return "Sin datos"
        End Select
    End Function

Private Sub AgregarFilaCortanteFrame(dgv As DataGridView, idx As Integer,
                                          piso As String, viga As String, tramo As String,
                                          zona As VigaService.ResultadoCDDefZona,
                                          cumple As Boolean, etiqueta As String)
        Dim r = dgv.Rows.Add()
        Dim row = dgv.Rows(r)
        If idx Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
        row.Cells("Piso").Value = piso
        row.Cells("Viga").Value = viga
        row.Cells("Tramo").Value = tramo
        row.Cells("Zona").Value = VigaService.EtiquetaZona(zona.Posicion)
        row.Cells("VuT").Value = Math.Round(zona.Vu_Tipico, 2).ToString("F2")
        row.Cells("Vn").Value = Math.Round(zona.phiVn_Tipico, 2).ToString("F2")
        AsignarFactorCelda(row.Cells("CDT"), If(zona.CD_Tipico > 0, zona.CD_Tipico, Double.MaxValue))
        If zona.TienePlastico Then
            row.Cells("VuP").Value = Math.Round(zona.Vu_Plastico, 2).ToString("F2")
            AsignarFactorCelda(row.Cells("CDP"), zona.CD_Plastico)
        Else
            row.Cells("VuP").Value = "—"
            row.Cells("CDP").Value = "—"
        End If
        AsignarFactorCelda(row.Cells("CDDef"), zona.CD_Def)
        row.Cells("Estado").Value = etiqueta
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
        AgregarColumna(dgv, "FCor", "C/D (Def) Cor", 110)
        AgregarColumna(dgv, "EstFlex", "Flexión", 90)
        AgregarColumna(dgv, "EstCor", "Cortante", 100)
        AgregarColumna(dgv, "Estado", "Estado", 90)

        Dim idx As Integer = 0

        For Each viga In Vigas

            ' Solo vigas con refuerzo longitudinal colocado (implica que fue revisada)
            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())
            Dim tieneCor = viga.Frames.Any(Function(f) f.RevisionCortante.Any(Function(z) z.phiVn > 0))
            If Not tieneRef Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue
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
            Next

            ' Cortante: envolvente C/D (Def) zona-a-zona, unificada en VigaService.
            Dim evalCor = VigaService.EvaluarCDDefViga(viga, UMBRAL_CD)
            Dim cumpleCor = (evalCor.Estado = VigaService.EstadoEnvolventeCortante.Cumple OrElse
                             evalCor.Estado = VigaService.EstadoEnvolventeCortante.CumplePlastico)
            Dim fFin = If(evalCor.Estado = VigaService.EstadoEnvolventeCortante.SinDatos,
                          Double.MaxValue, evalCor.CD_Def)

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

            ' Cortante (etiqueta según envolvente unificada)
            Select Case evalCor.Estado
                Case VigaService.EstadoEnvolventeCortante.SinDatos
                    AplicarEstado(row.Cells("EstCor"), "Sin datos", ColorAlerta, ColorAlertaTexto)
                Case VigaService.EstadoEnvolventeCortante.Cumple
                    AplicarEstado(row.Cells("EstCor"), "OK", ColorOK, ColorOKTexto)
                Case VigaService.EstadoEnvolventeCortante.CumplePlastico
                    AplicarEstado(row.Cells("EstCor"), "OK (Plást.)", ColorOK, ColorOKTexto)
                Case Else
                    AplicarEstado(row.Cells("EstCor"), "Revisar", ColorMal, ColorMalTexto)
            End Select

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

                Dim rhoMaxSec = VigaService.RhoMaxViga(frame.Section.fc, frame.Section.fy)
                Dim sobreI = revIzq IsNot Nothing AndAlso revIzq.ResultadoActual.SobreRhoMaxSup
                Dim sobreJ = revDer IsNot Nothing AndAlso revDer.ResultadoActual.SobreRhoMaxSup
                Dim sobreC = revCen IsNot Nothing AndAlso revCen.ResultadoActual.SobreRhoMaxInf
                Dim excI = revIzq IsNot Nothing AndAlso revIzq.ResultadoActual.RhoExcesivoSup
                Dim excJ = revDer IsNot Nothing AndAlso revDer.ResultadoActual.RhoExcesivoSup
                Dim excC = revCen IsNot Nothing AndAlso revCen.ResultadoActual.RhoExcesivoInf
                Dim zonasAlerta As New List(Of String)
                If excI Then
                    zonasAlerta.Add("apoyo I (excesivo)")
                ElseIf sobreI Then
                    zonasAlerta.Add("apoyo I")
                End If
                If excJ Then
                    zonasAlerta.Add("apoyo J (excesivo)")
                ElseIf sobreJ Then
                    zonasAlerta.Add("apoyo J")
                End If
                If excC Then
                    zonasAlerta.Add("centro (excesivo)")
                ElseIf sobreC Then
                    zonasAlerta.Add("centro")
                End If
                If zonasAlerta.Count > 0 Then
                    obs.Add("ρ > ρ_max en " & String.Join(", ", zonasAlerta))
                End If
                Dim obsStr = If(obs.Count > 0, String.Join(". ", obs), "")

                ws.Cell(fila, 1).Value = viga.Piso
                ws.Cell(fila, 2).Value = NombreReporte(viga)
                ws.Cell(fila, 3).Value = tramoStr
                ' ClosedXML 0.100+: .Value es XLCellValue, no acepta Object.
                ' If(cond, CObj(Double), "-") devuelve Object → InvalidCastException al asignar.
                If asColSupI > 0 Then ws.Cell(fila, 4).Value = Math.Round(asColSupI, 2) Else ws.Cell(fila, 4).Value = "-"
                If asReqSupI > 0 Then ws.Cell(fila, 5).Value = Math.Round(asReqSupI, 2) Else ws.Cell(fila, 5).Value = "-"
                If cdSupI > 0 Then EscribirFactor(ws.Cell(fila, 6), cdSupI) Else ws.Cell(fila, 6).Value = "-"
                If asColSupJ > 0 Then ws.Cell(fila, 7).Value = Math.Round(asColSupJ, 2) Else ws.Cell(fila, 7).Value = "-"
                If asReqSupJ > 0 Then ws.Cell(fila, 8).Value = Math.Round(asReqSupJ, 2) Else ws.Cell(fila, 8).Value = "-"
                If cdSupJ > 0 Then EscribirFactor(ws.Cell(fila, 9), cdSupJ) Else ws.Cell(fila, 9).Value = "-"
                If asColInf > 0 Then ws.Cell(fila, 10).Value = Math.Round(asColInf, 2) Else ws.Cell(fila, 10).Value = "-"
                If asReqInf > 0 Then ws.Cell(fila, 11).Value = Math.Round(asReqInf, 2) Else ws.Cell(fila, 11).Value = "-"
                If cdInf > 0 Then EscribirFactor(ws.Cell(fila, 12), cdInf) Else ws.Cell(fila, 12).Value = "-"
                ws.Cell(fila, 13).Value = obsStr
                ws.Cell(fila, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left

                EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1)

                ' Alerta "cuantía > máxima" — se aplica DESPUÉS del zebrado para no perderla.
                MarcarCeldaExcelNivelCuantia(ws.Cell(fila, 5),
                                             If(revIzq IsNot Nothing, revIzq.ResultadoActual.RhoReqSup, 0),
                                             rhoMaxSec, sobreI, excI)
                MarcarCeldaExcelNivelCuantia(ws.Cell(fila, 8),
                                             If(revDer IsNot Nothing, revDer.ResultadoActual.RhoReqSup, 0),
                                             rhoMaxSec, sobreJ, excJ)
                MarcarCeldaExcelNivelCuantia(ws.Cell(fila, 11),
                                             If(revCen IsNot Nothing, revCen.ResultadoActual.RhoReqInf, 0),
                                             rhoMaxSec, sobreC, excC)

                fila += 1
            Next
        Next

        AjustarColumnas(ws, enc.Length)
        AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
    End Sub

    ''' <summary>
    ''' Marca en Excel escalada por nivel de cuantia:
    '''   rho excesivo   -> rojo intenso + comentario (NSR-10 C.21.5.2.1)
    '''   rho sobre max  -> amarillo + comentario (NSR-10 C.10.3.5)
    '''   caso normal    -> sin marca
    ''' </summary>
    Private Sub MarcarCeldaExcelNivelCuantia(cell As IXLCell, rhoReq As Double, rhoMax As Double,
                                             sobreMax As Boolean, excesivo As Boolean)
        If excesivo Then
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C00000")
            cell.Style.Font.FontColor = XLColor.FromHtml("#FFFFFF")
            cell.Style.Font.Bold = True
            cell.CreateComment().AddText(
                $"Cuantía excesiva (NSR-10 C.21.5.2.1)." & vbCrLf &
                $"ρ requerido = {rhoReq * 100:F2} %  >  2.50 %." & vbCrLf &
                $"ρ_max NSR-10 C.10.3.5 = {rhoMax * 100:F2} %.")
        ElseIf sobreMax Then
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEB9C")
            cell.Style.Font.FontColor = XLColor.FromHtml("#9C5700")
            cell.Style.Font.Bold = True
            cell.CreateComment().AddText(
                $"Cuantía mayor a la máxima (NSR-10 C.10.3.5)." & vbCrLf &
                $"ρ requerido = {rhoReq * 100:F2} %  >  ρ_max = {rhoMax * 100:F2} %.")
        End If
    End Sub

    ' ── Hoja Cortante ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCortante(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Revisión Cortante")
        Dim enc = {"Piso", "Viga", "Tramo", "Zona",
                   "Vu (kN)", "φVn (kN)", "C/D",
                   "Vu Plást. (kN)", "C/D Plást.", "C/D (Def)", "Estado"}

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
                                           Zona As VigaService.ResultadoCDDefZona, Etiqueta As String))

        For Each viga In Vigas
            For Each frame In viga.Frames
                If Not (frame.RefuerzoSuperior.Any() OrElse frame.RefuerzoInferior.Any()) Then Continue For

                Dim eval = VigaService.EvaluarCDDefFrame(frame, UMBRAL_CD)
                If eval.Estado = VigaService.EstadoEnvolventeCortante.SinDatos Then Continue For

                Dim tramoStr = TramoLabel(frame)
                Dim zona = eval.ZonaGobernante
                Dim cumple = (eval.Estado <> VigaService.EstadoEnvolventeCortante.NoCumple)
                Dim etiqExcel = EtiquetaExcelCumple(eval.Estado)

                EscribirFilaCortanteExcel(ws, fila, viga.Piso, NombreReporte(viga), tramoStr, zona, cumple, etiqExcel, enc.Length)
                fila += 1

                If Not cumple Then
                    filasNoCumplen.Add((viga.Piso, NombreReporte(viga), tramoStr, zona, etiqExcel))
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
                EscribirFilaCortanteExcel(ws, fila, item.Piso, item.Viga, item.Tramo, item.Zona, False, item.Etiqueta, enc.Length)
                fila += 1
            Next
            AgregarBordesTabla(ws, fila - filasNoCumplen.Count - 1, fila - 1, enc.Length)
        End If

        AjustarColumnas(ws, enc.Length)
    End Sub

    Private Shared Function EtiquetaExcelCumple(e As VigaService.EstadoEnvolventeCortante) As String
        Select Case e
            Case VigaService.EstadoEnvolventeCortante.Cumple : Return "SI"
            Case VigaService.EstadoEnvolventeCortante.CumplePlastico : Return "OK (Plást.)"
            Case VigaService.EstadoEnvolventeCortante.NoCumple : Return "NO"
            Case Else : Return "-"
        End Select
    End Function

    Private Sub EscribirFilaCortanteExcel(ws As IXLWorksheet, fila As Integer,
                                          piso As String, viga As String, tramo As String,
                                          zona As VigaService.ResultadoCDDefZona,
                                          cumple As Boolean, etiqueta As String, numCols As Integer)
        ws.Cell(fila, 1).Value = piso
        ws.Cell(fila, 2).Value = viga
        ws.Cell(fila, 3).Value = tramo
        ws.Cell(fila, 4).Value = VigaService.EtiquetaZona(zona.Posicion)
        ws.Cell(fila, 5).Value = Math.Round(zona.Vu_Tipico, 2)
        ws.Cell(fila, 6).Value = Math.Round(zona.phiVn_Tipico, 2)
        EscribirFactor(ws.Cell(fila, 7), If(zona.CD_Tipico > 0, zona.CD_Tipico, Double.MaxValue))
        If zona.TienePlastico Then
            ws.Cell(fila, 8).Value = Math.Round(zona.Vu_Plastico, 2)
            EscribirFactor(ws.Cell(fila, 9), zona.CD_Plastico)
        Else
            ws.Cell(fila, 8).Value = "-"
            ws.Cell(fila, 9).Value = "-"
        End If
        EscribirFactor(ws.Cell(fila, 10), zona.CD_Def)
        ws.Cell(fila, 11).Value = etiqueta
        With ws.Cell(fila, 11).Style
            .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            .Font.Bold = True
            .Fill.BackgroundColor = If(cumple, ReporteHelpers.XlOKFondo, ReporteHelpers.XlMalFondo)
            .Font.FontColor = If(cumple, ReporteHelpers.XlOKTexto, ReporteHelpers.XlMalTexto)
        End With
        EstilarFilaDatos(ws, fila, numCols, fila Mod 2 = 1)
    End Sub

    ' ── Hoja Completo ─────────────────────────────────────────────────────────

    Private Sub ExportarHojaCompleto(wb As XLWorkbook)

        Dim ws = wb.Worksheets.Add("Resumen Completo")

        Dim encabezados = {"Piso", "Eje", "Nombre (plano)", "Frames ETABS", "F M- mín", "F M+ mín", "C/D (Def) Cor", "Flexión", "Cortante", "Estado"}
        EscribirEncabezados(ws, 1, encabezados)

        Dim fila As Integer = 2

        For Each viga In Vigas

            Dim tieneRef = viga.Frames.Any(Function(f) f.RefuerzoSuperior.Any() OrElse f.RefuerzoInferior.Any())
            Dim tieneCor = viga.Frames.Any(Function(f) f.RevisionCortante.Any(Function(z) z.phiVn > 0))
            If Not tieneRef Then Continue For

            Dim fNegMin As Double = Double.MaxValue
            Dim fPosMin As Double = Double.MaxValue
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
            Next

            ' Cortante: envolvente C/D (Def) zona-a-zona, unificada en VigaService.
            Dim evalCor = VigaService.EvaluarCDDefViga(viga, UMBRAL_CD)
            Dim cumpleCor = (evalCor.Estado = VigaService.EstadoEnvolventeCortante.Cumple OrElse
                             evalCor.Estado = VigaService.EstadoEnvolventeCortante.CumplePlastico)
            Dim fFin = If(evalCor.Estado = VigaService.EstadoEnvolventeCortante.SinDatos,
                          Double.MaxValue, evalCor.CD_Def)

            ws.Cell(fila, 1).Value = viga.Piso
            ws.Cell(fila, 2).Value = If(String.IsNullOrWhiteSpace(viga.EjeParalelo), "-", viga.EjeParalelo)
            ws.Cell(fila, 3).Value = NombreReporte(viga)
            ws.Cell(fila, 4).Value = String.Join(", ", viga.Frames.Select(Function(f) f.ObjectLabel))

            EscribirFactor(ws.Cell(fila, 5), fNegMin)
            EscribirFactor(ws.Cell(fila, 6), fPosMin)
            EscribirFactor(ws.Cell(fila, 7), fFin)

            ' Col 8 — Flexión
            If Not tieneRef Then
                EscribirEstado(ws.Cell(fila, 8), "Sin ref.", ReporteHelpers.XlAlertaFondo, ReporteHelpers.XlAlertaTexto)
            ElseIf cumpleFlex Then
                EscribirEstado(ws.Cell(fila, 8), "OK", ReporteHelpers.XlOKFondo, ReporteHelpers.XlOKTexto)
            Else
                EscribirEstado(ws.Cell(fila, 8), "Revisar", ReporteHelpers.XlMalFondo, ReporteHelpers.XlMalTexto)
            End If

            ' Col 9 — Cortante (etiqueta según envolvente unificada)
            Select Case evalCor.Estado
                Case VigaService.EstadoEnvolventeCortante.SinDatos
                    EscribirEstado(ws.Cell(fila, 9), "Sin datos", ReporteHelpers.XlAlertaFondo, ReporteHelpers.XlAlertaTexto)
                Case VigaService.EstadoEnvolventeCortante.Cumple
                    EscribirEstado(ws.Cell(fila, 9), "OK", ReporteHelpers.XlOKFondo, ReporteHelpers.XlOKTexto)
                Case VigaService.EstadoEnvolventeCortante.CumplePlastico
                    EscribirEstado(ws.Cell(fila, 9), "OK (Plást.)", ReporteHelpers.XlOKFondo, ReporteHelpers.XlOKTexto)
                Case Else
                    EscribirEstado(ws.Cell(fila, 9), "Revisar", ReporteHelpers.XlMalFondo, ReporteHelpers.XlMalTexto)
            End Select

            ' Col 10 — Estado general
            Dim ok = tieneRef AndAlso cumpleFlex AndAlso tieneCor AndAlso cumpleCor
            Dim pendiente = Not tieneRef OrElse Not tieneCor
            If ok Then
                EscribirEstado(ws.Cell(fila, 10), "OK", ReporteHelpers.XlOKFondo, ReporteHelpers.XlOKTexto)
            ElseIf pendiente Then
                EscribirEstado(ws.Cell(fila, 10), "Pendiente", ReporteHelpers.XlAlertaFondo, ReporteHelpers.XlAlertaTexto)
            Else
                EscribirEstado(ws.Cell(fila, 10), "Revisar", ReporteHelpers.XlMalFondo, ReporteHelpers.XlMalTexto)
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
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirEncabezados(ws, fila, encabezados)
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirFactor(cell, valor, ReporteHelpers.SinDato.MaxValue, UMBRAL_CD)
    End Sub

    Private Sub EscribirCeldaCumple(cell As IXLCell, cumple As Boolean)
        cell.Value = If(cumple, "SI", "NO")
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        cell.Style.Font.Bold = True
        If cumple Then
            cell.Style.Fill.BackgroundColor = ReporteHelpers.XlOKFondo
            cell.Style.Font.FontColor = ReporteHelpers.XlOKTexto
        Else
            cell.Style.Fill.BackgroundColor = ReporteHelpers.XlMalFondo
            cell.Style.Font.FontColor = ReporteHelpers.XlMalTexto
        End If
    End Sub

    Private Sub EscribirEstado(cell As IXLCell, texto As String, fondo As XLColor, textoColor As XLColor)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.EscribirEstado(cell, texto, fondo, textoColor)
    End Sub

    Private Sub EstilarFilaDatos(ws As IXLWorksheet, fila As Integer, numCols As Integer, esPar As Boolean)
        ' Delega en los helpers compartidos de reporte.
        ReporteHelpers.EstilarFilaDatos(ws, fila, numCols, esPar, columnasIzquierda:=3)
    End Sub

    Private Sub AgregarBordesTabla(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.AgregarBordesTabla(ws, filaIni, filaFin, numCols)
    End Sub

    Private Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer)
        ' Delega en ReporteHelpers: el cuerpo estaba duplicado en varios reportes.
        ReporteHelpers.AjustarColumnas(ws, numCols, columnaAncha:=3, anchoMinimo:=25)
    End Sub

    ' =========================================================================
    ' HELPERS UI (DataGridView)
    ' =========================================================================

    Private Sub EstilarGrid(dgv As DataGridView)
        ' Delega en los helpers compartidos de reporte.
        ReporteGridHelpers.EstilarGrid(dgv)
    End Sub

    Private Sub AgregarColumna(dgv As DataGridView, nombre As String, header As String, ancho As Integer)
        ' Delega en los helpers compartidos de reporte.
        ReporteGridHelpers.AgregarColumna(dgv, nombre, header, ancho)
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
        ' Delega en los helpers compartidos de reporte.
        ReporteGridHelpers.AsignarEstado(cell, texto, fondo, textoColor)
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
