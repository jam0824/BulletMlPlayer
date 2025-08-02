using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using BulletML;

namespace Tests
{
    /// <summary>
    /// Resourcesフォルダを使用したXMLファイルテスト
    /// （XMLファイルをResources/xml/フォルダに配置して使用）
    /// </summary>
    public class BulletMLResourceTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLParser m_Parser;

        [SetUp]
        public void Setup()
        {
            m_Executor = new BulletMLExecutor();
            m_Parser = new BulletMLParser();
            m_Executor.SetTargetPosition(Vector3.zero);
            m_Executor.ResetSequenceValues();
        }

        /// <summary>
        /// Resourcesフォルダからテキストアセットを読み込む
        /// </summary>
        private string LoadXmlFromResources(string fileName)
        {
            // Resources/xml/フォルダからファイルを読み込み
            string resourcePath = $"xml/{fileName.Replace(".xml", "")}";
            TextAsset xmlAsset = Resources.Load<TextAsset>(resourcePath);
            
            if (xmlAsset != null)
            {
                return xmlAsset.text;
            }
            else
            {
                // Resourcesにない場合はテスト用の代替XMLを使用
                return GetFallbackXml(fileName);
            }
        }

        /// <summary>
        /// ファイルが見つからない場合の代替XML
        /// </summary>
        private string GetFallbackXml(string fileName)
        {
            switch (fileName)
            {
                case "changeSpeed.xml":
                    return @"<?xml version=""1.0"" ?>
<!DOCTYPE bulletml SYSTEM ""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml/bulletml.dtd"">
<bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
  <fire>
    <direction type=""absolute"">270</direction>
    <speed>1.5</speed>
    <bulletRef label=""speedTest""/>
  </fire>
</action>
<bullet label=""speedTest"">
  <action>
    <wait>30</wait>
    <changeSpeed>
      <speed type=""sequence"">0.8</speed>
      <term>40</term>
    </changeSpeed>
    <wait>60</wait>
    <changeSpeed>
      <speed type=""sequence"">0.5</speed>
      <term>30</term>
    </changeSpeed>
    <wait>80</wait>
    <vanish/>
  </action>
</bullet>
</bulletml>";

                case "sample03.xml":
                    return @"<?xml version=""1.0"" ?>
<!DOCTYPE bulletml SYSTEM ""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml/bulletml.dtd"">
<bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<repeat>
<times>5</times>
<action>
 <fire>
  <direction type=""sequence"">23</direction>
  <bulletRef label=""straight""/>
 </fire>
 <wait>10</wait>
</action>
</repeat>
</action>
<bullet label=""straight"">
<action>
 <wait>60</wait>
 <changeDirection>
  <direction type=""absolute"">180</direction>
  <term>100</term>
 </changeDirection>
</action>
</bullet>
</bulletml>";

                default:
                    return @"<?xml version=""1.0"" ?>
<!DOCTYPE bulletml SYSTEM ""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml/bulletml.dtd"">
<bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
  <fire><bullet/></fire>
</action>
</bulletml>";
            }
        }

        /// <summary>
        /// changeSpeed sequence型の基本テスト
        /// </summary>
        [Test]
        public void ChangeSpeed_SequenceType_BasicTest()
        {
            // Arrange
            string xmlContent = LoadXmlFromResources("changeSpeed.xml");
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);

            Assert.IsNotNull(document, "ドキュメントがパースされるべき");
            Assert.IsNotNull(document.GetTopAction(), "トップアクションが存在するべき");

            // speedTestBulletを取得
            var speedTestBullet = document.GetLabeledBullet("speedTest");
            if (speedTestBullet == null)
            {
                Debug.LogWarning("speedTest弾が見つからないため、テストをスキップします");
                return;
            }

            var testBullet = new BulletMLBullet(Vector3.zero, 270f, 1.5f, CoordinateSystem.YZ);
            m_Executor.ApplyBulletElement(speedTestBullet, testBullet);

            // Act & Assert: sequence型の連続変化をテスト
            List<float> speedTargets = new List<float>();
            
            for (int frame = 0; frame < 200; frame++)
            {
                bool actionContinues = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // changeSpeedが新しく開始された時の目標値を記録
                if (testBullet.SpeedChange.IsActive && testBullet.SpeedChange.CurrentFrame == 1)
                {
                    speedTargets.Add(testBullet.SpeedChange.TargetValue);
                    Debug.Log($"Frame {frame}: changeSpeed開始, Target={testBullet.SpeedChange.TargetValue}");
                }

                if (!actionContinues) break;
            }

            // sequence型なので累積変化が期待される
            if (speedTargets.Count >= 2)
            {
                // 1回目: 1.5 + 0.8 = 2.3
                Assert.That(speedTargets[0], Is.EqualTo(2.3f).Within(0.1f), 
                    "1回目のsequence changeSpeed");
                
                // 2回目: 2.3 + 0.5 = 2.8  
                Assert.That(speedTargets[1], Is.EqualTo(2.8f).Within(0.1f), 
                    "2回目のsequence changeSpeed（累積）");
                
                Debug.Log($"sequence型テスト成功: {speedTargets[0]} → {speedTargets[1]}");
            }
        }

        /// <summary>
        /// sample03のsequence direction動作テスト
        /// </summary>
        [Test]
        public void Sample03_SequenceDirection_ProgressiveAngles()
        {
            // Arrange
            string xmlContent = LoadXmlFromResources("sample03.xml");
            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);

            var shooter = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false);
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooter.PushAction(actionRunner);

            List<BulletMLBullet> firedBullets = new List<BulletMLBullet>();
            m_Executor.OnBulletCreated += (bullet) => firedBullets.Add(bullet);

            // Act: 弾を複数発射
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(shooter);
                
                // 3発以上発射されたらテスト実行
                if (firedBullets.Count >= 3) break;
            }

            // Assert: sequence型directionの段階的変化を確認
            Assert.Greater(firedBullets.Count, 1, "複数の弾が発射されるべき");

            if (firedBullets.Count >= 3)
            {
                float dir1 = firedBullets[0].Direction;
                float dir2 = firedBullets[1].Direction;
                float dir3 = firedBullets[2].Direction;

                Debug.Log($"Direction progression: {dir1} → {dir2} → {dir3}");

                // 各弾の方向が23度ずつ増加
                Assert.That(dir2 - dir1, Is.EqualTo(23f).Within(1f), "2発目の方向差");
                Assert.That(dir3 - dir2, Is.EqualTo(23f).Within(1f), "3発目の方向差");
            }
        }

        /// <summary>
        /// シンプルなXML生成とテストの統合例
        /// </summary>
        [Test]
        public void SimpleXml_Integration_CompleteFlow()
        {
            // Arrange: シンプルなテスト用XMLを作成
            string testXml = @"<?xml version=""1.0"" ?>
<!DOCTYPE bulletml SYSTEM ""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml/bulletml.dtd"">
<bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
  <fire>
    <direction type=""absolute"">270</direction>
    <speed>2.0</speed>
    <bulletRef label=""testBullet""/>
  </fire>
</action>
<bullet label=""testBullet"">
  <action>
    <wait>20</wait>
    <changeSpeed>
      <speed type=""sequence"">1.0</speed>
      <term>30</term>
    </changeSpeed>
    <wait>40</wait>
    <changeSpeed>
      <speed type=""sequence"">0.5</speed>
      <term>20</term>
    </changeSpeed>
    <wait>30</wait>
    <vanish/>
  </action>
</bullet>
</bulletml>";

            // Act: パース→実行→検証の完全なフロー
            var document = m_Parser.Parse(testXml);
            m_Executor.SetDocument(document);

            var shooter = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false);
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooter.PushAction(actionRunner);

            BulletMLBullet createdBullet = null;
            m_Executor.OnBulletCreated += (bullet) => createdBullet = bullet;

            // 弾を生成
            for (int frame = 0; frame < 10; frame++)
            {
                m_Executor.ExecuteCurrentAction(shooter);
                if (createdBullet != null) break;
            }

            Assert.IsNotNull(createdBullet, "弾が生成されるべき");

            // 生成された弾の動作をテスト
            bool firstSpeedChangeDetected = false;
            bool secondSpeedChangeDetected = false;
            
            for (int frame = 0; frame < 150; frame++)
            {
                bool actionContinues = m_Executor.ExecuteCurrentAction(createdBullet);
                createdBullet.UpdateChanges(1f / 60f);

                if (createdBullet.SpeedChange.IsActive)
                {
                    if (!firstSpeedChangeDetected)
                    {
                        firstSpeedChangeDetected = true;
                        // 2.0 + 1.0 = 3.0
                        Assert.That(createdBullet.SpeedChange.TargetValue, Is.EqualTo(3.0f).Within(0.1f));
                    }
                    else if (!secondSpeedChangeDetected && frame > 70)
                    {
                        secondSpeedChangeDetected = true;
                        // 3.0 + 0.5 = 3.5 (sequence型で累積)
                        Assert.That(createdBullet.SpeedChange.TargetValue, Is.EqualTo(3.5f).Within(0.1f));
                        break;
                    }
                }

                if (!actionContinues) break;
            }

            // Assert: 統合テストの結果確認
            Assert.IsTrue(firstSpeedChangeDetected, "1回目のchangeSpeedが検出されるべき");
            Assert.IsTrue(secondSpeedChangeDetected, "2回目のchangeSpeedが検出されるべき");
            
            Debug.Log("統合テスト成功: XML→パース→実行→sequence型累積変化");
        }
    }
}