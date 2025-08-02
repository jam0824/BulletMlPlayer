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
    /// accel機能専用テスト
    /// </summary>
    public class BulletMLAccelTests
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
        /// 基本的なaccel（horizontal型のみ）のテスト
        /// </summary>
        [Test]
        public void Accel_HorizontalOnly_CorrectAcceleration()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelTest1");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool accelDetected = false;
            float horizontalAccel = 0f;
            float verticalAccel = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    accelDetected = true;
                    horizontalAccel = testBullet.AccelInfo.HorizontalAccel;
                    verticalAccel = testBullet.AccelInfo.VerticalAccel;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(accelDetected, "accelが実行されるべき");
            Assert.AreEqual(0.1f, horizontalAccel, 0.01f, "horizontal加速度が0.1であるべき");
            Assert.AreEqual(0f, verticalAccel, 0.01f, "vertical加速度が0（指定なし）であるべき");
        }

        /// <summary>
        /// accel（vertical型のみ）のテスト
        /// </summary>
        [Test]
        public void Accel_VerticalOnly_CorrectAcceleration()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelTest2");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.2f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool accelDetected = false;
            float horizontalAccel = 0f;
            float verticalAccel = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    accelDetected = true;
                    horizontalAccel = testBullet.AccelInfo.HorizontalAccel;
                    verticalAccel = testBullet.AccelInfo.VerticalAccel;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(accelDetected, "accelが実行されるべき");
            Assert.AreEqual(0f, horizontalAccel, 0.01f, "horizontal加速度が0（指定なし）であるべき");
            Assert.AreEqual(0.15f, verticalAccel, 0.01f, "vertical加速度が0.15であるべき");
        }

        /// <summary>
        /// accel（horizontal + vertical同時）のテスト
        /// </summary>
        [Test]
        public void Accel_BothDirections_CorrectAcceleration()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelTest3");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.5f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool accelDetected = false;
            float horizontalAccel = 0f;
            float verticalAccel = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    accelDetected = true;
                    horizontalAccel = testBullet.AccelInfo.HorizontalAccel;
                    verticalAccel = testBullet.AccelInfo.VerticalAccel;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(accelDetected, "accelが実行されるべき");
            Assert.AreEqual(0.12f, horizontalAccel, 0.01f, "horizontal加速度が0.12であるべき");
            Assert.AreEqual(-0.08f, verticalAccel, 0.01f, "vertical加速度が-0.08であるべき");
        }

        /// <summary>
        /// accel（relative型）のテスト
        /// </summary>
        [Test]
        public void Accel_RelativeType_CorrectAcceleration()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelTest4");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.3f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool accelDetected = false;
            float horizontalAccel = 0f;
            float verticalAccel = 0f;

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    accelDetected = true;
                    horizontalAccel = testBullet.AccelInfo.HorizontalAccel;
                    verticalAccel = testBullet.AccelInfo.VerticalAccel;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(accelDetected, "accelが実行されるべき");
            Assert.AreEqual(0.05f, horizontalAccel, 0.01f, "relative型でhorizontal加速度が0.05であるべき");
            Assert.AreEqual(0.1f, verticalAccel, 0.01f, "relative型でvertical加速度が0.1であるべき");
        }

        /// <summary>
        /// sequence型accelの段階的変化テスト
        /// </summary>
        [Test]
        public void Accel_SequenceType_StepwiseChange()
        {
            // Arrange
            string xmlContent = LoadXmlFile("sequenceAccelTest.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("sequenceAlternating");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.0f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            List<float> horizontalAccels = new List<float>();
            List<float> verticalAccels = new List<float>();

            // Act
            for (int frame = 0; frame < 350; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    horizontalAccels.Add(testBullet.AccelInfo.HorizontalAccel);
                    verticalAccels.Add(testBullet.AccelInfo.VerticalAccel);
                    Debug.Log($"Frame {frame}: accel、Horizontal: {testBullet.AccelInfo.HorizontalAccel}, Vertical: {testBullet.AccelInfo.VerticalAccel}");
                }
            }

            // Assert
            Assert.AreEqual(4, horizontalAccels.Count, "4回のaccelが実行されるべき");
            Assert.AreEqual(0.08f, horizontalAccels[0], 0.01f, "1回目: 0 + 0.08 = 0.08");
            Assert.AreEqual(-0.04f, horizontalAccels[1], 0.01f, "2回目: 0.08 + (-0.12) = -0.04");
            Assert.AreEqual(0.06f, horizontalAccels[2], 0.01f, "3回目: -0.04 + 0.1 = 0.06");
            Assert.AreEqual(0f, horizontalAccels[3], 0.01f, "4回目: 0.06 + (-0.06) = 0");

            Assert.AreEqual(0.06f, verticalAccels[0], 0.01f, "1回目: 0 + 0.06 = 0.06");
            Assert.AreEqual(0.02f, verticalAccels[1], 0.01f, "2回目: 0.06 + (-0.04) = 0.02");
            Assert.AreEqual(0.1f, verticalAccels[2], 0.01f, "3回目: 0.02 + 0.08 = 0.1");
            Assert.AreEqual(0f, verticalAccels[3], 0.01f, "4回目: 0.1 + (-0.1) = 0");
        }

        /// <summary>
        /// 複数段階accelのテスト
        /// </summary>
        [Test]
        public void Accel_MultipleStages_CorrectSequence()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelTest6");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.4f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            List<float> horizontalAccels = new List<float>();
            List<float> verticalAccels = new List<float>();

            // Act
            for (int frame = 0; frame < 250; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    horizontalAccels.Add(testBullet.AccelInfo.HorizontalAccel);
                    verticalAccels.Add(testBullet.AccelInfo.VerticalAccel);
                    Debug.Log($"Frame {frame}: accel #{horizontalAccels.Count}、Horizontal: {testBullet.AccelInfo.HorizontalAccel}, Vertical: {testBullet.AccelInfo.VerticalAccel}");
                }
            }

            // Assert
            Assert.AreEqual(3, horizontalAccels.Count, "3回のaccelが実行されるべき");
            Assert.AreEqual(0.1f, horizontalAccels[0], 0.01f, "1回目: absolute 0.1");
            Assert.AreEqual(-0.15f, horizontalAccels[1], 0.01f, "2回目: absolute -0.15");
            // 3回目はrelative型なので現在値により変動
        }

        /// <summary>
        /// accel + changeDirectionの複合テスト
        /// </summary>
        [Test]
        public void Accel_WithChangeDirection_BothActive()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelDirectionTest");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.6f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool hasAccel = false;
            bool hasDirectionChange = false;
            bool simultaneouslyActive = false;

            // Act
            for (int frame = 0; frame < 200; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive) hasAccel = true;
                if (testBullet.DirectionChange.IsActive) hasDirectionChange = true;

                // 同時に両方がアクティブかチェック
                if (testBullet.AccelInfo.IsActive && testBullet.DirectionChange.IsActive)
                {
                    simultaneouslyActive = true;
                }
            }

            // Assert
            Assert.IsTrue(hasAccel, "accelが実行されるべき");
            Assert.IsTrue(hasDirectionChange, "changeDirectionが実行されるべき");
            Assert.IsTrue(simultaneouslyActive, "accelとchangeDirectionが同時に動作するべき");
        }

        /// <summary>
        /// accel + changeSpeedの複合テスト
        /// </summary>
        [Test]
        public void Accel_WithChangeSpeed_BothActive()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("accelSpeedTest");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.2f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            bool hasAccel = false;
            bool hasSpeedChange = false;
            bool simultaneouslyActive = false;

            // Act
            for (int frame = 0; frame < 200; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive) hasAccel = true;
                if (testBullet.SpeedChange.IsActive) hasSpeedChange = true;

                // 同時に両方がアクティブかチェック
                if (testBullet.AccelInfo.IsActive && testBullet.SpeedChange.IsActive)
                {
                    simultaneouslyActive = true;
                }
            }

            // Assert
            Assert.IsTrue(hasAccel, "accelが実行されるべき");
            Assert.IsTrue(hasSpeedChange, "changeSpeedが実行されるべき");
            Assert.IsTrue(simultaneouslyActive, "accelとchangeSpeedが同時に動作するべき");
        }

        /// <summary>
        /// 短期間高強度accelのテスト
        /// </summary>
        [Test]
        public void Accel_ShortTermHighIntensity_CorrectValues()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accel.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("shortAccelTest");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 2.8f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            List<float> horizontalAccels = new List<float>();
            List<float> verticalAccels = new List<float>();

            // Act
            for (int frame = 0; frame < 100; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    horizontalAccels.Add(testBullet.AccelInfo.HorizontalAccel);
                    verticalAccels.Add(testBullet.AccelInfo.VerticalAccel);
                    Debug.Log($"Frame {frame}: 短期間accel #{horizontalAccels.Count}、Horizontal: {testBullet.AccelInfo.HorizontalAccel}, Vertical: {testBullet.AccelInfo.VerticalAccel}");
                }
            }

            // Assert
            Assert.AreEqual(2, horizontalAccels.Count, "2回の短期間accelが実行されるべき");
            Assert.AreEqual(0.3f, horizontalAccels[0], 0.01f, "1回目: 高強度horizontal 0.3");
            Assert.AreEqual(-0.2f, horizontalAccels[1], 0.01f, "2回目: 逆方向horizontal -0.2");
            Assert.AreEqual(0.25f, verticalAccels[0], 0.01f, "1回目: 高強度vertical 0.25");
            Assert.AreEqual(-0.2f, verticalAccels[1], 0.01f, "2回目: 逆方向vertical -0.2");
        }

        /// <summary>
        /// 波型accelパターンのテスト
        /// </summary>
        [Test]
        public void Accel_WavePattern_CorrectOscillation()
        {
            // Arrange
            string xmlContent = LoadXmlFile("accelAdvanced.xml");
            var document = m_Parser.Parse(xmlContent);
            var bullet = document.GetLabeledBullet("waveAccel");
            var topAction = bullet.GetChild(BulletMLElementType.action);

            var testBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1.5f, CoordinateSystem.YZ, true
            );

            var actionRunner = new BulletMLActionRunner(topAction);
            testBullet.PushAction(actionRunner);

            List<float> horizontalAccels = new List<float>();

            // Act
            for (int frame = 0; frame < 300; frame++)
            {
                m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(1f / 60f);

                if (testBullet.AccelInfo.IsActive && testBullet.AccelInfo.CurrentFrame == 1)
                {
                    horizontalAccels.Add(testBullet.AccelInfo.HorizontalAccel);
                    Debug.Log($"Frame {frame}: 波型accel #{horizontalAccels.Count}、Horizontal: {testBullet.AccelInfo.HorizontalAccel}");
                }
            }

            // Assert
            Assert.AreEqual(4, horizontalAccels.Count, "4回の波型accelが実行されるべき");
            Assert.AreEqual(0.15f, horizontalAccels[0], 0.01f, "1回目: 右に加速 +0.15");
            Assert.AreEqual(-0.2f, horizontalAccels[1], 0.01f, "2回目: 左に加速 -0.2");
            Assert.AreEqual(0.18f, horizontalAccels[2], 0.01f, "3回目: 再び右に +0.18");
            Assert.AreEqual(-0.25f, horizontalAccels[3], 0.01f, "4回目: 強く左に -0.25");
        }
    }
}