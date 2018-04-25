using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

class LibraryCheck : IPreprocessBuild
{
	public int callbackOrder { get { return 0; } }
	public void OnPreprocessBuild(BuildTarget target, string path) {

		string placenoteLibraryPath = "Assets/Placenote/Plugins/iOS/Placenote.framework/Placenote";
		if (new FileInfo (placenoteLibraryPath).Length < 1000000) {
			Debug.LogError ("Placenote library missing. Please install git lfs to download the proper file. You can follow the instructions here:\nhttps://github.com/Placenote/PlacenoteSDK-Unity");
		}
	}
}
