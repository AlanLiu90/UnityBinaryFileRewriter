using System;
using System.Linq;

namespace EngineBinaryFileRewriter
{
    internal sealed class ToolSetIOS : ToolSetBase
    {
        public string NM { get; private set; }
        public string AR { get; private set; }
        public string OTool { get; private set; }
        public string ObjDump { get; private set; }
        public string CppFilt { get; private set; }

        private readonly string mInstructionPattern;

        public ToolSetIOS()
        {
            if (string.IsNullOrEmpty(Utility.RunProcess("which", "nm")))
                throw new Exception("Failed to find 'nm'");

            if (string.IsNullOrEmpty(Utility.RunProcess("which", "ar")))
                throw new Exception("Failed to find 'ar'");

            if (string.IsNullOrEmpty(Utility.RunProcess("which", "otool")))
                throw new Exception("Failed to find 'otool'");

            if (string.IsNullOrEmpty(Utility.RunProcess("which", "objdump")))
                throw new Exception("Failed to find 'objdump'");

            if (string.IsNullOrEmpty(Utility.RunProcess("which", "c++filt")))
                throw new Exception("Failed to find 'c++filt'");

            NM = "nm";
            AR = "ar";
            OTool = "otool";
            ObjDump = "objdump";
            CppFilt = "c++filt";

            mInstructionPattern = @"^\s+(\w+): {0}{1}{2}{3}\s+.*$";
        }

        protected override string GetInstructionPattern(string machineCode)
        {
            var pattern = string.Format(mInstructionPattern,
                Enumerable.Range(0, machineCode.Length / 2)
                    .Select(i => machineCode.Substring(i * 2, 2).ToLowerInvariant())
                    .Reverse()
                    .ToArray());

            return pattern;
        }
    }
}
