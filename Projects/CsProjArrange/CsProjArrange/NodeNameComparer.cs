using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace CsProjArrange
{
    /// <summary>
    /// Orders the nodes by nearest element name with some element names optionally stuck to the top in the supplied order.
    /// </summary>
    public class NodeNameComparer : IComparer<XNode>
    {
        public NodeNameComparer(IList<string> stickyElementNames = null, CsProjArrange.ArrangeOptions options = CsProjArrange.ArrangeOptions.None)
        {
            StickyElementNames = stickyElementNames ?? new string[] { };
        }

        public IList<string> StickyElementNames
        {
            get;
            set;
        }

        public int Compare(XNode x, XNode y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");
            string xName = GetName(x);
            string yName = GetName(y);
            var stickyElement1 = StickyElementNames.IndexOf(xName);
            var stickyElement2 = StickyElementNames.IndexOf(yName);
            if ((stickyElement1 == -1) && (stickyElement2 == -1))
            {
                return String.Compare(xName, yName, StringComparison.InvariantCulture);
            }
            return Compare(stickyElement1, stickyElement2);
        }

        private static int Compare(int stickyElement1, int stickyElement2)
        {
            if ((stickyElement2 == -1) || ((stickyElement1 != -1) && (stickyElement1 < stickyElement2)))
            {
                return -1;
            }
            if ((stickyElement1 == -1) || ((stickyElement2 != -1) && (stickyElement1 > stickyElement2)))
            {
                return 1;
            }

            return 0;
        }

        private string GetName(XNode node)
        {
            string name = null;
            if (node.NodeType == XmlNodeType.Element) {
                name = ((XElement)node).Name.LocalName;
            }

            return name;
        }
    }
}