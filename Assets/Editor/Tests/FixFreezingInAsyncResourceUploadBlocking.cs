using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EngineBinaryFileRewriter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

        Utility.ValidateAndroid(GradleProjectDir, development, Feature, AndroidBackupDir, GetDiffs);
    }

    [Test]
    public void TestAndroidApk([Values] bool stripEngineCode)
    {
        bool development = false;

        using (var scope = BackupLibUnity.CreateScope(AndroidBackupDir))
        {
            Utility.BuildAndroid(Apk, development, stripEngineCode, Feature, TargetAndroidArchitectures);
        }

        Utility.ValidateAndroid(Apk, development, Feature, AndroidBackupDir, GetDiffs);
    }

    [TestCase(false)]
    public void TestIOS(bool development)
    {
        Utility.BuildIOS(XcodeProjectDir, development, Feature);

        Utility.ValidateIOS(XcodeProjectDir, development, Feature, GetDiffs);
    }

    private static (int, int)[] GetDiffs(string feature, BuildTarget target, Architecture architecture, bool development)
    {
        var rule = Utility.GetCodeRewriteRule(feature, target, architecture, development);
        Assert.IsNotNull(rule);

        int repeat = target == BuildTarget.iOS ? 5 : 1;

        int expectedCount;
        var diffs = new List<(int, int)>();

        if (target == BuildTarget.iOS)
            expectedCount = repeat * 24;
        else
            expectedCount = architecture == Architecture.ARMv7 ? 11 : 24;

        Assert.AreEqual(1, rule.Symbols.Length);

        while (repeat-- > 0)
        {
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
