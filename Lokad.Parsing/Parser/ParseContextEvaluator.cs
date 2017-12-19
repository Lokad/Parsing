using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing.Error;
using Lokad.Parsing.Lexer;

namespace Lokad.Parsing.Parser
{
    /// <summary>
    /// This class implement a "pseudo-parsing", maintaining the SLR parsing state
    /// but not producing any value beside an interpretable description of the parsing
    /// state. Can be used to determine precise information for auto completion.
    /// </summary>
    /// <typeparam name="TGrammar">
    /// Class implementing the grammar to evaluate only used for its type definition.
    /// </typeparam>
    /// <typeparam name="TTok">Type of the tokens.</typeparam>
    /// <typeparam name="TRez">Result type of the parsing. (root node of the grammar)</typeparam>
    /// <typeparam name="TCtxt">Type of the context</typeparam>
    public class ParseContextEvaluator<TGrammar, TTok, TRez, TCtxt>
        where TTok : struct
    {
        /// <summary> The rules of the grammar to be parsed. </summary>
        public readonly RuleSet Rules;

        /// <summary> The actions for each state. </summary>
        private readonly IReadOnlyList<short> _actions;

        /// <summary> Mapping for all states storing their associated tag list. </summary>
        private readonly IReadOnlyList<IReadOnlyList<TCtxt>> _stateContexts;

        /// <summary> Token namer used for context simplification. </summary>
        private readonly ITokenNamer<TTok> _namer;

        /// <summary> The initial state of the parser. </summary>
        private readonly int _initialState;

        public ParseContextEvaluator(
            ITokenNamer<TTok> namer,
            TokenReader<TTok> reader,
            Func<int, TCtxt> toContext)
        {
            Rules = new RuleSet(
                typeof(TGrammar), 
                typeof(TTok), 
                typeof(TRez), 
                (int) (object) reader.EndOfStream,
                reader.PublicChildren.ToDictionary(
                    kv => (int) (object) kv.Key, 
                    kv => (IReadOnlyList<int>) kv.Value.Select( i => (int)(object) i).ToArray()));

            var smb = new StateMachineBuilder(Rules);
            _namer = namer;
            _initialState = smb.Build();
            _actions = smb.Actions;
            _stateContexts = smb.TagContexts.Select(arr => arr.Select(toContext).ToArray()).ToArray();
        }

        /// <summary>
        /// Describe what the parser awaits from the last valid parsing
        /// state to the next one.
        /// </summary>
        public struct ShiftContext
        {
            public ShiftContext(TTok shiftToken, IReadOnlyList<TCtxt> context)
            {
                ShiftToken = shiftToken;
                Context = context;
            }

            /// <summary> Token that is required to shift in the next state </summary>
            public readonly TTok ShiftToken;

            /// <summary> What context are associated with the following state. </summary>
            public readonly IReadOnlyList<TCtxt> Context;

            public override string ToString() =>
                $"{nameof(ShiftToken)}: {ShiftToken}, {nameof(Context)}: {(string.Join(", ", Context.Select(v => v.ToString())))}";
        }

        /// <summary>
        /// Describes the parsing context above the current states, help
        /// understand in which global state we are in.
        /// </summary>
        public struct ParseFrame
        {
            public ParseFrame(IReadOnlyList<TCtxt> contexts)
                { Contexts = contexts; }

            /// <summary>
            /// List of context above us, extracted from the list of parsing states.
            /// </summary>
            public readonly IReadOnlyList<TCtxt> Contexts;

            public override string ToString() =>
                $"{nameof(Contexts)}: {string.Join(" ,", Contexts.Select(v => v.ToString()))}";
        }

        /// <summary>
        /// Result of the pseudo parsing up to the given limit or the
        /// first parsing error.
        /// </summary>
        public struct ContextResult
        {
            public ContextResult(
                IReadOnlyList<ParseFrame> context,
                IReadOnlyList<ShiftContext> comeAfter,
                TTok lastToken,
                int lastPosition,
                bool isOnError)
            {
                Context = context;
                LastToken = lastToken;
                LastPosition = lastPosition;
                IsOnError = isOnError;
                ComeAfter = comeAfter;
            }

            /// <summary>
            /// Information about the valid parse available from the
            /// last state.
            /// </summary>
            public readonly IReadOnlyList<ShiftContext> ComeAfter;

            /// <summary>
            /// Pile of context describing the global context of the
            /// current parsing state.
            /// </summary>
            public readonly IReadOnlyList<ParseFrame> Context;

            /// <summary>
            /// Copy of the last processed token during the pseudo parsing.
            /// </summary>
            public readonly TTok LastToken;

            /// <summary>
            /// Index of the last read token, in the input token stream.
            /// </summary>
            public readonly int LastPosition;

            /// <summary>
            /// true if the parsing halted on an error, false
            /// if the parsing stopped on the span limit.
            /// </summary>
            public readonly bool IsOnError;
        }

        private static bool IsShiftAction(short act) => act > 0;
        private static bool IsReduceAction(short act) => act < 0;

        private int BaseIndexOf(int state) =>
            (state - 1) * Rules.EntityCount;

        private short ActionOf(int state, int tok) =>
            _actions[tok + BaseIndexOf(state)];

        /// <summary>
        /// Contextual parse of tokens upto a given position.
        /// </summary>
        /// <param name="tokens">Result of parsing.</param>
        /// <param name="upto">Position to not go above for any parsed token</param>
        public ContextResult Compute(LexerResult<TTok> tokens, SourceSpan upto)
        {
            var contextStack = new Stack<int>();

            var onError = false;
            var readIndex = 0;
            var token = tokens.Tokens[0];
            var currentState = _initialState;

            while (true)
            {
                var action = ActionOf(currentState, (int)(object)token.Token);

                if (IsShiftAction(action))
                {
                    contextStack.Push(currentState);
                    currentState = action;

                    if (readIndex + 1 >= tokens.Count)
                        break;

                    token = tokens.Tokens[++readIndex];

                    if (token.Start + token.Length > upto.Location.Position)
                        break;
                }
                else if (IsReduceAction(action))
                {
                    var rule = -action;
                    var popCount = Rules.Rules[rule - Rules.TokenNames.Count].Steps.Count;

                    while (popCount-- > 1) contextStack.Pop();

                    currentState = ActionOf(contextStack.Peek(), rule);
                }
                else // error
                {
                    onError = true;
                    readIndex--; /* we didn't consume the token in the end */
                    break;
                }
            }

            var baseIndex = BaseIndexOf(currentState);

            var tokSet = new HashSet<TTok>(Enumerable
                .Range(0, Rules.TokenNames.Count)
                .Where(i => IsShiftAction(_actions[baseIndex + i]))
                .Select(i => (TTok) (object) i));

            // we minimize the token displayed using the token namer,
            // using the foldable information.
            var minimalTokens = tokSet
                .Where(t => !_namer.IsFolded(t, tokSet))
                .Select(t => new ShiftContext(t, _stateContexts[_actions[baseIndex + (int)(object)t]]))
                .ToArray();

            return new ContextResult(
                context: contextStack
                    .Where(st => _stateContexts[st].Count > 0)
                    .Select(st => new ParseFrame(_stateContexts[st]))
                    .ToArray(),
                comeAfter: minimalTokens,
                lastToken: readIndex >= 0 ? tokens.Tokens[readIndex].Token : default(TTok),
                lastPosition: readIndex,
                isOnError: onError);
        }
    }
}
