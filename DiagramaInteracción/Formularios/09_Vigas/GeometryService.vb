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


End Class
