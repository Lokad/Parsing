using System;
using Lokad.Parsing.Parser;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Marks an enumeration value as a sub-token of another token. </summary>
    /// <remarks>
    ///     When the <see cref="TokenReader{TTok}"/> examines potential token candidates
    ///     for recognizing the next piece of text, only tokens that are not sub-tokens 
    ///     will be considered.
    /// 
    ///     When a token is generated, the <see cref="TokenReader{TTok}"/> will attempt 
    ///     to match all its sub-tokens against the exact piece of text that was recognized.
    ///     If one of them matches, then it is generated instead of its parent. This 
    ///     sub-token recognition phase applies recursively until no sub-token matches,
    ///     which makes it possible for a sub-token to have sub-tokens of its own.
    /// 
    ///     You are encouraged to create a child class of <see cref="FromAttribute"/>
    ///     adapted to your enumeration, to avoid casting to integer on every time.    
    /// </remarks>
    /// <example><code> enum Token 
    /// {
    ///     [Pattern("[a-z]+")] Identifier,
    ///     [Any("if"), From((int)Identifier, true)] If
    /// }
    /// </code></example>
    /// <example><code>class FAttribute : FromAttribute 
    /// {
    ///     public FAttribute(Token s, bool isPrivate = false) : base((int)s, isPrivate) {}
    /// }
    /// 
    /// enum MyEnum
    /// {
    ///      [Pattern("[a-z]")] Identifier,
    ///      [Any("if"), F(Identifier, true)] If
    /// }
    /// </code></example>
    public class FromAttribute : Attribute
    {
        /// <summary> The value of the parent enumeration. </summary>
        /// <remarks> 
        ///     .NET does not support generic attributes (where the token enumeration would
        ///     be the type parameter), so we have to settle for casting the enumeration values
        ///     to and from an integer.
        /// </remarks>
        public int Parent { get; }

        /// <summary> Is this a private sub-token ?  </summary>
        /// <remarks> 
        ///     Private sub-tokens are entirely distinct from their parent tokens
        ///     (e.g <c>Token.If</c> is distinct from <c>Token.Identifier</c>). The sub-token 
        ///     relationship is used only for token generation, and is not available to the 
        ///     <see cref="GrammarParser{TSelf,TTok,TResult}"/>.
        /// 
        ///     Public sub-tokens are recognized by any grammar terminal that recognizes 
        ///     their parent token (e.g. a terminal that recognizes <c>Token.Operator</c>
        ///     will also implicitly recognize <c>Token.Multiply</c>). This is a convenience
        ///     feature that does exactly what a human author would do: any terminal that 
        ///     recognizes a token is automatically rewritten to recognize all public 
        ///     sub-tokens of that token as well.
        /// 
        ///     Sub-tokens are public by default.
        /// </remarks>
        public bool IsPrivate { get; }

        public FromAttribute(int parent, bool isPrivate = false)
        {
            Parent = parent;
            IsPrivate = isPrivate;
        }
    }
}
