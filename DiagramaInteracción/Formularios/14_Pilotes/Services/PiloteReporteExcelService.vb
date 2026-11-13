Imports ClosedXML.Excel

''' <summary>
''' Exportación del módulo de Pilotes a Excel. Cinco hojas:
'''
'''   1. Resumen          · KPIs del proyecto + Top-20 críticos (orden por C/D mín).
'''   2. Apoyos           · Un renglón por Dado: coord, origen, dado asignado,
'''                         Pu max/min, 4 C/D (geot + estr) con semáforo, combo
'''                         gobernante, veredicto consolidado.
'''   3. Catálogo Pilotes · Un renglón por TipoPilote: geometría, capacidades
'''                         admisibles, refuerzo, cuantía, φPn, φTn.
'''   4. Catálogo Dados   · Un renglón por TipoDado: espesor, pilotes, dimensiones
'''                         del polígono, cuántos apoyos lo usan.
'''   5. Combinaciones    · Combos seleccionados de diseño vs. todas las del modelo.
'''
''' Reutiliza ReporteHelpers compartido (EscribirEncabezados, EscribirFactor,
''' EscribirValor, EstilarFilaDatos, AjustarColumnas, AgregarBordesTabla) para que
''' el estilo coincida con el resto de reportes del programa.
'''
''' Patrón ARCO: servicio puro, sin UI. El caller (ViewModel) abre SaveFileDialog,
''' nos pasa la ruta y nosotros devolvemos True/False + mensaje de error.
''' </summary>
Public Module PiloteReporteExcelService

    Public Class ResultadoExportacion
        Public Property Exito As Boolean
        Public Property Mensaje As String = ""
        Public Property NumHojas As Integer
        Public Property Ruta As String = ""
    End Class

    ''' <summary>
    ''' Genera el Excel completo. Si el modelo está vacío (sin apoyos, sin tipos),
    ''' igual se crea el archivo con las hojas y un mensaje "sin datos" en cada
    ''' una, para que el caller no tenga que validar antes.
    ''' </summary>
    Public Function Exportar(contenedor As cPilotes,
                             proyecto As Proyecto,
                             rutaDestino As String) As ResultadoExportacion
        Dim r As New ResultadoExportacion With {.Ruta = rutaDestino}
        If contenedor Is Nothing Then
            r.Mensaje = "No hay datos de pilotes en el proyecto."
            Return r
        End If
        If String.IsNullOrEmpty(rutaDestino) Then
            r.Mensaje = "Ruta de destino vacía."
            Return r
        End If

        Try
            Using wb As New XLWorkbook()
                ExportarResumen(wb, contenedor, proyecto)
                ExportarApoyos(wb, contenedor)
                ExportarCatalogoPilotes(wb, contenedor)
                ExportarCatalogoDados(wb, contenedor)
                ExportarCombinaciones(wb, contenedor)
                r.NumHojas = wb.Worksheets.Count
                wb.SaveAs(rutaDestino)
            End Using
            r.Exito = True
            r.Mensaje = $"Reporte generado correctamente ({r.NumHojas} hojas)."
        Catch ex As Exception
            Logger.Error(ex, "PiloteReporteExcelService.Exportar", rutaDestino)
            r.Mensaje = ex.Message
        End Try
        Return r
    End Function

    ' ══════════════════════════════════════════════════════════════════════
    ' Hoja 1 — Resumen
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub ExportarResumen(wb As XLWorkbook, c As cPilotes, p As Proyecto)
        Dim ws = wb.Worksheets.Add("Resumen")

        ' Encabezado del proyecto
        ws.Cell(1, 1).Value = "REPORTE DE PILOTES — ARCO"
        ws.Range(1, 1, 1, 6).Merge()
        ws.Cell(1, 1).Style.Font.Bold = True
        ws.Cell(1, 1).Style.Font.FontSize = 16
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(1).Height = 28

        Dim nombreProy As String = ""
        If p IsNot Nothing Then
            If p.Info IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(p.Info.Nombre) Then
                nombreProy = p.Info.Nombre
            ElseIf Not String.IsNullOrWhiteSpace(p.Ruta) Then
                nombreProy = IO.Path.GetFileNameWithoutExtension(p.Ruta)
            End If
        End If
        ws.Cell(2, 1).Value = "Proyecto:"
        ws.Cell(2, 1).Style.Font.Bold = True
        ws.Cell(2, 2).Value = If(String.IsNullOrEmpty(nombreProy), "—", nombreProy)
        ws.Cell(3, 1).Value = "Fecha:"
        ws.Cell(3, 1).Style.Font.Bold = True
        ws.Cell(3, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm")

        ' KPIs — tabla de 2 columnas
        Dim filaKPI As Integer = 5
        ws.Cell(filaKPI, 1).Value = "INDICADORES"
        ws.Range(filaKPI, 1, filaKPI, 2).Merge()
        ws.Cell(filaKPI, 1).Style.Font.Bold = True
        ws.Cell(filaKPI, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(filaKPI, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(filaKPI, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(filaKPI).Height = 22

        Dim total = c.Dados.Count
        Dim sinTipo = c.Dados.Where(Function(d) String.IsNullOrEmpty(d.NombreTipoDado)).Count()
        Dim umbral = PiloteService.UMBRAL_CD
        Dim cumplen = c.Dados.Where(Function(d) EsCumplimientoOk(d, umbral)).Count()
        Dim noCumplen = c.Dados.Where(Function(d) NoCumpleAlguno(d, umbral)).Count()
        Dim nTiposPilote = c.TiposPilote.Count
        Dim nTiposDado = c.TiposDado.Count
        Dim nCombosSel = If(c.ListA_Combinaciones_Design IsNot Nothing,
                             c.ListA_Combinaciones_Design.Count, 0)

        EscribirKPI(ws, filaKPI + 1, "Total apoyos", total)
        EscribirKPI(ws, filaKPI + 2, "Apoyos que cumplen", cumplen)
        EscribirKPI(ws, filaKPI + 3, "Apoyos que no cumplen (C/D < 0.90)", noCumplen)
        EscribirKPI(ws, filaKPI + 4, "Apoyos sin tipo de dado asignado", sinTipo)
        EscribirKPI(ws, filaKPI + 5, "Tipos de pilote en catálogo", nTiposPilote)
        EscribirKPI(ws, filaKPI + 6, "Tipos de dado en catálogo", nTiposDado)
        EscribirKPI(ws, filaKPI + 7, "Combinaciones de diseño seleccionadas", nCombosSel)

        ' Top-20 críticos por C/D mínimo (ignora los que no se calcularon)
        Dim filaTop As Integer = filaKPI + 9
        ws.Cell(filaTop, 1).Value = "TOP 20 APOYOS MÁS CRÍTICOS"
        ws.Range(filaTop, 1, filaTop, 8).Merge()
        ws.Cell(filaTop, 1).Style.Font.Bold = True
        ws.Cell(filaTop, 1).Style.Fill.BackgroundColor = ReporteHelpers.XlEncabezado
        ws.Cell(filaTop, 1).Style.Font.FontColor = XLColor.White
        ws.Cell(filaTop, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Row(filaTop).Height = 22

        Dim enc = {"Joint/Pier", "Origen", "Tipo dado", "Pu max [kN]",
                   "C/D geot. comp.", "C/D geot. trac.",
                   "C/D estr. comp.", "C/D estr. trac."}
        ReporteHelpers.EscribirEncabezados(ws, filaTop + 1, enc)

        Dim fila = filaTop + 2
        Dim criticos = c.Dados.
            Where(Function(d) PeorCD(d) > 0).
            OrderBy(Function(d) PeorCD(d)).
            Take(20).ToList()

        If criticos.Count = 0 Then
            ws.Cell(fila, 1).Value = "Sin datos calculados — importa apoyos y asigna tipos de dado."
            ws.Range(fila, 1, fila, enc.Length).Merge()
            ws.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ws.Cell(fila, 1).Style.Font.Italic = True
        Else
            For Each d In criticos
                ws.Cell(fila, 1).Value = d.JointLabel
                ws.Cell(fila, 2).Value = If(String.IsNullOrEmpty(d.SourceType), "—", d.SourceType)
                ws.Cell(fila, 3).Value = If(String.IsNullOrEmpty(d.NombreTipoDado), "—", d.NombreTipoDado)
                ReporteHelpers.EscribirValor(ws.Cell(fila, 4), d.Pu_Pilote_Max, "0")
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 5), d.CD_Compresion, ReporteHelpers.SinDato.CeroOMenor)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 6), d.CD_Traccion, ReporteHelpers.SinDato.CeroOMenor)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 7), d.CD_Compresion_Estructural, ReporteHelpers.SinDato.CeroOMenor)
                ReporteHelpers.EscribirFactor(ws.Cell(fila, 8), d.CD_Traccion_Estructural, ReporteHelpers.SinDato.CeroOMenor)
                ReporteHelpers.EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1, columnasIzquierda:=3)
                fila += 1
            Next
            ReporteHelpers.AgregarBordesTabla(ws, filaTop + 1, fila - 1, enc.Length)
        End If

        ReporteHelpers.AjustarColumnas(ws, 8, anchoMinimo:=14)
    End Sub

    Private Sub EscribirKPI(ws As IXLWorksheet, fila As Integer, label As String, valor As Integer)
        ws.Cell(fila, 1).Value = label
        ws.Cell(fila, 2).Value = valor
        ws.Cell(fila, 2).Style.Font.Bold = True
        ws.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        ws.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    ' Hoja 2 — Apoyos (un renglón por Dado)
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub ExportarApoyos(wb As XLWorkbook, c As cPilotes)
        Dim ws = wb.Worksheets.Add("Apoyos")
        Dim enc = {"Joint/Pier", "Origen", "X global [m]", "Y global [m]",
                   "Z base [m]", "Ángulo planta [°]", "Tipo dado",
                   "Pu max comp [kN]", "Pu min trac [kN]",
                   "C/D geot. comp.", "C/D geot. trac.",
                   "C/D estr. comp.", "C/D estr. trac.",
                   "Combo gob. comp.", "Combo gob. trac.",
                   "Veredicto"}
        ReporteHelpers.EscribirEncabezados(ws, 1, enc)

        If c.Dados.Count = 0 Then
            ws.Cell(2, 1).Value = "Sin apoyos. Importa desde ETABS en la vista 'Apoyos'."
            ws.Range(2, 1, 2, enc.Length).Merge()
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ws.Cell(2, 1).Style.Font.Italic = True
            ReporteHelpers.AjustarColumnas(ws, enc.Length)
            Return
        End If

        Dim umbral = PiloteService.UMBRAL_CD
        Dim fila = 2
        For Each d In c.Dados.OrderBy(Function(x) x.JointLabel)
            ws.Cell(fila, 1).Value = d.JointLabel
            ws.Cell(fila, 2).Value = If(String.IsNullOrEmpty(d.SourceType), "—", d.SourceType)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 3), d.X_Global, "0.000")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 4), d.Y_Global, "0.000")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 5), d.Z_Base, "0.000")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 6), d.AnguloPlanta, "0.0")
            ws.Cell(fila, 7).Value = If(String.IsNullOrEmpty(d.NombreTipoDado), "—", d.NombreTipoDado)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 8), d.Pu_Pilote_Max, "0")
            ' Pu min negativo = tracción. resaltarNegativo=True pinta en ámbar
            ' para que el ingeniero lo vea de un vistazo.
            ReporteHelpers.EscribirValor(ws.Cell(fila, 9), d.Pu_Pilote_Min, "0", resaltarNegativo:=True)
            ReporteHelpers.EscribirFactor(ws.Cell(fila, 10), d.CD_Compresion, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirFactor(ws.Cell(fila, 11), d.CD_Traccion, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirFactor(ws.Cell(fila, 12), d.CD_Compresion_Estructural, ReporteHelpers.SinDato.CeroOMenor)
            ReporteHelpers.EscribirFactor(ws.Cell(fila, 13), d.CD_Traccion_Estructural, ReporteHelpers.SinDato.CeroOMenor)
            ws.Cell(fila, 14).Value = If(String.IsNullOrEmpty(d.Combo_Gob_Compresion), "—", d.Combo_Gob_Compresion)
            ws.Cell(fila, 15).Value = If(String.IsNullOrEmpty(d.Combo_Gob_Traccion), "—", d.Combo_Gob_Traccion)

            ' Veredicto consolidado: OK si todos los C/D activos ≥ umbral; Revisar en caso contrario.
            Dim cumple = EsCumplimientoOk(d, umbral) AndAlso Not String.IsNullOrEmpty(d.NombreTipoDado)
            ReporteHelpers.EscribirEstado(ws.Cell(fila, 16), cumple)

            ReporteHelpers.EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1, columnasIzquierda:=2)
            fila += 1
        Next

        ReporteHelpers.AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
        ReporteHelpers.AjustarColumnas(ws, enc.Length, anchoMinimo:=12)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    ' Hoja 3 — Catálogo de pilotes (un renglón por TipoPilote)
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub ExportarCatalogoPilotes(wb As XLWorkbook, c As cPilotes)
        Dim ws = wb.Worksheets.Add("Catálogo Pilotes")
        Dim enc = {"Nombre", "Tipo", "Diámetro [m]", "Longitud [m]",
                   "Qc adm [kN]", "Qt adm [kN]",
                   "fc [MPa]", "fy [MPa]",
                   "Nº barras", "Ø barra", "Recubrim. [m]",
                   "Ag [mm²]", "Ast [mm²]", "Cuantía [%]",
                   "φPn [kN]", "φTn [kN]", "Advertencias NSR-10"}
        ReporteHelpers.EscribirEncabezados(ws, 1, enc)

        If c.TiposPilote.Count = 0 Then
            ws.Cell(2, 1).Value = "Sin tipos de pilote en el catálogo."
            ws.Range(2, 1, 2, enc.Length).Merge()
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ws.Cell(2, 1).Style.Font.Italic = True
            ReporteHelpers.AjustarColumnas(ws, enc.Length)
            Return
        End If

        Dim fila = 2
        For Each tp In c.TiposPilote
            ws.Cell(fila, 1).Value = tp.Nombre
            ws.Cell(fila, 2).Value = tp.Tipo.ToString()
            ReporteHelpers.EscribirValor(ws.Cell(fila, 3), tp.Diametro, "0.000")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 4), tp.Longitud, "0.00")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 5), tp.Qc_Admisible, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 6), tp.Qt_Admisible, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 7), tp.Fc, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 8), tp.Fy, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 9), tp.NumBarras_Long, "0")
            ws.Cell(fila, 10).Value = If(String.IsNullOrEmpty(tp.Diametro_Barra_Long), "—", tp.Diametro_Barra_Long)
            ws.Cell(fila, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ReporteHelpers.EscribirValor(ws.Cell(fila, 11), tp.Recubrimiento, "0.000")

            ' Recalcular en vivo el chequeo estructural para capturar las
            ' advertencias y mostrarlas en la columna final. No dependemos de
            ' estado precalculado en el Dado — este es el reporte del catálogo.
            Dim rEstr = PiloteService.EvaluarEstructuralPilote(tp, 1.0, 0.0)
            ReporteHelpers.EscribirValor(ws.Cell(fila, 12), rEstr.Ag_mm2, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 13), rEstr.Ast_mm2, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 14), rEstr.CuantiaPct, "0.00")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 15), rEstr.PhiPn_kN, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 16), rEstr.PhiTn_kN, "0")

            Dim adv = If(rEstr.Advertencias.Count = 0, "OK", String.Join(" · ", rEstr.Advertencias))
            ws.Cell(fila, 17).Value = adv
            ws.Cell(fila, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            If rEstr.Advertencias.Count > 0 Then
                ws.Cell(fila, 17).Style.Fill.BackgroundColor = ReporteHelpers.XlAlertaFondo
                ws.Cell(fila, 17).Style.Font.FontColor = ReporteHelpers.XlAlertaTexto
                ws.Cell(fila, 17).Style.Font.Bold = True
            End If

            ReporteHelpers.EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1, columnasIzquierda:=2)
            fila += 1
        Next

        ReporteHelpers.AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
        ReporteHelpers.AjustarColumnas(ws, enc.Length, anchoMinimo:=10)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    ' Hoja 4 — Catálogo de dados
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub ExportarCatalogoDados(wb As XLWorkbook, c As cPilotes)
        Dim ws = wb.Worksheets.Add("Catálogo Dados")
        Dim enc = {"Nombre", "Descripción", "Nº pilotes",
                   "Espesor [m]", "Recubr. [m]", "Área polígono [m²]",
                   "Ancho [m]", "Largo [m]",
                   "Apoyos que lo usan"}
        ReporteHelpers.EscribirEncabezados(ws, 1, enc)

        If c.TiposDado.Count = 0 Then
            ws.Cell(2, 1).Value = "Sin tipos de dado en el catálogo."
            ws.Range(2, 1, 2, enc.Length).Merge()
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ws.Cell(2, 1).Style.Font.Italic = True
            ReporteHelpers.AjustarColumnas(ws, enc.Length)
            Return
        End If

        Dim fila = 2
        For Each td In c.TiposDado
            Dim bbox = BoundingBoxPoligono(td.Poligono)
            Dim usos = c.Dados.Where(Function(d) String.Equals(d.NombreTipoDado,
                                                               td.Nombre,
                                                               StringComparison.OrdinalIgnoreCase)).Count()

            ws.Cell(fila, 1).Value = td.Nombre
            ws.Cell(fila, 2).Value = If(String.IsNullOrEmpty(td.Descripcion), "—", td.Descripcion)
            ws.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            ReporteHelpers.EscribirValor(ws.Cell(fila, 3), td.Pilotes.Count, "0")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 4), td.Espesor, "0.00")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 5), td.Recubrimiento, "0.000")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 6), DadoGeometria.AreaPoligono(td.Poligono), "0.00")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 7), bbox.Item1, "0.00")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 8), bbox.Item2, "0.00")
            ReporteHelpers.EscribirValor(ws.Cell(fila, 9), usos, "0")

            ReporteHelpers.EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1, columnasIzquierda:=2)
            fila += 1
        Next

        ReporteHelpers.AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
        ReporteHelpers.AjustarColumnas(ws, enc.Length, anchoMinimo:=10)
    End Sub

    ''' <summary>Ancho y largo del rectángulo que envuelve el polígono.</summary>
    Private Function BoundingBoxPoligono(poligono As List(Of Punto2D)) As Tuple(Of Double, Double)
        If poligono Is Nothing OrElse poligono.Count = 0 Then
            Return Tuple.Create(0.0, 0.0)
        End If
        Dim minX = poligono.Min(Function(v) v.X)
        Dim maxX = poligono.Max(Function(v) v.X)
        Dim minY = poligono.Min(Function(v) v.Y)
        Dim maxY = poligono.Max(Function(v) v.Y)
        Return Tuple.Create(maxX - minX, maxY - minY)
    End Function

    ' ══════════════════════════════════════════════════════════════════════
    ' Hoja 5 — Combinaciones
    ' ══════════════════════════════════════════════════════════════════════
    Private Sub ExportarCombinaciones(wb As XLWorkbook, c As cPilotes)
        Dim ws = wb.Worksheets.Add("Combinaciones")
        Dim enc = {"Nombre combinación", "¿En diseño?"}
        ReporteHelpers.EscribirEncabezados(ws, 1, enc)

        Dim seleccion As HashSet(Of String) = Nothing
        If c.ListA_Combinaciones_Design IsNot Nothing Then
            seleccion = New HashSet(Of String)(c.ListA_Combinaciones_Design,
                                               StringComparer.OrdinalIgnoreCase)
        End If

        If c.Lista_Combinaciones Is Nothing OrElse c.Lista_Combinaciones.Count = 0 Then
            ws.Cell(2, 1).Value = "Sin combinaciones. Importa desde ETABS para poblar la lista."
            ws.Range(2, 1, 2, enc.Length).Merge()
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            ws.Cell(2, 1).Style.Font.Italic = True
            ReporteHelpers.AjustarColumnas(ws, enc.Length)
            Return
        End If

        Dim fila = 2
        For Each nom In c.Lista_Combinaciones
            ws.Cell(fila, 1).Value = nom
            Dim enDiseno = seleccion IsNot Nothing AndAlso seleccion.Contains(nom)
            ReporteHelpers.EscribirEstado(ws.Cell(fila, 2),
                                          If(enDiseno, "Sí", "No"),
                                          If(enDiseno, ReporteHelpers.XlOKFondo, ReporteHelpers.XlFilaPar),
                                          If(enDiseno, ReporteHelpers.XlOKTexto, XLColor.Black))
            ReporteHelpers.EstilarFilaDatos(ws, fila, enc.Length, fila Mod 2 = 1, columnasIzquierda:=1)
            fila += 1
        Next

        ReporteHelpers.AgregarBordesTabla(ws, 1, fila - 1, enc.Length)
        ReporteHelpers.AjustarColumnas(ws, enc.Length, anchoMinimo:=12)
    End Sub

    ' ══════════════════════════════════════════════════════════════════════
    ' Helpers locales
    ' ══════════════════════════════════════════════════════════════════════

    ''' <summary>Peor C/D activo entre los 4 chequeos. 0 si ninguno aplica.</summary>
    Private Function PeorCD(d As Dado) As Double
        Dim vals As New List(Of Double)
        If d.CD_Compresion > 0 Then vals.Add(d.CD_Compresion)
        If d.CD_Traccion > 0 Then vals.Add(d.CD_Traccion)
        If d.CD_Compresion_Estructural > 0 Then vals.Add(d.CD_Compresion_Estructural)
        If d.CD_Traccion_Estructural > 0 Then vals.Add(d.CD_Traccion_Estructural)
        If vals.Count = 0 Then Return 0
        Return vals.Min()
    End Function

    Private Function EsCumplimientoOk(d As Dado, umbral As Double) As Boolean
        If d.CD_Compresion > 0 AndAlso d.CD_Compresion < umbral Then Return False
        If d.CD_Traccion > 0 AndAlso d.CD_Traccion < umbral Then Return False
        If d.CD_Compresion_Estructural > 0 AndAlso d.CD_Compresion_Estructural < umbral Then Return False
        If d.CD_Traccion_Estructural > 0 AndAlso d.CD_Traccion_Estructural < umbral Then Return False
        Return True
    End Function

    Private Function NoCumpleAlguno(d As Dado, umbral As Double) As Boolean
        If d.CD_Compresion > 0 AndAlso d.CD_Compresion < umbral Then Return True
        If d.CD_Traccion > 0 AndAlso d.CD_Traccion < umbral Then Return True
        If d.CD_Compresion_Estructural > 0 AndAlso d.CD_Compresion_Estructural < umbral Then Return True
        If d.CD_Traccion_Estructural > 0 AndAlso d.CD_Traccion_Estructural < umbral Then Return True
        Return False
    End Function

End Module
