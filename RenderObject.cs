using System.Drawing;

namespace LPH_Edit_Viewer
{
    public class RenderObject
    {
        public bool IsPolyline = false;
        public bool IsLine = false;
        public int X1 = 0;
        public int X2 = 0;
        public int Y1 = 0;
        public int Y2 = 0;
        public decimal X1p = 0;
        public decimal X2p = 0;
        public decimal Y1p = 0;
        public decimal Y2p = 0;
        public decimal Rp = 0;
        public decimal Gp = 0;
        public decimal Bp = 0;
        public Color CLR = Color.White;
        public RenderObject()
        {
            
        }

        public RenderObject(
            bool isLine,
            bool isPline,
            int x1,
            int x2,
            int y1,
            int y2,
            Color clr,
            decimal x1p,
            decimal x2p,
            decimal y1p,
            decimal y2p,
            decimal rp,
            decimal gp,
            decimal bp)
        {
            IsLine = isLine;
            IsPolyline = isPline;
            X1 = x1;
            X2 = x2;
            Y1 = y1;
            Y2 = y2;
            CLR = clr;
            X1p = x1p;
            X2p = x2p;
            Y1p = y1p;
            Y2p = y2p;
            Rp = rp;
            Gp = gp;
            Bp = bp;
        }
        /// <summary>
        /// Draws this object on to the specified surface, that's represented with a <seealso cref="Graphics"/>
        /// </summary>
        /// <param name="surface"><seealso cref="Graphics"/>, that this polyline will be drawed to</param>
        /// <param name="boundingBox"><seealso cref="Rectangle"/>, that this polyline will take as a percent to pixel conversion reference</param>
        /// <returns>true, if draw was successful, otherwise - false</returns>
        public virtual bool Draw(Graphics g, Rectangle boundingBox)
        {
            try
            {
                if (IsLine)
                {
                    g.DrawLine(new Pen(CLR), X1, Y1, X2, Y2);
                    return true;
                }
                else
                {
                    g.FillRectangle(new SolidBrush(CLR), X1 - 2, Y1 - 2, 4, 4);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
