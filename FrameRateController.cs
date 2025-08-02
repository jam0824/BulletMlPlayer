using UnityEngine;

/// <summary>
/// フレームレート制御スクリプト
/// ゲーム開始時にフレームレートを設定する
/// </summary>
public class FrameRateController : MonoBehaviour
{
    [Header("フレームレート設定")]
    [SerializeField] private int m_TargetFrameRate = 60;
    [SerializeField] private bool m_DisableVSync = true;
    
    [Header("デバッグ情報")]
    [SerializeField] private bool m_ShowFrameRateOnStart = true;
    
    void Awake()
    {
        // VSync設定（必要に応じて無効化）
        if (m_DisableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }
        
        // フレームレート設定
        Application.targetFrameRate = m_TargetFrameRate;
        
        // 開始時情報表示
        if (m_ShowFrameRateOnStart)
        {
            Debug.Log($"[FrameRateController] フレームレート設定完了");
            Debug.Log($"  目標フレームレート: {m_TargetFrameRate} FPS");
            Debug.Log($"  VSync: {(m_DisableVSync ? "無効" : "有効")} (QualitySettings.vSyncCount = {QualitySettings.vSyncCount})");
            Debug.Log($"  Application.targetFrameRate: {Application.targetFrameRate}");
        }
    }
    
    /// <summary>
    /// ランタイム中にフレームレートを変更する
    /// </summary>
    /// <param name="_frameRate">設定したいFPS値</param>
    public void SetFrameRate(int _frameRate)
    {
        m_TargetFrameRate = _frameRate;
        Application.targetFrameRate = m_TargetFrameRate;
        Debug.Log($"[FrameRateController] フレームレートを変更: {m_TargetFrameRate} FPS");
    }
    
    /// <summary>
    /// VSync設定を切り替える
    /// </summary>
    /// <param name="_enable">true: VSync有効, false: VSync無効</param>
    public void SetVSyncEnabled(bool _enable)
    {
        QualitySettings.vSyncCount = _enable ? 1 : 0;
        m_DisableVSync = !_enable;
        Debug.Log($"[FrameRateController] VSync設定変更: {(_enable ? "有効" : "無効")}");
    }
    
    /// <summary>
    /// 現在のフレームレート情報を表示する
    /// </summary>
    public void ShowCurrentFrameRateInfo()
    {
        float currentFPS = 1f / Time.unscaledDeltaTime;
        Debug.Log($"=== FrameRateController 情報 ===");
        Debug.Log($"目標フレームレート: {Application.targetFrameRate} FPS");
        Debug.Log($"現在のフレームレート: {currentFPS:F1} FPS");
        Debug.Log($"VSync: {(QualitySettings.vSyncCount > 0 ? "有効" : "無効")} (Count: {QualitySettings.vSyncCount})");
        Debug.Log($"================================");
    }
    
    void Update()
    {
        // Gキーでフレームレート情報表示
        if (Input.GetKeyDown(KeyCode.G))
        {
            ShowCurrentFrameRateInfo();
        }
    }
}