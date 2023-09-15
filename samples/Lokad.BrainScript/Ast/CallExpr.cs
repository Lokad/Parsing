using System;
using System.Collections.Generic;

namespace Lokad.BrainScript.Ast
{
    /// <summary> A function call expression. </summary>
    public sealed class CallExpr : IExpr
    {
        public CallExpr(IExpr function, IReadOnlyList<Arg> arguments, bool layerSyntax)
        {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            LayerSyntax = layerSyntax;
        }

        /// <summary> The function to call. </summary>
        public IExpr Function { get; }

        /// <summary> The function arguments. </summary>
        public IReadOnlyList<Arg> Arguments { get; }

        /// <summary> True if called with '{}' instead of '()'. </summary>
        public bool LayerSyntax { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <summary> An argument in a call expression. </summary>
        public struct Arg
        {
            public Arg(IExpr value, string label = null)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
                Label = label;
            }

            /// <summary> An optional label argument. </summary>
            public string Label { get; }

            /// <summary> The value of the argument. </summary>
            public IExpr Value { get; }
        }

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            var wrap = Function.Precedence < Precedence;

            if (wrap) p.Write('(');
            Function.PrintTo(p);
            if (wrap) p.Write(')');

            if (p.Pretty) p.Write(' ');

            p.Write(LayerSyntax ? '{' : '(');

            var first = true;
            foreach (var arg in Arguments)
            {
                if (first)
                    first = false;
                else
                    p.Write(p.Pretty ? ", " : ",");

                if (arg.Label != null)
                {
                    p.Write(arg.Label);
                    p.Write('=');
                }

                arg.Value.PrintTo(p);
            }

            p.Write(LayerSyntax ? '}' : ')');
        }

    }
}
