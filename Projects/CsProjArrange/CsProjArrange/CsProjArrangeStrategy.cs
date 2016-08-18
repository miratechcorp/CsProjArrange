using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            _attributeKeyComparer = CreateAttributeKeyComparer(_sortAttributes);
            _nodeNameComparer = new NodeNameComparer(_stickyElementNames);

            input.Root.ReplaceNodes(
                UnfoldSections(
                    FoldSections(input.Root.Nodes())
                        .OrderBy(x => x.OptionSection ? 2 : 1)
                    )
                );
        }


        private void ArrangeSection(XNodeSection section, CsProjArrange.ArrangeOptions options)
        {

            CombineRootElementsAndSort(section, options);

            if (options.HasFlag(CsProjArrange.ArrangeOptions.SplitItemGroups)) {
                SplitItemGroups(section, _stickyElementNames);
            }

            if (options.HasFlag(CsProjArrange.ArrangeOptions.SortRootElements)) {
                SortRootElements(section);
            }
        }

        private AttributeKeyComparer CreateAttributeKeyComparer(IEnumerable<string> sortAttributes)
        {
            return new AttributeKeyComparer(sortAttributes);
        }

        private void SortRootElements(XNodeSection section)
        {
            // Sort the elements in root.
            if (_options.HasFlag(CsProjArrange.ArrangeOptions.KeepCommentWithNext)) {
                section.Nodes =
                    UnfoldComments(
                        FoldComments(section.Nodes)
                        .OrderBy(x => x.Element, _nodeNameComparer)
                        .ThenBy(x => x.Element.NodeType == XmlNodeType.Element ? x.Element.Attributes() : null, _attributeKeyComparer)
                        )
                    .ToList();
            } else {
                section.Nodes =
                    section.Nodes
                        .OrderBy(x => x, _nodeNameComparer)
                        .ThenBy(x => x.NodeType == XmlNodeType.Element ? ((XElement)x).Attributes() : null,
                            _attributeKeyComparer)
                        .ToList();
            }
        }

        private class XElementsWithComments
        {
            public IList<XComment> Comments
            {
                get;
                set;
            }

            public XElement Element
            {
                get;
                set;
            }

            public XElementsWithComments()
            {
                Comments = new List<XComment>();
            }
        }

        private class XNodeSection
        {
            public IList<XNode> Nodes
            {
                get;
                set;
            }

            public XComment OpenComment
            {
                get;
                set;
            }

            public XComment CloseComment
            {
                get;
                set;
            }

            public CsProjArrange.ArrangeOptions? Options
            {
                get;
                set;
            }

            public bool OptionSection
            {
                get;
                set;
            }

            public XNodeSection()
            {
                Nodes = new List<XNode>();
            }
        }

        private IEnumerable<XElementsWithComments> FoldComments(IEnumerable<XNode> nodes)
        {
            XElementsWithComments current = new XElementsWithComments();
            foreach (var node in nodes)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Comment:
                        current.Comments.Add(node as XComment);
                        break;
                    case XmlNodeType.Element:
                        current.Element = node as XElement;
                        yield return current;
                        current = new XElementsWithComments();
                        break;
                }
            }

            // fold last standing comment into fake element
            if (current.Comments.Any())
            {
                current.Element = new XElement(NameOfFakeNodeForLastComment);
                yield return current;
            }
        }

        private string _optionsOpenCommentRegexString = string.Format(@"^(\s*Options:\s*)({0}(,({0}))*)\s*$", string.Join("|", ((CsProjArrange.ArrangeOptions[])Enum.GetValues(typeof(CsProjArrange.ArrangeOptions))).Select(x => x.ToString())));
        private string _optionsCloseCommentRegexString = @"^(\s*/Options\s*)$";
        private IEnumerable<XNodeSection> FoldSections(IEnumerable<XNode> nodes)
        {
            XNodeSection none = new XNodeSection();
            XNodeSection current = none;
            bool section = false;
            foreach (var node in nodes) {
                switch (node.NodeType) {
                    case XmlNodeType.Comment:
                        var comment = (node as XComment).Value;
                        if (section) {
                            if (Regex.IsMatch(comment, _optionsCloseCommentRegexString)) {
                                current.CloseComment = node as XComment;
                                yield return current;
                                current = none;
                                section = false;
                            } else {
                                current.Nodes.Add(node);
                            }
                        } else {
                            var match = Regex.Match(comment, _optionsOpenCommentRegexString);
                            if (match.Success) {
                                if (current != none) {
                                    // Missing closing comment for previous section.
                                    yield return current;
                                }
                                current = new XNodeSection();
                                current.OpenComment = node as XComment;
                                current.Options = (CsProjArrange.ArrangeOptions)Enum.Parse(typeof(CsProjArrange.ArrangeOptions), match.Groups[2].Value);
                                current.OptionSection = true;
                                section = true;
                            } else {
                                current.Nodes.Add(node);
                            }
                        }
                        break;
                    default:
                        current.Nodes.Add(node);
                        break;
                }
            }
            if (current != none) {
                yield return current;
            }
            yield return none;
        }

        private IEnumerable<XNode> UnfoldComments(IEnumerable<XElementsWithComments> elements)
        {
            var nodes = new List<XNode>();
            foreach (var element in elements) {
                nodes.AddRange(element.Comments);
                if (element.Element.Name != NameOfFakeNodeForLastComment) {
                    nodes.Add(element.Element);
                }
            }

            return nodes;
        }

        private IEnumerable<XNode> UnfoldSections(IEnumerable<XNodeSection> sections)
        {
            var nodes = new List<XNode>();
            foreach (var section in sections)
            {
                if (section.OpenComment != null) {
                    nodes.Add(section.OpenComment);
                }
                ArrangeSection(section, section.Options ?? _options);
                nodes.AddRange(section.Nodes);
                if (section.CloseComment != null) {
                    nodes.Add(section.CloseComment);
                }
            }

            return nodes;
        }

        private void SplitItemGroups(XNodeSection section, IList<string> stickyElementNames)
        {
            foreach (var group in section.Nodes.Where(x => x is XElement).Cast<XElement>().Where(x => x.Name.LocalName == "ItemGroup").ToList())
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
                        // Insert node after.
                        section.Nodes.Insert(section.Nodes.IndexOf(@group) + 1, newElement);
                    }
                    @group.ReplaceNodes(@group.Elements(firstType));
                }
            }
        }

        private void CombineRootElementsAndSort(XNodeSection section, CsProjArrange.ArrangeOptions options)
        {
            var combineGroups =
                section.Nodes
                    .Where(x => x is XElement)
                    .Cast<XElement>()
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
                    CombineIdenticalRootElements(section, elementGroup);
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

        private void CombineIdenticalRootElements(XNodeSection section, IGrouping<CombineGroups, XElement> elementGroup)
        {
            XElement first = elementGroup.First();
            // Combine multiple elements if they have the same name and attributes.
            if (elementGroup.Count() > 1)
            {
                var restGroup = elementGroup.Skip(1);
                first.Add(restGroup.SelectMany(x => x.Elements()));
                foreach (var rest in restGroup)
                {
                    section.Nodes.Remove(rest);
                }
            }
        }
    }
}