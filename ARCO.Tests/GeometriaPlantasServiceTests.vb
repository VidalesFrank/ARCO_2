Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports ARCO

''' <summary>
''' GeometriaPlantasService — cómo se resuelve la posición X,Y de cada pila
''' en el plano de cimentación a partir de los nodos importados.
'''
''' Lo mismo que para zapatas (ver ZapataUbicacionTests): se cruza por
''' ObjectLabel y, para pilas con el mismo label repetido en varios pisos,
''' se usa el nodo de MENOR Z (cimentación).
''' </summary>
<TestClass>
Public Class GeometriaPlantasServiceTests

    Private Shared Function Joint(objectLabel As String, elementLabel As String,
                                  x As Double, y As Double, z As Double) As cJoint
        Return New cJoint With {
            .ObjectType = "Joint",
            .ObjectLabel = objectLabel,
            .ElementLabel = elementLabel,
            .GlobalX = x,
            .GlobalY = y,
            .GlobalZ = z
        }
    End Function

    Private Shared Function Pila(label As String) As Elemento_Pila
        Return New Elemento_Pila With {
            .Name_Label = label,
            .Name_Elemento = label
        }
    End Function

    ' =====================================================================
    ' ResolverPilas: enlace por ObjectLabel
    ' =====================================================================

    <TestMethod>
    Public Sub ResolverPilas_LabelCoincideConObjectLabel_SeteaCoordenadas()
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("P1"))

        Dim joints = New List(Of cJoint) From {
            Joint("P1", "7", 10.0, 20.0, 0.0)
        }

        Dim n = GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(1, n)
        Assert.IsTrue(pilas.ListaElementos(0).TieneCoordenadas)
        Assert.AreEqual(10.0F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(20.0F, pilas.ListaElementos(0).CoordY)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_SinJointCoincidente_NoResuelveYNoRompe()
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("P9"))

        Dim joints = New List(Of cJoint) From {
            Joint("P1", "7", 10.0, 20.0, 0.0)
        }

        Dim n = GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(0, n)
        Assert.IsFalse(pilas.ListaElementos(0).TieneCoordenadas)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_LabelRepetidoEnVariosPisos_TomaElDeMenorZ()
        ' Una pila "P1" cuyo label aparece en 3 pisos — se queda con el nodo de
        ' cimentación (menor Z), que es el que define la posición de la pila.
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("P1"))

        Dim joints = New List(Of cJoint) From {
            Joint("P1", "a", 10.0, 20.0, 10.0),
            Joint("P1", "b", 11.0, 22.0, -5.0),
            Joint("P1", "c", 15.0, 25.0, 5.0)
        }

        GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(11.0F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(22.0F, pilas.ListaElementos(0).CoordY)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_FallbackElementLabelCuandoObjectLabelNoCruza()
        ' Si no cruza por Object Label se intenta por Element Label — cubre
        ' modelos (E17) donde las dos numeraciones no corren parejas.
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("E7"))

        Dim joints = New List(Of cJoint) From {
            Joint("P1", "E7", 15.0, 25.0, 0.0)
        }

        Dim n = GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(1, n)
        Assert.AreEqual(15.0F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(25.0F, pilas.ListaElementos(0).CoordY)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_PilaYaUbicada_NoLaRepisa()
        ' Idempotencia: si una pila ya tiene coord explícita (p.ej. seteada a
        ' mano en una futura UI) se preserva aunque el joint diga otra cosa.
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(New Elemento_Pila With {
            .Name_Label = "P1",
            .CoordX = 100.0F,
            .CoordY = 200.0F,
            .TieneCoordenadas = True
        })

        Dim joints = New List(Of cJoint) From {
            Joint("P1", "7", 10.0, 20.0, 0.0)
        }

        GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(100.0F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(200.0F, pilas.ListaElementos(0).CoordY)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_ListasNullOVacias_NoRompe()
        Assert.AreEqual(0, GeometriaPlantasService.ResolverPilas(Nothing, Nothing))

        Dim pilasVacias As New cPilas()
        Assert.AreEqual(0, GeometriaPlantasService.ResolverPilas(pilasVacias, Nothing))

        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("P1"))
        ' Sin joints Y sin CoordsPorLabel = no resuelve pero no rompe.
        Assert.AreEqual(0, GeometriaPlantasService.ResolverPilas(pilas, New List(Of cJoint)()))
    End Sub

    ' =====================================================================
    ' Fallback por cPilas.CoordsPorLabel — para pilas Pier
    ' =====================================================================
    '
    ' Para pilas Pier, Name_Label = pierName (no es un ObjectLabel de nodo),
    ' entonces el cruce por Joints no las encuentra. El fallback usa
    ' el diccionario poblado al confirmar la importación.

    <TestMethod>
    Public Sub ResolverPilas_PierUsaCoordsPorLabelCuandoNoCruzaConJoints()
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("W1"))   ' label típico de pier shell
        pilas.CoordsPorLabel = New Dictionary(Of String, System.Drawing.PointF)(StringComparer.OrdinalIgnoreCase) From {
            {"W1", New System.Drawing.PointF(5.5F, 3.25F)}
        }

        ' Hay joints pero ninguno con ObjectLabel "W1" (los piers no están en Joints).
        Dim joints = New List(Of cJoint) From {
            Joint("P1", "7", 10.0, 20.0, 0.0)
        }

        Dim n = GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(1, n)
        Assert.IsTrue(pilas.ListaElementos(0).TieneCoordenadas)
        Assert.AreEqual(5.5F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(3.25F, pilas.ListaElementos(0).CoordY)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_FrameTomaJointAunCuandoHayCoordsPorLabel()
        ' Si una Frame tiene su joint, Y además está en el diccionario, gana
        ' el cruce por joint (es la fuente autoritativa, viene del nodo de
        ' cimentación — menor Z — que es la coordenada estructural correcta).
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("P1"))
        pilas.CoordsPorLabel = New Dictionary(Of String, System.Drawing.PointF)(StringComparer.OrdinalIgnoreCase) From {
            {"P1", New System.Drawing.PointF(999.0F, 999.0F)}
        }

        Dim joints = New List(Of cJoint) From {
            Joint("P1", "7", 10.0, 20.0, 0.0)
        }

        GeometriaPlantasService.ResolverPilas(pilas, joints)

        Assert.AreEqual(10.0F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(20.0F, pilas.ListaElementos(0).CoordY)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_SinJointsPeroConCoordsPorLabel_ResuelveTodas()
        ' Un proyecto que no importó la hoja de joints igual puede dibujar la
        ' planta si el diccionario del import está disponible — vale para
        ' Frame y Pier por igual.
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("P1"))
        pilas.ListaElementos.Add(Pila("W1"))
        pilas.CoordsPorLabel = New Dictionary(Of String, System.Drawing.PointF)(StringComparer.OrdinalIgnoreCase) From {
            {"P1", New System.Drawing.PointF(1, 2)},
            {"W1", New System.Drawing.PointF(3, 4)}
        }

        Dim n = GeometriaPlantasService.ResolverPilas(pilas, New List(Of cJoint)())

        Assert.AreEqual(2, n)
        Assert.AreEqual(1.0F, pilas.ListaElementos(0).CoordX)
        Assert.AreEqual(3.0F, pilas.ListaElementos(1).CoordX)
    End Sub

    <TestMethod>
    Public Sub ResolverPilas_CoordsPorLabelDiccionarioCaseInsensitive()
        ' Al construir el diccionario se usa OrdinalIgnoreCase. Confirmamos
        ' que el lookup resuelve aunque difiera en mayúsculas.
        Dim pilas As New cPilas()
        pilas.ListaElementos.Add(Pila("w1"))
        pilas.CoordsPorLabel = New Dictionary(Of String, System.Drawing.PointF)(StringComparer.OrdinalIgnoreCase) From {
            {"W1", New System.Drawing.PointF(7, 8)}
        }

        GeometriaPlantasService.ResolverPilas(pilas, New List(Of cJoint)())

        Assert.IsTrue(pilas.ListaElementos(0).TieneCoordenadas)
        Assert.AreEqual(7.0F, pilas.ListaElementos(0).CoordX)
    End Sub

    ' =====================================================================
    ' ResolverYValidar: reporte completo
    ' =====================================================================

    <TestMethod>
    Public Sub ResolverYValidar_SinJointsImportados_ReportaFaltantesConMensaje()
        Dim p As New Proyecto()
        p.Elementos.Pilas.ListaElementos.Add(Pila("P1"))

        Dim rep = GeometriaPlantasService.ResolverYValidar(p)

        Assert.IsTrue(rep.HayFaltantes)
        Assert.AreEqual(1, rep.PilasSinCoord)
        Assert.AreEqual(0, rep.PilasConCoord)
        Assert.AreEqual(0, rep.JointsImportados)
        Assert.IsFalse(rep.PlantaPilasListaParaDibujar)
        Assert.IsTrue(rep.MensajePara("pilas").Contains("nodos"))
    End Sub

    <TestMethod>
    Public Sub ResolverYValidar_PilaConJoint_ReportaResueltas()
        Dim p As New Proyecto()
        p.Elementos.Pilas.ListaElementos.Add(Pila("P1"))
        p.Elementos.Pilas.ListaElementos.Add(Pila("P2"))
        p.Elementos.Joints.Add(Joint("P1", "7", 10.0, 20.0, 0.0))

        Dim rep = GeometriaPlantasService.ResolverYValidar(p)

        Assert.AreEqual(1, rep.PilasConCoord)
        Assert.AreEqual(1, rep.PilasSinCoord)
        Assert.IsTrue(rep.PlantaPilasListaParaDibujar)
        Assert.IsTrue(rep.HayFaltantes) ' porque P2 no se resolvió
    End Sub

    <TestMethod>
    Public Sub ResolverYValidar_SinModulos_NoRompeNiMarcaFaltantes()
        Dim p As New Proyecto()
        Dim rep = GeometriaPlantasService.ResolverYValidar(p)

        Assert.IsFalse(rep.HayFaltantes)
        Assert.AreEqual(0, rep.PilasConCoord)
        Assert.AreEqual(0, rep.ColumnasConCoord)
        Assert.AreEqual(0, rep.MurosConCoord)
    End Sub

    <TestMethod>
    Public Sub ResolverYValidar_ProyectoNull_DevuelveReporteVacio()
        Dim rep = GeometriaPlantasService.ResolverYValidar(Nothing)
        Assert.IsNotNull(rep)
        Assert.IsFalse(rep.HayFaltantes)
    End Sub

End Class
