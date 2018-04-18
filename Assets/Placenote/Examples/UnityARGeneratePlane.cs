using System;
using System.Collections.Generic;

namespace UnityEngine.XR.iOS
{
	public class UnityARGeneratePlane : MonoBehaviour
	{
		public GameObject planePrefab;
		public GameObject meshPrefab; //for use when software version >iOS 11.3
        private UnityARAnchorManager unityARAnchorManager;

		// Use this for initialization
		void Start () {
            unityARAnchorManager = new UnityARAnchorManager();

			if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
				UnityARUtility.InitializePlanePrefab (meshPrefab);
			} else {
				UnityARUtility.InitializePlanePrefab (planePrefab);
			}
		}

        void OnDestroy()
        {
            unityARAnchorManager.Destroy ();
        }

        void OnGUI()
        {
			IEnumerable<ARPlaneAnchorGameObject> arpags = unityARAnchorManager.GetCurrentPlaneAnchors ();
			foreach(var planeAnchor in arpags)
			{
                //ARPlaneAnchor ap = planeAnchor;
                //GUI.Box (new Rect (100, 100, 800, 60), string.Format ("Center: x:{0}, y:{1}, z:{2}", ap.center.x, ap.center.y, ap.center.z));
                //GUI.Box(new Rect(100, 200, 800, 60), string.Format ("Extent: x:{0}, y:{1}, z:{2}", ap.extent.x, ap.extent.y, ap.extent.z));
            }
        }
	}
}

