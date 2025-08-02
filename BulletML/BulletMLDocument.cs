using System.Collections.Generic;
using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletMLドキュメント全体を管理するクラス
    /// </summary>
    [System.Serializable]
    public class BulletMLDocument
    {
        [SerializeField] private BulletMLElement m_RootElement;
        [SerializeField] private BulletMLType m_Type;
        [SerializeField] private Dictionary<string, BulletMLElement> m_LabeledBullets;
        [SerializeField] private Dictionary<string, BulletMLElement> m_LabeledActions;
        [SerializeField] private Dictionary<string, BulletMLElement> m_LabeledFires;

        public BulletMLElement RootElement => m_RootElement;
        public BulletMLType Type => m_Type;

        public BulletMLDocument()
        {
            m_LabeledBullets = new Dictionary<string, BulletMLElement>();
            m_LabeledActions = new Dictionary<string, BulletMLElement>();
            m_LabeledFires = new Dictionary<string, BulletMLElement>();
            m_Type = BulletMLType.none;
        }

        /// <summary>
        /// ルート要素を設定する
        /// </summary>
        public void SetRootElement(BulletMLElement _rootElement)
        {
            m_RootElement = _rootElement;
            
            // type属性を取得
            string typeStr = _rootElement.GetAttribute("type", "none");
            switch (typeStr)
            {
                case "vertical": m_Type = BulletMLType.vertical; break;
                case "horizontal": m_Type = BulletMLType.horizontal; break;
                default: m_Type = BulletMLType.none; break;
            }

            // ラベル付き要素をインデックス化
            IndexLabeledElements(_rootElement);
        }

        /// <summary>
        /// ラベル付き要素をインデックス化する
        /// </summary>
        private void IndexLabeledElements(BulletMLElement _element)
        {
            if (!string.IsNullOrEmpty(_element.Label))
            {
                switch (_element.ElementType)
                {
                    case BulletMLElementType.bullet:
                        m_LabeledBullets[_element.Label] = _element;
                        break;
                    case BulletMLElementType.action:
                        m_LabeledActions[_element.Label] = _element;
                        break;
                    case BulletMLElementType.fire:
                        m_LabeledFires[_element.Label] = _element;
                        break;
                }
            }

            foreach (var child in _element.Children)
            {
                IndexLabeledElements(child);
            }
        }

        /// <summary>
        /// ラベル付きbullet要素を取得する
        /// </summary>
        public BulletMLElement GetLabeledBullet(string _label)
        {
            return m_LabeledBullets.ContainsKey(_label) ? m_LabeledBullets[_label] : null;
        }

        /// <summary>
        /// ラベル付きaction要素を取得する
        /// </summary>
        public BulletMLElement GetLabeledAction(string _label)
        {
            return m_LabeledActions.ContainsKey(_label) ? m_LabeledActions[_label] : null;
        }

        /// <summary>
        /// ラベル付きfire要素を取得する
        /// </summary>
        public BulletMLElement GetLabeledFire(string _label)
        {
            return m_LabeledFires.ContainsKey(_label) ? m_LabeledFires[_label] : null;
        }

        /// <summary>
        /// "top"ラベルのアクションを取得する
        /// </summary>
        public BulletMLElement GetTopAction()
        {
            return GetLabeledAction("top");
        }
    }
}