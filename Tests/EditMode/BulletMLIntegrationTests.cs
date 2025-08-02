using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLIntegrationTests
    {
        private BulletMLParser m_Parser;
        private BulletMLExecutor m_Executor;

        [SetUp]
        public void Setup()
        {
            m_Parser = new BulletMLParser();
            m_Executor = new BulletMLExecutor();
        }

        [Test]
        public void ParseAndExecute_SimpleFire_Works()
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

            // Act
            var document = m_Parser.Parse(simpleFireXml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            // アクションを実行
            bool result = m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.IsNotNull(document);
            Assert.IsNotNull(topAction);
            Assert.AreEqual("top", topAction.Label);
            Assert.IsTrue(bullet.IsActive);
        }

        [Test]
        public void ParseAndExecute_DirectionAndSpeed_SetsCorrectValues()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<fire>
<direction type=""absolute"">90</direction>
<speed>3</speed>
<bullet/>
</fire>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            
            var fireElement = topAction.GetChild(BulletMLElementType.fire);
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(90f, newBullets[0].Direction, 0.001f);
            Assert.AreEqual(3f, newBullets[0].Speed, 0.001f);
        }

        [Test]
        public void ParseAndExecute_WaitCommand_SetsWaitFrames()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<wait>30</wait>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            bool result = m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(30, actionRunner.WaitFrames);
        }

        [Test]
        public void ParseAndExecute_RepeatCommand_CreatesMultipleActions()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<repeat>
<times>3</times>
<action>
<wait>5</wait>
</action>
</repeat>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            // repeat実行
            bool result = m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.IsTrue(result);
            // repeatによって3つのアクションがスタックに積まれているはず
            Assert.AreEqual(4, bullet.ActionStack.Count); // 元のtop + repeat用の3つ
        }

        [Test]
        public void ParseAndExecute_BulletRef_ResolvesCorrectly()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<fire>
<bulletRef label=""testBullet""/>
</fire>
</action>
<bullet label=""testBullet"">
<direction>270</direction>
<speed>2.5</speed>
</bullet>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            
            var fireElement = topAction.GetChild(BulletMLElementType.fire);
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(270f, newBullets[0].Direction, 0.001f);
            Assert.AreEqual(2.5f, newBullets[0].Speed, 0.001f);
        }

        [Test]
        public void ParseAndExecute_ChangeDirection_InitializesCorrectly()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<changeDirection>
<direction type=""absolute"">180</direction>
<term>60</term>
</changeDirection>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            bool result = m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(bullet.DirectionChange.IsActive);
            Assert.AreEqual(0f, bullet.DirectionChange.StartValue, 0.001f);
            Assert.AreEqual(180f, bullet.DirectionChange.TargetValue, 0.001f);
            Assert.AreEqual(60, bullet.DirectionChange.Duration);
        }

        [Test]
        public void ParseAndExecute_VerticalType_SetsCorrectCoordinateSystem()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
<action label=""top"">
<fire><bullet/></fire>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            // Assert
            Assert.AreEqual(BulletMLType.vertical, document.Type);
            Assert.AreEqual(CoordinateSystem.YZ, m_Executor.CoordinateSystem);
        }

        [Test]
        public void ParseAndExecute_HorizontalType_SetsCorrectCoordinateSystem()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml type=""horizontal"">
<action label=""top"">
<fire><bullet/></fire>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            // Assert
            Assert.AreEqual(BulletMLType.horizontal, document.Type);
            Assert.AreEqual(CoordinateSystem.XY, m_Executor.CoordinateSystem);
        }

        [Test]
        public void BulletUpdate_WithDirectionChange_GraduallyChangesDirection()
        {
            // Arrange
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f);
            
            // 90度に30フレームかけて変更
            bullet.DirectionChange.StartValue = 0f;
            bullet.DirectionChange.TargetValue = 90f;
            bullet.DirectionChange.Duration = 30;
            bullet.DirectionChange.CurrentFrame = 0;
            bullet.DirectionChange.IsActive = true;

            // Act & Assert
            // 開始時
            Assert.AreEqual(0f, bullet.Direction, 0.001f);

            // 15フレーム後（半分）
            for (int i = 0; i < 15; i++)
            {
                bullet.Update(1f / 60f); // 1/60秒 = 1フレーム
            }
            Assert.AreEqual(45f, bullet.Direction, 1f); // 誤差考慮

            // 30フレーム後（完了）
            for (int i = 0; i < 15; i++)
            {
                bullet.Update(1f / 60f);
            }
            Assert.AreEqual(90f, bullet.Direction, 0.001f);
            Assert.IsFalse(bullet.DirectionChange.IsActive);
        }
    }
}