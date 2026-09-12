using System.Drawing;

namespace LPH_Edit_Viewer
{
    public struct PointM
    {
        public decimal X;
        public decimal Y;

        public PointM(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }

        public static PointM FromStrings(string x, string y)
        {
            return new PointM(decimal.Parse(x), decimal.Parse(y));
        }

        public override string ToString()
        {
            return $"{X} {Y}";
        }

        public static PointM FromNonNormalizedCoordinates(int x, int y, int width, int height)
        {
            return new PointM((decimal)x / width * 100, (decimal)y / height * 100);
        }

        public Point ToPoint(int fieldWidth, int fieldHeight)
        {
            return new Point((int)(X / 100 * fieldWidth), (int)(Y / 100 * fieldHeight));
        }
    }
}
