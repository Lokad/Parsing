using System;
using Lokad.Parsing.Lexer;
using NUnit.Framework;

namespace Lokad.Parsing.Test.Lexer
{
    [TestFixture]
    public sealed class any_ci_attribute
    {
        [Test]
        public void empty()
        {
            Assert.Catch<ArgumentException>(
                // ReSharper disable once ObjectCreationAsStatement
                () => new AnyCiAttribute(),
                "Expected at least one value.");
        }

        [Test]
        public void one()
        {
            var a = new AnyCiAttribute("abc");
            CollectionAssert.AreEquivalent(new[]{"abc"}, a.Options);
            Assert.IsFalse(a.CaseSensitive);

            var td = a.ToDefinition();

            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(0, td.MatchLength("xabxx", 1));
            Assert.AreEqual(3, td.MatchLength("xaBcx", 1));
        }
        
        [Test]
        public void two()
        {
            var a = new AnyCiAttribute("ab", "abc");
            CollectionAssert.AreEquivalent(new[] { "ab", "abc" }, a.Options);
            Assert.IsFalse(a.CaseSensitive);

            var td = a.ToDefinition();

            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(2, td.MatchLength("xabxx", 1));
            Assert.AreEqual(3, td.MatchLength("xaBcx", 1));
        }
    }
}
