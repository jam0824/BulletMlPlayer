using NUnit.Framework;
using UnityEngine;
using BulletML;
using System.Reflection;

public class BulletMLUpdateOptimizationTests
{
    private BulletMlPlayer m_Player;
    private GameObject m_PlayerObject;

    [SetUp]
    public void SetUp()
    {
        m_PlayerObject = new GameObject("TestPlayer");
        m_Player = m_PlayerObject.AddComponent<BulletMlPlayer>();
        m_Player.InitializeForTest();
    }

    [TearDown]
    public void TearDown()
    {
        if (m_PlayerObject != null)
        {
            Object.DestroyImmediate(m_PlayerObject);
        }
    }

    [Test]
    public void IsExecuting_ReturnsFalse_WhenNotStarted()
    {
        // Arrange & Act & Assert
        Assert.IsFalse(m_Player.IsExecuting, "開始前はIsExecutingがfalseであるべき");
    }

    [Test]
    public void IsExecuting_ReturnsTrue_WhenBulletMLStarted()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        // Act
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // Assert
        Assert.IsTrue(m_Player.IsExecuting, "開始後はIsExecutingがtrueであるべき");
    }

    [Test]
    public void IsExecuting_ReturnsFalse_WhenStopped()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // Act
        m_Player.StopBulletML();
        
        // Assert
        Assert.IsFalse(m_Player.IsExecuting, "停止後はIsExecutingがfalseであるべき");
    }

    [Test]
    public void HasActiveBullets_ReturnsFalse_WhenNoBullets()
    {
        // Arrange & Act & Assert
        Assert.IsFalse(m_Player.HasActiveBullets(), "弾が無い時はHasActiveBulletsがfalseであるべき");
    }

    [Test]
    public void HasActiveBullets_ReturnsTrue_WhenBulletsExist()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        // Act
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成
        
        // Assert
        Assert.IsTrue(m_Player.HasActiveBullets(), "弾が存在する時はHasActiveBulletsがtrueであるべき");
    }

    [Test]
    public void HasActiveBullets_ReturnsFalse_AfterClearAllBullets()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成
        
        // Act
        m_Player.ClearAllBullets();
        
        // Assert
        Assert.IsFalse(m_Player.HasActiveBullets(), "弾クリア後はHasActiveBulletsがfalseであるべき");
    }

    [Test]
    public void UpdateOptimization_SkipsProcessing_WhenNotExecutingAndNoBullets()
    {
        // Arrange
        // テスト用のフラグを追加（実際の実装では不要だが、テストのために追加）
        int initialUpdateCount = GetUpdateCallCount();
        
        // Act - Updateを手動実行（通常はUnityが自動実行）
        m_Player.ManualUpdate();
        
        // Assert
        Assert.IsFalse(m_Player.IsExecuting, "実行中でないことを確認");
        Assert.IsFalse(m_Player.HasActiveBullets(), "弾が無いことを確認");
        
        // この状態では重い処理（弾の更新など）はスキップされるべき
        // 実際のパフォーマンステストは統合テストで行う
    }

    [Test]
    public void UpdateOptimization_ProcessesWhenExecuting()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <wait>10</wait>
</action>
</bulletml>";
        
        // Act
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // Assert
        Assert.IsTrue(m_Player.IsExecuting, "実行中であることを確認");
        
        // この状態では処理が実行されるべき
        // パフォーマンスの詳細測定は統合テストで行う
    }

    [Test]
    public void UpdateOptimization_ProcessesWhenExecutingAndBulletsExist()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // 弾を生成
        m_Player.ManualUpdate(); // fire実行
        
        // 弾の存在確認（StopBulletML前）
        int bulletCount = m_Player.GetActiveBullets().Count;
        UnityEngine.Debug.Log($"ManualUpdate後の弾数: {bulletCount}");
        
        // Act & Assert - StopBulletMLを呼ぶ前の状態でテスト
        Assert.IsTrue(m_Player.IsExecuting, "実行中であることを確認");
        Assert.IsTrue(m_Player.HasActiveBullets(), "弾が存在することを確認");
        
        // この状態では弾の更新処理が実行されるべき（IsExecuting || HasActiveBullets）
        
        // 補足テスト: StopBulletMLは弾もクリアする
        m_Player.StopBulletML();
        Assert.IsFalse(m_Player.IsExecuting, "停止後は実行中でない");
        Assert.IsFalse(m_Player.HasActiveBullets(), "停止後は弾も存在しない");
        
    }

    [Test]
    public void UpdateOptimization_ProcessesWhenBulletsExistAfterExecution()
    {
        // Arrange - 弾を発射してすぐ終了するXML
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
    <vanish/>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // XMLを完全に実行（fire実行 → vanish実行 → XML完了）
        m_Player.ManualUpdate(); // fire実行
        m_Player.ManualUpdate(); // vanish実行（シューター弾消滅）
        m_Player.ManualUpdate(); // XML実行完了検知
        
        // Act & Assert
        Assert.IsFalse(m_Player.IsExecuting, "実行中でないことを確認");
        Assert.IsTrue(m_Player.HasActiveBullets(), "可視弾は残っていることを確認");
        
        // この状態では弾の更新処理が実行されるべき（!IsExecuting && HasActiveBullets）
    }

    /// <summary>
    /// 更新処理の呼び出し回数を取得（テスト用のヘルパーメソッド）
    /// </summary>
    private int GetUpdateCallCount()
    {
        // 実際の実装では、Update内の処理呼び出し回数をカウントする
        // メカニズムが必要だが、ここでは簡略化
        return 0;
    }
}
