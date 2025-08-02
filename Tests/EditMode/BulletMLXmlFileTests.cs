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
                "sequenceSpeedTest.xml",
                "changeDirection.xml",
                "changeDirectionAdvanced.xml",
                "sequenceDirectionTest.xml",
                "accel.xml",
                "accelAdvanced.xml",
                "sequenceAccelTest.xml"
            };

            foreach (string fileName in expectedFiles)
            {
                string filePath = Path.Combine(Application.dataPath, "Script", "xml", fileName);
                Assert.IsTrue(File.Exists(filePath), $"XMLファイルが存在するべき: {fileName}");
            }
        }

        /// <summary>
        /// changeDirection.xmlの読み込みとパースのテスト
        /// </summary>
        [Test]
        public void ChangeDirection_LoadAndParse_Success()
        {
            // Arrange & Act
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);

            // Assert
            Assert.IsNotNull(document, "changeDirection.xmlが正しくパースされるべき");
            Assert.IsNotNull(document.RootElement, "ルート要素が存在するべき");
            Assert.IsNotNull(document.GetTopAction(), "トップアクションが存在するべき");
            
            // 弾の定義が存在することを確認
            var directionTest1 = document.GetLabeledBullet("directionTest1");
            Assert.IsNotNull(directionTest1, "directionTest1弾が定義されているべき");
        }

        /// <summary>
        /// changeDirection sequence型の累積変化テスト
        /// </summary>
        [Test]
        public void ChangeDirection_SequenceType_CumulativeChange()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("directionTest4");
            var topAction = bullet.GetChild(BulletMLElementType.action); // action要素

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            int changeDirectionCount = 0;
            float firstChangeTargetDirection = 0f;
            float secondChangeTargetDirection = 0f;

            // Act & Assert
            for (int frame = 0; frame < 200; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // changeDirectionコマンドの実行を検出
                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    changeDirectionCount++;
                    if (changeDirectionCount == 1)
                    {
                        // 1回目のchangeDirection: sequence型で+30度
                        firstChangeTargetDirection = testBullet.DirectionChange.TargetValue;
                        Debug.Log($"Frame {frame}: 1回目のchangeDirection、ターゲット方向: {firstChangeTargetDirection}");
                        // 初期方向0度 + 30度 = 30度
                        Assert.AreEqual(30f, firstChangeTargetDirection, 0.1f, "1回目のsequence changeDirection");
                    }
                    else if (changeDirectionCount == 2)
                    {
                        // 2回目のchangeDirection: sequence型で+45度（累積）
                        secondChangeTargetDirection = testBullet.DirectionChange.TargetValue;
                        Debug.Log($"Frame {frame}: 2回目のchangeDirection、ターゲット方向: {secondChangeTargetDirection}");
                        // 前回結果30度 + 45度 = 75度
                        Assert.AreEqual(75f, secondChangeTargetDirection, 0.1f, "sequence型でターゲット方向が累積されるべき");
                        break;
                    }
                }
            }

            Assert.AreEqual(2, changeDirectionCount, "2回のchangeDirectionが実行されるべき");
        }

        /// <summary>
        /// sequenceDirectionTest.xmlの詳細テスト
        /// </summary>
        [Test]
        public void SequenceDirectionTest_DetailedTest_Success()
        {
            // Arrange
            string xmlContent = LoadXmlFile("sequenceDirectionTest.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("sequencePositive");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            List<float> targetDirections = new List<float>();

            // Act
            for (int frame = 0; frame < 250; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // changeDirectionの開始フレームを検出
                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    targetDirections.Add(testBullet.DirectionChange.TargetValue);
                    Debug.Log($"Frame {frame}: changeDirection開始、ターゲット: {testBullet.DirectionChange.TargetValue}");
                }
            }

            // Assert
            Assert.AreEqual(3, targetDirections.Count, "3回のchangeDirectionが実行されるべき");
            Assert.AreEqual(15f, targetDirections[0], 0.1f, "1回目: 0 + 15 = 15");
            Assert.AreEqual(35f, targetDirections[1], 0.1f, "2回目: 15 + 20 = 35");
            Assert.AreEqual(60f, targetDirections[2], 0.1f, "3回目: 35 + 25 = 60");
        }

        /// <summary>
        /// changeDirectionAdvanced.xmlの複合機能テスト
        /// </summary>
        [Test]
        public void ChangeDirectionAdvanced_ComplexFeatures_Success()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirectionAdvanced.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("fullComboTest");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            bool hasDirectionChange = false;
            bool hasSpeedChange = false;
            bool hasAccel = false;

            // Act
            for (int frame = 0; frame < 300; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive) hasDirectionChange = true;
                if (testBullet.SpeedChange.IsActive) hasSpeedChange = true;
                if (testBullet.AccelInfo.IsActive) hasAccel = true;

                // 全ての機能が同時に動作することを確認
                if (hasDirectionChange && hasSpeedChange && hasAccel)
                {
                    break;
                }
            }

            // Assert
            Assert.IsTrue(hasDirectionChange, "changeDirectionが実行されるべき");
            Assert.IsTrue(hasSpeedChange, "changeSpeedが実行されるべき");
            Assert.IsTrue(hasAccel, "accelが実行されるべき");
        }

        /// <summary>
        /// accel.xmlの読み込みとパースのテスト
        /// </summary>
        [Test]
        public void Accel_LoadAndParse_Success()
        {
            // Arrange & Act
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);

            // Assert
            Assert.IsNotNull(document, "accel.xmlが正しくパースされるべき");
            Assert.IsNotNull(document.RootElement, "ルート要素が存在するべき");
            Assert.IsNotNull(document.GetTopAction(), "トップアクションが存在するべき");
            
            // 弾の定義が存在することを確認
            var accelTest1 = document.GetLabeledBullet("accelTest1");
            Assert.IsNotNull(accelTest1, "accelTest1弾が定義されているべき");
        }

        /// <summary>
        /// accel sequence型の累積変化テスト
        /// </summary>
        [Test]
        public void Accel_SequenceType_CumulativeChange()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelTest5");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            int accelCount = 0;
            float firstHorizontalTarget = 0f;
            float firstVerticalTarget = 0f;
            float secondHorizontalTarget = 0f;
            float secondVerticalTarget = 0f;

            // Act & Assert
            for (int frame = 0; frame < 200; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // accelコマンドの実行を検出
                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    accelCount++;
                    if (accelCount == 1)
                    {
                        // 1回目のaccel: sequence型で+0.08, +0.06
                        firstHorizontalTarget = testBullet.AccelInfo.HorizontalAccel;
                        firstVerticalTarget = testBullet.AccelInfo.VerticalAccel;
                        Debug.Log($"Frame {frame}: 1回目のaccel、Horizontal: {firstHorizontalTarget}, Vertical: {firstVerticalTarget}");
                        // 初期値0 + 0.08 = 0.08, 0 + 0.06 = 0.06
                        Assert.AreEqual(0.08f, firstHorizontalTarget, 0.01f, "1回目のsequence accel horizontal");
                        Assert.AreEqual(0.06f, firstVerticalTarget, 0.01f, "1回目のsequence accel vertical");
                    }
                    else if (accelCount == 2)
                    {
                        // 2回目のaccel: sequence型で+0.04, -0.03（累積）
                        secondHorizontalTarget = testBullet.AccelInfo.HorizontalAccel;
                        secondVerticalTarget = testBullet.AccelInfo.VerticalAccel;
                        Debug.Log($"Frame {frame}: 2回目のaccel、Horizontal: {secondHorizontalTarget}, Vertical: {secondVerticalTarget}");
                        // 前回結果0.08 + 0.04 = 0.12, 0.06 + (-0.03) = 0.03
                        Assert.AreEqual(0.12f, secondHorizontalTarget, 0.01f, "sequence型でhorizontal加速度が累積されるべき");
                        Assert.AreEqual(0.03f, secondVerticalTarget, 0.01f, "sequence型でvertical加速度が累積されるべき");
                        break;
                    }
                }
            }

            Assert.AreEqual(2, accelCount, "2回のaccelが実行されるべき");
        }

        /// <summary>
        /// sequenceAccelTest.xmlの詳細テスト
        /// </summary>
        [Test]
        public void SequenceAccelTest_DetailedTest_Success()
        {
            // Arrange
            string xmlContent = LoadXmlFile("sequenceAccelTest.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("sequenceBothDirections");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            List<float> horizontalTargets = new List<float>();
            List<float> verticalTargets = new List<float>();

            // Act
            for (int frame = 0; frame < 250; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                // accelの開始フレームを検出
                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    horizontalTargets.Add(testBullet.AccelInfo.HorizontalAccel);
                    verticalTargets.Add(testBullet.AccelInfo.VerticalAccel);
                    Debug.Log($"Frame {frame}: accel開始、Horizontal: {testBullet.AccelInfo.HorizontalAccel}, Vertical: {testBullet.AccelInfo.VerticalAccel}");
                }
            }

            // Assert
            Assert.AreEqual(3, horizontalTargets.Count, "3回のaccelが実行されるべき");
            Assert.AreEqual(0.05f, horizontalTargets[0], 0.01f, "1回目: 0 + 0.05 = 0.05");
            Assert.AreEqual(0.08f, horizontalTargets[1], 0.01f, "2回目: 0.05 + 0.03 = 0.08");
            Assert.AreEqual(0.16f, horizontalTargets[2], 0.01f, "3回目: 0.08 + 0.08 = 0.16");
            
            Assert.AreEqual(0.04f, verticalTargets[0], 0.01f, "1回目: 0 + 0.04 = 0.04");
            Assert.AreEqual(0.1f, verticalTargets[1], 0.01f, "2回目: 0.04 + 0.06 = 0.1");
            Assert.AreEqual(0.08f, verticalTargets[2], 0.01f, "3回目: 0.1 + (-0.02) = 0.08");
        }

        /// <summary>
        /// accelAdvanced.xmlの複合機能テスト
        /// </summary>
        [Test]
        public void AccelAdvanced_ComplexFeatures_Success()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accelAdvanced.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("fullComboAccel");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool hasAccel = false;
            bool hasDirectionChange = false;
            bool hasSpeedChange = false;

            // Act
            for (int frame = 0; frame < 300; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive) hasAccel = true;
                if (testBullet.DirectionChange.IsActive) hasDirectionChange = true;
                if (testBullet.SpeedChange.IsActive) hasSpeedChange = true;

                // 全ての機能が同時に動作することを確認
                if (hasAccel && hasDirectionChange && hasSpeedChange)
                {
                    break;
                }
            }

            // Assert
            Assert.IsTrue(hasAccel, "accelが実行されるべき");
            Assert.IsTrue(hasDirectionChange, "changeDirectionが実行されるべき");
            Assert.IsTrue(hasSpeedChange, "changeSpeedが実行されるべき");
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
                "sequenceSpeedTest.xml",
                "changeDirection.xml",
                "changeDirectionAdvanced.xml",
                "sequenceDirectionTest.xml",
                "accel.xml",
                "accelAdvanced.xml",
                "sequenceAccelTest.xml"
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