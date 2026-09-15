' Partial de Funciones_00_Varias: propiedades del refuerzo por designación de barra (#N).
' Área en mm², diámetro en metros (m). Referencia: NSR-10 C.3.5.
Partial Public Class Funciones_00_Varias

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
        Else
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

End Class
