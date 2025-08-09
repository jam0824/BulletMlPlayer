using UnityEngine;

/// <summary>
/// 弾の独立性テスト用マネージャー
/// BulletMlPlayerとAutoMovementControllerを組み合わせて独立性をテストする便利クラス
/// </summary>
public class BulletIndependenceTestManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField, Tooltip("テスト対象のBulletMlPlayer")] 
    private BulletMlPlayer m_BulletPlayer;
    
    [SerializeField, Tooltip("自動移動コントローラー")] 
    private AutoMovementController m_MovementController;

    [Header("テスト設定")]
    [SerializeField, Tooltip("テスト開始時に自動実行する")] 
    private bool m_AutoStartTest = true;
    
    [SerializeField, Tooltip("テスト用BulletMLファイル")] 
    private TextAsset m_TestBulletML;

    [Header("キーボード操作")]
    [SerializeField, Tooltip("独立性切り替えキー")] 
    private KeyCode m_ToggleIndependenceKey = KeyCode.I;
    
    [SerializeField, Tooltip("移動パターン切り替えキー")] 
    private KeyCode m_ToggleMovementKey = KeyCode.M;
    
    [SerializeField, Tooltip("移動開始/停止キー")] 
    private KeyCode m_ToggleMovementEnabledKey = KeyCode.Space;
    
    [SerializeField, Tooltip("位置リセットキー")] 
    private KeyCode m_ResetPositionKey = KeyCode.R;

    private int m_CurrentPatternIndex = 0;
    private bool m_IsTestRunning = false;

    void Start()
    {
        // 自動参照設定
        if (m_BulletPlayer == null)
            m_BulletPlayer = GetComponent<BulletMlPlayer>();
        
        if (m_MovementController == null)
            m_MovementController = GetComponent<AutoMovementController>();

        // 参照チェック
        if (m_BulletPlayer == null)
        {
            Debug.LogError("BulletMlPlayerが見つかりません。同じGameObjectにアタッチしてください。");
            return;
        }

        if (m_MovementController == null)
        {
            Debug.LogError("AutoMovementControllerが見つかりません。同じGameObjectにアタッチしてください。");
            return;
        }

        // 自動テスト開始
        if (m_AutoStartTest)
        {
            StartTest();
        }

        // 操作方法を表示
        ShowInstructions();
    }

    void Update()
    {
        if (!m_IsTestRunning)
            return;

        // キーボード入力処理
        HandleKeyboardInput();
    }

    /// <summary>
    /// テストを開始
    /// </summary>
    public void StartTest()
    {
        if (m_BulletPlayer == null || m_MovementController == null)
        {
            Debug.LogError("必要なコンポーネントが不足しています");
            return;
        }

        // テスト用BulletMLを読み込み
        if (m_TestBulletML != null)
        {
            m_BulletPlayer.LoadBulletML(m_TestBulletML.text);
        }
        else
        {
            // デフォルトのテスト用XML
            string defaultXml = @"<?xml version=""1.0"" ?>
                <bulletml type=""vertical"" xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
                    <action label=""top"">
                        <repeat>
                            <times>5</times>
                            <action>
                                <fire>
                                    <direction type=""sequence"">0</direction>
                                    <speed>3</speed>
                                </fire>
                                <wait>15</wait>
                            </action>
                        </repeat>
                    </action>
                </bulletml>";
            m_BulletPlayer.LoadBulletML(defaultXml);
        }

        // BulletMLを開始
        m_BulletPlayer.StartTopAction();
        
        m_IsTestRunning = true;
        
        Debug.Log("=== 弾独立性テスト開始 ===");
        Debug.Log($"独立性設定: {(m_BulletPlayer.GetBulletIndependence() ? "有効" : "無効")}");
        Debug.Log("操作方法:");
        Debug.Log($"[{m_ToggleIndependenceKey}] 独立性切り替え");
        Debug.Log($"[{m_ToggleMovementKey}] 移動パターン切り替え");
        Debug.Log($"[C] 座標系切り替え（XY/YZ）");
        Debug.Log($"[{m_ToggleMovementEnabledKey}] 移動開始/停止");
        Debug.Log($"[{m_ResetPositionKey}] 位置リセット");
    }

    /// <summary>
    /// テストを停止
    /// </summary>
    public void StopTest()
    {
        m_IsTestRunning = false;
        
        if (m_MovementController != null)
        {
            m_MovementController.SetMovementEnabled(false);
        }
        
        if (m_BulletPlayer != null)
        {
            m_BulletPlayer.ClearAllBullets();
        }
        
        Debug.Log("=== 弾独立性テスト停止 ===");
    }

    /// <summary>
    /// キーボード入力処理
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 独立性切り替え
        if (Input.GetKeyDown(m_ToggleIndependenceKey))
        {
            bool currentIndependence = m_BulletPlayer.GetBulletIndependence();
            m_BulletPlayer.SetBulletIndependence(!currentIndependence);
            
            string status = !currentIndependence ? "有効" : "無効";
            Debug.Log($"弾の独立性を{status}に変更しました");
            
            // 既存の弾をクリアして効果を分かりやすくする
            m_BulletPlayer.ClearAllBullets();
            m_BulletPlayer.StartTopAction();
        }

        // 移動パターン切り替え
        if (Input.GetKeyDown(m_ToggleMovementKey))
        {
            var patterns = System.Enum.GetValues(typeof(AutoMovementController.MovementPattern));
            m_CurrentPatternIndex = (m_CurrentPatternIndex + 1) % patterns.Length;
            var newPattern = (AutoMovementController.MovementPattern)patterns.GetValue(m_CurrentPatternIndex);
            
            m_MovementController.SetMovementPattern(newPattern);
            Debug.Log($"移動パターンを{newPattern}に変更しました");
        }

        // 座標系切り替え（Cキー）
        if (Input.GetKeyDown(KeyCode.C))
        {
            var currentCoordinate = m_BulletPlayer.CoordinateSystem;
            var newCoordinate = (currentCoordinate == BulletML.CoordinateSystem.XY) ? 
                BulletML.CoordinateSystem.YZ : BulletML.CoordinateSystem.XY;
            
            // BulletMlPlayerとAutoMovementController両方の座標系を同期
            m_BulletPlayer.SetCoordinateSystem(newCoordinate);
            var autoCoordinate = (newCoordinate == BulletML.CoordinateSystem.XY) ? 
                AutoMovementController.CoordinateSystem.XY : AutoMovementController.CoordinateSystem.YZ;
            m_MovementController.SetCoordinateSystem(autoCoordinate);
            
            Debug.Log($"座標系を{newCoordinate}に変更しました");
        }

        // 移動開始/停止
        if (Input.GetKeyDown(m_ToggleMovementEnabledKey))
        {
            // 現在の状態を反転（AutoMovementControllerの状態を直接取得する方法がないので、
            // 簡易的に停止→開始→停止のトグル動作とする）
            m_MovementController.SetMovementEnabled(true);
            // すぐに停止したい場合は、Inspectorで手動制御するか、
            // AutoMovementControllerにGetterを追加する必要がある
        }

        // 位置リセット
        if (Input.GetKeyDown(m_ResetPositionKey))
        {
            m_MovementController.ResetToStartPosition();
            Debug.Log("位置をリセットしました");
        }
    }

    /// <summary>
    /// 操作方法を表示
    /// </summary>
    private void ShowInstructions()
    {
        Debug.Log("=== 弾独立性テスト 操作方法 ===");
        Debug.Log("このテストでは親オブジェクトが移動する際の弾の挙動を確認できます");
        Debug.Log("");
        Debug.Log("【独立性有効時】: 弾は発射方向に独立して移動");
        Debug.Log("【独立性無効時】: 弾は親オブジェクトと一緒に移動");
        Debug.Log("");
        Debug.Log("Inspectorで以下の設定を調整してテストしてください:");
        Debug.Log("- BulletMlPlayer > 弾の独立性設定 > Bullet Independence");
        Debug.Log("- AutoMovementController > 移動設定 > Movement Pattern/Speed/Range");
    }

    void OnGUI()
    {
        if (!m_IsTestRunning)
            return;

        // 画面上部に現在の状態を表示
        GUILayout.BeginArea(new Rect(10, 10, 400, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== 弾独立性テスト ===");
        
        string independenceStatus = m_BulletPlayer.GetBulletIndependence() ? "有効" : "無効";
        GUILayout.Label($"弾の独立性: {independenceStatus}");
        
        GUILayout.Label($"アクティブ弾数: {m_BulletPlayer.GetActiveBullets().Count}");
        
        GUILayout.Space(10);
        GUILayout.Label("操作:");
        GUILayout.Label($"[{m_ToggleIndependenceKey}] 独立性切り替え");
        GUILayout.Label($"[{m_ToggleMovementKey}] 移動パターン切り替え");
        GUILayout.Label($"[C] 座標系切り替え（XY/YZ）");
        GUILayout.Label($"[{m_ResetPositionKey}] 位置リセット");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
