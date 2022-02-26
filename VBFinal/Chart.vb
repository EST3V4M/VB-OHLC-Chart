Imports System.IO

Public Class Chart
    Dim bitmap As Bitmap
    Dim entries As List(Of Entry) = New List(Of Entry)
    Dim MAs As List(Of Double) = New List(Of Double)
    Dim max As Double
    Dim min As Double
    Dim width As Double
    Dim MAValue As Integer = 0
    Dim MAValue2 As Integer = 20
    Dim startIndex As Integer = 0
    Dim range As Integer = 0

    Private Sub LoadToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClearToolStripMenuItem.Click
        If MessageBox.Show("Are you sure?", "Clear Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            entries.Clear()
            MAs.Clear()
            PictureBox1.Image = Nothing
        End If
    End Sub

    Private Sub LoadData(sender As Object, e As EventArgs) Handles Button1.Click, LoadToolStripMenuItem.Click
        entries.Clear()
        MAs.Clear()
        If OpenFileDialog1.ShowDialog() = DialogResult.Cancel Then
            Return
        End If
        Panel1.Dispose()
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
        NumericUpDown1.Maximum = entries.Count
        NumericUpDown1.Value = 20
        NumericUpDown2.Maximum = entries.Count
        NumericUpDown2.Value = entries.Count
        Label5.Text = OpenFileDialog1.FileName.Split("/").Last.Split(".").First
        'entries = (From x In entries Select x Order By x.timeStramp Descending).ToList

        Dim test As Integer = PictureBox1.Width / 30
        range = entries.Count
        startIndex = 0

        HScrollBar1.Maximum = entries.Count - 1
        HScrollBar1.LargeChange = range

        RenderChart()
    End Sub

    Private Sub ToggleMA(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged

        If MAValue = 0 Then
            MAValue = MAValue2
        Else
            MAValue = 0
        End If

        If entries.Count <> 0 And MAs.Count = 0 Then
            For counter As Integer = 0 To entries.Count - MAValue
                MAs.Add((From x In entries Select x.AVG Skip counter Take MAValue).Average)
            Next
        End If


        RenderChart()
    End Sub
    Private Sub Zoom(sender As Object, e As EventArgs) Handles PictureBox1.MouseWheel, Button2.MouseClick, Button3.MouseClick
        If entries.Count = 0 Then
            Return
        End If
        Dim test = sender
        Dim zoomValue As Integer
        If sender Is PictureBox1 Then
            Dim mouseEvent As MouseEventArgs = e
            zoomValue = mouseEvent.Delta
        ElseIf sender Is Button2 Then
            zoomValue = 30
        ElseIf sender Is Button3 Then
            zoomValue = -30
        End If


        If range + (-zoomValue / 30) > entries.Count Then
            range += (-zoomValue / 30) - ((range + (-zoomValue / 30)) - entries.Count)
        ElseIf range + (-zoomValue / 30) < 1 Then
            range -= (zoomValue / 30) + (range + (-zoomValue / 30)) - 1
        Else
            range += (-zoomValue / 30)
        End If

        'If HScrollBar1.Value + range > entries.Count Then
        '    HScrollBar1.Value -= (HScrollBar1.Value + range) - entries.Count
        'End If
        HScrollBar1.Value -= (HScrollBar1.Value + range) - entries.Count
        startIndex = HScrollBar1.Value
        HScrollBar1.LargeChange = range

        NumericUpDown1.Value = MAValue2

        RenderChart()
    End Sub
    Private Sub Scroll(sender As Object, e As EventArgs) Handles HScrollBar1.Scroll
        startIndex = HScrollBar1.Value

        max = (From x In entries Select x.high Skip startIndex Take range).Max
        min = (From x In entries Select x.low Skip startIndex Take range).Min

        Label1.Text = max
        Label2.Text = min
        'Label3.Text = (max - min) / 2 + min

        RenderChart()
    End Sub

    Private Sub RenderChart() Handles MyBase.ResizeEnd, SplitContainer1.DragOver
        max = (From x In entries Select x.high Skip startIndex Take range).Max
        min = (From x In entries Select x.low Skip startIndex Take range).Min
        ListView1.Clear()
        bitmap = New Bitmap(PictureBox1.Width, PictureBox1.Height, Imaging.PixelFormat.Format32bppArgb)
        width = PictureBox1.Width / range
        Dim Open As Boolean = False
        Dim sP As Double
        Dim sPIndex As Integer
        Dim eP As Double
        Dim ePIndex As Integer
        For i As Integer = startIndex To startIndex + range - 1
            DrawCandle(entries(i), i - startIndex)
            DrawMA(i - startIndex)

            If i >= MAValue2 And MAValue <> 0 Then
                If MAs(i - MAValue2) < entries(i).close And Not Open Then
                    'buy
                    Open = True
                    sP = MAs(i - startIndex + startIndex - MAValue2)
                    sPIndex = i - startIndex
                ElseIf MAs(i - MAValue2) > entries(i).close And Open Then
                    'sell
                    Open = False
                    eP = MAs(i - startIndex + startIndex - MAValue2)
                    ePIndex = i - startIndex
                End If
            End If
            If sP <> 0 And eP <> 0 Then
                DrawPosition(sP, sPIndex, eP, ePIndex)
                sP = 0
                sPIndex = 0
                eP = 0
                sPIndex = 0
            End If
        Next

        Label1.Text = max
        Label2.Text = min
        'Label3.Text = (max - min) / 2 + min

        Label3.Text = entries(startIndex).timeStramp
        Label4.Text = entries(startIndex + range - 1).timeStramp

        SplitContainer1.SplitterDistance = SplitContainer1.Width - (Label1.Width + SplitContainer1.SplitterWidth + 10)

        PictureBox1.Image = bitmap
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
        g.FillRectangle(New SolidBrush(color), New Rectangle(width * index + 1, PictureBox1.Height - y - height, width - 1, height))

        'high/low
        Dim hlX As Integer = width * index + (width / 2)
        Dim hY As Integer = (entry.high - min) * PictureBox1.Height / (max - min)
        Dim lY As Integer = (entry.low - min) * PictureBox1.Height / (max - min)
        g.DrawLine(New Pen(color), hlX, PictureBox1.Height - lY, hlX, PictureBox1.Height - hY)
    End Sub
    Private Sub DrawMA(index As Integer)
        If index + startIndex >= MAValue And MAValue <> 0 Then
            Dim g As Graphics = Graphics.FromImage(bitmap)
            Dim sX As Integer = width * index + (width / 2)
            Dim sY As Integer = (MAs(index + startIndex - MAValue2) - min) * PictureBox1.Height / (max - min)
            Dim eX As Integer = width * (index + 1) + (width / 2)
            Dim eY As Integer = (MAs(index + startIndex - MAValue2 + 1) - min) * PictureBox1.Height / (max - min)
            g.DrawLine(New Pen(Color.Orange, 2), sX, PictureBox1.Height - sY, eX, PictureBox1.Height - eY)
        End If
    End Sub
    Private Sub DrawPosition(sP As Double, sPIndex As Integer, eP As Double, ePIndex As Integer)
        Dim g As Graphics = Graphics.FromImage(bitmap)
        Dim x1 As Integer = width * sPIndex + width / 2
        Dim y1 As Integer = (sP - min) * PictureBox1.Height / (max - min) 'maximo/minimo do MA passado
        Dim x2 As Integer = width * ePIndex + width / 2
        Dim y2 As Integer = (eP - min) * PictureBox1.Height / (max - min)

        Dim gainLoss As Double = Math.Truncate((eP / sP * 100 - 100) * 10) / 10

        Dim color As Color
        If sP < eP Then
            color = Color.DarkGreen
        ElseIf sP > eP Then
            color = Color.DarkRed
        Else
            color = Color.White
        End If


        Dim item As ListViewItem = New ListViewItem()
        item.Text = gainLoss.ToString + "%"
        item.BackColor = color
        ListView1.Items.Add(item)

        g.DrawLine(New Pen(Color.White, 1), x1, PictureBox1.Height - y1, x2 + 2, PictureBox1.Height - y1)
        g.DrawLine(New Pen(color, 5), x2, PictureBox1.Height - y1 + 1, x2, PictureBox1.Height - y2)
        g.DrawString(gainLoss.ToString + "%", Label1.Font, New SolidBrush(color), New PointF(x1 + ((x2 - x1) / 2) - (((gainLoss.ToString + "%").Length * 10)) / 2, PictureBox1.Height - y1))
    End Sub

    Private Sub Chart_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub


    Private Sub TextBox1_Leave(sender As Object, e As KeyPressEventArgs) Handles NumericUpDown1.KeyPress
        If MAValue = 0 Then
            Return
        End If
        If e.KeyChar = vbCr Then
            MAValue2 = NumericUpDown1.Value
            MAValue = MAValue2
            MAs.Clear()
            If entries.Count <> 0 And MAs.Count = 0 Then
                For counter As Integer = 0 To entries.Count - MAValue
                    MAs.Add((From x In entries Select x.AVG Skip counter Take MAValue).Average)
                Next
            End If
            RenderChart()
        End If

    End Sub

    Private Sub TextBox2_Leave(sender As Object, e As KeyPressEventArgs) Handles NumericUpDown2.KeyPress
        'If MAValue = 0 Then
        '    Return
        'End If
        If e.KeyChar = vbCr Then
            range = NumericUpDown2.Value
            HScrollBar1.Value -= HScrollBar1.Value + range - entries.Count
            startIndex = HScrollBar1.Value
            HScrollBar1.LargeChange = range

            RenderChart()
        End If

    End Sub

    Private Sub ClearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClearToolStripMenuItem.Click

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