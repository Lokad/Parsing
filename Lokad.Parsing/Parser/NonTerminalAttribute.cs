using System;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Parsing.Parser
{
    /// <summary>
    /// On a parameter of a function marked with <see cref="RuleAttribute"/>,
    /// indicates that the value should be extracted from another function
    /// marked with <see cref="RuleAttribute"/>.
    /// </summary>
    public class NonTerminalAttribute : Attribute
    {
        /// <summary>
        /// The rule used to generate this non-terminal should have at most
        /// this priority. By default, there is no limit.
        /// </summary>
        public int? MaxRank { get; set; }

        public bool Optional { get; set; }

        protected NonTerminalAttribute(int maxRank = -1, bool optional = false)
        {
            MaxRank = maxRank < 0 ? (int?)null : maxRank;

            Optional = optional;
        }
    }

    /// <summary> Shorthand notation for <see cref="NonTerminalAttribute"/>. </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class NTAttribute : NonTerminalAttribute
    {
        public NTAttribute(int maxRank = -1) : base(maxRank, false) { }
    }

    /// <summary> 
    ///     Shorthand notation for <see cref="NonTerminalAttribute"/>, with
    ///     optional set to true.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class NTOAttribute : NonTerminalAttribute
    {
        public NTOAttribute(int maxRank = -1) : base(maxRank, true) { }
    }
}
