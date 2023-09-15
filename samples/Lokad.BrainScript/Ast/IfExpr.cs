namespace Lokad.BrainScript.Ast
{
    /// <summary> Ternary conditional expression. </summary>
    public sealed class IfExpr : IExpr
    {
        public IfExpr(IExpr condition, IExpr ifTrue, IExpr ifFalse)
        {
            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }

        /// <summary> Boolean condition. </summary>
        public IExpr Condition { get; }

        /// <summary> Returned if condition is true. </summary>
        public IExpr IfTrue { get; }

        /// <summary> Returned if condition is false. </summary>
        public IExpr IfFalse { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Conditional;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            if (p.Pretty) p.LineBreak(true);

            p.Write("if ");
            Condition.PrintTo(p);

            if (p.Pretty) p.LineBreak(); 
            else p.Write(' ');

            p.Write("then ");
            IfTrue.PrintTo(p);

            if (p.Pretty) p.LineBreak();
            else p.Write(' ');

            p.Write("else ");
            IfFalse.PrintTo(p);

            if (p.Pretty) p.Dedent();
        }

    }
}
