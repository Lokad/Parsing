using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Parsing.Parser
{
    /// <summary>
    /// Used on a parameter of a function marked as <see cref="RuleAttribute"/>
    /// to indicate that it expects a terminal token to be parsed.
    /// </summary>
    public class TerminalAttribute : Attribute
    {
        /// <summary> The expected tokens. </summary>
        /// <remarks> Any of these tokens can be parsed as the terminal. </remarks>
        public readonly IReadOnlyList<int> Tokens;

        /// <summary> Can this terminal be matched by nothing ? </summary>
        public readonly bool Optional;

        protected TerminalAttribute(IEnumerable<int> tokens, bool optional = false)
        {
            Tokens = tokens.ToArray();
            Optional = optional;
        }
    }
}
