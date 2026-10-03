Imports System.Reflection
Imports System.Text
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.TestTools.UnitTesting

' Instancia todos los Form del ensamblado ARCO para cazar excepciones en
' el constructor: Designer sin New, Handles que corren dentro de
' InitializeComponent tocando listas Nothing, resx con nombre desalineado
' del tipo, etc. CLAUDE.md documenta que dos bugs de este tipo llegaron a
' producción el 2026-09-15 y no los cazó la compilación — este test sí.
' No hace aserciones de negocio, solo verifica que New() no reviente.
<TestClass>
Public Class SmokeFormsTests

    <TestMethod>
    Public Sub TodosLosFormsSeInstancianSinExcepcion()
        Dim asm = GetType(ARCO.Proyecto).Assembly
        Dim forms = asm.GetTypes().
            Where(Function(t) GetType(Form).IsAssignableFrom(t) AndAlso Not t.IsAbstract AndAlso
                              t.GetConstructor(Type.EmptyTypes) IsNot Nothing).
            OrderBy(Function(t) t.FullName).
            ToList()

        Dim fallos As New StringBuilder()
        Dim okCount As Integer = 0
        For Each t In forms
            Try
                Dim f As Form = CType(Activator.CreateInstance(t), Form)
                f.Dispose()
                okCount += 1
            Catch ex As Exception
                Dim inner = ex
                While inner.InnerException IsNot Nothing
                    inner = inner.InnerException
                End While
                fallos.AppendLine($"[{inner.GetType().Name}] {t.FullName}")
                fallos.AppendLine($"  {inner.Message}")
                If inner.StackTrace IsNot Nothing Then
                    Dim primera = inner.StackTrace.Split(New Char() {ControlChars.Lf}, StringSplitOptions.None).First().Trim()
                    fallos.AppendLine($"  {primera}")
                End If
            End Try
        Next

        Console.WriteLine($"Total: {forms.Count}   OK: {okCount}   FAIL: {forms.Count - okCount}")
        If fallos.Length > 0 Then
            Console.WriteLine(fallos.ToString())
            Assert.Fail($"{forms.Count - okCount} formularios fallan en el constructor. Ver detalles arriba.")
        End If
    End Sub

    ' Reportes globales toman Proyecto. Son los más críticos: el usuario los
    ' abre desde el menú principal con un proyecto vacío/importado-pero-sin-
    ' procesar, y cualquier iteración ciega sobre Lista_Muros/Columnas/etc.
    ' revienta. Si estos cuatro pasan con Proyecto() vacío, el flujo "importar
    ' y abrir reporte antes de procesar" ya no mata la app.
    <TestMethod>
    Public Sub ReportesGlobales_SeAbrenConProyectoVacio()
        Dim fallos As New StringBuilder()
        Dim tipos = New String() {
            "ARCO.Form_Reporte_Zapatas",
            "ARCO.Form_Reporte_Revision",
            "ARCO.Form_Reporte_Proyecto_Completo",
            "ARCO.Form_InformeEstadoMuros"
        }

        Dim asm = GetType(ARCO.Proyecto).Assembly
        For Each nombre In tipos
            Dim t = asm.GetType(nombre)
            Assert.IsNotNull(t, $"Tipo no encontrado: {nombre}")
            Try
                Dim f As Form = CType(Activator.CreateInstance(t, New ARCO.Proyecto()), Form)
                f.Dispose()
            Catch ex As Exception
                Dim inner = ex
                While inner.InnerException IsNot Nothing
                    inner = inner.InnerException
                End While
                fallos.AppendLine($"[{inner.GetType().Name}] {nombre}")
                fallos.AppendLine($"  {inner.Message}")
                If inner.StackTrace IsNot Nothing Then
                    Dim primera = inner.StackTrace.Split(New Char() {ControlChars.Lf}, StringSplitOptions.None).First().Trim()
                    fallos.AppendLine($"  {primera}")
                End If
            End Try
        Next

        If fallos.Length > 0 Then
            Console.WriteLine(fallos.ToString())
            Assert.Fail("Reportes globales fallan con Proyecto() vacío. Ver detalles arriba.")
        End If
    End Sub

    ' Reporta — sin fallar — qué Form del ensamblado NO quedan cubiertos por
    ' el smoke paramless. Diálogos modales que toman args específicos (listas,
    ' cNervio, GrupoReplicaViga, etc.) solo pueden probarse con datos reales.
    ' Si un Form nuevo aparece acá, al menos nos enteramos.
    <TestMethod>
    Public Sub Inventario_FormsNoCubiertosPorElSmoke()
        Dim asm = GetType(ARCO.Proyecto).Assembly
        Dim sinParamless = asm.GetTypes().
            Where(Function(t) GetType(Form).IsAssignableFrom(t) AndAlso Not t.IsAbstract AndAlso
                              t.GetConstructor(Type.EmptyTypes) Is Nothing).
            OrderBy(Function(t) t.FullName).
            ToList()

        Dim lista As New StringBuilder()
        lista.AppendLine($"Forms sin constructor paramless (no cubiertos por smoke): {sinParamless.Count}")
        For Each t In sinParamless
            Dim ctor = t.GetConstructors().FirstOrDefault()
            Dim sig = If(ctor Is Nothing, "(sin ctor público)",
                "(" & String.Join(", ", ctor.GetParameters().Select(Function(p) p.ParameterType.Name)) & ")")
            lista.AppendLine($"  • {t.Name}{sig}")
        Next
        Console.WriteLine(lista.ToString())
    End Sub

End Class
