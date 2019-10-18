using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

// Classes to hold Notes information.

namespace StickyNotes
{

    [System.Serializable]
    public class NoteInfo
    {
        public float px;
        public float py;
        public float pz;
        public float qx;
        public float qy;
        public float qz;
        public float qw;
        public string note;
    }

    [System.Serializable]
    public class NotesList
    {
        // List of all notes stored in the current Place.
        public NoteInfo[] notes;
    }


    // Main class for managing notes.
    public class NotesManager : MonoBehaviour
    {

        public List<NoteInfo> mNotesInfoList = new List<NoteInfo>();
        public List<GameObject> mNotesObjList = new List<GameObject>();

        // Prefab for the Note
        public GameObject mNotePrefab;

        private GameObject mCurrNote;
        private NoteInfo mCurrNoteInfo;

        [SerializeField] ARRaycastManager mRaycastManager;

        // Use this for initialization
        void Start()
        {

        }

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
                    // Create note
                    InstantiateNote(hitResult.pose.position);

                    return true;
                }
            }
            return false;
        }


        // Update checks for hit test
        void Update()
        {
            // For hit testing on the device.
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended)
                {
                    if (EventSystem.current.currentSelectedGameObject == null)
                    {
                        Debug.Log("Not touching a UI button, moving on.");

                        // Test if you are hitting an existing marker
                        RaycastHit hit = new RaycastHit();
                        Ray ray = Camera.main.ScreenPointToRay(touch.position);

                        if (Physics.Raycast(ray, out hit))
                        {
                            Debug.Log("Selected an existing note.");

                            GameObject note = hit.transform.gameObject;

                            // If the previous note was deleted, switch
                            if (!mCurrNote)
                            {
                                mCurrNote = note;
                                TurnOnButtons();
                            }
                            else if (note.GetComponent<NoteController>().mIndex != mCurrNote.GetComponent<NoteController>().mIndex)
                            {
                                // New note selected is not the current note. Disable the buttons of the current note.
                                TurnOffButtons();

                                mCurrNote = note;

                                // Turn on buttons for the new selected note.
                                TurnOnButtons();

                            }
                            else
                            {
                                // Selected note is already the current note, just toggle buttons.
                                ToggleButtons();
                            }
                        }
                        else
                        {
                            Debug.Log("Creating new note.");



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
        }


        private void ToggleButtons()
        {
            int index = mCurrNote.GetComponent<NoteController>().mIndex;
            mCurrNoteInfo = mNotesInfoList[index];

            // Toggle the edit and delete buttons
            if (!mCurrNote.GetComponent<NoteController>().mActiveButtons)
            {
                TurnOnButtons();
            }
            else
            {
                TurnOffButtons();
            }
        }

        private void TurnOnButtons()
        {
            mCurrNote.GetComponent<NoteController>().mEditButton.gameObject.SetActive(true);
            mCurrNote.GetComponent<NoteController>().mDeleteButton.gameObject.SetActive(true);
            mCurrNote.GetComponent<NoteController>().mActiveButtons = true;
        }

        private void TurnOffButtons()
        {
            mCurrNote.GetComponent<NoteController>().mEditButton.gameObject.SetActive(false);
            mCurrNote.GetComponent<NoteController>().mDeleteButton.gameObject.SetActive(false);
            mCurrNote.GetComponent<NoteController>().mActiveButtons = false;
        }


        public void InstantiateNote(Vector3 notePosition)
        {
            // Instantiate new note prefab and set transform.
            GameObject note = Instantiate(mNotePrefab);
            note.transform.position = notePosition;
            note.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Turn the note to point at the camera
            Vector3 targetPosition = new Vector3(Camera.main.transform.position.x,
                                                 Camera.main.transform.position.y,
                                                 Camera.main.transform.position.z);
            note.transform.LookAt(targetPosition);
            note.transform.Rotate(0f, -180f, 0f);

            // don't think i need to do this
            note.SetActive(true);


            if (mCurrNote)
            {
                TurnOffButtons();
            }

            // Set new note as the current one.
            mCurrNote = note;

            mCurrNoteInfo = new NoteInfo
            {
                px = note.transform.position.x,
                py = note.transform.position.y,
                pz = note.transform.position.z,
                qx = note.transform.rotation.x,
                qy = note.transform.rotation.y,
                qz = note.transform.rotation.z,
                qw = note.transform.rotation.w,
                note = ""
            };

            // Set up the buttons on each note
            note.GetComponent<NoteController>().mEditButton.onClick.AddListener(OnEditButtonClick);
            note.GetComponent<NoteController>().mDeleteButton.onClick.AddListener(OnDeleteButtonClick);
            TurnOnButtons();

            EditCurrNote();
        }

        private void EditCurrNote()
        {
            Debug.Log("Editing selected note.");

            // Activate input field
            InputField input = mCurrNote.GetComponentInChildren<InputField>();
            input.interactable = true;
            input.ActivateInputField();

            input.onEndEdit.AddListener(delegate { OnNoteClosed(input); });
        }

        private void OnNoteClosed(InputField input)
        {
            Debug.Log("No longer editing current note!");

            // Save input text, and set input field as non interactable
            mCurrNoteInfo.note = input.text;
            input.DeactivateInputField();
            input.interactable = false;

            //TurnOffButtons();

            int index = mCurrNote.GetComponent<NoteController>().mIndex;
            if (index < 0)
            {
                // New note being saved!
                mCurrNote.GetComponent<NoteController>().mIndex = mNotesObjList.Count;
                Debug.Log("Saving note with ID " + mNotesObjList.Count);
                mNotesInfoList.Add(mCurrNoteInfo);
                mNotesObjList.Add(mCurrNote);
            }
            else
            {
                // Need to re-save the object.
                mNotesObjList[index] = mCurrNote;
                mNotesInfoList[index] = mCurrNoteInfo;
            }
        }

        public GameObject NoteFromInfo(NoteInfo info)
        {
            GameObject note = Instantiate(mNotePrefab);
            note.transform.position = new Vector3(info.px, info.py, info.pz);
            note.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
            note.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            note.SetActive(true);

            // add listeners to the buttons
            note.GetComponent<NoteController>().mEditButton.onClick.AddListener(OnEditButtonClick);
            note.GetComponent<NoteController>().mDeleteButton.onClick.AddListener(OnDeleteButtonClick);


            note.GetComponent<NoteController>().mEditButton.gameObject.SetActive(false);
            note.GetComponent<NoteController>().mDeleteButton.gameObject.SetActive(false);
            note.GetComponent<NoteController>().mActiveButtons = false;

            note.GetComponentInChildren<InputField>().text = info.note;

            return note;
        }

        public void OnEditButtonClick()
        {
            Debug.Log("Edit button clicked!");
            // Set current note to the right edit button.
            mCurrNoteInfo = mNotesInfoList[mCurrNote.GetComponent<NoteController>().mIndex];
            EditCurrNote();
        }

        public void OnDeleteButtonClick()
        {
            Debug.Log("Delete button clicked!");
            DeleteCurrentNote();
        }

        private void DeleteCurrentNote()
        {
            Debug.Log("Deleting current note!");
            int index = mCurrNote.GetComponent<NoteController>().mIndex;

            if (index >= 0)
            {
                Debug.Log("Index is " + index);
                mNotesObjList.RemoveAt(index);
                mNotesInfoList.RemoveAt(index);

                // Refresh Note indices
                for (int i = 0; i < mNotesObjList.Count; ++i)
                {
                    mNotesObjList[i].GetComponent<NoteController>().mIndex = i;
                }
            }

            Destroy(mCurrNote);
        }

        public void ClearNotes()
        {
            foreach (var obj in mNotesObjList)
            {
                Destroy(obj);
            }

            mNotesObjList.Clear();
            mNotesInfoList.Clear();
        }

        public JObject Notes2JSON()
        {
            NotesList notesList = new NotesList
            {
                notes = new NoteInfo[mNotesInfoList.Count]
            };

            for (int i = 0; i < mNotesInfoList.Count; ++i)
            {
                notesList.notes[i] = mNotesInfoList[i];
            }

            return JObject.FromObject(notesList);
        }

        public void LoadNotesJSON(JToken mapMetadata)
        {
            ClearNotes();

            if (mapMetadata is JObject && mapMetadata["notesList"] is JObject)
            {
                NotesList notesList = mapMetadata["notesList"].ToObject<NotesList>();

                if (notesList.notes == null)
                {
                    Debug.Log("No notes created!");
                    return;
                }

                foreach (var noteInfo in notesList.notes)
                {
                    GameObject note = NoteFromInfo(noteInfo);
                    note.GetComponent<NoteController>().mIndex = mNotesObjList.Count;

                    mNotesObjList.Add(note);
                    mNotesInfoList.Add(noteInfo);
                }
            }
        }
    }
}