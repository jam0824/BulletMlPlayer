using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BulletML;

namespace Tests
{
            /// <summary>
        /// bulletRef要素の包括的テスト
        /// 弾参照システム、パラメータ渡し、弾属性の継承の詳細検証
        /// 
        /// 重要: BulletMLでは bullet要素の速度が fire要素の速度を上書きする
        /// </summary>
    public class BulletMLBulletRefTests
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
        public void BulletRef_BasicReference_InheritsProperties()
        {
            // 基本的なbulletRef参照と弾属性の継承をテスト
            // bullet要素の速度がfire要素の速度を上書きすることを確認
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction type=""absolute"">90</direction>
      <speed>3</speed>
      <bulletRef label=""basicBullet""/>
    </fire>
    <fire>
      <direction type=""absolute"">270</direction>
      <speed>1.5</speed>
      <bulletRef label=""basicBullet""/>
    </fire>
  </action>
  
  <bullet label=""basicBullet"">
    <speed>2.5</speed>
  </bullet>
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
            
            // 結果確認
            Assert.AreEqual(2, m_FiredBullets.Count, "bulletRef経由で2発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 2)
            {
                // 1発目: bullet要素の速度（2.5）がfire要素の速度（3）を上書き
                Assert.AreEqual(90f, m_FiredBullets[0].Direction, 1f, "1発目は90度方向");
                Assert.AreEqual(2.5f, m_FiredBullets[0].Speed, 0.1f, "1発目の速度はbullet要素の値（2.5）");
                
                // 2発目: bullet要素の速度（2.5）がfire要素の速度（1.5）を上書き
                Assert.AreEqual(270f, m_FiredBullets[1].Direction, 1f, "2発目は270度方向");
                Assert.AreEqual(2.5f, m_FiredBullets[1].Speed, 0.1f, "2発目の速度はbullet要素の値（2.5）");
            }
        }
        
        [Test]
        public void BulletRef_WithParameters_PassesValuesCorrectly()
        {
            // パラメータ付きbulletRefをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction type=""absolute"">45</direction>
      <speed>2</speed>
      <bulletRef label=""paramBullet"">
        <param>1.5</param>
        <param>90</param>
        <param>3</param>
      </bulletRef>
    </fire>
  </action>
  
  <bullet label=""paramBullet"">
    <speed>$1</speed>
    <action>
      <wait>10</wait>
      <changeDirection>
        <direction type=""relative"">$2</direction>
        <term>$3*10</term>
      </changeDirection>
    </action>
  </bullet>
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
                
                if (m_FiredBullets.Count >= 1) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // パラメータ評価結果の確認
            Assert.AreEqual(1, m_FiredBullets.Count, "パラメータ付きbulletRefで1発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 1)
            {
                var bullet = m_FiredBullets[0];
                
                // bullet要素の速度（$1=1.5）がfire要素の速度（2）を上書き
                Assert.AreEqual(1.5f, bullet.Speed, 0.1f, "弾の速度はbullet要素の値（$1=1.5）");
                Assert.AreEqual(45f, bullet.Direction, 1f, "弾の方向はfire要素の値（45度）");
                
                // 弾にアクションが設定されていることを確認
                Assert.Greater(bullet.ActionStack.Count, 0, "bulletRefで参照された弾にアクションが設定されているべき");
            }
        }
        
        [Test]
        public void BulletRef_WithAction_ExecutesCorrectly()
        {
            // bulletRefで参照された弾のアクション実行をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction type=""absolute"">0</direction>
      <speed>2</speed>
      <bulletRef label=""actionBullet""/>
    </fire>
  </action>
  
  <bullet label=""actionBullet"">
    <speed>1.5</speed>
    <action>
      <wait>20</wait>
      <changeSpeed>
        <speed>3</speed>
        <term>30</term>
      </changeSpeed>
      <wait>40</wait>
      <fire>
        <direction type=""absolute"">180</direction>
        <speed>1</speed>
        <bullet/>
      </fire>
    </action>
  </bullet>
</bulletml>";

            var document = m_Parser.Parse(xmlContent);
            m_Executor.SetDocument(document);
            
            var shooterBullet = new BulletMLBullet(
                Vector3.zero, 0f, 1f, CoordinateSystem.YZ, false
            );
            
            var topAction = document.GetTopAction();
            var actionRunner = new BulletMLActionRunner(topAction);
            shooterBullet.PushAction(actionRunner);
            
            // 実行（アクション完了まで十分な時間）
            // changeSpeed(30) + wait(40) + fire実行 = 約71フレーム
            for (int frame = 0; frame < 200; frame++)
            {
                bool hasShooterAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                // 全ての弾のアクションも実行（新しく作成された弾も含む）
                for (int i = 0; i < m_FiredBullets.Count; i++)
                {
                    var bullet = m_FiredBullets[i];
                    if (bullet.ActionStack.Count > 0)
                    {
                        m_Executor.ExecuteCurrentAction(bullet);
                        bullet.UpdateChanges(DELTA_TIME);
                    }
                }
                
                if (m_FiredBullets.Count >= 2) break; // 親弾 + 子弾
                
                // シューターのアクションが完了し、全ての弾のアクションも完了した場合に終了
                bool anyBulletHasAction = m_FiredBullets.Any(b => b.ActionStack.Count > 0);
                if (!hasShooterAction && !anyBulletHasAction) break;
            }
            
            // アクション実行結果の確認
            Assert.AreEqual(2, m_FiredBullets.Count, "親弾のアクションで子弾も発射されるべき");
            
            if (m_FiredBullets.Count >= 2)
            {
                var parentBullet = m_FiredBullets[0];
                var childBullet = m_FiredBullets[1];
                
                // 親弾の確認（changeSpeedアクション実行後）
                Assert.AreEqual(0f, parentBullet.Direction, 1f, "親弾の方向は0度");
                Assert.AreEqual(3f, parentBullet.Speed, 0.1f, "親弾の速度はchangeSpeed後の値（3）");
                
                // 子弾の確認（親弾のアクションで発射）
                Assert.AreEqual(180f, childBullet.Direction, 1f, "子弾の方向は180度");
                Assert.AreEqual(1f, childBullet.Speed, 0.1f, "子弾の速度は1");
            }
        }
        
        [Test]
        public void BulletRef_ComplexParameters_EvaluatesCorrectly()
        {
            // 複雑なパラメータ式を含むbulletRefをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction type=""absolute"">30</direction>
      <speed>2.5</speed>
      <bulletRef label=""complexBullet"">
        <param>10+$rank*20</param>
        <param>$rand*360</param>
        <param>1+$rank</param>
      </bulletRef>
    </fire>
  </action>
  
  <bullet label=""complexBullet"">
    <speed>$3</speed>
    <action>
      <changeDirection>
        <direction type=""relative"">$1+$2</direction>
        <term>30</term>
      </changeDirection>
    </action>
  </bullet>
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
                
                if (m_FiredBullets.Count >= 1) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 複雑パラメータ評価結果の確認
            Assert.AreEqual(1, m_FiredBullets.Count, "複雑パラメータ付きbulletRefで1発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 1)
            {
                var bullet = m_FiredBullets[0];
                
                // パラメータ計算値の確認（$rank=0.5の場合）
                // $1 = 10+0.5*20 = 20
                // $2 = $rand*360 (0-360の範囲)
                // $3 = 1+0.5 = 1.5
                
                Assert.AreEqual(30f, bullet.Direction, 1f, "弾の方向はfire要素の値（30度）");
                Assert.AreEqual(1.5f, bullet.Speed, 0.1f, "弾の速度はbullet要素の値（$3=1.5）");
                
                // アクションが設定されていることを確認
                Assert.Greater(bullet.ActionStack.Count, 0, "複雑パラメータを持つ弾にアクションが設定されているべき");
            }
        }
        
        [Test]
        public void BulletRef_NestedReference_ResolvesCorrectly()
        {
            // ネストしたbulletRef（bulletRefが別のbulletRefを参照）をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <fire>
      <direction type=""absolute"">45</direction>
      <speed>2</speed>
      <bulletRef label=""outerBullet"">
        <param>90</param>
      </bulletRef>
    </fire>
  </action>
  
  <bullet label=""outerBullet"">
    <speed>1.5</speed>
    <action>
      <fire>
        <direction type=""relative"">$1</direction>
        <speed>2.5</speed>
        <bulletRef label=""innerBullet"">
          <param>$1/2</param>
        </bulletRef>
      </fire>
    </action>
  </bullet>
  
  <bullet label=""innerBullet"">
    <speed>$1/10</speed>
    <action>
      <wait>10</wait>
      <changeSpeed>
        <speed>3</speed>
        <term>20</term>
      </changeSpeed>
    </action>
  </bullet>
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
                
                // 全ての弾のアクションも実行
                for (int i = m_FiredBullets.Count - 1; i >= 0; i--)
                {
                    var bullet = m_FiredBullets[i];
                    if (bullet.ActionStack.Count > 0)
                    {
                        m_Executor.ExecuteCurrentAction(bullet);
                        bullet.UpdateChanges(DELTA_TIME);
                    }
                }
                
                if (m_FiredBullets.Count >= 2) break; // outer + inner
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // ネスト実行結果の確認
            Assert.AreEqual(2, m_FiredBullets.Count, "ネストしたbulletRefで2発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 2)
            {
                var outerBullet = m_FiredBullets[0];
                var innerBullet = m_FiredBullets[1];
                
                // outer弾の確認: bullet要素の速度が優先
                Assert.AreEqual(45f, outerBullet.Direction, 1f, "outer弾の方向は45度");
                Assert.AreEqual(1.5f, outerBullet.Speed, 0.1f, "outer弾の速度は1.5（bullet要素の値）");
                
                // inner弾の確認: 相対方向45+90=135度、速度は$1/10=45/10=4.5
                Assert.AreEqual(135f, innerBullet.Direction, 1f, "inner弾の方向は135度（45+90）");
                Assert.AreEqual(4.5f, innerBullet.Speed, 0.1f, "inner弾の速度は4.5（$1/10=45/10）");
            }
        }
        
        [Test]
        public void BulletRef_WithRepeat_CreatesMultipleBullets()
        {
            // repeat内でのbulletRef実行をテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>4</times>
      <action>
        <fire>
          <direction type=""sequence"">45</direction>
          <speed>2</speed>
          <bulletRef label=""repeatBullet"">
            <param>0.5</param>
          </bulletRef>
        </fire>
        <wait>5</wait>
      </action>
    </repeat>
  </action>
  
  <bullet label=""repeatBullet"">
    <speed>$1</speed>
    <action>
      <changeSpeed>
        <speed>3</speed>
        <term>20</term>
      </changeSpeed>
    </action>
  </bullet>
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
                
                if (m_FiredBullets.Count >= 4) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // repeat実行結果の確認
            Assert.AreEqual(4, m_FiredBullets.Count, "repeat内のbulletRefで4発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 4)
            {
                // sequence型方向の累積確認（45度ずつ増加）
                Assert.AreEqual(45f, m_FiredBullets[0].Direction, 1f, "1発目の方向は45度");
                Assert.AreEqual(90f, m_FiredBullets[1].Direction, 1f, "2発目の方向は90度");
                Assert.AreEqual(135f, m_FiredBullets[2].Direction, 1f, "3発目の方向は135度");
                Assert.AreEqual(180f, m_FiredBullets[3].Direction, 1f, "4発目の方向は180度");
                
                // 全弾同じ速度（bullet要素が優先）
                foreach (var bullet in m_FiredBullets)
                {
                    Assert.AreEqual(0.5f, bullet.Speed, 0.1f, "全弾の速度は0.5（bullet要素の$1値）であるべき");
                    Assert.Greater(bullet.ActionStack.Count, 0, "全弾にアクションが設定されているべき");
                }
            }
        }
        
        [Test]
        public void BulletRef_WithFireRef_CombinedExecution()
        {
            // bulletRefとfireRefの組み合わせをテスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>3</times>
      <action>
        <fireRef label=""combinedFire"">
          <param>60</param>
          <param>1.5</param>
        </fireRef>
        <wait>10</wait>
      </action>
    </repeat>
  </action>
  
  <fire label=""combinedFire"">
    <direction type=""sequence"">$1</direction>
    <speed>$2</speed>
    <bulletRef label=""combinedBullet"">
      <param>$1/3</param>
    </bulletRef>
  </fire>
  
  <bullet label=""combinedBullet"">
    <speed>$1/10</speed>
    <action>
      <wait>$1</wait>
      <fire>
        <direction type=""absolute"">180</direction>
        <speed>1</speed>
        <bullet/>
      </fire>
    </action>
  </bullet>
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
                
                // 全ての弾のアクションも実行
                for (int i = m_FiredBullets.Count - 1; i >= 0; i--)
                {
                    var bullet = m_FiredBullets[i];
                    if (bullet.ActionStack.Count > 0)
                    {
                        m_Executor.ExecuteCurrentAction(bullet);
                        bullet.UpdateChanges(DELTA_TIME);
                    }
                }
                
                if (m_FiredBullets.Count >= 3) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            // 組み合わせ実行結果の確認
            Assert.AreEqual(3, m_FiredBullets.Count, "fireRef+bulletRefの組み合わせで3発の弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // fireRefのsequence方向確認（60度ずつ増加）
                Assert.AreEqual(60f, m_FiredBullets[0].Direction, 1f, "1発目の方向は60度");
                Assert.AreEqual(120f, m_FiredBullets[1].Direction, 1f, "2発目の方向は120度");
                Assert.AreEqual(180f, m_FiredBullets[2].Direction, 1f, "3発目の方向は180度");
                
                // 全弾の速度確認（bullet要素の$1/10、$1=60（fireRefの値）を参照）
                foreach (var bullet in m_FiredBullets)
                {
                    Assert.AreEqual(6f, bullet.Speed, 0.1f, "全弾の速度は6（$1/10=60/10）であるべき");
                }
            }
        }
        
        [Test]
        public void BulletRef_Performance_LargeScale()
        {
            // 大量のbulletRef実行時のパフォーマンステスト
            
            string xmlContent = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
  <action label=""top"">
    <repeat>
      <times>100</times>
      <action>
        <fire>
          <direction type=""sequence"">3.6</direction>
          <speed>1.5</speed>
          <bulletRef label=""performanceBullet"">
            <param>0.5</param>
          </bulletRef>
        </fire>
      </action>
    </repeat>
  </action>
  
  <bullet label=""performanceBullet"">
    <speed>$1</speed>
    <action>
      <changeSpeed>
        <speed>2</speed>
        <term>10</term>
      </changeSpeed>
    </action>
  </bullet>
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
            for (int frame = 0; frame < 500; frame++)
            {
                bool hasAction = m_Executor.ExecuteCurrentAction(shooterBullet);
                shooterBullet.UpdateChanges(DELTA_TIME);
                
                if (m_FiredBullets.Count >= 100) break;
                if (!hasAction && shooterBullet.WaitFrames == 0) break;
            }
            
            var executionTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            
            // パフォーマンス確認
            Assert.AreEqual(100, m_FiredBullets.Count, "100回のbulletRef実行で100発の弾が生成されるべき");
            Assert.Less(executionTime, 1000, "100回のbulletRef実行が1秒以内で完了するべき");
            
            // sequence方向の正確性確認（最初の数発）
            if (m_FiredBullets.Count >= 3)
            {
                Assert.AreEqual(3.6f, m_FiredBullets[0].Direction, 0.1f, "1発目の方向は3.6度");
                Assert.AreEqual(7.2f, m_FiredBullets[1].Direction, 0.1f, "2発目の方向は7.2度");
                Assert.AreEqual(10.8f, m_FiredBullets[2].Direction, 0.1f, "3発目の方向は10.8度");
                
                // 全弾の速度確認（bullet要素が優先、$1=0.5）
                foreach (var bullet in m_FiredBullets.Take(10)) // 最初の10発をチェック
                {
                    Assert.AreEqual(0.5f, bullet.Speed, 0.1f, "全弾の速度は0.5（bullet要素の$1値）");
                    Assert.Greater(bullet.ActionStack.Count, 0, "bulletRefで生成された弾にアクションが設定されているべき");
                }
            }
        }
        
        [Test]
        public void BulletRef_InvalidLabel_HandlesGracefully()
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
    <fire>
      <direction type=""absolute"">90</direction>
      <speed>1.5</speed>
      <bulletRef label=""nonexistentBullet""/>
    </fire>
    <fire>
      <direction type=""absolute"">180</direction>
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
            
            // 不正なbulletRef参照の場合、エラーログは出力されず、通常のbullet要素として処理される
            
            // 実行（エラー時でも継続すること）
            bool noException = true;
            try
            {
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
            
            // 全ての弾が発射されることを確認（不正なbulletRefも通常のbullet要素として処理される）
            Assert.AreEqual(3, m_FiredBullets.Count, "不正参照があっても全ての弾が発射されるべき");
            
            if (m_FiredBullets.Count >= 3)
            {
                // 1発目の正常弾
                Assert.AreEqual(0f, m_FiredBullets[0].Direction, 1f, "1発目（正常）の方向は0度");
                Assert.AreEqual(1f, m_FiredBullets[0].Speed, 0.1f, "1発目（正常）の速度は1");
                
                // 2発目の不正bulletRef（通常のbullet要素として処理）
                Assert.AreEqual(90f, m_FiredBullets[1].Direction, 1f, "2発目（不正bulletRef）の方向は90度");
                Assert.AreEqual(1.5f, m_FiredBullets[1].Speed, 0.1f, "2発目（不正bulletRef）の速度は1.5");
                
                // 3発目の正常弾
                Assert.AreEqual(180f, m_FiredBullets[2].Direction, 1f, "3発目（正常）の方向は180度");
                Assert.AreEqual(2f, m_FiredBullets[2].Speed, 0.1f, "3発目（正常）の速度は2");
            }
        }
    }
}