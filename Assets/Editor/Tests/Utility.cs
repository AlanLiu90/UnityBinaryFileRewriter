using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EngineBinaryFileRewriter;
using HybridCLR.Editor.Commands;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class Utility
{
#if UNITY_EDITOR_WIN
    private const string Cmp = "C:\\Program Files\\Git\\usr\\bin\\cmp.exe";
#else
    private const string Cmp = "cmp";
#endif

    public static readonly Dictionary<Architecture, string> AndroidArchitectures = new Dictionary<Architecture, string>()
    {
        [Architecture.ARMv7] = "armeabi-v7a",
        [Architecture.ARM64] = "arm64-v8a",
        [Architecture.X86] = "x86",
        [Architecture.X86_64] = "x86_64",
    };

    public static void EnableFeature(string name)
    {
        var feature = EngineBinaryFileRewriterSettings.Instance.CodeRewriterFeatures
            .Where(x => x.Name == name)
            .First();

        feature.Enable = true;

        foreach (var f in EngineBinaryFileRewriterSettings.Instance.CodeRewriterFeatures)
        {
            if (f != feature)
                f.Enable = false;
        }

        EngineBinaryFileRewriterSettings.Save();
    }

    public static void ExtractThinLibrary(string path, string arch, string output)
    {
        string temp = output + ".tmp";
        RunProcess("lipo", $"-extract {arch} {path} -output {temp}");
        RunProcess("lipo", $"-thin {arch} {temp} -output {output}");
        File.Delete(temp);
    }

    public static void CompareFiles(string file1, string file2, (int, int)[] expectedDiffs)
    {
        Assert.AreEqual(new FileInfo(file1).Length, new FileInfo(file2).Length);

        var diffs = GetDiffs(file1, file2).ToArray();

        Assert.AreEqual(expectedDiffs.Length, diffs.Length);

        int i = 0;
        foreach (var diff in diffs)
        {
            Assert.AreEqual(expectedDiffs[i].Item1, diff.Item1);
            Assert.AreEqual(expectedDiffs[i].Item2, diff.Item2);

            i++;
        }
    }

    private static IEnumerable<(int, int)> GetDiffs(string file1, string file2)
    {
        var (exitCode, output, error) = RunProcess(Cmp, $"-l \"{file1}\" \"{file2}\"");

        if (exitCode == 0)
        {
            yield break;
        }
        else if (exitCode == 1)
        {
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var columns = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                var byte1 = Convert.ToInt32(columns[1], 8);
                var byte2 = Convert.ToInt32(columns[2], 8);
                yield return (byte1, byte2);
            }
        }
        else
        {
            throw new Exception("RunProcess failed: " + error.ToString());
        }
    }

    public static void Unzip(string fileName, string output)
    {
        var executableName = Application.platform == RuntimePlatform.WindowsEditor ? "7z.exe" : "7za";
        var zipFileName = Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "Tools", executableName));

        if (Directory.Exists(output))
            Directory.Delete(output, true);

        var result = RunProcess(zipFileName, string.Format("x -o\"{0}\" \"{1}\"", output, fileName));
        if (result.Item1 != 0)
            throw new Exception(result.Item3.ToString());
    }

    public static void BuildAndroid(string output, bool development, bool stripEngineCode, string feature, AndroidArchitecture architectures)
    {
        BuildTarget target = BuildTarget.Android;
        BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (activeTarget != BuildTarget.Android)
        {
            Assert.Fail("Switch to Android platform before running the test");
            return;
        }

        if (output.EndsWith(".apk", StringComparison.Ordinal))
        {
            if (File.Exists(output))
                File.Delete(output);

            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        }
        else
        {
            if (Directory.Exists(output))
                Directory.Delete(output, true);

            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        }

        PlayerSettings.Android.targetArchitectures = architectures;
        PlayerSettings.stripEngineCode = stripEngineCode;

        var buildOptions = BuildOptions.CompressWithLz4;
        if (development)
            buildOptions |= BuildOptions.Development;

        PrebuildCommand.GenerateAll();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = new string[] { "Assets/Scenes/main.unity" },
            locationPathName = output,
            options = buildOptions,
            target = target,
            targetGroup = BuildTargetGroup.Android,
        };

        EnableFeature(feature);

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Assert.Fail("Failed to build");
            return;
        }
    }

    public static void BuildIOS(string output, bool development, string feature)
    {
        if (Application.platform != RuntimePlatform.OSXEditor)
        {
            Assert.Fail("Use macOS Editor to run the test");
            return;
        }

        BuildTarget target = BuildTarget.iOS;
        BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (activeTarget != BuildTarget.iOS)
        {
            Assert.Fail("Switch to iOS platform before running the test");
            return;
        }

        if (Directory.Exists(output))
            Directory.Delete(output, true);

        var buildOptions = BuildOptions.CompressWithLz4;
        if (development)
            buildOptions |= BuildOptions.Development;

        PrebuildCommand.GenerateAll();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = new string[] { "Assets/Scenes/main.unity" },
            locationPathName = output,
            options = buildOptions,
            target = target,
            targetGroup = BuildTargetGroup.iOS,
        };

        EnableFeature(feature);

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Assert.Fail("Failed to build");
            return;
        }
    }

    public static (int, int)[] GetDiffsInUIDAndGID(string file1, string file2)
    {
        // [36, 42): UID
        // [42, 48): GID

        byte[] buffer1 = new byte[12];

        using (var fs = File.OpenRead(file1))
        {
            fs.Seek(36, SeekOrigin.Begin);
            fs.Read(buffer1, 0, buffer1.Length);
        }

        byte[] buffer2 = new byte[12];

        using (var fs = File.OpenRead(file2))
        {
            fs.Seek(36, SeekOrigin.Begin);
            fs.Read(buffer2, 0, buffer2.Length);
        }

        var diffs = new List<(int, int)>();

        for (int i = 0; i < buffer1.Length; i++)
        {
            if (buffer1[i] != buffer2[i])
                diffs.Add((buffer1[i], buffer2[i]));
        }

        return diffs.ToArray();
    }

    public static (int, string, string) RunProcess(string fileName, string args)
    {
        return RunProcess(Directory.GetCurrentDirectory(), fileName, args);
    }

    private static (int, string, string) RunProcess(string workingDirectory, string fileName, string args)
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

            return (process.ExitCode, output.ToString(), error.ToString());
        }
    }

    public static CodeRewriterRule GetCodeRewriteRule(string featureName, BuildTarget buildTarget, Architecture architecture, bool development)
    {
        var feature = EngineBinaryFileRewriterSettings.Instance.CodeRewriterFeatures
            .Where(x => x.Name == featureName)
            .First();

        if (feature.RuleSets == null)
            return null;

        var ruleSet = feature.RuleSets.Where(x => Regex.IsMatch(Application.unityVersion, x.UnityVersion)).FirstOrDefault();
        if (ruleSet == null)
            return null;

        if (ruleSet.Rules == null)
            return null;

        var rule = ruleSet.Rules
            .Where(x => x.BuildTarget == buildTarget && x.Architecture == architecture && x.Development == development)
            .FirstOrDefault();

        if (rule != null && rule.Symbols != null && rule.Symbols.Length > 0)
            return rule;

        return null;
    }
}
