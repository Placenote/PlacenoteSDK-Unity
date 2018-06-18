using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorSetSkybox : MonoBehaviour {

    public Camera mCamera;

    void Start (){
        #if UNITY_EDITOR
        mCamera = GetComponentInParent<Camera> ();
        mCamera.clearFlags = CameraClearFlags.Skybox;
        #endif
    }
}
