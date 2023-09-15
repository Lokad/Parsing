using System;
using Lokad.Parsing;

namespace Lokad.BrainScript
{
    /// <summary> Thrown when a script cannot be parsed. </summary>
    public class ParserException : Exception
    {
        public SourceSpan Location { get; }

        public ParserException(string message, SourceSpan s, Exception inner) 
            : base(s.Location.Line + ":" + s.Location.Column + " " + message, inner)
        {
            Location = s;
        }

        public ParserException(string message, SourceSpan s) : this(message, s, null)
        {
        }

    }
}