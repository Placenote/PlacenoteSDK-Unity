using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

public class DrawingHistoryManager : MonoBehaviour {


	public GameObject paintBrushSceneObject;

	public class drawingcommand{
		public int index;
		public int objType;
		public Vector3 position;
		public Color color;
		public float lineWidth;

		public drawingcommand(int _index, int _objType, Vector3 _position, Color _color, float _lineWidth)
		{
			index = _index;
			objType = _objType;
			position = _position;
			color = _color;
			lineWidth = _lineWidth;
		}
	}

	public List<drawingcommand> drawingHistory;

	// Use this for initialization
	void Start () {

		// initialize the queue.
		drawingHistory = new List<drawingcommand>();
	}

	// Update is called once per frame
	void Update () {

	}

	public void resetHistory()
	{
		drawingHistory.Clear ();
	}

	public void addDrawingCommand(int index, int objType, Vector3 position, Color color, float lineWidth)
	{
		drawingcommand newCommand = new drawingcommand (index, objType, position, color, lineWidth);
		drawingHistory.Add (newCommand);
	}

	public IEnumerator replayDrawing()
	{
		int currentIndex = -1;

		foreach (drawingcommand cmd in drawingHistory) {

			if (cmd.index == currentIndex) {

				// continue line

				paintBrushSceneObject.GetComponent<DrawLineManager>().addReplayLineSegment(true, cmd.lineWidth, cmd.position, cmd.color);

				currentIndex = cmd.index;

			} else if (cmd.index > currentIndex) {

				if (cmd.objType == 0)
				{
					// add line

					paintBrushSceneObject.GetComponent<DrawLineManager>().addReplayLineSegment(false, cmd.lineWidth, cmd.position, cmd.color);
					currentIndex = cmd.index;


				}
				else if (cmd.objType == 1)
				{
					// add cube
					//cubePanel.GetComponent<DrawCubeManager>().addReplayCubeAtEndpoint(cmd.position, cmd.color);
					currentIndex = cmd.index;

				}

			}

            yield return null;

		}

	}

	public bool isDrawingHistoryEmpty()
	{
		if (drawingHistory.Count == 0)
			return true;
		else
			return false;
	}

	public void replayDrawingFast()
	{
		int currentIndex = -1;

		foreach (drawingcommand cmd in drawingHistory) {

			if (cmd.index == currentIndex) {

				// continue line

				paintBrushSceneObject.GetComponent<DrawLineManager>().addReplayLineSegment(true, cmd.lineWidth, cmd.position, cmd.color);

				currentIndex = cmd.index;

			} else if (cmd.index > currentIndex) {

				if (cmd.objType == 0)
				{
					// add line

					paintBrushSceneObject.GetComponent<DrawLineManager>().addReplayLineSegment(false, cmd.lineWidth, cmd.position, cmd.color);
					currentIndex = cmd.index;


				}
				else if (cmd.objType == 1)
				{
					// add cube
					//cubePanel.GetComponent<DrawCubeManager>().addReplayCubeAtEndpoint(cmd.position, cmd.color);
					currentIndex = cmd.index;

				}

			}
				


		}

	}


	public void saveMapIDToFile(string mapid)
	{
		string filePath = Application.persistentDataPath + "/mapIDFile.txt";
		StreamWriter sr = File.CreateText (filePath);
		sr.WriteLine (mapid);
		sr.Close ();
	}

	public string loadMapIDFromFile ()
	{
		string savedMapID;

		// read history file
		FileInfo historyFile = new FileInfo(Application.persistentDataPath + "/mapIDFile.txt");
		StreamReader sr = historyFile.OpenText ();
		string text;

		do {
			text = sr.ReadLine();

			if (text != null)
			{
				// Create drawing command structure from string.
				savedMapID = text;
				return savedMapID;

			}

		} while (text != null);

		return null;
	}


	// save the drawing histor
	public void saveDrawingHistory()
	{
		// save the current drawingHistory as the only file allowed to be saved.
		// write to file
		string filePath = Application.persistentDataPath + "/historyFile.txt";

		Debug.Log ("File path saved = " + filePath);

		StreamWriter sr = File.CreateText (filePath);

		foreach (drawingcommand cmd in drawingHistory)
		{
			string toWrite = cmd.index.ToString() + "," + cmd.objType.ToString() + "," + cmd.position.x.ToString() + "," + cmd.position.y.ToString() + "," 
				+ cmd.position.z.ToString() + "," + cmd.color.r.ToString() + "," 
				+ cmd.color.g.ToString() + "," + cmd.color.b.ToString() + "," 
				+ cmd.lineWidth.ToString();

			sr.WriteLine (toWrite);

		}
		sr.Close ();

	}

	public void loadFromDrawingHistory()
	{

		// read history file
		FileInfo historyFile = new FileInfo(Application.persistentDataPath + "/historyFile.txt");
		StreamReader sr = historyFile.OpenText ();
		string text;

		do {
			text = sr.ReadLine();

			if (text != null)
			{
				// Create drawing command structure from string.
				drawingcommand lineCmd = parseSaveCmdLine(text);
				drawingHistory.Add(lineCmd);

			}

		} while (text != null);

		// load into scene

	}

	drawingcommand parseSaveCmdLine(string textLine)
	{
		string[] values = textLine.Split (',');

		int _index = Int32.Parse (values [0]);
		int _objType = Int32.Parse (values [1]);
		Vector3 _position = new Vector3 (Convert.ToSingle (values [2]), Convert.ToSingle (values [3]), Convert.ToSingle (values [4]));
		Color _color = new Color (Convert.ToSingle (values [5]), Convert.ToSingle (values [6]), Convert.ToSingle (values [7]));
		float _lineWidth = Convert.ToSingle (values [8]);

		drawingcommand theCmd = new drawingcommand (_index, _objType, _position, _color, _lineWidth);

		return theCmd;
	}



}
