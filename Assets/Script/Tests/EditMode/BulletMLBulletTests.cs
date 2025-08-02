using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLBulletTests
    {
        [Test]
        public void BulletMLBullet_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            // Assert
            Assert.That(Vector3.Distance(Vector3.zero, bullet.Position), Is.LessThan(0.001f), "Position should be approximately Vector3.zero");
            Assert.AreEqual(0f, bullet.Direction, 0.001f);
            Assert.AreEqual(1f, bullet.Speed, 0.001f);
            Assert.That(Vector3.Distance(Vector3.zero, bullet.Acceleration), Is.LessThan(0.001f), "Acceleration should be approximately Vector3.zero");
            Assert.IsTrue(bullet.IsActive);
            Assert.IsNotNull(bullet.ActionStack);
        }

        [Test]
        public void SetPosition_UpdatesPosition()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var newPosition = new Vector3(5f, 10f, 0f);

            // Act
            bullet.SetPosition(newPosition);

            // Assert
            Assert.AreEqual(newPosition, bullet.Position);
        }

        [Test]
        public void SetDirection_UpdatesDirection()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            float newDirection = 90f;

            // Act
            bullet.SetDirection(newDirection);

            // Assert
            Assert.AreEqual(newDirection, bullet.Direction, 0.001f);
        }

        [Test]
        public void SetSpeed_UpdatesSpeed()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            float newSpeed = 3.5f;

            // Act
            bullet.SetSpeed(newSpeed);

            // Assert
            Assert.AreEqual(newSpeed, bullet.Speed, 0.001f);
        }

        [Test]
        public void SetAcceleration_UpdatesAcceleration()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var newAccel = new Vector3(1f, 2f, 0f);

            // Act
            bullet.SetAcceleration(newAccel);

            // Assert
            Assert.AreEqual(newAccel, bullet.Acceleration);
        }

        [Test]
        public void Vanish_DeactivatesBullet()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            // Act
            bullet.Vanish();

            // Assert
            Assert.IsFalse(bullet.IsActive);
        }

        [Test]
        public void Update_MovesPositionBasedOnDirectionAndSpeed()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f); // 上方向、速度1
            float deltaTime = 1f;

            // Act
            bullet.Update(deltaTime);

            // Assert
            // 上方向(0度)に速度1で1秒移動すると、Y座標が1増加するはず
            Assert.AreEqual(0f, bullet.Position.x, 0.001f);
            Assert.AreEqual(1f, bullet.Position.y, 0.001f);
        }

        [Test]
        public void Update_AppliesAcceleration()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            bullet.SetAcceleration(new Vector3(1f, 0f, 0f)); // X方向に加速度1
            float deltaTime = 1f;

            // Act
            bullet.Update(deltaTime);

            // Assert
            // 加速度による位置変化も考慮される
            Assert.Greater(bullet.Position.x, 0f);
        }

        [Test]
        public void GetVelocityVector_ReturnsCorrectVector()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 90f, 2f); // 右方向、速度2

            // Act
            var velocity = bullet.GetVelocityVector();

            // Assert
            Assert.AreEqual(2f, velocity.x, 0.001f);
            Assert.AreEqual(0f, velocity.y, 0.001f);
        }

        [Test]
        public void ConvertAngleToVector_XYPlane_ReturnsCorrectVector()
        {
            // Arrange & Act
            var vectorUp = BulletMLBullet.ConvertAngleToVector(0f, CoordinateSystem.XY);
            var vectorRight = BulletMLBullet.ConvertAngleToVector(90f, CoordinateSystem.XY);
            var vectorDown = BulletMLBullet.ConvertAngleToVector(180f, CoordinateSystem.XY);
            var vectorLeft = BulletMLBullet.ConvertAngleToVector(270f, CoordinateSystem.XY);

            // Assert (浮動小数点数の誤差を考慮)
            Assert.That(Vector3.Distance(Vector3.up, vectorUp), Is.LessThan(0.001f), "vectorUp should be approximately Vector3.up");
            Assert.That(Vector3.Distance(Vector3.right, vectorRight), Is.LessThan(0.001f), "vectorRight should be approximately Vector3.right");
            Assert.That(Vector3.Distance(Vector3.down, vectorDown), Is.LessThan(0.001f), "vectorDown should be approximately Vector3.down");
            Assert.That(Vector3.Distance(Vector3.left, vectorLeft), Is.LessThan(0.001f), "vectorLeft should be approximately Vector3.left");
        }

        [Test]
        public void ConvertAngleToVector_YZPlane_ReturnsCorrectVector()
        {
            // Arrange & Act
            var vectorUp = BulletMLBullet.ConvertAngleToVector(0f, CoordinateSystem.YZ);
            var vectorForward = BulletMLBullet.ConvertAngleToVector(90f, CoordinateSystem.YZ);
            var vectorDown = BulletMLBullet.ConvertAngleToVector(180f, CoordinateSystem.YZ);
            var vectorBack = BulletMLBullet.ConvertAngleToVector(270f, CoordinateSystem.YZ);

            // Assert (浮動小数点数の誤差を考慮)
            Assert.That(Vector3.Distance(Vector3.up, vectorUp), Is.LessThan(0.001f), "vectorUp should be approximately Vector3.up");
            Assert.That(Vector3.Distance(Vector3.forward, vectorForward), Is.LessThan(0.001f), "vectorForward should be approximately Vector3.forward");
            Assert.That(Vector3.Distance(Vector3.down, vectorDown), Is.LessThan(0.001f), "vectorDown should be approximately Vector3.down");
            Assert.That(Vector3.Distance(Vector3.back, vectorBack), Is.LessThan(0.001f), "vectorBack should be approximately Vector3.back");
        }
    }
}