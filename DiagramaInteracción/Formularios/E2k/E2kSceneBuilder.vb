Imports System.Windows.Media
Imports System.Windows.Media.Media3D

''' <summary>
''' Construye una escena 3D (<see cref="E2kScene"/>) a partir de un
''' <see cref="E2kModel"/>. Convierte coordenadas ETABS (X, Y, Z-up) a WPF
''' (X, Y-up, Z hacia el visor) con la transformación (x, y, z) → (x, z, -y).
'''
''' Rendimiento: todos los elementos de un mismo tipo se mezclan en una
''' sola <see cref="MeshGeometry3D"/>. Para un edificio típico (~2000
''' columnas + 5000 vigas + 500 muros + 500 losas) quedan ~5 meshes totales
''' con decenas de miles de triángulos — WPF 3D lo maneja sin problema.
''' </summary>
Public Module E2kSceneBuilder

    ' Radio por defecto para "líneas gruesas" (ejes del grid). En metros.
    Private Const GrosorGrid As Double = 0.025
    ' Sección por defecto cuando falta (metros).
    Private Const B_Default As Double = 0.30
    Private Const D_Default As Double = 0.30
    Private Const B_DefaultNervio As Double = 0.10
    Private Const D_DefaultNervio As Double = 0.40
    ' Tamaño de cross-section en modo "Líneas". 2 cm = visible pero esbelto.
    Private Const GrosorLineasBD As Double = 0.025

    Public Function Construir(model As E2kModel, Optional modoLineas As Boolean = False) As E2kScene
        Dim scene As New E2kScene()

        scene.ColumnasPick = New List(Of E2kPickable)()
        scene.VigasPick = New List(Of E2kPickable)()
        scene.NerviosPick = New List(Of E2kPickable)()
        scene.MurosPick = New List(Of E2kPickable)()

        scene.Columnas = ConstruirColumnas(model, scene.ColumnasPick, modoLineas)
        scene.Vigas = ConstruirVigas(model, esNervio:=False, pickables:=scene.VigasPick, modoLineas:=modoLineas)
        scene.Nervios = ConstruirVigas(model, esNervio:=True, pickables:=scene.NerviosPick, modoLineas:=modoLineas)
        scene.Muros = ConstruirMuros(model, scene.MurosPick, modoLineas)
        scene.Losas = ConstruirLosas(model)
        scene.Grids = ConstruirGrids(model)
        scene.PisoBase = ConstruirPisoBase(model)
        scene.Bounds = CalcularBounds(model)

        Return scene
    End Function

    ' ─────────────────────────────────────────────────────────────
    ' Columnas: cajas orientadas B×D×H con rotación por LineAssign.Angle
    ' ─────────────────────────────────────────────────────────────
    Private Function ConstruirColumnas(model As E2kModel,
                                        pickables As List(Of E2kPickable),
                                        Optional modoLineas As Boolean = False) As GeometryModel3D
        Dim mesh As New MeshGeometry3D()
        For Each la In model.LineAssigns
            Dim ln = GetLinea(model, la.LineID)
            If ln Is Nothing OrElse ln.LineType <> E2kLineType.Column Then Continue For
            Dim p = GetPunto(model, ln.Point1)
            If p Is Nothing Then Continue For
            Dim st = model.GetStory(la.Story)
            If st Is Nothing Then Continue For
            Dim sec = GetSeccion(model, la.Section)
            Dim B = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B, B_Default)
            Dim D = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D, D_Default)
            Dim ang = la.Angle * Math.PI / 180.0
            Dim vStart = mesh.Positions.Count
            Dim tStart = mesh.TriangleIndices.Count
            ' En "Líneas" la columna es un prisma muy delgado (eje central visible).
            ' La sección real sigue almacenada en el pickable para info y highlight.
            Dim Bdib = If(modoLineas, GrosorLineasBD, B)
            Dim Ddib = If(modoLineas, GrosorLineasBD, D)
            Dim angDib = If(modoLineas, 0.0, ang)
            AgregarColumnaBox(mesh, p.X, p.Y, st.ElevBottom, st.ElevTop, Bdib, Ddib, angDib)
            pickables.Add(New E2kPickable With {
                .Tipo = "Columna",
                .Label = ln.ID,
                .Story = la.Story,
                .Section = la.Section,
                .SectionB = B,
                .SectionD = D,
                .StartVertex = vStart,
                .EndVertex = mesh.Positions.Count,
                .StartTriangle = tStart,
                .EndTriangle = mesh.TriangleIndices.Count,
                .Anchor = Mapear(p.X, p.Y, (st.ElevBottom + st.ElevTop) / 2.0)
            })
        Next
        If mesh.Positions.Count = 0 Then Return Nothing
        Return CrearModelo(mesh, E2k3DPalette.Columna)
    End Function

    ' ─────────────────────────────────────────────────────────────
    ' Vigas / Nervios: cajas horizontales B×D que cuelgan de ElevTop
    ' ─────────────────────────────────────────────────────────────
    Private Function ConstruirVigas(model As E2kModel, esNervio As Boolean,
                                     pickables As List(Of E2kPickable),
                                     Optional modoLineas As Boolean = False) As GeometryModel3D
        Dim mesh As New MeshGeometry3D()
        For Each la In model.LineAssigns
            Dim ln = GetLinea(model, la.LineID)
            If ln Is Nothing OrElse ln.LineType <> E2kLineType.Beam Then Continue For
            Dim sec = GetSeccion(model, la.Section)
            Dim esNerv = (sec IsNot Nothing AndAlso
                          (sec.Name.ToUpperInvariant().Contains("_N_") OrElse
                           sec.Name.ToUpperInvariant().Contains("NERV")))
            If esNerv <> esNervio Then Continue For

            Dim p1 = GetPunto(model, ln.Point1)
            Dim p2 = GetPunto(model, ln.Point2)
            If p1 Is Nothing OrElse p2 Is Nothing Then Continue For
            Dim st = model.GetStory(la.Story)
            If st Is Nothing Then Continue For

            Dim B = If(sec IsNot Nothing AndAlso sec.B > 0, sec.B,
                       If(esNervio, B_DefaultNervio, B_Default))
            Dim D = If(sec IsNot Nothing AndAlso sec.D > 0, sec.D,
                       If(esNervio, D_DefaultNervio, D_Default))
            Dim Bdib = If(modoLineas, GrosorLineasBD, B)
            Dim Ddib = If(modoLineas, GrosorLineasBD, D)
            Dim vStart = mesh.Positions.Count
            Dim tStart = mesh.TriangleIndices.Count
            AgregarVigaBox(mesh, p1.X, p1.Y, p2.X, p2.Y, st.ElevTop, Bdib, Ddib)
            pickables.Add(New E2kPickable With {
                .Tipo = If(esNervio, "Nervio", "Viga"),
                .Label = ln.ID,
                .Story = la.Story,
                .Section = la.Section,
                .SectionB = B,
                .SectionD = D,
                .StartVertex = vStart,
                .EndVertex = mesh.Positions.Count,
                .StartTriangle = tStart,
                .EndTriangle = mesh.TriangleIndices.Count,
                .Anchor = Mapear((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, st.ElevTop - D / 2.0)
            })
        Next
        If mesh.Positions.Count = 0 Then Return Nothing
        Dim color = If(esNervio, E2k3DPalette.Nervio, E2k3DPalette.Viga)
        Return CrearModelo(mesh, color)
    End Function

    ' ─────────────────────────────────────────────────────────────
    ' Muros: paneles verticales entre pares consecutivos de puntos
    ' ─────────────────────────────────────────────────────────────
    Private Function ConstruirMuros(model As E2kModel,
                                     pickables As List(Of E2kPickable),
                                     Optional modoLineas As Boolean = False) As GeometryModel3D
        Dim mesh As New MeshGeometry3D()
        For Each aa In model.AreaAssigns
            Dim ar = GetArea(model, aa.AreaID)
            If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Panel Then Continue For
            Dim st = model.GetStory(aa.Story)
            If st Is Nothing Then Continue For
            Dim puntos = PuntosUnicos(model, ar)
            If puntos.Count < 2 Then Continue For

            ' Espesor real de la sección (SHELLPROP). Si no viene, 0.
            Dim thick As Double = 0.0
            Dim shell As E2kShellSection = Nothing
            If model.ShellSections.TryGetValue(aa.Section, shell) AndAlso shell.Thickness > 0 Then
                thick = shell.Thickness
            End If

            Dim vStart = mesh.Positions.Count
            Dim tStart = mesh.TriangleIndices.Count
            Dim sumX As Double = 0, sumY As Double = 0, cuenta As Integer = 0
            For i = 0 To puntos.Count - 2
                Dim p1 = puntos(i), p2 = puntos(i + 1)
                If modoLineas OrElse thick <= 0 Then
                    ' Modo Líneas o sin espesor conocido → panel plano (no z-extrude)
                    AgregarPanelMuro(mesh, p1.X, p1.Y, p2.X, p2.Y, st.ElevBottom, st.ElevTop)
                Else
                    AgregarMuroBox(mesh, p1.X, p1.Y, p2.X, p2.Y, st.ElevBottom, st.ElevTop, thick)
                End If
                sumX += (p1.X + p2.X) / 2.0 : sumY += (p1.Y + p2.Y) / 2.0 : cuenta += 1
            Next
            If cuenta = 0 Then Continue For
            Dim cx = sumX / cuenta, cy = sumY / cuenta
            Dim etiq = If(Not String.IsNullOrEmpty(aa.PierName), aa.PierName,
                           If(Not String.IsNullOrEmpty(aa.SpandrelName), aa.SpandrelName, ar.ID))
            pickables.Add(New E2kPickable With {
                .Tipo = "Muro",
                .Label = etiq,
                .Story = aa.Story,
                .Section = aa.Section,
                .SectionB = thick,
                .SectionD = 0,
                .StartVertex = vStart,
                .EndVertex = mesh.Positions.Count,
                .StartTriangle = tStart,
                .EndTriangle = mesh.TriangleIndices.Count,
                .Anchor = Mapear(cx, cy, (st.ElevBottom + st.ElevTop) / 2.0)
            })
        Next
        If mesh.Positions.Count = 0 Then Return Nothing
        Return CrearModelo(mesh, E2k3DPalette.Muro)
    End Function

    ' ─────────────────────────────────────────────────────────────
    ' Losas: polígonos horizontales en ElevTop
    ' ─────────────────────────────────────────────────────────────
    Private Function ConstruirLosas(model As E2kModel) As GeometryModel3D
        Dim mesh As New MeshGeometry3D()
        For Each aa In model.AreaAssigns
            Dim ar = GetArea(model, aa.AreaID)
            If ar Is Nothing OrElse ar.AreaType <> E2kAreaType.Floor Then Continue For
            Dim st = model.GetStory(aa.Story)
            If st Is Nothing Then Continue For
            Dim puntos = PuntosUnicos(model, ar)
            If puntos.Count < 3 Then Continue For
            Dim coords = puntos.Select(Function(pt) Mapear(pt.X, pt.Y, st.ElevTop)).ToList()
            AgregarPoligonoHorizontal(mesh, coords)
        Next
        If mesh.Positions.Count = 0 Then Return Nothing
        Return CrearModelo(mesh, E2k3DPalette.Losa)
    End Function

    ' ─────────────────────────────────────────────────────────────
    ' Grids: líneas gruesas (cilindros muy delgados) para cada eje
    ' ─────────────────────────────────────────────────────────────
    Private Function ConstruirGrids(model As E2kModel) As GeometryModel3D
        If model.Grids.Count = 0 Then Return Nothing
        Dim mesh As New MeshGeometry3D()
        Dim zbase = If(model.Stories.Count > 0, model.Stories.Min(Function(s) s.ElevBottom), 0.0)
        For Each g In model.Grids
            Dim p1 = Mapear(g.X1, g.Y1, zbase)
            Dim p2 = Mapear(g.X2, g.Y2, zbase)
            AgregarCilindro(mesh, p1, p2, GrosorGrid, 6)
        Next
        If mesh.Positions.Count = 0 Then Return Nothing
        Return CrearModelo(mesh, E2k3DPalette.Grid, plano:=True)
    End Function

    ' ─────────────────────────────────────────────────────────────
    ' Plano suelo translúcido para dar referencia espacial
    ' ─────────────────────────────────────────────────────────────
    Private Function ConstruirPisoBase(model As E2kModel) As GeometryModel3D
        Dim r = model.GetBoundsXY()
        Dim margen As Single = CSng(Math.Max(r.Width, r.Height) * 0.3F)
        Dim zbase = If(model.Stories.Count > 0, model.Stories.Min(Function(s) s.ElevBottom), 0.0)
        Dim mesh As New MeshGeometry3D()
        Dim p1 = Mapear(r.X - margen, r.Y - margen, zbase)
        Dim p2 = Mapear(r.X + r.Width + margen, r.Y - margen, zbase)
        Dim p3 = Mapear(r.X + r.Width + margen, r.Y + r.Height + margen, zbase)
        Dim p4 = Mapear(r.X - margen, r.Y + r.Height + margen, zbase)
        AgregarQuadDosCaras(mesh, p1, p2, p3, p4)
        Return CrearModelo(mesh, E2k3DPalette.PisoBase)
    End Function

    ' ═════════════════════════════════════════════════════════════
    ' HELPERS GEOMÉTRICOS
    ' ═════════════════════════════════════════════════════════════

    ''' <summary>ETABS (x, y, z) → WPF (x, z, -y)</summary>
    Public Function Mapear(x As Double, y As Double, z As Double) As Point3D
        Return New Point3D(x, z, -y)
    End Function

    ''' <summary>
    ''' Columna: caja orientada vertical B×D con rotación angle.
    ''' Convención ETABS (y ARCO, ver Form_Planta_Columnas): el eje local 2 =
    ''' Depth (<paramref name="D"/>), y con ángulo 0 va a lo largo de +X. El
    ''' eje local 3 = Width (<paramref name="B"/>) y con ángulo 0 va a lo largo
    ''' de +Y. Antes del fix el código asumía al revés (B sobre axis-2), y las
    ''' columnas aparecían rotadas 90° respecto a ETABS.
    ''' </summary>
    Private Sub AgregarColumnaBox(mesh As MeshGeometry3D,
                                   cx As Double, cy As Double,
                                   zBot As Double, zTop As Double,
                                   B As Double, D As Double, angleRad As Double)
        Dim ux = Math.Cos(angleRad), uy = Math.Sin(angleRad)   ' axis 2 = D
        Dim vx = -uy, vy = ux                                    ' axis 3 = B
        Dim hD = D / 2, hB = B / 2
        Dim base = mesh.Positions.Count
        For Each dz In {zBot, zTop}
            For Each sD In {-hD, hD}
                For Each sB In {-hB, hB}
                    mesh.Positions.Add(Mapear(cx + sD * ux + sB * vx,
                                              cy + sD * uy + sB * vy, dz))
                Next
            Next
        Next
        ' Orden del loop: dz(0,1) | sD(0,1) | sB(0,1) → idx = dz*4 + sD*2 + sB
        AgregarCarasCaja(mesh, base)
    End Sub

    ''' <summary>Viga horizontal entre (x1,y1) y (x2,y2) a elevación z, B×D.</summary>
    Private Sub AgregarVigaBox(mesh As MeshGeometry3D,
                                x1 As Double, y1 As Double,
                                x2 As Double, y2 As Double,
                                zTop As Double, B As Double, D As Double)
        Dim dx = x2 - x1, dy = y2 - y1
        Dim len = Math.Sqrt(dx * dx + dy * dy)
        If len < 1e-6 Then Return
        ' Axis 1 (longitudinal, en el plano XY)
        Dim a1x = dx / len, a1y = dy / len
        ' Axis 2 (ancho horizontal, perpendicular en plano XY)
        Dim a2x = -a1y, a2y = a1x
        ' Axis 3: vertical hacia abajo (la viga cuelga de ElevTop)
        Dim hB = B / 2
        Dim zBot = zTop - D
        Dim base = mesh.Positions.Count
        ' 8 vertices: para cada combinación (zBot/zTop) × (−B/2, +B/2) × (p1-end / p2-end)
        For Each endIdx In {0, 1}
            Dim bx = If(endIdx = 0, x1, x2)
            Dim by = If(endIdx = 0, y1, y2)
            For Each sB In {-hB, hB}
                For Each dz In {zBot, zTop}
                    mesh.Positions.Add(Mapear(bx + sB * a2x, by + sB * a2y, dz))
                Next
            Next
        Next
        ' idx = endIdx*4 + sB*2 + dz
        AgregarCarasCaja(mesh, base)
    End Sub

    ''' <summary>Panel muro: rectángulo vertical entre (x1,y1,zBot) y (x2,y2,zTop).</summary>
    Private Sub AgregarPanelMuro(mesh As MeshGeometry3D,
                                  x1 As Double, y1 As Double,
                                  x2 As Double, y2 As Double,
                                  zBot As Double, zTop As Double)
        Dim p1 = Mapear(x1, y1, zBot)
        Dim p2 = Mapear(x2, y2, zBot)
        Dim p3 = Mapear(x2, y2, zTop)
        Dim p4 = Mapear(x1, y1, zTop)
        AgregarQuadDosCaras(mesh, p1, p2, p3, p4)
    End Sub

    ''' <summary>
    ''' Muro extruido: prisma rectangular que extruye el segmento (x1,y1)→(x2,y2)
    ''' perpendicularmente por <paramref name="thickness"/> entre zBot y zTop.
    ''' Produce 8 vértices + 36 triangle indices (igual estructura que una caja
    ''' de columna/viga). Orden de inserción: iEnd × sSide × dz, con idx local
    ''' idx = iEnd·4 + iSide·2 + iZ, compatible con AgregarCarasCaja.
    ''' </summary>
    Private Sub AgregarMuroBox(mesh As MeshGeometry3D,
                                x1 As Double, y1 As Double,
                                x2 As Double, y2 As Double,
                                zBot As Double, zTop As Double,
                                thickness As Double)
        Dim dx = x2 - x1, dy = y2 - y1
        Dim L = Math.Sqrt(dx * dx + dy * dy)
        If L < 1.0E-6 Then Return
        Dim nx = -dy / L, ny = dx / L     ' perpendicular horizontal al segmento
        Dim ht = thickness / 2.0
        Dim base = mesh.Positions.Count
        For Each iEnd In {0, 1}
            Dim bx = If(iEnd = 0, x1, x2)
            Dim by = If(iEnd = 0, y1, y2)
            For Each sSide In {-ht, ht}
                For Each dz In {zBot, zTop}
                    mesh.Positions.Add(Mapear(bx + sSide * nx, by + sSide * ny, dz))
                Next
            Next
        Next
        AgregarCarasCaja(mesh, base)
    End Sub

    ''' <summary>
    ''' Agrega las 12 caras (2 triángulos/cara) de una caja definida por los 8
    ''' vértices consecutivos ya insertados a partir de <paramref name="base"/>.
    ''' Orden de los índices: bit0 = eje más fino, bit1, bit2 = más grueso.
    ''' </summary>
    Private Sub AgregarCarasCaja(mesh As MeshGeometry3D, base As Integer)
        ' Vertices en orden (a, b, c) donde idx = a*4 + b*2 + c, a/b/c ∈ {0,1}.
        Dim ix As Func(Of Integer, Integer, Integer, Integer) = Function(a, b, c) base + a * 4 + b * 2 + c
        ' Cara a=0
        Q(mesh, ix(0, 0, 0), ix(0, 0, 1), ix(0, 1, 1), ix(0, 1, 0))
        ' Cara a=1
        Q(mesh, ix(1, 1, 0), ix(1, 1, 1), ix(1, 0, 1), ix(1, 0, 0))
        ' Cara b=0
        Q(mesh, ix(1, 0, 0), ix(1, 0, 1), ix(0, 0, 1), ix(0, 0, 0))
        ' Cara b=1
        Q(mesh, ix(0, 1, 0), ix(0, 1, 1), ix(1, 1, 1), ix(1, 1, 0))
        ' Cara c=0
        Q(mesh, ix(0, 0, 0), ix(0, 1, 0), ix(1, 1, 0), ix(1, 0, 0))
        ' Cara c=1
        Q(mesh, ix(0, 0, 1), ix(1, 0, 1), ix(1, 1, 1), ix(0, 1, 1))
    End Sub

    Private Sub Q(mesh As MeshGeometry3D, a As Integer, b As Integer, c As Integer, d As Integer)
        mesh.TriangleIndices.Add(a) : mesh.TriangleIndices.Add(b) : mesh.TriangleIndices.Add(c)
        mesh.TriangleIndices.Add(a) : mesh.TriangleIndices.Add(c) : mesh.TriangleIndices.Add(d)
    End Sub

    Private Sub AgregarQuadDosCaras(mesh As MeshGeometry3D,
                                    p1 As Point3D, p2 As Point3D,
                                    p3 As Point3D, p4 As Point3D)
        Dim i = mesh.Positions.Count
        mesh.Positions.Add(p1)
        mesh.Positions.Add(p2)
        mesh.Positions.Add(p3)
        mesh.Positions.Add(p4)
        ' Frente
        mesh.TriangleIndices.Add(i) : mesh.TriangleIndices.Add(i + 1) : mesh.TriangleIndices.Add(i + 2)
        mesh.TriangleIndices.Add(i) : mesh.TriangleIndices.Add(i + 2) : mesh.TriangleIndices.Add(i + 3)
        ' Revés (para que se vea desde ambos lados)
        mesh.TriangleIndices.Add(i) : mesh.TriangleIndices.Add(i + 2) : mesh.TriangleIndices.Add(i + 1)
        mesh.TriangleIndices.Add(i) : mesh.TriangleIndices.Add(i + 3) : mesh.TriangleIndices.Add(i + 2)
    End Sub

    Private Sub AgregarPoligonoHorizontal(mesh As MeshGeometry3D, pts As IList(Of Point3D))
        If pts.Count < 3 Then Return
        Dim base = mesh.Positions.Count
        For Each p In pts : mesh.Positions.Add(p) : Next
        ' Abanico desde el punto 0 (válido para polígonos convexos; ETABS
        ' tipicamente usa losas convexas).
        For i = 1 To pts.Count - 2
            mesh.TriangleIndices.Add(base)
            mesh.TriangleIndices.Add(base + i)
            mesh.TriangleIndices.Add(base + i + 1)
            ' Reverso
            mesh.TriangleIndices.Add(base)
            mesh.TriangleIndices.Add(base + i + 1)
            mesh.TriangleIndices.Add(base + i)
        Next
    End Sub

    Private Sub AgregarCilindro(mesh As MeshGeometry3D,
                                 p1 As Point3D, p2 As Point3D,
                                 radio As Double, segs As Integer)
        Dim dir = p2 - p1
        Dim L = dir.Length
        If L < 1e-6 Then Return
        dir.Normalize()
        Dim perp As Vector3D
        If Math.Abs(dir.Y) < 0.95 Then
            perp = Vector3D.CrossProduct(dir, New Vector3D(0, 1, 0))
        Else
            perp = Vector3D.CrossProduct(dir, New Vector3D(1, 0, 0))
        End If
        perp.Normalize()
        Dim perp2 = Vector3D.CrossProduct(dir, perp)
        perp2.Normalize()

        Dim base = mesh.Positions.Count
        For i = 0 To segs - 1
            Dim a = 2 * Math.PI * i / segs
            Dim off = perp * (radio * Math.Cos(a)) + perp2 * (radio * Math.Sin(a))
            mesh.Positions.Add(p1 + off)
            mesh.Positions.Add(p2 + off)
        Next
        For i = 0 To segs - 1
            Dim j = (i + 1) Mod segs
            Dim i0 = base + i * 2
            Dim i1 = base + i * 2 + 1
            Dim i2 = base + j * 2
            Dim i3 = base + j * 2 + 1
            mesh.TriangleIndices.Add(i0) : mesh.TriangleIndices.Add(i1) : mesh.TriangleIndices.Add(i3)
            mesh.TriangleIndices.Add(i0) : mesh.TriangleIndices.Add(i3) : mesh.TriangleIndices.Add(i2)
        Next
    End Sub

    ' ═════════════════════════════════════════════════════════════
    ' LOOKUPS
    ' ═════════════════════════════════════════════════════════════

    Private Function GetLinea(m As E2kModel, id As String) As E2kLine
        Dim r As E2kLine = Nothing
        m.Lines.TryGetValue(id, r) : Return r
    End Function
    Private Function GetPunto(m As E2kModel, id As String) As E2kPoint
        Dim r As E2kPoint = Nothing
        m.Points.TryGetValue(id, r) : Return r
    End Function
    Private Function GetSeccion(m As E2kModel, name As String) As E2kFrameSection
        If String.IsNullOrEmpty(name) Then Return Nothing
        Dim r As E2kFrameSection = Nothing
        m.FrameSections.TryGetValue(name, r) : Return r
    End Function
    Private Function GetArea(m As E2kModel, id As String) As E2kArea
        Dim r As E2kArea = Nothing
        m.Areas.TryGetValue(id, r) : Return r
    End Function

    Private Function PuntosUnicos(m As E2kModel, ar As E2kArea) As List(Of E2kPoint)
        Dim res As New List(Of E2kPoint)
        Dim vistos As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each pid In ar.Points
            If vistos.Contains(pid) Then Continue For
            Dim p = GetPunto(m, pid)
            If p IsNot Nothing Then
                res.Add(p)
                vistos.Add(pid)
            End If
        Next
        Return res
    End Function

    ' ═════════════════════════════════════════════════════════════
    ' BOUNDS Y MATERIAL FACTORY
    ' ═════════════════════════════════════════════════════════════

    Private Function CalcularBounds(m As E2kModel) As Rect3D
        If m.Points.Count = 0 Then Return New Rect3D(-5, 0, -5, 10, 10, 10)
        Dim r = m.GetBoundsXY()
        Dim zMin = If(m.Stories.Count > 0, m.Stories.Min(Function(s) s.ElevBottom), 0.0)
        Dim zMax = If(m.Stories.Count > 0, m.Stories.Max(Function(s) s.ElevTop), 10.0)
        ' WPF: X = ETABS X, Y = ETABS Z, Z = -ETABS Y
        Return New Rect3D(r.X, zMin, -(r.Y + r.Height),
                           r.Width, zMax - zMin, r.Height)
    End Function

    ''' <summary>
    ''' Extrae la geometría del elemento <paramref name="pick"/> del mesh base
    ''' <paramref name="meshBase"/>, aplicando una escala desde el centroide
    ''' para evitar z-fighting con la mesh original. Devuelve Nothing si los
    ''' rangos están vacíos.
    ''' </summary>
    Public Function ExtraerMeshDe(meshBase As MeshGeometry3D,
                                   pick As E2kPickable,
                                   Optional escala As Double = 1.02) As MeshGeometry3D
        If meshBase Is Nothing OrElse pick Is Nothing Then Return Nothing
        If pick.EndVertex <= pick.StartVertex Then Return Nothing
        If pick.EndTriangle <= pick.StartTriangle Then Return Nothing

        ' Centroide de las posiciones del elemento
        Dim cx As Double = 0, cy As Double = 0, cz As Double = 0
        Dim n = pick.EndVertex - pick.StartVertex
        For i = pick.StartVertex To pick.EndVertex - 1
            Dim p = meshBase.Positions(i)
            cx += p.X : cy += p.Y : cz += p.Z
        Next
        cx /= n : cy /= n : cz /= n

        Dim m As New MeshGeometry3D()
        For i = pick.StartVertex To pick.EndVertex - 1
            Dim p = meshBase.Positions(i)
            m.Positions.Add(New Point3D(
                cx + (p.X - cx) * escala,
                cy + (p.Y - cy) * escala,
                cz + (p.Z - cz) * escala))
        Next
        ' Reindexar: índice local = índice global - StartVertex
        For t = pick.StartTriangle To pick.EndTriangle - 1
            m.TriangleIndices.Add(meshBase.TriangleIndices(t) - pick.StartVertex)
        Next
        m.Freeze()
        Return m
    End Function

    Private Function CrearModelo(mesh As MeshGeometry3D, color As Color,
                                  Optional plano As Boolean = False) As GeometryModel3D
        mesh.Freeze()
        Dim model As New GeometryModel3D()
        model.Geometry = mesh
        Dim mat = If(plano, E2k3DPalette.MaterialPlano(color), E2k3DPalette.MaterialDe(color))
        model.Material = mat
        model.BackMaterial = mat
        model.Freeze()
        Return model
    End Function

End Module

''' <summary>Contenedor con los modelos 3D por categoría y los bounds.</summary>
Public Class E2kScene
    Public Property Columnas As GeometryModel3D
    Public Property Vigas As GeometryModel3D
    Public Property Nervios As GeometryModel3D
    Public Property Muros As GeometryModel3D
    Public Property Losas As GeometryModel3D
    Public Property Grids As GeometryModel3D
    Public Property PisoBase As GeometryModel3D
    Public Property Bounds As Rect3D

    ' Índices de picking: metadatos por elemento + rango de vértices en el
    ' mesh combinado. Permite recuperar la identidad del elemento tras un
    ' RayMeshGeometry3DHitTestResult.
    Public Property ColumnasPick As List(Of E2kPickable)
    Public Property VigasPick As List(Of E2kPickable)
    Public Property NerviosPick As List(Of E2kPickable)
    Public Property MurosPick As List(Of E2kPickable)
End Class

''' <summary>
''' Metadato de un elemento picable. <see cref="StartVertex"/> y
''' <see cref="EndVertex"/> delimitan el rango de posiciones en el mesh
''' combinado de la categoría (EndVertex es exclusivo).
''' </summary>
Public Class E2kPickable
    Public Property Tipo As String = ""       ' "Columna" | "Viga" | "Nervio" | "Muro"
    Public Property Label As String = ""      ' ID del Line / Pier / Spandrel / Area
    Public Property Story As String = ""
    Public Property Section As String = ""
    Public Property SectionB As Double        ' metros (0 si no aplica)
    Public Property SectionD As Double        ' metros (0 si no aplica)
    Public Property StartVertex As Integer    ' índice en MeshGeometry3D.Positions (inclusivo)
    Public Property EndVertex As Integer      ' exclusivo
    Public Property StartTriangle As Integer  ' índice en MeshGeometry3D.TriangleIndices (inclusivo)
    Public Property EndTriangle As Integer    ' exclusivo
    Public Property Anchor As Point3D          ' en coords WPF, para superponer etiquetas
End Class
