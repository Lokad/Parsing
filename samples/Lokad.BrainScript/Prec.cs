using Lokad.BrainScript.Ast;

namespace Lokad.BrainScript
{
    /// <summary> Precedence values. </summary>
    public static class Prec
    {
        /// <summary> The highest possible precedence. Never needs parentheses. </summary>
        public const int Top = Unary + 1;

        /// <summary> Unary operator. </summary>
        public const int Unary = Conditional + 2 + BinaryOpExtensions.MaxPriority;

        /// <summary> Binary operators, when on the left. </summary>
        public static int Binary(BinaryOp op) => 
            Conditional + 1 + op.Priority();
        
        /// <summary> Conditional ternary operator. </summary>
        public const int Conditional = 0;
    }
}