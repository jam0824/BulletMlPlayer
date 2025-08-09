using UnityEngine;

/// <summary>
/// BulletMlPlayerの弾独立性テスト用自動移動コントローラー
/// 上下左右に自動で移動してBulletMlPlayerの親オブジェクト移動をテストします
/// </summary>
public class AutoMovementController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField, Tooltip("移動パターンの種類")] 
    private MovementPattern m_MovementPattern = MovementPattern.Square;
    
    [SerializeField, Tooltip("座標系の種類")] 
    private CoordinateSystem m_CoordinateSystem = CoordinateSystem.XY;
    
    [SerializeField, Tooltip("移動速度（units/second）")] 
    private float m_MovementSpeed = 2.0f;
    
    [SerializeField, Tooltip("移動範囲（中心からの距離）")] 
    private float m_MovementRange = 3.0f;
    
    [SerializeField, Tooltip("移動サイクル時間（秒）")] 
    private float m_CycleDuration = 8.0f;
    
    [Header("オプション")]
    [SerializeField, Tooltip("移動を有効にする")] 
    private bool m_EnableMovement = true;
    
    [SerializeField, Tooltip("移動経路を表示する")] 
    private bool m_ShowMovementPath = true;
    
    [SerializeField, Tooltip("現在位置を表示する")] 
    private bool m_ShowCurrentPosition = true;

    /// <summary>
    /// 移動パターンの種類
    /// </summary>
    public enum MovementPattern
    {
        [Tooltip("四角形の軌道（上→右→下→左）")]
        Square,
        
        [Tooltip("円形の軌道")]
        Circle,
        
        [Tooltip("横に往復移動")]
        HorizontalPingPong,
        
        [Tooltip("縦に往復移動")]
        VerticalPingPong,
        
        [Tooltip("8の字軌道")]
        Figure8,
        
        [Tooltip("ランダム移動")]
        Random
    }

    /// <summary>
    /// 座標系の種類
    /// </summary>
    public enum CoordinateSystem
    {
        [Tooltip("XY面（縦スクロール: X=横、Y=縦）")]
        XY,
        
        [Tooltip("YZ面（横スクロール: Y=縦、Z=前後）")]
        YZ
    }

    // 内部状態
    private Vector3 m_StartPosition;
    private float m_ElapsedTime = 0f;
    private Vector3 m_RandomTarget;
    private float m_RandomMoveStartTime;
    private const float RANDOM_TARGET_INTERVAL = 2.0f;

    void Start()
    {
        // 開始位置を記録
        m_StartPosition = transform.position;
        
        // ランダム移動用の初期ターゲット設定
        if (m_MovementPattern == MovementPattern.Random)
        {
            SetNewRandomTarget();
        }

        if (m_EnableMovement)
        {
            Debug.Log($"AutoMovementController開始: パターン={m_MovementPattern}, 速度={m_MovementSpeed}, 範囲={m_MovementRange}");
        }
    }

    void Update()
    {
        if (!m_EnableMovement)
            return;

        m_ElapsedTime += Time.deltaTime;

        Vector3 newPosition = CalculatePosition();
        transform.position = newPosition;

        // 現在位置をログ出力（デバッグ用）
        if (m_ShowCurrentPosition && Time.frameCount % 60 == 0) // 1秒に1回
        {
            Debug.Log($"自動移動中: {transform.position:F2} (パターン: {m_MovementPattern})");
        }
    }

    /// <summary>
    /// 移動パターンに応じて位置を計算
    /// </summary>
    private Vector3 CalculatePosition()
    {
        Vector3 offset = Vector3.zero;
        
        switch (m_MovementPattern)
        {
            case MovementPattern.Square:
                offset = CalculateSquareMovement();
                break;
                
            case MovementPattern.Circle:
                offset = CalculateCircleMovement();
                break;
                
            case MovementPattern.HorizontalPingPong:
                offset = CalculateHorizontalPingPong();
                break;
                
            case MovementPattern.VerticalPingPong:
                offset = CalculateVerticalPingPong();
                break;
                
            case MovementPattern.Figure8:
                offset = CalculateFigure8Movement();
                break;
                
            case MovementPattern.Random:
                offset = CalculateRandomMovement();
                break;
        }

        return m_StartPosition + offset;
    }

    /// <summary>
    /// 座標系に応じてオフセットベクトルを作成
    /// </summary>
    private Vector3 CreateOffsetVector(float horizontal, float vertical)
    {
        switch (m_CoordinateSystem)
        {
            case CoordinateSystem.XY:
                // XY面: X=横、Y=縦、Z=0
                return new Vector3(horizontal, vertical, 0);
                
            case CoordinateSystem.YZ:
                // YZ面: X=0、Y=縦、Z=前後（横に相当）
                return new Vector3(0, vertical, horizontal);
                
            default:
                return new Vector3(horizontal, vertical, 0);
        }
    }

    /// <summary>
    /// 四角形移動を計算
    /// </summary>
    private Vector3 CalculateSquareMovement()
    {
        float normalizedTime = (m_ElapsedTime % m_CycleDuration) / m_CycleDuration;
        float segmentTime = normalizedTime * 4f; // 4つのセグメント
        int segment = Mathf.FloorToInt(segmentTime);
        float t = segmentTime - segment;

        Vector3 offset = Vector3.zero;
        
        switch (segment)
        {
            case 0: // 上方向
                offset = CreateOffsetVector(-m_MovementRange + t * 2 * m_MovementRange, m_MovementRange);
                break;
            case 1: // 右方向
                offset = CreateOffsetVector(m_MovementRange, m_MovementRange - t * 2 * m_MovementRange);
                break;
            case 2: // 下方向
                offset = CreateOffsetVector(m_MovementRange - t * 2 * m_MovementRange, -m_MovementRange);
                break;
            case 3: // 左方向
                offset = CreateOffsetVector(-m_MovementRange, -m_MovementRange + t * 2 * m_MovementRange);
                break;
        }

        return offset;
    }

    /// <summary>
    /// 円形移動を計算
    /// </summary>
    private Vector3 CalculateCircleMovement()
    {
        float normalizedTime = (m_ElapsedTime % m_CycleDuration) / m_CycleDuration;
        float angle = normalizedTime * 2f * Mathf.PI;
        
        float horizontal = Mathf.Cos(angle) * m_MovementRange;
        float vertical = Mathf.Sin(angle) * m_MovementRange;
        
        return CreateOffsetVector(horizontal, vertical);
    }

    /// <summary>
    /// 横往復移動を計算
    /// </summary>
    private Vector3 CalculateHorizontalPingPong()
    {
        float normalizedTime = (m_ElapsedTime % m_CycleDuration) / m_CycleDuration;
        float x = Mathf.PingPong(normalizedTime * 2f, 1f) * 2f - 1f; // -1 to 1
        
        return CreateOffsetVector(x * m_MovementRange, 0);
    }

    /// <summary>
    /// 縦往復移動を計算
    /// </summary>
    private Vector3 CalculateVerticalPingPong()
    {
        float normalizedTime = (m_ElapsedTime % m_CycleDuration) / m_CycleDuration;
        float y = Mathf.PingPong(normalizedTime * 2f, 1f) * 2f - 1f; // -1 to 1
        
        return CreateOffsetVector(0, y * m_MovementRange);
    }

    /// <summary>
    /// 8の字移動を計算
    /// </summary>
    private Vector3 CalculateFigure8Movement()
    {
        float normalizedTime = (m_ElapsedTime % m_CycleDuration) / m_CycleDuration;
        float angle = normalizedTime * 2f * Mathf.PI;
        
        float horizontal = Mathf.Sin(angle) * m_MovementRange;
        float vertical = Mathf.Sin(angle * 2f) * m_MovementRange * 0.5f;
        
        return CreateOffsetVector(horizontal, vertical);
    }

    /// <summary>
    /// ランダム移動を計算
    /// </summary>
    private Vector3 CalculateRandomMovement()
    {
        // 一定間隔で新しいランダムターゲットを設定
        if (m_ElapsedTime - m_RandomMoveStartTime >= RANDOM_TARGET_INTERVAL)
        {
            SetNewRandomTarget();
        }

        // 現在位置からターゲット位置へ移動
        Vector3 currentOffset = transform.position - m_StartPosition;
        Vector3 direction = (m_RandomTarget - currentOffset).normalized;
        float moveDistance = m_MovementSpeed * Time.deltaTime;
        
        Vector3 newOffset = currentOffset + direction * moveDistance;
        
        // ターゲットに近づいたら新しいターゲットを設定
        if (Vector3.Distance(newOffset, m_RandomTarget) < 0.1f)
        {
            SetNewRandomTarget();
        }

        return newOffset;
    }

    /// <summary>
    /// 新しいランダムターゲットを設定
    /// </summary>
    private void SetNewRandomTarget()
    {
        float horizontal = Random.Range(-m_MovementRange, m_MovementRange);
        float vertical = Random.Range(-m_MovementRange, m_MovementRange);
        m_RandomTarget = CreateOffsetVector(horizontal, vertical);
        m_RandomMoveStartTime = m_ElapsedTime;
    }

    /// <summary>
    /// 移動を開始/停止
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        m_EnableMovement = enabled;
        if (enabled)
        {
            Debug.Log("自動移動を開始しました");
        }
        else
        {
            Debug.Log("自動移動を停止しました");
        }
    }

    /// <summary>
    /// 移動パターンを変更
    /// </summary>
    public void SetMovementPattern(MovementPattern pattern)
    {
        m_MovementPattern = pattern;
        m_ElapsedTime = 0f; // タイマーリセット
        
        if (pattern == MovementPattern.Random)
        {
            SetNewRandomTarget();
        }
        
        Debug.Log($"移動パターンを変更: {pattern}");
    }

    /// <summary>
    /// 座標系を変更
    /// </summary>
    public void SetCoordinateSystem(CoordinateSystem coordinateSystem)
    {
        m_CoordinateSystem = coordinateSystem;
        m_ElapsedTime = 0f; // タイマーリセット
        
        if (m_MovementPattern == MovementPattern.Random)
        {
            SetNewRandomTarget();
        }
        
        Debug.Log($"座標系を変更: {coordinateSystem}");
    }

    /// <summary>
    /// 開始位置にリセット
    /// </summary>
    public void ResetToStartPosition()
    {
        transform.position = m_StartPosition;
        m_ElapsedTime = 0f;
        
        if (m_MovementPattern == MovementPattern.Random)
        {
            SetNewRandomTarget();
        }
        
        Debug.Log("開始位置にリセットしました");
    }

    void OnDrawGizmos()
    {
        if (!m_ShowMovementPath)
            return;

        Vector3 center = Application.isPlaying ? m_StartPosition : transform.position;
        
        // 移動パターンに応じたガイド表示
        Gizmos.color = Color.yellow;
        
        switch (m_MovementPattern)
        {
            case MovementPattern.Square:
                DrawSquareGizmo(center);
                break;
                
            case MovementPattern.Circle:
                DrawCircleGizmo(center);
                break;
                
            case MovementPattern.HorizontalPingPong:
                DrawHorizontalLineGizmo(center);
                break;
                
            case MovementPattern.VerticalPingPong:
                DrawVerticalLineGizmo(center);
                break;
                
            case MovementPattern.Figure8:
                DrawFigure8Gizmo(center);
                break;
                
            case MovementPattern.Random:
                DrawRandomAreaGizmo(center);
                break;
        }

        // 現在位置表示
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
    }

    private void DrawSquareGizmo(Vector3 center)
    {
        Vector3[] corners = {
            center + CreateOffsetVector(-m_MovementRange, m_MovementRange),
            center + CreateOffsetVector(m_MovementRange, m_MovementRange),
            center + CreateOffsetVector(m_MovementRange, -m_MovementRange),
            center + CreateOffsetVector(-m_MovementRange, -m_MovementRange)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
        }
    }

    private void DrawCircleGizmo(Vector3 center)
    {
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * 2f * Mathf.PI;
            float angle2 = (float)(i + 1) / segments * 2f * Mathf.PI;
            
            Vector3 point1 = center + CreateOffsetVector(Mathf.Cos(angle1) * m_MovementRange, Mathf.Sin(angle1) * m_MovementRange);
            Vector3 point2 = center + CreateOffsetVector(Mathf.Cos(angle2) * m_MovementRange, Mathf.Sin(angle2) * m_MovementRange);
            
            Gizmos.DrawLine(point1, point2);
        }
    }

    private void DrawHorizontalLineGizmo(Vector3 center)
    {
        Vector3 left = center + CreateOffsetVector(-m_MovementRange, 0);
        Vector3 right = center + CreateOffsetVector(m_MovementRange, 0);
        Gizmos.DrawLine(left, right);
    }

    private void DrawVerticalLineGizmo(Vector3 center)
    {
        Vector3 bottom = center + CreateOffsetVector(0, -m_MovementRange);
        Vector3 top = center + CreateOffsetVector(0, m_MovementRange);
        Gizmos.DrawLine(bottom, top);
    }

    private void DrawFigure8Gizmo(Vector3 center)
    {
        int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            
            float angle1 = t1 * 2f * Mathf.PI;
            float angle2 = t2 * 2f * Mathf.PI;
            
            Vector3 point1 = center + CreateOffsetVector(
                Mathf.Sin(angle1) * m_MovementRange,
                Mathf.Sin(angle1 * 2f) * m_MovementRange * 0.5f
            );
            Vector3 point2 = center + CreateOffsetVector(
                Mathf.Sin(angle2) * m_MovementRange,
                Mathf.Sin(angle2 * 2f) * m_MovementRange * 0.5f
            );
            
            Gizmos.DrawLine(point1, point2);
        }
    }

    private void DrawRandomAreaGizmo(Vector3 center)
    {
        Vector3 size = CreateOffsetVector(m_MovementRange * 2, m_MovementRange * 2);
        // YZ面の場合はXは0なので、適切なサイズを設定
        if (m_CoordinateSystem == CoordinateSystem.YZ)
        {
            size = new Vector3(0.1f, m_MovementRange * 2, m_MovementRange * 2);
        }
        Gizmos.DrawWireCube(center, size);
    }
}
