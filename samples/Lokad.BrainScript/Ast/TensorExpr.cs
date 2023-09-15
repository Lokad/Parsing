using System.Collections.Generic;

namespace Lokad.BrainScript.Ast
{
    /// <summary> A tensor (a K-dimensional vector). </summary>
    public sealed class TensorExpr : IExpr
    {
        public TensorExpr(IReadOnlyList<IExpr> sizes) { Sizes = sizes; }

        /// <summary> Sizes of the tensor across each dimension. </summary>
        public IReadOnlyList<IExpr> Sizes { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            p.Write('(');

            var first = true;
            foreach (var size in Sizes)
            {
                if (first) first = false;
                else p.Write(':');

                size.PrintTo(p);
            }

            p.Write(')');
        }
    }
}
