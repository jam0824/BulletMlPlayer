using NUnit.Framework;
using BulletML;
using UnityEngine;
using System.Collections.Generic;

namespace BulletMLTests
{
    public class BulletMLFireCommandTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLBullet m_SourceBullet;

        [SetUp]
        public void Setup()
        {
            m_Executor = new BulletMLExecutor();
            m_SourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
        }

        [Test]
        public void ExecuteFireCommand_SimpleFire_CreatesBullet()
        {
            // Arrange
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.IsTrue(newBullets[0].IsActive);
        }

        [Test]
        public void ExecuteFireCommand_NoDirection_DefaultsToAim()
        {
            // Arrange
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);
            
            // 座標系をYZに設定
            m_Executor.SetCoordinateSystem(CoordinateSystem.YZ);
            
            // シューターとターゲット位置を設定
            var shooter = new BulletMLBullet(new Vector3(0f, 0f, 5f), 0f, 1f, CoordinateSystem.YZ, false);
            m_Executor.SetTargetPosition(new Vector3(0f, 0f, -5f)); // プレイヤー位置
            
            // toTarget = (0,0,-5) - (0,0,5) = (0,0,-10)
            // YZ面: Atan2(-10, 0) = -90度 = 270度（Z軸負方向）

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, shooter);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            // YZ面でZ軸負方向（270度）になるはず
            Assert.AreEqual(270f, newBullets[0].Direction, 1f); // 自機狙い方向
        }

        [Test]
        public void ExecuteFireCommand_NoDirection_SamePosition_UsesDefault()
        {
            // Arrange
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);
            
            // 座標系をXYに設定
            m_Executor.SetCoordinateSystem(CoordinateSystem.XY);
            
            // シューターとターゲットが同じ位置
            var shooter = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            m_Executor.SetTargetPosition(Vector3.zero); // 同じ位置

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, shooter);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            // 同じ位置の場合はデフォルト方向（0度=上方向）
            Assert.AreEqual(0f, newBullets[0].Direction, 0.001f);
        }

        [Test]
        public void ExecuteFireCommand_WithDirection_SetsBulletDirection()
        {
            // Arrange
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "90");
            directionElement.AddAttribute("type", "absolute");
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            
            fireElement.AddChild(directionElement);
            fireElement.AddChild(bulletElement);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(90f, newBullets[0].Direction, 0.001f);
        }

        [Test]
        public void ExecuteFireCommand_WithSpeed_SetsBulletSpeed()
        {
            // Arrange
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var speedElement = new BulletMLElement(BulletMLElementType.speed, null, "3.5");
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            
            fireElement.AddChild(speedElement);
            fireElement.AddChild(bulletElement);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(3.5f, newBullets[0].Speed, 0.001f);
        }

        [Test]
        public void ExecuteFireCommand_DirectionTypeAim_AimsAtTarget()
        {
            // Arrange
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "0");
            directionElement.AddAttribute("type", "aim");
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            
            fireElement.AddChild(directionElement);
            fireElement.AddChild(bulletElement);

            // ターゲット位置を設定（右側）
            var targetPosition = new Vector3(10f, 0f, 0f);
            m_Executor.SetTargetPosition(targetPosition);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            // 右方向を向いているはず（90度）
            Assert.AreEqual(90f, newBullets[0].Direction, 0.001f);
        }

        [Test]
        public void ExecuteFireCommand_DirectionTypeRelative_UsesRelativeDirection()
        {
            // Arrange
            m_SourceBullet.SetDirection(45f); // 45度方向
            
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "30");
            directionElement.AddAttribute("type", "relative");
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            
            fireElement.AddChild(directionElement);
            fireElement.AddChild(bulletElement);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            // 45 + 30 = 75度になるはず
            Assert.AreEqual(75f, newBullets[0].Direction, 0.001f);
        }

        [Test]
        public void ExecuteFireCommand_SpeedTypeRelative_UsesRelativeSpeed()
        {
            // Arrange
            m_SourceBullet.SetSpeed(2f);
            
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var speedElement = new BulletMLElement(BulletMLElementType.speed, null, "1.5");
            speedElement.AddAttribute("type", "relative");
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            
            fireElement.AddChild(speedElement);
            fireElement.AddChild(bulletElement);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            // 2 + 1.5 = 3.5になるはず
            Assert.AreEqual(3.5f, newBullets[0].Speed, 0.001f);
        }

        [Test]
        public void ExecuteFireCommand_WithBulletRef_ResolvesBulletReference()
        {
            // Arrange
            var document = new BulletMLDocument();
            var root = new BulletMLElement(BulletMLElementType.bulletml);
            var bulletTemplate = new BulletMLElement(BulletMLElementType.bullet, "testBullet");
            var directionInBullet = new BulletMLElement(BulletMLElementType.direction, null, "180");
            bulletTemplate.AddChild(directionInBullet);
            root.AddChild(bulletTemplate);
            document.SetRootElement(root);
            
            m_Executor.SetDocument(document);

            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            var bulletRefElement = new BulletMLElement(BulletMLElementType.bulletRef);
            bulletRefElement.AddAttribute("label", "testBullet");
            fireElement.AddChild(bulletRefElement);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, m_SourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(180f, newBullets[0].Direction, 0.001f);
        }
    }
}