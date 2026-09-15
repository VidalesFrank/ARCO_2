Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Renderer ARCO para MenuStrip: dropdown oscuro (57,57,57), hover verde (0,150,70),
''' texto siempre blanco. Uso: MenuStrip1.Renderer = New ARCOMenuRenderer()
''' </summary>
Public Class ARCOMenuColorTable
    Inherits ProfessionalColorTable

    Private Shared ReadOnly BgBar   As Color = Color.FromArgb(87, 87, 87)
    Private Shared ReadOnly BgDrop  As Color = Color.FromArgb(57, 57, 57)
    Private Shared ReadOnly BgHover As Color = Color.FromArgb(0, 150, 70)
    Private Shared ReadOnly Borde   As Color = Color.FromArgb(70, 70, 70)

    Public Overrides ReadOnly Property MenuStripGradientBegin As Color
        Get
            Return BgBar
        End Get
    End Property

    Public Overrides ReadOnly Property MenuStripGradientEnd As Color
        Get
            Return BgBar
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemSelected As Color
        Get
            Return BgHover
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemSelectedGradientBegin As Color
        Get
            Return BgHover
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemSelectedGradientEnd As Color
        Get
            Return BgHover
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemPressedGradientBegin As Color
        Get
            Return BgDrop
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemPressedGradientEnd As Color
        Get
            Return BgDrop
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemBorder As Color
        Get
            Return BgHover
        End Get
    End Property

    Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
        Get
            Return BgDrop
        End Get
    End Property

    Public Overrides ReadOnly Property MenuBorder As Color
        Get
            Return Borde
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
        Get
            Return BgDrop
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
        Get
            Return BgDrop
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
        Get
            Return BgDrop
        End Get
    End Property

    Public Overrides ReadOnly Property SeparatorDark As Color
        Get
            Return Color.FromArgb(90, 90, 90)
        End Get
    End Property

    Public Overrides ReadOnly Property SeparatorLight As Color
        Get
            Return Color.FromArgb(70, 70, 70)
        End Get
    End Property

End Class

Public Class ARCOMenuRenderer
    Inherits ToolStripProfessionalRenderer

    Public Sub New()
        MyBase.New(New ARCOMenuColorTable())
        Me.RoundedEdges = False
    End Sub

    Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
        e.TextColor = Color.White
        MyBase.OnRenderItemText(e)
    End Sub

    Protected Overrides Sub OnRenderArrow(e As ToolStripArrowRenderEventArgs)
        e.ArrowColor = Color.White
        MyBase.OnRenderArrow(e)
    End Sub

End Class
