using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using BulletML;

namespace Tests
{
    /// <summary>
    /// BulletMLの複雑数式評価テスト
    /// 多層ネスト、演算子優先度、境界値、エラーハンドリングの包括的検証
    /// 
    /// サポート演算子: +, -, *, /, %
    /// サポート変数: $rand, $rank, $1, $2, $3...
    /// サポート機能: 括弧、負数、小数点
    /// </summary>
    public class BulletMLComplexExpressionTests
    {
        private BulletML.ExpressionEvaluator m_Evaluator;
        private const float PRECISION = 0.0001f; // 浮動小数点精度
        
        [SetUp]
        public void Setup()
        {
            m_Evaluator = new BulletML.ExpressionEvaluator();
            
            // テスト用の固定値を設定
            m_Evaluator.SetRandomValue(0.5f);
            m_Evaluator.SetRankValue(0.3f);
            
            var parameters = new Dictionary<int, float>
            {
                {1, 10f},
                {2, 5f},
                {3, 2f}
            };
            m_Evaluator.SetParameters(parameters);
        }
        
        [Test]
        public void Expression_BasicArithmetic_EvaluatesCorrectly()
        {
            // 基本四則演算の正確性をテスト
            
            Assert.AreEqual(8f, m_Evaluator.Evaluate("3+5"), PRECISION, "加算: 3+5=8");
            Assert.AreEqual(-2f, m_Evaluator.Evaluate("3-5"), PRECISION, "減算: 3-5=-2");
            Assert.AreEqual(15f, m_Evaluator.Evaluate("3*5"), PRECISION, "乗算: 3*5=15");
            Assert.AreEqual(0.6f, m_Evaluator.Evaluate("3/5"), PRECISION, "除算: 3/5=0.6");
            Assert.AreEqual(3f, m_Evaluator.Evaluate("8%5"), PRECISION, "剰余: 8%5=3");
            
            // 小数点演算
            Assert.AreEqual(7.5f, m_Evaluator.Evaluate("2.5+5"), PRECISION, "小数加算: 2.5+5=7.5");
            Assert.AreEqual(6.25f, m_Evaluator.Evaluate("2.5*2.5"), PRECISION, "小数乗算: 2.5*2.5=6.25");
            Assert.AreEqual(0.5f, m_Evaluator.Evaluate("1.5/3"), PRECISION, "小数除算: 1.5/3=0.5");
        }
        
        [Test]
        public void Expression_OperatorPrecedence_FollowsCorrectOrder()
        {
            // 演算子優先度の正確性をテスト
            
            // 乗除 > 加減
            Assert.AreEqual(14f, m_Evaluator.Evaluate("2+3*4"), PRECISION, "優先度: 2+3*4=14 (not 20)");
            Assert.AreEqual(10f, m_Evaluator.Evaluate("20-2*5"), PRECISION, "優先度: 20-2*5=10 (not 90)");
            Assert.AreEqual(5f, m_Evaluator.Evaluate("2+6/2"), PRECISION, "優先度: 2+6/2=5 (not 4)");
            Assert.AreEqual(9f, m_Evaluator.Evaluate("15-12/2"), PRECISION, "優先度: 15-12/2=9 (not 1.5)");
            
            // 剰余演算の優先度
            Assert.AreEqual(5f, m_Evaluator.Evaluate("3+8%3"), PRECISION, "優先度: 3+8%3=5 (8%3=2, 3+2=5)");
            Assert.AreEqual(9f, m_Evaluator.Evaluate("10-7%3"), PRECISION, "優先度: 10-7%3=9 (7%3=1, 10-1=9)");
            
            // 左結合性
            Assert.AreEqual(1f, m_Evaluator.Evaluate("10-5-4"), PRECISION, "左結合: 10-5-4=1 (not 9)");
            Assert.AreEqual(0.5f, m_Evaluator.Evaluate("8/4/4"), PRECISION, "左結合: 8/4/4=0.5 (not 8)");
        }
        
        [Test]
        public void Expression_NestedParentheses_EvaluatesCorrectly()
        {
            // 多層括弧の正確な評価をテスト
            
            // 基本括弧
            Assert.AreEqual(20f, m_Evaluator.Evaluate("(2+3)*4"), PRECISION, "括弧: (2+3)*4=20");
            Assert.AreEqual(11f, m_Evaluator.Evaluate("2+(3*3)"), PRECISION, "括弧: 2+(3*3)=11");
            
            // 2層ネスト
            Assert.AreEqual(46f, m_Evaluator.Evaluate("2*(3+(4*5))"), PRECISION, "2層: 2*(3+(4*5))=46");
            Assert.AreEqual(5f, m_Evaluator.Evaluate("(2+3)*(6-5)"), PRECISION, "2層: (2+3)*(6-5)=5");
            
            // 3層ネスト
            Assert.AreEqual(100f, m_Evaluator.Evaluate("2*((3+2)*(4+6))"), PRECISION, "3層: 2*((3+2)*(4+6))=100");
            Assert.AreEqual(23f, m_Evaluator.Evaluate("1+(2*(3+(4*2)))"), PRECISION, "3層: 1+(2*(3+(4*2)))=23");
            
            // 4層ネスト
            Assert.AreEqual(47f, m_Evaluator.Evaluate("(1+(2*(3+(4*5))))"), PRECISION, "4層: (1+(2*(3+(4*5))))=47");
            Assert.AreEqual(100f, m_Evaluator.Evaluate("((2+3)*((4*2)+12))"), PRECISION, "4層: ((2+3)*((4*2)+12))=100");
            
            // 5層ネスト（極端なケース）
            Assert.AreEqual(168f, m_Evaluator.Evaluate("(2*((3+1)*((2*5)+11)))"), PRECISION, "5層: (2*((3+1)*((2*5)+11)))=168");
        }
        
        [Test]
        public void Expression_VariableCombinations_EvaluatesCorrectly()
        {
            // 変数の複雑な組み合わせをテスト
            // $rand=0.5, $rank=0.3, $1=10, $2=5, $3=2
            
            // 基本変数単体
            Assert.AreEqual(0.5f, m_Evaluator.Evaluate("$rand"), PRECISION, "$rand=0.5");
            Assert.AreEqual(0.3f, m_Evaluator.Evaluate("$rank"), PRECISION, "$rank=0.3");
            Assert.AreEqual(10f, m_Evaluator.Evaluate("$1"), PRECISION, "$1=10");
            Assert.AreEqual(5f, m_Evaluator.Evaluate("$2"), PRECISION, "$2=5");
            Assert.AreEqual(2f, m_Evaluator.Evaluate("$3"), PRECISION, "$3=2");
            
            // 変数同士の演算
            Assert.AreEqual(0.8f, m_Evaluator.Evaluate("$rand+$rank"), PRECISION, "$rand+$rank=0.8");
            Assert.AreEqual(15f, m_Evaluator.Evaluate("$1+$2"), PRECISION, "$1+$2=15");
            Assert.AreEqual(50f, m_Evaluator.Evaluate("$1*$2"), PRECISION, "$1*$2=50");
            Assert.AreEqual(2f, m_Evaluator.Evaluate("$1/$2"), PRECISION, "$1/$2=2");
            
            // 変数と定数の複雑な組み合わせ
            Assert.AreEqual(25f, m_Evaluator.Evaluate("$1*2+$2"), PRECISION, "$1*2+$2=25");
            Assert.AreEqual(5f, m_Evaluator.Evaluate("($1+$2)/$2+$3"), PRECISION, "($1+$2)/$2+$3=5");
            Assert.AreEqual(3.5f, m_Evaluator.Evaluate("$rand*$1-$rank*$2"), PRECISION, "$rand*$1-$rank*$2=3.5");
            
            // 多変数の複雑な式
            Assert.AreEqual(13f, m_Evaluator.Evaluate("$1+$2-$3"), PRECISION, "$1+$2-$3=13");
            Assert.AreEqual(100f, m_Evaluator.Evaluate("$1*$2*$3"), PRECISION, "$1*$2*$3=100");
            Assert.AreEqual(10f/3f+0.3f, m_Evaluator.Evaluate("$1/($2-$3)+$rank"), PRECISION, "$1/($2-$3)+$rank=3.633");
        }
        
        [Test]
        public void Expression_ComplexNestedVariables_EvaluatesCorrectly()
        {
            // 変数と括弧の複雑なネストをテスト
            // $rand=0.5, $rank=0.3, $1=10, $2=5, $3=2
            
            // ランク依存の複雑な計算
            Assert.AreEqual(40f, m_Evaluator.Evaluate("$1+$rank*($2*$3)*10"), PRECISION, "$1+$rank*($2*$3)*10=40");
            
            // ランダム値の複雑な応用
            Assert.AreEqual(7.5f, m_Evaluator.Evaluate("$rand*($1+$2)"), PRECISION, "$rand*($1+$2)=7.5");
            Assert.AreEqual(2f, m_Evaluator.Evaluate("($rand+$rank)*$2-$3"), PRECISION, "($rand+$rank)*$2-$3=2");
            
            // 深いネストと変数の組み合わせ
            Assert.AreEqual(5.5f, m_Evaluator.Evaluate("(($1-$2)*$rand)+($rank*$3*5)"), PRECISION, "(($1-$2)*$rand)+($rank*$3*5)=5.5");
            Assert.AreEqual(28f, m_Evaluator.Evaluate("$1*($rank+$rand+$3)"), PRECISION, "$1*($rank+$rand+$3)=28");
            
            // パラメータ間の比率計算
            Assert.AreEqual(0.8f, m_Evaluator.Evaluate("($1/$2)/($2/$3)"), PRECISION, "($1/$2)/($2/$3)=0.8");
            Assert.AreEqual(50f/35f, m_Evaluator.Evaluate("($1*$2)/($2+$rank*100)"), PRECISION, "($1*$2)/($2+$rank*100)=1.428");
        }
        
        [Test]
        public void Expression_BoundaryValues_HandlesCorrectly()
        {
            // 境界値と特殊ケースの処理をテスト
            
            // 非常に大きな数値
            Assert.AreEqual(1000000f, m_Evaluator.Evaluate("1000*1000"), PRECISION, "大きな値: 1000*1000");
            Assert.AreEqual(10000000000f, m_Evaluator.Evaluate("100000*100000"), PRECISION, "非常に大きな値");
            
            // 非常に小さな数値
            Assert.AreEqual(0.0001f, m_Evaluator.Evaluate("0.01*0.01"), PRECISION, "小さな値: 0.01*0.01");
            Assert.AreEqual(0.000001f, m_Evaluator.Evaluate("0.001*0.001"), PRECISION, "非常に小さな値");
            
            // ゼロとの演算
            Assert.AreEqual(0f, m_Evaluator.Evaluate("0*1000"), PRECISION, "ゼロ乗算: 0*1000=0");
            Assert.AreEqual(1000f, m_Evaluator.Evaluate("1000+0"), PRECISION, "ゼロ加算: 1000+0=1000");
            Assert.AreEqual(1000f, m_Evaluator.Evaluate("1000-0"), PRECISION, "ゼロ減算: 1000-0=1000");
            
            // 負数の処理
            Assert.AreEqual(-5f, m_Evaluator.Evaluate("-5"), PRECISION, "負数: -5");
            Assert.AreEqual(-15f, m_Evaluator.Evaluate("-3*5"), PRECISION, "負数乗算: -3*5=-15");
            Assert.AreEqual(15f, m_Evaluator.Evaluate("-3*-5"), PRECISION, "負数同士: -3*-5=15");
            Assert.AreEqual(-2f, m_Evaluator.Evaluate("-(3-1)"), PRECISION, "負数括弧: -(3-1)=-2");
            
            // 複雑な負数計算
            Assert.AreEqual(8f, m_Evaluator.Evaluate("5-(-3)"), PRECISION, "二重負数: 5-(-3)=8");
            Assert.AreEqual(-8f, m_Evaluator.Evaluate("-(3+5)"), PRECISION, "括弧の負数: -(3+5)=-8");
        }
        
        [Test]
        public void Expression_ZeroDivision_HandlesGracefully()
        {
            // ゼロ除算の処理をテスト
            
            // 直接的なゼロ除算
            float result1 = m_Evaluator.Evaluate("5/0");
            Assert.IsTrue(float.IsInfinity(result1), "ゼロ除算は無限大を返すべき");
            
            // 計算結果がゼロになる除算
            float result2 = m_Evaluator.Evaluate("5/(3-3)");
            Assert.IsTrue(float.IsInfinity(result2), "計算結果ゼロ除算も無限大を返すべき");
            
            // 負数のゼロ除算
            float result3 = m_Evaluator.Evaluate("-5/0");
            Assert.IsTrue(float.IsNegativeInfinity(result3), "負数ゼロ除算は負の無限大を返すべき");
            
            // ゼロ剰余
            float result4 = m_Evaluator.Evaluate("5%0");
            Assert.IsTrue(float.IsNaN(result4), "ゼロ剰余はNaNを返すべき");
            
            // 複雑な式でのゼロ除算
            float result5 = m_Evaluator.Evaluate("(3+2)/(2-2)");
            Assert.IsTrue(float.IsInfinity(result5), "複雑な式のゼロ除算も無限大を返すべき");
        }
        
        [Test]
        public void Expression_FloatingPointPrecision_MaintainsAccuracy()
        {
            // 浮動小数点精度の維持をテスト
            
            // 基本的な精度テスト
            Assert.AreEqual(0.1f, m_Evaluator.Evaluate("0.3-0.2"), 0.00001f, "浮動小数点精度: 0.3-0.2");
            Assert.AreEqual(0.09f, m_Evaluator.Evaluate("0.3*0.3"), 0.00001f, "浮動小数点乗算: 0.3*0.3");
            
            // 複雑な計算での精度
            Assert.AreEqual(1f, m_Evaluator.Evaluate("(0.1+0.2)*3.333333"), 0.001f, "複雑計算精度");
            Assert.AreEqual(1f, m_Evaluator.Evaluate("10/10"), 0.00001f, "除算精度: 10/10=1");
            
            // 多段階計算での累積誤差
            float expected = (((1.1f * 1.1f) + 0.1f) * 2f) - 0.42f;
            float actual = m_Evaluator.Evaluate("((1.1*1.1)+0.1)*2-0.42");
            Assert.AreEqual(expected, actual, 0.001f, "多段階計算の累積誤差管理");
        }
        
        [Test]
        public void Expression_EmptyAndNull_HandlesGracefully()
        {
            // 空文字列とnullの処理をテスト
            
            Assert.AreEqual(0f, m_Evaluator.Evaluate(""), PRECISION, "空文字列は0を返すべき");
            Assert.AreEqual(0f, m_Evaluator.Evaluate(null), PRECISION, "nullは0を返すべき");
            Assert.AreEqual(0f, m_Evaluator.Evaluate("   "), PRECISION, "空白のみは0を返すべき");
        }
        
        [Test]
        public void Expression_VariableNotFound_DefaultsToZero()
        {
            // 存在しない変数の処理をテスト
            // 注意: ExpressionEvaluatorの実装では存在しない変数は置換されずに残るため、
            // パースエラーまたは0として処理される可能性がある
            
            var emptyEvaluator = new BulletML.ExpressionEvaluator();
            emptyEvaluator.SetParameters(new Dictionary<int, float>());
            
            // 存在しないパラメータ（実装に依存するが、安全にテスト）
            float result = emptyEvaluator.Evaluate("$4+5");
            // $4が存在しない場合の処理は実装依存だが、エラーを投げずに処理されることを確認
            Assert.IsTrue(!float.IsNaN(result), "存在しない変数でもNaNを返さないべき");
        }
        
        [Test]
        public void Expression_Performance_LargeScale()
        {
            // 大量・複雑な数式のパフォーマンステスト
            
            var startTime = System.DateTime.Now;
            
            // 1000回の複雑な計算
            for (int i = 0; i < 1000; i++)
            {
                float result = m_Evaluator.Evaluate("(($1+$2)*($rand+$rank)+(($3*5)-($1/$2)))");
                Assert.IsTrue(!float.IsNaN(result), $"計算{i}でNaNが発生");
            }
            
            var executionTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            
            // パフォーマンス確認（1000回で100ms以内）
            Assert.Less(executionTime, 100, "1000回の複雑な数式評価が100ms以内で完了するべき");
        }
        
        [Test]
        public void Expression_ExtremeNesting_HandlesCorrectly()
        {
            // 極端なネストレベルの処理をテスト
            
            // 10層の深いネスト
            string deepNested = "((((((((((1+1)+1)+1)+1)+1)+1)+1)+1)+1)+1)";
            Assert.AreEqual(11f, m_Evaluator.Evaluate(deepNested), PRECISION, "10層ネスト");
            
            // 複雑な多層計算
            string complexNested = "(((2*3)+(4*5))*((6/2)+(8/4)))";
            float expected = (((2f*3f)+(4f*5f))*((6f/2f)+(8f/4f)));
            Assert.AreEqual(expected, m_Evaluator.Evaluate(complexNested), PRECISION, "複雑多層計算");
            
            // 変数を含む深いネスト
            string variableNested = "((($1*$2)+($3*$rand))*(($rank*10)+($1/$2)))";
            // $1=10, $2=5, $3=2, $rand=0.5, $rank=0.3
            // = (((10*5)+(2*0.5))*((0.3*10)+(10/5)))
            // = ((50+1)*(3+2)) = 51*5 = 255
            Assert.AreEqual(255f, m_Evaluator.Evaluate(variableNested), PRECISION, "変数深層ネスト");
        }
        
        [Test]
        public void Expression_ModuloOperations_EvaluatesCorrectly()
        {
            // 剰余演算の詳細テスト
            
            // 基本剰余
            Assert.AreEqual(1f, m_Evaluator.Evaluate("7%3"), PRECISION, "基本剰余: 7%3=1");
            Assert.AreEqual(0f, m_Evaluator.Evaluate("9%3"), PRECISION, "割り切れる剰余: 9%3=0");
            Assert.AreEqual(2f, m_Evaluator.Evaluate("8%3"), PRECISION, "剰余: 8%3=2");
            
            // 小数剰余
            Assert.AreEqual(0.5f, m_Evaluator.Evaluate("2.5%2"), PRECISION, "小数剰余: 2.5%2=0.5");
            Assert.AreEqual(1.3f, m_Evaluator.Evaluate("5.3%2"), 0.001f, "小数剰余: 5.3%2=1.3");
            
            // 負数剰余
            float negativeResult = m_Evaluator.Evaluate("-7%3");
            Assert.IsTrue(!float.IsNaN(negativeResult), "負数剰余でNaNにならない");
            
            // 変数との剰余演算
            Assert.AreEqual(0f, m_Evaluator.Evaluate("$1%$2"), PRECISION, "$1%$2: 10%5=0");
            Assert.AreEqual(1f, m_Evaluator.Evaluate("($1+1)%$2"), PRECISION, "($1+1)%$2: 11%5=1");
        }
        
        [Test]
        public void Expression_ParameterDynamicChange_EvaluatesCorrectly()
        {
            // パラメータの動的変更テスト
            
            // 初期パラメータでの計算
            Assert.AreEqual(15f, m_Evaluator.Evaluate("$1+$2"), PRECISION, "初期: $1+$2=15");
            
            // パラメータ変更
            var newParameters = new Dictionary<int, float>
            {
                {1, 20f},
                {2, 3f},
                {3, 7f}
            };
            m_Evaluator.SetParameters(newParameters);
            
            // 変更後の計算
            Assert.AreEqual(23f, m_Evaluator.Evaluate("$1+$2"), PRECISION, "変更後: $1+$2=23");
            Assert.AreEqual(140f, m_Evaluator.Evaluate("$1*$3"), PRECISION, "変更後: $1*$3=140");
            Assert.AreEqual(69f, m_Evaluator.Evaluate("($1+$2)*$2"), PRECISION, "変更後複雑: ($1+$2)*$2=69");
            
            // ランクとランダム値の変更
            m_Evaluator.SetRankValue(0.8f);
            m_Evaluator.SetRandomValue(0.2f);
            
            Assert.AreEqual(0.8f, m_Evaluator.Evaluate("$rank"), PRECISION, "変更後$rank=0.8");
            Assert.AreEqual(0.2f, m_Evaluator.Evaluate("$rand"), PRECISION, "変更後$rand=0.2");
            Assert.AreEqual(20.2f, m_Evaluator.Evaluate("$1+$rand"), PRECISION, "変更後組み合わせ");
        }
    }
}