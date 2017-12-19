using System;

namespace Lokad.Parsing.Parser
{
    /// <summary> A type and rank. Each rule has one. </summary>
    public struct RankedType
    {
        /// <summary> The type returned by the rule's method. </summary>
        public readonly Type Type;

        /// <summary> The rank of the rule. </summary>
        /// <remarks> Used by <see cref="NonTerminalAttribute.MaxRank"/>. </remarks>
        public readonly int Rank;

        public RankedType(Type type, int rank = 0)
        {
            Type = type;
            Rank = rank;
        }

        public override string ToString() =>
            Type.Name + (Rank == 0 ? "" : ":" + Rank);

        #region Equality

        private bool Equals(RankedType other)
        {
            return Type == other.Type && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RankedType && Equals((RankedType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.GetHashCode()*397) ^ Rank;
            }
        }

        #endregion
    }
}