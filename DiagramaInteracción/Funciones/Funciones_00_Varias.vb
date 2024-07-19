Imports System.IO
Public Class Funciones_00_Varias
    Public Shared Function AreaRefuerzo(ByVal Barra As String)
        Dim Ac As Single
        If Barra = "#2" Then
            Ac = 32
        ElseIf Barra = "#3" Then
            Ac = 71
        ElseIf Barra = "#4" Then
            Ac = 129
        ElseIf Barra = "#5" Then
            Ac = 199
        ElseIf Barra = "#6" Then
            Ac = 284
        ElseIf Barra = "#7" Then
            Ac = 387
        ElseIf Barra = "#8" Then
            Ac = 510
        ElseIf Barra = "#10" Then
            Ac = 819
        ElseIf Barra = "None" Then
            Ac = 0
        End If
        AreaRefuerzo = Ac
    End Function
    Public Shared Function DiametroRefuerzo(ByVal Barra As String)
        Dim Db As Single
        If Barra = "#2" Then
            Db = 2 * 25.4 / 8000
        ElseIf Barra = "#3" Then
            Db = 3 * 25.4 / 8000
        ElseIf Barra = "#4" Then
            Db = 4 * 25.4 / 8000
        ElseIf Barra = "#5" Then
            Db = 5 * 25.4 / 8000
        ElseIf Barra = "#6" Then
            Db = 6 * 25.4 / 8000
        ElseIf Barra = "#7" Then
            Db = 7 * 25.4 / 8000
        ElseIf Barra = "#8" Then
            Db = 8 * 25.4 / 8000
        ElseIf Barra = "#10" Then
            Db = 10 * 25.4 / 8000
        Else
            'Db = Math.Sqrt(4 * Convert.ToSingle(AreaLongitudinal.Text) / Math.PI) / 1000
        End If
        DiametroRefuerzo = Db
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

    Public Shared Function DiseñoFlexion(ByVal Fy As Single, ByVal Fc As Single, ByVal Base As Single, ByVal H As Single, ByVal r As Single, ByVal Momento As Single, ByVal CuantiaMinima1 As Single, ByVal CuantiaMinima2 As Single) As Single
        Dim Ace As Single
        '------------------ VALORES DE ALFA Y BETA DE ACUERDO A LA RESISTENCIA DEL CONCRETO --------------------
        Dim alfa As Single
        Dim beta As Single
        Dim pmax As Single
        Dim pmin As Single
        If Fc < 28 Then
            alfa = 0.7225
            beta = 0.425
        Else
            alfa = 0.7225 - (0.04 * ((Fc - 28) / 7))
            beta = 0.425 - (0.025 * ((Fc - 28) / 7))
        End If
        '------------------VALORES MAXIMOS --------------------
        pmax = 0.75 * (alfa * (Fc / Fy)) * (600 / (600 + Fy))
        '------------------VALORES DE CUANTIA MINIMA --------------------
        Dim pmin_1 As Single = CuantiaMinima1
        Dim pmin_2 As Single = CuantiaMinima2
        '------------------REVISA LA CUANTIA MINIMA --------------------
        If pmin_1 >= pmin_2 Then
            pmin = pmin_1
        Else
            pmin = pmin_2
        End If
        '------------------CÁLCULO DE ACERO DE REFUERZO --------------------
        Dim Mom As Single = Momento * 1000000
        Dim B As Single = Base
        Dim d As Single = H - r
        Dim m As Single = Fy / (0.85 * Fc)
        Dim p As Single = (1 - (1 - (2 * m * (Mom / (0.9 * B * d * d)) / Fy)) ^ 0.5) / m
        '------------------CHEQUEO DE CUANTIA MINIMA --------------------
        Dim pmin_3 As Double = 1.33 * p
        If p <= pmin And pmin_3 < pmin Then
            p = pmin_3
        End If
        If p <= pmin And pmin_3 > pmin Then
            p = pmin
        End If
        Ace = p * B * d
        DiseñoFlexion = Ace
    End Function

    Public Shared Sub GuardarProyecto(ByVal Objeto As Object, ByVal Nombre_Archivo As String)
        Try
            Dim SaveAs As New SaveFileDialog
            SaveAs.Filter = "Archivo|*.esm"
            SaveAs.Title = "Guardar Archivo"
            SaveAs.FileName = Nombre_Archivo
            SaveAs.ShowDialog()
            If SaveAs.FileName <> String.Empty Then
                Objeto.Ruta = Path.GetFullPath(SaveAs.FileName)
                Funciones_Programa.Serializar(SaveAs.FileName, Objeto)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Public Shared Sub Abrir_Importar_Excel()
        Dim OpenFileDialog As New OpenFileDialog
        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivos"
            .Filter = "Archivos Excel(*.xls;*.xlsx)|*.xls;*xlsx|Todos los archivos(*.*)|*.*"
            .Multiselect = False
            .InitialDirectory = My.Computer.FileSystem.SpecialDirectories.Desktop
            If .ShowDialog = Windows.Forms.DialogResult.OK Then
                Form_01_PagPilas.ImportExcellToDataGridView_CServicio(.FileName, Form_01_PagPilas.TablaCServicio)
                Form_01_PagPilas.ImportExcellToDataGridView_CUltimas(.FileName, Form_01_PagPilas.TablaCUltimas)
            End If
        End With
    End Sub
    Public Shared Sub Casilla_Cumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(198, 239, 206)
        Casilla.ForeColor = Color.FromArgb(0, 97, 0)
    End Sub
    Public Shared Sub Casilla_Nocumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(255, 199, 206)
        Casilla.ForeColor = Color.FromArgb(156, 0, 6)
    End Sub

    Public Shared Sub Estilo_Tabla(ByVal Tabla As DataGridView)

        With Tabla.DefaultCellStyle
            .Font = New Font("Arial", 10)

            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With

        With Tabla.ColumnHeadersDefaultCellStyle
            .Font = New Font("Arial", 10, FontStyle.Bold)
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        End With
    End Sub

End Class
