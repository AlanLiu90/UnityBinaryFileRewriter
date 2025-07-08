using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateBundles
{
    [MenuItem("Freeze Repro/Build bundles")]
    static void BuildBundles() {
        string assetBundleDirectory = "Assets/StreamingAssets";
        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        BuildAssetBundleOptions options = BuildAssetBundleOptions.ForceRebuildAssetBundle;
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, options, EditorUserBuildSettings.activeBuildTarget);
    }

#if false
	[MenuItem("Freeze Repro/Build bundles SBP")]
    static void BuildBundlesSBP() {
        string assetBundleDirectory = "Assets/StreamingAssets";
        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        BuildAssetBundlesScriptableBuildPipeline(
	        AssetDatabase.GetAllAssetBundleNames(),
	        assetBundleDirectory,
            EditorUserBuildSettings.activeBuildTarget
        );
    }
    
	private static void BuildAssetBundlesScriptableBuildPipeline(string[] bundleNames, string outputDirectory, BuildTarget buildTarget) {
		var bundlesToBuild = GetAssetBundleBuildFor(bundleNames);

		BundleBuildContent buildContent = new BundleBuildContent(bundlesToBuild);
		BundleBuildParameters buildParams = new BundleBuildParameters(buildTarget, BuildPipeline.GetBuildTargetGroup(buildTarget), outputDirectory);
		
		buildParams.BundleCompression = BuildCompression.Uncompressed;

		ContentPipeline.BuildAssetBundles(
			buildParams,
			buildContent,
			out IBundleBuildResults results
		);
	}
	
	private static AssetBundleBuild[] GetAssetBundleBuildFor(string[] bundleNames) {
		
		List<AssetBundleBuild> assetBundleBuilds = new ();

		foreach(string bundleName in bundleNames) {
			string[] allAssetsInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);

			AssetBundleBuild build = new AssetBundleBuild {
				assetBundleName = bundleName,
				assetNames = allAssetsInBundle
			};
			assetBundleBuilds.Add(build);
		}

		return assetBundleBuilds.ToArray();
	}
#endif
}
