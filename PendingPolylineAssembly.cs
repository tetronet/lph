using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPH_Edit_Viewer
{
    public class PendingPolylineAssembly
    {
        public ulong TotalCount;
        public SortedDictionary<ulong, string> Fragments = new SortedDictionary<ulong, string>();
        public Color FullObjectColor;

        public PendingPolylineAssembly()
        {
            
        }

        public PendingPolylineAssembly(Color clr)
        {
            FullObjectColor = clr;
        }
    }
}
