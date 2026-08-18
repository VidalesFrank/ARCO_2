<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form_11_Nervios
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        ' ── Declaraciones ─────────────────────────────────────────────────────
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.Archivo_Zapatas = New System.Windows.Forms.ToolStripMenuItem()
        Me.Save_Pilas = New System.Windows.Forms.ToolStripMenuItem()
        Me.Importar_Zapatas = New System.Windows.Forms.ToolStripMenuItem()
        Me.ImportarDemandasToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.OpcionesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ActualizarDemandasToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.DefinirEjesManualmenteToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Exportar_Zapatas = New System.Windows.Forms.ToolStripMenuItem()
        Me.Exportar_Excel = New System.Windows.Forms.ToolStripMenuItem()

        Me.Panel3 = New System.Windows.Forms.Panel()
        Me.GbFiltro = New System.Windows.Forms.GroupBox()
        Me.Lista_Pisos = New System.Windows.Forms.ComboBox()
        Me.GbNervio = New System.Windows.Forms.GroupBox()
        Me.Lista_Nervios = New System.Windows.Forms.ComboBox()
        Me.LblNombreNervio = New System.Windows.Forms.Label()
        Me.Nombre_Nervio = New System.Windows.Forms.TextBox()
        Me.BtnRenombrar = New System.Windows.Forms.Button()
        Me.BtnSeparar = New System.Windows.Forms.Button()
        Me.GbLosa = New System.Windows.Forms.GroupBox()
        Me.LblTf = New System.Windows.Forms.Label()
        Me.NudTf = New System.Windows.Forms.NumericUpDown()
        Me.GbPlanta = New System.Windows.Forms.GroupBox()
        Me.ChkMostrarEtiquetas = New System.Windows.Forms.CheckBox()
        Me.ChkMostrarApoyos = New System.Windows.Forms.CheckBox()
        Me.BtnVerPlantaAmpliada = New System.Windows.Forms.Button()

        Me.Panel_Bottom = New System.Windows.Forms.Panel()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()

        Me.Panel_Main = New System.Windows.Forms.Panel()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.PicPlanta = New System.Windows.Forms.PictureBox()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.SplitRef = New System.Windows.Forms.SplitContainer()
        Me.PanelRefSup = New System.Windows.Forms.Panel()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.Ref_Superior = New System.Windows.Forms.DataGridView()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.Ref_Inferior = New System.Windows.Forms.DataGridView()
        Me.GroupBox8 = New System.Windows.Forms.GroupBox()
        Me.Ref_Cortante = New System.Windows.Forms.DataGridView()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.Tabla_Demandas = New System.Windows.Forms.DataGridView()
        Me.TabPage3 = New System.Windows.Forms.TabPage()
        Me.SplitRes = New System.Windows.Forms.SplitContainer()
        Me.GroupBox7 = New System.Windows.Forms.GroupBox()
        Me.Tabla_Resultados_Flexion = New System.Windows.Forms.DataGridView()
        Me.GroupBox_Cortante = New System.Windows.Forms.GroupBox()
        Me.Tabla_Resultados_Cortante = New System.Windows.Forms.DataGridView()
        Me.TabPage4 = New System.Windows.Forms.TabPage()
        Me.SplitDiag = New System.Windows.Forms.SplitContainer()
        Me.GroupBoxMomento = New System.Windows.Forms.GroupBox()
        Me.PanelDiagCtrl = New System.Windows.Forms.Panel()
        Me.ChkMostrarCapacidad = New System.Windows.Forms.CheckBox()
        Me.Diagrama_Momento = New System.Windows.Forms.PictureBox()
        Me.GroupBoxCortanteDiag = New System.Windows.Forms.GroupBox()
        Me.Diagrama_Cortante = New System.Windows.Forms.PictureBox()
        Me.TabPage5 = New System.Windows.Forms.TabPage()
        Me.SplitNervios = New System.Windows.Forms.SplitContainer()
        Me.GbTablaNervios = New System.Windows.Forms.GroupBox()
        Me.Tabla_Nervios = New System.Windows.Forms.DataGridView()
        Me.GbTablaFrames = New System.Windows.Forms.GroupBox()
        Me.Tabla_Frames_Nervio = New System.Windows.Forms.DataGridView()

        Me.MenuStrip1.SuspendLayout()
        Me.Panel3.SuspendLayout()
        Me.GbFiltro.SuspendLayout()
        Me.GbNervio.SuspendLayout()
        Me.GbLosa.SuspendLayout()
        CType(Me.NudTf, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GbPlanta.SuspendLayout()
        Me.Panel_Bottom.SuspendLayout()
        Me.Panel_Main.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        CType(Me.PicPlanta, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        CType(Me.SplitRef, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitRef.Panel1.SuspendLayout()
        Me.SplitRef.Panel2.SuspendLayout()
        Me.SplitRef.SuspendLayout()
        Me.PanelRefSup.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        CType(Me.Ref_Superior, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox6.SuspendLayout()
        CType(Me.Ref_Inferior, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox8.SuspendLayout()
        CType(Me.Ref_Cortante, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage2.SuspendLayout()
        CType(Me.Tabla_Demandas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage3.SuspendLayout()
        CType(Me.SplitRes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitRes.Panel1.SuspendLayout()
        Me.SplitRes.Panel2.SuspendLayout()
        Me.SplitRes.SuspendLayout()
        Me.GroupBox7.SuspendLayout()
        CType(Me.Tabla_Resultados_Flexion, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox_Cortante.SuspendLayout()
        CType(Me.Tabla_Resultados_Cortante, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage4.SuspendLayout()
        CType(Me.SplitDiag, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitDiag.Panel1.SuspendLayout()
        Me.SplitDiag.Panel2.SuspendLayout()
        Me.SplitDiag.SuspendLayout()
        Me.GroupBoxMomento.SuspendLayout()
        Me.PanelDiagCtrl.SuspendLayout()
        CType(Me.Diagrama_Momento, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBoxCortanteDiag.SuspendLayout()
        CType(Me.Diagrama_Cortante, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage5.SuspendLayout()
        CType(Me.SplitNervios, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitNervios.Panel1.SuspendLayout()
        Me.SplitNervios.Panel2.SuspendLayout()
        Me.SplitNervios.SuspendLayout()
        Me.GbTablaNervios.SuspendLayout()
        CType(Me.Tabla_Nervios, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GbTablaFrames.SuspendLayout()
        CType(Me.Tabla_Frames_Nervio, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()

        ' ── MenuStrip ─────────────────────────────────────────────────────────
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {
            Me.Archivo_Zapatas, Me.Importar_Zapatas, Me.OpcionesToolStripMenuItem, Me.Exportar_Zapatas})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(1200, 24)

        Me.Archivo_Zapatas.Name = "Archivo_Zapatas"
        Me.Archivo_Zapatas.Text = "Archivo"
        Me.Save_Pilas.Name = "Save_Pilas"
        Me.Save_Pilas.Text = "Guardar"
        Me.Save_Pilas.ShortcutKeys = System.Windows.Forms.Keys.Control Or System.Windows.Forms.Keys.S
        Me.Archivo_Zapatas.DropDownItems.Add(Me.Save_Pilas)

        Me.Importar_Zapatas.Name = "Importar_Zapatas"
        Me.Importar_Zapatas.Text = "Importar"
        Me.ImportarDemandasToolStripMenuItem.Name = "ImportarDemandasToolStripMenuItem"
        Me.ImportarDemandasToolStripMenuItem.Text = "Importar demandas ETABS..."
        Me.Importar_Zapatas.DropDownItems.Add(Me.ImportarDemandasToolStripMenuItem)

        Me.OpcionesToolStripMenuItem.Name = "OpcionesToolStripMenuItem"
        Me.OpcionesToolStripMenuItem.Text = "Opciones"
        Me.ActualizarDemandasToolStripMenuItem.Name = "ActualizarDemandasToolStripMenuItem"
        Me.ActualizarDemandasToolStripMenuItem.Text = "Combinaciones de diseño..."
        Me.OpcionesToolStripMenuItem.DropDownItems.Add(Me.ActualizarDemandasToolStripMenuItem)
        Me.DefinirEjesManualmenteToolStripMenuItem.Name = "DefinirEjesManualmenteToolStripMenuItem"
        Me.DefinirEjesManualmenteToolStripMenuItem.Text = "Definir ejes manualmente..."
        Me.OpcionesToolStripMenuItem.DropDownItems.Add(Me.DefinirEjesManualmenteToolStripMenuItem)

        Me.Exportar_Zapatas.Name = "Exportar_Zapatas"
        Me.Exportar_Zapatas.Text = "Exportar"
        Me.Exportar_Excel.Name = "Exportar_Excel"
        Me.Exportar_Excel.Text = "Exportar Excel..."
        Me.Exportar_Zapatas.DropDownItems.Add(Me.Exportar_Excel)

        ' ── Panel izquierdo ───────────────────────────────────────────────────
        Me.Panel3.Dock = System.Windows.Forms.DockStyle.Left
        Me.Panel3.Width = 270
        Me.Panel3.Padding = New System.Windows.Forms.Padding(4)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.BackColor = System.Drawing.Color.FromArgb(240, 240, 240)

        ' GroupBox Filtro piso
        Me.GbFiltro.Text = "Piso"
        Me.GbFiltro.Location = New System.Drawing.Point(4, 4)
        Me.GbFiltro.Size = New System.Drawing.Size(258, 56)
        Me.GbFiltro.Name = "GbFiltro"
        Me.GbFiltro.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        Me.Lista_Pisos.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Lista_Pisos.Location = New System.Drawing.Point(8, 22)
        Me.Lista_Pisos.Size = New System.Drawing.Size(242, 21)
        Me.Lista_Pisos.Name = "Lista_Pisos"
        Me.GbFiltro.Controls.Add(Me.Lista_Pisos)

        ' GroupBox Nervio selección
        Me.GbNervio.Text = "Nervio"
        Me.GbNervio.Location = New System.Drawing.Point(4, 64)
        Me.GbNervio.Size = New System.Drawing.Size(258, 152)
        Me.GbNervio.Name = "GbNervio"
        Me.GbNervio.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)

        Me.Lista_Nervios.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Lista_Nervios.Location = New System.Drawing.Point(8, 20)
        Me.Lista_Nervios.Size = New System.Drawing.Size(242, 21)
        Me.Lista_Nervios.Name = "Lista_Nervios"

        Me.LblNombreNervio.AutoSize = True
        Me.LblNombreNervio.Location = New System.Drawing.Point(8, 52)
        Me.LblNombreNervio.Text = "Nombre:"

        Me.Nombre_Nervio.Location = New System.Drawing.Point(8, 68)
        Me.Nombre_Nervio.Size = New System.Drawing.Size(242, 21)
        Me.Nombre_Nervio.Name = "Nombre_Nervio"
        Me.Nombre_Nervio.ReadOnly = True
        Me.Nombre_Nervio.BackColor = System.Drawing.Color.WhiteSmoke

        Me.BtnRenombrar.Location = New System.Drawing.Point(8, 98)
        Me.BtnRenombrar.Size = New System.Drawing.Size(116, 26)
        Me.BtnRenombrar.Text = "Renombrar"
        Me.BtnRenombrar.Name = "BtnRenombrar"
        Me.BtnRenombrar.BackColor = System.Drawing.Color.FromArgb(224, 224, 224)
        Me.BtnRenombrar.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        Me.BtnRenombrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.BtnRenombrar.FlatAppearance.BorderSize = 0

        Me.BtnSeparar.Location = New System.Drawing.Point(132, 98)
        Me.BtnSeparar.Size = New System.Drawing.Size(118, 26)
        Me.BtnSeparar.Text = "Separar tramo"
        Me.BtnSeparar.Name = "BtnSeparar"
        Me.BtnSeparar.BackColor = System.Drawing.Color.FromArgb(224, 224, 224)
        Me.BtnSeparar.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        Me.BtnSeparar.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.BtnSeparar.FlatAppearance.BorderSize = 0

        Me.GbNervio.Controls.Add(Me.Lista_Nervios)
        Me.GbNervio.Controls.Add(Me.LblNombreNervio)
        Me.GbNervio.Controls.Add(Me.Nombre_Nervio)
        Me.GbNervio.Controls.Add(Me.BtnRenombrar)
        Me.GbNervio.Controls.Add(Me.BtnSeparar)

        ' GroupBox Losa
        Me.GbLosa.Text = "Losa"
        Me.GbLosa.Location = New System.Drawing.Point(4, 220)
        Me.GbLosa.Size = New System.Drawing.Size(258, 58)
        Me.GbLosa.Name = "GbLosa"
        Me.GbLosa.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)

        Me.LblTf.AutoSize = True
        Me.LblTf.Location = New System.Drawing.Point(8, 26)
        Me.LblTf.Text = "Espesor tf (m):"

        Me.NudTf.Location = New System.Drawing.Point(130, 24)
        Me.NudTf.Size = New System.Drawing.Size(80, 22)
        Me.NudTf.DecimalPlaces = 3
        Me.NudTf.Increment = New Decimal(New Integer() {5, 0, 0, 196608})
        Me.NudTf.Minimum = New Decimal(New Integer() {5, 0, 0, 196608})
        Me.NudTf.Maximum = New Decimal(New Integer() {3, 0, 0, 65536})
        Me.NudTf.Value = New Decimal(New Integer() {50, 0, 0, 196608})
        Me.NudTf.Name = "NudTf"

        Me.GbLosa.Controls.Add(Me.LblTf)
        Me.GbLosa.Controls.Add(Me.NudTf)

        ' GroupBox opciones planta
        Me.GbPlanta.Text = "Vista planta"
        Me.GbPlanta.Location = New System.Drawing.Point(4, 282)
        Me.GbPlanta.Size = New System.Drawing.Size(258, 104)
        Me.GbPlanta.Name = "GbPlanta"
        Me.GbPlanta.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)

        Me.ChkMostrarEtiquetas.AutoSize = True
        Me.ChkMostrarEtiquetas.Location = New System.Drawing.Point(8, 22)
        Me.ChkMostrarEtiquetas.Text = "Mostrar etiquetas"
        Me.ChkMostrarEtiquetas.Checked = True
        Me.ChkMostrarEtiquetas.Name = "ChkMostrarEtiquetas"

        Me.ChkMostrarApoyos.AutoSize = True
        Me.ChkMostrarApoyos.Location = New System.Drawing.Point(8, 44)
        Me.ChkMostrarApoyos.Text = "Mostrar apoyos detectados"
        Me.ChkMostrarApoyos.Checked = True
        Me.ChkMostrarApoyos.Name = "ChkMostrarApoyos"

        Me.BtnVerPlantaAmpliada.Location = New System.Drawing.Point(8, 68)
        Me.BtnVerPlantaAmpliada.Size = New System.Drawing.Size(242, 28)
        Me.BtnVerPlantaAmpliada.Text = "🔍  Ver planta ampliada"
        Me.BtnVerPlantaAmpliada.Name = "BtnVerPlantaAmpliada"
        Me.BtnVerPlantaAmpliada.BackColor = System.Drawing.Color.FromArgb(224, 224, 224)
        Me.BtnVerPlantaAmpliada.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        Me.BtnVerPlantaAmpliada.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.BtnVerPlantaAmpliada.FlatAppearance.BorderSize = 0
        Me.BtnVerPlantaAmpliada.Font = New System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)

        Me.GbPlanta.Controls.Add(Me.ChkMostrarEtiquetas)
        Me.GbPlanta.Controls.Add(Me.ChkMostrarApoyos)
        Me.GbPlanta.Controls.Add(Me.BtnVerPlantaAmpliada)

        Me.Panel3.Controls.Add(Me.GbFiltro)
        Me.Panel3.Controls.Add(Me.GbNervio)
        Me.Panel3.Controls.Add(Me.GbLosa)
        Me.Panel3.Controls.Add(Me.GbPlanta)

        ' ── Panel inferior (Calcular) ──────────────────────────────────────────
        Me.Panel_Bottom.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.Panel_Bottom.Height = 38
        Me.Panel_Bottom.Padding = New System.Windows.Forms.Padding(6, 4, 6, 4)
        Me.Panel_Bottom.Name = "Panel_Bottom"
        Me.Panel_Bottom.BackColor = System.Drawing.Color.FromArgb(240, 240, 240)

        Me.Button1.Location = New System.Drawing.Point(6, 5)
        Me.Button1.Size = New System.Drawing.Size(150, 28)
        Me.Button1.Text = "▶  Calcular"
        Me.Button1.Name = "Button1"
        Me.Button1.BackColor = System.Drawing.Color.FromArgb(0, 150, 70)
        Me.Button1.ForeColor = System.Drawing.Color.White
        Me.Button1.Font = New System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        Me.Button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat

        Me.Label1.Location = New System.Drawing.Point(168, 10)
        Me.Label1.Size = New System.Drawing.Size(800, 18)
        Me.Label1.Name = "Label1"
        Me.Label1.ForeColor = System.Drawing.Color.DimGray
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 9)

        Me.Panel_Bottom.Controls.Add(Me.Button1)
        Me.Panel_Bottom.Controls.Add(Me.Label1)

        ' ── Panel principal (Fill) ─────────────────────────────────────────────
        Me.Panel_Main.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel_Main.Name = "Panel_Main"

        ' GroupBox1 — Planta (top, 250px)
        Me.GroupBox1.Text = "Planta del piso"
        Me.GroupBox1.Dock = System.Windows.Forms.DockStyle.Top
        Me.GroupBox1.Height = 250
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(2)
        Me.GroupBox1.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)

        Me.PicPlanta.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PicPlanta.BackColor = System.Drawing.Color.FromArgb(245, 248, 252)
        Me.PicPlanta.Name = "PicPlanta"
        Me.PicPlanta.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.GroupBox1.Controls.Add(Me.PicPlanta)

        ' TabControl1 (Fill bajo GroupBox1)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.TabPages.AddRange(New System.Windows.Forms.TabPage() {
            Me.TabPage5, Me.TabPage1, Me.TabPage2, Me.TabPage3, Me.TabPage4})

        ' ── TabPage5 "Nervios" ─────────────────────────────────────────────────
        Me.TabPage5.Text = "Nervios"
        Me.TabPage5.Name = "TabPage5"
        Me.TabPage5.Padding = New System.Windows.Forms.Padding(3)

        Me.SplitNervios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitNervios.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.SplitNervios.SplitterDistance = 260
        Me.SplitNervios.Name = "SplitNervios"
        Me.SplitNervios.SplitterWidth = 4

        Me.GbTablaNervios.Text = "Lista de nervios del piso"
        Me.GbTablaNervios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GbTablaNervios.Name = "GbTablaNervios"
        Me.GbTablaNervios.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GbTablaNervios.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Tabla_Nervios)
        Me.Tabla_Nervios.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.Tabla_Nervios.MultiSelect = False
        Me.Tabla_Nervios.ReadOnly = True
        Me.Tabla_Nervios.Name = "Tabla_Nervios"
        Me.GbTablaNervios.Controls.Add(Me.Tabla_Nervios)
        Me.SplitNervios.Panel1.Controls.Add(Me.GbTablaNervios)

        Me.GbTablaFrames.Text = "Composición del nervio seleccionado (frames ETABS)"
        Me.GbTablaFrames.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GbTablaFrames.Name = "GbTablaFrames"
        Me.GbTablaFrames.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GbTablaFrames.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Tabla_Frames_Nervio)
        Me.Tabla_Frames_Nervio.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.Tabla_Frames_Nervio.MultiSelect = False
        Me.Tabla_Frames_Nervio.ReadOnly = True
        Me.Tabla_Frames_Nervio.Name = "Tabla_Frames_Nervio"
        Me.GbTablaFrames.Controls.Add(Me.Tabla_Frames_Nervio)
        Me.SplitNervios.Panel2.Controls.Add(Me.GbTablaFrames)

        Me.TabPage5.Controls.Add(Me.SplitNervios)

        ' ── TabPage1 "Refuerzo" ────────────────────────────────────────────────
        Me.TabPage1.Text = "Refuerzo"
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)

        ' SplitRef — izq = Sup+Inf | der = Cortante
        Me.SplitRef.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitRef.Orientation = System.Windows.Forms.Orientation.Vertical
        Me.SplitRef.SplitterDistance = 340
        Me.SplitRef.Name = "SplitRef"
        Me.SplitRef.SplitterWidth = 4

        ' Panel izq SplitRef: Ref_Superior + Ref_Inferior
        Me.PanelRefSup.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PanelRefSup.Name = "PanelRefSup"

        Me.GroupBox5.Text = "Refuerzo Superior  (momentos negativos en apoyos)"
        Me.GroupBox5.Dock = System.Windows.Forms.DockStyle.Top
        Me.GroupBox5.Height = 168
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBox5.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Ref_Superior)
        Me.Ref_Superior.Name = "Ref_Superior"
        Me.GroupBox5.Controls.Add(Me.Ref_Superior)

        Me.GroupBox6.Text = "Refuerzo Inferior  (momentos positivos en vano)"
        Me.GroupBox6.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBox6.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Ref_Inferior)
        Me.Ref_Inferior.Name = "Ref_Inferior"
        Me.GroupBox6.Controls.Add(Me.Ref_Inferior)

        Me.PanelRefSup.Controls.Add(Me.GroupBox6)
        Me.PanelRefSup.Controls.Add(Me.GroupBox5)
        Me.SplitRef.Panel1.Controls.Add(Me.PanelRefSup)

        ' Panel der SplitRef: Ref_Cortante
        Me.GroupBox8.Text = "Refuerzo a Cortante"
        Me.GroupBox8.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBox8.Name = "GroupBox8"
        Me.GroupBox8.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBox8.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Ref_Cortante)
        Me.Ref_Cortante.Name = "Ref_Cortante"
        Me.GroupBox8.Controls.Add(Me.Ref_Cortante)
        Me.SplitRef.Panel2.Controls.Add(Me.GroupBox8)

        Me.TabPage1.Controls.Add(Me.SplitRef)

        ' ── TabPage2 "Demandas" ────────────────────────────────────────────────
        Me.TabPage2.Text = "Demandas"
        Me.TabPage2.Name = "TabPage2"
        ApplyDgvStyle(Me.Tabla_Demandas)
        Me.Tabla_Demandas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Tabla_Demandas.Name = "Tabla_Demandas"
        Me.Tabla_Demandas.ReadOnly = False  ' Es T / Eje Izq / Eje Der son editables (ver ConstruirTablaDemandas)
        Me.Tabla_Demandas.RowHeadersWidth = 150
        Me.TabPage2.Controls.Add(Me.Tabla_Demandas)

        ' ── TabPage3 "Resultados" ──────────────────────────────────────────────
        Me.TabPage3.Text = "Resultados"
        Me.TabPage3.Name = "TabPage3"

        Me.SplitRes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitRes.Orientation = System.Windows.Forms.Orientation.Vertical
        Me.SplitRes.SplitterDistance = 250
        Me.SplitRes.Name = "SplitRes"
        Me.SplitRes.SplitterWidth = 4

        Me.GroupBox7.Text = "Resultados Flexión"
        Me.GroupBox7.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBox7.Name = "GroupBox7"
        Me.GroupBox7.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBox7.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Tabla_Resultados_Flexion)
        Me.Tabla_Resultados_Flexion.ReadOnly = True
        Me.Tabla_Resultados_Flexion.Name = "Tabla_Resultados_Flexion"
        Me.GroupBox7.Controls.Add(Me.Tabla_Resultados_Flexion)
        Me.SplitRes.Panel1.Controls.Add(Me.GroupBox7)

        Me.GroupBox_Cortante.Text = "Resultados Cortante"
        Me.GroupBox_Cortante.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBox_Cortante.Name = "GroupBox_Cortante"
        Me.GroupBox_Cortante.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBox_Cortante.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        ApplyDgvStyle(Me.Tabla_Resultados_Cortante)
        Me.Tabla_Resultados_Cortante.ReadOnly = True
        Me.Tabla_Resultados_Cortante.Name = "Tabla_Resultados_Cortante"
        Me.GroupBox_Cortante.Controls.Add(Me.Tabla_Resultados_Cortante)
        Me.SplitRes.Panel2.Controls.Add(Me.GroupBox_Cortante)

        Me.TabPage3.Controls.Add(Me.SplitRes)

        ' ── TabPage4 "Diagramas" ───────────────────────────────────────────────
        Me.TabPage4.Text = "Diagramas"
        Me.TabPage4.Name = "TabPage4"

        Me.SplitDiag.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitDiag.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.SplitDiag.SplitterDistance = 280
        Me.SplitDiag.Name = "SplitDiag"
        Me.SplitDiag.SplitterWidth = 4

        Me.GroupBoxMomento.Text = "Diagrama de Momento (M3)"
        Me.GroupBoxMomento.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBoxMomento.Name = "GroupBoxMomento"
        Me.GroupBoxMomento.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBoxMomento.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)

        Me.PanelDiagCtrl.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelDiagCtrl.Height = 22
        Me.PanelDiagCtrl.Name = "PanelDiagCtrl"
        Me.PanelDiagCtrl.BackColor = System.Drawing.Color.Transparent

        Me.ChkMostrarCapacidad.AutoSize = True
        Me.ChkMostrarCapacidad.Checked = True
        Me.ChkMostrarCapacidad.CheckState = System.Windows.Forms.CheckState.Checked
        Me.ChkMostrarCapacidad.Text = "Mostrar φMn(x)"
        Me.ChkMostrarCapacidad.Name = "ChkMostrarCapacidad"
        Me.ChkMostrarCapacidad.Location = New System.Drawing.Point(6, 2)
        Me.PanelDiagCtrl.Controls.Add(Me.ChkMostrarCapacidad)

        Me.Diagrama_Momento.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Diagrama_Momento.BackColor = System.Drawing.Color.White
        Me.Diagrama_Momento.Name = "Diagrama_Momento"
        Me.Diagrama_Momento.TabStop = False
        Me.GroupBoxMomento.Controls.Add(Me.Diagrama_Momento)
        Me.GroupBoxMomento.Controls.Add(Me.PanelDiagCtrl)
        Me.SplitDiag.Panel1.Controls.Add(Me.GroupBoxMomento)

        Me.GroupBoxCortanteDiag.Text = "Diagrama de Cortante (V2)"
        Me.GroupBoxCortanteDiag.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBoxCortanteDiag.Name = "GroupBoxCortanteDiag"
        Me.GroupBoxCortanteDiag.Padding = New System.Windows.Forms.Padding(4, 16, 4, 4)
        Me.GroupBoxCortanteDiag.ForeColor = System.Drawing.Color.FromArgb(87, 87, 87)
        Me.Diagrama_Cortante.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Diagrama_Cortante.BackColor = System.Drawing.Color.White
        Me.Diagrama_Cortante.Name = "Diagrama_Cortante"
        Me.Diagrama_Cortante.TabStop = False
        Me.GroupBoxCortanteDiag.Controls.Add(Me.Diagrama_Cortante)
        Me.SplitDiag.Panel2.Controls.Add(Me.GroupBoxCortanteDiag)

        Me.TabPage4.Controls.Add(Me.SplitDiag)

        Me.Panel_Main.Controls.Add(Me.TabControl1)
        Me.Panel_Main.Controls.Add(Me.GroupBox1)

        ' ── Form ──────────────────────────────────────────────────────────────
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1300, 800)
        Me.Controls.Add(Me.Panel_Main)
        Me.Controls.Add(Me.Panel3)
        Me.Controls.Add(Me.Panel_Bottom)
        Me.Controls.Add(Me.MenuStrip1)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.MinimumSize = New System.Drawing.Size(1000, 700)
        Me.Name = "Form_11_Nervios"
        Me.Text = "Módulo 11 — Nervios / Losas Nervadas"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.Font = New System.Drawing.Font("Segoe UI", 9)
        Me.BackColor = System.Drawing.Color.FromArgb(240, 240, 240)

        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.Panel3.ResumeLayout(False)
        Me.GbFiltro.ResumeLayout(False)
        Me.GbNervio.ResumeLayout(False)
        Me.GbNervio.PerformLayout()
        Me.GbLosa.ResumeLayout(False)
        Me.GbLosa.PerformLayout()
        CType(Me.NudTf, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GbPlanta.ResumeLayout(False)
        Me.GbPlanta.PerformLayout()
        Me.Panel_Bottom.ResumeLayout(False)
        Me.Panel_Main.ResumeLayout(False)
        Me.GroupBox1.ResumeLayout(False)
        CType(Me.PicPlanta, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        CType(Me.SplitRef, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitRef.Panel1.ResumeLayout(False)
        Me.SplitRef.Panel2.ResumeLayout(False)
        Me.SplitRef.ResumeLayout(False)
        Me.PanelRefSup.ResumeLayout(False)
        Me.GroupBox5.ResumeLayout(False)
        CType(Me.Ref_Superior, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox6.ResumeLayout(False)
        CType(Me.Ref_Inferior, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox8.ResumeLayout(False)
        CType(Me.Ref_Cortante, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPage2.ResumeLayout(False)
        CType(Me.Tabla_Demandas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPage3.ResumeLayout(False)
        CType(Me.SplitRes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitRes.Panel1.ResumeLayout(False)
        Me.SplitRes.Panel2.ResumeLayout(False)
        Me.SplitRes.ResumeLayout(False)
        Me.GroupBox7.ResumeLayout(False)
        CType(Me.Tabla_Resultados_Flexion, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox_Cortante.ResumeLayout(False)
        CType(Me.Tabla_Resultados_Cortante, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPage4.ResumeLayout(False)
        CType(Me.SplitDiag, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitDiag.Panel1.ResumeLayout(False)
        Me.SplitDiag.Panel2.ResumeLayout(False)
        Me.SplitDiag.ResumeLayout(False)
        Me.GroupBoxMomento.ResumeLayout(False)
        Me.PanelDiagCtrl.ResumeLayout(False)
        Me.PanelDiagCtrl.PerformLayout()
        CType(Me.Diagrama_Momento, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBoxCortanteDiag.ResumeLayout(False)
        CType(Me.Diagrama_Cortante, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPage5.ResumeLayout(False)
        CType(Me.SplitNervios, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitNervios.Panel1.ResumeLayout(False)
        Me.SplitNervios.Panel2.ResumeLayout(False)
        Me.SplitNervios.ResumeLayout(False)
        Me.GbTablaNervios.ResumeLayout(False)
        CType(Me.Tabla_Nervios, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GbTablaFrames.ResumeLayout(False)
        CType(Me.Tabla_Frames_Nervio, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Shared Sub ApplyDgvStyle(dgv As System.Windows.Forms.DataGridView)
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.BorderStyle = System.Windows.Forms.BorderStyle.None
        dgv.Dock = System.Windows.Forms.DockStyle.Fill
        dgv.RowHeadersWidth = 80
        dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect
        dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(87, 87, 87)
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White
        dgv.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Bold)
        dgv.EnableHeadersVisualStyles = False
        dgv.RowHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(245, 245, 245)
        dgv.RowHeadersDefaultCellStyle.Font = New System.Drawing.Font("Segoe UI", 8)
        dgv.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(245, 248, 255)
        dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells
        dgv.GridColor = System.Drawing.Color.FromArgb(210, 210, 210)
        dgv.DefaultCellStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight
        dgv.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        dgv.BackgroundColor = System.Drawing.Color.White
    End Sub

    Friend WithEvents MenuStrip1 As System.Windows.Forms.MenuStrip
    Friend WithEvents Archivo_Zapatas As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Save_Pilas As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Importar_Zapatas As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ImportarDemandasToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents OpcionesToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents ActualizarDemandasToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents DefinirEjesManualmenteToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Exportar_Zapatas As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Exportar_Excel As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents GbFiltro As System.Windows.Forms.GroupBox
    Friend WithEvents Lista_Pisos As System.Windows.Forms.ComboBox
    Friend WithEvents GbNervio As System.Windows.Forms.GroupBox
    Friend WithEvents Lista_Nervios As System.Windows.Forms.ComboBox
    Friend WithEvents LblNombreNervio As System.Windows.Forms.Label
    Friend WithEvents Nombre_Nervio As System.Windows.Forms.TextBox
    Friend WithEvents BtnRenombrar As System.Windows.Forms.Button
    Friend WithEvents BtnSeparar As System.Windows.Forms.Button
    Friend WithEvents GbLosa As System.Windows.Forms.GroupBox
    Friend WithEvents LblTf As System.Windows.Forms.Label
    Friend WithEvents NudTf As System.Windows.Forms.NumericUpDown
    Friend WithEvents GbPlanta As System.Windows.Forms.GroupBox
    Friend WithEvents ChkMostrarEtiquetas As System.Windows.Forms.CheckBox
    Friend WithEvents ChkMostrarApoyos As System.Windows.Forms.CheckBox
    Friend WithEvents BtnVerPlantaAmpliada As System.Windows.Forms.Button
    Friend WithEvents Panel_Bottom As System.Windows.Forms.Panel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Panel_Main As System.Windows.Forms.Panel
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents PicPlanta As System.Windows.Forms.PictureBox
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents SplitRef As System.Windows.Forms.SplitContainer
    Friend WithEvents PanelRefSup As System.Windows.Forms.Panel
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents Ref_Superior As System.Windows.Forms.DataGridView
    Friend WithEvents GroupBox6 As System.Windows.Forms.GroupBox
    Friend WithEvents Ref_Inferior As System.Windows.Forms.DataGridView
    Friend WithEvents GroupBox8 As System.Windows.Forms.GroupBox
    Friend WithEvents Ref_Cortante As System.Windows.Forms.DataGridView
    Friend WithEvents TabPage2 As System.Windows.Forms.TabPage
    Friend WithEvents Tabla_Demandas As System.Windows.Forms.DataGridView
    Friend WithEvents TabPage3 As System.Windows.Forms.TabPage
    Friend WithEvents SplitRes As System.Windows.Forms.SplitContainer
    Friend WithEvents GroupBox7 As System.Windows.Forms.GroupBox
    Friend WithEvents Tabla_Resultados_Flexion As System.Windows.Forms.DataGridView
    Friend WithEvents GroupBox_Cortante As System.Windows.Forms.GroupBox
    Friend WithEvents Tabla_Resultados_Cortante As System.Windows.Forms.DataGridView
    Friend WithEvents TabPage4 As System.Windows.Forms.TabPage
    Friend WithEvents SplitDiag As System.Windows.Forms.SplitContainer
    Friend WithEvents GroupBoxMomento As System.Windows.Forms.GroupBox
    Friend WithEvents PanelDiagCtrl As System.Windows.Forms.Panel
    Friend WithEvents ChkMostrarCapacidad As System.Windows.Forms.CheckBox
    Friend WithEvents Diagrama_Momento As System.Windows.Forms.PictureBox
    Friend WithEvents GroupBoxCortanteDiag As System.Windows.Forms.GroupBox
    Friend WithEvents Diagrama_Cortante As System.Windows.Forms.PictureBox
    Friend WithEvents TabPage5 As System.Windows.Forms.TabPage
    Friend WithEvents SplitNervios As System.Windows.Forms.SplitContainer
    Friend WithEvents GbTablaNervios As System.Windows.Forms.GroupBox
    Friend WithEvents Tabla_Nervios As System.Windows.Forms.DataGridView
    Friend WithEvents GbTablaFrames As System.Windows.Forms.GroupBox
    Friend WithEvents Tabla_Frames_Nervio As System.Windows.Forms.DataGridView
End Class
