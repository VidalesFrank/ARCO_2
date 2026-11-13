Imports System.IO

''' <summary>
''' Tema visual compartido por los dashboards premium de todos los módulos
''' (Columnas, Pilas, Muros, Vigas, Nervios, Zapatas). Dos variantes
''' —Dark y Light— con colores de superficie, texto, acento y semáforo C/D.
'''
''' La preferencia se persiste en un archivo plano junto a `ARCO.exe` para
''' no inflar `My.Settings`. Cualquier formulario que quiera reaccionar al
''' cambio en caliente se suscribe a <see cref="TemaCambiado"/>.
''' </summary>
Public NotInheritable Class DashboardTheme

    Public Enum TemaModo
        Dark
        Light
    End Enum

    ' ─── Paletas ───────────────────────────────────────────────────────────
    Public ReadOnly Property Fondo As Color
    Public ReadOnly Property Panel As Color
    Public ReadOnly Property PanelAlt As Color
    Public ReadOnly Property Texto As Color
    Public ReadOnly Property TextoTenue As Color
    Public ReadOnly Property Acento As Color
    Public ReadOnly Property Verde As Color
    Public ReadOnly Property Ambar As Color
    Public ReadOnly Property Rojo As Color
    Public ReadOnly Property Grilla As Color
    Public ReadOnly Property Separador As Color
    Public ReadOnly Property Modo As TemaModo

    Private Sub New(m As TemaModo, fondo As Color, panel As Color, panelAlt As Color,
                     texto As Color, textoTenue As Color, acento As Color,
                     verde As Color, ambar As Color, rojo As Color,
                     grilla As Color, separador As Color)
        Me.Modo = m
        Me.Fondo = fondo
        Me.Panel = panel
        Me.PanelAlt = panelAlt
        Me.Texto = texto
        Me.TextoTenue = textoTenue
        Me.Acento = acento
        Me.Verde = verde
        Me.Ambar = ambar
        Me.Rojo = rojo
        Me.Grilla = grilla
        Me.Separador = separador
    End Sub

    Public Shared ReadOnly TemaDark As New DashboardTheme(
        TemaModo.Dark,
        fondo:=Color.FromArgb(18, 22, 30),
        panel:=Color.FromArgb(28, 33, 44),
        panelAlt:=Color.FromArgb(36, 42, 55),
        texto:=Color.FromArgb(230, 234, 240),
        textoTenue:=Color.FromArgb(150, 160, 180),
        acento:=Color.FromArgb(0, 212, 255),
        verde:=Color.FromArgb(56, 220, 130),
        ambar:=Color.FromArgb(255, 180, 40),
        rojo:=Color.FromArgb(255, 85, 90),
        grilla:=Color.FromArgb(55, 62, 78),
        separador:=Color.FromArgb(60, 70, 90))

    Public Shared ReadOnly TemaLight As New DashboardTheme(
        TemaModo.Light,
        fondo:=Color.FromArgb(243, 246, 250),
        panel:=Color.White,
        panelAlt:=Color.FromArgb(242, 244, 248),
        texto:=Color.FromArgb(31, 41, 55),
        textoTenue:=Color.FromArgb(107, 114, 128),
        acento:=Color.FromArgb(3, 105, 161),
        verde:=Color.FromArgb(22, 163, 74),
        ambar:=Color.FromArgb(234, 88, 12),
        rojo:=Color.FromArgb(220, 38, 38),
        grilla:=Color.FromArgb(229, 231, 235),
        separador:=Color.FromArgb(209, 213, 219))

    ' ─── Umbral C/D compartido ─────────────────────────────────────────────
    ''' <summary>
    ''' Umbral de cumplimiento efectivo. 0.895 ≡ redondear a 2 cifras y comparar
    ''' contra 0.90: así un C/D de 0.89842 (que la UI muestra como "0.90") no se
    ''' marca como no cumple. Sincronizado con Funciones_00_Varias.UMBRAL_CD.
    ''' </summary>
    Public Const UMBRAL_CD As Double = 0.895

    Public Function ColorPorCD(cd As Double) As Color
        If cd <= 0 Then Return TextoTenue
        If cd >= UMBRAL_CD Then Return Verde
        If cd >= 0.75 Then Return Ambar
        Return Rojo
    End Function

    ' ─── Preferencia global ────────────────────────────────────────────────
    Private Shared _actual As DashboardTheme = CargarPreferencia()
    Public Shared Event TemaCambiado As EventHandler

    Public Shared Property Actual As DashboardTheme
        Get
            Return _actual
        End Get
        Set(value As DashboardTheme)
            If value Is Nothing OrElse value Is _actual Then Return
            _actual = value
            GuardarPreferencia(value.Modo)
            RaiseEvent TemaCambiado(Nothing, EventArgs.Empty)
        End Set
    End Property

    Public Shared Sub Alternar()
        Actual = If(_actual.Modo = TemaModo.Dark, TemaLight, TemaDark)
    End Sub

    ' Archivo plano junto al ejecutable, para no tocar Settings.settings.
    Private Shared ReadOnly RutaPref As String =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dashboard.theme")

    Private Shared Function CargarPreferencia() As DashboardTheme
        Try
            If File.Exists(RutaPref) Then
                Dim txt = File.ReadAllText(RutaPref).Trim().ToLowerInvariant()
                If txt = "light" Then Return TemaLight
            End If
        Catch
            ' silencio: cualquier error de lectura cae al default (Dark)
        End Try
        Return TemaDark
    End Function

    Private Shared Sub GuardarPreferencia(m As TemaModo)
        Try
            File.WriteAllText(RutaPref, If(m = TemaModo.Light, "light", "dark"))
        Catch
            ' si no se puede escribir no hay nada que hacer; se usa en memoria
        End Try
    End Sub

End Class
