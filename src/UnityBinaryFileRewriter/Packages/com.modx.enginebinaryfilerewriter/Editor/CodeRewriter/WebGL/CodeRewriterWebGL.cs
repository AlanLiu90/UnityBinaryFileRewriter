#if UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    internal sealed class CodeRewriterWebGL : IPreprocessBuildWithReport
    {
        public int callbackOrder => 100;

        private const string BackupExtension = ".bak";

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != UnityEditor.BuildTarget.WebGL)
                return;

            if (!Utility.ValidateEngineBinaryFileRewriterSettings())
                return;

            bool development = (report.summary.options & BuildOptions.Development) != 0;
            bool threadsSupport = PlayerSettings.WebGL.threadsSupport;
            bool optimizeForSize = UserBuildSettings.codeOptimization == WasmCodeOptimization.DiskSize ||
                UserBuildSettings.codeOptimization == WasmCodeOptimization.DiskSizeLTO;

            var modulesPath = Path.Combine(GetLibPath(), ModulesPathName(development, threadsSupport, optimizeForSize));

            var parameters = new Dictionary<string, object>()
            {
                ["Development"] = development,
                ["ThreadsSupport"] = threadsSupport,
                ["OptimizeForSize"] = optimizeForSize
            };

            var rules = Utility.GetCodeRewriteRules(BuildTarget.WebGL, parameters);
            if (!rules.Any())
            {
                RecoverOriginalFiles(modulesPath);
                return;
            }

            var files = new HashSet<string>();

            foreach (var (name, rule) in rules)
            {
                foreach (var symbol in rule.GetRule<PlatformCodeRewriterRuleWebGL>().Symbols)
                {
                    files.Add(symbol.FileName);
                }
            }

            BackupFiles(modulesPath, files);

            var fileBytes = new Dictionary<string, byte[]>();

            foreach (var (name, rule) in rules)
            {
                foreach (var symbol in rule.GetRule<PlatformCodeRewriterRuleWebGL>().Symbols)
                {
                    var fileName = symbol.FileName;

                    if (!fileBytes.TryGetValue(fileName, out var bytes))
                    {
                        var filePath = Path.Combine(modulesPath, fileName);
                        bytes = File.ReadAllBytes(filePath + BackupExtension);
                        fileBytes[fileName] = bytes;
                    }

                    foreach (var inst in symbol.Instructions)
                    {
                        var offsets = FindByteSequence(bytes, HexToByteArray(inst.OriginalMachineCode));

                        if (offsets.Count == 0)
                            throw new Exception($"Failed to find the original machine code for {inst.OriginalInstructionDescription} in file {symbol.FileName}");
                        else if (offsets.Count > 1 && inst.Index == -1)
                            throw new Exception($"Multiple occurrences of the original machine code found for {inst.OriginalInstructionDescription} in file {symbol.FileName}, but no specific index was provided.");
                        else if (offsets.Count > 1 && inst.Index >= offsets.Count)
                            throw new Exception($"Multiple occurrences of the original machine code found for {inst.OriginalInstructionDescription} in file {symbol.FileName}, but the provided index {inst.Index} is out of range.");

                        int targetIndex = inst.Index == -1 ? 0 : inst.Index;
                        Buffer.BlockCopy(HexToByteArray(inst.NewMachineCode), 0, bytes, offsets[targetIndex], inst.NewMachineCode.Length / 2);
                    }
                }
            }

            foreach (var kv in fileBytes)
            {
                var filePath = Path.Combine(modulesPath, kv.Key);

                var oldBytes = File.ReadAllBytes(filePath);

                if (kv.Value.SequenceEqual(oldBytes))
                {
                    Debug.LogFormat("No changes for {0}, skip rewriting.", kv.Key);
                    continue;
                }

                File.WriteAllBytes(filePath, kv.Value);

                Debug.LogFormat("Rewrited {0}", filePath);
            }
        }

        private static string GetLibPath()
        {
            string libPath = EditorApplication.applicationContentsPath;

            if (Application.platform == RuntimePlatform.OSXEditor)
                libPath = Path.Combine(libPath, "../..");

            libPath = Path.Combine(libPath, $"PlaybackEngines/WebGLSupport/BuildTools/lib");

            return libPath;
        }

        private static string ModulesPathName(bool development, bool threadsSupport, bool optimizeForSize)
        {
            string modules = "modules";
            if (development)
            {
                modules += "_development";
            }

            if (threadsSupport)
            {
                modules += "_mt";
            }

            if (!development && optimizeForSize)
            {
                modules += "_optsize";
            }

            return modules;
        }

        private static void RecoverOriginalFiles(string modulesPath)
        {
            var backupFiles = Directory.GetFiles(modulesPath, "*" + BackupExtension);

            foreach (var backupFile in backupFiles)
            {
                var originalFile = backupFile.Substring(0, backupFile.Length - BackupExtension.Length);
                if (File.Exists(originalFile))
                    File.Delete(originalFile);

                File.Move(backupFile, originalFile);

                Debug.LogFormat("Recovered {0}", originalFile);
            }
        }

        private static void BackupFiles(string modulesPath, HashSet<string> files)
        {
            foreach (var file in files)
            {
                var filePath = Path.Combine(modulesPath, file);
                if (!File.Exists(filePath))
                    throw new Exception($"Failed to find file: {filePath}");

                var backupFile = filePath + BackupExtension;
                if (!File.Exists(backupFile))
                {
                    File.Copy(filePath, backupFile);
                    Debug.LogFormat("Backed up {0}", filePath);
                }
            }
        }

        private static List<int> FindByteSequence(byte[] fileBytes, byte[] pattern)
        {
            List<int> foundOffsets = new List<int>();

            for (int i = 0; i <= fileBytes.Length - pattern.Length; i++)
            {
                bool isMatch = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (fileBytes[i + j] != pattern[j])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    foundOffsets.Add(i);
                }
            }

            return foundOffsets;
        }

        private static byte[] HexToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}

#endif
