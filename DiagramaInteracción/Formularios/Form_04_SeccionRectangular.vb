Imports System.Data.OleDb
Public Class SeccionR
    Public PictureBox5 As New PictureBox()
    Class Eleccion
        Public Tipo As String
    End Class
    Public Opcion As New List(Of Eleccion)
    Public Function ImportExcellToDataGridView_CCortante(ByRef path As String, ByVal Datagrid As DataGridView)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            Dim Cmd As New OleDbCommand("Select * From [CargasCortante$]")
            cnConex.Open()
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function
    Public Function ImportExcellToDataGridView_CUltimas(ByRef path As String, ByVal Datagrid As DataGridView)
        Try
            Me.Cursor = Cursors.WaitCursor
            Dim Ds As New DataSet
            Dim Da As New OleDbDataAdapter
            Dim Dt As New DataTable
            Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
            Dim cnConex As New OleDbConnection(stConexion)
            Dim Cmd As New OleDbCommand("Select * From [CargasUltimas$]")
            cnConex.Open()
            Cmd.Connection = cnConex
            Da.SelectCommand = Cmd
            Da.Fill(Ds)
            Dt = Ds.Tables(0)
            Datagrid.Columns.Clear()
            Datagrid.DataSource = Dt
            cnConex.Close()
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
        Finally
            Me.Cursor = Cursors.Arrow
        End Try
        Return True
    End Function
    Private Sub TipoFrameToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TipoFrameToolStripMenuItem.Click
        Dim Desicion As New Eleccion
        Desicion.Tipo = "Frame"
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            .InitialDirectory = My.Computer.FileSystem.SpecialDirectories.Desktop
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                ImportExcellToDataGridView_CCortante(.FileName, TablaCCortante)
                ImportExcellToDataGridView_CUltimas(.FileName, TablaCUltimas)
            End If
        End With
        Opcion.Add(Desicion)
    End Sub
    Private Sub TipoPierToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TipoPierToolStripMenuItem.Click
        Dim Desicion As New Eleccion
        Desicion.Tipo = "Pier"
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            .InitialDirectory = My.Computer.FileSystem.SpecialDirectories.Desktop
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                ImportExcellToDataGridView_CCortante(.FileName, TablaCCortante)
                ImportExcellToDataGridView_CUltimas(.FileName, TablaCUltimas)
            End If
        End With
        Opcion.Add(Desicion)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

    End Sub
    'Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
    '    PictureBox5.Location = New Point(25, 70)
    '    PictureBox5.Size = New Size(SRectangular.Panel1.Width - 50, SRectangular.Panel1.Height - 100)
    '    PictureBox5.BackColor = Color.White
    '    PictureBox5.Anchor = AnchorStyles.Left And AnchorStyles.Top And AnchorStyles.Right And AnchorStyles.Bottom
    '    SRectangular.Panel1.Controls.Add(PictureBox5)
    '    AddHandler PictureBox5.Paint, AddressOf SRectangular.PictureBox5_Paint
    '    PictureBox5.Refresh()
    '    SRectangular.Show()
    'End Sub
End Class