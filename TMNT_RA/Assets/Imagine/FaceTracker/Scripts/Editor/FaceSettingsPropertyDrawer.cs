// using UnityEditor;
// using UnityEngine;
// using System.Text.RegularExpressions;
// using System.Collections.Generic;

// namespace Imagine.WebAR.Editor{
//     [CustomPropertyDrawer(typeof(FaceSettings))]
//     public class FaceSettingsDrawer : PropertyDrawer
//     {
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             var index = GetListIndex(property);
            
//             // DrawBackground(position, property, label, color);

//             var isActive = (index <= FaceTrackerGlobalSettings.Instance.maxFaces - 1);
//             GUI.enabled = isActive;
//             // Debug.Log("maxFaces = " + FaceTrackerGlobalSettings.Instance.maxFaces + ", enabled = " + enabled + ", index = " + index);

    
//             EditorGUI.BeginProperty(position, label, property);

//             Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
//             EditorGUI.LabelField(labelRect, "Face " + index + (isActive ? "" : " (unused)"), EditorStyles.boldLabel);
//             position.x += 20;// tab
//             position.width -= 20;

//             position.y += EditorGUIUtility.singleLineHeight;
//             Rect modeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//             SerializedProperty modeProp = property.FindPropertyRelative("mode");
//             EditorGUI.PropertyField(modeRect, modeProp);

//             if((FaceSettings.FaceSettingsMode)modeProp.enumValueIndex == FaceSettings.FaceSettingsMode.NONE){
//                 EditorGUI.EndProperty();
//                 return;
//             }

//             else if ((FaceSettings.FaceSettingsMode)modeProp.enumValueIndex == FaceSettings.FaceSettingsMode.DUPLICATE)
//             {
//                 position.y += EditorGUIUtility.singleLineHeight;
//                 Rect duplicateIndexRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//                 SerializedProperty duplicateIndexProp = property.FindPropertyRelative("duplicateIndex");
//                 EditorGUI.PropertyField(duplicateIndexRect, duplicateIndexProp);
//                 EditorGUI.EndProperty();
//                 return;
//             }

//             position.y += 20; //Space 

//             position.y += EditorGUIUtility.singleLineHeight;
//             Rect faceObjectRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//             SerializedProperty faceObjectProp = property.FindPropertyRelative("faceObject");
//             EditorGUI.PropertyField(faceObjectRect, faceObjectProp);

//             position.y += 20; //Space 
            
//             position.y += EditorGUIUtility.singleLineHeight;
//             Rect useFaceMeshRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//             SerializedProperty useFaceMeshProp = property.FindPropertyRelative("useFaceMesh");
//             EditorGUI.PropertyField(useFaceMeshRect, useFaceMeshProp);

//             if (useFaceMeshProp.boolValue)
//             {
//                 position.y += EditorGUIUtility.singleLineHeight;
//                 Rect faceMeshRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//                 SerializedProperty faceMeshProp = property.FindPropertyRelative("faceMesh");
//                 EditorGUI.PropertyField(faceMeshRect, faceMeshProp);
//             }

//             position.y += 20; //Space 

//             position.y += EditorGUIUtility.singleLineHeight;
//             Rect useBlendShapesRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//             SerializedProperty useBlendShapesProp = property.FindPropertyRelative("useBlendShapes");
//             EditorGUI.PropertyField(useBlendShapesRect, useBlendShapesProp);

//             if (useBlendShapesProp.boolValue)
//             {
//                 position.y += EditorGUIUtility.singleLineHeight;
//                 Rect avatarRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//                 SerializedProperty avatarProp = property.FindPropertyRelative("avatar");
//                 EditorGUI.PropertyField(avatarRect, avatarProp);
                
//                 position.y += EditorGUIUtility.singleLineHeight;
//                 Rect blendShapeMapRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
//                 SerializedProperty blendShapeMapProp = property.FindPropertyRelative("shapesMap");
//                 EditorGUI.PropertyField(blendShapeMapRect, blendShapeMapProp);
//             }

//             position.y += EditorGUIUtility.singleLineHeight;
//             EditorGUI.EndProperty();
//         }

//         public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//         {
//             if ((FaceSettings.FaceSettingsMode)property.FindPropertyRelative("mode").enumValueIndex == FaceSettings.FaceSettingsMode.NONE)
//                 return EditorGUIUtility.singleLineHeight * 3;
            
//             else if ((FaceSettings.FaceSettingsMode)property.FindPropertyRelative("mode").enumValueIndex == FaceSettings.FaceSettingsMode.DUPLICATE)
//                 return EditorGUIUtility.singleLineHeight * 4;

//             //SET MANUALLY
//             float lines = 5; // Base lines
//             if ((FaceSettings.FaceSettingsMode)property.FindPropertyRelative("mode").enumValueIndex == FaceSettings.FaceSettingsMode.DUPLICATE)
//                 lines++; // Add one line for duplicateIndex
//             if (property.FindPropertyRelative("useFaceMesh").boolValue)
//                 lines++; // Add one line for faceMesh
//             if (property.FindPropertyRelative("useBlendShapes").boolValue)
//             {
//                 lines += 2; // Add one line for avatar and shapesMap
//                 var shapesMapProp = property.FindPropertyRelative("shapesMap");
//                 if(shapesMapProp.isExpanded){
//                     lines += (4.44f * shapesMapProp.arraySize + 2);
//                 }

//             }

//             //Add on line for spacing
//             lines++;

//             return EditorGUIUtility.singleLineHeight * lines + 60;//30px for spaces
//         }

//         private int GetListIndex(SerializedProperty property)
//         {
//             string propertyPath = property.propertyPath;
//             string indexString = propertyPath.Substring(propertyPath.IndexOf("[") + 1, propertyPath.IndexOf("]") - propertyPath.IndexOf("[") - 1);
//             int index;
//             if (int.TryParse(indexString, out index))
//             {
//                 return index;
//             }
//             return -1; // Return -1 if unable to parse index
//         }
        
//     }
// }
