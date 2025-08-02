using NUnit.Framework;
using BulletML;

namespace BulletMLTests
{
    public class BulletMLDocumentTests
    {
        [Test]
        public void BulletMLDocument_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var document = new BulletMLDocument();

            // Assert
            Assert.IsNull(document.RootElement);
            Assert.AreEqual(BulletMLType.none, document.Type);
        }

        [Test]
        public void SetRootElement_SetsTypeVertical()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            root.AddAttribute("type", "vertical");

            // Act
            document.SetRootElement(root);

            // Assert
            Assert.AreEqual(root, document.RootElement);
            Assert.AreEqual(BulletMLType.vertical, document.Type);
        }

        [Test]
        public void SetRootElement_SetsTypeHorizontal()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            root.AddAttribute("type", "horizontal");

            // Act
            document.SetRootElement(root);

            // Assert
            Assert.AreEqual(BulletMLType.horizontal, document.Type);
        }

        [Test]
        public void GetLabeledBullet_ReturnsCorrectBullet()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            var bullet = new BulletMLElement(BulletMLElementType.bullet, "testBullet");
            root.AddChild(bullet);
            document.SetRootElement(root);

            // Act
            var result = document.GetLabeledBullet("testBullet");

            // Assert
            Assert.AreEqual(bullet, result);
        }

        [Test]
        public void GetLabeledAction_ReturnsCorrectAction()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            var action = new BulletMLElement(BulletMLElementType.action, "testAction");
            root.AddChild(action);
            document.SetRootElement(root);

            // Act
            var result = document.GetLabeledAction("testAction");

            // Assert
            Assert.AreEqual(action, result);
        }

        [Test]
        public void GetLabeledFire_ReturnsCorrectFire()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            var fire = new BulletMLElement(BulletMLElementType.fire, "testFire");
            root.AddChild(fire);
            document.SetRootElement(root);

            // Act
            var result = document.GetLabeledFire("testFire");

            // Assert
            Assert.AreEqual(fire, result);
        }

        [Test]
        public void GetTopAction_ReturnsTopLabeledAction()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            var topAction = new BulletMLElement(BulletMLElementType.action, "top");
            var otherAction = new BulletMLElement(BulletMLElementType.action, "other");
            root.AddChild(topAction);
            root.AddChild(otherAction);
            document.SetRootElement(root);

            // Act
            var result = document.GetTopAction();

            // Assert
            Assert.AreEqual(topAction, result);
        }

        [Test]
        public void GetLabeledBullet_ReturnsNullForNonExistentLabel()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            document.SetRootElement(root);

            // Act
            var result = document.GetLabeledBullet("nonExistent");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void SetRootElement_IndexesNestedLabeledElements()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            var action = new BulletMLElement(BulletMLElementType.action, "parent");
            var nestedBullet = new BulletMLElement(BulletMLElementType.bullet, "nested");
            action.AddChild(nestedBullet);
            root.AddChild(action);

            // Act
            document.SetRootElement(root);

            // Assert
            Assert.AreEqual(action, document.GetLabeledAction("parent"));
            Assert.AreEqual(nestedBullet, document.GetLabeledBullet("nested"));
        }
    }
}