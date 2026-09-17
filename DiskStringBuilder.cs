using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace LPH_Edit_Viewer
{
    /// <summary>
    /// Works like a StringBuilder, but it's buffer is saved and modified ONLY on disk to prevent OutOfMemoryException when working with extrelemy large strings.
    /// </summary>
    public class DiskStringBuilder : IDisposable
    {
        private string tempFileName = "";
        private string realFileName = "";
        private bool isInited = false;
        private List<Action> changeEvents = new List<Action>();
        private FileStream fs;
        private object globalLock = new object();
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="tempFname">What file will be used as a temporary storage for the output string</param>
        public DiskStringBuilder(string tempFname)
        {
            tempFileName = tempFname;
            if (File.Exists(tempFname))
            {
                File.Delete(tempFname);
            }
        }
        /// <summary>
        /// Initializes this Disk String Builder holding an empty file.
        /// </summary>
        /// <returns>Was this initialization successful</returns>
        public bool Init()
        {
            lock (globalLock)
            {
                if (isInited)
                {
                    return false;
                }
                else
                {
                    isInited = true;
                    File.WriteAllText(tempFileName, "");
                    fs = new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    return true;
                }
            }
            
        }
        /// <summary>
        /// Initializes this Disk String Builder by copying the file to the temp file and opening it for read and write
        /// </summary>
        /// <param name="fname">What file to copy to temp storage</param>
        /// <returns>Was the initialization process successful</returns>
        public bool Init(string fname)
        {
            lock (globalLock)
            {
                if (isInited)
                {
                    return false;
                }
                if (!File.Exists(fname))
                {
                    return false;
                }
                try
                {
                    File.Copy(fname, tempFileName);
                    realFileName = fname;
                    fs = new FileStream(tempFileName, FileMode.OpenOrCreate);
                    isInited = true;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            
        }
        /// <summary>
        /// Adds specified string to the end of the stream.
        /// </summary>
        /// <param name="data">What to add</param>
        /// <returns>Itself</returns>
        public DiskStringBuilder Append(string data)
        {
            lock (globalLock)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                fs.Seek(0, SeekOrigin.End);
                fs.Write(bytes, 0, bytes.Length);
                MInvokeEvents();
                return this;
            }
        }
        public override string ToString()
        {
            lock (globalLock)
            {
                if (!isInited)
                {
                    throw new InvalidOperationException("not inited");
                }
                fs.Position = 0;
                byte[] returnValue = new byte[fs.Length];
                fs.Read(returnValue, 0, returnValue.Length);
                return Encoding.UTF8.GetString(returnValue);
            }
        }
        /// <summary>
        /// Continously reads bytes while specified character is not found in the stream. After reading, the reading cursor will be left on the position where the terminating character was.
        /// </summary>
        /// <param name="readBefore">What character will terminate the reading process</param>
        /// <param name="result">Result, a string, that contains all data read while specified character was not presented in the stream</param>
        /// <param name="startFrom">Where to start reading</param>
        /// <returns>Where the cursor is after the reading process</returns>
        public long ReadBefore(char readBefore, long startFrom, out string result)
        {
            lock (globalLock)
            {
                if (!isInited)
                {
                    throw new InvalidOperationException("not inited");
                }
                List<byte> outputBytes = new List<byte>();
                fs.Position = startFrom;
                while (true)
                {
                    int read = fs.ReadByte();
                    if (read == -1)
                    {
                        break;
                    }
                    if (read == readBefore)
                    {
                        break;
                    }
                    outputBytes.Add((byte)read);
                }
                result = Encoding.UTF8.GetString(outputBytes.ToArray());

            }
            return fs.Position;
        }
        public long GetTotalLength()
        {
            return fs.Length;
        }
        /// <summary>
        /// Saves this String Builder string representation to it's origin file.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Flush()
        {
            lock (globalLock)
            {
                if (isInited)
                {
                    fs.Flush(true);
                    File.Copy(tempFileName, realFileName, true);
                }
                else
                {
                    throw new InvalidOperationException("not inited");
                }
            }
            
        }
        /// <summary>
        /// Saves this String Builder string representation to the specified file. Destination file must NOT exist.
        /// </summary>
        /// <param name="whereToFlush"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Flush(string whereToFlush)
        {
            lock (globalLock)
            {
                if (isInited)
                {
                    fs.Flush(true);
                    File.Copy(tempFileName, whereToFlush, true);
                }
                else
                {
                    throw new InvalidOperationException("not inited");
                }
            }
        }
        /// <summary>
        /// Removes every character from the buffer.
        /// </summary>
        public void Clear()
        {
            lock (globalLock)
            {
                if (!isInited) throw new InvalidOperationException("not inited");
                fs.SetLength(0);
                fs.Position = 0;
            }
        }
        public long TruncateFromEndBefore(char eraseBefore, out string deleted)
        {
            lock (globalLock)
            {
                if (!isInited) throw new InvalidOperationException("not inited");

                fs.Flush();
                long originalPosition = fs.Position;

                StringBuilder sb = new();

                // Идём с конца файла
                for (long pos = fs.Length - 1; pos >= 0; pos--)
                {
                    fs.Position = pos;
                    int b = fs.ReadByte();

                    if (b == eraseBefore)
                    {
                        // Обрезаем файл после этой позиции
                        Console.WriteLine($"length before: {fs.Length}");
                        fs.SetLength(pos);
                        fs.Position = Math.Min(originalPosition, fs.Length);
                        Console.WriteLine($"length after: {fs.Length}");
                        MInvokeEvents();
                        deleted = sb.ToString();
                        return fs.Length;
                    }

                    sb.Append((char)b);
                }

                // Если символ не найден, ничего не удаляем
                fs.Position = originalPosition;
                deleted = "";
                return fs.Length;
            }
            
        }
        public bool RemoveString(string whatToRemove)
        {
            lock (globalLock)
            {
                if (!isInited)
                throw new InvalidOperationException("not inited");

                byte[] target = Encoding.UTF8.GetBytes(whatToRemove);

                long foundPos = -1;

                // Поиск
                for (long pos = 0; pos <= fs.Length - target.Length; pos++)
                {
                    fs.Position = pos;

                    byte[] buffer = new byte[target.Length];
                    int read = fs.Read(buffer, 0, buffer.Length);

                    if (read != target.Length)
                        break;

                    bool match = true;

                    for (int i = 0; i < target.Length; i++)
                    {
                        if (buffer[i] != target[i])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        foundPos = pos;
                        break;
                    }
                }

                if (foundPos == -1)
                    return false;

                // Сдвиг хвоста файла влево
                const int blockSize = 64 * 1024;
                byte[] moveBuffer = new byte[blockSize];

                long sourcePos = foundPos + target.Length;
                long destPos = foundPos;

                while (sourcePos < fs.Length)
                {
                    fs.Position = sourcePos;

                    int bytesToRead = (int)Math.Min(blockSize, fs.Length - sourcePos);
                    int bytesRead = fs.Read(moveBuffer, 0, bytesToRead);

                    if (bytesRead == 0)
                        break;

                    fs.Position = destPos;
                    fs.Write(moveBuffer, 0, bytesRead);

                    sourcePos += bytesRead;
                    destPos += bytesRead;
                }

                // Укорачиваем файл
                fs.SetLength(fs.Length - target.Length);

                MInvokeEvents();

                return true;
            }
        }
        public void Dispose()
        {
            lock (globalLock)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
                if (File.Exists(tempFileName))
                {
                    File.Delete(tempFileName);
                }
            }
        }
        public int CountChars(char c)
        {
            long currentIndex = fs.Position;
            fs.Position = 0;
            int result = 0;
            while (true)
            {
                int currentByte = fs.ReadByte();
                if (currentByte == -1)
                {
                    break;
                }
                if (currentByte == c)
                {
                    result++;
                }
            }
            fs.Position = currentIndex;
            return result;
        }
        public void AttachGenericModificationEvent(Action e)
        {
            changeEvents.Add(e);
        }
        private void MInvokeEvents()
        {
            foreach (var e in changeEvents)
            {
                e();
            }
        }
    }
}
