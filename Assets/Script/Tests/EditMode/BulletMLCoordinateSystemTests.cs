using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLCoordinateSystemTests
    {
        [Test]
        public void ConvertAngleToVector_XY_ZeroDegrees_ReturnsUpDirection()
        {
            // Arrange
            float angle = 0f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.XY);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f);
            Assert.AreEqual(1f, result.y, 0.001f);
            Assert.AreEqual(0f, result.z, 0.001f);
            Debug.Log($"XY面 0度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_XY_NinetyDegrees_ReturnsRightDirection()
        {
            // Arrange
            float angle = 90f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.XY);

            // Assert
            Assert.AreEqual(1f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
            Assert.AreEqual(0f, result.z, 0.001f);
            Debug.Log($"XY面 90度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_XY_OneEightyDegrees_ReturnsDownDirection()
        {
            // Arrange
            float angle = 180f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.XY);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f);
            Assert.AreEqual(-1f, result.y, 0.001f);
            Assert.AreEqual(0f, result.z, 0.001f);
            Debug.Log($"XY面 180度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_XY_TwoSeventyDegrees_ReturnsLeftDirection()
        {
            // Arrange
            float angle = 270f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.XY);

            // Assert
            Assert.AreEqual(-1f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
            Assert.AreEqual(0f, result.z, 0.001f);
            Debug.Log($"XY面 270度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_YZ_ZeroDegrees_ReturnsUpDirection()
        {
            // Arrange
            float angle = 0f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f); // X軸は使用しない
            Assert.AreEqual(1f, result.y, 0.001f); // 上方向
            Assert.AreEqual(0f, result.z, 0.001f); // 前後方向
            Debug.Log($"YZ面 0度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_YZ_NinetyDegrees_ReturnsForwardDirection()
        {
            // Arrange
            float angle = 90f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f); // X軸は使用しない
            Assert.AreEqual(0f, result.y, 0.001f); // 縦方向
            Assert.AreEqual(1f, result.z, 0.001f); // 前方向
            Debug.Log($"YZ面 90度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_YZ_OneEightyDegrees_ReturnsDownDirection()
        {
            // Arrange
            float angle = 180f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f); // X軸は使用しない
            Assert.AreEqual(-1f, result.y, 0.001f); // 下方向
            Assert.AreEqual(0f, result.z, 0.001f); // 前後方向
            Debug.Log($"YZ面 180度: {result}");
        }

        [Test]
        public void ConvertAngleToVector_YZ_TwoSeventyDegrees_ReturnsBackDirection()
        {
            // Arrange
            float angle = 270f;

            // Act
            Vector3 result = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f); // X軸は使用しない
            Assert.AreEqual(0f, result.y, 0.001f); // 縦方向
            Assert.AreEqual(-1f, result.z, 0.001f); // 後方向
            Debug.Log($"YZ面 270度: {result}");
        }

        [Test]
        public void CircularPattern_XY_vs_YZ_ShowsDifference()
        {
            // Arrange
            var anglesXY = new System.Collections.Generic.List<Vector3>();
            var anglesYZ = new System.Collections.Generic.List<Vector3>();

            // Act
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f; // 0, 45, 90, 135, 180, 225, 270, 315度
                anglesXY.Add(BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.XY));
                anglesYZ.Add(BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ));
            }

            // Assert & Debug
            Debug.Log("=== XY面 vs YZ面の比較 ===");
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Debug.Log($"{angle:000}度 - XY: {anglesXY[i]} vs YZ: {anglesYZ[i]}");
                
                // XY面とYZ面で異なるベクトルになることを確認（0度と180度は同じでも良い）
                if (angle == 90f || angle == 270f)
                {
                    Assert.IsFalse(anglesXY[i] == anglesYZ[i], $"XY面とYZ面で同じベクトルになってはいけません（{angle}度）");
                }
            }
        }
    }
}