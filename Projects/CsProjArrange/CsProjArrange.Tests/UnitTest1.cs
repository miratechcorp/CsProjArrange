using System;
using System.IO;
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
            var target = new CsProjArrange();

            target.Arrange(@".\TestData\CsProjArrangeInput.csproj", @"CsProjArrangeExpectedCombineRootElements.csproj", null, null, CsProjArrange.ArrangeOptions.CombineRootElements);


        }

        [Test]
        public void Arrange_when_defaults_used_should_return_expected_csproj()
        {
            var target = CreateTestTarget();

            // Act
            target.Arrange(@".\TestData\CsProjArrangeInput.csproj", "CsProjActualOutput.csproj", null, null, CsProjArrange.ArrangeOptions.None);

            // Assert
            string[] actualOutput = File.ReadAllLines(@"CsProjActualOutput.csproj");
            string[] expectedOutput = File.ReadAllLines(@".\TestData\Expected\CsProjArrangeExpectedDefault.csproj");
            actualOutput.Should().Equal(expectedOutput);
        }

        [Test]
        public void Arrange_when_combineRootElements_used_should_return_expected_csproj()
        {
            var target = CreateTestTarget();

            // Act
            target.Arrange(@".\TestData\CsProjArrangeInput.csproj", "CsProjActualOutput.csproj", null, null, CsProjArrange.ArrangeOptions.CombineRootElements);

            // Assert
            string[] actualOutput = File.ReadAllLines(@"CsProjActualOutput.csproj");
            string[] expectedOutput = File.ReadAllLines(@".\TestData\Expected\CsProjArrangeExpectedCombineRootElements.csproj");
            actualOutput.Should().Equal(expectedOutput);
        }

    }
}
