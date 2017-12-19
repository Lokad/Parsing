using System;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Marks a token as the error token. </summary>
    /// <remarks> 
    ///     Error tokens are generated when the lexer fails to extract
    ///     a correct token. They are exactly one character long.
    /// </remarks>
    public sealed class ErrorAttribute : Attribute {}
}
