using System;
using System.Collections.Generic;

namespace Lokad.Parsing.Lexer
{
    /// <summary> A token that matches a list of possible values. </summary>
    /// <remarks>
    ///     This attribute should be placed on a member of the token <c>enum</c>. It 
    ///     specifies a list of constant strings, all of which will be tokenized by 
    ///     the <see cref="TokenReader{TTok}"/> as that member.
    /// </remarks>    
    public class AnyAttribute : Attribute
    {
        /// <summary> All possible values for this token. </summary>
        public IReadOnlyList<string> Options { get; }

        /// <summary> Is this token case sensitive ? </summary>
        public bool CaseSensitive { get; set; } = true;

        public AnyAttribute(params string[] options)
        {
            if (options.Length == 0)
                throw new ArgumentException(@"Expected at least one value.", nameof(options));

            Options = options;
        }

        /// <summary> Convert to a <see cref="TokenDefinition"/>. </summary>
        public TokenDefinition ToDefinition() => new TokenDefinition(Options, CaseSensitive);
    }

    /// <summary> Like <see cref="AnyAttribute"/> but case-insensitive by default. </summary>
    public sealed class AnyCiAttribute : AnyAttribute
    {
        public AnyCiAttribute(params string[] options) : base(options)
        {
            CaseSensitive = false;
        }
    }
}
