using Lokad.Parsing.Lexer;
using NUnit.Framework;
using System.Linq;

namespace Lokad.Parsing.Test.Lexer
{
    [TestFixture]
    public sealed class lexer_result
    {
        [Test]
        public void fields()
        {
            var tokens = new[]
            {
                new LexerToken<int>(0, 0, 0),
                new LexerToken<int>(0, 0, 2),
                new LexerToken<int>(0, 2, 0)
            };

            var newlines = new[] {1, 5};

            var r = new LexerResult<int>(
                buffer: "foobar",
                tokens: tokens,
                newlines: newlines,
                hasInvalidTokens: true);

            Assert.IsTrue(r.HasInvalidTokens);
            Assert.AreEqual("foobar", r.Buffer);
            Assert.AreSame(tokens, r.Tokens);
            Assert.AreSame(newlines, r.Newlines);
            Assert.AreEqual(3, r.Count);
        }

        [Test]
        public void get_string()
        {
            var tokens = new[]
            {
                new LexerToken<int>(0,  0, 3),
                new LexerToken<int>(0,  4, 1),
                new LexerToken<int>(0,  5, 0),
                new LexerToken<int>(0,  8, 3),
                new LexerToken<int>(0, 11, 0)
            };
            
            var r = new LexerResult<int>(
                buffer: "foo +\n  bar",
                tokens: tokens,
                newlines: new[] { 5 },
                hasInvalidTokens: true);

            Assert.AreEqual("foo", r.GetString(0));
            Assert.AreEqual("+", r.GetString(1));
            Assert.AreEqual("", r.GetString(2));
            Assert.AreEqual("bar", r.GetString(3));
            Assert.AreEqual("", r.GetString(4));
        }
        
        [Test]
        public void get_string_pos()
        {
            var tokens = new[]
            {
                new LexerToken<int>(0,  0, 3),
                new LexerToken<int>(0,  4, 1),
                new LexerToken<int>(0,  5, 0),
                new LexerToken<int>(0,  8, 3),
                new LexerToken<int>(0, 11, 0)
            };

            var r = new LexerResult<int>(
                buffer: "foo +\n  bar",
                tokens: tokens,
                newlines: new[] { 5 },
                hasInvalidTokens: true);

            Assert.AreEqual("foo", r.GetStringPos(0).Value);
            Assert.AreEqual(new SourceSpan(new SourceLocation(0, 1, 1), 3), r.GetStringPos(0).Location);

            Assert.AreEqual("+", r.GetStringPos(1).Value);
            Assert.AreEqual(new SourceSpan(new SourceLocation(4, 1, 5), 1), r.GetStringPos(1).Location);

            Assert.AreEqual("", r.GetStringPos(2).Value);
            Assert.AreEqual(new SourceSpan(new SourceLocation(5, 1, 6), 0), r.GetStringPos(2).Location);

            Assert.AreEqual("bar", r.GetStringPos(3).Value);
            Assert.AreEqual(new SourceSpan(new SourceLocation(8, 2, 3), 3), r.GetStringPos(3).Location);

            Assert.AreEqual("", r.GetStringPos(4).Value);
            Assert.AreEqual(new SourceSpan(new SourceLocation(11, 2, 6), 0), r.GetStringPos(4).Location);
        }

        [Test]
        public void line_of_position()
        {
            var nl = new[] {2, 5, 7, 11, 13, 17, 19, 23};
            var r = new LexerResult<int>("", new LexerToken<int>[0], nl, false);

            var l = 1;
            var c = 1;
            for (var p = 0; p < 25; ++p)
            {
                r.LineOfPosition(p, out var line, out var column);
                Assert.AreEqual(l, line, $"line at {p}");
                Assert.AreEqual(c, column, $"column at {p}");

                if (nl.Contains(p))
                {
                    l += 1;
                    c = 1;
                }
                else
                {
                    c += 1;
                }
            }
        }
    }
}
