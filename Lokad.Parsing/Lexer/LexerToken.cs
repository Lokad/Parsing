namespace Lokad.Parsing.Lexer
{
    /// <summary> A token, as retrieved by a lexer from an input buffer. </summary>
    /// <remarks>
    ///     There will be thousands of tokens in a typical input, so every per-token
    ///     piece of information should be a value type in order to reduce memory
    ///     allocations. 
    /// </remarks>
    public struct LexerToken<TTok>
    {
        public LexerToken(TTok token, int start, int length)
        {
            Token = token;
            Start = start;
            Length = length;
        }

        /// <summary> The token type. </summary>
        public TTok Token { get; }

        /// <summary> The start of the token within the buffer. </summary>
        public int Start { get; }

        /// <summary> The length of the token. </summary>
        public int Length { get; }

        public override string ToString() => Token.ToString();
    }
}