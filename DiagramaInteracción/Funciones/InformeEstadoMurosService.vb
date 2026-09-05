Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports DocumentFormat.OpenXml.Packaging
Imports DocumentFormat.OpenXml.Wordprocessing
Imports ARCO.eNumeradores

''' <summary>Una fila editable de las tablas "Información recibida" / "Detalles de avance".</summary>
Public Class ItemChecklistInforme
    Public Property Detalle As String = ""
    Public Property Estado As String = ""
    Public Property Observaciones As String = ""

    Public Sub New()
    End Sub

    Public Sub New(detalle As String, estadoPorDefecto As String)
        Me.Detalle = detalle
        Me.Estado = estadoPorDefecto
    End Sub
End Class

''' <summary>Opciones capturadas en <see cref="Form_InformeEstadoMuros"/> para generar el informe.</summary>
Public Class OpcionesInformeEstadoMuros
    Public Property Fecha As Date = Date.Today
    Public Property CodigoProyecto As String = ""
    Public Property NombreProyecto As String = ""
    Public Property Direccion As String = ""
    Public Property Propietario As String = ""
    Public Property SistemaEstructural As String = ""
    Public Property NPisos As String = ""
    Public Property AreaPlanta As String = ""
    Public Property ElaboradoPor As String = ""
    Public Property RevisadoPor As String = ""

    Public Property InfoRecibida As New List(Of ItemChecklistInforme) From {
        New ItemChecklistInforme("Planos Arquitectónicos", "Pendiente"),
        New ItemChecklistInforme("Planos Estructurales", "Pendiente"),
        New ItemChecklistInforme("Memorias de cálculo", "Pendiente"),
        New ItemChecklistInforme("Estudio de suelos", "Pendiente")
    }

    Public Property DetallesAvance As New List(Of ItemChecklistInforme) From {
        New ItemChecklistInforme("Modelo estructural", "Pendiente"),
        New ItemChecklistInforme("Análisis dinámico", "Pendiente"),
        New ItemChecklistInforme("Verificación de derivas", "Pendiente"),
        New ItemChecklistInforme("Verificación de diseño de elementos estructurales", "Pendiente")
    }

    Public Property ObservacionDensidad As String = ""
    Public Property ObservacionDerivas As String = ""
    Public Property ObservacionALR As String = ""

    Public Property AnalisisPreliminarRecomendaciones As String = ""
End Class

''' <summary>
''' Genera el "Informe de Estado del Proyecto" (.docx) — revisión conceptual/preliminar del módulo de
''' Muros (densidad, derivas, ALR, muros protagónicos, espesores, factor de forma). Es un reporte
''' independiente y anterior en el tiempo al "Reporte de Revisión" (<see cref="ReporteRevisionService"/>):
''' se usa en etapas tempranas del proyecto, antes de tener el refuerzo diseñado.
''' Reconstruye, con DocumentFormat.OpenXml (sin dependencia de Word instalado), la misma plantilla
''' conceptual de FormatosRevisión\Prueba_Reporte.docx sobre el membrete corporativo ya usado por
''' <see cref="ReporteRevisionService"/> (My.Resources.PlantillaReporteRevision).
''' </summary>
Public Class InformeEstadoMurosService

    Private _numFigura As Integer = 0
    Private _numTabla As Integer = 0

    Private Shared Function FNum(valor As Double, Optional decimales As Integer = 2) As String
        Return valor.ToString("0." & New String("0"c, decimales))
    End Function

    Private Shared Function FechaLarga(fecha As Date) As String
        Dim meses() As String = {"enero", "febrero", "marzo", "abril", "mayo", "junio",
                                  "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"}
        Return fecha.Day & " de " & meses(fecha.Month - 1) & " de " & fecha.Year
    End Function

    ' =========================================================================
    '  Punto de entrada
    ' =========================================================================
    Public Shared Sub GenerarInforme(proyecto As Proyecto, opciones As OpcionesInformeEstadoMuros, rutaSalida As String)
        Dim servicio As New InformeEstadoMurosService()
        servicio.Generar(proyecto, opciones, rutaSalida)
    End Sub

    Private Sub Generar(proyecto As Proyecto, opciones As OpcionesInformeEstadoMuros, rutaSalida As String)
        File.WriteAllBytes(rutaSalida, My.Resources.PlantillaReporteRevision)

        Using wordDoc As WordprocessingDocument = WordprocessingDocument.Open(rutaSalida, True)
            Dim mainPart As MainDocumentPart = wordDoc.MainDocumentPart
            Dim body As Body = mainPart.Document.Body
            Dim sectPr As SectionProperties = body.Elements(Of SectionProperties)().FirstOrDefault()
            If sectPr IsNot Nothing Then sectPr.Remove()

            body.Append(Heading1("INFORME DE ESTADO DEL PROYECTO"))
            body.Append(Heading2("INFORMACIÓN GENERAL DEL PROYECTO"))
            EscribirInformacionGeneral(body, opciones)

            body.Append(Heading1("INFORMACIÓN RECIBIDA"))
            EscribirChecklist(body, "Se relaciona a continuación la información recibida para el desarrollo de la revisión.",
                               {"DETALLE", "ESTADO", "OBSERVACIONES"}, opciones.InfoRecibida)

            body.Append(Heading1("DETALLES DE AVANCE"))
            EscribirChecklist(body, "Se relaciona a continuación el grado de avance del proyecto al momento de este informe.",
                               {"ÍTEM", "GRADO DE AVANCE", "OBSERVACIONES"}, opciones.DetallesAvance)

            body.Append(Heading1("ANÁLISIS CONCEPTUAL"))
            EscribirDensidadMuros(body, proyecto, opciones)
            EscribirDerivas(body, mainPart, proyecto, opciones)
            EscribirALR(body, mainPart, proyecto, opciones)

            body.Append(Heading1("ANEXOS"))
            EscribirAnexos(body, mainPart, proyecto, opciones)

            body.Append(Heading1("ANÁLISIS PRELIMINAR Y RECOMENDACIONES"))
            EscribirRecomendaciones(body, opciones)

            If sectPr IsNot Nothing Then body.Append(sectPr)

            ActualizarCodigoEnEncabezado(wordDoc, opciones.CodigoProyecto)

            mainPart.Document.Save()
        End Using
    End Sub

    ' =========================================================================
    '  Información general
    ' =========================================================================
    Private Sub EscribirInformacionGeneral(body As Body, opciones As OpcionesInformeEstadoMuros)
        body.Append(ParrafoNormal("Fecha del informe: ", FechaLarga(opciones.Fecha)))
        body.Append(ParrafoNormal("Nombre del proyecto: ", opciones.NombreProyecto))
        body.Append(ParrafoNormal("Dirección: ", opciones.Direccion))
        body.Append(ParrafoNormal("Propietario: ", opciones.Propietario))
        body.Append(ParrafoNormal("Sistema estructural: ", opciones.SistemaEstructural))
        body.Append(ParrafoNormal("Número de pisos: ", opciones.NPisos))
        body.Append(ParrafoNormal("Área en planta típica: ", If(String.IsNullOrWhiteSpace(opciones.AreaPlanta), "", opciones.AreaPlanta & " m²")))
        body.Append(ParrafoNormal("Elaborado por: ", opciones.ElaboradoPor))
        body.Append(ParrafoNormal("Revisado por: ", opciones.RevisadoPor))
        body.Append(ParrafoNormal(""))
    End Sub

    ' =========================================================================
    '  Información recibida / Detalles de avance (misma estructura de tabla)
    ' =========================================================================
    Private Sub EscribirChecklist(body As Body, introduccion As String, headers() As String, items As List(Of ItemChecklistInforme))
        body.Append(ParrafoNormal(introduccion))
        If items Is Nothing OrElse items.Count = 0 Then
            body.Append(ParrafoNormal("No se registraron elementos."))
            Return
        End If
        Dim filas As New List(Of String())
        For Each item In items
            filas.Add({item.Detalle, item.Estado, item.Observaciones})
        Next
        body.Append(TablaDatos(headers, filas, {}))
    End Sub

    ' =========================================================================
    '  Análisis conceptual
    ' =========================================================================
    Private Sub EscribirDensidadMuros(body As Body, proyecto As Proyecto, opciones As OpcionesInformeEstadoMuros)
        body.Append(Heading2("DENSIDAD DE MUROS"))
        Dim densX As Double = proyecto.Elementos.Muros.Densidad_X * 100
        Dim densY As Double = proyecto.Elementos.Muros.Densidad_Y * 100
        body.Append(ParrafoNormal("Densidad de muros: ", "X = " & FNum(densX) & "%  —  Y = " & FNum(densY) & "%"))
        If Not String.IsNullOrWhiteSpace(opciones.ObservacionDensidad) Then
            body.Append(ParrafoNormal(opciones.ObservacionDensidad))
        End If
        body.Append(ParrafoNormal(""))
    End Sub

    Private Sub EscribirDerivas(body As Body, mainPart As MainDocumentPart, proyecto As Proyecto, opciones As OpcionesInformeEstadoMuros)
        body.Append(Heading2("VARIACIÓN DE LAS DERIVAS EN ALTURA"))

        Dim hayDerivas As Boolean = proyecto.ResultadosGlobales IsNot Nothing AndAlso
                                     proyecto.ResultadosGlobales.DerivasX IsNot Nothing AndAlso
                                     proyecto.ResultadosGlobales.DerivasX.Count > 0
        If Not hayDerivas Then
            body.Append(PendienteImplementar("no hay resultados de derivas cargados en el proyecto (menú Derivas)."))
        Else
            Dim imagen As Byte() = Nothing
            Try
                Using chart As New Chart()
                    chart.Width = 900 : chart.Height = 650
                    Funciones_Muros.GraficarDeriva(chart)
                    imagen = BytesDeChart(chart)
                End Using
            Catch ex As Exception
                Logger.Error(ex, "InformeEstadoMurosService.EscribirDerivas")
            End Try
            AgregarFiguraOPendiente(body, mainPart, imagen, 14, 10,
                                     "Variación de la deriva en altura.",
                                     "no fue posible generar el gráfico de derivas.")
        End If

        If Not String.IsNullOrWhiteSpace(opciones.ObservacionDerivas) Then
            body.Append(ParrafoNormal(opciones.ObservacionDerivas))
        End If
        body.Append(ParrafoNormal(""))
    End Sub

    Private Sub EscribirALR(body As Body, mainPart As MainDocumentPart, proyecto As Proyecto, opciones As OpcionesInformeEstadoMuros)
        body.Append(Heading2("RELACIÓN DE CARGA AXIAL DE LOS ELEMENTOS VERTICALES (ALR = N / (f'c·Ag))"))

        Dim muros = proyecto.Elementos.Muros.Lista_Muros
        If muros Is Nothing OrElse muros.Count = 0 Then
            body.Append(PendienteImplementar("no hay muros calculados en el proyecto."))
        Else
            Dim imagen As Byte() = Nothing
            Try
                Using chart As New Chart()
                    chart.Width = 1100 : chart.Height = 550
                    Funciones_Muros.GraficarALRMuros(chart)
                    imagen = BytesDeChart(chart)
                End Using
            Catch ex As Exception
                Logger.Error(ex, "InformeEstadoMurosService.EscribirALR")
            End Try
            AgregarFiguraOPendiente(body, mainPart, imagen, 16, 8,
                                     "Relación de carga axial (ALR) de los muros del proyecto.",
                                     "no fue posible generar el gráfico de ALR.")
        End If

        If Not String.IsNullOrWhiteSpace(opciones.ObservacionALR) Then
            body.Append(ParrafoNormal(opciones.ObservacionALR))
        End If
        body.Append(ParrafoNormal(""))
    End Sub

    ' =========================================================================
    '  Anexos
    ' =========================================================================
    Private Sub EscribirAnexos(body As Body, mainPart As MainDocumentPart, proyecto As Proyecto, opciones As OpcionesInformeEstadoMuros)
        Dim muros = proyecto.Elementos.Muros.Lista_Muros
        If muros Is Nothing OrElse muros.Count = 0 Then
            body.Append(PendienteImplementar("no hay muros calculados en el proyecto; no es posible generar los anexos gráficos."))
            Return
        End If

        Dim imgPlanta As Byte() = Nothing
        Dim imgSeleccion As Byte() = Nothing
        Dim imgEspesorPlanta As Byte() = Nothing
        Dim imgEspesorAltura As Byte() = Nothing
        Dim imgProtagonicos As Byte() = Nothing

        Try
            Funciones_Muros.CalcularGeometriaMuros()

            Using pb As New PictureBox()
                pb.Width = 900 : pb.Height = 650
                Funciones_Muros.FiguraMurosPlanta(pb)
                imgPlanta = BytesDeImagen(pb.Image)
            End Using

            Using pbTw As New PictureBox(), pbProt As New PictureBox()
                pbTw.Width = 900 : pbTw.Height = 650
                pbProt.Width = 900 : pbProt.Height = 650
                Funciones_Muros.GraficosMurosPlanta(pbTw, pbProt)
                imgEspesorPlanta = BytesDeImagen(pbTw.Image)
                imgProtagonicos = BytesDeImagen(pbProt.Image)
            End Using

            Using chart As New Chart()
                chart.Width = 1000 : chart.Height = 600
                Funciones_Muros.GraficarTwAltura(chart)
                imgEspesorAltura = BytesDeChart(chart)
            End Using
        Catch ex As Exception
            Logger.Error(ex, "InformeEstadoMurosService.EscribirAnexos (geometría/espesores)")
        End Try

        Dim hayProtX As Boolean = muros.Any(Function(m) m.Direccion = eDireccion.X AndAlso m.TipoMuro = eTipoMuro.Protagonico)
        Dim hayProtY As Boolean = muros.Any(Function(m) m.Direccion = eDireccion.Y AndAlso m.TipoMuro = eTipoMuro.Protagonico)
        If hayProtX AndAlso hayProtY Then
            Try
                Using chart As New Chart()
                    chart.Width = 1100 : chart.Height = 600
                    chart.ChartAreas.Add(New ChartArea("ChartArea1"))
                    chart.ChartAreas.Add(New ChartArea("ChartArea2"))
                    Funciones_Muros.GraficoPorcentajeMuros(chart, 18, 16, 14)
                    imgSeleccion = BytesDeChart(chart)
                End Using
            Catch ex As Exception
                Logger.Error(ex, "InformeEstadoMurosService.EscribirAnexos.GraficoPorcentajeMuros")
            End Try
        End If

        body.Append(Heading2("UBICACIÓN Y NOMENCLATURA EN PLANTA"))
        body.Append(ParrafoNormal("Ubicación y nomenclatura en planta de los muros del proyecto " & opciones.NombreProyecto & "."))
        AgregarFiguraOPendiente(body, mainPart, imgPlanta, 14, 10,
                                 "Localización y nomenclatura de muros en el modelo estructural.",
                                 "no fue posible generar la figura de planta.")

        body.Append(Heading2("DETERMINACIÓN DE LOS MUROS PROTAGÓNICOS"))
        body.Append(ParrafoNormal(
            "Se definieron los muros representativos (protagónicos) en cada dirección principal a partir del " &
            "porcentaje de cortante basal (%Vb) que resiste cada muro. Se identifican como protagónicos los dos " &
            "muros de mayor %Vb en cada dirección y, a partir de ahí, los siguientes mientras el %Vb del muro no " &
            "caiga más de un 25% respecto al muro anterior en la lista ordenada de forma descendente."))
        AgregarFiguraOPendiente(body, mainPart, imgSeleccion, 16, 9,
                                 "Selección de muros protagónicos en cada dirección.",
                                 "no hay muros protagónicos clasificados en ambas direcciones; recalcule los macroparámetros de muros.")

        body.Append(Heading2("ESPESOR DE MUROS"))
        AgregarFiguraOPendiente(body, mainPart, imgEspesorPlanta, 14, 10,
                                 "Identificación de espesor de muros en planta.",
                                 "no fue posible generar la figura de espesores en planta.")
        AgregarFiguraOPendiente(body, mainPart, imgEspesorAltura, 15, 9,
                                 "Variación de espesor en altura de los muros protagónicos.",
                                 "no fue posible generar el gráfico de espesor en altura.")

        body.Append(Heading2("MUROS PROTAGÓNICOS EN PLANTA"))
        AgregarFiguraOPendiente(body, mainPart, imgProtagonicos, 14, 10,
                                 "Muros protagónicos identificados en planta.",
                                 "no fue posible generar la figura de muros protagónicos.")

        body.Append(Heading2("RESUMEN DEL ANÁLISIS DE MUROS"))
        _numTabla += 1
        body.Append(TituloTabla(_numTabla, "Resumen del análisis conceptual de los muros del edificio."))
        body.Append(TablaDatos({"PARÁMETRO", "VALOR", "OBSERVACIÓN"}, ConstruirFilasResumen(proyecto), {}))
    End Sub

    Private Sub AgregarFiguraOPendiente(body As Body, mainPart As MainDocumentPart, imagen As Byte(),
                                         anchoCm As Double, altoCm As Double, pieDeFoto As String, mensajePendiente As String)
        If imagen Is Nothing OrElse imagen.Length = 0 Then
            body.Append(PendienteImplementar(mensajePendiente))
            Return
        End If
        _numFigura += 1
        body.Append(ImagenCentrada(mainPart, imagen, anchoCm, altoCm))
        body.Append(TituloFigura(_numFigura, pieDeFoto))
    End Sub

    Private Function ConstruirFilasResumen(proyecto As Proyecto) As List(Of String())
        Dim filas As New List(Of String())
        Dim protagonicos = proyecto.Elementos.Muros.Lista_Muros.Where(Function(m) m.TipoMuro = eTipoMuro.Protagonico).ToList()

        ' Espesor de muros
        Dim espesores As List(Of Single) = protagonicos.Select(Function(m) m.tw).Distinct().OrderByDescending(Function(t) t).ToList()
        If espesores.Count > 0 Then
            Dim txtEspesores As String = String.Join(" / ", espesores.Select(Function(t) FNum(t) & " m"))
            Dim obsEspesor As String = If(espesores.Count > 1,
                "Se identificaron " & espesores.Count & " espesores distintos entre los muros protagónicos.",
                "Espesor uniforme en los muros protagónicos.")
            filas.Add({"Espesor de muros (tw)", txtEspesores, obsEspesor})
        Else
            filas.Add({"Espesor de muros (tw)", "N/D", "No hay muros protagónicos clasificados."})
        End If

        ' ALR máxima
        If protagonicos.Count > 0 Then
            Dim alrMaxPct As Double = protagonicos.Max(Function(m) m.ALR_D) * 100
            Dim obsAlr As String
            If alrMaxPct < 20 Then
                obsAlr = "Por debajo del límite moderado (20%)."
            ElseIf alrMaxPct < 35 Then
                obsAlr = "Entre el límite moderado (20%) y el límite alto (35%)."
            Else
                obsAlr = "Supera el límite alto (35%); revisar en detalle."
            End If
            filas.Add({"ALR máxima (muros protagónicos)", FNum(alrMaxPct) & "%", obsAlr})
        Else
            filas.Add({"ALR máxima (muros protagónicos)", "N/D", "No hay muros protagónicos clasificados."})
        End If

        ' Factor de forma (BL/BT)
        Dim factorForma As Single = proyecto.Elementos.Muros.Factor_Forma
        If factorForma > 0 Then
            Dim obsForma As String = If(factorForma > 2, "Planta alargada; mayor probabilidad de torsión en planta.", "Planta compacta.")
            filas.Add({"Factor de forma (BL/BT)", FNum(factorForma), obsForma})
        Else
            filas.Add({"Factor de forma (BL/BT)", "N/D", "Ejecute ""Calcular"" en el módulo de Muros para obtener la geometría en planta."})
        End If

        Return filas
    End Function

    ' =========================================================================
    '  Análisis preliminar y recomendaciones
    ' =========================================================================
    Private Sub EscribirRecomendaciones(body As Body, opciones As OpcionesInformeEstadoMuros)
        If String.IsNullOrWhiteSpace(opciones.AnalisisPreliminarRecomendaciones) Then
            body.Append(ParrafoNormal("Pendiente de definir por el ingeniero revisor."))
            Return
        End If
        For Each linea In opciones.AnalisisPreliminarRecomendaciones.Split({vbCrLf, vbLf}, StringSplitOptions.None)
            body.Append(ParrafoNormal(linea))
        Next
    End Sub

    ' =========================================================================
    '  Conversión de controles de graficación a PNG en memoria (sin archivos temporales en disco)
    ' =========================================================================
    Private Shared Function BytesDeChart(chart As Chart) As Byte()
        Using ms As New MemoryStream()
            chart.SaveImage(ms, ChartImageFormat.Png)
            Return ms.ToArray()
        End Using
    End Function

    Private Shared Function BytesDeImagen(imagen As Image) As Byte()
        If imagen Is Nothing Then Return Nothing
        Using ms As New MemoryStream()
            imagen.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
            Return ms.ToArray()
        End Using
    End Function

End Class
