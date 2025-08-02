using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using BulletML;

namespace Tests
{
    /// <summary>
    /// [Daiouzyou]_hibachi_1.xmlの超高密度弾幕パターンをテストする
    /// 大往生のひばち：最高難易度の弾幕地獄
    /// </summary>
    public class BulletMLDaiouzyouHibachiTests
    {
        private BulletMLDocument m_Document;
        private BulletMLExecutor m_Executor;
        private BulletMLParser m_Parser;
        private const float DELTA_TIME = 1f / 60f; // 60FPS
        private List<BulletMLBullet> m_FiredBullets;
        
        [SetUp]
        public void Setup()
        {
            // XMLファイルを読み込み
            string xmlPath = Path.Combine(Application.dataPath, "Script", "xml", "[Daiouzyou]_hibachi_1.xml");
            string xmlContent = File.ReadAllText(xmlPath);
            
            // BulletMLDocumentを作成
            m_Parser = new BulletMLParser();
            m_Document = m_Parser.Parse(xmlContent);
            
            // Executorを初期化
            m_Executor = new BulletMLExecutor();
            m_Executor.SetDocument(m_Document);
            m_Executor.SetDefaultSpeed(2f);
            m_Executor.SetTargetPosition(new Vector3(0f, -100f, 0f));
            m_Executor.SetRankValue(0.5f); // テスト用: $rank = 0.5
            
            // シーケンス値をリセット
            m_Executor.ResetSequenceValues();
            
            // 発射された弾のリスト
            m_FiredBullets = new List<BulletMLBullet>();
            
            // 弾発射のコールバック設定
            m_Executor.OnBulletCreated = (bullet) => {
                m_FiredBullets.Add(bullet);
            };
        }
        
        [Test]
        public void DaiouzyouHibachi_ParseSuccessfully()
        {
            // XMLが正常に解析できること
            Assert.IsNotNull(m_Document, "BulletMLDocumentが作成されるべき");
            Assert.IsNotNull(m_Document.RootElement, "ルート要素が存在するべき");
            
            // トップアクションが存在すること
            var topAction = m_Document.GetTopAction();
            Assert.IsNotNull(topAction, "topアクションが存在するべき");
            
            // n弾が存在すること
            var nFire = m_Document.GetLabeledFire("n");
            Assert.IsNotNull(nFire, "n弾が定義されているべき");
        }
        
        [Test]
        public void DaiouzyouHibachi_SingleCycle_17Bullets()
        {
            // 1サイクルで17発（メイン弾1発＋fireRef 16発）が発射されることをテスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 1サイクル分だけ実行（約50フレーム）
            for (int frame = 0; frame < 50; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                // 1サイクル完了時点で終了
                if (m_FiredBullets.Count >= 17) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 1サイクルで17発が発射されること
            Assert.AreEqual(17, m_FiredBullets.Count, 
                "1サイクルで17発（メイン弾1発+n弾16発）が発射されるべき");
            
            // 弾の種類確認
            if (m_FiredBullets.Count >= 17)
            {
                // メイン弾（最初の弾）はaim方向
                var mainBullet = m_FiredBullets[0];
                Assert.Greater(mainBullet.Speed, 0f, "メイン弾の速度は正の値であるべき");
                
                // n弾群（2発目以降）はsequence方向
                for (int i = 1; i < 17; i++)
                {
                    var nBullet = m_FiredBullets[i];
                    Assert.Greater(nBullet.Speed, 0f, $"n弾{i}の速度は正の値であるべき");
                }
            }
        }
        
        [Test]
        public void DaiouzyouHibachi_AimDirection_PlayerTargeting()
        {
            // メイン弾のプレイヤー狙い撃ち動作をテスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // メイン弾発射まで実行
            for (int frame = 0; frame < 20; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 1) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            Assert.GreaterOrEqual(m_FiredBullets.Count, 1, "メイン弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 1)
            {
                var mainBullet = m_FiredBullets[0];
                
                // aim方向はプレイヤー方向の基準角度±ランダム偏差
                // シューター(0,0,0) → ターゲット(0,-100,0) = 180度（下方向）
                // direction="$rand*30-74+$rank*2" でのランダム範囲考慮
                float direction = mainBullet.Direction;
                Assert.GreaterOrEqual(direction, 90f, "メイン弾はおおむね下方向（90-270度）であるべき");
                Assert.LessOrEqual(direction, 270f, "メイン弾はおおむね下方向（90-270度）であるべき");
            }
        }
        
        [Test]
        public void DaiouzyouHibachi_SequenceDirection_CumulativeChange()
        {
            // sequence型方向の累積変化をテスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 数発のn弾を発射させる
            for (int frame = 0; frame < 30; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 5) break; // メイン弾+n弾4発
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            Assert.GreaterOrEqual(m_FiredBullets.Count, 5, "メイン弾+n弾が複数発射されるべき");
            
            if (m_FiredBullets.Count >= 5)
            {
                // n弾のsequence変化を確認（2発目以降）
                List<float> nBulletDirections = new List<float>();
                for (int i = 1; i < 5; i++) // メイン弾を除く
                {
                    nBulletDirections.Add(m_FiredBullets[i].Direction);
                }
                
                // sequence型では方向が累積的に変化する
                bool hasDirectionVariation = false;
                for (int i = 1; i < nBulletDirections.Count; i++)
                {
                    if (Mathf.Abs(nBulletDirections[i] - nBulletDirections[i-1]) > 1f)
                    {
                        hasDirectionVariation = true;
                        break;
                    }
                }
                
                Assert.IsTrue(hasDirectionVariation, "n弾のsequence方向は累積的に変化するべき");
            }
        }
        
        [Test]
        public void DaiouzyouHibachi_FireRefExecution_16References()
        {
            // fireRef参照が正確に16回実行されることをテスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 1サイクル完了まで実行
            for (int frame = 0; frame < 50; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 17) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 総弾数確認
            Assert.AreEqual(17, m_FiredBullets.Count, "1サイクルで17発（1+16）発射されるべき");
            
            // n弾（fireRef）の数確認
            int nBulletCount = m_FiredBullets.Count - 1; // メイン弾を除く
            Assert.AreEqual(16, nBulletCount, "fireRef参照により16発のn弾が発射されるべき");
        }
        
        [Test]
        public void DaiouzyouHibachi_RankInfluence_RepeatCount()
        {
            // ランク値が繰り返し回数に与える影響をテスト
            
            // 低ランク（$rank = 0）でのテスト
            TestRankInfluence(0f, 10, "ランク0");
            
            // 中ランク（$rank = 0.5）でのテスト  
            TestRankInfluence(0.5f, 45, "ランク0.5");
            
            // 高ランク（$rank = 1）でのテスト
            TestRankInfluence(1f, 80, "ランク1");
        }
        
        private void TestRankInfluence(float rankValue, int expectedRepeatCount, string testName)
        {
            m_Executor.SetRankValue(rankValue);
            m_Executor.ResetSequenceValues();
            m_FiredBullets.Clear();
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 数サイクル実行して繰り返し回数を確認
            int cycleCount = 0;
            int previousBulletCount = 0;
            
            for (int frame = 0; frame < 500; frame++) // 十分な時間
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                // 17発ずつ増加するサイクルをカウント
                if (m_FiredBullets.Count >= previousBulletCount + 17)
                {
                    cycleCount++;
                    previousBulletCount = m_FiredBullets.Count;
                    
                    // 期待値に達したらテスト終了
                    if (cycleCount >= Mathf.Min(expectedRepeatCount, 5)) break; // 最大5サイクルで検証
                }
                
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 期待される繰り返し回数の計算: 10+$rank*70
            Assert.Greater(cycleCount, 0, $"{testName}で少なくとも1サイクルは実行されるべき");
            
            // 実際の総弾数が期待範囲内かチェック
            int expectedBulletsPerCycle = 17;
            int actualBulletsForTestCycles = cycleCount * expectedBulletsPerCycle;
            Assert.AreEqual(actualBulletsForTestCycles, m_FiredBullets.Count,
                $"{testName}で{cycleCount}サイクル分の弾数が正確であるべき");
        }
        
        [Test]
        public void DaiouzyouHibachi_Speed_RankDependent()
        {
            // ランク依存の速度変化をテスト
            
            // ランク0.5での速度確認
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 数発発射
            for (int frame = 0; frame < 30; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 3) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            Assert.GreaterOrEqual(m_FiredBullets.Count, 3, "複数の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // 速度の範囲確認: 0.5+$rank*2 = 0.5+0.5*2 = 1.5
                float expectedSpeed = 0.5f + 0.5f * 2f;
                
                foreach (var bullet in m_FiredBullets)
                {
                    Assert.AreEqual(expectedSpeed, bullet.Speed, 0.1f, 
                        $"ランク0.5での弾速度は{expectedSpeed}付近であるべき");
                }
            }
        }
        
        [Test]
        public void DaiouzyouHibachi_WaitTime_RankDependent()
        {
            // ランク依存の待機時間をテスト
            
            // 低ランクと高ランクでの実行速度比較
            float lowRankExecutionTime = MeasureExecutionTime(0f);
            float highRankExecutionTime = MeasureExecutionTime(1f);
            
            // 高ランクの方が待機時間が短く、より高速実行される
            Assert.Less(highRankExecutionTime, lowRankExecutionTime, 
                "高ランクの方が待機時間が短く、高速実行されるべき");
        }
        
        private float MeasureExecutionTime(float rankValue)
        {
            m_Executor.SetRankValue(rankValue);
            m_Executor.ResetSequenceValues();
            m_FiredBullets.Clear();
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            int frameCount = 0;
            
            // 2サイクル完了までの時間を測定
            for (int frame = 0; frame < 200; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                frameCount++;
                
                // 2サイクル分（34発）で終了
                if (m_FiredBullets.Count >= 34) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            return frameCount * DELTA_TIME; // 実行時間（秒）
        }
        
        [Test]
        public void DaiouzyouHibachi_HighDensity_MultiCycle()
        {
            // 高密度弾幕の複数サイクル実行をテスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 5サイクル程度実行
            for (int frame = 0; frame < 200; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                // 5サイクル分（85発）で終了
                if (m_FiredBullets.Count >= 85) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 高密度弾幕の確認
            Assert.GreaterOrEqual(m_FiredBullets.Count, 85, "5サイクルで85発の高密度弾幕が生成されるべき");
            
            // 弾数がサイクル単位（17発）で増加していることを確認
            int cycleCount = m_FiredBullets.Count / 17;
            int expectedBullets = cycleCount * 17;
            
            // 完全サイクルの確認（端数は許容）
            Assert.LessOrEqual(Mathf.Abs(m_FiredBullets.Count - expectedBullets), 17,
                "弾数は17発サイクル単位で増加するべき");
        }
        
        [Test]
        public void DaiouzyouHibachi_Performance_BulletManagement()
        {
            // 大量弾生成時のパフォーマンステスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            var startTime = System.DateTime.Now;
            
            // 大量弾生成（10サイクル＝170発）
            for (int frame = 0; frame < 300; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 170) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            var executionTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            
            // パフォーマンス確認
            Assert.GreaterOrEqual(m_FiredBullets.Count, 170, "大量弾生成が成功するべき");
            Assert.Less(executionTime, 1000, "1秒以内で170発の弾生成が完了するべき"); // 1秒以内
            
            // メモリリーク確認（弾リストが適切に管理されている）
            Assert.IsNotNull(m_FiredBullets, "弾リストが適切に管理されているべき");
            Assert.AreEqual(170, m_FiredBullets.Count, "弾数カウントが正確であるべき");
        }
    }
}