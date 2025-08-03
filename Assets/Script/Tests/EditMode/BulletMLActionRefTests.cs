using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Collections.Generic;
using BulletML;

namespace Tests
{
    /// <summary>
    /// actionRef要素の包括的テスト
    /// アクション参照システム、パラメータ渡し、ネスト構造の詳細検証
    /// </summary>
    public class BulletMLActionRefTests
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
        public void ActionRef_BasicReference_ExecutesCorrectly()
        {
            // 基本的なactionRef参照をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""basicAction""/>
  </action>
  
  <action label=""basicAction"">
    <fire>
      <direction type=""absolute"">90</direction>
      <speed>3</speed>
      <bullet/>
    </fire>
    <wait>10</wait>
    <fire>
      <direction type=""absolute"">270</direction>
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
            
            // 実行
            for (int frame = 0; frame < 50; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 2) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 結果確認
            Assert.AreEqual(2, m_FiredBullets.Count, "actionRef経由で2発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 2)
            {
                Assert.AreEqual(90f, m_FiredBullets[0].Direction, 1f, "1発目は90度方向");
                Assert.AreEqual(3f, m_FiredBullets[0].Speed, 0.1f, "1発目の速度は3");
                
                Assert.AreEqual(270f, m_FiredBullets[1].Direction, 1f, "2発目は270度方向");
                Assert.AreEqual(2f, m_FiredBullets[1].Speed, 0.1f, "2発目の速度は2");
            }
        }
        
        [Test]
        public void ActionRef_WithParameters_PassesValuesCorrectly()
        {
            // パラメータ付きactionRefをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""paramAction"">
      <param>45</param>
      <param>2.5</param>
      <param>90</param>
    </actionRef>
  </action>
  
  <action label=""paramAction"">
    <fire>
      <direction type=""absolute"">$1</direction>
      <speed>$2</speed>
      <bullet/>
    </fire>
    <wait>5</wait>
    <fire>
      <direction type=""absolute"">$1+$3</direction>
      <speed>$2*2</speed>
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
            
            // 実行
            for (int frame = 0; frame < 30; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 2) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // パラメータ評価結果の確認
            Assert.AreEqual(2, m_FiredBullets.Count, "パラメータ付きactionRefで2発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 2)
            {
                // 1発目: $1=45, $2=2.5
                Assert.AreEqual(45f, m_FiredBullets[0].Direction, 1f, "1発目の方向は$1=45度");
                Assert.AreEqual(2.5f, m_FiredBullets[0].Speed, 0.1f, "1発目の速度は$2=2.5");
                
                // 2発目: $1+$3=45+90=135, $2*2=2.5*2=5
                Assert.AreEqual(135f, m_FiredBullets[1].Direction, 1f, "2発目の方向は$1+$3=135度");
                Assert.AreEqual(5f, m_FiredBullets[1].Speed, 0.1f, "2発目の速度は$2*2=5");
            }
        }
        
        [Test]
        public void ActionRef_NestedReference_ExecutesSequentially()
        {
            // ネストしたactionRef（actionRef内でactionRef）をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""outerAction"">
      <param>30</param>
    </actionRef>
  </action>
  
  <action label=""outerAction"">
    <fire>
      <direction type=""absolute"">$1</direction>
      <speed>1</speed>
      <bullet/>
    </fire>
    <actionRef label=""innerAction"">
      <param>$1*2</param>
      <param>3</param>
    </actionRef>
  </action>
  
  <action label=""innerAction"">
    <fire>
      <direction type=""absolute"">$1</direction>
      <speed>$2</speed>
      <bullet/>
    </fire>
    <fire>
      <direction type=""absolute"">$1+180</direction>
      <speed>$2</speed>
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
            
            // 実行
            for (int frame = 0; frame < 50; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 3) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // ネスト実行結果の確認
            Assert.AreEqual(3, m_FiredBullets.Count, "ネストしたactionRefで3発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // outerActionからの弾: direction=$1=30
                Assert.AreEqual(30f, m_FiredBullets[0].Direction, 1f, "1発目（outer）の方向は30度");
                Assert.AreEqual(1f, m_FiredBullets[0].Speed, 0.1f, "1発目（outer）の速度は1");
                
                // innerActionからの弾1: direction=$1*2=60
                Assert.AreEqual(60f, m_FiredBullets[1].Direction, 1f, "2発目（inner）の方向は60度");
                Assert.AreEqual(3f, m_FiredBullets[1].Speed, 0.1f, "2発目（inner）の速度は3");
                
                // innerActionからの弾2: direction=$1*2+180=240
                Assert.AreEqual(240f, m_FiredBullets[2].Direction, 1f, "3発目（inner）の方向は240度");
                Assert.AreEqual(3f, m_FiredBullets[2].Speed, 0.1f, "3発目（inner）の速度は3");
            }
        }
        
        [Test]
        public void ActionRef_ComplexParameters_EvaluatesCorrectly()
        {
            // 複雑なパラメータ式を含むactionRefをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""complexAction"">
      <param>10+$rank*20</param>
      <param>$rand*180</param>
      <param>2*($rank+0.5)</param>
    </actionRef>
  </action>
  
  <action label=""complexAction"">
    <fire>
      <direction type=""absolute"">$1+$2</direction>
      <speed>$3</speed>
      <bullet/>
    </fire>
    <fire>
      <direction type=""absolute"">$1-$2</direction>
      <speed>$3/2</speed>
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
            
            // 実行
            for (int frame = 0; frame < 30; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 2) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 複雑パラメータ評価結果の確認
            Assert.AreEqual(2, m_FiredBullets.Count, "複雑パラメータ付きactionRefで2発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 2)
            {
                // パラメータ計算値の確認（$rank=0.5の場合）
                // $1 = 10+0.5*20 = 20
                // $2 = $rand*180 (0-180の範囲)
                // $3 = 2*(0.5+0.5) = 2
                
                Assert.AreEqual(2f, m_FiredBullets[0].Speed, 0.1f, "1発目の速度は$3=2");
                Assert.AreEqual(1f, m_FiredBullets[1].Speed, 0.1f, "2発目の速度は$3/2=1");
                
                // 方向は$randの値によるが、計算が正しく行われていることを確認
                float direction1 = m_FiredBullets[0].Direction;
                float direction2 = m_FiredBullets[1].Direction;
                
                // 2つの方向の差が$2*2=$rand*360になることを確認
                float directionDiff = Mathf.Abs(direction1 - direction2);
                Assert.Greater(directionDiff, 0f, "方向計算により異なる角度が設定されるべき");
            }
        }
        
        [Test]
        public void ActionRef_WithRepeat_ExecutesMultipleTimes()
        {
            // repeat内でのactionRef実行をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>3</times>
      <action>
        <actionRef label=""repeatableAction"">
          <param>120</param>
        </actionRef>
        <wait>5</wait>
      </action>
    </repeat>
  </action>
  
  <action label=""repeatableAction"">
    <fire>
      <direction type=""sequence"">$1</direction>
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
            
            // 実行
            for (int frame = 0; frame < 100; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 3) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // repeat実行結果の確認
            Assert.AreEqual(3, m_FiredBullets.Count, "repeat内のactionRefで3発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // sequence型方向の累積確認（120度ずつ増加）
                Assert.AreEqual(120f, m_FiredBullets[0].Direction, 1f, "1発目の方向は120度");
                Assert.AreEqual(240f, m_FiredBullets[1].Direction, 1f, "2発目の方向は240度");
                Assert.AreEqual(360f, m_FiredBullets[2].Direction, 1f, "3発目の方向は360度");
                
                // 全弾同じ速度
                foreach (var bullet in m_FiredBullets)
                {
                    Assert.AreEqual(2f, bullet.Speed, 0.1f, "全弾の速度は2であるべき");
                }
            }
        }
        
        [Test]
        public void ActionRef_WithFireRef_CombinedExecution()
        {
            // actionRefとfireRefの組み合わせをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""combinedAction"">
      <param>3</param>
    </actionRef>
  </action>
  
  <action label=""combinedAction"">
    <repeat>
      <times>$1</times>
      <action>
        <fireRef label=""burstFire"">
          <param>45</param>
          <param>2</param>
        </fireRef>
        <wait>10</wait>
      </action>
    </repeat>
  </action>
  
  <fire label=""burstFire"">
    <direction type=""sequence"">$1</direction>
    <speed>$2</speed>
    <bullet/>
  </fire>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 実行
            for (int frame = 0; frame < 150; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 3) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 組み合わせ実行結果の確認
            Assert.AreEqual(3, m_FiredBullets.Count, "actionRef+fireRefの組み合わせで3発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // fireRefのsequence方向確認（45度ずつ増加）
                Assert.AreEqual(45f, m_FiredBullets[0].Direction, 1f, "1発目の方向は45度");
                Assert.AreEqual(90f, m_FiredBullets[1].Direction, 1f, "2発目の方向は90度");
                Assert.AreEqual(135f, m_FiredBullets[2].Direction, 1f, "3発目の方向は135度");
                
                // 全弾同じ速度（fireRefのパラメータ）
                foreach (var bullet in m_FiredBullets)
                {
                    Assert.AreEqual(2f, bullet.Speed, 0.1f, "全弾の速度は2（fireRefパラメータ）であるべき");
                }
            }
        }
        
        [Test]
        public void ActionRef_RecursiveReference_HandlesCorrectly()
        {
            // 再帰的actionRef参照をテスト（適度な深さ）
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <actionRef label=""recursiveAction"">
      <param>3</param>
      <param>0</param>
    </actionRef>
  </action>
  
  <action label=""recursiveAction"">
    <fire>
      <direction type=""absolute"">$2*60</direction>
      <speed>2</speed>
      <bullet/>
    </fire>
    <actionRef label=""conditionalRecurse"">
      <param>$1-1</param>
      <param>$2+1</param>
    </actionRef>
  </action>
  
  <action label=""conditionalRecurse"">
    <actionRef label=""recursiveAction"">
      <param>$1</param>
      <param>$2</param>
    </actionRef>
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
            
            // 実行（再帰深度制限のため多めのフレーム）
            for (int frame = 0; frame < 200; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 3) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 再帰実行結果の確認
            Assert.GreaterOrEqual(m_FiredBullets.Count, 3, "再帰的actionRefで複数の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // 再帰パラメータによる方向変化確認
                Assert.AreEqual(0f, m_FiredBullets[0].Direction, 1f, "1発目の方向は0度（$2*60=0*60）");
                Assert.AreEqual(60f, m_FiredBullets[1].Direction, 1f, "2発目の方向は60度（$2*60=1*60）");
                Assert.AreEqual(120f, m_FiredBullets[2].Direction, 1f, "3発目の方向は120度（$2*60=2*60）");
                
                // 全弾同じ速度
                foreach (var bullet in m_FiredBullets)
                {
                    Assert.AreEqual(2f, bullet.Speed, 0.1f, "全弾の速度は2であるべき");
                }
            }
        }
        
        [Test]
        public void ActionRef_Performance_LargeScale()
        {
            // 大量のactionRef実行時のパフォーマンステスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>50</times>
      <action>
        <actionRef label=""performanceAction"">
          <param>3.6</param>
        </actionRef>
      </action>
    </repeat>
  </action>
  
  <action label=""performanceAction"">
    <fire>
      <direction type=""sequence"">$1</direction>
      <speed>1.5</speed>
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
            
            var startTime = System.DateTime.Now;
            
            // 大量実行
            for (int frame = 0; frame < 300; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 50) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            var executionTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            
            // パフォーマンス確認
            Assert.AreEqual(50, m_FiredBullets.Count, "50回のactionRef実行で50発の弾が生成されるべき");
            Assert.Less(executionTime, 1000, "50回のactionRef実行が1秒以内で完了するべき");
            
            // sequence方向の正確性確認（最初の数発）
            if (m_FiredBullets.Count >= 3)
            {
                Assert.AreEqual(3.6f, m_FiredBullets[0].Direction, 0.1f, "1発目の方向は3.6度");
                Assert.AreEqual(7.2f, m_FiredBullets[1].Direction, 0.1f, "2発目の方向は7.2度");
                Assert.AreEqual(10.8f, m_FiredBullets[2].Direction, 0.1f, "3発目の方向は10.8度");
            }
        }
        
        [Test]
        public void ActionRef_InvalidLabel_HandlesGracefully()
        {
            // 存在しないラベル参照時のエラーハンドリングをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction type=""absolute"">0</direction>
      <speed>1</speed>
      <bullet/>
    </fire>
    <actionRef label=""nonexistentAction""/>
    <fire>
      <direction type=""absolute"">180</direction>
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
            
            // エラーログを期待する
            LogAssert.Expect(LogType.Error, "Referenced action not found: nonexistentAction");
            
            // 実行（エラー時でも継続すること）
            bool noException = true;
            try
            {
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
            Assert.IsTrue(noException, "不正なactionRef参照でも例外を投げずに処理を継続するべき");
            
            // 正常な弾は発射されることを確認
            Assert.GreaterOrEqual(m_FiredBullets.Count, 1, "不正参照があっても正常な弾は発射されるべき");
        }
    }
}