using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLYZAimTests
    {
        [Test]
        public void YZ_AimCalculation_NegativeZ_Returns270Degrees()
        {
            // Arrange: YZ面でZ軸負方向のベクトル
            Vector3 toTarget = new Vector3(0f, 0f, -10f);
            
            // Act: 角度計算（BulletMLExecutorの実装と同じ）
            float angle = Mathf.Atan2(toTarget.z, toTarget.y) * Mathf.Rad2Deg;
            
            // Assert
            Debug.Log($"YZ面でtoTarget={toTarget} -> 角度={angle}度");
            Assert.AreEqual(-90f, angle, 0.001f, "Z軸負方向は-90度（または270度）であるべき");
        }

        [Test]
        public void YZ_AimCalculation_PositiveZ_Returns90Degrees()
        {
            // Arrange: YZ面でZ軸正方向のベクトル
            Vector3 toTarget = new Vector3(0f, 0f, 10f);
            
            // Act: 角度計算
            float angle = Mathf.Atan2(toTarget.z, toTarget.y) * Mathf.Rad2Deg;
            
            // Assert
            Debug.Log($"YZ面でtoTarget={toTarget} -> 角度={angle}度");
            Assert.AreEqual(90f, angle, 0.001f, "Z軸正方向は90度であるべき");
        }

        [Test]
        public void YZ_AimCalculation_NegativeY_Returns180Degrees()
        {
            // Arrange: YZ面でY軸負方向のベクトル
            Vector3 toTarget = new Vector3(0f, -10f, 0f);
            
            // Act: 角度計算
            float angle = Mathf.Atan2(toTarget.z, toTarget.y) * Mathf.Rad2Deg;
            
            // Assert
            Debug.Log($"YZ面でtoTarget={toTarget} -> 角度={angle}度");
            Assert.AreEqual(180f, Mathf.Abs(angle), 0.001f, "Y軸負方向は180度（または-180度）であるべき");
        }

        [Test]
        public void YZ_AimCalculation_PositiveY_Returns0Degrees()
        {
            // Arrange: YZ面でY軸正方向のベクトル
            Vector3 toTarget = new Vector3(0f, 10f, 0f);
            
            // Act: 角度計算
            float angle = Mathf.Atan2(toTarget.z, toTarget.y) * Mathf.Rad2Deg;
            
            // Assert
            Debug.Log($"YZ面でtoTarget={toTarget} -> 角度={angle}度");
            Assert.AreEqual(0f, angle, 0.001f, "Y軸正方向は0度であるべき");
        }

        [Test]
        public void YZ_ConvertAngleToVector_MinusNinety_ReturnsNegativeZ()
        {
            // Arrange: -90度の角度
            float angle = -90f;
            
            // Act: 角度からベクトルに変換（BulletMLBullet.ConvertAngleToVectorと同じ）
            float angleRadians = angle * Mathf.Deg2Rad;
            Vector3 vector = new Vector3(0f, Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            
            // Assert
            Debug.Log($"YZ面で角度{angle}度 -> ベクトル={vector}");
            Assert.AreEqual(0f, vector.y, 0.001f, "Y成分は0であるべき");
            Assert.AreEqual(-1f, vector.z, 0.001f, "Z成分は-1であるべき");
        }
    }
}