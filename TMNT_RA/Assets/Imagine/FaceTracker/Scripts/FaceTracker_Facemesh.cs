using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Imagine.WebAR{
    public partial class FaceTracker
    {
        private Vector3[] vertices = new Vector3[468];

        void Start_FaceMesh(){
            foreach(var fo in faceObjects){
                if(fo == null || !fo.useFaceMesh)
                    continue;

                fo.faceMesh.mesh.MarkDynamic();

                //init deformations
                if(fo.deformedMesh != null){
                    var faceRenderer = fo.faceMesh.GetComponent<MeshRenderer>();
                    if(faceRenderer.material.shader == Shader.Find("Imagine/FaceDeformation")){
#if UNITY_EDITOR
                        faceRenderer.material.SetFloat("_EditMode", 1);
#else
                        faceRenderer.material.SetFloat("_EditMode", 0);
#endif
                    }
                    // fo.InitDeformationVectors();
                }
            }
        }
        
        void OnUpdateFaceVerticesLocal(string vertString){
            vertString = vertString.Replace("\n","");
            var vals = vertString.Split(",", System.StringSplitOptions.RemoveEmptyEntries);
            int faceIndex = int.Parse(vals[0]);
            // Debug.Log("OnUpdateFaceVerticesLocal " + faceIndex);

            vals = vals.Skip(1).ToArray();

            var fo = faceObjects[faceIndex];
            if(fo == null || !fo.useFaceMesh)
                return;

            var faceMesh = fo.faceMesh;
            var otherFaceMeshes = fo.otherFaceMeshes;
            var deformedMesh = fo.deformedMesh;

            
            for(var i = 0; i < 468; i++){
                vertices[i] = new Vector3(
                    float.Parse(vals[i * 3 + 0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(vals[i * 3 + 1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(vals[i * 3 + 2], System.Globalization.CultureInfo.InvariantCulture)
                );

                // if(fo.deformedMesh != null){
                //     vertices[i] += (Vector3)fo.deformationOffsets[i];
                // }

                // facePoints[i].localPosition = vertices[i];
            }
            faceMesh.mesh.vertices = vertices;
            faceMesh.mesh.RecalculateNormals();

            foreach(var other in otherFaceMeshes){
                other.mesh.vertices = vertices;
                other.mesh.RecalculateNormals();
            }
        }
    }
}

