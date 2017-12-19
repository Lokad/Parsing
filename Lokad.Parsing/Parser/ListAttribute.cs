using System;
using System.Collections.Generic;

namespace Lokad.Parsing.Parser
{
    /// <summary> Instructions about parsing an <see cref="IEnumerable{T}"/>. </summary>
    /// <remarks> 
    /// Used as an attribute on an <see cref="IEnumerable{T}"/> parameter of a function
    /// marked as <see cref="RuleAttribute"/>, describes whether the list is separated,
    /// terminated or neither, and whether there is a minimum number of values.
    /// </remarks>
    public class ListAttribute : Attribute
    {
        /// <summary> The instance separator token, placed between instances. </summary>
        public int? Separator { get; protected set; }

        /// <summary> The instance terminator token, placed after instances. </summary>
        public int? Terminator { get; protected set; }

        /// <summary> The minimum number of instances allowed. </summary>
        public int Min { get; set; }

        /// <summary> The maximum rank allowed for the expression within. </summary>
        public int? MaxRank { get; set; }

        public ListAttribute(int maxRank = -1)
        {
            MaxRank = maxRank >= 0 ? maxRank : (int?) null;
        } 
    }
}
