Imports ARCO.Funciones_00_Varias

' Servicio de negocio del módulo Pilas — cálculos NSR-10 puros y construcción de secciones.
' Renombrado desde Funciones_01_Pilas (agosto 2026) para alinear la arquitectura con
' VigaService / NervioService (patrón: co-ubicado con su formulario en Formularios/XX_XXX/).
Public Class PilaService

    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto


    ' =========================================================================
    ' Construye los Elemento_Pila a partir de los parámetros comunes del UI y
    ' la lista de labels de apoyos. Reemplaza el loop inline de Button1_Click
    ' del formulario — separa lógica de dominio de la manipulación del DataGridView.
    ' =========================================================================
    Public Shared Sub CrearSeccionesDesdeParametros(pilas As cPilas,
                                                    params As ParametrosSeccionPila,
                                                    nombresElementos As List(Of String))
        pilas.ListaElementos.Clear()
        For Each nombreElemento In nombresElementos
            Dim seccion As New Elemento_Pila With {
                .Name_Elemento    = nombreElemento,
                .Name_Label       = nombreElemento,
                .Df               = params.Df,
                .Dc               = params.Dc,
                .L_Pila           = params.L_Pila,
                .fc               = params.Fc,
                .Opcion_Hueca     = If(params.EsHueca, "Si", "No"),
                .Esp_Anillo       = If(params.EsHueca, params.EspAnillo, 0),
                .N_Barra_Long     = params.NBarraLong,
                .Cant_Barras_Long = params.CantBarrasLong,
                .N_Barra_Trans    = params.NBarraTrans,
                .Separacion_Trans = params.SeparacionTrans,
                .Acero_Long       = CDbl(AreaRefuerzo(params.NBarraLong))
            }
            pilas.ListaElementos.Add(seccion)
        Next
    End Sub


    ' =========================================================================
    ' CHEQUEOS DE ESFUERZOS (columnas "Chequeo 1" a "Chequeo 5" de TablaRevi)
    '
    ' CONVENIO DE SIGNOS DEL MÓDULO: FZ de Joint Reactions es POSITIVO en
    ' COMPRESIÓN. No es casualidad: al leer "Pier Forces" se invierte el P de
    ' ETABS justamente para que coincida con Joint Reactions
    ' (Funciones_00_Varias: "P negativo = compresión -> FZ positivo = compresión").
    ' Por eso:
    '   - Chequeos 1..4 (compresión) toman Max(FZ) -> la compresión máxima, > 0.
    '   - Chequeo 5    (tracción)    toma  Min(FZ) -> el valor más NEGATIVO, que
    '     es la tracción. Solo hay tracción si ese mínimo es negativo.
    '
    ' SENTINELA COMPARTIDA: C/D = 0 significa "no aplica / sin cálculo". Así ya
    ' lo leen Form_Graficos_Pilas (tooltip de tracción solo si > 0) y
    ' ReporteRevisionService.MinEsfConcreto (Where v > 0). Estas funciones nunca
    ' devuelven infinito ni negativo: eran los dos valores que pintaban de rojo
    ' un chequeo inexistente y rompían la exportación a Excel.
    ' =========================================================================

    ''' <summary>
    ''' C/D a compresión: (coef x fc x Ag) / P. El coeficiente es el límite de
    ''' esfuerzo en el concreto (0.25 Ps est., 0.33 Ps din., 0.35 Pu).
    ''' </summary>
    ''' <param name="agF">Área del fuste [m2].</param>
    ''' <param name="p">Compresión máxima [kN], positiva. Si es menor o igual a
    ''' cero no hay demanda de compresión en las combinaciones elegidas y se
    ''' devuelve 0 (no aplica), no infinito.</param>
    Public Shared Function ChequeoCompresion(coef As Double, fc As Double,
                                             agF As Double, p As Double) As Single
        If p <= 0 Then Return 0
        If coef <= 0 OrElse fc <= 0 OrElse agF <= 0 Then Return 0
        ' fc [MPa] x Ag [m2] x 1000 = kN
        Return CSng(coef * fc * agF * 1000.0 / p)
    End Function

    ''' <summary>
    ''' C/D a tracción: phi x fy x As_total / |Pu_tracción|, con phi = 0.90
    ''' (NSR-10 C.9.3.2.2, tracción axial). Todo el tirón lo toma el acero: el
    ''' concreto fisurado no aporta.
    ''' </summary>
    ''' <param name="pTraccion">Mínimo de FZ [kN] con el signo de ETABS. Solo
    ''' aplica si es NEGATIVO; si es mayor o igual a cero la pila nunca se
    ''' levanta y el chequeo no aplica (0).</param>
    ''' <param name="areaBarra">Área de UNA barra longitudinal [mm2].</param>
    Public Shared Function ChequeoTraccion(pTraccion As Double, fy As Double,
                                           areaBarra As Double, cantBarras As Double) As Single
        If pTraccion >= 0 Then Return 0
        If fy <= 0 OrElse areaBarra <= 0 OrElse cantBarras <= 0 Then Return 0
        Dim phiTn As Double = 0.9 * fy * areaBarra * cantBarras   ' MPa x mm2 = N
        Return CSng(phiTn / (Math.Abs(pTraccion) * 1000.0))       ' N / (kN -> N)
    End Function

    ''' <summary>
    ''' Mínimo C/D entre los cinco chequeos de esfuerzos que apliquen; 0 si no
    ''' aplica ninguno. Criterio único para el veredicto "Cargas" del resumen,
    ''' el dashboard y el reporte de revisión.
    ''' </summary>
    Public Shared Function MinChequeoEsfuerzos(p As Elemento_Pila) As Double
        If p Is Nothing Then Return 0
        Dim valores = {p.Check1_PsE, p.Check2_PsD, p.Check3_PuE, p.Check4_PuD, p.Check5_PuT}.
                      Where(Function(v) v > 0 AndAlso Not Single.IsInfinity(v)).ToList()
        If valores.Count = 0 Then Return 0
        ' Redondeado a los dos decimales que se muestran: el veredicto tiene que
        ' coincidir con la tabla. Un C/D que se imprime 0.90 vale 0.8999999 como
        ' Single, y comparado crudo contra el umbral salía "no cumple".
        Return Math.Round(CDbl(valores.Min()), 2)
    End Function

    ''' <summary>
    ''' True si todos los chequeos de esfuerzos que aplican alcanzan el umbral
    ''' único del programa. Sin ningún chequeo calculado devuelve False
    ''' ("Revisar"): no se puede afirmar que cumple algo que no se calculó.
    ''' </summary>
    Public Shared Function CumpleEsfuerzos(p As Elemento_Pila) As Boolean
        Dim minimo As Double = MinChequeoEsfuerzos(p)
        Return minimo > 0 AndAlso minimo >= Funciones_00_Varias.UMBRAL_CD
    End Function


    '-------------------------- FUNCIÓN PARA DETERMINAR EL DIAGRAMA DE INTERACCIÓN EN UNA SECCIÓN CIRCULAR --------------------------
    Public Shared Function DiagramaInteraccionCircular(ByVal D As Single, ByVal R As Single, ByVal Barra As String, ByVal AreaBarraL As Single, ByVal NBarras As Single, ByVal Fc As Single, ByVal Es As Single, ByVal ecu As Single, ByVal Fy As Single)
        Dim esy As Single = Fy / Es
        Dim Rb As Single = D / 2 - R
        Dim Ag As Single = Math.PI * D ^ 2 / 4
        Dim es1 As Single
        Dim Mst As Single
        Dim Fst As Single
        Dim c As Single
        Dim di As Single
        Dim esi As Single
        Dim fsi As Single
        Dim b1 As Single
        Dim a As Single
        Dim x As Single
        Dim Beta As Single
        Dim Tetha As Single
        Dim Acc As Single
        Dim Mc As Single
        Dim Fs As Single
        Dim Phi As Single
        Dim Cc As Single
        Dim Mcc As Single
        Dim P As Single
        Dim M As Single
        Dim Db As Single = DiametroRefuerzo(Barra)
        Dim Ac As Single = AreaRefuerzo(Barra)
        Dim Acero As Single = Ac / 1000000
        Dim AceroT As Single = Acero * NBarras
        Dim Pmax As Single = 0.85 * Fc * (Ag - AceroT) * 1000 + Fy * AceroT * 1000
        Dim Pmin As Single = -Fy * AceroT * 1000
        Dim Resultados(1000, 5) As Single
        Resultados(1, 1) = Pmax
        Resultados(1, 2) = 0
        Resultados(1, 3) = Pmax * 0.8 * 0.65
        Resultados(1, 4) = 0
        Dim MinY As Single = 0
        Dim YBarras(100) As Single
        For i = 1 To NBarras
            Beta = (360 / NBarras) * i
            YBarras(i) = (D / 2) - Rb * Math.Sin(Beta * Math.PI / 180)
            If MinY <= YBarras(i) Then
                MinY = YBarras(i)
            End If
        Next
        Dim k As Single = 2
        Dim desy As Single = esy / 100
        For c = D To 0.04 Step -(D - 0.04) / 10
            Mst = 0
            Fst = 0
            For i = 1 To NBarras
                di = YBarras(i)
                esi = (c - di) * 0.003 / c
                fsi = Es * esi
                If fsi > Fy Then
                    fsi = Fy
                ElseIf fsi < -Fy Then
                    fsi = -Fy
                End If
                b1 = 0.85 - (0.05 * (Fc - 28) / 7)
                If b1 < 0.65 Then
                    b1 = 0.65
                End If
                If b1 > 0.85 Then
                    b1 = 0.85
                End If
                a = b1 * c
                If a <= (D / 2) Then
                    x = ((D / 2) - a) / (D / 2)
                    Tetha = Math.Acos(x)
                Else
                    If a > D Then
                        x = 1
                    Else
                        x = (a - (D / 2)) / (D / 2)
                    End If
                    If x = 1 Then
                        Tetha = Math.PI
                    Else
                        Tetha = Math.PI - Math.Acos(x)
                    End If
                End If
                Acc = D ^ 2 * (Tetha - Math.Sin(Tetha) * Math.Cos(Tetha)) / 4
                Mc = D ^ 3 * (Math.Sin(Tetha) ^ 3) / 12
                If c < di Then
                    Fs = fsi * Acero * 1000
                End If
                If c >= di Then
                    Fs = (fsi - 0.85 * Fc) * Acero * 1000
                End If
                Fst = Fs + Fst
                Mst = Fs * (D / 2 - di) + Mst
            Next
            es1 = (MinY - c) * 0.003 / c
            If Math.Abs(es1) <= esy Then
                Phi = 0.65
            ElseIf Math.Abs(es1) > esy And Math.Abs(es1) <= (2.5 * esy) Then
                Phi = 0.65 + (Math.Abs(es1) - 0.002) * (250 / 3)
            Else
                Phi = 0.9
            End If
            If Math.Abs(es1) > 0.015 Then
                Exit For
            End If
            Cc = Acc * Fc * 1000 * 0.85
            Mcc = Mc * Fc * 1000 * 0.85
            P = Cc + Fst
            M = Mcc + Mst
            Resultados(k, 1) = P
            Resultados(k, 2) = M
            Resultados(k, 3) = P * Phi
            Resultados(k, 4) = M * Phi
            If P * Phi > Pmax * 0.65 * 0.8 Then
                Resultados(k, 3) = Pmax * 0.65 * 0.8
            End If
            k += 1
        Next
        Resultados(k, 1) = Pmin
        Resultados(k, 2) = 0
        Resultados(k, 3) = Pmin * 0.9
        Resultados(k, 4) = 0
        Resultados(1, 5) = k
        DiagramaInteraccionCircular = Resultados
    End Function

    '-------------------------- REVISIÓN A CORTANTE --------------------------
    Public Shared Function ShearCheck(ByVal D As Single, ByVal fc As Single, ByVal Fy As Single, ByVal s As Single, ByVal Ref_Trans As String, ByVal Lista_V2 As List(Of Single),
                                      ByVal Lista_V3 As List(Of Single), ByVal Lista_Pu As List(Of Single), ByVal Opcion_Elemento As String, ByVal Esp_Anillo As Single)
        Dim Vc As Single
        Dim Vc1 As Single
        Dim Vc2 As Single
        Dim Vuu As Single
        Dim Vu As Single
        Dim Vnmax As Single
        Dim Nu As Single
        Dim Revision(1, 7)
        Dim Fmax As Single = 100
        Dim DbT As Single = DiametroRefuerzo(Ref_Trans)
        Dim Ass As Single = AreaRefuerzo(Ref_Trans)
        Dim FT As Single = -1

        If Proyecto.Elementos.Pilas.Opcion_Elemento = "Punto" Then
            FT = 1
        End If

        Dim Ag As Single = Math.PI * D ^ 2 / 4
        Dim D2 As Single
        Dim RelA_E As Single

        If Opcion_Elemento = "Si" Then
            D2 = D - 2 * Esp_Anillo
            Ag = Math.PI * (D ^ 2 ^ -D2 ^ 2) / 4
            RelA_E = D / Esp_Anillo
        End If

        Dim Ae As Single = D * 0.8 * D
        Dim Vc0 As Single = 0.17 * 0.75 * Math.Sqrt(fc) * Ag * 1000                                                   'C.11-3
        Dim Vs As Single = 0.75 * 2 * Ass * Fy * (0.8 * D) / (s * 1000)
        Dim Vn As Single

        Revision(1, 1) = "φVc"
        Revision(1, 2) = "Chequeo V2"
        Revision(1, 3) = "Chequeo V3"
        Revision(1, 4) = "φVc/Vu"

        For i = 0 To Lista_V2.Count - 1
            Nu = FT * Lista_Pu(i)
            Vc1 = 0.17 * 0.75 * (1 + (Nu / (14000 * Ag))) * Math.Sqrt(fc) * Ae * 1000                       'C.11-4
            Vc2 = 0.29 * Math.Sqrt(fc) * Ae * 1000 * Math.Sqrt(1 + (0.29 * Nu / (1000 * Ag)))
            Vc = Math.Min(Vc1, Vc0)

            If Opcion_Elemento = "Si" Then
                'Vc = RelA_E / (D * Esp_Anillo ^ 2)
            End If

            Vn = Vc + Vs
            Vuu = Math.Max(Lista_V2(i), Lista_V3(i))

            ' Vuu = 0 (pila sin combinaciones de cortante) → Vn/0 = Infinity y ClosedXML
            ' aborta la exportación con "Value can't be NaN or infinity".
            If Vuu <= 0 Then Continue For
            If Fmax > Vn / Vuu Then
                Fmax = Vn / Vuu
                Vu = Vuu
                Vnmax = Vn
                If Lista_V2(i) <= Vn And Lista_V3(i) <= Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "Cumple V2"
                    Revision(1, 3) = "Cumple V3"
                ElseIf Lista_V2(i) > Vn And Lista_V3(i) <= Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "No Cumple V2"
                    Revision(1, 3) = "Cumple V3"
                ElseIf Lista_V2(i) <= Vn And Lista_V3(i) > Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "Cumple V2"
                    Revision(1, 3) = "No Cumple V3"
                ElseIf Lista_V2(i) > Vn And Lista_V3(i) > Vn Then
                    Revision(1, 1) = Math.Round(Vc, 3)
                    Revision(1, 2) = "No Cumple V2"
                    Revision(1, 3) = "No Cumple V3"
                End If
            End If
        Next

        Revision(1, 4) = Math.Round(Fmax, 2)
        Revision(1, 5) = Math.Round(Vu, 3)
        Revision(1, 6) = Math.Round(Vs, 3)
        Revision(1, 7) = Math.Round(Vnmax, 3)
        ShearCheck = Revision
    End Function

    '---------------------------- CALCULO DE RELACIÓN CAPACIDAD/DEMANDA POR MEDIO DE RECTA DIAGONAL EN EL DI -----------------------
    Public Shared Function FDiagonal(ByVal Psol As Double, ByVal Msol As Double, ByVal Lista_Pn As List(Of Single), ByVal Lista_Mn As List(Of Single))
        Dim m_sol As Double
        Dim d_sol As Double
        If Msol > 0 Then
            m_sol = Psol / Msol
            d_sol = Math.Sqrt(Psol ^ 2 + Msol ^ 2)
        ElseIf Msol = 0 And Psol >= 0 Then
            m_sol = 9999
            d_sol = Psol
        ElseIf Msol = 0 And Psol < 0 Then
            m_sol = -9999
            d_sol = Psol
        End If
        Dim d_cap As Double
        For i = 0 To Lista_Pn.Count - 1
            Dim P1 As Double
            Dim P2 As Double
            Dim M1 As Double
            Dim M2 As Double
            If i <= Lista_Pn.Count - 2 Then
                P1 = Lista_Pn(i)
                P2 = Lista_Pn(i + 1)
                M1 = Lista_Mn(i)
                M2 = Lista_Mn(i + 1)
            End If
            Dim m_1 As Double
            Dim m_2 As Double
            If M1 = 0 Then
                m_1 = 9999
            Else
                m_1 = P1 / M1
            End If
            If M2 = 0 Then
                m_2 = -9999
            Else
                m_2 = P2 / M2
            End If
            If m_sol <= m_1 And m_sol >= m_2 Then
                Dim m As Double = (P1 - P2) / (M1 - M2)
                Dim x As Double = (P1 - m * M1) / (m_sol - m)
                Dim y As Double = m_sol * x
                d_cap = Math.Sqrt(x ^ 2 + y ^ 2)
                Exit For
            Else
                If Psol > 0 Then
                    d_cap = Math.Abs(Lista_Pn(0))
                Else
                    d_cap = Math.Abs(Lista_Pn(Lista_Pn.Count() - 1))
                End If
            End If
        Next
        Dim F = Math.Abs(d_cap / d_sol)
        FDiagonal = F
    End Function

    '---------------------------- CALCULO DE RELACIÓN CAPACIDAD/DEMANDA POR MEDIO DE CORTES HORIZONTALES EN EL DI -----------------------
    Public Shared Function FCorte(ByVal Psol As Double, ByVal Msol As Double, ByVal Lista_Pn As List(Of Single), ByVal Lista_Mn As List(Of Single))
        Dim F
        If Psol > Lista_Pn.Max Or Psol < Lista_Pn.Min Then
            F = 0
        Else
            Dim d_sol As Double = Msol
            Dim d_cap As Double
            For i = 0 To Lista_Pn.Count - 1
                Dim P1 As Double
                Dim P2 As Double
                Dim M1 As Double
                Dim M2 As Double
                If i <= Lista_Pn.Count - 2 Then
                    P1 = Lista_Pn(i)
                    P2 = Lista_Pn(i + 1)
                    M1 = Lista_Mn(i)
                    M2 = Lista_Mn(i + 1)
                End If

                Dim m As Double = (P1 - P2) / (M1 - M2)
                If P1 >= Psol And Psol > P2 Then
                    Dim x As Double = (Psol - P1) / m
                    d_cap = x + M1
                    Exit For
                End If
            Next

            If d_sol > 0 Then
                F = d_cap / d_sol
            Else
                F = 99
            End If
        End If
        FCorte = F
    End Function

End Class


' Parámetros de sección común compartidos por todas las pilas de un cálculo.
' Se lee UNA VEZ desde el UI y se propaga a cada Elemento_Pila via PilaService.CrearSeccionesDesdeParametros.
Public Structure ParametrosSeccionPila
    Public Df As Double
    Public Dc As Single
    Public L_Pila As Single
    Public Fc As Single
    Public EsHueca As Boolean
    Public EspAnillo As Single
    Public NBarraLong As String
    Public CantBarrasLong As Integer
    Public NBarraTrans As String
    Public SeparacionTrans As Single
End Structure
