using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lokad.BrainScript.Ast;
using Lokad.Parsing;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;
using Tk = Lokad.BrainScript.Token;

namespace Lokad.BrainScript
{
    public sealed class Parser : GrammarParser<Parser, Tk, Parser.Root>
    {
        /// <summary> The root type of the parser. </summary>
        public struct Root
        {
            public Root(IReadOnlyList<IAssign> fields) { Fields = fields; }

            /// <summary> All assigned global fields in the script. </summary>
            public IReadOnlyList<IAssign> Fields { get; }
        }

        /// <summary> Parse a script, returns the root record. </summary>
        /// <exception cref="ParserException"> If the script cannot be parsed. </exception>
        public static RecordExpr Parse(string brainscript)
        {
            var p = new Parser();
            
            var tokens = p.Tokens = MakeTokenReader().ReadAllTokens(brainscript);

            if (tokens.HasInvalidTokens)
            {
                var t = tokens.Tokens.First(tok => tok.Token == Tk.Error);
                
                tokens.LineOfPosition(t.Start, out int line, out int col);

                throw new ParserException(
                    "Invalid character: '" + brainscript[t.Start] + "'.",
                    new SourceSpan(new SourceLocation(t.Start, line, col), t.Length));
            }

            try
            {
                var parsed = StreamParser(p, tokens);
                return new RecordExpr(parsed.Fields);
            }
            catch (ParseException e)
            {
                throw new ParserException(
                    $"Found `{e.Token}` but expected {string.Concat(", ", e.Expected)}.",
                    e.Location,
                    e);
            }
        }

        public Parser() : base(TokenNamer.Instance) { }

        #region Literals and identifiers

        [Rule] public LitExpr TrueLiteral([T(Tk.True)] Tk a) => new LitExpr<bool>(true);
        [Rule] public LitExpr FalseLiteral([T(Tk.False)] Tk b) => new LitExpr<bool>(false);
        
        [Rule] public LitExpr NumberLiteral([T(Tk.Number)] string n) =>
            new LitExpr<float>(float.Parse(n, CultureInfo.InvariantCulture));

        [Rule] public LitExpr StringLiteral([T(Tk.String)] string s) =>
            new LitExpr<string>(s.Substring(1, s.Length - 2));

        [Rule] public IExpr Literal([NT] LitExpr e) => e;

        [Rule] public IExpr Identifier([T(Tk.Id)] string s) =>
            new IdExpr(s);

        [Rule] public IExpr Parenthesis(
            [T(Tk.OpenParen)]  Tk a,
            [NT]               IExpr inner,
            [T(Tk.CloseParen)] Tk b)
        =>
            inner;

        #endregion

        #region Lambda and array expressions

        // (param => expr)
        [Rule] public IExpr Lambda(
            [T(Tk.OpenParen)]  Tk a,
            [T(Tk.Id)]         string parameter,
            [T(Tk.Lambda)]     Tk b,
            [NT]               IExpr body,         
            [T(Tk.CloseParen)] Tk c)
        =>
            new LambdaExpr(parameter, body);

        // ( expr : expr ), (expr : expr : expr), etc 
        [Rule]
        public IExpr Tensor(
            [T(Tk.OpenParen)]  Tk a,
            [NT]               IExpr head,
            [T(Tk.Colon)]      Tk b,            
            [L(Sep=Tk.Colon)]  IExpr[] tail,
            [T(Tk.CloseParen)] Tk c)
        {
            var arr = new IExpr[tail.Length + 1];
            arr[0] = head;
            Array.Copy(tail, 0, arr, 1, tail.Length);
            return new TensorExpr(arr);
        }

        // array[expr..expr] (param => expr)
        [Rule] public IExpr ArrayFunc(
            [T(Tk.Array)]        Tk a,
            [T(Tk.OpenBracket)]  Tk b,
            [NT]                 IExpr min,
            [T(Tk.DotDot)]       Tk c,
            [NT]                 IExpr max,
            [T(Tk.CloseBracket)] Tk d,
            [T(Tk.OpenParen)]    Tk e,
            [T(Tk.Id)]           string parameter,
            [T(Tk.Lambda)]       Tk f,
            [NT]                 IExpr body,
            [T(Tk.CloseParen)]   Tk g)
        =>
            new ArrayExpr(min, max, parameter, body);

        [Rule] public IExpr ArrayAt(
            [NT(0)]              IExpr array,
            [T(Tk.OpenBracket)]  Tk a,
            [NT]                 IExpr index,
            [T(Tk.CloseBracket)] Tk b)
        =>
            new AtExpr(array, index);

        #endregion

        #region Assignment

        // id = expr
        [Rule] public IAssign IdAssign(
            [T(Tk.Id)]        string name,
            [T(Tk.Assign)]    Tk a,
            [NT]              IExpr expr,
            [O(Tk.Semicolon)] Tk? b)
        =>
            new IdAssign(name, expr);

        // id[param:expr..expr] = expr
        [Rule]
        public IAssign ArrayAssign(
            [T(Tk.Id)]           string name,
            [T(Tk.OpenBracket)]  Tk a,
            [T(Tk.Id)]           string param,
            [T(Tk.Colon)]        Tk b,
            [NT]                 IExpr min,
            [T(Tk.DotDot)]       Tk c,
            [NT]                 IExpr max,
            [T(Tk.CloseBracket)] Tk d,
            [T(Tk.Assign)]       Tk e,
            [NT]                 IExpr body,
            [O(Tk.Semicolon)]    Tk? f)
        =>
            new IdAssign(name, new ArrayExpr(min, max, param, body));
        
        // id(paramlist) = expr
        [Rule] public IAssign FuncAssign(
            [T(Tk.Id)]         string name,
            [T(Tk.OpenParen)]  Tk a,
            [L(Sep=Tk.Comma)]  FuncAssign.Param[] parameters,
            [T(Tk.CloseParen)] Tk b,
            [T(Tk.Assign)]     Tk c,
            [NT]               IExpr expr,
            [O(Tk.Semicolon)]  Tk? d)
        =>
            new FuncAssign(name, parameters, false, expr);

        // id{paramlist} = expr
        [Rule] public IAssign LayerAssign(
            [T(Tk.Id)]         string name,
            [T(Tk.OpenBrace)]  Tk a,
            [L(Sep=Tk.Comma)]  FuncAssign.Param[] parameters,
            [T(Tk.CloseBrace)] Tk b,
            [T(Tk.Assign)]     Tk c,
            [NT]               IExpr expr,
            [O(Tk.Semicolon)]  Tk? d)
        =>
            new FuncAssign(name, parameters, true, expr);

        /// <summary> A simple named parameter. </summary>
        [Rule] public FuncAssign.Param SimpleParameter([T(Tk.Id)] string name) => 
            new FuncAssign.Param(name);

        /// <summary> A named parameter with a default value. </summary>
        [Rule] public FuncAssign.Param DefaultParameter(
            [T(Tk.Id)]     string name,
            [T(Tk.Assign)] Tk a,
            [NT]           LitExpr dflt)
        =>
            new FuncAssign.Param(name, dflt);

        #endregion

        #region Record expressions

        // expr.member
        [Rule] public IExpr Member(
            [NT(0)]     IExpr expr,
            [T(Tk.Dot)] Tk a,
            [T(Tk.Id)]  string field)
        =>
            new FieldExpr(expr, field);

        // { id = expr ; ... }
        [Rule] public IExpr Record(
            [T(Tk.OpenBrace)]  Tk a,
            [L]                IAssign[] fields,
            [T(Tk.CloseBrace)] Tk b)
        =>
            new RecordExpr(fields);

        // Root of the script
        [Rule] public Root Script(
            [L(Min=1)]  IAssign[] fields,
            [T(Tk.EoS)] Tk eos)
        =>
            new Root(fields);

        #endregion

        #region Call expressions

        [Rule] public IExpr FuncCall(
            [NT(0)]            IExpr func,
            [T(Tk.OpenParen)]  Tk a,
            [L(Sep=Tk.Comma)]  CallExpr.Arg[] args,
            [T(Tk.CloseParen)] Tk b)
        =>
            new CallExpr(func, args, false);

        [Rule] public IExpr LayerCall(
            [NT(0)]            IExpr func,
            [T(Tk.OpenBrace)]  Tk a,
            [L(Sep=Tk.Comma)]  CallExpr.Arg[] args,
            [T(Tk.CloseBrace)] Tk b)
        =>
            new CallExpr(func, args, true);

        [Rule] public CallExpr.Arg SimpleArg([NT] IExpr arg) =>
            new CallExpr.Arg(arg);

        [Rule] public CallExpr.Arg LabeledArg(
            [T(Tk.Id)]     string label,
            [T(Tk.Assign)] Tk a,
            [NT]           IExpr arg)
        =>
            new CallExpr.Arg(arg, label);

        #endregion

        #region Operators

        [Rule(Rank = 1)] public IExpr Unary(
            [T(Tk.Minus,Tk.Not)] Tk t,
            [NT(1)]              IExpr operand)
        =>
            new UnaryExpr(t == Tk.Minus ? UnaryOp.Minus : UnaryOp.Not, operand);

        /// <summary> The right side of an infix operation. </summary>
        /// <remarks>
        ///     To keep the grammar simple, we implement operator priorities on 
        ///     top of a list of infix-separated expressions.
        /// </remarks>
        public struct InfixRight
        {
            public BinaryOp Op { get; set; }
            public IExpr Right { get; set; }
        }

        [Rule]
        public InfixRight AndThen(
            [T(Tk.Plus, Tk.Minus, Tk.And, Tk.Or, Tk.Mult, Tk.DotMult,
               Tk.Div, Tk.Leq, Tk.Lt, Tk.Geq, Tk.Gt, Tk.Eq, Tk.Neq)] Tk t,
            [NT(1)] IExpr right)
        {
            BinaryOp op;
            switch (t)
            {
                case Tk.Plus:    op = BinaryOp.Plus; break;
                case Tk.Minus:   op = BinaryOp.Minus; break;
                case Tk.Mult:    op = BinaryOp.Mult; break;
                case Tk.Div:     op = BinaryOp.Div; break;
                case Tk.DotMult: op = BinaryOp.DotMult; break;
                case Tk.And:     op = BinaryOp.And; break;
                case Tk.Or:      op = BinaryOp.Or; break;
                case Tk.Lt:      op = BinaryOp.Lt; break;
                case Tk.Gt:      op = BinaryOp.Gt; break;
                case Tk.Eq:      op = BinaryOp.Eq; break;
                case Tk.Leq:     op = BinaryOp.Leq; break;
                case Tk.Geq:     op = BinaryOp.Geq; break;
                case Tk.Neq:     op = BinaryOp.Neq; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }

            return new InfixRight {Op = op, Right = right};
        }

        [Rule(Rank = 2)]
        public IExpr Binary(
            [NT(1)]    IExpr left,
            [L(Min=1)] InfixRight[] right)
        {
            if (right.Length == 1) return new BinaryExpr(left, right[0].Op, right[0].Right);

            // This algorithm traverses the array of expressions and operators multiple times,
            // once for each priority level, and turns operations of that priority level into
            // expressions, with left-associativity. The end result, once all priority levels
            // are traversed, should be a single expression stored in "left". Variable "length"
            // represents the number of unmerged expressions still in "right" (all of them 
            // are shifted to the beginning of the array) and should be 0 at the end.

            var length = right.Length;
            for (var prio = BinaryOpExtensions.MaxPriority; length > 0; --prio)
            {
                var j = 0;
                for (var i = 0; i < length; ++i, ++j)
                {
                    if (right[i].Op.Priority() == prio)
                    {
                        var myLeft = j == 0 ? left : right[j - 1].Right;
                        var expr = new BinaryExpr(myLeft, right[i].Op, right[i].Right);

                        if (j == 0) left = expr;
                        else right[j - 1].Right = expr;

                        --j;
                    }
                    else
                    {
                        right[j] = right[i];
                    }
                }

                length = j;
            }

            return left;
        }

        [Rule(Rank = 3)] public IExpr IfThenElse(
            [T(Tk.If)]   Tk a,
            [NT]         IExpr cond,
            [T(Tk.Then)] Tk b,
            [NT]         IExpr ifTrue,
            [T(Tk.Else)] Tk c,
            [NT]         IExpr ifFalse)
        =>
            new IfExpr(cond, ifTrue, ifFalse);

        #endregion
    }

    #region Helper attributes
    // ReSharper disable InconsistentNaming

    /// <summary> Type-safe non-optional <see cref="TerminalAttribute"/>. </summary>
    public class TAttribute : TerminalAttribute        
    {
        public TAttribute(params Tk[] read) : base(read.Select(t => (int)t)) {}
    }

    /// <summary> Type-safe optional <see cref="TerminalAttribute"/>. </summary>
    public class OAttribute : TerminalAttribute
    {
        public OAttribute(params Tk[] read) : base(read.Select(t => (int)t), true) { }
    }

    /// <summary> Type-safe <see cref="ListAttribute"/>. </summary>
    public class LAttribute : ListAttribute
    {
        public LAttribute(int maxRank = -1) : base(maxRank) { }

        public Tk Sep
        {
            get => (Tk)(Separator ?? 0);
            set => Separator = (int)value;
        }

        public Tk End
        {
            get => (Tk)(Terminator ?? 0);
            set => Terminator = (int)value;
        }
    }

    /// <summary> Shorthand notation for <see cref="NonTerminalAttribute"/>. </summary>    
    public class NTAttribute : NonTerminalAttribute
    {
        public NTAttribute(int maxRank = -1) : base(maxRank) { }
    }

    // ReSharper restore InconsistentNaming
    #endregion
}
