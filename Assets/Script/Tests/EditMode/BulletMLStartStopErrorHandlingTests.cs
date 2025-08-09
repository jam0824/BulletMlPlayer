using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;

public class BulletMLStartStopErrorHandlingTests
{
    private BulletMlPlayer m_Player;
    private GameObject m_PlayerObject;

    [SetUp]
    public void SetUp()
    {
        // ログアサートを無効にしてエラーログを無視
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
    public void IsExecuting_ReturnsFalse_WhenInvalidXML()
    {
        // Arrange - 不正なXMLを読み込み
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletMLの読み込みに失敗しました:.*"));
        
        string invalidXml = "invalid xml content";
        m_Player.LoadBulletML(invalidXml);
        
        // Act
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();
        
        // Assert
        Assert.IsFalse(m_Player.IsExecuting, "不正XMLの場合IsExecutingはfalse");
        Assert.IsFalse(m_Player.HasActiveBullets(), "不正XMLの場合弾も生成されない");
    }

    [Test]
    public void IsExecuting_ReturnsFalse_WhenXMLWithoutTopAction()
    {
        // Arrange - topアクションが存在しないXML
        string xmlWithoutTop = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""notTop"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(xmlWithoutTop);
        
        // Act
        LogAssert.Expect(LogType.Error, "トップアクションが見つかりません");
        m_Player.StartBulletML();
        
        // Assert
        Assert.IsFalse(m_Player.IsExecuting, "topアクションなしの場合IsExecutingはfalse");
        Assert.IsFalse(m_Player.HasActiveBullets(), "topアクションなしの場合弾も生成されない");
    }

    [Test]
    public void IsExecuting_HandlesExecutorStateCorrectly()
    {
        // Arrange
        string validXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(validXml);
        
        // Executorが正しく初期化されていることを確認
        Assert.IsNotNull(m_Player.Document, "Documentが読み込まれている");
        Assert.IsNotNull(m_Player.Document.GetTopAction(), "TopActionが存在する");
        
        // Act
        m_Player.StartBulletML();
        
        // Assert
        Assert.IsTrue(m_Player.IsExecuting, "正常なXMLとExecutorでIsExecutingはtrue");
    }

    [Test]
    public void IsExecuting_UpdateOptimization_HandlesAllErrorCases()
    {
        // Case 1: 未初期化状態
        bool shouldSkip1 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip1, "未初期化時はUpdate処理をスキップ");
        
        // Case 2: 不正XML読み込み後
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletMLの読み込みに失敗しました:.*"));
        m_Player.LoadBulletML("invalid xml");
        
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();
        
        bool shouldSkip2 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip2, "不正XML後はUpdate処理をスキップ");
        
        // Case 3: topアクションなしXML読み込み後
        string xmlWithoutTop = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""notTop"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(xmlWithoutTop);
        
        LogAssert.Expect(LogType.Error, "トップアクションが見つかりません");
        m_Player.StartBulletML();
        
        bool shouldSkip3 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip3, "topアクションなし後はUpdate処理をスキップ");
        
        // Case 4: 正常XML読み込み後
        string validXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(validXml);
        m_Player.StartBulletML();
        
        bool shouldSkip4 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsFalse(shouldSkip4, "正常XML後はUpdate処理を実行");
    }

    [Test]
    public void IsExecuting_StopsCorrectly_AfterErrors()
    {
        // Arrange - エラー状態でもStopBulletMLが安全に動作するか
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletMLの読み込みに失敗しました:.*"));
        m_Player.LoadBulletML("invalid xml");
        
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();
        
        Assert.IsFalse(m_Player.IsExecuting, "エラー状態では実行中でない");
        
        // Act - エラー状態でStopBulletMLを呼び出し
        m_Player.StopBulletML(); // エラーが発生しないことを確認
        
        // Assert
        Assert.IsFalse(m_Player.IsExecuting, "エラー状態でのStop後も実行中でない");
        Assert.IsFalse(m_Player.HasActiveBullets(), "エラー状態でのStop後も弾なし");
    }

    [Test]
    public void IsExecuting_RestartAfterError_WorksCorrectly()
    {
        // Arrange - エラー後の正常復旧テスト
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletMLの読み込みに失敗しました:.*"));
        m_Player.LoadBulletML("invalid xml");
        
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();
        
        Assert.IsFalse(m_Player.IsExecuting, "エラー後は実行中でない");
        
        // Act - 正常なXMLで再開始
        string validXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(validXml);
        m_Player.StartBulletML();
        
        // Assert
        Assert.IsTrue(m_Player.IsExecuting, "正常XML読み込み後は実行中");
        
        // 停止も正常に動作することを確認
        m_Player.StopBulletML();
        Assert.IsFalse(m_Player.IsExecuting, "停止後は実行中でない");
    }
}
