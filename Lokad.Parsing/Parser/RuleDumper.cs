using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Lokad.Parsing.Lexer;

namespace Lokad.Parsing.Parser
{
    /// <see cref="ToEbnfGrammar"/>
    public sealed class RuleDumper
    {
        /// <summary> The names of tokens. </summary>
        private readonly IReadOnlyList<string> _tokenNames;

        /// <summary> Hide these type prefixes. </summary>
        /// <remarks> Sorted by decreasing prefix length. </remarks>
        private readonly IReadOnlyList<string> _typePrefixes;

        /// <summary> Gathering of grammar rules </summary>
        private readonly Dictionary<string, List<string>> _productions;

        private string EnumFieldInfo(FieldInfo finfo)
        {
            var key = $"'{finfo.Name.ToLower()}'";
            var pattCi = finfo.GetCustomAttribute<PatternCiAttribute>();

            if (pattCi != null)
            {
                var patLst = new List<string> { "/" + pattCi.Pattern + "/"};
                _productions.Add(key, patLst);
                return key;
            }

            var anyCi = finfo.GetCustomAttribute<AnyAttribute>();

            if (anyCi == null) return $"'{finfo.Name.ToLower()}'";

            if (anyCi.Options.Count == 1)
                return $"'{anyCi.Options[0]}'";

            var lst = anyCi.Options.Select(v => $"'{v}'").ToList();
            _productions.Add(key, lst);
            return key;
        }

        private RuleDumper(Type t, Type token, IEnumerable<string> typePrefixes)
        {
            _productions = new Dictionary<string, List<string>>();
            _tokenNames = token.GetFields().Select(EnumFieldInfo).Skip(1).ToArray();
            _typePrefixes = typePrefixes.OrderByDescending(prefix => prefix.Length).ToArray();

            foreach (var m in t.GetMethods())
            {
                var ruleAttribute = m.GetCustomAttribute<RuleAttribute>();
                if (ruleAttribute == null) continue;
                ProcessMethod(m);
            }
        }

        /// <summary> Dumps a reflection-based grammar in Extended Backus-Naur Form. </summary>
        /// <param name="grammar">
        ///     The grammar itself, as passed to the first type argument of 
        ///     <see cref="GrammarParser{TSelf,TTok,TResult}"/>.
        /// </param>
        /// <param name="tok">
        ///     The token <code>enum</code>, as passed to the second type argument of 
        ///     <see cref="GrammarParser{TSelf,TTok,TResult}"/>.
        /// </param>
        /// <param name="typePrefix">
        ///     When naming types (for textual display), any prefix in this list should
        ///     be removed first. For instance, if displaying <code>System.Text.Encoding</code>
        ///     and prefix <code>"System.Text"</code> is provided in this argument, then the displayed
        ///     type name would be <code>"Encoding"</code>.
        /// </param>
        public static string ToEbnfGrammar(
            Type grammar,
            Type tok,
            IReadOnlyList<string> typePrefix)
        {
            var dumper = new RuleDumper(grammar, tok, typePrefix);
            return dumper.AssembleGrammar();
        }

        private string AssembleGrammar()
        {
            var sb = new StringBuilder();

            foreach (var production in _productions)
            {
                sb.AppendLine($"{ToDisplayName(production.Key)} ::=");

                foreach (var p in production.Value)
                    sb.AppendLine($"  | {p}");

                sb.AppendLine("");
            }

            return sb.ToString();
        }

        static readonly Regex InnerClassRegex = new Regex(".*\\+(.*)");

        private string ToDisplayName(Type t) => ToDisplayName(t.ToString());

        private string ToDisplayName(string str)
        {
            var innerMatch = InnerClassRegex.Match(str);
            if (innerMatch.Success)
                return innerMatch.Groups[1].Value;

            foreach (var pref in _typePrefixes)
            {
                if (str.StartsWith(pref))
                    return str.Substring(pref.Length);
            }

            return str;
        }

        private void ProcessMethod(MethodInfo m)
        {
            var prod = "";
            foreach (var p in m.GetParameters())
            {
                var nta = p.GetCustomAttribute<NonTerminalAttribute>();
                if (nta != null)
                {
                    var pType = p.ParameterType;
                    if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        pType = pType.GetGenericArguments()[0];

                    if (nta.Optional)
                        prod += $" [{ToDisplayName(pType)}]";
                    else
                        prod += " " + ToDisplayName(pType);
                }

                var ta = p.GetCustomAttribute<TerminalAttribute>();
                if (ta != null)
                {
                    if (ta.Tokens.Count == 1)
                    {
                        if (ta.Optional)
                            prod += " [" + _tokenNames[ta.Tokens[0]] + "]";
                        else
                            prod += " " + _tokenNames[ta.Tokens[0]];
                    }
                    else
                    {
                        var tokens = string.Join(" | ", ta.Tokens.Select(ix => _tokenNames[ix]));
                        if (ta.Optional)
                            prod += " [" + tokens + "]";
                        else
                            prod += " (" + tokens + ")";
                    }
                }

                var la = p.GetCustomAttribute<ListAttribute>();
                if (la != null)
                {
                    var sep = la.Separator.HasValue ? _tokenNames[la.Separator.Value] : "";
                    var pType = p.ParameterType;

                    if (pType.IsArray) pType = pType.GetElementType();

                    var tStr = ToDisplayName(pType);

                    if (la.Min > 0)
                        prod += $" {tStr} {{{sep} {tStr}}}";
                    else
                        prod += $" [{tStr} {{{sep} {tStr}}}]";

                }
            }

            var productionKey = m.ReturnType.ToString();

            if (!_productions.TryGetValue(productionKey, out var prods))
            {
                prods = new List<string>();
                _productions.Add(productionKey, prods);
            }

            prods.Add(prod);

        }
    }
}
