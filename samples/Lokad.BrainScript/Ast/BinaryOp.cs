using System;

namespace Lokad.BrainScript.Ast
{
    /// <summary> All allowed binary operations. </summary>
    public enum BinaryOp
    {
        Plus,
        Minus,
        Mult,
        Div,
        DotMult,
        And,
        Or,
        Lt,
        Gt,
        Eq,
        Leq,
        Geq,
        Neq
    }

    public static class BinaryOpExtensions
    {
        /// <summary> The priority of an operation. </summary>
        /// <remarks>
        ///     Since <c>a + b * c</c> is <c>a + (b * c)</c>, multiplication
        ///     has higher priority than addition.
        /// </remarks>
        public static int Priority(this BinaryOp op)
        {
            switch (op)
            {
                case BinaryOp.And:
                case BinaryOp.Or:
                    return 0;
                case BinaryOp.Lt:
                case BinaryOp.Gt:
                case BinaryOp.Eq:
                case BinaryOp.Leq:
                case BinaryOp.Geq:
                case BinaryOp.Neq:
                    return 1;
                case BinaryOp.Plus:
                case BinaryOp.Minus:
                    return 2;
                case BinaryOp.Mult:
                case BinaryOp.Div:
                case BinaryOp.DotMult:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        /// <summary> The maximum allowed priority. </summary>
        public const int MaxPriority = 3;
    }
}
