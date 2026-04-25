using System;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    [Serializable]
    public sealed class CodeRewriterRule
    {
        public bool IsValid => Rule != null && Rule.IsValid;

        public BuildTarget BuildTarget;

        [SerializeReference]
        public PlatformCodeRewriter Rule;

        public T GetRule<T>() where T : PlatformCodeRewriter
        {
            return (T)Rule;
        }

        public string GetTargetString()
        {
            return $"{BuildTarget}+{Rule.GetTargetString()}";
        }
    }
}
