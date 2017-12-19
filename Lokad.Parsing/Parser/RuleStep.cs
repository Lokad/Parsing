using System.Collections.Generic;

namespace Lokad.Parsing.Parser
{
    /// <summary> Represents an individual step in parsing a rule.  </summary>
    /// <remarks>
    /// Each parsing step correspond to an argument of the parsing rule method.
    /// </remarks>
    public struct RuleStep
    {
        /// <summary> Expect one of these values. </summary>
        /// <remarks> These can be either terminals or non-terminals. </remarks>
        public readonly IReadOnlyList<int> Source;

        /// <summary> Is this a terminal ? </summary>
        public readonly bool IsTerminal;

        public readonly int? Tag;

        public RuleStep(IReadOnlyList<int> source, int? tag, bool terminal)
        {
            Source = source;
            IsTerminal = terminal;
            Tag = tag;
        }
    }
}