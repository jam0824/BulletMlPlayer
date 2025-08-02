using NUnit.Framework;
using BulletML;
using UnityEngine;
using System.Collections.Generic;

namespace BulletMLTests
{
    public class BulletMLDirectionTests
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
        public void FireWithoutDirection_FliesUpward()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<fire>
<bullet/>
</fire>
</action>
</bulletml>";

            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            var fireElement = topAction.GetChild(BulletMLElementType.fire);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(0f, newBullets[0].Direction, 0.001f); // 上方向（0度）
            Debug.Log($"direction省略時の方向: {newBullets[0].Direction}度");
        }

        [Test]
        public void FireWithAimDirection_AimsAtTarget()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<fire>
<direction type=""aim"">0</direction>
<bullet/>
</fire>
</action>
</bulletml>";

            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            // ターゲット位置を設定（右側）
            m_Executor.SetTargetPosition(new Vector3(10f, 0f, 0f));

            var topAction = document.GetTopAction();
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            var fireElement = topAction.GetChild(BulletMLElementType.fire);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(90f, newBullets[0].Direction, 0.001f); // 右方向（90度）
            Debug.Log($"aim type での方向: {newBullets[0].Direction}度");
        }

        [Test]
        public void FireWithAbsoluteDirection_UsesSpecifiedDirection()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<fire>
<direction type=""absolute"">180</direction>
<bullet/>
</fire>
</action>
</bulletml>";

            var document = m_Parser.Parse(xml);
            m_Executor.SetDocument(document);

            var topAction = document.GetTopAction();
            var sourceBullet = new BulletMLBullet(Vector3.zero, 0f, 1f);

            var fireElement = topAction.GetChild(BulletMLElementType.fire);

            // Act
            var newBullets = m_Executor.ExecuteFireCommand(fireElement, sourceBullet);

            // Assert
            Assert.AreEqual(1, newBullets.Count);
            Assert.AreEqual(180f, newBullets[0].Direction, 0.001f); // 下方向（180度）
            Debug.Log($"absolute type での方向: {newBullets[0].Direction}度");
        }

        [Test]
        public void DirectionTypes_Comparison()
        {
            // Arrange
            var targetPosition = new Vector3(5f, 5f, 0f); // 右上
            m_Executor.SetTargetPosition(targetPosition);

            var sourceBullet = new BulletMLBullet(Vector3.zero, 30f, 1f); // 30度方向

            // direction要素なし
            var fireNone = new BulletMLElement(BulletMLElementType.fire);
            var bulletNone = new BulletMLElement(BulletMLElementType.bullet);
            fireNone.AddChild(bulletNone);

            // aim type
            var fireAim = new BulletMLElement(BulletMLElementType.fire);
            var directionAim = new BulletMLElement(BulletMLElementType.direction, null, "0");
            directionAim.AddAttribute("type", "aim");
            var bulletAim = new BulletMLElement(BulletMLElementType.bullet);
            fireAim.AddChild(directionAim);
            fireAim.AddChild(bulletAim);

            // relative type
            var fireRelative = new BulletMLElement(BulletMLElementType.fire);
            var directionRelative = new BulletMLElement(BulletMLElementType.direction, null, "45");
            directionRelative.AddAttribute("type", "relative");
            var bulletRelative = new BulletMLElement(BulletMLElementType.bullet);
            fireRelative.AddChild(directionRelative);
            fireRelative.AddChild(bulletRelative);

            // Act
            var bulletsNone = m_Executor.ExecuteFireCommand(fireNone, sourceBullet);
            var bulletsAim = m_Executor.ExecuteFireCommand(fireAim, sourceBullet);
            var bulletsRelative = m_Executor.ExecuteFireCommand(fireRelative, sourceBullet);

            // Assert & Debug
            Debug.Log("=== 方向タイプ比較 ===");
            Debug.Log($"direction省略: {bulletsNone[0].Direction}度 (期待値: 30度 - ソース弾の方向)");
            Debug.Log($"aim type: {bulletsAim[0].Direction}度 (期待値: 45度 - ターゲット方向)");
            Debug.Log($"relative type: {bulletsRelative[0].Direction}度 (期待値: 75度 - 30+45度)");

            Assert.AreEqual(30f, bulletsNone[0].Direction, 0.001f); // ソース弾の方向
            Assert.AreEqual(45f, bulletsAim[0].Direction, 0.001f); // ターゲット方向
            Assert.AreEqual(75f, bulletsRelative[0].Direction, 0.001f); // 30 + 45
        }
    }
}