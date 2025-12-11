using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Imagine.WebAR{

    public class FaceObject : MonoBehaviour
    {
        [Range(0,3)] public int faceIndex = 0;

        public bool useFaceMesh = false;
        public MeshFilter faceMesh;
        public List<MeshFilter> otherFaceMeshes;
        public Mesh deformedMesh;
        public Texture2D deformTexture;
        public Vector3 deformMinVals, deformMaxVals;
        [Range(0,2)]public float deformStrength = 1;
        // [HideInInspector] public Vector4[] deformationOffsets;
        private Vector3[] canonicalFaceVerts;


        public bool useBlendshapes = false;
        public SkinnedMeshRenderer avatar;
        public List<BlendshapeMapElement> shapesMap;
        public bool smoothenBlendshapes = true;
        [Range(1, 50)] public float smoothFactor = 20;


        public bool compensateScaleOnMouthOpen = false;
        [Range(0.5f, 1)]public float scaleOnMouthFullyOpen = 0.9f;
        public List<Transform> scaleTransformsOnMouthOpen = new List<Transform>();
        public Dictionary<Transform, Vector3> origScales = new Dictionary<Transform, Vector3>();
        public Dictionary<string, BlendshapeMapElement> shapesDictionary = new Dictionary<string, BlendshapeMapElement>();

        public List<FaceShapeEvent> faceShapeEvents = new List<FaceShapeEvent>();
        public HeadAngleEvents headAngleEvents;



        public bool dontHideOnFaceLost = false;

        public bool flipOnFrontCam = false;

        public void InitCanonicalFaceVerts(){
            canonicalFaceVerts = new Vector3[468];
            for(var i = 0; i < faceMesh.mesh.vertexCount; i++){
                canonicalFaceVerts[i] = faceMesh.mesh.vertices[i];
            }
        }

        // public void SetDeformedMesh(Mesh deformedMesh){
        //     this.deformedMesh = deformedMesh;
        //     InitDeformationVectors();
        // }

        public void InitDeformationVectors(){
            var deformationOffsets = new Vector3[468];

            for(var i = 0; i < 468; i++){
#if UNITY_EDITOR
                var v = faceMesh.sharedMesh.vertices[i];
#else
                var v = canonicalFaceVerts[i];
#endif
                deformationOffsets[i] = (deformedMesh.vertices[i] - v) * deformStrength;

                Debug.Log(i + "->" + deformationOffsets[i]);
                }

            // Shader.SetGlobalVectorArray("_DeformationOffsets", deformationOffsets);
            deformTexture = ConvertToTexture(deformationOffsets);

            var faceMat = faceMesh.GetComponent<MeshRenderer>().sharedMaterial;
            faceMat.SetTexture("_DeformationMap", deformTexture);
            faceMat.SetVector("_DeformMinVals", deformMinVals);
            faceMat.SetVector("_DeformMaxVals", deformMaxVals);

        }

        public Texture2D ConvertToTexture(Vector3[] points)
        {
            if (points.Length != 468)
            {
                Debug.LogError("Array must contain exactly 468 Vector3 elements.");
                return null;
            }

            int width = 26;
            int height = 18;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Point;

            // Determine min/max values and store them
            deformMinVals = new Vector3(
                points.Min(p => p.x),
                points.Min(p => p.y),
                points.Min(p => p.z)
            );

            deformMaxVals = new Vector3(
                points.Max(p => p.x),
                points.Max(p => p.y),
                points.Max(p => p.z)
            );

            Color[] colors = new Color[468];

            for (int i = 0; i < 468; i++)
            {
                Vector3 p = points[i];
                float r = Mathf.InverseLerp(deformMinVals.x, deformMaxVals.x, p.x);
                float g = Mathf.InverseLerp(deformMinVals.y, deformMaxVals.y, p.y);
                float b = Mathf.InverseLerp(deformMinVals.z, deformMaxVals.z, p.z);
                colors[i] = new Color(r, g, b, 1f);
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        void Awake(){
            InitCanonicalFaceVerts();
        }

#if UNITY_EDITOR
        void OnDrawGizmos(){
            Gizmos.color = FaceTracker.GetFaceObjectGizmoColor;
            if( FaceTracker.GetFaceObjectGizmoType == FaceTracker.FaceObjectGizmoType.MESH_GIZMO || 
                FaceTracker.GetFaceObjectGizmoType == FaceTracker.FaceObjectGizmoType.MESH_AND_WIRE_GIZMO){
                Gizmos.DrawMesh(FaceTracker.EditorSceneViewMesh, 0, transform.position, transform.rotation);
            }
            if( FaceTracker.GetFaceObjectGizmoType == FaceTracker.FaceObjectGizmoType.WIRE_GIZMO || 
                FaceTracker.GetFaceObjectGizmoType == FaceTracker.FaceObjectGizmoType.MESH_AND_WIRE_GIZMO){
                    
                Gizmos.DrawWireMesh(FaceTracker.EditorSceneViewMesh, 0, transform.position, transform.rotation);
            }
        }
#endif

        public void SetMakeupAlpha(float alpha){
            var r = faceMesh.GetComponent<MeshRenderer>();
            var col = r.material.GetColor("_BlushColor");
            var newCol = new Color(col.r, col.g, col.b, alpha);
            r.material.SetColor("_BlushColor", newCol);
        }

    }
}
