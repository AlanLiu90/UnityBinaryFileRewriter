#if TUANJIE_1_0_OR_NEWER

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    internal sealed class ToolSetOpenHarmony : ToolSetBase
    {
        public string ObjDump { get; private set; }
        public string ReadElf { get; private set; }
        public string Strip { get; private set; }
        public string ObjCopy { get; private set; }
        public string CppFilt { get; private set; }

        public ToolSetOpenHarmony(Architecture architecture)
        {
            var toolDir = GetToolDir();

            ObjDump = GetTool("llvm-objdump", toolDir);
            ReadElf = GetTool("llvm-readelf", toolDir);
            Strip = GetTool("llvm-strip", toolDir);
            ObjCopy = GetTool("llvm-objcopy", toolDir);
            CppFilt = GetTool("llvm-cxxfilt", toolDir);
        }

        protected override string GetInstructionPattern(string machineCode)
        {
            string format = @"^\s+(\w+): {3}{2}{1}{0}\s+.*$";

            var pattern = string.Format(format,
                Enumerable.Range(0, machineCode.Length / 2)
                    .Select(i => machineCode.Substring(i * 2, 2).ToLowerInvariant())
                    .ToArray());

            return pattern;
        }

        private static string GetToolDir()
        {
            string toolDir = EditorApplication.applicationContentsPath;

            if (Application.platform == RuntimePlatform.OSXEditor)
                toolDir = Path.Combine(toolDir, "../..");

            toolDir = Path.Combine(toolDir, $"PlaybackEngines/OpenHarmonyPlayer/SDK");

            toolDir = Directory.GetDirectories(toolDir)
                .Where(x => int.TryParse(Path.GetFileName(x), out _))
                .OrderBy(x => Path.GetFileName(x))
                .FirstOrDefault();

            if (string.IsNullOrEmpty(toolDir))
                throw new Exception("Failed to find valid sdk directory");

            toolDir = Path.Combine(toolDir, "native/llvm/bin");
            return toolDir;
        }

        private static string GetTool(string tool, string toolDir)
        {
            var toolPath = Path.Combine(toolDir, tool);
            if (Application.platform == RuntimePlatform.WindowsEditor)
                toolPath += ".exe";

            if (!File.Exists(toolPath))
                throw new Exception($"Failed to find '{tool}'");

            return toolPath;
        }
    }
}

#endif
