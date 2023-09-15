namespace Lokad.BrainScript.Ast
{
    /// <summary> An identifier expression. </summary>
    public sealed class IdExpr : IExpr
    {
        public IdExpr(string name) { Name = name; }

        /// <summary> The name of the identifier. </summary>
        public string Name { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p) => p.Write(Name);
    }
}
