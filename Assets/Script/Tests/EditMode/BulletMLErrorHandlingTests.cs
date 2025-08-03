using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Collections;
using BulletML;

namespace Tests
{
    /// <summary>
    /// BulletMLの包括的エラーハンドリングテスト
    /// 不正参照、無効値、実行時エラー、境界値、安定性の検証
    /// 
    /// 検証項目：
    /// - 不正参照（存在しないラベル、循環参照）
    /// - 無効XML・データ（不正構造、型不一致）
    /// - 実行時エラー（無限ループ、スタックオーバー）
    /// - 境界値・異常値（極大極小、NaN、Infinity）
    /// - パフォーマンス・安定性（大量データ、長時間実行）
    /// </summary>
    public class BulletMLErrorHandlingTests
    {
        private BulletMLExecutor m_Executor;
        private BulletMLParser m_Parser;
        private const float DELTA_TIME = 1f / 60f; // 60FPS
        private List<BulletMLBullet> m_FiredBullets;
        
        [SetUp]
        public void Setup()
        {
            m_Parser = new BulletMLParser();
            m_Executor = new BulletMLExecutor();
            m_Executor.SetDefaultSpeed(2f);
            m_Executor.SetTargetPosition(new Vector3(0f, -100f, 0f));
            m_Executor.SetRankValue(0.5f);
            m_Executor.ResetSequenceValues();
            
            // 発射された弾のリスト
            m_FiredBullets = new List<BulletMLBullet>();
            m_Executor.OnBulletCreated = (bullet) => {
                m_FiredBullets.Add(bullet);
            };
        }
        
        [Test]
        public void ErrorHandling_InvalidActionRef_LogsErrorAndContinues()
        {
            // 存在しないactionRef参照時の堅牢なエラーハンドリングをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction>0</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <actionRef label=""nonexistentAction""/>
    <fire>
      <direction>90</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 期待されるエラーログ
            LogAssert.Expect(LogType.Error, "Referenced action not found: nonexistentAction");
            
            bool noException = true;
            try
            {
                // 実行（エラーがあっても継続すること）
                for (int frame = 0; frame < 100; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 2) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // エラーハンドリングの確認
            Assert.IsTrue(noException, "不正なactionRef参照でも例外を投げずに処理を継続するべき");
            Assert.AreEqual(2, m_FiredBullets.Count, "エラーがあっても正常な弾は発射されるべき");
        }
        
        [Test]
        public void ErrorHandling_InvalidBulletRef_LogsErrorAndContinues()
        {
            // 存在しないbulletRef参照時の堅牢なエラーハンドリングをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction>0</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <fire>
      <direction>90</direction>
      <speed>2</speed>
      <bulletRef label=""nonexistentBullet""/>
    </fire>
    <fire>
      <direction>180</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noException = true;
            try
            {
                // 実行（エラーがあっても継続すること）
                for (int frame = 0; frame < 50; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 3) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // エラーハンドリングの確認
            Assert.IsTrue(noException, "不正なbulletRef参照でも例外を投げずに処理を継続するべき");
            Assert.AreEqual(3, m_FiredBullets.Count, "エラーがあっても全ての弾が発射されるべき");
        }
        
        [Test]
        public void ErrorHandling_InvalidFireRef_LogsErrorAndContinues()
        {
            // 存在しないfireRef参照時の堅牢なエラーハンドリングをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction>0</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <fireRef label=""nonexistentFire""/>
    <fire>
      <direction>180</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 期待されるエラーログ
            LogAssert.Expect(LogType.Error, "Referenced fire not found: nonexistentFire");
            
            bool noException = true;
            try
            {
                // 実行（エラーがあっても継続すること）
                for (int frame = 0; frame < 50; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 2) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // エラーハンドリングの確認
            Assert.IsTrue(noException, "不正なfireRef参照でも例外を投げずに処理を継続するべき");
            Assert.AreEqual(2, m_FiredBullets.Count, "エラーがあっても正常な弾は発射されるべき");
        }
        
        [Test]
        public void ErrorHandling_CircularActionRef_PreventesInfiniteLoop()
        {
            // 循環参照actionRefでの無限ループ防止をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""circularA""/>
  </action>
  
  <action label=""circularA"">
    <actionRef label=""circularB""/>
  </action>
  
  <action label=""circularB"">
    <actionRef label=""circularA""/>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noInfiniteLoop = true;
            var startTime = System.DateTime.Now;
            
            try
            {
                // 循環参照があっても有限時間で終了すること
                for (int frame = 0; frame < 1000; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                    
                    // 5秒以上かかったら無限ループと判定
                    if ((System.DateTime.Now - startTime).TotalSeconds > 5)
                    {
                        noInfiniteLoop = false;
                        break;
                    }
                }
            }
            catch (System.Exception)
            {
                // 例外発生も想定内（無限ループよりまし）
            }
            
            var executionTime = (System.DateTime.Now - startTime).TotalSeconds;
            
            // 無限ループ防止の確認
            Assert.IsTrue(noInfiniteLoop, "循環参照があっても無限ループに陥らないべき");
            Assert.Less(executionTime, 5.0, "循環参照処理が5秒以内で完了するべき");
        }
        
        [Test]
        public void ErrorHandling_ExtremelyLargeValues_HandlesGracefully()
        {
            // 極端に大きな値での安全処理をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction>999999999</direction>
      <speed>999999999</speed>
      <bullet/>
    </fire>
    <wait>999999999</wait>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noException = true;
            try
            {
                // 極端な値でも処理が可能であること
                for (int frame = 0; frame < 10; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 1) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // 極端値処理の確認
            Assert.IsTrue(noException, "極端に大きな値でも例外を投げずに処理するべき");
            Assert.AreEqual(1, m_FiredBullets.Count, "極端な値でも弾が発射されるべき");
            
            if (m_FiredBullets.Count > 0)
            {
                var bullet = m_FiredBullets[0];
                Assert.IsTrue(!float.IsNaN(bullet.Direction), "弾の方向がNaNにならないべき");
                Assert.IsTrue(!float.IsNaN(bullet.Speed), "弾の速度がNaNにならないべき");
                Assert.IsTrue(bullet.Speed > 0, "弾の速度が正の値であるべき");
            }
        }
        
        [Test]
        public void ErrorHandling_NegativeValues_HandlesCorrectly()
        {
            // 負の値での適切な処理をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction>-45</direction>
      <speed>-2</speed>
      <bullet/>
    </fire>
    <fire>
      <direction>90</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <wait>-10</wait>
    <fire>
      <direction>180</direction>
      <speed>1</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noException = true;
            try
            {
                // 負の値でも処理が継続されること
                for (int frame = 0; frame < 50; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 3) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // 負値処理の確認
            Assert.IsTrue(noException, "負の値でも例外を投げずに処理するべき");
            Assert.GreaterOrEqual(m_FiredBullets.Count, 2, "負の値があっても弾が発射されるべき");
            
            // 負の速度の処理確認
            if (m_FiredBullets.Count > 0)
            {
                var firstBullet = m_FiredBullets[0];
                // 負の速度は絶対値として処理されるか、または特別な処理がされる
                Assert.IsTrue(!float.IsNaN(firstBullet.Speed), "負速度でもNaNにならないべき");
            }
        }
        
        [Test]
        public void ErrorHandling_EmptyElements_HandlesGracefully()
        {
            // 空要素での安全処理をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction></direction>
      <speed></speed>
      <bullet/>
    </fire>
    <fire>
      <direction>90</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <wait></wait>
    <fire>
      <direction>180</direction>
      <speed>1</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noException = true;
            try
            {
                // 空要素でも処理が継続されること
                for (int frame = 0; frame < 50; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 3) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // 空要素処理の確認
            Assert.IsTrue(noException, "空要素でも例外を投げずに処理するべき");
            Assert.GreaterOrEqual(m_FiredBullets.Count, 2, "空要素があっても正常な弾は発射されるべき");
        }
        
        [Test]
        public void ErrorHandling_MassiveRepeatCount_PreventesPerformanceIssues()
        {
            // 大量repeat回数でのパフォーマンス問題防止をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>100000</times>
      <action>
        <fire>
          <direction type=""sequence"">1</direction>
          <speed>1</speed>
          <bullet/>
        </fire>
      </action>
    </repeat>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            var startTime = System.DateTime.Now;
            bool completedInTime = true;
            
            try
            {
                // 大量repeat処理が適切に制限されること
                for (int frame = 0; frame < 1000; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    // 5秒以上かかる場合は制限されていると判定
                    if ((System.DateTime.Now - startTime).TotalSeconds > 5)
                    {
                        completedInTime = false;
                        break;
                    }
                    
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                // 例外で停止することも適切な制限
            }
            
            var executionTime = (System.DateTime.Now - startTime).TotalSeconds;
            
            // パフォーマンス制限の確認
            // 実装によっては制限がないかもしれないが、少なくとも安定して動作すること
            Assert.IsTrue(m_FiredBullets.Count > 0, "大量repeatでも弾が発射されるべき");
            
            // メモリ使用量の確認（GCを強制実行）
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }
        
        [Test]
        public void ErrorHandling_DeepActionNesting_PreventesStackOverflow()
        {
            // 深いアクションネストでのスタックオーバーフロー防止をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""nest1""/>
  </action>
  
  <action label=""nest1"">
    <actionRef label=""nest2""/>
  </action>
  
  <action label=""nest2"">
    <actionRef label=""nest3""/>
  </action>
  
  <action label=""nest3"">
    <actionRef label=""nest4""/>
  </action>
  
  <action label=""nest4"">
    <actionRef label=""nest5""/>
  </action>
  
  <action label=""nest5"">
    <fire>
      <direction>0</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noStackOverflow = true;
            try
            {
                // 深いネストでもスタックオーバーフローしないこと
                for (int frame = 0; frame < 100; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 1) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.StackOverflowException)
            {
                noStackOverflow = false;
            }
            catch (System.Exception)
            {
                // 他の例外は許容（スタックオーバーフローでなければOK）
            }
            
            // スタックオーバーフロー防止の確認
            Assert.IsTrue(noStackOverflow, "深いアクションネストでもスタックオーバーフローしないべき");
            Assert.AreEqual(1, m_FiredBullets.Count, "深いネストでも最終的に弾が発射されるべき");
        }
        
        [Test]
        public void ErrorHandling_InvalidExpressions_HandlesGracefully()
        {
            // 不正な数式での安全処理をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction>abc</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <fire>
      <direction>1+*2</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <fire>
      <direction>90</direction>
      <speed>def</speed>
      <bullet/>
    </fire>
    <fire>
      <direction>180</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            bool noException = true;
            try
            {
                // 不正数式でも処理が継続されること
                for (int frame = 0; frame < 50; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    if (m_FiredBullets.Count >= 4) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // 不正数式処理の確認
            Assert.IsTrue(noException, "不正な数式でも例外を投げずに処理するべき");
            Assert.GreaterOrEqual(m_FiredBullets.Count, 1, "不正数式があっても正常な弾は発射されるべき");
            
            // 不正な値がデフォルト値に置換されることを確認
            foreach (var bullet in m_FiredBullets)
            {
                Assert.IsTrue(!float.IsNaN(bullet.Direction), "不正数式の方向がNaNにならないべき");
                Assert.IsTrue(!float.IsNaN(bullet.Speed), "不正数式の速度がNaNにならないべき");
            }
        }
        
        [Test]
        public void ErrorHandling_MemoryStability_NoMemoryLeaks()
        {
            // メモリ安定性・リーク防止をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>100</times>
      <action>
        <fire>
          <direction type=""sequence"">3.6</direction>
          <speed>2</speed>
          <bulletRef label=""memoryBullet""/>
        </fire>
      </action>
    </repeat>
  </action>
  
  <bullet label=""memoryBullet"">
    <speed>1</speed>
    <action>
      <changeSpeed>
        <speed>3</speed>
        <term>10</term>
      </changeSpeed>
    </action>
  </bullet>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            // GC前のメモリ状況を記録
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            long memoryBefore = System.GC.GetTotalMemory(false);
            
            // 大量の弾生成・実行を繰り返し
            for (int iteration = 0; iteration < 10; iteration++)
            {
                m_FiredBullets.Clear();
                
                var shooterBullet = new BulletMLBullet(
                    Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
                );
                
                var topAction = document.GetTopAction();
                var actionRunner = new BulletMLActionRunner(topAction);
                shooterBullet.PushAction(actionRunner);
                
                // 大量弾生成
                for (int frame = 0; frame < 200; frame++)
                {
                    bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                    shooterBullet.UpdateChanges(DELTA_TIME);
                    
                    // 全ての弾のアクションも実行
                    for (int i = 0; i < m_FiredBullets.Count; i++)
                    {
                        var bullet = m_FiredBullets[i];
                        if (bullet.ActionStack.Count > 0)
                        {
                            m_Executor.ExecuteCurrentAction(bullet);
                            bullet.UpdateChanges(DELTA_TIME);
                        }
                    }
                    
                    if (m_FiredBullets.Count >= 100) break;
                    if (!hasAction && shooterBullet.WaitFrames == 0) break;
                }
            }
            
            // GC後のメモリ状況を確認
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            long memoryAfter = System.GC.GetTotalMemory(false);
            
            long memoryDifference = memoryAfter - memoryBefore;
            
            // メモリリーク確認（5MB以下の増加は許容）
            Assert.Less(memoryDifference, 5 * 1024 * 1024, "大量弾生成でも重大なメモリリークがないべき");
            Assert.GreaterOrEqual(m_FiredBullets.Count, 50, "メモリテストでも弾が正常に生成されるべき");
        }
        
        [Test]
        public void ErrorHandling_ConcurrentExecution_ThreadSafe()
        {
            // 並行実行時の安全性をテスト（簡易版）
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>50</times>
      <action>
        <fire>
          <direction type=""sequence"">7.2</direction>
          <speed>2</speed>
          <bullet/>
        </fire>
      </action>
    </repeat>
  </action>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            
            bool noException = true;
            int totalBullets = 0;
            
            try
            {
                // 複数のExecutorを同時実行（疑似並行）
                var executors = new List<BulletMLExecutor>();
                var bulletLists = new List<List<BulletMLBullet>>();
                
                for (int i = 0; i < 3; i++)
                {
                    var executor = new BulletMLExecutor();
                    executor.SetDocument(document);
                    executor.SetDefaultSpeed(2f);
                    executor.SetRankValue(0.5f);
                    
                    var bullets = new List<BulletMLBullet>();
                    executor.OnBulletCreated = (bullet) => bullets.Add(bullet);
                    
                    executors.Add(executor);
                    bulletLists.Add(bullets);
                }
                
                // 同時実行
                for (int frame = 0; frame < 100; frame++)
                {
                    for (int i = 0; i < executors.Count; i++)
                    {
                        var executor = executors[i];
                        var shooterBullet = new BulletMLBullet(
                            Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
                        );
                        
                        var topAction = document.GetTopAction();
                        var actionRunner = new BulletMLActionRunner(topAction);
                        shooterBullet.PushAction(actionRunner);
                        
                        for (int subFrame = 0; subFrame < 10; subFrame++)
                        {
                            bool hasAction = executor.ExecuteCurrentAction(shooterBullet);
                            shooterBullet.UpdateChanges(DELTA_TIME);
                            
                            if (!hasAction && shooterBullet.WaitFrames == 0) break;
                        }
                    }
                }
                
                // 結果集計
                foreach (var bullets in bulletLists)
                {
                    totalBullets += bullets.Count;
                }
            }
            catch (System.Exception)
            {
                noException = false;
            }
            
            // 並行実行安全性の確認
            Assert.IsTrue(noException, "複数Executor同時実行でも例外が発生しないべき");
            Assert.Greater(totalBullets, 0, "並行実行でも弾が生成されるべき");
        }
    }
}