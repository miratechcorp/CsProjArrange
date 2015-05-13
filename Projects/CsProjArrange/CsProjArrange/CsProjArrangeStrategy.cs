using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CsProjArrange
{
    public class CsProjArrangeStrategy
    {
        private CsProjArrange.AttributeKeyComparer _attributeKeyComparer;
        private CsProjArrange.NodeNameComparer _nodeNameComparer;

        private void ArrangeElement(XElement element)
        {
            // Order by element name then by attributes.
            element.ReplaceNodes(
                element.Nodes()
                    .OrderBy(x => x, _nodeNameComparer)
                    .ThenBy(x => x.NodeType == XmlNodeType.Element ? ((XElement)x).Attributes() : null, _attributeKeyComparer)
                );
            // Arrange child elements.
            foreach (var child in element.Elements()) {
                ArrangeElement(child);
            }
        }

        public void Arrange(XDocument input, IList<string> stickyElementNames, IEnumerable<string> sortAttributes, CsProjArrange.ArrangeOptions options)
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
            _nodeNameComparer = new CsProjArrange.NodeNameComparer(stickyElementNames);

            _attributeKeyComparer  = CreateAttributeKeyComparer(sortAttributes);

            CombineRootElementsAndSort(input, options);

            if (options.HasFlag(CsProjArrange.ArrangeOptions.SplitItemGroups))
            {
                SplitItemGroups(input, stickyElementNames);
            }

            if (options.HasFlag(CsProjArrange.ArrangeOptions.SortRootElements))
            {
                SortRootElements(input);
            }
        }

        private static CsProjArrange.AttributeKeyComparer CreateAttributeKeyComparer(IEnumerable<string> sortAttributes)
        {
            if (sortAttributes == null)
            {
                sortAttributes = new string[]
                {
                    "Include",
                };
            }

            return new CsProjArrange.AttributeKeyComparer(sortAttributes);
        }

        private void SortRootElements(XDocument input)
        {
            // Sort the elements in root.
            input.Root.ReplaceNodes(
                input.Root.Nodes()
                    .OrderBy(x => x, _nodeNameComparer)
                    .ThenBy(x => x.NodeType == XmlNodeType.Element ? ((XElement) x).Attributes() : null,
                        _attributeKeyComparer)
                );
        }

        private static void SplitItemGroups(XDocument input, IList<string> stickyElementNames)
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

        private void CombineRootElementsAndSort(XDocument input, CsProjArrange.ArrangeOptions options)
        {
            var combineGroups =
                input.Root.Elements()
                    .GroupBy(
                        x =>
                            new CsProjArrange.CombineGroups
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
                if (options.HasFlag(CsProjArrange.ArrangeOptions.CombineRootElements))
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
        }
    }
}