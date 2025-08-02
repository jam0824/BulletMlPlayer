using NUnit.Framework;
using BulletML;
using UnityEngine;
using System.Collections.Generic;

namespace BulletMLTests
{
    public class BulletMLCirclePatternTests
    {
        private BulletMLParser m_Parser;
        private BulletMLExecutor m_Executor;
        private List<BulletMLBullet> m_CreatedBullets;

        [SetUp]
        public void Setup()
        {
            m_Parser = new BulletMLParser();
            m_Executor = new BulletMLExecutor();
            m_CreatedBullets = new List<BulletMLBullet>();
            
            // 弾生成のコールバックを設定
            m_Executor.OnBulletCreated = OnBulletCreated;
        }

        private void OnBulletCreated(BulletMLBullet _bullet)
        {
            m_CreatedBullets.Add(_bullet);
        }

        [Test]
        public void CirclePattern_CreatesMultipleBullets()
        {
            // Arrange
            string circleXml = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
<action label=""top"">
<repeat>
<times>36</times>
<action>
 <fire>
  <direction type=""sequence"">10</direction>
  <speed>2</speed>
  <bullet/>
 </fire>
 <wait>1</wait>
</action>
</repeat>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(circleXml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false); // 非表示のシューター弾
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            // カウンターをリセット
            m_CreatedBullets.Clear();

            // repeatコマンドを実行してすべてのアクションを完了させる
            bool hasMoreActions = true;
            while (bullet.GetCurrentAction() != null && hasMoreActions)
            {
                hasMoreActions = m_Executor.ExecuteCurrentAction(bullet);
            }

            // Assert
            Assert.IsTrue(hasMoreActions || bullet.GetCurrentAction() == null, "All actions should complete successfully");
            
            // デバッグログ
            Debug.Log($"ActionStack.Count: {bullet.ActionStack.Count}");
            Debug.Log($"CreatedBullets.Count: {m_CreatedBullets.Count}");
            
            // 作成された弾の数をチェック（期待値36個）
            Assert.AreEqual(36, m_CreatedBullets.Count); // XMLで指定された弾の数
        }

        [Test]
        public void CirclePattern_ExecuteMultipleSteps_CreatesBullets()
        {
            // Arrange
            string circleXml = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
<action label=""top"">
<repeat>
<times>3</times>
<action>
 <fire>
  <direction type=""sequence"">30</direction>
  <speed>2</speed>
  <bullet/>
 </fire>
 <wait>1</wait>
</action>
</repeat>
</action>
</bulletml>";

            var document = m_Parser.Parse(circleXml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false); // 非表示のシューター弾
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            // カウンターをリセット
            m_CreatedBullets.Clear();

            // Act
            // repeatを実行
            m_Executor.ExecuteCurrentAction(bullet);

            // 最初のfireアクションを実行
            m_Executor.ExecuteCurrentAction(bullet);

            // Assert
            Assert.AreEqual(1, m_CreatedBullets.Count);
            Assert.AreEqual(30f, m_CreatedBullets[0].Direction, 0.001f); // 最初の弾は30度（sequence開始値）
            Assert.AreEqual(2f, m_CreatedBullets[0].Speed, 0.001f);
        }

        [Test]
        public void CirclePattern_SequenceDirection_IncrementsCorrectly()
        {
            // Arrange
            string circleXml = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
<action label=""top"">
<repeat>
<times>3</times>
<action>
 <fire>
  <direction type=""sequence"">60</direction>
  <speed>1</speed>
  <bullet/>
 </fire>
</action>
</repeat>
</action>
</bulletml>";

            var document = m_Parser.Parse(circleXml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false); // 非表示のシューター弾
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            // カウンターをリセット
            m_CreatedBullets.Clear();

            // Act - すべてのアクションを完全実行
            while (bullet.GetCurrentAction() != null)
            {
                bool hasMoreActions = m_Executor.ExecuteCurrentAction(bullet);
                if (!hasMoreActions) break;
            }

            // Assert
            Assert.AreEqual(3, m_CreatedBullets.Count);
            
            // 1発目: 60度（sequence開始値）
            Assert.AreEqual(60f, m_CreatedBullets[0].Direction, 0.001f);
            
            // 2発目: 60 + 60 = 120度
            Assert.AreEqual(120f, m_CreatedBullets[1].Direction, 0.001f);
            
            // 3発目: 120 + 60 = 180度
            Assert.AreEqual(180f, m_CreatedBullets[2].Direction, 0.001f);
        }

        [Test]
        public void CirclePattern_WithWait_CreatesCorrectTiming()
        {
            // Arrange
            string circleXml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<repeat>
<times>2</times>
<action>
 <fire>
  <direction type=""sequence"">90</direction>
  <bullet/>
 </fire>
 <wait>5</wait>
</action>
</repeat>
</action>
</bulletml>";

            var document = m_Parser.Parse(circleXml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var bullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false); // 非表示のシューター弾
            var actionRunner = new BulletMLActionRunner(topAction);
            bullet.PushAction(actionRunner);

            // カウンターをリセット
            m_CreatedBullets.Clear();

            // Act - すべてのアクションを完全実行
            while (bullet.GetCurrentAction() != null)
            {
                bool hasMoreActions = m_Executor.ExecuteCurrentAction(bullet);
                if (!hasMoreActions) break;
            }

            // Assert
            Assert.AreEqual(2, m_CreatedBullets.Count);
            Assert.AreEqual(90f, m_CreatedBullets[0].Direction, 0.001f); // 1発目: 90度（sequence開始値）
            Assert.AreEqual(180f, m_CreatedBullets[1].Direction, 0.001f); // 2発目: 90 + 90 = 180度
        }
    }
}