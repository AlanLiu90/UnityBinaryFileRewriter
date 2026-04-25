using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    internal static class Utility
    {
        public static string RunProcess(string fileName, string args)
        {
            return RunProcess(Directory.GetCurrentDirectory(), fileName, args);
        }

        public static string RunProcess(string workingDirectory, string fileName, string args)
        {
            UnityEngine.Debug.LogFormat("RunProcess {0} {1}", fileName, args);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.CreateNoWindow = true;

                var output = new StringBuilder();
                process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                    }
                });

                var error = new StringBuilder();
                process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                    }
                });

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception("RunProcess failed: " + error.ToString());

                return output.ToString();
            }
        }

        public static string GetUnityVersion()
        {
#if TUANJIE_1_0_OR_NEWER
            return Application.tuanjieVersion;
#else
            return Application.unityVersion;
#endif
        }

        public static IEnumerable<(string, CodeRewriterRule)> GetCodeRewriteRules(BuildTarget buildTarget, Architecture architecture, bool development)
        {
            var parameters = new Dictionary<string, object>()
            {
                ["Architecture"] = architecture,
                ["Development"] = development,
            };

            return GetCodeRewriteRules(buildTarget, parameters);
        }

        public static IEnumerable<(string, CodeRewriterRule)> GetCodeRewriteRules(BuildTarget buildTarget, Dictionary<string, object> parameters)
        {
            var unityVersion = GetUnityVersion();
            var features = EngineBinaryFileRewriterSettings.Instance.CodeRewriterFeatures;

            if (features == null)
                yield break;

            foreach (var feature in features)
            {
                if (!feature.Enable)
                    continue;

                if (feature.RuleSets == null)
                    continue;

                var ruleSet = feature.RuleSets.Where(x => Regex.IsMatch(unityVersion, x.UnityVersion) ).FirstOrDefault();
                if (ruleSet == null)
                    continue;

                if (ruleSet.Rules == null)
                    continue;

                var rule = ruleSet.Rules
                    .Where(x => x.BuildTarget == buildTarget && x.Rule != null && x.Rule.Match(parameters))
                    .FirstOrDefault();

                if (rule != null && rule.IsValid)
                    yield return (feature.Name, rule);
            }
        }

        public static bool ValidateEngineBinaryFileRewriterSettings()
        {
            if (ValidateEngineBinaryFileRewriterSettings(out var errorMessage))
                return true;

            UnityEngine.Debug.LogError(errorMessage);
            return false;
        }

        public static bool ValidateEngineBinaryFileRewriterSettings(out string errorMessage)
        {
            errorMessage = "";

            var sb = new StringBuilder();
            var errors = new List<string>();

            var settings = EngineBinaryFileRewriterSettings.Instance;

            if (settings.CodeRewriterFeatures != null)
            {
                foreach (var feature in settings.CodeRewriterFeatures)
                {
                    errors.Clear();

                    if (string.IsNullOrEmpty(feature.Name))
                        errors.Add("Feature's Name is empty");

                    if (feature.RuleSets == null || feature.RuleSets.Length == 0)
                    {
                        errors.Add("Feature's RuleSets is empty");
                        continue;
                    }

                    foreach (var ruleSet in feature.RuleSets)
                    {
                        if (string.IsNullOrEmpty(ruleSet.UnityVersion))
                            errors.Add("RuleSet's UnityVersion is empty");

                        foreach (var rule in ruleSet.Rules)
                        {
                            rule.Rule.Validate(errors);
                        }
                    }

                    if (errors.Count > 0)
                    {
                        sb.AppendFormat("Errors in the feature ({0})\n", feature.Name);

                        foreach (var error in errors)
                        {
                            sb.AppendFormat("  {0}", error);
                            sb.AppendLine();
                        }
                    }
                }
            }

            if (sb.Length > 0)
                errorMessage = sb.ToString();

            return errorMessage.Length == 0;
        }

        public static bool ValidateMachineCode(string code, Architecture architecture)
        {
            bool isValid;

            switch (architecture)
            {
                case Architecture.ARMv7:
                    isValid = code.Length == 4 || code.Length == 8;
                    break;

                case Architecture.ARM64:
                    isValid = code.Length == 8;
                    break;

                case Architecture.X86:
                case Architecture.X86_64:
                    isValid = code.Length >= 2 && code.Length <= 30 && code.Length % 2 == 0;
                    break;

                case Architecture.WASM:
                    isValid = code.Length >= 2 && code.Length % 2 == 0;
                    break;

                default:
                    isValid = false;
                    break;
            }

            if (isValid)
            {
                for (int i = 0; i < code.Length / 2; i++)
                {
                    if (!byte.TryParse(code.Substring(i * 2, 2), NumberStyles.HexNumber, null, out _))
                    {
                        isValid = false;
                        break;
                    }    
                }
            }

            return isValid;
        }
    }
}
