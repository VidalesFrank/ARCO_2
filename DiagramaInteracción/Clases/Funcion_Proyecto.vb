Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary

Public Class Funciones_Programa

    Public Shared Sub Serializar(ByVal Ruta As String, ByVal Objeto As Object)
        'Dim Format As New BinaryFormatter
        'Dim Stream As New FileStream(Ruta, FileMode.Create, FileAccess.Write, FileShare.None)

        'Format.Serialize(Stream, Objeto)

        'Stream.Close()

        Try
            Dim Format As New BinaryFormatter

            Using Stream As New FileStream(Ruta, FileMode.Create, FileAccess.Write, FileShare.None)
                Format.Serialize(Stream, Objeto)
            End Using

        Catch ex As Exception
            Throw New Exception("Error al serializar: " & ex.Message)
        End Try

    End Sub

    ''' <summary>
    ''' Combina módulos seleccionados desde un archivo .esm origen al proyecto destino.
    ''' Cada módulo lleva sus propios Joints/Frames embebidos, por lo que la geometría
    ''' de otros módulos en el destino no se ve afectada.
    ''' </summary>
    Public Shared Sub CombinarDesdeArchivo(proyectoDestino As Proyecto,
                                            rutaOrigen As String,
                                            modulosSeleccionados As HashSet(Of String))
        Dim origen As Proyecto
        Try
            origen = DeSerializar(Of Proyecto)(rutaOrigen)
        Catch ex As Exception
            Throw New Exception("No se pudo leer el archivo origen: " & ex.Message)
        End Try

        If modulosSeleccionados.Contains("Pilas") Then
            proyectoDestino.Elementos.Pilas = origen.Elementos.Pilas
        End If
        If modulosSeleccionados.Contains("Columnas") Then
            proyectoDestino.Elementos.Columnas = origen.Elementos.Columnas
        End If
        If modulosSeleccionados.Contains("Muros") Then
            proyectoDestino.Elementos.Muros = origen.Elementos.Muros
        End If
        If modulosSeleccionados.Contains("Vigas") Then
            proyectoDestino.Elementos.Vigas = origen.Elementos.Vigas
        End If
        If modulosSeleccionados.Contains("Nervios") Then
            proyectoDestino.Elementos.Nervios = origen.Elementos.Nervios
        End If
        If modulosSeleccionados.Contains("Zapatas") Then
            proyectoDestino.Elementos.Zapatas = origen.Elementos.Zapatas
        End If
    End Sub

    ''' <summary>
    ''' Devuelve el número de elementos del módulo indicado en el proyecto dado.
    ''' </summary>
    Public Shared Function ContarElementosMod(modulo As String, p As Proyecto) As Integer
        If p Is Nothing OrElse p.Elementos Is Nothing Then Return 0
        Try
            Select Case modulo
                Case "Pilas"
                    Return If(p.Elementos.Pilas?.ListaElementos?.Count, 0)
                Case "Columnas"
                    Return If(p.Elementos.Columnas?.Lista_Columnas?.Count, 0)
                Case "Muros"
                    Return If(p.Elementos.Muros?.Lista_Muros?.Count, 0)
                Case "Vigas"
                    Return If(p.Elementos.Vigas?.Frames?.Count, 0)
                Case "Nervios"
                    Return If(p.Elementos.Nervios?.Elementos?.Count, 0)
                Case "Zapatas"
                    Return 0
                Case Else
                    Return 0
            End Select
        Catch
            Return 0
        End Try
    End Function

    Public Shared Function DeSerializar(Of T)(ByVal Ruta As String) As T
        'Dim Objeto1 As Object

        'Dim Format As New BinaryFormatter
        'Dim Stream As New FileStream(Ruta, FileMode.Open, FileAccess.Read, FileShare.None)
        'Objeto1 = Format.Deserialize(Stream)
        'Stream.Close()
        'Return CType(Objeto1, T)

        Try
            Dim Format As New BinaryFormatter

            Using Stream As New FileStream(Ruta, FileMode.Open, FileAccess.Read, FileShare.Read)
                Return CType(Format.Deserialize(Stream), T)
            End Using

        Catch ex As Exception
            Throw New Exception("Error al deserializar: " & ex.Message)
        End Try

    End Function

End Class
