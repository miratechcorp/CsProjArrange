// Copyright 2014 MIRATECH
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CsProjArrange
{
    /// <summary>
    /// A class to arrange .csproj files.
    /// </summary>
    public class CsProjArrange
    {
        private static readonly XmlNodeType[] includeXmlNodeTypes = 
            new XmlNodeType[] {
                XmlNodeType.Comment,
                XmlNodeType.Element,
                XmlNodeType.EndElement,
                XmlNodeType.Text,
            };

        private AttributeKeyComparer attributeKeyComparer;
        private NodeNameComparer nodeNameComparer;

        [Flags]
        public enum ArrangeOptions
        {
            None = 0,
            All = -1,
            CombineRootElements = 1 << 0,
            KeepCommentWithNext = 1 << 1,
            KeepImportWithNext = 1 << 2,
            SortRootElements = 1 << 3,
            SplitItemGroups = 1 << 4,
            NoRoot = All & ~CombineRootElements & ~SortRootElements,
        }

        /// <summary>
        /// Arrange the project file using the specified options.
        /// </summary>
        /// <param name="inputFile">The file path for the input .csproj file. If none is specified, it will use the standard input.</param>
        /// <param name="outputFile">The file path for the output .csproj file. If none is specified, it will output to standard output.</param>
        /// <param name="stickyElementNames">A list of element names which should be stuck to the top when sorting the nodes. Defaults to the values: Import, Task, PropertyGroup, ItemGroup, Target, Configuration, Platform, ProjectReference, Reference, Compile, Folder, Content, None, When, and Otherwise.</param>
        /// <param name="sortAttributes">A list of attributes which should be used to sort the elements after they have been sorted by name. Defaults to the values: Include.</param>
        /// <param name="options">Options for the arrange.</param>
        public void Arrange(string inputFile, string outputFile, IList<string> stickyElementNames, IEnumerable<string> sortAttributes, ArrangeOptions options)
        {
            // Load the document.
            XDocument input;
            if (inputFile == null)
            {
                input = XDocument.Load(Console.OpenStandardInput());
            }
            else
            {
                input = XDocument.Load(inputFile);
            }

            Arrange(input, stickyElementNames, sortAttributes, options);

            // Backup input file if we are overwriting.
            if ((inputFile != null) && (inputFile == outputFile)) {
                File.Copy(inputFile, inputFile + ".bak", true);
            }

            if (outputFile == null) {
                // Write the output to standard output.
                var writerSettings = new XmlWriterSettings();
                writerSettings.Encoding = Encoding.UTF8;
                writerSettings.Indent = true;
                using (var writer = XmlWriter.Create(Console.OpenStandardOutput(), writerSettings)) {
                    input.WriteTo(writer);
                }
            } else {
                // Write the output file.
                input.Save(outputFile);
            }
        }

        private void ArrangeElement(XElement element)
        {
            // Order by element name then by attributes.
            element.ReplaceNodes(
                element.Nodes()
                    .OrderBy(x => x, nodeNameComparer)
                    .ThenBy(x => x.NodeType == XmlNodeType.Element ? ((XElement)x).Attributes() : null, attributeKeyComparer)
                );
            // Arrange child elements.
            foreach (var child in element.Elements()) {
                ArrangeElement(child);
            }
        }

        /// <summary>
        /// Compares a list of attributes in order by value.
        /// </summary>
        internal class AttributeKeyComparer : IComparer<IEnumerable<XAttribute>>
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
                if (x == null) {
                    if (y == null) {
                        return 0;
                    }
                    return 1;
                }
                if (y == null) {
                    return -1;
                }
                foreach (var attribute in SortAttributes) {
                    string xValue = null;
                    string yValue = null;
                    var xAttribute = x.FirstOrDefault(a => a.Name.LocalName == attribute);
                    var yAttribute = y.FirstOrDefault(a => a.Name.LocalName == attribute);
                    if (xAttribute != null) {
                        xValue = xAttribute.Value;
                    }
                    if (yAttribute != null) {
                        yValue = yAttribute.Value;
                    }
                    int result = string.Compare(xValue ?? string.Empty, yValue ?? string.Empty);
                    if (result != 0) {
                        return result;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Orders the nodes by nearest element name with some element names optionally stuck to the top in the supplied order.
        /// </summary>
        internal class NodeNameComparer : IComparer<XNode>
        {
            public NodeNameComparer(IList<string> stickyElementNames = null, ArrangeOptions options = ArrangeOptions.None)
            {
                StickyElementNames = stickyElementNames ?? new string[] { };
                Options = options;
            }

            public ArrangeOptions Options
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
                string xName = GetName(x);
                string yName = GetName(y);
                var xIndex = StickyElementNames.IndexOf(xName);
                var yIndex = StickyElementNames.IndexOf(yName);
                if ((xIndex == -1) && (yIndex == -1)) {
                    return string.Compare(xName, yName);
                }
                if ((yIndex == -1) || ((xIndex != -1) && (xIndex < yIndex))) {
                    return -1;
                }
                if ((xIndex == -1) || ((yIndex != -1) && (xIndex > yIndex))) {
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
                    if (Options.HasFlag(ArrangeOptions.KeepImportWithNext)) {
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


        public void Arrange(XDocument input, IList<string> stickyElementNames, IEnumerable<string> sortAttributes, ArrangeOptions options)
        {
            // Default values.
            var encoding = new UTF8Encoding(false);
            if (stickyElementNames == null)
            {
                stickyElementNames = new string[]
                {
                    // Primary
                    "Task",
                    "PropertyGroup",
                    "ItemGroup",
                    "Target",
                    // Secondary: PropertyGroup
                    "Configuration",
                    "Platform",
                    // Secondary: ItemGroup
                    "ProjectReference",
                    "Reference",
                    "Compile",
                    "Folder",
                    "Content",
                    "None",
                    // Secondary: Choose
                    "When",
                    "Otherwise",
                };
            }
            if (sortAttributes == null)
            {
                sortAttributes = new string[]
                {
                    "Include",
                };
            }
            // Set up sorting comparers.
            nodeNameComparer = new NodeNameComparer(stickyElementNames);
            attributeKeyComparer = new AttributeKeyComparer(sortAttributes);

            var combineGroups =
                input.Root.Elements()
                     .GroupBy(
                         x =>
                         new CombineGroups
                         {
                             Name = x.Name.Namespace.ToString() + ":" + x.Name.LocalName,
                             Attributes =
                             string.Join(Environment.NewLine,
                                 x.Attributes()
                                  .Select(y => y.Name.Namespace.ToString() + ":" + y.Name.LocalName + ":" + y.Value)),
                         }
                    );

            foreach (var elementGroup in combineGroups)
            {
                if (options.HasFlag(ArrangeOptions.CombineRootElements))
                {
                    XElement first = elementGroup.First();
                    // Combine multiple elements if they have the same name and attributes.
                    if (elementGroup.Count() > 1)
                    {
                        var restGroup = elementGroup.Skip(1);
                        first.Add(restGroup.SelectMany(x => x.Elements()));
                        foreach (var rest in restGroup)
                        {
                            rest.Remove();
                        }
                    }
                }
                
                foreach (var element in elementGroup)
                {
                    ArrangeElement(element);
                }                
            }

            if (options.HasFlag(ArrangeOptions.SplitItemGroups))
            {
                var ns = input.Root.Name.Namespace;
                foreach (var group in input.Root.Elements(ns + "ItemGroup"))
                {
                    var uniqueTypes =
                        @group.Elements()
                             .Select(x => x.Name)
                             .Distinct()
                             .OrderBy(
                                 x =>
                                 stickyElementNames.IndexOf(x.LocalName) == -1
                                     ? int.MaxValue
                                     : stickyElementNames.IndexOf(x.LocalName))
                             .ThenBy(x => x.LocalName)
                        ;
                    // Split into multiple item groups if there are multiple types included.
                    if (uniqueTypes.Count() > 1)
                    {
                        var firstType = uniqueTypes.First();
                        var restTypes = uniqueTypes.Skip(1).Reverse();
                        foreach (var type in restTypes)
                        {
                            var newElement = new XElement(@group.Name, @group.Attributes(), @group.Elements(type));
                            @group.AddAfterSelf(newElement);
                        }
                        @group.ReplaceNodes(@group.Elements(firstType));
                    }
                }
            }
            if (options.HasFlag(ArrangeOptions.SortRootElements))
            {
                // Sort the elements in root.
                input.Root.ReplaceNodes(
                    input.Root.Nodes()
                         .OrderBy(x => x, nodeNameComparer)
                         .ThenBy(x => x.NodeType == XmlNodeType.Element ? ((XElement)x).Attributes() : null,
                             attributeKeyComparer)
                    );
            }
        }

        internal struct CombineGroups
        {
            public string Name;
            public string Attributes;
        }
    }
}