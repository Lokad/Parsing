using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing.Lexer;
using NUnit.Framework;

namespace Lokad.Parsing.Test.Lexer
{
    [TestFixture]
    public sealed class reflection_token_reader
    {
        [Tokens(Comments = "//[^\\n]*")]
        private enum Tok
        {
            [Error] Error,
            [End] EoS,
            [EndOfLine] EoL,
            [Indent] Indent,
            [Dedent] Dedent,

            [PatternCi("[a-z]+")] Identifier,
            [Ci, From((int)Identifier, true)] If,
            [Ci, From((int)Identifier, true)] Then,
            [Ci, From((int)Identifier, true)] Else,
            [Ci, From((int)Identifier)] This,

            [Any("+", "-", "*", "/"), Infix] Binary,
            [Any("--"), Infix(CanBePrefix = true)] Decrement,
            [Any("++"), Infix(CanBePostfix = true)] Increment
        }

        private readonly TokenReader<Tok> _reader = new ReflectionTokenReader<Tok>();
        private IReadOnlyList<LexerToken<Tok>> _read;
        private int _tok;
        private int _pos;

        private void With(string txt, bool truncate = false)
        {
            var result = _reader.ReadAllTokens(txt, isInputTruncated: truncate);

            CollectionAssert.AreEquivalent(
                txt.Select((c, i) => c == '\n' ? i : -1).Where(i => i >= 0).ToArray(),
                result.Newlines);

            Assert.AreEqual(result.Buffer, txt);
            Assert.AreEqual(result.Tokens.Any(t => t.Token == Tok.Error), result.HasInvalidTokens);

            _read = result.Tokens;

            _tok = _pos = 0;
        }

        private void Is(Tok t, int skip = 0, int len = 0)
        {
            var lt = _read[_tok++];
            Assert.AreEqual(t, lt.Token);
            _pos += skip;
            Assert.AreEqual(_pos, lt.Start);
            Assert.AreEqual(len, lt.Length);
            _pos += len;
        }

        [Test]
        public void empty()
        {
            With("");
            Is(Tok.EoS);
        }

        [Test]
        public void fields()
        {
            Assert.AreEqual(Tok.Error, _reader.Error);
            Assert.AreEqual(Tok.EoS, _reader.EndOfStream);
            Assert.AreEqual(Tok.Indent, _reader.Indent);
            Assert.AreEqual(Tok.Dedent, _reader.Dedent);
            Assert.AreEqual(Tok.EoL, _reader.EndOfLine);

            Assert.IsFalse(_reader.EscapeNewlines);

            Assert.AreEqual(8, _reader.PublicChildren.Count); // One for each enumeration key

            foreach (var kv in _reader.PublicChildren)
            {
                if (kv.Key == Tok.Identifier)
                    CollectionAssert.AreEquivalent(new[] { Tok.This }, kv.Value);
                else
                    Assert.IsEmpty(kv.Value);
            }
        }

        [Test]
        public void empty_collapsed()
        {
            With("\n\n\n");
            Is(Tok.EoS, 3);
        }

        [Test]
        public void identifier()
        {
            With("abc");
            Is(Tok.Identifier, 0, 3);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void identifiers()
        {
            With("abc def\nghi jkl");
            Is(Tok.Identifier, 0, 3);
            Is(Tok.Identifier, 1, 3);
            Is(Tok.EoL);
            Is(Tok.Identifier, 1, 3);
            Is(Tok.Identifier, 1, 3);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void keywords()
        {
            With("if then\nelse this");
            Is(Tok.If, 0, 2);
            Is(Tok.Then, 1, 4);
            Is(Tok.EoL);
            Is(Tok.Else, 1, 4);
            Is(Tok.This, 1, 4);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void keywords_case_insensitive()
        {
            With("iF thEn\neLse thiS");
            Is(Tok.If, 0, 2);
            Is(Tok.Then, 1, 4);
            Is(Tok.EoL);
            Is(Tok.Else, 1, 4);
            Is(Tok.This, 1, 4);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void operators()
        {
            With("+++a---");
            Is(Tok.Increment, 0, 2);
            Is(Tok.Binary, 0, 1);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Decrement, 0, 2);
            Is(Tok.Binary, 0, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void empty_lines_collapse()
        {
            With("a\n\n\nb");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Identifier, 3, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void indent()
        {
            With("a\n  b");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void indent_tabs()
        {
            // '\t' == '  '
            With("a\n   b\n \tc");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 4);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Identifier, 3, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void indent_empty_lines()
        {
            With("a\n  b\n \n   \n  c");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Identifier, 9, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void indent_nested()
        {
            With("a\n  b\n    c\n  d\ne");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 5);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent, 1);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void indented_comment()
        {
            With("a\n  //Comment!\nb");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Identifier, 14, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void indent_mismatch()
        {
            With("a\n    b\n  c");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 5);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent, 3);
            Is(Tok.Indent);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void indent_truncate()
        {
            With("a\n  b\n    c", truncate: true);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 5);
            Is(Tok.Identifier, 0, 1);
            Assert.AreEqual(_read.Count, _tok);
        }

        [Test]
        public void escape()
        {
            With("a\\\n  b");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Error, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void escape_comment()
        {
            With("a \\ // A comment ! \n  b");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Error, 1, 1);
            Is(Tok.EoL, 16);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void error()
        {
            With("a%b");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Error, 0, 1);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void errors()
        {
            With("a%$b");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Error, 0, 1);
            Is(Tok.Error, 0, 1);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void not_prefix()
        {
            With("a\n/ b\n  / c\n  d");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Binary, 1, 1);
            Is(Tok.Identifier, 1, 1);
            // Ignored EoL + Ident
            Is(Tok.Binary, 3, 1);
            Is(Tok.Identifier, 1, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }

        [Test]
        public void not_prefix_empty_lines()
        {
            With("b\n  \n  / c");
            Is(Tok.Identifier, 0, 1);
            // Ignored EoL + Ident
            Is(Tok.Binary, 6, 1);
            Is(Tok.Identifier, 1, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void not_prefix_comments()
        {
            With("b // Foo\n  // Bar\n  / c");
            Is(Tok.Identifier, 0, 1);
            // Ignored EoL + Ident
            Is(Tok.Binary, 19, 1);
            Is(Tok.Identifier, 1, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void not_suffix_empty_lines()
        {
            With("b /\n  \n  c");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Binary, 1, 1);
            // Ignored EoL + Ident
            Is(Tok.Identifier, 6, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void not_suffix_comments()
        {
            With("b / // Foo\n  // Bar\n  c");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Binary, 1, 1);
            // Ignored EoL + Ident
            Is(Tok.Identifier, 19, 1);
            Is(Tok.EoL);
            Is(Tok.EoS);
        }

        [Test]
        public void not_postfix()
        {
            With("a /\nb /\n  c\n  d");
            Is(Tok.Identifier, 0, 1);
            Is(Tok.Binary, 1, 1);
            Is(Tok.EoL);
            Is(Tok.Identifier, 1, 1);
            Is(Tok.Binary, 1, 1);
            // Ignored EoL + Ident
            Is(Tok.Identifier, 3, 1);
            Is(Tok.EoL);
            Is(Tok.Indent, 3);
            Is(Tok.Identifier, 0, 1);
            Is(Tok.EoL);
            Is(Tok.Dedent);
            Is(Tok.EoS);
        }
    }
}



