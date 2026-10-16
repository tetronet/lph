using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace LPH_Edit_Viewer
{
    /// <summary>
    /// Fast renderer for LPH text data. Reads the data in big blocks, parses numbers straight from the bytes and writes points
    /// directly into the <see cref="PixelCanvas"/> pixels; lines and polylines are still drawn by GDI+ into the same memory, in file order.
    /// The result is pixel-identical to the old path (decimal math + GDI+ FillEllipse/DrawLine for every object).
    /// </summary>
    public static class FastLphLoader
    {
        private const int BLOCK_SIZE = 1 << 20;
        private static readonly long[] Pow10 = CreatePow10();
        private static readonly int[] PointMaskX;
        private static readonly int[] PointMaskY;

        static FastLphLoader()
        {
            // take the exact pixels GDI+ fills for FillEllipse(x - 2, y - 2, 4, 4), so stamped points look exactly the same
            using Bitmap mask = new(8, 8, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(mask))
            {
                g.Clear(Color.White);
                g.FillEllipse(Brushes.Black, 2, 2, 4, 4);
            }
            var xs = new System.Collections.Generic.List<int>();
            var ys = new System.Collections.Generic.List<int>();
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (mask.GetPixel(x, y).R < 128)
                    {
                        xs.Add(x - 4);
                        ys.Add(y - 4);
                    }
                }
            }
            PointMaskX = xs.ToArray();
            PointMaskY = ys.ToArray();
        }

        /// <summary>
        /// Draws LPH objects from <paramref name="input"/> on top of what is already on the canvas.
        /// The lock is taken for each block (about 1 MB of data), so the screen can be updated while a big file is loading.
        /// </summary>
        /// <param name="length">How many bytes of <paramref name="input"/> to read</param>
        /// <param name="defaultColorWhenMissing">Viewer behaviour: "point x y" and "line x1 y1 x2 y2" without a color are drawn blue (Editor skips them)</param>
        /// <param name="onProgress">Gets called after each block with the count of lines processed in it</param>
        /// <returns>How many lines were processed (invalid objects are not counted)</returns>
        public static long Render(Stream input, long length, PixelCanvas canvas, object renderLock, bool defaultColorWhenMissing, Action<int>? onProgress = null)
        {
            Renderer renderer = new(canvas, defaultColorWhenMissing);
            byte[] buffer = new byte[BLOCK_SIZE];
            int filled = 0;
            long remaining = length;
            long total = 0;
            while (true)
            {
                if (filled == buffer.Length)
                {
                    // one line is longer than the buffer
                    Array.Resize(ref buffer, buffer.Length * 2);
                }
                int toRead = (int)Math.Min(buffer.Length - filled, remaining);
                int read = toRead > 0 ? input.Read(buffer, filled, toRead) : 0;
                filled += read;
                remaining -= read;
                bool end = read == 0;
                int processUpTo = end ? filled : buffer.AsSpan(0, filled).LastIndexOf((byte)'\n') + 1;
                if (processUpTo > 0)
                {
                    int processed;
                    lock (renderLock)
                    {
                        processed = renderer.ProcessLines(buffer.AsSpan(0, processUpTo));
                    }
                    total += processed;
                    onProgress?.Invoke(processed);
                    Buffer.BlockCopy(buffer, processUpTo, buffer, 0, filled - processUpTo);
                    filled -= processUpTo;
                }
                if (end)
                {
                    break;
                }
            }
            return total;
        }

        private sealed class Renderer
        {
            private readonly PixelCanvas canvas;
            private readonly bool defaultColorWhenMissing;
            private readonly PolylineAssembler polylines = new();
            private readonly Pen pen = new(Color.Black);
            private readonly Rectangle bounds;
            // GDI+ drew something since the last direct pixel write
            private bool gdiUsed = false;

            public Renderer(PixelCanvas canvas, bool defaultColorWhenMissing)
            {
                this.canvas = canvas;
                this.defaultColorWhenMissing = defaultColorWhenMissing;
                bounds = new Rectangle(0, 0, canvas.Width, canvas.Height);
            }

            /// <summary>
            /// Processes '\n'-separated lines. The empty piece after a final '\n' is not a line.
            /// </summary>
            public int ProcessLines(ReadOnlySpan<byte> data)
            {
                int processed = 0;
                while (data.Length > 0)
                {
                    int nl = data.IndexOf((byte)'\n');
                    ReadOnlySpan<byte> line = nl < 0 ? data : data[..nl];
                    data = nl < 0 ? ReadOnlySpan<byte>.Empty : data[(nl + 1)..];
                    if (ProcessLine(line))
                    {
                        processed++;
                    }
                }
                return processed;
            }

            /// <returns>false if the object is invalid and was skipped</returns>
            private bool ProcessLine(ReadOnlySpan<byte> line)
            {
                // split by single spaces, exactly like string.Split(' ')
                Span<int> starts = stackalloc int[9];
                Span<int> ends = stackalloc int[9];
                int count = 0;
                int start = 0;
                for (int i = 0; i <= line.Length; i++)
                {
                    if (i == line.Length || line[i] == (byte)' ')
                    {
                        if (count < starts.Length)
                        {
                            starts[count] = start;
                            ends[count] = i;
                        }
                        count++;
                        start = i + 1;
                    }
                }
                ReadOnlySpan<byte> kind = line[starts[0]..ends[0]];
                ReadOnlySpan<byte> Token(int index, ReadOnlySpan<byte> l, Span<int> s, Span<int> e) => l[s[index]..e[index]];

                if (kind.SequenceEqual("point"u8))
                {
                    bool defaultColor = defaultColorWhenMissing && count == 3;
                    if (!defaultColor && count < 6)
                    {
                        return false;
                    }
                    if (!TryCoord(Token(1, line, starts, ends), canvas.Width, out int x) || !TryCoord(Token(2, line, starts, ends), canvas.Height, out int y))
                    {
                        return false;
                    }
                    int argb;
                    if (defaultColor)
                    {
                        argb = Color.FromArgb(0, 0, 255).ToArgb();
                    }
                    else if (!TryColor(Token(3, line, starts, ends), Token(4, line, starts, ends), Token(5, line, starts, ends), out argb))
                    {
                        return false;
                    }
                    StampPoint(x, y, argb);
                    return true;
                }
                if (kind.SequenceEqual("line"u8))
                {
                    bool defaultColor = defaultColorWhenMissing && count == 5;
                    if (!defaultColor && count < 8)
                    {
                        return false;
                    }
                    if (!TryCoord(Token(1, line, starts, ends), canvas.Width, out int x1) || !TryCoord(Token(2, line, starts, ends), canvas.Height, out int y1)
                        || !TryCoord(Token(3, line, starts, ends), canvas.Width, out int x2) || !TryCoord(Token(4, line, starts, ends), canvas.Height, out int y2))
                    {
                        return false;
                    }
                    int argb;
                    if (defaultColor)
                    {
                        argb = Color.FromArgb(0, 0, 255).ToArgb();
                    }
                    else if (!TryColor(Token(5, line, starts, ends), Token(6, line, starts, ends), Token(7, line, starts, ends), out argb))
                    {
                        return false;
                    }
                    pen.Color = Color.FromArgb(argb);
                    canvas.Graphics.DrawLine(pen, x1, y1, x2, y2);
                    gdiUsed = true;
                    return true;
                }
                if (kind.SequenceEqual("polyline"u8) || kind.SequenceEqual("pld"u8))
                {
                    try
                    {
                        polylines.ReceiveData(Encoding.UTF8.GetString(line));
                        if (polylines.TryTakePolyline(out PolylineObject polyline))
                        {
                            polyline.Draw(canvas.Graphics, bounds);
                            gdiUsed = true;
                        }
                    }
                    catch (Exception e)
                    {
                        DebugWriter.WriteDebug($"error when decoding polyline: {e}");
                    }
                    return true;
                }
                // unknown objects are ignored, like before
                return true;
            }

            private void StampPoint(int x, int y, int argb)
            {
                if (gdiUsed)
                {
                    // make sure everything GDI+ drew earlier is in memory before writing pixels after it
                    canvas.Graphics.Flush(FlushIntention.Sync);
                    gdiUsed = false;
                }
                int[] pixels = canvas.Pixels;
                int width = canvas.Width;
                int height = canvas.Height;
                for (int k = 0; k < PointMaskX.Length; k++)
                {
                    int px = x + PointMaskX[k];
                    int py = y + PointMaskY[k];
                    if ((uint)px < (uint)width && (uint)py < (uint)height)
                    {
                        pixels[py * width + px] = argb;
                    }
                }
            }
        }

        /// <summary>
        /// Same as (int)(size * value / 100) with decimal math.
        /// </summary>
        private static bool TryCoord(ReadOnlySpan<byte> token, int size, out int result)
        {
            result = 0;
            if (TryParseFast(token, out long mantissa, out int scale))
            {
                Int128 value = (Int128)mantissa * size / ((Int128)Pow10[scale] * 100);
                if (value < int.MinValue || value > int.MaxValue)
                {
                    return false;
                }
                result = (int)value;
                return true;
            }
            if (!TryParseSlow(token, out decimal d))
            {
                return false;
            }
            try
            {
                result = (int)(size * d / 100);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Same as Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255)) with decimal math; false where FromArgb would throw.
        /// </summary>
        private static bool TryColor(ReadOnlySpan<byte> r, ReadOnlySpan<byte> g, ReadOnlySpan<byte> b, out int argb)
        {
            argb = 0;
            if (!TryChannel(r, out int red) || !TryChannel(g, out int green) || !TryChannel(b, out int blue))
            {
                return false;
            }
            argb = Color.FromArgb(red, green, blue).ToArgb();
            return true;
        }

        private static bool TryChannel(ReadOnlySpan<byte> token, out int result)
        {
            result = 0;
            if (TryParseFast(token, out long mantissa, out int scale))
            {
                Int128 value = (Int128)mantissa * 255 / Pow10[scale];
                if (value < 0 || value > 255)
                {
                    return false;
                }
                result = (int)value;
                return true;
            }
            if (!TryParseSlow(token, out decimal d))
            {
                return false;
            }
            try
            {
                result = (int)(d * 255);
            }
            catch (OverflowException)
            {
                return false;
            }
            return result >= 0 && result <= 255;
        }

        /// <summary>
        /// Parses the common form "-123,456" / "123.456" (up to 18 digits) as mantissa / 10^scale.
        /// Anything else returns false and goes to <see cref="TryParseSlow"/>, so the result is always the same as <see cref="Helpers.ParseDecimal"/>.
        /// </summary>
        private static bool TryParseFast(ReadOnlySpan<byte> token, out long mantissa, out int scale)
        {
            mantissa = 0;
            scale = 0;
            token = TrimWhite(token);
            int i = 0;
            bool negative = false;
            if (i < token.Length && token[i] == (byte)'-')
            {
                negative = true;
                i++;
            }
            int digits = 0;
            bool separator = false;
            for (; i < token.Length; i++)
            {
                byte c = token[i];
                if (c >= (byte)'0' && c <= (byte)'9')
                {
                    if (++digits > 18)
                    {
                        return false;
                    }
                    mantissa = mantissa * 10 + (c - '0');
                    if (separator)
                    {
                        scale++;
                    }
                }
                else if ((c == (byte)',' || c == (byte)'.') && !separator)
                {
                    separator = true;
                }
                else
                {
                    return false;
                }
            }
            if (digits == 0)
            {
                return false;
            }
            if (negative)
            {
                mantissa = -mantissa;
            }
            return true;
        }

        private static bool TryParseSlow(ReadOnlySpan<byte> token, out decimal value)
        {
            try
            {
                value = Helpers.ParseDecimal(Encoding.UTF8.GetString(token));
                return true;
            }
            catch (Exception e) when (e is FormatException || e is OverflowException)
            {
                value = 0;
                return false;
            }
        }

        private static ReadOnlySpan<byte> TrimWhite(ReadOnlySpan<byte> token)
        {
            int start = 0;
            int end = token.Length;
            while (start < end && IsWhite(token[start])) start++;
            while (end > start && IsWhite(token[end - 1])) end--;
            return token[start..end];
        }

        private static bool IsWhite(byte c) => c == (byte)' ' || (c >= 0x09 && c <= 0x0D);

        private static long[] CreatePow10()
        {
            long[] result = new long[19];
            result[0] = 1;
            for (int i = 1; i < result.Length; i++)
            {
                result[i] = result[i - 1] * 10;
            }
            return result;
        }
    }
}
