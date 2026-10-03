' ============================================================================
' PreflightValidador — chequeos de prerrequisitos antes de ejecutar un handler.
'
' Patrón único en todos los módulos: cada "Procesar", "Reportar" o "Exportar"
' empieza con una llamada corta que verifica que haya datos suficientes y, si
' no los hay, muestra al ingeniero QUÉ falta y DÓNDE conseguirlo.
'
'   If Not PreflightValidador.HayMurosImportados(proyecto) Then Return
'
' Convención: la función devuelve True cuando se puede continuar. En caso
' contrario muestra un MessageBox (a menos que se pase mostrarMensaje:=False
' para pruebas o reutilización silenciosa) y devuelve False.
'
' Mensajes orientativos: siempre indican el menú/pestaña donde el usuario
' encuentra lo que falta — nunca un "Sin datos" seco.
' ============================================================================
Public Module PreflightValidador

    Private Const TITULO_FALTA_DATOS As String = "Datos insuficientes"

    ' ---------------------------------------------------------------------
    ' Infraestructura
    ' ---------------------------------------------------------------------

    Private Function Avisar(mensaje As String, mostrar As Boolean,
                            Optional titulo As String = TITULO_FALTA_DATOS) As Boolean
        If mostrar Then
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
        Return False
    End Function

    Public Function HayProyectoActivo(proyecto As Proyecto,
                                      Optional mostrarMensaje As Boolean = True) As Boolean
        If proyecto Is Nothing Then
            Return Avisar(
                "No hay un proyecto abierto." & vbCrLf & vbCrLf &
                "Cree uno nuevo o abra un .esm existente desde el menú Archivo.",
                mostrarMensaje)
        End If
        If proyecto.Elementos Is Nothing Then
            Return Avisar(
                "El proyecto está corrupto (sin estructura de Elementos)." & vbCrLf &
                "Intente abrir el .esm nuevamente o cree uno nuevo.",
                mostrarMensaje)
        End If
        Return True
    End Function

    ' ---------------------------------------------------------------------
    ' Vigas
    ' ---------------------------------------------------------------------

    Public Function HayVigasImportadas(proyecto As Proyecto,
                                       Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Vigas Is Nothing OrElse
           proyecto.Elementos.Vigas.Frames Is Nothing OrElse
           proyecto.Elementos.Vigas.Frames.Count = 0 Then
            Return Avisar(
                "No hay vigas importadas desde ETABS." & vbCrLf & vbCrLf &
                "Vaya al menú ""Importar → Importar Demandas"" dentro del módulo de Vigas " &
                "y cargue el archivo exportado de ETABS.",
                mostrarMensaje)
        End If
        Return True
    End Function

    Public Function HayVigasProcesadas(proyecto As Proyecto,
                                       Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayVigasImportadas(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Vigas.Vigas Is Nothing OrElse
           proyecto.Elementos.Vigas.Vigas.Count = 0 Then
            Return Avisar(
                "Las vigas están importadas pero no procesadas." & vbCrLf & vbCrLf &
                "En el módulo de Vigas presione el botón ""Procesar"" para armar las " &
                "vigas a partir de los frames y calcular las demandas.",
                mostrarMensaje)
        End If
        Return True
    End Function

    ' ---------------------------------------------------------------------
    ' Columnas
    ' ---------------------------------------------------------------------

    Public Function HayColumnasImportadas(proyecto As Proyecto,
                                          Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Columnas Is Nothing OrElse
           proyecto.Elementos.Columnas.Lista_Columnas Is Nothing OrElse
           proyecto.Elementos.Columnas.Lista_Columnas.Count = 0 Then
            Return Avisar(
                "No hay columnas importadas desde ETABS." & vbCrLf & vbCrLf &
                "Vaya al módulo de Columnas y presione ""Importar"" para cargar las " &
                "secciones, fuerzas y, si aplica, el diseño de ETABS.",
                mostrarMensaje)
        End If
        Return True
    End Function

    Public Function HayColumnasProcesadas(proyecto As Proyecto,
                                          Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayColumnasImportadas(proyecto, mostrarMensaje) Then Return False
        Dim tieneTramos = proyecto.Elementos.Columnas.Lista_Columnas.
            Any(Function(c) c.Lista_Tramos_Columnas IsNot Nothing AndAlso
                            c.Lista_Tramos_Columnas.Count > 0)
        If Not tieneTramos Then
            Return Avisar(
                "Las columnas están importadas pero sin tramos calculados." & vbCrLf & vbCrLf &
                "En el módulo de Columnas presione ""Calcular"" para generar los tramos " &
                "y evaluar diagramas de interacción.",
                mostrarMensaje)
        End If
        Return True
    End Function

    ' ---------------------------------------------------------------------
    ' Muros
    ' ---------------------------------------------------------------------

    Public Function HayMurosImportados(proyecto As Proyecto,
                                       Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Muros Is Nothing OrElse
           proyecto.Elementos.Muros.Lista_Muros Is Nothing OrElse
           proyecto.Elementos.Muros.Lista_Muros.Count = 0 Then
            Return Avisar(
                "No hay muros importados desde ETABS." & vbCrLf & vbCrLf &
                "Vaya al módulo de Muros, menú ""Importar Datos ETABS"", y cargue las " &
                "hojas de Pier Design/Pier Forces/Shear Wall Pier Summary.",
                mostrarMensaje)
        End If
        Return True
    End Function

    Public Function HayMurosConSecciones(proyecto As Proyecto,
                                         Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayMurosImportados(proyecto, mostrarMensaje) Then Return False
        If Not proyecto.Elementos.Muros.Info_Secciones Then
            Return Avisar(
                "Faltan las secciones de los muros." & vbCrLf & vbCrLf &
                "En el módulo de Muros, menú ""Importar Datos ETABS"", cargue la hoja " &
                "de secciones (Pier Section Properties) antes de procesar.",
                mostrarMensaje)
        End If
        Return True
    End Function

    Public Function HayMurosProcesados(proyecto As Proyecto,
                                       Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayMurosImportados(proyecto, mostrarMensaje) Then Return False
        Dim tieneRef = proyecto.Elementos.Muros.Lista_Muros.
            Any(Function(m) m.Ref_Modificado_Muros)
        If Not tieneRef Then
            Return Avisar(
                "Los muros están importados pero sin refuerzo asignado." & vbCrLf & vbCrLf &
                "En el módulo de Muros abra ""Información de Muros"", ingrese el refuerzo " &
                "colocado por sección y presione Guardar para que se evalúe la capacidad.",
                mostrarMensaje)
        End If
        Return True
    End Function

    ' ---------------------------------------------------------------------
    ' Pilas
    ' ---------------------------------------------------------------------

    Public Function HayPilasImportadas(proyecto As Proyecto,
                                       Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Pilas Is Nothing OrElse
           proyecto.Elementos.Pilas.ListaElementos Is Nothing OrElse
           proyecto.Elementos.Pilas.ListaElementos.Count = 0 Then
            Return Avisar(
                "No hay pilas importadas desde ETABS." & vbCrLf & vbCrLf &
                "Vaya al módulo de Pilas, menú ""Importar"", y cargue el modelo ETABS " &
                "con las reacciones de los nodos de apoyo.",
                mostrarMensaje)
        End If
        Return True
    End Function

    ' ---------------------------------------------------------------------
    ' Zapatas
    ' ---------------------------------------------------------------------

    Public Function HayReaccionesDeZapatas(proyecto As Proyecto,
                                           Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Zapatas Is Nothing OrElse
           proyecto.Elementos.Zapatas.Reactions Is Nothing OrElse
           proyecto.Elementos.Zapatas.Reactions.Count = 0 Then
            Return Avisar(
                "No hay reacciones de nodos de apoyo cargadas." & vbCrLf & vbCrLf &
                "En el módulo de Zapatas vaya a ""Importar"" y cargue el Excel ETABS con " &
                "la hoja ""Joint Reactions"" antes de asignar tipos de zapata.",
                mostrarMensaje)
        End If
        Return True
    End Function

    Public Function HayZapatasImportadas(proyecto As Proyecto,
                                         Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Zapatas Is Nothing OrElse
           proyecto.Elementos.Zapatas.Apoyos Is Nothing OrElse
           proyecto.Elementos.Zapatas.Apoyos.Count = 0 Then
            Return Avisar(
                "No hay apoyos de zapatas importados desde ETABS." & vbCrLf & vbCrLf &
                "Vaya al módulo de Zapatas, menú ""Importar"", y cargue el Excel que " &
                "contenga la hoja ""Joint Reactions"" (obligatoria) y, opcionalmente, " &
                "las hojas de nodos y ejes para la vista en planta.",
                mostrarMensaje)
        End If
        Return True
    End Function

    Public Function HayZapatasProcesadas(proyecto As Proyecto,
                                         Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayZapatasImportadas(proyecto, mostrarMensaje) Then Return False
        Dim tieneResultados = proyecto.Elementos.Zapatas.Apoyos.
            Any(Function(a) a.Resultados IsNot Nothing AndAlso a.Resultados.Count > 0)
        If Not tieneResultados Then
            Return Avisar(
                "Las zapatas están importadas pero sin resultados." & vbCrLf & vbCrLf &
                "En el módulo de Zapatas presione ""Procesar"" para evaluar las cinco " &
                "revisiones (C/D de capacidad portante, flexión, cortante, punzonamiento, excentricidad).",
                mostrarMensaje)
        End If
        Return True
    End Function

    ' ---------------------------------------------------------------------
    ' Nervios
    ' ---------------------------------------------------------------------

    Public Function HayNerviosImportados(proyecto As Proyecto,
                                         Optional mostrarMensaje As Boolean = True) As Boolean
        If Not HayProyectoActivo(proyecto, mostrarMensaje) Then Return False
        If proyecto.Elementos.Nervios Is Nothing OrElse
           proyecto.Elementos.Nervios.Elementos Is Nothing OrElse
           proyecto.Elementos.Nervios.Elementos.Count = 0 Then
            Return Avisar(
                "No hay nervios importados desde ETABS." & vbCrLf & vbCrLf &
                "Vaya al módulo de Nervios, menú ""Importar"", y cargue el Excel ETABS " &
                "con los frames de las losas nervadas.",
                mostrarMensaje)
        End If
        Return True
    End Function

End Module
