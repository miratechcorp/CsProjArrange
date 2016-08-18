using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CsProjArrange
{
    /// <summary>
    /// Compares a list of attributes in order by value.
    /// </summary>
    public class AttributeKeyComparer : IComparer<IEnumerable<XAttribute>>
    {
        public AttributeKeyComparer(IEnumerable<string> sortAttributes)
        {
            SortAttributes = sortAttributes;
        }

        public IEnumerable<string> SortAttributes
        {
            get;
            set;
        }

        public int Compare(IEnumerable<XAttribute> x, IEnumerable<XAttribute> y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                return 1;
            }
            if (y == null)
            {
                return -1;
            }
            foreach (var attribute in SortAttributes)
            {
                string xValue = null;
                string yValue = null;
                var xAttribute = x.FirstOrDefault(a => a.Name.LocalName == attribute);
                var yAttribute = y.FirstOrDefault(a => a.Name.LocalName == attribute);
                if (xAttribute != null)
                {
                    xValue = xAttribute.Value;
                }
                if (yAttribute != null)
                {
                    yValue = yAttribute.Value;
                }
                int result = string.Compare(xValue ?? string.Empty, yValue ?? string.Empty, StringComparison.InvariantCulture);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }
}