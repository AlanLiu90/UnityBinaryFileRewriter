using System;
using UnityEditor;

namespace EngineBinaryFileRewriter
{
    [Serializable]
    public sealed class CodeRewriterRule
    {
        public BuildTarget BuildTarget;
        public bool Development;
        public Architecture Architecture;
        public Symbol[] Symbols;
    }
}
