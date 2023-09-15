namespace Lokad.BrainScript.Ast
{
    /// <summary> A function-based one-dimensional array initialization. </summary>
    public sealed class ArrayExpr : IExpr
    {
        public ArrayExpr(IExpr min, IExpr max, string parameter, IExpr body)
        {
            Min = min;
            Max = max;
            Parameter = parameter;
            Body = body;
        }

        /// <summary> Lower bound of array indices (one-based). </summary>
        public IExpr Min { get; }

        /// <summary> Upper bound of array indices, inclusive. </summary>
        public IExpr Max { get; }

        /// <summary> The name of the lambda parameter. </summary>
        public string Parameter { get; }

        /// <summary> The body of the lambda. </summary>
        public IExpr Body { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            p.Write("array[");
            Min.PrintTo(p);
            p.Write("..");
            Max.PrintTo(p);
            p.Write("] (");
            p.Write(Parameter);
            p.Write(" => ");

            if (p.Pretty)
            {
                p.LineBreak(true);
                Body.PrintTo(p);
                p.Dedent();
            }
            else
            {
                Body.PrintTo(p);
            }
            
            p.Write(')');
        }
    }
}
