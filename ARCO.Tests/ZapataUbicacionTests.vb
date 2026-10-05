Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Collections.Generic
Imports System.Data
Imports ARCO

''' <summary>
''' De qué nodo saca cada zapata su X,Y para la vista en planta.
'''
''' El bug: Form_07_Pag_Zapatas emparejaba el Label de "Joint Reactions" contra
''' cJoint.ElementLabel, que viene de la columna "Element Name" — el nombre
''' único del ELEMENTO. La etiqueta que referencia "Joint Reactions" es
''' "Object Label". En modelos donde las dos numeraciones corren parejas el
''' error no se nota; en cuanto divergen, unas pocas zapatas toman la coordenada
''' de otro nodo y en la planta aparecen encima de sus vecinas.
'''
''' El importador de Pilas ya lo tenía bien y hasta documentado
''' (Funciones_00_Varias, diccionarios byLabel / byElem).
''' </summary>
<TestClass>
Public Class ZapataUbicacionTests

    Private Shared Function Joint(story As String, objectLabel As String, elementName As String,
                                  x As Double, y As Double, z As Double) As cJoint
        Return New cJoint With {
            .Story = story,
            .ObjectType = "Joint",
            .ObjectLabel = objectLabel,
            .ElementLabel = elementName,
            .GlobalX = x,
            .GlobalY = y,
            .GlobalZ = z
        }
    End Function

    Private Shared Function Zapata(label As String, Optional nombre As String = "") As cZapata
        Return New cZapata With {
            .Label_joint = label,
            .Nombre = If(nombre = "", "Z-" & label, nombre),
            .L_b = 2.0,
            .L_h = 2.0
        }
    End Function

    ' =====================================================================
    ' El vínculo correcto: Object Label
    ' =====================================================================

    ''' <summary>
    ''' Caso mínimo que reproduce el bug. El nodo de la esquina lleva
    ''' Object Label = "4" y Element Name = "9"; otro nodo, en el centro, lleva
    ''' Element Name = "4". Buscando por Element Name la zapata 4 se iba al
    ''' centro, encima de la zapata que de verdad vive ahí.
    ''' </summary>
    <TestMethod>
    Public Sub UbicarZapatas_ResuelvePorObjectLabelNoPorElementName()
        Dim joints As New List(Of cJoint) From {
            Joint("Base", "4", "9", 0.0, 0.0, 0.0),
            Joint("Base", "7", "4", 5.0, 5.0, 0.0)
        }
        Dim zapatas As New List(Of cZapata) From {Zapata("4")}

        ZapataService.UbicarZapatas(zapatas, joints)

        Assert.IsTrue(zapatas(0).TieneCoordenadas)
        Assert.AreEqual(0.0, zapatas(0).CoordX, 0.0001, "debe tomar el nodo cuyo Object Label es 4")
        Assert.AreEqual(0.0, zapatas(0).CoordY, 0.0001)
    End Sub

    ''' <summary>
    ''' La etiqueta del nodo se repite en todos los pisos. La zapata va en la
    ''' cimentación, así que gana el nodo de menor Z.
    ''' </summary>
    <TestMethod>
    Public Sub UbicarZapatas_TomaElNodoDeMenorZ()
        Dim joints As New List(Of cJoint) From {
            Joint("Story2", "12", "40", 99.0, 99.0, 6.0),
            Joint("Story1", "12", "25", 88.0, 88.0, 3.0),
            Joint("Base", "12", "12", 3.5, 7.2, 0.0)
        }
        Dim zapatas As New List(Of cZapata) From {Zapata("12")}

        ZapataService.UbicarZapatas(zapatas, joints)

        Assert.AreEqual(3.5, zapatas(0).CoordX, 0.0001)
        Assert.AreEqual(7.2, zapatas(0).CoordY, 0.0001)
    End Sub

    ''' <summary>
    ''' Respaldo: si la hoja no trae Object Label para esa etiqueta, se intenta
    ''' por Element Name. Cubre los archivos donde las dos numeraciones
    ''' coinciden y el formato E17.
    ''' </summary>
    <TestMethod>
    Public Sub UbicarZapatas_CaeEnElementNameSiNoHayObjectLabel()
        Dim joints As New List(Of cJoint) From {
            Joint("Base", "", "77", 10.0, 20.0, 0.0)
        }
        Dim zapatas As New List(Of cZapata) From {Zapata("77")}

        ZapataService.UbicarZapatas(zapatas, joints)

        Assert.IsTrue(zapatas(0).TieneCoordenadas)
        Assert.AreEqual(10.0, zapatas(0).CoordX, 0.0001)
    End Sub

    ''' <summary>Un apoyo sin nodo no se ubica y queda reportado.</summary>
    <TestMethod>
    Public Sub UbicarZapatas_SinNodoQuedaSinCoordenadasYSeReporta()
        Dim joints As New List(Of cJoint) From {Joint("Base", "1", "1", 0.0, 0.0, 0.0)}
        Dim zapatas As New List(Of cZapata) From {Zapata("1"), Zapata("99")}

        Dim res = ZapataService.UbicarZapatas(zapatas, joints)

        Assert.AreEqual(1, res.Ubicadas)
        Assert.IsFalse(zapatas(1).TieneCoordenadas)
        CollectionAssert.Contains(res.SinNodo, "99")
        Assert.IsTrue(res.HayProblemas)
    End Sub

    ''' <summary>
    ''' Volver a ubicar con una hoja de nodos que ya no trae esa etiqueta tiene
    ''' que BORRAR la marca, no dejar la coordenada del cálculo anterior.
    ''' </summary>
    <TestMethod>
    Public Sub UbicarZapatas_LimpiaLaMarcaVieja()
        Dim z = Zapata("50")
        z.CoordX = 1.0 : z.CoordY = 2.0 : z.TieneCoordenadas = True
        Dim zapatas As New List(Of cZapata) From {z}

        Dim res = ZapataService.UbicarZapatas(zapatas, New List(Of cJoint)())

        Assert.IsFalse(z.TieneCoordenadas)
        Assert.AreEqual(0, res.Ubicadas)
    End Sub

    ' =====================================================================
    ' Superposición — el síntoma visible
    ' =====================================================================

    ''' <summary>
    ''' Dos apoyos del modelo no pueden compartir coordenada. Cuando pasa, hay
    ''' que decirlo: es la señal de que una etiqueta se resolvió mal.
    ''' </summary>
    <TestMethod>
    Public Sub DetectarSuperpuestas_AvisaDeLasQueCompartenPunto()
        Dim a = Zapata("1", "Z1") : a.CoordX = 3.0 : a.CoordY = 4.0 : a.TieneCoordenadas = True
        Dim b = Zapata("2", "Z2") : b.CoordX = 3.0 : b.CoordY = 4.0 : b.TieneCoordenadas = True
        Dim c = Zapata("3", "Z3") : c.CoordX = 9.0 : c.CoordY = 4.0 : c.TieneCoordenadas = True

        Dim avisos = ZapataService.DetectarSuperpuestas(New List(Of cZapata) From {a, b, c})

        Assert.AreEqual(1, avisos.Count, "un solo grupo superpuesto")
        Assert.IsTrue(avisos(0).Contains("Z1"), avisos(0))
        Assert.IsTrue(avisos(0).Contains("Z2"), avisos(0))
        Assert.IsFalse(avisos(0).Contains("Z3"), avisos(0))
    End Sub

    ''' <summary>Una planta sana no genera avisos.</summary>
    <TestMethod>
    Public Sub DetectarSuperpuestas_PlantaSanaNoAvisa()
        Dim a = Zapata("1") : a.CoordX = 0.0 : a.CoordY = 0.0 : a.TieneCoordenadas = True
        Dim b = Zapata("2") : b.CoordX = 5.0 : b.CoordY = 0.0 : b.TieneCoordenadas = True

        Assert.AreEqual(0, ZapataService.DetectarSuperpuestas(New List(Of cZapata) From {a, b}).Count)
    End Sub

    ''' <summary>
    ''' Las que no tienen coordenadas no cuentan como superpuestas: todas
    ''' estarían en (0,0) y el aviso sería ruido.
    ''' </summary>
    <TestMethod>
    Public Sub DetectarSuperpuestas_IgnoraLasNoUbicadas()
        Dim a = Zapata("1") : a.TieneCoordenadas = False
        Dim b = Zapata("2") : b.TieneCoordenadas = False

        Assert.AreEqual(0, ZapataService.DetectarSuperpuestas(New List(Of cZapata) From {a, b}).Count)
    End Sub

    ''' <summary>
    ''' El bug completo, de punta a punta: cuatro apoyos cuyas numeraciones de
    ''' label y de element name están cruzadas. Resolviendo por Object Label
    ''' cada uno cae en su sitio y no hay superpuestas.
    ''' </summary>
    <TestMethod>
    Public Sub UbicarZapatas_CuatroApoyosConNumeracionCruzada()
        ' Object Label 1..4 en las cuatro esquinas; Element Name rotado.
        Dim joints As New List(Of cJoint) From {
            Joint("Base", "1", "3", 0.0, 0.0, 0.0),
            Joint("Base", "2", "4", 6.0, 0.0, 0.0),
            Joint("Base", "3", "1", 0.0, 6.0, 0.0),
            Joint("Base", "4", "2", 6.0, 6.0, 0.0)
        }
        Dim zapatas As New List(Of cZapata) From {
            Zapata("1"), Zapata("2"), Zapata("3"), Zapata("4")
        }

        Dim res = ZapataService.UbicarZapatas(zapatas, joints)

        Assert.AreEqual(4, res.Ubicadas)
        Assert.AreEqual(0, res.Superpuestas.Count, "ninguna debe caer encima de otra")
        Assert.AreEqual(0.0, zapatas(0).CoordY, 0.0001)
        Assert.AreEqual(6.0, zapatas(2).CoordY, 0.0001, "la 3 va arriba, no abajo")
    End Sub

    ' =====================================================================
    ' Lectura de la hoja de nodos
    ' =====================================================================

    Private Shared Function TablaE23() As DataTable
        Dim dt As New DataTable()
        dt.Columns.Add("Story")
        dt.Columns.Add("Object Type")
        dt.Columns.Add("Object Label")
        dt.Columns.Add("Element Name")
        dt.Columns.Add("Global X")
        dt.Columns.Add("Global Y")
        dt.Columns.Add("Global Z")
        dt.Rows.Add("Base", "Joint", "4", "9", "1.5", "2.5", "0")
        dt.Rows.Add("Base", "Frame", "5", "10", "9.9", "9.9", "0")
        Return dt
    End Function

    <TestMethod>
    Public Sub DataTableToJoints_E23_LeeLabelYNombreDeElementoPorSeparado()
        Dim joints = Funciones_00_Varias.DataTableToJoints(TablaE23())

        Assert.AreEqual(1, joints.Count, "solo las filas de tipo Joint")
        Assert.AreEqual("4", joints(0).ObjectLabel)
        Assert.AreEqual("9", joints(0).ElementLabel)
        Assert.AreEqual(1.5, joints(0).GlobalX, 0.0001)
    End Sub

    ''' <summary>
    ''' "Joint Coordinates" de E17 no trae "Object Type" ni "Global X": antes la
    ''' función descartaba todas las filas y devolvía una lista vacía, así que
    ''' un archivo E17 no tenía planta ni clasificación de apoyos.
    ''' </summary>
    <TestMethod>
    Public Sub DataTableToJoints_E17_TambienDevuelveNodos()
        Dim dt As New DataTable()
        dt.Columns.Add("Story")
        dt.Columns.Add("Label")
        dt.Columns.Add("Unique Name")
        dt.Columns.Add("X")
        dt.Columns.Add("Y")
        dt.Columns.Add("Z")
        dt.Rows.Add("Base", "4", "9", "1.5", "2.5", "0")

        Dim joints = Funciones_00_Varias.DataTableToJoints(dt)

        Assert.AreEqual(1, joints.Count, "E17 devolvía lista vacía")
        Assert.AreEqual("4", joints(0).ObjectLabel, "Label es la etiqueta del nodo")
        Assert.AreEqual(1.5, joints(0).GlobalX, 0.0001)
        Assert.AreEqual(2.5, joints(0).GlobalY, 0.0001)
    End Sub

    ''' <summary>
    ''' Un E23 leído y ubicado en una sola corrida: es la ruta real del
    ''' formulario, hoja de Excel incluida.
    ''' </summary>
    <TestMethod>
    Public Sub Integracion_DesdeLaHojaHastaLaCoordenada()
        Dim joints = Funciones_00_Varias.DataTableToJoints(TablaE23())
        Dim zapatas As New List(Of cZapata) From {Zapata("4")}

        Dim res = ZapataService.UbicarZapatas(zapatas, joints)

        Assert.AreEqual(1, res.Ubicadas)
        Assert.IsFalse(res.HayProblemas)
        Assert.AreEqual(1.5, zapatas(0).CoordX, 0.0001)
        Assert.AreEqual(2.5, zapatas(0).CoordY, 0.0001)
    End Sub

End Class
