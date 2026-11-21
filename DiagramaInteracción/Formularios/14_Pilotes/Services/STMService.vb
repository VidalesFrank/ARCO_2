''' <summary>
''' Servicio de modelo puntal-tensor (NSR-10 C.23) para dados de pilotes.
''' Sin dependencias de UI, testeable desde ARCO.Tests.
'''
''' Convenciones:
'''   - Nodo superior CCC: centroide del polígono a Z = Espesor (bajo la columna).
'''   - Nodo inferior CCT: cada pilote a Z = 0, nivel del acero inferior.
'''   - Altura efectiva d = Espesor − Recubrimiento (distancia del nodo sup al
'''     centroide del acero inferior).
'''   - Ejes locales del dado (origen en centroide), X horizontal, Y horizontal.
'''   - Cada pilote se evalúa con su Pu_Max individual (envolvente).
'''
''' φ = 0.75 (todos los chequeos, NSR-10 C.9.3.2.6). Ningún C/D es 0 por
''' sentinela "no aplica" — si el pilote no tiene compresión su puntal no se
''' crea y ya está. Sentinela solo cuando la geometría es inválida.
''' </summary>
Public Module STMService

    Public Const PHI As Double = 0.75

    ''' <summary>
    ''' Resultado del análisis STM. Incluye el modelo poblado con chequeos y
    ''' los agregados por categoría (peor C/D entre puntales, tensores, nodos)
    ''' que luego se guardan en Dado.CD_STM_*.
    ''' </summary>
    Public Class ResultadoSTM
        Public Property Modelo As ModeloSTM
        Public Property CD_Puntal_Minimo As Double
        Public Property CD_Tensor_Minimo As Double
        Public Property CD_Nodo_Minimo As Double
        Public Property As_X_cm2 As Double          ' requerido por STM
        Public Property As_Y_cm2 As Double          ' requerido por STM
        Public Property As_X_Provisto_cm2 As Double ' calculado del refuerzo del dado
        Public Property As_Y_Provisto_cm2 As Double
        Public Property CD_Tensor_X As Double        ' As_provisto / As_requerido
        Public Property CD_Tensor_Y As Double
        Public Property Advertencias As New List(Of String)
        ''' <summary>True si la relación L_claro/h sugiere método de sección en lugar de STM.</summary>
        Public Property DadoEsbelto As Boolean
    End Class

    ''' <summary>
    ''' Genera el modelo STM a partir del resultado del cálculo geotécnico.
    ''' Usa el Pu_Max de cada pilote individual (envolvente de combinaciones
    ''' de diseño). Pilotes con Pu_Max ≤ 0 no generan puntal (no hay compresión
    ''' que transmitir).
    ''' </summary>
    Public Function GenerarModelo(tipoDado As TipoDado,
                                  resultado As PiloteService.ResultadoDado,
                                  contenedor As cPilotes) As ModeloSTM
        Dim m As New ModeloSTM()
        If tipoDado Is Nothing OrElse resultado Is Nothing Then Return m
        If tipoDado.Pilotes.Count = 0 OrElse resultado.PorPilote.Count = 0 Then Return m

        Dim d = Math.Max(0.01, tipoDado.Espesor - tipoDado.Recubrimiento)
        m.D_Efectivo = d

        ' Nodo superior en el centroide del polígono a Z = Espesor.
        ' Origen local del polígono ya está en el centroide, así que (0,0).
        m.NodoSuperior = New NodoSTM With {
            .Nombre = "N_sup",
            .X = 0, .Y = 0, .Z = tipoDado.Espesor,
            .Tipo = eTipoNodoSTM.CCC,
            .AreaNodal = tipoDado.LadoColumna_X * tipoDado.LadoColumna_Y,
            .BetaN = 1.0
        }

        ' Nodos inferiores: uno por pilote, a Z = Recubrimiento (centroide del
        ' acero inferior). Esto da una distancia vertical entre nodos = d, que
        ' es la geometría que el método usa en todas sus fórmulas.
        For Each rp In resultado.PorPilote
            Dim nInf As New NodoSTM With {
                .Nombre = $"N_p{rp.Numero}",
                .X = rp.X_Local, .Y = rp.Y_Local, .Z = tipoDado.Recubrimiento,
                .Tipo = eTipoNodoSTM.CCT,
                .BetaN = 0.80
            }
            Dim tp = contenedor?.BuscarTipoPilote(rp.NombreTipoPilote)
            Dim diam = If(tp IsNot Nothing, tp.Diametro, 0.6)
            nInf.AreaNodal = Math.PI * diam * diam / 4.0
            m.NodosInferiores.Add(nInf)
        Next

        ' Puntales: nodo sup → cada nodo inf con Pu > 0.
        For i = 0 To resultado.PorPilote.Count - 1
            Dim rp = resultado.PorPilote(i)
            If rp.Pu_Max <= 0 Then Continue For
            Dim nInf = m.NodosInferiores(i)
            Dim dx = nInf.X - m.NodoSuperior.X
            Dim dy = nInf.Y - m.NodoSuperior.Y
            Dim dz = m.NodoSuperior.Z - nInf.Z   ' positivo
            Dim rHoriz = Math.Sqrt(dx * dx + dy * dy)
            Dim L = Math.Sqrt(rHoriz * rHoriz + dz * dz)
            Dim sinTheta = dz / L
            Dim angGrados = Math.Asin(sinTheta) * 180.0 / Math.PI

            m.Puntales.Add(New PuntalSTM With {
                .NodoSup = m.NodoSuperior,
                .NodoInf = nInf,
                .Longitud = L,
                .Angulo = angGrados,
                .Fuerza = If(sinTheta > 1.0E-6, rp.Pu_Max / sinTheta, rp.Pu_Max)
            })
        Next

        ' Tensores: agrupo pilotes por fila (Y similar) y por columna (X similar).
        ' La fuerza de un tensor efectivo es Pu_pilote·|coord_local|/d del peor
        ' pilote de ese tramo — es la componente horizontal del puntal que el
        ' tensor debe atar. Tolerancia de agrupación: 5% del vano máximo.
        Dim tol = CalcularToleranciaAgrupacion(tipoDado.Pilotes)
        GenerarTensores(m, resultado, tol, "X", d)
        GenerarTensores(m, resultado, tol, "Y", d)

        ' Relación L_claro/h (orientativa): claro mayor entre pilotes vs espesor.
        Dim claroMax As Double = 0
        For i = 0 To tipoDado.Pilotes.Count - 1
            For j = i + 1 To tipoDado.Pilotes.Count - 1
                Dim pi = tipoDado.Pilotes(i), pj = tipoDado.Pilotes(j)
                Dim dd = Math.Sqrt((pi.X_Local - pj.X_Local) ^ 2 + (pi.Y_Local - pj.Y_Local) ^ 2)
                If dd > claroMax Then claroMax = dd
            Next
        Next
        m.RelacionLH = If(tipoDado.Espesor > 0, claroMax / tipoDado.Espesor, 0)

        Return m
    End Function

    Private Function CalcularToleranciaAgrupacion(pilotes As List(Of PilotePosicion)) As Double
        If pilotes.Count < 2 Then Return 0.05
        Dim vanoMax As Double = 0
        For i = 0 To pilotes.Count - 1
            For j = i + 1 To pilotes.Count - 1
                Dim dd = Math.Sqrt((pilotes(i).X_Local - pilotes(j).X_Local) ^ 2 +
                                   (pilotes(i).Y_Local - pilotes(j).Y_Local) ^ 2)
                If dd > vanoMax Then vanoMax = dd
            Next
        Next
        Return Math.Max(0.05, vanoMax * 0.05)
    End Function

    ''' <summary>
    ''' Agrupa pilotes por línea (en Y si direccion="X", en X si direccion="Y")
    ''' y crea tensores entre consecutivos en la línea. La fuerza del tensor
    ''' es la componente horizontal del puntal del pilote más cargado del
    ''' tramo, proyectada en la dirección correspondiente.
    ''' </summary>
    Private Sub GenerarTensores(m As ModeloSTM,
                                resultado As PiloteService.ResultadoDado,
                                tol As Double, direccion As String, d As Double)
        ' coordLinea = Y si tensor X, X si tensor Y (define la línea)
        ' coordPos   = X si tensor X, Y si tensor Y (ordena pilotes en la línea)
        Dim esTensorX = (direccion = "X")
        Dim grupos As New Dictionary(Of Integer, List(Of Integer))

        For i = 0 To resultado.PorPilote.Count - 1
            Dim rp = resultado.PorPilote(i)
            Dim coordLinea = If(esTensorX, rp.Y_Local, rp.X_Local)
            ' Claves agrupadas por redondeo a tol.
            Dim key = CInt(Math.Round(coordLinea / tol))
            If Not grupos.ContainsKey(key) Then grupos(key) = New List(Of Integer)
            grupos(key).Add(i)
        Next

        For Each g In grupos.Values
            If g.Count < 2 Then Continue For
            ' Ordenar por coordenada en la dirección del tensor.
            Dim ordenados = g.OrderBy(Function(idx)
                                          Dim rp = resultado.PorPilote(idx)
                                          Return If(esTensorX, rp.X_Local, rp.Y_Local)
                                      End Function).ToList()
            For k = 0 To ordenados.Count - 2
                Dim idxA = ordenados(k), idxB = ordenados(k + 1)
                Dim rpA = resultado.PorPilote(idxA)
                Dim rpB = resultado.PorPilote(idxB)
                ' Fuerza del tensor = la mayor componente H del tramo (seguro).
                Dim hA = Math.Abs(ComponenteHorizontal(rpA, esTensorX, d))
                Dim hB = Math.Abs(ComponenteHorizontal(rpB, esTensorX, d))
                Dim F = Math.Max(hA, hB)
                If F <= 0 Then Continue For
                Dim nA = m.NodosInferiores(idxA)
                Dim nB = m.NodosInferiores(idxB)
                Dim L = Math.Sqrt((nA.X - nB.X) ^ 2 + (nA.Y - nB.Y) ^ 2)
                Dim tensor = New TensorSTM With {
                    .NodoInfA = nA,
                    .NodoInfB = nB,
                    .Direccion = direccion,
                    .Fuerza = F,
                    .Longitud = L
                }
                If esTensorX Then m.TensoresX.Add(tensor) Else m.TensoresY.Add(tensor)
            Next
        Next
    End Sub

    Private Function ComponenteHorizontal(rp As PiloteService.ResumenPilote,
                                          esTensorX As Boolean, d As Double) As Double
        If rp.Pu_Max <= 0 Then Return 0
        ' Hx = Pu · |x_local| / d — componente horizontal del puntal en X.
        Dim coord = If(esTensorX, rp.X_Local, rp.Y_Local)
        Return rp.Pu_Max * coord / d
    End Function

    ''' <summary>
    ''' Evalúa el modelo STM contra NSR-10 C.23. Rellena los campos de
    ''' resistencia y C/D de puntales y nodos; calcula As requerido de
    ''' tensores. Devuelve el ResultadoSTM con los mínimos agregados.
    ''' </summary>
    Public Function Evaluar(m As ModeloSTM, tipoDado As TipoDado) As ResultadoSTM
        Dim r As New ResultadoSTM With {.Modelo = m}
        If m Is Nothing OrElse tipoDado Is Nothing Then Return r
        If m.NodoSuperior Is Nothing Then Return r

        Dim fc = tipoDado.Fc
        Dim fy = tipoDado.Fy
        Dim bDado = tipoDado.Espesor
        Dim betaS = tipoDado.Beta_Puntal

        If fc <= 0 OrElse fy <= 0 Then
            r.Advertencias.Add($"TipoDado '{tipoDado.Nombre}' sin fc/fy válidos.")
            Return r
        End If

        ' ─── Puntales ──────────────────────────────────────────────
        Dim cdPuntalMin As Double = Double.MaxValue
        For Each p In m.Puntales
            ' ws: ancho del puntal usando la geometría del nodo superior
            ' proyectada perpendicular al eje del puntal.
            '    ws = lb · sin(θ) + ha · cos(θ)  (NSR-10 C.23.9.4)
            ' lb = lado de la columna en el plano del puntal (usamos LadoColumna_X
            ' para puntales con rumbo mayormente en X, LadoColumna_Y para los
            ' demás — usamos la proyección más representativa).
            Dim theta = p.Angulo * Math.PI / 180.0
            Dim dx = p.NodoInf.X - p.NodoSup.X
            Dim dy = p.NodoInf.Y - p.NodoSup.Y
            Dim rHor = Math.Sqrt(dx * dx + dy * dy)
            Dim fracX = If(rHor > 0, Math.Abs(dx) / rHor, 1.0)
            Dim lbCol = tipoDado.LadoColumna_X * fracX + tipoDado.LadoColumna_Y * (1 - fracX)
            Dim haNodo = tipoDado.Recubrimiento * 2.0  ' altura del nodo CCT ≈ 2·rec
            Dim ws = lbCol * Math.Sin(theta) + haNodo * Math.Cos(theta)
            p.AnchoPuntal = ws
            p.EspesorPuntal = bDado
            p.Acs = ws * bDado
            p.BetaS = betaS
            p.FCU = 0.85 * betaS * fc
            ' φFns [kN] = φ · (fcu [MPa] · Acs [m²] · 1000)
            p.PhiFns = PHI * p.FCU * p.Acs * 1000.0
            p.CD = If(p.Fuerza > 0, Math.Round(p.PhiFns / p.Fuerza, 2), 0)
            If p.CD > 0 AndAlso p.CD < cdPuntalMin Then cdPuntalMin = p.CD
        Next
        r.CD_Puntal_Minimo = If(cdPuntalMin < Double.MaxValue, cdPuntalMin, 0)

        ' ─── Tensores ──────────────────────────────────────────────
        ' As [cm²] = F [kN] · 1000 / (φ · fy [MPa] · 100)
        '          = F · 10 / (φ · fy)
        Dim maxAsX As Double = 0
        For Each t In m.TensoresX
            t.As_Requerida_cm2 = If(t.Fuerza > 0, t.Fuerza * 10.0 / (PHI * fy), 0)
            If t.As_Requerida_cm2 > maxAsX Then maxAsX = t.As_Requerida_cm2
        Next
        Dim maxAsY As Double = 0
        For Each t In m.TensoresY
            t.As_Requerida_cm2 = If(t.Fuerza > 0, t.Fuerza * 10.0 / (PHI * fy), 0)
            If t.As_Requerida_cm2 > maxAsY Then maxAsY = t.As_Requerida_cm2
        Next
        r.As_X_cm2 = maxAsX
        r.As_Y_cm2 = maxAsY

        ' As provisto en cada dirección (barras × área unitaria), pasado a cm².
        r.As_X_Provisto_cm2 = tipoDado.NumBarras_Inf_X *
                              Funciones_00_Varias.AreaRefuerzo(tipoDado.Diametro_Barras_Inf_X) / 100.0
        r.As_Y_Provisto_cm2 = tipoDado.NumBarras_Inf_Y *
                              Funciones_00_Varias.AreaRefuerzo(tipoDado.Diametro_Barras_Inf_Y) / 100.0

        ' C/D del tensor por dirección = As_provisto / As_requerido.
        ' Sentinelas (= 0, "no aplica"):
        '  · sin tensores en esa dirección (no hay demanda)
        '  · con tensores pero sin refuerzo todavía → C/D = 0 marca "no cumple"
        '    para que el usuario vea que aún le falta ingresar acero.
        If maxAsX > 0 Then
            r.CD_Tensor_X = If(r.As_X_Provisto_cm2 > 0,
                               Math.Round(r.As_X_Provisto_cm2 / maxAsX, 2), 0.01)
        End If
        If maxAsY > 0 Then
            r.CD_Tensor_Y = If(r.As_Y_Provisto_cm2 > 0,
                               Math.Round(r.As_Y_Provisto_cm2 / maxAsY, 2), 0.01)
        End If

        ' C/D agregado del tensor = mínimo entre X e Y (el que gobierna).
        Dim cds = {r.CD_Tensor_X, r.CD_Tensor_Y}.Where(Function(v) v > 0).ToList()
        r.CD_Tensor_Minimo = If(cds.Count > 0, cds.Min(), 0)

        ' ─── Nodos ─────────────────────────────────────────────────
        ' Nodo superior CCC: toma la suma de componentes verticales (= ΣPu).
        ' Nodo inferior CCT: toma la componente vertical del puntal que llega
        ' (= Pu del pilote de abajo).
        Dim sumPu As Double = 0
        For Each p In m.Puntales
            sumPu += p.Fuerza * Math.Sin(p.Angulo * Math.PI / 180.0)
        Next
        m.NodoSuperior.FuerzaCompresion = sumPu
        m.NodoSuperior.FCU = 0.85 * m.NodoSuperior.BetaN * fc
        m.NodoSuperior.PhiFn = PHI * m.NodoSuperior.FCU * m.NodoSuperior.AreaNodal * 1000.0
        m.NodoSuperior.CD = If(sumPu > 0, Math.Round(m.NodoSuperior.PhiFn / sumPu, 2), 0)

        Dim cdNodoMin As Double = If(m.NodoSuperior.CD > 0, m.NodoSuperior.CD, Double.MaxValue)
        For i = 0 To m.NodosInferiores.Count - 1
            Dim n = m.NodosInferiores(i)
            ' Buscamos el puntal que termina en este nodo.
            Dim pOwner = m.Puntales.FirstOrDefault(Function(pp) pp.NodoInf Is n)
            If pOwner Is Nothing Then
                n.FuerzaCompresion = 0
                n.FCU = 0.85 * n.BetaN * fc
                n.PhiFn = PHI * n.FCU * n.AreaNodal * 1000.0
                n.CD = 0
                Continue For
            End If
            n.FuerzaCompresion = pOwner.Fuerza * Math.Sin(pOwner.Angulo * Math.PI / 180.0)
            n.FCU = 0.85 * n.BetaN * fc
            n.PhiFn = PHI * n.FCU * n.AreaNodal * 1000.0
            n.CD = If(n.FuerzaCompresion > 0, Math.Round(n.PhiFn / n.FuerzaCompresion, 2), 0)
            If n.CD > 0 AndAlso n.CD < cdNodoMin Then cdNodoMin = n.CD
        Next
        r.CD_Nodo_Minimo = If(cdNodoMin < Double.MaxValue, cdNodoMin, 0)

        ' ─── Aplicabilidad del método ──────────────────────────────
        ' Si L_claro/h > 2 el dado es "esbelto" y el método de sección es más
        ' apropiado. STM queda del lado seguro pero puede sobredimensionar.
        r.DadoEsbelto = (m.RelacionLH > 2.0)
        If r.DadoEsbelto Then
            r.Advertencias.Add(
                $"Relación L_claro/h = {m.RelacionLH:0.00} > 2.0 — el dado es esbelto, " &
                "el método de sección (flexión + cortante) puede ser más apropiado.")
        End If

        Return r
    End Function

    ''' <summary>
    ''' Evalúa STM para todos los dados del contenedor y persiste los peores
    ''' C/D en Dado.CD_STM_*. Diseñado para llamarse después de
    ''' PiloteService.EvaluarTodos — depende de que PorPilote esté calculado.
    ''' </summary>
    Public Sub EvaluarTodos(contenedor As cPilotes)
        If contenedor Is Nothing Then Return

        For Each d In contenedor.Dados
            d.CD_STM_Puntal = 0
            d.CD_STM_Tensor = 0
            d.CD_STM_Nodo = 0
            d.As_STM_X_cm2 = 0
            d.As_STM_Y_cm2 = 0

            Dim td = contenedor.BuscarTipoDado(d.NombreTipoDado)
            If td Is Nothing OrElse td.Pilotes.Count = 0 Then Continue For
            Dim tp = contenedor.BuscarTipoPilote(td.Pilotes.First().NombreTipoPilote)
            If tp Is Nothing Then Continue For

            Dim rGeot = PiloteService.EvaluarDado(d, td, tp, contenedor)
            Dim modelo = GenerarModelo(td, rGeot, contenedor)
            Dim rSTM = Evaluar(modelo, td)

            d.CD_STM_Puntal = rSTM.CD_Puntal_Minimo
            d.CD_STM_Tensor = rSTM.CD_Tensor_Minimo
            d.CD_STM_Nodo = rSTM.CD_Nodo_Minimo
            d.As_STM_X_cm2 = rSTM.As_X_cm2
            d.As_STM_Y_cm2 = rSTM.As_Y_cm2
            d.CD_STM_Tensor_X = rSTM.CD_Tensor_X
            d.CD_STM_Tensor_Y = rSTM.CD_Tensor_Y
        Next
    End Sub

End Module
