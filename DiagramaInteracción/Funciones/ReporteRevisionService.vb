Imports System.IO
Imports DocumentFormat.OpenXml.Packaging
Imports DocumentFormat.OpenXml.Wordprocessing
Imports ARCO.eNumeradores

''' <summary>Opciones capturadas en <see cref="Form_Reporte_Revision"/> para generar el reporte.</summary>
Public Class OpcionesReporteRevision
    Public Property Fecha As Date = Date.Today
    Public Property CodigoProyecto As String = ""
    Public Property DestinatarioNombre As String = ""
    Public Property DestinatarioCargo As String = ""
    Public Property Cliente As String = ""
    Public Property AsuntoProyecto As String = ""

    Public Property IncluirCimentaciones As Boolean = True
    Public Property IncluirMuros As Boolean = True
    Public Property IncluirVigas As Boolean = True
    Public Property IncluirLosas As Boolean = True
    Public Property IncluirDetallesPlanos As Boolean = True

    Public Property DetallesPlanos As New List(Of String)
End Class

''' <summary>
''' Genera el "Reporte de Revisión" (.docx) con el estilo de EstrucMed a partir de los resultados
''' ya calculados en un <see cref="Proyecto"/> de ARCO. Solo lista, por verificación, los elementos
''' con capacidad/demanda (C/D) inferior a <see cref="UMBRAL_CD"/> — igual que el reporte manual de
''' referencia (FormatosRevisión\P53844_C008_ReporteRevisión_E1A.docx).
''' </summary>
Public Class ReporteRevisionService

    Private Const UMBRAL_CD As Double = 0.9

    Private _numTabla As Integer = 0

    Private Shared Function EsConforme(ratio As Double) As Boolean
        Return ratio >= UMBRAL_CD
    End Function

    Private Shared Function FCD(valor As Double) As String
        Return valor.ToString("0.00")
    End Function

    Private Shared Function FNum(valor As Double, Optional decimales As Integer = 2) As String
        Return valor.ToString("0." & New String("0"c, decimales))
    End Function

    ' =========================================================================
    '  Punto de entrada
    ' =========================================================================
    Public Shared Sub GenerarReporte(proyecto As Proyecto, opciones As OpcionesReporteRevision, rutaSalida As String)
        Dim servicio As New ReporteRevisionService()
        servicio.Generar(proyecto, opciones, rutaSalida)
    End Sub

    Private Sub Generar(proyecto As Proyecto, opciones As OpcionesReporteRevision, rutaSalida As String)
        File.WriteAllBytes(rutaSalida, My.Resources.PlantillaReporteRevision)

        Using wordDoc As WordprocessingDocument = WordprocessingDocument.Open(rutaSalida, True)
            Dim body As Body = wordDoc.MainDocumentPart.Document.Body
            Dim sectPr As SectionProperties = body.Elements(Of SectionProperties)().FirstOrDefault()
            If sectPr IsNot Nothing Then sectPr.Remove()

            EscribirCarta(body, proyecto, opciones)

            If opciones.IncluirCimentaciones Then
                body.Append(Heading1("CIMENTACIONES"))
                EscribirSeccionPilas(body, proyecto)
                body.Append(Heading2("VIGAS DE CIMENTACIÓN"))
                EscribirVigasCimentacion(body)
            End If

            If opciones.IncluirMuros Then
                body.Append(Heading1("MUROS"))
                EscribirSeccionMuros(body, proyecto)
            End If

            If opciones.IncluirVigas Then
                body.Append(Heading1("VIGAS"))
                EscribirSeccionVigas(body, proyecto)
            End If

            If opciones.IncluirLosas Then
                body.Append(Heading1("LOSAS"))
                EscribirSeccionLosas(body, proyecto)
            End If

            If opciones.IncluirDetallesPlanos Then
                body.Append(Heading1("DETALLES EN PLANOS"))
                EscribirDetallesPlanos(body, opciones.DetallesPlanos)
            End If

            EscribirCierre(body)

            If sectPr IsNot Nothing Then body.Append(sectPr)

            ActualizarCodigoEnEncabezado(wordDoc, opciones.CodigoProyecto)

            wordDoc.MainDocumentPart.Document.Save()
        End Using
    End Sub

    ' =========================================================================
    '  Carta / portada
    ' =========================================================================
    Private Sub EscribirCarta(body As Body, proyecto As Proyecto, opciones As OpcionesReporteRevision)
        Dim info = proyecto.Info

        If Not String.IsNullOrWhiteSpace(opciones.DestinatarioNombre) Then
            body.Append(ParrafoNormal(opciones.DestinatarioNombre & ","))
        End If
        If Not String.IsNullOrWhiteSpace(opciones.DestinatarioCargo) Then
            body.Append(ParrafoNormal(opciones.DestinatarioCargo))
        End If
        If Not String.IsNullOrWhiteSpace(opciones.Cliente) Then
            body.Append(NuevoParrafoNegrita(opciones.Cliente))
        End If
        body.Append(ParrafoNormal(""))

        Dim asunto As String = If(String.IsNullOrWhiteSpace(opciones.AsuntoProyecto), info.Nombre, opciones.AsuntoProyecto)
        body.Append(ParrafoNormal("Asunto: ", "Reporte de la revisión del diseño estructural del proyecto " & asunto & "."))
        body.Append(ParrafoNormal(""))

        body.Append(ParrafoNormal(TextoIntroProyecto(info)))

        body.Append(ParrafoNormal(
            "Se realizó la revisión del diseño estructural de acuerdo con la información recibida. En esta se evalúan los " &
            "elementos estructurales y se presenta la relación más desfavorable entre la capacidad y la demanda; si esta " &
            "relación es mayor a la unidad indica que la capacidad del elemento es mayor que la demanda; sin embargo, el " &
            "grupo técnico de EstrucMed considera que los elementos que presentan una relación cercana a 0,90 se encuentran " &
            "dentro del rango admisible asociado probablemente a diferencias menores de modelación y distribución de cargas; " &
            "mientras que los elementos que presentan una relación inferior podrían requerir una mayor capacidad para cumplir " &
            "con el requisito evaluado."))

        body.Append(ParrafoNormal(
            "En este documento se presentan los resultados de los elementos en los cuales la relación capacidad/demanda " &
            "obtenida es inferior a 0,90 y por lo tanto deben ser revisados por el diseñador estructural. Los elementos que " &
            "no se presentan en este documento, cumplen con la capacidad requerida."))
        body.Append(ParrafoNormal(""))
    End Sub

    Private Shared Function TextoIntroProyecto(info As cInfoProyecto) As String
        Dim nombre As String = If(String.IsNullOrWhiteSpace(info.Nombre), "xx", info.Nombre)
        Dim ubicacion As String = String.Join(", ", {info.Direccion, info.Ciudad, info.Departamento}.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
        If String.IsNullOrWhiteSpace(ubicacion) Then ubicacion = "xx"
        Dim disenador As String = If(String.IsNullOrWhiteSpace(info.Designer), "xx", info.Designer)
        Dim sistema As String = TextoSistemaEstructural(info.SistemaEstructural)
        Dim pisos As String = If(info.NPisos > 0, info.NPisos.ToString(), "xx")

        Dim comillaI As String = ChrW(8220)
        Dim comillaD As String = ChrW(8221)
        Return "El presente reporte incluye la revisión del sistema estructural, superestructura y subestructura del " &
               "proyecto " & comillaI & nombre & comillaD & ", el cual se encuentra localizado en " & ubicacion & ". El diseño estructural " &
               "fue desarrollado por " & disenador & ". El sistema estructural principal consiste en " & sistema &
               ". Este proyecto cuenta con " & pisos & " piso(s)."
    End Function

    Private Shared Function TextoSistemaEstructural(sistema As eSistemaEstructural) As String
        Select Case sistema
            Case eSistemaEstructural.Porticos : Return "pórticos de concreto reforzado"
            Case eSistemaEstructural.Combinado : Return "un sistema combinado de pórticos y muros de concreto reforzado"
            Case eSistemaEstructural.MCR : Return "muros de concreto reforzado"
            Case Else : Return "xx"
        End Select
    End Function

    Private Sub EscribirCierre(body As Body)
        body.Append(ParrafoNormal("Quedamos atentos a cualquier aclaración o información adicional que usted requiera."))
        body.Append(ParrafoNormal(""))
        body.Append(ParrafoNormal("Atentamente,"))
        body.Append(ParrafoNormal(""))
        body.Append(ParrafoNormal(""))
        body.Append(ParrafoNormal("Ricardo León Bonett Díaz"))
        body.Append(ParrafoNormal("Gerente Técnico"))
        body.Append(NuevoParrafoNegrita("EstrucMed Ingeniería Especializada"))
    End Sub

    Private Shared Function NuevoParrafoNegrita(texto As String) As Paragraph
        Dim p As New Paragraph()
        p.Append(CrearRun(texto, negrita:=True))
        Return p
    End Function

    ' =========================================================================
    '  PILAS
    ' =========================================================================
    Private Sub EscribirSeccionPilas(body As Body, proyecto As Proyecto)
        body.Append(Heading2("PILAS"))

        Dim pilas As List(Of Elemento_Pila) = proyecto.Elementos.Pilas.ListaElementos
        If pilas Is Nothing Then pilas = New List(Of Elemento_Pila)

        ' ── Flexo compresión ────────────────────────────────────────────────
        body.Append(Heading2("Flexo compresión"))
        Dim noFlexo = pilas.Where(Function(p) p.Factor_Diagonal > 0 AndAlso Not EsConforme(p.Factor_Diagonal)).ToList()
        If noFlexo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de flexo compresión es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexo-compresión de las pilas."))
            Dim filas As New List(Of String())
            For Each p In noFlexo
                filas.Add({p.Name_Elemento, FNum(p.Df), FNum(p.Dc), FormatoRefuerzo(p), FCD(p.Factor_Diagonal)})
            Next
            body.Append(TablaDatos({"PILA", "Df (m)", "Dc (m)", "REFUERZO", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("Cortante"))
        Dim noCortante = pilas.Where(Function(p) p.FactorShear > 0 AndAlso Not EsConforme(p.FactorShear)).ToList()
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de cortante de las pilas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de las pilas."))
            Dim filas As New List(Of String())
            For Each p In noCortante
                filas.Add({p.Name_Elemento, FCD(p.FactorShear)})
            Next
            body.Append(TablaDatos({"PILA", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Esfuerzos admisibles del concreto ───────────────────────────────
        body.Append(Heading2("Esfuerzos admisibles del concreto"))
        Dim conEsfConcreto = pilas.Select(Function(p) (Pila:=p, CD:=MinEsfConcreto(p))).Where(Function(x) x.CD > 0 AndAlso Not EsConforme(x.CD)).ToList()
        If conEsfConcreto.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad admisible del concreto es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de los esfuerzos del concreto."))
            Dim filas As New List(Of String())
            For Each x In conEsfConcreto
                filas.Add({x.Pila.Name_Elemento, FCD(x.CD)})
            Next
            body.Append(TablaDatos({"PILA", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Esfuerzos transmitidos al suelo ─────────────────────────────────
        body.Append(Heading2("Esfuerzos transmitidos al suelo"))
        Dim conEsfSuelo = pilas.Select(Function(p) (Pila:=p, CD:=MinEsfSuelo(p))).Where(Function(x) x.CD > 0 AndAlso Not EsConforme(x.CD)).ToList()
        If conEsfSuelo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de esfuerzos transmitidos al terreno es adecuada para las cargas del proyecto."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Capacidad de esfuerzos sobre el terreno."))
            Dim filas As New List(Of String())
            For Each x In conEsfSuelo
                filas.Add({x.Pila.Name_Elemento, FNum(x.Pila.Dc), FCD(x.CD)})
            Next
            body.Append(TablaDatos({"PILA", "CAMPANA COLOCADA (m)", "CAPACIDAD/DEMANDA"}, filas))
        End If
    End Sub

    Private Shared Function FormatoRefuerzo(p As Elemento_Pila) As String
        Return p.Cant_Barras_Long.ToString() & " x " & p.N_Barra_Long
    End Function

    Private Shared Function MinEsfConcreto(p As Elemento_Pila) As Double
        Dim valores = {p.Check1_PsE, p.Check2_PsD, p.Check3_PuE, p.Check4_PuD, p.Check5_PuT}.Where(Function(v) v > 0).ToList()
        If valores.Count = 0 Then Return 0
        Return valores.Min()
    End Function

    Private Shared Function MinEsfSuelo(p As Elemento_Pila) As Double
        Dim valores = {p.Relacion_EsfE, p.Relacion_EsfD}.Where(Function(v) v > 0).ToList()
        If valores.Count = 0 Then Return 0
        Return valores.Min()
    End Function

    Private Sub EscribirVigasCimentacion(body As Body)
        body.Append(PendienteImplementar(
            "el módulo de vigas de cimentación (Form_08_VigasFundacion) calcula Pmáx a compresión/tracción y φPn, " &
            "pero el resultado no se persiste por elemento dentro del proyecto — corre manualmente, uno a la vez — " &
            "por lo que hoy no es posible listarlos automáticamente en este reporte."))
    End Sub

    ' =========================================================================
    '  MUROS
    ' =========================================================================
    Private Sub EscribirSeccionMuros(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de los muros a partir de la información recibida."))

        Dim muros As List(Of Muro) = proyecto.Elementos.Muros.Lista_Muros
        If muros Is Nothing Then muros = New List(Of Muro)
        Dim murosCalculados = muros.Where(Function(m) m.Ref_Modificado_Muros).ToList()

        ' ── Flexo-compresión ─────────────────────────────────────────────────
        body.Append(Heading2("FLEXO-COMPRESIÓN"))
        Dim noFlexo As New List(Of (Muro As Muro, Sec As SeccionMuro, CD As Double))
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                Dim valores = {s.F_Flexo_Top, s.F_Flexo_Bot}.Where(Function(v) v > 0).ToList()
                If valores.Count = 0 Then Continue For
                Dim cd = valores.Min()
                If Not EsConforme(cd) Then noFlexo.Add((m, s, cd))
            Next
        Next
        If noFlexo.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de flexo compresión es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a flexo-compresión de los muros."))
            Dim filas As New List(Of String())
            For Each x In noFlexo
                Dim asReq = Math.Max(x.Sec.As_Top_Req, x.Sec.As_Bot_Req)
                Dim asCol = Math.Max(x.Sec.AsT_Top_Col, x.Sec.AsT_Bot_Col)
                filas.Add({x.Muro.Name, x.Sec.Piso, FNum(asReq), FNum(asCol), FCD(x.CD)})
            Next
            body.Append(TablaDatos({"MURO", "PISO", "As Requerido (cm2)", "As Suministrado (cm2)", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of (Muro As Muro, Sec As SeccionMuro))
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                If s.F_Cortante > 0 AndAlso Not EsConforme(s.F_Cortante) Then noCortante.Add((m, s))
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad de cortante de los muros es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la capacidad a cortante de los muros."))
            Dim filas As New List(Of String())
            For Each x In noCortante
                Dim asReq = Math.Max(x.Sec.AsH_Req_Top, x.Sec.AsH_Req_Bot)
                filas.Add({x.Muro.Name, x.Sec.Piso, FNum(asReq), FNum(x.Sec.AsH_Col), FCD(x.Sec.F_Cortante)})
            Next
            body.Append(TablaDatos({"MURO", "PISO", "As Requerido (cm2)", "As Suministrado (cm2)", "CAPACIDAD/DEMANDA"}, filas))
        End If

        ' ── Longitud vertical elemento de borde ─────────────────────────────
        body.Append(Heading2("LONGITUD VERTICAL ELEMENTO DE BORDE"))
        Dim noLV As New List(Of String())
        For Each m In murosCalculados
            Dim reqI = Math.Max(m.LV_EB_I_Req_Def, m.LV_EB_I_Req_Esf)
            If reqI > 0 AndAlso m.LV_EB_I_Col_Esp < reqI Then
                noLV.Add({m.Name, "Izquierdo", FNum(reqI), FNum(m.LV_EB_I_Col_Esp), "No Cumple"})
            End If
            Dim reqD = Math.Max(m.LV_EB_D_Req_Def, m.LV_EB_D_Req_Esf)
            If reqD > 0 AndAlso m.LV_EB_D_Col_Esp < reqD Then
                noLV.Add({m.Name, "Derecho", FNum(reqD), FNum(m.LV_EB_D_Col_Esp), "No Cumple"})
            End If
        Next
        If noLV.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la longitud vertical del elemento de borde es acorde a la solicitación en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la longitud vertical de los elementos de borde."))
            body.Append(TablaDatos({"MURO", "LADO", "LV Requerida (m)", "LV Colocada (m)", "VERIFICACIÓN"}, noLV))
        End If

        ' ── Longitud horizontal elemento de borde ───────────────────────────
        body.Append(Heading2("LONGITUD HORIZONTAL ELEMENTO DE BORDE"))
        Dim noLH As New List(Of String())
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                For Each par In New List(Of (Lado As String, EB As SeccionMuro.ElementoBorde)) From {
                    ("Izquierdo - Top", s.EB_I_Top), ("Izquierdo - Bot", s.EB_I_Bot),
                    ("Derecho - Top", s.EB_D_Top), ("Derecho - Bot", s.EB_D_Bot)}
                    If par.EB Is Nothing OrElse par.EB.L_EB_Req <= 0 Then Continue For
                    Dim cumple = par.EB.L_EB_Req <= par.EB.L_EB / 0.9
                    If Not cumple Then
                        noLH.Add({m.Name, s.Piso, par.Lado, FNum(par.EB.L_EB_Req), FNum(par.EB.L_EB), "No Cumple"})
                    End If
                Next
            Next
        Next
        If noLH.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la longitud horizontal del elemento de borde cumple en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la longitud horizontal de los elementos de borde."))
            body.Append(TablaDatos({"MURO", "PISO", "LADO", "LH Requerida (m)", "LH Colocada (m)", "VERIFICACIÓN"}, noLH))
        End If

        ' ── Confinamiento ────────────────────────────────────────────────────
        body.Append(Heading2("CONFINAMIENTO"))
        Dim noConf As New List(Of String())
        Dim nde = proyecto.ParametrosSismicos.NDE
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                For Each par In New List(Of (Lado As String, EB As SeccionMuro.ElementoBorde)) From {
                    ("Izquierdo - Top", s.EB_I_Top), ("Izquierdo - Bot", s.EB_I_Bot),
                    ("Derecho - Top", s.EB_D_Top), ("Derecho - Bot", s.EB_D_Bot)}
                    If par.EB Is Nothing OrElse par.EB.RefH Is Nothing OrElse par.EB.RefH.Separacion <= 0 Then Continue For
                    Try
                        Dim resultado = Funciones_Muros.AceroH_EB(nde, par.EB, s.tw_Planos, s.fc, s.fy)
                        Dim sMaxReq As Double = CDbl(resultado(0)) / 1000.0 ' mm -> m
                        Dim sCol As Double = par.EB.RefH.Separacion
                        If Math.Round(sCol, 3) > Math.Round(sMaxReq, 3) Then
                            noConf.Add({m.Name, s.Piso, par.Lado, FNum(sMaxReq, 3), FNum(sCol, 3), "No Cumple"})
                        End If
                    Catch
                        ' Datos insuficientes para calcular la separación requerida en este elemento de borde — se omite.
                    End Try
                Next
            Next
        Next
        If noConf.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la separación del refuerzo transversal de los elementos de borde cumple en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de la separación máxima del refuerzo transversal de los elementos de borde."))
            body.Append(TablaDatos({"MURO", "PISO", "LADO", "S Requerida (m)", "S Colocada (m)", "VERIFICACIÓN"}, noConf))
            body.Append(ParrafoNormal(
                "Nota: esta verificación evalúa únicamente la separación del refuerzo transversal; la distribución de " &
                "ganchos suplementarios en el elemento de borde debe revisarse manualmente en los planos."))
        End If

        ' ── Requisitos normativos ────────────────────────────────────────────
        body.Append(Heading2("REQUISITOS NORMATIVOS"))
        body.Append(Heading2("Cuantías máximas"))
        body.Append(PendienteImplementar("no existe cálculo de cuantía máxima (Rho_Max) en el módulo de Muros."))

        body.Append(Heading2("Cuantías mínimas"))
        Dim noCuantiaMin As New List(Of String())
        For Each m In murosCalculados
            For Each s In m.Lista_Secciones
                If s.Rho_Min_L <= 0 Then Continue For
                If s.Cuantia_Top_Col > 0 AndAlso s.Cuantia_Top_Col < s.Rho_Min_L Then
                    noCuantiaMin.Add({s.Piso, m.Name, "Top", FNum(s.Cuantia_Top_Col * 100, 2) & "%", "El refuerzo vertical (Top) no alcanza la cuantía mínima requerida."})
                End If
                If s.Cuantia_Bot_Col > 0 AndAlso s.Cuantia_Bot_Col < s.Rho_Min_L Then
                    noCuantiaMin.Add({s.Piso, m.Name, "Bot", FNum(s.Cuantia_Bot_Col * 100, 2) & "%", "El refuerzo vertical (Bot) no alcanza la cuantía mínima requerida."})
                End If
            Next
        Next
        If noCuantiaMin.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones; la cuantía vertical colocada cumple la cuantía mínima en todos los muros revisados."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Muros donde la cuantía colocada es menor a la mínima requerida."))
            body.Append(TablaDatos({"PISO", "MURO", "TRAMO", "ρCol", "OBSERVACIÓN"}, noCuantiaMin, {}))
        End If
    End Sub

    ' =========================================================================
    '  VIGAS
    ' =========================================================================
    Private Sub EscribirSeccionVigas(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de las vigas a partir de la información recibida."))

        Dim vigas As List(Of cViga) = proyecto.Elementos.Vigas.Vigas
        If vigas Is Nothing Then vigas = New List(Of cViga)

        ' ── Flexión ──────────────────────────────────────────────────────────
        body.Append(Heading2("FLEXIÓN"))
        Dim noFlexion As New List(Of String())
        For Each v In vigas
            Dim nombreViga = ObtenerNombreViga(v)
            For Each f In v.Frames
                If f.RevisionFlexion Is Nothing Then Continue For
                For Each zona In f.RevisionFlexion
                    Dim r = zona.ResultadoActual
                    If r Is Nothing Then Continue For
                    If r.AsReqSup > 0 AndAlso Not EsConforme(r.RatioSup) Then
                        noFlexion.Add({f.Story, nombreViga, zona.Posicion.ToString(), FNum(r.AsProvSup), FNum(r.AsReqSup), FCD(r.RatioSup), "Momento negativo (refuerzo superior)"})
                    End If
                    If r.AsReqInf > 0 AndAlso Not EsConforme(r.RatioInf) Then
                        noFlexion.Add({f.Story, nombreViga, zona.Posicion.ToString(), FNum(r.AsProvInf), FNum(r.AsReqInf), FCD(r.RatioInf), "Momento positivo (refuerzo inferior)"})
                    End If
                Next
            Next
        Next
        If noFlexion.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a flexión de las vigas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de flexión en vigas."))
            body.Append(TablaDatos({"PISO", "VIGA", "TRAMO", "Ash colocada (cm2)", "Ash requerida (cm2)", "CAPACIDAD/DEMANDA", "OBSERVACIÓN"}, noFlexion, {5}))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of String())
        For Each v In vigas
            Dim nombreViga = ObtenerNombreViga(v)
            For Each f In v.Frames
                If f.RevisionCortante Is Nothing Then Continue For
                For Each zona In f.RevisionCortante
                    If zona.Vu > 0 AndAlso zona.Factor > 0 AndAlso Not EsConforme(zona.Factor) Then
                        noCortante.Add({f.Story, nombreViga, zona.Posicion.ToString(), FNum(zona.Vu), FNum(zona.phiVn), FCD(zona.Factor)})
                    End If
                Next
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a cortante de las vigas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de cortante en vigas."))
            body.Append(TablaDatos({"PISO", "VIGA", "TRAMO", "Vu (kN)", "φVn (kN)", "CAPACIDAD/DEMANDA"}, noCortante))
        End If

        body.Append(Heading2("DEFLEXIONES"))
        body.Append(PendienteImplementar("el módulo de Vigas no calcula deflexiones inmediatas/diferidas ni el chequeo de elementos susceptibles de dañarse."))

        body.Append(Heading2("REQUISITOS NORMATIVOS"))
        body.Append(Heading2("Cuantías máximas"))
        body.Append(PendienteImplementar("no existe cálculo de cuantía máxima en el módulo de Vigas."))

        body.Append(Heading2("Separación horizontal entre barras longitudinales"))
        body.Append(PendienteImplementar("no existe verificación de separación libre entre barras longitudinales (NSR-10 C.7.6.1) en el módulo de Vigas."))
    End Sub

    Private Shared Function ObtenerNombreViga(v As cViga) As String
        Return If(String.IsNullOrWhiteSpace(v.NombrePlano), v.Nombre, v.NombrePlano)
    End Function

    ' =========================================================================
    '  LOSAS (viguetas de losa nervada — paneles macizos: pendiente)
    ' =========================================================================
    Private Sub EscribirSeccionLosas(body As Body, proyecto As Proyecto)
        body.Append(ParrafoNormal("Se realiza la revisión de las viguetas de losas nervadas a partir de la información recibida."))

        Dim nervios As List(Of cNervio) = proyecto.Elementos.Nervios.Elementos
        If nervios Is Nothing Then nervios = New List(Of cNervio)

        ' ── Flexión ──────────────────────────────────────────────────────────
        body.Append(Heading2("FLEXIÓN"))
        Dim noFlexion As New List(Of String())
        For Each n In nervios
            Dim nombre = If(String.IsNullOrWhiteSpace(n.NombrePlano), n.Nombre, n.NombrePlano)
            For Each f In n.Frames
                For Each z In New List(Of (Zona As String, Mu As Double, PhiMn As Double, CD As Double)) From {
                    ("Apoyo izquierdo", f.Mu_Neg_I, f.PhiMn_Sup_I, f.CD_Flex_Sup_I),
                    ("Centro (vano)", f.Mu_Pos_C, f.PhiMn_Inf_C, f.CD_Flex_Inf_C),
                    ("Apoyo derecho", f.Mu_Neg_D, f.PhiMn_Sup_D, f.CD_Flex_Sup_D)}
                    If z.CD > 0 AndAlso Not EsConforme(z.CD) Then
                        noFlexion.Add({f.Story, nombre, z.Zona, FNum(z.Mu), FNum(z.PhiMn), FCD(z.CD)})
                    End If
                Next
            Next
        Next
        If noFlexion.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a flexión de las viguetas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de flexión en viguetas."))
            body.Append(TablaDatos({"PISO", "VIGUETA", "ZONA", "Mu (kN·m)", "φMn (kN·m)", "CAPACIDAD/DEMANDA"}, noFlexion))
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        body.Append(Heading2("CORTANTE"))
        Dim noCortante As New List(Of String())
        For Each n In nervios
            Dim nombre = If(String.IsNullOrWhiteSpace(n.NombrePlano), n.Nombre, n.NombrePlano)
            For Each f In n.Frames
                For Each z In New List(Of (Zona As String, Vu As Double, PhiVn As Double, CD As Double)) From {
                    ("Izquierdo", f.Vu_I, f.PhiVn_I, f.CD_Cortante_I),
                    ("Derecho", f.Vu_D, f.PhiVn_D, f.CD_Cortante_D)}
                    If z.CD > 0 AndAlso Not EsConforme(z.CD) Then
                        noCortante.Add({f.Story, nombre, z.Zona, FNum(z.Vu), FNum(z.PhiVn), FCD(z.CD)})
                    End If
                Next
            Next
        Next
        If noCortante.Count = 0 Then
            body.Append(ParrafoSinAnotaciones("No se tienen anotaciones debido a que la capacidad a cortante de las viguetas es mayor a la demanda."))
        Else
            _numTabla += 1
            body.Append(TituloTabla(_numTabla, "Verificación de cortante en viguetas."))
            body.Append(TablaDatos({"PISO", "VIGUETA", "ZONA", "Vu (kN)", "φVn (kN)", "CAPACIDAD/DEMANDA"}, noCortante))
        End If

        body.Append(Heading2("DEFLEXIONES"))
        body.Append(PendienteImplementar("el módulo de Nervios no calcula deflexiones inmediatas/diferidas."))

        body.Append(Heading2("PANELES DE LOSA MACIZA"))
        body.Append(PendienteImplementar(
            "el módulo de Losas macizas (Form_03_Losas / Proyecto_Losas) es hoy una calculadora aislada de un solo " &
            "panel, sin lista persistida dentro del proyecto ni cálculo de As requerido/colocado o C/D por panel — " &
            "no es posible listar paneles automáticamente en este reporte."))
    End Sub

    ' =========================================================================
    '  DETALLES EN PLANOS
    ' =========================================================================
    Private Sub EscribirDetallesPlanos(body As Body, detalles As List(Of String))
        If detalles Is Nothing OrElse detalles.Count = 0 Then
            body.Append(ParrafoNormal("No se registraron detalles pendientes en planos para este proyecto."))
            Return
        End If
        For Each linea In detalles
            If String.IsNullOrWhiteSpace(linea) Then Continue For
            body.Append(NuevoParrafoViñeta(linea.Trim()))
        Next
    End Sub

    Private Shared Function NuevoParrafoViñeta(texto As String) As Paragraph
        Return ParrafoNormal("•  " & texto)
    End Function

End Class
