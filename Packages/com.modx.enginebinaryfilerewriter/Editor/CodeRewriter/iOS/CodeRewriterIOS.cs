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

            bool development = (report.summary.options & BuildOptions.Development) != 0;
            var rules = Utility.GetCodeRewriteRules(BuildTarget.iOS, Architecture.ARM64, development);
            if (!rules.Any())
                return;

            string libiPhonePath = Path.Combine(report.summary.outputPath, "Libraries/libiPhone-lib.a");

            var toolset = new ToolSetIOS();

            Debug.Log("Start to process " + libiPhonePath);

            var originalLibPath = libiPhonePath;
            libiPhonePath = Path.Combine(report.summary.outputPath, "Libraries/libiPhone-lib.tmp.a");

            File.Copy(originalLibPath, libiPhonePath, true);

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
                        string demangledSymbolName = Utility.RunProcess(toolset.CppFilt, symbolName).Trim();

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

                        Utility.RunProcess(toolset.AR, $"xo \"{libiPhonePath}\" \"{objectFilePath}\"");

                        var result = Utility.RunProcess(toolset.OTool, $"-l \"{objectFilePath}\"");

                        var textSectionPattern = new Regex(@"sectname __text\n\s+segname __TEXT\n\s+addr\s+0x(\d+).*\n\s+size.*\n\s+offset\s+(\d+)");
                        var textSectionMatch = textSectionPattern.Match(result);
                        int textAddress = int.Parse(textSectionMatch.Groups[1].Value, NumberStyles.HexNumber);
                        int textOffset = int.Parse(textSectionMatch.Groups[2].Value);

                        bool dirty = false;

                        foreach (var symbolName in kv.Value)
                        {
                            Debug.LogFormat("Start to rewrite symbol: {0}", symbolName);

                            result = Utility.RunProcess(toolset.ObjDump, $"--disassemble-symbols={symbolName} \"{objectFilePath}\"");

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
                            var timestamp = new byte[12];
                            using (var fs = File.OpenRead(libiPhonePath))
                            {
                                fs.Seek(24, SeekOrigin.Begin);
                                fs.Read(timestamp, 0, timestamp.Length);
                            }

                            Utility.RunProcess("ar", $"r \"{libiPhonePath}\" \"{objectFilePath}\"");

                            using (var fs = File.OpenWrite(libiPhonePath))
                            {
                                fs.Seek(24, SeekOrigin.Begin);
                                fs.Write(timestamp, 0, timestamp.Length);
                            }
                        }

                        File.Delete(objectFilePath);
                    }
                }
            }

            var backupFile = originalLibPath + ".bak";
            if (File.Exists(backupFile))
                File.Delete(backupFile);

            File.Move(originalLibPath, backupFile);
            File.Move(libiPhonePath, originalLibPath);

            Debug.LogFormat("Rewrited {0}", originalLibPath);
        }
    }
}
