using System;
using System.Diagnostics.Contracts;

namespace Lokad.Parsing
{
    /// <summary> A position within the parsed script. </summary>
    public struct SourceLocation
    {
        /// <summary> Position within the input file. </summary>
        public int Position { get; }
        
        /// <summary>Line number, 1-based.</summary>
        public int Line { get; }

        /// <summary>Source column number, 1-based.</summary>        
        public int Column { get; }

        public SourceLocation(int position, int line, int column)
        {
            Position = position;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"{Line}:{Column}";

        /// <summary> Shift the location by a given number of characters. </summary>
        /// <remarks> Assumes that the location's start remains on the same line. </remarks>
        [Pure]
        public SourceLocation ShiftColumn(int by)
        {
            if (Column + by < 1)
                throw new ArgumentException($"Invalid shift (column {Column}, shift {by})", nameof(by));

            return new SourceLocation(Position + by, Line, Column + by);
        }

        #region Equality

        public bool Equals(SourceLocation other) => 
            Position == other.Position && Line == other.Line && Column == other.Column;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SourceLocation location && Equals(location);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position;
                hashCode = (hashCode*397) ^ Line;
                hashCode = (hashCode*397) ^ Column;
                return hashCode;
            }
        }

        #endregion
    }
}