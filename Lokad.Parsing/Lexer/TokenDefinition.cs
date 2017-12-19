using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lokad.Parsing.Parser;

namespace Lokad.Parsing.Lexer
{
    
    /// <summary> Describes how a token is recognized from the source. </summary>
    /// <remarks>
    ///     In theory, you could create instances of this class yourself and feed
    ///     them to a <see cref="TokenReader{TTok}"/> as configuration. If you have
    ///     a very specific use case that warrants such an extreme approach, feel
    ///     free to do so ; this class is public.
    /// 
    ///     In practice, the recommended approach is to define a token <c>enum</c>,
    ///     then use it as a generic argument to <see cref="ReflectionTokenReader{T}"/>,
    ///     which will extract all <see cref="TokenDefinition"/> instances from the 
    ///     attributes placed on the members of that enumeration.
    /// 
    ///     The <see cref="GrammarParser{TSelf,TTok,TResult}"/> will use the latter
    ///     approach by default, so when using a tokenizer-parser combination you
    ///     only need to define the token <c>enum</c> and the grammar itself.
    /// </remarks>
    public sealed class TokenDefinition
    {
        // Current implementation relies on regular expressions. We could likely do 
        // much better, both in terms of allocation and in terms of pure speed of
        // traversal, if we implemented a custom recognition engine. However, 
        // tokenization is currently only a very small portion of script compilation
        // (about 2ms lex + parse on a typical script), so optimization efforts are
        // best spent elsewhere.
        
        /// <summary> A token recognized as a regular expression. </summary>
        internal TokenDefinition(Regex regularExpression, int? maximumLength = null, string startsWith = null)
        {
            RegularExpression = regularExpression;
            MaximumLength = maximumLength ?? int.MaxValue;
            _startsWith = startsWith;
        }

        /// <summary> A token recognized as one of many strings. </summary>
        public TokenDefinition(IReadOnlyList<string> strings, bool caseSensitive = true)
        {
            var flags = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline;
            if (!caseSensitive) flags = flags | RegexOptions.IgnoreCase;

            // Order strings by descending length, because C# regular expressions match "a|b" from
            // left to right rather than the longest match.

            var pattern = string.Join("|", strings.OrderByDescending(s => s.Length).Select(Regex.Escape));

            RegularExpression = new Regex("\\G(" + pattern + ")", flags);
            MaximumLength = strings.Select(s => s.Length).Max();

            var chars = new HashSet<char>(strings.Select(s => s.ToLowerInvariant()[0]));
            chars.UnionWith(strings.Select(s => s.ToUpperInvariant()[0]));

            _startsWith = new string(chars.ToArray());
        }

        /// <summary> Attempt to match the buffer contents, starting at the specified position. </summary>
        /// <returns> The length of the match, 0 if no match. </returns>
        public int MatchLength(string buffer, int start)
        {
            var match = RegularExpression.Match(buffer, start);
            return match.Success && match.Index == start ? match.Length : 0;
        }

        /// <summary> The maximum length of a token matched by this definition. </summary> 
        /// <remarks> 
        ///     When a matching sub-tokens (<see cref="FromAttribute"/>) against the text matched 
        ///     by their parent token, the match is a full-string match instead of a prefix 
        ///     match. By knowing the maximum length that can be matched by this definition, we
        ///     can skip the match attempt altogether if it is too short.
        /// </remarks>       
        public int MaximumLength { get; }
        
        /// <summary> The regular expression that recognizes this token. </summary>
        private Regex RegularExpression { get; }
        
        /// <see cref="StartsWith"/>
        /// <remarks> If not provided, assume that it can start with any character. </remarks>
        private readonly string _startsWith;

        /// <summary> True if the match can start with this character. </summary>
        public bool StartsWith(char c) => _startsWith == null || _startsWith.IndexOf(c) != -1;        

        public override string ToString() => RegularExpression.ToString();
    }
}
