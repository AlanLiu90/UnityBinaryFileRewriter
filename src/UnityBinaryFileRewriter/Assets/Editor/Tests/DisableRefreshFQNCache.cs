using System.Collections.Generic;
using System.Globalization;
using EngineBinaryFileRewriter;
using NUnit.Framework;
using UnityEditor;
using BuildTarget = UnityEditor.BuildTarget;

public class DisableRefreshFQNCache
{
    private const string Feature = "Disable RefreshFQNCache";
    private const string GradleProjectDir = "GradleProject_" + nameof(DisableRefreshFQNCache);
    private const string Apk = nameof(DisableRefreshFQNCache) + ".apk";
    private const string AndroidBackupDir = "AndroidBackup_" + nameof(DisableRefreshFQNCache);
    private const string XcodeProjectDir = "XcodeProject_" + nameof(DisableRefreshFQNCache);
    private const AndroidArchitecture TargetAndroidArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

#if TUANJIE_1_0_OR_NEWER
    private const string OpenHarmonyProjectDir = "OpenHarmony_" + nameof(DisableRefreshFQNCache);
    private const string Hap = nameof(DisableRefreshFQNCache) + ".hap";
    private const string OpenHarmonyBackupDir = "OpenHarmonyBackup_" + nameof(DisableRefreshFQNCache);
    private const OpenHarmonyArchitecture TargetOpenHarmonyArchitectures = OpenHarmonyArchitecture.ARMv7 | OpenHarmonyArchitecture.ARM64;
#endif

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

#if TUANJIE_1_0_OR_NEWER
    [Test]
    public void TestOpenHarmonyProject([Values] bool development)
    {
        using (var scope = BackupLibUnity.CreateScope(OpenHarmonyBackupDir))
        {
            Utility.BuildOpenHarmony(OpenHarmonyProjectDir, development, Feature, TargetOpenHarmonyArchitectures);
        }

        Utility.ValidateOpenHarmony(OpenHarmonyProjectDir, development, Feature, OpenHarmonyBackupDir, GetDiffs);
    }

    [Test]
    public void TestOpenHarmonyHap([Values] bool development)
    {
        using (var scope = BackupLibUnity.CreateScope(OpenHarmonyBackupDir))
        {
            Utility.BuildOpenHarmony(Hap, development, Feature, TargetOpenHarmonyArchitectures);
        }

        Utility.ValidateOpenHarmony(Hap, development, Feature, OpenHarmonyBackupDir, GetDiffs);
    }
#endif

    private static (int, int)[] GetDiffs(string feature, BuildTarget target, Architecture architecture, bool development)
    {
        var rule = Utility.GetCodeRewriteRule(feature, target, architecture, development);
        Assert.IsNotNull(rule);

        int expectedCount;
        if (target == BuildTarget.iOS)
            expectedCount = 2;
        else if (target == BuildTarget.Android)
            expectedCount = 2;
        else
            expectedCount = architecture == Architecture.ARMv7 ? 3 : 2;

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
