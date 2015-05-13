using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace CsProjArrange.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void GetSomeData()
        {
            var target = new CsProjArrange();

            target.Arrange(@".\TestData\CsProjArrangeInput.csproj", @"CsProjArrangeExpectedDefault.csproj", null, null, CsProjArrange.ArrangeOptions.None);


        }
    }
}
