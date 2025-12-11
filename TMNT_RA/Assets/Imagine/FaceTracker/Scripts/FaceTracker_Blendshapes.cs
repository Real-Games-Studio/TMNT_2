using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Imagine.WebAR {

    [System.Serializable]
    public class BlendshapeMapElement{
        public int avatarBlendshapeIndex;

        public FaceTrackerBlendShape facetrackerBlendshape;
        public BlendshapeMappingCurve curve = BlendshapeMappingCurve.LINEAR;

        public float GetCurveValue(float t){
            if(curve == BlendshapeMappingCurve.LINEAR){
                return t;
            }
            else if(curve == BlendshapeMappingCurve.QUAD_EASE_IN){
                return t * t; 
            }
            else if(curve == BlendshapeMappingCurve.QUAD_EASE_OUT){
                return 2*t - t * t;
            }
            else if (curve == BlendshapeMappingCurve.QUAD_EASE_IN_OUT){
                t *= 2;
                if (t < 1)
                    return 0.5f * t * t;
                t--;
                return -0.5f * (t * (t - 2) - 1);
            }
            else{
                return t;
            }
        }
    }
    public partial class FaceTracker
    {
        // [SerializeField] bool useBlendShapes = true;
        //[SerializeField] SkinnedMeshRenderer avatar;


        private Dictionary<FaceObject, Dictionary<int, float>> targetWeightsDicts = new Dictionary<FaceObject, Dictionary<int, float>>();

        void Start_Blendshapes(){
            
            foreach(var fo in faceObjects){
                if(fo == null || !fo.useBlendshapes)
                    continue;
                
                var weightsDict = new Dictionary<int, float>();
                foreach(var el in fo.shapesMap){
                    var key = Enum.GetName(typeof(FaceTrackerBlendShape), el.facetrackerBlendshape);
                    fo.shapesDictionary.Add(key, el);

                    weightsDict.Add(el.avatarBlendshapeIndex, 0);
                }

                targetWeightsDicts.Add(fo, weightsDict);

                Debug.Log("Started face with " + fo.shapesMap.Count + " Blendshapes");

                //record orig object scales
                if(fo.compensateScaleOnMouthOpen){
                    foreach(var scaledTransform in fo.scaleTransformsOnMouthOpen){
                        fo.origScales.Add(scaledTransform, scaledTransform.localScale);
                    }
                }
                
            }        
        }

        void OnUpdateBlendShapes(string blendshapesString){
            // Debug.Log(blendshapesString);

            int ctr = 0;
            var rows = blendshapesString.Split(new string[]{";"}, System.StringSplitOptions.RemoveEmptyEntries);
            var faceId = int.Parse(rows[0]);
            // Debug.Log("OnUpdateBlendShapes " + faceId);
            rows = rows.Skip(1).ToArray();

            var fo = faceObjects[faceId];
            if(fo == null || !fo.useBlendshapes)
                return;

            var shapesDictionary = fo.shapesDictionary;
            var shapeValues = new Dictionary<string, float>();
            var avatar = fo.avatar;

            foreach(var row in rows){
                
                var fields = row.Split(new string[]{"="}, System.StringSplitOptions.RemoveEmptyEntries);
                var key = fields[0];
                var weight = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                shapeValues.Add(key, weight);


                if(shapesDictionary.ContainsKey(key)){
                    var index = shapesDictionary[key].avatarBlendshapeIndex;
                    weight = shapesDictionary[key].GetCurveValue(weight);
                    
                    if(!fo.smoothenBlendshapes){
                        avatar.SetBlendShapeWeight(index, weight * 100);
                    }
                    else{
                        targetWeightsDicts[fo][index] = weight * 100;
                    }

                    
                    // Debug.LogFormat("[{0}] {1} - {2}", index, key, weight);
                    ctr++;
                }

                if( key == "jawOpen" && 
                    fo.compensateScaleOnMouthOpen &&
                    fo.scaleTransformsOnMouthOpen.Count > 0
                ){
                    // var weight = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                    var scale = Mathf.Lerp(1, fo.scaleOnMouthFullyOpen, weight);

                    foreach(var scaledTransform in fo.scaleTransformsOnMouthOpen){
                        var origScale = fo.origScales[scaledTransform];
                        scaledTransform.localScale = scale * origScale;
                    }
                }
            }

            //check faceshape events
            OnUpdateFaceShapeEvents(fo, shapeValues);

            // Debug.Log("Updated " + ctr + " blendshapes");
        }

        void Update_BlendShapesInternal(){
            foreach(var fo in faceObjects){
                if(fo != null && fo.useBlendshapes && fo.smoothenBlendshapes){
                    var weightsDict = targetWeightsDicts[fo];
                    foreach(var el in weightsDict){
                        var smoothedWeight = Mathf.Lerp(fo.avatar.GetBlendShapeWeight(el.Key), el.Value, Time.deltaTime * fo.smoothFactor);
                        fo.avatar.SetBlendShapeWeight(el.Key, smoothedWeight);
                    }
                }
            }
        }
        
    }


    public enum FaceTrackerBlendShape{
        _neutral,
        browDownLeft,
        browDownRight,
        browInnerUp,
        browOuterUpLeft,
        browOuterUpRight,
        cheekPuff,
        cheekSquintLeft,
        cheekSquintRight,
        eyeBlinkLeft,
        eyeBlinkRight,
        eyeLookDownLeft,
        eyeLookDownRight,
        eyeLookInLeft,
        eyeLookInRight,
        eyeLookOutLeft,
        eyeLookOutRight,
        eyeLookUpLeft,
        eyeLookUpRight,
        eyeSquintLeft,
        eyeSquintRight,
        eyeWideLeft,
        eyeWideRight,
        jawForward,
        jawLeft,
        jawOpen,
        jawRight,
        mouthClose,
        mouthDimpleLeft,
        mouthDimpleRight,
        mouthFrownLeft,
        mouthFrownRight,
        mouthFunnel,
        mouthLeft,
        mouthLowerDownLeft,
        mouthLowerDownRight,
        mouthPressLeft,
        mouthPressRight,
        mouthPucker,
        mouthRight,
        mouthRollLower,
        mouthRollUpper,
        mouthShrugLower,
        mouthShrugUpper,
        mouthSmileLeft,
        mouthSmileRight,
        mouthStretchLeft,
        mouthStretchRight,
        mouthUpperUpLeft,
        mouthUpperUpRight,
        noseSneerLeft,
        noseSneerRight,
    }

    public enum BlendshapeMappingCurve {
        LINEAR,
        QUAD_EASE_IN,
        QUAD_EASE_OUT,
        QUAD_EASE_IN_OUT
    }
}

