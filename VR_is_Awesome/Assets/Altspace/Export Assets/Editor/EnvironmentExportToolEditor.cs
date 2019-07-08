using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


[CustomEditor(typeof(EnvironmentExportTool))]
public class EnvironmentExportToolEditor : Editor
{
	private string tempSceneFile = "";
	private string outAssetBundleName = "";

	private const string macAssetBundleFolder = "OSX",
						pcAssetBundleFolder = "Windows",
						androidAssetBundleFolder = "Android";

	private string AssetBundleFileExtension
	{
		get
		{
			string fileExtension = null;

			// TODO: Clean up versioning
			if (Application.unityVersion.StartsWith("2018.1"))
			{
				fileExtension = ".unity2018_1";
			}
			else if (Application.unityVersion.StartsWith("5"))
			{
				fileExtension = ".unity5";
			}
			else
			{
				throw new Exception("update unity version extension.");
			}
			return fileExtension;
		}
	}

	private EnvironmentExportTool environmentExportTool;

	[ExecuteInEditMode]
	void OnEnable()
	{
		//environmentExportTool = (EnvironmentExportTool)target;
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		//environmentExportTool = (EnvironmentExportTool)target;

		EditorGUILayout.BeginHorizontal();
		environmentExportTool.buildPCAssetBundle = GUILayout.Toggle(environmentExportTool.buildPCAssetBundle, "Windows");
		environmentExportTool.buildAndroidAssetBundle = GUILayout.Toggle(environmentExportTool.buildAndroidAssetBundle, "Android");
		EditorGUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Build Bundles", GUILayout.MaxWidth(200)))
		{
			if (!InitializeFilePaths(environmentExportTool.assetBundleName))
			{
				Debug.LogError("Unable to save out scene");
			}

			EditorApplication.SaveScene(EditorApplication.currentScene);

			//save out tmp scene
			if (!SaveOutScene())
			{
				Debug.LogError("Ran into error trying to save out scene.");
			}
			if (environmentExportTool.buildPCAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.PC))
			{
				Debug.LogError("Unable to save out asset bundle for PC (Windows).");
			}
			if (environmentExportTool.buildMacAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.MAC))
			{
				Debug.LogError("Unable to save out asset bundle for Mac.");
			}
			if (environmentExportTool.buildAndroidAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.ANDROID))
			{
				Debug.LogError("Unable to save out asset bundle for Android.");
			}
			return;
		}
		GUILayout.EndHorizontal();
	}

	public static EnvironmentExportTool.Platform GetCurrentPlatform()
	{
		if (Application.platform == RuntimePlatform.WindowsEditor)
			return EnvironmentExportTool.Platform.PC;
		else
			return EnvironmentExportTool.Platform.MAC;
	}


	public bool InitializeFilePaths(string assetBundleName)
	{
		if (assetBundleName == "")
		{
			Debug.LogError("Must provide a name or path for unity file to be save and uploaded!");
			return false;
		}
		outAssetBundleName = assetBundleName;
		string exportDirectory = GetExportDirectory();
		if (!Directory.Exists(exportDirectory))
		{
			Directory.CreateDirectory(exportDirectory);
		}

		tempSceneFile = exportDirectory + Path.DirectorySeparatorChar + assetBundleName + ".unity";

		return true;
	}

	private string GetExportDirectory()
	{
		string[] exportDirParts = { "Assets", "Altspace", "Export" };
		return string.Join(Path.DirectorySeparatorChar.ToString(), exportDirParts);
	}

	private string AssetBundleDirForPlatform(EnvironmentExportTool.Platform platform)
	{
		string exportDir = pcAssetBundleFolder;
		if (platform == EnvironmentExportTool.Platform.MAC)
		{
			exportDir = macAssetBundleFolder;
		}
		else if (platform == EnvironmentExportTool.Platform.ANDROID)
		{
			exportDir = androidAssetBundleFolder;
		}
		return GetExportDirectory() + Path.DirectorySeparatorChar + exportDir + Path.DirectorySeparatorChar;
	}

	public bool SaveOutScene()
	{
		var originalActiveScene = EditorSceneManager.GetActiveScene();
		string originalActiveScenePath = originalActiveScene.path;
		EditorSceneManager.SaveScene(originalActiveScene);
		bool success = false;
		success = EditorSceneManager.SaveScene(originalActiveScene, tempSceneFile);

		if (success)
		{
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(); // necessary?
			// EditorSceneManager.CloseScene(originalActiveScene, true); //TODO UNITYUPGRADE
			var tempScene = EditorSceneManager.OpenScene(tempSceneFile);
			EditorSceneManager.SetActiveScene(tempScene);
			DestroyImmediate(GameObject.Find("Tools"));
			EditorSceneManager.SaveScene(tempScene);
			// EditorSceneManager.CloseScene(tempScene, true); //TODO UNITYUPGRADE
			originalActiveScene = EditorSceneManager.OpenScene(originalActiveScenePath);
			EditorSceneManager.SetActiveScene(originalActiveScene);
		}
		return success;
	}

	public bool SaveOutAssetBundle(EnvironmentExportTool.Platform platform, bool shouldCompress = true)
	{
		if (tempSceneFile.Length != 0)
		{
			string[] scenes = { tempSceneFile };
			AssetBundleBuild[] buildMap = { new AssetBundleBuild() };

			buildMap[0].assetNames = scenes;
			buildMap[0].assetBundleName = outAssetBundleName;
			buildMap[0].assetBundleVariant = "unity2018_1";
			BuildTarget target = BuildTarget.StandaloneWindows;
			if (platform == EnvironmentExportTool.Platform.MAC)
			{
				//target = BuildTarget.StandaloneOSX;
			}
			else if (platform == EnvironmentExportTool.Platform.ANDROID)
			{
				target = BuildTarget.Android;
			}
			string outputDir = AssetBundleDirForPlatform(platform);
			if (!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			BuildAssetBundleOptions options = shouldCompress ? BuildAssetBundleOptions.None : BuildAssetBundleOptions.UncompressedAssetBundle;

			BuildPipeline.BuildAssetBundles(outputDir, buildMap, options, target);
			Debug.Log("Asset Bundle exported to: " + outputDir + ".");
			return true;
		}

		return false;
	}
}
