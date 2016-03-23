using System.Collections.Generic;
using System.Xml.Linq;
using CsProjArrange.Tests.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace CsProjArrange.Tests
{
    [TestFixture]
    public class CsProjArrangeStrategyTests
    {
        private static CsProjArrangeStrategy CreateTestTarget(IEnumerable<string> stickyElementNames, IEnumerable<string> keepOrderElementNames,
            IEnumerable<string> sortAttributes, CsProjArrange.ArrangeOptions options)
        {
            return new CsProjArrangeStrategy(stickyElementNames, keepOrderElementNames, sortAttributes, options);
        }

        private static CsProjArrangeStrategy CreateDefaultTestTarget(CsProjArrange.ArrangeOptions options)
        {
            return new CsProjArrangeStrategy(null, null, null, options);
        }

        [Test]
        [Ignore]
        public void GetSomeData()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.CsProjArrangeInput.csproj");
            var target = CreateDefaultTestTarget(CsProjArrange.ArrangeOptions.CombineRootElements);

            target.Arrange(inputDocument);

            inputDocument.Save(@"CsProjArrangeExpectedCombineRootElements.csproj");
        }

        [Test]
        public void Arrange_when_defaults_used_should_return_expected_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.CsProjArrangeInput.csproj");
            var target = CreateDefaultTestTarget(CsProjArrange.ArrangeOptions.None);

            // Act
            target.Arrange(inputDocument);

            // Assert
            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.CsProjArrangeExpectedDefault.csproj");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }

        [Test]
        public void Arrange_when_combineRootElements_used_should_return_expected_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.CsProjArrangeInput.csproj");
            var target = CreateDefaultTestTarget(CsProjArrange.ArrangeOptions.CombineRootElements);

            // Act
            target.Arrange(inputDocument);

            // Assert
            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.CsProjArrangeExpectedCombineRootElements.csproj");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }

        [Test]
        public void Arrange_when_not_combineRootElements_should_return_sorted_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.SortingInput.xml");
            var target = CreateDefaultTestTarget(CsProjArrange.ArrangeOptions.None);

            // Act
            target.Arrange(inputDocument);

            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.SortingInput.xml");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }

        [Test]
        public void Arrange_when_some_elements_are_not_to_be_sorted_sort_the_other_elements_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.DoNotSortInput.xml");
            var target = CreateTestTarget(null, new[] {"DoNotSort"}, null, CsProjArrange.ArrangeOptions.None);

            // Act
            target.Arrange(inputDocument);

            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.DoNotSortInput.xml");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }

        [Test]
        public void Arrange_when_combineRootElements_should_return_sorted_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.SortingInput.xml");
            var target = CreateDefaultTestTarget(CsProjArrange.ArrangeOptions.CombineRootElements);

            target.Arrange(inputDocument);

            // Assert
            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.SortingInputCombineRootElements.xml");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }
    }
}
