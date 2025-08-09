using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;
using System.Diagnostics;

public class BulletMLUpdatePerformanceTests
{
    private BulletMlPlayer m_Player;
    private GameObject m_PlayerObject;

    [SetUp]
    public void SetUp()
    {
        m_PlayerObject = new GameObject("TestPlayer");
        m_Player = m_PlayerObject.AddComponent<BulletMlPlayer>();
        m_Player.InitializeForTest();
        m_Player.SetEnableDebugLog(false); // パフォーマンステストではログ無効
    }

    [TearDown]
    public void TearDown()
    {
        if (m_PlayerObject != null)
        {
            Object.DestroyImmediate(m_PlayerObject);
        }
    }

    [UnityTest]
    public IEnumerator UpdatePerformance_IdleState_ShouldBeFast()
    {
        // Arrange - アイドル状態（実行中でなく、弾も無し）
        Assert.IsFalse(m_Player.IsExecuting, "実行中でないことを確認");
        Assert.IsFalse(m_Player.HasActiveBullets(), "弾が無いことを確認");
        
        // Act - 10000回のUpdate処理の実行時間を測定（精度向上）
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            m_Player.ManualUpdate();
        }
        
        stopwatch.Stop();
        var timeMicroseconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000;
        
        // Assert - アイドル状態では非常に高速であるべき（10000回で5ms = 5000μs未満）
        Assert.Less(timeMicroseconds, 5000, 
            "アイドル状態での10000回Update処理は5000μs未満であるべき（実際: {0:F1}μs）", timeMicroseconds);
        
        UnityEngine.Debug.Log($"アイドルパフォーマンス - 10000回処理時間: {timeMicroseconds:F1}μs");
        
        yield return null;
    }

    [UnityTest]
    public IEnumerator UpdatePerformance_ActiveState_WithinReasonableTime()
    {
        // Arrange - アクティブ状態（弾幕実行中）
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <repeat><times>100</times>
        <action>
            <fire><bullet/></fire>
            <wait>1</wait>
        </action>
    </repeat>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // 数フレーム実行して弾を生成
        for (int i = 0; i < 10; i++)
        {
            m_Player.ManualUpdate();
        }
        
        Assert.IsTrue(m_Player.IsExecuting || m_Player.HasActiveBullets(), 
            "実行中または弾が存在することを確認");
        
        // Act - アクティブ状態での1000回Update処理の実行時間を測定（精度向上）
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            m_Player.ManualUpdate();
        }
        
        stopwatch.Stop();
        var timeMicroseconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000;
        
        // Assert - アクティブ状態でも合理的な時間内であるべき（1000回で20ms = 20000μs未満）
        Assert.Less(timeMicroseconds, 20000, 
            "アクティブ状態での1000回Update処理は20000μs未満であるべき（実際: {0:F1}μs）", timeMicroseconds);
        
        UnityEngine.Debug.Log($"アクティブパフォーマンス - 1000回処理時間: {timeMicroseconds:F1}μs");
        
        yield return null;
    }

    [UnityTest]
    public IEnumerator UpdatePerformance_CompareIdleVsActive()
    {
        // Phase 1: アイドル状態の測定（より多くの反復で精度向上）
        var idleStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++) // 反復回数を大幅に増加
        {
            m_Player.ManualUpdate();
        }
        idleStopwatch.Stop();
        var idleTimeTicks = idleStopwatch.ElapsedTicks;
        var idleTimeMicroseconds = (double)idleTimeTicks / Stopwatch.Frequency * 1000000;
        
        yield return null;
        
        // Phase 2: アクティブ状態の測定
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <repeat><times>200</times>
        <action>
            <fire><bullet/></fire>
            <wait>1</wait>
        </action>
    </repeat>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // 弾を生成
        for (int i = 0; i < 50; i++)
        {
            m_Player.ManualUpdate();
        }
        
        Assert.IsTrue(m_Player.IsExecuting || m_Player.HasActiveBullets(), 
            "アクティブ状態であることを確認");
        
        var activeStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++) // 同じ反復回数
        {
            m_Player.ManualUpdate();
        }
        activeStopwatch.Stop();
        var activeTimeTicks = activeStopwatch.ElapsedTicks;
        var activeTimeMicroseconds = (double)activeTimeTicks / Stopwatch.Frequency * 1000000;
        
        // Assert - アイドル状態がアクティブ状態より高速であることを確認
        Assert.Less(idleTimeTicks, activeTimeTicks, 
            "アイドル状態（{0}μs）はアクティブ状態（{1}μs）より高速であるべき", 
            idleTimeMicroseconds, activeTimeMicroseconds);
        
        // 統計的に意味のある差があることを確認（最低50%以上の差）
        Assert.Less(idleTimeTicks * 1.5, activeTimeTicks, 
            "アイドル状態はアクティブ状態より最低50%は高速であるべき（アイドル×1.5 < アクティブ）");
        
        UnityEngine.Debug.Log($"パフォーマンス比較 - アイドル: {idleTimeMicroseconds:F1}μs, アクティブ: {activeTimeMicroseconds:F1}μs, 比率: {activeTimeMicroseconds / idleTimeMicroseconds:F2}x");
        
        yield return null;
    }

    [UnityTest]
    public IEnumerator UpdatePerformance_ManyBullets_StillFast()
    {
        // Arrange - 大量の弾を生成（より確実な方法）
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <repeat><times>100</times>
        <action>
            <fire><bullet/></fire>
        </action>
    </repeat>
</action>
</bulletml>";
        
        m_Player.SetMaxBullets(300); // 上限を上げる
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // 十分な回数のUpdateで大量弾を生成（repeatの100回分 + 余裕）
        for (int i = 0; i < 120; i++)
        {
            m_Player.ManualUpdate();
            
            // 早期終了条件：十分な弾数に達したら停止
            if (m_Player.GetActiveBullets().Count >= 80)
            {
                UnityEngine.Debug.Log($"大量弾生成完了: {i + 1}回目のUpdateで{m_Player.GetActiveBullets().Count}発");
                break;
            }
        }
        
        var bulletCount = m_Player.GetActiveBullets().Count;
        Assert.Greater(bulletCount, 30, "大量の弾が生成されていることを確認（30発以上）");
        
        UnityEngine.Debug.Log($"大量弾テスト開始 - 弾数: {bulletCount}発");
        
        // Act - 大量の弾がある状態での処理時間測定
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 60; i++) // 1秒分（60FPS）
        {
            m_Player.ManualUpdate();
        }
        
        stopwatch.Stop();
        
        var timeMicroseconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000;
        
        // Assert - 大量の弾があっても60FPSを維持できる程度であるべき（60フレームで16ms/frame = 960ms = 960000μs）
        Assert.Less(timeMicroseconds, 960000, 
            "大量弾での60フレーム処理は960000μs未満であるべき（実際: {0:F1}μs, 弾数: {1}）", 
            timeMicroseconds, bulletCount);
        
        UnityEngine.Debug.Log($"大量弾パフォーマンス - 弾数: {bulletCount}, 60フレーム処理時間: {timeMicroseconds:F1}μs");
        
        yield return null;
    }
}
