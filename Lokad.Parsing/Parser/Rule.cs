using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lokad.Parsing.Parser
{
    /// <summary> An individual parsing rule. </summary>
    public sealed class Rule
    {
        /// <summary> The method to be called for this rule. </summary>
        /// <remarks> Null if a list-rule. </remarks>
        public readonly MethodInfo Method;

        /// <summary> For each method argument, whether it is provided by this rule. </summary>
        /// <remarks> 
        ///     If an argument is an optional terminal or a possibly empty list, then 
        ///     the rule parser will generate alternative rules which call the same 
        ///     method but do not expect values for those arguments. This indicates whether
        ///     the rule will provide each argument.
        /// </remarks>
        public readonly IReadOnlyList<bool> Provided; 

        /// <summary> The type of item to store. </summary>
        /// <remarks> For list rules, this is the list element type. </remarks>
        public readonly Type Type;

        /// <summary> All available steps for this rule. </summary>
        public readonly IReadOnlyList<RuleStep> Steps;

        /// <summary> Is this part of a list non-terminal ? </summary>
        public bool IsList => Method == null;

        /// <summary> Is this the "end" rule of a list non-terminal ? </summary>
        public readonly bool IsListEnd;

        /// <summary> All starting tokens for this rule. </summary>
        /// <remarks> 
        /// Initially, set to only the tokens that can be found in the
        /// starting terminals. Set to the full set of starting tokens 
        /// afterwards.
        /// </remarks>
        public readonly HashSet<int> StartingTokens = new HashSet<int>();

        public readonly int ContextTag;
        
        public Rule(MethodInfo method, IReadOnlyList<RuleStep> steps, IReadOnlyList<bool> args, int contextTag)
            : this(method.ReturnType, steps, false)
        {
            Method = method;
            Provided = args;
            ContextTag = contextTag;
        }

        public Rule(Type type, IReadOnlyList<RuleStep> steps, bool isListEnd)
        {
            Steps = steps;
            Type = type;
            IsListEnd = isListEnd;
            ContextTag = -1;

            FillStartingTokens();
        }

        /// <summary> All tokens which can be found after this rule. </summary>
        /// <remarks> Filled externally after construction. </remarks>
        public readonly HashSet<int> ReducingTokens = new HashSet<int>(); 

        /// <summary> Fills <see cref="StartingTokens"/>. </summary>
        private void FillStartingTokens()
        {
            if (Steps[0].IsTerminal)
                foreach (var t in Steps[0].Source)
                    StartingTokens.Add(t);
        }

        /// <summary> Fill the <see cref="ReducingTokens"/> of other rules involved in this one. </summary>
        public void ExtractEndingTokens(RuleSet rules)
        {
            for (var i = 0; i < Steps.Count - 1; ++i)
            {
                var step = Steps[i];
                if (step.IsTerminal) continue;

                var next = Steps[i + 1];

                if (next.IsTerminal)
                {
                    foreach (var rule in step.Source)
                        foreach (var tok in next.Source)
                            rules.GetRule(rule).AddReducingToken(rules, tok);
                }
                else
                {
                    var tokens = new HashSet<int>();
                    foreach (var rule in next.Source)
                        tokens.UnionWith(rules.GetRule(rule).StartingTokens);

                    foreach (var rule in step.Source)
                        foreach (var tok in tokens)
                            rules.GetRule(rule).AddReducingToken(rules, tok);
                }
            }
        }

        /// <summary> Add a reducing token to this rule. </summary>
        /// <remarks> If the rule ends with a non-terminal, propagates. </remarks>
        public void AddReducingToken(RuleSet rules, int tok)
        {
            if (ReducingTokens.Contains(tok)) return;

            ReducingTokens.Add(tok);

            var last = Steps[Steps.Count - 1];
            if (last.IsTerminal) return;

            foreach (var rule in last.Source)
                rules.GetRule(rule).AddReducingToken(rules, tok);
        }

        /// <summary> True if this rule can start by left-derivation of rule i. </summary>
        public bool StartsWithRule(int rule) => 
            !Steps[0].IsTerminal && Steps[0].Source.Contains(rule);

        public override string ToString() => 
            Method?.Name ?? Type.Name;
    }
}