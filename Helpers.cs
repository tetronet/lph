using System.Drawing.Imaging;
using System.Globalization;

namespace LPH_Edit_Viewer
{
    public class Helpers
    {
        private static readonly Point[] CursorPoints = [
            new(3, 2),
            new(3, 30),
            new(12, 29),
            new(18, 48),
            new(28, 47),
            new(23, 29),
            new(33, 27)
            ];
        public static Bitmap GetDrawnCursors(ViewersCursor[] data, int width, int height)
        {
            Bitmap result = new(width, height, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(result);
            for (int i = 0; i < data.Length; i++)
            {
                ViewersCursor currentCursor = data[i];
                if (currentCursor == null)
                {
                    continue;
                }
                g.FillPolygon(new SolidBrush(currentCursor.CursorColor), OffsetPointArray(CursorPoints, currentCursor.MouseX, currentCursor.MouseY));
            }
            return result;
        }

        public static Point[] OffsetPointArray(Point[] input, int dx, int dy)
        {
            Point[] result = new Point[input.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Point.Add(input[i], new(dx, dy));
            }
            return result;
        }
        /// <summary>
        /// Parses a number from LPH data independently of the system locale. Accepts both '.' and ',' as the decimal separator,
        /// because older LPH versions wrote numbers using the current culture (e.g. "12,5" on Russian Windows).
        /// </summary>
        public static decimal ParseDecimal(string s)
        {
            return decimal.Parse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static void CancelActionSafe(DiskStringBuilder dsb)
        {
            dsb.RemoveLastObject();
        }
    }
}
