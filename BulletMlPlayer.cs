using System.Collections.Generic;
using UnityEngine;
using BulletML;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// BulletMLシステムの統合管理クラス
/// </summary>
public class BulletMlPlayer : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private TextAsset m_BulletMLXml;
    [SerializeField] private CoordinateSystem m_CoordinateSystem = CoordinateSystem.XY;
    [SerializeField] private string m_TargetTag = "Player"; // ターゲットオブジェクトのタグ
    [SerializeField] private Vector3 m_FallbackPlayerPosition = new Vector3(0f, -3f, -5f); // タグオブジェクトが見つからない場合の位置
    [SerializeField] private bool m_UseShooterOffset = false; // オフセット使用フラグ
    [SerializeField] private Vector3 m_ShooterOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float m_RankValue = 0.5f;
    [SerializeField] private float m_DefaultSpeed = 1f; // デフォルト速度
    [SerializeField] private bool m_AutoStart = true;

    [Header("弾のプレハブ")]
    [SerializeField] private GameObject m_BulletPrefab;

    [Header("デバッグ")]
    [SerializeField] private bool m_EnableDebugLog = false;
    [SerializeField] private int m_MaxBullets = 1000;

    private BulletMLDocument m_Document;
    private BulletMLParser m_Parser;
    private BulletMLExecutor m_Executor;
    private List<BulletMLBullet> m_ListActiveBullets;
    private List<GameObject> m_ListBulletObjects;
    private Queue<GameObject> m_BulletPool;
    
    // ターゲット管理
    private GameObject m_TargetObject;
    private Vector3 m_CurrentPlayerPosition;

    public BulletMLDocument Document => m_Document;
    public Vector3 PlayerPosition => m_CurrentPlayerPosition;
    public float RankValue => m_RankValue;
    
    /// <summary>
    /// アクティブな弾のリストを取得（デバッグ用）
    /// </summary>
    public List<BulletMLBullet> GetActiveBullets()
    {
        return m_ListActiveBullets;
    }

    void Start()
    {
        InitializeSystem();
        
        if (m_AutoStart && m_BulletMLXml != null)
        {
            LoadBulletML(m_BulletMLXml.text);
            StartTopAction();
        }
    }

    void Update()
    {
        UpdateBullets();
        UpdateBulletObjects();
        
        // ターゲット位置を更新
        UpdateTargetPosition();
        
        // プレイヤー位置を更新（座標系に応じて補正）
        Vector3 effectivePlayerPosition = GetEffectivePlayerPosition();
        m_Executor.SetTargetPosition(effectivePlayerPosition);
    }

    /// <summary>
    /// システムを初期化する
    /// </summary>
    private void InitializeSystem()
    {
        m_Parser = new BulletMLParser();
        m_Executor = new BulletMLExecutor();
        m_ListActiveBullets = new List<BulletMLBullet>();
        m_ListBulletObjects = new List<GameObject>();
        m_BulletPool = new Queue<GameObject>();

        // 初期ターゲット位置を更新
        UpdateTargetPosition();
        
        // Executorを設定
        m_Executor.SetCoordinateSystem(m_CoordinateSystem);
        m_Executor.SetTargetPosition(m_CurrentPlayerPosition);
        m_Executor.SetRankValue(m_RankValue);
        m_Executor.SetDefaultSpeed(m_DefaultSpeed);
        
        // 新しい弾生成のコールバックを設定
        m_Executor.OnBulletCreated = OnNewBulletCreated;

        if (m_EnableDebugLog)
        {
            Debug.Log("BulletMLシステムが初期化されました");
        }
    }

    /// <summary>
    /// BulletMLを読み込む
    /// </summary>
    public void LoadBulletML(string _xmlContent)
    {
        try
        {
            m_Document = m_Parser.Parse(_xmlContent);
            m_Executor.SetDocument(m_Document);
            
            // Inspectorで設定した座標系とデフォルト速度を強制的に適用
            // （SetDocumentがXMLのtypeに基づいて座標系を変更するため）
            m_Executor.SetCoordinateSystem(m_CoordinateSystem);
            m_Executor.SetDefaultSpeed(m_DefaultSpeed);

            if (m_EnableDebugLog)
            {
                Debug.Log($"BulletMLが読み込まれました。タイプ: {m_Document.Type}");
                Debug.Log($"座標系を強制設定: {m_CoordinateSystem}");
                Debug.Log($"デフォルト速度を設定: {m_DefaultSpeed}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"BulletMLの読み込みに失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// トップアクションを開始する
    /// </summary>
    public void StartTopAction()
    {
        if (m_Document == null)
        {
            Debug.LogError("ドキュメントが読み込まれていません");
            return;
        }

        var topAction = m_Document.GetTopAction();
        if (topAction == null)
        {
            Debug.LogError("トップアクションが見つかりません");
            return;
        }

        // 初期弾を作成（シューターなので非表示）
        // シューター位置を決定
        Vector3 shooterPosition = GetShooterPosition();
        
        // シューターは移動しないように速度を0に設定
        var initialBullet = new BulletMLBullet(shooterPosition, 0f, 0f, m_CoordinateSystem, false);
        var actionRunner = new BulletMLActionRunner(topAction);
        initialBullet.PushAction(actionRunner);
        
        AddBullet(initialBullet);

        if (m_EnableDebugLog)
        {
            Vector3 effectivePlayerPos = GetEffectivePlayerPosition();
            Vector3 toTarget = effectivePlayerPos - shooterPosition;
            
            Debug.Log($"トップアクションを開始しました");
            Debug.Log($"座標系: {m_CoordinateSystem}");
            Debug.Log($"オフセット使用: {m_UseShooterOffset}");
            Debug.Log($"シューター位置: {shooterPosition} (transform.position: {transform.position})");
            Debug.Log($"ターゲットタグ: '{m_TargetTag}'");
            Debug.Log($"ターゲットオブジェクト: {(m_TargetObject ? m_TargetObject.name : "見つからず")}");
            Debug.Log($"現在のプレイヤー位置: {m_CurrentPlayerPosition}");
            Debug.Log($"フォールバック位置: {m_FallbackPlayerPosition}");
            Debug.Log($"有効プレイヤー位置: {effectivePlayerPos}");
            Debug.Log($"toTargetベクトル: {toTarget}");
        }
    }

    /// <summary>
    /// シューター位置を取得
    /// </summary>
    private Vector3 GetShooterPosition()
    {
        if (m_UseShooterOffset)
        {
            // オフセット使用時: 従来のロジック
            Vector3 effectiveOffset = GetEffectiveShooterOffset();
            return transform.position + effectiveOffset;
        }
        else
        {
            // オフセット不使用時: transform.positionをそのまま使用
            return transform.position;
        }
    }

    /// <summary>
    /// 座標系に応じて有効なシューターオフセットを取得
    /// </summary>
    private Vector3 GetEffectiveShooterOffset()
    {
        switch (m_CoordinateSystem)
        {
            case CoordinateSystem.XY:
                // XY面: X軸が横、Y軸が縦、Z軸は無視
                Vector3 xyOffset = new Vector3(m_ShooterOffset.x, m_ShooterOffset.y, 0f);
                // オフセットがゼロの場合はデフォルトのY方向オフセットを使用
                if (xyOffset.magnitude < 0.001f)
                    xyOffset = new Vector3(0f, 2f, 0f);
                return xyOffset;
                
            case CoordinateSystem.YZ:
                // YZ面: Y軸が縦、Z軸が前後、X軸は無視
                Vector3 yzOffset = new Vector3(0f, m_ShooterOffset.y, m_ShooterOffset.z);
                // オフセットがゼロの場合はデフォルトのY方向オフセットを使用
                if (yzOffset.magnitude < 0.001f)
                    yzOffset = new Vector3(0f, 2f, 0f);
                return yzOffset;
                
            default:
                return m_ShooterOffset;
        }
    }

    /// <summary>
    /// ターゲット位置を更新
    /// </summary>
    private void UpdateTargetPosition()
    {
        // ターゲットオブジェクトが無効になった場合は再検索
        if (m_TargetObject == null || !m_TargetObject.activeInHierarchy)
        {
            m_TargetObject = GameObject.FindWithTag(m_TargetTag);
        }

        // ターゲットオブジェクトが見つかった場合はその位置を使用、そうでなければフォールバック
        if (m_TargetObject != null)
        {
            m_CurrentPlayerPosition = m_TargetObject.transform.position;
        }
        else
        {
            m_CurrentPlayerPosition = m_FallbackPlayerPosition;
            
            if (m_EnableDebugLog && Time.frameCount % 60 == 0) // 1秒に1回ログ出力
            {
                Debug.LogWarning($"タグ '{m_TargetTag}' のオブジェクトが見つかりません。フォールバック位置を使用: {m_FallbackPlayerPosition}");
            }
        }
    }

    /// <summary>
    /// 座標系に応じて有効なプレイヤー位置を取得
    /// </summary>
    private Vector3 GetEffectivePlayerPosition()
    {
        switch (m_CoordinateSystem)
        {
            case CoordinateSystem.XY:
                // XY面: Z成分を無視してXY平面上に投影
                return new Vector3(m_CurrentPlayerPosition.x, m_CurrentPlayerPosition.y, 0f);
                
            case CoordinateSystem.YZ:
                // YZ面: X成分を無視してYZ平面上に投影
                return new Vector3(0f, m_CurrentPlayerPosition.y, m_CurrentPlayerPosition.z);
                
            default:
                return m_CurrentPlayerPosition;
        }
    }

    /// <summary>
    /// 弾を追加する
    /// </summary>
    private void AddBullet(BulletMLBullet _bullet)
    {
        if (m_ListActiveBullets.Count >= m_MaxBullets)
        {
            return; // 最大数に達している
        }

        m_ListActiveBullets.Add(_bullet);
        
        // 可視の弾のみGameObjectを作成
        if (_bullet.IsVisible)
        {
            GameObject bulletObj = GetBulletObject();
            bulletObj.transform.position = _bullet.Position;
            bulletObj.SetActive(true);
            m_ListBulletObjects.Add(bulletObj);
        }
        else
        {
            // 非表示の弾にはnullを追加してインデックスを合わせる
            m_ListBulletObjects.Add(null);
        }
    }

    /// <summary>
    /// 弾オブジェクトを取得する（プーリング）
    /// </summary>
    private GameObject GetBulletObject()
    {
        if (m_BulletPool.Count > 0)
        {
            return m_BulletPool.Dequeue();
        }

        if (m_BulletPrefab != null)
        {
            return Instantiate(m_BulletPrefab, transform);
        }

        // デフォルトの弾オブジェクト
        var bulletObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletObj.transform.localScale = Vector3.one * 0.1f;
        bulletObj.transform.SetParent(transform);
        return bulletObj;
    }

    /// <summary>
    /// 弾オブジェクトをプールに戻す
    /// </summary>
    private void ReturnBulletObject(GameObject _bulletObj)
    {
        _bulletObj.SetActive(false);
        m_BulletPool.Enqueue(_bulletObj);
    }

    /// <summary>
    /// 弾を更新する
    /// </summary>
    private void UpdateBullets()
    {
        for (int i = m_ListActiveBullets.Count - 1; i >= 0; i--)
        {
            var bullet = m_ListActiveBullets[i];

            if (!bullet.IsActive)
            {
                // 弾を削除
                RemoveBulletAt(i);
                continue;
            }

            // アクションを実行
            bool actionContinues = m_Executor.ExecuteCurrentAction(bullet);

            // 弾の物理更新
            bullet.Update(Time.deltaTime);

            // 画面外チェック（簡易）
            if (IsOutOfBounds(bullet.Position))
            {
                bullet.Vanish();
            }
        }
    }

    /// <summary>
    /// 新しい弾が生成された時のコールバック
    /// </summary>
    private void OnNewBulletCreated(BulletMLBullet _newBullet)
    {
        if (_newBullet != null && _newBullet.IsActive)
        {
            AddBullet(_newBullet);
            
            if (m_EnableDebugLog)
            {
                Vector3 velocityVector = _newBullet.GetVelocityVector();
                Debug.Log($"新しい弾を追加: 位置={_newBullet.Position}, 方向={_newBullet.Direction}度, 速度={_newBullet.Speed}");
                Debug.Log($"  → 速度ベクトル: {velocityVector} (座標系: {m_CoordinateSystem})");
                Debug.Log($"  → 期待される移動: X={velocityVector.x:F3}, Y={velocityVector.y:F3}, Z={velocityVector.z:F3}");
            }
        }
    }

    /// <summary>
    /// 弾オブジェクトを更新する
    /// </summary>
    private void UpdateBulletObjects()
    {
        for (int i = 0; i < m_ListActiveBullets.Count && i < m_ListBulletObjects.Count; i++)
        {
            var bullet = m_ListActiveBullets[i];
            var bulletObj = m_ListBulletObjects[i];
            
            // 可視の弾のみ位置を更新
            if (bulletObj != null && bullet.IsVisible)
            {
                bulletObj.transform.position = bullet.Position;
            }
        }
    }

    /// <summary>
    /// 指定インデックスの弾を削除する
    /// </summary>
    private void RemoveBulletAt(int _index)
    {
        if (_index >= 0 && _index < m_ListActiveBullets.Count)
        {
            m_ListActiveBullets.RemoveAt(_index);
            
            if (_index < m_ListBulletObjects.Count)
            {
                var bulletObj = m_ListBulletObjects[_index];
                m_ListBulletObjects.RemoveAt(_index);
                
                if (bulletObj != null)
                {
                    ReturnBulletObject(bulletObj);
                }
            }
        }
    }

    /// <summary>
    /// 画面外判定
    /// </summary>
    private bool IsOutOfBounds(Vector3 _position)
    {
        // 簡易的な画面外判定
        return Mathf.Abs(_position.x) > 20f || Mathf.Abs(_position.y) > 20f || Mathf.Abs(_position.z) > 20f;
    }

    /// <summary>
    /// ランク値を設定する
    /// </summary>
    public void SetRankValue(float _rankValue)
    {
        m_RankValue = Mathf.Clamp01(_rankValue);
        if (m_Executor != null)
        {
            m_Executor.SetRankValue(m_RankValue);
        }
    }

    /// <summary>
    /// 座標系を設定する
    /// </summary>
    public void SetCoordinateSystem(CoordinateSystem _coordinateSystem)
    {
        m_CoordinateSystem = _coordinateSystem;
        if (m_Executor != null)
        {
            m_Executor.SetCoordinateSystem(_coordinateSystem);
        }
    }

    /// <summary>
    /// すべての弾をクリアする
    /// </summary>
    public void ClearAllBullets()
    {
        for (int i = m_ListBulletObjects.Count - 1; i >= 0; i--)
        {
            RemoveBulletAt(i);
        }
        
        m_ListActiveBullets.Clear();
    }

    void OnDrawGizmos()
    {
        // プレイヤー位置を表示
        Gizmos.color = m_TargetObject != null ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(m_CurrentPlayerPosition, 0.5f);
        
        // ターゲットオブジェクトの名前を表示（エディタのみ）
        #if UNITY_EDITOR
        if (m_TargetObject != null)
        {
            UnityEditor.Handles.Label(m_CurrentPlayerPosition + Vector3.up * 0.8f, 
                $"Target: {m_TargetObject.name} (Tag: {m_TargetTag})");
        }
        else
        {
            UnityEditor.Handles.Label(m_CurrentPlayerPosition + Vector3.up * 0.8f, 
                $"Fallback Position (Tag: {m_TargetTag} not found)");
        }
        #endif
        
        // 可視の弾の位置を表示
        Gizmos.color = Color.red;
        if (m_ListActiveBullets != null)
        {
            foreach (var bullet in m_ListActiveBullets)
            {
                if (bullet.IsVisible)
                {
                    Gizmos.DrawWireSphere(bullet.Position, 0.1f);
                }
            }
        }
        
        // 非表示の弾（シューター）を表示
        Gizmos.color = Color.yellow;
        if (m_ListActiveBullets != null)
        {
            foreach (var bullet in m_ListActiveBullets)
            {
                if (!bullet.IsVisible)
                {
                    Gizmos.DrawWireCube(bullet.Position, Vector3.one * 0.2f);
                }
            }
        }
    }
}
