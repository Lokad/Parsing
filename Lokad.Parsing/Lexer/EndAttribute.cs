using System;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Marks a token as the end-of-stream. </summary>
    /// <remarks> 
    ///     This token is generated, exactly once, when the end of the input 
    ///     stream is reached. It has a length of zero.
    /// 
    ///     It is recommended to place this attribute on the enumeration member 
    ///     with a value of zero.
    /// </remarks>
    public sealed class EndAttribute : Attribute {}
}
