#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;


[CustomEditor(typeof(LibPlacenote))]
public class LibPlacenoteEditor : Editor, IPreprocessBuild
{
	public int callbackOrder { get { return 0; } }
	string filePath;
	void OnEnable()
	{
		//File where APIKey gets written out
		filePath = Application.persistentDataPath
			+ @"/apikey.dat";
	}

	public override void OnInspectorGUI()
	{
		//Called everytime PlacenoteCameraManager (and the attached LibPlacenote script) is touched. We write out the APIKey
		var lib = target as LibPlacenote;
		DrawDefaultInspector ();
		StreamWriter writer = new StreamWriter (filePath, false);
		writer.WriteLine(lib.apiKey);
		writer.Close();
	}

	public void OnPreprocessBuild(BuildTarget target, string path) {

		//Right before a build starts, read the APIKey that was entered in OnInspectorGUI and make sure its not blank. 
		StreamReader reader = new StreamReader(filePath); 
		string keyRead = reader.ReadToEnd ();
	
		if (keyRead == null) {
			Debug.LogError ("API Key Empty. Please get an API Key from http://developers.placenote.com and enter it under the LibPlacenote Object in the PlacenoteCameraManager");
		} else if (keyRead.Trim () == "") {
			Debug.LogError ("API Key Empty. Please get an API Key from http://developers.placenote.com and enter it under the LibPlacenote Object in the PlacenoteCameraManager");
		} else {
			Debug.Log ("API Key Entered:" + keyRead);
		}
		reader.Close();
	}
}
#endif

