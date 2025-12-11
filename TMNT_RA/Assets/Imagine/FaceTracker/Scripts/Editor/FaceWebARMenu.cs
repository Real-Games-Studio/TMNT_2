using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Imagine.WebAR.Editor
{
	public class FaceWebARMenu
	{
		[MenuItem("Assets/Imagine WebAR/Create/FaceObject - Avatar", false, 1010)]
		public static void CreateFaceObjectAvatar()
		{
			var faceIndex = GameObject.FindObjectsOfType<FaceObject>().Length;
			if(faceIndex >= 4)
			{
				EditorUtility.DisplayDialog("Maximum Face Object Size Reached", "The maximum number of faces you can have is 4", "Okay");
				return;
			}

			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imagine/FaceTracker/Prefabs/FaceObject - Avatar.prefab");
			GameObject gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
			Selection.activeGameObject = gameObject;

			gameObject.GetComponent<FaceObject>().faceIndex = faceIndex;
			gameObject.name = "FaceObject - Avatar [" + faceIndex + "]";
		}
		[MenuItem("Assets/Imagine WebAR/Create/FaceObject - FaceMesh", false, 1010)]
		public static void CreateFaceObjectFaceMesh()
		{
			var faceIndex = GameObject.FindObjectsOfType<FaceObject>().Length;
			if(faceIndex >= 4)
			{
				EditorUtility.DisplayDialog("Maximum Face Object Size Reached", "The maximum number of faces you can have is 4", "Okay");
				return;
			}

			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imagine/FaceTracker/Prefabs/FaceObject - FaceMesh.prefab");
			GameObject gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
			Selection.activeGameObject = gameObject;

			gameObject.GetComponent<FaceObject>().faceIndex = faceIndex;
			gameObject.name = "FaceObject - FaceMesh [" + faceIndex + "]";
		}
		[MenuItem("Assets/Imagine WebAR/Create/FaceObject - Wearable", false, 1010)]
		public static void CreateFaceObjectWearables()
		{
			var faceIndex = GameObject.FindObjectsOfType<FaceObject>().Length;
			if(faceIndex >= 4)
			{
				EditorUtility.DisplayDialog("Maximum Face Object Size Reached", "The maximum number of faces you can have is 4", "Okay");
				return;
			}

			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imagine/FaceTracker/Prefabs/FaceObject - Wearable.prefab");
			GameObject gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
			Selection.activeGameObject = gameObject;

			gameObject.GetComponent<FaceObject>().faceIndex = faceIndex;
			gameObject.name = "FaceObject - Wearable [" + faceIndex + "]";
		}

		

		[MenuItem("Assets/Imagine WebAR/Create/Face Tracker", false, 1100)]
		public static void CreateFaceTracker()
		{
			GameObject prefab = (GameObject)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imagine/FaceTracker/Prefabs/FaceTracker.prefab");
			GameObject gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
			Selection.activeGameObject = gameObject;
			gameObject.name = "FaceTracker";
		}
	}
}

