Imports System.IO
Imports ARCO.Funciones_00_Varias
Imports ARCO.Funciones_Reportes_PDF
Imports iTextSharp.text
Imports iTextSharp.text.pdf
Public Class Form_05_MurosNoEstructurales
    Public Shared Proyecto As New Proyecto_MNE
    Private Sub T_Hn_TextChanged(sender As Object, e As EventArgs) Handles T_Hn.TextChanged
        Try
            Dim Hn As Single = Convert.ToSingle(T_Hn.Text)
            If Hn > 0 Then
                T_Heq.Text = Math.Round(0.75 * Hn, 2)
            End If

        Catch ex As Exception

        End Try
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Dim Aa As Single = Convert.ToSingle(T_Aa.Text)
        Dim Av As Single = Convert.ToSingle(T_Av.Text)
        Dim Fa As Single = Convert.ToSingle(T_Fa.Text)
        Dim Fv As Single = Convert.ToSingle(T_Fv.Text)
        Dim Imp As Single = Convert.ToSingle(T_I.Text)
        Dim Sa As Single = Convert.ToSingle(T_Sa.Text)
        Dim As_ As Single = Aa * Fa
        Dim N_Pisos As Integer = Convert.ToInt32(T_NPisos.Text)
        Dim Hn As Single = Convert.ToSingle(T_Hn.Text)
        Dim Hx As Single = Math.Round(Convert.ToSingle(T_Hx.Text), 2)
        Dim Heq As Single = Convert.ToSingle(T_Heq.Text)
        Dim B As Single = Convert.ToSingle(T_B.Text)
        Dim Peso_Especifico As Single = Convert.ToSingle(T_PesoEspecifico.Text)
        Dim fm As Single = Convert.ToSingle(T_fm.Text)

        Dim Rp As Single = Convert.ToSingle(T_Rp.Text)
        Dim ap As Single = Convert.ToSingle(T_ap.Text)

        Dim B_Efec As Single = B

        If Op_SinInyectar.Checked = True Then
            If Format(B, "##,##0.00") = 0.1 Then
                B_Efec = 0.066
            ElseIf Format(B, "##,##0.00") = 0.12 Then
                B_Efec = 0.071
            ElseIf Format(B, "##,##0.00") = 0.15 Then
                B_Efec = 0.09
            End If
        End If

        Proyecto.Aa = Aa
        Proyecto.Av = Av
        Proyecto.Fa = Fa
        Proyecto.Fv = Fv
        Proyecto.Imp = Imp
        Proyecto.Sa = Sa
        Proyecto.As_ = As_
        Proyecto.Rp = Rp
        Proyecto.ap = ap
        Proyecto.fm = fm
        Proyecto.Peso_Especifico = Peso_Especifico
        Proyecto.B = B
        Proyecto.B_Efect = B_Efec
        Proyecto.N_Pisos = N_Pisos
        Proyecto.Hx = Hx
        Proyecto.Hn = Hn
        Proyecto.Heq = Heq

        Proyecto.Lista_Antepechos.Clear()
        Proyecto.Lista_Divisorios.Clear()

        Proyecto.Muro_Divisorio = True
        Proyecto.Muro_Antepecho = False

        For i = N_Pisos To 1 Step -1
            Dim Mp As Single = B * Peso_Especifico * Hx
            Dim Wm As Single = Mp
            Dim ax As Single
            Dim Hw = Hx
            If i * Hx <= Heq Then
                ax = As_ + (Sa - As_) * i * Hx / Heq
            Else
                ax = Sa * i * Hx / Heq
            End If
            Dim Fp As Single = Math.Max(ax * ap * Wm / Rp, Aa * Imp * Wm / 2)
            Dim Mu As Single = Fp * Hw / 8
            Dim Vu As Single = Fp / 2

            Dim Acero = DiseñoFlexion(420, fm, 1000, B * 1000, 0.5 * B * 1000, Mu, 1.4 / 420, 0.25 * Math.Sqrt(fm) / 420)
            Dim Area_Barra As Single = AreaRefuerzo(C_Refuerzo.Text)

            If Vu > Convert.ToSingle(T_Vu.Text) Then
                T_Vu.Text = Format(Vu, "##,##0.00")
                Proyecto.Vu_Divisorio = Vu
            End If

            Dim Nivel As New Proyecto_MNE.Divisorio
            Nivel.Piso = i
            Nivel.Hw = Hx
            Nivel.Hx = i * Hx
            Nivel.b = Proyecto.B
            Nivel.Wm = Wm
            Nivel.ax = ax
            Nivel.Fp = Fp
            Nivel.Presion = Fp / Hx
            Nivel.Mu = Mu
            Nivel.Vu = Vu
            Nivel.Acero = Acero
            Nivel.Separacion = Area_Barra / Acero

            Proyecto.Lista_Divisorios.Add(Nivel)
        Next

        Proyecto.Muro_Antepecho = True
        Proyecto.Muro_Divisorio = False

        For i = 0.6 To 2.0 Step 0.1
            Dim Mu As Single = 1.6 * i
            Dim Acero = DiseñoFlexion(420, fm, 1000, B * 1000, 0.5 * B * 1000, Mu, 1.4 / 420, 0.25 * Math.Sqrt(fm) / 420)
            Dim Area_Barra As Single = AreaRefuerzo(C_Refuerzo.Text)

            Dim Muro_ As New Proyecto_MNE.Antepecho
            Muro_.Altura = i
            Muro_.b = B
            Muro_.B_Efec = B_Efec
            Muro_.Am = 1 * B_Efec
            Muro_.Mu = Mu
            Muro_.Vu = 1.6
            Muro_.Acero = Acero
            Muro_.Separacion = Area_Barra / Acero

            Proyecto.Lista_Antepechos.Add(Muro_)

        Next
        T_Vu.Text = Format(1.6, "##,##0.00")
        Proyecto.Vu_Antepecho = 1.6
        'End If

        Proyecto.Vn = 0.6 * Proyecto.B_Efect * Math.Sqrt(Proyecto.fm) * 1000 / 6
        T_Vn.Text = Format(Proyecto.Vn, "##,##0.00")

        C_Refuerzo.SelectedIndex = 1

        TabControl1.SelectedIndex = 1

        Rellenar()

    End Sub

    Private Sub Op_Divisiorio_CheckedChanged(sender As Object, e As EventArgs) Handles Op_Inyectadas.CheckedChanged
        T_ap.Text = Format(1, "##,##0.0")
        T_Rp.Text = Format(1.5, "##,##0.0")
    End Sub

    Private Sub Op_Antepecho_CheckedChanged(sender As Object, e As EventArgs) Handles Op_SinInyectar.CheckedChanged
        T_ap.Text = Format(2.5, "##,##0.0")
        T_Rp.Text = Format(1.5, "##,##0.0")
    End Sub

    Private Sub C_Refuerzo_SelectedIndexChanged(sender As Object, e As EventArgs) Handles C_Refuerzo.SelectedIndexChanged

        Proyecto.Opcion_Barra_Flexion = C_Refuerzo.SelectedIndex

        Try
            Proyecto.Lista_Divisorios.Clear()

            For i = Proyecto.N_Pisos To 1 Step -1
                Dim Mp As Single = Proyecto.B * Proyecto.Peso_Especifico * Proyecto.Hx
                Dim Wm As Single = Mp
                Dim ax As Single
                Dim Hw = Proyecto.Hx
                If i * Proyecto.Hx <= Proyecto.Heq Then
                    ax = Proyecto.As_ + (Proyecto.Sa - Proyecto.As_) * i * Proyecto.Hx / Proyecto.Heq
                Else
                    ax = Proyecto.Sa * i * Proyecto.Hx / Proyecto.Heq
                End If
                Dim Fp As Single = Math.Max(ax * Proyecto.ap * Wm / Proyecto.Rp, Proyecto.Aa * Proyecto.Imp * Wm / 2)
                Dim Mu As Single = Fp * Hw / 8
                Dim Vu As Single = Fp / 2

                If Op_SinInyectar.Checked = True Then
                    Mu = Fp * Hw / 2
                    Vu = Fp
                End If

                Dim Acero = DiseñoFlexion(420, Proyecto.fm, 1000, Proyecto.B_Efect * 1000, 0, Mu, 1.4 / 420, 0.25 * Math.Sqrt(Proyecto.fm) / 420)
                Dim Area_Barra As Single = AreaRefuerzo(C_Refuerzo.Text)

                If Vu > Convert.ToSingle(T_Vu.Text) Then
                    Proyecto.Vu_Divisorio = Vu
                End If

                Dim Nivel As New Proyecto_MNE.Divisorio
                Nivel.Piso = i
                Nivel.Hw = Proyecto.Hx
                Nivel.Hx = i * Proyecto.Hx
                Nivel.b = Proyecto.B
                Nivel.Wm = Wm
                Nivel.ax = ax
                Nivel.Fp = Fp
                Nivel.Presion = Fp / Proyecto.Hx
                Nivel.Mu = Mu
                Nivel.Vu = Vu
                Nivel.Acero = Acero
                Nivel.Separacion = Area_Barra / Acero

                Proyecto.Lista_Divisorios.Add(Nivel)
            Next

            Proyecto.Lista_Antepechos.Clear()

            For i = 0.6 To 1.7 Step 0.1
                Dim Mu As Single = 1.6 * i
                Dim Acero = DiseñoFlexion(420, Proyecto.fm, 1000, Proyecto.B_Efect * 1000, 0, Mu, 1.4 / 420, 0.25 * Math.Sqrt(Proyecto.fm) / 420)
                Dim Area_Barra As Single = AreaRefuerzo(C_Refuerzo.Text)

                Dim Muro_ As New Proyecto_MNE.Antepecho
                Muro_.Altura = i
                Muro_.b = Proyecto.B
                Muro_.B_Efec = Proyecto.B_Efect
                Muro_.Am = 1 * Proyecto.B_Efect
                Muro_.Mu = Mu
                Muro_.Vu = 1.6
                Muro_.Acero = Acero
                Muro_.Separacion = Area_Barra / Acero

                Proyecto.Lista_Antepechos.Add(Muro_)
            Next
            Proyecto.Vu_Antepecho = 1.6

            Rellenar()

        Catch ex As Exception

        End Try

    End Sub

    Private Sub GuardarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Save_MNE.Click
        Try
            If Proyecto.Ruta = String.Empty Then
                GuardarProyecto(Proyecto, "RevisiónMurosNoEstructurales")
            Else
                Funciones_Programa.Serializar(Proyecto.Ruta, Proyecto)
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub GuardarComoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SaveAs_MNE.Click
        GuardarProyecto(Proyecto, "RevisiónMurosNoEstructurales")
    End Sub

    Private Sub AbrirToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Open_MNE.Click
        Open()
    End Sub

    Public Sub Open()
        Dim Open As New OpenFileDialog
        Open.Filter = "Archivo|*.esm"
        Open.Title = "Abrir Archivo"
        Open.ShowDialog()
        If Open.FileName <> String.Empty Then
            Proyecto = Funciones_Programa.DeSerializar(Of Proyecto_MNE)(Open.FileName)
            Llenar_Celdas()
            Rellenar()
        End If
    End Sub

    Public Sub Llenar_Celdas()
        T_Aa.Text = Proyecto.Aa
        T_Av.Text = Proyecto.Av
        T_Fa.Text = Proyecto.Fa
        T_Fv.Text = Proyecto.Fv
        T_I.Text = Proyecto.Imp
        T_Sa.Text = Proyecto.Sa
        T_Rp.Text = Proyecto.Rp
        T_ap.Text = Proyecto.ap
        T_fm.Text = Proyecto.fm
        T_PesoEspecifico.Text = Proyecto.Peso_Especifico
        T_B.Text = Proyecto.B
        T_NPisos.Text = Proyecto.N_Pisos
        T_Hx.Text = Proyecto.Hx
        T_Hn.Text = Proyecto.Hn
        T_Heq.Text = Proyecto.Heq

        C_Refuerzo.SelectedIndex = Proyecto.Opcion_Barra_Flexion
    End Sub
    Public Sub Rellenar()
        Tabla_Resultados.Rows.Clear()
        Tabla_Resultados.Columns.Clear()
        Tabla_Resultados.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Arial", 11, FontStyle.Bold)

        Tabla_Antepechos.Rows.Clear()
        Tabla_Antepechos.Columns.Clear()
        Tabla_Antepechos.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Arial", 11, FontStyle.Bold)
        Dim Vu As Single
        Try

            Tabla_Resultados.Columns.Add("Col1", "Piso")
            Tabla_Resultados.Columns.Add("Col2", "hw (m)")
            Tabla_Resultados.Columns.Add("Col3", "hx (m)")
            Tabla_Resultados.Columns.Add("Col4", "b (m)")
            Tabla_Resultados.Columns.Add("Col5", "Wm (kN/m)")
            Tabla_Resultados.Columns.Add("Col6", "ax")
            Tabla_Resultados.Columns.Add("Col7", "Fp (kN)")
            Tabla_Resultados.Columns.Add("Col8", "Presión (kN/m2)")
            Tabla_Resultados.Columns.Add("Col9", "Mu (kN.m/m)")
            Tabla_Resultados.Columns.Add("Col10", "Vu (kN/m)")
            Tabla_Resultados.Columns.Add("Col11", "Acero (mm2/m)")
            Tabla_Resultados.Columns.Add("Col12", "Separación (m)")

            For i = 0 To Proyecto.Lista_Divisorios.Count - 1

                Tabla_Resultados.Rows.Add(Proyecto.Lista_Divisorios(i).Piso,
                                                          Format(Proyecto.Lista_Divisorios(i).Hw, "##,##0.00"),
                                                          Format(Math.Round(Proyecto.Lista_Divisorios(i).Hx, 2), "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).b, "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).Wm, "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).ax, "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).Fp, "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).Presion, "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).Mu, "##,##0.00"),
                                                          Format(Proyecto.Lista_Divisorios(i).Vu, "##,##0.00"),
                                                          Math.Round(Proyecto.Lista_Divisorios(i).Acero, 0),
                                                          Format(Proyecto.Lista_Divisorios(i).Separacion, "##,##0.00"))

            Next
            T_Vu.Text = Format(Proyecto.Vu_Divisorio, "##,##0.00")
            Vu = Proyecto.Vu_Divisorio

            Tabla_Antepechos.Columns.Add("Col1", "Hx (m)")
            Tabla_Antepechos.Columns.Add("Col2", "b (m)")
            Tabla_Antepechos.Columns.Add("Col3", "Am (m2)")
            Tabla_Antepechos.Columns.Add("Col4", "Mu (kN.m/m)")
            Tabla_Antepechos.Columns.Add("Col5", "Vu (kN/m)")
            Tabla_Antepechos.Columns.Add("Col6", "Acero (mm2/m)")
            Tabla_Antepechos.Columns.Add("Col7", "Separación (m)")

            For i = 0 To Proyecto.Lista_Antepechos.Count - 1

                Tabla_Antepechos.Rows.Add(Format(Proyecto.Lista_Antepechos(i).Altura, "##,##0.00"),
                                                          Format(Proyecto.Lista_Antepechos(i).b, "##,##0.00"),
                                                          Format(Proyecto.Lista_Antepechos(i).Am, "##,##0.00"),
                                                          Format(Proyecto.Lista_Antepechos(i).Mu, "##,##0.00"),
                                                          Format(Proyecto.Lista_Antepechos(i).Vu, "##,##0.00"),
                                                          Math.Round(Proyecto.Lista_Antepechos(i).Acero, 0),
                                                          Format(Proyecto.Lista_Antepechos(i).Separacion, "##,##0.00"))

            Next

            T_Vu.Text = Format(Math.Max(Proyecto.Vu_Antepecho, Proyecto.Vu_Divisorio), "##,##0.00")
            Vu = Math.Max(Proyecto.Vu_Antepecho, Proyecto.Vu_Divisorio)

            T_Vn.Text = Format(Proyecto.Vn, "##,##0.00")

            If Vu < Proyecto.Vn Then
                T_Chequeo.Text = "Cumple"
                Casilla_Cumple(T_Chequeo)
            Else
                T_Chequeo.Text = "No cumple"
                Casilla_Nocumple(T_Chequeo)
            End If

        Catch ex As Exception

        End Try

    End Sub

    Private Sub Form_05_MurosNoEstructurales_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        GroupBox10.Left = TabPage1.Width / 2 - GroupBox10.Width / 2
        Dim Alto As Integer = TabPage1.Height - 2 * GroupBox10.Top - GroupBox10.Height
        Dim Dimension_min As Integer = Math.Min(Alto, TabPage1.Width / 2 - 50)
        GroupBox11.Width = Dimension_min
        GroupBox11.Height = Dimension_min / 1.25
        GroupBox12.Width = Dimension_min
        GroupBox12.Height = Dimension_min / 1.25

        GroupBox11.Left = TabPage1.Width / 2 - GroupBox11.Width - 8
        GroupBox12.Left = TabPage1.Width / 2 + 8

        GroupBox11.Top = GroupBox10.Top + GroupBox10.Height + 15
        GroupBox12.Top = GroupBox10.Top + GroupBox10.Height + 15
    End Sub

    '-------------- Reporte PDF -------------
    Private Sub ReportePDFToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Reporte_PDF_MNE.Click
        Dim pdfDoc As New Document
        pdfDoc.SetMargins(30.0F, 30.0F, 70.0F, 40.0F)

        Dim SaveAs As New SaveFileDialog
        SaveAs.Filter = "Archivo|*.pdf"
        SaveAs.Title = "Guardar Archivo"
        SaveAs.FileName = "RevisiónMurosNoEstructurales"
        SaveAs.ShowDialog()

        Dim pdfWrite As PdfWriter = PdfWriter.GetInstance(pdfDoc, New FileStream(SaveAs.FileName, FileMode.Create))
        Dim Events As New MypageEvents
        pdfWrite.PageEvent = Events

        pdfDoc.Open()

        '------ Fuentes para el documento PDF ----------------
        Dim Arial As BaseFont = BaseFont.CreateFont("c:\windows\fonts\arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED)
        Dim Arial_12_N As New Font(Arial, 12, FontStyle.Bold)
        Dim Arial_12_B As New Font(Arial, 12, FontStyle.Bold) : Arial_12_B.Color = BaseColor.WHITE
        Dim Arial_11_N As New Font(Arial, 11, FontStyle.Bold)
        Dim Arial_11_B As New Font(Arial, 11) : Arial_11_B.Color = BaseColor.WHITE
        Dim Arial_11 As New Font(Arial, 11)
        Dim Arial_10_N As New Font(Arial, 10, FontStyle.Bold)
        Dim Arial_10_B As New Font(Arial, 10) : Arial_10_B.Color = BaseColor.WHITE
        Dim Arial_10 As New Font(Arial, 10)

        '------ Fondos de Tablas -----------
        Dim Fondo_Titulo As New BaseColor(74, 74, 74)
        Dim Fondo_Celda As New BaseColor(229, 234, 238)

        pdfDoc.Add(Chunk.NEWLINE)

        Dim Parrafo As New Paragraph
        Parrafo.Alignment = Element.ALIGN_CENTER
        Parrafo.Font = Arial_12_N
        Parrafo.SpacingBefore = 6
        Parrafo.SpacingAfter = 12
        Parrafo.Add("ANEXO 1 - REVISIÓN DE MUROS NO ESTRUCTURALES")
        pdfDoc.Add(Parrafo)

        pdfDoc.Add(Texto_Parrafo("La revisión de los muros no estructurales fue realizado en un programa especializado desarrollado por la empresa EstrucMed Ingeniería Especializada, " &
                                 "donde se consideran las diferentes condiciones y tipos de muros del proyecto. En la Figura 1 se presenta una visualización de la interfaz del módulo " &
                                 "de muros no estructurales.", 40, 40, 6))


        ''Dim gr As Graphics = Me.CreateGraphics
        'Dim gr As Graphics = Me.CreateGraphics
        '' Tamaño de lo que queremos copiar
        'Dim fSize As Size = Me.Size
        ''fSize.Width = 1920
        ''fSize.Height = 1010
        '' Creamos el bitmap con el área que vamos a capturaDim bm As New Bitmap(fSize.Width, fSize.Height, gr)r
        '' En este caso, con el tamaño del formulario actual
        ''Dim bm As New Bitmap(fSize.Width, fSize.Height, gr)
        'Dim bm As New Bitmap(fSize.Width, fSize.Height, gr)
        ''bm = Bitmap(2480, 1368, gr)
        '' Un objeto Graphics a partir del bitmap
        'Dim gr2 As Graphics = Graphics.FromImage(bm)
        '' Copiar el área de la pantalla que ocupa el formulario
        ''gr2.CopyFromScreen(Screen.PrimaryScreen.Bounds.Width/2-Me.Location.X, Me.Location.Y, 0, 0, fSize)
        'gr2.CopyFromScreen(Screen.Bounds.Width / 2 - fSize.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2 - fSize.Height / 2, 0, 0, fSize)

        'Dim ms As MemoryStream = New MemoryStream()
        'bm.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp)
        'Dim Imagen As Image
        'Imagen = Image.GetInstance(ms.GetBuffer)
        'Imagen.ScalePercent(25.0F)
        'Imagen.Alignment = Element.ALIGN_CENTER
        'pdfDoc.Add(Imagen)

        P_Imagen_MuroAnalisis.Image.Save(Application.StartupPath & "\TiposMuros.bmp", System.Drawing.Imaging.ImageFormat.Bmp)
        P_ImagenMuros.Image.Save(Application.StartupPath & "\ImagenMuros.bmp", System.Drawing.Imaging.ImageFormat.Bmp)
        P_ImagenAntepecho.Image.Save(Application.StartupPath & "\ImagenAntepecho.bmp", System.Drawing.Imaging.ImageFormat.Bmp)


        Dim Tabla_TipoMuros As New PdfPTable(1)
        Tabla_TipoMuros.DefaultCell.Border = Rectangle.NO_BORDER
        Tabla_TipoMuros.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER
        Tabla_TipoMuros.KeepTogether = True
        Tabla_TipoMuros.SpacingAfter = 10

        Dim Imagen_TMuros As Image = Image.GetInstance(Application.StartupPath & "\TiposMuros.bmp")
        Imagen_TMuros.ScalePercent(10.0F)

        Dim Cell_Imagen As New PdfPCell
        Cell_Imagen.Image = Imagen_TMuros
        Cell_Imagen.BorderColor = BaseColor.WHITE
        Cell_Imagen.BorderWidth = 1
        Tabla_TipoMuros.AddCell(Cell_Imagen)

        pdfDoc.Add(Tabla_TipoMuros)
        pdfDoc.Add(Titulo_Figura("Figura 1.   ", "Tipos de muros.", "Figura"))

        Dim Imagen_MurosDiv As Image = Image.GetInstance(Application.StartupPath & "\ImagenMuros.bmp")
        Dim Imagen_MurosAntepecho As Image = Image.GetInstance(Application.StartupPath & "\ImagenAntepecho.bmp")

        Dim Table As New PdfPTable(2)
        Table.DefaultCell.Border = Rectangle.NO_BORDER
        Table.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER
        Table.KeepTogether = True
        Table.SpacingAfter = 10

        Table.AddCell(Insertar_Figura(Imagen_MurosDiv))
        Table.AddCell(Insertar_Figura(Imagen_MurosAntepecho))
        Table.AddCell(Titulo_Figura("Figura 2.   ", "Tipo de muro divisorio.", "Figura"))
        Table.AddCell(Titulo_Figura("Figura 3.   ", "Tipo de muro antepecho.", "Figura"))

        pdfDoc.Add(Table)

        'pdfDoc.Add(Titulo_Figura("Figura 1.   ", "ARCO - Muros no estructurales.", "Figura"))

        pdfDoc.Add(Texto_Parrafo("La verificación de los muros no estructurales fue desarrollada tal y como se presenta en el capítulo A.9 de la NSR-10; " &
                                 "con base en la información presentada en los planos, se verificó la capacidad de los siguientes tipos de muros:", 40, 40, 6))

        pdfDoc.Add(Texto_Parrafo("  - Muros Divisorios: Muros apoyados por lo menos en sus extremos superior e inferior.", 70, 40, 6))
        pdfDoc.Add(Texto_Parrafo("  - Muros en voladizo: Antepechos y muros ático.", 70, 40, 10))

        pdfDoc.Add(Texto_Parrafo("Para la revisión de los muros no estructurales se consideran los siguientes parámetros:", 40, 40, 6))
        pdfDoc.Add(Texto_Parrafo("  - ap = 1,0 según el numeral A.9.4.2.2 y la Tabla A.9.5-1 de la NSR-10.", 70, 40, 6))
        pdfDoc.Add(Texto_Parrafo("  - Rp = 1,5 según el numeral A.9.4.9 y la Tabla A.9.5-1 de la NSR-10.", 70, 40, 10))

        pdfDoc.Add(Texto_Parrafo("En la Tabla 1, se presentan aspectos generales de la estructura.", 40, 40, 10))

        Dim Tabla_DatosBasicos As New PdfPTable(2)
        Tabla_DatosBasicos.SpacingAfter = 12
        Tabla_DatosBasicos.SpacingBefore = 2
        Tabla_DatosBasicos.PaddingTop = 0
        Tabla_DatosBasicos.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER
        Tabla_DatosBasicos.DefaultCell.VerticalAlignment = Element.ALIGN_CENTER
        Tabla_DatosBasicos.HeaderRows = 1
        Tabla_DatosBasicos.KeepTogether = True
        Tabla_DatosBasicos.WidthPercentage = 60.0F

        Tabla_DatosBasicos.AddCell(Texto_Tabla("Parámetro", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_DatosBasicos.AddCell(Texto_Tabla("Valor", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))

        Tabla_DatosBasicos.AddCell(Texto_Tabla("Sa", Arial_10_N, Fondo_Celda, "Centrado", 2, 6))
        Tabla_DatosBasicos.AddCell(Texto_Tabla(Proyecto.Sa, Arial_10, Fondo_Celda, "Centrado", 2, 6))

        Tabla_DatosBasicos.AddCell(Texto_Tabla("As", Arial_10_N, Fondo_Celda, "Centrado", 2, 6))
        Tabla_DatosBasicos.AddCell(Texto_Tabla(Proyecto.As_, Arial_10, Fondo_Celda, "Centrado", 2, 6))

        Tabla_DatosBasicos.AddCell(Texto_Tabla("Altura (m)", Arial_10_N, Fondo_Celda, "Centrado", 2, 6))
        Tabla_DatosBasicos.AddCell(Texto_Tabla(Proyecto.Hn, Arial_10, Fondo_Celda, "Centrado", 2, 6))

        Tabla_DatosBasicos.AddCell(Texto_Tabla("Altura entrepiso típica (m)", Arial_10_N, Fondo_Celda, "Centrado", 2, 6))
        Tabla_DatosBasicos.AddCell(Texto_Tabla(Proyecto.Hx, Arial_10, Fondo_Celda, "Centrado", 2, 6))

        pdfDoc.Add(Titulo_Figura("Tabla 1.   ", "Aspectos generales de la estructura.", "Tabla"))
        pdfDoc.Add(Tabla_DatosBasicos)

        pdfDoc.Add(Texto_Parrafo("En la Tabla 2, se presentan los resultados obtenidos en la revisión de los muros divisorios.", 40, 40, 10))

        Dim Tabla_MurosDivisorios As New PdfPTable(8)
        Tabla_MurosDivisorios.SpacingAfter = 12
        Tabla_MurosDivisorios.SpacingBefore = 2
        Tabla_MurosDivisorios.PaddingTop = 0
        Tabla_MurosDivisorios.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER
        Tabla_MurosDivisorios.DefaultCell.VerticalAlignment = Element.ALIGN_CENTER
        Tabla_MurosDivisorios.HeaderRows = 1
        Tabla_MurosDivisorios.WidthPercentage = 85.0F

        pdfDoc.Add(Titulo_Figura("Tabla 2.   ", "Verificación de muros no estructurales - Muros divisorios.", "Tabla"))

        Tabla_MurosDivisorios.AddCell(Texto_Tabla("Piso", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("hw (m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("hx (m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("Wm (kN/m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("ax", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("Fp (kN)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("Mu (kN.m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosDivisorios.AddCell(Texto_Tabla("Vu (kN.m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))

        For i = 0 To Proyecto.Lista_Divisorios.Count - 1
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Proyecto.Lista_Divisorios(i).Piso, Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).Hw, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).Hx, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).Wm, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).ax, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).Fp, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).Mu, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosDivisorios.AddCell(Texto_Tabla(Format(Proyecto.Lista_Divisorios(i).Vu, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
        Next
        pdfDoc.Add(Tabla_MurosDivisorios)

        pdfDoc.Add(Texto_Parrafo("En la Tabla 3, se presentan los resultados obtenidos en la revisión de los muros antepecho.", 40, 40, 10))

        Dim Tabla_MurosAntepecho As New PdfPTable(5)
        Tabla_MurosAntepecho.SpacingAfter = 12
        Tabla_MurosAntepecho.SpacingBefore = 2
        Tabla_MurosAntepecho.PaddingTop = 0
        Tabla_MurosAntepecho.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER
        Tabla_MurosAntepecho.DefaultCell.VerticalAlignment = Element.ALIGN_CENTER
        Tabla_MurosAntepecho.HeaderRows = 1
        Tabla_MurosAntepecho.WidthPercentage = 85.0F

        pdfDoc.Add(Titulo_Figura("Tabla 3.   ", "Verificación de muros no estructurales - Muros antepecho.", "Tabla"))

        Tabla_MurosAntepecho.AddCell(Texto_Tabla("Altura (m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosAntepecho.AddCell(Texto_Tabla("Am (m2)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosAntepecho.AddCell(Texto_Tabla("Vu (kN.m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosAntepecho.AddCell(Texto_Tabla("Mu (kN.m)", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))
        Tabla_MurosAntepecho.AddCell(Texto_Tabla("Propuesta refuerzo", Arial_10_B, Fondo_Titulo, "Centrado", 2, 6))

        For i = 0 To Proyecto.Lista_Antepechos.Count - 1
            Tabla_MurosAntepecho.AddCell(Texto_Tabla(Format(Proyecto.Lista_Antepechos(i).Altura, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosAntepecho.AddCell(Texto_Tabla(Format(Proyecto.Lista_Antepechos(i).Am, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosAntepecho.AddCell(Texto_Tabla(Format(Proyecto.Lista_Antepechos(i).Vu, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosAntepecho.AddCell(Texto_Tabla(Format(Proyecto.Lista_Antepechos(i).Mu, "##,##0.00"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
            Tabla_MurosAntepecho.AddCell(Texto_Tabla(Convert.ToString(C_Refuerzo.Text & " @ " & Format(Proyecto.Lista_Antepechos(i).Separacion, "##,##0.00") & " m"), Arial_10, Fondo_Celda, "Centrado", 2, 6))
        Next
        pdfDoc.Add(Tabla_MurosAntepecho)

        pdfDoc.Add(Texto_Parrafo("Con base en los detalles presentados en los planos estructurales; y de acuerdo con lo presentado en la Tabla 2 y en la Tabla 3" &
                                 ", se concluye que para los dos tipos de muros se tiene un diseño adecuado.", 40, 40, 10))


        pdfDoc.Close()

        Process.Start(SaveAs.FileName)

        '----- Eliminar Archivos provisionales
        My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\TiposMuros.bmp")
        My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\ImagenMuros.bmp")
        My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\ImagenAntepecho.bmp")

    End Sub

    Public Function Insertar_Figura(ByVal Imagen As Image)
        Imagen.Alignment = Element.ALIGN_CENTER
        Imagen.ScalePercent(1000.0F)
        Return Imagen
    End Function
    Public Function Titulo_Figura(ByVal Figura1 As String, ByVal Figura2 As String, ByVal Tipo_Titulo As String)
        Dim arial As BaseFont = BaseFont.CreateFont("c:\windows\fonts\arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED)
        Dim Font_Figura As New Font(arial, 10)
        Dim Font_Titulo_Figura As New Font(arial, 10, FontStyle.Bold)
        Dim Parrafo As New Paragraph
        If Tipo_Titulo = "Figura" Then
            Parrafo.Alignment = Element.ALIGN_CENTER
        ElseIf Tipo_Titulo = "Tabla" Then
            Parrafo.Alignment = Element.ALIGN_JUSTIFIED
            Parrafo.IndentationLeft = 50
        End If
        Parrafo.Font = Font_Titulo_Figura
        Parrafo.Add(Figura1)
        Parrafo.Font = Font_Figura
        Parrafo.Add(Figura2)
        Return Parrafo
    End Function

    Public Class MypageEvents
        Inherits PdfPageEventHelper
        Public Overrides Sub onStartPage(ByVal Writer As PdfWriter, ByVal Documento As Document)
            Dim ms_Logo As MemoryStream = New MemoryStream()

            Form_Logos.P_Logo_EstrucMed.Image.Save(ms_Logo, System.Drawing.Imaging.ImageFormat.Png)

            Dim Imagen As Image = Image.GetInstance(ms_Logo.GetBuffer)
            Imagen.ScalePercent(10.0F)
            Imagen.SetAbsolutePosition(3 * Documento.PageSize.Width / 4 - 50, Documento.PageSize.Height - 60)
            Imagen.Alignment = Image.ALIGN_RIGHT

            Documento.Add(Imagen)
        End Sub
    End Class


End Class