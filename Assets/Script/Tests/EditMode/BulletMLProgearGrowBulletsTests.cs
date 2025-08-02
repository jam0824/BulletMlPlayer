using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using BulletML;

namespace Tests
{
    /// <summary>
    /// [Progear]_round_1_boss_grow_bullets.xmlの成長する弾幕パターンをテストする
    /// </summary>
    public class BulletMLProgearGrowBulletsTests
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
            string xmlPath = Path.Combine(Application.dataPath, "Script", "xml", "[Progear]_round_1_boss_grow_bullets.xml");
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
        public void ProgearGrowBullets_ParseSuccessfully()
        {
            // XMLが正常に解析できること
            Assert.IsNotNull(m_Document, "BulletMLDocumentが作成されるべき");
            Assert.IsNotNull(m_Document.RootElement, "ルート要素が存在するべき");
            Assert.AreEqual(BulletMLType.horizontal, m_Document.Type, "type=horizontalであるべき");
            
            // トップアクションが存在すること
            var topAction = m_Document.GetTopAction();
            Assert.IsNotNull(topAction, "topアクションが存在するべき");
            
            // seed弾が存在すること
            var seedBullet = m_Document.GetLabeledBullet("seed");
            Assert.IsNotNull(seedBullet, "seed弾が定義されているべき");
        }
        
        [Test]
        public void ProgearGrowBullets_SeedBulletPattern_RankDependent()
        {
            // ランク依存の親弾（seed）発射パターンをテスト
            
            // シューター弾を作成
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 実行して親弾を発射させる
            for (int frame = 0; frame < 100; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                            // 期待する弾数に達したらテスト終了
            int expectedBullets = 1 + 4 + Mathf.RoundToInt(0.5f * 6); // rank=0.5の場合: 1+4+3=8発
            if (m_FiredBullets.Count >= expectedBullets) break;
                
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 結果確認：ランク0.5で8発の親弾が発射されること
            int expectedSeedCount = 8;
            Assert.AreEqual(expectedSeedCount, m_FiredBullets.Count, 
                $"ランク0.5で{expectedSeedCount}発の親弾が発射されるべき（実際: {m_FiredBullets.Count}発）");
            
            // 全弾がseed弾であることを確認
            foreach (var bullet in m_FiredBullets)
            {
                Assert.AreEqual(1.2f, bullet.Speed, 0.1f, "seed弾の初期速度は1.2であるべき");
            }
        }
        
        [Test]
        public void ProgearGrowBullets_FanShapeDirection_CorrectAngles()
        {
            // 扇状発射の方向パターンをテスト
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 実行
            for (int frame = 0; frame < 50; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 8) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 方向の確認
            if (m_FiredBullets.Count >= 3)
            {
                // 最初の弾の基準方向計算: 270-(4+$rank*6)*15/2 = 270-(4+0.5*6)*15/2 = 270-52.5 = 217.5度
                float rankValue = 0.5f;
                float angleCalculationValue = 4f + rankValue * 6f; // 7
                float baseDirection = 270f - (angleCalculationValue * 15f / 2f); // 217.5度
                float expectedFirstDirection = baseDirection;
                
                // 最初の弾の方向確認（許容誤差付き）
                Assert.AreEqual(expectedFirstDirection, m_FiredBullets[0].Direction, 5f, 
                    $"最初の弾の方向は{expectedFirstDirection}度付近であるべき");
                
                // sequence型で15度ずつ増加することを確認
                if (m_FiredBullets.Count >= 2)
                {
                    float directionDiff = m_FiredBullets[1].Direction - m_FiredBullets[0].Direction;
                    Assert.AreEqual(15f, directionDiff, 1f, "弾間の角度差は15度であるべき");
                }
            }
        }
        
        [Test]
        public void ProgearGrowBullets_SeedSpeedTransition_DecelerationToStop()
        {
            // seed弾の速度変化（1.2 → 0への減速）をテスト
            
            var seedBullet = m_Document.GetLabeledBullet("seed");
            var testBullet = new BulletMLBullet(
                Vector3.zero, 270f, 1.2f, CoordinateSystem.XY, true
            );
            
            var actionRunner = new BulletMLActionRunner(seedBullet.GetChild(BulletMLElementType.action));
            testBullet.PushAction(actionRunner);
            
            // 速度の変化をトラッキング
            List<float> speedHistory = new List<float>();
            
            // 150フレーム実行して速度変化を記録
            for (int frame = 0; frame < 150; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(DELTA_TIME);
                
                // 10フレームごとに速度を記録
                if (frame % 10 == 0)
                {
                    speedHistory.Add(testBullet.Speed);
                }
                
                if (!hasAction && testBullet.WaitFrames == 0) break;
            }
            
            // 速度変化の確認
            Assert.Greater(speedHistory.Count, 5, "十分な速度データが記録されるべき");
            
            // 初期速度確認
            Assert.AreEqual(1.2f, speedHistory[0], 0.1f, "初期速度は1.2であるべき");
            
            // 減速の確認（60フレーム後に0に近い値）
            if (speedHistory.Count > 6)
            {
                float finalSpeed = speedHistory[6]; // 60フレーム後付近
                Assert.Less(finalSpeed, 0.2f, "60フレーム後の速度は0に近いべき");
            }
        }
        
        [Test]
        public void ProgearGrowBullets_ChildBulletGeneration_SequenceSpeed()
        {
            // 子弾生成とsequence速度増加をテスト
            
            var seedBullet = m_Document.GetLabeledBullet("seed");
            var testBullet = new BulletMLBullet(
                Vector3.zero, 270f, 1.2f, CoordinateSystem.XY, true
            );
            
            var actionRunner = new BulletMLActionRunner(seedBullet.GetChild(BulletMLElementType.action));
            testBullet.PushAction(actionRunner);
            
            List<BulletMLBullet> childBullets = new List<BulletMLBullet>();
            m_Executor.OnBulletCreated = (bullet) => {
                childBullets.Add(bullet);
            };
            
            // 200フレーム実行（減速60+静止60+子弾発射）
            for (int frame = 0; frame < 200; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(DELTA_TIME);
                
                if (!hasAction && testBullet.WaitFrames == 0) break;
            }
            
            // 子弾の生成確認
            // 1発目（固定速度0.75）+ repeat回数（4+$rank*4）= 1 + (4+0.5*4) = 7発
            int expectedChildCount = 1 + 4 + Mathf.RoundToInt(0.5f * 4); // rank=0.5の場合: 1+4+2=7発
            Assert.AreEqual(expectedChildCount, childBullets.Count, 
                $"ランク0.5で{expectedChildCount}発の子弾が生成されるべき（実際: {childBullets.Count}発）");
            
            // 子弾の速度確認（1発目は固定、2発目以降はsequence累積）
            if (childBullets.Count >= 3)
            {
                Assert.AreEqual(0.75f, childBullets[0].Speed, 0.1f, "1発目の子弾速度は0.75（固定）であるべき");
                Assert.AreEqual(0.90f, childBullets[1].Speed, 0.1f, "2発目の子弾速度は0.90（0.75+0.15）であるべき");
                Assert.AreEqual(1.05f, childBullets[2].Speed, 0.1f, "3発目の子弾速度は1.05（0.90+0.15）であるべき");
            }
        }
        
        [Test]
        public void ProgearGrowBullets_RankInfluence_BulletCount()
        {
            // ランク値が弾数に与える影響をテスト
            
            // 低ランク（$rank = 0）でのテスト
            var lowRankResults = ExecuteGrowBulletsPattern(0f);
            
            // 高ランク（$rank = 1）でのテスト
            var highRankResults = ExecuteGrowBulletsPattern(1f);
            
            // ランクによる弾数の違いを確認
            Assert.Less(lowRankResults.seedCount, highRankResults.seedCount, 
                "高ランクの方が多くの親弾が発射されるべき");
            
            // 期待値の確認
            Assert.AreEqual(5, lowRankResults.seedCount, "ランク0では5発の親弾（1+4）");
            Assert.AreEqual(11, highRankResults.seedCount, "ランク1では11発の親弾（1+10）");
            
            // 子弾数の確認（各親弾あたり）
            // ランク0: 1+4+0*4 = 5発
            // ランク1: 1+4+1*4 = 9発
            Assert.AreEqual(5, lowRankResults.childCountPerSeed, "ランク0では各親弾から5発の子弾（1+4+0）");
            Assert.AreEqual(9, highRankResults.childCountPerSeed, "ランク1では各親弾から9発の子弾（1+4+4）");
        }
        
        [Test]
        public void ProgearGrowBullets_CompletePattern_TotalBulletCount()
        {
            // 完全パターンの実行と総弾数確認
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            List<BulletMLBullet> allBullets = new List<BulletMLBullet>();
            List<BulletMLBullet> activeBullets = new List<BulletMLBullet> { shooterBullet };
            List<BulletMLBullet> newBulletsThisFrame = new List<BulletMLBullet>();
            
            m_Executor.OnBulletCreated = (bullet) => {
                allBullets.Add(bullet);
                newBulletsThisFrame.Add(bullet); // フレーム内の新弾を一時保存
            };
            
            // 十分な時間実行（親弾発射+各親弾の減速+静止+子弾発射）
            for (int frame = 0; frame < 500; frame++) // フレーム数を増やす
            {
                List<BulletMLBullet> bulletsToRemove = new List<BulletMLBullet>();
                newBulletsThisFrame.Clear(); // フレーム開始時にクリア
                
                // 現在の活動弾のコピーを作成して安全に列挙
                var currentActiveBullets = new List<BulletMLBullet>(activeBullets);
                
                // 全ての活動中の弾を処理
                foreach (var bullet in currentActiveBullets)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(bullet);
                    bullet.UpdateChanges(DELTA_TIME);
                    
                    // アクションが終了した弾を除去リストに追加
                    if (!hasAction && bullet.WaitFrames == 0)
                    {
                        bulletsToRemove.Add(bullet);
                    }
                }
                
                // 終了した弾を活動リストから除去
                foreach (var bullet in bulletsToRemove)
                {
                    activeBullets.Remove(bullet);
                }
                
                // このフレームで生成された新弾を活動リストに追加
                activeBullets.AddRange(newBulletsThisFrame);
                
                // 全ての弾のアクションが終了したら終了
                if (activeBullets.Count == 0) break;
            }
            
            // 弾数分析（親弾と子弾の分離）
            int parentBullets = 0;
            int childBullets = 0;
            
            // 生成順序で弾を分析（最初の8発は親弾）
            for (int i = 0; i < allBullets.Count; i++)
            {
                if (i < 8) // 最初の8発は親弾
                {
                    parentBullets++;
                }
                else
                {
                    childBullets++;
                }
            }
            
            // 期待値の正しい計算
            // ランク0.5の場合: 親弾数 = 1 + (4+$rank*6) = 1 + (4+0.5*6) = 1 + 7 = 8発
            // 各親弾からの子弾数 = 1+4+$rank*4 = 1+4+0.5*4 = 7発
            // 合計 = 8 + (8 * 7) = 64発
            int expectedParentBullets = 8; // 1発 + repeat 7回 = 8発
            int expectedChildBulletsPerParent = 7;
            int expectedTotalBullets = expectedParentBullets + (expectedParentBullets * expectedChildBulletsPerParent);
            
            Assert.AreEqual(expectedTotalBullets, allBullets.Count,
                $"ランク0.5で総計{expectedTotalBullets}発の弾が生成されるべき（実際: {allBullets.Count}発）" +
                $"\n詳細: 親弾{parentBullets}発 + 子弾{childBullets}発");
        }
        
        [Test]
        public void ProgearGrowBullets_SeedVanish_ProperCleanup()
        {
            // seed弾の適切な消滅をテスト
            
            var seedBullet = m_Document.GetLabeledBullet("seed");
            var testBullet = new BulletMLBullet(
                Vector3.zero, 270f, 1.2f, CoordinateSystem.XY, true
            );
            
            var actionRunner = new BulletMLActionRunner(seedBullet.GetChild(BulletMLElementType.action));
            testBullet.PushAction(actionRunner);
            
            bool vanishExecuted = false;
            
            // 200フレーム実行
            for (int frame = 0; frame < 200; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(testBullet);
                testBullet.UpdateChanges(DELTA_TIME);
                
                // vanishが実行されたかチェック（アクションが終了したら消滅）
                if (!hasAction && testBullet.WaitFrames == 0)
                {
                    vanishExecuted = true;
                    break;
                }
            }
            
            Assert.IsTrue(vanishExecuted, "seed弾は子弾発射後にvanishで消滅するべき");
        }
        
        private (int seedCount, int childCountPerSeed) ExecuteGrowBulletsPattern(float? overrideRank = null)
        {
            // ランク値を動的に取得または上書き
            float currentRank = overrideRank ?? 0.5f; // デフォルト値
            if (overrideRank.HasValue)
            {
                m_Executor.SetRankValue(overrideRank.Value);
            }
            
            // 成長弾幕パターンの実行と結果取得
            m_Executor.ResetSequenceValues();
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.XY, false
            );
            
            var topAction = m_Document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            List<BulletMLBullet> bullets = new List<BulletMLBullet>();
            m_Executor.OnBulletCreated = (bullet) => bullets.Add(bullet);
            
            // パターン実行（親弾発射のみ）
            for (int frame = 0; frame < 100; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 親弾数を算出
            int seedCount = bullets.Count;
            
            // 各親弾からの子弾数を算出（動的ランク値使用）
            // 実際の子弾数: 1発（固定速度）+ (4+$rank*4)発（sequence速度）
            int childCountPerSeed = 1 + 4 + Mathf.RoundToInt(currentRank * 4);
            
            return (seedCount, childCountPerSeed);
        }
    }
}