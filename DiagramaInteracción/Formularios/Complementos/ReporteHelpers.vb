Imports ClosedXML.Excel

''' <summary>
''' Helpers de exportación a Excel compartidos por los formularios de reporte.
'''
''' Antes de esto, cada Form_Reporte_* llevaba su propia copia: EscribirFactor
''' estaba definido seis veces, EscribirEncabezados seis, AjustarColumnas cuatro
''' y AgregarBordesTabla cuatro. Y no eran copias iguales — se habían ido
''' desviando entre sí, así que un cambio de estilo había que repetirlo en seis
''' sitios y era fácil que uno quedara distinto.
'''
''' Los parámetros opcionales existen para reproducir EXACTAMENTE lo que hacía
''' cada reporte. No se unificó ningún comportamiento al centralizar: primero
''' quitar la duplicación, después decidir si conviene estandarizar. Las
''' diferencias reales entre reportes están documentadas en cada parámetro.
''' </summary>
Public NotInheritable Class ReporteHelpers

    Private Sub New()
    End Sub

    ' -----------------------------------------------------------------------
    ' Paleta Excel — idéntica en todos los reportes, solo cambiaban los nombres
    ' locales (XlClOK / XlOKFondo, XlClEnc / XlEncabezado, ...).
    ' -----------------------------------------------------------------------
    Public Shared ReadOnly XlEncabezado As XLColor = XLColor.FromHtml("#575757")
    Public Shared ReadOnly XlOKFondo As XLColor = XLColor.FromHtml("#C6EFCE")
    Public Shared ReadOnly XlOKTexto As XLColor = XLColor.FromHtml("#006100")
    Public Shared ReadOnly XlMalFondo As XLColor = XLColor.FromHtml("#FFC7CE")
    Public Shared ReadOnly XlMalTexto As XLColor = XLColor.FromHtml("#9C0006")
    Public Shared ReadOnly XlAlertaFondo As XLColor = XLColor.FromHtml("#FFEB9C")
    Public Shared ReadOnly XlAlertaTexto As XLColor = XLColor.FromHtml("#9C5700")

    ''' <summary>
    ''' Fila alternada. Ojo: aquí SÍ había dos valores distintos en producción,
    ''' #F8F8F8 en cuatro reportes y #F0F4FA en dos. Se toma el mayoritario; el
    ''' que quiera el otro que lo pase explícito.
    ''' </summary>
    Public Shared ReadOnly XlFilaPar As XLColor = XLColor.FromHtml("#F8F8F8")

    ''' <summary>
    ''' Cómo representa cada reporte un valor que no tiene cálculo. Los seis
    ''' EscribirFactor originales usaban tres convenciones distintas.
    ''' </summary>
    Public Enum SinDato
        ''' <summary>No se contempla: se escribe el número tal cual, aunque sea 0.</summary>
        NoAplica = 0
        ''' <summary>valor &lt;= 0 se escribe como "-".</summary>
        CeroOMenor = 1
        ''' <summary>valor = Double.MaxValue se escribe como "-".</summary>
        MaxValue = 2
    End Enum

    ' -----------------------------------------------------------------------
    ' Celda de factor C/D, con semáforo
    ' -----------------------------------------------------------------------
    ''' <param name="umbralOK">
    ''' Valor a partir del cual la celda va en verde. Por defecto el umbral
    ''' único del programa (0.90). Los reportes de Zapatas y Proyecto Completo
    ''' usan 1.0 junto con la banda de alerta.
    ''' </param>
    ''' <param name="conBandaAlerta">
    ''' True añade una banda ámbar intermedia entre 0.90 y <paramref name="umbralOK"/>.
    ''' Es el semáforo de tres bandas de Zapatas y Proyecto Completo: verde
    ''' "cumple con holgura", ámbar "cumple, pero justo", rojo "no cumple".
    ''' </param>
    Public Shared Sub EscribirFactor(cell As IXLCell, valor As Double,
                                     Optional sinDato As SinDato = SinDato.NoAplica,
                                     Optional umbralOK As Double = Funciones_00_Varias.UMBRAL_CD,
                                     Optional conBandaAlerta As Boolean = False)

        If cell Is Nothing Then Exit Sub

        If (sinDato = SinDato.CeroOMenor AndAlso valor <= 0) OrElse
           (sinDato = SinDato.MaxValue AndAlso valor = Double.MaxValue) Then
            cell.Value = "-"
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
            Exit Sub
        End If

        Dim v As Double = Math.Round(Math.Min(valor, 9.99), 2)
        cell.Value = v
        cell.Style.NumberFormat.Format = "0.00"
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center

        If v >= umbralOK Then
            cell.Style.Fill.BackgroundColor = XlOKFondo
            cell.Style.Font.FontColor = XlOKTexto
        ElseIf conBandaAlerta AndAlso v >= Funciones_00_Varias.UMBRAL_CD Then
            cell.Style.Fill.BackgroundColor = XlAlertaFondo
            cell.Style.Font.FontColor = XlAlertaTexto
        Else
            cell.Style.Fill.BackgroundColor = XlMalFondo
            cell.Style.Font.FontColor = XlMalTexto
        End If

        cell.Style.Font.Bold = True

    End Sub

    ' -----------------------------------------------------------------------
    ' Fila de encabezados
    ' -----------------------------------------------------------------------
    ''' <param name="conBorde">
    ''' Borde blanco fino entre encabezados. El reporte de Muros era el único
    ''' que no lo ponía.
    ''' </param>
    Public Shared Sub EscribirEncabezados(ws As IXLWorksheet, fila As Integer, enc As String(),
                                          Optional tamanoFuente As Double = 11,
                                          Optional altoFila As Double = 22,
                                          Optional conBorde As Boolean = True)

        If ws Is Nothing OrElse enc Is Nothing Then Exit Sub

        For i As Integer = 0 To enc.Length - 1
            Dim cell = ws.Cell(fila, i + 1)
            cell.Value = enc(i)
            With cell.Style
                .Fill.BackgroundColor = XlEncabezado
                .Font.FontColor = XLColor.White
                .Font.Bold = True
                .Font.FontSize = tamanoFuente
                .Alignment.Horizontal = XLAlignmentHorizontalValues.Center
                .Alignment.Vertical = XLAlignmentVerticalValues.Center
                If conBorde Then
                    .Border.OutsideBorder = XLBorderStyleValues.Thin
                    .Border.OutsideBorderColor = XLColor.White
                End If
            End With
        Next

        ws.Row(fila).Height = altoFila

    End Sub

    ' -----------------------------------------------------------------------
    ' Celda de estado (OK / Revisar / Cumple...)
    ' -----------------------------------------------------------------------
    Public Shared Sub EscribirEstado(cell As IXLCell, texto As String,
                                     fondo As XLColor, colorTexto As XLColor)
        If cell Is Nothing Then Exit Sub
        cell.Value = texto
        cell.Style.Fill.BackgroundColor = fondo
        cell.Style.Font.FontColor = colorTexto
        cell.Style.Font.Bold = True
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center
    End Sub

    ''' <summary>Estado booleano con el semáforo estándar OK / Revisar.</summary>
    Public Shared Sub EscribirEstado(cell As IXLCell, cumple As Boolean,
                                     Optional textoOK As String = "OK",
                                     Optional textoMal As String = "Revisar")
        EscribirEstado(cell,
                       If(cumple, textoOK, textoMal),
                       If(cumple, XlOKFondo, XlMalFondo),
                       If(cumple, XlOKTexto, XlMalTexto))
    End Sub

    ' -----------------------------------------------------------------------
    ' Ancho de columnas + congelar encabezado
    ' -----------------------------------------------------------------------
    ''' <param name="anchoMaximo">Tope de ancho. Muros usaba 40; el resto, 45.</param>
    ''' <param name="columnaAncha">
    ''' Columna que necesita un mínimo garantizado (la de Frames de ETABS, que
    ''' trae etiquetas largas). 0 = ninguna.
    ''' </param>
    Public Shared Sub AjustarColumnas(ws As IXLWorksheet, numCols As Integer,
                                      Optional anchoMaximo As Double = 45,
                                      Optional columnaAncha As Integer = 0,
                                      Optional anchoMinimo As Double = 0)

        If ws Is Nothing OrElse numCols < 1 Then Exit Sub

        ws.Columns(1, numCols).AdjustToContents()

        If columnaAncha >= 1 AndAlso columnaAncha <= numCols AndAlso anchoMinimo > 0 Then
            If ws.Column(columnaAncha).Width < anchoMinimo Then
                ws.Column(columnaAncha).Width = anchoMinimo
            End If
        End If

        For c As Integer = 1 To numCols
            If ws.Column(c).Width > anchoMaximo Then ws.Column(c).Width = anchoMaximo
        Next

        ws.SheetView.FreezeRows(1)

    End Sub

    ' -----------------------------------------------------------------------
    ' Fila de datos: alternado y alineación
    ' -----------------------------------------------------------------------
    ''' <param name="columnasIzquierda">
    ''' Cuántas columnas de la izquierda se alinean a la izquierda en vez de al
    ''' centro: son las de texto (piso, elemento, tramo). Vigas de Fundación
    ''' usaba 2; Nervios y el Resumen de Vigas, 3.
    ''' </param>
    ''' <remarks>
    ''' El fondo alternado solo se aplica si la celda no tiene ya uno propio, para
    ''' no pisar el semáforo de las columnas de C/D.
    ''' </remarks>
    Public Shared Sub EstilarFilaDatos(ws As IXLWorksheet, fila As Integer,
                                       numCols As Integer, esPar As Boolean,
                                       Optional columnasIzquierda As Integer = 3,
                                       Optional altoFila As Double = 18)

        If ws Is Nothing Then Exit Sub

        ws.Row(fila).Height = altoFila

        For col As Integer = 1 To numCols
            Dim cell = ws.Cell(fila, col)
            If esPar AndAlso cell.Style.Fill.BackgroundColor = XLColor.NoColor Then
                cell.Style.Fill.BackgroundColor = XlFilaPar
            End If
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center
            If col <= columnasIzquierda Then
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left
            End If
        Next

    End Sub

    ' -----------------------------------------------------------------------
    ' Bordes del bloque de datos
    ' -----------------------------------------------------------------------
    Public Shared Sub AgregarBordesTabla(ws As IXLWorksheet,
                                         filaIni As Integer, filaFin As Integer,
                                         numCols As Integer)

        If ws Is Nothing OrElse filaFin < filaIni Then Exit Sub

        Dim rango = ws.Range(filaIni, 1, filaFin, numCols)
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Hair
        rango.Style.Border.InsideBorderColor = XLColor.FromHtml("#CCCCCC")
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Medium
        rango.Style.Border.OutsideBorderColor = XlEncabezado

    End Sub

End Class
