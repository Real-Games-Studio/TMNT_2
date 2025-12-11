using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Imagine.WebAR.Editor
{
    public class PostProcessBuild_FT : MonoBehaviour
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string buildPath)
        {
            var htmlLines = File.ReadAllLines(buildPath + "/index.html");
            
            int faceCount = FaceTrackerGlobalSettings.Instance.maxFaces;
            if(FaceTrackerGlobalSettings.Instance.dontOverrideFaceCount){
                Debug.Log("Keeping numFaces variable in index.html");
                return;
            }

            Debug.Log("Setting maxFaces to " + faceCount);
            bool found = false;

            for(var i = 0; i < htmlLines.Length; i++){
                if(htmlLines[i].Contains("var numFaces = ")){
                    // Debug.Log("found numFaces Line... replacing: " + htmlLines[i]);
                    found = true;
                    htmlLines[i] = string.Format("\t\t\t\t\tvar numFaces = {0};", faceCount);
                    // Debug.Log("new Line: " + htmlLines[i]);
                }
            }
            if(!found){
                Debug.LogWarning("numFaces line not found in index.html! It's recommended to enable FaceTrackerGlobalSettings.dontOverrideFaceCount");
            }
                
            File.WriteAllLines(buildPath + "/index.html", htmlLines);
        }
    }
}

