using System.Collections.Generic;
using Lokad.Parsing.Parser;

namespace Lokad.Parsing.Lexer
{
    /// <summary> The result of tokenizing an input buffer. </summary>
    public sealed class LexerResult<TTok>
    {
        /// <summary> The buffer from which this result was obtained. </summary>
        /// <remarks>
        ///     This is the string passed to <see cref="TokenReader{TTok}.ReadAllTokens"/>.
        /// </remarks>
        public string Buffer { get; }

        /// <summary> Does this buffer contain invalid tokens ? </summary>
        /// <see cref="ErrorAttribute"/>
        public bool HasInvalidTokens { get; }

        /// <summary> The tokens extracted from the buffer. </summary>
        public IReadOnlyList<LexerToken<TTok>> Tokens { get; }

        /// <summary> The position of all newline characters in the buffer, in ascending order. </summary>
        /// <see cref="LineOfPosition"/>
        public IReadOnlyList<int> Newlines { get; }

        /// <summary> Total number of tokens. </summary>
        public int Count => Tokens.Count;

        /// <summary> Get the string at the specified token position. </summary>
        public string GetString(int pos)
        {
            var tok = Tokens[pos];
            if (tok.Length == 0) return string.Empty;
            return Buffer.Substring(tok.Start, tok.Length);
        }

        /// <summary> Get the string and source-span at the specified token position. </summary>
        public Pos<string> GetStringPos(int pos)
        {
            var tok = Tokens[pos];

            LineOfPosition(tok.Start, out var line, out var column);

            var span = new SourceSpan(new SourceLocation(tok.Start, line, column), tok.Length);
            var str = Buffer.Substring(tok.Start, tok.Length);

            return new Pos<string>(str, span);
        }
        
        public LexerResult(
            string buffer, 
            IReadOnlyList<LexerToken<TTok>> tokens, 
            IReadOnlyList<int> newlines, 
            bool hasInvalidTokens)
        {
            Buffer = buffer;
            Tokens = tokens;
            Newlines = newlines;
            HasInvalidTokens = hasInvalidTokens;
        }

        /// <summary> Return the line and column on which a certain position appears. </summary>
        /// <remarks> 
        ///     This library assumes that the line and column are purely human concepts,
        ///     and therefore should be one-based instead of zero-based. The first line is 
        ///     line 1, the leftmost column is column 1.
        /// 
        ///     A newline character is the last character of its line (rather than the first character of
        ///     the next line).
        /// </remarks>
        public void LineOfPosition(int position, out int line, out int column)
        {
            var a = 0;
            var b = Newlines.Count;

            if (b == 0 || position <= Newlines[0])
            {
                line = 1; // +1 for one-based
                column = position + 1; // +1 for one-based
                return;
            }

            var last = Newlines[b - 1];
            if (position > last)
            {
                line = b + 1; // +1 for one-based
                column = position - last;
                return;
            }
            
            while (a + 1 < b)
            {
                var m = (a + b)/2;
                var v = Newlines[m];
                if (position <= v) b = m;
                else a = m;
            }

            // 'a' is the index of the last newline that is strictly before the specified
            // position, meaning the actual line is 'a + 2' (e.g. the line after 
            // newline 10 is line 12).                        
            line = a + 2; 
            column = position - Newlines[a];
        } 
    }
}