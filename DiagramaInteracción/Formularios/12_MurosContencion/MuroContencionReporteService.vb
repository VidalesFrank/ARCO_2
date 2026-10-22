Imports ClosedXML.Excel
Imports System.IO

' ═══════════════════════════════════════════════════════════════════════════════
'  MuroContencionReporteService — exporta los resultados a .xlsx.
'
'  Estructura del libro:
'    1. Resumen       — portada + semáforo global de veredictos.
'    2. Geometría     — tabla de parámetros + dimensiones derivadas.
'    3. Cargas        — suelo + materiales + sismo + sobrecarga + coeficientes.
'    4. Pesos         — lista de cuerpos con peso, brazo, momento resistente.
'    5. Estabilidad   — chequeos (vol/des/qmax/exc) por caso estático y sísmico.
'    6. Diseño        — refuerzo por elemento con C/D de flexión y cortante.
'
'  Estilos: reusa ReporteHelpers (encabezado gris ARCO, verde/rojo de veredictos,
'  EscribirFactor con semáforo, EscribirValor/EscribirNumero para magnitudes).
' ═══════════════════════════════════════════════════════════════════════════════
Public Class MuroContencionReporteService

    Private Const UMBRAL As Double = Funciones_00_Varias.UMBRAL_CD

    Public Shared Sub Exportar(muro As MuroContencion, rutaSalida As String)
        If muro Is Nothing Then Throw New ArgumentNullException(NameOf(muro))
        If muro.Resultados Is Nothing Then
            Throw New InvalidOperationException("El muro no tiene resultados — ejecute Analizar antes de exportar.")
        End If

        Using wb As New XLWorkbook()
            EscribirResumen(wb, muro)
            EscribirGeometria(wb, muro)
            EscribirCargas(wb, muro)
            EscribirPesos(wb, muro)
            EscribirEstabilidad(wb, muro)
            EscribirDiseno(wb, muro)
            wb.SaveAs(rutaSalida)
        End Using
    End Sub

    ' =====================================================================
    ' Hoja 1 — Resumen (portada + semáforo global)
    ' =====================================================================
    Private Shared Sub EscribirResumen(wb As XLWorkbook, muro As MuroContencion)
        Dim ws = wb.Worksheets.Add("Resumen")
        Dim info = muro.Info

        ' Título
        ws.Cell(1, 1).Value = "INFORME DE REVISIÓN — MURO DE CONTENCIÓN"
        ws.Range(1, 1, 1, 4).Merge()
        ws.Cell(1, 1).Style.Font.Bold = True
        ws.Cell(1, 1).Style.Font.FontSize = 14
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(1).Height = 28

        ' Datos del proyecto (dos columnas: campo + valor)
        Dim f As Integer = 3
        EscribirCampo(ws, f, "Nombre del muro", info.Nombre) : f += 1
        EscribirCampo(ws, f, "Proyecto", info.Proyecto) : f += 1
        EscribirCampo(ws, f, "Cliente", info.Cliente) : f += 1
        EscribirCampo(ws, f, "Ubicación", info.Ubicacion) : f += 1
        EscribirCampo(ws, f, "Responsable", info.Responsable) : f += 1
        EscribirCampo(ws, f, "Código", info.Codigo) : f += 1
        EscribirCampo(ws, f, "Fecha", info.Fecha.ToString("dd 'de' MMMM 'de' yyyy",
                                                            Globalization.CultureInfo.GetCultureInfo("es-CO"))) : f += 1
        EscribirCampo(ws, f, "Normativa",
                       If(muro.Normativa = eNormativaMuro.Vial,
                          "Vial (CCP-14 / AASHTO LRFD)",
                          "Residencial (NSR-10)")) : f += 1
        If Not String.IsNullOrWhiteSpace(info.Descripcion) Then
            EscribirCampo(ws, f, "Descripción", info.Descripcion) : f += 1
        End If

        ' --- Semáforo de veredictos (tabla con 1 fila por caso) ----------
        f += 2
        ws.Cell(f, 1).Value = "VEREDICTO DE REVISIÓN"
        ws.Range(f, 1, f, 6).Merge()
        ws.Cell(f, 1).Style.Font.Bold = True
        ws.Cell(f, 1).Style.Font.FontSize = 12
        ws.Cell(f, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(f, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(f, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(f).Height = 24
        f += 1

        ReporteHelpers.EscribirEncabezados(ws, f,
            {"Caso", "Volcamiento", "Deslizamiento", "Cap. portante", "Excentricidad", "Veredicto"})
        f += 1

        EscribirFilaEstabilidadResumen(ws, f, "Estático", muro.Resultados.Estabilidad.CasoEstatico) : f += 1
        If muro.Resultados.Estabilidad.CasoSismico IsNot Nothing Then
            EscribirFilaEstabilidadResumen(ws, f, "Sísmico", muro.Resultados.Estabilidad.CasoSismico) : f += 1
        End If

        ' Fila final: veredicto global del DISEÑO
        f += 1
        ws.Cell(f, 1).Value = "DISEÑO DE REFUERZO"
        ws.Range(f, 1, f, 6).Merge()
        ws.Cell(f, 1).Style.Font.Bold = True
        ws.Cell(f, 1).Style.Font.FontSize = 12
        ws.Cell(f, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(f, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(f, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(f).Height = 24
        f += 1

        ReporteHelpers.EscribirEncabezados(ws, f, {"Elemento", "C/D Flexión", "C/D Cortante", "Veredicto"})
        f += 1

        Dim dis = muro.Resultados.Disenos
        For Each d In NullSafeDisenos(dis)
            ws.Cell(f, 1).Value = d.Nombre
            ReporteHelpers.EscribirFactor(ws.Cell(f, 2), d.FactorFlexion, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirFactor(ws.Cell(f, 3), d.FactorCortante, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirEstado(ws.Cell(f, 4), d.CumpleTodo, "Cumple", "Revisar")
            f += 1
        Next

        ws.Columns(1, 6).AdjustToContents()
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 22)
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 18)
    End Sub

    Private Shared Sub EscribirFilaEstabilidadResumen(ws As IXLWorksheet, fila As Integer, caso As String, c As ChequeoCaso)
        ws.Cell(fila, 1).Value = caso
        ws.Cell(fila, 1).Style.Font.Bold = True
        ws.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ReporteHelpers.EscribirFactor(ws.Cell(fila, 2), c.FactorVolcamiento, ReporteHelpers.SinDato.CeroOMenor)
        ReporteHelpers.EscribirFactor(ws.Cell(fila, 3), c.FactorDeslizamiento, ReporteHelpers.SinDato.CeroOMenor)
        ReporteHelpers.EscribirFactor(ws.Cell(fila, 4), c.FactorCapacidadPortante, ReporteHelpers.SinDato.CeroOMenor)
        ReporteHelpers.EscribirFactor(ws.Cell(fila, 5), c.FactorExcentricidad, ReporteHelpers.SinDato.CeroOMenor)
        ReporteHelpers.EscribirEstado(ws.Cell(fila, 6), c.CumpleTodo, "Cumple", "Revisar")
    End Sub

    ' =====================================================================
    ' Hoja 2 — Geometría
    ' =====================================================================
    Private Shared Sub EscribirGeometria(wb As XLWorkbook, muro As MuroContencion)
        Dim ws = wb.Worksheets.Add("Geometría")
        Dim g = muro.Geometria
        Dim talonLargo = Math.Max(g.B - g.b1 - g.e1, 0)

        TituloHoja(ws, "GEOMETRÍA DEL MURO")

        Dim f As Integer = 3
        ReporteHelpers.EscribirEncabezados(ws, f, {"Parámetro", "Símbolo", "Valor", "Unidad"})
        f += 1

        FilaGeo(ws, f, "Altura total", "H", g.H, "m") : f += 1
        FilaGeo(ws, f, "Espesor de la losa (zapata)", "h₁", g.h1, "m") : f += 1
        FilaGeo(ws, f, "Altura del vástago", "h₂", g.h2, "m") : f += 1
        FilaGeo(ws, f, "Ancho total de la zapata", "B", g.B, "m") : f += 1
        FilaGeo(ws, f, "Puntera (pie → cara frontal vástago)", "b₁", g.b1, "m") : f += 1
        FilaGeo(ws, f, "Espesor del vástago en la base", "e₁", g.e1, "m") : f += 1
        FilaGeo(ws, f, "Espesor del vástago en la corona", "e₂", g.e2, "m") : f += 1
        FilaGeo(ws, f, "Talón (derivado = B − b₁ − e₁)", "", talonLargo, "m") : f += 1

        If muro.Opciones.IncluirLlave Then
            f += 1
            ws.Cell(f, 1).Value = "LLAVE DE CORTANTE"
            ws.Range(f, 1, f, 4).Merge()
            ws.Cell(f, 1).Style.Font.Bold = True
            ws.Cell(f, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
            ws.Cell(f, 1).Style.Font.FontColor = XLColor.White
            ws.Cell(f, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            f += 1
            FilaGeo(ws, f, "Ancho de la llave", "B_LL", g.B_LL, "m") : f += 1
            FilaGeo(ws, f, "Profundidad bajo la zapata", "H_LL", g.H_LL, "m") : f += 1
        End If

        ' Nota técnica sobre la convención
        f += 2
        ws.Cell(f, 1).Value = "Nota: la cara TRASERA del vástago (lado del relleno) es vertical. " &
                               "Cuando e₁ ≠ e₂, la cara DELANTERA se inclina (mejor transmisión de cargas)."
        ws.Range(f, 1, f, 4).Merge()
        ws.Cell(f, 1).Style.Alignment.WrapText = True
        ws.Cell(f, 1).Style.Font.Italic = True
        ws.Cell(f, 1).Style.Font.FontColor = XLColor.FromHtml("#555555")

        ws.Columns(1, 4).AdjustToContents()
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 40)
    End Sub

    Private Shared Sub FilaGeo(ws As IXLWorksheet, fila As Integer, nombre As String, simbolo As String, valor As Double, unidad As String)
        ws.Cell(fila, 1).Value = nombre
        ws.Cell(fila, 2).Value = simbolo
        ws.Cell(fila, 2).Style.Font.Bold = True
        ws.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ReporteHelpers.EscribirNumero(ws.Cell(fila, 3), valor, 3)
        ws.Cell(fila, 4).Value = unidad
        ws.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Cell(fila, 4).Style.Font.FontColor = XLColor.FromHtml("#777777")
    End Sub

    ' =====================================================================
    ' Hoja 3 — Cargas (suelo, materiales, sismo, sobrecarga, coeficientes)
    ' =====================================================================
    Private Shared Sub EscribirCargas(wb As XLWorkbook, muro As MuroContencion)
        Dim ws = wb.Worksheets.Add("Cargas")
        Dim s = muro.Suelo
        Dim r = muro.Resultados

        TituloHoja(ws, "CARGAS, MATERIALES Y COEFICIENTES")

        Dim f As Integer = 3

        BloqueCarga(ws, f, "SUELO") : f += 1
        FilaGeo(ws, f, "Ángulo de fricción interna", "φ", s.Phi, "°") : f += 1
        FilaGeo(ws, f, "Fricción muro-suelo", "δ", s.Delta, "°") : f += 1
        FilaGeo(ws, f, "Peso específico del relleno", "γ", s.GammaSuelo, "kN/m³") : f += 1
        FilaGeo(ws, f, "Peso específico cimentación", "γ_c", s.GammaCimentacion, "kN/m³") : f += 1
        FilaGeo(ws, f, "Cohesión", "c", s.Cohesion, "kPa") : f += 1
        FilaGeo(ws, f, "Profundidad de desplante", "Df", s.Df, "m") : f += 1
        FilaGeo(ws, f, "Capacidad portante admisible", "q_adm", s.CapacidadPortante, "kPa") : f += 1
        FilaGeo(ws, f, "Coef. fricción base-suelo", "μ",
                 If(s.MuBase > 0, s.MuBase, Math.Tan(s.Phi * 2 / 3 * Math.PI / 180)), "—") : f += 1
        ws.Cell(f, 1).Value = "Teoría de empuje"
        ws.Cell(f, 2).Value = If(s.Teoria = eTeoriaEmpuje.Coulomb, "Coulomb", "Rankine")
        ws.Cell(f, 2).Style.Font.Bold = True
        f += 2

        BloqueCarga(ws, f, "MATERIALES") : f += 1
        FilaGeo(ws, f, "Resistencia concreto", "f'c", muro.Materiales.Fc, "MPa") : f += 1
        FilaGeo(ws, f, "Fluencia acero", "fy", muro.Materiales.Fy, "MPa") : f += 1
        FilaGeo(ws, f, "Peso específico concreto", "γ_c", muro.Materiales.PesoConcreto, "kN/m³") : f += 1
        FilaGeo(ws, f, "Recubrimiento", "r", muro.Materiales.Recubrimiento, "m") : f += 2

        If muro.SueloSismico.IncluirSismo Then
            BloqueCarga(ws, f, "SISMO (Mononobe-Okabe)") : f += 1
            FilaGeo(ws, f, "Coef. sísmico horizontal", "Kh", muro.SueloSismico.Kh, "—") : f += 1
            FilaGeo(ws, f, "Coef. sísmico vertical", "Kv", muro.SueloSismico.Kv, "—") : f += 2
        End If

        If muro.Sobrecarga.IncluirSobrecarga Then
            BloqueCarga(ws, f, "SOBRECARGA") : f += 1
            FilaGeo(ws, f, "Sobrecarga variable", "q", muro.Sobrecarga.SobrecargaKPa, "kPa") : f += 1
            FilaGeo(ws, f, "Distancia al tráfico", "d", muro.Sobrecarga.DistanciaTraficoMm, "mm") : f += 1
            FilaGeo(ws, f, "Altura equivalente", "H_eq",
                     MuroContencionService.HeqAASHTO(muro.Geometria.H, muro.Sobrecarga.DistanciaTraficoMm), "m") : f += 2
        End If

        BloqueCarga(ws, f, "COEFICIENTES CALCULADOS") : f += 1
        FilaGeo(ws, f, "Coef. empuje activo", "Ka", r.Ka, "—") : f += 1
        FilaGeo(ws, f, "Coef. empuje pasivo", "Kp", r.Kp, "—") : f += 1
        If muro.SueloSismico.IncluirSismo Then
            FilaGeo(ws, f, "Coef. activo sísmico (M-O)", "Kae", r.Kae, "—") : f += 1
        End If

        ws.Columns(1, 4).AdjustToContents()
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 36)
    End Sub

    Private Shared Sub BloqueCarga(ws As IXLWorksheet, fila As Integer, titulo As String)
        ws.Cell(fila, 1).Value = titulo
        ws.Range(fila, 1, fila, 4).Merge()
        ws.Cell(fila, 1).Style.Font.Bold = True
        ws.Cell(fila, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(fila, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(fila).Height = 22
    End Sub

    ' =====================================================================
    ' Hoja 4 — Pesos
    ' =====================================================================
    Private Shared Sub EscribirPesos(wb As XLWorkbook, muro As MuroContencion)
        Dim ws = wb.Worksheets.Add("Pesos")
        TituloHoja(ws, "PESOS DE LOS CUERPOS DEL MURO")

        Dim f As Integer = 3
        ReporteHelpers.EscribirEncabezados(ws, f,
            {"Cuerpo", "Área (m²)", "γ (kN/m³)", "Peso (kN/m)", "Brazo X (m)", "M resistente (kN·m/m)"})
        f += 1

        Dim totalPeso As Double = 0
        Dim totalMR As Double = 0
        For Each p In muro.Resultados.Pesos
            ws.Cell(f, 1).Value = p.Nombre
            ReporteHelpers.EscribirNumero(ws.Cell(f, 2), p.Area, 3)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 3), p.PesoEspecifico, 2)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 4), p.Peso, 2)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 5), p.BrazoX, 3)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 6), p.MomentoResistente, 2)
            totalPeso += p.Peso
            totalMR += p.MomentoResistente
            f += 1
        Next

        ' Totales
        ws.Cell(f, 1).Value = "TOTAL"
        ws.Cell(f, 1).Style.Font.Bold = True
        ReporteHelpers.EscribirNumero(ws.Cell(f, 4), totalPeso, 2)
        ReporteHelpers.EscribirNumero(ws.Cell(f, 6), totalMR, 2)
        For col As Integer = 1 To 6
            ws.Cell(f, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8")
            ws.Cell(f, col).Style.Font.Bold = True
        Next

        ws.Columns(1, 6).AdjustToContents()
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 28)
    End Sub

    ' =====================================================================
    ' Hoja 5 — Estabilidad
    ' =====================================================================
    Private Shared Sub EscribirEstabilidad(wb As XLWorkbook, muro As MuroContencion)
        Dim ws = wb.Worksheets.Add("Estabilidad")
        TituloHoja(ws, "CHEQUEOS DE ESTABILIDAD EXTERNA")

        Dim f As Integer = 3
        ReporteHelpers.EscribirEncabezados(ws, f,
            {"Caso", "Chequeo", "Calculado", "Admisible", "C/D", "Veredicto"})
        f += 1

        EscribirCasoEstabilidad(ws, f, "Estático", muro.Resultados.Estabilidad.CasoEstatico)
        f += 4
        If muro.Resultados.Estabilidad.CasoSismico IsNot Nothing Then
            EscribirCasoEstabilidad(ws, f, "Sísmico", muro.Resultados.Estabilidad.CasoSismico)
            f += 4
        End If

        ws.Columns(1, 6).AdjustToContents()
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 26)
    End Sub

    Private Shared Sub EscribirCasoEstabilidad(ws As IXLWorksheet, filaBase As Integer, nombreCaso As String, c As ChequeoCaso)
        ws.Cell(filaBase, 1).Value = nombreCaso
        ws.Range(filaBase, 1, filaBase + 3, 1).Merge()
        ws.Cell(filaBase, 1).Style.Font.Bold = True
        ws.Cell(filaBase, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Cell(filaBase, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
        ws.Cell(filaBase, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F4FA")

        Dim fila As Integer = filaBase
        FilaChequeo(ws, fila, "Volcamiento", c.FsVolcamiento, c.FsVolcamientoAdmisible,
                     c.FactorVolcamiento, c.CumpleVolcamiento) : fila += 1
        FilaChequeo(ws, fila, "Deslizamiento", c.FsDeslizamiento, c.FsDeslizamientoAdmisible,
                     c.FactorDeslizamiento, c.CumpleDeslizamiento) : fila += 1
        FilaChequeo(ws, fila, "Capacidad portante (kPa)", c.PresionMax, c.PresionAdmisible,
                     c.FactorCapacidadPortante, c.CumpleCapacidadPortante) : fila += 1
        FilaChequeo(ws, fila, "Excentricidad (m)", Math.Abs(c.Excentricidad), c.ExcentricidadAdmisible,
                     c.FactorExcentricidad, c.CumpleExcentricidad)
    End Sub

    Private Shared Sub FilaChequeo(ws As IXLWorksheet, fila As Integer, nombre As String,
                                    calc As Double, adm As Double, cd As Double, cumple As Boolean)
        ws.Cell(fila, 2).Value = nombre
        ReporteHelpers.EscribirNumero(ws.Cell(fila, 3), calc, 2)
        ReporteHelpers.EscribirNumero(ws.Cell(fila, 4), adm, 2)
        ReporteHelpers.EscribirFactor(ws.Cell(fila, 5), cd, ReporteHelpers.SinDato.CeroOMenor)
        ReporteHelpers.EscribirEstado(ws.Cell(fila, 6), cumple, "Cumple", "Revisar")
    End Sub

    ' =====================================================================
    ' Hoja 6 — Diseño
    ' =====================================================================
    Private Shared Sub EscribirDiseno(wb As XLWorkbook, muro As MuroContencion)
        Dim ws = wb.Worksheets.Add("Diseño")
        TituloHoja(ws, "DISEÑO Y REVISIÓN DE REFUERZO")

        Dim f As Integer = 3
        ReporteHelpers.EscribirEncabezados(ws, f,
            {"Elemento", "Espesor (m)", "d (m)", "Mu (kN·m)", "Vu (kN)",
             "As req (mm²/m)", "Barra", "Sep (cm)", "As prov (mm²/m)",
             "C/D Flexión", "φVc (kN)", "C/D Cortante", "Veredicto"})
        f += 1

        Dim dis = muro.Resultados.Disenos
        For Each d In NullSafeDisenos(dis)
            ws.Cell(f, 1).Value = d.Nombre
            ws.Cell(f, 1).Style.Font.Bold = True
            ReporteHelpers.EscribirNumero(ws.Cell(f, 2), d.H, 3)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 3), d.d, 3)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 4), d.Mu, 2)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 5), d.Vu, 2)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 6), d.AsReq, 0)
            ws.Cell(f, 7).Value = d.BarraPrincipal
            ws.Cell(f, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ReporteHelpers.EscribirNumero(ws.Cell(f, 8), d.SeparacionCm, 1)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 9), d.AsProv, 0)
            ReporteHelpers.EscribirFactor(ws.Cell(f, 10), d.FactorFlexion, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 11), d.PhiVc, 2)
            ReporteHelpers.EscribirFactor(ws.Cell(f, 12), d.FactorCortante, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirEstado(ws.Cell(f, 13), d.CumpleTodo, "Cumple", "Revisar")
            f += 1
        Next

        ' Refuerzo de temperatura
        f += 2
        ws.Cell(f, 1).Value = "REFUERZO POR TEMPERATURA Y RETRACCIÓN"
        ws.Range(f, 1, f, 7).Merge()
        ws.Cell(f, 1).Style.Font.Bold = True
        ws.Cell(f, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(f, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(f, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        f += 1

        ReporteHelpers.EscribirEncabezados(ws, f, {"Elemento", "As req (mm²/m)", "Barra", "Sep (cm)", "As prov (mm²/m)", "C/D"})
        f += 1
        For Each d In NullSafeDisenos(dis)
            ws.Cell(f, 1).Value = d.Nombre
            ReporteHelpers.EscribirNumero(ws.Cell(f, 2), d.AsReqTemperatura, 0)
            ws.Cell(f, 3).Value = d.BarraTemperatura
            ws.Cell(f, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ReporteHelpers.EscribirNumero(ws.Cell(f, 4), d.SeparacionTempCm, 1)
            ReporteHelpers.EscribirNumero(ws.Cell(f, 5), d.AsProvTemperatura, 0)
            ReporteHelpers.EscribirFactor(ws.Cell(f, 6), d.FactorTemperatura, ReporteHelpers.SinDato.CeroOMenor)
            f += 1
        Next

        ws.Columns(1, 13).AdjustToContents()
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 20)
    End Sub

    ' =====================================================================
    ' Helpers de hoja
    ' =====================================================================
    Private Shared Sub TituloHoja(ws As IXLWorksheet, texto As String)
        ws.Cell(1, 1).Value = texto
        ws.Range(1, 1, 1, 6).Merge()
        ws.Cell(1, 1).Style.Font.Bold = True
        ws.Cell(1, 1).Style.Font.FontSize = 13
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(1).Height = 26
    End Sub

    Private Shared Sub EscribirCampo(ws As IXLWorksheet, fila As Integer, nombre As String, valor As String)
        ws.Cell(fila, 1).Value = nombre
        ws.Cell(fila, 1).Style.Font.Bold = True
        ws.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F0F0")
        ws.Cell(fila, 2).Value = If(String.IsNullOrWhiteSpace(valor), "—", valor)
        ws.Range(fila, 2, fila, 4).Merge()
    End Sub

    Private Shared Iterator Function NullSafeDisenos(dis As DisenosElementos) As IEnumerable(Of DisenoElementoMuro)
        If dis Is Nothing Then Return
        If dis.Vastago IsNot Nothing Then Yield dis.Vastago
        If dis.Talon IsNot Nothing Then Yield dis.Talon
        If dis.Pie IsNot Nothing Then Yield dis.Pie
        If dis.Llave IsNot Nothing Then Yield dis.Llave
    End Function

End Class
