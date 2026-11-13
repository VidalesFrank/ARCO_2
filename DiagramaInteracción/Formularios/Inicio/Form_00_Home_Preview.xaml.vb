Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Input

''' <summary>
''' Window WPF que ofrece una vista previa de la nueva pantalla de inicio
''' ARCO. Convive con <see cref="Form_00_PaginaPrincipal"/> (WinForms), que
''' sigue siendo la pantalla principal activa. Cada acción delega a los
''' handlers ya existentes en Form_00_PaginaPrincipal para preservar 100%
''' de la lógica (serialización .esm, validaciones, Pilas setup, etc.).
''' </summary>
Partial Public Class Form_00_Home_Preview
    Inherits Window

    Public Sub New()
        InitializeComponent()
        CargarListas()
        AddHandler Me.Loaded, AddressOf OnLoaded
    End Sub

    Private Sub CargarListas()
        ListaAccesoRapido.ItemsSource = ModuleCatalog.AccesoRapido()
        ListaSuper.ItemsSource  = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Super)
        ListaCimen.ItemsSource  = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Cimen)
        ListaEspec.ItemsSource  = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Espec)
        ListaTools.ItemsSource  = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Tools)

        SidebarSuper.ItemsSource = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Super)
        SidebarCimen.ItemsSource = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Cimen)
        SidebarEspec.ItemsSource = ModuleCatalog.PorCategoria(ModuleCatalog.Cat_Espec)
    End Sub

    Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
        ActualizarNombreProyecto()
    End Sub

    Private Sub ActualizarNombreProyecto()
        Try
            Dim nombre As String = Nothing
            If Form_00_PaginaPrincipal.proyecto IsNot Nothing AndAlso
               Form_00_PaginaPrincipal.proyecto.Info IsNot Nothing Then
                nombre = Form_00_PaginaPrincipal.proyecto.Info.Nombre
            End If
            LblSeccion.Text = If(Not String.IsNullOrWhiteSpace(nombre), "Inicio · " & nombre, "Inicio")
        Catch
            LblSeccion.Text = "Inicio"
        End Try
    End Sub

    ' ── Búsqueda ───────────────────────────────────────────────

    Private Sub TxtBuscar_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim q = TxtBuscar.Text
        If String.IsNullOrWhiteSpace(q) Then
            CargarListas()
            Return
        End If
        Dim resultado = ModuleCatalog.Buscar(q)
        ListaAccesoRapido.ItemsSource = Nothing
        ListaSuper.ItemsSource = resultado.Where(Function(m) m.Categoria = ModuleCatalog.Cat_Super).ToList()
        ListaCimen.ItemsSource = resultado.Where(Function(m) m.Categoria = ModuleCatalog.Cat_Cimen).ToList()
        ListaEspec.ItemsSource = resultado.Where(Function(m) m.Categoria = ModuleCatalog.Cat_Espec).ToList()
        ListaTools.ItemsSource = resultado.Where(Function(m) m.Categoria = ModuleCatalog.Cat_Tools).ToList()
    End Sub

    ' ── Acciones de la TopBar ──────────────────────────────────

    Private Sub BtnNuevo_Click(sender As Object, e As RoutedEventArgs)
        Try
            ' El NuevoProyectoToolStripMenuItem no tiene handler asignado en
            ' Form_00_PaginaPrincipal; creamos un proyecto limpio y sincronizamos.
            Form_00_PaginaPrincipal.proyecto = New Proyecto()
            Form_00_PaginaPrincipal.SincronizarModulos()
            ActualizarNombreProyecto()
            System.Windows.MessageBox.Show("Nuevo proyecto creado.", "ARCO",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information)
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.BtnNuevo_Click")
        End Try
    End Sub

    Private Sub BtnAbrir_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_00_PaginaPrincipal.Open()
            ActualizarNombreProyecto()
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.BtnAbrir_Click")
        End Try
    End Sub

    Private Sub BtnGuardar_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_00_PaginaPrincipal.GuardarToolStripMenuItem1.PerformClick()
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.BtnGuardar_Click")
        End Try
    End Sub

    Private Sub BtnImportar_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_00_PaginaPrincipal.ImportarToolStripMenuItem.PerformClick()
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.BtnImportar_Click")
        End Try
    End Sub

    Private Sub BtnReportes_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = TryCast(sender, Button)
        If btn Is Nothing OrElse btn.ContextMenu Is Nothing Then Return
        btn.ContextMenu.PlacementTarget = btn
        btn.ContextMenu.Placement = PlacementMode.Bottom
        btn.ContextMenu.IsOpen = True
    End Sub

    Private Sub BtnAyuda_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_00_PaginaPrincipal.MenuDocumentacion.PerformClick()
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.BtnAyuda_Click")
        End Try
    End Sub

    ' ── Menú de Reportes ───────────────────────────────────────

    Private Sub MenuReporteCompleto_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_Reporte_Proyecto_Completo.Mostrar(Form_00_PaginaPrincipal.proyecto)
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.MenuReporteCompleto_Click")
        End Try
    End Sub

    Private Sub MenuReporteRevision_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_Reporte_Revision.Mostrar(Form_00_PaginaPrincipal.proyecto)
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.MenuReporteRevision_Click")
        End Try
    End Sub

    Private Sub MenuInformeTecnico_Click(sender As Object, e As RoutedEventArgs)
        Try
            Form_Informe_Tecnico.Mostrar(Form_00_PaginaPrincipal.proyecto)
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.MenuInformeTecnico_Click")
        End Try
    End Sub

    Private Sub MenuCombinar_Click(sender As Object, e As RoutedEventArgs)
        Try
            Using frm As New Form_CombinarProyecto()
                frm.ProyectoDestino = Form_00_PaginaPrincipal.proyecto
                If frm.ShowDialog() = Windows.Forms.DialogResult.OK Then
                    Form_00_PaginaPrincipal.SincronizarModulos()
                    Form_00_PaginaPrincipal.MarcarCambios()
                End If
            End Using
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.MenuCombinar_Click")
        End Try
    End Sub

    ' ── Controles de ventana ──────────────────────────────────

    Private Sub BtnMinimize_Click(sender As Object, e As RoutedEventArgs)
        Me.WindowState = System.Windows.WindowState.Minimized
    End Sub

    Private Sub BtnMaximize_Click(sender As Object, e As RoutedEventArgs)
        Me.WindowState = If(Me.WindowState = System.Windows.WindowState.Maximized,
                            System.Windows.WindowState.Normal,
                            System.Windows.WindowState.Maximized)
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub

    ' ── Click en módulos del sidebar ────────────────────────────

    Private Sub SidebarModulo_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = TryCast(sender, Button)
        If btn Is Nothing Then Return
        Dim info = TryCast(btn.DataContext, ModuleInfo)
        If info Is Nothing OrElse Not info.Disponible OrElse info.Accion Is Nothing Then Return
        Try
            info.Accion.Invoke()
        Catch ex As Exception
            Logger.Error(ex, "Form_00_Home_Preview.SidebarModulo_Click", info.Id)
        End Try
    End Sub

End Class
