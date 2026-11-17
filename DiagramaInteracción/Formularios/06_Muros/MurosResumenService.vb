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

    ' ── Fila por sección (Form_Reporte_Resumen_Muros) ─────────────────────

    ''' <summary>
    ''' Muros que entran al reporte por sección: solo los que ya tienen refuerzo
    ''' asignado (Ref_Modificado_Muros). Un muro sin refuerzo no está "mal": no
    ''' se ha diseñado, y listarlo con factores en 0 lo pintaba de rojo.
    ''' </summary>
    Public Shared Function MurosCalculados(muros As IEnumerable(Of Muro)) As List(Of Muro)
        If muros Is Nothing Then Return New List(Of Muro)
        Return muros.Where(Function(m) m IsNot Nothing AndAlso m.Ref_Modificado_Muros).ToList()
    End Function

    ''' <summary>
    ''' Todos los valores de una sección que muestra el reporte, calculados una
    ''' sola vez para que la grilla y el Excel no puedan discrepar.
    '''
    ''' Flexión = relación de cuantías ρ_col / ρ_req. Si ρ_req ≤ 0 el factor es
    ''' NaN ("sin dato"): ni cumple ni falla. Antes una pestaña lo tomaba como 0
    ''' (Revisar) y otra como 9.99 (OK) para la misma sección.
    '''
    ''' Elementos de borde: se revisan los extremos superior E inferior de la
    ''' sección (antes solo el superior).
    ''' </summary>
    Public Shared Function CalcularFilaSeccion(m As Muro, sec As SeccionMuro) As FilaSeccionMuro
        Dim f As New FilaSeccionMuro With {
            .Muro = m.Label,
            .Piso = sec.Piso,
            .tw = sec.tw_Planos,
            .Lw = sec.Lw_Planos,
            .CuantiaTopReq = sec.Cuantia_Top_Req,
            .CuantiaBotReq = sec.Cuantia_Bot_Req,
            .FTop = RelacionCuantia(sec.Cuantia_Top_Col, sec.Cuantia_Top_Req),
            .FBot = RelacionCuantia(sec.Cuantia_Bot_Col, sec.Cuantia_Bot_Req),
            .Vu = sec.Vu, .Vc = sec.Vc, .Vs = sec.Vs, .Vn = sec.Vn,
            .FCortante = sec.F_Cortante,
            .CLimite = sec.C_Limite,
            .C_I = Math.Max(sec.C_I_Top, sec.C_I_Bot),
            .C_D = Math.Max(sec.C_D_Top, sec.C_D_Bot),
            .EB_I_Esf = PeorChequeo(sec.Chequeo_EB_I_Top_Esf, sec.Chequeo_EB_I_Bot_Esf),
            .EB_D_Esf = PeorChequeo(sec.Chequeo_EB_D_Top_Esf, sec.Chequeo_EB_D_Bot_Esf),
            .EB_I_Def = PeorChequeo(sec.Chequeo_EB_I_Top_Def, sec.Chequeo_EB_I_Bot_Def),
            .EB_D_Def = PeorChequeo(sec.Chequeo_EB_D_Top_Def, sec.Chequeo_EB_D_Bot_Def)
        }

        ' Flexión: el menor de los factores con dato
        If Double.IsNaN(f.FTop) Then
            f.FFlexMin = f.FBot
        ElseIf Double.IsNaN(f.FBot) Then
            f.FFlexMin = f.FTop
        Else
            f.FFlexMin = Math.Min(f.FTop, f.FBot)
        End If

        ' Diagrama de interacción: factor y combinación del extremo que gobierna
        Dim dTop = sec.Factor_Demanda_Flexo_Top, dBot = sec.Factor_Demanda_Flexo_Bot
        f.FDI = Math.Min(dTop, dBot)
        f.CombCritica = If(dTop <= dBot, sec.Combinacion_Demanda_Flexo_Top, sec.Combinacion_Demanda_Flexo_Bot)
        If String.IsNullOrEmpty(f.CombCritica) Then f.CombCritica = "-"

        Return f
    End Function

    ''' <summary>
    ''' Colocado / requerido. NaN si no hay requerido (≤ 0): "sin dato", nunca
    ''' Infinity ni 0, que se leerían como "cumple sobrado" o "no cumple".
    ''' </summary>
    Public Shared Function RelacionCuantia(col As Single, req As Single) As Double
        If req <= 0 Then Return Double.NaN
        Return col / req
    End Function

    ''' <summary>
    ''' Área bruta [m²] con la que se calculó la cuantía colocada (AgM). Si la
    ''' sección aún no la tiene (muro sin refuerzo procesado), Lw·tw de planos.
    ''' </summary>
    Public Shared Function AreaBruta(sec As SeccionMuro) As Double
        If sec.AgM > 0 Then Return sec.AgM
        Return CDbl(sec.Lw_Planos) * sec.tw_Planos
    End Function

    ''' <summary>
    ''' Extremo (superior/inferior) que gobierna la flexo-compresión por
    ''' relación de cuantías: el de menor ρ_col/ρ_req entre los que tienen
    ''' requerido. Si ninguno lo tiene, F = NaN y se reportan las cuantías del
    ''' superior.
    ''' </summary>
    Public Shared Function FlexionGobernante(sec As SeccionMuro) As FlexionExtremo
        Dim fTop = RelacionCuantia(sec.Cuantia_Top_Col, sec.Cuantia_Top_Req)
        Dim fBot = RelacionCuantia(sec.Cuantia_Bot_Col, sec.Cuantia_Bot_Req)
        Dim usarBot = Not Double.IsNaN(fBot) AndAlso (Double.IsNaN(fTop) OrElse fBot < fTop)
        Dim area = AreaBruta(sec)
        Dim r As New FlexionExtremo
        If usarBot Then
            r.CuantiaCol = sec.Cuantia_Bot_Col : r.CuantiaReq = sec.Cuantia_Bot_Req : r.F = fBot
        Else
            r.CuantiaCol = sec.Cuantia_Top_Col : r.CuantiaReq = sec.Cuantia_Top_Req : r.F = fTop
        End If
        ' As [mm²] = ρ[%] / 100 × Ag[m²] × 1e6
        r.AsCol = r.CuantiaCol / 100.0 * area * 1000000.0
        r.AsReq = r.CuantiaReq / 100.0 * area * 1000000.0
        Return r
    End Function

    ''' <summary>"Requiere" si alguno de los dos extremos lo requiere.</summary>
    Public Shared Function PeorChequeo(top As String, bot As String) As String
        If top = "Requiere" OrElse bot = "Requiere" Then Return "Requiere"
        If String.IsNullOrEmpty(top) AndAlso String.IsNullOrEmpty(bot) Then Return "-"
        Return "No requiere"
    End Function

End Class

''' <summary>
''' Estado de un chequeo en el reporte por sección. SinDato = no hay con qué
''' evaluar (p. ej. ρ_req = 0): no se reporta como cumplimiento.
''' </summary>
Public Enum EstadoChequeoMuro
    OK
    Revisar
    SinDato
End Enum

Public Class FlexionExtremo
    Public Property CuantiaCol As Double     ' %
    Public Property CuantiaReq As Double     ' %
    Public Property AsCol As Double          ' mm²
    Public Property AsReq As Double          ' mm²
    Public Property F As Double              ' ρ_col/ρ_req; NaN = sin dato
End Class

Public Class FilaSeccionMuro
    Public Property Muro As String
    Public Property Piso As String
    Public Property tw As Single
    Public Property Lw As Single
    Public Property CuantiaTopReq As Single
    Public Property CuantiaBotReq As Single
    Public Property FTop As Double           ' NaN = sin dato (ρ_req = 0)
    Public Property FBot As Double           ' NaN = sin dato
    Public Property FFlexMin As Double       ' NaN = sin dato
    Public Property FDI As Single
    Public Property CombCritica As String
    Public Property Vu As Single
    Public Property Vc As Single
    Public Property Vs As Single
    Public Property Vn As Single
    Public Property FCortante As Single
    Public Property CLimite As Single
    Public Property C_I As Single
    Public Property C_D As Single
    Public Property EB_I_Esf As String
    Public Property EB_D_Esf As String
    Public Property EB_I_Def As String
    Public Property EB_D_Def As String

    Public ReadOnly Property EstadoFlexion As EstadoChequeoMuro
        Get
            ' Basta con que un extremo con dato falle para revisar.
            If (Not Double.IsNaN(FTop) AndAlso FTop < Funciones_00_Varias.UMBRAL_CD) OrElse
               (Not Double.IsNaN(FBot) AndAlso FBot < Funciones_00_Varias.UMBRAL_CD) Then Return EstadoChequeoMuro.Revisar
            If Double.IsNaN(FFlexMin) Then Return EstadoChequeoMuro.SinDato
            Return EstadoChequeoMuro.OK
        End Get
    End Property

    Public ReadOnly Property CumpleCortante As Boolean
        Get
            Return FCortante >= Funciones_00_Varias.UMBRAL_CD
        End Get
    End Property

    Public ReadOnly Property RequiereEB As Boolean
        Get
            Return EB_I_Esf = "Requiere" OrElse EB_D_Esf = "Requiere" OrElse
                   EB_I_Def = "Requiere" OrElse EB_D_Def = "Requiere"
        End Get
    End Property

    ''' <summary>Texto de estado global: "Revisar" manda sobre "Rev. EB" y "Sin dato".</summary>
    Public ReadOnly Property EstadoGlobal As String
        Get
            If EstadoFlexion = EstadoChequeoMuro.Revisar OrElse Not CumpleCortante Then Return "Revisar"
            If RequiereEB Then Return "Rev. EB"
            If EstadoFlexion = EstadoChequeoMuro.SinDato Then Return "Sin dato"
            Return "OK"
        End Get
    End Property
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
