Imports ClosedXML.Excel

Public Class Form_02_Reporte_Columnas
    Inherits Form

    Public Property Columnas As List(Of Columna)

    ' ── Paleta ─────────────────────────────────────────────────────────────
    Private ReadOnly ClEnc As Color = Color.FromArgb(87, 87, 87)
    Private ReadOnly ClOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Private ReadOnly ClOKTxt As Color = ColorTranslator.FromHtml("#006100")
    Private ReadOnly ClMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Private ReadOnly ClMalTxt As Color = ColorTranslator.FromHtml("#9C0006")
    Private ReadOnly ClAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Private ReadOnly ClAlertaTxt As Color = ColorTranslator.FromHtml("#9C5700")

    Private ReadOnly XlClEnc As XLColor = XLColor.FromHtml("#575757")
    Private ReadOnly XlClOK As XLColor = XLColor.FromHtml("#C6EFCE")
    Private ReadOnly XlClOKTxt As XLColor = XLColor.FromHtml("#006100")
    Private ReadOnly XlClMal As XLColor = XLColor.FromHtml("#FFC7CE")
    Private ReadOnly XlClMalTxt As XLColor = XLColor.FromHtml("#9C0006")

    ' ── Controles ──────────────────────────────────────────────────────────
    Private WithEvents TabControl1 As New TabControl()
    Private WithEvents DgvFlex As New DataGridView()
    Private WithEvents DgvCortante As New DataGridView()
    Private WithEvents DgvConfinamiento As New DataGridView()
    Private WithEvents DgvALR As New DataGridView()
    Private WithEvents DgvResumen As New DataGridView()
    Private WithEvents ChkSoloBad As New CheckBox()
    Private WithEvents BtnExport As New Button()
    Private _lblConteo As New Label()

    ' ── Datos ──────────────────────────────────────────────────────────────
    Private _filasFlex As New List(Of FilaFlex)
    Private _filasCortante As New List(Of FilaCortante)
    Private _filasConf As New List(Of FilaConfinamiento)
    Private _filasALR As New List(Of FilaALR)
    Private _filasResumen As New List(Of FilaResumen)

    ' ── Constructor ────────────────────────────────────────────────────────
    Public Sub New()
        Me.Text = "Reporte de Resultados — Columnas (NSR-10)"
        Me.Size = New Size(1420, 780)
        Me.MinimumSize = New Size(1000, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 9)
        BuildUI()
        AddHandler Me.Load, AddressOf OnLoad
    End Sub

    ' ── Construcción UI ────────────────────────────────────────────────────
    Private Sub BuildUI()
        ' ── TabControl (Fill) debe agregarse PRIMERO para que los paneles Top lo desplacen
        TabControl1.Dock = DockStyle.Fill
        TabControl1.Font = New Font("Segoe UI", 9, FontStyle.Bold)

        Dim tabFlex As New TabPage("Flexo-Compresion")
        Dim tabCort As New TabPage("Cortante")
        Dim tabConf As New TabPage("Confinamiento NSR-10")
        Dim tabALR As New TabPage("ALR")
        Dim tabRes As New TabPage("Resumen Ejecutivo")

        ConfigDgv(DgvFlex) : tabFlex.Controls.Add(DgvFlex)
        ConfigDgv(DgvCortante) : tabCort.Controls.Add(DgvCortante)
        ConfigDgv(DgvConfinamiento) : tabConf.Controls.Add(DgvConfinamiento)
        ConfigDgv(DgvALR) : tabALR.Controls.Add(DgvALR)
        ConfigDgv(DgvResumen) : tabRes.Controls.Add(DgvResumen)

        TabControl1.TabPages.AddRange({tabFlex, tabCort, tabConf, tabALR, tabRes})
        Me.Controls.Add(TabControl1)   ' primero en Controls → detrás en Z-order

        ' ── Toolbar (se agrega después → queda encima visualmente)
        Dim pnlTools As New Panel With {
            .Dock = DockStyle.Top, .Height = 38,
            .BackColor = Color.FromArgb(245, 245, 245)
        }
        ChkSoloBad.Text = "Solo elementos con observaciones"
        ChkSoloBad.AutoSize = True
        ChkSoloBad.Location = New Point(10, 10)

        BtnExport.Text = "Exportar a Excel"
        BtnExport.Size = New Size(130, 24)
        BtnExport.Location = New Point(295, 7)
        BtnExport.BackColor = Color.FromArgb(33, 115, 70)
        BtnExport.ForeColor = Color.White
        BtnExport.FlatStyle = FlatStyle.Flat
        BtnExport.Font = New Font("Segoe UI", 9, FontStyle.Bold)

        _lblConteo.AutoSize = True
        _lblConteo.Location = New Point(440, 12)
        _lblConteo.ForeColor = Color.FromArgb(80, 80, 80)

        pnlTools.Controls.AddRange({ChkSoloBad, BtnExport, _lblConteo})
        Me.Controls.Add(pnlTools)

        ' ── Header (agregado al final → queda al frente y en la cima visual)
        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top, .Height = 34,
            .BackColor = ClEnc
        }
        Dim lblTitle As New Label With {
            .Text = "REPORTE DE RESULTADOS — COLUMNAS ESTRUCTURALES (NSR-10)",
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
            .AutoSize = True,
            .Location = New Point(10, 9)
        }
        pnlHeader.Controls.Add(lblTitle)
        Me.Controls.Add(pnlHeader)

        ' ── Columnas de cada grilla
        DefColsFlex()
        DefColsCortante()
        DefColsConfinamiento()
        DefColsALR()
        DefColsResumen()
    End Sub

    Private Sub ConfigDgv(dgv As DataGridView)
        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToResizeRows = False
        dgv.RowHeadersVisible = False
        dgv.BorderStyle = BorderStyle.None
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        dgv.BackgroundColor = Color.White
        dgv.GridColor = Color.FromArgb(210, 210, 210)
        dgv.EnableHeadersVisualStyles = False
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.ColumnHeadersHeight = 30
        dgv.RowTemplate.Height = 22
        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor = ClEnc
            .ForeColor = Color.White
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
        With dgv.DefaultCellStyle
            .Font = New Font("Segoe UI", 8.5F)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

    Private Function NCol(header As String, width As Integer,
                          Optional alin As DataGridViewContentAlignment = DataGridViewContentAlignment.MiddleCenter) As DataGridViewTextBoxColumn
        Return New DataGridViewTextBoxColumn With {
            .HeaderText = header, .Width = width,
            .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = alin},
            .SortMode = DataGridViewColumnSortMode.NotSortable
        }
    End Function

    Private Sub DefColsFlex()
        DgvFlex.Columns.Add(NCol("Elemento", 110, DataGridViewContentAlignment.MiddleLeft))
        DgvFlex.Columns.Add(NCol("Piso", 68))
        DgvFlex.Columns.Add(NCol("B (m)", 62))
        DgvFlex.Columns.Add(NCol("H (m)", 62))
        DgvFlex.Columns.Add(NCol("f'c (MPa)", 68))
        DgvFlex.Columns.Add(NCol("As Req Top (mm²)", 118))
        DgvFlex.Columns.Add(NCol("As Col Top (mm²)", 118))
        DgvFlex.Columns.Add(NCol("F Flex Top", 82))
        DgvFlex.Columns.Add(NCol("As Req Bot (mm²)", 118))
        DgvFlex.Columns.Add(NCol("As Col Bot (mm²)", 118))
        DgvFlex.Columns.Add(NCol("F Flex Bot", 82))
        DgvFlex.Columns.Add(NCol("Estado", 80))
    End Sub

    Private Sub DefColsCortante()
        DgvCortante.Columns.Add(NCol("Elemento", 110, DataGridViewContentAlignment.MiddleLeft))
        DgvCortante.Columns.Add(NCol("Piso", 68))
        DgvCortante.Columns.Add(NCol("B (m)", 58))
        DgvCortante.Columns.Add(NCol("H (m)", 58))
        DgvCortante.Columns.Add(NCol("Sep ZC (m)", 82))
        DgvCortante.Columns.Add(NCol("# Barra", 72))
        DgvCortante.Columns.Add(NCol("Ram. L", 60))
        DgvCortante.Columns.Add(NCol("Ram. C", 60))
        DgvCortante.Columns.Add(NCol("Vu2 (kN)", 72))
        DgvCortante.Columns.Add(NCol("Vc2 (kN)", 72))
        DgvCortante.Columns.Add(NCol("Vn2 (kN)", 72))
        DgvCortante.Columns.Add(NCol("F V2", 62))
        DgvCortante.Columns.Add(NCol("Vu3 (kN)", 72))
        DgvCortante.Columns.Add(NCol("Vc3 (kN)", 72))
        DgvCortante.Columns.Add(NCol("Vn3 (kN)", 72))
        DgvCortante.Columns.Add(NCol("F V3", 62))
        DgvCortante.Columns.Add(NCol("Estado", 80))
    End Sub

    Private Sub DefColsConfinamiento()
        ' Ash (zona larga y corta) + L0 según NSR-10 C.21.4.4
        DgvConfinamiento.Columns.Add(NCol("Elemento", 110, DataGridViewContentAlignment.MiddleLeft))
        DgvConfinamiento.Columns.Add(NCol("Piso", 68))
        DgvConfinamiento.Columns.Add(NCol("B (m)", 58))
        DgvConfinamiento.Columns.Add(NCol("H (m)", 58))
        DgvConfinamiento.Columns.Add(NCol("B. Long Min", 85))
        DgvConfinamiento.Columns.Add(NCol("S0_L (m)", 68))
        DgvConfinamiento.Columns.Add(NCol("Ash_L Req (mm²)", 108))
        DgvConfinamiento.Columns.Add(NCol("Ash_L Prov (mm²)", 108))
        DgvConfinamiento.Columns.Add(NCol("F Ash L", 68))
        DgvConfinamiento.Columns.Add(NCol("S0_C (m)", 68))
        DgvConfinamiento.Columns.Add(NCol("Ash_C Req (mm²)", 108))
        DgvConfinamiento.Columns.Add(NCol("Ash_C Prov (mm²)", 108))
        DgvConfinamiento.Columns.Add(NCol("F Ash C", 68))
        ' L0 — Longitud zona confinada NSR-10 C.21.4.4
        DgvConfinamiento.Columns.Add(NCol("L0 Req (m)", 90))
        DgvConfinamiento.Columns.Add(NCol("L0 Prov (m)", 90))
        DgvConfinamiento.Columns.Add(NCol("F L0", 62))
        DgvConfinamiento.Columns.Add(NCol("Estado", 80))
    End Sub

    Private Sub DefColsALR()
        DgvALR.Columns.Add(NCol("Elemento", 110, DataGridViewContentAlignment.MiddleLeft))
        DgvALR.Columns.Add(NCol("Piso", 68))
        DgvALR.Columns.Add(NCol("f'c (MPa)", 68))
        DgvALR.Columns.Add(NCol("B (m)", 62))
        DgvALR.Columns.Add(NCol("H (m)", 62))
        DgvALR.Columns.Add(NCol("Combinacion", 160, DataGridViewContentAlignment.MiddleLeft))
        DgvALR.Columns.Add(NCol("Pu (kN)", 82))
        DgvALR.Columns.Add(NCol("Ag (m²)", 75))
        DgvALR.Columns.Add(NCol("ALR", 75))
        DgvALR.Columns.Add(NCol("Estado", 80))
    End Sub

    Private Sub DefColsResumen()
        DgvResumen.Columns.Add(NCol("Elemento", 110, DataGridViewContentAlignment.MiddleLeft))
        DgvResumen.Columns.Add(NCol("Secc.", 52))
        DgvResumen.Columns.Add(NCol("F Flex min", 85))
        DgvResumen.Columns.Add(NCol("Piso Flex", 80))
        DgvResumen.Columns.Add(NCol("F Cort V2", 85))
        DgvResumen.Columns.Add(NCol("F Cort V3", 85))
        DgvResumen.Columns.Add(NCol("Piso Cort", 80))
        DgvResumen.Columns.Add(NCol("F Ash L", 72))
        DgvResumen.Columns.Add(NCol("F Ash C", 72))
        DgvResumen.Columns.Add(NCol("F L0", 68))
        DgvResumen.Columns.Add(NCol("ALR max", 75))
        DgvResumen.Columns.Add(NCol("Estado", 80))
    End Sub

    ' ── Carga ──────────────────────────────────────────────────────────────
    Private Sub OnLoad(sender As Object, e As EventArgs)
        If Columnas Is Nothing OrElse Columnas.Count = 0 Then Return
        CalcularFilas()
        MostrarFlex()
        MostrarCortante()
        MostrarConfinamiento()
        MostrarALR()
        MostrarResumen()
    End Sub

    ' ── Cálculo de filas ───────────────────────────────────────────────────
    Private Sub CalcularFilas()
        _filasFlex.Clear() : _filasCortante.Clear()
        _filasConf.Clear() : _filasALR.Clear() : _filasResumen.Clear()

        For Each col In Columnas
            Dim fMinFlex As Single = Single.MaxValue
            Dim pisoCritFlex As String = "—"
            Dim fMinV2 As Single = Single.MaxValue
            Dim fMinV3 As Single = Single.MaxValue
            Dim pisoCritCort As String = "—"
            Dim fMinAshL As Single = Single.MaxValue
            Dim fMinAshC As Single = Single.MaxValue
            Dim fMinL0 As Single = Single.MaxValue
            Dim alrMax As Single = 0

            For Each tr In col.Lista_Tramos_Columnas
                _filasFlex.Add(New FilaFlex With {
                    .Elemento = col.Name_Label, .Piso = tr.Piso,
                    .B = tr.B_Plano, .H = tr.H_Plano, .fc = tr.fc,
                    .AsReqTop = tr.As_Req_Top, .AsColTop = tr.As_Col_Top, .FFlexTop = tr.F_Flexo_Top,
                    .AsReqBot = tr.As_Req_Bottom, .AsColBot = tr.As_Col_Bottom, .FFlexBot = tr.F_Flexo_Bottom
                })

                _filasCortante.Add(New FilaCortante With {
                    .Elemento = col.Name_Label, .Piso = tr.Piso,
                    .B = tr.B_Plano, .H = tr.H_Plano,
                    .SepZC = tr.Separacion_Estribos, .Barra = tr.Numero_Barras_Estribo,
                    .RamasL = tr.Num_Ramas_Largo, .RamasC = tr.Num_Ramas_Corto,
                    .Vu2 = tr.Vu_2, .Vc2 = tr.Vc_2, .Vn2 = tr.Vn_2, .FV2 = tr.F_Cortante_2,
                    .Vu3 = tr.Vu_3, .Vc3 = tr.Vc_3, .Vn3 = tr.Vn_3, .FV3 = tr.F_Cortante_3
                })

                ' Para circular: Ash provista total = estribo circular + ganchos (ambas col. muestran el total)
                Dim ashProvL As Single = If(tr.EsCircular, tr.Ash_Col_Largo + tr.Ash_Col_Corto, tr.Ash_Col_Largo)
                Dim ashProvC As Single = If(tr.EsCircular, tr.Ash_Col_Largo + tr.Ash_Col_Corto, tr.Ash_Col_Corto)
                _filasConf.Add(New FilaConfinamiento With {
                    .Elemento = col.Name_Label, .Piso = tr.Piso,
                    .B = tr.B_Plano, .H = tr.H_Plano, .BarraLongMin = tr.Barra_Long_Min,
                    .S0_L = tr.S0_L, .AshLReq = tr.Ash_L, .AshLProv = ashProvL,
                    .FAshL = tr.F_Ash_Largo,
                    .S0_C = tr.S0_C, .AshCReq = tr.Ash_C, .AshCProv = ashProvC,
                    .FAshC = tr.F_Ash_Corto,
                    .L0Req = Math.Max(tr.L0_L, tr.L0_C),
                    .L0Prov = tr.L0_Prov,
                    .FL0 = If(Math.Max(tr.L0_L, tr.L0_C) > 0 AndAlso tr.L0_Prov > 0,
                              CSng(Math.Round(tr.L0_Prov / Math.Max(tr.L0_L, tr.L0_C), 2)), 0)
                })

                Dim wFlex = If(tr.F_Flexo_Top > 0 AndAlso tr.F_Flexo_Bottom > 0,
                               Math.Min(tr.F_Flexo_Top, tr.F_Flexo_Bottom),
                               Math.Max(tr.F_Flexo_Top, tr.F_Flexo_Bottom))
                If wFlex > 0 AndAlso wFlex < fMinFlex Then fMinFlex = wFlex : pisoCritFlex = tr.Piso
                If tr.F_Cortante_2 > 0 AndAlso tr.F_Cortante_2 < fMinV2 Then fMinV2 = tr.F_Cortante_2 : pisoCritCort = tr.Piso
                If tr.F_Cortante_3 > 0 AndAlso tr.F_Cortante_3 < fMinV3 Then fMinV3 = tr.F_Cortante_3
                If tr.F_Ash_Largo > 0 AndAlso tr.F_Ash_Largo < fMinAshL Then fMinAshL = tr.F_Ash_Largo
                If tr.F_Ash_Corto > 0 AndAlso tr.F_Ash_Corto < fMinAshC Then fMinAshC = tr.F_Ash_Corto
                Dim fl0Tr As Single = If(Math.Max(tr.L0_L, tr.L0_C) > 0 AndAlso tr.L0_Prov > 0,
                                         CSng(Math.Round(tr.L0_Prov / Math.Max(tr.L0_L, tr.L0_C), 2)), 0)
                If fl0Tr > 0 AndAlso fl0Tr < fMinL0 Then fMinL0 = fl0Tr
            Next

            For Each alrItem In col.Lista_ALR
                Dim trBase = If(col.Lista_Tramos_Columnas.Count > 0, col.Lista_Tramos_Columnas(col.Lista_Tramos_Columnas.Count - 1), Nothing)
                Dim fa As New FilaALR With {
                    .Elemento = col.Name_Label,
                    .Piso = If(trBase IsNot Nothing, trBase.Piso, ""),
                    .fc = If(trBase IsNot Nothing, trBase.fc, 0),
                    .B = If(trBase IsNot Nothing, trBase.B_Plano, 0),
                    .H = If(trBase IsNot Nothing, trBase.H_Plano, 0),
                    .Combinacion = alrItem.Combinacion,
                    .ALR = alrItem.ALR
                }
                If trBase IsNot Nothing Then
                    fa.Ag = fa.B * fa.H
                    Dim fuerza = trBase.Lista_Combinaciones.Find(Function(x) x.Name = alrItem.Combinacion)
                    If fuerza IsNot Nothing Then fa.Pu = Math.Abs(fuerza.P)
                End If
                If fa.ALR > alrMax Then alrMax = fa.ALR
                _filasALR.Add(fa)
            Next

            Dim fr As New FilaResumen With {
                .Elemento = col.Name_Label,
                .Secciones = col.Lista_Tramos_Columnas.Count,
                .FFlexMin = If(fMinFlex = Single.MaxValue, -1, fMinFlex),
                .PisoCritFlex = pisoCritFlex,
                .FV2Min = If(fMinV2 = Single.MaxValue, -1, fMinV2),
                .FV3Min = If(fMinV3 = Single.MaxValue, -1, fMinV3),
                .PisoCritCort = pisoCritCort,
                .FAshLMin = If(fMinAshL = Single.MaxValue, -1, fMinAshL),
                .FAshCMin = If(fMinAshC = Single.MaxValue, -1, fMinAshC),
                .FL0Min = If(fMinL0 = Single.MaxValue, -1, fMinL0),
                .ALRMax = alrMax
            }
            fr.Cumple = (fr.FFlexMin < 0 OrElse fr.FFlexMin >= 0.9F) AndAlso
                        (fr.FV2Min < 0 OrElse fr.FV2Min >= 0.9F) AndAlso
                        (fr.FV3Min < 0 OrElse fr.FV3Min >= 0.9F) AndAlso
                        (fr.FAshLMin < 0 OrElse fr.FAshLMin >= 0.9F) AndAlso
                        (fr.FAshCMin < 0 OrElse fr.FAshCMin >= 0.9F) AndAlso
                        (fr.FL0Min < 0 OrElse fr.FL0Min >= 0.9F)
            _filasResumen.Add(fr)
        Next
    End Sub

    ' ── Mostrar por pestaña ────────────────────────────────────────────────
    Private Sub MostrarFlex()
        DgvFlex.Rows.Clear()
        For i = 0 To _filasFlex.Count - 1
            Dim f = _filasFlex(i)
            Dim cumple = (f.FFlexTop >= 0.9F OrElse f.FFlexTop = 0) AndAlso (f.FFlexBot >= 0.9F OrElse f.FFlexBot = 0)
            If ChkSoloBad.Checked AndAlso cumple Then Continue For
            Dim r = DgvFlex.Rows(DgvFlex.Rows.Add())
            If i Mod 2 = 1 Then r.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            r.Cells(0).Value = f.Elemento
            r.Cells(1).Value = f.Piso
            r.Cells(2).Value = Math.Round(f.B, 3)
            r.Cells(3).Value = Math.Round(f.H, 3)
            r.Cells(4).Value = f.fc
            r.Cells(5).Value = If(f.AsReqTop > 0, CObj(Math.Round(f.AsReqTop, 0)), "—")
            r.Cells(6).Value = If(f.AsColTop > 0, CObj(Math.Round(f.AsColTop, 0)), "—")
            r.Cells(7).Value = If(f.FFlexTop > 0, Math.Round(f.FFlexTop, 2).ToString("F2"), "—")
            r.Cells(8).Value = If(f.AsReqBot > 0, CObj(Math.Round(f.AsReqBot, 0)), "—")
            r.Cells(9).Value = If(f.AsColBot > 0, CObj(Math.Round(f.AsColBot, 0)), "—")
            r.Cells(10).Value = If(f.FFlexBot > 0, Math.Round(f.FFlexBot, 2).ToString("F2"), "—")
            r.Cells(11).Value = If(cumple, "OK", "Revisar")
            AplicarColorFactor(r.Cells(7), f.FFlexTop)
            AplicarColorFactor(r.Cells(10), f.FFlexBot)
            AplicarColorEstado(r.Cells(11), cumple)
        Next
    End Sub

    Private Sub MostrarCortante()
        DgvCortante.Rows.Clear()
        For i = 0 To _filasCortante.Count - 1
            Dim f = _filasCortante(i)
            Dim cumple = (f.FV2 >= 0.9F OrElse f.FV2 = 0) AndAlso (f.FV3 >= 0.9F OrElse f.FV3 = 0)
            If ChkSoloBad.Checked AndAlso cumple Then Continue For
            Dim r = DgvCortante.Rows(DgvCortante.Rows.Add())
            If i Mod 2 = 1 Then r.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            r.Cells(0).Value = f.Elemento
            r.Cells(1).Value = f.Piso
            r.Cells(2).Value = Math.Round(f.B, 3)
            r.Cells(3).Value = Math.Round(f.H, 3)
            r.Cells(4).Value = If(f.SepZC > 0, CObj(Math.Round(f.SepZC, 3)), "—")
            r.Cells(5).Value = If(f.Barra IsNot Nothing, f.Barra, "—")
            r.Cells(6).Value = f.RamasL
            r.Cells(7).Value = f.RamasC
            r.Cells(8).Value = If(f.Vu2 > 0, CObj(Math.Round(f.Vu2, 2)), "—")
            r.Cells(9).Value = If(f.Vc2 > 0, CObj(Math.Round(f.Vc2, 2)), "—")
            r.Cells(10).Value = If(f.Vn2 > 0, CObj(Math.Round(f.Vn2, 2)), "—")
            r.Cells(11).Value = If(f.FV2 > 0, Math.Round(f.FV2, 2).ToString("F2"), "—")
            r.Cells(12).Value = If(f.Vu3 > 0, CObj(Math.Round(f.Vu3, 2)), "—")
            r.Cells(13).Value = If(f.Vc3 > 0, CObj(Math.Round(f.Vc3, 2)), "—")
            r.Cells(14).Value = If(f.Vn3 > 0, CObj(Math.Round(f.Vn3, 2)), "—")
            r.Cells(15).Value = If(f.FV3 > 0, Math.Round(f.FV3, 2).ToString("F2"), "—")
            r.Cells(16).Value = If(cumple, "OK", "Revisar")
            AplicarColorFactor(r.Cells(11), f.FV2)
            AplicarColorFactor(r.Cells(15), f.FV3)
            AplicarColorEstado(r.Cells(16), cumple)
        Next
    End Sub

    Private Sub MostrarConfinamiento()
        ' Pestaña "Confinamiento NSR-10":
        '   Ash L/C (cols 5-12) → verificación de área de refuerzo de confinamiento
        '   L0 L/C (cols 13-18) → verificación longitud de zona confinada NSR-10 C.21.4.4
        DgvConfinamiento.Rows.Clear()
        For i = 0 To _filasConf.Count - 1
            Dim f = _filasConf(i)
            Dim cumpleAsh = (f.FAshL >= 0.9F OrElse f.FAshL = 0) AndAlso (f.FAshC >= 0.9F OrElse f.FAshC = 0)
            Dim cumpleL0 = (f.FL0 >= 0.9F OrElse f.FL0 = 0)
            Dim cumple = cumpleAsh AndAlso cumpleL0
            If ChkSoloBad.Checked AndAlso cumple Then Continue For
            Dim r = DgvConfinamiento.Rows(DgvConfinamiento.Rows.Add())
            If i Mod 2 = 1 Then r.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            r.Cells(0).Value = f.Elemento
            r.Cells(1).Value = f.Piso
            r.Cells(2).Value = Math.Round(f.B, 3)
            r.Cells(3).Value = Math.Round(f.H, 3)
            r.Cells(4).Value = If(f.BarraLongMin IsNot Nothing, f.BarraLongMin, "—")
            r.Cells(5).Value = If(f.S0_L > 0, CObj(Math.Round(f.S0_L, 3)), "—")
            r.Cells(6).Value = If(f.AshLReq > 0, CObj(Math.Round(f.AshLReq, 1)), "—")
            r.Cells(7).Value = If(f.AshLProv > 0, CObj(Math.Round(f.AshLProv, 1)), "—")
            r.Cells(8).Value = If(f.FAshL > 0, Math.Round(f.FAshL, 2).ToString("F2"), "—")
            r.Cells(9).Value = If(f.S0_C > 0, CObj(Math.Round(f.S0_C, 3)), "—")
            r.Cells(10).Value = If(f.AshCReq > 0, CObj(Math.Round(f.AshCReq, 1)), "—")
            r.Cells(11).Value = If(f.AshCProv > 0, CObj(Math.Round(f.AshCProv, 1)), "—")
            r.Cells(12).Value = If(f.FAshC > 0, Math.Round(f.FAshC, 2).ToString("F2"), "—")
            r.Cells(13).Value = If(f.L0Req > 0, CObj(Math.Round(f.L0Req, 3)), "—")
            r.Cells(14).Value = If(f.L0Prov > 0, CObj(Math.Round(f.L0Prov, 3)), "—")
            r.Cells(15).Value = If(f.FL0 > 0, Math.Round(f.FL0, 2).ToString("F2"), "—")
            r.Cells(16).Value = If(cumple, "OK", "Revisar")
            AplicarColorFactor(r.Cells(8), f.FAshL)
            AplicarColorFactor(r.Cells(12), f.FAshC)
            AplicarColorFactor(r.Cells(15), f.FL0)
            AplicarColorEstado(r.Cells(16), cumple)
        Next
    End Sub

    Private Sub MostrarALR()
        DgvALR.Rows.Clear()
        For i = 0 To _filasALR.Count - 1
            Dim f = _filasALR(i)
            Dim cumple = (f.ALR <= 0.35F OrElse f.ALR = 0)
            If ChkSoloBad.Checked AndAlso cumple Then Continue For
            Dim r = DgvALR.Rows(DgvALR.Rows.Add())
            If i Mod 2 = 1 Then r.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            r.Cells(0).Value = f.Elemento
            r.Cells(1).Value = f.Piso
            r.Cells(2).Value = f.fc
            r.Cells(3).Value = Math.Round(f.B, 3)
            r.Cells(4).Value = Math.Round(f.H, 3)
            r.Cells(5).Value = f.Combinacion
            r.Cells(6).Value = If(f.Pu > 0, CObj(Math.Round(f.Pu, 1)), "—")
            r.Cells(7).Value = If(f.Ag > 0, CObj(Math.Round(f.Ag, 4)), "—")
            r.Cells(8).Value = If(f.ALR > 0, Math.Round(f.ALR, 3).ToString("F3"), "—")
            r.Cells(9).Value = If(f.ALR <= 0.35F, "OK", If(f.ALR <= 0.40F, "Alerta", "Revisar"))
            AplicarColorALR(r.Cells(8), f.ALR)
            AplicarColorALREst(r.Cells(9), f.ALR)
        Next
    End Sub

    Private Sub MostrarResumen()
        DgvResumen.Rows.Clear()
        Dim total = _filasResumen.Count
        Dim conObs = _filasResumen.Where(Function(x) Not x.Cumple).Count()
        For i = 0 To _filasResumen.Count - 1
            Dim f = _filasResumen(i)
            If ChkSoloBad.Checked AndAlso f.Cumple Then Continue For
            Dim r = DgvResumen.Rows(DgvResumen.Rows.Add())
            If i Mod 2 = 1 Then r.DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            r.Cells(0).Value = f.Elemento
            r.Cells(1).Value = f.Secciones
            r.Cells(2).Value = If(f.FFlexMin < 0, "Sin calc.", Math.Round(f.FFlexMin, 2).ToString("F2"))
            r.Cells(3).Value = f.PisoCritFlex
            r.Cells(4).Value = If(f.FV2Min < 0, "Sin calc.", Math.Round(f.FV2Min, 2).ToString("F2"))
            r.Cells(5).Value = If(f.FV3Min < 0, "Sin calc.", Math.Round(f.FV3Min, 2).ToString("F2"))
            r.Cells(6).Value = f.PisoCritCort
            r.Cells(7).Value = If(f.FAshLMin < 0, "Sin calc.", Math.Round(f.FAshLMin, 2).ToString("F2"))
            r.Cells(8).Value = If(f.FAshCMin < 0, "Sin calc.", Math.Round(f.FAshCMin, 2).ToString("F2"))
            r.Cells(9).Value = If(f.FL0Min < 0, "Sin calc.", Math.Round(f.FL0Min, 2).ToString("F2"))
            r.Cells(10).Value = If(f.ALRMax > 0, Math.Round(f.ALRMax, 3).ToString("F3"), "—")
            r.Cells(11).Value = If(f.Cumple, "OK", "Revisar")
            AplicarColorFactor(r.Cells(2), f.FFlexMin)
            AplicarColorFactor(r.Cells(4), f.FV2Min)
            AplicarColorFactor(r.Cells(5), f.FV3Min)
            AplicarColorFactor(r.Cells(7), f.FAshLMin)
            AplicarColorFactor(r.Cells(8), f.FAshCMin)
            AplicarColorFactor(r.Cells(9), f.FL0Min)
            AplicarColorALR(r.Cells(10), f.ALRMax)
            AplicarColorEstado(r.Cells(11), f.Cumple)
        Next
        _lblConteo.Text = $"{total} elementos  |  {conObs} con observaciones  |  {total - conObs} OK"
    End Sub

    ' ── Color helpers ──────────────────────────────────────────────────────
    Private Sub AplicarColorFactor(cell As DataGridViewCell, factor As Single)
        If factor <= 0 Then
            cell.Style.BackColor = Color.FromArgb(238, 238, 238)
            cell.Style.ForeColor = Color.Gray
        ElseIf factor >= 0.9F Then
            cell.Style.BackColor = ClOK : cell.Style.ForeColor = ClOKTxt
        Else
            cell.Style.BackColor = ClMal : cell.Style.ForeColor = ClMalTxt
            cell.Style.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        End If
    End Sub

    Private Sub AplicarColorALR(cell As DataGridViewCell, alr As Single)
        If alr <= 0 Then Return
        If alr <= 0.35F Then
            cell.Style.BackColor = ClOK : cell.Style.ForeColor = ClOKTxt
        ElseIf alr <= 0.40F Then
            cell.Style.BackColor = ClAlerta : cell.Style.ForeColor = ClAlertaTxt
        Else
            cell.Style.BackColor = ClMal : cell.Style.ForeColor = ClMalTxt
        End If
    End Sub

    Private Sub AplicarColorALREst(cell As DataGridViewCell, alr As Single)
        If alr <= 0 Then Return
        If alr <= 0.35F Then
            cell.Style.BackColor = ClOK : cell.Style.ForeColor = ClOKTxt
        ElseIf alr <= 0.40F Then
            cell.Style.BackColor = ClAlerta : cell.Style.ForeColor = ClAlertaTxt
            cell.Style.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        Else
            cell.Style.BackColor = ClMal : cell.Style.ForeColor = ClMalTxt
            cell.Style.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        End If
    End Sub

    Private Sub AplicarColorEstado(cell As DataGridViewCell, cumple As Boolean)
        cell.Style.BackColor = If(cumple, ClOK, ClMal)
        cell.Style.ForeColor = If(cumple, ClOKTxt, ClMalTxt)
        cell.Style.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
    End Sub

    ' ── Eventos ────────────────────────────────────────────────────────────
    Private Sub ChkSoloBad_CheckedChanged(sender As Object, e As EventArgs) Handles ChkSoloBad.CheckedChanged
        MostrarFlex()
        MostrarCortante()
        MostrarConfinamiento()
        MostrarALR()
        MostrarResumen()
    End Sub

    Private Sub BtnExport_Click(sender As Object, e As EventArgs) Handles BtnExport.Click
        Dim dlg As New SaveFileDialog With {
            .Filter = "Excel (*.xlsx)|*.xlsx",
            .FileName = "Reporte_Columnas.xlsx"
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return
        Try
            Using wb As New XLWorkbook()
                ExportarHojaFlex(wb.Worksheets.Add("Flexo-Compresion"))
                ExportarHojaCortante(wb.Worksheets.Add("Cortante"))
                ExportarHojaConfinamiento(wb.Worksheets.Add("Confinamiento"))
                ExportarHojaALR(wb.Worksheets.Add("ALR"))
                ExportarHojaResumen(wb.Worksheets.Add("Resumen Ejecutivo"))
                wb.SaveAs(dlg.FileName)
            End Using
            MessageBox.Show("Exportado correctamente.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ── Exportación Excel ──────────────────────────────────────────────────
    Private Sub Enc(ws As IXLWorksheet, headers As String())
        For j = 0 To headers.Length - 1
            Dim c = ws.Cell(1, j + 1)
            c.Value = headers(j)
            c.Style.Fill.BackgroundColor = XlClEnc
            c.Style.Font.FontColor = XLColor.White
            c.Style.Font.Bold = True
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        Next
    End Sub

    Private Sub FXL(cell As IXLCell, factor As Single)
        If factor <= 0 Then Return
        If factor >= 0.9F Then
            cell.Style.Fill.BackgroundColor = XlClOK : cell.Style.Font.FontColor = XlClOKTxt
        Else
            cell.Style.Fill.BackgroundColor = XlClMal : cell.Style.Font.FontColor = XlClMalTxt : cell.Style.Font.Bold = True
        End If
    End Sub

    Private Sub ALRXL(cell As IXLCell, alr As Single)
        If alr <= 0 Then Return
        If alr <= 0.35F Then
            cell.Style.Fill.BackgroundColor = XlClOK : cell.Style.Font.FontColor = XlClOKTxt
        ElseIf alr <= 0.40F Then
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEB9C")
            cell.Style.Font.FontColor = XLColor.FromHtml("#9C5700")
        Else
            cell.Style.Fill.BackgroundColor = XlClMal : cell.Style.Font.FontColor = XlClMalTxt
        End If
    End Sub

    Private Sub ExportarHojaFlex(ws As IXLWorksheet)
        Enc(ws, {"Elemento", "Piso", "B (m)", "H (m)", "f'c (MPa)",
                 "As Req Top (mm2)", "As Col Top (mm2)", "F Flex Top",
                 "As Req Bot (mm2)", "As Col Bot (mm2)", "F Flex Bot", "Estado"})
        Dim r = 2
        For Each f In _filasFlex
            ws.Cell(r, 1).Value = f.Elemento : ws.Cell(r, 2).Value = f.Piso
            ws.Cell(r, 3).Value = CDbl(Math.Round(f.B, 3)) : ws.Cell(r, 4).Value = CDbl(Math.Round(f.H, 3))
            ws.Cell(r, 5).Value = CDbl(f.fc)
            ws.Cell(r, 6).Value = CDbl(Math.Round(f.AsReqTop, 0)) : ws.Cell(r, 7).Value = CDbl(Math.Round(f.AsColTop, 0))
            ws.Cell(r, 8).Value = If(f.FFlexTop > 0, CObj(CDbl(Math.Round(f.FFlexTop, 2))), "-")
            ws.Cell(r, 9).Value = CDbl(Math.Round(f.AsReqBot, 0)) : ws.Cell(r, 10).Value = CDbl(Math.Round(f.AsColBot, 0))
            ws.Cell(r, 11).Value = If(f.FFlexBot > 0, CObj(CDbl(Math.Round(f.FFlexBot, 2))), "-")
            Dim ok = (f.FFlexTop >= 0.9F OrElse f.FFlexTop = 0) AndAlso (f.FFlexBot >= 0.9F OrElse f.FFlexBot = 0)
            ws.Cell(r, 12).Value = If(ok, "OK", "Revisar")
            FXL(ws.Cell(r, 8), f.FFlexTop) : FXL(ws.Cell(r, 11), f.FFlexBot)
            r += 1
        Next
        ws.Columns().AdjustToContents() : ws.SheetView.FreezeRows(1)
    End Sub

    Private Sub ExportarHojaCortante(ws As IXLWorksheet)
        Enc(ws, {"Elemento", "Piso", "B (m)", "H (m)", "Sep ZC (m)", "Barra",
                 "Ram. L", "Ram. C", "Vu2 (kN)", "Vc2 (kN)", "Vn2 (kN)", "F V2",
                 "Vu3 (kN)", "Vc3 (kN)", "Vn3 (kN)", "F V3", "Estado"})
        Dim r = 2
        For Each f In _filasCortante
            ws.Cell(r, 1).Value = f.Elemento : ws.Cell(r, 2).Value = f.Piso
            ws.Cell(r, 3).Value = CDbl(Math.Round(f.B, 3)) : ws.Cell(r, 4).Value = CDbl(Math.Round(f.H, 3))
            ws.Cell(r, 5).Value = If(f.SepZC > 0, CObj(CDbl(Math.Round(f.SepZC, 3))), "-")
            ws.Cell(r, 6).Value = If(f.Barra IsNot Nothing, f.Barra, "-")
            ws.Cell(r, 7).Value = f.RamasL : ws.Cell(r, 8).Value = f.RamasC
            ws.Cell(r, 9).Value = If(f.Vu2 > 0, CObj(CDbl(Math.Round(f.Vu2, 2))), "-")
            ws.Cell(r, 10).Value = If(f.Vc2 > 0, CObj(CDbl(Math.Round(f.Vc2, 2))), "-")
            ws.Cell(r, 11).Value = If(f.Vn2 > 0, CObj(CDbl(Math.Round(f.Vn2, 2))), "-")
            ws.Cell(r, 12).Value = If(f.FV2 > 0, CObj(CDbl(Math.Round(f.FV2, 2))), "-")
            ws.Cell(r, 13).Value = If(f.Vu3 > 0, CObj(CDbl(Math.Round(f.Vu3, 2))), "-")
            ws.Cell(r, 14).Value = If(f.Vc3 > 0, CObj(CDbl(Math.Round(f.Vc3, 2))), "-")
            ws.Cell(r, 15).Value = If(f.Vn3 > 0, CObj(CDbl(Math.Round(f.Vn3, 2))), "-")
            ws.Cell(r, 16).Value = If(f.FV3 > 0, CObj(CDbl(Math.Round(f.FV3, 2))), "-")
            Dim ok = (f.FV2 >= 0.9F OrElse f.FV2 = 0) AndAlso (f.FV3 >= 0.9F OrElse f.FV3 = 0)
            ws.Cell(r, 17).Value = If(ok, "OK", "Revisar")
            FXL(ws.Cell(r, 12), f.FV2) : FXL(ws.Cell(r, 16), f.FV3)
            r += 1
        Next
        ws.Columns().AdjustToContents() : ws.SheetView.FreezeRows(1)
    End Sub

    Private Sub ExportarHojaConfinamiento(ws As IXLWorksheet)
        Enc(ws, {"Elemento", "Piso", "B (m)", "H (m)", "B. Long Min",
                 "S0_L (m)", "Ash_L Req (mm2)", "Ash_L Prov (mm2)", "F Ash L",
                 "S0_C (m)", "Ash_C Req (mm2)", "Ash_C Prov (mm2)", "F Ash C",
                 "L0 Req (m)", "L0 Prov (m)", "F L0", "Estado"})
        Dim r = 2
        For Each f In _filasConf
            ws.Cell(r, 1).Value = f.Elemento : ws.Cell(r, 2).Value = f.Piso
            ws.Cell(r, 3).Value = CDbl(Math.Round(f.B, 3)) : ws.Cell(r, 4).Value = CDbl(Math.Round(f.H, 3))
            ws.Cell(r, 5).Value = If(f.BarraLongMin IsNot Nothing, f.BarraLongMin, "-")
            ws.Cell(r, 6).Value = If(f.S0_L > 0, CObj(CDbl(Math.Round(f.S0_L, 3))), "-")
            ws.Cell(r, 7).Value = If(f.AshLReq > 0, CObj(CDbl(Math.Round(f.AshLReq, 1))), "-")
            ws.Cell(r, 8).Value = If(f.AshLProv > 0, CObj(CDbl(Math.Round(f.AshLProv, 1))), "-")
            ws.Cell(r, 9).Value = If(f.FAshL > 0, CObj(CDbl(Math.Round(f.FAshL, 2))), "-")
            ws.Cell(r, 10).Value = If(f.S0_C > 0, CObj(CDbl(Math.Round(f.S0_C, 3))), "-")
            ws.Cell(r, 11).Value = If(f.AshCReq > 0, CObj(CDbl(Math.Round(f.AshCReq, 1))), "-")
            ws.Cell(r, 12).Value = If(f.AshCProv > 0, CObj(CDbl(Math.Round(f.AshCProv, 1))), "-")
            ws.Cell(r, 13).Value = If(f.FAshC > 0, CObj(CDbl(Math.Round(f.FAshC, 2))), "-")
            ws.Cell(r, 14).Value = If(f.L0Req > 0, CObj(CDbl(Math.Round(f.L0Req, 3))), "-")
            ws.Cell(r, 15).Value = If(f.L0Prov > 0, CObj(CDbl(Math.Round(f.L0Prov, 3))), "-")
            ws.Cell(r, 16).Value = If(f.FL0 > 0, CObj(CDbl(Math.Round(f.FL0, 2))), "-")
            Dim ok = (f.FAshL >= 0.9F OrElse f.FAshL = 0) AndAlso (f.FAshC >= 0.9F OrElse f.FAshC = 0) AndAlso
                     (f.FL0 >= 0.9F OrElse f.FL0 = 0)
            ws.Cell(r, 17).Value = If(ok, "OK", "Revisar")
            FXL(ws.Cell(r, 9), f.FAshL) : FXL(ws.Cell(r, 13), f.FAshC)
            FXL(ws.Cell(r, 16), f.FL0)
            r += 1
        Next
        ws.Columns().AdjustToContents() : ws.SheetView.FreezeRows(1)
    End Sub

    Private Sub ExportarHojaALR(ws As IXLWorksheet)
        Enc(ws, {"Elemento", "Piso", "f'c (MPa)", "B (m)", "H (m)",
                 "Combinacion", "Pu (kN)", "Ag (m2)", "ALR", "Estado"})
        Dim r = 2
        For Each f In _filasALR
            ws.Cell(r, 1).Value = f.Elemento : ws.Cell(r, 2).Value = f.Piso
            ws.Cell(r, 3).Value = CDbl(f.fc)
            ws.Cell(r, 4).Value = CDbl(Math.Round(f.B, 3)) : ws.Cell(r, 5).Value = CDbl(Math.Round(f.H, 3))
            ws.Cell(r, 6).Value = f.Combinacion
            ws.Cell(r, 7).Value = If(f.Pu > 0, CObj(CDbl(Math.Round(f.Pu, 1))), "-")
            ws.Cell(r, 8).Value = If(f.Ag > 0, CObj(CDbl(Math.Round(f.Ag, 4))), "-")
            ws.Cell(r, 9).Value = If(f.ALR > 0, CObj(CDbl(Math.Round(f.ALR, 3))), "-")
            ws.Cell(r, 10).Value = If(f.ALR <= 0.35F, "OK", If(f.ALR <= 0.40F, "Alerta", "Revisar"))
            ALRXL(ws.Cell(r, 9), f.ALR)
            r += 1
        Next
        ws.Columns().AdjustToContents() : ws.SheetView.FreezeRows(1)
    End Sub

    Private Sub ExportarHojaResumen(ws As IXLWorksheet)
        Enc(ws, {"Elemento", "Secc.", "F Flex min", "Piso Flex",
                 "F Cort V2", "F Cort V3", "Piso Cort",
                 "F Ash L", "F Ash C", "F L0", "ALR max", "Estado"})
        Dim r = 2
        For Each f In _filasResumen
            ws.Cell(r, 1).Value = f.Elemento : ws.Cell(r, 2).Value = f.Secciones
            ws.Cell(r, 3).Value = If(f.FFlexMin < 0, "Sin calc.", CObj(CDbl(Math.Round(f.FFlexMin, 2))))
            ws.Cell(r, 4).Value = f.PisoCritFlex
            ws.Cell(r, 5).Value = If(f.FV2Min < 0, "Sin calc.", CObj(CDbl(Math.Round(f.FV2Min, 2))))
            ws.Cell(r, 6).Value = If(f.FV3Min < 0, "Sin calc.", CObj(CDbl(Math.Round(f.FV3Min, 2))))
            ws.Cell(r, 7).Value = f.PisoCritCort
            ws.Cell(r, 8).Value = If(f.FAshLMin < 0, "Sin calc.", CObj(CDbl(Math.Round(f.FAshLMin, 2))))
            ws.Cell(r, 9).Value = If(f.FAshCMin < 0, "Sin calc.", CObj(CDbl(Math.Round(f.FAshCMin, 2))))
            ws.Cell(r, 10).Value = If(f.FL0Min < 0, "Sin calc.", CObj(CDbl(Math.Round(f.FL0Min, 2))))
            ws.Cell(r, 11).Value = If(f.ALRMax > 0, CObj(CDbl(Math.Round(f.ALRMax, 3))), "-")
            ws.Cell(r, 12).Value = If(f.Cumple, "OK", "Revisar")
            FXL(ws.Cell(r, 3), f.FFlexMin) : FXL(ws.Cell(r, 5), f.FV2Min) : FXL(ws.Cell(r, 6), f.FV3Min)
            FXL(ws.Cell(r, 8), f.FAshLMin) : FXL(ws.Cell(r, 9), f.FAshCMin)
            FXL(ws.Cell(r, 10), f.FL0Min)
            ALRXL(ws.Cell(r, 11), f.ALRMax)
            r += 1
        Next
        ws.Columns().AdjustToContents() : ws.SheetView.FreezeRows(1)
    End Sub

    ' ── Clases de datos ────────────────────────────────────────────────────
    Private Class FilaFlex
        Public Property Elemento As String
        Public Property Piso As String
        Public Property B As Single
        Public Property H As Single
        Public Property fc As Single
        Public Property AsReqTop As Single
        Public Property AsColTop As Single
        Public Property FFlexTop As Single
        Public Property AsReqBot As Single
        Public Property AsColBot As Single
        Public Property FFlexBot As Single
    End Class

    Private Class FilaCortante
        Public Property Elemento As String
        Public Property Piso As String
        Public Property B As Single
        Public Property H As Single
        Public Property SepZC As Single
        Public Property Barra As String
        Public Property RamasL As Integer
        Public Property RamasC As Integer
        Public Property Vu2 As Single
        Public Property Vc2 As Single
        Public Property Vn2 As Single
        Public Property FV2 As Single
        Public Property Vu3 As Single
        Public Property Vc3 As Single
        Public Property Vn3 As Single
        Public Property FV3 As Single
    End Class

    Private Class FilaConfinamiento
        Public Property Elemento As String
        Public Property Piso As String
        Public Property B As Single
        Public Property H As Single
        Public Property BarraLongMin As String
        Public Property S0_L As Single
        Public Property AshLReq As Single
        Public Property AshLProv As Single
        Public Property FAshL As Single
        Public Property S0_C As Single
        Public Property AshCReq As Single
        Public Property AshCProv As Single
        Public Property FAshC As Single
        Public Property L0Req As Single
        Public Property L0Prov As Single
        Public Property FL0 As Single
    End Class

    Private Class FilaALR
        Public Property Elemento As String
        Public Property Piso As String
        Public Property fc As Single
        Public Property B As Single
        Public Property H As Single
        Public Property Combinacion As String
        Public Property Pu As Single
        Public Property Ag As Single
        Public Property ALR As Single
    End Class

    Private Class FilaResumen
        Public Property Elemento As String
        Public Property Secciones As Integer
        Public Property FFlexMin As Single
        Public Property PisoCritFlex As String
        Public Property FV2Min As Single
        Public Property FV3Min As Single
        Public Property PisoCritCort As String
        Public Property FAshLMin As Single
        Public Property FAshCMin As Single
        Public Property FL0Min As Single
        Public Property ALRMax As Single
        Public Property Cumple As Boolean
    End Class

End Class
