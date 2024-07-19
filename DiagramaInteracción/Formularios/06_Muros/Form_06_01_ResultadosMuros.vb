Imports ARCO.Funciones_00_Varias
Imports Excel = Microsoft.Office.Interop.Excel
Imports System.Data.OleDb
Imports System.Windows.Controls
Imports iTextSharp
Imports System.Windows.Controls.Primitives
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar

Public Class Form_06_01_ResultadosMuros
    Public Shared proyecto As Proyecto = Form_00_PaginaPrincipal.proyecto

    Private Sub Combo_Elementos_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Combo_Elementos.SelectedIndexChanged

        Tabla_Result_Flexo.Rows.Clear()
        Tabla_Result_Cortante.Rows.Clear()
        Tabla_Result_EB1.Rows.Clear()
        Tabla_Result_EB2.Rows.Clear()

        Dim Elemento = proyecto.Muros.Lista_Muros.Find(Function(p) p.Label = Combo_Elementos.Text)
        Dim Seccion = proyecto.Muros.Lista_Muros.Find(Function(p) p.Label = Combo_Elementos.Text).Lista_Secciones

        ' ------------- INSERTAR FILAS EN LAS TABLAS DE RESULTANTE A FLEXO-COMPRESIÓN
        For i = 0 To (Seccion.Count - 1) * 2 + 1
            Tabla_Result_Flexo.Rows.Add()
            Tabla_Result_EB1.Rows.Add()
            Tabla_Result_EB2.Rows.Add()
        Next

        For i = 0 To (Seccion.Count - 1) + 1
            Tabla_Result_Cortante.Rows.Add()
        Next


        ' ------------------ LLENAR TABLA DE RESULTADOS A FLEXO-COMPRESIÓN -----------
        For i = 0 To (Seccion.Count - 1) * 2 Step 2
            Tabla_Result_Flexo.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Result_Flexo.Rows(i).Cells(1).Value = Seccion(i / 2).fc
            Tabla_Result_Flexo.Rows(i).Cells(2).Value = Seccion(i / 2).tw_Planos
            Tabla_Result_Flexo.Rows(i).Cells(3).Value = Seccion(i / 2).Lw_Planos
            Tabla_Result_Flexo.Rows(i).Cells(4).Value = "Top"
            Tabla_Result_Flexo.Rows(i + 1).Cells(4).Value = "Bottom"


            Tabla_Result_Flexo.Rows(i).Cells(5).Value = Format(Seccion(i / 2).V_Limite_Cuantia, "##,##0.00")
            Tabla_Result_Flexo.Rows(i).Cells(6).Value = Format(Seccion(i / 2).Vu, "##,##0.00")
            Tabla_Result_Flexo.Rows(i).Cells(7).Value = Format(Seccion(i / 2).Rho_Min_L, "##,##0.00")
            Tabla_Result_Flexo.Rows(i).Cells(8).Value = Format(Seccion(i / 2).Cuantia_Top_Req, "##,##0.00")
            Tabla_Result_Flexo.Rows(i + 1).Cells(8).Value = Format(Seccion(i / 2).Cuantia_Bot_Req, "##,##0.00")
            Tabla_Result_Flexo.Rows(i).Cells(9).Value = Format(Seccion(i / 2).Cuantia_Top_Col, "##,##0.00")
            Tabla_Result_Flexo.Rows(i + 1).Cells(9).Value = Format(Seccion(i / 2).Cuantia_Bot_Col, "##,##0.00")
            Tabla_Result_Flexo.Rows(i).Cells(10).Value = Format(Seccion(i / 2).Cuantia_Top_Col / Seccion(i / 2).Cuantia_Top_Req, "##,##0.00")
            Tabla_Result_Flexo.Rows(i + 1).Cells(10).Value = Format(Seccion(i / 2).Cuantia_Bot_Col / Seccion(i / 2).Cuantia_Bot_Req, "##,##0.00")
            Seccion(i / 2).F_Flexo_Top = Math.Round(Seccion(i / 2).Cuantia_Top_Col / Seccion(i / 2).Cuantia_Top_Req, 2)
            Seccion(i / 2).F_Flexo_Bot = Math.Round(Seccion(i / 2).Cuantia_Bot_Col / Seccion(i / 2).Cuantia_Bot_Req, 2)


            If (Seccion(i / 2).Cuantia_Top_Col / Seccion(i / 2).Cuantia_Top_Req) >= 0.9 Then
                Tabla_Result_Flexo.Rows(i).Cells(11).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_Flexo, i, 11, "Cumple")
            Else
                Tabla_Result_Flexo.Rows(i).Cells(11).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_Flexo, i, 11, "No cumple")
            End If
            If (Seccion(i / 2).Cuantia_Bot_Col / Seccion(i / 2).Cuantia_Bot_Req) >= 0.9 Then
                Tabla_Result_Flexo.Rows(i + 1).Cells(11).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_Flexo, i + 1, 11, "Cumple")
            Else
                Tabla_Result_Flexo.Rows(i + 1).Cells(11).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_Flexo, i + 1, 11, "No cumple")
            End If

            ' ------------------ LLENAR TABLA DE RESULTADOS A CORTANTE -----------
            Tabla_Result_Cortante.Rows(i / 2).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Result_Cortante.Rows(i / 2).Cells(1).Value = Seccion(i / 2).fc
            Tabla_Result_Cortante.Rows(i / 2).Cells(2).Value = Format(Seccion(i / 2).tw_Planos, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(3).Value = Format(Seccion(i / 2).Lw_Planos, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(4).Value = Format(Seccion(i / 2).V_Limite_Cuantia, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(5).Value = Format(Seccion(i / 2).Vu, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(6).Value = Format(Seccion(i / 2).Rho_Min_T, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(7).Value = Format(Seccion(i / 2).Vc, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(8).Value = Format(Seccion(i / 2).Vs, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(9).Value = Format(Seccion(i / 2).Vn, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(10).Value = Format(Seccion(i / 2).F_Cortante, "##,##0.00")
            If Seccion(i / 2).F_Cortante >= 0.9 Then
                Tabla_Result_Cortante.Rows(i / 2).Cells(10).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_Cortante, i / 2, 10, "Cumple")
            Else
                Tabla_Result_Cortante.Rows(i / 2).Cells(10).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_Cortante, i / 2, 10, "No cumple")
            End If

            Tabla_Result_Cortante.Rows(i / 2).Cells(11).Value = Format(Seccion(i / 2).AsH_Col, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(12).Value = Format(Seccion(i / 2).AsH_Req_Top, "##,##0.00")
            Tabla_Result_Cortante.Rows(i / 2).Cells(13).Value = Format(Seccion(i / 2).AsH_Col / Seccion(i / 2).AsH_Req_Top, "##,##0.00")

            If (Seccion(i / 2).AsH_Col / Seccion(i / 2).AsH_Req_Top) >= 0.9 Then
                Tabla_Result_Cortante.Rows(i / 2).Cells(14).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_Cortante, i / 2, 14, "Cumple")
            Else
                Tabla_Result_Cortante.Rows(i / 2).Cells(14).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_Cortante, i / 2, 14, "No cumple")
            End If

            ' ------------------ LLENAR TABLA DE RESULTADOS A EB(1) -----------
            Tabla_Result_EB1.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Result_EB1.Rows(i).Cells(1).Value = Seccion(i / 2).fc
            Tabla_Result_EB1.Rows(i).Cells(2).Value = Format(Seccion(i / 2).tw_Planos, "##,##0.00")
            Tabla_Result_EB1.Rows(i).Cells(3).Value = Format(Seccion(i / 2).Lw_Planos, "##,##0.00")
            Tabla_Result_EB1.Rows(i).Cells(4).Value = Format(Seccion(i / 2).Esf_max, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(4).Value = Format(Seccion(i / 2).Esf_max, "##,##0.00")
            Tabla_Result_EB1.Rows(i).Cells(5).Value = Format(Seccion(i / 2).Esf_I_Top, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(5).Value = Format(Seccion(i / 2).Esf_I_Bot, "##,##0.00")

            Tabla_Result_EB1.Rows(i).Cells(6).Value = Seccion(i / 2).Chequeo_EB_I_Top_Esf
            Tabla_Result_EB1.Rows(i + 1).Cells(6).Value = Seccion(i / 2).Chequeo_EB_I_Bot_Esf

            If Seccion(i / 2).Chequeo_EB_I_Top_Esf = "No requiere" Then
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 6, "Cumple")
            Else
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 6, "No cumple")
            End If
            If Seccion(i / 2).Chequeo_EB_I_Bot_Esf = "No requiere" Then
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 6, "Cumple")
            Else
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 6, "No cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(7).Value = Format(Seccion(i / 2).Esf_D_Top, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(7).Value = Format(Seccion(i / 2).Esf_D_Bot, "##,##0.00")
            Tabla_Result_EB1.Rows(i).Cells(8).Value = Seccion(i / 2).Chequeo_EB_D_Top_Esf
            Tabla_Result_EB1.Rows(i + 1).Cells(8).Value = Seccion(i / 2).Chequeo_EB_D_Bot_Esf

            If Seccion(i / 2).Chequeo_EB_D_Top_Esf = "No requiere" Then
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 8, "Cumple")
            Else
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 8, "No cumple")
            End If
            If Seccion(i / 2).Chequeo_EB_D_Bot_Esf = "No requiere" Then
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 8, "Cumple")
            Else
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 8, "No cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(9).Value = Format(Seccion(i / 2).C_Limite, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(9).Value = Format(Seccion(i / 2).C_Limite, "##,##0.00")
            Tabla_Result_EB1.Rows(i).Cells(10).Value = Format(Seccion(i / 2).C_I_Top, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(10).Value = Format(Seccion(i / 2).C_I_Bot, "##,##0.00")

            If Seccion(i / 2).C_I_Top <= Seccion(i / 2).C_Limite / 0.9 Then
                Tabla_Result_EB1.Rows(i).Cells(11).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 11, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i).Cells(11).Value = "Requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 11, "No cumple")
            End If
            If Seccion(i / 2).C_I_Bot <= Seccion(i / 2).C_Limite / 0.9 Then
                Tabla_Result_EB1.Rows(i + 1).Cells(11).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 11, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(11).Value = "Requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 11, "No cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(12).Value = Format(Seccion(i / 2).C_D_Top, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(12).Value = Format(Seccion(i / 2).C_D_Bot, "##,##0.00")

            If Seccion(i / 2).C_D_Top <= Seccion(i / 2).C_Limite / 0.9 Then
                Tabla_Result_EB1.Rows(i).Cells(13).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 13, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i).Cells(13).Value = "Requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 13, "No cumple")
            End If
            If Seccion(i / 2).C_D_Bot <= Seccion(i / 2).C_Limite / 0.9 Then
                Tabla_Result_EB1.Rows(i + 1).Cells(13).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 13, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(13).Value = "Requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 13, "No cumple")
            End If

            If Seccion(i / 2).Chequeo_EB_I_Top_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_I_Top_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i).Cells(14).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 14, "Cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_I_Top_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_I_Top_Def = "Requiere" Then
                Tabla_Result_EB1.Rows(i).Cells(14).Value = "Requiere por deformaciones"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 14, "No cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_I_Top_Esf = "Requiere" And Seccion(i / 2).Chequeo_EB_I_Top_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i).Cells(14).Value = "Requiere por esfuerzos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 14, "No cumple")
            Else
                Tabla_Result_EB1.Rows(i).Cells(14).Value = "Requiere por ambos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 14, "No cumple")
            End If

            If Seccion(i / 2).Chequeo_EB_I_Bot_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_I_Bot_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i + 1).Cells(14).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 14, "Cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_I_Bot_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_I_Bot_Def = "Requiere" Then
                Tabla_Result_EB1.Rows(i + 1).Cells(14).Value = "Requiere por deformaciones"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 14, "No cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_I_Bot_Esf = "Requiere" And Seccion(i / 2).Chequeo_EB_I_Bot_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i + 1).Cells(14).Value = "Requiere por esfuerzos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 14, "No cumple")
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(14).Value = "Requiere por ambos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 14, "No cumple")
            End If

            If Seccion(i / 2).Chequeo_EB_D_Top_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_D_Top_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i).Cells(15).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 15, "Cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_D_Top_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_D_Top_Def = "Requiere" Then
                Tabla_Result_EB1.Rows(i).Cells(15).Value = "Requiere por deformaciones"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 15, "No cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_D_Top_Esf = "Requiere" And Seccion(i / 2).Chequeo_EB_D_Top_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i).Cells(15).Value = "Requiere por esfuerzos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 15, "No cumple")
            Else
                Tabla_Result_EB1.Rows(i).Cells(15).Value = "Requiere por ambos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 15, "No cumple")
            End If

            If Seccion(i / 2).Chequeo_EB_D_Bot_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_D_Bot_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i + 1).Cells(15).Value = "No requiere"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 15, "Cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_D_Bot_Esf = "No requiere" And Seccion(i / 2).Chequeo_EB_D_Bot_Def = "Requiere" Then
                Tabla_Result_EB1.Rows(i + 1).Cells(15).Value = "Requiere por deformaciones"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 15, "No cumple")
            ElseIf Seccion(i / 2).Chequeo_EB_D_Bot_Esf = "Requiere" And Seccion(i / 2).Chequeo_EB_D_Bot_Def = "No requiere" Then
                Tabla_Result_EB1.Rows(i + 1).Cells(15).Value = "Requiere por esfuerzos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 15, "No cumple")
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(15).Value = "Requiere por ambos"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 15, "No cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(16).Value = Format(Seccion(i / 2).EB_I_Top.L_EB_Req, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(16).Value = Format(Seccion(i / 2).EB_I_Bot.L_EB_Req, "##,##0.00")

            Tabla_Result_EB1.Rows(i).Cells(17).Value = Format(Seccion(i / 2).EB_I_Top.L_EB, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(17).Value = Format(Seccion(i / 2).EB_I_Bot.L_EB, "##,##0.00")

            If Seccion(i / 2).EB_I_Top.L_EB_Req <= (Seccion(i / 2).EB_I_Top.L_EB / 0.9) Then
                Tabla_Result_EB1.Rows(i).Cells(18).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 18, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i).Cells(18).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 18, "No cumple")
            End If
            If Seccion(i / 2).EB_I_Bot.L_EB_Req <= (Seccion(i / 2).EB_I_Bot.L_EB) / 0.9 Then
                Tabla_Result_EB1.Rows(i + 1).Cells(18).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 18, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(18).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 18, "No cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(19).Value = Format(Seccion(i / 2).EB_D_Top.L_EB_Req, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(19).Value = Format(Seccion(i / 2).EB_D_Bot.L_EB_Req, "##,##0.00")

            Tabla_Result_EB1.Rows(i).Cells(20).Value = Format(Seccion(i / 2).EB_D_Top.L_EB, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(20).Value = Format(Seccion(i / 2).EB_D_Bot.L_EB, "##,##0.00")

            If Seccion(i / 2).EB_D_Top.L_EB_Req <= (Seccion(i / 2).EB_D_Top.L_EB) / 0.9 Then
                Tabla_Result_EB1.Rows(i).Cells(21).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 21, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i).Cells(21).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 21, "No cumple")
            End If
            If Seccion(i / 2).EB_D_Bot.L_EB_Req <= (Seccion(i / 2).EB_D_Bot.L_EB) / 0.9 Then
                Tabla_Result_EB1.Rows(i + 1).Cells(21).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 21, "Cumple")
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(21).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 21, "No cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(22).Value = Format(Seccion(i / 2).EB_I_Top.Cuantia_L, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(22).Value = Format(Seccion(i / 2).EB_I_Bot.Cuantia_L, "##,##0.00")

            If Seccion(i / 2).Req_EB_I_Top_Esp = False Then
                If Seccion(i / 2).Req_EB_I_Top_NoEsp = False Then
                    Tabla_Result_EB1.Rows(i).Cells(23).Value = "No requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i, 23, "Cumple")
                Else
                    Tabla_Result_EB1.Rows(i).Cells(23).Value = "Requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i, 23, "No cumple")
                End If
            Else
                Tabla_Result_EB1.Rows(i).Cells(23).Value = "Rev. EB Especial"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 23, "Cumple")
            End If

            If Seccion(i / 2).Req_EB_I_Bot_Esp = False Then
                If Seccion(i / 2).Req_EB_I_Bot_NoEsp = False Then
                    Tabla_Result_EB1.Rows(i + 1).Cells(23).Value = "No requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 23, "Cumple")
                Else
                    Tabla_Result_EB1.Rows(i + 1).Cells(23).Value = "Requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 23, "No cumple")
                End If
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(23).Value = "Rev. EB Especial"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 23, "Cumple")
            End If

            Tabla_Result_EB1.Rows(i).Cells(24).Value = Format(Seccion(i / 2).EB_D_Top.Cuantia_L, "##,##0.00")
            Tabla_Result_EB1.Rows(i + 1).Cells(24).Value = Format(Seccion(i / 2).EB_D_Bot.Cuantia_L, "##,##0.00")
            If Seccion(i / 2).Req_EB_D_Top_Esp = False Then
                If Seccion(i / 2).Req_EB_D_Top_NoEsp = False Then
                    Tabla_Result_EB1.Rows(i).Cells(25).Value = "No requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i, 25, "Cumple")
                Else
                    Tabla_Result_EB1.Rows(i).Cells(25).Value = "Requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i, 25, "No cumple")
                End If
            Else
                Tabla_Result_EB1.Rows(i).Cells(25).Value = "Rev. EB Especial"
                Funcion_Color_Cumple(Tabla_Result_EB1, i, 25, "Cumple")
            End If
            If Seccion(i / 2).Req_EB_D_Bot_Esp = False Then
                If Seccion(i / 2).Req_EB_D_Bot_NoEsp = False Then
                    Tabla_Result_EB1.Rows(i + 1).Cells(25).Value = "No requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 25, "Cumple")
                Else
                    Tabla_Result_EB1.Rows(i + 1).Cells(25).Value = "Requiere"
                    Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 25, "No cumple")
                End If
            Else
                Tabla_Result_EB1.Rows(i + 1).Cells(25).Value = "Rev. EB Especial"
                Funcion_Color_Cumple(Tabla_Result_EB1, i + 1, 25, "Cumple")
            End If

            ' ------------------ LLENAR TABLA DE RESULTADOS A EB(2) -----------
            Tabla_Result_EB2.Rows(i).Cells(0).Value = Seccion(i / 2).Piso
            Tabla_Result_EB2.Rows(i).Cells(1).Value = Seccion(i / 2).fc
            Tabla_Result_EB2.Rows(i).Cells(2).Value = Format(Seccion(i / 2).tw_Planos, "##,##0.00")
            Tabla_Result_EB2.Rows(i).Cells(3).Value = Format(Seccion(i / 2).Lw_Planos, "##,##0.00")

            Tabla_Result_EB2.Rows(i).Cells(4).Value = Seccion(i / 2).EB_I_Top.Tipo_EB_Req
            Tabla_Result_EB2.Rows(i + 1).Cells(4).Value = Seccion(i / 2).EB_I_Bot.Tipo_EB_Req

            Tabla_Result_EB2.Rows(i).Cells(5).Value = Seccion(i / 2).EB_I_Top.Tipo_EB_Col
            Tabla_Result_EB2.Rows(i + 1).Cells(5).Value = Seccion(i / 2).EB_I_Bot.Tipo_EB_Col

            Tabla_Result_EB2.Rows(i).Cells(6).Value = Format(Seccion(i / 2).EB_I_Top.RefH_Req, "##,##0.00")
            Tabla_Result_EB2.Rows(i).Cells(7).Value = Format(Seccion(i / 2).EB_I_Top.RefH.Acero_T, "##,##0.00")
            Tabla_Result_EB2.Rows(i + 1).Cells(6).Value = Format(Seccion(i / 2).EB_I_Bot.RefH_Req, "##,##0.00")
            Tabla_Result_EB2.Rows(i + 1).Cells(7).Value = Format(Seccion(i / 2).EB_I_Bot.RefH.Acero_T, "##,##0.00")


            If Seccion(i / 2).EB_I_Top.RefH_Req * 0.9 <= Seccion(i / 2).EB_I_Top.RefH.Acero_T Then
                Tabla_Result_EB2.Rows(i).Cells(8).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i, 8, "Cumple")
            Else
                Tabla_Result_EB2.Rows(i).Cells(8).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i, 8, "No cumple")
            End If
            If Seccion(i / 2).EB_I_Bot.RefH_Req * 0.9 <= Seccion(i / 2).EB_I_Bot.RefH.Acero_T Then
                Tabla_Result_EB2.Rows(i + 1).Cells(8).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i + 1, 8, "Cumple")
            Else
                Tabla_Result_EB2.Rows(i + 1).Cells(8).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i + 1, 8, "No cumple")
            End If

            Tabla_Result_EB2.Rows(i).Cells(10).Value = Seccion(i / 2).EB_D_Top.Tipo_EB_Req
            Tabla_Result_EB2.Rows(i + 1).Cells(10).Value = Seccion(i / 2).EB_D_Bot.Tipo_EB_Req

            Tabla_Result_EB2.Rows(i).Cells(11).Value = Seccion(i / 2).EB_D_Top.Tipo_EB_Col
            Tabla_Result_EB2.Rows(i + 1).Cells(11).Value = Seccion(i / 2).EB_D_Bot.Tipo_EB_Col

            Tabla_Result_EB2.Rows(i).Cells(12).Value = Format(Seccion(i / 2).EB_D_Top.RefH_Req, "##,##0.00")
            Tabla_Result_EB2.Rows(i).Cells(13).Value = Format(Seccion(i / 2).EB_D_Top.RefH.Acero_T, "##,##0.00")
            Tabla_Result_EB2.Rows(i + 1).Cells(12).Value = Format(Seccion(i / 2).EB_D_Bot.RefH_Req, "##,##0.00")
            Tabla_Result_EB2.Rows(i + 1).Cells(13).Value = Format(Seccion(i / 2).EB_D_Bot.RefH.Acero_T, "##,##0.00")

            If Seccion(i / 2).EB_D_Top.RefH_Req * 0.9 <= Seccion(i / 2).EB_D_Top.RefH.Acero_T Then
                Tabla_Result_EB2.Rows(i).Cells(14).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i, 14, "Cumple")
            Else
                Tabla_Result_EB2.Rows(i).Cells(14).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i, 14, "No cumple")
            End If
            If Seccion(i / 2).EB_D_Bot.RefH_Req * 0.9 <= Seccion(i / 2).EB_D_Bot.RefH.Acero_T Then
                Tabla_Result_EB2.Rows(i + 1).Cells(14).Value = "Cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i + 1, 14, "Cumple")
            Else
                Tabla_Result_EB2.Rows(i + 1).Cells(14).Value = "No cumple"
                Funcion_Color_Cumple(Tabla_Result_EB2, i + 1, 14, "No cumple")
            End If

        Next

        Tabla_Result_EB2.Rows(0).Cells(15).Value = Elemento.LV_EB_I_Req_Def
        Tabla_Result_EB2.Rows(0).Cells(16).Value = Elemento.LV_EB_I_Req_Esf
        Tabla_Result_EB2.Rows(0).Cells(17).Value = Elemento.LV_EB_I_Col_Esp
        'Tabla_Result_EB2.Rows(0).Cells(17).Value = Math.Max(Elemento.LV_EB_I_Col_Esp, Elemento.LV_EB_I_Col_NoEsp)

        If Math.Max(Elemento.LV_EB_I_Req_Def, Elemento.LV_EB_I_Req_Esf) <= Elemento.LV_EB_I_Col_Esp Then
            Tabla_Result_EB2.Rows(0).Cells(18).Value = "Cumple"
            Funcion_Color_Cumple(Tabla_Result_EB2, 0, 18, "Cumple")
        Else
            Tabla_Result_EB2.Rows(0).Cells(18).Value = "Revisar"
            Funcion_Color_Cumple(Tabla_Result_EB2, 0, 18, "No cumple")
        End If

        Tabla_Result_EB2.Rows(0).Cells(19).Value = Elemento.LV_EB_D_Req_Def
        Tabla_Result_EB2.Rows(0).Cells(20).Value = Elemento.LV_EB_D_Req_Esf
        Tabla_Result_EB2.Rows(0).Cells(21).Value = Elemento.LV_EB_D_Col_Esp
        'Tabla_Result_EB2.Rows(0).Cells(21).Value = Math.Max(Elemento.LV_EB_D_Col_Esp, Elemento.LV_EB_D_Col_NoEsp)

        If Math.Max(Elemento.LV_EB_D_Req_Def, Elemento.LV_EB_D_Req_Esf) <= Elemento.LV_EB_D_Col_Esp Then
            Tabla_Result_EB2.Rows(0).Cells(22).Value = "Cumple"
            Funcion_Color_Cumple(Tabla_Result_EB2, 0, 22, "Cumple")
        Else
            Tabla_Result_EB2.Rows(0).Cells(22).Value = "Revisar"
            Funcion_Color_Cumple(Tabla_Result_EB2, 0, 22, "No cumple")
        End If

    End Sub

    Private Sub AExcelToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AExcelToolStripMenuItem.Click

        Me.Cursor = Cursors.WaitCursor

        'try

        Dim Archivo As String = "RevisiónMuros"
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDbConnection(connection)

        Dim Color_Interior As Color = Color.FromArgb(200, 200, 200)
        Dim appXL As New Excel.Application
        Dim wbXL As Excel.Workbook
        Dim Hoja_Resultados As Excel.Worksheet
        wbXL = appXL.Workbooks.Add()

        Hoja_Resultados = wbXL.Sheets("Hoja1")
        Hoja_Resultados.PageSetup.Application.ActiveWindow.DisplayGridlines = False

        Hoja_Resultados.Name = "Flexo-Compresion"

        Hoja_Resultados.Range("A1:G1").Interior.Color = Color_Interior
        Hoja_Resultados.Range("A1:P10000").Font.Name = "Arial"
        Hoja_Resultados.Range("A1:G1").Font.Bold = True
        Hoja_Resultados.Range("A1:G2").Font.Size = 11
        Hoja_Resultados.Range("A2:P10000").Font.Size = 10
        Hoja_Resultados.Range("A1:P100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A1:P10000").VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A:G").ColumnWidth = 17

        Hoja_Resultados.Cells(1, 1) = "Muro"
        Hoja_Resultados.Cells(1, 2) = "Piso critico"
        Hoja_Resultados.Cells(1, 3) = "Lw (m)"
        Hoja_Resultados.Cells(1, 4) = "tw (m)"
        Hoja_Resultados.Cells(1, 5) = "Cuantía Col (%)"
        Hoja_Resultados.Cells(1, 6) = "Cuantía Col/Req"
        Hoja_Resultados.Cells(1, 7) = "Verificación)"

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
            Dim muro = proyecto.Muros.Lista_Muros(i)
            Hoja_Resultados.Cells(2 + i, 1) = muro.Name
            Hoja_Resultados.Cells(2 + i, 3) = muro.Lw
            Hoja_Resultados.Cells(2 + i, 4) = muro.tw

            Dim valmin_Bot As Single = muro.Lista_Secciones.Min(Of Single)(Function(p) p.F_Flexo_Bot)
            Dim valmin_Top As Single = muro.Lista_Secciones.Min(Of Single)(Function(p) p.F_Flexo_Top)

            Dim posCri As Integer = muro.Lista_Secciones.FindIndex(Function(p) p.F_Flexo_Bot = valmin_Bot)
            Dim posCri1 As Integer = muro.Lista_Secciones.FindIndex(Function(p) p.F_Flexo_Top = valmin_Top)

            If valmin_Bot > valmin_Top Then
                posCri = posCri1
            End If
            Hoja_Resultados.Cells(2 + i, 2) = muro.Lista_Secciones(posCri).Piso
            Hoja_Resultados.Cells(2 + i, 5) = Format(muro.Lista_Secciones(posCri).Cuantia_Bot_Col, "##,##0.00")
            Hoja_Resultados.Cells(2 + i, 6) = Format(Math.Min(muro.Lista_Secciones(posCri).F_Flexo_Bot, muro.Lista_Secciones(posCri).F_Flexo_Top), "##,##0.00")

            If Math.Min(muro.Lista_Secciones(posCri).F_Flexo_Bot, muro.Lista_Secciones(posCri).F_Flexo_Top) < 0.9 Then
                Hoja_Resultados.Cells(2 + i, 7) = "No cumple"
                Hoja_Resultados.Range("F" & 2 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("F" & 2 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            Else
                Hoja_Resultados.Cells(2 + i, 7) = "Cumple"
                Hoja_Resultados.Range("F" & 2 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("F" & 2 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            End If

        Next

        Dim saveFileDialog1 As New SaveFileDialog()
        saveFileDialog1.Title = "Guardar documento Excel"
        saveFileDialog1.Filter = "Excel File|*.xlsx"
        saveFileDialog1.FileName = Convert.ToString(Archivo & "_Proyecto - " & PagInfoGeneral.NameProject.Text)
        saveFileDialog1.ShowDialog()
        wbXL.SaveAs(saveFileDialog1.FileName)
        appXL.Workbooks.Close()
        appXL.Quit()
        System.Diagnostics.Process.Start(saveFileDialog1.FileName)

        'Catch ex As Exception
        'MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Finally
        conexion.Close()
        Cursor = Cursors.Arrow
        'End Try


    End Sub

    Private Sub ResultadosAExcelToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ResultadosAExcelToolStripMenuItem1.Click

        Me.Cursor = Cursors.WaitCursor

        Dim P As Single = 2

        Dim Archivo As String = "RevisiónMuros"
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDbConnection(connection)

        'Try
        Dim Color_Interior As Color = Color.FromArgb(200, 200, 200)
        Dim appXL As New Excel.Application
        Dim wbXL As Excel.Workbook
        Dim Hoja_Resultados As Excel.Worksheet
        wbXL = appXL.Workbooks.Add()

        Hoja_Resultados = wbXL.Sheets("Hoja1")
        Hoja_Resultados.PageSetup.Application.ActiveWindow.DisplayGridlines = False

        Hoja_Resultados.Name = "Revisión de Capacidad"

        Hoja_Resultados.Range("A1:V1").Interior.Color = Color_Interior
        Hoja_Resultados.Range("A1:V10000").Font.Name = "Arial"
        Hoja_Resultados.Range("A1:V1").Font.Bold = True
        Hoja_Resultados.Range("A1:V1").Font.Size = 11
        Hoja_Resultados.Range("A2:V10000").Font.Size = 10
        Hoja_Resultados.Range("A1:V100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A1:V10000").VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A:V").ColumnWidth = 13

        Hoja_Resultados.Cells(1, 1) = "Nombre Muro - REV"
        Hoja_Resultados.Cells(1, 2) = "Nombre Muro - DIS"
        Hoja_Resultados.Cells(1, 3) = "Lw (m)"
        Hoja_Resultados.Cells(1, 4) = "tw (m)"

        Hoja_Resultados.Cells(1, 5) = "Cuantía Req"
        Hoja_Resultados.Cells(1, 6) = "Cuantía Col"
        Hoja_Resultados.Cells(1, 7) = "Cuantia Col/Req"

        Hoja_Resultados.Cells(1, 9) = "Av Req (mm2/m)"
        Hoja_Resultados.Cells(1, 10) = "Av Col (mm2/m)"
        Hoja_Resultados.Cells(1, 11) = "Av Col/Req"

        Hoja_Resultados.Cells(1, 13) = "Chequeo EB"
        Hoja_Resultados.Cells(1, 14) = "LH Req (m)"
        Hoja_Resultados.Cells(1, 15) = "LH Col (m)"
        Hoja_Resultados.Cells(1, 16) = "Chequeo LH"

        Hoja_Resultados.Cells(1, 17) = "LV Req (m)"
        Hoja_Resultados.Cells(1, 18) = "LV Col (m)"
        Hoja_Resultados.Cells(1, 19) = "Chequeo LV"

        Hoja_Resultados.Cells(1, 20) = "AshReq (mm2)"
        Hoja_Resultados.Cells(1, 21) = "AshCol (mm2)"
        Hoja_Resultados.Cells(1, 22) = "Chequeo Ash"

        Dim g As Integer = 3
        Dim R_Ash_Min As Single = 1
        Dim Fc_Max As Single = 35

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1

            Dim Muro = proyecto.Muros.Lista_Muros(i)

            Hoja_Resultados.Cells(2 + i, 1) = Muro.Name
            Hoja_Resultados.Cells(2 + i, 2) = Muro.Label
            Hoja_Resultados.Cells(2 + i, 3) = Muro.Lw.ToString("0.00")
            Hoja_Resultados.Cells(2 + i, 4) = Muro.tw.ToString("0.00")

            Dim F_Flexo As Single = 100
            Dim Cuantia_Col As Single
            Dim Cuantia_Req As Single

            Dim F_Av As Single = 100
            Dim Av_Req As Single
            Dim Av_Col As Single

            For j = 0 To Muro.Lista_Secciones.Count - 1
                Dim Tramo_I = Muro.Lista_Secciones(j)

                If Math.Min(Tramo_I.F_Flexo_Top, Tramo_I.F_Flexo_Bot) < F_Flexo Then
                    F_Flexo = Math.Min(Tramo_I.Cuantia_Top_Col / Tramo_I.Cuantia_Top_Req, Tramo_I.Cuantia_Bot_Col / Tramo_I.Cuantia_Bot_Req)
                    Cuantia_Col = Tramo_I.Cuantia_Bot_Col
                    Cuantia_Req = Tramo_I.Cuantia_Bot_Req
                End If

                If (Tramo_I.AsH_Col / Tramo_I.AsH_Req_Top) < F_Av Then
                    F_Av = Tramo_I.AsH_Col / Tramo_I.AsH_Req_Top
                    Av_Req = Tramo_I.AsH_Req_Top
                    Av_Col = Tramo_I.AsH_Col
                End If
            Next

            Dim listaSecciones As List(Of SeccionMuro) = Muro.Lista_Secciones
            Dim seccion_P01 As SeccionMuro = listaSecciones.OrderBy(Function(seccion) seccion.Altura).First()

            Dim LEB_Col As Single
            Dim LEB_Req As Single

            If seccion_P01.EB_I_Bot.L_EB_Req > 0 And seccion_P01.EB_D_Bot.L_EB_Req > 0 Then
                If (seccion_P01.EB_I_Bot.L_EB / seccion_P01.EB_I_Bot.L_EB_Req) > (seccion_P01.EB_D_Bot.L_EB / seccion_P01.EB_D_Bot.L_EB_Req) Then
                    LEB_Col = seccion_P01.EB_D_Bot.L_EB
                    LEB_Req = seccion_P01.EB_D_Bot.L_EB_Req
                Else
                    LEB_Col = seccion_P01.EB_I_Bot.L_EB
                    LEB_Req = seccion_P01.EB_I_Bot.L_EB_Req
                End If
            Else
                LEB_Req = 0
                LEB_Col = Math.Max(seccion_P01.EB_I_Bot.L_EB, seccion_P01.EB_D_Bot.L_EB)
            End If

            Dim LH_Req As Single = LEB_Req
            Dim LH_Col As Single = LEB_Col

            Dim LV_Req As Single
            Dim LV_Col As Single

            If Muro.LV_EB_I_Col_Esp > Muro.LV_EB_D_Col_Esp Then
                LV_Col = Muro.LV_EB_I_Col_Esp
                LV_Req = Math.Max(Muro.LV_EB_I_Req_Esf, Muro.LV_EB_I_Req_Def)
            Else
                LV_Col = Muro.LV_EB_D_Col_Esp
                LV_Req = Math.Max(Muro.LV_EB_D_Req_Esf, Muro.LV_EB_D_Req_Def)
            End If

            Dim Ash_Req As Single = seccion_P01.EB_I_Bot.RefH_Req
            Dim Ash_Col As Single = seccion_P01.EB_I_Bot.RefH.Acero_T

            If seccion_P01.EB_I_Bot.RefH_Req < seccion_P01.EB_D_Bot.RefH_Req Then
                Ash_Req = seccion_P01.EB_D_Bot.RefH_Req
                Ash_Col = seccion_P01.EB_D_Bot.RefH.Acero_T
            End If

            '========== Llenar info a flexocompresión ============
            Hoja_Resultados.Cells(2 + i, 5) = Convert.ToString(Math.Round(Cuantia_Req, 2) & " %")
            Hoja_Resultados.Cells(2 + i, 6) = Convert.ToString(Math.Round(Cuantia_Col, 2) & " %")
            Hoja_Resultados.Cells(2 + i, 7) = Math.Round(F_Flexo, 2).ToString("0.00")

            If F_Flexo < 0.9 Then
                Hoja_Resultados.Range("G" & 2 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("G" & 2 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            Else
                Hoja_Resultados.Range("G" & 2 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("G" & 2 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            End If

            Hoja_Resultados.Range("H" & 2 + i).Interior.Color = Color_Interior

            '========== Llenar info a Cortante ============
            Hoja_Resultados.Cells(2 + i, 9) = Av_Req.ToString("0.00")
            Hoja_Resultados.Cells(2 + i, 10) = Av_Col.ToString("0.00")
            Hoja_Resultados.Cells(2 + i, 11) = F_Av.ToString("0.00")

            If F_Av < 0.9 Then
                Hoja_Resultados.Range("K" & 2 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("K" & 2 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            Else
                Hoja_Resultados.Range("K" & 2 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("K" & 2 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            End If

            Hoja_Resultados.Range("L" & 2 + i).Interior.Color = Color_Interior

            '======= Llenar info de EB =======
            Dim Tramo = seccion_P01
            If Tramo.Chequeo_EB_I_Top_Esf = "No requiere" And
                    Tramo.Chequeo_EB_D_Top_Esf = "No requiere" And
                    Tramo.Chequeo_EB_I_Bot_Esf = "No requiere" And
                    Tramo.Chequeo_EB_D_Bot_Esf = "No requiere" And
                    Tramo.Chequeo_EB_I_Top_Def = "No requiere" And
                    Tramo.Chequeo_EB_I_Bot_Def = "No requiere" And
                    Tramo.Chequeo_EB_D_Top_Def = "No requiere" And
                    Tramo.Chequeo_EB_D_Bot_Def = "No requiere" Then

                Hoja_Resultados.Cells(2 + i, 13) = "No requiere"

            ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_I_Bot_Def = "Requiere") Or
                    (Tramo.Chequeo_EB_D_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_D_Bot_Def = "Requiere") Then

                Hoja_Resultados.Cells(2 + i, 13) = "Requiere por ambos"

            ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_I_Bot_Def = "No requiere") Or
                    (Tramo.Chequeo_EB_D_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_D_Bot_Def = "No requiere") Then

                Hoja_Resultados.Cells(2 + i, 13) = "Requiere por esfuerzos"

            ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_I_Bot_Def = "Requiere") Or
                    (Tramo.Chequeo_EB_D_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_D_Bot_Def = "Requiere") Then

                Hoja_Resultados.Cells(2 + i, 13) = "Requiere por deformaciones"
            End If

            Hoja_Resultados.Cells(2 + i, 14) = LEB_Req.ToString("0.00")
            Hoja_Resultados.Cells(2 + i, 15) = LEB_Col.ToString("0.00")
            If LEB_Col > 0.9 * LEB_Req Then
                Hoja_Resultados.Cells(2 + i, 16) = "Cumple"
                Hoja_Resultados.Range("P" & 2 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("P" & 2 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            Else
                Hoja_Resultados.Cells(2 + i, 16) = "No cumple"
                Hoja_Resultados.Range("P" & 2 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("P" & 2 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            End If

            Hoja_Resultados.Cells(2 + i, 17) = LV_Req.ToString("0.00")
            Hoja_Resultados.Cells(2 + i, 18) = LV_Col.ToString("0.00")
            If LEB_Col > 0.9 * LEB_Req Then
                Hoja_Resultados.Cells(2 + i, 19) = "Cumple"
                Hoja_Resultados.Range("S" & 2 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("S" & 2 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            Else
                Hoja_Resultados.Cells(2 + i, 19) = "No cumple"
                Hoja_Resultados.Range("S" & 2 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("S" & 2 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            End If

            Hoja_Resultados.Cells(2 + i, 20) = Ash_Req.ToString("0.00")
            Hoja_Resultados.Cells(2 + i, 21) = Ash_Col.ToString("0.00")
            If Ash_Col > 0.9 * Ash_Req Then
                Hoja_Resultados.Cells(2 + i, 22) = "Cumple"
                Hoja_Resultados.Range("V" & 2 + i).Font.Color = Color.FromArgb(0, 97, 0)
                Hoja_Resultados.Range("V" & 2 + i).Interior.Color = Color.FromArgb(198, 239, 206)
            Else
                Hoja_Resultados.Cells(2 + i, 22) = "No cumple"
                Hoja_Resultados.Range("V" & 2 + i).Font.Color = Color.FromArgb(156, 0, 6)
                Hoja_Resultados.Range("V" & 2 + i).Interior.Color = Color.FromArgb(255, 199, 206)
            End If
        Next

        Dim saveFileDialog1 As New SaveFileDialog()
        saveFileDialog1.Title = "Guardar documento Excel"
        saveFileDialog1.Filter = "Excel File|*.xlsx"
        saveFileDialog1.FileName = Convert.ToString(Archivo & "_Proyecto - " & PagInfoGeneral.NameProject.Text)
        saveFileDialog1.ShowDialog()
        wbXL.SaveAs(saveFileDialog1.FileName)
        appXL.Workbooks.Close()
        appXL.Quit()
        System.Diagnostics.Process.Start(saveFileDialog1.FileName)

        'Catch ex As Exception
        'MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Finally
        conexion.Close()
        Cursor = Cursors.Arrow
        'End Try


    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click

        Me.Cursor = Cursors.WaitCursor

        Dim P As Single = 2

        Dim Archivo As String = "RevisiónMuros"
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDbConnection(connection)

        'Try
        Dim Color_Interior As Color = Color.FromArgb(200, 200, 200)
        Dim appXL As New Excel.Application
        Dim wbXL As Excel.Workbook
        Dim Hoja_Resultados As Excel.Worksheet
        wbXL = appXL.Workbooks.Add()

        Hoja_Resultados = wbXL.Sheets("Hoja1")
        Hoja_Resultados.PageSetup.Application.ActiveWindow.DisplayGridlines = False

        Hoja_Resultados.Name = "Revisión de Capacidad"

        'Hoja_Resultados.Range("E1:F1").Merge(True)
        'Hoja_Resultados.Range("B1:D1").Merge(True)
        Hoja_Resultados.Range("A1:A2").MergeCells = True
        Hoja_Resultados.Range("B1:B2").MergeCells = True
        Hoja_Resultados.Range("C1:C2").MergeCells = True
        'Hoja_Resultados.Range("B1:B2").MergeCells = True
        'Hoja_Resultados.Range("C1:C2").MergeCells = True
        Hoja_Resultados.Range("A1:Z2").Interior.Color = Color_Interior
        Hoja_Resultados.Range("A1:Z10000").Font.Name = "Arial"
        Hoja_Resultados.Range("A1:Z2").Font.Bold = True
        Hoja_Resultados.Range("A1:Z2").Font.Size = 11
        Hoja_Resultados.Range("A3:Z10000").Font.Size = 10
        Hoja_Resultados.Range("A1:Z10000").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A1:Z10000").VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A:Z").ColumnWidth = 15

        Hoja_Resultados.Cells(1, 1) = "Nombre Muro - REV"
        Hoja_Resultados.Cells(1, 2) = "Nombre Muro - DIS"
        Hoja_Resultados.Cells(1, 3) = "Piso"

        Dim Col_Flexo As Int16 = 4
        Hoja_Resultados.Cells(1, Col_Flexo) = "Flexo-Compresión"
        Hoja_Resultados.Cells(2, Col_Flexo) = "Cuantía Colocada"
        Hoja_Resultados.Cells(2, Col_Flexo + 1) = "Cuantía Requerida"
        Hoja_Resultados.Cells(2, Col_Flexo + 2) = "Capacidad/Demanda"
        Hoja_Resultados.Range("D1:F1").Merge(True)

        Dim Col_Cortante As Int16 = Col_Flexo + 3
        Hoja_Resultados.Cells(1, Col_Cortante) = "Revisión Cortante"
        Hoja_Resultados.Cells(2, Col_Cortante) = "Av Req (mm2/m)"
        Hoja_Resultados.Cells(2, Col_Cortante + 1) = "Av Col (mm2/m)"
        Hoja_Resultados.Cells(2, Col_Cortante + 2) = "Av Col/Req"
        Hoja_Resultados.Range("G1:I1").Merge(True)

        Dim Col_EB As Int16 = Col_Cortante + 3
        Hoja_Resultados.Range("J1:Z1").MergeCells = True
        Hoja_Resultados.Cells(1, Col_EB) = "Chequeo Elemento de Borde"
        Hoja_Resultados.Cells(2, Col_EB) = "Chequeo"
        Hoja_Resultados.Cells(2, Col_EB + 1) = "LH I Col (m)"
        Hoja_Resultados.Cells(2, Col_EB + 2) = "LH I Req (m)"
        Hoja_Resultados.Cells(2, Col_EB + 3) = "Chequeo LH I"
        Hoja_Resultados.Cells(2, Col_EB + 4) = "LH D Col (m)"
        Hoja_Resultados.Cells(2, Col_EB + 5) = "LH D Req (m)"
        Hoja_Resultados.Cells(2, Col_EB + 6) = "Chequeo LH D"

        Hoja_Resultados.Cells(2, Col_EB + 7) = "Tipo EB-Req I"
        Hoja_Resultados.Cells(2, Col_EB + 8) = "Tipo EB-Col I"
        Hoja_Resultados.Cells(2, Col_EB + 9) = "AshReq I (mm2)"
        Hoja_Resultados.Cells(2, Col_EB + 10) = "AshCol I (mm2)"
        Hoja_Resultados.Cells(2, Col_EB + 11) = "Chequeo EB I"

        Hoja_Resultados.Cells(2, Col_EB + 12) = "Tipo EB-Req D"
        Hoja_Resultados.Cells(2, Col_EB + 13) = "Tipo EB-Col D"
        Hoja_Resultados.Cells(2, Col_EB + 14) = "AshReq D (mm2)"
        Hoja_Resultados.Cells(2, Col_EB + 15) = "AshCol D (mm2)"
        Hoja_Resultados.Cells(2, Col_EB + 16) = "Chequeo EB D"

        Dim g As Integer = 3
        Dim R_Ash_Min As Single = 1
        Dim Fc_Max As Single = 35

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
            Dim Muro = proyecto.Muros.Lista_Muros(i)

            Hoja_Resultados.Cells(g, 1) = Muro.Name
            Hoja_Resultados.Cells(g, 2) = Muro.Label

            Dim F As Single = 100
            Dim Cuantia_Col As Single
            Dim Cuantia_Req As Single

            For j = 0 To Muro.Lista_Secciones.Count - 1

                Dim Tramo = Muro.Lista_Secciones(j)

                If (Tramo.Cuantia_Top_Col / Tramo.Cuantia_Top_Req) <= (Tramo.Cuantia_Bot_Col / Tramo.Cuantia_Bot_Req) Then
                    F = Tramo.Cuantia_Top_Col / Tramo.Cuantia_Top_Req
                    Cuantia_Col = Tramo.Cuantia_Top_Col
                    Cuantia_Req = Tramo.Cuantia_Top_Req
                Else
                    F = Tramo.Cuantia_Bot_Col / Tramo.Cuantia_Bot_Req
                    Cuantia_Col = Tramo.Cuantia_Bot_Col
                    Cuantia_Req = Tramo.Cuantia_Bot_Req
                End If

                Hoja_Resultados.Cells(g, 3) = Tramo.Piso

                Hoja_Resultados.Cells(g, 4) = Convert.ToString(Math.Round(Cuantia_Col * Tramo.Lw_Planos * Tramo.tw_Planos * 10000, 2) & " - (" & Math.Round(Cuantia_Col, 2) & " %)")
                Hoja_Resultados.Cells(g, 5) = Convert.ToString(Math.Round(Cuantia_Req * Tramo.Lw_Planos * Tramo.tw_Planos * 10000, 2) & " - (" & Math.Round(Cuantia_Req, 2) & " %)")
                Hoja_Resultados.Cells(g, 6) = Math.Round(F, 2)

                If F < 0.9 Then
                    Hoja_Resultados.Range("F" & g).Font.Color = Color.FromArgb(156, 0, 6)
                    Hoja_Resultados.Range("F" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                Else
                    Hoja_Resultados.Range("F" & g).Font.Color = Color.FromArgb(0, 97, 0)
                    Hoja_Resultados.Range("F" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                End If


                Hoja_Resultados.Cells(g, 7) = Math.Round(Tramo.AsH_Req_Top, 2)
                Hoja_Resultados.Cells(g, 8) = Math.Round(Tramo.AsH_Col, 2)
                Hoja_Resultados.Cells(g, 9) = Math.Round(Tramo.AsH_Col / Tramo.AsH_Req_Top, 2)

                If Tramo.AsH_Col < 0.9 * Tramo.AsH_Req_Top Then
                    Hoja_Resultados.Range("I" & g).Font.Color = Color.FromArgb(156, 0, 6)
                    Hoja_Resultados.Range("I" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                Else
                    Hoja_Resultados.Range("I" & g).Font.Color = Color.FromArgb(0, 97, 0)
                    Hoja_Resultados.Range("I" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                End If

                If Tramo.Chequeo_EB_I_Top_Esf = "No requiere" And
                    Tramo.Chequeo_EB_D_Top_Esf = "No requiere" And
                    Tramo.Chequeo_EB_I_Bot_Esf = "No requiere" And
                    Tramo.Chequeo_EB_D_Bot_Esf = "No requiere" And
                    Tramo.Chequeo_EB_I_Top_Def = "No requiere" And
                    Tramo.Chequeo_EB_I_Bot_Def = "No requiere" And
                    Tramo.Chequeo_EB_D_Top_Def = "No requiere" And
                    Tramo.Chequeo_EB_D_Bot_Def = "No requiere" Then

                    Hoja_Resultados.Cells(g, 10) = "No requiere"

                ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_I_Bot_Def = "Requiere") Or
                    (Tramo.Chequeo_EB_D_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_D_Bot_Def = "Requiere") Then

                    Hoja_Resultados.Cells(g, 10) = "Requiere por ambos"

                ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_I_Bot_Def = "No requiere") Or
                    (Tramo.Chequeo_EB_D_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_D_Bot_Def = "No requiere") Then

                    Hoja_Resultados.Cells(g, 10) = "Requiere por esfuerzos"

                ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_I_Bot_Def = "Requiere") Or
                    (Tramo.Chequeo_EB_D_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_D_Bot_Def = "Requiere") Then

                    Hoja_Resultados.Cells(g, 10) = "Requiere por deformaciones"
                End If

                Hoja_Resultados.Cells(g, 11) = Math.Round(Tramo.EB_I_Bot.L_EB, 2)
                Hoja_Resultados.Cells(g, 12) = Math.Round(Tramo.EB_I_Bot.L_EB_Req, 2)
                If Tramo.EB_I_Bot.L_EB >= 0.9 * Tramo.EB_I_Bot.L_EB_Req Then
                    Hoja_Resultados.Cells(g, 13) = "Cumple"
                    Hoja_Resultados.Range("M" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                Else
                    Hoja_Resultados.Cells(g, 13) = "No cumple"
                    Hoja_Resultados.Range("M" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                End If

                Hoja_Resultados.Cells(g, 14) = Math.Round(Tramo.EB_D_Bot.L_EB, 2)
                Hoja_Resultados.Cells(g, 15) = Math.Round(Tramo.EB_D_Bot.L_EB_Req, 2)
                If Tramo.EB_D_Bot.L_EB >= 0.9 * Tramo.EB_D_Bot.L_EB_Req Then
                    Hoja_Resultados.Cells(g, 16) = "Cumple"
                    Hoja_Resultados.Range("P" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                Else
                    Hoja_Resultados.Cells(g, 16) = "No cumple"
                    Hoja_Resultados.Range("P" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                End If

                Hoja_Resultados.Cells(g, 17) = Tramo.EB_I_Bot.Tipo_EB_Req
                Hoja_Resultados.Cells(g, 18) = Tramo.EB_I_Bot.Tipo_EB_Col
                Hoja_Resultados.Cells(g, 19) = Tramo.EB_I_Bot.RefH_Req
                Hoja_Resultados.Cells(g, 20) = Tramo.EB_I_Bot.RefH.Acero_T
                If Tramo.EB_I_Bot.RefH_Req * 0.9 <= Tramo.EB_I_Bot.RefH.Acero_T Then
                    Hoja_Resultados.Cells(g, 21) = "Cumple"
                    Hoja_Resultados.Range("U" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                Else
                    Hoja_Resultados.Cells(g, 21) = "No cumple"
                    Hoja_Resultados.Range("U" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                End If

                Hoja_Resultados.Cells(g, 22) = Tramo.EB_D_Bot.Tipo_EB_Req
                Hoja_Resultados.Cells(g, 23) = Tramo.EB_D_Bot.Tipo_EB_Col
                Hoja_Resultados.Cells(g, 24) = Tramo.EB_D_Bot.RefH_Req
                Hoja_Resultados.Cells(g, 25) = Tramo.EB_D_Bot.RefH.Acero_T
                If Tramo.EB_D_Bot.RefH_Req * 0.9 <= Tramo.EB_D_Bot.RefH.Acero_T Then
                    Hoja_Resultados.Cells(g, 26) = "Cumple"
                    Hoja_Resultados.Range("Z" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                Else
                    Hoja_Resultados.Cells(g, 26) = "No cumple"
                    Hoja_Resultados.Range("Z" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                End If

                g += 1
            Next
        Next

        Dim saveFileDialog1 As New SaveFileDialog()
        saveFileDialog1.Title = "Guardar documento Excel"
        saveFileDialog1.Filter = "Excel File|*.xlsx"
        saveFileDialog1.FileName = Convert.ToString(Archivo & "_Proyecto - " & PagInfoGeneral.NameProject.Text)
        saveFileDialog1.ShowDialog()
        wbXL.SaveAs(saveFileDialog1.FileName)
        appXL.Workbooks.Close()
        appXL.Quit()
        System.Diagnostics.Process.Start(saveFileDialog1.FileName)

        'Catch ex As Exception
        'MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Finally
        conexion.Close()
        Cursor = Cursors.Arrow
        'End Try


    End Sub

    Private Sub Form_06_01_ResultadosMuros_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Dim nuevaFuente As Font = New Font("Microsoft Sans Serif", 10, FontStyle.Bold)

        Dim Tabla = Tabla_Result_Flexo
        For i = 0 To Tabla.ColumnCount - 1
            If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                Tabla.Columns(i).ReadOnly = True
                Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
            End If
        Next

        Tabla = Tabla_Result_Cortante
        For i = 0 To Tabla.ColumnCount - 1
            If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                Tabla.Columns(i).ReadOnly = True
                Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
            End If
        Next

        Tabla = Tabla_Result_EB1
        For i = 0 To Tabla.ColumnCount - 1
            If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                Tabla.Columns(i).ReadOnly = True
                Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
            End If
        Next

        Tabla = Tabla_Result_EB2
        For i = 0 To Tabla.ColumnCount - 1
            If Tabla.Columns(i).GetType() Is GetType(DataGridViewTextBoxColumn) Then
                Tabla.Columns(i).ReadOnly = True
                Tabla.Columns(i).SortMode = DataGridViewColumnSortMode.NotSortable
                Tabla.Columns(i).HeaderCell.Style.Font = nuevaFuente
            End If
        Next





    End Sub

    Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click

        Me.Cursor = Cursors.WaitCursor

        Dim P As Single = 2

        Dim Archivo As String = "RevisiónMuros"
        Dim connection As String = "Provider=sqloledb;Data Source==miServidor;Initial Catalog=bdd_Web;User Id=web;Password="
        Dim conexion As New OleDbConnection(connection)

        'Try
        Dim Color_Interior As Color = Color.FromArgb(200, 200, 200)
        Dim appXL As New Excel.Application
        Dim wbXL As Excel.Workbook
        Dim Hoja_Resultados As Excel.Worksheet
        wbXL = appXL.Workbooks.Add()

        Hoja_Resultados = wbXL.Sheets("Hoja1")
        Hoja_Resultados.PageSetup.Application.ActiveWindow.DisplayGridlines = False
        Hoja_Resultados.Name = "Revisión Flexo-Compresion"

        'Hoja_Resultados.Range("A1:A2").MergeCells = True
        'Hoja_Resultados.Range("B1:B2").MergeCells = True
        'Hoja_Resultados.Range("C1:C2").MergeCells = True
        Hoja_Resultados.Range("A1:J2").Interior.Color = Color_Interior
        Hoja_Resultados.Range("A1:P10000").Font.Name = "Arial"
        Hoja_Resultados.Range("A1:P2").Font.Bold = True
        Hoja_Resultados.Range("A1:P2").Font.Size = 11
        Hoja_Resultados.Range("A3:P10000").Font.Size = 10
        Hoja_Resultados.Range("A1:P100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A1:P10000").VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Resultados.Range("A:J").ColumnWidth = 15

        Hoja_Resultados.Cells(1, 1) = "Nombre Muro - REV"
        Hoja_Resultados.Cells(1, 2) = "Nombre Muro - DIS"
        Hoja_Resultados.Cells(1, 3) = "Lw (m)"
        Hoja_Resultados.Cells(1, 4) = "tw (m)"
        Hoja_Resultados.Cells(1, 5) = "Piso"

        Hoja_Resultados.Cells(1, 6) = "Flexo-Compresión"
        Hoja_Resultados.Cells(2, 6) = "Cuantía Colocada"
        Hoja_Resultados.Cells(2, 7) = "Cuantía Requerida"
        Hoja_Resultados.Cells(2, 8) = "Capacidad/Demanda"
        Hoja_Resultados.Cells(2, 9) = "As Colocado (cm2)"
        Hoja_Resultados.Cells(2, 10) = "As Requerido (cm2)"
        Hoja_Resultados.Range("F1:J1").Merge(True)


        Dim g As Integer = 3
        Dim R_Ash_Min As Single = 1
        Dim Fc_Max As Single = 35

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
            Dim Muro = proyecto.Muros.Lista_Muros(i)

            Dim F As Single = 100
            Dim Cuantia_Col As Single
            Dim Cuantia_Req As Single

            Dim As_Col As Single
            Dim As_Req As Single

            For j = 0 To Muro.Lista_Secciones.Count - 1
                Dim Tramo = Muro.Lista_Secciones(j)

                If (Tramo.Cuantia_Top_Col / Tramo.Cuantia_Top_Req) <= (Tramo.Cuantia_Bot_Col / Tramo.Cuantia_Bot_Req) Then
                    F = Tramo.Cuantia_Top_Col / Tramo.Cuantia_Top_Req
                    Cuantia_Col = Tramo.Cuantia_Top_Col
                    Cuantia_Req = Tramo.Cuantia_Top_Req

                    As_Col = Tramo.AsT_Top_Col
                    As_Req = Tramo.As_Top_Req / 100
                Else
                    F = Tramo.Cuantia_Bot_Col / Tramo.Cuantia_Bot_Req
                    Cuantia_Col = Tramo.Cuantia_Bot_Col
                    Cuantia_Req = Tramo.Cuantia_Bot_Req

                    As_Col = Tramo.AsT_Bot_Col
                    As_Req = Tramo.As_Bot_Req / 100
                End If

                If F < 0.9 Then
                    Hoja_Resultados.Cells(g, 1) = Muro.Name
                    Hoja_Resultados.Cells(g, 2) = Muro.Label
                    Hoja_Resultados.Cells(g, 3) = Muro.Lw
                    Hoja_Resultados.Cells(g, 4) = Muro.tw
                    Hoja_Resultados.Cells(g, 5) = Tramo.Piso

                    Hoja_Resultados.Cells(g, 6) = Convert.ToString(Math.Round(Cuantia_Col, 2) & " %")
                    Hoja_Resultados.Cells(g, 7) = Convert.ToString(Math.Round(Cuantia_Req, 2) & " %")
                    Hoja_Resultados.Cells(g, 8) = Math.Round(F, 2)

                    If F < 0.9 Then
                        Hoja_Resultados.Range("H" & g).Font.Color = Color.FromArgb(156, 0, 6)
                        Hoja_Resultados.Range("h" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                    Else
                        Hoja_Resultados.Range("H" & g).Font.Color = Color.FromArgb(0, 97, 0)
                        Hoja_Resultados.Range("H" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                    End If

                    Hoja_Resultados.Cells(g, 9) = Math.Round(As_Col / 100, 2)
                    Hoja_Resultados.Cells(g, 10) = Math.Round(As_Req / 100, 2)

                    g += 1
                End If
            Next
        Next

        Dim Hoja_Cortante As Excel.Worksheet
        Hoja_Cortante = wbXL.Sheets.Add()
        Hoja_Cortante = wbXL.Sheets("Hoja2")
        Hoja_Cortante.PageSetup.Application.ActiveWindow.DisplayGridlines = False
        Hoja_Cortante.Name = "Revisión Cortante"
        g = 3

        Hoja_Cortante.Cells(1, 1) = "Nombre Muro - REV"
        Hoja_Cortante.Cells(1, 2) = "Nombre Muro - DIS"
        Hoja_Cortante.Cells(1, 3) = "Piso"
        Hoja_Cortante.Cells(1, 4) = "Revisión Cortante"
        Hoja_Cortante.Cells(2, 4) = "Av Req (mm2/m)"
        Hoja_Cortante.Cells(2, 5) = "Av Col (mm2/m)"
        Hoja_Cortante.Cells(2, 6) = "Av Col/Req"
        Hoja_Cortante.Range("D1:F1").Merge(True)

        Hoja_Cortante.Range("A1:A2").MergeCells = True
        Hoja_Cortante.Range("B1:B2").MergeCells = True
        Hoja_Cortante.Range("C1:C2").MergeCells = True
        Hoja_Cortante.Range("A1:F2").Interior.Color = Color_Interior
        Hoja_Cortante.Range("A1:P10000").Font.Name = "Arial"
        Hoja_Cortante.Range("A1:P2").Font.Bold = True
        Hoja_Cortante.Range("A1:P2").Font.Size = 11
        Hoja_Cortante.Range("A3:P10000").Font.Size = 10
        Hoja_Cortante.Range("A1:P100").HorizontalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Cortante.Range("A1:P10000").VerticalAlignment = Excel.XlVAlign.xlVAlignCenter
        Hoja_Cortante.Range("A:F").ColumnWidth = 15

        For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
            Dim Muro = proyecto.Muros.Lista_Muros(i)
            For j = 0 To Muro.Lista_Secciones.Count - 1
                Dim Tramo = Muro.Lista_Secciones(j)

                If Tramo.AsH_Col < 0.9 * Tramo.AsH_Req_Top Then
                    Hoja_Cortante.Cells(g, 1) = Muro.Name
                    Hoja_Cortante.Cells(g, 2) = Muro.Label
                    Hoja_Cortante.Cells(g, 3) = Tramo.Piso

                    Hoja_Cortante.Cells(g, 4) = Math.Round(Tramo.AsH_Req_Top, 2)
                    Hoja_Cortante.Cells(g, 5) = Math.Round(Tramo.AsH_Col, 2)
                    Hoja_Cortante.Cells(g, 6) = Math.Round(Tramo.AsH_Col / Tramo.AsH_Req_Top, 2)

                    If Tramo.AsH_Col < 0.9 * Tramo.AsH_Req_Top Then
                        Hoja_Cortante.Range("F" & g).Font.Color = Color.FromArgb(156, 0, 6)
                        Hoja_Cortante.Range("F" & g).Interior.Color = Color.FromArgb(255, 199, 206)
                    Else
                        Hoja_Cortante.Range("F" & g).Font.Color = Color.FromArgb(0, 97, 0)
                        Hoja_Cortante.Range("F" & g).Interior.Color = Color.FromArgb(198, 239, 206)
                    End If
                    g += 1
                End If
            Next
        Next


        'Dim Hoja_EB As Excel.Worksheet
        'Hoja_EB = wbXL.Sheets.Add("ElementodeBorde")
        'g = 3





        'For i = 0 To proyecto.Muros.Lista_Muros.Count - 1
        '    Dim Muro = proyecto.Muros.Lista_Muros(i)

        '    Hoja_EB.Cells(g, 1) = Muro.Label


        '    For j = 0 To Muro.Lista_Secciones.Count - 1
        '        Dim Tramo = Muro.Lista_Secciones(j)

        '        If Tramo.Chequeo_EB_I_Top_Esf = "No requiere" And Tramo.Chequeo_EB_D_Top_Esf = "No requiere" And Tramo.Chequeo_EB_I_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_D_Bot_Esf = "No requiere" Then
        '            Hoja_EB.Cells(g, 2) = "No requiere"
        '        ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_I_Bot_Def = "Requiere") Or (Tramo.Chequeo_EB_D_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_D_Bot_Def = "Requiere") Then
        '            Hoja_EB.Cells(g, 2) = "Requiere por ambos"
        '        ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_I_Bot_Def = "No requiere") Or (Tramo.Chequeo_EB_D_Bot_Esf = "Requiere" And Tramo.Chequeo_EB_D_Bot_Def = "No requiere") Then
        '            Hoja_EB.Cells(g, 2) = "Requiere por esfuerzos"
        '        ElseIf (Tramo.Chequeo_EB_I_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_I_Bot_Def = "Requiere") Or (Tramo.Chequeo_EB_D_Bot_Esf = "No requiere" And Tramo.Chequeo_EB_D_Bot_Def = "Requiere") Then
        '            Hoja_EB.Cells(g, 2) = "Requiere por deformaciones"
        '        End If

        '        Hoja_EB.Cells(g, 3) = Math.Round(Tramo.EB_I_Bot.L_EB, 2)
        '        Hoja_EB.Cells(g, 4) = Math.Round(Tramo.EB_I_Bot.L_EB_Req, 2)
        '        If Tramo.EB_I_Bot.L_EB / Tramo.EB_I_Bot.L_EB_Req < 0.9 Then
        '            Hoja_EB.Cells(g, 5) = "Cumple"
        '            Hoja_EB.Range("E" & g).Interior.Color = Color.FromArgb(198, 239, 206)
        '        Else
        '            Hoja_EB.Cells(g, 5) = "No cumple"
        '            Hoja_EB.Range("E" & g).Interior.Color = Color.FromArgb(255, 199, 206)
        '        End If

        '        Hoja_EB.Cells(g, 6) = Math.Round(Tramo.EB_D_Bot.L_EB, 2)
        '        Hoja_EB.Cells(g, 7) = Math.Round(Tramo.EB_D_Bot.L_EB_Req, 2)
        '        If Tramo.EB_I_Bot.L_EB / Tramo.EB_I_Bot.L_EB_Req < 0.9 Then
        '            Hoja_EB.Cells(g, 8) = "Cumple"
        '            Hoja_EB.Range("H" & g).Interior.Color = Color.FromArgb(198, 239, 206)
        '        Else
        '            Hoja_EB.Cells(g, 8) = "No cumple"
        '            Hoja_EB.Range("H" & g).Interior.Color = Color.FromArgb(255, 199, 206)
        '        End If

        '        g += 1
        '    Next
        'Next



        Dim saveFileDialog1 As New SaveFileDialog()
        saveFileDialog1.Title = "Guardar documento Excel"
        saveFileDialog1.Filter = "Excel File|*.xlsx"
        saveFileDialog1.FileName = Convert.ToString(Archivo & "_Proyecto - " & PagInfoGeneral.NameProject.Text)
        saveFileDialog1.ShowDialog()
        wbXL.SaveAs(saveFileDialog1.FileName)
        appXL.Workbooks.Close()
        appXL.Quit()
        System.Diagnostics.Process.Start(saveFileDialog1.FileName)

        'Catch ex As Exception
        'MessageBox.Show("Error al exportar los datos a excel.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        'Finally
        conexion.Close()
        Cursor = Cursors.Arrow

    End Sub
End Class