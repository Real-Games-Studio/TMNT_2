using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Imagine.WebAR {

    [System.Serializable]
    public class FaceShapeEvent{
        public string faceEventName;
        public List<FaceTrackerBlendShape> blendShapes;
        [Range(0,1)] public float threshold = 0.5f;

        public enum DetectMode {GREATER_THAN_THRESHOLD, LESS_THAN_THRESHOLD}
        public DetectMode detectMode;
        public UnityEvent OnDetected;
        public UnityEvent OnLost;

        [HideInInspector] float lastValue = 0.5f;

        public void CheckFaceEvent(Dictionary<string, float> shapeValues){
            float newValue = 0;
            int ctr = 0;
            
            foreach(var blendShape in blendShapes){
                var key = blendShape.ToString();
                if(!shapeValues.ContainsKey(key)) 
                    continue;
                
                newValue += shapeValues[key];
                ctr++;
            }
            newValue /= ctr;

            if( detectMode == DetectMode.GREATER_THAN_THRESHOLD) 
            {
                if(lastValue <= threshold && newValue > threshold){
                    OnDetected?.Invoke();
                }
                else if(lastValue >= threshold && newValue < threshold){
                    OnLost?.Invoke();
                }
            }
            else if( detectMode == DetectMode.LESS_THAN_THRESHOLD) 
            {
                if(lastValue >= threshold && newValue < threshold){
                    OnDetected?.Invoke();
                }
                else if(lastValue <= threshold && newValue > threshold){
                    OnLost?.Invoke();
                }
            }

            // Debug.Log("CheckFaceEvent " + faceEventName + ": last=" + lastValue + ", new=" + newValue + ", thresh = " + threshold);
            lastValue = newValue;     
        }

    }

    [System.Serializable]
    public class HeadAngleEvents{
        [HideInInspector]public float lastTiltV, lastTiltH, lastTurn;
        public float tiltVThreshold = 0.1f, tiltHThreshold = 0.1f, turnThreshold = 0.1f;

        public UnityEvent OnHeadTiltUp, OnHeadTiltDown, OnHeadTiltLeft, OnHeadTiltRight, OnHeadTurnLeft, OnHeadTurnRight;
        [Space(60)]
        public UnityEvent<float>OnTiltVertically;
        public UnityEvent<float>OnTiltHorizontally, OnTurn;
        
        public void CheckHeadAngles(FaceObject faceObject, Transform cam)
        {
            // Get directional vectors
            Vector3 faceForward = faceObject.transform.forward;
            Vector3 faceUp = faceObject.transform.up;
            Vector3 faceRight = faceObject.transform.right;

            Vector3 camForward = cam.forward;
            Vector3 camUp = cam.up;
            Vector3 camRight = cam.right;

            // Vertical Tilt (Pitch) - Compare forward vectors projected onto cam's right-up plane
            Vector3 faceForwardProjected = Vector3.ProjectOnPlane(faceForward, camRight);
            Vector3 camForwardProjected = Vector3.ProjectOnPlane(camForward, camRight);
            float tiltV = SignedDotProduct(faceForwardProjected, camForwardProjected, -camRight); // Use camRight as the reference normal

            // Horizontal Tilt (Roll) - Compare up vectors projected onto cam's forward-up plane
            Vector3 faceUpProjected = Vector3.ProjectOnPlane(faceUp, camForward);
            Vector3 camUpProjected = Vector3.ProjectOnPlane(camUp, camForward);
            float tiltH = SignedDotProduct(faceUpProjected, camUpProjected, camForward); // Use camForward as the reference normal

            // Turn (Yaw) - Compare right vectors projected onto cam's forward-right plane
            Vector3 faceRightProjected = Vector3.ProjectOnPlane(faceRight, camUp);
            Vector3 camRightProjected = Vector3.ProjectOnPlane(camRight, camUp);
            float turn = SignedDotProduct(faceRightProjected, camRightProjected, camUp); // Use camUp as the reference normal

            // Debug.Log("tiltV = " + tiltV);
            // Debug.Log("tiltH = " + tiltH);
            // Debug.Log("turn = " + turn);

            OnTiltVertically?.Invoke(tiltV);
            OnTiltHorizontally?.Invoke(tiltH);
            OnTurn?.Invoke(turn);

            if(lastTiltV < tiltVThreshold && tiltV > tiltVThreshold){
                OnHeadTiltUp?.Invoke();
            }
            else if(lastTiltV > -tiltVThreshold && tiltV < -tiltVThreshold){
                OnHeadTiltDown?.Invoke();
            }
            
            if(lastTiltH < tiltHThreshold && tiltH > tiltHThreshold){
                OnHeadTiltRight?.Invoke();
            }
            else if(lastTiltH > -tiltHThreshold && tiltH < -tiltHThreshold){
                OnHeadTiltLeft?.Invoke();
            }

            if(lastTurn < turnThreshold && turn > turnThreshold){
                OnHeadTurnRight?.Invoke();
            }
            else if(lastTurn > -turnThreshold && turn < -turnThreshold){
                OnHeadTurnLeft?.Invoke();
            }
        }

        static float SignedDotProduct(Vector3 a, Vector3 b, Vector3 referenceNormal)
        {
            float dot = Vector3.Dot(a, b);
            float sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(a, b), referenceNormal));
            return (1-dot) * sign;
        }
    }


    
    public partial class FaceTracker
    {

        void Start_Events(){
            
        }

        void OnUpdateFaceShapeEvents(FaceObject faceObject, Dictionary<string, float> shapeValues){
           foreach(var faceShapeEvent in faceObject.faceShapeEvents){
                faceShapeEvent.CheckFaceEvent(shapeValues);
            }
        }

        void OnUpdateHeadAngles(FaceObject faceObject, Transform cam){
           faceObject.headAngleEvents.CheckHeadAngles(faceObject, cam);
        }
    }
}