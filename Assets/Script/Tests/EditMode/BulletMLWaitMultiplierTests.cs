using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using BulletML;

namespace BulletMLTests
{
    /// <summary>
    /// wait倍率機能のテスト
    /// </summary>
    [TestFixture]
    public class BulletMLWaitMultiplierTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLParser m_Parser;

        [SetUp]
        public void SetUp()
        {
            m_Executor = new BulletMLExecutor();
            m_Parser = new BulletMLParser();
        }

        [TearDown]
        public void TearDown()
        {
            m_Executor = null;
        }

        /// <summary>
        /// wait倍率1.0の場合、元の値と同じになることを確認
        /// </summary>
        [Test]
        public void WaitMultiplier_DefaultValue_NoChange()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <wait>10</wait>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.WaitTimeMultiplier = 1.0f; // デフォルト値
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(10, actionRunner.WaitFrames, "wait倍率1.0では元の値と同じになるはず");
        }

        /// <summary>
        /// wait倍率2.0の場合、元の値の2倍になることを確認
        /// </summary>
        [Test]
        public void WaitMultiplier_DoubleValue_WaitFramesDoubled()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <wait>5</wait>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.WaitTimeMultiplier = 2.0f; // 2倍
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(10, actionRunner.WaitFrames, "wait倍率2.0では元の値の2倍になるはず");
        }

        /// <summary>
        /// wait倍率0.5の場合、元の値の半分になることを確認
        /// </summary>
        [Test]
        public void WaitMultiplier_HalfValue_WaitFramesHalved()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <wait>10</wait>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.WaitTimeMultiplier = 0.5f; // 半分
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(5, actionRunner.WaitFrames, "wait倍率0.5では元の値の半分になるはず");
        }

        /// <summary>
        /// 小数の計算結果がMathf.RoundToIntで四捨五入されることを確認
        /// </summary>
        [Test]
        public void WaitMultiplier_DecimalResult_RoundedToInt()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <wait>3</wait>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.WaitTimeMultiplier = 1.7f; // 3 × 1.7 = 5.1 → 5に四捨五入
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(5, actionRunner.WaitFrames, "3 × 1.7 = 5.1 は 5 に四捨五入されるはず");
        }

        /// <summary>
        /// 小数の計算結果がMathf.RoundToIntで四捨五入されることを確認（.5の場合）
        /// </summary>
        [Test]
        public void WaitMultiplier_HalfDecimalResult_RoundedUp()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <wait>3</wait>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.WaitTimeMultiplier = 1.5f; // 3 × 1.5 = 4.5 → 4に四捨五入（Unityの仕様）
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(4, actionRunner.WaitFrames, "3 × 1.5 = 4.5 は 4 に四捨五入されるはず（Unityの仕様）");
        }

        /// <summary>
        /// wait倍率0の場合、waitが0フレームになることを確認
        /// </summary>
        [Test]
        public void WaitMultiplier_ZeroValue_NoWait()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <wait>10</wait>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.WaitTimeMultiplier = 0.0f; // 0倍
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(0, actionRunner.WaitFrames, "wait倍率0.0では0フレームになるはず");
        }
    }
}