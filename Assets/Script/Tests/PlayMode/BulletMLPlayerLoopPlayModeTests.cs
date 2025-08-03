using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;

namespace BulletMLTests.PlayMode
{
    /// <summary>
    /// BulletMLPlayerのループ機能をPlayModeでテストするクラス
    /// 実際のUnityフレームサイクルを使用したテスト
    /// </summary>
    public class BulletMLPlayerLoopPlayModeTests
    {
        private GameObject m_PlayerGameObject;
        private BulletMlPlayer m_Player;
        private string m_SimpleTestXml;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // シンプルなテスト用XML
            m_SimpleTestXml = @"<?xml version=""1.0"" ?>
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<fire>
<direction type=""absolute"">0</direction>
<speed>1</speed>
<bullet/>
</fire>
<vanish/>
</action>
</bulletml>";
        }

        [SetUp]
        public void SetUp()
        {
            // テスト用GameObjectとBulletMLPlayerを作成
            m_PlayerGameObject = new GameObject("TestBulletMLPlayer");
            m_Player = m_PlayerGameObject.AddComponent<BulletMlPlayer>();

            // 確実に初期化するために手動でInitializeSystemを呼ぶ
            var initializeMethod = typeof(BulletMlPlayer).GetMethod("InitializeSystem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            initializeMethod?.Invoke(m_Player, null);

            // テスト用の設定
            m_Player.SetRankValue(0.5f);
            m_Player.SetCoordinateSystem(CoordinateSystem.XY);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_PlayerGameObject != null)
            {
                Object.DestroyImmediate(m_PlayerGameObject);
            }
        }

        [UnityTest]
        public IEnumerator BasicLoopFunctionality_WithRealFrames_WorksCorrectly()
        {
            // Arrange
            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(30); // 0.5秒間隔（60FPSで）
            m_Player.LoadBulletML(m_SimpleTestXml);
            m_Player.StartTopAction();

            // 初期状態確認
            Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "初期弾があるはず");

            // Act - XML実行完了状態を正しくシミュレート
            m_Player.ClearAllBullets();
            m_Player.ResetLoopState();
            Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "弾がクリアされたはず");

            // 動的観察によるループ開始確認（遅延30フレーム ± 5フレームの誤差許容）
            bool loopStarted = false;
            int maxFramesToTest = 37; // 遅延30フレーム + 誤差5フレーム + 余裕2フレーム
            
            for (int i = 1; i <= maxFramesToTest; i++)
            {
                yield return null;
                int currentBullets = m_Player.GetActiveBullets().Count;
                
                UnityEngine.Debug.Log($"PlayMode遅延30フレームテスト - フレーム{i}: 弾数={currentBullets}");
                
                if (currentBullets >= 1)
                {
                    loopStarted = true;
                    UnityEngine.Debug.Log($"PlayModeでループ開始検知: フレーム{i}");
                    break;
                }
            }

            // Assert - 数フレームの誤差を許容してループ開始を確認
            Assert.IsTrue(loopStarted, "ループが開始されて弾が生成されたはず（±5フレーム誤差許容）");
        }

        [UnityTest]
        public IEnumerator MultipleLoopCycles_WithRealFrames_WorksStably()
        {
            // Arrange
            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(10); // 短い間隔でテスト
            m_Player.LoadBulletML(m_SimpleTestXml);

            int successfulLoops = 0;
            const int targetLoops = 5;

            // Act - 各ループサイクルを動的観察で処理
            for (int loop = 0; loop < targetLoops; loop++)
            {
                UnityEngine.Debug.Log($"複数ループサイクルテスト - サイクル{loop + 1}開始");
                
                // 新しいループサイクルを開始
                m_Player.LoadBulletML(m_SimpleTestXml);
                m_Player.StartTopAction();
                
                // XML実行完了状態を正しくシミュレート
                m_Player.ClearAllBullets();
                m_Player.ResetLoopState();
                
                // 動的観察によるループ開始確認（遅延10フレーム ± 3フレームの誤差許容）
                bool currentLoopStarted = false;
                int maxFramesToTest = 15; // 遅延10フレーム + 誤差3フレーム + 余裕2フレーム
                
                for (int i = 1; i <= maxFramesToTest; i++)
                {
                    yield return null;
                    int currentBullets = m_Player.GetActiveBullets().Count;
                    
                    UnityEngine.Debug.Log($"複数ループサイクル サイクル{loop + 1} - フレーム{i}: 弾数={currentBullets}");
                    
                    if (currentBullets >= 1)
                    {
                        currentLoopStarted = true;
                        successfulLoops++;
                        UnityEngine.Debug.Log($"複数ループサイクル - サイクル{loop + 1}完了（フレーム{i}）");
                        break;
                    }
                }
                
                if (!currentLoopStarted)
                {
                    UnityEngine.Debug.Log($"複数ループサイクル - サイクル{loop + 1}失敗（ループ開始なし）");
                }

                // 次のループサイクルのために少し待機
                yield return new WaitForSeconds(0.1f);
            }

            UnityEngine.Debug.Log($"複数ループサイクルテスト完了 - 成功したループ数: {successfulLoops}/{targetLoops}");

            // Assert - 数フレームの誤差を許容してループ完了を確認
            Assert.AreEqual(targetLoops, successfulLoops, 
                $"{targetLoops}回の連続ループがすべて成功するはず（±3フレーム誤差許容）");
        }

        [UnityTest]
        public IEnumerator LoopDisabled_DoesNotRestart()
        {
            // Arrange
            m_Player.SetLoopEnabled(false);
            m_Player.SetLoopDelayFrames(5);
            m_Player.LoadBulletML(m_SimpleTestXml);
            m_Player.StartTopAction();

            // Act
            m_Player.ClearAllBullets();

            // 十分な時間待機
            for (int i = 0; i < 20; i++)
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(0, m_Player.GetActiveBullets().Count, 
                "ループが無効なので再開してはいけない");
        }

        [UnityTest]
        public IEnumerator RuntimeLoopSettingsChange_AffectsImmediately()
        {
            // Arrange
            m_Player.SetLoopEnabled(false);
            m_Player.SetLoopDelayFrames(10);
            m_Player.LoadBulletML(m_SimpleTestXml);
            m_Player.StartTopAction();
            m_Player.ClearAllBullets();

            // 最初はループ無効なので再開しない
            for (int i = 0; i < 15; i++)
            {
                yield return null;
            }
            Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "ループ無効時は再開しない");

            // Act - まずはループ無効のまま新しい実行を開始
            m_Player.LoadBulletML(m_SimpleTestXml);
            m_Player.StartTopAction();
            
            // 最初の実行で弾が生成されることを確認
            Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "実行開始時に弾が生成される");

            // XML実行完了まで待機（シューターのvanishまで）
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
            
            // XML実行完了後も発射された子弾は残る
            Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "XML実行完了後も子弾は残る");

            // ここで実行時にループ設定を変更
            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(3);

            // 動的観察によるループ開始確認（遅延3フレーム ± 3フレームの誤差許容）
            bool loopStarted = false;
            int initialBullets = m_Player.GetActiveBullets().Count;
            int maxFramesToTest = 8; // 遅延3フレーム + 誤差3フレーム + 余裕2フレーム
            
            for (int i = 1; i <= maxFramesToTest; i++)
            {
                yield return null;
                int currentBullets = m_Player.GetActiveBullets().Count;
                
                UnityEngine.Debug.Log($"PlayMode実行時設定変更テスト - フレーム{i}: 弾数={currentBullets}（初期:{initialBullets}）");
                
                // 弾数が増加した場合はループ開始
                if (currentBullets > initialBullets)
                {
                    loopStarted = true;
                    UnityEngine.Debug.Log($"PlayModeで実行時設定変更後のループ開始検知: フレーム{i}（弾数: {initialBullets} → {currentBullets}）");
                    break;
                }
            }

            // Assert - 数フレームの誤差を許容してループ開始を確認
            Assert.IsTrue(loopStarted, "実行時にループ設定を変更すると即座に反映されるはず（±3フレーム誤差許容）");
        }

        [UnityTest]
        public IEnumerator ZeroDelayFrames_LoopsImmediately()
        {
            // Arrange
            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(0); // 即座にループ
            m_Player.LoadBulletML(m_SimpleTestXml);
            m_Player.StartTopAction();

            // Act - XML実行完了状態を正しくシミュレート
            m_Player.ClearAllBullets();
            m_Player.ResetLoopState();

            // 動的観察による即座ループ確認（遅延0フレーム ± 2フレームの誤差許容）
            bool loopStarted = false;
            int maxFramesToTest = 3; // 即座ループ + 誤差2フレーム + 余裕1フレーム
            
            for (int i = 1; i <= maxFramesToTest; i++)
            {
                yield return null;
                int currentBullets = m_Player.GetActiveBullets().Count;
                
                UnityEngine.Debug.Log($"PlayMode遅延0フレームテスト - フレーム{i}: 弾数={currentBullets}");
                
                if (currentBullets >= 1)
                {
                    loopStarted = true;
                    UnityEngine.Debug.Log($"PlayModeで即座ループ検知: フレーム{i}");
                    break;
                }
            }

            // Assert - 数フレームの誤差を許容してループ開始を確認
            Assert.IsTrue(loopStarted, "遅延フレーム0では即座にループするはず（±2フレーム誤差許容）");
        }

        [UnityTest]
        public IEnumerator ComplexXmlPattern_LoopsCorrectly()
        {
            // Arrange - より複雑なパターンのXML
            string complexXml = @"<?xml version=""1.0"" ?>
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<repeat>
<times>3</times>
<action>
<fire>
<direction type=""absolute"">0</direction>
<speed>1</speed>
<bullet/>
</fire>
<wait>10</wait>
</action>
</repeat>
<vanish/>
</action>
</bulletml>";

            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(20);
            m_Player.LoadBulletML(complexXml);
            m_Player.StartTopAction();

            // 初期状態確認
            Assert.GreaterOrEqual(m_Player.GetActiveBullets().Count, 1, "初期弾があるはず");

            // Act - パターンが完了するまで十分な時間実行
            float elapsedTime = 0f;
            const float maxWaitTime = 3f; // 最大3秒待機

            while (m_Player.GetActiveBullets().Count > 0 && elapsedTime < maxWaitTime)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            // XML実行完了状態を正しくシミュレート
            if (m_Player.GetActiveBullets().Count > 0)
            {
                m_Player.ClearAllBullets(); // 強制的にクリア
            }
            m_Player.ResetLoopState();

            // 動的観察によるループ開始確認（遅延20フレーム ± 5フレームの誤差許容）
            bool loopStarted = false;
            int maxFramesToTest = 27; // 遅延20フレーム + 誤差5フレーム + 余裕2フレーム
            
            for (int i = 1; i <= maxFramesToTest; i++)
            {
                yield return null;
                int currentBullets = m_Player.GetActiveBullets().Count;
                
                UnityEngine.Debug.Log($"PlayMode複雑パターンテスト - フレーム{i}: 弾数={currentBullets}");
                
                if (currentBullets >= 1)
                {
                    loopStarted = true;
                    UnityEngine.Debug.Log($"PlayModeで複雑パターンループ開始検知: フレーム{i}");
                    break;
                }
            }

            // Assert - 数フレームの誤差を許容してループ開始を確認
            Assert.IsTrue(loopStarted, "複雑なパターンでもループが正常に動作するはず（±5フレーム誤差許容）");
        }

        [UnityTest]
        public IEnumerator LongRunningLoopTest_StaysStable()
        {
            // Arrange
            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(30);
            m_Player.LoadBulletML(m_SimpleTestXml);

            int completedLoops = 0;
            const int targetLoops = 3;
            const float maxTestTime = 15f; // 最大15秒でテスト終了（余裕を持って）

            // Act - 各ループサイクルを動的観察で処理
            float startTime = Time.time;

            for (int cycle = 0; cycle < targetLoops && (Time.time - startTime) < maxTestTime; cycle++)
            {
                UnityEngine.Debug.Log($"長期実行テスト - サイクル{cycle + 1}開始");
                
                // 新しいループサイクルを開始
                m_Player.LoadBulletML(m_SimpleTestXml);
                m_Player.StartTopAction();
                
                // XML実行完了状態を正しくシミュレート
                m_Player.ClearAllBullets();
                m_Player.ResetLoopState();
                
                // 動的観察によるループ開始確認（遅延30フレーム ± 5フレームの誤差許容）
                bool currentLoopStarted = false;
                int maxFramesToTest = 37; // 遅延30フレーム + 誤差5フレーム + 余裕2フレーム
                
                for (int i = 1; i <= maxFramesToTest; i++)
                {
                    yield return null;
                    
                    // タイムアウトチェック
                    if ((Time.time - startTime) >= maxTestTime)
                    {
                        UnityEngine.Debug.Log($"長期実行テスト - タイムアウト（サイクル{cycle + 1}、フレーム{i}）");
                        break;
                    }
                    
                    int currentBullets = m_Player.GetActiveBullets().Count;
                    
                    if (i <= 5 || currentBullets > 0) // 最初の5フレームまたは弾が生成された場合のみログ
                    {
                        UnityEngine.Debug.Log($"長期実行テスト サイクル{cycle + 1} - フレーム{i}: 弾数={currentBullets}");
                    }
                    
                    if (currentBullets >= 1)
                    {
                        currentLoopStarted = true;
                        completedLoops++;
                        UnityEngine.Debug.Log($"長期実行テスト - サイクル{cycle + 1}完了（フレーム{i}）");
                        break;
                    }
                }
                
                if (!currentLoopStarted)
                {
                    UnityEngine.Debug.Log($"長期実行テスト - サイクル{cycle + 1}失敗（ループ開始なし）");
                }

                // 次のサイクルのために少し待機
                yield return new WaitForSeconds(0.1f);
            }

            UnityEngine.Debug.Log($"長期実行テスト完了 - 成功したループ数: {completedLoops}/{targetLoops}");

            // Assert - 数フレームの誤差を許容してループ完了を確認
            Assert.GreaterOrEqual(completedLoops, targetLoops, 
                $"{targetLoops}回のループが完了するはず（実際: {completedLoops}回、±5フレーム誤差許容）");
        }

        [UnityTest]
        public IEnumerator ManualRestart_DuringLoopWait_ResetsCorrectly()
        {
            // Arrange
            m_Player.SetLoopEnabled(true);
            m_Player.SetLoopDelayFrames(60); // 長い待機時間
            m_Player.LoadBulletML(m_SimpleTestXml);
            m_Player.StartTopAction();

            // Act - 実行完了状態にする
            m_Player.ClearAllBullets();

            // ループ待機中に手動でStartTopActionを呼ぶ
            for (int i = 0; i < 30; i++) // 待機時間の半分
            {
                yield return null;
            }

            m_Player.StartTopAction(); // 手動再開

            // Assert
            Assert.AreEqual(1, m_Player.GetActiveBullets().Count, 
                "手動再開により即座に弾が生成されるはず");

            // さらに少し待って、自動ループが正常に動作することも確認
            m_Player.ClearAllBullets();
            m_Player.ResetLoopState();
            
            // 動的観察による手動再開後の自動ループ確認（遅延60フレーム ± 5フレームの誤差許容）
            bool autoLoopStarted = false;
            int maxFramesToTest = 67; // 遅延60フレーム + 誤差5フレーム + 余裕2フレーム
            
            for (int i = 1; i <= maxFramesToTest; i++)
            {
                yield return null;
                int currentBullets = m_Player.GetActiveBullets().Count;
                
                if (i <= 5 || currentBullets > 0) // 最初の5フレームまたは弾が生成された場合のみログ
                {
                    UnityEngine.Debug.Log($"手動再開後の自動ループテスト - フレーム{i}: 弾数={currentBullets}");
                }
                
                if (currentBullets >= 1)
                {
                    autoLoopStarted = true;
                    UnityEngine.Debug.Log($"手動再開後の自動ループ開始検知: フレーム{i}");
                    break;
                }
            }

            // Assert - 数フレームの誤差を許容して自動ループ開始を確認
            Assert.IsTrue(autoLoopStarted, "手動再開後も自動ループが正常に動作するはず（±5フレーム誤差許容）");
        }
    }
}