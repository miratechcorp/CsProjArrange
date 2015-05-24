using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;
using Moq;
using Moq.Protected;
using FluentAssertions;

namespace CsProjArrange.Tests
{
    [TestFixture]
    public class NodeNameComparerTests
    {
        internal NodeNameComparer CreateTestTarget()
        {
            var target = new NodeNameComparer();
            return target;
        }


        [Test]
        public void When_first_node_name_before_second_node_name_should_return_negative_one()
        {
            var target = CreateTestTarget();

            XNode nodea = new XElement(name: "A");
            XNode nodeb = new XElement(name: "B");

            // Act
            int compareResult = target.Compare(nodea, nodeb);

            // Assert
            compareResult.Should().Be(-1);
        }


        [Test]
        public void When_first_node_name_after_second_node_name_should_return_one()
        {
            var target = CreateTestTarget();

            XNode nodea = new XElement(name: "A");
            XNode nodeb = new XElement(name: "B");

            // Act
            int compareResult = target.Compare(nodeb, nodea);

            // Assert
            compareResult.Should().Be(1);
        }

        [Test]
        public void When_node_names_are_the_same_should_return_zero()
        {
            var target = CreateTestTarget();

            XNode nodea1 = new XElement(name: "A");
            XNode nodea2 = new XElement(name: "A");

            // Act
            int compareResult = target.Compare(nodea1, nodea2);

            // Assert
            compareResult.Should().Be(0);
        }

        [Test]
        public void When_nodes_null_should_throw_ArgumentNullException()
        {
            var target = CreateTestTarget();

            // Act
            Action comparingNullNode = () => target.Compare(x: null, y: null);

            // Assert
            comparingNullNode.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void When_node_names_match_stickyNames_ordering_should_match_sticky_order()
        {
            var target = CreateTestTarget();
            target.StickyElementNames = new List<string> {"B", "A"};

            XNode nodea = new XElement(name: "A");
            XNode nodeb = new XElement(name: "B");

            // Act
            int compareResult = target.Compare(nodea, nodeb);

            // Assert
            compareResult.Should().Be(1);
        }

        [Test]
        public void When_node_names_do_not_match_stickyNames_ordering_should_match_node_name_order()
        {
            var target = CreateTestTarget();
            target.StickyElementNames = new List<string> { "C", "D" };

            XNode nodea = new XElement(name: "A");
            XNode nodeb = new XElement(name: "B");

            // Act
            int compareResult = target.Compare(nodea, nodeb);

            // Assert
            compareResult.Should().Be(-1);
        }

    }
}
