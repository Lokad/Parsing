using System;

namespace Lokad.BrainScript.Ast
{
    /// <summary> An infix binary operation. </summary>
    public sealed class BinaryExpr : IExpr
    {
        public BinaryExpr(IExpr left, BinaryOp op, IExpr right)
        {
            Left = left;
            Right = right;
            Operator = op;
            Precedence = Prec.Binary(op);
        }

        /// <summary> Left operand. </summary>
        public IExpr Left { get; }

        /// <summary> Right operand. </summary>
        public IExpr Right { get; }

        /// <summary> The binary operator. </summary>
        public BinaryOp Operator { get; }

        /// <see cref="IExpr.Precedence"/>
        public int Precedence { get; }

        /// <see cref="IExpr.PrintTo"/>
        public void PrintTo(Printer p)
        {
            var wrapl = Left.Precedence < Precedence;
            if (wrapl) p.Write('(');
            Left.PrintTo(p);
            if (wrapl) p.Write(')');

            if (p.Pretty) p.Write(' ');

            switch (Operator)
            {
                case BinaryOp.Plus: p.Write('+'); break;
                case BinaryOp.Minus: p.Write('-'); break;
                case BinaryOp.Mult: p.Write('*'); break;
                case BinaryOp.Div: p.Write('/'); break;
                case BinaryOp.DotMult: p.Write(".*"); break;
                case BinaryOp.And: p.Write("&&"); break;
                case BinaryOp.Or: p.Write("||"); break;
                case BinaryOp.Lt: p.Write('<'); break;
                case BinaryOp.Gt: p.Write('>'); break;
                case BinaryOp.Eq: p.Write("=="); break;
                case BinaryOp.Leq: p.Write("<="); break;
                case BinaryOp.Geq: p.Write(">="); break;
                case BinaryOp.Neq: p.Write("!="); break;
                default: throw new ArgumentOutOfRangeException();
            }

            if (p.Pretty) p.Write(' ');

            // <= due to left-associativity
            var wrapr = Right.Precedence <= Precedence;
            if (wrapr) p.Write('(');
            Right.PrintTo(p);
            if (wrapr) p.Write(')');
        }
    }
}
