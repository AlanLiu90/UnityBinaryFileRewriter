using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace EngineBinaryFileRewriter
{
    internal sealed class ToolSetAndroid : ToolSetBase
    {
        public string ObjDump { get; private set; }
        public string ReadElf {  get; private set; }
        public string Strip { get; private set; }
        public string ObjCopy { get; private set; }
        public string CppFilt { get; private set; }

        public ToolSetAndroid(Architecture architecture)
        {
            ObjDump = GetTool("objdump", architecture);
            ReadElf = GetTool("readelf", architecture);
            Strip = GetTool("strip", architecture);
            ObjCopy = GetTool("objcopy", architecture);
            CppFilt = GetTool("c++filt", architecture);
        }

        protected override string GetInstructionPattern(string machineCode)
        {
            string format;

#if UNITY_2022_1_OR_NEWER
            format = @"^\s+(\w+): {0} {1} {2} {3}\s+.*$";

            if (machineCode.Length < 8)
                machineCode += new string(' ', 8 - machineCode.Length);
#else
            var objDump = Path.GetFileName(ObjDump);

            if (objDump.StartsWith("arm-", StringComparison.Ordinal))
            {
                if (machineCode.Length < 8)
                    format = @"^\s+(\w+):\s+{1}{0}\s+.*$";
                else
                    format = @"^\s+(\w+):\s+{1}{0} {3}{2}\s+.*$";
            }
            else
            {
                Assert.IsTrue(objDump.StartsWith("aarch64-", StringComparison.Ordinal));
                format = @"^\s+(\w+):\s+{3}{2}{1}{0}\s+.*$";
            }
#endif

            var pattern = string.Format(format,
                Enumerable.Range(0, machineCode.Length / 2)
                    .Select(i => machineCode.Substring(i * 2, 2).ToLowerInvariant())
                    .ToArray());

            return pattern;
        }

        private static string GetTool(string tool, Architecture architecture)
        {
            string toolDir = EditorApplication.applicationContentsPath;

            if (Application.platform == RuntimePlatform.OSXEditor)
                toolDir = Path.Combine(toolDir, "../..");

            string llvmArch;
            if (Application.platform == RuntimePlatform.WindowsEditor)
                llvmArch = "windows-x86_64";
            else if (Application.platform == RuntimePlatform.OSXEditor)
                llvmArch = "darwin-x86_64";
            else
                llvmArch = "???";

            toolDir = Path.Combine(toolDir, $"PlaybackEngines/AndroidPlayer/NDK/toolchains/llvm/prebuilt/{llvmArch}/bin");

            string prefix;

            switch (architecture)
            {
                case Architecture.ARMv7:
                    prefix = "arm-linux-androideabi-";
                    break;

                case Architecture.ARM64:
                    prefix = "aarch64-linux-android-";
                    break;

                case Architecture.X86:
                    prefix = "i686-linux-android-";
                    break;

                case Architecture.X86_64:
                    prefix = "x86_64-linux-android-";
                    break;

                default:
                    throw new NotSupportedException(architecture.ToString());
            }

            var toolPath = Path.Combine(toolDir, prefix + tool);
            if (Application.platform == RuntimePlatform.WindowsEditor)
                toolPath += ".exe";

            if (!File.Exists(toolPath))
            {
                toolPath = Path.Combine(toolDir, GetLLVMTool(tool));
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    toolPath += ".exe";

                if (!File.Exists(toolPath))
                    throw new Exception($"Failed to find '{tool}'");
            }
            
            return toolPath;
        }

        private static string GetLLVMTool(string tool)
        {
            if (tool == "c++filt")
                return "llvm-cxxfilt";

            return "llvm-" + tool;
        }
    }
}
