using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Parsing.Parser
{
    /// <summary> The representation of a node in the parser state machine. </summary>
    public sealed class ParserState
    {
        public struct RulePosition
        {
            /// <summary> The identifier of the rule. </summary>
            public readonly int Rule;

            /// <summary> The position within the rule. </summary>
            public readonly int Position;

            public RulePosition(int rule, int position)
            {
                Rule = rule;
                Position = position;
            }

            #region Equality

            private bool Equals(RulePosition other)
            {
                return Rule == other.Rule && Position == other.Position;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is RulePosition && Equals((RulePosition) obj);
            }

            public override int GetHashCode() => (Rule << 8) + Position;

            #endregion
        }

        /// <summary> The current position in the explored rules. </summary>
        public readonly IReadOnlyList<RulePosition> Rules;

        /// <summary> The hash code for this rule. </summary>
        private readonly int _hashcode;

        public ParserState(IEnumerable<RulePosition> rules)
        {            
            // Sort rules to have a canonical representation.
            var rulesA = rules.Distinct().ToArray();
            Array.Sort(rulesA, Compare);
            Rules = rulesA;

            foreach (var r in rulesA)
                _hashcode = (_hashcode*397) ^ r.GetHashCode();
        }

        #region Equality

        private bool Equals(ParserState other)
        {
            if (_hashcode != other._hashcode) return false;
            return Rules.Count == other.Rules.Count && Rules.SequenceEqual(other.Rules);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ParserState && Equals((ParserState) obj);
        }

        public override int GetHashCode() => _hashcode;

        #endregion

        /// <summary> A comparer for <see cref="RulePosition"/>. </summary>
        private static readonly  Comparer<RulePosition> Compare = Comparer<RulePosition>.Create((a, b) => 
            a.Rule < b.Rule ? -1 : a.Rule > b.Rule ? 1 : a.Position - b.Position);

        public string ToString(RuleSet rules)
        {
            var sb = new StringBuilder();
            foreach (var r in Rules)
            {
                var rule = rules.GetRule(r.Rule);
                sb.AppendFormat("{0}: ", rule);

                for (var i = 0; i < rule.Steps.Count; ++i)
                {
                    if (i == r.Position)
                        sb.AppendFormat(". ");

                    var step = rule.Steps[i];

                    if (step.IsTerminal)
                    {
                        if (step.Source.Count == 1)
                            sb.AppendFormat("{0} ", rules.TokenNames[step.Source[0]].ToUpperInvariant());
                        else
                            sb.AppendFormat("<{0}> ", string.Join(" ", step.Source.Select(n => rules.TokenNames[n].ToUpperInvariant()).Distinct()));
                    }
                    else
                    {
                        if (step.Source.Count == 1)
                            sb.AppendFormat("{0} ", rules.GetRule(step.Source[0]));
                        else
                            sb.AppendFormat("{{{0}}} ", string.Join(" ", step.Source.Select(n => rules.GetRule(n).ToString()).Distinct()));
                    }
                }

                if (r.Position == rule.Steps.Count)
                    sb.AppendFormat(". ");

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
