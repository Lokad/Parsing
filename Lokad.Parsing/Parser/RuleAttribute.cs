using System;
using JetBrains.Annotations;

namespace Lokad.Parsing.Parser
{
    [MeansImplicitUse]
    public class RuleAttribute : Attribute
    {
        /// <summary> The priority level of this rule. </summary>
        /// <remarks> Used to filter based on <see cref="NonTerminalAttribute.MaxRank"/>. </remarks>
        public int Rank { get; set; }
    }
}
