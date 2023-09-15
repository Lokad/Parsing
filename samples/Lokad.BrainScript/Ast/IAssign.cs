namespace Lokad.BrainScript.Ast
{
    /// <summary> An assignment to a field. </summary>
    public interface IAssign
    {
        /// <see cref="IExpr.PrintTo"/>
        void PrintTo(Printer p);
    }
}
