Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary

Public Class Funciones_Programa

    Public Shared Sub Serializar(ByVal Ruta As String, ByVal Objeto As Object)
        Dim Format As New BinaryFormatter
        Dim Stream As New FileStream(Ruta, FileMode.Create, FileAccess.Write, FileShare.None)

        Format.Serialize(Stream, Objeto)

        Stream.Close()

    End Sub

    Public Shared Function DeSerializar(Of T)(ByVal Ruta As String) As T
        Dim Objeto1 As Object

        Dim Format As New BinaryFormatter
        Dim Stream As New FileStream(Ruta, FileMode.Open, FileAccess.Read, FileShare.None)
        Objeto1 = Format.Deserialize(Stream)
        Stream.Close()
        Return CType(Objeto1, T)

    End Function

End Class
