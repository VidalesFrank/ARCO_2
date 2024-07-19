Imports ARCO.Funciones_00_Varias
Public Class Funciones_02_Columnas
    Public Shared Function Funcion_Cortante(ByVal B As Single, ByVal H As Single, ByVal fc As Single, ByVal Fy As Single, ByVal s As Single,
                        ByVal Ref_Trans As String, ByVal Num_Ramas As Integer, ByVal Vu As Single, ByVal Pu As Single)

        Dim Revision(5)

        Dim Ass As Single = AreaRefuerzo(Ref_Trans)
        Dim Ae As Single = B * (H - 0.04)
        Dim Vs As Single = 0.75 * Num_Ramas * Ass * Fy * (H - 0.04) / (s * 1000)
        Dim Vc0 As Single = 0.17 * 0.75 * Math.Sqrt(fc) * Ae * 1000                                                   'C.11-3
        Dim Vc1 As Single = 0.17 * 0.75 * (1 + (Pu / (14000 * Ae))) * Math.Sqrt(fc) * Ae * 1000                       'C.11-4
        Dim Vc2 As Single = 0.29 * Math.Sqrt(fc) * Ae * 1000 * Math.Sqrt(1 + (0.29 * Pu / Ae))                        'C.11-7
        Dim Vc As Single = Math.Min(Math.Max(Vc0, Vc1), Vc2)
        Dim Vn As Single = Vc + Vs
        Dim Fmax As Single = Vn / Vu

        Revision(1) = Math.Round(Vc, 2)
        Revision(2) = Math.Round(Vs, 2)
        Revision(3) = Math.Round(Vn, 2)
        Revision(4) = Math.Round(Vu, 2)
        Revision(5) = Math.Round(Fmax, 2)

        Funcion_Cortante = Revision

    End Function
    Public Shared Function Funcion_Confinamiento(ByVal B As Single, ByVal H As Single, ByVal fc As Single, ByVal fy As Single, ByVal s As Single,
                                   ByVal Numero_Barra_Min_Long As String, ByVal Numero_Barra_Estribo As String, ByVal Disipacion As String)

        Dim bc As Single = B - 0.08
        Dim hc As Single = H - 0.08

        Dim Ag As Single = B * H
        Dim Ach As Single = bc * hc

        Dim Ash1 As Single = 0.2 * s * bc * fc * ((Ag / Ach) - 1) / fy
        Dim Ash2 As Single = 0.06 * s * bc * fc / fy

        Dim Db_Barra_Long As Single = DiametroRefuerzo(Numero_Barra_Min_Long)
        Dim As_Barra_Long As Single = AreaRefuerzo(Numero_Barra_Min_Long)

        Dim Db_Estribo As Single = DiametroRefuerzo(Numero_Barra_Estribo)
        Dim As_Estribo As Single = AreaRefuerzo(Numero_Barra_Estribo)

        Dim S0 As Single = Math.Min(Math.Min(Math.Min(8 * Db_Barra_Long, 16 * Db_Estribo), Math.Min(B, H) / 3), 0.15)
        Dim L0 As Single = Math.Max(Math.Max(2.3 / 6, Math.Max(B, H)), 0.5)

        If Disipacion = "DES" Then
            Ash1 = 0.3 * s * bc * fc * ((Ag / Ach) - 1) / fy
            Ash2 = 0.09 * s * bc * fc / fy

            S0 = Math.Min(Math.Min(Math.Min(B, H) / 4, Db_Barra_Long * 6), 0.15)
            L0 = Math.Max(2.3 / 6, 0.45)
        End If

        Dim Ash As Single = Math.Max(Ash1, Ash2) * 1000000
        Dim Cantidad_Ramas_Req As Single = Ash / As_Estribo

        Dim Resultados_Confinamiento(4)

        Resultados_Confinamiento(1) = Ash
        Resultados_Confinamiento(2) = Cantidad_Ramas_Req
        Resultados_Confinamiento(3) = S0
        Resultados_Confinamiento(4) = L0

        Funcion_Confinamiento = Resultados_Confinamiento
    End Function

    Public Shared Function Funcion_ALR(ByVal B As Single, ByVal H As Single, ByVal fc As Single, ByVal Pu As Single)

        Dim Ag As Single = B * H
        Funcion_ALR = Pu / (Ag * fc * 1000)

    End Function

    Public Shared Function Columnas_Diseno(ByVal Opcion As String)
        Dim Vector_Columnas(5)

        Dim Piso As Integer = 0
        Dim Label As Integer = 1
        Dim Seccion As Integer
        Dim Salto As Integer
        Dim As_Req As Integer

        If Opcion = "Frame" Then
            Seccion = 3
            Salto = 3
            As_Req = 10
        Else
            Seccion = 1
            Salto = 2
            As_Req = 9
        End If

        Vector_Columnas(0) = Piso
        Vector_Columnas(1) = Label
        Vector_Columnas(2) = Seccion
        Vector_Columnas(3) = Salto
        Vector_Columnas(4) = As_Req

        Columnas_Diseno = Vector_Columnas

    End Function

    Public Shared Function Columnas_Fuerzas(ByVal Opcion As String)
        Dim Vector_Columnas(10)

        Dim Piso As Integer = 0
        Dim Label As Integer = 1
        Dim Combinacion As Integer
        Dim Salto As Integer
        Dim P As Integer
        Dim V2 As Integer
        Dim V3 As Integer
        Dim T As Integer
        Dim M2 As Integer
        Dim M3 As Integer

        If Opcion = "Frame" Then
            Combinacion = 3
            Salto = 3
            P = 5
            V2 = 6
            V3 = 7
            T = 8
            M2 = 9
            M3 = 10
        Else
            Combinacion = 2
            Salto = 2
            P = 4
            V2 = 5
            V3 = 6
            T = 7
            M2 = 8
            M3 = 9
        End If

        Vector_Columnas(0) = Piso
        Vector_Columnas(1) = Label
        Vector_Columnas(2) = Combinacion
        Vector_Columnas(3) = Salto
        Vector_Columnas(4) = P
        Vector_Columnas(5) = V2
        Vector_Columnas(6) = V3
        Vector_Columnas(7) = T
        Vector_Columnas(8) = M2
        Vector_Columnas(9) = M3

        Columnas_Fuerzas = Vector_Columnas


    End Function

    Public Shared Function Columnas_Secciones(ByVal Opcion As String)

        Dim Vector_Columnas(5)

        Dim Piso As Integer
        Dim Name As Integer
        Dim Material As Integer
        Dim B As Integer
        Dim H As Integer

        If Opcion = "Frame" Then
            Name = 0
            Material = 1
            B = 3
            H = 4
        Else
            Piso = 0
            Name = 1
            Material = 9
            B = 5
            H = 6
        End If

        Vector_Columnas(0) = Piso
        Vector_Columnas(1) = Name
        Vector_Columnas(2) = Material
        Vector_Columnas(3) = B
        Vector_Columnas(4) = H

        Columnas_Secciones = Vector_Columnas

    End Function

    Public Shared Sub Funcion_Color_Cumple(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer, ByVal Opcion As String)

        If Opcion = "Cumple" Then
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 239, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(0, 97, 0)
        Else
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(255, 199, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(156, 0, 6)
        End If

    End Sub


    Public Shared Function Coordenadas_Barras(ByVal B As Single, ByVal H As Single, ByVal Cantidad As Integer, ByVal Cantidad_Corto As Integer, ByVal Cantidad_Largo As Integer)

        Dim Perimetro As Single = (B - 2 * 0.05) * 2 + (H - 2 * 0.05) * 2
        Dim Dist_Barras As Single = Perimetro / Cantidad

        Dim Bc As Single = B - 0.1
        Dim Hc As Single = H - 0.1

        Dim Lista_Coordendas(Cantidad, 2)

        Lista_Coordendas(0, 1) = 0.05 - B / 2
        Lista_Coordendas(0, 2) = H / 2 - 0.05
        Lista_Coordendas(1, 1) = B / 2 - 0.05
        Lista_Coordendas(1, 2) = H / 2 - 0.05
        Lista_Coordendas(2, 1) = 0.05 - B / 2
        Lista_Coordendas(2, 2) = 0.05 - H / 2
        Lista_Coordendas(3, 1) = B / 2 - 0.05
        Lista_Coordendas(3, 2) = 0.05 - H / 2

        For i = 0 To Cantidad_Corto - 1
            Lista_Coordendas(4 + i, 1) = Bc * ((i + 1) / (Cantidad_Corto + 1) - 0.5)
            Lista_Coordendas(4 + i, 2) = Hc / 2
        Next
        For i = 0 To Cantidad_Corto - 1
            Lista_Coordendas(4 + Cantidad_Corto + i, 1) = Bc * ((i + 1) / (Cantidad_Corto + 1) - 0.5)
            Lista_Coordendas(4 + Cantidad_Corto + i, 2) = -1 * Hc / 2
        Next
        For i = 0 To Cantidad_Largo - 1
            Lista_Coordendas(4 + 2 * Cantidad_Corto + i, 1) = -1 * Bc / 2
            Lista_Coordendas(4 + 2 * Cantidad_Corto + i, 2) = Hc * ((i + 1) / (Cantidad_Largo + 1) - 0.5)
        Next
        For i = 0 To Cantidad_Largo - 1
            Lista_Coordendas(4 + 2 * Cantidad_Corto + Cantidad_Largo + i, 1) = Bc / 2
            Lista_Coordendas(4 + 2 * Cantidad_Corto + Cantidad_Largo + i, 2) = Hc * ((i + 1) / (Cantidad_Largo + 1) - 0.5)
        Next

        Coordenadas_Barras = Lista_Coordendas
    End Function






End Class
