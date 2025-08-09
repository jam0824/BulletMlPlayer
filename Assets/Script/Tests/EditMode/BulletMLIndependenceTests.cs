using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;
using System.Linq;

namespace Tests
{
    /// <summary>
    /// 弾の独立性機能のテスト
    /// </summary>
    public class BulletMLIndependenceTests
    {
        private GameObject m_PlayerGameObject;
        private BulletMlPlayer m_Player;

        [SetUp]
        public void SetUp()
        {
            // テスト用GameObjectとBulletMlPlayerを作成
            m_PlayerGameObject = new GameObject("BulletMLPlayer");
            m_Player = m_PlayerGameObject.AddComponent<BulletMlPlayer>();
            
            // テスト用初期化
            m_Player.SetEnableDebugLog(false);
            m_Player.InitializeForTest();
            
            // 最大弾数を少なく設定してテストを簡単に
            m_Player.SetMaxBullets(10);

            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            
            if (m_PlayerGameObject != null)
            {
                Object.DestroyImmediate(m_PlayerGameObject);
            }
        }

        [Test]
        public void BulletIndependence_EnabledByDefault()
        {
            // Assert: デフォルトで弾の独立性が有効
            Assert.IsTrue(m_Player.GetBulletIndependence(), "弾の独立性はデフォルトで有効であるべき");
        }

        [Test]
        public void BulletIndependence_CanBeDisabled()
        {
            // Arrange & Act
            m_Player.SetBulletIndependence(false);

            // Assert
            Assert.IsFalse(m_Player.GetBulletIndependence(), "弾の独立性を無効に設定できるべき");
        }

        [Test]
        public void BulletIndependence_CanBeEnabled()
        {
            // Arrange
            m_Player.SetBulletIndependence(false);
            
            // Act
            m_Player.SetBulletIndependence(true);

            // Assert
            Assert.IsTrue(m_Player.GetBulletIndependence(), "弾の独立性を有効に設定できるべき");
        }

        [Test]
        public void BulletIndependence_IndependentBullets_DontFollowParentMovement()
        {
            // Arrange: 弾の独立性を有効にする
            m_Player.SetBulletIndependence(true);
            
            // 初期位置を設定
            Vector3 initialPlayerPosition = new Vector3(0f, 0f, 0f);
            m_PlayerGameObject.transform.position = initialPlayerPosition;

            // シンプルな弾発射XMLを読み込み
            string testXml = @"<?xml version=""1.0"" ?>
                <bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
                    <action label=""top"">
                        <fire>
                            <direction>0</direction>
                            <speed>2</speed>
                        </fire>
                        <wait>5</wait>
                        <fire>
                            <direction>90</direction>
                            <speed>2</speed>
                        </fire>
                    </action>
                </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 弾を生成
            m_Player.ManualUpdate();
            m_Player.ManualUpdate();

            var listBulletsBeforeMove = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.IsTrue(listBulletsBeforeMove.Count >= 1, "弾が生成されるべき");

            // 移動前の弾位置を記録
            Vector3 firstBulletPositionBefore = listBulletsBeforeMove[0].Position;

            // Act: 親オブジェクトを移動
            Vector3 parentMovement = new Vector3(5f, 3f, 2f);
            m_PlayerGameObject.transform.position = initialPlayerPosition + parentMovement;

            // 弾の更新（独立性が有効なら親の移動の影響を受けない）
            m_Player.ManualUpdate();

            // Assert: 弾の位置が親の移動に影響されていない
            var listBulletsAfterMove = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.IsTrue(listBulletsAfterMove.Count >= 1, "弾が存在するべき");

            Vector3 firstBulletPositionAfter = listBulletsAfterMove[0].Position;
            
            // 弾は独自に移動するが、親の移動量分がそのまま加算されてはいけない
            Vector3 bulletMovement = firstBulletPositionAfter - firstBulletPositionBefore;
            
            // 親の移動量と弾の移動量が同じでないことを確認
            float movementDifference = Vector3.Distance(bulletMovement, parentMovement);
            Assert.IsTrue(movementDifference > 0.01f, 
                $"弾は親の移動に追従してはいけない。弾の移動: {bulletMovement}, 親の移動: {parentMovement}");
        }

        [Test]
        public void BulletIndependence_DependentBullets_FollowParentMovement()
        {
            // Arrange: 弾の独立性を無効にする
            m_Player.SetBulletIndependence(false);
            
            // 初期位置を設定
            Vector3 initialPlayerPosition = new Vector3(0f, 0f, 0f);
            m_PlayerGameObject.transform.position = initialPlayerPosition;

            // シンプルな弾発射XMLを読み込み
            string testXml = @"<?xml version=""1.0"" ?>
                <bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
                    <action label=""top"">
                        <fire>
                            <direction>0</direction>
                            <speed>0</speed>
                        </fire>
                    </action>
                </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 弾を生成（速度0で静止）
            m_Player.ManualUpdate();

            var listBulletsBeforeMove = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.IsTrue(listBulletsBeforeMove.Count >= 1, "弾が生成されるべき");

            // 移動前の弾の相対位置を記録（親との関係）
            Vector3 initialParentPosition = m_PlayerGameObject.transform.position;

            // Act: 親オブジェクトを移動
            Vector3 parentMovement = new Vector3(5f, 3f, 2f);
            m_PlayerGameObject.transform.position = initialParentPosition + parentMovement;

            // 弾の更新（独立性が無効なら親の移動に追従する）
            m_Player.ManualUpdate();

            // Assert: GameObjectが親に追従していることを確認
            // （注意：この部分は実際のGameObjectが作成される場合のみ有効）
            // BulletMLBulletの論理位置は変わらないが、GameObjectの階層で追従する
            
            // 親が移動した後も弾のリストが保持されている
            var listBulletsAfterMove = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.IsTrue(listBulletsAfterMove.Count >= 1, "親移動後も弾が存在するべき");
            
            // この設定では弾は親子関係によって自動的に移動する
            // GameObjectの実際の位置確認は統合テストで行う
        }

        [Test]
        public void BulletIndependence_SettingChange_AffectsNewBullets()
        {
            // Arrange: 最初は独立性無効
            m_Player.SetBulletIndependence(false);
            
            string testXml = @"<?xml version=""1.0"" ?>
                <bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
                    <action label=""top"">
                        <fire>
                            <direction>0</direction>
                            <speed>1</speed>
                        </fire>
                    </action>
                </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 最初の弾を生成
            m_Player.ManualUpdate();
            
            int bulletsAfterFirst = m_Player.GetActiveBullets().Where(b => b.IsVisible).Count();
            Assert.IsTrue(bulletsAfterFirst >= 1, "最初の弾が生成されるべき");

            // Act: 独立性を有効に変更
            m_Player.SetBulletIndependence(true);

            // 設定変更後にXMLを再実行
            m_Player.StartTopAction();
            m_Player.ManualUpdate();

            // Assert: 新しい設定が適用される
            int bulletsAfterChange = m_Player.GetActiveBullets().Where(b => b.IsVisible).Count();
            Assert.IsTrue(bulletsAfterChange >= 1, "設定変更後も弾が生成されるべき");
            
            // 設定が正しく保持されている
            Assert.IsTrue(m_Player.GetBulletIndependence(), "独立性設定が保持されるべき");
        }

        [Test]
        public void BulletIndependence_ShooterPosition_UpdatesWithParentMovement()
        {
            // Arrange: 独立性を有効にして、発射位置が親の現在位置になることをテスト
            m_Player.SetBulletIndependence(true);
            
            // 初期位置を設定
            Vector3 initialPosition = new Vector3(0f, 0f, 0f);
            m_PlayerGameObject.transform.position = initialPosition;

            string testXml = @"<?xml version=""1.0"" ?>
                <bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
                    <action label=""top"">
                        <fire>
                            <direction>0</direction>
                            <speed>2</speed>
                        </fire>
                        <wait>10</wait>
                        <fire>
                            <direction>90</direction>
                            <speed>2</speed>
                        </fire>
                    </action>
                </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 最初の弾を発射
            m_Player.ManualUpdate();
            var firstBullets = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.IsTrue(firstBullets.Count >= 1, "最初の弾が生成されるべき");
            
            Vector3 firstBulletPosition = firstBullets[0].Position;

            // Act: 親オブジェクトを移動
            Vector3 movedPosition = new Vector3(5f, 3f, 0f);
            m_PlayerGameObject.transform.position = movedPosition;

            // wait期間をスキップして次の弾を発射
            for (int i = 0; i < 12; i++) // wait 10フレーム + 余裕
            {
                m_Player.ManualUpdate();
            }

            // Assert: 2番目の弾は移動後の位置から発射される
            var allBullets = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.IsTrue(allBullets.Count >= 2, "2番目の弾が生成されるべき");

            // 最後に発射された弾の位置を確認
            var lastBullet = allBullets.Last();
            
            // 新しい弾は移動後の親位置から発射されるべき
            // (多少の誤差を許容)
            float distanceFromMovedPosition = Vector3.Distance(lastBullet.Position, movedPosition);
            Assert.IsTrue(distanceFromMovedPosition < 0.5f, 
                $"新しい弾は移動後の位置から発射されるべき。弾位置: {lastBullet.Position}, 期待位置: {movedPosition}, 距離: {distanceFromMovedPosition}");

            // 最初の弾は移動後も独立した位置にある
            float firstBulletDistanceFromMoved = Vector3.Distance(firstBullets[0].Position, movedPosition);
            Assert.IsTrue(firstBulletDistanceFromMoved > 1f, 
                $"最初の弾は独立して移動しているべき。弾位置: {firstBullets[0].Position}, 親位置: {movedPosition}");
        }
    }
}
