using System;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    [Serializable]
    public sealed class CodeRewriterRuleSet
    {
        [Tooltip("Regex to match Unity versions")]
        public string UnityVersion;
        public CodeRewriterRule[] Rules;
    }
}
