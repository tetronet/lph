using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPH_Edit_Viewer
{
    public class PolylineAssembler
    {
        private List<PolylineObject> output = new List<PolylineObject>(); 
        private Dictionary<Guid, PendingPolylineAssembly> pending = new Dictionary<Guid, PendingPolylineAssembly>();

        public PolylineAssembler()
        {
            
        }
        /// <summary>
        /// Receives polyline data on works around it like as it was received by a network client.
        /// </summary>
        /// <param name="data">Data</param>
        /// <returns>Was receive succesful</returns>
        public bool ReceiveData(string data)
        {
            Console.WriteLine("[total " + output.Count + "] polyline asm received: " + data);
            string[] dataSplit = data.Split(' ');
            if (data.StartsWith("pld")) // PolyLine Data
            {
                if (Guid.TryParse(dataSplit[1], out Guid g)
                    && pending.TryGetValue(g, out PendingPolylineAssembly polyline))
                {
                    Console.WriteLine("pld" + dataSplit[2]);
                    polyline.Fragments.Add(ulong.Parse(dataSplit[2]), data);
                    if (dataSplit[3] == "last")
                    {
                        Console.WriteLine("pld/last");
                        polyline.TotalCount = ulong.Parse(dataSplit[2]) + 1;
                        if (polyline.Fragments.Count == (int)polyline.TotalCount)
                        {
                            Console.WriteLine("pld/last/totalCountCheck: " + polyline.Fragments.Count);
                            output.Add(Assemble(polyline));
                            pending.Remove(g);
                        }
                    }
                    return true;
                }
            }
            else if (data.StartsWith("polyline"))
            {
                if (Guid.TryParse(dataSplit[4], out Guid guid))
                {
                    Console.WriteLine("polyline");
                    pending.Add(guid, new PendingPolylineAssembly(Color.FromArgb((int)(decimal.Parse(dataSplit[1]) * 255), (int)(decimal.Parse(dataSplit[2]) * 255), (int)(decimal.Parse(dataSplit[3]) * 255))));
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Returns first parsed PLD-style polyline in the buffer and fails with exceptions if there's no polylines available.
        /// </summary>
        public PolylineObject AvailablePolyline {
            get
            {
                PolylineObject r = output.First();
                output.RemoveAt(0);
                return r;
            }
        }
        /// <summary>
        /// true if there is an available for read parsed PLD-style polyline, otherwise - false.
        /// </summary>
        public bool Available {
            get
            {
                return output.Count != 0;
            }
        }

        private PolylineObject Assemble(PendingPolylineAssembly polylineAssembly)
        {
            PolylineObject result = new PolylineObject
            {
                Color = polylineAssembly.FullObjectColor
            };

            // sort
            var sortedFragments = polylineAssembly.Fragments
                .OrderBy(kvp => kvp.Key)  // by the segment seq
                .Select(kvp => kvp.Value)
                .ToList();

            foreach (string data in sortedFragments)
            {
                string[] pointData = data.Split(' ').Skip(4).ToArray();
                for (int i = 0; i < pointData.Length; i += 2)
                {
                    result.AddPoint(PointM.FromStrings(pointData[i], pointData[i + 1]));
                }
            }
            return result;
        }
        /*private bool CheckAllReceived(Guid id)
        {
            SortedDictionary<ulong, string> current = pending[id].Fragments;
            for (ulong i = 0; i < (ulong)current.LongCount(); i++)
            {
                if (current.ElementAt((int)i).Key != i) // tehnologia de genialia
                {
                    return false;
                }
            }
            return true;
        }*/
    }
}
