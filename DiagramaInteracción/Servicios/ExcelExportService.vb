Imports System.Runtime.InteropServices
Imports Excel = Microsoft.Office.Interop.Excel

' Utilidades comunes para exportación a Excel via COM Interop.
' Consolida el patrón de limpieza que se repite en Form_02_01_ResultadosColumnas
' y Form_06_01_ResultadosMuros. Sin esto, cada exportación deja procesos EXCEL.EXE
' huérfanos porque Marshal.ReleaseComObject no se llama en orden.
'
' Uso típico en un método que exporta:
'   Dim appXL As Excel.Application = Nothing
'   Dim wbXL As Excel.Workbook = Nothing
'   Dim hoja As Excel.Worksheet = Nothing
'   Try
'       appXL = New Excel.Application
'       wbXL  = appXL.Workbooks.Add()
'       hoja  = wbXL.Sheets("Hoja1")
'       ' ... llenar hoja ...
'       wbXL.SaveAs(rutaDestino)
'   Catch ex As Exception
'       Logger.Error(ex, "MiForm.Exportar", "...")
'   Finally
'       ExcelExportService.CerrarYLiberar(appXL, wbXL, hoja)
'   End Try
Public Class ExcelExportService

    ''' <summary>Libera un objeto COM si no es Nothing. Silencioso ante errores.</summary>
    Public Shared Sub LiberarCOM(obj As Object)
        Try
            If obj IsNot Nothing Then
                Marshal.ReleaseComObject(obj)
            End If
        Catch
            ' Fallar el release no debe propagar — es limpieza best-effort.
        End Try
    End Sub

    ''' <summary>
    ''' Cierra Excel y libera hojas + workbook + application en el orden correcto (inverso a la
    ''' creación). Llama Quit y fuerza GC para eliminar el proceso EXCEL.EXE. Idempotente:
    ''' llamar Quit dos veces es inofensivo.
    ''' </summary>
    Public Shared Sub CerrarYLiberar(appXL As Excel.Application, wbXL As Excel.Workbook, ParamArray hojas As Object())
        ' 1. Hojas primero (orden inverso al que se abrieron)
        If hojas IsNot Nothing Then
            For i As Integer = hojas.Length - 1 To 0 Step -1
                LiberarCOM(hojas(i))
            Next
        End If

        ' 2. Workbook
        LiberarCOM(wbXL)

        ' 3. Application: Quit + release
        If appXL IsNot Nothing Then
            Try
                appXL.Quit()
            Catch
                ' Puede fallar si ya se llamó Quit — ignorar.
            End Try
            LiberarCOM(appXL)
        End If

        ' 4. Forzar GC para que .NET libere los CCW y termine el proceso EXCEL.EXE
        GC.Collect()
        GC.WaitForPendingFinalizers()
    End Sub

    ''' <summary>Alineación centrada horizontal + vertical para el mismo rango.</summary>
    Public Shared Sub CentrarRango(hoja As Excel.Worksheet, rango As String)
        hoja.Range(rango).HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        hoja.Range(rango).VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
    End Sub

End Class
