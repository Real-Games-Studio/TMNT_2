using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Imagine.WebAR.Editor{
    [CustomPropertyDrawer(typeof(BlendshapeMapElement))]
    public class BlendshapeMapElementDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var avatar = (SkinnedMeshRenderer)property.serializedObject.FindProperty("avatar").objectReferenceValue;
            // SkinnedMeshRenderer avatar = FindAvatar(property);
            if(avatar == null){
                
                var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                GUI.color = Color.yellow;
                EditorGUI.LabelField(rect, "Avatar is null! Set Avatar first!");
                GUI.color = Color.white;
                return;
            }
            // else{
            //     Debug.Log("Avatar null");
            // }
            var avatarMesh = avatar.sharedMesh;
            string[] avatarBlendshapeNames = new string[avatarMesh.blendShapeCount];
            for(var i = 0; i < avatarMesh.blendShapeCount; i++){
                avatarBlendshapeNames[i] = avatarMesh.GetBlendShapeName(i);
            }


            EditorGUI.BeginProperty(position, label, property);

            // Calculate rect for fields
            var avatarBlendshapeIndexRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var faceTrackerBlendShapeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
            var curveRect = new Rect(position.x, position.y + 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), position.width, EditorGUIUtility.singleLineHeight);

            // Draw fields - use FindPropertyRelative to get properties within the BlendshapeMapElement object
            // EditorGUI.PropertyField(avatarBlendshapeIndexRect, property.FindPropertyRelative("avatarBlendshapeIndex"));
            var avatarBlendshapeIndexProp = property.FindPropertyRelative("avatarBlendshapeIndex");
            avatarBlendshapeIndexProp.intValue = EditorGUI.Popup(avatarBlendshapeIndexRect, "Avatar Blendshape", avatarBlendshapeIndexProp.intValue, avatarBlendshapeNames);
            EditorGUI.PropertyField(faceTrackerBlendShapeRect, property.FindPropertyRelative("facetrackerBlendshape"));
            EditorGUI.PropertyField(curveRect, property.FindPropertyRelative("curve"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var avatar = (SkinnedMeshRenderer)property.serializedObject.FindProperty("avatar").objectReferenceValue;
            if(avatar == null){
                return EditorGUIUtility.singleLineHeight;
            }

            // Calculate the height of the property drawer
            return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
        }

        // SkinnedMeshRenderer FindAvatar(SerializedProperty property){
        //     var faceIndexStr = property.propertyPath.Split(new string[]{"faceSettings.Array.data[","].shapesMap.Array.data["}, System.StringSplitOptions.RemoveEmptyEntries);
        //     var faceIndex = int.Parse(faceIndexStr[0]);

        //     SerializedProperty faceSettingsListProp = property.serializedObject.FindProperty("faceSettings");
        //     SerializedProperty faceSettingsProp = faceSettingsListProp.GetArrayElementAtIndex(faceIndex);
        //     return (SkinnedMeshRenderer) faceSettingsProp.FindPropertyRelative("avatar").objectReferenceValue;
        // }
    }
}
