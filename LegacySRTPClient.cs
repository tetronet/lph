using LPH_Edit_Viewer;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class LegacySRTPClient
{
    private IPEndPoint CommunicatingWith = null;
    private bool IsFirstReceivedPacket = false;
    private UdpClient InnerClient;
    private long LastReceived = -1;
    private long SequentialPacketNumber = 0;
    private bool IsClosed = false;
    private int TicksTimeout;
    private bool IsServer;
    private HashSet<long> ReceivedNumbers = new HashSet<long>();
    private ConcurrentDictionary<long, byte[]> ReorderingBuffer = new ConcurrentDictionary<long, byte[]>();
    private ConcurrentDictionary<long, ReliablePacketInformation> InternalState = new ConcurrentDictionary<long, ReliablePacketInformation>();

    public Action<byte[]> OnMessageReceived = delegate { };
    public Action<int, string> OnError = delegate { };
    public IPEndPoint RemoteEndPoint { get { return new IPEndPoint(CommunicatingWith.Address, CommunicatingWith.Port); } }
    public LegacySRTPClient(bool isServer, IPEndPoint dst, int tickTimeout)
    {
        IsServer = isServer;
        DebugWriter.WriteDebug($"[server? {isServer}]: address {dst}");
        if (isServer)
        {
            InnerClient = new UdpClient();
            InnerClient.Client.Bind(dst);
        }
        else
        {
            InnerClient = new UdpClient(0);
            InnerClient.Connect(dst);
        }
        IsFirstReceivedPacket = !isServer;
        CommunicatingWith = dst;
        TicksTimeout = tickTimeout;
        // receiver task
        Task.Run(delegate ()
        {
            while (!IsClosed)
            {
                try
                {
                    byte[] dataReceived = InnerClient.Receive(ref CommunicatingWith);
                    // check IP end point
                    if (!IsFirstReceivedPacket)
                    {
                        IsFirstReceivedPacket = true;
                    }
                    // parse received packet
                    if (dataReceived.Length < 8)
                    {
                        OnError(16, "Packet too short.");
                        continue;
                    }
                    long id = BinaryPrimitives.ReadInt64BigEndian(dataReceived);
                    if (dataReceived.Length == 8)
                    {
                        // == ack receiver ==
                        InternalState.TryRemove(id, out _);
                    }
                    if (dataReceived.Length > 8)
                    {
                        // == data receiver ==
                        if (id == LastReceived + 1)
                        {
                            UnivSend(dataReceived.AsSpan(0, 8).ToArray()); // send acknowledgement
                            if (!ReceivedNumbers.Contains(id))
                            {
                                OnMessageReceived(dataReceived.AsSpan(8).ToArray()); // call user events
                                Interlocked.Increment(ref LastReceived); // increment LastReceived so everything will work
                                ReceivedNumbers.Add(id);
                            }
                            int i = 1;
                            // check if next packets could be received from the buffer
                            while (ReorderingBuffer.TryRemove(id + i, out byte[] dataFromBuffer))
                            {
                                OnMessageReceived(dataFromBuffer); // call user events
                                Interlocked.Increment(ref LastReceived); // increment LastReceived so everything will work
                                ReceivedNumbers.Add(id + i);
                                i++;
                            }
                        }
                        // save to a temp buffer
                        else
                        {
                            ReorderingBuffer.TryAdd(id, dataReceived.AsSpan(8).ToArray());
                            UnivSend(dataReceived.AsSpan(0, 8).ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    OnError(-1, "Exception: " + e.ToString());
                }
            }
        });
        // error correction task
        Task.Run(async delegate ()
        {
            while (!IsClosed)
            {
                foreach (ReliablePacketInformation info in InternalState.Values)
                {
                    if (info.CheckTimeout(TicksTimeout))
                    {
                        // retransmit packets
                        UnivSend(info.Data);
                        // update timeout
                        info.UpdateTimeStamp();
                        DebugWriter.WriteDebug($"retransmit id: {info.Id} total: {InternalState.Count}");
                    }
                }
                await Task.Delay(1);
            }
        });
    }
    public void Transmit(byte[] data)
    {
        byte[] data_ = new byte[data.Length + 8];
        BinaryPrimitives.WriteInt64BigEndian(data_.AsSpan(), SequentialPacketNumber);
        data.CopyTo(data_, 8);
        InternalState.TryAdd(SequentialPacketNumber, new ReliablePacketInformation(SequentialPacketNumber, DateTime.Now.Ticks, data_));
        Interlocked.Increment(ref SequentialPacketNumber);
        UnivSend(data_);
    }
    public void Close()
    {
        IsClosed = true;
        InnerClient.Close();
        InternalState.Clear();

    }
    private void UnivSend(byte[] data)
    {
        if (IsServer)
        {
            InnerClient.Send(data, data.Length, CommunicatingWith);
        }
        else
        {
            InnerClient.Send(data, data.Length);
        }
    }
    private static bool CheckIPEndPointEquality(IPEndPoint a, IPEndPoint b)
    {
        return a.Address.Equals(b.Address) && a.Port.Equals(b.Port);
    }
}