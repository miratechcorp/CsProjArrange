using System;
using System.Collections.Generic;
using System.Reflection;
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
        internal class Mocks
        {
            public Mocks()
            {
                InitializeMockFields();
            }

            public List<FieldInfo> GetMockFields()
            {
                List<FieldInfo> mockFields = new List<FieldInfo>();

                var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var fieldInfo in fields)
                {
                    if (fieldInfo.FieldType.BaseType.Name == "Mock")
                    {
                        mockFields.Add(this.GetType().GetField(fieldInfo.Name));
                    }
                }

                return mockFields;
            }

            public void InitializeMockFields()
            {
                List<FieldInfo> mocks = GetMockFields();
                foreach (var fieldInfo in mocks)
                {
                    var instance = Activator.CreateInstance(fieldInfo.FieldType);
                    fieldInfo.SetValue(this, instance);
                }
            }
        }

        internal NodeNameComparer CreateTestTarget(Mocks mocks)
        {
            var target = new NodeNameComparer();
            return target;
        }


        [Test]
        public void When_first_node_name_before_second_node_name_should_return_negative_one()
        {
            var mocks = new Mocks();
            var target = CreateTestTarget(mocks);

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
            var mocks = new Mocks();
            var target = CreateTestTarget(mocks);

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
            var mocks = new Mocks();
            var target = CreateTestTarget(mocks);

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
            var mocks = new Mocks();
            var target = CreateTestTarget(mocks);

            // Act
            Action comparingNullNode = () => target.Compare(x: null, y: null);

            // Assert
            comparingNullNode.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void When_node_names_match_stickyNames_ordering_should_match_sticky_order()
        {
            var mocks = new Mocks();
            var target = CreateTestTarget(mocks);
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
            var mocks = new Mocks();
            var target = CreateTestTarget(mocks);
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
