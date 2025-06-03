using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    internal sealed class ToolSetIOS : ToolSetBase
    {
        public string NM { get; private set; }
        public string AR { get; private set; }
        public string ObjDump { get; private set; }
        public string CppFilt { get; private set; }
        public string ReadElf { get; private set; }
#if !UNITY_2020_1_OR_NEWER
        public string Lipo { get; private set; }
#endif

        public ToolSetIOS()
        {
            NM = GetTool("llvm-nm");
            AR = GetTool("llvm-ar");
            ObjDump = GetTool("llvm-objdump");
            CppFilt = GetTool("llvm-cxxfilt");
            ReadElf = GetTool("llvm-readelf");

#if !UNITY_2020_1_OR_NEWER
            if (string.IsNullOrEmpty(Utility.RunProcess("which", "lipo")))
                throw new Exception("Failed to find 'lipo'");

            Lipo = "lipo";
#endif
        }

        protected override string GetInstructionPattern(string machineCode)
        {
            string format = @"^\s+(\w+): {0} {1} {2} {3}\s+.*$";

            var pattern = string.Format(format,
                Enumerable.Range(0, machineCode.Length / 2)
                    .Select(i => machineCode.Substring(i * 2, 2).ToLowerInvariant())
                    .ToArray());

            return pattern;
        }

        private static string GetTool(string tool)
        {
            string arch = "";

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    arch = "windows-x86_64";
                    break;

                case RuntimePlatform.OSXEditor:
                    arch = "darwin-x86_64";
                    break;

                case RuntimePlatform.LinuxEditor:
                    arch = "linux-x86_64";
                    break;

                default:
                    throw new NotSupportedException($"{Application.platform}");
            }

            string toolPath = Path.GetFullPath($"Packages/com.modx.enginebinaryfilerewriter/Editor/Tools~/{arch}/{tool}");

            if (Application.platform == RuntimePlatform.WindowsEditor)
                toolPath += ".exe";

            return toolPath;
        }
    }
}
