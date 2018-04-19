using System;
using System.Collections.Generic;

namespace UnityEngine.XR.iOS
{
	public class PlacenoteARGeneratePlane : MonoBehaviour
	{
		public GameObject planePrefab;
		public GameObject meshPrefab;
        private PlacenoteARAnchorManager placenoteARAnchorManager;

		// Use this for initialization
		void Start () {
			placenoteARAnchorManager = new PlacenoteARAnchorManager();

			if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
				PlacenotePlaneUtility.InitializePlanePrefab (meshPrefab);
			} else {
				PlacenotePlaneUtility.InitializePlanePrefab (planePrefab);
			}
				
		}

        void OnDestroy()
        {
            placenoteARAnchorManager.Destroy ();
        }

        void OnGUI()
        {
			IEnumerable<ARPlaneAnchorGameObject> arpags = placenoteARAnchorManager.GetCurrentPlaneAnchors ();
			foreach(var planeAnchor in arpags)
			{
                //ARPlaneAnchor ap = planeAnchor;
                //GUI.Box (new Rect (100, 100, 800, 60), string.Format ("Center: x:{0}, y:{1}, z:{2}", ap.center.x, ap.center.y, ap.center.z));
                //GUI.Box(new Rect(100, 200, 800, 60), string.Format ("Extent: x:{0}, y:{1}, z:{2}", ap.extent.x, ap.extent.y, ap.extent.z));
            }
        }
	}
}

