using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLMovementDebugTests
    {
        [Test]
        public void YZ_TwentyThreeDegrees_CalculateExpectedMovement()
        {
            // Arrange: YZ面で23度の弾
            float angle = 23f;
            Vector3 expectedVector = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);
            
            // Expected calculation
            float angleRadians = angle * Mathf.Deg2Rad;
            float expectedX = 0f;  // YZ面ではX軸は使用しない
            float expectedY = Mathf.Cos(angleRadians);  // 縦方向（上下）
            float expectedZ = Mathf.Sin(angleRadians);  // 前後方向
            
            // Debug output
            Debug.Log($"23度のYZ面での期待ベクトル:");
            Debug.Log($"  X成分: {expectedX:F3} (期待値: 0)");
            Debug.Log($"  Y成分: {expectedY:F3} (上方向成分)");
            Debug.Log($"  Z成分: {expectedZ:F3} (前方向成分)");
            Debug.Log($"  実際の計算結果: {expectedVector}");
            
            // Assert
            Assert.AreEqual(0f, expectedVector.x, 0.001f, "X成分は0であるべき");
            Assert.AreEqual(expectedY, expectedVector.y, 0.001f, "Y成分が間違っている");
            Assert.AreEqual(expectedZ, expectedVector.z, 0.001f, "Z成分が間違っている");
            
            // 23度なので、Y成分の方がZ成分より大きいはず
            Assert.Greater(expectedVector.y, expectedVector.z, "23度ではY成分（上方向）がZ成分（前方向）より大きいはず");
        }

        [Test]
        public void YZ_SixtyNineDegrees_CalculateExpectedMovement()
        {
            // Arrange: YZ面で69度の弾
            float angle = 69f;
            Vector3 expectedVector = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);
            
            // Expected calculation
            float angleRadians = angle * Mathf.Deg2Rad;
            float expectedX = 0f;  
            float expectedY = Mathf.Cos(angleRadians);  
            float expectedZ = Mathf.Sin(angleRadians);  
            
            // Debug output
            Debug.Log($"69度のYZ面での期待ベクトル:");
            Debug.Log($"  X成分: {expectedX:F3} (期待値: 0)");
            Debug.Log($"  Y成分: {expectedY:F3} (上方向成分)");
            Debug.Log($"  Z成分: {expectedZ:F3} (前方向成分)");
            Debug.Log($"  実際の計算結果: {expectedVector}");
            
            // Assert
            Assert.AreEqual(0f, expectedVector.x, 0.001f, "X成分は0であるべき");
            Assert.AreEqual(expectedY, expectedVector.y, 0.001f, "Y成分が間違っている");
            Assert.AreEqual(expectedZ, expectedVector.z, 0.001f, "Z成分が間違っている");
            
            // 69度なので、Z成分の方がY成分より大きいはず（前方向メイン）
            Assert.Greater(expectedVector.z, expectedVector.y, "69度ではZ成分（前方向）がY成分（上方向）より大きいはず");
        }

        [Test]
        public void YZ_AngleProgression_ShowsSpiral()
        {
            // Arrange: sequence型のように23度ずつ増加する弾幕の最初の10発
            Debug.Log("YZ面での螺旋弾幕シミュレーション (23度ずつ増加):");
            
            for (int i = 0; i < 10; i++)
            {
                float angle = 23f * i; // 0, 23, 46, 69, 92, ...
                Vector3 vector = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);
                
                Debug.Log($"  弾{i+1}: {angle:F0}度 → ベクトル({vector.x:F3}, {vector.y:F3}, {vector.z:F3})");
                
                // X成分は常に0であるべき
                Assert.AreEqual(0f, vector.x, 0.001f, $"弾{i+1}のX成分が0でない");
            }
        }

        [Test]
        public void CompareXYvsYZ_SameAngle()
        {
            // Arrange: 同じ角度でXY面とYZ面を比較
            float angle = 45f;
            
            Vector3 vectorXY = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.XY);
            Vector3 vectorYZ = BulletMLBullet.ConvertAngleToVector(angle, CoordinateSystem.YZ);
            
            Debug.Log($"45度での座標系比較:");
            Debug.Log($"  XY面: {vectorXY} (X=横, Y=縦, Z=0)");
            Debug.Log($"  YZ面: {vectorYZ} (X=0, Y=縦, Z=前後)");
            
            // XY面ではZ成分が0、YZ面ではX成分が0
            Assert.AreEqual(0f, vectorXY.z, 0.001f, "XY面のZ成分は0であるべき");
            Assert.AreEqual(0f, vectorYZ.x, 0.001f, "YZ面のX成分は0であるべき");
            
            // Y成分は両方とも同じ（縦方向）
            Assert.AreEqual(vectorXY.y, vectorYZ.y, 0.001f, "Y成分（縦方向）は両座標系で同じであるべき");
        }
    }
}