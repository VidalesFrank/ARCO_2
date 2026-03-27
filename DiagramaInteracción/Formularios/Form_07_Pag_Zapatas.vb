Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias
Imports DocumentFormat.OpenXml.Office.PowerPoint.Y2022.M03.Main

Public Class Form_07_Pag_Zapatas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Private Sub ImportarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ImportarDemandasToolStripMenuItem.Click


        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivo de resultados ETABS"
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*"
            .Multiselect = False

            If .ShowDialog() = DialogResult.OK Then
                Dim path As String = .FileName
                Me.Cursor = Cursors.WaitCursor

                Try
                    ' Leer cada hoja

                    Proyecto.Elementos.Zapatas.Tabla_JointReactions = LeerHojaExcel(path, "Joint Reactions")

                    MsgBox("Importación completada correctamente.", MsgBoxStyle.Information)

                    Proyecto.Elementos.Zapatas.Reactions = DataTableToReactions(Proyecto.Elementos.Zapatas.Tabla_JointReactions)

                    ' 🔹 Extraer combinaciones únicas
                    Proyecto.Elementos.Zapatas.Lista_Combinaciones = Proyecto.Elementos.Zapatas.Reactions.Select(Function(r) r.LoadCase) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

                    For Each Combinacion As String In Proyecto.Elementos.Zapatas.Lista_Combinaciones
                        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(Combinacion)
                    Next

                    Form_Opciones_Combinaciones.OpcionLlamado = "Zapatas"

                    Form_Opciones_Combinaciones.Evaluacion = "Estaticas"

                    Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones Análisis Estático"

                    Form_Opciones_Combinaciones.ShowDialog()

                    ' Este código ejecuta después de que el formulario inicial se cierra con OK
                    If Form_Opciones_Combinaciones.DialogResult = DialogResult.OK Then
                        ' Limpiar el ListBox de combinaciones
                        Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Clear()
                        Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()

                        ' Agregar las nuevas combinaciones sísmicas
                        For Each Combinacion As String In Proyecto.Elementos.Zapatas.Lista_Combinaciones
                            Form_Opciones_Combinaciones.Lista_Combinaciones.Items.Add(Combinacion)
                        Next

                        ' Cambiar los valores de OpcionLlamado y Evaluacion para combinaciones de diseño
                        Form_Opciones_Combinaciones.OpcionLlamado = "Zapatas"
                        Form_Opciones_Combinaciones.Evaluacion = "Dinamico"

                        Form_Opciones_Combinaciones.GroupBox2.Text = "Combinaciones Análisis Dinámico"
                        Form_Opciones_Combinaciones.Lista_Cargas_Design.Items.Clear()

                        ' Mostrar nuevamente el formulario con combinaciones sísmicas
                        Form_Opciones_Combinaciones.ShowDialog()

                    End If

                    MsgBox("Importación completada.", MsgBoxStyle.Information)

                Catch ex As Exception
                    MsgBox("Error al importar: " & ex.Message, MsgBoxStyle.Critical)
                Finally
                    Me.Cursor = Cursors.Arrow
                End Try
            End If
        End With

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim Lista_Elementos As New List(Of String)

        Lista_Elementos = Proyecto.Elementos.Zapatas.Reactions.Select(Function(r) r.JointLabel) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

        Tabla_Elementos.Rows.Clear()


        For Each Elemento_ In Lista_Elementos

            Dim Seccion As New cZapata

            Seccion.Nombre = Elemento_
            Seccion.Label_joint = Elemento_

            'Geometría Pedestal y Zapata
            Seccion.b = Convert.ToDouble(b_Pedestal.Text)
            Seccion.h = Convert.ToDouble(h_Pedestal.Text)
            Seccion.L_b = Convert.ToDouble(L1_Zapata.Text)
            Seccion.L_h = Convert.ToDouble(L2_Zapata.Text)
            Seccion.e = Convert.ToDouble(h_Zapata.Text)
            Seccion.rec = Convert.ToDouble(Recubrimiento.Text)
            Seccion.d = Seccion.e - Seccion.rec

            Dim r1 As New cRefuerzo With {
                            .Direccion = eDireccionRefuerzo.L2,
                            .Tipo = eTipoRefuerzo.Inferior,
                            .Diametro = Ref_L1.Text,
                            .Diametro_mm = 15.9,
                            .AreaBarra = Convert.ToDouble(As_L1.Text),
                            .Cantidad = Convert.ToDouble(NumRef_L1.Text),
                            .Espaciamiento = (Seccion.L_b - 2 * Seccion.rec) / .Cantidad
                        }

            Dim r2 As New cRefuerzo With {
                            .Direccion = eDireccionRefuerzo.L1,
                            .Tipo = eTipoRefuerzo.Inferior,
                            .Diametro = Ref_L2.Text,
                            .Diametro_mm = 15.9,
                            .AreaBarra = Convert.ToDouble(As_L2.Text),
                            .Cantidad = Convert.ToDouble(NumRef_L2.Text),
                            .Espaciamiento = (Seccion.L_h - 2 * Seccion.rec) / .Cantidad
                        }

            Seccion.Refuerzos.Add(r1)
            Seccion.Refuerzos.Add(r2)

            Seccion.Rho_L1 = r2.AsTotal / (Seccion.L_b * Seccion.d * 1000000)
            Seccion.Rho_L2 = r1.AsTotal / (Seccion.L_h * Seccion.d * 1000000)

            'Materiales
            Seccion.fc = Convert.ToDouble(T_fc.Text)
            Seccion.fy = Convert.ToDouble(T_fy.Text)

            'Capacidad Suelo
            Seccion.qAdm_Est = Convert.ToDouble(EadmEst.Text)
            Seccion.qAdm_Din = Convert.ToDouble(EadmDin.Text)
            Seccion.gammaSuelo = 18

            Seccion.FD_E = Convert.ToDouble(FD_E.Text)
            Seccion.FD_D = Convert.ToDouble(FD_D.Text)

            Proyecto.Elementos.Zapatas.Tipos.Add(Seccion)

            Tabla_Elementos.Rows.Add(Seccion.Label_joint,
                                    Seccion.Nombre,
                                    Seccion.b,
                                    Seccion.h,
                                    Seccion.e,
                                    Seccion.L_b,
                                    Seccion.L_h,
                                    r1.Diametro, r1.AreaBarra, r1.Cantidad,
                                    r2.Diametro, r2.AreaBarra, r2.Cantidad,
                                    Seccion.fc)

        Next


    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        TablaResultados.Rows.Clear()
        Tabla_Reporte.Rows.Clear()

        For i = 0 To Proyecto.Elementos.Zapatas.Tipos.Count - 1

            Dim Label_Element As String : Label_Element = Tabla_Elementos.Rows(i).Cells(0).Value

            Dim Elemento = Proyecto.Elementos.Zapatas.Tipos.Find(Function(p) p.Label_joint = Label_Element)

            Elemento.Nombre = Tabla_Elementos.Rows(i).Cells(1).Value
            Elemento.b = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(2).Value)
            Elemento.h = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(3).Value)
            Elemento.e = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(4).Value)
            Elemento.d = Elemento.e - 0.075
            Elemento.L_b = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(5).Value)
            Elemento.L_h = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(6).Value)
            Elemento.fc = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(13).Value)

            Elemento.Refuerzos.Clear()

            Dim Num_Barra_L2 As String = Tabla_Elementos.Rows(i).Cells(7).Value
            Dim As_L2 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(8).Value)
            Dim Cant_Ref_L2 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(9).Value)
            Dim Sep_Ref_L2 As Double = (Elemento.L_b - 2 * Elemento.rec) / Cant_Ref_L2

            Dim r1 As New cRefuerzo With {
                .Direccion = eDireccionRefuerzo.L2,
                .Tipo = eTipoRefuerzo.Inferior,
                .Diametro = Num_Barra_L2,
                .Diametro_mm = 15.9,
                .AreaBarra = As_L2,
                .Cantidad = Cant_Ref_L2,
                .Espaciamiento = Sep_Ref_L2
            }

            Dim Num_Barra_L1 As String = Tabla_Elementos.Rows(i).Cells(10).Value
            Dim As_L1 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(11).Value)
            Dim Cant_Ref_L1 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(12).Value)
            Dim Sep_Ref_L1 As Double = (Elemento.L_h - 2 * Elemento.rec) / Cant_Ref_L1

            Dim r2 As New cRefuerzo With {
                            .Direccion = eDireccionRefuerzo.L1,
                            .Tipo = eTipoRefuerzo.Inferior,
                            .Diametro = Num_Barra_L1,
                            .Diametro_mm = 15.9,
                            .AreaBarra = As_L1,
                            .Cantidad = Cant_Ref_L1,
                            .Espaciamiento = Sep_Ref_L1
                        }

            Elemento.Refuerzos.Add(r1)
            Elemento.Refuerzos.Add(r2)

            Dim combosValidos_Estatica = Proyecto.Elementos.Zapatas.Reactions.
    Where(Function(r) Proyecto.Elementos.Zapatas.Lista_Combinaciones_Estaticas.Contains(r.LoadCase) _
                   AndAlso r.JointLabel = Label_Element).ToList()

            Dim combosValidos_Dinamica = Proyecto.Elementos.Zapatas.Reactions.
    Where(Function(r) Proyecto.Elementos.Zapatas.Lista_Combinaciones_Dinamicas.Contains(r.LoadCase) _
                   AndAlso r.JointLabel = Label_Element).ToList()


            Elemento.Lista_Combinaciones_Estaticas = combosValidos_Estatica
            Elemento.Lista_Combinaciones_Dinamicas = combosValidos_Dinamica

            Elemento.Resultados.Clear()

            Dim g_Esf_max As Double = Double.MinValue
            Dim g_adm As Double
            Dim F_Esf As Double = Double.MaxValue
            Dim Check As Boolean = True

            Dim g_Esf_max_Din As Double = Double.MinValue
            Dim g_adm_Din As Double
            Dim F_Esf_Din As Double = Double.MaxValue
            Dim Check_Din As Boolean = True

            Dim Vu_Punzonamiento_max As Double = Double.MinValue
            Dim Vc_Punzonamiento As Double = Double.MaxValue
            Dim check_Punzonamiento As Boolean
            Dim F_Punzonamiento As Double = Double.MaxValue

            Dim Vu_Cortante_max As Double = Double.MinValue
            Dim Vc_Cortante As Double
            Dim Check_Cortante As Boolean
            Dim F_Cortante As Double = Double.MaxValue


            Dim M_Max As Double = Double.MinValue
            Dim Rho_Req As Double
            Dim Rho_Col As Double
            Dim F_Momento As Double = Double.MaxValue
            Dim Check_Momento As Boolean = True

            For Each comb In combosValidos_Estatica

                Dim P As Double = comb.FZ
                Dim Mx As Double = comb.MX
                Dim My As Double = comb.MY
                Dim Op_Comb As String = "EST"

                Dim res As ResultadoZapata = Funciones_Zapatas.EvaluarZapata(Elemento, P, Mx, My, Op_Comb)

                Elemento.Resultados(comb.LoadCase) = res

                Dim Cumple_cortante As Boolean = res.CumpleCortante_1 And res.CumpleCortante_2 And res.CumpleCortante_3 And res.CumpleCortante_4
                Dim Cumple_Flexion As Boolean = res.Cumple_L1 And res.Cumple_L2

                TablaResultados.Rows.Add(Elemento.Nombre,
                                            Elemento.L_b,
                                            Elemento.L_h,
                                            comb.LoadCase, comb.FZ, comb.MX, comb.MY,
                                                Math.Round(res.g1, 2),
                                                Math.Round(res.g2, 2),
                                                Math.Round(res.g3, 2),
                                                Math.Round(res.g4, 2),
                                                Math.Round(res.g5, 2),
                                                Math.Round(res.g6, 2),
                                                Math.Round(res.g7, 2),
                                                Math.Round(res.g8, 2),
                                                If(res.CumpleCapacidad, "Cumple", "No cumple"),
                                                If(res.CumplePunzonamiento, "Cumple", "No cumple"),
                                                If(Cumple_cortante, "Cumple", "No cumple"),
                                                If(Cumple_Flexion, "Cumple", "No cumple"))

                Dim fila_res As DataGridViewRow = TablaResultados.Rows(TablaResultados.Rows.Count - 2)

                Dim columnasCumple_report() As Integer = {15, 16, 17, 18}

                For Each col As Integer In columnasCumple_report

                    Dim valor As String = fila_res.Cells(col).Value.ToString()

                    If valor = "Cumple" Then
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                    Else
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                    End If

                    ' Opcional: mejorar presentación
                    fila_res.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                    fila_res.Cells(col).Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
                Next

                Dim valores() As Double = {res.g1, res.g2, res.g3, res.g4}
                Dim maxValor As Double = valores.Max()

                g_Esf_max = Math.Max(g_Esf_max, res.qMax)
                If (Elemento.qAdm_Est / g_Esf_max) < F_Esf Then
                    g_adm = Elemento.qAdm_Est
                    F_Esf = Elemento.qAdm_Est / g_Esf_max
                End If

                If (res.Vc_p / res.Vu_p) < F_Punzonamiento Then
                    Vu_Punzonamiento_max = Math.Max(Vu_Punzonamiento_max, res.Vu_p)
                    Vc_Punzonamiento = Math.Min(Vc_Punzonamiento, res.Vc_p)
                    check_Punzonamiento = res.CumplePunzonamiento
                End If

                Dim Vu_C_max As Double = Math.Max(res.Vu1_C, res.Vu3_C)
                If (res.Vc2_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc2_C
                    Check_Cortante = res.CumpleCortante_1 And res.CumpleCortante_3
                    F_Cortante = res.Vc2_C / Vu_C_max
                End If

                Vu_C_max = Math.Max(res.Vu2_C, res.Vu4_C)
                If (res.Vc1_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc1_C
                    Check_Cortante = res.CumpleCortante_2 And res.CumpleCortante_4
                    F_Cortante = res.Vc1_C / Vu_C_max
                End If

                Dim M_max_Dem As Double = Math.Max(res.Mu_1, res.Mu_2)

                Dim F1_M As Double = Elemento.Rho_L1 / res.Rho_1
                Dim F2_M As Double = Elemento.Rho_L2 / res.Rho_2

                If F_Momento > Math.Min(F1_M, F2_M) Then
                    F_Momento = Math.Min(F1_M, F2_M)
                    If F1_M < F2_M Then
                        M_Max = res.Mu_1
                        Rho_Req = res.Rho_1
                        Rho_Col = Elemento.Rho_L1
                    Else
                        M_Max = res.Mu_2
                        Rho_Req = res.Rho_2
                        Rho_Col = Elemento.Rho_L2
                    End If
                End If

            Next

            For Each comb In combosValidos_Dinamica

                Dim P As Double = comb.FZ
                Dim Mx As Double = comb.MX
                Dim My As Double = comb.MY
                Dim Op_Comb As String = "DIN"

                Dim res As ResultadoZapata = Funciones_Zapatas.EvaluarZapata(Elemento, P, Mx, My, Op_Comb)

                Elemento.Resultados(comb.LoadCase) = res

                Dim Cumple_cortante As Boolean = res.CumpleCortante_1 And res.CumpleCortante_2 And res.CumpleCortante_3 And res.CumpleCortante_4
                Dim Cumple_Flexion As Boolean = res.Cumple_L1 And res.Cumple_L2

                TablaResultados.Rows.Add(Elemento.Nombre,
                            Elemento.L_b,
                            Elemento.L_h,
                            comb.LoadCase, comb.FZ, comb.MX, comb.MY,
                                                Math.Round(res.g1, 2),
                                                Math.Round(res.g2, 2),
                                                Math.Round(res.g3, 2),
                                                Math.Round(res.g4, 2),
                                                Math.Round(res.g5, 2),
                                                Math.Round(res.g6, 2),
                                                Math.Round(res.g7, 2),
                                                Math.Round(res.g8, 2),
                                                If(res.CumpleCapacidad, "Cumple", "No cumple"),
                                                If(res.CumplePunzonamiento, "Cumple", "No cumple"),
                                                If(Cumple_cortante, "Cumple", "No cumple"),
                                                If(Cumple_Flexion, "Cumple", "No cumple"))

                Dim fila_res As DataGridViewRow = TablaResultados.Rows(TablaResultados.Rows.Count - 2)

                Dim columnasCumple_report() As Integer = {15, 16, 17, 18}

                For Each col As Integer In columnasCumple_report

                    Dim valor As String = fila_res.Cells(col).Value.ToString()

                    If valor = "Cumple" Then
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                    Else
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                    End If

                    ' Opcional: mejorar presentación
                    fila_res.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                    fila_res.Cells(col).Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
                Next


                Dim valores() As Double = {res.g1, res.g2, res.g3, res.g4}
                Dim maxValor As Double = valores.Max()

                g_Esf_max_Din = Math.Max(g_Esf_max_Din, res.qMax)
                If (Elemento.qAdm_Din / g_Esf_max_Din) < F_Esf_Din Then
                    g_adm_Din = Elemento.qAdm_Din
                    F_Esf_Din = Elemento.qAdm_Din / g_Esf_max_Din
                End If

                If (res.Vc_p / res.Vu_p) < F_Punzonamiento Then
                    Vu_Punzonamiento_max = Math.Max(Vu_Punzonamiento_max, res.Vu_p)
                    Vc_Punzonamiento = Math.Min(Vc_Punzonamiento, res.Vc_p)
                    check_Punzonamiento = res.CumplePunzonamiento
                End If

                Dim Vu_C_max As Double = Math.Max(res.Vu1_C, res.Vu3_C)
                If (res.Vc2_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc2_C
                    Check_Cortante = res.CumpleCortante_1 And res.CumpleCortante_3
                    F_Cortante = res.Vc2_C / Vu_C_max
                End If

                Vu_C_max = Math.Max(res.Vu2_C, res.Vu4_C)
                If (res.Vc1_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc1_C
                    Check_Cortante = res.CumpleCortante_2 And res.CumpleCortante_4
                    F_Cortante = res.Vc1_C / Vu_C_max
                End If

                Dim M_max_Dem As Double = Math.Max(res.Mu_1, res.Mu_2)

                Dim F1_M As Double = Elemento.Rho_L1 / res.Rho_1
                Dim F2_M As Double = Elemento.Rho_L2 / res.Rho_2

                If F_Momento > Math.Min(F1_M, F2_M) Then
                    F_Momento = Math.Min(F1_M, F2_M)
                    If F1_M < F2_M Then
                        M_Max = res.Mu_1
                        Rho_Req = res.Rho_1
                        Rho_Col = Elemento.Rho_L1
                    Else
                        M_Max = res.Mu_2
                        Rho_Req = res.Rho_2
                        Rho_Col = Elemento.Rho_L2
                    End If
                End If

            Next

            Tabla_Reporte.Rows.Add(Elemento.Nombre,
            Elemento.L_b,
            Elemento.L_h,
            Elemento.e,
            Math.Round(g_Esf_max, 2),
            If((g_Esf_max * 0.9 < g_adm), "Cumple", "No cumple"),
            Math.Round(g_Esf_max_Din, 2),
            If((g_Esf_max_Din * 0.9 < g_adm_Din), "Cumple", "No cumple"),
            Math.Round(Vu_Punzonamiento_max, 2),
            Math.Round(Vc_Punzonamiento, 2),
            If(check_Punzonamiento, "Cumple", "No cumple"),
            Math.Round(Vc_Cortante, 2),
            Math.Round(Vu_Cortante_max, 2),
            If(Check_Cortante, "Cumple", "No cumple"),
            Math.Round(Rho_Req, 5),
            Math.Round(Rho_Col, 5),
            If((F_Momento > 0.9), "Cumple", "No cumple"))

            Dim fila As DataGridViewRow = Tabla_Reporte.Rows(Tabla_Reporte.Rows.Count - 2)

            Dim columnasCumple() As Integer = {5, 7, 10, 13, 16}

            For Each col As Integer In columnasCumple

                Dim valor As String = fila.Cells(col).Value.ToString()

                If valor = "Cumple" Then
                    fila.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                    fila.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                Else
                    fila.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                    fila.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                End If

                ' Opcional: mejorar presentación
                fila.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                fila.Cells(col).Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
            Next

        Next





    End Sub

    Private Sub Ref_L1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_L1.SelectedIndexChanged
        Try
            If Ref_L1.Text <> "Usuario" Then
                As_L1.Text = AreaRefuerzo(Ref_L1.Text)
            Else
                As_L1.Text = 199
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Ref_L2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_L2.SelectedIndexChanged
        Try
            If Ref_L2.Text <> "Usuario" Then
                As_L2.Text = AreaRefuerzo(Ref_L2.Text)
            Else
                As_L2.Text = 199
            End If
        Catch ex As Exception

        End Try
    End Sub
End Class