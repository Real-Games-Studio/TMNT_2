using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imagine.WebAR{

    public class FaceTrackerGlobalSettings : ScriptableObject
    {
        [SerializeField][Range(1,4)] public int maxFaces = 1;
        [SerializeField] public bool dontOverrideFaceCount = false;
        
        
        private static FaceTrackerGlobalSettings _instance;
        public static FaceTrackerGlobalSettings Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = Resources.Load<FaceTrackerGlobalSettings>("FaceTrackerGlobalSettings");
                }
                return _instance;

            }
        }
    }
}
