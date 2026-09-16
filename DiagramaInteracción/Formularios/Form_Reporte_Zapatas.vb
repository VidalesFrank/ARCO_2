Imports System.Drawing
Imports ClosedXML.Excel
Imports ARCO.eNumeradores

''' <summary>
''' Reporte de revisión de zapatas: Resumen + Detalle por combinación + Excel.
'''
''' Qué se corrigió aquí (2026-09-15)
''' ---------------------------------
''' El detalle mostraba columnas llamadas "g1", "g2", "g5 Punz.", "gf Cort."
''' y las pasaba por el helper de factores C/D. Pero esos valores NO son
''' factores: son PRESIONES DE CONTACTO en kN/m2 — la presión del suelo en cada
''' esquina de la zapata (g1..g4), en las esquinas del perímetro de
''' punzonamiento (g5..g8) y en las secciones críticas de cortante y flexión.
''' Tratarlas como C/D tenía dos consecuencias:
'''
'''   * el helper recorta todo factor en 9.99, así que en el Excel cualquier
'''     presión real — del orden de decenas o centenas — salía escrita como
'''     9.99. El dato exportado era falso.
'''   * el semáforo las pintaba verdes siempre, porque cualquier presión supera
'''     el umbral de 0.90. La columna no informaba nada.
'''
''' Y una presión negativa, que es tracción bajo la zapata y hay que verla, se
''' escribía como "-" por la convención de "sin dato" de los factores.
'''
''' Ahora las presiones van como presiones (sin recorte, con los negativos en
''' ámbar) y las columnas de C/D son C/D de verdad, calculadas por
''' ZapataService igual que la vista en planta.
''' </summary>
Public Class Form_Reporte_Zapatas
    Inherits Form

    Private WithEvents DgvResumen As New DataGridView()
    Private WithEvents DgvDetalle As New DataGridView()
    Private _tabs As TabControl
    Private _chkSoloObs As CheckBox
    Private _lblConteo As Label

    Private ReadOnly _proyecto As Proyecto

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_Reporte_Zapatas(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
        CargarTodo()
    End Sub

    ' =========================================================================
    ' Interfaz
    ' =========================================================================
    Private Sub BuildUI()

        Me.Text = "Reporte — Revisión de Zapatas"
        Me.Size = New Size(1400, 720)
        Me.MinimumSize = New Size(900, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        _tabs = New TabControl() With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Padding = New Point(16, 6)
        }

        Dim tabRes As New TabPage("  Resumen  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvResumen.Dock = DockStyle.Fill
        EstilarGrid(DgvResumen)
        tabRes.Controls.Add(DgvResumen)
        _tabs.TabPages.Add(tabRes)

        Dim tabDet As New TabPage("  Detalle por Combinación  ") With {.BackColor = Color.White, .UseVisualStyleBackColor = False}
        DgvDetalle.Dock = DockStyle.Fill
        EstilarGrid(DgvDetalle)
        tabDet.Controls.Add(DgvDetalle)
        _tabs.TabPages.Add(tabDet)

        Me.Controls.Add(_tabs)

        Dim barra As New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 54,
            .BackColor = Color.FromArgb(245, 245, 245),
            .Padding = New Padding(10, 9, 10, 9)
        }

        Dim btnActualizar As New Button() With {
            .Text = "Actualizar", .Size = New Size(120, 36), .Location = New Point(10, 9),
            .FlatStyle = FlatStyle.Flat, .BackColor = ReporteGridHelpers.ColorEncabezado,
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand
        }
        btnActualizar.FlatAppearance.BorderSize = 0
        AddHandler btnActualizar.Click, Sub(s, ev) CargarTodo()
        barra.Controls.Add(btnActualizar)

        Dim btnExportar As New Button() With {
            .Text = "Exportar a Excel", .Size = New Size(160, 36), .Location = New Point(140, 9),
            .FlatStyle = FlatStyle.Flat, .BackColor = Color.FromArgb(21, 130, 70),
            .ForeColor = Color.White, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .Cursor = Cursors.Hand
        }
        btnExportar.FlatAppearance.BorderSize = 0
        AddHandler btnExportar.Click, AddressOf BtnExportar_Click
        barra.Controls.Add(btnExportar)

        ' Con cien zapatas, lo primero que se busca es cuáles fallan.
        _chkSoloObs = New CheckBox() With {
            .Text = "Solo las que no cumplen",
            .Location = New Point(320, 16),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler _chkSoloObs.CheckedChanged, Sub(s, ev) CargarTodo()
        barra.Controls.Add(_chkSoloObs)

        _lblConteo = New Label() With {
            .Location = New Point(540, 18),
            .AutoSize = True,
            .ForeColor = ReporteGridHelpers.ColorEncabezado,
            .Font = New Font("Segoe UI", 9.5!)
        }
        barra.Controls.Add(_lblConteo)

        Me.Controls.Add(barra)

    End Sub

    ' =========================================================================
    ' Carga
    ' =========================================================================
    Private Sub CargarTodo()

        If _proyecto Is Nothing Then Return

        Dim tipos = _proyecto.Elementos.Zapatas.Tipos
        If tipos Is Nothing OrElse tipos.Count = 0 Then Return

        CargarResumen(tipos)
        CargarDetalle(tipos)

    End Sub

    ''' <summary>
    ''' Una zapata tiene observaciones si falla POR CUALQUIERA de los dos
    ''' caminos, o si no se ha calculado.
    '''
    ''' Son dos veredictos distintos y hay que respetar los dos: CumpleGeneral
    ''' es el que emite el propio cálculo del módulo — e incluye revisiones que
    ''' no se reducen a un cociente, como la excentricidad — mientras que el peor
    ''' C/D es una medida de qué tan justa va. Filtrar solo por uno podría
    ''' esconder algo que el otro marcó.
    ''' </summary>
    Private Shared Function TieneObservaciones(z As cZapata, r As ZapataService.ResumenZapata) As Boolean

        If Not r.TieneResultados Then Return True
        If Not r.Cumple Then Return True

        Dim vals = z.Resultados.Values.Where(Function(res) res IsNot Nothing).ToList()
        Return Not vals.All(Function(res) res.CumpleGeneral)

    End Function

    ''' <summary>
    ''' El filtro esconde las que están bien, nunca las que no se han calculado:
    ''' una zapata sin calcular es justamente algo que hay que mirar.
    ''' </summary>
    Private Function Visible(z As cZapata, r As ZapataService.ResumenZapata) As Boolean
        If Not _chkSoloObs.Checked Then Return True
        Return TieneObservaciones(z, r)
    End Function

    ' ── TAB 1: RESUMEN (peor caso entre todas las combinaciones) ─────────────
    Private Sub CargarResumen(tipos As List(Of cZapata))

        Dim dgv = DgvResumen
        dgv.SuspendLayout()
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Nombre", "Zapata", 110)
        Col(dgv, "Joint", "Nodo", 80)
        Col(dgv, "Apoyo", "Tipo de apoyo", 110)
        Col(dgv, "Lb", "L_b [m]", 70)
        Col(dgv, "Lh", "L_h [m]", 70)
        Col(dgv, "e", "e [m]", 60)
        Col(dgv, "b", "b [m]", 60)
        Col(dgv, "h", "h [m]", 60)
        Col(dgv, "fc", "fc [MPa]", 70)
        Col(dgv, "qAdmE", "qAdm Est. [kN/m²]", 100)
        Col(dgv, "qAdmD", "qAdm Din. [kN/m²]", 100)
        Col(dgv, "Capacidad", "Capacidad", 90)
        Col(dgv, "Punz", "Punzonamiento", 110)
        Col(dgv, "Cortante", "Cortante", 90)
        Col(dgv, "Flexion", "Flexión", 80)
        Col(dgv, "PeorCD", "Peor C/D", 80)
        Col(dgv, "Gobierna", "Gobierna", 120)
        Col(dgv, "General", "General", 90)

        Dim mostradas As Integer = 0
        Dim noCumplen As Integer = 0

        For i = 0 To tipos.Count - 1

            Dim z = tipos(i)
            Dim resumen = ZapataService.Resumir(z)

            If TieneObservaciones(z, resumen) Then noCumplen += 1
            If Not Visible(z, resumen) Then Continue For

            Dim row = dgv.Rows(dgv.Rows.Add())
            mostradas += 1

            row.Cells("Nombre").Value = z.Nombre
            row.Cells("Joint").Value = z.Label_joint
            row.Cells("Apoyo").Value = ZapataService.NombreTipo(z.TipoApoyo) &
                                       If(z.TipoApoyoManual, " *", "")
            row.Cells("Apoyo").ToolTipText = If(z.TipoApoyoManual,
                                                "Fijado manualmente por el ingeniero.",
                                                "Propuesto por la posición en planta.")
            ReporteGridHelpers.AsignarValor(row.Cells("Lb"), z.L_b)
            ReporteGridHelpers.AsignarValor(row.Cells("Lh"), z.L_h)
            ReporteGridHelpers.AsignarValor(row.Cells("e"), z.e)
            ReporteGridHelpers.AsignarValor(row.Cells("b"), z.b)
            ReporteGridHelpers.AsignarValor(row.Cells("h"), z.h)
            row.Cells("fc").Value = z.fc
            ReporteGridHelpers.AsignarValor(row.Cells("qAdmE"), z.qAdm_Est)
            ReporteGridHelpers.AsignarValor(row.Cells("qAdmD"), z.qAdm_Din)

            If mostradas Mod 2 = 0 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)

            If Not resumen.TieneResultados Then
                For Each k In {"Capacidad", "Punz", "Cortante", "Flexion", "PeorCD", "Gobierna", "General"}
                    ReporteGridHelpers.AsignarEstado(row.Cells(k), "Sin calcular",
                                                     ReporteGridHelpers.ColorAlerta,
                                                     ReporteGridHelpers.ColorAlertaTexto)
                Next
                Continue For
            End If

            Dim vals = z.Resultados.Values.Where(Function(res) res IsNot Nothing).ToList()
            AsignarOk(row.Cells("Capacidad"), vals.All(Function(res) res.CumpleCapacidad))
            AsignarOk(row.Cells("Punz"), vals.All(Function(res) res.CumplePunzonamiento))
            AsignarOk(row.Cells("Cortante"), vals.All(Function(res) res.CumpleCortante_1 AndAlso res.CumpleCortante_2 AndAlso
                                                                    res.CumpleCortante_3 AndAlso res.CumpleCortante_4))
            AsignarOk(row.Cells("Flexion"), vals.All(Function(res) res.Cumple_L1 AndAlso res.Cumple_L2))

            ReporteGridHelpers.AsignarCD(row.Cells("PeorCD"), resumen.PeorFactor, negrita:=True)
            row.Cells("Gobierna").Value = resumen.Revision
            row.Cells("Gobierna").ToolTipText = $"Combinación {resumen.Combinacion}"

            AsignarOk(row.Cells("General"), vals.All(Function(res) res.CumpleGeneral))

        Next

        dgv.ResumeLayout(True)

        _lblConteo.Text = $"{tipos.Count} zapatas   |   {noCumplen} con observaciones   |   {mostradas} en pantalla"

    End Sub

    ' ── TAB 2: DETALLE POR COMBINACIÓN ───────────────────────────────────────
    Private Sub CargarDetalle(tipos As List(Of cZapata))

        Dim dgv = DgvDetalle
        dgv.SuspendLayout()
        dgv.Columns.Clear() : dgv.Rows.Clear()

        Col(dgv, "Nombre", "Zapata", 110)
        Col(dgv, "Combo", "Combinación", 140)
        Col(dgv, "Tipo", "Tipo", 70)
        Col(dgv, "qMax", "qMáx [kN/m²]", 100)
        Col(dgv, "qMin", "qMín [kN/m²]", 100)
        Col(dgv, "CDSuelo", "C/D suelo", 85)
        Col(dgv, "Cap", "Capacidad", 90)
        Col(dgv, "qPunz", "q media perím. punz. [kN/m²]", 150)
        Col(dgv, "CDPunz", "C/D punz.", 85)
        Col(dgv, "Punz", "Punzonamiento", 110)
        Col(dgv, "qCort", "q sección cortante [kN/m²]", 150)
        Col(dgv, "CDCort", "C/D cortante", 95)
        Col(dgv, "Cort", "Cortante", 90)
        Col(dgv, "qFlex", "q cara pedestal [kN/m²]", 140)
        Col(dgv, "CDFlex", "C/D flexión", 90)
        Col(dgv, "Flex", "Flexión", 80)
        Col(dgv, "Gen", "General", 80)

        Dim fila As Integer = 0

        For Each z In tipos

            If z.Resultados Is Nothing Then Continue For
            If Not Visible(z, ZapataService.Resumir(z)) Then Continue For

            Dim factores = ZapataService.FactoresPorCombinacion(z) _
                                        .ToDictionary(Function(f) f.Combinacion)

            For Each kvp In z.Resultados

                Dim res = kvp.Value
                If res Is Nothing Then Continue For

                Dim f As ZapataService.FactoresCombinacion = Nothing
                factores.TryGetValue(kvp.Key, f)

                Dim row = dgv.Rows(dgv.Rows.Add())

                row.Cells("Nombre").Value = z.Nombre
                row.Cells("Combo").Value = kvp.Key
                row.Cells("Tipo").Value = If(f IsNot Nothing AndAlso f.EsDinamica, "Din.", "Est.")

                ' Presiones: son kN/m2, no factores. Los negativos son tracción
                ' bajo la zapata y van resaltados, no ocultos.
                ReporteGridHelpers.AsignarValor(row.Cells("qMax"), res.qMax, resaltarNegativo:=True)
                ReporteGridHelpers.AsignarValor(row.Cells("qMin"), res.qMin, resaltarNegativo:=True)
                ReporteGridHelpers.AsignarValor(row.Cells("qPunz"), Promedio(res.g5, res.g6, res.g7, res.g8), resaltarNegativo:=True)
                ReporteGridHelpers.AsignarValor(row.Cells("qCort"), MaximoAbs(res.gf_C, res.ga_C, res.gi_C, res.ge_C), resaltarNegativo:=True)
                ReporteGridHelpers.AsignarValor(row.Cells("qFlex"), MaximoAbs(res.gf_F, res.ga_F, res.gi_F, res.ge_F), resaltarNegativo:=True)

                AsignarCD(row.Cells("CDSuelo"), If(f Is Nothing, 0, f.Suelo))
                AsignarCD(row.Cells("CDPunz"), If(f Is Nothing, 0, f.Punzonamiento))
                AsignarCD(row.Cells("CDCort"), If(f Is Nothing, 0, f.Cortante))
                AsignarCD(row.Cells("CDFlex"), If(f Is Nothing, 0, f.Flexion))

                AsignarOk(row.Cells("Cap"), res.CumpleCapacidad)
                AsignarOk(row.Cells("Punz"), res.CumplePunzonamiento)
                AsignarOk(row.Cells("Cort"), res.CumpleCortante_1 AndAlso res.CumpleCortante_2 AndAlso
                                             res.CumpleCortante_3 AndAlso res.CumpleCortante_4)
                AsignarOk(row.Cells("Flex"), res.Cumple_L1 AndAlso res.Cumple_L2)
                AsignarOk(row.Cells("Gen"), res.CumpleGeneral)

                If fila Mod 2 = 1 Then row.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
                fila += 1

            Next
        Next

        dgv.ResumeLayout(True)

    End Sub

    ''' <summary>La presión media en el perímetro de punzonamiento, que es la que descuenta del Vu.</summary>
    Private Shared Function Promedio(ParamArray v As Double()) As Double
        If v Is Nothing OrElse v.Length = 0 Then Return 0
        Return v.Average()
    End Function

    ''' <summary>
    ''' De las cuatro presiones en las secciones críticas se reporta la de mayor
    ''' magnitud: es la que gobierna la revisión, y conserva su signo para que
    ''' una tracción se vea como tracción.
    ''' </summary>
    Private Shared Function MaximoAbs(ParamArray v As Double()) As Double
        If v Is Nothing OrElse v.Length = 0 Then Return 0
        Dim mejor As Double = v(0)
        For Each x In v
            If Math.Abs(x) > Math.Abs(mejor) Then mejor = x
        Next
        Return mejor
    End Function

    ' =========================================================================
    ' Exportar a Excel
    ' =========================================================================
    Private Sub BtnExportar_Click(sender As Object, e As EventArgs)

        If _proyecto Is Nothing OrElse _proyecto.Elementos.Zapatas.Tipos.Count = 0 Then
            MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim dlg As New SaveFileDialog With {
            .Title = "Guardar Reporte de Zapatas",
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = "Reporte_Zapatas_" & DateTime.Now.ToString("yyyyMMdd")
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            Dim tipos = _proyecto.Elementos.Zapatas.Tipos
            Using wb As New XLWorkbook()
                ExportarHojaResumen(wb, tipos)
                ExportarHojaDetalle(wb, tipos)
                wb.SaveAs(dlg.FileName)
            End Using

            Dim abrir = MessageBox.Show("Reporte exportado. ¿Abrir ahora?", "Listo",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If

        Catch ex As Exception
            Logger.Error(ex, "Form_Reporte_Zapatas.BtnExportar_Click")
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    Private Sub ExportarHojaResumen(wb As XLWorkbook, tipos As List(Of cZapata))

        Dim ws = wb.Worksheets.Add("Resumen")
        ws.ShowGridLines = False

        Dim hdrs = {"Zapata", "Nodo", "Tipo de apoyo", "L_b (m)", "L_h (m)", "e (m)", "b (m)", "h (m)",
                    "fc (MPa)", "qAdm Est. (kN/m2)", "qAdm Din. (kN/m2)",
                    "Capacidad", "Punzonamiento", "Cortante", "Flexion",
                    "Peor C/D", "Gobierna", "General"}
        ReporteHelpers.EscribirEncabezados(ws, 1, hdrs, tamanoFuente:=10, altoFila:=20)

        Dim fila As Integer = 2

        For Each z In tipos

            Dim resumen = ZapataService.Resumir(z)
            If Not Visible(z, resumen) Then Continue For

            ws.Cell(fila, 1).Value = z.Nombre
            ws.Cell(fila, 2).Value = z.Label_joint
            ws.Cell(fila, 3).Value = ZapataService.NombreTipo(z.TipoApoyo) & If(z.TipoApoyoManual, " (manual)", "")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 4), z.L_b)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 5), z.L_h)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 6), z.e)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 7), z.b)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 8), z.h)
            ws.Cell(fila, 9).Value = z.fc
            ReporteHelpers.EscribirValor(ws.Cell(fila, 10), z.qAdm_Est)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 11), z.qAdm_Din)

            If resumen.TieneResultados Then
                Dim vals = z.Resultados.Values.Where(Function(r) r IsNot Nothing).ToList()
                EscribirCumple(ws.Cell(fila, 12), vals.All(Function(r) r.CumpleCapacidad))
                EscribirCumple(ws.Cell(fila, 13), vals.All(Function(r) r.CumplePunzonamiento))
                EscribirCumple(ws.Cell(fila, 14), vals.All(Function(r) r.CumpleCortante_1 AndAlso r.CumpleCortante_2 AndAlso
                                                                      r.CumpleCortante_3 AndAlso r.CumpleCortante_4))
                EscribirCumple(ws.Cell(fila, 15), vals.All(Function(r) r.Cumple_L1 AndAlso r.Cumple_L2))
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 16), resumen.PeorFactor, ReporteHelpers.SinDato.CeroOMenor)
                ws.Cell(fila, 17).Value = resumen.Revision
                EscribirCumple(ws.Cell(fila, 18), vals.All(Function(r) r.CumpleGeneral))
            Else
                For c = 12 To 18
                    ReporteHelpers.EscribirEstado(ws.Cell(fila, c), "Sin calcular",
                                                  ReporteHelpers.XlAlertaFondo, ReporteHelpers.XlAlertaTexto)
                Next
            End If

            ReporteHelpers.EstilarFilaDatos(ws, fila, hdrs.Length, fila Mod 2 = 0, columnasIzquierda:=3)
            fila += 1

        Next

        ReporteHelpers.AgregarBordesTabla(ws, 1, fila - 1, hdrs.Length)
        ReporteHelpers.AjustarColumnas(ws, hdrs.Length, anchoMaximo:=22)

    End Sub

    Private Sub ExportarHojaDetalle(wb As XLWorkbook, tipos As List(Of cZapata))

        Dim ws = wb.Worksheets.Add("Detalle Combinaciones")
        ws.ShowGridLines = False

        Dim hdrs = {"Zapata", "Combinacion", "Tipo",
                    "qMax (kN/m2)", "qMin (kN/m2)", "C/D suelo", "Capacidad",
                    "q media perim. punz. (kN/m2)", "C/D punz.", "Punzonamiento",
                    "q seccion cortante (kN/m2)", "C/D cortante", "Cortante",
                    "q cara pedestal (kN/m2)", "C/D flexion", "Flexion", "General"}
        ReporteHelpers.EscribirEncabezados(ws, 1, hdrs, tamanoFuente:=10, altoFila:=20)

        Dim fila As Integer = 2

        For Each z In tipos

            If z.Resultados Is Nothing Then Continue For
            If Not Visible(z, ZapataService.Resumir(z)) Then Continue For

            Dim factores = ZapataService.FactoresPorCombinacion(z) _
                                        .ToDictionary(Function(f) f.Combinacion)

            For Each kvp In z.Resultados

                Dim res = kvp.Value
                If res Is Nothing Then Continue For

                Dim f As ZapataService.FactoresCombinacion = Nothing
                factores.TryGetValue(kvp.Key, f)

                ws.Cell(fila, 1).Value = z.Nombre
                ws.Cell(fila, 2).Value = kvp.Key
                ws.Cell(fila, 3).Value = If(f IsNot Nothing AndAlso f.EsDinamica, "Dinamica", "Estatica")

                ReporteHelpers.EscribirValor(ws.Cell(fila, 4), res.qMax, resaltarNegativo:=True)
                ReporteHelpers.EscribirValor(ws.Cell(fila, 5), res.qMin, resaltarNegativo:=True)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 6), If(f Is Nothing, 0, f.Suelo), ReporteHelpers.SinDato.CeroOMenor)
                EscribirCumple(ws.Cell(fila, 7), res.CumpleCapacidad)

                ReporteHelpers.EscribirValor(ws.Cell(fila, 8), Promedio(res.g5, res.g6, res.g7, res.g8), resaltarNegativo:=True)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 9), If(f Is Nothing, 0, f.Punzonamiento), ReporteHelpers.SinDato.CeroOMenor)
                EscribirCumple(ws.Cell(fila, 10), res.CumplePunzonamiento)

                ReporteHelpers.EscribirValor(ws.Cell(fila, 11), MaximoAbs(res.gf_C, res.ga_C, res.gi_C, res.ge_C), resaltarNegativo:=True)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 12), If(f Is Nothing, 0, f.Cortante), ReporteHelpers.SinDato.CeroOMenor)
                EscribirCumple(ws.Cell(fila, 13), res.CumpleCortante_1 AndAlso res.CumpleCortante_2 AndAlso
                                                  res.CumpleCortante_3 AndAlso res.CumpleCortante_4)

                ReporteHelpers.EscribirValor(ws.Cell(fila, 14), MaximoAbs(res.gf_F, res.ga_F, res.gi_F, res.ge_F), resaltarNegativo:=True)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 15), If(f Is Nothing, 0, f.Flexion), ReporteHelpers.SinDato.CeroOMenor)
                EscribirCumple(ws.Cell(fila, 16), res.Cumple_L1 AndAlso res.Cumple_L2)

                EscribirCumple(ws.Cell(fila, 17), res.CumpleGeneral)

                ReporteHelpers.EstilarFilaDatos(ws, fila, hdrs.Length, fila Mod 2 = 0, columnasIzquierda:=3)
                fila += 1

            Next
        Next

        ReporteHelpers.AgregarBordesTabla(ws, 1, fila - 1, hdrs.Length)
        ReporteHelpers.AjustarColumnas(ws, hdrs.Length, anchoMaximo:=22)

    End Sub

    ' =========================================================================
    ' Helpers locales — solo lo que no está ya en los helpers compartidos
    ' =========================================================================
    Private Sub EstilarGrid(dgv As DataGridView)
        ReporteGridHelpers.EstilarGrid(dgv, ReporteGridHelpers.EstiloGrid.Compacto, altoFila:=26)
    End Sub

    Private Sub Col(dgv As DataGridView, name As String, header As String, width As Integer)
        ReporteGridHelpers.AgregarColumna(dgv, name, header, width, ordenable:=False)
    End Sub

    ''' <summary>Un C/D que vale 0 es una revisión que no aplica, no una que falla.</summary>
    Private Shared Sub AsignarCD(cell As DataGridViewCell, cd As Double)
        ReporteGridHelpers.AsignarCD(cell, cd, guionSinDato:=True)
    End Sub

    Private Shared Sub AsignarOk(cell As DataGridViewCell, cumple As Boolean)
        ReporteGridHelpers.AsignarEstado(cell, cumple, "Cumple", "No cumple", negrita:=True)
    End Sub

    Private Shared Sub EscribirCumple(cell As IXLCell, cumple As Boolean)
        ReporteHelpers.EscribirEstado(cell, cumple, "CUMPLE", "NO CUMPLE")
    End Sub

End Class
