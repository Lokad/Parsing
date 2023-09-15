namespace Lokad.BrainScript.Ast
{
    /// <summary> Interface implemented by all expressions. </summary>
    public interface IExpr
    {
        /// <summary> The precedence level of this expression. </summary>
        /// <remarks>
        ///     Used when pretty-printing expressions, in order to only add parentheses
        ///     where they are needed. The idea is that it should not be necessary to 
        ///     add parentheses around an expression with a higher precedence.        
        /// </remarks>
        int Precedence { get; }

        /// <summary> Print the contents of this expression to the specified printer. </summary>
        void PrintTo(Printer p);
    }
}
