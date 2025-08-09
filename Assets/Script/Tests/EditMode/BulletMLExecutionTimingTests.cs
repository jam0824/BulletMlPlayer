using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BulletML;
using System.Linq;

public class BulletMLExecutionTimingTests
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
    public void BulletMLExecution_TimingSequence_ShooterThenVisibleBullets()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        // Phase 1: 初期状態
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "初期状態では弾なし");
        
        // Phase 2: XML読み込み
        m_Player.LoadBulletML(testXml);
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count, "XML読み込み後も弾なし");
        
        // Phase 3: StartBulletML()実行 - シューター弾のみ生成
        m_Player.StartBulletML();
        
        int totalBulletsAfterStart = m_Player.GetActiveBullets().Count;
        int visibleBulletsAfterStart = m_Player.GetActiveBullets().Count(b => b.IsVisible);
        int invisibleBulletsAfterStart = m_Player.GetActiveBullets().Count(b => !b.IsVisible);
        
        Assert.AreEqual(1, totalBulletsAfterStart, "StartBulletML()後は1発（シューター弾）のみ");
        Assert.AreEqual(0, visibleBulletsAfterStart, "StartBulletML()直後は可視弾なし");
        Assert.AreEqual(1, invisibleBulletsAfterStart, "StartBulletML()直後は非可視弾（シューター）1発");
        
        // Phase 4: ManualUpdate()実行 - fire要素の実行により可視弾生成
        m_Player.ManualUpdate();
        
        int totalBulletsAfterUpdate = m_Player.GetActiveBullets().Count;
        int visibleBulletsAfterUpdate = m_Player.GetActiveBullets().Count(b => b.IsVisible);
        int invisibleBulletsAfterUpdate = m_Player.GetActiveBullets().Count(b => !b.IsVisible);
        
        Assert.AreEqual(2, totalBulletsAfterUpdate, "ManualUpdate()後は2発（シューター + 可視弾）");
        Assert.AreEqual(1, visibleBulletsAfterUpdate, "ManualUpdate()後は可視弾1発生成");
        Assert.AreEqual(1, invisibleBulletsAfterUpdate, "シューター弾は維持される");
    }

    [Test]
    public void BulletMLExecution_WaitCommand_DelaysExecution()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <wait>3</wait>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // シューター弾のみ存在
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "開始直後はシューター弾のみ");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾はまだなし");
        
        // Update 1: wait実行（WaitFrames=3設定、Index=1に進む）
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "Update 1: wait実行、弾数変化なし");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), "Update 1: 可視弾なし");
        
        // Update 2: wait継続（WaitFrames=2）
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "Update 2: wait継続、弾数変化なし");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), "Update 2: 可視弾なし");
        
        // Update 3: wait継続（WaitFrames=1）
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "Update 3: wait継続、弾数変化なし");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), "Update 3: 可視弾なし");
        
        // Update 4: wait継続（WaitFrames=0）
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "Update 4: wait継続、弾数変化なし");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), "Update 4: 可視弾なし");
        
        // Update 5: wait完了、fire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 5: wait完了後は弾が生成される");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count(b => b.IsVisible), "Update 5: wait完了後は可視弾1発");
    }

    [Test]
    public void BulletMLExecution_Wait1_DetailedSteps()
    {
        // Arrange - wait=1の詳細動作確認
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <wait>1</wait>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // 開始直後
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "開始直後: シューター弾のみ");
        
        // Update 1: wait実行（WaitFrames=1設定、Index進行）
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "Update 1: wait実行");
        
        // Update 2: wait継続（WaitFrames=0になる）
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "Update 2: wait継続、WaitFrames=0");
        
        // Update 3: wait完了、fire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 3: wait完了、fire実行");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾1発");
    }

    [Test]
    public void BulletMLExecution_WaitValues_FrameCount()
    {
        // wait値と実際のフレーム数の対応を確認
        
        // wait=0の場合
        string testXml0 = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <wait>0</wait>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml0);
        m_Player.StartBulletML();
        
        // Update 1: wait=0実行
        m_Player.ManualUpdate();
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "wait=0: Update 1");
        
        // Update 2: fire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "wait=0: 2フレーム目でfire実行");
        
        m_Player.ClearAllBullets();
        
        // wait=1の場合
        string testXml1 = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <wait>1</wait>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml1);
        m_Player.StartBulletML();
        
        // wait=1は3フレーム目でfire実行
        m_Player.ManualUpdate(); // Update 1: wait実行
        m_Player.ManualUpdate(); // Update 2: wait継続
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "wait=1: 2フレーム目はまだwait中");
        
        m_Player.ManualUpdate(); // Update 3: fire実行
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "wait=1: 3フレーム目でfire実行");
    }

    [Test]
    public void BulletMLExecution_MultipleFireCommands_OnePerUpdate()
    {
        // Arrange - 複数のfire要素
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
    <fire><bullet/></fire>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // シューター弾のみ
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "開始直後はシューター弾のみ");
        
        // 1回目の更新：1つ目のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "1回目更新後: シューター + 可視弾1発");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count(b => b.IsVisible), "1発目のfire実行");
        
        // 2回目の更新：2つ目のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(3, m_Player.GetActiveBullets().Count, "2回目更新後: シューター + 可視弾2発");
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count(b => b.IsVisible), "2発目のfire実行");
        
        // 3回目の更新：3つ目のfire実行
        m_Player.ManualUpdate();
        
        int totalBullets = m_Player.GetActiveBullets().Count;
        int visibleBullets = m_Player.GetActiveBullets().Count(b => b.IsVisible);
        
        Assert.AreEqual(4, totalBullets, "3回更新後: 3発のfire + 1発のシューター = 4発");
        Assert.AreEqual(3, visibleBullets, "3回更新後: 3発のfireにより可視弾3発生成");
    }

    [Test]
    public void BulletMLExecution_SingleCommandPerUpdate_DetailedTiming()
    {
        // Arrange - fireとwaitが混在するXML
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
    <wait>1</wait>
    <fire><bullet/></fire>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // シューター弾のみ
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "開始直後: シューター弾のみ");
        
        // Update 1: 1つ目のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 1: 1つ目のfire実行");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾1発");
        
        // Update 2: wait実行（WaitFrames = 1に設定、Index = 2に進む）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 2: wait実行、弾数変化なし");
        
        // Update 3: wait継続（WaitFrames = 0になる）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 3: wait継続、WaitFrames=0");
        
        // Update 4: wait完了、2つ目のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(3, m_Player.GetActiveBullets().Count, "Update 4: wait完了、2つ目のfire実行");
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾2発");
        
        // Update 5: 3つ目のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(4, m_Player.GetActiveBullets().Count, "Update 5: 3つ目のfire実行");
        Assert.AreEqual(3, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾3発");
        
        // Update 6: topアクション完了、それ以上の弾生成なし
        m_Player.ManualUpdate();
        Assert.AreEqual(4, m_Player.GetActiveBullets().Count, "Update 6: アクション完了、弾数維持");
    }

    [Test]
    public void BulletMLExecution_WaitBehavior_ExactTiming()
    {
        // Arrange - waitの動作を詳細テスト
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
    <wait>3</wait>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // 開始直後
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "開始直後: シューター弾のみ");
        
        // Update 1: fire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 1: fire実行");
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾1発");
        
        // Update 2: wait実行（WaitFrames=3設定、Index進行）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 2: wait開始");
        
        // Update 3: wait継続（WaitFrames=2になる）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 3: wait継続1");
        
        // Update 4: wait継続（WaitFrames=1になる）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 4: wait継続2");
        
        // Update 5: wait継続（WaitFrames=0になる）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 5: wait継続、WaitFrames=0");
        
        // Update 6: wait完了、次のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(3, m_Player.GetActiveBullets().Count, "Update 6: wait完了、fire実行");
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾2発");
    }

    [Test]
    public void BulletMLExecution_Wait0_ImmediateExecution()
    {
        // Arrange - wait=0の場合の動作確認
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
    <wait>0</wait>
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // Update 1: fire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 1: fire実行");
        
        // Update 2: wait=0実行（WaitFrames=0設定、Index進行）
        m_Player.ManualUpdate();
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count, "Update 2: wait=0実行");
        
        // Update 3: wait完了（WaitFrames=0なので即座に）、次のfire実行
        m_Player.ManualUpdate();
        Assert.AreEqual(3, m_Player.GetActiveBullets().Count, "Update 3: 次のfire実行");
        Assert.AreEqual(2, m_Player.GetActiveBullets().Count(b => b.IsVisible), "可視弾2発");
    }

    [Test]
    public void BulletMLExecution_UpdateOptimization_WorksWithTiming()
    {
        // Arrange
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <fire><bullet/></fire>
</action>
</bulletml>";
        
        // Phase 1: 読み込み前 - Update処理スキップ
        bool shouldSkip1 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip1, "読み込み前はUpdate処理をスキップ");
        
        // Phase 2: 読み込み後、開始前 - Update処理スキップ
        m_Player.LoadBulletML(testXml);
        bool shouldSkip2 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip2, "読み込み後、開始前はUpdate処理をスキップ");
        
        // Phase 3: 開始直後 - Update処理実行（シューター弾存在）
        m_Player.StartBulletML();
        bool shouldSkip3 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsFalse(shouldSkip3, "開始直後はUpdate処理を実行（IsExecuting=true）");
        
        // Phase 4: Update実行後 - Update処理実行（可視弾追加）
        m_Player.ManualUpdate();
        bool shouldSkip4 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsFalse(shouldSkip4, "Update実行後もUpdate処理を実行（弾存在）");
        
        // Phase 5: 停止後 - Update処理スキップ
        m_Player.StopBulletML();
        bool shouldSkip5 = !(m_Player.IsExecuting || m_Player.HasActiveBullets());
        Assert.IsTrue(shouldSkip5, "停止後はUpdate処理をスキップ");
    }

    [Test]
    public void BulletMLExecution_NoFireCommand_OnlyShooterBullet()
    {
        // Arrange - fire要素なしのXML
        string testXml = @"<?xml version=""1.0""?>
<bulletml type=""vertical"">
<action label=""top"">
    <wait>10</wait>
</action>
</bulletml>";
        
        m_Player.LoadBulletML(testXml);
        m_Player.StartBulletML();
        
        // シューター弾のみ
        Assert.AreEqual(1, m_Player.GetActiveBullets().Count, "開始直後はシューター弾のみ");
        Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), "fire要素なしなので可視弾なし");
        
        // 複数回更新しても可視弾は生成されない
        for (int i = 0; i < 5; i++)
        {
            m_Player.ManualUpdate();
            Assert.AreEqual(1, m_Player.GetActiveBullets().Count, $"{i+1}回目の更新後も弾数変化なし");
            Assert.AreEqual(0, m_Player.GetActiveBullets().Count(b => b.IsVisible), $"{i+1}回目の更新後も可視弾なし");
        }
    }
}
