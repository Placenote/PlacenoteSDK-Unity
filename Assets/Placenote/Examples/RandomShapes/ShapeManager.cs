using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json.Linq;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

// Classes to hold shape information
[System.Serializable]
public class ShapeInfo
{
    public float px;
    public float py;
    public float pz;
    public float qx;
    public float qy;
    public float qz;
    public float qw;
    public int shapeType;
    public int colorType;
}


[System.Serializable]
public class ShapeList
{
    public ShapeInfo[] shapes;
}


// Main Class for Managing Markers
public class ShapeManager : MonoBehaviour
{
    public List<ShapeInfo> shapeInfoList = new List<ShapeInfo>();
    public List<GameObject> shapeObjList = new List<GameObject>();
    public Material mShapeMaterial;
    [SerializeField] ARRaycastManager mRaycastManager;
    private Color[] colorTypeOptions = {Color.cyan, Color.red, Color.yellow};

	// Use this for initialization
	void Start () {}

    // The HitTest to Add a Marker
    bool HitTestWithResultType(Vector2 point, TrackableType resultType)
    {
        List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
        Debug.Log("point: " + point.x + " " + point.y);
        mRaycastManager.Raycast(point, hitResults, resultType);

        if (hitResults.Count > 0)
        {
            foreach (var hitResult in hitResults)
            {
                Debug.Log("Got hit!");

                // add shape
                AddShape(hitResult.pose.position, hitResult.pose.rotation);

                return true;
            }
        }
        return false;
    }


    // Update function checks for hittest

    void Update()
    {
        // Check if the screen is touched
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    Debug.Log("Not touching a UI button. Moving on.");

                    // prioritize reults types
                    TrackableType resultType = TrackableType.FeaturePoint;
                    if (HitTestWithResultType(touch.position, resultType))
                    {
                        Debug.Log("Found a hit test result");
                    }
                }
            }
        }
    }

	public void OnSimulatorDropShape()
	{
		Vector3 dropPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
		Quaternion dropRotation = Camera.main.transform.rotation;

		AddShape(dropPosition, dropRotation);
	}


    // All shape management functions (add shapes, save shapes to metadata etc.
    public void AddShape(Vector3 shapePosition, Quaternion shapeRotation)
    {
        System.Random rnd = new System.Random();
        PrimitiveType type = (PrimitiveType)rnd.Next(0, 4);

        int colorType =  rnd.Next(0, 3);

        ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = shapePosition.x;
        shapeInfo.py = shapePosition.y;
        shapeInfo.pz = shapePosition.z;
        shapeInfo.qx = shapeRotation.x;
        shapeInfo.qy = shapeRotation.y;
        shapeInfo.qz = shapeRotation.z;
        shapeInfo.qw = shapeRotation.w;
        shapeInfo.shapeType = type.GetHashCode();
        shapeInfo.colorType = colorType;
        shapeInfoList.Add(shapeInfo);

        GameObject shape = ShapeFromInfo(shapeInfo);
        shapeObjList.Add(shape);
    }


    public GameObject ShapeFromInfo(ShapeInfo info)
    {
        GameObject shape = GameObject.CreatePrimitive((PrimitiveType)info.shapeType);
        shape.transform.position = new Vector3(info.px, info.py, info.pz);
        shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
        shape.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        shape.GetComponent<MeshRenderer>().material = mShapeMaterial;
        shape.GetComponent<MeshRenderer>().material.color = colorTypeOptions[info.colorType];
        return shape;
    }

    public void ClearShapes()
    {
        foreach (var obj in shapeObjList)
        {
            Destroy(obj);
        }
        shapeObjList.Clear();
        shapeInfoList.Clear();
    }


    public JObject Shapes2JSON()
    {
        ShapeList shapeList = new ShapeList();
        shapeList.shapes = new ShapeInfo[shapeInfoList.Count];
        for (int i = 0; i < shapeInfoList.Count; i++)
        {
            shapeList.shapes[i] = shapeInfoList[i];
        }

        return JObject.FromObject(shapeList);
    }

    public void LoadShapesJSON(JToken mapMetadata)
    {
        ClearShapes();
        if (mapMetadata is JObject && mapMetadata["shapeList"] is JObject)
        {
            ShapeList shapeList = mapMetadata["shapeList"].ToObject<ShapeList>();
            if (shapeList.shapes == null)
            {
                Debug.Log("no shapes dropped");
                return;
            }

            foreach (var shapeInfo in shapeList.shapes)
            {
                shapeInfoList.Add(shapeInfo);
                GameObject shape = ShapeFromInfo(shapeInfo);
                shapeObjList.Add(shape);
            }
        }
    }



}
