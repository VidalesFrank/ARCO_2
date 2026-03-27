Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_01_Pilas
Public Class Form_01_00_PagInfoPilas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Form_01_PagPilas.ComboElementos.Items.Clear()

        Dim FT As Double = 1

        If Proyecto.Elementos.Pilas.Opcion_Elemento = "Punto" Then
            FT = 1
        End If

        For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count - 1

            Dim Label_Element As String : Label_Element = Tabla_Elementos.Rows(i).Cells(0).Value
            Dim Elemento = Proyecto.Elementos.Pilas.ListaElementos.Find(Function(p) p.Name_Label = Label_Element)

            Elemento.Name_Elemento = Tabla_Elementos.Rows(i).Cells(1).Value
            Elemento.Df = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(2).Value)
            Elemento.Dc = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(3).Value)
            Elemento.L_Pila = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(4).Value)
            Elemento.Opcion_Hueca = Tabla_Elementos.Rows(i).Cells(5).Value
            Elemento.Esp_Anillo = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(6).Value)
            Elemento.N_Barra_Long = Tabla_Elementos.Rows(i).Cells(7).Value
            Elemento.Acero_Long = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(8).Value)
            Elemento.Cant_Barras_Long = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(9).Value)
            Elemento.fc = Convert.ToSingle(Tabla_Elementos.Rows(i).Cells(10).Value)

            Elemento.N_Barra_Trans = Form_01_PagPilas.RefuerzoTransv.Text
            Elemento.Separacion_Trans = Convert.ToSingle(Form_01_PagPilas.Separacion.Text)

            If Elemento.Opcion_Hueca = "Si" Then
                Elemento.Ag_F = Math.PI * (Elemento.Df ^ 2 - (Elemento.Df - 2 * Elemento.Esp_Anillo) ^ 2) / 4
            Else
                Elemento.Ag_F = Math.PI * (Elemento.Df ^ 2) / 4
            End If

            Elemento.Ag_C = Math.PI * Elemento.Dc ^ 2 / 4
            Elemento.Cuantia = Math.Round(Elemento.Acero_Long * Elemento.Cant_Barras_Long / (Elemento.Ag_F * 1000000), 4)

            '------------------------ DEMANDAS EN ELEMENTOS ------------------------
            Dim FzMax As Single = 0
            FzMax = ObtenerFZMaximoPorElemento(Proyecto.Elementos.Pilas.Reactions, Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Servicio, Label_Element)
            Elemento.Ps_Estatica = FzMax
            FzMax = ObtenerFZMaximoPorElemento(Proyecto.Elementos.Pilas.Reactions, Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Servicio, Label_Element)
            Elemento.Ps_Dinamica = FzMax
            FzMax = ObtenerFZMaximoPorElemento(Proyecto.Elementos.Pilas.Reactions, Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Design, Label_Element)
            Elemento.Pu_Estatica = FzMax
            FzMax = ObtenerFZMaximoPorElemento(Proyecto.Elementos.Pilas.Reactions, Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Design, Label_Element)
            Elemento.Pu_Dinamica = FzMax
            FzMax = ObtenerFZMaximoPorElemento(Proyecto.Elementos.Pilas.Reactions, Proyecto.Elementos.Pilas.Lista_Combinaciones_Traccion, Label_Element, buscarMin:=True)
            Elemento.P_Traccion = FzMax

            Elemento.Matriz_PU = New List(Of Single)
            Elemento.Matriz_MU = New List(Of Single)
            Elemento.Matriz_V2 = New List(Of Single)
            Elemento.Matriz_V3 = New List(Of Single)
            Elemento.Matriz_Combinaciones = New List(Of String)

            Dim CombinacionesDesign As New List(Of String)
            CombinacionesDesign.AddRange(Proyecto.Elementos.Pilas.Lista_Combinaciones_Gravitacionales_Design)
            CombinacionesDesign.AddRange(Proyecto.Elementos.Pilas.Lista_Combinaciones_Sismicas_Design)

            Dim ReaccionesElemento = Proyecto.Elementos.Pilas.Reactions.Where(Function(r) r.JointLabel = Elemento.Name_Label AndAlso
                         CombinacionesDesign.Contains(r.LoadCase)).ToList()

            For Each r In ReaccionesElemento
                ' FZ → PU
                Elemento.Matriz_PU.Add(r.FZ)

                ' MU → mayor entre |MX| y |MY|
                Dim MU_Abs As Single = Math.Max(Math.Abs(r.MX), Math.Abs(r.MY))
                Elemento.Matriz_MU.Add(MU_Abs)

                ' FX → V2
                Elemento.Matriz_V2.Add(r.FX)

                ' FY → V3
                Elemento.Matriz_V3.Add(r.FY)

                Elemento.Matriz_Combinaciones.Add(r.LoadCase)
            Next

            '------------------------ VERIFICACIÓN DE ESFUERZOS EN EL CONCRETO ------------------------
            Elemento.Check1_PsE = 0.25 * Elemento.fc * Elemento.Ag_F * 1000 / (Elemento.Ps_Estatica)
            Elemento.Check2_PsD = 0.33 * Elemento.fc * Elemento.Ag_F * 1000 / (Elemento.Ps_Dinamica)
            Elemento.Check3_PuE = 0.35 * Elemento.fc * Elemento.Ag_F * 1000 / (Elemento.Pu_Estatica)
            Elemento.Check4_PuD = 0.35 * Elemento.fc * Elemento.Ag_F * 1000 / (Elemento.Pu_Dinamica)
            Elemento.Check5_PuT = 0.9 * Proyecto.Elementos.Pilas.Fy * Elemento.Acero_Long * Elemento.Cant_Barras_Long / (Elemento.P_Traccion * 1000)

            '------------------------ VERIFICACIÓN DE ESFUERZOS TRANSMITIDOS AL SUELO ------------------------
            Proyecto.Elementos.Pilas.Esf_Adm_Est = Convert.ToSingle(Form_01_PagPilas.EadmEst.Text)
            Proyecto.Elementos.Pilas.Esf_Adm_Din = Convert.ToSingle(Form_01_PagPilas.EadmDin.Text)

            Elemento.EsfE_Trans = Elemento.Ps_Estatica / Elemento.Ag_C
            Elemento.EsfD_Trans = Elemento.Ps_Dinamica / Elemento.Ag_C
            Elemento.Relacion_EsfE = Proyecto.Elementos.Pilas.Esf_Adm_Est / Elemento.EsfE_Trans
            Elemento.Relacion_EsfD = Proyecto.Elementos.Pilas.Esf_Adm_Din / Elemento.EsfD_Trans

            '------------------------ ANÁLISIS DE ESFUERZOS CORTANTES ----------------------------- 
            Dim RevisionV = ShearCheck(Elemento.Df, Elemento.fc, Proyecto.Elementos.Pilas.Fy, Elemento.Separacion_Trans, Elemento.N_Barra_Trans,
                                       Elemento.Matriz_V2, Elemento.Matriz_V3, Elemento.Matriz_PU, Elemento.Opcion_Hueca, Elemento.Esp_Anillo)
            Elemento.Vn = RevisionV(1, 7)
            Elemento.Vu = RevisionV(1, 5)
            Elemento.Check_V2 = RevisionV(1, 2)
            Elemento.Check_V3 = RevisionV(1, 3)
            Elemento.FactorShear = RevisionV(1, 4)

            '------------------------ CALCULAR EL DIAGRAMA DE INTERACCIÓN DE CADA SECCIÓN ---------------------
            Dim Diagrama_Interaccion = DiagramaInteraccionCircular(Elemento.Df, 0.075, Elemento.N_Barra_Long, Elemento.Acero_Long, Elemento.Cant_Barras_Long, Elemento.fc,
                                                                   Proyecto.Elementos.Pilas.ModuloE_Acero, Proyecto.Elementos.Pilas.Def_Uni_ConcAs, Proyecto.Elementos.Pilas.Fy)

            Elemento.Matriz_DI_Mn = New List(Of Single)
            Elemento.Matriz_DI_Pn = New List(Of Single)
            Elemento.Matriz_DI_PhiMn = New List(Of Single)
            Elemento.Matriz_DI_PhiPn = New List(Of Single)

            For j = 1 To Diagrama_Interaccion(1, 5)
                Elemento.Matriz_DI_Mn.Add(Convert.ToSingle(Diagrama_Interaccion(j, 2)))
                Elemento.Matriz_DI_Pn.Add(Convert.ToSingle(Diagrama_Interaccion(j, 1)))
                Elemento.Matriz_DI_PhiMn.Add(Convert.ToSingle(Diagrama_Interaccion(j, 4)))
                Elemento.Matriz_DI_PhiPn.Add(Convert.ToSingle(Diagrama_Interaccion(j, 3)))
            Next

            '-------------------------- ANÁLISIS A FLEXO-COMPRESIÓN DE LOS ELEMENTOS -----------------------
            Dim FF As Double = 100
            Dim F_Co As Double = 100
            For fi = 0 To Elemento.Matriz_PU.Count - 1
                Dim Pmax As Single = FT * Elemento.Matriz_PU(fi)
                Dim Mmax As Single = Elemento.Matriz_MU(fi)
                Dim RevServ = FDiagonal(Pmax, Mmax, Elemento.Matriz_DI_PhiPn, Elemento.Matriz_DI_PhiMn)
                If Math.Abs(FF) > Math.Abs(RevServ) Then
                    Elemento.Combinacion_Factor_Diagonal = Elemento.Matriz_Combinaciones(fi)
                    FF = RevServ
                End If
                If Math.Abs(Pmax) <= Elemento.Matriz_DI_PhiPn(0) Then
                    Dim RevCor = FCorte(Pmax, Mmax, Elemento.Matriz_DI_PhiPn, Elemento.Matriz_DI_PhiMn)
                    If Math.Abs(F_Co) > Math.Abs(RevCor) Then
                        Elemento.combinacion_Factor_CortesH = Elemento.Matriz_Combinaciones(fi)
                        F_Co = RevCor
                    End If
                Else
                    F_Co = 0
                End If
            Next
            Elemento.Factor_Diagonal = FF
            Elemento.Factor_CortesH = F_Co
            Form_01_PagPilas.Tabla_ResumenVisual.Rows.Add()
        Next

        For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count - 1
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(0).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Name_Elemento
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(1).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Ps_Estatica, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(2).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Ps_Dinamica, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(3).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Pu_Estatica, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(4).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Pu_Dinamica, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(5).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).P_Traccion, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(6).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check1_PsE, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(7).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check2_PsD, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(8).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check3_PuE, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(9).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check4_PuD, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(10).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check5_PuT, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(11).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).EsfE_Trans, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(12).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).EsfD_Trans, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(13).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Relacion_EsfE, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(14).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Relacion_EsfD, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(15).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Vn, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(16).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Vu, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(17).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).FactorShear, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(18).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Check_V2
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(19).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Check_V3
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(20).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Cuantia
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(21).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Factor_CortesH, 2)
            Form_01_PagPilas.TablaRevi.Rows(i).Cells(22).Value = Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Factor_Diagonal, 2)

            Form_01_PagPilas.ComboElementos.Items.Add(Proyecto.Elementos.Pilas.ListaElementos(i).Name_Elemento)

            For j = 6 To 22
                If j <> 11 And j <> 12 And j <> 15 And j <> 16 And j <> 18 And j <> 19 And j <> 20 Then
                    If Form_01_PagPilas.TablaRevi.Rows(i).Cells(j).Value >= 0.9 Then
                        Form_01_PagPilas.TablaRevi.Rows(i).Cells(j).Style.BackColor = Color.FromArgb(198, 239, 206)
                        Form_01_PagPilas.TablaRevi.Rows(i).Cells(j).Style.ForeColor = Color.FromArgb(0, 97, 0)
                    Else
                        Form_01_PagPilas.TablaRevi.Rows(i).Cells(j).Style.BackColor = Color.FromArgb(255, 199, 206)
                        Form_01_PagPilas.TablaRevi.Rows(i).Cells(j).Style.ForeColor = Color.FromArgb(156, 0, 6)
                    End If
                End If
            Next

            Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(0).Value = Proyecto.Elementos.Pilas.ListaElementos(i).Name_Elemento
            If Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check1_PsE, 2) >= 0.9 And Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check2_PsD, 2) >= 0.9 And Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check3_PuE, 2) >= 0.9 And Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Check4_PuD, 2) >= 0.9 Then
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(1).Value = "Ok"
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(1).Style.BackColor = Color.FromArgb(198, 239, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(1).Style.ForeColor = Color.FromArgb(0, 97, 0)
            Else
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(1).Style.BackColor = Color.FromArgb(255, 199, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(1).Style.ForeColor = Color.FromArgb(156, 0, 6)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(1).Value = "Revisar"
            End If
            If Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Relacion_EsfD, 2) >= 0.9 And Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Relacion_EsfE, 2) >= 0.9 Then
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(2).Value = "Ok"
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(2).Style.BackColor = Color.FromArgb(198, 239, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(2).Style.ForeColor = Color.FromArgb(0, 97, 0)
            Else
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(2).Style.BackColor = Color.FromArgb(255, 199, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(2).Style.ForeColor = Color.FromArgb(156, 0, 6)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(2).Value = "Revisar"
            End If
            If Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).FactorShear, 2) >= 0.9 Then
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(3).Value = "Ok"
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(3).Style.BackColor = Color.FromArgb(198, 239, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(3).Style.ForeColor = Color.FromArgb(0, 97, 0)
            Else
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(3).Style.BackColor = Color.FromArgb(255, 199, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(3).Style.ForeColor = Color.FromArgb(156, 0, 6)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(3).Value = "Revisar"
            End If
            If Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Factor_CortesH, 2) >= 0.9 And Math.Round(Proyecto.Elementos.Pilas.ListaElementos(i).Factor_Diagonal, 2) >= 0.9 Then
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(4).Value = "Ok"
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(4).Style.BackColor = Color.FromArgb(198, 239, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(4).Style.ForeColor = Color.FromArgb(0, 97, 0)
            Else
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(4).Value = "Revisar"
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(4).Style.BackColor = Color.FromArgb(255, 199, 206)
                Form_01_PagPilas.Tabla_ResumenVisual.Rows(i).Cells(4).Style.ForeColor = Color.FromArgb(156, 0, 6)
            End If
        Next

        Form_01_PagPilas.ComboElementos.Text = Proyecto.Elementos.Pilas.ListaElementos(0).Name_Elemento
        Me.WindowState = FormWindowState.Minimized
        Form_01_PagPilas.Tabla_ResumenVisual.Visible = True
        Form_01_PagPilas.Label20.Visible = True

        Form_01_PagPilas.ExportarToolStripMenuItem.Enabled = True
        MessageBox.Show("Análisis Finalizado con Éxito.", "Ejecución de Análisis", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Op_Seccion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Tabla_Elementos.CellValueChanged
        'If Tabla_Elementos.Rows.Count > 1 Then
        '    For i = 0 To Proyecto.Elementos.Pilas.ListaElementos.Count - 1
        '        Tabla_Elementos.Rows(i).Cells(8).Value = AreaRefuerzo(Tabla_Elementos.Rows(i).Cells(7).Value)
        '    Next
        'End If
    End Sub

    Private Sub Form_02_PagInfoPilas_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        Button1.Left = (Panel1.Width - Button1.Width) / 2
        Label1.Left = (Panel1.Width - Label1.Width) / 2
    End Sub

    Private Sub Tabla_Elementos_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Tabla_Elementos.CellContentClick

    End Sub
End Class