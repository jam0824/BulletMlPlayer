using NUnit.Framework;
using BulletML;
using System.Collections.Generic;

namespace BulletMLTests
{
    public class ExpressionEvaluatorTests
    {
        private ExpressionEvaluator m_Evaluator;

        [SetUp]
        public void Setup()
        {
            m_Evaluator = new ExpressionEvaluator();
        }

        [Test]
        public void Evaluate_SimpleNumber_ReturnsNumber()
        {
            // Act
            float result = m_Evaluator.Evaluate("35");

            // Assert
            Assert.AreEqual(35f, result, 0.001f);
        }

        [Test]
        public void Evaluate_DecimalNumber_ReturnsDecimal()
        {
            // Act
            float result = m_Evaluator.Evaluate("2.5");

            // Assert
            Assert.AreEqual(2.5f, result, 0.001f);
        }

        [Test]
        public void Evaluate_Addition_ReturnsSum()
        {
            // Act
            float result = m_Evaluator.Evaluate("10 + 5");

            // Assert
            Assert.AreEqual(15f, result, 0.001f);
        }

        [Test]
        public void Evaluate_Subtraction_ReturnsDifference()
        {
            // Act
            float result = m_Evaluator.Evaluate("10 - 3");

            // Assert
            Assert.AreEqual(7f, result, 0.001f);
        }

        [Test]
        public void Evaluate_Multiplication_ReturnsProduct()
        {
            // Act
            float result = m_Evaluator.Evaluate("4 * 3");

            // Assert
            Assert.AreEqual(12f, result, 0.001f);
        }

        [Test]
        public void Evaluate_Division_ReturnsQuotient()
        {
            // Act
            float result = m_Evaluator.Evaluate("15 / 3");

            // Assert
            Assert.AreEqual(5f, result, 0.001f);
        }

        [Test]
        public void Evaluate_Modulo_ReturnsRemainder()
        {
            // Act
            float result = m_Evaluator.Evaluate("17 % 5");

            // Assert
            Assert.AreEqual(2f, result, 0.001f);
        }

        [Test]
        public void Evaluate_Parentheses_RespectsOrderOfOperations()
        {
            // Act
            float result = m_Evaluator.Evaluate("(2 + 3) * 4");

            // Assert
            Assert.AreEqual(20f, result, 0.001f);
        }

        [Test]
        public void Evaluate_ComplexExpression_CalculatesCorrectly()
        {
            // Act
            float result = m_Evaluator.Evaluate("360/16");

            // Assert
            Assert.AreEqual(22.5f, result, 0.001f);
        }

        [Test]
        public void Evaluate_WithRandVariable_ReturnsValueBetween0And1()
        {
            // Arrange
            m_Evaluator.SetRandomValue(0.5f);

            // Act
            float result = m_Evaluator.Evaluate("$rand");

            // Assert
            Assert.GreaterOrEqual(result, 0f);
            Assert.LessOrEqual(result, 1f);
        }

        [Test]
        public void Evaluate_WithRankVariable_ReturnsRankValue()
        {
            // Arrange
            m_Evaluator.SetRankValue(0.7f);

            // Act
            float result = m_Evaluator.Evaluate("$rank");

            // Assert
            Assert.AreEqual(0.7f, result, 0.001f);
        }

        [Test]
        public void Evaluate_WithParameters_ReturnsParameterValue()
        {
            // Arrange
            var parameters = new Dictionary<int, float> { { 1, 10f }, { 2, 20f } };
            m_Evaluator.SetParameters(parameters);

            // Act
            float result1 = m_Evaluator.Evaluate("$1");
            float result2 = m_Evaluator.Evaluate("$2");

            // Assert
            Assert.AreEqual(10f, result1, 0.001f);
            Assert.AreEqual(20f, result2, 0.001f);
        }

        [Test]
        public void Evaluate_ComplexExpressionWithVariables_CalculatesCorrectly()
        {
            // Arrange
            m_Evaluator.SetRandomValue(0.5f);
            m_Evaluator.SetRankValue(0.8f);
            var parameters = new Dictionary<int, float> { { 1, 2f } };
            m_Evaluator.SetParameters(parameters);

            // Act
            float result = m_Evaluator.Evaluate("(2+$1)*0.3");

            // Assert
            Assert.AreEqual(1.2f, result, 0.001f); // (2+2)*0.3 = 1.2
        }

        [Test]
        public void Evaluate_ExpressionFromBulletMLSample_CalculatesCorrectly()
        {
            // Arrange
            m_Evaluator.SetRandomValue(0.5f);

            // Act
            float result = m_Evaluator.Evaluate("0.7 + 0.9*$rand");

            // Assert
            Assert.AreEqual(1.15f, result, 0.001f); // 0.7 + 0.9*0.5 = 1.15
        }

        [Test]
        public void Evaluate_RankExpression_CalculatesCorrectly()
        {
            // Arrange
            m_Evaluator.SetRankValue(0.5f);

            // Act
            float result = m_Evaluator.Evaluate("180-$rank*20");

            // Assert
            Assert.AreEqual(170f, result, 0.001f); // 180-0.5*20 = 170
        }
    }
}