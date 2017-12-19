using System;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Placed on an enumeration of tokens to provide general information. </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class TokensAttribute : Attribute
    {
        /// <summary> Is it possible to escape newlines with the '\' character ? </summary>
        /// <remarks>
        ///     If nothing separates a newline from a preceding '\', then both are canceled
        ///     out. Comments count as nothing.
        /// </remarks>
        public bool EscapeNewlines { get; set; }

        /// <summary> A pattern for comments. </summary>
        /// <example> "//[^\n]*" </example>
        public string Comments { get; set; }
    }
}
