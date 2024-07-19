Imports System.Windows.Forms.DataVisualization.Charting
Imports ARCO.eNumeradores
Imports Word = Microsoft.Office.Interop.Word
Imports Fun_Muros = ARCO.Funciones_Muros

Public Class ReporteInicial
    Public Shared proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto
    Public Sub Generar_Reporte()

        Dim wordApp As New Word.Application()

        Dim wordDoc As Word.Document = wordApp.Documents.Add()

        Dim plantillaPath As String = "C:\Users\vidal\Desktop\InsumosPruebas\Prueba_Reporte_1.docx"

        wordDoc = wordApp.Documents.Open(plantillaPath)

        '======== REEMPLAZAR TEXTO EN EL DOCUMENTO =============
        Dim fechaActual As String = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy")
        ReemplazarTexto(wordDoc, "{Fecha_Informe}", fechaActual)
        ReemplazarTexto(wordDoc, "{Nombre_Proyecto}", proyecto.Nombre)
        ReemplazarTexto(wordDoc, "{Direccion_Proyecto}", proyecto.Direccion)
        ReemplazarTexto(wordDoc, "{Propietario}", proyecto.Propietario)

        Dim valorSistemaEstructural As String = DirectorioSistemaEstructural.dSistemaEstructural(proyecto.SistemaEstructural).NameSistema
        ReemplazarTexto(wordDoc, "{Sistema_Estructural}", valorSistemaEstructural)
        ReemplazarTexto(wordDoc, "{N_Pisos}", proyecto.NumPisos)
        ReemplazarTexto(wordDoc, "{Area_Planta}", proyecto.Area_Planta)

        Dim Persona_Responsable As String = DirectorioResponsables.dResponsables(proyecto.Persona_Responsable).NombreCompleto
        ReemplazarTexto(wordDoc, "{Responsable}", Persona_Responsable)

        ReemplazarTexto(wordDoc, "{Densidad_X}", Math.Round(proyecto.Muros.Densidad_X * 100, 2))
        ReemplazarTexto(wordDoc, "{Densidad_Y}", Math.Round(proyecto.Muros.Densidad_Y * 100, 2))

        ReemplazarTexto(wordDoc, "{Nombre_Proyecto_Anexos}", proyecto.Nombre)

        Dim anchoCm As Double = 10
        Dim altoCm As Double = 3.5
        Dim dpi As Double = 300

        ' Convertir cm a pulgadas
        Dim anchoIn As Double = anchoCm / 2.54
        Dim altoIn As Double = altoCm / 2.54

        ' Convertir pulgadas a píxeles
        Dim anchoPx As Integer = Convert.ToInt32(anchoIn * dpi)
        Dim altoPx As Integer = Convert.ToInt32(altoIn * dpi)

        Dim Chart1 As New Chart
        Chart1.ChartAreas.Clear()

        Dim ChartArea1 As New ChartArea("ChartArea1")
        Chart1.ChartAreas.Add(ChartArea1)
        Dim ChartArea2 As New ChartArea("ChartArea2")
        Chart1.ChartAreas.Add(ChartArea2)

        Funciones_Muros.Grafico_PorcentajeMuros(Chart1, 18, 16, 14)

        ' Ajustar tamaño del gráfico
        Chart1.Width = anchoPx
        Chart1.Height = altoPx

        ' Ruta del archivo donde se guardará la imagen
        Dim rutaArchivo As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico.png"

        ' Guardar el gráfico como imagen
        Chart1.SaveImage(rutaArchivo, ChartImageFormat.Png)

        ' Reemplazar una imagen en el documento
        ReemplazarImagen(wordDoc, "{Grafico_SeleccionMurosProtagonicos}", rutaArchivo)

        ' PRUEBA DE GRAFICOS DE PLANTAS
        Dim chart_Muros_Protagonicos As New PictureBox
        Dim chart_Espesores As New PictureBox
        Dim chart_MurosPlanta As New PictureBox

        chart_Muros_Protagonicos.Height = altoPx
        chart_Muros_Protagonicos.Width = anchoPx

        chart_Espesores.Height = altoPx
        chart_Espesores.Width = anchoPx

        chart_MurosPlanta.Height = altoPx / 3
        chart_MurosPlanta.Width = anchoPx / 3

        Fun_Muros.CalcularGeometriaMuros()
        Fun_Muros.GraficosMurosPlanta(chart_Espesores, chart_Muros_Protagonicos)
        Fun_Muros.FiguraMurosPlanta(chart_MurosPlanta)

        Dim ruta_Muros As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico_Muros.png"
        Dim ruta_Espesor As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico_Espesor.png"
        Dim ruta_MurosPlanta As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico_MurosPlanta.png"
        chart_Muros_Protagonicos.Image.Save(ruta_Muros, System.Drawing.Imaging.ImageFormat.Png)
        chart_Espesores.Image.Save(ruta_Espesor, System.Drawing.Imaging.ImageFormat.Png)
        chart_MurosPlanta.Image.Save(ruta_MurosPlanta, System.Drawing.Imaging.ImageFormat.Png)

        ReemplazarImagen(wordDoc, "{Grafico_MurosProtagonicosPlanta}", ruta_Muros)
        ReemplazarImagen(wordDoc, "{Grafico_EspesorMurosPlanta}", ruta_Espesor)
        ReemplazarImagen(wordDoc, "{Grafico_FactorForma}", ruta_MurosPlanta)

        Dim Grafico_Derivas As New Chart
        Grafico_Derivas.Width = anchoPx / 2.5
        Grafico_Derivas.Height = altoPx
        Fun_Muros.GraficarDeriva(Grafico_Derivas)
        Dim ruta_Derivas As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico_Derivas.png"
        Grafico_Derivas.SaveImage(ruta_Derivas, ChartImageFormat.Png)
        ReemplazarImagen(wordDoc, "{Grafico_Derivas}", ruta_Derivas)

        '===== GRAFICOS ALR ===== 
        Dim chart_ALR_Muros As New Chart
        chart_ALR_Muros.Width = anchoPx / 2.5
        chart_ALR_Muros.Height = altoPx / 1.5
        Fun_Muros.GraficarALRMuros(chart_ALR_Muros)
        Dim ruta_ALR_Muros As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico_ALR_Muros.png"
        chart_ALR_Muros.SaveImage(ruta_ALR_Muros, ChartImageFormat.Png)
        ReemplazarImagen(wordDoc, "{Grafico_ALR_Muros}", ruta_ALR_Muros)

        '===== GRAFICO TW EN ALTURA ===== 
        Dim chart_TwAltura As New Chart
        chart_TwAltura.Width = anchoPx / 1.5
        chart_TwAltura.Height = altoPx
        Fun_Muros.GraficarTwAltura(chart_TwAltura)
        Dim ruta_TwAltura As String = "C:\Users\vidal\Desktop\InsumosPruebas\grafico_TwAltura.png"
        chart_TwAltura.SaveImage(ruta_TwAltura, ChartImageFormat.Png)
        ReemplazarImagen(wordDoc, "{Grafico_EspesorMurosAltura}", ruta_TwAltura)

        ' Guardar el documento modificado
        wordDoc.SaveAs2("C:\Users\vidal\Desktop\InsumosPruebas\Prueba_Reporte_V01.docx")

        ' Cerrar Word
        wordDoc.Close()
        wordApp.Quit()

        ' Liberar recursos
        System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc)
        System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp)

        MessageBox.Show("Documento Word generado y guardado con éxito.")

    End Sub

    Private Sub ReemplazarTexto(documento As Word.Document, textoBusqueda As String, nuevoTexto As String)
        ' Buscar y reemplazar texto en el documento
        Dim findObject As Word.Find = documento.Content.Find
        With findObject
            .ClearFormatting()
            .Text = textoBusqueda
            .Replacement.ClearFormatting()
            .Replacement.Text = nuevoTexto
            .Execute(Replace:=Word.WdReplace.wdReplaceAll)
        End With
    End Sub

    Private Sub ReemplazarImagen(documento As Word.Document, nombreImagen As String, rutaNuevaImagen As String)
        Dim rango As Word.Range
        Dim encontrado As Boolean
        encontrado = False

        For Each rango In documento.StoryRanges
            With rango.Find
                .Text = nombreImagen
                .Replacement.Text = ""
                .Wrap = Word.WdFindWrap.wdFindContinue
                .Execute()

                If .Found Then
                    encontrado = True
                    Exit For
                End If
            End With
        Next rango

        If encontrado Then
            rango.Select()
            documento.Application.Selection.Range.Delete()
            documento.Application.Selection.InlineShapes.AddPicture(FileName:=rutaNuevaImagen, LinkToFile:=False, SaveWithDocument:=True, Range:=documento.Application.Selection.Range)
        Else
            MsgBox("Texto no encontrado.")
        End If

    End Sub

End Class
