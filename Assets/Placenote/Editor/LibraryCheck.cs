using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;


class LibraryCheck : ScriptableObject, IPreprocessBuild
{
	public int callbackOrder { get { return 0; } }
	public void OnPreprocessBuild(BuildTarget target, string path) {
		var script = MonoScript.FromScriptableObject (this);
		string scriptLoc = AssetDatabase.GetAssetPath (script); //this scripts path
		string[] splitPathStr = scriptLoc.Split ('/');
		string placenoteLibraryPath = string.Join ("/", splitPathStr.Take (splitPathStr.Length - 2).ToArray ()) + "/Plugins/iOS/Placenote.framework/Placenote"; //find library relative to this scripts path

		if (!File.Exists (placenoteLibraryPath)) {
			Debug.LogError ("Can't find Placenote Library. It should be in Placenote/Plugins/iOS/Placenote.framework Please install git lfs to download the proper file. You can follow the instructions here:\nhttps://github.com/Placenote/PlacenoteSDK-Unity");
		}

		if (new FileInfo (placenoteLibraryPath).Length < 1000000) {
			Debug.LogError ("Placenote not properly downloaded. Please install git lfs to download it properly. You can follow the instructions here:\nhttps://github.com/Placenote/PlacenoteSDK-Unity");
		}
	}
}


public class PlacenoteVersionInfo : EditorWindow
{

	private string versionString = "v1.6.1";

	// Add a menu item named "Do Something" to MyMenu in the menu bar.
	[MenuItem("Placenote/About Placenote...")]
	static void VersionOutput()
	{

		PlacenoteVersionInfo window = ScriptableObject.CreateInstance<PlacenoteVersionInfo>();
		window.position = new Rect(Screen.width / 2, Screen.height / 2, 450, 100);
		window.ShowPopup();
	}

	[MenuItem("Placenote/Help...")]
	static void HelpOutput()
	{
		Application.OpenURL("https://vertical.us11.list-manage.com/track/click?u=b63923e54766af5486b0555d4&id=f8c5cd33ec&e=e427dca59e");
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("www.placenote.com\nPlacenote Version " + versionString + "\n" +
			"", EditorStyles.wordWrappedLabel);

		GUILayout.Space(5);
		if (GUILayout.Button("Done")) this.Close();

		GUILayout.Space(5);
		if (GUILayout.Button("Details.."))  {
			this.Close ();
			Application.OpenURL("https://github.com/Placenote/PlacenoteSDK-Unity/releases");
		}
	}
}
