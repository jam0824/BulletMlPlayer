using System.Collections.Generic;
using UnityEngine;
using BulletML;

/// <summary>
/// BulletMLシステムの統合管理クラス
/// </summary>
public class BulletMlPlayer : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private TextAsset m_BulletMLXml;
    [SerializeField] private CoordinateSystem m_CoordinateSystem = CoordinateSystem.XY;
    [SerializeField] private Vector3 m_PlayerPosition = Vector3.zero;
    [SerializeField] private float m_RankValue = 0.5f;
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

    public BulletMLDocument Document => m_Document;
    public Vector3 PlayerPosition => m_PlayerPosition;
    public float RankValue => m_RankValue;

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
        
        // プレイヤー位置を更新（テスト用）
        m_PlayerPosition = transform.position;
        m_Executor.SetTargetPosition(m_PlayerPosition);
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

        // Executorを設定
        m_Executor.SetCoordinateSystem(m_CoordinateSystem);
        m_Executor.SetTargetPosition(m_PlayerPosition);
        m_Executor.SetRankValue(m_RankValue);
        
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

            if (m_EnableDebugLog)
            {
                Debug.Log($"BulletMLが読み込まれました。タイプ: {m_Document.Type}");
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
        var initialBullet = new BulletMLBullet(transform.position, 0f, 1f, m_CoordinateSystem, false);
        var actionRunner = new BulletMLActionRunner(topAction);
        initialBullet.PushAction(actionRunner);
        
        AddBullet(initialBullet);

        if (m_EnableDebugLog)
        {
            Debug.Log("トップアクションを開始しました");
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
                Debug.Log($"新しい弾を追加: 位置={_newBullet.Position}, 方向={_newBullet.Direction}度, 速度={_newBullet.Speed}");
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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(m_PlayerPosition, 0.5f);
        
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
