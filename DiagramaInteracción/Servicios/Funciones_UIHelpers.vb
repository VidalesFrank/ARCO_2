Imports System.Drawing

' Partial de Funciones_00_Varias: helpers de UI (coloreo condicional de celdas/casillas y estilos de tabla).
' Los colores verde/rojo corresponden a la paleta de "cumple/no cumple" que usa toda la app.
Partial Public Class Funciones_00_Varias

    Public Shared Sub FuncionColorCumple(ByVal Tabla As DataGridView, ByVal Fila As Integer, ByVal Columna As Integer, ByVal Opcion As String)

        If Opcion = "Cumple" Then
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(198, 239, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(0, 97, 0)
        Else
            Tabla.Rows(Fila).Cells(Columna).Style.BackColor = Color.FromArgb(255, 199, 206)
            Tabla.Rows(Fila).Cells(Columna).Style.ForeColor = Color.FromArgb(156, 0, 6)
        End If

    End Sub

    Public Shared Sub CasillaCumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(198, 239, 206)
        Casilla.ForeColor = Color.FromArgb(0, 97, 0)
    End Sub

    Public Shared Sub CasillaNoCumple(ByVal Casilla As TextBox)
        Casilla.BackColor = Color.FromArgb(255, 199, 206)
        Casilla.ForeColor = Color.FromArgb(156, 0, 6)
    End Sub

    Public Shared Sub EstiloTabla(ByVal Tabla As DataGridView)

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
