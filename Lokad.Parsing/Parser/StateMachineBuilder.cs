using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Parsing.Parser
{
    public sealed class StateMachineBuilder
    {
        /// <summary> The rules from which the state machine is built. </summary>
        public readonly RuleSet Rules;

        /// <summary> Each state, bound to its associated identifier. </summary>
        private readonly Dictionary<ParserState, short> _stateIds = 
            new Dictionary<ParserState, short>();

        /// <summary> The states by identifier. </summary>
        /// <remarks> State 0 does not exist. </remarks>
        /// <c> _stateIds[_states[i]] == i </c>
        private readonly List<ParserState> _states = new List<ParserState> {null};

        /// <summary> All the states not processed yet. </summary>
        private readonly Stack<int> _unprocessed = new Stack<int>();

        public StateMachineBuilder(RuleSet rules)
        {
            Rules = rules;
        }

        /// <summary> Associate a unique hash to a parser state. </summary>
        private short HashState(ParserState state)
        {
            short found;
            if (_stateIds.TryGetValue(state, out found)) return found;

            if (_states.Count >= short.MaxValue)
                throw new Exception("Maximum number of states reached.");

            _stateIds.Add(state, found = (short)_states.Count);
            _unprocessed.Push(found);
            _states.Add(state);
            _actions.Add(new short[Rules.TokenNames.Count + Rules.Rules.Count]);
            return found;
        }

        /// <summary> Compute the list of tags for all states of the parser. </summary>
        public IEnumerable<IReadOnlyList<int>> TagContexts =>
            _states.Select(st => 
                st == null ? new int[0] : TagOfState(st).ToArray());

        /// <summary>
        /// For a given parsing state, retrieve all the context tags
        /// applied to it.
        /// </summary>
        private IEnumerable<int> TagOfState(ParserState st) => st.Rules
            .Where(rp =>
                Rules.TokenNames.Count <= rp.Rule &&
                rp.Rule < Rules.Rules.Count + Rules.TokenNames.Count)
            .Select(rp =>
            {
                var rule = Rules.Rules[rp.Rule - Rules.TokenNames.Count];
                var stepCount = rule.Steps.Count;
                return (rp.Position < stepCount ? rule.Steps[rp.Position].Tag : null)
                    ?? rule.ContextTag;
            })
            .Where(t => t >= 0)
            .Distinct();

        /// <summary> For each parser state, the transition table </summary>
        /// <remarks>
        /// For each token, holds the shifted state (if greater than 0) or 
        /// minus the id of the rule to reduce. Zero means 'nothing'.
        /// 
        /// For each rule, holds the goto for reducing that rule.
        /// </remarks>
        private readonly List<short[]> _actions = new List<short[]> { null };

        public int Build()
        {
            // Construct the initial state.
            var initial = HashState(new ParserState(
                Rules.InitialRules.Select(i => new ParserState.RulePosition(i, 0))));
            
            while (_unprocessed.Count > 0)
            {
                var id = _unprocessed.Pop();
                var state = _states[id];

                var shifts = state.Rules.SelectMany(Shifts).GroupBy(kv => kv.Key)
                    .Select(g => new KeyValuePair<int, ParserState>(g.Key, new ParserState(g.Select(kv => kv.Value))));

                foreach (var shift in shifts)
                    _actions[id][shift.Key] = HashState(shift.Value);

                foreach (var r in state.Rules)
                {
                    foreach (var t in Reduces(r))
                    {
                        if (_actions[id][t] > 0)
                        {
                            //Console.WriteLine("Shift/Reduce conflict.");
                            //Console.WriteLine("Read token {0} in\n====", Rules.TokenNames[t]);
                            //Console.WriteLine(_states[id].ToString(Rules));
                            //Console.WriteLine("====\nReduce rule [{1}]{0} or shift to:\n====", Rules.GetRule(r.Rule), r.Rule);
                            //Console.WriteLine(_states[_actions[id][t]].ToString(Rules));

                            // Prefer shift to reduce, makes more sense in SLR
                            // (since reduces are context-independent, they often result
                            // in false positives).
                            continue;
                        }

                        if (_actions[id][t] < 0 && _actions[id][t] != -r.Rule)
                        {
                            //Console.WriteLine("Reduce/Reduce conflict.");
                            //Console.WriteLine("Read token {0} in\n====", Rules.TokenNames[t]);
                            //Console.WriteLine(_states[id].ToString(Rules));
                            //Console.WriteLine("====\nReduce rule [{2}]{0} or\nreduce rule [{3}]{1}", 
                            //    Rules.GetRule(r.Rule), Rules.GetRule(-_actions[id][t]),
                            //    r.Rule, -_actions[id][t]);
                        }

                        _actions[id][t] = (short)-r.Rule;
                    }
                }
            }

            return initial;
        }

        /// <summary> Retrieve the actions, in a flat layout. </summary>
        /// <remarks> 
        /// The actions for state <c>i</c> are between <c>N * (i-1)</c> and <c>N * i</c>
        /// where <c>N</c> is the <see cref="RuleSet.EntityCount"/>
        /// </remarks>
        public short[] Actions => _actions.Skip(1).SelectMany(a => a).ToArray();
        
        /// <summary> Shifts for a specific position in a rule. </summary>
        /// <remarks>
        /// The value is the result of the shift, may be in the same rule or in
        /// another rule. The key is the shift source, can be either a token 
        /// (in which case the action is a true SLR shift) or a rule (a SLR goto).
        /// </remarks>
        private IEnumerable<KeyValuePair<int, ParserState.RulePosition>> Shifts(ParserState.RulePosition rulepos)
            => AllShifts(rulepos, null);

        /// <summary> Recursive exploration for <see cref="Shifts"/>. </summary>
        private IEnumerable<KeyValuePair<int, ParserState.RulePosition>> AllShifts(ParserState.RulePosition rulepos, HashSet<int> alreadySeenRules)
        { 
            if (alreadySeenRules != null)
                if (rulepos.Position == 0 && alreadySeenRules.Contains(rulepos.Rule))
                    // This rule was already seen during the recursive exploration
                    yield break;
            
            var rule = Rules.GetRule(rulepos.Rule);
            var pos = rulepos.Position;

            // Reached the end of the rule, only reduce is allowed now
            if (rule.Steps.Count == pos) yield break;

            var step = rule.Steps[pos];

            // These values move us one step forward
            var next = new ParserState.RulePosition(rulepos.Rule, pos + 1);
            foreach (var v in step.Source)
                yield return new KeyValuePair<int, ParserState.RulePosition>(v, next);
            
            if (step.IsTerminal) yield break;

            // Non-terminals allow stepping into any of their sub-rules, so consider
            // these as well using depth-first exploration using a hashset to avoid 
            // infinite loops.

            if (alreadySeenRules == null) alreadySeenRules = new HashSet<int>();

            if (pos == 0) alreadySeenRules.Add(rulepos.Rule);

            foreach (var v in step.Source)
                foreach (var kv in AllShifts(new ParserState.RulePosition(v, 0), alreadySeenRules))
                    yield return kv;
        }

        /// <summary> All tokens that can cause this rule to reduce. </summary>
        private IEnumerable<int> Reduces(ParserState.RulePosition rulepos)
        {
            var rule = Rules.GetRule(rulepos.Rule);
            var pos = rulepos.Position;

            // Reduce only allowed at end of rule
            if (rule.Steps.Count > pos) return new int[0];

            return rule.ReducingTokens;
        }

        public string Describe()
        {
            var sb = new StringBuilder();

            for (var i = 1; i < _states.Count; ++i)
            {
                var s = _states[i];
                sb.AppendFormat("==== {0} ====\n", i);
                sb.Append(s.ToString(Rules));

                for (var t = 0; t < _actions[i].Length; ++t)
                {
                    var a = _actions[i][t];

                    if (a == 0) continue;
                    sb.AppendFormat("== On {0} ", t < Rules.TokenNames.Count 
                        ? Rules.TokenNames[t].ToUpperInvariant() 
                        : Rules.GetRule(t).ToString());

                    if (a > 0)
                    {
                        sb.AppendFormat(" shift to {0}:\n", a);
                        sb.Append(_states[a].ToString(Rules));
                    }
                    else
                    {
                        sb.AppendFormat(" reduce {0}\n", Rules.GetRule(-a));
                    }
                }
            }

            return sb.ToString();
        }
    }
}
