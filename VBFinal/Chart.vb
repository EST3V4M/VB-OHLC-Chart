Imports System.IO

Public Class Chart
    Dim bitmap As Bitmap
    Dim entries As List(Of Entry) = New List(Of Entry)
    Dim MAs As List(Of Double) = New List(Of Double)
    Dim max As Double
    Dim min As Double
    Dim width As Double
    Dim MAValue As Integer = 0
    Dim startIndex As Integer = 0
    Dim range As Integer = 0

    Private Sub LoadData(sender As Object, e As EventArgs) Handles Button1.Click
        entries.Clear()
        If OpenFileDialog1.ShowDialog() = DialogResult.Cancel Then
            Return
        End If

        Dim csvReader As New StreamReader(OpenFileDialog1.OpenFile)
        Do While csvReader.Peek() <> -1
            Dim _entry As String() = csvReader.ReadLine().Split(",")
            Try
                Dim timeStamp As DateTime = DateTime.Parse(_entry(0))
                Dim open As Double = Double.Parse(_entry(1).Replace(".", ","))
                Dim high As Double = Double.Parse(_entry(2).Replace(".", ","))
                Dim low As Double = Double.Parse(_entry(3).Replace(".", ","))
                Dim close As Double = Double.Parse(_entry(4).Replace(".", ","))
                entries.Add(New Entry(timeStamp, open, high, low, close))
            Catch ex As Exception
                Continue Do
            End Try
        Loop

        For counter As Integer = 0 To entries.Count - 20
            MAs.Add((From x In entries Select x.AVG Skip counter Take 20).Average)
        Next
        'entries.Reverse()

        range = 50 'entries.Count
        HScrollBar1.Maximum = entries.Count - 1
        HScrollBar1.LargeChange = range
        width = PictureBox1.Width / range
        max = (From x In entries Select x.high).Max
        min = (From x In entries Select x.low).Min

        Label1.Text = max
        Label2.Text = min
        Label3.Text = (max - min) / 2 + min

        RenderChart()
    End Sub

    Private Sub Chart_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        bitmap = New Bitmap(PictureBox1.Width, PictureBox1.Height, Imaging.PixelFormat.Format32bppArgb)
        'If entries.Count = 0 Then
        '    Return
        'End If
        width = PictureBox1.Width / range
        RenderChart()
    End Sub

    Private Sub ZoomOut(sender As Object, e As EventArgs) Handles Button3.Click
        Zoom("out")
    End Sub
    Private Sub ZoomIn(sender As Object, e As EventArgs) Handles Button2.Click
        Zoom("in")
    End Sub
    Sub Zoom(type As String)
        If type = "in" Then
            If entries.Count = 0 Or range - 10 < 0 Then
                Return
            End If
            range -= 10
        ElseIf type = "out" Then
            If entries.Count = 0 Or range + 10 > entries.Count Then
                Return
            End If
            range += 10
        End If

        HScrollBar1.Maximum = entries.Count - 1
        HScrollBar1.LargeChange = range

        width = PictureBox1.Width / range
        max = (From x In entries Select x.high Skip startIndex Take range).Max
        min = (From x In entries Select x.low Skip startIndex Take range).Min

        Label1.Text = max
        Label2.Text = min
        Label3.Text = (max - min) / 2 + min

        RenderChart()
    End Sub

    Private Sub Scroll(sender As Object, e As EventArgs) Handles HScrollBar1.ValueChanged
        'If entries.Count = 0 Then
        '    Return
        'End If
        startIndex = HScrollBar1.Value

        'width = PictureBox1.Width / range
        max = (From x In entries Select x.high Skip startIndex Take range).Max
        min = (From x In entries Select x.low Skip startIndex Take range).Min

        'Label1.Text = max
        'Label2.Text = min
        'Label3.Text = (max - min) / 2 + min

        RenderChart()
    End Sub

    Private Sub DrawCandle(entry As Entry, index As Integer)
        Dim _y As Double
        Dim _height As Double
        If entry.open < entry.close Then
            _y = entry.open
            _height = entry.close - entry.open
        Else
            _y = entry.close
            _height = entry.open - entry.close
        End If
        Dim height As Double = (PictureBox1.Height * _height / (max - min))
        Dim y As Double = ((_y - min) * PictureBox1.Height) / (max - min)
        Dim color As Color = entry.color

        'open/close
        Dim g As Graphics = Graphics.FromImage(bitmap)
        g.FillRectangle(New SolidBrush(color), New Rectangle(width * index + 1, y, width - 1, height))

        'high/low
        Dim hlX As Integer = width * index + (width / 2)
        Dim hY As Integer = (entry.high - min) * PictureBox1.Height / (max - min)
        Dim lY As Integer = (entry.low - min) * PictureBox1.Height / (max - min)
        g.DrawLine(New Pen(color), hlX, lY, hlX, hY)
    End Sub

    Private Sub ToggleMA(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If MAValue = 0 Then
            MAValue = 20
        Else
            MAValue = 0
        End If

        RenderChart()
    End Sub
    Sub DrawMA(index As Integer)
        If index + startIndex >= MAValue And MAValue <> 0 Then
            Dim g As Graphics = Graphics.FromImage(bitmap)
            Dim sX As Integer = width * index
            Dim sY As Integer = (MAs(index + startIndex - 20) - min) * PictureBox1.Height / (max - min)
            Dim eX As Integer = width * (index + 1)
            Dim eY As Integer = (MAs(index + startIndex - 20 + 1) - min) * PictureBox1.Height / (max - min)
            g.DrawLine(New Pen(Color.Orange), sX, sY, eX, eY)
        End If
    End Sub

    Sub Position(sP As Double, sPIndex As Integer, eP As Double, ePIndex As Integer)
        Dim g As Graphics = Graphics.FromImage(bitmap)
        Dim x1 As Integer = width * sPIndex + width / 2
        Dim y1 As Integer = (sP - min) * PictureBox1.Height / (max - min)
        Dim x2 As Integer = width * ePIndex + width / 2
        Dim y2 As Integer = (eP - min) * PictureBox1.Height / (max - min)

        Dim gainLoss As Int16 = (((eP - sP) + sP) / sP * 100) - 100

        Dim color As Color
        If sP < eP Then
            color = Color.Green
        ElseIf sP > eP Then
            color = Color.Red
        Else
            color = Color.White
        End If
        g.DrawLine(New Pen(color, 2), x1, y1, x2, y1)
        g.DrawLine(New Pen(color, 2), x2, y1, x2, y2)

        g.DrawString(gainLoss.ToString + "%", Label1.Font, New SolidBrush(Color.White), New PointF(x1, y1))
    End Sub

    Sub RenderChart()
        bitmap = New Bitmap(PictureBox1.Width, PictureBox1.Height, Imaging.PixelFormat.Format32bppArgb)

        Dim Open As Boolean = False
        Dim sP As Double
        Dim sPIndex As Integer
        Dim eP As Double
        Dim ePIndex As Integer
        For i As Integer = startIndex To startIndex + range - 1 'entries.Count - 1
            DrawCandle(entries(i), i - startIndex)
            DrawMA(i - startIndex)

            If i >= 20 Then
                If MAs(i - 20) < entries(i).close And Not Open Then
                    'buy
                    Open = True
                    sP = MAs(i - startIndex + startIndex - 20)
                    sPIndex = i - startIndex
                ElseIf MAs(i - 20) > entries(i).close And Open Then
                    'sell
                    Open = False
                    eP = MAs(i - startIndex + startIndex - 20)
                    ePIndex = i - startIndex
                End If
            End If
            If sP <> 0 And eP <> 0 Then
                Position(sP, sPIndex, eP, ePIndex)
                sP = 0
                sPIndex = 0
                eP = 0
                sPIndex = 0
            End If
        Next
        bitmap.RotateFlip(RotateFlipType.Rotate180FlipX)
        PictureBox1.Image = bitmap
    End Sub
End Class

Public Class Entry
    Public open As Double
    Public high As Double
    Public low As Double
    Public close As Double
    Public timeStramp As DateTime
    Public AVG As Double
    Public color As Color

    Public Sub New(timeStamp As DateTime, open As Double, high As Double, low As Double, close As Double)
        Me.timeStramp = timeStamp
        Me.open = open
        Me.high = high
        Me.low = low
        Me.close = close
        Me.AVG = (high + low) / 2
        If open < close Then
            Me.color = Color.FromArgb(38, 166, 154)
        Else
            Me.color = Color.FromArgb(239, 83, 80)
        End If
    End Sub
End Class