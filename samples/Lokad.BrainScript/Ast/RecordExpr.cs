using System.Collections.Generic;

namespace Lokad.BrainScript.Ast
{
    /// <summary> A record, consisting of assigned fields. </summary>
    public sealed class RecordExpr : IExpr
    {
        public RecordExpr(IReadOnlyList<IAssign> fields) { Fields = fields; }

        /// <summary> The fields in the record. </summary>
        public IReadOnlyList<IAssign> Fields { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Top;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            p.Write('{');
            if (p.Pretty) p.LineBreak(true);

            var first = true;
            foreach (var field in Fields)
            {
                if (first) first = false;
                else if (p.Pretty) p.LineBreak();
                else p.Write(';');

                field.PrintTo(p);
            }

            if (p.Pretty)
            {
                p.Dedent();
                p.LineBreak();
            }

            p.Write('}');
        }

        /// <summary> Print this *root* record expression as a string. </summary>
        public string Print(bool pretty)
        {
            var p = new Printer(pretty);
            foreach (var field in Fields)
            {
                field.PrintTo(p);
                p.LineBreak();
            }
            return p.ToString();
        }
    }
}
