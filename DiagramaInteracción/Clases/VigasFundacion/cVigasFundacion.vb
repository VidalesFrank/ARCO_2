Imports System.Runtime.Serialization

' ═══════════════════════════════════════════════════════════════════════════════
'  Módulo 08 — Vigas de Fundación (puntales)
'  Análisis por Diagrama de Interacción — C/D compresión y tracción + NSR-10
' ═══════════════════════════════════════════════════════════════════════════════

<Serializable>
Public Class cVigasFundacion
    <OptionalField> Public Elementos As New List(Of cVigaFundacion)()

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Elementos Is Nothing Then Elementos = New List(Of cVigaFundacion)()
    End Sub
End Class

<Serializable>
Public Class cVigaFundacion

    ' ── Identificación ────────────────────────────────────────────────────────
    <OptionalField> Public Nombre As String = "VF-1"
    <OptionalField> Public NombrePlano As String = ""

    ' ── Geometría ─────────────────────────────────────────────────────────────
    <OptionalField> Public B As Double = 0.3          ' m
    <OptionalField> Public H As Double = 0.5          ' m
    <OptionalField> Public L As Double = 3.0          ' m
    <OptionalField> Public Recubrimiento As Double = 0.05  ' m

    ' ── Materiales ────────────────────────────────────────────────────────────
    <OptionalField> Public fc As Double = 21          ' MPa
    <OptionalField> Public fy As Double = 420         ' MPa

    ' ── Demanda axial ─────────────────────────────────────────────────────────
    <OptionalField> Public P_Columna As Double = 0    ' kN — carga máx. columna/análisis
    <OptionalField> Public Aa_Tribu As Double = 0     ' m² — área tributaria
    <OptionalField> Public Pu As Double = 0           ' kN — Pu diseño = 0.25 × Aa × P_Columna

    ' ── Refuerzo longitudinal ─────────────────────────────────────────────────
    <OptionalField> Public BarraSup As String = "#4"
    <OptionalField> Public CantSup As Integer = 2
    <OptionalField> Public BarraInf As String = "#4"
    <OptionalField> Public CantInf As Integer = 3

    ' ── Refuerzo transversal ──────────────────────────────────────────────────
    <OptionalField> Public BarraEstribo As String = "#3"
    <OptionalField> Public SepEstribo As Double = 0.15   ' m
    <OptionalField> Public RamasEstribo As Integer = 2

    ' ── Resultados DI ─────────────────────────────────────────────────────────
    <OptionalField> Public PhiPn_Max As Double = 0    ' kN — capacidad máx. compresión
    <OptionalField> Public PhiPn_Min As Double = 0    ' kN — capacidad mín. (tracción, neg.)
    <OptionalField> Public CD_Comp As Double = 0      ' φPn_Max / Pu
    <OptionalField> Public CD_Trac As Double = 0      ' |φPn_Min| / Pu

    ' Área de acero colocada (para chequeo cuantía)
    <OptionalField> Public As_Prov_Sup As Double = 0  ' cm²
    <OptionalField> Public As_Prov_Inf As Double = 0  ' cm²

    ' ── Requisitos normativos ─────────────────────────────────────────────────
    <OptionalField> Public CumpleDim As Boolean = False
    <OptionalField> Public CumpleCuantia As Boolean = False
    <OptionalField> Public CumpleEstribo As Boolean = False
    <OptionalField> Public DetalleDim As String = ""
    <OptionalField> Public DetalleCuantia As String = ""
    <OptionalField> Public DetalleEstribo As String = ""

    ' ── Estado general ────────────────────────────────────────────────────────
    <OptionalField> Public CumpleDI As Boolean = False
    <OptionalField> Public CumpleNormativo As Boolean = False
    <OptionalField> Public Cumple As Boolean = False
    <OptionalField> Public Calculado As Boolean = False

    <OnDeserialized>
    Private Sub OnDeserialized(ctx As StreamingContext)
        If Nombre Is Nothing Then Nombre = "VF-1"
        If NombrePlano Is Nothing Then NombrePlano = ""
        If BarraSup Is Nothing Then BarraSup = "#4"
        If BarraInf Is Nothing Then BarraInf = "#4"
        If BarraEstribo Is Nothing Then BarraEstribo = "#3"
        If fc = 0 Then fc = 21
        If fy = 0 Then fy = 420
        If Recubrimiento = 0 Then Recubrimiento = 0.05
        If B = 0 Then B = 0.3
        If H = 0 Then H = 0.5
        If L = 0 Then L = 3.0
        If RamasEstribo = 0 Then RamasEstribo = 2
        If SepEstribo = 0 Then SepEstribo = 0.15
        If DetalleDim Is Nothing Then DetalleDim = ""
        If DetalleCuantia Is Nothing Then DetalleCuantia = ""
        If DetalleEstribo Is Nothing Then DetalleEstribo = ""
    End Sub

    Public Overrides Function ToString() As String
        Return If(Not String.IsNullOrWhiteSpace(NombrePlano), $"{Nombre} — {NombrePlano}", Nombre)
    End Function

End Class
