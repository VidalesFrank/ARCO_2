Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Helpers de las grillas en pantalla de los formularios de reporte. Es la
''' contraparte WinForms de <see cref="ReporteHelpers"/>, que hace lo mismo del
''' lado de Excel.
'''
''' Al comparar las copias duplicadas apareció que las "cinco variantes" de
''' EstilarGrid eran en realidad DOS familias:
'''
'''   Amplio    10 pt, encabezado 42 px, fila 28, bordes horizontales, colores
'''             de selección. Lo usan Muros, Vigas de Fundación, Nervios y el
'''             Resumen de Vigas — y entre esos cuatro eran idénticos salvo el
'''             nombre local del color del encabezado, que en todos es blanco.
'''
'''   Compacto  9.5 pt, encabezado 34 px, sin bordes de celda ni colores de
'''             selección. Lo usan Pilas y Zapatas, que solo se diferencian en
'''             el alto de fila.
'''
''' Igual que en ReporteHelpers, los parámetros reproducen exactamente lo que
''' hacía cada reporte: centralizar no cambió ningún comportamiento.
''' </summary>
Public NotInheritable Class ReporteGridHelpers

    Private Sub New()
    End Sub

    ' -----------------------------------------------------------------------
    ' Paleta — era idéntica en los seis reportes, solo cambiaban los nombres
    ' locales (ColorOK / ClOK, ColorOKTexto / ColorOKTxt / ClOKTexto, ...).
    ' -----------------------------------------------------------------------
    Public Shared ReadOnly ColorEncabezado As Color = Color.FromArgb(87, 87, 87)
    Public Shared ReadOnly ColorEncabezadoTexto As Color = Color.White
    Public Shared ReadOnly ColorOK As Color = ColorTranslator.FromHtml("#C6EFCE")
    Public Shared ReadOnly ColorOKTexto As Color = ColorTranslator.FromHtml("#006100")
    Public Shared ReadOnly ColorMal As Color = ColorTranslator.FromHtml("#FFC7CE")
    Public Shared ReadOnly ColorMalTexto As Color = ColorTranslator.FromHtml("#9C0006")
    Public Shared ReadOnly ColorAlerta As Color = ColorTranslator.FromHtml("#FFEB9C")
    Public Shared ReadOnly ColorAlertaTexto As Color = ColorTranslator.FromHtml("#9C5700")

    ''' <summary>
    ''' Fuentes compartidas para no crear una por celda. Viven lo que vive la
    ''' aplicación: no llamar Dispose sobre ellas.
    ''' </summary>
    Public Shared ReadOnly FuenteNegrita As New Font("Segoe UI", 10, FontStyle.Bold)
    Public Shared ReadOnly FuenteNegritaCompacta As New Font("Segoe UI", 9.5!, FontStyle.Bold)

    Public Enum EstiloGrid
        ''' <summary>10 pt, encabezado 42, fila 28. Muros, Vigas de Fundación, Nervios, Resumen.</summary>
        Amplio = 0
        ''' <summary>9.5 pt, encabezado 34. Pilas y Zapatas.</summary>
        Compacto = 1
    End Enum

    ' -----------------------------------------------------------------------
    ' Estilo de la grilla
    ' -----------------------------------------------------------------------
    ''' <param name="altoFila">
    ''' -1 usa el del estilo: 28 en Amplio, y en Compacto deja que la fila se
    ''' ajuste al contenido (AutoSizeRowsMode.AllCells, como hacía Pilas).
    ''' Un valor mayor que 0 fija ese alto (Zapatas usaba 26).
    ''' </param>
    Public Shared Sub EstilarGrid(dgv As DataGridView,
                                  Optional estilo As EstiloGrid = EstiloGrid.Amplio,
                                  Optional altoFila As Integer = -1)

        If dgv Is Nothing Then Exit Sub

        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.RowHeadersVisible = False
        dgv.BackgroundColor = Color.White
        dgv.BorderStyle = BorderStyle.None
        dgv.EnableHeadersVisualStyles = False
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect

        Dim tam As Single = If(estilo = EstiloGrid.Amplio, 10.0!, 9.5!)

        If estilo = EstiloGrid.Amplio Then
            dgv.MultiSelect = False
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            dgv.GridColor = Color.FromArgb(210, 210, 210)
            dgv.ColumnHeadersHeight = 42
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        Else
            dgv.GridColor = Color.FromArgb(220, 220, 220)
            dgv.ColumnHeadersHeight = 34
        End If

        With dgv.ColumnHeadersDefaultCellStyle
            .BackColor = ColorEncabezado
            .ForeColor = ColorEncabezadoTexto
            .Font = New Font("Segoe UI", tam, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With

        With dgv.DefaultCellStyle
            .Font = New Font("Segoe UI", tam)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
            If estilo = EstiloGrid.Amplio Then
                .BackColor = Color.White
                .ForeColor = Color.Black
                .SelectionBackColor = Color.FromArgb(200, 225, 255)
                .SelectionForeColor = Color.Black
            End If
        End With

        Dim alto As Integer = altoFila
        If alto < 0 Then alto = If(estilo = EstiloGrid.Amplio, 28, 0)

        If alto > 0 Then
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
            dgv.RowTemplate.Height = alto
        Else
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        End If

    End Sub

    ' -----------------------------------------------------------------------
    ' Columnas
    ' -----------------------------------------------------------------------
    ''' <param name="ordenable">
    ''' False deja la columna sin ordenar al hacer clic en el encabezado. Es lo
    ''' que hacía el helper "Col" de Pilas y Zapatas; el "AgregarColumna" de los
    ''' otros reportes dejaba el orden por defecto.
    ''' </param>
    Public Shared Sub AgregarColumna(dgv As DataGridView, nombre As String,
                                     encabezado As String, ancho As Integer,
                                     Optional ordenable As Boolean = True)

        If dgv Is Nothing Then Exit Sub

        Dim col As New DataGridViewTextBoxColumn() With {
            .Name = nombre,
            .HeaderText = encabezado,
            .Width = ancho
        }
        If Not ordenable Then col.SortMode = DataGridViewColumnSortMode.NotSortable

        dgv.Columns.Add(col)

    End Sub

    ' -----------------------------------------------------------------------
    ' Celda de C/D con semáforo
    ' -----------------------------------------------------------------------
    ''' <param name="guionSinDato">
    ''' True escribe "—" cuando el valor es &lt;= 0 o Double.MaxValue, como hacía
    ''' el reporte de Nervios. Vigas de Fundación no lo contemplaba.
    ''' </param>
    ''' <param name="negrita">Vigas de Fundación ponía la celda en negrita; Nervios no.</param>
    Public Shared Sub AsignarCD(cell As DataGridViewCell, cd As Double,
                                Optional guionSinDato As Boolean = False,
                                Optional negrita As Boolean = False)

        If cell Is Nothing Then Exit Sub

        If guionSinDato AndAlso (cd <= 0 OrElse cd = Double.MaxValue) Then
            cell.Value = "—"
            Exit Sub
        End If

        Dim v As Double = Math.Round(Math.Min(cd, 9.99), 2)
        cell.Value = v.ToString("F2")

        If cd >= Funciones_00_Varias.UMBRAL_CD Then
            cell.Style.BackColor = ColorOK
            cell.Style.ForeColor = ColorOKTexto
        Else
            cell.Style.BackColor = ColorMal
            cell.Style.ForeColor = ColorMalTexto
        End If

        If negrita Then cell.Style.Font = FuenteNegrita

    End Sub

    ' -----------------------------------------------------------------------
    ' Celda de valor físico (presión, fuerza, momento...)
    ' -----------------------------------------------------------------------
    ''' <summary>
    ''' Contraparte en pantalla de <see cref="ReporteHelpers.EscribirValor"/>.
    ''' Un número con unidades no lleva semáforo de C/D: 180 kN/m2 no es "bueno"
    ''' ni "malo" por sí solo, solo comparado con su admisible.
    ''' </summary>
    ''' <param name="resaltarNegativo">
    ''' Ámbar para los negativos. En presiones de contacto significan tracción.
    ''' </param>
    Public Shared Sub AsignarValor(cell As DataGridViewCell, valor As Double,
                                   Optional decimales As Integer = 2,
                                   Optional resaltarNegativo As Boolean = False)

        If cell Is Nothing Then Exit Sub

        If Double.IsNaN(valor) OrElse Double.IsInfinity(valor) Then
            cell.Value = "—"
            Exit Sub
        End If

        cell.Value = Math.Round(valor, decimales).ToString("F" & decimales)

        If resaltarNegativo AndAlso valor < 0 Then
            cell.Style.BackColor = ColorAlerta
            cell.Style.ForeColor = ColorAlertaTexto
        End If

    End Sub

    ' -----------------------------------------------------------------------
    ' Celda de estado
    ' -----------------------------------------------------------------------
    Public Shared Sub AsignarEstado(cell As DataGridViewCell, texto As String,
                                    fondo As Color, colorTexto As Color)
        If cell Is Nothing Then Exit Sub
        cell.Value = texto
        cell.Style.BackColor = fondo
        cell.Style.ForeColor = colorTexto
    End Sub

    ''' <param name="negrita">
    ''' Zapatas ponía el estado en negrita compacta (9.5); Pilas no lo hacía.
    ''' </param>
    Public Shared Sub AsignarEstado(cell As DataGridViewCell, cumple As Boolean,
                                    Optional textoOK As String = "Ok",
                                    Optional textoMal As String = "Revisar",
                                    Optional negrita As Boolean = False)

        AsignarEstado(cell,
                      If(cumple, textoOK, textoMal),
                      If(cumple, ColorOK, ColorMal),
                      If(cumple, ColorOKTexto, ColorMalTexto))

        If negrita AndAlso cell IsNot Nothing Then cell.Style.Font = FuenteNegritaCompacta

    End Sub

End Class
