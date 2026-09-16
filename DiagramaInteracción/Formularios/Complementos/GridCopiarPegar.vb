''' <summary>
''' Copiar / pegar / rellenar en un DataGridView, con el mismo comportamiento que
''' espera cualquiera que venga de Excel.
'''
''' El helper que ya existía en Form_09_Vigas copiaba UNA sola celda y al pegar
''' solo aceptaba enteros (Integer.TryParse), lo que lo hacía inservible para
''' valores decimales como una altura de 2.95 m. Este soporta rangos, decimales
''' y el formato TSV del portapapeles, así que se puede copiar una columna desde
''' Excel y pegarla de un golpe.
'''
''' Atajos que habilita:
'''   Ctrl+C   copia el rango seleccionado (tabulador entre columnas, salto de
'''            línea entre filas: lo que Excel entiende)
'''   Ctrl+V   pega desde la celda actual hacia abajo y a la derecha, saltando
'''            las columnas de solo lectura
'''   Ctrl+D   rellena hacia abajo: repite el valor de la primera fila
'''            seleccionada en el resto de la selección
'''
''' Y un menú contextual con lo mismo, para quien no use atajos.
''' </summary>
Public NotInheritable Class GridCopiarPegar

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Activa copiar/pegar/rellenar en la grilla. Idempotente: llamarlo dos
    ''' veces sobre la misma grilla no duplica los manejadores.
    ''' </summary>
    Public Shared Sub Activar(dgv As DataGridView)

        If dgv Is Nothing Then Exit Sub
        If dgv.Tag IsNot Nothing AndAlso Convert.ToString(dgv.Tag) = "CopiarPegar" Then Exit Sub

        dgv.MultiSelect = True
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect
        dgv.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText

        AddHandler dgv.KeyDown, AddressOf Grid_KeyDown

        dgv.ContextMenuStrip = CrearMenu(dgv)
        dgv.Tag = "CopiarPegar"

    End Sub

    Private Shared Function CrearMenu(dgv As DataGridView) As ContextMenuStrip

        Dim menu As New ContextMenuStrip()

        Dim miCopiar As New ToolStripMenuItem("Copiar" & vbTab & "Ctrl+C")
        AddHandler miCopiar.Click, Sub() Copiar(dgv)

        Dim miPegar As New ToolStripMenuItem("Pegar" & vbTab & "Ctrl+V")
        AddHandler miPegar.Click, Sub() Pegar(dgv)

        Dim miRellenar As New ToolStripMenuItem("Rellenar hacia abajo" & vbTab & "Ctrl+D")
        AddHandler miRellenar.Click, Sub() RellenarHaciaAbajo(dgv)

        Dim miTodos As New ToolStripMenuItem("Aplicar este valor a toda la columna")
        AddHandler miTodos.Click, Sub() AplicarATodaLaColumna(dgv)

        menu.Items.AddRange({miCopiar, miPegar, miRellenar, New ToolStripSeparator(), miTodos})
        Return menu

    End Function

    Private Shared Sub Grid_KeyDown(sender As Object, e As KeyEventArgs)

        Dim dgv = TryCast(sender, DataGridView)
        If dgv Is Nothing OrElse Not e.Control Then Exit Sub

        Select Case e.KeyCode
            Case Keys.C
                Copiar(dgv)
                e.Handled = True
            Case Keys.V
                Pegar(dgv)
                e.Handled = True
            Case Keys.D
                RellenarHaciaAbajo(dgv)
                e.Handled = True
        End Select

    End Sub

    ' -----------------------------------------------------------------------
    ' Copiar: rango seleccionado en formato TSV
    ' -----------------------------------------------------------------------
    Public Shared Sub Copiar(dgv As DataGridView)

        Try
            If dgv Is Nothing OrElse dgv.SelectedCells.Count = 0 Then Exit Sub

            Dim filas = dgv.SelectedCells.Cast(Of DataGridViewCell)().Select(Function(c) c.RowIndex).Distinct().OrderBy(Function(i) i).ToList()
            Dim cols = dgv.SelectedCells.Cast(Of DataGridViewCell)().Select(Function(c) c.ColumnIndex).Distinct().OrderBy(Function(i) i).ToList()

            Dim sb As New Text.StringBuilder()
            For Each f In filas
                Dim partes As New List(Of String)
                For Each c In cols
                    Dim celda = dgv.Rows(f).Cells(c)
                    partes.Add(If(celda.Selected, Convert.ToString(celda.Value), ""))
                Next
                sb.AppendLine(String.Join(vbTab, partes))
            Next

            Clipboard.SetText(sb.ToString().TrimEnd(CChar(vbCr), CChar(vbLf)))

        Catch ex As Exception
            Logger.Error(ex, "GridCopiarPegar.Copiar")
        End Try

    End Sub

    ' -----------------------------------------------------------------------
    ' Pegar: TSV del portapapeles desde la celda actual
    ' -----------------------------------------------------------------------
    Public Shared Sub Pegar(dgv As DataGridView)

        Try
            If dgv Is Nothing OrElse dgv.CurrentCell Is Nothing Then Exit Sub
            If Not Clipboard.ContainsText() Then Exit Sub

            Dim texto = Clipboard.GetText()
            If String.IsNullOrWhiteSpace(texto) Then Exit Sub

            Dim lineas = texto.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf) _
                              .Split(CChar(vbLf)) _
                              .Where(Function(l) Not String.IsNullOrWhiteSpace(l)) _
                              .ToArray()

            Dim filaIni = dgv.CurrentCell.RowIndex
            Dim colIni = dgv.CurrentCell.ColumnIndex

            For i = 0 To lineas.Length - 1
                Dim fila = filaIni + i
                If fila > dgv.Rows.Count - 1 Then Exit For        ' no se crean filas nuevas

                Dim campos = lineas(i).Split(CChar(vbTab))
                For j = 0 To campos.Length - 1
                    Dim col = colIni + j
                    If col > dgv.Columns.Count - 1 Then Exit For
                    If dgv.Columns(col).ReadOnly Then Continue For   ' p. ej. la columna "Piso"
                    dgv.Rows(fila).Cells(col).Value = campos(j).Trim()
                Next
            Next

        Catch ex As Exception
            Logger.Error(ex, "GridCopiarPegar.Pegar")
        End Try

    End Sub

    ' -----------------------------------------------------------------------
    ' Rellenar hacia abajo dentro de la selección
    ' -----------------------------------------------------------------------
    Public Shared Sub RellenarHaciaAbajo(dgv As DataGridView)

        Try
            If dgv Is Nothing OrElse dgv.SelectedCells.Count < 2 Then Exit Sub

            Dim cols = dgv.SelectedCells.Cast(Of DataGridViewCell)().Select(Function(c) c.ColumnIndex).Distinct()

            For Each col In cols
                If dgv.Columns(col).ReadOnly Then Continue For

                Dim filas = dgv.SelectedCells.Cast(Of DataGridViewCell)() _
                               .Where(Function(c) c.ColumnIndex = col) _
                               .Select(Function(c) c.RowIndex) _
                               .OrderBy(Function(i) i).ToList()
                If filas.Count < 2 Then Continue For

                Dim origen = dgv.Rows(filas(0)).Cells(col).Value
                For k = 1 To filas.Count - 1
                    dgv.Rows(filas(k)).Cells(col).Value = origen
                Next
            Next

        Catch ex As Exception
            Logger.Error(ex, "GridCopiarPegar.RellenarHaciaAbajo")
        End Try

    End Sub

    ' -----------------------------------------------------------------------
    ' Aplicar el valor de la celda actual a toda su columna
    ' -----------------------------------------------------------------------
    Public Shared Sub AplicarATodaLaColumna(dgv As DataGridView)

        Try
            If dgv Is Nothing OrElse dgv.CurrentCell Is Nothing Then Exit Sub

            Dim col = dgv.CurrentCell.ColumnIndex
            If dgv.Columns(col).ReadOnly Then Exit Sub

            Dim valor = dgv.CurrentCell.Value
            For Each fila As DataGridViewRow In dgv.Rows
                If fila.IsNewRow Then Continue For
                fila.Cells(col).Value = valor
            Next

        Catch ex As Exception
            Logger.Error(ex, "GridCopiarPegar.AplicarATodaLaColumna")
        End Try

    End Sub

End Class
