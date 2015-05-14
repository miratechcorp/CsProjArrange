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
            Options = options;
        }

        public CsProjArrange.ArrangeOptions Options
        {
            get;
            set;
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
                return String.Compare(xName, yName);
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
            if (node.NodeType == XmlNodeType.Comment) {
                name = GetNextClosestElementName(node);
            }
            if (node.NodeType == XmlNodeType.Element) {
                name = ((XElement)node).Name.LocalName;
                if (Options.HasFlag(CsProjArrange.ArrangeOptions.KeepImportWithNext)) {
                    if (name == "Import") {
                        // HACK: Need to figure out how to handle import. Just sticking to next element for now.
                        name = GetNextClosestElementName(node.NextNode);
                    }
                }
            }

            return name;
        }

        private string GetNextClosestElementName(XNode node)
        {
            XElement result;
            XNode current = node;
            while (((result = current as XElement) == null) && ((current = current.NextNode) != null)) {
                // Everything is already done in the condition.
            }
            if (result == null) {
                return null;
            }

            return result.Name.LocalName;
        }
    }
}