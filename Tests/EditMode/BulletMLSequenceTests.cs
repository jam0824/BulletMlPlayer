using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;
using System.Collections.Generic;

/// <summary>
/// sequence型の要素のテストクラス
/// </summary>
public class BulletMLSequenceTests
{
    private BulletMLParser m_Parser;
    private BulletMLExecutor m_Executor;
    private BulletMLBullet m_Bullet;
    private List<BulletMLBullet> m_CreatedBullets;

    [SetUp]
    public void Setup()
    {
        m_Parser = new BulletMLParser();
        m_Executor = new BulletMLExecutor();
        m_CreatedBullets = new List<BulletMLBullet>();
        
        // 新しい弾が作成された時のコールバック設定
        m_Executor.OnBulletCreated += (_bullet) => m_CreatedBullets.Add(_bullet);
        
        // 座標系を設定
        m_Executor.SetCoordinateSystem(CoordinateSystem.YZ);
        
        // ターゲット位置を設定
        m_Executor.SetTargetPosition(new Vector3(0, 0, -5));
        
        // sequence値をリセット（重要：他のテストの影響を避ける）
        m_Executor.ResetSequenceValues();
        
        // テスト用の弾を作成（shooter）
        m_Bullet = new BulletMLBullet(Vector3.zero, 0f, 0f, CoordinateSystem.YZ, false);
    }

    /// <summary>
    /// speed sequence型が直前の弾の速度との相対値として動作するかのテスト
    /// </summary>
    [Test]
    public void SpeedSequenceType_InFireElement_UsesLastBulletSpeed()
    {
        // Arrange: sequence型speed要素を持つ複数のfire要素
        string xml = @"
        <bulletml>
            <action label=""top"">
                <fire>
                    <speed>1</speed>
                    <bullet/>
                </fire>
                <fire>
                    <speed type=""sequence"">1</speed>
                    <bullet/>
                </fire>
                <fire>
                    <speed type=""sequence"">0.5</speed>
                    <bullet/>
                </fire>
            </action>
        </bulletml>";
        
        // Act: XMLをパースし、連続して弾を発射
        var document = m_Parser.Parse(xml);
        m_Executor.SetDocument(document);
        
        var topAction = document.GetTopAction();
        var actionRunner = new BulletMLActionRunner(topAction);
        m_Bullet.PushAction(actionRunner);
        
        m_CreatedBullets.Clear();
        
        // 1番目の弾を発射（速度1）
        bool result1 = m_Executor.ExecuteCurrentAction(m_Bullet);
        Assert.IsTrue(result1, "1番目の弾の発射に失敗");
        
        // 2番目の弾を発射（sequence: 前の弾(1) + 1 = 2）
        bool result2 = m_Executor.ExecuteCurrentAction(m_Bullet);
        Assert.IsTrue(result2, "2番目の弾の発射に失敗");
        
        // 3番目の弾を発射（sequence: 前の弾(2) + 0.5 = 2.5）
        bool result3 = m_Executor.ExecuteCurrentAction(m_Bullet);
        Assert.IsTrue(result3, "3番目の弾の発射に失敗");
        
        // Assert: 速度が期待通りに設定される
        Assert.AreEqual(3, m_CreatedBullets.Count, "3発の弾が発射されるべき");
        
        Assert.AreEqual(1f, m_CreatedBullets[0].Speed, 0.01f, "1番目の弾の速度が1であるべき");
        Assert.AreEqual(2f, m_CreatedBullets[1].Speed, 0.01f, "2番目の弾の速度が2であるべき (1+1)");
        Assert.AreEqual(2.5f, m_CreatedBullets[2].Speed, 0.01f, "3番目の弾の速度が2.5であるべき (2+0.5)");
    }

    /// <summary>
    /// changeSpeed内でのspeed sequence型の連続変化テスト
    /// </summary>
    [Test]
    public void SpeedSequenceType_InChangeSpeed_ContinuousChange()
    {
        // Arrange: changeSpeed内でsequence型speedを使用
        string xml = @"
        <bulletml>
            <bullet>
                <speed>1</speed>
                <action>
                    <changeSpeed>
                        <speed type=""sequence"">1</speed>
                        <term>60</term>
                    </changeSpeed>
                    <wait>60</wait>
                    <changeSpeed>
                        <speed type=""sequence"">0.5</speed>
                        <term>60</term>
                    </changeSpeed>
                </action>
            </bullet>
        </bulletml>";
        
        // Act: XMLをパースし、弾を作成してchangeSpeedを実行
        var document = m_Parser.Parse(xml);
        m_Executor.SetDocument(document);
        
        var bulletElement = document.GetTopBullet();
        Assert.IsNotNull(bulletElement, "トップbullet要素が見つかりません");
        
        // 弾を作成
        var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, true);
        m_Executor.ApplyBulletElement(bulletElement, testBullet);
        
        // 最初のchangeSpeed実行前の速度確認
        Assert.AreEqual(1f, testBullet.Speed, 0.01f, "初期速度が1であるべき");
        
        // 最初のchangeSpeedを実行
        bool result1 = m_Executor.ExecuteCurrentAction(testBullet);
        Assert.IsTrue(result1, "最初のchangeSpeedの実行に失敗");
        
        // changeSpeedが開始されたことを確認
        Assert.IsTrue(testBullet.SpeedChange.IsActive, "速度変更が開始されるべき");
        
        // 60フレーム後に2番目のchangeSpeedが実行される想定
        for (int i = 0; i < 60; i++)
        {
            testBullet.UpdateChanges(1f / 60f);
            m_Executor.ExecuteCurrentAction(testBullet);
        }
        
        // 2番目のchangeSpeed実行後、連続変化の値が適用されているかを確認
        // sequence型では前の変更値に追加される形になるべき
        float expectedSpeed = 2f; // 1 + 1 (最初のsequence)
        Assert.That(testBullet.Speed, Is.EqualTo(expectedSpeed).Within(0.1f), 
            $"1回目のsequence changeSpeed後の速度が{expectedSpeed}付近であるべき");
    }

    /// <summary>
    /// horizontal/vertical sequence型の連続変化テスト
    /// </summary>
    [Test]
    public void AccelSequenceType_ContinuousChange()
    {
        // Arrange: sequence型horizontal/verticalを使用するaccel
        string xml = @"
        <bulletml>
            <bullet>
                <action>
                    <accel>
                        <horizontal type=""sequence"">0.1</horizontal>
                        <vertical type=""sequence"">0.2</vertical>
                        <term>30</term>
                    </accel>
                    <wait>30</wait>
                    <accel>
                        <horizontal type=""sequence"">0.05</horizontal>
                        <vertical type=""sequence"">0.1</vertical>
                        <term>30</term>
                    </accel>
                </action>
            </bullet>
        </bulletml>";
        
        // Act: XMLをパースし、弾を作成してaccelを実行
        var document = m_Parser.Parse(xml);
        m_Executor.SetDocument(document);
        
        var bulletElement = document.GetTopBullet();
        Assert.IsNotNull(bulletElement, "トップbullet要素が見つかりません");
        
        // 弾を作成
        var testBullet = new BulletMLBullet(Vector3.zero, 0f, 1f, CoordinateSystem.YZ, true);
        m_Executor.ApplyBulletElement(bulletElement, testBullet);
        
        // 初期状態を確認
        var initialAction = testBullet.GetCurrentAction();
        Debug.Log($"Initial: CurrentIndex={initialAction?.CurrentIndex}, ActionStack={testBullet.ActionStack.Count}");
        
        // XMLの構造を詳しく確認
        var actionElement = initialAction?.ActionElement;
        if (actionElement != null)
        {
            Debug.Log($"Action has {actionElement.Children.Count} children:");
            for (int i = 0; i < actionElement.Children.Count; i++)
            {
                var child = actionElement.Children[i];
                Debug.Log($"  [{i}] {child.ElementType}");
            }
        }
        
        // 正確な検出ロジック：CurrentIndexとElementTypeで判別
        bool firstAccelExecuted = false;
        bool secondAccelExecuted = false;
        
        for (int i = 0; i < 70; i++) // 十分な期間実行
        {
            var currentAction = testBullet.GetCurrentAction();
            if (currentAction == null) break;
            
            var currentActionElement = currentAction.ActionElement;
            var currentCommand = (currentActionElement != null && currentAction.CurrentIndex < currentActionElement.Children.Count) 
                ? currentActionElement.Children[currentAction.CurrentIndex] : null;
            
            // 詳細ログ（最初の10フレームのみ）
            if (i < 10)
            {
                Debug.Log($"Frame {i + 1}: CurrentIndex={currentAction.CurrentIndex}, Command={currentCommand?.ElementType}, WaitFrames={currentAction.WaitFrames}");
            }
            
            // 1回目のaccel実行を検出（CurrentIndex=0 かつ accel）
            if (currentAction.CurrentIndex == 0 && currentCommand?.ElementType == BulletMLElementType.accel && !firstAccelExecuted)
            {
                firstAccelExecuted = true;
                Debug.Log($"Frame {i + 1}: 1回目のaccel実行！CurrentIndex={currentAction.CurrentIndex}");
                Debug.Log($"AccelInfo before: H={testBullet.AccelInfo.HorizontalAccel}, V={testBullet.AccelInfo.VerticalAccel}");
            }
            
            // 2回目のaccel実行を検出（CurrentIndex=2 かつ accel）
            if (currentAction.CurrentIndex == 2 && currentCommand?.ElementType == BulletMLElementType.accel && currentAction.WaitFrames == 0 && !secondAccelExecuted)
            {
                secondAccelExecuted = true;
                Debug.Log($"Frame {i + 1}: 2回目のaccel実行！CurrentIndex={currentAction.CurrentIndex}");
                Debug.Log($"AccelInfo before: H={testBullet.AccelInfo.HorizontalAccel}, V={testBullet.AccelInfo.VerticalAccel}");

            }
            
            testBullet.UpdateChanges(1f / 60f);
            m_Executor.ExecuteCurrentAction(testBullet);
            
            // 2番目のaccel実行後にループを抜ける
            if (secondAccelExecuted && i > 35) // wait期間(30)+α後にテスト終了
            {
                break;
            }
        }
        
        Assert.IsTrue(firstAccelExecuted, "1回目のaccelが実行されていない");
        Assert.IsTrue(secondAccelExecuted, "2回目のaccelが実行されていない");
        
        // 2番目のaccel実行後、sequence累積が機能するかテスト
        testBullet.UpdateChanges(1f / 60f);
        
        // AccelInfoを確認（sequence累積されているべき）
        Debug.Log($"After 2nd accel execution:");
        Debug.Log($"AccelInfo - Horizontal: {testBullet.AccelInfo.HorizontalAccel}, Vertical: {testBullet.AccelInfo.VerticalAccel}");
        Debug.Log($"AccelInfo - CurrentFrame: {testBullet.AccelInfo.CurrentFrame}, Duration: {testBullet.AccelInfo.Duration}");
        Debug.Log($"Bullet.Acceleration: {testBullet.Acceleration}");
        
        // sequence累積をテスト（0.1+0.05=0.15, 0.2+0.1=0.3）
        Assert.AreEqual(0.15f, testBullet.AccelInfo.HorizontalAccel, 0.001f, "Horizontal sequence累積が正しくない");
        Assert.AreEqual(0.3f, testBullet.AccelInfo.VerticalAccel, 0.001f, "Vertical sequence累積が正しくない");
        
        // CurrentFrameに基づく加速度をテスト
        float t = (float)testBullet.AccelInfo.CurrentFrame / testBullet.AccelInfo.Duration;
        Vector3 expectedAccel = new Vector3(0f, 0.3f * t, 0.15f * t); // YZ座標系
        Debug.Log($"CurrentFrame: {testBullet.AccelInfo.CurrentFrame}, Duration: {testBullet.AccelInfo.Duration}, t: {t}");
        Debug.Log($"Expected: {expectedAccel}, Actual: {testBullet.Acceleration}");
        Assert.That(Vector3.Distance(testBullet.Acceleration, expectedAccel), Is.LessThan(0.01f),
            "sequence累積後の加速度が期待値と異なる");
    }

    /// <summary>
    /// direction sequence型が直前の方向との相対値として動作するかのテスト
    /// </summary>
    [Test]
    public void DirectionSequenceType_UsesLastDirection()
    {
        // Arrange: sequence型direction要素を持つ複数のfire要素
        string xml = @"
        <bulletml>
            <action label=""top"">
                <fire>
                    <direction type=""absolute"">0</direction>
                    <bullet/>
                </fire>
                <fire>
                    <direction type=""sequence"">30</direction>
                    <bullet/>
                </fire>
                <fire>
                    <direction type=""sequence"">45</direction>
                    <bullet/>
                </fire>
            </action>
        </bulletml>";
        
        // Act: XMLをパースし、連続して弾を発射
        var document = m_Parser.Parse(xml);
        m_Executor.SetDocument(document);
        
        var topAction = document.GetTopAction();
        var actionRunner = new BulletMLActionRunner(topAction);
        m_Bullet.PushAction(actionRunner);
        
        m_CreatedBullets.Clear();
        
        // 1番目の弾を発射（方向0度）
        bool result1 = m_Executor.ExecuteCurrentAction(m_Bullet);
        Assert.IsTrue(result1, "1番目の弾の発射に失敗");
        
        // 2番目の弾を発射（sequence: 前の弾(0) + 30 = 30）
        bool result2 = m_Executor.ExecuteCurrentAction(m_Bullet);
        Assert.IsTrue(result2, "2番目の弾の発射に失敗");
        
        // 3番目の弾を発射（sequence: 前の弾(30) + 45 = 75）
        bool result3 = m_Executor.ExecuteCurrentAction(m_Bullet);
        Assert.IsTrue(result3, "3番目の弾の発射に失敗");
        
        // Assert: 方向が期待通りに設定される
        Assert.AreEqual(3, m_CreatedBullets.Count, "3発の弾が発射されるべき");
        
        Assert.AreEqual(0f, m_CreatedBullets[0].Direction, 0.01f, "1番目の弾の方向が0度であるべき");
        Assert.AreEqual(30f, m_CreatedBullets[1].Direction, 0.01f, "2番目の弾の方向が30度であるべき (0+30)");
        Assert.AreEqual(75f, m_CreatedBullets[2].Direction, 0.01f, "3番目の弾の方向が75度であるべき (30+45)");
    }
}