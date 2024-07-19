Imports System.Data.OleDb
Imports System.IO
Imports System.Security.Cryptography.X509Certificates
Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores
Public Class Form_00_PaginaPrincipal
    Public Shared proyecto As New Proyecto

    Private Sub Opcion1_CheckedChanged(sender As Object, e As EventArgs) Handles Opcion1.CheckedChanged
        SeccionR.Show()
        SeccionR.WindowState = FormWindowState.Maximized
        Form_01_PagPilas.Hide()
    End Sub
    Private Sub MaterialesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MaterialesToolStripMenuItem.Click
        PagMateriales.Ec.Text = Math.Round(4700 * Math.Sqrt(Convert.ToDouble(PagMateriales.Fc.Text)), 3)
        PagMateriales.Show()

        'INCLUIR LOS MODELOS CONSTITUTIVOS DE LOS DIFERENTES MATERIALES QUE SE PUEDEN MANEJAR, UNA LISTA CON 21,28,35,42,49,56
        'INCLUIR MODELOS CONSTITUTIVOS DEL ACERO DE REFUERZO

    End Sub

    Private Sub RefuerzoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RefuerzoToolStripMenuItem.Click
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Clear()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows.Add()
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(0).Cells(0).Value = "No. 2"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(1).Cells(0).Value = "No. 3"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(2).Cells(0).Value = "No. 4"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(3).Cells(0).Value = "No. 5"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(4).Cells(0).Value = "No. 6"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(5).Cells(0).Value = "No. 7"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(6).Cells(0).Value = "No. 8"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(7).Cells(0).Value = "No. 10"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(0).Cells(1).Value = "1/4"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(1).Cells(1).Value = "3/8"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(2).Cells(1).Value = "1/2"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(3).Cells(1).Value = "5/8"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(4).Cells(1).Value = "3/4"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(5).Cells(1).Value = "7/8"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(6).Cells(1).Value = "1"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(7).Cells(1).Value = "1-1/4"
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(0).Cells(2).Value = 6.4
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(1).Cells(2).Value = 9.5
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(2).Cells(2).Value = 12.7
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(3).Cells(2).Value = 15.9
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(4).Cells(2).Value = 19.1
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(5).Cells(2).Value = 22.2
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(6).Cells(2).Value = 25.4
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(7).Cells(2).Value = 32.3
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(0).Cells(3).Value = 32
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(1).Cells(3).Value = 71
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(2).Cells(3).Value = 129
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(3).Cells(3).Value = 199
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(4).Cells(3).Value = 284
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(5).Cells(3).Value = 387
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(6).Cells(3).Value = 510
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(7).Cells(3).Value = 819
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(0).Cells(4).Value = 20
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(1).Cells(4).Value = 30
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(2).Cells(4).Value = 40
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(3).Cells(4).Value = 50
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(4).Cells(4).Value = 60
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(5).Cells(4).Value = 70
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(6).Cells(4).Value = 80
        Form_12_PagPropRefuerzo.PropieadesRefuerzo.Rows(7).Cells(4).Value = 101.3
        Form_12_PagPropRefuerzo.Show()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Boton_Pilas.Click

        Dim Paso As Boolean = False

        If proyecto.Nombre Is Nothing Then
            Dim resultado As DialogResult = MessageBox.Show("No se ha ingresado la información del proyecto. ¿Desea agregarla?", "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If resultado = DialogResult.Yes Then
                PagInfoGeneral.Show()
            Else
                Paso = True

            End If
        End If

        If proyecto.Nombre IsNot Nothing Or Paso = True Then

            Form_01_PagPilas.Show()
            Form_01_PagPilas.WindowState = FormWindowState.Maximized

            Form_01_PagPilas.TabControl2.TabPages(1).Text = "Cargas de Servicio"
            Form_01_PagPilas.Label11.Visible = True
            Form_01_PagPilas.Label12.Visible = True
            Form_01_PagPilas.Dc.Visible = True
            Form_01_PagPilas.GroupBox4.Size = New Size(285, 60)
            Form_01_PagPilas.Op_Seccion.Visible = True
            Form_01_PagPilas.GroupBox1.Height = 110

            Form_01_PagPilas.GroupBox1.Location = New Point(10, Form_01_PagPilas.GroupBox4.Top + Form_01_PagPilas.GroupBox4.Height + 10)
            Form_01_PagPilas.GroupBox2.Location = New Point(10, Form_01_PagPilas.GroupBox1.Top + Form_01_PagPilas.GroupBox1.Height + 10)
            Form_01_PagPilas.GroupBox6.Location = New Point(10, Form_01_PagPilas.GroupBox2.Top + Form_01_PagPilas.GroupBox2.Height + 10)
            Form_01_PagPilas.GroupBox6.Visible = True
            Form_01_PagPilas.OpcionPila.Checked = True

            Form_01_PagPilas.Op_Seccion.SelectedIndex = 0
            Form_01_PagPilas.RefuerzoLong.SelectedIndex = 3
            Form_01_PagPilas.RefuerzoTransv.SelectedIndex = 2
        End If

    End Sub

    Private Sub PictureBox6_Click(sender As Object, e As EventArgs) Handles Imag_Pilas.Click
        Form_01_PagPilas.Show()
        Form_01_PagPilas.WindowState = FormWindowState.Maximized

        Form_01_PagPilas.TabControl2.TabPages(1).Text = "Cargas de Servicio"
        Form_01_PagPilas.Label11.Visible = True
        Form_01_PagPilas.Label12.Visible = True
        Form_01_PagPilas.Dc.Visible = True
        Form_01_PagPilas.GroupBox4.Size = New Size(285, 60)
        Form_01_PagPilas.Op_Seccion.Visible = True
        Form_01_PagPilas.GroupBox1.Height = 110

        Form_01_PagPilas.GroupBox1.Location = New Point(10, Form_01_PagPilas.GroupBox4.Top + Form_01_PagPilas.GroupBox4.Height + 10)
        Form_01_PagPilas.GroupBox2.Location = New Point(10, Form_01_PagPilas.GroupBox1.Top + Form_01_PagPilas.GroupBox1.Height + 10)
        Form_01_PagPilas.GroupBox6.Location = New Point(10, Form_01_PagPilas.GroupBox2.Top + Form_01_PagPilas.GroupBox2.Height + 10)
        Form_01_PagPilas.GroupBox6.Visible = True
        Form_01_PagPilas.OpcionPila.Checked = True

        Form_01_PagPilas.Op_Seccion.SelectedIndex = 0
        Form_01_PagPilas.RefuerzoLong.SelectedIndex = 3
        Form_01_PagPilas.RefuerzoTransv.SelectedIndex = 2
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Boton_Columnas.Click
        Dim Paso As Boolean = False

        If proyecto.Nombre Is Nothing Then
            Dim resultado As DialogResult = MessageBox.Show("No se ha ingresado la información del proyecto. ¿Desea agregarla?", "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If resultado = DialogResult.Yes Then
                PagInfoGeneral.Show()
            Else
                Paso = True

            End If
        End If

        If proyecto.Nombre IsNot Nothing Or Paso = True Then
            Form_02_PagColumnas.Show()
        End If
    End Sub

    Private Sub PictureBox4_Click(sender As Object, e As EventArgs) Handles Imag_Col.Click
        Form_02_PagColumnas.Show()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Boton_Propiedades.Click
        Form1.Show()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Boton_Losas.Click
        Form_03_Losas.Show()
    End Sub

    Private Sub PictureBox5_Click_1(sender As Object, e As EventArgs) Handles Imag_Los.Click
        Form_03_Losas.Show()
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Boton_Escaleras.Click
        Form_04_Escaleras.Show()
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Boton_MNE.Click
        Form_05_MurosNoEstructurales.Show()
    End Sub

    Private Sub PictureBox7_Click(sender As Object, e As EventArgs) Handles Imag_Escaleras.Click
        Form_04_Escaleras.Show()
    End Sub

    Private Sub PictureBox8_Click(sender As Object, e As EventArgs) Handles Imag_MNE.Click
        Form_05_MurosNoEstructurales.Show()
        Form_05_MurosNoEstructurales.WindowState = FormWindowState.Maximized
    End Sub

    Private Sub Button7_Click_1(sender As Object, e As EventArgs) Handles Boton_Muros.Click

        Dim Paso As Boolean = False

        If proyecto.Nombre Is Nothing Then
            Dim resultado As DialogResult = MessageBox.Show("No se ha ingresado la información del proyecto. ¿Desea agregarla?", "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If resultado = DialogResult.Yes Then
                PagInfoGeneral.Show()
            Else
                Paso = True

            End If
        End If

        If proyecto.Nombre IsNot Nothing Or Paso = True Then
            Form_06_PagMuros.Show()
            Form_06_PagMuros.WindowState = FormWindowState.Maximized
        End If

    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click
        Try
            Dim Carpeta As String = My.Computer.FileSystem.CurrentDirectory
            Dim Software As String = Carpeta + "\Manual de Ayuda - ARCO-V.0.1.pdf"
            Process.Start(Software)
        Catch ex As Exception
            MsgBox("No se pudo encontrar el archivo")
        End Try
    End Sub

    Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click
        Try
            Dim Carpeta As String = My.Computer.FileSystem.CurrentDirectory
            Dim Software As String = Carpeta + "\ReadMe.pdf"
            Process.Start(Software)
        Catch ex As Exception
            MsgBox("No se pudo encontrar el archivo")
        End Try
    End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem3.Click
        Licencia.Show()
    End Sub

    Private Sub Form_00_PaginaPrincipal_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub AbrirProyectoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AbrirProyectoToolStripMenuItem.Click
        Open()
    End Sub
    Public Sub Open()
        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(Open.FileName)
        End If

    End Sub

    Private Sub InformaciónDeProyectoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles InformaciónDeProyectoToolStripMenuItem.Click
        If proyecto.Year = 0 Then
            PagInfoGeneral.C_YearBuilding.Text = "2024"
        Else
            PagInfoGeneral.C_YearBuilding.Text = proyecto.Year
        End If

        If IsNothing(proyecto.Disipacion) Then
            PagInfoGeneral.C_DM.Text = "DMO"
            PagInfoGeneral.C_TS.Text = "D"
            PagInfoGeneral.C_GU.Text = "I"

        Else
            PagInfoGeneral.C_DM.Text = proyecto.Disipacion
            PagInfoGeneral.T_NameProjet.Text = proyecto.Nombre
            PagInfoGeneral.T_Direction.Text = proyecto.Direccion
            PagInfoGeneral.T_Department.Text = proyecto.Departamento
            PagInfoGeneral.T_City.Text = proyecto.Ciudad
            PagInfoGeneral.T_Propietario.Text = proyecto.Propietario
            PagInfoGeneral.T_Disenador.Text = proyecto.Disenador
            PagInfoGeneral.C_SE.Text = proyecto.SistemaEstructural

            PagInfoGeneral.C_SE.Text = DirectorioSistemaEstructural.dSistemaEstructural(proyecto.SistemaEstructural).ToString()

            Dim responsableEnum As eResponsables = proyecto.Persona_Responsable

            Dim nombreSeleccionado As String = DirectorioResponsables.dResponsables(responsableEnum).NombreCompleto

            If PagInfoGeneral.C_Responsable.Items.Contains(nombreSeleccionado) Then
                PagInfoGeneral.C_Responsable.SelectedItem = nombreSeleccionado
            Else
                PagInfoGeneral.C_Responsable.Items.Add(nombreSeleccionado)
                PagInfoGeneral.C_Responsable.SelectedItem = nombreSeleccionado
            End If

            PagInfoGeneral.C_GU.Text = proyecto.GrupoU
            PagInfoGeneral.C_TS.Text = proyecto.T_Suelo

        End If

        PagInfoGeneral.Show()

    End Sub

    Private Sub ModeloElasticoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ModeloElasticoToolStripMenuItem.Click

        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False

            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Try
                    Dim path As String = .FileName

                    Me.Cursor = Cursors.WaitCursor

                    Dim Ds As New DataSet
                    Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
                    Dim cnConex As New OleDbConnection(stConexion)
                    cnConex.Open()

                    Dim Cmd As New OleDbCommand("Select * From [Story Drifts$]")
                    Cmd.Connection = cnConex

                    Dim Da As New OleDbDataAdapter(Cmd)
                    Dim Dt As New DataTable

                    Da.Fill(Ds)
                    Dt = Ds.Tables(0)

                    For Each row As DataRow In Dt.Rows
                        If Not IsDBNull(row(0)) AndAlso row(0).ToString() <> "Story" Then
                            Dim comb As String = row(1).ToString()
                            comb = comb.Substring(0, comb.Length - 4)
                            If comb.Contains("X") And proyecto.Combinacion_SismoX = Nothing Then
                                proyecto.Combinacion_SismoX = comb
                            ElseIf comb.Contains("Y") And proyecto.Combinacion_SismoY = Nothing Then
                                proyecto.Combinacion_SismoY = comb
                            End If

                            Dim piso_ As New cPiso
                            piso_.Nombre = row(0).ToString()
                            piso_.CoorZ = Convert.ToSingle(row(7))

                            Dim Deriva_ As New cDeriva
                            Deriva_.Piso = piso_
                            Deriva_.Deriva = Convert.ToSingle(row(3))

                            If comb.Contains("X") And row(2) = "X" Then
                                If Not proyecto.Deriva_X.Any(Function(p) p.Piso.Nombre = piso_.Nombre) Then
                                    proyecto.Deriva_X.Add(Deriva_)
                                End If
                            ElseIf comb.Contains("Y") And row(2) = "Y" Then
                                If Not proyecto.Deriva_Y.Any(Function(p) p.Piso.Nombre = piso_.Nombre) Then
                                    proyecto.Deriva_Y.Add(Deriva_)
                                End If
                            End If
                        End If
                    Next

                    cnConex.Close()
                Catch ex As Exception
                    MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
                Finally
                    Dim resultado As DialogResult = MessageBox.Show("Datos de deriva de modelo elástico cargados con éxito. ¿Desea visualizarlos?", "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

                    If resultado = DialogResult.Yes Then
                        Form_Derivas.Tabla_Derivas.Rows.Clear()
                        Form_Derivas.Tabla_Derivas.Columns.Clear()

                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_01", "Piso")
                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_02", "Deriva X (%)")
                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_03", "Deriva Y (%)")
                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_04", "Altura (m)")

                        Dim valores_X_Der_X As New List(Of Single)
                        Dim valores_X_Der_Y As New List(Of Single)
                        Dim valores_Y As New List(Of Single)

                        For i = 0 To proyecto.Deriva_X.Count - 1
                            Form_Derivas.Tabla_Derivas.Rows.Add(proyecto.Deriva_X(i).Piso.Nombre,
                                                                Math.Round(proyecto.Deriva_X(i).Deriva * 100, 2),
                                                                Math.Round(proyecto.Deriva_Y(i).Deriva * 100, 2),
                                                                Math.Round(proyecto.Deriva_X(i).Piso.CoorZ, 2))

                            valores_X_Der_X.Add(proyecto.Deriva_X(i).Deriva * 100)
                            valores_X_Der_Y.Add(proyecto.Deriva_Y(i).Deriva * 100)
                            valores_Y.Add(proyecto.Deriva_X(i).Piso.CoorZ)
                        Next
                        Form_Derivas.Tabla_Derivas.RowHeadersVisible = False

                        valores_X_Der_X.Add(0)
                        valores_X_Der_Y.Add(0)
                        valores_Y.Add(0)

                        Form_Derivas.Grafico_Derivas.Series.Clear()
                        Form_Derivas.Grafico_Derivas.Series.Add("Deriva X")
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").Points.DataBindXY(valores_X_Der_X, valores_Y)
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").ChartType = SeriesChartType.Spline
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").Color = Color.Green
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").BorderWidth = 2
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").MarkerStyle = MarkerStyle.Circle
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").MarkerSize = 8

                        Form_Derivas.Grafico_Derivas.Series.Add("Deriva Y")
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").Points.DataBindXY(valores_X_Der_Y, valores_Y)
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").ChartType = SeriesChartType.Spline
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").Color = Color.OrangeRed
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").BorderWidth = 2
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").MarkerStyle = MarkerStyle.Diamond
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").MarkerSize = 8

                        Form_Derivas.Grafico_Derivas.Update()

                        Form_Derivas.Show()

                    End If

                    Me.Cursor = Cursors.Arrow

                End Try


            End If
        End With

    End Sub

    Private Sub ModeloAgrietadoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ModeloAgrietadoToolStripMenuItem.Click
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False

            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Try
                    Dim path As String = .FileName

                    Me.Cursor = Cursors.WaitCursor

                    Dim Ds As New DataSet
                    Dim stConexion As String = ("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & (path & ";Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;';"))
                    Dim cnConex As New OleDbConnection(stConexion)
                    cnConex.Open()

                    Dim Cmd As New OleDbCommand("Select * From [Story Drifts$]")
                    Cmd.Connection = cnConex

                    Dim Da As New OleDbDataAdapter(Cmd)
                    Dim Dt As New DataTable

                    Da.Fill(Ds)
                    Dt = Ds.Tables(0)

                    For Each row As DataRow In Dt.Rows
                        If Not IsDBNull(row(0)) AndAlso row(0).ToString() <> "Story" Then
                            Dim comb As String = row(1).ToString()
                            comb = comb.Substring(0, comb.Length - 4)
                            If comb.Contains("X") And proyecto.Combinacion_SismoX = Nothing Then
                                proyecto.Combinacion_SismoX = comb
                            ElseIf comb.Contains("Y") And proyecto.Combinacion_SismoY = Nothing Then
                                proyecto.Combinacion_SismoY = comb
                            End If

                            Dim piso_ As New cPiso
                            piso_.Nombre = row(0).ToString()
                            piso_.CoorZ = Convert.ToSingle(row(7))

                            Dim Deriva_ As New cDeriva
                            Deriva_.Piso = piso_
                            Deriva_.Deriva = Convert.ToSingle(row(3))

                            If comb.Contains("X") And row(2) = "X" Then
                                If Not proyecto.DerivaCr_X.Any(Function(p) p.Piso.Nombre = piso_.Nombre) Then
                                    proyecto.DerivaCr_X.Add(Deriva_)
                                End If
                            ElseIf comb.Contains("Y") And row(2) = "Y" Then
                                If Not proyecto.DerivaCr_Y.Any(Function(p) p.Piso.Nombre = piso_.Nombre) Then
                                    proyecto.DerivaCr_Y.Add(Deriva_)
                                End If
                            End If
                        End If
                    Next

                    cnConex.Close()
                Catch ex As Exception
                    MsgBox(ex.Message, MsgBoxStyle.Critical, "Error")
                Finally

                    Dim resultado As DialogResult = MessageBox.Show("Datos de deriva de modelo fisurado cargados con éxito. ¿Desea visualizarlos?", "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

                    If resultado = DialogResult.Yes Then

                        Form_Derivas.Tabla_Derivas.Rows.Clear()
                        Form_Derivas.Tabla_Derivas.Columns.Clear()

                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_01", "Piso")
                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_02", "Deriva X (%)")
                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_03", "Deriva Y (%)")
                        Form_Derivas.Tabla_Derivas.Columns.Add("Col_04", "Altura (m)")

                        Dim valores_X_Der_X As New List(Of Single)
                        Dim valores_X_Der_Y As New List(Of Single)
                        Dim valores_Y As New List(Of Single)

                        For i = 0 To proyecto.DerivaCr_X.Count - 1
                            Form_Derivas.Tabla_Derivas.Rows.Add(proyecto.DerivaCr_X(i).Piso.Nombre,
                                                                Math.Round(proyecto.DerivaCr_X(i).Deriva * 100, 2),
                                                                Math.Round(proyecto.DerivaCr_Y(i).Deriva * 100, 2),
                                                                Math.Round(proyecto.DerivaCr_X(i).Piso.CoorZ, 2))

                            valores_X_Der_X.Add(proyecto.DerivaCr_X(i).Deriva * 100)
                            valores_X_Der_Y.Add(proyecto.DerivaCr_Y(i).Deriva * 100)
                            valores_Y.Add(proyecto.DerivaCr_X(i).Piso.CoorZ)
                        Next
                        Form_Derivas.Tabla_Derivas.RowHeadersVisible = False

                        valores_X_Der_X.Add(0)
                        valores_X_Der_Y.Add(0)
                        valores_Y.Add(0)

                        Form_Derivas.Grafico_Derivas.Series.Clear()
                        Form_Derivas.Grafico_Derivas.Series.Add("Deriva X")
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").Points.DataBindXY(valores_X_Der_X, valores_Y)
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").ChartType = SeriesChartType.Spline
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").Color = Color.Green
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").BorderWidth = 2
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").MarkerStyle = MarkerStyle.Circle
                        Form_Derivas.Grafico_Derivas.Series("Deriva X").MarkerSize = 8

                        Form_Derivas.Grafico_Derivas.Series.Add("Deriva Y")
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").Points.DataBindXY(valores_X_Der_Y, valores_Y)
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").ChartType = SeriesChartType.Spline
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").Color = Color.OrangeRed
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").BorderWidth = 2
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").MarkerStyle = MarkerStyle.Diamond
                        Form_Derivas.Grafico_Derivas.Series("Deriva Y").MarkerSize = 8

                        Form_Derivas.Grafico_Derivas.Update()

                        Form_Derivas.Show()

                    End If

                    Me.Cursor = Cursors.Arrow

                End Try


            End If
        End With
    End Sub

    Private Sub GuardarComoToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles GuardarComoToolStripMenuItem1.Click
        SaveAs(proyecto)
    End Sub

    Private Sub SaveAs(ByVal Objeto As Object)
        Try
            Dim SaveAs As New SaveFileDialog
            SaveAs.Filter = "Archivo|*.esm"
            SaveAs.Title = "Guardar Archivo"
            SaveAs.FileName = Convert.ToString("RevisiónMuros_Proyecto - " & proyecto.Nombre)
            SaveAs.ShowDialog()
            If SaveAs.FileName <> String.Empty Then
                proyecto.Ruta = Path.GetFullPath(SaveAs.FileName)
                Funciones_Programa.Serializar(SaveAs.FileName, Objeto)
            End If
        Catch ex As Exception

        End Try

    End Sub

    Private Sub GuardarToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles GuardarToolStripMenuItem1.Click
        Try
            If proyecto.Ruta = String.Empty Then
                SaveAs(proyecto)
            Else
                Funciones_Programa.Serializar(proyecto.Ruta, proyecto)
            End If
        Catch ex As Exception

        End Try
    End Sub
End Class
