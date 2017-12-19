using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Step = Lokad.Parsing.Parser.RuleStep;

namespace Lokad.Parsing.Parser
{
    /// <summary> A set of rules obtained by reflection from a type. </summary>
    /// <remarks> Includes, for each possible ranked type, all methods that return it. </remarks>
    public sealed class RuleSet
    {
        /// <summary> The total number of entities in this rule set. </summary>
        public int EntityCount => TokenNames.Count + Rules.Count;        

        /// <summary> The maximum rule rank for each type (will be 0 for most). </summary>
        public readonly IReadOnlyDictionary<Type, int> MaxRank;

        /// <summary> The identifiers of the initial allowed rules. </summary>
        public readonly IReadOnlyList<int> InitialRules;

        /// <summary> The integer identifier for each ranked type. </summary>
        public readonly IReadOnlyDictionary<RankedType, int> RankedTypeId;

        /// <summary> The rule identifiers for each ranked type. </summary>
        /// <remarks> Indexed by <see cref="RankedTypeId"/></remarks>
        public readonly IReadOnlyList<IReadOnlyList<int>> RuleId;

        /// <summary> The inverse mapping of <see cref="RankedTypeId"/>. </summary>
        public readonly IReadOnlyList<RankedType> RankedTypes;

        /// <summary> The individual rules.  </summary>
        /// <remarks> 
        /// Indexed by <see cref="RuleId"/> minus the length of <see cref="TokenNames"/>. 
        /// In other words, if there are 42 tokens, then rules start their numbering at 
        /// 42 (tokens are numbered 0 to 41) and rule 52 is found at index 10 in <see cref="Rules"/>.
        /// Use <see cref="GetRule"/> instead to avoid all the hassle of indexing.
        /// </remarks>
        public readonly IReadOnlyList<Rule> Rules;

        /// <summary> Retrieves a rule by its identifier. </summary>
        public Rule GetRule(int i) => Rules[i - TokenNames.Count];

        /// <summary> The names of tokens. </summary>
        public readonly IReadOnlyList<string> TokenNames;

        /// <summary> The type of tokens. </summary>
        public readonly Type TokenType;

        /// <summary> The value of the end-of-stream token. </summary>
        public readonly int EndOfStream;

        /// <summary> A rule before compilation. </summary>
        /// <remarks> List rules are not included, they are generated separately. </remarks>
        private struct UncompiledRule
        {
            /// <summary> All the choices, if there's an option. </summary>
            public readonly int Choices;

            /// <summary> Context tag... </summary>
            public readonly int? Context;

            /// <summary> The method called to reduce this rule. </summary>
            public readonly MethodInfo Method;

            public UncompiledRule(MethodInfo method, int choices, int? context)
            {
                Method = method;
                Choices = choices;
                Context = context;
            }
        }

        /// <summary> The public children of each token. </summary>
        public readonly IReadOnlyDictionary<int, IReadOnlyList<int>> PublicChildren;

        /// <summary> Extract the reflection rules from type <paramref name="t"/>. </summary>
        public RuleSet(Type t, Type token, Type result, int end, IReadOnlyDictionary<int, IReadOnlyList<int>> publicChildren)
        {
            PublicChildren = publicChildren;

            // Extract token names
            TokenNames = token.GetEnumNames();
            TokenType = token;
            EndOfStream = end;

            var maxRank = new Dictionary<Type, int>();
            var rankedTypeId = new Dictionary<RankedType, int>();
            var rankedTypes = new List<RankedType>();
            var ruleId = new List<List<int>>();
            var uncompiledRules = new List<UncompiledRule>();

            // Explore methods to find those to be compiled
            foreach (var m in t.GetMethods())
            {
                var ruleAttribute = m.GetCustomAttribute<RuleAttribute>();
                if (ruleAttribute == null) continue;

                // Update the max-rank for this type
                if (!maxRank.TryGetValue(m.ReturnType, out var tMaxRank) || tMaxRank < ruleAttribute.Rank)
                    maxRank[m.ReturnType] = ruleAttribute.Rank;

                // Get or create the RankedType and its identifier
                var rt = new RankedType(m.ReturnType, ruleAttribute.Rank);
                
                if (!rankedTypeId.TryGetValue(rt, out var rtid))
                {
                    rankedTypes.Add(rt);
                    ruleId.Add(new List<int>());
                    rankedTypeId.Add(rt, rtid = rankedTypeId.Count);
                }

                // Determine the number of optional parameters, to generate one
                // rule for each parameter combination. This includes both optional
                // terminals AND lists which allow zero elements.
                var options = m.GetParameters()
                    .Count(p => 
                        (p.GetCustomAttribute<TerminalAttribute>()?.Optional ?? false) ||
                        (p.GetCustomAttribute<NonTerminalAttribute>()?.Optional ?? false) ||
                        (p.GetCustomAttribute<ListAttribute>()?.Min ?? 1) == 0);

                var combinations = 1 << options;

                var context = m.GetCustomAttribute<ContextAttribute>();

                // Allocate news rule (not compiled yet) and bind to RankedType
                for (var i = 0; i < combinations; ++i)
                {
                    var rid = uncompiledRules.Count + TokenNames.Count;
                    uncompiledRules.Add(new UncompiledRule(m, i, context?.GetTag(m.ReturnType)));
                    ruleId[rtid].Add(rid);
                }
            }

            MaxRank = maxRank;
            RankedTypeId = rankedTypeId;
            RankedTypes = rankedTypes;
            RuleId = ruleId;

            // All RankedTypes have been identified. Now explore method parameters
            // to deduce actual rules, and generate any 'list' (star, actually) rules
            // on-the-fly.

            var listRules = new List<Rule>();
            var nextListRule = uncompiledRules.Count + TokenNames.Count;
            var rules = uncompiledRules.Select(r => Compile(r, ref nextListRule, listRules)).ToArray();
            Rules = rules.Concat(listRules).ToArray();

            // The starting tokens for each rule

            var isStartOf = Enumerable.Range(TokenNames.Count, Rules.Count).Select(i =>
                Rules.Select((r, j) => r.StartsWithRule(i) ? j : -1)
                    .Where(j => j >= 0).ToArray()).ToArray();

            void PropagateStart(int stok, int rul)
            {
                foreach (var rul2 in isStartOf[rul])
                    if (!Rules[rul2].StartingTokens.Contains(stok))
                    {
                        Rules[rul2].StartingTokens.Add(stok);
                        PropagateStart(stok, rul2);
                    }
            }

            for (var r = 0; r < Rules.Count; ++r)
                foreach (var tok in Rules[r].StartingTokens)
                    PropagateStart(tok, r);

            // The reducing tokens for each rule

            foreach (var r in Rules)
            {                
                r.AddReducingToken(this, EndOfStream);
                r.ExtractEndingTokens(this);
            }
            // Initial rules

            if (!MaxRank.TryGetValue(result, out var resultMaxRank))
                throw new Exception($"Unknown result type {result}.");

            InitialRules = Enumerable.Range(0, resultMaxRank + 1)
                .SelectMany(i => RankedTypeId.TryGetValue(new RankedType(result, i), out var id) ? RuleId[id] : new int[0])
                .ToArray();
        }

        private Rule Compile(UncompiledRule uncompiledRule, ref int nextListRule, List<Rule> listRules)
        {
            var option = uncompiledRule.Choices;
            var m = uncompiledRule.Method;

            var steps = new List<Step>();
            var args = new List<bool>();
            foreach (var p in m.GetParameters())
            {
                var tag = p.GetCustomAttribute<ContextAttribute>()?.GetTag(p.ParameterType);

                var nonTerminal = p.GetCustomAttribute<NonTerminalAttribute>();
                if (nonTerminal != null)
                {
                    if (nonTerminal.Optional)
                    {
                        var keep = option % 2 == 1;
                        option = option/2;

                        if (!keep)
                        {
                            args.Add(false);
                            continue;
                        }
                    }

                    // Non-terminal step

                    var type = p.ParameterType;

                    if (type.IsGenericType &&
                        (type.GetGenericTypeDefinition() == typeof(Pos<>) ||
                         type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        type = type.GetGenericArguments()[0];
                    }

                    if (type.IsArray)
                        throw new Exception($"Parameter {p.Name} in {m.Name}, array type {type} only allowed for lists.");

                    if (!MaxRank.TryGetValue(type, out var typeMaxRank))
                        throw new Exception($"Parameter {p.Name} in {m.Name}, unknown type {type}.");

                    if (nonTerminal.MaxRank != null)
                        typeMaxRank = (int)nonTerminal.MaxRank;

                    var source = Enumerable.Range(0, typeMaxRank + 1)
                        .SelectMany(i => RankedTypeId.TryGetValue(new RankedType(type, i), out var id) ? RuleId[id] : new int[0])
                        .ToArray();

                    steps.Add(new Step(source, tag, false));
                    args.Add(true);
                    continue;
                }

                var terminal = p.GetCustomAttribute<TerminalAttribute>();
                if (terminal != null)
                {
                    if (terminal.Optional)
                    {
                        var keep = option%2 == 1;
                        option = option/2;

                        args.Add(keep);

                        if (!keep) continue;
                    }
                    else
                    {
                        args.Add(true);
                    }

                    var type = p.ParameterType;
                    if (type != TokenType &&
                        type != typeof (Nullable<>).MakeGenericType(TokenType) &&
                        type != typeof (string) &&
                        type != typeof (Pos<string>))
                        throw new Exception($"Parameter {p.Name} in {m.Name}, unknown terminal type {type}.");

                    // Terminal step
                    
                    steps.Add(new Step(Expand(terminal.Tokens, PublicChildren), tag, true));
                    continue;
                }

                var list = p.GetCustomAttribute<ListAttribute>();
                if (list != null)
                {
                    if (list.Min == 0)
                    {
                        // The zero-element list is handled in a separate rule, so the code
                        // below can assume that it has at least one element.
                        var keep = option % 2 == 1;
                        option = option / 2;

                        args.Add(keep);

                        if (!keep) continue;
                    }
                    else
                    {
                        args.Add(true);
                    }
                    
                    // List step

                    var type = p.ParameterType;
                    if (!type.IsArray || type.GetArrayRank() != 1)
                        throw new Exception($"Parameter {p.Name} in {m.Name}, unsupported list type {type}.");

                    type = type.GetElementType();

                    var separator = 
                          list.Separator != null
                        ? Expand(new[] {list.Separator.Value}, PublicChildren)
                        : list.Terminator != null
                        ? Expand(new[] {list.Terminator.Value}, PublicChildren)
                        : null;
            
                    if (!MaxRank.TryGetValue(type, out var typeMaxRank))
                        throw new Exception($"Parameter {p.Name} in {m.Name}, unknown type {type}.");

                    if (list.MaxRank != null)
                        typeMaxRank = (int)list.MaxRank;

                    var itemSource = Enumerable.Range(0, typeMaxRank + 1)
                        .SelectMany(i => RankedTypeId.TryGetValue(new RankedType(type, i), out var id) ? RuleId[id] : new int[0])
                        .ToArray();

                    // The list is split as (LOOP* END) with: 
                    //
                    //   LIST = LOOP LIST 
                    //        | END
                    //
                    // So there are always two rules to consider.

                    var loopKey = new TypeLoopKey(list, type);                    
                    if (!_typeLoops.TryGetValue(loopKey, out var typeLoop))
                    {
                        var end = nextListRule++;
                        var loop = nextListRule++;
                        typeLoop = new[] { end, loop };

                        listRules.Add(new Rule(
                            isListEnd: true,
                            type: type, 
                            steps: list.Terminator == null
                                ? new[] {new Step(itemSource, tag, false)}
                                : new[] {new Step(itemSource, tag, false), new Step(separator, tag, true)}
                            ));
                        
                        listRules.Add(new Rule(
                            isListEnd: false,
                            type: type, 
                            steps: separator == null
                                ? new[] {new Step(itemSource, tag, false), new Step(typeLoop, tag, false)}
                                : new[] {new Step(itemSource, tag, false), new Step(separator, tag, true), new Step(typeLoop, tag, false)}));

                        _typeLoops.Add(loopKey, typeLoop);
                    }

                    // If the list has a minimum size, we need an initial segment before starting the loop, 
                    // e.g. FULL = LOOP LOOP LOOP LIST <-- a list of min size 4 (!)

                    if (list.Min > 2)
                    {
                        var init = nextListRule++;
                        var initSteps = new List<Step>();

                        for (var i = 1; i < list.Min; ++i)
                        {
                            initSteps.Add(new Step(itemSource, tag, false));
                            if (separator != null) initSteps.Add(new Step(separator, tag, true));
                        }

                        initSteps.Add(new Step(typeLoop, tag, false));
                        listRules.Add(new Rule(isListEnd: false, type: type, steps: initSteps));

                        steps.Add(new Step(new[] {init}, tag, false));
                    }
                    else if (list.Min == 2)
                    {
                        // typeLoop[1] is the loop rule "LOOP LIST" which always 
                        // contains at least two elements. So this is an optimization
                        // to avoid creating an 'init' rule.
                        steps.Add(new Step(new [] { typeLoop[1] }, tag, false));
                    }
                    else
                    {
                        steps.Add(new Step(typeLoop, tag, false));
                    }

                    continue;
                }

                throw new Exception($"Parameter {p.Name} in {m.Name}: nothing to do.");
            }

            return new Rule(m, steps, args, uncompiledRule.Context ?? -1);
        }

        /// <summary> Dictionary of loop rules, to allow reuse in <see cref="Compile"/>. </summary>
        /// <remarks> Values are always in the order <c>new[]{ end, loop }</c>. </remarks>
        private readonly Dictionary<TypeLoopKey, IReadOnlyList<int>> _typeLoops = 
            new Dictionary<TypeLoopKey, IReadOnlyList<int>>();

        /// <see cref="_typeLoops"/>
        private struct TypeLoopKey
        {
            private readonly int? _separator;
            private readonly int? _terminator;
            private readonly Type _type;

            public TypeLoopKey(ListAttribute list, Type type)
            {
                _separator = list.Separator;
                _terminator = list.Terminator;
                _type = type;
            }

            #region Equality

            private bool Equals(TypeLoopKey other) => 
                _separator == other._separator && _terminator == other._terminator && _type == other._type;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is TypeLoopKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _separator.GetHashCode();
                    hashCode = (hashCode*397) ^ _terminator.GetHashCode();
                    hashCode = (hashCode*397) ^ _type.GetHashCode();
                    return hashCode;
                }
            }

            #endregion
        }

        /// <summary> Expand a list of tokens to also include all public descendants. </summary>
        private IReadOnlyList<int> Expand(IReadOnlyList<int> tokens, IReadOnlyDictionary<int, IReadOnlyList<int>> publicChildren)
        {
            // Optimization: early-out if this would have kept the same tokens
            if (!tokens.Any(publicChildren.ContainsKey)) return tokens;

            var stack = new Stack<int>();
            var seen = new HashSet<int>();
            var output = new List<int>();

            foreach (var t in tokens) stack.Push(t);

            while (stack.Count > 0)
            {
                var t = stack.Pop();
                if (seen.Contains(t)) continue;

                seen.Add(t);
                output.Add(t);
                
                if (publicChildren.TryGetValue(t, out var c))
                    foreach (var t2 in c)
                        stack.Push(t2);
            }

            return output;
        }
    }
}

