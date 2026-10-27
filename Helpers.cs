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

        private static readonly string[] ColorChannelTexts = CreateColorChannelTexts();

        /// <summary>
        /// Text of one color channel for LPH data, written with Half (float16) precision: a few characters instead of ~29 for decimal.
        /// Readers decode a channel as (int)(value * 255), so the text is chosen to decode back to exactly the same byte.
        /// </summary>
        public static string ColorChannelToLph(int channel)
        {
            return ColorChannelTexts[channel];
        }

        /// <summary>
        /// "r g b" for LPH data, see <see cref="ColorChannelToLph"/>.
        /// </summary>
        public static string ColorToLph(Color color)
        {
            return $"{ColorChannelTexts[color.R]} {ColorChannelTexts[color.G]} {ColorChannelTexts[color.B]}";
        }

        private static string[] CreateColorChannelTexts()
        {
            string[] result = new string[256];
            for (int c = 0; c < result.Length; c++)
            {
                // plain Half(c / 255) is the shortest, but rounding often puts it just below c / 255, and (int)(value * 255) then gives c - 1;
                // the middle of the byte's range (c + 0.5) / 255 survives Half rounding and the truncation
                string plain = ((Half)(c / 255.0)).ToString(CultureInfo.InvariantCulture);
                string middle = ((Half)((c + 0.5) / 255.0)).ToString(CultureInfo.InvariantCulture);
                if (DecodeColorChannel(plain) == c)
                {
                    result[c] = plain;
                }
                else if (DecodeColorChannel(middle) == c)
                {
                    result[c] = middle;
                }
                else
                {
                    // never happens with float16, but keep the color exact anyway
                    result[c] = ((decimal)c / 255).ToString(CultureInfo.InvariantCulture);
                }
            }
            return result;
        }

        private static int DecodeColorChannel(string text)
        {
            return (int)(ParseDecimal(text) * 255);
        }

        public static void CancelActionSafe(DiskStringBuilder dsb)
        {
            dsb.RemoveLastObject();
        }
    }
}
