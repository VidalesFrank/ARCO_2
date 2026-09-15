''' <summary>
''' Agregación de resultados por muro: reduce todas las secciones de un muro a
''' una sola fila con la peor condición de toda su altura (flexión, cortante y
''' elementos de borde).
'''
''' Vive fuera de los formularios a propósito: la consumen tanto el Resumen
''' Ejecutivo (Form_Reporte_Ejecutivo_Muros) como el dashboard de gráficas
''' (Form_Graficos_Muros), y así ambos no pueden contradecirse.
''' </summary>
Public NotInheritable Class MurosResumenService

    Private Sub New()
        ' Clase de servicio: no se instancia.
    End Sub

    ''' <summary>
    ''' Peor condición del muro entre todas sus secciones. Un FFlexMin o
    ''' FCortMin igual a -1 significa “sin cálculo” (el muro no se ha
    ''' verificado todavía), no “cumple con holgura”.
    ''' </summary>
    Public Shared Function CalcularFila(m As Muro) As FilaMuro
        Dim f As New FilaMuro With {
            .Label = m.Label,
            .Direccion = m.Direccion.ToString(),
            .Lw = m.Lw,
            .tw = m.tw,
            .Hw = m.Hw,
            .ALR_G = m.ALR_G,
            .ALR_D = m.ALR_D,
            .PorcVs = m.Porc_Vs
        }

        ' Flexión: peor factor entre todas las secciones
        Dim fFlexMin As Single = Single.MaxValue
        Dim pisoCriticoFlex As String = "—"
        For Each sec In m.Lista_Secciones
            Dim ff As Single = Math.Min(sec.Factor_Demanda_Flexo_Top, sec.Factor_Demanda_Flexo_Bot)
            If ff < 99 AndAlso ff < fFlexMin Then
                fFlexMin = ff
                pisoCriticoFlex = sec.Piso
            End If
        Next
        f.FFlexMin = If(fFlexMin = Single.MaxValue, -1, fFlexMin)
        f.PisoCriticoFlex = pisoCriticoFlex

        ' Cortante: peor factor entre todas las secciones
        Dim fCortMin As Single = Single.MaxValue
        Dim pisoCriticoCort As String = "—"
        For Each sec In m.Lista_Secciones
            If sec.F_Cortante > 0 AndAlso sec.F_Cortante < fCortMin Then
                fCortMin = sec.F_Cortante
                pisoCriticoCort = sec.Piso
            End If
        Next
        f.FCortMin = If(fCortMin = Single.MaxValue, -1, fCortMin)
        f.PisoCriticoCort = pisoCriticoCort

        ' Elementos de Borde: peor condición en toda la altura
        f.EBIzq = PeorEB(m, esIzquierdo:=True)
        f.EBDer = PeorEB(m, esIzquierdo:=False)

        ' Estado global
        Dim cumpleFlex = f.FFlexMin < 0 OrElse f.FFlexMin >= 0.9F
        Dim cumpleCort = f.FCortMin < 0 OrElse f.FCortMin >= 0.9F
        Dim ebOK = (f.EBIzq = "No Requiere") AndAlso (f.EBDer = "No Requiere")
        f.Cumple = cumpleFlex AndAlso cumpleCort AndAlso ebOK
        Return f
    End Function

    Public Shared Function PeorEB(m As Muro, esIzquierdo As Boolean) As String
        Dim reqEsp As Boolean = False
        Dim reqNoEsp As Boolean = False
        For Each sec In m.Lista_Secciones
            If esIzquierdo Then
                reqEsp = reqEsp OrElse sec.Req_EB_I_Top_Esp OrElse sec.Req_EB_I_Bot_Esp
                reqNoEsp = reqNoEsp OrElse sec.Req_EB_I_Top_NoEsp OrElse sec.Req_EB_I_Bot_NoEsp
            Else
                reqEsp = reqEsp OrElse sec.Req_EB_D_Top_Esp OrElse sec.Req_EB_D_Bot_Esp
                reqNoEsp = reqNoEsp OrElse sec.Req_EB_D_Top_NoEsp OrElse sec.Req_EB_D_Bot_NoEsp
            End If
        Next
        Return If(reqEsp, "Especializado", If(reqNoEsp, "No Especial", "No Requiere"))
    End Function

End Class

Public Class FilaMuro
    Public Property Label As String
    Public Property Direccion As String
    Public Property Lw As Single
    Public Property tw As Single
    Public Property Hw As Single
    Public Property ALR_G As Single
    Public Property ALR_D As Single
    Public Property FFlexMin As Single       ' -1 = sin cálculo
    Public Property PisoCriticoFlex As String
    Public Property FCortMin As Single        ' -1 = sin cálculo
    Public Property PisoCriticoCort As String
    Public Property EBIzq As String
    Public Property EBDer As String
    Public Property PorcVs As Single
    Public Property Cumple As Boolean
End Class
