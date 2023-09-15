namespace Lokad.BrainScript.Ast
{
    /// <summary> Access a field in a record. </summary>
    public sealed class FieldExpr : IExpr
    {
        public FieldExpr(IExpr record, string field)
        {
            Record = record;
            Field = field;
        }

        /// <summary> The expression that evaluates to a record. </summary>
        public IExpr Record { get; }

        /// <summary> The name of the field. </summary>
        public string Field { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            var wrap = Record.Precedence < Precedence;

            if (wrap) p.Write('(');
            Record.PrintTo(p);
            if (wrap) p.Write(')');

            p.Write('.');
            p.Write(Field);
        }
    }
}
