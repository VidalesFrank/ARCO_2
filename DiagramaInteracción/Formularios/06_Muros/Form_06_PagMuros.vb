Imports System.IO
Imports System.Data.OleDb
Imports System.Drawing
Imports Microsoft.ReportingServices.Rendering.WordRenderer.WordOpenXmlRenderer.Parser.dc.elements
Imports ARCO.ReporteInicial
Imports System.Windows.Forms.DataVisualization.Charting
Imports Func_Muros = ARCO.Funciones_Muros
'Imports System.Windows.Media

Public Class Form_06_PagMuros

    Public Shared proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public Shared Muro As New Muro

    Private Rectangulos As New List(Of Rectangulo)
    Private MaxCoorX As Single = 0
    Private MinCoorX As Single = 0
    Private MaxCoorY As Single = 0
    Private MinCoorY As Single = 0

    'Private WithEvents Reporte As ReporteInicial

    Public Class Rectangulo
        Public Property Name As String
        Public Property Direccion As eNumeradores.eDireccion
        Public Property Tipo_M As eNumeradores.eTipoMuro
        Public Property CoorX As Single
        Public Property CoorY As Single
        Public Property Largo As Single
        Public Property Espesor As Single
    End Class

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Cursor = Cursors.WaitCursor

        'Try
        Dim Tabla As DataGridView
        Dim Col_Diseno = Funciones_02_Columnas.Columnas_Diseno("Pier")
        Dim Col_Secciones = Funciones_02_Columnas.Columnas_Secciones("Pier")
        Dim Col_Fuerzas = Funciones_02_Columnas.Columnas_Fuerzas("Pier")

        If proyecto.Muros.Info_Diseño = True Then
            Tabla = Tabla_Diseño_Flexo

            Dim Col_Piso As Integer = 0
            Dim Col_Label As Integer = 1
            Dim Col_Seccion As Integer = 1
            Dim Salto As Integer = 2
            Dim Col_As_Req As Integer = 9
            Dim Col_Ash_Req As Integer = 15
            Dim Col_Esf_I As Integer = 16
            Dim Col_Esf_D As Integer = 17
            Dim Col_C_I As Integer = 20
            Dim Col_C_D As Integer = 22
            Dim Col_LEB_I As Integer = 24
            Dim Col_LEB_D As Integer = 25

            For i = 0 To 25
                Dim C As String = Tabla.Rows(0).Cells(i).Value.ToString
                If C.Contains("Required") Then
                    Col_As_Req = i
                End If
                If C.Contains("Shear Rebar") Then
                    Col_Ash_Req = i
                    Col_Esf_I = i + 1
                    Col_Esf_D = i + 2
                    Col_C_I = i + 5
                    Col_C_D = i + 7
                    Col_LEB_I = i + 9
                    Col_LEB_D = i + 10
                End If
            Next

            Dim Lista(2, 6) As Single

            For i = 2 To Tabla.Rows.Count() - 1 Step Salto
                If Tabla.Rows(i).Cells(0).Value <> String.Empty Then

                    For j = 0 To 5
                        If IsDBNull(Tabla.Rows(i).Cells(Col_C_I + j).Value) Or IsNothing(Tabla.Rows(i).Cells(Col_C_I + j).Value) Then
                            Lista(1, j) = 0
                        Else
                            Lista(1, j) = Tabla.Rows(i).Cells(Col_C_I + j).Value
                        End If
                        If IsDBNull(Tabla.Rows(i + 1).Cells(Col_C_I + j).Value) Or IsNothing(Tabla.Rows(i + 1).Cells(Col_C_I + j).Value) Then
                            Lista(2, j) = 0
                        Else
                            Lista(2, j) = Tabla.Rows(i + 1).Cells(Col_C_I + j).Value
                        End If
                    Next

                    Dim Seccion As New SeccionMuro
                    With Seccion
                        .Name = Tabla.Rows(i).Cells(Col_Label).Value
                        .Piso = Tabla.Rows(i).Cells(Col_Piso).Value
                        .Seccion = Tabla.Rows(i).Cells(Col_Seccion).Value

                        'Asignación de la cuantia leida de la hoja de Shear Wall Pier Summary
                        .Cuantia_Top_Req = Convert.ToSingle(Tabla.Rows(i).Cells(Col_As_Req).Value)
                        .Cuantia_Bot_Req = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_As_Req).Value)

                        'Asignación de la cuantia volumetrica leida de la hoja de Shear Wall Pier Summary
                        .AsH_Req_Top = Convert.ToSingle(Tabla.Rows(i).Cells(Col_Ash_Req).Value)
                        .AsH_Req_Bot = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_Ash_Req).Value)

                        'Asignación de los esfuerzos del muro leido de la hoja de Shear Wall Pier Summary
                        .Esf_I_Top = Convert.ToSingle(Tabla.Rows(i).Cells(Col_Esf_I).Value)
                        .Esf_I_Bot = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_Esf_I).Value)
                        .Esf_D_Top = Convert.ToSingle(Tabla.Rows(i).Cells(Col_Esf_D).Value)
                        .Esf_D_Bot = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_Esf_D).Value)

                        'Asignación de la longitud del eje neutro del muro leido de la hoja de Shear Wall Pier Summary
                        .C_I_Top = Convert.ToSingle(Lista(1, 0) / 1000)
                        .C_I_Bot = Convert.ToSingle(Lista(2, 0) / 1000)
                        .C_D_Top = Convert.ToSingle(Lista(1, 2) / 1000)
                        .C_D_Bot = Convert.ToSingle(Lista(2, 2) / 1000)
                    End With

                    Muro.Lista_Secciones.Add(Seccion)

                End If
            Next

            For i = 0 To Muro.Lista_Secciones.Count - 1
                Dim Muro_ As New Muro
                Muro_.Name = Muro.Lista_Secciones(i).Name
                Muro_.Label = Muro.Lista_Secciones(i).Name
                Muro_.Lista_Secciones = Muro.Lista_Secciones.FindAll(Function(p) p.Name = Muro_.Name)

                If proyecto.Muros.Lista_Muros.Exists(Function(p) p.Name = Muro_.Name) Then
                Else
                    proyecto.Muros.Lista_Muros.Add(Muro_)
                    Combo_Elementos.Items.Add(Muro_.Name)
                End If
            Next

            Check_Design.Checked = True

        End If

        If proyecto.Muros.Info_Secciones = True Then
            Tabla = Tabla_secciones

            Dim Col_Piso As Integer = Col_Secciones(0)
            Dim Col_Name As Integer = Col_Secciones(1)
            Dim Col_Material As Integer = Col_Secciones(2)
            Dim Col_B As Integer = Col_Secciones(3)
            Dim Col_H As Integer = Col_Secciones(4)

            If proyecto.Muros.Lista_Muros.Count = 0 Then
                For i = 2 To Tabla.Rows.Count() - 1
                    Dim Seccion As New SeccionMuro
                    Seccion.Name = Tabla.Rows(i).Cells(Col_Name).Value
                    Seccion.Piso = Tabla.Rows(i).Cells(Col_Piso).Value

                    Muro.Lista_Secciones.Add(Seccion)
                Next

                For i = 0 To Muro.Lista_Secciones.Count - 1
                    'Dim Seccion = Muro.Lista_Secciones(i)
                    Dim Muro_ As New Muro
                    Muro_.Name = Muro.Lista_Secciones(i).Name
                    Muro_.Label = Muro.Lista_Secciones(i).Name
                    Muro_.Lista_Secciones = Muro.Lista_Secciones.FindAll(Function(p) p.Name = Muro_.Name)

                    If proyecto.Muros.Lista_Muros.Exists(Function(p) p.Name = Muro_.Name) Then
                    Else
                        If Muro_ IsNot Nothing AndAlso Not String.IsNullOrEmpty(Muro_.Name) Then
                            proyecto.Muros.Lista_Muros.Add(Muro_)
                            Combo_Elementos.Items.Add(Muro_.Name)
                        End If

                    End If

                Next

            End If

            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                Dim Elemento As String = proyecto.Muros.Lista_Muros(i).Name

                For Np = 0 To proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1
                    Dim Name As String = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Name
                    Dim Piso As String = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Piso

                    Dim rowIndex As Integer = Tabla.Rows.Cast(Of DataGridViewRow)().ToList().FindIndex(Function(row) If(row.Cells(0).Value?.ToString() = Piso AndAlso
                            row.Cells(1).Value?.ToString() = Name, True, False))

                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model = Math.Min(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(Col_B).Value), Convert.ToSingle(Tabla.Rows(rowIndex).Cells(Col_H).Value)) / 1000
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model = Math.Max(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(Col_B).Value), Convert.ToSingle(Tabla.Rows(rowIndex).Cells(Col_H).Value)) / 1000
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Planos = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Planos = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).fc = Convert.ToSingle(Mid(Tabla.Rows(rowIndex).Cells(Col_Material).Value, 1, 2))

                    If Convert.ToSingle(Tabla.Rows(rowIndex).Cells(2).Value) > 10 Then
                        proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Direccion_Muro = eNumeradores.eDireccion.Y
                        proyecto.Muros.Lista_Muros(i).Direccion = eNumeradores.eDireccion.Y
                    Else
                        proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Direccion_Muro = eNumeradores.eDireccion.X
                        proyecto.Muros.Lista_Muros(i).Direccion = eNumeradores.eDireccion.X
                    End If

                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Altura = Convert.ToSingle(Tabla.Rows(rowIndex).Cells(15).Value)
                    proyecto.Muros.Lista_Muros(i).Coor_X = Convert.ToSingle(Tabla.Rows(rowIndex).Cells(10).Value)
                    proyecto.Muros.Lista_Muros(i).Coor_Y = Convert.ToSingle(Tabla.Rows(rowIndex).Cells(11).Value)
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Coor_Z_Bot = Convert.ToSingle(Tabla.Rows(rowIndex).Cells(12).Value)
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Coor_Z_Top = Convert.ToSingle(Tabla.Rows(rowIndex).Cells(15).Value)

                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).As_Top_Req = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Cuantia_Top_Req * 1000000
                    proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).As_Bot_Req = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Cuantia_Bot_Req * 1000000

                Next

                Dim seccionMayorZ As SeccionMuro = proyecto.Muros.Lista_Muros(i).Lista_Secciones.OrderBy(Function(seccion) seccion.Coor_Z_Top).Last()

                proyecto.Muros.Lista_Muros(i).Hw = seccionMayorZ.Coor_Z_Top
                proyecto.Muros.Lista_Muros(i).tw = proyecto.Muros.Lista_Muros(i).Lista_Secciones(proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1).tw_Model
                proyecto.Muros.Lista_Muros(i).Lw = proyecto.Muros.Lista_Muros(i).Lista_Secciones(proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1).Lw_Model
                proyecto.Muros.Lista_Muros(i).Ar = proyecto.Muros.Lista_Muros(i).Hw / proyecto.Muros.Lista_Muros(i).Lw
            Next

            Check_Secciones.Checked = True
        End If

        If proyecto.Muros.Info_Fuerzas = True Then
            Tabla = Tabla_Fuerzas

            Dim Col_Piso As Integer = Col_Fuerzas(0)
            Dim Col_Label As Integer = Col_Fuerzas(1)
            Dim Col_Combinacion As Integer = Col_Fuerzas(2)
            Dim Salto As Integer = Col_Fuerzas(3)
            Dim Col_P As Integer = Col_Fuerzas(4)
            Dim Col_V2 As Integer = Col_Fuerzas(5)
            Dim Col_V3 As Integer = Col_Fuerzas(6)
            Dim Col_T As Integer = Col_Fuerzas(7)
            Dim Col_M2 As Integer = Col_Fuerzas(8)
            Dim Col_M3 As Integer = Col_Fuerzas(9)

            proyecto.Muros.Lista_Combinaciones_Muros.Add(Tabla.Rows(2).Cells(Col_Combinacion).Value.ToString)

            Dim valoresDistintos As List(Of String) = Tabla.Rows.OfType(Of DataGridViewRow)().Skip(1) _
                    .Select(Function(row) If(row.Cells(Col_Combinacion).Value IsNot Nothing, row.Cells(Col_Combinacion).Value.ToString(), Nothing)) _
                    .Where(Function(valor) Not String.IsNullOrEmpty(valor)).Distinct().ToList()

            'For i = 0 To valoresDistintos.Count() - 1
            '    Dim Comb As String = valoresDistintos(i)
            '    If Not proyecto.Muros.Lista_Combinaciones_Muros.Exists(Function(p) p = Comb) Then
            '        proyecto.Muros.Lista_Combinaciones_Muros.Add(valoresDistintos(i))
            '    End If
            'Next

            For Each Comb As String In valoresDistintos.Except(proyecto.Muros.Lista_Combinaciones_Muros)
                proyecto.Muros.Lista_Combinaciones_Muros.Add(Comb)
            Next

            'For Each Elemento As String In proyecto.Muros.Lista_Muros.Select(Function(m) m.Name)
            '    For Each Piso As String In proyecto.Muros.Lista_Muros.SelectMany(Function(m) m.Lista_Secciones.Select(Function(s) s.Piso))
            '        For Each Combinacion As String In proyecto.Muros.Lista_Combinaciones_Muros

            '            Dim rowIndex As Integer = Tabla.Rows.Cast(Of DataGridViewRow)().ToList().FindIndex(Function(row) If(row.Cells(eNumeradores.ColumnaFuerzas.Piso).Value?.ToString() = Piso AndAlso
            '               row.Cells(eNumeradores.ColumnaFuerzas.Label).Value?.ToString() = Elemento AndAlso row.Cells(eNumeradores.ColumnaFuerzas.Combinacion).Value?.ToString() = Combinacion, True, False))

            '            If rowIndex <> -1 Then
            '                Dim Fuerza As New SeccionMuro.Fuerzas_Elementos With {
            '                        .Name = Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.Combinacion).Value.ToString,
            '                        .P = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.P).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.P).Value))),
            '                        .V2 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.V2).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.V2).Value))),
            '                        .V3 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.V3).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.V3).Value))),
            '                        .T = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.T).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.T).Value))),
            '                        .M2 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.M2).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.M2).Value))),
            '                        .M3 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.M3).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.M3).Value)))
            '                }

            '                Dim Seccion = proyecto.Muros.Lista_Muros.FirstOrDefault(Function(m) m.Name = Elemento)?.Lista_Secciones.FirstOrDefault(Function(s) s.Piso = Piso)
            '                If Seccion IsNot Nothing Then
            '                    Seccion.Lista_Combinaciones.Add(Fuerza)
            '                End If
            '            End If

            '            Console.WriteLine($"Elemento: {Elemento}, Piso: {Piso}, Combinacion: {Combinacion}")

            '        Next
            '    Next
            'Next
            'Dim diccionarioFilas As New Dictionary(Of String, Dictionary(Of String, DataGridViewRow))

            'For Each fila As DataGridViewRow In Tabla.Rows
            '    Dim combinacion As String = fila.Cells(eNumeradores.ColumnaFuerzas.Combinacion).Value?.ToString()
            '    Dim elemento As String = fila.Cells(eNumeradores.ColumnaFuerzas.Label).Value?.ToString()

            '    If Not diccionarioFilas.ContainsKey(combinacion) Then
            '        diccionarioFilas(combinacion) = New Dictionary(Of String, DataGridViewRow)()
            '    End If

            '    diccionarioFilas(combinacion)(elemento) = fila
            '    Console.WriteLine(fila)

            'Next

            'For Each Combinacion As String In proyecto.Muros.Lista_Combinaciones_Muros
            '    For Each Elemento As String In proyecto.Muros.Lista_Muros.Select(Function(m) m.Name)
            '        ' Verificar si existe la combinación y el elemento en el diccionario
            '        If diccionarioFilas.ContainsKey(Combinacion) AndAlso diccionarioFilas(Combinacion).ContainsKey(Elemento) Then
            '            ' Obtener la fila directamente del diccionario
            '            Dim fila As DataGridViewRow = diccionarioFilas(Combinacion)(Elemento)
            '            Dim piso As String = fila.Cells(eNumeradores.ColumnaFuerzas.Piso).Value?.ToString()

            '            ' Resto del código permanece igual
            '            Dim Fuerza As New SeccionMuro.Fuerzas_Elementos With {
            '                    .Name = fila.Cells(eNumeradores.ColumnaFuerzas.Combinacion).Value.ToString,
            '                    .P = Math.Max(Math.Abs(Convert.ToSingle(fila.Cells(eNumeradores.ColumnaFuerzas.P).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(fila.Index + 1).Cells(eNumeradores.ColumnaFuerzas.P).Value))),
            '                    .V2 = Math.Max(Math.Abs(Convert.ToSingle(fila.Cells(eNumeradores.ColumnaFuerzas.V2).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(fila.Index + 1).Cells(eNumeradores.ColumnaFuerzas.V2).Value))),
            '                    .V3 = Math.Max(Math.Abs(Convert.ToSingle(fila.Cells(eNumeradores.ColumnaFuerzas.V3).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(fila.Index + 1).Cells(eNumeradores.ColumnaFuerzas.V3).Value))),
            '                    .T = Math.Max(Math.Abs(Convert.ToSingle(fila.Cells(eNumeradores.ColumnaFuerzas.T).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(fila.Index + 1).Cells(eNumeradores.ColumnaFuerzas.T).Value))),
            '                    .M2 = Math.Max(Math.Abs(Convert.ToSingle(fila.Cells(eNumeradores.ColumnaFuerzas.M2).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(fila.Index + 1).Cells(eNumeradores.ColumnaFuerzas.M2).Value))),
            '                    .M3 = Math.Max(Math.Abs(Convert.ToSingle(fila.Cells(eNumeradores.ColumnaFuerzas.M3).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(fila.Index + 1).Cells(eNumeradores.ColumnaFuerzas.M3).Value)))
            '                }

            '            Dim Seccion = proyecto.Muros.Lista_Muros.FirstOrDefault(Function(m) m.Name = Elemento)?.Lista_Secciones.FirstOrDefault(Function(s) s.Piso = Piso)

            '            If Seccion IsNot Nothing Then
            '                Seccion.Lista_Combinaciones.Add(Fuerza)
            '            End If
            '        End If
            '    Next
            'Next


            For Each Elemento As String In proyecto.Muros.Lista_Muros.Select(Function(m) m.Name)
                For Each Piso As String In proyecto.Muros.Lista_Muros.SelectMany(Function(m) m.Lista_Secciones.Select(Function(s) s.Piso)).Distinct()
                    For Each Combinacion As String In proyecto.Muros.Lista_Combinaciones_Muros

                        Dim rowIndex As Integer = Tabla.Rows.Cast(Of DataGridViewRow)().ToList().FindIndex(Function(row) If(row.Cells(eNumeradores.ColumnaFuerzas.Piso).Value?.ToString() = Piso AndAlso
                           row.Cells(eNumeradores.ColumnaFuerzas.Label).Value?.ToString() = Elemento AndAlso row.Cells(eNumeradores.ColumnaFuerzas.Combinacion).Value?.ToString() = Combinacion, True, False))

                        If rowIndex <> -1 Then
                            Dim Fuerza As New SeccionMuro.Fuerzas_Elementos With {
                                    .Name = Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.Combinacion).Value.ToString,
                                    .P = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.P).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.P).Value))),
                                    .V2 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.V2).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.V2).Value))),
                                    .V3 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.V3).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.V3).Value))),
                                    .T = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.T).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.T).Value))),
                                    .M2 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.M2).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex + 1).Cells(eNumeradores.ColumnaFuerzas.M2).Value))),
                                    .M3 = Math.Max(Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.M3).Value)), Math.Abs(Convert.ToSingle(Tabla.Rows(rowIndex).Cells(eNumeradores.ColumnaFuerzas.M3).Value)))
                            }

                            Dim Seccion = proyecto.Muros.Lista_Muros.FirstOrDefault(Function(m) m.Name = Elemento)?.Lista_Secciones.FirstOrDefault(Function(s) s.Piso = Piso)
                            If Seccion IsNot Nothing Then
                                Seccion.Lista_Combinaciones.Add(Fuerza)

                                Dim combinacionesFiltradas = Seccion.Lista_Combinaciones.Where(Function(c) c.Name.ToUpper().Contains("ENV"))

                                If combinacionesFiltradas.Any() Then
                                    Seccion.Vu = combinacionesFiltradas.Max(Function(c) Math.Abs(c.V2))
                                End If

                            End If
                        End If
                    Next
                Next
            Next

            Check_Fuerzas.Checked = True

        End If

        'For Each combinacion In proyecto.Muros.Lista_Combinaciones_Muros
        '    Form_Combinaciones.Combo_Combinaciones.Items.Add(combinacion.ToString)
        'Next

        'If proyecto.Muros.Lista_Combinaciones_Muros.Count > 0 Then
        '    Form_Combinaciones.Combo_Combinaciones.Text = proyecto.Muros.Lista_Combinaciones_Muros(0).ToString
        '    proyecto.Muros.Lista_Combinaciones_ALR_Muros.ForEach(Sub(item) Form_Combinaciones.Tabla_combinaciones.Rows.Add(item))
        '    If proyecto.Muros.Lista_Combinaciones_ALR_Muros.Count > 0 Then
        '        Form_Combinaciones.Tabla_combinaciones.Rows.AddRange(proyecto.Muros.Lista_Combinaciones_ALR_Muros.ToArray())
        '    End If
        'End If

        If proyecto.Muros.Info_Diseño = True Then
            Combo_Elementos.Text = proyecto.Muros.Lista_Muros(0).Name
        End If

        'Catch ex As Exception
        '    MessageBox.Show("Error al leer la información.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Finally
        Obtencion_Macroparametros()
        Cursor = Cursors.Arrow
        MessageBox.Show("Información Cargada con Éxito.")
        'End Try

    End Sub

    Private Sub LlenarTabla_Diseno_Muros()
        proyecto.Muros.Info_Diseño = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Form_02_PagColumnas.Importar_Datos_de_Excel(.FileName, Tabla_Diseño_Flexo, "Diseño", "Pier")
            End If
        End With
    End Sub

    Private Sub LlenarTabla_Secciones()
        proyecto.Muros.Info_Secciones = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Form_02_PagColumnas.Importar_Datos_de_Excel(.FileName, Tabla_secciones, "Secciones", "Pier")
            End If
        End With
    End Sub

    Private Sub LlenarTabla_Fuerzas()
        proyecto.Muros.Info_Fuerzas = True
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Form_02_PagColumnas.Importar_Datos_de_Excel(.FileName, Tabla_Fuerzas, "Fuerzas", "Pier")
            End If
        End With
    End Sub

    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged
        Try
            Tabla_Resumen.Rows.Clear()
            Tabla_Resumen.Columns.Clear()
            If proyecto.Muros.Info_Diseño = True Then
                Tabla_Resumen.Columns.Add("Column1", "Elemento")
                Tabla_Resumen.Columns.Add("Column2", "Piso")
                Tabla_Resumen.Columns.Add("Column3", "Sección")
                Tabla_Resumen.Columns.Add("Column4", "Estación")
                Tabla_Resumen.Columns.Add("Column5", "Cuantia Requerida (%)")
            End If
            Dim Elemento As String = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Name
            Dim Seccion = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Combo_Elementos.Text).Lista_Secciones
            For i = 0 To (Seccion.Count - 1) * 2
                Tabla_Resumen.Rows.Add()
            Next


            If proyecto.Muros.Info_Fuerzas = True Then
                Tabla_Resumen.Columns.Add("Column6", "Vu (kN)")
            End If
            If proyecto.Muros.Info_Secciones = True Then
                Tabla_Resumen.Columns.Add("Column7", "tw (m)")
                Tabla_Resumen.Columns.Add("Column8", "Lw (m)")
                Tabla_Resumen.Columns.Add("Column9", "f'c (MPa)")
            End If
            'If proyecto.Info_Diseño = True Then
            '    Tabla_Resumen.Columns.Add("Column10", "C-Izq (m)")
            '    Tabla_Resumen.Columns.Add("Column11", "C-Der (m)")
            '    Tabla_Resumen.Columns.Add("Column12", "L.EB-Izq (m)")
            '    Tabla_Resumen.Columns.Add("Column13", "L.EB-Der (m)")
            '    Tabla_Resumen.Columns.Add("Column14", "Esf-Izq (MPa)")
            '    Tabla_Resumen.Columns.Add("Column15", "Esf-Der (MPa)")
            'End If

            If proyecto.Muros.Info_Diseño = True Then
                Tabla_Resumen.Rows(0).Cells(0).Value = Elemento
                For i = 0 To (Seccion.Count - 1) * 2 Step 2
                    Dim N_Cont As Integer = 5
                    Tabla_Resumen.Rows(i).Cells(1).Value = Seccion(i / 2).Piso
                    Tabla_Resumen.Rows(i).Cells(2).Value = Seccion(i / 2).Seccion
                    Tabla_Resumen.Rows(i + 1).Cells(2).Value = Seccion(i / 2).Seccion
                    Tabla_Resumen.Rows(i).Cells(3).Value = "Top"
                    Tabla_Resumen.Rows(i + 1).Cells(3).Value = "Bottom"
                    Tabla_Resumen.Rows(i).Cells(4).Value = Format(Seccion(i / 2).Cuantia_Top_Req, "##,##0.00")
                    Tabla_Resumen.Rows(i + 1).Cells(4).Value = Format(Seccion(i / 2).Cuantia_Bot_Req, "##,##0.00")

                    If proyecto.Muros.Info_Fuerzas = True Then
                        Tabla_Resumen.Rows(i).Cells(5).Value = Math.Round(Seccion(i / 2).Vu, 2)
                        N_Cont += 1
                    End If
                    If proyecto.Muros.Info_Secciones = True Then
                        Tabla_Resumen.Rows(i).Cells(6).Value = Format(Seccion(i / 2).tw_Model, "##,##0.00")
                        Tabla_Resumen.Rows(i).Cells(7).Value = Format(Seccion(i / 2).Lw_Model, "##,##0.00")
                        Tabla_Resumen.Rows(i).Cells(8).Value = Seccion(i / 2).fc
                        N_Cont += 3
                    End If
                    'Tabla_Resumen.Rows(i).Cells(N_Cont).Value = Format(Seccion(i / 2).C_I_Top, "##,##0.00")
                    'Tabla_Resumen.Rows(i + 1).Cells(N_Cont).Value = Format(Seccion(i / 2).C_I_Bot, "##,##0.00")
                    'Tabla_Resumen.Rows(i).Cells(N_Cont + 1).Value = Format(Seccion(i / 2).C_D_Top, "##,##0.00")
                    'Tabla_Resumen.Rows(i + 1).Cells(N_Cont + 1).Value = Format(Seccion(i / 2).C_D_Bot, "##,##0.00")
                    'Tabla_Resumen.Rows(i).Cells(N_Cont + 2).Value = Format(Seccion(i / 2).LEB_I_Top, "##,##0.00")
                    'Tabla_Resumen.Rows(i + 1).Cells(N_Cont + 2).Value = Format(Seccion(i / 2).LEB_I_Bot, "##,##0.00")
                    'Tabla_Resumen.Rows(i).Cells(N_Cont + 3).Value = Format(Seccion(i / 2).LEB_D_Top, "##,##0.00")
                    'Tabla_Resumen.Rows(i + 1).Cells(N_Cont + 3).Value = Format(Seccion(i / 2).LEB_D_Bot, "##,##0.00")
                    'Tabla_Resumen.Rows(i).Cells(N_Cont + 4).Value = Format(Seccion(i / 2).Esf_I_Top, "##,##0.00")
                    'Tabla_Resumen.Rows(i + 1).Cells(N_Cont + 4).Value = Format(Seccion(i / 2).Esf_I_Bot, "##,##0.00")
                    'Tabla_Resumen.Rows(i).Cells(N_Cont + 5).Value = Format(Seccion(i / 2).Esf_D_Top, "##,##0.00")
                    'Tabla_Resumen.Rows(i + 1).Cells(N_Cont + 5).Value = Format(Seccion(i / 2).Esf_D_Bot, "##,##0.00")

                Next
            End If

        Catch ex As Exception

        End Try
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

    Public Sub Open()
        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(Open.FileName)

            Rellenar_Columnas()

        End If

    End Sub

    Public Sub Rellenar_Columnas()
        If proyecto.Muros.Info_Diseño = True Then

            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                Combo_Elementos.Items.Add(proyecto.Muros.Lista_Muros(i).Name)
                Form_02_01_ResultadosColumnas.Combo_Elementos.Items.Add(proyecto.Muros.Lista_Muros(i).Name)
            Next

            Combo_Elementos.Text = proyecto.Muros.Lista_Muros(0).Name
        End If

    End Sub

    Private Sub ToolStripMenuItem6_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem6.Click
        Open()
    End Sub

    Private Sub ToolStripMenuItem7_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem7.Click
        Try
            If proyecto.Ruta = String.Empty Then
                SaveAs(proyecto)
            Else
                Funciones_Programa.Serializar(proyecto.Ruta, proyecto)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub ToolStripMenuItem8_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem8.Click
        SaveAs(proyecto)
    End Sub

    Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem4.Click


        If proyecto.Muros.D_Techo_X > 0 Then
            Form_InfoDesignMuros.T_Dtecho_X.Text = Math.Round(proyecto.Muros.D_Techo_X, 3)
            Form_InfoDesignMuros.T_Dtecho_Y.Text = Math.Round(proyecto.Muros.D_Techo_Y, 3)
        End If

        Form_InfoDesignMuros.Show()
    End Sub

    Private Sub ToolStripMenuItem10_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem10.Click
        LlenarTabla_Diseno_Muros()
    End Sub

    Private Sub ToolStripMenuItem11_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem11.Click
        LlenarTabla_Secciones()
    End Sub

    Private Sub ToolStripMenuItem12_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem12.Click
        LlenarTabla_Fuerzas()
    End Sub

    Private Sub ToolStripMenuItem16_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem16.Click
        Form_06_00_PagInfoMuros.Show()
        Form_06_00_PagInfoMuros.WindowState = FormWindowState.Maximized
    End Sub

    Private Sub ToolStripMenuItem14_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem14.Click
        If proyecto.Muros.Lista_Muros.Count > 0 Then
            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                Form_06_01_ResultadosMuros.Combo_Elementos.Items.Add(proyecto.Muros.Lista_Muros(i).Name)
            Next
            Form_06_01_ResultadosMuros.Combo_Elementos.Text = proyecto.Muros.Lista_Muros(0).Name
        End If

        Form_06_01_ResultadosMuros.Show()
        Form_06_01_ResultadosMuros.WindowState = FormWindowState.Maximized
    End Sub

    Private Sub DiseñoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DiseñoToolStripMenuItem.Click

        LlenarTabla_Diseno_Muros()

        Dim Tabla As DataGridView

        Dim Col_Diseno = Funciones_02_Columnas.Columnas_Diseno("Pier")

        If proyecto.Muros.Info_Diseño = True Then
            Tabla = Tabla_Diseño_Flexo

            Dim Col_Piso As Integer = 0
            Dim Col_Label As Integer = 1
            Dim Col_Seccion As Integer = 1
            Dim Salto As Integer = 2
            Dim Col_As_Req As Integer = 9
            Dim Col_Ash_Req As Integer = 15
            Dim Col_Esf_I As Integer = 16
            Dim Col_Esf_D As Integer = 17
            Dim Col_C_I As Integer = 20
            Dim Col_C_D As Integer = 22
            Dim Col_LEB_I As Integer = 24
            Dim Col_LEB_D As Integer = 25


            For i = 0 To 25
                Dim C As String = Tabla.Rows(0).Cells(i).Value.ToString
                If C.Contains("Required") Then
                    Col_As_Req = i
                End If
                If C.Contains("Shear Rebar") Then
                    Col_Ash_Req = i
                    Col_Esf_I = i + 1
                    Col_Esf_D = i + 2
                    Col_C_I = i + 5
                    Col_C_D = i + 7
                    Col_LEB_I = i + 9
                    Col_LEB_D = i + 10
                End If
            Next

            Dim Lista(2, 6) As Single

            For i = 2 To Tabla.Rows.Count() - 1 Step Salto
                If Tabla.Rows(i).Cells(0).Value <> String.Empty Then

                    For j = 0 To 5
                        If IsDBNull(Tabla.Rows(i).Cells(Col_C_I + j).Value) Or IsNothing(Tabla.Rows(i).Cells(Col_C_I + j).Value) Then
                            Lista(1, j) = 0
                        Else
                            Lista(1, j) = Tabla.Rows(i).Cells(Col_C_I + j).Value
                        End If
                        If IsDBNull(Tabla.Rows(i + 1).Cells(Col_C_I + j).Value) Or IsNothing(Tabla.Rows(i + 1).Cells(Col_C_I + j).Value) Then
                            Lista(2, j) = 0
                        Else
                            Lista(2, j) = Tabla.Rows(i + 1).Cells(Col_C_I + j).Value
                        End If
                    Next

                    Dim Muro_ = proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = Tabla.Rows(i).Cells(Col_Label).Value)

                    Dim Seccion = Nothing
                    If Muro_ IsNot Nothing Then
                        Seccion = Muro_.Lista_Secciones.Find(Function(p) p.Piso = Tabla.Rows(i).Cells(Col_Piso).Value)
                    End If

                    If Muro_ IsNot Nothing And Seccion IsNot Nothing Then
                        'Asignación de la cuantia leida de la hoja de Shear Wall Pier Summary
                        Seccion.Cuantia_Top_Req = Convert.ToSingle(Tabla.Rows(i).Cells(Col_As_Req).Value)
                        Seccion.Cuantia_Bot_Req = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_As_Req).Value)

                        'Asignación de la cuantia volumetrica leida de la hoja de Shear Wall Pier Summary
                        Seccion.AsH_Req_Top = Convert.ToSingle(Tabla.Rows(i).Cells(Col_Ash_Req).Value)
                        Seccion.AsH_Req_Bot = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_Ash_Req).Value)

                        'Asignación de los esfuerzos del muro leido de la hoja de Shear Wall Pier Summary
                        Seccion.Esf_I_Top = Convert.ToSingle(Tabla.Rows(i).Cells(Col_Esf_I).Value)
                        Seccion.Esf_I_Bot = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_Esf_I).Value)
                        Seccion.Esf_D_Top = Convert.ToSingle(Tabla.Rows(i).Cells(Col_Esf_D).Value)
                        Seccion.Esf_D_Bot = Convert.ToSingle(Tabla.Rows(i + Salto - 1).Cells(Col_Esf_D).Value)

                        'Asignación de la longitud del eje neutro del muro leido de la hoja de Shear Wall Pier Summary
                        Seccion.C_I_Top = Convert.ToSingle(Lista(1, 0) / 1000)
                        Seccion.C_I_Bot = Convert.ToSingle(Lista(2, 0) / 1000)
                        Seccion.C_D_Top = Convert.ToSingle(Lista(1, 2) / 1000)
                        Seccion.C_D_Bot = Convert.ToSingle(Lista(2, 2) / 1000)
                    End If
                End If
            Next

        End If


    End Sub

    Private Sub Form_06_PagMuros_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim nuevaFuente As Font = New Font("Microsoft Sans Serif", 10, FontStyle.Bold)

        Dim Tabla = Tabla_Resumen
        For i = 0 To Tabla.ColumnCount - 1
            If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                Tabla.Columns(i).ReadOnly = True
                Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
            End If
        Next


    End Sub

    Private Sub UnirArchivosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles UnirArchivosToolStripMenuItem.Click

        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            Dim Proyecto_1 As New Proyecto
            Proyecto_1 = Funciones_Programa.DeSerializar(Of Proyecto)(Open.FileName)

            Dim muros_SinRef = proyecto.Muros.Lista_Muros.FindAll(Function(muro) muro.Lista_Secciones(0).AsH_Col = 0)
            Dim muros_ConRef = Proyecto_1.Muros.Lista_Muros.FindAll(Function(muro) muro.Lista_Secciones(0).AsH_Col > 0)

            For i = 0 To muros_SinRef.Count() - 1
                Dim Muro = muros_ConRef.Find(Function(p) p.Name = muros_SinRef(i).Name)

                If Muro IsNot Nothing Then
                    Dim index = proyecto.Muros.Lista_Muros.FindIndex(Function(p) p.Name = Muro.Name)

                    If index <> -1 Then
                        proyecto.Muros.Lista_Muros(index) = Muro
                    End If
                End If

            Next

        End If

    End Sub

    Private Sub SeccionesDeMurosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SeccionesDeMurosToolStripMenuItem.Click

        LlenarTabla_Secciones()

        Dim Tabla As DataGridView

        Dim Col_Secciones = Funciones_02_Columnas.Columnas_Secciones("Pier")

        If proyecto.Muros.Info_Secciones = True Then

            Tabla = Tabla_secciones

            Dim Col_Piso As Integer = Col_Secciones(0)
            Dim Col_Name As Integer = Col_Secciones(1)
            Dim Col_Material As Integer = Col_Secciones(2)
            Dim Col_B As Integer = Col_Secciones(3)
            Dim Col_H As Integer = Col_Secciones(4)

            For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
                Dim Elemento As String = proyecto.Muros.Lista_Muros(i).Name

                For Np = 0 To proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1
                    For j = 2 To Tabla.Rows.Count - 1
                        If Tabla.Rows(j).Cells(Col_Name).Value <> String.Empty And Tabla.Rows(j).Cells(Col_Name).Value = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Seccion And Tabla.Rows(j).Cells(Col_Piso).Value = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Piso Then
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model = Math.Min(Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value), Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value)) / 1000
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model = Math.Max(Convert.ToSingle(Tabla.Rows(j).Cells(Col_B).Value), Convert.ToSingle(Tabla.Rows(j).Cells(Col_H).Value)) / 1000
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Planos = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Planos = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).fc = Convert.ToSingle(Mid(Tabla.Rows(j).Cells(Col_Material).Value, 1, 2))
                            If Convert.ToSingle(Tabla.Rows(j).Cells(2).Value) > 10 Then
                                proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Direccion_Muro = "Y"
                            Else
                                proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Direccion_Muro = "X"
                            End If
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Altura = Convert.ToSingle(Tabla.Rows(j).Cells(15).Value)

                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).As_Top_Req = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Cuantia_Top_Req * 1000000
                            proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).As_Bot_Req = proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).tw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Lw_Model * proyecto.Muros.Lista_Muros(i).Lista_Secciones(Np).Cuantia_Bot_Req * 1000000

                            Exit For
                        End If
                    Next
                Next
                proyecto.Muros.Lista_Muros(i).Hw = proyecto.Muros.Lista_Muros(i).Lista_Secciones.Max(Function(p) p.Altura)
                proyecto.Muros.Lista_Muros(i).tw = proyecto.Muros.Lista_Muros(i).Lista_Secciones(proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1).tw_Model
                proyecto.Muros.Lista_Muros(i).Lw = proyecto.Muros.Lista_Muros(i).Lista_Secciones(proyecto.Muros.Lista_Muros(i).Lista_Secciones.Count - 1).Lw_Model
                proyecto.Muros.Lista_Muros(i).Ar = proyecto.Muros.Lista_Muros(i).Hw / proyecto.Muros.Lista_Muros(i).Lw
            Next
        End If


    End Sub


    Private Sub ReporteInicialToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ReporteInicialToolStripMenuItem.Click

        Crear_Subgrafico()

        Dim List_X As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()
        Dim List_Y As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()

        Llenar_Tablas_Macroparametros(Tabla_Parametros, List_X, List_Y)
        Funciones_00_Varias.Estilo_Tabla(Tabla_Parametros)

        Dim List_Prot_X As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X And p.TipoMuro = eNumeradores.eTipoMuro.Protagonico).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()
        Dim List_Prot_Y As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y And p.TipoMuro = eNumeradores.eTipoMuro.Protagonico).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()
        Llenar_Tablas_Macroparametros(Tabla_Muros_Protagonicos, List_Prot_X, List_Prot_Y)
        Funciones_00_Varias.Estilo_Tabla(Tabla_Muros_Protagonicos)

        Func_Muros.CalcularGeometriaMuros()
        Func_Muros.GraficosMurosPlanta(Figura_Muros_Tw, Figura_Muros_Protagonicos)

        'R_ReporteInicial.Show()
        Dim reporte As New ReporteInicial()
        reporte.Generar_Reporte()

    End Sub

    Public Sub Obtencion_Macroparametros()

        Dim Comb_Sismo As String
        Dim Comb_G As String
        Dim Comb_D As String

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
            Dim seccionMenorZ As SeccionMuro = proyecto.Muros.Lista_Muros(i).Lista_Secciones.OrderBy(Function(seccion) seccion.Coor_Z_Bot).First()
            Dim PisoBase As String = seccionMenorZ.Piso
            Dim Dir As String = [Enum].GetName(GetType(eNumeradores.eDireccion), seccionMenorZ.Direccion_Muro)

            Comb_Sismo = proyecto.Muros.Lista_Combinaciones_Muros.Find(Function(comb) comb.ToUpper().Substring(0, 3).Contains("S") And comb.ToUpper().Substring(0, comb.Length - 3).Contains(Dir.ToUpper()))

            Comb_G = proyecto.Muros.Lista_Combinaciones_Muros.FirstOrDefault(Function(comb) comb.ToUpper().Contains("CM") And comb.ToUpper().Contains("CV"))
            Comb_D = proyecto.Muros.Lista_Combinaciones_Muros.FirstOrDefault(Function(comb) comb.ToUpper().Contains("ENV") And comb.ToUpper().Contains("MIN"))

            proyecto.Muros.Lista_Muros(i).fc_Base = seccionMenorZ.fc
            proyecto.Muros.Lista_Muros(i).Vmax_S = seccionMenorZ.Lista_Combinaciones.Where(Function(p) p.Name = Comb_Sismo).Max(Function(p) Math.Abs(p.V2))
            proyecto.Muros.Lista_Muros(i).Pmax_G = seccionMenorZ.Lista_Combinaciones.Where(Function(p) p.Name = Comb_G).Max(Function(p) Math.Abs(p.P))
            proyecto.Muros.Lista_Muros(i).Pmax_D = seccionMenorZ.Lista_Combinaciones.Where(Function(p) p.Name = Comb_D).Max(Function(p) Math.Abs(p.P))
            proyecto.Muros.Lista_Muros(i).ALR_G = proyecto.Muros.Lista_Muros(i).Pmax_G / (seccionMenorZ.fc * 1000 * seccionMenorZ.Lw_Model * seccionMenorZ.tw_Model)
            proyecto.Muros.Lista_Muros(i).ALR_D = proyecto.Muros.Lista_Muros(i).Pmax_D / (seccionMenorZ.fc * 1000 * seccionMenorZ.Lw_Model * seccionMenorZ.tw_Model)
            proyecto.Muros.Lista_Muros(i).Porc_Vs_Geo = proyecto.Muros.Lista_Muros(i).tw * proyecto.Muros.Lista_Muros(i).Lw * proyecto.Muros.Lista_Muros(i).Lw
            proyecto.Muros.Lista_Muros(i).Ag_M = proyecto.Muros.Lista_Muros(i).tw * proyecto.Muros.Lista_Muros(i).Lw

        Next

        Dim VB_S_T_X As Single = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X).Sum(Function(p) p.Vmax_S)
        Dim VB_S_T_Y As Single = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y).Sum(Function(p) p.Vmax_S)

        Dim Porc_Geo_X As Single = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X).Sum(Function(p) p.Porc_Vs_Geo)
        Dim Porc_Geo_Y As Single = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y).Sum(Function(p) p.Porc_Vs_Geo)

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
            If proyecto.Muros.Lista_Muros(i).Direccion = eNumeradores.eDireccion.X Then
                proyecto.Muros.Lista_Muros(i).Porc_Vs = proyecto.Muros.Lista_Muros(i).Vmax_S / VB_S_T_X
                proyecto.Muros.Lista_Muros(i).Porc_Vs_Geo = proyecto.Muros.Lista_Muros(i).Porc_Vs_Geo / Porc_Geo_X
            Else
                proyecto.Muros.Lista_Muros(i).Porc_Vs = proyecto.Muros.Lista_Muros(i).Vmax_S / VB_S_T_Y
                proyecto.Muros.Lista_Muros(i).Porc_Vs_Geo = proyecto.Muros.Lista_Muros(i).Porc_Vs_Geo / Porc_Geo_Y
            End If
        Next

        proyecto.Muros.Densidad_X = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X).Sum(Function(p) p.Ag_M) / proyecto.Area_Planta
        proyecto.Muros.Densidad_Y = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y).Sum(Function(p) p.Ag_M) / proyecto.Area_Planta
        proyecto.Muros.IM_X = proyecto.Muros.Densidad_X / proyecto.NumPisos
        proyecto.Muros.IM_Y = proyecto.Muros.Densidad_Y / proyecto.NumPisos

        Dim List_X As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()
        Dim List_Y As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()

        Llenar_Tablas_Macroparametros(Tabla_Parametros, List_X, List_Y)
        Funciones_00_Varias.Estilo_Tabla(Tabla_Parametros)

        proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_X(0).Name).TipoMuro = eNumeradores.eTipoMuro.Protagonico
        proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_X(1).Name).TipoMuro = eNumeradores.eTipoMuro.Protagonico

        Dim Paso_X As Boolean = False

        For i = 2 To List_X.Count() - 1
            If List_X(i).Porc_Vs / List_X(i - 1).Porc_Vs > 0.75 And Paso_X = False Then
                proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_X(i).Name).TipoMuro = eNumeradores.eTipoMuro.Protagonico
            Else
                proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_X(i).Name).TipoMuro = eNumeradores.eTipoMuro.Complemento
                Paso_X = True
            End If
        Next

        proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_Y(0).Name).TipoMuro = eNumeradores.eTipoMuro.Protagonico
        proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_Y(1).Name).TipoMuro = eNumeradores.eTipoMuro.Protagonico
        Dim Paso_Y As Boolean = False

        For i = 2 To List_Y.Count() - 1
            If List_Y(i).Porc_Vs / List_Y(i - 1).Porc_Vs > 0.75 And Paso_Y = False Then
                proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_Y(i).Name).TipoMuro = eNumeradores.eTipoMuro.Protagonico
            Else
                proyecto.Muros.Lista_Muros.Find(Function(p) p.Name = List_Y(i).Name).TipoMuro = eNumeradores.eTipoMuro.Complemento
                Paso_Y = True
            End If
        Next

        Dim List_Prot_X As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.X And p.TipoMuro = eNumeradores.eTipoMuro.Protagonico).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()
        Dim List_Prot_Y As List(Of Muro) = proyecto.Muros.Lista_Muros.Where(Function(p) p.Direccion = eNumeradores.eDireccion.Y And p.TipoMuro = eNumeradores.eTipoMuro.Protagonico).OrderByDescending(Function(seccion) seccion.Porc_Vs).ToList()

        Llenar_Tablas_Macroparametros(Tabla_Muros_Protagonicos, List_Prot_X, List_Prot_Y)
        Funciones_00_Varias.Estilo_Tabla(Tabla_Muros_Protagonicos)

        'MaxCoorX = 0
        'MinCoorX = 0
        'MaxCoorY = 0
        'MinCoorY = 0

        'For i = 0 To proyecto.Muros.Lista_Muros.Count() - 1
        '    Dim Muro_ As Muro = proyecto.Muros.Lista_Muros(i)

        '    If Muro_.Lista_Secciones(0).Direccion_Muro = eNumeradores.eDireccion.X Then
        '        Rectangulos.Add(New Rectangulo() With {.Name = Muro_.Name, .Direccion = Muro_.Direccion, .Tipo_M = Muro_.TipoMuro, .CoorX = Muro_.Coor_X, .CoorY = Muro_.Coor_Y, .Largo = Muro_.Lw, .Espesor = Muro_.tw})
        '        If (Muro_.Coor_X - Muro_.Lw / 2) < MinCoorX Then
        '            MinCoorX = Muro_.Coor_X - Muro_.Lw / 2
        '        End If
        '        If (Muro_.Coor_X + Muro_.Lw / 2) > MaxCoorX Then
        '            MaxCoorX = Muro_.Coor_X + Muro_.Lw / 2
        '        End If
        '        If (Muro_.Coor_Y - Muro_.tw / 2) < MinCoorY Then
        '            MinCoorY = Muro_.Coor_Y - Muro_.tw / 2
        '        End If
        '        If (Muro_.Coor_Y + Muro_.tw / 2) > MaxCoorY Then
        '            MaxCoorY = Muro_.Coor_Y + Muro_.tw / 2
        '        End If

        '    Else
        '        Rectangulos.Add(New Rectangulo() With {.Name = Muro_.Name, .Direccion = Muro_.Direccion, .Tipo_M = Muro_.TipoMuro, .CoorX = Muro_.Coor_X, .CoorY = Muro_.Coor_Y, .Largo = Muro_.tw, .Espesor = Muro_.Lw})
        '        If (Muro_.Coor_X - Muro_.tw / 2) < MinCoorX Then
        '            MinCoorX = Muro_.Coor_X - Muro_.tw / 2
        '        End If
        '        If (Muro_.Coor_X + Muro_.tw / 2) > MaxCoorX Then
        '            MaxCoorX = Muro_.Coor_X + Muro_.tw / 2
        '        End If
        '        If (Muro_.Coor_Y - Muro_.Lw / 2) < MinCoorY Then
        '            MinCoorY = Muro_.Coor_Y - Muro_.Lw / 2
        '        End If
        '        If (Muro_.Coor_Y + Muro_.Lw / 2) > MaxCoorY Then
        '            MaxCoorY = Muro_.Coor_Y + Muro_.Lw / 2
        '        End If
        '    End If
        'Next

        Func_Muros.CalcularGeometriaMuros()
        Func_Muros.GraficosMurosPlanta(Figura_Muros_Tw, Figura_Muros_Protagonicos)

        'DibujarRectangulos()

    End Sub

    Public Sub Llenar_Tablas_Macroparametros(ByVal Tabla As DataGridView, ByVal List_X As List(Of Muro), ByVal List_Y As List(Of Muro))

        Tabla.Columns.Clear()

        Tabla.Columns.Add("C_01", "Muro")
        Tabla.Columns.Add("C_02", "Dirección")
        Tabla.Columns.Add("C_03", "Lw (m)")
        Tabla.Columns.Add("C_04", "tw (m)")
        Tabla.Columns.Add("C_05", "Hw (m)")
        Tabla.Columns.Add("C_06", "f'c (MPa)")
        Tabla.Columns.Add("C_07", "Pmax-G (kN)")
        Tabla.Columns.Add("C_08", "ALR-G (%)")
        Tabla.Columns.Add("C_09", "Pmax-D (kN)")
        Tabla.Columns.Add("C_10", "ALR-D")
        Tabla.Columns.Add("C_11", "Vmax (kN)")
        Tabla.Columns.Add("C_12", "% Vbasal Geo. (%)")
        Tabla.Columns.Add("C_13", "% Vbasal Sis. (%)")
        Tabla.Columns.Add("C_14", "Ar")

        For Each Muro As Muro In List_X
            Tabla.Rows.Add(Muro.Name, Muro.Direccion,
                        Math.Round(Muro.Lw, 2), Math.Round(Muro.tw, 2), Math.Round(Muro.Hw, 2), Math.Round(Muro.fc_Base, 1), Math.Round(Muro.Pmax_G, 0), Math.Round(Muro.ALR_G * 100, 1),
                        Math.Round(Muro.Pmax_D, 0), Math.Round(Muro.ALR_D * 100, 1), Math.Round(Muro.Vmax_S, 2), Math.Round(Muro.Porc_Vs_Geo * 100, 2), Math.Round(Muro.Porc_Vs * 100, 2),
                        Math.Round(Muro.Ar, 1))
        Next

        For Each Muro As Muro In List_Y
            Tabla.Rows.Add(Muro.Name, Muro.Direccion,
                        Math.Round(Muro.Lw, 2), Math.Round(Muro.tw, 2), Math.Round(Muro.Hw, 2), Math.Round(Muro.fc_Base, 1), Math.Round(Muro.Pmax_G, 0), Math.Round(Muro.ALR_G * 100, 1),
                        Math.Round(Muro.Pmax_D, 0), Math.Round(Muro.ALR_D * 100, 1), Math.Round(Muro.Vmax_S, 2), Math.Round(Muro.Porc_Vs_Geo * 100, 2), Math.Round(Muro.Porc_Vs * 100, 2),
                        Math.Round(Muro.Ar, 1))
        Next



    End Sub

    Private Sub Crear_Subgrafico()

        Func_Muros.Grafico_PorcentajeMuros(Grafico_MurosProtagonicos, 18, 16, 14)

    End Sub

    Private Sub DibujarRectangulos(ByVal Figura_Tw As PictureBox, ByVal Figura_Protagonicos As PictureBox)
        Figura_Tw.Refresh()
        Figura_Protagonicos.Refresh()

        '================== DEFINICIÓN DE COLORES, ESTILOS Y LAPICES PARA DIBUJO ==========================
        Dim Letra As New System.Drawing.Font("Arial", 10, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_12 As New System.Drawing.Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_14 As New System.Drawing.Font("Arial", 14, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_16 As New System.Drawing.Font("Arial", 16, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim Letra_18 As New System.Drawing.Font("Arial", 18, FontStyle.Regular, GraphicsUnit.Pixel)
        Dim CorR As New SolidBrush(Color.Red)
        Dim Pen_R As New Pen(Color.Red)
        Dim Pen_Black As New Pen(Color.Black)
        Dim Pen_Black_Line As New Pen(Color.Black)
        Pen_Black_Line.Width = 2
        Dim Pen_Blue As New Pen(Color.FromArgb(0, 0, 255))
        Dim Pen_Green As New Pen(Color.Green)
        Dim Pen_Orange As New Pen(Color.Orange)
        Dim Pen_Orangered As New Pen(Color.OrangeRed)
        Dim Pen_Magenta As New Pen(Color.Magenta)
        Dim Pen_Skyblue As New Pen(Color.SkyBlue)

        '=================== DEFINICIÓN DE DIMENSIONES DE EDIFICIO Y ESPACIO DE TRABAJO ===================
        Dim Len_X_Edificio As Single = MaxCoorX - MinCoorX
        Dim Len_Y_Edificio As Single = MaxCoorY - MinCoorY
        Dim W_Tablero As Single = Figura_Tw.Width
        Dim H_Tablero As Single = Figura_Tw.Height
        Dim W_Grafico As Single = W_Tablero
        Dim H_Grafico As Single = Figura_Tw.Height * 10 / 11
        Dim W_Leyenda As Single = W_Tablero
        Dim H_Leyenda As Single = Figura_Tw.Height / 11

        Dim W_Medida_X As Single = W_Grafico * 14 / 15
        Dim H_Medida_X As Single = H_Grafico / 15

        Dim W_Medida_Y As Single = W_Grafico / 15
        Dim H_Medida_Y As Single = H_Grafico * 12 / 13

        Dim W_Grafico_Final As Single = W_Grafico * 14 / 15
        Dim CooX_Grafico_Final As Single = W_Grafico / 15
        Dim H_Grafico_Final As Single = H_Grafico * 14 / 15


        Dim Rel_Aspect = Len_X_Edificio / Len_Y_Edificio
        Dim F_escala As Single = W_Grafico_Final / Len_X_Edificio
        If Len_X_Edificio < Len_Y_Edificio Then
            F_escala = H_Grafico_Final / Len_Y_Edificio
        End If

        Dim Coor_X_Base As Single = W_Medida_Y
        Dim Coor_Y_Base As Single = H_Medida_Y
        Dim Coor_X_Edificio As Single = MinCoorX
        Dim Coor_Y_Edificio As Single = MaxCoorY - MinCoorY

        '====================  GRÁFICO DE MUROS CON VARIACIÓN DE ESPESOR =====================
        Dim bmp As New Bitmap(Figura_Tw.Width, Figura_Tw.Height)

        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.White)

            Dim List_tw As New List(Of String)
            Dim List_Colors As New List(Of Brush)

            Dim Coor_X_Min = 0.95 * CooX_Grafico_Final + Coor_X_Edificio * F_escala
            Dim Coor_X_Max = 0.95 * CooX_Grafico_Final + Len_X_Edificio * F_escala

            Dim Coor_Y_Min = 0
            Dim Coor_Y_Max = Coor_Y_Edificio * F_escala

            For Each rectangulo In Rectangulos
                Dim CoorX_Real As Single = rectangulo.CoorX - Coor_X_Edificio
                Dim CoorY_Real As Single = Coor_Y_Edificio - (rectangulo.CoorY - MinCoorY)

                Dim x_Centro As Single = 0.95 * CooX_Grafico_Final + CoorX_Real * F_escala
                Dim x As Single = 0.95 * CooX_Grafico_Final + (CoorX_Real - rectangulo.Largo / 2) * F_escala

                Dim y_Centro As Single = CoorY_Real * F_escala
                Dim y As Single = (CoorY_Real - rectangulo.Espesor / 2) * F_escala

                Dim Pencil As Pen
                Dim Colors As Brush

                If Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.2 Then
                    Pencil = Pen_Green
                    List_tw.Add("0.20")
                    Colors = Brushes.Green
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.15 Then
                    Pencil = Pen_Blue
                    List_tw.Add("0.15")
                    Colors = Brushes.Blue
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.12 Then
                    Pencil = Pen_Magenta
                    List_tw.Add("0.12")
                    Colors = Brushes.Magenta
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.1 Then
                    Pencil = Pen_Skyblue
                    List_tw.Add("0.10")
                    Colors = Brushes.SkyBlue
                ElseIf Math.Round(Math.Min(rectangulo.Espesor, rectangulo.Largo), 2) = 0.25 Then
                    Pencil = Pen_Orangered
                    List_tw.Add("0.25")
                    Colors = Brushes.OrangeRed
                Else
                    Pencil = Pen_Black
                    List_tw.Add("Other")
                    Colors = Brushes.Black
                End If
                g.FillRectangle(Colors, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)
                g.DrawRectangle(Pencil, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)

                g.DrawString(rectangulo.Name, Letra_12, Brushes.Black, New PointF(x_Centro, y_Centro))

                Dim Y_Linea As Single = Coor_Y_Max * 1.1
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea), New PointF(Coor_X_Max, Y_Linea))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea + 10), New PointF(Coor_X_Min, Y_Linea - 10))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Max, Y_Linea + 10), New PointF(Coor_X_Max, Y_Linea - 10))
                g.DrawString(Convert.ToString(Math.Round(Len_X_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF((Coor_X_Min + Coor_X_Max) / 2, Y_Linea - 20))

                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2, 0), New PointF(W_Medida_Y / 2, Coor_Y_Max))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, 0), New PointF(W_Medida_Y / 2 - 10, 0))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, Coor_Y_Max), New PointF(W_Medida_Y / 2 - 10, Coor_Y_Max))
                g.DrawString(Convert.ToString(Math.Round(Len_Y_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF(W_Medida_Y / 2, Coor_Y_Max / 2))

            Next

            Dim Uniques_Tw As List(Of String) = List_tw.Distinct().ToList()
            H_Leyenda = Coor_Y_Max * 1.1 + 50

            For i = 0 To Uniques_Tw.Count - 1
                Dim x_1 As Single = W_Leyenda / 4 + (i + 1) * (Figura_Tw.Width / 2 - (60 * Uniques_Tw.Count) / 2) / (Uniques_Tw.Count + 1) + i * 60
                Dim y_1 As Single = H_Leyenda
                Dim Colors As Brush

                If Uniques_Tw(i) = "0.20" Then
                    Colors = Brushes.Green
                ElseIf Uniques_Tw(i) = "0.15" Then
                    Colors = Brushes.Blue
                ElseIf Uniques_Tw(i) = "0.12" Then
                    Colors = Brushes.Magenta
                ElseIf Uniques_Tw(i) = "0.10" Then
                    Colors = Brushes.SkyBlue
                ElseIf Uniques_Tw(i) = "0.25" Then
                    Colors = Brushes.OrangeRed
                Else
                    Colors = Brushes.Black
                End If

                g.FillRectangle(Colors, x_1, y_1, 25, 25)
                g.DrawString(Convert.ToString("Tw = " + Uniques_Tw(i) + " m"), Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1))
            Next
            g.Dispose()

        End Using
        Figura_Tw.Image = bmp


        Figura_Protagonicos.Refresh()
        Dim bmp_mp As New Bitmap(Figura_Protagonicos.Width, Figura_Protagonicos.Height)

        Using g As Graphics = Graphics.FromImage(bmp_mp)
            g.Clear(Color.White)

            Dim list_tw As New List(Of String)
            Dim list_colors As New List(Of Brush)

            Dim Coor_X_Min = 0.95 * CooX_Grafico_Final + Coor_X_Edificio * F_escala
            Dim Coor_X_Max = 0.95 * CooX_Grafico_Final + Len_X_Edificio * F_escala

            Dim Coor_Y_Min = 0
            Dim Coor_Y_Max = Coor_Y_Edificio * F_escala

            Dim colorrgb_Pr_X As Color = ColorTranslator.FromHtml("#1874cd")
            Dim colorrgb_Pr_Y As Color = ColorTranslator.FromHtml("#fc4e07")

            For Each rectangulo In Rectangulos
                Dim CoorX_Real As Single = rectangulo.CoorX - Coor_X_Edificio
                Dim CoorY_Real As Single = Coor_Y_Edificio - (rectangulo.CoorY - MinCoorY)

                Dim x_Centro As Single = 0.95 * CooX_Grafico_Final + CoorX_Real * F_escala
                Dim x As Single = 0.95 * CooX_Grafico_Final + (CoorX_Real - rectangulo.Largo / 2) * F_escala

                Dim y_Centro As Single = CoorY_Real * F_escala
                Dim y As Single = (CoorY_Real - rectangulo.Espesor / 2) * F_escala

                Dim pencil As Pen
                Dim colors As Brush

                If rectangulo.Tipo_M = eNumeradores.eTipoMuro.Protagonico Then
                    If rectangulo.Direccion = eNumeradores.eDireccion.X Then

                        Dim pen As New Pen(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
                        pencil = pen

                        Dim col As New SolidBrush(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
                        colors = col

                    Else

                        Dim pen As New Pen(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))
                        pencil = pen

                        Dim col As New SolidBrush(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))
                        colors = col

                    End If
                Else
                    pencil = New Pen(Color.Silver)
                    colors = Brushes.Silver
                End If

                g.FillRectangle(colors, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)
                g.DrawRectangle(pencil, x, y, rectangulo.Largo * F_escala, rectangulo.Espesor * F_escala)

                g.DrawString(rectangulo.Name, Letra_12, Brushes.Black, New PointF(x_Centro, y_Centro))

                Dim Y_Linea As Single = Coor_Y_Max * 1.1
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea), New PointF(Coor_X_Max, Y_Linea))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Min, Y_Linea + 10), New PointF(Coor_X_Min, Y_Linea - 10))
                g.DrawLine(Pen_Black_Line, New PointF(Coor_X_Max, Y_Linea + 10), New PointF(Coor_X_Max, Y_Linea - 10))
                g.DrawString(Convert.ToString(Math.Round(Len_X_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF((Coor_X_Min + Coor_X_Max) / 2, Y_Linea - 20))

                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2, 0), New PointF(W_Medida_Y / 2, Coor_Y_Max))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, 0), New PointF(W_Medida_Y / 2 - 10, 0))
                g.DrawLine(Pen_Black_Line, New PointF(W_Medida_Y / 2 + 10, Coor_Y_Max), New PointF(W_Medida_Y / 2 - 10, Coor_Y_Max))
                g.DrawString(Convert.ToString(Math.Round(Len_Y_Edificio, 1) & " m"), Letra_18, Brushes.Black, New PointF(W_Medida_Y / 2, Coor_Y_Max / 2))

            Next

            Dim Uniques_Tw As List(Of String) = list_tw.Distinct().ToList()
            H_Leyenda = Coor_Y_Max * 1.1 + 50

            Dim pen_PrX As New Pen(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
            Dim pen_PrY As New Pen(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))
            Dim colB_PrX As New SolidBrush(Color.FromArgb(colorrgb_Pr_X.R, colorrgb_Pr_X.G, colorrgb_Pr_X.B))
            Dim colB_PrY As New SolidBrush(Color.FromArgb(colorrgb_Pr_Y.R, colorrgb_Pr_Y.G, colorrgb_Pr_Y.B))

            Dim Texto_1 As String = "Muros Protagónicos Dir. Longitudinal (X)"
            Dim Texto_2 As String = "Muros Protagónicos Dir. Longitudinal (Y)"
            Dim Texto_3 As String = "Muros"
            Dim AnchoText_Total As Single = g.MeasureString(Texto_1, Letra_16).Width + g.MeasureString(Texto_2, Letra_16).Width + g.MeasureString(Texto_3, Letra_16).Width + 70
            Dim AltoTest As Single = g.MeasureString(Texto_1, Letra_16).Height

            Dim x_1 As Single = (W_Leyenda - AnchoText_Total) / 2
            Dim y_1 As Single = H_Leyenda
            Dim y_1_T As Single = H_Leyenda + (25 - AltoTest) / 2

            g.FillRectangle(colB_PrX, x_1, y_1, 25, 25)
            g.DrawString(Texto_1, Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1_T))
            Dim AnchoText As Single = g.MeasureString(Texto_1, Letra_16).Width

            x_1 = x_1 + AnchoText + 35
            g.FillRectangle(colB_PrY, x_1, y_1, 25, 25)
            g.DrawString(Texto_2, Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1_T))
            AnchoText = g.MeasureString(Texto_2, Letra_16).Width

            x_1 = x_1 + AnchoText + 35
            g.FillRectangle(Brushes.Silver, x_1, y_1, 25, 25)
            g.DrawString("Muros", Letra_16, Brushes.Black, New PointF(x_1 + 25, y_1_T))

            g.Dispose()

        End Using
        Figura_Protagonicos.Image = bmp_mp


    End Sub

    Private Sub Form_06_PagMuros_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize

        If Rectangulos.Count > 0 Then
            DibujarRectangulos(Figura_Muros_Tw, Figura_Muros_Protagonicos)
        End If

    End Sub
End Class