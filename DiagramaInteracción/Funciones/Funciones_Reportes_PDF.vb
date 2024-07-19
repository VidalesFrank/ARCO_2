Imports itextsharp.text
Imports itextsharp.text.pdf
Public Class Funciones_Reportes_PDF
    Public Shared Function Texto_Parrafo(ByVal Texto As String, ByVal Identacion_I As Single, ByVal Identacion_D As Single, ByVal Espacio As Integer)
        Dim arial As BaseFont = BaseFont.CreateFont("c:\windows\fonts\arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED)
        Dim Font_Textos As New Font(arial, 11)
        Dim Parrafo_Conclusion As New Paragraph
        Parrafo_Conclusion.IndentationLeft = Identacion_I
        Parrafo_Conclusion.IndentationRight = Identacion_D
        Parrafo_Conclusion.Alignment = Element.ALIGN_JUSTIFIED
        Parrafo_Conclusion.Font = Font_Textos
        Parrafo_Conclusion.SpacingAfter = Espacio
        Parrafo_Conclusion.Add(Texto)

        Return Parrafo_Conclusion

    End Function
    Public Shared Function Titulo_Figura(ByVal Figura1 As String, ByVal Figura2 As String, ByVal Tipo_Titulo As String)
        Dim arial As BaseFont = BaseFont.CreateFont("c:\windows\fonts\arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED)
        Dim Font_Figura As New Font(arial, 10)
        Dim Font_Titulo_Figura As New Font(arial, 10, FontStyle.Bold)
        Dim Parrafo As New Paragraph
        If Tipo_Titulo = "Figura" Then
            Parrafo.Alignment = Element.ALIGN_CENTER
            Parrafo.SpacingAfter = 10
        ElseIf Tipo_Titulo = "Tabla" Then
            Parrafo.Alignment = Element.ALIGN_JUSTIFIED
            Parrafo.IndentationLeft = 40
        End If
        Parrafo.Font = Font_Titulo_Figura
        Parrafo.Add(Figura1)
        Parrafo.Font = Font_Figura
        Parrafo.Add(Figura2)
        Return Parrafo
    End Function
    Public Shared Function Texto_Tabla(ByVal Texto As String, ByVal Fuente As Font, ByVal Fondo As BaseColor, ByVal Alineacion As String, ByVal Top As Integer, ByVal Bottom As Integer)

        Dim Text As New PdfPCell
        Text.BackgroundColor = Fondo
        Text.BorderColor = BaseColor.WHITE
        Text.BorderWidth = 1
        Text.PaddingTop = Top
        Text.PaddingBottom = Bottom
        Text.VerticalAlignment = Element.ALIGN_CENTER
        If Alineacion = "Centrado" Then
            Text.HorizontalAlignment = Element.ALIGN_CENTER
        End If

        Text.Phrase = New Phrase(Texto, Fuente)

        Return Text

    End Function
End Class
