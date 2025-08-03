using System.Collections.Generic;
using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletMLアクションの実行状態を管理するクラス
    /// </summary>
    [System.Serializable]
    public class BulletMLActionRunner
    {
        [SerializeField] private BulletMLElement m_ActionElement;
        [SerializeField] private int m_CurrentIndex;
        [SerializeField] private int m_WaitFrames;
        [SerializeField] private bool m_IsFinished;
        [SerializeField] private Dictionary<int, float> m_Parameters;

        public BulletMLElement ActionElement => m_ActionElement;
        public int CurrentIndex => m_CurrentIndex;
        public int WaitFrames => m_WaitFrames;
        public bool IsFinished => m_IsFinished;
        public Dictionary<int, float> Parameters => m_Parameters;

        public BulletMLActionRunner(BulletMLElement _actionElement, Dictionary<int, float> _parameters = null)
        {
            if (_actionElement == null)
            {
                Debug.LogError("BulletMLActionRunner: actionElement is null");
            }
            
            m_ActionElement = _actionElement;
            m_CurrentIndex = 0;
            m_WaitFrames = 0;
            m_IsFinished = false;
            m_Parameters = _parameters ?? new Dictionary<int, float>();
        }

        public void SetWaitFrames(int _frames)
        {
            m_WaitFrames = _frames;
        }

        public void DecrementWaitFrames()
        {
            if (m_WaitFrames > 0)
                m_WaitFrames--;
        }

        public void IncrementIndex()
        {
            m_CurrentIndex++;
        }

        public void Finish()
        {
            m_IsFinished = true;
        }
    }

    /// <summary>
    /// 弾の状態を管理するクラス
    /// </summary>
    [System.Serializable]
    public class BulletMLBullet
    {
        [SerializeField] private Vector3 m_Position;
        [SerializeField] private float m_Direction; // 度単位
        [SerializeField] private float m_Speed;
        [SerializeField] private Vector3 m_Acceleration;
        [SerializeField] private bool m_IsActive;
        [SerializeField] private bool m_IsVisible; // 弾が表示されるかどうか
        [SerializeField] private Stack<BulletMLActionRunner> m_ActionStack;
        [SerializeField] private CoordinateSystem m_CoordinateSystem;
        [SerializeField] private BulletMLChangeInfo m_DirectionChange;
        [SerializeField] private BulletMLChangeInfo m_SpeedChange;
        [SerializeField] private BulletMLAccelInfo m_AccelInfo;
        [SerializeField] private Vector3 m_AccumulatedVelocity; // 加速度による累積速度変化

        public Vector3 Position => m_Position;
        public float Direction => m_Direction;
        public float Speed => m_Speed;
        public Vector3 Acceleration => m_Acceleration;
        public bool IsActive => m_IsActive;
        public bool IsVisible => m_IsVisible;
        public Stack<BulletMLActionRunner> ActionStack => m_ActionStack;
        public CoordinateSystem CoordinateSystem => m_CoordinateSystem;
        public BulletMLChangeInfo DirectionChange => m_DirectionChange;
        public BulletMLChangeInfo SpeedChange => m_SpeedChange;
        public BulletMLAccelInfo AccelInfo => m_AccelInfo;
        public Vector3 AccumulatedVelocity => m_AccumulatedVelocity;
        public int WaitFrames => GetCurrentAction()?.WaitFrames ?? 0;

        public BulletMLBullet(Vector3 _position, float _direction, float _speed, CoordinateSystem _coordinateSystem = CoordinateSystem.XY, bool _isVisible = true)
        {
            m_Position = _position;
            m_Direction = _direction;
            m_Speed = _speed;
            m_Acceleration = Vector3.zero;
            m_IsActive = true;
            m_IsVisible = _isVisible;
            m_ActionStack = new Stack<BulletMLActionRunner>();
            m_CoordinateSystem = _coordinateSystem;
            m_DirectionChange = new BulletMLChangeInfo();
            m_SpeedChange = new BulletMLChangeInfo();
            m_AccelInfo = new BulletMLAccelInfo();
            m_AccumulatedVelocity = Vector3.zero;
        }

        /// <summary>
        /// 位置を設定する
        /// </summary>
        public void SetPosition(Vector3 _position)
        {
            m_Position = _position;
        }

        /// <summary>
        /// 方向を設定する（度単位）
        /// </summary>
        public void SetDirection(float _direction)
        {
            m_Direction = _direction;
        }

        /// <summary>
        /// 速度を設定する
        /// </summary>
        public void SetSpeed(float _speed)
        {
            m_Speed = _speed;
        }

        /// <summary>
        /// 加速度を設定する
        /// </summary>
        public void SetAcceleration(Vector3 _acceleration)
        {
            m_Acceleration = _acceleration;
        }

        /// <summary>
        /// 座標系を設定する
        /// </summary>
        public void SetCoordinateSystem(CoordinateSystem _coordinateSystem)
        {
            m_CoordinateSystem = _coordinateSystem;
        }

        /// <summary>
        /// 可視性を設定する
        /// </summary>
        public void SetVisible(bool _isVisible)
        {
            m_IsVisible = _isVisible;
        }

        /// <summary>
        /// 弾を消去する
        /// </summary>
        public void Vanish()
        {
            m_IsActive = false;
        }

        /// <summary>
        /// 弾を更新する
        /// </summary>
        public void Update(float _deltaTime)
        {
            if (!m_IsActive)
                return;

            // 変更情報を更新
            UpdateChangesInternal();

            // 非表示弾（シューター）は移動しない
            if (!m_IsVisible)
            {
                return;
            }

            // 前回の位置を保存
            Vector3 oldPosition = m_Position;

            // 速度ベクトルを計算
            Vector3 velocity = GetVelocityVector();

            // 位置を更新
            m_Position += velocity * _deltaTime;

            // 加速度を適用
            m_Position += m_Acceleration * _deltaTime * _deltaTime * 0.5f;


        }

        /// <summary>
        /// 変更情報を更新する（テスト用public版）
        /// </summary>
        public void UpdateChanges(float _deltaTime)
        {
            UpdateChangesInternal();
            
            // 非表示弾（シューター）は移動しない
            if (!m_IsVisible)
            {
                return;
            }

            // 加速度による速度変化を累積（重力効果）
            Vector3 velocityChange = m_Acceleration * _deltaTime;
            m_AccumulatedVelocity += velocityChange;

            // 現在の実効速度ベクトルを計算
            Vector3 currentVelocity = GetVelocityVector();

            // 位置を更新
            m_Position += currentVelocity * _deltaTime;
        }

        /// <summary>
        /// 変更情報を更新する（内部実装）
        /// </summary>
        private void UpdateChangesInternal()
        {
            // 方向変更の処理
            if (m_DirectionChange.IsActive)
            {
                if (m_DirectionChange.IsCompleted)
                {
                    m_Direction = m_DirectionChange.TargetValue;
                    m_DirectionChange.IsActive = false;
                }
                else
                {
                    // フレームを先にインクリメントしてから計算
                    m_DirectionChange.IncrementFrame();
                    
                    // インクリメント後に完了判定を再チェック
                    if (m_DirectionChange.IsCompleted)
                    {
                        m_Direction = m_DirectionChange.TargetValue;
                        m_DirectionChange.IsActive = false;
                    }
                    else
                    {
                        float t = (float)m_DirectionChange.CurrentFrame / m_DirectionChange.Duration;
                        float oldDirection = m_Direction;
                        m_Direction = Mathf.Lerp(m_DirectionChange.StartValue, m_DirectionChange.TargetValue, t);
                    }
                }
            }

            // 速度変更の処理
            if (m_SpeedChange.IsActive)
            {
                if (m_SpeedChange.IsCompleted)
                {
                    m_Speed = m_SpeedChange.TargetValue;
                    m_SpeedChange.IsActive = false;
                }
                else
                {
                    // フレームを先にインクリメントしてから計算
                    m_SpeedChange.IncrementFrame();
                    
                    // インクリメント後に完了判定を再チェック
                    if (m_SpeedChange.IsCompleted)
                    {
                        m_Speed = m_SpeedChange.TargetValue;
                        m_SpeedChange.IsActive = false;
                    }
                    else
                    {
                        float t = (float)m_SpeedChange.CurrentFrame / m_SpeedChange.Duration;
                        m_Speed = Mathf.Lerp(m_SpeedChange.StartValue, m_SpeedChange.TargetValue, t);
                    }
                }
            }

            // 加速度の処理
            if (m_AccelInfo.IsActive)
            {
                if (m_AccelInfo.IsCompleted)
                {
                    // 完了時は最大値を設定して無効化
                    Vector3 finalAccel = new Vector3(
                        m_AccelInfo.HorizontalAccel,
                        m_AccelInfo.VerticalAccel,
                        0f
                    );
                    
                    // 座標系に応じて加速度を適用
                    if (m_CoordinateSystem == CoordinateSystem.YZ)
                    {
                        finalAccel = new Vector3(0f, finalAccel.y, finalAccel.x);
                    }
                    
                    m_Acceleration = finalAccel;
                    m_AccelInfo.IsActive = false;
                }
                else
                {
                    // フレームを先にインクリメントしてから計算
                    m_AccelInfo.IncrementFrame();
                    
                    // インクリメント後に完了判定を再チェック
                    if (m_AccelInfo.IsCompleted)
                    {
                        // 完了時は最大値を設定して無効化
                        Vector3 finalAccel = new Vector3(
                            m_AccelInfo.HorizontalAccel,
                            m_AccelInfo.VerticalAccel,
                            0f
                        );
                        
                        // 座標系に応じて加速度を適用
                        if (m_CoordinateSystem == CoordinateSystem.YZ)
                        {
                            finalAccel = new Vector3(0f, finalAccel.y, finalAccel.x);
                        }
                        
                        m_Acceleration = finalAccel;
                        m_AccelInfo.IsActive = false;
                    }
                    else
                    {
                        // termフレームかけて徐々に加速度を上げていく（changeDirectionやchangeSpeedと同様）
                        float t = (float)m_AccelInfo.CurrentFrame / m_AccelInfo.Duration;
                        
                        Vector3 currentAccel = new Vector3(
                            m_AccelInfo.HorizontalAccel * t,
                            m_AccelInfo.VerticalAccel * t,
                            0f
                        );
                        
                        // 座標系に応じて加速度を適用
                        if (m_CoordinateSystem == CoordinateSystem.YZ)
                        {
                            currentAccel = new Vector3(0f, currentAccel.y, currentAccel.x);
                        }
                        
                        m_Acceleration = currentAccel;
                    }
                }
            }
        }

        /// <summary>
        /// 現在の方向と速度から速度ベクトルを取得する
        /// </summary>
        public Vector3 GetVelocityVector()
        {
            Vector3 baseVelocity = ConvertAngleToVector(m_Direction, m_CoordinateSystem) * m_Speed;
            Vector3 totalVelocity = baseVelocity + m_AccumulatedVelocity;
            return totalVelocity;
        }

        /// <summary>
        /// 角度をベクトルに変換する
        /// </summary>
        public static Vector3 ConvertAngleToVector(float _angleDegrees, CoordinateSystem _coordinateSystem)
        {
            float angleRadians = _angleDegrees * Mathf.Deg2Rad;

            Vector3 result;
            
            switch (_coordinateSystem)
            {
                case CoordinateSystem.XY:
                    // XY面（縦スクロールシューティング）：上方向が0度、時計回り
                    // X軸が左右、Y軸が上下、Z軸は使用しない
                    result = new Vector3(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians), 0f);
                    break;

                case CoordinateSystem.YZ:
                    // YZ面（横スクロールシューティング）：上方向が0度、時計回り
                    // X軸は使用しない、Y軸が上下、Z軸が左右
                    result = new Vector3(0f, Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
                    break;

                default:
                    result = Vector3.zero;
                    Debug.LogError($"[ConvertAngleToVector] 不明な座標系: {_coordinateSystem}");
                    break;
            }

            return result;
        }

        /// <summary>
        /// アクションを追加する
        /// </summary>
        public void PushAction(BulletMLActionRunner _action)
        {
            m_ActionStack.Push(_action);
        }

        /// <summary>
        /// 現在のアクションを取得する
        /// </summary>
        public BulletMLActionRunner GetCurrentAction()
        {
            return m_ActionStack.Count > 0 ? m_ActionStack.Peek() : null;
        }

        /// <summary>
        /// 現在のアクションを終了する
        /// </summary>
        public void PopAction()
        {
            if (m_ActionStack.Count > 0)
                m_ActionStack.Pop();
        }
    }
}