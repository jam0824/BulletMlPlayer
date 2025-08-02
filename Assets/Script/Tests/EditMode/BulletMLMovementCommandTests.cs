using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLMovementCommandTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLBullet m_Bullet;

        [SetUp]
        public void Setup()
        {
            m_Executor = new BulletMLExecutor();
            m_Bullet = new BulletMLBullet(Vector3.zero, 0f, 2f);
        }

        [Test]
        public void ExecuteChangeDirectionCommand_InitializesCorrectly()
        {
            // Arrange
            var changeDirectionElement = new BulletMLElement(BulletMLElementType.changeDirection);
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "90");
            directionElement.AddAttribute("type", "absolute");
            var termElement = new BulletMLElement(BulletMLElementType.term, null, "60");
            
            changeDirectionElement.AddChild(directionElement);
            changeDirectionElement.AddChild(termElement);

            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(changeDirectionElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            m_Bullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(m_Bullet);

            // Assert
            Assert.IsTrue(result);
            // changeDirectionが開始されているはず
        }

        [Test]
        public void ChangeDirection_OverTime_GraduallyChangesDirection()
        {
            // Arrange
            m_Bullet.SetDirection(0f); // 開始方向は0度（上）
            
            // 90度に60フレームかけて変更
            var changeInfo = new BulletMLChangeInfo
            {
                StartValue = 0f,
                TargetValue = 90f,
                Duration = 60,
                CurrentFrame = 0,
                ChangeType = BulletMLChangeType.Direction
            };

            // Act & Assert
            // 30フレーム後（半分）
            changeInfo.CurrentFrame = 30;
            float direction30 = m_Executor.CalculateChangeValue(changeInfo);
            Assert.AreEqual(45f, direction30, 0.001f); // 0度から90度の半分

            // 60フレーム後（完了）
            changeInfo.CurrentFrame = 60;
            float direction60 = m_Executor.CalculateChangeValue(changeInfo);
            Assert.AreEqual(90f, direction60, 0.001f);
        }

        [Test]
        public void ExecuteChangeSpeedCommand_InitializesCorrectly()
        {
            // Arrange
            var changeSpeedElement = new BulletMLElement(BulletMLElementType.changeSpeed);
            var speedElement = new BulletMLElement(BulletMLElementType.speed, null, "5");
            speedElement.AddAttribute("type", "absolute");
            var termElement = new BulletMLElement(BulletMLElementType.term, null, "30");
            
            changeSpeedElement.AddChild(speedElement);
            changeSpeedElement.AddChild(termElement);

            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(changeSpeedElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            m_Bullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(m_Bullet);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ChangeSpeed_OverTime_GraduallyChangesSpeed()
        {
            // Arrange
            m_Bullet.SetSpeed(2f); // 開始速度は2
            
            // 5に30フレームかけて変更
            var changeInfo = new BulletMLChangeInfo
            {
                StartValue = 2f,
                TargetValue = 5f,
                Duration = 30,
                CurrentFrame = 0,
                ChangeType = BulletMLChangeType.Speed
            };

            // Act & Assert
            // 15フレーム後（半分）
            changeInfo.CurrentFrame = 15;
            float speed15 = m_Executor.CalculateChangeValue(changeInfo);
            Assert.AreEqual(3.5f, speed15, 0.001f); // 2から5の半分

            // 30フレーム後（完了）
            changeInfo.CurrentFrame = 30;
            float speed30 = m_Executor.CalculateChangeValue(changeInfo);
            Assert.AreEqual(5f, speed30, 0.001f);
        }

        [Test]
        public void ExecuteAccelCommand_InitializesCorrectly()
        {
            // Arrange
            var accelElement = new BulletMLElement(BulletMLElementType.accel);
            var horizontalElement = new BulletMLElement(BulletMLElementType.horizontal, null, "1");
            var verticalElement = new BulletMLElement(BulletMLElementType.vertical, null, "2");
            var termElement = new BulletMLElement(BulletMLElementType.term, null, "120");
            
            accelElement.AddChild(horizontalElement);
            accelElement.AddChild(verticalElement);
            accelElement.AddChild(termElement);

            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(accelElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            m_Bullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(m_Bullet);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Accel_OverTime_GraduallyAppliesAcceleration()
        {
            // Arrange
            var accelInfo = new BulletMLAccelInfo
            {
                HorizontalAccel = 1f,
                VerticalAccel = 2f,
                Duration = 120,
                CurrentFrame = 0
            };

            // Act & Assert
            // 60フレーム後（半分）
            accelInfo.CurrentFrame = 60;
            var accel60 = m_Executor.CalculateAcceleration(accelInfo);
            // 60フレーム分の加速度が適用されているはず
            Assert.Greater(accel60.x, 0f);
            Assert.Greater(accel60.y, 0f);

            // 120フレーム後（完了）
            accelInfo.CurrentFrame = 120;
            var accel120 = m_Executor.CalculateAcceleration(accelInfo);
            // 最大加速度が適用されているはず
            Assert.AreEqual(1f, accel120.x, 0.001f);
            Assert.AreEqual(2f, accel120.y, 0.001f);
        }

        [Test]
        public void ChangeDirection_RelativeType_UsesCurrentDirection()
        {
            // Arrange
            m_Bullet.SetDirection(45f);
            
            var changeDirectionElement = new BulletMLElement(BulletMLElementType.changeDirection);
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "30");
            directionElement.AddAttribute("type", "relative");
            var termElement = new BulletMLElement(BulletMLElementType.term, null, "1");
            
            changeDirectionElement.AddChild(directionElement);
            changeDirectionElement.AddChild(termElement);

            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(changeDirectionElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            m_Bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(m_Bullet);

            // 変更処理を完了させる
            var changeInfo = new BulletMLChangeInfo
            {
                StartValue = 45f,
                TargetValue = 45f + 30f, // relative +30
                Duration = 1,
                CurrentFrame = 1,
                ChangeType = BulletMLChangeType.Direction
            };

            float finalDirection = m_Executor.CalculateChangeValue(changeInfo);

            // Assert
            Assert.AreEqual(75f, finalDirection, 0.001f); // 45 + 30 = 75
        }

        [Test]
        public void ChangeSpeed_RelativeType_UsesCurrentSpeed()
        {
            // Arrange
            m_Bullet.SetSpeed(3f);
            
            var changeSpeedElement = new BulletMLElement(BulletMLElementType.changeSpeed);
            var speedElement = new BulletMLElement(BulletMLElementType.speed, null, "-1");
            speedElement.AddAttribute("type", "relative");
            var termElement = new BulletMLElement(BulletMLElementType.term, null, "1");
            
            changeSpeedElement.AddChild(speedElement);
            changeSpeedElement.AddChild(termElement);

            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(changeSpeedElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            m_Bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(m_Bullet);

            // 変更処理を完了させる
            var changeInfo = new BulletMLChangeInfo
            {
                StartValue = 3f,
                TargetValue = 3f + (-1f), // relative -1
                Duration = 1,
                CurrentFrame = 1,
                ChangeType = BulletMLChangeType.Speed
            };

            float finalSpeed = m_Executor.CalculateChangeValue(changeInfo);

            // Assert
            Assert.AreEqual(2f, finalSpeed, 0.001f); // 3 - 1 = 2
        }
    }
}