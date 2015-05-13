using System;
using System.IO;
using System.Xml.Linq;
using CsProjArrange.Tests.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace CsProjArrange.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        private static CsProjArrange CreateTestTarget()
        {
            return new CsProjArrange();
        }

        [Test]
        public void GetSomeData()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.CsProjArrangeInput.csproj");
            var target = new CsProjArrange();

            target.Arrange(inputDocument, null, null, CsProjArrange.ArrangeOptions.CombineRootElements);

            inputDocument.Save(@"CsProjArrangeExpectedCombineRootElements.csproj");
        }

        [Test]
        public void Arrange_when_defaults_used_should_return_expected_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.CsProjArrangeInput.csproj");
            var target = CreateTestTarget();

            // Act
            target.Arrange(inputDocument, null, null, CsProjArrange.ArrangeOptions.None);

            // Assert
            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.CsProjArrangeExpectedDefault.csproj");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }

        [Test]
        public void Arrange_when_combineRootElements_used_should_return_expected_csproj()
        {
            XDocument inputDocument =
                EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Input.CsProjArrangeInput.csproj");
            var target = CreateTestTarget();

            // Act
            target.Arrange(inputDocument, null, null, CsProjArrange.ArrangeOptions.CombineRootElements);

            // Assert
            XDocument expectedDocument = EmbeddedResourceHelper.ExtractManifestResourceAsXDocument("TestData.Expected.CsProjArrangeExpectedCombineRootElements.csproj");
            inputDocument.ToString().Should().BeEquivalentTo(expectedDocument.ToString());
        }

    }
}
