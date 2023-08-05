using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateAssetBundles
{
	[MenuItem("Assets/Build AssetBundles")]
	static void BuildAllAssetBundles()
	{
		if (!Directory.Exists("Assets/AssetBundles"))
			Directory.CreateDirectory("Assets/AssetBundles");

		string[] assetNames = AssetDatabase.GetAssetPathsFromAssetBundle("asdfasdfasdf");

		AssetBundleBuild bundleBuild = new AssetBundleBuild();
		bundleBuild.assetBundleName = "SEGI.asset";
		bundleBuild.assetNames = assetNames;

		AssetBundleBuild[] buildMap = new AssetBundleBuild[] { bundleBuild };

		BuildPipeline.BuildAssetBundles(
			"Assets/AssetBundles",
			buildMap,
			BuildAssetBundleOptions.CollectDependencies
			| BuildAssetBundleOptions.ForceRebuildAssetBundle
			| BuildAssetBundleOptions.UncompressedAssetBundle
			| BuildAssetBundleOptions.CompleteAssets,
			BuildTarget.StandaloneWindows);
	}
}