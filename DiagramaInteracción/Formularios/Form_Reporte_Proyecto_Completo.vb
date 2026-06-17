Imports ClosedXML.Excel
Imports System.IO

''' <summary>
''' Reporte completo del proyecto en Excel: Portada + Columnas + Vigas + Muros.
''' </summary>
Public Class Form_Reporte_Proyecto_Completo
    Inherits Form

    ' ── Paleta ClosedXML ──────────────────────────────────────────────────────
    Private ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlSubEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private ReadOnly XlAlertaFondo As XLColor = XLColor.FromHtml("#FFEB9C")
    Private ReadOnly XlAlertaTexto As XLColor = XLColor.FromHtml("#9C5700")
    Private ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F0F4FA")

    ' ── Referencia al proyecto ────────────────────────────────────────────────
    Private ReadOnly _proyecto As Proyecto

    ' ── Controles ────────────────────────────────────────────────────────────
    Private _lblEstado As Label

    ' =========================================================================
    Public Shared Sub Mostrar(proyecto As Proyecto)
        Using frm As New Form_Reporte_Proyecto_Completo(proyecto)
            frm.ShowDialog()
        End Using
    End Sub

    Public Sub New(proyecto As Proyecto)
        _proyecto = proyecto
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Me.Text = "Reporte Completo del Proyecto"
        Me.Size = New Size(560, 420)
        Me.MinimumSize = New Size(500, 360)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.Font = New Font("Segoe UI", 9)
        Me.BackColor = Color.FromArgb(245, 245, 248)

        ' ── Encabezado ───────────────────────────────────────────────────────
        Dim panelTop As New Panel With {.Dock = DockStyle.Top, .Height = 60, .BackColor = Color.FromArgb(87, 87, 87)}
        Dim lblTit As New Label With {
            .Text = "  Reporte Completo — Proyecto ARCO",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 12, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblTit)

        ' ── Panel central ────────────────────────────────────────────────────
        Dim panelInfo As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}
        panelInfo.BackColor = Color.White

        Dim nombreProyecto As String = "(sin nombre)"
        If _proyecto IsNot Nothing AndAlso _proyecto.Info IsNot Nothing AndAlso
           Not String.IsNullOrWhiteSpace(_proyecto.Info.Nombre) Then
            nombreProyecto = _proyecto.Info.Nombre
        End If

        Dim lblProyecto As New Label With {
            .Text = "Proyecto: " & nombreProyecto,
            .Font = New Font("Segoe UI Semibold", 10, FontStyle.Bold),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .Location = New Point(16, 12),
            .AutoSize = True
        }

        Dim lblDetalle As New Label With {
            .Text = ResumenModulos(),
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.FromArgb(60, 60, 60),
            .Location = New Point(16, 38),
            .Size = New Size(500, 140),
            .BackColor = Color.Transparent
        }

        _lblEstado = New Label With {
            .Text = "",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.FromArgb(87, 87, 87),
            .Location = New Point(16, 185),
            .AutoSize = True,
            .BackColor = Color.Transparent
        }

        panelInfo.Controls.AddRange({lblProyecto, lblDetalle, _lblEstado})

        ' ── Botones inferiores ────────────────────────────────────────────────
        Dim panelBot As New Panel With {.Dock = DockStyle.Bottom, .Height = 52, .BackColor = Color.FromArgb(240, 240, 244)}
        Dim btnExportar As New Button With {
            .Text = "  Exportar a Excel",
            .Size = New Size(150, 32),
            .Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(87, 87, 87),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnExportar.FlatAppearance.BorderSize = 0
        Dim btnCerrar As New Button With {
            .Text = "Cerrar",
            .Size = New Size(80, 32),
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.Cancel
        }
        AddHandler panelBot.Resize, Sub(s, e)
                                        btnCerrar.Location = New Point(panelBot.Width - 96, 10)
                                        btnExportar.Location = New Point(panelBot.Width - 258, 10)
                                    End Sub
        AddHandler btnExportar.Click, AddressOf Exportar_Click
        panelBot.Controls.AddRange({btnExportar, btnCerrar})
        Me.CancelButton = btnCerrar

        Me.Controls.Add(panelInfo)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBot)
    End Sub

    ' =========================================================================
    ' Resumen de módulos disponibles
    ' =========================================================================
    Private Function ResumenModulos() As String
        Dim sb As New System.Text.StringBuilder

        Dim nCols As Integer = 0
        Dim nColsCal As Integer = 0
        If _proyecto IsNot Nothing AndAlso _proyecto.Elementos.Columnas.Lista_Columnas IsNot Nothing Then
            nCols = _proyecto.Elementos.Columnas.Lista_Columnas.Count
            nColsCal = _proyecto.Elementos.Columnas.Lista_Columnas.Where(Function(c) c.Ref_Modificado).Count()
        End If
        sb.AppendLine("  • Columnas :  " & nCols & " elementos importados, " & nColsCal & " calculados")

        Dim nVigas As Integer = 0
        If _proyecto IsNot Nothing AndAlso _proyecto.Elementos.Vigas.Vigas IsNot Nothing Then
            nVigas = _proyecto.Elementos.Vigas.Vigas.Count
        End If
        sb.AppendLine("  • Vigas    :  " & nVigas & " vigas importadas / generadas")

        Dim nMuros As Integer = 0
        Dim nMurosCal As Integer = 0
        If _proyecto IsNot Nothing AndAlso _proyecto.Elementos.Muros.Lista_Muros IsNot Nothing Then
            nMuros = _proyecto.Elementos.Muros.Lista_Muros.Count
            nMurosCal = _proyecto.Elementos.Muros.Lista_Muros.Where(Function(m) m.Ref_Modificado_Muros).Count()
        End If
        sb.AppendLine("  • Muros    :  " & nMuros & " muros importados, " & nMurosCal & " calculados")

        Dim nPilas As Integer = 0
        If _proyecto IsNot Nothing AndAlso _proyecto.Elementos.Pilas.ListaElementos IsNot Nothing Then
            nPilas = _proyecto.Elementos.Pilas.ListaElementos.Count
        End If
        If nPilas > 0 Then sb.AppendLine("  • Pilas    :  " & nPilas & " pilas calculadas")

        Dim nZapatas As Integer = 0
        If _proyecto IsNot Nothing AndAlso _proyecto.Elementos.Zapatas.Tipos IsNot Nothing Then
            nZapatas = _proyecto.Elementos.Zapatas.Tipos.Count
        End If
        If nZapatas > 0 Then sb.AppendLine("  • Zapatas  :  " & nZapatas & " zapatas definidas")

        sb.AppendLine()
        sb.AppendLine("  El Excel incluye una hoja por módulo con los factores de")
        sb.AppendLine("  revisión coloreados según NSR-10 (verde/amarillo/rojo).")
        Return sb.ToString()
    End Function

    ' =========================================================================
    ' Exportar
    ' =========================================================================
    Private Sub Exportar_Click(sender As Object, e As EventArgs)
        Dim nombreArchivo As String = "Reporte_Proyecto_ARCO_" & DateTime.Now.ToString("yyyyMMdd")
        If _proyecto IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(_proyecto.Info.Nombre) Then
            nombreArchivo = "Reporte_Proyecto_" & _proyecto.Info.Nombre & "_" & DateTime.Now.ToString("yyyyMMdd")
        End If

        Dim dlg As New SaveFileDialog With {
            .Title = "Guardar Reporte del Proyecto",
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = nombreArchivo
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return

        Try
            _lblEstado.Text = "Generando reporte…"
            Me.Refresh()

            Using wb As New XLWorkbook()
                wb.Style.Font.FontName = "Segoe UI"
                wb.Style.Font.FontSize = 9

                GenerarPortada(wb)
                GenerarHojaColumnas(wb)
                GenerarHojaVigas(wb)
                GenerarHojaMuros(wb)
                GenerarHojaPilas(wb)
                GenerarHojaZapatas(wb)

                wb.SaveAs(dlg.FileName)
            End Using

            _lblEstado.Text = "Reporte exportado correctamente."
            Dim abrir = MessageBox.Show("¿Desea abrir el archivo ahora?", "Reporte generado",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Information)
            If abrir = DialogResult.Yes Then
                Process.Start(New ProcessStartInfo(dlg.FileName) With {.UseShellExecute = True})
            End If

        Catch ex As Exception
            _lblEstado.Text = "Error al generar el reporte."
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' =========================================================================
    ' PORTADA
    ' =========================================================================
    Private Sub GenerarPortada(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Portada")
        ws.ShowGridLines = False
        ws.Column(1).Width = 4
        ws.Column(2).Width = 28
        ws.Column(3).Width = 38

        ' Título principal
        Dim celdaTit = ws.Range("B2:C3")
        celdaTit.Merge()
        celdaTit.Value = "PROGRAMA ARCO — REVISIÓN ESTRUCTURAL"
        With celdaTit.Style
            .Font.FontSize = 16
            .Font.Bold = True
            .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XlEncabezado
            .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            .Alignment.Vertical = XLAlignmentVerticalValues.Center
        End With
        ws.Row(2).Height = 24
        ws.Row(3).Height = 24

        ' Subtítulo norma
        Dim celdaNorma = ws.Range("B4:C4")
        celdaNorma.Merge()
        celdaNorma.Value = "Norma NSR-10  |  Concreto Reforzado"
        With celdaNorma.Style
            .Font.FontSize = 10
            .Font.Italic = True
            .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XLColor.FromHtml("#4A6B9A")
            .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        End With

        Dim info = If(_proyecto IsNot Nothing, _proyecto.Info, New cInfoProyecto())
        Dim fila As Integer = 6

        EscribirFila(ws, fila,  "Proyecto",      If(String.IsNullOrWhiteSpace(info.Nombre),       "-", info.Nombre)) : fila += 1
        EscribirFila(ws, fila,  "Dirección",      If(String.IsNullOrWhiteSpace(info.Direccion),    "-", info.Direccion)) : fila += 1
        EscribirFila(ws, fila,  "Ciudad",         If(String.IsNullOrWhiteSpace(info.Ciudad),       "-", info.Ciudad)) : fila += 1
        EscribirFila(ws, fila,  "Departamento",   If(String.IsNullOrWhiteSpace(info.Departamento), "-", info.Departamento)) : fila += 1
        EscribirFila(ws, fila,  "Propietario",    If(String.IsNullOrWhiteSpace(info.Propietario),  "-", info.Propietario)) : fila += 1
        EscribirFila(ws, fila,  "Diseñador",      If(String.IsNullOrWhiteSpace(info.Designer),     "-", info.Designer)) : fila += 1
        EscribirFila(ws, fila,  "Año",            If(info.Year > 0, info.Year.ToString(), DateTime.Now.Year.ToString())) : fila += 1
        EscribirFila(ws, fila,  "N.° Pisos",      If(info.NPisos > 0, info.NPisos.ToString(), "-")) : fila += 1
        EscribirFila(ws, fila,  "Fecha reporte",  DateTime.Now.ToString("dd/MM/yyyy HH:mm")) : fila += 1

        ' Resumen de módulos
        fila += 1
        Dim hdr = ws.Range("B" & fila & ":C" & fila)
        hdr.Merge()
        hdr.Value = "CONTENIDO DEL REPORTE"
        With hdr.Style
            .Font.Bold = True
            .Font.FontColor = XLColor.White
            .Fill.BackgroundColor = XlSubEncabezado
            .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        End With
        ws.Row(fila).Height = 18
        fila += 1

        Dim el = If(_proyecto IsNot Nothing, _proyecto.Elementos, Nothing)

        Dim datosModulos As New List(Of String())
        datosModulos.Add(New String() {"Columnas",
            If(el IsNot Nothing, el.Columnas.Lista_Columnas.Count.ToString(), "0") & " importados — " &
            If(el IsNot Nothing, el.Columnas.Lista_Columnas.Where(Function(c) c.Ref_Modificado).Count().ToString(), "0") & " calculados"})
        datosModulos.Add(New String() {"Vigas",
            If(el IsNot Nothing, el.Vigas.Vigas.Count.ToString(), "0") & " importadas / generadas"})
        datosModulos.Add(New String() {"Muros",
            If(el IsNot Nothing, el.Muros.Lista_Muros.Count.ToString(), "0") & " importados — " &
            If(el IsNot Nothing, el.Muros.Lista_Muros.Where(Function(m) m.Ref_Modificado_Muros).Count().ToString(), "0") & " calculados"})

        Dim nPilasPortada As Integer = If(el IsNot Nothing, el.Pilas.ListaElementos.Count, 0)
        If nPilasPortada > 0 Then
            datosModulos.Add(New String() {"Pilas", nPilasPortada.ToString() & " pilas calculadas"})
        End If
        Dim nZapPortada As Integer = If(el IsNot Nothing, el.Zapatas.Tipos.Count, 0)
        If nZapPortada > 0 Then
            datosModulos.Add(New String() {"Zapatas", nZapPortada.ToString() & " zapatas definidas"})
        End If

        For Each dm In datosModulos
            Dim c1 = ws.Cell(fila, 2)
            Dim c2 = ws.Cell(fila, 3)
            c1.Value = dm(0)
            c1.Style.Font.Bold = True
            c2.Value = dm(1)
            If Not dm(1).StartsWith("0") Then
                c2.Style.Fill.BackgroundColor = XlOKFondo
                c2.Style.Font.FontColor = XlOKTexto
            End If
            fila += 1
        Next
    End Sub

    Private Sub EscribirFila(ws As IXLWorksheet, fila As Integer, etiqueta As String, valor As String)
        Dim c1 = ws.Cell(fila, 2)
        Dim c2 = ws.Cell(fila, 3)
        c1.Value = etiqueta
        c1.Style.Font.Bold = True
        c1.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFEFEF")
        c2.Value = valor
        ws.Row(fila).Height = 16
    End Sub

    ' =========================================================================
    ' HOJA COLUMNAS
    ' =========================================================================
    Private Sub GenerarHojaColumnas(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Columnas")
        ws.SheetView.FreezeRows(1)

        Dim encabezados = {"Columna", "B (cm)", "H (cm)", "Piso Ini", "Piso Fin",
                           "F.Flexion min", "Piso", "F.Cort.V2 min", "Piso",
                           "F.Cort.V3 min", "Piso", "Estado"}
        EscribirEncabezados(ws, 1, encabezados)

        If _proyecto Is Nothing OrElse _proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            ws.Cell(2, 1).Value = "(sin datos)"
            Return
        End If

        Dim cols = _proyecto.Elementos.Columnas.Lista_Columnas
        Dim fila As Integer = 2

        For Each col In cols.Where(Function(c) c.Ref_Modificado)
            Dim dim_ = col.Dimensiones
            ws.Cell(fila, 1).Value = col.Name_Label
            ws.Cell(fila, 2).Value = If(dim_ IsNot Nothing, Math.Round(dim_.B * 100, 0), 0)
            ws.Cell(fila, 3).Value = If(dim_ IsNot Nothing, Math.Round(dim_.H * 100, 0), 0)
            ws.Cell(fila, 4).Value = col.PisoInicial
            ws.Cell(fila, 5).Value = col.PisoFinal

            Dim fFlex = If(col.Lista_F.Count > 0, col.Lista_F(0), 0.0F)
            Dim fCv2  = If(col.Lista_F.Count > 1, col.Lista_F(1), 0.0F)
            Dim fCv3  = If(col.Lista_F.Count > 2, col.Lista_F(2), 0.0F)
            Dim pFlex = If(col.Lista_F_Piso.Count > 0, col.Lista_F_Piso(0), "")
            Dim pCv2  = If(col.Lista_F_Piso.Count > 1, col.Lista_F_Piso(1), "")
            Dim pCv3  = If(col.Lista_F_Piso.Count > 2, col.Lista_F_Piso(2), "")

            EscribirFactor(ws.Cell(fila, 6), fFlex)
            ws.Cell(fila, 7).Value = pFlex
            EscribirFactor(ws.Cell(fila, 8), fCv2)
            ws.Cell(fila, 9).Value = pCv2
            EscribirFactor(ws.Cell(fila, 10), fCv3)
            ws.Cell(fila, 11).Value = pCv3

            Dim cumple = fFlex >= 1.0F AndAlso fCv2 >= 1.0F AndAlso fCv3 >= 1.0F
            Dim cEstado = ws.Cell(fila, 12)
            cEstado.Value = If(cumple, "CUMPLE", "NO CUMPLE")
            cEstado.Style.Font.Bold = True
            cEstado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            If cumple Then
                cEstado.Style.Fill.BackgroundColor = XlOKFondo
                cEstado.Style.Font.FontColor = XlOKTexto
            Else
                cEstado.Style.Fill.BackgroundColor = XlMalFondo
                cEstado.Style.Font.FontColor = XlMalTexto
            End If

            If fila Mod 2 = 0 Then
                For c = 1 To 5
                    ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                Next
            End If

            fila += 1
        Next

        AgregarBordesTabla(ws, 1, fila - 1, 12)
        ws.Columns().AdjustToContents()

        ' Columnas sin calcular
        If cols.Any(Function(c) Not c.Ref_Modificado) Then
            fila += 1
            ws.Cell(fila, 1).Value = "Columnas sin refuerzo definido (omitidas del cálculo):"
            ws.Cell(fila, 1).Style.Font.Bold = True
            ws.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#9C5700")
            fila += 1
            For Each col In cols.Where(Function(c) Not c.Ref_Modificado)
                ws.Cell(fila, 1).Value = col.Name_Label
                fila += 1
            Next
        End If
    End Sub

    ' =========================================================================
    ' HOJA VIGAS
    ' =========================================================================
    Private Sub GenerarHojaVigas(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Vigas")
        ws.SheetView.FreezeRows(1)

        Dim encabezados = {"Viga", "Plano", "Piso", "L total (m)",
                           "As req. (cm2)", "As prov. (cm2)", "F.Flex (prov/req)", "Cumple Flexion"}
        EscribirEncabezados(ws, 1, encabezados)

        If _proyecto Is Nothing OrElse _proyecto.Elementos.Vigas.Vigas.Count = 0 Then
            ws.Cell(2, 1).Value = "(sin datos)"
            Return
        End If

        Dim fila As Integer = 2
        For Each v In _proyecto.Elementos.Vigas.Vigas.OrderBy(Function(x) x.Piso).ThenBy(Function(x) x.Nombre)
            ws.Cell(fila, 1).Value = v.Nombre
            ws.Cell(fila, 2).Value = v.NombrePlano
            ws.Cell(fila, 3).Value = v.Piso
            ws.Cell(fila, 4).Value = Math.Round(v.LongitudTotal, 2)
            ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"

            Dim asReq = v.AsRequerido * 10000
            Dim asProv = v.AsProvisto * 10000
            ws.Cell(fila, 5).Value = Math.Round(asReq, 2)
            ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 6).Value = Math.Round(asProv, 2)
            ws.Cell(fila, 6).Style.NumberFormat.Format = "0.00"

            Dim factor As Double = If(asReq > 0, asProv / asReq, 0)
            EscribirFactor(ws.Cell(fila, 7), factor)

            Dim cCumple = ws.Cell(fila, 8)
            cCumple.Value = If(v.CumpleFlexion, "CUMPLE", "NO CUMPLE")
            cCumple.Style.Font.Bold = True
            cCumple.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            If v.CumpleFlexion Then
                cCumple.Style.Fill.BackgroundColor = XlOKFondo
                cCumple.Style.Font.FontColor = XlOKTexto
            Else
                cCumple.Style.Fill.BackgroundColor = XlMalFondo
                cCumple.Style.Font.FontColor = XlMalTexto
            End If

            If fila Mod 2 = 0 Then
                For c = 1 To 4
                    ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                Next
            End If

            fila += 1
        Next

        AgregarBordesTabla(ws, 1, fila - 1, 8)
        ws.Columns().AdjustToContents()
    End Sub

    ' =========================================================================
    ' HOJA MUROS
    ' =========================================================================
    Private Sub GenerarHojaMuros(wb As XLWorkbook)
        Dim ws = wb.Worksheets.Add("Muros")
        ws.SheetView.FreezeRows(1)

        Dim encabezados = {"Muro", "Piso", "Lw (m)", "tw (m)",
                           "F.Flex Top", "F.Flex Bot", "Estado"}
        EscribirEncabezados(ws, 1, encabezados)

        If _proyecto Is Nothing OrElse _proyecto.Elementos.Muros.Lista_Muros.Count = 0 Then
            ws.Cell(2, 1).Value = "(sin datos)"
            Return
        End If

        Dim fila As Integer = 2
        For Each muro In _proyecto.Elementos.Muros.Lista_Muros.Where(Function(m) m.Ref_Modificado_Muros)
            For Each sec In muro.Lista_Secciones
                ws.Cell(fila, 1).Value = muro.Name
                ws.Cell(fila, 2).Value = sec.Piso
                ws.Cell(fila, 3).Value = Math.Round(sec.Lw_Model, 2)
                ws.Cell(fila, 3).Style.NumberFormat.Format = "0.00"
                ws.Cell(fila, 4).Value = Math.Round(sec.tw_Model, 2)
                ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"

                EscribirFactor(ws.Cell(fila, 5), sec.F_Flexo_Top)
                EscribirFactor(ws.Cell(fila, 6), sec.F_Flexo_Bot)

                Dim cumple = sec.F_Flexo_Top >= 1.0 AndAlso sec.F_Flexo_Bot >= 1.0
                Dim cE = ws.Cell(fila, 7)
                cE.Value = If(cumple, "CUMPLE", "NO CUMPLE")
                cE.Style.Font.Bold = True
                cE.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                If cumple Then
                    cE.Style.Fill.BackgroundColor = XlOKFondo
                    cE.Style.Font.FontColor = XlOKTexto
                Else
                    cE.Style.Fill.BackgroundColor = XlMalFondo
                    cE.Style.Font.FontColor = XlMalTexto
                End If

                If fila Mod 2 = 0 Then
                    For c = 1 To 4
                        ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                    Next
                End If

                fila += 1
            Next
        Next

        If fila = 2 Then ws.Cell(2, 1).Value = "(muros sin calculo definido)"
        AgregarBordesTabla(ws, 1, Math.Max(fila - 1, 1), 7)
        ws.Columns().AdjustToContents()
    End Sub

    ' =========================================================================
    ' Helpers de formato
    ' =========================================================================
    Private Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, encabezados As String())
        For i = 0 To encabezados.Length - 1
            Dim cell = ws.Cell(fila, i + 1)
            cell.Value = encabezados(i)
            With cell.Style
                .Fill.BackgroundColor = XlEncabezado
                .Font.FontColor = XLColor.White
                .Font.Bold = True
                .Font.FontSize = 10
                .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                .Alignment.Vertical = XLAlignmentVerticalValues.Center
                .Border.OutsideBorder = XLBorderStyleValues.Thin
                .Border.OutsideBorderColor = XLColor.White
            End With
            ws.Row(fila).Height = 20
        Next
    End Sub

    Private Sub EscribirFactor(cell As IXLCell, valor As Double)
        If valor <= 0 Then
            cell.Value = "-"
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            Return
        End If
        Dim v = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        If v >= 1.0 Then
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor = XlOKTexto
        ElseIf v >= 0.9 Then
            cell.Style.Fill.BackgroundColor = XlAlertaFondo
            cell.Style.Font.FontColor = XlAlertaTexto
        Else
            cell.Style.Fill.BackgroundColor = XlMalFondo
            cell.Style.Font.FontColor = XlMalTexto
        End If
        cell.Style.Font.Bold = True
    End Sub

    Private Sub AgregarBordesTabla(ws As IXLWorksheet, filaIni As Integer, filaFin As Integer, numCols As Integer)
        If filaFin < filaIni Then Return
        Dim rango = ws.Range(filaIni, 1, filaFin, numCols)
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair
        rango.Style.Border.InsideBorderColor = XLColor.FromHtml("#CCCCCC")
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Medium
        rango.Style.Border.OutsideBorderColor = XlEncabezado
    End Sub

    ' =========================================================================
    ' HOJA PILAS
    ' =========================================================================
    Private Sub GenerarHojaPilas(wb As XLWorkbook)
        If _proyecto Is Nothing OrElse _proyecto.Elementos.Pilas.ListaElementos.Count = 0 Then Return

        Dim ws = wb.Worksheets.Add("Pilas")
        ws.SheetView.FreezeRows(1)

        Dim encabezados = {"Elemento", "Df (m)", "Dc (m)", "L (m)", "fc (MPa)",
                           "Refuerzo", "Cuantia (%)",
                           "Cargas", "Suelo", "Cortante", "Interaccion", "Estado"}
        EscribirEncabezados(ws, 1, encabezados)

        Dim fila As Integer = 2
        For Each p In _proyecto.Elementos.Pilas.ListaElementos
            ws.Cell(fila, 1).Value = p.Name_Elemento
            ws.Cell(fila, 2).Value = Math.Round(p.Df, 2)
            ws.Cell(fila, 3).Value = Math.Round(CDbl(p.Dc), 2)
            ws.Cell(fila, 4).Value = Math.Round(CDbl(p.L_Pila), 2)
            ws.Cell(fila, 5).Value = p.fc
            ws.Cell(fila, 6).Value = p.N_Barra_Long & " x " & p.Cant_Barras_Long
            ws.Cell(fila, 7).Value = Math.Round(CDbl(p.Cuantia) * 100, 3)
            ws.Cell(fila, 7).Style.NumberFormat.Format = "0.000"

            Dim okCargas     = p.Check1_PsE >= 0.9 AndAlso p.Check2_PsD >= 0.9 AndAlso
                               p.Check3_PuE >= 0.9 AndAlso p.Check4_PuD >= 0.9
            Dim okSuelo      = p.Relacion_EsfE >= 0.9 AndAlso p.Relacion_EsfD >= 0.9
            Dim okCortante   = p.FactorShear >= 0.9
            Dim okInteracc   = p.Factor_Diagonal >= 0.9 AndAlso p.Factor_CortesH >= 0.9
            Dim cumpleGeneral = okCargas AndAlso okSuelo AndAlso okCortante AndAlso okInteracc

            EscribirCumpleTexto(ws.Cell(fila, 8),  okCargas)
            EscribirCumpleTexto(ws.Cell(fila, 9),  okSuelo)
            EscribirCumpleTexto(ws.Cell(fila, 10), okCortante)
            EscribirCumpleTexto(ws.Cell(fila, 11), okInteracc)
            EscribirCumpleTexto(ws.Cell(fila, 12), cumpleGeneral)

            If fila Mod 2 = 0 Then
                For c = 1 To 7
                    ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                Next
            End If
            fila += 1
        Next

        AgregarBordesTabla(ws, 1, fila - 1, 12)
        ws.Columns().AdjustToContents()
    End Sub

    ' =========================================================================
    ' HOJA ZAPATAS
    ' =========================================================================
    Private Sub GenerarHojaZapatas(wb As XLWorkbook)
        If _proyecto Is Nothing OrElse _proyecto.Elementos.Zapatas.Tipos.Count = 0 Then Return

        Dim ws = wb.Worksheets.Add("Zapatas")
        ws.SheetView.FreezeRows(1)

        Dim encabezados = {"Zapata", "L_b (m)", "L_h (m)", "e (m)", "b (m)", "h (m)",
                           "fc (MPa)", "qAdm Est.", "qAdm Din.",
                           "Capacidad", "Punzonamiento", "Cortante", "Flexion", "General"}
        EscribirEncabezados(ws, 1, encabezados)

        Dim fila As Integer = 2
        For Each z In _proyecto.Elementos.Zapatas.Tipos
            ws.Cell(fila, 1).Value = z.Nombre
            ws.Cell(fila, 2).Value = Math.Round(z.L_b, 2)
            ws.Cell(fila, 3).Value = Math.Round(z.L_h, 2)
            ws.Cell(fila, 4).Value = Math.Round(z.e, 2)
            ws.Cell(fila, 5).Value = Math.Round(z.b, 2)
            ws.Cell(fila, 6).Value = Math.Round(z.h, 2)
            ws.Cell(fila, 7).Value = z.fc
            ws.Cell(fila, 8).Value = z.qAdm_Est
            ws.Cell(fila, 9).Value = z.qAdm_Din

            If z.Resultados IsNot Nothing AndAlso z.Resultados.Count > 0 Then
                Dim vals = z.Resultados.Values.ToList()
                EscribirCumpleTexto(ws.Cell(fila, 10), vals.All(Function(r) r.CumpleCapacidad))
                EscribirCumpleTexto(ws.Cell(fila, 11), vals.All(Function(r) r.CumplePunzonamiento))
                EscribirCumpleTexto(ws.Cell(fila, 12), vals.All(Function(r) r.CumpleCortante_1 AndAlso r.CumpleCortante_2 AndAlso
                                                                               r.CumpleCortante_3 AndAlso r.CumpleCortante_4))
                EscribirCumpleTexto(ws.Cell(fila, 13), vals.All(Function(r) r.Cumple_L1 AndAlso r.Cumple_L2))
                EscribirCumpleTexto(ws.Cell(fila, 14), vals.All(Function(r) r.CumpleGeneral))
            Else
                For c = 10 To 14
                    ws.Cell(fila, c).Value = "Sin calcular"
                Next
            End If

            If fila Mod 2 = 0 Then
                For c = 1 To 9
                    ws.Cell(fila, c).Style.Fill.BackgroundColor = XlFilaPar
                Next
            End If
            fila += 1
        Next

        AgregarBordesTabla(ws, 1, fila - 1, 14)
        ws.Columns().AdjustToContents()
    End Sub

    Private Sub EscribirCumpleTexto(cell As IXLCell, cumple As Boolean)
        cell.Value = If(cumple, "CUMPLE", "NO CUMPLE")
        cell.Style.Font.Bold = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        If cumple Then
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor = XlOKTexto
        Else
            cell.Style.Fill.BackgroundColor = XlMalFondo
            cell.Style.Font.FontColor = XlMalTexto
        End If
    End Sub

End Class
