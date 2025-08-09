using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// StopBulletML()メソッドのテストクラス
/// </summary>
public class BulletMLStopBulletMLTests
{
    private GameObject m_GameObject;
    private BulletMlPlayer m_Player;
    private GameObject m_BulletPrefab;

    // テスト用のシンプルなXML
    private readonly string m_SimpleTestXml = @"
<bulletml>
    <action label=""top"">
        <fire>
            <direction type=""absolute"">0</direction>
            <speed>2</speed>
        </fire>
    </action>
</bulletml>";

    // テスト用のループXML
    private readonly string m_LoopTestXml = @"
<bulletml>
    <action label=""top"">
        <repeat>
            <times>999</times>
            <action>
                <fire>
                    <direction type=""absolute"">0</direction>
                    <speed>1</speed>
                </fire>
                <wait>10</wait>
            </action>
        </repeat>
    </action>
</bulletml>";

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;

        // テスト用GameObjectとBulletMlPlayerを作成
        m_GameObject = new GameObject("TestBulletMlPlayer");
        m_Player = m_GameObject.AddComponent<BulletMlPlayer>();

        // テスト用弾プレハブを作成
        m_BulletPrefab = new GameObject("TestBullet");
        m_BulletPrefab.AddComponent<Rigidbody>();

        // BulletMlPlayerの基本設定
        var bulletPrefabField = typeof(BulletMlPlayer).GetField("m_BulletPrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bulletPrefabField.SetValue(m_Player, m_BulletPrefab);

        var autoStartField = typeof(BulletMlPlayer).GetField("m_AutoStart", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        autoStartField.SetValue(m_Player, false);

        m_Player.SetEnableDebugLog(false);
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (m_GameObject != null)
        {
            Object.DestroyImmediate(m_GameObject);
        }
        if (m_BulletPrefab != null)
        {
            Object.DestroyImmediate(m_BulletPrefab);
        }
    }

    [Test]
    public void StopBulletML_ClearsAllActiveBullets()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成

        // 弾が存在することを確認
        Assert.Greater(m_Player.GetActiveBullets().Count, 0, "事前条件: 弾が存在している");

        // Act
        m_Player.StopBulletML();

        // Assert
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "全ての弾がクリアされるべき");
    }

    [Test]
    public void StopBulletML_StopsLoopExecution()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_LoopTestXml);
        m_Player.SetLoopEnabled(true);
        m_Player.SetLoopDelayFrames(1);
        m_Player.StartBulletML();

        // Act
        m_Player.StopBulletML();

        // ループ状態フィールドを確認
        var isXmlCompletedField = typeof(BulletMlPlayer).GetField("m_IsXmlExecutionCompleted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var loopCounterField = typeof(BulletMlPlayer).GetField("m_LoopWaitFrameCounter", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsTrue((bool)isXmlCompletedField.GetValue(m_Player), "XML実行完了フラグがtrueになるべき");
        Assert.AreEqual(0, (int)loopCounterField.GetValue(m_Player), "ループカウンターがリセットされるべき");
    }

    [Test]
    public void StopBulletML_ClearsShooterBullet()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.StartBulletML();

        // シューター弾が存在することを確認
        var shooterBulletField = typeof(BulletMlPlayer).GetField("m_ShooterBullet", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(shooterBulletField.GetValue(m_Player), "事前条件: シューター弾が存在している");

        // Act
        m_Player.StopBulletML();

        // Assert
        Assert.IsNull(shooterBulletField.GetValue(m_Player), "シューター弾がクリアされるべき");
    }

    [Test]
    public void StopBulletML_WithDebugLogEnabled_LogsStopMessage()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.StartBulletML();
        m_Player.SetEnableDebugLog(true);

        // Act & Assert
        LogAssert.Expect(LogType.Log, "BulletMLを停止しました");
        m_Player.StopBulletML();
    }

    [Test]
    public void StopBulletML_CalledWithoutStarting_HandlesGracefully()
    {
        // Arrange - 何も開始していない状態
        m_Player.InitializeForTest();

        // Act - 開始していない状態でStopBulletMLを呼び出し
        m_Player.StopBulletML();

        // Assert - エラーなく正常終了
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "弾数は0のまま");
        
        var shooterBulletField = typeof(BulletMlPlayer).GetField("m_ShooterBullet", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNull(shooterBulletField.GetValue(m_Player), "シューター弾はnullのまま");
    }

    [Test]
    public void StopBulletML_AfterStop_CanRestartWithStartBulletML()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        
        // Act - 開始→停止→再開始
        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成
        int firstRunBullets = m_Player.GetActiveBullets().Count;

        m_Player.StopBulletML();
        int afterStopBullets = m_Player.GetActiveBullets().Count;

        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成
        int afterRestartBullets = m_Player.GetActiveBullets().Count;

        // Assert
        Assert.Greater(firstRunBullets, 0, "1回目実行後: 弾が存在するべき");
        Assert.AreEqual(0, afterStopBullets, "停止後: 弾がクリアされるべき");
        Assert.Greater(afterRestartBullets, 0, "再開始後: 弾が再び生成されるべき");
    }

    [Test]
    public void StopBulletML_PreventsLoopRestart()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.SetLoopEnabled(true);
        m_Player.SetLoopDelayFrames(1);
        m_Player.StartBulletML();

        // Act
        m_Player.StopBulletML();

        // ループ処理を手動で数回実行（通常ならループが再開するはず）
        var checkMethod = typeof(BulletMlPlayer).GetMethod("CheckAndHandleXmlExecutionCompletion", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        for (int i = 0; i < 5; i++)
        {
            checkMethod.Invoke(m_Player, null);
        }

        // Assert - ループが再開されないことを確認
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "ループが再開されず、弾は生成されないべき");
    }

    [Test]
    public void StopBulletML_MultipleCalls_HandlesGracefully()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.StartBulletML();

        // Act - 複数回StopBulletMLを呼び出し
        m_Player.StopBulletML();
        m_Player.StopBulletML();
        m_Player.StopBulletML();

        // Assert - エラーなく正常終了
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "弾数は0のまま");
        
        var shooterBulletField = typeof(BulletMlPlayer).GetField("m_ShooterBullet", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNull(shooterBulletField.GetValue(m_Player), "シューター弾はnullのまま");
    }
}
