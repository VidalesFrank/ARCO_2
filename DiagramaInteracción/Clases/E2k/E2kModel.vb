Imports System.Drawing
Imports System.Linq

' ════════════════════════════════════════════════════════════════════════════
'  Clases de datos para el modelo ETABS (.e2k)
' ════════════════════════════════════════════════════════════════════════════

Public Class E2kModel
    Public Property FilePath As String = ""
    Public Property Units As String = ""
    Public Property Stories As New List(Of E2kStory)
    Public Property Grids As New List(Of E2kGrid)
    Public Property Points As New Dictionary(Of String, E2kPoint)(StringComparer.OrdinalIgnoreCase)
    Public Property FrameSections As New Dictionary(Of String, E2kFrameSection)(StringComparer.OrdinalIgnoreCase)
    Public Property PierSections As New Dictionary(Of String, E2kPierSection)(StringComparer.OrdinalIgnoreCase)
    Public Property Lines As New Dictionary(Of String, E2kLine)(StringComparer.OrdinalIgnoreCase)
    Public Property Areas As New Dictionary(Of String, E2kArea)(StringComparer.OrdinalIgnoreCase)
    Public Property LineAssigns As New List(Of E2kLineAssign)
    Public Property AreaAssigns As New List(Of E2kAreaAssign)
    Public Property PierNames As New List(Of String)

    Public Function GetStory(name As String) As E2kStory
        Return Stories.FirstOrDefault(Function(s) String.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
    End Function

    Public Function MaxElevation() As Double
        If Stories.Count = 0 Then Return 0
        Return Stories.Max(Function(s) s.ElevTop)
    End Function

    Public Function GetBoundsXY() As RectangleF
        If Points.Count = 0 Then Return New RectangleF(0, 0, 10, 10)
        Dim xs = Points.Values.Select(Function(p) CSng(p.X))
        Dim ys = Points.Values.Select(Function(p) CSng(p.Y))
        Dim x0 = xs.Min() : Dim x1 = xs.Max()
        Dim y0 = ys.Min() : Dim y1 = ys.Max()
        Dim pad = CSng(Math.Max(Math.Max(x1 - x0, y1 - y0) * 0.08, 1.0))
        Return New RectangleF(x0 - pad, y0 - pad, (x1 - x0) + 2 * pad, (y1 - y0) + 2 * pad)
    End Function

    Public ReadOnly Property StoryCount As Integer
        Get
            Return Stories.Count
        End Get
    End Property

    Public ReadOnly Property FrameCount As Integer
        Get
            Return LineAssigns.Count
        End Get
    End Property

    Public ReadOnly Property AreaCount As Integer
        Get
            Return AreaAssigns.Count
        End Get
    End Property
End Class

' ─────────────────────────────────────────────────────────────────────────────

Public Class E2kStory
    Public Property Name As String = ""
    Public Property Height As Double
    Public Property ElevBottom As Double
    Public Property ElevTop As Double
    Public Property IsMaster As Boolean
    Public Property SimilarTo As String = ""
    Public Overrides Function ToString() As String
        Return $"{Name}  [{ElevBottom:F2} – {ElevTop:F2} m]"
    End Function
End Class

Public Class E2kGrid
    Public Property Label As String = ""
    Public Property X1 As Double
    Public Property Y1 As Double
    Public Property X2 As Double
    Public Property Y2 As Double
    Public ReadOnly Property IsVertical As Boolean
        Get
            Return Math.Abs(X2 - X1) < Math.Abs(Y2 - Y1)
        End Get
    End Property
End Class

Public Class E2kPoint
    Public Property ID As String = ""
    Public Property X As Double
    Public Property Y As Double
End Class

Public Enum E2kLineType
    Column
    Beam
    Brace
    Other
End Enum

Public Enum E2kSectionShape
    Rectangular
    Circle
    IShape
    Other
End Enum

Public Class E2kFrameSection
    Public Property Name As String = ""
    Public Property Shape As E2kSectionShape = E2kSectionShape.Rectangular
    Public Property D As Double   ' depth (local-3 direction)
    Public Property B As Double   ' width (local-2 direction)
    Public Property Material As String = ""

    Public ReadOnly Property IsBeamLike As Boolean
        Get
            Dim n = Name.ToUpperInvariant()
            Return n.Contains("_V_") OrElse n.Contains("_N_") OrElse
                   n.Contains("VIGA") OrElse n.Contains("NERV") OrElse n.Contains("BEAM")
        End Get
    End Property
End Class

Public Class E2kPierSection
    Public Property Name As String = ""
    Public Property Material As String = ""
    Public Property Shapes As New List(Of List(Of PointF))
End Class

Public Class E2kLine
    Public Property ID As String = ""
    Public Property LineType As E2kLineType
    Public Property Point1 As String = ""
    Public Property Point2 As String = ""
End Class

Public Enum E2kAreaType
    Panel   ' muro
    Floor   ' losa
    Other
End Enum

Public Class E2kArea
    Public Property ID As String = ""
    Public Property AreaType As E2kAreaType
    Public Property Points As New List(Of String)
End Class

Public Class E2kLineAssign
    Public Property LineID As String = ""
    Public Property Story As String = ""
    Public Property Section As String = ""
    Public Property Angle As Double   ' grados, rotación de sección
End Class

Public Class E2kAreaAssign
    Public Property AreaID As String = ""
    Public Property Story As String = ""
    Public Property Section As String = ""
    Public Property PierName As String = ""
    Public Property SpandrelName As String = ""
End Class

' ─────────────────────────────────────────────────────────────────────────────
'  Vector 3D — usado por el renderer
' ─────────────────────────────────────────────────────────────────────────────

Public Structure V3
    Public X As Double
    Public Y As Double
    Public Z As Double

    Public Sub New(x As Double, y As Double, z As Double)
        Me.X = x : Me.Y = y : Me.Z = z
    End Sub

    Public Function Add(v As V3) As V3
        Return New V3(X + v.X, Y + v.Y, Z + v.Z)
    End Function

    Public Function Scale(s As Double) As V3
        Return New V3(X * s, Y * s, Z * s)
    End Function

    Public Function Dot(v As V3) As Double
        Return X * v.X + Y * v.Y + Z * v.Z
    End Function

    Public Function Len() As Double
        Return Math.Sqrt(X * X + Y * Y + Z * Z)
    End Function

    Public Function Norm() As V3
        Dim L = Len()
        If L < 1e-10 Then Return New V3(0, 0, 1)
        Return New V3(X / L, Y / L, Z / L)
    End Function

    Public Function Cross(v As V3) As V3
        Return New V3(Y * v.Z - Z * v.Y, Z * v.X - X * v.Z, X * v.Y - Y * v.X)
    End Function
End Structure
