using System;

namespace Lokad.Parsing.Lexer
{
    /// <summary> A token that matches its own name, case-insensitive. </summary>
    /// <remarks>
    ///     This attribute should be placed on a member of the token <c>enum</c>. 
    ///     Any occurrences of that member's name, case-insensitive, will be tokenized 
    ///     by the <see cref="TokenReader{TTok}"/> as that member.
    /// </remarks>
    /// <example><code>enum Token 
    /// {
    ///     // Recognizes 'if', 'If', 'IF' and 'iF'
    ///     [Ci] If 
    /// } 
    /// </code></example>
    public sealed class CiAttribute : Attribute {}
}
