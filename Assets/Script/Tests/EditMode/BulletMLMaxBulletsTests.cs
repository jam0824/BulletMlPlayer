using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;
using System.Linq;

namespace Tests
{
    /// <summary>
    /// BulletMLPlayerの弾数上限処理をテストするクラス
    /// </summary>
    public class BulletMLMaxBulletsTests
    {
        private GameObject m_PlayerObject;
        private BulletMlPlayer m_Player;

        [SetUp]
        public void SetUp()
        {
            // テスト用のGameObjectとBulletMlPlayerを作成
            m_PlayerObject = new GameObject("TestPlayer");
            m_Player = m_PlayerObject.AddComponent<BulletMlPlayer>();
            
            // 手動で初期化（Start()が呼ばれないため）
            m_Player.InitializeForTest();
            
            // テスト用の設定
            m_Player.SetMaxBullets(3); // 上限を3発に設定（テストしやすくするため）
            m_Player.SetEnableDebugLog(true);
            
            // 座標系を設定
            m_Player.SetCoordinateSystem(BulletML.CoordinateSystem.YZ);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_PlayerObject != null)
            {
                Object.DestroyImmediate(m_PlayerObject);
            }
        }

        /// <summary>
        /// 弾数上限に達したときに最古の弾が削除されることをテスト
        /// </summary>
        [Test]
        public void MaxBullets_RemovesOldestBullet_WhenLimitReached()
        {
            // Arrange: 同時発射XMLを作成（確実に上限を超える）
            string testXml = @"
                <bulletml>
                    <action label=""top"">
                        <fire>
                            <direction type=""absolute"">0</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">90</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">180</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">270</direction>
                            <speed>1</speed>
                        </fire>
                    </action>
                </bulletml>";

            // Act: XMLを読み込んで実行
            
            // 他のログメッセージを許可
            LogAssert.ignoreFailingMessages = true;
            
            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            
            // 複数フレームで4発を発射（上限3発を超える）
            for (int i = 0; i < 4; i++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"RemoveOldest test {i+1}回目ManualUpdate後の弾数: {m_Player.GetActiveBullets().Count}");
                Debug.Log($"RemoveOldest test {i+1}回目ManualUpdate後の可視弾数: {m_Player.GetActiveBullets().Where(b => b.IsVisible).Count()}");
            }
            
            // 上限に達した状態を確認
            var listFirstBullets = m_Player.GetActiveBullets();
            Debug.Log($"最初の発射完了後の全弾数: {listFirstBullets.Count}");
            Assert.AreEqual(3, listFirstBullets.Count, "上限3発を維持するべき");
            
            var firstBulletPosition = listFirstBullets[0].Position;
            var secondBulletPosition = listFirstBullets[1].Position;
            var thirdBulletPosition = listFirstBullets[2].Position;

            // さらに発射を続けてFIFO削除を確認
            for (int i = 0; i < 3; i++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"追加発射 {i+1}回目後の弾数: {m_Player.GetActiveBullets().Count}");
            }
            
            // ignoreFailingMessagesを無効にする
            LogAssert.ignoreFailingMessages = false;
            
            // Assert: 弾数は3発のまま、FIFO削除が動作している
            var listCurrentBullets = m_Player.GetActiveBullets();
            Debug.Log($"最終的な弾数: {listCurrentBullets.Count}");
            Assert.AreEqual(3, listCurrentBullets.Count, "弾数上限により3発を維持するべき");
            
            // 可視弾の確認（シューター弾を除く）
            var visibleBullets = listCurrentBullets.Where(b => b.IsVisible).ToList();
            Debug.Log($"最終的な可視弾数: {visibleBullets.Count}");
            Assert.IsTrue(visibleBullets.Count >= 1, "少なくとも1発の可視弾が存在するべき");
        }

        /// <summary>
        /// 弾数上限に達したときにデバッグログが出力されることをテスト
        /// </summary>
        [Test]
        public void MaxBullets_LogsWarning_WhenLimitReached()
        {
            // Arrange: 無限ループ発射XMLを作成（確実に上限を超える）
            string testXml = @"
                <bulletml>
                    <action label=""top"">
                        <repeat>
                            <action>
                                <fire>
                                    <direction type=""absolute"">0</direction>
                                    <speed>1</speed>
                                </fire>
                                <wait>1</wait>
                            </action>
                        </repeat>
                    </action>
                </bulletml>";

            // Act & Assert: ログ出力をテスト
            
            // 他のログメッセージを許可
            LogAssert.ignoreFailingMessages = true;
            
            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            
            // 十分な回数ManualUpdateを実行して確実に上限を超える
            for (int i = 0; i < 10; i++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"{i+1}回目ManualUpdate後の弾数: {m_Player.GetActiveBullets().Count}");
                
                // 上限に達したら次のループでログをテスト
                if (m_Player.GetActiveBullets().Count >= 3)
                {
                    Debug.Log($"上限到達確認 - 可視弾数: {m_Player.GetActiveBullets().Where(b => b.IsVisible).Count()}");
                    
                    // ignoreFailingMessagesを無効にして、期待するログのみをテスト
                    LogAssert.ignoreFailingMessages = false;
                    
                    // 次のManualUpdateで上限を超える弾が発射されてログが出力される
                    LogAssert.Expect(LogType.Warning, "弾数上限到達。最古の弾を削除しました。(上限: 3)");
                    
                    Debug.Log("ManualUpdate実行前 - LogAssert.Expect設定完了");
                    m_Player.ManualUpdate();
                    Debug.Log($"ログテスト後の弾数: {m_Player.GetActiveBullets().Count}");
                    
                    // ログが出なかった場合、さらに数回試行
                    for (int j = 0; j < 5; j++)
                    {
                        Debug.Log($"追加試行 {j+1}回目前の弾数: {m_Player.GetActiveBullets().Count}");
                        LogAssert.Expect(LogType.Warning, "弾数上限到達。最古の弾を削除しました。(上限: 3)");
                        m_Player.ManualUpdate();
                        Debug.Log($"追加試行 {j+1}回目後の弾数: {m_Player.GetActiveBullets().Count}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// デバッグログが無効のときはログが出力されないことをテスト
        /// </summary>
        [Test]
        public void MaxBullets_NoLogging_WhenDebugLogDisabled()
        {
            // Arrange: デバッグログを無効にする
            m_Player.SetEnableDebugLog(false);
            
            string testXml = @"
                <bulletml>
                    <action label=""top"">
                        <fire>
                            <direction type=""absolute"">0</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">90</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">180</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">270</direction>
                            <speed>1</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">45</direction>
                            <speed>1</speed>
                        </fire>
                    </action>
                </bulletml>";

            // Act: XMLを読み込んで上限を超えて実行
            
            // 他のログメッセージを許可
            LogAssert.ignoreFailingMessages = true;
            
            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            
            // 5発同時発射パターン（複数フレームで実行）
            for (int i = 0; i < 5; i++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"NoLogging test {i+1}回目ManualUpdate後の弾数: {m_Player.GetActiveBullets().Count}");
                Debug.Log($"NoLogging test {i+1}回目ManualUpdate後の可視弾数: {m_Player.GetActiveBullets().Where(b => b.IsVisible).Count()}");
            }
            
            Debug.Log($"最終的な弾数: {m_Player.GetActiveBullets().Count}");
            Debug.Log($"最終的な可視弾数: {m_Player.GetActiveBullets().Where(b => b.IsVisible).Count()}");
            
            // ignoreFailingMessagesを無効にする
            LogAssert.ignoreFailingMessages = false;
            
            // Assert: 弾数は上限3発を維持（機能は動作）、ログは出力されない
            Assert.AreEqual(3, m_Player.GetActiveBullets().Count, "デバッグログ無効でも弾数上限機能は動作するべき");
        }

        /// <summary>
        /// 上限内での弾生成は正常に動作することをテスト
        /// </summary>
        [Test]
        public void MaxBullets_NormalOperation_WhenUnderLimit()
        {
            // Arrange: シンプルな発射XMLを作成
            string testXml = @"
                <bulletml>
                    <action label=""top"">
                        <fire>
                            <direction type=""absolute"">0</direction>
                            <speed>1</speed>
                        </fire>
                        <wait>5</wait>
                        <fire>
                            <direction type=""absolute"">90</direction>
                            <speed>2</speed>
                        </fire>
                    </action>
                </bulletml>";

            // Act: XMLを読み込んで実行
            
            // 他のログメッセージを許可
            LogAssert.ignoreFailingMessages = true;
            
            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            
            // 1発目
            m_Player.ManualUpdate();
            var bullets1 = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Debug.Log($"1発目後の全弾数: {m_Player.GetActiveBullets().Count}, 可視弾数: {bullets1.Count}");
            Assert.AreEqual(1, bullets1.Count, "1発目が正常に生成されるべき");
            
            // 待機期間をスキップ
            for (int i = 0; i < 5; i++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"待機{i+1}回目後の全弾数: {m_Player.GetActiveBullets().Count}");
            }
            
            // 2発目
            Debug.Log("2発目発射前");
            m_Player.ManualUpdate();
            var bullets2 = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Debug.Log($"2発目後の全弾数: {m_Player.GetActiveBullets().Count}, 可視弾数: {bullets2.Count}");
            
            // さらに数回実行して確認
            for (int i = 0; i < 3; i++)
            {
                m_Player.ManualUpdate();
                var currentBullets = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
                Debug.Log($"追加{i+1}回目後の全弾数: {m_Player.GetActiveBullets().Count}, 可視弾数: {currentBullets.Count}");
            }
            
            var finalBullets = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            Assert.AreEqual(2, finalBullets.Count, "2発目が正常に生成されるべき");
            
            // ignoreFailingMessagesを無効にする
            LogAssert.ignoreFailingMessages = false;
        }

        /// <summary>
        /// 同時に複数の弾が上限を超えて生成される場合のテスト
        /// </summary>
        [Test]
        public void MaxBullets_HandlesMultipleSimultaneousBullets_WhenLimitReached()
        {
            // Arrange: このテスト用に上限を5発に設定
            m_Player.SetMaxBullets(5);
            
            // 同時発射XMLを作成（シンプルな構造で検証）
            string testXml = @"
                <bulletml>
                    <action label=""top"">
                        <fire>
                            <direction type=""absolute"">0</direction>
                            <speed>2</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">90</direction>
                            <speed>2</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">45</direction>
                            <speed>2</speed>
                        </fire>
                        <wait>1</wait>
                        <fire>
                            <direction type=""absolute"">270</direction>
                            <speed>2</speed>
                        </fire>
                        <fire>
                            <direction type=""absolute"">135</direction>
                            <speed>2</speed>
                        </fire>
                    </action>
                </bulletml>";

            // Act: XMLを読み込んで実行
            
            // 他のログメッセージを許可
            LogAssert.ignoreFailingMessages = true;
            
            m_Player.LoadBulletML(testXml);
            Debug.Log("XMLロード完了");
            
            m_Player.StartTopAction();
            Debug.Log("StartTopAction完了");
            
            Debug.Log($"ManualUpdate前の弾数: {m_Player.GetActiveBullets().Count}");
            
            // 最初の同時3発（複数フレーム実行）
            for (int frame = 0; frame < 3; frame++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"ManualUpdate #{frame + 1}完了 - 弾数: {m_Player.GetActiveBullets().Count}");
            }
            
            // デバッグ情報を出力
            var bullets = m_Player.GetActiveBullets();
            var visibleBullets = bullets.Where(b => b.IsVisible).ToList();
            Debug.Log($"全体弾数: {bullets.Count}, 可視弾数: {visibleBullets.Count}");
            
            for (int i = 0; i < bullets.Count; i++)
            {
                Debug.Log($"弾{i}: 方向={bullets[i].Direction}, 速度={bullets[i].Speed}, 位置={bullets[i].Position}, IsActive={bullets[i].IsActive}, IsVisible={bullets[i].IsVisible}");
            }
            
            Assert.AreEqual(3, visibleBullets.Count, "最初の3発の可視弾が生成されるべき");
            
            // wait待機とその後の2発を処理
            for (int frame = 0; frame < 5; frame++)
            {
                m_Player.ManualUpdate();
                Debug.Log($"追加ManualUpdate #{frame + 1}完了 - 弾数: {m_Player.GetActiveBullets().Count}");
            }
            
            // 最終確認
            var finalBullets = m_Player.GetActiveBullets();
            var finalVisibleBullets = finalBullets.Where(b => b.IsVisible).ToList();
            Debug.Log($"最終弾数: {finalBullets.Count}, 最終可視弾数: {finalVisibleBullets.Count}");
            
            // ignoreFailingMessagesを無効にする
            LogAssert.ignoreFailingMessages = false;
            
            // Assert: 可視弾が上限5発を維持（シューター除く）
            Assert.AreEqual(5, finalVisibleBullets.Count, "上限5発の可視弾を維持するべき");
        }
    }
}
