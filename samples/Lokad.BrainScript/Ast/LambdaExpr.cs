namespace Lokad.BrainScript.Ast
{
    /// <summary> An inline, single-argument function definition. </summary>
    public sealed class LambdaExpr : IExpr
    {
        public LambdaExpr(string parameter, IExpr body)
        {
            Parameter = parameter;
            Body = body;
        }

        /// <summary> The parameter name. </summary>
        public string Parameter { get; }

        /// <summary> The body of the expression. </summary>
        public IExpr Body { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            p.Write('(');
            p.Write(Parameter);
            p.Write(" => ");
            Body.PrintTo(p);
            p.Write(')');
        }
    }
}
