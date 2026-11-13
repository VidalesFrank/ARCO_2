Imports System.Windows.Media
Imports System.Windows.Media.Media3D

''' <summary>
''' Paleta centralizada para el visualizador WPF de modelos ETABS (.e2k).
''' Materiales listos para usar en MeshGeometry3D + GeometryModel3D.
'''
''' NotInheritable Class en lugar de Module A PROPÓSITO: los Modules de
''' VB.NET exponen sus miembros al namespace GLOBAL del assembly. Si esta
''' paleta fuera Module, miembros como `Viga` o `Columna` colisionarían
''' con variables de loop en el resto del proyecto (p.ej. `For Each viga
''' In vigas` en VigaService.vb). Como clase estática con miembros Shared,
''' el acceso es `E2k3DPalette.Viga` y no hay contaminación global.
''' </summary>
Public NotInheritable Class E2k3DPalette

    Private Sub New() : End Sub

    ' ── Colores por tipo de elemento ───────────────────────────────
    Public Shared ReadOnly Fondo         As Color = Color.FromRgb(245, 247, 250)
    Public Shared ReadOnly FondoViewport As Color = Color.FromRgb(234, 240, 246)

    Public Shared ReadOnly Columna  As Color = Color.FromRgb(36, 74, 104)
    Public Shared ReadOnly Viga     As Color = Color.FromRgb(42, 157, 95)
    Public Shared ReadOnly Riostra  As Color = Color.FromRgb(229, 115, 70)
    Public Shared ReadOnly Nervio   As Color = Color.FromRgb(214, 158, 46)

    ' Áreas con alpha para dejar ver a través
    Public Shared ReadOnly Muro As Color = Color.FromArgb(185, 214, 158, 46)
    Public Shared ReadOnly Losa As Color = Color.FromArgb(120, 135, 160, 182)

    Public Shared ReadOnly Grid         As Color = Color.FromArgb(170, 125, 145, 170)
    Public Shared ReadOnly GridEtiqueta As Color = Color.FromRgb(104, 119, 134)

    Public Shared ReadOnly Punto     As Color = Color.FromRgb(50, 60, 75)
    Public Shared ReadOnly PisoBase  As Color = Color.FromArgb(70, 215, 222, 232)
    Public Shared ReadOnly Resaltado As Color = Color.FromRgb(50, 115, 168)

    ' Overlays de interacción (emisivo puro, sin Diffuse → siempre brillan)
    Public Shared ReadOnly Hover     As Color = Color.FromRgb(90, 200, 255)   ' cyan suave
    Public Shared ReadOnly Seleccion As Color = Color.FromRgb(255, 170, 60)   ' naranja saturado

    ''' <summary>
    ''' Material difuso + emisivo tenue — da "cuerpo" sin perder legibilidad.
    ''' </summary>
    Public Shared Function MaterialDe(c As Color) As Material
        Dim brush As New SolidColorBrush(c)
        brush.Freeze()
        Dim grp As New MaterialGroup()
        grp.Children.Add(New DiffuseMaterial(brush))
        If c.A = 255 Then
            Dim emi As New SolidColorBrush(Color.FromArgb(40, c.R, c.G, c.B))
            emi.Freeze()
            grp.Children.Add(New EmissiveMaterial(emi))
        End If
        grp.Freeze()
        Return grp
    End Function

    ''' <summary>Material sin iluminación, ideal para líneas/grids.</summary>
    Public Shared Function MaterialPlano(c As Color) As Material
        Dim brush As New SolidColorBrush(c)
        brush.Freeze()
        Dim m As New EmissiveMaterial(brush)
        m.Freeze()
        Return m
    End Function

End Class
