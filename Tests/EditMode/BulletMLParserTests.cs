using NUnit.Framework;
using BulletML;

namespace BulletMLTests
{
    public class BulletMLParserTests
    {
        private BulletMLParser m_Parser;

        [SetUp]
        public void Setup()
        {
            m_Parser = new BulletMLParser();
        }

        [Test]
        public void Parse_SimpleXML_ReturnsCorrectDocument()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<!DOCTYPE bulletml SYSTEM ""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml/bulletml.dtd"">
<bulletml xmlns=""http://www.asahi-net.or.jp/~cs8k-cyu/bulletml"">
<action label=""top"">
<fire>
<bullet/>
</fire>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);

            // Assert
            Assert.IsNotNull(document);
            Assert.IsNotNull(document.RootElement);
            Assert.AreEqual(BulletMLElementType.bulletml, document.RootElement.ElementType);
            
            var topAction = document.GetTopAction();
            Assert.IsNotNull(topAction);
            Assert.AreEqual("top", topAction.Label);
        }

        [Test]
        public void Parse_WithTypeAttribute_SetsTypeCorrectly()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
<action label=""top"">
<fire><bullet/></fire>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);

            // Assert
            Assert.AreEqual(BulletMLType.vertical, document.Type);
        }

        [Test]
        public void Parse_WithDirection_ParsesCorrectly()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<fire>
<direction type=""absolute"">270</direction>
<speed>2</speed>
<bullet/>
</fire>
</action>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);

            // Assert
            var topAction = document.GetTopAction();
            var fireElement = topAction.GetChild(BulletMLElementType.fire);
            var directionElement = fireElement.GetChild(BulletMLElementType.direction);
            
            Assert.IsNotNull(directionElement);
            Assert.AreEqual("270", directionElement.Value);
            Assert.AreEqual(DirectionType.absolute, directionElement.GetDirectionType());
        }

        [Test]
        public void Parse_WithLabeledBullet_IndexesCorrectly()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml>
<action label=""top"">
<fire>
<bulletRef label=""downAccel""/>
</fire>
</action>
<bullet label=""downAccel"">
<direction>270</direction>
<speed>2</speed>
</bullet>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);

            // Assert
            var bullet = document.GetLabeledBullet("downAccel");
            Assert.IsNotNull(bullet);
            Assert.AreEqual("downAccel", bullet.Label);
            
            var direction = bullet.GetChild(BulletMLElementType.direction);
            Assert.IsNotNull(direction);
            Assert.AreEqual("270", direction.Value);
        }

        [Test]
        public void Parse_ComplexExample_ParsesCorrectly()
        {
            // Arrange
            string xml = @"<?xml version=""1.0"" ?>
<bulletml type=""vertical"">
<action label=""top"">
<repeat>
<times>100</times>
<action>
<fire>
<direction type=""sequence"">23</direction>
<bulletRef label=""straight""/>
</fire>
<wait>1</wait>
</action>
</repeat>
</action>
<bullet label=""straight"">
<action>
<wait>20+$rand*50</wait>
<changeDirection>
<direction type=""absolute"">180</direction>
<term>10</term>
</changeDirection>
</action>
</bullet>
</bulletml>";

            // Act
            var document = m_Parser.Parse(xml);

            // Assert
            Assert.AreEqual(BulletMLType.vertical, document.Type);
            
            var topAction = document.GetTopAction();
            Assert.IsNotNull(topAction);
            
            var repeatElement = topAction.GetChild(BulletMLElementType.repeat);
            Assert.IsNotNull(repeatElement);
            
            var timesElement = repeatElement.GetChild(BulletMLElementType.times);
            Assert.IsNotNull(timesElement);
            Assert.AreEqual("100", timesElement.Value);
            
            var straightBullet = document.GetLabeledBullet("straight");
            Assert.IsNotNull(straightBullet);
        }

        [Test]
        public void Parse_WithInvalidXML_ThrowsException()
        {
            // Arrange
            string invalidXml = "<invalid>incomplete";

            // Act & Assert
            Assert.Throws<System.Exception>(() => m_Parser.Parse(invalidXml));
        }

        [Test]
        public void Parse_EmptyString_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => m_Parser.Parse(""));
        }

        [Test]
        public void Parse_NullString_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => m_Parser.Parse(null));
        }
    }
}