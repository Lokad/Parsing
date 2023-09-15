using System;
using System.Collections.Generic;
using Lokad.Parsing.Error;

namespace Lokad.BrainScript
{
    /// <see cref="ITokenNamer{TTok}"/>
    public sealed class TokenNamer : ITokenNamer<Token>
    {
        /// <summary> Singleton instance. </summary>
        public static readonly TokenNamer Instance = new TokenNamer();

        public bool IsFolded(Token t, ICollection<Token> others)
        {
            return false;
        }

        /// <see cref="ITokenNamer{TTok}.TokenName"/>
        public string TokenName(Token t, ICollection<Token> others)
        {
            switch (t)
            {
                case Token.Error: return "error";
                case Token.EoS: return "end-of-script";
                case Token.OpenBrace: return "'{'";
                case Token.CloseBrace: return "'}'";
                case Token.OpenParen: return "'('";
                case Token.CloseParen: return "')'";
                case Token.OpenBracket: return "'['";
                case Token.CloseBracket: return "']'";
                case Token.Semicolon: return "';'";
                case Token.Comma: return "','";
                case Token.Colon: return "':'";
                case Token.Dot: return "'.'";
                case Token.DotDot: return "'..'";
                case Token.Assign: return "'='";
                case Token.Lambda: return "'=>'";
                case Token.Plus: 
                case Token.Minus:
                case Token.Mult:
                case Token.Div:
                case Token.DotMult:
                case Token.And:
                case Token.Or:
                case Token.Lt:
                case Token.Gt:
                case Token.Eq:
                case Token.Leq:
                case Token.Geq:
                case Token.Neq: return "operator";
                case Token.Id: return "identifier";
                case Token.If: return "'if'";
                case Token.Then: return "'then'";
                case Token.Else: return "'else'";
                case Token.True: 
                case Token.False: return "boolean";
                case Token.String: return "string";
                case Token.Number: return "number";
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }
    }
}
