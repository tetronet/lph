using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LPH_Edit_Viewer
{
    /// <summary>
    /// A bitmap whose pixels live in a managed array, so they can be written directly (fast point stamping)
    /// while GDI+ (<see cref="Graphics"/>) draws into the very same memory. Both must be used under the same lock.
    /// </summary>
    public sealed class PixelCanvas : IDisposable
    {
        public readonly int Width;
        public readonly int Height;
        /// <summary>
        /// ARGB pixels, row by row, one row is exactly <see cref="Width"/> pixels.
        /// </summary>
        public readonly int[] Pixels;
        public readonly Bitmap Bitmap;
        public readonly Graphics Graphics;

        public PixelCanvas(int width, int height)
        {
            Width = Math.Max(width, 1);
            Height = Math.Max(height, 1);
            // pinned: GDI+ keeps the address of this memory for the whole life of the bitmap
            Pixels = GC.AllocateArray<int>(Width * Height, pinned: true);
            Bitmap = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppArgb, Marshal.UnsafeAddrOfPinnedArrayElement(Pixels, 0));
            Graphics = Graphics.FromImage(Bitmap);
        }

        public void Dispose()
        {
            Graphics.Dispose();
            Bitmap.Dispose();
        }
    }
}
