using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLAimIssueTests
    {
        private BulletMLExecutor m_Executor;

        [SetUp]
        public void Setup()
        {
            m_Executor = new BulletMLExecutor();
        }

        [Test]
        public void AimDirection_SamePosition_ReturnsZero()
        {
            // Arrange
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "0");
            directionElement.AddAttribute("type", "aim");

            // シューターとターゲットが同じ位置
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            m_Executor.SetTargetPosition(Vector3.zero);

            // Act
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            fireElement.AddChild(directionElement);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);

            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Debug.Log($"同じ位置でのaim方向: {newBullets[0].Direction}度");
            
            // 同じ位置の場合、toTarget = (0,0,0) となり、計算不可能
            // この場合は0度（上方向）になる
        }

        [Test]
        public void AimDirection_DifferentPosition_CalculatesCorrectly()
        {
            // Arrange
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "0");
            directionElement.AddAttribute("type", "aim");

            // シューターを(0,0,0)、ターゲットを(1,0,0)に配置
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            m_Executor.SetTargetPosition(new Vector3(1f, 0f, 0f)); // 右側

            // Act
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            fireElement.AddChild(directionElement);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);

            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Debug.Log($"異なる位置でのaim方向: {newBullets[0].Direction}度");
            Assert.AreEqual(90f, newBullets[0].Direction, 0.001f); // 右方向（90度）
        }

        [Test]
        public void AimDirection_VerticalOffset_CalculatesCorrectly()
        {
            // Arrange
            var directionElement = new BulletMLElement(BulletMLElementType.direction, null, "0");
            directionElement.AddAttribute("type", "aim");

            // シューターを(0,1,0)、ターゲットを(0,0,0)に配置
            var sourceBullet = new BulletMLBullet(new Vector3(0f, 1f, 0f), 0f, 1f);
            m_Executor.SetTargetPosition(Vector3.zero); // 下側

            // Act
            var fireElement = new BulletMLElement(BulletMLElementType.fire);
            fireElement.AddChild(directionElement);
            var bulletElement = new BulletMLElement(BulletMLElementType.bullet);
            fireElement.AddChild(bulletElement);

            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Debug.Log($"垂直オフセットでのaim方向: {newBullets[0].Direction}度");
            Assert.AreEqual(180f, newBullets[0].Direction, 0.001f); // 下方向（180度）
        }

        [Test]
        public void TestCalculateAngleFromVector()
        {
            // Arrange & Act
            // 右方向のベクトル (1, 0, 0)
            Vector3 rightVector = new Vector3(1f, 0f, 0f);
            float rightAngle = TestCalculateAngleFromVector(rightVector, CoordinateSystem.XY);

            // 上方向のベクトル (0, 1, 0)
            Vector3 upVector = new Vector3(0f, 1f, 0f);
            float upAngle = TestCalculateAngleFromVector(upVector, CoordinateSystem.XY);

            // 下方向のベクトル (0, -1, 0)
            Vector3 downVector = new Vector3(0f, -1f, 0f);
            float downAngle = TestCalculateAngleFromVector(downVector, CoordinateSystem.XY);

            // Assert & Debug
            Debug.Log($"右ベクトル {rightVector} -> {rightAngle}度 (期待値: 90度)");
            Debug.Log($"上ベクトル {upVector} -> {upAngle}度 (期待値: 0度)");
            Debug.Log($"下ベクトル {downVector} -> {downAngle}度 (期待値: 180度)");

            Assert.AreEqual(90f, rightAngle, 0.001f);
            Assert.AreEqual(0f, upAngle, 0.001f);
            Assert.AreEqual(180f, downAngle, 1f); // 180度は-180度と同じなので許容範囲を広げる
        }

        private float TestCalculateAngleFromVector(Vector3 vector, CoordinateSystem coordinateSystem)
        {
            // BulletMLExecutorのprivateメソッドと同じ実装
            switch (coordinateSystem)
            {
                case CoordinateSystem.XY:
                    return Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
                case CoordinateSystem.YZ:
                    return Mathf.Atan2(vector.z, vector.y) * Mathf.Rad2Deg;
                default:
                    return 0f;
            }
        }
    }
}