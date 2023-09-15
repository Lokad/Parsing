using System;

namespace Lokad.BrainScript.Ast
{
    /// <summary> Unary expression. </summary>
    public sealed class UnaryExpr : IExpr
    {
        public UnaryExpr(UnaryOp operation, IExpr operand)
        {
            Operation = operation;
            Operand = operand;
        }

        /// <summary> The applied unary operation. </summary>
        public UnaryOp Operation { get; }

        /// <summary> The operand. </summary>
        public IExpr Operand { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence => Prec.Unary;

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            var wrap = Operand.Precedence < Precedence;

            switch (Operation)
            {
                case UnaryOp.Not: p.Write('!'); break;
                case UnaryOp.Minus: p.Write('-'); break;
                default: throw new ArgumentOutOfRangeException();
            }

            if (wrap) p.Write('(');
            Operand.PrintTo(p);
            if (wrap) p.Write(')');
        }
    }
}
