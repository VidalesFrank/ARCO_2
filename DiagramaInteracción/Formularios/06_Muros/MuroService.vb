Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores
Imports ARCO.Form_06_PagMuros
Imports ARCO.Funciones_00_Varias
'Imports Microsoft.Office.Interop.Word
Imports Colores = ARCO.ColoresProyecto

' Servicio de negocio del módulo Muros — NSR-10 (cortante, cuantías, elementos de borde,
' interacción P-M, factor capacidad/demanda) + renderizado de gráficos (planta, derivas,
' ALR, tw vs altura). Renombrado desde Funciones_Muros (agosto 2026) para alinear la
' arquitectura con VigaService / ColumnaService / PilaService.
Partial Public Class MuroService
    Public Shared proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public Shared Muro As New Muro

    Public Shared Function FuncionCorteLimite(ByVal tw As Single, ByVal Lw As Single, ByVal fc As Single, ByVal Vu As Single)

        Dim Resultados_Rho(3)

        Dim V_Lim As Single = 0.083 * tw * Lw * Math.Sqrt(fc) * 1000
        Dim Rho_L As Single = 0.0025
        Dim Rho_t As Single = 0.0025

        If V_Lim >= Vu Then
            Rho_L = 0.0012
            Rho_t = 0.002
        End If

        Resultados_Rho(0) = Rho_L * 100
        Resultados_Rho(1) = Rho_t * 100
        Resultados_Rho(2) = V_Lim

        FuncionCorteLimite = Resultados_Rho
    End Function

    Public Shared Function FuncionCortante(ByVal tw As Single, ByVal Lw As Single, ByVal fc As Single, ByVal Fy As Single, ByVal CuantiaH As Single, ByVal Vu As Single)

        Dim Revision(5)

        Dim Vs As Single = 0.75 * tw * Lw * CuantiaH * Fy * 1000
        Dim Vc As Single = 0.75 * 0.17 * tw * Lw * Math.Sqrt(fc) * 1000
        Dim Vn As Single = Vc + Vs
        Dim F_ As Single = Vn / Vu

        Revision(1) = Math.Round(Vc, 2)
        Revision(2) = Math.Round(Vs, 2)
        Revision(3) = Math.Round(Vn, 2)
        Revision(4) = Math.Round(F_, 2)

        FuncionCortante = Revision
    End Function

    Public Shared Function CalculoCuantia(ByVal tw As Single, ByVal Ref_Malla As String, ByVal Capas_1 As Integer, ByVal Sep_1 As Single, ByVal Ref_2 As String, ByVal Capas_2 As Integer, ByVal Sep_2 As Single)
        Dim mallaTipo As MallaTipo = StringToMallaTipo(Ref_Malla)

        Dim As_1 As Single = AceroMallas(mallaTipo)
        Dim As_2 As Single = AreaRefuerzo(Ref_2)

        Dim Rho_1 As Single = 0
        If Sep_1 > 0 Then
            Rho_1 = Capas_1 * (1 / Sep_1) * As_1 / (1000 * tw * 1000)
        End If

        Dim Rho_2 As Single = 0
        If Sep_2 > 0 Then
            Rho_2 = Capas_2 * (1 / Sep_2) * As_2 / (1000 * tw * 1000)
        End If

        CalculoCuantia = Rho_1 + Rho_2

    End Function

    Public Shared Function EB_C(ByVal Dis As eDisipasion, ByVal Du As Single, ByVal Hw As Single, ByVal Lw As Single)

        Dim du_hw_Lim As Single = 0.0075
        Dim du_hw As Single = Math.Max(Du / Hw, du_hw_Lim)

        If Dis = eDisipasion.DMO Then
            du_hw_Lim = 0.0035
            du_hw = Math.Max(Du / Hw, du_hw_Lim)
        End If

        Dim C_Lim As Single = Lw / (600 * du_hw)

        EB_C = C_Lim

    End Function

    Public Shared Function EB_Esf(ByVal Dis As eDisipasion, ByVal Hw As Single, ByVal Lw As Single, ByVal fc As Single, ByVal Esf_ As Single)

        Dim esf_max As Single = 0.2 * fc
        Dim esf_lim As Single = 0.15 * fc

        If Dis = eDisipasion.DMO Then
            esf_max = 0.3 * fc
            esf_lim = 0.22 * fc
        End If

        Dim Chequeo As String = "No requiere"

        If Esf_ > esf_max / 0.9 Then
            Chequeo = "Requiere"
        End If
        Dim Revision(3)

        Revision(0) = Chequeo
        Revision(1) = esf_max
        Revision(2) = esf_lim

        EB_Esf = Revision

    End Function


    Public Shared Function AceroMallas(ByVal malla As MallaTipo) As Single
        If MallaData.MallaAreas.ContainsKey(malla) Then
            Return MallaData.MallaAreas(malla)
        Else
            Throw New ArgumentOutOfRangeException("malla", "Tipo de malla no soportado")
        End If
    End Function

    Public Shared Function StringToMallaTipo(ByVal mallaString As String) As MallaTipo
        Select Case mallaString
            Case "D-84"
                Return MallaTipo.D_84
            Case "D-106"
                Return MallaTipo.D_106
            Case "D-131"
                Return MallaTipo.D_131
            Case "D-158"
                Return MallaTipo.D_158
            Case "D-188"
                Return MallaTipo.D_188
            Case "D-221"
                Return MallaTipo.D_221
            Case "D-257"
                Return MallaTipo.D_257
            Case "D-295"
                Return MallaTipo.D_295
            Case "D-335"
                Return MallaTipo.D_335
            Case "None"
                Return MallaTipo.None
            Case Else
                Throw New ArgumentOutOfRangeException("mallaString", "Cadena de malla no soportada")
        End Select
    End Function

    Public Shared Function Acero_Barras(ByVal Barra As BarraTipo) As Single
        If BarraData.BarraAreas.ContainsKey(Barra) Then
            Return BarraData.BarraAreas(Barra)
        Else
            Throw New ArgumentOutOfRangeException("malla", "Tipo de malla no soportado")
        End If
    End Function

    Public Shared Function StringToBarraTipo(ByVal BarraString As String) As BarraTipo
        Select Case BarraString
            Case "#2"  : Return BarraTipo.Num_2
            Case "#3"  : Return BarraTipo.Num_3
            Case "#4"  : Return BarraTipo.Num_4
            Case "#5"  : Return BarraTipo.Num_5
            Case "#6"  : Return BarraTipo.Num_6
            Case "#7"  : Return BarraTipo.Num_7
            Case "#8"  : Return BarraTipo.Num_8
            Case "#10" : Return BarraTipo.Num_10
            Case "None" : Return BarraTipo.None
            Case Else  : Return BarraTipo.None
        End Select
    End Function

    Public Shared Function AceroH_EB(ByVal Dis As eDisipasion, ByVal E_Borde As SeccionMuro.ElementoBorde, ByVal tw As Single, ByVal fc As Single, ByVal fy As Single)

        Dim Resultados(2)

        Dim dbl As Single
        Dim dbe As Single = Math.Sqrt(AreaRefuerzo(E_Borde.RefH.Acero) * 4 / Math.PI)

        If E_Borde.Barras_L.Barras_2 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#2") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_3 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#3") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_4 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#4") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_5 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#5") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_6 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#6") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_7 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#7") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_8 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#8") * 4 / Math.PI)
        ElseIf E_Borde.Barras_L.Barras_10 > 0 Then
            dbl = Math.Sqrt(AreaRefuerzo("#10") * 4 / Math.PI)
        End If

        Dim s0 As Single = Math.Min(Math.Min(8 * dbl, 16 * dbe), Math.Min(tw * 1000 / 2, 150))

        Dim bc As Single = tw - 0.08
        Dim hc As Single = E_Borde.L_EB - 0.08

        Dim Ag As Single = tw * E_Borde.L_EB
        Dim Ach As Single = bc * hc
        Dim s As Single = E_Borde.RefH.Separacion

        If s = 0 And E_Borde.Tipo_EB_Req = "Especial" Then
            hc = E_Borde.L_EB_Req - 0.08
            s = Math.Min(tw / 2, 0.15)
        End If

        'Dim Ash1 As Single = 0.3 * s * bc * fc * ((Ag / Ach) - 1) / fy
        Dim Ash2 As Single = 0.09 * s * hc * fc / fy
        If Dis = eDisipasion.DMO Then
            Ash2 = 0.06 * s * hc * fc / fy
        End If

        Dim Ash As Single = Ash2 * 1000000

        Resultados(0) = s0
        Resultados(1) = Ash

        AceroH_EB = Resultados

    End Function


    Public Sub Obtencion_Macroparametros(ByVal Proyecto As Proyecto)





    End Sub


    ' Intersección de rayo desde origen en dirección (Md, Pd) con la envolvente.
    ' Retorna t tal que el punto de intersección es (t*Md, t*Pd).
    ' Factor = t: si t < 1 la demanda supera la capacidad; si t > 1 está dentro.
    Private Shared Function FactorInteraccionDireccion(Md As Single, Pd As Single,
                                                        Lista_Mn As List(Of Single),
                                                        lista_Pn As List(Of Single)) As Single
        Dim t_mejor As Single = 100

        For i As Integer = 0 To Lista_Mn.Count - 2
            Dim M1 As Single = Lista_Mn(i)
            Dim P1 As Single = lista_Pn(i)
            Dim dM As Single = Lista_Mn(i + 1) - M1
            Dim dP As Single = lista_Pn(i + 1) - P1

            ' Denominador del sistema rayo (t*Md, t*Pd) vs segmento (M1+s*dM, P1+s*dP)
            Dim denom As Single = Pd * dM - Md * dP
            If Math.Abs(denom) < 1.0E-9F Then Continue For

            Dim t As Single = (P1 * dM - M1 * dP) / denom
            If t <= 0 Then Continue For

            ' Parámetro s en el segmento
            Dim s As Single = If(Math.Abs(dM) >= Math.Abs(dP),
                                 If(Math.Abs(dM) > 1.0E-10F, (t * Md - M1) / dM, 0),
                                 If(Math.Abs(dP) > 1.0E-10F, (t * Pd - P1) / dP, 0))

            If s >= -0.001F AndAlso s <= 1.001F AndAlso t < t_mejor Then
                t_mejor = t
            End If
        Next

        Return t_mejor
    End Function

    Public Shared Function CalculoFactorCapacidad(solicitaciones As List(Of SeccionMuro.Fuerzas_Elementos),
                                                   Lista_Mn As List(Of Single),
                                                   lista_Pn As List(Of Single)) As (String, Single)
        Dim Factor_demanda As Single = 100
        Dim Solicitacion_Pr As String = ""

        If solicitaciones Is Nothing OrElse solicitaciones.Count = 0 OrElse
           Lista_Mn Is Nothing OrElse Lista_Mn.Count < 2 Then
            Return (If(solicitaciones IsNot Nothing AndAlso solicitaciones.Count > 0,
                       solicitaciones(0).Name, ""), Factor_demanda)
        End If

        For Each solicitacion As SeccionMuro.Fuerzas_Elementos In solicitaciones
            If solicitacion Is Nothing Then Continue For

            Dim Md As Single = Math.Abs(solicitacion.M3)
            Dim Pd As Single = solicitacion.P
            If Md < 0.0001F Then Continue For

            Dim Factor As Single = FactorInteraccionDireccion(Md, Pd, Lista_Mn, lista_Pn)

            If Factor < Factor_demanda Then
                Factor_demanda = Factor
                Solicitacion_Pr = solicitacion.Name
            End If
        Next

        If String.IsNullOrEmpty(Solicitacion_Pr) Then Solicitacion_Pr = solicitaciones(0).Name

        Return (Solicitacion_Pr, Factor_demanda)
    End Function

    Public Shared Function RectaCapacidadDemanda(solicitacion As SeccionMuro.Fuerzas_Elementos,
                                                  Lista_Mn As List(Of Single),
                                                  lista_Pn As List(Of Single)) As (List(Of Single), List(Of Single))
        Dim List_X As New List(Of Single)
        Dim List_Y As New List(Of Single)
        List_X.Add(0) : List_Y.Add(0)

        If solicitacion Is Nothing OrElse Lista_Mn Is Nothing OrElse Lista_Mn.Count < 2 Then
            Return (List_X, List_Y)
        End If

        Dim Md As Single = Math.Abs(solicitacion.M3)
        Dim Pd As Single = solicitacion.P
        If Md < 0.0001F Then Return (List_X, List_Y)

        Dim t As Single = FactorInteraccionDireccion(Md, Pd, Lista_Mn, lista_Pn)

        If t < 100 Then
            List_X.Add(t * Md)
            List_Y.Add(t * Pd)
        End If

        Return (List_X, List_Y)
    End Function





End Class
