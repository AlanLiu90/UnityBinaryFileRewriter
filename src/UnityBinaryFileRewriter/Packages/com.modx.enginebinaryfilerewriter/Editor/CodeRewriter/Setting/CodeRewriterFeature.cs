using System;

namespace EngineBinaryFileRewriter
{
    [Serializable]
    public sealed class CodeRewriterFeature
    {
        public string Name;
        public bool Enable;
        public CodeRewriterRuleSet[] RuleSets;
    }
}
