using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Imagine.WebAR
{

    public partial class FaceTracker : MonoBehaviour
    {
        [DllImport("__Internal")] private static extern void StartWebGLfTracker(string name);
        [DllImport("__Internal")] private static extern void StopWebGLfTracker();
        [DllImport("__Internal")] private static extern bool IsWebGLfTrackerReady();
        [DllImport("__Internal")] private static extern void SetWebGLfTrackerSettings(string settings);

        [HideInInspector][SerializeField] FaceObject[] faceObjects = new FaceObject[4];

        [SerializeField] ARCamera trackerCam;
        [SerializeField] private float faceDetectionDebounce = 0.3f; // Time to wait before hiding face after loss
        Camera cam;

        private Vector3 right, up, forward, pos, origScale;
        Dictionary<FaceObject, Vector3> origFaceObjectScales = new Dictionary<FaceObject, Vector3>();
        Dictionary<int, float> faceLostTimers = new Dictionary<int, float>(); // Track time since face was lost
        Dictionary<int, Coroutine> hideFaceCoroutines = new Dictionary<int, Coroutine>(); // Track hide coroutines



#if UNITY_EDITOR
        public enum FaceObjectGizmoType { MESH_GIZMO, WIRE_GIZMO, MESH_AND_WIRE_GIZMO, NONE };
        private static FaceTracker _editorInstance;
        [Space(10)]
        [Header("Editor Gizmos")]
        [SerializeField] FaceObjectGizmoType faceObjectGizmoType;
        [SerializeField] Color faceObjectGizmoColor = new Color(1, 1, 1, 0.2f);
        public static FaceObjectGizmoType GetFaceObjectGizmoType
        {
            get
            {
                if (_editorInstance == null)
                {
                    _editorInstance = FindFirstObjectByType<FaceTracker>();
                }
                return _editorInstance.faceObjectGizmoType;
            }
        }
        public static Color GetFaceObjectGizmoColor
        {
            get
            {
                if (_editorInstance == null)
                {
                    _editorInstance = FindFirstObjectByType<FaceTracker>();
                }
                return _editorInstance.faceObjectGizmoColor;
            }
        }

        static Mesh _editorSceneViewMesh;
        public static Mesh EditorSceneViewMesh
        {
            get
            {
                if (_editorSceneViewMesh == null)
                {
                    Debug.Log("try get EditorFaceMeshGizmo_WithNormals.mesh");
                    _editorSceneViewMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Imagine/FaceTracker/Models/EditorFaceMeshGizmo_WithNormals.mesh");
                }
                return _editorSceneViewMesh;
            }
        }
#endif
        [HideInInspector][SerializeField] float debugCamMoveSensitivity = 20, debugCamTiltSensitivity = 20;


        void Start()
        {
            cam = trackerCam.GetComponent<Camera>();

            Debug.Log($"[FACETRACKER] Procurando FaceObjects na cena...");
            var foundFaceObjects = FindObjectsByType<FaceObject>(FindObjectsSortMode.None);
            Debug.Log($"[FACETRACKER] Encontrados {foundFaceObjects.Length} FaceObjects");

            foreach (var fo in foundFaceObjects)
            {
                if (faceObjects[fo.faceIndex] != null)
                {
                    Debug.LogWarning(string.Format("Ignoring duplicate FaceObject with index = {0} ({1})", fo.faceIndex, fo.name));
                    continue;
                }
                faceObjects[fo.faceIndex] = fo;
                fo.gameObject.SetActive(false);
                Debug.Log($"[FACETRACKER] ✓ FaceObject registrado: {fo.name} (index={fo.faceIndex}, filhos={fo.transform.childCount})");

                origFaceObjectScales.Add(fo, fo.transform.localScale);
            }

            Debug.Log($"[FACETRACKER] Array faceObjects preenchido: [{(faceObjects[0] != null ? "✓" : "X")}][{(faceObjects[1] != null ? "✓" : "X")}][{(faceObjects[2] != null ? "✓" : "X")}][{(faceObjects[3] != null ? "✓" : "X")}]");

            // Corrige os targets dos PositionTrackers para corresponderem aos FaceObjects corretos
            FixPositionTrackerTargets();

            Start_Blendshapes();
            Start_FaceMesh();

#if UNITY_EDITOR

#endif

#if !UNITY_EDITOR && UNITY_WEBGL
            StartTracker();
            SetTrackerSettings();
#endif
        }

        void Update()
        {
            Update_BlendShapesInternal();

#if UNITY_EDITOR
            Update_Debug();
#endif
        }

        public void StartTracker()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartWebGLfTracker(name);
#endif
        }

        public void StopTracker()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StopWebGLfTracker();
#endif
        }

        void SetTrackerSettings()
        {

            var useFaceMesh = false;
            var useBlendshapes = false;

            foreach (var fo in faceObjects)
            {
                useFaceMesh = useFaceMesh || fo.useFaceMesh;
                useBlendshapes = useBlendshapes || fo.useBlendshapes;
            }

            var json = "{";
            json += "\"USE_FACEMESH\":" + (useFaceMesh ? "true" : "false") + ",";
            json += "\"USE_BLENDSHAPES\":" + (useBlendshapes ? "true" : "false");
            json += "}";
            SetWebGLfTrackerSettings(json);
        }

        void OnFaceFound(int faceIndex)
        {
            Debug.Log($"[MULTIFACE] 🔍 OnFaceFound chamado para faceIndex={faceIndex}");

            var fo = faceObjects[faceIndex];
            if (fo == null)
            {
                Debug.LogWarning($"[MULTIFACE] ⚠️ Invalid Face ID Found. FaceObject NULL para faceIndex={faceIndex}");
                Debug.LogWarning("Invalid Face ID Found. Your scene's FaceObject count doesn't match FaceTrackerGlobalSettings maxFaces. This can cause detection problems.");
                fo = faceObjects[0];
                // return;
            }

            // Cancel any pending hide operation for this face
            if (hideFaceCoroutines.ContainsKey(faceIndex) && hideFaceCoroutines[faceIndex] != null)
            {
                StopCoroutine(hideFaceCoroutines[faceIndex]);
                hideFaceCoroutines[faceIndex] = null;
            }

            bool wasActive = fo.gameObject.activeSelf;
            fo.gameObject.SetActive(true);

            Debug.Log($"[MULTIFACE] ✓ FaceObject {faceIndex} ({fo.name}): wasActive={wasActive}, nowActive={fo.gameObject.activeSelf}, filhos={fo.transform.childCount}");

            if (faceObjects.Length != FaceTrackerGlobalSettings.Instance.maxFaces)
            {
            }
        }

        void OnFaceLost(int faceIndex)
        {
            var fo = faceObjects[faceIndex];
            if (fo == null)
            {
                Debug.LogWarning("Invalid Face ID Lost. Your scene's FaceObject count doesn't match FaceTrackerGlobalSettings maxFaces. This can cause detection problems.");
                fo = faceObjects[0];
                // return;
            }

            if (!fo.dontHideOnFaceLost)
            {
                // Start debounce timer before hiding the face
                if (hideFaceCoroutines.ContainsKey(faceIndex) && hideFaceCoroutines[faceIndex] != null)
                {
                    StopCoroutine(hideFaceCoroutines[faceIndex]);
                }

                hideFaceCoroutines[faceIndex] = StartCoroutine(HideFaceAfterDelay(faceIndex, faceDetectionDebounce));
            }
        }

        private System.Collections.IEnumerator HideFaceAfterDelay(int faceIndex, float delay)
        {
            yield return new WaitForSeconds(delay);

            var fo = faceObjects[faceIndex];
            if (fo != null && !fo.dontHideOnFaceLost)
            {
                fo.gameObject.SetActive(false);
            }

            // Clean up the coroutine reference
            if (hideFaceCoroutines.ContainsKey(faceIndex))
            {
                hideFaceCoroutines[faceIndex] = null;
            }
        }

        private void OnDestroy()
        {
            StopTracker();
        }

        void OnUpdateFaceTransform(string data)
        {
            // Debug.Log("update face transform");
            ParseData(data);
        }

        void ParseData(string data)
        {
            string[] values = data.Split(new char[] { ',' });

            int faceIndex = int.Parse(values[0]);

            var fo = faceObjects[faceIndex];
            if (fo == null)
            {
                Debug.LogWarning($"[MULTIFACE] ⚠️ FaceObject é NULL para faceIndex={faceIndex}");
                //This can happen if your faceobject count doesn't match FaceTrackerGlobalSettings.maxFaces
                return;
            }

            if (!fo.gameObject.activeSelf)
            {
                Debug.Log($"[MULTIFACE] ✓ Ativando FaceObject {faceIndex} ({fo.name}) com {fo.transform.childCount} filhos");
                OnFaceFound(faceIndex);
            }

            forward.x = float.Parse(values[4], System.Globalization.CultureInfo.InvariantCulture);
            forward.y = float.Parse(values[5], System.Globalization.CultureInfo.InvariantCulture);
            forward.z = float.Parse(values[6], System.Globalization.CultureInfo.InvariantCulture);

            up.x = float.Parse(values[7], System.Globalization.CultureInfo.InvariantCulture);
            up.y = float.Parse(values[8], System.Globalization.CultureInfo.InvariantCulture);
            up.z = float.Parse(values[9], System.Globalization.CultureInfo.InvariantCulture);

            right.x = float.Parse(values[10], System.Globalization.CultureInfo.InvariantCulture);
            right.y = float.Parse(values[11], System.Globalization.CultureInfo.InvariantCulture);
            right.z = float.Parse(values[12], System.Globalization.CultureInfo.InvariantCulture);

            var rot = Quaternion.LookRotation(forward, up);

            // Debug.Log("Unity fwd = " +  forward);

            pos.x = float.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
            pos.y = float.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);
            pos.z = float.Parse(values[3], System.Globalization.CultureInfo.InvariantCulture);

            // DEBUG: Log position for each face
            Debug.Log($"[MULTIFACE] Face {faceIndex}: pos=({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");

            origScale = origFaceObjectScales[fo];

            if (trackerCam.isFlipped)
            {
                rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y * -1, rot.eulerAngles.z * -1);
                pos.x *= -1;
                if (fo.flipOnFrontCam)
                    origScale.x *= -1;
            }

            fo.transform.position = trackerCam.transform.TransformPoint(pos);
            fo.transform.rotation = trackerCam.transform.rotation * rot;
            fo.transform.localScale = origScale;

            //head angle events
            OnUpdateHeadAngles(fo, trackerCam.transform);


        }

        private void Update_Debug()
        {
            var x_left = Input.GetKey(KeyCode.A);
            var x_right = Input.GetKey(KeyCode.D);
            var z_forward = Input.GetKey(KeyCode.W); ;
            var z_back = Input.GetKey(KeyCode.S); ;
            var y_up = Input.GetKey(KeyCode.R);
            var y_down = Input.GetKey(KeyCode.F);

            float speed = debugCamMoveSensitivity * Time.deltaTime;
            float dx = (x_right ? speed : 0) + (x_left ? -speed : 0);
            float dy = (y_up ? speed : 0) + (y_down ? -speed : 0);
            //float dsca = 1 + (z_forward ? speed : 0) + (z_back ? -speed : 0);
            float dz = (z_forward ? speed : 0) + (z_back ? -speed : 0);


            var y_rot_left = Input.GetKey(KeyCode.LeftArrow);
            var y_rot_right = Input.GetKey(KeyCode.RightArrow);
            var x_rot_up = Input.GetKey(KeyCode.UpArrow);
            var x_rot_down = Input.GetKey(KeyCode.DownArrow);
            var z_rot_cw = Input.GetKey(KeyCode.Comma);
            var z_rot_ccw = Input.GetKey(KeyCode.Period);

            var angularSpeed = debugCamTiltSensitivity * Time.deltaTime; //degrees per frame
            var d_rotx = (x_rot_up ? angularSpeed : 0) + (x_rot_down ? -angularSpeed : 0);
            var d_roty = (y_rot_right ? angularSpeed : 0) + (y_rot_left ? -angularSpeed : 0);
            var d_rotz = (z_rot_ccw ? angularSpeed : 0) + (z_rot_cw ? -angularSpeed : 0);

            Quaternion rot;
            rot.w = trackerCam.transform.rotation.w;
            rot.x = trackerCam.transform.rotation.x;
            rot.y = trackerCam.transform.rotation.y;
            rot.z = trackerCam.transform.rotation.z;

            // rot = new Quaternion(i, j, k, w);

            var dq = Quaternion.Euler(d_rotx, d_roty, d_rotz);
            rot *= dq;
            //rot *= Quaternion.AngleAxis(d_rotz, trackerCamera.transform.forward);
            //rot *= Quaternion.AngleAxis(d_roty, trackerCamera.transform.up);
            //rot *= Quaternion.AngleAxis(d_rotx, trackerCamera.transform.right);

            //Debug.Log(dx + "," + dy + "," + dsca);
            var dp = Vector3.right * dx + Vector3.up * dy + Vector3.forward * dz;
            trackerCam.transform.Translate(dp);
            trackerCam.transform.rotation = rot;
        }

        private void FixPositionTrackerTargets()
        {
            Debug.Log("[FACETRACKER] Verificando e corrigindo targets dos PositionTrackers...");

            var allTrackers = FindObjectsByType<PositionTracker>(FindObjectsSortMode.None);
            Debug.Log($"[FACETRACKER] Encontrados {allTrackers.Length} PositionTrackers");

            foreach (var tracker in allTrackers)
            {
                // Identifica o índice do tracker pelo nome
                string trackerName = tracker.name.Replace("HeadTrackerObjectHolder", "").Trim();
                int trackerIndex = 0;

                if (trackerName.StartsWith("(") && trackerName.EndsWith(")"))
                {
                    string numberStr = trackerName.Substring(1, trackerName.Length - 2);
                    if (int.TryParse(numberStr, out int parsed))
                    {
                        trackerIndex = parsed;
                    }
                }

                // Verifica se o FaceObject correto existe
                if (faceObjects[trackerIndex] == null)
                {
                    Debug.LogWarning($"[FACETRACKER] ⚠️ FaceObject[{trackerIndex}] não existe para {tracker.name}");
                    continue;
                }

                // Verifica se o target atual está correto
                var currentTarget = tracker.Target;
                var correctTarget = faceObjects[trackerIndex].transform;

                if (currentTarget != correctTarget)
                {
                    Debug.LogWarning($"[FACETRACKER] ❌ {tracker.name} (índice {trackerIndex}) estava linkado ao {currentTarget?.name ?? "NULL"}. Corrigindo para {correctTarget.name}...");

                    // Usa reflection para modificar o campo privado target
                    var field = typeof(PositionTracker).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(tracker, correctTarget);
                        Debug.Log($"[FACETRACKER] ✓ {tracker.name} agora está linkado corretamente ao {correctTarget.name}");
                    }
                }
                else
                {
                    Debug.Log($"[FACETRACKER] ✓ {tracker.name} (índice {trackerIndex}) já está corretamente linkado ao {correctTarget.name}");
                }
            }
        }

    }
}


