using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BulletML
{
    /// <summary>
    /// BulletMLの数式を評価するクラス
    /// </summary>
    public class ExpressionEvaluator
    {
        private float m_RandValue;
        private float m_RankValue;
        private Dictionary<int, float> m_Parameters;

        public ExpressionEvaluator()
        {
            m_RandValue = Random.Range(0f, 1f);
            m_RankValue = 0.5f; // デフォルト難易度
            m_Parameters = new Dictionary<int, float>();
        }

        /// <summary>
        /// ランダム値を設定する（テスト用）
        /// </summary>
        public void SetRandomValue(float _value)
        {
            m_RandValue = Mathf.Clamp01(_value);
        }

        /// <summary>
        /// ランク値（難易度）を設定する
        /// </summary>
        public void SetRankValue(float _value)
        {
            m_RankValue = Mathf.Clamp01(_value);
        }

        /// <summary>
        /// パラメータを設定する
        /// </summary>
        public void SetParameters(Dictionary<int, float> _parameters)
        {
            m_Parameters = _parameters ?? new Dictionary<int, float>();
        }

        /// <summary>
        /// 数式を評価する
        /// </summary>
        public float Evaluate(string _expression)
        {
            if (string.IsNullOrEmpty(_expression))
                return 0f;

            // 変数を置換する
            string processedExpression = SubstituteVariables(_expression);

            // 数式を評価する
            return EvaluateExpression(processedExpression);
        }

        /// <summary>
        /// 変数を置換する
        /// </summary>
        private string SubstituteVariables(string _expression)
        {
            string result = _expression;

            // $randを置換
            result = result.Replace("$rand", m_RandValue.ToString("F6"));

            // $rankを置換
            result = result.Replace("$rank", m_RankValue.ToString("F6"));

            // $1, $2, $3... を置換
            foreach (var param in m_Parameters)
            {
                string variable = "$" + param.Key;
                result = result.Replace(variable, param.Value.ToString("F6"));
            }

            return result;
        }

        /// <summary>
        /// 数式を評価する（再帰的降下パーサー）
        /// </summary>
        private float EvaluateExpression(string _expression)
        {
            // 空白を除去
            _expression = Regex.Replace(_expression, @"\s+", "");

            return ParseExpression(_expression, 0).value;
        }

        /// <summary>
        /// 式をパースする
        /// </summary>
        private (float value, int nextIndex) ParseExpression(string _expr, int _index)
        {
            var left = ParseTerm(_expr, _index);
            _index = left.nextIndex;

            while (_index < _expr.Length)
            {
                char op = _expr[_index];
                if (op == '+' || op == '-')
                {
                    _index++;
                    var right = ParseTerm(_expr, _index);
                    _index = right.nextIndex;

                    if (op == '+')
                        left.value += right.value;
                    else
                        left.value -= right.value;
                }
                else
                {
                    break;
                }
            }

            return (left.value, _index);
        }

        /// <summary>
        /// 項をパースする
        /// </summary>
        private (float value, int nextIndex) ParseTerm(string _expr, int _index)
        {
            var left = ParseFactor(_expr, _index);
            _index = left.nextIndex;

            while (_index < _expr.Length)
            {
                char op = _expr[_index];
                if (op == '*' || op == '/' || op == '%')
                {
                    _index++;
                    var right = ParseFactor(_expr, _index);
                    _index = right.nextIndex;

                    if (op == '*')
                        left.value *= right.value;
                    else if (op == '/')
                        left.value /= right.value;
                    else // %
                        left.value %= right.value;
                }
                else
                {
                    break;
                }
            }

            return (left.value, _index);
        }

        /// <summary>
        /// 因子をパースする
        /// </summary>
        private (float value, int nextIndex) ParseFactor(string _expr, int _index)
        {
            if (_index >= _expr.Length)
                return (0f, _index);

            // 負の数
            if (_expr[_index] == '-')
            {
                var result = ParseFactor(_expr, _index + 1);
                return (-result.value, result.nextIndex);
            }

            // 正の数（+は無視）
            if (_expr[_index] == '+')
            {
                return ParseFactor(_expr, _index + 1);
            }

            // 括弧
            if (_expr[_index] == '(')
            {
                var result = ParseExpression(_expr, _index + 1);
                // ')'をスキップ
                return (result.value, result.nextIndex + 1);
            }

            // 数値
            int startIndex = _index;
            while (_index < _expr.Length &&
                   (char.IsDigit(_expr[_index]) || _expr[_index] == '.'))
            {
                _index++;
            }

            if (_index > startIndex)
            {
                string numberStr = _expr.Substring(startIndex, _index - startIndex);
                if (float.TryParse(numberStr, out float value))
                {
                    return (value, _index);
                }
            }

            return (0f, _index);
        }
    }
}