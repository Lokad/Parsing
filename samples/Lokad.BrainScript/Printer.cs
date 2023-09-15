using System.Text;

namespace Lokad.BrainScript
{
    /// <summary> Used for printing out scripts. </summary>
    public sealed class Printer
    {
        /// <summary> Generated code is appended here. </summary>
        private readonly StringBuilder _sb = new StringBuilder();

        /// <summary> The current indent. </summary>
        private string _indent = "\n";

        public Printer(bool pretty = true)
        {
            Pretty = pretty;
        }

        /// <summary> Are we pretty-printing or just generating compact code ? </summary>
        public bool Pretty { get; }

        /// <summary> Print an arbitrary string. </summary>
        public void Write(string s) => _sb.Append(s);

        /// <summary> Append an arbitrary character. </summary>
        public void Write(char c) => _sb.Append(c);

        /// <summary> Insert a line break, possibly incrementing the indent. </summary>
        public void LineBreak(bool indent = false)
        {
            if (indent) _indent += "    ";
            _sb.Append(_indent);
        }

        /// <summary> Reduce the current indent. </summary>
        public void Dedent() => _indent = _indent.Substring(0, _indent.Length - 4);

        /// <summary> The string output of the printer. </summary>
        public override string ToString() => _sb.ToString();
    }
}
