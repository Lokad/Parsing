using System;
using System.Globalization;

namespace Lokad.BrainScript.Ast
{
    /// <summary> An expression that is a simple literal. </summary>
    /// <typeparam name="T"> string, boolean or floating-point </typeparam>
    public sealed class LitExpr<T> : LitExpr
    {
        public LitExpr(T value) { Value = value; }

        /// <summary> The literal value of this expression. </summary>
        public T Value { get; }

        /// <see cref="IExpr.PrintTo"/>
        public override void PrintTo(Printer p)
        {
            if (typeof(T) == typeof(string))
            {
                var str = (string) (object) Value;
                var sep = '\'';
                var wrap = false;
                if (str.Contains("'"))
                {
                    wrap = str.Contains("\"");
                    if (wrap) str = str.Replace("\"", "\"+'\"'+\"");

                    sep = '"';
                }

                if (wrap) p.Write('(');
                p.Write(sep);
                p.Write(str);
                p.Write(sep);
                if (wrap) p.Write(')');
            }
            else if (typeof(T) == typeof(float))
            {
                var num = (float) (object) Value;
                p.Write(num.ToString("G", CultureInfo.InvariantCulture));
            }
            else if (typeof(T) == typeof(bool))
            {
                var bit = (bool) (object) Value;
                p.Write(bit ? "true" : "false");
            }
            else throw new NotSupportedException();
        }
    }

    /// <summary> A type-erased literal expression. </summary>
    public abstract class LitExpr : IExpr
    {
        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        public abstract void PrintTo(Printer p);
    }
}
