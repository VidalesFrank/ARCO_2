Imports System.IO

Public Class Form_Documentacion
    Private _docRoot As String

    Public Sub New()
        InitializeComponent()
        _docRoot = Path.Combine(Application.StartupPath, "Documentacion")
        Me.Icon = My.Resources.Resources.ARCO_ICONOGRIS
    End Sub

    Private Sub Form_Documentacion_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CargarArbol()
        NavegaA("index.html")
    End Sub

    Private Sub CargarArbol()
        TreeView1.Nodes.Clear()
        TreeView1.ShowLines = True
        TreeView1.ShowPlusMinus = True
        TreeView1.Font = New Font("Segoe UI", 9)

        Dim nManual As New TreeNode("Manual de Usuario")
        nManual.Nodes.Add(New TreeNode("Introduccion") With {.Tag = "manual/01_introduccion.html"})
        nManual.Nodes.Add(New TreeNode("Importacion desde ETABS") With {.Tag = "manual/02_importacion_etabs.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Vigas") With {.Tag = "manual/03_vigas.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Columnas") With {.Tag = "manual/04_columnas.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Muros") With {.Tag = "manual/05_muros.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Pilas") With {.Tag = "manual/06_pilas.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Nervios") With {.Tag = "manual/07_nervios.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Losas") With {.Tag = "manual/08_losas.html"})
        nManual.Nodes.Add(New TreeNode("Modulo Escaleras") With {.Tag = "manual/09_escaleras.html"})
        nManual.Nodes.Add(New TreeNode("Muros No Estructurales") With {.Tag = "manual/10_muros_no_estructurales.html"})

        Dim nCrit As New TreeNode("Criterios de Diseno")
        nCrit.Nodes.Add(New TreeNode("Vigas — Flexion y Cortante") With {.Tag = "criterios/01_vigas.html"})
        nCrit.Nodes.Add(New TreeNode("Columnas — Flexo-compresion Biaxial") With {.Tag = "criterios/02_columnas.html"})
        nCrit.Nodes.Add(New TreeNode("Muros — Diseno y EB") With {.Tag = "criterios/03_muros.html"})
        nCrit.Nodes.Add(New TreeNode("Pilas — DI y Cortante") With {.Tag = "criterios/04_pilas.html"})
        nCrit.Nodes.Add(New TreeNode("Nervios — Flexion y Cortante") With {.Tag = "criterios/05_nervios.html"})
        nCrit.Nodes.Add(New TreeNode("Losas — Metodo Coeficientes") With {.Tag = "criterios/06_losas.html"})
        nCrit.Nodes.Add(New TreeNode("Escaleras — Deflexiones") With {.Tag = "criterios/07_escaleras.html"})
        nCrit.Nodes.Add(New TreeNode("Muros No Estructurales") With {.Tag = "criterios/08_muros_no_estructurales.html"})

        TreeView1.Nodes.Add(nManual)
        TreeView1.Nodes.Add(nCrit)
        nManual.Expand()
        nCrit.Expand()
    End Sub

    Private Sub TreeView1_NodeMouseClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseClick
        If e.Node.Tag IsNot Nothing Then
            NavegaA(e.Node.Tag.ToString())
        End If
    End Sub

    Private Sub NavegaA(ruta As String)
        Dim fullPath As String = Path.Combine(_docRoot, ruta)
        If File.Exists(fullPath) Then
            WebBrowser1.Navigate(New Uri(fullPath))
        Else
            WebBrowser1.DocumentText = $"<html><body style='font-family:Segoe UI;padding:30px;color:#555'><h3>Archivo no encontrado</h3><p>{fullPath}</p></body></html>"
        End If
    End Sub

    Private Sub BtnInicio_Click(sender As Object, e As EventArgs) Handles BtnInicio.Click
        NavegaA("index.html")
    End Sub

    Private Sub BtnAtras_Click(sender As Object, e As EventArgs) Handles BtnAtras.Click
        If WebBrowser1.CanGoBack Then WebBrowser1.GoBack()
    End Sub

    Private Sub BtnAdelante_Click(sender As Object, e As EventArgs) Handles BtnAdelante.Click
        If WebBrowser1.CanGoForward Then WebBrowser1.GoForward()
    End Sub

    Private Sub WebBrowser1_Navigated(sender As Object, e As WebBrowserNavigatedEventArgs) Handles WebBrowser1.Navigated
        BtnAtras.Enabled = WebBrowser1.CanGoBack
        BtnAdelante.Enabled = WebBrowser1.CanGoForward
    End Sub
End Class
