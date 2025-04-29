using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Assertions;

namespace EngineBinaryFileRewriter
{
    internal sealed class CodeRewriterIOS : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
                return;

            if (PlayerSettings.iOS.sdkVersion != iOSSdkVersion.DeviceSDK)
                return;

            if (!Utility.ValidateEngineBinaryFileRewriterSettings())
                return;

#if UNITY_2020_1_OR_NEWER
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            var rules = Utility.GetCodeRewriteRules(BuildTarget.iOS, Architecture.ARM64, development);
            if (!rules.Any())
                return;

            var toolset = new ToolSetIOS();

            string libiPhonePath = Path.Combine(report.summary.outputPath, "Libraries/libiPhone-lib.a");
            Debug.Log("Start to process " + libiPhonePath);

            var originalLibPath = libiPhonePath;
            libiPhonePath = Path.Combine(report.summary.outputPath, "Libraries/libiPhone-lib.tmp.a");

            File.Copy(originalLibPath, libiPhonePath, true);

            ModifyLibrary(toolset, Architecture.ARM64, rules, libiPhonePath);
#else
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            var armv7Rules = Utility.GetCodeRewriteRules(BuildTarget.iOS, Architecture.ARMv7, development);
            var arm64Rules = Utility.GetCodeRewriteRules(BuildTarget.iOS, Architecture.ARM64, development);
            if (!armv7Rules.Any() && !arm64Rules.Any())
                return;

            var toolset = new ToolSetIOS();

            string libiPhonePath = Path.Combine(report.summary.outputPath, "Libraries/libiPhone-lib.a");
            Debug.Log("Start to process " + libiPhonePath);

            string armv7Path = libiPhonePath.Replace(".a", "-armv7.a");
            string arm64Path = libiPhonePath.Replace(".a", "-arm64.a");

            ExtractThinLibrary(toolset, libiPhonePath, "armv7", armv7Path);
            ExtractThinLibrary(toolset, libiPhonePath, "arm64", arm64Path);

            ModifyLibrary(toolset, Architecture.ARMv7, armv7Rules, armv7Path);
            ModifyLibrary(toolset, Architecture.ARM64, arm64Rules, arm64Path);

            var originalLibPath = libiPhonePath;
            libiPhonePath = Path.Combine(report.summary.outputPath, "Libraries/libiPhone-lib.tmp.a");

            Utility.RunProcess(toolset.Lipo, $"-create {armv7Path} {arm64Path} -output {libiPhonePath}");
#endif

            var backupFile = originalLibPath + ".bak";
            if (File.Exists(backupFile))
                File.Delete(backupFile);

            File.Move(originalLibPath, backupFile);
            File.Move(libiPhonePath, originalLibPath);

            Debug.LogFormat("Rewrited {0}", originalLibPath);
        }

        private void ModifyLibrary(ToolSetIOS toolset, Architecture architecture, IEnumerable<(string, CodeRewriterRule)> rules, string libiPhonePath)
        {
            if (!rules.Any())
                return;

            var library = StaticLibrary.Parse(libiPhonePath);

            string libDir = Path.GetDirectoryName(libiPhonePath);

            var symbolText = Utility.RunProcess(libDir, toolset.NM, $"-o \"{Path.GetFileName(libiPhonePath)}\"");

            foreach (var (name, rule) in rules)
            {
                Debug.LogFormat("Start to apply rule: {0}", name);

                foreach (var symbol in rule.Symbols)
                {
                    // .o -> symbols
                    var objectFiles = new Dictionary<string, List<string>>();

                    foreach (Match match in symbol.Match(symbolText))
                    {
                        if (match.Groups.Count != 2)
                            throw new Exception($"Regex should have one group: {symbol.Pattern}");

                        var symbolName = match.Groups[0].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Last();
                        var demangledSymbolName = Utility.RunProcess(toolset.CppFilt, $"--strip-underscore {symbolName}").Trim();

                        if (symbol.DemangledName != demangledSymbolName)
                            continue;

                        var objectFile = match.Groups[1].Value;

                        if (!objectFiles.TryGetValue(objectFile, out var list))
                        {
                            list = new List<string>();
                            objectFiles.Add(objectFile, list);
                        }

                        list.Add(symbolName);
                    }

                    foreach (var kv in objectFiles)
                    {
                        var objectFile = kv.Key;
                        var objectFilePath = Path.Combine(libDir, objectFile);

                        Debug.LogFormat("Start to rewrite object file: {0}", objectFile);

                        Utility.RunProcess(libDir, toolset.AR, $"xo \"{libiPhonePath}\" \"{objectFile}\"");

                        var result = Utility.RunProcess(toolset.ReadElf, $"-S \"{objectFilePath}\"");

                        var textSectionPattern = new Regex(@"Name: __text.*\n\s+Segment: __TEXT.*\n\s+Address:\s+0x(\d+).*\n\s+Size:.*\n\s+Offset:\s+(\d+)");
                        var textSectionMatch = textSectionPattern.Match(result);
                        int textAddress = int.Parse(textSectionMatch.Groups[1].Value, NumberStyles.HexNumber);
                        int textOffset = int.Parse(textSectionMatch.Groups[2].Value);

                        bool dirty = false;

                        foreach (var symbolName in kv.Value)
                        {
                            Debug.LogFormat("Start to rewrite symbol: {0}", symbolName);

                            string arguments = $"--disassemble-symbols={symbolName} \"{objectFilePath}\"";
                            if (architecture == Architecture.ARMv7)
                                arguments += " --mattr=+armv7s";

                            result = Utility.RunProcess(toolset.ObjDump, arguments);

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

                                var fileOffset = address - textAddress + textOffset;

                                var fileInfo = new FileInfo(objectFilePath);
                                var lastWriteTime = fileInfo.LastWriteTimeUtc;

                                using (var fs = File.Open(objectFilePath, FileMode.Open, FileAccess.ReadWrite))
                                {
                                    fs.Seek(fileOffset, SeekOrigin.Begin);
                                    inst.Apply(fs);
                                }

                                fileInfo.LastWriteTimeUtc = lastWriteTime;

                                Debug.LogFormat("Rewrited instruction: {0}", line);

                                rewritedInstructions++;
                                dirty = true;
                            }

                            if (rewritedInstructions > 0 && rewritedInstructions != symbol.Instructions.Length)
                                throw new Exception("Failed to rewrite all instructions");
                        }

                        if (dirty)
                        {
                            using (var fs = File.OpenWrite(libiPhonePath))
                            {
                                var bytes = File.ReadAllBytes(objectFilePath);
                                var item = library.GetItem(objectFile);
                                Assert.AreEqual(item.Length, bytes.Length);

                                fs.Seek(item.Offset, SeekOrigin.Begin);
                                fs.Write(bytes, 0, bytes.Length);
                            }

                            Debug.LogFormat("Replaced {0}", objectFilePath);
                        }

                        File.Delete(objectFilePath);
                    }
                }
            }
        }

#if !UNITY_2020_1_OR_NEWER
        private static void ExtractThinLibrary(ToolSetIOS toolset, string path, string arch, string output)
        {
            string temp = output + ".tmp";
            Utility.RunProcess(toolset.Lipo, $"-extract {arch} {path} -output {temp}");
            Utility.RunProcess(toolset.Lipo, $"-thin {arch} {temp} -output {output}");
            File.Delete(temp);
        }
#endif
    }
}
