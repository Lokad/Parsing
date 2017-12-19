using System.Text.RegularExpressions;
using Lokad.Parsing.Lexer;
using NUnit.Framework;

namespace Lokad.Parsing.Test.Lexer
{
    [TestFixture]
    public sealed class token_definition
    {
        [Test]
        public void regex()
        {
            var re = new Regex("abc");
            var td = new TokenDefinition(re);
            
            Assert.AreEqual(int.MaxValue, td.MaximumLength);

            Assert.IsTrue(td.StartsWith('a'));
            Assert.IsTrue(td.StartsWith('x'));

            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(0, td.MatchLength("xabcx", 2));

            Assert.AreEqual("abc", td.ToString());
        }

        [Test]
        public void regex_maximumLength()
        {
            var td = new TokenDefinition(new Regex("abc"), maximumLength: 3);
            Assert.AreEqual(3, td.MaximumLength);
        }

        [Test]
        public void regex_startsWith()
        {
            var td = new TokenDefinition(new Regex("abc"), startsWith: "a");
            
            Assert.IsTrue(td.StartsWith('a'));
            Assert.IsFalse(td.StartsWith('A'));
            Assert.IsFalse(td.StartsWith('x'));
        }

        [Test]
        public void set()
        {
            var td = new TokenDefinition(new[] {"ab", "abc", "bc"});

            Assert.AreEqual(3, td.MaximumLength);

            Assert.IsTrue(td.StartsWith('a'));
            Assert.IsTrue(td.StartsWith('A')); // Approximation, may change in the future
            Assert.IsFalse(td.StartsWith('x'));

            Assert.AreEqual("\\G(abc|ab|bc)", td.ToString());
            
            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(2, td.MatchLength("xabcx", 2));
            Assert.AreEqual(0, td.MatchLength("xaBcx", 1));
        }


        [Test]
        public void set_escape()
        {
            var td = new TokenDefinition(new[] { "[a-z]" });

            Assert.AreEqual(5, td.MaximumLength);

            Assert.IsTrue(td.StartsWith('['));
            Assert.IsFalse(td.StartsWith('x'));

            Assert.AreEqual("\\G(\\[a-z])", td.ToString());

            Assert.AreEqual(0, td.MatchLength("x[a-z]x", 0));
            Assert.AreEqual(5, td.MatchLength("x[a-z]x", 1));
            Assert.AreEqual(0, td.MatchLength("x[a-z]x", 2));
        }
        
        [Test]
        public void set_case_insensitive()
        {
            var td = new TokenDefinition(new[] { "ab", "abc", "bc" }, caseSensitive: false);

            Assert.AreEqual(3, td.MaximumLength);

            Assert.IsTrue(td.StartsWith('a'));
            Assert.IsTrue(td.StartsWith('A')); 
            Assert.IsFalse(td.StartsWith('x'));

            Assert.AreEqual("\\G(abc|ab|bc)", td.ToString());

            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(2, td.MatchLength("xabcx", 2));
            Assert.AreEqual(3, td.MatchLength("xaBcx", 1));
        }
    }
}
