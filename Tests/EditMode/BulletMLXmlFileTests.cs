using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using BulletML;

namespace Tests
{
    /// <summary>
    /// 実際のXMLファイルを使用したBulletMLテスト
    /// </summary>
    public class BulletMLXmlFileTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLDocument m_Document;
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
        /// XMLファイルを読み込む共通メソッド
        /// </summary>
        private string LoadXmlFile(string fileName)
        {
            string filePath = Path.Combine(Application.dataPath, "Script", "xml", fileName);
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                Assert.Fail($"XMLファイルが見つかりません: {filePath}");
                return null;
            }
        }

        /// <summary>
        /// sample03.xmlの読み込みとパースのテスト
        /// </summary>
        [Test]
        public void Sample03_LoadAndParse_Success()
        {
            // Arrange & Act
            string xmlContent = LoadXmlFile("sample03.xml");
            m_Document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(m_Document);

            // Assert
            Assert.IsNotNull(m_Document, "ドキュメントが正しくパースされるべき");
            Assert.AreEqual(BulletMLType.vertical, m_Document.Type, "型がverticalであるべき");
            Assert.IsNotNull(m_Document.GetTopAction(), "トップアクションが存在するべき");
            Assert.IsNotNull(m_Document.GetLabeledBullet("straight"), "straightラベルの弾が存在するべき");
        }

        /// <summary>
        /// sample03.xmlのsequence型direction動作テスト
        /// </summary>
        [Test]
        public void Sample03_SequenceDirection_WorksCorrectly()
        {
            // Arrange
            string xmlContent = LoadXmlFile("sample03.xml");
            m_Document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(m_Document);

            var shooter = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false);
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooter.PushAction(actionRunner);

            List<BulletMLBullet> firedBullets = new List<BulletMLBullet>();
            m_Executor.OnBulletCreated += (bullet) => firedBullets.Add(bullet);

            // Act: 複数フレーム実行して弾を生成
            for (int frame = 0; frame < 50; frame++)
            {
                m_Executor.ExecuteCurrentAction(shooter);
            }

            // Assert
            Assert.Greater(firedBullets.Count, 0, "弾が生成されるべき");
            
            // sequence型なので、各弾の方向が23度ずつ増加しているはず
            if (firedBullets.Count >= 2)
            {
                float firstDirection = firedBullets[0].Direction;
                float secondDirection = firedBullets[1].Direction;
                
                // 方向の差が約23度であることを確認（誤差を考慮）
                float directionDiff = Mathf.Abs(secondDirection - firstDirection);
                Assert.That(directionDiff, Is.EqualTo(23f).Within(0.1f), 
                    $"sequence型で方向差が23度であるべき: first={firstDirection}, second={secondDirection}");
            }
        }

        /// <summary>
        /// changeSpeed.xmlの基本機能テスト
        /// </summary>
        [Test]
        public void ChangeSpeed_BasicTypes_WorkCorrectly()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeSpeed.xml");
            m_Document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(m_Document);

            // Act & Assert: XMLが正しくパースされることを確認
            Assert.IsNotNull(m_Document, "changeSpeed.xmlが正しくパースされるべき");
            Assert.IsNotNull(m_Document.GetLabeledBullet("speedTest1"), "speedTest1弾が存在するべき");
            Assert.IsNotNull(m_Document.GetLabeledBullet("speedTest2"), "speedTest2弾が存在するべき");
            Assert.IsNotNull(m_Document.GetLabeledBullet("speedTest3"), "speedTest3弾が存在するべき");
        }

        /// <summary>
        /// changeSpeed.xmlのsequence型速度変化テスト
        /// </summary>
        [Test]
        public void ChangeSpeed_SequenceType_CumulativeChange()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeSpeed.xml");
            m_Document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(m_Document);

            // speedTest3弾を直接テスト
            var speedTest3Bullet = m_Document.GetLabeledBullet("speedTest3");
            Assert.IsNotNull(speedTest3Bullet, "speedTest3弾が存在するべき");

            var testBullet = new BulletMLBullet(Vector3.zero, 270f, 1.5f, CoordinateSystem.YZ);
            m_Executor.ApplyBulletElement(speedTest3Bullet, testBullet);

            // Act: フレームを進めてchangeSpeedを実行
            bool firstChangeSpeedExecuted = false;
            bool secondChangeSpeedExecuted = false;
            int changeSpeedCount = 0;
            
            for (int frame = 0; frame < 200; frame++)
            {
                bool actionContinues = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // changeSpeedが新しく開始された時を検出
                if (testBullet.SpeedChange.IsActive && testBullet.SpeedChange.CurrentFrame == 1)
                {
                    changeSpeedCount++;
                    
                    if (changeSpeedCount == 1 && !firstChangeSpeedExecuted)
                    {
                        firstChangeSpeedExecuted = true;
                        Debug.Log($"Frame {frame}: 1回目のchangeSpeed実行中, TargetValue={testBullet.SpeedChange.TargetValue}");
                        
                        // 1回目の確認: 1.5 + 0.8 = 2.3
                        Assert.That(testBullet.SpeedChange.TargetValue, Is.EqualTo(2.3f).Within(0.1f),
                            "1回目のsequence changeSpeed");
                    }
                    else if (changeSpeedCount == 2 && firstChangeSpeedExecuted && !secondChangeSpeedExecuted)
                    {
                        secondChangeSpeedExecuted = true;
                        Debug.Log($"Frame {frame}: 2回目のchangeSpeed実行中, TargetValue={testBullet.SpeedChange.TargetValue}");
                        
                        // 2回目の確認: 2.3 + 0.5 = 2.8
                        Assert.That(testBullet.SpeedChange.TargetValue, Is.EqualTo(2.8f).Within(0.1f),
                            "sequence型でターゲット速度が累積されるべき");
                        break;
                    }
                }

                if (!actionContinues) break;
            }

            Assert.IsTrue(firstChangeSpeedExecuted, "1回目のchangeSpeedが実行されるべき");
            Assert.IsTrue(secondChangeSpeedExecuted, "2回目のchangeSpeedが実行されるべき");
        }

        /// <summary>
        /// sequenceSpeedTest.xmlの詳細テスト
        /// </summary>
        [Test]
        public void SequenceSpeedTest_PositiveSequence_CorrectProgression()
        {
            // Arrange
            string xmlContent = LoadXmlFile("sequenceSpeedTest.xml");
            m_Document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(m_Document);

            var positiveSequenceBullet = m_Document.GetLabeledBullet("positiveSequence");
            Assert.IsNotNull(positiveSequenceBullet, "positiveSequence弾が存在するべき");

            var testBullet = new BulletMLBullet(Vector3.zero, 260f, 1.0f, CoordinateSystem.YZ);
            m_Executor.ApplyBulletElement(positiveSequenceBullet, testBullet);

            // Act & Assert: 段階的な速度変化を確認
            List<float> speedProgression = new List<float>();
            
            for (int frame = 0; frame < 300; frame++)
            {
                bool actionContinues = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // changeSpeedが開始された時の目標速度を記録
                if (testBullet.SpeedChange.IsActive && testBullet.SpeedChange.CurrentFrame == 1)
                {
                    speedProgression.Add(testBullet.SpeedChange.TargetValue);
                    Debug.Log($"Frame {frame}: 新しいchangeSpeed開始, Target={testBullet.SpeedChange.TargetValue}");
                }

                if (!actionContinues) break;
            }

            // 段階的な増加を確認: 1.0 → 1.3 → 1.8 → 2.5 → 3.5 → 5.0
            Assert.Greater(speedProgression.Count, 2, "複数のchangeSpeedが実行されるべき");
            
            if (speedProgression.Count >= 3)
            {
                Assert.That(speedProgression[0], Is.EqualTo(1.3f).Within(0.1f), "1回目: 1.0 + 0.3 = 1.3");
                Assert.That(speedProgression[1], Is.EqualTo(1.8f).Within(0.1f), "2回目: 1.3 + 0.5 = 1.8");
                Assert.That(speedProgression[2], Is.EqualTo(2.5f).Within(0.1f), "3回目: 1.8 + 0.7 = 2.5");
            }
        }

        /// <summary>
        /// changeSpeedAdvanced.xmlの複合機能テスト
        /// </summary>
        [Test]
        public void ChangeSpeedAdvanced_ComplexBullet_CombinedEffects()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeSpeedAdvanced.xml");
            m_Document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(m_Document);

            var complexBullet = m_Document.GetLabeledBullet("complexSpeedBullet");
            Assert.IsNotNull(complexBullet, "complexSpeedBullet弾が存在するべき");

            var testBullet = new BulletMLBullet(Vector3.zero, 225f, 0.8f, CoordinateSystem.YZ);
            m_Executor.ApplyBulletElement(complexBullet, testBullet);

            // Act: 複合効果の動作を確認
            bool accelActivated = false;
            bool changeSpeedActivated = false;
            bool sequenceChangeSpeedActivated = false;

            for (int frame = 0; frame < 250; frame++)
            {
                bool actionContinues = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive) accelActivated = true;
                if (testBullet.SpeedChange.IsActive) changeSpeedActivated = true;
                
                // sequence型のchangeSpeedを検出
                if (testBullet.SpeedChange.IsActive && frame > 100)
                {
                    sequenceChangeSpeedActivated = true;
                }

                if (!actionContinues) break;
            }

            // Assert: 複合機能が正しく動作することを確認
            Assert.IsTrue(accelActivated, "加速度が適用されるべき");
            Assert.IsTrue(changeSpeedActivated, "速度変更が適用されるべき");
            Assert.IsTrue(sequenceChangeSpeedActivated, "sequence型速度変更が適用されるべき");
        }

        /// <summary>
        /// XMLファイルの存在確認テスト
        /// </summary>
        [Test]
        public void XmlFiles_Existence_AllFilesPresent()
        {
            // 作成したXMLファイルが全て存在することを確認
            string[] expectedFiles = {
                "sample03.xml",
                "changeSpeed.xml",
                "changeSpeedAdvanced.xml",
                "sequenceSpeedTest.xml"
            };

            foreach (string fileName in expectedFiles)
            {
                string filePath = Path.Combine(Application.dataPath, "Script", "xml", fileName);
                Assert.IsTrue(File.Exists(filePath), $"XMLファイルが存在するべき: {fileName}");
            }
        }

        /// <summary>
        /// 全XMLファイルのパース成功テスト
        /// </summary>
        [Test]
        public void AllXmlFiles_Parse_Successfully()
        {
            string[] xmlFiles = {
                "changeSpeed.xml",
                "changeSpeedAdvanced.xml",
                "sequenceSpeedTest.xml"
            };

            foreach (string fileName in xmlFiles)
            {
                // Arrange & Act
                string xmlContent = LoadXmlFile(fileName);
                var document = m_Parser.Parse(xmlContent);

                // Assert
                Assert.IsNotNull(document, $"{fileName}が正しくパースされるべき");
                Assert.IsNotNull(document.RootElement, $"{fileName}のルート要素が存在するべき");
                Assert.IsNotNull(document.GetTopAction(), $"{fileName}のトップアクションが存在するべき");
            }
        }
    }
}