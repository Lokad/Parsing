using System.Collections.Generic;
using Lokad.Parsing.Parser;

namespace Lokad.Parsing.Error
{
    /// <summary> Provides names for tokens of the specified type. </summary>
    /// <remarks>
    ///     When a <see cref="GrammarParser{TSelf,TTok,TResult}"/> or another module 
    ///     needs to display a human-readable description of a token or set of 
    ///     tokens (such as the expected tokens, as part of a parsing error caused
    ///     by an unexpected token), it uses an object implementing this interface.
    /// </remarks>
    public interface ITokenNamer<TTok>
    {
        /// <summary> Whether a token should be displayed in a certain context. </summary>
        /// <example>
        ///     Consider a C#-like language, with a token <c>Tok.Operator</c> and two 
        ///     special cases, <c>Tok.Less</c> and <c>Tok.Greater</c>, which are used for
        ///     generics syntax in addition to their normal comparison operator usage.
        /// 
        ///     To generate list of acceptable tokens at a point in the source code, 
        ///     either for auto-completion or to clarify a parsing error, where the
        ///     only acceptable token is <c>Tok.Less</c>, then that token should be
        ///     displayed.
        /// 
        ///     If the acceptable tokens are <c>Tok.Operator</c>, <c>Tok.Less</c> and
        ///     <c>Tok.Greater</c>, then neither <c>Tok.Less</c> nor <c>Tok.Greater</c>
        ///     should be displayed, as they are included in (or <b>folded</b> in)
        ///     <c>Tok.Operator</c>.
        /// </example>
        /// <param name="t"> 
        ///     The token for which the question "Is this folded into any of the tokens 
        ///     in <paramref name="others"/> ?" is asked.
        /// </param>
        /// <param name="others">
        ///     All available tokens. Should include <paramref name="t"/>.
        /// </param>
        /// <returns>
        ///     True if <paramref name="t"/> should not be displayed in a context 
        ///     defined by <paramref name="others"/>.
        /// </returns>
        bool IsFolded(TTok t, ICollection<TTok> others);

        /// <summary> The human-readable contextual name of a token among a list of tokens. </summary>
        /// <remarks> Allowed to return null. </remarks>
        /// <param name="t"> The token that should be described as text. </param>
        /// <param name="others"> All available tokens. Should include <paramref name="t"/>. </param>
        /// <returns>
        ///     The name of token <paramref name="t"/>, knowing that <paramref name="others"/> 
        ///     contains all tokens being described. If the token is folded (in the sense
        ///     of <see cref="IsFolded"/>), this function should return null.
        /// </returns>
        string TokenName(TTok t, ICollection<TTok> others);
    }
}
