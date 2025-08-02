using System;
using System.Xml;
using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletMLのXMLを解析するクラス
    /// </summary>
    public class BulletMLParser
    {
        /// <summary>
        /// XMLからBulletMLDocumentを作成する
        /// </summary>
        public BulletMLDocument Parse(string _xmlContent)
        {
            if (string.IsNullOrEmpty(_xmlContent))
                throw new ArgumentException("XML content cannot be null or empty");

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(_xmlContent);

                var document = new BulletMLDocument();
                var rootElement = ParseElement(xmlDoc.DocumentElement);
                document.SetRootElement(rootElement);

                return document;
            }
            catch (XmlException ex)
            {
                throw new Exception($"Failed to parse BulletML XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// XMLエレメントをBulletMLElementに変換する
        /// </summary>
        private BulletMLElement ParseElement(XmlNode _xmlNode)
        {
            if (_xmlNode == null)
                return null;

            // 要素タイプを取得
            var elementType = GetElementType(_xmlNode.Name);
            
            // ラベル属性を取得
            string label = GetAttributeValue(_xmlNode, "label");
            
            // 要素の値（テキストコンテント）を取得
            string value = GetTextContent(_xmlNode);

            var element = new BulletMLElement(elementType, label, value);

            // 属性を追加
            if (_xmlNode.Attributes != null)
            {
                foreach (XmlAttribute attr in _xmlNode.Attributes)
                {
                    element.AddAttribute(attr.Name, attr.Value);
                }
            }

            // 子要素を追加
            foreach (XmlNode childNode in _xmlNode.ChildNodes)
            {
                // テキストノードや改行は無視
                if (childNode.NodeType == XmlNodeType.Element)
                {
                    var childElement = ParseElement(childNode);
                    if (childElement != null)
                    {
                        element.AddChild(childElement);
                    }
                }
            }

            return element;
        }

        /// <summary>
        /// 要素名からBulletMLElementTypeを取得する
        /// </summary>
        private BulletMLElementType GetElementType(string _elementName)
        {
            switch (_elementName)
            {
                case "bulletml": return BulletMLElementType.bulletml;
                case "bullet": return BulletMLElementType.bullet;
                case "action": return BulletMLElementType.action;
                case "fire": return BulletMLElementType.fire;
                case "changeDirection": return BulletMLElementType.changeDirection;
                case "changeSpeed": return BulletMLElementType.changeSpeed;
                case "accel": return BulletMLElementType.accel;
                case "wait": return BulletMLElementType.wait;
                case "vanish": return BulletMLElementType.vanish;
                case "repeat": return BulletMLElementType.repeat;
                case "direction": return BulletMLElementType.direction;
                case "speed": return BulletMLElementType.speed;
                case "horizontal": return BulletMLElementType.horizontal;
                case "vertical": return BulletMLElementType.vertical;
                case "term": return BulletMLElementType.term;
                case "times": return BulletMLElementType.times;
                case "bulletRef": return BulletMLElementType.bulletRef;
                case "actionRef": return BulletMLElementType.actionRef;
                case "fireRef": return BulletMLElementType.fireRef;
                case "param": return BulletMLElementType.param;
                default:
                    Debug.LogWarning($"Unknown BulletML element: {_elementName}");
                    return BulletMLElementType.action; // デフォルト
            }
        }

        /// <summary>
        /// 属性値を取得する
        /// </summary>
        private string GetAttributeValue(XmlNode _node, string _attributeName)
        {
            if (_node.Attributes == null)
                return null;

            var attr = _node.Attributes[_attributeName];
            return attr?.Value;
        }

        /// <summary>
        /// 要素のテキストコンテントを取得する
        /// </summary>
        private string GetTextContent(XmlNode _node)
        {
            // 子要素がない場合のみテキストコンテントを取得
            bool hasElementChildren = false;
            foreach (XmlNode child in _node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    hasElementChildren = true;
                    break;
                }
            }

            if (!hasElementChildren)
            {
                return _node.InnerText?.Trim();
            }

            return null;
        }
    }
}