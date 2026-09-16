Imports ARCO.eNumeradores
Imports ARCO.Funciones_00_Varias
Imports DocumentFormat.OpenXml.Office.PowerPoint.Y2022.M03.Main

Public Class Form_07_Pag_Zapatas
    Public Shared Proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Private _hayCambiosZapatas As Boolean = False

    Private Sub Form_07_Pag_Zapatas_LoadAdaptable(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Ajusta la ventana al monitor y habilita scroll vertical: la maqueta
        ' de este formulario tiene Y y altos fijos y no cabe en pantallas bajas.
        PilaVerticalAdaptable.AjustarAPantallaConScroll(Me)
        PrepararColumnaTipoApoyo()
        PrepararColumnasDesplanteYConcreto()
        AgregarMenuAyudaTablas()
        AgregarMenuPlanta()
        AgregarMenuGraficas()
        AgregarBotonVerDetalle()
        AgregarPanelPesoEstabilizante()
        RefrescarUIPesoEstabilizante()
        AjustarAnchoTablaElementos()
    End Sub

    ''' <summary>
    ''' Con 17 columnas a 125 px cada una la tabla necesita ~2100 px, mucho más
    ''' que el Panel2 (~1238 px), y aparecía una barra horizontal. Se pone en
    ''' Fill con pesos por columna para que se repartan el ancho disponible:
    ''' los textos (etiqueta, nombre, tipo de apoyo) ganan más peso, los números
    ''' cortos (diámetros, cantidades, fc) van con menos.
    ''' </summary>
    Private Sub AjustarAnchoTablaElementos()

        If Tabla_Elementos Is Nothing Then Exit Sub
        If Tabla_Elementos.Columns.Count = 0 Then Exit Sub

        Tabla_Elementos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Tabla_Elementos.RowHeadersWidth = 30

        ' Índices originales del Designer:
        ' 0 Label, 1 Nombre, 2 b, 3 h, 4 e, 5 L_b, 6 L_h,
        ' 7 Ref_L1_Diam, 8 Ref_L1_Area, 9 Ref_L1_Cant,
        ' 10 Ref_L2_Diam, 11 Ref_L2_Area, 12 Ref_L2_Cant, 13 fc
        Dim pesosBase() As Integer = {90, 90, 60, 60, 60, 65, 65,
                                       60, 65, 65, 60, 65, 65, 60}
        For i As Integer = 0 To Math.Min(pesosBase.Length, Tabla_Elementos.Columns.Count) - 1
            Tabla_Elementos.Columns(i).FillWeight = pesosBase(i)
        Next

        ' Columnas agregadas en runtime
        If Tabla_Elementos.Columns.Contains(COL_TIPO_APOYO) Then _
            Tabla_Elementos.Columns(COL_TIPO_APOYO).FillWeight = 95
        If Tabla_Elementos.Columns.Contains(COL_DF) Then _
            Tabla_Elementos.Columns(COL_DF).FillWeight = 55
        If Tabla_Elementos.Columns.Contains(COL_GCONC) Then _
            Tabla_Elementos.Columns(COL_GCONC).FillWeight = 75
        If Tabla_Elementos.Columns.Contains(COL_GRUPO) Then _
            Tabla_Elementos.Columns(COL_GRUPO).FillWeight = 55
        If Tabla_Elementos.Columns.Contains(COL_PATRON) Then _
            Tabla_Elementos.Columns(COL_PATRON).FillWeight = 45

    End Sub

    ''' <summary>
    ''' Menú "? Tablas ETABS", igual que en Pilas, Columnas, Muros y Vigas.
    ''' Zapatas ya tenía la ficha de ayuda definida, pero ningún menú la abría.
    ''' </summary>
    Private Sub AgregarMenuAyudaTablas()

        Dim itemAyuda As New ToolStripMenuItem("? Tablas ETABS") With {
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(87, 87, 87),
            .ToolTipText = "Qué hojas necesita este módulo y cómo se llaman en E17 y E23"
        }
        AddHandler itemAyuda.Click, Sub(s, ev) Form_AyudaImportacion.MostrarModulo("Zapatas")
        MenuStrip1.Items.Add(itemAyuda)
    End Sub

    ' =====================================================================
    ' PLANTA DE CIMENTACIÓN
    ' =====================================================================

    Private _planta As Form_Planta_Zapatas = Nothing

    ''' <summary>
    ''' Agrega "Planta de cimentación..." al menú Ver. Es la forma de revisar de
    ''' un vistazo la clasificación de medianeras y esquineras: sobre la planta
    ''' se ve enseguida si el programa acertó, cosa que en una tabla de cien
    ''' filas es imposible.
    ''' </summary>
    Private Sub AgregarMenuPlanta()

        Dim item As New ToolStripMenuItem("Planta de cimentación...") With {
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(87, 87, 87),
            .ToolTipText = "Vista en planta con ejes, tipo de apoyo y estado de cada zapata"
        }
        AddHandler item.Click, AddressOf AbrirPlanta_Click
        Ver_Zapatas.DropDownItems.Add(item)

    End Sub

    Private Sub AbrirPlanta_Click(sender As Object, e As EventArgs)

        If Proyecto.Elementos.Zapatas.Tipos Is Nothing OrElse Proyecto.Elementos.Zapatas.Tipos.Count = 0 Then
            MessageBox.Show("Primero importe y calcule las zapatas.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Una sola ventana: reabrirla la trae al frente con los datos al día.
        If _planta IsNot Nothing AndAlso Not _planta.IsDisposed Then
            _planta.Zapatas = Proyecto.Elementos.Zapatas.Tipos
            _planta.GridLines = Proyecto.Elementos.Grids.GridLines
            _planta.Refrescar()
            _planta.BringToFront()
            Return
        End If

        _planta = New Form_Planta_Zapatas() With {
            .Zapatas = Proyecto.Elementos.Zapatas.Tipos,
            .GridLines = Proyecto.Elementos.Grids.GridLines
        }
        AddHandler _planta.ZapataSeleccionada, AddressOf SeleccionarDesdeLaPlanta
        _planta.Show(Me)

    End Sub

    ''' <summary>Al hacer clic en la planta, se lleva la tabla a esa fila.</summary>
    Private Sub SeleccionarDesdeLaPlanta(z As cZapata)

        If z Is Nothing OrElse Tabla_Elementos Is Nothing Then Exit Sub

        For Each fila As DataGridViewRow In Tabla_Elementos.Rows
            If fila.IsNewRow Then Continue For
            If Not String.Equals(Convert.ToString(fila.Cells(0).Value),
                                 Convert.ToString(z.Label_joint), StringComparison.OrdinalIgnoreCase) Then Continue For

            Tabla_Elementos.ClearSelection()
            fila.Selected = True
            Tabla_Elementos.FirstDisplayedScrollingRowIndex = fila.Index
            Exit For
        Next

    End Sub

    ' =====================================================================
    ' GRÁFICAS RESUMEN
    ' =====================================================================
    ' Dashboard con las 5 revisiones C/D (peor global, suelo, excentricidad,
    ' punzonamiento, cortante+flexión) y dos conteos (tipo de apoyo y
    ' cumple/no cumple). Comparte la infraestructura de GraficosResumen con
    ' los otros cinco módulos.

    ''' <summary>
    ''' Agrega "Gráficas resumen..." al menú Ver. Se registra como método aparte
    ''' de AgregarMenuPlanta para minimizar el roce con otros cambios al menú.
    ''' </summary>
    Private Sub AgregarMenuGraficas()

        Dim item As New ToolStripMenuItem("Gráficas resumen...") With {
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(87, 87, 87),
            .ToolTipText = "Barras C/D por revisión y conteos"
        }
        AddHandler item.Click, AddressOf AbrirGraficas_Click
        Ver_Zapatas.DropDownItems.Add(item)

    End Sub

    Private Sub AbrirGraficas_Click(sender As Object, e As EventArgs)

        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse
           Proyecto.Elementos.Zapatas Is Nothing OrElse
           Proyecto.Elementos.Zapatas.Tipos Is Nothing OrElse
           Proyecto.Elementos.Zapatas.Tipos.Count = 0 Then
            MessageBox.Show("Primero calcule las zapatas.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Dim f As New Form_Graficos_Zapatas() With {
                .Zapatas = Proyecto.Elementos.Zapatas.Tipos
            }
            f.Show(Me)
        Catch ex As Exception
            Logger.Error(ex, "Form_07_Pag_Zapatas.AbrirGraficas_Click")
            MessageBox.Show("No se pudo abrir la ventana de gráficas." & vbCrLf & ex.Message,
                            "ARCO", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

    End Sub

    ' =====================================================================
    ' VER DETALLE DE UNA ZAPATA (Form_07_Zapata_Detalle)
    ' =====================================================================
    ''' <summary>
    ''' Agrega al menú Ver el item que abre la vista de análisis fino de la
    ''' zapata seleccionada en Tabla_Elementos: presiones, capacidad vs demanda,
    ''' envolvente por combinación y secciones críticas. Método independiente
    ''' para minimizar el roce con otras integraciones del mismo menú.
    ''' </summary>
    Private Sub AgregarBotonVerDetalle()

        Try
            Dim item As New ToolStripMenuItem("Ver detalle de zapata seleccionada...") With {
                .ForeColor = Color.White,
                .BackColor = Color.FromArgb(87, 87, 87),
                .ToolTipText = "Presiones bajo la zapata, capacidad vs demanda, envolvente y secciones críticas"
            }
            AddHandler item.Click, AddressOf AbrirDetalleZapata_Click
            Ver_Zapatas.DropDownItems.Add(item)
        Catch ex As Exception
            Logger.Error(ex, "Form_07_Pag_Zapatas.AgregarBotonVerDetalle", "")
        End Try

    End Sub

    Private Sub AbrirDetalleZapata_Click(sender As Object, e As EventArgs)

        Try
            If Tabla_Elementos Is Nothing OrElse Tabla_Elementos.SelectedRows Is Nothing OrElse
               Tabla_Elementos.SelectedRows.Count = 0 Then
                MessageBox.Show("Seleccione una fila en la tabla de zapatas antes de abrir el detalle.",
                                "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim fila = Tabla_Elementos.SelectedRows(0)
            If fila Is Nothing OrElse fila.IsNewRow Then
                MessageBox.Show("La fila seleccionada no corresponde a una zapata.",
                                "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim etiqueta = Convert.ToString(fila.Cells(0).Value)
            If String.IsNullOrEmpty(etiqueta) Then
                MessageBox.Show("La zapata seleccionada no tiene etiqueta.",
                                "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim zapatas = Proyecto?.Elementos?.Zapatas?.Tipos
            If zapatas Is Nothing OrElse zapatas.Count = 0 Then
                MessageBox.Show("No hay zapatas cargadas en el proyecto.",
                                "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim z = zapatas.FirstOrDefault(Function(x) String.Equals(Convert.ToString(x.Label_joint), etiqueta,
                                                                     StringComparison.OrdinalIgnoreCase))
            If z Is Nothing Then
                MessageBox.Show($"No se encontró la zapata ""{etiqueta}"" en el modelo.",
                                "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Form_07_Zapata_Detalle.Mostrar(z, Me)

        Catch ex As Exception
            Logger.Error(ex, "Form_07_Pag_Zapatas.AbrirDetalleZapata_Click", "")
            MessageBox.Show("No se pudo abrir el detalle: " & ex.Message,
                            "Detalle de zapata", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    ' =====================================================================
    ' PESO ESTABILIZANTE Y LÍMITE DE EXCENTRICIDAD DINÁMICA
    ' =====================================================================
    ' Ambos parámetros son de proyecto: se aplican a todas las zapatas.
    ' Peso estabilizante = W_zapata + W_pedestal + W_suelo, se suma al P para
    ' revisar suelo y excentricidad. Punzonamiento, cortante y flexión NO lo
    ' usan.
    ' Límite excentricidad dinámica: por norma estático fijo en L/6; en sismo
    ' se admite relajar hasta L/N (típico L/4 o L/3). Editable acá.

    Private _chkPesoEstabilizante As CheckBox
    Private _numLimExcDin As NumericUpDown

    Private Sub AgregarPanelPesoEstabilizante()

        If Panel3 Is Nothing Then Exit Sub

        ' Reorganización de la columna derecha con separación uniforme de 12 px
        ' vertical, X a 10 px de la columna izquierda, y top alineado con
        ' GroupBox2 (que es la referencia visual de la columna izquierda).
        Const gapVer As Integer = 12
        Dim xColDer As Integer = 423
        Dim yTop As Integer = 86
        If GroupBox2 IsNot Nothing Then
            xColDer = GroupBox2.Right + 10
            yTop = GroupBox2.Top
        End If
        Dim y As Integer = yTop
        For Each g In {GroupBox4, GroupBox8, GroupBox1, GroupBox7}
            If g Is Nothing Then Continue For
            g.Location = New Point(xColDer, y)
            y = g.Bottom + gapVer
        Next
        Dim yBase As Integer = If(GroupBox7 IsNot Nothing, GroupBox7.Bottom + gapVer, y)

        ' Ancho y estilo tomados del GroupBox7 para que el grupo quede
        ' visualmente idéntico al resto de la columna derecha (296 px en el
        ' Designer, pero se lee dinámicamente por si se cambia el maestro).
        Dim wCol As Integer = If(GroupBox7 IsNot Nothing, GroupBox7.Width, 296)

        Dim grp As New GroupBox() With {
            .Location = New Point(xColDer, yBase),
            .Size = New Size(wCol, 140),
            .Text = "Condiciones adicionales",
            .Font = New Font("Microsoft Sans Serif", 11.0!, FontStyle.Bold),
            .ForeColor = Color.White
        }

        ' Checkbox en dos líneas para que quepa el texto completo dentro del
        ' ancho del grupo. Altura 52 px porque a 9.5 pt cada línea ocupa ~16 px
        ' y el "cuadrito" del checkbox roba ~4 px arriba y abajo; con 42 se
        ' cortaba la segunda línea y "suelo" quedaba invisible.
        _chkPesoEstabilizante = New CheckBox() With {
            .Location = New Point(12, 26),
            .Size = New Size(wCol - 24, 52),
            .Text = "Considerar peso zapata + pedestal +" & vbCrLf & "suelo",
            .Font = New Font("Microsoft Sans Serif", 9.5!, FontStyle.Regular),
            .ForeColor = Color.White,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        AddHandler _chkPesoEstabilizante.CheckedChanged, AddressOf ChkPesoEstabilizante_CheckedChanged

        ' Etiqueta y NumericUpDown pegados: antes había ~170 px entre el texto
        ' y el control (por copiar el patrón de FD_D con label ancho), lo que
        ' se veía desconectado. Ahora Label auto-anchable + control adyacente.
        Dim lbl As New Label() With {
            .Location = New Point(12, 92),
            .AutoSize = True,
            .Text = "Excentricidad dinámica: L /",
            .Font = New Font("Microsoft Sans Serif", 9.5!, FontStyle.Regular),
            .ForeColor = Color.White
        }

        _numLimExcDin = New NumericUpDown() With {
            .Size = New Size(58, 27),
            .Font = New Font("Arial", 10.0!, FontStyle.Regular),
            .Minimum = 2D,
            .Maximum = 10D,
            .DecimalPlaces = 0,
            .Value = 4D,
            .TextAlign = HorizontalAlignment.Center
        }
        AddHandler _numLimExcDin.ValueChanged, AddressOf NumLimExcDin_ValueChanged
        ' La posición X del selector se decide DESPUÉS de que se sepa el ancho
        ' real de la etiqueta (AutoSize la mide al agregarla al grupo).
        _numLimExcDin.Location = New Point(0, 80)  ' Y aprox; X se corrige tras Controls.Add

        grp.Controls.Add(_chkPesoEstabilizante)
        grp.Controls.Add(lbl)
        grp.Controls.Add(_numLimExcDin)
        Panel3.Controls.Add(grp)

        ' Ya con el Label agregado se conoce su ancho real: se pega el
        ' selector a 6 px del final del texto.
        _numLimExcDin.Location = New Point(lbl.Right + 6, lbl.Top - 2)

        ' El botón "Ejecutar" del Designer (Button2) estaba en (448, 533), que
        ' quedaba encima del grupo de peso estabilizante. Se recoloca justo
        ' debajo de este grupo, alineado con la columna derecha, para que el
        ' flujo Materiales → Factores → Peso → Ejecutar quede en orden. El
        ' botón "Calcular" (Button1 en Panel4, docked al fondo del formulario)
        ' no se toca: es el paso final que corre el cálculo sobre toda la tabla.
        If Button2 IsNot Nothing Then
            Button2.Location = New Point(xColDer, grp.Bottom + 12)
            Button2.Size = New Size(wCol, 50)
            Button2.BringToFront()
        End If

    End Sub

    ''' <summary>
    ''' Refresca las dos entradas con lo que tenga el proyecto. Se llama al abrir
    ''' el formulario y también al abrir un .esm para reflejar lo guardado.
    ''' </summary>
    Private Sub RefrescarUIPesoEstabilizante()
        If _chkPesoEstabilizante Is Nothing Then Exit Sub
        If Proyecto Is Nothing OrElse Proyecto.Elementos Is Nothing OrElse Proyecto.Elementos.Zapatas Is Nothing Then Exit Sub

        _chkPesoEstabilizante.Checked = Proyecto.Elementos.Zapatas.UsarPesoEstabilizante
        Dim n As Double = Proyecto.Elementos.Zapatas.LimiteExcentricidadDinamicaN
        If n < 2 Then n = 4
        If n > 10 Then n = 10
        _numLimExcDin.Value = CDec(Math.Round(n))
    End Sub

    Private Sub ChkPesoEstabilizante_CheckedChanged(sender As Object, e As EventArgs)
        If Proyecto?.Elementos?.Zapatas Is Nothing Then Exit Sub
        Proyecto.Elementos.Zapatas.UsarPesoEstabilizante = _chkPesoEstabilizante.Checked
        _hayCambiosZapatas = True
    End Sub

    Private Sub NumLimExcDin_ValueChanged(sender As Object, e As EventArgs)
        If Proyecto?.Elementos?.Zapatas Is Nothing Then Exit Sub
        Proyecto.Elementos.Zapatas.LimiteExcentricidadDinamicaN = CDbl(_numLimExcDin.Value)
        _hayCambiosZapatas = True
    End Sub

    ' =====================================================================
    ' Columnas Df (profundidad de desplante) y γ_concreto en Tabla_Elementos
    ' =====================================================================

    Private Const COL_DF As String = "ColDf"
    Private Const COL_GCONC As String = "ColGammaConcreto"
    Private Const COL_GRUPO As String = "ColGrupo"
    Private Const COL_PATRON As String = "ColPatron"

    Private Sub PrepararColumnasDesplanteYConcreto()
        If Tabla_Elementos Is Nothing Then Exit Sub

        If Not Tabla_Elementos.Columns.Contains(COL_DF) Then
            Dim colDf As New DataGridViewTextBoxColumn() With {
                .Name = COL_DF,
                .HeaderText = "Df (m)",
                .Width = 80,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            }
            Tabla_Elementos.Columns.Add(colDf)
        End If

        If Not Tabla_Elementos.Columns.Contains(COL_GCONC) Then
            Dim colG As New DataGridViewTextBoxColumn() With {
                .Name = COL_GCONC,
                .HeaderText = "γ concreto (kN/m³)",
                .Width = 110,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            }
            Tabla_Elementos.Columns.Add(colG)
        End If

        ' Columna Grupo: el usuario escribe un nombre (ej. "Z1"). Las zapatas
        ' con el mismo nombre comparten geometría/refuerzo con la que esté
        ' marcada como patrón en la columna siguiente.
        If Not Tabla_Elementos.Columns.Contains(COL_GRUPO) Then
            Dim colGr As New DataGridViewTextBoxColumn() With {
                .Name = COL_GRUPO,
                .HeaderText = "Grupo",
                .Width = 80,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            }
            Tabla_Elementos.Columns.Add(colGr)
        End If

        ' Columna Patrón: checkbox. Al marcar, se desmarcan las demás del mismo
        ' grupo (exclusividad) — se maneja en CellValueChanged más abajo.
        If Not Tabla_Elementos.Columns.Contains(COL_PATRON) Then
            Dim colP As New DataGridViewCheckBoxColumn() With {
                .Name = COL_PATRON,
                .HeaderText = "Patrón",
                .Width = 60,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            }
            Tabla_Elementos.Columns.Add(colP)
        End If
    End Sub

    ' =====================================================================
    ' Propagación patrón → hijas al calcular
    ' =====================================================================
    ' Justo antes de Calcular, cada grupo elige su patrón (o la primera fila
    ' si nadie está marcado) y copia SUS celdas de geometría/materiales/
    ' refuerzo/Df/γ_c a las celdas de las demás filas del grupo. Así el bucle
    ' que sigue lee valores ya sincronizados sin tener que conocer el
    ' concepto de grupo. Columnas que NO se copian: 0 Label_joint, 1 Nombre,
    ' Tipo de apoyo (depende de la posición), Grupo y Patrón.

    Private Shared ReadOnly _colsCopiablesPatron() As Integer = {
        2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13
    }
    Private Shared ReadOnly _colsCopiablesPatronNombradas() As String = {
        COL_DF, COL_GCONC
    }

    Private Sub PropagarPatronEnTabla()

        If Tabla_Elementos Is Nothing OrElse Tabla_Elementos.Rows.Count = 0 Then Exit Sub
        If Not Tabla_Elementos.Columns.Contains(COL_GRUPO) Then Exit Sub

        ' 1) Agrupar índices de fila por grupo (ignorando filas sin grupo).
        Dim grupos As New Dictionary(Of String, List(Of Integer))(StringComparer.OrdinalIgnoreCase)
        For i As Integer = 0 To Tabla_Elementos.Rows.Count - 1
            Dim fila = Tabla_Elementos.Rows(i)
            If fila.IsNewRow Then Continue For
            Dim gTxt = Convert.ToString(fila.Cells(COL_GRUPO).Value)
            If String.IsNullOrWhiteSpace(gTxt) Then Continue For
            gTxt = gTxt.Trim()
            If Not grupos.ContainsKey(gTxt) Then grupos(gTxt) = New List(Of Integer)
            grupos(gTxt).Add(i)
        Next

        _actualizandoTipoApoyo = True   ' reusa el guardián para evitar reentradas
        Try
            For Each kv In grupos
                If kv.Value.Count < 2 Then Continue For  ' grupo de una sola: nada que sincronizar

                ' 2) Buscar la patrón: la marcada, o la primera si no hay ninguna.
                Dim idxPatron As Integer = -1
                For Each idx In kv.Value
                    If CBool(If(Tabla_Elementos.Rows(idx).Cells(COL_PATRON).Value, False)) Then
                        idxPatron = idx
                        Exit For
                    End If
                Next
                If idxPatron < 0 Then
                    idxPatron = kv.Value.First()
                    Tabla_Elementos.Rows(idxPatron).Cells(COL_PATRON).Value = True
                End If

                ' 3) Copiar celdas de la patrón a cada hija.
                Dim filaPatron = Tabla_Elementos.Rows(idxPatron)
                For Each idxHija In kv.Value
                    If idxHija = idxPatron Then Continue For
                    Dim filaHija = Tabla_Elementos.Rows(idxHija)

                    For Each c As Integer In _colsCopiablesPatron
                        If c < Tabla_Elementos.Columns.Count Then
                            filaHija.Cells(c).Value = filaPatron.Cells(c).Value
                        End If
                    Next
                    For Each nom In _colsCopiablesPatronNombradas
                        If Tabla_Elementos.Columns.Contains(nom) Then
                            filaHija.Cells(nom).Value = filaPatron.Cells(nom).Value
                        End If
                    Next
                    ' Además, forzamos EsPatron=False para las hijas
                    filaHija.Cells(COL_PATRON).Value = False
                Next
            Next
        Finally
            _actualizandoTipoApoyo = False
        End Try

    End Sub

    ' =====================================================================
    ' TIPO DE APOYO — columna editable
    ' =====================================================================
    ' El punzonamiento depende de si la zapata es central, medianera o
    ' esquinera. El programa lo propone por geometría, pero el ingeniero manda:
    ' un voladizo, una junta de dilatación o una zapata combinada rompen la
    ' inferencia, y lo que se marque a mano no se vuelve a sobrescribir.

    Private Const COL_TIPO_APOYO As String = "ColTipoApoyo"
    Private _actualizandoTipoApoyo As Boolean = False

    Private Sub PrepararColumnaTipoApoyo()

        If Tabla_Elementos Is Nothing Then Exit Sub
        If Tabla_Elementos.Columns.Contains(COL_TIPO_APOYO) Then Exit Sub

        Dim col As New DataGridViewComboBoxColumn() With {
            .Name = COL_TIPO_APOYO,
            .HeaderText = "Tipo de apoyo",
            .Width = 130,
            .FlatStyle = FlatStyle.Flat,
            .DropDownWidth = 130
        }
        col.Items.AddRange(ZapataService.NombreTipo(eTipoApoyoZapata.Central),
                           ZapataService.NombreTipo(eTipoApoyoZapata.Medianera),
                           ZapataService.NombreTipo(eTipoApoyoZapata.Esquinera))

        Tabla_Elementos.Columns.Add(col)

    End Sub

    ''' <summary>Refleja en la tabla el tipo que tiene cada zapata en el modelo.</summary>
    Private Sub ActualizarColumnaTipoApoyo()

        If Tabla_Elementos Is Nothing OrElse Not Tabla_Elementos.Columns.Contains(COL_TIPO_APOYO) Then Exit Sub

        Dim zapatas = Proyecto.Elementos.Zapatas.Tipos
        If zapatas Is Nothing Then Exit Sub

        _actualizandoTipoApoyo = True
        Try
            For Each fila As DataGridViewRow In Tabla_Elementos.Rows
                If fila.IsNewRow Then Continue For
                Dim etiqueta = Convert.ToString(fila.Cells(0).Value)
                Dim z = zapatas.FirstOrDefault(Function(x) String.Equals(Convert.ToString(x.Label_joint), etiqueta,
                                                                         StringComparison.OrdinalIgnoreCase))
                If z Is Nothing Then Continue For

                fila.Cells(COL_TIPO_APOYO).Value = ZapataService.NombreTipo(z.TipoApoyo)

                ' Marca visual: lo fijado a mano se distingue de lo propuesto.
                If z.TipoApoyoManual Then
                    fila.Cells(COL_TIPO_APOYO).Style.ForeColor = ReporteGridHelpers.ColorEncabezado
                    fila.Cells(COL_TIPO_APOYO).Style.Font = ReporteGridHelpers.FuenteNegrita
                    fila.Cells(COL_TIPO_APOYO).ToolTipText = "Fijado manualmente: la clasificación automática no lo cambia."
                Else
                    fila.Cells(COL_TIPO_APOYO).ToolTipText = "Propuesto por la posición en planta. Puede corregirse."
                End If
            Next
        Finally
            _actualizandoTipoApoyo = False
        End Try

    End Sub

    Private Sub Tabla_Elementos_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles Tabla_Elementos.CellValueChanged

        If _actualizandoTipoApoyo Then Exit Sub
        If e.RowIndex < 0 Then Exit Sub

        ' Marcar patrón: garantizar exclusividad dentro del grupo. Se hace
        ' aquí para que el usuario vea el cambio al instante, sin esperar a
        ' Ejecutar/Calcular.
        If Tabla_Elementos.Columns.Contains(COL_PATRON) AndAlso
           e.ColumnIndex = Tabla_Elementos.Columns(COL_PATRON).Index Then
            Dim filaP = Tabla_Elementos.Rows(e.RowIndex)
            Dim marcada As Boolean = CBool(If(filaP.Cells(COL_PATRON).Value, False))
            If marcada Then
                Dim grupoP As String = Convert.ToString(filaP.Cells(COL_GRUPO).Value)
                If Not String.IsNullOrWhiteSpace(grupoP) Then
                    _actualizandoTipoApoyo = True
                    Try
                        For Each otra As DataGridViewRow In Tabla_Elementos.Rows
                            If otra.IsNewRow OrElse otra.Index = filaP.Index Then Continue For
                            Dim gOtra As String = Convert.ToString(otra.Cells(COL_GRUPO).Value)
                            If String.Equals(gOtra, grupoP, StringComparison.OrdinalIgnoreCase) Then
                                otra.Cells(COL_PATRON).Value = False
                            End If
                        Next
                    Finally
                        _actualizandoTipoApoyo = False
                    End Try
                End If
            End If
            _hayCambiosZapatas = True
            Return
        End If

        If Not Tabla_Elementos.Columns.Contains(COL_TIPO_APOYO) Then Exit Sub
        If e.ColumnIndex <> Tabla_Elementos.Columns(COL_TIPO_APOYO).Index Then Exit Sub

        Dim fila = Tabla_Elementos.Rows(e.RowIndex)
        Dim etiqueta = Convert.ToString(fila.Cells(0).Value)
        Dim zapatas = Proyecto.Elementos.Zapatas.Tipos
        If zapatas Is Nothing Then Exit Sub

        Dim z = zapatas.FirstOrDefault(Function(x) String.Equals(Convert.ToString(x.Label_joint), etiqueta,
                                                                 StringComparison.OrdinalIgnoreCase))
        If z Is Nothing Then Exit Sub

        Select Case Convert.ToString(fila.Cells(COL_TIPO_APOYO).Value)
            Case "Esquinera" : z.TipoApoyo = eTipoApoyoZapata.Esquinera
            Case "Medianera" : z.TipoApoyo = eTipoApoyoZapata.Medianera
            Case Else : z.TipoApoyo = eTipoApoyoZapata.Central
        End Select

        z.TipoApoyoManual = True
        _hayCambiosZapatas = True
        ActualizarColumnaTipoApoyo()

    End Sub

    ''' <summary>
    ''' Un ComboBox dentro de una grilla no confirma el valor hasta que la celda
    ''' pierde el foco. Esto hace que el cambio se registre al instante.
    ''' </summary>
    Private Sub Tabla_Elementos_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) _
        Handles Tabla_Elementos.CurrentCellDirtyStateChanged

        If Tabla_Elementos.IsCurrentCellDirty AndAlso
           TypeOf Tabla_Elementos.CurrentCell Is DataGridViewComboBoxCell Then
            Tabla_Elementos.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If

    End Sub


    Private Sub ImportarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ImportarDemandasToolStripMenuItem.Click


        Dim openFD As New OpenFileDialog()
        With openFD
            .Title = "Seleccionar archivo de resultados ETABS"
            .Filter = "Archivos Excel (*.xls;*.xlsx)|*.xls;*.xlsx|Todos los archivos (*.*)|*.*"
            .Multiselect = False

            If .ShowDialog() = DialogResult.OK Then
                Dim path As String = .FileName
                Me.Cursor = Cursors.WaitCursor

                Try
                    ' Leer cada hoja

                    Dim hojas = ObtenerHojasExcel(path)

                    Proyecto.Elementos.Zapatas.Tabla_JointReactions = LeerHojaExcel(path, "Joint Reactions")

                    ' Coordenadas y ejes: opcionales, pero sin ellas no hay planta
                    ' ni clasificación automática de medianeras y esquineras.
                    Dim faltantes As New List(Of String)

                    Dim hJoints = ResolverNombreHoja(hojas, "Objects and Elements - Joints", "Joint Coordinates")
                    If Not String.IsNullOrEmpty(hJoints) Then
                        Proyecto.Elementos.Joints = DataTableToJoints(LeerHojaExcel(path, hJoints))
                    Else
                        faltantes.Add("• Coordenadas de nodos  (""Objects and Elements - Joints"" en E23, ""Joint Coordinates"" en E17)")
                    End If

                    Dim hGrids = ResolverNombreHoja(hojas, "Grid Definitions - Grid Lines", "Grid Lines")
                    If Not String.IsNullOrEmpty(hGrids) Then
                        Proyecto.Elementos.Grids.GridLines = DataTableToGridLines(LeerHojaExcel(path, hGrids))
                    Else
                        faltantes.Add("• Ejes estructurales  (""Grid Definitions - Grid Lines"" o ""Grid Lines"")")
                    End If

                    If faltantes.Count = 0 Then
                        MsgBox("Importación completada correctamente.", MsgBoxStyle.Information)
                    Else
                        MessageBox.Show(
                            "Las reacciones se importaron bien, pero el archivo no trae:" & vbCrLf & vbCrLf &
                            String.Join(vbCrLf, faltantes) & vbCrLf & vbCrLf &
                            "Sin esas hojas no se puede dibujar la vista en planta ni clasificar " &
                            "automáticamente qué zapatas son medianeras o esquineras; todas quedarán " &
                            "como centrales y habrá que marcarlas a mano." & vbCrLf & vbCrLf &
                            "Para incluirlas, re-exporte desde ETABS agregándolas en Table Options. " &
                            "Vea el menú ""? Tablas ETABS"".",
                            "Importación incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If

                    Proyecto.Elementos.Zapatas.Reactions = DataTableToReactions(Proyecto.Elementos.Zapatas.Tabla_JointReactions)

                    ' 🔹 Extraer combinaciones únicas
                    Proyecto.Elementos.Zapatas.Lista_Combinaciones = Proyecto.Elementos.Zapatas.Reactions.Select(Function(r) r.LoadCase) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

                    MostrarSelectorCombinacionesZapatas()

                    MsgBox("Importación completada.", MsgBoxStyle.Information)

                Catch ex As Exception
                    MsgBox("Error al importar: " & ex.Message, MsgBoxStyle.Critical)
                Finally
                    Me.Cursor = Cursors.Arrow
                End Try
            End If
        End With

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim Lista_Elementos As New List(Of String)

        Lista_Elementos = Proyecto.Elementos.Zapatas.Reactions.Select(Function(r) r.JointLabel) _
                                                        .Where(Function(x) Not String.IsNullOrWhiteSpace(x)) _
                                                        .Distinct() _
                                                        .OrderBy(Function(x) x) _
                                                        .ToList()

        Tabla_Elementos.Rows.Clear()


        For Each Elemento_ In Lista_Elementos

            Dim Seccion As New cZapata

            Seccion.Nombre = Elemento_
            Seccion.Label_joint = Elemento_

            'Geometría Pedestal y Zapata
            Seccion.b = Convert.ToDouble(b_Pedestal.Text)
            Seccion.h = Convert.ToDouble(h_Pedestal.Text)
            Seccion.L_b = Convert.ToDouble(L1_Zapata.Text)
            Seccion.L_h = Convert.ToDouble(L2_Zapata.Text)
            Seccion.e = Convert.ToDouble(h_Zapata.Text)
            Seccion.rec = Convert.ToDouble(Recubrimiento.Text)
            Seccion.d = Seccion.e - Seccion.rec

            Dim r1 As New cRefuerzo With {
                            .Direccion = eDireccionRefuerzo.L2,
                            .Tipo = eTipoRefuerzo.Inferior,
                            .Diametro = Ref_L1.Text,
                            .Diametro_mm = 15.9,
                            .AreaBarra = Convert.ToDouble(As_L1.Text),
                            .Cantidad = Convert.ToDouble(NumRef_L1.Text),
                            .Espaciamiento = (Seccion.L_b - 2 * Seccion.rec) / .Cantidad
                        }

            Dim r2 As New cRefuerzo With {
                            .Direccion = eDireccionRefuerzo.L1,
                            .Tipo = eTipoRefuerzo.Inferior,
                            .Diametro = Ref_L2.Text,
                            .Diametro_mm = 15.9,
                            .AreaBarra = Convert.ToDouble(As_L2.Text),
                            .Cantidad = Convert.ToDouble(NumRef_L2.Text),
                            .Espaciamiento = (Seccion.L_h - 2 * Seccion.rec) / .Cantidad
                        }

            Seccion.Refuerzos.Add(r1)
            Seccion.Refuerzos.Add(r2)

            Seccion.Rho_L1 = r2.AsTotal / (Seccion.L_b * Seccion.d * 1000000)
            Seccion.Rho_L2 = r1.AsTotal / (Seccion.L_h * Seccion.d * 1000000)

            'Materiales
            Seccion.fc = Convert.ToDouble(T_fc.Text)
            Seccion.fy = Convert.ToDouble(T_fy.Text)

            'Capacidad Suelo
            Seccion.qAdm_Est = Convert.ToDouble(EadmEst.Text)
            Seccion.qAdm_Din = Convert.ToDouble(EadmDin.Text)
            Seccion.gammaSuelo = 18

            ' Df predeterminado en 1.5 m: cimentación superficial típica en
            ' Colombia. El usuario lo ajusta por fila en la tabla si es distinto.
            ' gammaConcreto en 24 kN/m³ por convención.
            Seccion.Df = 1.5
            Seccion.gammaConcreto = 24.0

            Seccion.FD_E = Convert.ToDouble(FD_E.Text)
            Seccion.FD_D = Convert.ToDouble(FD_D.Text)

            ' Coordenadas del nodo, si se importaron. Sin ellas la zapata no se
            ' puede ubicar en planta ni clasificar automáticamente.
            AsignarCoordenadas(Seccion)

            Proyecto.Elementos.Zapatas.Tipos.Add(Seccion)

            Dim idx As Integer = Tabla_Elementos.Rows.Add(Seccion.Label_joint,
                                    Seccion.Nombre,
                                    Seccion.b,
                                    Seccion.h,
                                    Seccion.e,
                                    Seccion.L_b,
                                    Seccion.L_h,
                                    r1.Diametro, r1.AreaBarra, r1.Cantidad,
                                    r2.Diametro, r2.AreaBarra, r2.Cantidad,
                                    Seccion.fc)
            If Tabla_Elementos.Columns.Contains(COL_DF) Then
                Tabla_Elementos.Rows(idx).Cells(COL_DF).Value = Seccion.Df
            End If
            If Tabla_Elementos.Columns.Contains(COL_GCONC) Then
                Tabla_Elementos.Rows(idx).Cells(COL_GCONC).Value = Seccion.gammaConcreto
            End If
            ' Grupo y Patrón se inicializan vacíos: agrupar es una decisión
            ' explícita del ingeniero, no algo que se infiera.
            If Tabla_Elementos.Columns.Contains(COL_GRUPO) Then
                Tabla_Elementos.Rows(idx).Cells(COL_GRUPO).Value = ""
            End If
            If Tabla_Elementos.Columns.Contains(COL_PATRON) Then
                Tabla_Elementos.Rows(idx).Cells(COL_PATRON).Value = False
            End If

        Next

        ' Con todas las zapatas creadas ya se puede ver el conjunto y proponer
        ' cuáles son medianeras y esquineras.
        ClasificarYAvisar()

    End Sub

    ''' <summary>
    ''' Copia a la zapata las coordenadas en planta de su nodo. El vínculo es el
    ''' JointLabel de "Joint Reactions" contra el ElementLabel de la hoja de
    ''' nodos; en ETABS son la misma etiqueta.
    ''' </summary>
    Private Sub AsignarCoordenadas(z As cZapata)

        Dim joints = Proyecto.Elementos.Joints
        If joints Is Nothing OrElse joints.Count = 0 Then Exit Sub

        Dim j = joints.FirstOrDefault(Function(x) String.Equals(Convert.ToString(x.ElementLabel).Trim(),
                                                                Convert.ToString(z.Label_joint).Trim(),
                                                                StringComparison.OrdinalIgnoreCase))
        If j Is Nothing Then Exit Sub

        z.CoordX = j.GlobalX
        z.CoordY = j.GlobalY
        z.TieneCoordenadas = True

    End Sub

    ''' <summary>
    ''' Propone el tipo de apoyo de cada zapata y avisa del resultado. Lo que el
    ''' ingeniero haya marcado a mano no se toca.
    ''' </summary>
    Private Sub ClasificarYAvisar()

        Dim zapatas = Proyecto.Elementos.Zapatas.Tipos
        If zapatas Is Nothing OrElse zapatas.Count = 0 Then Exit Sub

        Dim conCoord = zapatas.Where(Function(z) z.TieneCoordenadas).Count
        If conCoord = 0 Then
            Logger.Warning("Form_07_Pag_Zapatas.ClasificarYAvisar",
                           "Ninguna zapata tiene coordenadas: no se pudo clasificar el tipo de apoyo.")
            Exit Sub
        End If

        ZapataService.ClasificarApoyos(zapatas)
        ActualizarColumnaTipoApoyo()

        Dim nEsq = zapatas.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Esquinera).Count
        Dim nMed = zapatas.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Medianera).Count
        Dim nCen = zapatas.Where(Function(z) z.TipoApoyo = eTipoApoyoZapata.Central).Count

        Logger.Info("Form_07_Pag_Zapatas.ClasificarYAvisar",
                    $"Clasificadas {conCoord} de {zapatas.Count}: {nCen} centrales, {nMed} medianeras, {nEsq} esquineras.")

        If nEsq + nMed > 0 Then
            MessageBox.Show(
                $"Se clasificaron {conCoord} zapatas por su posición en planta:" & vbCrLf & vbCrLf &
                $"    Centrales    {nCen}" & vbCrLf &
                $"    Medianeras   {nMed}" & vbCrLf &
                $"    Esquineras   {nEsq}" & vbCrLf & vbCrLf &
                "El punzonamiento de las medianeras y esquineras se revisa con perímetro " &
                "crítico abierto, que es menor: su capacidad baja respecto a una central." & vbCrLf & vbCrLf &
                "Revise la columna ""Tipo de apoyo"" y corrija a mano las que la geometría no " &
                "acierte (voladizos, juntas, zapatas combinadas).",
                "Clasificación de apoyos", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        TablaResultados.Rows.Clear()
        Tabla_Reporte.Rows.Clear()

        ' Antes de calcular, propaga la geometría, materiales y refuerzo de la
        ' zapata patrón a todas las hijas de cada grupo. Esto se hace en la
        ' tabla para que el bucle de lectura que sigue no tenga que enterarse
        ' del concepto de grupo.
        PropagarPatronEnTabla()

        Dim combsEst = New HashSet(Of String)(Proyecto.Elementos.Zapatas.Lista_Combinaciones_Estaticas.Select(Function(c) NormalizarClaveCombo(c)))
        Dim combsDin = New HashSet(Of String)(Proyecto.Elementos.Zapatas.Lista_Combinaciones_Dinamicas.Select(Function(c) NormalizarClaveCombo(c)))
        Dim fontBold As New Font("Segoe UI", 9, FontStyle.Bold)
        TablaResultados.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
        Tabla_Reporte.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
        TablaResultados.SuspendLayout()
        Tabla_Reporte.SuspendLayout()

        For i = 0 To Proyecto.Elementos.Zapatas.Tipos.Count - 1

            Dim Label_Element As String : Label_Element = Tabla_Elementos.Rows(i).Cells(0).Value

            Dim Elemento = Proyecto.Elementos.Zapatas.Tipos.Find(Function(p) p.Label_joint = Label_Element)

            Elemento.Nombre = Tabla_Elementos.Rows(i).Cells(1).Value
            Elemento.b = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(2).Value)
            Elemento.h = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(3).Value)
            Elemento.e = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(4).Value)
            Elemento.d = Elemento.e - 0.075
            Elemento.L_b = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(5).Value)
            Elemento.L_h = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(6).Value)
            Elemento.fc = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(13).Value)

            ' Df y γ_concreto: si el usuario dejó la celda vacía o texto no
            ' numérico, se mantiene el valor que ya tenía la zapata (default o
            ' importado). Sin esto un espacio en blanco tumbaría el cálculo.
            If Tabla_Elementos.Columns.Contains(COL_DF) Then
                Dim vDf As Double
                If Double.TryParse(Convert.ToString(Tabla_Elementos.Rows(i).Cells(COL_DF).Value), vDf) Then
                    Elemento.Df = vDf
                End If
            End If
            If Tabla_Elementos.Columns.Contains(COL_GCONC) Then
                Dim vG As Double
                If Double.TryParse(Convert.ToString(Tabla_Elementos.Rows(i).Cells(COL_GCONC).Value), vG) AndAlso vG > 0 Then
                    Elemento.gammaConcreto = vG
                End If
            End If

            ' Grupo y Patrón: se leen de la tabla. La sincronización
            ' (copiar geometría del patrón a las hijas) se hace UNA sola vez
            ' fuera del loop, después de recorrer toda la tabla.
            If Tabla_Elementos.Columns.Contains(COL_GRUPO) Then
                Elemento.Grupo = If(Convert.ToString(Tabla_Elementos.Rows(i).Cells(COL_GRUPO).Value), "").Trim()
            End If
            If Tabla_Elementos.Columns.Contains(COL_PATRON) Then
                Elemento.EsPatron = CBool(If(Tabla_Elementos.Rows(i).Cells(COL_PATRON).Value, False))
            End If

            Elemento.Refuerzos.Clear()

            Dim Num_Barra_L2 As String = Tabla_Elementos.Rows(i).Cells(7).Value
            Dim As_L2 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(8).Value)
            Dim Cant_Ref_L2 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(9).Value)
            Dim Sep_Ref_L2 As Double = (Elemento.L_b - 2 * Elemento.rec) / Cant_Ref_L2

            Dim r1 As New cRefuerzo With {
                .Direccion = eDireccionRefuerzo.L2,
                .Tipo = eTipoRefuerzo.Inferior,
                .Diametro = Num_Barra_L2,
                .Diametro_mm = 15.9,
                .AreaBarra = As_L2,
                .Cantidad = Cant_Ref_L2,
                .Espaciamiento = Sep_Ref_L2
            }

            Dim Num_Barra_L1 As String = Tabla_Elementos.Rows(i).Cells(10).Value
            Dim As_L1 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(11).Value)
            Dim Cant_Ref_L1 As Double = Convert.ToDouble(Tabla_Elementos.Rows(i).Cells(12).Value)
            Dim Sep_Ref_L1 As Double = (Elemento.L_h - 2 * Elemento.rec) / Cant_Ref_L1

            Dim r2 As New cRefuerzo With {
                            .Direccion = eDireccionRefuerzo.L1,
                            .Tipo = eTipoRefuerzo.Inferior,
                            .Diametro = Num_Barra_L1,
                            .Diametro_mm = 15.9,
                            .AreaBarra = As_L1,
                            .Cantidad = Cant_Ref_L1,
                            .Espaciamiento = Sep_Ref_L1
                        }

            Elemento.Refuerzos.Add(r1)
            Elemento.Refuerzos.Add(r2)

            Dim combosValidos_Estatica = Proyecto.Elementos.Zapatas.Reactions.
    Where(Function(r) combsEst.Contains(r.LoadCase) _
                   AndAlso r.JointLabel = Label_Element).ToList()

            Dim combosValidos_Dinamica = Proyecto.Elementos.Zapatas.Reactions.
    Where(Function(r) combsDin.Contains(r.LoadCase) _
                   AndAlso r.JointLabel = Label_Element).ToList()


            Elemento.Lista_Combinaciones_Estaticas = combosValidos_Estatica
            Elemento.Lista_Combinaciones_Dinamicas = combosValidos_Dinamica

            Elemento.Resultados.Clear()

            Dim g_Esf_max As Double = Double.MinValue
            Dim g_adm As Double
            Dim F_Esf As Double = Double.MaxValue
            Dim Check As Boolean = True

            Dim g_Esf_max_Din As Double = Double.MinValue
            Dim g_adm_Din As Double
            Dim F_Esf_Din As Double = Double.MaxValue
            Dim Check_Din As Boolean = True

            Dim Vu_Punzonamiento_max As Double = Double.MinValue
            Dim Vc_Punzonamiento As Double = Double.MaxValue
            Dim check_Punzonamiento As Boolean
            Dim F_Punzonamiento As Double = Double.MaxValue

            Dim Vu_Cortante_max As Double = Double.MinValue
            Dim Vc_Cortante As Double
            Dim Check_Cortante As Boolean
            Dim F_Cortante As Double = Double.MaxValue


            Dim M_Max As Double = Double.MinValue
            Dim Rho_Req As Double
            Dim Rho_Col As Double
            Dim F_Momento As Double = Double.MaxValue
            Dim Check_Momento As Boolean = True

            Dim usarPeso As Boolean = Proyecto.Elementos.Zapatas.UsarPesoEstabilizante
            Dim limDinN As Double = Proyecto.Elementos.Zapatas.LimiteExcentricidadDinamicaN

            For Each comb In combosValidos_Estatica

                Dim P As Double = comb.FZ
                Dim Mx As Double = comb.MX
                Dim My As Double = comb.MY
                Dim Op_Comb As String = "EST"

                Dim res As ResultadoZapata = Funciones_Zapatas.EvaluarZapata(Elemento, P, Mx, My, Op_Comb, usarPeso, limDinN)

                Elemento.Resultados(comb.LoadCase) = res

                Dim Cumple_cortante As Boolean = res.CumpleCortante_1 And res.CumpleCortante_2 And res.CumpleCortante_3 And res.CumpleCortante_4
                Dim Cumple_Flexion As Boolean = res.Cumple_L1 And res.Cumple_L2

                TablaResultados.Rows.Add(Elemento.Nombre,
                                            Elemento.L_b,
                                            Elemento.L_h,
                                            comb.LoadCase, comb.FZ, comb.MX, comb.MY,
                                                Math.Round(res.g1, 2),
                                                Math.Round(res.g2, 2),
                                                Math.Round(res.g3, 2),
                                                Math.Round(res.g4, 2),
                                                Math.Round(res.g5, 2),
                                                Math.Round(res.g6, 2),
                                                Math.Round(res.g7, 2),
                                                Math.Round(res.g8, 2),
                                                If(res.CumpleCapacidad, "Cumple", "No cumple"),
                                                If(res.CumplePunzonamiento, "Cumple", "No cumple"),
                                                If(Cumple_cortante, "Cumple", "No cumple"),
                                                If(Cumple_Flexion, "Cumple", "No cumple"))

                Dim fila_res As DataGridViewRow = TablaResultados.Rows(TablaResultados.Rows.Count - 2)

                Dim columnasCumple_report() As Integer = {15, 16, 17, 18}

                For Each col As Integer In columnasCumple_report

                    Dim valor As String = fila_res.Cells(col).Value.ToString()

                    If valor = "Cumple" Then
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                    Else
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                    End If

                    fila_res.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                    fila_res.Cells(col).Style.Font = fontBold
                Next

                g_Esf_max = Math.Max(g_Esf_max, res.qMax)
                If (Elemento.qAdm_Est / g_Esf_max) < F_Esf Then
                    g_adm = Elemento.qAdm_Est
                    F_Esf = Elemento.qAdm_Est / g_Esf_max
                End If

                Dim vuAbs As Double = Math.Abs(res.Vu_p)
                If vuAbs > 0 AndAlso (res.Vc_p / vuAbs) < F_Punzonamiento Then
                    Vu_Punzonamiento_max = Math.Max(Vu_Punzonamiento_max, vuAbs)
                    Vc_Punzonamiento = Math.Min(Vc_Punzonamiento, res.Vc_p)
                    check_Punzonamiento = res.CumplePunzonamiento
                    F_Punzonamiento = res.Vc_p / vuAbs
                End If

                Dim Vu_C_max As Double = Math.Max(res.Vu1_C, res.Vu3_C)
                If (res.Vc2_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc2_C
                    Check_Cortante = res.CumpleCortante_1 And res.CumpleCortante_3
                    F_Cortante = res.Vc2_C / Vu_C_max
                End If

                Vu_C_max = Math.Max(res.Vu2_C, res.Vu4_C)
                If (res.Vc1_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc1_C
                    Check_Cortante = res.CumpleCortante_2 And res.CumpleCortante_4
                    F_Cortante = res.Vc1_C / Vu_C_max
                End If

                Dim F1_M As Double = Elemento.Rho_L1 / res.Rho_1
                Dim F2_M As Double = Elemento.Rho_L2 / res.Rho_2

                If F_Momento > Math.Min(F1_M, F2_M) Then
                    F_Momento = Math.Min(F1_M, F2_M)
                    If F1_M < F2_M Then
                        M_Max = res.Mu_1
                        Rho_Req = res.Rho_1
                        Rho_Col = Elemento.Rho_L1
                    Else
                        M_Max = res.Mu_2
                        Rho_Req = res.Rho_2
                        Rho_Col = Elemento.Rho_L2
                    End If
                End If

            Next

            For Each comb In combosValidos_Dinamica

                Dim P As Double = comb.FZ
                Dim Mx As Double = comb.MX
                Dim My As Double = comb.MY
                Dim Op_Comb As String = "DIN"

                Dim res As ResultadoZapata = Funciones_Zapatas.EvaluarZapata(Elemento, P, Mx, My, Op_Comb, usarPeso, limDinN)

                Elemento.Resultados(comb.LoadCase) = res

                Dim Cumple_cortante As Boolean = res.CumpleCortante_1 And res.CumpleCortante_2 And res.CumpleCortante_3 And res.CumpleCortante_4
                Dim Cumple_Flexion As Boolean = res.Cumple_L1 And res.Cumple_L2

                TablaResultados.Rows.Add(Elemento.Nombre,
                            Elemento.L_b,
                            Elemento.L_h,
                            comb.LoadCase, comb.FZ, comb.MX, comb.MY,
                                                Math.Round(res.g1, 2),
                                                Math.Round(res.g2, 2),
                                                Math.Round(res.g3, 2),
                                                Math.Round(res.g4, 2),
                                                Math.Round(res.g5, 2),
                                                Math.Round(res.g6, 2),
                                                Math.Round(res.g7, 2),
                                                Math.Round(res.g8, 2),
                                                If(res.CumpleCapacidad, "Cumple", "No cumple"),
                                                If(res.CumplePunzonamiento, "Cumple", "No cumple"),
                                                If(Cumple_cortante, "Cumple", "No cumple"),
                                                If(Cumple_Flexion, "Cumple", "No cumple"))

                Dim fila_res As DataGridViewRow = TablaResultados.Rows(TablaResultados.Rows.Count - 2)

                Dim columnasCumple_report() As Integer = {15, 16, 17, 18}

                For Each col As Integer In columnasCumple_report

                    Dim valor As String = fila_res.Cells(col).Value.ToString()

                    If valor = "Cumple" Then
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                    Else
                        fila_res.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                        fila_res.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                    End If

                    fila_res.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                    fila_res.Cells(col).Style.Font = fontBold
                Next

                g_Esf_max_Din = Math.Max(g_Esf_max_Din, res.qMax)
                If (Elemento.qAdm_Din / g_Esf_max_Din) < F_Esf_Din Then
                    g_adm_Din = Elemento.qAdm_Din
                    F_Esf_Din = Elemento.qAdm_Din / g_Esf_max_Din
                End If

                Dim vuAbs_D As Double = Math.Abs(res.Vu_p)
                If vuAbs_D > 0 AndAlso (res.Vc_p / vuAbs_D) < F_Punzonamiento Then
                    Vu_Punzonamiento_max = Math.Max(Vu_Punzonamiento_max, vuAbs_D)
                    Vc_Punzonamiento = Math.Min(Vc_Punzonamiento, res.Vc_p)
                    check_Punzonamiento = res.CumplePunzonamiento
                    F_Punzonamiento = res.Vc_p / vuAbs_D
                End If

                Dim Vu_C_max As Double = Math.Max(res.Vu1_C, res.Vu3_C)
                If (res.Vc2_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc2_C
                    Check_Cortante = res.CumpleCortante_1 And res.CumpleCortante_3
                    F_Cortante = res.Vc2_C / Vu_C_max
                End If

                Vu_C_max = Math.Max(res.Vu2_C, res.Vu4_C)
                If (res.Vc1_C / Vu_C_max) < F_Cortante Then
                    Vu_Cortante_max = Vu_C_max
                    Vc_Cortante = res.Vc1_C
                    Check_Cortante = res.CumpleCortante_2 And res.CumpleCortante_4
                    F_Cortante = res.Vc1_C / Vu_C_max
                End If

                Dim F1_M As Double = Elemento.Rho_L1 / res.Rho_1
                Dim F2_M As Double = Elemento.Rho_L2 / res.Rho_2

                If F_Momento > Math.Min(F1_M, F2_M) Then
                    F_Momento = Math.Min(F1_M, F2_M)
                    If F1_M < F2_M Then
                        M_Max = res.Mu_1
                        Rho_Req = res.Rho_1
                        Rho_Col = Elemento.Rho_L1
                    Else
                        M_Max = res.Mu_2
                        Rho_Req = res.Rho_2
                        Rho_Col = Elemento.Rho_L2
                    End If
                End If

            Next

            Tabla_Reporte.Rows.Add(Elemento.Nombre,
            Elemento.L_b,
            Elemento.L_h,
            Elemento.e,
            Math.Round(g_Esf_max, 2),
            If((g_Esf_max * 0.9 < g_adm), "Cumple", "No cumple"),
            Math.Round(g_Esf_max_Din, 2),
            If((g_Esf_max_Din * 0.9 < g_adm_Din), "Cumple", "No cumple"),
            Math.Round(Vu_Punzonamiento_max, 2),
            Math.Round(Vc_Punzonamiento, 2),
            If(check_Punzonamiento, "Cumple", "No cumple"),
            Math.Round(Vc_Cortante, 2),
            Math.Round(Vu_Cortante_max, 2),
            If(Check_Cortante, "Cumple", "No cumple"),
            Math.Round(Rho_Req, 5),
            Math.Round(Rho_Col, 5),
            If((F_Momento > 0.9), "Cumple", "No cumple"))

            Dim fila As DataGridViewRow = Tabla_Reporte.Rows(Tabla_Reporte.Rows.Count - 2)

            Dim columnasCumple() As Integer = {5, 7, 10, 13, 16}

            For Each col As Integer In columnasCumple

                Dim valor As String = fila.Cells(col).Value.ToString()

                If valor = "Cumple" Then
                    fila.Cells(col).Style.BackColor = Color.FromArgb(198, 239, 206)
                    fila.Cells(col).Style.ForeColor = Color.FromArgb(0, 97, 0)
                Else
                    fila.Cells(col).Style.BackColor = Color.FromArgb(255, 199, 206)
                    fila.Cells(col).Style.ForeColor = Color.FromArgb(156, 0, 6)
                End If

                fila.Cells(col).Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                fila.Cells(col).Style.Font = fontBold
            Next

        Next

        TablaResultados.ResumeLayout(True)
        Tabla_Reporte.ResumeLayout(True)
        fontBold.Dispose()


    End Sub

    ' ── Carga Tabla_Elementos desde los datos ya guardados en el proyecto ──────
    Public Sub PopularTablaElementos()
        If Proyecto.Elementos.Zapatas.Tipos.Count = 0 Then Return
        Tabla_Elementos.Rows.Clear()
        For Each z In Proyecto.Elementos.Zapatas.Tipos
            Dim r1 = z.Refuerzos.FirstOrDefault(Function(r) r.Direccion = eDireccionRefuerzo.L2)
            Dim r2 = z.Refuerzos.FirstOrDefault(Function(r) r.Direccion = eDireccionRefuerzo.L1)
            Dim idx As Integer = Tabla_Elementos.Rows.Add(
                z.Label_joint, z.Nombre,
                z.b, z.h, z.e, z.L_b, z.L_h,
                If(r1 IsNot Nothing, r1.Diametro, ""),
                If(r1 IsNot Nothing, r1.AreaBarra, 0.0),
                If(r1 IsNot Nothing, r1.Cantidad, 0.0),
                If(r2 IsNot Nothing, r2.Diametro, ""),
                If(r2 IsNot Nothing, r2.AreaBarra, 0.0),
                If(r2 IsNot Nothing, r2.Cantidad, 0.0),
                z.fc)
            If Tabla_Elementos.Columns.Contains(COL_DF) Then
                Tabla_Elementos.Rows(idx).Cells(COL_DF).Value = z.Df
            End If
            If Tabla_Elementos.Columns.Contains(COL_GCONC) Then
                Tabla_Elementos.Rows(idx).Cells(COL_GCONC).Value = z.gammaConcreto
            End If
            If Tabla_Elementos.Columns.Contains(COL_GRUPO) Then
                Tabla_Elementos.Rows(idx).Cells(COL_GRUPO).Value = If(z.Grupo, "")
            End If
            If Tabla_Elementos.Columns.Contains(COL_PATRON) Then
                Tabla_Elementos.Rows(idx).Cells(COL_PATRON).Value = z.EsPatron
            End If
        Next
    End Sub

    ''' <summary>
    ''' Llamado por la página principal al abrir un proyecto.
    ''' Re-sincroniza la referencia y vuelve a mostrar los resultados calculados.
    ''' </summary>
    Public Sub RefrescarDesdeProyecto()
        Proyecto = Form_00_PaginaPrincipal.proyecto
        RefrescarUIPesoEstabilizante()
        If Proyecto.Elementos.Zapatas.Tipos.Count = 0 Then Return
        PopularTablaElementos()
        If Proyecto.Elementos.Zapatas.Reactions.Count > 0 Then
            Button1_Click(Nothing, EventArgs.Empty)
        End If
    End Sub

    Private Sub Open_Pilas_Click(sender As Object, e As EventArgs) Handles Open_Pilas.Click
        Dim dlg As New OpenFileDialog With {.Filter = "Archivo|*.esm", .Title = "Abrir Archivo"}
        If dlg.ShowDialog() <> DialogResult.OK Then Return
        Try
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto)(dlg.FileName)
        Catch
            Try
                Dim elementos = Funciones_Programa.DeSerializar(Of cElementos)(dlg.FileName)
                Proyecto = New Proyecto()
                Proyecto.Elementos = elementos
            Catch ex As Exception
                MessageBox.Show("No se pudo abrir el archivo." & vbCrLf & ex.Message,
                                "Error al abrir", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try
        End Try
        Proyecto.Ruta = dlg.FileName
        Form_00_PaginaPrincipal.proyecto = Proyecto
        Form_00_PaginaPrincipal.SincronizarModulos()
        RefrescarDesdeProyecto()
    End Sub

    Private Sub Save_Pilas_Click(sender As Object, e As EventArgs) Handles Save_Pilas.Click
        If String.IsNullOrEmpty(Proyecto.Ruta) Then
            SaveAs_Pilas_Click(Nothing, EventArgs.Empty)
            Return
        End If
        Try
            Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            _hayCambiosZapatas = False
            MessageBox.Show("El archivo se guardó correctamente.", "Guardar", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveAs_Pilas_Click(sender As Object, e As EventArgs) Handles SaveAs_Pilas.Click
        Dim dlg As New SaveFileDialog With {
            .Filter = "Archivo|*.esm",
            .Title = "Guardar Archivo",
            .FileName = "RevisiónZapatas_Proyecto-" & Proyecto.Info.Nombre
        }
        If dlg.ShowDialog() <> DialogResult.OK Then Return
        Try
            Proyecto.Ruta = dlg.FileName
            Form_00_PaginaPrincipal.proyecto = Proyecto
            Funciones_Programa.Serializar(dlg.FileName, Proyecto)
            _hayCambiosZapatas = False
            MessageBox.Show("El archivo se guardó correctamente.", "Guardar Como", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error al guardar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub New_Pilas_Click(sender As Object, e As EventArgs) Handles New_Pilas.Click
        Dim result = MessageBox.Show("¿Desea crear un nuevo proyecto? Se perderán los cambios no guardados.",
                                     "Nuevo Proyecto", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If result <> DialogResult.Yes Then Return
        Proyecto = New Proyecto()
        Form_00_PaginaPrincipal.proyecto = Proyecto
        Tabla_Elementos.Rows.Clear()
        TablaResultados.Rows.Clear()
        Tabla_Reporte.Rows.Clear()
    End Sub

    Private Sub ReporteZapatas_MenuItem_Click(sender As Object, e As EventArgs) Handles ReporteZapatas_MenuItem.Click
        If Proyecto.Elementos.Zapatas.Tipos.Count = 0 Then
            MessageBox.Show("Ejecute primero la revisión de zapatas.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Form_Reporte_Zapatas.Mostrar(Proyecto)
    End Sub

    Private Sub Exportar_Excel_Click(sender As Object, e As EventArgs) Handles Exportar_Excel.Click
        If Proyecto.Elementos.Zapatas.Tipos.Count = 0 Then
            MessageBox.Show("Ejecute primero la revisión de zapatas.", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Form_Reporte_Zapatas.Mostrar(Proyecto)
    End Sub

    Private Sub Ref_L1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_L1.SelectedIndexChanged
        Try
            If Ref_L1.Text <> "Usuario" Then
                As_L1.Text = AreaRefuerzo(Ref_L1.Text)
            Else
                As_L1.Text = 199
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Ref_L2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Ref_L2.SelectedIndexChanged
        Try
            If Ref_L2.Text <> "Usuario" Then
                As_L2.Text = AreaRefuerzo(Ref_L2.Text)
            Else
                As_L2.Text = 199
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub ActualizarDemandasToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ActualizarDemandasToolStripMenuItem.Click
        If Proyecto.Elementos.Zapatas.Lista_Combinaciones.Count = 0 Then
            MsgBox("No hay combinaciones cargadas. Importe las demandas primero.", MsgBoxStyle.Exclamation)
            Return
        End If
        MostrarSelectorCombinacionesZapatas()
    End Sub

    Private Sub MostrarSelectorCombinacionesZapatas()
        Dim combos = Proyecto.Elementos.Zapatas.Lista_Combinaciones

        Dim fEst As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Zapatas.Lista_Combinaciones_Estaticas.Contains(c) Then fEst.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Zapatas.Lista_Combinaciones_Estaticas
            fEst.Lista_Cargas_Design.Items.Add(c)
        Next
        fEst.OpcionLlamado = "Zapatas" : fEst.Evaluacion = "Estaticas"
        fEst.GroupBox2.Text = "Combinaciones Análisis Estático"
        fEst.ShowDialog()

        Dim fDin As New Form_Opciones_Combinaciones()
        For Each c As String In combos
            If Not Proyecto.Elementos.Zapatas.Lista_Combinaciones_Dinamicas.Contains(c) Then fDin.Lista_Combinaciones.Items.Add(c)
        Next
        For Each c As String In Proyecto.Elementos.Zapatas.Lista_Combinaciones_Dinamicas
            fDin.Lista_Cargas_Design.Items.Add(c)
        Next
        fDin.OpcionLlamado = "Zapatas" : fDin.Evaluacion = "Dinamico"
        fDin.GroupBox2.Text = "Combinaciones Análisis Dinámico"
        fDin.ShowDialog()
    End Sub

    Private Sub Form_07_Pag_Zapatas_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not _hayCambiosZapatas Then Return
        Dim r = MessageBox.Show("Hay cambios sin guardar. ¿Guardar antes de cerrar?",
                                "Cerrar", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)
        If r = DialogResult.Yes Then Save_Pilas_Click(sender, e)
        If r = DialogResult.Cancel Then e.Cancel = True
    End Sub

End Class