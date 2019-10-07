using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace HelloWorld
{
    public class FileReadWrite : MonoBehaviour
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


        public void WriteModelTransformToFile(Vector3 position, Quaternion rotation)
        {
            string path = Application.persistentDataPath + "/modelTransform.txt";
            Debug.Log(path);
            StreamWriter writer = new StreamWriter(path, false);

            string modelTfString = position.x + "," + position.y + "," + position.z + ","
                                    + rotation.x + "," + rotation.y + "," + rotation.z + "," + rotation.w;

            Debug.Log("Saving Transform Details : " + modelTfString);
            writer.WriteLine(modelTfString);
            writer.Close();
        }


        public bool ReadModelTransformFromFile(ref Vector3 position, ref Quaternion rotation)
        {
            string path = Application.persistentDataPath + "/modelTransform.txt";
            Debug.Log(path);

            if (System.IO.File.Exists(path))
            {
                StreamReader reader = new StreamReader(path);
                string modelTfString = reader.ReadLine();

                string[] modelTfArray = modelTfString.Split(',');
                position = new Vector3(
                                        float.Parse(modelTfArray[0]),
                                        float.Parse(modelTfArray[1]),
                                        float.Parse(modelTfArray[2]));

                rotation = new Quaternion(
                                        float.Parse(modelTfArray[3]),
                                        float.Parse(modelTfArray[4]),
                                        float.Parse(modelTfArray[5]),
                                        float.Parse(modelTfArray[6]));


                reader.Close();


                return true;
            }
            else
            {
                return false;
            }

        }

    }
}
