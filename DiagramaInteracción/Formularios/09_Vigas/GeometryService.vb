Imports ARCO.Funciones_00_Varias

Public Class GeometryService

    Public Function PuntoMedioFrame(f As cFrame,
                      joints As Dictionary(Of String, cJoint)) As Vector3

        Dim ji = joints(f.JointI)
        Dim jj = joints(f.JointJ)

        Return New Vector3(
        (ji.GlobalX + jj.GlobalX) / 2.0,
        (ji.GlobalY + jj.GlobalY) / 2.0,
        (ji.GlobalZ + jj.GlobalZ) / 2.0
    )
    End Function


    Public Function Distancia_fj(f As cFrame,
                   joints As Dictionary(Of String, cJoint)) As Double

        ' Obtener joints I y J desde el diccionario
        Dim j1 As cJoint = Nothing
        Dim j2 As cJoint = Nothing

        If Not joints.TryGetValue(f.JointI, j1) Then
            Throw New Exception($"No se encontró el Joint I: {f.JointI}")
        End If

        If Not joints.TryGetValue(f.JointJ, j2) Then
            Throw New Exception($"No se encontró el Joint J: {f.JointJ}")
        End If

        ' Calcular distancia 3D
        Return Math.Sqrt(
        (j1.GlobalX - j2.GlobalX) ^ 2 +
        (j1.GlobalY - j2.GlobalY) ^ 2 +
        (j1.GlobalZ - j2.GlobalZ) ^ 2
    )
    End Function

    Private Shared Function Distancia(p1 As cJoint, p2 As cJoint) As Double
        Return Math.Sqrt((p1.GlobalX - p2.GlobalX) ^ 2 +
                     (p1.GlobalY - p2.GlobalY) ^ 2 +
                     (p1.GlobalZ - p2.GlobalZ) ^ 2)
    End Function

    Public Function PuntoJoint(id As String, joints As Dictionary(Of String, cJoint)) As Vector3
        Dim j = joints(id)
        Return New Vector3(j.GlobalX, j.GlobalY, j.GlobalZ)
    End Function

    Public Function VectorFrame(f As cFrame, joints As Dictionary(Of String, cJoint)) As Vector3
        Return PuntoJoint(f.JointJ, joints) - PuntoJoint(f.JointI, joints)
    End Function

    Function SonColineales(f As cFrame,
                       dirViga As Vector3,
                       joints As Dictionary(Of String, cJoint),
                       tol As Double) As Boolean

        Dim v = VectorFrame(f, joints)
        v.Normalize()

        ' Producto cruz con la dirección de la viga
        Dim cross = Vector3.Cross(v, dirViga)

        Return cross.Length < tol
    End Function

    Public Function CompartenJoint(f1 As cFrame, f2 As cFrame) As Boolean
        Return f1.JointI = f2.JointI OrElse
           f1.JointI = f2.JointJ OrElse
           f1.JointJ = f2.JointI OrElse
           f1.JointJ = f2.JointJ
    End Function

    Public Shared Function EstanEnLaMismaLinea(f1 As cFrame, f2 As cFrame,
                                            joints As Dictionary(Of String, cJoint),
                                            tol As Double) As Boolean

        Dim p1 = joints(f1.JointI)
        Dim p2 = joints(f1.JointJ)
        Dim pTest = joints(f2.JointI)

        ' Vector dirección de la viga base
        Dim vx = p2.GlobalX - p1.GlobalX
        Dim vy = p2.GlobalY - p1.GlobalY
        Dim vz = p2.GlobalZ - p1.GlobalZ

        ' Vector desde línea base al punto a evaluar
        Dim wx = pTest.GlobalX - p1.GlobalX
        Dim wy = pTest.GlobalY - p1.GlobalY
        Dim wz = pTest.GlobalZ - p1.GlobalZ

        ' Producto cruz (distancia a la recta)
        Dim cx = vy * wz - vz * wy
        Dim cy = vz * wx - vx * wz
        Dim cz = vx * wy - vy * wx

        Dim distancia = Math.Sqrt(cx * cx + cy * cy + cz * cz) /
                    Math.Sqrt(vx * vx + vy * vy + vz * vz)

        Return distancia <= tol

    End Function

    Public Shared Function SonContinuos(f1 As cFrame, f2 As cFrame,
                                    joints As Dictionary(Of String, cJoint),
                                    tol As Double) As Boolean

        Dim extremos1 = ObtenerExtremos(f1, joints)
        Dim extremos2 = ObtenerExtremos(f2, joints)

        For Each p1 In extremos1
            For Each p2 In extremos2

                Dim d = Distancia(p1, p2)

                If d <= tol Then
                    Return True
                End If

            Next
        Next

        Return False

    End Function

    Public Shared Function ObtenerExtremos(f As cFrame,
                                    joints As Dictionary(Of String, cJoint)) _
                                    As List(Of cJoint)

        Dim lista As New List(Of cJoint)

        If joints.ContainsKey(f.JointI) Then lista.Add(joints(f.JointI))
        If joints.ContainsKey(f.JointJ) Then lista.Add(joints(f.JointJ))

        Return lista

    End Function

    ''' Asigna EjeApoyo_I / EjeApoyo_J a cada frame de la viga buscando el grid
    ''' perpendicular más cercano al joint correspondiente dentro de la tolerancia.
    ''' Soporta X Cartesian, Y Cartesian y General Cartesian (X1,Y1→X2,Y2).
    Public Sub AsignarEjesAViga(viga As cViga,
                                grids As List(Of cGridLine),
                                joints As Dictionary(Of String, cJoint),
                                Optional tolMax As Double = 0.5)

        If grids Is Nothing OrElse grids.Count = 0 Then Exit Sub
        If viga.Frames Is Nothing OrElse viga.Frames.Count = 0 Then Exit Sub

        Dim esX As Boolean = Math.Abs(viga.Direccion.X) >= Math.Abs(viga.Direccion.Y)
        Dim dirBuscar As String = If(esX, "X", "Y")

        ' Incluir grids X/Y perpendiculares al beam + todos los General (se filtran por distancia)
        Dim gridsPerp = grids.Where(Function(g) Not String.IsNullOrWhiteSpace(g.GridID) AndAlso
                                                (g.Direction = dirBuscar OrElse g.EsTipoGeneral)).ToList()
        If gridsPerp.Count = 0 Then Exit Sub

        For Each frame In viga.Frames
            frame.EjeApoyo_I = BuscarEjeMasCercano(frame.JointI, gridsPerp, joints, esX, tolMax)
            frame.EjeApoyo_J = BuscarEjeMasCercano(frame.JointJ, gridsPerp, joints, esX, tolMax)
        Next

    End Sub

    ''' Distancia perpendicular de un punto (px,py) a la línea infinita que pasa por (x1,y1)-(x2,y2).
    Private Shared Function DistanciaPuntoALinea(px As Double, py As Double,
                                                  x1 As Double, y1 As Double,
                                                  x2 As Double, y2 As Double) As Double
        Dim dx = x2 - x1
        Dim dy = y2 - y1
        Dim len2 = dx * dx + dy * dy
        If len2 = 0.0 Then Return Math.Sqrt((px - x1) ^ 2 + (py - y1) ^ 2)
        Return Math.Abs(dy * (px - x1) - dx * (py - y1)) / Math.Sqrt(len2)
    End Function

    Private Function BuscarEjeMasCercano(jointId As String,
                                         grids As List(Of cGridLine),
                                         joints As Dictionary(Of String, cJoint),
                                         esX As Boolean,
                                         tolMax As Double) As String

        Dim j As cJoint = Nothing
        If Not joints.TryGetValue(jointId, j) Then Return ""

        Dim mejor As String = ""
        Dim menorDist As Double = Double.MaxValue

        For Each gl In grids
            Dim dist As Double
            If gl.EsTipoGeneral Then
                ' Distancia perpendicular del joint a la línea General
                dist = DistanciaPuntoALinea(j.GlobalX, j.GlobalY, gl.X1, gl.Y1, gl.X2, gl.Y2)
            Else
                ' Distancia a la ordenada del eje X o Y Cartesian
                Dim coord As Double = If(esX, j.GlobalX, j.GlobalY)
                dist = Math.Abs(coord - gl.Ordinate)
            End If
            If dist < menorDist Then
                menorDist = dist
                mejor = gl.GridID
            End If
        Next

        Return If(menorDist <= tolMax, mejor, "")

    End Function

    ''' Asigna ejes estructurales a todas las vigas de la lista.
    Public Sub AsignarEjesAVigas(vigas As List(Of cViga),
                                 grids As List(Of cGridLine),
                                 joints As Dictionary(Of String, cJoint),
                                 Optional tolMax As Double = 0.5)

        If vigas Is Nothing OrElse grids Is Nothing Then Exit Sub
        For Each v In vigas
            AsignarEjesAViga(v, grids, joints, tolMax)
        Next

    End Sub

    ''' Asigna EjeParalelo a una viga: el eje estructural que la viga "sigue" (paralelo a ella).
    ''' Para viga en X busca el grid tipo "Y" (coordenada Y constante) más cercano al centroide Y de la viga.
    ''' Para viga en Y busca el grid tipo "X" (coordenada X constante) más cercano al centroide X.
    ''' Soporta también grids General cuya dirección sea casi paralela a la viga.
    Public Sub AsignarEjeParaleloAViga(viga As cViga,
                                        grids As List(Of cGridLine),
                                        joints As Dictionary(Of String, cJoint),
                                        Optional tolMax As Double = 1.0)

        If grids Is Nothing OrElse grids.Count = 0 Then Exit Sub
        If viga.Frames Is Nothing OrElse viga.Frames.Count = 0 Then Exit Sub

        Dim esX As Boolean = Math.Abs(viga.Direccion.X) >= Math.Abs(viga.Direccion.Y)
        ' Eje paralelo: si la viga va en X, sigue un grid de tipo Y (ordinate Y constante)
        Dim dirParalelo As String = If(esX, "Y", "X")

        ' Recolectar todos los joints de la viga
        Dim allJoints As New List(Of cJoint)
        For Each frame In viga.Frames
            Dim ji, jj As cJoint
            If joints.TryGetValue(frame.JointI, ji) Then allJoints.Add(ji)
            If joints.TryGetValue(frame.JointJ, jj) Then allJoints.Add(jj)
        Next
        If allJoints.Count = 0 Then Exit Sub

        ' Coordenada transversal promedio de la viga (perpendicular a su dirección de viaje)
        Dim coordProm As Double = If(esX,
            allJoints.Average(Function(j) j.GlobalY),
            allJoints.Average(Function(j) j.GlobalX))

        ' Centro geométrico de la viga (para medir distancia a grids General)
        Dim cx As Double = allJoints.Average(Function(j) j.GlobalX)
        Dim cy As Double = allJoints.Average(Function(j) j.GlobalY)

        Dim mejor As String = ""
        Dim menorDist As Double = Double.MaxValue

        For Each gl In grids
            If String.IsNullOrWhiteSpace(gl.GridID) Then Continue For

            Dim dist As Double
            If gl.EsTipoGeneral Then
                ' Grid General: solo considerar si su dirección es casi paralela a la viga
                Dim gdx = gl.X2 - gl.X1
                Dim gdy = gl.Y2 - gl.Y1
                Dim glen = Math.Sqrt(gdx * gdx + gdy * gdy)
                If glen < 0.001 Then Continue For
                gdx /= glen : gdy /= glen
                ' Producto punto con dirección viga (cos del ángulo)
                Dim dot = Math.Abs(gdx * viga.Direccion.X + gdy * viga.Direccion.Y)
                If dot < 0.866 Then Continue For  ' > 30° de diferencia → ignorar
                dist = DistanciaPuntoALinea(cx, cy, gl.X1, gl.Y1, gl.X2, gl.Y2)
            ElseIf gl.Direction = dirParalelo Then
                dist = Math.Abs(coordProm - gl.Ordinate)
            Else
                Continue For
            End If

            If dist < menorDist Then
                menorDist = dist
                mejor = gl.GridID
            End If
        Next

        viga.EjeParalelo = If(menorDist <= tolMax, mejor, "")
    End Sub

    ''' Asigna EjeParalelo a todas las vigas de la lista.
    Public Sub AsignarEjesParalelosAVigas(vigas As List(Of cViga),
                                           grids As List(Of cGridLine),
                                           joints As Dictionary(Of String, cJoint),
                                           Optional tolMax As Double = 1.0)

        If vigas Is Nothing OrElse grids Is Nothing Then Exit Sub
        For Each v In vigas
            AsignarEjeParaleloAViga(v, grids, joints, tolMax)
        Next

    End Sub

    ''' Asigna EjeApoyo_I / EjeApoyo_D a cada tramo de un nervio buscando el grid
    ''' perpendicular más cercano al joint correspondiente dentro de la tolerancia.
    ''' Soporta X Cartesian, Y Cartesian y General Cartesian.
    Public Sub AsignarEjesANervio(nervio As cNervio,
                                  grids As List(Of cGridLine),
                                  joints As Dictionary(Of String, cJoint),
                                  Optional tolMax As Double = 0.5)

        If grids Is Nothing OrElse grids.Count = 0 Then Exit Sub
        If nervio.Frames Is Nothing OrElse nervio.Frames.Count = 0 Then Exit Sub

        Dim dir = PuntoJoint(nervio.Frames(0).JointJ, joints) - PuntoJoint(nervio.Frames(0).JointI, joints)
        dir.Normalize()

        Dim esX As Boolean = Math.Abs(dir.X) >= Math.Abs(dir.Y)
        Dim dirBuscar As String = If(esX, "X", "Y")

        Dim gridsPerp = grids.Where(Function(g) Not String.IsNullOrWhiteSpace(g.GridID) AndAlso
                                                (g.Direction = dirBuscar OrElse g.EsTipoGeneral)).ToList()
        If gridsPerp.Count = 0 Then Exit Sub

        For Each fn In nervio.Frames
            fn.EjeApoyo_I = BuscarEjeMasCercano(fn.JointI, gridsPerp, joints, esX, tolMax)
            fn.EjeApoyo_D = BuscarEjeMasCercano(fn.JointJ, gridsPerp, joints, esX, tolMax)
        Next

    End Sub

    ''' Asigna ejes estructurales a todos los nervios de la lista.
    Public Sub AsignarEjesANervios(nervios As List(Of cNervio),
                                   grids As List(Of cGridLine),
                                   joints As Dictionary(Of String, cJoint),
                                   Optional tolMax As Double = 0.5)

        If nervios Is Nothing OrElse grids Is Nothing Then Exit Sub
        For Each n In nervios
            AsignarEjesANervio(n, grids, joints, tolMax)
        Next

    End Sub


End Class
