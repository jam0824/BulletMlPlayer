using NUnit.Framework;
using BulletML;
using UnityEngine;

namespace BulletMLTests
{
    public class BulletMLControlCommandTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLBullet m_Bullet;

        [SetUp]
        public void Setup()
        {
            m_Executor = new BulletMLExecutor();
            m_Bullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false); // 非表示のシューター弾
        }

        [Test]
        public void ExecuteWaitCommand_SetsWaitFrames()
        {
            // Arrange
            var waitElement = new BulletMLElement(BulletMLElementType.wait, null, "30");
            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(waitElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            
            // 独立性を確保するため新しい弾インスタンスを作成
            var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            testBullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(testBullet);

            // Assert
            Assert.IsTrue(result); // アクションが継続中
            Assert.AreEqual(30, actionRunner.WaitFrames);
        }

        [Test]
        public void ExecuteWaitCommand_WithExpression_EvaluatesCorrectly()
        {
            // Arrange
            var waitElement = new BulletMLElement(BulletMLElementType.wait, null, "20+10");
            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(waitElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            
            // 独立性を確保するため新しい弾インスタンスを作成
            var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            testBullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(testBullet);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(30, actionRunner.WaitFrames);
        }

        [Test]
        public void ExecuteVanishCommand_DeactivatesBullet()
        {
            // Arrange
            var vanishElement = new BulletMLElement(BulletMLElementType.vanish);
            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(vanishElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            
            // 独立性を確保するため新しい弾インスタンスを作成
            var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            testBullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(testBullet);

            // Assert
            Assert.IsFalse(testBullet.IsActive);
        }

        [Test]
        public void WaitFrames_DecrementOverTime()
        {
            // Arrange
            var waitElement = new BulletMLElement(BulletMLElementType.wait, null, "3");
            var actionElement = new BulletMLElement(BulletMLElementType.action);
            actionElement.AddChild(waitElement);
            
            var actionRunner = new BulletMLActionRunner(actionElement);
            
            // 独立性を確保するため新しい弾インスタンスを作成
            var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            testBullet.PushAction(actionRunner);

            // 最初の実行でwaitを設定
            m_Executor.ExecuteCurrentAction(testBullet);
            Assert.AreEqual(3, actionRunner.WaitFrames);

            // Act & Assert
            // フレーム1
            bool result1 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result1);
            Assert.AreEqual(2, actionRunner.WaitFrames);

            // フレーム2
            bool result2 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result2);
            Assert.AreEqual(1, actionRunner.WaitFrames);

            // フレーム3
            bool result3 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result3);
            Assert.AreEqual(0, actionRunner.WaitFrames);

            // フレーム4 - wait終了、次のコマンドへ
            bool result4 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsFalse(result4); // アクション終了
        }

        [Test]
        public void ExecuteRepeatCommand_RepeatsAction()
        {
            // Arrange
            var repeatElement = new BulletMLElement(BulletMLElementType.repeat);
            var timesElement = new BulletMLElement(BulletMLElementType.times, null, "3");
            var innerAction = new BulletMLElement(BulletMLElementType.action);
            var waitElement = new BulletMLElement(BulletMLElementType.wait, null, "1");
            innerAction.AddChild(waitElement);
            
            repeatElement.AddChild(timesElement);
            repeatElement.AddChild(innerAction);

            var mainAction = new BulletMLElement(BulletMLElementType.action);
            mainAction.AddChild(repeatElement);
            
            var actionRunner = new BulletMLActionRunner(mainAction);
            
            // 独立性を確保するため新しい弾インスタンスを作成
            var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            testBullet.PushAction(actionRunner);

            // Act & Assert
            // repeat開始
            bool result1 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result1);
            Assert.AreEqual(4, testBullet.ActionStack.Count); // mainAction(1) + repeat追加(3) = 4

            // 1回目のwait
            bool result2 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result2);

            // wait終了、inner action完了
            bool result3 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result3);

            // 2回目のrepeat開始
            bool result4 = m_Executor.ExecuteCurrentAction(testBullet);
            Assert.IsTrue(result4);
        }

        [Test]
        public void ExecuteRepeatCommand_ZeroTimes_SkipsRepeat()
        {
            // Arrange
            var repeatElement = new BulletMLElement(BulletMLElementType.repeat);
            var timesElement = new BulletMLElement(BulletMLElementType.times, null, "0");
            var innerAction = new BulletMLElement(BulletMLElementType.action);
            var waitElement = new BulletMLElement(BulletMLElementType.wait, null, "10");
            innerAction.AddChild(waitElement);
            
            repeatElement.AddChild(timesElement);
            repeatElement.AddChild(innerAction);

            var mainAction = new BulletMLElement(BulletMLElementType.action);
            mainAction.AddChild(repeatElement);
            
            var actionRunner = new BulletMLActionRunner(mainAction);
            
            // 独立性を確保するため新しい弾インスタンスを作成
            var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.XY, false);
            testBullet.PushAction(actionRunner);

            // Act
            bool result = m_Executor.ExecuteCurrentAction(testBullet);

            // Assert
            Assert.IsFalse(result); // アクション完了（repeatがスキップされた）
            Assert.AreEqual(0, actionRunner.WaitFrames); // waitが実行されていない
        }
    }
}