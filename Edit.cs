using Microsoft.VisualBasic;
using ModemAPI;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LPH_Edit_Viewer
{
    public partial class Edit : Form
    {
        // LPH stuff
        private DiskStringBuilder outputExpression = new($"./temp/editor-{Guid.NewGuid()}.tmp");
        // graphics
        //private object _renderQueueLock = new object();
        //private List<RenderObject> renderQueue = new List<RenderObject>();
        private PolylineAssembler polylineAsm = new();
        // misc
        private bool isPresetLoaded = false;
        private object _renderLock = new();
        private bool hostSaved = false;
        private bool polylineDrawing = false;
        private int drawnPolylinesCount = 0;
        private int receivedPolylinesCount = 0;
        // drawing
        private bool isRedrawing = false;
        private Graphics graphics;
        private Graphics tempGraphics;
        private Graphics overlayGraphics;
        private Bitmap pictureOnScreen;
        private Bitmap overlayBitmap;
        private SelectedInstrument instrument = SelectedInstrument.None;
        private Guid currentPolylineUuid = Guid.Empty;
        private ulong currentPolylinePLDSeq = 0;
        // net
        private bool TransmitPictureToSerialPort = false;
        private bool IsConnectionActive = false;
        private bool isDownloadingBuffer = false;
        private int downloadProgess = 0;
        private bool usingTetronet = false;
        private IModem? Modem;
        private SRTPClient? ReliableTetronetClient;
        private int CHUNK_SIZE = 8000;
        private const uint LPH_CONNECTION_ID = 125000000;
        private const string LPH_QUERY_TYPE = "lphrp";
        //private UdpClient serialPort = new UdpClient();
        private object transmissionLock = new object();
        private IPEndPoint? communicatingWith;
        // more misc
        private Color currentColor = Color.Blue;
        private ViewersCursor[] cursors = new ViewersCursor[256];
        private long pingWasSent = 0;
        private bool alive = true;
        private bool renderingOverlay = true;
        // encryption
        private string login = "";
        private string password = "";
        private bool encryption = false;
        private bool actuallyEncrypting = false;
        private RandomNumberGenerator rng = RandomNumberGenerator.Create();
        private byte[] AesKey = new byte[32];
        // homemade tcp
        private const int TIMEOUT_MS = 2000;
        private int pps = 500;
        /*private ulong CurrentPacketNo = 0;
        private ulong CurrentPacketNoReceived = 0;
        private ulong ExpectedSeqReorderingBuffer = 0;
        private ulong LastPacketNoReceived = 0;
        private ConcurrentDictionary<ulong, long> PendingForAcknowledgement = new ConcurrentDictionary<ulong, long>();
        private HashSet<ulong> AlreadyReceived = new HashSet<ulong>();
        private ConcurrentDictionary<ulong, byte[]> ReorderingBuffer = new ConcurrentDictionary<ulong, byte[]>();
        private ConcurrentDictionary<ulong, byte[]> PendingForAcknowledgementData = new ConcurrentDictionary<ulong, byte[]>();*/
        private TcpClient? serialPort = null;
        private List<byte> receiveBuffer = [];
        private object _receiveBufferLock = new();
        public Edit(string filename, bool whatIsCarried)
        {
            InitializeComponent();
            graphics = panel1.CreateGraphics();
            pictureOnScreen = new Bitmap(panel1.Width, panel1.Height);
            overlayBitmap = new Bitmap(panel1.Width, panel1.Height);
            overlayGraphics = Graphics.FromImage(overlayBitmap);
            tempGraphics = Graphics.FromImage(pictureOnScreen);
            if (whatIsCarried)
            {
                if (filename != "")
                {
                    if (!outputExpression.Init(filename))
                    {
                        Interaction.MsgBox("Failed to open this file", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                    }
                }
                else
                {
                    outputExpression.Init();
                }
            }
            else
            {
                if (!outputExpression.Init())
                {
                    Interaction.MsgBox("Failed to open this file", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
                else
                {
                    outputExpression.Append(filename);
                }
            }
            Task.Run(async delegate ()
            {
                while (true)
                {
                    if (polylineAsm.Available)
                    {
                        receivedPolylinesCount++;
                        DebugWriter.WriteDebug("drawn polylines: " + receivedPolylinesCount);
                        lock (_renderLock)
                        {
                            //renderQueue.Add(polylineAsm.AvailablePolyline);
                            polylineAsm.AvailablePolyline.Draw(tempGraphics, new Rectangle(Point.Empty, panel1.Size));
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            });
            // render queue parser
            /*Task.Run(async delegate ()
            {
                while (true)
                {
                    if (renderQueue.Count != 0)
                    {
                        try
                        {
                            RenderObject[] renderQueueDump = new RenderObject[renderQueue.Count];
                            renderQueue.Take(renderQueueDump.Length).ToList().CopyTo(renderQueueDump);
                            foreach (RenderObject objectForDisplaying in renderQueueDump)
                            {
                                if (objectForDisplaying == null)
                                {
                                    continue;
                                }
                                objectForDisplaying.Draw(tempGraphics, new(new(), panel1.Size));
                            }
                            lock (_renderQueueLock)
                            {
                                renderQueue = [.. renderQueue.Skip(renderQueueDump.Length)];
                            }
                        }
                        catch (Exception e)
                        {
                            DebugWriter.WriteDebug($"editor - error while working with renderQueue: {e}");
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }
            });*/
        }

        private void ParseOutputExpression()
        {
            if (!isRedrawing)
            {
                isRedrawing = true;
                long idx = 0;
                while (true)
                {
                    idx = outputExpression.ReadBefore('\n', idx, out string shape);
                    string[] shapeData = shape.Split(' ');
                    switch (shapeData[0])
                    {
                        case "point":
                            try
                            {
                                decimal x_percent_point = decimal.Parse(shapeData[1]);
                                decimal y_percent_point = decimal.Parse(shapeData[2]);
                                int x_point = (int)(panel1.Width * x_percent_point / 100);
                                int y_point = (int)(panel1.Height * y_percent_point / 100);
                                Color shapeColor = Color.FromArgb((int)(decimal.Parse(shapeData[3] ?? "0") * 255), (int)(decimal.Parse(shapeData[4] ?? "0") * 255), (int)(decimal.Parse(shapeData[5] ?? "0") * 255));
                                lock (_renderLock)
                                {
                                    tempGraphics.FillEllipse(new SolidBrush(shapeColor), x_point - 2, y_point - 2, 4, 4);
                                }
                                if (!isPresetLoaded && TransmitPictureToSerialPort)
                                {
                                    try
                                    {
                                        string dataToSend = shape + "\r\n";
                                        Task.Run(() => Write(dataToSend));
                                    }
                                    catch
                                    {
                                        DebugWriter.WriteDebug("point was not transmitted");
                                    }
                                }
                                //Console.WriteLine("draw point");
                            }
                            catch (Exception e)
                            {
                                DebugWriter.WriteDebug($"error when decoding point: {e}");
                                DebugWriter.WriteDebug($"failed to decode this point: {shape}");
                            }
                            break;
                        case "line":
                            try
                            {
                                decimal x_percent_line = decimal.Parse(shapeData[1]);
                                decimal y_percent_line = decimal.Parse(shapeData[2]);
                                decimal x_end_percent_line = decimal.Parse(shapeData[3]);
                                decimal y_end_percent_line = decimal.Parse(shapeData[4]);
                                decimal red_color_line = decimal.Parse(shapeData[5] ?? "0");
                                decimal green_color_line = decimal.Parse(shapeData[6] ?? "0");
                                decimal blue_color_line = decimal.Parse(shapeData[7] ?? "1");
                                int x_line = (int)(panel1.Width * x_percent_line / 100);
                                int y_line = (int)(panel1.Height * y_percent_line / 100);
                                int x_end_line = (int)(panel1.Width * x_end_percent_line / 100);
                                int y_end_line = (int)(panel1.Height * y_end_percent_line / 100);
                                int red_line = (int)(red_color_line * 255);
                                int green_line = (int)(green_color_line * 255);
                                int blue_line = (int)(blue_color_line * 255);
                                lock (_renderLock)
                                {
                                    tempGraphics.DrawLine(new Pen(Color.FromArgb(red_line, green_line, blue_line)), x_line, y_line, x_end_line, y_end_line);
                                }
                                if (!isPresetLoaded && TransmitPictureToSerialPort)
                                {
                                    try
                                    {
                                        string dataToSend = shape + "\r\n";
                                        Task.Run(() => Write(dataToSend));
                                    }
                                    catch
                                    {
                                        DebugWriter.WriteDebug("line was not transmitted");
                                    }
                                }
                                //Console.WriteLine("draw line");
                            }
                            catch (Exception e)
                            {
                                DebugWriter.WriteDebug($"error when decoding line: {e}");
                                DebugWriter.WriteDebug($"failed to decode this line: {shape}");
                                DebugWriter.WriteDebug("this object will be disposed");
                                outputExpression.RemoveString(shape);
                            }
                            break;
                        case "polyline":
                        case "pld":
                            try
                            {
                                polylineAsm.ReceiveData(shape);
                            }
                            catch (Exception e)
                            {
                                DebugWriter.WriteDebug($"error when decoding this polyline/pld: {e}");
                                DebugWriter.WriteDebug($"failed to decode this polyline/pld: {shape}");
                            }
                            break;
                    }
                    if (idx >= outputExpression.GetTotalLength())
                    {
                        break;
                    }
                }
                isPresetLoaded = true;
                isRedrawing = false;
            }
        }

        private void Edit_Load(object sender, EventArgs e)
        {
            panel1.Paint += new PaintEventHandler(panel1_Paint);
            panel1.Refresh();
        }

        /*private byte[] ConvertLPHtToLPHb(string lph)
        {
            List<byte> output = new List<byte>();
            string[] objects = lph.Split(',');
            foreach (string shape in objects)
            {
                switch (shape)
                {
                    case "":
                        break;
                    case "point":

                        break;
                }
            }
            return output.ToArray();
        }*/

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

            //ParseOutputExpression();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.Line;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.Point;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.Rectangle;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.Brush;
        }

        private bool isSecondClick = false;
        private Point clickPos = new Point();
        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                switch (instrument)
                {
                    case SelectedInstrument.None:
                        break;
                    case SelectedInstrument.Line:
                        if (!isSecondClick)
                        {
                            clickPos = new Point(e.X, e.Y);
                            isSecondClick = true;
                        }
                        else
                        {
                            lock (_renderLock)
                            {
                                tempGraphics.DrawLine(new Pen(currentColor), clickPos, new Point(e.X, e.Y));
                            }
                            isSecondClick = false;
                            string toRender1 = $"line {(decimal)clickPos.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n";
                            outputExpression.Append(toRender1);
                            if (TransmitPictureToSerialPort)
                                Task.Run(() => Write(toRender1.Replace("\n", "\r\n")));
                            clickPos = new Point();
                        }
                        break;
                    case SelectedInstrument.LinePoint:
                        if (!isSecondClick)
                        {
                            clickPos = new Point(e.X, e.Y);
                            isSecondClick = true;
                        }
                        else
                        {
                            lock (_renderLock)
                            {
                                tempGraphics.DrawLine(new Pen(currentColor), clickPos, new Point(e.X, e.Y));
                                tempGraphics.FillEllipse(new SolidBrush(currentColor), e.X - 2, e.Y - 2, 4, 4);
                                tempGraphics.FillEllipse(new SolidBrush(currentColor), clickPos.X - 2, clickPos.Y - 2, 4, 4);
                            }
                            isSecondClick = false;
                            string toRender2 = $"line {(decimal)clickPos.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n" +
                                $"point {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n" +
                                $"point {(decimal)clickPos.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n";
                            outputExpression.Append(toRender2);
                            if (TransmitPictureToSerialPort)
                                Task.Run(() => Write(toRender2.Replace("\n", "\r\n")));
                            clickPos = new Point();
                        }
                        break;
                    case SelectedInstrument.Point:
                        lock (_renderLock)
                        {
                            tempGraphics.FillEllipse(new Pen(currentColor).Brush, e.X - 2, e.Y - 2, 4, 4);
                        }
                        string toRender3 = $"point {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n";
                        outputExpression.Append(toRender3);
                        if (TransmitPictureToSerialPort)
                            Task.Run(() => Write(toRender3.Replace("\n", "\r\n")));
                        DebugWriter.WriteDebug($"{e.X}:{e.Y}");
                        break;
                    case SelectedInstrument.Rectangle:
                        if (!isSecondClick)
                        {
                            isSecondClick = true;
                            clickPos = new Point(e.X, e.Y);
                        }
                        else
                        {
                            lock (_renderLock)
                            {
                                tempGraphics.DrawRectangle(new Pen(currentColor), new Rectangle(clickPos, new Size(e.X - clickPos.X, e.Y - clickPos.Y)));
                            }
                            isSecondClick = false;
                            string toRender4 = $"line {(decimal)clickPos.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)e.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n" +
                                $"line {(decimal)clickPos.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)clickPos.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n" +
                                $"line {(decimal)e.X / panel1.Width * 100} {(decimal)clickPos.Y / panel1.Height * 100} {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n" +
                                $"line {(decimal)clickPos.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n";
                            outputExpression.Append(toRender4);
                            if (TransmitPictureToSerialPort)
                                Task.Run(() => Write(toRender4.Replace("\n", "\r\n")));
                            clickPos = new Point();
                        }
                        break;
                    case SelectedInstrument.FillRectangeLines:
                        if (!isSecondClick)
                        {
                            isSecondClick = true;
                            clickPos = new Point(e.X, e.Y);
                        }
                        else
                        {
                            Task.Run(delegate ()
                            {
                                Point clickPosCopy = new(clickPos.X, Math.Min(clickPos.Y, e.Y));
                                Point eCopy = new Point(e.X, Math.Max(clickPos.Y, e.Y));
                                clickPos = new Point();
                                //MessageBox.Show("it worked for {(float)clickPos.Y / panel1.Height * 100} to {(float)e.Y / panel1.Height * 100}");
                                for (decimal i = (decimal)clickPosCopy.Y / panel1.Height * 100; i < (decimal)eCopy.Y / panel1.Height * 100; i += 0.02m)
                                {
                                    //Console.WriteLine("edit.cs filling: " + i.ToString());          
                                    int x_line = clickPosCopy.X;
                                    int y_line = (int)(panel1.Height * i / 100);
                                    int x_end_line = eCopy.X;
                                    int y_end_line = (int)(panel1.Height * i / 100);
                                    lock (_renderLock)
                                    {
                                        tempGraphics.DrawLine(new Pen(currentColor), x_line, y_line, x_end_line, y_end_line);
                                    }
                                    string lineObjectForAddingAndTransmittingFill = $"line {(decimal)clickPosCopy.X / panel1.Width * 100} {i} {(decimal)eCopy.X / panel1.Width * 100} {i} {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255}\n";
                                    outputExpression.Append(lineObjectForAddingAndTransmittingFill);
                                    if (TransmitPictureToSerialPort)
                                    {
                                        Write(lineObjectForAddingAndTransmittingFill);
                                    }
                                }
                            });
                            isSecondClick = false;
                        }
                        break;
                    case SelectedInstrument.Ruler:
                        if (!isSecondClick)
                        {
                            clickPos = new Point(e.X, e.Y);
                            DebugWriter.WriteDebug("ruler not second clk");
                            isSecondClick = true;
                        }
                        else
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            double x1 = (double)clickPos.X / panel1.Width * 100;
                            double y1 = (double)clickPos.Y / panel1.Height * 100;
                            double x2 = (double)e.X / panel1.Width * 100;
                            double y2 = (double)e.Y / panel1.Height * 100;
                            DebugWriter.WriteDebug($"rulering from {x1}:{y1} to {x2}:{y2}");
                            double distance = Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
                            lock (_renderLock)
                            {
                                tempGraphics.DrawLine(new Pen(Color.Black, 3), clickPos, e.Location);
                            }
                            Task.Run(() =>
                            {
                                Interaction.MsgBox($"Selected point are {distance} meters apart and calculating it took {sw.ElapsedTicks * 100}ns", MsgBoxStyle.Information, "Tsu-k Interruptive Window");
                                lock (_renderLock)
                                {
                                    tempGraphics.Clear(Color.White);
                                }
                                ParseOutputExpression();
                            });
                            clickPos = new Point();
                            isSecondClick = false;
                        }
                        break;
                    case SelectedInstrument.Triangle:
                        if (!isSecondClick)
                        {
                            clickPos = new Point(e.X, e.Y);
                            isSecondClick = true;
                        }
                        else
                        {
                            int topPointX = (clickPos.X + e.X) / 2;
                            decimal topPointXpercent = (decimal)topPointX / panel1.Width * 100;
                            tempGraphics.DrawLines(new Pen(currentColor), new Point[] {
                                new(topPointX, clickPos.Y),
                                e.Location,
                                new(clickPos.X, e.Y),
                                new(topPointX, clickPos.Y)
                            });
                            Guid trianglePolylineGuid = Guid.NewGuid();
                            string triangleData = $"polyline {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255} {trianglePolylineGuid}\n" +
                                $"pld {trianglePolylineGuid} 0 notLast {topPointXpercent} {(decimal)clickPos.Y / panel1.Height * 100}\n" +
                                $"pld {trianglePolylineGuid} 1 notLast {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100}\n" +
                                $"pld {trianglePolylineGuid} 2 notLast {(decimal)clickPos.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100}\n" +
                                $"pld {trianglePolylineGuid} 3 last {topPointXpercent} {(decimal)clickPos.Y / panel1.Height * 100}\n";
                            outputExpression.Append(triangleData);
                            if (TransmitPictureToSerialPort)
                            {
                                Write(triangleData);
                            }
                            clickPos = new Point();
                            isSecondClick = false;
                        }
                        break;
                }
                /*graphics.Clear(Color.White);
                ParseOutputExpression();*/
            }
            catch (Exception ex)
            {
                DebugWriter.WriteDebug(ex.ToString());
            }
        }

        private void button7_Click(object? sender, EventArgs? e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                outputExpression.Flush(saveFileDialog1.FileName);
                File.AppendAllText("Recent.text", saveFileDialog1.FileName + "\n");
                if (polylineTempContainer.Length != 0)
                {
                    throw new Exception("debug");
                }
                Task.Run(delegate () { MessageBox.Show("Successfuly saved file!"); });
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.LinePoint;
        }

        StringBuilder polylineTempContainer = new StringBuilder();

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (instrument == SelectedInstrument.Brush && !polylineDrawing)
            {
                DebugWriter.WriteDebug("panel1_MouseDown/instrumentBrush");
                polylineDrawing = true;
                Guid id = Guid.NewGuid();
                currentPolylineUuid = id;
                string polylineHeader = $"polyline {(decimal)currentColor.R / 255} {(decimal)currentColor.G / 255} {(decimal)currentColor.B / 255} {id}\n";
                string polylineFirstSegment = $"pld {currentPolylineUuid} {currentPolylinePLDSeq} notLast {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100}\n";
                currentPolylinePLDSeq++;
                polylineTempContainer.Append(polylineHeader).Append(polylineFirstSegment);
                BrushTimer.Start();
                lastCursorPosition = e.Location;
                if (TransmitPictureToSerialPort)
                {
                    Write(polylineHeader);
                    Write(polylineFirstSegment);
                }
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (instrument == SelectedInstrument.Brush && polylineDrawing)
            {
                drawnPolylinesCount++;
                DebugWriter.WriteDebug($"panel1_MouseUp/instrumentBrush, polyline {drawnPolylinesCount} was drawn");
                polylineDrawing = false;
                BrushTimer.Stop();
                string brushElementData = $"pld {currentPolylineUuid} {currentPolylinePLDSeq} last {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100}\n";
                polylineTempContainer.Append(brushElementData);
                outputExpression.Append(polylineTempContainer.ToString());
                currentPolylinePLDSeq = 0;
                Write(brushElementData);
                /*if (serialPort != null && TransmitPictureToSerialPort)
                {
                    try
                    {
                        Task.Run(delegate ()
                        {
                            foreach (string part in polylineTempContainer.ToString().Split('\n'))
                            {
                                Write(part);
                            }
                            polylineTempContainer.Clear();
                        });
                    }
                    catch
                    {
                        Console.WriteLine("err: Edit.cs, port");
                    }
                }
                else
                {
                    polylineTempContainer.Clear();
                }*/
                polylineTempContainer.Clear();
                lastCursorPosition = new Point();
            }
        }

        private Point lastCursorPosition = new Point();
        private void BrushTimer_Tick(object sender, EventArgs e)
        {
            if (polylineDrawing)
            {
                DebugWriter.WriteDebug($"drawing a polyline, total: {drawnPolylinesCount}, buffer: {polylineTempContainer.Length} bytes");
            }
            Point cursorPosition = panel1.PointToClient(Cursor.Position);
            if (!lastCursorPosition.Equals(cursorPosition))
            {
                lock (_renderLock)
                {
                    tempGraphics.DrawLine(new Pen(currentColor), lastCursorPosition, cursorPosition);
                }
                string brushElementData = $"pld {currentPolylineUuid} {currentPolylinePLDSeq} notLast {(decimal)cursorPosition.X / panel1.Width * 100} {(decimal)cursorPosition.Y / panel1.Height * 100}\n";
                currentPolylinePLDSeq++;
                Write(brushElementData);
                polylineTempContainer.Append(brushElementData);
                lastCursorPosition = cursorPosition;
            }
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            try
            {
                outputExpression.TruncateFromEndBefore('\n', out _);
                lock (_renderLock)
                {
                    if (!isRedrawing)
                    {
                        graphics.Clear(Color.White);
                        tempGraphics.Clear(Color.White);
                    }
                }
                if (TransmitPictureToSerialPort)
                    Task.Run(() => Write("cnc_lst"));    // cnc_lst = cancel last
                Task.Run(() =>
                {
                    try
                    {
                        ParseOutputExpression();
                    }
                    catch
                    {
                        Interaction.MsgBox("Error happened while parsing drawing. Try again.", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                    }
                });
            }
            catch
            {
                DebugWriter.WriteDebug("error when working with button5");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            lock (_renderLock)
            {
                if (!isRedrawing)
                {
                    graphics.Clear(Color.White);
                    tempGraphics.Clear(Color.White);
                }
            }
            Task.Run(() =>
            {
                try
                {
                    ParseOutputExpression();
                }
                catch
                {
                    Interaction.MsgBox("Error happened while parsing drawing. Try again.", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Interaction.MsgBox("Delete this drawing without any ability to recover?", MsgBoxStyle.YesNo, "Tsu-k Interruptive Window") == MsgBoxResult.Yes)
            {
                outputExpression.Clear();
                lock (_renderLock)
                {
                    graphics.Clear(Color.White);
                    tempGraphics.Clear(Color.White);
                }
                try
                {
                    if (TransmitPictureToSerialPort)
                        Task.Run(() => Write("clr"));
                }
                catch
                {
                    DebugWriter.WriteDebug("clear packet was unsent");
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            SerialPortOptions serialPortOptions = new SerialPortOptions();
            serialPortOptions.Show();
            serialPortOptions.OnSubmit(async delegate (SerialPortOptions options)
            {
                if (options.UseNetwork)
                {
                    try
                    {
                        if (options.TetronetAddress)
                        {
                            //await Task.Run(delegate ()
                            {
                                SetupTNet();
                                if (Modem == null)
                                {
                                    return;
                                }
                                ReliableTetronetClient = new(Modem, new(options.SerialPortName), LPH_QUERY_TYPE, LPH_CONNECTION_ID, TIMEOUT_MS * 10000);
                                ReliableTetronetClient.TicksPacketDelay = (int)(10000000d / options.DataBitrate);
                                usingTetronet = true;
                                _ = Task.Run(async delegate ()
                                {
                                    while (true)
                                    {
                                        Debug.WriteLine($"Currently there are {ReliableTetronetClient.CountReorderingPackets()} packets in the reordeing buffer");
                                        Debug.WriteLineIf(ReliableTetronetClient.AreAllPacketsFromReorderingBufferIncludedInAlreadyReceived(), "All packets in the ReordBuf were received at some point");
                                        await Task.Delay(1000);
                                    }
                                });
                            }//);
                        }
                        else
                        {
                            communicatingWith = new IPEndPoint(IPAddress.Parse(options.SerialPortName.Split(':')[0]), int.Parse(options.SerialPortName.Split(':')[1]));
                            //serialPort.Client.Bind(communicatingWith);
                            serialPort = new TcpClient();
                            serialPort.Connect(communicatingWith);
                        }
                        DebugWriter.WriteDebug("serial open");
                        if (!options.OpenNetworkPortWithoutConnecting)
                        {
                            _ = Task.Run(() => MessageBox.Show($"Connecting to network {options.SerialPortName}... Please wait", "Tsu-k Executive"));
                            /*if (options.AuthReqired)
                            {
                                Task.Run(() => Write($"l:{options.Login}"));     // l = login
                                Thread.Sleep(250);
                                Task.Run(() => Write($"k:{options.Password}"));  // k = key
                            }*/
                            login = options.Login;
                            DebugWriter.WriteDebug("login: " + options.Login);
                            password = options.Password;
                            AesKey = [.. SHA512.HashData(Encoding.UTF8.GetBytes(password)).Take(32)];
                            DebugWriter.WriteDebug("password was not printed out to the standart I/O stream for security reasons");
                            encryption = options.UsingEncryption;
                            DebugWriter.WriteDebug("encrypt: " + options.UsingEncryption);
                            pps = options.DataBitrate;
                            DebugWriter.WriteDebug("pps: " + options.DataBitrate);
                            StartWaitingForPackets();
                            Thread.Sleep(1000);
                            if (!usingTetronet)
                            {
                                _ = Task.Run(() => Write("cnt"));    // cnt = connect
                            }
                            else
                            {
                                Modem?.Transmit("cnt", new(options.SerialPortName), "lphrp", 125000000);
                            }
                            //Thread.Sleep(1000);
                            
                            TransmitPictureToSerialPort = true;
                        }
                        else
                        {
                            MessageBox.Show("Port was opened, but you didn't connect to the drawing network. Drawings will not be transmitted. You can forcefully transmit data to the interface by using \"Send Data To The Port\" button.", "Tsu-k Executive");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex, "Tsu-k Executive");
                    }
                }
                else
                {
                    MessageBox.Show("This client is not going to transmit your drawing to the network or port", "Tsu-k Executive");
                }
            });
        }
        public void UpdateTitle(string title)
        {
            Text = title;
        }

        private void StartWaitingForPackets()
        {
            // reordered packets parsing
            /*Task.Run(delegate ()
            {
                while (alive)
                {
                    if (ReorderingBuffer.Count == 0)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        bool packetsReordered = true;
                        ulong min = ReorderingBuffer.Keys.Min();
                        ulong max = ReorderingBuffer.Keys.Max();
                        for (ulong i = min; i <= max; i++)
                        {
                            if (!ReorderingBuffer.ContainsKey(i))
                            {
                                packetsReordered = false;
                                break;
                            }
                        }
                        if (packetsReordered)
                        {
                            Dictionary<ulong, byte[]> reordBufferCopy = new Dictionary<ulong, byte[]>(ReorderingBuffer);
                            for (ulong i = min; i <= max; i++)
                            {
                                byte[] data = reordBufferCopy[i];
                                // parse reordered server data grams
                                ParseServerDatagrams(Encoding.UTF8.GetString(data));
                                ReorderingBuffer.TryRemove(i, out _);
                            }
                        }
                    }
                }
            });*/
            // ack timeout watchdog
            /*Task.Run(delegate ()
            {
                while (alive)
                {
                    try
                    {
                        Dictionary<ulong, long> pendingForAckCopy = new Dictionary<ulong, long>(PendingForAcknowledgement);
                        foreach (KeyValuePair<ulong, long> packNoAndTime in pendingForAckCopy)
                        {
                            // check for timeouted packets
                            if (DateTime.Now.Ticks - packNoAndTime.Value > TIMEOUT_MS * 10000)
                            {
                                //Console.WriteLine($"!!! TIMEOUT FOR PACKET: {packNoAndTime.Key} !!!");
                                // prepare new array for retransmitted packet
                                if (!PendingForAcknowledgementData.ContainsKey(packNoAndTime.Key))
                                {
                                    Console.WriteLine("edit.cs: this packet id unexists in Pending For Ack Data dict");
                                    if (PendingForAcknowledgement.TryRemove(packNoAndTime.Key, out _))
                                    {
                                        Console.WriteLine("viewer.cs: deleting unexisting key success");
                                    }
                                    else
                                    {
                                        Console.WriteLine("viewer.cs: deleting unexisting key faild");
                                    }
                                    continue;
                                }
                                byte[] toSend = new byte[PendingForAcknowledgementData[packNoAndTime.Key].Length + 8];
                                // convert ulong packet seq number to byte[]
                                Span<byte> packNo = new byte[8];
                                BinaryPrimitives.WriteUInt64BigEndian(packNo, packNoAndTime.Key);
                                // copy bytes from arrays to array for sending
                                Array.Copy(packNo.ToArray(), toSend, 8);
                                Array.Copy(PendingForAcknowledgementData[packNoAndTime.Key], 0, toSend, 8, PendingForAcknowledgementData[packNoAndTime.Key].Length);
                                // transmit datagram
                                try
                                {
                                    serialPort.Send(toSend, toSend.Length, communicatingWith);
                                }
                                catch
                                {
                                    
                                }
                                // change time to now to prevent sending 41544465454545156 packets per second
                                PendingForAcknowledgement[packNoAndTime.Key] = DateTime.Now.Ticks;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"editor - error while handling timeouts: {e}");
                    }
                }
            });*/
            // packet receiver
            void OnMessageReceived(byte[] onWire)
            {
                try
                {
                    string dataReceived = "";
                    try
                    {
                        if (!actuallyEncrypting)
                        {
                            // read payload for this lph datagram
                            dataReceived = Encoding.UTF8.GetString([.. onWire]);
                        }
                        else
                        {
                            // read payload for this lph datagram
                            byte[] receivedEncrypted = onWire;
                            dataReceived = Aes256Helper.Decrypt(receivedEncrypted, AesKey);
                        }

                    }
                    catch (Exception e)
                    {
                        DebugWriter.WriteDebug($"error editor recv: {e}");
                    }
                    DebugWriter.WriteDebug("editor recv: " + dataReceived);
                    ParseServerDatagrams(dataReceived);
                }
                catch (Exception e)
                {
                    DebugWriter.WriteDebug($"(probably reading datagram from server, this is editor) exception = {e}");
                }
            }
            // ======= ПОТОК ЧТЕНИЯ (только читаем) =======
            Task.Run(async delegate ()
            {
                if (!usingTetronet)
                {
                    if (serialPort == null)
                    {
                        throw new NullReferenceException("serialPort was null.");
                    }
                    var stream = serialPort.GetStream(); // предполагаю, что это NetworkStream
                    byte[] buffer = new byte[8192]; // буфер для чтения

                    while (true)
                    {
                        try
                        {
                            if (!serialPort.Connected)
                            {
                                break; // сокет закрыт
                            }
                            // Читаем БЛОКОМ, а не по Available
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            //if (bytesRead == 0) break; 

                            

                            lock (_receiveBufferLock)
                            {
                                // Добавляем ТОЛЬКО прочитанные байты
                                receiveBuffer.AddRange(buffer.Take(bytesRead));
                            }
                        }
                        catch (Exception e)
                        {
                            DebugWriter.ReceiveException("Read thread", e);
                            await Task.Delay(1000);
                        }
                    }
                }
                else
                {
                    if (ReliableTetronetClient == null)
                    {
                        throw new NullReferenceException("ReliableTetronetClient was null.");
                    }
                    ReliableTetronetClient.OnMessageReceived += delegate (SRTPClient sender, byte[] data)
                    {
                        lock (_receiveBufferLock)
                        {
                            receiveBuffer.AddRange(data);
                        }
                        DebugWriter.WriteDebug($"editor SRTP received {data.Length} bytes of data");
                    };
                }
            });

            // ======= ПОТОК ОБРАБОТКИ (разбираем пакеты) =======
            Task.Run(delegate ()
            {
                while (true)
                {
                    try
                    {
                        // ВСЮ проверку делаем внутри одного lock, без continue внутри!
                        int packetLength = 0;
                        bool hasPacket = false;
                        byte[]? onWire = null;

                        lock (_receiveBufferLock)
                        {
                            // Проверяем, хватает ли данных для чтения длины (4 байта)
                            if (receiveBuffer.Count >= 4)
                            {
                                // Читаем длину БЕЗ создания копии всего буфера
                                packetLength = (receiveBuffer[0] << 24) | (receiveBuffer[1] << 16) |
                                               (receiveBuffer[2] << 8) | receiveBuffer[3];

                                // Проверяем, хватает ли данных для всего пакета
                                if (receiveBuffer.Count >= packetLength + 4)
                                {
                                    // Копируем ТОЛЬКО тело пакета
                                    onWire = [.. receiveBuffer.Skip(4).Take(packetLength)];
                                    receiveBuffer.RemoveRange(0, packetLength + 4);
                                    hasPacket = true;
                                }
                            }
                        } // Блокировка ЗДЕСЬ освобождается!

                        // Обработка ВНЕ блокировки, чтобы не тормозить чтение
                        if (hasPacket)
                        {
                            if (packetLength > 1073741824)
                            {
                                Interaction.MsgBox($"Length is bigger than 1 gigabyte (how your pc is still alive at this point)... Packet says {packetLength} bytes.",
                                                   MsgBoxStyle.OkOnly, "Tsu-k Interruptive Window");
                            }
                            else
                            {
                                if (onWire != null)
                                {
                                    OnMessageReceived(onWire);
                                }
                            }
                        }
                        else
                        {
                            // Если пакета нет - ждем 1 мс, чтобы не грузить процессор
                            Thread.Sleep(1); // или await Task.Delay(1) если метод async
                        }
                    }
                    catch (Exception e)
                    {
                        DebugWriter.ReceiveException("Viewer, network buffer read", e);
                    }
                }
            });
        }
        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                BrushTimer.Interval = 1000 / int.Parse(Interaction.InputBox("Enter Line Sample Rate (default: 50): ", "Tsu-k Executive"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.GetType() + ": " + ex.Message);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort == null)
                {
                    throw new NullReferenceException("data port is not configured properply");
                }
                Task.Run(() => Write($"{Interaction.InputBox("Enter message to be sent to the serial: ", "Tsu-k Executive")}\r\n"));
            }
            catch (Exception ex)
            {
                Interaction.MsgBox("Error: " + ex.GetType() + ": " + ex.Message, MsgBoxStyle.Critical, "Network error");
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            MessageBox.Show("You don't need to enter following criteria: \"Network Port\", \"Output Data Rate\".");
            SerialPortOptions serialPortOptions = new SerialPortOptions();
            serialPortOptions.Show();
            serialPortOptions.OnSubmit(delegate (SerialPortOptions options)
            {
                if (options.UseNetwork)
                {
                    TransmitPictureToSerialPort = true;
                    try
                    {
                        DebugWriter.WriteDebug("serial open");
                        Task.Run(() => Write("cnt"));    // cnt = connect
                        Thread.Sleep(250);
                        Task.Run(() => Write("binit"));  // binit = buffer initialize
                        /*if (options.AuthReqired)
                        {
                            Task.Run(() => Write($"l:{options.Login}"));     // l = login
                            Thread.Sleep(250);
                            Task.Run(() => Write($"k:{options.Password}"));  // k = key
                        }*/
                        login = options.Login;
                        password = options.Password;
                        encryption = options.UsingEncryption;
                        StartWaitingForPackets();
                        MessageBox.Show("Connected to network " + options.SerialPortName, "Tsu-k Executive");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex, "Tsu-k Executive");
                    }
                }
                else
                {
                    MessageBox.Show("This client is not going to transmit your drawing to the network or port", "Tsu-k Executive");
                }
            });
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                currentColor = colorDialog1.Color;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            serialPort?.Close();
            Close();
        }
        private void CursorUpdater_Tick(object sender, EventArgs e)
        {
            /*try
            {
                foreach (ViewersCursor cursor in cursors)
                {
                    if (cursor != null)
                    {
                        if (!lastVCursorpos.Equals(new Point(cursor.MouseX, cursor.MouseY)))
                        {
                            lock (_renderLock)
                            {
                                overlayGraphics.FillRectangle(Brushes.White, new Rectangle(lastVCursorpos, new Size(30, 30)));
                            }
                        }
                        lastVCursorpos = new Point(cursor.MouseX - 15, cursor.MouseY - 15);
                        lock (_renderLock)
                        {
                            overlayGraphics.DrawEllipse(new Pen(cursor.CursorColor), new Rectangle(cursor.MouseX - 10, cursor.MouseY - 10, 20, 20));
                        }
                    }
                }
            }
            catch
            {
                DebugWriter.WriteDebug("Cursor update failed");
            }*/
        }

        private void ServerPingTimer_Tick(object sender, EventArgs e)
        {
            if (TransmitPictureToSerialPort)
            {
                Task.Run(() => Write("ping\r\n"));
            }
        }


        private void button16_Click(object sender, EventArgs e)
        {
            if (TransmitPictureToSerialPort)
            {
                MessageBox.Show($"Server response must took less than 10000ms");
                Task.Run(() => Write("ping\r\n"));
                pingWasSent = DateTime.Now.Ticks;
            }
            else
            {
                Interaction.MsgBox("Error: port error", MsgBoxStyle.Critical, "Network error");
            }

        }

        private void Edit_Shown(object sender, EventArgs e)
        {
            Task.Run(delegate ()
            {
                try
                {
                    ParseOutputExpression();
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox($"Error happened while rendering this buffer: {ex}", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            });
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (!TransmitPictureToSerialPort)
            {
                Interaction.MsgBox("You're not connected to any drawing network which is required for this operation.", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                return;
            }
            if (Interaction.MsgBox("This operation could potentially delete your drawing if the server is dead. Proceed?", MsgBoxStyle.YesNo, "Tsu-k Interruptive Window") == MsgBoxResult.Yes)
            {
                try
                {
                    lock (_renderLock)
                    {
                        graphics.Clear(Color.White);
                        tempGraphics.Clear(Color.White);
                    }
                    outputExpression.Clear();
                    Task.Run(() => Write("bufget"));
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox($"Error happened while fetching remote buffer: {ex}", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            }
            else
            {
                MessageBox.Show("Operation was terminated by the user.", "Tsu-k Executive");
            }
        }

        /*private void FDrawPolyline(string data)
        {
            if (!data.StartsWith("polyline"))
            {
                throw new Exception("not a polyline figure");
            }
            List<PointM> path = new List<PointM>();
            string[] posdata = data.Split(' ').Skip(4).ToArray();
            for (int i = 0; i < posdata.Length - 2; i += 2)
            {
                path.Add(PointM.FromStrings(posdata[i], posdata[i + 1]));
            }
            List<Point> pathInt = new List<Point>();
            for (int i = 0; i < path.Count; i++)
            {
                pathInt.Add(path[i].ToPoint(panel1.Width, panel1.Height));
            }
            Color polylineColor = Color.FromArgb((int)(decimal.Parse(data.Split(' ')[1]) * 255), (int)(decimal.Parse(data.Split(' ')[2]) * 255), (int)(decimal.Parse(data.Split(' ')[3]) * 255));
            tempGraphics.DrawLines(new Pen(polylineColor), pathInt.ToArray());
        }*/

        private void button18_Click(object sender, EventArgs e)
        {
            if (Interaction.MsgBox("This operation is for debugging purposes only. It will show full buffer value (as text). Can potentially crash this program if buffer is really big", MsgBoxStyle.YesNo, "Tsu-k Interruptive Window") == MsgBoxResult.Yes)
            {
                new TsukTerminal(outputExpression.ToString()).Show();
            }
            else
            {
                MessageBox.Show("Operation was terminated by the user.", "Tsu-k Executive");
            }
        }
        long lastpkttx = 0;
        private void Write(string data)
        {
            lock (transmissionLock)
            {
                try
                {
                    while (DateTime.Now.Ticks - lastpkttx < 10000000 / pps) { }
                    if (!actuallyEncrypting)
                    {
                        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                        byte[] dataToSend = new byte[dataBytes.Length + 4];
                        dataBytes.CopyTo(dataToSend, 4);
                        BinaryPrimitives.WriteInt32BigEndian(dataToSend, dataBytes.Length);
                        UnivWrite(dataToSend);
                    }
                    else
                    {
                        byte[] encrypted = Aes256Helper.Encrypt(data, AesKey);
                        byte[] dataToSend = new byte[4 + encrypted.Length];
                        encrypted.CopyTo(dataToSend, 4);
                        BinaryPrimitives.WriteInt32BigEndian(dataToSend, encrypted.Length);
                        UnivWrite(dataToSend);
                    }
                    lastpkttx = DateTime.Now.Ticks;
                }
                catch (Exception e)
                {
                    Interaction.MsgBox($"VIEWER: Error while transmitting datagram: {e}", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            }
        }
        private void UnivWrite(byte[] data)
        {
            if (usingTetronet)
            {
                if (ReliableTetronetClient == null) return;

                if (data.Length <= CHUNK_SIZE)
                {
                    ReliableTetronetClient.Transmit(data);
                }
                else
                {
                    for (int offset = 0; offset < data.Length; offset += CHUNK_SIZE)
                    {
                        int remaining = data.Length - offset;
                        int chunkSize = Math.Min(CHUNK_SIZE, remaining);
                        var chunk = data.AsSpan(offset, chunkSize).ToArray();
                        ReliableTetronetClient.Transmit(chunk);
                    }
                }
            }
            else
            {
                if (serialPort == null)
                {
                    return;
                }
                if (!serialPort.Connected)
                {
                    return;
                }
                serialPort.GetStream().Write(data, 0, data.Length);
            }
        }

        private void Edit_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!hostSaved)
            {
                MsgBoxResult userResponse = Interaction.MsgBox("Closing the Editor while image is modified and not saved will result in data loss. Save?", MsgBoxStyle.YesNoCancel, "Tsu-k Interruptive Window");
                if (userResponse == MsgBoxResult.Yes)
                {
                    button7_Click(null, null); // virtually press the "Save..." button

                }
                else if (userResponse == MsgBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            alive = false;
            try
            {
                if (serialPort != null)
                {
                    try
                    {
                        if (IsConnectionActive)
                        {
                            Write("session_reset");
                            Thread.Sleep(100);
                            serialPort.Close();
                            //serialPort.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() => Interaction.MsgBox("Failed to disconnect!", MsgBoxStyle.Critical, "Tsu-k Interruptive Window"));
                        DebugWriter.WriteDebug($"editor failed to disconnect: {ex}");
                    }
                }
                outputExpression.Dispose();
            }
            catch
            {

            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.FillRectangeLines;
        }

        private void ScreenUpdater_Tick(object sender, EventArgs e)
        {
            try
            {
                using (Bitmap b = new(panel1.Width, panel1.Height))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.DrawImageUnscaled(pictureOnScreen, 0, 0);
                        if (renderingOverlay)
                        {
                            g.DrawImageUnscaled(overlayBitmap, 0, 0);
                        }
                        if (TransmitPictureToSerialPort)
                        {
                            using (Bitmap cursorsOverlay = Helpers.GetDrawnCursors(cursors, panel1.Width, panel1.Height))
                            {
                                g.DrawImageUnscaled(cursorsOverlay, 0, 0);
                            }
                        }
                    }
                    lock (_renderLock)
                    {
                        graphics.DrawImageUnscaled(b, 0, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugWriter.ReceiveException("ScreenUpdater", ex);
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            try
            {
                if (IsConnectionActive)
                {
                    Task.Run(() => Write("session_reset"));
                }
            }
            catch (Exception ex)
            {
                Task.Run(() => Interaction.MsgBox("Failed to disconnect!", MsgBoxStyle.Critical, "Tsu-k Interruptive Window"));
                DebugWriter.WriteDebug($"editor failed to disconnect: {ex}");
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.Ruler;
        }

        private void button22_Click(object sender, EventArgs e)
        {
            renderingOverlay = !renderingOverlay;
            if (renderingOverlay)
            {
                button22.Text = "Show overlay: True";
            }
            else
            {
                button22.Text = "Show overlay: False";
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            Task.Run(async delegate ()
            {
                overlayGraphics.Clear(Color.Transparent);
                overlayGraphics.DrawString($"=== DEBUG INFO ABOUT LPH ===\nBufferSize: {(double)outputExpression.GetTotalLength() / 1048576}M", new Font("Times New Roman", 20), new SolidBrush(Color.Red), 10, 10);
                await Task.Delay(1000);
                overlayGraphics.Clear(Color.Transparent);
            });
        }

        private void ParseServerDatagrams(string dataReceived)
        {
            string[] receivedCommands = dataReceived.Replace("\r", "").Split('\n');
            foreach (string command in receivedCommands)
            {
                string[] dataReceivedParts = command.Split(' ');
                switch (dataReceivedParts[0].Trim())
                {
                    case "":
                        break;
                    case "bufend":
                        isDownloadingBuffer = false;
                        break;
                    case "bufsize":
                        Console.WriteLine($"Receiving {dataReceivedParts[1]} objects from the remote server");
                        break;
                    case "pong":
                        Task.Run(() => MessageBox.Show($"The server response took {(DateTime.Now.Ticks - pingWasSent) / 10000}ms", "Tsu-k Interruptive Window"));
                        break;
                    case "session_reset_acknowledged":
                        Task.Run(() => MessageBox.Show("Disconnected successfully! Now you can draw locally without sending data to a remote server.", "Tsu-k Interruptive Window"));
                        TransmitPictureToSerialPort = false;
                        ReliableTetronetClient?.Close();
                        serialPort?.Close();
                        break;
                    case "curpos":
                        // curpos = cursor position `curpos <x> <y> <r> <g> <b> <cursor_id> <mouse_button>`
                        DebugWriter.WriteDebug("received cursor");
                        ViewersCursor cursor = new();
                        try
                        {
                            cursor.MouseX = (int)(decimal.Parse(dataReceivedParts[1]) / 100 * panel1.Width);
                            cursor.MouseY = (int)(decimal.Parse(dataReceivedParts[2]) / 100 * panel1.Height);
                            cursor.CursorColor = Color.FromArgb(
                                (int)(decimal.Parse(dataReceivedParts[3]) * 255),
                                (int)(decimal.Parse(dataReceivedParts[4]) * 255),
                                (int)(decimal.Parse(dataReceivedParts[5]) * 255)
                            );
                            cursor.CursorID = int.Parse(dataReceivedParts[6]);
                            cursor.MouseButton = int.Parse(dataReceivedParts[7]);
                        }
                        catch
                        {
                            DebugWriter.WriteDebug("error while decoding remote cursor position");
                        }
                        cursors[cursor.CursorID] = cursor;
                        break;
                    case "epoint":
                        try
                        {
                            decimal x_percent_point = decimal.Parse(dataReceivedParts[1]);
                            decimal y_percent_point = decimal.Parse(dataReceivedParts[2]);
                            int x_point = (int)(panel1.Width * x_percent_point / 100);
                            int y_point = (int)(panel1.Height * y_percent_point / 100);
                            Color shapeColor = Color.FromArgb((int)(decimal.Parse(dataReceivedParts[3] ?? "0") * 255), (int)(decimal.Parse(dataReceivedParts[4] ?? "0") * 255), (int)(decimal.Parse(dataReceivedParts[5] ?? "0") * 255));
                            lock (_renderLock)
                            {
                                tempGraphics.FillEllipse(new SolidBrush(shapeColor), x_point - 2, y_point - 2, 4, 4);
                            }
                            outputExpression.Append($"point {x_percent_point} {y_percent_point} {shapeColor.R / 255m} {shapeColor.G / 255m} {shapeColor.B / 255m}\n");
                            /*lock (_renderQueueLock)
                            {
                                renderQueue.Add(new RenderObject(
                                    false,
                                    false,
                                    x_point,
                                    0,
                                    y_point,
                                    0,
                                    shapeColor,
                                    x_percent_point,
                                    0,
                                    y_percent_point,
                                    0,
                                    decimal.Parse(dataReceivedParts[3] ?? "0"),
                                    decimal.Parse(dataReceivedParts[4] ?? "0"),
                                    decimal.Parse(dataReceivedParts[5] ?? "0")
                                ));
                            }*/
                            DebugWriter.WriteDebug("draw point");
                        }
                        catch
                        {
                            DebugWriter.WriteDebug("error when decoding point");
                        }
                        break;
                    case "eline":
                        try
                        {
                            decimal x_percent_line = decimal.Parse(dataReceivedParts[1]);
                            decimal y_percent_line = decimal.Parse(dataReceivedParts[2]);
                            decimal x_end_percent_line = decimal.Parse(dataReceivedParts[3]);
                            decimal y_end_percent_line = decimal.Parse(dataReceivedParts[4]);
                            decimal red_color_line = decimal.Parse(dataReceivedParts[5] ?? "0");
                            decimal green_color_line = decimal.Parse(dataReceivedParts[6] ?? "0");
                            decimal blue_color_line = decimal.Parse(dataReceivedParts[7] ?? "1");
                            int x_line = (int)(panel1.Width * x_percent_line / 100);
                            int y_line = (int)(panel1.Height * y_percent_line / 100);
                            int x_end_line = (int)(panel1.Width * x_end_percent_line / 100);
                            int y_end_line = (int)(panel1.Height * y_end_percent_line / 100);
                            int red_line = (int)(red_color_line * 255);
                            int green_line = (int)(green_color_line * 255);
                            int blue_line = (int)(blue_color_line * 255);
                            lock (_renderLock)
                            {
                                tempGraphics.DrawLine(new Pen(Color.FromArgb(red_line, green_line, blue_line)), x_line, y_line, x_end_line, y_end_line);
                            }
                            outputExpression.Append($"line {x_percent_line} {y_percent_line} {x_end_percent_line} {y_end_percent_line} {red_color_line} {green_color_line} {blue_color_line}\n");
                            /*lock (_renderQueueLock)
                            {
                                renderQueue.Add(new RenderObject(
                                    true,
                                    false,
                                    x_line,
                                    x_end_line,
                                    y_line,
                                    y_end_line,
                                    Color.FromArgb(red_line, green_line, blue_line),
                                    x_percent_line,
                                    x_end_percent_line,
                                    y_percent_line,
                                    y_end_percent_line,
                                    red_color_line,
                                    green_color_line,
                                    blue_color_line
                                ));
                            }*/
                            DebugWriter.WriteDebug("draw line");
                        }
                        catch
                        {
                            DebugWriter.WriteDebug("error when decoding line");
                        }
                        break;
                    case "epolyline":
                        outputExpression.Append(command[1..] + '\n');
                        try
                        {
                            polylineAsm.ReceiveData(command[1..]);
                        }
                        catch
                        {
                            DebugWriter.WriteDebug("EDITOR: not a valid polyline (epolyline single-packet)");
                        }
                        break;
                    case "epld":
                        outputExpression.Append(command[1..] + '\n');
                        try
                        {
                            polylineAsm.ReceiveData(command[1..]);
                        }
                        catch
                        {
                            DebugWriter.WriteDebug("EDITOR: not a valid polyline (epolyline single-packet)");
                        }
                        break;
                    case "ans":
                        if (!IsConnectionActive)
                        {
                            Task.Run(() => MessageBox.Show("Connection established", "Tsu-k Executive"));
                            IsConnectionActive = true;
                            _ = Task.Run(() =>
                            {
                                DebugWriter.WriteDebug("SENT BIINT!!");
                                Write("binit");
                            });  // binit = buffer initialize
                            if (login.Length != 0)
                            {
                                Task.Run(() => Write("l:" + login));
                            }
                            if (encryption)
                            {
                                Thread.Sleep(250);
                                Task.Run(() => Write("encryption"));
                            }
                            Thread.Sleep(250);
                            if (!encryption && password.Length != 0)
                            {
                                Task.Run(() => Write("k:" + password));
                            }
                        }
                        break;
                    case "encryption_ok":
                        actuallyEncrypting = true;
                        Task.Run(() => Interaction.MsgBox("Remote side accepted encryption. This means, that nobody could see this session's payload.", MsgBoxStyle.Information, "Tsu-k Interruptive Window"));
                        Task.Run(() => Write("k:" + password));
                        break;
                    case "encryption_error":
                        actuallyEncrypting = false;
                        Task.Run(() => Interaction.MsgBox("Remote side refused to use encryption. This session will be NOT encrypted and anybody could possibly see your drawing process!", MsgBoxStyle.Exclamation, "Tsu-k Interruptive Window"));
                        break;
                    case "auth_error_password":
                        Task.Run(() => Interaction.MsgBox("Failed to connect due to incorrect password", MsgBoxStyle.Critical, "Tsu-k Interruptive Window"));
                        break;
                    case "auth_error_login_unknown":
                        Task.Run(() => Interaction.MsgBox("Failed to connect due to unknown login", MsgBoxStyle.Critical, "Tsu-k Interruptive Window"));
                        break;
                }
            }
        }
        public void SetupTNet()
        {
            // measure time because whyn't?
            Stopwatch stopwatch = Stopwatch.StartNew();
            string[] config = File.ReadAllLines("tetronet.txt");
            // check config header
            if (config[0] != "tetronet")
            {
                TetronetCorruptedConfigMessage();
                return;
            }
            // parse modem setup data
            if (config[1] == "virtual")
            {
                DebugWriter.WriteDebug("Switching modes: tetronet will be using Virtual Modem to connect");
                Modem = new VirtualModem(config[2], new(), rawWs: config[3] == "websocket");
                CHUNK_SIZE = 8000;
            }
            else if (config[1] == "ciocil")
            {
                DebugWriter.WriteDebug("Switching modes: tetronet will be using Low Latency Physical Modem to connect");
                Modem = new LowLatencyPhysicalModem(config[2].Split(' ')[0], int.Parse(config[2].Split(' ')[1]), L1Types.L1_SERIAL);
                CHUNK_SIZE = 500;
            }
            else
            {
                TetronetCorruptedConfigMessage();
                return;
            }
            // connect the modem to the tetronet
            Modem.Dial();
            Stopwatch stopwatchConnection = Stopwatch.StartNew();
            while (!Modem.IsModemConnected || Modem.LocalModemAddress == null) { }
            File.WriteAllText("ci_address_editor.txt", Modem.LocalModemAddress.ToString());
            DebugWriter.WriteDebug($"Setup tetronet took {stopwatch.ElapsedMilliseconds}ms to complete and handshake took {(double)stopwatchConnection.ElapsedTicks / stopwatch.ElapsedTicks * 100}% of that time");
        }
        private static void TetronetCorruptedConfigMessage()
        {
            MessageBox.Show("Failed to connect to the Tetronet: tetronet.txt configuration file is invalid or damaged.", "Tsu-k Interruptive Window", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            instrument = SelectedInstrument.Triangle;
        }
    }
}
