''' <summary>
''' Formulario de ayuda: muestra las tablas de Excel requeridas para importar desde ETABS
''' en cada módulo de la aplicación. Soporta ETABS E17 y E23.
''' Uso: Form_AyudaImportacion.MostrarModulo("Vigas")
''' </summary>
Public Class Form_AyudaImportacion
    Inherits Form

    ' ──────────────────────────────────────────────────────────────────────────
    ' Definición de tablas requeridas por módulo
    ' ──────────────────────────────────────────────────────────────────────────

    Private Structure InfoTabla
        Public Property Nombre As String
        Public Property Proposito As String
        Public Property NombreE17 As String
        Public Property NombreE23 As String
        Public Property Obligatoria As Boolean
        Public Sub New(n As String, prop As String, e17 As String, e23 As String, Optional oblig As Boolean = True)
            Nombre = n : Proposito = prop : NombreE17 = e17 : NombreE23 = e23 : Obligatoria = oblig
        End Sub
    End Structure

    Private Shared ReadOnly _tablasPorModulo As New Dictionary(Of String, List(Of InfoTabla)) From {
        {
            "Pilas", New List(Of InfoTabla) From {
                New InfoTabla("Joint Reactions", "Reacciones en nodos de cimentación", "Joint Reactions", "Joint Reactions")
            }
        },
        {
            "Columnas", New List(Of InfoTabla) From {
                New InfoTabla("Diseño columnas Frame", "Resultados de diseño ACI 318", "Concrete Column Summary - ACI 3", "Conc Col Sum - ACI 318-14"),
                New InfoTabla("Diseño columnas Pier", "Resultados de diseño muros/piers", "Shear Wall Pier Summary", "Pier Dgn Sum"),
                New InfoTabla("Secciones Frame", "Definición de secciones rectangulares", "Frame Sections", "Frame Sec Def - Conc Rect"),
                New InfoTabla("Propiedades Pier", "Dimensiones de secciones pier", "Pier Section Properties", "Pier Section Properties"),
                New InfoTabla("Fuerzas Frame", "Fuerzas internas en columnas", "Column Forces", "Element Forces - Columns"),
                New InfoTabla("Fuerzas Pier", "Fuerzas internas en piers", "Pier Forces", "Pier Forces")
            }
        },
        {
            "Muros", New List(Of InfoTabla) From {
                New InfoTabla("Diseño muros", "Resultados de diseño Pier/Muro", "Shear Wall Pier Summary", "Pier Dgn Sum"),
                New InfoTabla("Propiedades sección", "Geometría de la sección del muro", "Pier Section Properties", "Pier Section Properties"),
                New InfoTabla("Fuerzas Pier", "Fuerzas internas (P, V2, M3)", "Pier Forces", "Pier Forces")
            }
        },
        {
            "Vigas", New List(Of InfoTabla) From {
                New InfoTabla("Joints y Frames", "Geometría y conectividad de elementos", "Joint Coordinates / Connectivity - Frame", "Objects and Elements - Joints / Frames"),
                New InfoTabla("Asignación secciones", "Secciones asignadas a cada frame", "Frame Assignments - Sections", "Frame Assigns - Sect Prop"),
                New InfoTabla("Definición secciones", "Dimensiones b×h de secciones rect.", "Frame Sections", "Frame Sec Def - Conc Rect"),
                New InfoTabla("Materiales concreto", "f'c y propiedades del concreto", "Material Properties - Concrete", "Mat Prop - Concrete Data"),
                New InfoTabla("Fuerzas vigas", "M3 y V2 por combinación y estación", "Beam Forces", "Element Forces - Beams"),
                New InfoTabla("Grillas", "Definición de líneas de cuadrícula", "Grid Lines", "Grid Definitions - Grid Lines")
            }
        },
        {
            "Zapatas", New List(Of InfoTabla) From {
                New InfoTabla("Joint Reactions", "Reacciones en nodos de cimentación", "Joint Reactions", "Joint Reactions")
            }
        }
    }

    ' ──────────────────────────────────────────────────────────────────────────
    ' Método de entrada público
    ' ──────────────────────────────────────────────────────────────────────────

    Public Shared Sub MostrarModulo(modulo As String)
        Using frm As New Form_AyudaImportacion(modulo)
            frm.ShowDialog()
        End Using
    End Sub

    ' ──────────────────────────────────────────────────────────────────────────
    ' Construcción de UI
    ' ──────────────────────────────────────────────────────────────────────────

    Private _modulo As String

    Public Sub New(modulo As String)
        _modulo = modulo
        InitializeComponent()
        CargarContenido()
    End Sub

    Private _tabs As TabControl
    Private _dgvActivo As DataGridView
    Private _dgvTodos As DataGridView

    Private Sub InitializeComponent()
        Me.Text = "Tablas requeridas — " & _modulo & " (ETABS)"
        Me.Size = New Size(860, 560)
        Me.MinimumSize = New Size(700, 420)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.FromArgb(245, 245, 245)
        Me.Font = New Font("Segoe UI", 9)

        ' Panel superior — título
        Dim panelTop As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 52,
            .BackColor = Color.FromArgb(50, 75, 115)
        }
        Dim lblTitulo As New Label With {
            .Text = "  Tablas ETABS requeridas — Módulo: " & _modulo,
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI Semibold", 11, FontStyle.Bold),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        panelTop.Controls.Add(lblTitulo)

        ' Panel info — nota de versiones
        Dim panelInfo As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 36,
            .BackColor = Color.FromArgb(255, 243, 205)
        }
        Dim lblInfo As New Label With {
            .Text = "  La aplicación detecta automáticamente la versión de ETABS (E17 / E23) al importar.",
            .ForeColor = Color.FromArgb(102, 77, 3),
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Segoe UI", 8.5)
        }
        panelInfo.Controls.Add(lblInfo)

        ' TabControl
        _tabs = New TabControl With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 9)
        }

        ' Tab módulo actual
        Dim tabActivo As New TabPage("Módulo " & _modulo) With {
            .BackColor = Color.White
        }
        _dgvActivo = CrearDGV()
        tabActivo.Controls.Add(_dgvActivo)

        ' Tab todos los módulos
        Dim tabTodos As New TabPage("Todos los módulos") With {
            .BackColor = Color.White
        }
        _dgvTodos = CrearDGV(conColumnaModulo:=True)
        tabTodos.Controls.Add(_dgvTodos)

        _tabs.TabPages.Add(tabActivo)
        _tabs.TabPages.Add(tabTodos)

        ' Panel botones
        Dim panelBot As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 44,
            .BackColor = Color.FromArgb(240, 240, 240)
        }
        Dim btnCerrar As New Button With {
            .Text = "Cerrar",
            .Size = New Size(90, 28),
            .Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        }
        btnCerrar.Location = New Point(panelBot.Width - 106, 8)
        btnCerrar.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        AddHandler btnCerrar.Click, Sub(s, e) Me.Close()
        panelBot.Controls.Add(btnCerrar)

        Me.Controls.Add(_tabs)
        Me.Controls.Add(panelInfo)
        Me.Controls.Add(panelTop)
        Me.Controls.Add(panelBot)
    End Sub

    Private Function CrearDGV(Optional conColumnaModulo As Boolean = False) As DataGridView
        Dim dgv As New DataGridView With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            .Font = New Font("Segoe UI", 8.5)
        }
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 75, 115)
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9, FontStyle.Bold)
        dgv.EnableHeadersVisualStyles = False

        If conColumnaModulo Then
            dgv.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Módulo", .Name = "Modulo", .Width = 80})
        End If
        dgv.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Tabla / Propósito", .Name = "Nombre", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .MinimumWidth = 160})
        dgv.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "ETABS E17", .Name = "E17", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .MinimumWidth = 140})
        dgv.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "ETABS E23", .Name = "E23", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .MinimumWidth = 140})

        Dim colObl As New DataGridViewTextBoxColumn With {.HeaderText = "¿Requerida?", .Name = "Obl", .Width = 90}
        dgv.Columns.Add(colObl)

        AddHandler dgv.CellFormatting, AddressOf DGV_CellFormatting

        Return dgv
    End Function

    Private Sub DGV_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        Dim dgv = CType(sender, DataGridView)
        If e.RowIndex < 0 Then Return
        Dim oblCol = dgv.Columns("Obl")
        If oblCol Is Nothing Then Return
        Dim oblVal = dgv.Rows(e.RowIndex).Cells("Obl").Value?.ToString()
        If oblVal = "Sí" Then
            dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 235)
        End If
        ' Filas alternas en tab todos
        If e.RowIndex Mod 2 = 1 Then
            If dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.White Then
                dgv.Rows(e.RowIndex).DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248)
            End If
        End If
    End Sub

    Private Sub CargarContenido()
        ' Tab módulo activo
        If _tablasPorModulo.ContainsKey(_modulo) Then
            For Each t In _tablasPorModulo(_modulo)
                _dgvActivo.Rows.Add(t.Proposito, t.NombreE17, t.NombreE23, If(t.Obligatoria, "Sí", "Opcional"))
            Next
        End If

        ' Tab todos los módulos
        For Each kvp In _tablasPorModulo
            For Each t In kvp.Value
                _dgvTodos.Rows.Add(kvp.Key, t.Proposito, t.NombreE17, t.NombreE23, If(t.Obligatoria, "Sí", "Opcional"))
            Next
        Next
    End Sub

End Class
