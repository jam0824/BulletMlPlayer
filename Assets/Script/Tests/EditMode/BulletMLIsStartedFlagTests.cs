using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;

public class BulletMLIsStartedFlagTests
{
    private BulletMlPlayer m_Player;
    private GameObject m_PlayerObject;

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        
        m_PlayerObject = new GameObject("TestPlayer");
        m_Player = m_PlayerObject.AddComponent<BulletMlPlayer>();
        m_Player.InitializeForTest();
        m_Player.SetEnableDebugLog(false);
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        
        if (m_PlayerObject != null)
        {
            Object.DestroyImmediate(m_PlayerObject);
        }
    }

    [Test]
    public void IsExecuting_ReturnsFalse_AfterLoadBulletMLOnly()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        // Act - XMLを読み込むだけ（StartBulletML()は呼ばない）
        m_Player.LoadBulletML(testXml);
        
        // Assert - XMLを読み込んだだけでは実行中ではない
        Assert.IsFalse(m_Player.IsExecuting, "LoadBulletML()のみではIsExecutingはfalse");
        Assert.IsFalse(m_Player.HasActiveBullets(), "LoadBulletML()のみでは弾は生成されない");
    }

    [Test]
    public void IsExecuting_BecomesTrue_OnlyAfterStartBulletML()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        // Phase 1: 初期状態
        Assert.IsFalse(m_Player.IsExecuting, "初期状態ではIsExecutingはfalse");
        
        // Phase 2: XML読み込み後
        m_Player.LoadBulletML(testXml);
        Assert.IsFalse(m_Player.IsExecuting, "XML読み込み後でもIsExecutingはfalse");
        
        // Phase 3: StartBulletML()呼び出し後
        m_Player.StartBulletML();
        Assert.IsTrue(m_Player.IsExecuting, "StartBulletML()後のみIsExecutingはtrue");
    }

    [Test]
    public void IsExecuting_RequiresAllConditions()
    {
        // Phase 1: Document未設定
        Assert.IsFalse(m_Player.IsExecuting, "Document未設定ではfalse");
        
        // Phase 2: Document設定済み、StartTopAction未実行
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        
        // この時点では条件：
        // ✅ m_Document != null
        // ✅ m_Executor != null
        // ✅ m_Document.GetTopAction() != null
        // ❌ m_IsStarted == false  ← これが重要
        // ✅ !m_IsXmlExecutionCompleted
        // ✅ !m_IsStopped
        
        Assert.IsFalse(m_Player.IsExecuting, "全条件が満たされていなければfalse");
        
        // Phase 3: StartTopAction実行後
        m_Player.StartBulletML(); // これでm_IsStarted = trueになる
        
        Assert.IsTrue(m_Player.IsExecuting, "全条件が満たされればtrue");
    }

    [Test]
    public void IsExecuting_ResetsCorrectly_OnStop()
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
        
        Assert.IsTrue(m_Player.IsExecuting, "開始後は実行中");
        
        // Act
        m_Player.StopBulletML();
        
        // Assert
        Assert.IsFalse(m_Player.IsExecuting, "停止後は実行中でない");
        
        // 再開始も正常に動作することを確認
        m_Player.StartBulletML();
        Assert.IsTrue(m_Player.IsExecuting, "再開始も正常動作");
    }

    [Test]
    public void IsExecuting_HandlesErrorStates()
    {
        // Case 1: 不正XMLでは開始できない
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletMLの読み込みに失敗しました:.*"));
        m_Player.LoadBulletML("invalid xml");
        
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();
        
        Assert.IsFalse(m_Player.IsExecuting, "不正XML後は実行中でない");
        
        // Case 2: TopActionなしXMLでは開始できない
        string xmlWithoutTop = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""notTop"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(xmlWithoutTop);
        
        LogAssert.Expect(LogType.Error, "トップアクションが見つかりません");
        m_Player.StartBulletML();
        
        Assert.IsFalse(m_Player.IsExecuting, "TopActionなし後は実行中でない");
    }

    [Test]
    public void IsExecuting_UpdateOptimization_WorksWithIsStartedFlag()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
    <wait>5</wait>
</action>
</bulletml>";
        
        // Phase 1: 初期状態 - スキップ
        bool shouldSkip1 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip1, "初期状態ではUpdate処理をスキップ");
        
        // Phase 2: XML読み込み後 - まだスキップ
        m_Player.LoadBulletML(testXml);
        bool shouldSkip2 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip2, "XML読み込み後でもUpdate処理をスキップ");
        
        // Phase 3: StartBulletML()後 - 実行
        m_Player.StartBulletML();
        bool shouldSkip3 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsFalse(shouldSkip3, "StartBulletML()後はUpdate処理を実行");
        
        // Phase 4: StopBulletML()後 - スキップ
        m_Player.StopBulletML();
        bool shouldSkip4 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip4, "StopBulletML()後はUpdate処理をスキップ");
    }

    [Test]
    public void IsExecuting_InitializationSetsCorrectState()
    {
        // 新しいプレイヤーインスタンスを作成
        var newPlayerObject = new GameObject("NewTestPlayer");
        var newPlayer = newPlayerObject.AddComponent<BulletMlPlayer>();
        
        try
        {
            // 初期化前
            Assert.IsFalse(newPlayer.IsExecuting, "初期化前はIsExecutingはfalse");
            
            // 初期化後
            newPlayer.InitializeForTest();
            Assert.IsFalse(newPlayer.IsExecuting, "初期化後でもIsExecutingはfalse");
        }
        finally
        {
            Object.DestroyImmediate(newPlayerObject);
        }
    }
}
