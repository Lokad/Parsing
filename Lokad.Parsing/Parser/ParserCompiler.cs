using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lokad.Parsing.Lexer;

namespace Lokad.Parsing.Parser
{
    public sealed class ParserCompiler<TSelf, TTok, TResult> where TTok : struct
    {
        /// <summary> The rules of the grammar to be parsed. </summary>
        public readonly RuleSet Rules;

        /// <summary> The actions for each state. </summary>
        private readonly short[] _actions;
        
        /// <summary> The initial state of the parser. </summary>
        private readonly int _initialState;

        /// <summary> The 'self' object used for method calls. </summary>
        private readonly ParameterExpression _selfP = Expression.Parameter(typeof (TSelf), "self");

        /// <summary> Input variable: the tokens. </summary>
        private readonly ParameterExpression _tokensP = Expression.Parameter(typeof (LexerResult<TTok>), "tokens");

        /// <summary> Input variable: the actions. </summary>
        /// <see cref="_actions"/>
        private readonly ParameterExpression _actionsP = Expression.Parameter(typeof (short[]), "actions");

        /// <summary> The stack of currently un-reduced tokens. </summary>
        /// <remarks> 
        ///     For each terminal or non-terminal currently on the stack, its initial
        ///     token (for terminals, this is obviously the ONLY token).
        /// 
        ///     Values are indices into <see cref="_tokensP"/>, not token values themselves ! 
        /// </remarks>
        private readonly ParameterExpression _startTokensV = Expression.Parameter(typeof (Stack<int>), "unreduced");

        /// <summary> The state stack (not including current state). </summary>
        private readonly ParameterExpression _statesV = Expression.Parameter(typeof (Stack<short>), "states");

        /// <summary> The current state (as if on top of the stack). </summary>
        private readonly ParameterExpression _stateV = Expression.Parameter(typeof (short), "state");

        /// <summary> The position being read in the input. </summary>
        private readonly ParameterExpression _posV = Expression.Parameter(typeof (int), "pos");

        /// <summary> The last read token, cast to <c>int</c>. </summary>
        private readonly ParameterExpression _tokenV = Expression.Parameter(typeof (int), "token");

        /// <summary> The current action. </summary>
        private readonly ParameterExpression _actionV = Expression.Parameter(typeof (short), "action");

        /// <summary> The break target for the main parser loop. </summary>
        private readonly LabelTarget _loopBreak = Expression.Label();

        /// <summary> One stack for each type that can be returned by a rule. </summary>
        /// <remarks> Lists are handled in <see cref="_listStackV"/> instead. </remarks>
        private readonly Dictionary<Type,ParameterExpression> _stacks = new Dictionary<Type, ParameterExpression>();

        /// <summary> The *length* of each list unreduced list is pushed down here. </summary>
        private readonly ParameterExpression _listStackV = Expression.Parameter(typeof (Stack<int>), "list");

        public ParserCompiler(TokenReader<TTok> reader)
        {
            Rules = new RuleSet(
                typeof(TSelf), 
                typeof(TTok), 
                typeof(TResult), 
                (int) (object) reader.EndOfStream,
                reader.PublicChildren.ToDictionary(
                    kv => (int) (object) kv.Key, 
                    kv => (IReadOnlyList<int>) kv.Value.Select( i => (int)(object) i).ToArray()));

            var smb = new StateMachineBuilder(Rules);
            _initialState = smb.Build();
            _actions = smb.Actions;

            // File.WriteAllText("C:/LokadData/priceforge/slr.txt", smb.Describe());

            foreach (var r in Rules.Rules)
            {
                var t = r.Type;
                if (!_stacks.ContainsKey(t))
                    _stacks.Add(t, Expression.Parameter(typeof(Stack<>).MakeGenericType(t), "stack_" + t.Name));
            }
        }

        /// <summary> Generates initialization code. </summary>
        private BlockExpression Initialization()
        {
            var init = new List<Expression>
            {
                Expression.Assign(_posV, Expression.Constant(0)),
                ReadToken(),
                Expression.Assign(_startTokensV, Expression.New(_startTokensV.Type)),
                Expression.Assign(_listStackV, Expression.New(_listStackV.Type)),
                Expression.Assign(_statesV, Expression.New(_statesV.Type)),
                Expression.Assign(_stateV, Expression.Constant((short)_initialState))
            };

            init.AddRange(_stacks.Values.Select(v => Expression.Assign(v, Expression.New(v.Type))));

            return Expression.Block(init);
        }

        /// <summary> Generates an error message when a token matches nothing. </summary>
        /// <remarks>
        ///     Breaks out of the main loop.
        /// </remarks>
        private Expression Error()
        {
            var method = _selfP.Type.GetMethod(nameof(GrammarParser<int, int, int>.OnErrorRaw))
                ?? throw new Exception($"Did not find {nameof(GrammarParser<int, int, int>.OnErrorRaw)}.");

            var step = Expression.Constant(Rules.EntityCount, typeof (int));

            return Expression.Block(
                SetEndToken(_posV),
                SetStartToken(_posV),
                Expression.Call(_selfP, method, _actionsP, step, _stateV, _statesV));
        }

        public Func<TSelf, LexerResult<TTok>, TResult> Compile()
        {            
            var loop = new List<Expression>
            {
                // At the start of the loop, token is 'already' set to the token 
                // at position 'pos' (this is done upon initialization, then each
                // time 'pos' is incremented).

                // action = actions[token + (state - 1) * N]
                Expression.Assign(_actionV, Expression.ArrayAccess(_actionsP, 
                    Expression.Add(_tokenV, 
                        Expression.Multiply(
                            Expression.Convert(Expression.Decrement(_stateV), typeof(int)),
                            Expression.Constant(Rules.EntityCount))))),

                // if (action == 0) error !
                Expression.IfThen(Expression.Equal(_actionV, Expression.Constant((short)0)),
                    Error()),

                // if (action > 0)
                Expression.IfThenElse(
                    Expression.GreaterThan(_actionV, Expression.Constant((short)0)),
                    Shift(),
                    Reduce())

                 // The loop ends when Reduce() hits the initial rule.
            };

            var full = Expression.Block(
                new[] { _actionV, _listStackV, _posV, _stateV, _statesV, _startTokensV, _tokenV }
                    .Concat(_stacks.Values),
                Initialization(),
                Expression.Loop(Expression.Block(loop), _loopBreak),
                Pop(_stacks[typeof (TResult)]));

            var lambda = Expression.Lambda<Func<short[], TSelf, LexerResult<TTok>, TResult>>(
                full, _actionsP, _selfP, _tokensP);

            // Compile and include '_actions' in closure

            var func = lambda.Compile();
            var actions = _actions;

            return (self, tokens) => func(actions, self, tokens);
        }

        /// <summary> Push a value onto a stack. </summary>
        private Expression Push(Expression stack, Expression arg)
            => Expression.Call(stack, "Push", new Type[0], arg);

        /// <summary> Pop a value onto a stack. </summary>
        private Expression Pop(Expression stack)
            => Expression.Call(stack, "Pop", new Type[0]);

        /// <summary> Peek at a stack value. </summary>
        private Expression Peek(ParameterExpression stack)
            => Expression.Call(stack, "Peek", new Type[0]);
        
        /// <summary>
        /// Sets the <see cref="GrammarParser{TSelf,TTok,TResult}.EndToken"/> of the 
        /// underlying parser to the specified position.
        /// </summary>
        private Expression SetEndToken(Expression pos) =>             
            Expression.Assign(
                Expression.Property(_selfP, "EndToken"),
                pos);

        /// <summary>
        /// Sets the <see cref="GrammarParser{TSelf,TTok,TResult}.StartToken"/> of the 
        /// underlying parser to the specified position.
        /// </summary>
        private Expression SetStartToken(Expression pos) =>
            Expression.Assign(
                Expression.Property(_selfP, "StartToken"),
                pos);
        
        /// <summary> Performs the LR reduce. </summary>
        /// <remarks>
        ///     Pops from the various stacks depending on the function to reduce, 
        ///     pushes the function's result onto the appropriate non-terminal stack, 
        ///     then replaces the current state with the one pointed at by the 
        ///     reduction action in the current state.
        /// </remarks>
        private Expression Reduce()
        {
            var rule = Expression.Parameter(typeof (int), "rule");
            var pops = Expression.Parameter(typeof (int), "pops");
            var endPop = Expression.Label();

            var todo = new List<Expression>
            {
                // rule = -(int)action
                Expression.Assign(rule, Expression.Negate(Expression.Convert(_actionV, typeof(int)))),

                // Perform the specific reduce (pop, push). This code does not touch the state
                // stack, instead leaving the value 'pops' equal to how many states should be popped
                // off that stack (always at least one).

                ReduceRules(rule, pops),

                // Move to the new, appropriate current state.
            
                //Expression.Call(typeof (Console), "WriteLine", new Type[0],
                //    Expression.Constant("Stack [{1} {0}] pops {2}"),
                //    Expression.Convert(Expression.Call(typeof(string), "Join", new [] {typeof(short)},
                //        Expression.Constant(" "),
                //        Expression.Convert(_statesV, typeof(IEnumerable<short>))), typeof(object)),
                //    Expression.Convert(_stateV, typeof(object)),
                //    Expression.Convert(pops, typeof (object))),

                // while (pops-- > 1) states.Pop()
                Expression.Loop(Expression.IfThenElse(
                    Expression.GreaterThan(Expression.PostDecrementAssign(pops), Expression.Constant(1)),
                    Pop(_statesV),
                    Expression.Break(endPop)),
                    endPop),

                //Expression.Call(typeof (Console), "WriteLine", new Type[0],
                //    Expression.Constant("Stack [{0}]"),
                //    Expression.Convert(Expression.Call(typeof(string), "Join", new [] {typeof(short)},
                //        Expression.Constant(" "),
                //        Expression.Convert(_statesV, typeof(IEnumerable<short>))), typeof(object)),
                //    Expression.Convert(_stateV, typeof(object))),
                
                // state = states.Peek()
                Expression.Assign(_stateV, Peek(_statesV)),
                
                // state = actions[rule + (state - 1) * N]
                Expression.Assign(_stateV, Expression.ArrayAccess(_actionsP,
                    Expression.Add(rule,
                        Expression.Multiply(
                            Expression.Convert(Expression.Decrement(_stateV), typeof(int)),
                            Expression.Constant(Rules.EntityCount)))))
            };

            return Expression.Block(new[] { rule, pops }, todo);
        }
        

        /// <summary> A switch statement to decide how to handle the reduced rule. </summary>
        private Expression ReduceRules(ParameterExpression rule, ParameterExpression pops)
        {
            var cases = new SwitchCase[Rules.Rules.Count];

            for (var r = 0; r < cases.Length; ++r)
            {
                var reduced = Rules.Rules[r];

                var locals = new List<ParameterExpression>();
                var body = new List<Expression>
                {
                    // Pop one state for each shift in this rule (because each shift
                    // has generated one rule).
                    Expression.Assign(pops, Expression.Constant(reduced.Steps.Count))
                };

                if (reduced.IsList)
                {
                    // List reduction does not actually construct a list, it merely keeps track
                    // of the list length in '_listStackV', with the actual list data found in 
                    // the stack for its own type.

                    if (reduced.IsListEnd)
                    {
                        // list.Push(1)
                        body.Add(Push(_listStackV, Expression.Constant(1)));
                    }
                    else
                    {
                        // N is the number of non-terminals in the rule.
                        // list.Push(list.Pop() + (N - 1))
                        var n = reduced.Steps.Count(s => !s.IsTerminal) - 1;
                        body.Add(Push(_listStackV, 
                                Expression.Add(Expression.Constant(n), Pop(_listStackV))));
                    }

                    // Also pop all start-tokens except our own.
                    var terminals = reduced.Steps.Count - 1;
                    if (terminals > 0)
                    {
                        while (terminals-- > 0)
                            body.Add(Pop(_startTokensV));

                        // The overall block returns void instead of the last pop.
                        body.Add(Expression.Empty());
                    }
                }
                else
                {
                    // Method reduction pops all the stacks into local variables first, then performs
                    // the call. Arguments are constructed left-to-right.

                    var ps = reduced.Method.GetParameters();
                    var args = new Expression[ps.Length];

                    var firstProvided = 0;
                    while (!reduced.Provided[firstProvided]) ++firstProvided;

                    for (var i = reduced.Provided.Count - 1; i >= 0; --i)
                    {
                        var p = ps[i];
                        var t = p.ParameterType;
                        var isNullable =
                            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);

                        if (!reduced.Provided[i])
                        {
                            // Argument not provided.
                            if (t == typeof (TTok?) || t == typeof (string))
                            {
                                args[i] = Expression.Constant(null, t);
                            }
                            else if (t.IsArray)
                            {
                                var elemType = t.GetElementType()
                                    ?? throw new Exception($"{t}.IsArray is true, but null GetElementType()");

                                args[i] = Expression.NewArrayBounds(elemType, Expression.Constant(0));
                            }
                            // for optional non-terminal.
                            else if (t.IsClass || isNullable)
                            {
                                args[i] = Expression.Default(t);
                            }
                            else
                            {
                                throw new Exception($"Unsupported optional parameter type: {t}.");
                            }
                        }
                        else if (t.IsArray)
                        {
                            var elemType = t.GetElementType()
                                           ?? throw new Exception($"{t}.IsArray is true, but null GetElementType()");

                            // Argument is a list, so construct it
                            var arg = Expression.Parameter(t, "arg" + i);
                            var len = Expression.Parameter(typeof(int), "arg" + i + "_len");
                            args[i] = arg;
                            locals.Add(arg);
                            locals.Add(len);

                            // len = list.Pop()
                            body.Add(Expression.Assign(len, Pop(_listStackV)));

                            // arg = new T[len]
                            body.Add(Expression.Assign(arg,
                                Expression.NewArrayBounds(elemType, len)));

                            // do { arg[--len] = stack.Pop() } while (len > 0)
                            var stack = _stacks[elemType];
                            var brk = Expression.Label();
                            body.Add(Expression.Loop(
                                Expression.Block(
                                    Expression.Assign(
                                        Expression.ArrayAccess(arg, Expression.PreDecrementAssign(len)),
                                        Pop(stack)),
                                    Expression.IfThen(
                                        Expression.LessThanOrEqual(len, Expression.Constant(0)),
                                        Expression.Break(brk))),
                                brk));

                            // Pop the start token of this non-terminal, unless it is also OUR
                            // start token.
                            if (i > firstProvided)                                
                                body.Add(Pop(_startTokensV));
                        }
                        else if (t == typeof (TTok) || t == typeof (TTok?))
                        {
                            // Argument is the token itself
                            var arg = Expression.Parameter(t, "arg" + i);
                            args[i] = arg;
                            locals.Add(arg);

                            // Do not pop our own start token
                            var token = i == firstProvided ? Peek(_startTokensV) : Pop(_startTokensV);

                            // arg = tokens.Tokens[unreduced.{Pop|Peek}()].Token
                            var value = (Expression) Expression.PropertyOrField(
                                Expression.Property(
                                    Expression.PropertyOrField(_tokensP, nameof(LexerResult<TTok>.Tokens)),
                                    "Item",
                                    token),
                                nameof(LexerToken<TTok>.Token));

                            if (t == typeof (TTok?))
                                value = Expression.Convert(value, t);

                            body.Add(Expression.Assign(arg, value));
                        }
                        else if (t == typeof(string) || t == typeof(Pos<string>))
                        {
                            // Extract the value of a token
                            var arg = Expression.Parameter(t, "arg" + i);
                            args[i] = arg;
                            locals.Add(arg);

                            // Do not pop our own start token
                            var token = i == firstProvided ? Peek(_startTokensV) : Pop(_startTokensV);

                            // arg = tokens.{GetString|GetStringPos}(unreduced.{Peek|Pop}())

                            var method = t == typeof (string)
                                ? nameof(LexerResult<TTok>.GetString)
                                : nameof(LexerResult<TTok>.GetStringPos);

                            body.Add(Expression.Assign(arg, 
                                Expression.Call(_tokensP, method, new Type[0],
                                    token)));
                        }
                        else if (isNullable)
                        {
                            var arg = Expression.Parameter(t, "arg" + i);
                            args[i] = arg;
                            locals.Add(arg);

                            // arg = stack.Pop()
                            var stack = _stacks[t.GetGenericArguments()[0]];
                            body.Add(Expression.Assign(arg, Expression.Convert(Pop(stack), t)));

                            // Pop the start token of this non-terminal, unless it is also OUR
                            // start token.
                            if (i > firstProvided)
                                body.Add(Pop(_startTokensV));
                        }
                        else
                        {
                            // Argument is a non-terminal value, pop it
                            var arg = Expression.Parameter(t, "arg" + i);
                            args[i] = arg;
                            locals.Add(arg);
                            
                            // arg = stack.Pop()
                            var stack = _stacks[t];
                            body.Add(Expression.Assign(arg, Pop(stack)));

                            // Pop the start token of this non-terminal, unless it is also OUR
                            // start token.
                            if (i > firstProvided)
                                body.Add(Pop(_startTokensV));
                        }
                    }

                    // Prepare the 'where'
                    body.Add(SetStartToken(Peek(_startTokensV)));
                    body.Add(SetEndToken(Expression.Decrement(_posV)));

                    // All arguments ready ! Call.
                    
                    // stack.Push(self.method(args))
                    var outStack = _stacks[reduced.Type];
                    body.Add(Push(outStack, Expression.Call(_selfP, reduced.Method, args)));
                }

                // Is this an initial rule ? Then break out !
                if (Rules.InitialRules.Contains(r + Rules.TokenNames.Count))
                    body.Add(Expression.Break(_loopBreak));

                cases[r] = Expression.SwitchCase(
                    Expression.Block(locals, body), 
                    Expression.Constant(r + Rules.TokenNames.Count));
            }

            return Expression.Switch(rule, cases);
        }

        /// <summary> Performs the LR shift. </summary>
        /// <remarks> 
        ///     This pushes the current state and token on their respective stacks, 
        ///     moves to the new state and reads the next token.
        /// </remarks>
        private BlockExpression Shift() => Expression.Block(
                        
            // unreduced.Push(pos++)
            Push(_startTokensV, Expression.PostIncrementAssign(_posV)),

            ReadToken(),

            // states.Push(state)
            Push(_statesV, _stateV),

            // state = action
            Expression.Assign(_stateV, _actionV));
        
        /// <summary> Read the currently pointed-at token. </summary>
        /// <remarks>
        ///     Once the end of the stream is reached, repeats 'EOS' indefinitely.
        /// </remarks>
        private Expression ReadToken() =>

            // if (pos < tokens.Count) 
            //   token = (int) tokens.Tokens[pos].Token

            Expression.IfThen(
                Expression.LessThan(_posV, Expression.PropertyOrField(_tokensP, nameof(LexerResult<TTok>.Count))),                
                Expression.Assign(_tokenV, Expression.Convert(
                    Expression.PropertyOrField(
                        Expression.Property(
                            Expression.PropertyOrField(_tokensP, nameof(LexerResult<TTok>.Tokens)),
                            "Item",
                            _posV),
                        nameof(LexerToken<TTok>.Token)),
                    typeof (int))));
    }
}
