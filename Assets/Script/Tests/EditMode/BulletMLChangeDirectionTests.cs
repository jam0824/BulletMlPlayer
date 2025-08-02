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
    /// changeDirection機能専用テスト
    /// </summary>
    public class BulletMLChangeDirectionTests
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
        /// 基本的なchangeDirection（absolute型）のテスト
        /// </summary>
        [Test]
        public void ChangeDirection_AbsoluteType_CorrectTargetDirection()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("directionTest1");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            bool changeDirectionDetected = false;
            float targetDirection = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    changeDirectionDetected = true;
                    targetDirection = testBullet.DirectionChange.TargetValue;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(changeDirectionDetected, "changeDirectionが実行されるべき");
            Assert.AreEqual(90f, targetDirection, 0.1f, "absolute型でターゲット方向が90度であるべき");
        }

        /// <summary>
        /// changeDirection（relative型）のテスト
        /// </summary>
        [Test]
        public void ChangeDirection_RelativeType_CorrectTargetDirection()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("directionTest2");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 30f, 2.5f, CoordinateSystem.YZ, true  // 初期方向30度
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            bool changeDirectionDetected = false;
            float targetDirection = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    changeDirectionDetected = true;
                    targetDirection = testBullet.DirectionChange.TargetValue;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(changeDirectionDetected, "changeDirectionが実行されるべき");
            Assert.AreEqual(75f, targetDirection, 0.1f, "relative型で30+45=75度であるべき");
        }

        /// <summary>
        /// changeDirection（aim型）のテスト
        /// </summary>
        [Test]
        public void ChangeDirection_AimType_TargetsPlayer()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("directionTest3");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                new Vector3(0f, 5f, 0f), 0f, 3.0f, CoordinateSystem.YZ, true
            );

            // プレイヤー位置を設定（弾から見て下に）
            m_Executor.SetTargetPosition(new Vector3(0f, -5f, 0f));

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            bool changeDirectionDetected = false;
            float targetDirection = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    changeDirectionDetected = true;
                    targetDirection = testBullet.DirectionChange.TargetValue;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(changeDirectionDetected, "changeDirectionが実行されるべき");
            // プレイヤーが下にいるので、180度（下向き）を狙うべき
            Assert.AreEqual(180f, targetDirection, 5f, "aim型でプレイヤー方向を狙うべき");
        }

        /// <summary>
        /// sequence型changeDirectionの段階的変化テスト
        /// </summary>
        [Test]
        public void ChangeDirection_SequenceType_StepwiseChange()
        {
            // Arrange
            string xmlContent = LoadXmlFile("sequenceDirectionTest.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("sequenceAlternating");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            List<float> targetDirections = new List<float>();

            // Act
            for (int frame = 0; frame < 300; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    targetDirections.Add(testBullet.DirectionChange.TargetValue);
                    Debug.Log($"Frame {frame}: changeDirection、ターゲット: {testBullet.DirectionChange.TargetValue}");
                }
            }

            // Assert
            Assert.AreEqual(4, targetDirections.Count, "4回のchangeDirectionが実行されるべき");
            Assert.AreEqual(30f, targetDirections[0], 0.1f, "1回目: 0 + 30 = 30");
            Assert.AreEqual(-15f, targetDirections[1], 0.1f, "2回目: 30 + (-45) = -15");
            Assert.AreEqual(45f, targetDirections[2], 0.1f, "3回目: -15 + 60 = 45");
            Assert.AreEqual(-30f, targetDirections[3], 0.1f, "4回目: 45 + (-75) = -30");
        }

        /// <summary>
        /// 複数回のchangeDirection段階的変化テスト
        /// </summary>
        [Test]
        public void ChangeDirection_MultipleStages_CorrectSequence()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("directionTest5");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.8f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            List<float> targetDirections = new List<float>();
            List<string> directionTypes = new List<string>();

            // Act
            for (int frame = 0; frame < 200; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    targetDirections.Add(testBullet.DirectionChange.TargetValue);
                    Debug.Log($"Frame {frame}: changeDirection #{targetDirections.Count}、ターゲット: {testBullet.DirectionChange.TargetValue}");
                }
            }

            // Assert
            Assert.AreEqual(3, targetDirections.Count, "3回のchangeDirectionが実行されるべき");
            Assert.AreEqual(90f, targetDirections[0], 0.1f, "1回目: absolute 90度");
            Assert.AreEqual(30f, targetDirections[1], 0.1f, "2回目: relative -60度 (90-60=30)");
            // 3回目はaim型なので、プレイヤー位置により変動
        }

        /// <summary>
        /// changeDirection + changeSpeedの複合テスト
        /// </summary>
        [Test]
        public void ChangeDirection_WithChangeSpeed_BothActive()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("directionSpeedTest");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.3f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            bool hasDirectionChange = false;
            bool hasSpeedChange = false;
            bool simultaneouslyActive = false;

            // Act
            for (int frame = 0; frame < 200; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive) hasDirectionChange = true;
                if (testBullet.SpeedChange.IsActive) hasSpeedChange = true;

                // 同時に両方がアクティブかチェック
                if (testBullet.DirectionChange.IsActive && testBullet.SpeedChange.IsActive)
                {
                    simultaneouslyActive = true;
                }
            }

            // Assert
            Assert.IsTrue(hasDirectionChange, "changeDirectionが実行されるべき");
            Assert.IsTrue(hasSpeedChange, "changeSpeedが実行されるべき");
            Assert.IsTrue(simultaneouslyActive, "changeDirectionとchangeSpeedが同時に動作するべき");
        }

        /// <summary>
        /// 高速連続changeDirectionのテスト
        /// </summary>
        [Test]
        public void ChangeDirection_RapidSequence_AllExecuted()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirection.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("rapidDirectionTest");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.4f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);


            List<float> targetDirections = new List<float>();

            // Act
            for (int frame = 0; frame < 150; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    targetDirections.Add(testBullet.DirectionChange.TargetValue);
                    Debug.Log($"Frame {frame}: 高速changeDirection #{targetDirections.Count}、ターゲット: {testBullet.DirectionChange.TargetValue}");
                }
            }

            // Assert
            Assert.AreEqual(4, targetDirections.Count, "4回の高速changeDirectionが実行されるべき");
            Assert.AreEqual(20f, targetDirections[0], 0.1f, "1回目: 0 + 20 = 20");
            Assert.AreEqual(45f, targetDirections[1], 0.1f, "2回目: 20 + 25 = 45");
            Assert.AreEqual(15f, targetDirections[2], 0.1f, "3回目: 45 + (-30) = 15");
            Assert.AreEqual(55f, targetDirections[3], 0.1f, "4回目: 15 + 40 = 55");
        }

        /// <summary>
        /// 振り子型changeDirectionのテスト
        /// </summary>
        [Test]
        public void ChangeDirection_PendulumPattern_CorrectOscillation()
        {
            // Arrange
            string xmlContent = LoadXmlFile("changeDirectionAdvanced.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("pendulumDirection");
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

                if (testBullet.DirectionChange.IsActive && testBullet.DirectionChange.CurrentFrame == 1)
                {
                    targetDirections.Add(testBullet.DirectionChange.TargetValue);
                    Debug.Log($"Frame {frame}: 振り子changeDirection #{targetDirections.Count}、ターゲット: {testBullet.DirectionChange.TargetValue}");
                }
            }

            // Assert
            Assert.AreEqual(3, targetDirections.Count, "3回の振り子changeDirectionが実行されるべき");
            Assert.AreEqual(60f, targetDirections[0], 0.1f, "1回目: relative +60度");
            Assert.AreEqual(-60f, targetDirections[1], 0.1f, "2回目: relative -120度 (60-120=-60)");
            Assert.AreEqual(60f, targetDirections[2], 0.1f, "3回目: relative +120度 (-60+120=60)");
        }
    }
}