namespace Lokad.Parsing.Parser
{
    /// <summary> A parsed value and its position. </summary>
    public struct Pos<T>
    {
        public Pos(T value, SourceSpan location)
        {
            Value = value;
            Location = location;
        }

        public T Value { get; }

        public SourceSpan Location { get; }
    }
}