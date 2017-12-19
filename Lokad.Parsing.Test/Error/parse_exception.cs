using Lokad.Parsing.Error;
using NUnit.Framework;

namespace Lokad.Parsing.Test.Error
{
    [TestFixture]
    public sealed class parse_exception
    {
        [Test]
        public void none()
        {
            var e = new ParseException("operator", new string[0], new SourceSpan(new SourceLocation(11, 1, 3), 5));
            Assert.AreEqual("Syntax error, unexpected operator.", e.Message);
            Assert.AreEqual("operator", e.Token);
            Assert.IsEmpty(e.Expected);
            Assert.AreEqual(new SourceSpan(new SourceLocation(11, 1, 3), 5), e.Location);
        }
        
        [Test]
        public void one()
        {
            var e = new ParseException("operator", new []{ "identifier" }, new SourceSpan(new SourceLocation(11, 1, 3), 5));
            Assert.AreEqual("Syntax error, found operator but expected identifier.", e.Message);
            Assert.AreEqual("operator", e.Token);
            CollectionAssert.AreEqual(new[]{ "identifier" }, e.Expected);
            Assert.AreEqual(new SourceSpan(new SourceLocation(11, 1, 3), 5), e.Location);
        }

        [Test]
        public void two()
        {
            var e = new ParseException("operator", new[] { "identifier", "'if'" }, new SourceSpan(new SourceLocation(11, 1, 3), 5));
            Assert.AreEqual("Syntax error, found operator but expected identifier or 'if'.", e.Message);
            Assert.AreEqual("operator", e.Token);
            CollectionAssert.AreEqual(new[] { "identifier", "'if'" }, e.Expected);
            Assert.AreEqual(new SourceSpan(new SourceLocation(11, 1, 3), 5), e.Location);
        }

        [Test]
        public void three()
        {
            var e = new ParseException("operator", new[] { "identifier", "'if'", "'else'" }, new SourceSpan(new SourceLocation(11, 1, 3), 5));
            Assert.AreEqual("Syntax error, found operator but expected identifier, 'if' or 'else'.", e.Message);
            Assert.AreEqual("operator", e.Token);
            CollectionAssert.AreEqual(new[] { "identifier", "'if'", "'else'" }, e.Expected);
            Assert.AreEqual(new SourceSpan(new SourceLocation(11, 1, 3), 5), e.Location);
        }


        [Test]
        public void at_least_one_char()
        {
            var e = new ParseException("operator", new string[0], new SourceSpan(new SourceLocation(11, 1, 3), 0));
            Assert.AreEqual(new SourceSpan(new SourceLocation(11, 1, 3), 1), e.Location);
        }
    }
}
