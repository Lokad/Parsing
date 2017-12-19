using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Lokad.Parsing.Lexer
{
    /// <summary>
    ///     A <see cref="TokenReader{TTok}"/> that infers its rules and other information
    ///     from the <see cref="TokensAttribute"/> on its type parameter.
    /// </summary>
    public sealed class ReflectionTokenReader<TTok> : TokenReader<TTok> where TTok : struct
    {
        public ReflectionTokenReader() : base(
            StaticRules, 
            StaticError, 
            StaticEnd, 
            StaticIndent, 
            StaticDedent, 
            StaticEndOfLine, 
            StaticEscapeNewlines, 
            StaticComments)
        {
        }

        /// <summary>
        ///     Initializes all static readonly members of this class from the attributes
        ///     on the <typeparamref name="TTok"/> enumeration.
        /// </summary>
        static ReflectionTokenReader()
        {
            var t = typeof (TTok);
            if (!t.IsEnum)
                throw new ArgumentException($"Type {t} is not an enum.", nameof(TTok));

            // The TokensAttribute itself

            var tokensAttribute = t.GetCustomAttribute<TokensAttribute>();
            if (tokensAttribute == null)
                throw new ArgumentException($"Enum {t} does not carry {nameof(TokensAttribute)}.", nameof(TTok));

            var commentStart = tokensAttribute.Comments[0];
            var commentStartsWith = "[]\\(.<".IndexOf(commentStart) > -1 ? null : new string(commentStart, 1);

            var csComments = tokensAttribute.Comments.StartsWith("\\G")
                ? tokensAttribute.Comments
                : $"\\G({tokensAttribute.Comments})";
            
            StaticComments = new TokenDefinition(new Regex(csComments), startsWith: commentStartsWith);
            StaticEscapeNewlines = tokensAttribute.EscapeNewlines;

            // The enumeration contents
            var names  = t.GetEnumNames();
            var values = t.GetEnumValues();

            var pairs = names.Select((n, i) => new KeyValuePair<string, TTok>(n, (TTok)values.GetValue(i))).ToArray();

            // Detect 'end', 'indent', 'dedent' and 'error', and extract the structure and
            // definition for the other rules.

            var infix = new Dictionary<TTok, InfixAttribute>();
            var definitions = new Dictionary<TTok, TokenDefinition>();
            var parent = new Dictionary<TTok, TTok>();
            var publicChild = new HashSet<TTok>();

            foreach (var kv in pairs)
            {
                var name = kv.Key;
                var tok = kv.Value;
                var mbr = t.GetMember(name)[0];

                var endAttribute = mbr.GetCustomAttribute<EndAttribute>();
                if (endAttribute != null)
                {
                    StaticEnd = tok;
                    continue;
                }

                var errorAttribute = mbr.GetCustomAttribute<ErrorAttribute>();
                if (errorAttribute != null)
                {
                    StaticError = tok;
                    continue;
                }

                var indentAttribute = mbr.GetCustomAttribute<IndentAttribute>();
                if (indentAttribute != null)
                {
                    StaticIndent = tok;
                    continue;
                }

                var endOfLineAttribute = mbr.GetCustomAttribute<EndOfLineAttribute>();
                if (endOfLineAttribute != null)
                {
                    StaticEndOfLine = tok;
                    continue;
                }

                var dedentAttribute = mbr.GetCustomAttribute<DedentAttribute>();
                if (dedentAttribute != null)
                {
                    StaticDedent = tok;
                    continue;
                }

                var infixAttribute = mbr.GetCustomAttribute<InfixAttribute>();
                if (infixAttribute != null)
                    infix.Add(tok, infixAttribute);

                var fromAttribute = mbr.GetCustomAttribute<FromAttribute>();
                if (fromAttribute != null)
                {
                    parent.Add(tok, (TTok)(object)fromAttribute.Parent);
                    if (!fromAttribute.IsPrivate) publicChild.Add(tok);
                    // No continue: still need to determine definition
                }

                var patternAttribute = mbr.GetCustomAttribute<PatternAttribute>();
                if (patternAttribute != null)
                {
                    definitions.Add(tok, patternAttribute.ToDefinition());
                    continue;
                }

                var anyAttribute = mbr.GetCustomAttribute<AnyAttribute>();
                if (anyAttribute != null)
                {
                    definitions.Add(tok, anyAttribute.ToDefinition());
                    continue;
                }

                var ciAttribute = mbr.GetCustomAttribute<CiAttribute>();
                if (ciAttribute != null)
                {
                    definitions.Add(tok, new AnyAttribute(name) {CaseSensitive = false}.ToDefinition());
                }
            }

            // Construct the actual rules. This algorithm is NOT optimal, but most
            // languages have a small enough number of tokens that this des not matter.

            var seen = new HashSet<TTok>();
            StaticRules = definitions
                .Where(kv => !parent.ContainsKey(kv.Key))
                .Select(kv => new LexerRule<TTok>(
                    kv.Value, 
                    kv.Key, 
                    publicChild.Contains(kv.Key), 
                    infix.TryGetValue(kv.Key, out var inf) ? inf : null,
                    SubRules(kv.Key, definitions, infix, parent, publicChild, seen)))
                .ToArray();            
        }

        /// <summary>
        /// From a set of definitions and parent links, construct the lexer rules for all
        /// children of a specified parent.
        /// </summary>
        private static IReadOnlyList<LexerRule<TTok>> SubRules(
            TTok parent,
            IReadOnlyDictionary<TTok, TokenDefinition> defs, 
            IReadOnlyDictionary<TTok, InfixAttribute> infix,
            IReadOnlyDictionary<TTok, TTok> parents,
            ICollection<TTok> publicChildren,
            ISet<TTok> seen)
        {
            if (seen.Contains(parent))
                throw new ArgumentException($"Cycle found, involving {parent}.", nameof(parent));

            seen.Add(parent);

            return parents
                .Where(kv => Equals(kv.Value, parent))
                .Select(kv => new LexerRule<TTok>(
                    defs[kv.Key], 
                    kv.Key, 
                    publicChildren.Contains(kv.Key),
                    infix.TryGetValue(kv.Key, out var inf) ? inf : null,
                    SubRules(kv.Key, defs, infix, parents, publicChildren, seen)))
                .ToArray();
        } 

        // ReSharper disable StaticMemberInGenericType

        /// <see cref="TokenReader{TTok}.Comments"/>
        private static readonly TokenDefinition StaticComments;

        /// <see cref="TokenReader{TTok}.Rules"/>
        private static readonly IReadOnlyList<LexerRule<TTok>> StaticRules;

        /// <see cref="TokenReader{TTok}.Error"/>
        private static readonly TTok StaticError;

        /// <see cref="TokenReader{TTok}.EndOfStream"/>
        private static readonly TTok StaticEnd;

        /// <see cref="TokenReader{TTok}.Indent"/>
        private static readonly TTok? StaticIndent;

        /// <see cref="TokenReader{TTok}.Dedent"/>
        private static readonly TTok? StaticDedent;

        /// <see cref="TokenReader{TTok}.EndOfLine"/>
        private static readonly TTok? StaticEndOfLine;

        /// <see cref="TokenReader{TTok}.EscapeNewlines"/>
        private static readonly bool StaticEscapeNewlines;

        // ReSharper restore StaticMemberInGenericType
    }
}
