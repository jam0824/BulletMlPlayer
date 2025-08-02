using NUnit.Framework;
using BulletML;

namespace BulletMLTests
{
    public class BulletMLElementTests
    {
        [Test]
        public void BulletMLElement_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var element = new BulletMLElement(BulletMLElementType.bullet, "testLabel", "testValue");

            // Assert
            Assert.AreEqual(BulletMLElementType.bullet, element.ElementType);
            Assert.AreEqual("testLabel", element.Label);
            Assert.AreEqual("testValue", element.Value);
            Assert.IsNotNull(element.Attributes);
            Assert.IsNotNull(element.Children);
        }

        [Test]
        public void AddAttribute_AddsAttributeCorrectly()
        {
            // Arrange
            var element = new BulletMLElement(BulletMLElementType.action);

            // Act
            element.AddAttribute("type", "absolute");

            // Assert
            Assert.AreEqual("absolute", element.GetAttribute("type"));
        }

        [Test]
        public void AddChild_AddsChildCorrectly()
        {
            // Arrange
            var parent = new BulletMLElement(BulletMLElementType.action);
            var child = new BulletMLElement(BulletMLElementType.fire);

            // Act
            parent.AddChild(child);

            // Assert
            Assert.AreEqual(1, parent.Children.Count);
            Assert.AreEqual(child, parent.Children[0]);
        }

        [Test]
        public void GetChild_ReturnsCorrectChild()
        {
            // Arrange
            var parent = new BulletMLElement(BulletMLElementType.action);
            var fireChild = new BulletMLElement(BulletMLElementType.fire);
            var waitChild = new BulletMLElement(BulletMLElementType.wait);
            parent.AddChild(fireChild);
            parent.AddChild(waitChild);

            // Act
            var result = parent.GetChild(BulletMLElementType.fire);

            // Assert
            Assert.AreEqual(fireChild, result);
        }

        [Test]
        public void GetChildren_ReturnsAllMatchingChildren()
        {
            // Arrange
            var parent = new BulletMLElement(BulletMLElementType.action);
            var fire1 = new BulletMLElement(BulletMLElementType.fire);
            var fire2 = new BulletMLElement(BulletMLElementType.fire);
            var wait = new BulletMLElement(BulletMLElementType.wait);
            parent.AddChild(fire1);
            parent.AddChild(fire2);
            parent.AddChild(wait);

            // Act
            var fires = parent.GetChildren(BulletMLElementType.fire);

            // Assert
            Assert.AreEqual(2, fires.Count);
            Assert.Contains(fire1, fires);
            Assert.Contains(fire2, fires);
        }

        [Test]
        public void GetDirectionType_ParsesCorrectly()
        {
            // Arrange
            var element = new BulletMLElement(BulletMLElementType.direction);
            element.AddAttribute("type", "absolute");

            // Act
            var directionType = element.GetDirectionType();

            // Assert
            Assert.AreEqual(DirectionType.absolute, directionType);
        }

        [Test]
        public void GetDirectionType_DefaultsToAim()
        {
            // Arrange
            var element = new BulletMLElement(BulletMLElementType.direction);

            // Act
            var directionType = element.GetDirectionType();

            // Assert
            Assert.AreEqual(DirectionType.aim, directionType);
        }

        [Test]
        public void GetSpeedType_ParsesCorrectly()
        {
            // Arrange
            var element = new BulletMLElement(BulletMLElementType.speed);
            element.AddAttribute("type", "relative");

            // Act
            var speedType = element.GetSpeedType();

            // Assert
            Assert.AreEqual(SpeedType.relative, speedType);
        }

        [Test]
        public void GetAccelType_ParsesCorrectly()
        {
            // Arrange
            var element = new BulletMLElement(BulletMLElementType.horizontal);
            element.AddAttribute("type", "sequence");

            // Act
            var accelType = element.GetAccelType();

            // Assert
            Assert.AreEqual(AccelType.sequence, accelType);
        }
    }
}