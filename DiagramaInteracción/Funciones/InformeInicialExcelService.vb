Imports System.IO
Imports ClosedXML.Excel

''' <summary>
''' Opciones del "Informe Inicial" — selección de módulos a exportar y datos de portada.
''' Produce un .xlsx con una hoja por tabla de la plantilla P58501_I001 (Tablas 11-28),
''' lista para que el usuario copie/pegue al .docx del informe final.
''' </summary>
Public Class OpcionesInformeInicial
    Public Property Fecha As Date = Date.Today
    Public Property CodigoProyecto As String = ""
    Public Property NombreProyecto As String = ""

    Public Property IncluirColumnas As Boolean = True
    Public Property IncluirVigas As Boolean = True
    Public Property IncluirNervios As Boolean = True
    Public Property IncluirPilas As Boolean = True
    Public Property IncluirVigasFundacion As Boolean = True
    ''' <summary>Relación viga-columna NSR-10 C.21.6.2 — requiere vigas y columnas
    ''' con refuerzo y DI calculados. Default False porque es un chequeo cruzado
    ''' que puede fallar si faltan datos; el usuario lo activa cuando corresponde.</summary>
    Public Property IncluirRelacionVigaColumna As Boolean = False

    ''' <summary>
    ''' Si es True, se agregan dos hojas extra (T11_Detalle y T12_Detalle) con
    ''' una fila por cada tramo de cada columna — útil para auditorías internas.
    ''' Las hojas "oficiales" del informe (T11, T12) siempre muestran solo una
    ''' fila por columna con el tramo más crítico, que es el formato pedido por
    ''' Curaduría.
    ''' </summary>
    Public Property IncluirDetalleColumnas As Boolean = False
End Class

''' <summary>
''' Genera el "Informe Inicial" (.xlsx) con las tablas de verificación ya pobladas a partir
''' del <see cref="Proyecto"/> actual. Lista TODOS los elementos (no sólo los que incumplen)
''' en el orden piso inferior → piso superior cuando la tabla incluye piso como dimensión.
'''
''' Tablas cubiertas (numeración de la plantilla P58501_I001_INK.docx):
'''   T11 flexo-compresión columnas, T12 cortante columnas, T13 Ash confinamiento,
'''   T14 S0/L0 confinamiento, T17 flexión vigas, T19 flexión nervios, T20 cortante nervios,
'''   T22 σ suelo estático, T23 σ suelo dinámico, T24 σ concreto pilas, T28 vigas de cimentación.
'''
''' Excluidas intencionalmente (por fuera del alcance de este primer release):
'''   T15 relación viga-columna (requiere cruzar nodos con vigas+columnas por piso/dirección).
'''   T29 muro de contención (módulo 12 es independiente del Proyecto; se exporta aparte).
''' </summary>
Public Class InformeInicialExcelService

    Private Const UMBRAL As Double = Funciones_00_Varias.UMBRAL_CD

    ' ── Paleta (coincide con Form_Reporte_Resumen_Muros) ──────────────────────
    Private Shared ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Private Shared ReadOnly XlEncabezadoAlt As XLColor = XLColor.FromHtml("#8C8C8C")
    Private Shared ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Private Shared ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Private Shared ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Private Shared ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Private Shared ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F8F8F8")

    ' Índice piso → orden (más bajo = 0). Se puebla al inicio de Generar.
    Private Shared _ordenPisos As Dictionary(Of String, Integer)

    ' =========================================================================
    '  Punto de entrada
    ' =========================================================================
    Public Shared Sub Generar(proyecto As Proyecto, opciones As OpcionesInformeInicial, rutaSalida As String)
        InicializarOrdenPisos(proyecto)

        Using wb As New XLWorkbook()
            EscribirPortada(wb, proyecto, opciones)

            If opciones.IncluirColumnas Then
                EscribirT11_FlexoCompColumnas(wb, proyecto)
                EscribirT12_CortanteColumnas(wb, proyecto)
                EscribirT13_AshConfinamiento(wb, proyecto)
                EscribirT14_S0L0_Confinamiento(wb, proyecto)
                If opciones.IncluirDetalleColumnas Then
                    EscribirT11_Detalle(wb, proyecto)
                    EscribirT12_Detalle(wb, proyecto)
                End If
            End If

            If opciones.IncluirVigas Then
                EscribirT17_FlexionVigas(wb, proyecto)
            End If

            If opciones.IncluirNervios Then
                EscribirT19_FlexionNervios(wb, proyecto)
                EscribirT20_CortanteNervios(wb, proyecto)
            End If

            If opciones.IncluirPilas Then
                EscribirT22_SueloEstatico(wb, proyecto)
                EscribirT23_SueloDinamico(wb, proyecto)
                EscribirT24_ConcretoPilas(wb, proyecto)
            End If

            If opciones.IncluirVigasFundacion Then
                EscribirT28_VigasFundacion(wb, proyecto)
            End If

            If opciones.IncluirRelacionVigaColumna Then
                Dim validar = RelacionVigaColumnaService.PuedeEvaluar(proyecto)
                If validar.OK Then
                    Dim nodos = RelacionVigaColumnaService.Evaluar(proyecto)
                    EscribirT15_RelacionVigaColumna(wb, nodos)
                End If
            End If

            wb.SaveAs(rutaSalida)
        End Using
    End Sub

    ' =========================================================================
    '  Orden por piso (más bajo = 0). Idéntica semántica a ReporteRevisionService.
    ' =========================================================================
    Private Shared Sub InicializarOrdenPisos(proyecto As Proyecto)
        _ordenPisos = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        If proyecto?.Pisos Is Nothing Then Return
        Dim ordenados = proyecto.Pisos _
            .Where(Function(p) p IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(p.Nombre)) _
            .OrderBy(Function(p) p.CoorZ).ToList()
        For i = 0 To ordenados.Count - 1
            _ordenPisos(ordenados(i).Nombre.Trim()) = i
        Next
    End Sub

    Private Shared Function OrdenPiso(nombre As String) As Integer
        If String.IsNullOrWhiteSpace(nombre) Then Return Integer.MaxValue
        Dim clave = nombre.Trim()

        Dim idx As Integer
        If _ordenPisos IsNot Nothing AndAlso _ordenPisos.TryGetValue(clave, idx) Then Return idx

        Dim n = clave.ToUpperInvariant()
        If n.Contains("CIMENT") OrElse n.Contains("FUND") OrElse n = "BASE" Then Return -1000
        If n.Contains("CUBIERTA") OrElse n.Contains("TECHO") OrElse n.Contains("TERRAZA") OrElse n.Contains("AZOTEA") Then Return 900

        Dim mSot = System.Text.RegularExpressions.Regex.Match(n, "S[OÓ]?TANO?\s*(\d+)")
        If mSot.Success Then Return -CInt(mSot.Groups(1).Value)

        Dim mPiso = System.Text.RegularExpressions.Regex.Match(n, "^\s*(?:PISO|PLANTA|STORY|NIVEL|P)\s*(\d+)")
        If mPiso.Success Then Return CInt(mPiso.Groups(1).Value)

        Dim mNum = System.Text.RegularExpressions.Regex.Match(n, "^\s*(\d+)\s*$")
        If mNum.Success Then Return CInt(mNum.Groups(1).Value)

        Return Integer.MaxValue
    End Function

    ' =========================================================================
    '  Helpers de formato
    ' =========================================================================

    ' Escribe la cabecera: título de tabla (fila 1, combinado) + fila(s) de encabezado
    ' gris con texto blanco. Devuelve la primera fila de datos (1-based).
    Private Shared Function EscribirCabecera(ws As IXLWorksheet,
                                              numeroTabla As Integer,
                                              titulo As String,
                                              encabezados As String()) As Integer
        Dim nCol = encabezados.Length

        ' Fila 1: título de la tabla (texto corrido estilo "Tabla 11. ...")
        ws.Cell(1, 1).Value = $"Tabla {numeroTabla}. {titulo}"
        Dim rngTit = ws.Range(1, 1, 1, nCol)
        rngTit.Merge()
        rngTit.Style.Font.Bold = True
        rngTit.Style.Font.FontSize = 11
        rngTit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        rngTit.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
        ws.Row(1).Height = 22

        ' Fila 2: encabezados
        For i = 0 To nCol - 1
            ws.Cell(2, i + 1).Value = encabezados(i)
        Next
        Dim rngEnc = ws.Range(2, 1, 2, nCol)
        rngEnc.Style.Fill.BackgroundColor = XlEncabezado
        rngEnc.Style.Font.FontColor = XLColor.White
        rngEnc.Style.Font.Bold = True
        rngEnc.Style.Font.FontSize = 10
        rngEnc.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        rngEnc.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
        rngEnc.Style.Alignment.WrapText = True
        rngEnc.Style.Border.OutsideBorder = XLBorderStyleValues.Thin
        rngEnc.Style.Border.InsideBorder = XLBorderStyleValues.Thin
        ws.Row(2).Height = 32

        Return 3
    End Function

    ' Bordes, zebra y auto-fit al terminar de escribir una tabla.
    Private Shared Sub TerminarTabla(ws As IXLWorksheet, filaInicio As Integer, filaFin As Integer, nColumnas As Integer)
        If filaFin < filaInicio Then Return

        Dim rngDatos = ws.Range(filaInicio, 1, filaFin, nColumnas)
        rngDatos.Style.Border.OutsideBorder = XLBorderStyleValues.Thin
        rngDatos.Style.Border.InsideBorder = XLBorderStyleValues.Thin
        rngDatos.Style.Font.FontSize = 10
        rngDatos.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center

        For f = filaInicio To filaFin
            If (f - filaInicio) Mod 2 = 1 Then
                ws.Range(f, 1, f, nColumnas).Style.Fill.BackgroundColor = XlFilaPar
            End If
        Next

        ws.Columns(1, nColumnas).AdjustToContents()
        ws.SheetView.FreezeRows(2)
    End Sub

    ' Escribe "Cumple" / "No cumple" en la celda con semáforo, basado en el ratio.
    ' ratio = 0 significa "no aplica" → deja celda vacía (misma convención que Pilas).
    Private Shared Sub AsignarVerificacion(cell As IXLCell, ratio As Double, Optional sinDato As Boolean = False)
        If sinDato Then
            cell.Value = "—"
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            Return
        End If
        Dim cumple As Boolean = ratio >= UMBRAL
        cell.Value = If(cumple, "Cumple", "No cumple")
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

    Private Shared Function FmtSeccion(b As Single, h As Single) As String
        ' Formato "0,50x1,50" (coma decimal, estilo colombiano — coincide con la plantilla).
        Return b.ToString("0.00", Globalization.CultureInfo.GetCultureInfo("es-CO")) & "x" &
               h.ToString("0.00", Globalization.CultureInfo.GetCultureInfo("es-CO"))
    End Function

    ' =========================================================================
    '  PORTADA (hoja 1): identifica el informe y lista qué tablas contiene.
    ' =========================================================================
    Private Shared Sub EscribirPortada(wb As XLWorkbook, proyecto As Proyecto, opciones As OpcionesInformeInicial)
        Dim ws = wb.Worksheets.Add("Portada")

        ws.Cell(1, 1).Value = "INFORME INICIAL — TABLAS DE VERIFICACIÓN"
        Dim tit = ws.Range(1, 1, 1, 4)
        tit.Merge()
        tit.Style.Font.Bold = True
        tit.Style.Font.FontSize = 14
        tit.Style.Fill.BackgroundColor = XlEncabezado
        tit.Style.Font.FontColor = XLColor.White
        tit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
        tit.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
        ws.Row(1).Height = 28

        Dim fila As Integer = 3
        Dim nombreProy As String = If(Not String.IsNullOrWhiteSpace(opciones.NombreProyecto),
                                        opciones.NombreProyecto,
                                        If(proyecto?.Info?.Nombre, ""))
        Dim entradas = {
            ("Proyecto:", nombreProy),
            ("Código:", opciones.CodigoProyecto),
            ("Fecha:", opciones.Fecha.ToString("dd/MM/yyyy")),
            ("Generado por:", "ARCO " & Date.Now.ToString("yyyy-MM-dd HH:mm"))
        }
        For Each e In entradas
            ws.Cell(fila, 1).Value = e.Item1
            ws.Cell(fila, 1).Style.Font.Bold = True
            ws.Cell(fila, 2).Value = e.Item2
            ws.Range(fila, 2, fila, 4).Merge()
            fila += 1
        Next

        fila += 1
        ws.Cell(fila, 1).Value = "Tablas incluidas"
        ws.Cell(fila, 1).Style.Font.Bold = True
        ws.Cell(fila, 1).Style.Font.FontSize = 11
        fila += 1

        Dim items As New List(Of (String, String))
        If opciones.IncluirColumnas Then
            items.Add(("T11", "Verificación a flexo-compresión de las columnas"))
            items.Add(("T12", "Verificación a cortante de las columnas"))
            items.Add(("T13", "Verificación del área de confinamiento en las columnas"))
            items.Add(("T14", "Verificación de la longitud L₀ y separación máxima por confinamiento"))
        End If
        If opciones.IncluirVigas Then
            items.Add(("T17", "Verificación a flexión de las vigas estructurales"))
        End If
        If opciones.IncluirNervios Then
            items.Add(("T19", "Verificación de la capacidad a flexión de los nervios"))
            items.Add(("T20", "Verificación de la capacidad a cortante de los nervios"))
        End If
        If opciones.IncluirPilas Then
            items.Add(("T22", "Verificación de esfuerzos admisibles en el suelo — condiciones estáticas"))
            items.Add(("T23", "Verificación de esfuerzos admisibles en el suelo — condiciones dinámicas"))
            items.Add(("T24", "Verificación de esfuerzos del concreto en pilas"))
        End If
        If opciones.IncluirVigasFundacion Then
            items.Add(("T28", "Verificación de fuerza de compresión y tracción en las vigas de cimentación"))
        End If
        If opciones.IncluirRelacionVigaColumna Then
            items.Add(("T15", "Verificación de la relación viga-columna (NSR-10 C.21.6.2)"))
        End If
        If opciones.IncluirColumnas AndAlso opciones.IncluirDetalleColumnas Then
            items.Add(("T11_Detalle", "Flexo-compresión columnas — una fila por tramo (auditoría)"))
            items.Add(("T12_Detalle", "Cortante columnas — una fila por tramo (auditoría)"))
        End If

        For Each it In items
            ws.Cell(fila, 1).Value = it.Item1
            ws.Cell(fila, 1).Style.Font.Bold = True
            ws.Cell(fila, 2).Value = it.Item2
            ws.Range(fila, 2, fila, 4).Merge()
            fila += 1
        Next

        fila += 1
        ws.Cell(fila, 1).Value = "Umbral de cumplimiento: C/D ≥ 0,90 (Funciones_00_Varias.UMBRAL_CD)"
        ws.Cell(fila, 1).Style.Font.Italic = True
        ws.Range(fila, 1, fila, 4).Merge()
        fila += 1
        ws.Cell(fila, 1).Value = "T11/T12 — una fila por columna (tramo crítico). T13/T14 — una fila por sección única."
        ws.Cell(fila, 1).Style.Font.Italic = True
        ws.Range(fila, 1, fila, 4).Merge()
        fila += 1
        ws.Cell(fila, 1).Value = "T17 — solo vigas con refuerzo real. T19/T20 — solo nervios revisados por el usuario."
        ws.Cell(fila, 1).Style.Font.Italic = True
        ws.Range(fila, 1, fila, 4).Merge()
        fila += 1
        ws.Cell(fila, 1).Value = "Orden de piso: inferior → superior cuando la tabla usa piso como dimensión."
        ws.Cell(fila, 1).Style.Font.Italic = True
        ws.Range(fila, 1, fila, 4).Merge()

        ws.Columns(1, 4).AdjustToContents()
    End Sub

    ' =========================================================================
    '  COLUMNAS
    '
    '  T11 y T12 (hojas oficiales del informe): una fila por COLUMNA. Para cada
    '  columna se reporta el "tramo crítico" según el criterio de la tabla
    '  (CDFlexoEnvolvente en T11, min(F_Cortante_2, F_Cortante_3) en T12). Es
    '  el formato que Curaduría recibe — no tiene sentido abrumarlos con el
    '  mismo elemento repetido piso a piso cuando la mayoría pasa.
    '
    '  T13 y T14 (confinamiento): una fila por SECCIÓN ÚNICA (B×H × f'c). Los
    '  requisitos Ash, S₀ y L₀ dependen solo de la geometría y el material, así
    '  que repetirlos para cada columna que comparte sección es ruido. Entre
    '  tramos del mismo grupo se elige el representante con peor ratio para que
    '  el "colocado" refleje el peor refuerzo transversal que tocó esa sección.
    '
    '  El modo "detallado por tramo" (opciones.IncluirDetalleColumnas) escribe
    '  hojas extra T11_Detalle y T12_Detalle para quien quiera auditar piso a
    '  piso.
    ' =========================================================================

    ' Agrupador base: por cada Columna recorre sus tramos y escoge el tramo con
    ' menor valor retornado por 'selector' (ignorando 0 = sin dato). Devuelve
    ' las columnas ordenadas por el piso del tramo crítico, bottom→top. Las
    ' columnas sin ningún tramo evaluable quedan al final con el tramo [0].
    Private Shared Function PeorTramoPorColumna(proyecto As Proyecto,
                                                 selector As Func(Of Tramo_Columna, Single)) _
                                                 As List(Of (Col As Columna, Tr As Tramo_Columna, Ratio As Double))
        Dim result As New List(Of (Columna, Tramo_Columna, Double))
        If proyecto?.Elementos?.Columnas?.Lista_Columnas Is Nothing Then Return result

        For Each col In proyecto.Elementos.Columnas.Lista_Columnas
            If col?.Lista_Tramos_Columnas Is Nothing OrElse col.Lista_Tramos_Columnas.Count = 0 Then Continue For
            Dim peorTramo As Tramo_Columna = Nothing
            Dim peorRatio As Double = Double.MaxValue
            For Each tr In col.Lista_Tramos_Columnas
                If tr Is Nothing Then Continue For
                Dim r As Double = selector(tr)
                If r > 0 AndAlso r < peorRatio Then
                    peorRatio = r
                    peorTramo = tr
                End If
            Next
            If peorTramo Is Nothing Then
                ' Sin cálculo en ningún tramo: tomar el primero como "placeholder"
                ' para que la columna aparezca en el informe con "—".
                peorTramo = col.Lista_Tramos_Columnas.First(Function(t) t IsNot Nothing)
                peorRatio = 0
            End If
            result.Add((col, peorTramo, peorRatio))
        Next

        Return result _
            .Select(Function(x, i) (Item:=x, Ord:=OrdenPiso(x.Item2.Piso), Idx:=i)) _
            .OrderBy(Function(x) x.Ord).ThenBy(Function(x) x.Idx) _
            .Select(Function(x) x.Item).ToList()
    End Function

    ' Nombre "del usuario" de una columna: ¡en Columna la nomenclatura es al
    ' revés que en Elemento_Pila! Name_Elemento guarda el label ETABS original
    ' (inmutable desde la importación) y Name_Label es lo que el usuario edita
    ' en Form_02_00_PagInfoColumnas (línea 105 asigna T_Seccion.Text → Name_Label)
    ' y es lo que puebla los combos de la UI. Para el informe final siempre se
    ' usa Name_Label; Name_Elemento queda de fallback para proyectos sin
    ' renombrar.
    Private Shared Function NombreColumna(col As Columna) As String
        If col Is Nothing Then Return ""
        If Not String.IsNullOrWhiteSpace(col.Name_Label) Then Return col.Name_Label
        Return If(col.Name_Elemento, "")
    End Function

    ' T11 — Flexo-compresión (una fila por columna, tramo crítico)
    Private Shared Sub EscribirT11_FlexoCompColumnas(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T11_FlexoComp_Col")
        Dim headers = {"COLUMNA", "PISO CRÍTICO", "SECCIÓN (m)", "CUANTÍA", "C/D", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 11, "Verificación a flexo-compresión de las columnas.", headers)
        Dim f0 = fila

        Dim datos = PeorTramoPorColumna(proyecto, Function(t) ColumnaService.CDFlexoEnvolvente(t))
        For Each d In datos
            Dim tr = d.Tr
            Dim cuantia As Single = (tr.Cuantia_Col_Top + tr.Cuantia_Col_Bottom) / 2.0F
            Dim cd As Double = d.Ratio

            ws.Cell(fila, 1).Value = NombreColumna(d.Col)
            ws.Cell(fila, 2).Value = tr.Piso
            ws.Cell(fila, 3).Value = FmtSeccion(tr.B_Plano, tr.H_Plano)
            ws.Cell(fila, 4).Value = cuantia : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00%"
            ws.Cell(fila, 5).Value = cd : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 6), cd, sinDato:=(cd = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' T12 — Cortante (una fila por columna, tramo con peor ratio entre LL y LC)
    Private Shared Sub EscribirT12_CortanteColumnas(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T12_Cortante_Col")
        Dim headers = {"COLUMNA", "PISO CRÍTICO", "SECCIÓN (m)", "φVn/Vu (LL)", "φVn/Vu (LC)", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 12, "Verificación a cortante de las columnas.", headers)
        Dim f0 = fila

        ' Selector: el ratio crítico del tramo = min(F_Cortante_2 nonzero, F_Cortante_3 nonzero).
        Dim selector As Func(Of Tramo_Columna, Single) =
            Function(t) As Single
                Dim a = If(t.F_Cortante_2 > 0, t.F_Cortante_2, Single.MaxValue)
                Dim b = If(t.F_Cortante_3 > 0, t.F_Cortante_3, Single.MaxValue)
                Dim m = Math.Min(a, b)
                Return If(m = Single.MaxValue, 0, CSng(m))
            End Function

        Dim datos = PeorTramoPorColumna(proyecto, selector)
        For Each d In datos
            Dim tr = d.Tr
            Dim ratioLL As Single = tr.F_Cortante_3   ' V3 → lado largo
            Dim ratioLC As Single = tr.F_Cortante_2   ' V2 → lado corto
            Dim peor = d.Ratio

            ws.Cell(fila, 1).Value = NombreColumna(d.Col)
            ws.Cell(fila, 2).Value = tr.Piso
            ws.Cell(fila, 3).Value = FmtSeccion(tr.B_Plano, tr.H_Plano)
            ws.Cell(fila, 4).Value = ratioLL : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 5).Value = ratioLC : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 6), peor, sinDato:=(peor = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' Clave de agrupación por sección única — redondeo a 1 mm para absorber el
    ' ruido numérico de Single.
    Private Shared Function ClaveSeccion(tr As Tramo_Columna) As String
        Dim b = Math.Round(tr.B_Plano, 3).ToString(Globalization.CultureInfo.InvariantCulture)
        Dim h = Math.Round(tr.H_Plano, 3).ToString(Globalization.CultureInfo.InvariantCulture)
        Return $"{b}|{h}|{CInt(tr.fc)}"
    End Function

    ' Agrupa los tramos de todas las columnas por sección única (B×H × fc) y
    ' devuelve, por cada grupo, el tramo con peor 'selector' (ignorando 0). Si
    ' todos son 0, devuelve el primero. Ordena por B, luego H, luego fc.
    Private Shared Function RepresentantesPorSeccion(proyecto As Proyecto,
                                                      selector As Func(Of Tramo_Columna, Single)) _
                                                      As List(Of Tramo_Columna)
        Dim all As New List(Of Tramo_Columna)
        If proyecto?.Elementos?.Columnas?.Lista_Columnas Is Nothing Then Return New List(Of Tramo_Columna)
        For Each col In proyecto.Elementos.Columnas.Lista_Columnas
            If col?.Lista_Tramos_Columnas Is Nothing Then Continue For
            For Each tr In col.Lista_Tramos_Columnas
                If tr IsNot Nothing AndAlso tr.B_Plano > 0 AndAlso tr.H_Plano > 0 Then all.Add(tr)
            Next
        Next

        Return all _
            .GroupBy(AddressOf ClaveSeccion) _
            .Select(Function(g)
                        Dim peor As Tramo_Columna = Nothing
                        Dim rMin = Single.MaxValue
                        For Each t In g
                            Dim r = selector(t)
                            If r > 0 AndAlso r < rMin Then
                                rMin = r
                                peor = t
                            End If
                        Next
                        If peor Is Nothing Then peor = g.First()
                        Return peor
                    End Function) _
            .OrderBy(Function(t) t.B_Plano) _
            .ThenBy(Function(t) t.H_Plano) _
            .ThenBy(Function(t) t.fc) _
            .ToList()
    End Function

    ' T13 — Área de confinamiento Ash (dos filas por sección única: LL y LC)
    Private Shared Sub EscribirT13_AshConfinamiento(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T13_Ash_Col")
        Dim headers = {"SECCIÓN (m)", "f'c (MPa)", "DIRECCIÓN",
                       "Ash colocado (cm²)", "Ash requerido (cm²)", "Ash_col / Ash_req", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 13, "Verificación del área de confinamiento en las columnas.", headers)
        Dim f0 = fila

        ' Representante por sección = tramo con peor ratio Ash (en cualquier dirección).
        Dim selector As Func(Of Tramo_Columna, Single) =
            Function(t)
                Dim a = If(t.F_Ash_Largo > 0, t.F_Ash_Largo, Single.MaxValue)
                Dim b = If(t.F_Ash_Corto > 0, t.F_Ash_Corto, Single.MaxValue)
                Dim m = Math.Min(a, b)
                Return If(m = Single.MaxValue, 0, CSng(m))
            End Function

        Dim representantes = RepresentantesPorSeccion(proyecto, selector)
        For Each tr In representantes
            Dim seccion = FmtSeccion(tr.B_Plano, tr.H_Plano)

            ' LL (lado largo)
            ws.Cell(fila, 1).Value = seccion
            ws.Cell(fila, 2).Value = tr.fc
            ws.Cell(fila, 3).Value = "LL"
            ws.Cell(fila, 4).Value = tr.Ash_Col_Largo : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 5).Value = tr.Ash_L : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 6).Value = tr.F_Ash_Largo : ws.Cell(fila, 6).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 7), tr.F_Ash_Largo, sinDato:=(tr.Ash_L <= 0))
            fila += 1

            ' LC (lado corto)
            ws.Cell(fila, 1).Value = seccion
            ws.Cell(fila, 2).Value = tr.fc
            ws.Cell(fila, 3).Value = "LC"
            ws.Cell(fila, 4).Value = tr.Ash_Col_Corto : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 5).Value = tr.Ash_C : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 6).Value = tr.F_Ash_Corto : ws.Cell(fila, 6).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 7), tr.F_Ash_Corto, sinDato:=(tr.Ash_C <= 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' T14 — Longitud L0 y separación máxima S0 (una fila por sección única)
    ' S0 requerido reportado = min(S0_L, S0_C): lo más restrictivo.
    ' L0 requerido reportado = max(L0_L, L0_C): la zona de confinamiento más larga.
    Private Shared Sub EscribirT14_S0L0_Confinamiento(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T14_S0L0_Col")
        Dim headers = {"SECCIÓN (m)", "f'c (MPa)",
                       "S₀ colocado (m)", "S₀ requerido (m)", "VERIF. S₀",
                       "L₀ colocado (m)", "L₀ requerido (m)", "VERIF. L₀"}
        Dim fila = EscribirCabecera(ws, 14, "Verificación de la longitud L₀ y separación máxima por confinamiento en columnas.", headers)
        Dim f0 = fila

        ' Representante por sección = tramo con peor F_L0 (ratio L0 colocado / req).
        Dim representantes = RepresentantesPorSeccion(proyecto, Function(t) t.F_L0)
        For Each tr In representantes
            Dim s0Req As Double = Math.Max(0, Math.Min(If(tr.S0_L > 0, tr.S0_L, Single.MaxValue),
                                                        If(tr.S0_C > 0, tr.S0_C, Single.MaxValue)))
            If s0Req = Single.MaxValue Then s0Req = 0
            Dim l0Req As Double = Math.Max(tr.L0_L, tr.L0_C)

            Dim ratioS0 As Double = 0
            If tr.Separacion_Estribos > 0 AndAlso s0Req > 0 Then
                ratioS0 = s0Req / tr.Separacion_Estribos
            End If

            ws.Cell(fila, 1).Value = FmtSeccion(tr.B_Plano, tr.H_Plano)
            ws.Cell(fila, 2).Value = tr.fc
            ws.Cell(fila, 3).Value = tr.Separacion_Estribos : ws.Cell(fila, 3).Style.NumberFormat.Format = "0.000"
            ws.Cell(fila, 4).Value = s0Req : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.000"
            AsignarVerificacion(ws.Cell(fila, 5), ratioS0, sinDato:=(s0Req <= 0))
            ws.Cell(fila, 6).Value = tr.L0_Prov : ws.Cell(fila, 6).Style.NumberFormat.Format = "0.000"
            ws.Cell(fila, 7).Value = l0Req : ws.Cell(fila, 7).Style.NumberFormat.Format = "0.000"
            AsignarVerificacion(ws.Cell(fila, 8), tr.F_L0, sinDato:=(l0Req <= 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' =========================================================================
    '  COLUMNAS — hojas detalle opcionales (opciones.IncluirDetalleColumnas)
    '  Una fila por tramo; replica el formato anterior para auditoría.
    ' =========================================================================

    Private Shared Iterator Function EnumerarTramosOrdenado(proyecto As Proyecto) As IEnumerable(Of (Col As Columna, Tr As Tramo_Columna))
        If proyecto?.Elementos?.Columnas?.Lista_Columnas Is Nothing Then Return
        Dim tramos = New List(Of (Columna, Tramo_Columna))
        For Each col In proyecto.Elementos.Columnas.Lista_Columnas
            If col?.Lista_Tramos_Columnas Is Nothing Then Continue For
            For Each tr In col.Lista_Tramos_Columnas
                If tr IsNot Nothing Then tramos.Add((col, tr))
            Next
        Next
        Dim ordenados = tramos _
            .Select(Function(x, i) (Item:=x, Ord:=OrdenPiso(x.Item2.Piso), Idx:=i)) _
            .OrderBy(Function(x) x.Ord).ThenBy(Function(x) x.Idx) _
            .Select(Function(x) x.Item)
        For Each p In ordenados
            Yield p
        Next
    End Function

    Private Shared Sub EscribirT11_Detalle(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T11_Detalle")
        Dim headers = {"COLUMNA", "PISO", "SECCIÓN (m)", "CUANTÍA", "C/D", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 11, "Flexo-compresión — detalle por tramo (todas las columnas).", headers)
        Dim f0 = fila

        For Each p In EnumerarTramosOrdenado(proyecto)
            Dim tr = p.Tr
            Dim cuantia As Single = (tr.Cuantia_Col_Top + tr.Cuantia_Col_Bottom) / 2.0F
            Dim cd As Single = ColumnaService.CDFlexoEnvolvente(tr)

            ws.Cell(fila, 1).Value = NombreColumna(p.Col)
            ws.Cell(fila, 2).Value = tr.Piso
            ws.Cell(fila, 3).Value = FmtSeccion(tr.B_Plano, tr.H_Plano)
            ws.Cell(fila, 4).Value = cuantia : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00%"
            ws.Cell(fila, 5).Value = cd : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 6), cd, sinDato:=(cd = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    Private Shared Sub EscribirT12_Detalle(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T12_Detalle")
        Dim headers = {"COLUMNA", "PISO", "SECCIÓN (m)", "φVn/Vu (LL)", "φVn/Vu (LC)", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 12, "Cortante — detalle por tramo (todas las columnas).", headers)
        Dim f0 = fila

        For Each p In EnumerarTramosOrdenado(proyecto)
            Dim tr = p.Tr
            Dim ratioLL As Single = tr.F_Cortante_3
            Dim ratioLC As Single = tr.F_Cortante_2
            Dim peor As Double = Math.Min(If(ratioLL > 0, ratioLL, Double.MaxValue),
                                          If(ratioLC > 0, ratioLC, Double.MaxValue))
            If peor = Double.MaxValue Then peor = 0

            ws.Cell(fila, 1).Value = NombreColumna(p.Col)
            ws.Cell(fila, 2).Value = tr.Piso
            ws.Cell(fila, 3).Value = FmtSeccion(tr.B_Plano, tr.H_Plano)
            ws.Cell(fila, 4).Value = ratioLL : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 5).Value = ratioLC : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 6), peor, sinDato:=(peor = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' =========================================================================
    '  VIGAS (T17)
    '  Solo se exportan las vigas que fueron REVISADAS (al menos un frame con
    '  refuerzo sup Y inf real — regla endurecida en VigaService.TieneRefuerzoFlexion
    '  tras el incidente de 2026-11-04). Las vigas sin refuerzo saldrían con
    '  ratios en 0 y confundirían a Curaduría; mejor omitirlas.
    '  El nombre que ve el usuario es NombrePlano; fallback a Nombre.
    ' =========================================================================
    Private Shared Sub EscribirT17_FlexionVigas(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T17_Flexion_Vig")
        Dim headers = {"PISO", "VIGA", "SECCIÓN", "φMn⁻ / Mu⁻", "φMn⁺ / Mu⁺", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 17, "Verificación a flexión de las vigas estructurales.", headers)
        Dim f0 = fila

        Dim vigas = If(proyecto?.Elementos?.Vigas?.Vigas, New List(Of cViga))
        Dim vigasOrdenadas = vigas _
            .Where(Function(v) v IsNot Nothing AndAlso v.IncluirEnRevision AndAlso VigaTieneRefuerzo(v)) _
            .Select(Function(v, i) (V:=v, Ord:=OrdenPiso(v.Piso), Idx:=i)) _
            .OrderBy(Function(x) x.Ord).ThenBy(Function(x) x.Idx) _
            .Select(Function(x) x.V)

        For Each v In vigasOrdenadas
            Dim ratiosSup = New List(Of Double)
            Dim ratiosInf = New List(Of Double)
            Dim secciones As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            For Each fr In If(v.Frames, New List(Of cFrame))
                If fr?.Section IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(fr.Section.Nombre) Then
                    secciones.Add(fr.Section.Nombre)
                End If
                ' Solo contar ratios de frames con refuerzo real. Los demás son
                ' tramos auxiliares (riostras, fragmentos) que arrastrarían ceros.
                If Not VigaService.TieneRefuerzoFlexion(fr) Then Continue For
                For Each zona In If(fr.RevisionFlexion, New List(Of cRevisionFlexionZona))
                    Dim ra = zona?.ResultadoActual
                    If ra Is Nothing Then Continue For
                    If ra.RatioSup > 0 Then ratiosSup.Add(ra.RatioSup)
                    If ra.RatioInf > 0 Then ratiosInf.Add(ra.RatioInf)
                Next
            Next

            Dim rSup = If(ratiosSup.Count > 0, ratiosSup.Min(), 0.0)
            Dim rInf = If(ratiosInf.Count > 0, ratiosInf.Min(), 0.0)
            Dim peor = If(rSup = 0 AndAlso rInf = 0, 0.0,
                          Math.Min(If(rSup > 0, rSup, Double.MaxValue), If(rInf > 0, rInf, Double.MaxValue)))
            If peor = Double.MaxValue Then peor = 0

            ws.Cell(fila, 1).Value = v.Piso
            ws.Cell(fila, 2).Value = NombreViga(v)
            ws.Cell(fila, 3).Value = String.Join(", ", secciones)
            ws.Cell(fila, 4).Value = rSup : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 5).Value = rInf : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 6), peor, sinDato:=(peor = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' Nombre de viga para el informe: NombrePlano (el que tipeó el usuario) con
    ' fallback a Nombre auto-generado. Sin los prefijos [P]/[S] del display UI.
    Private Shared Function NombreViga(v As cViga) As String
        If v Is Nothing Then Return ""
        If Not String.IsNullOrWhiteSpace(v.NombrePlano) Then Return v.NombrePlano
        Return If(v.Nombre, "")
    End Function

    ' True si al menos un frame de la viga tiene refuerzo sup+inf real.
    Private Shared Function VigaTieneRefuerzo(v As cViga) As Boolean
        If v?.Frames Is Nothing Then Return False
        For Each fr In v.Frames
            If VigaService.TieneRefuerzoFlexion(fr) Then Return True
        Next
        Return False
    End Function

    ' =========================================================================
    '  NERVIOS (T19, T20)
    '  Una fila por nervio; iterar frames y tomar el mínimo de los ratios.
    ' =========================================================================

    ' Solo nervios donde el usuario ingresó refuerzo (al menos un frame con
    ' Ref_Modificado = True). Sin ese flag, el nervio no fue revisado — y los
    ' CD_M_* persistidos pueden ser basura de un cálculo preliminar.
    Private Shared Function NervioTieneRefuerzo(n As cNervio) As Boolean
        If n?.Frames Is Nothing Then Return False
        Return n.Frames.Any(Function(fn) fn IsNot Nothing AndAlso fn.Ref_Modificado)
    End Function

    Private Shared Function NombreNervio(n As cNervio) As String
        If n Is Nothing Then Return ""
        If Not String.IsNullOrWhiteSpace(n.NombrePlano) Then Return n.NombrePlano
        Return If(n.Nombre, "")
    End Function

    Private Shared Iterator Function EnumerarNerviosOrdenados(proyecto As Proyecto) As IEnumerable(Of cNervio)
        Dim lista = If(proyecto?.Elementos?.Nervios?.Elementos, New List(Of cNervio))
        Dim ordenados = lista _
            .Where(Function(n) n IsNot Nothing AndAlso NervioTieneRefuerzo(n)) _
            .Select(Function(n, i) (N:=n, Ord:=OrdenPiso(n.Piso), Idx:=i)) _
            .OrderBy(Function(x) x.Ord).ThenBy(Function(x) x.Idx) _
            .Select(Function(x) x.N)
        For Each n In ordenados
            Yield n
        Next
    End Function

    ' T19 — Flexión nervios
    Private Shared Sub EscribirT19_FlexionNervios(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T19_Flexion_Ner")
        Dim headers = {"PISO", "NERVIO", "SECCIÓN", "φMn⁻ / Mu⁻", "φMn⁺ / Mu⁺", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 19, "Verificación de la capacidad a flexión de los nervios.", headers)
        Dim f0 = fila

        For Each n In EnumerarNerviosOrdenados(proyecto)
            Dim ratiosNeg = New List(Of Double)
            Dim ratiosPos = New List(Of Double)
            Dim secciones As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each fn In If(n.Frames, New List(Of cFrameNervio))
                ' Solo frames que el usuario revisó; los demás arrastran ratios en 0 o basura legacy.
                If Not fn.Ref_Modificado Then Continue For
                If Not String.IsNullOrWhiteSpace(fn.NombreSeccion) Then secciones.Add(fn.NombreSeccion)
                If fn.CD_M_Sup_I > 0 Then ratiosNeg.Add(fn.CD_M_Sup_I)
                If fn.CD_M_Sup_D > 0 Then ratiosNeg.Add(fn.CD_M_Sup_D)
                If fn.CD_M_Inf_C > 0 Then ratiosPos.Add(fn.CD_M_Inf_C)
            Next
            Dim rNeg = If(ratiosNeg.Count > 0, ratiosNeg.Min(), 0.0)
            Dim rPos = If(ratiosPos.Count > 0, ratiosPos.Min(), 0.0)
            Dim peor = If(rNeg = 0 AndAlso rPos = 0, 0.0,
                          Math.Min(If(rNeg > 0, rNeg, Double.MaxValue), If(rPos > 0, rPos, Double.MaxValue)))
            If peor = Double.MaxValue Then peor = 0

            ws.Cell(fila, 1).Value = n.Piso
            ws.Cell(fila, 2).Value = NombreNervio(n)
            ws.Cell(fila, 3).Value = String.Join(", ", secciones)
            ws.Cell(fila, 4).Value = rNeg : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 5).Value = rPos : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 6), peor, sinDato:=(peor = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' T20 — Cortante nervios
    Private Shared Sub EscribirT20_CortanteNervios(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T20_Cortante_Ner")
        Dim headers = {"PISO", "NERVIO", "SECCIÓN", "φVn / Vu", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 20, "Verificación de la capacidad a cortante de los nervios.", headers)
        Dim f0 = fila

        For Each n In EnumerarNerviosOrdenados(proyecto)
            Dim ratios = New List(Of Double)
            Dim secciones As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each fn In If(n.Frames, New List(Of cFrameNervio))
                If Not fn.Ref_Modificado Then Continue For
                If Not String.IsNullOrWhiteSpace(fn.NombreSeccion) Then secciones.Add(fn.NombreSeccion)
                If fn.CD_Cortante_I > 0 Then ratios.Add(fn.CD_Cortante_I)
                If fn.CD_Cortante_D > 0 Then ratios.Add(fn.CD_Cortante_D)
            Next
            Dim r = If(ratios.Count > 0, ratios.Min(), 0.0)

            ws.Cell(fila, 1).Value = n.Piso
            ws.Cell(fila, 2).Value = NombreNervio(n)
            ws.Cell(fila, 3).Value = String.Join(", ", secciones)
            ws.Cell(fila, 4).Value = r : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 5), r, sinDato:=(r = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' =========================================================================
    '  PILAS (T22, T23, T24)
    '  Las pilas no tienen "piso" en el sentido del edificio; van en orden
    '  natural (como las ingresa el usuario / como vienen de la importación).
    ' =========================================================================

    Private Shared Function EnumerarPilas(proyecto As Proyecto) As IEnumerable(Of Elemento_Pila)
        Dim lista = If(proyecto?.Elementos?.Pilas?.ListaElementos, New List(Of Elemento_Pila))
        Return lista.Where(Function(p) p IsNot Nothing)
    End Function

    ' T22 — σ transmitido al suelo bajo condiciones ESTÁTICAS (Check 1)
    Private Shared Sub EscribirT22_SueloEstatico(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T22_Suelo_Estatico")
        Dim headers = {"PILA", "Dc (m)", "σ transmitido (kPa)", "C/D", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 22, "Verificación de esfuerzos admisibles en el suelo para condiciones estáticas.", headers)
        Dim f0 = fila

        For Each p In EnumerarPilas(proyecto)
            ws.Cell(fila, 1).Value = p.Name_Elemento
            ws.Cell(fila, 2).Value = p.Dc : ws.Cell(fila, 2).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 3).Value = p.EsfE_Trans : ws.Cell(fila, 3).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 4).Value = p.Check1_PsE : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 5), p.Check1_PsE, sinDato:=(p.Check1_PsE = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' T23 — σ transmitido al suelo bajo condiciones DINÁMICAS (Check 2)
    Private Shared Sub EscribirT23_SueloDinamico(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T23_Suelo_Dinamico")
        Dim headers = {"PILA", "Dc (m)", "σ transmitido (kPa)", "C/D", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 23, "Verificación de esfuerzos admisibles en el suelo para condiciones dinámicas.", headers)
        Dim f0 = fila

        For Each p In EnumerarPilas(proyecto)
            ws.Cell(fila, 1).Value = p.Name_Elemento
            ws.Cell(fila, 2).Value = p.Dc : ws.Cell(fila, 2).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 3).Value = p.EsfD_Trans : ws.Cell(fila, 3).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 4).Value = p.Check2_PsD : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 5), p.Check2_PsD, sinDato:=(p.Check2_PsD = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' T24 — σ admisible del CONCRETO en la cabeza de la pila (Check 3 estático
    ' y Check 4 dinámico). El veredicto global toma el peor de los dos checks.
    Private Shared Sub EscribirT24_ConcretoPilas(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T24_Concreto_Pilas")
        Dim headers = {"PILA", "Df (m)", "C/D estático", "C/D dinámico", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 24, "Verificación de esfuerzos del concreto en pilas.", headers)
        Dim f0 = fila

        For Each p In EnumerarPilas(proyecto)
            ' 0 = "no aplica" (misma convención que MinChequeoEsfuerzos / dashboards).
            Dim peor As Double
            If p.Check3_PuE = 0 AndAlso p.Check4_PuD = 0 Then
                peor = 0
            ElseIf p.Check3_PuE = 0 Then
                peor = p.Check4_PuD
            ElseIf p.Check4_PuD = 0 Then
                peor = p.Check3_PuE
            Else
                peor = Math.Min(p.Check3_PuE, p.Check4_PuD)
            End If

            ws.Cell(fila, 1).Value = p.Name_Elemento
            ws.Cell(fila, 2).Value = p.Df : ws.Cell(fila, 2).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 3).Value = p.Check3_PuE : ws.Cell(fila, 3).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 4).Value = p.Check4_PuD : ws.Cell(fila, 4).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 5), peor, sinDato:=(peor = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' =========================================================================
    '  VIGAS DE CIMENTACIÓN (T28)
    '  P_Max compresión = PhiPn_Max, P_Max tracción = |PhiPn_Min| (si negativo).
    '  Verificación: cumple cuando el Pu está dentro del DI (CD_Comp y CD_Trac ≥ 0.9).
    ' =========================================================================
    Private Shared Sub EscribirT28_VigasFundacion(wb As XLWorkbook, proyecto As Proyecto)
        Dim ws = wb.Worksheets.Add("T28_VigasFund")
        Dim headers = {"VIGA", "Pmáx compresión φPn (kN)", "Pmáx tracción φPn (kN)",
                       "Pu (kN)", "C/D compresión", "C/D tracción", "VERIFICACIÓN"}
        Dim fila = EscribirCabecera(ws, 28, "Verificación de fuerza de compresión y tracción en las vigas de cimentación.", headers)
        Dim f0 = fila

        Dim lista = If(proyecto?.Elementos?.VigasFundacion?.Elementos, New List(Of cVigaFundacion))
        For Each vf In lista.Where(Function(x) x IsNot Nothing)
            ' Peor ratio (mínimo entre compresión y tracción, ignorando 0 = "no aplica").
            Dim peor As Double
            If vf.CD_Comp > 0 AndAlso vf.CD_Trac > 0 Then
                peor = Math.Min(vf.CD_Comp, vf.CD_Trac)
            ElseIf vf.CD_Comp > 0 Then
                peor = vf.CD_Comp
            ElseIf vf.CD_Trac > 0 Then
                peor = vf.CD_Trac
            Else
                peor = 0
            End If

            ws.Cell(fila, 1).Value = If(String.IsNullOrWhiteSpace(vf.NombrePlano), vf.Nombre, vf.NombrePlano)
            ws.Cell(fila, 2).Value = vf.PhiPn_Max : ws.Cell(fila, 2).Style.NumberFormat.Format = "0"
            ws.Cell(fila, 3).Value = Math.Abs(vf.PhiPn_Min) : ws.Cell(fila, 3).Style.NumberFormat.Format = "0"
            ws.Cell(fila, 4).Value = vf.Pu : ws.Cell(fila, 4).Style.NumberFormat.Format = "0"
            ws.Cell(fila, 5).Value = vf.CD_Comp : ws.Cell(fila, 5).Style.NumberFormat.Format = "0.00"
            ws.Cell(fila, 6).Value = vf.CD_Trac : ws.Cell(fila, 6).Style.NumberFormat.Format = "0.00"
            AsignarVerificacion(ws.Cell(fila, 7), peor, sinDato:=(peor = 0))
            fila += 1
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' =========================================================================
    '  T15 — Relación viga-columna (NSR-10 C.21.6.2)
    '  Una fila por nodo × dirección (LL y LC — ejes locales de la columna del
    '  nudo). LL = paralelo al lado largo, LC = paralelo al lado corto. Reporta
    '  ΣMnc, ΣMnv y dos verdictos: DES (ratio ≥ 1.2) y DMO (ratio ≥ 1.0).
    '  Ver RelacionVigaColumnaService.
    ' =========================================================================
    Private Shared Sub EscribirT15_RelacionVigaColumna(wb As XLWorkbook, nodos As List(Of NodoRVC))
        Dim ws = wb.Worksheets.Add("T15_VigaColumna")
        Dim headers = {"PISO", "COORDENADA (X, Y)", "COL. ARRIBA", "COL. ABAJO", "DIRECCIÓN",
                       "ΣMnc (kN·m)", "ΣMnv (kN·m)", "Mc/Mv",
                       "VERIF. DES (≥1,2)", "VERIF. DMO (≥1,0)"}
        Dim fila = EscribirCabecera(ws, 15, "Verificación de la relación viga-columna (NSR-10 C.21.6.2).", headers)
        Dim f0 = fila

        ' Ordenados por Z ascendente (piso inferior → superior), luego X, Y.
        Dim ordenados = nodos.OrderBy(Function(n) n.Z).ThenBy(Function(n) n.X).ThenBy(Function(n) n.Y).ToList()

        For Each n In ordenados
            Dim nombreArr = If(n.ColumnaArriba Is Nothing, "—", n.ColumnaArriba.Name_Elemento)
            Dim nombreAba = If(n.ColumnaAbajo Is Nothing, "—", n.ColumnaAbajo.Name_Elemento)
            Dim coord = $"({n.X:0.00}, {n.Y:0.00})"

            For Each r In {n.DireccionLL, n.DireccionLC}
                ws.Cell(fila, 1).Value = n.Piso
                ws.Cell(fila, 2).Value = coord
                ws.Cell(fila, 3).Value = nombreArr
                ws.Cell(fila, 4).Value = nombreAba
                ws.Cell(fila, 5).Value = If(r.Direccion = eDirColumna.LL, "LL", "LC")
                ws.Cell(fila, 6).Value = r.SumMnc : ws.Cell(fila, 6).Style.NumberFormat.Format = "0"
                ws.Cell(fila, 7).Value = r.SumMnv : ws.Cell(fila, 7).Style.NumberFormat.Format = "0"
                ws.Cell(fila, 8).Value = r.Ratio : ws.Cell(fila, 8).Style.NumberFormat.Format = "0.00"

                AsignarVerifRVC(ws.Cell(fila, 9), r.Ratio, RelacionVigaColumnaService.RATIO_DES)
                AsignarVerifRVC(ws.Cell(fila, 10), r.Ratio, RelacionVigaColumnaService.RATIO_DMO)
                fila += 1
            Next
        Next

        TerminarTabla(ws, f0, fila - 1, headers.Length)
    End Sub

    ' Variante de AsignarVerificacion con umbral configurable (DES=1.2, DMO=1.0).
    Private Shared Sub AsignarVerifRVC(cell As IXLCell, ratio As Double, umbral As Double)
        If ratio <= 0 Then
            cell.Value = "—"
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            Return
        End If
        Dim cumple As Boolean = ratio >= umbral
        cell.Value = If(cumple, "Cumple", "No cumple")
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

    ''' <summary>
    ''' Genera un .xlsx aislado con solo la hoja T15 — usado por el botón "Exportar"
    ''' de Form_15_RelacionVigaColumna. Los nodos ya están evaluados por la UI.
    ''' </summary>
    Public Shared Sub GenerarSoloRVC(nodos As List(Of NodoRVC), rutaSalida As String, proyecto As Proyecto)
        InicializarOrdenPisos(proyecto)
        Using wb As New XLWorkbook()
            Dim opts As New OpcionesInformeInicial With {
                .IncluirColumnas = False, .IncluirVigas = False, .IncluirNervios = False,
                .IncluirPilas = False, .IncluirVigasFundacion = False,
                .IncluirRelacionVigaColumna = True,
                .NombreProyecto = If(proyecto?.Info?.Nombre, "")
            }
            EscribirPortada(wb, proyecto, opts)
            EscribirT15_RelacionVigaColumna(wb, nodos)
            wb.SaveAs(rutaSalida)
        End Using
    End Sub

End Class
