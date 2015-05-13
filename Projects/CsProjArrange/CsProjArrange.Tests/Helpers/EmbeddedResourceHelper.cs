using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CsProjArrange.Tests.Helpers
{
    public class EmbeddedResourceHelper
    {
        public static Stream ExtractManifestResourceToDisk(string relativeManifestUri)
        {
            var assembly = Assembly.GetCallingAssembly();

            var uri = String.Format("{0}.{1}", assembly.GetName().Name, relativeManifestUri);

            return assembly.GetManifestResourceStream(uri);
        }

        public static XDocument ExtractManifestResourceAsXDocument(string relativeManifestUri)
        {
            XDocument doc;
            using (var stream = ExtractManifestResourceToDisk(relativeManifestUri))
            {
                doc = XDocument.Load(stream);
            }
            return doc;
        }

    }
}
