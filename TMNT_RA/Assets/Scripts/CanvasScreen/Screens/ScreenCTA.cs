using System.Collections.Generic;
using UnityEngine;

public class ScreenCTA : CanvasScreen
{

    [SerializeField] private List<GameObject> objectsToCallNextScreen;
    [SerializeField] private float activeSecondsThreshold = 2f; // seconds an object must remain active

    [SerializeField] private List<PositionTracker> positionTrackersToReset;

    // track how long each object has been continuously active
    private readonly Dictionary<GameObject, float> activeTimers = new Dictionary<GameObject, float>();
    private bool nextScreenCalled = false;
    void Update()
    {
        if (IsOn())
        {
            // update timers for each tracked object
            if (objectsToCallNextScreen == null || objectsToCallNextScreen.Count == 0) return;

            // initialize missing keys
            foreach (var obj in objectsToCallNextScreen)
            {
                if (obj == null) continue;
                if (!activeTimers.ContainsKey(obj)) activeTimers[obj] = 0f;
            }

            foreach (var obj in objectsToCallNextScreen)
            {
                if (obj == null) continue;

                if (obj.activeInHierarchy)
                {
                    activeTimers[obj] += Time.deltaTime;
                    if (!nextScreenCalled && activeTimers[obj] >= activeSecondsThreshold)
                    {
                        nextScreenCalled = true;

                        foreach (var tracker in positionTrackersToReset)
                        {
                            if (tracker != null && tracker.Target != null && tracker.Target.gameObject.activeInHierarchy)
                            {
                                tracker.ActivateChildByFaceIndex();
                            }
                        }

                        CallNextScreen();
                        break;
                    }
                }
                else
                {
                    // reset timer when object stops being active
                    activeTimers[obj] = 0f;
                }
            }
        }
        else
        {
            // when screen is off, reset state so it can trigger again next time
            if (activeTimers.Count > 0) activeTimers.Clear();
            nextScreenCalled = false;
        }
    }
}
