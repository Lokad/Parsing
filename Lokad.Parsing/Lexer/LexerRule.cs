using System.Collections.Generic;

namespace Lokad.Parsing.Lexer
{
    /// <summary> A lexing rule: a pattern and its token. </summary>
    public struct LexerRule<TTok>
    {
        /// <summary> The definition for this rule. </summary>
        public readonly TokenDefinition Definition;

        /// <summary> The token to be returned. </summary>
        public readonly TTok Token;

        /// <summary> True if this is a public child of another token. </summary>
        public readonly bool IsPublicChild;

        /// <see cref="InfixAttribute.CanBePrefix"/>
        public readonly bool CanBePrefix;

        /// <see cref="InfixAttribute.CanBePostfix"/>
        public readonly bool CanBePostfix;

        /// <summary> Sub-tokens of this token, as described in <see cref="FromAttribute"/>. </summary>
        public readonly IReadOnlyList<LexerRule<TTok>> SubTokens; 

        public LexerRule(
            TokenDefinition definition, 
            TTok token, 
            bool publicChild = false, 
            InfixAttribute infix = null,
            IReadOnlyList<LexerRule<TTok>> subTokens = null)
        {
            Definition = definition;
            Token = token;
            SubTokens = subTokens;
            CanBePrefix = infix?.CanBePrefix ?? true;
            CanBePostfix = infix?.CanBePostfix ?? true;
            IsPublicChild = publicChild;
        }

        public override string ToString() => $"{Token} = {Definition}";
    }
}