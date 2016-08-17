using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CsProjArrange
{
    /// <summary>
    /// Strategy for arranging a Visual Studio csproj file
    /// </summary>
    public class CsProjArrangeStrategy
    {
        private const string DefaultMarker = "[Default]";
        private const string NameOfFakeNodeForLastComment = "\x03A9";


        private readonly string[] _defaultStickyElementNames =
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

        private readonly string[] _defaultKeepOrderElementNames =
        {
            "Target",
        };

        private readonly string[] _defaultSortAttributes =
        {
            "Include",
        };

        private AttributeKeyComparer _attributeKeyComparer;
        private NodeNameComparer _nodeNameComparer;
        private readonly List<string> _stickyElementNames;
        private readonly List<string> _keepOrderElementNames;
        private readonly List<string> _sortAttributes;
        private readonly CsProjArrange.ArrangeOptions _options;

        public CsProjArrangeStrategy(IEnumerable<string> stickyElementNames, IEnumerable<string> keepOrderElementNames,
            IEnumerable<string> sortAttributes, CsProjArrange.ArrangeOptions options)
        {
            _stickyElementNames = (stickyElementNames ?? new[] {DefaultMarker}).ToList();
            ReplaceDefaultMarker(_stickyElementNames, _defaultStickyElementNames);
            _keepOrderElementNames = (keepOrderElementNames ?? new[] { DefaultMarker }).ToList();
            ReplaceDefaultMarker(_keepOrderElementNames, _defaultKeepOrderElementNames);
            _sortAttributes = (sortAttributes ?? new[] {DefaultMarker}).ToList();
            ReplaceDefaultMarker(_sortAttributes, _defaultSortAttributes);
            _options = options;
        }

        private static void ReplaceDefaultMarker(List<string> collection, IList<string> defaultValues)
        {
            if (collection.Contains(DefaultMarker))
            {
                collection.Remove(DefaultMarker);
                collection.AddRange(defaultValues);
            }
        }

        private void ArrangeElementByNameThenAttributes(XElement element)
        {
            if (!_keepOrderElementNames.Contains(element.Name.LocalName))
            {
                element.ReplaceNodes(
                    element.Nodes()
                        .OrderBy(x => x, _nodeNameComparer)
                        .ThenBy(x => x.NodeType == XmlNodeType.Element ? ((XElement) x).Attributes() : null,
                            _attributeKeyComparer)
                    );
            }

            // Arrange child elements.
            foreach (var child in element.Elements())
            {
                ArrangeElementByNameThenAttributes(child);
            }
        }

        public void Arrange(XDocument input)
        {           
            _nodeNameComparer = new NodeNameComparer(_stickyElementNames);

            _attributeKeyComparer  = CreateAttributeKeyComparer(_sortAttributes);

            CombineRootElementsAndSort(input, _options);

            if (_options.HasFlag(CsProjArrange.ArrangeOptions.SplitItemGroups))
            {
                SplitItemGroups(input, _stickyElementNames);
            }

            if (_options.HasFlag(CsProjArrange.ArrangeOptions.SortRootElements))
            {
                SortRootElements(input);
            }
        }

        private AttributeKeyComparer CreateAttributeKeyComparer(IEnumerable<string> sortAttributes)
        {
            return new AttributeKeyComparer(sortAttributes);
        }

        private void SortRootElements(XDocument input)
        {
            // Sort the elements in root.
            input.Root.ReplaceNodes(
                UnfoldComments(
                    FoldComments(input.Root.Nodes())
                    .OrderBy(x => x.Element, _nodeNameComparer)
                    .ThenBy(x => x.Element.NodeType == XmlNodeType.Element ? x.Element.Attributes() : null, _attributeKeyComparer))
                );
        }

        private class XElementWithComments
        {
            public XElement Element {
                get;
                set;
            }

            public IList<XComment> Comments
            {
                get;
                set;
            }

            public XElementWithComments()
            {
                Comments = new List<XComment>();
            }
        }

        private IEnumerable<XElementWithComments> FoldComments(IEnumerable<XNode> nodes)
        {
            var elements = new List<XElementWithComments>();

            XElementWithComments current = new XElementWithComments();
            foreach (var node in nodes)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Comment:
                        current.Comments.Add(node as XComment);
                        break;
                    case XmlNodeType.Element:
                        current.Element = node as XElement;
                        elements.Add(current);
                        current = new XElementWithComments();
                        break;
                }
            }

            // fold last standing comment into fake element
            if (current.Comments.Any())
            {
                current.Element = new XElement(NameOfFakeNodeForLastComment);
                elements.Add(current);
            }

            return elements;
        }

        private IEnumerable<XNode> UnfoldComments(IEnumerable<XElementWithComments> elements)
        {
            var nodes = new List<XNode>();
            foreach (var element in elements)
            {
                nodes.AddRange(element.Comments);
                if (element.Element.Name != NameOfFakeNodeForLastComment)
                {
                    nodes.Add(element.Element);
                }
            }

            return nodes;
        }

        private void SplitItemGroups(XDocument input, IList<string> stickyElementNames)
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
                if (options.HasFlag(CsProjArrange.ArrangeOptions.CombineRootElements))
                {
                    CombineIdenticalRootElements(elementGroup);
                }

                ArrangeAllElementsInGroup(elementGroup);
            }
        }

        private void ArrangeAllElementsInGroup(IGrouping<CombineGroups, XElement> elementGroup)
        {
            foreach (var element in elementGroup)
            {
                ArrangeElementByNameThenAttributes(element);
            }
        }

        private void CombineIdenticalRootElements(IGrouping<CombineGroups, XElement> elementGroup)
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
    }
}