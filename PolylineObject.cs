using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPH_Edit_Viewer
{
    public class PolylineObject : RenderObject
    {
        private List<PointM> points = [];
        public Color Color;
        public PolylineObject() { }
        public PolylineObject(List<PointM> points, Color color)
        {
            this.points = points;
            this.Color = color;
            IsPolyline = true;
        }
        
        public override bool Draw(Graphics g, Rectangle boundingBox)
        {
            Point[] pointsBound = new Point[points.Count];
            for (int i = 0; i < pointsBound.Length; i++)
            {
                pointsBound[i] = points[i].ToPoint(boundingBox.Width, boundingBox.Height);
            }
            if (pointsBound.Length < 2)
            {
                Console.WriteLine("not enough points: " + pointsBound.Length);
                return false;
            }
            try
            {
                g.DrawLines(new Pen(Color), pointsBound);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void AddPoint(PointM point)
        {
            //Console.WriteLine("append point to polyline");
            points.Add(point);
        }
    }
}
