Imports ARCO.Funciones_00_Varias

' ═══════════════════════════════════════════════════════════════════════════════
'  Form_11_00_InfoNervios — Ingreso de refuerzo por tramo de nervio
' ═══════════════════════════════════════════════════════════════════════════════
Public Class Form_11_00_InfoNervios
    Inherits Form

    Public Shared Proyecto As Proyecto

    Private _svc As New NervioService()
    Private _joints As Dictionary(Of String, cJoint)
    Private _nervioActual As cNervio
    Private _tramoActual As cFrameNervio
    Private _cargando As Boolean = False

    ' ── Controles ─────────────────────────────────────────────────────────────
    Private WithEvents CmbNervio As New ComboBox()
    Private WithEvents LstTramos As New ListBox()
    Private WithEvents GrpGeom As New GroupBox()
    Private WithEvents TbBw As New TextBox()
    Private WithEvents TbH As New TextBox()
    Private WithEvents TbRec As New TextBox()
    Private WithEvents TbFc As New TextBox()
    Private WithEvents TbFy As New TextBox()
    Private WithEvents ChkSeccionT As New CheckBox()
    Private WithEvents TbBe As New TextBox()
    Private WithEvents TbTf As New TextBox()
    Private WithEvents TbPaso As New TextBox()
    Private WithEvents GrpRef As New GroupBox()
    Private WithEvents NudBarrasSup As New NumericUpDown()
    Private WithEvents CmbCalibreSup As New ComboBox()
    Private WithEvents NudBarrasInf As New NumericUpDown()
    Private WithEvents CmbCalibresInf As New ComboBox()
    Private WithEvents GrpEstribos As New GroupBox()
    Private WithEvents ChkTieneEstribos As New CheckBox()
    Private WithEvents NudRamas As New NumericUpDown()
    Private WithEvents CmbCalibresEst As New ComboBox()
    Private WithEvents TbSepEst As New TextBox()
    Private WithEvents GrpApoyos As New GroupBox()
    Private WithEvents TbApoyoI As New TextBox()
    Private WithEvents TbApoyoD As New TextBox()
    Private WithEvents GrpResultados As New GroupBox()
    Private LblMuNegI As New Label()
    Private LblMuPosC As New Label()
    Private LblMuNegD As New Label()
    Private LblVuI As New Label()
    Private LblVuD As New Label()
    Private LblCD_FI As New Label()
    Private LblCD_FC As New Label()
    Private LblCD_FD As New Label()
    Private LblCD_VI As New Label()
    Private LblCD_VD As New Label()
    Private LblAsMin As New Label()
    Private LblAsMax As New Label()
    Private LblAsSup As New Label()
    Private LblAsInf As New Label()
    Private LblBe As New Label()
    Private WithEvents BtnGuardar As New Button()
    Private WithEvents BtnCalcular As New Button()
    Private WithEvents BtnAnterior As New Button()
    Private WithEvents BtnSiguiente As New Button()

    Public Sub New()
        InitUI()
    End Sub

    Private Sub InitUI()
        Me.Text = "Módulo 11 — Refuerzo de Nervios"
        Me.Size = New System.Drawing.Size(1100, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New System.Drawing.Font("Segoe UI", 9)

        ' ── Panel izquierdo: selección ──────────────────────────────────────
        Dim panLeft As New Panel() With {
            .Dock = DockStyle.Left, .Width = 280, .Padding = New Padding(6)}

        Dim lbl1 As New Label() With {.Text = "Nervio:", .Location = New Point(6, 6), .AutoSize = True}
        CmbNervio.Location = New Point(6, 24)
        CmbNervio.Size = New Size(265, 22)
        CmbNervio.DropDownStyle = ComboBoxStyle.DropDownList

        Dim lbl2 As New Label() With {.Text = "Tramos:", .Location = New Point(6, 54), .AutoSize = True}
        LstTramos.Location = New Point(6, 72)
        LstTramos.Size = New Size(265, 560)

        panLeft.Controls.AddRange(New Control() {lbl1, CmbNervio, lbl2, LstTramos})

        ' ── Panel central: geometría + materiales ──────────────────────────
        Dim panCenter As New Panel() With {
            .Dock = DockStyle.Fill, .Padding = New Padding(6)}

        ' Geometría
        GrpGeom.Text = "Geometría y Materiales"
        GrpGeom.Location = New Point(6, 6)
        GrpGeom.Size = New Size(380, 220)

        Dim campos() As String = {"bw (m):", "h (m):", "Rec. (m):", "f'c (MPa):", "fy (MPa):"}
        Dim ctls() As Control = {TbBw, TbH, TbRec, TbFc, TbFy}
        For i As Integer = 0 To campos.Length - 1
            Dim lbC As New Label() With {.Text = campos(i), .Location = New Point(10, 24 + i * 32), .Size = New Size(90, 22), .TextAlign = ContentAlignment.MiddleRight}
            ctls(i).Location = New Point(106, 22 + i * 32)
            ctls(i).Size = New Size(90, 22)
            GrpGeom.Controls.Add(lbC)
            GrpGeom.Controls.Add(ctls(i))
        Next

        ChkSeccionT.Text = "Activar sección T"
        ChkSeccionT.Location = New Point(10, 188)
        ChkSeccionT.AutoSize = True
        GrpGeom.Controls.Add(ChkSeccionT)

        ' Sección T
        Dim GrpT As New GroupBox() With {.Text = "Sección T (opcional)", .Location = New Point(6, 232), .Size = New Size(380, 120)}
        Dim labelsT() As String = {"be (m) calculado:", "tf (m):", "Paso c-c (m):"}
        Dim ctlsT() As Control = {TbBe, TbTf, TbPaso}
        TbBe.ReadOnly = True
        TbBe.BackColor = Drawing.Color.FromArgb(240, 240, 240)
        For i As Integer = 0 To labelsT.Length - 1
            Dim lbT As New Label() With {.Text = labelsT(i), .Location = New Point(10, 24 + i * 28), .Size = New Size(120, 22), .TextAlign = ContentAlignment.MiddleRight}
            ctlsT(i).Location = New Point(136, 22 + i * 28)
            ctlsT(i).Size = New Size(100, 22)
            GrpT.Controls.Add(lbT)
            GrpT.Controls.Add(ctlsT(i))
        Next

        ' Refuerzo
        GrpRef.Text = "Refuerzo Longitudinal"
        GrpRef.Location = New Point(6, 358)
        GrpRef.Size = New Size(380, 130)
        Dim calibres() As String = {"#2", "#3", "#4", "#5", "#6", "#7", "#8", "#10"}
        For Each c In calibres
            CmbCalibreSup.Items.Add(c)
            CmbCalibresInf.Items.Add(c)
        Next
        CmbCalibreSup.DropDownStyle = ComboBoxStyle.DropDownList
        CmbCalibresInf.DropDownStyle = ComboBoxStyle.DropDownList
        NudBarrasSup.Minimum = 0 : NudBarrasSup.Maximum = 20
        NudBarrasInf.Minimum = 0 : NudBarrasInf.Maximum = 20

        Dim refLabels() As String = {"Sup. (#barras):", "Sup. (calibre):", "Inf. (#barras):", "Inf. (calibre):"}
        Dim refCtls() As Control = {NudBarrasSup, CmbCalibreSup, NudBarrasInf, CmbCalibresInf}
        For i As Integer = 0 To refLabels.Length - 1
            Dim lbR As New Label() With {.Text = refLabels(i), .Location = New Point(10, 24 + i * 24), .Size = New Size(100, 22), .TextAlign = ContentAlignment.MiddleRight}
            refCtls(i).Location = New Point(116, 22 + i * 24)
            refCtls(i).Size = New Size(100, 22)
            GrpRef.Controls.Add(lbR)
            GrpRef.Controls.Add(refCtls(i))
        Next

        ' Estribos
        GrpEstribos.Text = "Refuerzo Transversal (Estribos)"
        GrpEstribos.Location = New Point(6, 494)
        GrpEstribos.Size = New Size(380, 120)
        ChkTieneEstribos.Text = "Tiene estribos"
        ChkTieneEstribos.Location = New Point(10, 20)
        ChkTieneEstribos.AutoSize = True
        For Each c In calibres
            CmbCalibresEst.Items.Add(c)
        Next
        CmbCalibresEst.DropDownStyle = ComboBoxStyle.DropDownList
        NudRamas.Minimum = 1 : NudRamas.Maximum = 6

        Dim estLabels() As String = {"Ramas:", "Calibre:", "Sep. (m):"}
        Dim estCtls() As Control = {NudRamas, CmbCalibresEst, TbSepEst}
        For i As Integer = 0 To estLabels.Length - 1
            Dim lbE As New Label() With {.Text = estLabels(i), .Location = New Point(10, 44 + i * 24), .Size = New Size(80, 22), .TextAlign = ContentAlignment.MiddleRight}
            estCtls(i).Location = New Point(96, 42 + i * 24)
            estCtls(i).Size = New Size(100, 22)
            GrpEstribos.Controls.Add(lbE)
            GrpEstribos.Controls.Add(estCtls(i))
        Next
        GrpEstribos.Controls.Add(ChkTieneEstribos)

        panCenter.Controls.AddRange(New Control() {GrpGeom, GrpT, GrpRef, GrpEstribos})

        ' ── Panel derecho: apoyos + resultados ────────────────────────────
        Dim panRight As New Panel() With {
            .Dock = DockStyle.Right, .Width = 340, .Padding = New Padding(6)}

        GrpApoyos.Text = "Apoyos detectados (editable)"
        GrpApoyos.Location = New Point(6, 6)
        GrpApoyos.Size = New Size(320, 90)
        Dim apoyoLabels() As String = {"b_apoyo_Izq (m):", "b_apoyo_Der (m):"}
        Dim apoyoCtls() As Control = {TbApoyoI, TbApoyoD}
        For i As Integer = 0 To apoyoLabels.Length - 1
            Dim lbA As New Label() With {.Text = apoyoLabels(i), .Location = New Point(10, 24 + i * 28), .Size = New Size(120, 22), .TextAlign = ContentAlignment.MiddleRight}
            apoyoCtls(i).Location = New Point(136, 22 + i * 28)
            apoyoCtls(i).Size = New Size(80, 22)
            GrpApoyos.Controls.Add(lbA)
            GrpApoyos.Controls.Add(apoyoCtls(i))
        Next

        GrpResultados.Text = "Resultados C/D"
        GrpResultados.Location = New Point(6, 104)
        GrpResultados.Size = New Size(320, 360)

        Dim resLabels() As String = {
            "Mu_neg_I (kN·m):", "Mu_pos_C (kN·m):", "Mu_neg_D (kN·m):",
            "Vu_I (kN):", "Vu_D (kN):",
            "φMn sup (kN·m):", "φMn inf (kN·m):", "φVn (kN):",
            "As_min (cm²):", "As_max (cm²):",
            "As_sup prov. (cm²):", "As_inf prov. (cm²):",
            "C/D flex. Izq:", "C/D flex. Cen:", "C/D flex. Der:",
            "C/D cort. Izq:", "C/D cort. Der:"}
        Dim resCtls() As Label = {
            LblMuNegI, LblMuPosC, LblMuNegD, LblVuI, LblVuD,
            New Label(), New Label(), New Label(),
            LblAsMin, LblAsMax, LblAsSup, LblAsInf,
            LblCD_FI, LblCD_FC, LblCD_FD, LblCD_VI, LblCD_VD}

        For i As Integer = 0 To resLabels.Length - 1
            Dim lbRes As New Label() With {
                .Text = resLabels(i),
                .Location = New Point(6, 20 + i * 20),
                .Size = New Size(160, 18),
                .TextAlign = ContentAlignment.MiddleRight}
            resCtls(i).Location = New Point(170, 20 + i * 20)
            resCtls(i).Size = New Size(140, 18)
            resCtls(i).Text = "—"
            GrpResultados.Controls.Add(lbRes)
            GrpResultados.Controls.Add(resCtls(i))
        Next

        ' Botones
        BtnAnterior.Text = "◀ Anterior"
        BtnAnterior.Location = New Point(6, 480)
        BtnAnterior.Size = New Size(100, 28)

        BtnSiguiente.Text = "Siguiente ▶"
        BtnSiguiente.Location = New Point(110, 480)
        BtnSiguiente.Size = New Size(100, 28)

        BtnGuardar.Text = "Guardar tramo"
        BtnGuardar.Location = New Point(6, 514)
        BtnGuardar.Size = New Size(200, 28)
        BtnGuardar.BackColor = Drawing.Color.FromArgb(0, 120, 215)
        BtnGuardar.ForeColor = Drawing.Color.White
        BtnGuardar.Font = New Drawing.Font("Segoe UI", 9, Drawing.FontStyle.Bold)

        BtnCalcular.Text = "▶ Calcular tramo"
        BtnCalcular.Location = New Point(6, 548)
        BtnCalcular.Size = New Size(200, 28)
        BtnCalcular.BackColor = Drawing.Color.FromArgb(0, 150, 70)
        BtnCalcular.ForeColor = Drawing.Color.White

        panRight.Controls.AddRange(New Control() {GrpApoyos, GrpResultados,
                                                   BtnAnterior, BtnSiguiente,
                                                   BtnGuardar, BtnCalcular})

        Me.Controls.Add(panCenter)
        Me.Controls.Add(panRight)
        Me.Controls.Add(panLeft)
    End Sub

    Private Sub Form_11_00_InfoNervios_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Me.DesignMode Then Return
        Try
            Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch
        End Try
        _joints = Proyecto.Elementos.Joints.ToDictionary(Function(j) j.ElementLabel)
        CargarComboNervios()
    End Sub

    Private Sub CargarComboNervios()
        _cargando = True
        CmbNervio.DataSource = Nothing
        CmbNervio.DataSource = Proyecto.Elementos.Nervios.Elementos
        CmbNervio.DisplayMember = "Nombre"
        _cargando = False
        If CmbNervio.Items.Count > 0 Then CmbNervio.SelectedIndex = 0
    End Sub

    Private Sub CmbNervio_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CmbNervio.SelectedIndexChanged
        If _cargando Then Return
        _nervioActual = TryCast(CmbNervio.SelectedItem, cNervio)
        If _nervioActual Is Nothing Then Return

        _cargando = True
        LstTramos.DataSource = Nothing
        LstTramos.DataSource = _nervioActual.Frames
        LstTramos.DisplayMember = "ObjectLabel"
        _cargando = False

        If LstTramos.Items.Count > 0 Then LstTramos.SelectedIndex = 0
    End Sub

    Private Sub LstTramos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LstTramos.SelectedIndexChanged
        If _cargando Then Return
        _tramoActual = TryCast(LstTramos.SelectedItem, cFrameNervio)
        MostrarTramo(_tramoActual)
    End Sub

    Private Sub MostrarTramo(fn As cFrameNervio)
        If fn Is Nothing Then Return
        _cargando = True

        TbBw.Text = fn.Bw.ToString("F3")
        TbH.Text = fn.H.ToString("F3")
        TbRec.Text = fn.Recubrimiento.ToString("F3")
        TbFc.Text = fn.fc.ToString("F1")
        TbFy.Text = fn.fy.ToString("F1")

        ChkSeccionT.Checked = fn.EsSeccionT
        TbBe.Text = fn.Be.ToString("F3")
        TbTf.Text = fn.Tf.ToString("F3")
        TbPaso.Text = fn.Paso.ToString("F3")

        NudBarrasSup.Value = fn.Barras_Sup
        CmbCalibreSup.SelectedItem = fn.Calibre_Sup
        NudBarrasInf.Value = fn.Barras_Inf
        CmbCalibresInf.SelectedItem = fn.Calibre_Inf

        ChkTieneEstribos.Checked = fn.TieneEstribos
        NudRamas.Value = fn.Estribo_Ramas
        CmbCalibresEst.SelectedItem = fn.Estribo_Calibre
        TbSepEst.Text = fn.Estribo_Sep.ToString("F3")

        TbApoyoI.Text = fn.B_Apoyo_I.ToString("F3")
        TbApoyoD.Text = fn.B_Apoyo_D.ToString("F3")

        MostrarResultados(fn)
        ActualizarBe(fn)

        _cargando = False
    End Sub

    Private Sub MostrarResultados(fn As cFrameNervio)
        If Not fn.Ref_Modificado Then
            For Each lbl In {LblMuNegI, LblMuPosC, LblMuNegD, LblVuI, LblVuD,
                              LblCD_FI, LblCD_FC, LblCD_FD, LblCD_VI, LblCD_VD,
                              LblAsMin, LblAsMax, LblAsSup, LblAsInf}
                lbl.Text = "—"
                lbl.ForeColor = Drawing.Color.Gray
            Next
            Return
        End If

        LblMuNegI.Text = fn.Mu_Neg_I.ToString("F2")
        LblMuPosC.Text = fn.Mu_Pos_C.ToString("F2")
        LblMuNegD.Text = fn.Mu_Neg_D.ToString("F2")
        LblVuI.Text = fn.Vu_I.ToString("F2")
        LblVuD.Text = fn.Vu_D.ToString("F2")
        LblAsMin.Text = fn.As_Min.ToString("F2")
        LblAsMax.Text = fn.As_Max.ToString("F2")
        LblAsSup.Text = fn.As_Prov_Sup.ToString("F2")
        LblAsInf.Text = fn.As_Prov_Inf.ToString("F2")

        Dim pares() As (lbl As Label, cd As Double) = {
            (LblCD_FI, fn.CD_Flex_Sup_I), (LblCD_FC, fn.CD_Flex_Inf_C),
            (LblCD_FD, fn.CD_Flex_Sup_D), (LblCD_VI, fn.CD_Cortante_I),
            (LblCD_VD, fn.CD_Cortante_D)}

        For Each p In pares
            p.lbl.Text = p.cd.ToString("F2")
            p.lbl.ForeColor = If(p.cd >= 1.0, Drawing.Color.DarkGreen,
                               If(p.cd >= 0.9, Drawing.Color.DarkOrange,
                                              Drawing.Color.Red))
        Next
    End Sub

    Private Sub ActualizarBe(fn As cFrameNervio)
        If fn.EsSeccionT Then
            fn.Be = _svc.CalcularBe(fn.Bw, fn.Tf, fn.Paso, fn.Longitud, fn.B_Apoyo_I, fn.B_Apoyo_D)
            TbBe.Text = fn.Be.ToString("F4")
            LblBe.Text = "be = " & (fn.Be * 100).ToString("F1") & " cm"
        End If
    End Sub

    Private Sub ChkSeccionT_CheckedChanged(sender As Object, e As EventArgs) Handles ChkSeccionT.CheckedChanged
        If _tramoActual IsNot Nothing AndAlso Not _cargando Then
            _tramoActual.EsSeccionT = ChkSeccionT.Checked
            ActualizarBe(_tramoActual)
        End If
    End Sub

    Private Sub BtnGuardar_Click(sender As Object, e As EventArgs) Handles BtnGuardar.Click
        If _tramoActual Is Nothing Then Return
        Try
            _tramoActual.Bw = ParseD(TbBw.Text)
            _tramoActual.H = ParseD(TbH.Text)
            _tramoActual.Recubrimiento = ParseD(TbRec.Text)
            _tramoActual.fc = ParseD(TbFc.Text)
            _tramoActual.fy = ParseD(TbFy.Text)
            _tramoActual.EsSeccionT = ChkSeccionT.Checked
            _tramoActual.Tf = ParseD(TbTf.Text)
            _tramoActual.Paso = ParseD(TbPaso.Text)
            _tramoActual.Barras_Sup = CInt(NudBarrasSup.Value)
            _tramoActual.Calibre_Sup = CmbCalibreSup.SelectedItem?.ToString()
            _tramoActual.Barras_Inf = CInt(NudBarrasInf.Value)
            _tramoActual.Calibre_Inf = CmbCalibresInf.SelectedItem?.ToString()
            _tramoActual.TieneEstribos = ChkTieneEstribos.Checked
            _tramoActual.Estribo_Ramas = CInt(NudRamas.Value)
            _tramoActual.Estribo_Calibre = CmbCalibresEst.SelectedItem?.ToString()
            _tramoActual.Estribo_Sep = ParseD(TbSepEst.Text)
            _tramoActual.B_Apoyo_I = ParseD(TbApoyoI.Text)
            _tramoActual.B_Apoyo_D = ParseD(TbApoyoD.Text)
            _tramoActual.Ref_Modificado = True

            ActualizarBe(_tramoActual)
        Catch ex As FormatException
            MessageBox.Show("Verifique los valores numéricos ingresados.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub BtnCalcular_Click(sender As Object, e As EventArgs) Handles BtnCalcular.Click
        BtnGuardar_Click(Nothing, Nothing)
        If _tramoActual Is Nothing Then Return
        Try
            ' Recalcular be antes del diseño
            If _tramoActual.EsSeccionT Then
                _tramoActual.Be = _svc.CalcularBe(_tramoActual.Bw, _tramoActual.Tf,
                                                    _tramoActual.Paso, _tramoActual.Longitud,
                                                    _tramoActual.B_Apoyo_I, _tramoActual.B_Apoyo_D)
            End If
            _svc.CalcularFlexion(_tramoActual)
            _svc.CalcularCortante(_tramoActual)
            _tramoActual.Cumple = _tramoActual.CD_Flex_Sup_I >= 0.9 AndAlso
                                   _tramoActual.CD_Flex_Inf_C >= 0.9 AndAlso
                                   _tramoActual.CD_Flex_Sup_D >= 0.9 AndAlso
                                   _tramoActual.CD_Cortante_I >= 0.9 AndAlso
                                   _tramoActual.CD_Cortante_D >= 0.9
            MostrarResultados(_tramoActual)
            TbBe.Text = _tramoActual.Be.ToString("F4")
        Catch ex As Exception
            Logger.Error(ex, "Form_11_00_InfoNervios.BtnCalcular_Click")
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnAnterior_Click(sender As Object, e As EventArgs) Handles BtnAnterior.Click
        If LstTramos.SelectedIndex > 0 Then LstTramos.SelectedIndex -= 1
    End Sub

    Private Sub BtnSiguiente_Click(sender As Object, e As EventArgs) Handles BtnSiguiente.Click
        If LstTramos.SelectedIndex < LstTramos.Items.Count - 1 Then
            LstTramos.SelectedIndex += 1
        End If
    End Sub

    Private Shared Function ParseD(txt As String) As Double
        Dim d As Double
        Double.TryParse(txt.Replace(",", "."), Globalization.NumberStyles.Any,
                        Globalization.CultureInfo.InvariantCulture, d)
        Return d
    End Function
End Class
