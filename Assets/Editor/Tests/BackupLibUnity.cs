using System;
using System.IO;
using UnityEditor.Android;

class BackupLibUnity : IPostGenerateGradleAndroidProject
#if TUANJIE_1_0_OR_NEWER
    , UnityEditor.OpenHarmony.IPostGenerateOpenHarmonyProject
#endif
{
    public int callbackOrder => 99;

    private static string mBackupDir;

    public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
    {
        if (string.IsNullOrEmpty(mBackupDir))
            return;

        if (Directory.Exists(mBackupDir))
            Directory.Delete(mBackupDir, true);

        Directory.CreateDirectory(mBackupDir);

        foreach (var arch in Utility.AndroidArchitectures.Values)
        {
            var path = Path.Combine(unityLibraryPath, "src/main/jniLibs", arch, Utility.GetLibUnity());
            if (File.Exists(path))
            {
                var archBackupPath = Path.Combine(mBackupDir, arch);
                Directory.CreateDirectory(archBackupPath);
                File.Copy(path, Path.Combine(archBackupPath, Path.GetFileName(path)));
            }
        }
    }

    public void OnPostGenerateOpenHarmonyProject(string entryPath)
    {
        if (string.IsNullOrEmpty(mBackupDir))
            return;

        if (Directory.Exists(mBackupDir))
            Directory.Delete(mBackupDir, true);

        Directory.CreateDirectory(mBackupDir);

        foreach (var arch in Utility.AndroidArchitectures.Values)
        {
            var path = Path.Combine(entryPath, "libs", arch, "libtuanjie.so");
            if (File.Exists(path))
            {
                var archBackupPath = Path.Combine(mBackupDir, arch);
                Directory.CreateDirectory(archBackupPath);
                File.Copy(path, Path.Combine(archBackupPath, Path.GetFileName(path)));
            }
        }
    }

    public static BackupLibUnityScope CreateScope(string backupDir)
    {
        return new BackupLibUnityScope(backupDir);
    }

    public sealed class BackupLibUnityScope : IDisposable
    {
        public BackupLibUnityScope(string backupDir)
        {
            mBackupDir = backupDir;
        }

        public void Dispose()
        {
            mBackupDir = null;
        }
    }
}