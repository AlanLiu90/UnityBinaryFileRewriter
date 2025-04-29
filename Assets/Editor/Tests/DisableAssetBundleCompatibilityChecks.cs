using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using EngineBinaryFileRewriter;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class DisableAssetBundleCompatibilityChecks
{
    private const string Feature = "Disable Asset Bundle Compatibility Checks";
    private const string GradleProjectDir = "GradleProject_" + nameof(DisableAssetBundleCompatibilityChecks);
    private const string Apk = nameof(DisableAssetBundleCompatibilityChecks) + ".apk";
    private const string AndroidBackupDir = "Backup_" + nameof(DisableAssetBundleCompatibilityChecks);
    private const string XcodeProjectDir = "XcodeProject_" + nameof(DisableAssetBundleCompatibilityChecks);
    private const AndroidArchitecture TargetAndroidArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

    [Test]
    public void TestAndroidGradleProject([Values] bool development, [Values] bool stripEngineCode)
    {
        using (var scope = BackupLibUnity.CreateScope(AndroidBackupDir))
        {
            Utility.BuildAndroid(GradleProjectDir, development, stripEngineCode, Feature, TargetAndroidArchitectures);
        }

        Utility.ValidateAndroid(GradleProjectDir, development, Feature, AndroidBackupDir, GetDiffs);
    }

    [Test]
    public void TestAndroidApk([Values] bool development, [Values] bool stripEngineCode)
    {
        using (var scope = BackupLibUnity.CreateScope(AndroidBackupDir))
        {
            Utility.BuildAndroid(Apk, development, stripEngineCode, Feature, TargetAndroidArchitectures);
        }

        Utility.ValidateAndroid(Apk, development, Feature, AndroidBackupDir, GetDiffs);
    }

    [Test]
    public void TestIOS([Values] bool development)
    {
        Utility.BuildIOS(XcodeProjectDir, development, Feature);

        Utility.ValidateIOS(XcodeProjectDir, development, Feature, GetDiffs);
    }

    private static (int, int)[] GetDiffs(string feature, BuildTarget target, Architecture architecture, bool development)
    {
        var rule = Utility.GetCodeRewriteRule(feature, target, architecture, development);
        Assert.IsNotNull(rule);

        var expectedCount = 1;
        var diffs = new List<(int, int)>();

        Assert.AreEqual(1, rule.Symbols.Length);

        foreach (var symbol in rule.Symbols)
        {
            Assert.AreEqual(1, symbol.Instructions.Length);

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

        Assert.AreEqual(expectedCount, diffs.Count);

        return diffs.ToArray();
    }
}
