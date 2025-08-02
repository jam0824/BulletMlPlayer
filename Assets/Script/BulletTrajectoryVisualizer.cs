using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 弾の軌道を視覚化するためのヘルパークラス
/// </summary>
public class BulletTrajectoryVisualizer : MonoBehaviour
{
    [Header("軌道可視化設定")]
    [SerializeField] private bool m_ShowTrajectory = true;
    [SerializeField] private float m_TrajectoryLength = 5f;
    [SerializeField] private Color m_TrajectoryColor = Color.cyan;
    
    [Header("座標軸表示")]
    [SerializeField] private bool m_ShowAxes = true;
    [SerializeField] private float m_AxesLength = 3f;
    
    private BulletMlPlayer m_BulletMlPlayer;
    
    void Start()
    {
        m_BulletMlPlayer = FindObjectOfType<BulletMlPlayer>();
    }
    
    void OnDrawGizmos()
    {
        if (!m_ShowAxes && !m_ShowTrajectory)
            return;
            
        Vector3 center = transform.position;
        
        // 座標軸を表示
        if (m_ShowAxes)
        {
            DrawAxes(center);
        }
        
        // 弾の軌道を表示
        if (m_ShowTrajectory && m_BulletMlPlayer != null)
        {
            DrawBulletTrajectories(center);
            DrawActualBullets();
        }
    }
    
    /// <summary>
    /// 座標軸を描画
    /// </summary>
    private void DrawAxes(Vector3 _center)
    {
        // X軸 - 赤
        Gizmos.color = Color.red;
        Gizmos.DrawLine(_center, _center + Vector3.right * m_AxesLength);
        Gizmos.DrawWireCube(_center + Vector3.right * m_AxesLength, Vector3.one * 0.1f);
        
        // Y軸 - 緑  
        Gizmos.color = Color.green;
        Gizmos.DrawLine(_center, _center + Vector3.up * m_AxesLength);
        Gizmos.DrawWireCube(_center + Vector3.up * m_AxesLength, Vector3.one * 0.1f);
        
        // Z軸 - 青
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_center, _center + Vector3.forward * m_AxesLength);
        Gizmos.DrawWireCube(_center + Vector3.forward * m_AxesLength, Vector3.one * 0.1f);
        
        #if UNITY_EDITOR
        // 軸ラベル
        UnityEditor.Handles.Label(_center + Vector3.right * (m_AxesLength + 0.2f), "X");
        UnityEditor.Handles.Label(_center + Vector3.up * (m_AxesLength + 0.2f), "Y");
        UnityEditor.Handles.Label(_center + Vector3.forward * (m_AxesLength + 0.2f), "Z");
        #endif
    }
    
    /// <summary>
    /// 弾の軌道を描画
    /// </summary>
    private void DrawBulletTrajectories(Vector3 _center)
    {
        Gizmos.color = m_TrajectoryColor;
        
        // sequence型の弾幕をシミュレーション（最初の16発）
        for (int i = 0; i < 16; i++)
        {
            float angle = 23f * i; // sample02.xmlと同じ
            // 座標系を取得（BulletMlPlayerから）
            BulletML.CoordinateSystem coordinateSystem = BulletML.CoordinateSystem.YZ;
            if (m_BulletMlPlayer != null && m_BulletMlPlayer.Document != null)
            {
                // BulletMLDocumentのTypeから座標系を判定
                var docType = m_BulletMlPlayer.Document.Type;
                coordinateSystem = (docType == BulletML.BulletMLType.horizontal) ? 
                    BulletML.CoordinateSystem.XY : BulletML.CoordinateSystem.YZ;
            }
            
            Vector3 direction = BulletML.BulletMLBullet.ConvertAngleToVector(angle, coordinateSystem);
            
            Vector3 endPoint = _center + direction * m_TrajectoryLength;
            
            // 軌道線を描画
            Gizmos.DrawLine(_center, endPoint);
            
            // 角度ラベル
            #if UNITY_EDITOR
            Vector3 labelPos = _center + direction * (m_TrajectoryLength * 0.8f);
            UnityEditor.Handles.Label(labelPos, $"{angle:F0}°");
            #endif
        }
        
        // 中心点
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(_center, 0.1f);
    }
    
    /// <summary>
    /// 実際の弾の位置を描画
    /// </summary>
    private void DrawActualBullets()
    {
        if (m_BulletMlPlayer == null)
            return;
            
        // 可視弾の位置に方向ベクトルを描画
        var activeBullets = m_BulletMlPlayer.GetActiveBullets();
        if (activeBullets != null)
        {
            foreach (var bullet in activeBullets)
            {
                if (bullet != null && bullet.IsVisible && bullet.IsActive)
                {
                    Vector3 position = bullet.Position;
                    Vector3 velocity = bullet.GetVelocityVector();
                    
                    // 弾の現在位置
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(position, 0.05f);
                    
                    // 速度ベクトル
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(position, position + velocity);
                    
                    #if UNITY_EDITOR
                    // 弾の情報をラベル表示
                    string info = $"{bullet.Direction:F0}°\n({velocity.x:F2},{velocity.y:F2},{velocity.z:F2})";
                    UnityEditor.Handles.Label(position + Vector3.up * 0.2f, info);
                    #endif
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BulletTrajectoryVisualizer))]
public class BulletTrajectoryVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "このスクリプトをシーンに配置すると、Scene Viewで以下が表示されます：\n" +
            "• 赤線: X軸（横方向）\n" +
            "• 緑線: Y軸（縦方向）\n" +
            "• 青線: Z軸（前後方向）\n" +
            "• シアン線: 弾の軌道（sample02.xmlの最初の16発）\n\n" +
            "YZ面では弾はY軸とZ軸方向にのみ移動し、X軸方向には移動しません。",
            MessageType.Info
        );
    }
}
#endif