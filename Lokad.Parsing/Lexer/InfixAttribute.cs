using System;

namespace Lokad.Parsing.Lexer
{
    /// <summary> Describes the interaction between a token and end-of-line tokens. </summary>
    /// <remarks>
    ///     By default, normal tokens do not interact with end-of-line tokens and indent/dedent
    ///     tokens. When marked with <see cref="InfixAttribute"/> a token can be flagged as 
    ///     cannot-be-prefix or cannot-be-postfix (or both). 
    /// 
    ///     A cannot-be-postfix cannot appear at the end of a line. If followed directly by 
    ///     an end-of-line AND indent, then both the end-of-line and indent are omitted.
    /// 
    ///     A cannot-be-prefix cannot appear at the beginning of a line. If it follows directly
    ///     after an end-of-line AND indent, then both the end-of-line and indent are omitted.
    /// 
    ///     The omitted indent is NOT added to the indent stack (see example).
    /// 
    ///     This is a property of the token, and is not inherited by its children, public
    ///     or private.
    /// </remarks>
    /// <example><code>
    ///    foo / bar     
    ///    quux
    /// -> 'foo' DIV 'bar' EoL 'quux'
    ///     
    ///    foo / 
    ///    bar
    ///    quux
    /// -> 'foo' DIV EoL 'bar' EoL 'quux'
    /// 
    ///    foo / 
    ///      bar
    ///    quux
    /// -> 'foo' DIV 'bar' EoL 'quux'
    /// 
    ///    foo
    ///      / bar
    ///    quux
    /// -> 'foo' DIV 'bar' EoL 'quux'
    /// 
    ///    foo 
    ///      / bar
    ///      / baz
    ///      quux
    /// -> 'foo' DIV 'bar' DIV 'baz' EoL INDENT 'quux'
    /// </code></example>
    public sealed class InfixAttribute : Attribute
    {
        /// <summary> Can this token appear at the beginning of a line ? </summary>
        /// <remarks> Default is false. </remarks>
        public bool CanBePrefix { get; set; }

        /// <summary> Can this token appear at the end of a line ? </summary>
        /// <remarks> Default is false. </remarks>
        public bool CanBePostfix { get; set; }
    }
}
