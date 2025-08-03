using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using BulletML;

namespace BulletMLTests
{
    /// <summary>
    /// 角度オフセット機能のテスト
    /// </summary>
    [TestFixture]
    public class BulletMLAngleOffsetTests
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
        /// 角度オフセット0の場合、元の値と同じになることを確認
        /// </summary>
        [Test]
        public void AngleOffset_DefaultValue_NoChange()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""absolute"">180</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 0.0f; // デフォルト値
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var fireElement = document.GetTopAction().Children[0];

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count, "弾が1発生成されるはず");
            Assert.AreEqual(180f, newBullets[0].Direction, 1e-5f, "角度オフセット0では元の値と同じになるはず");
        }

        /// <summary>
        /// absolute type: 角度オフセット90度の場合、元の値+90度になることを確認
        /// </summary>
        [Test]
        public void AngleOffset_AbsoluteType_AddsOffset()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""absolute"">180</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 90.0f; // 90度オフセット
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var fireElement = document.GetTopAction().Children[0];

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count, "弾が1発生成されるはず");
            Assert.AreEqual(270f, newBullets[0].Direction, 1e-5f, "180 + 90 = 270度になるはず");
        }

        /// <summary>
        /// relative type: 角度オフセットが適用されることを確認
        /// </summary>
        [Test]
        public void AngleOffset_RelativeType_AddsOffset()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""relative"">30</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 45.0f; // 45度オフセット
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 90f, 1f); // 元の方向90度
            var fireElement = document.GetTopAction().Children[0];

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count, "弾が1発生成されるはず");
            Assert.AreEqual(165f, newBullets[0].Direction, 1e-5f, "90 + 30 + 45 = 165度になるはず");
        }

        /// <summary>
        /// sequence type: 角度オフセットが適用されることを確認
        /// </summary>
        [Test]
        public void AngleOffset_SequenceType_AddsOffset()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""sequence"">10</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 30.0f; // 30度オフセット
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var fireElement = document.GetTopAction().Children[0];

            // Act - 1回目
            var newBullets1 = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);
            
            // Act - 2回目
            var newBullets2 = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets1.Count, "1回目: 弾が1発生成されるはず");
            Assert.AreEqual(1, newBullets2.Count, "2回目: 弾が1発生成されるはず");
            
            Assert.AreEqual(40f, newBullets1[0].Direction, 1e-5f, "1回目: 0 + 10 + 30 = 40度になるはず");
            Assert.AreEqual(50f, newBullets2[0].Direction, 1e-5f, "2回目: 10 + 10 + 30 = 50度になるはず");
        }

        /// <summary>
        /// aim type: 角度オフセットが適用されることを確認
        /// </summary>
        [Test]
        public void AngleOffset_AimType_AddsOffset()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""aim"">15</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.SetTargetPosition(new Vector3(0f, 1f, 0f)); // 上方向にターゲット
            m_Executor.AngleOffset = 60.0f; // 60度オフセット
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var fireElement = document.GetTopAction().Children[0];

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count, "弾が1発生成されるはず");
            // aim=0度（上方向）+ 15度 + 60度オフセット = 75度
            Assert.AreEqual(75f, newBullets[0].Direction, 1e-5f, "0 + 15 + 60 = 75度になるはず");
        }

        /// <summary>
        /// 360度を超える場合の正規化確認
        /// </summary>
        [Test]
        public void AngleOffset_Over360_Normalized()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""absolute"">300</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 120.0f; // 120度オフセット
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var fireElement = document.GetTopAction().Children[0];

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count, "弾が1発生成されるはず");
            // 300 + 120 = 420度 → 420 - 360 = 60度に正規化
            Assert.AreEqual(60f, newBullets[0].Direction, 1e-5f, "300 + 120 = 420度は60度に正規化されるはず");
        }

        /// <summary>
        /// 小数の角度オフセットが適用されることを確認
        /// </summary>
        [Test]
        public void AngleOffset_DecimalValue_Applied()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <fire>
            <direction type=""absolute"">100</direction>
            <bullet/>
        </fire>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 22.5f; // 22.5度オフセット（小数）
            
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var fireElement = document.GetTopAction().Children[0];

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count, "弾が1発生成されるはず");
            Assert.AreEqual(122.5f, newBullets[0].Direction, 1e-5f, "100 + 22.5 = 122.5度になるはず");
        }

        /// <summary>
        /// changeDirectionコマンドでも角度オフセットが適用されることを確認
        /// </summary>
        [Test]
        public void AngleOffset_ChangeDirection_Applied()
        {
            // Arrange
            string xmlContent = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
    <action label=""top"">
        <changeDirection>
            <direction type=""absolute"">90</direction>
            <term>1</term>
        </changeDirection>
    </action>
</bulletml>";
            
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            m_Executor.AngleOffset = 45.0f; // 45度オフセット
            
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionElement = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(actionElement);
            bullet.PushAction(actionRunner);

            // Act
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            var directionChange = bullet.DirectionChange;
            Assert.IsTrue(directionChange.IsActive, "方向変更が開始されているはず");
            // 90 + 45 = 135度
            Assert.AreEqual(135f, directionChange.TargetValue, 1e-5f, "ターゲット方向は90 + 45 = 135度になるはず");
        }
    }
}