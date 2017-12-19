using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Lokad.Parsing.Error;
using Lokad.Parsing.Lexer;

namespace Lokad.Parsing.Parser
{
    /// <summary>
    /// Parses a stream of <see cref="TTok"/> according to the grammar
    /// in the child class, resulting in an output of type 
    /// <see cref="TResult"/>.
    /// </summary>
    public abstract class 
        GrammarParser<TSelf, TTok, TResult> where TTok : struct
    {
        /// <summary> For error messages. </summary>
        private readonly ITokenNamer<TTok> _naming; 

        /// <summary> The start location of the currently evaluated rule.  </summary>
        /// <remarks> Computed from <see cref="StartToken"/> </remarks>
        private SourceLocation _startLocation;

        /// <summary> Backing for <see cref="StartToken"/>. </summary>
        /// <remarks>
        /// Using the default value of 0 prevent correct update of position information
        /// for the first matched rule, yielding a (0:0) SourceLocation instead of the
        /// real position of the first token.
        /// </remarks>
        private int _startToken = -1;

        /// <summary> The position of the first token of the currently reduced rule, in the stream. </summary>
        /// <remarks> Set from <see cref="StreamParser"/> before each reduction. </remarks>
        [UsedImplicitly]
        public int StartToken
        {
            get => _startToken;
            set
            {
                if (_startToken == value) return;

                _startToken = value;

                var position = Tokens.Tokens[_startToken].Start;

                Tokens.LineOfPosition(position, out var line, out var col);
                _startLocation = new SourceLocation(position, line, col);
            }
        }

        /// <summary> The position of the last (inclusive) token in the currently reduced rule. </summary>
        /// <remarks> Set from <see cref="StreamParser"/> before each reduction. </remarks>
        [UsedImplicitly]
        public int EndToken { get; set; }

        /// <summary> The location of the currently reduced rule. </summary>
        public SourceSpan Location
        {
            get
            {
                var end = Tokens.Tokens[EndToken];
                var start = EndToken == StartToken ? end : Tokens.Tokens[StartToken];
                var length = end.Length + end.Start - start.Start;
                return new SourceSpan(_startLocation, length);
            }
        }

        /// <summary> The available tokens. </summary>
        public LexerResult<TTok> Tokens { get; protected set; }
        
        /// <summary>
        /// The token reader used to parse strings into streams of 
        /// <see cref="TTok"/>, also contains reflexive information about
        /// the token type itself. 
        /// </summary>
        public static TokenReader<TTok> MakeTokenReader() => 
            new ReflectionTokenReader<TTok>();

        /// <summary> Used to compile <see cref="StreamParser"/> </summary>
        public static readonly ParserCompiler<TSelf, TTok, TResult> ParserCompiler =
            new ParserCompiler<TSelf, TTok, TResult>(MakeTokenReader());

        /// <summary> The parser used to reduce rules. </summary>
        public static readonly Func<TSelf, LexerResult<TTok>, TResult> StreamParser = 
            ParserCompiler.Compile();

        protected GrammarParser(ITokenNamer<TTok> naming)
        {
            _naming = naming;
        }

        /// <summary> Signal that an error has occurred during parsing. </summary>
        /// <remarks> Called from <see cref="StreamParser"/>. </remarks>
        public void OnErrorRaw(
            short[] actions, 
            int step, 
            short state, 
            Stack<short> stateStack)
        {
            var tokens = typeof (TTok).GetEnumNames().Length;
            var seenStates = new HashSet<short>();

            var token = Tokens.GetString(EndToken);
            if (token.Length > 0)
            {
                token = "'" + token + "'";
            }
            else
            {
                token = _naming.TokenName(Tokens.Tokens[EndToken].Token, new TTok[0]);
            }

            var found = new HashSet<TTok>(AcceptableValues(actions, tokens, step, state, stateStack, seenStates).Select(i => (TTok)(object)i));

            throw new ParseException(
                token,
                found.Select(t => _naming.TokenName(t, found)).Where(n => n != null).Distinct().ToArray(),
                Location);
        }

        /// <summary> Retrieve all token values that can shift from the current position. </summary>
        private static IEnumerable<int> AcceptableValues(
            short[] actions,
            int tokens, 
            int step, 
            short state,
            Stack<short> stateStack,
            HashSet<short> seenStates)
        {
            if (seenStates.Contains(state)) yield break;
            seenStates.Add(state);

            var a = (state-1)*step;
            for (var i = 0; i < tokens; ++i)
            {
                var r = actions[a + i];

                if (r == 0) continue;

                if (r > 0)
                {
                    // This is a shift, so the token is accepted.
                    yield return i;
                    continue;
                }

                var rule = -r;
                var pop = ParserCompiler.Rules.GetRule(rule).Steps.Count;
                var popped = new Stack<short>();

                if (pop <= stateStack.Count) continue;
                
                // Simulate reducing the rule...
                stateStack.Push(state);
                while (pop-- > 0) popped.Push(stateStack.Pop());
                state = stateStack.Pop();

                var a2 = (state-1)*step;
                var s2 = actions[a2 + rule];

                // ...list post-reduction shifts available...
                if (s2 > 0)
                    foreach (var t in AcceptableValues(actions, tokens, step, s2, stateStack, seenStates))
                        yield return t;
                
                // ...then undo the rule reduction
                stateStack.Push(state);
                while (popped.Count > 0) stateStack.Push(popped.Pop());
                state = stateStack.Pop();
            }
        } 
    }
}
