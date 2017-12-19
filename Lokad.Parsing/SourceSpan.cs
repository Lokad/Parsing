using System;

namespace Lokad.Parsing
{
    /// <summary> A position and length within the parsed script. </summary>
    public struct SourceSpan
    {
        /// <summary> The start position of this span. </summary>
        public SourceLocation Location { get; }

        /// <summary> The length of this span, in characters. </summary>
        public int Length { get; }

        public SourceSpan(SourceLocation location, int length)
        {
            Location = location;
            Length = length;
        }

        public override string ToString() => 
            $"{Location.Line}:{Location.Column}-{Location.Column + Length}";

        /// <summary>
        ///     True if the <see cref="SourceLocation.Position"/> and <see cref="Location"/> 
        ///     of <paramref name="other"/> represent an interval contained within or equal to
        ///     the interval represented by this span. 
        /// </summary>
        /// <remarks>
        ///     The <see cref="SourceLocation.Line"/> and <see cref="SourceLocation.Column"/>
        ///     are ignored for this test.
        /// </remarks>
        public bool EqualsOrContains(SourceSpan other)
        {
            var thisEnd = Location.Position + Length;
            var otherEnd = other.Location.Position + other.Length;
            return Location.Position <= other.Location.Position && otherEnd <= thisEnd;
        }

        /// <see cref="SourceLocation.ShiftColumn"/>
        public SourceSpan ShiftColumn(int by) =>
            new SourceSpan(Location.ShiftColumn(by), Length);

        /// <summary>
        ///     A span with the same <see cref="Location"/> but a different 
        ///     <see cref="Length"/>
        /// </summary>
        public SourceSpan WithLength(int newLength) =>
            new SourceSpan(Location, newLength);

        /// <summary> The smallest span that contains both of these spans. </summary>
        public SourceSpan MergeWith(SourceSpan other)
        {
            if (other.Location.Position < Location.Position)
                return other.MergeWith(this);

            var maxPos = Math.Max(
                other.Location.Position + other.Length,
                Location.Position + Length);

            return new SourceSpan(Location, maxPos - Location.Position);
        }

        #region Equality

        public bool Equals(SourceSpan other) => 
            Location.Equals(other.Location) && Length == other.Length;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SourceSpan span && Equals(span);
        }

        public override int GetHashCode() => 
            unchecked((Location.GetHashCode()*397) ^ Length);

        #endregion
    }
}