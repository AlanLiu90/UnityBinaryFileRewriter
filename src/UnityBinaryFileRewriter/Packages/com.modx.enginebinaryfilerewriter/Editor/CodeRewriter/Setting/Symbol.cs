using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    [Serializable]
    public sealed class Symbol
    {
        public string DemangledName;
        public string Pattern;
        public Instruction[] Instructions;

        public MatchCollection Match(string text)
        {
            var regex = new Regex(Pattern, RegexOptions.Multiline);
            return regex.Matches(text);
        }
    }
}
