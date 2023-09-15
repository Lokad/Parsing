using Lokad.Parsing.Lexer;

namespace Lokad.BrainScript
{
    public sealed class FAttribute : FromAttribute
    {
        public FAttribute(Token parent, bool isPrivate = true) : base((int)parent, isPrivate) { }
    }

    /// <summary> A grammar token in BrainScript. </summary>
    [Tokens(Comments = "#[^\n]*")]
    public enum Token
    {
        [Error] Error,
        [End] EoS,
        
        [Any("{")] OpenBrace,
        [Any("}")] CloseBrace,
        [Any("(")] OpenParen,
        [Any(")")] CloseParen,
        [Any("[")] OpenBracket,
        [Any("]")] CloseBracket,
        [Any(";")] Semicolon,
        [Any(",")] Comma,
        [Any(":")] Colon,
        [Any(".")] Dot,
        [Any("..")] DotDot,
        [Any("=")] Assign,
        [Any("=>")] Lambda,
        
        [Any("!")] Not,
        [Any("+")] Plus,
        [Any("-")] Minus,
        [Any("*")] Mult,
        [Any("/")] Div,
        [Any(".*")] DotMult,
        [Any("&&")] And,
        [Any("||")] Or,
        [Any("<")] Lt,
        [Any(">")] Gt,
        [Any("==")] Eq,
        [Any("<=")] Leq,
        [Any(">=")] Geq,
        [Any("!=")] Neq,
        
        [Pattern("[a-zA-Z][a-zA-Z0-9_]*")] Id,

        [Ci, F(Id)] If,
        [Ci, F(Id)] Then,
        [Ci, F(Id)] Else,
        [Ci, F(Id)] True,
        [Ci, F(Id)] False,
        [Ci, F(Id)] Array,

        [Pattern("(\"[^\"]*\")|('[^']*')", Start = "'\"")] String,
        [PatternCi("[0-9]+(\\.[0-9]+)?(e[+-]?[0-9]+)?", Start = "0123456789")] Number
    }
}
