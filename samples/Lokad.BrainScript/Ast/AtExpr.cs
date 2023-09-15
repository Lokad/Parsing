namespace Lokad.BrainScript.Ast
{
    /// <summary> Access an element in an array. </summary>
    public sealed class AtExpr : IExpr
    {
        public AtExpr(IExpr array, IExpr index)
        {
            Array = array;
            Index = index;
        }

        /// <summary> The expression that returns an array. </summary>
        public IExpr Array { get; }

        /// <summary> The expression that returns the index. </summary>
        public IExpr Index { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            var wrap = Array.Precedence < Precedence;

            if (wrap) p.Write('(');
            Array.PrintTo(p);
            if (wrap) p.Write(')');

            p.Write('[');
            Index.PrintTo(p);
            p.Write(']');
        }
    }
}
