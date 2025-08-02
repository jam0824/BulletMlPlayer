using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// 変更の種類
    /// </summary>
    public enum BulletMLChangeType
    {
        Direction,
        Speed
    }

    /// <summary>
    /// 方向や速度の変更情報を管理するクラス
    /// </summary>
    [System.Serializable]
    public class BulletMLChangeInfo
    {
        [SerializeField] private BulletMLChangeType m_ChangeType;
        [SerializeField] private float m_StartValue;
        [SerializeField] private float m_TargetValue;
        [SerializeField] private int m_Duration;
        [SerializeField] private int m_CurrentFrame;
        [SerializeField] private bool m_IsActive;

        public BulletMLChangeType ChangeType
        {
            get => m_ChangeType;
            set => m_ChangeType = value;
        }

        public float StartValue
        {
            get => m_StartValue;
            set => m_StartValue = value;
        }

        public float TargetValue
        {
            get => m_TargetValue;
            set => m_TargetValue = value;
        }

        public int Duration
        {
            get => m_Duration;
            set => m_Duration = value;
        }

        public int CurrentFrame
        {
            get => m_CurrentFrame;
            set => m_CurrentFrame = value;
        }

        public bool IsActive
        {
            get => m_IsActive;
            set => m_IsActive = value;
        }

        public bool IsCompleted => m_CurrentFrame >= m_Duration;

        public BulletMLChangeInfo()
        {
            m_ChangeType = BulletMLChangeType.Direction;
            m_StartValue = 0f;
            m_TargetValue = 0f;
            m_Duration = 1;
            m_CurrentFrame = 0;
            m_IsActive = false;
        }

        /// <summary>
        /// フレームを進める
        /// </summary>
        public void IncrementFrame()
        {
            if (m_IsActive && m_CurrentFrame < m_Duration)
            {
                m_CurrentFrame++;
            }
        }

        /// <summary>
        /// リセットする
        /// </summary>
        public void Reset()
        {
            m_CurrentFrame = 0;
            m_IsActive = false;
        }
    }

    /// <summary>
    /// 加速情報を管理するクラス
    /// </summary>
    [System.Serializable]
    public class BulletMLAccelInfo
    {
        [SerializeField] private float m_HorizontalAccel;
        [SerializeField] private float m_VerticalAccel;
        [SerializeField] private int m_Duration;
        [SerializeField] private int m_CurrentFrame;
        [SerializeField] private bool m_IsActive;

        public float HorizontalAccel
        {
            get => m_HorizontalAccel;
            set => m_HorizontalAccel = value;
        }

        public float VerticalAccel
        {
            get => m_VerticalAccel;
            set => m_VerticalAccel = value;
        }

        public int Duration
        {
            get => m_Duration;
            set => m_Duration = value;
        }

        public int CurrentFrame
        {
            get => m_CurrentFrame;
            set => m_CurrentFrame = value;
        }

        public bool IsActive
        {
            get => m_IsActive;
            set => m_IsActive = value;
        }

        public bool IsCompleted => m_CurrentFrame >= m_Duration;

        public BulletMLAccelInfo()
        {
            m_HorizontalAccel = 0f;
            m_VerticalAccel = 0f;
            m_Duration = 1;
            m_CurrentFrame = 0;
            m_IsActive = false;
        }

        /// <summary>
        /// フレームを進める
        /// </summary>
        public void IncrementFrame()
        {
            if (m_IsActive && m_CurrentFrame < m_Duration)
            {
                m_CurrentFrame++;
            }
        }

        /// <summary>
        /// リセットする
        /// </summary>
        public void Reset()
        {
            m_CurrentFrame = 0;
            m_IsActive = false;
        }
    }
}