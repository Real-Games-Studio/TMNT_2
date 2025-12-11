using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Imagine.WebAR.Editor{

[CustomEditor(typeof(FaceTracker))]
    public class FaceTrackerEditor : UnityEditor.Editor
    {
        FaceTracker _target;

        void OnEnable(){
            _target = (FaceTracker)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // var maxFaces = FaceTrackerGlobalSettings.Instance.maxFaces;

            DrawEditorDebugger();

            serializedObject.ApplyModifiedProperties();
        }

        bool showKeyboardCameraControls = false;
        void DrawEditorDebugger(){
            //Editor Runtime Debugger
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Debug Mode");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if(Application.IsPlaying(_target)){
                //Enable Disable
                var faceObjectsProp = serializedObject.FindProperty("faceObjects");                
                for(var i = 0; i < faceObjectsProp.arraySize; i++){
                    var faceObjectProp = faceObjectsProp.GetArrayElementAtIndex(i);
                    
                    var face = (FaceObject)(faceObjectProp.objectReferenceValue);
                    if(face != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var name = face.gameObject.name;
                        EditorGUILayout.LabelField(name);

                        var faceFound = face.gameObject.activeInHierarchy;

                        GUI.enabled = !faceFound;
                        if(GUILayout.Button("Found")){
                            _target.SendMessage("OnFaceFound",i);

                            // var imageTargetTransform = ((Transform)imageTargetProp.FindPropertyRelative("transform").objectReferenceValue);
                            var cam = ((ARCamera)serializedObject.FindProperty("trackerCam").objectReferenceValue).transform;

                            cam.transform.position = face.transform.position + face.transform.forward * -75;
                            cam.LookAt(face.transform);
                        }
                        GUI.enabled = faceFound;
                        if(GUILayout.Button("Lost")){
                             _target.SendMessage("OnFaceLost",i);

                        }
                        GUI.enabled = true;
                        EditorGUILayout.EndHorizontal();
                    }
                        
                }    

                  
            }
            else{
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("Enter Play-mode to Debug In Editor");
                GUI.color = Color.white;
            }

            EditorGUILayout.Space();
            //keyboard camera controls
            showKeyboardCameraControls = EditorGUILayout.Toggle ("Show Keyboard Camera Controls", showKeyboardCameraControls);
            if(showKeyboardCameraControls){
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("W", "Move Forward (Z)");
                EditorGUILayout.LabelField("S", "Move Backward (Z)");
                EditorGUILayout.LabelField("A", "Move Left (X)");
                EditorGUILayout.LabelField("D", "Move Right (X)");
                EditorGUILayout.LabelField("R", "Move Up (Y)");
                EditorGUILayout.LabelField("F", "Move Down (Y)");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Up Arrow", "Tilt Up (along X-Axis)");
                EditorGUILayout.LabelField("Down Arrow", "Tilt Down (along X-Axis)");
                EditorGUILayout.LabelField("Left Arrow", "Tilt Left (along Y-Axis)");
                EditorGUILayout.LabelField("Right Arrow", "Tilt Right (Along Y-Axis)");
                EditorGUILayout.LabelField("Period", "Tilt Clockwise (Along Z-Axis)");
                EditorGUILayout.LabelField("Comma", "Tilt Counter Clockwise (Along Z-Axis)");
                EditorGUILayout.Space(40);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugCamMoveSensitivity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugCamTiltSensitivity"));
                EditorGUILayout.EndVertical();
                
            }    

            EditorGUILayout.EndVertical();
        }
    }
}

