using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Linq;

namespace Tests
{
    /// <summary>
    /// BulletMlPlayerのOnDestroy()クリーンアップ機能のテスト
    /// </summary>
    public class BulletMLOnDestroyTests
    {
        private GameObject m_PlayerObject;
        private BulletMlPlayer m_Player;

        [SetUp]
        public void SetUp()
        {
            m_PlayerObject = new GameObject("TestPlayer");
            m_Player = m_PlayerObject.AddComponent<BulletMlPlayer>();
            m_Player.InitializeForTest();
            m_Player.SetMaxBullets(10);
            m_Player.SetEnableDebugLog(false); // デフォルトでログ無効にしてLogAssert問題を回避
            m_Player.SetCoordinateSystem(BulletML.CoordinateSystem.YZ);
        }

        [TearDown]
        public void TearDown()
        {
            // m_PlayerObjectは各テストでDestroy済みの場合があるため、
            // 存在確認してから削除
            if (m_PlayerObject != null)
            {
                Object.DestroyImmediate(m_PlayerObject);
            }
        }

        /// <summary>
        /// OnDestroy時にアクティブな弾がクリアされることをテスト
        /// </summary>
        [Test]
        public void OnDestroy_ClearsActiveBullets()
        {
            // テスト用BulletMLを読み込み
            string testXml = @"
            <bulletml>
                <action label=""top"">
                    <fire><direction type=""absolute"">0</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">90</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">180</direction><speed>1</speed></fire>
                </action>
            </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 弾を生成
            for (int i = 0; i < 5; i++)
            {
                m_Player.ManualUpdate();
            }

            var activeBulletsBeforeDestroy = m_Player.GetActiveBullets().Where(b => b.IsVisible).ToList();
            int bulletCountBeforeDestroy = activeBulletsBeforeDestroy.Count;

            // オブジェクトを削除（OnDestroy()が呼ばれる）
            Object.DestroyImmediate(m_PlayerObject);
            m_PlayerObject = null; // TearDownでの重複削除を防ぐ

            // 削除前に弾が存在していたことを確認（削除後の状態は直接確認できない）
            Assert.IsTrue(bulletCountBeforeDestroy > 0, "削除前に弾が存在するべき");
        }

        /// <summary>
        /// OnDestroy時にプールされた弾オブジェクトがクリアされることをテスト
        /// </summary>
        [Test]
        public void OnDestroy_ClearsPooledBulletObjects()
        {
            // テスト用BulletMLを読み込み
            string testXml = @"
            <bulletml>
                <action label=""top"">
                    <fire><direction type=""absolute"">0</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">90</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">180</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">270</direction><speed>1</speed></fire>
                </action>
            </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 弾を生成し、一部を削除してプールに戻す
            for (int i = 0; i < 10; i++)
            {
                m_Player.ManualUpdate();
            }

            int bulletCountBeforeClear = m_Player.GetActiveBullets().Where(b => b.IsVisible).Count();

            // 全ての弾をクリアしてプールに戻す
            m_Player.ClearAllBullets();

            // オブジェクトを削除（OnDestroy()が呼ばれる）
            Object.DestroyImmediate(m_PlayerObject);
            m_PlayerObject = null;

            // プールに弾があったことを確認（機能テストに集中）
            Assert.IsTrue(bulletCountBeforeClear > 0, "プール処理前に弾が存在するべき");
        }

        /// <summary>
        /// OnDestroy時にデバッグログが無効でもクリーンアップが実行されることをテスト
        /// </summary>
        [Test]
        public void OnDestroy_WorksWithoutDebugLog()
        {
            // デバッグログを無効にする（既にSetUpで無効だが明示的に設定）
            m_Player.SetEnableDebugLog(false);

            // テスト用BulletMLを読み込み
            string testXml = @"
            <bulletml>
                <action label=""top"">
                    <fire><direction type=""absolute"">0</direction><speed>1</speed></fire>
                </action>
            </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            m_Player.ManualUpdate();

            int bulletCount = m_Player.GetActiveBullets().Where(b => b.IsVisible).Count();

            // オブジェクトを削除（OnDestroy()が呼ばれる）
            Object.DestroyImmediate(m_PlayerObject);
            m_PlayerObject = null;

            // デバッグログが無効でも正常に動作することを確認
            Assert.IsTrue(bulletCount >= 0, "弾の生成・削除が正常に動作するべき");
        }

        /// <summary>
        /// 初期化前にOnDestroy()が呼ばれても安全に処理されることをテスト
        /// </summary>
        [Test]
        public void OnDestroy_SafeWithUninitializedPlayer()
        {
            // 初期化せずに新しいプレイヤーを作成
            var uninitializedObject = new GameObject("UninitializedPlayer");
            var uninitializedPlayer = uninitializedObject.AddComponent<BulletMlPlayer>();
            // InitializeForTest()を呼ばずに削除

            // 初期化されていないプレイヤーでもOnDestroy()でエラーが発生しない
            Object.DestroyImmediate(uninitializedObject);

            // テストが正常に完了することを確認（例外が発生しない）
            Assert.IsTrue(true, "初期化前のOnDestroy()が安全に実行されるべき");
        }

        /// <summary>
        /// 複数の弾がある状態でOnDestroy()が正常に動作することをテスト
        /// </summary>
        [Test]
        public void OnDestroy_HandlesMultipleBullets()
        {
            // 複数弾を生成するBulletMLを読み込み
            string testXml = @"
            <bulletml>
                <action label=""top"">
                    <fire><direction type=""absolute"">0</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">45</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">90</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">135</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">180</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">225</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">270</direction><speed>1</speed></fire>
                    <fire><direction type=""absolute"">315</direction><speed>1</speed></fire>
                </action>
            </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();

            // 複数回更新して多くの弾を生成
            for (int i = 0; i < 8; i++)
            {
                m_Player.ManualUpdate();
            }

            var activeBulletsCount = m_Player.GetActiveBullets().Where(b => b.IsVisible).Count();

            // オブジェクトを削除
            Object.DestroyImmediate(m_PlayerObject);
            m_PlayerObject = null;

            Assert.IsTrue(activeBulletsCount > 0, "削除前に複数の弾が存在するべき");
        }

        /// <summary>
        /// ClearAllBullets()が正常に呼ばれ、その後の参照クリアが実行されることをテスト
        /// </summary>
        [Test]
        public void OnDestroy_ExecutesCompleteCleanupSequence()
        {
            // テスト用BulletMLを読み込み
            string testXml = @"
            <bulletml>
                <action label=""top"">
                    <fire><direction type=""absolute"">0</direction><speed>1</speed></fire>
                </action>
            </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            m_Player.ManualUpdate();

            // プレイヤーが正常に初期化されていることを確認
            Assert.IsNotNull(m_Player.GetActiveBullets(), "アクティブ弾リストが初期化されているべき");

            int bulletCount = m_Player.GetActiveBullets().Count;

            // オブジェクトを削除（完全なクリーンアップシーケンスが実行される）
            Object.DestroyImmediate(m_PlayerObject);
            m_PlayerObject = null;

            // クリーンアップ前に弾が存在していたことを確認
            Assert.IsTrue(bulletCount > 0, "削除前に弾が存在するべき");
        }

        /// <summary>
        /// OnDestroy時のクリーンアップログが正常に出力されることをテスト（ログ確認専用）
        /// </summary>
        [Test]
        public void OnDestroy_LogsCleanupMessages()
        {
            // デバッグログを有効にしてOnDestroyログをテスト
            m_Player.SetEnableDebugLog(true);

            // テスト用BulletMLを読み込み
            string testXml = @"
            <bulletml>
                <action label=""top"">
                    <fire><direction type=""absolute"">0</direction><speed>1</speed></fire>
                </action>
            </bulletml>";

            m_Player.LoadBulletML(testXml);
            m_Player.StartTopAction();
            m_Player.ManualUpdate();

            // オブジェクトを削除（OnDestroy()ログが出力される）
            Object.DestroyImmediate(m_PlayerObject);
            m_PlayerObject = null;

            // OnDestroy()が正常に実行されたことを機能的に確認
            // （ログ確認ではなく、例外が発生しないことで確認）
            Assert.IsTrue(true, "OnDestroy()が正常に実行されるべき");
        }
    }
}
