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
		public void StartPlaneDetection () {
			placenoteARAnchorManager = new PlacenoteARAnchorManager();

			if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
				PlacenotePlaneUtility.InitializePlanePrefab (meshPrefab);
			} else {
				PlacenotePlaneUtility.InitializePlanePrefab (planePrefab);
			}
				
		}

		public void ClearPlanes() {
			placenoteARAnchorManager.Destroy();
		}

        void OnDestroy()
        {
            placenoteARAnchorManager.Destroy ();
        }
	}
}

