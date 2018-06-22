using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
#if UNITY_EDITOR
public class SimulatorHideARRemote : MonoBehaviour {

    public bool DisableARRemote = true;
    bool runOnce = true;
    ARKitRemoteConnection[] ARKitWorldTrackingList;

    void Update (){
        
        if (runOnce && DisableARRemote)
        {
            runOnce = false;
            ARKitWorldTrackingList = Object.FindObjectsOfType<ARKitRemoteConnection> ();
            foreach (ARKitRemoteConnection ARKitWorldTracking in ARKitWorldTrackingList)
            {
                ARKitWorldTracking.gameObject.SetActive (false);
            } 
        }

    }
}
#endif