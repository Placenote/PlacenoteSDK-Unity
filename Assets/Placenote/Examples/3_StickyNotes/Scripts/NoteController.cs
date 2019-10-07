using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StickyNotes
{
    public class NoteController : MonoBehaviour
    {
        // This is set to -1 when instantiated, and assigned when saving notes.
        [SerializeField] public int mIndex = -1;
        [SerializeField] public bool mActiveButtons = false;


        public Button mEditButton;
        public Button mDeleteButton;

    }
}
