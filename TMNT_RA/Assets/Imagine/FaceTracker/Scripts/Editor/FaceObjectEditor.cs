using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Imagine.WebAR;
using UnityEditor.UIElements;
using System.IO;

namespace  Imagine.WebAR.Editor{
    [CustomEditor(typeof(FaceObject))]
    public class FaceObjectEditor : UnityEditor.Editor
    {
        FaceObject _target;

        // Mesh _editorSceneViewMesh;

        void OnEnable(){
            _target = (FaceObject)target;
            // _editorSceneViewMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Imagine/FaceTracker/Models/FaceMesh.fbx");
        }
        // bool shouldInitDeformationVectors = false;
        public override void OnInspectorGUI()
        {
            // if(shouldInitDeformationVectors){
            //     if(_target.deformedMesh != null){
            //         _target.InitDeformationVectors();
            //     }
            //     shouldInitDeformationVectors = false;
            // }

            //base.OnInspectorGUI();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("faceIndex"));

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var useFaceMeshProp = serializedObject.FindProperty("useFaceMesh");
            EditorGUILayout.PropertyField(useFaceMeshProp);
            if(useFaceMeshProp.boolValue){
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("faceMesh"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("otherFaceMeshes"));

                EditorGUILayout.Space();
                // EditorGUI.BeginChangeCheck();
                var deformedMeshProp = serializedObject.FindProperty("deformedMesh");
                EditorGUILayout.PropertyField(deformedMeshProp);
                // var deformTextureProp = serializedObject.FindProperty("deformTexture");
                // EditorGUILayout.PropertyField(deformTextureProp);

                var deformStrengthProp = serializedObject.FindProperty("deformStrength");
                EditorGUILayout.PropertyField(deformStrengthProp);

                //  var deformMaxValProp = serializedObject.FindProperty("maxDeformVal");
                // EditorGUILayout.PropertyField(deformMaxValProp);
            
                // if(EditorGUI.EndChangeCheck()){
                //     shouldInitDeformationVectors = true;
                // }

                if(_target.deformedMesh != null){
                    if(GUILayout.Button("Bake Deformation To Material")){
                        _target.InitDeformationVectors();
                        var filePath = EditorUtility.SaveFilePanel("Save Deformation Map", "Assets/Imagine/FaceTracker/Textures", _target.deformedMesh.name + "_deformationMap", "png");
                        Debug.Log(filePath);
                        if(!string.IsNullOrEmpty(filePath)){
                            var pngBytes = _target.deformTexture.EncodeToPNG();
                            File.WriteAllBytes(filePath, pngBytes);
                            var assetPath = "Assets" + filePath.Replace(Application.dataPath,"");
                            // Debug.Log("assetPath = " + assetPath);

                            AssetDatabase.Refresh();

                            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                            textureImporter.npotScale = TextureImporterNPOTScale.None;
                            textureImporter.filterMode = FilterMode.Point;
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);


                            var texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                            _target.deformTexture = texAsset;
                            var faceMeshMat =  _target.faceMesh.GetComponent<MeshRenderer>().sharedMaterial;
                            faceMeshMat.shader = Shader.Find("Imagine/FaceDeformation");
                            faceMeshMat.SetTexture("_DeformationMap", texAsset);
                            faceMeshMat.SetFloat("_EditMode", 1);
                            Selection.activeObject = texAsset;
                        }
                    }
                }
                EditorGUI.indentLevel--;

            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var useBlendShapesProp = serializedObject.FindProperty("useBlendshapes");
            EditorGUILayout.PropertyField(useBlendShapesProp);
            if(useBlendShapesProp.boolValue){
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("avatar"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shapesMap"));
                
                EditorGUILayout.Space();
                var smoothenBlendshapesProp = serializedObject.FindProperty("smoothenBlendshapes");
                EditorGUILayout.PropertyField(smoothenBlendshapesProp);
                if(smoothenBlendshapesProp.boolValue){
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothFactor"));
                }

                EditorGUILayout.Space();
                var faceShapeEventsProp = serializedObject.FindProperty("faceShapeEvents");
                EditorGUILayout.PropertyField(faceShapeEventsProp);

                EditorGUILayout.Space();
                var headAngleEventsProp = serializedObject.FindProperty("headAngleEvents");
                EditorGUILayout.PropertyField(headAngleEventsProp);

                EditorGUILayout.Space();
                var compensateScaleOnMouthOpenProp = serializedObject.FindProperty("compensateScaleOnMouthOpen");
                EditorGUILayout.PropertyField(compensateScaleOnMouthOpenProp);
                if(compensateScaleOnMouthOpenProp.boolValue){
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleOnMouthFullyOpen"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleTransformsOnMouthOpen"));
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dontHideOnFaceLost"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flipOnFrontCam"));

            // EditorGUILayout.PropertyField(serializedObject.FindProperty("_editorSceneViewMesh"));

            serializedObject.ApplyModifiedProperties();

        }
    }


}

