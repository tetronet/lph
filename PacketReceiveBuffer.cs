using System;
using System.Buffers.Binary;
using System.Threading;

namespace LPH_Edit_Viewer
{
    /// <summary>
    /// Receive buffer for the length-prefixed LPH stream ([int32 big endian length][payload]).
    /// Received bytes are kept in one array with a read offset, so taking a packet doesn't move the whole backlog.
    /// The writer is blocked while enough data is waiting to be processed, so a slow renderer slows the sender down
    /// (TCP flow control / SRTP stops acknowledging) instead of eating all the memory.
    /// </summary>
    public class PacketReceiveBuffer
    {
        private byte[] buffer = new byte[64 * 1024];
        private int readPos = 0;
        private int writePos = 0;
        private long bytesToSkip = 0;
        private bool closed = false;
        private readonly object _lock = new();
        private readonly int maxPacketLength;
        private readonly int highWaterMark;
        /// <summary>
        /// Gets called (inside the buffer lock, so keep it short) with the length of a packet that was longer than allowed and was skipped.
        /// </summary>
        public Action<int> OnOversizedPacket = delegate { };
        /// <summary>
        /// Gets called (inside the buffer lock, so keep it short) with a negative length; everything buffered is dropped because the stream is out of sync.
        /// </summary>
        public Action<int> OnInvalidLength = delegate { };

        /// <param name="maxPacketLength">Longer packets are skipped</param>
        /// <param name="highWaterMark">How many buffered bytes make the writer wait for the reader</param>
        public PacketReceiveBuffer(int maxPacketLength, int highWaterMark = 4 * 1024 * 1024)
        {
            this.maxPacketLength = maxPacketLength;
            this.highWaterMark = highWaterMark;
        }

        private int Count => writePos - readPos;

        /// <summary>
        /// Adds received bytes. Blocks while the buffer is full of data the reader can already process.
        /// </summary>
        public void Write(ReadOnlySpan<byte> data)
        {
            lock (_lock)
            {
                // waiting only while the reader has something to do: if a packet is bigger than highWaterMark, it still has to be received completely
                while (!closed && Count >= highWaterMark && HasWorkForReader())
                {
                    Monitor.Wait(_lock);
                }
                if (closed)
                {
                    return;
                }
                if (bytesToSkip > 0)
                {
                    int skip = (int)Math.Min(bytesToSkip, data.Length);
                    data = data[skip..];
                    bytesToSkip -= skip;
                }
                EnsureSpace(data.Length);
                data.CopyTo(buffer.AsSpan(writePos));
                writePos += data.Length;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Waits for the next complete packet.
        /// </summary>
        /// <returns>true if a packet was taken, false on timeout or if the buffer was closed</returns>
        public bool TryTake(out byte[] packet, int timeoutMs)
        {
            lock (_lock)
            {
                while (true)
                {
                    if (TryExtract(out packet))
                    {
                        // there's free space now, wake up the writer
                        Monitor.PulseAll(_lock);
                        return true;
                    }
                    if (closed)
                    {
                        return false;
                    }
                    if (!Monitor.Wait(_lock, timeoutMs))
                    {
                        bool taken = TryExtract(out packet);
                        if (taken)
                        {
                            Monitor.PulseAll(_lock);
                        }
                        return taken;
                    }
                }
            }
        }

        /// <summary>
        /// Drops all buffered data, e.g. when a new connection starts and old partial packets must not be glued to it.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                readPos = 0;
                writePos = 0;
                bytesToSkip = 0;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Releases all waiting threads; further data is ignored.
        /// </summary>
        public void Close()
        {
            lock (_lock)
            {
                closed = true;
                Monitor.PulseAll(_lock);
            }
        }

        private bool HasWorkForReader()
        {
            if (Count < 4)
            {
                return false;
            }
            int length = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(readPos));
            return length < 0 || length > maxPacketLength || Count >= length + 4;
        }

        private bool TryExtract(out byte[] packet)
        {
            while (Count >= 4)
            {
                int length = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(readPos));
                if (length < 0)
                {
                    // the stream is out of sync, there's no way to find the next packet
                    OnInvalidLength(length);
                    readPos = 0;
                    writePos = 0;
                    break;
                }
                if (length > maxPacketLength)
                {
                    // skip the packet without storing it, so the stream stays in sync
                    readPos += 4;
                    int skipNow = Math.Min(length, Count);
                    readPos += skipNow;
                    bytesToSkip = length - skipNow;
                    OnOversizedPacket(length);
                    continue;
                }
                if (Count < length + 4)
                {
                    break;
                }
                packet = buffer.AsSpan(readPos + 4, length).ToArray();
                readPos += length + 4;
                if (readPos == writePos)
                {
                    readPos = 0;
                    writePos = 0;
                    // give the memory back after a huge packet
                    if (buffer.Length > highWaterMark * 2)
                    {
                        buffer = new byte[64 * 1024];
                    }
                }
                return true;
            }
            packet = null!;
            return false;
        }

        private void EnsureSpace(int needed)
        {
            if (buffer.Length - writePos >= needed)
            {
                return;
            }
            // move unread data to the beginning, grow only if it still doesn't fit
            int count = Count;
            byte[] target = buffer;
            if (count + needed > buffer.Length)
            {
                target = new byte[Math.Max(buffer.Length * 2, count + needed)];
            }
            Buffer.BlockCopy(buffer, readPos, target, 0, count);
            buffer = target;
            readPos = 0;
            writePos = count;
        }
    }
}
