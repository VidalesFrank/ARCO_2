Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_04_Escalera

Public Class Form_04_Escaleras
    Public Shared Proyecto As New Proyecto_Escaleras

    ' ── Símbolos Unicode construidos en tiempo de ejecución (independiente de codificación del archivo) ──
    Private ReadOnly U_DELTA As String = ChrW(948)    ' δ — deflexión
    Private ReadOnly U_LAMBDA As String = ChrW(955)   ' λ — multiplicador
    Private ReadOnly U_RHO As String = ChrW(961)      ' ρ — cuantía
    Private ReadOnly U_SUP2 As String = ChrW(178)     ' ² — superíndice
    Private ReadOnly U_SUP4 As String = ChrW(8308)    ' ⁴ — superíndice
    Private ReadOnly U_DOT As String = ChrW(183)      ' · — punto medio
    Private ReadOnly U_GE As String = ChrW(8805)      ' ≥ — mayor o igual
    Private ReadOnly U_LE As String = ChrW(8804)      ' ≤ — menor o igual

    ' ── Controles de TabPage2 creados programáticamente ──────────────────────
    ' Refuerzo superior
    Private T_RefSup_Estado As TextBox
    Private T_RefSup_Motivo As TextBox
    Private T_RefSup_AsReq As TextBox
    Private T_RefSup_SReq As TextBox
    Private T_RefSup_SCol As TextBox
    Private T_RefSup_Verif As TextBox
    Private C_RefSup_Barra As ComboBox

    ' Deflexiones — parámetros
    Private T_Ec As TextBox
    Private T_fr As TextBox
    Private T_Mcr As TextBox
    Private T_Ig As TextBox
    Private T_Icr As TextBox
    Private T_Ie As TextBox
    Private T_Weq As TextBox

    ' Deflexiones — resultados
    Private T_DeltaInm As TextBox
    Private T_Adm360 As TextBox
    Private T_Verif360 As TextBox
    Private T_Lambda As TextBox
    Private T_DeltaLP As TextBox
    Private T_Adm480 As TextBox
    Private T_Verif480 As TextBox
    Private C_LimiteLP As ComboBox    ' selector de límite largo plazo

#Region "Inicialización"

    Private Sub Form_04_Escaleras_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InicializarTabPage2()
    End Sub

    Private Sub InicializarTabPage2()
        ' Segoe UI tiene cobertura Unicode completa en Windows 7+:
        ' letras griegas (δ λ ρ), superíndices (² ⁴), operadores (· ≥ ≤) se renderizan correctamente.
        ' Los caracteres se construyen con ChrW() para independencia de la codificación del .vb.
        Dim fnt As New System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular)
        Dim fntBold As New System.Drawing.Font("Segoe UI", 11.0F, System.Drawing.FontStyle.Bold)
        Dim colorTab As System.Drawing.Color = System.Drawing.Color.FromArgb(200, 200, 200)
        Dim colorRO As System.Drawing.Color = System.Drawing.Color.FromArgb(240, 240, 240)

        ' Panel scrollable como contenedor — protege si el form se redimensiona
        Dim pnlScroll As New Panel() With {
            .Dock = DockStyle.Fill, .AutoScroll = True, .BackColor = colorTab
        }
        TabPage2.Controls.Add(pnlScroll)

        ' ── GroupBox Refuerzo Superior ───────────────────────────────────────
        Dim gbSup As New GroupBox() With {
            .Text = "Refuerzo Superior (Doble Capa)",
            .Font = fntBold, .ForeColor = System.Drawing.Color.Black,
            .Location = New System.Drawing.Point(9, 12),
            .Size = New System.Drawing.Size(615, 265),
            .BackColor = colorTab
        }

        AgregarFila(gbSup, fnt, "Estado:", 28, T_RefSup_Estado, True, 375)
        AgregarFila(gbSup, fnt, "Motivo:", 66, T_RefSup_Motivo, True, 375)
        AgregarFila(gbSup, fnt, "As req. (mm" & U_SUP2 & "):", 104, T_RefSup_AsReq, True, 120)
        AgregarFila(gbSup, fnt, "S requerida (m):", 142, T_RefSup_SReq, True, 100)

        ' Fila 180: Barra | ComboBox | Verif. | TextBox (misma línea horizontal)
        gbSup.Controls.Add(New Label() With {
            .Text = "Barra superior:", .Font = fnt,
            .Location = New System.Drawing.Point(10, 184), .AutoSize = True
        })
        C_RefSup_Barra = New ComboBox() With {
            .Font = fnt, .DropDownStyle = ComboBoxStyle.DropDownList,
            .Location = New System.Drawing.Point(220, 180), .Size = New System.Drawing.Size(90, 28)
        }
        C_RefSup_Barra.Items.AddRange({"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"})
        AddHandler C_RefSup_Barra.SelectedIndexChanged, AddressOf C_RefSup_Barra_SelectedIndexChanged
        gbSup.Controls.Add(C_RefSup_Barra)

        gbSup.Controls.Add(New Label() With {
            .Text = "Verif.:", .Font = fnt,
            .Location = New System.Drawing.Point(335, 184), .AutoSize = True
        })
        T_RefSup_Verif = New TextBox() With {
            .Font = fnt, .ReadOnly = True, .TextAlign = HorizontalAlignment.Center,
            .BackColor = colorRO,
            .Location = New System.Drawing.Point(382, 180), .Size = New System.Drawing.Size(160, 28)
        }
        gbSup.Controls.Add(T_RefSup_Verif)

        AgregarFila(gbSup, fnt, "S colocada (m):", 218, T_RefSup_SCol, False, 100)
        AddHandler T_RefSup_SCol.TextChanged, AddressOf T_RefSup_SCol_TextChanged

        pnlScroll.Controls.Add(gbSup)

        ' ── GroupBox Parámetros de Cálculo ────────────────────────────────────
        Dim gbParam As New GroupBox() With {
            .Text = "Parámetros de Cálculo",
            .Font = fntBold, .ForeColor = System.Drawing.Color.Black,
            .Location = New System.Drawing.Point(633, 12),
            .Size = New System.Drawing.Size(355, 265),
            .BackColor = colorTab
        }

        AgregarFila(gbParam, fnt, "Ec (MPa):", 28, T_Ec, True, 120)
        AgregarFila(gbParam, fnt, "fr (MPa):", 66, T_fr, True, 120)
        AgregarFila(gbParam, fnt, "Mcr (kN" & U_DOT & "m):", 104, T_Mcr, True, 120)
        AgregarFila(gbParam, fnt, "Ig (cm" & U_SUP4 & "):", 142, T_Ig, True, 120)
        AgregarFila(gbParam, fnt, "Icr (cm" & U_SUP4 & "):", 180, T_Icr, True, 120)
        AgregarFila(gbParam, fnt, "Ie (cm" & U_SUP4 & "):", 218, T_Ie, True, 120)

        pnlScroll.Controls.Add(gbParam)

        ' ── GroupBox Deflexión Inmediata ─────────────────────────────────────
        ' Título usa U_DOT para el punto medio y U_SUP4 para el superíndice ⁴
        Dim gbInm As New GroupBox() With {
            .Text = "Deflexión Inmediata  [5" & U_DOT & "w" & U_DOT & "L" & U_SUP4 & " / (384" & U_DOT & "E" & U_DOT & "I)]",
            .Font = fntBold, .ForeColor = System.Drawing.Color.Black,
            .Location = New System.Drawing.Point(9, 290),
            .Size = New System.Drawing.Size(615, 180),
            .BackColor = colorTab
        }

        AgregarFila(gbInm, fnt, "Carga equiv. (kN/m):", 28, T_Weq, True, 120)
        AgregarFila(gbInm, fnt, U_DELTA & " inmediata (mm):", 66, T_DeltaInm, True, 120)
        AgregarFila(gbInm, fnt, U_DELTA & " adm. L/360 (mm):", 104, T_Adm360, True, 120)
        AgregarFila(gbInm, fnt, "Verif. L/360:", 142, T_Verif360, True, 140)

        pnlScroll.Controls.Add(gbInm)

        ' ── GroupBox Deflexión Largo Plazo ────────────────────────────────────
        Dim gbLP As New GroupBox() With {
            .Text = "Deflexión Largo Plazo  [" & U_LAMBDA & " = 2.0 — NSR-10 C.9.5.2.5]",
            .Font = fntBold, .ForeColor = System.Drawing.Color.Black,
            .Location = New System.Drawing.Point(9, 484),
            .Size = New System.Drawing.Size(615, 220),
            .BackColor = colorTab
        }

        ' Fila 28: selector de condición (elementos susceptibles o no)
        gbLP.Controls.Add(New Label() With {
            .Text = "Condición:", .Font = fnt,
            .Location = New System.Drawing.Point(10, 32), .AutoSize = True
        })
        C_LimiteLP = New ComboBox() With {
            .Font = fnt, .DropDownStyle = ComboBoxStyle.DropDownList,
            .Location = New System.Drawing.Point(220, 28), .Size = New System.Drawing.Size(365, 28)
        }
        C_LimiteLP.Items.Add("Con elementos susceptibles a daño  " & U_DELTA & " adm = L/480")
        C_LimiteLP.Items.Add("Sin elementos susceptibles a daño  " & U_DELTA & " adm = L/240")
        C_LimiteLP.SelectedIndex = 0
        AddHandler C_LimiteLP.SelectedIndexChanged, AddressOf C_LimiteLP_SelectedIndexChanged
        gbLP.Controls.Add(C_LimiteLP)

        ' Filas de resultados (paso 38 px, desplazadas 38 por el selector)
        AgregarFila(gbLP, fnt, U_LAMBDA & " (mult.):", 66, T_Lambda, True, 80)
        AgregarFila(gbLP, fnt, U_DELTA & " total (mm):", 104, T_DeltaLP, True, 120)
        AgregarFila(gbLP, fnt, U_DELTA & " adm. LP (mm):", 142, T_Adm480, True, 120)
        AgregarFila(gbLP, fnt, "Verif. LP:", 180, T_Verif480, True, 140)

        pnlScroll.Controls.Add(gbLP)
    End Sub

    ' Crea Label (ancho fijo 205 px, sin desbordamiento) + TextBox en Y fijo.
    Private Sub AgregarFila(container As Control, fnt As System.Drawing.Font, lblText As String,
                             y As Integer, ByRef txb As TextBox,
                             Optional soloLectura As Boolean = True,
                             Optional width As Integer = 120)
        container.Controls.Add(New Label() With {
            .Text = lblText, .Font = fnt,
            .Location = New System.Drawing.Point(10, y + 4),
            .Width = 205, .AutoEllipsis = True
        })
        txb = New TextBox() With {
            .Font = fnt,
            .Location = New System.Drawing.Point(220, y),
            .Size = New System.Drawing.Size(width, 28),
            .ReadOnly = soloLectura,
            .TextAlign = HorizontalAlignment.Center,
            .BackColor = If(soloLectura, System.Drawing.Color.FromArgb(240, 240, 240), System.Drawing.Color.White)
        }
        container.Controls.Add(txb)
    End Sub

#End Region

#Region "Datos Iniciales — handlers existentes"

    Private Sub T_LDescanso_TextChanged(sender As Object, e As EventArgs) Handles T_LDescanso.TextChanged
        Try
            If T_LPeldaños.Text <> String.Empty And T_LDescanso.Text <> String.Empty Then
                T_L.Text = Convert.ToSingle(T_LPeldaños.Text) + Convert.ToSingle(T_LDescanso.Text)
                T_Hminima.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
                T_H.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub T_LPeldaños_TextChanged(sender As Object, e As EventArgs) Handles T_LPeldaños.TextChanged
        Try
            If T_LPeldaños.Text <> String.Empty And T_LDescanso.Text <> String.Empty Then
                T_L.Text = Convert.ToSingle(T_LPeldaños.Text) + Convert.ToSingle(T_LDescanso.Text)
                T_Hminima.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
                T_H.Text = Funcion_Multiplo(Convert.ToSingle(T_L.Text) / 20, 0.05)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub T_Huella_TextChanged(sender As Object, e As EventArgs) Handles T_Huella.TextChanged
        Try
            If T_NPeldaños.Text <> String.Empty And T_Huella.Text <> String.Empty Then
                T_LPeldaños.Text = Math.Round(Convert.ToSingle(T_NPeldaños.Text) * Convert.ToSingle(T_Huella.Text), 2)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub T_NPeldaños_TextChanged(sender As Object, e As EventArgs) Handles T_NPeldaños.TextChanged
        Try
            If T_NPeldaños.Text <> String.Empty And T_Huella.Text <> String.Empty Then
                T_LPeldaños.Text = Math.Round(Convert.ToSingle(T_NPeldaños.Text) * Convert.ToSingle(T_Huella.Text), 2)
            End If
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "Cálculo principal"

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Try
            Dim fc As Single = Convert.ToSingle(T_fc.Text)
            Dim fy As Single = Convert.ToSingle(T_fy.Text)
            Dim C_Impuesta As Single = Convert.ToSingle(T_C_Impuesta.Text)
            Dim C_Viva As Single = Convert.ToSingle(T_C_Viva.Text)
            Dim Peso_ConcretoReforzado As Single = Convert.ToSingle(T_PesoConcretoReforzado.Text)
            Dim Peso_Concreto As Single = Convert.ToSingle(T_PesoConcreto.Text)

            Dim Huella As Single = Convert.ToSingle(T_Huella.Text)
            Dim Contrahuella As Single = Convert.ToSingle(T_Contrahuella.Text)
            Dim N_Peldanos As Integer = Convert.ToInt32(T_NPeldaños.Text)
            Dim L_Peldaños As Single = Convert.ToSingle(T_LPeldaños.Text)
            Dim L_Descanso As Single = Convert.ToSingle(T_LDescanso.Text)
            Dim L_Total As Single = Convert.ToSingle(T_L.Text)
            Dim A_Escalera As Single = Convert.ToSingle(T_AEscalera.Text)
            Dim A_Estudio As Single = Convert.ToSingle(T_AEstudio.Text)
            Dim Recubrimiento As Single = Convert.ToSingle(T_Recubrimiento.Text)
            Dim h As Single = Convert.ToSingle(T_H.Text)

            ' ── Cargas ───────────────────────────────────────────────────────
            Dim L_Inclinada As Single = Math.Sqrt(L_Peldaños ^ 2 + (N_Peldanos * Contrahuella) ^ 2)
            Dim Peso_Losa As Single = h * Peso_ConcretoReforzado * A_Estudio
            Dim Peso_Peldanos As Single = (Huella * Contrahuella / 2) * N_Peldanos * Peso_Concreto * A_Estudio
            Dim Peso_Impuesta As Single = C_Impuesta * A_Estudio
            Dim Peso_Viva As Single = C_Viva * A_Estudio
            Dim Peso_ImpuestaPeldanos As Single = C_Impuesta * A_Estudio * (Huella + Contrahuella) / Huella

            Dim Wpp_Inclinada As Single = (Peso_Losa / (Math.Cos(Math.Atan(Contrahuella / Huella)))) + Peso_ImpuestaPeldanos
            Dim Wu_Inclinada As Single = 1.2 * Wpp_Inclinada + 1.6 * Peso_Viva
            Dim Wu_Descanso As Single = 1.2 * Peso_Losa + 1.2 * Peso_Impuesta + 1.6 * Peso_Viva

            ' Cargas de servicio para deflexión (sin factores de amplificación)
            Dim Wserv_Inclinada As Single = Wpp_Inclinada + Peso_Viva
            Dim Wserv_Descanso As Single = Peso_Losa + Peso_Impuesta + Peso_Viva

            ' ── Guardar datos base ───────────────────────────────────────────
            Proyecto.fc = fc : Proyecto.fy = fy
            Proyecto.C_Imp = C_Impuesta : Proyecto.C_Viv = C_Viva
            Proyecto.P_ConR = Peso_ConcretoReforzado : Proyecto.P_Con = Peso_Concreto
            Proyecto.Huella = Huella : Proyecto.Contrahuella = Contrahuella
            Proyecto.N_Peldanos = N_Peldanos : Proyecto.L_Peldanos = L_Peldaños
            Proyecto.L_Descanso = L_Descanso : Proyecto.L_Total = L_Total
            Proyecto.A_Escalera = A_Escalera : Proyecto.A_Estudio = A_Estudio
            Proyecto.Recubrimiento = Recubrimiento : Proyecto.h = h
            Proyecto.Wu_Inclinada = Wu_Inclinada : Proyecto.Wu_Descanso = Wu_Descanso

            ' ── Diagrama M y V ───────────────────────────────────────────────
            Proyecto.Abscisas.Clear()
            Proyecto.Momentos.Clear()
            Proyecto.Cortantes.Clear()
            Dim N_Puntos As Integer = Convert.ToInt32(T_NPuntos.Text) + 1
            For i = 0 To Proyecto.L_Total Step Proyecto.L_Total / N_Puntos
                Proyecto.Abscisas.Add(i)
                Proyecto.Momentos.Add(MomentoEscalera(i, Proyecto.L_Peldanos, Proyecto.L_Descanso, Proyecto.Wu_Inclinada, Proyecto.Wu_Descanso))
                Proyecto.Cortantes.Add(CortanteEscalera(i, Proyecto.L_Peldanos, Proyecto.L_Descanso, Proyecto.Wu_Inclinada, Proyecto.Wu_Descanso))
            Next
            T_Vmax.Text = Math.Round(Proyecto.Cortantes.Max, 2)
            T_Mmax.Text = Math.Round(Proyecto.Momentos.Max, 2)

            ' ── Temperatura ──────────────────────────────────────────────────
            Proyecto.Cuantia_Temperatura = Convert.ToSingle(T_CuantiaTemperatura.Text)
            Proyecto.Acero_Temperatura = h * A_Estudio * Proyecto.Cuantia_Temperatura * 1000 ^ 2
            C_BarraTemperatura.SelectedIndex = 1

            ' ── Flexión inferior ─────────────────────────────────────────────
            Proyecto.Mu = Math.Round(Proyecto.Momentos.Max, 2)
            Proyecto.Acero_Flexion = DiseñoFlexion(Proyecto.fy, Proyecto.fc, Proyecto.A_Estudio * 1000, Proyecto.h * 1000, Proyecto.Recubrimiento * 1000, Proyecto.Mu, 1.4 / Proyecto.fy, 0.0033)
            Proyecto.Cuantia_Flexion = Math.Round(Proyecto.Acero_Flexion / (Proyecto.A_Estudio * (Proyecto.h - Proyecto.Recubrimiento) * 1000000), 5)
            C_BarraFlexion.SelectedIndex = 2

            ' ── Cortante ─────────────────────────────────────────────────────
            Proyecto.Vu = Math.Max(Proyecto.Cortantes.Max, Math.Abs(Proyecto.Cortantes.Min))
            Proyecto.Vc = 0.75 * 0.17 * Math.Sqrt(fc) * 1000 * (h - Recubrimiento) * A_Estudio

            ' ── Deflexiones ──────────────────────────────────────────────────
            CalcularDeflexiones(fc, fy, h, Recubrimiento, A_Estudio, L_Peldaños, L_Descanso, L_Total,
                                Wserv_Inclinada, Wserv_Descanso)

            ' ── Doble refuerzo ───────────────────────────────────────────────
            EvaluarDobleRefuerzo(h, fy, A_Estudio)

            Rellenar()

        Catch ex As FormatException
            Logger.Warning("Form_04_Escaleras.Button2_Click", "Dato de entrada inválido: " & ex.Message)
            MessageBox.Show("Verifique que todos los campos numéricos tengan valores válidos.", "Dato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Catch ex As Exception
            Logger.Error(ex, "Form_04_Escaleras.Button2_Click", "Error durante el cálculo de escaleras")
            MessageBox.Show("Error durante el cálculo. Revise el log para más detalles.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CalcularDeflexiones(fc As Single, fy As Single, h As Single, rec As Single,
                                     b As Single, L1 As Single, L2 As Single, L As Single,
                                     Wserv1 As Single, Wserv2 As Single)
        ' Ec en MPa → convertir a kN/m² para usar con L en m y w en kN/m
        Dim Ec_MPa As Single = 4700.0F * Math.Sqrt(fc)
        Dim Ec_kNm2 As Single = Ec_MPa * 1000.0F   ' 1 MPa = 1 kN/m² × 1000? No: 1 MPa = 1000 kN/m²
        ' 1 MPa = 1 N/mm² = 1000 kN/m²
        Proyecto.Ec = Ec_MPa

        Dim d As Single = h - rec   ' peralte efectivo (m)

        ' Inercia bruta por ancho b (m^4)
        Dim Ig As Single = CalcularIg(b, h)
        Proyecto.Ig = Ig

        ' fr = 0.62*√fc (MPa) → Mcr en kN·m con Ig en m^4
        ' Mcr = fr * Ig / yt    fr en kN/m², yt = h/2 en m
        Dim fr_MPa As Single = 0.62F * Math.Sqrt(fc)
        Dim fr_kNm2 As Single = fr_MPa * 1000.0F
        Proyecto.fr = fr_MPa
        Dim yt As Single = h / 2.0F
        Dim Mcr As Single = fr_kNm2 * Ig / yt    ' kN·m
        Proyecto.Mcr = Mcr

        ' As en m² (usar el calculado para la sección de estudio)
        Dim As_m2 As Single = Proyecto.Acero_Flexion / 1.0E6F  ' de mm² a m²
        Dim n As Single = 200000.0F / Ec_MPa  ' n = Es/Ec (Es=200000 MPa)
        Dim Icr As Single = CalcularIcr(b, d, As_m2, n)
        Proyecto.Icr = Icr

        Dim Ma As Single = Proyecto.Mu   ' momento máximo de diseño (kN·m)
        Dim Ie As Single = CalcularIe(Mcr, Ma, Ig, Icr)
        Proyecto.Ie = Ie

        ' Carga de servicio equivalente (kN/m) para el tramo total L
        Dim Weq As Single = CargaEquivalente(Wserv1, L1, Wserv2, L2)
        Proyecto.W_Equivalente = Weq

        ' Deflexión inmediata (m) → convertir a mm
        Dim delta_m As Single = CalcularDeflexion(Weq, L, Ec_kNm2, Ie)
        Proyecto.Delta_Inmediata = delta_m * 1000.0F   ' mm

        ' Admisible L/360 para deflexión inmediata
        Proyecto.Delta_Adm_360 = (L / 360.0F) * 1000.0F  ' mm
        Proyecto.Verif_Deflexion_360 = (Proyecto.Delta_Inmediata <= Proyecto.Delta_Adm_360)

        ' Deflexión largo plazo: λ = 2.0 (sin refuerzo de compresión, NSR-10 C.9.5.2.5)
        Proyecto.Delta_LP = Proyecto.Delta_Inmediata * 2.0F   ' mm
        ' Límite según condición de susceptibilidad del elemento (NSR-10 Tabla C.9.5(b))
        Dim limitLP As Single = If(Proyecto.ElementosSusceptibles, 480.0F, 240.0F)
        Proyecto.Delta_Adm_480 = (L / limitLP) * 1000.0F
        Proyecto.Verif_Deflexion_480 = (Proyecto.Delta_LP <= Proyecto.Delta_Adm_480)
    End Sub

    Private Sub EvaluarDobleRefuerzo(h As Single, fy As Single, b As Single)
        ' Criterio: h ≥ 0.20 m → doble capa recomendada (ACI 318 R.9.8.1)
        Dim por_espesor As Boolean = (h >= 0.20F)
        ' Criterio adicional: cuantía > 0.5*ρ_bal
        Dim rho_bal As Single = 0.85F * 0.85F * (Proyecto.fc / fy) * (600.0F / (600.0F + fy))
        Dim por_cuantia As Boolean = (Proyecto.Cuantia_Flexion > 0.5F * rho_bal)

        Proyecto.RequiereDobleRefuerzo = por_espesor OrElse por_cuantia

        ' ρ de retracción total (NSR-10 C.7.12.2.1b)
        Dim rho_ret As Single = If(fy >= 420, 0.0018F, 0.002F)

        If Proyecto.RequiereDobleRefuerzo Then
            ' Con doble capa: el acero de retracción total se reparte entre las dos capas
            ' → cada capa lleva la mitad (NSR-10 C.7.12.3 permite distribuirlo en ambos lechos)
            Dim rho_porCapa = rho_ret / 2.0F
            Proyecto.Acero_Temperatura = rho_porCapa * b * h * 1.0E6F          ' mm² — capa inferior
            Proyecto.Cuantia_Superior = rho_porCapa
            Proyecto.Acero_Superior_Requerido = rho_porCapa * b * h * 1.0E6F   ' mm² — capa superior
        Else
            ' Capa única: todo el acero de retracción va en la capa inferior
            Proyecto.Acero_Temperatura = rho_ret * b * h * 1.0E6F
            Proyecto.Cuantia_Superior = rho_ret
            Proyecto.Acero_Superior_Requerido = rho_ret * b * h * 1.0E6F
        End If

        ' Recalcular separación de temperatura con el As_req actualizado
        If C_BarraTemperatura IsNot Nothing AndAlso C_BarraTemperatura.SelectedIndex >= 0 Then
            Dim aBarTemp As Single = AreaRefuerzo(C_BarraTemperatura.Text)
            Proyecto.S_Temperatura = Math.Round(Math.Min(0.45F, aBarTemp / Proyecto.Acero_Temperatura), 2)
        End If

        ' Separación tentativa capa superior
        If Proyecto.RequiereDobleRefuerzo AndAlso C_RefSup_Barra IsNot Nothing AndAlso C_RefSup_Barra.SelectedIndex >= 0 Then
            Dim AreaBarra As Single = AreaRefuerzo(C_RefSup_Barra.Text)
            Proyecto.S_Superior = Math.Round(Math.Min(0.45F, AreaBarra / Proyecto.Acero_Superior_Requerido), 3)
        End If
    End Sub

#End Region

#Region "Refuerzo inferior — handlers existentes"

    Private Sub C_BarraTemperatura_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_BarraTemperatura.SelectedIndexChanged
        Try
            If C_BarraTemperatura.Text <> String.Empty Then
                Dim Acero_BarraTemperatura As Single = AreaRefuerzo(C_BarraTemperatura.Text)
                Proyecto.Barra_Temperatura = C_BarraTemperatura.Items.IndexOf(C_BarraTemperatura.Text)
                Proyecto.S_Temperatura = Math.Round(Math.Min(0.45, Acero_BarraTemperatura / Proyecto.Acero_Temperatura), 2)
                T_SRequeridaTemperatura.Text = Proyecto.S_Temperatura
            End If
        Catch ex As Exception
            Logger.Warning("Form_04_Escaleras.C_BarraTemperatura_SelectedIndexChanged",
                           "Error al calcular separación de temperatura con barra: " & C_BarraTemperatura.Text)
        End Try
    End Sub

    Private Sub C_BarraFlexion_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_BarraFlexion.SelectedIndexChanged
        Try
            If C_BarraFlexion.Text <> String.Empty Then
                Dim Acero_BarraFlexion As Single = AreaRefuerzo(C_BarraFlexion.Text)
                Proyecto.Barra_Flexion = C_BarraFlexion.Items.IndexOf(C_BarraFlexion.Text)
                Proyecto.S_Flexion = Math.Round(Math.Min(0.45, Acero_BarraFlexion / Proyecto.Acero_Flexion), 2)
                T_SRequeridaFlexion.Text = Proyecto.S_Flexion
            End If
        Catch ex As Exception
            Logger.Warning("Form_04_Escaleras.C_BarraFlexion_SelectedIndexChanged",
                           "Error al calcular separación a flexión con barra: " & C_BarraFlexion.Text)
        End Try
    End Sub

    Private Sub T_SColocadaTemperatura_TextChanged(sender As Object, e As EventArgs) Handles T_SColocadaTemperatura.TextChanged
        Try
            Dim S_Colocada As Single = Convert.ToSingle(T_SColocadaTemperatura.Text)
            If S_Colocada > 0 Then
                Proyecto.Cuantia_Temperaruta_Colocada = (Proyecto.A_Estudio / S_Colocada) * AreaRefuerzo(C_BarraTemperatura.Text) / (Proyecto.A_Estudio * Proyecto.h * 1000000)
                Proyecto.S_Temperatura_Colocada = S_Colocada
                Dim cuantiaRefTemp = Proyecto.Cuantia_Temperatura * If(Proyecto.RequiereDobleRefuerzo, 0.5F, 1.0F)
                If Proyecto.Cuantia_Temperaruta_Colocada >= 0.9 * cuantiaRefTemp Then
                    CasillaCumple(T_VerificacionTemperatura) : T_VerificacionTemperatura.Text = "Cumple"
                Else
                    CasillaNocumple(T_VerificacionTemperatura) : T_VerificacionTemperatura.Text = "No cumple"
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub T_SColocadaFlexion_TextChanged(sender As Object, e As EventArgs) Handles T_SColocadaFlexion.TextChanged
        Try
            Dim S_Colocada As Single = Convert.ToSingle(T_SColocadaFlexion.Text)
            If S_Colocada > 0 Then
                Proyecto.Acero_Flexion_Colocado = (Proyecto.A_Estudio / S_Colocada) * AreaRefuerzo(C_BarraFlexion.Text)
                Proyecto.S_Flexion_Colocada = S_Colocada
                If Proyecto.Acero_Flexion_Colocado >= 0.9 * Proyecto.Acero_Flexion Then
                    CasillaCumple(T_VerificacionFlexion) : T_VerificacionFlexion.Text = "Cumple"
                Else
                    CasillaNocumple(T_VerificacionFlexion) : T_VerificacionFlexion.Text = "No cumple"
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "Refuerzo superior y límite LP — handlers"

    Private Sub C_LimiteLP_SelectedIndexChanged(sender As Object, e As EventArgs)
        If C_LimiteLP Is Nothing OrElse Proyecto.Delta_LP = 0 Then Return
        Try
            Proyecto.ElementosSusceptibles = (C_LimiteLP.SelectedIndex = 0)
            Dim limitLP As Single = If(Proyecto.ElementosSusceptibles, 480.0F, 240.0F)
            Proyecto.Delta_Adm_480 = (Proyecto.L_Total / limitLP) * 1000.0F
            Proyecto.Verif_Deflexion_480 = (Proyecto.Delta_LP <= Proyecto.Delta_Adm_480)
            RellenarDeflexiones()
        Catch ex As Exception
            Logger.Warning("Form_04_Escaleras.C_LimiteLP_SelectedIndexChanged", ex.Message)
        End Try
    End Sub

    Private Sub C_RefSup_Barra_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            If C_RefSup_Barra.Text <> String.Empty AndAlso Proyecto.Acero_Superior_Requerido > 0 Then
                Dim areaBarra As Single = AreaRefuerzo(C_RefSup_Barra.Text)
                Proyecto.Barra_Superior = C_RefSup_Barra.Items.IndexOf(C_RefSup_Barra.Text)
                Proyecto.S_Superior = Math.Round(Math.Min(0.45F, areaBarra / Proyecto.Acero_Superior_Requerido), 3)
                T_RefSup_SReq.Text = Format(Proyecto.S_Superior, "0.000")
            End If
        Catch ex As Exception
            Logger.Warning("Form_04_Escaleras.C_RefSup_Barra_SelectedIndexChanged", ex.Message)
        End Try
    End Sub

    Private Sub T_RefSup_SCol_TextChanged(sender As Object, e As EventArgs)
        Try
            Dim sCol As Single = Convert.ToSingle(T_RefSup_SCol.Text)
            If sCol > 0 AndAlso C_RefSup_Barra IsNot Nothing AndAlso C_RefSup_Barra.SelectedIndex >= 0 Then
                Dim areaBarra As Single = AreaRefuerzo(C_RefSup_Barra.Text)
                Proyecto.Acero_Superior_Colocado = (Proyecto.A_Estudio / sCol) * areaBarra
                Proyecto.Cuantia_Superior_Colocada = Proyecto.Acero_Superior_Colocado / (Proyecto.A_Estudio * Proyecto.h * 1000000)
                Proyecto.S_Superior_Colocada = sCol

                If Proyecto.Acero_Superior_Colocado >= 0.9F * Proyecto.Acero_Superior_Requerido Then
                    CasillaCumple(T_RefSup_Verif) : T_RefSup_Verif.Text = "Cumple"
                    Proyecto.Verificacion_Superior = True
                Else
                    CasillaNocumple(T_RefSup_Verif) : T_RefSup_Verif.Text = "No cumple"
                    Proyecto.Verificacion_Superior = False
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "Presentación"

    Public Sub Rellenar()

        Label43.Visible = True : T_NPuntos.Visible = True
        Label47.Visible = True : T_Vmax.Visible = True
        Label48.Visible = True : T_Mmax.Visible = True

        AddHandler P_Momento.Paint, AddressOf Me.P_Momento_Paint
        P_Momento.Refresh()
        AddHandler P_Cortante.Paint, AddressOf Me.P_Cortante_Paint
        P_Cortante.Refresh()

        ' ── Temperatura ──────────────────────────────────────────────────────
        T_CuantiaTemperatura.Text = Format(Proyecto.Cuantia_Temperatura, "##,##0.0000")
        T_AsTemperatura.Text = Math.Round(Proyecto.Acero_Temperatura, 0)
        T_SRequeridaTemperatura.Text = Format(Proyecto.S_Temperatura, "##,##0.000")
        T_SColocadaTemperatura.Text = Format(Proyecto.S_Temperatura_Colocada, "##,##0.000")
        Dim cuantiaRefTempRel = Proyecto.Cuantia_Temperatura * If(Proyecto.RequiereDobleRefuerzo, 0.5F, 1.0F)
        If Proyecto.Cuantia_Temperaruta_Colocada >= 0.9 * cuantiaRefTempRel Then
            CasillaCumple(T_VerificacionTemperatura) : T_VerificacionTemperatura.Text = "Cumple"
        Else
            CasillaNocumple(T_VerificacionTemperatura) : T_VerificacionTemperatura.Text = "No cumple"
        End If

        ' ── Flexión inferior ─────────────────────────────────────────────────
        T_MuFlexion.Text = Math.Round(Proyecto.Momentos.Max, 2)
        T_CuantiaFlexion.Text = Format(Proyecto.Cuantia_Flexion, "##,##0.0000")
        T_AsFlexion.Text = Math.Round(Proyecto.Acero_Flexion, 0)
        T_SRequeridaFlexion.Text = Format(Proyecto.S_Flexion, "##,##0.000")
        T_SColocadaFlexion.Text = Format(Proyecto.S_Flexion_Colocada, "##,##0.000")
        If Proyecto.Acero_Flexion_Colocado >= 0.9 * Proyecto.Acero_Flexion Then
            CasillaCumple(T_VerificacionFlexion) : T_VerificacionFlexion.Text = "Cumple"
        Else
            CasillaNocumple(T_VerificacionFlexion) : T_VerificacionFlexion.Text = "No cumple"
        End If

        ' ── Cortante ─────────────────────────────────────────────────────────
        T_Vu.Text = Format(Proyecto.Vu, "##,##0.00")
        T_Vc.Text = Format(Proyecto.Vc, "##,##0.00")
        If Proyecto.Vc >= Proyecto.Vu Then
            Proyecto.Verificacion_Cortante = True
            CasillaCumple(T_VerificacionCortante) : T_VerificacionCortante.Text = "Cumple"
        Else
            Proyecto.Verificacion_Cortante = False
            CasillaNocumple(T_VerificacionCortante) : T_VerificacionCortante.Text = "No cumple"
        End If

        ' ── TabPage2: Refuerzo Superior ──────────────────────────────────────
        If T_RefSup_Estado IsNot Nothing Then
            If Proyecto.RequiereDobleRefuerzo Then
                T_RefSup_Estado.Text = "Se requiere doble capa"
                T_RefSup_Estado.BackColor = System.Drawing.Color.FromArgb(255, 230, 153)

                Dim motivos As New List(Of String)
                If Proyecto.h >= 0.20F Then motivos.Add("h " & U_GE & " 20 cm")
                Dim rho_bal As Single = 0.85F * 0.85F * (Proyecto.fc / Proyecto.fy) * (600.0F / (600.0F + Proyecto.fy))
                If Proyecto.Cuantia_Flexion > 0.5F * rho_bal Then motivos.Add(U_RHO & " > 0.5" & U_RHO & ChrW(8203) & "_bal")
                T_RefSup_Motivo.Text = String.Join("  |  ", motivos)
            Else
                T_RefSup_Estado.Text = "No se requiere doble capa"
                T_RefSup_Estado.BackColor = System.Drawing.Color.FromArgb(198, 239, 206)
                T_RefSup_Motivo.Text = "h < 20 cm  y  " & U_RHO & " " & U_LE & " 0.5" & U_RHO & "_bal"
            End If

            T_RefSup_AsReq.Text = Math.Round(Proyecto.Acero_Superior_Requerido, 1).ToString() & " mm" & U_SUP2
            T_RefSup_SReq.Text = Format(Proyecto.S_Superior, "0.000")
            T_RefSup_SCol.Text = Format(Proyecto.S_Superior_Colocada, "0.000")
            If C_RefSup_Barra IsNot Nothing Then C_RefSup_Barra.SelectedIndex = Math.Max(0, Proyecto.Barra_Superior)

            If Proyecto.S_Superior_Colocada > 0 Then
                If Proyecto.Verificacion_Superior Then
                    CasillaCumple(T_RefSup_Verif) : T_RefSup_Verif.Text = "Cumple"
                Else
                    CasillaNocumple(T_RefSup_Verif) : T_RefSup_Verif.Text = "No cumple"
                End If
            End If
        End If

        ' ── TabPage2: Deflexiones ────────────────────────────────────────────
        RellenarDeflexiones()
    End Sub

    Private Sub RellenarDeflexiones()
        If T_Ec Is Nothing Then Return

        ' Parámetros de cálculo
        T_Ec.Text = Format(Proyecto.Ec, "0.0")
        T_fr.Text = Format(Proyecto.fr, "0.000")
        T_Mcr.Text = Format(Proyecto.Mcr, "0.00")
        T_Ig.Text = Format(Proyecto.Ig * 1.0E8F, "0.0")    ' m⁴ → cm⁴
        T_Icr.Text = Format(Proyecto.Icr * 1.0E8F, "0.0")
        T_Ie.Text = Format(Proyecto.Ie * 1.0E8F, "0.0")
        T_Weq.Text = Format(Proyecto.W_Equivalente, "0.00")

        ' Deflexión inmediata — límite siempre L/360 para pisos (NSR-10 Tab. C.9.5b)
        T_DeltaInm.Text = Format(Proyecto.Delta_Inmediata, "0.00")
        T_Adm360.Text = Format(Proyecto.Delta_Adm_360, "0.00")
        If Proyecto.Verif_Deflexion_360 Then
            CasillaCumple(T_Verif360) : T_Verif360.Text = "Cumple"
        Else
            CasillaNocumple(T_Verif360) : T_Verif360.Text = "No cumple"
        End If

        ' Deflexión largo plazo — límite según susceptibilidad
        T_Lambda.Text = "2.00"
        T_DeltaLP.Text = Format(Proyecto.Delta_LP, "0.00")
        Dim limitStr As String = If(Proyecto.ElementosSusceptibles, "L/480", "L/240")
        T_Adm480.Text = Format(Proyecto.Delta_Adm_480, "0.00") & "  (" & limitStr & ")"
        If Proyecto.Verif_Deflexion_480 Then
            CasillaCumple(T_Verif480) : T_Verif480.Text = "Cumple"
        Else
            CasillaNocumple(T_Verif480) : T_Verif480.Text = "No cumple"
        End If

        ' Sincronizar ComboBox con lo almacenado en el proyecto
        If C_LimiteLP IsNot Nothing Then
            C_LimiteLP.SelectedIndex = If(Proyecto.ElementosSusceptibles, 0, 1)
        End If
    End Sub

    Public Sub Llenar_Celdas()
        T_fc.Text = Proyecto.fc
        T_fy.Text = Proyecto.fy
        T_C_Impuesta.Text = Proyecto.C_Imp
        T_C_Viva.Text = Proyecto.C_Viv
        T_PesoConcretoReforzado.Text = Proyecto.P_ConR
        T_PesoConcreto.Text = Proyecto.P_Con

        T_Huella.Text = Proyecto.Huella
        T_Contrahuella.Text = Proyecto.Contrahuella
        T_NPeldaños.Text = Proyecto.N_Peldanos
        T_LPeldaños.Text = Proyecto.L_Peldanos
        T_LDescanso.Text = Proyecto.L_Descanso
        T_L.Text = Proyecto.L_Total
        T_AEscalera.Text = Proyecto.A_Escalera
        T_AEstudio.Text = Proyecto.A_Estudio
        T_Recubrimiento.Text = Proyecto.Recubrimiento
        T_H.Text = Proyecto.h

        C_BarraFlexion.SelectedIndex = Proyecto.Barra_Flexion
        C_BarraTemperatura.SelectedIndex = Proyecto.Barra_Temperatura
        If C_RefSup_Barra IsNot Nothing Then
            C_RefSup_Barra.SelectedIndex = Math.Max(0, Proyecto.Barra_Superior)
        End If
        If T_RefSup_SCol IsNot Nothing Then
            T_RefSup_SCol.Text = Format(Proyecto.S_Superior_Colocada, "0.000")
        End If
        If C_LimiteLP IsNot Nothing Then
            C_LimiteLP.SelectedIndex = If(Proyecto.ElementosSusceptibles, 0, 1)
        End If
    End Sub

#End Region

#Region "Gráficos"

    Public Sub P_Momento_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        e.Graphics.Clear(P_Momento.BackColor)
        If Proyecto.Abscisas.Count < 2 OrElse Proyecto.Momentos.Max <= 0 Then Return

        Dim H_Cuadro As Single = P_Momento.Height
        Dim B_Cuadro As Single = P_Momento.Width
        Dim Escala_X As Single = B_Cuadro / (Proyecto.L_Total * 1.01)
        Dim Escala_Y As Single = H_Cuadro / (Proyecto.Momentos.Max * 2.05)
        Dim Cy As Single = H_Cuadro / 2

        Dim Lapiz_Negro As New Pen(Color.Black) : Lapiz_Negro.Width = 2
        Dim Lapiz_Azul As New Pen(Color.Blue) : Lapiz_Azul.Width = 1

        g.DrawLine(Lapiz_Negro, New PointF(Proyecto.Abscisas(0) * Escala_X, Cy),
                                New PointF(Proyecto.Abscisas(Proyecto.Abscisas.Count - 1) * Escala_X, Cy))

        Dim M_Point As New List(Of PointF)
        For i = 0 To Proyecto.Abscisas.Count - 1
            Dim P_1 As New PointF(Proyecto.Abscisas(i) * Escala_X, Cy + Proyecto.Momentos(i) * Escala_Y)
            M_Point.Add(P_1)
            If i > 0 Then g.DrawLine(Lapiz_Azul, M_Point(i - 1), M_Point(i))
            g.DrawLine(Lapiz_Azul, P_1, New PointF(P_1.X, Cy))
        Next
    End Sub

    Public Sub P_Cortante_Paint(ByVal sender As Object, ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        e.Graphics.Clear(P_Cortante.BackColor)
        If Proyecto.Abscisas.Count < 2 OrElse Proyecto.Cortantes.Max <= 0 Then Return

        Dim H_Cuadro As Single = P_Cortante.Height
        Dim B_Cuadro As Single = P_Cortante.Width
        Dim Escala_X As Single = B_Cuadro / (Proyecto.L_Total * 1.01)
        Dim Escala_Y As Single = H_Cuadro / (Proyecto.Cortantes.Max * 2.05)
        Dim Cy As Single = H_Cuadro / 2
        Dim Cx As Single = 5

        Dim Lapiz_Negro As New Pen(Color.Black) : Lapiz_Negro.Width = 2
        Dim Lapiz_Azul As New Pen(Color.Blue) : Lapiz_Azul.Width = 1

        g.DrawLine(Lapiz_Negro, New PointF(Proyecto.Abscisas(0) * Escala_X + Cx, Cy),
                                New PointF(Proyecto.Abscisas(Proyecto.Abscisas.Count - 1) * Escala_X + Cx, Cy))

        Dim M_Point As New List(Of PointF)
        For i = 0 To Proyecto.Abscisas.Count - 1
            Dim P_1 As New PointF(Proyecto.Abscisas(i) * Escala_X + Cx, Cy + Proyecto.Cortantes(i) * Escala_Y)
            M_Point.Add(P_1)
            If i > 0 Then g.DrawLine(Lapiz_Azul, M_Point(i - 1), M_Point(i))
            g.DrawLine(Lapiz_Azul, P_1, New PointF(P_1.X, Cy))
        Next
    End Sub

#End Region

#Region "Guardar / Abrir"

    Private Sub ToolStripMenuItem5_Click(sender As Object, e As EventArgs) Handles SaveAs_Escaleras.Click
        GuardarProyecto(Proyecto, "RevisiónEscaleras")
    End Sub

    Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs) Handles Save_Escaleras.Click
        Try
            If Proyecto.Ruta = String.Empty Then
                GuardarProyecto(Proyecto, "RevisiónEscaleras")
            Else
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            End If
        Catch ex As Exception
            Logger.Critical(ex, "Form_04_Escaleras.ToolStripMenuItem4_Click",
                            "No se pudo guardar el proyecto de escaleras. Verifique permisos de escritura.")
        End Try
    End Sub

    Private Sub ToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles Open_Escaleras.Click
        Open()
    End Sub

    Public Sub Open()
        Dim dlg As New OpenFileDialog
        dlg.Filter = "Archivo|*.esm"
        dlg.Title = "Abrir Archivo"
        dlg.ShowDialog()
        If dlg.FileName <> String.Empty Then
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto_Escaleras)(dlg.FileName)
            Llenar_Celdas()
            Rellenar()
        End If
    End Sub

#End Region

#Region "Utilidades"

    Public Function Funcion_Multiplo(ByVal Valor As Single, ByVal Multiplo As Single) As Single
        Dim Tol As Single = 1
        Dim H As Single = 0
        For i = Multiplo To 10 * Multiplo Step Multiplo
            If Math.Abs(i - Valor) < Tol Then
                Tol = Math.Abs(i - Valor)
                H = i
            End If
        Next
        Return Math.Round(H, 2)
    End Function

#End Region

End Class
