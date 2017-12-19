using System;
using JetBrains.Annotations;

namespace Lokad.Parsing.Parser
{
    [MeansImplicitUse]
    public abstract class ContextAttribute : Attribute
    {
        public abstract int GetTag(Type t);
    }
}
