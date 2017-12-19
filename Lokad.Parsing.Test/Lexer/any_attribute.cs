using System;
using Lokad.Parsing.Lexer;
using NUnit.Framework;

namespace Lokad.Parsing.Test.Lexer
{
    [TestFixture]
    public sealed class any_attribute
    {
        [Test]
        public void empty()
        {
            Assert.Catch<ArgumentException>(
                // ReSharper disable once ObjectCreationAsStatement
                () => new AnyAttribute(),
                "Expected at least one value.");
        }

        [Test]
        public void one()
        {
            var a = new AnyAttribute("abc");
            CollectionAssert.AreEquivalent(new[]{"abc"}, a.Options);
            Assert.IsTrue(a.CaseSensitive);

            var td = a.ToDefinition();

            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(0, td.MatchLength("xabxx", 1));
            Assert.AreEqual(0, td.MatchLength("xaBcx", 1));
        }
        
        [Test]
        public void two()
        {
            var a = new AnyAttribute("ab", "abc");
            CollectionAssert.AreEquivalent(new[] { "ab", "abc" }, a.Options);
            Assert.IsTrue(a.CaseSensitive);

            var td = a.ToDefinition();

            Assert.AreEqual(0, td.MatchLength("xabcx", 0));
            Assert.AreEqual(3, td.MatchLength("xabcx", 1));
            Assert.AreEqual(2, td.MatchLength("xabxx", 1));
            Assert.AreEqual(0, td.MatchLength("xaBcx", 1));
        }
    }
}
