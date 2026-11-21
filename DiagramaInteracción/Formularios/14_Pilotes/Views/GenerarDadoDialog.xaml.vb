Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls

''' <summary>
''' Diálogo modal para generar la geometría de un TipoDado con parámetros
''' editables (sustituye a los 4 generadores hardcoded). Devuelve el polígono
''' y el layout de pilotes al caller en las propiedades PoligonoResultante /
''' PilotesResultantes cuando DialogResult = True.
''' </summary>
Partial Public Class GenerarDadoDialog
    Inherits Window

    Public Property PoligonoResultante As List(Of Punto2D)
    Public Property PilotesResultantes As List(Of PilotePosicion)

    ''' <summary>Nombre del TipoPilote que se asignará a las nuevas posiciones.</summary>
    Public Property NombreTipoPilote As String

    Public Sub New(nombreTipoPilote As String)
        InitializeComponent()
        Me.NombreTipoPilote = If(nombreTipoPilote, "")
    End Sub

    Private Sub BtnCerrar_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

    Private Sub BtnCancelar_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
        Close()
    End Sub

    Private Sub BtnAplicar_Click(sender As Object, e As RoutedEventArgs)
        LblError.Text = ""
        Try
            If TabForma.SelectedIndex = 0 Then
                AplicarRectangular()
            Else
                AplicarRegular()
            End If
            DialogResult = True
            Close()
        Catch ex As Exception
            LblError.Text = ex.Message
        End Try
    End Sub

    Private Sub AplicarRectangular()
        Dim ancho = ParseDouble(TxtAncho.Text, "Ancho")
        Dim alto = ParseDouble(TxtAlto.Text, "Alto")
        Dim cols = ParseInt(TxtColumnas.Text, "Columnas")
        Dim filas = ParseInt(TxtFilas.Text, "Filas")
        Dim sep = ParseDouble(TxtSeparacion.Text, "Separación")

        If ancho <= 0 OrElse alto <= 0 Then Throw New Exception("Ancho y alto deben ser > 0.")
        If cols < 1 OrElse filas < 1 Then Throw New Exception("Filas y columnas deben ser ≥ 1.")
        If sep <= 0 Then Throw New Exception("La separación debe ser > 0.")

        Dim anchoPilotes = (cols - 1) * sep
        Dim altoPilotes = (filas - 1) * sep
        If anchoPilotes > ancho Then
            Throw New Exception($"Los pilotes no caben: ancho requerido {anchoPilotes:0.00} m > {ancho:0.00} m.")
        End If
        If altoPilotes > alto Then
            Throw New Exception($"Los pilotes no caben: alto requerido {altoPilotes:0.00} m > {alto:0.00} m.")
        End If

        PoligonoResultante = PiloteService.GenerarPoligonoRectangular(ancho, alto)
        PilotesResultantes = PiloteService.GenerarLayoutRectangular(filas, cols, sep, NombreTipoPilote)
    End Sub

    Private Sub AplicarRegular()
        Dim nLados = ObtenerNLados()
        Dim radioDado = ParseDouble(TxtRadioDado.Text, "Radio del dado")
        Dim nPilotes = ParseInt(TxtNPilotes.Text, "Número de pilotes")
        Dim radioAnillo = ParseDouble(TxtRadioAnillo.Text, "Radio del anillo")

        If nLados < 3 Then Throw New Exception("El polígono necesita al menos 3 lados.")
        If radioDado <= 0 Then Throw New Exception("El radio del dado debe ser > 0.")
        If nPilotes < 1 Then Throw New Exception("Debe haber al menos 1 pilote.")
        If radioAnillo < 0 Then Throw New Exception("El radio del anillo no puede ser negativo.")
        If radioAnillo > radioDado Then
            Throw New Exception($"El anillo de pilotes ({radioAnillo:0.00} m) excede el dado ({radioDado:0.00} m).")
        End If

        PoligonoResultante = PiloteService.GenerarPoligonoRegular(nLados, radioDado)
        Dim pilotes = PiloteService.GenerarLayoutCircular(nPilotes, radioAnillo, NombreTipoPilote)
        If ChkPiloteCentral.IsChecked = True Then
            pilotes.Insert(0, New PilotePosicion(0, 0, NombreTipoPilote))
        End If
        PilotesResultantes = pilotes
    End Sub

    Private Function ObtenerNLados() As Integer
        Dim item = TryCast(CmbLados.SelectedItem, ComboBoxItem)
        If item Is Nothing Then Return 4
        ' El texto es "Pentágono (5)" — extraer el número entre paréntesis.
        Dim txt = CStr(item.Content)
        Dim i1 = txt.IndexOf("("c)
        Dim i2 = txt.IndexOf(")"c)
        If i1 < 0 OrElse i2 <= i1 Then Return 4
        Return Integer.Parse(txt.Substring(i1 + 1, i2 - i1 - 1), CultureInfo.InvariantCulture)
    End Function

    Private Function ParseDouble(txt As String, campo As String) As Double
        Dim v As Double
        If Not Double.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, v) AndAlso
           Not Double.TryParse(txt, v) Then
            Throw New Exception($"{campo}: valor inválido '{txt}'.")
        End If
        Return v
    End Function

    Private Function ParseInt(txt As String, campo As String) As Integer
        Dim v As Integer
        If Not Integer.TryParse(txt, v) Then
            Throw New Exception($"{campo}: valor inválido '{txt}'.")
        End If
        Return v
    End Function

End Class
