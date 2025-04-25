using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EngineBinaryFileRewriter;
using NUnit.Framework;
using UnityEditor;

public class FixFreezingInAsyncResourceUploadBlocking
{
    private const string Feature = "Fix Freezing in AsyncResourceUploadBlocking";
    private const string GradleProjectDir = "GradleProject_" + nameof(FixFreezingInAsyncResourceUploadBlocking);
    private const string Apk = nameof(FixFreezingInAsyncResourceUploadBlocking) + ".apk";
    private const string AndroidBackupDir = "Backup_" + nameof(FixFreezingInAsyncResourceUploadBlocking);
    private const string XcodeProjectDir = "XcodeProject_" + nameof(FixFreezingInAsyncResourceUploadBlocking);
    private const AndroidArchitecture TargetAndroidArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

    [Test]
    public void TestAndroidGradleProject([Values] bool stripEngineCode)
    {
        bool development = false;

        using (var scope = BackupLibUnity.CreateScope(AndroidBackupDir))
        {
            Utility.BuildAndroid(GradleProjectDir, development, stripEngineCode, Feature, TargetAndroidArchitectures);
        }

        int archCount = 0;

        foreach (var kv in Utility.AndroidArchitectures)
        {
            var arch = kv.Key;
            var archName = kv.Value;

            var path = Path.Combine(GradleProjectDir, "unityLibrary/src/main/jniLibs", archName, "libunity.so");
            if (File.Exists(path))
            {
                var diffs = GetDiffs(Feature, BuildTarget.Android, arch, development).ToArray();

                var backupPath = Path.Combine(AndroidBackupDir, archName, "libunity.so");
                Utility.CompareFiles(backupPath, path, diffs);

                archCount++;
            }
        }

        Assert.AreEqual(2, archCount);
    }

    [Test]
    public void TestAndroidApk([Values] bool stripEngineCode)
    {
        bool development = false;

        using (var scope = BackupLibUnity.CreateScope(AndroidBackupDir))
        {
            Utility.BuildAndroid(Apk, development, stripEngineCode, Feature, TargetAndroidArchitectures);
        }

        string outputDir = Path.GetFileNameWithoutExtension(Apk);
        Utility.Unzip(Apk, outputDir);

        int archCount = 0;

        foreach (var kv in Utility.AndroidArchitectures)
        {
            var arch = kv.Key;
            var archName = kv.Value;

            var path = Path.Combine(outputDir, "lib", archName, "libunity.so");
            if (File.Exists(path))
            {
                var diffs = GetDiffs(Feature, BuildTarget.Android, arch, development).ToArray();

                var backupPath = Path.Combine(AndroidBackupDir, archName, "libunity.so");
                Utility.CompareFiles(backupPath, path, diffs);

                archCount++;
            }
        }

        Assert.AreEqual(2, archCount);
    }

    [TestCase(false)]
    public void TestIOS(bool development)
    {
        Utility.BuildIOS(XcodeProjectDir, development, Feature);

        var path = Path.Combine(XcodeProjectDir, "Libraries/libiPhone-lib.a");
        var backupPath = path + ".bak";

#if UNITY_2020_1_OR_NEWER
        var diffsInUIDAndGID = Utility.GetDiffsInUIDAndGID(backupPath, path);
        var diffs = GetDiffs(Feature, BuildTarget.iOS, Architecture.ARM64, development, diffsInUIDAndGID).ToArray();

        Utility.CompareFiles(backupPath, path, diffs);
#else
        var archs = new Architecture[] { Architecture.ARMv7, Architecture.ARM64 };

        foreach (var arch in archs)
        {
            var archStr = arch.ToString().ToLowerInvariant();

            string file1 = $"{archStr}.a";
            string file2 = $"{archStr}.a.bak";

            Utility.ExtractThinLibrary(path, archStr, file1);
            Utility.ExtractThinLibrary(backupPath, archStr, file2);

            var diffsInUIDAndGID = Utility.GetDiffsInUIDAndGID(file2, file1);
            var diffs = GetDiffs(Feature, BuildTarget.iOS, arch, development, diffsInUIDAndGID).ToArray();

            Utility.CompareFiles(file2, file1, diffs);
        }
#endif
    }

    private static (int, int)[] GetDiffs(string feature, BuildTarget target, Architecture architecture, bool development, (int, int)[] diffsInUIDAndGID = null)
    {
        var rule = Utility.GetCodeRewriteRule(feature, target, architecture, development);
        Assert.IsNotNull(rule);

        int repeat = target == BuildTarget.iOS ? 5 : 1;

        int expectedCount;
        var diffs = new List<(int, int)>();

        if (target == BuildTarget.iOS)
        {
            expectedCount = diffsInUIDAndGID.Length + repeat * (diffsInUIDAndGID.Length + 24);

            diffs.AddRange(diffsInUIDAndGID);
        }
        else
        {
            expectedCount = architecture == Architecture.ARMv7 ? 11 : 24;
        }

        Assert.AreEqual(1, rule.Symbols.Length);

        while (repeat-- > 0)
        {
            if (target == BuildTarget.iOS)
                diffs.AddRange(diffsInUIDAndGID);

            foreach (var symbol in rule.Symbols)
            {
                Assert.AreEqual(6, symbol.Instructions.Length);

                foreach (var inst in symbol.Instructions)
                {
                    for (int i = 0; i < inst.OriginalMachineCode.Length / 2; i++)
                    {
                        var byte1 = byte.Parse(inst.OriginalMachineCode.Substring(i * 2, 2), NumberStyles.HexNumber);
                        var byte2 = byte.Parse(inst.NewMachineCode.Substring(i * 2, 2), NumberStyles.HexNumber);

                        if (byte1 != byte2)
                            diffs.Add((byte1, byte2));
                    }
                }
            }
        }

        Assert.AreEqual(expectedCount, diffs.Count);
        return diffs.ToArray();
    }
}
