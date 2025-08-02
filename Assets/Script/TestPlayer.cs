using UnityEngine;
using BulletML;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// BulletMLのテスト用プレイヤー
/// WASDキーで移動可能
/// </summary>
public class TestPlayer : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float m_MoveSpeed = 5.0f;
    [SerializeField] private Vector3 m_MovementBounds = new Vector3(10f, 10f, 10f);
    
    [Header("座標系")]
    [SerializeField] private CoordinateSystem m_CoordinateSystem = CoordinateSystem.XY;
    
    void Update()
    {
        HandleMovement();
    }
    
    /// <summary>
    /// キー入力による移動処理
    /// </summary>
    private void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        
        // 座標系に応じた移動
        switch (m_CoordinateSystem)
        {
            case CoordinateSystem.XY:
                // XY面: X軸が横、Y軸が縦
                if (Input.GetKey(KeyCode.A)) movement.x -= 1f;
                if (Input.GetKey(KeyCode.D)) movement.x += 1f;
                if (Input.GetKey(KeyCode.W)) movement.y += 1f;
                if (Input.GetKey(KeyCode.S)) movement.y -= 1f;
                break;
                
            case CoordinateSystem.YZ:
                // YZ面: Y軸が縦、Z軸が前後
                if (Input.GetKey(KeyCode.A)) movement.z -= 1f;
                if (Input.GetKey(KeyCode.D)) movement.z += 1f;
                if (Input.GetKey(KeyCode.W)) movement.y += 1f;
                if (Input.GetKey(KeyCode.S)) movement.y -= 1f;
                break;
        }
        
        // 移動を適用
        if (movement != Vector3.zero)
        {
            Vector3 newPosition = transform.position + movement.normalized * m_MoveSpeed * Time.deltaTime;
            
            // 境界チェック
            newPosition.x = Mathf.Clamp(newPosition.x, -m_MovementBounds.x, m_MovementBounds.x);
            newPosition.y = Mathf.Clamp(newPosition.y, -m_MovementBounds.y, m_MovementBounds.y);
            newPosition.z = Mathf.Clamp(newPosition.z, -m_MovementBounds.z, m_MovementBounds.z);
            
            transform.position = newPosition;
        }
    }
    
    void OnDrawGizmos()
    {
        // プレイヤーの表示
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // 移動範囲の表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, m_MovementBounds * 2f);
        
        // ラベル表示
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
            $"Player ({m_CoordinateSystem})");
        #endif
    }
}