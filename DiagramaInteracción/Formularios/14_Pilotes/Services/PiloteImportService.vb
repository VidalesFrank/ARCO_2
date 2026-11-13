''' <summary>
''' Importación ETABS del módulo de Pilotes. Delega la detección completa a
''' Funciones_00_Varias.DetectarCandidatosETABS (ya probada en Pilas) y
''' sólo transforma cada cCandidatoPila a un Dado del módulo de Pilotes.
'''
''' La detección existente ya resuelve las trampas conocidas:
'''   · Frame → lee Joint Reactions en el nodo de MENOR Z por ObjectLabel.
'''   · Pier  → lee Pier Forces con Location="Bottom" en la story base e
'''             invierte el signo de P (ETABS: P&lt;0 compresión → FZ&gt;0 compresión).
'''   · ObjectLabel vs ElementLabel: usa el label correcto para cada hoja.
'''
''' Resultado: cada Dado queda etiquetado con SourceType ∈ {"Frame","Pier"}
''' para trazabilidad y para que la UI pueda diferenciarlos visualmente.
''' </summary>
Public Module PiloteImportService

    ''' <summary>
    ''' Lee un Excel de ETABS, detecta apoyos tipo Frame y tipo Pier, y crea
    ''' un Dado por cada uno. No pisa los dados existentes — los nuevos se
    ''' fusionan por JointLabel/SourceName. Si ya existía un Dado con el mismo
    ''' identificador, se reemplazan sus combinaciones y coordenadas; se
    ''' preserva el NombreTipoDado asignado y la bandera AsignacionManual.
    ''' </summary>
    ''' <returns>Resumen de la importación para mostrar al usuario.</returns>
    Public Function Importar(contenedor As cPilotes,
                             rutaExcel As String,
                             Optional toleranciaXY As Double = 0.5,
                             Optional proyecto As Proyecto = Nothing) As ResultadoImportacion

        Dim r As New ResultadoImportacion()
        r.RutaOrigen = rutaExcel

        If contenedor Is Nothing Then
            r.ErrorFatal = "No hay contenedor de pilotes en el proyecto."
            Return r
        End If
        If String.IsNullOrEmpty(rutaExcel) OrElse Not IO.File.Exists(rutaExcel) Then
            r.ErrorFatal = "No se encontró el archivo: " & rutaExcel
            Return r
        End If

        ' Pre-chequeo de hojas: Joint Reactions es obligatoria. Las hojas Pier
        ' son opcionales — un modelo sin muros Pier es un caso normal, no un
        ' error. Si faltan, informamos y seguimos con solo Frame.
        Try
            Dim hojas = Funciones_00_Varias.ObtenerHojasExcel(rutaExcel)
            If hojas IsNot Nothing Then
                Dim tieneHoja = Function(nombre As String) _
                    hojas.Any(Function(h) h.Equals(nombre, StringComparison.OrdinalIgnoreCase))

                If Not tieneHoja("Joint Reactions") Then
                    r.ErrorFatal = "El archivo no tiene la hoja 'Joint Reactions'. " &
                                   "Verifica que hayas exportado reacciones desde ETABS."
                    Return r
                End If

                Dim tienePierForces = tieneHoja("Pier Forces")
                Dim tienePierProps = tieneHoja("Pier Section Properties")
                If Not tienePierForces OrElse Not tienePierProps Then
                    r.ModeloSinPier = True
                    r.NotasInformativas.Add(
                        "El modelo no tiene muros tipo Pier (faltan hojas " &
                        If(Not tienePierForces, "'Pier Forces'", "") &
                        If((Not tienePierForces) AndAlso (Not tienePierProps), " y ", "") &
                        If(Not tienePierProps, "'Pier Section Properties'", "") &
                        "). Se importarán solo apoyos tipo Frame.")
                End If
            End If
        Catch ex As Exception
            ' No es fatal: si el listador falla, dejamos que DetectarCandidatosETABS
            ' intente y los errores quedarán en Logger. Lo que no queremos es
            ' bloquear al usuario por una validación opcional.
            Logger.Warning("PiloteImportService.Importar",
                           "No se pudo listar las hojas del Excel: " & ex.Message)
        End Try

        ' ── Grids de ETABS ──────────────────────────────────────────
        ' Importamos los ejes estructurales si vienen en el mismo Excel para
        ' que el usuario los vea en la planta sin tener que pasar por el
        ' módulo de Zapatas o Vigas. Mismo patrón que Form_07_Pag_Zapatas.
        ' Solo escribimos si el proyecto todavía no tiene grids (no pisamos
        ' una importación previa hecha desde otro módulo).
        If proyecto IsNot Nothing Then
            Try
                Dim hojasTodas = Funciones_00_Varias.ObtenerHojasExcel(rutaExcel)
                If hojasTodas IsNot Nothing Then
                    Dim hGrids = Funciones_00_Varias.ResolverNombreHoja(hojasTodas,
                                                                        "Grid Definitions - Grid Lines",
                                                                        "Grid Lines")
                    If Not String.IsNullOrEmpty(hGrids) Then
                        Dim gridsExistentes As Integer = 0
                        If proyecto.Elementos.Grids IsNot Nothing AndAlso
                           proyecto.Elementos.Grids.GridLines IsNot Nothing Then
                            gridsExistentes = proyecto.Elementos.Grids.GridLines.Count
                        End If

                        Dim tablaGrids = Funciones_00_Varias.LeerHojaExcel(rutaExcel, hGrids)
                        Dim nuevos = Funciones_00_Varias.DataTableToGridLines(tablaGrids)
                        If nuevos IsNot Nothing AndAlso nuevos.Count > 0 Then
                            proyecto.Elementos.Grids.GridLines = nuevos
                            r.GridsImportados = nuevos.Count
                            If gridsExistentes > 0 Then
                                r.NotasInformativas.Add(
                                    $"Se reemplazaron {gridsExistentes} ejes previos por {nuevos.Count} del Excel actual.")
                            End If
                        End If
                    Else
                        r.NotasInformativas.Add(
                            "El Excel no trae la hoja 'Grid Lines' / 'Grid Definitions - Grid Lines'. " &
                            "La planta mostrará solo la cuadrícula métrica.")
                    End If
                End If
            Catch ex As Exception
                Logger.Warning("PiloteImportService.Importar",
                               "No se pudieron importar los grids: " & ex.Message)
            End Try
        End If

        Dim candidatos As List(Of cCandidatoPila)
        Try
            candidatos = Funciones_00_Varias.DetectarCandidatosETABS(rutaExcel, toleranciaXY)
        Catch ex As Exception
            Logger.Error(ex, "PiloteImportService.Importar", rutaExcel)
            r.ErrorFatal = "Error leyendo el Excel: " & ex.Message
            Return r
        End Try

        If candidatos Is Nothing OrElse candidatos.Count = 0 Then
            r.ErrorFatal = "No se detectaron apoyos (ni Frame ni Pier) en el archivo."
            Return r
        End If

        ' Diccionario de dados existentes por identificador para fusión.
        ' Clave compuesta "SourceType|SourceName" para evitar colisiones entre
        ' una columna Frame y un muro Pier que compartan label numérico.
        Dim existentes As New Dictionary(Of String, Dado)(StringComparer.OrdinalIgnoreCase)
        For Each d In contenedor.Dados
            Dim k = ClaveDado(d.SourceType, d.SourceName, d.JointLabel)
            If Not existentes.ContainsKey(k) Then existentes(k) = d
        Next

        Dim combosVistos As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        For Each c In candidatos
            Dim tipo = If(c.Tipo, "")
            Dim label = If(c.SourceLabel, c.Nombre)

            Dim dado As Dado = Nothing
            Dim clave = ClaveDado(tipo, label, label)
            If existentes.TryGetValue(clave, dado) Then
                r.Actualizados += 1
            Else
                dado = New Dado With {
                    .JointLabel = label,
                    .SourceType = tipo,
                    .SourceName = label,
                    .NombreTipoDado = ""
                }
                contenedor.Dados.Add(dado)
                r.Nuevos += 1
            End If

            dado.X_Global = c.X
            dado.Y_Global = c.Y
            dado.Z_Base = c.Z
            ' No pisa SourceType ni SourceName si ya existía — se preserva la
            ' identidad del apoyo. Pero sí pisa combos: la importación es la
            ' fuente de verdad para las demandas.
            dado.Combinaciones = TransformarReacciones(c.Reactions)

            For Each combo In dado.Combinaciones
                If Not String.IsNullOrWhiteSpace(combo.LoadCase) Then
                    combosVistos.Add(combo.LoadCase)
                End If
            Next

            If tipo.Equals("Frame", StringComparison.OrdinalIgnoreCase) Then
                r.FrameCount += 1
            ElseIf tipo.Equals("Pier", StringComparison.OrdinalIgnoreCase) Then
                r.PierCount += 1
            End If
        Next

        ' Poblar listas de combinaciones a nivel contenedor, igual que hacen
        ' zapatas y pilas; esto habilita el selector de combos de diseño.
        For Each lc In combosVistos
            If Not contenedor.Lista_Combinaciones.Contains(lc) Then
                contenedor.Lista_Combinaciones.Add(lc)
            End If
        Next

        r.TotalCombinaciones = combosVistos.Count
        Return r
    End Function

    ''' <summary>
    ''' Convierte List(Of cCombinacionPila) (lo que trae cCandidatoPila.Reactions)
    ''' a List(Of CombinacionDado) (el modelo propio de Pilotes). Los signos
    ''' ya vienen correctos de la detección: FZ > 0 = compresión en los dos
    ''' modos (Frame directo, Pier ya invertido por ETABSReaccionesPier).
    ''' </summary>
    Private Function TransformarReacciones(src As List(Of cCombinacionPila)) As List(Of CombinacionDado)
        Dim lista As New List(Of CombinacionDado)
        If src Is Nothing Then Return lista
        For Each s In src
            lista.Add(New CombinacionDado With {
                .LoadCase = If(s.LoadCase, ""),
                .FZ = s.FZ,
                .VX = s.FX,
                .VY = s.FY,
                .MX = s.MX,
                .MY = s.MY
            })
        Next
        Return lista
    End Function

    Private Function ClaveDado(sourceType As String, sourceName As String, fallbackLabel As String) As String
        Dim name = If(String.IsNullOrEmpty(sourceName), fallbackLabel, sourceName)
        Return (If(sourceType, "")).ToUpperInvariant() & "|" & (If(name, "")).ToUpperInvariant()
    End Function

    ''' <summary>Resumen de una importación, mostrado al usuario al terminar.</summary>
    Public Class ResultadoImportacion
        Public Property RutaOrigen As String = ""
        Public Property Nuevos As Integer
        Public Property Actualizados As Integer
        Public Property FrameCount As Integer
        Public Property PierCount As Integer
        Public Property TotalCombinaciones As Integer
        Public Property GridsImportados As Integer
        Public Property ErrorFatal As String = ""

        ''' <summary>True si el archivo no tenía las hojas Pier (modelo sin muros).</summary>
        Public Property ModeloSinPier As Boolean

        ''' <summary>Avisos informativos al usuario (no bloqueantes).</summary>
        Public Property NotasInformativas As New List(Of String)

        Public ReadOnly Property Total As Integer
            Get
                Return Nuevos + Actualizados
            End Get
        End Property

        Public ReadOnly Property Exito As Boolean
            Get
                Return String.IsNullOrEmpty(ErrorFatal) AndAlso Total > 0
            End Get
        End Property
    End Class

End Module
