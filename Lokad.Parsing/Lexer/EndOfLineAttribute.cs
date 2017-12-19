using System;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Used to mark the end-of-line token. </summary>
    /// <remarks>
    ///     Generated when the <see cref="TokenReader{TTok}"/> reaches an end-of-line
    ///     character not ignored by another feature of the language. If indent and 
    ///     dedent tokens are in use (<see cref="IndentAttribute"/>), the indent
    ///     and dedent tokens (if any) are generated immediately after the end-of-line
    ///     token.
    /// 
    ///     End-of-line tokens always have a length of zero.
    /// </remarks>
    public sealed class EndOfLineAttribute : Attribute {}
}