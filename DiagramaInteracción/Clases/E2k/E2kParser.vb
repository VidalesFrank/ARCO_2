Imports System.IO

' ════════════════════════════════════════════════════════════════════════════
'  Parser de archivos ETABS .e2k
'  Lee el archivo línea a línea detectando la sección actual ($) y extrayendo
'  los datos en las clases de E2kModel.
' ════════════════════════════════════════════════════════════════════════════

Public Class E2kParser

    Public Shared Function Parse(filePath As String) As E2kModel
        Dim model As New E2kModel()
        model.FilePath = filePath

        If Not File.Exists(filePath) Then Return model

        Dim allLines() As String
        Try
            allLines = File.ReadAllLines(filePath)
        Catch ex As Exception
            Logger.Error(ex, "E2kParser.Parse")
            Return model
        End Try

        Dim section As String = ""
        Dim sdCorners As New Dictionary(Of String, List(Of PointF))(StringComparer.OrdinalIgnoreCase)

        For Each rawLine As String In allLines
            Dim line As String = rawLine.Trim()
            If line = "" Then Continue For

            If line.StartsWith("$") Then
                section = line.Substring(1).Trim().ToUpper()
                Continue For
            End If

            Dim tok() As String = Tokenize(line)
            If tok.Length = 0 Then Continue For
            Dim kw = tok(0).ToUpper()

            Try
                Select Case section
                    Case "CONTROLS"
                        If kw = "UNITS" AndAlso tok.Length >= 2 Then
                            model.Units = tok(1)
                        End If

                    Case "STORIES - IN SEQUENCE FROM TOP"
                        ParseStory(tok, model)

                    Case "GRIDS"
                        ParseGrid(tok, model)

                    Case "POINT COORDINATES"
                        If kw = "POINT" AndAlso tok.Length >= 4 Then
                            model.Points(tok(1)) = New E2kPoint() With {
                                .ID = tok(1), .X = ToD(tok(2)), .Y = ToD(tok(3))
                            }
                        End If

                    Case "FRAME SECTIONS"
                        ParseFrameSection(tok, model)

                    Case "PIER SECTIONS"
                        If kw = "PIERSECTION" AndAlso tok.Length >= 2 Then
                            Dim pn = tok(1)
                            If Not model.PierSections.ContainsKey(pn) Then
                                model.PierSections(pn) = New E2kPierSection() With {.Name = pn}
                            End If
                            For i = 2 To tok.Length - 2
                                If tok(i).ToUpper() = "BASEMATERIAL" Then
                                    model.PierSections(pn).Material = tok(i + 1)
                                End If
                            Next i
                        End If

                    Case "SECTION DESIGNER SECTIONS"
                        ParseSDSection(tok, model, sdCorners)

                    Case "LINE CONNECTIVITIES"
                        If kw = "LINE" AndAlso tok.Length >= 5 Then
                            Dim ln As New E2kLine() With {.ID = tok(1)}
                            Select Case tok(2).ToUpper()
                                Case "COLUMN" : ln.LineType = E2kLineType.Column
                                Case "BEAM"   : ln.LineType = E2kLineType.Beam
                                Case "BRACE"  : ln.LineType = E2kLineType.Brace
                                Case Else     : ln.LineType = E2kLineType.Other
                            End Select
                            ln.Point1 = tok(3)
                            ln.Point2 = tok(4)
                            model.Lines(ln.ID) = ln
                        End If

                    Case "AREA CONNECTIVITIES"
                        ParseArea(tok, model)

                    Case "LINE ASSIGNS"
                        If kw = "LINEASSIGN" AndAlso tok.Length >= 3 Then
                            Dim la As New E2kLineAssign() With {
                                .LineID = tok(1), .Story = tok(2)
                            }
                            For i = 3 To tok.Length - 2
                                Select Case tok(i).ToUpper()
                                    Case "SECTION" : la.Section = tok(i + 1)
                                    Case "ANG"     : la.Angle = ToD(tok(i + 1))
                                End Select
                            Next i
                            model.LineAssigns.Add(la)
                        End If

                    Case "AREA ASSIGNS"
                        If kw = "AREAASSIGN" AndAlso tok.Length >= 3 Then
                            Dim aa As New E2kAreaAssign() With {
                                .AreaID = tok(1), .Story = tok(2)
                            }
                            For i = 3 To tok.Length - 2
                                Select Case tok(i).ToUpper()
                                    Case "SECTION"  : aa.Section = tok(i + 1)
                                    Case "PIER"     : aa.PierName = tok(i + 1)
                                    Case "SPANDREL" : aa.SpandrelName = tok(i + 1)
                                End Select
                            Next i
                            model.AreaAssigns.Add(aa)
                        End If

                    Case "PIER/SPANDREL NAMES"
                        If kw = "PIERNAME" AndAlso tok.Length >= 2 Then
                            If Not model.PierNames.Contains(tok(1)) Then
                                model.PierNames.Add(tok(1))
                            End If
                        End If
                End Select
            Catch
                ' Línea mal formada — ignorar
            End Try
        Next

        ' Transferir polígonos SDS a pier sections
        For Each kvp In sdCorners
            Dim parts = kvp.Key.Split("|"c)
            If parts.Length >= 1 AndAlso model.PierSections.ContainsKey(parts(0)) AndAlso kvp.Value.Count >= 3 Then
                model.PierSections(parts(0)).Shapes.Add(New List(Of PointF)(kvp.Value))
            End If
        Next

        ComputeStoryElevations(model)
        Return model
    End Function

    ' ─── Parsers por sección ─────────────────────────────────────────────────

    Private Shared Sub ParseStory(tok() As String, model As E2kModel)
        If tok(0).ToUpper() <> "STORY" OrElse tok.Length < 3 Then Return
        Dim st As New E2kStory() With {.Name = tok(1)}
        For i = 2 To tok.Length - 2
            Select Case tok(i).ToUpper()
                Case "HEIGHT"      : st.Height = ToD(tok(i + 1))
                Case "ELEV"        : st.ElevTop = ToD(tok(i + 1))   ' solo en S2 base
                Case "MASTERSTORY" : st.IsMaster = tok(i + 1).ToUpper() = "YES"
                Case "SIMILARTO"   : st.SimilarTo = tok(i + 1)
            End Select
        Next i
        model.Stories.Add(st)
    End Sub

    Private Shared Sub ParseGrid(tok() As String, model As E2kModel)
        If tok(0).ToUpper() <> "GENGRID" OrElse tok.Length < 2 Then Return
        Dim g As New E2kGrid()
        For i = 2 To tok.Length - 2
            Select Case tok(i).ToUpper()
                Case "LABEL" : g.Label = tok(i + 1)
                Case "X1"    : g.X1 = ToD(tok(i + 1))
                Case "Y1"    : g.Y1 = ToD(tok(i + 1))
                Case "X2"    : g.X2 = ToD(tok(i + 1))
                Case "Y2"    : g.Y2 = ToD(tok(i + 1))
            End Select
        Next i
        model.Grids.Add(g)
    End Sub

    Private Shared Sub ParseFrameSection(tok() As String, model As E2kModel)
        If tok(0).ToUpper() <> "FRAMESECTION" OrElse tok.Length < 2 Then Return
        Dim sn = tok(1)
        If Not model.FrameSections.ContainsKey(sn) Then
            model.FrameSections(sn) = New E2kFrameSection() With {.Name = sn}
        End If
        Dim fs = model.FrameSections(sn)
        For i = 2 To tok.Length - 2
            Select Case tok(i).ToUpper()
                Case "MATERIAL"
                    fs.Material = tok(i + 1)
                Case "D"
                    Dim dv = ToD(tok(i + 1))
                    If dv > 0 Then fs.D = dv
                Case "B"
                    Dim bv = ToD(tok(i + 1))
                    If bv > 0 Then fs.B = bv
                Case "SHAPE"
                    Dim shp = tok(i + 1).ToUpperInvariant()
                    If shp.Contains("CIRCLE") Then
                        fs.Shape = E2kSectionShape.Circle
                    ElseIf shp.Contains("I/WF") OrElse shp.Contains("I-SECT") Then
                        fs.Shape = E2kSectionShape.IShape
                    Else
                        fs.Shape = E2kSectionShape.Rectangular
                    End If
            End Select
        Next i
        ' Círculo: D es diámetro, usar para B también
        If fs.Shape = E2kSectionShape.Circle AndAlso fs.B = 0 AndAlso fs.D > 0 Then
            fs.B = fs.D
        End If
    End Sub

    Private Shared Sub ParseSDSection(tok() As String, model As E2kModel,
                                       sdCorners As Dictionary(Of String, List(Of PointF)))
        If tok(0).ToUpper() <> "SDSECTION" OrElse tok.Length < 3 Then Return
        Dim sdName = tok(1)
        If Not model.PierSections.ContainsKey(sdName) Then
            model.PierSections(sdName) = New E2kPierSection() With {.Name = sdName}
        End If
        ' Solo nos interesan las líneas POLYCORNER
        If tok(2).ToUpper() = "SHAPE" AndAlso tok.Length >= 5 Then
            Dim shapeNum = tok(3)
            Dim key = sdName & "|" & shapeNum
            For i = 4 To tok.Length - 1
                If tok(i).ToUpper() = "POLYCORNER" Then
                    If Not sdCorners.ContainsKey(key) Then
                        sdCorners(key) = New List(Of PointF)()
                    End If
                    Dim px As Single = 0, py As Single = 0
                    For j = i + 1 To tok.Length - 2
                        Select Case tok(j).ToUpper()
                            Case "X" : px = CSng(ToD(tok(j + 1)))
                            Case "Y" : py = CSng(ToD(tok(j + 1)))
                        End Select
                    Next j
                    sdCorners(key).Add(New PointF(px, py))
                    Exit For
                End If
            Next i
        End If
    End Sub

    Private Shared Sub ParseArea(tok() As String, model As E2kModel)
        If tok(0).ToUpper() <> "AREA" OrElse tok.Length < 5 Then Return
        Dim ar As New E2kArea() With {.ID = tok(1)}
        Select Case tok(2).ToUpper()
            Case "PANEL" : ar.AreaType = E2kAreaType.Panel
            Case "FLOOR" : ar.AreaType = E2kAreaType.Floor
            Case Else    : ar.AreaType = E2kAreaType.Other
        End Select
        Dim numPts As Integer = CInt(ToD(tok(3)))
        For i = 4 To 4 + numPts - 1
            If i < tok.Length Then ar.Points.Add(tok(i))
        Next i
        model.Areas(ar.ID) = ar
    End Sub

    ' ─── Post-proceso: elevaciones de pisos ──────────────────────────────────

    Private Shared Sub ComputeStoryElevations(model As E2kModel)
        ' Stories listados de arriba hacia abajo → invertir para procesar de abajo hacia arriba
        Dim ordered As New List(Of E2kStory)(model.Stories)
        ordered.Reverse()

        For i = 0 To ordered.Count - 1
            Dim st = ordered(i)
            If i = 0 Then
                ' Story base (S2 ELEV 0) — sin altura propia
                st.ElevBottom = 0
                st.ElevTop = 0
            Else
                st.ElevBottom = ordered(i - 1).ElevTop
                st.ElevTop = st.ElevBottom + st.Height
            End If
        Next i
    End Sub

    ' ─── Tokenizador ─────────────────────────────────────────────────────────

    Friend Shared Function Tokenize(line As String) As String()
        Dim result As New List(Of String)
        Dim i As Integer = 0

        While i < line.Length
            ' Saltar espacios
            While i < line.Length AndAlso Char.IsWhiteSpace(line(i))
                i += 1
            End While
            If i >= line.Length Then Exit While

            If line(i) = """"c Then
                ' Token entre comillas
                i += 1
                Dim start = i
                While i < line.Length AndAlso line(i) <> """"c
                    i += 1
                End While
                result.Add(line.Substring(start, i - start))
                If i < line.Length Then i += 1
            Else
                ' Token sin comillas
                Dim start = i
                While i < line.Length AndAlso Not Char.IsWhiteSpace(line(i))
                    i += 1
                End While
                result.Add(line.Substring(start, i - start))
            End If
        End While

        Return result.ToArray()
    End Function

    Private Shared Function ToD(s As String) As Double
        Dim v As Double
        Double.TryParse(s, Globalization.NumberStyles.Float,
                        Globalization.CultureInfo.InvariantCulture, v)
        Return v
    End Function

End Class
