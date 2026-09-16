Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Drawing
Imports System.Windows.Forms
Imports ARCO

''' <summary>
''' Fija el comportamiento de ReporteGridHelpers, la clase que unificó los
''' helpers de las grillas en pantalla de los formularios de reporte.
'''
''' Cada prueba comprueba una de las configuraciones tal como la usaba su
''' reporte, así que si la centralización hubiera cambiado el aspecto de alguno,
''' falla: el estilo amplio de Muros/Vigas de Fundación/Nervios/Resumen, el
''' compacto de Pilas y Zapatas, y las variantes de C/D y estado.
''' </summary>
<TestClass>
Public Class ReporteGridHelpersTests

    Private _dgv As DataGridView

    <TestInitialize>
    Public Sub Preparar()
        _dgv = New DataGridView()
    End Sub

    <TestCleanup>
    Public Sub Limpiar()
        If _dgv IsNot Nothing Then _dgv.Dispose()
    End Sub

    Private Function CeldaSuelta() As DataGridViewCell
        _dgv.Columns.Clear()
        _dgv.Rows.Clear()
        _dgv.AllowUserToAddRows = False
        _dgv.Columns.Add("c", "c")
        _dgv.Rows.Add()
        Return _dgv.Rows(0).Cells(0)
    End Function

    ' =====================================================================
    ' EstilarGrid
    ' =====================================================================

    ''' <summary>Estilo de Muros, Vigas de Fundación, Nervios y el Resumen de Vigas.</summary>
    <TestMethod>
    Public Sub EstilarGrid_Amplio_UsaLaMaquetaDeDiezPuntos()
        ReporteGridHelpers.EstilarGrid(_dgv)
        Assert.AreEqual(42, _dgv.ColumnHeadersHeight, "alto de encabezado")
        Assert.AreEqual(28, _dgv.RowTemplate.Height, "alto de fila")
        Assert.AreEqual(10.0F, _dgv.DefaultCellStyle.Font.Size, 0.01, "tamaño de fuente")
        Assert.AreEqual(DataGridViewCellBorderStyle.SingleHorizontal, _dgv.CellBorderStyle)
        Assert.AreEqual(Color.FromArgb(210, 210, 210), _dgv.GridColor)
        Assert.IsFalse(_dgv.MultiSelect)
        Assert.AreEqual(Color.FromArgb(200, 225, 255), _dgv.DefaultCellStyle.SelectionBackColor)
    End Sub

    ''' <summary>Estilo de Pilas y Zapatas.</summary>
    <TestMethod>
    Public Sub EstilarGrid_Compacto_UsaLaMaquetaDeNueveYMedio()
        ReporteGridHelpers.EstilarGrid(_dgv, ReporteGridHelpers.EstiloGrid.Compacto)
        Assert.AreEqual(34, _dgv.ColumnHeadersHeight, "alto de encabezado")
        Assert.AreEqual(9.5F, _dgv.DefaultCellStyle.Font.Size, 0.01, "tamaño de fuente")
        Assert.AreEqual(Color.FromArgb(220, 220, 220), _dgv.GridColor)
    End Sub

    ''' <summary>Pilas dejaba que la fila se ajustara al contenido.</summary>
    <TestMethod>
    Public Sub EstilarGrid_CompactoSinAlto_AjustaLaFilaAlContenido()
        ReporteGridHelpers.EstilarGrid(_dgv, ReporteGridHelpers.EstiloGrid.Compacto)
        Assert.AreEqual(DataGridViewAutoSizeRowsMode.AllCells, _dgv.AutoSizeRowsMode)
    End Sub

    ''' <summary>Zapatas fijaba 26 px por fila.</summary>
    <TestMethod>
    Public Sub EstilarGrid_CompactoConAlto_FijaEseAlto()
        ReporteGridHelpers.EstilarGrid(_dgv, ReporteGridHelpers.EstiloGrid.Compacto, altoFila:=26)
        Assert.AreEqual(DataGridViewAutoSizeRowsMode.None, _dgv.AutoSizeRowsMode)
        Assert.AreEqual(26, _dgv.RowTemplate.Height)
    End Sub

    ''' <summary>Lo que comparten los dos estilos: solo lectura, sin cabeceras de fila, encabezado gris ARCO.</summary>
    <TestMethod>
    Public Sub EstilarGrid_AmbosEstilos_CompartenLoBasico()
        For Each estilo In New ReporteGridHelpers.EstiloGrid() {ReporteGridHelpers.EstiloGrid.Amplio,
                                                               ReporteGridHelpers.EstiloGrid.Compacto}
            Using d As New DataGridView()
                ReporteGridHelpers.EstilarGrid(d, estilo)
                Assert.IsTrue(d.ReadOnly, $"{estilo}: solo lectura")
                Assert.IsFalse(d.AllowUserToAddRows, $"{estilo}: sin fila nueva")
                Assert.IsFalse(d.RowHeadersVisible, $"{estilo}: sin cabecera de fila")
                Assert.IsFalse(d.EnableHeadersVisualStyles, $"{estilo}: encabezado propio")
                Assert.AreEqual(ReporteGridHelpers.ColorEncabezado,
                                d.ColumnHeadersDefaultCellStyle.BackColor, $"{estilo}: color de encabezado")
                Assert.AreEqual(DataGridViewSelectionMode.FullRowSelect, d.SelectionMode, $"{estilo}: selección")
            End Using
        Next
    End Sub

    ' =====================================================================
    ' AgregarColumna
    ' =====================================================================

    <TestMethod>
    Public Sub AgregarColumna_CreaLaColumnaConSuAncho()
        ReporteGridHelpers.AgregarColumna(_dgv, "Piso", "Piso", 120)
        Assert.AreEqual(1, _dgv.Columns.Count)
        Assert.AreEqual("Piso", _dgv.Columns(0).Name)
        Assert.AreEqual("Piso", _dgv.Columns(0).HeaderText)
        Assert.AreEqual(120, _dgv.Columns(0).Width)
    End Sub

    ''' <summary>El helper "Col" de Pilas y Zapatas dejaba las columnas sin ordenar.</summary>
    <TestMethod>
    Public Sub AgregarColumna_NoOrdenable_DesactivaElOrden()
        ReporteGridHelpers.AgregarColumna(_dgv, "a", "A", 80, ordenable:=False)
        Assert.AreEqual(DataGridViewColumnSortMode.NotSortable, _dgv.Columns(0).SortMode)
    End Sub

    <TestMethod>
    Public Sub AgregarColumna_PorDefectoEsOrdenable()
        ReporteGridHelpers.AgregarColumna(_dgv, "a", "A", 80)
        Assert.AreNotEqual(DataGridViewColumnSortMode.NotSortable, _dgv.Columns(0).SortMode)
    End Sub

    ' =====================================================================
    ' AsignarCD
    ' =====================================================================

    <TestMethod>
    Public Sub AsignarCD_CumpleVaEnVerdeConDosDecimales()
        Dim c = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c, 1.234)
        Assert.AreEqual("1.23", Convert.ToString(c.Value))
        Assert.AreEqual(ReporteGridHelpers.ColorOK, c.Style.BackColor)
        Assert.AreEqual(ReporteGridHelpers.ColorOKTexto, c.Style.ForeColor)
    End Sub

    <TestMethod>
    Public Sub AsignarCD_NoCumpleVaEnRojo()
        Dim c = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c, 0.75)
        Assert.AreEqual(ReporteGridHelpers.ColorMal, c.Style.BackColor)
    End Sub

    <TestMethod>
    Public Sub AsignarCD_RecortaEn999()
        Dim c = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c, 500.0)
        Assert.AreEqual("9.99", Convert.ToString(c.Value))
    End Sub

    ''' <summary>Nervios escribe "—" cuando no hay dato; Vigas de Fundación no lo hacía.</summary>
    <TestMethod>
    Public Sub AsignarCD_ConGuion_MarcaLoQueNoTieneDato()
        Dim c1 = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c1, 0.0, guionSinDato:=True)
        Assert.AreEqual("—", Convert.ToString(c1.Value), "cero")

        Dim c2 = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c2, Double.MaxValue, guionSinDato:=True)
        Assert.AreEqual("—", Convert.ToString(c2.Value), "centinela")
    End Sub

    <TestMethod>
    Public Sub AsignarCD_SinGuion_ElCeroSeEscribeComoNumero()
        Dim c = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c, 0.0)
        Assert.AreEqual("0.00", Convert.ToString(c.Value))
        Assert.AreEqual(ReporteGridHelpers.ColorMal, c.Style.BackColor)
    End Sub

    <TestMethod>
    Public Sub AsignarCD_Negrita_SoloCuandoSePide()
        Dim c1 = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c1, 1.5, negrita:=True)
        Assert.IsNotNull(c1.Style.Font)
        Assert.IsTrue(c1.Style.Font.Bold)

        Dim c2 = CeldaSuelta()
        ReporteGridHelpers.AsignarCD(c2, 1.5)
        Assert.IsNull(c2.Style.Font, "sin pedirla no debe fijar fuente")
    End Sub

    ' =====================================================================
    ' AsignarEstado
    ' =====================================================================

    ''' <summary>Pilas usaba "Ok" / "Revisar", sin negrita.</summary>
    <TestMethod>
    Public Sub AsignarEstado_TextosPorDefecto()
        Dim ok = CeldaSuelta()
        ReporteGridHelpers.AsignarEstado(ok, True)
        Assert.AreEqual("Ok", Convert.ToString(ok.Value))
        Assert.AreEqual(ReporteGridHelpers.ColorOK, ok.Style.BackColor)

        Dim mal = CeldaSuelta()
        ReporteGridHelpers.AsignarEstado(mal, False)
        Assert.AreEqual("Revisar", Convert.ToString(mal.Value))
        Assert.AreEqual(ReporteGridHelpers.ColorMal, mal.Style.BackColor)
    End Sub

    ''' <summary>Zapatas usaba "Cumple" / "No cumple" y en negrita.</summary>
    <TestMethod>
    Public Sub AsignarEstado_TextosYNegritaDeZapatas()
        Dim c = CeldaSuelta()
        ReporteGridHelpers.AsignarEstado(c, False, "Cumple", "No cumple", negrita:=True)
        Assert.AreEqual("No cumple", Convert.ToString(c.Value))
        Assert.IsNotNull(c.Style.Font)
        Assert.IsTrue(c.Style.Font.Bold)
    End Sub

    ''' <summary>La sobrecarga con colores explícitos, que usan Muros y el Resumen.</summary>
    <TestMethod>
    Public Sub AsignarEstado_ConColoresExplicitos()
        Dim c = CeldaSuelta()
        ReporteGridHelpers.AsignarEstado(c, "Sin ref.",
                                         ReporteGridHelpers.ColorAlerta,
                                         ReporteGridHelpers.ColorAlertaTexto)
        Assert.AreEqual("Sin ref.", Convert.ToString(c.Value))
        Assert.AreEqual(ReporteGridHelpers.ColorAlerta, c.Style.BackColor)
        Assert.AreEqual(ReporteGridHelpers.ColorAlertaTexto, c.Style.ForeColor)
    End Sub

End Class
