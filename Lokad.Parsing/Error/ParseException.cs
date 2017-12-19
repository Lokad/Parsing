using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lokad.Parsing.Lexer;
using Lokad.Parsing.Parser;

namespace Lokad.Parsing.Error
{
    /// <summary>
    ///     An exception thrown by the <see cref="GrammarParser{TSelf,TTok,TResult}"/>
    ///     when it encounters a token that is not acceptable.
    /// </summary>
    /// <remarks>
    ///     The exception's message is auto-generated, but you can construct your 
    ///     own error message by using <see cref="Token"/> and <see cref="Expected"/>.
    /// </remarks>
    public sealed class ParseException : Exception
    {
        /// <summary> The token that caused an exception. </summary>
        /// <remarks> The name is generated using the parser's <see cref="ITokenNamer{TTok}"/>. </remarks>
        public string Token { get; }

        /// <summary> The list of expected tokens. </summary>
        /// <remarks> The names are generated using the parser's <see cref="ITokenNamer{TTok}"/>. </remarks>
        public IReadOnlyList<string> Expected { get; }

        /// <summary> Where the token was found. </summary>
        /// <remarks> 
        ///     In the case of a zero-length token (such as one marked by <see cref="DedentAttribute"/>) 
        ///     this location will instead have length one.
        /// </remarks>
        public SourceSpan Location { get; }

        public ParseException(string token, IReadOnlyList<string> expected, SourceSpan location) 
            : base(expected.Count == 0 
                  ? $"Syntax error, unexpected {token}." 
                  : $"Syntax error, found {token} but expected {Expect(expected)}.")
        {
            Token = token;
            Expected = expected;
            Location = location;

            // Always select at least one character.
            if (location.Length == 0) Location = new SourceSpan(location.Location, 1);
        }

        /// <summary> Pretty-printing an expectation list. </summary>
        private static string Expect(IReadOnlyList<string> expected)
        {
            Debug.Assert(expected.Count > 0);

            if (expected.Count == 1) return expected[0];

            var sb = new StringBuilder();

            sb.Append(expected[0]);
            for (var i = 1; i < expected.Count - 1; ++i)
            {
                sb.Append(", ");
                sb.Append(expected[i]);
            }
            sb.Append(" or ");
            sb.Append(expected[expected.Count - 1]);

            return sb.ToString();
        }
    }
}
