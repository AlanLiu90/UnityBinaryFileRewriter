#if TUANJIE_1_0_OR_NEWER

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.OpenHarmony;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    internal sealed class CodeRewriterOpenHarmony : IPreprocessBuildWithReport, IPostGenerateOpenHarmonyProject
    {
        private static readonly Dictionary<Architecture, string> mArchs = new Dictionary<Architecture, string>()
        {
            [Architecture.ARMv7] = "armeabi-v7a",
            [Architecture.ARM64] = "arm64-v8a",
            [Architecture.X86_64] = "x86_64",
        };

        public int callbackOrder => 100;

        private static bool mDevelopmentBuild;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != UnityEditor.BuildTarget.OpenHarmony)
                return;

            mDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
        }

        public void OnPostGenerateOpenHarmonyProject(string outputPath)
        {
            if (!Utility.ValidateEngineBinaryFileRewriterSettings())
                return;

            if (!Directory.Exists(outputPath))
                return;

            var textPattern = new Regex(@"^.*\.text.*$", RegexOptions.Multiline);

            foreach (var kv in mArchs)
            {
                var arch = kv.Value;

                var toolset = new ToolSetOpenHarmony(kv.Key);

                var libUnityPath = Path.Combine(outputPath, $"libs/{arch}/libtuanjie.so");

                if (!File.Exists(libUnityPath))
                    continue;

                var rules = Utility.GetCodeRewriteRules(BuildTarget.OpenHarmony, kv.Key, mDevelopmentBuild);
                if (!rules.Any())
                    continue;

                Debug.LogFormat("Start to process: {0}", libUnityPath);

                var tempLibUnityPath = libUnityPath + ".tmp";
                File.Copy(libUnityPath, tempLibUnityPath, true);

                var result = Utility.RunProcess(toolset.ReadElf, $"-S \"{libUnityPath}\"");

                var textMatch = textPattern.Match(result);
                var columns = textMatch.Groups[0].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var vma = int.Parse(columns[3], NumberStyles.HexNumber);
                var offset = int.Parse(columns[4], NumberStyles.HexNumber);

                var symbolFile = GetLibUnitySymbol(arch);
                var symbolText = Utility.RunProcess(toolset.ObjDump, $"--syms \"{symbolFile}\"");

                bool dirty = false;

                foreach (var (name, rule) in rules)
                {
                    Debug.LogFormat("Start to apply rule: {0}", name);

                    foreach (var symbol in rule.Symbols)
                    {
                        foreach (Match match in symbol.Match(symbolText))
                        {
                            string matchedLine = match.Groups[0].Value.Trim();
                            string symbolName = matchedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();

                            string demangledSymbolName = Utility.RunProcess(toolset.CppFilt, symbolName).Trim();
                            Debug.Log(demangledSymbolName);

                            if (symbol.DemangledName != demangledSymbolName)
                                continue;

                            Debug.LogFormat("Start to rewrite symbol: {0}", symbolName);

                            result = DisassembleSymbol(toolset, libUnityPath, kv.Key, matchedLine);

                            // Debug.Log(result);

                            int rewritedInstructions = 0;

                            foreach (var inst in symbol.Instructions)
                            {
                                Debug.LogFormat("Start to rewrite instruction: {0} {1}", inst.OriginalMachineCode, inst.OriginalInstructionDescription);

                                string line;
                                int address;

                                if (!toolset.TryGetInstruction(result, inst.OriginalMachineCode, inst.Index, out line, out address))
                                {
                                    Debug.LogFormat("Failed to find the instruction");
                                    continue;
                                }

                                var fileOffset = address - vma + offset;

                                // Debug.LogFormat("{0:X}", address);
                                // Debug.LogFormat("{0:X}, {1:X}", vma, offset);

                                using (var fs = File.Open(tempLibUnityPath, FileMode.Open, FileAccess.ReadWrite))
                                {
                                    fs.Seek(fileOffset, SeekOrigin.Begin);
                                    inst.Apply(fs);
                                }

                                Debug.LogFormat("Rewrited instruction: {0}", line);

                                rewritedInstructions++;
                                dirty = true;
                            }

                            if (rewritedInstructions > 0 && rewritedInstructions != symbol.Instructions.Length)
                                throw new Exception("Failed to rewrite all instructions");
                        }
                    }
                }

                if (dirty)
                {
                    File.Delete(libUnityPath);
                    File.Move(tempLibUnityPath, libUnityPath);

                    Debug.LogFormat("Rewrited {0}", libUnityPath);
                }
            }
        }

        private string GetLibUnitySymbol(string arch)
        {
            var path = EditorApplication.applicationContentsPath;

            if (Application.platform == RuntimePlatform.OSXEditor)
                path = Path.Combine(path, "../..");

            string type = mDevelopmentBuild ? "Development" : "Release";
            path = Path.Combine(path, $"PlaybackEngines/OpenHarmonyPlayer/Variations/il2cpp/{type}/Symbols/{arch}/libtuanjie.sym.so");

            return path;
        }

        private string DisassembleSymbol(ToolSetOpenHarmony toolset, string libUnityPath, Architecture architecture, string text)
        {
            string[] words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            var symbolAddress = words[0];
            var symbolSize = words[4];

            // Debug.Log(symbolAddress + " " + symbolSize);

            int address = int.Parse(symbolAddress, NumberStyles.HexNumber);
            int size = int.Parse(symbolSize, NumberStyles.HexNumber);

            return Utility.RunProcess(toolset.ObjDump, $"--start-address=0x{address:x} --stop-address=0x{address + size:x} -d \"{libUnityPath}\"");
        }
    }
}

#endif
