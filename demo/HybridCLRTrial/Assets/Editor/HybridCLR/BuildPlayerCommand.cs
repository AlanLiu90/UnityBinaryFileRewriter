#define EXPORT_AS_GRADLE_PROJECT

using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace HybridCLR.Editor
{
    public class BuildPlayerCommand
    {
        public static void CopyAssets(string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            foreach(var srcFile in Directory.GetFiles(Application.streamingAssetsPath))
            {
                string dstFile = $"{outputDir}/{Path.GetFileName(srcFile)}";
                File.Copy(srcFile, dstFile, true);
            }
        }

        public static void InstallFromRepo()
        {
            var ic = new InstallerController();
            ic.InstallDefaultHybridCLR();
        }

        public static void InstallBuildWin64()
        {
            InstallFromRepo();
            Build_Win64(true);
        }

        [MenuItem("Build/Win64")]
        public static void Build_Win64()
        {
            Build_Win64(false);
        }

        [MenuItem("Build/Win64_Assets")]
        public static void BuildAssets_Win64()
        {
            BuildAssets_Win64_();
        }

        [MenuItem("Build/Android")]
        public static void Build_Android()
        {
            Build_Android(false);
        }

        [MenuItem("Build/Android_Assets")]
        public static void BuildAssets_Android()
        {
            BuildAssets_Android_();
        }

        [MenuItem("Build/iOS")]
        public static void Build_iOS()
        {
            Build_iOS(false);
        }

        [MenuItem("Build/iOS_Assets")]
        public static void BuildAssets_iOS()
        {
            BuildAssets_iOS_();
        }

#if TUANJIE_1_0_OR_NEWER
        [MenuItem("Build/OpenHarmony")]
        public static void Build_OpenHarmony()
        {
            Build_OpenHarmony(false);
        }

        [MenuItem("Build/OpenHarmony_Assets")]
        public static void BuildAssets_OpenHarmony()
        {
            BuildAssets_OpenHarmony_();
        }
#endif

        public static void Build_Win64(bool exitWhenCompleted)
        {
            BuildTarget target = BuildTarget.StandaloneWindows64;
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (activeTarget != BuildTarget.StandaloneWindows64 && activeTarget != BuildTarget.StandaloneWindows)
            {
                Debug.LogError("请先切到Win平台再打包");
                return;
            }
            // Get filename.
            string outputPath = $"{SettingsUtil.ProjectDir}/Release-Win64";

            var buildOptions = BuildOptions.CompressWithLz4;

            string location = $"{outputPath}/HybridCLRTrial.exe";

            PrebuildCommand.GenerateAll();
            Debug.Log("====> Build App");
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/Scenes/main.unity" },
                locationPathName = location,
                options = buildOptions,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("打包失败");
                if (exitWhenCompleted)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            BuildAssets_Win64_();
        }

        public static void BuildAssets_Win64_()
        {
            string outputPath = $"{SettingsUtil.ProjectDir}/Release-Win64";

            Debug.Log("====> 复制热更新资源和代码");
            BuildAssetsCommand.BuildAndCopyABAOTHotUpdateDlls();
            CopyDir(Application.streamingAssetsPath, $"{outputPath}/HybridCLRTrial_Data/StreamingAssets", true);
        }

        public static void Build_Android(bool exitWhenCompleted)
        {
            BuildTarget target = BuildTarget.Android;
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (activeTarget != BuildTarget.Android)
            {
                Debug.LogError("请先切到Android平台再打包");
                return;
            }

#if EXPORT_AS_GRADLE_PROJECT
            // Get filename.
            string outputPath = $"{SettingsUtil.ProjectDir}/GradleProject";

            string location = $"{outputPath}/HybridCLRTrial";

            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
#else
            string outputPath = $"{SettingsUtil.ProjectDir}/HybridCLRTrial.apk";

            string location = $"{outputPath}";

            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
#endif

            var buildOptions = BuildOptions.CompressWithLz4;

            PrebuildCommand.GenerateAll();
            Debug.Log("====> Build App");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/Scenes/main.unity" },
                locationPathName = location,
                options = buildOptions,
                target = target,
                targetGroup = BuildTargetGroup.Android,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("打包失败");
                if (exitWhenCompleted)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

#if EXPORT_AS_GRADLE_PROJECT
            BuildAssets_Android_();
#endif
        }

        public static void BuildAssets_Android_()
        {
            string outputPath = $"{SettingsUtil.ProjectDir}/GradleProject";

            Debug.Log("====> 复制热更新资源和代码");
            BuildAssetsCommand.BuildAndCopyABAOTHotUpdateDlls();
            CopyDir(Application.streamingAssetsPath, $"{outputPath}/HybridCLRTrial/unityLibrary/src/main/assets", true, false);
        }

        public static void Build_iOS(bool exitWhenCompleted)
        {
            BuildTarget target = BuildTarget.iOS;
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (activeTarget != BuildTarget.iOS)
            {
                Debug.LogError("请先切到iOS平台再打包");
                return;
            }
            // Get filename.
            string outputPath = $"{SettingsUtil.ProjectDir}/XcodeProject";

            var buildOptions = BuildOptions.CompressWithLz4;

            string location = $"{outputPath}/HybridCLRTrial";

            PrebuildCommand.GenerateAll();
            Debug.Log("====> Build App");

            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/Scenes/main.unity" },
                locationPathName = location,
                options = buildOptions,
                target = target,
                targetGroup = BuildTargetGroup.iOS,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("打包失败");
                if (exitWhenCompleted)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            BuildAssets_iOS_();
        }

        public static void BuildAssets_iOS_()
        {
            string outputPath = $"{SettingsUtil.ProjectDir}/XcodeProject";

            Debug.Log("====> 复制热更新资源和代码");
            BuildAssetsCommand.BuildAndCopyABAOTHotUpdateDlls();
            CopyDir(Application.streamingAssetsPath, $"{outputPath}/HybridCLRTrial/Data/Raw", true, false);
        }

#if TUANJIE_1_0_OR_NEWER
        public static void Build_OpenHarmony(bool exitWhenCompleted)
        {
            BuildTarget target = BuildTarget.OpenHarmony;
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            if (activeTarget != BuildTarget.OpenHarmony)
            {
                Debug.LogError("请先切到OpenHarmony平台再打包");
                return;
            }

#if EXPORT_AS_GRADLE_PROJECT
            // Get filename.
            string outputPath = $"{SettingsUtil.ProjectDir}/OpenHarmonyProject";

            string location = $"{outputPath}/HybridCLRTrial";

            EditorUserBuildSettings.exportAsOpenHarmonyProject = true;
#else
            string outputPath = $"{SettingsUtil.ProjectDir}/HybridCLRTrial.hap";

            string location = $"{outputPath}";

            EditorUserBuildSettings.exportAsOpenHarmonyProject = false;
#endif

            var buildOptions = BuildOptions.CompressWithLz4;

            PrebuildCommand.GenerateAll();
            Debug.Log("====> Build App");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/Scenes/main.unity" },
                locationPathName = location,
                options = buildOptions,
                target = target,
                targetGroup = BuildTargetGroup.OpenHarmony,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError("打包失败");
                if (exitWhenCompleted)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

#if EXPORT_AS_GRADLE_PROJECT
            BuildAssets_OpenHarmony_();
#endif
        }

        public static void BuildAssets_OpenHarmony_()
        {
            string outputPath = $"{SettingsUtil.ProjectDir}/OpenHarmonyProject";

            Debug.Log("====> 复制热更新资源和代码");
            BuildAssetsCommand.BuildAndCopyABAOTHotUpdateDlls();
            CopyDir(Application.streamingAssetsPath, $"{outputPath}/HybridCLRTrial/entry/src/main/resources/rawfile/Data/StreamingAssets", true, false);
        }
#endif

        private static void CopyWithCheckLongFile(string srcFile, string dstFile)
        {
            var maxPathLength = 255;
#if UNITY_EDITOR_OSX
            maxPathLength = 1024;
#endif
            if (srcFile.Length > maxPathLength)
            {
                UnityEngine.Debug.LogError($"srcFile:{srcFile} path is too long. copy ignore!");
                return;
            }
            if (dstFile.Length > maxPathLength)
            {
                UnityEngine.Debug.LogError($"dstFile:{dstFile} path is too long. copy ignore!");
                return;
            }
            File.Copy(srcFile, dstFile, true);
        }

        private static void CopyDir(string src, string dst, bool log = false, bool deleteOld = true)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"[BashUtil] CopyDir {src} => {dst}");
            }

            if (deleteOld)
                RemoveDir(dst);

            if (!Directory.Exists(dst))
                Directory.CreateDirectory(dst);

            foreach (var file in Directory.GetFiles(src))
            {
                CopyWithCheckLongFile(file, $"{dst}/{Path.GetFileName(file)}");
            }
            foreach (var subDir in Directory.GetDirectories(src))
            {
                CopyDir(subDir, $"{dst}/{Path.GetFileName(subDir)}", log, deleteOld);
            }
        }

        private static void RemoveDir(string dir, bool log = false)
        {
            if (log)
            {
                UnityEngine.Debug.Log($"[BashUtil] RemoveDir dir:{dir}");
            }

            int maxTryCount = 5;
            for (int i = 0; i < maxTryCount; ++i)
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        return;
                    }
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    foreach (var subDir in Directory.GetDirectories(dir))
                    {
                        RemoveDir(subDir);
                    }
                    Directory.Delete(dir);
                    break;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[BashUtil] RemoveDir:{dir} with exception:{e}. try count:{i}");
                    Thread.Sleep(100);
                }
            }
        }
    }
}
