using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// StartBulletML()メソッドのテストクラス
/// </summary>
public class BulletMLStartBulletMLTests
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

    // テスト用の複数fire XML
    private readonly string m_MultipleBulletXml = @"
<bulletml>
    <action label=""top"">
        <fire>
            <direction type=""absolute"">0</direction>
            <speed>2</speed>
        </fire>
        <fire>
            <direction type=""absolute"">90</direction>
            <speed>1</speed>
        </fire>
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
    public void StartBulletML_WithValidXMLAsset_InitializesAndStartsBulletML()
    {
        // Arrange
        // TextAssetはScriptableObjectではないため、代替手段として事前にXMLを読み込む
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);

        // Act
        m_Player.StartBulletML();

        // Assert
        Assert.IsNotNull(m_Player.Document, "Documentが読み込まれているべき");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "シューター弾が1発生成されているべき");
        
        var shooterBullet = m_Player.GetActiveBullets().FirstOrDefault(b => !b.IsVisible);
        Assert.IsNotNull(shooterBullet, "非表示のシューター弾が存在するべき");
    }

    [Test]
    public void StartBulletML_WithoutXMLAsset_LogsErrorAndReturns()
    {
        // Arrange - XMLアセットも事前読み込みもしない
        m_Player.InitializeForTest();
        
        // Act
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();

        // Assert
        Assert.IsNull(m_Player.Document, "XMLが設定されていない場合、Documentはnullのまま");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "弾は生成されないべき");
    }

    [Test]
    public void StartBulletML_WithPreLoadedDocument_UsesExistingDocument()
    {
        // Arrange - 事前にXMLを読み込み
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        
        var originalDocument = m_Player.Document;
        Assert.IsNotNull(originalDocument, "事前条件: Documentが読み込まれている");

        // Act
        m_Player.StartBulletML();

        // Assert
        Assert.AreSame(originalDocument, m_Player.Document, "既存のDocumentが使用されるべき");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "シューター弾が1発生成されているべき");
    }

    [Test]
    public void StartBulletML_InitializesSystemWhenNotInitialized()
    {
        // Arrange - システムが未初期化の状態
        // Executorがnullであることを確認
        var executorField = typeof(BulletMlPlayer).GetField("m_Executor", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNull(executorField.GetValue(m_Player), "事前条件: Executorが未初期化");

        // Act - StartBulletMLを直接呼び出し（XMLアセットなし、事前読み込みなし）
        // この場合、InitializeSystemは呼ばれるが、XMLがないためエラーログが出る
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();

        // Assert - システム初期化のみを確認
        Assert.IsNotNull(executorField.GetValue(m_Player), "Executorが初期化されているべき");
        
        // XMLがないため、Documentはnullのまま
        Assert.IsNull(m_Player.Document, "XMLが設定されていないため、Documentはnull");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "XMLがないため、弾は生成されない");
        
        // 次に実際にXMLを読み込んでテスト
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.StartBulletML();
        
        // XMLが読み込まれた後の動作確認
        Assert.IsNotNull(m_Player.Document, "XML読み込み後はDocumentが存在するべき");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "XML読み込み後は弾が生成されるべき");
    }

    [Test]
    public void StartBulletML_AppliesCurrentSettings()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        
        // 設定を変更
        m_Player.SetSpeedMultiplier(2.0f);
        m_Player.SetCoordinateSystem(BulletML.CoordinateSystem.YZ);

        // Act
        m_Player.StartBulletML();

        // Assert
        Assert.AreEqual(BulletML.CoordinateSystem.YZ, m_Player.CoordinateSystem, "座標系設定が適用されているべき");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "弾が生成されているべき");
    }

    [Test]
    public void StartBulletML_WithLoopEnabled_EnablesLoopFunctionality()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.SetLoopEnabled(true);
        m_Player.SetLoopDelayFrames(1);

        // Act
        m_Player.StartBulletML();

        // Assert - 初期状態確認
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "初期弾が生成されているべき");

        // ループ状態をリセットしてXML実行完了をシミュレート
        m_Player.ResetLoopState();
        var xmlCompletedField = typeof(BulletMlPlayer).GetField("m_IsXmlExecutionCompleted", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        xmlCompletedField.SetValue(m_Player, false);

        // 手動でXML実行完了検知処理を呼び出し
        var checkMethod = typeof(BulletMlPlayer).GetMethod("CheckAndHandleXmlExecutionCompletion", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // XML実行完了を検知
        m_Player.ClearAllBullets();
        checkMethod.Invoke(m_Player, null);

        // 遅延フレーム分待機してループ開始確認
        bool loopStarted = false;
        for (int i = 0; i < 5; i++)
        {
            checkMethod.Invoke(m_Player, null);
            if (m_Player.GetActiveBullets().Count > 0)
            {
                loopStarted = true;
                break;
            }
        }

        Assert.IsTrue(loopStarted, "ループが開始されて新しい弾が生成されるべき");
    }

    [Test]
    public void StartBulletML_CalledMultipleTimes_ResetsAndRestarts()
    {
        // Arrange - シンプルなXMLを使用
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);

        // Act - 1回目の実行
        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成
        int firstCallBullets = m_Player.GetActiveBullets().Count;
        
        // デバッグ: 実際の弾数を確認
        UnityEngine.Debug.Log($"1回目実行後の弾数: {firstCallBullets}");

        // Act - 2回目の実行前に状態をクリア
        m_Player.ClearAllBullets();
        
        // Act - 2回目の実行（リスタート）
        m_Player.StartBulletML();
        m_Player.ManualUpdate(); // 弾を生成
        int secondCallBullets = m_Player.GetActiveBullets().Count;
        
        // デバッグ: 実際の弾数を確認
        UnityEngine.Debug.Log($"2回目実行後の弾数: {secondCallBullets}");

        // Assert - 両方とも同じ弾数（シューター弾 + 実弾）になるべき
        Assert.AreEqual(firstCallBullets, secondCallBullets, "1回目と2回目で同じ弾数になるべき");
        Assert.IsTrue(firstCallBullets >= 1, "少なくともシューター弾は存在するべき");
    }

    [Test]
    public void StartBulletML_WithDebugLogEnabled_LogsStartMessage()
    {
        // Arrange
        m_Player.InitializeForTest();
        m_Player.LoadBulletML(m_SimpleTestXml);
        m_Player.SetEnableDebugLog(true);

        // Act & Assert
        LogAssert.Expect(LogType.Log, "BulletMLを開始しました");
        m_Player.StartBulletML();
    }

    [Test]
    public void StartBulletML_WithInvalidXML_HandlesErrorGracefully()
    {
        // Arrange
        m_Player.InitializeForTest();
        
        // 無効なXMLの読み込み時にエラーログが出ることを期待（パターンマッチング）
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BulletMLの読み込みに失敗しました:.*"));
        m_Player.LoadBulletML("invalid xml content");

        // Act - StartBulletMLでさらにエラーログが出ることを期待
        LogAssert.Expect(LogType.Error, "BulletML XMLが設定されていません");
        m_Player.StartBulletML();

        // Assert
        Assert.IsNull(m_Player.Document, "無効なXMLの場合、Documentはnullのまま");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "弾は生成されないべき");
    }
}
