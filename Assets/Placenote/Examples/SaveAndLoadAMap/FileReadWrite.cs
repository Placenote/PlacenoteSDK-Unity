using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FileReadWrite: MonoBehaviour
{

    public void WriteMapIDToFile(string mapID)
    {
        string path = Application.persistentDataPath + "/mapID.txt";
        Debug.Log(path);
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(mapID);
        writer.Close();
    }

    public string ReadMapIDFromFile()
    {
        string path = Application.persistentDataPath + "/mapID.txt";
        Debug.Log(path);

        if (System.IO.File.Exists(path))
        {
            StreamReader reader = new StreamReader(path);
            string returnValue = reader.ReadLine();

            Debug.Log(returnValue);
            reader.Close();

            return returnValue;
        }
        else
        {
            return null;
        }


    }
}
