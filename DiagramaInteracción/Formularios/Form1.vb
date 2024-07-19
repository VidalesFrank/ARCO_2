Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
'Imports FCB_Etabs.Funciones
'Imports FCB_Etabs.Elementos
Imports System.Windows.Forms

Public Class Form1
    'Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

    '    Dim Open As New OpenFileDialog
    '    Open.Filter = "Etabs|*.e2k"
    '    Open.ShowDialog()

    '    'Dim ArchivoE2k As List(Of String) = cFuncionesImportacion.CargarArchivoTXT(Open.FileName)
    '    'Dim datosEtabs As cDatosEtabs = cFuncionesImportacion.CrearObjetosEtabs(ArchivoE2k)


    '    'PictureBox1.Refresh()
    '    '
    '    Dim PictureBox5 = PictureBox1
    '    'AddHandler PictureBox5.Paint, AddressOf Me.PictureBox5_Paint

    '    Dim g As Graphics = PictureBox1.CreateGraphics

    '    Dim Esc As Double

    '    Dim B As Single = datosEtabs.Lista_Points(0).X
    '    Dim H As Single = datosEtabs.Lista_Points(0).Y

    '    For i = 0 To datosEtabs.Lista_Points.Count - 1
    '        If B < Math.Abs(datosEtabs.Lista_Points(i).X) Then
    '            B = Math.Abs(datosEtabs.Lista_Points(i).X)
    '        End If
    '        If H < Math.Abs(datosEtabs.Lista_Points(i).Y) Then
    '            H = Math.Abs(datosEtabs.Lista_Points(i).Y)
    '        End If
    '    Next


    '    If B > H Then
    '        Esc = (Math.Min(PictureBox5.Width, PictureBox5.Height) - 40) / B
    '    Else
    '        Esc = (Math.Min(PictureBox5.Width, PictureBox5.Height) - 40) / H
    '    End If

    '    For i = 0 To datosEtabs.Lista_Points_Piso.Count - 1
    '        Dim s = i
    '        If datosEtabs.Lista_Points_Piso(i).Piso = "Base" Then
    '            Dim punto = datosEtabs.Lista_Points.Find(Function(p) p.Nombre = datosEtabs.Lista_Points_Piso(s).Nombre)
    '            Dim D As Double = 1
    '            Dim Dbb As Integer = D * Esc
    '            Dim Cxx As Integer = punto.X * Esc
    '            Dim Cyy As Integer = PictureBox5.Height() - 40 - punto.Y * Esc
    '            Dim solidBruh As New SolidBrush(Color.FromArgb(121, 121, 121))
    '            Dim penB As New Pen(Color.FromArgb(21, 21, 21))
    '            penB.Width = 1
    '            Dim letra As New Font("Arial", 9, FontStyle.Regular, GraphicsUnit.Pixel)
    '            Dim cor As New SolidBrush(Color.FromArgb(0, 0, 0))
    '            Dim corBl As New SolidBrush(Color.Blue)
    '            Dim corG As New SolidBrush(Color.Green)
    '            Dim CorN As New SolidBrush(Color.Black)

    '            g.FillEllipse(solidBruh, Cxx, Cyy, Dbb, Dbb)
    '            g.DrawEllipse(penB, Cxx, Cyy, Dbb, Dbb)
    '            g.DrawString(Convert.ToString(punto.Nombre), letra, CorN, New PointF(Cxx + Dbb / 2, Cyy - Dbb / 2))
    '        End If

    '    Next


    'End Sub


    'Public Sub PictureBox5_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
    '    Dim g As Graphics = e.Graphics
    '    Dim PictureBox5 = PictureBox1


    '    For i = 0 To Detalles_Refuerzo.Count - 1
    '        Dim D As Double = Detalles_Refuerzo(i).Db / 1000
    '        Dim Dbb As Integer = D * Esc
    '        Dim Cxx As Integer = PictureBox5.Width() / 2 + Detalles_Refuerzo(i).Coordenada_X * Esc - Detalles_Refuerzo(i).Db / 2000 * Esc
    '        Dim Cyy As Integer = PictureBox5.Height() / 2 - Detalles_Refuerzo(i).Coordenada_Y * Esc - Detalles_Refuerzo(i).Db / 2000 * Esc
    '        Dim solidBruh As New SolidBrush(Color.FromArgb(121, 121, 121))
    '        Dim penB As New Pen(Color.FromArgb(21, 21, 21))
    '        penB.Width = 1
    '        Dim letra As New Font("Arial", 9, FontStyle.Regular, GraphicsUnit.Pixel)
    '        Dim cor As New SolidBrush(Color.FromArgb(0, 0, 0))
    '        Dim corBl As New SolidBrush(Color.Blue)
    '        Dim corG As New SolidBrush(Color.Green)
    '        Dim CorN As New SolidBrush(Color.Black)

    '        g.FillEllipse(solidBruh, Cxx, Cyy, Dbb, Dbb)
    '        g.DrawEllipse(penB, Cxx, Cyy, Dbb, Dbb)
    '        g.DrawString(Convert.ToString(Detalles_Refuerzo(i).Name_Barra), letra, CorN, New PointF(Cxx + Dbb, Cyy - Dbb))

    '    Next
    'End Sub





End Class