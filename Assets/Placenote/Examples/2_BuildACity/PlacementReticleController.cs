using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace BuildACity
{
    public class PlacementReticleController : MonoBehaviour
    {
        public GameObject mReticle;
        public GameObject mObjReticle;



        [SerializeField] Text notifications;

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
        public ARRaycastManager m_RaycastManager;

        private IEnumerator mContinuousHittest;

        void Start()
        {
            mContinuousHittest = ContinuousHittest();

        }

        // starts the cursor
        public void StartReticle()
        {

            //mObjReticle = Instantiate(modelPrefab);
            //mObjReticle.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            //mObjReticle.SetActive(false);

            mReticle.SetActive(false);

            StartCoroutine(mContinuousHittest);
        }


        public void ObjReticleActivate(GameObject modelPrefab)
        {
            mObjReticle = Instantiate(modelPrefab);
            mObjReticle.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            mObjReticle.SetActive(false);
        }

        public void ObjReticleDeactivate()
        {
            if (mObjReticle != null)
            {
                Destroy(mObjReticle);
            }

            mObjReticle.SetActive(false);

        }

        public void StopReticle()
        {
            StopCoroutine(mContinuousHittest);

            mReticle.SetActive(false);

            if (mObjReticle != null)
            {
                Destroy(mObjReticle);
                mObjReticle.SetActive(false);
            }


        }


        private IEnumerator ContinuousHittest()
        {

            while (true)
            {
                // getting screen point
                var screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

                // World Hit Test
                if (m_RaycastManager.Raycast(screenPosition, s_Hits, TrackableType.PlaneWithinBounds))
                {

                    // Raycast hits are sorted by distance, so get the closest hit.
                    var targetPose = s_Hits[0].pose;

                    mReticle.transform.position = targetPose.position;
                    mReticle.SetActive(true);

                    if (mObjReticle != null)
                    {
                        mObjReticle.transform.position = targetPose.position;

                        mObjReticle.transform.LookAt(new Vector3(Camera.main.transform.position.x, mObjReticle.transform.position.y, Camera.main.transform.position.z));

                        mObjReticle.SetActive(true);

                    }


                }
                else
                {
                    mReticle.SetActive(false);

                    if (mObjReticle != null)
                    {
                        mObjReticle.SetActive(false);
                    }
                }

                // go to next frame
                yield return null;
            }
        }



    }
}