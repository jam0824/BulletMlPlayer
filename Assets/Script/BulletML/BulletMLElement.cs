using System.Collections.Generic;
using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletML要素を表すクラス
    /// </summary>
    [System.Serializable]
    public class BulletMLElement
    {
        [SerializeField] private BulletMLElementType m_ElementType;
        [SerializeField] private string m_Label;
        [SerializeField] private string m_Value;
        [SerializeField] private Dictionary<string, string> m_Attributes;
        [SerializeField] private List<BulletMLElement> m_Children;

        public BulletMLElementType ElementType => m_ElementType;
        public string Label => m_Label;
        public string Value => m_Value;
        public Dictionary<string, string> Attributes => m_Attributes;
        public List<BulletMLElement> Children => m_Children;

        public BulletMLElement(BulletMLElementType _elementType, string _label = null, string _value = null)
        {
            m_ElementType = _elementType;
            m_Label = _label;
            m_Value = _value;
            m_Attributes = new Dictionary<string, string>();
            m_Children = new List<BulletMLElement>();
        }

        /// <summary>
        /// 属性を追加する
        /// </summary>
        public void AddAttribute(string _key, string _value)
        {
            m_Attributes[_key] = _value;
        }

        /// <summary>
        /// 子要素を追加する
        /// </summary>
        public void AddChild(BulletMLElement _child)
        {
            m_Children.Add(_child);
        }

        /// <summary>
        /// 指定したタイプの最初の子要素を取得する
        /// </summary>
        public BulletMLElement GetChild(BulletMLElementType _type)
        {
            foreach (var child in m_Children)
            {
                if (child.ElementType == _type)
                    return child;
            }
            return null;
        }

        /// <summary>
        /// 指定したタイプの全ての子要素を取得する
        /// </summary>
        public List<BulletMLElement> GetChildren(BulletMLElementType _type)
        {
            List<BulletMLElement> listResult = new List<BulletMLElement>();
            foreach (var child in m_Children)
            {
                if (child.ElementType == _type)
                    listResult.Add(child);
            }
            return listResult;
        }

        /// <summary>
        /// 属性値を取得する
        /// </summary>
        public string GetAttribute(string _key, string _defaultValue = null)
        {
            return m_Attributes.ContainsKey(_key) ? m_Attributes[_key] : _defaultValue;
        }

        /// <summary>
        /// type属性を取得する（direction用）
        /// </summary>
        public DirectionType GetDirectionType()
        {
            string typeStr = GetAttribute("type", "aim");
            switch (typeStr)
            {
                case "aim": return DirectionType.aim;
                case "absolute": return DirectionType.absolute;
                case "relative": return DirectionType.relative;
                case "sequence": return DirectionType.sequence;
                default: return DirectionType.aim;
            }
        }

        /// <summary>
        /// type属性を取得する（speed用）
        /// </summary>
        public SpeedType GetSpeedType()
        {
            string typeStr = GetAttribute("type", "absolute");
            switch (typeStr)
            {
                case "absolute": return SpeedType.absolute;
                case "relative": return SpeedType.relative;
                case "sequence": return SpeedType.sequence;
                default: return SpeedType.absolute;
            }
        }

        /// <summary>
        /// type属性を取得する（加速度用）
        /// </summary>
        public AccelType GetAccelType()
        {
            string typeStr = GetAttribute("type", "absolute");
            switch (typeStr)
            {
                case "absolute": return AccelType.absolute;
                case "relative": return AccelType.relative;
                case "sequence": return AccelType.sequence;
                default: return AccelType.absolute;
            }
        }
    }
}