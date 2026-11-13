''' <summary>
''' Agrupa pisos "estructuralmente iguales" para que el reporte técnico no
''' repita N veces la misma planta cuando la torre es homogénea en altura.
''' Dos pisos son iguales si tienen el mismo set de aristas XY (vigas o
''' nervios), con tolerancia de 2 cm.
'''
''' Si un edificio tiene toda la altura idéntica → 1 grupo → 1 planta.
''' Si hay dos tipologías (ej. subsuelo + torre uniforme) → 2 grupos → 2 plantas.
''' </summary>
Public Class PisosUnicosService

    Public Class GrupoPisos
        Public Property Firma As String
        ''' <summary>Pisos del grupo ordenados sótano → cubierta (por CoorZ si existe).</summary>
        Public Property Pisos As New List(Of String)
        ''' <summary>Z promedio del grupo — se usa para ordenar los grupos entre sí.</summary>
        Public Property ZPromedio As Double
        Public ReadOnly Property PisoRepresentante As String
            Get
                Return If(Pisos.Count > 0, Pisos(0), "")
            End Get
        End Property
    End Class

    ' =========================================================================
    ' Vigas — agrupa por aristas XY de cada frame del piso.
    ' =========================================================================
    Public Shared Function AgruparPisosVigas(proyecto As Proyecto) As List(Of GrupoPisos)
        Dim res As New List(Of GrupoPisos)
        If proyecto Is Nothing OrElse proyecto.Elementos Is Nothing OrElse proyecto.Elementos.Vigas Is Nothing Then Return res
        Dim frames = proyecto.Elementos.Vigas.Frames
        If frames Is Nothing OrElse frames.Count = 0 Then Return res

        Dim joints = IndexarJoints(proyecto.Elementos.Joints)
        Dim framesPorPiso = frames.
            Where(Function(f) f IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(f.Story)).
            GroupBy(Function(f) f.Story)

        Return AgruparPorFirma(framesPorPiso.ToDictionary(
                               Function(g) g.Key,
                               Function(g) g.Select(Function(f) Tuple.Create(f.JointI, f.JointJ)).ToList()),
                               joints, ZPorPiso(proyecto))
    End Function

    ' =========================================================================
    ' Nervios — agrupa por aristas XY de cada cFrameNervio del piso.
    ' Los nervios tienen sus propios Joints (snapshot del import del módulo).
    ' =========================================================================
    Public Shared Function AgruparPisosNervios(proyecto As Proyecto) As List(Of GrupoPisos)
        Dim res As New List(Of GrupoPisos)
        If proyecto Is Nothing OrElse proyecto.Elementos Is Nothing OrElse proyecto.Elementos.Nervios Is Nothing Then Return res
        Dim nervios = proyecto.Elementos.Nervios.Elementos
        If nervios Is Nothing OrElse nervios.Count = 0 Then Return res

        ' Nervios tiene sus propios Joints; si vienen vacíos cae a los del proyecto.
        Dim jSrc = proyecto.Elementos.Nervios.Joints
        If jSrc Is Nothing OrElse jSrc.Count = 0 Then jSrc = proyecto.Elementos.Joints
        Dim joints = IndexarJoints(jSrc)

        Dim framesPorPiso As New Dictionary(Of String, List(Of Tuple(Of String, String)))
        For Each n In nervios
            If n Is Nothing OrElse String.IsNullOrWhiteSpace(n.Piso) OrElse n.Frames Is Nothing Then Continue For
            If Not framesPorPiso.ContainsKey(n.Piso) Then framesPorPiso(n.Piso) = New List(Of Tuple(Of String, String))
            For Each fn In n.Frames
                If fn Is Nothing OrElse String.IsNullOrWhiteSpace(fn.JointI) OrElse String.IsNullOrWhiteSpace(fn.JointJ) Then Continue For
                framesPorPiso(n.Piso).Add(Tuple.Create(fn.JointI, fn.JointJ))
            Next
        Next

        Return AgruparPorFirma(framesPorPiso, joints, ZPorPiso(proyecto))
    End Function

    ' =========================================================================
    ' Núcleo compartido
    ' =========================================================================
    Private Shared Function AgruparPorFirma(framesPorPiso As Dictionary(Of String, List(Of Tuple(Of String, String))),
                                             joints As Dictionary(Of String, cJoint),
                                             zPorPiso As Dictionary(Of String, Double)) As List(Of GrupoPisos)
        Dim firmaPorPiso As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each kv In framesPorPiso
            Dim piso = kv.Key
            Dim aristas As New List(Of String)
            For Each t In kv.Value
                Dim jI As cJoint = Nothing, jJ As cJoint = Nothing
                If Not joints.TryGetValue(t.Item1, jI) Then Continue For
                If Not joints.TryGetValue(t.Item2, jJ) Then Continue For
                ' Arista normalizada: punto menor primero, redondeo a 2 cm.
                Dim ax = Math.Round(jI.GlobalX, 2) : Dim ay = Math.Round(jI.GlobalY, 2)
                Dim bx = Math.Round(jJ.GlobalX, 2) : Dim by_ = Math.Round(jJ.GlobalY, 2)
                Dim a = $"({ax:F2},{ay:F2})"
                Dim b = $"({bx:F2},{by_:F2})"
                aristas.Add(If(String.Compare(a, b) <= 0, a & "-" & b, b & "-" & a))
            Next
            firmaPorPiso(piso) = String.Join(";", aristas.OrderBy(Function(s) s))
        Next

        ' Agrupar pisos por firma; dentro de cada grupo ordenar por Z (sótano→cubierta).
        Dim grupos = firmaPorPiso.
            GroupBy(Function(kv) kv.Value).
            Select(Function(gr) New GrupoPisos With {
                .Firma = gr.Key,
                .Pisos = gr.Select(Function(kv) kv.Key).
                          OrderBy(Function(p) If(zPorPiso.ContainsKey(p), zPorPiso(p), Double.MaxValue)).ToList()
            }).ToList()

        ' Ordenar los grupos por Z promedio.
        For Each g In grupos
            Dim zs = g.Pisos.Where(Function(p) zPorPiso.ContainsKey(p)).Select(Function(p) zPorPiso(p)).ToList()
            g.ZPromedio = If(zs.Count = 0, Double.MaxValue, zs.Average())
        Next
        Return grupos.OrderBy(Function(g) g.ZPromedio).ToList()
    End Function

    Private Shared Function IndexarJoints(jointsRaw As List(Of cJoint)) As Dictionary(Of String, cJoint)
        Dim res As New Dictionary(Of String, cJoint)(StringComparer.OrdinalIgnoreCase)
        If jointsRaw Is Nothing Then Return res
        For Each j In jointsRaw
            If j Is Nothing Then Continue For
            Dim key = If(Not String.IsNullOrWhiteSpace(j.ElementLabel), j.ElementLabel, j.ObjectLabel)
            If String.IsNullOrWhiteSpace(key) Then Continue For
            If Not res.ContainsKey(key) Then res(key) = j
        Next
        Return res
    End Function

    Private Shared Function ZPorPiso(proyecto As Proyecto) As Dictionary(Of String, Double)
        Dim res As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        If proyecto IsNot Nothing AndAlso proyecto.Pisos IsNot Nothing Then
            For Each cp In proyecto.Pisos
                If cp Is Nothing OrElse String.IsNullOrWhiteSpace(cp.Nombre) Then Continue For
                res(cp.Nombre.Trim()) = CDbl(cp.CoorZ)
            Next
        End If
        Return res
    End Function

    ''' <summary>
    ''' Texto compacto para el pie de figura: "Pisos N1 a N5 (5 iguales)",
    ''' "Piso N1" si es uno solo, o un conteo si son muchos.
    ''' </summary>
    Public Shared Function DescribirGrupo(g As GrupoPisos) As String
        If g Is Nothing OrElse g.Pisos.Count = 0 Then Return ""
        If g.Pisos.Count = 1 Then Return "piso " & g.Pisos(0)
        If g.Pisos.Count <= 4 Then Return "pisos " & String.Join(", ", g.Pisos)
        Return $"pisos {g.Pisos.First()} a {g.Pisos.Last()} ({g.Pisos.Count} iguales)"
    End Function

End Class
