using System;
using System.Collections.Generic;

namespace Lokad.BrainScript.Ast
{
    /// <summary> A function definition (as an assignment). </summary>
    public sealed class FuncAssign : IAssign
    {
        public FuncAssign(string name, IReadOnlyList<Param> parameters, bool layerSyntax, IExpr body)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            LayerSyntax = layerSyntax;
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary> True if parameters are passed with '{}' instead of '()'. </summary>
        public bool LayerSyntax { get; }

        /// <summary> The name of the function. </summary>
        public string Name { get; }

        /// <summary> The parameters of the function. </summary>
        public IReadOnlyList<Param> Parameters { get; }

        /// <summary> The body of the function. </summary>
        public IExpr Body { get; }

        /// <summary> A parameter in a function definition. </summary>
        public struct Param
        {
            public Param(string name, LitExpr def = null)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Default = def;
            }

            /// <summary> The name of the parameter. </summary>
            public string Name { get; }

            /// <summary> Optional default value for the parameter. </summary>
            public LitExpr Default { get; }
        }

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            p.Write(Name);

            if (p.Pretty) p.Write(' ');
            p.Write(LayerSyntax ? '{' : '(');

            var first = true;
            foreach (var param in Parameters)
            {
                if (first) first = false;
                else p.Write(p.Pretty ? ", " : ",");

                p.Write(param.Name);

                if (param.Default != null)
                {
                    p.Write('=');
                    param.Default.PrintTo(p);
                }
            }

            p.Write(LayerSyntax ? '}' : ')');
            p.Write(" = ");

            var indent = p.Pretty 
                && !(Body is RecordExpr) 
                && !(Body is FieldExpr fe && fe.Record is RecordExpr);

            if (indent) p.LineBreak(true);
            Body.PrintTo(p);
            if (indent) p.Dedent();
        }
    }
}
