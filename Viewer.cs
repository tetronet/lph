using Microsoft.VisualBasic;
using ModemAPI;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
    public partial class Viewer : Form
    {
        private string filename;
        private int objectsRendered = 0;
        private int totalObjectCount = 0;
        private int lastObjectCount = 0;
        private float objectRate = 0;
        private DiskStringBuilder lphData = new($"./temp/viewer-{Guid.NewGuid()}.tmp");
        private Graphics graphics;
        private Graphics tempGraphics;
        private Bitmap pictureOnScreen;
        //private UdpClient serialPort;
        private IPEndPoint communicatingWith;
        private string AcceptOnlyFromLogin = "";
        private string AcceptOnlyFromPassword = "";
        private bool IsConnectionActive = false;
        private bool IsBufferReady = false;
        private bool isCursorPressing = false;
        private bool WasPasswordOk = false;
        private bool WasLoginOk = false;
        private bool EncryptingDatagrams = false;
        private bool AllowEncryption = false;
        private int mouseButton = 0;
        private bool alive = true;
        private ulong CurrentPacketNo = 0;
        private ConcurrentDictionary<ulong, byte[]> PendingForAcknowledgementData = new ConcurrentDictionary<ulong, byte[]>();
        private ConcurrentDictionary<ulong, long> PendingForAcknowledgement = new ConcurrentDictionary<ulong, long>();
        private HashSet<ulong> AlreadyReceived = new HashSet<ulong>();
        private const int TIMEOUT_MS = 2000;
        private RandomNumberGenerator rng = RandomNumberGenerator.Create();
        private int pps = 500;
        private bool isReceivingPolyline = false;
        private StringBuilder polylineRxBuffer = new StringBuilder();
        private object transmissionLock = new object();
        private ConcurrentDictionary<ulong, byte[]> ReorderingBuffer = new ConcurrentDictionary<ulong, byte[]>();
        private TcpListener tcpListener;
        private TcpClient? serialPort;
        private PolylineAssembler polylineAssembler = new PolylineAssembler();
        private List<byte> receiveBuffer = new List<byte>();
        private readonly Lock _receiveBufferLock = new();
        private bool usingTetronet = false;
        private IModem? Modem;
        private SRTPClient? ReliableTetronetClient;
        private const int CHUNK_SIZE = 8000;
        private int RetransmittedPacketCount = 0;
        private byte[] AesKey = new byte[32];

        public Viewer(string filename)
        {
            InitializeComponent();
            this.filename = filename;
        }
        public Viewer(DiskStringBuilder data)
        {
            InitializeComponent();
            lphData = data;
        }

        private void Viewer_Load(object sender, EventArgs e)
        {
            panel1.Paint += new PaintEventHandler(panel1_Paint);
            panel1.Refresh();
            pictureOnScreen = new Bitmap(panel1.Width, panel1.Height);
            tempGraphics = Graphics.FromImage(pictureOnScreen);
            graphics = panel1.CreateGraphics();
            if (filename != "")
            {
                lphData.Init(filename);
            }
            else
            {
                lphData.Init();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serialPort?.Close();
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Edit(lphData.ToString(), false).Show();
        }

        private void panel1_Paint(object? sender, PaintEventArgs? e)
        {

        }

        private void ForceRedraw(DiskStringBuilder toParse, bool deleteBeforeRedrawing)
        {
            //string[] shapes = toParse.Split('\n');
            if (deleteBeforeRedrawing)
            {
                try
                {
                    objectsRendered = 0;
                    graphics.Clear(Color.White);
                    tempGraphics.Clear(Color.White);
                }
                catch (Exception e)
                {
                    DebugWriter.ReceiveException("ForceRedraw(DiskStringBuilder toParse, bool deleteBeforeRedrawing)", e);
                }
                //totalObjectCount = shapes.Length;
            }
            else
            {
                //totalObjectCount += shapes.Length;
            }
            long idx = 0;
            while (idx < toParse.GetTotalLength())
            {
                idx = toParse.ReadBefore('\n', idx, out string shape);
                try
                {
                    string[] shapeData = shape.Split(' ');
                    switch (shapeData[0])
                    {
                        case "point":
                            decimal x_percent_point = decimal.Parse(shapeData[1]);
                            decimal y_percent_point = decimal.Parse(shapeData[2]);
                            decimal r_float_point;
                            decimal g_float_point;
                            decimal b_float_point;
                            if (shapeData.Length == 3)
                            {
                                r_float_point = 0;
                                g_float_point = 0;
                                b_float_point = 1;
                            }
                            else
                            {
                                r_float_point = decimal.Parse(shapeData[3] ?? "0");
                                g_float_point = decimal.Parse(shapeData[4] ?? "0");
                                b_float_point = decimal.Parse(shapeData[5] ?? "1");
                            }

                            int x_point = (int)(panel1.Width * x_percent_point / 100);
                            int y_point = (int)(panel1.Height * y_percent_point / 100);
                            int r_point = (int)(r_float_point * 255);
                            int g_point = (int)(g_float_point * 255);
                            int b_point = (int)(b_float_point * 255);
                            tempGraphics.FillEllipse(new SolidBrush(Color.FromArgb(r_point, g_point, b_point)), x_point - 2, y_point - 2, 4, 4);
                            break;
                        case "line":
                            decimal x_percent_line = decimal.Parse(shapeData[1]);
                            decimal y_percent_line = decimal.Parse(shapeData[2]);
                            decimal x_end_percent_line = decimal.Parse(shapeData[3]);
                            decimal y_end_percent_line = decimal.Parse(shapeData[4]);
                            decimal r_float_line;
                            decimal g_float_line;
                            decimal b_float_line;
                            if (shapeData.Length == 5)
                            {
                                r_float_line = 0;
                                g_float_line = 0;
                                b_float_line = 1;
                            }
                            else
                            {
                                r_float_line = decimal.Parse(shapeData[5] ?? "0");
                                g_float_line = decimal.Parse(shapeData[6] ?? "0");
                                b_float_line = decimal.Parse(shapeData[7] ?? "1");
                            }
                            int x_line = (int)(panel1.Width * x_percent_line / 100);
                            int y_line = (int)(panel1.Height * y_percent_line / 100);
                            int x_end_line = (int)(panel1.Width * x_end_percent_line / 100);
                            int y_end_line = (int)(panel1.Height * y_end_percent_line / 100);
                            int r_line = (int)(r_float_line * 255);
                            int g_line = (int)(g_float_line * 255);
                            int b_line = (int)(b_float_line * 255);
                            tempGraphics.DrawLine(new Pen(Color.FromArgb(r_line, g_line, b_line)), x_line, y_line, x_end_line, y_end_line);
                            break;
                        case "polyline":
                        case "pld":
                            try
                            {
                                /*List<PointM> path = new List<PointM>();
                                string[] posdata = shapeData.Skip(4).ToArray();
                                for (int i = 0; i < posdata.Length - 2; i += 2)
                                {
                                    path.Add(PointM.FromStrings(posdata[i], posdata[i + 1]));
                                }
                                List<Point> pathInt = new List<Point>();
                                for (int i = 0; i < path.Count; i++)
                                {
                                    pathInt.Add(path[i].ToPoint(panel1.Width, panel1.Height));
                                }
                                Color polylineColor = Color.FromArgb((int)(decimal.Parse(shapeData[1]) * 255), (int)(decimal.Parse(shapeData[2]) * 255), (int)(decimal.Parse(shapeData[3]) * 255));
                                tempGraphics.DrawLines(new Pen(polylineColor), pathInt.ToArray());*/
                                polylineAssembler.ReceiveData(shape);
                                if (polylineAssembler.Available)
                                {
                                    DebugWriter.WriteDebug("new polyline found in the buffer, bb " + panel1.Bounds.ToString());
                                    if (!polylineAssembler.AvailablePolyline.Draw(tempGraphics, panel1.Bounds))
                                    {
                                        throw new Exception();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                DebugWriter.WriteDebug($"error when decoding polyline: {e}");
                                DebugWriter.WriteDebug($"failed to decode this polyline: {shape}");
                            }
                            break;
                    }
                    objectsRendered++;
                }
                catch
                {
                    DebugWriter.WriteDebug($"skip invalid object: {shape}");
                }
            }
        }

        private void ForceRedraw(string toParse, bool deleteBeforeRedrawing)
        {
            string[] shapes = toParse.Split('\n');
            if (deleteBeforeRedrawing)
            {
                objectsRendered = 0;
                graphics.Clear(Color.White);
                tempGraphics.Clear(Color.White);
                totalObjectCount = shapes.Length;
            }
            else
            {
                totalObjectCount += shapes.Length;
            }
            foreach (string shape in shapes)
            {
                try
                {
                    string[] shapeData = shape.Split(' ');
                    switch (shapeData[0])
                    {
                        case "point":
                            decimal x_percent_point = decimal.Parse(shapeData[1]);
                            decimal y_percent_point = decimal.Parse(shapeData[2]);
                            decimal r_float_point;
                            decimal g_float_point;
                            decimal b_float_point;
                            if (shapeData.Length == 3)
                            {
                                r_float_point = 0;
                                g_float_point = 0;
                                b_float_point = 1;
                            }
                            else
                            {
                                r_float_point = decimal.Parse(shapeData[3] ?? "0");
                                g_float_point = decimal.Parse(shapeData[4] ?? "0");
                                b_float_point = decimal.Parse(shapeData[5] ?? "1");
                            }

                            int x_point = (int)(panel1.Width * x_percent_point / 100);
                            int y_point = (int)(panel1.Height * y_percent_point / 100);
                            int r_point = (int)(r_float_point * 255);
                            int g_point = (int)(g_float_point * 255);
                            int b_point = (int)(b_float_point * 255);
                            tempGraphics.FillEllipse(new SolidBrush(Color.FromArgb(r_point, g_point, b_point)), x_point - 2, y_point - 2, 4, 4);
                            break;
                        case "line":
                            decimal x_percent_line = decimal.Parse(shapeData[1]);
                            decimal y_percent_line = decimal.Parse(shapeData[2]);
                            decimal x_end_percent_line = decimal.Parse(shapeData[3]);
                            decimal y_end_percent_line = decimal.Parse(shapeData[4]);
                            decimal r_float_line;
                            decimal g_float_line;
                            decimal b_float_line;
                            if (shapeData.Length == 5)
                            {
                                r_float_line = 0;
                                g_float_line = 0;
                                b_float_line = 1;
                            }
                            else
                            {
                                r_float_line = decimal.Parse(shapeData[5] ?? "0");
                                g_float_line = decimal.Parse(shapeData[6] ?? "0");
                                b_float_line = decimal.Parse(shapeData[7] ?? "1");
                            }
                            int x_line = (int)(panel1.Width * x_percent_line / 100);
                            int y_line = (int)(panel1.Height * y_percent_line / 100);
                            int x_end_line = (int)(panel1.Width * x_end_percent_line / 100);
                            int y_end_line = (int)(panel1.Height * y_end_percent_line / 100);
                            int r_line = (int)(r_float_line * 255);
                            int g_line = (int)(g_float_line * 255);
                            int b_line = (int)(b_float_line * 255);
                            tempGraphics.DrawLine(new Pen(Color.FromArgb(r_line, g_line, b_line)), x_line, y_line, x_end_line, y_end_line);
                            break;
                        case "polyline":
                        case "pld":
                            try
                            {
                                /*List<PointM> path = new List<PointM>();
                                string[] posdata = shapeData.Skip(4).ToArray();
                                for (int i = 0; i < posdata.Length - 2; i += 2)
                                {
                                    path.Add(PointM.FromStrings(posdata[i], posdata[i + 1]));
                                }
                                List<Point> pathInt = new List<Point>();
                                for (int i = 0; i < path.Count; i++)
                                {
                                    pathInt.Add(path[i].ToPoint(panel1.Width, panel1.Height));
                                }
                                Color polylineColor = Color.FromArgb((int)(decimal.Parse(shapeData[1]) * 255), (int)(decimal.Parse(shapeData[2]) * 255), (int)(decimal.Parse(shapeData[3]) * 255));
                                tempGraphics.DrawLines(new Pen(polylineColor), pathInt.ToArray());*/
                                polylineAssembler.ReceiveData(shape);
                                if (polylineAssembler.Available)
                                {
                                    polylineAssembler.AvailablePolyline.Draw(tempGraphics, panel1.Bounds);
                                }
                            }
                            catch (Exception e)
                            {
                                DebugWriter.WriteDebug($"error when decoding polyline: {e}");
                                DebugWriter.WriteDebug($"failed to decode this polyline: {shape}");
                            }
                            break;
                    }
                    objectsRendered++;
                }
                catch
                {
                    DebugWriter.WriteDebug($"skip invalid object: {shape}");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SerialPortOptions opt = new();
            opt.Show();
            opt.OnSubmit(async delegate (SerialPortOptions options)
            {
                if (options.UseNetwork)
                {
                    try
                    {
                        if (options.TetronetAddress)
                        {
                            usingTetronet = true;
                            //_ = Task.Run(async delegate ()
                            {
                                SetupTNet();
                                if (options.SerialPortName == "")
                                {
                                    ReliableTetronetClient = await WaitForSrtpConnection("lphrp", 125000000);
                                    ReliableTetronetClient.TicksPacketDelay = (int)(10000000d / options.DataBitrate);
                                    ParseClientDatagrams("cnt", null);
                                    DebugWriter.WriteDebug("test");
                                }
                                else
                                {
                                    if (Modem == null)
                                    {
                                        return;
                                    }
                                    ReliableTetronetClient = new(Modem, new(options.SerialPortName), "lphrp", 125000000, TIMEOUT_MS * 10000);
                                    ReliableTetronetClient.TicksPacketDelay = (int)(10000000d / options.DataBitrate);
                                }
                                ReliableTetronetClient.OnRetransmit += delegate (long seqno)
                                {
                                    RetransmittedPacketCount++;
                                    Debug.WriteLineIf(RetransmittedPacketCount % 100 == 0, $"Retransmitted {RetransmittedPacketCount} packets");
                                };
                            }
                        }
                        else
                        {
                            string ips = options.SerialPortName.Split(':')[0];
                            string ports = options.SerialPortName.Split(':')[1];
                            communicatingWith = new IPEndPoint(ips == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(ips), int.Parse(ports));
                            if (ips == "0.0.0.0")
                            {
                                tcpListener = new TcpListener(communicatingWith);
                                tcpListener.Start(1);
                                while (!tcpListener.Pending())
                                {
                                    await Task.Delay(100);
                                }
                                serialPort = tcpListener.AcceptTcpClient();
                                DebugWriter.WriteDebug("viewer is server");
                            }
                            else
                            {
                                serialPort = new TcpClient();
                                serialPort.Connect(communicatingWith);
                                _ = Task.Run(() => Write("wfi")); // wfi = wait for incoming
                                DebugWriter.WriteDebug("viewer is client");
                            }
                        }
                        AcceptOnlyFromLogin = options.Login;
                        AcceptOnlyFromPassword = options.Password;
                        AesKey = [.. SHA512.HashData(Encoding.UTF8.GetBytes(AcceptOnlyFromPassword)).Take(32)];
                        AllowEncryption = options.UsingEncryption;
                        pps = options.DataBitrate;
                        if (!options.AuthReqired)
                        {
                            WasLoginOk = true;
                            WasPasswordOk = true;
                        }
                        WaitForShapesFromPort();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.GetType() + ": " + ex.Message, "Tsu-k Executive");
                    }
                }
                else
                {
                    MessageBox.Show("You did not setup the network, so this host is not going to be able to receive data from the network", "Tsu-k Executive");
                }
            });
        }
        private void WaitForShapesFromPort()
        {
            //serialPort.Client.Bind(communicatingWith);
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
                        ulong[] keys = ReorderingBuffer.Keys.ToArray();
                        Array.Sort(keys);
                        long lastKey = (long)keys[0] - 1;
                        foreach (ulong key in keys)
                        {
                            if (lastKey + 1 != (long)key)
                            {
                                packetsReordered = false;
                                Console.WriteLine("editor: reorder packets failed due to not all sequence collected");
                                break;
                            }
                        }
                        if (packetsReordered)
                        {
                            Dictionary<ulong, byte[]> reordBufferCopy = new Dictionary<ulong, byte[]>(ReorderingBuffer);
                            foreach (byte[] data in reordBufferCopy.Values)
                            {
                                // parse reordered server data grams
                                ParseClientDatagrams(Encoding.UTF8.GetString(data), communicatingWith);
                            }
                        }
                    }
                }
            });
            Task.Run(() =>
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
                                if (!PendingForAcknowledgementData.ContainsKey(packNoAndTime.Key) || PendingForAcknowledgementData[packNoAndTime.Key] == null)
                                {
                                    Console.WriteLine("viewer.cs: this packet id is null or unexist for Pending For Ack Data dict, deleting from Pending For Ack dict...");
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
                                serialPort.Send(toSend, toSend.Length, communicatingWith);
                                // change time to now to prevent sending 41544465454545156 packets per second
                                PendingForAcknowledgement[packNoAndTime.Key] = DateTime.Now.Ticks;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"viewer - error while handling timeouts: {e}");
                    }
                }
            });*/
            void OnMessageReceived(byte[]? onWire)
            {
                if (onWire == null)
                {
                    return;
                }
                try
                {
                    DebugWriter.WriteDebug("------------");
                    IPEndPoint? sentfrom = null;
                    if (serialPort != null)
                    {
                        sentfrom = (IPEndPoint?)serialPort.Client.RemoteEndPoint;
                    }
                    string dataReceived = "";
                    try
                    {
                        if (!EncryptingDatagrams)
                        {
                            dataReceived = Encoding.UTF8.GetString(onWire.ToArray());
                        }
                        else
                        {
                            byte[] receivedEncrypted = [.. onWire];
                            if (receivedEncrypted.Length < 16 && EncryptingDatagrams)
                            {
                                DebugWriter.WriteDebug("[ERROR]: Packet too short, discarding");
                                return;
                            }
                            DebugWriter.WriteDebug("data on wire: " + string.Join(",", receivedEncrypted));
                            dataReceived = Aes256Helper.Decrypt(receivedEncrypted, AesKey);
                        }
                    }
                    catch (Exception e)
                    {
                        DebugWriter.WriteDebug($"error viewer recv: {e}");
                    }
                    DebugWriter.WriteDebug("viewer recv (" + dataReceived.Length + "): " + dataReceived);

                    ParseClientDatagrams(dataReceived, sentfrom);
                }
                catch (Exception e)
                {
                    DebugWriter.WriteDebug($"(probably reading datagram from client, this is viewer) exception = {e}");
                }
            };
            // ======= ПОТОК ЧТЕНИЯ (только читаем) =======
            _ = Task.Run(async delegate ()
            {
                if (!usingTetronet)
                {
                    if (serialPort == null)
                    {
                        return;
                    }
                    var stream = serialPort.GetStream(); // предполагаю, что это NetworkStream
                    byte[] buffer = new byte[8192]; // буфер для чтения

                    while (true)
                    {
                        if (serialPort == null)
                        {
                            break;
                        }
                        if (!serialPort.Connected)
                        {
                            break;
                        }
                        try
                        {
                            // Читаем БЛОКОМ, а не по Available
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break; // сокет закрыт

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
                        return;
                    }
                    ReliableTetronetClient.OnMessageReceived += delegate (SRTPClient sender, byte[] data)
                    {
                        lock (_receiveBufferLock)
                        {
                            receiveBuffer.AddRange(data);
                        }
                        DebugWriter.WriteDebug($"Viewer received {data.Length} bytes from the SRTP tetronet client");
                    };
                }
            });

            // ======= ПОТОК ОБРАБОТКИ (разбираем пакеты) =======
            _ = Task.Run(delegate ()
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
                            if (packetLength > 10000)
                            {
                                Interaction.MsgBox($"Length is bigger than 10000 bytes... Packet says {packetLength} bytes.",
                                                   MsgBoxStyle.OkOnly, "Tsu-k Interruptive Window");
                            }
                            else
                            {
                                OnMessageReceived(onWire);
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

        private void button4_Click(object sender, EventArgs e)
        {
            Task.Run(delegate ()
            {
                try
                {
                    ForceRedraw(lphData, true);
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox($"Error: {ex}", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            });
        }
        private long lastCursorPacket = 0;
        private Point lastCursorPos = new();


        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((DateTime.Now.Ticks - lastCursorPacket) > 200000 && !lastCursorPos.Equals(e.Location) && IsConnectionActive && WasLoginOk && WasPasswordOk)
            {
                lastCursorPacket = DateTime.Now.Ticks;
                int red = isCursorPressing ? 1 : 0;
                DebugWriter.WriteDebug("curpos was sent to the port: " + $"curpos {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {red} 0 0 255 {mouseButton}\r\n");
                Task.Run(() => Write($"curpos {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {red} 0 0 255 {mouseButton}\r\n")); // curpos = cursor position `curpos <x> <y> <r> <g> <b> <cursor_id> <mouse_button>`
                lastCursorPos = e.Location;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsConnectionActive && WasLoginOk && WasPasswordOk)
            {
                isCursorPressing = true;
                switch (e.Button)
                {
                    case MouseButtons.Left:
                        mouseButton = 1;
                        break;
                    case MouseButtons.Right:
                        mouseButton = 2;
                        break;
                    case MouseButtons.Middle:
                        mouseButton = -1;
                        break;
                    case MouseButtons.XButton1:
                        mouseButton = 255;
                        break;
                    case MouseButtons.XButton2:
                        mouseButton = 254;
                        break;
                }
                int red = isCursorPressing ? 1 : 0;
                Task.Run(() => Write($"curpos {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {red} 0 0 255 {mouseButton}\r\n")); // curpos = cursor position `curpos <x> <y> <r> <g> <b> <cursor_id> <mouse_button>`
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isCursorPressing || !IsConnectionActive || !WasLoginOk || !WasPasswordOk)
            {
                return;
            }
            isCursorPressing = false;
            mouseButton = 0;
            int red = isCursorPressing ? 1 : 0;
            Task.Run(() => Write($"curpos {(decimal)e.X / panel1.Width * 100} {(decimal)e.Y / panel1.Height * 100} {red} 0 0 255 {mouseButton}\r\n")); // curpos = cursor position `curpos <x> <y> <r> <g> <b> <cursor_id> <mouse_button>`
        }

        private void Viewer_Shown(object sender, EventArgs e)
        {
            Task.Run(delegate ()
            {
                try
                {
                    ForceRedraw(lphData, true);
                }
                catch (Exception ex)
                {
                    Interaction.MsgBox($"Error: {ex}", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                }
            });
        }

        private void RenderedObjectCountUpdater_Tick(object sender, EventArgs e)
        {
            label1.Text = $"Objects Rendered: {objectsRendered} (rate: {(float.IsInfinity(objectRate) ? 0 : objectRate):f1} O/s)";
            progressBar1.Maximum = totalObjectCount;
            if (objectsRendered < totalObjectCount && objectsRendered < progressBar1.Maximum)
            {
                progressBar1.Value = objectsRendered;
            }
        }

        private void ObjectRateUpdater_Tick(object sender, EventArgs e)
        {
            objectRate = objectsRendered - lastObjectCount;
            if (objectRate < 0)
            {
                objectRate = 0;
            }
            lastObjectCount = objectsRendered;
        }

        long lastpkttx = 0;
        private void Write(string data)
        {
            lock (transmissionLock)
            {
                try
                {
                    while (DateTime.Now.Ticks - lastpkttx < 10000000 / pps) { }
                    if (!EncryptingDatagrams)
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
                        byte[] dataToSend = new byte[encrypted.Length + 4];
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

        private void Viewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            alive = false;
            try
            {
                serialPort?.Close();
                lphData?.Dispose();
            }
            catch
            {

            }
        }

        private void ScreenUpdater_Tick(object sender, EventArgs e)
        {
            try
            {
                graphics.DrawImageUnscaled(pictureOnScreen, 0, 0);
            }
            catch
            {

            }
        }

        private void ChangeWindowTitle(string newTitle)
        {
            Invoke(delegate () { Text = newTitle; });
        }

        private void button5_Click(object sender, EventArgs e)
        {
            new TsukTerminal(lphData.ToString()).Show();
        }
        private void ParseClientDatagrams(string dataReceived, IPEndPoint? sentfrom)
        {
            switch (dataReceived.Trim())
            {
                case "":
                    break;
                case "clr":
                    if (!IsConnectionActive || !WasLoginOk || !WasPasswordOk || !IsBufferReady)
                    {
                        break;
                    }
                    lphData.Clear();
                    ForceRedraw(lphData, true);
                    break;
                case "cnc_lst":
                    if (!IsConnectionActive || !WasLoginOk || !WasPasswordOk || !IsBufferReady)
                    {
                        break;
                    }
                    graphics.Clear(Color.White);
                    tempGraphics.Clear(Color.White);
                    lphData.TruncateFromEndBefore('\n', out _);
                    Task.Run(delegate () { ForceRedraw(lphData, true); });
                    break;
                case "cnt":
                    IsConnectionActive = true;
                    DebugWriter.WriteDebug("connected");
                    if (ReliableTetronetClient == null)
                    {
                        DebugWriter.WriteDebug("NULL HERE!!!!!!!!!!!!!!!");
                    }
                    Task.Run(() =>
                    {
                        Write("ans\r\n");
                    }); // ans = answer
                    if (sentfrom == null)
                    {
                        break;
                    }
                    communicatingWith = sentfrom;
                    Task.Run(() => Interaction.MsgBox($"Received new LPH connection from {sentfrom.Address.MapToIPv4()}", MsgBoxStyle.Information, "Tsu-k Interruptive Window"));
                    break;
                case "binit":
                    DebugWriter.WriteDebug("VIEWER - BUFFER INIT");
                    IsBufferReady = true;
                    break;
                case "ping":
                    if (!IsConnectionActive && !WasLoginOk && !WasPasswordOk)
                    {
                        break;
                    }
                    DebugWriter.WriteDebug("received a ping request: responding...");
                    Task.Run(() => Write("pong\r\n"));
                    break;
                case "bufget":
                    DebugWriter.WriteDebug("Connection Active: " + IsConnectionActive);
                    DebugWriter.WriteDebug("Buffer Ready: " + IsBufferReady);
                    DebugWriter.WriteDebug("Login OK: " + IsBufferReady);
                    DebugWriter.WriteDebug("Password OK: " + WasPasswordOk);
                    if (!IsConnectionActive || !IsBufferReady || !WasLoginOk || !WasPasswordOk)
                    {
                        DebugWriter.WriteDebug("refused to transmit local viewer's buffer");
                        break;
                    }
                    DebugWriter.WriteDebug("client asked for the buffer: responding...");
                    DebugWriter.WriteDebug($"buffer size is going to be {lphData.GetTotalLength()} chars");
                    Task.Run(delegate ()
                    {
                        try
                        {
                            Stopwatch stopwatch2 = Stopwatch.StartNew();
                            int objectCount = lphData.CountChars('\n');
                            Console.WriteLine($"Viewer: It took {stopwatch2.ElapsedMilliseconds}ms to count objects in the buffer.");
                            Write($"bufsize {objectCount}");
                            Stopwatch stopwatch = Stopwatch.StartNew();
                            Stopwatch stopwatch1 = Stopwatch.StartNew();
                            long totalTicks = 0;
                            long idx = 0;
                            bool didntfoundEnd = true;
                            while (didntfoundEnd)
                            {
                                StringBuilder sb = new();
                                // batched reading
                                for (int i = 0; i < 1024; i++)
                                {
                                    idx = lphData.ReadBefore('\n', idx, out string lineObject);
                                    if (lineObject.Length < 2)
                                    {
                                        didntfoundEnd = false;
                                        Debug.WriteLine("found the end");
                                        break;
                                    }
                                    sb.Append('e').Append(lineObject).Append('\n');
                                }
                                stopwatch1.Restart();
                                Write(sb.ToString());
                                totalTicks += stopwatch1.ElapsedTicks;
                            }
                            Write("bufend");
                            DebugWriter.WriteDebug($"viewer sent buffer data in {stopwatch.ElapsedTicks * 100}ns, networking took {totalTicks / (double)stopwatch.ElapsedTicks * 100}% of that time");
                        }
                        catch (OutOfMemoryException)
                        {
                            Interaction.MsgBox("Out of memory!", MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
                        }
                    });
                    break;
                case "encryption":
                    if (IsConnectionActive)
                    {
                        if (!EncryptingDatagrams)
                        {

                            if (AllowEncryption)
                            {
                                Task.Run(() => Interaction.MsgBox("This LPH session is securely encrypted using AES-256 cipher, so nobody will ever see your drawing", MsgBoxStyle.Information, "Tsu-k Interruptive Window"));
                                Task.Run(() => Write("encryption_ok"));
                                Thread.Sleep(100);
                                EncryptingDatagrams = true;
                            }
                            else
                            {
                                Task.Run(() => Write("encryption_error"));
                            }
                        }
                    }
                    break;
                case "session_reset":
                    if (IsConnectionActive)
                    {
                        IsConnectionActive = false;
                        IsBufferReady = false;
                        if (AcceptOnlyFromLogin != "" || AcceptOnlyFromPassword != "")
                        {
                            WasPasswordOk = false;
                            WasLoginOk = false;
                        }
                        Task.Run(() => Write("session_reset_acknowledged"));
                        Task.Run(() => Interaction.MsgBox("Current session was terminated.", MsgBoxStyle.Critical, "Tsu-k Interruptive Window"));
                        serialPort?.Close();
                    }
                    break;
                default:
                    if (IsConnectionActive)
                    {
                        if (dataReceived[0] == 'l' && dataReceived[1] == ':')
                        {
                            string loginFromEditor = dataReceived.Substring(2);
                            DebugWriter.WriteDebug($"editor sent login: {loginFromEditor}");
                            if (AcceptOnlyFromLogin == loginFromEditor)
                            {
                                WasLoginOk = true;
                            }
                            else
                            {
                                Task.Run(() => Write("auth_error_login_unknown"));
                            }
                            break;
                        }
                        if (dataReceived[0] == 'k' && dataReceived[1] == ':')
                        {
                            string passwordFromEditor = dataReceived.Substring(2);
                            DebugWriter.WriteDebug($"editor sent password: {passwordFromEditor}");
                            if (AcceptOnlyFromPassword == passwordFromEditor)
                            {
                                WasPasswordOk = true;
                            }
                            else
                            {
                                Task.Run(() => Write("auth_error_password"));
                            }
                            break;
                        }
                    }
                    DebugWriter.WriteDebug("Connection Active: " + IsConnectionActive);
                    DebugWriter.WriteDebug("Buffer Ready: " + IsBufferReady);
                    DebugWriter.WriteDebug("Login OK: " + IsBufferReady);
                    DebugWriter.WriteDebug("Password OK: " + WasPasswordOk);
                    DebugWriter.WriteDebug("Packet Length: " + dataReceived.Length);
                    if (IsConnectionActive && IsBufferReady && IsBufferReady && WasPasswordOk && dataReceived.Length > 3)
                    {
                        lphData.Append(dataReceived);
                        if (dataReceived.StartsWith("pld") || dataReceived.StartsWith("polyline"))
                        {
                            polylineAssembler.ReceiveData(dataReceived);
                        }
                        /*if (!isReceivingPolyline)
                        {
                            if (dataReceived.StartsWith("polyline") && dataReceived.EndsWith("\n"))
                            {
                                ForceRedraw(dataReceived, false);
                                Console.WriteLine("EVENT VIEWER: received a single-packet sized polyline");
                            }
                            else if (dataReceived.StartsWith("polyline"))
                            {
                                isReceivingPolyline = true;
                                Console.WriteLine("EVENT VIEWER: receiving a polyline");
                                polylineRxBuffer.Append(dataReceived);
                            }
                            else
                            {
                                ForceRedraw(dataReceived, false);
                            }
                        }
                        else
                        {
                            polylineRxBuffer.Append(dataReceived);
                            if (dataReceived.EndsWith("\n"))
                            {
                                ForceRedraw(polylineRxBuffer.ToString(), false);
                                polylineRxBuffer.Clear();
                                isReceivingPolyline = false;
                                Console.WriteLine("EVENT VIEWER: fully received a polyline");
                                Console.WriteLine(polylineRxBuffer.ToString());
                            }
                        }*/
                    }
                    break;
            }
        }
        public void SetupTNet()
        {
            // measure time because whyn't?
            Stopwatch stopwatch = Stopwatch.StartNew();
            string[] config = File.ReadAllLines("tetronet.txt");
            Address wantedAddress = new();
            // check if we could load our local address from a file
            if (File.Exists("ci_address_viewer.txt"))
            {
                wantedAddress = new Address(File.ReadAllText("ci_address_viewer.txt"));
            }
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
                Modem = new VirtualModem(config[2], new(), rawWs:config[3] == "websocket");
            }
            else if (config[1] == "ciocil")
            {
                DebugWriter.WriteDebug("Switching modes: tetronet will be using Low Latency Physical Modem to connect");
                Modem = new LowLatencyPhysicalModem(config[2].Split(' ')[0], int.Parse(config[2].Split(' ')[1]), L1Types.L1_SERIAL, true);
            }
            else
            {
                TetronetCorruptedConfigMessage();
                return;
            }
            // connect the modem to the tetronet
            Modem.Dial();
            Stopwatch stopwatchConnection = Stopwatch.StartNew();
            while (!Modem.IsModemConnected) { }
            Task.Run(delegate () { MessageBox.Show("Your Tetronet address is " + Modem.LocalModemAddress, "Tsu-k Interruptive Window", MessageBoxButtons.OK, MessageBoxIcon.Information); });
            ChangeWindowTitle($"LPH Viewer ({Modem.LocalModemAddress})");
            DebugWriter.WriteDebug($"Setup tetronet (with address: {Modem.LocalModemAddress}) took {stopwatch.ElapsedMilliseconds}ms to complete and handshake took {(double)stopwatchConnection.ElapsedTicks / stopwatch.ElapsedTicks * 100}% of that time");
        }
        public async Task<SRTPClient> WaitForSrtpConnection(string qt, uint cid)
        {
            if (Modem == null || !Modem.IsModemConnected) { throw new NullReferenceException("can't init SRTP: modem null"); }
            SRTPClient? result = null;
            Modem.AttachReceiveEventNoUnfragment(delegate (Packet p, Action k)
            {
                if (p.QueryType == qt && p.ConnectionID == cid && Encoding.UTF8.GetString([.. p.DataBytes]).Trim() == "cnt")
                {
                    if (ReliableTetronetClient != null)
                    {
                        return;
                    }
                    result = new(Modem, p.Transmitter, qt, cid, TIMEOUT_MS * 10000);
                    Task.Run(() => Interaction.MsgBox($"Received new LPH connection from {p.Transmitter}", MsgBoxStyle.Information, "Tsu-k Interruptive Window"));
                }
            });
            while (result == null) { await Task.Delay(100); }
            return result;
        }
        private static void TetronetCorruptedConfigMessage()
        {
            MessageBox.Show("Failed to connect to the Tetronet: tetronet.txt configuration file is invalid or damaged.", "Tsu-k Interruptive Window", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                lphData.Flush(saveFileDialog1.FileName);
            }
        }
    }
}
