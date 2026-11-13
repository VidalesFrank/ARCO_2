Imports System.Data

''' <summary>
''' ViewModel de la ventana de detalle de un apoyo (Dado). Expone:
'''   · Datos del apoyo (joint, dado asignado, coords)
'''   · ResumenPorPilote — una fila por pilote con máx/mín y combo gobernante
'''   · Matriz combinaciones × pilotes como DataTable (columnas dinámicas)
''' La UI se enlaza directamente; el VM no tiene commands porque es puro
''' detalle de solo-lectura.
''' </summary>
Public Class DadoDetalleVM
    Inherits ObservableVM

    Public ReadOnly Modelo As Dado
    Public ReadOnly TipoDado As TipoDado
    Public ReadOnly Contenedor As cPilotes
    Public ReadOnly Resultado As PiloteService.ResultadoDado

    ''' <summary>Tabla con columnas dinámicas: Combo | P1 | P2 | ... | Pn | Pmax | Pmin.</summary>
    Public ReadOnly Property MatrizCombinaciones As DataTable

    ''' <summary>Resumen por pilote individual.</summary>
    Public ReadOnly Property ResumenPorPilote As List(Of PiloteService.ResumenPilote)

    Public Sub New(modelo As Dado, contenedor As cPilotes)
        Me.Modelo = modelo
        Me.Contenedor = contenedor
        Me.TipoDado = contenedor?.BuscarTipoDado(modelo.NombreTipoDado)

        Dim tpDefault As TipoPilote = Nothing
        If TipoDado IsNot Nothing AndAlso TipoDado.Pilotes.Count > 0 Then
            tpDefault = contenedor?.BuscarTipoPilote(TipoDado.Pilotes.First().NombreTipoPilote)
        End If

        If TipoDado Is Nothing OrElse tpDefault Is Nothing Then
            Resultado = New PiloteService.ResultadoDado()
        Else
            Resultado = PiloteService.EvaluarDado(modelo, TipoDado, tpDefault, contenedor)
        End If

        _resumenPorPilote = Resultado.PorPilote
        _matrizCombinaciones = BuildMatriz()
    End Sub

    Private Function BuildMatriz() As DataTable
        Dim dt As New DataTable()
        dt.Columns.Add("Combo", GetType(String))
        Dim n = If(TipoDado IsNot Nothing, TipoDado.Pilotes.Count, 0)
        For i = 1 To n
            dt.Columns.Add($"P{i}", GetType(Double))
        Next
        dt.Columns.Add("Pmax", GetType(Double))
        dt.Columns.Add("Pmin", GetType(Double))
        dt.Columns.Add("Gob", GetType(String))

        If Resultado Is Nothing OrElse Resultado.PorCombinacion Is Nothing Then Return dt
        For Each rc In Resultado.PorCombinacion
            Dim row = dt.NewRow()
            row("Combo") = rc.LoadCase
            Dim valores = rc.Fuerzas
            If valores IsNot Nothing Then
                For i = 0 To Math.Min(n, valores.Length) - 1
                    row($"P{i + 1}") = Math.Round(valores(i), 1)
                Next
            End If
            row("Pmax") = Math.Round(rc.Pu_Max, 1)
            row("Pmin") = Math.Round(rc.Pu_Min, 1)
            Dim marca = ""
            If rc.LoadCase = Resultado.Combo_Gob_Compresion Then marca &= "★ comp"
            If rc.LoadCase = Resultado.Combo_Gob_Traccion Then
                If marca.Length > 0 Then marca &= " · "
                marca &= "★ tracc"
            End If
            row("Gob") = marca
            dt.Rows.Add(row)
        Next
        Return dt
    End Function

    ' ── Propiedades de visualización ─────────────────────────────

    Public ReadOnly Property Titulo As String
        Get
            Return $"Detalle apoyo {Modelo.JointLabel}"
        End Get
    End Property

    Public ReadOnly Property SubtituloOrigen As String
        Get
            Return If(String.IsNullOrEmpty(Modelo.SourceType), "—", Modelo.SourceType)
        End Get
    End Property

    Public ReadOnly Property SubtituloDado As String
        Get
            Return If(String.IsNullOrEmpty(Modelo.NombreTipoDado), "(sin dado asignado)",
                      $"dado {Modelo.NombreTipoDado}")
        End Get
    End Property

    Public ReadOnly Property SubtituloCoords As String
        Get
            Return $"({Modelo.X_Global:0.00}, {Modelo.Y_Global:0.00}, {Modelo.Z_Base:0.00}) m"
        End Get
    End Property

    Public ReadOnly Property NumPilotes As Integer
        Get
            Return If(TipoDado IsNot Nothing, TipoDado.Pilotes.Count, 0)
        End Get
    End Property

    Public ReadOnly Property NumCombinaciones As Integer
        Get
            Return If(Modelo.Combinaciones Is Nothing, 0, Modelo.Combinaciones.Count)
        End Get
    End Property

    Public ReadOnly Property Pu_Max As Double
        Get
            Return Resultado.Pu_Max_Compresion
        End Get
    End Property

    Public ReadOnly Property Pu_Min As Double
        Get
            Return Resultado.Pu_Min_Traccion
        End Get
    End Property

    Public ReadOnly Property Combo_Gob_Compresion As String
        Get
            Return Resultado.Combo_Gob_Compresion
        End Get
    End Property

    Public ReadOnly Property Combo_Gob_Traccion As String
        Get
            Return Resultado.Combo_Gob_Traccion
        End Get
    End Property

    Public ReadOnly Property CD_Compresion As Double
        Get
            Return Resultado.CD_Compresion
        End Get
    End Property

    Public ReadOnly Property CD_Traccion As Double
        Get
            Return Resultado.CD_Traccion
        End Get
    End Property

End Class
