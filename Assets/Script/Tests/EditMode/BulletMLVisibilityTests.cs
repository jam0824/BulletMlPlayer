using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLVisibilityTests
    {
        [Test]
        public void BulletMLBullet_DefaultConstructor_IsVisible()
        {
            // Arrange & Act
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            // Assert
            Assert.IsTrue(bullet.IsVisible);
        }

        [Test]
        public void BulletMLBullet_InvisibleConstructor_IsNotVisible()
        {
            // Arrange & Act
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);

            // Assert
            Assert.IsFalse(bullet.IsVisible);
        }

        [Test]
        public void SetVisible_ChangesVisibility()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            // Act
            bullet.SetVisible(false);

            // Assert
            Assert.IsFalse(bullet.IsVisible);

            // Act
            bullet.SetVisible(true);

            // Assert
            Assert.IsTrue(bullet.IsVisible);
        }

        [Test]
        public void SimpleFire_ShouldCreateOnlyOneVisibleBullet()
        {
            // Arrange
            string simpleFireXml = @"<?xml version=""1.0"" ?>
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<fire>
<bullet/>
</fire>
</action>
</bulletml>";

            var parser = new BulletMLParser();
            var executor = new BulletMLExecutor();
            var createdBullets = new System.Collections.Generic.List<BulletMLBullet>();

            executor.OnBulletCreated = bullet => createdBullets.Add(bullet);

            // Act
            var document = parser.Parse(simpleFireXml);
            executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var initialBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false); // 非表示のシューター
            var actionRunner = new BulletMLActionRunner(topAction);
            initialBullet.PushAction(actionRunner);

            // fireコマンドを実行
            executor.ExecuteCurrentAction(initialBullet);

            // Assert
            Assert.IsFalse(initialBullet.IsVisible); // シューターは非表示
            Assert.AreEqual(1, createdBullets.Count); // fireで1発作成
            Assert.IsTrue(createdBullets[0].IsVisible); // 作成された弾は表示
        }

        [Test]
        public void FireCommand_CreatesVisibleBullets()
        {
            // Arrange
            var executor = new BulletMLExecutor();
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);

            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false); // 非表示のシューター

            // Act
            var newBullets = executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.IsTrue(newBullets[0].IsVisible); // fireで作成された弾は表示される
        }
    }
}