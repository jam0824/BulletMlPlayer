using NUnit.Framework;
using UnityEngine;
using BulletML;

namespace BulletMLTests
{
    public class BulletMLSpeedMultiplierTests
    {
        [Test]
        public void Executor_AppliesSpeedMultiplier_ToNewBullets()
        {
            var executor = new BulletMLExecutor();
            executor.SetCoordinateSystem(CoordinateSystem.XY);
            executor.SpeedMultiplier = 2.0f; // 倍率2

            var shooter = new BulletMLBullet(Vector3.zero, 0f, 0f, CoordinateSystem.XY, false);

            // fire要素を用意（速度3の弾を発射）
            var fire = new BulletMLElement(BulletMLElementType.fire);
            var speed = new BulletMLElement(BulletMLElementType.speed, null, "3");
            var bullet = new BulletMLElement(BulletMLElementType.bullet);
            fire.AddChild(speed);
            fire.AddChild(bullet);

            var created = executor.ExecuteFireCommand(fire, shooter);
            Assert.AreEqual(1, created.Count);

            // 位置更新で実効速度が2倍になっているかを確認
            var b = created[0];
            float initialY = b.Position.y;
            b.Update(1.0f); // dt=1秒、0度=上向き
            float movedY = b.Position.y - initialY;

            // 速度3 × 倍率2 = 6 なので、1秒で約6上昇するはず
            Assert.That(movedY, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void Player_PropagatesSpeedMultiplier_ToExecutor()
        {
            var go = new GameObject("BulletMlPlayerTest");
            var player = go.AddComponent<BulletMlPlayer>();

            // privateフィールドだが、公開セッターで設定
            player.SetSpeedMultiplier(1.5f);

            // 初期化を呼ぶ
            var initMethod = typeof(BulletMlPlayer).GetMethod("InitializeSystem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            initMethod.Invoke(player, null);

            // XML最小構成をロード
            string xml = "<bulletml xmlns=\"http://www.asahi-net.or.jp/~cs8k-cyu/bulletml\"><action label=\"top\"><fire><speed>2</speed><bullet/></fire></action></bulletml>";
            player.LoadBulletML(xml);

            // Executorに倍率が反映されたかを確認するため、リフレクションで取得
            var execField = typeof(BulletMlPlayer).GetField("m_Executor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var executor = (BulletMLExecutor)execField.GetValue(player);

            // SpeedMultiplierプロパティ値を確認
            Assert.AreEqual(1.5f, executor.SpeedMultiplier, 0.0001f);

            Object.DestroyImmediate(go);
        }
    }
}


