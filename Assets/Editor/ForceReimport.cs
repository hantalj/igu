using UnityEditor;

public static class ForceReimport
{
	[MenuItem("Tools/Force Reimport Art")]
	public static void Run()
	{
		AssetDatabase.ImportAsset("Assets/Art/Tiles", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
	}
}
