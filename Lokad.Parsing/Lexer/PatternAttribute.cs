using System;
using System.Text.RegularExpressions;

namespace Lokad.Parsing.Lexer
{
    /// <summary> A token that matches a regular expression. </summary>
    /// <remarks>
    ///     This attribute should be placed on a member of the token <c>enum</c>. It 
    ///     specifies a regular expression pattern, which will be used by 
    ///     the <see cref="TokenReader{TTok}"/> to recognize that member.
    /// </remarks>
    public class PatternAttribute : Attribute
    {
        /// <summary> The pattern. </summary>
        /// <remarks> 
        ///     Will be used as a regular expression. 
        /// 
        ///     A <c>"^"</c> will be prepended, if none is found. 
        /// </remarks>
        public string Pattern { get; }

        /// <summary> The characters with which this pattern can start. </summary>
        /// <remarks> 
        ///     This is used as an optimization. Starting the regular expression engine is
        ///     time-consuming, so the <see cref="TokenReader{TTok}"/> will only attempt
        ///     to match the pattern if the first character of the text to be matched
        ///     appears in <see cref="Start"/>.
        /// </remarks>
        public string Start { get; set; }

        /// <summary> Is this pattern case-sensitive ? </summary>
        /// <remarks> Default is true. </remarks>
        public bool CaseSensitive { get; set; } = true;

        public PatternAttribute(string pattern)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        }

        /// <summary> Converts attribute to <see cref="TokenDefinition"/>. </summary>
        public TokenDefinition ToDefinition()
        {
            var flags = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            if (!CaseSensitive) flags = flags | RegexOptions.IgnoreCase;

            var csPattern = Pattern.StartsWith("\\G") ? Pattern : $"\\G({Pattern})";
            
            return new TokenDefinition(new Regex(csPattern, flags), startsWith: Start);
        }
    }

    /// <summary> As <see cref="PatternAttribute"/> but case-insensitive by default. </summary>
    public sealed class PatternCiAttribute : PatternAttribute
    {
        public PatternCiAttribute(string pattern) : base(pattern)
        {
            CaseSensitive = false;
        }
    }
}
