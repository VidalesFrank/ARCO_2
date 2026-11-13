''' <summary>
''' Servicio de cálculo del módulo de Pilotes. Pura lógica sin dependencias
''' de UI — testeable desde ARCO.Tests. Mismo patrón que ZapataService y
''' PilaService: funciones estáticas, estado en los modelos.
'''
''' Algoritmo central: cabezal rígido con flexión biaxial y tensor de inercia
''' completo (Σuv puede ser distinto de cero en formas irregulares).
''' </summary>
Public Module PiloteService

    Public Const UMBRAL_CD As Double = 0.90

    ''' <summary>
    ''' Resultado del cálculo para una sola combinación.
    ''' </summary>
    Public Class ResultadoCombinacion
        Public Property LoadCase As String = ""
        ''' <summary>Fuerza en cada pilote [kN]. + = compresión.</summary>
        Public Property Fuerzas As Double()
        Public Property Pu_Max As Double
        Public Property Pu_Min As Double
    End Class

    ''' <summary>Resultado agregado de un dado para todas sus combinaciones.</summary>
    Public Class ResultadoDado
        Public Property Pu_Max_Compresion As Double
        Public Property Pu_Min_Traccion As Double
        Public Property Combo_Gob_Compresion As String = ""
        Public Property Combo_Gob_Traccion As String = ""
        Public Property CD_Compresion As Double
        ''' <summary>
        ''' C/D en tracción. 0 (sentinela "no aplica") si no hay combinación con
        ''' Pu_Min &lt; 0. Igual regla que PilaService.MinChequeoEsfuerzos.
        ''' </summary>
        Public Property CD_Traccion As Double
        Public Property PorCombinacion As New List(Of ResultadoCombinacion)

        ''' <summary>
        ''' Resumen pilote por pilote: cada entrada es el máx/mín que ve ese
        ''' pilote individual a lo largo de TODAS las combinaciones, con el
        ''' nombre del combo gobernante y el C/D individual del pilote.
        ''' Es la vista "qué P llega a cada pilote y quién lo controla".
        ''' </summary>
        Public Property PorPilote As New List(Of ResumenPilote)
    End Class

    ''' <summary>
    ''' Resultado agregado por pilote individual dentro de un dado. Dice cuánta
    ''' carga ve ese pilote en el peor caso de compresión y en el peor de
    ''' tracción, y cuál combinación disparó cada uno.
    ''' </summary>
    Public Class ResumenPilote
        ''' <summary>Índice del pilote en TipoDado.Pilotes (0-based).</summary>
        Public Property Indice As Integer
        ''' <summary>Número 1-based para mostrar al usuario (P1, P2, …).</summary>
        Public Property Numero As Integer
        Public Property X_Local As Double
        Public Property Y_Local As Double
        Public Property NombreTipoPilote As String = ""
        Public Property Pu_Max As Double
        Public Property Combo_Max As String = ""
        Public Property Pu_Min As Double
        Public Property Combo_Min As String = ""
        ''' <summary>C/D individual del pilote en compresión (0 = no aplica).</summary>
        Public Property CD_Compresion As Double
        ''' <summary>C/D individual del pilote en tracción (0 = no aplica).</summary>
        Public Property CD_Traccion As Double
    End Class

    ''' <summary>
    ''' Fuerza axial en cada pilote bajo cabezal rígido. Resuelve el sistema
    ''' acoplado si el tensor de inercia tiene Σuv ≠ 0.
    '''
    ''' Cargas en SISTEMA LOCAL del dado (el caller rota M_x, M_y antes de llamar
    ''' si AnguloPlanta ≠ 0). Convenciones:
    '''   P  > 0 = compresión sobre el dado
    '''   M_x = momento alrededor del eje X local → genera gradiente en Y
    '''   M_y = momento alrededor del eje Y local → genera gradiente en X
    '''
    ''' Para un grupo con n pilotes en posiciones (x_i, y_i) locales:
    '''   u_i = x_i − x̄,  v_i = y_i − ȳ
    '''   Tensor I = [[Σu², Σuv], [Σuv, Σv²]],  det = Σu²·Σv² − (Σuv)²
    '''   [a]     1   [ Σv²   −Σuv] [M_y]
    '''   [b]  = ─── [−Σuv    Σu² ] [M_x]
    '''              det
    '''   P_i = P/n + a·u_i + b·v_i
    ''' </summary>
    Public Function CalcularFuerzaPilotes(pilotes As IList(Of PilotePosicion),
                                          P As Double, M_x As Double, M_y As Double) As Double()
        If pilotes Is Nothing OrElse pilotes.Count = 0 Then
            Return New Double() {}
        End If

        Dim n = pilotes.Count
        Dim resultado(n - 1) As Double

        Dim tensor = DadoGeometria.TensorInerciaGrupo(pilotes)
        Dim centro = tensor.Centroide

        Dim a As Double = 0, b As Double = 0
        If Not tensor.Singular Then
            ' [a]   1   [ Σv²   −Σuv] [M_y]
            ' [b] = ─── [−Σuv    Σu² ] [M_x]
            '          det
            a = (tensor.Svv * M_y - tensor.Suv * M_x) / tensor.Determinante
            b = (-tensor.Suv * M_y + tensor.Suu * M_x) / tensor.Determinante
        End If

        For i = 0 To n - 1
            Dim u = pilotes(i).X_Local - centro.X
            Dim v = pilotes(i).Y_Local - centro.Y
            resultado(i) = P / n + a * u + b * v
        Next

        Return resultado
    End Function

    ''' <summary>
    ''' Rota el par (Mx, My) global al sistema local de un dado girado
    ''' AnguloPlanta grados antihorario. Vectorialmente, los momentos son
    ''' pseudo-vectores en el plano horizontal, por lo que la misma matriz
    ''' de rotación 2D aplica.
    ''' </summary>
    Public Function RotarMomentoAGlobalALocal(Mx As Double, My As Double,
                                              anguloGrados As Double) As Tuple(Of Double, Double)
        Dim rad = anguloGrados * Math.PI / 180.0
        Dim cs = Math.Cos(rad), sn = Math.Sin(rad)
        Dim mxL = Mx * cs + My * sn
        Dim myL = -Mx * sn + My * cs
        Return Tuple.Create(mxL, myL)
    End Function

    ''' <summary>
    ''' Evalúa todas las combinaciones del dado contra su TipoDado asignado.
    ''' Si usarPesoEstabilizante=True, el peso del dado y del suelo se suma
    ''' al P (compresión incrementa, tracción se reduce — lado seguro para
    ''' compresión, menos conservador para tracción, igual que en zapatas).
    ''' </summary>
    Public Function EvaluarDado(dado As Dado,
                                tipoDado As TipoDado,
                                tipoPiloteDefault As TipoPilote,
                                Optional contenedor As cPilotes = Nothing) As ResultadoDado

        Dim r As New ResultadoDado()
        If dado Is Nothing OrElse tipoDado Is Nothing OrElse tipoPiloteDefault Is Nothing Then
            Return r
        End If
        If tipoDado.Pilotes.Count = 0 Then Return r

        ' Peso propio opcional ─────────────────────────────────────
        Dim pesoExtra As Double = 0
        If contenedor IsNot Nothing AndAlso contenedor.UsarPesoEstabilizante Then
            Dim area = DadoGeometria.AreaPoligono(tipoDado.Poligono)
            Dim pesoDado = area * tipoDado.Espesor * 24.0                  ' γ_concreto = 24 kN/m³
            Dim pesoSuelo = area * Math.Max(0, contenedor.ProfundidadDesplante - tipoDado.Espesor) _
                            * contenedor.DensidadSuelo
            pesoExtra = pesoDado + pesoSuelo
        End If

        ' Filtro por combinaciones de diseño seleccionadas por el usuario.
        ' Patrón ARCO (igual que Zapatas/Columnas/Muros): el análisis solo
        ' considera las combos explícitamente marcadas. Si la lista está vacía
        ' se usan todas (fallback para casos de prueba). Se recomienda al
        ' usuario llenar la lista porque mezclar servicio y último distorsiona
        ' el Pu_max contra Q_admisible.
        Dim seleccion As HashSet(Of String) = Nothing
        If contenedor IsNot Nothing AndAlso
           contenedor.ListA_Combinaciones_Design IsNot Nothing AndAlso
           contenedor.ListA_Combinaciones_Design.Count > 0 Then
            seleccion = New HashSet(Of String)(contenedor.ListA_Combinaciones_Design,
                                               StringComparer.OrdinalIgnoreCase)
        End If

        Dim comboCompMax As Double = Double.MinValue
        Dim comboTraccMin As Double = Double.MaxValue
        Dim nombreCompGob As String = ""
        Dim nombreTracGob As String = ""

        For Each combo In dado.Combinaciones
            If seleccion IsNot Nothing AndAlso Not seleccion.Contains(combo.LoadCase) Then
                Continue For
            End If
            Dim mLocal = RotarMomentoAGlobalALocal(combo.MX, combo.MY, dado.AnguloPlanta)
            Dim fuerzas = CalcularFuerzaPilotes(tipoDado.Pilotes,
                                                combo.FZ + pesoExtra,
                                                mLocal.Item1, mLocal.Item2)
            Dim pMax = fuerzas.Max()
            Dim pMin = fuerzas.Min()

            r.PorCombinacion.Add(New ResultadoCombinacion With {
                .LoadCase = combo.LoadCase,
                .Fuerzas = fuerzas,
                .Pu_Max = pMax,
                .Pu_Min = pMin
            })

            If pMax > comboCompMax Then
                comboCompMax = pMax
                nombreCompGob = combo.LoadCase
            End If
            If pMin < comboTraccMin Then
                comboTraccMin = pMin
                nombreTracGob = combo.LoadCase
            End If
        Next

        r.Pu_Max_Compresion = comboCompMax
        r.Pu_Min_Traccion = comboTraccMin
        r.Combo_Gob_Compresion = nombreCompGob
        r.Combo_Gob_Traccion = nombreTracGob

        ' Agregado per-pilote: cruza todas las combinaciones y saca máx/mín que
        ' ve cada pilote individual, con el combo que lo gobernó.
        Dim n = tipoDado.Pilotes.Count
        For i = 0 To n - 1
            Dim rp As New ResumenPilote With {
                .Indice = i,
                .Numero = i + 1,
                .X_Local = tipoDado.Pilotes(i).X_Local,
                .Y_Local = tipoDado.Pilotes(i).Y_Local,
                .NombreTipoPilote = tipoDado.Pilotes(i).NombreTipoPilote,
                .Pu_Max = Double.MinValue,
                .Pu_Min = Double.MaxValue
            }
            For Each rc In r.PorCombinacion
                If rc.Fuerzas Is Nothing OrElse i >= rc.Fuerzas.Length Then Continue For
                Dim p = rc.Fuerzas(i)
                If p > rp.Pu_Max Then
                    rp.Pu_Max = p : rp.Combo_Max = rc.LoadCase
                End If
                If p < rp.Pu_Min Then
                    rp.Pu_Min = p : rp.Combo_Min = rc.LoadCase
                End If
            Next

            ' C/D por pilote: usa el TipoPilote del pilote (no el default del dado)
            Dim tpI = If(contenedor IsNot Nothing,
                         contenedor.BuscarTipoPilote(tipoDado.Pilotes(i).NombreTipoPilote),
                         tipoPiloteDefault)
            If tpI Is Nothing Then tpI = tipoPiloteDefault
            If tpI IsNot Nothing Then
                If tpI.Qc_Admisible > 0 AndAlso rp.Pu_Max > 0 Then
                    rp.CD_Compresion = Math.Round(tpI.Qc_Admisible / rp.Pu_Max, 2)
                End If
                If rp.Pu_Min < 0 Then
                    Dim pT = Math.Abs(rp.Pu_Min)
                    If tpI.Qt_Admisible > 0 Then
                        rp.CD_Traccion = Math.Round(tpI.Qt_Admisible / pT, 2)
                    Else
                        rp.CD_Traccion = 0.01
                    End If
                Else
                    rp.CD_Traccion = 0
                End If
            End If

            r.PorPilote.Add(rp)
        Next

        ' C/D compresión ─────────────────────────────────────────
        If tipoPiloteDefault.Qc_Admisible > 0 AndAlso comboCompMax > 0 Then
            r.CD_Compresion = Math.Round(tipoPiloteDefault.Qc_Admisible / comboCompMax, 2)
        End If

        ' C/D tracción: sentinela 0 si no hay tracción (C/D=0 → "—" en UI).
        ' Mismo criterio que PilaService.MinChequeoEsfuerzos: si Pu_min >= 0,
        ' no hay demanda de tracción, el chequeo no aplica.
        If comboTraccMin < 0 Then
            Dim pT = Math.Abs(comboTraccMin)
            If tipoPiloteDefault.Qt_Admisible > 0 Then
                r.CD_Traccion = Math.Round(tipoPiloteDefault.Qt_Admisible / pT, 2)
            Else
                ' Pilote sin capacidad a tracción + demanda a tracción = falla.
                r.CD_Traccion = 0.01   ' marcador de "no cumple por norma"
            End If
        Else
            r.CD_Traccion = 0           ' sentinela "no aplica"
        End If

        Return r
    End Function

    ''' <summary>
    ''' Corre EvaluarDado para cada Dado del contenedor y escribe los resultados
    ''' agregados (geotécnicos y estructurales) en el propio Dado para que la UI
    ''' los pueda bindear. Dado sin TipoDado o sin TipoPilote resuelto queda con
    ''' resultados en cero.
    ''' </summary>
    Public Sub EvaluarTodos(contenedor As cPilotes)
        If contenedor Is Nothing Then Return

        For Each d In contenedor.Dados
            d.Pu_Pilote_Max = 0
            d.Pu_Pilote_Min = 0
            d.CD_Compresion = 0
            d.CD_Traccion = 0
            d.CD_Compresion_Estructural = 0
            d.CD_Traccion_Estructural = 0
            d.Combo_Gob_Compresion = ""
            d.Combo_Gob_Traccion = ""

            Dim td = contenedor.BuscarTipoDado(d.NombreTipoDado)
            If td Is Nothing OrElse td.Pilotes.Count = 0 Then Continue For

            ' TipoPilote default: el primero que aparezca en el dado.
            Dim nombreTp = td.Pilotes.First().NombreTipoPilote
            Dim tp = contenedor.BuscarTipoPilote(nombreTp)
            If tp Is Nothing Then Continue For

            Dim r = EvaluarDado(d, td, tp, contenedor)
            d.Pu_Pilote_Max = r.Pu_Max_Compresion
            d.Pu_Pilote_Min = r.Pu_Min_Traccion
            d.CD_Compresion = r.CD_Compresion
            d.CD_Traccion = r.CD_Traccion
            d.Combo_Gob_Compresion = r.Combo_Gob_Compresion
            d.Combo_Gob_Traccion = r.Combo_Gob_Traccion

            ' Chequeo estructural axial — se corre sobre los resultados per-pilote
            ' que ya calculó EvaluarDado, usando el TipoPilote de cada pilote.
            Dim rEstr = EvaluarEstructuralDado(r, td, contenedor)
            d.CD_Compresion_Estructural = rEstr.CD_Compresion_Minima
            d.CD_Traccion_Estructural = rEstr.CD_Traccion_Minima
        Next
    End Sub

    ''' <summary>
    ''' Resultado del chequeo estructural axial de un TipoPilote. Todas las
    ''' magnitudes de área en mm² y fuerzas en kN. Convención C/D habitual del
    ''' programa: 0 = "no aplica" (no se pudo evaluar o no hay demanda).
    ''' </summary>
    Public Class ResultadoEstructuralPilote
        Public Property Ag_mm2 As Double
        Public Property Ast_mm2 As Double
        Public Property CuantiaPct As Double
        Public Property PhiPn_kN As Double
        Public Property PhiTn_kN As Double
        Public Property Pu_max_kN As Double
        Public Property Pu_min_kN As Double
        ''' <summary>φPn / Pu_max. Sentinela 0 = "no aplica".</summary>
        Public Property CD_Compresion As Double
        ''' <summary>φTn / |Pu_min|. Sentinela 0 = "no aplica" (sin tracción).</summary>
        Public Property CD_Traccion As Double
        Public Property Advertencias As New List(Of String)
    End Class

    ''' <summary>
    ''' Agregado por dado: busca el chequeo estructural más crítico entre todos
    ''' los TipoPilote presentes en el dado. Un dado puede mezclar tipos (p.ej.
    ''' pilotes perimetrales + micropilotes en el centro) y el C/D que gobierna
    ''' es el peor de todos.
    ''' </summary>
    Public Class ResultadoEstructuralDado
        Public Property PorTipoPilote As New Dictionary(Of String, ResultadoEstructuralPilote)
        Public Property CD_Compresion_Minima As Double
        Public Property CD_Traccion_Minima As Double
        Public Property Advertencias As New List(Of String)
    End Class

    ''' <summary>
    ''' Chequeo estructural axial de un TipoPilote circular. Fórmulas NSR-10:
    '''
    '''   Ag  = π·D²/4         (área bruta de concreto, mm²)
    '''   Ast = N·Ab           (área total refuerzo longitudinal, mm²)
    '''   φPn = 0.80·φ·(0.85·fc·(Ag − Ast) + fy·Ast)   [C.10.3.6.2, estribos cerrados]
    '''   φTn = 0.90·fy·Ast                             [C.9.3.2.1, tracción axial]
    '''
    ''' φ = 0.65 (estribos) por default. Cuantía válida: 0.01·Ag ≤ Ast ≤ 0.08·Ag
    ''' (C.10.9.1). Fuera del rango se emite advertencia pero se calcula igual.
    ''' Entradas inválidas (D≤0, fc≤0, fy≤0, N≤0) devuelven resultado con C/D=0
    ''' y advertencia descriptiva.
    '''
    ''' Para el chequeo biaxial con M en la cabeza del pilote (DI circular) ver
    ''' v1.1 — este v1 cubre el caso dominante que es compresión/tracción axial
    ''' donde el cabezal rígido ya absorbió el momento en el grupo.
    ''' </summary>
    Public Function EvaluarEstructuralPilote(tp As TipoPilote,
                                             Pu_max_kN As Double,
                                             Pu_min_kN As Double) As ResultadoEstructuralPilote
        Dim r As New ResultadoEstructuralPilote With {
            .Pu_max_kN = Pu_max_kN,
            .Pu_min_kN = Pu_min_kN
        }
        If tp Is Nothing Then
            r.Advertencias.Add("No se resolvió el TipoPilote.")
            Return r
        End If
        If tp.Diametro <= 0 Then
            r.Advertencias.Add("Diámetro del pilote en cero.")
            Return r
        End If
        If tp.Fc <= 0 OrElse tp.Fy <= 0 Then
            r.Advertencias.Add($"Falta fc o fy en el TipoPilote '{tp.Nombre}'.")
            Return r
        End If
        If tp.NumBarras_Long <= 0 OrElse String.IsNullOrEmpty(tp.Diametro_Barra_Long) Then
            r.Advertencias.Add($"Falta refuerzo longitudinal en el TipoPilote '{tp.Nombre}'.")
            Return r
        End If

        ' Áreas (mm²). Diametro está en metros.
        Dim D_mm = tp.Diametro * 1000.0
        r.Ag_mm2 = Math.PI * D_mm * D_mm / 4.0
        r.Ast_mm2 = tp.NumBarras_Long * Funciones_00_Varias.AreaRefuerzo(tp.Diametro_Barra_Long)
        r.CuantiaPct = 100.0 * r.Ast_mm2 / r.Ag_mm2

        If r.Ast_mm2 <= 0 Then
            r.Advertencias.Add("Área de refuerzo en cero — revisa diámetro de barra.")
            Return r
        End If
        If r.CuantiaPct < 1.0 Then
            r.Advertencias.Add($"Cuantía {r.CuantiaPct:0.00}% < 1% (NSR-10 C.10.9.1).")
        End If
        If r.CuantiaPct > 8.0 Then
            r.Advertencias.Add($"Cuantía {r.CuantiaPct:0.00}% > 8% (NSR-10 C.10.9.1).")
        End If

        ' φPn y φTn. fc, fy en MPa → N/mm². Pn en N → ÷1000 para kN.
        Const phi_c As Double = 0.65          ' φ para estribos (conservador).
        Const factor_axial As Double = 0.80   ' Factor por carga axial máxima (C.10.3.6.2).
        Const phi_t As Double = 0.90          ' φ para tracción (C.9.3.2.1).

        Dim Pn_N = 0.85 * tp.Fc * (r.Ag_mm2 - r.Ast_mm2) + tp.Fy * r.Ast_mm2
        r.PhiPn_kN = factor_axial * phi_c * Pn_N / 1000.0

        Dim Tn_N = tp.Fy * r.Ast_mm2
        r.PhiTn_kN = phi_t * Tn_N / 1000.0

        ' C/D ───────────────────────────────────────────────────────
        If Pu_max_kN > 0 AndAlso r.PhiPn_kN > 0 Then
            r.CD_Compresion = Math.Round(r.PhiPn_kN / Pu_max_kN, 2)
        End If
        ' Tracción: sentinela 0 cuando Pu_min >= 0 (no hay demanda).
        If Pu_min_kN < 0 AndAlso r.PhiTn_kN > 0 Then
            r.CD_Traccion = Math.Round(r.PhiTn_kN / Math.Abs(Pu_min_kN), 2)
        End If

        Return r
    End Function

    ''' <summary>
    ''' Chequeo estructural agregado por dado. Para cada TipoPilote presente en
    ''' el dado se toma el peor Pu_max y el peor Pu_min del conjunto de pilotes
    ''' de ese tipo (resultado de EvaluarDado.PorPilote). El C/D mínimo entre
    ''' todos los tipos gobierna y queda en CD_Compresion_Minima / CD_Traccion_Minima.
    ''' </summary>
    Public Function EvaluarEstructuralDado(resultado As ResultadoDado,
                                           tipoDado As TipoDado,
                                           contenedor As cPilotes) As ResultadoEstructuralDado
        Dim r As New ResultadoEstructuralDado()
        If resultado Is Nothing OrElse tipoDado Is Nothing OrElse contenedor Is Nothing Then
            Return r
        End If
        If resultado.PorPilote Is Nothing OrElse resultado.PorPilote.Count = 0 Then Return r

        ' Agrupar los pilotes por NombreTipoPilote y encontrar las demandas peores.
        Dim peorComp As Double = Double.MaxValue
        Dim peorTracc As Double = Double.MaxValue    ' Sentinela: si queda MaxValue → no hay tracción.
        Dim algunoConDemanda As Boolean = False

        For Each grupo In resultado.PorPilote.GroupBy(Function(p) p.NombreTipoPilote)
            Dim tp = contenedor.BuscarTipoPilote(grupo.Key)
            If tp Is Nothing Then Continue For

            Dim puMaxGrp = grupo.Max(Function(p) p.Pu_Max)
            Dim puMinGrp = grupo.Min(Function(p) p.Pu_Min)
            Dim rPilote = EvaluarEstructuralPilote(tp, puMaxGrp, puMinGrp)
            r.PorTipoPilote(grupo.Key) = rPilote

            For Each adv In rPilote.Advertencias
                r.Advertencias.Add($"[{grupo.Key}] {adv}")
            Next

            If rPilote.CD_Compresion > 0 AndAlso rPilote.CD_Compresion < peorComp Then
                peorComp = rPilote.CD_Compresion
                algunoConDemanda = True
            End If
            If rPilote.CD_Traccion > 0 AndAlso rPilote.CD_Traccion < peorTracc Then
                peorTracc = rPilote.CD_Traccion
            End If
        Next

        r.CD_Compresion_Minima = If(algunoConDemanda, peorComp, 0.0)
        r.CD_Traccion_Minima = If(peorTracc < Double.MaxValue, peorTracc, 0.0)
        Return r
    End Function

    ''' <summary>
    ''' Validación geométrica: pilotes dentro del polígono con recubrimiento y
    ''' separación mínima entre pilotes.
    ''' </summary>
    Public Function ValidarGeometria(tipoDado As TipoDado,
                                     contenedor As cPilotes) As List(Of String)
        Dim errores As New List(Of String)
        If tipoDado Is Nothing Then Return errores

        If tipoDado.Pilotes Is Nothing OrElse tipoDado.Pilotes.Count = 0 Then
            errores.Add("El dado no tiene pilotes definidos.")
            Return errores
        End If

        If tipoDado.Poligono Is Nothing OrElse tipoDado.Poligono.Count < 3 Then
            errores.Add("El polígono del dado necesita al menos 3 vértices.")
        End If

        ' Pilote dentro del polígono con holgura de recubrimiento
        For i = 0 To tipoDado.Pilotes.Count - 1
            Dim p = tipoDado.Pilotes(i)
            Dim tp = contenedor?.BuscarTipoPilote(p.NombreTipoPilote)
            Dim radioP = If(tp IsNot Nothing, tp.Diametro * 0.5, 0.15)
            Dim holguraReq = radioP + tipoDado.Recubrimiento

            Dim punto = New Punto2D(p.X_Local, p.Y_Local)
            If Not DadoGeometria.PuntoDentroPoligono(punto, tipoDado.Poligono) Then
                errores.Add($"El pilote #{i + 1} está fuera del polígono del dado.")
            Else
                Dim dBorde = DadoGeometria.DistanciaAlBordePoligono(punto, tipoDado.Poligono)
                If dBorde < holguraReq Then
                    errores.Add($"El pilote #{i + 1} queda a {dBorde * 100:0} cm del borde (se requieren {holguraReq * 100:0} cm).")
                End If
            End If
        Next

        ' Separación mínima entre pilotes (3·d del pilote mayor)
        Dim dMax As Double = 0.3
        For Each p In tipoDado.Pilotes
            Dim tp = contenedor?.BuscarTipoPilote(p.NombreTipoPilote)
            If tp IsNot Nothing AndAlso tp.Diametro > dMax Then dMax = tp.Diametro
        Next
        Dim sMin = tipoDado.FactorSeparacionMinima * dMax
        For Each par In DadoGeometria.PilotesDemasiadoCerca(tipoDado.Pilotes, sMin)
            errores.Add($"Pilotes #{par.Item1 + 1} y #{par.Item2 + 1} están a menos de {sMin * 100:0} cm (mínimo {tipoDado.FactorSeparacionMinima:0}·d).")
        Next

        Return errores
    End Function

    ''' <summary>
    ''' Genera un layout de pilotes dentro de un polígono rectangular n × m.
    ''' Útil para que la UI arranque con algo sensato. El caller sobreescribe
    ''' tipoDado.Pilotes.
    ''' </summary>
    Public Function GenerarLayoutRectangular(filas As Integer, columnas As Integer,
                                             separacion As Double, nombreTipoPilote As String) _
                                             As List(Of PilotePosicion)
        Dim lista As New List(Of PilotePosicion)
        If filas < 1 OrElse columnas < 1 Then Return lista
        Dim x0 = -(columnas - 1) * separacion * 0.5
        Dim y0 = -(filas - 1) * separacion * 0.5
        For i = 0 To filas - 1
            For j = 0 To columnas - 1
                lista.Add(New PilotePosicion(x0 + j * separacion, y0 + i * separacion, nombreTipoPilote))
            Next
        Next
        Return lista
    End Function

    ''' <summary>
    ''' Genera un layout circular de n pilotes a radio r. Útil para pilas
    ''' circulares o grupos compactos con simetría radial.
    ''' </summary>
    Public Function GenerarLayoutCircular(numPilotes As Integer, radio As Double,
                                          nombreTipoPilote As String) As List(Of PilotePosicion)
        Dim lista As New List(Of PilotePosicion)
        If numPilotes < 1 Then Return lista
        For i = 0 To numPilotes - 1
            Dim theta = 2 * Math.PI * i / numPilotes - Math.PI * 0.5
            lista.Add(New PilotePosicion(radio * Math.Cos(theta),
                                         radio * Math.Sin(theta),
                                         nombreTipoPilote))
        Next
        Return lista
    End Function

    ''' <summary>
    ''' Genera un polígono regular de n lados con radio circunscrito r, centrado
    ''' en el origen. Para hexágono/pentágono/octágono.
    ''' </summary>
    Public Function GenerarPoligonoRegular(numLados As Integer, radio As Double) As List(Of Punto2D)
        Dim lista As New List(Of Punto2D)
        If numLados < 3 Then Return lista
        For i = 0 To numLados - 1
            Dim theta = 2 * Math.PI * i / numLados - Math.PI * 0.5
            lista.Add(New Punto2D(radio * Math.Cos(theta), radio * Math.Sin(theta)))
        Next
        Return lista
    End Function

    ''' <summary>Genera un polígono rectangular centrado en el origen.</summary>
    Public Function GenerarPoligonoRectangular(ancho As Double, alto As Double) As List(Of Punto2D)
        Dim a = ancho * 0.5, b = alto * 0.5
        Return New List(Of Punto2D) From {
            New Punto2D(-a, -b),
            New Punto2D(a, -b),
            New Punto2D(a, b),
            New Punto2D(-a, b)
        }
    End Function

End Module
