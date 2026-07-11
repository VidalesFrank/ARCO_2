Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_Muros
Imports ARCO.SeccionMuro

Public Class Form_06_00_PagInfoMuros
    Public Shared proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Dim nuevaFuente As System.Drawing.Font = New System.Drawing.Font("Microsoft Sans Serif", 10, FontStyle.Bold)

    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Try
            Tabla_Info_Seccion.Rows.Clear()
            Tabla_Info_EBorde.Rows.Clear()

            Dim Elemento As String = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Name
            Dim Seccion = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Lista_Secciones

            Dim SeccionesPatron As List(Of SeccionMuro)

            If IsNothing(Seccion.FindAll(Function(p) p.S_Patron = True)) Then
            Else
                SeccionesPatron = Seccion.FindAll(Function(p) p.S_Patron = True)

                For j = 0 To SeccionesPatron.Count - 1
                    C_Similar_1.Items.Add(SeccionesPatron(j).Piso)
                    C_Similar.Items.Add(SeccionesPatron(j).Piso)
                Next
            End If

            For i = 0 To (Seccion.Count * 2 - 1)
                Tabla_Info_Seccion.Rows.Add()
                Tabla_Info_EBorde.Rows.Add()
            Next

            Tabla_Info_Seccion.Refresh()
            Tabla_Info_EBorde.Refresh()

            For i = 0 To ((Seccion.Count * 2) - 1) Step 2

                Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

                If Seccion(i / 2).S_Patron = False Then
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "None"
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "Si"
                End If

                Tabla_Info_EBorde.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_EBorde.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_EBorde.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_EBorde.Rows(i + 1).Cells(4).Value = "Bottom"

                If proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Ref_Modificado_Muros = False Then
                    For j = 7 To 14

                        Tabla_Info_Seccion.Rows(i).Cells(j).Value = 0
                        Tabla_Info_Seccion.Rows(i + 1).Cells(j).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 1).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 1).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 15).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 15).Value = 0

                    Next

                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = ""
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = 0

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = "0"
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = "0"

                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(6).Value = ""
                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = 0

                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(18).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(20).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = 0

                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(20).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(34).Value = "None"

                Else

                    Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_2
                    Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_3
                    Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_4
                    Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_5
                    Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_6
                    Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_7
                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_8
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_10

                    Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_2
                    Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_3
                    Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_4
                    Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_5
                    Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_6
                    Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_7
                    Tabla_Info_Seccion.Rows(i + 1).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_8
                    Tabla_Info_Seccion.Rows(i + 1).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_10

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Malla.Malla
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).Malla.Capas)
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = Seccion(i / 2).RefH_W_Col.Acero
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = Seccion(i / 2).RefH_W_Col.Separacion
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = Convert.ToString(Seccion(i / 2).RefH_W_Col.Capas)

                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = Seccion(i / 2).EB_I_Top.L_EB
                    Tabla_Info_EBorde.Rows(i).Cells(8).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(9).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(10).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(11).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(12).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(13).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(14).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(15).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(8).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(9).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(10).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(11).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(12).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(13).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(14).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(15).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).EB_I_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = Seccion(i / 2).EB_I_Top.RefH.Acero
                    If Seccion(i / 2).EB_I_Top.RefH.Acero = "User" Then
                        Tabla_Info_EBorde.Rows(i).Cells(18).Value = Seccion(i / 2).EB_I_Top.RefH.Acero_T / Seccion(i / 2).EB_I_Top.RefH.Capas
                    End If

                    Tabla_Info_EBorde.Rows(i).Cells(19).Value = Seccion(i / 2).EB_I_Top.RefH.Separacion

                    Tabla_Info_EBorde.Rows(i).Cells(21).Value = Seccion(i / 2).EB_D_Top.L_EB

                    Tabla_Info_EBorde.Rows(i).Cells(22).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(23).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(24).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(25).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(26).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(27).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(28).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(22).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(23).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(24).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(25).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(26).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(27).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(28).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(29).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = Convert.ToString(Seccion(i / 2).EB_D_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = Seccion(i / 2).EB_D_Top.RefH.Acero
                    If Seccion(i / 2).EB_D_Top.RefH.Acero = "User" Then
                        Tabla_Info_EBorde.Rows(i).Cells(32).Value = Seccion(i / 2).EB_D_Top.RefH.Acero_T / Seccion(i / 2).EB_D_Top.RefH.Capas
                    End If
                    Tabla_Info_EBorde.Rows(i).Cells(33).Value = Seccion(i / 2).EB_D_Top.RefH.Separacion

                    ' Mostrar "Similar A" guardada
                    Dim simVal As String = If(String.IsNullOrEmpty(Seccion(i / 2).S_Similar), "", Seccion(i / 2).S_Similar)
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = simVal
                    Tabla_Info_EBorde.Rows(i).Cells(6).Value = simVal

                End If
            Next

        Catch ex As Exception
            Logger.Error(ex, "Form_06_00_PagInfoMuros.Combo_Elementos_SelectedIndexChanged",
                         "Error al cargar secciones del muro: " & Combo_Elementos.Text)
        Finally
            T_Seccion.Text = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Label
        End Try

    End Sub

    Private Sub Tabla_Info_Seccion_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_Seccion.CellValueChanged
        For i = 0 To Tabla_Info_Seccion.Rows.Count - 1
            For j = 7 To 14
                If Tabla_Info_Seccion.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_Seccion, i, j)
                End If
            Next
        Next
    End Sub
    Private Sub Tabla_Info_Eb_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Info_EBorde.CellValueChanged
        For i = 0 To Tabla_Info_EBorde.Rows.Count - 1
            For j = 8 To 15
                If Tabla_Info_EBorde.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_EBorde, i, j)
                End If
            Next
            For j = 21 To 28
                If Tabla_Info_EBorde.Rows(i).Cells(j).Value <> 0 Then
                    Color_Celda(Tabla_Info_EBorde, i, j)
                End If
            Next
        Next
    End Sub

    Sub Color_Celda(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer)
        Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 224, 180)
        Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.Red
    End Sub


    Private Sub Form_06_00_PagInfoMuros_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Dim Tabla = Tabla_Info_Seccion
        For i = 0 To Tabla.ColumnCount - 1
            If i < 5 Then
                If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                    Tabla.Columns(i).ReadOnly = True
                    Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                    Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
                End If
            End If
        Next


        Tabla = Tabla_Info_EBorde
        For i = 0 To Tabla.ColumnCount - 1
            If i < 5 Then
                If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                    Tabla.Columns(i).ReadOnly = True
                    Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                    Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
                End If
            End If
        Next

        If proyecto.Elementos.Muros.Lista_Muros.Count > 0 Then
            For i = 0 To proyecto.Elementos.Muros.Lista_Muros.Count - 1
                Combo_Elementos.Items.Add(proyecto.Elementos.Muros.Lista_Muros(i).Name)
            Next
            Combo_Elementos.Text = proyecto.Elementos.Muros.Lista_Muros(0).Name
        End If

        ' ── Menú: Diagrama de Interacción ────────────────────────────────────
        Dim menuDI As New ToolStripMenuItem("Diagrama de Interacción P-M")
        menuDI.ForeColor = Color.White
        menuDI.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuDI.Click, AddressOf AbrirDiagramaInteraccion
        OpcionesToolStripMenuItem.DropDownItems.Add(menuDI)

        ' ── Menú: Sección Transversal ─────────────────────────────────────────
        Dim menuSec As New ToolStripMenuItem("Ver Sección Transversal")
        menuSec.ForeColor = Color.White
        menuSec.BackColor = Color.FromArgb(87, 87, 87)
        AddHandler menuSec.Click, AddressOf AbrirSeccionTransversal
        OpcionesToolStripMenuItem.DropDownItems.Add(menuSec)

        ' ── Menú contextual: llenar columna ──────────────────────────────────
        ConfigurarMenuLlenarColumna()

    End Sub

    Private Sub AbrirDiagramaInteraccion(sender As Object, e As EventArgs)
        Dim muro = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text)
        If muro Is Nothing OrElse muro.Lista_Secciones.Count = 0 Then
            MessageBox.Show("Seleccione un muro con secciones calculadas.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim sec = muro.Lista_Secciones(0)
        If sec.Lista_Mn_Top Is Nothing OrElse sec.Lista_Mn_Top.Count = 0 Then
            MessageBox.Show("Primero aplique el refuerzo (Botón 'Guardar') para calcular el diagrama de interacción.",
                            "Sin diagrama", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim form As New Form_DiagramaInteraccionMuro() With {
            .Seccion = sec,
            .NombreMuro = muro.Label,
            .NombrePiso = sec.Piso,
            .ListaCombinacionesDesign = proyecto.Elementos.Muros.ListA_Combinaciones_Design,
            .Combinaciones = sec.Lista_Combinaciones
        }
        form.Show(Me)
    End Sub

    Private Sub AbrirSeccionTransversal(sender As Object, e As EventArgs)
        Dim muro = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text)
        If muro Is Nothing OrElse muro.Lista_Secciones.Count = 0 Then
            MessageBox.Show("Seleccione un muro con secciones.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim sec = muro.Lista_Secciones(0)
        Dim form As New Form_SeccionMuroViewer() With {
            .Seccion = sec,
            .NombreMuro = muro.Label,
            .NombrePiso = sec.Piso
        }
        form.Show(Me)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        'Try

        Dim Seccion = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Lista_Secciones
        Dim Elemento As String = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Name

        proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Label = T_Seccion.Text

        ' Pre-lectura: guardar S_Patron y S_Similar para todas las secciones
        For k = 0 To (Seccion.Count - 1) * 2 Step 2
            Seccion(k / 2).S_Patron = (If(Tabla_Info_Seccion.Rows(k).Cells(5).Value?.ToString(), "") = "Si")
            Seccion(k / 2).S_Similar = If(Tabla_Info_Seccion.Rows(k).Cells(6).Value?.ToString(), "").Trim()
        Next

        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Seccion(i / 2).fc = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(1).Value)
            Seccion(i / 2).tw_Planos = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(2).Value)
            Seccion(i / 2).Lw_Planos = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(3).Value)
            Seccion(i / 2).AgM = Seccion(i / 2).Lw_Planos * Seccion(i / 2).tw_Planos

            ' Si es "Similar a" otra sección, saltarla — se procesa en la segunda pasada
            If Not String.IsNullOrEmpty(Seccion(i / 2).S_Similar) Then Continue For

            '============== REFUERZO LONGITUDINAL =====================

            Dim AsT_Top As Single = 0
            Dim AsT_Bot As Single = 0

            Dim Count_Barras_Top As Integer = 0
            Dim Count_Barras_Bot As Integer = 0

            Dim currentIndex As Integer = i

            ' Usar un array para las propiedades de refuerzo de muro
            Dim topBarras() As Action(Of Integer) = {
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_2 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_3 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_4 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_5 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_6 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_7 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_8 = c,
                Nothing, ' Para #9, no se usa
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Top.Barras_10 = c
            }

            Dim bottomBarras() As Action(Of Integer) = {
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_2 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_3 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_4 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_5 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_6 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_7 = c,
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_8 = c,
                Nothing, ' Para #9, no se usa
                Sub(c) Seccion(currentIndex / 2).Refuerzo_Muro_Bottom.Barras_10 = c
            }

            Dim Count_Barras As Integer = 1


            If Seccion(i / 2).Refuerzo_Muro_Top_Pr.Barras.Count > 0 Then

                Seccion(i / 2).Refuerzo_Muro_Top_Pr.Barras.Clear()

            End If

            If Seccion(i / 2).Refuerzo_Muro_Bot_Pr.Barras.Count > 0 Then

                Seccion(i / 2).Refuerzo_Muro_Bot_Pr.Barras.Clear()

            End If

            For j As Integer = 2 To 10 ' Asumiendo que las barras están desde #2 hasta #10
                If j <> 9 Then
                    Dim columnIndex As Integer = 7 + (j - 2)
                    If j = 10 Then
                        columnIndex = 7 + (j - 3)
                    End If

                    Dim CantidadBarra_Top As Integer = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(columnIndex).Value)
                    Dim CantidadBarra_Bot As Integer = Convert.ToInt16(Tabla_Info_Seccion.Rows(i + 1).Cells(columnIndex).Value)

                    ' Asignar la cantidad a la propiedad correspondiente
                    topBarras(j - 2)?.Invoke(CantidadBarra_Top)
                    bottomBarras(j - 2)?.Invoke(CantidadBarra_Bot)

                    Dim barra As String = "#" & j.ToString()
                    Dim As_Barra As Single = BarraData.BarraAreas(barra)
                    Dim Db_Barra As Single = BarraData.BarraDb(barra)

                    If BarraData.BarraAreas.ContainsKey(barra) Then
                        AsT_Top += As_Barra * CantidadBarra_Top
                        AsT_Bot += As_Barra * CantidadBarra_Bot
                    End If

                    Count_Barras_Top += CantidadBarra_Top
                    Count_Barras_Bot += CantidadBarra_Bot

                    If CantidadBarra_Top > 0 Then
                        Dim barraInfoTop As New BarraInfo With {
                            .Id = Count_Barras,
                            .Id_Barra = j,
                            .String_Barra = barra,
                            .Count_Barras = CantidadBarra_Top,
                            .Ast = As_Barra * CantidadBarra_Top,
                            .Db = Db_Barra
                        }

                        Seccion(i / 2).Refuerzo_Muro_Top_Pr.Barras.Add(barraInfoTop)
                        Count_Barras += 1

                    End If

                    If CantidadBarra_Bot > 0 Then
                        Dim barraInfoBottom As New BarraInfo With {
                            .Id = Count_Barras,
                            .Id_Barra = j,
                            .String_Barra = barra,
                            .Count_Barras = CantidadBarra_Bot,
                            .Ast = As_Barra * CantidadBarra_Bot,
                            .Db = Db_Barra
                        }

                        Seccion(i / 2).Refuerzo_Muro_Bot_Pr.Barras.Add(barraInfoBottom)
                        Count_Barras += 1

                    End If

                End If
            Next

            Seccion(i / 2).Cantidad_Barras_Col_Bot = Count_Barras_Bot
            Seccion(i / 2).Cantidad_Barras_Col_Top = Count_Barras_Top

            Dim mallaString As String = Tabla_Info_Seccion.Rows(i).Cells(15).Value.ToString()
            Dim mallaTipo As MallaTipo = StringToMallaTipo(mallaString)

            Seccion(i / 2).AsT_Top_Col = AsT_Top + (AceroMallas(mallaTipo) * Seccion(i / 2).Malla.Capas * Seccion(i / 2).Lw_Planos)
            Seccion(i / 2).AsT_Bot_Col = AsT_Bot + (AceroMallas(mallaTipo) * Seccion(i / 2).Malla.Capas * Seccion(i / 2).Lw_Planos)
            Seccion(i / 2).Cuantia_Top_Col = (Seccion(i / 2).AsT_Top_Col / (Seccion(i / 2).AgM * 1000000)) * 100
            Seccion(i / 2).Cuantia_Bot_Col = (Seccion(i / 2).AsT_Bot_Col / (Seccion(i / 2).AgM * 1000000)) * 100

            'Seccion(i / 2).Cuantia_Top_Col = (AsT_Top / (Seccion(i / 2).AgM * 1000000) + CalculoCuantia(Seccion(i / 2).tw_Planos, Seccion(i / 2).Malla.Malla, Seccion(i / 2).Malla.Capas, 1, Seccion(i / 2).RefH_W_Col.Acero, 1, 0)) * 100
            'Seccion(i / 2).Cuantia_Bot_Col = (AsT_Bot / (Seccion(i / 2).AgM * 1000000) + CalculoCuantia(Seccion(i / 2).tw_Planos, Seccion(i / 2).Malla.Malla, Seccion(i / 2).Malla.Capas, 1, Seccion(i / 2).RefH_W_Col.Acero, 1, 0)) * 100


            Seccion(i / 2).Malla.Tipo_Malla = mallaTipo
            Seccion(i / 2).Malla.Malla = mallaString
            Seccion(i / 2).Malla.Capas = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(16).Value)
            Seccion(i / 2).Malla.Acero_T = AceroMallas(mallaTipo) * Seccion(i / 2).Malla.Capas

            Seccion(i / 2).RefH_W_Col.Acero = Tabla_Info_Seccion.Rows(i).Cells(17).Value
            Seccion(i / 2).RefH_W_Col.Separacion = Convert.ToSingle(Tabla_Info_Seccion.Rows(i).Cells(18).Value)
            Seccion(i / 2).RefH_W_Col.Capas = Convert.ToInt16(Tabla_Info_Seccion.Rows(i).Cells(19).Value)
            If Seccion(i / 2).RefH_W_Col.Separacion > 0 Then
                Seccion(i / 2).RefH_W_Col.Acero_T = (1 / Seccion(i / 2).RefH_W_Col.Separacion + 1) * AreaRefuerzo(Seccion(i / 2).RefH_W_Col.Acero) * Seccion(i / 2).RefH_W_Col.Capas
            End If
            Seccion(i / 2).Rho_H_col = CalculoCuantia(Seccion(i / 2).tw_Planos, Seccion(i / 2).Malla.Malla, Seccion(i / 2).Malla.Capas, 1, Seccion(i / 2).RefH_W_Col.Acero, Seccion(i / 2).RefH_W_Col.Capas, Seccion(i / 2).RefH_W_Col.Separacion)
            'Seccion(i / 2).AsH_Col = Seccion(i / 2).Malla.Acero_T + Seccion(i / 2).RefH_W_Col.Acero_T
            Seccion(i / 2).AsH_Col = Seccion(i / 2).Rho_H_col * 1000 * Seccion(i / 2).tw_Planos * 1000

            ' ------------- Tabla Elementos de Borde ----------------------
            '--------- INFORMACION DE EB IZQUIERDO ---------------
            Seccion(i / 2).EB_I_Top.L_EB = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(7).Value)
            Seccion(i / 2).EB_I_Bot.L_EB = Seccion(i / 2).EB_I_Top.L_EB

            Seccion(i / 2).EB_I_Top.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i).Cells(8).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i).Cells(9).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i).Cells(10).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i).Cells(11).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i).Cells(12).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i).Cells(13).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i).Cells(14).Value
            Seccion(i / 2).EB_I_Top.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i).Cells(15).Value

            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i + 1).Cells(8).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i + 1).Cells(9).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i + 1).Cells(10).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i + 1).Cells(11).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i + 1).Cells(12).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i + 1).Cells(13).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i + 1).Cells(14).Value
            Seccion(i / 2).EB_I_Bot.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i + 1).Cells(15).Value

            AsT_Top = 0
            AsT_Bot = 0

            For j As Integer = 2 To 10
                If j <> 9 Then
                    Dim columnIndex As Integer = 8 + (j - 2)
                    If j = 10 Then
                        columnIndex = 8 + (j - 3)
                    End If

                    Dim barra As String = "#" & j.ToString()
                    Dim ac As Single = 0
                    If BarraData.BarraAreas.ContainsKey(barra) Then
                        ac = BarraData.BarraAreas(barra)
                    End If

                    Dim numBar_Top As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(columnIndex).Value)
                    Dim numBar_Bot As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i + 1).Cells(columnIndex).Value)
                    AsT_Top += ac * numBar_Top
                    AsT_Bot += ac * numBar_Bot
                End If
            Next

            Seccion(i / 2).EB_I_Top.Cuantia_L = (AsT_Top / (Seccion(i / 2).EB_I_Top.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100

            Seccion(i / 2).EB_I_Top.RefH.Capas = Convert.ToInt16(Tabla_Info_EBorde.Rows(i).Cells(16).Value)
            Seccion(i / 2).EB_I_Top.RefH.Acero = Tabla_Info_EBorde.Rows(i).Cells(17).Value

            Dim area_ref_var As Single = 0
            If Seccion(i / 2).EB_I_Top.RefH.Acero = "User" Then
                area_ref_var = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(18).Value)
            Else
                area_ref_var = AreaRefuerzo(Seccion(i / 2).EB_I_Top.RefH.Acero)
            End If

            Seccion(i / 2).EB_I_Top.RefH.Acero_T = area_ref_var * Seccion(i / 2).EB_I_Top.RefH.Capas

            Seccion(i / 2).EB_I_Top.RefH.Separacion = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(19).Value)

            If Seccion(i / 2).EB_I_Top.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_I_Top.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_I_Top_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_I_Top_NoEsp = False
            End If

            Seccion(i / 2).EB_I_Bot.RefH.Capas = Seccion(i / 2).EB_I_Top.RefH.Capas
            Seccion(i / 2).EB_I_Bot.RefH.Acero = Seccion(i / 2).EB_I_Top.RefH.Acero
            Seccion(i / 2).EB_I_Bot.RefH.Separacion = Seccion(i / 2).EB_I_Top.RefH.Separacion
            Seccion(i / 2).EB_I_Bot.RefH.Acero_T = Seccion(i / 2).EB_I_Top.RefH.Acero_T
            Seccion(i / 2).EB_I_Bot.Cuantia_L = (AsT_Bot / (Seccion(i / 2).EB_I_Bot.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100
            If Seccion(i / 2).EB_I_Bot.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_I_Bot.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_I_Bot_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_I_Bot_NoEsp = False
            End If

            '--------- INFORMACION DE EB DERECHO ---------------
            Seccion(i / 2).EB_D_Top.L_EB = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(21).Value)
            Seccion(i / 2).EB_D_Bot.L_EB = Seccion(i / 2).EB_D_Top.L_EB

            Seccion(i / 2).EB_D_Top.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i).Cells(22).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i).Cells(23).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i).Cells(24).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i).Cells(25).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i).Cells(26).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i).Cells(27).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i).Cells(28).Value
            Seccion(i / 2).EB_D_Top.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i).Cells(29).Value

            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_2 = Tabla_Info_EBorde.Rows(i + 1).Cells(22).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_3 = Tabla_Info_EBorde.Rows(i + +1).Cells(23).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_4 = Tabla_Info_EBorde.Rows(i + +1).Cells(24).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_5 = Tabla_Info_EBorde.Rows(i + +1).Cells(25).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_6 = Tabla_Info_EBorde.Rows(i + +1).Cells(26).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_7 = Tabla_Info_EBorde.Rows(i + +1).Cells(27).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_8 = Tabla_Info_EBorde.Rows(i + +1).Cells(28).Value
            Seccion(i / 2).EB_D_Bot.Barras_L.Barras_10 = Tabla_Info_EBorde.Rows(i + 1).Cells(29).Value

            AsT_Top = 0
            AsT_Bot = 0

            For j As Integer = 2 To 10
                If j <> 9 Then
                    Dim columnIndex As Integer = 22 + (j - 2)
                    If j = 10 Then
                        columnIndex = 22 + (j - 3)
                    End If

                    Dim barra As String = "#" & j.ToString()
                    Dim ac As Single = 0
                    If BarraData.BarraAreas.ContainsKey(barra) Then
                        ac = BarraData.BarraAreas(barra)
                    End If

                    Dim numBar_Top As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(columnIndex).Value)
                    Dim numBar_Bot As Single = Convert.ToSingle(Tabla_Info_EBorde.Rows(i + 1).Cells(columnIndex).Value)
                    AsT_Top += ac * numBar_Top
                    AsT_Bot += ac * numBar_Bot
                End If
            Next

            Seccion(i / 2).EB_D_Top.RefH.Capas = Convert.ToInt16(Tabla_Info_EBorde.Rows(i).Cells(30).Value)
            Seccion(i / 2).EB_D_Top.RefH.Acero = Tabla_Info_EBorde.Rows(i).Cells(31).Value


            If Seccion(i / 2).EB_D_Top.RefH.Acero = "User" Then
                area_ref_var = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(32).Value)
            Else
                area_ref_var = AreaRefuerzo(Seccion(i / 2).EB_D_Top.RefH.Acero)
            End If

            Seccion(i / 2).EB_D_Top.RefH.Acero_T = area_ref_var * Seccion(i / 2).EB_D_Top.RefH.Capas
            Seccion(i / 2).EB_D_Top.RefH.Separacion = Convert.ToSingle(Tabla_Info_EBorde.Rows(i).Cells(33).Value)

            Seccion(i / 2).EB_D_Top.Cuantia_L = (AsT_Top / (Seccion(i / 2).EB_D_Top.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100

            If Seccion(i / 2).EB_D_Top.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_D_Top.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_D_Top_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_D_Top_NoEsp = False
            End If

            Seccion(i / 2).EB_D_Bot.RefH.Capas = Seccion(i / 2).EB_D_Top.RefH.Capas
            Seccion(i / 2).EB_D_Bot.RefH.Acero = Seccion(i / 2).EB_D_Top.RefH.Acero
            Seccion(i / 2).EB_D_Bot.RefH.Separacion = Seccion(i / 2).EB_D_Top.RefH.Separacion
            Seccion(i / 2).EB_D_Bot.RefH.Acero_T = Seccion(i / 2).EB_D_Top.RefH.Acero_T
            Seccion(i / 2).EB_D_Bot.Cuantia_L = (AsT_Bot / (Seccion(i / 2).EB_D_Bot.L_EB * Seccion(i / 2).tw_Planos * 1000000)) * 100

            If Seccion(i / 2).EB_D_Bot.Cuantia_L > (2.8 / 420) Then
                Seccion(i / 2).EB_D_Bot.Tipo_EB_Req = "No especial"
                Seccion(i / 2).Req_EB_D_Bot_NoEsp = True
            Else
                Seccion(i / 2).Req_EB_D_Bot_NoEsp = False
            End If

            'Analisis de la ubicación y tipo de refuerzo
            Dim Num_Capas As Integer = Seccion(i / 2).RefH_W_Col.Capas
            Dim B As Single = Seccion(i / 2).tw_Planos
            Dim H As Single = Seccion(i / 2).Lw_Planos

            Seccion(i / 2).Refuerzo_Muro_Top_Pr.OrdenarBarrasPorId()
            Seccion(i / 2).Refuerzo_Muro_Bot_Pr.OrdenarBarrasPorId()

            ' -------------- UBICACIÓN DE REFUERZO TOP --------------

            Dim Num_Lineas As Integer = If(Num_Capas > 0, Seccion(i / 2).Cantidad_Barras_Col_Top \ Num_Capas, 0)

            If Seccion(i / 2).ListaRefuerzoCompleto_Top.Count > 0 Then
                Seccion(i / 2).ListaRefuerzoCompleto_Top.Clear()
            End If

            If Num_Lineas > 0 AndAlso Seccion(i / 2).Refuerzo_Muro_Top_Pr.Barras.Count > 0 Then

                ' Orden de asignación exterior→interior: [0, N-1, 1, N-2, 2, ...]
                Dim Assign_Top As New List(Of Integer)
                Dim lo_t As Integer = 0, hi_t As Integer = Num_Lineas - 1
                While lo_t <= hi_t
                    If lo_t = hi_t Then
                        Assign_Top.Add(lo_t)
                    Else
                        Assign_Top.Add(lo_t)
                        Assign_Top.Add(hi_t)
                    End If
                    lo_t += 1 : hi_t -= 1
                End While

                ' Espaciado basado en la primera barra (la mayor)
                Dim Db_ref_t As Single = Seccion(i / 2).Refuerzo_Muro_Top_Pr.Barras(0).Db / 1000
                Dim S_t As Single = If(Num_Lineas > 1, (H - 0.04 - Db_ref_t) / (Num_Lineas - 1), 0)

                Dim fillIdx_t As Integer = 0

                For Each Barra As BarraInfo In Seccion(i / 2).Refuerzo_Muro_Top_Pr.Barras
                    Dim Db_m As Single = Barra.Db / 1000
                    Dim As_b As Single = BarraData.BarraAreas(Barra.String_Barra)
                    Dim Y_Bot_b As Single = -H / 2 + 0.02 + Db_m / 2
                    Dim Coor_X_1 As Single = -B / 2 + 0.02 + Db_m / 2
                    Dim Coor_X_2 As Single = B / 2 - 0.02 - Db_m / 2

                    Dim N_pos As Integer = Barra.Count_Barras \ Num_Capas

                    For k = 0 To N_pos - 1
                        Dim posIdx As Integer = Assign_Top(fillIdx_t + k)
                        Dim Coor_Y As Single = Y_Bot_b + posIdx * S_t

                        If Num_Capas = 2 Then
                            Seccion(i / 2).ListaRefuerzoCompleto_Top.Add(New RefuerzoSimple(Barra.Id, Barra.String_Barra, Barra.Db, As_b, Coor_X_1, Coor_Y))
                            Seccion(i / 2).ListaRefuerzoCompleto_Top.Add(New RefuerzoSimple(Barra.Id, Barra.String_Barra, Barra.Db, As_b, Coor_X_2, Coor_Y))
                        Else
                            Seccion(i / 2).ListaRefuerzoCompleto_Top.Add(New RefuerzoSimple(Barra.Id, Barra.String_Barra, Barra.Db, As_b, 0, Coor_Y))
                        End If
                    Next

                    fillIdx_t += N_pos
                Next

            End If

            ' -------------- UBICACIÓN DE REFUERZO BOT --------------

            Num_Lineas = If(Num_Capas > 0, Seccion(i / 2).Cantidad_Barras_Col_Bot \ Num_Capas, 0)

            If Seccion(i / 2).ListaRefuerzoCompleto_Bot.Count > 0 Then
                Seccion(i / 2).ListaRefuerzoCompleto_Bot.Clear()
            End If

            If Num_Lineas > 0 AndAlso Seccion(i / 2).Refuerzo_Muro_Bot_Pr.Barras.Count > 0 Then

                Dim Assign_Bot As New List(Of Integer)
                Dim lo_b As Integer = 0, hi_b As Integer = Num_Lineas - 1
                While lo_b <= hi_b
                    If lo_b = hi_b Then
                        Assign_Bot.Add(lo_b)
                    Else
                        Assign_Bot.Add(lo_b)
                        Assign_Bot.Add(hi_b)
                    End If
                    lo_b += 1 : hi_b -= 1
                End While

                Dim Db_ref_b As Single = Seccion(i / 2).Refuerzo_Muro_Bot_Pr.Barras(0).Db / 1000
                Dim S_b As Single = If(Num_Lineas > 1, (H - 0.04 - Db_ref_b) / (Num_Lineas - 1), 0)

                Dim fillIdx_b As Integer = 0

                For Each Barra As BarraInfo In Seccion(i / 2).Refuerzo_Muro_Bot_Pr.Barras
                    Dim Db_m As Single = Barra.Db / 1000
                    Dim As_b As Single = BarraData.BarraAreas(Barra.String_Barra)
                    Dim Y_Bot_b As Single = -H / 2 + 0.02 + Db_m / 2
                    Dim Coor_X_1 As Single = -B / 2 + 0.02 + Db_m / 2
                    Dim Coor_X_2 As Single = B / 2 - 0.02 - Db_m / 2

                    Dim N_pos As Integer = Barra.Count_Barras \ Num_Capas

                    For k = 0 To N_pos - 1
                        Dim posIdx As Integer = Assign_Bot(fillIdx_b + k)
                        Dim Coor_Y As Single = Y_Bot_b + posIdx * S_b

                        If Num_Capas = 2 Then
                            Seccion(i / 2).ListaRefuerzoCompleto_Bot.Add(New RefuerzoSimple(Barra.Id, Barra.String_Barra, Barra.Db, As_b, Coor_X_1, Coor_Y))
                            Seccion(i / 2).ListaRefuerzoCompleto_Bot.Add(New RefuerzoSimple(Barra.Id, Barra.String_Barra, Barra.Db, As_b, Coor_X_2, Coor_Y))
                        Else
                            Seccion(i / 2).ListaRefuerzoCompleto_Bot.Add(New RefuerzoSimple(Barra.Id, Barra.String_Barra, Barra.Db, As_b, 0, Coor_Y))
                        End If
                    Next

                    fillIdx_b += N_pos
                Next

            End If

            '====== CALCULO DE DIAGRAMA DE INTERACCIÓN PARA CADA SECCIÓN =======
            Dim List_Ref As List(Of RefuerzoSimple) = Seccion(i / 2).ListaRefuerzoCompleto_Top
            Dim fc As Single = Seccion(i / 2).fc

            Dim result = Funcion_Diagrama_Interaccion(B, H, List_Ref, fc, 420, 200000, 0)
            Seccion(i / 2).Lista_Mn_Top = result.Item1
            Seccion(i / 2).Lista_Pn_Top = result.Item2
            Seccion(i / 2).Lista_M_Top = result.Item3
            Seccion(i / 2).Lista_P_Top = result.Item4

            List_Ref = Seccion(i / 2).ListaRefuerzoCompleto_Bot
            result = Funcion_Diagrama_Interaccion(B, H, List_Ref, fc, 420, 200000, 0)
            Seccion(i / 2).Lista_Mn_Bot = result.Item1
            Seccion(i / 2).Lista_Pn_Bot = result.Item2
            Seccion(i / 2).Lista_M_Bot = result.Item3
            Seccion(i / 2).Lista_P_Bot = result.Item4

            Dim Lista_Combinaciones_Design As List(Of SeccionMuro.Fuerzas_Elementos) = Seccion(i / 2).Lista_Combinaciones.Where(Function(p) proyecto.Elementos.Muros.ListA_Combinaciones_Design.Contains(p.Name)).ToList()

            Dim resultado_Rev_Flexo_Top = CalculoFactorCapacidad(Lista_Combinaciones_Design, Seccion(i / 2).Lista_Mn_Top, Seccion(i / 2).Lista_Pn_Top)
            Seccion(i / 2).Combinacion_Demanda_Flexo_Top = resultado_Rev_Flexo_Top.Item1
            Seccion(i / 2).Factor_Demanda_Flexo_Top = resultado_Rev_Flexo_Top.Item2

            Dim resultado_Rev_Flexo_Bot = CalculoFactorCapacidad(Lista_Combinaciones_Design, Seccion(i / 2).Lista_Mn_Bot, Seccion(i / 2).Lista_Pn_Bot)
            Seccion(i / 2).Combinacion_Demanda_Flexo_Bot = resultado_Rev_Flexo_Bot.Item1
            Seccion(i / 2).Factor_Demanda_Flexo_Bot = resultado_Rev_Flexo_Bot.Item2

        Next

        ' Segunda pasada: procesar secciones "Similar a" copiando datos del patrón
        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Dim simPiso As String = Seccion(i / 2).S_Similar
            If String.IsNullOrEmpty(simPiso) Then Continue For

            Dim patronSec = Seccion.Find(Function(s) s.Piso = simPiso AndAlso s.S_Patron)
            If patronSec Is Nothing Then Continue For

            CopiarRefuerzoDesdePatron(Seccion(i / 2), patronSec)

            Dim B_s As Single = Seccion(i / 2).tw_Planos
            Dim H_s As Single = Seccion(i / 2).Lw_Planos
            Dim fc_s As Single = Seccion(i / 2).fc

            Dim res_t = Funcion_Diagrama_Interaccion(B_s, H_s, Seccion(i / 2).ListaRefuerzoCompleto_Top, fc_s, 420, 200000, 0)
            Seccion(i / 2).Lista_Mn_Top = res_t.Item1
            Seccion(i / 2).Lista_Pn_Top = res_t.Item2
            Seccion(i / 2).Lista_M_Top = res_t.Item3
            Seccion(i / 2).Lista_P_Top = res_t.Item4

            Dim res_b = Funcion_Diagrama_Interaccion(B_s, H_s, Seccion(i / 2).ListaRefuerzoCompleto_Bot, fc_s, 420, 200000, 0)
            Seccion(i / 2).Lista_Mn_Bot = res_b.Item1
            Seccion(i / 2).Lista_Pn_Bot = res_b.Item2
            Seccion(i / 2).Lista_M_Bot = res_b.Item3
            Seccion(i / 2).Lista_P_Bot = res_b.Item4

            Dim combisD = Seccion(i / 2).Lista_Combinaciones.Where(Function(p) proyecto.Elementos.Muros.ListA_Combinaciones_Design.Contains(p.Name)).ToList()
            Dim res_ft = CalculoFactorCapacidad(combisD, Seccion(i / 2).Lista_Mn_Top, Seccion(i / 2).Lista_Pn_Top)
            Seccion(i / 2).Combinacion_Demanda_Flexo_Top = res_ft.Item1
            Seccion(i / 2).Factor_Demanda_Flexo_Top = res_ft.Item2

            Dim res_fb = CalculoFactorCapacidad(combisD, Seccion(i / 2).Lista_Mn_Bot, Seccion(i / 2).Lista_Pn_Bot)
            Seccion(i / 2).Combinacion_Demanda_Flexo_Bot = res_fb.Item1
            Seccion(i / 2).Factor_Demanda_Flexo_Bot = res_fb.Item2
        Next

        ' Calcular verificaciones (cortante, C_Limite, EB) para el muro actual
        Dim muroActual = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Elemento)
        If muroActual IsNot Nothing Then VerificarMuro(muroActual)

        muroActual.Ref_Modificado_Muros = True

        If proyecto.Elementos.Muros.Lista_Muros.FindIndex(Function(p) p.Name = Elemento) < Combo_Elementos.Items.Count - 1 Then

            Combo_Elementos.Text = proyecto.Elementos.Muros.Lista_Muros(proyecto.Elementos.Muros.Lista_Muros.FindIndex(Function(p) p.Name = Elemento) + 1).Name

        Else

            MessageBox.Show("Hecho.", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Information)

        End If

        'Catch ex As Exception

        'End Try

    End Sub

    ' Verifica cortante, C_Limite y elementos de borde para UN muro.
    ' Es llamado automáticamente por Button4 (al guardar) y por Button3 (todos los muros).
    Private Sub VerificarMuro(Elemento As Muro)

        For j = 0 To Elemento.Lista_Secciones.Count - 1
            Dim seccion = Elemento.Lista_Secciones(j)

                    '----- Verificación Cuantias minimas ---------
                    seccion.fy = 420
                    Dim Ver_Cuantia = FuncionCorteLimite(seccion.tw_Planos, seccion.Lw_Planos, seccion.fc, seccion.Vu)
                    seccion.Rho_Min_L = Ver_Cuantia(0)
                    seccion.Rho_Min_T = Ver_Cuantia(1)
                    seccion.V_Limite_Cuantia = Ver_Cuantia(2)

                    '------ Verificación a Flexo-Compresión ---------
                    seccion.F_Flexo_Top = Math.Round(seccion.AsT_Top_Col / seccion.As_Top_Req, 2)
                    seccion.F_Flexo_Bot = Math.Round(seccion.AsT_Bot_Col / seccion.As_Bot_Req, 2)

                    '----- Verificación a Cortante ----------
                    Dim Rev_Cortante = FuncionCortante(seccion.tw_Planos, seccion.Lw_Planos, seccion.fc, 420, seccion.Rho_H_col, seccion.Vu)
                    seccion.Vc = Rev_Cortante(1)
                    seccion.Vs = Rev_Cortante(2)
                    seccion.Vn = Rev_Cortante(3)
                    seccion.F_Cortante = Rev_Cortante(4)

                    seccion.AsH_Req_Top = seccion.Rho_Min_T * seccion.tw_Planos * 10000

                    '----- Verificacion de los elementos de borde --------------
                    seccion.EB_I_Top.Tipo_EB_Col = ""
                    seccion.EB_I_Bot.Tipo_EB_Col = ""
                    seccion.EB_D_Top.Tipo_EB_Col = ""
                    seccion.EB_D_Bot.Tipo_EB_Col = ""

                    Dim D_Techo As Single = proyecto.Elementos.Muros.D_Techo_X
                    If seccion.Direccion_Muro = eNumeradores.eDireccion.Y Then
                        D_Techo = proyecto.Elementos.Muros.D_Techo_Y
                    End If
                    seccion.C_Limite = EB_C(proyecto.ParametrosSismicos.NDE, D_Techo, Elemento.Hw, seccion.Lw_Planos)
                    seccion.Chequeo_EB_I_Top_Def = "No requiere"
                    seccion.Chequeo_EB_I_Bot_Def = "No requiere"
                    seccion.Chequeo_EB_D_Top_Def = "No requiere"
                    seccion.Chequeo_EB_D_Bot_Def = "No requiere"

                    seccion.EB_I_Top.Tipo_EB_Req = "No requiere"
                    seccion.EB_I_Bot.Tipo_EB_Req = "No requiere"
                    seccion.EB_D_Top.Tipo_EB_Req = "No requiere"
                    seccion.EB_D_Bot.Tipo_EB_Req = "No requiere"

                    seccion.EB_I_Top.L_EB_Req = 0
                    seccion.EB_I_Bot.L_EB_Req = 0
                    seccion.EB_D_Top.L_EB_Req = 0
                    seccion.EB_D_Bot.L_EB_Req = 0

                    seccion.Req_EB_I_Top_Esp = False
                    seccion.Req_EB_I_Bot_Esp = False
                    seccion.Req_EB_D_Top_Esp = False
                    seccion.Req_EB_D_Bot_Esp = False

                    seccion.Req_EB_I_Top_NoEsp = False
                    seccion.Req_EB_I_Bot_NoEsp = False
                    seccion.Req_EB_D_Top_NoEsp = False
                    seccion.Req_EB_D_Bot_NoEsp = False

                    If seccion.C_Limite / 0.9 < seccion.C_I_Top Then
                        seccion.EB_I_Top.L_EB_Req = Math.Max(seccion.C_I_Top - 0.1 * seccion.Lw_Planos, seccion.C_I_Top / 2)
                        seccion.Chequeo_EB_I_Top_Def = "Requiere"
                        seccion.EB_I_Top.Tipo_EB_Req = "Especial"
                        seccion.Req_EB_I_Top_Esp = True

                        If Elemento.LV_EB_I_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Def = seccion.Altura
                        End If
                    End If

                    If seccion.C_Limite / 0.9 < seccion.C_I_Bot Then
                        seccion.EB_I_Bot.L_EB_Req = Math.Max(seccion.C_I_Bot - 0.1 * seccion.Lw_Planos, seccion.C_I_Bot / 2)
                        seccion.Chequeo_EB_I_Bot_Def = "Requiere"
                        seccion.EB_I_Bot.Tipo_EB_Req = "Especial"
                        seccion.Req_EB_I_Bot_Esp = True

                        If Elemento.LV_EB_I_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Def = seccion.Altura
                        End If
                    End If

                    If seccion.C_Limite / 0.9 < seccion.C_D_Top Then
                        seccion.EB_D_Top.L_EB_Req = Math.Max(seccion.C_D_Top - 0.1 * seccion.Lw_Planos, seccion.C_D_Top / 2)
                        seccion.Chequeo_EB_D_Top_Def = "Requiere"
                        seccion.EB_D_Top.Tipo_EB_Req = "Especial"

                        seccion.Req_EB_D_Top_Esp = True
                        If Elemento.LV_EB_D_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Def = seccion.Altura
                        End If
                    End If

                    If seccion.C_Limite / 0.9 < seccion.C_D_Bot Then
                        seccion.EB_D_Bot.L_EB_Req = Math.Max(seccion.C_D_Bot - 0.1 * seccion.Lw_Planos, seccion.C_D_Bot / 2)
                        seccion.Chequeo_EB_D_Bot_Def = "Requiere"
                        seccion.EB_D_Bot.Tipo_EB_Req = "Especial"

                        seccion.Req_EB_D_Bot_Esp = True
                        If Elemento.LV_EB_D_Req_Def <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Def = seccion.Altura
                        End If
                    End If

                    Dim Rev_EB_Esf_I_Top = EB_Esf(proyecto.ParametrosSismicos.NDE, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_I_Top)
                    Dim Rev_EB_Esf_I_Bot = EB_Esf(proyecto.ParametrosSismicos.NDE, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_I_Bot)

                    Dim Rev_EB_Esf_D_Top = EB_Esf(proyecto.ParametrosSismicos.NDE, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_D_Top)
                    Dim Rev_EB_Esf_D_Bot = EB_Esf(proyecto.ParametrosSismicos.NDE, Elemento.Hw, seccion.Lw_Planos, seccion.fc, seccion.Esf_D_Bot)

                    seccion.Esf_max = Rev_EB_Esf_I_Top(1)
                    seccion.Esf_Lim = Rev_EB_Esf_I_Top(2)

                    seccion.Chequeo_EB_I_Top_Esf = Rev_EB_Esf_I_Top(0)
                    If seccion.Chequeo_EB_I_Top_Esf = "Requiere" Then
                        seccion.EB_I_Top.Tipo_EB_Req = "Especial"
                        seccion.EB_I_Top.L_EB_Req = Math.Max(seccion.C_I_Top - 0.1 * seccion.Lw_Planos, seccion.C_I_Top / 2)
                        seccion.Req_EB_I_Top_Esp = True
                        If Elemento.LV_EB_I_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Esf = seccion.Altura
                        End If
                    End If

                    seccion.Chequeo_EB_I_Bot_Esf = Rev_EB_Esf_I_Bot(0)
                    If seccion.Chequeo_EB_I_Bot_Esf = "Requiere" Then
                        seccion.EB_I_Bot.Tipo_EB_Req = "Especial"
                        seccion.EB_I_Bot.L_EB_Req = Math.Max(seccion.C_I_Bot - 0.1 * seccion.Lw_Planos, seccion.C_I_Bot / 2)
                        seccion.Req_EB_I_Bot_Esp = True
                        If Elemento.LV_EB_I_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_I_Req_Esf = seccion.Altura
                        End If
                    End If

                    seccion.Chequeo_EB_D_Top_Esf = Rev_EB_Esf_D_Top(0)
                    If seccion.Chequeo_EB_D_Top_Esf = "Requiere" Then
                        seccion.EB_D_Top.Tipo_EB_Req = "Especial"
                        seccion.EB_D_Top.L_EB_Req = Math.Max(seccion.C_D_Top - 0.1 * seccion.Lw_Planos, seccion.C_D_Top / 2)
                        seccion.Req_EB_D_Top_Esp = True
                        If Elemento.LV_EB_D_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Esf = seccion.Altura
                        End If
                    End If

                    seccion.Chequeo_EB_D_Bot_Esf = Rev_EB_Esf_D_Bot(0)
                    If seccion.Chequeo_EB_D_Bot_Esf = "Requiere" Then
                        seccion.EB_D_Bot.Tipo_EB_Req = "Especial"
                        seccion.EB_D_Bot.L_EB_Req = Math.Max(seccion.C_D_Bot - 0.1 * seccion.Lw_Planos, seccion.C_D_Bot / 2)
                        seccion.Req_EB_D_Bot_Esp = True
                        If Elemento.LV_EB_D_Req_Esf <= seccion.Altura Then
                            Elemento.LV_EB_D_Req_Esf = seccion.Altura
                        End If
                    End If

                    Dim Ash_H_EBI_Top = AceroH_EB(proyecto.ParametrosSismicos.NDE, seccion.EB_I_Top, seccion.tw_Planos, seccion.fc, seccion.fy)
                    Dim Ash_H_EBI_Bot = AceroH_EB(proyecto.ParametrosSismicos.NDE, seccion.EB_I_Bot, seccion.tw_Planos, seccion.fc, seccion.fy)
                    seccion.EB_I_Top.RefH_Req = Ash_H_EBI_Top(1)
                    seccion.EB_I_Bot.RefH_Req = Ash_H_EBI_Bot(1)

                    Dim Ash_H_EBD_Top = AceroH_EB(proyecto.ParametrosSismicos.NDE, seccion.EB_D_Top, seccion.tw_Planos, seccion.fc, seccion.fy)
                    Dim Ash_H_EBD_Bot = AceroH_EB(proyecto.ParametrosSismicos.NDE, seccion.EB_D_Bot, seccion.tw_Planos, seccion.fc, seccion.fy)
                    seccion.EB_D_Top.RefH_Req = Ash_H_EBD_Top(1)
                    seccion.EB_D_Bot.RefH_Req = Ash_H_EBD_Bot(1)

                    If 0 < seccion.EB_I_Top.RefH.Separacion And seccion.EB_I_Top.RefH.Separacion <= Ash_H_EBI_Top(0) / 1000 Then
                        seccion.EB_I_Top.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_I_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_I_Top.RefH.Separacion And seccion.EB_I_Top.RefH.Separacion > 0 Then
                        seccion.EB_I_Top.Tipo_EB_Col = "No especial"
                        If seccion.EB_I_Top.Tipo_EB_Req = "No especial" Then
                            seccion.EB_I_Top.RefH_Req = (Math.Round(seccion.EB_I_Top.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_I_Top.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_I_Top.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_I_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If 0 < seccion.EB_I_Bot.RefH.Separacion And seccion.EB_I_Bot.RefH.Separacion <= Ash_H_EBI_Bot(0) / 1000 Then
                        seccion.EB_I_Bot.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_I_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_I_Bot.RefH.Separacion And seccion.EB_I_Bot.RefH.Separacion > 0 Then
                        seccion.EB_I_Bot.Tipo_EB_Col = "No especial"
                        If seccion.EB_I_Bot.Tipo_EB_Req = "No especial" Then
                            seccion.EB_I_Bot.RefH_Req = (Math.Round(seccion.EB_I_Bot.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_I_Bot.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_I_Bot.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_I_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_I_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If 0 < seccion.EB_D_Top.RefH.Separacion And seccion.EB_D_Top.RefH.Separacion <= Ash_H_EBD_Top(0) / 1000 Then
                        seccion.EB_D_Top.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_D_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_D_Top.RefH.Separacion And seccion.EB_D_Top.RefH.Separacion > 0 Then
                        seccion.EB_D_Top.Tipo_EB_Col = "No especial"
                        If seccion.EB_D_Top.Tipo_EB_Req = "No especial" Then
                            seccion.EB_D_Top.RefH_Req = (Math.Round(seccion.EB_D_Top.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_D_Top.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_D_Top.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_D_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If 0 < seccion.EB_D_Bot.RefH.Separacion And seccion.EB_D_Bot.RefH.Separacion <= Ash_H_EBD_Bot(0) / 1000 Then
                        seccion.EB_D_Bot.Tipo_EB_Col = "Especial"
                        If Elemento.LV_EB_D_Col_Esp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_Esp = seccion.Altura
                        End If
                    ElseIf Ash_H_EBI_Top(0) / 1000 <= seccion.EB_D_Bot.RefH.Separacion And seccion.EB_D_Bot.RefH.Separacion > 0 Then
                        seccion.EB_D_Bot.Tipo_EB_Col = "No especial"
                        If seccion.EB_D_Bot.Tipo_EB_Req = "No especial" Then
                            seccion.EB_D_Bot.RefH_Req = (Math.Round(seccion.EB_D_Bot.L_EB / 0.35, 0) + 1) * 32
                        ElseIf seccion.EB_D_Bot.Tipo_EB_Req = "No requiere" Then
                            seccion.EB_D_Bot.RefH_Req = 0
                        End If
                        If Elemento.LV_EB_D_Col_NoEsp <= seccion.Altura Then
                            Elemento.LV_EB_D_Col_NoEsp = seccion.Altura
                        End If
                    End If

                    If seccion.Req_EB_I_Top_Esp = False And seccion.EB_I_Top.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_I_Top_NoEsp = True
                        seccion.EB_I_Top.Tipo_EB_Req = "No especial"
                        seccion.EB_I_Top.RefH_Req = (Math.Round(seccion.EB_I_Top.L_EB / 0.35, 0) + 1) * 32
                    End If
                    If seccion.Req_EB_I_Bot_Esp = False And seccion.EB_I_Bot.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_I_Bot_NoEsp = True
                        seccion.EB_I_Bot.Tipo_EB_Req = "No especial"
                        seccion.EB_I_Bot.RefH_Req = (Math.Round(seccion.EB_I_Bot.L_EB / 0.35, 0) + 1) * 32
                    End If

                    If seccion.Req_EB_D_Top_Esp = False And seccion.EB_D_Top.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_D_Top_NoEsp = True
                        seccion.EB_D_Top.Tipo_EB_Req = "No especial"
                        seccion.EB_D_Top.RefH_Req = (Math.Round(seccion.EB_D_Top.L_EB / 0.35, 0) + 1) * 32
                    End If
                    If seccion.Req_EB_D_Bot_Esp = False And seccion.EB_D_Bot.Cuantia_L > 2.8 * 100 / seccion.fy Then
                        seccion.Req_EB_D_Bot_NoEsp = True
                        seccion.EB_D_Bot.Tipo_EB_Req = "No especial"
                        seccion.EB_D_Bot.RefH_Req = (Math.Round(seccion.EB_D_Bot.L_EB / 0.35, 0) + 1) * 32
                    End If
        Next

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Try
            For i = 0 To proyecto.Elementos.Muros.Lista_Muros.Count - 1
                VerificarMuro(proyecto.Elementos.Muros.Lista_Muros(i))
            Next

        Catch ex As Exception

        Finally
            If proyecto.Elementos.Muros.Lista_Muros.Count > 0 Then
                For i = 0 To proyecto.Elementos.Muros.Lista_Muros.Count - 1
                    Form_06_01_ResultadosMuros.Combo_Elementos.Items.Add(proyecto.Elementos.Muros.Lista_Muros(i).Label)
                Next
                Form_06_01_ResultadosMuros.Combo_Elementos.Text = proyecto.Elementos.Muros.Lista_Muros(0).Label
            End If

            Form_06_01_ResultadosMuros.Show()
            Form_06_01_ResultadosMuros.WindowState = FormWindowState.Maximized

        End Try

    End Sub

    Private Sub Op_SeccionSimilar_CheckedChanged(sender As Object, e As EventArgs) Handles Op_SeccionSimilar.CheckedChanged
        'Dim Secciones_Principales = Proyecto.Elementos.Muros.Lista_Muros.FindAll(Function(p) p.Secciones_Principal = True)
        C_Lista_Secciones_Principales.Items.Clear()

        If Op_SeccionPrincipal.Checked = False Then
            'If Secciones_Principales.Count < 1 Then
            '    '    MessageBox.Show("No se tiene registro de ninguna sección", "Información Ingresada", MessageBoxButtons.OK, MessageBoxIcon.Error)
            '    '    Op_SeccionPrincipal.Checked = True
            '    'Else
            '    '    For i = 0 To Secciones_Principales.Count - 1
            '    '        C_Lista_Secciones_Principales.Items.Add(Secciones_Principales(i).Name_Label)
            '    '    Next
            '    '    Form_02_PagColumnas.Proyecto.Elementos.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Similar = True
            '    '    Form_02_PagColumnas.Proyecto.Elementos.Lista_Columnas.Find(Function(p) p.Name_Elemento = Combo_Elementos.Text).Secciones_Principal = False
            '    '    C_Lista_Secciones_Principales.Enabled = True
            '    'End If
            For i = 0 To proyecto.Elementos.Muros.Lista_Muros.Count - 1
                Dim muro = proyecto.Elementos.Muros.Lista_Muros(i)
                If muro.Ref_Modificado_Muros AndAlso muro.Lista_Secciones.Any(Function(s) s.S_Patron) Then
                    C_Lista_Secciones_Principales.Items.Add(muro.Label)
                End If
            Next

            C_Lista_Secciones_Principales.Enabled = True
        End If
    End Sub

    Private Sub C_Lista_Secciones_Principales_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_Lista_Secciones_Principales.SelectedIndexChanged

        If Op_SeccionSimilar.Checked = True Then

            Tabla_Info_Seccion.Rows.Clear()
            Tabla_Info_EBorde.Rows.Clear()

            Dim Elemento = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Label = C_Lista_Secciones_Principales.Text)
            Dim Seccion = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Label = C_Lista_Secciones_Principales.Text).Lista_Secciones

            For i = 0 To (Seccion.Count - 1) * 2 + 1
                Tabla_Info_Seccion.Rows.Add()
                Tabla_Info_EBorde.Rows.Add()
            Next
            Tabla_Info_Seccion.Refresh()
            Tabla_Info_EBorde.Refresh()


            For i = 0 To ((Seccion.Count - 1) * 2) Step 2
                Tabla_Info_Seccion.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_Seccion.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_Seccion.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_Seccion.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_Seccion.Rows(i + 1).Cells(4).Value = "Bottom"

                If Seccion(i / 2).S_Patron = False Then
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "None"
                Else
                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_Seccion.Rows(i + 1).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "Si"
                    Tabla_Info_EBorde.Rows(i + 1).Cells(5).Value = "Si"
                End If

                Tabla_Info_EBorde.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
                Tabla_Info_EBorde.Rows(i).Cells(1).Value = Seccion(i / 2).fc
                Tabla_Info_EBorde.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
                Tabla_Info_EBorde.Rows(i).Cells(4).Value = "Top"
                Tabla_Info_EBorde.Rows(i + 1).Cells(4).Value = "Bottom"

                If proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Label = C_Lista_Secciones_Principales.Text).Ref_Modificado_Muros = False Then

                    For j = 7 To 14

                        Tabla_Info_Seccion.Rows(i).Cells(j).Value = 0
                        Tabla_Info_Seccion.Rows(i + 1).Cells(j).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 1).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 1).Value = 0

                        Tabla_Info_EBorde.Rows(i).Cells(j + 14).Value = 0
                        Tabla_Info_EBorde.Rows(i + 1).Cells(j + 14).Value = 0

                    Next

                    Tabla_Info_Seccion.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(6).Value = ""
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = 0

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = "0"
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = "None"
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = "0"

                    Tabla_Info_EBorde.Rows(i).Cells(5).Value = "None"
                    Tabla_Info_EBorde.Rows(i).Cells(6).Value = ""
                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(18).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(21).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(32).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(33).Value = 0

                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = "#2"
                    Tabla_Info_EBorde.Rows(i).Cells(19).Value = 0
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = "#2"

                Else

                    Tabla_Info_Seccion.Rows(i).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_2
                    Tabla_Info_Seccion.Rows(i).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_3
                    Tabla_Info_Seccion.Rows(i).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_4
                    Tabla_Info_Seccion.Rows(i).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_5
                    Tabla_Info_Seccion.Rows(i).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_6
                    Tabla_Info_Seccion.Rows(i).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_7
                    Tabla_Info_Seccion.Rows(i).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_8
                    Tabla_Info_Seccion.Rows(i).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Top.Barras_10

                    Tabla_Info_Seccion.Rows(i + 1).Cells(7).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_2
                    Tabla_Info_Seccion.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_3
                    Tabla_Info_Seccion.Rows(i + 1).Cells(9).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_4
                    Tabla_Info_Seccion.Rows(i + 1).Cells(10).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_5
                    Tabla_Info_Seccion.Rows(i + 1).Cells(11).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_6
                    Tabla_Info_Seccion.Rows(i + 1).Cells(12).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_7
                    Tabla_Info_Seccion.Rows(i + 1).Cells(13).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_8
                    Tabla_Info_Seccion.Rows(i + 1).Cells(14).Value = Seccion(i / 2).Refuerzo_Muro_Bottom.Barras_10

                    Tabla_Info_Seccion.Rows(i).Cells(15).Value = Seccion(i / 2).Malla.Malla
                    Tabla_Info_Seccion.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).Malla.Capas)
                    Tabla_Info_Seccion.Rows(i).Cells(17).Value = Seccion(i / 2).RefH_W_Col.Acero
                    Tabla_Info_Seccion.Rows(i).Cells(18).Value = Seccion(i / 2).RefH_W_Col.Separacion
                    Tabla_Info_Seccion.Rows(i).Cells(19).Value = Convert.ToString(Seccion(i / 2).RefH_W_Col.Capas)

                    Tabla_Info_EBorde.Rows(i).Cells(7).Value = Seccion(i / 2).EB_I_Top.L_EB
                    Tabla_Info_EBorde.Rows(i).Cells(8).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(9).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(10).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(11).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(12).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(13).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(14).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(15).Value = Seccion(i / 2).EB_I_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(8).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(9).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(10).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(11).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(12).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(13).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(14).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(15).Value = Seccion(i / 2).EB_I_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(16).Value = Convert.ToString(Seccion(i / 2).EB_I_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(17).Value = Seccion(i / 2).EB_I_Top.RefH.Acero
                    Tabla_Info_EBorde.Rows(i).Cells(19).Value = Seccion(i / 2).EB_I_Top.RefH.Separacion

                    Tabla_Info_EBorde.Rows(i).Cells(21).Value = Seccion(i / 2).EB_D_Top.L_EB

                    Tabla_Info_EBorde.Rows(i).Cells(22).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i).Cells(23).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i).Cells(24).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i).Cells(25).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i).Cells(26).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i).Cells(27).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i).Cells(28).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i).Cells(29).Value = Seccion(i / 2).EB_D_Top.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i + 1).Cells(22).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_2
                    Tabla_Info_EBorde.Rows(i + 1).Cells(23).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_3
                    Tabla_Info_EBorde.Rows(i + 1).Cells(24).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_4
                    Tabla_Info_EBorde.Rows(i + 1).Cells(25).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_5
                    Tabla_Info_EBorde.Rows(i + 1).Cells(26).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_6
                    Tabla_Info_EBorde.Rows(i + 1).Cells(27).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_7
                    Tabla_Info_EBorde.Rows(i + 1).Cells(28).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_8
                    Tabla_Info_EBorde.Rows(i + 1).Cells(29).Value = Seccion(i / 2).EB_D_Bot.Barras_L.Barras_10

                    Tabla_Info_EBorde.Rows(i).Cells(30).Value = Convert.ToString(Seccion(i / 2).EB_D_Top.RefH.Capas)
                    Tabla_Info_EBorde.Rows(i).Cells(31).Value = Seccion(i / 2).EB_D_Top.RefH.Acero
                    Tabla_Info_EBorde.Rows(i).Cells(33).Value = Seccion(i / 2).EB_D_Top.RefH.Separacion

                End If
            Next


        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As System.Windows.Forms.Message, keyData As System.Windows.Forms.Keys) As Boolean

        ' Verificamos si el control que tiene el foco es un DataGridView.
        Dim focusedControl = Me.ActiveControl

        If TypeOf focusedControl Is DataGridView Then
            Dim dataGridView As DataGridView = DirectCast(focusedControl, DataGridView)

            ' Comprobamos si se ha pulsado la combinación de teclas Ctrl + V.
            If (keyData = (Keys.Control Or Keys.V)) Then
                ' Comprobamos si el contenido del portapapeles es texto.
                If Clipboard.ContainsText() Then
                    ' Recorremos las celdas seleccionadas y pegamos el texto del portapapeles en cada una de ellas.
                    For Each celda As DataGridViewCell In dataGridView.SelectedCells
                        celda.Value = Clipboard.GetText()
                    Next

                    ' Indicamos que hemos manejado la combinación de teclas.
                    Return True
                End If
            End If
        End If

        ' Si no se cumple ninguna de las condiciones anteriores, pasamos al procesamiento predeterminado de comandos.
        Return MyBase.ProcessCmdKey(msg, keyData)

    End Function

    Private Sub VerSecciónMuroToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VerSecciónMuroToolStripMenuItem.Click

        Dim Elemento As String = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Name
        Dim Seccion = proyecto.Elementos.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Lista_Secciones

        Dim b As Single = 0.1
        Dim h As Single = 2.5
        Dim cuantia As Single = 0.0025
        Dim listaRefuerzo As New List(Of RefuerzoSimple)()

        For i As Integer = 1 To 20
            Dim refuerzo As New RefuerzoSimple(
            id_patron:=1,
            name_Barra:=i,
            db:=9.6,
            asb:=71,
            coord_X:=0,
            coord_Y:=-h / 2 + i * (h / 21)
        )
            listaRefuerzo.Add(refuerzo)
        Next

        Dim sRectangularForm As New SRectangular(b, h, cuantia, listaRefuerzo)

        For Each Muro As Muro In proyecto.Elementos.Muros.Lista_Muros
            sRectangularForm.Combo_Seccion.Items.Add(Muro.Name)
        Next

        For Each SeccionMuro As SeccionMuro In Seccion
            sRectangularForm.Combo_Tramos.Items.Add(SeccionMuro.Piso)
        Next

        sRectangularForm.Combo_Estacion.Items.Add("Top")
        sRectangularForm.Combo_Estacion.Items.Add("Bottom")

        sRectangularForm.Show()

    End Sub

#Region " Menú contextual: Aplicar valor a todos los pisos "

    ' Columnas ComboBox que deben mostrar el menú en Tabla_Info_Seccion:
    '   "Column17" = Malla,  "Column15" = As Htal (#)
    Private Shared ReadOnly _colNamesLlenarSeccion As New HashSet(Of String) From {"Column15", "Column17"}

    ' Columnas ComboBox que deben mostrar el menú en Tabla_Info_EBorde:
    '   "DataGridViewComboBoxColumn6" = As Ramas (#) Borde Izq
    '   "DataGridViewComboBoxColumn7" = As Ramas (#) Borde Der
    Private Shared ReadOnly _colNamesLlenarEBorde As New HashSet(Of String) From {"DataGridViewComboBoxColumn6", "DataGridViewComboBoxColumn7"}

    Private _ctxLlenarTable As DataGridView = Nothing
    Private _ctxLlenarColIndex As Integer = -1
    Private _ctxLlenarValue As Object = Nothing
    Private _menuLlenarColumna As ContextMenuStrip

    Private Sub ConfigurarMenuLlenarColumna()
        _menuLlenarColumna = New ContextMenuStrip()
        _menuLlenarColumna.Font = New Font("Microsoft Sans Serif", 9.5F)
        Dim item As New ToolStripMenuItem("Aplicar a todos los pisos")
        AddHandler item.Click, AddressOf MenuLlenarColumna_Click
        _menuLlenarColumna.Items.Add(item)

        AddHandler Tabla_Info_Seccion.CellMouseDown, AddressOf Tabla_CellMouseDown_LlenarColumna
        AddHandler Tabla_Info_EBorde.CellMouseDown, AddressOf Tabla_CellMouseDown_LlenarColumna
    End Sub

    Private Sub Tabla_CellMouseDown_LlenarColumna(sender As Object, e As DataGridViewCellMouseEventArgs)
        If e.Button <> MouseButtons.Right OrElse e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return

        Dim tabla = DirectCast(sender, DataGridView)
        Dim colName = tabla.Columns(e.ColumnIndex).Name

        Dim esTarget As Boolean
        If tabla Is Tabla_Info_Seccion Then
            esTarget = _colNamesLlenarSeccion.Contains(colName)
        ElseIf tabla Is Tabla_Info_EBorde Then
            esTarget = _colNamesLlenarEBorde.Contains(colName)
        Else
            Return
        End If

        If Not esTarget Then Return

        Dim valor = tabla.Rows(e.RowIndex).Cells(e.ColumnIndex).Value
        If valor Is Nothing OrElse String.IsNullOrEmpty(valor.ToString()) Then Return

        _ctxLlenarTable = tabla
        _ctxLlenarColIndex = e.ColumnIndex
        _ctxLlenarValue = valor

        CType(_menuLlenarColumna.Items(0), ToolStripMenuItem).Text =
            String.Format("Aplicar ""{0}"" a todos los pisos", valor.ToString())

        tabla.CurrentCell = tabla.Rows(e.RowIndex).Cells(e.ColumnIndex)
        _menuLlenarColumna.Show(tabla, tabla.PointToClient(Cursor.Position))
    End Sub

    Private Sub MenuLlenarColumna_Click(sender As Object, e As EventArgs)
        If _ctxLlenarTable Is Nothing OrElse _ctxLlenarColIndex < 0 Then Return

        For Each row As DataGridViewRow In _ctxLlenarTable.Rows
            row.Cells(_ctxLlenarColIndex).Value = _ctxLlenarValue
        Next

        _ctxLlenarTable = Nothing
        _ctxLlenarColIndex = -1
        _ctxLlenarValue = Nothing
    End Sub

#End Region

    ' Copia todos los datos de refuerzo del patrón al destino (sección "Similar a").
    Private Sub CopiarRefuerzoDesdePatron(destino As SeccionMuro, patron As SeccionMuro)
        ' Refuerzo longitudinal Top
        destino.Refuerzo_Muro_Top.Barras_2  = patron.Refuerzo_Muro_Top.Barras_2
        destino.Refuerzo_Muro_Top.Barras_3  = patron.Refuerzo_Muro_Top.Barras_3
        destino.Refuerzo_Muro_Top.Barras_4  = patron.Refuerzo_Muro_Top.Barras_4
        destino.Refuerzo_Muro_Top.Barras_5  = patron.Refuerzo_Muro_Top.Barras_5
        destino.Refuerzo_Muro_Top.Barras_6  = patron.Refuerzo_Muro_Top.Barras_6
        destino.Refuerzo_Muro_Top.Barras_7  = patron.Refuerzo_Muro_Top.Barras_7
        destino.Refuerzo_Muro_Top.Barras_8  = patron.Refuerzo_Muro_Top.Barras_8
        destino.Refuerzo_Muro_Top.Barras_10 = patron.Refuerzo_Muro_Top.Barras_10
        ' Refuerzo longitudinal Bottom
        destino.Refuerzo_Muro_Bottom.Barras_2  = patron.Refuerzo_Muro_Bottom.Barras_2
        destino.Refuerzo_Muro_Bottom.Barras_3  = patron.Refuerzo_Muro_Bottom.Barras_3
        destino.Refuerzo_Muro_Bottom.Barras_4  = patron.Refuerzo_Muro_Bottom.Barras_4
        destino.Refuerzo_Muro_Bottom.Barras_5  = patron.Refuerzo_Muro_Bottom.Barras_5
        destino.Refuerzo_Muro_Bottom.Barras_6  = patron.Refuerzo_Muro_Bottom.Barras_6
        destino.Refuerzo_Muro_Bottom.Barras_7  = patron.Refuerzo_Muro_Bottom.Barras_7
        destino.Refuerzo_Muro_Bottom.Barras_8  = patron.Refuerzo_Muro_Bottom.Barras_8
        destino.Refuerzo_Muro_Bottom.Barras_10 = patron.Refuerzo_Muro_Bottom.Barras_10
        ' Malla
        destino.Malla.Tipo_Malla = patron.Malla.Tipo_Malla
        destino.Malla.Malla      = patron.Malla.Malla
        destino.Malla.Capas      = patron.Malla.Capas
        destino.Malla.Acero_T    = patron.Malla.Acero_T
        ' Refuerzo horizontal
        destino.RefH_W_Col.Acero      = patron.RefH_W_Col.Acero
        destino.RefH_W_Col.Capas      = patron.RefH_W_Col.Capas
        destino.RefH_W_Col.Separacion = patron.RefH_W_Col.Separacion
        destino.RefH_W_Col.Acero_T    = patron.RefH_W_Col.Acero_T
        destino.Rho_H_col = patron.Rho_H_col
        destino.AsH_Col   = patron.AsH_Col
        ' Totales y cuantías
        destino.AsT_Top_Col            = patron.AsT_Top_Col
        destino.AsT_Bot_Col            = patron.AsT_Bot_Col
        destino.Cuantia_Top_Col        = patron.Cuantia_Top_Col
        destino.Cuantia_Bot_Col        = patron.Cuantia_Bot_Col
        destino.Cantidad_Barras_Col_Top = patron.Cantidad_Barras_Col_Top
        destino.Cantidad_Barras_Col_Bot = patron.Cantidad_Barras_Col_Bot
        ' Objetos de refuerzo detallado (referencia compartida — solo lectura posterior)
        destino.Refuerzo_Muro_Top_Pr = patron.Refuerzo_Muro_Top_Pr
        destino.Refuerzo_Muro_Bot_Pr = patron.Refuerzo_Muro_Bot_Pr
        ' Listas completas de refuerzo para diagrama
        destino.ListaRefuerzoCompleto_Top = patron.ListaRefuerzoCompleto_Top.ToList()
        destino.ListaRefuerzoCompleto_Bot = patron.ListaRefuerzoCompleto_Bot.ToList()
        ' Elementos de Borde
        CopiarEB(destino.EB_I_Top, patron.EB_I_Top)
        CopiarEB(destino.EB_I_Bot, patron.EB_I_Bot)
        CopiarEB(destino.EB_D_Top, patron.EB_D_Top)
        CopiarEB(destino.EB_D_Bot, patron.EB_D_Bot)
        destino.Req_EB_I_Top_NoEsp = patron.Req_EB_I_Top_NoEsp
        destino.Req_EB_I_Bot_NoEsp = patron.Req_EB_I_Bot_NoEsp
        destino.Req_EB_D_Top_NoEsp = patron.Req_EB_D_Top_NoEsp
        destino.Req_EB_D_Bot_NoEsp = patron.Req_EB_D_Bot_NoEsp
    End Sub

    Private Sub CopiarEB(destino As SeccionMuro.ElementoBorde, patron As SeccionMuro.ElementoBorde)
        destino.L_EB      = patron.L_EB
        destino.L_EB_Req  = patron.L_EB_Req
        destino.Tipo_EB_Col = patron.Tipo_EB_Col
        destino.Tipo_EB_Req = patron.Tipo_EB_Req
        destino.Cuantia_L = patron.Cuantia_L
        destino.Ash       = patron.Ash
        destino.Ash_Req   = patron.Ash_Req
        destino.Barras_L.Barras_2  = patron.Barras_L.Barras_2
        destino.Barras_L.Barras_3  = patron.Barras_L.Barras_3
        destino.Barras_L.Barras_4  = patron.Barras_L.Barras_4
        destino.Barras_L.Barras_5  = patron.Barras_L.Barras_5
        destino.Barras_L.Barras_6  = patron.Barras_L.Barras_6
        destino.Barras_L.Barras_7  = patron.Barras_L.Barras_7
        destino.Barras_L.Barras_8  = patron.Barras_L.Barras_8
        destino.Barras_L.Barras_10 = patron.Barras_L.Barras_10
        destino.RefH.Acero      = patron.RefH.Acero
        destino.RefH.Capas      = patron.RefH.Capas
        destino.RefH.Separacion = patron.RefH.Separacion
        destino.RefH.Acero_T    = patron.RefH.Acero_T
    End Sub

End Class