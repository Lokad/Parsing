namespace Lokad.BrainScript.Ast
{
    /// <summary> Assign to an identifier. </summary>
    public sealed class IdAssign : IAssign
    {
        public IdAssign(string name, IExpr assigned)
        {
            Name = name;
            Assigned = assigned;
        }

        /// <summary> The name of the assigned identifier. </summary>
        public string Name { get; }

        /// <summary> The assigned expression. </summary>
        public IExpr Assigned { get; }

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            if (Assigned is ArrayExpr asArray)
            {
                ArrayPrintTo(asArray, p);
                return;
            }

            p.Write(Name);
            p.Write(" = ");
            Assigned.PrintTo(p);
        }

        /// <summary>
        ///     Like <see cref="PrintTo"/>, but merges the child expression (which
        ///     is an array definition) into the assignment.
        /// </summary>
        /// <example>
        ///     <c>a = array [1..L] (l => e)</c> becomes <c>a[l:1..L] = e</c>
        /// </example>
        private void ArrayPrintTo(ArrayExpr e, Printer p)
        {
            p.Write(Name);
            p.Write('[');
            p.Write(e.Parameter);
            p.Write(':');
            e.Min.PrintTo(p);
            p.Write("..");
            e.Max.PrintTo(p);
            p.Write("] = ");
            e.Body.PrintTo(p);
        }
    }
}
