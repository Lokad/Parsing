using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Reads tokens from an input buffer. </summary>
    public class TokenReader<TTok> where TTok : struct
    {
        /// <summary> The rules used to match tokens. </summary>
        public readonly IReadOnlyList<LexerRule<TTok>> Rules;

        /// <summary> The token issued when a character cannot be parsed. </summary>
        public readonly TTok Error;

        /// <summary> The last token in the stream. </summary>
        public readonly TTok EndOfStream;

        /// <summary> The indent token. </summary>
        /// <remarks> Equal to <see cref="EndOfStream"/> if indents not being tracked. </remarks>
        public readonly TTok Indent;

        /// <summary> The dedent token. </summary>
        /// <remarks> Equal to <see cref="EndOfStream"/> if indents not being tracked. </remarks>
        public readonly TTok Dedent;

        /// <summary> The end-of-line token. </summary>
        ///         /// <remarks> Equal to <see cref="EndOfStream"/> if newlines are not being tracked. </remarks>
        public readonly TTok EndOfLine;

        /// <summary> If tracking indents, the stack of indent lengths. </summary>
        /// <remarks> NULL if indents not being tracked. </remarks>
        private readonly Stack<int> _indents;

        /// <summary> Should newlines be escaped ? </summary>
        public readonly bool EscapeNewlines;

        /// <summary> Comment recognition, null if not recognized. </summary>
        public readonly TokenDefinition Comments;

        /// <summary> For each token, the "public" tokens that it can appear as. </summary>
        public readonly IReadOnlyDictionary<TTok, IReadOnlyList<TTok>> PublicChildren;

        private static bool IsSkippable(char c)
        {
            return c == ' ' || c == '\t' || c == '\r';
        }

        /// <summary> Reads all tokens in the buffer. </summary>
        /// <param name="buffer">Code to tokenize</param>
        /// <param name="isInputTruncated">
        /// If true, don't insert end of line and dedent and EoS token at the end of stream
        /// </param>
        public LexerResult<TTok> ReadAllTokens(string buffer, bool isInputTruncated = false)
        {
            _indents?.Clear();
            _indents?.Push(0);
            
            var hasErrors = false;

            // The last 'normal' token we emitted cannot appear at end of line.
            var lastCannotBePostfix = false;

            // The position of the last seen backslash. 
            int? backslash = null;

            var output = new List<LexerToken<TTok>>();
            var rules = Rules.Count;

            var bufLength = buffer.Length;
            
            // Skip backwards from end until we reach an interesting character.
            while (bufLength > 0 && IsSkippable(buffer[bufLength - 1])) --bufLength;

            var start = 0;
            while (start < bufLength)
            {
                var first = buffer[start];
                
                // Ignore non-newline whitespace.

                if (IsSkippable(first))
                {
                    ++start;
                    continue;
                }
                
                // Ignore newlines.
                // If indentation lexing is enabled, produce the indent and dedent
                // tokens as required by the spaces found after the newlines.

                if (first == '\n')
                {
                    if (backslash != null)
                    {
                        // Escaped newline ! 
                        backslash = null;
                        ++start;
                        continue;
                    }

                    OnNewline(buffer, bufLength, ref start, output);

                    if (lastCannotBePostfix)
                        RemoveLastIndent(output);

                    continue;
                }

                // Comment detection does not follow the same 'longest match' rule
                // as other tokens: every time the comment pattern matches the 
                // buffer, it's a comment.

                if (Comments != null)
                {
                    var length = Comments.MatchLength(buffer, start);
                    if (length > 0)
                    {
                        start += length;
                        continue;
                    }
                }

                if (backslash != null)
                {
                    // If we're keeping track of a backslash, drop it now, because clearly
                    // we won't reach a newline without an intervening non-comment, non-whitespace
                    // section. This consists in backtracking back to the backslash position
                    // and not treating it in a special fashion at all.
                    start = (int) backslash;
                    backslash = null;
                }
                else if (first == '\\' && EscapeNewlines)
                {
                    // We have encountered a backslash, we are escaping new-lines, and this is
                    // NOT a backtrack where the backslash would have to be treated as a normal
                    // character: keep the backslash around and skip forward.
                    backslash = start++;
                    continue;
                }

                // We've determined that what follows must be a token, so find the
                // definition that matches the longest token.

                var bestLength = 0;
                var bestRule = 0;
                
                for (var i = 0; i < rules; ++i)
                {
                    var def = Rules[i].Definition;
                    if (def.MaximumLength <= bestLength) continue;
                    if (!def.StartsWith(first)) continue;
                    var length = def.MatchLength(buffer, start);

                    if (length <= bestLength) continue;
                    bestLength = length;
                    bestRule = i;
                }

                TTok token;
                
                if (bestLength == 0)
                {
                    token = Error;
                    bestLength = 1;
                    hasErrors = true;

                    lastCannotBePostfix = false;
                }
                else
                {
                    var rule = Rules[bestRule];
                    token = rule.Token;

                    // Attempt to match sub-tokens, if there are any
                    while (rule.SubTokens != null)
                    {
                        var found = false;

                        foreach (var r in rule.SubTokens)
                        {
                            var def = r.Definition;
                            if (def.MaximumLength < bestLength) continue;
                            if (!def.StartsWith(first)) continue;
                            if (def.MatchLength(buffer, start) != bestLength) continue;

                            found = true;
                            token = r.Token;
                            rule = r;
                        }

                        if (!found) break;
                    }

                    if (!rule.CanBePrefix)
                        RemoveLastIndent(output);

                    lastCannotBePostfix = !rule.CanBePostfix;
                }
                
                output.Add(new LexerToken<TTok>(token, start, bestLength));

                start += bestLength;
            }
            
            // Before the end-of-stream, generate any required dedents and a 
            // final newline
            if (output.Count > 0 && !isInputTruncated)
            {
                if (!Equals(EndOfLine, EndOfStream) )
                    if (!Equals(output[output.Count - 1].Token, EndOfLine) &&
                        !Equals(output[output.Count - 1].Token, Dedent))
                        output.Add(new LexerToken<TTok>(EndOfLine, start, 0));

                if (_indents != null && _indents.Count > 1)
                    while (_indents.Count > 1)
                    {
                        output.Add(new LexerToken<TTok>(Dedent, start, 0));
                        _indents.Pop();
                    }
            }
            
            if (!isInputTruncated)
                output.Add(new LexerToken<TTok>(EndOfStream, start, 0));

            // Extract the newline positions
            // We do this here rather than above, because tokens are allowed
            // to consume newlines on their own.

            var newlines = new List<int>();
            var pos = 0;

            while (pos < buffer.Length)
            {
                var newline = buffer.IndexOf('\n', pos);
                if (newline == -1) break;

                newlines.Add(newline);
                pos = newline + 1;
            }

            return new LexerResult<TTok>(buffer, output, newlines, hasErrors);
        }

        /// <summary>
        ///     If the last two tokens are an end-of-line and an indent (in this order), 
        ///     remove them.
        /// </summary>
        /// <remarks>
        ///     If removed, the indent will also be forgotten from <see cref="_indents"/>.
        /// </remarks>
        private void RemoveLastIndent(List<LexerToken<TTok>> output)
        {
            var n = output.Count;
            if (_indents == null || n < 2) return;
            if (!output[n - 1].Token.Equals(Indent) || !output[n - 2].Token.Equals(EndOfLine)) return;

            _indents.Pop();
            output.RemoveRange(n - 2, 2);
        }

        /// <summary> Process a non-escaped newline. </summary>
        private void OnNewline(string buffer, int length, ref int start, List<LexerToken<TTok>> output)
        {
            if (!Equals(EndOfLine, EndOfStream) && output.Count > 0 && 
                !Equals(output[output.Count - 1].Token, Indent) && 
                !Equals(output[output.Count - 1].Token, Dedent))
                // Never start the token chain with newlines, 
                // never add a newline after an indent or dedent.
                output.Add(new LexerToken<TTok>(EndOfLine, start, 0));

            ++start;

            if (_indents == null) return;

            var indentLength = 0;
            while (start < length)
            {
                var first = buffer[start];

                if (first == '\n')
                    indentLength = 0;
                else if (first == ' ')
                    indentLength++;
                else if (first == '\t')
                    indentLength += 2;
                else if (first != '\r')
                {
                    // Reached a non-space, non-newline character : in theory, we need to compute
                    // our indent now. BUT ! If this is a comment, then we skip the line.

                    if (Comments == null || !Comments.StartsWith(first))
                        break;

                    var comment = Comments.MatchLength(buffer, start);
                    if (comment == 0) break;

                    start += comment;
                    continue;
                }

                ++start;
            }

            while (_indents.Peek() > indentLength)
            {
                _indents.Pop();
                output.Add(new LexerToken<TTok>(Dedent, start, 0));
            }

            if (_indents.Peek() < indentLength)
            {
                _indents.Push(indentLength);
                output.Add(new LexerToken<TTok>(Indent, start, 0));
            }
        }

        /// <summary> Create a token reader for a specified set of rules. </summary>
        /// <remarks> 
        /// Rules are applied in order. Longest match is kept, first match is kept
        /// if two matches are of equal length. The rule set should be designed so 
        /// that at least one 
        /// </remarks>
        public TokenReader(
            IEnumerable<LexerRule<TTok>> rules, 
            TTok error, 
            TTok endOfStream, 
            TTok? indent = null, 
            TTok? dedent = null,
            TTok? newline = null,
            bool escapeNewlines = false,
            TokenDefinition comments = null)
        {
            Rules = rules.ToArray();
            Error = error;
            EndOfStream = endOfStream;
            Indent = indent ?? endOfStream;
            Dedent = dedent ?? endOfStream;
            EndOfLine = newline ?? endOfStream;

            if (indent != null || dedent != null)
            {
                _indents = new Stack<int>();

                if (indent == null)
                    throw new ArgumentNullException(nameof(indent));

                if (dedent == null)
                    throw new ArgumentException(nameof(dedent));
            }

            EscapeNewlines = escapeNewlines;
            Comments = comments;

            var publicChildren = new Dictionary<TTok, IReadOnlyList<TTok>>();
            foreach (var r in Rules)
                GetPublicChildren(r, publicChildren);

            PublicChildren = publicChildren;
        }

        /// <summary> Extract the public children of each token. </summary>
        private void GetPublicChildren(LexerRule<TTok> lexerRule, Dictionary<TTok, IReadOnlyList<TTok>> publicChildren)
        {
            var tok = lexerRule.Token;
            if (lexerRule.SubTokens == null)
            {
                publicChildren.Add(tok, new TTok[0]);
                return;
            }

            publicChildren.Add(tok, lexerRule.SubTokens.Where(s =>
            {
                GetPublicChildren(s, publicChildren);
                return s.IsPublicChild;
            }).Select(s => s.Token).ToArray());
        }
    }
}
